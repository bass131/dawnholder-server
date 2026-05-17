using System.Net;
using Dawnholder.Client.Net;

namespace Dawnholder.Tools.HeadlessBot;

// Phase 08 Step 2: 봇 측 세션 최소 구현.
//
// **상속 의도** (ADR-012 Y2 정합): 04_ClientNet의 PacketSession을 그대로 받음 →
// 프레이밍·SendBuffer·RecvBuffer는 클라와 100% 동일 코드. 봇은 게임 로직만 다름.
//
// **시나리오 훅**: OnConnected/OnRecvPacket를 콜백으로 외부에 노출 → 시나리오
// 코드(Scenarios/M2BasicMovement.cs 등)가 패킷 driving 담당. 세션 자체는 dumb.
public class BotSession : PacketSession
{
    public Action<EndPoint>? OnConnectedCallback;
    public Action<EndPoint>? OnDisconnectedCallback;
    public Action<ArraySegment<byte>>? OnPacketCallback;

    public override void OnConnected(EndPoint endPoint)
    {
        OnConnectedCallback?.Invoke(endPoint);
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        OnDisconnectedCallback?.Invoke(endPoint);
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        OnPacketCallback?.Invoke(buffer);
    }

    public override void OnSend(int numOfBytes)
    {
        // 봇은 보낸 byte 수에 관심 없음 (Step 4 통합 테스트에서 필요 시 추가).
    }
}
