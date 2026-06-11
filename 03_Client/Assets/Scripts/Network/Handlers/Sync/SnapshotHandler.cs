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
    // S_Snapshot (ID 4)
    // entityId 분기 — 본인 → reconcile, 타인 → RemoteEntityRegistry 보간 buffer push.
    internal sealed class SnapshotHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_Snapshot pkt = new S_Snapshot();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.x;
            float y = pkt.y;
            float vx = pkt.vx;
            float vy = pkt.vy;
            int sTick = pkt.serverTick;
            uint ackedTick = pkt.lastAckedClientTick;
            byte animState = pkt.animState;

            MainThreadDispatcher.Enqueue(() =>
            {
                // 본인/타인 무관 최신 serverTick 갱신 (lag comp 기준점).
                session.SetLastReceivedServerTick(sTick);

                // LocalEntityId 모르면 (EnterMap 전 Snapshot race) drop.
                if (session.LocalEntityId == null) return;

                if (eid == session.LocalEntityId.Value)
                {
                    // 본인 path — reconcile(+넉백 force-adopt) + 서버 animState 전달.
                    // animState는 두 소비자: 이동 게이트(LocalPlayerMovement) + 시각 애니(LocalPlayerMotion).
                    if (LocalPlayerMovement.Instance != null)
                    {
                        LocalPlayerMovement.Instance.OnServerSnapshot(x, y, vx, vy, sTick, ackedTick, animState);
                        LocalPlayerMovement.Instance.GetComponent<LocalPlayerMotion>()
                            ?.SetServerAnimState(animState);
                    }
                }
                else
                {
                    // 타인 path — 전환 중이면 roster buffer 캐싱.
                    float capturedX = x;
                    float capturedY = y;
                    float capturedVx = vx;
                    int capturedEid = eid;
                    byte capturedAnimState = animState;
                    int capturedTick = sTick;
                    if (session.RosterBuffer.TryBuffer(
                            $"S_Snapshot entity={eid}",
                            () =>
                            {
                                if (RemoteEntityRegistry.Instance != null)
                                    RemoteEntityRegistry.Instance.UpdateSnapshot(capturedEid, capturedTick, capturedX, capturedY, capturedVx, capturedAnimState);
                            }))
                        return;

                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.UpdateSnapshot(eid, sTick, x, y, vx, animState);
                }
            });
        }
    }
}
