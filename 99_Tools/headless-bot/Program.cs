using System.Net;
using Dawnholder.Client.Net;
using Dawnholder.Tools.HeadlessBot;
using Dawnholder.Tools.HeadlessBot.Scenarios;

// Phase 08 봇 콘솔 entry. 인자 파싱 → 시나리오 분기 → 결과 출력 + exit code.
//
// 사용 예:
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario M2BasicMovement
//   HeadlessBot --host 127.0.0.1 --port 7777 --scenario MultiRosterSmoke
//   HeadlessBot --scenario smoke   (단순 connect 검증)

string host = "127.0.0.1";
int port = 7777;
string scenarioName = "smoke";

for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--host": host = args[i + 1]; break;
        case "--port": port = int.Parse(args[i + 1]); break;
        case "--scenario": scenarioName = args[i + 1]; break;
    }
}

Console.WriteLine($"=== HeadlessBot ===");
Console.WriteLine($"target: {host}:{port}  scenario: {scenarioName}");

// 시나리오 분기.
if (string.Equals(scenarioName, "MultiRosterSmoke", StringComparison.OrdinalIgnoreCase))
{
    MultiRosterSmoke.Result r = await MultiRosterSmoke.Run(host, port);
    Console.WriteLine($"[Bot] MultiRosterSmoke: success={r.Success} " +
                      $"entities=({r.FirstEntityId}, {r.SecondEntityId}, reconnect:{r.ReconnectEntityId})");
    Console.WriteLine($"      firstJoins={r.FirstJoinCount} secondJoins={r.SecondJoinCount} " +
                      $"secondLeaves={r.SecondLeaveCount} reconnectRoster={r.ReconnectRosterCount}");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

if (scenarioName == "M2BasicMovement")
{
    M2BasicMovement.Result r = await M2BasicMovement.Run(host, port);
    Console.WriteLine($"[Bot] M2BasicMovement: success={r.Success} " +
                      $"intents={r.IntentsSent} snapshots={r.SnapshotsReceived}");
    Console.WriteLine($"      bot=({r.BotSimFinal.X:F2},{r.BotSimFinal.Y:F2}) " +
                      $"server=({r.ServerFinal.X:F2},{r.ServerFinal.Y:F2}) " +
                      $"desync=(dx={r.FinalDesyncX:F2}, dy={r.FinalDesyncY:F2})");
    if (!r.Success) Console.WriteLine($"      reason: {r.Reason}");
    return r.Success ? 0 : 1;
}

BotSession? session = null;
ManualResetEventSlim connectedEvent = new ManualResetEventSlim(false);
ManualResetEventSlim disconnectedEvent = new ManualResetEventSlim(false);

Connector connector = new Connector();
connector.Connect(
    new IPEndPoint(IPAddress.Parse(host), port),
    sessionFactory: () =>
    {
        session = new BotSession
        {
            OnConnectedCallback = ep =>
            {
                Console.WriteLine($"[Bot] Connected to {ep}");
                connectedEvent.Set();
            },
            OnDisconnectedCallback = ep =>
            {
                Console.WriteLine($"[Bot] Disconnected from {ep}");
                disconnectedEvent.Set();
            },
            OnPacketCallback = buffer =>
            {
                // Step 2는 받은 패킷 수만 카운트 (시나리오는 Step 3에서 디코드).
                Console.WriteLine($"[Bot] recv packet: {buffer.Count} bytes");
            }
        };
        return session;
    });

// connect 5초 대기 → 못 받으면 실패 종료.
if (!connectedEvent.Wait(TimeSpan.FromSeconds(5)))
{
    Console.WriteLine("[Bot] FAIL: connect timeout (5s)");
    return 1;
}

// Step 2 smoke: 2초간 서버 S_EnterMap 등 패킷 받아보고 우아하게 종료.
await Task.Delay(TimeSpan.FromSeconds(2));

session?.Disconnect();
disconnectedEvent.Wait(TimeSpan.FromSeconds(2));

Console.WriteLine("[Bot] Done.");
return 0;
