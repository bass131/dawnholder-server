using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 게임 도메인의 한 클라이언트 세션. ServerCore의 <see cref="PacketSession"/>을 상속.
///
/// **Phase 03 변경**: OnConnected/Disconnected가 GameMap actor로 마샬링하도록 진화.
/// 헌법 #1(Server Authority): spawn 좌표는 서버가 정함 — 클라는 S_EnterMap을 받기 전엔
/// 자기 좌표를 결정하지 않는다.
///
/// 콜백은 모두 socket 워커 스레드(IOCP)에서 호출됨. GameMap mutation은 직접 하지 않고
/// `GameMap.EnqueueJob`으로 push → tick thread에서 실행. lock 없음.
/// </summary>
public class GameSession : PacketSession
{
    // 자기 entity의 캐시. 여러 콜백(Disconnect 등)에서 정리에 필요.
    // IOCP는 같은 세션에 OnConnected/Disconnected를 직렬 호출 → race 부재.
    int _entityId = -1;

    public override void OnConnected(EndPoint endPoint)
    {
        EndPoint ep = endPoint;
        Console.WriteLine($"[GameSession] OnConnected from {ep}");

        // Phase 03 (M2): 서버 권위 spawn. 마샬링을 거쳐 tick thread에서 entity 생성.
        GameMap map = GameWorld.Instance.Map;
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // 헌법 #1 시연을 위해 spawn 좌표를 서버가 정함. 현재 (0, 0).
            // Phase 03 검증 단계엔 (3, 0)으로 잠시 바꿔 Unity 캐릭터가 그 자리에 뜨는지
            // 캡처로 시각 확인 완료 (DONE.md AC 섹션 참조).
            Vector2 spawnPos = new Vector2(0f, 0f);
            PlayerEntity entity = map.AddPlayer(self, spawnPos);
            self._entityId = entity.EntityId;

            S_EnterMap pkt = new S_EnterMap
            {
                entityId = entity.EntityId,
                spawnX = entity.Position.X,
                spawnY = entity.Position.Y
            };
            self.Send(pkt.Write());

            Console.WriteLine(
                $"[Map] Player {entity.EntityId} entered at ({entity.Position.X}, {entity.Position.Y})");
        });
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        EndPoint ep = endPoint;
        Console.WriteLine($"[GameSession] OnDisconnected from {ep}");

        if (_entityId < 0) return; // AddPlayer가 아직 처리 안 됐다면 정리할 게 없음

        GameMap map = GameWorld.Instance.Map;
        int eid = _entityId;
        map.EnqueueJob(() =>
        {
            bool removed = map.RemovePlayer(eid);
            Console.WriteLine($"[Map] Player {eid} left (removed={removed})");
        });
    }

    public override void OnSend(int numOfBytes)
        => Console.WriteLine($"[GameSession] OnSend {numOfBytes} bytes");

    /// <summary>
    /// PacketSession이 framing을 끝낸 *완전한 한 패킷*을 넘김.
    /// buffer = `[size:2][packetId:2][payload...]` 통째.
    /// </summary>
    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

        switch ((PacketID)packetId)
        {
            case PacketID.C_Ping:
                HandlePing(buffer);
                break;

            default:
                Console.WriteLine($"[GameSession] Unknown PacketId {packetId} — dropped");
                break;
        }
    }

    void HandlePing(ArraySegment<byte> buffer)
    {
        C_Ping ping = new C_Ping();
        ping.Read(buffer);

        S_Pong pong = new S_Pong
        {
            clientTimestampMs = ping.clientTimestampMs,
            serverTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Console.WriteLine($"[GameSession] Ping received (clientTs={ping.clientTimestampMs}) → Pong");
        Send(pong.Write());
    }
}
