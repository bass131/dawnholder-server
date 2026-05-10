using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 게임 도메인의 한 클라이언트 세션. ServerCore의 <see cref="PacketSession"/>을 상속.
///
/// **Phase 05 변경**: Phase 04의 raw <see cref="Session"/> 상속에서 PacketSession으로 교체.
/// 이제 framing(`[size(2)][packetId(2)][payload]`)이 자동 처리되고 <see cref="OnRecvPacket"/>이
/// *완전한 한 패킷 단위*로 호출됨.
///
/// 콜백은 모두 socket 워커 스레드에서 호출됨. 본 Phase에선 Console.WriteLine만 하므로
/// 스레드 안전. 게임 로직(Phase 06+)이 들어오면 맵별 actor 큐로 marshalling 필요
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
        // buffer[2..4] = packetId (little-endian, PingPacket/PongPacket과 동일 약속).
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

        switch ((PacketId)packetId)
        {
            case PacketId.Ping:
                HandlePing(buffer);
                break;

            default:
                // 헌법 #3 (Trust Boundary): 알 수 없는 패킷은 *조용히 drop + 로그*.
                // Phase 06+에서 cheat-flag 테이블에 기록 추가 예정.
                Console.WriteLine($"[GameSession] Unknown PacketId {packetId} — dropped");
                break;
        }
    }

    void HandlePing(ArraySegment<byte> buffer)
    {
        PingPacket ping = new PingPacket();
        ping.Read(buffer);

        // Pong 응답: 클라 timestamp echo + 서버 timestamp.
        PongPacket pong = new PongPacket
        {
            ClientTimestampMs = ping.ClientTimestampMs,
            ServerTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"[GameSession] Ping received (clientTs={ping.ClientTimestampMs}) → Pong");
        Send(new ArraySegment<byte>(pong.ToBytes()));
    }
}
