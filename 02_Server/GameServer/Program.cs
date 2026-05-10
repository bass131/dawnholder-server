using System.Net;
using Dawnholder.Server.Network;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

Console.WriteLine("=== Dawnholder Server ===");
Console.WriteLine($"Tick rate: {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms)");

// IPAddress.Any (= 0.0.0.0) → 모든 네트워크 인터페이스에서 listen.
// 같은 머신 안 (loopback)도 LAN의 다른 머신도 다 들어옴.
// 클라(Unity)는 같은 머신이면 127.0.0.1, LAN 시연 시엔 서버 LAN IP 입력.
IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7777);

Listener listener = new Listener();
listener.Init(endPoint, () => new GameSession());

Console.WriteLine($"Listening on {endPoint}. Press Enter to stop.");
Console.ReadLine();
