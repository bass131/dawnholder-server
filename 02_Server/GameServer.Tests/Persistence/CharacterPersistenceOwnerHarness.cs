using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Wires one CharacterPersistenceOwner to the recording repository and the controllable clock, and holds
/// the scripted store answers shared by the owner tests. Store answers carry literal fences, sequences and
/// tokens chosen by the test; they do not reproduce the owner's own sequencing or authority decisions.
/// </summary>
sealed class CharacterPersistenceOwnerHarness
{
    public CharacterPersistenceOwnerHarness(int queueCapacity = 4)
    {
        Owner = new CharacterPersistenceOwner(Identity(), RuntimeOwnerId, Repository, Clock, queueCapacity);
    }

    public ControllableTimeProvider Clock { get; } = new();

    public RecordingCharacterRepository Repository { get; } = new();

    public CharacterPersistenceOwner Owner { get; }

    public static CloseCaptureTicket Ticket(SafeCheckpoint? checkpoint = null) => new(Identity(), RuntimeOwnerId, checkpoint);

    public static WriteSafeCheckpointResult Durable(Guid operationId, long confirmedSequence, PersistenceToken character, PersistenceToken progress)
    {
        PersistenceAuthority after = Runtime(8, confirmedSequence);
        return WriteSafeCheckpointResult.Durable(
            CheckpointReceipt(operationId, PersistenceResultCode.CheckpointApplied, after, character, progress),
            Observed(after, character, progress));
    }

    public static WriteSafeCheckpointResult NoChange(Guid operationId, long confirmedSequence, PersistenceToken character, PersistenceToken progress)
    {
        PersistenceAuthority after = Runtime(8, confirmedSequence);
        return WriteSafeCheckpointResult.CheckpointNoChange(
            CheckpointReceipt(operationId, PersistenceResultCode.CheckpointNoChange, after, character, progress),
            Observed(after, character, progress));
    }

    /// <summary>Store confirms the release: authority becomes Free with fence 9.</summary>
    public static ReleaseResult Released(Guid operationId)
        => ReleaseResult.Released(ReleasedReceipt(operationId, Free(9)), Observed(Free(9), Token(1), Token(2)));

    /// <summary>
    /// Gives a misbehaving owner a short real-time window to emit a forbidden call before a count is read.
    /// A correct owner is not slowed down by it; it only makes absence assertions less likely to pass early.
    /// </summary>
    public static Task Settle() => Task.Delay(50);

    /// <summary>
    /// Runs a call that must return without waiting for repository I/O and fails if it blocks past the limit.
    /// StartNew is used instead of Task.Run so a returned Task is handed back as a value, not awaited.
    /// </summary>
    public static async Task<T> ReturnsPromptly<T>(Func<T> call)
    {
        Task<T> started = Task.Factory.StartNew(call, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        return await started.WaitAsync(RecordingCharacterRepository.Limit);
    }

    public PersistenceDeadline Budget(double seconds = 30) => PersistenceDeadline.FromBudget(Clock, TimeSpan.FromSeconds(seconds));

    /// <summary>Acquire with expected fence 7; the scripted store answers fence 8, sequence 0, tokens 1 and 2.</summary>
    public async Task AcquireAsync()
    {
        PersistenceSubmission submission = Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), Budget());
        Assert.Equal(PersistenceSubmitStatus.Accepted, submission.Status);
        RecordingCharacterRepository.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await Repository.NextAcquireAsync();
        call.Complete(FreshAcquired(call.Request.Operation.OperationId, 8, Token(1), Token(2)));
        PersistenceWorkResult result = await Within(submission.Completion!);
        Assert.Equal(PersistenceWorkStatus.Acquired, result.Status);
    }

    /// <summary>Submits a checkpoint and returns it once the repository has received it.</summary>
    public async Task<(PersistenceSubmission Submission, RecordingCharacterRepository.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> Call)> CheckpointInFlightAsync(SafeCheckpoint safe, PersistenceDeadline deadline)
    {
        PersistenceSubmission submission = Owner.TrySubmitCheckpoint(safe, deadline);
        Assert.Equal(PersistenceSubmitStatus.Accepted, submission.Status);
        return (submission, await Repository.NextCheckpointAsync());
    }

    /// <summary>
    /// Answers resolver calls until <paramref name="terminal"/> completes and returns their modes in order.
    /// Every resolver call must carry <paramref name="expected"/>; any other repository call in between fails.
    /// </summary>
    public async Task<IReadOnlyList<ResolveMode>> AnswerResolverUntilAsync(
        OperationIdentity expected,
        Func<ResolveMode, ResolveOperationResult> answer,
        Task terminal)
    {
        List<ResolveMode> modes = new();
        for (int round = 0; round < 4; round++)
        {
            Task arrival = Repository.WhenUntakenCallAsync();
            await Task.WhenAny(terminal, arrival).WaitAsync(RecordingCharacterRepository.Limit);
            if (!arrival.IsCompleted)
                return modes;
            RecordingCharacterRepository.PendingCall<ResolveOperationRequest, ResolveOperationResult> call = await Repository.NextResolveAsync();
            Assert.Equal(expected, call.Request.Operation);
            modes.Add(call.Request.Mode);
            call.Complete(answer(call.Request.Mode));
        }
        Assert.Fail("The owner called the resolver more than four times for one operation.");
        return modes;
    }

    /// <summary>The contract allows QueryOnly once, then at most one SealIfAbsent, never the reverse.</summary>
    public static void AssertQueryThenOptionalSeal(IReadOnlyList<ResolveMode> modes)
    {
        Assert.InRange(modes.Count, 1, 2);
        Assert.Equal(ResolveMode.QueryOnly, modes[0]);
        if (modes.Count == 2)
            Assert.Equal(ResolveMode.SealIfAbsent, modes[1]);
    }
}
