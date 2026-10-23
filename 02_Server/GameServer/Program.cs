using System.Net;
using Dawnholder.Server.GameServer.Hosting;
using Dawnholder.Server.GameServer.Hosting.PersistenceRuntime;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Persistence;
using Shared.GameData;

Console.WriteLine("=== Dawnholder Server ===");
Console.WriteLine($"Tick rate: {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms)");

// Safe defaults come from the same loaded world content used for gameplay, before persistence preflight.
var mapProvider = MapDataLoader.LoadAll();
GameWorld world = new(mapProvider);
var town = world.Map.PlayerSpawnPosition;
SafeDefaults defaults = new(town.X, town.Y, PlayerStats.Knight().MaxHp, PlayerStats.Mage().MaxHp);

PersistenceRuntimeStartup? persistence;
try
{
    persistence = await PersistenceRuntimeStartup.CreateAsync(args, CancellationToken.None, defaults);
}
catch
{
    // Provider/config/credential exception text must never reach the console or client.
    Console.Error.WriteLine("Persistence startup failed. Check the approved public profile and runtime access.");
    Environment.ExitCode = 1;
    return;
}
using var persistenceLifetime = persistence;

// IPAddress.Any (= 0.0.0.0) → 모든 네트워크 인터페이스에서 listen.
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7777);

if (persistence is not null)
    world.AttachPersistence(persistence.Services);
ServerHost host = new(world, endPoint);
host.Start();

Console.WriteLine($"Listening on {endPoint}. Press Enter to stop.");
Console.ReadLine();

try
{
    host.Stop();
}
catch
{
    Console.Error.WriteLine("Server shutdown is incomplete; persistence release is not confirmed.");
    Environment.ExitCode = 1;
    return;
}
Console.WriteLine("Server stopped.");
