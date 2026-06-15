using System;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    // S_PartyError (ID 31) — 파티 조작 실패 코드 수신 → PartyState에 전달.
    // reason: 0=상대없음 1=이미파티 2=자기자신 3=정원초과.
    // P2 UI가 PartyState.OnError를 구독해 피드백 표시.
    internal sealed class PartyErrorHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PartyError pkt = new S_PartyError();
            pkt.Read(buffer);

            byte reason = pkt.reason;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Party] 에러 수신 — reason={reason}");
                AudioManager.Instance?.PlaySfx(SoundKeys.UiError);
                PartyState.Instance.SetLastError(reason);
            });
        }
    }
}
