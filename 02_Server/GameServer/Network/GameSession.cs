using System.Net;
using Dawnholder.Server.Network;

namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 게임 도메인의 한 클라이언트 세션. ServerCore의 <see cref="Session"/>을 상속.
///
/// **Phase 04 범위**: 살아있는 connection 1개가 양쪽에 인지되는지만 시연.
/// 패킷 처리·해석은 Phase 05(framing 도입)에서.
///
/// 콜백 4종(OnConnected/OnDisconnected/OnRecv/OnSend)은 모두
/// **socket 워커 스레드**에서 호출됨. 본 Phase에선 Console.WriteLine만 하므로
/// 스레드 안전 (Console은 내부 lock 보유). 게임 로직이 들어오면(Phase 05+)
/// 맵별 actor 큐로 marshalling 필요 (헌법 #5: 맵당 단일 스레드).
/// </summary>
public class GameSession : Session
{
    public override void OnConnected(EndPoint endPoint)
        => Console.WriteLine($"[GameSession] OnConnected from {endPoint}");

    public override void OnDisconnected(EndPoint endPoint)
        => Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

    /// <summary>
    /// 받은 바이트 수만 로그. framing 없이 *모든 바이트를 처리한 것으로* 반환 →
    /// RecvBuffer가 즉시 비워짐. Phase 05에서 PacketSession으로 교체 예정.
    /// </summary>
    public override int OnRecv(ArraySegment<byte> buffer)
    {
        Console.WriteLine($"[GameSession] OnRecv {buffer.Count} bytes");
        return buffer.Count;
    }

    public override void OnSend(int numOfBytes)
        => Console.WriteLine($"[GameSession] OnSend {numOfBytes} bytes");
}
