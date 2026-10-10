using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// One test's temporary root under TMPDIR (run-tests.sh points it at the Linux cache, so flock and modes are real).
/// Deleted on dispose.
/// </summary>
internal sealed class ScratchDirectory : IDisposable
{
    public ScratchDirectory(string purpose)
    {
        Root = Path.Combine(Path.GetTempPath(), "dawnholder-management-tests", $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Create(string relative)
    {
        string path = Path.Combine(Root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}

/// <summary>Free loopback TCP ports for the stub, never the live server port or ports other parts reserved.</summary>
internal static class FreePort
{
    // 7777 is the live game server; 14333 is Core's database container test port (goal, msg_27c6883a8b34).
    static readonly HashSet<int> Reserved = [7777, 14333];
    static readonly HashSet<int> Handed = [];

    public static bool IsReserved(int port) => Reserved.Contains(port);

    public static int Next()
    {
        lock (Handed)
        {
            while (true)
            {
                TcpListener probe = new(IPAddress.Loopback, 0);
                probe.Start();
                int port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                if (!Reserved.Contains(port) && Handed.Add(port))
                {
                    return port;
                }
            }
        }
    }
}
