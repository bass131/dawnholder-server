namespace Dawnholder.Server.GameServer.Persistence;

public enum RecoverAndLoadStatus : byte
{
    Recovered = 1,
    RecoveredAbsent = 2,
    Rejected = 3,
    NotDispatched = 4,
    RecoverUnknown = 5
}
