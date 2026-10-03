Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Open-LocalDatabase(
    [string]$Instance,
    [string]$Database
) {
    # Deliberately local Windows/shared memory only. No password argument or remote alias.
    if ($Instance -notmatch '^\.\\[A-Za-z0-9_]+$') {
        throw 'Instance must be .\LOCAL_INSTANCE.'
    }
    if ($Database -ne 'master' -and $Database -notmatch '^Dawnholder_Dev(?:_[A-Za-z0-9_]+)?$') {
        throw 'Only Dawnholder_Dev or Dawnholder_Dev_<suffix> is allowed.'
    }
    if ($Database.Length -gt 128) {
        throw 'Database identifier is too long.'
    }
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
        $machine = Invoke-DbScalar -Connection $connection -Sql "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('MachineName'))"
        if ($machine -ne $env:COMPUTERNAME) {
            throw 'SQL machine does not match this Windows computer.'
        }
        return $connection
    } catch {
        $connection.Dispose()
        throw
    }
}

function New-DbCommand(
    $Connection,
    [string]$Sql,
    [hashtable]$Parameters = @{},
    $Transaction = $null
) {
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    if ($null -ne $Transaction) {
        $command.Transaction = $Transaction
    }
    try {
        foreach ($key in $Parameters.Keys) {
            $value = $Parameters[$key]
            # The existing test/tool API uses concrete CLR values. Preserve that API while
            # selecting SqlDbType and variable-length Size here, before reaching the provider.
            if ($value -is [Guid]) {
                $type = [Data.SqlDbType]::UniqueIdentifier
                $size = 0
            }
            elseif ($value -is [int]) {
                $type = [Data.SqlDbType]::Int
                $size = 0
            }
            elseif ($value -is [long]) {
                $type = [Data.SqlDbType]::BigInt
                $size = 0
            }
            elseif ($value -is [byte]) {
                $type = [Data.SqlDbType]::TinyInt
                $size = 0
            }
            elseif ($value -is [bool]) {
                $type = [Data.SqlDbType]::Bit
                $size = 0
            }
            elseif ($value -is [string]) {
                $type = [Data.SqlDbType]::NVarChar
                $size = [Math]::Max(1, $value.Length)
            }
            elseif ($value -is [byte[]]) {
                $type = [Data.SqlDbType]::VarBinary
                $size = [Math]::Max(1, $value.Length)
            }
            else {
                throw 'SQL tool parameters need a supported explicit CLR value; no provider inference.'
            }
            $parameter = $command.Parameters.Add('@' + $key, $type)
            if ($size) {
                $parameter.Size = $size
            }
            $parameter.Value = $value
        }
        return $command
    } catch {
        $command.Dispose()
        throw
    }
}

function Invoke-DbScalar(
    $Connection,
    [string]$Sql,
    [hashtable]$Parameters = @{},
    $Transaction = $null
) {
    $command = New-DbCommand -Connection $Connection -Sql $Sql -Parameters $Parameters -Transaction $Transaction
    try {
        return $command.ExecuteScalar()
    } finally {
        $command.Dispose()
    }
}

function Invoke-DbNonQuery(
    $Connection,
    [string]$Sql,
    [hashtable]$Parameters = @{},
    $Transaction = $null
) {
    $command = New-DbCommand -Connection $Connection -Sql $Sql -Parameters $Parameters -Transaction $Transaction
    try {
        return $command.ExecuteNonQuery()
    } finally {
        $command.Dispose()
    }
}

function Assert-DatabaseOwner(
    $Connection
) {
    $owner = Invoke-DbScalar `
        -Connection $Connection `
        -Sql "SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'Dawnholder.DatabaseTool'"
    if ($owner -ne 'development-v1') {
        throw 'Database lacks the expected owner marker; refusing to adopt or modify it.'
    }
}

. (Join-Path $PSScriptRoot 'ModuleHash.Common.ps1')

. (Join-Path $PSScriptRoot 'Module.Common.ps1')

function Invoke-Migrations(
    $Connection,
    $Transaction = $null,
    [ValidateSet('Complete', 'Baseline001')][string]$Phase = 'Complete',
    $Contract = $null
) {
    # Complete requires the separately reviewed plan; default game DB and environment fallback stay closed.
    if ($Phase -eq 'Complete') {
        if ($null -eq $Contract) {
            throw 'Complete installation needs an explicit reviewed test-environment contract.'
        }
        Assert-TestEnvironmentTarget -Database $Connection.Database -Contract $Contract
        Assert-TestEnvironmentExecutionApproval -Contract $Contract
    }
    # Freeze reviewed inputs before any database mutation. No directory-discovered module execution.
    $sources = @(Get-DatabaseMigrationSources -Phase $Phase)
    $bundle = $null
    $catalog = $null
    if ($Phase -eq 'Complete') {
        $bundle = Read-ModuleBundle -DatabaseRoot $PSScriptRoot
        $catalog = Get-MigrationText -Path (Join-Path $PSScriptRoot 'verify-schema.sql')
    }
    $ownsTransaction = $null -eq $Transaction
    if ($ownsTransaction) {
        $Transaction = $Connection.BeginTransaction()
    }
    try {
        [void](Invoke-DbNonQuery `
                -Connection $Connection `
                -Sql @'
SET XACT_ABORT ON;
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = N'Dawnholder.SchemaMigration',
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 5000;
IF @result < 0
    THROW 51000, 'Could not acquire migration lock.', 1;
'@ `
                -Parameters @{
            } `
                -Transaction $Transaction)
        # First installation reads empty metadata; the SQL boundary always returns a JSON array.
        $historyText = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql @'
SELECT ISNULL((
    SELECT Version, Name, Checksum
    FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH
), N'[]') AS MigrationRows;
'@
        $history = @(ConvertFrom-DatabaseJsonRows -Json $historyText)
        Assert-DatabaseMigrationHistory -History $history -Sources $sources
        foreach ($source in $sources) {
            if ($source.Version -le $history.Count) {
                Write-Output "Migration $($source.Version) unchanged."
                continue
            }
            [void](Invoke-DbNonQuery -Connection $Connection -Sql $source.Sql -Transaction $Transaction)
            [void](Invoke-DbNonQuery `
                    -Connection $Connection `
                    -Sql 'INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash)' `
                    -Parameters @{
                    version = $source.Version
                    name = $source.Name
                    hash = $source.Checksum
                } `
                    -Transaction $Transaction)
            Write-Output "Migration $($source.Version) applied."
        }
        $finalHistoryText = Invoke-DbScalar -Connection $Connection -Transaction $Transaction -Sql @'
SELECT ISNULL((
    SELECT Version, Name, Checksum
    FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH
), N'[]') AS MigrationRows;
'@
        $finalHistory = @(ConvertFrom-DatabaseJsonRows -Json $finalHistoryText)
        Assert-DatabaseMigrationHistory -History $finalHistory -Sources $sources
        if ($finalHistory.Count -ne $sources.Count) { throw 'Incomplete migration history after application.' }
        if ($Phase -eq 'Complete') {
            # Only creating metadata for the first time permits an empty object/registration set.
            # Erasing both after an installed release must not turn into automatic adoption/repair.
            Invoke-ModuleBundle -Connection $Connection -Transaction $Transaction -Bundle $bundle `
                -AllowFirstInstall ($history.Count -lt 3) `
                -ReleaseAlreadyApplied ($history.Count -ge $bundle.ReleaseVersion)
            [void](Invoke-DbNonQuery -Connection $Connection -Sql $catalog -Transaction $Transaction)
        } else {
            # Phase-specific structural boundary, not the final four-version catalog or S01 PASS.
            [void](Invoke-DbNonQuery `
                    -Connection $Connection `
                    -Sql @'
IF (SELECT COUNT( * ) FROM dh.SchemaVersion) <> 1 OR
    NOT EXISTS(SELECT 1 FROM dh.SchemaVersion WHERE Version = 1 AND Name = N'001_initial.sql') OR
    OBJECT_ID(N'dh.Account', N'U') IS NULL OR OBJECT_ID(N'dh.Character', N'U') IS NULL OR
    OBJECT_ID(N'dh.CharacterProgress', N'U') IS NULL OR OBJECT_ID(N'dh.CharacterAuthority') IS NOT NULL OR
    OBJECT_ID(N'dh.CharacterOperation') IS NOT NULL OR DATABASE_PRINCIPAL_ID(N'dh_runtime') IS NOT NULL OR
    DATABASE_PRINCIPAL_ID(N'dh_recovery') IS NOT NULL
    THROW 51007, '001 phase boundary mismatch; final catalog has not been run.', 1;
'@ `
                    -Parameters @{
                } `
                    -Transaction $Transaction)
        }
        if ($ownsTransaction) {
            $Transaction.Commit()
        }
    } catch {
        if ($ownsTransaction -and $null -ne $Transaction.Connection) {
            $Transaction.Rollback()
        }
        throw
    } finally {
        if ($ownsTransaction) {
            $Transaction.Dispose()
        }
    }
}
