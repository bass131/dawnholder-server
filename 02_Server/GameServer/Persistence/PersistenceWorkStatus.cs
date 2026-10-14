namespace Dawnholder.Server.GameServer.Persistence;

public enum PersistenceWorkStatus : byte
{
    Acquired = 1,
    Durable,
    CheckpointNoChange,
    Rejected,
    Conflict,
    NotDispatched,
    NotApplied,
    Unresolved,
    FenceLost
}
