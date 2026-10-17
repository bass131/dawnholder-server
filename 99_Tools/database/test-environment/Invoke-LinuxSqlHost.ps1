#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ApprovalPlanPath,
    [Parameter(Mandatory)][string]$ExpectedApprovalPlanHash,
    [Parameter(Mandatory)][ValidateSet('ready', 'bootstrap', 'runtime', 'sa-disabled')][string]$Operation
)

. (Join-Path $PSScriptRoot 'LinuxSql.Common.ps1')
$credential = $null
$runtimeText = $null
$adminText = $null
$envelope = $null
try {
    $plan = Read-LinuxSqlPlan -Path $ApprovalPlanPath -ExpectedHash $ExpectedApprovalPlanHash
    Assert-LinuxSqlPrepared -Plan $plan
    $owner = Get-Content -LiteralPath (Join-Path $plan.PrivateRoot 'owner.json') -Raw | ConvertFrom-Json
    if ($owner.LifetimeId -cne $plan.LifetimeId -or $owner.PlanHash -cne $plan.PlanHash) {
        throw 'LINUX_SQL_PRIVATE_OWNER_CHANGED'
    }
    $statePath = Join-Path $plan.EvidenceRoot 'container-created.json'
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.LifetimeId -cne $plan.LifetimeId -or $state.PlanHash -cne $plan.PlanHash -or
        $state.ContainerId -cnotmatch '^[a-f0-9]{64}$') { throw 'LINUX_SQL_CONTAINER_OWNER_MISSING' }
    $identity = Invoke-LinuxSqlProcess -FileName 'docker.exe' -Arguments @(
        'container', 'inspect', '--format',
        '{{.Id}}|{{index .Config.Labels "dawnholder.lifetime"}}|{{.State.Running}}', $state.ContainerId)
    if ($identity.ExitCode -ne 0 -or $identity.Output.Trim() -cne
        ($state.ContainerId + '|' + $plan.LifetimeId + '|true')) { throw 'LINUX_SQL_CONTAINER_NOT_RUNNING' }
    $planWsl = ConvertTo-LinuxSqlPath -Path $plan.PlanPath
    $privateWsl = ConvertTo-LinuxSqlPath -Path $plan.PrivateRoot
    # The approved execution boundary pins the host DLL and SDK version before reading credentials.
    $sourceWsl = ConvertTo-LinuxSqlPath -Path $plan.SourceRoot
    $ownerMarker = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments @(
        '-d', 'Ubuntu', '--exec', 'cat', ($plan.LinuxBuildRoot + '/.dawnholder-source'))
    if ($ownerMarker.ExitCode -ne 0 -or $ownerMarker.Output.TrimEnd("`r", "`n") -cne $sourceWsl) {
        throw 'LINUX_SQL_BUILD_OWNER_CHANGED'
    }
    $version = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments @(
        '-d', 'Ubuntu', '--cd', ($plan.LinuxBuildRoot + '/src'), '--exec', 'env',
        'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0', 'DOTNET_GENERATE_ASPNET_CERTIFICATE=false',
        'DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1', 'DOTNET_CLI_TELEMETRY_OPTOUT=1',
        ('DOTNET_CLI_HOME=' + $plan.LinuxBuildRoot + '/cli-home'), $plan.DotnetPath, '--version')
    if ($version.ExitCode -ne 0 -or $version.Error -or $version.Output.Trim() -cne $plan.DotnetVersion) {
        throw 'LINUX_SQL_SDK_VERSION_CHANGED'
    }
    $hash = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments @(
        '-d', 'Ubuntu', '--exec', 'sha256sum', '--', $plan.LinuxHostDll)
    $expected = $plan.LinuxHostSha256.ToLowerInvariant() + '  ' + $plan.LinuxHostDll
    if ($hash.ExitCode -ne 0 -or $hash.Error -or $hash.Output.TrimEnd("`r", "`n") -cne $expected) {
        throw 'LINUX_SQL_HOST_DLL_CHANGED'
    }
    if ($Operation -ne 'runtime') {
        $adminText = [IO.File]::ReadAllText((Join-Path $plan.PrivateRoot 'sa.txt'))
        if ($adminText -cnotmatch '^Dh1![0-9A-F]{64}$') { throw 'LINUX_SQL_ADMIN_SECRET_INVALID' }
    } else { $adminText = '' }
    if ($Operation -in @('bootstrap', 'runtime')) {
        $credential = Import-Clixml -LiteralPath (Join-Path $plan.PrivateRoot 'runtime.clixml')
        if ($credential -isnot [Management.Automation.PSCredential] -or
            $credential.UserName -cne $plan.RuntimeLogin -or $credential.Password.Length -ne 68) {
            throw 'LINUX_SQL_RUNTIME_SECRET_INVALID'
        }
        $runtimeText = $credential.GetNetworkCredential().Password
    } else { $runtimeText = '' }
    $envelope = @{ Version = 1; Admin = $adminText; Runtime = $runtimeText } | ConvertTo-Json -Compress
    $arguments = @(
        '-d', 'Ubuntu', '--exec', 'env', '-i', 'PATH=/usr/bin:/bin', 'DOTNET_EnableDiagnostics=0',
        'DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1', 'DOTNET_GENERATE_ASPNET_CERTIFICATE=false',
        'DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0', "SSL_CERT_FILE=$privateWsl/ca.pem", "SSL_CERT_DIR=$privateWsl/ca-empty",
        $plan.DotnetPath, $plan.LinuxHostDll, $planWsl, $plan.PlanHash, $Operation
    )
    $result = Invoke-LinuxSqlProcess -FileName 'wsl.exe' -Arguments $arguments `
        -InputText $envelope -Seconds $plan.HostSeconds -OutputLimit 4096
    foreach ($secret in @($adminText, $runtimeText)) {
        if ($secret -and ($result.Output.Contains($secret) -or $result.Error.Contains($secret))) {
            throw 'LINUX_SQL_SECRET_OUTPUT_DETECTED'
        }
    }
    if ($result.Error) { throw 'LINUX_SQL_CHILD_STDERR_SUPPRESSED' }
    try {
        $report = ConvertFrom-Json -InputObject $result.Output -NoEnumerate -ErrorAction Stop
    } catch {
        throw 'LINUX_SQL_HOST_REPORT_INVALID'
    }
    if ($report -isnot [pscustomobject] -or $null -eq $report.PSObject.Properties['Status'] -or
        $report.Status -isnot [string] -or $report.Status -cnotin @('Succeeded', 'Failed')) {
        throw 'LINUX_SQL_HOST_REPORT_INVALID'
    }
    # The known host only emits this bounded schema. Never forward arbitrary child properties.
    if ($report.Status -cne 'Succeeded' -or $result.ExitCode -ne 0) {
        if ($report.Status -cne 'Failed' -or $result.ExitCode -ne 2 -or
            $null -eq $report.PSObject.Properties['Kind'] -or
            $report.Kind -isnot [string] -or $report.Kind -cne 'Sql') {
            throw 'LINUX_SQL_HOST_BOUNDARY_FAILURE'
        }
        # ConvertFrom-Json emits Int64 integers; reject coercion and preserve SqlException's signed Int32 value.
        if ($null -eq $report.PSObject.Properties['Code'] -or
            ($report.Code -isnot [long] -and $report.Code -isnot [int]) -or
            $report.Code -lt [int]::MinValue -or $report.Code -gt [int]::MaxValue) {
            throw 'LINUX_SQL_HOST_REPORT_INVALID'
        }
        $number = [int]$report.Code
        throw ('LINUX_SQL_HOST_FAILED_' + $number.ToString([Globalization.CultureInfo]::InvariantCulture))
    }
    if ($report.Operation -cne $Operation) { throw 'LINUX_SQL_HOST_OPERATION_CHANGED' }
    [pscustomobject]@{
        Operation = $Operation; Status = 'Succeeded'; ExitCode = $result.ExitCode
        Pid = $result.Pid; BirthUtc = $result.BirthUtc; ElapsedMs = $result.ElapsedMs
        Observation = $report
    }
} catch {
    # Prevent PowerShell's inner ErrorRecord from serializing credentials or raw native diagnostics.
    $safe = [string]$_.Exception.Message
    if ($safe -cnotmatch '^LINUX_SQL_(?:[A-Z0-9_]+|HOST_FAILED_-[0-9]+)$') {
        $safe = 'LINUX_SQL_HOST_HANDOFF_FAILED'
    }
    throw [InvalidOperationException]::new($safe)
} finally {
    if ($credential -is [Management.Automation.PSCredential]) { $credential.Password.Dispose() }
    $credential = $null
    $adminText = $null
    $runtimeText = $null
    $envelope = $null
}
