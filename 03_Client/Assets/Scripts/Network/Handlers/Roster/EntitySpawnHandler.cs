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
    // S_EntitySpawn (ID 12) — enemy/boss spawn. entityKind 분기. 전환 중 roster buffer 캐싱.
    internal sealed class EntitySpawnHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntitySpawn pkt = new S_EntitySpawn();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            byte kind = pkt.entityKind;
            float x = pkt.x;
            float y = pkt.y;
            int hp = pkt.currentHp;
            int maxHp = pkt.maxHp;

            MainThreadDispatcher.Enqueue(() =>
            {
                // 전환 중이면 roster buffer 캐싱.
                int capturedEid = eid;
                byte capturedKind = kind;
                float capturedX = x;
                float capturedY = y;
                int capturedHp = hp;
                int capturedMaxHp = maxHp;
                if (session.RosterBuffer.TryBuffer(
                        $"S_EntitySpawn entity={eid}",
                        () =>
                        {
                            if (EnemyRegistry.Instance == null)
                            {
                                Debug.LogWarning($"[Unity] EnemyRegistry 미박힘 (roster drain) — entity {capturedEid} spawn drop.");
                                return;
                            }
                            EnemyRegistry.Instance.Spawn(capturedEid, capturedKind, capturedX, capturedY, capturedHp, capturedMaxHp);
                        }))
                    return;

                if (EnemyRegistry.Instance == null)
                {
                    Debug.LogWarning($"[Unity] EnemyRegistry 미박힘 — entity {eid} spawn drop. CombatBootstrap 누락?");
                    return;
                }
                EnemyRegistry.Instance.Spawn(eid, kind, x, y, hp, maxHp);
            });
        }
    }
}
