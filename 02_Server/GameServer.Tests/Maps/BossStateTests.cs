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
/// BossStates (Idle / Move / Telegraph / Attack) State 머신 단위 테스트.
///
/// **검증 시나리오**:
///   1. Idle dwell 소진 → Move 전환 (blind-timer 폐기, 탐지 구동).
///   2. Move 사거리 도달 → Telegraph broadcast.
///   3. Telegraph → Attack → Idle: telegraph 소진 후 S_EnemyAttack + Idle 복귀.
///   4. Attack Enter → AttackCooldownTicks 리셋 검증.
///   5. EnterHitState 가드: 보스에 EnterHitState 호출 시 FSM이 BossStates 유지.
///
/// **픽스처**: BossBehaviorTests 패턴 재사용 (GameMap 직접 주입 / MapId.BossRoom / Send override).
/// </summary>
[Collection("ConsoleSerial")]
public class BossStateTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    const int BossEntityId = 1;
    const int PlayerEntityId = 2;
    const float BossX = 22f;
    const float BossY = 0f;
    const int BossMaxHp = 100;

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

    public BossStateTests()
    {
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

    TestGameSession SetupSession()
    {
        TestGameSession s = new(_map);
        s.OnConnected(new IPEndPoint(System.Net.IPAddress.Loopback, 0));
        s.BypassHandshake();
        _map.Tick(1);
        return s;
    }

    static void PlaceInBossRange(PlayerEntity player)
        => player.Position = new Vector2(BossX + 1f, BossY);

    // ─── 시나리오 1: Idle dwell 소진 → Move 전환 ──────────────────────────────

    [Fact]
    public void BossIdle_DwellEnds_TransitionsToMove()
    {
        // AttackCooldownTicks 소진 후 Fsm이 BossMoveState로 전환됨.
        // (Telegraph가 아님 — 새 모델은 Idle→Move→{Telegraph|Idle} 사이클.)
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // SetupSession Tick(1)이 이미 1회 감소 → 39 남음.
        Assert.Equal(CombatConstants.BossPhase1CooldownTicks - 1, boss.AttackCooldownTicks);

        // Idle.Tick 로직: cooldown>0이면 감소 후 null, cooldown==0이면 Move 반환.
        // 39→0까지 39틱(t=2..40) + cooldown==0 판정 1틱(t=41) = 총 40틱 추가 필요.
        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks + 1; t++)
            _map.Tick(t);

        // Move 전환 확인 (Telegraph 아님).
        Assert.IsType<BossMoveState>(boss.Fsm!.CurrentState);
    }

    // ─── 시나리오 2: Move 사거리 도달 → Telegraph broadcast ───────────────────

    [Fact]
    public void BossMove_InRange_BeginsTelegraphBroadcast()
    {
        // player를 trigger 사거리에 두고 Move 진입 후 사거리 도달 틱에 S_EntityState animState=Attack.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        // 충분 틱(80) 돌려 S_EntityState animState=Attack broadcast 검증.
        // 80틱 이후엔 이미 telegraph가 완료돼 Idle 복귀 상태일 수 있으므로
        // broadcast 패킷 히스토리에서 검증 (틱 진행 후 latch 확인 X).
        for (long t = 2; t <= 80; t++)
            _map.Tick(t);

        bool hasTelegraphPkt = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Any(p =>
            {
                S_EntityState pkt = new();
                pkt.Read(new ArraySegment<byte>(p));
                return pkt.entityId == BossEntityId && pkt.animState == (byte)AnimState.Attack;
            });

        Assert.True(hasTelegraphPkt, "Move 사거리 도달 틱에 S_EntityState(animState=Attack) broadcast 필요");
    }

    // ─── 시나리오 3: Telegraph → Attack → Idle 전환 ──────────────────────────

    [Fact]
    public void BossTelegraph_Expires_AppliesAttackThenReturnsToIdle()
    {
        // player in range + 충분 틱 → S_EnemyAttack 1회 + 어느 시점 Idle 복귀.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        // 충분 틱(90) 돌려 Attack 1회 + Idle 복귀 확인.
        bool sawAttack = false;
        bool sawIdleAfterAttack = false;
        for (long t = 2; t <= 90; t++)
        {
            _map.Tick(t);
            if (!sawAttack && s.SentPackets.Any(p => PacketIdOf(p) == PacketID.S_EnemyAttack))
                sawAttack = true;
            if (sawAttack && boss.Fsm!.CurrentState is BossIdleState)
                sawIdleAfterAttack = true;
        }

        Assert.True(sawAttack, "S_EnemyAttack 1회 이상 필요");
        Assert.True(sawIdleAfterAttack, "Attack 후 BossIdleState 복귀 필요");
    }

    // ─── 시나리오 4: Attack Enter → AttackCooldownTicks 리셋 ──────────────────

    [Fact]
    public void BossAttack_Enter_ResetsCooldown()
    {
        // Attack 상태로 직접 전환 후 AttackCooldownTicks == BossPhase1CooldownTicks 확인.
        // (BossAttackState.Enter가 ApplyBossAttack + 쿨다운 리셋을 수행.)
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // FSM을 직접 Attack으로 전환 (map에 player 있어도 ApplyBossAttack은 player 스캔하므로 무방).
        boss.Fsm!.ChangeState(BossStates.Attack, boss);

        Assert.Equal(CombatConstants.BossPhase1CooldownTicks, boss.AttackCooldownTicks);
    }

    // ─── 시나리오 5: EnterHitState 가드 ──────────────────────────────────────

    [Fact]
    public void BossEnterHitState_DoesNotTransitionFsmToEnemyHit()
    {
        // 보스에 EnterHitState 호출 시 FSM이 BossStates에 머뭄.
        // Kind==Boss 가드: EnemyStates.Hit로 전환하면 보스 AI가 완전히 멈추는 회귀 발생.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        // Idle 상태에서 피격.
        Assert.IsType<BossIdleState>(boss.Fsm!.CurrentState);
        boss.EnterHitState(1f);

        // FSM은 여전히 BossIdleState.
        Assert.IsType<BossIdleState>(boss.Fsm!.CurrentState);
        Assert.Equal(CombatConstants.AnimLatchTicks, boss.HitLatchTicks);

        // Telegraph 상태에서도 동일하게 가드 작동.
        // player를 trigger 사거리에 두고 Telegraph 진입까지 틱 진행.
        for (long t = 2; t <= 80; t++)
        {
            _map.Tick(t);
            if (boss.Fsm!.CurrentState is BossTelegraphState) break;
        }

        if (boss.Fsm!.CurrentState is BossTelegraphState)
        {
            boss.EnterHitState(-1f);
            Assert.IsType<BossTelegraphState>(boss.Fsm!.CurrentState);
        }
    }
}
