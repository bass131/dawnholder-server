using System.Buffers.Binary;
using System.Diagnostics;
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
/// **Phase 03 변경**: OnConnected/Disconnected가 GameMap actor로 마샬링.
/// **Phase 04 변경**: C_MoveIntent 핸들러 + 검증(헌법 #3) + rate-limit 골격.
///
/// 콜백은 socket 워커(IOCP) 스레드. GameMap mutation은 직접 X — `EnqueueJob`으로 push.
/// </summary>
public class GameSession : PacketSession
{
    int _entityId = -1;

    // Phase 04: rate-limit 골격 (헌법 #3). 1초 슬라이딩 윈도우.
    // 차단은 안 함 (Phase 05+에서 정책 결정). 일단 *기록*만 — 보안 일반 원칙.
    const int IntentRateLimitPerSecond = 100;
    readonly Stopwatch _rateLimitWindow = Stopwatch.StartNew();
    int _intentCountInWindow;

    public override void OnConnected(EndPoint endPoint)
    {
        EndPoint ep = endPoint;
        Console.WriteLine($"[GameSession] OnConnected from {ep}");

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

        if (_entityId < 0) return;

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

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

        switch ((PacketID)packetId)
        {
            case PacketID.C_Ping:
                HandlePing(buffer);
                break;

            case PacketID.C_MoveIntent:
                HandleMoveIntent(buffer);
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

    // Phase 04 (M2): 클라 의도 수신 → 검증 + tick thread로 마샬링.
    // 헌법 #3 (Trust Boundary): 모든 클라 입력은 untrusted. 범위·rate 둘 다 검증.
    void HandleMoveIntent(ArraySegment<byte> buffer)
    {
        C_MoveIntent pkt = new C_MoveIntent();
        pkt.Read(buffer);

        // Rate-limit 윈도우 갱신 (1초 슬라이딩).
        if (_rateLimitWindow.ElapsedMilliseconds >= 1000)
        {
            _rateLimitWindow.Restart();
            _intentCountInWindow = 0;
        }
        _intentCountInWindow++;
        if (_intentCountInWindow > IntentRateLimitPerSecond)
        {
            // *차단 X, 기록 O* — Phase 05+에서 정책 결정.
            Console.WriteLine(
                $"[Cheat] Player {_entityId}: intent rate {_intentCountInWindow}/s > {IntentRateLimitPerSecond}");
            // 그래도 처리 진행 (Phase 04는 기록만).
        }

        // 범위 검증. |inputX| > 1은 즉시 cheat 폐기.
        if (Math.Abs(pkt.inputX) > 1)
        {
            Console.WriteLine(
                $"[Cheat] Player {_entityId}: inputX={pkt.inputX} (range violation) — dropped");
            return;
        }

        if (_entityId < 0) return; // 아직 EnterMap 안 끝남

        // tick thread로 마샬링: PlayerEntity 갱신.
        GameMap map = GameWorld.Instance.Map;
        int eid = _entityId;
        sbyte inputX = pkt.inputX;
        uint clientTick = (uint)pkt.clientTick;
        map.EnqueueJob(() =>
        {
            PlayerEntity? entity = map.GetPlayer(eid);
            if (entity == null) return; // 이미 RemovePlayer 됐을 수도
            entity.PendingInputX = inputX;
            entity.LastClientTick = clientTick;
        });
    }
}
