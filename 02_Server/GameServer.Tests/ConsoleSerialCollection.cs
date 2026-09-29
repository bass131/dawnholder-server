namespace GameServer.Tests;

// Console.SetOut is process-global; isolate capture/disposal from every other collection's logging.
[CollectionDefinition("ConsoleSerial", DisableParallelization = true)]
public sealed class ConsoleSerialCollection
{
}
