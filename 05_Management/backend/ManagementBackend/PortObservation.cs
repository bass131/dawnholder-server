using System.Globalization;

namespace Dawnholder.Management.Backend;

internal sealed record PortObservation(bool Listening, string Owner, string? Detail)
{
    public static PortObservation Read(int port, int? ownPid)
    {
        HashSet<string> sockets = [];
        foreach (string table in new[] { "/proc/net/tcp", "/proc/net/tcp6" })
        {
            if (!File.Exists(table)) continue;
            foreach (string line in File.ReadLines(table).Skip(1))
            {
                string[] columns = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length < 10 || columns[3] != "0A") continue;
                string address = columns[1];
                int localPort = int.Parse(address.AsSpan(address.IndexOf(':') + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (localPort == port) sockets.Add($"socket:[{columns[9]}]");
            }
        }

        if (sockets.Count == 0) return new(false, "none", null);
        if (ownPid.HasValue && OwnsAny(ownPid.Value, sockets)) return new(true, "self", null);
        foreach (string directory in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(directory), out int pid) || !OwnsAny(pid, sockets)) continue;
            string command;
            try { command = File.ReadAllText(Path.Combine(directory, "comm")).Trim(); }
            catch (IOException) { command = "unknown"; }
            return new(true, "other", $"pid={pid} command={command}");
        }

        return new(true, "unknown", "포트는 대기 중이나 소유 프로세스를 읽을 수 없습니다.");
    }

    private static bool OwnsAny(int pid, HashSet<string> sockets)
    {
        try
        {
            foreach (string fd in Directory.EnumerateFileSystemEntries($"/proc/{pid}/fd"))
            {
                try
                {
                    if (new FileInfo(fd).LinkTarget is { } target && sockets.Contains(target)) return true;
                }
                catch (IOException) { } // Descriptors may close during observation.
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A process may exit or belong to another user. Do not infer ownership from its saved record.
        }

        return false;
    }
}
