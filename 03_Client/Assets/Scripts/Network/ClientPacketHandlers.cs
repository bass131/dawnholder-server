using System;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Network
{
    // S2C 패킷 핸들러 — IClientPacketHandler 구현체. 서버 02_Server/GameServer/Handlers/ 미러.
    //
    // **Handle 진입 시점**: socket 워커 스레드.
    //   Unity API 직접 접근 금지. MainThreadDispatcher.Enqueue 경유 의무.
    // ========================================================================

    // S_HandshakeResult (ID 1)
    // ok=true → HandshakeOk 박음 + OnHandshakeOkEvent 호출. ok=false → 에러 로그 + Disconnect.
    internal sealed class HandshakeResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_HandshakeResult pkt = new S_HandshakeResult();
            pkt.Read(buffer);

            bool ok = pkt.ok;
            ushort sv = pkt.serverVersion;
            string reason = pkt.reason;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (ok)
                {
                    session.SetHandshakeOk();
                    Debug.Log($"[Unity] Handshake OK (server version={sv})");
                    session.RaiseHandshakeOk();
                }
                else
                {
                    Debug.LogError($"[Unity] Handshake FAILED — {reason} (server version={sv}). Disconnecting.");
                    session.Disconnect();
                }
            });
        }
    }

    // S_Pong (ID 2)
    internal sealed class PongHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_Pong pong = new S_Pong();
            pong.Read(buffer);

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long rtt = now - pong.clientTimestampMs;
            long oneWayLatencyEstimate = rtt / 2;
            long serverTs = pong.serverTimestampMs;

            MainThreadDispatcher.Enqueue(() =>
                Debug.Log($"[Unity] Pong! RTT = {rtt}ms (one-way ≈ {oneWayLatencyEstimate}ms, serverTs={serverTs})"));
        }
    }

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
                    // 본인 path — reconcile + 서버 animState 전달.
                    if (LocalPlayerMovement.Instance != null)
                    {
                        LocalPlayerMovement.Instance.OnServerSnapshot(x, y, vx, vy, sTick, ackedTick);
                        LocalPlayerMovement.Instance.GetComponent<LocalPlayerMotion>()
                            ?.SetServerAnimState(animState);
                    }
                }
                else
                {
                    // 타인 path — 전환 중이면 roster buffer 캐싱.
                    float capturedX = x;
                    float capturedY = y;
                    int capturedEid = eid;
                    byte capturedAnimState = animState;
                    if (session.RosterBuffer.TryBuffer(
                            $"S_Snapshot entity={eid}",
                            () =>
                            {
                                if (RemoteEntityRegistry.Instance != null)
                                    RemoteEntityRegistry.Instance.UpdateSnapshot(capturedEid, capturedX, capturedY, capturedAnimState);
                            }))
                        return;

                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.UpdateSnapshot(eid, x, y, animState);
                }
            });
        }
    }

    // S_PlayerJoin (ID 5)
    // 타인 entity spawn. 전환 중이면 roster buffer 캐싱.
    internal sealed class PlayerJoinHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerJoin pkt = new S_PlayerJoin();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId != null && eid == session.LocalEntityId.Value) return;

                // 전환 중이면 roster buffer 캐싱.
                if (session.RosterBuffer.TryBuffer(
                        $"S_PlayerJoin entity={eid}",
                        () =>
                        {
                            if (RemoteEntityRegistry.Instance != null)
                                RemoteEntityRegistry.Instance.Spawn(eid, x, y);
                        }))
                    return;

                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Spawn(eid, x, y);
            });
        }
    }

    // S_PlayerLeave (ID 6)
    // 타인 entity despawn.
    internal sealed class PlayerLeaveHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerLeave pkt = new S_PlayerLeave();
            pkt.Read(buffer);

            int eid = pkt.entityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Despawn(eid);
            });
        }
    }

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

    // S_HitResult (ID 13) — damage 적용 + HP 갱신 표시.
    // 헌법 #1: 클라는 서버 결과 표시만. 판정 X.
    internal sealed class HitResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_HitResult pkt = new S_HitResult();
            pkt.Read(buffer);

            int attackerId = pkt.attackerEntityId;
            int targetId = pkt.targetEntityId;
            int dmg = pkt.damage;
            int hp = pkt.currentHp;
            int maxHp = pkt.maxHp;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] Hit: attacker={attackerId} target={targetId} dmg={dmg} hp={hp}/{maxHp}");
                if (EnemyRegistry.Instance == null) return;
                EnemyRegistry.Instance.ApplyHit(targetId, hp, maxHp);
            });
        }
    }

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
                EnemyRegistry.Instance.Despawn(eid);
            });
        }
    }

    // S_StageClear (ID 15) — 보스 처치 → UI 표시.
    internal sealed class StageClearHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_StageClear pkt = new S_StageClear();
            pkt.Read(buffer);

            int bossId = pkt.bossEntityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] StageClear! (boss entity {bossId})");
                if (StageClearUI.Instance == null)
                {
                    Debug.LogWarning("[Unity] StageClearUI 미박힘 — UI drop. CombatBootstrap 누락?");
                    return;
                }
                StageClearUI.Instance.Show(bossId);
            });
        }
    }

    // S_EntityState (ID 19) — 적 AI 위치/상태 주기적 갱신.
    // 서버가 SnapshotTickInterval(=2틱=100ms)마다 broadcast.
    internal sealed class EntityStateHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntityState pkt = new S_EntityState();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.x;
            float y = pkt.y;
            // state(byte) = 서버 AI FSM 상태 — 시각 미사용.
            // animState(byte) = 시각 애니 상태 — AnimatorDriver 경로로 전달.
            byte animState = pkt.animState;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (EnemyRegistry.Instance == null) return;
                // spawn 전 도착(race)이면 EnemyRegistry.UpdatePosition이 silent skip.
                EnemyRegistry.Instance.UpdatePosition(eid, x, y, animState);
            });
        }
    }

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
