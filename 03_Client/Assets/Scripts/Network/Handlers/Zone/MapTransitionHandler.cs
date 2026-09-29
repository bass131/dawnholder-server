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

namespace Dawnholder.Client.Network.Handlers.Zone
{
    // S_MapTransition (ID 18) — 맵 전환.
    // 헌법 #1: S_MapTransition 도착 후 비로소 scene 전환. 클라 자체 portal 판정 X.
    internal sealed class MapTransitionHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_MapTransition pkt = new S_MapTransition();
            pkt.Read(buffer);

            byte destMapId = pkt.destMapId;
            float spawnX = pkt.spawnX;
            float spawnY = pkt.spawnY;

            session.EnqueueApply(() =>
            {
                session.BeginMapEntry(destMapId, spawnX, spawnY);
            });
        }
    }
}
