namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>The final slot is independent of the bounded waiting queue and is captured only once.</summary>
sealed class PersistenceCloseState
{
    internal PersistenceCloseState(CloseCaptureTicket ticket, PersistenceDeadline deadline)
    {
        Ticket = ticket;
        Deadline = deadline;
    }

    internal CloseCaptureTicket Ticket { get; }
    internal PersistenceDeadline Deadline { get; }
    internal TaskCompletionSource<PersistenceCloseResult> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal PersistenceDeadlineWatch? Watch { get; set; }
    internal bool Started { get; set; }
}
