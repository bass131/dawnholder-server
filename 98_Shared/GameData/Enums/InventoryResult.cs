namespace Shared.GameData;

// S_ItemUseResult codes. Capacity failures also describe a rejected reward bundle internally.
public enum InventoryResult : byte
{
    Success = 0,
    Stale = 1,
    NotOwned = 2,
    NotUsable = 3,
    CurrencyCap = 4,
    InventoryFull = 5,
    RevisionExhausted = 6,
}
