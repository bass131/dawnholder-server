using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 이동 계열 State 3종 (Idle / Move / Jump).
//
// Flyweight 패턴(GPP-06): 상태별 전용 필드 없음 → 정적 인스턴스 재사용.
// tick 루프 안에서 new 없음 = 헌법 #5 정합 + GC spike 0.
//
// 전환 조건은 기존 ComputePlayerAnimState 이동 계열 분기와 비트 동일:
//   OnGround=false → Jump / |vx|>VxEpsilon → Move / else → Idle
// 이 값을 바꾸면 행동 불변 보장이 깨짐 — 변경 금지.
internal static class PlayerMovementStates
{
    internal static readonly IdleState  Idle  = new();
    internal static readonly MoveState  Move  = new();
    internal static readonly JumpState  Jump  = new();

    // 기존 ComputePlayerAnimState와 동일한 임계값.
    internal const float VxEpsilon = 0.01f;

    // 착지 후 또는 commit window 종료 후 이동 물리 상태를 보고 다음 State를 결정하는 헬퍼.
    // DRY: JumpState 착지 분기 + AttackState/HitState 종료 분기가 모두 이 헬퍼를 재사용한다.
    // 전환 로직 값은 기존 ComputePlayerAnimState와 완전히 동일 — 절대 변경 금지.
    internal static ActorState<PlayerEntity> ResolveGrounded(PlayerEntity p)
    {
        if (!p.OnGround)
            return Jump;
        if (p.Velocity.X > VxEpsilon || p.Velocity.X < -VxEpsilon)
            return Move;
        return Idle;
    }
}

// ── Idle ─────────────────────────────────────────────────────────────────────

internal sealed class IdleState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Idle;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        if (!player.OnGround)
            return PlayerMovementStates.Jump;
        if (player.Velocity.X > PlayerMovementStates.VxEpsilon ||
            player.Velocity.X < -PlayerMovementStates.VxEpsilon)
            return PlayerMovementStates.Move;
        return null;
    }
}

// ── Move (Walk) ───────────────────────────────────────────────────────────────

internal sealed class MoveState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Walk;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        if (!player.OnGround)
            return PlayerMovementStates.Jump;
        if (player.Velocity.X <= PlayerMovementStates.VxEpsilon &&
            player.Velocity.X >= -PlayerMovementStates.VxEpsilon)
            return PlayerMovementStates.Idle;
        return null;
    }
}

// ── Jump ─────────────────────────────────────────────────────────────────────

internal sealed class JumpState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Jump;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        if (player.OnGround)
            return PlayerMovementStates.ResolveGrounded(player);
        return null;
    }
}
