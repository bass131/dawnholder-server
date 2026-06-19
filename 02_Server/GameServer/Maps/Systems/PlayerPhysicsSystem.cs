using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.States;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.Systems;

/// <summary>
/// §2.2 PlayerPhysicsSystem — GameMap(컨테이너)에서 플레이어 물리 적분 스텝 추출 (M7.7 P4b).
///
/// **단일 책임**: 매 틱 각 플레이어의 입력 1개 소비 → Physics.Step → kill-plane → facing/ack →
///   RecordPosition → ActionFsm.Tick. *행동 불변(behavior-invariant)* — 추출 전 GameMap.Tick 인라인 블록과
///   연산·순서·상태변경 byte-exact 동일.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (Tick 1순위, job 처리 직후).
/// **헌법 #1**: 서버 권위 이동. 클라 prediction은 서버 snapshot으로 reconcile.
/// **헌법 #5**: await / Task.Delay / Thread.Sleep / lock 없음 — 순수 동기. 신규 할당은 추출 전과 동일 패턴.
///
/// **틱당 정확히 Physics.Step 1회 불변식**: 물리 시간 = 벽시계 시간 (50ms/tick).
///   큐에 N개 쌓여도 이 틱에는 1개만 소비 — 멀티 드레인 금지.
///   큐 비면(starvation) neutral (0, false) 적용 — 세계는 계속 흐름(중력/마찰).
/// </summary>
internal sealed class PlayerPhysicsSystem
{
    internal void Step(GameMap map, long tickNumber)
    {
        MapTerrain? terrain = map.Terrain;

        foreach (PlayerEntity p in map.Players)
        {
            // death-guard: IsDead인데 DeathState가 아니면 즉시 전이.
            // BossBehaviorSystem.ApplyBossAttack이 Hp를 0으로 내린 직후에도 안전하게 잡힘.
            if (p.IsDead && p.ActionFsm.CurrentState is not DeathState)
                p.ActionFsm.ChangeState(PlayerCombatStates.Death, p);

            bool hasInput = p.TryDequeueInput(out PlayerEntity.InputCommand cmd);
            sbyte inputX = hasInput ? cmd.InputX : (sbyte)0;
            bool rawJump = hasInput && cmd.JumpPressed;

            // movement-gate: LocksMovement=true인 State(Attack/Hit/Death)면 이동 입력 무효.
            bool locked = p.ActionFsm.CurrentState.LocksMovement;
            if (locked)
            {
                inputX = 0;
                rawJump = false;
            }

            bool jumpPressed = p.ResolveJump(rawJump); // jump buffer: 공중 입력 → 착지 틱 발사

            // ExternalImpulseVx: 대쉬/lunge(AttackState) + 넉백(HitState) 통합 단일 필드.
            //   두 State는 상호배타라 항상 하나만 활성. 0이면 기존 이동과 동일.
            PhysicsInput input = new PhysicsInput(inputX, jumpPressed, Constants.TickDuration, p.ExternalImpulseVx);
            PhysicsState before = new PhysicsState(p.Position, p.Velocity, p.OnGround);
            MoveParams move = new MoveParams(p.Stats.MoveSpeed, p.Stats.JumpVel);
            PhysicsState after = Physics.Step(before, input, terrain, move);
            p.Position = after.Position;
            p.Velocity = after.Velocity;
            p.OnGround = after.OnGround;

            // kill-plane: 낙하로 맵 밖 벗어나면 PlayerSpawn 재배치. HP 무변화 (낙사 데미지 M4.5 이월).
            // terrain null이면 체크 skip (평지 맵은 낙사 없음).
            if (terrain != null && p.Position.Y < terrain.KillPlaneY)
            {
                Vector2 spawn = map.PlayerSpawnPosition;
                p.Position = spawn;
                p.Velocity = Vector2.Zero;
                p.OnGround = false;
            }

            // 이동 방향 갱신 — inputX가 0이 아닐 때만. 0이면 마지막 방향 유지.
            // FacingDir은 S_PlayerAttack.facing 직렬화에 사용 (공격 연출 방향 결정).
            if (inputX != 0)
                p.FacingDir = inputX > 0 ? (sbyte)1 : (sbyte)-1;

            // ack = 적용 시점 clientTick. 빈 틱(starvation)은 불변 — 클라 reconcile 정합.
            if (hasInput)
                p.LastClientTick = cmd.ClientTick;

            // Physics.Step 완료 후 위치 기록 — "그 tick에 실제로 있던 위치".
            p.RecordPosition(tickNumber, p.Position);

            // ActionFsm Tick: 전투 State(Attack/Hit)의 카운터 감소 + 이동 State 전환 판정을 통합 처리.
            // Attack/HitState는 내부에서 StateTicksRemaining을 감소시키고 0이면 ResolveGrounded 반환.
            // 이동 State(Idle/Move/Jump)는 물리 상태(OnGround/Velocity)를 보고 전환.
            p.ActionFsm.Tick(p);
        }
    }
}
