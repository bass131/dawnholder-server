using Dawnholder.Server.GameServer.Combat;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.States;

// 플레이어 전투 계열 State 3종 (Attack / Hit / Death).
//
// Flyweight 패턴(GPP-06): 필드 없는 정적 인스턴스 재사용 — 틱 루프 new 0 (헌법 #5 정합).
// 엔티티별 지속 카운터(StateTicksRemaining, KnockbackVx, AttackLungeVx)는 PlayerEntity에 보관.
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
// Enter에서 StateTicksRemaining + AttackLungeVx + LungeDecayPerTick 세팅 (§8 상태 소유).
// Tick에서 카운터 감소 → 0이면 ResolveGrounded로 복귀.
internal sealed class AttackState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Attack;
    public override bool LocksMovement      => true;
    public override bool InterruptibleByHit => false;

    // lungeVx, decayPerTick: EnterAttackState가 계산해 전달 → Enter에서 세팅(§8 상태 소유).
    // Flyweight 정적 인스턴스이므로 tick thread invariant(단일 스레드) 내에서만 유효.
    internal float PendingLungeVx { get; set; }
    internal float PendingDecayPerTick { get; set; } = Constants.KnockbackDecayPerTick;

    // commit window 중 모든 행동 거부 — Dash 중 평타 구멍 봉합.
    public override bool AcceptsAction(ActionKind kind) => false;

    public override void Enter(PlayerEntity player)
    {
        player.StateTicksRemaining = Constants.AttackCommitWindowTicks;
        player.AttackLungeVx      = PendingLungeVx;
        player.LungeDecayPerTick  = PendingDecayPerTick;
    }

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player)
    {
        // 전방 lunge 감쇠: 매 틱 LungeDecayPerTick 곱. 평타=0.75, Dash=0.85(더 완만).
        player.AttackLungeVx *= player.LungeDecayPerTick;
        if (System.MathF.Abs(player.AttackLungeVx) < Constants.ExternalImpulseEpsilon)
            player.AttackLungeVx = 0f;

        if (--player.StateTicksRemaining > 0)
            return null;
        return PlayerMovementStates.ResolveGrounded(player);
    }

    public override void Exit(PlayerEntity player)
    {
        player.AttackLungeVx      = 0f;
        player.LungeDecayPerTick  = Constants.KnockbackDecayPerTick;
        PendingLungeVx            = 0f;
        PendingDecayPerTick       = Constants.KnockbackDecayPerTick;
    }
}

// ── HitState ─────────────────────────────────────────────────────────────────

// 피격 hitstun. 이동 잠금 + 넉백 감쇠 + 행동 거부.
// Enter에서 StateTicksRemaining = AnimLatchTicks 세팅 (KnockbackVx는 EnterHitState에서 먼저 세팅됨).
// Tick에서 넉백 감쇠 후 카운터 감소 → 0이면 ResolveGrounded로 복귀.
// Exit에서 KnockbackVx=0 보장.
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
        // 넉백 감쇠: 매 틱 계수를 곱해 지수 감소. 공유 ε(클라 force-adopt 게이트와의 계약) 미만이면 0으로 정리.
        player.KnockbackVx *= Constants.KnockbackDecayPerTick;
        if (System.MathF.Abs(player.KnockbackVx) < Constants.ExternalImpulseEpsilon)
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

// 사망 terminal state. 행동 전부 거부. Tick → null (전환 없음 — Revive()로만 탈출).
internal sealed class DeathState : ActorState<PlayerEntity>
{
    public override AnimState AnimState => AnimState.Death;
    public override bool AcceptsAction(ActionKind kind) => false;

    public override ActorState<PlayerEntity>? Tick(PlayerEntity player) => null;
}
