using System.Net;
using System.Net.Sockets;
using Dawnholder.Management.GameServerStub;

// Like 02_Server/GameServer/Program.cs, the stub listens on one TCP port and stops when a line arrives on
// standard input or when standard input closes. It binds 127.0.0.1 only so tests never open a LAN port.
StubOptions options;
try
{
    options = StubOptions.FromEnvironment();
}
catch (FormatException error)
{
    Console.Error.WriteLine($"GameServerStub configuration error: {error.Message}");
    return 64;
}

if (options.PidDirectory != null)
{
    File.WriteAllText(Path.Combine(options.PidDirectory, $"{Environment.ProcessId}.pid"), $"{Environment.ProcessId}\n");
}

Console.WriteLine($"GameServerStub mode={options.Mode} pid={Environment.ProcessId}");
string markerPath = Path.Combine(AppContext.BaseDirectory, "release-marker.txt");
if (File.Exists(markerPath))
{
    Console.WriteLine($"release-marker: {File.ReadAllText(markerPath).Trim()}");
}

if (options.Mode == StubMode.ExitBeforeListen)
{
    Console.Error.WriteLine($"exiting before listening with code {options.ExitCode}");
    return options.ExitCode;
}

// A stub orphaned by a crashed backend or test must not linger past the test run.
_ = ExitAfterAsync(options.MaxLifetime, 70, "max lifetime reached");

await Task.Delay(options.ListenDelay);
TcpListener? listener = null;
if (options.Mode != StubMode.NoListen)
{
    listener = new TcpListener(IPAddress.Loopback, options.Port);
    try
    {
        listener.Start();
    }
    catch (SocketException error)
    {
        Console.Error.WriteLine($"cannot listen on 127.0.0.1:{options.Port}: {error.Message}");
        return 65;
    }

    Console.WriteLine($"listening on 127.0.0.1:{options.Port}");
    _ = AcceptAndCloseAsync(listener);
}

await WriteOutputAsync(options);

if (options.Mode == StubMode.Crash)
{
    _ = ExitAfterAsync(options.CrashAfter, options.ExitCode, $"crashing with code {options.ExitCode}");
}

if (options.Mode == StubMode.NoListen)
{
    // A real server stuck before listening never reaches Console.ReadLine, so Enter and EOF go unread.
    await Task.Delay(Timeout.Infinite);
}

string? input;
while (true)
{
    input = Console.ReadLine();
    if (options.Mode != StubMode.IgnoreEnter)
    {
        break;
    }

    if (input == null)
    {
        await Task.Delay(Timeout.Infinite);
    }

    Console.WriteLine("ignoring stop request");
}

Console.WriteLine(input == null ? "standard input closed" : "stop requested");
await Task.Delay(options.ExitDelay);
listener?.Stop();
Console.WriteLine("stopped");
return 0;

static async Task ExitAfterAsync(TimeSpan delay, int exitCode, string reason)
{
    await Task.Delay(delay);
    Console.Error.WriteLine(reason);
    Environment.Exit(exitCode);
}

static async Task AcceptAndCloseAsync(TcpListener listener)
{
    try
    {
        while (true)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
        }
    }
    catch (Exception error) when (error is SocketException or ObjectDisposedException or InvalidOperationException)
    {
        // The listener was stopped on the way out.
    }
}

static async Task WriteOutputAsync(StubOptions options)
{
    foreach (StubOutputLine line in options.Output)
    {
        TextWriter writer = line.Stream == "stderr" ? Console.Error : Console.Out;
        for (int index = 1; index <= line.Count; index++)
        {
            writer.WriteLine(line.Count == 1 ? line.Text : $"{line.Text} {index:D4}");
            await Task.Delay(options.LineInterval);
        }
    }
}
