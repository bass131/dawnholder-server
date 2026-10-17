using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dawnholder.Server.GameServer.Persistence.Sql;

internal static class SqlPersistenceDeployment
{
    internal const int SchemaVersion = 4;
    static readonly string[] MigrationNames =
    [
        "001_initial.sql", "002_persistence_metadata.sql",
        "003_module_metadata.sql", "004_module_release.sql"
    ];

    internal static void ValidateManifest(string manifest)
    {
        if (manifest.Length > 4096)
            throw new InvalidDataException("Invalid migration manifest.");
        using JsonDocument document = JsonDocument.Parse(manifest);
        JsonElement rows = document.RootElement;
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != MigrationNames.Length)
            throw new InvalidDataException("Incompatible migration manifest.");
        Assembly assembly = typeof(SqlPersistenceDeployment).Assembly;
        for (int index = 0; index < MigrationNames.Length; index++)
        {
            JsonElement row = rows[index];
            SqlPersistenceSnapshot.Keys(row, "version", "name", "checksum");
            string name = MigrationNames[index];
            using Stream source = assembly.GetManifestResourceStream("Persistence.Migrations." + name)
                ?? throw new InvalidDataException("Missing deployment identity.");
            using StreamReader reader = new(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            // Match ModuleHash.Common.ps1: UTF-8 without BOM and normalized CRLF only.
            string text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
            string checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            if (row.GetProperty("version").GetInt32() != index + 1 ||
                row.GetProperty("name").GetString() != name || row.GetProperty("checksum").GetString() != checksum)
                throw new InvalidDataException("Incompatible migration identity.");
        }
    }
}
