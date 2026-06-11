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
    // S_MapTransition (ID 18) — 맵 전환.
    // 헌법 #1: S_MapTransition 도착 후 비로소 scene 전환. 클라 자체 portal 판정 X.
    internal sealed class MapTransitionHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_MapTransition pkt = new S_MapTransition();
            pkt.Read(buffer);

            byte destMapId = pkt.destMapId;
            float spawnX = pkt.spawnX;
            float spawnY = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                string sceneName = SceneRouter.MapIdToSceneName(destMapId);
                Debug.Log($"[Unity] MapTransition → destMapId={destMapId} scene='{sceneName}' spawn=({spawnX:F2},{spawnY:F2})");

                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogError($"[Unity] S_MapTransition: 알 수 없는 destMapId={destMapId} — 전환 취소.");
                    return;
                }

                MapNameDisplay.SetMapId(destMapId);

                // roster buffer 활성화 — 전환 중 도착하는 roster 패킷 캐싱.
                session.RosterBuffer.BeginTransition(sceneName);

                // prediction 버퍼 리셋: 이전 맵 입력이 새 맵 좌표계에서 replay되면 캐릭터가 튐.
                if (LocalPlayerMovement.Instance != null)
                    LocalPlayerMovement.Instance.ResetPredictionForMapTransition();

                // spawn 좌표 + mapId 보관 — 씬 로드 완료 후 새 LocalPlayerMovement.Awake()가 읽어 적용.
                // PendingMapId: Awake에서 ClientTerrainStore.Load(mapId) 호출 → predictor terrain 주입.
                // 갱신 누락 시 이전 맵 지형으로 예측 → 드리프트 폭증.
                UnityClientSession.PendingSpawnX = spawnX;
                UnityClientSession.PendingSpawnY = spawnY;
                UnityClientSession.PendingMapId = destMapId;
                UnityClientSession.HasPendingSpawn = true;

                // SceneTransition(페이드) 경유 씬 전환. Instance null 시 직접 LoadScene으로 fallback.
                if (SceneTransition.Instance != null)
                    SceneTransition.Instance.LoadScene(sceneName);
                else
                {
                    Debug.LogWarning("[Unity] SceneTransition.Instance null — direct LoadScene fallback (페이드 없음).");
                    SceneManager.LoadScene(sceneName);
                }
            });
        }
    }
}
