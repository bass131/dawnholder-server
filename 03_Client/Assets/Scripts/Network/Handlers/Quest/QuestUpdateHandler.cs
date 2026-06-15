using System;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    // S_QuestUpdate (ID 32) — 퀘스트 진행 카운터 → QuestState 미러 갱신.
    internal sealed class QuestUpdateHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_QuestUpdate pkt = new S_QuestUpdate();
            pkt.Read(buffer);

            int currentCount = pkt.currentCount;
            int targetCount  = pkt.targetCount;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Quest] 업데이트 수신 — {currentCount}/{targetCount}");
                QuestState.Instance.ApplyUpdate(currentCount, targetCount);
            });
        }
    }
}
