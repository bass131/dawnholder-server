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
    // S_EnterMap (ID 3)
    // 서버가 정한 spawn 좌표로 Player GameObject 배치 (헌법 #1).
    internal sealed class EnterMapHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EnterMap pkt = new S_EnterMap();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                session.SetLocalEntityId(eid);
                Debug.Log($"[Unity] EnterMap as entity {eid} at server spawn ({x}, {y})");
                MapNameDisplay.SetMapId(0); // S_EnterMap = Town 고정
                AudioManager.Instance?.PlayBgm(SoundKeys.BgmTown); // 최초 입장 = Town BGM (메뉴 BGM 이어짐 방지)
                if (LocalPlayerMovement.Instance != null)
                {
                    // 이 분기도 terrain 주입 — ADR-027 (첫 진입 race 두 순서 모두 관측,
                    // movement가 먼저 깨어난 경우 pending 경로를 안 타서 주입 누락됨).
                    LocalPlayerMovement.Instance.InjectTerrain(0); // S_EnterMap = Town 고정
                    LocalPlayerMovement.Instance.SetServerPosition(new Vector3(x, y, 0f));
                }
                else
                {
                    // LocalPlayerSpawner가 아직 Instantiate 전(초기 진입 race) →
                    // PendingSpawn에 보관 → 곧 spawn될 LocalPlayerMovement.Awake()가 소비.
                    // S_EnterMap에 mapId 없음 → Town(0) 고정. MapTransition 경로는 destMapId 박음.
                    UnityClientSession.PendingSpawnX = x;
                    UnityClientSession.PendingSpawnY = y;
                    UnityClientSession.PendingMapId = 0;
                    UnityClientSession.HasPendingSpawn = true;
                }
            });
        }
    }
}
