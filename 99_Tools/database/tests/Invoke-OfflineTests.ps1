[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot
)
# Runs every offline database tool suite in its own child process of this PowerShell executable.
# Each suite writes fixtures and results.json below WorkRoot; nothing opens SQL or changes identity/ACL state.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [IO.Path]::IsPathRooted($WorkRoot)) {
    throw 'WorkRoot must be an explicit absolute path.'
}
$root = [IO.Path]::GetFullPath($WorkRoot)
if (-not [IO.Directory]::Exists($root)) {
    throw "WorkRoot does not exist: $root"
}
$summaryPath = Join-Path $root 'summary.json'
if ([IO.File]::Exists($summaryPath)) {
    throw "Summary already exists; use a new run directory instead of overwriting: $summaryPath"
}

$suites = @(
    @{ File = 'ModuleStructureCli.Tests.ps1'; Directory = 'module-structure-cli' },
    @{ File = 'ModuleHashConsumers.Tests.ps1'; Directory = 'module-hash-consumers' },
    @{ File = 'ModuleHashLiterals.Tests.ps1'; Directory = 'module-hash-literals' },
    @{ File = 'ModuleBundle.Tests.ps1'; Directory = 'module-bundle' },
    @{ File = 'ModuleDeployment.Tests.ps1'; Directory = 'module-deployment' },
    @{ File = 'TestEnvironmentLifecycle.Tests.ps1'; Directory = 'test-environment-lifecycle' },
    @{ File = 'EnvironmentGuards.Tests.ps1'; Directory = 'environment-guards' },
    @{ File = 'TestDatabaseContract.Tests.ps1'; Directory = 'test-database-contract' }
)
$exe = [Diagnostics.Process]::GetCurrentProcess().Path
$summary = New-Object 'Collections.Generic.List[object]'
foreach ($suite in $suites) {
    $file = Join-Path $PSScriptRoot $suite.File
    $stdoutPath = Join-Path $root ($suite.Directory + '.stdout.txt')
    $stderrPath = Join-Path $root ($suite.Directory + '.stderr.txt')
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $exe
    $info.Arguments = '-NoLogo -NoProfile -File "{0}" -WorkRoot "{1}"' -f $file, $root
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $started = [DateTime]::UtcNow
    $process = [Diagnostics.Process]::Start($info)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $finished = [DateTime]::UtcNow
    $utf8 = New-Object Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($stdoutPath, $stdout.Result, $utf8)
    [IO.File]::WriteAllText($stderrPath, $stderr.Result, $utf8)

    # A suite that stops before Complete-TestSuite leaves no results file; that is a failure, not zero tests.
    $resultsPath = Join-Path (Join-Path $root $suite.Directory) 'results.json'
    $results = @()
    if ([IO.File]::Exists($resultsPath)) {
        # Parentheses enumerate the parsed array; Windows PowerShell otherwise emits it as one object.
        $results = @(([IO.File]::ReadAllText($resultsPath) | ConvertFrom-Json) | ForEach-Object { $_ })
    }
    $entry = [pscustomobject][ordered]@{
        Suite = $suite.Directory
        File = $suite.File
        StartedUtc = $started.ToString('o')
        FinishedUtc = $finished.ToString('o')
        ExitCode = $process.ExitCode
        ResultsWritten = [IO.File]::Exists($resultsPath)
        Pass = @($results | Where-Object Outcome -CEQ 'PASS').Count
        Fail = @($results | Where-Object Outcome -CEQ 'FAIL').Count
        Observed = @($results | Where-Object Outcome -CEQ 'OBSERVED').Count
    }
    $summary.Add($entry)
    Write-Output ('{0}: exit={1} pass={2} fail={3} observed={4}' -f
        $entry.Suite, $entry.ExitCode, $entry.Pass, $entry.Fail, $entry.Observed)
}

$json = ConvertTo-Json -InputObject @($summary.ToArray()) -Depth 4
[IO.File]::WriteAllText($summaryPath, $json + "`n", (New-Object Text.UTF8Encoding($false)))
$broken = @($summary | Where-Object { $_.ExitCode -ne 0 -or -not $_.ResultsWritten -or $_.Fail -gt 0 })
if ($broken.Count -gt 0) {
    exit 1
}
exit 0
