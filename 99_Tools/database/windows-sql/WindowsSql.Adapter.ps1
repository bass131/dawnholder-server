. (Join-Path $PSScriptRoot 'WindowsSql.Runtime.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.PathLease.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Store.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Certificate.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Instance.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Sql.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Verify.ps1')

function New-WindowsSqlAdapter {
    param($Plan, [string]$RuntimePath)
    $runtime = Read-WindowsSqlRuntime -Plan $Plan -Relative $RuntimePath
    Assert-WindowsSqlExecutor -Plan $Plan -Runtime $runtime
    $sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')).TrimEnd('\', '/')
    if ($Plan.SourceRoot -ine $sourceRoot) { throw 'WINDOWS_SQL_SOURCE_ROOT_MISMATCH' }
    $adapter = [pscustomobject]@{
        Kind = 'WindowsLocal'
        Plan = $Plan
        Runtime = $runtime
        RuntimePath = $RuntimePath
        Lease = $null
        DirectoryLease = $null
        Mutex = $null
        MutexOwned = $false
        Master = $null
        Sources = $null
        ExpectedRegistry = $null
        LastTls = $null
    }
    try {
        $adapter.Mutex = [Threading.Mutex]::new($false,
            ('Global\Dawnholder.WindowsSql.' + $Plan.MachineName + '.' + $Plan.InstanceName))
        try { $adapter.MutexOwned = $adapter.Mutex.WaitOne(0) }
        catch [Threading.AbandonedMutexException] {
            $adapter.MutexOwned = $true
            # Durable journal, not abandonment, decides whether a new operation is safe.
        }
        if (-not $adapter.MutexOwned) { throw 'WINDOWS_SQL_BUSY' }
        $paths = @($Plan.SourceRoot, $Plan.PrivateRoot) + @($Plan.Inputs | ForEach-Object {
                Join-Path $Plan.SourceRoot $_.Path
            })
        $adapter.DirectoryLease = Open-WindowsSqlDirectoryLease -Paths $paths
        $adapter.Lease = Open-WindowsSqlInputLease -Plan $Plan -RuntimePath $RuntimePath
        Assert-WindowsSqlPrivateAcl -Path $Plan.PrivateRoot -Sid $runtime.ExecutorSid
        $childEnvironment = @{
            TEMP = $Plan.PrivateRoot
            TMP = $Plan.PrivateRoot
            LOCALAPPDATA = $env:LOCALAPPDATA
        }
        $output = Invoke-WindowsSqlChild -FileName 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
            -Arguments @('-NoLogo', '-NoProfile', '-NonInteractive', '-File',
            (Join-Path $PSScriptRoot 'Read-WindowsSqlSources.ps1')) -Environment $childEnvironment
        $adapter.Sources = ConvertFrom-Json -InputObject $output -Depth 30
        if ($adapter.Sources.Version -ne 1 -or $adapter.Sources.PowerShellVersion -notmatch '^5\.1\.' -or
            $adapter.Sources.Sources.Count -ne 4 -or $adapter.Sources.Bundle.ReleaseVersion -ne 4) {
            throw 'WINDOWS_SQL_SOURCE_READER_FAILED'
        }
        Assert-WindowsSqlProfile -Adapter $adapter
        $adapter | Add-Member -MemberType ScriptMethod -Name InvokeOperation -Value {
            param([string]$Operation, $Request)
            Invoke-WindowsSqlNativeOperation -Adapter $this -Operation $Operation -Request $Request
        }
        return $adapter
    }
    catch {
        Close-WindowsSqlAdapter -Adapter $adapter
        throw
    }
}

function Close-WindowsSqlAdapter {
    param($Adapter)
    if ($null -ne $Adapter.Master) { $Adapter.Master.Dispose() }
    if ($null -ne $Adapter.Lease) { foreach ($stream in $Adapter.Lease) { $stream.Dispose() } }
    if ($null -ne $Adapter.DirectoryLease) { $Adapter.DirectoryLease.Dispose() }
    if ($Adapter.MutexOwned) { $Adapter.Mutex.ReleaseMutex() }
    if ($null -ne $Adapter.Mutex) { $Adapter.Mutex.Dispose() }
}

function Invoke-WindowsSqlNativeOperation {
    param($Adapter, [string]$Operation, $Request)
    Assert-WindowsSqlExecutor -Plan $Adapter.Plan -Runtime $Adapter.Runtime
    $plan = $Adapter.Plan
    if ($Operation -ceq 'WriteLifecycleState') {
        if ($Request.PlanHash -cne $plan.PlanHash -or $Request.LifetimeId -cne $plan.LifetimeId) {
            throw 'WINDOWS_SQL_RECEIPT_MISMATCH'
        }
        Write-WindowsSqlPrivateJson -Adapter $Adapter -Name 'lifecycle.json' -Value $Request
        return [pscustomobject]@{ Completed = $true }
    }
    foreach ($name in @('MachineName', 'InstanceName', 'InstanceId', 'Database', 'LifetimeId', 'Port')) {
        if ($Request[$name] -cne $plan.$name) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
    }
    if ($Request.Login -cne $plan.RuntimeLogin) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
    switch -CaseSensitive ($Operation) {
        'ReadMachineIdentity' { return [pscustomobject]@{ MachineName = [Environment]::MachineName } }
        'ReadInstanceIdentity' { return Read-WindowsSqlInstanceIdentity -Adapter $Adapter }
        'ReadInstanceConfiguration' {
            $raw = Read-WindowsSqlRegistry -Adapter $Adapter
            $Adapter.ExpectedRegistry = $raw
            return ConvertTo-WindowsSqlConfiguration -Registry $raw
        }
        'ReadPortOwner' { return Read-WindowsSqlPort -Adapter $Adapter }
        'ReadNetworkMode' { return Read-WindowsSqlNetwork -Adapter $Adapter }
        'ReadTargetDatabase' { return Read-WindowsSqlDatabase -Adapter $Adapter }
        'ReadTargetLogin' { return Read-WindowsSqlLogin -Adapter $Adapter }
        'ReadLifecycleState' { return Read-WindowsSqlPrivateJson -Adapter $Adapter -Name 'lifecycle.json' }
        'VerifyTlsConnection' {
            $Adapter.LastTls = Invoke-WindowsSqlWslVerification -Adapter $Adapter
            return [pscustomobject]@{ Encrypted = $true; CertificateValidated = $true }
        }
        'VerifyTargetCatalog' {
            if ($null -eq $Adapter.LastTls) { throw 'WINDOWS_SQL_TLS_VERIFY_FAILED' }
            Assert-WindowsSqlCatalog -Adapter $Adapter
            return [pscustomobject]@{ CatalogMatches = $true; SchemaVersion = 4 }
        }
        'SaveConfigurationSnapshot' {
            Assert-WindowsSqlRegistryUnchanged -Adapter $Adapter
            $snapshot = [pscustomobject]@{
                LifetimeId = $plan.LifetimeId
                PlanHash = $plan.PlanHash
                Registry = $Adapter.ExpectedRegistry | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json
                ServiceAccount = $Adapter.Runtime.ServiceAccount
            }
            if ($null -ne (Read-WindowsSqlPrivateJson -Adapter $Adapter -Name 'snapshot.json')) {
                throw 'WINDOWS_SQL_SNAPSHOT_EXISTS'
            }
            Write-WindowsSqlPrivateJson -Adapter $Adapter -Name 'snapshot.json' -Value $snapshot
            return $snapshot
        }
        default {
            $known = @('SetMixedAuthentication', 'SetTcpPort', 'SetServerCertificate', 'RestartService',
                'AddFirewallRule', 'CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin',
                'RemoveRuntimeLogin', 'RemoveDatabase', 'RemoveFirewallRule', 'RestoreConfiguration')
            if ($Operation -cnotin $known) { throw 'WINDOWS_SQL_OPERATION_INVALID' }
            $journal = Read-WindowsSqlPrivateJson -Adapter $Adapter -Name 'lifecycle.json'
            if ($null -eq $journal -or $journal.Status -cne 'Applying' -or
                $journal.PendingOperation -cne $Operation) { throw 'WINDOWS_SQL_JOURNAL_REQUIRED' }
            Assert-WindowsSqlApproval -Plan $plan
            Assert-WindowsSqlInputs -Plan $plan
            Assert-WindowsSqlRegistryUnchanged -Adapter $Adapter
            if ($Operation -cin @('CreateDatabase', 'ApplyMigrations', 'CreateRuntimeLogin',
                    'RemoveRuntimeLogin', 'RemoveDatabase')) {
                Invoke-WindowsSqlDatabaseMutation -Adapter $Adapter -Operation $Operation
            }
            else { Invoke-WindowsSqlInstanceMutation -Adapter $Adapter -Operation $Operation -Request $Request }
            return [pscustomobject]@{ Completed = $true }
        }
    }
}
