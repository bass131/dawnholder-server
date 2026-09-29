using System;
using Dawnholder.Client.Audio;
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

namespace Dawnholder.Client.Network.Handlers.Session
{
    // S_EnterMap (ID 3)
    // 서버가 정한 spawn 좌표로 Player GameObject 배치 (헌법 #1).
    internal sealed class EnterMapHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EnterMap pkt = new S_EnterMap();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            session.EnqueueApply(() =>
            {
                session.SetLocalEntityId(eid);
                Debug.Log($"[Unity] EnterMap as entity {eid} at server spawn ({x}, {y})");
                session.BeginMapEntry(0, x, y, initial: true);
            });
        }
    }
}
