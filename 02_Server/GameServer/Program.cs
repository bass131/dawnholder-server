using System.Net;
using Dawnholder.Server.Network;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Loop;
using Shared.GameData;

Console.WriteLine("=== Dawnholder Server ===");
Console.WriteLine($"Tick rate: {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms)");

// IPAddress.Any (= 0.0.0.0) → 모든 네트워크 인터페이스에서 listen.
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7777);

Listener listener = new Listener();
listener.Init(endPoint, () => new GameSession());

GameWorld world = new GameWorld();
world.Start();

Console.WriteLine($"Listening on {endPoint}. Press Enter to stop.");
Console.ReadLine();

world.Stop();
Console.WriteLine("Server stopped.");
