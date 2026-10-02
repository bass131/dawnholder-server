[CmdletBinding()]
param(
    [string]$Instance = '.\SQLEXPRESS',
    [string]$Database, [string]$ManifestPath,
    [ValidateSet('Complete','Baseline001')][string]$Phase = 'Complete'
)
function Invoke-D1bInstall {
    param([string]$Database, [string]$ManifestPath,
        [ValidateSet('Complete','Baseline001')][string]$Phase,
        [Data.SqlClient.SqlConnection]$Connection, [IO.FileStream]$ManifestLock)
    Assert-D1bTarget $Database
    if ($null -eq $ManifestLock -or -not $ManifestLock.CanWrite -or
        $ManifestLock.Name -ine ($ManifestPath+'.lock') -or $Connection.Database -cne $Database) {
        throw 'Installer core requires the lifecycle-owned manifest lock and exact connection.'
    }
    $manifest=Read-D1bManifest $Database $ManifestPath
    $pending=@($manifest.Steps | Where-Object Status -ne 'Done')
    if ($pending.Count -ne 1 -or $pending[0].Name -cne ('Install'+$Phase) -or $pending[0].Status -cne 'Pending') {
        throw 'Installer requires the recorded active D1b installation step.'
    }
    Assert-D1bMarkers $Connection $manifest
    . (Join-Path $PSScriptRoot 'Database.Common.ps1')
    Invoke-Migrations $Connection -Phase $Phase
}
if ($MyInvocation.InvocationName -eq '.') { return }
. (Join-Path $PSScriptRoot 'D1b.Common.ps1')
Assert-D1bTarget $Database $Instance
& (Join-Path $PSScriptRoot 'New-D1bTestDatabase.ps1') -Action Install -Database $Database -ManifestPath $ManifestPath -Phase $Phase
