namespace Shared.Protocol;

public enum CharacterLoadStatus : byte
{
    Success = 0,
    Busy = 1,
    RecoveryRequired = 2,
    Unavailable = 3
}
