using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Independent verifier cases. Expected values come from API v1 「공개 API」·「오류 의미」 (Created/Applied return the
/// summary of that create; selection returns the stored class and summary; IntegrityFailure for broken data invariants)
/// and API v2 「소유권」 (a closed host does not dispatch; a pre-dispatch refusal is a confirmed no-change result).
/// Storage answers are literal vectors released through the existing recording repositories.
/// </summary>
public sealed class CharacterCatalogIndependentContractTests
{
    static readonly Guid OtherCharacter = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Theory]
    [InlineData(false, "slot")]
    [InlineData(false, "display name")]
    [InlineData(false, "class")]
    [InlineData(false, "empty character id")]
    [InlineData(true, "slot")]
    [InlineData(true, "display name")]
    [InlineData(true, "class")]
    [InlineData(true, "empty character id")]
    public async Task StoredSummaryThatIsNotTheRequestedCreate_IsIntegrityFailureWithoutPublishingIt(bool resolve, string change)
    {
        RecordingCatalogRepository repository = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, new Repo(), PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 21);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        CreateDescriptor descriptor = service.PrepareCreate((AccountContext)OfflineCatalog.AliceAccount(),
            1, "Dawn01", CharacterClass.Knight).Descriptor!;
        CharacterSummary stored = change switch
        {
            "slot" => new(OfflineCatalog.CharacterA, 2, "Dawn01", CharacterClass.Knight),
            "display name" => new(OfflineCatalog.CharacterA, 1, "DAWN01", CharacterClass.Knight),
            "class" => new(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Mage),
            "empty character id" => new(Guid.Empty, 1, "Dawn01", CharacterClass.Knight),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };

        Task<CatalogResult> answer = resolve
            ? service.ResolveCreateAsync(descriptor, OfflineCatalog.OperationA, request, budget, CancellationToken.None)
            : service.CreateCharacterAsync(descriptor, OfflineCatalog.OperationA, request, budget, CancellationToken.None);
        RecordingCatalogRepository.Call call = await repository.Next(resolve ? "Resolve" : "Create");
        call.Complete(new(resolve ? CatalogStatus.Applied : CatalogStatus.Created, stored));
        CatalogResult result = await answer.WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(CatalogStatus.IntegrityFailure, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Null(result.Character);
        Assert.Empty(result.Characters);
        Assert.Null(result.Lease);
        await service.FlushAsync(budget).WaitAsync(StoreCallRecorder.Limit);
    }

    [Theory]
    [InlineData("another character")]
    [InlineData("slot four")]
    [InlineData("invalid class")]
    public async Task SelectedSummaryThatIsNotTheRequestedCharacter_IsIntegrityFailureBeforeAdmission(string corruption)
    {
        RecordingCatalogRepository repository = new();
        Repo characters = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, characters, PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 22);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        CharacterSummary stored = corruption switch
        {
            "another character" => new(OtherCharacter, 1, "Dawn01", CharacterClass.Knight),
            "slot four" => new(OfflineCatalog.CharacterA, 4, "Dawn01", CharacterClass.Knight),
            "invalid class" => new(OfflineCatalog.CharacterA, 1, "Dawn01", (CharacterClass)255),
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };

        CharacterSelectionHandle handle = service.BeginSelectCharacter((AccountContext)OfflineCatalog.AliceAccount(),
            OfflineCatalog.CharacterA, OfflineCatalog.OperationA, request, budget, CancellationToken.None);
        RecordingCatalogRepository.Call selection = await repository.Next("Select");
        selection.Complete(new(CatalogStatus.Selected, stored));
        CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(CatalogStatus.IntegrityFailure, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);
        await service.FlushAsync(budget).WaitAsync(StoreCallRecorder.Limit);
        Assert.Empty(characters.Calls);
    }

    [Fact]
    public async Task AcquiredClassOtherThanTheCatalogClass_IsIntegrityFailureAndReleasesTheSameOwner()
    {
        RecordingCatalogRepository repository = new();
        Repo characters = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, characters, PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 23);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        PersistenceIdentity identity = new(1, OfflineCatalog.Alice01, OfflineCatalog.CharacterA);

        CharacterSelectionHandle handle = service.BeginSelectCharacter((AccountContext)OfflineCatalog.AliceAccount(),
            OfflineCatalog.CharacterA, OfflineCatalog.OperationA, request, budget, CancellationToken.None);
        RecordingCatalogRepository.Call selection = await repository.Next("Select");
        selection.Complete(new(CatalogStatus.Selected, new CharacterSummary(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Mage)));
        var probe = Assert.IsType<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>>(await characters.NextAnyAsync());
        probe.Complete(ReadAdmissionResult.Probe(new(new(identity, PersistenceOwnerKind.Free, null, 7, 0), true, true)));
        var acquire = await characters.NextAcquireAsync();
        Assert.Equal(CharacterClass.Mage, acquire.Request.IntendedClass);
        // The literal receipt helper records a Knight row; the catalog row above says Mage.
        PersistenceAuthority held = new(identity, PersistenceOwnerKind.Runtime, acquire.Request.Operation.OwnerId, 8, 0);
        PersistenceToken character = PersistenceTestValues.Token(1);
        PersistenceToken progress = PersistenceTestValues.Token(2);
        acquire.Complete(AcquireAndLoadResult.Acquired(
            PersistenceTestValues.AcquiredReceipt(OfflineCatalog.OperationA, held, character, progress),
            new(held, character, progress)));

        CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(CatalogStatus.IntegrityFailure, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        var release = await characters.NextReleaseAsync();
        Assert.Equal(held, release.Request.Authority);
        PersistenceAuthority free = new(identity, PersistenceOwnerKind.Free, null, 9, 0);
        release.Complete(ReleaseResult.Released(
            PersistenceTestValues.ReleasedReceipt(release.Request.Operation.OperationId, free), new(free, null, null)));
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);
        await service.FlushAsync(budget).WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, characters.Calls);
    }

    [Fact]
    public async Task ClosedHost_RefusesEveryEntryWithoutStorageWork()
    {
        RecordingCatalogRepository repository = new();
        Repo characters = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, characters, PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 24);
        AccountContext account = (AccountContext)OfflineCatalog.AliceAccount();
        CreateDescriptor descriptor = service.PrepareCreate(account, 1, "Dawn01", CharacterClass.Knight).Descriptor!;
        PersistenceDeadline Budget() => PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        await service.FlushAsync(Budget()).WaitAsync(StoreCallRecorder.Limit);

        CatalogResult listed = await service.ListCharactersAsync(account, request, Budget(), CancellationToken.None)
            .WaitAsync(StoreCallRecorder.Limit);
        CatalogResult created = await service.CreateCharacterAsync(descriptor, OfflineCatalog.OperationA, request, Budget(),
            CancellationToken.None).WaitAsync(StoreCallRecorder.Limit);
        CatalogResult resolved = await service.ResolveCreateAsync(descriptor, OfflineCatalog.OperationA, request, Budget(),
            CancellationToken.None).WaitAsync(StoreCallRecorder.Limit);
        CharacterSelectionHandle handle = service.BeginSelectCharacter(account, OfflineCatalog.CharacterA,
            OfflineCatalog.OperationB, request, Budget(), CancellationToken.None);
        CatalogResult selected = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);

        // API v2 allows any confirmed no-change code here; the contract is "no dispatch", not one chosen code.
        CatalogStatus[] confirmedNoChange = [CatalogStatus.Unavailable, CatalogStatus.CancelledBeforeDispatch];
        Assert.Contains(listed.Status, confirmedNoChange);
        Assert.Contains(created.Status, confirmedNoChange);
        // Without a storage read a receipt cannot be confirmed either way.
        Assert.Contains(resolved.Status, new[] { CatalogStatus.StillUnknown, CatalogStatus.Unavailable });
        Assert.Contains(selected.Status, new[] { CatalogStatus.InvalidState }.Concat(confirmedNoChange));
        Assert.All(new[] { listed, created, resolved, selected }, result => Assert.Equal(request, result.Request));
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);
        Assert.Empty(repository.Calls);
        Assert.Empty(characters.Calls);
    }

    [Fact]
    public async Task ResolveWithAnotherIntentForAPendingOperation_IsOperationConflictWithoutStorage()
    {
        RecordingCatalogRepository repository = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, new Repo(), PersistenceTestValues.Defaults(), clock);
        RequestContext request = new(OfflineCatalog.SessionA, 25);
        AccountContext account = (AccountContext)OfflineCatalog.AliceAccount();
        PersistenceDeadline Budget() => PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        CreateDescriptor original = service.PrepareCreate(account, 1, "Dawn01", CharacterClass.Knight).Descriptor!;
        CreateDescriptor other = service.PrepareCreate(account, 1, "Dawn02", CharacterClass.Knight).Descriptor!;
        Task<CatalogResult> creating = service.CreateCharacterAsync(original, OfflineCatalog.OperationA, request, Budget(),
            CancellationToken.None);
        RecordingCatalogRepository.Call dispatched = await repository.Next("Create");

        CatalogResult conflict = await service.ResolveCreateAsync(other, OfflineCatalog.OperationA,
            request with { Generation = 26 }, Budget(), CancellationToken.None).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(CatalogStatus.OperationConflict, conflict.Status);
        Assert.Null(conflict.Character);
        Assert.Single(repository.Calls);
        dispatched.Complete(new(CatalogStatus.NameTaken));
        Assert.Equal(CatalogStatus.NameTaken, (await creating.WaitAsync(StoreCallRecorder.Limit)).Status);
        await service.FlushAsync(Budget()).WaitAsync(StoreCallRecorder.Limit);
    }
}
