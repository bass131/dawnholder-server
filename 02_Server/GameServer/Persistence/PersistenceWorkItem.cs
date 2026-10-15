namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Accepted intent. Checkpoint authority and tokens are bound only when the lane dispatches it.</summary>
sealed class PersistenceWorkItem
{
    internal PersistenceWorkItem(AcquireAndLoadRequest acquire, PersistenceDeadline deadline)
    {
        Acquire = acquire;
        Deadline = deadline;
    }

    internal PersistenceWorkItem(SafeCheckpoint checkpoint, PersistenceDeadline deadline)
    {
        Checkpoint = checkpoint;
        Deadline = deadline;
    }

    internal AcquireAndLoadRequest? Acquire { get; }
    internal SafeCheckpoint? Checkpoint { get; }
    internal PersistenceDeadline Deadline { get; }
    internal TaskCompletionSource<PersistenceWorkResult> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
