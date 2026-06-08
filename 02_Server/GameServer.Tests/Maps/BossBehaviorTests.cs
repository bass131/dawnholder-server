using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Maps;

/// <summary>
/// BossBehaviorSystem 회귀 안전망.
///
/// **검증 invariant** (M4.5 Phase 04 완료 조건 7항목):
///   1. 페이즈 전환: HP 51% → IsPhase2 false 유지 / HP ≤ 50% → true / 1회성 idempotent
///   2. 쿨다운 tick 정확성: BossPhase1CooldownTicks(40) 후 telegraph 시작 (S_EntityState animState=Attack)
///      → +BossTelegraphTicks(16) 후 S_EnemyAttack 송신. 페이즈 2 가속(24/10틱) 검증
///   3. 범위 내/밖 데미지: 범위 내 플레이어만 S_EnemyAttack + HP 감소 / 범위 밖 무변화
///   4. 데미지 = 서버 계산: damage == Formulas.ComputeDamage(BossDefault(), 플레이어 Stats, BossBaseDamage)
///   5. 사망→리스폰: HP 낮게 세팅 → 보스 공격 → Position==PlayerSpawnPosition + Hp==Stats.MaxHp + ActionFsm != DeathState
///   6. drift 방지: BossDefault().MaxHp == EnemyDefaultHp Boss 값(100) 일치 (spawn된 boss.MaxHp 간접 검증)
///   7. ProtocolVersion == 9 assert + S_EnemyAttack/S_PlayerJoin(characterClass 포함) 직렬화 왕복
///
/// **테스트 전략**:
///   - GameMap 직접 주입 → singleton race 차단.
///   - Send override로 broadcast 패킷 캡처.
///   - BossStageClearTests 픽스처 패턴 정합.
///   - 시간 의존 X — GameMap.Tick(n)을 정확한 횟수만큼 호출하는 tick 카운터 방식.
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

    // 보스 범위 밖 좌표 — BossAttackHalfExtent=2.5f이므로 |dx|>3 이상이면 AABB miss.
    static void PlaceOutsideBossRange(PlayerEntity player)
        => player.Position = new Vector2(BossX + 10f, BossY); // x=32, 범위 밖

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

    // ─── 항목 2: 쿨다운 tick 정확성 ──────────────────────────────────────────────

    [Fact]
    public void Phase1_TelegraphStartsAfterCooldown_SendsEntityStateAttack()
    {
        // BossPhase1CooldownTicks(40) 후 telegraph 시작 → S_EntityState animState=Attack broadcast.
        // ctor에서 AttackCooldownTicks=40으로 초기화. tick 1회당 1씩 감소.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // Setup에서 Tick(1)이 이미 1번 감소 → 39 남음.
        Assert.Equal(CombatConstants.BossPhase1CooldownTicks - 1, boss.AttackCooldownTicks);
        Assert.Equal(0, boss.TelegraphTicksRemaining);

        s.SentPackets.Clear();

        // 잔여 39번 감소 후 0 도달 → telegraph 시작.
        // tick 2~40(39번) 감소 → 0 도달 tick=40에서 telegraph 시작 + S_EntityState broadcast.
        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks; t++)
            _map.Tick(t);

        // tick 41에서 telegraph 시작 → S_EntityState(animState=Attack) broadcast.
        bool hasTelegraphSignal = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Any(p =>
            {
                S_EntityState pkt = new S_EntityState();
                pkt.Read(new ArraySegment<byte>(p));
                return pkt.entityId == BossEntityId && pkt.animState == (byte)AnimState.Attack;
            });

        Assert.True(hasTelegraphSignal,
            "쿨다운 종료 tick에 S_EntityState(entityId=Boss, animState=Attack) broadcast 필요");
        Assert.Equal(CombatConstants.BossTelegraphTicks, boss.TelegraphTicksRemaining);
    }

    [Fact]
    public void Phase1_S_EnemyAttackSentAfterTelegraph()
    {
        // 쿨다운(40) + telegraph(16) 후 S_EnemyAttack broadcast.
        // 범위 안 플레이어가 있어야 broadcast 발생.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        // tick 1 이미 소비(쿨다운 39 남음). 잔여 39 + telegraph 16 = 55틱.
        // tick 2~56(55번) 추가 → 쿨다운 소진 후 telegraph 완료 → S_EnemyAttack.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        int attackCount = CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack);
        Assert.Equal(1, attackCount);

        // S_EnemyAttack 페이로드 검증.
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
        // 페이즈 2 전환 후 쿨다운=24, telegraph=10으로 가속.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // 페이즈 2 강제 전환.
        boss.Hp = 50;
        // AttackCooldownTicks를 페이즈 2 쿨다운으로 직접 설정 (쿨다운 중일 때 전환 clamp 검증).
        boss.AttackCooldownTicks = CombatConstants.BossPhase2CooldownTicks;
        boss.IsPhase2 = true; // 이미 전환된 상태 시뮬

        s.SentPackets.Clear();

        // tick 1 이미 소비 → boss.AttackCooldownTicks를 페이즈 2 값으로 직접 세팅했으므로
        // 세팅 이후 1번 감소는 다음 Tick에서. 쿨다운 24 + telegraph 10 = 34틱.
        // boss.AttackCooldownTicks를 직접 세팅했으므로 tick 2~35(34번)면 됨.
        int totalTicks = CombatConstants.BossPhase2CooldownTicks + CombatConstants.BossPhase2TelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        int attackCount = CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack);
        Assert.Equal(1, attackCount);

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

        // tick 1 소비됨 → 잔여 쿨다운 39 + telegraph 16 = 55틱.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        Assert.True(player.Hp < hpBefore, "범위 안 플레이어 HP가 감소해야 함");
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_EnemyAttack));
    }

    [Fact]
    public void BossAttack_PlayerOutOfRange_NoDamage()
    {
        // 범위 밖 플레이어는 S_EnemyAttack 수신 X, HP 무변화.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceOutsideBossRange(player!);

        int hpBefore = player!.Hp;

        // tick 1 소비됨 → 잔여 55틱.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
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

        // tick 1 소비됨 → 잔여 55틱.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        byte[] attackPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EnemyAttack);
        S_EnemyAttack parsed = new S_EnemyAttack();
        parsed.Read(new ArraySegment<byte>(attackPkt));

        // wire damage == Formula 계산 (drift 방지).
        Assert.Equal(ExpectedBossDamage, parsed.damage);

        // HP = 이전 HP - 데미지 (targetCurrentHp 정합).
        Assert.Equal(hpBefore - ExpectedBossDamage, parsed.targetCurrentHp);
        Assert.Equal(hpBefore - ExpectedBossDamage, player.Hp);
    }

    // ─── 항목 5: 사망→리스폰 ─────────────────────────────────────────────────────

    [Fact]
    public void BossAttack_PlayerDies_Respawns()
    {
        // 플레이어 HP를 보스 1격으로 죽을 만큼 낮게 세팅 → 보스 공격 →
        // Position==PlayerSpawnPosition + Hp==Stats.MaxHp + ActionFsm != DeathState.
        TestGameSession s = SetupSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // HP를 보스 1격 데미지(15) 이하로 세팅 (반드시 사망하도록).
        player!.Hp = ExpectedBossDamage - 1; // 14 → 1격으로 HP <= 0

        // 스폰 위치는 content에서 설정한 (22, 0).
        Vector2 expectedSpawn = new Vector2(22f, 0f);

        // tick 1 소비됨 → 잔여 55틱.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        // 리스폰 검증.
        Assert.Equal(expectedSpawn, player.Position);
        Assert.Equal(player.Stats.MaxHp, player.Hp);
        // Phase 02: IsDeadAnimState 제거됨. Revive()로 ActionFsm이 DeathState 아님을 확인.
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

        player!.Hp = 1; // 최소 HP

        // tick 1 소비됨 → 잔여 55틱.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + CombatConstants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        Assert.Equal(player.Stats.MaxHp, player.Hp);
    }

    // ─── 항목 6: drift 방지 ──────────────────────────────────────────────────────

    [Fact]
    public void BossMaxHp_MatchesEnemyDefaultHpTable()
    {
        // spawn된 boss.MaxHp == 100 (EnemyDefaultHp.ByKind[Boss]) 일치 검증.
        // BossDefault().MaxHp와 GameMap 스폰 HP가 같은지 확인 — drift 방지.
        SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        Assert.Equal(BossMaxHp, boss.MaxHp);
        Assert.Equal(EnemyStats.BossDefault().MaxHp, boss.MaxHp);
    }

    // ─── 항목 8: animState 우선순위 — Attack > Hit (Phase 06 봉합) ─────────────────

    [Fact]
    public void AnimState_DuringTelegraph_HitDoesNotOverrideAttack()
    {
        // telegraph 진입 후 HitLatchTicks 세팅 → broadcast animState는 Attack 유지 (Hit 아님).
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // 쿨다운 소진 → telegraph 시작.
        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks; t++)
            _map.Tick(t);

        Assert.True(boss.TelegraphTicksRemaining > 0, "telegraph 진입 확인");
        Assert.True(boss.AttackLatchTicks > 0, "AttackLatchTicks 세팅 확인");

        // 피격 세팅 — telegraph 중 플레이어가 보스를 때린 상황 시뮬.
        boss.HitLatchTicks = CombatConstants.AnimLatchTicks;

        s.SentPackets.Clear();

        // SnapshotTickInterval 주기 broadcast 틱 진행.
        long nextTick = CombatConstants.BossPhase1CooldownTicks + 1;
        long broadcastTick = nextTick + (Constants.SnapshotTickInterval - (nextTick % Constants.SnapshotTickInterval));
        _map.Tick(broadcastTick);

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
    public void ProtocolVersion_Is9()
    {
        Assert.Equal(9, ProtocolVersion.Current);
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
}
