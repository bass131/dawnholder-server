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
    /// PacketSession 상속으로 교체. 이제 framing이 자동 처리되고 OnRecvPacket이
    /// *완전한 한 패킷* 단위로 호출됨.
    ///
    /// 콜백 모두 socket 워커 스레드에서 호출 → Unity API는 main-thread queue 경유.
    /// closure 캡처 가드(로컬 변수)는 Phase 04 패턴 그대로.
    /// </summary>
    public class UnityClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnConnected to {ep}"));
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnDisconnected from {ep}"));
        }

        public override void OnSend(int numOfBytes)
        {
            int n = numOfBytes;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnSend {n} bytes"));
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            // buffer = [size:2][packetId:2][payload...] 통째.
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

                default:
                    // 알 수 없는 ID — 클라가 받을 일 없는 게 정상. 로그만.
                    int unknownId = packetId;
                    MainThreadDispatcher.Enqueue(() =>
                        Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
                    break;
            }
        }

        // Phase 03 (M2): 서버가 정한 spawn 좌표로 Player GameObject를 배치.
        // 헌법 #1 첫 실전 — 클라는 자기 좌표를 결정하지 않는다.
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

        void HandlePong(ArraySegment<byte> buffer)
        {
            S_Pong pong = new S_Pong();
            pong.Read(buffer);

            // RTT 계산은 워커 스레드에서 즉시 (Unity API 미사용). 로그만 main thread로.
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long rtt = now - pong.clientTimestampMs;
            long oneWayLatencyEstimate = rtt / 2;
            long serverTs = pong.serverTimestampMs;

            MainThreadDispatcher.Enqueue(() =>
                Debug.Log($"[Unity] Pong! RTT = {rtt}ms (one-way ≈ {oneWayLatencyEstimate}ms, serverTs={serverTs})"));
        }
    }
}
