[CmdletBinding()]
param(
    [ValidateSet('OfflinePlan','Plan','Create','Install')][string]$Action = 'OfflinePlan',
    [string]$Database, [string]$ManifestPath, [string]$G2ApprovalMessage,
    [ValidateSet('Baseline001','Complete')][string]$Phase = 'Baseline001'
)
if ($MyInvocation.InvocationName -eq '.') { return }
. (Join-Path $PSScriptRoot 'D1b.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$VerbosePreference='SilentlyContinue'; $DebugPreference='SilentlyContinue'
Assert-D1bTarget $Database
Assert-D1bPath $ManifestPath (Get-D1bContract).ManifestPath
if ($Action -eq 'OfflinePlan') {
    Get-D1bContract
    return
}
Assert-D1bExecutor
$guard=Lock-D1bManifest $Database $ManifestPath
$master=$null; $connection=$null; $manifest=$null; $stepName=$null
try {
    if ($Action -eq 'Plan') {
        if (Test-Path -LiteralPath $ManifestPath) { throw 'Manifest already exists; this one-time lifetime cannot be restarted.' }
        $manifest=New-D1bManifest $Database $G2ApprovalMessage
        Write-D1bManifest $manifest
        Write-Output 'Nonsecret plan recorded; no SQL/credential/account operation was performed.'
        return
    }
    $manifest=Read-D1bManifest $Database $ManifestPath
    Assert-D1bReadyForStep $manifest
    $master=Open-D1bDatabase $manifest $Database -Master
    if ($Action -eq 'Create') {
        if ($manifest.State -cne 'Planned' -or $null -ne $manifest.DatabaseIdentity) { throw 'Database creation is one-time and create-only.' }
        Assert-D1bLocalAccountAbsent
        foreach ($path in @($manifest.PrivateDirectory,$manifest.IdentityDirectory)) {
            Assert-D1bNoReparse $path
            if (Test-Path -LiteralPath $path) { throw 'A target lifecycle directory already exists; no adoption.' }
        }
        $preflight=Invoke-D1bSql $master @'
SELECT (SELECT COUNT(*) FROM sys.databases WHERE name=@database) DatabaseCount,
 (SELECT COUNT(*) FROM sys.server_principals WHERE name IN (@runtime,@recovery)) LoginCount,
 CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) ProductVersion,
 CONVERT(nvarchar(128),SERVERPROPERTY('Collation')) ServerCollation, ORIGINAL_LOGIN() OriginalLogin;
'@ @{database=(New-D1bParameter NVarChar $Database 128); runtime=(New-D1bParameter NVarChar $manifest.RuntimeLogin 128)
        recovery=(New-D1bParameter NVarChar $manifest.RecoveryPrincipal 128)} -Result Rows
        $r=$preflight.Rows[0]
        if ($r.DatabaseCount -ne 0 -or $r.LoginCount -ne 0) { throw 'An exact database/login name is occupied; no adoption or rotation.' }
        $manifest.Engine=[pscustomobject]@{ProductVersion=[string]$r.ProductVersion; ServerCollation=[string]$r.ServerCollation
            OriginalLogin=[string]$r.OriginalLogin}
        $stepName='CreateDatabase'
        Start-D1bStep $manifest $stepName ([pscustomobject]@{Database=$Database; Mode='CreateOnly'; Lock='Dawnholder.Create.'+$Database})
        [void](Invoke-D1bSql $master @'
DECLARE @r int;
EXEC @r=sys.sp_getapplock @Resource=N'Dawnholder.Create.Dawnholder_Dev_D1b_20261002',
 @LockMode='Exclusive',@LockOwner='Session',@DbPrincipal='public',@LockTimeout=5000;
IF @r<0 THROW 51100, 'Creation lock unavailable.', 1;
IF DB_ID(N'Dawnholder_Dev_D1b_20261002') IS NOT NULL THROW 51101, 'Exact database already exists.', 1;
CREATE DATABASE [Dawnholder_Dev_D1b_20261002];
'@ -Result NonQuery)
        $manifest.DatabaseIdentity=Get-D1bDatabaseIdentity $master $manifest
        Write-D1bManifest $manifest
        Complete-D1bStep $manifest $stepName $manifest.DatabaseIdentity
        $stepName='InitializeDatabaseMarkers'
        Start-D1bStep $manifest $stepName ([pscustomobject]@{OwnerMarker='development-v1'; GoalMarker=$manifest.GoalMarker})
        $connection=Open-D1bDatabase $manifest $Database
        [void](Invoke-D1bSql $connection @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
EXEC(N'CREATE SCHEMA dh AUTHORIZATION dbo');
CREATE TABLE dh.SchemaVersion
(
 Version int NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
 Name nvarchar(128) NOT NULL CONSTRAINT UQ_SchemaVersion_Name UNIQUE,
 Checksum char(64) NOT NULL,
 AppliedUtc datetime2(3) NOT NULL CONSTRAINT DF_SchemaVersion_AppliedUtc DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_SchemaVersion_Version CHECK (Version > 0)
);
EXEC sys.sp_addextendedproperty @name=N'Dawnholder.DatabaseTool',@value=N'development-v1';
EXEC sys.sp_addextendedproperty @name=N'Dawnholder.D1bGoal',@value=@goal;
COMMIT;
'@ @{goal=(New-D1bParameter NVarChar $manifest.GoalMarker 128)} -Result NonQuery)
        Assert-D1bMarkers $connection $manifest
        $manifest.State='Created'
        Complete-D1bStep $manifest $stepName ([pscustomobject]@{Owner='development-v1'; Goal=$manifest.GoalMarker})
        Write-Output 'Exact database created and marked; no migration or game fixture has been installed.'
        return
    }
    Assert-D1bDatabaseIdentity $master $manifest
    if (($Phase -eq 'Baseline001' -and $manifest.State -cne 'Created') -or
        ($Phase -eq 'Complete' -and $manifest.State -cne 'Baseline001')) {
        throw 'Install 001 first, leave its fixture to the independent verifier, then install 002+ in the same database.'
    }
    $connection=Open-D1bDatabase $manifest $Database
    Assert-D1bMarkers $connection $manifest
    $stepName='Install'+$Phase
    Start-D1bStep $manifest $stepName ([pscustomobject]@{Phase=$Phase; GameFixture='Independent verifier owns it'})
    # The installer receives an explicit Database, never a game/environment default.
    . (Join-Path $PSScriptRoot 'Install-Database.ps1') -Database $Database -ManifestPath $ManifestPath -Phase $Phase
    Invoke-D1bInstall -Database $Database -ManifestPath $ManifestPath -Phase $Phase -Connection $connection -ManifestLock $guard
    $versions=Invoke-D1bSql $connection 'SELECT Version,Name,Checksum FROM dh.SchemaVersion ORDER BY Version' -Result Rows
    $manifest.MigrationManifest=@(foreach ($row in $versions.Rows) {
        [pscustomobject]@{Version=[int]$row.Version; Name=[string]$row.Name; Checksum=[string]$row.Checksum}
    })
    $manifest.State=$(if ($Phase -eq 'Baseline001') { 'Baseline001' } else { 'Installed' })
    Complete-D1bStep $manifest $stepName ([pscustomobject]@{Phase=$Phase; MigrationManifest=$manifest.MigrationManifest})
    Write-Output "Install $Phase recorded; fixtures and independent SQL tests remain pending."
} catch {
    if ($null -ne $manifest -and $stepName) { Fail-D1bStep $manifest $stepName (Get-D1bFailureCode $_.Exception) }
    throw 'D1b database lifecycle stopped; preserve manifest and all resources. No automatic retry/cleanup.'
} finally {
    if ($null -ne $connection) { $connection.Dispose() }
    if ($null -ne $master) { $master.Dispose() } # Pooling=false also releases any session creation lock.
    $guard.Dispose()
}
