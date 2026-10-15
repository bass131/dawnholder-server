namespace Dawnholder.Server.GameServer.Persistence;

public enum PersistenceOperationKind : byte
{
    Acquire = 1,
    Checkpoint = 2,
    ReleaseRuntime = 3,
    Recover = 4,
    ReleaseRecovery = 5
}
