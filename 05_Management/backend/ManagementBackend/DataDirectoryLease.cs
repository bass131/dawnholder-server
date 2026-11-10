using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Dawnholder.Management.Backend;

/// <summary>
/// One backend owns a data directory for its whole life: its run records, logs and connection file are not shared.
/// The owner holds an exclusive flock on <c>&lt;dataDirectory&gt;/backend.lock</c> until it is disposed; the kernel releases
/// it when the process dies, so a killed owner gives the directory up at once.
/// </summary>
internal sealed class DataDirectoryLease : IDisposable
{
    private readonly SafeFileHandle _handle;

    private DataDirectoryLease(SafeFileHandle handle) => _handle = handle;

    /// <summary>Returns null when another backend owns the directory; other failures throw.</summary>
    public static DataDirectoryLease? TryAcquire(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        string path = Path.Combine(dataDirectory, "backend.lock");
        // O_RDWR | O_CREAT | O_CLOEXEC, deliberately no O_TRUNC and no unlink on release, as PortLease.
        // O_CLOEXEC keeps the lock out of game server children: a server left behind by a killed owner must not own the directory.
        int descriptor = open(path, 2 | 64 | 0x80000, 0x180);
        if (descriptor < 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "데이터 폴더 잠금 파일 열기 실패");
        SafeFileHandle handle = new((IntPtr)descriptor, ownsHandle: true);
        if (flock(descriptor, 2 | 4) == 0) return new DataDirectoryLease(handle); // LOCK_EX | LOCK_NB
        int error = Marshal.GetLastPInvokeError();
        handle.Dispose();
        if (error == 11) return null; // EWOULDBLOCK: another backend holds it.
        throw new Win32Exception(error, "데이터 폴더 잠금 획득 실패");
    }

    public void Dispose() => _handle.Dispose();

    // Same libc calls as PortLease, declared again because its imports are private and PortLease owns a different thing.
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags, uint mode);

    [DllImport("libc", SetLastError = true)]
    private static extern int flock(int descriptor, int operation);
}
