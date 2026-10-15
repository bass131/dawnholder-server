using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// One absolute close deadline across every close step and resolver call, no store call after it, and
/// unfinished calls that stay tracked with live cancellation resources until their real task ends.
/// Source: api-contract.md 「저장 작업 핵심」(close·deadline), api-surface-addendum-1.md 「B 소비 경계」, technical-spec §6·§7.
/// </summary>
public sealed class CharacterPersistenceOwnerDeadlineTests
{
    [Fact]
    public async Task CloseSteps_ShareTheCloseDeadline_AndIgnoreWallClockMoves()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        PersistenceDeadline closeDeadline = lane.Budget(10);

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(KnightSafeMoved()), closeDeadline);
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> write = await lane.Repository.NextCheckpointAsync();
        lane.Clock.MoveWallClock(TimeSpan.FromHours(2));
        lane.Clock.Advance(TimeSpan.FromSeconds(4));
        write.Complete(Durable(write.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        lane.Clock.Advance(TimeSpan.FromSeconds(1));
        TimeSpan releaseBudgetNow = release.Deadline.GetRemaining();
        TimeSpan closeBudgetNow = closeDeadline.GetRemaining();
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult result = await Within(close);

        Assert.Equal(TimeSpan.FromSeconds(10), write.RemainingAtCall);
        Assert.Equal(TimeSpan.FromSeconds(6), release.RemainingAtCall);
        Assert.Equal(closeBudgetNow, releaseBudgetNow);
        Assert.Equal(PersistenceCloseStatus.Released, result.Status);
    }

    [Fact]
    public async Task ResolverAfterCloseCapture_IsBoundedByTheCloseDeadline_AndNoSealFollowsItsExpiry()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(60));
        OperationIdentity original = call.Request.Operation;

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget(5));
        lane.Clock.Advance(TimeSpan.FromSeconds(1));
        call.Complete(WriteSafeCheckpointResult.CommitUnknown());
        Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> query = await lane.Repository.NextResolveAsync();
        lane.Clock.Advance(TimeSpan.FromSeconds(4));
        PersistenceCloseResult closed = await Within(close);
        query.Complete(ResolveOperationResult.StillUnknown());
        PersistenceWorkResult settled = await Within(work.Completion!);
        await Settle();

        Assert.Equal(original, query.Request.Operation);
        Assert.Equal(ResolveMode.QueryOnly, query.Request.Mode);
        Assert.InRange(query.RemainingAtCall, TimeSpan.FromTicks(1), TimeSpan.FromSeconds(4));
        Assert.Equal(PersistenceCloseStatus.ShutdownIncomplete, closed.Status);
        Assert.Contains(original.OperationId, closed.UnfinishedOperationIds);
        Assert.Equal(PersistenceWorkStatus.Unresolved, settled.Status);
        Assert.Contains(original.OperationId, lane.Owner.UnfinishedOperationIds);
        Assert.Equal(1, lane.Repository.Count(Repo.Resolve));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task LateAcquireAfterTheCloseDeadline_IsSettledWithoutNewCalls()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget(60));
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        Guid acquireId = call.Request.Operation.OperationId;

        Task<PersistenceCloseResult> close = await ReturnsPromptly(() => lane.Owner.CloseAsync(Ticket(), lane.Budget(5)));
        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        PersistenceCloseResult closed = await Within(close);
        bool trackedBeforeAnswer = lane.Owner.UnfinishedOperationIds.Contains(acquireId);
        call.Complete(FreshAcquired(acquireId, 8, Token(1), Token(2)));
        await Within(acquire.Completion!);
        await Settle();

        Assert.Equal(PersistenceCloseStatus.ShutdownIncomplete, closed.Status);
        Assert.True(trackedBeforeAnswer);
        Assert.DoesNotContain(acquireId, lane.Owner.UnfinishedOperationIds);
        // The close result stays the snapshot taken at the deadline after the owner settles the late answer.
        Assert.Contains(acquireId, closed.UnfinishedOperationIds);
        if (closed.UnfinishedOperationIds is ICollection<Guid> collection)
            Assert.True(collection.IsReadOnly);
        Assert.Equal(new[] { Repo.Acquire }, lane.Repository.Calls);
        Assert.Equal(PersistenceSubmitStatus.Closing, lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget()).Status);
    }

    [Fact]
    public async Task CloseDeadline_RequestsCancellation_ButKeepsTheCallTrackedAndItsTokenAlive()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission work, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(60));
        Guid stuckId = call.Request.Operation.OperationId;

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget(5));
        bool cancelledBeforeDeadline = call.CancellationToken.IsCancellationRequested;
        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        PersistenceCloseResult closed = await Within(close);
        bool cancelledAtDeadline = call.CancellationToken.IsCancellationRequested;
        Exception? disposedWhilePending = Record.Exception(() => call.CancellationToken.WaitHandle);
        bool trackedWhilePending = lane.Owner.UnfinishedOperationIds.Contains(stuckId);
        bool workDoneWhilePending = work.Completion!.IsCompleted;

        call.Cancel();
        PersistenceWorkResult settled = await Within(work.Completion!);
        await Settle();

        Assert.False(cancelledBeforeDeadline);
        Assert.Equal(PersistenceCloseStatus.ShutdownIncomplete, closed.Status);
        Assert.True(cancelledAtDeadline);
        Assert.Null(disposedWhilePending);
        Assert.True(trackedWhilePending);
        Assert.False(workDoneWhilePending);
        // A cancelled task after dispatch is still unknown, and the deadline has passed: no resolver, no release.
        Assert.Equal(PersistenceWorkStatus.Unresolved, settled.Status);
        Assert.Contains(stuckId, lane.Owner.UnfinishedOperationIds);
        Assert.Equal(0, lane.Repository.Count(Repo.Resolve));
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task CallDeadline_RequestsCancellationOfThePendingCall()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (_, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(5));

        bool before = call.CancellationToken.IsCancellationRequested;
        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        bool after = call.CancellationToken.IsCancellationRequested;
        call.Cancel();
        await Settle();

        Assert.False(before);
        Assert.True(after);
        Assert.Equal(0, lane.Repository.Count(Repo.Resolve));
    }

    // api-surface-addendum-1 adopts B's policy: waiting work is settled as NotDispatched and the final capture
    // keeps its own space.
    [Fact]
    public async Task CloseWithWaitingWork_SettlesItAsNotDispatched_AndSendsTheFinalCaptureOnce()
    {
        CharacterPersistenceOwnerHarness lane = new(queueCapacity: 2);
        await lane.AcquireAsync();
        (PersistenceSubmission running, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> runningCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        PersistenceSubmission waitingA = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());
        PersistenceSubmission waitingB = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        PersistenceSubmission overflow = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        SafeCheckpoint finalSafe = new(CharacterClass.Knight, -40f, 2f, 150);

        Task<PersistenceCloseResult> close = await ReturnsPromptly(() => lane.Owner.CloseAsync(Ticket(finalSafe), lane.Budget()));
        PersistenceWorkResult waitingAResult = await Within(waitingA.Completion!);
        PersistenceWorkResult waitingBResult = await Within(waitingB.Completion!);
        runningCall.Complete(Durable(runningCall.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> finalCall = await lane.Repository.NextCheckpointAsync();
        finalCall.Complete(Durable(finalCall.Request.Operation.OperationId, 2, Token(5), Token(6)));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult closed = await Within(close);
        await Within(running.Completion!);
        await Settle();

        Assert.Equal(PersistenceSubmitStatus.Accepted, waitingA.Status);
        Assert.Equal(PersistenceSubmitStatus.Accepted, waitingB.Status);
        Assert.Equal(PersistenceSubmitStatus.QueueFull, overflow.Status);
        Assert.Equal(PersistenceWorkStatus.NotDispatched, waitingAResult.Status);
        Assert.Equal(PersistenceWorkStatus.NotDispatched, waitingBResult.Status);
        Assert.Equal(finalSafe, finalCall.Request.Safe);
        Assert.Equal(2, finalCall.Request.Operation.Sequence);
        Assert.Equal(Token(3), finalCall.Request.ExpectedCharacterToken);
        Assert.Equal(Token(4), finalCall.Request.ExpectedProgressToken);
        Assert.Equal(Runtime(8, 2), release.Request.Authority);
        Assert.Equal(3, release.Request.Operation.Sequence);
        Assert.Equal(PersistenceCloseStatus.Released, closed.Status);
        Assert.Equal(new[] { Repo.Acquire, Repo.Checkpoint, Repo.Checkpoint, Repo.Release }, lane.Repository.Calls);
        Assert.Equal(1, lane.Repository.MaxInFlight);
    }

    [Fact]
    public async Task CloseBeforeAnyAcquire_SendsNothing_AndRefusesLaterWork()
    {
        CharacterPersistenceOwnerHarness lane = new();

        PersistenceCloseResult closed = await Within(lane.Owner.CloseAsync(Ticket(KnightSafe()), lane.Budget()));
        PersistenceSubmission late = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        await Settle();

        Assert.NotEqual(PersistenceCloseStatus.Released, closed.Status);
        Assert.NotEqual(PersistenceCloseStatus.ShutdownIncomplete, closed.Status);
        Assert.Empty(closed.UnfinishedOperationIds);
        Assert.Equal(PersistenceSubmitStatus.Closing, late.Status);
        Assert.Empty(lane.Repository.Calls);
    }
}
