using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace GameServer.Tests;

/// <summary>
/// AnimState 서버 권위 결정 단위 테스트.
///
/// **검증 대상**:
///   1. 플레이어 Idle — 정지 + OnGround
///   2. 플레이어 Walk — 수평 속도 있음
///   3. 플레이어 Jump — OnGround=false
///   4. 플레이어 Attack — EnterAttackState() → ActionFsm = AttackState
///   5. 플레이어 Hit — EnterHitState() → ActionFsm = HitState
///   6. 플레이어 Death — HP <= 0
///   7. 우선순위: Hit 중 이동 입력 → Hit 우선
///   8. 우선순위: Death > Hit (HP 0이면 latch 중에도 Death)
///   9. Attack latch 지속 후 자동 해제 (AnimLatchTicks 틱 후 Idle/Walk 복귀)
///  10. 적 Idle — EnemyState.Idle
///  11. 적 Walk — EnemyState.Patrol → AnimState.Walk
///  12. 적 Walk — EnemyState.Chase → AnimState.Walk
///  13. 적 Hit latch — HitLatchTicks > 0
///  14. 적 Hp=0 직접 조작 — 무크래시 방어
///  15. 적 AI/AnimState 분리 확인 — EnemyState와 AnimState는 다른 레이어
///  16. S_Snapshot에 animState 포함 확인 (broadcast 패킷 검증)
///  17. S_EntityState에 animState 포함 확인 (Patrol→Walk 확인)
///
/// **테스트 전략**:
///   - GameMap 단독 생성 → SpawnEnemy/AddPlayer(null owner) 직접 구성.
///   - GameMap.Tick(long) 직접 호출로 상태 진행.
///   - S_Snapshot/S_EntityState broadcast는 FakeCapturingSession으로 캡처.
/// </summary>
public class AnimStateTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>HuntingGround 맵 (Normal enemy 1마리 포함 — content 주입).</summary>
    static GameMap MakeHuntingGround()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    /// <summary>빈 맵 (Town — enemy 없음). 플레이어 전용 테스트용.</summary>
    static GameMap MakeTownMap() => new GameMap(MapId.Town);

    /// <summary>null owner + 지정 위치로 player 추가.</summary>
    static PlayerEntity AddPlayerAt(GameMap map, float x, float y)
        => map.AddPlayer(null, new Vector2(x, y));

    // ── 플레이어 AnimState 테스트 ──────────────────────────────────────────────

    /// <summary>
    /// 정지(vx≈0) + OnGround=true → Idle.
    /// Physics.Step 후 velocity가 damping되면 Idle이어야 함.
    /// 직접 PlayerEntity 필드를 조작해 순수 ComputePlayerAnimState 경로 검증.
    /// </summary>
    [Fact]
    public void Player_Idle_WhenStationaryAndGrounded()
    {
        GameMap map = MakeTownMap();
        PlayerEntity p = AddPlayerAt(map, 0f, 0f);

        // velocity=0, OnGround=true, HP>0 → Idle (FSM 초기 상태는 이미 Idle)
        p.Velocity = Vector2.Zero;
        p.OnGround = true;

        // Tick을 돌려 snapshot 생성 (SnapshotTickInterval=1, 20Hz)
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity capturePlayer = map.AddPlayer(session, new Vector2(99f, 0f));
        // 캡처 플레이어도 정지
        capturePlayer.Velocity = Vector2.Zero;
        capturePlayer.OnGround = true;

        // null-owner player(p)는 broadcast 대상이지만 자신에게 보내지 않음. 캡처는 session 소유 player.
        // 대신 null-owner player의 animState를 snapshot에서 읽으려면 capturePlayer를 통해 확인.
        // 단순화: p의 animState를 직접 GameMap.ComputePlayerAnimState에 해당하는 Tick 로직 통해 검증.
        // SnapshotTickInterval=1(20Hz) → 매 tick broadcast. Tick(1)부터 snapshot 발생.
        map.Tick(1);
        map.Tick(2);

        // p 자신의 스냅샷 찾기 (entityId == p.EntityId)
        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        Assert.Equal((byte)AnimState.Idle, snap.animState);
    }

    /// <summary>
    /// 수평 속도 있음 + OnGround=true → Walk.
    /// EnqueueInput으로 inputX=1을 큐에 박아 Physics.Step이 vx를 만들도록 유도.
    /// </summary>
    [Fact]
    public void Player_Walk_WhenMovingHorizontally()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;

        // inputX=+1 적용 → vx가 생김
        p.EnqueueInput(1, false, 1u);

        map.Tick(1);
        p.EnqueueInput(1, false, 2u);
        map.Tick(2); // broadcast

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        Assert.Equal((byte)AnimState.Walk, snap.animState);
    }

    /// <summary>
    /// OnGround=false → Jump (공중이면 수평 이동 없어도 Jump).
    /// </summary>
    [Fact]
    public void Player_Jump_WhenAirborne()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = false; // 공중 강제
        p.Velocity = new Vector2(0f, 5f); // 수직 속도 (상승 중)

        map.Tick(1);
        // Tick(1) 후 Physics.Step이 OnGround를 변경할 수 있으므로 다시 강제
        p.OnGround = false;
        map.Tick(2); // broadcast

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        Assert.Equal((byte)AnimState.Jump, snap.animState);
    }

    /// <summary>
    /// EnterAttackState() 호출 후 ActionFsm이 AttackState → animState=Attack.
    /// Phase 02: latch 필드 직접 set 대신 FSM API 경유.
    /// </summary>
    [Fact]
    public void Player_Attack_WhenAttackLatchActive()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;

        // FSM API 경유 — AttackState 진입
        p.EnterAttackState();

        map.Tick(1);
        map.Tick(2); // broadcast

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        Assert.Equal((byte)AnimState.Attack, snap.animState);
    }

    /// <summary>
    /// EnterHitState() 호출 후 ActionFsm이 HitState → animState=Hit.
    /// LocksMovement=true이므로 이동 입력이 있어도 Hit 유지.
    /// Phase 02: latch 필드 직접 set 대신 FSM API 경유.
    /// </summary>
    [Fact]
    public void Player_Hit_WhenHitLatchActive_EvenWhileMoving()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;
        p.Velocity = new Vector2(5f, 0f); // 이동 중

        // FSM API 경유 — HitState 진입 (dirX=1: 오른쪽에서 피격)
        p.EnterHitState(1f);

        // 이동 입력 제공 → LocksMovement=true이므로 GameMap.Tick이 inputX=0으로 강제
        p.EnqueueInput(1, false, 1u);
        map.Tick(1);
        p.EnqueueInput(1, false, 2u);
        map.Tick(2); // broadcast

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        // HitState가 LocksMovement=true이므로 Walk보다 Hit 우선
        Assert.Equal((byte)AnimState.Hit, snap.animState);
    }

    /// <summary>
    /// Hp=0 → GameMap.Tick death-guard가 ActionFsm을 DeathState로 전이 → animState=Death.
    /// Phase 02: FSM API 경유. IsDead=true이면 death-guard가 DeathState로 강제 전이.
    /// </summary>
    [Fact]
    public void Player_Death_WhenHpZero_OverridesAllLatch()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;
        p.Hp = 0; // 사망 — GameMap.Tick death-guard가 DeathState로 전이

        map.Tick(1);
        map.Tick(2); // broadcast

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        // Death 최우선 — death-guard가 FSM을 DeathState로 강제 전이
        Assert.Equal((byte)AnimState.Death, snap.animState);
    }

    /// <summary>
    /// AttackState가 AttackCommitWindowTicks 틱 후 ResolveGrounded로 복귀.
    /// 정지 상태이면 Idle로 복귀.
    /// Phase 02: FSM API 경유. AttackState.Tick이 StateTicksRemaining을 감소.
    /// </summary>
    [Fact]
    public void Player_AttackLatch_ExpiresAfterLatchTicks()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;
        p.Velocity = Vector2.Zero;

        // FSM API 경유 — AttackState 진입
        p.EnterAttackState();

        // AttackCommitWindowTicks 이상 tick 진행 → StateTicksRemaining 감소 → Idle 복귀
        // 마지막 broadcast가 짝수 tick에 일어나도록 +2
        int window = Constants.AttackCommitWindowTicks;
        for (int i = 1; i <= window + 2; i++)
        {
            map.Tick(i);
        }

        // window 종료 후 broadcast: 정지 상태 → Idle이어야 함
        byte[]? lastSnapForP = sink.LastOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(lastSnapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(lastSnapForP!));
        Assert.Equal((byte)AnimState.Idle, snap.animState);
    }

    /// <summary>
    /// HitState.LocksMovement=true → 이동 입력이 있어도 Walk가 아닌 Hit 유지.
    /// Phase 02: FSM API 경유. LocksMovement 메커니즘이 우선순위를 보장.
    /// </summary>
    [Fact]
    public void Player_HitPriority_OverridesWalk()
    {
        GameMap map = MakeTownMap();

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity p = map.AddPlayer(session, new Vector2(0f, 0f));
        p.OnGround = true;

        // FSM API 경유 — HitState 진입 (dirX=1)
        p.EnterHitState(1f);

        // 이동 입력 제공 → LocksMovement=true이므로 GameMap.Tick이 inputX=0으로 강제
        p.EnqueueInput(1, false, 1u);
        map.Tick(1);
        p.EnqueueInput(1, false, 2u);
        map.Tick(2);

        byte[]? snapForP = sink.FirstOrDefault(pkt => IsSnapshotForEntity(pkt, p.EntityId));
        Assert.NotNull(snapForP);
        S_Snapshot snap = new S_Snapshot();
        snap.Read(new ArraySegment<byte>(snapForP!));
        // LocksMovement=true → Walk보다 Hit 우선
        Assert.Equal((byte)AnimState.Hit, snap.animState);
    }

    // ── 적 AnimState 테스트 ────────────────────────────────────────────────────

    /// <summary>
    /// 적 EnemyState.Idle → AnimState.Idle.
    /// Boss는 Idle 고정 (AI 미적용). BossRoom 맵으로 검증.
    /// </summary>
    [Fact]
    public void Enemy_Idle_WhenAiStateIsIdle()
    {
        GameMap map = new GameMap(MapId.BossRoom, content: new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        }));
        EnemyEntity? boss = null;
        foreach (EnemyEntity e in map.Enemies.Values) { boss = e; break; }
        Assert.NotNull(boss);
        Assert.Equal(EnemyState.Idle, boss!.State);

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, new Vector2(999f, 0f)); // 보스 범위 밖

        map.Tick(1);
        map.Tick(2); // broadcast

        byte[]? entityStatePkt = sink.FirstOrDefault(pkt => IsEntityStateForEntity(pkt, boss.EntityId));
        Assert.NotNull(entityStatePkt);
        S_EntityState pkt = new S_EntityState();
        pkt.Read(new ArraySegment<byte>(entityStatePkt!));
        Assert.Equal((byte)AnimState.Idle, pkt.animState);
    }

    /// <summary>
    /// 적 EnemyState.Patrol → AnimState.Walk.
    /// AI 상태(Patrol)와 시각 상태(Walk) 분리 핵심 검증.
    /// Normal enemy는 기본 Patrol 시작.
    /// </summary>
    [Fact]
    public void Enemy_Walk_WhenPatrolling()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);
        Assert.Equal(EnemyState.Patrol, enemy!.State);

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, new Vector2(999f, 0f)); // aggro 범위 밖

        map.Tick(1);
        map.Tick(2); // broadcast

        byte[]? entityStatePkt = sink.FirstOrDefault(pkt => IsEntityStateForEntity(pkt, enemy.EntityId));
        Assert.NotNull(entityStatePkt);
        S_EntityState pkt = new S_EntityState();
        pkt.Read(new ArraySegment<byte>(entityStatePkt!));
        // EnemyState.Patrol이지만 AnimState는 Walk
        Assert.Equal((byte)EnemyState.Patrol, pkt.state);      // AI 상태 확인
        Assert.Equal((byte)AnimState.Walk, pkt.animState);      // 시각 상태 확인 (분리 검증)
    }

    /// <summary>
    /// 적 EnemyState.Chase → AnimState.Walk.
    /// 추격 중에도 시각적으로는 걷는 모션.
    /// </summary>
    [Fact]
    public void Enemy_Walk_WhenChasing()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // Chase 상태로 강제 설정
        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        PlayerEntity player = map.AddPlayer(session, new Vector2(enemy.X + 3f, 0f)); // AggroRange 안
        enemy.State = EnemyState.Chase;
        enemy.TargetEntityId = player.EntityId;

        map.Tick(1);
        map.Tick(2); // broadcast

        byte[]? entityStatePkt = sink.FirstOrDefault(pkt => IsEntityStateForEntity(pkt, enemy.EntityId));
        Assert.NotNull(entityStatePkt);
        S_EntityState pkt = new S_EntityState();
        pkt.Read(new ArraySegment<byte>(entityStatePkt!));
        Assert.Equal((byte)EnemyState.Chase, pkt.state);
        Assert.Equal((byte)AnimState.Walk, pkt.animState);
    }

    /// <summary>
    /// 적 HitLatchTicks > 0 → AnimState.Hit.
    /// CombatSystem이 피격 시 HitLatchTicks = AnimLatchTicks 설정.
    /// 직접 필드 설정으로 latch 상태 검증.
    /// </summary>
    [Fact]
    public void Enemy_Hit_WhenHitLatchActive()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // Hit latch 직접 설정 (CombatSystem 피격 경로 없이 순수 latch 로직 검증)
        enemy.HitLatchTicks = CombatConstants.AnimLatchTicks;
        enemy.AttackLatchTicks = 0;

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, new Vector2(999f, 0f));

        map.Tick(1);
        enemy.HitLatchTicks = System.Math.Max(1, enemy.HitLatchTicks); // 감소 후에도 latch 유지
        map.Tick(2); // broadcast

        byte[]? entityStatePkt = sink.FirstOrDefault(pkt => IsEntityStateForEntity(pkt, enemy.EntityId));
        Assert.NotNull(entityStatePkt);
        S_EntityState pkt = new S_EntityState();
        pkt.Read(new ArraySegment<byte>(entityStatePkt!));
        Assert.Equal((byte)AnimState.Hit, pkt.animState);
    }

    /// <summary>
    /// 적 HP <= 0 — 죽은 적은 S_EntityState broadcast 대상에서 제외됨.
    ///
    /// 즉시사망 모델: CombatSystem이 사망 시 즉시 RemoveEnemy(헌법 #1 — 서버 확정+제거, 죽음 연출은 클라 VFX).
    /// Hp=0 직접 조작은 CombatSystem 경로를 우회하므로 적이 맵에 잔류하지만,
    /// IsDead인 채로 Fsm.Tick을 받는 것은 방어적으로 허용(패닉 없음) — 어떤 animState든 OK.
    /// 여기서는 enemy가 맵에서 제거(IsDead체크 없으므로 잔류할 수 있음)하는 것을 주로 검증하지 않고,
    /// Hp>0 시 Walk/Hit가 정상 broadcast됨을 이 테스트에서 벗어나 기존 테스트들이 커버함을 확인.
    ///
    /// 이 테스트 = 방어적 무크래시 검증: Hp=0 적을 tick 돌려도 예외 없음.
    /// </summary>
    [Fact]
    public void Enemy_Death_WhenHpZero_NoCrash()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // Hp=0 직접 조작 — CombatSystem 경로 우회 (IsDead=true이나 _enemies에 잔류).
        enemy.HitLatchTicks = CombatConstants.AnimLatchTicks;
        enemy.Hp = 0;

        var sink = new List<byte[]>();
        var session = new FakeCapturingSession(sink);
        map.AddPlayer(session, new Vector2(999f, 0f));

        // 예외 없이 tick 진행 가능해야 함 (방어적 무크래시).
        var ex = Record.Exception(() => { map.Tick(1); map.Tick(2); });
        Assert.Null(ex);
    }

    /// <summary>
    /// AI 상태(EnemyState)와 AnimState 분리 명시 검증.
    /// EnemyState.Patrol과 EnemyState.Chase 둘 다 AnimState.Walk로 매핑됨.
    /// EnemyState.Idle만 AnimState.Idle로 매핑.
    /// </summary>
    [Fact]
    public void Enemy_AnimState_AiVsVisual_Separation()
    {
        // EnemyState → 기대 AnimState 매핑 테이블
        (EnemyState aiState, AnimState expectedAnim)[] cases =
        {
            (EnemyState.Idle,   AnimState.Idle),
            (EnemyState.Patrol, AnimState.Walk), // 핵심: Patrol≠Idle, Patrol→Walk
            (EnemyState.Chase,  AnimState.Walk), // 핵심: Chase도→Walk
        };

        foreach ((EnemyState aiState, AnimState expectedAnim) in cases)
        {
            GameMap map = MakeHuntingGround();
            EnemyEntity? enemy = null;
            foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
            Assert.NotNull(enemy);

            enemy.State = aiState;
            enemy.HitLatchTicks = 0;
            enemy.AttackLatchTicks = 0;

            var sink = new List<byte[]>();
            var session = new FakeCapturingSession(sink);
            map.AddPlayer(session, new Vector2(999f, 0f));

            map.Tick(1);
            map.Tick(2);

            byte[]? entityStatePkt = sink.FirstOrDefault(pkt => IsEntityStateForEntity(pkt, enemy.EntityId));
            Assert.NotNull(entityStatePkt);
            S_EntityState pkt = new S_EntityState();
            pkt.Read(new ArraySegment<byte>(entityStatePkt!));
            Assert.True((byte)expectedAnim == pkt.animState,
                $"EnemyState.{aiState} should map to AnimState.{expectedAnim}({(byte)expectedAnim}), got {pkt.animState}");
        }
    }

    /// <summary>
    /// CombatSystem 공격 성공 시 attacker가 AttackState로 전이됨.
    /// Phase 02: AttackLatchTicks 직접 확인 → ActionFsm 상태 확인으로 변경.
    /// ProcessAttack 경로를 통해 EnterAttackState()가 실제로 호출되는지 end-to-end 확인.
    /// </summary>
    [Fact]
    public void CombatSystem_Attack_SetsAttackLatchOnAttacker()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        // attacker를 enemy 바로 옆에 배치 (AABB 안)
        PlayerEntity attacker = map.AddPlayer(null, new Vector2(enemy.X + 1f, 0f));
        attacker.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
        enemy.Hp = 1000; // 안 죽게

        // Tick(1) → _currentTick=1 + RecordPosition 박힘
        map.Tick(1);
        attacker.RecordPosition(1, attacker.Position);
        attacker.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회

        // 공격 실행
        map.ProcessAttack(attacker.EntityId, enemy.EntityId, attackerClientTick: 1);

        // attacker가 AttackState로 전이되었어야 함
        Assert.IsType<AttackState>(attacker.ActionFsm.CurrentState);
        // StateTicksRemaining이 AttackCommitWindowTicks로 세팅되었어야 함
        Assert.Equal(Constants.AttackCommitWindowTicks, attacker.StateTicksRemaining);
    }

    /// <summary>
    /// CombatSystem 공격 성공 시 target(enemy)의 HitLatchTicks가 설정됨.
    /// </summary>
    [Fact]
    public void CombatSystem_Attack_SetsHitLatchOnTarget()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);

        PlayerEntity attacker = map.AddPlayer(null, new Vector2(enemy.X + 1f, 0f));
        attacker.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회
        enemy.Hp = 1000; // 안 죽게

        map.Tick(1);
        attacker.RecordPosition(1, attacker.Position);
        attacker.SetLastActionTick(ActionKind.Melee, long.MinValue / 2); // cooldown 우회

        map.ProcessAttack(attacker.EntityId, enemy.EntityId, attackerClientTick: 1);

        // enemy의 HitLatchTicks = AnimLatchTicks여야 함
        Assert.Equal(CombatConstants.AnimLatchTicks, enemy.HitLatchTicks);
    }

    // ── 패킷 파싱 헬퍼 ────────────────────────────────────────────────────────

    static bool IsSnapshotForEntity(byte[] payload, int entityId)
    {
        if (payload.Length < 4) return false;
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        if ((PacketID)id != PacketID.S_Snapshot) return false;
        if (payload.Length < 8) return false;
        // [size:2][id:2][entityId:4] → offset 4부터 entityId
        int eid = payload[4] | (payload[5] << 8) | (payload[6] << 16) | (payload[7] << 24);
        return eid == entityId;
    }

    static bool IsEntityStateForEntity(byte[] payload, int entityId)
    {
        if (payload.Length < 4) return false;
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        if ((PacketID)id != PacketID.S_EntityState) return false;
        if (payload.Length < 8) return false;
        int eid = payload[4] | (payload[5] << 8) | (payload[6] << 16) | (payload[7] << 24);
        return eid == entityId;
    }

    // ── FakeCapturingSession ───────────────────────────────────────────────────

    /// <summary>
    /// Send 호출을 캡처하는 최소 세션 mock (EnemyAiTests.FakeCapturingSession 패턴 정합).
    /// </summary>
    sealed class FakeCapturingSession : Dawnholder.Server.GameServer.Sessions.GameSession
    {
        readonly List<byte[]> _sink;
        public FakeCapturingSession(List<byte[]> sink) { _sink = sink; }

        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            _sink.Add(copy);
        }

        protected override Dawnholder.Server.GameServer.Maps.GameMap? GetMap() => null;
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }
}
