#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    [string]$LauncherPath = (Join-Path $PSScriptRoot '../../Start-PersistenceServer.ps1')
)

# Independent byte/line boundary probes of the unchanged capture functions. Only owned PowerShell
# children emit synthetic bytes; this does not replace the whole-entry suite or real WSL verification.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'persistence-launcher-framing'
$launcher = (Resolve-Path -LiteralPath $LauncherPath).Path
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($launcher, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Launcher does not parse; no probe executed.' }
$definitions = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.FunctionDefinitionAst]
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
. ([scriptblock]::Create($definitions))
Initialize-PersistenceOutputCapture

$writerPath = Join-Path $script:SuiteRoot 'Write-SyntheticOutput.ps1'
$writer = @'
param([Parameter(Mandatory)][string]$FixturePath)
$ErrorActionPreference = 'Stop'
$fixture = Get-Content -LiteralPath $FixturePath -Raw | ConvertFrom-Json
foreach ($channel in @('Stdout', 'Stderr')) {
    $stream = if ($channel -ceq 'Stdout') { [Console]::OpenStandardOutput() } else {
        [Console]::OpenStandardError()
    }
    $bytes = [Convert]::FromBase64String($fixture.$channel)
    # Small writes exercise byte framing without relying on PowerShell's text encoding defaults.
    for ($index = 0; $index -lt $bytes.Length; $index += $fixture.WriteSize) {
        $length = [Math]::Min($fixture.WriteSize, $bytes.Length - $index)
        $stream.Write($bytes, $index, $length)
        $stream.Flush()
    }
}
'@
[IO.File]::WriteAllText($writerPath, $writer)
$encoding = [Text.UTF8Encoding]::new($false, $true)
$listening = 'Listening on 0.0.0.0:7777. Press Enter to stop.'
$stopped = 'Server stopped.'
$startupFailure = 'Persistence startup failed. Check the approved public profile and runtime access.'
$shutdownFailure = 'Server shutdown is incomplete; persistence release is not confirmed.'

# Expected rows and counts come from these fixed stimuli and the public whole-line contract,
# never from the product's matcher, constants, counters or a copy of its parsing calculation.
$cases = @(
    [pscustomobject]@{
        Name = 'crlf-limit-and-eof'
        Stdout = $encoding.GetBytes(('x' * 4096) + "`r`n" + ('y' * 4097) + "`n" + $listening + "`r`n" + $stopped)
        Stderr = $encoding.GetBytes(('z' * 4096) + "`n" + $startupFailure + "`r`n")
        ExpectedStdout = @($listening, $stopped)
        ExpectedStderr = @($startupFailure)
        Excluded = @(1, 1)
        Overlong = @(1, 0)
        Complete = $false
        WriteSize = 127
    },
    [pscustomobject]@{
        Name = 'exact-text-and-stream'
        Stdout = $encoding.GetBytes("server stopped.`nServer stopped. `n" +
            "prefix Server stopped.`nServer stopped.`rX`n" + [char]0 + "Server stopped.`n" +
            [char]27 + "[31mServer stopped.`n" + $stopped + "`n")
        Stderr = $encoding.GetBytes($stopped + "`n" + $shutdownFailure)
        ExpectedStdout = @($stopped)
        ExpectedStderr = @($shutdownFailure)
        Excluded = @(6, 1)
        Overlong = @(0, 0)
        Complete = $true
        WriteSize = 1
    },
    [pscustomobject]@{
        Name = 'unicode-and-bom'
        Stdout = $encoding.GetBytes("한글🙂`n" + $listening + "`n")
        Stderr = $encoding.GetBytes([string][char]0xFEFF + $startupFailure + "`n" + $startupFailure)
        ExpectedStdout = @($listening)
        ExpectedStderr = @($startupFailure)
        Excluded = @(1, 1)
        Overlong = @(0, 0)
        Complete = $true
        WriteSize = 1
    }
)

foreach ($case in $cases) {
    $fixturePath = Join-Path $script:SuiteRoot ($case.Name + '-input.json')
    [ordered]@{
        Stdout = [Convert]::ToBase64String($case.Stdout)
        Stderr = [Convert]::ToBase64String($case.Stderr)
        WriteSize = $case.WriteSize
    } | ConvertTo-Json | Set-Content -LiteralPath $fixturePath
    $start = [Diagnostics.ProcessStartInfo]::new([Diagnostics.Process]::GetCurrentProcess().Path)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $arguments = @('-NoLogo', '-NoProfile', '-File', $writerPath, '-FixturePath', $fixturePath)
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $startedUtc = [DateTime]::UtcNow
    $process = [Diagnostics.Process]::Start($start)
    try {
        [Dawnholder.PersistenceLauncherOutput]::Start($process)
        $capture = [Dawnholder.PersistenceLauncherOutput]::Find($process)
        $returned = $process.WaitForExit(10000)
        if (-not $returned) { throw 'Synthetic writer did not return; do not retry or kill.' }
        $capture.WaitForReaders(5000)
        $rows = @($capture.TakeLines())
        $complete = Complete-PersistenceOutputCapture -Process $process
        $stdoutCounts = $capture.Counts('stdout')
        $stderrCounts = $capture.Counts('stderr')
        $stdoutRows = @($rows | Where-Object Stream -CEQ 'stdout' | ForEach-Object Text)
        $stderrRows = @($rows | Where-Object Stream -CEQ 'stderr' | ForEach-Object Text)
        $record = [ordered]@{
            Case = $case.Name
            Executable = $start.FileName
            Argv = $arguments
            Cwd = (Get-Location).Path
            LauncherHash = (Get-FileHash -LiteralPath $launcher -Algorithm SHA256).Hash
            WriterHash = (Get-FileHash -LiteralPath $writerPath -Algorithm SHA256).Hash
            InputHash = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
            StartedUtc = $startedUtc.ToString('o')
            EndedUtc = [DateTime]::UtcNow.ToString('o')
            Id = $process.Id
            BirthUtc = $process.StartTime.ToUniversalTime().ToString('o')
            ExitCode = $process.ExitCode
            Returned = $returned
            Rows = $rows
            StdoutCounts = $stdoutCounts
            StderrCounts = $stderrCounts
            ReadersCompleted = $capture.ReadersCompleted
            Complete = $complete
        }
        $record | ConvertTo-Json -Depth 6 |
            Set-Content -LiteralPath (Join-Path $script:SuiteRoot ($case.Name + '-observed.json'))
        Assert-Equal -Name ($case.Name + ': writer exit') -Expected 0 -Actual $process.ExitCode
        Assert-Equal -Name ($case.Name + ': exact stdout rows') `
            -Expected ($case.ExpectedStdout -join "`n") -Actual ($stdoutRows -join "`n")
        Assert-Equal -Name ($case.Name + ': exact stderr rows') `
            -Expected ($case.ExpectedStderr -join "`n") -Actual ($stderrRows -join "`n")
        Assert-Equal -Name ($case.Name + ': excluded stdout') -Expected $case.Excluded[0] -Actual $stdoutCounts.Excluded
        Assert-Equal -Name ($case.Name + ': excluded stderr') -Expected $case.Excluded[1] -Actual $stderrCounts.Excluded
        Assert-Equal -Name ($case.Name + ': overlong stdout') -Expected $case.Overlong[0] -Actual $stdoutCounts.Overlong
        Assert-Equal -Name ($case.Name + ': overlong stderr') -Expected $case.Overlong[1] -Actual $stderrCounts.Overlong
        $unrelatedErrors = $stdoutCounts.DecodeErrors + $stderrCounts.DecodeErrors +
            $stdoutCounts.ReaderErrors + $stderrCounts.ReaderErrors +
            $stdoutCounts.QueueDropped + $stderrCounts.QueueDropped
        Assert-Equal -Name ($case.Name + ': no unrelated errors') -Expected 0 -Actual $unrelatedErrors
        Assert-Equal -Name ($case.Name + ': completion status') -Expected $case.Complete -Actual $complete
        $timestampsValid = @($rows | Where-Object {
                $_.Utc -lt $startedUtc -or $_.Utc -gt [DateTime]::UtcNow
            }).Count -eq 0
        $captureReleased = $null -eq [Dawnholder.PersistenceLauncherOutput]::Find($process)
        Assert-True -Name ($case.Name + ': readers returned and ownership released') `
            -Condition ($capture.ReadersCompleted -and $captureReleased)
        Assert-True -Name ($case.Name + ': capture timestamps belong to this attempt') -Condition $timestampsValid
    } finally {
        $process.Dispose()
    }
}
Complete-TestSuite
