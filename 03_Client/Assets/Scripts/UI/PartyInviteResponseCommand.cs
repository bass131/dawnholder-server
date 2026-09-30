#nullable enable
using System;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    // 초대 응답의 로컬 요청 처리만 맡으며 membership 확정은 서버 update에 남긴다.
    internal sealed class PartyInviteResponseCommand
    {
        readonly Func<PartyState?> _getPartyState;
        readonly Func<UnityClientSession?> _getSession;

        internal PartyInviteResponseCommand(Func<PartyState?> getPartyState,
            Func<UnityClientSession?> getSession)
        {
            _getPartyState = getPartyState;
            _getSession = getSession;
        }
        internal void Execute(byte accept)
        {
            // PartyState는 DontDestroyOnLoad — 런타임 null 방어.
            if (_getPartyState() == null) return;
            PartyState state = _getPartyState()!;
            if (!state.HasPendingInvite)
            {
                Debug.LogWarning("[PartyInvitePopup] HasPendingInvite=false — 응답 취소.");
                return;
            }

            UnityClientSession? session = _getSession();
            if (session == null || !session.HandshakeOk)
            {
                Debug.LogWarning("[PartyInvitePopup] 세션 없음 또는 Handshake 미완료 — 응답 송신 불가.");
                return;
            }

            int inviterId = state.PendingInviterEntityId;
            var pkt = new C_PartyRespond { inviterEntityId = inviterId, accept = accept };
            session.SendIntent(pkt.Write());

            // void 정상 반환은 송신 접수나 서버 수락을 보장하지 않는다.
            state.ClearPendingInvite();
            Debug.Log($"[PartyInvitePopup] C_PartyRespond 송신 — inviterId={inviterId} accept={accept}");
        }

    }
}
