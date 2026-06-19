using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// 행동 1건의 계약. 전략 패턴(GPP Strategy) — 새 행동은 구현 클래스 1개 + Registry 한 줄.
// Flyweight: 구현체는 상태 없는 정적 인스턴스 — 틱 루프 new 0 (헌법 #5 정합).
internal interface IGameAction
{
    ActionKind Kind { get; }

    // 쿨다운 (틱 단위). ActionGate가 LastActionTick과 비교.
    int CooldownTicks { get; }

    // 시전 클래스 제약. null = 모든 클래스 허용 (평타 Melee).
    CharacterClass? RequiredClass { get; }

    // 권위 실행. 반환값: 실제로 행동이 적용됐으면 true, 거부면 false.
    // 호출 전제: ActionGate가 상태·쿨다운·클래스·rewind를 모두 통과시킨 뒤에만 호출.
    bool Execute(GameMap map, PlayerEntity caster, in ActionContext ctx);
}
