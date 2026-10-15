namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>A settlement snapshot; a later answer can settle an operation still listed in this result.</summary>
public sealed class PersistenceCloseResult
{
    internal PersistenceCloseResult(
        PersistenceCloseStatus status,
        CloseWriteDecision writeDecision,
        IEnumerable<Guid> unfinishedOperationIds)
    {
        Status = status;
        WriteDecision = writeDecision;
        UnfinishedOperationIds = Array.AsReadOnly(unfinishedOperationIds.ToArray());
    }

    public PersistenceCloseStatus Status { get; }
    public CloseWriteDecision WriteDecision { get; }
    public IReadOnlyCollection<Guid> UnfinishedOperationIds { get; }
}
