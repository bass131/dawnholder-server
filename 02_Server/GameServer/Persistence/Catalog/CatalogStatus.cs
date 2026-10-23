namespace Dawnholder.Server.GameServer.Persistence.Catalog;

public enum CatalogStatus
{
    Prepared, Listed, Created, Selected, Applied, NotApplied, StillUnknown,
    InvalidInput, OperationConflict, SlotOccupied, SlotsFull, NameTaken,
    CharacterNotFound, NotOwned, RequestInProgress, InvalidState, Busy, RecoveryRequired,
    CancelledBeforeDispatch, Unavailable, SchemaMismatch, IntegrityFailure, OutcomeUnknown
}
