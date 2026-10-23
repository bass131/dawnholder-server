using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// API v1 requires corrupted stored lists to return IntegrityFailure. These literal vectors test the facade;
/// they do not replace the SQL adapter's response/error classification or an actual database corruption fixture.
/// </summary>
public sealed class CharacterCatalogIntegrityBoundaryTests
{
    [Theory]
    [InlineData("too many characters")]
    [InlineData("duplicate slot")]
    [InlineData("duplicate character")]
    [InlineData("invalid class")]
    [InlineData("invalid name")]
    [InlineData("empty character id")]
    [InlineData("slot zero")]
    [InlineData("slot four")]
    [InlineData("noncanonical name")]
    public async Task CorruptedStoredList_IsIntegrityFailureWithoutPublishingCharacters(string corruption)
    {
        RecordingCatalogRepository repository = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, new RecordingCharacterRepository(),
            PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 17);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        CharacterSummary first = new(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Knight);
        CharacterSummary second = new(Guid.Parse("11111111-2222-3333-4444-555555555555"),
            2, "Dawn02", CharacterClass.Mage);
        CharacterSummary[] stored = corruption switch
        {
            "too many characters" => [first, second,
                new(Guid.Parse("22222222-3333-4444-5555-666666666666"), 3, "Dawn03", CharacterClass.Knight),
                new(Guid.Parse("33333333-4444-5555-6666-777777777777"), 1, "Dawn04", CharacterClass.Mage)],
            "duplicate slot" => [first, second with { Slot = 1 }],
            "duplicate character" => [first, second with { CharacterId = first.CharacterId }],
            "invalid class" => [first, second with { Class = (CharacterClass)255 }],
            "invalid name" => [first, second with { Name = "bad name" }],
            "empty character id" => [first, second with { CharacterId = Guid.Empty }],
            "slot zero" => [first, second with { Slot = 0 }],
            "slot four" => [first, second with { Slot = 4 }],
            "noncanonical name" => [first, second with { Name = "\u1100\u1161\u1102\u1161" }],
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };

        Task<CatalogResult> listing = service.ListCharactersAsync(
            (AccountContext)OfflineCatalog.AliceAccount(), request, budget, CancellationToken.None);
        RecordingCatalogRepository.Call call = await repository.Next("List");
        call.Complete(new(CatalogStatus.Listed, characters: stored));
        CatalogResult result = await listing.WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(CatalogStatus.IntegrityFailure, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Empty(result.Characters);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        Assert.Single(repository.Calls);
        await service.FlushAsync(budget).WaitAsync(StoreCallRecorder.Limit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ValidStoredList_PreservesCorrelationAndPublishesAnImmutableOrderedSnapshot(int count)
    {
        RecordingCatalogRepository repository = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, new RecordingCharacterRepository(),
            PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 18);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        CharacterSummary first = new(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Knight);
        CharacterSummary second = new(Guid.Parse("11111111-2222-3333-4444-555555555555"),
            2, "Dawn02", CharacterClass.Mage);
        CharacterSummary third = new(Guid.Parse("22222222-3333-4444-5555-666666666666"),
            3, "Dawn03", CharacterClass.Knight);
        CharacterSummary[] stored = count switch
        {
            0 => [],
            1 => [first],
            3 => [third, first, second],
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };
        CharacterSummary[] expected = count switch
        {
            0 => [],
            1 => [first],
            3 => [first, second, third],
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };

        Task<CatalogResult> listing = service.ListCharactersAsync(
            (AccountContext)OfflineCatalog.AliceAccount(), request, budget, CancellationToken.None);
        RecordingCatalogRepository.Call call = await repository.Next("List");
        call.Complete(new(CatalogStatus.Listed, characters: stored));
        CatalogResult result = await listing.WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(CatalogStatus.Listed, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Equal(expected, result.Characters);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        Assert.Single(repository.Calls);
        if (count > 0)
        {
            stored[0] = first with { Name = "Changed" };
            Assert.Equal(expected, result.Characters);
            IList<CharacterSummary> published = Assert.IsAssignableFrom<IList<CharacterSummary>>(result.Characters);
            Assert.True(published.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => published[0] = third);
            Assert.Equal(expected, result.Characters);
        }
        await service.FlushAsync(budget).WaitAsync(StoreCallRecorder.Limit);
    }
}
