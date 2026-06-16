using System;
using Dawnholder.Client.Audio;
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
    // S_EntityDeath (ID 14) — entity 사라짐.
    internal sealed class EntityDeathHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntityDeath pkt = new S_EntityDeath();
            pkt.Read(buffer);

            int eid = pkt.entityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] Entity {eid} died");
                if (EnemyRegistry.Instance == null) return;

                string dieKey = SoundKeys.EnemyDie;
                if (EnemyRegistry.Instance.TryGetKind(eid, out EnemyKind kind))
                {
                    dieKey = kind == EnemyKind.Boss ? SoundKeys.BossDie
                           : kind == EnemyKind.Golem ? SoundKeys.GolemDie
                           : SoundKeys.EnemyDie;
                }
                AudioManager.Instance?.PlaySfx(dieKey);

                EnemyRegistry.Instance.Despawn(eid);
            });
        }
    }
}
