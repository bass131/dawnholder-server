using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dawnholder.Tools.LinuxSqlHost;

// This host accepts only an externally hash-approved plan and named operations, never caller SQL.
internal sealed class HostPlan
{
    internal static readonly string[] Migrations =
    ["001_initial.sql", "002_persistence_metadata.sql", "003_module_metadata.sql", "004_module_release.sql"];
    readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);

    internal string Endpoint { get; private init; } = "";
    internal string Database { get; private init; } = "";
    internal string RuntimeLogin { get; private init; } = "";
    internal string Hostname { get; private init; } = "";
    internal string ProductVersion { get; private init; } = "";
    internal string Collation { get; private init; } = "";
    internal string LifetimeId { get; private init; } = "";
    internal Guid AccountId { get; private init; }
    internal Guid CharacterId { get; private init; }
    internal int ConnectSeconds { get; private init; }
    internal int CommandSeconds { get; private init; }
    internal IReadOnlyList<ModuleInput> Modules { get; private set; } = [];
    internal string ManifestHash { get; private set; } = "";

    internal static HostPlan Read(string path, string expectedHash)
    {
        if (!OperatingSystem.IsLinux() || !Regex.IsMatch(expectedHash, "^[A-F0-9]{64}$"))
            throw new InvalidDataException();
        byte[] bytes = ReadBounded(path, 131072);
        if (Hash(bytes) != expectedHash)
            throw new InvalidDataException();
        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        if (root.GetProperty("Version").GetInt32() != 1 || !root.GetProperty("ExecutionApproved").GetBoolean() ||
            root.GetProperty("Encrypt").GetString() != "Mandatory" ||
            root.GetProperty("TrustServerCertificate").GetBoolean() ||
            !root.GetProperty("SaContainerEnvironmentException").GetBoolean() ||
            root.GetProperty("ExpiresUtc").GetDateTimeOffset() <= DateTimeOffset.UtcNow)
            throw new InvalidDataException();
        HostPlan plan = new()
        {
            Endpoint = Required(root, "Endpoint"),
            Database = Required(root, "Database"),
            RuntimeLogin = Required(root, "RuntimeLogin"),
            Hostname = Required(root, "ContainerHostname"),
            ProductVersion = Required(root, "ExpectedProductVersion"),
            Collation = Required(root, "ExpectedCollation"),
            LifetimeId = Required(root, "LifetimeId"),
            AccountId = root.GetProperty("AccountId").GetGuid(),
            CharacterId = root.GetProperty("CharacterId").GetGuid(),
            ConnectSeconds = root.GetProperty("ConnectSeconds").GetInt32(),
            CommandSeconds = root.GetProperty("CommandSeconds").GetInt32()
        };
        if (!Regex.IsMatch(plan.Endpoint, @"^127\.0\.0\.1,[0-9]{4,5}$") ||
            plan.Endpoint != "127.0.0.1," + root.GetProperty("Port").GetInt32() ||
            !Regex.IsMatch(plan.Database, "^Dawnholder_Test_[A-Za-z0-9_]{1,80}$") ||
            !Regex.IsMatch(plan.RuntimeLogin, "^dh_[a-z0-9_]{1,80}$") ||
            !Regex.IsMatch(plan.Hostname, "^[a-z][a-z0-9-]{1,62}$") ||
            !Regex.IsMatch(plan.LifetimeId, "^[a-f0-9]{32}$") ||
            plan.AccountId == Guid.Empty || plan.CharacterId == Guid.Empty ||
            plan.ConnectSeconds != 5 || plan.CommandSeconds != 30 || root.GetProperty("SlotId").GetInt32() != 1)
            throw new InvalidDataException();
        // Only Windows-mounted reviewed sources are accepted; artifacts/builds stay outside this source root.
        string sourceRoot = WindowsMount(Required(root, "SourceRoot"));
        HashSet<string> seen = new(StringComparer.Ordinal);
        long sourceBytes = 0;
        if (root.GetProperty("Inputs").GetArrayLength() is < 25 or > 128)
            throw new InvalidDataException();
        foreach (JsonElement input in root.GetProperty("Inputs").EnumerateArray())
        {
            string relative = Required(input, "Path");
            if (!Regex.IsMatch(relative, "^[A-Za-z0-9_./-]+$") || relative.StartsWith('/') ||
                relative.Split('/').Contains("..") || !seen.Add(relative))
                throw new InvalidDataException();
            string file = Path.Combine(sourceRoot, relative);
            byte[] source = ReadBounded(file, 2 * 1024 * 1024);
            sourceBytes += source.Length;
            if (sourceBytes > 16 * 1024 * 1024)
                throw new InvalidDataException();
            if (Hash(source) != Required(input, "Sha256"))
                throw new InvalidDataException();
            if (relative.StartsWith("99_Tools/database/", StringComparison.Ordinal) &&
                (relative.EndsWith(".sql", StringComparison.Ordinal) || relative.EndsWith("manifest.json", StringComparison.Ordinal)))
            {
                string text = new UTF8Encoding(false, true).GetString(source).TrimStart('\uFEFF').Replace("\r\n", "\n");
                plan._sources.Add(relative["99_Tools/database/".Length..], text);
            }
        }
        foreach (string migration in Migrations)
            _ = plan.Source("migrations/" + migration);
        _ = plan.Source("verify-schema.sql");
        plan.ReadModules();
        return plan;
    }

    internal string Source(string relative) => _sources[relative];

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static string TextHash(string text) => Hash(Encoding.UTF8.GetBytes(text));

    internal static string WindowsMount(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (!Regex.IsMatch(normalized, "^[A-Z]:/[A-Za-z0-9_./ -]+$", RegexOptions.IgnoreCase) ||
            normalized.Split('/').Contains(".."))
            throw new InvalidDataException();
        return "/mnt/" + char.ToLowerInvariant(normalized[0]) + normalized[2..];
    }

    static byte[] ReadBounded(string path, int limit)
    {
        string full = Path.GetFullPath(path);
        for (string? item = full; item != null; item = Path.GetDirectoryName(item))
        {
            if ((File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException();
        }
        using FileStream file = new(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > limit)
            throw new InvalidDataException();
        byte[] bytes = new byte[checked((int)file.Length)];
        file.ReadExactly(bytes);
        if (file.ReadByte() != -1)
            throw new InvalidDataException();
        return bytes;
    }

    static string Required(JsonElement root, string field) => root.GetProperty(field).GetString()
        ?? throw new InvalidDataException();

    void ReadModules()
    {
        string manifest = Source("modules/manifest.json");
        ManifestHash = TextHash(manifest);
        using JsonDocument document = JsonDocument.Parse(manifest);
        JsonElement root = document.RootElement;
        if (root.GetProperty("FormatVersion").GetInt32() != 1 || root.GetProperty("ReleaseVersion").GetInt32() != 4)
            throw new InvalidDataException();
        List<ModuleInput> modules = [];
        HashSet<string> objects = new(StringComparer.Ordinal);
        bool permissionsSeen = false;
        foreach (JsonElement entry in root.GetProperty("Entries").EnumerateArray())
        {
            string path = Required(entry, "Path");
            string sql = Source(path);
            string checksum = Required(entry, "SourceChecksum");
            if (TextHash(sql) != checksum || permissionsSeen || Regex.IsMatch(sql, @"(?im)^\s*GO(?:\s|$)"))
                throw new InvalidDataException();
            string kind = Required(entry, "Kind");
            if (kind == "Permissions")
            {
                if (path != "modules/permissions.sql" || entry.GetProperty("ObjectName").ValueKind != JsonValueKind.Null)
                    throw new InvalidDataException();
                permissionsSeen = true;
                continue;
            }
            string name = Required(entry, "ObjectName");
            if (!Regex.IsMatch(name, @"^dh\.[A-Za-z][A-Za-z0-9]+$") || !objects.Add(name) || kind is not ("P" or "FN"))
                throw new InvalidDataException();
            foreach (JsonElement dependency in entry.GetProperty("DependsOn").EnumerateArray())
            {
                if (dependency.GetString() == name || !objects.Contains(dependency.GetString()!))
                    throw new InvalidDataException();
            }
            string prefix = "CREATE OR ALTER " + (kind == "P" ? "PROCEDURE " : "FUNCTION ") + name;
            if (!sql.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException();
            // Same normalized expected definition contract as ModuleHash.Common; never learn expected bytes from DB.
            byte[] definition = Encoding.Unicode.GetBytes("CREATE   " + sql[16..]);
            int definitionBytes = entry.GetProperty("DefinitionBytes").GetInt32();
            string definitionHash = Required(entry, "DefinitionChecksum");
            if (definition.Length != definitionBytes || Hash(definition) != definitionHash)
                throw new InvalidDataException();
            modules.Add(new(path, name, kind, checksum, definitionBytes, definitionHash));
        }
        if (!permissionsSeen || modules.Count != 18 || _sources.Count != Migrations.Length + modules.Count + 3)
            throw new InvalidDataException();
        Modules = modules;
    }
}

internal sealed record ModuleInput(string Path, string Name, string Kind, string SourceHash,
    int DefinitionBytes, string DefinitionHash);
