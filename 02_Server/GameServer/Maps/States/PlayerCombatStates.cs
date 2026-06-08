using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 플레이어 전투 계열 State 3종 (Attack / Hit / Death).
//
// Flyweight 패턴(GPP-06): 필드 없는 정적 인스턴스 재사용 — 틱 루프 new 0 (헌법 #5 정합).
// 엔티티별 지속 카운터(StateTicksRemaining, KnockbackVx)는 PlayerEntity에 보관.
//
// LocksMovement=true인 State: GameMap.Tick이 inputX=0, rawJump=false로 강제 (서버 권위, 헌법 #1).
// InterruptibleByHit=false인 State(AttackState): 공격 commit window 중 피격으로 끊어지지 않음.
internal static class PlayerCombatStates
{
    internal static readonly AttackState Attack = new();
    internal static readonly HitState    Hit    = new();
    internal static readonly DeathState  Death  = new();
}

// ── AttackState ───────────────────────────────────────────────────────────────

// 공격 commit window. 이동 잠금 + 불가침(피격 불가).
// Enter에서 StateTicksRemaining = AttackCommitWindowTicks 세팅.
// Tick에서 카운터 감소 → 0이면 ResolveGrounded로 복귀.
internal sealed class AttackState : ActorState
{
    public override AnimState AnimState => AnimState.Attack;
    public override bool LocksMovement     => true;
    public override bool InterruptibleByHit => false;

    public override void Enter(PlayerEntity player)
    {
        player.StateTicksRemaining = Constants.AttackCommitWindowTicks;
    }

    public override ActorState? Tick(PlayerEntity player)
    {
        if (--player.StateTicksRemaining > 0)
            return null;
        return PlayerMovementStates.ResolveGrounded(player);
    }
}

// ── HitState ─────────────────────────────────────────────────────────────────

// 피격 hitstun. 이동 잠금 + 넉백 감쇠.
// Enter에서 StateTicksRemaining = AnimLatchTicks 세팅 (KnockbackVx는 EnterHitState에서 먼저 세팅됨).
// Tick에서 넉백 감쇠 후 카운터 감소 → 0이면 ResolveGrounded로 복귀.
// Exit에서 KnockbackVx=0 보장.
internal sealed class HitState : ActorState
{
    public override AnimState AnimState => AnimState.Hit;
    public override bool LocksMovement     => true;
    public override bool InterruptibleByHit => true;

    public override void Enter(PlayerEntity player)
    {
        player.StateTicksRemaining = CombatConstants.AnimLatchTicks;
    }

    public override ActorState? Tick(PlayerEntity player)
    {
        // 넉백 감쇠: 매 틱 계수를 곱해 지수 감소. 매우 작아지면 0으로 정리.
        player.KnockbackVx *= Constants.KnockbackDecayPerTick;
        if (System.MathF.Abs(player.KnockbackVx) < 0.05f)
            player.KnockbackVx = 0f;

        if (--player.StateTicksRemaining > 0)
            return null;
        return PlayerMovementStates.ResolveGrounded(player);
    }

    public override void Exit(PlayerEntity player)
    {
        player.KnockbackVx = 0f;
    }
}

// ── DeathState ────────────────────────────────────────────────────────────────

// 사망 terminal state. Tick → null (전환 없음 — Revive()로만 탈출).
internal sealed class DeathState : ActorState
{
    public override AnimState AnimState => AnimState.Death;

    public override ActorState? Tick(PlayerEntity player) => null;
}
