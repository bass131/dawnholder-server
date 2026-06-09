using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// BossBehaviorSystem 회귀 안전망.
///
/// **검증 invariant**:
///   1. 페이즈 전환: HP 51% → IsPhase2 false 유지 / HP ≤ 50% → true / 1회성 idempotent
///   2. 공격 시퀀스: player in range + 충분 틱 → S_EnemyAttack ≥1 검증 (옛 정확 틱 산술 하드코딩 제거)
///   3. 범위 내/밖 데미지: 범위 내 플레이어만 S_EnemyAttack + HP 감소 / 범위 밖 무변화
///      ⚠️ 배회 가장자리 고려: BossX=22, PatrolRange=4 → 배회 x∈[18,26].
///         "범위 밖" = BossX+15(x=37). 배회 가장자리(x=26)서도 |dx|=11>AggroRange(7) → aggro 없음.
///   4. 데미지 = 서버 계산: damage == Formulas.ComputeDamage(BossDefault(), 플레이어 Stats, BossBaseDamage)
///   5. 사망→리스폰: HP 낮게 세팅 → 보스 공격 → Position==PlayerSpawnPosition + Hp==Stats.MaxHp + ActionFsm != DeathState
///   6. drift 방지: BossDefault().MaxHp == EnemyDefaultHp Boss 값(100) 일치 (spawn된 boss.MaxHp 간접 검증)
///   7. ProtocolVersion == 10 assert + S_EnemyAttack/S_PlayerJoin(characterClass 포함) 직렬화 왕복
///   8. animState 우선순위 — Attack > Hit
///   9. 보스 이동 행동: 배회(aggro 밖), 접근 후 공격, Walk animState
///
/// **테스트 전략**:
///   - GameMap 직접 주입 → singleton race 차단.
///   - Send override로 broadcast 패킷 캡처.
///   - 시간 의존 X — 충분한 틱(80~120) 돌리고 결과 검증 (옛 정확 산술 하드코딩 제거).
///   - 이유: blind-timer 폐기로 공격 시점이 탐지/Move 전환 틱 추가로 가변적.
///
/// **entity id 풀 약속** (BossRoom 맵, Normal 없이 Boss만):
///   - Boss entityId=1 (첫 AllocId)
///   - Player entityId=2 (다음 발급)
/// </summary>
[Collection("ConsoleSerial")]
public class BossBehaviorTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    const int BossEntityId = 1;
    const int PlayerEntityId = 2;
    const float BossX = 22f;
    const float BossY = 0f;
    const int BossMaxHp = 100;

    // 보스 데미지 계산 — Formulas 직접 참조로 drift 방지.
    // BossDefault().Attack=12, Knight().Defense=5, BossBaseDamage=8
    // Max(1, 8 + 12 - 5) = 15.
    static readonly int ExpectedBossDamage = Formulas.ComputeDamage(
        EnemyStats.BossDefault(), PlayerStats.Knight(), CombatConstants.BossBaseDamage);

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }

        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();
            SetCharacterClass(0); // Knight
            EnterGameWorldIfReady();
        }
    }

    public BossBehaviorTests()
    {
        // BossRoom: Boss 1마리만. PlayerSpawnPosition = (22, 0).
        var content = new MapContent(22f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, BossX, BossY),
        });
        _map = new GameMap(MapId.BossRoom, content: content);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    TestGameSession SetupSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshake();
        _map.Tick(1);
        return s;
    }

    // 보스 ±BossAttackHalfExtent(2.5f) 범위 안 좌표 — Boss=(22,0), 범위 안 = x∈[19.5, 24.5].
    static void PlaceInBossRange(PlayerEntity player)
        => player.Position = new Vector2(BossX + 1f, BossY); // x=23, 범위 안

    // 보스 범위 밖 좌표.
    // ⚠️ 배회 가장자리 고려: BossX=22, PatrolRange=4 → 배회 x∈[18,26].
    // BossX+10(x=32)은 배회 가장자리(x=26)서 dx=6 < AggroRange(7) → aggro 발생 → 테스트 깨짐.
    // BossX+15(x=37): 배회 가장자리(x=26)서도 dx=11 > AggroRange(7) → 안전.
    static void PlaceOutsideBossRange(PlayerEntity player)
        => player.Position = new Vector2(BossX + 15f, BossY); // x=37, 배회 가장자리서도 aggro 밖

    // ─── 항목 1: 페이즈 전환 ────────────────────────────────────────────────────

    [Fact]
    public void Phase2Transition_AboveThreshold_IsPhase2RemainsFalse()
    {
        // HP > 50% 구간에서 IsPhase2 = false 유지.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // 51% HP = 51. threshold = MaxHp * 0.5 = 50.
        boss.Hp = 51;

        _map.Tick(2);

        Assert.False(boss.IsPhase2, "HP=51(51%) 구간에서 IsPhase2가 true로 전환되면 안 됨");
    }

    [Fact]
    public void Phase2Transition_AtThreshold_IsPhase2BecomesTrue()
    {
        // HP = 50% (= 50) 시 IsPhase2 true 전환.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        boss.Hp = 50;

        _map.Tick(2);

        Assert.True(boss.IsPhase2, "HP=50(50%) 시 IsPhase2 true 전환 필요");
    }

    [Fact]
    public void Phase2Transition_BelowThreshold_IsPhase2BecomesTrue()
    {
        // HP < 50% (= 49) 시 IsPhase2 true 전환.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        boss.Hp = 49;

        _map.Tick(2);

        Assert.True(boss.IsPhase2);
    }

    [Fact]
    public void Phase2Transition_Idempotent_OnlyTransitionsOnce()
    {
        // 이미 IsPhase2=true인 상태에서 추가 tick이 흘러도 재진입/재설정 없음 — 1회성.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        boss.Hp = 50;
        _map.Tick(2); // IsPhase2 true 전환
        Assert.True(boss.IsPhase2);

        // HP를 다시 51로 올려도 Phase2가 false로 되돌아가지 않음.
        boss.Hp = 51;
        _map.Tick(3);

        Assert.True(boss.IsPhase2, "Phase2 전환은 1회성 — HP가 다시 올라가도 false로 되돌아가지 않음");
    }

    // ─── 항목 2: 공격 시퀀스 ─────────────────────────────────────────────────

    [Fact]
    public void Phase1_TelegraphStartsAfterCooldown_SendsEntityStateAttack()
    {
        // player를 trigger 사거리에 두고 80틱 → S_EntityState animState=Attack broadcast ≥1
        // + TelegraphTicksRemaining>0 또는 AttackLatchTicks>0 (telegraph 진입 확인).
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        bool hasTelegraphSignal = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Any(p =>
            {
                S_EntityState pkt = new S_EntityState();
                pkt.Read(new ArraySegment<byte>(p));
                return pkt.entityId == BossEntityId && pkt.animState == (byte)AnimState.Attack;
            });

        Assert.True(hasTelegraphSignal,
            "80틱 이내에 S_EntityState(entityId=Boss, animState=Attack) broadcast 필요");
    }

    [Fact]
    public void Phase1_S_EnemyAttackSentAfterTelegraph()
    {
        // player in range + 충분 틱(80) → S_EnemyAttack ≥1 + 페이로드 검증.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        int attackCount = CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack);
        Assert.True(attackCount >= 1, "80틱 이내에 S_EnemyAttack ≥1 필요");

        byte[] attackPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EnemyAttack);
        S_EnemyAttack parsed = new S_EnemyAttack();
        parsed.Read(new ArraySegment<byte>(attackPkt));
        Assert.Equal(BossEntityId, parsed.attackerId);
        Assert.Equal(PlayerEntityId, parsed.targetId);
        Assert.Equal(0, parsed.attackPattern); // 페이즈 1 = 0
    }

    [Fact]
    public void Phase2_TelegraphAndCooldownAccelerated()
    {
        // player in range + 강제 phase2 → 충분 틱(80) → S_EnemyAttack attackPattern=1 검증.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // 페이즈 2 강제 전환.
        boss.Hp = 50;
        boss.IsPhase2 = true;
        boss.AttackCooldownTicks = CombatConstants.BossPhase2CooldownTicks;

        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        int attackCount = CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack);
        Assert.True(attackCount >= 1, "페이즈 2 가속 후 80틱 이내에 S_EnemyAttack ≥1 필요");

        byte[] attackPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EnemyAttack);
        S_EnemyAttack parsed = new S_EnemyAttack();
        parsed.Read(new ArraySegment<byte>(attackPkt));
        Assert.Equal(1, parsed.attackPattern); // 페이즈 2 = 1
    }

    // ─── 항목 3: 범위 내/밖 데미지 ──────────────────────────────────────────────

    [Fact]
    public void BossAttack_PlayerInRange_ReceivesDamage()
    {
        // 범위 내 플레이어는 S_EnemyAttack 수신 + HP 감소.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        int hpBefore = player!.Hp;

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        Assert.True(player.Hp < hpBefore, "범위 안 플레이어 HP가 감소해야 함");
        Assert.True(CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack) >= 1);
    }

    [Fact]
    public void BossAttack_PlayerOutOfRange_NoDamage()
    {
        // 범위 밖 플레이어는 S_EnemyAttack 수신 X, HP 무변화.
        // ⚠️ BossX+15(x=37): 배회 가장자리(x=26)서도 dx=11>AggroRange(7) → aggro 없음 (배회 함정 봉합).
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceOutsideBossRange(player!); // x=37

        int hpBefore = player!.Hp;

        for (long t = 2; t <= 100; t++)
            _map.Tick(t);

        Assert.Equal(hpBefore, player.Hp);
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack));
    }

    // ─── 항목 4: 데미지 = 서버 계산 ────────────────────────────────────────────

    [Fact]
    public void BossAttack_DamageMatchesFormula()
    {
        // damage == Formulas.ComputeDamage(BossDefault(), PlayerStats.Knight(), BossBaseDamage).
        // Knight Defense=5: Max(1, 8+12-5)=15.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        int hpBefore = player!.Hp;

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        byte[] attackPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EnemyAttack);
        S_EnemyAttack parsed = new S_EnemyAttack();
        parsed.Read(new ArraySegment<byte>(attackPkt));

        Assert.Equal(ExpectedBossDamage, parsed.damage);
        Assert.Equal(hpBefore - ExpectedBossDamage, parsed.targetCurrentHp);
        Assert.Equal(hpBefore - ExpectedBossDamage, player.Hp);
    }

    // ─── 항목 5: 사망→리스폰 ─────────────────────────────────────────────────────

    [Fact]
    public void BossAttack_PlayerDies_Respawns()
    {
        // 플레이어 HP를 보스 1격으로 죽을 만큼 낮게 세팅 → 보스 공격 → 리스폰 검증.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        player!.Hp = ExpectedBossDamage - 1; // 1격에 사망

        Vector2 expectedSpawn = new Vector2(22f, 0f);

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        Assert.Equal(expectedSpawn, player.Position);
        Assert.Equal(player.Stats.MaxHp, player.Hp);
        Assert.False(player.ActionFsm.CurrentState is Dawnholder.Server.GameServer.Maps.States.DeathState,
            "리스폰 후 ActionFsm이 DeathState면 안 됨 — Revive()로 Idle로 복귀해야 함");
    }

    [Fact]
    public void BossAttack_PlayerRespawns_HpFull()
    {
        // 리스폰 후 HP == Stats.MaxHp (Knight MaxHp=100).
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        player!.Hp = 1;

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        Assert.Equal(player.Stats.MaxHp, player.Hp);
    }

    // ─── 항목 6: drift 방지 ──────────────────────────────────────────────────────

    [Fact]
    public void BossMaxHp_MatchesEnemyDefaultHpTable()
    {
        // spawn된 boss.MaxHp == 100 (EnemyDefaultHp.ByKind[Boss]) 일치 검증.
        SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        Assert.Equal(BossMaxHp, boss.MaxHp);
        Assert.Equal(EnemyStats.BossDefault().MaxHp, boss.MaxHp);
    }

    // ─── 항목 8: animState 우선순위 — Attack > Hit ─────────────────────────────

    [Fact]
    public void AnimState_DuringTelegraph_HitDoesNotOverrideAttack()
    {
        // telegraph 진입 후 HitLatchTicks 세팅 → broadcast animState는 Attack 유지 (Hit 아님).
        // 새 모델: player를 trigger 사거리에 두고 TelegraphTicksRemaining>0 될 때까지 틱 진행.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // Telegraph 진입까지 틱 진행 (최대 80틱).
        for (long t = 2; t <= 80; t++)
        {
            _map.Tick(t);
            if (boss.TelegraphTicksRemaining > 0 && boss.AttackLatchTicks > 0) break;
        }

        Assert.True(boss.TelegraphTicksRemaining > 0, "telegraph 진입 확인 (80틱 내)");
        Assert.True(boss.AttackLatchTicks > 0, "AttackLatchTicks 세팅 확인");

        // 피격 세팅 — telegraph 중 플레이어가 보스를 때린 상황 시뮬.
        boss.HitLatchTicks = CombatConstants.AnimLatchTicks;

        s.SentPackets.Clear();

        // 현재 tick에서 다음 SnapshotTickInterval 경계 틱 찾기.
        long currentTick = 80;
        while (currentTick % Constants.SnapshotTickInterval != 0) currentTick++;
        _map.Tick(currentTick);

        byte[] statePkt = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Select(p => { S_EntityState d = new(); d.Read(new ArraySegment<byte>(p)); return d; })
            .Where(d => d.entityId == BossEntityId)
            .Select(d => new byte[] { d.animState })
            .LastOrDefault() ?? Array.Empty<byte>();

        Assert.True(statePkt.Length > 0, "S_EntityState broadcast 발생 필요");
        Assert.Equal((byte)AnimState.Attack, statePkt[0]);
    }

    [Fact]
    public void AnimState_OutsideTelegraph_HitShowsCorrectly()
    {
        // telegraph/AttackLatch 없는 상태(쿨다운 중)에서 HitLatchTicks 세팅 → animState == Hit (회귀 보장).
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // 쿨다운 중 상태 확인 (AttackLatchTicks == 0).
        Assert.Equal(0, boss.AttackLatchTicks);

        // 피격 세팅.
        boss.HitLatchTicks = CombatConstants.AnimLatchTicks;

        s.SentPackets.Clear();

        // 다음 SnapshotTickInterval 경계 틱 진행.
        long broadcastTick = 2 + (Constants.SnapshotTickInterval - (2 % Constants.SnapshotTickInterval));
        _map.Tick(broadcastTick);

        byte[] statePkt = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Select(p => { S_EntityState d = new(); d.Read(new ArraySegment<byte>(p)); return d; })
            .Where(d => d.entityId == BossEntityId)
            .Select(d => new byte[] { d.animState })
            .LastOrDefault() ?? Array.Empty<byte>();

        Assert.True(statePkt.Length > 0, "S_EntityState broadcast 발생 필요");
        Assert.Equal((byte)AnimState.Hit, statePkt[0]);
    }

    // ─── 항목 7: ProtocolVersion + 직렬화 왕복 ────────────────────────────────────

    [Fact]
    public void ProtocolVersion_Is10()
    {
        Assert.Equal(10, ProtocolVersion.Current);
    }

    [Fact]
    public void S_EnemyAttack_RoundTrip_PreservesAllFields()
    {
        // S_EnemyAttack(ID 20): attackerId/targetId/damage/targetCurrentHp/attackPattern 왕복 검증.
        S_EnemyAttack pkt = new S_EnemyAttack
        {
            attackerId = 1,
            targetId = 2,
            damage = 15,
            targetCurrentHp = 85,
            attackPattern = 1,
        };

        ArraySegment<byte> bytes = pkt.Write();
        S_EnemyAttack decoded = new S_EnemyAttack();
        decoded.Read(bytes);

        Assert.Equal(1, decoded.attackerId);
        Assert.Equal(2, decoded.targetId);
        Assert.Equal(15, decoded.damage);
        Assert.Equal(85, decoded.targetCurrentHp);
        Assert.Equal((byte)1, decoded.attackPattern);
    }

    [Fact]
    public void S_EnemyAttack_RoundTrip_BoundaryValues()
    {
        // 사망 직후 음수 HP + attackPattern byte 최대값 경계 왕복.
        S_EnemyAttack pkt = new S_EnemyAttack
        {
            attackerId = int.MaxValue,
            targetId = 1,
            damage = 9999,
            targetCurrentHp = -10,
            attackPattern = 255,
        };

        ArraySegment<byte> bytes = pkt.Write();
        S_EnemyAttack decoded = new S_EnemyAttack();
        decoded.Read(bytes);

        Assert.Equal(int.MaxValue, decoded.attackerId);
        Assert.Equal(1, decoded.targetId);
        Assert.Equal(9999, decoded.damage);
        Assert.Equal(-10, decoded.targetCurrentHp);
        Assert.Equal((byte)255, decoded.attackPattern);
    }

    [Fact]
    public void S_EnemyAttack_Write_ProducesCorrectPacketId()
    {
        // PDL.xml 20번째 정의 = PacketID 20.
        S_EnemyAttack pkt = new S_EnemyAttack { attackerId = 0, targetId = 0, damage = 0, targetCurrentHp = 0, attackPattern = 0 };

        ArraySegment<byte> bytes = pkt.Write();

        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset + 2, 2));
        Assert.Equal((ushort)PacketID.S_EnemyAttack, packetId);
        Assert.Equal((ushort)20, packetId);
    }

    [Fact]
    public void S_PlayerJoin_RoundTrip_IncludesCharacterClass()
    {
        // S_PlayerJoin(ID 9): characterClass byte 포함 왕복 검증 (v9 append).
        S_PlayerJoin pkt = new S_PlayerJoin
        {
            entityId = 5,
            spawnX = 22f,
            spawnY = 0f,
            characterClass = (byte)CharacterClass.Knight,
        };

        ArraySegment<byte> bytes = pkt.Write();
        S_PlayerJoin decoded = new S_PlayerJoin();
        decoded.Read(bytes);

        Assert.Equal(5, decoded.entityId);
        Assert.Equal(22f, decoded.spawnX);
        Assert.Equal(0f, decoded.spawnY);
        Assert.Equal((byte)CharacterClass.Knight, decoded.characterClass);
    }

    [Fact]
    public void S_PlayerJoin_Write_ProducesCorrectSizeWithCharacterClass()
    {
        // [size:2][id:2][entityId:4][spawnX:4][spawnY:4][characterClass:1] = 17 bytes.
        S_PlayerJoin pkt = new S_PlayerJoin
        {
            entityId = 0, spawnX = 0f, spawnY = 0f, characterClass = 0,
        };

        ArraySegment<byte> bytes = pkt.Write();

        Assert.Equal(17, bytes.Count);
        ushort size = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes.Array!, bytes.Offset, 2));
        Assert.Equal(17, size);
    }

    // ─── 항목 9: 보스 이동 행동 ──────────────────────────────────────────────────

    [Fact]
    public void Boss_NoTargetInAggro_WandersWithoutAttacking()
    {
        // player를 BossX+15(aggro 밖)로. 100틱 → boss.X != BossX(이동했음) + S_EnemyAttack 0회.
        // 배회 가장자리(x=26)서도 |dx|=11 > AggroRange(7) → aggro 없음.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceOutsideBossRange(player!); // x=37

        float initialX = boss.X;
        s.SentPackets.Clear();

        for (long t = 2; t <= 100; t++)
            _map.Tick(t);

        Assert.NotEqual(initialX, boss.X);  // 배회로 이동했음
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack)); // 공격 없음
    }

    [Fact]
    public void Boss_DetectsAndApproaches_ThenAttacks()
    {
        // player를 aggro 안·trigger 밖(BossX+5=x=27, dx=5: AggroRange(7) 안, TriggerRange(2.5) 밖).
        // 120틱 → 도중 boss.X가 player 쪽으로 이동(접근) + 최종 S_EnemyAttack ≥1.
        // (MoveChase 0.075/틱: dx=5→2.5 좁히는 데 ~33틱 + telegraph16 → 넉넉히 120틱.)
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        player!.Position = new Vector2(BossX + 5f, BossY); // x=27, aggro 안 trigger 밖

        float initialX = boss.X;
        s.SentPackets.Clear();

        for (long t = 2; t <= 120; t++)
            _map.Tick(t);

        // 보스가 player 쪽으로 이동했음 (초기 위치에서 오른쪽으로 이동).
        Assert.True(boss.X > initialX, "보스가 player 방향(우)으로 접근 이동해야 함");
        Assert.True(CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack) >= 1,
            "120틱 이내에 S_EnemyAttack ≥1 필요");
    }

    // ─── S_PlayerHp 송신 검증 ────────────────────────────────────────────────────

    static S_PlayerHp? LastPlayerHpPacket(List<byte[]> sent)
    {
        byte[]? raw = sent.LastOrDefault(p => PacketIdOf(p) == PacketID.S_PlayerHp);
        if (raw == null) return null;
        S_PlayerHp pkt = new S_PlayerHp();
        pkt.Read(new ArraySegment<byte>(raw));
        return pkt;
    }

    [Fact]
    public void BossAttack_PlayerHit_SendsS_PlayerHpWithReducedHp()
    {
        // 피격 직후 S_PlayerHp(currentHp < before) 송신 검증.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        int hpBefore = player!.Hp;
        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        // S_EnemyAttack ≥1 전제 (피격 발생 확인).
        Assert.True(CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack) >= 1,
            "피격 전제: S_EnemyAttack ≥1 필요");

        S_PlayerHp? hpPkt = LastPlayerHpPacket(s.SentPackets);
        Assert.NotNull(hpPkt);
        Assert.Equal(PlayerEntityId, hpPkt!.entityId);
        Assert.True(hpPkt.currentHp < hpBefore,
            $"피격 후 currentHp({hpPkt.currentHp}) < before({hpBefore}) 필요");
        Assert.Equal(player.MaxHp, hpPkt.maxHp);
    }

    [Fact]
    public void BossAttack_PlayerDies_SendsS_PlayerHpWithFullHpOnRevive()
    {
        // 부활 시 S_PlayerHp(currentHp == maxHp) 송신 검증 — 표시 미러 제거의 핵심.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        player!.Hp = ExpectedBossDamage - 1; // 1격 사망 세팅
        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        // 부활 확인 (HP full 복구).
        Assert.Equal(player.Stats.MaxHp, player.Hp);

        // 마지막 S_PlayerHp = 부활 full HP 통지.
        S_PlayerHp? hpPkt = LastPlayerHpPacket(s.SentPackets);
        Assert.NotNull(hpPkt);
        Assert.Equal(PlayerEntityId, hpPkt!.entityId);
        Assert.Equal(player.MaxHp, hpPkt.currentHp);
        Assert.Equal(player.MaxHp, hpPkt.maxHp);
    }

    [Fact]
    public void BossAttack_PlayerHit_CurrentHpIsNonNegative()
    {
        // 음수 HP 피격(1격 사망 포함)에도 S_PlayerHp.currentHp >= 0 보장 (floor 검증).
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        player!.Hp = 1; // 어떤 피격에도 음수 HP 발생 가능
        s.SentPackets.Clear();

        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        foreach (byte[] raw in s.SentPackets.Where(p => PacketIdOf(p) == PacketID.S_PlayerHp))
        {
            S_PlayerHp pkt = new S_PlayerHp();
            pkt.Read(new ArraySegment<byte>(raw));
            Assert.True(pkt.currentHp >= 0,
                $"S_PlayerHp.currentHp={pkt.currentHp} 음수 불가 — Math.Max(0, Hp) floor 필요");
        }
    }

    [Fact]
    public void Boss_StaysInRange_AttacksRepeatedly()
    {
        // player가 trigger 사거리에 계속 머물면 쿨다운(40)+dwell 소비 후 재탐지→재telegraph→2회차 공격.
        // 1회차 ~t58, 쿨다운40 카운트(Idle dwell) 후 2회차 ~t117. 200틱이면 ≥2회 확정.
        // 회귀망: 쿨다운/dwell 통합이 깨져(예: cooldown 리셋 누락) 2회차 영구 정지 시 이 테스트가 잡음.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // 1격사 방지 — player HP를 충분히 높게(여러 대 버티게).
        player!.Hp = 9999;

        s.SentPackets.Clear();

        for (long t = 2; t <= 200; t++)
            _map.Tick(t);

        Assert.True(CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack) >= 2,
            "200틱 내 연속 공격 ≥2회 (Attack→Idle→Move→Telegraph→Attack 루프 재순환)");
    }

    [Fact]
    public void Boss_TargetFleesBeyondDeAggro_StopsChasingAndReturnsToWander()
    {
        // player가 aggro 안(x27)서 탐지·추격되다 de-aggro 거리(>AggroRange*1.5=10.5) 밖으로 도주 →
        // 보스가 TargetEntityId 해제 + 더는 공격 안 함(배회 복귀). 회귀: 도주 player 영원히 추격 버그 방지.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        player!.Position = new Vector2(BossX + 5f, BossY); // x=27, aggro(7) 안 trigger(2.5) 밖 → 추격 시작

        // 보스가 Move로 추격 시작할 때까지 틱 진행 (타겟 획득 확인).
        for (long t = 2; t <= 60; t++)
        {
            _map.Tick(t);
            if (boss.Fsm!.CurrentState is BossMoveState && boss.TargetEntityId.HasValue) break;
        }
        Assert.True(boss.TargetEntityId.HasValue, "추격 시작(타겟 획득) 확인");

        // player를 de-aggro 거리 밖으로 도주(x=50: 보스 어디서든 dx>10.5).
        player.Position = new Vector2(50f, BossY);
        s.SentPackets.Clear();

        for (long t = 61; t <= 160; t++)
            _map.Tick(t);

        // 타겟 해제 + 도주 후 공격 0 (배회로 복귀, x50은 배회 가장자리서도 aggro 밖).
        Assert.Null(boss.TargetEntityId);
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack));
    }

    [Fact]
    public void Boss_InMoveState_BroadcastsWalkAnimState()
    {
        // player BossX+15(배회 유도). Move 상태 진입까지 틱 진행 후,
        // AttackLatch=0·HitLatch=0인 broadcast 경계 틱에서 animState==Walk 확인.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceOutsideBossRange(player!); // x=37, 배회 유도

        // Move 상태 진입까지 틱 (최대 60틱 — Idle dwell 40 + 여유).
        long moveTick = 1;
        for (long t = 2; t <= 60; t++)
        {
            _map.Tick(t);
            moveTick = t;
            if (boss.Fsm!.CurrentState is BossMoveState) break;
        }

        Assert.IsType<BossMoveState>(boss.Fsm!.CurrentState);

        // AttackLatch=0, HitLatch=0 확인 (배회 중엔 없어야).
        Assert.Equal(0, boss.AttackLatchTicks);
        Assert.Equal(0, boss.HitLatchTicks);

        s.SentPackets.Clear();

        // 다음 SnapshotTickInterval 경계 틱에서 broadcast.
        long broadcastTick = moveTick + 1;
        while (broadcastTick % Constants.SnapshotTickInterval != 0) broadcastTick++;
        _map.Tick(broadcastTick);

        byte[] statePkt = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Select(p => { S_EntityState d = new(); d.Read(new ArraySegment<byte>(p)); return d; })
            .Where(d => d.entityId == BossEntityId)
            .Select(d => new byte[] { d.animState })
            .LastOrDefault() ?? Array.Empty<byte>();

        Assert.True(statePkt.Length > 0, "S_EntityState broadcast 발생 필요");
        Assert.Equal((byte)AnimState.Walk, statePkt[0]);
    }
}
