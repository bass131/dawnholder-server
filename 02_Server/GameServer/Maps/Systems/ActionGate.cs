using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.Actions;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.Systems;

// [흐름] GameMap.ProcessAttack/ProcessSkill → ActionGate.TryPerform → IGameAction.Execute
//
// 서버 행동 단일 입구 — 분기 0, OCP 만족 (헌법 §1·§3 권위 검증).
// ①상태 허용 ②쿨다운 ③클래스 ④rewind 4단계 검증 후 Execute 위임.
// tick thread invariant: GameMap.Tick 안에서만 호출 (§1.1).
internal sealed class ActionGate
{
    // 서버 행동 단일 입구 — kind 분기 0. ①상태 ②쿨다운 ③클래스 ④rewind 검증 후 Execute 위임.
    // ctx: ClientTick(rewind), TargetEntityId(평타 전용), Facing(대쉬 방향 권위).
    internal bool TryPerform(GameMap map, PlayerEntity player, ActionKind kind, in ActionContext ctx)
    {
        if (!ActionRegistry.TryGet(kind, out IGameAction action)) return false;

        if (!Validate(map, player, action, ctx.ClientTick, out long currentTick)) return false;

        player.SetLastActionTick(kind, currentTick);

        // 대쉬 방향 권위: 클라 화면 방향을 FacingDir로 갱신 후 Execute(EnterAttackState가 FacingDir로 임펄스).
        //   방향전환 직후 대쉬는 서버 FacingDir이 C_MoveIntent 입력 큐 지연으로 옛 방향이라, 클라 예측(화면
        //   방향)과 반대로 튀어 reconcile 클러스터 발생 → 클라 방향을 권위로 정렬해 봉합. Validate 통과 후만
        //   적용(거부 시 FacingDir 부작용 0). Dash 한정 — Thunderbolt/Teleport는 기존 FacingDir(타겟/박스) 유지.
        if (kind == ActionKind.Dash)
            player.FacingDir = ctx.Facing;

        return action.Execute(map, player, in ctx);
    }

    // 4단계 검증 (헌법 §3 fail-closed silent drop):
    //   ① 상태 허용 — AcceptsAction(kind): commit window·hitstun·사망 중 거부
    //   ② 쿨다운 — LastActionTick(kind) + CooldownTicks > currentTick
    //   ③ 클래스 — RequiredClass(null=무제한) 불일치
    //   ④ rewind — clientTick 범위 검증 (음수/미래/상한 초과)
    bool Validate(GameMap map, PlayerEntity player, IGameAction action, long clientTick, out long currentTick)
    {
        currentTick = map.CurrentTick;

        // ① 상태 허용 (AcceptsAction = 상태가 선언식으로 정책 소유)
        if (!player.ActionFsm.CurrentState.AcceptsAction(action.Kind)) return false;

        // ② 쿨다운 (tick 통일 — ms 기반 LastAttackTickMs 완전 대체)
        if (currentTick - player.GetLastActionTick(action.Kind) < action.CooldownTicks) return false;

        // ③ 클래스 게이트 (defense-in-depth: 핸들러 1차 + 게이트 권위 2차)
        if (action.RequiredClass is { } rc && player.Stats.Class != rc) return false;

        // ④ rewind 범위 검증 (헌법 §3 Trust Boundary)
        if (!CombatSystem.ValidateRewind(clientTick, currentTick)) return false;

        return true;
    }
}
