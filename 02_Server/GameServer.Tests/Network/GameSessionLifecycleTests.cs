using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;

namespace GameServer.Tests.Network;

/// <summary>
/// connect/disconnect race window 봉합 회귀 안전망.
///
/// **검증 invariant**: connect 직후 tick 전에 disconnect가 와도 ghost player가 맵에 남지 않는다.
/// 진입 job과 월드 종료 요청 큐의 순서를 바꿔도 닫힌 세션이 player로 남지 않는지 확인한다.
///
/// **테스트 전략 (deterministic)**:
/// - 실제 GameWorld를 만들고 world tick을 직접 진행하여 close queue까지 검증
/// - world tick 호출 시점을 제어하여 각 순서를 결정론적으로 검증
/// - rapid smoke 100회는 누적 누수 회귀 보호
/// </summary>
[Collection("GameWorldRegistryTests")]
public class GameSessionLifecycleTests : IDisposable
{
    readonly GameWorld _world;
    readonly GameMap _map;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // GameMap 주입 + Send/Disconnect 차단 (socket 없음).
    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // handshake mock: 본 테스트는 *handshake 이후*의 race 흐름 검증.
        // 월드 진입 = handshake + class 선택 양쪽 충족 필요.
        // lifecycle race 검증 목적이라 두 조건 모두 우회.
        public override void OnConnected(EndPoint endPoint)
        {
            CompleteHandshakeAndEnter();     // handshake 우회 (_handshakeCompleted = true)
            SetCharacterClass(0);             // class 선택 우회 (Knight)
            EnterGameWorldIfReady();          // 두 조건 충족 → EnterGameWorld() 호출
        }
        public override void Send(ArraySegment<byte> _) { }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }

    public GameSessionLifecycleTests()
    {
        // Town(빈 맵) — lifecycle 테스트는 enemy 불필요.
        // 플레이어 entityId = 1 (enemy 없음 → 첫 발급 = 1).
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        _map = _world.Map;
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);
    }

    public void Dispose() { _world.Stop(); Console.SetOut(_originalOut); }
    void Tick(long tick) => LifecycleTestWorld.Tick(_world, tick);

    static IPEndPoint Ep() => new IPEndPoint(IPAddress.Loopback, 0);

    [Fact]
    public void ScenarioA_DisconnectBeforeTick_NoGhostPlayer()
    {
        // 핵심 race 시나리오 — deterministic.
        // 1. OnConnected → AddPlayer job 1개 enqueue (아직 tick 안 함)
        // 2. tick 전에 OnDisconnected → 별도 월드 close queue에 요청
        // 3. World tick → close queue 및 map job 처리 → players=0
        TestGameSession session = new(_map);

        session.OnConnected(Ep());     // job 1 enqueue
        Assert.Empty(_map.Players);     // 아직 tick 안 함

        session.OnDisconnected(Ep());   // job 2 enqueue (closing 플래그 박힘)
        Assert.Empty(_map.Players);

        Tick(1); // 두 job 처리

        // AddPlayer job은 _closing 체크로 skip. RemovePlayerBySession은 0건 제거(이미 없음).
        // 결과: ghost player 없음.
        Assert.Empty(_map.Players);
        Assert.Contains("AddPlayer skipped", _consoleCapture.ToString());
    }

    [Fact]
    public void ScenarioB_DisconnectAfterTick_CleansUpProperly()
    {
        // 회귀 시나리오 — 정상 flow.
        // 1. OnConnected → tick → AddPlayer 적용, _entityId 박힘
        // 2. OnDisconnected → tick → RemovePlayer
        TestGameSession session = new(_map);

        session.OnConnected(Ep());
        Tick(1);
        Assert.Single(_map.Players);

        session.OnDisconnected(Ep());
        Tick(2);

        Assert.Empty(_map.Players);
        Assert.Equal(-1, session.EntityId);
    }

    [Fact]
    public void ScenarioC_DoubleDisconnect_Idempotent()
    {
        // 멱등성 — OnDisconnected 2회 호출해도 enqueue 1회만.
        // Interlocked.Exchange 반환값 1이면 두 번째 호출은 early return.
        TestGameSession session = new(_map);

        session.OnConnected(Ep());
        Tick(1);

        // 첫 OnDisconnected → cleanup job enqueue
        session.OnDisconnected(Ep());
        // 두 번째 OnDisconnected → early return (이중 enqueue 차단)
        session.OnDisconnected(Ep());

        Tick(2);

        Assert.Empty(_map.Players);
        // "[GameSession] OnDisconnected from" 로그는 첫 호출에서만 박힘 = 1번
        int disconnectLogs = CountSubstring(_consoleCapture.ToString(), "[GameSession] OnDisconnected from");
        Assert.Equal(1, disconnectLogs);
    }

    [Fact]
    public void ScenarioD_RapidConnectDisconnect_100x_NoLeak()
    {
        // 반복 진입·종료의 누적 누수 회귀 안전망.
        // 매 iteration마다 새 session, 매 iteration 후 tick.
        for (int i = 0; i < 100; i++)
        {
            TestGameSession s = new(_map);
            s.OnConnected(Ep());
            s.OnDisconnected(Ep());
            Tick((long)(i + 1));
        }

        Assert.Empty(_map.Players); // 누적 player 0
    }

    [Fact]
    public void ScenarioA2_DisconnectThenConnect_OrderReversed_NoGhostPlayer()
    {
        // ScenarioA는 OnConnected → OnDisconnected 순서.
        // 역순(OnDisconnected → OnConnected)도 안전한지 별도 검증.
        // 실 운영엔 거의 없을 케이스지만 race 안전망의 대칭성 확증.
        TestGameSession session = new(_map);

        session.OnDisconnected(Ep());  // _closing=1 박힘, cleanup job 1 enqueue
        session.OnConnected(Ep());     // closing 상태에서는 진입을 예약하지 않는다.


        Tick(1); // 종료 요청 처리. 역순 진입은 예약되지 않아 player가 생기지 않는다.

        Assert.Empty(_map.Players);
        Assert.Equal(-1, session.EntityId);
    }

    [Fact]
    public void EntityId_ResetAfterCleanup()
    {
        // Observe the reset directly after the real world cleanup tick.
        TestGameSession session = new(_map);
        session.OnConnected(Ep());
        Tick(1);
        Assert.Single(_map.Players);

        session.OnDisconnected(Ep());
        Tick(2);

        Assert.Equal(-1, session.EntityId);
    }

    [Fact]
    public void ScenarioE_DisconnectBeforeConnect_DoesNotCrash()
    {
        // edge case — disconnect만 호출 (connect 없이). 실 운영엔 없을 케이스지만 방어.
        TestGameSession session = new(_map);

        // closing 박히지만 enqueue는 정상 (map job → RemovePlayerBySession 0건 → false)
        session.OnDisconnected(Ep());
        Tick(1);

        Assert.Empty(_map.Players);
    }

    [Fact]
    public void ScenarioF_TwoSessions_OnlyTargetSessionRemoved()
    {
        // owner 기반 cleanup 정확성 — 동시 세션 2개, 한 명만 disconnect 시 다른 사람 영향 X.
        TestGameSession s1 = new(_map);
        TestGameSession s2 = new(_map);

        s1.OnConnected(Ep());
        s2.OnConnected(Ep());
        Tick(1); // 둘 다 AddPlayer

        Assert.Equal(2, _map.Players.Count);

        s1.OnDisconnected(Ep()); // s1만 disconnect
        Tick(2);

        Assert.Single(_map.Players); // s2만 남음
        Assert.Equal(s2, _map.Players[0].Owner);
    }

    static int CountSubstring(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
