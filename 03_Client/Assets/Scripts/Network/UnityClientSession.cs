using System;
using System.Net;
using Dawnholder.Client.Net;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// ClientNet의 <see cref="ClientSession"/>을 Unity 컨텍스트로 wrap.
    ///
    /// 콜백 4종(OnConnected/OnDisconnected/OnRecv/OnSend)은 socket 워커 스레드
    /// 에서 호출됨 → 모두 <see cref="MainThreadDispatcher"/>에 enqueue 한 뒤
    /// Unity Update()에서 실행되도록 marshalling.
    ///
    /// **Phase 04 범위**: 패킷 해석 X. Debug.Log만. Phase 05에서 PacketSession
    /// 기반 wrapper로 교체 또는 분리 예정.
    ///
    /// **closure 캡처 가드**: 콜백 인자(<c>endPoint</c>, <c>buffer.Count</c>)를
    /// 람다에 직접 캡처하면 워커 스레드의 *변경 가능 상태*를 가둘 위험. 안전하게
    /// 로컬 변수로 박은 뒤 캡처.
    /// </summary>
    public class UnityClientSession : ClientSession
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

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            // 모두 처리한 것으로 즉시 반환 (Phase 04: framing 없음).
            // count는 워커 스레드에서 즉시 평가, 람다는 main thread에서 실행.
            int count = buffer.Count;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnRecv {count} bytes"));
            return count;
        }

        public override void OnSend(int numOfBytes)
        {
            int n = numOfBytes;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnSend {n} bytes"));
        }
    }
}
