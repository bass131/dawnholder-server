using System;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.Network.Handlers;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network.Handlers.Party
{
    // S_PartyInviteRecv (ID 29) — 파티 초대 수신 → PartyState에 대기 초대 기록.
    // P2 팝업이 PartyState.OnInviteReceived를 구독해 표시.
    internal sealed class PartyInviteRecvHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PartyInviteRecv pkt = new S_PartyInviteRecv();
            pkt.Read(buffer);

            int inviterId = pkt.inviterEntityId;
            byte inviterClass = pkt.inviterClass;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Party] 초대 수신 — inviter={inviterId} class={inviterClass}");
                PartyState.Instance.SetPendingInvite(inviterId, inviterClass);
            });
        }
    }
}
