using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence;
using Microsoft.Data.SqlClient;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Explicit local development input; uses the Windows process identity and never provisions a database.</summary>
sealed class CatalogDatabaseInput : IDisposable
{
    readonly string _connectionString;
    bool _disposed;

    CatalogDatabaseInput(PersistenceOptions options)
    {
        Options = options;
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = options.ServerEndpoint,
            InitialCatalog = options.Database,
            IntegratedSecurity = true,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            // This loopback-only development fixture does not test the runtime's certificate or SQL-login contract.
            TrustServerCertificate = true,
            PersistSecurityInfo = false,
            MultipleActiveResultSets = false,
            Enlist = false,
            ConnectRetryCount = 0,
            ConnectTimeout = options.ConnectTimeoutSeconds,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = options.MaxPoolSize,
            ApplicationName = "Dawnholder.CharacterCatalog.Tests"
        };
        _connectionString = builder.ConnectionString;
    }

    public PersistenceOptions Options { get; }

    public static CatalogDatabaseInput Read(string path)
    {
        Require(Path.IsPathFullyQualified(path));
        FileInfo file = new(path);
        Require(file.Exists && file.Length is > 0 and <= 16384);
        using JsonDocument input = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = input.RootElement;
        Require(root.ValueKind == JsonValueKind.Object &&
            root.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(
                new[] { "database", "mode", "serverEndpoint", "version" }));
        Require(root.GetProperty("version").GetInt32() == 1 && Text(root, "mode") == "localWindows");
        Require(OperatingSystem.IsWindows());

        PersistenceOptions options = new(2, Text(root, "database"), Text(root, "serverEndpoint"));
        string host = options.ServerEndpoint[4..options.ServerEndpoint.LastIndexOf(',')];
        Require(host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1");
        Require(options.Database.StartsWith("Dawnholder_", StringComparison.OrdinalIgnoreCase));
        return new(options);
    }

    public SqlConnection CreateConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new SqlConnection(_connectionString);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        using SqlConnection connection = new(_connectionString);
        SqlConnection.ClearPool(connection);
    }

    static string Text(JsonElement value, string name)
        => value.GetProperty(name).GetString() ?? throw new InvalidDataException("Catalog fixture value is missing.");

    static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException("Catalog fixture requires an explicit localWindows development input.");
    }
}
