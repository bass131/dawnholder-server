using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

// Core status ordinals are private to persistence. Only these named failures cross the wire;
// wrapper success/reconciliation and the request lifetime belong to the session adapter.
internal static class CharacterMenuCatalogResultMapper
{
    internal static (CharacterMenuResult Result, CharacterMenuInvalidField InvalidField) MapFailure(
        CatalogStatus status, InvalidField invalidField)
    {
        if (status == CatalogStatus.InvalidInput)
        {
            return invalidField switch
            {
                InvalidField.Slot => (CharacterMenuResult.InvalidInput, CharacterMenuInvalidField.Slot),
                InvalidField.Name => (CharacterMenuResult.InvalidInput, CharacterMenuInvalidField.Name),
                InvalidField.Class => (CharacterMenuResult.InvalidInput, CharacterMenuInvalidField.Class),
                InvalidField.CharacterId => (CharacterMenuResult.InvalidInput, CharacterMenuInvalidField.CharacterId),
                _ => (CharacterMenuResult.IntegrityFailure, CharacterMenuInvalidField.None),
            };
        }

        CharacterMenuResult result = status switch
        {
            CatalogStatus.RequestInProgress => CharacterMenuResult.RequestInProgress,
            CatalogStatus.InvalidState => CharacterMenuResult.InvalidState,
            CatalogStatus.SlotOccupied => CharacterMenuResult.SlotOccupied,
            CatalogStatus.SlotsFull => CharacterMenuResult.SlotsFull,
            CatalogStatus.NameTaken => CharacterMenuResult.NameTaken,
            CatalogStatus.CharacterNotFound or CatalogStatus.NotOwned => CharacterMenuResult.CharacterUnavailable,
            CatalogStatus.Busy => CharacterMenuResult.Busy,
            CatalogStatus.RecoveryRequired => CharacterMenuResult.RecoveryRequired,
            CatalogStatus.CancelledBeforeDispatch => CharacterMenuResult.CancelledBeforeDispatch,
            CatalogStatus.Unavailable => CharacterMenuResult.Unavailable,
            CatalogStatus.OutcomeUnknown or CatalogStatus.StillUnknown => CharacterMenuResult.OutcomeUnknown,
            CatalogStatus.SchemaMismatch => CharacterMenuResult.SchemaMismatch,
            CatalogStatus.IntegrityFailure => CharacterMenuResult.IntegrityFailure,
            CatalogStatus.OperationConflict => CharacterMenuResult.OperationConflict,
            _ => CharacterMenuResult.IntegrityFailure,
        };
        return (result, CharacterMenuInvalidField.None);
    }
}
