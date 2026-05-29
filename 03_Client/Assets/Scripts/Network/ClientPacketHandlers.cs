using System;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Input;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Network
{
    // S2C 패킷 핸들러 12개 — IClientPacketHandler 구현체.
    // 서버 02_Server/GameServer/Handlers/ 미러 (§3.2).
    //
    // **파일 내 12 핸들러 묶음 이유 (§0.3)**:
    //   각 핸들러는 "파싱 → MainThreadDispatcher.Enqueue" 패턴으로 10~20줄 수준.
    //   모두 Network 도메인의 단일 책임(S2C 디코딩+dispatch). 12개 별도 파일로 쪼개면
    //   "두 파일 열어야 이해 가능" 없이 탐색 비용만 늘어남 → §0.3 과분할 경고.
    //
    // **Handle 진입 시점**: socket 워커 스레드.
    //   Unity API 직접 접근 금지. MainThreadDispatcher.Enqueue 경유 의무.
    // ========================================================================

    // S_HandshakeResult (ID 1)
    // M3 Phase 02: ok=true → HandshakeOk 박음 + OnHandshakeOkEvent 호출.
    // ok=false → 에러 로그 + Disconnect.
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
    // Phase 03: 서버가 정한 spawn 좌표로 Player GameObject 배치. 헌법 #1 첫 실전.
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
                if (LocalPlayerController.Instance != null)
                {
                    LocalPlayerController.Instance.SetServerPosition(new Vector3(x, y, 0f));
                }
                else
                {
                    // M4.2: LocalPlayerSpawner가 아직 Instantiate 전(초기 진입 race) →
                    // PendingSpawn에 보관 → 곧 spawn될 LocalPlayerController.Start()가 소비.
                    UnityClientSession.PendingSpawnX = x;
                    UnityClientSession.PendingSpawnY = y;
                    UnityClientSession.HasPendingSpawn = true;
                }
            });
        }
    }

    // S_Snapshot (ID 4)
    // Phase 05: entityId 분기 — 본인 → reconcile, 타인 → RemoteEntityRegistry 보간 buffer push.
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

            MainThreadDispatcher.Enqueue(() =>
            {
                // M4.1 Phase 06: 본인/타인 무관 최신 serverTick 갱신 (lag comp 기준점).
                session.SetLastReceivedServerTick(sTick);

                // M3 Phase 05: LocalEntityId 모르면 (EnterMap 전 Snapshot race) drop.
                if (session.LocalEntityId == null) return;

                if (eid == session.LocalEntityId.Value)
                {
                    // 본인 path — 기존 reconcile flow 그대로.
                    if (LocalPlayerController.Instance != null)
                        LocalPlayerController.Instance.OnServerSnapshot(x, y, vx, vy, sTick, ackedTick);
                }
                else
                {
                    // 타인 path — P1 봉합: 전환 중이면 roster buffer 캐싱.
                    float capturedX = x;
                    float capturedY = y;
                    int capturedEid = eid;
                    if (session.RosterBuffer.TryBuffer(
                            $"S_Snapshot entity={eid}",
                            () =>
                            {
                                if (RemoteEntityRegistry.Instance != null)
                                    RemoteEntityRegistry.Instance.UpdateSnapshot(capturedEid, capturedX, capturedY);
                            }))
                        return;

                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.UpdateSnapshot(eid, x, y);
                }
            });
        }
    }

    // S_PlayerJoin (ID 5)
    // M3 Phase 05: 타인 entity spawn. P1 봉합: _pendingMapTransition 시 roster buffer 캐싱.
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

                // P1 봉합: 전환 중이면 roster buffer 캐싱.
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
    // M3 Phase 05: 타인 entity despawn.
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

    // S_EntitySpawn (ID 12) — M3 Phase 08c: enemy/boss spawn. entityKind 분기.
    // P1 봉합: 전환 중 roster buffer 캐싱.
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
                // P1 봉합: 전환 중이면 roster buffer 캐싱.
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

    // S_HitResult (ID 13) — M3 Phase 08c: damage 적용 + HP 갱신 표시.
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

    // S_EntityDeath (ID 14) — M3 Phase 08c: entity 사라짐.
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

    // S_StageClear (ID 15) — M3 Phase 08c: 보스 처치 → UI 표시.
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

    // S_MapTransition (ID 18) — M4.2 Phase 04: 맵 전환.
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

                // P1 봉합: roster buffer 활성화.
                session.RosterBuffer.BeginTransition(sceneName);

                // prediction 버퍼 리셋: 이전 맵 입력이 새 맵 좌표계에서 replay되면 캐릭터가 튐.
                if (LocalPlayerController.Instance != null)
                    LocalPlayerController.Instance.ResetPredictionForMapTransition();

                // spawn 좌표 보관 — 씬 로드 완료 후 새 LocalPlayerController가 읽어 적용.
                UnityClientSession.PendingSpawnX = spawnX;
                UnityClientSession.PendingSpawnY = spawnY;
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
