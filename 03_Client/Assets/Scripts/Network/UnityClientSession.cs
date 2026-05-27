using System;
using System.Buffers.Binary;
using System.Net;
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
    /// <summary>
    /// ClientNet의 <see cref="PacketSession"/>을 Unity 컨텍스트로 wrap.
    ///
    /// **Phase 05 변경**: Phase 04의 <see cref="ClientSession"/> 직접 상속에서
    /// PacketSession 상속으로 교체. framing 자동, OnRecvPacket은 *완전한 한 패킷*.
    /// **Phase 03 변경**: S_EnterMap 핸들러 추가 — 서버 결정 spawn 좌표 적용.
    /// **Phase 04 변경**: S_Snapshot 핸들러 + Instance singleton (LocalPlayerController가 Send용으로 참조).
    /// **M4.1 Phase 02 변경**: OnHandshakeOkEvent 추가 — NetworkService가 event 기반으로
    ///   C_CharacterSelect 송신 (race 봉합, 옵션 A). (ADR-027: NetworkBootstrap→NetworkService 재정의)
    ///
    /// 콜백 모두 socket 워커 스레드 → Unity API는 main-thread queue 경유.
    /// </summary>
    public class UnityClientSession : PacketSession
    {
        // Phase 04: LocalPlayerController가 매 frame C_MoveIntent를 Send하려면 정적 접근점 필요.
        // 일회 설정. NetworkService가 connect 콜백에서 본 객체를 만들 때 등록.
        public static UnityClientSession Instance { get; private set; }

        // M3 Phase 02 (Codex review #2): handshake 완료 게이트.
        // OnConnected가 socket 워커 스레드에서 C_Handshake를 자동 Send하지만,
        // *main thread Update*가 그 사이 LocalPlayerController.SendIntent를 호출할 race window가 짧게 존재.
        // 본 플래그는 main thread에서 HandleHandshakeResult가 박음(dispatcher 큐 안) → 같은 thread의
        // SendIntent에서 visibility 보장. ok 회신 도착 전 송신은 drop (헌법 #2 first-packet 정합).
        public bool HandshakeOk { get; private set; }

        // M4.1 Phase 02 5-B: handshake OK event. NetworkService가 등록 후 S_HandshakeResult(ok=true)
        // 수신 시 main thread에서 호출됨. C_CharacterSelect 송신 race 봉합 핵심.
        // event 패턴: 구독자 없어도 null check로 안전 (PersistentServices 미생성 씬 단독 Play 방어).
        public event Action OnHandshakeOkEvent;

        // M3 Phase 05: 본인 entityId. HandleEnterMap에서 박음 (main thread).
        // HandleSnapshot이 entityId 비교로 본인/타인 분기. null이면 (EnterMap 도착 전 Snapshot race)
        // 해당 Snapshot drop — 다음 Snapshot에서 정상화.
        public int? LocalEntityId { get; private set; }

        // M4.1 Phase 06 (lag comp 3단계): 마지막으로 수신한 S_Snapshot의 serverTick.
        // C_Attack 송신 시 attackerClientTick 필드에 박아 서버 rewind 기준점을 제공.
        // HandleSnapshot(main thread)에서 갱신. 초기값 0 — 첫 Snapshot 전 공격은 서버가
        // silent drop(검증 규칙: currentServerTick - attackerClientTick > 4)하므로 실전 영향 없음.
        // 본인/타인 Snapshot 모두 갱신 (어느 것이든 서버 현재 tick을 표현하므로 기준점으로 유효).
        public int LastReceivedServerTick { get; private set; }

        // Phase 05: Editor only 송신 latency 시뮬레이션.
        //   0이면 직통 (Release/일반 Play 동작).
        //   >0이면 SendIntent 경로에 한해 N ms 지연 후 실제 Send.
        //   값 변경은 코드 수정 후 Play 재시작 (Inspector 노출은 미래 옵션).
        //   완료 조건 ②③ 검증: 0 → snap 분당 5회 미만 / 200 → snap 빈도 증가 + 점프 시각 확인.
#if UNITY_EDITOR
        public static int SimulatedLatencyMs = 0;
#endif

        public UnityClientSession() => Instance = this;

        /// <summary>
        /// Phase 05: 입력 intent 송신용 wrapper. Editor에선 SimulatedLatencyMs 적용.
        /// Release/일반 Play에선 Send 직통 — 컴파일 시 분기 사라짐(<c>#if UNITY_EDITOR</c>).
        ///
        /// 본 Phase에선 LocalPlayerController가 C_MoveIntent를 이 경로로 보냄.
        /// 다른 패킷(Ping 등)은 그대로 Send 직통 — RTT 측정 시 latency 영향 분리 가능.
        /// </summary>
        public void SendIntent(ArraySegment<byte> buf)
        {
            // M3 Phase 02 (Codex review #2): handshake 통과 전 송신은 drop.
            // 정상 흐름에선 OnConnected의 C_Handshake → S_HandshakeResult OK가 첫 Update tick 안에 박혀서 영향 X.
            // race window (handshake 결과 도착 *이전*에 LocalPlayerController.Update가 SendIntent 호출)에서만 발동.
            if (!HandshakeOk)
            {
                // 폭주 차단 위해 main thread에서 한 줄만. 정상 흐름엔 거의 0회 박힘.
                return;
            }
#if UNITY_EDITOR
            if (SimulatedLatencyMs > 0)
            {
                // buf는 GenPackets.Write()가 매번 새로 할당한 byte[]라 큐 보존 안전(corruption X).
                ArraySegment<byte> captured = buf;
                MainThreadDispatcher.EnqueueDelayed(() => Send(captured), SimulatedLatencyMs / 1000f);
                return;
            }
#endif
            Send(buf);
        }

        public override void OnConnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnConnected to {ep}"));

            // M3 Phase 02 (헌법 #2 봉합): 첫 패킷 = 반드시 C_Handshake.
            // 서버가 first-packet 강제 패턴 박혀있어 다른 패킷 먼저 보내면 즉시 Disconnect.
            // Send 자체는 thread-safe(Session.m_lock) — socket 워커 스레드에서 직접 호출 OK.
            C_Handshake handshake = new C_Handshake { clientVersion = ProtocolVersion.Current };
            Send(handshake.Write());
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] OnDisconnected from {ep}");
                // M3 Phase 05: 모든 타인 entity cleanup — 메모리 누수 차단.
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Clear();
                // M3 Phase 08c: enemy/boss도 동일 cleanup. StageClearUI는 누적 표시 OK라 유지.
                if (EnemyRegistry.Instance != null)
                    EnemyRegistry.Instance.Clear();
                if (Instance == this) Instance = null;
            });
        }

        public override void OnSend(int numOfBytes)
        {
            int n = numOfBytes;
            // Phase 04: intent를 매 frame 보내면 OnSend가 60/s 흘러 console 폭주.
            // 12 bytes 미만(=C_MoveIntent의 [size:2][id:2][inputX:1][padding:3][clientTick:4]?)은 무시.
            // 실제로는 size+id+1+4=9 bytes지만 Write에서 패딩 없음 — 단순히 *짧은 패킷은 조용히*.
            if (n <= 12) return;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnSend {n} bytes"));
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
                new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

            switch ((PacketID)packetId)
            {
                case PacketID.S_HandshakeResult:
                    HandleHandshakeResult(buffer);
                    break;

                case PacketID.S_Pong:
                    HandlePong(buffer);
                    break;

                case PacketID.S_EnterMap:
                    HandleEnterMap(buffer);
                    break;

                case PacketID.S_Snapshot:
                    HandleSnapshot(buffer);
                    break;

                case PacketID.S_PlayerJoin:
                    HandlePlayerJoin(buffer);
                    break;

                case PacketID.S_PlayerLeave:
                    HandlePlayerLeave(buffer);
                    break;

                // M3 Phase 08c: combat dispatch (4 신규 패킷).
                case PacketID.S_EntitySpawn:
                    HandleEntitySpawn(buffer);
                    break;

                case PacketID.S_HitResult:
                    HandleHitResult(buffer);
                    break;

                case PacketID.S_EntityDeath:
                    HandleEntityDeath(buffer);
                    break;

                case PacketID.S_StageClear:
                    HandleStageClear(buffer);
                    break;

                // M4.2 Phase 04: 맵 전환 패킷.
                case PacketID.S_MapTransition:
                    HandleMapTransition(buffer);
                    break;

                default:
                    int unknownId = packetId;
                    MainThreadDispatcher.Enqueue(() =>
                        Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
                    break;
            }
        }

        // M3 Phase 02 (헌법 #2 봉합): 서버 handshake 결과 처리.
        // ok=true → HandshakeOk 박음 + OnHandshakeOkEvent 호출 (M4.1 Phase 02 추가).
        // ok=false → 에러 로그 + 명시적 Disconnect (서버가 이미 끊을 거지만 클라 측 cleanup 일관성).
        //
        // M4.1 Phase 02 5-B: OnHandshakeOkEvent 발화 시점 = HandshakeOk = true 박힌 직후 (같은 main thread).
        // NetworkService.OnHandshakeOk()가 이 이벤트를 받아 C_CharacterSelect 송신.
        void HandleHandshakeResult(ArraySegment<byte> buffer)
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
                    // main thread에서 HandshakeOk 박음 — 같은 thread의 SendIntent visibility 보장.
                    HandshakeOk = true;
                    Debug.Log($"[Unity] Handshake OK (server version={sv})");

                    // M4.1 Phase 02 5-B: event 기반 C_CharacterSelect 송신 트리거.
                    // 구독자(NetworkService) 없는 씬 단독 Play에서도 null이라 안전.
                    OnHandshakeOkEvent?.Invoke();
                }
                else
                {
                    Debug.LogError($"[Unity] Handshake FAILED — {reason} (server version={sv}). Disconnecting.");
                    Disconnect();
                }
            });
        }

        // Phase 03: 서버가 정한 spawn 좌표로 Player GameObject 배치. 헌법 #1 첫 실전.
        void HandleEnterMap(ArraySegment<byte> buffer)
        {
            S_EnterMap pkt = new S_EnterMap();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                LocalEntityId = eid; // M3 Phase 05: 본인 entityId 박음 — Snapshot 분기 기준점.
                Debug.Log($"[Unity] EnterMap as entity {eid} at server spawn ({x}, {y})");
                if (LocalPlayerController.Instance != null)
                {
                    LocalPlayerController.Instance.SetServerPosition(new Vector3(x, y, 0f));
                }
                else
                {
                    // M4.2: LocalPlayerSpawner가 아직 Instantiate 전(초기 진입 race) →
                    // PendingSpawn에 보관 → 곧 spawn될 LocalPlayerController.Start()가 소비.
                    // 맵 전환(HandleMapTransition) 경로와 동일 메커니즘으로 대칭.
                    PendingSpawnX = x;
                    PendingSpawnY = y;
                    HasPendingSpawn = true;
                }
            });
        }

        // Phase 04 (M2): 서버 권위 좌표 적용. prediction 없음 → 매 250ms 스냅 (lag 체감).
        // Phase 05 (M2): prediction 도입 → SetServerPosition 직접 호출 X.
        //   OnServerSnapshot에 위임 → predictor가 threshold 비교 후 snap or 무시.
        // Phase 06 (M2): lastAckedClientTick + input replay로 snap → 부드러운 reconcile.
        // Phase 07 (M2): vx/vy 추가 — Y축 prediction(점프) 도입으로 velocity 동기화 필요.
        // Phase 05 (M3): entityId 분기. 본인 → 기존 reconcile flow (회귀 X 보장).
        //   타인 → RemoteEntityRegistry로 보간 buffer push (지연 spawn 패턴).
        void HandleSnapshot(ArraySegment<byte> buffer)
        {
            S_Snapshot pkt = new S_Snapshot();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.x;
            float y = pkt.y;
            float vx = pkt.vx; // Phase 07: 서버 권위 속도
            float vy = pkt.vy;
            int sTick = pkt.serverTick;
            uint ackedTick = pkt.lastAckedClientTick;

            MainThreadDispatcher.Enqueue(() =>
            {
                // M4.1 Phase 06 (lag comp 3단계): 수신 시점마다 최신 serverTick 보관.
                // LocalEntityId 검사 전에 먼저 갱신 — 어느 entity의 Snapshot이든 서버 현재 tick 표현.
                // C_Attack.attackerClientTick 송신 시 이 값을 참조해 rewind 기준점 제공.
                LastReceivedServerTick = sTick;

                // M3 Phase 05: LocalEntityId 모르면 (EnterMap 전 Snapshot 도착 race) drop.
                if (LocalEntityId == null) return;

                if (eid == LocalEntityId.Value)
                {
                    // 본인 path — 기존 reconcile flow 그대로 (회귀 X 보장).
                    if (LocalPlayerController.Instance != null)
                        LocalPlayerController.Instance.OnServerSnapshot(x, y, vx, vy, sTick, ackedTick);
                }
                else
                {
                    // 타인 path — registry 위임 (지연 spawn 포함).
                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.UpdateSnapshot(eid, x, y);
                }
            });
        }

        // M3 Phase 05: 타인 entity spawn. Phase 04 broadcast 인프라 (S_PlayerJoin) 수신측 dispatch.
        // 본인 entityId가 잘못 박혀 도착해도 무시 (idempotent 안전망 — 정상 흐름엔 X).
        void HandlePlayerJoin(ArraySegment<byte> buffer)
        {
            S_PlayerJoin pkt = new S_PlayerJoin();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (LocalEntityId != null && eid == LocalEntityId.Value) return;
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Spawn(eid, x, y);
            });
        }

        // M3 Phase 05: 타인 entity despawn. Phase 04 broadcast 인프라 (S_PlayerLeave) 수신측 dispatch.
        void HandlePlayerLeave(ArraySegment<byte> buffer)
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

        void HandlePong(ArraySegment<byte> buffer)
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

        // ========================================================================
        // M3 Phase 08c: combat dispatch — enemy/boss spawn + hit + death + clear.
        // 헌법 #1 (Server Authority): 모두 *서버 신호 표시만*. 클라 자체 판정 0.
        // ========================================================================

        // S_EntitySpawn (ID 12) — enemy/boss 새 spawn. entityKind 분기.
        void HandleEntitySpawn(ArraySegment<byte> buffer)
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
                if (EnemyRegistry.Instance == null)
                {
                    Debug.LogWarning($"[Unity] EnemyRegistry 미박힘 — entity {eid} spawn drop. CombatBootstrap 누락?");
                    return;
                }
                EnemyRegistry.Instance.Spawn(eid, kind, x, y, hp, maxHp);
            });
        }

        // S_HitResult (ID 13) — damage 적용 + currentHp/maxHp 갱신.
        // attackerEntityId는 로깅용 (어느 플레이어가 때렸는지). UI 갱신은 target HP bar만.
        void HandleHitResult(ArraySegment<byte> buffer)
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

        // S_EntityDeath (ID 14) — entity 사라짐. Despawn 호출만.
        void HandleEntityDeath(ArraySegment<byte> buffer)
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

        // S_StageClear (ID 15) — 보스 처치 → UI 표시.
        void HandleStageClear(ArraySegment<byte> buffer)
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

        // ========================================================================
        // M4.2 Phase 04: 맵 전환 dispatch.
        // 헌법 #1 (Server Authority): S_MapTransition이 도착해야 비로소 scene 전환.
        //   클라 스스로 "portal에 도달했다" 판정 X. 서버 통보 후 렌더러 역할만 함.
        // ========================================================================

        // S_MapTransition (ID 18) — 맵 전환. destMapId → scene 전환 + spawn 배치.
        //
        // **흐름**:
        //   1. IOCP 워커 스레드에서 패킷 디코드 → main thread 큐 push.
        //   2. Main thread에서: prediction 버퍼 리셋 → remote entity 정리(씬 파괴 자동) →
        //      SceneTransition.Instance.LoadScene(씬이름) 호출 (페이드아웃→LoadAsync→페이드인).
        //   3. 씬 로드 완료 후 새 씬의 LocalPlayerController.Start/Awake에서 Instance 재등록.
        //      S_EnterMap 없이도 spawn 좌표는 S_MapTransition.spawnX/Y로 적용.
        //
        // **prediction 버퍼 리셋 이유**:
        //   좌표계가 맵마다 다름(서버도 맵별 독립 좌표). 이전 맵 입력이 버퍼에 남아있으면
        //   새 맵의 서버 snapshot과 reconcile 시 엉뚱한 좌표로 snap됨. 리셋 의무.
        //
        // **LocalEntityId 유지 (ADR-026)**:
        //   entity id는 migration 내내 유지. 재배정 X. entityId 필드가 패킷에 없음.
        //
        // **remote entity 정리**:
        //   SceneManager.LoadScene(Single)이 옛 씬 GameObject를 모두 파괴 →
        //   RemoteEntityRegistry/EnemyRegistry의 OnDestroy가 Clear()를 자동 호출.
        //   단 씬 전환 전에 Instance가 null이 되는 창이 생기므로 null 가드 필수.
        void HandleMapTransition(ArraySegment<byte> buffer)
        {
            S_MapTransition pkt = new S_MapTransition();
            pkt.Read(buffer);

            byte destMapId = pkt.destMapId;
            float spawnX = pkt.spawnX;
            float spawnY = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                string sceneName = MapIdToSceneName(destMapId);
                Debug.Log($"[Unity] MapTransition → destMapId={destMapId} scene='{sceneName}' spawn=({spawnX:F2},{spawnY:F2})");

                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogError($"[Unity] S_MapTransition: 알 수 없는 destMapId={destMapId} — 전환 취소.");
                    return;
                }

                // prediction 버퍼 리셋: 이전 맵 입력이 새 맵 좌표계에서 replay되면 캐릭터가 튐.
                // LocalPlayerController 씬 파괴 전 미리 리셋 (SetInitialPosition(Vector2.zero)로 클리어).
                if (LocalPlayerController.Instance != null)
                    LocalPlayerController.Instance.ResetPredictionForMapTransition();

                // spawn 좌표 보관 — 씬 로드 완료 후 새 LocalPlayerController가 읽어 적용.
                PendingSpawnX = spawnX;
                PendingSpawnY = spawnY;
                HasPendingSpawn = true;

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

        // M4.2 Phase 04: 씬 로드 완료 후 새 LocalPlayerController가 참조하는 pending spawn 좌표.
        // UnityClientSession은 DontDestroyOnLoad 없이 IOCP 스레드에서 계속 살아있으므로 static 공유.
        // LocalPlayerController.Awake()에서 HasPendingSpawn 확인 → SetServerPosition 호출 → Clear.
        public static float PendingSpawnX { get; private set; }
        public static float PendingSpawnY { get; private set; }
        public static bool HasPendingSpawn { get; private set; }

        // LocalPlayerController.Start()에서 pending spawn 소비 후 호출.
        public static void ConsumePendingSpawn()
        {
            HasPendingSpawn = false;
            PendingSpawnX = 0f;
            PendingSpawnY = 0f;
        }

        // M4.2 Phase 04: destMapId(byte) → Unity 씬 이름 매핑.
        // 서버 MapId enum 값과 정합 (Town=0/HuntingGround=1/BossRoom=2/Ending=3).
        // 씬 이름은 Build Settings의 파일명 기준 (폴더 경로 무관).
        // 매핑이 클라 표현(렌더링 책임)이라 헌법 #1 위반 아님.
        static string MapIdToSceneName(byte mapId) => mapId switch
        {
            0 => "Town",
            1 => "HuntingGround",
            2 => "BossRoom",
            3 => "Ending",
            _ => string.Empty
        };
    }
}
