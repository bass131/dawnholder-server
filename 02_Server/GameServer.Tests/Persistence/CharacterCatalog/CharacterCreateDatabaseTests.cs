using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Shared.Protocol;
using Xunit.Abstractions;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog.CatalogDatabase;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Real Core API → SQL → result for list and create (goal 완료조건 4·5·6, API v1 「공개 작업과 결과」, 「오류 의미와 우선순위」,
/// 「생성 transaction과 재시도」). Expected values are the inputs the test sent and the API v1 rules: three slots per
/// account listed in slot order, SlotsFull before SlotOccupied before NameTaken, global names unique ignoring ASCII case
/// while the display keeps its case, NFC-equal raw names are the same name and payload, one winner under contention,
/// the same OperationId and payload returning the same character, and a different payload being OperationConflict.
/// </summary>
[Trait("Category", "CharacterCatalogDatabase")]
[Collection(CatalogDatabase.Collection)]
public sealed class CharacterCreateDatabaseTests
{
    readonly ITestOutputHelper _output;

    public CharacterCreateDatabaseTests(ITestOutputHelper output) => _output = output;

    [CatalogDatabaseFact]
    public async Task TwoAccounts_EachCreateThree_ListedInSlotOrderAndIsolated()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        var (bob, _) = db.NewAccount();
        // Alice creates out of slot order so the list order is the product's, not the insertion order.
        (int Slot, string Name, CharacterClass Class)[] aliceRows =
        {
            (3, db.NewName("a3"), CharacterClass.Mage),
            (1, db.NewName("a1"), CharacterClass.Knight),
            (2, db.NewName("a2"), CharacterClass.Mage),
        };
        (int Slot, string Name, CharacterClass Class)[] bobRows =
        {
            (1, db.NewName("b1"), CharacterClass.Mage),
            (2, db.NewName("b2"), CharacterClass.Knight),
            (3, db.NewName("b3"), CharacterClass.Knight),
        };

        SummaryView[] aliceCreated = await CreateAll(catalog, alice, aliceRows);
        SummaryView[] bobCreated = await CreateAll(catalog, bob, bobRows);
        IReadOnlyList<SummaryView> aliceListed = await Listed(catalog, alice);
        IReadOnlyList<SummaryView> bobListed = await Listed(catalog, bob);

        Assert.Equal(new[] { 1, 2, 3 }, aliceListed.Select(row => row.Slot));
        Assert.Equal(aliceCreated.OrderBy(row => row.Slot), aliceListed);
        Assert.Equal(aliceRows.OrderBy(row => row.Slot).Select(row => (row.Name, row.Class)),
            aliceListed.Select(row => (row.Name, row.Class)));
        Assert.Equal(bobCreated.OrderBy(row => row.Slot), bobListed);
        Assert.Empty(aliceListed.Select(row => row.CharacterId).Intersect(bobListed.Select(row => row.CharacterId)));
    }

    [CatalogDatabaseFact]
    public async Task FullAccount_IsSlotsFullBeforeSlotOrName_AndAddsNothing()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        string taken = db.NewName("t");
        await CreateAll(catalog, alice, new[]
        {
            (1, taken, CharacterClass.Knight),
            (2, db.NewName("x"), CharacterClass.Knight),
            (3, db.NewName("y"), CharacterClass.Mage),
        });

        CatalogOutcome fresh = await catalog.CreateAsync(Call(alice, slot: 2, name: db.NewName("n")));
        CatalogOutcome everythingTaken = await catalog.CreateAsync(Call(alice, slot: 1, name: taken.ToUpperInvariant()));
        IReadOnlyList<SummaryView> listed = await Listed(catalog, alice);

        Assert.Equal("SlotsFull", fresh.Status);
        Assert.Equal("SlotsFull", everythingTaken.Status);
        Assert.Equal(3, listed.Count);
    }

    [CatalogDatabaseFact]
    public async Task OccupiedSlot_IsSlotOccupiedBeforeNameTaken()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        string taken = db.NewName("t");
        await CreateAll(catalog, alice, new[] { (1, taken, CharacterClass.Knight) });

        CatalogOutcome slotOnly = await catalog.CreateAsync(Call(alice, slot: 1, name: db.NewName("n")));
        CatalogOutcome nameOnly = await catalog.CreateAsync(Call(alice, slot: 2, name: taken));
        CatalogOutcome both = await catalog.CreateAsync(Call(alice, slot: 1, name: taken));

        Assert.Equal("SlotOccupied", slotOnly.Status);
        Assert.Equal("NameTaken", nameOnly.Status);
        Assert.Equal("SlotOccupied", both.Status);
        Assert.Single(await Listed(catalog, alice));
    }

    [CatalogDatabaseFact]
    public async Task Name_IsUniqueAcrossAccountsIgnoringAsciiCase_AndDisplayKeepsItsCase()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        var (bob, _) = db.NewAccount();
        string display = db.NewName("Ab");

        await CreateAll(catalog, alice, new[] { (1, display, CharacterClass.Knight) });
        CatalogOutcome upper = await catalog.CreateAsync(Call(bob, slot: 1, name: display.ToUpperInvariant()));
        CatalogOutcome lower = await catalog.CreateAsync(Call(bob, slot: 1, name: display.ToLowerInvariant()));
        IReadOnlyList<SummaryView> aliceListed = await Listed(catalog, alice);

        Assert.Equal("NameTaken", upper.Status);
        Assert.Equal("NameTaken", lower.Status);
        Assert.Equal(display, Assert.Single(aliceListed).Name);
        Assert.Empty(await Listed(catalog, bob));
    }

    // 가나 + ASCII suffix in decomposed jamo is stored as the composed display name and collides with its composed form.
    [CatalogDatabaseFact]
    public async Task DecomposedHangul_IsStoredComposedAndCollidesWithTheComposedForm()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        var (bob, _) = db.NewAccount();
        string suffix = db.NewName("h")[^6..];
        string composed = "가나" + suffix;
        string decomposed = "가나" + suffix;

        CatalogOutcome created = await catalog.CreateAsync(Call(alice, slot: 1, name: decomposed));
        CatalogOutcome collided = await catalog.CreateAsync(Call(bob, slot: 1, name: composed));

        Assert.Equal("Created", created.Status);
        Assert.Equal(composed, created.Character!.Name);
        Assert.Equal("NameTaken", collided.Status);
    }

    [CatalogDatabaseFact]
    public async Task ConcurrentCreates_ForOneSlot_HaveExactlyOneWinner()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        for (int round = 0; round < 5; round++)
        {
            var (alice, _) = db.NewAccount();

            CatalogOutcome[] outcomes = await Task.WhenAll(
                catalog.CreateAsync(Call(alice, slot: 1, name: db.NewName("p"))),
                catalog.CreateAsync(Call(alice, slot: 1, name: db.NewName("q"))));
            IReadOnlyList<SummaryView> listed = await Listed(catalog, alice);

            Assert.Equal(new[] { "Created", "SlotOccupied" }, outcomes.Select(o => o.Status).Order());
            Assert.Equal(outcomes.Single(o => o.Status == "Created").Character, Assert.Single(listed));
        }
    }

    [CatalogDatabaseFact]
    public async Task ConcurrentCreates_ForOneNameOnTwoAccounts_HaveExactlyOneWinner()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        for (int round = 0; round < 5; round++)
        {
            var (alice, _) = db.NewAccount();
            var (bob, _) = db.NewAccount();
            string name = db.NewName("c");

            CatalogOutcome[] outcomes = await Task.WhenAll(
                catalog.CreateAsync(Call(alice, slot: 1, name: name)),
                catalog.CreateAsync(Call(bob, slot: 1, name: name.ToLowerInvariant())));
            int listedRows = (await Listed(catalog, alice)).Count + (await Listed(catalog, bob)).Count;

            Assert.Equal(new[] { "Created", "NameTaken" }, outcomes.Select(o => o.Status).Order());
            Assert.Equal(1, listedRows);
        }
    }

    [CatalogDatabaseFact]
    public async Task RetryOfTheSameOperation_ReturnsTheSameCharacterWithoutADuplicate()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        string suffix = db.NewName("r")[^6..];
        CatalogCall first = Call(alice, slot: 2, name: "가나" + suffix, characterClass: CharacterClass.Mage);

        CatalogOutcome original = await catalog.CreateAsync(first);
        // A new RequestContext and a decomposed raw name with the same NFC value are the same logical request.
        CatalogOutcome retried = await catalog.CreateAsync(Call(alice, slot: 2, name: "가나" + suffix,
            characterClass: CharacterClass.Mage, operationId: first.OperationId));
        IReadOnlyList<SummaryView> listed = await Listed(catalog, alice);

        Assert.Equal("Created", original.Status);
        Assert.Equal("Created", retried.Status);
        Assert.Equal(original.Character, retried.Character);
        Assert.Equal(original.Character, Assert.Single(listed));
    }

    public static TheoryData<string, int, string, CharacterClass, bool> ChangedPayloads => new()
    {
        { "display case changed", 1, "AB", CharacterClass.Knight, false },
        { "slot changed", 2, "Ab", CharacterClass.Knight, false },
        { "class changed", 1, "Ab", CharacterClass.Mage, false },
        { "another account", 1, "Ab", CharacterClass.Knight, true },
    };

    [CatalogDatabaseTheory]
    [MemberData(nameof(ChangedPayloads))]
    public async Task SameOperationWithAnotherPayload_IsOperationConflict(
        string change, int slot, string tag, CharacterClass characterClass, bool otherAccount)
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        var (bob, _) = db.NewAccount();
        string stem = db.NewName("x")[..^1];
        CatalogCall first = Call(alice, slot: 1, name: stem + "Ab", characterClass: CharacterClass.Knight);
        await CreateAll(catalog, alice, first);

        CatalogOutcome conflict = await catalog.CreateAsync(Call(otherAccount ? bob : alice, slot: slot,
            name: stem + tag, characterClass: characterClass, operationId: first.OperationId));

        Assert.True(conflict.Status == "OperationConflict", $"{change}: got {conflict.Status}");
        Assert.Single(await Listed(catalog, alice));
        Assert.Empty(await Listed(catalog, bob));
    }

    [CatalogDatabaseFact]
    public async Task RetryAfterABusinessRejection_ReproducesTheRejection()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        string rejectedName = db.NewName("q");
        await CreateAll(catalog, alice, new[] { (1, db.NewName("p"), CharacterClass.Knight) });
        CatalogCall rejectedCall = Call(alice, slot: 1, name: rejectedName);

        CatalogOutcome rejected = await catalog.CreateAsync(rejectedCall);
        CatalogOutcome retried = await catalog.CreateAsync(Again(rejectedCall, 2));
        // The rejected request reserved nothing: a new request may still use that name in a free slot.
        CatalogOutcome freeSlot = await catalog.CreateAsync(Call(alice, slot: 2, name: rejectedName));

        Assert.Equal("SlotOccupied", rejected.Status);
        Assert.Equal("SlotOccupied", retried.Status);
        Assert.Equal("Created", freeSlot.Status);
    }

    [CatalogDatabaseFact]
    public async Task ResolveCreate_ReportsAppliedAndNotAppliedFromTheDurableReceipt()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        CatalogCall applied = Call(alice, slot: 1, name: db.NewName("a"));
        CatalogCall rejected = Call(alice, slot: 1, name: db.NewName("b"));
        CatalogOutcome created = await catalog.CreateAsync(applied);
        CatalogOutcome occupied = await catalog.CreateAsync(rejected);
        Assert.Equal("SlotOccupied", occupied.Status);

        // A new catalog has no memory of the requests; only durable receipts can answer (「생성 transaction과 재시도」).
        CatalogUnderTest restarted = db.NewCatalog();
        CatalogOutcome resolvedApplied = await restarted.ResolveCreateAsync(Recovery(applied));
        CatalogOutcome resolvedRejected = await restarted.ResolveCreateAsync(Recovery(rejected));
        CatalogOutcome resolvedChanged = await restarted.ResolveCreateAsync(Recovery(applied) with { Slot = 2 });

        Assert.Equal("Applied", resolvedApplied.Status);
        Assert.Equal(created.Character, resolvedApplied.Character);
        Assert.Equal("NotApplied", resolvedRejected.Status);
        Assert.Equal("SlotOccupied", resolvedRejected.NotAppliedCode);
        Assert.Equal("OperationConflict", resolvedChanged.Status);
    }

    [CatalogDatabaseFact]
    public async Task ResolveCreate_ForAnOperationNeverSent_IsNeverApplied()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();

        CatalogOutcome resolved = await catalog.ResolveCreateAsync(Call(alice, slot: 1, name: db.NewName("z")));

        Assert.Contains(resolved.Status, new[] { "NotApplied", "StillUnknown" });
        Assert.Empty(await Listed(catalog, alice));
    }

    // An unanswered create may be OutcomeUnknown; the transaction holding dh.Account stands in for a database stall.
    // Releasing that lock does not prove the stalled command ended (pretest supplement v2, 5c), so the answers given
    // before the proof are only recorded. The final reconciliation is judged after the probe shows every stalled session
    // without a request or transaction, the database online with dh.Account free, and a reconciliation that answered
    // inside a separate bounded restart recovery budget; otherwise the run is a test limit. The original user
    // retry keeps its spent deadline (v2). A missing receipt may stay StillUnknown
    // (API v1 「생성 transaction과 재시도」), so the durable rows decide which final answers agree with the request.
    [CatalogDatabaseFact]
    public async Task UnansweredCreate_IsReconciledConsistentlyWithTheOriginalRequest()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        CatalogCall stalled = Call(alice, slot: 1, name: db.NewName("u"));
        await using SqlConnection probe = db.OpenRawConnection();
        HashSet<long> stalledSessions = new();
        bool engaged;
        CatalogOutcome first;
        CatalogOutcome during;
        await using (SqlConnection blocker = db.OpenRawConnection())
        {
            long blockerSession = await Scalar(blocker, "SELECT CAST(@@SPID AS bigint);");
            await using SqlTransaction hold = (SqlTransaction)await blocker.BeginTransactionAsync();
            await using (SqlCommand holdAccounts = new(LockAccounts, blocker, hold))
                await holdAccounts.ExecuteScalarAsync();

            Task<CatalogOutcome> create = catalog.CreateAsync(stalled);
            engaged = await WatchBlocked(probe, blockerSession, stalledSessions, create);
            first = await Answer(create, "the stalled create");
            Task<CatalogOutcome> reconcileDuring = catalog.ResolveCreateAsync(Again(stalled, 2));
            engaged |= await WatchBlocked(probe, blockerSession, stalledSessions, reconcileDuring);
            during = await Answer(reconcileDuring, "the reconciliation during the stall");
            await hold.RollbackAsync();
        }
        string sessions = string.Join(",", stalledSessions);
        _output.WriteLine($"stalled sessions [{sessions}], create {first.Status}, " +
            $"reconciliation during the stall {during.Status} {during.NotAppliedCode}");
        Assert.True(engaged, TestLimit + "no catalog session was seen waiting on the held dh.Account lock: the stall " +
            "did not engage or the probe cannot read other sessions (VIEW SERVER STATE)");

        bool ended = await BecomesZero(probe, StillWorking, TimeSpan.FromSeconds(10), ("@sessions", sessions));
        Assert.True(ended, TestLimit + $"sessions [{sessions}] still had a request or transaction 10 s after the release");
        Assert.True(await Scalar(probe, DatabaseOnline) == 1, TestLimit + "the database is not ONLINE after the stall");
        Exception? locked = await Record.ExceptionAsync(() => Scalar(probe, AccountsFree));
        Assert.True(locked is null, TestLimit + $"dh.Account is still locked after the stall: {locked?.Message}");
        CatalogUnderTest recoveryCatalog = db.NewCatalog();
        CatalogCall reconcile = Recovery(stalled, 3);
        Stopwatch elapsed = Stopwatch.StartNew();
        CatalogOutcome resolved = await Answer(recoveryCatalog.ResolveCreateAsync(reconcile), "the bounded restart reconciliation");
        elapsed.Stop();
        IReadOnlyList<SummaryView> listed = await Listed(catalog, alice);
        _output.WriteLine($"final reconciliation {resolved.Status} {resolved.NotAppliedCode} after " +
            $"{elapsed.ElapsedMilliseconds} ms of a {CallBudget.TotalMilliseconds:0} ms budget; listed {listed.Count}");
        Assert.True(elapsed.Elapsed < CallBudget, TestLimit + "the final reconciliation spent its whole budget");

        Assert.Contains(first.Status, new[] { "OutcomeUnknown", "Unavailable" });
        if (listed.Count == 0)
        {
            // Nothing durable: the request may be declared NotApplied with its confirmed code, or stay unknown.
            Assert.Contains(resolved.Status, new[] { "NotApplied", "StillUnknown" });
            if (resolved.Status == "NotApplied")
                Assert.NotNull(resolved.NotAppliedCode);
        }
        else
        {
            // The character and its receipt commit in one transaction, so a durable character reconciles only as
            // Applied, and a create answered Unavailable promised that no change was dispatched (API v1 「오류 의미」).
            Assert.NotEqual("Unavailable", first.Status);
            Assert.Equal("Applied", resolved.Status);
            Assert.Equal(resolved.Character, Assert.Single(listed));
            CatalogOutcome retried = await catalog.CreateAsync(Again(stalled, 4));
            Assert.Equal("CancelledBeforeDispatch", retried.Status);
            Assert.Equal(resolved.Character, Assert.Single(await Listed(recoveryCatalog, alice)));
        }
        // A decided answer never changes, even when it was given while the stall still held the lock.
        if (during.Status is "Applied" or "NotApplied")
            Assert.Equal(during.Status, resolved.Status);
    }

    const string LockAccounts = "SELECT COUNT(*) FROM dh.Account WITH (TABLOCKX, HOLDLOCK);";
    const string BlockedSessions = "SELECT CAST(session_id AS bigint), CAST(blocking_session_id AS bigint) " +
        "FROM sys.dm_exec_requests WHERE database_id = DB_ID() AND blocking_session_id <> 0;";
    const string StillWorking = "SELECT COUNT_BIG(*) FROM (SELECT session_id FROM sys.dm_exec_requests " +
        "UNION ALL SELECT session_id FROM sys.dm_tran_session_transactions) AS work " +
        "WHERE work.session_id IN (SELECT CAST(value AS int) FROM STRING_SPLIT(@sessions, ','));";
    const string DatabaseOnline =
        "SELECT COUNT_BIG(*) FROM sys.databases WHERE database_id = DB_ID() AND state_desc = N'ONLINE';";
    const string AccountsFree = "SET LOCK_TIMEOUT 0; SELECT COUNT_BIG(*) FROM dh.Account WITH (TABLOCK, HOLDLOCK);";

    /// <summary>
    /// Collects every blocked session of this database until <paramref name="call"/> answers or twice its budget passes;
    /// true when one of them waited on <paramref name="blocker"/> itself.
    /// </summary>
    static async Task<bool> WatchBlocked(SqlConnection probe, long blocker, ISet<long> seen, Task call)
    {
        bool waitedOnBlocker = false;
        DateTime end = DateTime.UtcNow + CallBudget * 2;
        while (!call.IsCompleted && DateTime.UtcNow < end)
        {
            await using (SqlCommand command = new(BlockedSessions, probe))
            await using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    seen.Add(reader.GetInt64(0));
                    waitedOnBlocker |= reader.GetInt64(1) == blocker;
                }
            }
            await Task.Delay(20);
        }
        return waitedOnBlocker;
    }

    static async Task<CatalogOutcome> Answer(Task<CatalogOutcome> call, string step)
    {
        Task done = await Task.WhenAny(call, Task.Delay(CallBudget * 2));
        Assert.True(done == call, $"{step} did not answer within twice its {CallBudget.TotalSeconds:0} s budget");
        return await call;
    }

    static async Task<SummaryView[]> CreateAll(CatalogUnderTest catalog, object account,
        IEnumerable<(int Slot, string Name, CharacterClass Class)> rows)
        => await CreateAll(catalog, account, rows.Select(row => Call(account, row.Slot, row.Name, row.Class)).ToArray());

    static async Task<SummaryView[]> CreateAll(CatalogUnderTest catalog, object account, params CatalogCall[] calls)
    {
        List<SummaryView> created = new();
        foreach (CatalogCall call in calls)
        {
            CatalogOutcome outcome = await catalog.CreateAsync(call with { Account = account });
            Assert.True(outcome.Status == "Created", $"fixture create of {call.Name} ended as {outcome.Status}");
            SummaryView summary = outcome.Character!;
            Assert.Equal((call.Slot, call.Name, call.Class), (summary.Slot, summary.Name, summary.Class));
            created.Add(summary);
        }
        return created.ToArray();
    }

    internal static async Task<IReadOnlyList<SummaryView>> Listed(CatalogUnderTest catalog, object account)
    {
        CatalogOutcome outcome = await catalog.ListAsync(Call(account));
        Assert.True(outcome.Status == "Listed", $"list ended as {outcome.Status}");
        Assert.NotNull(outcome.Characters);
        return outcome.Characters!;
    }
}
