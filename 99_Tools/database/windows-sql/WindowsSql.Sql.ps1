. (Join-Path $PSScriptRoot '../Database.Common.ps1')

function Open-WindowsSqlAdminConnection {
    param($Adapter, [string]$Database = 'master')
    $builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder['Data Source'] = 'lpc:' + $Adapter.Plan.MachineName + '\' + $Adapter.Plan.InstanceName
    $builder['Initial Catalog'] = $Database
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $false
    $builder['TrustServerCertificate'] = $false
    $builder['Pooling'] = $false
    $builder['Persist Security Info'] = $false
    $builder['Connect Timeout'] = 5
    $builder['ConnectRetryCount'] = 0
    $builder['Application Name'] = 'Dawnholder.WindowsSql.Admin'
    $connection = [Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    try {
        $connection.Open()
        # This is the SAME connection used below. LPC failure is fatal; no TCP/NP/SQL-auth fallback.
        [void](Invoke-DbNonQuery -Connection $connection -Sql @'
IF CONVERT(nvarchar(128), CONNECTIONPROPERTY('net_transport')) <> N'Shared memory' OR
    CONVERT(nvarchar(128), SERVERPROPERTY('MachineName')) COLLATE Latin1_General_100_BIN2 <> @machine OR
    CONVERT(nvarchar(128), SERVERPROPERTY('InstanceName')) COLLATE Latin1_General_100_BIN2 <> @instance OR
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) <> @version OR IS_SRVROLEMEMBER(N'sysadmin') <> 1
    THROW 51200, 'Local administrator boundary mismatch.', 1;
'@ -Parameters @{
                machine = $Adapter.Plan.MachineName
                instance = $Adapter.Plan.InstanceName
                version = $Adapter.Plan.ExpectedProductVersion
            })
        return $connection
    }
    catch {
        $connection.Dispose()
        throw 'WINDOWS_SQL_LOCAL_CONNECTION_FAILED'
    }
}

function Get-WindowsSqlMaster {
    param($Adapter)
    if ($null -eq $Adapter.Master) {
        $Adapter.Master = Open-WindowsSqlAdminConnection -Adapter $Adapter
        [void](Invoke-DbNonQuery -Connection $Adapter.Master -Sql @'
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = @resource,
    @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0;
IF @result < 0 THROW 51201, 'Target is busy.', 1;
'@ -Parameters @{ resource = 'Dawnholder.WindowsSql.' + $Adapter.Plan.InstanceName })
    }
    return $Adapter.Master
}

function Get-WindowsSqlDatabaseGuid {
    param($Adapter)
    $master = Get-WindowsSqlMaster -Adapter $Adapter
    $value = Invoke-DbScalar -Connection $master -Sql @'
SELECT CONVERT(nvarchar(36), database_guid)
FROM sys.database_recovery_status WHERE database_id = DB_ID(@name);
'@ -Parameters @{ name = $Adapter.Plan.Database }
    if ($null -eq $value -or $value -is [DBNull]) { return $null }
    return [string]$value
}

function Read-WindowsSqlDatabase {
    param($Adapter)
    $master = Get-WindowsSqlMaster -Adapter $Adapter
    $exists = [int](Invoke-DbScalar -Connection $master -Sql `
            'SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END;' -Parameters @{ name = $Adapter.Plan.Database }) -eq 1
    $owner = $null
    $version = $null
    $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
    if ($exists -and $null -ne $receipt.DatabaseGuid) {
        if ((Get-WindowsSqlDatabaseGuid -Adapter $Adapter) -cne $receipt.DatabaseGuid) {
            throw 'WINDOWS_SQL_DATABASE_CHANGED'
        }
        $connection = Open-WindowsSqlAdminConnection -Adapter $Adapter -Database $Adapter.Plan.Database
        try {
            $owner = Invoke-DbScalar -Connection $connection -Sql @'
SELECT CONVERT(nvarchar(128), value) FROM sys.extended_properties
WHERE class = 0 AND name = N'Dawnholder.WindowsSql.Lifetime';
'@
            if ($owner -is [DBNull] -or $null -eq $owner) { $owner = $null }
            if ($owner -ceq $Adapter.Plan.LifetimeId) {
                $version = [int](Invoke-DbScalar -Connection $connection -Sql `
                        'SELECT COALESCE(MAX(Version), 0) FROM dh.SchemaVersion;')
            }
        }
        finally { $connection.Dispose() }
    }
    return [pscustomobject]@{
        Name = $Adapter.Plan.Database
        Exists = $exists
        OwnedLifetimeId = $owner
        SchemaVersion = $version
    }
}

function Read-WindowsSqlLogin {
    param($Adapter)
    $value = Invoke-DbScalar -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql @'
SELECT CONVERT(varchar(170), sid, 2) FROM sys.server_principals WHERE name = @name AND type = 'S';
'@ -Parameters @{ name = $Adapter.Plan.RuntimeLogin }
    $exists = [int](Invoke-DbScalar -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql `
            'SELECT CASE WHEN SUSER_ID(@name) IS NULL THEN 0 ELSE 1 END;' `
            -Parameters @{ name = $Adapter.Plan.RuntimeLogin }) -eq 1
    $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
    $owner = $null
    if ($exists -and $null -ne $receipt.LoginSid -and $value -ceq $receipt.LoginSid) {
        $owner = $Adapter.Plan.LifetimeId
    }
    return [pscustomobject]@{ Name = $Adapter.Plan.RuntimeLogin; Exists = $exists; OwnedLifetimeId = $owner }
}

function Open-WindowsSqlOwnedDatabase {
    param($Adapter)
    $observation = Read-WindowsSqlDatabase -Adapter $Adapter
    if (-not $observation.Exists -or $observation.OwnedLifetimeId -cne $Adapter.Plan.LifetimeId) {
        throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
    }
    return Open-WindowsSqlAdminConnection -Adapter $Adapter -Database $Adapter.Plan.Database
}

function Invoke-WindowsSqlFreshDatabase {
    param($Adapter)
    $master = Get-WindowsSqlMaster -Adapter $Adapter
    [void](Invoke-DbNonQuery -Connection $master -Sql @'
IF DB_ID(@name) IS NOT NULL OR SUSER_ID(@login) IS NOT NULL
    THROW 51202, 'Exact targets must be new.', 1;
DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt = @sql;
'@ -Parameters @{ name = $Adapter.Plan.Database; login = $Adapter.Plan.RuntimeLogin })
    $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
    $receipt.DatabaseGuid = Get-WindowsSqlDatabaseGuid -Adapter $Adapter
    if ($null -eq $receipt.DatabaseGuid) { throw 'WINDOWS_SQL_DATABASE_CHANGED' }
    Save-WindowsSqlResourceReceipt -Adapter $Adapter -Receipt $receipt
    $connection = Open-WindowsSqlAdminConnection -Adapter $Adapter -Database $Adapter.Plan.Database
    try {
        [void](Invoke-DbNonQuery -Connection $connection -Sql @'
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
EXEC sys.sp_addextendedproperty @name = N'Dawnholder.DatabaseTool', @value = N'development-v1';
EXEC sys.sp_addextendedproperty @name = N'Dawnholder.WindowsSql.Lifetime', @value = @lifetime;
COMMIT;
'@ -Parameters @{ lifetime = $Adapter.Plan.LifetimeId })
    }
    finally { $connection.Dispose() }
}

function Invoke-WindowsSqlMigrations {
    param($Adapter)
    $connection = Open-WindowsSqlOwnedDatabase -Adapter $Adapter
    $transaction = $null
    try {
        $transaction = $connection.BeginTransaction()
        [void](Invoke-DbNonQuery -Connection $connection -Transaction $transaction -Sql @'
SET XACT_ABORT ON;
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = N'Dawnholder.SchemaMigration',
    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
IF @result < 0 OR EXISTS(SELECT 1 FROM dh.SchemaVersion)
    THROW 51203, 'Fresh owned schema required; no repair.', 1;
'@)
        foreach ($source in $Adapter.Sources.Sources) {
            [void](Invoke-DbNonQuery -Connection $connection -Transaction $transaction -Sql $source.Sql)
            [void](Invoke-DbNonQuery -Connection $connection -Transaction $transaction -Sql @'
INSERT dh.SchemaVersion(Version, Name, Checksum) VALUES(@version, @name, @hash);
'@ -Parameters @{ version = [int]$source.Version; name = $source.Name; hash = $source.Checksum })
        }
        Invoke-ModuleBundle -Connection $connection -Transaction $transaction -Bundle $Adapter.Sources.Bundle `
            -AllowFirstInstall $true -ReleaseAlreadyApplied $false | Out-Null
        [void](Invoke-DbNonQuery -Connection $connection -Transaction $transaction -Sql $Adapter.Sources.Catalog)
        [void](Invoke-DbNonQuery -Connection $connection -Transaction $transaction -Sql @'
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1',
    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 2000;
IF @result < 0 OR EXISTS(SELECT 1 FROM dh.CharacterAuthority WITH (UPDLOCK, HOLDLOCK))
    THROW 51204, 'Fresh binding required.', 1;
INSERT dh.CharacterAuthority(SlotId, AccountId, CharacterId, Fence, OwnerKind, OwnerId, LastSequence, ChangedUtc)
VALUES(1, @account, @character, 0, 0, NULL, 0, SYSUTCDATETIME());
'@ -Parameters @{ account = [Guid]$Adapter.Runtime.AccountId; character = [Guid]$Adapter.Runtime.CharacterId })
        $transaction.Commit()
    }
    finally {
        if ($null -ne $transaction) {
            if ($null -ne $transaction.Connection) { $transaction.Rollback() }
            $transaction.Dispose()
        }
        $connection.Dispose()
    }
}

function Invoke-WindowsSqlCreateLogin {
    param($Adapter)
    $connection = Open-WindowsSqlOwnedDatabase -Adapter $Adapter
    $credential = $null
    $secure = $null
    $password = $null
    $credentialPath = $Adapter.Runtime.CredentialPath
    try {
        Assert-WindowsSqlPrivateAcl -Path $Adapter.Plan.PrivateRoot -Sid $Adapter.Runtime.ExecutorSid
        if ([IO.File]::Exists($credentialPath)) { throw 'WINDOWS_SQL_CREDENTIAL_ALREADY_EXISTS' }
        $password = 'Dh!9' + [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
        $secure = ConvertTo-SecureString -String $password -AsPlainText -Force
        $credential = [Management.Automation.PSCredential]::new($Adapter.Plan.RuntimeLogin, $secure)
        # CurrentUser DPAPI is persisted before SQL mutation; an orphan is retained, never silently regenerated.
        $credential | Export-Clixml -LiteralPath $credentialPath -NoClobber -ErrorAction Stop
        Set-WindowsSqlPrivateFileOwner -Path $credentialPath -Sid $Adapter.Runtime.ExecutorSid
        $sid = [Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
        [void](Invoke-DbNonQuery -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql @'
IF SUSER_ID(@name) IS NOT NULL THROW 51205, 'Login exists.', 1;
DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@name) + N' WITH PASSWORD=' +
    QUOTENAME(@password, '''') + N', SID=' + CONVERT(varchar(34), @sid, 1) +
    N', CHECK_POLICY=ON, CHECK_EXPIRATION=OFF, DEFAULT_DATABASE=' + QUOTENAME(@database) + N';';
EXEC sys.sp_executesql @stmt = @sql;
'@ -Parameters @{
                name = $Adapter.Plan.RuntimeLogin
                password = $password
                sid = $sid
                database = $Adapter.Plan.Database
            })
        $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
        $receipt.LoginSid = [Convert]::ToHexString($sid)
        $receipt.CredentialHash = (Get-FileHash -LiteralPath $credentialPath).Hash
        Save-WindowsSqlResourceReceipt -Adapter $Adapter -Receipt $receipt
        [void](Invoke-DbNonQuery -Connection $connection -Sql @'
DECLARE @sql nvarchar(max) = N'CREATE USER ' + QUOTENAME(@name) + N' FOR LOGIN ' + QUOTENAME(@name) +
    N'; ALTER ROLE dh_runtime ADD MEMBER ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt = @sql;
IF (SELECT sid FROM sys.database_principals WHERE name = @name) <> @sid
    THROW 51206, 'User identity differs.', 1;
'@ -Parameters @{ name = $Adapter.Plan.RuntimeLogin; sid = $sid })
    }
    finally {
        $password = $null
        $credential = $null
        if ($null -ne $secure) { $secure.Dispose() }
        $connection.Dispose()
    }
}

function Assert-WindowsSqlCatalog {
    param($Adapter)
    $connection = Open-WindowsSqlOwnedDatabase -Adapter $Adapter
    try {
        $history = Invoke-DbScalar -Connection $connection -Sql @'
SELECT ISNULL((SELECT Version, Name, Checksum FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH), N'[]');
'@
        Assert-DatabaseMigrationHistory -History @(ConvertFrom-DatabaseJsonRows -Json $history) `
            -Sources $Adapter.Sources.Sources
        if (@(ConvertFrom-DatabaseJsonRows -Json $history).Count -ne 4) { throw 'WINDOWS_SQL_CATALOG_VERIFY_FAILED' }
        Assert-DatabaseModuleState -State (Get-DatabaseModuleState -Connection $connection -Transaction $null) `
            -Bundle $Adapter.Sources.Bundle -RequireCurrentSource $true
        [void](Invoke-DbNonQuery -Connection $connection -Sql $Adapter.Sources.Catalog)
        [void](Invoke-DbNonQuery -Connection $connection -Sql @'
IF (SELECT COUNT(*) FROM dh.CharacterAuthority) <> 1 OR
    NOT EXISTS(SELECT 1 FROM dh.CharacterAuthority WHERE SlotId=1 AND AccountId=@account AND CharacterId=@character)
    THROW 51207, 'Binding mismatch.', 1;
IF (SELECT COUNT(*) FROM sys.database_role_members WHERE member_principal_id=USER_ID(@name)) <> 1 OR
    NOT EXISTS(SELECT 1 FROM sys.database_role_members
        WHERE member_principal_id=USER_ID(@name) AND role_principal_id=DATABASE_PRINCIPAL_ID(N'dh_runtime')) OR
    EXISTS(SELECT 1 FROM sys.database_permissions WHERE grantee_principal_id=USER_ID(@name)
        AND NOT(class=0 AND permission_name=N'CONNECT' AND state='G')) OR
    EXISTS(SELECT 1 FROM sys.schemas WHERE principal_id=USER_ID(@name))
    THROW 51208, 'Unexpected user authority.', 1;
'@ -Parameters @{ account = [Guid]$Adapter.Runtime.AccountId; character = [Guid]$Adapter.Runtime.CharacterId;
                name = $Adapter.Plan.RuntimeLogin })
        [void](Invoke-DbNonQuery -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql @'
IF EXISTS(SELECT 1 FROM sys.server_role_members WHERE member_principal_id=SUSER_ID(@name)) OR
    EXISTS(SELECT 1 FROM sys.server_permissions WHERE grantee_principal_id=SUSER_ID(@name)
        AND NOT(permission_name=N'CONNECT SQL' AND state='G'))
    THROW 51209, 'Unexpected login authority.', 1;
'@ -Parameters @{ name = $Adapter.Plan.RuntimeLogin })
    }
    finally { $connection.Dispose() }
}

function Invoke-WindowsSqlDatabaseMutation {
    param($Adapter, [string]$Operation)
    switch -CaseSensitive ($Operation) {
        'CreateDatabase' { Invoke-WindowsSqlFreshDatabase -Adapter $Adapter }
        'ApplyMigrations' { Invoke-WindowsSqlMigrations -Adapter $Adapter }
        'CreateRuntimeLogin' { Invoke-WindowsSqlCreateLogin -Adapter $Adapter }
        'RemoveRuntimeLogin' {
            $login = Read-WindowsSqlLogin -Adapter $Adapter
            if (-not $login.Exists -or $login.OwnedLifetimeId -cne $Adapter.Plan.LifetimeId) {
                throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
            }
            $connection = Open-WindowsSqlOwnedDatabase -Adapter $Adapter
            try {
                [void](Invoke-DbNonQuery -Connection $connection -Sql @'
DECLARE @sql nvarchar(max) = N'DROP USER ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt=@sql;
'@ -Parameters @{ name = $Adapter.Plan.RuntimeLogin })
            }
            finally { $connection.Dispose() }
            $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
            [void](Invoke-DbNonQuery -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql @'
IF CONVERT(varchar(170), SUSER_SID(@name), 2) <> @sid THROW 51210, 'Login changed.', 1;
DECLARE @sql nvarchar(max) = N'DROP LOGIN ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt=@sql;
'@ -Parameters @{ name = $Adapter.Plan.RuntimeLogin; sid = $receipt.LoginSid })
        }
        'RemoveDatabase' {
            $database = Read-WindowsSqlDatabase -Adapter $Adapter
            if (-not $database.Exists -or $database.OwnedLifetimeId -cne $Adapter.Plan.LifetimeId) {
                throw 'WINDOWS_SQL_TARGET_NOT_OWNED'
            }
            $receipt = Get-WindowsSqlResourceReceipt -Adapter $Adapter
            [void](Invoke-DbNonQuery -Connection (Get-WindowsSqlMaster -Adapter $Adapter) -Sql @'
IF NOT EXISTS(SELECT 1 FROM sys.database_recovery_status
    WHERE database_id=DB_ID(@name) AND database_guid=@guid)
    THROW 51211, 'Database changed.', 1;
DECLARE @sql nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@name) + N';';
EXEC sys.sp_executesql @stmt=@sql;
'@ -Parameters @{ name = $Adapter.Plan.Database; guid = [Guid]$receipt.DatabaseGuid })
        }
    }
}
