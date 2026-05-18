using System.Net;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;
using Shared.Protocol;

namespace GameServer.Tests.Network;

/// <summary>
/// Phase 09 (M2.5 Trust-boundary): rate-limit fail-closed 회귀 안전망.
///
/// **검증 invariant**: 1초 윈도우 안 IntentRateLimitPerSecond(=500) 초과 intent는
/// map job 큐에 진입하지 *않는다*. 이전엔 로그만 찍고 진행 (γ 감사 위반 1순위).
///
/// **테스트 전략**:
/// - GameSession.GetMap() override로 GameMap 직접 주입 (singleton 의존 차단)
///   → ServerFixture 통합 테스트와 GameWorld.Instance 공유 race 회피
/// - CountingGameMap 서브클래스로 EnqueueJob 호출 카운트 추적
/// - OnConnected → 1 tick → AddPlayer 적용 후 _entityId 설정됨
/// - 500번 정상 enqueue, 501번 drop, 윈도우 reset 후 재개 검증
/// - Console.Out 캡처로 [Cheat] 로그 윈도우당 1회 박히는지 확증
///
/// **Codex β 권장(Phase 09)**: Console.SetOut 전역 캡처 + Thread.Sleep은 병렬 flake 위험 →
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
        public override void EnqueueJob(Action job)
        {
            EnqueueJobCalls++;
            base.EnqueueJob(job);
        }
    }

    // 테스트용 GameSession — GameMap 직접 주입 + Send/Disconnect 차단.
    // Codex β 검토(Phase 09): Send를 `new`로 hide하면 base에서 호출 시 base.Send가 실행됨
    // (compile-time type binding) → m_Socket NRE. 정정: Session.Send를 virtual로 → override.
    class TestGameSession : GameSession
    {
        readonly GameMap _injectedMap;
        public TestGameSession(GameMap map) { _injectedMap = map; }
        protected override GameMap GetMap() => _injectedMap;

        public override void Send(ArraySegment<byte> _) { /* skip — socket I/O 차단 */ }
        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { /* skip */ }
    }

    public GameSessionRateLimitTests()
    {
        _map = new CountingGameMap();
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
    public void Drop_PreventsEntityStateChange_AfterTick()
    {
        // Codex β 추가 권장(Phase 09): drop된 intent가 *실제로* entity state를 안 바꾸는지
        // tick 결과로 검증. enqueue 카운트만 보는 게 아니라 invariant 끝까지 추적.
        //
        // 시나리오:
        // 1. 500개 +1 input → enqueue
        // 2. tick → entity.Position.X += MoveSpeed * TickDuration (PendingInputX 마지막 값=+1 적용)
        // 3. entity.PendingInputX는 tick 후 0 리셋
        // 4. 501번째 -1 input → drop
        // 5. tick → PendingInputX는 0(이전 tick에서 리셋)이라 Position 변경 X
        //    만약 drop이 안 됐다면 501번째 -1이 PendingInputX 박혀서 좌측 이동했을 것
        for (int i = 0; i < 500; i++)
            _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, (uint)(i + 1))); // +1

        _map.Tick(2); // 첫 tick은 ctor에서 Tick(1) — 본 tick은 2번째
        PlayerEntity? entity = _map.GetPlayer(1);
        Assert.NotNull(entity);
        float posAfterFirstTick = entity!.Position.X;
        Assert.True(posAfterFirstTick > 0, $"+1 input 적용 안 됨 — Position.X={posAfterFirstTick}");

        // 501번째 drop
        _session.OnRecvPacket(MakeMoveIntent(0b00_0_0_0_010, 501)); // -1

        _map.Tick(3);
        // drop됐으면 PendingInputX는 0 (이전 tick에서 리셋된 상태). Position 변경 X.
        // 만약 drop 안 됐으면 PendingInputX = -1 적용 → Position 좌측 이동.
        Assert.Equal(posAfterFirstTick, entity.Position.X);
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
