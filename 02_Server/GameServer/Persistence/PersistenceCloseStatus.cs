namespace Dawnholder.Server.GameServer.Persistence;

public enum PersistenceCloseStatus : byte
{
    Released = 1,
    FenceLost,
    Unresolved,
    RecoveryRequired,
    ShutdownIncomplete,
    NotHeld
}
