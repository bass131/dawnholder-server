using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// M3 Phase 04: Multi-player broadcast + initial roster + PlayerLeave 회귀 안전망.
///
/// **검증 invariant**:
///   - S_PlayerJoin: 신규 entity 접속 시 *기존 player 전원*에게 broadcast (자기 제외) +
///                   *자기*에게 기존 entity 다발 Send (initial roster)
///   - S_PlayerLeave: disconnect 시 *남은 player 전원*에게 broadcast (자기 제외)
///   - S_Snapshot broadcast: tick 시 *전원에게* (자기 자신 포함, remote view 정합)
///   - **Lifecycle race 재발 봉합** (Codex Phase 04 risk 1순위): closing 중인 세션에는
///       broadcast Send X. Phase 10 봉합 패턴 일반화 (`IsClosing` getter + BroadcastToAll skip)
///
/// **테스트 전략**:
///   - HandshakeHandlerTests 패턴 정합 — Send/Disconnect/GetMap override로 캡처
///   - 두 TestGameSession 인스턴스 + 단일 GameMap 주입 + tick 직접 제어
/// </summary>
[Collection("ConsoleSerial")]
public class BroadcastTests : IDisposable
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

        public override void OnConnected(EndPoint endPoint) { CompleteHandshakeAndEnter(); }
        public override void Send(ArraySegment<byte> seg)
        {
            byte[] copy = new byte[seg.Count];
            Array.Copy(seg.Array!, seg.Offset, copy, 0, seg.Count);
            SentPackets.Add(copy);
        }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { DisconnectCalls++; }
    }

    public BroadcastTests()
    {
        _map = new GameMap();
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() => Console.SetOut(_originalOut);

    static IPEndPoint Ep() => new IPEndPoint(IPAddress.Loopback, 0);

    // PacketID 헤더(offset 2~3)에서 ID 추출.
    static PacketID PacketIdOf(byte[] payload)
    {
        ushort id = (ushort)(payload[2] | (payload[3] << 8));
        return (PacketID)id;
    }

    static int CountPacketsOfType(List<byte[]> sent, PacketID type)
        => sent.Count(p => PacketIdOf(p) == type);

    [Fact]
    public void TwoSessions_FirstReceivesPlayerJoin_WhenSecondJoins()
    {
        TestGameSession s1 = new(_map);
        s1.OnConnected(Ep());
        _map.Tick(1); // s1 AddPlayer + S_EnterMap (자기에게)
        // s1.SentPackets[0]=S_HandshakeResult, [1]=S_EnterMap

        int s1BaselineCount = s1.SentPackets.Count;

        // s2 접속
        TestGameSession s2 = new(_map);
        s2.OnConnected(Ep());
        _map.Tick(2); // s2 EnterGameWorld job 처리 → s1에 S_PlayerJoin broadcast

        // s1이 s2의 PlayerJoin 1건 받았는지 검증
        List<byte[]> s1New = s1.SentPackets.Skip(s1BaselineCount).ToList();
        Assert.Equal(1, CountPacketsOfType(s1New, PacketID.S_PlayerJoin));
        // entityId=2 (s2)
        S_PlayerJoin parsed = new S_PlayerJoin();
        byte[] joinPacket = s1New.First(p => PacketIdOf(p) == PacketID.S_PlayerJoin);
        parsed.Read(new ArraySegment<byte>(joinPacket));
        Assert.Equal(2, parsed.entityId);
    }

    [Fact]
    public void SecondSession_ReceivesInitialRoster_OnJoin()
    {
        TestGameSession s1 = new(_map);
        s1.OnConnected(Ep());
        _map.Tick(1);

        TestGameSession s2 = new(_map);
        s2.OnConnected(Ep());
        _map.Tick(2); // s2 EnterGameWorld job 처리

        // s2 받은 패킷: S_HandshakeResult + S_EnterMap + S_PlayerJoin(s1 initial roster)
        // initial roster는 자기 entity는 빠지고 *기존* entity만.
        int rosterCount = CountPacketsOfType(s2.SentPackets, PacketID.S_PlayerJoin);
        Assert.Equal(1, rosterCount);

        S_PlayerJoin parsed = new S_PlayerJoin();
        byte[] rosterPacket = s2.SentPackets.First(p => PacketIdOf(p) == PacketID.S_PlayerJoin);
        parsed.Read(new ArraySegment<byte>(rosterPacket));
        Assert.Equal(1, parsed.entityId); // s1
    }

    [Fact]
    public void RemainingSession_ReceivesPlayerLeave_WhenOtherLeaves()
    {
        TestGameSession s1 = new(_map);
        TestGameSession s2 = new(_map);
        s1.OnConnected(Ep());
        s2.OnConnected(Ep());
        _map.Tick(1); // 두 AddPlayer 모두 처리
        Assert.Equal(2, _map.Players.Count);

        int s2BaselineCount = s2.SentPackets.Count;

        // s1 disconnect → cleanup job + S_PlayerLeave broadcast to s2
        s1.OnDisconnected(Ep());
        _map.Tick(2);

        // s2가 PlayerLeave 1건 받았는지
        List<byte[]> s2New = s2.SentPackets.Skip(s2BaselineCount).ToList();
        int leaveCount = CountPacketsOfType(s2New, PacketID.S_PlayerLeave);
        Assert.Equal(1, leaveCount);

        S_PlayerLeave parsed = new S_PlayerLeave();
        byte[] leavePacket = s2New.First(p => PacketIdOf(p) == PacketID.S_PlayerLeave);
        parsed.Read(new ArraySegment<byte>(leavePacket));
        Assert.Equal(1, parsed.entityId); // s1
    }

    [Fact]
    public void Snapshot_BroadcastsToAll_IncludingSelf()
    {
        TestGameSession s1 = new(_map);
        TestGameSession s2 = new(_map);
        s1.OnConnected(Ep());
        s2.OnConnected(Ep());
        _map.Tick(1); // AddPlayer 처리

        int s1Baseline = s1.SentPackets.Count;
        int s2Baseline = s2.SentPackets.Count;

        // SnapshotTickInterval=5 → tick 5의 배수에서 broadcast
        _map.Tick(Shared.GameData.Constants.SnapshotTickInterval);

        // 두 entity 각각의 snapshot이 두 session 양쪽에 도달 (N=2 환경 → 2 packets per session)
        List<byte[]> s1New = s1.SentPackets.Skip(s1Baseline).ToList();
        List<byte[]> s2New = s2.SentPackets.Skip(s2Baseline).ToList();
        Assert.Equal(2, CountPacketsOfType(s1New, PacketID.S_Snapshot));
        Assert.Equal(2, CountPacketsOfType(s2New, PacketID.S_Snapshot));
    }

    [Fact]
    public void LifecycleRace_NewJoinBroadcastSkipsClosingSession()
    {
        // Codex Phase 04 risk 1순위: B가 disconnect 중일 때 A가 join 시도하면 broadcast가 B에게 안 가야.
        // Phase 10 _closing + always-enqueue 패턴 일반화 deterministic 재현.
        //
        // 시나리오:
        // 1) s1 정상 접속 + tick (자리잡힘, _closing=0)
        // 2) s1 OnDisconnected → _closing=1 박힘, cleanup job enqueue (아직 tick 안 함)
        // 3) s2 OnConnected → s2 EnterGameWorld job enqueue (아직 tick 안 함)
        // 4) Tick → 두 job 순차 처리. s2의 EnterGameWorld는 *기존 _players에 s1이 있는 상태*에서 실행 가능
        //    (s1 cleanup이 먼저 실행되면 그 후에 s2 들어옴 → s2는 initial roster 0건)
        //    (s2 EnterGameWorld가 먼저 실행되면 s1.IsClosing=true라 broadcast skip + initial roster skip)
        //
        // 어느 순서든 s1.Send에 PlayerJoin이 박히면 안 됨 (IsClosing skip 검증).
        TestGameSession s1 = new(_map);
        s1.OnConnected(Ep());
        _map.Tick(1); // s1 자리잡힘

        int s1BaselineCount = s1.SentPackets.Count;

        // race window 시작: s1 disconnect + s2 connect 동시 (둘 다 enqueue만 됨)
        s1.OnDisconnected(Ep());                 // _closing=1
        TestGameSession s2 = new(_map);
        s2.OnConnected(Ep());                    // s2 EnterGameWorld enqueue

        _map.Tick(2); // 두 job 처리

        // 검증: s1은 PlayerJoin 0건 추가 받음 (IsClosing skip 덕분).
        List<byte[]> s1New = s1.SentPackets.Skip(s1BaselineCount).ToList();
        Assert.Equal(0, CountPacketsOfType(s1New, PacketID.S_PlayerJoin));

        // 부수 검증: s2 ghost 박힌 s1이 initial roster에 들어가도 *order에 의해 빠짐*.
        // s1.OnDisconnected가 먼저 호출되면 _players에 cleanup job이 enqueue되고
        // s2.OnConnected가 다음 enqueue. Tick에서 cleanup이 먼저 dequeue되면 s2 진입 시
        // _players=[] → initial roster 0건. cleanup이 나중이면 s1.IsClosing=true라 roster skip.
        int s2RosterCount = CountPacketsOfType(s2.SentPackets, PacketID.S_PlayerJoin);
        Assert.Equal(0, s2RosterCount);
    }
}
