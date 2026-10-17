#requires -Version 7.0
# A separate, explicit post-Cleanup action. Journal and public evidence are retained.
[CmdletBinding()]
param([string]$PlanPath, [string]$ExpectedPlanHash, [string]$RuntimePath)
if ($MyInvocation.InvocationName -eq '.') { return }
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WindowsSql.Common.ps1')
. (Join-Path $PSScriptRoot 'WindowsSql.Adapter.ps1')
$mutex = $null
$owned = $false
$lease = $null
$inputLease = $null
try {
    $plan = Read-WindowsSqlPlan -Path $PlanPath -ExpectedHash $ExpectedPlanHash
    if ([Environment]::MachineName -cne $plan.MachineName) { throw 'WINDOWS_SQL_IDENTITY_MISMATCH' }
    $runtime = Read-WindowsSqlRuntime -Plan $plan -Relative $RuntimePath
    Assert-WindowsSqlExecutor -Plan $plan -Runtime $runtime
    $sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')).TrimEnd('\', '/')
    if ($plan.SourceRoot -ine $sourceRoot) { throw 'WINDOWS_SQL_SOURCE_ROOT_MISMATCH' }
    $mutex = [Threading.Mutex]::new($false,
        ('Global\Dawnholder.WindowsSql.' + $plan.MachineName + '.' + $plan.InstanceName))
    try { $owned = $mutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $owned = $true }
    if (-not $owned) { throw 'WINDOWS_SQL_BUSY' }
    $paths = @($plan.SourceRoot, $plan.PrivateRoot) + @($plan.Inputs | ForEach-Object {
            Join-Path $plan.SourceRoot $_.Path
        })
    $lease = Open-WindowsSqlDirectoryLease -Paths $paths
    $inputLease = Open-WindowsSqlInputLease -Plan $plan -RuntimePath $RuntimePath
    $adapter = [pscustomobject]@{ Plan = $plan; Runtime = $runtime }
    $state = Read-WindowsSqlPrivateJson -Adapter $adapter -Name 'lifecycle.json'
    if ($null -eq $state -or $state.Status -cne 'Cleaned') { throw 'WINDOWS_SQL_CLEANUP_REQUIRED' }
    $registry = Read-WindowsSqlRegistry -Adapter $adapter
    if (-not (Test-WindowsSqlConfigurationEqual -Left (ConvertTo-WindowsSqlConfiguration -Registry $registry) `
                -Right $state.OriginalConfiguration) -or
        $registry.Certificate -ieq $plan.CertificateThumbprint) { throw 'WINDOWS_SQL_CONFIGURATION_CHANGED' }
    Assert-WindowsSqlCertificate -Adapter $adapter
    $receipt = Get-WindowsSqlResourceReceipt -Adapter $adapter
    if ($null -ne $receipt.CredentialHash) {
        Assert-WindowsSqlPrivateAcl -Path $runtime.CredentialPath -Sid $runtime.ExecutorSid
        if ((Get-FileHash -LiteralPath $runtime.CredentialPath).Hash -cne $receipt.CredentialHash) {
            throw 'WINDOWS_SQL_CREDENTIAL_CHANGED'
        }
    }
    $intent = [pscustomobject]@{ LifetimeId = $plan.LifetimeId; PlanHash = $plan.PlanHash; Status = 'Removing' }
    if ($null -ne (Read-WindowsSqlPrivateJson -Adapter $adapter -Name 'certificate-cleanup.json')) {
        throw 'WINDOWS_SQL_CERTIFICATE_CLEANUP_UNCERTAIN'
    }
    Write-WindowsSqlPrivateJson -Adapter $adapter -Name 'certificate-cleanup.json' -Value $intent
    Remove-Item -LiteralPath ('Cert:\LocalMachine\My\' + $plan.CertificateThumbprint) -DeleteKey -ErrorAction Stop
    if ($null -ne $receipt.CredentialHash) { Remove-Item -LiteralPath $runtime.CredentialPath -ErrorAction Stop }
    $intent.Status = 'Removed'
    Write-WindowsSqlPrivateJson -Adapter $adapter -Name 'certificate-cleanup.json' -Value $intent
    '{"Status":"PreparedCertificateRemoved"}'
    exit 0
}
catch {
    $code = $_.Exception.Message
    if ($code -cnotmatch '^WINDOWS_SQL_[A-Z0-9_]+$') { $code = 'WINDOWS_SQL_CERTIFICATE_CLEANUP_FAILED' }
    [Console]::Error.WriteLine($code)
    exit 1
}
finally {
    if ($null -ne $inputLease) { foreach ($stream in $inputLease) { $stream.Dispose() } }
    if ($null -ne $lease) { $lease.Dispose() }
    if ($owned) { $mutex.ReleaseMutex() }
    if ($null -ne $mutex) { $mutex.Dispose() }
}
