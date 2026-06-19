using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.Systems;

/// <summary>
/// §2.2 EnemyGravitySystem — GameMap(컨테이너)에서 적 수직 중력 패스 추출 (M7.7 P4b).
///
/// **단일 책임**: 살아 있는 모든 적(Normal/Golem/Boss)에 수직 중력 패스 적용 + kill-plane 낙사 despawn.
///   *행동 불변(behavior-invariant)* — 추출 전 GameMap.ApplyEnemyGravity 인라인과 연산·순서·할당 패턴 동일.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (EnemyAISystem/BossBehaviorSystem이 X를 세팅한 *뒤*).
///   inputX=0으로 Physics.Step을 호출하면 X 변화 없이 Y/Vy/OnGround만 갱신된다 → FSM 세팅 X 보존.
///
/// **terrain==null(평지 맵)**: Physics.Step이 StepFlat으로 위임 — Y&lt;=0 이면 clamp+onGround=true(지면 아래 차단).
/// **moveParams**: inputX=0+jumpPressed=false라 MoveSpeed/JumpVel은 사실상 미사용이지만 Physics.Step 시그니처
///   충족을 위해 실제 적 스탯 기반 값을 전달(추출 전과 동일).
/// **collect-then-remove**: 순회 중 _enemies 수정 금지 → 낙사 대상을 fallen에 모은 뒤 despawn.
/// **헌법 #5**: async/await/Thread.Sleep/lock 없음 — 순수 동기.
/// </summary>
internal sealed class EnemyGravitySystem
{
    internal void Apply(GameMap map, long tickNumber)
    {
        MapTerrain? terrain = map.Terrain;
        PhysicsInput gravityInput = new PhysicsInput((sbyte)0, false, Constants.TickDuration);
        List<EnemyEntity>? fallen = null; // 낙사 대상 — 순회 중 _enemies 수정 금지, collect-then-remove
        foreach (EnemyEntity enemy in map.Enemies.Values)
        {
            MoveParams move = new MoveParams(enemy.Stats.MoveSpeed, 0f);
            PhysicsState before = new PhysicsState(
                new Vector2(enemy.X, enemy.Y),
                new Vector2(0f, enemy.Vy),
                enemy.OnGround);
            PhysicsState after = Physics.Step(before, gravityInput, terrain, move);
            enemy.Y        = after.Position.Y;
            enemy.Vy       = after.Velocity.Y;
            enemy.OnGround = after.OnGround;

            if (terrain != null && enemy.Y < terrain.KillPlaneY)
            {
                fallen ??= new List<EnemyEntity>();
                fallen.Add(enemy);
            }
        }

        if (fallen != null)
        {
            foreach (EnemyEntity enemy in fallen)
                map.DespawnEnemyByFall(enemy);
        }
    }
}
