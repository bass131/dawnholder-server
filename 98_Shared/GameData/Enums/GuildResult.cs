namespace Shared.GameData;

// Shared wire codes for action results and snapshot availability. Snapshot Success means
// available at publication time, never a reservation or a replayable success event.
public enum GuildResult : byte
{
    Success = 0,
    Stale = 1,
    NotMember = 2,
    NotInTown = 3,
    Dead = 4,
    FacilityLocked = 5,
    MaxLevel = 6,
    InsufficientCurrency = 7,
    InsufficientMaterial = 8,
    RevisionExhausted = 9,
    Unavailable = 10,
}
