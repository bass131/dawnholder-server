using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// Multi-player broadcast + initial roster + PlayerLeave 회귀 안전망.
///
/// **검증 invariant**:
///   - S_PlayerJoin: 신규 entity 접속 시 *기존 player 전원*에게 broadcast (자기 제외) +
///                   *자기*에게 기존 entity 다발 Send (initial roster)
///   - S_PlayerLeave: disconnect 시 *남은 player 전원*에게 broadcast (자기 제외)
///   - S_Snapshot broadcast: tick 시 *전원에게* (자기 자신 포함, remote view 정합)
///   - **Lifecycle race 봉합**: closing 중인 세션에는
///       broadcast Send X (`IsClosing` getter + BroadcastToAll skip)
///
/// **테스트 전략**:
///   - Send/Disconnect/GetMap override로 캡처
///   - 두 TestGameSession 인스턴스 + 단일 GameMap 주입 + tick 직접 제어
/// </summary>
[Collection("ConsoleSerial")]
public class BroadcastTests : IDisposable
{
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // MapSpawnTable 단일 진실 공급원에서 spawn 정의 추출.
    static readonly EnemySpawnDef NormalDef = MapSpawnTable.GetSpawnsFor(MapId.HuntingGround)[0];
    static readonly EnemySpawnDef BossDef   = MapSpawnTable.GetSpawnsFor(MapId.BossRoom)[0];

    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public List<byte[]> SentPackets { get; } = new();
        public int DisconnectCalls { get; private set; }

        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // handshake + class 선택 양쪽 우회 (월드 진입까지 mock).
        public override void OnConnected(EndPoint endPoint)
        {
            CompleteHandshakeAndEnter();   // _handshakeCompleted = true
            SetCharacterClass(0);           // HasSelectedClass = true (Warrior)
            EnterGameWorldIfReady();        // → EnterGameWorld() 호출
        }
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
        // HuntingGround(Normal id=1) + Boss 수동 spawn(id=2).
        // NewSession_ReceivesActiveEnemyRoster_OnEnter가 Normal+Boss 2마리 roster를 검증하므로
        // 두 enemy 모두 필요. 결과적으로 플레이어 entityId = 3부터.
        // 좌표/HP는 MapSpawnTable 단일 진실 공급원에서 가져옴.
        _map = new GameMap(MapId.HuntingGround);
        _map.SpawnEnemy(BossDef.Kind, BossDef.X, BossDef.Y, BossDef.MaxHp);

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
        // enemy(1) + Boss(2) ctor spawn에 따른 player id offset — s1=3, s2=4.
        S_PlayerJoin parsed = new S_PlayerJoin();
        byte[] joinPacket = s1New.First(p => PacketIdOf(p) == PacketID.S_PlayerJoin);
        parsed.Read(new ArraySegment<byte>(joinPacket));
        Assert.Equal(4, parsed.entityId);
    }

    [Fact]
    public void NewSession_ReceivesActiveEnemyRoster_OnEnter()
    {
        // 신규 client EnterGameWorld 시 active enemy roster(`S_EntitySpawn`) 다발 전송 검증.
        //
        // **검증 invariant**:
        //   - GameMap ctor가 Normal enemy 1마리 spawn (entityId=1, Normal/(10,0)/HP 30) +
        //     Boss 1마리 spawn (entityId=2, Boss/(30,0)/HP 100) → s1 OnConnected 시 S_EntitySpawn 2건 받음
        //   - entityKind 분류: Normal=0, Boss=1 (wire byte 약속)
        //   - 헌법 #1 정합: server-only spawn 흐름 (클라가 트리거 X)
        TestGameSession s1 = new(_map);
        s1.OnConnected(Ep());
        _map.Tick(1); // EnterGameWorld job 처리 → enemy roster Send 포함

        // s1 받은 패킷에 S_EntitySpawn 2건 박혔는지 (Normal + Boss)
        List<byte[]> spawnPackets = s1.SentPackets
            .Where(p => PacketIdOf(p) == PacketID.S_EntitySpawn).ToList();
        Assert.Equal(2, spawnPackets.Count);

        // entityId로 매칭해 페이로드 내용 검증 (다발 전송 순서에 무관하게 검증 — Dictionary 순회).
        S_EntitySpawn parsedNormal = new S_EntitySpawn();
        S_EntitySpawn parsedBoss = new S_EntitySpawn();
        foreach (byte[] pkt in spawnPackets)
        {
            S_EntitySpawn tmp = new S_EntitySpawn();
            tmp.Read(new ArraySegment<byte>(pkt));
            if (tmp.entityId == 1) parsedNormal = tmp;
            else if (tmp.entityId == 2) parsedBoss = tmp;
        }

        // Normal
        Assert.Equal(1, parsedNormal.entityId);
        Assert.Equal((byte)Dawnholder.Server.GameServer.Combat.EnemyKind.Normal, parsedNormal.entityKind);
        Assert.Equal(NormalDef.X, parsedNormal.x);
        Assert.Equal(NormalDef.Y, parsedNormal.y);
        Assert.Equal(NormalDef.MaxHp, parsedNormal.currentHp);
        Assert.Equal(NormalDef.MaxHp, parsedNormal.maxHp);

        // Boss
        Assert.Equal(2, parsedBoss.entityId);
        Assert.Equal((byte)Dawnholder.Server.GameServer.Combat.EnemyKind.Boss, parsedBoss.entityKind);
        Assert.Equal(BossDef.X, parsedBoss.x);
        Assert.Equal(BossDef.Y, parsedBoss.y);
        Assert.Equal(BossDef.MaxHp, parsedBoss.currentHp);
        Assert.Equal(BossDef.MaxHp, parsedBoss.maxHp);
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
        // enemy(1) + Boss(2) ctor spawn에 따른 player id offset — s1=entityId 3.
        Assert.Equal(3, parsed.entityId);
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
        // enemy(1) + Boss(2) ctor spawn에 따른 player id offset — s1=entityId 3.
        Assert.Equal(3, parsed.entityId);
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
        // IsClosing skip *분기 자체*를 때리는 시나리오 (실제 skip 분기 강제 trigger):
        //   1) s1 정상 접속 + tick → _players=[s1] 안정
        //   2) s2.OnConnected() → s2 EnterGameWorld job *먼저* enqueue
        //   3) s1.OnDisconnected() → s1 cleanup job 나중 enqueue + IsClosing=true *즉시 세팅*
        //   4) Tick → FIFO로 s2 EnterGameWorld가 먼저 실행 → 그 시점에 _players=[s1] *남아있고* s1.IsClosing=true
        //      → BroadcastToAll의 IsClosing skip / initial roster의 IsClosing skip이 *반드시* 작동해야 통과
        //      → skip 깨지면 s1.SentPackets에 PlayerJoin 박힘 = 테스트 fail
        TestGameSession s1 = new(_map);
        s1.OnConnected(Ep());
        _map.Tick(1); // s1 자리잡힘 (_closing=0, _players=[s1])

        int s1BaselineCount = s1.SentPackets.Count;

        // race window: s2 enter job 먼저 enqueue → s1 disconnect 그 다음 (IsClosing 즉시 세팅).
        TestGameSession s2 = new(_map);
        s2.OnConnected(Ep());           // s2 EnterGameWorld enqueue (먼저)
        s1.OnDisconnected(Ep());        // cleanup enqueue (뒤) + Interlocked로 _closing=1 즉시

        _map.Tick(2); // FIFO: s2 enter 처리 시점에 _players=[s1] + s1.IsClosing=true

        // 검증 1: s1은 *map에 남아있는 상태에서* broadcast를 안 받음. IsClosing skip 직접 검증.
        List<byte[]> s1New = s1.SentPackets.Skip(s1BaselineCount).ToList();
        Assert.Equal(0, CountPacketsOfType(s1New, PacketID.S_PlayerJoin));

        // 검증 2: s2의 initial roster는 s1을 *map에서 봤지만* IsClosing skip으로 제외.
        // 만약 roster skip이 깨지면 s2.SentPackets에 s1의 PlayerJoin이 들어감.
        int s2RosterCount = CountPacketsOfType(s2.SentPackets, PacketID.S_PlayerJoin);
        Assert.Equal(0, s2RosterCount);

        // 검증 3: tick 후 s1만 사라지고 s2는 정상 add됨 (s2는 closing 아님).
        // s2 EnterGameWorld 람다는 `self=s2`의 _closing(=0)만 보고 add 진행한다.
        // s1만 cleanup으로 제거되고 s2는 남는다.
        Assert.Single(_map.Players);
        Assert.Same(s2, _map.Players[0].Owner);
    }
}
