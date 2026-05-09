using Shared.GameData;

Console.WriteLine("Hello, Dawnholder!");
Console.WriteLine($"Server tick rate (from shared): {Constants.ServerTickRate} TPS ({Constants.TickIntervalMs}ms 간격)");
Console.WriteLine($"Server starting at {DateTime.UtcNow:o}");
Console.WriteLine("Press Enter to exit.");
Console.ReadLine();
