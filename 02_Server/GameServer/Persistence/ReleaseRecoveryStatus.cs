namespace Dawnholder.Server.GameServer.Persistence;

public enum ReleaseRecoveryStatus : byte
{
    Released = 1,
    Rejected = 2,
    NotDispatched = 3,
    ReleaseUnknown = 4
}
