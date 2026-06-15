using System;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    // S_PartyUpdate (ID 30) — 파티 상태 브로드캐스트 → PartyState 미러 갱신.
    // partyId==0 → 파티 해산(미러 클리어) 약속.
    internal sealed class PartyUpdateHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PartyUpdate pkt = new S_PartyUpdate();
            pkt.Read(buffer);

            int partyId       = pkt.partyId;
            int leaderId      = pkt.leaderEntityId;
            int member0Id     = pkt.member0EntityId;
            int member1Id     = pkt.member1EntityId;
            byte member0Class = pkt.member0Class;
            byte member1Class = pkt.member1Class;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (partyId == 0)
                {
                    Debug.Log("[Party] 파티 해산 수신 → 미러 클리어");
                    PartyState.Instance.Clear();
                }
                else
                {
                    Debug.Log($"[Party] 업데이트 수신 — partyId={partyId} leader={leaderId} m0={member0Id}(cls={member0Class}) m1={member1Id}(cls={member1Class})");
                    bool wasInParty = PartyState.Instance.InParty;
                    PartyState.Instance.ApplyUpdate(partyId, leaderId, member0Id, member1Id, member0Class, member1Class);
                    if (!wasInParty) AudioManager.Instance?.PlaySfx(SoundKeys.PartyFormed);
                }
            });
        }
    }
}
