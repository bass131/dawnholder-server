using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Tests.Network;

// The same explicit public localWindows input as Core's catalog DB fixture. No database,
// login or secret is provisioned here; missing input is a reported skip, not a DB success.
internal sealed class CharacterMenuDatabaseFactAttribute : FactAttribute
{
    public CharacterMenuDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CharacterMenuDatabaseInput.Variable)))
            Skip = "Actual DB input not supplied: DAWNHOLDER_CATALOG_DB_INPUT is required in an assigned execution window.";
    }
}

internal sealed class CharacterMenuDatabaseInput : IDisposable
{
    internal const string Variable = "DAWNHOLDER_CATALOG_DB_INPUT";
    readonly string _connectionString;

    CharacterMenuDatabaseInput(PersistenceOptions options)
    {
        Options = options;
        _connectionString = new SqlConnectionStringBuilder
        {
            DataSource = options.ServerEndpoint,
            InitialCatalog = options.Database,
            IntegratedSecurity = true,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            // Test-only loopback connection, matching Core's local Windows fixture.
            // Runtime SQL-login and certificate validation are separate observations.
            TrustServerCertificate = true,
            PersistSecurityInfo = false,
            MultipleActiveResultSets = false,
            Enlist = false,
            ConnectRetryCount = 0,
            ConnectTimeout = options.ConnectTimeoutSeconds,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = options.MaxPoolSize,
            ApplicationName = "Dawnholder.CharacterMenu.Tests",
        }.ConnectionString;
    }

    internal PersistenceOptions Options { get; }

    public void Dispose()
    {
        using SqlConnection connection = new(_connectionString);
        SqlConnection.ClearPool(connection);
    }

    internal static CharacterMenuDatabaseInput Open()
    {
        string path = Environment.GetEnvironmentVariable(Variable) ?? throw new InvalidDataException("Explicit DB input missing.");
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(path))
            throw new InvalidDataException("The DB fixture requires the local Windows process identity.");
        FileInfo file = new(path);
        if (!file.Exists || file.Length is <= 0 or > 16384) throw new InvalidDataException("Invalid public DB input.");
        using JsonDocument input = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = input.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).Order().SequenceEqual(
                new[] { "database", "mode", "serverEndpoint", "version" }) ||
            root.GetProperty("version").GetInt32() != 1 || root.GetProperty("mode").GetString() != "localWindows")
        {
            throw new InvalidDataException("The public localWindows DB input is required.");
        }
        PersistenceOptions options = new(2, root.GetProperty("database").GetString()!, root.GetProperty("serverEndpoint").GetString()!);
        string host = options.ServerEndpoint[4..options.ServerEndpoint.LastIndexOf(',')];
        if ((!host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && host != "127.0.0.1") ||
            !options.Database.StartsWith("Dawnholder_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only the explicit loopback development database is accepted.");
        }
        return new(options);
    }

    internal SqlConnection CreateConnection() => new(_connectionString);
}
