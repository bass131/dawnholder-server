using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Tools.WindowsSqlVerifyHost;

// Read-only WSL runtime probe. There is no admin credential, caller SQL, or trust bypass API.
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsLinux() || args.Length != 0)
                throw new InvalidDataException();
            using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(35));
            using JsonDocument document = await ReadEnvelopeAsync(deadline.Token);
            JsonElement root = document.RootElement;
            string[] fields = ["Version", "EndpointAddress", "ServerName", "Port", "Database", "RuntimeLogin",
                "MachineName", "InstanceName", "ProductVersion", "PublicCaHash", "Password"];
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!fields.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                    throw new InvalidDataException();
            }
            if (seen.Count != fields.Length || root.GetProperty("Version").GetInt32() != 1)
                throw new InvalidDataException();
            string name = Text(root, "ServerName", "^(?=.{1,253}$)[a-zA-Z0-9]+([.-][a-zA-Z0-9]+)*$");
            string address = Text(root, "EndpointAddress", "^[0-9.]+$");
            string database = Text(root, "Database", "^Dawnholder_[A-Za-z0-9_]{1,100}$");
            string login = Text(root, "RuntimeLogin", "^dh_[a-z0-9_]{1,80}$");
            string machine = Text(root, "MachineName", "^[A-Za-z0-9][A-Za-z0-9-]{0,14}$");
            string instance = Text(root, "InstanceName", "^[A-Za-z][A-Za-z0-9_]{0,15}$");
            string version = Text(root, "ProductVersion", @"^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$");
            int port = root.GetProperty("Port").GetInt32();
            if (port is < 1024 or > 65535 || !IPAddress.TryParse(address, out IPAddress? expected) ||
                expected.AddressFamily != AddressFamily.InterNetwork)
                throw new InvalidDataException();
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(name, deadline.Token);
            IPAddress[] ipv4 = addresses.Where(item => item.AddressFamily == AddressFamily.InterNetwork).ToArray();
            if (ipv4.Length == 0 || ipv4.Any(item => !item.Equals(expected)))
                throw new InvalidDataException();
            string caPath = Environment.GetEnvironmentVariable("SSL_CERT_FILE") ?? "";
            string caDirectory = Environment.GetEnvironmentVariable("SSL_CERT_DIR") ?? "";
            if (!Path.IsPathFullyQualified(caPath) || !Path.IsPathFullyQualified(caDirectory) ||
                Directory.EnumerateFileSystemEntries(caDirectory).Any())
                throw new InvalidDataException();
            byte[] ca = await File.ReadAllBytesAsync(caPath, deadline.Token);
            if (ca.Length > 16384 || Convert.ToHexString(SHA256.HashData(ca)) !=
                Text(root, "PublicCaHash", "^[A-F0-9]{64}$"))
                throw new InvalidDataException();
            using SecureString password = new();
            string raw = root.GetProperty("Password").GetString() ?? "";
            if (raw.Length is < 16 or > 128 || raw.Any(char.IsControl))
                throw new InvalidDataException();
            foreach (char character in raw)
                password.AppendChar(character);
            password.MakeReadOnly();
            SqlConnectionStringBuilder builder = new()
            {
                // DNS name is the certificate validation name in BOTH this probe and GameServer's profile.
                DataSource = $"tcp:{name},{port}",
                InitialCatalog = database,
                Encrypt = SqlConnectionEncryptOption.Mandatory,
                TrustServerCertificate = false,
                IntegratedSecurity = false,
                PersistSecurityInfo = false,
                Pooling = false,
                MultipleActiveResultSets = false,
                Enlist = false,
                ConnectTimeout = 5,
                ConnectRetryCount = 0,
                ApplicationName = "Dawnholder.WindowsSql.Verify"
            };
            using SqlConnection connection = new(builder.ConnectionString, new SqlCredential(login, password));
            await connection.OpenAsync(deadline.Token);
            await ExecuteAsync(connection, """
                IF CONVERT(nvarchar(128),CONNECTIONPROPERTY('net_transport')) <> N'TCP' OR
                    CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')) COLLATE Latin1_General_100_BIN2 <> @machine OR
                    CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) COLLATE Latin1_General_100_BIN2 <> @instance OR
                    CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) <> @version OR ORIGINAL_LOGIN() <> @login OR
                    IS_SRVROLEMEMBER(N'sysadmin') <> 0 OR IS_MEMBER(N'db_owner') <> 0 OR
                    IS_MEMBER(N'dh_runtime') <> 1 OR IS_MEMBER(N'dh_recovery') <> 0 OR
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') <> 0 OR
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') <> 0 OR
                    HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'SELECT') <> 0 OR
                    HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'INSERT') <> 0 OR
                    HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'UPDATE') <> 0 OR
                    HAS_PERMS_BY_NAME(N'dh.Character', 'OBJECT', 'DELETE') <> 0
                    THROW 51220, 'Runtime identity or authority mismatch.', 1;
                """, deadline.Token, Parameter("machine", machine), Parameter("instance", instance),
                Parameter("version", version), Parameter("login", login));
            int grants = 0;
            foreach (string rpc in new[] { "ReadAdmission", "AcquireAndLoad", "WriteSafeCheckpoint",
                "ReleaseRuntime", "ResolveRuntimeOperation" })
            {
                await ExecuteAsync(connection, """
                    IF COALESCE(HAS_PERMS_BY_NAME(@name, 'OBJECT', 'EXECUTE'), 0) <> 1
                        THROW 51221, 'Missing runtime grant.', 1;
                    """, deadline.Token, Parameter("name", "dh." + rpc));
                grants++;
            }
            foreach (string rpc in new[] { "InspectRecovery", "RecoverAndLoad", "ReleaseRecovery",
                "ResolveRecoveryOperation", "LockAndReadAuthority" })
            {
                await ExecuteAsync(connection, """
                    IF COALESCE(HAS_PERMS_BY_NAME(@name, 'OBJECT', 'EXECUTE'), 0) <> 0
                        THROW 51222, 'Unexpected recovery grant.', 1;
                    """, deadline.Token, Parameter("name", "dh." + rpc));
            }
            bool denied = false;
            try
            {
                await ExecuteAsync(connection, "UPDATE dh.Character SET Class = Class WHERE 1 = 0;", deadline.Token);
            }
            catch (SqlException exception) when (exception.Number == 229)
            {
                denied = true;
            }
            if (!denied)
                throw new InvalidDataException();
            // Mandatory + no trust override is the TLS proof; LPC administration never sets these results.
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Status = "Verified",
                Transport = "TCP",
                Encrypted = true,
                CertificateValidated = true,
                RuntimeExecuteGrants = grants,
                DirectDmlDenied = denied
            }));
            return 0;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("WINDOWS_SQL_WSL_VERIFY_FAILED");
            return 1;
        }
    }

    static async Task<JsonDocument> ReadEnvelopeAsync(CancellationToken cancellation)
    {
        byte[] bytes = new byte[8192];
        try
        {
            using Stream stdin = Console.OpenStandardInput();
            int used = 0;
            while (used < bytes.Length)
            {
                int count = await stdin.ReadAsync(bytes.AsMemory(used, 1), cancellation).AsTask().WaitAsync(cancellation);
                if (count == 0)
                    throw new InvalidDataException();
                if (bytes[used] == (byte)'\n')
                    return JsonDocument.Parse(bytes.AsMemory(0, used).ToArray());
                used++;
            }
            throw new InvalidDataException();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    static string Text(JsonElement root, string name, string pattern)
    {
        string value = root.GetProperty(name).GetString() ?? "";
        return Regex.IsMatch(value, pattern) ? value : throw new InvalidDataException();
    }

    static SqlParameter Parameter(string name, string value) => new("@" + name, SqlDbType.NVarChar, 128) { Value = value };

    static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellation,
        params SqlParameter[] parameters)
    {
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 5;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellation);
    }
}
