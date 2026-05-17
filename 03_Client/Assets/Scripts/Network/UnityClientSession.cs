using System;
using System.Buffers.Binary;
using System.Net;
using Dawnholder.Client.Input;
using Dawnholder.Client.Net;
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
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] OnDisconnected from {ep}");
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
                case PacketID.S_Pong:
                    HandlePong(buffer);
                    break;

                case PacketID.S_EnterMap:
                    HandleEnterMap(buffer);
                    break;

                case PacketID.S_Snapshot:
                    HandleSnapshot(buffer);
                    break;

                default:
                    int unknownId = packetId;
                    MainThreadDispatcher.Enqueue(() =>
                        Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
                    break;
            }
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
                Debug.Log($"[Unity] EnterMap as entity {eid} at server spawn ({x}, {y})");
                if (LocalPlayerController.Instance != null)
                    LocalPlayerController.Instance.SetServerPosition(new Vector3(x, y, 0f));
                else
                    Debug.LogWarning("[Unity] LocalPlayerController.Instance가 없음 — Player GameObject 미배치?");
            });
        }

        // Phase 04: 서버 권위 좌표 적용. prediction 없음 → 매 250ms 스냅 (lag 체감).
        // Phase 05: prediction 도입 → SetServerPosition 직접 호출 X.
        //   OnServerSnapshot에 위임 → predictor가 threshold 비교 후 snap or 무시.
        //   매 snapshot 로그는 폐기 (250ms × 다인 → 폭주). snap 발생 시에만 LocalPlayerController가 로깅.
        // Phase 06+: lastAckedClientTick + input replay로 snap → 부드러운 reconcile 진화.
        void HandleSnapshot(ArraySegment<byte> buffer)
        {
            S_Snapshot pkt = new S_Snapshot();
            pkt.Read(buffer);

            float x = pkt.x;
            float y = pkt.y;
            int sTick = pkt.serverTick;
            // TEMP-yuhyeon-20260517: PDL의 lastAckedClientTick이 int인데 클라는 uint로 다룸 —
            // tick counter uint 통일 결정 후 팀장이 PDL 재생성 누락. PDL 재생성되면 캐스트 제거.
            uint ackedTick = (uint)pkt.lastAckedClientTick; // Phase 06 Step 5: replay reconcile 기준점

            MainThreadDispatcher.Enqueue(() =>
            {
                if (LocalPlayerController.Instance != null)
                    LocalPlayerController.Instance.OnServerSnapshot(x, y, sTick, ackedTick);
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
