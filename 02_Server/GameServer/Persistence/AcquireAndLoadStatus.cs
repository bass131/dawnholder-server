namespace Dawnholder.Server.GameServer.Persistence;

public enum AcquireAndLoadStatus : byte
{
    Acquired = 1,
    Rejected = 2,
    Conflict = 3,
    NotDispatched = 4,
    AcquireUnknown = 5
}
