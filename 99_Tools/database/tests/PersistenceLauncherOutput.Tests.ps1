#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    [string]$LauncherPath = (Join-Path $PSScriptRoot '../../Start-PersistenceServer.ps1'),
    [string]$CompilerPath = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
)

# Full, byte-identical launcher entry against an owned native Windows wsl.exe fixture, plus reader probes.
# No real WSL/SDK/server/DB/port is used. Native streams are filtered in memory before any evidence is written.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'persistence-launcher-output'
if (-not [OperatingSystem]::IsWindows()) { throw 'This fixture requires Windows process and DPAPI boundaries.' }
$launcher = (Resolve-Path -LiteralPath $LauncherPath).Path
$compiler = (Resolve-Path -LiteralPath $CompilerPath).Path
$pwsh = [Diagnostics.Process]::GetCurrentProcess().Path
$script:OutputAssertionEvidence = [Collections.Generic.List[object]]::new()
$suitePrefix = $script:SuiteRoot.TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar

function Assert-FixtureOwnedPath {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($suitePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Fixture paths, especially synthetic credentials, must belong to the new suite directory.'
    }
    $ancestor = [IO.Path]::GetDirectoryName($fullPath)
    while ($ancestor -and $ancestor.StartsWith($script:SuiteRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Fixture paths cannot traverse a reparse point.'
        }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
}

function Assert-OutputChecks {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][Collections.IDictionary]$Checks,
        [Collections.IDictionary]$Observed = @{}
    )

    $failedChecks = @($Checks.Keys | Where-Object {
            $Checks[$_] -isnot [bool] -or -not $Checks[$_]
        })
    $condition = $failedChecks.Count -eq 0
    $script:OutputAssertionEvidence.Add([pscustomobject]@{
            Name = $Name
            Checks = $Checks
            Observed = $Observed
            Condition = $condition
            FailedChecks = $failedChecks
        })
    ConvertTo-Json -InputObject $script:OutputAssertionEvidence.ToArray() -Depth 7 |
        Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'assertion-observations.json')
    $detail = if ($condition) { 'all observed conditions true' } else {
        'failed checks: ' + ($failedChecks -join ', ')
    }
    Assert-True -Name $Name -Condition $condition -Detail $detail
}

# This observer is deliberately independent of the product matcher. Unknown native text is never persisted,
# even on a failing/mutated launcher; sentinel detection continues past the bounded per-line buffer.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace DawnholderLauncherOutputTests
{
    // The same live OS handle owns identity and exit observation; no fixture-return file supplies an exit code.
    public sealed class ChildExitObservation : IDisposable
    {
        private const uint QueryLimitedInformation = 0x1000;
        private const uint Synchronize = 0x00100000;
        private const uint WaitSignaled = 0;
        private const uint WaitTimeout = 258;
        private const uint WaitFailed = 0xffffffff;
        private readonly SafeProcessHandle handle;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetProcessId(SafeProcessHandle process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessTimes(SafeProcessHandle process,
            out long creation, out long exit, out long kernel, out long user);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool QueryFullProcessImageNameW(SafeProcessHandle process,
            uint flags, StringBuilder path, ref uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint code);

        public int RequestedId { get; }
        public uint Id { get; private set; }
        public string BirthUtc { get; private set; }
        public string Executable { get; private set; }
        public string OpenedUtc { get; } = DateTime.UtcNow.ToString("o");
        public string WaitStartedUtc { get; private set; }
        public string ObservedUtc { get; private set; }
        public bool HandleAcquired { get; }
        public bool IdentityMatched { get; private set; }
        public bool CapturedLive { get; private set; }
        public bool Returned { get; private set; }
        public uint? ExitCode { get; private set; }
        public uint? WaitStatus { get; private set; }
        public int? NativeError { get; private set; }
        public string Status { get; private set; } = "missing";

        public ChildExitObservation(int id, DateTime birthUtc, string executable)
        {
            RequestedId = id;
            handle = OpenProcess(QueryLimitedInformation | Synchronize, false, id);
            HandleAcquired = !handle.IsInvalid;
            if (!HandleAcquired)
            {
                NativeError = Marshal.GetLastWin32Error();
                Status = "open-failed";
                return;
            }
            Id = GetProcessId(handle);
            if (Id == 0 || !GetProcessTimes(handle, out long birth, out _, out _, out _))
            {
                NativeError = Marshal.GetLastWin32Error();
                Status = "identity-query-failed";
                return;
            }
            var image = new StringBuilder(32768);
            uint size = (uint)image.Capacity;
            if (!QueryFullProcessImageNameW(handle, 0, image, ref size))
            {
                NativeError = Marshal.GetLastWin32Error();
                Status = "image-query-failed";
                return;
            }
            var actualBirth = DateTime.FromFileTimeUtc(birth);
            BirthUtc = actualBirth.ToString("o");
            Executable = image.ToString();
            IdentityMatched = Id == id && actualBirth.Ticks == birthUtc.ToUniversalTime().Ticks &&
                string.Equals(Executable, executable, StringComparison.OrdinalIgnoreCase);
            if (!IdentityMatched)
            {
                Status = "identity-mismatch";
                return;
            }
            WaitStatus = WaitForSingleObject(handle, 0);
            CapturedLive = WaitStatus == WaitTimeout;
            if (WaitStatus == WaitFailed)
                NativeError = Marshal.GetLastWin32Error();
            Status = CapturedLive ? "retained-live" : "not-live-at-acquisition";
        }

        public void ReadExit(uint milliseconds)
        {
            if (!IdentityMatched || !CapturedLive)
                return;
            WaitStartedUtc = DateTime.UtcNow.ToString("o");
            WaitStatus = WaitForSingleObject(handle, milliseconds);
            Returned = WaitStatus == WaitSignaled;
            if (!Returned)
            {
                if (WaitStatus == WaitFailed)
                    NativeError = Marshal.GetLastWin32Error();
                Status = WaitStatus == WaitTimeout ? "exit-timeout" : "exit-wait-failed";
                return;
            }
            if (!GetExitCodeProcess(handle, out uint code))
            {
                NativeError = Marshal.GetLastWin32Error();
                Status = "exit-query-failed";
                return;
            }
            ObservedUtc = DateTime.UtcNow.ToString("o");
            ExitCode = code;
            Status = "observed";
        }

        public void Dispose() { handle.Dispose(); }
    }

    // Test-only replacement of the owned Process stdout slot, before the unmodified product Start reads it.
    // A read/release handshake injects IOException in that actual reader path without a Dispose/EOF race.
    public sealed class ReaderFaultStream : Stream
    {
        private readonly Process process;
        private readonly FieldInfo outputSlot;
        private readonly StreamReader original;
        private readonly StreamReader replacement;
        private readonly TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long readTicks;
        private long releaseTicks;
        private long faultTicks;
        private int readCalls;
        private int faultsRaised;
        private int disposed;
        private string readApi;

        public string RuntimeSlot => outputSlot.Name;
        public string NativePipeType { get; }
        public bool StandardOutputBindingVerified { get; }
        public bool SlotRestored { get; private set; }
        public int ReadCalls => Volatile.Read(ref readCalls);
        public int FaultsRaised => Volatile.Read(ref faultsRaised);
        public string ReadApi => readApi;
        public string FaultExceptionType { get; private set; }
        public string ReadStartedUtc => FormatUtc(ref readTicks);
        public string ReleasedUtc => FormatUtc(ref releaseTicks);
        public string FaultRaisedUtc => FormatUtc(ref faultTicks);

        private static string FormatUtc(ref long value)
        {
            long ticks = Interlocked.Read(ref value);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc).ToString("o");
        }

        public ReaderFaultStream(Process ownedProcess)
        {
            process = ownedProcess;
            original = process.StandardOutput;
            outputSlot = typeof(Process).GetField("_standardOutput", BindingFlags.Instance | BindingFlags.NonPublic);
            if (outputSlot == null || outputSlot.FieldType != typeof(StreamReader) ||
                !ReferenceEquals(outputSlot.GetValue(process), original))
                throw new InvalidOperationException("Owned stdout runtime slot is unavailable; no fault was injected.");
            NativePipeType = original.BaseStream.GetType().FullName;
            replacement = new StreamReader(this, Encoding.UTF8, false, 1024, true);
            outputSlot.SetValue(process, replacement);
            StandardOutputBindingVerified = ReferenceEquals(process.StandardOutput.BaseStream, this);
            if (!StandardOutputBindingVerified)
            {
                Dispose();
                throw new InvalidOperationException("Owned stdout did not bind to the fault stream.");
            }
        }

        public bool WaitForRead(int milliseconds) { return entered.Task.Wait(milliseconds); }

        public void ReleaseFault()
        {
            if (!entered.Task.IsCompleted ||
                Interlocked.CompareExchange(ref releaseTicks, DateTime.UtcNow.Ticks, 0) != 0)
                throw new InvalidOperationException("Fault release requires exactly one observed product read.");
            released.SetResult(true);
        }

        private async Task<int> ReadFaultAsync(string api, CancellationToken cancellation)
        {
            Interlocked.Increment(ref readCalls);
            Interlocked.CompareExchange(ref readApi, api, null);
            Interlocked.CompareExchange(ref readTicks, DateTime.UtcNow.Ticks, 0);
            entered.TrySetResult(true);
            await released.Task.WaitAsync(cancellation).ConfigureAwait(false);
            var failure = new IOException("Owned stdout read fault.");
            FaultExceptionType = failure.GetType().FullName;
            Interlocked.Exchange(ref faultTicks, DateTime.UtcNow.Ticks);
            Interlocked.Increment(ref faultsRaised);
            throw failure;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadFaultAsync("Read(byte[])", CancellationToken.None).GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            return ReadFaultAsync("ReadAsync(byte[])", cancellation);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
        {
            return new ValueTask<int>(ReadFaultAsync("ReadAsync(Memory<byte>)", cancellation));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref disposed, 1) == 0)
            {
                released.TrySetCanceled();
                if (ReferenceEquals(outputSlot.GetValue(process), replacement))
                    outputSlot.SetValue(process, original);
                SlotRestored = ReferenceEquals(process.StandardOutput, original);
                replacement.Dispose();
                original.Dispose();
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }

    public sealed class SafeConsoleObserver
    {
        private readonly object gate = new();
        private readonly List<string> safeLines = new();
        private readonly Task readers;
        private long unknownLines;
        private long overlongLines;
        private bool sentinelSeen;
        private const string Sentinel = "fixture-secret-sentinel-9f27";
        private static readonly Regex lifecycle = new(
            @"^\[Persistence (stdout|stderr) \d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{7}Z\] " +
            @"(Listening on 0\.0\.0\.0:7777\. Press Enter to stop\.|Server stopped\.|" +
            @"Persistence startup failed\. Check the approved public profile and runtime access\.|" +
            @"Server shutdown is incomplete; persistence release is not confirmed\.)$");
        private static readonly Regex summary = new(
            @"^Persistence output (stdout|stderr): excluded=\d+, overlong=\d+, queueDropped=\d+, " +
            @"decodeErrors=\d+, readerErrors=\d+, drained=(True|False)\.$");

        public SafeConsoleObserver(Process process)
        {
            readers = Task.WhenAll(
                Task.Run(() => ReadAsync(process.StandardOutput)),
                Task.Run(() => ReadAsync(process.StandardError)));
        }

        private async Task ReadAsync(StreamReader reader)
        {
            var line = new StringBuilder(4096, 4096);
            var tail = new StringBuilder(Sentinel.Length);
            var buffer = new char[512];
            bool overlong = false;
            int length;
            while ((length = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                for (int index = 0; index < length; index++)
                {
                    char value = buffer[index];
                    tail.Append(value);
                    if (tail.Length > Sentinel.Length)
                        tail.Remove(0, 1);
                    if (tail.ToString() == Sentinel)
                        lock (gate) { sentinelSeen = true; }
                    if (value == '\n')
                    {
                        Accept(line, overlong);
                        line.Clear();
                        overlong = false;
                    }
                    else if (!overlong)
                    {
                        if (line.Length == 4096)
                        {
                            overlong = true;
                            line.Clear();
                        }
                        else
                            line.Append(value);
                    }
                }
            }
            if (overlong || line.Length > 0)
                Accept(line, overlong);
        }

        private void Accept(StringBuilder line, bool overlong)
        {
            string text = line.ToString().TrimEnd('\r');
            bool allowed = !overlong && (lifecycle.IsMatch(text) || summary.IsMatch(text) ||
                Regex.IsMatch(text, @"^Owned WSL process PID \d+, birth UTC \d{4}-.*Z\.$") ||
                text == "Persistence server process launched; press Enter to request normal shutdown" || text == "");
            lock (gate)
            {
                if (overlong)
                    overlongLines++;
                if (!allowed)
                    unknownLines++;
                else if (safeLines.Count < 256)
                    safeLines.Add(text);
                else
                    unknownLines++;
            }
        }

        public string[] Lines { get { lock (gate) { return safeLines.ToArray(); } } }
        public long UnknownLines { get { lock (gate) { return unknownLines; } } }
        public long OverlongLines { get { lock (gate) { return overlongLines; } } }
        public bool SentinelSeen { get { lock (gate) { return sentinelSeen; } } }
        public bool Wait(int milliseconds) { return readers.Wait(milliseconds); }
    }
}
'@

$bin = Join-Path $script:SuiteRoot 'bin'
$project = Join-Path $script:SuiteRoot 'project'
$tools = Join-Path $project '99_Tools'
foreach ($directory in @($bin, $project, $tools)) { [void][IO.Directory]::CreateDirectory($directory) }
$fixtureLauncher = Join-Path $tools 'Start-PersistenceServer.ps1'
[IO.File]::Copy($launcher, $fixtureLauncher, $false)
$originalHash = (Get-FileHash -LiteralPath $launcher -Algorithm SHA256).Hash
$entryHash = (Get-FileHash -LiteralPath $fixtureLauncher -Algorithm SHA256).Hash
Assert-Equal -Name 'whole entry is byte identical to the original launcher' -Expected $originalHash -Actual $entryHash
$profile = Join-Path $project 'public-profile.json'
[IO.File]::WriteAllText($profile, '{}')
$credentialPath = Join-Path $script:SuiteRoot 'synthetic-credential.xml'
Assert-FixtureOwnedPath -Path $credentialPath
# No credential path is accepted as a test parameter. This is a newly created dummy DPAPI file only.
$dummyPassword = ConvertTo-SecureString -String 'fixture-password-do-not-reuse' -AsPlainText -Force
try {
    $dummyCredential = [Management.Automation.PSCredential]::new('fixture-user', $dummyPassword)
    $dummyCredential | Export-Clixml -LiteralPath $credentialPath
} finally {
    $dummyPassword.Dispose()
    $dummyCredential = $null
}
$publicCa = Join-Path $script:SuiteRoot 'fixture-public-ca.pem'
[IO.File]::WriteAllText($publicCa, "-----BEGIN CERTIFICATE-----`nRklYVFVSRS1PTkxZ`n-----END CERTIFICATE-----`n")
$publicCaHash = (Get-FileHash -LiteralPath $publicCa -Algorithm SHA256).Hash
$emptyCa = Join-Path $script:SuiteRoot 'empty-ca'
[void][IO.Directory]::CreateDirectory($emptyCa)

$nativeSource = @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public static class NativeWslFixture
{
    const string Source = "/fixture/source with spaces/$literal";
    const string Clone = "/fixture/clone with spaces/$literal";
    const string Sdk = "/fixture/sdk with spaces/$literal/dotnet";
    const string Sentinel = "fixture-secret-sentinel-9f27";
    static string Root { get { return Environment.GetEnvironmentVariable("DAWNHOLDER_OUTPUT_FIXTURE_ROOT"); } }
    static string Case { get { return Environment.GetEnvironmentVariable("DAWNHOLDER_OUTPUT_FIXTURE_CASE"); } }

    static void Record(string name, object record)
    {
        string path = Path.Combine(Root, Case + "-" + name + ".json");
        File.WriteAllText(path + ".tmp",
            new JavaScriptSerializer().Serialize(record), new UTF8Encoding(false));
        File.Move(path + ".tmp", path);
    }

    static object Identity(string[] args)
    {
        using (var process = Process.GetCurrentProcess())
            return new { Id = process.Id, BirthUtc = process.StartTime.ToUniversalTime().ToString("o"),
                Executable = process.MainModule.FileName, Cwd = Environment.CurrentDirectory, Argv = args };
    }

    static bool Same(string[] actual, string[] expected)
    {
        if (actual.Length != expected.Length) return false;
        for (int i = 0; i < actual.Length; i++)
            if (actual[i] != expected[i]) return false;
        return true;
    }

    static void UnknownFlood(TextWriter writer)
    {
        for (int i = 0; i < 512; i++) writer.WriteLine("Unknown fixture line " + i + " " + Sentinel);
        writer.Flush();
    }

    static int Probe(string mode)
    {
        Record("probe-start", Identity(new[] { "--probe", mode }));
        if (mode == "reader-fault")
        {
            Thread.Sleep(1500);
            return 0;
        }
        if (mode == "inherited-pipe")
        {
            var start = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, "--holder");
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            using (var holder = Process.Start(start))
                Record("holder-start", new { Id = holder.Id,
                    BirthUtc = holder.StartTime.ToUniversalTime().ToString("o") });
            return 0;
        }
        Action<TextWriter> burst = writer => {
            for (int i = 0; i < 128; i++) writer.WriteLine("Server stopped.");
            writer.Write(new string('x', 131072));
            writer.Write(Sentinel);
            writer.WriteLine();
            writer.Flush();
        };
        Task.WaitAll(Task.Factory.StartNew(() => burst(Console.Out)),
            Task.Factory.StartNew(() => burst(Console.Error)));
        return 0;
    }

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (String.IsNullOrEmpty(Root) || String.IsNullOrEmpty(Case)) return 90;
        if (Environment.CurrentDirectory != Path.Combine(Root, "bin")) return 90;
        try
        {
            if (args.Length == 1 && args[0] == "--holder")
            {
                Thread.Sleep(8000);
                Record("holder-end", new { Returned = true });
                return 0;
            }
            if (args.Length == 2 && args[0] == "--probe") return Probe(args[1]);
            if (args.Length >= 4 && args[0] == "-d" && args[1] == "Ubuntu" && args[2] == "--exec")
            {
                string answer = null;
                if (args[3] == "wslpath")
                {
                    string file = args[args.Length - 1];
                    answer = file.EndsWith("public-profile.json") ? "/fixture/profile.json" :
                        file.EndsWith("fixture-public-ca.pem") ? "/fixture/public ca.pem" :
                        file.EndsWith("empty-ca") ? "/fixture/empty ca" : Source;
                }
                if (args[3] == "realpath") answer = args[args.Length - 1];
                if (args[3] == "cat") answer = Source;
                if (args[3] == "bash") answer = args[args.Length - 1] == "path" ? Clone : Sdk;
                if (answer == null) return 90;
                Console.WriteLine(answer);
                return 0;
            }
            var expected = new List<string>(new[] { "-d", "Ubuntu", "--cd", Clone, "--exec" });
            if (Case == "normal-ca")
                expected.AddRange(new[] { "env", "SSL_CERT_FILE=/fixture/public ca.pem", "SSL_CERT_DIR=/fixture/empty ca" });
            expected.AddRange(new[] { Sdk, Clone + "/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll",
                "--persistence-profile", "/fixture/profile.json" });
            Record("child-start", Identity(args));
            if (!Same(args, expected.ToArray())) return 92;
            using (var watchdog = new Timer(_ => Environment.Exit(91), null, 20000, Timeout.Infinite))
            {
                string envelope = Console.ReadLine();
                var fields = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(envelope);
                bool envelopeMatched = fields != null && fields.Count == 3 &&
                    fields.ContainsKey("version") && fields["version"] is int && (int)fields["version"] == 1 &&
                    fields.ContainsKey("userName") && (string)fields["userName"] == "fixture-user" &&
                    fields.ContainsKey("password") && (string)fields["password"] == "fixture-password-do-not-reuse";
                fields = null;
                envelope = null;
                Record("credential", new { EnvelopeMatched = envelopeMatched, EnvelopeLineCount = 1 });
                if (!envelopeMatched) return 94;
                if (Case == "startup-failure")
                {
                    Console.Error.WriteLine("Persistence startup failed. Check the approved public profile and runtime access.");
                    Thread.Sleep(1200);
                    return 7;
                }
                if (Case == "unknown-flood")
                    Task.WaitAll(Task.Factory.StartNew(() => UnknownFlood(Console.Out)),
                        Task.Factory.StartNew(() => UnknownFlood(Console.Error)));
                Console.Out.WriteLine(Sentinel);
                Console.Error.WriteLine(Sentinel);
                if (Case == "long-line")
                {
                    Console.Out.WriteLine(new string('x', 4096));
                    Console.Out.Write(new string('x', 131072));
                    Console.Out.Write(Sentinel);
                    Console.Out.WriteLine();
                }
                if (Case == "invalid-utf8")
                {
                    Console.Error.Flush();
                    var error = Console.OpenStandardError();
                    error.Write(new byte[] { 0xc3, 0x28, 0x0a }, 0, 3);
                    error.Flush();
                }
                if (Case == "wrong-stream") Console.Error.WriteLine("Server stopped.");
                Console.Out.WriteLine("Listening on 0.0.0.0:7777. Press Enter to stop.");
                Console.Out.Flush();
                if (Case == "exit-race")
                {
                    Console.Out.Write("Server stopped.");
                    Console.Out.Flush();
                    Thread.Sleep(1200);
                    return 0;
                }
                string stop = Console.ReadLine();
                string afterStop = Console.ReadLine();
                bool normalStop = stop == "" && afterStop == null;
                stop = null;
                afterStop = null;
                Record("shutdown", new { StopLineEmpty = normalStop, LineAfterStopIsEof = normalStop });
                if (!normalStop) return 95;
                if (Case == "shutdown-failure")
                {
                    Console.Error.WriteLine("Server shutdown is incomplete; persistence release is not confirmed.");
                    return 9;
                }
                Console.Out.WriteLine("Server stopped.");
                return 0;
            }
        }
        catch { return 93; }
    }
}
'@
$nativeSourcePath = Join-Path $script:SuiteRoot 'NativeWslFixture.cs'
$nativePath = Join-Path $bin 'wsl.exe'
[IO.File]::WriteAllText($nativeSourcePath, $nativeSource)
$compileArguments = @('/nologo', '/target:exe', ('/out:' + $nativePath),
    '/reference:System.Web.Extensions.dll', $nativeSourcePath)
$compileStart = [Diagnostics.ProcessStartInfo]::new($compiler)
$compileStart.UseShellExecute = $false
$compileStart.CreateNoWindow = $true
$compileStart.RedirectStandardOutput = $true
$compileStart.RedirectStandardError = $true
foreach ($argument in $compileArguments) { $compileStart.ArgumentList.Add($argument) }
$compileProcess = [Diagnostics.Process]::Start($compileStart)
$compileObserver = [DawnholderLauncherOutputTests.SafeConsoleObserver]::new($compileProcess)
try {
    if (-not $compileProcess.WaitForExit(30000)) { throw 'Owned native fixture compiler did not return; do not retry.' }
    $compileDrained = $compileObserver.Wait(5000)
    [ordered]@{ Compiler = $compiler; Argv = $compileArguments; ExitCode = $compileProcess.ExitCode
        Drained = $compileDrained; DiagnosticLines = $compileObserver.UnknownLines } | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'compiler.json')
    if ($compileProcess.ExitCode -ne 0 -or -not $compileDrained) { throw 'Native fixture compilation failed.' }
} finally {
    $compileProcess.Dispose()
}

function New-NativeFixtureStart {
    param([Parameter(Mandatory)][string]$Case, [string[]]$Arguments = @())

    $start = [Diagnostics.ProcessStartInfo]::new($nativePath)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WorkingDirectory = $bin
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['DAWNHOLDER_OUTPUT_FIXTURE_ROOT'] = $script:SuiteRoot
    $start.Environment['DAWNHOLDER_OUTPUT_FIXTURE_CASE'] = $Case
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    return $start
}

function Invoke-WholeLauncherFixture {
    param([Parameter(Mandatory)][string]$Case, [switch]$Eof, [switch]$EarlyExit)

    # ProcessStartInfo resolves wsl.exe in the owned cwd before System32; metadata uses the same bin first in PATH.
    # Refuse a competing executable in PowerShell's application directory rather than risk real WSL.
    if (Test-Path -LiteralPath (Join-Path (Split-Path $pwsh) 'wsl.exe')) {
        throw 'An application-directory wsl.exe would bypass the owned fixture.'
    }
    Assert-FixtureOwnedPath -Path $credentialPath
    $start = New-NativeFixtureStart -Case $Case
    $start.FileName = $pwsh
    $start.Environment['PATH'] = $bin + [IO.Path]::PathSeparator + $env:PATH
    $arguments = @('-NoLogo', '-NoProfile', '-File', $fixtureLauncher,
        '-ProfilePath', $profile, '-CredentialPath', $credentialPath)
    if ($Case -ceq 'normal-ca') {
        $arguments += @('-PublicCaPath', $publicCa, '-ExpectedPublicCaHash', $publicCaHash,
            '-EmptyCaDirectory', $emptyCa)
    }
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $observer = [DawnholderLauncherOutputTests.SafeConsoleObserver]::new($process)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $listeningBeforeInput = $false
    $returnedBeforeInput = $false
    $ownedChild = $null
    $childAcquisitionAttempted = $false
    $childObservationError = $null
    $childIdentityMatched = $false
    try {
        while (-not $process.HasExited -and $watch.Elapsed.TotalSeconds -lt 10) {
            $childStartPath = Join-Path $script:SuiteRoot ($Case + '-child-start.json')
            if (-not $childAcquisitionAttempted -and (Test-Path -LiteralPath $childStartPath)) {
                $childAcquisitionAttempted = $true
                $identity = Get-Content -LiteralPath $childStartPath -Raw | ConvertFrom-Json
                try {
                    $ownedChild = [DawnholderLauncherOutputTests.ChildExitObservation]::new(
                        [int]$identity.Id, ([datetime]$identity.BirthUtc).ToUniversalTime(), $nativePath)
                    $childIdentityMatched = $ownedChild.IdentityMatched
                } catch {
                    $childIdentityMatched = $false
                    $childObservationError = $_.Exception.GetType().FullName
                }
            }
            $listeningBeforeInput = @($observer.Lines | Where-Object {
                    $_ -cmatch '^\[Persistence stdout .+\] Listening on 0\.0\.0\.0:7777\. Press Enter to stop\.$'
                }).Count -eq 1
            if ($listeningBeforeInput -and -not $EarlyExit) { break }
            [Threading.Thread]::Sleep(25)
        }
        $returnedBeforeInput = $process.HasExited
        if (-not $process.HasExited) {
            if ($Eof) {
                $process.StandardInput.Close()
            } elseif (-not $EarlyExit) {
                $process.StandardInput.WriteLine()
                $process.StandardInput.Flush()
                $process.StandardInput.Close()
            }
        }
        $returned = $process.WaitForExit(15000)
        if (-not $returned) {
            $process.StandardInput.Close()
            $returned = $process.WaitForExit(5000)
        }
        if (-not $returned) {
            throw ('Owned launcher fixture remains uncertain, PID {0}; do not kill or retry.' -f $process.Id)
        }
        $drained = $observer.Wait(5000)
        if ($null -ne $ownedChild) { $ownedChild.ReadExit(5000) }
        $childReturned = $null -ne $ownedChild -and $childIdentityMatched -and $ownedChild.Returned
        $childStartPath = Join-Path $script:SuiteRoot ($Case + '-child-start.json')
        $credentialRecordPath = Join-Path $script:SuiteRoot ($Case + '-credential.json')
        $shutdownPath = Join-Path $script:SuiteRoot ($Case + '-shutdown.json')
        $record = [ordered]@{
            Case = $Case; Boundary = 'whole byte-identical launcher entry / native no-DB fixture'
            LauncherHash = $entryHash; Executable = $pwsh; Argv = $arguments; Cwd = $bin
            Id = $process.Id; BirthUtc = $process.StartTime.ToUniversalTime().ToString('o')
            ExitCode = $process.ExitCode; Milliseconds = $watch.Elapsed.TotalMilliseconds
            ListeningBeforeInput = $listeningBeforeInput; ReturnedBeforeInput = $returnedBeforeInput
            ObserverDrained = $drained; SentinelSeen = $observer.SentinelSeen
            ChildIdentityMatched = $childIdentityMatched; ChildReturned = $childReturned
            ChildExitCode = $(if ($null -ne $ownedChild) { $ownedChild.ExitCode } else { $null })
            ChildHandleCapturedLive = $null -ne $ownedChild -and $ownedChild.CapturedLive
            ChildExitObserved = $null -ne $ownedChild -and $ownedChild.Status -ceq 'observed'
            ChildObservation = $ownedChild; ChildObservationError = $childObservationError
            UnknownConsoleLines = $observer.UnknownLines; OverlongConsoleLines = $observer.OverlongLines
            SafeConsoleLines = $observer.Lines
            Child = $(if (Test-Path -LiteralPath $childStartPath) {
                    Get-Content -LiteralPath $childStartPath -Raw | ConvertFrom-Json
                } else { $null })
            Credential = $(if (Test-Path -LiteralPath $credentialRecordPath) {
                    Get-Content -LiteralPath $credentialRecordPath -Raw | ConvertFrom-Json
                } else { $null })
            Shutdown = $(if (Test-Path -LiteralPath $shutdownPath) {
                    Get-Content -LiteralPath $shutdownPath -Raw | ConvertFrom-Json
                } else { $null })
        }
        $record | ConvertTo-Json -Depth 7 |
            Set-Content -LiteralPath (Join-Path $script:SuiteRoot ($Case + '-entry.json'))
        return [pscustomobject]$record
    } finally {
        try { $process.StandardInput.Close() } catch { }
        if ($null -ne $ownedChild) { $ownedChild.Dispose() }
        $process.Dispose()
    }
}

$successCases = @('normal-ca', 'normal-eof', 'unknown-flood', 'wrong-stream', 'exit-race')
foreach ($case in $successCases) {
    $record = Invoke-WholeLauncherFixture -Case $case -Eof:($case -ceq 'normal-eof') `
        -EarlyExit:($case -ceq 'exit-race')
    $stopRows = @($record.SafeConsoleLines | Where-Object { $_ -cmatch '^\[Persistence stdout .+\] Server stopped\.$' })
    $streamSummaryCount = @($record.SafeConsoleLines | Where-Object {
            $_ -cmatch '^Persistence output (stdout|stderr): .+, drained=True\.$'
        }).Count
    $checks = [ordered]@{
        ChildRecordPresent = $null -ne $record.Child
        ChildExecutableMatches = $null -ne $record.Child -and $record.Child.Executable -ceq $nativePath
        ChildCwdMatches = $null -ne $record.Child -and $record.Child.Cwd -ceq $bin
        CredentialRecordPresent = $null -ne $record.Credential
        EnvelopeMatched = $null -ne $record.Credential -and $record.Credential.EnvelopeMatched
        EnvelopeLineCountOne = $null -ne $record.Credential -and $record.Credential.EnvelopeLineCount -eq 1
        LauncherExitZero = $record.ExitCode -eq 0
        ChildIdentityMatched = $record.ChildIdentityMatched
        ChildHandleCapturedLive = $record.ChildHandleCapturedLive
        ChildReturned = $record.ChildReturned
        ChildExitObserved = $record.ChildExitObserved
        ChildExitZero = $null -ne $record.ChildExitCode -and $record.ChildExitCode -eq 0
        ObserverDrained = $record.ObserverDrained
        TwoDrainedStreamSummaries = $streamSummaryCount -eq 2
        NoSentinel = -not $record.SentinelSeen
        OneStopRow = $stopRows.Count -eq 1
    }
    if ($case -ceq 'exit-race') {
        $checks.ReturnedBeforeInput = $record.ReturnedBeforeInput
    } else {
        $checks.ShutdownRecordPresent = $null -ne $record.Shutdown
        $checks.StopLineEmpty = $null -ne $record.Shutdown -and $record.Shutdown.StopLineEmpty
        $checks.LineAfterStopIsEof = $null -ne $record.Shutdown -and $record.Shutdown.LineAfterStopIsEof
    }
    Assert-OutputChecks -Name ($case + ': actual whole entry, dummy handoff and normal child lifetime') `
        -Checks $checks -Observed ([ordered]@{
            LauncherExit = $record.ExitCode; ChildExit = $record.ChildExitCode; ExpectedChildExit = 0
            ChildObservation = $record.ChildObservation; ChildObservationError = $record.ChildObservationError
            Credential = $record.Credential; Shutdown = $record.Shutdown
            DrainedStreamSummaryCount = $streamSummaryCount; StopRowCount = $stopRows.Count
        })
    if ($case -cne 'exit-race') {
        Assert-OutputChecks -Name ($case + ': lifecycle output arrives while operator input is still open') `
            -Checks ([ordered]@{
                ListeningBeforeInput = $record.ListeningBeforeInput
                StillWaitingForInput = -not $record.ReturnedBeforeInput
            }) -Observed ([ordered]@{ ReturnedBeforeInput = $record.ReturnedBeforeInput })
    }
    if ($case -ceq 'unknown-flood') {
        $drainedFloodStreamCount = @($record.SafeConsoleLines | Where-Object {
                $_ -cmatch '^Persistence output (stdout|stderr): excluded=513, overlong=0, queueDropped=0, ' +
                'decodeErrors=0, readerErrors=0, drained=True\.$'
            }).Count
        Assert-OutputChecks -Name 'simultaneous unknown stdout/stderr flood is drained without leaking text' `
            -Checks ([ordered]@{
                TwoFullyDrainedStreams = $drainedFloodStreamCount -eq 2
                NoSentinel = -not $record.SentinelSeen
            }) -Observed ([ordered]@{ DrainedFloodStreamCount = $drainedFloodStreamCount })
    }
    if ($case -ceq 'wrong-stream') {
        $wrongStreamRowCount = @($record.SafeConsoleLines | Where-Object {
                $_ -cmatch '^\[Persistence stderr .+\] Server stopped\.$'
            }).Count
        Assert-OutputChecks -Name 'a lifecycle-looking line on the wrong stream is excluded' `
            -Checks ([ordered]@{ NoWrongStreamRow = $wrongStreamRowCount -eq 0 }) `
            -Observed ([ordered]@{ WrongStreamRowCount = $wrongStreamRowCount })
    }
}

foreach ($case in @('startup-failure', 'shutdown-failure', 'long-line', 'invalid-utf8')) {
    $record = Invoke-WholeLauncherFixture -Case $case -EarlyExit:($case -ceq 'startup-failure')
    $expectedChildExit = switch ($case) { 'startup-failure' { 7 }; 'shutdown-failure' { 9 }; default { 0 } }
    $expected = switch ($case) {
        'startup-failure' { '\] Persistence startup failed\. Check the approved public profile and runtime access\.$' }
        'shutdown-failure' { '\] Server shutdown is incomplete; persistence release is not confirmed\.$' }
        'long-line' { '^Persistence output stdout: excluded=2, overlong=1, queueDropped=0, decodeErrors=0, readerErrors=0,' }
        'invalid-utf8' { '^Persistence output stderr: excluded=\d+, overlong=0, queueDropped=0, decodeErrors=1, readerErrors=0,' }
    }
    $diagnosticCount = @($record.SafeConsoleLines | Where-Object { $_ -cmatch $expected }).Count
    Assert-OutputChecks -Name ($case + ': failure remains nonzero with safe diagnostic and no secret sentinel') `
        -Checks ([ordered]@{
            LauncherExitNonzero = $record.ExitCode -ne 0
            ChildIdentityMatched = $record.ChildIdentityMatched
            ChildHandleCapturedLive = $record.ChildHandleCapturedLive
            ChildReturned = $record.ChildReturned
            ChildExitObserved = $record.ChildExitObserved
            ExpectedChildExitMatched = $null -ne $record.ChildExitCode -and
                $record.ChildExitCode -eq $expectedChildExit
            ObserverDrained = $record.ObserverDrained
            NoSentinel = -not $record.SentinelSeen
            OneSafeDiagnostic = $diagnosticCount -eq 1
        }) -Observed ([ordered]@{
            LauncherExit = $record.ExitCode; ChildExit = $record.ChildExitCode
            ExpectedChildExit = $expectedChildExit; DiagnosticCount = $diagnosticCount
            ChildObservation = $record.ChildObservation; ChildObservationError = $record.ChildObservationError
        })
}

# Lower-level probes complement, and never replace, the whole-entry cases above.
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($launcher, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Launcher must parse before its reader definitions can be hosted.' }
$definitions = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.FunctionDefinitionAst]
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
. ([scriptblock]::Create($definitions))
Initialize-PersistenceOutputCapture

foreach ($mode in @('queue-bound', 'reader-fault', 'inherited-pipe')) {
    $start = New-NativeFixtureStart -Case $mode -Arguments @('--probe', $mode)
    $process = [Diagnostics.Process]::Start($start)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $faultStream = $null
    $faultReadStarted = $false
    try {
        if ($mode -ceq 'reader-fault') {
            try {
                $faultStream = [DawnholderLauncherOutputTests.ReaderFaultStream]::new($process)
            } catch {
                [ordered]@{
                    Mode = $mode; Status = 'fault-stream-binding-failed'
                    ExceptionType = $_.Exception.GetType().FullName; FaultObserved = $false
                } | ConvertTo-Json | Set-Content -LiteralPath (
                    Join-Path $script:SuiteRoot 'reader-fault-injection.json')
                throw 'Owned stdout could not bind the fault stream; no fallback or retry.'
            }
        }
        [Dawnholder.PersistenceLauncherOutput]::Start($process)
        $capture = [Dawnholder.PersistenceLauncherOutput]::Find($process)
        if ($mode -ceq 'reader-fault') {
            $faultReadStarted = $faultStream.WaitForRead(3000)
            if (-not $faultReadStarted) {
                [ordered]@{
                    Mode = $mode; Status = 'actual-product-read-not-observed'
                    BindingVerified = $faultStream.StandardOutputBindingVerified; FaultObserved = $false
                } | ConvertTo-Json | Set-Content -LiteralPath (
                    Join-Path $script:SuiteRoot 'reader-fault-injection.json')
                throw 'Product read did not reach the fault stream; no fallback or retry.'
            }
            $faultStream.ReleaseFault()
        }
        $childReturned = $process.WaitForExit(5000)
        if (-not $childReturned) { throw 'Owned reader-probe child did not return; do not kill or retry.' }
        $pendingCount = $capture.TakeLines().Length
        $drainWatch = [Diagnostics.Stopwatch]::StartNew()
        $complete = Complete-PersistenceOutputCapture -Process $process
        $drainWatch.Stop()
        $stdoutCounts = $capture.Counts('stdout')
        $stderrCounts = $capture.Counts('stderr')
        $record = [ordered]@{
            Mode = $mode
            Boundary = $(if ($mode -ceq 'reader-fault') {
                    'lower-level unmodified product reader / injected stdout stream / native no-DB child'
                } else { 'lower-level actual native pipe reader probe' })
            Id = $process.Id; BirthUtc = $process.StartTime.ToUniversalTime().ToString('o')
            Executable = $nativePath; Argv = @('--probe', $mode); Cwd = $bin
            ExitCode = $process.ExitCode; ChildReturned = $childReturned; PendingCount = $pendingCount
            Complete = $complete; ReadersCompleted = $capture.ReadersCompleted
            DrainMilliseconds = $drainWatch.Elapsed.TotalMilliseconds
            StdoutCounts = $stdoutCounts; StderrCounts = $stderrCounts
        }
        if ($mode -ceq 'queue-bound') {
            Assert-OutputChecks `
                -Name 'bounded queue and unterminated oversized output drain without waiting for delivery' `
                -Checks ([ordered]@{
                    PendingWithinLimit = $pendingCount -le 32
                    StdoutQueueDropped = $stdoutCounts.QueueDropped -gt 0
                    OneStdoutOverlong = $stdoutCounts.Overlong -eq 1
                    OneStderrOverlong = $stderrCounts.Overlong -eq 1
                    CompleteFalse = -not $complete
                }) -Observed ([ordered]@{
                    PendingCount = $pendingCount; StdoutCounts = $stdoutCounts; StderrCounts = $stderrCounts
                    Complete = $complete
                })
        } elseif ($mode -ceq 'reader-fault') {
            $faultObservation = [ordered]@{
                Boundary = 'test-owned stdout stream replacement before unmodified product Start'
                RuntimeSlot = $faultStream.RuntimeSlot; NativePipeType = $faultStream.NativePipeType
                BindingVerified = $faultStream.StandardOutputBindingVerified
                ReadStarted = $faultReadStarted; ReadApi = $faultStream.ReadApi
                ReadCalls = $faultStream.ReadCalls; FaultsRaised = $faultStream.FaultsRaised
                FaultExceptionType = $faultStream.FaultExceptionType
                ReadStartedUtc = $faultStream.ReadStartedUtc; ReleasedUtc = $faultStream.ReleasedUtc
                FaultRaisedUtc = $faultStream.FaultRaisedUtc; SlotRestored = $faultStream.SlotRestored
                KernelPipeIoFaultMeasured = $false
            }
            $record.ReaderFault = $faultObservation
            $faultObservation | ConvertTo-Json -Depth 4 |
                Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'reader-fault-injection.json')
            $faultOrderObserved = $null -ne $faultStream.ReadStartedUtc -and
                $null -ne $faultStream.ReleasedUtc -and $null -ne $faultStream.FaultRaisedUtc -and
                [datetime]$faultStream.ReadStartedUtc -le [datetime]$faultStream.ReleasedUtc -and
                [datetime]$faultStream.ReleasedUtc -le [datetime]$faultStream.FaultRaisedUtc
            Assert-OutputChecks -Name 'reader I/O fault is counted and cannot produce successful completion' `
                -Checks ([ordered]@{
                    ActualStdoutBindingVerified = $faultStream.StandardOutputBindingVerified
                    ActualProductReadStarted = $faultReadStarted
                    OneActualRead = $faultStream.ReadCalls -eq 1
                    OneActualFault = $faultStream.FaultsRaised -eq 1
                    IOExceptionRaised = $faultStream.FaultExceptionType -ceq 'System.IO.IOException'
                    ReadReleaseFaultOrderObserved = $faultOrderObserved
                    ReaderErrorCounted = $stdoutCounts.ReaderErrors -ge 1
                    ReadersCompleted = $capture.ReadersCompleted
                    CompleteFalse = -not $complete
                }) -Observed ([ordered]@{
                    Injection = $faultObservation; StdoutCounts = $stdoutCounts; StderrCounts = $stderrCounts
                    Complete = $complete; ReadersCompleted = $capture.ReadersCompleted
                })
        } else {
            $holderEnd = Join-Path $script:SuiteRoot ($mode + '-holder-end.json')
            while (-not (Test-Path -LiteralPath $holderEnd) -and $watch.Elapsed.TotalSeconds -lt 12) {
                [Threading.Thread]::Sleep(50)
            }
            $record.HolderReturned = Test-Path -LiteralPath $holderEnd
            Assert-OutputChecks `
                -Name 'inherited output pipe reaches the five-second drain bound and reports incomplete' `
                -Checks ([ordered]@{
                    CompleteFalse = -not $complete
                    DrainReachedLowerBound = $drainWatch.Elapsed.TotalMilliseconds -ge 4800
                    DrainBelowUpperBound = $drainWatch.Elapsed.TotalMilliseconds -lt 6500
                    HolderReturned = $record.HolderReturned
                }) -Observed ([ordered]@{
                    Complete = $complete; DrainMilliseconds = $drainWatch.Elapsed.TotalMilliseconds
                    HolderReturned = $record.HolderReturned
                })
        }
        $record | ConvertTo-Json -Depth 5 |
            Set-Content -LiteralPath (Join-Path $script:SuiteRoot ($mode + '-reader.json'))
    } finally {
        if ($null -ne $faultStream) { $faultStream.Dispose() }
        try { $process.StandardInput.Close() } catch { }
        $process.Dispose()
    }
}

[ordered]@{
    OriginalLauncher = $launcher; OriginalHash = $originalHash; Entry = $fixtureLauncher; EntryHash = $entryHash
    NativeFixture = $nativePath; NativeHash = (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash
    ChildExitSource = 'retained live OS process handle / GetExitCodeProcess after signaled wait'
    ReaderFaultBoundary = 'lower-level stdout stream substitution only; native kernel I/O fault is not measured'
    ReplacedBoundaries = @('WSL metadata/clone ownership/SDK result', 'native WSL/server process', 'dummy DPAPI credential')
    RealWsl = $false; RealDb = $false; PortBinding = $false; FixtureLifecycleOnly = $true
    SyntheticCredentialContentOrHashRecorded = $false
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'entry-manifest.json')
Complete-TestSuite
