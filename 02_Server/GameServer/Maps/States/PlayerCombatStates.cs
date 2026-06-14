using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 플레이어 전투 계열 State 3종 (Attack / Hit / Death).
//
// Flyweight 패턴(GPP-06): 필드 없는 정적 인스턴스 재사용 — 틱 루프 new 0 (헌법 #5 정합).
// 엔티티별 지속 카운터(StateTicksRemaining, ExternalImpulseVx, ImpulseDecayPerTick)는 PlayerEntity에 보관.
//
// LocksMovement=true인 State: GameMap.Tick이 inputX=0, rawJump=false로 강제 (서버 권위, 헌법 #1).
// InterruptibleByHit=false인 State(AttackState): 공격 commit window 중 피격으로 끊어지지 않음.
// AcceptsAction=false인 State(Attack/Hit/Death): commit window·hitstun·사망 중 행동 입력 거부 (ActionGate 단일 접점).
internal static class PlayerCombatStates
{
    internal static readonly AttackState Attack = new();
    internal static readonly HitState    Hit    = new();
    internal static readonly DeathState  Death  = new();
}

// ── AttackState ───────────────────────────────────────────────────────────────

// 공격 commit window. 이동 잠금 + 불가침(피격 불가) + 행동 거부.
// EnterAttackState에서 ChangeState 후 StateTicksRemaining + ExternalImpulseVx + ImpulseDecayPerTick 엔티티에 직접 세팅.
// Tick에서 DecayImpulse() 단일 경로로 감쇠 후 카운터 감소 → 0이면 ResolveGrounded로 복귀.
internal sealed class AttackState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Attack;
    public override bool LocksMovement      => true;
    public override bool InterruptibleByHit => false;

    // commit window 중 모든 행동 거부 — Dash 중 평타 구멍 봉합.
    public override bool AcceptsAction(ActionKind kind) => false;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        // 임펄스 감쇠 단일 경로 (HitState.Tick과 동일 헬퍼).
        // 평타=decay 0.75, 대쉬=decay 1.0(등속, Exit가 0으로 정리).
        player.DecayImpulse();

        if (--player.StateTicksRemaining > 0)
            return null;
        return PlayerMovementStates.ResolveGrounded(player);
    }

    public override void Exit(PlayerEntity player)
    {
        player.ExternalImpulseVx   = 0f;
        player.ImpulseDecayPerTick = Constants.KnockbackDecayPerTick;
    }
}

// ── HitState ─────────────────────────────────────────────────────────────────

// 피격 hitstun. 이동 잠금 + 넉백 감쇠 + 행동 거부.
// Enter에서 StateTicksRemaining = AnimLatchTicks 세팅 (ExternalImpulseVx는 EnterHitState에서 먼저 세팅됨).
// Tick에서 DecayImpulse() 단일 경로로 감쇠 후 카운터 감소 → 0이면 ResolveGrounded로 복귀.
// Exit에서 ExternalImpulseVx=0 보장. M4.11 P2 force-adopt 계약 보존(ε/decay 거동 동일).
internal sealed class HitState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Hit;
    public override bool LocksMovement      => true;
    public override bool InterruptibleByHit => true;
    public override bool AcceptsAction(ActionKind kind) => false;

    public override void Enter(PlayerEntity player)
    {
        player.StateTicksRemaining = CombatConstants.AnimLatchTicks;
    }

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        // 임펄스 감쇠 단일 경로 (AttackState.Tick과 동일 헬퍼).
        // 넉백 decay=0.75, ε=0.05 — M4.11 P2 force-adopt 계약 비트단위 보존.
        player.DecayImpulse();

        if (--player.StateTicksRemaining > 0)
            return null;
        return PlayerMovementStates.ResolveGrounded(player);
    }

    public override void Exit(PlayerEntity player)
    {
        player.ExternalImpulseVx = 0f;
    }
}

// ── DeathState ────────────────────────────────────────────────────────────────

// 사망 terminal state. 행동 전부 거부. Tick → null (전환 없음 — Revive()로만 탈출).
internal sealed class DeathState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Death;
    public override bool AcceptsAction(ActionKind kind) => false;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player) => null;
}
