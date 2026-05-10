using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 게임 도메인의 한 클라이언트 세션. ServerCore의 <see cref="PacketSession"/>을 상속.
///
/// **Phase 07 변경**: Phase 05의 임시 BitConverter 패킷 클래스(PingPacket/PongPacket)에서
/// 자체 PDL이 자동 생성한 `C_Ping`/`S_Pong`로 교체. 명명은 PDL.xml 정의 그대로
/// (camelCase 멤버, C_/S_ 접두사).
///
/// 콜백은 모두 socket 워커 스레드에서 호출됨. 본 Phase에선 Console.WriteLine만 하므로
/// 스레드 안전. 게임 로직(M2+)이 들어오면 맵별 actor 큐로 marshalling 필요
/// (헌법 #5: 맵당 단일 스레드).
/// </summary>
public class GameSession : PacketSession
{
    public override void OnConnected(EndPoint endPoint)
        => Console.WriteLine($"[GameSession] OnConnected from {endPoint}");

    public override void OnDisconnected(EndPoint endPoint)
        => Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

    public override void OnSend(int numOfBytes)
        => Console.WriteLine($"[GameSession] OnSend {numOfBytes} bytes");

    /// <summary>
    /// PacketSession이 framing을 끝낸 *완전한 한 패킷*을 넘김.
    /// buffer = `[size:2][packetId:2][payload...]` 통째.
    /// </summary>
    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        // buffer[2..4] = packetId (little-endian).
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

        switch ((PacketID)packetId)
        {
            case PacketID.C_Ping:
                HandlePing(buffer);
                break;

            default:
                // 헌법 #3 (Trust Boundary): 알 수 없는 패킷은 *조용히 drop + 로그*.
                // M2+에서 cheat-flag 테이블에 기록 추가 예정.
                Console.WriteLine($"[GameSession] Unknown PacketId {packetId} — dropped");
                break;
        }
    }

    void HandlePing(ArraySegment<byte> buffer)
    {
        C_Ping ping = new C_Ping();
        ping.Read(buffer);

        // Pong 응답: 클라 timestamp echo + 서버 timestamp.
        S_Pong pong = new S_Pong
        {
            clientTimestampMs = ping.clientTimestampMs,
            serverTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"[GameSession] Ping received (clientTs={ping.clientTimestampMs}) → Pong");
        Send(pong.Write());
    }
}
