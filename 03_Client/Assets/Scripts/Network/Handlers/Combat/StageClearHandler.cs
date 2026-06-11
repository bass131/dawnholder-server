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
    // S_StageClear (ID 15) — 보스 처치 → UI 표시.
    internal sealed class StageClearHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_StageClear pkt = new S_StageClear();
            pkt.Read(buffer);

            int bossId = pkt.bossEntityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] StageClear! (boss entity {bossId})");
                if (StageClearUI.Instance == null)
                {
                    Debug.LogWarning("[Unity] StageClearUI 미박힘 — UI drop. CombatBootstrap 누락?");
                    return;
                }
                StageClearUI.Instance.Show(bossId);
            });
        }
    }
}
