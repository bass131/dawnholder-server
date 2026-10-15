namespace Dawnholder.Server.GameServer.Persistence;

public enum CloseWriteDecision : byte
{
    NoWriteNeeded = 1,
    CheckpointRequired = 2
}
