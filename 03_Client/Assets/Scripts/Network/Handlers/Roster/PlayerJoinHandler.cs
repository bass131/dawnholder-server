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
    // S_PlayerJoin (ID 9)
    // 타인 entity spawn. 전환 중이면 roster buffer 캐싱.
    internal sealed class PlayerJoinHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerJoin pkt = new S_PlayerJoin();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;
            CharacterClass cls = ClassLoadout.ByteToClass(pkt.characterClass);

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId != null && eid == session.LocalEntityId.Value) return;

                // 전환 중이면 roster buffer 캐싱.
                if (session.RosterBuffer.TryBuffer(
                        $"S_PlayerJoin entity={eid}",
                        () =>
                        {
                            if (RemoteEntityRegistry.Instance != null)
                                RemoteEntityRegistry.Instance.Spawn(eid, x, y, cls);
                        }))
                    return;

                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Spawn(eid, x, y, cls);
            });
        }
    }
}
