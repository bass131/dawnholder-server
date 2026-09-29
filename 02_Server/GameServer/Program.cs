using System.Net;
using Dawnholder.Server.GameServer.Hosting;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Shared.GameData;

Console.WriteLine("=== Dawnholder Server ===");
Console.WriteLine($"Tick rate: {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms)");

// 맵 바이너리 로드 — startup 1회. 플레이 맵 파일 부재 시 hard error (fail loud, 헌법 #3 정합).
var mapProvider = MapDataLoader.LoadAll();

// IPAddress.Any (= 0.0.0.0) → 모든 네트워크 인터페이스에서 listen.
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7777);

GameWorld world = new GameWorld(mapProvider);
using ServerHost host = new(world, endPoint);
host.Start();

Console.WriteLine($"Listening on {endPoint}. Press Enter to stop.");
Console.ReadLine();

host.Stop();
Console.WriteLine("Server stopped.");
