using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Unknown outcomes freeze the lane and are settled only through the same operation (QueryOnly, then at
/// most one SealIfAbsent); historical results whose current authority moved end as FenceLost.
/// Source: api-contract.md 「저장 작업 핵심」 and technical-spec §5.4·§6·§7.
/// </summary>
public sealed class CharacterPersistenceOwnerUnknownTests
{
    [Fact]
    public async Task CommitUnknown_FreezesTheLane_AndResolvesOnlyTheSameOperation()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;

        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        PersistenceSubmission whileFrozen = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());
        List<ResolveMode> modes = new() { query.Request.Mode };
        Assert.Equal(original, query.Request.Operation);
        query.Complete(ResolveOperationResult.StillUnknown());
        modes.AddRange(await lane.AnswerResolverUntilAsync(original, _ => ResolveOperationResult.StillUnknown(), work.Completion!));

        AssertQueryThenOptionalSeal(modes);
        Assert.Equal(PersistenceSubmitStatus.Frozen, whileFrozen.Status);
        Assert.Null(whileFrozen.Completion);
        Assert.Equal(PersistenceWorkStatus.Unresolved, (await Within(work.Completion!)).Status);
        Assert.Contains(original.OperationId, lane.Owner.UnfinishedOperationIds);

        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.Unresolved, closed.Status);
        Assert.Contains(original.OperationId, closed.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(modes.Count, lane.Repository.Count(Repo.Resolve));
    }

    [Fact]
    public async Task ResolverAppliedAndCurrent_ResumesFromTheConfirmedSequence()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;

        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        PersistenceAuthority after = Runtime(8, 1);
        query.Complete(ResolveOperationResult.Applied(
            CheckpointReceipt(original.OperationId, PersistenceResultCode.CheckpointApplied, after, Token(3), Token(4)),
            Observed(after, Token(3), Token(4))));

        Assert.Equal(ResolveMode.QueryOnly, query.Request.Mode);
        Assert.Equal(PersistenceWorkStatus.Durable, (await Within(work.Completion!)).Status);
        (_, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> next) = await lane.CheckpointInFlightAsync(KnightSafeMoved(), lane.Budget());
        Assert.Equal(2, next.Request.Operation.Sequence);
        Assert.Equal(Token(3), next.Request.ExpectedCharacterToken);
        Assert.NotEqual(original.OperationId, next.Request.Operation.OperationId);
        Assert.Equal(1, lane.Repository.Count(Repo.Resolve));
    }

    [Fact]
    public async Task ResolverFenceLost_EndsWithoutReleaseOrFurtherMutation()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;

        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        query.Complete(ResolveOperationResult.FenceLost(
            CheckpointReceipt(original.OperationId, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4)),
            Observed(Runtime(10, 0, OtherOwnerId), Token(3), Token(4))));

        Assert.Equal(PersistenceWorkStatus.FenceLost, (await Within(work.Completion!)).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.FenceLost, closed.Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task ResolverNotApplied_DoesNotRetryThePayloadUnderANewOperation()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());

        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        query.Complete(ResolveOperationResult.NotApplied(PersistenceResultCode.CancelledBeforeApply, Observed(Runtime(8, 0), Token(1), Token(2))));

        Assert.Equal(PersistenceWorkStatus.NotApplied, (await Within(work.Completion!)).Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(1, lane.Repository.Count(Repo.Resolve));
    }

    [Fact]
    public async Task AcquireUnknown_FreezesAndResolvesTheAcquireOperation()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        OperationIdentity original = call.Request.Operation;

        call.Complete(AcquireAndLoadResult.AcquireUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        PersistenceSubmission whileFrozen = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        List<ResolveMode> modes = new() { query.Request.Mode };
        Assert.Equal(original, query.Request.Operation);
        query.Complete(ResolveOperationResult.StillUnknown());
        modes.AddRange(await lane.AnswerResolverUntilAsync(original, _ => ResolveOperationResult.StillUnknown(), acquire.Completion!));

        AssertQueryThenOptionalSeal(modes);
        Assert.Equal(PersistenceSubmitStatus.Frozen, whileFrozen.Status);
        Assert.Equal(PersistenceWorkStatus.Unresolved, (await Within(acquire.Completion!)).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.Unresolved, closed.Status);
        Assert.Contains(original.OperationId, closed.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Acquire));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FaultOrCancellationAfterDispatch_IsResolvedNotAssumed(bool fault)
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        OperationIdentity original = call.Request.Operation;

        if (fault)
            call.Fail(new IOException("link lost after the command was sent"));
        else
            call.Cancel();
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        PersistenceSubmission whileFrozen = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());
        PersistenceAuthority after = Runtime(8, 1);
        query.Complete(ResolveOperationResult.Applied(
            CheckpointReceipt(original.OperationId, PersistenceResultCode.CheckpointApplied, after, Token(3), Token(4)),
            Observed(after, Token(3), Token(4))));

        Assert.Equal(original, query.Request.Operation);
        Assert.Equal(ResolveMode.QueryOnly, query.Request.Mode);
        Assert.Equal(PersistenceSubmitStatus.Frozen, whileFrozen.Status);
        Assert.Equal(PersistenceWorkStatus.Durable, (await Within(work.Completion!)).Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task UnknownAfterTheBudgetIsSpent_IsUnresolvedWithoutForcedCalls()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(5));

        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        call.Complete(WriteSafeCheckpointResult.CommitUnknown());

        Assert.Equal(PersistenceWorkStatus.Unresolved, (await Within(work.Completion!)).Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Resolve));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Contains(call.Request.Operation.OperationId, lane.Owner.UnfinishedOperationIds);
    }

    [Fact]
    public async Task AcquiredWhoseCurrentAuthorityMoved_DoesNotOpenTheRuntimeLifetime()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        PersistenceSubmission queued = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();

        call.Complete(AcquireAndLoadResult.Acquired(
            AcquiredReceipt(call.Request.Operation.OperationId, Runtime(8, 0), Token(1), Token(2)),
            Observed(Runtime(9, 0, OtherOwnerId), Token(1), Token(2))));

        Assert.Equal(PersistenceSubmitStatus.Accepted, queued.Status);
        Assert.Equal(PersistenceWorkStatus.FenceLost, (await Within(acquire.Completion!)).Status);
        Assert.NotEqual(PersistenceWorkStatus.Durable, (await Within(queued.Completion!)).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.FenceLost, closed.Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task DurableWhoseCurrentAuthorityMoved_StopsFurtherMutationAndRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        PersistenceSubmission queued = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());

        call.Complete(WriteSafeCheckpointResult.Durable(
            CheckpointReceipt(call.Request.Operation.OperationId, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4)),
            Observed(Runtime(10, 0, OtherOwnerId), Token(3), Token(4))));

        Assert.Equal(PersistenceSubmitStatus.Accepted, queued.Status);
        Assert.Equal(PersistenceWorkStatus.FenceLost, (await Within(work.Completion!)).Status);
        Assert.NotEqual(PersistenceWorkStatus.Durable, (await Within(queued.Completion!)).Status);
        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.Equal(PersistenceCloseStatus.FenceLost, closed.Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task ReleaseUnknown_IsResolvedWithTheSameReleaseOperation_NotRetried()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        OperationIdentity original = release.Request.Operation;
        release.Complete(ReleaseResult.ReleaseUnknown());
        IReadOnlyList<ResolveMode> modes = await lane.AnswerResolverUntilAsync(original, _ => ResolveOperationResult.StillUnknown(), close);
        PersistenceCloseResult closed = await Within(close);

        AssertQueryThenOptionalSeal(modes);
        Assert.Equal(PersistenceCloseStatus.Unresolved, closed.Status);
        Assert.Contains(original.OperationId, closed.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Release));
    }
}
