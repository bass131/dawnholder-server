namespace Shared.GameData;

// Stable wire IDs. None is the empty-slot sentinel; new items append without reusing IDs.
public enum ItemId : int
{
    None = 0,
    Material = 1,
    CoinPouch = 2,
}
