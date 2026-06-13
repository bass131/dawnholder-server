using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.Actions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

// [흐름] GameMap.ProcessAttack/ProcessSkill → ActionGate.TryPerform → IGameAction.Execute
//
// 서버 행동 단일 입구 — 분기 0, OCP 만족 (헌법 §1·§3 권위 검증).
// ①상태 허용 ②쿨다운 ③클래스 ④rewind 4단계 검증 후 Execute 위임.
// tick thread invariant: GameMap.Tick 안에서만 호출 (§1.1).
internal sealed class ActionGate
{
    // 평타(Melee)용 오버로드 — targetEntityId가 추가로 필요.
    internal bool TryPerformMelee(GameMap map, PlayerEntity player, int targetEntityId, long clientTick)
    {
        if (!ActionRegistry.TryGet(ActionKind.Melee, out IGameAction action)) return false;

        if (!Validate(map, player, action, clientTick, out long currentTick)) return false;

        player.SetLastActionTick(ActionKind.Melee, currentTick);
        return MeleeAction.Instance.ExecuteWithTarget(map, player, targetEntityId, clientTick);
    }

    // 스킬(Dash/Teleport/Thunderbolt)용 오버로드.
    internal bool TryPerformSkill(GameMap map, PlayerEntity player, ActionKind kind, long clientTick)
    {
        if (!ActionRegistry.TryGet(kind, out IGameAction action)) return false;

        if (!Validate(map, player, action, clientTick, out long currentTick)) return false;

        player.SetLastActionTick(kind, currentTick);
        return action.Execute(map, player, clientTick);
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
