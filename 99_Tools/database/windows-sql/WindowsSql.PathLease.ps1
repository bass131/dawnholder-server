# Windows directory handles deny rename/delete while reviewed inputs and private receipts are in use.
function Open-WindowsSqlDirectoryLease {
    param([string[]]$Paths)
    if (-not ('Dawnholder.WindowsSqlDirectoryLease' -as [type])) {
        Add-Type -TypeDefinition @'
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace Dawnholder
{
    public sealed class WindowsSqlDirectoryLease : IDisposable
    {
        readonly List<SafeFileHandle> handles = new();
        [StructLayout(LayoutKind.Sequential)]
        struct Info
        {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME Created, Accessed, Written;
            public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security,
            uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetFileInformationByHandle(SafeFileHandle handle, out Info info);
        public WindowsSqlDirectoryLease(string[] paths)
        {
            try
            {
                SortedSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
                foreach (string path in paths)
                {
                    string? directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                    while (directory != null)
                    {
                        directories.Add(directory);
                        directory = Path.GetDirectoryName(directory);
                    }
                }
                foreach (string directory in directories)
                {
                    // GENERIC_READ participates in delete sharing; READ_ATTRIBUTES alone does not.
                    // READ|WRITE sharing, no DELETE; OPEN_EXISTING; BACKUP|OPEN_REPARSE.
                    SafeFileHandle handle = CreateFileW(directory, 0x80000000, 3, IntPtr.Zero, 3,
                        0x02200000, IntPtr.Zero);
                    handles.Add(handle);
                    if (handle.IsInvalid || !GetFileInformationByHandle(handle, out Info info) ||
                        (info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0)
                        throw new IOException("WINDOWS_SQL_PATH_LEASE_FAILED");
                }
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            foreach (SafeFileHandle handle in handles) handle.Dispose();
            handles.Clear();
        }
    }
}
'@ -ErrorAction Stop
    }
    return [Dawnholder.WindowsSqlDirectoryLease]::new($Paths)
}
