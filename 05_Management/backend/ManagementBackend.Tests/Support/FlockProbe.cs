using System.Runtime.InteropServices;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Meets a lock file at arbitrary moments, as a development helper's `flock -n` would. A dedicated thread tries
/// LOCK_EX|LOCK_NB in a tight loop on its own open file description and releases every success at once, so any other
/// holder, even for microseconds, makes attempts fail. The file is opened with open(2): a FileStream would add .NET's own
/// advisory lock and hold the file itself.
/// </summary>
internal sealed class FlockProbe : IDisposable
{
    const int ReadWrite = 2;
    const int Create = 64;
    const int CloseOnExec = 0x80000;
    const uint OwnerReadWrite = 384; // 0600
    const int LockExclusive = 2;
    const int LockNonBlocking = 4;
    const int Unlock = 8;
    const int WouldBlock = 11;

    readonly int _descriptor;
    readonly Thread _thread;
    volatile bool _stopping;
    bool _closed;
    long _attempts;
    long _busy;
    int _unexpectedErrno;

    FlockProbe(int descriptor)
    {
        _descriptor = descriptor;
        _thread = new Thread(Run) { IsBackground = true, Name = "flock probe" };
        _thread.Start();
    }

    public static FlockProbe Start(string lockFile)
    {
        int descriptor = open(lockFile, ReadWrite | Create | CloseOnExec, OwnerReadWrite);
        if (descriptor < 0)
        {
            throw new InvalidOperationException($"open({lockFile}) failed with errno {Marshal.GetLastPInvokeError()}.");
        }

        return new FlockProbe(descriptor);
    }

    /// <summary>Stops the loop and counts its attempts and the attempts that found the lock held by someone else.</summary>
    public FlockProbeCount Stop()
    {
        _stopping = true;
        _thread.Join();
        Assert.True(_unexpectedErrno == 0, $"flock probe failed with errno {_unexpectedErrno}");
        return new FlockProbeCount(_attempts, _busy);
    }

    public void Dispose()
    {
        _stopping = true;
        _thread.Join();
        if (!_closed)
        {
            _closed = true;
            close(_descriptor);
        }
    }

    [DllImport("libc", SetLastError = true)]
    static extern int open(string path, int flags, uint mode);

    [DllImport("libc", SetLastError = true)]
    static extern int flock(int descriptor, int operation);

    [DllImport("libc", SetLastError = true)]
    static extern int close(int descriptor);

    void Run()
    {
        while (!_stopping)
        {
            _attempts++;
            if (flock(_descriptor, LockExclusive | LockNonBlocking) == 0)
            {
                flock(_descriptor, Unlock);
                continue;
            }

            int errno = Marshal.GetLastPInvokeError();
            if (errno != WouldBlock)
            {
                _unexpectedErrno = errno;
                return;
            }

            _busy++;
        }
    }
}

/// <summary>Attempts a <see cref="FlockProbe"/> made and how many of them found the lock held by someone else.</summary>
internal sealed record FlockProbeCount(long Attempts, long Busy);
