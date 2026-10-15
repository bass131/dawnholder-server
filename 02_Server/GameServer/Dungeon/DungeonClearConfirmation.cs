using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Maps.Transitions;
using Dawnholder.Server.GameServer.Sessions;

namespace Dawnholder.Server.GameServer.Dungeon;

internal static class DungeonClearConfirmation
{
    // Called only by the captured current map's job, after the receive thread has validated 40.
    internal static void Execute(GameSession session, int entityId, GameMap map)
    {
        if (map.MapId != MapId.BossRoom || !map.InstanceKey.HasValue || !map.IsStageCleared ||
            !session.OwnsPlayer(map, entityId))
        {
            return;
        }

        DungeonClearState? clear = map.DungeonClear;
        if (clear == null || !clear.IsAwaitingConfirmation(session, entityId)) return;

        MapMoveResult result = MapMigration.MoveToPublicMap(session, entityId, map, MapId.Town);
        // A refusal keeps the pending confirmation. Accepted means the transfer has started;
        // removing now prevents two queued confirms from starting the same transfer twice.
        if (result == MapMoveResult.Accepted) clear.MarkConfirmed(entityId);
    }
}
