using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// PingHandler 단위 회귀 안전망.
///
/// **검증 invariant**:
///   - happy: handshake 완료 후 C_Ping → S_Pong 회신, clientTimestampMs 보존
///   - auth: handshake 미완료 상태에서 C_Ping → first-packet 게이트가 차단 + Disconnect (Send X)
///
/// **invalid은 별도 필요 X**: Ping payload는 long timestamp 단독 — body 범위 invalid 없음.
/// malformed bytes(buffer 길이 부족) 검증은 PacketSessionLengthValidationTests가 커버.
/// </summary>
[Collection("ConsoleSerial")]
public class PingHandlerTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // OnConnected는 base 그대로 — handshake 대기 상태. 우회는 BypassHandshake 명시 호출.
        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }

        // 명시적 mock — Happy 테스트에서 handshake 완료 상태 만들 때만 호출.
        public void BypassHandshake() => CompleteHandshakeAndEnter();
    }

    public PingHandlerTests()
    {
        _map = new GameMap();
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    [Fact]
    public void Happy_AfterHandshake_PingResponds_WithPong()
    {
        TestGameSession session = new(_map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        session.BypassHandshake();           // → SentPackets[0] = S_HandshakeResult + AddPlayer enqueue
        _map.Tick(1);                        // → SentPackets[1] = S_EnterMap (EnterGameWorld)
        session.SentPackets.Clear();         // baseline reset — 이후 Ping 회신만 검증

        const long clientTs = 1234567890L;
        C_Ping ping = new C_Ping { clientTimestampMs = clientTs };
        session.OnRecvPacket(ping.Write());

        // S_Pong 회신 검증.
        Assert.Single(session.SentPackets);
        S_Pong pong = new S_Pong();
        pong.Read(new ArraySegment<byte>(session.SentPackets[0]));
        Assert.Equal(clientTs, pong.clientTimestampMs);
        Assert.True(pong.serverTimestampMs > 0);

        Assert.Equal(0, session.DisconnectCalls);
    }

    [Fact]
    public void Auth_BeforeHandshake_PingRejectedByFirstPacketGate()
    {
        TestGameSession session = new(_map);
        session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        // BypassHandshake 호출 X → _handshakeCompleted=false 상태.

        C_Ping ping = new C_Ping { clientTimestampMs = 99L };
        session.OnRecvPacket(ping.Write());

        // first-packet 게이트가 dispatcher 진입 전 차단 — S_Pong 회신 X, Disconnect 1회.
        Assert.Empty(session.SentPackets);
        Assert.Equal(1, session.DisconnectCalls);
        Assert.Contains("[Trust] First packet was C_Ping", _consoleCapture.ToString());
    }
}
