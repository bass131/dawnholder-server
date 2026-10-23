using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// How the owner settles store answers: contradictory or non-current results never advance the lane,
/// a resolver answer is the only way out of an unknown, and refused input never reaches the store.
/// Source: api-contract.md 「저장소·복구 API」 34행·「저장 작업 핵심」, api-surface v2 owner bullets, technical-spec §5.2~§5.4·§6·§7.
/// </summary>
public sealed class CharacterPersistenceOwnerSettlementTests
{
    [Theory]
    [InlineData(PersistenceResultCode.Busy)]
    [InlineData(PersistenceResultCode.StaleFence)]
    [InlineData(PersistenceResultCode.Conflict)]
    [InlineData(PersistenceResultCode.IdentityMismatch)]
    [InlineData(PersistenceResultCode.InvalidClass)]
    [InlineData(PersistenceResultCode.CancelledBeforeApply)]
    [InlineData(PersistenceResultCode.SequenceMismatch)]
    [InlineData(PersistenceResultCode.IntegrityFailure)]
    public async Task AcquireRejected_PreservesItsCode_WithoutAssigningItToQueuedWork(PersistenceResultCode code)
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget(), OperationA);
        var call = await lane.Repository.NextAcquireAsync();
        PersistenceSubmission queued = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        call.Complete(AcquireAndLoadResult.Rejected(code));

        PersistenceWorkResult result = await Within(acquire.Completion!);
        Assert.Equal(PersistenceWorkStatus.Rejected, result.Status);
        Assert.Equal(code, result.Code);
        Assert.Null(result.LoadedClass);
        PersistenceWorkResult cancelled = await Within(queued.Completion!);
        Assert.Equal(PersistenceWorkStatus.NotDispatched, cancelled.Status);
        Assert.Null(cancelled.Code);
        Assert.Equal(OperationA, call.Request.Operation.OperationId);
        Assert.Empty(lane.Owner.UnfinishedOperationIds);
        Assert.Equal(PersistenceCloseStatus.NotHeld, (await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()))).Status);
        Assert.Equal(new[] { Repo.Acquire }, lane.Repository.Calls);
    }

    [Theory]
    [InlineData(PersistenceResultCode.Busy)]
    [InlineData(PersistenceResultCode.StaleFence)]
    [InlineData(PersistenceResultCode.Conflict)]
    [InlineData(PersistenceResultCode.IdentityMismatch)]
    [InlineData(PersistenceResultCode.InvalidClass)]
    [InlineData(PersistenceResultCode.CancelledBeforeApply)]
    [InlineData(PersistenceResultCode.SequenceMismatch)]
    [InlineData(PersistenceResultCode.IntegrityFailure)]
    public async Task AcquireResolvedNotApplied_PreservesItsCodeAndConfirmedUnchangedAuthority(PersistenceResultCode code)
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget(), OperationA);
        var call = await lane.Repository.NextAcquireAsync();
        call.Complete(AcquireAndLoadResult.AcquireUnknown());
        var query = await lane.Repository.NextResolveAsync();
        Assert.Equal(call.Request.Operation, query.Request.Operation);
        Assert.Equal(ResolveMode.QueryOnly, query.Request.Mode);
        query.Complete(ResolveOperationResult.NotApplied(code, Observed(Free(7), null, null)));

        PersistenceWorkResult result = await Within(acquire.Completion!);
        Assert.Equal(PersistenceWorkStatus.NotApplied, result.Status);
        Assert.Equal(code, result.Code);
        Assert.Null(result.LoadedClass);
        Assert.Empty(lane.Owner.UnfinishedOperationIds);
        Assert.Equal(PersistenceCloseStatus.NotHeld, (await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()))).Status);
        Assert.Equal(new[] { Repo.Acquire, Repo.Resolve }, lane.Repository.Calls);
    }

    [Fact]
    public async Task AcquireNotAppliedWithChangedFence_PreservesFenceLostDespiteTheRejectionCode()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        var call = await lane.Repository.NextAcquireAsync();
        call.Complete(AcquireAndLoadResult.AcquireUnknown());
        var query = await lane.Repository.NextResolveAsync();
        query.Complete(ResolveOperationResult.NotApplied(PersistenceResultCode.StaleFence, Observed(Free(9), null, null)));

        PersistenceWorkResult result = await Within(acquire.Completion!);
        Assert.Equal(PersistenceWorkStatus.FenceLost, result.Status);
        Assert.Equal(PersistenceResultCode.StaleFence, result.Code);
        Assert.Null(result.LoadedClass);
        Assert.Equal(PersistenceCloseStatus.FenceLost, (await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()))).Status);
        Assert.Equal(new[] { Repo.Acquire, Repo.Resolve }, lane.Repository.Calls);
    }

    [Fact]
    public async Task AcquireNotAppliedForAnotherIdentity_RemainsUnresolvedWithoutPublishingThatCode()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        var call = await lane.Repository.NextAcquireAsync();
        call.Complete(AcquireAndLoadResult.AcquireUnknown());
        var query = await lane.Repository.NextResolveAsync();
        PersistenceAuthority foreign = new(new(1, Guid.NewGuid(), Guid.NewGuid()), PersistenceOwnerKind.Free, null, 7, 0);
        query.Complete(ResolveOperationResult.NotApplied(PersistenceResultCode.IdentityMismatch, new(foreign, null, null)));
        var seal = await lane.Repository.NextResolveAsync();
        Assert.Equal(call.Request.Operation, seal.Request.Operation);
        Assert.Equal(ResolveMode.SealIfAbsent, seal.Request.Mode);
        seal.Complete(ResolveOperationResult.StillUnknown());

        PersistenceWorkResult result = await Within(acquire.Completion!);
        Assert.Equal(PersistenceWorkStatus.Unresolved, result.Status);
        Assert.Null(result.Code);
        Assert.Null(result.LoadedClass);
        Assert.Contains(call.Request.Operation.OperationId, lane.Owner.UnfinishedOperationIds);
        Assert.Equal(PersistenceCloseStatus.Unresolved, (await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()))).Status);
        Assert.Equal(new[] { Repo.Acquire, Repo.Resolve, Repo.Resolve }, lane.Repository.Calls);
    }

    [Theory]
    [InlineData("otherOperation")]
    [InlineData("otherSequence")]
    [InlineData("otherFence")]
    public async Task ContradictoryDurableAnswer_DoesNotAdvanceTheLane_AndIsResolvedWithTheSameOperation(string defect)
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;
        PersistenceAuthority claimed = defect switch
        {
            "otherSequence" => Runtime(8, 5),
            "otherFence" => Runtime(9, 1),
            _ => Runtime(8, 1),
        };
        Guid claimedId = defect == "otherOperation" ? OperationB : original.OperationId;

        call.Complete(WriteSafeCheckpointResult.Durable(
            CheckpointReceipt(claimedId, PersistenceResultCode.CheckpointApplied, claimed, Token(3), Token(4)),
            Observed(claimed, Token(3), Token(4))));
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        query.Complete(ResolveOperationResult.NotApplied(PersistenceResultCode.CancelledBeforeApply, Observed(Runtime(8, 0), Token(1), Token(2))));

        Assert.Equal(original, query.Request.Operation);
        Assert.Equal(ResolveMode.QueryOnly, query.Request.Mode);
        Assert.Equal(PersistenceWorkStatus.NotApplied, (await Within(work.Completion!)).Status);

        // If the owner ends the lifetime with a release, it must still use the last confirmed authority.
        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Task arrival = lane.Repository.WhenUntakenCallAsync();
        await Task.WhenAny(close, arrival).WaitAsync(Repo.Limit);
        if (arrival.IsCompleted)
        {
            Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
            Assert.Equal(Runtime(8, 0), release.Request.Authority);
            Assert.Equal(1, release.Request.Operation.Sequence);
            release.Complete(Released(release.Request.Operation.OperationId));
        }
        await Within(close);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
    }

    // api-surface v2: an Acquired whose IsCurrent is false ends as FenceLost without mutation or release.
    // Only the progress token differs here; the authority itself still matches.
    [Fact]
    public async Task AcquiredWhoseCurrentTokenDiffers_OpensNoMutationOrRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        PersistenceSubmission queued = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        PersistenceAuthority held = Runtime(8, 0);

        call.Complete(AcquireAndLoadResult.Acquired(
            AcquiredReceipt(call.Request.Operation.OperationId, held, Token(1), Token(2)),
            Observed(held, Token(1), Token(9))));

        Assert.Equal(PersistenceWorkStatus.FenceLost, (await Within(acquire.Completion!)).Status);
        Assert.NotEqual(PersistenceWorkStatus.Durable, (await Within(queued.Completion!)).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.FenceLost, closed.Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task ResolverAppliedButNotCurrent_EndsAsFenceLost_WithoutRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;

        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        query.Complete(ResolveOperationResult.Applied(
            CheckpointReceipt(original.OperationId, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4)),
            Observed(Runtime(10, 0, OtherOwnerId), Token(3), Token(4))));

        Assert.Equal(PersistenceWorkStatus.FenceLost, (await Within(work.Completion!)).Status);
        Assert.Equal(PersistenceSubmitStatus.Frozen, lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget()).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.FenceLost, closed.Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(1, lane.Repository.Count(Repo.Resolve));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task ReleaseUnknownResolvedAsCurrentRelease_ClosesAsReleased_WithoutASecondRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        OperationIdentity original = release.Request.Operation;
        release.Complete(ReleaseResult.ReleaseUnknown());
        IReadOnlyList<ResolveMode> modes = await lane.AnswerResolverUntilAsync(
            original,
            _ => ResolveOperationResult.Applied(ReleasedReceipt(original.OperationId, Free(9)), Observed(Free(9), Token(1), Token(2))),
            close);
        PersistenceCloseResult closed = await Within(close);

        AssertQueryThenOptionalSeal(modes);
        Assert.Equal(PersistenceCloseStatus.Released, closed.Status);
        Assert.Empty(closed.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Release));
    }

    // technical-spec §5.3: a Released replay confirms the old release only; it never acts for the new owner.
    [Fact]
    public async Task ReleaseUnknownResolvedWhileAnotherOwnerHolds_IsNotReportedAsReleased()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        OperationIdentity original = release.Request.Operation;
        release.Complete(ReleaseResult.ReleaseUnknown());
        IReadOnlyList<ResolveMode> modes = await lane.AnswerResolverUntilAsync(
            original,
            _ => ResolveOperationResult.Applied(ReleasedReceipt(original.OperationId, Free(9)), Observed(Runtime(10, 0, OtherOwnerId), Token(1), Token(2))),
            close);
        PersistenceCloseResult closed = await Within(close);

        AssertQueryThenOptionalSeal(modes);
        Assert.Contains(closed.Status, new[] { PersistenceCloseStatus.FenceLost, PersistenceCloseStatus.RecoveryRequired });
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task ConfirmedConflict_IsNotRetriedWithFreshTokensOrANewOperation()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());

        call.Complete(WriteSafeCheckpointResult.Conflict());

        Assert.Equal(PersistenceWorkStatus.Conflict, (await Within(work.Completion!)).Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task AcquireConfirmedNotApplied_NeverHeld_ClosesWithoutRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        OperationIdentity original = call.Request.Operation;

        call.Complete(AcquireAndLoadResult.AcquireUnknown());
        IReadOnlyList<ResolveMode> modes = await lane.AnswerResolverUntilAsync(
            original,
            _ => ResolveOperationResult.NotApplied(PersistenceResultCode.CancelledBeforeApply, Observed(Free(7), null, null)),
            acquire.Completion!);

        AssertQueryThenOptionalSeal(modes);
        Assert.Equal(PersistenceWorkStatus.NotApplied, (await Within(acquire.Completion!)).Status);
        Assert.DoesNotContain(original.OperationId, lane.Owner.UnfinishedOperationIds);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.NotEqual(PersistenceCloseStatus.Released, closed.Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Equal(1, lane.Repository.Count(Repo.Acquire));
    }

    [Fact]
    public async Task InvalidAcquireInput_IsRefusedWithoutRepositoryCalls()
    {
        CharacterPersistenceOwnerHarness lane = new();

        PersistenceSubmission negativeFence = lane.Owner.TrySubmitAcquire(-1, CharacterClass.Knight, Defaults(), lane.Budget());
        PersistenceSubmission undefinedClass = lane.Owner.TrySubmitAcquire(7, (CharacterClass)9, Defaults(), lane.Budget());

        Assert.Equal(PersistenceSubmitStatus.Invalid, negativeFence.Status);
        Assert.Null(negativeFence.Completion);
        Assert.Equal(PersistenceSubmitStatus.Invalid, undefinedClass.Status);
        Assert.Null(undefinedClass.Completion);
        await Settle();
        Assert.Empty(lane.Repository.Calls);
    }

    // technical-spec §5.2: the checkpoint never changes the stored class.
    [Fact]
    public async Task CheckpointOfAnotherClass_IsRefusedWithoutRepositoryCall()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        PersistenceSubmission mage = lane.Owner.TrySubmitCheckpoint(new SafeCheckpoint(CharacterClass.Mage, 3.5f, 0f, 80), lane.Budget());

        Assert.Equal(PersistenceSubmitStatus.Invalid, mage.Status);
        Assert.Null(mage.Completion);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
    }

    // technical-spec §6: NotDispatched means the call never reached the store; Rejected is a store answer.
    // The stored class decides (technical-spec §5.1), so a payload accepted before it was known cannot be sent.
    [Fact]
    public async Task CheckpointAcceptedBeforeTheStoredClassWasKnown_IsSettledAsNotDispatched()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Mage, Defaults(), lane.Budget());
        PersistenceSubmission early = lane.Owner.TrySubmitCheckpoint(new SafeCheckpoint(CharacterClass.Mage, 3.5f, 0f, 80), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();

        // The store keeps its Knight class even though Mage was intended.
        call.Complete(FreshAcquired(call.Request.Operation.OperationId, 8, Token(1), Token(2)));
        PersistenceWorkResult acquired = await Within(acquire.Completion!);
        PersistenceWorkResult result = await Within(early.Completion!);
        await Settle();

        Assert.Equal(PersistenceSubmitStatus.Accepted, early.Status);
        Assert.Equal(PersistenceWorkStatus.Acquired, acquired.Status);
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(PersistenceWorkStatus.NotDispatched, result.Status);
    }
}
