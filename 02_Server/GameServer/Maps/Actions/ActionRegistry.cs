using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// ActionKind → IGameAction 단일 진실. OCP: 새 행동은 Flyweight 인스턴스 1개 + 여기 한 줄.
internal static class ActionRegistry
{
    internal static readonly IReadOnlyDictionary<ActionKind, IGameAction> All =
        new Dictionary<ActionKind, IGameAction>
        {
            { ActionKind.Melee,       MeleeAction.Instance },
            { ActionKind.Dash,        DashAction.Instance },
            { ActionKind.Teleport,    TeleportAction.Instance },
            { ActionKind.Thunderbolt, ThunderboltAction.Instance },
        };

    internal static bool TryGet(ActionKind kind, out IGameAction action)
        => All.TryGetValue(kind, out action!);

    // SkillId → ActionKind 매핑. 단일 진실 = ActionKindExtensions.FromSkillId.
    internal static ActionKind? FromSkillId(byte skillId)
        => ActionKindExtensions.FromSkillId(skillId);
}
