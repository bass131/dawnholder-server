using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Close hand-off: NoWriteNeeded with checkpoint 0 and one conditional release, idempotent close of one
/// lifetime, late create success, one shared absolute deadline and incomplete settlement that keeps tracking.
/// Source: api-contract.md 「저장 작업 핵심」 and technical-spec §5.3·§6·§7.
/// </summary>
public sealed class CharacterPersistenceOwnerCloseTests
{
    [Fact]
    public void Ticket_DefaultsToNoWriteNeeded_AndRejectsMissingLifetime()
    {
        CloseCaptureTicket noWrite = new(Identity(), RuntimeOwnerId, null);
        CloseCaptureTicket write = new(Identity(), RuntimeOwnerId, KnightSafe());

        Assert.Equal(CloseWriteDecision.NoWriteNeeded, noWrite.WriteDecision);
        Assert.Null(noWrite.Checkpoint);
        Assert.Equal(CloseWriteDecision.CheckpointRequired, write.WriteDecision);
        Assert.Equal(KnightSafe(), write.Checkpoint);
        Assert.Equal(Identity(), write.Identity);
        Assert.Equal(RuntimeOwnerId, write.RuntimeOwnerId);
        Assert.Throws<ArgumentNullException>(() => new CloseCaptureTicket(null!, RuntimeOwnerId, null));
        Assert.ThrowsAny<ArgumentException>(() => new CloseCaptureTicket(Identity(), Guid.Empty, null));
    }

    [Fact]
    public async Task NoWriteNeededClose_SendsNoCheckpoint_AndReleasesOnce()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult result = await Within(close);

        Assert.Equal(PersistenceOperationKind.ReleaseRuntime, release.Request.Operation.Kind);
        Assert.Equal(Runtime(8, 0), release.Request.Authority);
        Assert.Equal(1, release.Request.Operation.Sequence);
        Assert.Equal(RuntimeOwnerId, release.Request.Operation.OwnerId);
        Assert.Equal(PersistenceCloseStatus.Released, result.Status);
        Assert.Equal(CloseWriteDecision.NoWriteNeeded, result.WriteDecision);
        Assert.Empty(result.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(1, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task CheckpointRequiredClose_WritesOnce_ThenReleasesWithTheConfirmedSequence()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(KnightSafeMoved()), lane.Budget());
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> write = await lane.Repository.NextCheckpointAsync();
        Assert.Equal(KnightSafeMoved(), write.Request.Safe);
        Assert.Equal(1, write.Request.Operation.Sequence);
        write.Complete(Durable(write.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult result = await Within(close);

        Assert.Equal(Runtime(8, 1), release.Request.Authority);
        Assert.Equal(2, release.Request.Operation.Sequence);
        Assert.Equal(PersistenceCloseStatus.Released, result.Status);
        Assert.Equal(CloseWriteDecision.CheckpointRequired, result.WriteDecision);
        Assert.Equal(new[] { Repo.Acquire, Repo.Checkpoint, Repo.Release }, lane.Repository.Calls);
    }

    [Fact]
    public async Task RepeatedCloseOfTheSameLifetime_IsIdempotent()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> first = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        Task<PersistenceCloseResult> second = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult firstResult = await Within(first);
        PersistenceCloseResult secondResult = await Within(second);
        PersistenceCloseResult thirdResult = await Within(lane.Owner.CloseAsync(Ticket(), lane.Budget()));

        Assert.Equal(PersistenceCloseStatus.Released, firstResult.Status);
        Assert.Equal(PersistenceCloseStatus.Released, secondResult.Status);
        Assert.Equal(PersistenceCloseStatus.Released, thirdResult.Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Release));
    }

    [Fact]
    public async Task CloseTicketOfAnotherLifetime_IsRejected_AndTheRealCloseStillWorks()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        PersistenceIdentity otherCharacter = new(1, AccountId, OtherOwnerId);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => lane.Owner.CloseAsync(new CloseCaptureTicket(Identity(), OtherOwnerId, null), lane.Budget()));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => lane.Owner.CloseAsync(new CloseCaptureTicket(otherCharacter, RuntimeOwnerId, null), lane.Budget()));
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Release));

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        Assert.Equal(PersistenceCloseStatus.Released, (await Within(close)).Status);
    }

    [Fact]
    public async Task CloseDuringInFlightAcquire_ReleasesTheLateCreatedAuthority()
    {
        CharacterPersistenceOwnerHarness lane = new();
        lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire = await lane.Repository.NextAcquireAsync();

        Task<PersistenceCloseResult> close = await ReturnsPromptly(() => lane.Owner.CloseAsync(Ticket(), lane.Budget()));
        Assert.False(close.IsCompleted);
        acquire.Complete(FreshAcquired(acquire.Request.Operation.OperationId, 8, Token(1), Token(2)));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        PersistenceCloseResult result = await Within(close);

        Assert.Equal(Runtime(8, 0), release.Request.Authority);
        Assert.Equal(1, release.Request.Operation.Sequence);
        Assert.Equal(PersistenceCloseStatus.Released, result.Status);
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task Close_SharesOneAbsoluteDeadlineAcrossItsSteps()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(KnightSafeMoved()), lane.Budget(10));
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> write = await lane.Repository.NextCheckpointAsync();
        lane.Clock.Advance(TimeSpan.FromSeconds(4));
        write.Complete(Durable(write.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        release.Complete(Released(release.Request.Operation.OperationId));
        await Within(close);

        Assert.InRange(write.RemainingAtCall, TimeSpan.FromTicks(1), TimeSpan.FromSeconds(10));
        Assert.InRange(release.RemainingAtCall, TimeSpan.FromTicks(1), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task StuckCall_EndsCloseAtTheDeadlineAsShutdownIncomplete_AndKeepsTracking()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission stuck, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> stuckCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(60));
        Guid stuckId = stuckCall.Request.Operation.OperationId;

        Task<PersistenceCloseResult> close = await ReturnsPromptly(() => lane.Owner.CloseAsync(Ticket(), lane.Budget(5)));
        Assert.False(close.IsCompleted);
        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        PersistenceCloseResult result = await Within(close);

        Assert.Equal(PersistenceCloseStatus.ShutdownIncomplete, result.Status);
        Assert.Contains(stuckId, result.UnfinishedOperationIds);
        Assert.Contains(stuckId, lane.Owner.UnfinishedOperationIds);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Release));

        // A late durable answer after the close deadline is settled without new store calls.
        stuckCall.Complete(Durable(stuckId, 1, Token(3), Token(4)));
        await Within(stuck.Completion!);
        await Settle();
        Assert.Equal(new[] { Repo.Acquire, Repo.Checkpoint }, lane.Repository.Calls);
    }

    [Fact]
    public async Task CancelledCallAfterCloseDeadline_IsNotTakenAsRollbackOrRelease()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (_, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> stuckCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget(60));

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget(5));
        lane.Clock.Advance(TimeSpan.FromSeconds(5));
        PersistenceCloseResult result = await Within(close);
        stuckCall.Cancel();
        await Settle();

        Assert.NotEqual(PersistenceCloseStatus.Released, result.Status);
        Assert.Contains(stuckCall.Request.Operation.OperationId, result.UnfinishedOperationIds);
        Assert.Equal(0, lane.Repository.Count(Repo.Release));
        Assert.Equal(0, lane.Repository.Count(Repo.Resolve));
    }
}
