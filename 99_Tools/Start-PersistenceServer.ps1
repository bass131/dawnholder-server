#requires -Version 7.0
# Run only in a separately approved runtime window after the owned clone has been compiled.
# No sync/build/run action, server.stdin FIFO, secret environment or persistent secret output is used.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProfilePath,
    [Parameter(Mandatory)][string]$CredentialPath,
    [string]$PublicCaPath = '',
    [string]$ExpectedPublicCaHash = '',
    [string]$EmptyCaDirectory = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PersistenceWslMetadata {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $lines = @(& wsl.exe -d Ubuntu --exec @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0 -or $lines.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$lines[0])) {
        throw 'Persistence launcher metadata check failed; no credential was read.'
    }
    return ([string]$lines[0]).TrimEnd([char[]]"`r`n")
}

function Get-PersistenceDotnetPath {
    param([Parameter(Mandatory)][string]$RuntimeRoot)

    # sdk.sh owns selection and global.json validation. The clone path is data, never shell source.
    $resolveSdk = @'
set -euo pipefail
fail() { printf '%s\n' "$*" >&2; exit 1; }
source "$1/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$1" >/dev/null
printf '%s\n' "$DOTNET"
'@
    $dotnetPath = Get-PersistenceWslMetadata -Arguments @(
        'bash', '-c', $resolveSdk, 'persistence-sdk', $RuntimeRoot
    )
    if (-not $dotnetPath.StartsWith('/')) {
        throw 'The persistence SDK must resolve to an absolute executable path.'
    }
    return $dotnetPath
}

function Initialize-PersistenceOutputCapture {
    if ('Dawnholder.PersistenceLauncherOutput' -as [type]) { return }

    # Readers own only redirected child output. They never invoke PowerShell or touch operator stdin.
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dawnholder
{
    public sealed class PersistenceOutputLine
    {
        public string Stream { get; }
        public DateTime Utc { get; }
        public string Text { get; }

        public PersistenceOutputLine(string stream, string text)
        {
            Stream = stream;
            Utc = DateTime.UtcNow;
            Text = text;
        }
    }

    public sealed class PersistenceOutputCounts
    {
        public long Excluded { get; internal set; }
        public long Overlong { get; internal set; }
        public long QueueDropped { get; internal set; }
        public long DecodeErrors { get; internal set; }
        public long ReaderErrors { get; internal set; }
    }

    public sealed class PersistenceLauncherOutput
    {
        public const int MaximumLineCharacters = 4096;
        public const int MaximumPendingLines = 32;
        public const int DrainMilliseconds = 5000;

        private static readonly ConditionalWeakTable<Process, PersistenceLauncherOutput> captures = new();
        private readonly object gate = new();
        private readonly Queue<PersistenceOutputLine> pending = new();
        private readonly CancellationTokenSource cancellation = new();
        private readonly Stream stdout;
        private readonly Stream stderr;
        private readonly Task readers;
        private readonly PersistenceOutputCounts stdoutCounts = new();
        private readonly PersistenceOutputCounts stderrCounts = new();
        private bool finalized;
        private bool clean;

        private PersistenceLauncherOutput(Process process)
        {
            stdout = process.StandardOutput.BaseStream;
            stderr = process.StandardError.BaseStream;
            // Exactly two readers drain both pipes even when the delivery queue is full.
            readers = Task.WhenAll(
                Task.Run(() => ReadStreamAsync(stdout, "stdout", stdoutCounts)),
                Task.Run(() => ReadStreamAsync(stderr, "stderr", stderrCounts)));
        }

        public static void Start(Process process)
        {
            if (captures.TryGetValue(process, out _))
                throw new InvalidOperationException("Output capture already belongs to this process.");
            captures.Add(process, new PersistenceLauncherOutput(process));
        }

        public static PersistenceLauncherOutput Find(Process process)
        {
            return captures.TryGetValue(process, out var capture) ? capture : null;
        }

        public bool ReadersCompleted => readers.IsCompleted;
        public bool Finalized => finalized;
        public bool Clean => clean;

        private static bool IsAllowed(string stream, string text)
        {
            // Exact whole-line source strings from GameServer/Program.cs; provider text is never forwarded.
            return stream == "stdout"
                ? text == "Listening on 0.0.0.0:7777. Press Enter to stop." || text == "Server stopped."
                : text == "Persistence startup failed. Check the approved public profile and runtime access." ||
                  text == "Server shutdown is incomplete; persistence release is not confirmed.";
        }

        private void AcceptLine(string stream, StringBuilder line, bool overlong, PersistenceOutputCounts counts)
        {
            if (overlong)
                return;
            string text = line.ToString();
            lock (gate)
            {
                if (!IsAllowed(stream, text))
                    counts.Excluded++;
                else if (pending.Count == MaximumPendingLines)
                    counts.QueueDropped++;
                else
                    pending.Enqueue(new PersistenceOutputLine(stream, text));
            }
        }

        private void AppendCharacter(StringBuilder line, char character, ref bool overlong, PersistenceOutputCounts counts)
        {
            if (overlong)
                return;
            if (line.Length == MaximumLineCharacters)
            {
                lock (gate) { counts.Overlong++; }
                line.Clear();
                overlong = true;
            }
            else
                line.Append(character);
        }

        private async Task ReadStreamAsync(Stream stream, string name, PersistenceOutputCounts counts)
        {
            var line = new StringBuilder(MaximumLineCharacters, MaximumLineCharacters);
            var characters = new char[512];
            bool overlong = false;
            bool pendingCarriageReturn = false;
            try
            {
                using var reader = new StreamReader(stream, new UTF8Encoding(false, true), false, 1024, true);
                int length;
                while ((length = await reader.ReadAsync(characters.AsMemory(), cancellation.Token)
                    .ConfigureAwait(false)) != 0)
                {
                    for (int index = 0; index < length; index++)
                    {
                        char character = characters[index];
                        if (character == '\n')
                        {
                            AcceptLine(name, line, overlong, counts);
                            line.Clear();
                            overlong = false;
                            pendingCarriageReturn = false;
                        }
                        else
                        {
                            // Delay one CR so a CRLF terminator does not consume the 4096-character line budget.
                            if (pendingCarriageReturn)
                                AppendCharacter(line, '\r', ref overlong, counts);
                            pendingCarriageReturn = character == '\r';
                            if (!pendingCarriageReturn)
                                AppendCharacter(line, character, ref overlong, counts);
                        }
                    }
                }
                if (overlong || line.Length > 0)
                    AcceptLine(name, line, overlong, counts);
            }
            catch (DecoderFallbackException)
            {
                lock (gate) { counts.DecodeErrors++; }
                // Decoding is uncertain after a malformed byte. Discard, but keep draining this pipe.
                await DiscardStreamAsync(stream, counts).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (cancellation.IsCancellationRequested) { }
            catch (Exception)
            {
                lock (gate) { counts.ReaderErrors++; }
            }
        }

        private async Task DiscardStreamAsync(Stream stream, PersistenceOutputCounts counts)
        {
            var bytes = new byte[1024];
            try
            {
                while (await stream.ReadAsync(bytes.AsMemory(), cancellation.Token).ConfigureAwait(false) != 0) { }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (cancellation.IsCancellationRequested) { }
            catch (Exception)
            {
                lock (gate) { counts.ReaderErrors++; }
            }
        }

        public PersistenceOutputLine[] TakeLines()
        {
            lock (gate)
            {
                var lines = pending.ToArray();
                pending.Clear();
                return lines;
            }
        }

        public PersistenceOutputCounts Counts(string stream)
        {
            lock (gate)
            {
                var source = stream == "stdout" ? stdoutCounts : stderrCounts;
                return new PersistenceOutputCounts
                {
                    Excluded = source.Excluded,
                    Overlong = source.Overlong,
                    QueueDropped = source.QueueDropped,
                    DecodeErrors = source.DecodeErrors,
                    ReaderErrors = source.ReaderErrors
                };
            }
        }

        public void WaitForReaders(int milliseconds)
        {
            readers.Wait(milliseconds);
        }

        public void Finish(Process process, bool drained)
        {
            if (finalized)
                return;
            finalized = true;
            if (!drained)
                cancellation.Cancel();
            // Closing owned output pipes cancels a stalled reader, never the child process.
            try { stdout.Dispose(); } catch { }
            try { stderr.Dispose(); } catch { }
            lock (gate)
            {
                clean = drained && readers.IsCompleted &&
                    stdoutCounts.Overlong + stderrCounts.Overlong == 0 &&
                    stdoutCounts.QueueDropped + stderrCounts.QueueDropped == 0 &&
                    stdoutCounts.DecodeErrors + stderrCounts.DecodeErrors == 0 &&
                    stdoutCounts.ReaderErrors + stderrCounts.ReaderErrors == 0;
            }
            if (readers.IsCompleted)
            {
                cancellation.Dispose();
                captures.Remove(process);
            }
            else
            {
                _ = readers.ContinueWith(_ =>
                {
                    cancellation.Dispose();
                    captures.Remove(process);
                }, TaskScheduler.Default);
            }
        }

        public void RetainWhileChildIsUncertain(Process process)
        {
            finalized = true;
            clean = false;
            // Keep draining an uncertain live child without closing its output pipes or stopping it.
            // The continuation retains process identity and owns cleanup after both pipe readers return.
            _ = readers.ContinueWith(_ =>
            {
                try { stdout.Dispose(); } catch { }
                try { stderr.Dispose(); } catch { }
                cancellation.Dispose();
                captures.Remove(process);
                process.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
'@
}

function Receive-PersistenceOutput {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process)

    if (-not ('Dawnholder.PersistenceLauncherOutput' -as [type])) { return }
    $capture = [Dawnholder.PersistenceLauncherOutput]::Find($Process)
    if ($null -eq $capture) { return }
    foreach ($line in $capture.TakeLines()) {
        Write-Host ('[Persistence {0} {1:o}] {2}' -f $line.Stream, $line.Utc, $line.Text)
    }
}

function Complete-PersistenceOutputCapture {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [switch]$ChildStillRunning
    )

    # Existing isolated shutdown tests can own a process with no launcher output capture.
    if (-not ('Dawnholder.PersistenceLauncherOutput' -as [type])) { return $true }
    $capture = [Dawnholder.PersistenceLauncherOutput]::Find($Process)
    if ($null -eq $capture) { return $true }
    if ($capture.Finalized) { return $capture.Clean }
    if ($ChildStillRunning) {
        $capture.RetainWhileChildIsUncertain($Process)
        Write-Host 'Persistence output readers remain owned while child-process state is uncertain.'
        return $false
    }

    $drain = [Diagnostics.Stopwatch]::StartNew()
    $drainBudget = [Dawnholder.PersistenceLauncherOutput]::DrainMilliseconds
    while (-not $capture.ReadersCompleted -and $drain.ElapsedMilliseconds -lt $drainBudget) {
        Receive-PersistenceOutput -Process $Process
        $remaining = $drainBudget - [int]$drain.ElapsedMilliseconds
        if ($remaining -gt 0) { $capture.WaitForReaders([Math]::Min(50, $remaining)) }
    }
    $readersDrained = $capture.ReadersCompleted
    Receive-PersistenceOutput -Process $Process
    $capture.Finish($Process, $readersDrained)
    foreach ($stream in @('stdout', 'stderr')) {
        $counts = $capture.Counts($stream)
        Write-Host (('Persistence output {0}: excluded={1}, overlong={2}, queueDropped={3}, ' +
            'decodeErrors={4}, readerErrors={5}, drained={6}.') -f $stream, $counts.Excluded, $counts.Overlong,
            $counts.QueueDropped, $counts.DecodeErrors, $counts.ReaderErrors, $readersDrained)
    }
    return $capture.Clean
}

function Wait-PersistenceShutdownRequest {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process)

    if ($Process.HasExited) { return $false }
    Write-Host 'Persistence server process launched; press Enter to request normal shutdown'
    if (-not [Console]::IsInputRedirected) {
        while (-not $Process.HasExited) {
            Receive-PersistenceOutput -Process $Process
            if ([Console]::KeyAvailable -and [Console]::ReadKey($true).Key -eq [ConsoleKey]::Enter) {
                Write-Host
                return $true
            }
            [void]$Process.WaitForExit(100)
        }
        return $false
    }

    # A background ReadLine cannot be cancelled reliably and can consume the next command after
    # the child dies. Peek at redirected pipe input and read only available bytes; leave no reader behind.
    if (-not ('Dawnholder.PersistenceLauncherInput' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace Dawnholder
{
    public static class PersistenceLauncherInput
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int standardHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetFileType(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekNamedPipe(
            IntPtr handle, IntPtr buffer, uint bufferSize, IntPtr bytesRead,
            out uint availableBytes, IntPtr bytesLeft);
    }
}
'@
    }
    $inputHandle = [Dawnholder.PersistenceLauncherInput]::GetStdHandle(-10) # STD_INPUT_HANDLE
    $inputType = [Dawnholder.PersistenceLauncherInput]::GetFileType($inputHandle)
    if ($inputType -notin @(1, 3)) { # FILE_TYPE_DISK or FILE_TYPE_PIPE
        throw 'The redirected operator input is not a file or pipe.'
    }
    $inputStream = [Console]::OpenStandardInput()
    try {
        while (-not $Process.HasExited) {
            Receive-PersistenceOutput -Process $Process
            $availableBytes = [uint32]0
            if ($inputType -eq 3) {
                $peeked = [Dawnholder.PersistenceLauncherInput]::PeekNamedPipe(
                    $inputHandle, [IntPtr]::Zero, 0, [IntPtr]::Zero, [ref]$availableBytes, [IntPtr]::Zero)
                if (-not $peeked) {
                    $inputError = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
                    if ($inputError -in @(109, 232)) { return $true } # Closed pipe is EOF, like Read-Host.
                    throw 'The redirected operator input could not be observed.'
                }
            }
            if ($inputType -eq 1 -or $availableBytes -gt 0) {
                $inputByte = $inputStream.ReadByte()
                if ($inputByte -lt 0 -or $inputByte -eq 10) { return $true } # EOF or completed line.
                continue
            }
            [void]$Process.WaitForExit(100)
        }
        return $false
    } finally {
        $inputStream.Dispose()
    }
}

$runtimeProcess = $null
$credential = $null
$passwordPointer = [IntPtr]::Zero
$passwordText = $null
$payload = $null
$envelope = $null
try {
    if (-not [OperatingSystem]::IsWindows()) {
        throw 'The persistence launcher requires current-user Windows DPAPI.'
    }
    $projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $publicProfile = (Resolve-Path -LiteralPath $ProfilePath).Path
    $projectPrefix = $projectRoot.TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar
    if (-not $publicProfile.StartsWith($projectPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetExtension($publicProfile) -cne '.json') {
        throw 'The public persistence profile must be a JSON file in this project.'
    }
    $sourceWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $projectRoot)
    $profileWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $publicProfile)
    $runtimeRoot = Get-PersistenceWslMetadata -Arguments @('bash', "$sourceWsl/99_Tools/sync-wsl.sh", 'path')
    $canonicalRoot = Get-PersistenceWslMetadata -Arguments @('realpath', '-e', $runtimeRoot)
    if (-not $runtimeRoot.StartsWith('/') -or $runtimeRoot -cne $canonicalRoot -or
        $runtimeRoot -eq '/' -or $runtimeRoot.StartsWith('/mnt/')) {
        throw 'The compiled persistence clone path is invalid.'
    }
    $cloneOwner = Get-PersistenceWslMetadata -Arguments @('cat', "$runtimeRoot/.dawnholder-source")
    if ($cloneOwner -cne $sourceWsl) {
        throw 'The compiled persistence clone belongs to a different project.'
    }
    $serverDll = "$runtimeRoot/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll"
    $canonicalDll = Get-PersistenceWslMetadata -Arguments @('realpath', '-e', $serverDll)
    if ($canonicalDll -cne $serverDll) {
        throw 'The compiled persistence server is missing or linked outside its clone.'
    }
    $dotnetPath = Get-PersistenceDotnetPath -RuntimeRoot $runtimeRoot

    $trustArguments = @()
    if ($PublicCaPath -or $ExpectedPublicCaHash -or $EmptyCaDirectory) {
        if (-not $PublicCaPath -or -not $EmptyCaDirectory -or
            $ExpectedPublicCaHash -cnotmatch '^[A-F0-9]{64}$') {
            throw 'Public CA mode needs its path, reviewed hash and an empty CA directory.'
        }
        foreach ($trustPath in @($PublicCaPath, $EmptyCaDirectory)) {
            $ancestor = [IO.Path]::GetFullPath($trustPath)
            while ($ancestor) {
                if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                    throw 'Public CA paths cannot traverse reparse points.'
                }
                $ancestor = [IO.Path]::GetDirectoryName($ancestor)
            }
        }
        $caBytes = [IO.File]::ReadAllBytes($PublicCaPath)
        if ($caBytes.Length -gt 16384 -or
            [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($caBytes)) -cne $ExpectedPublicCaHash -or
            @(Get-ChildItem -LiteralPath $EmptyCaDirectory -Force).Count -ne 0) {
            throw 'Public CA input changed or the CA directory is not empty.'
        }
        $caText = [Text.UTF8Encoding]::new($false, $true).GetString($caBytes)
        if ($caText.Contains('PRIVATE KEY') -or -not $caText.Contains('BEGIN CERTIFICATE')) {
            throw 'Only a public PEM certificate is accepted.'
        }
        $caWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $PublicCaPath)
        $caDirectoryWsl = Get-PersistenceWslMetadata -Arguments @('wslpath', '-a', '-u', $EmptyCaDirectory)
        # These values are public paths and affect only the Linux child; no global trust store or WSLENV edit.
        $trustArguments = @('env', "SSL_CERT_FILE=$caWsl", "SSL_CERT_DIR=$caDirectoryWsl")
    }

    Initialize-PersistenceOutputCapture

    # Import-Clixml decrypts only in the approved current-user Windows DPAPI context.
    # Keep provider/DPAPI errors out of the console, including the exception's inner text.
    try {
        $credential = Import-Clixml -LiteralPath $CredentialPath
        if ($credential -isnot [Management.Automation.PSCredential] -or
            [string]::IsNullOrWhiteSpace($credential.UserName) -or $credential.UserName.Length -gt 128 -or
            $credential.UserName -match '[\x00-\x1f\x7f]' -or
            $credential.Password.Length -lt 1 -or $credential.Password.Length -gt 128) {
            throw 'Invalid runtime credential.'
        }
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($credential.Password)
        $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $envelope = [ordered]@{
            version = 1
            userName = $credential.UserName
            password = $passwordText
        }
        $payload = ConvertTo-Json -InputObject $envelope -Compress
        $utf8 = [Text.UTF8Encoding]::new($false, $true)
        if ($utf8.GetByteCount($payload) -ge 2048) {
            throw 'Runtime credential envelope exceeds its bound.'
        }
        $startInfo = [Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = 'wsl.exe'
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardInput = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.StandardInputEncoding = $utf8
        $startInfo.StandardOutputEncoding = $utf8
        $startInfo.StandardErrorEncoding = $utf8
        $childArguments = @('-d', 'Ubuntu', '--cd', $runtimeRoot, '--exec') + $trustArguments +
        @($dotnetPath, $serverDll, '--persistence-profile', $profileWsl)
        foreach ($argument in $childArguments) {
            $startInfo.ArgumentList.Add($argument)
        }
        $runtimeProcess = [Diagnostics.Process]::new()
        $runtimeProcess.StartInfo = $startInfo
        if (-not $runtimeProcess.Start()) {
            throw 'Persistence server process did not start.'
        }
        [Dawnholder.PersistenceLauncherOutput]::Start($runtimeProcess)
        $runtimeProcess.StandardInput.NewLine = "`n"
        $runtimeProcess.StandardInput.WriteLine($payload)
        $runtimeProcess.StandardInput.Flush()
        Write-Output ('Owned WSL process PID {0}, birth UTC {1:o}.' -f
            $runtimeProcess.Id, $runtimeProcess.StartTime.ToUniversalTime())
    } catch {
        throw 'Persistence credential handoff failed; provider and credential text suppressed.'
    } finally {
        if ($passwordPointer -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
            $passwordPointer = [IntPtr]::Zero
        }
        if ($credential -is [Management.Automation.PSCredential]) {
            $credential.Password.Dispose()
        }
        if ($null -ne $envelope) { $envelope.Clear() }
        $envelope = $null
        $credential = $null
        $passwordText = $null
        $payload = $null
    }

    # stdin stays open after the credential line for Program's normal operator stop input.
    if (Wait-PersistenceShutdownRequest -Process $runtimeProcess) {
        if (-not $runtimeProcess.HasExited) {
            $runtimeProcess.StandardInput.WriteLine()
            $runtimeProcess.StandardInput.Flush()
        }
    }
    $runtimeProcess.StandardInput.Close()
    while (-not $runtimeProcess.WaitForExit(100)) {
        Receive-PersistenceOutput -Process $runtimeProcess
    }
    $outputComplete = Complete-PersistenceOutputCapture -Process $runtimeProcess
    if ($runtimeProcess.ExitCode -ne 0) {
        throw 'Persistence server failed or shutdown is incomplete; inspect only safe server diagnostics.'
    }
    if (-not $outputComplete) {
        throw 'Persistence server output is incomplete; confirm reader diagnostics before another approved attempt.'
    }
} catch {
    # Do not retry or kill a child on an uncertain handoff/close. EOF lets an unstarted server refuse startup.
    if ($null -ne $runtimeProcess) {
        try { $runtimeProcess.StandardInput.Close() } catch { }
    }
    throw 'Persistence launcher did not complete; confirm child-process state before another approved attempt.'
} finally {
    if ($null -ne $runtimeProcess) {
        try {
            if ($runtimeProcess.HasExited) {
                $null = Complete-PersistenceOutputCapture -Process $runtimeProcess
                $runtimeProcess.Dispose()
            } else {
                $null = Complete-PersistenceOutputCapture -Process $runtimeProcess -ChildStillRunning
            }
        } catch {
            Write-Host 'Persistence output cleanup could not confirm reader state.'
        }
    }
}
