using System;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.Network.Handlers;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Network.Handlers.Roster
{
    // S_PlayerLeave (ID 6)
    // 타인 entity despawn.
    internal sealed class PlayerLeaveHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerLeave pkt = new S_PlayerLeave();
            pkt.Read(buffer);

            int eid = pkt.entityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Despawn(eid);
            });
        }
    }
}
