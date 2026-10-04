using System.Collections.ObjectModel;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// Immutable, canonical value owned by InventoryRegistry on the world tick. Construction copies
// caller storage, validates every slot, then orders and pads the fixed eight-slot snapshot.
internal sealed class InventoryState
{
    static readonly InventoryState s_empty = new(0, 0, Array.Empty<InventorySlot>());
    readonly ReadOnlyCollection<InventorySlot> _slots;

    internal InventoryState(uint revision, int currency, IEnumerable<InventorySlot> slots)
    {
        if (currency < 0 || currency > InventoryLimits.MaxCurrency)
            throw new InvalidOperationException("Inventory currency invariant violated.");
        ArgumentNullException.ThrowIfNull(slots);

        List<InventorySlot> occupied = new();
        HashSet<ItemId> ids = new();
        foreach (InventorySlot slot in slots)
        {
            if (slot.ItemId == ItemId.None && slot.Count == 0) continue;
            if (!ItemCatalog.IsDefined(slot.ItemId) || slot.Count <= 0 || slot.Count > InventoryLimits.MaxStack)
                throw new InvalidOperationException("Inventory slot invariant violated.");
            if (!ids.Add(slot.ItemId)) throw new InvalidOperationException("Duplicate inventory stack.");
            occupied.Add(slot);
        }
        if (occupied.Count > InventoryLimits.MaxSlots)
            throw new InvalidOperationException("Inventory slot capacity invariant violated.");

        occupied.Sort((left, right) => left.ItemId.CompareTo(right.ItemId));
        InventorySlot[] canonical = new InventorySlot[InventoryLimits.MaxSlots];
        occupied.CopyTo(canonical);
        _slots = Array.AsReadOnly(canonical);
        Revision = revision;
        Currency = currency;
    }

    internal static InventoryState Empty => s_empty;

    internal uint Revision { get; }

    internal int Currency { get; }

    internal IReadOnlyList<InventorySlot> Slots => _slots;

    internal int GetCount(ItemId itemId)
    {
        _ = ItemCatalog.For(itemId);
        foreach (InventorySlot slot in _slots)
        {
            if (slot.ItemId == itemId) return slot.Count;
        }
        return 0;
    }
}
