[CmdletBinding()]
param(
    [string]$Instance = $(if ($env:DAWNHOLDER_SQL_INSTANCE) { $env:DAWNHOLDER_SQL_INSTANCE } else { '.\SQLEXPRESS' }),
    [string]$Database = $(if ($env:DAWNHOLDER_SQL_DATABASE) { $env:DAWNHOLDER_SQL_DATABASE } else { 'Dawnholder_Dev' })
)
. (Join-Path $PSScriptRoot 'Database.Common.ps1')
# Validate the target before even opening master.
if ($Database -notmatch '^Dawnholder_Dev(?:_[A-Za-z0-9_]+)?$' -or $Database.Length -gt 128) { throw 'Invalid development database name.' }
$master = Open-LocalDatabase $Instance 'master'
$connection = $null
$lock = -999
try {
    # Serializes first creation too. Session lock is released when master closes.
    $lock = Invoke-DbScalar $master "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@name,@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=5000; SELECT @r;" @{name="Dawnholder.Create.$Database"}
    if ($lock -lt 0) { throw 'Could not acquire database creation lock.' }
    $exists = Invoke-DbScalar $master 'SELECT COUNT(*) FROM sys.databases WHERE name=@name' @{name=$Database}
    if ($exists -eq 0) {
        [void](Invoke-DbNonQuery $master "CREATE DATABASE [$Database]")
        $connection = Open-LocalDatabase $Instance $Database
        [void](Invoke-DbNonQuery $connection @'
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
COMMIT;
'@)
        Write-Output "Created local database $Database."
    } else { $connection = Open-LocalDatabase $Instance $Database }
    Assert-DatabaseOwner $connection
    Invoke-Migrations $connection
    Write-Output "PASS: $Instance / $Database schema installed and catalog verified."
} finally {
    if ($null -ne $connection) { $connection.Dispose() }
    # Pooling must not retain our session-owned application lock.
    try {
        if ($lock -ge 0) { [void](Invoke-DbNonQuery $master "EXEC sys.sp_releaseapplock @Resource=@name,@LockOwner='Session'" @{name="Dawnholder.Create.$Database"}) }
    } finally { $master.Dispose() }
}
