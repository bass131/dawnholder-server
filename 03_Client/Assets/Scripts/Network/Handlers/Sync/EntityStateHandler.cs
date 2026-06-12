using System;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
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

namespace Dawnholder.Client.Network
{
    // S_EntityState (ID 19) — 적 AI 위치/상태 주기적 갱신.
    // 서버가 SnapshotTickInterval(=2틱=100ms)마다 broadcast.
    internal sealed class EntityStateHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntityState pkt = new S_EntityState();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            int sTick = pkt.serverTick;
            float x = pkt.x;
            float y = pkt.y;
            // state(byte) = 서버 AI FSM 상태 — 시각 미사용.
            // animState(byte) = 시각 애니 상태 — AnimatorDriver 경로로 전달.
            byte animState = pkt.animState;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (EnemyRegistry.Instance == null) return;
                // spawn 전 도착(race)이면 EnemyRegistry.UpdatePosition이 silent skip.
                EnemyRegistry.Instance.UpdatePosition(eid, sTick, x, y, animState);
            });
        }
    }
}
