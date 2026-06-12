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
    // S_PlayerHp (ID 21) — 서버 권위 플레이어 HP 통지.
    // 헌법 #1: 클라는 이 값을 신뢰해 HUD에 표시만. HP 직접 계산 X.
    // entityId == LocalEntityId일 때만 소비 (원격 플레이어 HP 바는 미래 범위).
    internal sealed class PlayerHpHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerHp pkt = new S_PlayerHp();
            pkt.Read(buffer);

            int entityId = pkt.entityId;
            int currentHp = pkt.currentHp;
            int maxHp = pkt.maxHp;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;
                if (entityId != session.LocalEntityId.Value) return;

                HudController.Instance?.UpdateHP(currentHp, maxHp);
            });
        }
    }
}
