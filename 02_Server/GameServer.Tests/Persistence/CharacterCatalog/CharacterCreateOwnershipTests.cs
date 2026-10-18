using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>API v2 host ownership before dispatch, original user budget and separate bounded recovery.</summary>
public sealed class CharacterCreateOwnershipTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4097)]
    public void InvalidCapacity_IsRefused(int capacity)
    {
        Fixture fixture = new();
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterCatalogService(fixture.Repository,
            fixture.Characters, PersistenceTestValues.Defaults(), fixture.Clock, capacity));
        Assert.Empty(fixture.Repository.Calls);
    }

    [Fact]
    public async Task FullHost_RejectsAnotherOperationBeforeDispatch()
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");

        CatalogResult refused = await fixture.Create(OfflineCatalog.OperationB);

        Assert.Equal(CatalogStatus.Unavailable, refused.Status);
        Assert.Single(fixture.Repository.Calls);
        Assert.False(first.IsCompleted);
        dispatched.Complete(new(CatalogStatus.NameTaken));
        Assert.Equal(CatalogStatus.NameTaken, (await first.WaitAsync(StoreCallRecorder.Limit)).Status);
    }

    [Fact]
    public async Task SameOperation_JoinsTheRegisteredWorkWithoutASecondDispatch()
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        RequestContext secondRequest = new(OfflineCatalog.SessionB, 123);
        Task<CatalogResult> second = fixture.Service.CreateCharacterAsync(fixture.Descriptor, OfflineCatalog.OperationA,
            secondRequest, fixture.Budget(), CancellationToken.None);

        Assert.Single(fixture.Repository.Calls);
        Assert.False(second.IsCompleted);
        dispatched.Complete(new(CatalogStatus.SlotOccupied));
        CatalogResult[] results = await Task.WhenAll(first, second).WaitAsync(StoreCallRecorder.Limit);
        Assert.All(results, result => Assert.Equal(CatalogStatus.SlotOccupied, result.Status));
        Assert.Equal(secondRequest, results[1].Request);
    }

    [Theory]
    [InlineData("slot")]
    [InlineData("name")]
    [InlineData("class")]
    [InlineData("account")]
    public async Task SameOperationWithChangedIntent_IsConflictWithoutDispatch(string change)
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        AccountContext account = change == "account"
            ? new(OfflineCatalog.User123, "user_123") : fixture.Descriptor.Account;
        CreateDescriptor changed = fixture.Service.PrepareCreate(account, change == "slot" ? (byte)2 : (byte)1,
            change == "name" ? "DAWN01" : "Dawn01",
            change == "class" ? CharacterClass.Mage : CharacterClass.Knight).Descriptor!;

        CatalogResult conflict = await fixture.Service.CreateCharacterAsync(changed, OfflineCatalog.OperationA,
            fixture.Request, fixture.Budget(), CancellationToken.None);

        Assert.Equal(CatalogStatus.OperationConflict, conflict.Status);
        Assert.Single(fixture.Repository.Calls);
        dispatched.Complete(new(CatalogStatus.NameTaken));
        await first.WaitAsync(StoreCallRecorder.Limit);
    }

    [Fact]
    public async Task CallerCancellation_KeepsOriginalIdAndDescriptorForHostRecovery()
    {
        Fixture fixture = new();
        using CancellationTokenSource caller = new();
        Task<CatalogResult> first = fixture.Create(token: caller.Token);
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        caller.Cancel();

        CatalogResult unknown = await first.WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(CatalogStatus.OutcomeUnknown, unknown.Status);
        Assert.Equal(fixture.Request, unknown.Request);
        Assert.False(dispatched.Token.IsCancellationRequested);
        CatalogResult full = await fixture.Create(OfflineCatalog.OperationB);
        Assert.Equal(CatalogStatus.Unavailable, full.Status);
        dispatched.Complete(new(CatalogStatus.OutcomeUnknown));
        RecordingCatalogRepository.Call recovery = await fixture.Repository.Next("Resolve");
        Assert.Same(fixture.Descriptor, recovery.Descriptor);
        Assert.Equal(OfflineCatalog.OperationA, recovery.OperationId);
        recovery.Complete(new(CatalogStatus.NotApplied, rejection: CatalogStatus.NameTaken));
        await fixture.Service.FlushAsync(fixture.Budget()).WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(CatalogStatus.OutcomeUnknown, first.Result.Status);
        Assert.Equal(new[] { "Create", "Resolve" }, fixture.Repository.Calls.Select(call => call.Method));
    }

    [Fact]
    public async Task NewGeneration_DoesNotRenewTheOriginalUserDeadline()
    {
        Fixture fixture = new();
        PersistenceDeadline original = fixture.Budget();
        Task<CatalogResult> first = fixture.Service.CreateCharacterAsync(fixture.Descriptor, OfflineCatalog.OperationA,
            fixture.Request, original, CancellationToken.None);
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        fixture.Clock.Advance(TimeSpan.FromSeconds(4));
        Task<CatalogResult> next = fixture.Service.CreateCharacterAsync(fixture.Descriptor, OfflineCatalog.OperationA,
            fixture.Request with { Generation = 8 }, fixture.Budget(), CancellationToken.None);
        fixture.Clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(CatalogStatus.OutcomeUnknown, (await first.WaitAsync(StoreCallRecorder.Limit)).Status);
        Assert.Equal(CatalogStatus.OutcomeUnknown, (await next.WaitAsync(StoreCallRecorder.Limit)).Status);
        RecordingCatalogRepository.Call recovery = await fixture.Repository.Next("Resolve");
        Assert.Same(original, dispatched.Deadline);
        Assert.NotSame(original, recovery.Deadline);
        Assert.True(original.IsExpired);
        Assert.InRange(recovery.Deadline.GetRemaining(), TimeSpan.FromTicks(1), TimeSpan.FromSeconds(5));
        CatalogResult resolve = await fixture.Service.ResolveCreateAsync(fixture.Descriptor, OfflineCatalog.OperationA,
            fixture.Request with { Generation = 9 }, fixture.Budget(), CancellationToken.None);
        Assert.Equal(CatalogStatus.StillUnknown, resolve.Status);
        Assert.Equal(2, fixture.Repository.Calls.Count);
        // End the ignored driver task too; its late answer cannot republish success to either user task.
        dispatched.Complete(new(CatalogStatus.NameTaken));
        recovery.Complete(new(CatalogStatus.NotApplied, rejection: CatalogStatus.NameTaken));
        await fixture.Service.FlushAsync(fixture.Budget()).WaitAsync(StoreCallRecorder.Limit);
    }

    [Fact]
    public async Task RecoveryExhaustion_RemainsIncompleteAndDoesNotHotLoop()
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        dispatched.Complete(new(CatalogStatus.OutcomeUnknown));
        Assert.Equal(CatalogStatus.OutcomeUnknown, (await first.WaitAsync(StoreCallRecorder.Limit)).Status);
        RecordingCatalogRepository.Call recovery = await fixture.Repository.Next("Resolve");
        recovery.Complete(new(CatalogStatus.StillUnknown));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.FlushAsync(fixture.Budget()));
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.FlushAsync(fixture.Budget()));
        Assert.Equal(new[] { "Create", "Resolve" }, fixture.Repository.Calls.Select(call => call.Method));
    }

    [Fact]
    public async Task RecoveryBudgetExpiry_IsBoundedWithoutChangingTheUserOutcome()
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        dispatched.Complete(new(CatalogStatus.OutcomeUnknown));
        Assert.Equal(CatalogStatus.OutcomeUnknown, (await first.WaitAsync(StoreCallRecorder.Limit)).Status);
        RecordingCatalogRepository.Call recovery = await fixture.Repository.Next("Resolve");
        Task closing = fixture.Service.FlushAsync(fixture.Budget());

        fixture.Clock.Advance(TimeSpan.FromSeconds(6));

        Assert.Same(closing, await Task.WhenAny(closing, Task.Delay(StoreCallRecorder.Limit)));
        await Assert.ThrowsAnyAsync<Exception>(() => closing);
        Assert.True(recovery.Token.IsCancellationRequested);
        Assert.Equal(CatalogStatus.OutcomeUnknown, first.Result.Status);
        Assert.Equal(new[] { "Create", "Resolve" }, fixture.Repository.Calls.Select(call => call.Method));
        recovery.Complete(new(CatalogStatus.StillUnknown));
    }

    [Fact]
    public async Task Shutdown_TracksPendingWorkAndRejectsNewDispatch()
    {
        Fixture fixture = new();
        Task<CatalogResult> first = fixture.Create();
        RecordingCatalogRepository.Call dispatched = await fixture.Repository.Next("Create");
        Task closing = fixture.Service.FlushAsync(fixture.Budget());

        Assert.False(closing.IsCompleted);
        Assert.Equal(CatalogStatus.Unavailable, (await fixture.Create(OfflineCatalog.OperationB)).Status);
        Assert.Single(fixture.Repository.Calls);
        dispatched.Complete(new(CatalogStatus.NameTaken));
        await first.WaitAsync(StoreCallRecorder.Limit);
        await closing.WaitAsync(StoreCallRecorder.Limit);
    }

    sealed class Fixture
    {
        public Fixture()
        {
            Service = new(Repository, Characters, PersistenceTestValues.Defaults(), Clock, capacity: 1);
            Descriptor = Service.PrepareCreate((AccountContext)OfflineCatalog.AliceAccount(),
                1, "Dawn01", CharacterClass.Knight).Descriptor!;
        }

        public RecordingCatalogRepository Repository { get; } = new();
        public RecordingCharacterRepository Characters { get; } = new();
        public ControllableTimeProvider Clock { get; } = new();
        public CharacterCatalogService Service { get; }
        public CreateDescriptor Descriptor { get; }
        public RequestContext Request { get; } = new(OfflineCatalog.SessionA, 7);

        public PersistenceDeadline Budget() => PersistenceDeadline.FromBudget(Clock, TimeSpan.FromSeconds(5));

        public Task<CatalogResult> Create(Guid? operation = null, CancellationToken token = default)
            => Service.CreateCharacterAsync(Descriptor, operation ?? OfflineCatalog.OperationA, Request, Budget(), token);
    }
}
