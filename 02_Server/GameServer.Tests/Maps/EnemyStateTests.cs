using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace GameServer.Tests.Maps;

/// <summary>
/// EnemyStates (PatrolState/ChaseState/EnemyHitState) State 레벨 단위 테스트.
///
/// 검증 대상:
///   1. Aggro_TransitionTick_MovesAsChase_SameTick — #1 함정 회귀 잡이: 전환 틱에 Chase 이동까지 완료.
///   2. DeAggro_TransitionTick_MovesAsPatrol_SameTick — de-aggro 전환 틱에 Patrol 이동까지 완료.
///   3. Patrol_BoundaryReversal — 경계 도달 시 PatrolDir 반전.
///   4. Chase_RetargetsToCloserPlayer — 더 가까운 player가 범위 진입 시 TargetEntityId 교체.
///   5. Hit_PausesAiMovement_AndKnocksBack — HitState 진입 시 AI 이동 멈춤 + 넉백 방향 이동.
///   6. Hit_ResumesChase_WhenPlayerInAggro — stun 소진 후 aggro 안 player 있으면 Chase 복귀.
///   7. Hit_ResumesPatrol_WhenNoPlayer — stun 소진 후 player 없으면 Patrol 복귀.
///   8. Boss_EnterHitState_LatchOnly — Boss는 HitState 전환 없이 latch만 세팅.
/// </summary>
public class EnemyStateTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static GameMap MakeHuntingGround()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Normal, 10f, 0f),
        });
        return new GameMap(MapId.HuntingGround, content: content);
    }

    static GameMap MakeBossRoom()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Boss, 30f, 0f),
        });
        return new GameMap(MapId.BossRoom, content: content);
    }

    static PlayerEntity AddPlayerAt(GameMap map, float x, float y)
    {
        return map.AddPlayer(null, new Vector2(x, y));
    }

    static EnemyEntity GetEnemy(GameMap map)
    {
        EnemyEntity? enemy = null;
        foreach (EnemyEntity e in map.Enemies.Values) { enemy = e; break; }
        Assert.NotNull(enemy);
        return enemy!;
    }

    // ── 1. #1 함정 회귀 잡이: 전환 틱에 Chase 이동까지 완료 (선공 기준) ────────

    /// <summary>
    /// 선공(AggroOnSight=true) enemy — Patrol→Chase 전환이 일어나는 그 틱에 Chase 방향으로 X가 이동해야 함.
    ///
    /// 옛 구조: 전이 블록이 State를 Chase로 바꾼 뒤, 같은 루프 iteration의 movement 블록이
    /// 바뀐 State를 읽어 Chase 이동을 수행. "전환 틱 = Chase 이동 틱".
    ///
    /// 후공(Normal)은 시야 aggro 없으므로 Golem(선공) 기준 테스트.
    /// </summary>
    [Fact]
    public void Aggro_TransitionTick_MovesAsChase_SameTick()
    {
        // Golem = 선공(AggroOnSight=true)
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Golem, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        EnemyEntity enemy = GetEnemy(map);
        Assert.True(enemy.Stats.AggroOnSight, "Golem should be AggroOnSight=true");

        // enemy를 SpawnX에 고정, player를 AggroRange 안 오른쪽에 배치
        enemy.X = enemy.SpawnX; // 10
        float playerX = enemy.X + enemy.Stats.AggroRange * 0.5f; // AggroRange 안쪽
        AddPlayerAt(map, playerX, 0f);

        float beforeX = enemy.X;

        // Tick(1) — Patrol→Chase 전환 + 그 틱 Chase 이동까지
        map.Tick(1);

        // 전환 후 State=Chase
        Assert.Equal(EnemyState.Chase, enemy.State);
        // 같은 틱에 이미 Chase 방향(오른쪽)으로 이동했어야 함 (#1 함정 회귀 잡이)
        Assert.True(enemy.X > beforeX,
            $"전환 틱에 Chase 이동이 없었음. before={beforeX}, after={enemy.X}. " +
            "PatrolState.Tick이 전환 후 MoveChase를 같은 틱에 호출하는지 확인");
    }

    // ── 2. de-aggro 전환 틱에 Patrol 이동까지 완료 ────────────────────────────

    /// <summary>
    /// Chase→Patrol 전환이 일어나는 그 틱에 Patrol 이동(MovePatrol)까지 완료.
    ///
    /// 옛 구조와 동일 — 전환 틱에 이동 없으면 1틱 늦어 trajectory 어긋남.
    /// </summary>
    [Fact]
    public void DeAggro_TransitionTick_MovesAsPatrol_SameTick()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        // de-aggro 임계 밖에 player 배치
        float deAggroThreshold = enemy.Stats.AggroRange * CombatConstants.DeAggroHysteresis;
        float farX = enemy.X + deAggroThreshold + 1f;
        PlayerEntity player = AddPlayerAt(map, farX, 0f);

        // Chase 상태로 강제
        enemy.Fsm!.ChangeState(EnemyStates.Chase, enemy);
        enemy.TargetEntityId = player.EntityId;

        float beforeX = enemy.X;

        map.Tick(1);

        // 전환 후 State=Patrol + Target null
        Assert.Equal(EnemyState.Patrol, enemy.State);
        Assert.Null(enemy.TargetEntityId);
        // 같은 틱에 Patrol 이동(PatrolDir 방향)이 일어났어야 함 — X가 변하거나 경계 clamp
        // PatrolDir=+1(초기값)이므로 오른쪽으로 이동 or 경계로 clamp
        float leftBound  = enemy.SpawnX - enemy.Stats.PatrolRange;
        float rightBound = enemy.SpawnX + enemy.Stats.PatrolRange;
        bool movedOrClamped =
            enemy.X != beforeX ||
            (enemy.X >= leftBound - 0.001f && enemy.X <= rightBound + 0.001f);
        Assert.True(movedOrClamped,
            $"전환 틱에 Patrol 이동이 없었음. before={beforeX}, after={enemy.X}");
    }

    // ── 3. Patrol 경계 반전 ─────────────────────────────────────────────────

    [Fact]
    public void Patrol_BoundaryReversal_Left()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        float leftBound = enemy.SpawnX - enemy.Stats.PatrolRange;
        enemy.X = leftBound + 0.01f;
        enemy.PatrolDir = -1;

        map.Tick(1);

        Assert.Equal(1, enemy.PatrolDir);
        Assert.True(enemy.X >= leftBound - 0.001f,
            $"X={enemy.X} should be >= leftBound={leftBound}");
    }

    [Fact]
    public void Patrol_BoundaryReversal_Right()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        float rightBound = enemy.SpawnX + enemy.Stats.PatrolRange;
        enemy.X = rightBound - 0.01f;
        enemy.PatrolDir = 1;

        map.Tick(1);

        Assert.Equal(-1, enemy.PatrolDir);
        Assert.True(enemy.X <= rightBound + 0.001f,
            $"X={enemy.X} should be <= rightBound={rightBound}");
    }

    // ── 4. 더 가까운 player로 TargetEntityId 교체 ──────────────────────────────

    /// <summary>
    /// Chase 중 현재 target보다 더 가까운 player가 AggroRange 안에 들어오면
    /// TargetEntityId가 그쪽으로 교체됨.
    /// </summary>
    [Fact]
    public void Chase_RetargetsToCloserPlayer()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        // 첫 번째 player — AggroRange 안쪽, 멀리
        float farX = enemy.X + enemy.Stats.AggroRange * 0.8f;
        PlayerEntity farPlayer = AddPlayerAt(map, farX, 0f);

        // Chase 상태로 강제 + 첫 번째 player가 target
        enemy.Fsm!.ChangeState(EnemyStates.Chase, enemy);
        enemy.TargetEntityId = farPlayer.EntityId;

        // 두 번째 player — 더 가까이 (AggroRange 안쪽)
        float closeX = enemy.X + enemy.Stats.AggroRange * 0.3f;
        PlayerEntity closePlayer = AddPlayerAt(map, closeX, 0f);

        map.Tick(1);

        // 더 가까운 두 번째 player로 교체됐어야 함
        Assert.Equal(closePlayer.EntityId, enemy.TargetEntityId);
    }

    // ── 5. HitState 진입 시 AI 이동 멈춤 + 넉백 방향 이동 ─────────────────────

    /// <summary>
    /// Chase 상태 enemy에 EnterHitState(+1f) 호출 시:
    ///   - Fsm.CurrentState가 HitState
    ///   - KnockbackVx > 0 (오른쪽 방향)
    ///   - map.Tick(1) 후 enemy.X가 넉백 방향(+)으로 이동 (Chase 이동이 아님)
    ///
    /// Chase target이 왼쪽이어도 넉백(+)으로 가는 걸로 AI 이동 멈춤 입증.
    /// </summary>
    [Fact]
    public void Hit_PausesAiMovement_AndKnocksBack()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        // target을 enemy 왼쪽에 배치 → Chase라면 X 감소 방향으로 이동
        float targetX = enemy.X - enemy.Stats.AggroRange * 0.5f;
        PlayerEntity player = AddPlayerAt(map, targetX, 0f);

        // Chase 상태 강제
        enemy.Fsm!.ChangeState(EnemyStates.Chase, enemy);
        enemy.TargetEntityId = player.EntityId;

        float beforeX = enemy.X;

        // 오른쪽(+1f) 방향 피격 → KnockbackVx 양수
        enemy.EnterHitState(+1f);

        Assert.IsType<EnemyHitState>(enemy.Fsm.CurrentState);
        Assert.True(enemy.KnockbackVx > 0f, $"KnockbackVx should be positive, got {enemy.KnockbackVx}");

        // AnimLatchTicks = 8 (CombatConstants 참조, internal이므로 직접 사용)
        map.Tick(1);

        // 넉백(+) 방향으로 이동했어야 함 — Chase라면 왼쪽으로 이동했을 것
        Assert.True(enemy.X > beforeX,
            $"HitState여야 +방향 넉백이어야 하는데 X 감소. before={beforeX}, after={enemy.X}");
    }

    // ── 6. stun 소진 후 피격 target 추격 복귀 (후공 포함) ────────────────────

    /// <summary>
    /// player를 aggro 안에 두고 EnterHitState + TargetEntityId 세팅(피격 aggro 시뮬)
    /// → AnimLatchTicks+1회 tick → ChaseState 복귀.
    ///
    /// 후공(Normal, AggroOnSight=false)도 피격으로 세팅된 TargetEntityId가 있으면 Chase 복귀 — ResolveAfterHit 우선 경로.
    /// AnimLatchTicks = 8 (CombatConstants 내부 상수. 값 바뀌면 이 테스트도 갱신 필요 — 의도적 coupling).
    /// </summary>
    [Fact]
    public void Hit_ResumesChase_WhenPlayerInAggro()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        // player를 aggro 안에 배치 (AggroRange*1.5 히스테리시스 안쪽)
        float playerX = enemy.X + enemy.Stats.AggroRange * 0.5f;
        PlayerEntity player = AddPlayerAt(map, playerX, 0f);

        // 피격 aggro 트리거 시뮬: CombatSystem이 TargetEntityId를 공격자로 세팅하는 것과 동일.
        enemy.TargetEntityId = player.EntityId;
        enemy.EnterHitState(+1f);

        // AnimLatchTicks = 8. 9틱 돌리면 HitLatchTicks가 0이 되어 복귀 결정.
        const int AnimLatchTicks = 8; // CombatConstants.AnimLatchTicks
        for (int i = 1; i <= AnimLatchTicks + 1; i++)
            map.Tick(i);

        Assert.IsType<ChaseState>(enemy.Fsm!.CurrentState);
        Assert.Equal(player.EntityId, enemy.TargetEntityId);
    }

    // ── 7. stun 소진 후 player 없으면 Patrol 복귀 ────────────────────────────

    [Fact]
    public void Hit_ResumesPatrol_WhenNoPlayer()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);

        // aggro 밖에 player 배치 (FindClosestInAggro가 null 반환하도록)
        float farX = enemy.X + enemy.Stats.AggroRange + 10f;
        AddPlayerAt(map, farX, 0f);

        enemy.EnterHitState(+1f);

        const int AnimLatchTicks = 8; // CombatConstants.AnimLatchTicks
        for (int i = 1; i <= AnimLatchTicks + 1; i++)
            map.Tick(i);

        Assert.IsType<PatrolState>(enemy.Fsm!.CurrentState);
    }

    // ── 8. Boss는 HitState 전환 없이 latch만 세팅 ────────────────────────────

    /// <summary>
    /// BossRoom boss에 EnterHitState 호출 시:
    ///   - HitLatchTicks > 0 (애니 latch 세팅됨)
    ///   - Fsm이 BossIdleState에 머묾 (EnemyStates.Hit로 전환 금지 — 보스는 BossStates 전용 FSM)
    ///   - 예외 없음
    ///   - boss.State == Idle 유지
    ///   - map.Tick 여러 번에도 정상 동작
    /// </summary>
    [Fact]
    public void Boss_EnterHitState_LatchOnly()
    {
        GameMap map = MakeBossRoom();
        EnemyEntity? boss = null;
        foreach (EnemyEntity e in map.Enemies.Values) { boss = e; break; }
        Assert.NotNull(boss);
        Assert.Equal(EnemyKind.Boss, boss!.Kind);
        Assert.NotNull(boss.Fsm); // Phase 05: 보스에 BossStates FSM 추가됨

        // 예외 없이 호출 가능해야 함
        boss.EnterHitState(+1f);

        Assert.True(boss.HitLatchTicks > 0, "Boss: HitLatchTicks should be set by EnterHitState");
        // Kind==Boss 가드 — EnemyStates.Hit 전환 금지. FSM은 BossIdleState 유지.
        Assert.IsType<Dawnholder.Server.GameServer.Maps.States.BossIdleState>(boss.Fsm!.CurrentState);
        Assert.Equal(EnemyState.Idle, boss.State);

        // 여러 tick 진행해도 정상 (BossBehaviorSystem이 Boss 처리 — EnemyAISystem은 Boss skip)
        for (int i = 1; i <= 5; i++)
            map.Tick(i);

        Assert.Equal(EnemyState.Idle, boss.State);
    }

    // ── 9. 후공 = 시야 aggro 없음 (D-2: Reactive_DoesNotAggroOnSight) ──────────

    /// <summary>
    /// Normal enemy(AggroOnSight=false) — AggroRange 안에 player가 있어도 시야 aggro 없음 → Patrol 유지.
    /// 후공은 피격 트리거만 사용.
    /// </summary>
    [Fact]
    public void Reactive_DoesNotAggroOnSight()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);
        Assert.False(enemy.Stats.AggroOnSight, "NormalDefault should be AggroOnSight=false");

        // AggroRange 안에 player 배치
        float playerX = enemy.X + enemy.Stats.AggroRange * 0.5f;
        AddPlayerAt(map, playerX, 0f);

        map.Tick(1);

        Assert.IsType<PatrolState>(enemy.Fsm!.CurrentState);
        Assert.Equal(EnemyState.Patrol, enemy.State);
    }

    // ── 10. 후공 = 피격 후 Chase (D-2: Reactive_AggrosAfterHit) ───────────────

    /// <summary>
    /// Normal enemy(후공) — EnterHitState + TargetEntityId 직접 세팅(피격 aggro 시뮬)
    /// → stun 소진 후 ChaseState 복귀.
    ///
    /// CombatSystem이 피격 시 target.TargetEntityId = attacker.EntityId로 세팅하는 경로와 동일.
    /// </summary>
    [Fact]
    public void Reactive_AggrosAfterHit()
    {
        GameMap map = MakeHuntingGround();
        EnemyEntity enemy = GetEnemy(map);
        Assert.False(enemy.Stats.AggroOnSight, "NormalDefault should be AggroOnSight=false");

        // attacker player를 AggroRange 안 배치
        float playerX = enemy.X + enemy.Stats.AggroRange * 0.5f;
        PlayerEntity attacker = AddPlayerAt(map, playerX, 0f);

        // 피격 시뮬: CombatSystem이 하는 것처럼 TargetEntityId + EnterHitState
        enemy.TargetEntityId = attacker.EntityId;
        enemy.EnterHitState(+1f);

        // stun 소진
        const int AnimLatchTicks = 8;
        for (int i = 1; i <= AnimLatchTicks + 1; i++)
            map.Tick(i);

        // 피격 aggro → Chase 복귀
        Assert.IsType<ChaseState>(enemy.Fsm!.CurrentState);
        Assert.Equal(attacker.EntityId, enemy.TargetEntityId);
    }

    // ── 11. 선공 = 시야 aggro (D-2: Proactive_AggrosOnSight) ─────────────────

    /// <summary>
    /// Golem enemy(AggroOnSight=true) — AggroRange 안에 player 진입 시 즉시 Chase 전환.
    /// </summary>
    [Fact]
    public void Proactive_AggrosOnSight()
    {
        var content = new MapContent(0f, 0f, new[]
        {
            new EnemySpawnPoint((byte)EnemyKind.Golem, 10f, 0f),
        });
        GameMap map = new GameMap(MapId.HuntingGround, content: content);
        EnemyEntity enemy = GetEnemy(map);
        Assert.True(enemy.Stats.AggroOnSight, "GolemDefault should be AggroOnSight=true");

        // AggroRange 안에 player 배치
        float playerX = enemy.X + enemy.Stats.AggroRange * 0.5f;
        PlayerEntity player = AddPlayerAt(map, playerX, 0f);

        map.Tick(1);

        Assert.IsType<ChaseState>(enemy.Fsm!.CurrentState);
        Assert.Equal(player.EntityId, enemy.TargetEntityId);
    }

}
