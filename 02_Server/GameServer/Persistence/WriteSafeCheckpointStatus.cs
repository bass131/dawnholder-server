namespace Dawnholder.Server.GameServer.Persistence;

public enum WriteSafeCheckpointStatus : byte
{
    Durable = 1,
    CheckpointNoChange = 2,
    Rejected = 3,
    Conflict = 4,
    NotDispatched = 5,
    CommitUnknown = 6
}
