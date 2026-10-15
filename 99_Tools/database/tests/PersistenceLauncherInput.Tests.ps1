#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    [string]$LauncherPath = (Join-Path $PSScriptRoot '../../Start-PersistenceServer.ps1')
)

# Operator-input boundaries of the launcher's shutdown wait, each in a fresh PowerShell process: a real hidden console
# (dead child, Enter key), file redirection (line, empty), a pipe closed by the operator, input written after a dead
# child, and the NUL character device. The process hosts only the launcher's own function definitions and its
# post-handoff statements with an owned PowerShell child; no WSL, credential, server or SQL is involved.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'persistence-launcher-input'
$launcher = (Resolve-Path -LiteralPath $LauncherPath).Path
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($launcher, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Launcher must parse before its shutdown path can be hosted.' }
$definitions = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.FunctionDefinitionAst]
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
$mainTry = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.TryStatementAst] })
if ($mainTry.Count -ne 1) { throw 'Expected the launcher owning try block.' }
$handoff = @($mainTry[0].Body.Statements | Where-Object {
        $_ -is [Management.Automation.Language.TryStatementAst] -and $_.Extent.Text.Contains('Import-Clixml')
    })
if ($handoff.Count -ne 1) { throw 'Expected the credential handoff boundary.' }
$shutdown = @($mainTry[0].Body.Statements | Where-Object {
        $_.Extent.StartOffset -gt $handoff[0].Extent.EndOffset
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
if ([string]::IsNullOrWhiteSpace($shutdown)) { throw 'Missing production shutdown path.' }
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'production-functions.ps1'), $definitions)
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'production-shutdown.ps1'), $shutdown)

# The driver owns one child, runs the production shutdown statements once and records what it observed.
# Console cases inject key events into the driver's own console input buffer. A recovery Enter after 8 s lets an
# input-blocking implementation return without any process being killed; the record says whether it was needed.
$driver = @'
param([string]$Root, [string]$Case, [ValidateSet('dies', 'waits')][string]$ChildMode)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$recordPath = Join-Path $Root ($Case + '-record.json')
$record = [ordered]@{ Case = $Case; ChildMode = $ChildMode; InputRedirected = [Console]::IsInputRedirected
    ShutdownError = $null; RecoveryEnter = $false; ChildExitCode = $null; ChildReturned = $false; NextLine = $null }
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DawnholderLauncherInputTests
{
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct KeyInputRecord
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public int KeyDown;
        [FieldOffset(8)] public ushort RepeatCount;
        [FieldOffset(10)] public ushort VirtualKeyCode;
        [FieldOffset(12)] public ushort VirtualScanCode;
        [FieldOffset(14)] public char UnicodeChar;
        [FieldOffset(16)] public uint ControlKeyState;
    }

    public static class OwnConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int standardHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleInputW(IntPtr input, KeyInputRecord[] records, uint length, out uint written);

        static Timer recovery;
        public static volatile bool Recovered;

        public static bool PressEnter()
        {
            var down = new KeyInputRecord { EventType = 1, KeyDown = 1, RepeatCount = 1, VirtualKeyCode = 0x0D,
                VirtualScanCode = 0x1C, UnicodeChar = '\r' };
            var up = down;
            up.KeyDown = 0;
            uint written;
            return WriteConsoleInputW(GetStdHandle(-10), new[] { down, up }, 2, out written) && written == 2;
        }

        public static void ArmRecovery(int milliseconds)
        {
            recovery = new Timer(_ => { Recovered = PressEnter(); }, null, milliseconds, Timeout.Infinite);
        }
    }
}
"@
. (Join-Path $Root 'production-functions.ps1')
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = [Diagnostics.Process]::GetCurrentProcess().Path
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
# 'waits' tells the operator Enter (empty line, exit 0) apart from stdin EOF (exit 5) and other text (exit 9).
$program = if ($ChildMode -ceq 'dies') {
    '$null = [Console]::ReadLine(); Start-Sleep -Milliseconds 1200; exit 7'
} else {
    '$null = [Console]::ReadLine(); $line = [Console]::ReadLine(); if ($null -eq $line) { exit 5 }; ' +
        'if ($line -ceq "") { exit 0 }; exit 9'
}
foreach ($argument in @('-NoLogo', '-NoProfile', '-Command', $program)) { $start.ArgumentList.Add($argument) }
$runtimeProcess = [Diagnostics.Process]::Start($start)
$runtimeProcess.StandardInput.WriteLine('begin')
$runtimeProcess.StandardInput.Flush()
if ($Case -like 'console-*') { [DawnholderLauncherInputTests.OwnConsole]::ArmRecovery(8000) }
if ($Case -ceq 'console-enter') { $record.EnterInjected = [DawnholderLauncherInputTests.OwnConsole]::PressEnter() }
$watch = [Diagnostics.Stopwatch]::StartNew()
try {
    . (Join-Path $Root 'production-shutdown.ps1')
} catch {
    $record.ShutdownError = $_.Exception.Message
} finally {
    $record.ShutdownMilliseconds = $watch.Elapsed.TotalMilliseconds
    $record.RecoveryEnter = [DawnholderLauncherInputTests.OwnConsole]::Recovered
    try { $runtimeProcess.StandardInput.Close() } catch { }
    $record.ChildReturned = $runtimeProcess.WaitForExit(5000)
    if ($record.ChildReturned) { $record.ChildExitCode = $runtimeProcess.ExitCode }
}
if ($Case -ceq 'pipe-dead-next') {
    # The test writes one more operator line only after this marker: it must still be readable here.
    [IO.File]::WriteAllText((Join-Path $Root ($Case + '-returned.txt')), 'returned')
    $record.NextLine = [Console]::In.ReadLine()
}
$record | ConvertTo-Json | Set-Content -LiteralPath $recordPath
'@
$driverPath = Join-Path $script:SuiteRoot 'input-driver.ps1'
[IO.File]::WriteAllText($driverPath, $driver)
$pwsh = [Diagnostics.Process]::GetCurrentProcess().Path

function Start-DriverCase {
    param([string]$Case, [string]$ChildMode, [ValidateSet('console', 'file', 'pipe', 'nul')][string]$Channel)
    $driverArguments = @('-NoLogo', '-NoProfile', '-File', $driverPath, '-Root', $script:SuiteRoot, '-Case', $Case,
        '-ChildMode', $ChildMode)
    $observed = [ordered]@{ Case = $Case; Channel = $Channel; DriverReturned = $false; DriverExitCode = $null }
    $process = $null
    if ($Channel -ceq 'console') {
        # Shell execution gives the driver its own hidden console, so stdin is a real console input buffer.
        $process = Start-Process -FilePath $pwsh -ArgumentList $driverArguments -WindowStyle Hidden -PassThru
    } elseif ($Channel -in @('file', 'nul')) {
        $inputPath = if ($Channel -ceq 'nul') { 'NUL' } else {
            '"' + (Join-Path $script:SuiteRoot ($Case + '-input.txt')) + '"'
        }
        $quoted = @($driverArguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
        $start = [Diagnostics.ProcessStartInfo]::new($env:ComSpec)
        $start.Arguments = '/d /s /c ""' + $pwsh + '" ' + $quoted + ' < ' + $inputPath + '"'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $process = [Diagnostics.Process]::Start($start)
    } else {
        $start = [Diagnostics.ProcessStartInfo]::new($pwsh)
        foreach ($argument in $driverArguments) { $start.ArgumentList.Add($argument) }
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardInput = $true
        $process = [Diagnostics.Process]::Start($start)
        if ($Case -ceq 'pipe-eof') { $process.StandardInput.Close() }
        if ($Case -ceq 'pipe-dead-next') {
            $marker = Join-Path $script:SuiteRoot ($Case + '-returned.txt')
            $waited = [Diagnostics.Stopwatch]::StartNew()
            while (-not (Test-Path -LiteralPath $marker) -and $waited.Elapsed.TotalSeconds -lt 15) {
                Start-Sleep -Milliseconds 100
            }
            $observed.MarkerSeen = Test-Path -LiteralPath $marker
            $process.StandardInput.WriteLine('next-command')
            $process.StandardInput.Close()
        }
    }
    $observed.DriverReturned = $process.WaitForExit(30000)
    if ($observed.DriverReturned) { $observed.DriverExitCode = $process.ExitCode }
    $recordPath = Join-Path $script:SuiteRoot ($Case + '-record.json')
    $observed.Record = if (Test-Path -LiteralPath $recordPath) {
        Get-Content -Raw $recordPath | ConvertFrom-Json
    } else { $null }
    $observedPath = Join-Path $script:SuiteRoot ($Case + '-observed.json')
    $observed | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $observedPath
    $process.Dispose()
    return $observed
}

$failureMessage = 'Persistence server failed or shutdown is incomplete; inspect only safe server diagnostics.'
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'file-line-input.txt'), "`n")
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'file-empty-input.txt'), '')

$consoleDead = Start-DriverCase -Case 'console-dead' -ChildMode dies -Channel console
$deadReturned = $consoleDead.DriverReturned -and $null -ne $consoleDead.Record
$deadWithoutInput = $deadReturned -and -not $consoleDead.Record.RecoveryEnter -and
-not $consoleDead.Record.InputRedirected
$deadReported = $deadReturned -and $consoleDead.Record.ShutdownError -ceq $failureMessage -and
$consoleDead.Record.ChildExitCode -eq 7
Assert-True -Name 'real console: a dead child ends the wait without any key' `
    -Condition ($deadWithoutInput -and $deadReported) -Detail ($consoleDead | ConvertTo-Json -Compress -Depth 4)

$consoleEnter = Start-DriverCase -Case 'console-enter' -ChildMode waits -Channel console
$enterReturned = $consoleEnter.DriverReturned -and $null -ne $consoleEnter.Record
$enterNormal = $enterReturned -and $consoleEnter.Record.EnterInjected -and -not $consoleEnter.Record.RecoveryEnter -and
$null -eq $consoleEnter.Record.ShutdownError -and $consoleEnter.Record.ChildExitCode -eq 0
Assert-True -Name 'real console: the Enter key requests normal child shutdown' -Condition $enterNormal `
    -Detail ($consoleEnter | ConvertTo-Json -Compress -Depth 4)

foreach ($case in @('file-line', 'file-empty')) {
    $fileCase = Start-DriverCase -Case $case -ChildMode waits -Channel file
    $fileNormal = $fileCase.DriverReturned -and $null -ne $fileCase.Record -and $fileCase.Record.InputRedirected -and
    $null -eq $fileCase.Record.ShutdownError -and $fileCase.Record.ChildExitCode -eq 0
    Assert-True -Name "redirected file ($case): line or EOF requests normal child shutdown" -Condition $fileNormal `
        -Detail ($fileCase | ConvertTo-Json -Compress -Depth 4)
}

$pipeEof = Start-DriverCase -Case 'pipe-eof' -ChildMode waits -Channel pipe
$pipeNormal = $pipeEof.DriverReturned -and $null -ne $pipeEof.Record -and $null -eq $pipeEof.Record.ShutdownError -and
$pipeEof.Record.ChildExitCode -eq 0
Assert-True -Name 'closed operator pipe is EOF and requests normal child shutdown' -Condition $pipeNormal `
    -Detail ($pipeEof | ConvertTo-Json -Compress -Depth 4)

$pipeNext = Start-DriverCase -Case 'pipe-dead-next' -ChildMode dies -Channel pipe
$nextFailure = $null -ne $pipeNext.Record -and $pipeNext.Record.ShutdownError -ceq $failureMessage
$nextKept = $null -ne $pipeNext.Record -and $pipeNext.MarkerSeen -and $pipeNext.Record.NextLine -ceq 'next-command'
Assert-True -Name 'input written after a dead child is left for the next reader' `
    -Condition ($nextFailure -and $nextKept) -Detail ($pipeNext | ConvertTo-Json -Compress -Depth 4)

# The character device is not an operator channel the launcher documents; record the behavior without a verdict.
$nul = Start-DriverCase -Case 'nul-device' -ChildMode waits -Channel nul
Add-TestResult -Name 'NUL character device as operator input' -Outcome OBSERVED `
    -Detail ($nul | ConvertTo-Json -Compress -Depth 4)
Complete-TestSuite
