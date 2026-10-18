using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// A real catalog over <see cref="StoreCallRecorder"/> and a controllable clock, with literal request values. The
/// account IDs are the independent Python uuid5 vectors kept in AccountIdResolverTests; no ID is computed here.
/// </summary>
sealed class OfflineCatalog : IDisposable
{
    public static readonly Guid Alice01 = new("d0064115-982b-56a6-ae70-528b3cde4a09");
    public static readonly Guid User123 = new("18be3028-ade2-5ad0-9ffa-0fd51a36f461");
    public static readonly Guid SessionA = new("c4a7a10c-0000-4000-8000-000000000001");
    public static readonly Guid SessionB = new("c4a7a10c-0000-4000-8000-000000000002");
    public static readonly Guid OperationA = new("c4a7a10c-0000-4000-8000-0000000000a1");
    public static readonly Guid OperationB = new("c4a7a10c-0000-4000-8000-0000000000b2");
    public static readonly Guid CharacterA = new("c4a7a10c-0000-4000-8000-0000000000c1");

    public OfflineCatalog(StoreCallRecorder.Mode mode = StoreCallRecorder.Mode.Pending)
    {
        Store = new StoreCallRecorder(mode);
        Catalog = CharacterCatalogApi.Offline(Store, Time);
    }

    public StoreCallRecorder Store { get; }
    public ControllableTimeProvider Time { get; } = new();
    public CatalogUnderTest Catalog { get; }

    public static object AliceAccount()
    {
        Built<object> account = CharacterCatalogApi.Account(Alice01, "alice_01");
        Assert.True(account.Value is not null, $"A valid AccountContext was refused: {account.Refused}");
        return account.Value!;
    }

    public static object Request(Guid session, long generation) => CharacterCatalogApi.Request(session, generation);

    /// <summary>The default public operation budget of API v1 「값과 한도」 Deadline (5 seconds).</summary>
    public PersistenceDeadline Deadline() => PersistenceDeadline.FromBudget(Time, TimeSpan.FromSeconds(5));

    /// <summary>A structurally valid call; tests replace the one field they make invalid.</summary>
    public CatalogCall ValidCall(CancellationToken token = default) => new(
        AliceAccount(),
        Request(SessionA, 7),
        Deadline(),
        token,
        Slot: 1,
        Name: "Dawn01",
        Class: CharacterClass.Mage,
        OperationId: OperationA,
        CharacterId: CharacterA);

    /// <summary>
    /// Moves the clock past the 5-second budget until <paramref name="waiting"/> ends. The product may arm its deadline
    /// timer just after storage is reached, so one advance could land first; repeated advances only add expired time.
    /// </summary>
    public async Task ExpireBudgetUntil(Task waiting)
    {
        for (int attempt = 0; attempt < 20 && !waiting.IsCompleted; attempt++)
        {
            Time.Advance(TimeSpan.FromSeconds(6));
            await Task.WhenAny(waiting, Task.Delay(TimeSpan.FromMilliseconds(50)));
        }
    }

    /// <summary>Gives a product continuation that may still be scheduled a moment to reach storage.</summary>
    public async Task<IReadOnlyList<string>> CallsAfterSettling()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        return Store.Calls;
    }

    public void Dispose() => Store.Dispose();
}
