namespace Dawnholder.Server.GameServer.Persistence;

public sealed class PersistenceWorkResult
{
    internal PersistenceWorkResult(PersistenceWorkStatus status)
    {
        Status = status;
    }

    public PersistenceWorkStatus Status { get; }
}
