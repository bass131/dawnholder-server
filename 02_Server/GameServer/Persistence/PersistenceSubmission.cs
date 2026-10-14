namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Accepted transfers responsibility to the owner; only Completion describes the store outcome.</summary>
public sealed class PersistenceSubmission
{
    internal PersistenceSubmission(Task<PersistenceWorkResult> completion)
    {
        Status = PersistenceSubmitStatus.Accepted;
        Completion = completion;
    }

    internal PersistenceSubmission(PersistenceSubmitStatus status)
    {
        if (status == PersistenceSubmitStatus.Accepted || !Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        Status = status;
    }

    public PersistenceSubmitStatus Status { get; }
    public Task<PersistenceWorkResult>? Completion { get; }
}
