using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// One character lane: non-blocking acceptance, one repository call in progress, bounded waiting space,
/// a separate final close space and sequence/token advance only after confirmed results.
/// Source: api-contract.md 「저장 작업 핵심」 and technical-spec §6(D2 인계).
/// </summary>
public sealed class CharacterPersistenceOwnerLaneTests
{
    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        ControllableTimeProvider clock = new();
        Repo repository = new();

        Assert.Throws<ArgumentNullException>(() => new CharacterPersistenceOwner(null!, RuntimeOwnerId, repository, clock, 1));
        Assert.Throws<ArgumentNullException>(() => new CharacterPersistenceOwner(Identity(), RuntimeOwnerId, null!, clock, 1));
        Assert.Throws<ArgumentNullException>(() => new CharacterPersistenceOwner(Identity(), RuntimeOwnerId, repository, null!, 1));
        Assert.ThrowsAny<ArgumentException>(() => new CharacterPersistenceOwner(Identity(), Guid.Empty, repository, clock, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterPersistenceOwner(Identity(), RuntimeOwnerId, repository, clock, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterPersistenceOwner(Identity(), RuntimeOwnerId, repository, clock, -1));
    }

    [Fact]
    public void Owner_DependsOnTheRuntimeRepositoryOnly()
    {
        ConstructorInfo only = Assert.Single(typeof(CharacterPersistenceOwner).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        Assert.Equal(
            new[] { typeof(PersistenceIdentity), typeof(Guid), typeof(ICharacterRepository), typeof(TimeProvider), typeof(int) },
            only.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }

    [Fact]
    public async Task Acquire_IsBuiltOnceFromTheOwnerLifetime()
    {
        CharacterPersistenceOwnerHarness lane = new();

        PersistenceSubmission first = lane.Owner.TrySubmitAcquire(7, CharacterClass.Mage, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        PersistenceSubmission second = lane.Owner.TrySubmitAcquire(7, CharacterClass.Mage, Defaults(), lane.Budget());

        Assert.Equal(PersistenceSubmitStatus.Accepted, first.Status);
        Assert.NotNull(first.Completion);
        Assert.False(first.Completion!.IsCompleted);
        Assert.Equal(PersistenceOperationKind.Acquire, call.Request.Operation.Kind);
        Assert.Equal(Identity(), call.Request.Operation.Identity);
        Assert.Equal(RuntimeOwnerId, call.Request.Operation.OwnerId);
        Assert.Equal(7, call.Request.Operation.ExpectedFence);
        Assert.Equal(0, call.Request.Operation.Sequence);
        Assert.Equal(CharacterClass.Mage, call.Request.IntendedClass);
        Assert.Equal(Defaults(), call.Request.SafeDefaults);
        Assert.Equal(PersistenceSubmitStatus.Invalid, second.Status);
        Assert.Null(second.Completion);

        call.Complete(FreshAcquired(call.Request.Operation.OperationId, 8, Token(1), Token(2)));
        Assert.Equal(PersistenceWorkStatus.Acquired, (await Within(first.Completion!)).Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Acquire));
        Assert.Equal(0, lane.Repository.Count(Repo.Admission));
    }

    [Fact]
    public async Task Submit_ReturnsWhileRepositoryIsPending_AndAcceptedIsNotDurable()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission acquire = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> pendingAcquire = await lane.Repository.NextAcquireAsync();

        // The acquire never completes during this call; a submission that waited for I/O would time out here.
        PersistenceSubmission checkpoint = await ReturnsPromptly(() => lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget()));

        Assert.Equal(PersistenceSubmitStatus.Accepted, checkpoint.Status);
        Assert.False(checkpoint.Completion!.IsCompleted);
        Assert.False(acquire.Completion!.IsCompleted);
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));

        pendingAcquire.Complete(FreshAcquired(pendingAcquire.Request.Operation.OperationId, 8, Token(1), Token(2)));
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> write = await lane.Repository.NextCheckpointAsync();
        Assert.False(checkpoint.Completion!.IsCompleted);
        write.Complete(Durable(write.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Assert.Equal(PersistenceWorkStatus.Durable, (await Within(checkpoint.Completion!)).Status);
    }

    [Fact]
    public async Task Lane_SendsOneCallAtATime_AndAdvancesOnlyFromConfirmedResults()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        PersistenceSubmission first = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        PersistenceSubmission second = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());
        PersistenceSubmission third = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        Assert.All(new[] { first, second, third }, submission => Assert.Equal(PersistenceSubmitStatus.Accepted, submission.Status));

        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> one = await lane.Repository.NextCheckpointAsync();
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(Runtime(8, 0), one.Request.Authority);
        Assert.Equal(1, one.Request.Operation.Sequence);
        Assert.Equal(Token(1), one.Request.ExpectedCharacterToken);
        Assert.Equal(Token(2), one.Request.ExpectedProgressToken);
        one.Complete(Durable(one.Request.Operation.OperationId, 1, Token(3), Token(4)));

        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> two = await lane.Repository.NextCheckpointAsync();
        Assert.Equal(Runtime(8, 1), two.Request.Authority);
        Assert.Equal(2, two.Request.Operation.Sequence);
        Assert.Equal(Token(3), two.Request.ExpectedCharacterToken);
        Assert.Equal(Token(4), two.Request.ExpectedProgressToken);
        Assert.Equal(KnightSafeMoved(), two.Request.Safe);
        two.Complete(NoChange(two.Request.Operation.OperationId, 2, Token(3), Token(4)));

        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> three = await lane.Repository.NextCheckpointAsync();
        Assert.Equal(Runtime(8, 2), three.Request.Authority);
        Assert.Equal(3, three.Request.Operation.Sequence);
        Assert.Equal(Token(3), three.Request.ExpectedCharacterToken);
        three.Complete(Durable(three.Request.Operation.OperationId, 3, Token(5), Token(6)));

        Assert.Equal(PersistenceWorkStatus.Durable, (await Within(first.Completion!)).Status);
        Assert.Equal(PersistenceWorkStatus.CheckpointNoChange, (await Within(second.Completion!)).Status);
        Assert.Equal(PersistenceWorkStatus.Durable, (await Within(third.Completion!)).Status);
        Assert.Equal(3, new[] { one, two, three }.Select(call => call.Request.Operation.OperationId).Distinct().Count());
        Assert.Equal(1, lane.Repository.MaxInFlight);
    }

    [Fact]
    public async Task FullWaitingSpace_RejectsWithoutRepositoryCalls()
    {
        CharacterPersistenceOwnerHarness lane = new(queueCapacity: 1);
        await lane.AcquireAsync();
        (PersistenceSubmission running, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> runningCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());

        PersistenceSubmission waiting = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget());
        PersistenceSubmission overflow = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());

        Assert.Equal(PersistenceSubmitStatus.Accepted, waiting.Status);
        Assert.Equal(PersistenceSubmitStatus.QueueFull, overflow.Status);
        Assert.Null(overflow.Completion);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));

        runningCall.Complete(Durable(runningCall.Request.Operation.OperationId, 1, Token(3), Token(4)));
        Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> waitingCall = await lane.Repository.NextCheckpointAsync();
        Assert.Equal(KnightSafeMoved(), waitingCall.Request.Safe);
        waitingCall.Complete(Durable(waitingCall.Request.Operation.OperationId, 2, Token(5), Token(6)));
        await Within(running.Completion!);
        await Within(waiting.Completion!);
        await Settle();
        Assert.Equal(2, lane.Repository.Count(Repo.Checkpoint));
        Assert.Equal(1, lane.Repository.MaxInFlight);
    }

    [Fact]
    public async Task CloseHandOff_UsesTheFinalSpace_WhenWaitingSpaceIsFull()
    {
        CharacterPersistenceOwnerHarness lane = new(queueCapacity: 1);
        await lane.AcquireAsync();
        (_, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> runningCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        PersistenceSubmission waiting = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        Assert.Equal(PersistenceSubmitStatus.Accepted, waiting.Status);
        Assert.Equal(PersistenceSubmitStatus.QueueFull, lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget()).Status);
        SafeCheckpoint finalSafe = new(CharacterClass.Knight, -40f, 2f, 150);

        Task<PersistenceCloseResult> close = await ReturnsPromptly(() => lane.Owner.CloseAsync(Ticket(finalSafe), lane.Budget()));
        Assert.False(close.IsCompleted);
        runningCall.Complete(Durable(runningCall.Request.Operation.OperationId, 1, Token(3), Token(4)));

        // Whether the accepted waiting checkpoint runs before the final one is left open by the contract,
        // so each arriving checkpoint is confirmed with the sequence it carries.
        int finalWrites = 0;
        bool released = false;
        for (int round = 0; round < 6 && !released; round++)
        {
            object next = await lane.Repository.NextAnyAsync();
            switch (next)
            {
                case Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> write:
                    if (write.Request.Safe.Equals(finalSafe))
                        finalWrites++;
                    long sequence = write.Request.Operation.Sequence;
                    write.Complete(Durable(write.Request.Operation.OperationId, sequence, Token((byte)(10 + sequence)), Token((byte)(20 + sequence))));
                    break;
                case Repo.PendingCall<ReleaseRequest, ReleaseResult> release:
                    release.Complete(Released(release.Request.Operation.OperationId));
                    released = true;
                    break;
                default:
                    Assert.Fail($"Unexpected repository call {next.GetType().Name} during close.");
                    break;
            }
        }

        PersistenceCloseResult result = await Within(close);
        Assert.True(released);
        Assert.Equal(1, finalWrites);
        Assert.Equal(PersistenceCloseStatus.Released, result.Status);
        Assert.Equal(CloseWriteDecision.CheckpointRequired, result.WriteDecision);
        await Within(waiting.Completion!);
        Assert.Equal(1, lane.Repository.MaxInFlight);
    }

    [Fact]
    public async Task Submit_AfterCloseStarted_IsRejectedAsClosing()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();

        Task<PersistenceCloseResult> close = lane.Owner.CloseAsync(Ticket(), lane.Budget());
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await lane.Repository.NextReleaseAsync();
        PersistenceSubmission late = lane.Owner.TrySubmitCheckpoint(KnightSafe(), lane.Budget());
        release.Complete(Released(release.Request.Operation.OperationId));

        Assert.Equal(PersistenceSubmitStatus.Closing, late.Status);
        Assert.Null(late.Completion);
        Assert.Equal(PersistenceCloseStatus.Released, (await Within(close)).Status);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task Submit_WithExpiredDeadline_IsRejectedWithoutRepositoryCall()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        PersistenceDeadline shortBudget = lane.Budget(1);
        lane.Clock.Advance(TimeSpan.FromSeconds(1));

        PersistenceSubmission expired = lane.Owner.TrySubmitCheckpoint(KnightSafe(), shortBudget);

        Assert.Equal(PersistenceSubmitStatus.Expired, expired.Status);
        Assert.Null(expired.Completion);
        await Settle();
        Assert.Equal(0, lane.Repository.Count(Repo.Checkpoint));
    }

    [Fact]
    public async Task WaitingWork_WhoseDeadlinePassesBeforeDispatch_IsNeverSent()
    {
        CharacterPersistenceOwnerHarness lane = new(queueCapacity: 2);
        await lane.AcquireAsync();
        (_, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> runningCall) = await lane.CheckpointInFlightAsync(KnightSafe(), lane.Budget());
        PersistenceSubmission shortLived = lane.Owner.TrySubmitCheckpoint(KnightSafeMoved(), lane.Budget(2));
        Assert.Equal(PersistenceSubmitStatus.Accepted, shortLived.Status);

        lane.Clock.Advance(TimeSpan.FromSeconds(3));
        runningCall.Complete(Durable(runningCall.Request.Operation.OperationId, 1, Token(3), Token(4)));

        Assert.Equal(PersistenceWorkStatus.NotDispatched, (await Within(shortLived.Completion!)).Status);
        await Settle();
        Assert.Equal(1, lane.Repository.Count(Repo.Checkpoint));
    }
}
