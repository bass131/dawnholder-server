using System;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Net;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    // S_PortalLocked (서버 거부) → ToastUI 표시.
    // required/current는 서버 패킷값 그대로 — 하드코딩 없음 (헌법 #1).
    internal sealed class PortalLockedHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PortalLocked pkt = new S_PortalLocked();
            pkt.Read(buffer);

            int required = pkt.requiredCount;
            int current  = pkt.currentCount;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (ToastUI.Instance == null)
                {
                    Debug.LogWarning("[PortalLockedHandler] ToastUI 미박힘 — 메시지 drop. CombatBootstrap 누락?");
                    return;
                }
                AudioManager.Instance?.PlaySfx(SoundKeys.UiError);
                ToastUI.Instance.Show($"보스 입장: {required}킬 필요 (현재 {current})");
            });
        }
    }
}
