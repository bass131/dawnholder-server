using System.Data;
using System.Security;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Tools.LinuxSqlHost;

// A fresh-container installer, not an upgrade/repair API. Failure leaves partial resources for the owner.
internal sealed class SqlLifecycle(HostPlan plan, CancellationToken cancellation)
{
    internal async Task<object> ReadyAsync(CredentialEnvelope credentials)
    {
        using SqlConnection master = await OpenAsync("master", "sa", credentials.Admin);
        await AssertEngineAsync(master);
        return new { Status = "Succeeded", Operation = "ready" };
    }

    internal async Task<object> BootstrapAsync(CredentialEnvelope credentials)
    {
        if (credentials.Admin.Length < 16 || credentials.Runtime.Length < 16)
            throw new InvalidDataException();
        // Freeze every SQL source before connecting. HostPlan owns the reviewed bytes for this whole invocation.
        using SqlConnection master = await OpenAsync("master", "sa", credentials.Admin);
        await AssertEngineAsync(master);
        await ExecuteAsync(master, """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = @resource,
                @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 5000;
            IF @result < 0 OR DB_ID(@database) IS NOT NULL OR SUSER_ID(@login) IS NOT NULL
                THROW 51101, 'Fresh database/login required.', 1;
            DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@database) + N';';
            EXEC sys.sp_executesql @stmt = @sql;
            """, null, Text("resource", "Dawnholder.Create." + plan.Database, 255),
            Text("database", plan.Database), Text("login", plan.RuntimeLogin));
        using SqlConnection database = await OpenAsync(plan.Database, "sa", credentials.Admin);
        using (SqlTransaction transaction = database.BeginTransaction())
        {
            await ExecuteAsync(database, """
                SET XACT_ABORT ON;
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;
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
                EXEC sys.sp_addextendedproperty @name = N'Dawnholder.D1bGoal', @value = @goal;
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'Dawnholder.SchemaMigration',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
                IF @result < 0 THROW 51000, 'Migration lock unavailable.', 1;
                """, transaction, Text("goal", plan.LifetimeId));
            for (int index = 0; index < HostPlan.Migrations.Length; index++)
            {
                string name = HostPlan.Migrations[index];
                string sql = plan.Source("migrations/" + name);
                await ExecuteAsync(database, sql, transaction);
                await ExecuteAsync(database,
                    "INSERT dh.SchemaVersion(Version,Name,Checksum) VALUES(@version,@name,@hash);", transaction,
                    new SqlParameter("@version", SqlDbType.Int) { Value = index + 1 },
                    Text("name", name), Text("hash", HostPlan.TextHash(sql), 64));
            }
            await ExecuteAsync(database, """
                IF (SELECT COUNT(*) FROM dh.ModuleRelease) <> 1 OR
                    NOT EXISTS(SELECT 1 FROM dh.ModuleRelease WHERE Version = 4 AND ManifestChecksum = @hash)
                    THROW 51008, 'Release differs from reviewed source.', 1;
                """, transaction, Text("hash", plan.ManifestHash, 64));
            foreach (ModuleInput module in plan.Modules)
            {
                await ExecuteAsync(database, plan.Source(module.Path), transaction);
                await ExecuteAsync(database, """
                    IF NOT EXISTS(
                        SELECT 1 FROM sys.objects o JOIN sys.sql_modules m ON m.object_id = o.object_id
                        JOIN sys.schemas s ON s.schema_id = o.schema_id
                        WHERE o.object_id = OBJECT_ID(@name) AND o.type = @kind
                            AND DATALENGTH(REPLACE(m.definition, NCHAR(13), N'')) = @bytes
                            AND CONVERT(char(64), HASHBYTES('SHA2_256', REPLACE(m.definition, NCHAR(13), N'')), 2) = @hash
                            AND m.execute_as_principal_id IS NULL AND m.uses_ansi_nulls = 1
                            AND m.uses_quoted_identifier = 1 AND (@kind <> N'FN' OR m.is_schema_bound = 1)
                            AND COALESCE(o.principal_id, s.principal_id) = DATABASE_PRINCIPAL_ID(N'dbo'))
                        THROW 51008, 'Actual module differs from reviewed definition.', 1;
                    INSERT dh.ModuleDefinition(ObjectName,Kind,SourceChecksum,DefinitionBytes,DefinitionChecksum)
                    VALUES(@name,@kind,@source,@bytes,@hash);
                    """, transaction, Text("name", module.Name), Text("kind", module.Kind, 2),
                    Text("source", module.SourceHash, 64), Text("hash", module.DefinitionHash, 64),
                    new SqlParameter("@bytes", SqlDbType.Int) { Value = module.DefinitionBytes });
            }
            await ExecuteAsync(database, plan.Source("modules/permissions.sql"), transaction);
            await ExecuteAsync(database, plan.Source("verify-schema.sql"), transaction);
            await ExecuteAsync(database, """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 2000;
                IF @result < 0 OR EXISTS(SELECT 1 FROM dh.CharacterAuthority WITH (UPDLOCK,HOLDLOCK))
                    THROW 51113, 'Fresh binding required.', 1;
                INSERT dh.CharacterAuthority(SlotId,AccountId,CharacterId,Fence,OwnerKind,OwnerId,LastSequence,ChangedUtc)
                VALUES(1,@account,@character,0,0,NULL,0,SYSUTCDATETIME());
                """, transaction, Id("account", plan.AccountId), Id("character", plan.CharacterId));
            transaction.Commit();
        }
        byte[] sid = RandomNumberGenerator.GetBytes(16);
        // Password is a typed parameter, never interpolated into caller-visible command text.
        nint pointer = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(credentials.Runtime);
        try
        {
            string password = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(pointer);
            await ExecuteAsync(master, """
                IF SUSER_ID(@name) IS NOT NULL THROW 51120, 'Login already exists.', 1;
                DECLARE @sql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@name) + N' WITH PASSWORD=' +
                    QUOTENAME(@password, '''') + N',SID=' + CONVERT(varchar(34), @sid, 1) +
                    N',CHECK_POLICY=ON,CHECK_EXPIRATION=OFF,DEFAULT_DATABASE=' + QUOTENAME(@database) + N';';
                EXEC sys.sp_executesql @stmt = @sql;
                """, null, Text("name", plan.RuntimeLogin), Text("password", password),
                new SqlParameter("@sid", SqlDbType.VarBinary, 16) { Value = sid }, Text("database", plan.Database));
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(pointer);
        }
        await ExecuteAsync(database, """
            DECLARE @sql nvarchar(max) = N'CREATE USER ' + QUOTENAME(@name) + N' FOR LOGIN ' + QUOTENAME(@name) +
                N'; ALTER ROLE dh_runtime ADD MEMBER ' + QUOTENAME(@name) + N';';
            EXEC sys.sp_executesql @stmt = @sql;
            IF (SELECT sid FROM sys.database_principals WHERE name = @name) <> @sid
                THROW 51121, 'Principal identity mismatch.', 1;
            IF (SELECT COUNT(*) FROM sys.database_role_members WHERE member_principal_id = USER_ID(@name)) <> 1 OR
                NOT EXISTS(SELECT 1 FROM sys.database_role_members
                    WHERE member_principal_id = USER_ID(@name) AND role_principal_id = USER_ID(N'dh_runtime')) OR
                EXISTS(SELECT 1 FROM sys.database_permissions WHERE grantee_principal_id = USER_ID(@name)
                    AND NOT(class = 0 AND permission_name = N'CONNECT' AND state = 'G')) OR
                EXISTS(SELECT 1 FROM sys.schemas WHERE principal_id = USER_ID(@name))
                THROW 51122, 'Unexpected direct user authority.', 1;
            """, null, Text("name", plan.RuntimeLogin), new SqlParameter("@sid", SqlDbType.VarBinary, 16) { Value = sid });
        await ExecuteAsync(master, """
            IF EXISTS(SELECT 1 FROM sys.server_role_members WHERE member_principal_id = SUSER_ID(@name)) OR
                EXISTS(SELECT 1 FROM sys.server_permissions WHERE grantee_principal_id = SUSER_ID(@name)
                    AND NOT(permission_name = N'CONNECT SQL' AND state = 'G'))
                THROW 51122, 'Unexpected login authority.', 1;
            """, null, Text("name", plan.RuntimeLogin));
        Guid databaseGuid = (Guid)(await ScalarAsync(master,
            "SELECT database_guid FROM sys.database_recovery_status WHERE database_id = DB_ID(@name);",
            Text("name", plan.Database)))!;
        // Observe binding and exact fresh empty game state before the administrator becomes unavailable.
        await ExecuteAsync(database, """
            IF (SELECT COUNT(*) FROM dh.CharacterAuthority) <> 1 OR
                NOT EXISTS(SELECT 1 FROM dh.CharacterAuthority WHERE SlotId = 1 AND AccountId = @account
                    AND CharacterId = @character AND OwnerKind = 0 AND OwnerId IS NULL AND Fence = 0 AND LastSequence = 0) OR
                EXISTS(SELECT 1 FROM dh.Account) OR EXISTS(SELECT 1 FROM dh.Character) OR EXISTS(SELECT 1 FROM dh.CharacterProgress)
                THROW 51123, 'Fresh binding or empty game state differs.', 1;
            """, null, Id("account", plan.AccountId), Id("character", plan.CharacterId));
        _ = await RuntimeAsync(credentials);
        await ExecuteAsync(master, "ALTER LOGIN sa DISABLE;", null);
        int disabled = Convert.ToInt32(await ScalarAsync(master,
            "SELECT CONVERT(int,is_disabled) FROM sys.server_principals WHERE name = N'sa';"));
        if (disabled != 1)
            throw new InvalidDataException();
        return new
        {
            Status = "Succeeded", Operation = "bootstrap", DatabaseGuid = databaseGuid,
            RuntimeSid = Convert.ToHexString(sid), SaDisabled = true,
            AccountId = plan.AccountId, CharacterId = plan.CharacterId, SchemaVersion = 4
        };
    }

    internal async Task<object> RuntimeAsync(CredentialEnvelope credentials)
    {
        using SqlConnection connection = await OpenAsync(plan.Database, plan.RuntimeLogin, credentials.Runtime);
        await ExecuteAsync(connection, """
            IF ORIGINAL_LOGIN() <> @name OR IS_SRVROLEMEMBER(N'sysadmin') <> 0 OR IS_MEMBER(N'db_owner') <> 0 OR
                IS_MEMBER(N'dh_runtime') <> 1 OR IS_MEMBER(N'dh_recovery') <> 0 OR
                HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') <> 0 OR
                HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') <> 0 OR
                HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'SELECT') <> 0 OR
                HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'INSERT') <> 0 OR
                HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'UPDATE') <> 0 OR
                HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'DELETE') <> 0
                THROW 51124, 'Runtime permissions differ.', 1;
            """, null, Text("name", plan.RuntimeLogin));
        int executeGrants = 0;
        foreach (string name in new[] { "ReadAdmission", "AcquireAndLoad", "WriteSafeCheckpoint", "ReleaseRuntime", "ResolveRuntimeOperation" })
        {
            object? permission = await ScalarAsync(connection,
                "SELECT HAS_PERMS_BY_NAME(@name, 'OBJECT', 'EXECUTE');", Text("name", "dh." + name));
            if (Convert.ToInt32(permission) != 1)
                throw new InvalidDataException();
            executeGrants++;
        }
        foreach (string name in new[] { "InspectRecovery", "RecoverAndLoad", "ReleaseRecovery", "ResolveRecoveryOperation", "LockAndReadAuthority" })
        {
            object? permission = await ScalarAsync(connection,
                "SELECT HAS_PERMS_BY_NAME(@name, 'OBJECT', 'EXECUTE');", Text("name", "dh." + name));
            if (permission != DBNull.Value && Convert.ToInt32(permission) != 0)
                throw new InvalidDataException();
        }
        using SqlTransaction transaction = connection.BeginTransaction();
        bool denied = false;
        try
        {
            await ExecuteAsync(connection, "UPDATE dh.Character SET Class = Class WHERE 1 = 0;", transaction);
        }
        catch (SqlException exception) when (exception.Number == 229)
        {
            denied = true;
        }
        finally
        {
            transaction.Rollback();
        }
        if (!denied)
            throw new InvalidDataException();
        return new { Status = "Succeeded", Operation = "runtime", ExecuteGrants = executeGrants, DirectDmlDenied = denied };
    }

    internal async Task<object> SaDisabledAsync(CredentialEnvelope credentials)
    {
        try
        {
            using SqlConnection unexpected = await OpenAsync("master", "sa", credentials.Admin);
        }
        catch (SqlException exception) when (exception.Number is 18456 or 18470)
        {
            // The successful bootstrap separately observed is_disabled=1; login denial alone is insufficient.
            return new { Status = "Succeeded", Operation = "sa-disabled", LoginDenied = true, SqlNumber = exception.Number };
        }
        throw new InvalidDataException();
    }

    async Task<SqlConnection> OpenAsync(string database, string name, SecureString password)
    {
        if (password.Length == 0)
            throw new InvalidDataException();
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = plan.Endpoint, InitialCatalog = database, Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = false, Pooling = false, PersistSecurityInfo = false,
            IntegratedSecurity = false, MultipleActiveResultSets = false, Enlist = false,
            ConnectRetryCount = 0, ConnectTimeout = plan.ConnectSeconds, ApplicationName = "Dawnholder.LinuxSqlHost"
        };
        SqlConnection connection = new(builder.ConnectionString, new SqlCredential(name, password));
        try
        {
            await connection.OpenAsync(cancellation);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    async Task AssertEngineAsync(SqlConnection master)
    {
        await ExecuteAsync(master, """
            IF CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) <> @hostname OR
                CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) <> @version OR
                CONVERT(nvarchar(128),SERVERPROPERTY('Collation')) <> @collation OR
                ORIGINAL_LOGIN() <> N'sa'
                THROW 51125, 'Engine identity differs.', 1;
            """, null, Text("hostname", plan.Hostname), Text("version", plan.ProductVersion), Text("collation", plan.Collation));
    }

    async Task ExecuteAsync(SqlConnection connection, string sql, SqlTransaction? transaction, params SqlParameter[] parameters)
    {
        using SqlCommand command = NewCommand(connection, sql, transaction, parameters);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    async Task<object?> ScalarAsync(SqlConnection connection, string sql, params SqlParameter[] parameters)
    {
        using SqlCommand command = NewCommand(connection, sql, null, parameters);
        return await command.ExecuteScalarAsync(cancellation);
    }

    SqlCommand NewCommand(SqlConnection connection, string sql, SqlTransaction? transaction, SqlParameter[] parameters)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = plan.CommandSeconds;
        command.Transaction = transaction;
        command.Parameters.AddRange(parameters);
        return command;
    }

    static SqlParameter Text(string name, string value, int size = 128) =>
        new("@" + name, SqlDbType.NVarChar, size) { Value = value };

    static SqlParameter Id(string name, Guid value) => new("@" + name, SqlDbType.UniqueIdentifier) { Value = value };
}
