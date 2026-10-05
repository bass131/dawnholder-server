using System;

namespace Shared.GameData;

// Placeholder definitions live here; the server remains responsible for applying their effects.
public static class ItemCatalog
{
    static readonly ItemDefinition s_material = new(ItemId.Material, "재료", 0);
    static readonly ItemDefinition s_coinPouch = new(ItemId.CoinPouch, "재화 주머니", 50);

    public static bool IsDefined(ItemId id) => id == ItemId.Material || id == ItemId.CoinPouch;

    // Unknown internal IDs are a catalog/invariant error, rather than an ordinary use rejection.
    public static ItemDefinition For(ItemId id)
    {
        return id switch
        {
            ItemId.Material => s_material,
            ItemId.CoinPouch => s_coinPouch,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Undefined item ID."),
        };
    }
}
