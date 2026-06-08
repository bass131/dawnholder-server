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
/// BossStates (Idle / Telegraph / Attack) State 머신 단위 테스트.
///
/// **검증 시나리오**:
///   1. Idle → Telegraph 전환: 쿨다운 0 도달 틱에 TelegraphTicksRemaining 세팅 + State 전환.
///   2. Telegraph → Attack → Idle: telegraph 0 도달 틱에 S_EnemyAttack + Attack 전환, 다음 틱 Idle 복귀.
///   3. off-by-one 쿨다운: Attack 전환 틱은 리셋값 유지, 다음 틱 AttackState.Tick 첫 감소(Phase1-1).
///   4. EnterHitState 가드: 보스에 EnterHitState 호출 시 FSM이 BossStates 유지 (EnemyStates.Hit 전환 금지).
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

    // ─── 시나리오 1: Idle → Telegraph 전환 ─────────────────────────────────────

    [Fact]
    public void BossIdle_CooldownReaches0_TransitionsToTelegraph()
    {
        // 쿨다운 0 도달 틱에 TelegraphTicksRemaining 세팅 + Fsm이 BossTelegraphState.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // SetupSession의 Tick(1)이 이미 1회 감소 → 39 남음.
        Assert.Equal(CombatConstants.BossPhase1CooldownTicks - 1, boss.AttackCooldownTicks);

        // 잔여 39틱 감소 → 0 도달.
        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks; t++)
            _map.Tick(t);

        Assert.Equal(Constants.BossTelegraphTicks, boss.TelegraphTicksRemaining);
        Assert.IsType<BossTelegraphState>(boss.Fsm!.CurrentState);
    }

    [Fact]
    public void BossIdle_CooldownReaches0_BroadcastsTelegraphPacket()
    {
        // 쿨다운 0 도달 틱에 S_EntityState(animState=Attack) broadcast.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        s.SentPackets.Clear();

        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks; t++)
            _map.Tick(t);

        bool hasTelegraphPkt = s.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntityState)
            .Any(p =>
            {
                S_EntityState pkt = new();
                pkt.Read(new ArraySegment<byte>(p));
                return pkt.entityId == BossEntityId && pkt.animState == (byte)AnimState.Attack;
            });

        Assert.True(hasTelegraphPkt, "쿨다운 0 도달 틱에 S_EntityState(animState=Attack) 필요");
    }

    // ─── 시나리오 2: Telegraph → Attack → Idle 전환 + 데미지 판정 ──────────────

    [Fact]
    public void BossTelegraph_Expires_AppliesAttackThenReturnsToIdle()
    {
        // telegraph 0 도달 틱: S_EnemyAttack broadcast + Fsm이 BossAttackState.
        // 그 다음 틱: BossAttackState.Tick → BossIdleState 복귀.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlayerEntity? player = _map.GetPlayer(PlayerEntityId);
        Assert.NotNull(player);
        PlaceInBossRange(player!);

        s.SentPackets.Clear();

        // 쿨다운 39틱 + telegraph 16틱 = 55틱. telegraph 완료 틱 = 56.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + Constants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        // 완료 틱: 공격 1회 + AttackState.
        int attackCount = s.SentPackets.Count(p => PacketIdOf(p) == PacketID.S_EnemyAttack);
        Assert.Equal(1, attackCount);
        Assert.IsType<BossAttackState>(boss.Fsm!.CurrentState);

        // 다음 틱: Idle 복귀.
        _map.Tick(totalTicks + 2);
        Assert.IsType<BossIdleState>(boss.Fsm!.CurrentState);
    }

    // ─── 시나리오 3: off-by-one 쿨다운 정합 ──────────────────────────────────

    [Fact]
    public void BossAttack_OffByOneCooldown_ResetsThenDecrementsNextTick()
    {
        // Attack 전환 틱(=telegraph 완료): AttackState.Enter가 cooldown = Phase1CooldownTicks 리셋.
        //   이 틱엔 감소 X (옛 조건분기: 공격 틱엔 cooldown 감소 안 함).
        // 다음 틱: AttackState.Tick이 첫 감소(Phase1-1) 후 Idle 복귀.
        // off-by-one 보존: AttackState.Tick의 cooldown-- 1회가 없으면 다음 telegraph가 영구 1틱 지연.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];
        PlaceInBossRange(_map.GetPlayer(PlayerEntityId)!);

        // 쿨다운 39틱 + telegraph 16틱 = 55틱. telegraph 완료 틱 = 56.
        int totalTicks = (CombatConstants.BossPhase1CooldownTicks - 1) + Constants.BossTelegraphTicks;
        for (long t = 2; t <= totalTicks + 1; t++)
            _map.Tick(t);

        // 완료(Attack 전환) 틱 직후: 리셋값 그대로 (AttackState.Tick 아직 X).
        Assert.Equal(CombatConstants.BossPhase1CooldownTicks, boss.AttackCooldownTicks);

        // 다음 틱: AttackState.Tick 첫 감소 → Phase1-1.
        _map.Tick(totalTicks + 2);
        Assert.Equal(CombatConstants.BossPhase1CooldownTicks - 1, boss.AttackCooldownTicks);
    }

    // ─── 시나리오 4: EnterHitState 가드 ──────────────────────────────────────

    [Fact]
    public void BossEnterHitState_DoesNotTransitionFsmToEnemyHit()
    {
        // 보스에 EnterHitState 호출 시 FSM이 BossStates(Idle 또는 Telegraph)에 머뭄.
        // Kind==Boss 가드: EnemyStates.Hit로 전환하면 보스 AI가 완전히 멈추는 회귀 발생.
        TestGameSession s = SetupSession();
        EnemyEntity boss = _map.Enemies[BossEntityId];

        // Idle 상태에서 피격.
        Assert.IsType<BossIdleState>(boss.Fsm!.CurrentState);
        boss.EnterHitState(1f);

        // FSM은 여전히 BossIdleState.
        Assert.IsType<BossIdleState>(boss.Fsm!.CurrentState);
        // HitLatchTicks는 세팅됨 (애니 latch만 적용).
        Assert.Equal(CombatConstants.AnimLatchTicks, boss.HitLatchTicks);

        // Telegraph 상태에서도 동일하게 가드 작동.
        for (long t = 2; t <= CombatConstants.BossPhase1CooldownTicks; t++)
            _map.Tick(t);
        Assert.IsType<BossTelegraphState>(boss.Fsm!.CurrentState);

        boss.EnterHitState(-1f);
        Assert.IsType<BossTelegraphState>(boss.Fsm!.CurrentState);
    }
}
