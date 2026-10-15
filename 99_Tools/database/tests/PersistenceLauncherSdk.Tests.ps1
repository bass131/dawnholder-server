#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    [string]$LauncherPath = (Join-Path $PSScriptRoot '../../Start-PersistenceServer.ps1')
)

# Execute the launcher's own preflight/argv and shutdown statements. WSL and credential import are
# throwing/recording boundaries; lifetime probes use only owned PowerShell children, never a server.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'persistence-launcher-sdk'
$launcher = (Resolve-Path -LiteralPath $LauncherPath).Path
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($launcher, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Launcher must parse before its execution paths can be tested.' }
$definitions = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.FunctionDefinitionAst]
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
$mainTry = @($ast.EndBlock.Statements | Where-Object {
        $_ -is [Management.Automation.Language.TryStatementAst]
    })
if ($mainTry.Count -ne 1) { throw 'Expected the launcher owning try block.' }
$mainTry = $mainTry[0]
$rootAssignment = @($mainTry.FindAll({
            param($node)
            $node -is [Management.Automation.Language.AssignmentStatementAst] -and
            $node.Left.Extent.Text -ceq '$projectRoot'
        }, $true))
if ($rootAssignment.Count -ne 1) { throw 'Expected one production source-root assignment.' }
# A scriptblock has no file-backed PSScriptRoot. Substitute only that location boundary.
$preflight = $mainTry.Extent.Text.Replace($rootAssignment[0].Extent.Text, '$projectRoot = $fixtureProjectRoot')
$fixtureProjectRoot = [IO.Path]::GetFullPath((Join-Path $script:ToolRoot '../..'))
$argvAssignment = @($mainTry.FindAll({
            param($node)
            $node -is [Management.Automation.Language.AssignmentStatementAst] -and
            $node.Left.Extent.Text -ceq '$childArguments'
        }, $true))
if ($argvAssignment.Count -ne 1) { throw 'Expected one production child argv assignment.' }
$handoff = @($mainTry.Body.Statements | Where-Object {
        $_ -is [Management.Automation.Language.TryStatementAst] -and $_.Extent.Text.Contains('Import-Clixml')
    })
if ($handoff.Count -ne 1) { throw 'Expected the credential handoff boundary.' }
$shutdown = @($mainTry.Body.Statements | Where-Object {
        $_.Extent.StartOffset -gt $handoff[0].Extent.EndOffset
    } | ForEach-Object { $_.Extent.Text }) -join [Environment]::NewLine
if ([string]::IsNullOrWhiteSpace($shutdown)) { throw 'Missing production shutdown path.' }
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'production-functions.ps1'), $definitions)
[IO.File]::WriteAllText((Join-Path $script:SuiteRoot 'production-shutdown.ps1'), $shutdown)

$profile = Join-Path $script:SuiteRoot 'fixture-profile.json'
[IO.File]::WriteAllText($profile, '{}')
$expectedSdk = '/fixture/sdk with spaces/$literal/dotnet'
$expectedClone = '/fixture/clone with spaces/$literal'

function Invoke-LauncherPreflightFixture {
    param([string]$Mode)

    $PSScriptRoot = Split-Path $launcher
    $ProfilePath = $profile
    $CredentialPath = 'fixture-credential-must-not-be-read'
    $PublicCaPath = ''
    $ExpectedPublicCaHash = ''
    $EmptyCaDirectory = ''
    $runtimeProcess = $null
    $credential = $null
    $passwordPointer = [IntPtr]::Zero
    $passwordText = $null
    $payload = $null
    $envelope = $null
    $observed = [pscustomobject]@{
        CredentialReached = $false
        Metadata = [Collections.Generic.List[object]]::new()
        SdkRequests = 0
        MainError = ''
    }
    function wsl.exe {
        $observed.Metadata.Add(@($args))
        $global:LASTEXITCODE = 0
        switch ($args[3]) {
            'wslpath' {
                if ($args[-1] -ceq $profile) { return '/fixture/profile.json' }
                return '/fixture/source'
            }
            'realpath' { return $args[-1] }
            'cat' { return '/fixture/source' }
            'bash' {
                if ($args[-1] -ceq 'path') { return $expectedClone }
                $observed.SdkRequests++
                if ($Mode -ceq 'version-mismatch') {
                    $global:LASTEXITCODE = 1
                    return 'SDK fixture rejected the observed version.'
                }
                if ($Mode -ceq 'relative-result') { return 'dotnet' }
                return $expectedSdk
            }
            default { throw 'Unexpected offline WSL call.' }
        }
    }
    function Import-Clixml {
        $observed.CredentialReached = $true
        throw 'Fixture credential boundary: no credential was read.'
    }
    . ([scriptblock]::Create($definitions))
    try { . ([scriptblock]::Create($preflight)) } catch { $observed.MainError = $_.Exception.Message }
    $argv = @()
    if ($Mode -ceq 'selected') {
        # Actual production expression, with the public child-CA prefix as an independent fixture.
        $trustArguments = @('env', 'SSL_CERT_FILE=/fixture/public ca.pem', 'SSL_CERT_DIR=/fixture/empty')
        . ([scriptblock]::Create($argvAssignment[0].Extent.Text))
        $argv = @($childArguments)
    }
    return [pscustomobject]@{
        Mode = $Mode
        CredentialReached = $observed.CredentialReached
        SdkRequests = $observed.SdkRequests
        MainError = $observed.MainError
        Metadata = $observed.Metadata.ToArray()
        Argv = $argv
    }
}

$selected = Invoke-LauncherPreflightFixture -Mode selected
$mismatch = Invoke-LauncherPreflightFixture -Mode version-mismatch
$relative = Invoke-LauncherPreflightFixture -Mode relative-result
@($selected, $mismatch, $relative) | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $script:SuiteRoot 'preflight-observed.json')
$expectedArgv = @(
    '-d', 'Ubuntu', '--cd', $expectedClone, '--exec',
    'env', 'SSL_CERT_FILE=/fixture/public ca.pem', 'SSL_CERT_DIR=/fixture/empty',
    $expectedSdk, "$expectedClone/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll",
    '--persistence-profile', '/fixture/profile.json'
)
$sdkSelected = $selected.SdkRequests -eq 1 -and $selected.CredentialReached
$argvPreserved = ($selected.Argv | ConvertTo-Json -Compress) -ceq ($expectedArgv | ConvertTo-Json -Compress)
Assert-True -Name 'selected absolute SDK reaches the production child argv unchanged' -Condition (
    $sdkSelected -and $argvPreserved
) -Detail ('SDK requests={0}; argv={1}' -f $selected.SdkRequests, ($selected.Argv | ConvertTo-Json -Compress))
Assert-True -Name 'SDK validation failure stops before credential import' -Condition (
    $mismatch.SdkRequests -eq 1 -and -not $mismatch.CredentialReached
)
Assert-True -Name 'a relative SDK result cannot reach credential import' -Condition (
    $relative.SdkRequests -eq 1 -and -not $relative.CredentialReached
)

# A fresh process hosts the exact shutdown path with an owned child. Its stdin remains open:
# failure must return without any line, while the normal case receives one operator Enter.
$driverPath = Join-Path $script:SuiteRoot 'lifetime-driver.ps1'
$driver = @'
param([string]$Root, [string]$Mode)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $Root 'production-functions.ps1')
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = [Diagnostics.Process]::GetCurrentProcess().Path
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$program = if ($Mode -ceq 'failure') {
    '$null = [Console]::ReadLine(); Start-Sleep -Milliseconds 1200; exit 7'
} else {
    '$null = [Console]::ReadLine(); $line = [Console]::ReadLine(); if ($line -ceq "") { exit 0 }; exit 9'
}
foreach ($argument in @('-NoLogo', '-NoProfile', '-Command', $program)) { $start.ArgumentList.Add($argument) }
$runtimeProcess = [Diagnostics.Process]::Start($start)
$identity = [ordered]@{ Id = $runtimeProcess.Id; BirthUtc = $runtimeProcess.StartTime.ToUniversalTime().ToString('o') }
$identity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root ($Mode + '-child-start.json'))
$runtimeProcess.StandardInput.WriteLine('begin')
$runtimeProcess.StandardInput.Flush()
try {
    . (Join-Path $Root 'production-shutdown.ps1')
    exit 0
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
} finally {
    $runtimeProcess.StandardInput.Close()
    $returned = $runtimeProcess.WaitForExit(5000)
    [ordered]@{
        Id = $identity.Id
        BirthUtc = $identity.BirthUtc
        Returned = $returned
        ExitCode = $(if ($returned) { $runtimeProcess.ExitCode } else { $null })
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Root ($Mode + '-child-end.json'))
    $runtimeProcess.Dispose()
}
'@
[IO.File]::WriteAllText($driverPath, $driver)
foreach ($mode in @('failure', 'normal')) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = [Diagnostics.Process]::GetCurrentProcess().Path
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @('-NoLogo', '-NoProfile', '-File', $driverPath, $script:SuiteRoot, $mode)) {
        $start.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($start)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $returnedWithoutInput = $false
    try {
        if ($mode -ceq 'failure') { $returnedWithoutInput = $process.WaitForExit(5000) }
        if (-not $returnedWithoutInput) {
            # Also recovers the original blocking Read-Host mutant without killing it.
            $process.StandardInput.WriteLine()
            $process.StandardInput.Flush()
        }
        $returned = $process.WaitForExit(10000)
        if (-not $returned) {
            $process.StandardInput.Close()
            $returned = $process.WaitForExit(5000)
        }
        if (-not $returned) { throw "Owned fixture process did not return: $($process.Id)" }
        $watch.Stop()
        $observed = [ordered]@{
            Mode = $mode
            Id = $process.Id
            BirthUtc = $process.StartTime.ToUniversalTime().ToString('o')
            ReturnedWithoutInput = $returnedWithoutInput
            ExitCode = $process.ExitCode
            Milliseconds = $watch.Elapsed.TotalMilliseconds
            StdOut = $stdout.GetAwaiter().GetResult()
            StdErr = $stderr.GetAwaiter().GetResult()
        }
        $observed | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:SuiteRoot ($mode + '-driver.json'))
        $child = Get-Content -Raw (Join-Path $script:SuiteRoot ($mode + '-child-end.json')) | ConvertFrom-Json
        if ($mode -ceq 'failure') {
            $childFailureReported = $observed.StdErr.Contains(
                'Persistence server failed or shutdown is incomplete; inspect only safe server diagnostics.')
            Assert-True -Name 'failed child returns failure without operator input' -Condition (
                $returnedWithoutInput -and $observed.ExitCode -ne 0 -and $child.Returned -and
                $child.ExitCode -eq 7 -and $childFailureReported
            ) -Detail ($observed | ConvertTo-Json -Compress)
        } else {
            Assert-True -Name 'operator Enter preserves normal child shutdown' -Condition (
                $observed.ExitCode -eq 0 -and $child.Returned -and $child.ExitCode -eq 0
            ) -Detail ($observed | ConvertTo-Json -Compress)
        }
    } finally {
        $process.StandardInput.Close()
        $process.Dispose()
    }
}
Complete-TestSuite
