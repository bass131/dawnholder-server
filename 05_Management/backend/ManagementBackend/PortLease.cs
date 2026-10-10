using System.ComponentModel;
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

    public void Dispose() => _handle.Dispose();

    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags, uint mode);

    [DllImport("libc", SetLastError = true)]
    private static extern int flock(int descriptor, int operation);
}
