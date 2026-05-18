using System;
using System.Buffers.Binary;
using System.Net;
using Dawnholder.Client.Input;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// ClientNet의 <see cref="PacketSession"/>을 Unity 컨텍스트로 wrap.
    ///
    /// **Phase 05 변경**: Phase 04의 <see cref="ClientSession"/> 직접 상속에서
    /// PacketSession 상속으로 교체. framing 자동, OnRecvPacket은 *완전한 한 패킷*.
    /// **Phase 03 변경**: S_EnterMap 핸들러 추가 — 서버 결정 spawn 좌표 적용.
    /// **Phase 04 변경**: S_Snapshot 핸들러 + Instance singleton (LocalPlayerController가 Send용으로 참조).
    ///
    /// 콜백 모두 socket 워커 스레드 → Unity API는 main-thread queue 경유.
    /// </summary>
    public class UnityClientSession : PacketSession
    {
        // Phase 04: LocalPlayerController가 매 frame C_MoveIntent를 Send하려면 정적 접근점 필요.
        // 일회 설정. NetworkBootstrap이 connect 콜백에서 본 객체를 만들 때 등록.
        public static UnityClientSession? Instance { get; private set; }

        // M3 Phase 02 (Codex review #2): handshake 완료 게이트.
        // OnConnected가 socket 워커 스레드에서 C_Handshake를 자동 Send하지만,
        // *main thread Update*가 그 사이 LocalPlayerController.SendIntent를 호출할 race window가 짧게 존재.
        // 본 플래그는 main thread에서 HandleHandshakeResult가 박음(dispatcher 큐 안) → 같은 thread의
        // SendIntent에서 visibility 보장. ok 회신 도착 전 송신은 drop (헌법 #2 first-packet 정합).
        public bool HandshakeOk { get; private set; }

        // M3 Phase 05: 본인 entityId. HandleEnterMap에서 박음 (main thread).
        // HandleSnapshot이 entityId 비교로 본인/타인 분기. null이면 (EnterMap 도착 전 Snapshot race)
        // 해당 Snapshot drop — 다음 Snapshot에서 정상화.
        public int? LocalEntityId { get; private set; }

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

                default:
                    int unknownId = packetId;
                    MainThreadDispatcher.Enqueue(() =>
                        Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
                    break;
            }
        }

        // M3 Phase 02 (헌법 #2 봉합): 서버 handshake 결과 처리.
        // ok=true → 로그만 (서버가 곧 S_EnterMap 보냄). ok=false → 에러 로그 + 명시적 Disconnect
        // (서버가 이미 끊을 거지만 클라 측 cleanup 일관성).
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
                    LocalPlayerController.Instance.SetServerPosition(new Vector3(x, y, 0f));
                else
                    Debug.LogWarning("[Unity] LocalPlayerController.Instance가 없음 — Player GameObject 미배치?");
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
    }
}
