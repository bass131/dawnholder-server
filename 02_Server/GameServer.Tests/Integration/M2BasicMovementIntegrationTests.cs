using System.Net;
using System.Net.Sockets;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;
using Dawnholder.Tools.HeadlessBot.Scenarios;

namespace Dawnholder.Server.GameServer.Tests.Integration;

// Phase 08 Step 4: M2 회귀 안전망 — in-process 서버 spawn + 봇 호출 + 안정성·p99 검증.
//
// **포트 전략**: 매 fixture 인스턴스마다 OS가 free port 할당 (port 0 bind 후 실제 포트 추출).
// Listener.Stop 없음 → 테스트 process 종료 시 GC가 socket 정리. Fixture 1회 spawn.
//
// **시나리오 크기 절충**: 정의 파일 "100회 반복"은 1000 intent×100회=87분 비현실.
// 자동 테스트 = 50 intent×10회 (~25초). 100회 풀스케일 회귀는 별도 [Trait("Category","LongRunning")]
// + 수동 트리거 (`dotnet test --filter "Category=LongRunning"`).
//
// M4.1 Phase 06 (7단계): ICollectionFixture로 전환.
// LagSimIntegrationTests도 동일 서버 인스턴스를 공유 → GameWorld 싱글톤 위반 방지.
// Collection "IntegrationTests" = 모든 통합 테스트 sequential 실행 + 서버 1회 spawn.

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<ServerFixture> { }

public class ServerFixture : IDisposable
{
    public int Port { get; }
    public GameWorld World { get; }
    public Listener Listener { get; }

    public ServerFixture()
    {
        // OS에게 free port 받기 (0 bind → 실제 port 추출 → close → 같은 port 재사용).
        TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        Port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        IPEndPoint endPoint = new(IPAddress.Loopback, Port);
        Listener = new Listener();
        Listener.Init(endPoint, () => new GameSession());

        World = new GameWorld();
        World.Start();
    }

    public void Dispose()
    {
        World.Stop();
        // Listener.Stop이 없어서 socket은 GC에 의존 (process 종료 시 정리).
    }
}

[Collection("IntegrationTests")]
public class M2BasicMovementIntegrationTests
{
    readonly ServerFixture _server;
    const int SmokeIntents = 50;

    public M2BasicMovementIntegrationTests(ServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task Smoke_run_succeeds()
    {
        M2BasicMovement.Result r = await M2BasicMovement.Run(
            "127.0.0.1", _server.Port,
            intentCount: SmokeIntents);

        Assert.True(r.Success, r.Reason);
        Assert.Equal(SmokeIntents, r.IntentsSent);
        Assert.True(r.SnapshotsReceived > 0, "서버가 최소 1개 S_Snapshot 보내야 함");
    }

    [Fact]
    public async Task Ten_runs_all_succeed()
    {
        int failureCount = 0;
        List<string> failures = new();
        for (int i = 0; i < 10; i++)
        {
            M2BasicMovement.Result r = await M2BasicMovement.Run(
                "127.0.0.1", _server.Port,
                intentCount: SmokeIntents);
            if (!r.Success)
            {
                failureCount++;
                failures.Add($"run #{i}: {r.Reason}");
            }
            // 다음 run 전 짧은 휴식 (서버가 leave 처리 + 마지막 snapshot flush).
            await Task.Delay(200);
        }
        Assert.True(failureCount == 0,
            $"10회 중 {failureCount}회 실패:\n  " + string.Join("\n  ", failures));
    }

    [Fact]
    public async Task Tick_p99_under_threshold_during_run()
    {
        // PRD: tick p99 < 10ms (M2 1봇 + 1 Unity 클라 환경, M8 부하 100명은 별도).
        List<TickMetrics.Stats> snapshots = new();
        void Capture(TickMetrics.Stats s) => snapshots.Add(s);

        _server.World.Scheduler.OnMetricsSnapshot += Capture;
        try
        {
            // 1봇 시뮬 = 50 intent × 50ms = 2.5초 → 약 2~3개 1초 snapshot 모임.
            await M2BasicMovement.Run("127.0.0.1", _server.Port, intentCount: SmokeIntents);
        }
        finally
        {
            _server.World.Scheduler.OnMetricsSnapshot -= Capture;
        }

        Assert.True(snapshots.Count >= 2, $"최소 2 snapshot 필요 (받은 수={snapshots.Count})");
        double worstP99 = snapshots.Max(s => s.P99Ms);
        Assert.True(worstP99 < 10.0,
            $"tick p99 = {worstP99:F2}ms — PRD 10ms 기준 초과. " +
            $"snapshots: [{string.Join(", ", snapshots.Select(s => s.Format()))}]");
    }

    [Fact(Skip = "LongRunning: 100회 풀스케일 회귀 — 수동 트리거. `dotnet test --filter HundredRuns`로 명시 실행.")]
    public async Task Hundred_runs_all_succeed()
    {
        int failureCount = 0;
        List<string> failures = new();
        for (int i = 0; i < 100; i++)
        {
            M2BasicMovement.Result r = await M2BasicMovement.Run(
                "127.0.0.1", _server.Port,
                intentCount: SmokeIntents);
            if (!r.Success)
            {
                failureCount++;
                failures.Add($"run #{i}: {r.Reason}");
            }
            await Task.Delay(200);
        }
        Assert.True(failureCount == 0,
            $"100회 중 {failureCount}회 실패:\n  " + string.Join("\n  ", failures));
    }
}
