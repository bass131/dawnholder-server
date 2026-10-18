using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence.Sql;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Independent verification of the listen-before startup gate's deployment identity (technical-spec §2.1: configured
/// database, schema/version and binding are checked before persistent admission opens). The server compares the migration
/// manifest that dh.ReadAdmission returns with its own embedded migration sources. The expected catalog is the reviewed
/// literal in 99_Tools/database/verify-schema.sql, including the independently reviewed UTF-8/LF bytes of migration 005.
/// These checksums are fixed contract literals, not values computed by the product's hashing.
/// The manifest shape is dh.ReadAdmission's
/// <c>SELECT Version AS version, Name AS name, Checksum AS checksum FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH</c>.
/// </summary>
public sealed class SqlPersistenceDeploymentCatalogTests
{
    sealed record CatalogRow(int Version, string Name, string Checksum);

    static readonly CatalogRow[] ReviewedCatalog =
    {
        new(1, "001_initial.sql", "F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC"),
        new(2, "002_persistence_metadata.sql", "867F8CC52350029EE15E753EC9134DDB7F7FAD91D5A9383D0F2209EAAD2C3599"),
        new(3, "003_module_metadata.sql", "A4F271547EC0E2E700413F51D8281A361D8654D383996E73F9476E4540A40F9B"),
        new(4, "004_module_release.sql", "2B9480C378AFBFFD7FA09E45BC2C416977D70B3BFD5436D5DDD3A1C24CB207B0"),
        new(5, "005_character_catalog.sql", "745804775660CCF8A1EFFB0C4052E283374D0DEFDDF3B14E8E068E1C5D81C84D"),
    };

    static string Manifest(IEnumerable<CatalogRow> rows)
        => JsonSerializer.Serialize(rows.Select(row => new { version = row.Version, name = row.Name, checksum = row.Checksum }));

    [Fact]
    public void ReviewedCatalog_PassesTheStartupDeploymentGate()
    {
        Exception? refused = Record.Exception(() => SqlPersistenceDeployment.ValidateManifest(Manifest(ReviewedCatalog)));

        Assert.Null(refused);
    }

    public static TheoryData<string> Drifts => new()
    {
        "004 checksum differs by one digit",
        "005 checksum differs by one digit",
        "002 renamed",
        "003 missing",
        "005 missing",
        "005 duplicated",
        "duplicate version replaces 005",
        "001 and 002 swapped",
        "unknown sixth migration",
        "version numbering shifted",
        "extra manifest field",
        "not an array",
    };

    [Theory]
    [MemberData(nameof(Drifts))]
    public void CatalogDrift_FailsTheStartupDeploymentGate(string drift)
    {
        string manifest = drift switch
        {
            "004 checksum differs by one digit" => Manifest(ReviewedCatalog.Select(row => row.Version == 4
                ? row with { Checksum = "9" + row.Checksum[1..] } : row)),
            "005 checksum differs by one digit" => Manifest(ReviewedCatalog.Select(row => row.Version == 5
                ? row with { Checksum = "9" + row.Checksum[1..] } : row)),
            "002 renamed" => Manifest(ReviewedCatalog.Select(row => row.Version == 2
                ? row with { Name = "002_persistence.sql" } : row)),
            "003 missing" => Manifest(ReviewedCatalog.Where(row => row.Version != 3)),
            "005 missing" => Manifest(ReviewedCatalog.Where(row => row.Version != 5)),
            "005 duplicated" => Manifest(ReviewedCatalog.Append(ReviewedCatalog[4])),
            "duplicate version replaces 005" => Manifest(ReviewedCatalog.Select(row => row.Version == 5 ? ReviewedCatalog[3] : row)),
            "001 and 002 swapped" => Manifest(new[] { ReviewedCatalog[1], ReviewedCatalog[0] }.Concat(ReviewedCatalog.Skip(2))),
            "unknown sixth migration" => Manifest(ReviewedCatalog.Append(new CatalogRow(6, "006_future.sql", new string('0', 64)))),
            "version numbering shifted" => Manifest(ReviewedCatalog.Select(row => row with { Version = row.Version + 1 })),
            "extra manifest field" => JsonSerializer.Serialize(ReviewedCatalog.Select(row =>
                new { version = row.Version, name = row.Name, checksum = row.Checksum, appliedUtc = "2026-10-15" })),
            "not an array" => JsonSerializer.Serialize(new { version = 1, name = "001_initial.sql", checksum = ReviewedCatalog[0].Checksum }),
            _ => throw new ArgumentOutOfRangeException(nameof(drift), drift, "unknown drift case"),
        };

        Exception? refused = Record.Exception(() => SqlPersistenceDeployment.ValidateManifest(manifest));

        Assert.True(refused is not null, $"{drift}: the startup gate accepted a drifted deployment");
    }
}
