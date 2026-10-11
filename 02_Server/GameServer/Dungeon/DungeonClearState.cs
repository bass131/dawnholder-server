using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Dungeon;

// One clear's recipients and pending confirmations, owned by its GameMap and changed only on
// the map tick. A fresh clear replaces this object; retiring the copy releases it with the map.
internal sealed class DungeonClearState
{
    readonly Dictionary<int, GameSession> _awaitingConfirmation;

    DungeonClearState(List<(int EntityId, GameSession Session)> beneficiaries)
    {
        Beneficiaries = beneficiaries.AsReadOnly();
        _awaitingConfirmation = beneficiaries.ToDictionary(recipient => recipient.EntityId, recipient => recipient.Session);
    }

    internal IReadOnlyList<(int EntityId, GameSession Session)> Beneficiaries { get; }

    internal static DungeonClearState? Capture(MapId mapId, InstanceKey? instanceKey, IReadOnlyList<PlayerEntity> players)
    {
        if (mapId != MapId.BossRoom || !instanceKey.HasValue) return null;

        List<(int EntityId, GameSession Session)> beneficiaries = new();
        foreach (PlayerEntity player in players)
        {
            if (player.Owner is { IsClosing: false } session)
                beneficiaries.Add((player.EntityId, session));
        }
        return new DungeonClearState(beneficiaries);
    }

    internal bool IsAwaitingConfirmation(GameSession session, int entityId)
        => _awaitingConfirmation.TryGetValue(entityId, out GameSession? owner) && ReferenceEquals(owner, session);

    internal void MarkConfirmed(int entityId) => _awaitingConfirmation.Remove(entityId);
}
