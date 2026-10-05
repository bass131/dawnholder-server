using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// A whole reward bundle. Invalid policy data throws; exceeding a player's capacity is a normal
// InventoryTransitions rejection. Neither path permits partial grants or a retry queue.
internal sealed class InventoryReward
{
    internal InventoryReward(int currency, params InventorySlot[] items)
    {
        if (currency < 0) throw new ArgumentOutOfRangeException(nameof(currency));
        ArgumentNullException.ThrowIfNull(items);
        HashSet<ItemId> ids = new();
        foreach (InventorySlot item in items)
        {
            _ = ItemCatalog.For(item.ItemId);
            if (item.Count <= 0) throw new ArgumentOutOfRangeException(nameof(items), "Reward quantities must be positive.");
            if (!ids.Add(item.ItemId)) throw new ArgumentException("Duplicate reward stack.", nameof(items));
        }
        if (currency == 0 && items.Length == 0)
            throw new ArgumentException("A reward must contain a positive gain.", nameof(items));

        Currency = currency;
        Items = Array.AsReadOnly((InventorySlot[])items.Clone());
    }

    internal int Currency { get; }

    internal IReadOnlyList<InventorySlot> Items { get; }
}
