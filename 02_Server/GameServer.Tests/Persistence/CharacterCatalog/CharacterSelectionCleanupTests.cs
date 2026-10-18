using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Close is placed at a known storage barrier, not at an arbitrary delay after selection starts.</summary>
public sealed class CharacterSelectionCleanupTests
{
    [Fact]
    public async Task CloseBeforeAcquire_LateProbeCannotAcquireOrPublishALease()
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        await fixture.AnswerSelection();
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe =
            Assert.IsType<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>>(await fixture.Characters.NextAnyAsync());
        Task closing = handle.Close(null);
        fixture.AnswerProbe(probe);

        CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
        await closing.WaitAsync(StoreCallRecorder.Limit);
        Assert.NotEqual(CatalogStatus.Selected, result.Status);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        Assert.Equal(new[] { Repo.Admission }, fixture.Characters.Calls);
        await fixture.Service.FlushAsync(fixture.Budget()).WaitAsync(StoreCallRecorder.Limit);
    }

    [Fact]
    public async Task CloseDuringAcquire_LateSuccessReleasesTheSameOwnerWithoutPublishingALease()
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        await fixture.AnswerSelection();
        var probe = Assert.IsType<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>>(
            await fixture.Characters.NextAnyAsync());
        fixture.AnswerProbe(probe);
        var acquire = await fixture.Characters.NextAcquireAsync();
        Task closing = handle.Close(null);
        Assert.Same(closing, handle.Close(null));
        PersistenceAuthority held = new(fixture.Identity, PersistenceOwnerKind.Runtime,
            acquire.Request.Operation.OwnerId, 8, 0);
        PersistenceToken character = PersistenceTestValues.Token(1);
        PersistenceToken progress = PersistenceTestValues.Token(2);
        OperationReceipt receipt = PersistenceTestValues.AcquiredReceipt(acquire.Request.Operation.OperationId,
            held, character, progress);
        acquire.Complete(AcquireAndLoadResult.Acquired(receipt, new(held, character, progress)));

        var release = await fixture.Characters.NextReleaseAsync();
        Assert.Equal(held.OwnerId, release.Request.Authority.OwnerId);
        Assert.Equal(8, release.Request.Authority.Fence);
        Assert.Equal(fixture.Identity, release.Request.Authority.Identity);
        PersistenceAuthority free = new(fixture.Identity, PersistenceOwnerKind.Free, null, 9, 0);
        release.Complete(ReleaseResult.Released(
            PersistenceTestValues.ReleasedReceipt(release.Request.Operation.OperationId, free), new(free, null, null)));

        CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
        await closing.WaitAsync(StoreCallRecorder.Limit);
        Assert.NotEqual(CatalogStatus.Selected, result.Status);
        Assert.Null(result.Character);
        Assert.Null(result.Lease);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Characters.Calls);
        await fixture.Service.FlushAsync(fixture.Budget()).WaitAsync(StoreCallRecorder.Limit);
    }

    [Fact]
    public async Task IncompleteClose_RemainsTrackedByHost()
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        RecordingCatalogRepository.Call selection = await fixture.Repository.Next("Select");
        Task close = handle.Close(null);
        fixture.Clock.Advance(TimeSpan.FromSeconds(3));
        Assert.Same(close, await Task.WhenAny(close, Task.Delay(StoreCallRecorder.Limit)));
        await Assert.ThrowsAsync<TimeoutException>(() => close);
        selection.Complete(new(CatalogStatus.Unavailable));
        await handle.Completion.WaitAsync(StoreCallRecorder.Limit);

        Task flush = fixture.Service.FlushAsync(fixture.Budget());
        Assert.Same(flush, await Task.WhenAny(flush, Task.Delay(StoreCallRecorder.Limit)));
        await Assert.ThrowsAsync<TimeoutException>(() => flush);
        Assert.Null((await handle.Completion).Lease);
    }

    sealed class Fixture
    {
        public Fixture() => Service = new(Repository, Characters, PersistenceTestValues.Defaults(), Clock);

        public RecordingCatalogRepository Repository { get; } = new();
        public Repo Characters { get; } = new();
        public ControllableTimeProvider Clock { get; } = new();
        public CharacterCatalogService Service { get; }
        public PersistenceIdentity Identity { get; } = new(1, OfflineCatalog.Alice01, OfflineCatalog.CharacterA);
        public PersistenceDeadline Budget() => PersistenceDeadline.FromBudget(Clock, TimeSpan.FromSeconds(5));

        public CharacterSelectionHandle Begin() => Service.BeginSelectCharacter(
            (AccountContext)OfflineCatalog.AliceAccount(), OfflineCatalog.CharacterA, OfflineCatalog.OperationA,
            new(OfflineCatalog.SessionA, 7), Budget(), CancellationToken.None);

        public async Task AnswerSelection()
        {
            RecordingCatalogRepository.Call selection = await Repository.Next("Select");
            selection.Complete(new(CatalogStatus.Selected,
                new CharacterSummary(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Knight)));
        }

        public void AnswerProbe(Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe)
            => probe.Complete(ReadAdmissionResult.Probe(new(
                new(Identity, PersistenceOwnerKind.Free, null, 7, 0), true, true)));
    }
}
