using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// rate-limit fail-closed 회귀 안전망.
///
/// **검증 invariant**: 1초 윈도우 안 IntentRateLimitPerSecond(=500) 초과 intent는
/// map job 큐에 진입하지 *않는다*.
///
/// **테스트 전략**:
/// - GameSession.GetMap() override로 GameMap 직접 주입 (singleton 의존 차단)
///   → ServerFixture 통합 테스트와 GameWorld.Instance 공유 race 회피
/// - CountingGameMap 서브클래스로 EnqueueJob 호출 카운트 추적
/// - OnConnected → 1 tick → AddPlayer 적용 후 _entityId 설정됨
/// - 500번 정상 enqueue, 501번 drop, 윈도우 reset 후 재개 검증
/// - Console.Out 캡처로 [Cheat] 로그 윈도우당 1회 박히는지 확증
///
/// Console.SetOut 전역 캡처 + Thread.Sleep은 병렬 flake 위험 →
/// [Collection("ConsoleSerial")]로 다른 Console-capture 테스트와 직렬화.
/// </summary>
[Collection("ConsoleSerial")]
public class GameSessionRateLimitTests : IDisposable
{
    readonly CountingGameMap _map;
    readonly TestGameSession _session;
    readonly StringWriter _consoleCapture;
    readonly TextWriter _originalOut;

    // 테스트용 GameMap — EnqueueJob 호출 카운트 추적.
    class CountingGameMap : GameMap
    {
        public int EnqueueJobCalls;

        public CountingGameMap(MapId mapId = MapId.HuntingGround) : base(mapId) { }

        public override void EnqueueJob(Action job)
        {
            EnqueueJobCalls++;
            base.EnqueueJob(job);
        }
    }

    // 테스트용 GameSession — GameMap 직접 주입 + Send/Disconnect 차단.
    // Send를 `new`로 hide하면 base에서 호출 시 base.Send가 실행됨
    // (compile-time type binding) → m_Socket NRE. Session.Send를 virtual로 → override.
    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        // handshake + class 선택 양쪽 우회 (월드 진입까지 mock).
        // HasSelectedClass=true 없으면 MoveIntentHandler에서 class 선택 전 drop → rate-limit 카운트 X.
        public override void OnConnected(EndPoint endPoint)
        {
            CompleteHandshakeAndEnter();   // _handshakeCompleted = true
            SetCharacterClass(0);           // HasSelectedClass = true (Warrior)
            EnterGameWorldIfReady();        // → EnterGameWorld() 호출
        }

        public override void Send(ArraySegment<byte> _) { /* skip — socket I/O 차단 */ }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { /* skip */ }
    }

    public GameSessionRateLimitTests()
    {
        // rate-limit 테스트는 enemy 불필요. Town(빈 맵) → player=entityId 1 (enemy 없음).
        _map = new CountingGameMap(MapId.Town);
        _session = new TestGameSession(_map);
        _consoleCapture = new StringWriter();
        _originalOut = Console.Out;
        Console.SetOut(_consoleCapture);

        // OnConnected → AddPlayer job enqueue + tick으로 _entityId 설정
        _session.OnConnected(new IPEndPoint(IPAddress.Loopback, 0));
        _map.Tick(1);
        // baseline reset — 이후 intent의 EnqueueJob 호출만 카운트
        _map.EnqueueJobCalls = 0;
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
    }

    // C_MoveIntent buffer 생성 헬퍼.
    static ArraySegment<byte> MakeMoveIntent(byte inputBits, uint clientTick)
    {
        C_MoveIntent pkt = new C_MoveIntent { input = inputBits, clientTick = clientTick };
        return pkt.Write();
    }

    [Fact]
    public void Case_H_500_Intents_AllEnqueued()
    {
        // 윈도우 안 500개까지는 모두 통과 (정확히 임계와 같음).
        for (int i = 0; i < 500; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(i + 1))); // +1 input

        Assert.Equal(500, _map.EnqueueJobCalls);
        Assert.DoesNotContain("[Cheat]", _consoleCapture.ToString());
    }

    [Fact]
    public void Drop_PreventsQueueGrowthBeyondRateLimit()
    {
        // rate-limit drop된 intent는 입력 큐에 진입하지 않는다.
        // 500개 enqueue 후 501번째 drop → EnqueueJobCalls 불변 확인.
        for (int i = 0; i < 500; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(i + 1))); // +1

        int callsAfter500 = _map.EnqueueJobCalls;

        // 501번째: rate-limit drop
        _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, 501));

        // EnqueueJob 추가 호출 없음 — drop됐으면 tick thread에 job 안 들어감.
        Assert.Equal(callsAfter500, _map.EnqueueJobCalls);
    }

    [Fact]
    public void Case_I_501st_Intent_Dropped_LoggedOnce()
    {
        // 500까지 enqueue
        for (int i = 0; i < 500; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(i + 1)));

        // 501번째: drop
        _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, 501)); // -1 input
        Assert.Equal(500, _map.EnqueueJobCalls); // 증가 X
        Assert.Contains("[Cheat]", _consoleCapture.ToString());
        Assert.Contains("dropping intent", _consoleCapture.ToString());

        // 502~510: drop, 추가 로그 X (윈도우당 1회).
        int logCountBefore = CountSubstring(_consoleCapture.ToString(), "[Cheat]");
        for (int i = 0; i < 10; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(502 + i)));
        Assert.Equal(500, _map.EnqueueJobCalls); // 여전히 500
        Assert.Equal(logCountBefore, CountSubstring(_consoleCapture.ToString(), "[Cheat]"));
    }

    [Fact]
    public void Case_J_Window_Resets_After_1Second()
    {
        // 윈도우 1: 600 intent → 500 enqueue + 1 [Cheat] log + 100 drop
        for (int i = 0; i < 600; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(i + 1)));
        Assert.Equal(500, _map.EnqueueJobCalls);
        Assert.Equal(1, CountSubstring(_consoleCapture.ToString(), "[Cheat]"));

        // 1.1초 대기 → 윈도우 리셋 시점 도달 (다음 intent가 reset 트리거)
        Thread.Sleep(1100);

        // 윈도우 2: 다시 600 intent → 새 윈도우에서 500 enqueue + 1 [Cheat] log + 100 drop
        // (윈도우 리셋이 첫 intent의 ElapsedMilliseconds 체크에서 일어남)
        for (int i = 0; i < 600; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(601 + i)));

        Assert.Equal(1000, _map.EnqueueJobCalls); // 500 + 500

        // 두 윈도우 각각 1번씩 로그 = 총 2번 (윈도우 리셋 invariant 증명)
        Assert.Equal(2, CountSubstring(_consoleCapture.ToString(), "[Cheat]"));
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
