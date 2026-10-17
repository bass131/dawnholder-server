using System.Net;
using System.Text;
using Dawnholder.Server.GameServer.Hosting.PersistenceRuntime;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Microsoft.Data.SqlClient;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Independent verification of the launcher → server hand-off (technical-spec §2.1 stdin/LF boundary and no CLI secret;
/// runtime-entry-sql-1 contract: one UTF-8 LF JSON line with exactly version 1, userName and password, after which stdin
/// stays available for the operator's stop newline). The accepted line is the shape Start-PersistenceServer.ps1 writes
/// (ConvertTo-Json -Compress of an ordered version/userName/password map, then "\n"). Only MemoryStreams are read here;
/// process stdin, files, DPAPI and the network are never touched.
/// </summary>
public sealed class PersistenceRuntimeHandoffTests
{
    const string UserName = "dh_runtime_probe";
    const string PasswordText = "p@ss\"w0rd";
    const string LauncherLine = "{\"version\":1,\"userName\":\"dh_runtime_probe\",\"password\":\"p@ss\\\"w0rd\"}";

    static PersistenceOptions Options() => new(
        AdmissionTestWorld.PersistedName, "DawnholderRuntimeProbe", "tcp:127.0.0.1,14330", 1,
        AdmissionTestWorld.PersistedAccountId, CharacterId);

    static MemoryStream Input(string text) => new(Encoding.UTF8.GetBytes(text));

    static string Reveal(SqlConnection connection) => new NetworkCredential(string.Empty, connection.Credential.Password).Password;

    [Fact]
    public async Task NoArguments_KeepTheExplicitNonPersistentServer()
    {
        PersistenceRuntimeStartup? startup = await PersistenceRuntimeStartup.CreateAsync(Array.Empty<string>(), CancellationToken.None);

        Assert.Null(startup);
    }

    public static TheoryData<string[]> MalformedSelections => new()
    {
        new[] { "--persistence-profile" },
        new[] { "--persistence-profile", " " },
        new[] { "--profile", "persistence.runtime.json" },
        new[] { "--persistence-profile", "persistence.runtime.json", "--extra" },
    };

    [Theory]
    [MemberData(nameof(MalformedSelections))]
    public async Task MalformedProfileSelection_IsRefusedBeforeAnyProfileOrCredentialIsRead(string[] args)
    {
        // Argument validation precedes the profile read and the stdin read, so this never blocks on input.
        await Assert.ThrowsAsync<ArgumentException>(() => PersistenceRuntimeStartup.CreateAsync(args, CancellationToken.None));
    }

    [Fact]
    public async Task FirstLfLine_IsTheOnlyConsumedInput_AndTheOperatorNewlineRemains()
    {
        using MemoryStream input = Input(LauncherLine + "\n" + "\n");
        long lineEnd = Encoding.UTF8.GetByteCount(LauncherLine) + 1;

        using PersistenceRuntimeCredential credential = await PersistenceRuntimeCredential.ReadAsync(input, CancellationToken.None);
        using SqlPersistenceConnectionFactory factory = credential.CreateFactory(Options());
        using SqlConnection connection = factory.CreateConnection();

        Assert.Equal(lineEnd, input.Position);
        Assert.Equal((byte)'\n', input.ReadByte());
        Assert.Equal(UserName, connection.Credential.UserId);
        Assert.Equal(PasswordText, Reveal(connection));
    }

    [Fact]
    public async Task DisposingTheReadCredential_LeavesTheStartupFactoryUsable()
    {
        // PersistenceRuntimeStartup.CreateAsync disposes the credential right after building the factory it keeps.
        SqlPersistenceConnectionFactory factory;
        using (MemoryStream input = Input(LauncherLine + "\n"))
        using (PersistenceRuntimeCredential credential = await PersistenceRuntimeCredential.ReadAsync(input, CancellationToken.None))
            factory = credential.CreateFactory(Options());

        using (factory)
        using (SqlConnection connection = factory.CreateConnection())
            Assert.Equal(PasswordText, Reveal(connection));
    }

    public static TheoryData<string, string> MalformedTransports => new()
    {
        { "end of input before any line", "" },
        { "end of input before the LF", LauncherLine },
        { "CRLF instead of the WSL LF boundary", LauncherLine + "\r\n" },
        { "empty first line", "\n" + LauncherLine + "\n" },
        { "NUL inside the line", "{\"version\":1,\"userName\":\"u\",\"password\":\"p\"}\0\n" },
        { "unknown version", "{\"version\":2,\"userName\":\"u\",\"password\":\"p\"}\n" },
        { "missing password", "{\"version\":1,\"userName\":\"u\"}\n" },
        { "extra field", "{\"version\":1,\"userName\":\"u\",\"password\":\"p\",\"server\":\"x\"}\n" },
        { "duplicate field", "{\"version\":1,\"userName\":\"u\",\"userName\":\"v\",\"password\":\"p\"}\n" },
        { "blank user", "{\"version\":1,\"userName\":\" \",\"password\":\"p\"}\n" },
        { "empty password", "{\"version\":1,\"userName\":\"u\",\"password\":\"\"}\n" },
        { "not a JSON object", "[1,\"u\",\"p\"]\n" },
    };

    [Theory]
    [MemberData(nameof(MalformedTransports))]
    public async Task MalformedCredentialLine_IsRefused(string reason, string text)
    {
        using MemoryStream input = Input(text);

        Exception? refused = await Record.ExceptionAsync(() => PersistenceRuntimeCredential.ReadAsync(input, CancellationToken.None));

        Assert.True(refused is not null, $"{reason}: the credential line was accepted");
    }

    [Fact]
    public async Task OversizedLine_IsRefusedWithoutDrainingTheRestOfTheInput()
    {
        string longLine = new string('a', 64 * 1024) + "\n";
        using MemoryStream input = Input(longLine);

        Exception? refused = await Record.ExceptionAsync(() => PersistenceRuntimeCredential.ReadAsync(input, CancellationToken.None));

        Assert.NotNull(refused);
        Assert.True(input.Position < input.Length, $"the reader consumed {input.Position} of {input.Length} bytes");
    }
}
