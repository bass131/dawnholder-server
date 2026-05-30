using System.Net;
using Dawnholder.Client.Net;

namespace Dawnholder.Tools.HeadlessBot;

// **상속 의도** (ADR-012 Y2 정합): 04_ClientNet의 PacketSession을 그대로 받음 →
// 프레이밍·SendBuffer·RecvBuffer는 클라와 100% 동일 코드. 봇은 게임 로직만 다름.
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
    }
}
