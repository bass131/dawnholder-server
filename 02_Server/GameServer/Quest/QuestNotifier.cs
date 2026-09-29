using Dawnholder.Server.GameServer.Loop;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Quest;

internal static class QuestNotifier
{
    // Called in the quest job, preserving result order and the existing map send queue.
    public static void Send(GameWorld world, IReadOnlyList<QuestProgressUpdate> updates)
    {
        foreach (QuestProgressUpdate update in updates)
        {
            S_QuestUpdate packet = new S_QuestUpdate
            {
                currentCount = update.CurrentCount,
                targetCount = update.TargetCount,
            };
            world.SendToEntity(update.RecipientEntityId, packet.Write());
        }
    }
}
