using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Dawnholder.Management.Backend;

internal sealed class PortLease : IDisposable
{
    private readonly SafeFileHandle _handle;

    private PortLease(SafeFileHandle handle) => _handle = handle;

    public static PortLease? TryAcquire(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // O_RDWR | O_CREAT | O_CLOEXEC, deliberately no O_TRUNC and no unlink on release.
        // The explicit flock interoperates with the development helper's util-linux flock.
        int descriptor = open(path, 2 | 64 | 0x80000, 0x180);
        if (descriptor < 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "포트 잠금 파일 열기 실패");
        SafeFileHandle handle = new((IntPtr)descriptor, ownsHandle: true);
        if (flock(descriptor, 2 | 4) == 0) return new PortLease(handle);
        int error = Marshal.GetLastPInvokeError();
        handle.Dispose();
        if (error == 11) return null;
        throw new Win32Exception(error, "포트 잠금 획득 실패");
    }

    public static bool IsHeldByOther(string path)
    {
        // statx observes identity without opening the file: even a managed read handle can take an advisory lock.
        // This is a status snapshot only; starting a server still requires TryAcquire.
        if (statx(-100, path, 0, 0x100, out FileIdentity identity) != 0) // AT_FDCWD, STATX_INO
        {
            int error = Marshal.GetLastPInvokeError();
            if (error is 2 or 20) return false; // Missing file or parent directory.
            throw new Win32Exception(error, "포트 잠금 파일 관측 실패");
        }

        foreach (string line in File.ReadLines("/proc/locks"))
        {
            if (MatchesLock(line, identity)) return true;
        }

        // A short-lived `flock 9` child leaves the lock on its parent's open file description.
        // After that child exits, /proc/locks can omit it; the surviving holder's fdinfo still records it.
        foreach (string process in Directory.EnumerateDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(process), out int pid) || pid == Environment.ProcessId) continue;
            try
            {
                foreach (string descriptor in Directory.EnumerateFiles(Path.Combine(process, "fdinfo")))
                {
                    try
                    {
                        foreach (string line in File.ReadLines(descriptor))
                        {
                            if (line.StartsWith("lock:", StringComparison.Ordinal) && MatchesLock(line[5..], identity))
                                return true;
                        }
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                    {
                        // Descriptors may close between directory enumeration and reading fdinfo.
                    }
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Other users and exiting processes are not observable; do not fail the status request.
            }
        }

        return false;
    }

    public void Dispose() => _handle.Dispose();

    private static bool MatchesLock(string line, FileIdentity identity)
    {
        string[] columns = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        // Blocked requests contain an extra '->' column; they do not own a lock.
        if (columns.Length < 8 || columns[1] != "FLOCK") return false;
        string[] file = columns[5].Split(':');
        return file.Length == 3
            && uint.TryParse(file[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint major)
            && uint.TryParse(file[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint minor)
            && ulong.TryParse(file[2], NumberStyles.None, CultureInfo.InvariantCulture, out ulong inode)
            && major == identity.DeviceMajor && minor == identity.DeviceMinor && inode == identity.Inode
            && columns[4] != Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags, uint mode);

    [DllImport("libc", SetLastError = true)]
    private static extern int flock(int descriptor, int operation);

    [DllImport("libc", SetLastError = true)]
    private static extern int statx(int directory, string path, int flags, uint mask, out FileIdentity identity);

    // Linux statx has a fixed 256-byte ABI, unlike the architecture-dependent struct stat.
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct FileIdentity
    {
        [FieldOffset(0x20)] public ulong Inode;
        [FieldOffset(0x88)] public uint DeviceMajor;
        [FieldOffset(0x8c)] public uint DeviceMinor;
    }
}
