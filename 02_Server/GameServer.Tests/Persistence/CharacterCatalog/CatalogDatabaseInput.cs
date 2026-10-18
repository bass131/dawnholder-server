using System.IO.Pipes;
using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Sql;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Explicit release-5 fixture input; never adopts the old fixed-character fixture or provisions a database.</summary>
sealed class CatalogDatabaseInput : IDisposable
{
    CatalogDatabaseInput(PersistenceOptions options, SqlPersistenceConnectionFactory runtime,
        SqlPersistenceConnectionFactory probe)
    {
        Options = options;
        Runtime = runtime;
        Probe = probe;
    }

    public PersistenceOptions Options { get; }
    public SqlPersistenceConnectionFactory Runtime { get; }
    public SqlPersistenceConnectionFactory Probe { get; }

    public static CatalogDatabaseInput Read(string path)
    {
        using JsonDocument input = ReadPublicFile(path);
        JsonElement root = input.RootElement;
        Keys(root, "version", "fixtureKind", "serverEndpoint", "database", "profilePath", "profileHash",
            "approvalPath", "approvalHash", "credentialPipe");
        Require(root.GetProperty("version").GetInt32() == 2 &&
            Text(root, "fixtureKind") == "multi-character-release5");
        PersistenceOptions options = new(2, Text(root, "database"), Text(root, "serverEndpoint"));
        using JsonDocument profile = ReadPublicFile(Text(root, "profilePath"), Text(root, "profileHash"));
        JsonElement configured = profile.RootElement;
        Keys(configured, "profileVersion", "database", "serverEndpoint");
        Require(configured.GetProperty("profileVersion").GetInt32() == 2 &&
            Text(configured, "database") == options.Database && Text(configured, "serverEndpoint") == options.ServerEndpoint);
        using JsonDocument approval = ReadPublicFile(Text(root, "approvalPath"), Text(root, "approvalHash"));
        JsonElement approved = approval.RootElement;
        Keys(approved, "purpose", "fixtureKind", "serverEndpoint", "database", "profileHash",
            "schemaVersion", "moduleRelease", "allowCatalogTests");
        Require(Text(approved, "purpose") == "character-catalog-tests" &&
            Text(approved, "fixtureKind") == "multi-character-release5" &&
            Text(approved, "database") == options.Database &&
            Text(approved, "serverEndpoint") == options.ServerEndpoint &&
            Text(approved, "profileHash") == Text(root, "profileHash") &&
            approved.GetProperty("schemaVersion").GetInt32() == 5 &&
            approved.GetProperty("moduleRelease").GetInt32() == 5 &&
            approved.GetProperty("allowCatalogTests").GetBoolean());
        return ReadCredentials(options, Text(root, "credentialPipe")).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Runtime.Dispose();
        Probe.Dispose();
    }

    static JsonDocument ReadPublicFile(string path, string? expectedHash = null)
    {
        Require(Path.IsPathFullyQualified(path));
        FileInfo file = new(path);
        Require(file.Exists && file.Length is > 0 and <= 16384);
        byte[] bytes = File.ReadAllBytes(path);
        if (expectedHash is not null)
            Require(expectedHash.Length == 64 &&
                Convert.ToHexString(SHA256.HashData(bytes)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase));
        return JsonDocument.Parse(bytes);
    }

    static async Task<CatalogDatabaseInput> ReadCredentials(PersistenceOptions options, string pipeName)
    {
        Require(pipeName.Length is > 0 and <= 120 &&
            pipeName.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'));
        byte[] bytes = new byte[4096];
        SqlPersistenceConnectionFactory? runtime = null;
        try
        {
            using CancellationTokenSource budget = new(TimeSpan.FromSeconds(5));
            using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(budget.Token);
            int length = 0;
            while (length < bytes.Length)
            {
                int count = await pipe.ReadAsync(bytes.AsMemory(length, 1), budget.Token);
                if (count == 0 || bytes[length] is 0 or 13) throw new InvalidDataException();
                if (bytes[length] == 10) break;
                length++;
            }
            Require(length is > 0 and < 4096);
            using JsonDocument credentials = JsonDocument.Parse(bytes.AsMemory(0, length));
            JsonElement root = credentials.RootElement;
            Keys(root, "version", "runtime", "probe");
            Require(root.GetProperty("version").GetInt32() == 1);
            runtime = Factory(options, root.GetProperty("runtime"));
            SqlPersistenceConnectionFactory probe = Factory(options, root.GetProperty("probe"));
            return new(options, runtime, probe);
        }
        catch
        {
            runtime?.Dispose();
            // Do not attach an exception containing credential JSON, a driver response or secret transport bytes.
            throw new InvalidDataException("Catalog fixture credential transport failed; no database was provisioned.");
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    static SqlPersistenceConnectionFactory Factory(PersistenceOptions options, JsonElement value)
    {
        Keys(value, "userName", "password");
        using SecureString password = new();
        string text = Text(value, "password");
        Require(text.Length is > 0 and <= 128);
        foreach (char character in text) password.AppendChar(character);
        password.MakeReadOnly();
        return new(options, Text(value, "userName"), password);
    }

    static string Text(JsonElement value, string name)
        => value.GetProperty(name).GetString() ?? throw new InvalidDataException("Catalog fixture value is missing.");

    static void Keys(JsonElement value, params string[] names)
        => Require(value.ValueKind == JsonValueKind.Object &&
            value.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(names.Order()));

    static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException("Catalog fixture input is incompatible or unapproved.");
    }
}
