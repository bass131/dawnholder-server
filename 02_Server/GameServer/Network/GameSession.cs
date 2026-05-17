using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.Network;
using Shared.GameData;
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

    // Phase 04: rate-limit 골격 (헌법 #3). 1초 fixed 윈도우 (sliding 아님 — 학습 노트).
    // Phase 05 조정 (2026-05-11):
    //   - 임계값 100 → 500. 240Hz 모니터 사용자의 정상 wire rate가 ~300-500/s라 100은 너무 빡빡.
    //     framerate-bound 송신이 본질 문제 — Phase 06 fixed simulation에서 ~20/s로 정상화 예정.
    //   - 로그 폭주 차단: 윈도우당 *최초 1회만* 출력. 매 패킷마다 [Cheat] 1500줄 폭주 X.
    // Phase 09 (M2.5 Trust-boundary, 2026-05-18): 임계 초과 intent *drop* (헌법 #3 fail-closed 코드 실현).
    //   - 이전엔 로그만 + 처리 진행 → "주석으로 박힌 약속이 가짜" 패턴. 본 Phase에서 봉합.
    //   - 카운트는 임계 이상이어도 *계속 증가* (oscillation attack 방지).
    //   - drop만 — disconnect는 안 함 (정상 클라가 일시적 framerate spike로 임계 초과해도 게임 잘림 X).
    const int IntentRateLimitPerSecond = 500;
    readonly Stopwatch _rateLimitWindow = Stopwatch.StartNew();
    int _intentCountInWindow;
    bool _rateLimitLoggedThisWindow;

    // Phase 09 (M2.5): 테스트가 GameMap을 주입할 수 있는 hook + 셧다운 race null-safe.
    // GameWorld.Instance가 null인 race(테스트 dispose / 서버 종료 직후 in-flight socket callback)
    // 시 null 반환 → 호출자가 안전 no-op. 운영 시 정상 흐름엔 영향 X.
    protected virtual GameMap? GetMap() => GameWorld.Instance?.Map;

    public override void OnConnected(EndPoint endPoint)
    {
        EndPoint ep = endPoint;
        Console.WriteLine($"[GameSession] OnConnected from {ep}");

        GameMap? map = GetMap();
        if (map == null)
        {
            // Codex β 검토 권장(Phase 09): silent no-op은 shutdown race에는 맞지만
            // startup/config 버그(GameWorld 초기화 누락)를 은폐 가능. 명시 로그로 표면화.
            Console.WriteLine($"[Trust] GameSession.OnConnected: GetMap() returned null — config/shutdown race?");
            return;
        }
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

        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession: GetMap() returned null — config/shutdown race?");
            return;
        }
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
    // Phase 07 (M2): byte input 비트필드 도입 — InputBits.Decode로 inputX/jumpPressed 분리.
    //                옛 |inputX|>1 범위 검증은 InputBits가 0/-1/+1만 반환하므로 의미 X —
    //                대신 invalid `11` reserved 코드 cheat 기록으로 대체.
    // 헌법 #3 (Trust Boundary): 모든 클라 입력은 untrusted. rate + invalid 둘 다 검증.
    void HandleMoveIntent(ArraySegment<byte> buffer)
    {
        C_MoveIntent pkt = new C_MoveIntent();
        pkt.Read(buffer);

        // Rate-limit 윈도우 갱신 (1초 fixed).
        if (_rateLimitWindow.ElapsedMilliseconds >= 1000)
        {
            _rateLimitWindow.Restart();
            _intentCountInWindow = 0;
            _rateLimitLoggedThisWindow = false;
        }
        _intentCountInWindow++;
        if (_intentCountInWindow > IntentRateLimitPerSecond)
        {
            // Phase 09 (M2.5): fail-closed drop. 윈도우당 1회만 로그 (폭주 방지).
            // 카운트는 위에서 이미 증가 — drop 후에도 계속 누적 (oscillation 방지).
            if (!_rateLimitLoggedThisWindow)
            {
                Console.WriteLine(
                    $"[Cheat] Player {_entityId}: intent rate exceeded {IntentRateLimitPerSecond}/s — dropping intent (first warning this window)");
                _rateLimitLoggedThisWindow = true;
            }
            return; // 임계 초과 intent는 tick queue 진입 X.
        }

        // Phase 07 비트필드 디코드 (InputBits 단일 출처 — Codex 함정 #2: 양쪽 중복 디코드 금지).
        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(pkt.input);
        if (!valid)
        {
            // Codex 함정 #1: invalid `11` reserved 패턴 — cheat 또는 protocol mismatch.
            // Decode가 inputX=0으로 정상화했으니 시뮬은 폭주 X. 기록만.
            Console.WriteLine(
                $"[Cheat] Player {_entityId}: invalid input bits 0x{pkt.input:X2} — normalized to inputX=0");
        }

        if (_entityId < 0) return; // 아직 EnterMap 안 끝남

        // tick thread로 마샬링: PlayerEntity 갱신.
        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession: GetMap() returned null — config/shutdown race?");
            return;
        }
        int eid = _entityId;
        sbyte capturedInputX = inputX;
        bool capturedJump = jumpPressed;
        uint clientTick = pkt.clientTick;
        map.EnqueueJob(() =>
        {
            PlayerEntity? entity = map.GetPlayer(eid);
            if (entity == null) return; // 이미 RemovePlayer 됐을 수도
            entity.PendingInputX = capturedInputX;
            entity.PendingJumpPressed = capturedJump;
            entity.LastClientTick = clientTick;
        });
    }
}
