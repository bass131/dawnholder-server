[CmdletBinding()]
param(
    [string]$Instance = '',
    [string]$Database,
    [string]$ManifestPath,
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [ValidateSet('Complete', 'Baseline001')][string]$Phase = 'Complete'
)
function Invoke-TestEnvironmentInstall {
    param(
        $Contract,
        [string]$Database,
        [string]$ManifestPath,
        [ValidateSet('Complete', 'Baseline001')][string]$Phase,
        [Data.SqlClient.SqlConnection]$Connection,
        [IO.FileStream]$ManifestLock
    )
    Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
    if ($null -eq $ManifestLock -or -not $ManifestLock.CanWrite -or
        $ManifestLock.Name -ine ($ManifestPath + '.lock') -or $Connection.Database -cne $Database) {
        throw 'Installer core requires the lifecycle-owned manifest lock and exact connection.'
    }
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    $pending = @($manifest.Steps | Where-Object Status -ne 'Done')
    if ($pending.Count -ne 1 -or $pending[0].Name -cne ('Install' + $Phase) -or $pending[0].Status -cne 'Pending') {
        throw 'Installer requires the recorded active test-environment installation step.'
    }
    Assert-TestEnvironmentMarkers -Connection $Connection -Manifest $manifest
    . (Join-Path $PSScriptRoot 'Database.Common.ps1')
    Invoke-Migrations -Connection $Connection -Phase $Phase -Contract $Contract
}
if ($MyInvocation.InvocationName -eq '.') {
    return
}
. (Join-Path $PSScriptRoot 'test-environment/Environment.Common.ps1')
$Contract = Read-TestEnvironmentApprovalPlan `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database -Instance $Instance
& (Join-Path $PSScriptRoot 'test-environment/New-TestDatabase.ps1') `
    -Action Install `
    -Database $Database `
    -ManifestPath $ManifestPath `
    -Phase $Phase `
    -ApprovalPlanPath $ApprovalPlanPath `
    -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
