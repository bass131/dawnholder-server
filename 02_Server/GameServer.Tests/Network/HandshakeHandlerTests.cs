using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// M3 Phase 02 (헌법 #2 "Protocol is Sacred" 가짜 약속 1번째 봉합): handshake 핸들러 회귀 안전망.
///
/// **검증 invariant** (Phase 02 완료 조건):
///   - happy: C_Handshake(version=Current) → S_HandshakeResult(ok=true) 회신 + 게임 진입 (AddPlayer)
///   - mismatch: clientVersion != Current → S_HandshakeResult(ok=false, reason) 회신 + 즉시 Disconnect (헌법 #3 정합)
///   - non-handshake 첫 패킷: handshake 외 패킷으로 시작하면 즉시 Disconnect (first-packet 강제)
///
/// **버전 이력 추적**: `ProtocolVersion.Current` 상수를 직접 참조하므로 bump 시 본 테스트 자동 갱신.
///   - v3 = M3 Phase 06 Combat 4패킷 추가 (C_Attack/S_EntitySpawn/S_HitResult/S_EntityDeath).
///     mismatch 케이스는 Current+1(=4) 사용하므로 추가 갱신 불필요.
///
/// **테스트 전략** (lifecycle/rate-limit 테스트 패턴 정합):
///   - GameSession.GetMap() override로 GameMap 주입 (singleton race 차단)
///   - Send() override로 회신 패킷 캡처 (실제 socket I/O 차단)
///   - Disconnect() override로 호출 카운트 추적
///
/// **새 패킷 추가 규칙(02_Server/CLAUDE.md)**: happy + invalid input 두 갈래 + first-packet 강제 (Phase 02 특수).
/// </summary>
[Collection("ConsoleSerial")]
public class HandshakeHandlerTests : IDisposable
{
    readonly GameMap _map;
    readonly HandshakeTestGameSession _session;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // GameMap 주입 + Send/Disconnect 차단. Send된 패킷은 SentPackets에 캡처.
    class HandshakeTestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public HandshakeTestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> seg)
        {
            // payload 복사 — 호출자의 byte[] 재사용으로 인한 mutation 차단.
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }
        // 본 테스트는 *OnConnected 후 첫 OnRecvPacket*의 동작 검증.
        // OnConnected는 base 그대로 — handshake 대기 상태 (AddPlayer X).
    }

    public HandshakeHandlerTests()
    {
        _map = new GameMap();
        _session = new HandshakeTestGameSession(_map);
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);

        // OnConnected — handshake 대기 진입 (AddPlayer 안 함).
        _session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static ArraySegment<byte> MakeHandshakeBytes(ushort version)
    {
        C_Handshake pkt = new() { clientVersion = version };
        return pkt.Write();
    }

    static ArraySegment<byte> MakeMoveIntentBytes()
    {
        C_MoveIntent pkt = new() { input = 0x01, clientTick = 1 };
        return pkt.Write();
    }

    static S_HandshakeResult ParseHandshakeResult(byte[] sent)
    {
        S_HandshakeResult pkt = new();
        pkt.Read(new ArraySegment<byte>(sent));
        return pkt;
    }

    [Fact]
    public void Happy_MatchingVersion_AcksAndEntersGame()
    {
        // C_Handshake(version=Current) → 서버가 ok=true 회신 + AddPlayer enqueue.
        _session.OnRecvPacket(MakeHandshakeBytes(ProtocolVersion.Current));

        // S_HandshakeResult(ok=true) 회신 검증.
        Assert.Single(_session.SentPackets);
        S_HandshakeResult result = ParseHandshakeResult(_session.SentPackets[0]);
        Assert.True(result.ok);
        Assert.Equal(ProtocolVersion.Current, result.serverVersion);
        Assert.Equal("", result.reason);

        // Disconnect 호출 X.
        Assert.Equal(0, _session.DisconnectCalls);

        // EnterGameWorld가 AddPlayer job enqueue → tick 후 player=1.
        _map.Tick(1);
        Assert.Single(_map.Players);

        // tick 후 추가 패킷:
        //   - S_EnterMap (자기 entityId/spawn 통보, Phase 02부터)
        //   - S_EntitySpawn × N (Phase 06 Step 4 enemy roster 다발 전송 — ctor에서 Normal 1마리 spawn)
        // 헌법 #1 정합: server-only spawn 흐름, 신규 client에 active enemy roster 다발 전송.
        // SentPackets = S_HandshakeResult + S_EnterMap + S_EntitySpawn(enemy 수) = 2 + Enemies.Count.
        Assert.Equal(2 + _map.Enemies.Count, _session.SentPackets.Count);
    }

    [Fact]
    public void Mismatch_HigherVersion_RejectsAndDisconnects()
    {
        // 클라가 미래 버전 보내는 케이스 — 서버는 옛 버전이라 못 받음.
        // (응급 모드 == 비교: 호환 가능 minor도 거절. 본 마감 시 호환표 도입.)
        ushort futureVersion = (ushort)(ProtocolVersion.Current + 1);
        _session.OnRecvPacket(MakeHandshakeBytes(futureVersion));

        // S_HandshakeResult(ok=false, reason 박힘) 회신 검증.
        Assert.Single(_session.SentPackets);
        S_HandshakeResult result = ParseHandshakeResult(_session.SentPackets[0]);
        Assert.False(result.ok);
        Assert.Equal(ProtocolVersion.Current, result.serverVersion);
        Assert.Contains("mismatch", result.reason);
        Assert.Contains(futureVersion.ToString(), result.reason);

        // 즉시 Disconnect (헌법 #3 정합 — timeout 안 기다림).
        Assert.Equal(1, _session.DisconnectCalls);

        // AddPlayer enqueue 안 됨 → tick 후에도 player=0.
        _map.Tick(1);
        Assert.Empty(_map.Players);
    }

    [Fact]
    public void DuplicateHandshake_AfterCompleted_RejectsAsProtocolViolation()
    {
        // Codex review (2026-05-18) 권장 #5: handshake 통과 후 재-handshake는 protocol violation.
        // 1) 첫 handshake → 정상 통과, Disconnect 0회
        _session.OnRecvPacket(MakeHandshakeBytes(ProtocolVersion.Current));
        Assert.Equal(0, _session.DisconnectCalls);

        // 2) 두 번째 handshake → switch case에서 protocol violation 거절
        _session.OnRecvPacket(MakeHandshakeBytes(ProtocolVersion.Current));
        Assert.Equal(1, _session.DisconnectCalls);
        Assert.Contains("Duplicate C_Handshake", _consoleCapture.ToString());
    }

    [Fact]
    public void NonHandshakeFirstPacket_Rejected_NoEntry()
    {
        // 클라가 handshake 우회 시도 — 첫 패킷으로 C_MoveIntent 직진.
        // (악의적 클라 / 옛 클라 / 프로토콜 bug 모두 해당)
        _session.OnRecvPacket(MakeMoveIntentBytes());

        // 즉시 Disconnect. S_HandshakeResult 같은 회신 없음 (그냥 거절).
        Assert.Equal(1, _session.DisconnectCalls);
        Assert.Empty(_session.SentPackets);

        // AddPlayer enqueue 안 됨.
        _map.Tick(1);
        Assert.Empty(_map.Players);

        // [Trust] 로그 박혀있음 (first packet was 진단).
        Assert.Contains("[Trust] First packet was", _consoleCapture.ToString());
    }
}
