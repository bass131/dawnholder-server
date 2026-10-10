[CmdletBinding()]
param(
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [string]$Database,
    [string]$ManifestPath
)
if ($MyInvocation.InvocationName -eq '.') {
    return
}
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'
$planArguments = @{
    ApprovalPlanPath = $ApprovalPlanPath
    ExpectedApprovalPlanHash = $ExpectedApprovalPlanHash
}
$Contract = Read-TestEnvironmentApprovalPlan @planArguments
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
Assert-TestEnvironmentExecutor -Contract $Contract
if ($PSVersionTable.PSEdition -ne 'Desktop' -or -not [Environment]::Is64BitProcess) {
    throw 'Use the approved 64-bit Windows PowerShell 5.1 executor; no module or runtime installation.'
}
$guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
$manifest = $null
$secret = $null
$credential = $null
$step = $null
try {
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    Assert-TestEnvironmentReadyForStep -Manifest $manifest
    if ($manifest.State -cne 'Planned' -or $null -ne $manifest.MasterFamilyGuid -or
        $null -ne $manifest.AdminCredentialHash) {
        throw 'Admin credential preparation is one-time and precedes container startup.'
    }
    foreach ($path in @($manifest.PrivateDirectory, $manifest.IdentityDirectory)) {
        Assert-TestEnvironmentNoReparse -Path $path
        if (Test-Path -LiteralPath $path) {
            throw 'Lifecycle path already exists; never read or rotate existing secrets.'
        }
    }
    $step = 'CreatePrivateDirectory'
    $plan = [pscustomobject]@{
        Path = $manifest.PrivateDirectory
        FullControl = @($manifest.ExecutorSid, 'S-1-5-18')
    }
    Start-TestEnvironmentStep -Contract $Contract -Manifest $manifest -Name $step -Plan $plan
    Set-TestEnvironmentDirectoryAcl `
        -Contract $Contract `
        -Path $manifest.PrivateDirectory `
        -ExecutorSid $manifest.ExecutorSid
    $identity = [pscustomobject]@{
        Path = $manifest.PrivateDirectory
        OwnerSid = $manifest.ExecutorSid
    }
    Complete-TestEnvironmentStep -Contract $Contract -Manifest $manifest -Name $step -Identity $identity
    $step = 'SaveAdminCredential'
    $secret = New-TestEnvironmentSecret
    $credential = [Management.Automation.PSCredential]::new($manifest.AdminLogin, $secret)
    Save-TestEnvironmentCredential -Kind Admin -Credential $credential -Manifest $manifest -Contract $Contract
    # No SQL or container operation belongs here. Only the card imports this file into its process environment.
    Write-Output 'Admin DPAPI credential recorded; container startup and SQL connection have not been performed.'
} catch {
    $failure = $_.Exception
    if ($null -ne $manifest -and $step) {
        Fail-TestEnvironmentStep -Contract $Contract -Manifest $manifest -Name $step -FailureCode (
            Get-DatabaseFailureCode -Exception $failure
        )
    }
    $summary = Get-TestEnvironmentFailureSummary -Exception $failure
    throw (('Admin credential preparation stopped. {0} ' +
        'Preserve partial files and manifest; no retry or rotation.') -f $summary)
} finally {
    if ($null -ne $secret) {
        $secret.Dispose()
    }
    $credential = $null
    $guard.Dispose()
}

