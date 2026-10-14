namespace Dawnholder.Server.GameServer.Persistence;

public enum PersistenceResultCode : short
{
    Acquired = 100,
    CheckpointApplied = 101,
    CheckpointNoChange = 102,
    Released = 103,
    Recovered = 104,
    RecoveredAbsent = 105,
    Busy = 200,
    StaleFence = 201,
    Conflict = 202,
    IdentityMismatch = 203,
    InvalidClass = 204,
    CancelledBeforeApply = 205,
    SequenceMismatch = 206,
    IntegrityFailure = 207
}
