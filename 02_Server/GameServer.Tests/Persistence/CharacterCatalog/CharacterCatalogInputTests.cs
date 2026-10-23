using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// API v1 「값과 한도」, 「공개 작업과 결과」 and 「오류 의미와 우선순위」 at the Core boundary, without a database:
/// AccountContext (non-empty Guid, AccountNameRule name, UUIDv5 match), Slot 1..3, Class Knight/Mage, non-empty
/// OperationId are checked before storage and end as InvalidInput; a token cancelled before dispatch ends as
/// CancelledBeforeDispatch with no storage call; every completion returns the caller's RequestContext unchanged;
/// storage silence ends within the deadline without claiming success; and an operation never runs a blocking storage
/// prefix on the caller's thread (「책임과 호출 경계」: storage entry starts off the tick, like the existing admission).
/// </summary>
public sealed class CharacterCatalogInputTests
{
    public static TheoryData<string, Guid, string?, string> RejectedAccounts => new()
    {
        { CharacterCatalogApi.ListOperation, Guid.Empty, "alice_01", "empty AccountId" },
        { CharacterCatalogApi.ListOperation, OfflineCatalog.User123, "alice_01", "AccountId is the UUIDv5 of another name" },
        { CharacterCatalogApi.ListOperation, OfflineCatalog.Alice01, "ab", "name shorter than 3 characters" },
        { CharacterCatalogApi.ListOperation, OfflineCatalog.Alice01, null, "missing name" },
        { CharacterCatalogApi.CreateOperation, Guid.Empty, "alice_01", "empty AccountId" },
        { CharacterCatalogApi.CreateOperation, OfflineCatalog.User123, "alice_01", "AccountId is the UUIDv5 of another name" },
        { CharacterCatalogApi.CreateOperation, OfflineCatalog.Alice01, "alice-01", "hyphen outside the account rule" },
    };

    [Theory]
    [MemberData(nameof(RejectedAccounts))]
    public async Task RejectedAccount_NeverReachesStorage(string operation, Guid accountId, string? name, string reason)
    {
        using OfflineCatalog offline = new();
        Built<object> account = CharacterCatalogApi.Account(accountId, name);

        // A constructor refusal is the same pre-storage rejection: World cannot submit the value at all.
        CatalogOutcome? outcome = account.Value is null
            ? null
            : await Run(offline, operation, offline.ValidCall() with { Account = account.Value });
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.True(account.Refused is not null || outcome!.Status == "InvalidInput",
            $"{reason}: expected InvalidInput, got {outcome?.Status}");
        Assert.True(outcome?.InvalidField is null or "Account", $"{reason}: InvalidField is {outcome?.InvalidField}");
        Assert.True(storageCalls.Count == 0, $"{reason}: storage was called: {string.Join(", ", storageCalls)}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(255)]
    public async Task SlotOutsideOneToThree_IsInvalidInputBeforeStorage(int slot)
    {
        using OfflineCatalog offline = new();

        CatalogOutcome outcome = await offline.Catalog.CreateAsync(offline.ValidCall() with { Slot = slot });
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("InvalidInput", outcome.Status);
        Assert.True(outcome.InvalidField is null or "Slot", $"InvalidField is {outcome.InvalidField}");
        Assert.Empty(storageCalls);
    }

    [Theory]
    [InlineData((byte)2)]
    [InlineData((byte)255)]
    public async Task ClassOtherThanKnightOrMage_IsInvalidInputBeforeStorage(byte value)
    {
        using OfflineCatalog offline = new();

        CatalogOutcome outcome = await offline.Catalog.CreateAsync(offline.ValidCall() with { Class = (CharacterClass)value });
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("InvalidInput", outcome.Status);
        Assert.True(outcome.InvalidField is null or "Class", $"InvalidField is {outcome.InvalidField}");
        Assert.Empty(storageCalls);
    }

    [Fact]
    public async Task EmptyOperationId_IsInvalidInputBeforeStorage()
    {
        using OfflineCatalog offline = new();

        CatalogOutcome outcome = await offline.Catalog.CreateAsync(offline.ValidCall() with { OperationId = Guid.Empty });
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("InvalidInput", outcome.Status);
        Assert.True(outcome.InvalidField is null or "OperationId", $"InvalidField is {outcome.InvalidField}");
        Assert.Empty(storageCalls);
    }

    [Theory]
    [InlineData(CharacterCatalogApi.ListOperation)]
    [InlineData(CharacterCatalogApi.CreateOperation)]
    public async Task TokenCancelledBeforeDispatch_IsCancelledBeforeDispatchWithoutStorage(string operation)
    {
        using OfflineCatalog offline = new();
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        CatalogOutcome outcome = await Run(offline, operation, offline.ValidCall(cancelled.Token));
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("CancelledBeforeDispatch", outcome.Status);
        Assert.Empty(storageCalls);
    }

    // RequestContext is a correlation value returned with the completion (API v1 「공개 작업과 결과」); World compares it
    // with its current generation, so Core must not drop or rewrite it, including at the Int64 bounds.
    [Theory]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public async Task Completion_ReturnsTheCallersRequestContextUnchanged(long generation)
    {
        using OfflineCatalog offline = new();
        object request = OfflineCatalog.Request(OfflineCatalog.SessionB, generation);

        // Preparation has no RequestContext in v2. Use an invalid operation ID to reject at the facade itself.
        CatalogOutcome rejected = await offline.Catalog.CreateAsync(offline.ValidCall() with
        {
            Request = request,
            OperationId = Guid.Empty,
        });

        Assert.True(rejected.HasRequestContext, $"{rejected.Raw.GetType().Name} carries no RequestContext");
        Assert.Equal(request, rejected.RequestContext);
    }

    [Fact]
    public async Task StorageSilence_EndsTheListAsUnavailableWithinTheDeadline()
    {
        using OfflineCatalog offline = new();
        Task<CatalogOutcome> listing = offline.Catalog.ListAsync(offline.ValidCall());
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);

        await offline.ExpireBudgetUntil(listing);
        CatalogOutcome outcome = await listing;

        Assert.Equal("Unavailable", outcome.Status);
    }

    // A create whose storage call never answered may or may not have changed the database (API v1 「오류 의미」):
    // it ends bounded as OutcomeUnknown, or as Unavailable only when no change can have started. Never Created.
    [Fact]
    public async Task StorageSilence_EndsTheCreateBoundedWithoutClaimingAnOutcome()
    {
        using OfflineCatalog offline = new();
        Task<CatalogOutcome> creating = offline.Catalog.CreateAsync(offline.ValidCall());
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);

        await offline.ExpireBudgetUntil(creating);
        CatalogOutcome outcome = await creating;

        Assert.Contains(outcome.Status, new[] { "OutcomeUnknown", "Unavailable" });
    }

    [Theory]
    [InlineData(CharacterCatalogApi.ListOperation)]
    [InlineData(CharacterCatalogApi.CreateOperation)]
    [InlineData(CharacterCatalogApi.SelectOperation)]
    public async Task Entry_ReturnsWhileStorageBlocksItsCallingThread(string operation)
    {
        using OfflineCatalog offline = new(StoreCallRecorder.Mode.BlockCaller);
        Task<object?> entry = Task.Run(() => offline.Catalog.Invoke(operation, offline.ValidCall()));
        try
        {
            Task first = await Task.WhenAny(entry, Task.Delay(TimeSpan.FromSeconds(2)));
            bool returned = first == entry && entry.IsCompletedSuccessfully;

            Assert.True(returned, $"{operation} did not return while a storage call blocked its thread");
        }
        finally
        {
            offline.Store.Unblock();
        }
    }

    static Task<CatalogOutcome> Run(OfflineCatalog offline, string operation, CatalogCall call) => operation switch
    {
        CharacterCatalogApi.ListOperation => offline.Catalog.ListAsync(call),
        CharacterCatalogApi.CreateOperation => offline.Catalog.CreateAsync(call),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };
}
