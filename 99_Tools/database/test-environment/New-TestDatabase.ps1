[CmdletBinding()]
param(
    [string]$ApprovalPlanPath,
    [string]$ExpectedApprovalPlanHash,
    [ValidateSet('OfflinePlan', 'Plan', 'Create', 'Install')][string]$Action = 'OfflinePlan',
    [string]$Database,
    [string]$ManifestPath,
    [ValidateSet('Baseline001', 'Complete')][string]$Phase = 'Baseline001'
)
if ($MyInvocation.InvocationName -eq '.') {
    return
}
. (Join-Path $PSScriptRoot 'Environment.Common.ps1')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$VerbosePreference = 'SilentlyContinue'
$DebugPreference = 'SilentlyContinue'
$Contract = Read-TestEnvironmentApprovalPlan -ApprovalPlanPath $ApprovalPlanPath -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
Assert-TestEnvironmentTarget -Contract $Contract -Database $Database
Assert-TestEnvironmentPath -Path $ManifestPath -Expected ($Contract).ManifestPath
if ($Action -eq 'OfflinePlan') {
    $Contract
    return
}
Assert-TestEnvironmentExecutor -Contract $Contract
$guard = Lock-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
$master = $null
$connection = $null
$manifest = $null
$stepName = $null
try {
    if ($Action -eq 'Plan') {
        if (Test-Path -LiteralPath $ManifestPath) {
            throw 'Manifest already exists; this one-time lifetime cannot be restarted.'
        }
        $manifest = New-TestEnvironmentManifest -Contract $Contract -Database $Database
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
        Write-Output 'Nonsecret plan recorded; no SQL/credential/account operation was performed.'
        return
    }
    $manifest = Read-TestEnvironmentManifest -Contract $Contract -Database $Database -ManifestPath $ManifestPath
    Assert-TestEnvironmentReadyForStep -Manifest $manifest
    $master = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database -Master
    if ($Action -eq 'Create') {
        if ($manifest.State -cne 'Planned' -or $null -ne $manifest.DatabaseIdentity) {
            throw 'Database creation is one-time and create-only.'
        }
        Assert-TestEnvironmentLocalAccountAbsent -Contract $Contract
        foreach ($path in @($manifest.PrivateDirectory, $manifest.IdentityDirectory)) {
            Assert-TestEnvironmentNoReparse -Path $path
            if (Test-Path -LiteralPath $path) {
                throw 'A target lifecycle directory already exists; no adoption.'
            }
        }
        $preflight = Invoke-DatabaseSql `
            -Connection $master `
            -Sql @'
SELECT (SELECT COUNT( * ) FROM sys.databases WHERE name = @database) DatabaseCount,
    (SELECT COUNT( * ) FROM sys.server_principals WHERE name IN (@runtime, @recovery)) LoginCount,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) ProductVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY('Collation')) ServerCollation,
    ORIGINAL_LOGIN() OriginalLogin;
'@ `
            -Parameters @{
                database = (New-DatabaseSqlParameter -Type NVarChar -Value $Database -Size 128)
                runtime = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RuntimeLogin -Size 128)
                recovery = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.RecoveryPrincipal -Size 128)
            } `
            -Result Rows
        $r = $preflight.Rows[0]
        if ($r.DatabaseCount -ne 0 -or $r.LoginCount -ne 0) {
            throw 'An exact database/login name is occupied; no adoption or rotation.'
        }
        $manifest.Engine = [pscustomobject]@{
            ProductVersion = [string]$r.ProductVersion
            ServerCollation = [string]$r.ServerCollation
            OriginalLogin = [string]$r.OriginalLogin
        }
        $stepName = 'CreateDatabase'
        Start-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $stepName `
            -Plan ([pscustomobject]@{
                Database = $Database
                Mode = 'CreateOnly'
                Lock = 'Dawnholder.Create.'+$Database
            })
        [void](Invoke-DatabaseSql `
            -Connection $master `
            -Sql @'
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock @Resource = @resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @DbPrincipal = 'public',
    @LockTimeout = 5000;
IF @lockResult < 0
    THROW 51100, 'Creation lock unavailable.', 1;
IF DB_ID(@database) IS NOT NULL
    THROW 51101, 'Exact database already exists.', 1;
-- Identifiers cannot be SQL parameters; QUOTENAME uses only the exact reviewed target.
DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@database) + N';';
EXEC sys.sp_executesql @stmt = @sql;
'@ `
            -Parameters @{
                resource = (New-DatabaseSqlParameter -Type NVarChar -Value ('Dawnholder.Create.'+$Database) -Size 255)
                database = (New-DatabaseSqlParameter -Type NVarChar -Value $Database -Size 128)
            } `
            -Result NonQuery)
        $manifest.DatabaseIdentity = Get-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
        Write-TestEnvironmentManifest -Contract $Contract -Manifest $manifest
        Complete-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $stepName `
            -Identity $manifest.DatabaseIdentity
        $stepName = 'InitializeDatabaseMarkers'
        Start-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $stepName `
            -Plan ([pscustomobject]@{
                OwnerMarker = 'development-v1'
                GoalMarker = $manifest.GoalMarker
            })
        $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database
        [void](Invoke-DatabaseSql `
            -Connection $connection `
            -Sql @'
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
EXEC sys.sp_addextendedproperty @name = N'Dawnholder.DatabaseTool',
    @value = N'development-v1';
EXEC sys.sp_addextendedproperty @name = N'Dawnholder.D1bGoal',
    @value = @goal;
COMMIT;
'@ `
            -Parameters @{
                goal = (New-DatabaseSqlParameter -Type NVarChar -Value $manifest.GoalMarker -Size 128)
            } `
            -Result NonQuery)
        Assert-TestEnvironmentMarkers -Connection $connection -Manifest $manifest
        $manifest.State = 'Created'
        Complete-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $stepName `
            -Identity ([pscustomobject]@{
                Owner = 'development-v1'
                Goal = $manifest.GoalMarker
            })
        Write-Output 'Exact database created and marked; no migration or game fixture has been installed.'
        return
    }
    Assert-TestEnvironmentDatabaseIdentity -Master $master -Manifest $manifest
    if (($Phase -eq 'Baseline001' -and $manifest.State -cne 'Created') -or
        ($Phase -eq 'Complete' -and $manifest.State -cne 'Baseline001')) {
            throw 'Install 001 first, leave its fixture to the independent verifier, then install 002+ in the same database.'
        }
    $connection = Open-TestEnvironmentDatabase -Contract $Contract -Manifest $manifest -Database $Database
    Assert-TestEnvironmentMarkers -Connection $connection -Manifest $manifest
    $stepName = 'Install'+$Phase
    Start-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $stepName `
        -Plan ([pscustomobject]@{
            Phase = $Phase
            GameFixture = 'Independent verifier owns it'
        })
    # The installer receives an explicit Database, never a game/environment default.
    . (Join-Path $PSScriptRoot '../Install-Database.ps1') `
        -Database $Database `
        -ManifestPath $ManifestPath `
        -Phase $Phase `
        -ApprovalPlanPath $ApprovalPlanPath `
        -ExpectedApprovalPlanHash $ExpectedApprovalPlanHash
    Invoke-TestEnvironmentInstall `
        -Contract $Contract `
        -Database $Database `
        -ManifestPath $ManifestPath `
        -Phase $Phase `
        -Connection $connection `
        -ManifestLock $guard
    $versions = Invoke-DatabaseSql `
        -Connection $connection `
        -Sql 'SELECT Version,Name,Checksum FROM dh.SchemaVersion ORDER BY Version' `
        -Result Rows
    $manifest.MigrationManifest = @(foreach ($row in $versions.Rows) {
        [pscustomobject]@{
            Version = [int]$row.Version
            Name = [string]$row.Name
            Checksum = [string]$row.Checksum
        }
    })
    $manifest.State = $(if ($Phase -eq 'Baseline001') {
        'Baseline001'
    } else {
        'Installed'
    })
    Complete-TestEnvironmentStep `
        -Contract $Contract `
        -Manifest $manifest `
        -Name $stepName `
        -Identity ([pscustomobject]@{
            Phase = $Phase
            MigrationManifest = $manifest.MigrationManifest
        })
    Write-Output "Install $Phase recorded; fixtures and independent SQL tests remain pending."
} catch {
    if ($null -ne $manifest -and $stepName) {
        Fail-TestEnvironmentStep `
            -Contract $Contract `
            -Manifest $manifest `
            -Name $stepName `
            -FailureCode (Get-DatabaseFailureCode -Exception $_.Exception)
    }
    throw 'Test environment database lifecycle stopped; preserve manifest and all resources. No automatic retry/cleanup.'
} finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
    if ($null -ne $master) {
        $master.Dispose()
    } # Pooling=false also releases any session creation lock.
    $guard.Dispose()
}
