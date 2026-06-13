using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// 보스 + Stage Clear 회귀 안전망:
/// 보스(`EnemyKind.Boss`) HP 0 → `S_EntityDeath` 1회 + `S_StageClear` 1회 broadcast 후
/// 추가 attack 도달 시 idempotent no-op (HitResult/Death/StageClear 추가 broadcast X).
///
/// **검증 invariant**:
///   - Boss_Death_BroadcastsStageClearOnce — Boss HP 100 + damage 10 × 10 → S_HitResult 10건 +
///     S_EntityDeath 1건 + S_StageClear 1건 (bossEntityId=Boss) + `GameMap.IsStageCleared == true`
///   - BossDuplicateAttack_NoExtraStageClear — Boss 죽인 후 추가 C_Attack 송신 시 HitResult/Death/
///     StageClear 추가 broadcast 0건 (idempotent — 이중 안전망: target Remove + flag).
///   - NormalEnemy_Death_NoStageClear — Normal enemy 죽여도 S_StageClear 안 보냄 (Boss 전용 검증).
///
/// **테스트 전략**:
///   - GameMap 직접 주입 (`GetMap` override) → singleton race 차단
///   - Send override로 broadcast 패킷 캡처 (`SentPackets`) → 회신 byte 검증
///   - `BypassHandshake()` 명시 호출 — handshake 우회 (combat 흐름 isolation)
///   - rate-limit 우회: `player.LastAttackTickMs = 0` 직접 reset (public setter, production 변경 0)
///
/// **entity id 풀 약속** (ctor 박힘):
///   - Normal enemy entityId=1
///   - Boss entityId=2
///   - Player entityId=3 (다음 발급)
/// </summary>
[Collection("ConsoleSerial")]
public class BossStageClearTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    const int NormalEnemyId = 1;
    const int BossEntityId = 2;
    const int PlayerEntityId = 3;

    // Knight가 Boss에 가하는 데미지. Formulas 직접 참조로 drift 방지.
    // BossDefault().Defense=3: Max(1, 10+15-3) = 22.
    static readonly int ExpectedDamageToBoss = Formulas.ComputeDamage(
        PlayerStats.Knight(), EnemyStats.BossDefault(), baseDamage: 10);

    // Knight가 Normal enemy에 가하는 데미지.
    // NormalDefault().Defense=0: Max(1, 10+15-0) = 25.
    static readonly int ExpectedDamageToNormal = Formulas.ComputeDamage(
        PlayerStats.Knight(), EnemyStats.NormalDefault(), baseDamage: 10);

    // 옛 MapSpawnTable 값 보존 — inlined (MapSpawnTable 은퇴, M4.4 Phase 03).
    const float NormalX    = 10f;
    const float NormalY    = 0f;
    const int   NormalMaxHp = 30;
    const float BossX      = 30f;
    const float BossY      = 0f;
    const int   BossMaxHp   = 100;

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        // handshake + class 선택 양쪽 우회 (월드 진입까지 mock).
        public void BypassHandshake()
        {
            CompleteHandshakeAndEnter();   // _handshakeCompleted = true
            SetCharacterClass(0);           // HasSelectedClass = true (Knight)
            EnterGameWorldIfReady();        // → EnterGameWorld() 호출
        }
    }

    public BossStageClearTests()
    {
        // HuntingGround(Normal enemy id=1) + Boss(id=2) content 주입.
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, NormalX, NormalY),
            new EnemySpawnPoint((byte)EnemyKind.Boss,   BossX,   BossY),
        });
        _map = new GameMap(MapId.HuntingGround, content: content);

        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    // --- 헬퍼 (AttackHandlerTests 패턴 정합) ---

    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    TestGameSession SetupHandshakedSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        s.BypassHandshake();
        _map.Tick(1);
        return s;
    }

    // Boss 사거리 안 좌표. Boss는 (30, 0)에 박혀있고 AttackRangeSquared=9 → distance < 3 필요.
    static void PlaceInRangeOfBoss(PlayerEntity player)
        => player.Position = new Vector2(BossX - 1f, BossY);

    // Normal enemy 사거리 안 좌표.
    static void PlaceInRangeOfNormalEnemy(PlayerEntity player)
        => player.Position = new Vector2(NormalX - 1f, NormalY);

    // zero-lag 시뮬 = attackerClientTick을 현재 서버 tick과 동일하게 → diff=0 → rewind 없음.
    static ArraySegment<byte> AttackPacketBytes(int targetEntityId, long attackerClientTick = 0)
    {
        C_Attack pkt = new C_Attack { targetEntityId = targetEntityId, attackerClientTick = (int)attackerClientTick };
        return pkt.Write();
    }

    // --- 회귀 ---

    [Fact]
    public void Boss_Death_BroadcastsStageClearOnce()
    {
        // arrange: handshake + Boss 사거리 안. Boss HP 100 / damage 10 → 10회 공격 필요.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRangeOfBoss(player!);

        // 사전 검증: Boss spawn 박혔는지 + Stage Clear flag false.
        Assert.True(_map.Enemies.ContainsKey(BossEntityId));
        EnemyEntity boss = _map.Enemies[BossEntityId];
        Assert.Equal(EnemyKind.Boss, boss.Kind);
        Assert.Equal(BossMaxHp, boss.Hp);
        Assert.False(_map.IsStageCleared);

        s.SentPackets.Clear();

        // 22 dmg/hit (BossDefault.Defense=3) → 5회 attack으로 Boss HP 100 → 0.
        // attackerClientTick=tick → diff=0 → rewind 없음.
        int hitsNeeded = (int)Math.Ceiling((double)BossMaxHp / ExpectedDamageToBoss); // 5
        long tick = 2;
        for (int i = 0; i < hitsNeeded; i++)
        {
            player!.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
            player!.ActionFsm.ChangeState(PlayerMovementStates.Idle, player!); // AttackState 우회
            s.OnRecvPacket(AttackPacketBytes(BossEntityId, attackerClientTick: tick));
            _map.Tick(tick++);
        }

        // 검증: S_HitResult 5건 + S_EntityDeath 1건 + S_StageClear 1건.
        Assert.Equal(hitsNeeded, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_StageClear));

        // S_StageClear payload — bossEntityId 정합.
        byte[] stageClearPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_StageClear);
        S_StageClear parsedStageClear = new S_StageClear();
        parsedStageClear.Read(new ArraySegment<byte>(stageClearPkt));
        Assert.Equal(BossEntityId, parsedStageClear.bossEntityId);

        // S_EntityDeath payload — entityId=Boss 정합 (lifecycle broadcast 정상).
        byte[] deathPkt = s.SentPackets.First(p => PacketIdOf(p) == PacketID.S_EntityDeath);
        S_EntityDeath parsedDeath = new S_EntityDeath();
        parsedDeath.Read(new ArraySegment<byte>(deathPkt));
        Assert.Equal(BossEntityId, parsedDeath.entityId);

        // broadcast 순서 약속 (PDL.xml 박힘): S_EntityDeath → S_StageClear (lifecycle → game event).
        int deathIdx = s.SentPackets.FindIndex(p => PacketIdOf(p) == PacketID.S_EntityDeath);
        int stageClearIdx = s.SentPackets.FindIndex(p => PacketIdOf(p) == PacketID.S_StageClear);
        Assert.True(deathIdx >= 0);
        Assert.True(stageClearIdx >= 0);
        Assert.True(deathIdx < stageClearIdx,
            $"S_EntityDeath(idx={deathIdx}) must come before S_StageClear(idx={stageClearIdx})");

        // 권위 상태 — Boss는 _enemies에서 제거 + Stage Clear flag true.
        Assert.False(_map.Enemies.ContainsKey(BossEntityId));
        Assert.True(_map.IsStageCleared);

        // Disconnect 호출 X (정상 흐름).
        Assert.Equal(0, s.DisconnectCalls);
    }

    [Fact]
    public void BossDuplicateAttack_NoExtraStageClear()
    {
        // arrange: Boss 죽임 (Boss_Death_BroadcastsStageClearOnce 흐름 재사용).
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRangeOfBoss(player!);
        s.SentPackets.Clear();

        // 22 dmg/hit → 5회 attack으로 Boss 처치. attackerClientTick=tick → diff=0 → rewind 없음.
        int hitsNeeded = (int)Math.Ceiling((double)BossMaxHp / ExpectedDamageToBoss); // 5
        long tick = 2;
        for (int i = 0; i < hitsNeeded; i++)
        {
            player!.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
            player!.ActionFsm.ChangeState(PlayerMovementStates.Idle, player!); // AttackState 우회
            s.OnRecvPacket(AttackPacketBytes(BossEntityId, attackerClientTick: tick));
            _map.Tick(tick++);
        }

        // 사전 검증: Boss 죽었고 broadcast 1회 박힘.
        Assert.False(_map.Enemies.ContainsKey(BossEntityId));
        Assert.True(_map.IsStageCleared);
        int hitsAfterKill = CountPacketsOfType(s.SentPackets, PacketID.S_HitResult);
        int deathsAfterKill = CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath);
        int stageClearsAfterKill = CountPacketsOfType(s.SentPackets, PacketID.S_StageClear);
        Assert.Equal(hitsNeeded, hitsAfterKill);
        Assert.Equal(1, deathsAfterKill);
        Assert.Equal(1, stageClearsAfterKill);

        // act: kill 후 추가 attack — FSM 리셋 + cooldown 우회로 *최대한 통과 시도* → idempotent 검증.
        // Boss가 이미 사망 → step 2(GetEnemyById null) silent drop. rewind 검증엔 도달 X.
        for (int i = 0; i < 3; i++)
        {
            player!.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
            player!.ActionFsm.ChangeState(PlayerMovementStates.Idle, player!); // AttackState 우회
            s.OnRecvPacket(AttackPacketBytes(BossEntityId, attackerClientTick: tick));
            _map.Tick(tick++);
        }

        // 검증: HitResult/Death/StageClear 추가 broadcast 없음 (target lookup step 2 silent drop +
        // _stageCleared flag 이중 안전망).
        Assert.Equal(hitsAfterKill, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(deathsAfterKill, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
        Assert.Equal(stageClearsAfterKill, CountPacketsOfType(s.SentPackets, PacketID.S_StageClear));

        // Stage Clear flag 그대로 true 유지.
        Assert.True(_map.IsStageCleared);
    }

    [Fact]
    public void NormalEnemy_Death_NoStageClear()
    {
        // arrange: handshake + Normal enemy 사거리 안. Normal HP 30 / damage 10 → 3회 공격 필요.
        TestGameSession s = SetupHandshakedSession();
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInRangeOfNormalEnemy(player!);
        s.SentPackets.Clear();

        // 25 dmg/hit (NormalDefault.Defense=0) → 2회 attack으로 Normal HP 30 → 0 이하. attackerClientTick=tick → diff=0 → rewind 없음.
        int hitsNeeded = (int)Math.Ceiling((double)NormalMaxHp / ExpectedDamageToNormal); // 2
        long tick = 2;
        for (int i = 0; i < hitsNeeded; i++)
        {
            player!.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
            player!.ActionFsm.ChangeState(PlayerMovementStates.Idle, player!); // AttackState 우회
            s.OnRecvPacket(AttackPacketBytes(NormalEnemyId, attackerClientTick: tick));
            _map.Tick(tick++);
        }

        // 검증: HitResult 3 + Death 1 + StageClear 0건 (보스 전용 트리거 — Normal엔 미진입).
        Assert.Equal(hitsNeeded, CountPacketsOfType(s.SentPackets, PacketID.S_HitResult));
        Assert.Equal(1, CountPacketsOfType(s.SentPackets, PacketID.S_EntityDeath));
        Assert.Equal(0, CountPacketsOfType(s.SentPackets, PacketID.S_StageClear));

        // 권위 상태: Normal 제거 + Stage Clear flag *그대로 false* (Boss 살아있음).
        Assert.False(_map.Enemies.ContainsKey(NormalEnemyId));
        Assert.True(_map.Enemies.ContainsKey(BossEntityId)); // Boss는 살아있음
        Assert.False(_map.IsStageCleared);
    }
}
