using System.Data;
using Dawnholder.Server.GameServer.Accounts;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Runs a database-tier test only when a local run supplies <see cref="CatalogDatabase.InputVariable"/>; otherwise
/// the test is reported as skipped, never as passed. The variable names a public localWindows input file.
/// </summary>
public sealed class CatalogDatabaseFactAttribute : FactAttribute
{
    public CatalogDatabaseFactAttribute() => Skip = CatalogDatabase.SkipReason();
}

/// <summary>The theory form of <see cref="CatalogDatabaseFactAttribute"/>; every row is skipped without the run input.</summary>
public sealed class CatalogDatabaseTheoryAttribute : TheoryAttribute
{
    public CatalogDatabaseTheoryAttribute() => Skip = CatalogDatabase.SkipReason();
}

// The stall and lock probes read server-wide session state, so no other database test may run beside them.
[CollectionDefinition(CatalogDatabase.Collection, DisableParallelization = true)]
public sealed class CatalogDatabaseCollection
{
}

/// <summary>
/// API v2 SQL composition over an explicit localWindows release-5 fixture. Without the input, tests are skipped;
/// incompatible input fails before SQL. Both repository and probe connections use the Windows process identity;
/// this does not verify the runtime SQL login or its least-privilege grants.
/// Accounts and names are unique per run: the account ID is derived with the product's existing UUIDv5 resolver as
/// an input, the same way World derives it after login (its values are pinned by AccountIdResolverTests); no expected
/// value is computed from it.
/// </summary>
sealed class CatalogDatabase : IDisposable
{
    public const string InputVariable = "DAWNHOLDER_CATALOG_DB_INPUT";
    public const string Collection = "CharacterCatalogDatabase";
    // A precondition the test could not establish or prove; the run records it as a test limit, never as a pass.
    public const string TestLimit = "[시험 한계] ";
    public static readonly TimeSpan CallBudget = TimeSpan.FromSeconds(5);

    internal static string? SkipReason()
        => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(InputVariable))
            ? $"실제 DB 실행 입력 없음: 승인된 실행창에서 {InputVariable}을 지정한다."
            : null;

    readonly string _runToken = Guid.NewGuid().ToString("N")[..6];
    readonly CatalogDatabaseInput _input;
    int _counter;

    CatalogDatabase(CatalogDatabaseInput input) => _input = input;

    public static CatalogDatabase Open()
    {
        string? path = Environment.GetEnvironmentVariable(InputVariable);
        Assert.False(string.IsNullOrWhiteSpace(path), "An explicit localWindows fixture input is required.");
        CatalogDatabaseInput input = CatalogDatabaseInput.Read(path!);
        try
        {
            SqlCharacterCatalogRepository repository = new(input.Options, input.CreateConnection);
            bool compatible = repository.PreflightAsync(FullBudget(), CancellationToken.None)
                .WaitAsync(CallBudget).GetAwaiter().GetResult();
            Assert.True(compatible, "The local fixture failed the actual catalog schema/module preflight.");
            return new CatalogDatabase(input);
        }
        catch
        {
            input.Dispose();
            throw;
        }
    }

    /// <summary>A new catalog over new connections: the restart-equivalent reader of durable rows.</summary>
    public CatalogUnderTest NewCatalog()
    {
        return new(new CharacterCatalogService(
            new SqlCharacterCatalogRepository(_input.Options, _input.CreateConnection),
            new SqlCharacterRepository(_input.Options, _input.CreateConnection),
            PersistenceTestValues.Defaults(), TimeProvider.System));
    }

    /// <summary>
    /// An open connection for the probes the requirements name (dh.Account rows and locks, the old global Slot.1
    /// applock, other sessions' requests and transactions). It needs read and table-lock rights on dh.Account and
    /// VIEW SERVER STATE on the Windows identity; these are not runtime-principal permission checks.
    /// </summary>
    public SqlConnection OpenRawConnection()
    {
        SqlConnection connection = _input.CreateConnection();
        try
        {
            connection.OpenAsync().WaitAsync(CallBudget).GetAwaiter().GetResult();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public (object Context, Guid AccountId) NewAccount()
    {
        string name = $"t{_runToken}_{Interlocked.Increment(ref _counter):D3}";
        // Input derivation only, as World does after login; AccountIdResolverTests pins the resolver independently.
        Guid accountId = AccountIdResolver.Resolve(name);
        Built<object> account = CharacterCatalogApi.Account(accountId, name);
        Assert.True(account.Value is not null, $"A valid AccountContext for {name} was refused: {account.Refused}");
        return (account.Value!, accountId);
    }

    /// <summary>A run-unique ASCII character name of at most 12 characters ending with <paramref name="tag"/>.</summary>
    public string NewName(string tag)
    {
        Assert.InRange(tag.Length, 1, 4);
        int serial = Interlocked.Increment(ref _counter);
        Assert.InRange(serial, 1, 999);
        return $"N{_runToken[..4]}{serial:D3}{tag}";
    }

    /// <summary>A new connection's RequestContext: retries and reconciliations never reuse the original one.</summary>
    public static object FreshRequest(long generation = 1) => CharacterCatalogApi.Request(Guid.NewGuid(), generation);

    /// <summary>
    /// A user retry changes correlation only. API v2 forbids renewing the original deadline.
    /// </summary>
    public static CatalogCall Again(CatalogCall call, long generation = 1)
        => call with { Request = FreshRequest(generation) };

    /// <summary>Separate bounded host/restart receipt inspection, never a renewed user mutation or success send.</summary>
    public static CatalogCall Recovery(CatalogCall call, long generation = 1)
        => call with { Request = FreshRequest(generation), Deadline = FullBudget() };

    static PersistenceDeadline FullBudget() => PersistenceDeadline.FromBudget(TimeProvider.System, CallBudget);

    public static CatalogCall Call(object account, int slot = 1, string? name = null,
        CharacterClass characterClass = CharacterClass.Knight, Guid operationId = default, Guid characterId = default)
        => new(
            account,
            FreshRequest(),
            FullBudget(),
            CancellationToken.None,
            slot,
            name,
            characterClass,
            operationId == default ? Guid.NewGuid() : operationId,
            characterId);

    public static async Task<long> Scalar(SqlConnection connection, string sql, params (string Name, object Value)[] values)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        foreach ((string name, object value) in values)
            command.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    /// <summary>Polls <paramref name="sql"/> until it returns 0 or <paramref name="limit"/> passes; true when it did.</summary>
    public static async Task<bool> BecomesZero(SqlConnection connection, string sql, TimeSpan limit,
        params (string Name, object Value)[] values)
    {
        DateTime end = DateTime.UtcNow + limit;
        while (await Scalar(connection, sql, values) != 0)
        {
            if (DateTime.UtcNow >= end)
                return false;
            await Task.Delay(50);
        }
        return true;
    }

    public void Dispose()
    {
        _input.Dispose();
    }
}
