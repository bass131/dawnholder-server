namespace Dawnholder.Server.GameServer.Persistence;

public enum RecoveryOperationResolutionStatus : byte
{
    StillUnknown = 1,
    NotApplied = 2,
    Applied = 3,
    FenceLost = 4
}
