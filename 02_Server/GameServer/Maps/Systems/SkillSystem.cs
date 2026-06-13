using Dawnholder.Server.GameServer.Maps.Actions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// §2.2 SkillSystem — GameMap(컨테이너)에서 스킬 로직 추출.
///
/// **단일 책임**: 스킬 1건 처리를 ActionGate 단일 입구에 위임.
/// **호출 규율(§1.1)**: GameMap.Tick 안에서만 호출 (EnqueueJob 람다 경유).
/// **헌법 #1**: 쿨다운·박스 판정·데미지·freeze 서버 단독. 클라는 skillId+attackerClientTick 힌트뿐.
/// </summary>
internal sealed class SkillSystem
{
    readonly ActionGate _gate = new();

    internal void ProcessSkill(GameMap map, int casterEntityId, byte skillId, long attackerClientTick)
    {
        PlayerEntity? caster = map.GetPlayer(casterEntityId);
        if (caster == null) return;

        ActionKind? kind = ActionRegistry.FromSkillId(skillId);
        if (kind == null) return; // 미구현 skillId — 무해 drop.

        _gate.TryPerformSkill(map, caster, kind.Value, attackerClientTick);
    }
}
