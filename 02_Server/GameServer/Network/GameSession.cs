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
/// **M3 Phase 02 변경**: 헌법 #2 "Protocol is Sacred" 가짜 약속 1번째 봉합 — first-packet 강제 패턴
///   (handshake 통과 전까지 게임 진입 X). 옛 OnConnected의 AddPlayer 흐름을 EnterGameWorld로 이동,
///   HandleHandshake가 검증 통과 시 호출. mismatch는 즉시 Disconnect (헌법 #3 — timeout 안 기다림).
///
/// 콜백은 socket 워커(IOCP) 스레드. GameMap mutation은 직접 X — `EnqueueJob`으로 push.
/// </summary>
public class GameSession : PacketSession
{
    int _entityId = -1;

    // Phase 10 (M2.5 lifecycle race): _closing 플래그.
    // connect job과 disconnect handler가 *서로 다른 thread*에서 race할 때,
    // queued AddPlayer가 이미 닫힌 세션을 owner로 박지 못하게 + cleanup이 멱등하게 보장.
    // 0=open, 1=closing/closed. Interlocked.Exchange로 atomic.
    int _closing;

    // M3 Phase 02 (헌법 #2 가짜 약속 봉합): handshake 완료 플래그.
    // OnRecvPacket 첫 진입에서 false면 → handshake 패킷만 허용, 다른 패킷 = 즉시 Disconnect.
    // first-packet 강제 패턴 = isolation 보장 (다른 패킷 받기 전 version 검증).
    bool _handshakeCompleted;

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
        Console.WriteLine($"[GameSession] OnConnected from {endPoint} — awaiting C_Handshake");
        // M3 Phase 02: AddPlayer는 handshake 통과 후 EnterGameWorld()가 호출.
        // 권한 미부여 상태에서 서버 리소스(맵 entity)를 미리 박지 않음 = trust boundary 강화.
    }

    // M3 Phase 02 (헌법 #2 봉합): handshake 통과 시 게임 월드 진입.
    // 옛 OnConnected가 직접 호출하던 AddPlayer 흐름을 통째 이동.
    // protected — TestGameSession이 handshake 우회(mock) 시 직접 호출 가능 (lifecycle 테스트 호환).
    protected void EnterGameWorld()
    {
        GameMap? map = GetMap();
        if (map == null)
        {
            // Codex β 검토 권장(Phase 09): silent no-op은 shutdown race에는 맞지만
            // startup/config 버그(GameWorld 초기화 누락)를 은폐 가능. 명시 로그로 표면화.
            Console.WriteLine($"[Trust] GameSession.EnterGameWorld: GetMap() returned null — config/shutdown race?");
            return;
        }
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // Phase 10 (M2.5 lifecycle race): job 실행 시점에 이미 disconnect 왔으면 skip.
            // 안 그러면 닫힌 세션을 owner로 가진 player가 맵에 남아 ghost entity 발생.
            // tick thread에서 실행되므로 Volatile.Read로 충분 (memory barrier 보장).
            if (Volatile.Read(ref self._closing) == 1)
            {
                Console.WriteLine("[Map] AddPlayer skipped — session already closing (lifecycle race window)");
                return;
            }

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

    // M3 Phase 02 (Codex review 인사이트 — Phase 03 진입 캡슐화):
    // handshake 통과 후 lifecycle 전이 묶음 = `_handshakeCompleted` 박힘 + S_HandshakeResult(ok=true) 회신 + EnterGameWorld.
    // **왜 한 메서드로 묶었나**: Phase 03에서 핸들러 layer 분리 시 외부 핸들러 클래스가 *세션 내부 state를 직접 만지지 않게* 하는 게 깔끔. 핸들러는 packet decode + 검증 + (mismatch 거절 / OK이면 본 메서드 호출)만 책임.
    // **테스트 mock 역할도 겸함**: 기존 lifecycle/rate-limit 테스트는 *handshake 이후*의 race/rate 검증이라
    // TestGameSession.OnConnected에서 본 메서드 직접 호출 = handshake 우회. Send override가 socket I/O 차단해서 회신 byte는 버려짐.
    protected void CompleteHandshakeAndEnter()
    {
        _handshakeCompleted = true;
        S_HandshakeResult ok = new S_HandshakeResult
        {
            ok = true,
            serverVersion = ProtocolVersion.Current,
            reason = "",
        };
        Send(ok.Write());
        EnterGameWorld();
    }

    // Phase 10 (M2.5 lifecycle race) 재작성:
    //  - 이전엔 `_entityId < 0` early-return으로 race window cleanup 누락 (γ 감사 위반).
    //  - 이제 *항상* map job 보내고, owner reference 기반으로 cleanup (entityId 모를 때 안전).
    //  - Interlocked.Exchange로 _closing 박고 이중 호출 멱등성 보장.
    //  - 두 번째 OnDisconnected가 와도 enqueue 1회만.
    public override void OnDisconnected(EndPoint endPoint)
    {
        // 이미 닫혔으면 두 번째 호출 — enqueue 안 함 (Codex β 검토 권장: 명시 표현).
        // Exchange 반환값이 1이면 *직전*에 이미 1이었다는 뜻 = 이중 호출.
        if (Interlocked.Exchange(ref _closing, 1) == 1) return;

        Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession.OnDisconnected: GetMap() returned null — config/shutdown race?");
            return;
        }
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // owner reference 기반 cleanup — entityId가 race window 안 -1이어도 안전.
            // AddPlayer가 같은 batch에 들어왔으면 그 entity도 같이 제거 (멱등).
            bool removed = map.RemovePlayerBySession(self);
            Console.WriteLine($"[Map] Session cleanup (entityId={self._entityId}, removed={removed})");
            // Codex β 권장(Phase 10): cleanup 후 _entityId reset.
            // 기능상 ghost는 막혔지만 낡은 id가 로그/방어 로직에 남는 것 차단.
            self._entityId = -1;
        });
    }

    public override void OnSend(int numOfBytes)
        => Console.WriteLine($"[GameSession] OnSend {numOfBytes} bytes");

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

        // M3 Phase 02 (헌법 #2 봉합): first-packet 강제. handshake 통과 전엔 다른 dispatch X.
        if (!_handshakeCompleted)
        {
            if ((PacketID)packetId == PacketID.C_Handshake)
            {
                HandleHandshake(buffer);
            }
            else
            {
                Console.WriteLine(
                    $"[Trust] First packet was {(PacketID)packetId} (not C_Handshake) — disconnecting");
                Disconnect();
            }
            return;
        }

        switch ((PacketID)packetId)
        {
            case PacketID.C_Handshake:
                // M3 Phase 02 (Codex review #5): handshake 통과 후 재-handshake는 protocol violation.
                // 헌법 #2 "Protocol is Sacred" 정합 — silent drop보다 명시적 거절이 진단 가치 ↑.
                // 정상 클라가 두 번 보내는 경우는 *없어야 정상* — 버그 / 악의 / 옛 클라 어느 쪽이든 hard error.
                Console.WriteLine($"[Trust] Duplicate C_Handshake after handshake completed — protocol violation, disconnecting");
                Disconnect();
                break;

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

    // M3 Phase 02 (헌법 #2 봉합): handshake 검증 + 게임 진입 / 거절 분기.
    // ProtocolVersion.Current와 == 비교 (응급 모드). 호환 minor version 표는 본 마감 후 별도 Phase.
    // mismatch 즉시 Disconnect — 헌법 #3 정합 (timeout 안 기다림 = rate-limit 무효화 차단).
    void HandleHandshake(ArraySegment<byte> buffer)
    {
        C_Handshake pkt = new C_Handshake();
        pkt.Read(buffer);

        if (pkt.clientVersion != ProtocolVersion.Current)
        {
            string reason =
                $"ProtocolVersion mismatch (client={pkt.clientVersion}, server={ProtocolVersion.Current})";
            Console.WriteLine($"[Trust] Handshake rejected — {reason}");

            S_HandshakeResult fail = new S_HandshakeResult
            {
                ok = false,
                serverVersion = ProtocolVersion.Current,
                reason = reason,
            };
            Send(fail.Write());
            Disconnect();
            return;
        }

        Console.WriteLine($"[GameSession] Handshake OK (version={pkt.clientVersion})");
        CompleteHandshakeAndEnter();
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
