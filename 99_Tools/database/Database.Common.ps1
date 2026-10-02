Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Open-LocalDatabase([string]$Instance, [string]$Database) {
    # Deliberately local Windows/shared memory only. No password argument or remote alias.
    if ($Instance -notmatch '^\.\\[A-Za-z0-9_]+$') { throw 'Instance must be .\LOCAL_INSTANCE.' }
    if ($Database -ne 'master' -and $Database -notmatch '^Dawnholder_Dev(?:_[A-Za-z0-9_]+)?$') {
        throw 'Only Dawnholder_Dev or Dawnholder_Dev_<suffix> is allowed.'
    }
    if ($Database.Length -gt 128) { throw 'Database identifier is too long.' }
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source'] = 'lpc:' + $Instance
    $builder['Initial Catalog'] = $Database
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true # Local development shared memory only.
    $builder['Connect Timeout'] = 5
    $builder['Application Name'] = 'Dawnholder.DatabaseTool'
    $connection = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
    try {
        $connection.Open()
        $machine = Invoke-DbScalar $connection "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('MachineName'))"
        if ($machine -ne $env:COMPUTERNAME) { throw 'SQL machine does not match this Windows computer.' }
        return $connection
    } catch { $connection.Dispose(); throw }
}

function New-DbCommand($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null) {
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    if ($null -ne $Transaction) { $command.Transaction = $Transaction }
    foreach ($key in $Parameters.Keys) { [void]$command.Parameters.AddWithValue('@' + $key, $Parameters[$key]) }
    return $command
}

function Invoke-DbScalar($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null) {
    $command = New-DbCommand $Connection $Sql $Parameters $Transaction
    try { return $command.ExecuteScalar() } finally { $command.Dispose() }
}

function Invoke-DbNonQuery($Connection, [string]$Sql, [hashtable]$Parameters = @{}, $Transaction = $null) {
    $command = New-DbCommand $Connection $Sql $Parameters $Transaction
    try { return $command.ExecuteNonQuery() } finally { $command.Dispose() }
}

function Assert-DatabaseOwner($Connection) {
    $owner = Invoke-DbScalar $Connection "SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'Dawnholder.DatabaseTool'"
    if ($owner -ne 'development-v1') { throw 'Database lacks the expected owner marker; refusing to adopt or modify it.' }
}

function Get-MigrationText([string]$Path) {
    # Stable across Git CRLF/LF checkouts; BOM excluded by ReadAllText.
    return [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
}

function Get-MigrationHash([string]$Sql) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Sql)))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Invoke-Migrations($Connection, $Transaction = $null, [ValidateSet('Complete','Baseline001')][string]$Phase = 'Complete') {
    # 002+ may only enter the explicitly approved D1b database. No environment fallback.
    if ($Phase -eq 'Complete' -and $Connection.Database -cne 'Dawnholder_Dev_D1b_20261002') {
        throw '002+ installation requires the exact D1b target through its manifest-gated installer.'
    }
    $ownsTransaction = $null -eq $Transaction
    if ($ownsTransaction) { $Transaction = $Connection.BeginTransaction() }
    try {
        [void](Invoke-DbNonQuery $Connection @'
SET XACT_ABORT ON;
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource=N'Dawnholder.SchemaMigration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
IF @result < 0 THROW 51000, 'Could not acquire migration lock.', 1;
'@ @{} $Transaction)
        $files = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'migrations') -Filter '*.sql' | Sort-Object Name)
        if ($Phase -eq 'Baseline001') {
            $files = @($files | Where-Object Name -ceq '001_initial.sql')
            if ($files.Count -ne 1) { throw 'The immutable 001 baseline is missing.' }
        }
        $known = @()
        foreach ($file in $files) {
            if ($file.Name -notmatch '^(\d{3})_[a-z0-9_]+\.sql$') { throw "Invalid migration name: $($file.Name)" }
            $version = [int]$Matches[1]
            if ($version -in $known) { throw 'Duplicate migration version in files.' }
            $known += $version
            $sql = Get-MigrationText $file.FullName
            $hash = Get-MigrationHash $sql
            $applied = Invoke-DbScalar $Connection 'SELECT Checksum FROM dh.SchemaVersion WHERE Version=@version' @{version=$version} $Transaction
            if ($null -ne $applied -and $applied -isnot [DBNull]) {
                if ($applied -ne $hash) { throw "Migration $version checksum mismatch; add a new migration instead of editing history." }
                Write-Output "Migration $version unchanged."
                continue
            }
            [void](Invoke-DbNonQuery $Connection $sql @{} $Transaction)
            [void](Invoke-DbNonQuery $Connection 'INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash)' @{version=$version;name=$file.Name;hash=$hash} $Transaction)
            Write-Output "Migration $version applied."
        }
        $count = Invoke-DbScalar $Connection 'SELECT COUNT(*) FROM dh.SchemaVersion' @{} $Transaction
        if ($count -ne $known.Count) { throw 'Database has unknown migrations; use the matching tool revision.' }
        if ($Phase -eq 'Complete') {
            [void](Invoke-DbNonQuery $Connection (Get-MigrationText (Join-Path $PSScriptRoot 'verify-schema.sql')) @{} $Transaction)
        } else {
            # Phase-specific structural boundary, not the final 13-version catalog or S01 PASS.
            [void](Invoke-DbNonQuery $Connection @'
IF (SELECT COUNT(*) FROM dh.SchemaVersion)<>1 OR
 NOT EXISTS(SELECT 1 FROM dh.SchemaVersion WHERE Version=1 AND Name=N'001_initial.sql') OR
 OBJECT_ID(N'dh.Account',N'U') IS NULL OR OBJECT_ID(N'dh.Character',N'U') IS NULL OR
 OBJECT_ID(N'dh.CharacterProgress',N'U') IS NULL OR OBJECT_ID(N'dh.CharacterAuthority') IS NOT NULL OR
 OBJECT_ID(N'dh.CharacterOperation') IS NOT NULL OR DATABASE_PRINCIPAL_ID(N'dh_runtime') IS NOT NULL OR
 DATABASE_PRINCIPAL_ID(N'dh_recovery') IS NOT NULL
 THROW 51007, '001 phase boundary mismatch; final catalog has not been run.', 1;
'@ @{} $Transaction)
        }
        if ($ownsTransaction) { $Transaction.Commit() }
    } catch {
        if ($ownsTransaction -and $null -ne $Transaction.Connection) { $Transaction.Rollback() }
        throw
    } finally { if ($ownsTransaction) { $Transaction.Dispose() } }
}
