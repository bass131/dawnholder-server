using System;
using Dawnholder.Client.UI;
using Shared.GameData;

namespace Dawnholder.Client.Network.Handlers.Guild
{
    internal sealed class GuildSnapshotHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            if (!GuildPacketReader.TryReadSnapshot(buffer, out var snapshot)) return;
            session.EnqueueApply(() =>
            {
                // A treasury credit (dungeon clear, debug F9) shows its gain as a toast. The gain is the
                // difference between two applied server snapshots; a stale snapshot changes nothing and shows nothing.
                int? before = session.Guild.State.HasSnapshot ? session.Guild.State.Treasury : (int?)null;
                session.Guild.ReceiveSnapshot(snapshot);
                int gain = session.Guild.State.Treasury - (before ?? session.Guild.State.Treasury);
                if (snapshot.ChangeKind == GuildChangeKind.TreasuryCredit && gain > 0 && ToastUI.Instance != null)
                    ToastUI.Instance.Show($"길드 금고 +{gain}");
            });
        }
    }
}
