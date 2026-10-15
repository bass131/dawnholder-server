namespace Dawnholder.Server.GameServer.Persistence;

public enum PersistenceSubmitStatus : byte
{
    Accepted = 1,
    QueueFull = 2,
    Closing = 3,
    Frozen = 4,
    Invalid = 5,
    Expired = 6
}
