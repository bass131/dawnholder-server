using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>Signals and /proc observations of processes the tests did not necessarily start themselves.</summary>
internal static class LinuxProcess
{
    public const int SigKill = 9;
    public const int SigTerm = 15;

    const int NoSuchProcess = 3;

    /// <summary>Sends a signal; a process that is already gone is not an error.</summary>
    public static void Signal(int pid, int signal)
    {
        if (kill(pid, signal) != 0 && Marshal.GetLastPInvokeError() != NoSuchProcess)
        {
            throw new InvalidOperationException($"kill({pid}, {signal}) failed with errno {Marshal.GetLastPInvokeError()}.");
        }
    }

    /// <summary>Running or sleeping; a zombie waiting for its parent to reap it counts as exited.</summary>
    public static bool IsAlive(int pid)
    {
        string stat;
        try
        {
            stat = File.ReadAllText($"/proc/{pid}/stat");
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            return false;
        }

        char state = stat[(stat.LastIndexOf(')') + 2)..][0];
        return state is not ('Z' or 'X');
    }

    public static string? CommandLine(int pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/cmdline").Replace('\0', ' ').Trim();
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            return null;
        }
    }

    public static async Task<bool> WaitForExitAsync(int pid, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (IsAlive(pid))
        {
            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(50);
        }

        return true;
    }

    /// <summary>TCP sockets in LISTEN state whose inode is an open descriptor of <paramref name="pid"/>.</summary>
    public static IReadOnlyList<ListeningSocket> ListeningSocketsOf(int pid)
    {
        HashSet<long> inodes = [];
        foreach (string descriptor in Directory.EnumerateFileSystemEntries($"/proc/{pid}/fd"))
        {
            string? target;
            try
            {
                target = new FileInfo(descriptor).LinkTarget;
            }
            catch (IOException)
            {
                continue; // closed while the directory was being read
            }

            if (target != null && target.StartsWith("socket:[", StringComparison.Ordinal))
            {
                inodes.Add(long.Parse(target["socket:[".Length..^1], CultureInfo.InvariantCulture));
            }
        }

        return [.. AllListeningSockets().Where(socket => inodes.Contains(socket.Inode))];
    }

    public static IReadOnlyList<ListeningSocket> ListeningSocketsOnPort(int port) =>
        [.. AllListeningSockets().Where(socket => socket.Port == port)];

    static IEnumerable<ListeningSocket> AllListeningSockets()
    {
        foreach (string table in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
        {
            if (!File.Exists(table))
            {
                continue;
            }

            foreach (string line in File.ReadLines(table).Skip(1))
            {
                // sl local_address rem_address st tx_queue:rx_queue tr:tm->when retrnsmt uid timeout inode
                string[] columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length < 10 || columns[3] != "0A")
                {
                    continue;
                }

                string[] local = columns[1].Split(':');
                yield return new ListeningSocket(
                    DecodeAddress(local[0]),
                    int.Parse(local[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    long.Parse(columns[9], CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>/proc/net stores each 32-bit word of the address in host (little-endian) order.</summary>
    static IPAddress DecodeAddress(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int word = 0; word < hex.Length / 8; word++)
        {
            uint value = uint.Parse(hex.AsSpan(word * 8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            BitConverter.GetBytes(value).CopyTo(bytes, word * 4);
        }

        return new IPAddress(bytes);
    }

    [DllImport("libc", SetLastError = true)]
    static extern int kill(int pid, int sig);
}

internal sealed record ListeningSocket(IPAddress Address, int Port, long Inode);
