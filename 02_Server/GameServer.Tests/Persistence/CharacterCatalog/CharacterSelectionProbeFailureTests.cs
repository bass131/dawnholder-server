using System.Reflection;
using System.Runtime.CompilerServices;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Literal API outcomes through the facade; repository barriers control failure and publication order.</summary>
public sealed class CharacterSelectionProbeFailureTests
{
    [Theory]
    [InlineData("read failed")]
    [InlineData("not dispatched")]
    public async Task TypedReadFailure_IsUnavailableWithoutAcquiringOrPublishing(string failure)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        probe.Complete(fixture.Response(failure));

        await fixture.AssertFailure(handle, CatalogStatus.Unavailable);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task IncompatibleSchema_PrecedesBindingAndIdentityChecks(bool bindingMatches, bool identityMatches)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        PersistenceIdentity observed = identityMatches ? fixture.Identity :
            new(1, PersistenceTestValues.AccountId, OfflineCatalog.CharacterA);
        probe.Complete(fixture.Probe(schemaCompatible: false, bindingMatches: bindingMatches, identity: observed));

        await fixture.AssertFailure(handle, CatalogStatus.SchemaMismatch);
    }

    [Theory]
    [InlineData("binding")]
    [InlineData("slot")]
    [InlineData("account")]
    [InlineData("character")]
    public async Task CompatibleSchemaWithInvalidBindingOrIdentity_IsIntegrityFailure(string corruption)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        PersistenceIdentity observed = corruption switch
        {
            "binding" => fixture.Identity,
            "slot" => new(2, OfflineCatalog.Alice01, OfflineCatalog.CharacterA),
            "account" => new(1, PersistenceTestValues.AccountId, OfflineCatalog.CharacterA),
            "character" => new(1, OfflineCatalog.Alice01, PersistenceTestValues.CharacterId),
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        probe.Complete(fixture.Probe(bindingMatches: corruption != "binding", identity: observed));

        await fixture.AssertFailure(handle, CatalogStatus.IntegrityFailure);
    }

    [Theory]
    [InlineData("null result")]
    [InlineData("missing admission")]
    [InlineData("missing authority")]
    [InlineData("unknown status")]
    public async Task MalformedProbePayload_IsIntegrityFailureBeforeSchemaClassification(string corruption)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        // Constructors normally exclude these shapes. Inject corruption only at the repository boundary.
        ReadAdmissionResult malformed = corruption switch
        {
            "null result" => null!,
            "missing admission" => (ReadAdmissionResult)Activator.CreateInstance(typeof(ReadAdmissionResult),
                BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                args: [ReadAdmissionStatus.Probe, null], culture: null)!,
            "missing authority" => ReadAdmissionResult.Probe(
                (AdmissionProbe)RuntimeHelpers.GetUninitializedObject(typeof(AdmissionProbe))),
            "unknown status" => (ReadAdmissionResult)RuntimeHelpers.GetUninitializedObject(typeof(ReadAdmissionResult)),
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        probe.Complete(malformed);

        await fixture.AssertFailure(handle, CatalogStatus.IntegrityFailure);
    }

    [Theory]
    [InlineData("read failed")]
    [InlineData("not dispatched")]
    [InlineData("schema mismatch")]
    public async Task CloseBeforeFailedProbePublication_PreservesOutcomeUnknown(string failure)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        Task closing = handle.Close(null);
        Assert.Same(closing, handle.Close(null));
        probe.Complete(fixture.Response(failure));

        await fixture.AssertFailure(handle, CatalogStatus.OutcomeUnknown);
        await closing.WaitAsync(StoreCallRecorder.Limit);
    }

    [Theory]
    [InlineData(false, "read failed")]
    [InlineData(false, "not dispatched")]
    [InlineData(false, "schema mismatch")]
    [InlineData(false, "valid probe")]
    [InlineData(true, "read failed")]
    [InlineData(true, "not dispatched")]
    [InlineData(true, "schema mismatch")]
    [InlineData(true, "valid probe")]
    public async Task CancellationOrDeadline_CompletesBeforeLateProbeWithoutAcquiring(bool expireDeadline, string lateResponse)
    {
        Fixture fixture = new();
        using CancellationTokenSource cancellation = new();
        CharacterSelectionHandle handle = fixture.Begin(cancellation.Token);
        var probe = await fixture.ReadProbe();
        Assert.False(probe.ResultTask.IsCompleted);
        if (expireDeadline)
            fixture.Clock.Advance(TimeSpan.FromSeconds(5));
        else
            cancellation.Cancel();

        // Completion and Close must finish while the repository still withholds its answer.
        CatalogResult published = await fixture.AssertFailure(handle, CatalogStatus.OutcomeUnknown);
        Assert.False(probe.ResultTask.IsCompleted);
        probe.Complete(fixture.Response(lateResponse));
        await probe.ResultTask.WaitAsync(StoreCallRecorder.Limit);
        Assert.Same(published, await handle.Completion);
        fixture.AssertReadOnlyCalls();
    }

    [Fact]
    public async Task CloseBeforeValidProbe_ConfirmedNonDispatchRemainsCancelledBeforeDispatch()
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        Task closing = handle.Close(null);
        probe.Complete(fixture.Probe());

        await fixture.AssertFailure(handle, CatalogStatus.CancelledBeforeDispatch);
        await closing.WaitAsync(StoreCallRecorder.Limit);
    }

    [Theory]
    [InlineData(PersistenceOwnerKind.Runtime, CatalogStatus.Busy)]
    [InlineData(PersistenceOwnerKind.Recovery, CatalogStatus.RecoveryRequired)]
    public async Task CompatibleHeldProbe_PreservesOwnerRefusal(PersistenceOwnerKind ownerKind, CatalogStatus expected)
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        probe.Complete(ReadAdmissionResult.Probe(new(
            new(fixture.Identity, ownerKind, PersistenceTestValues.OtherOwnerId, 7, 0), true, true)));

        await fixture.AssertFailure(handle, expected);
    }

    [Fact]
    public async Task CompatibleFreeProbe_AcquiresAndPublishesTheOriginalCharacterAndLease()
    {
        Fixture fixture = new();
        CharacterSelectionHandle handle = fixture.Begin();
        var probe = await fixture.ReadProbe();
        probe.Complete(fixture.Probe());
        var acquire = await fixture.Characters.NextAcquireAsync();
        Assert.Equal(OfflineCatalog.OperationA, acquire.Request.Operation.OperationId);
        PersistenceAuthority held = new(fixture.Identity, PersistenceOwnerKind.Runtime,
            acquire.Request.Operation.OwnerId, 8, 0);
        PersistenceToken character = PersistenceTestValues.Token(1);
        PersistenceToken progress = PersistenceTestValues.Token(2);
        OperationReceipt receipt = PersistenceTestValues.AcquiredReceipt(OfflineCatalog.OperationA, held, character, progress);
        acquire.Complete(AcquireAndLoadResult.Acquired(receipt, new(held, character, progress)));

        CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(CatalogStatus.Selected, result.Status);
        Assert.Equal(fixture.Request, result.Request);
        Assert.Equal(fixture.Summary, result.Character);
        Assert.NotNull(result.Lease);
        Assert.Empty(result.Characters);
        Task closing = handle.Close(null);
        var release = await fixture.Characters.NextReleaseAsync();
        Assert.Equal(held, release.Request.Authority);
        PersistenceAuthority free = new(fixture.Identity, PersistenceOwnerKind.Free, null, 9, 0);
        release.Complete(ReleaseResult.Released(
            PersistenceTestValues.ReleasedReceipt(release.Request.Operation.OperationId, free), new(free, null, null)));
        await closing.WaitAsync(StoreCallRecorder.Limit);
        await fixture.Service.FlushAsync(fixture.Budget()).WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Characters.Calls);
        Assert.Single(fixture.Repository.Calls);
    }

    sealed class Fixture
    {
        public Fixture()
        {
            Deadline = Budget();
            Service = new(Repository, Characters, PersistenceTestValues.Defaults(), Clock);
        }

        public RecordingCatalogRepository Repository { get; } = new();
        public Repo Characters { get; } = new();
        public ControllableTimeProvider Clock { get; } = new();
        public CharacterCatalogService Service { get; }
        public PersistenceDeadline Deadline { get; }
        public PersistenceIdentity Identity { get; } = new(1, OfflineCatalog.Alice01, OfflineCatalog.CharacterA);
        public RequestContext Request { get; } = new(OfflineCatalog.SessionA, 17);
        public CharacterSummary Summary { get; } = new(OfflineCatalog.CharacterA, 1, "Dawn01", CharacterClass.Knight);

        public PersistenceDeadline Budget() => PersistenceDeadline.FromBudget(Clock, TimeSpan.FromSeconds(5));

        public CharacterSelectionHandle Begin(CancellationToken cancellation = default) => Service.BeginSelectCharacter(
            (AccountContext)OfflineCatalog.AliceAccount(), OfflineCatalog.CharacterA, OfflineCatalog.OperationA,
            Request, Deadline, cancellation);

        public async Task<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>> ReadProbe()
        {
            RecordingCatalogRepository.Call selection = await Repository.Next("Select");
            selection.Complete(new(CatalogStatus.Selected, Summary));
            var probe = Assert.IsType<Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult>>(
                await Characters.NextAnyAsync());
            Assert.Equal(Identity, probe.Request.Identity);
            Assert.Same(Deadline, probe.Deadline);
            return probe;
        }

        public ReadAdmissionResult Probe(bool schemaCompatible = true, bool bindingMatches = true,
            PersistenceIdentity? identity = null) => ReadAdmissionResult.Probe(new(
                new(identity ?? Identity, PersistenceOwnerKind.Free, null, 7, 0), schemaCompatible, bindingMatches));

        public ReadAdmissionResult Response(string response) => response switch
        {
            "read failed" => ReadAdmissionResult.ReadFailed(),
            "not dispatched" => ReadAdmissionResult.NotDispatched(),
            "schema mismatch" => Probe(schemaCompatible: false),
            "valid probe" => Probe(),
            _ => throw new ArgumentOutOfRangeException(nameof(response)),
        };

        public async Task<CatalogResult> AssertFailure(CharacterSelectionHandle handle, CatalogStatus expected)
        {
            CatalogResult result = await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
            Assert.Equal(expected, result.Status);
            Assert.Equal(Request, result.Request);
            Assert.Null(result.Character);
            Assert.Null(result.Lease);
            Assert.Empty(result.Characters);
            await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);
            await Service.FlushAsync(Budget()).WaitAsync(StoreCallRecorder.Limit);
            AssertReadOnlyCalls();
            return result;
        }

        public void AssertReadOnlyCalls()
        {
            Assert.Single(Repository.Calls);
            Assert.Equal(new[] { Repo.Admission }, Characters.Calls);
        }
    }
}
