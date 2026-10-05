using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// Pure transitions: prepare and validate a complete next value; only the registry can commit it.
// Every ordinary refusal returns the exact current reference, with no revision or slot mutation.
internal static class InventoryTransitions
{
    internal static InventoryResult Grant(InventoryState current, InventoryReward reward, out InventoryState next)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(reward);
        next = current;
        if (current.Revision == uint.MaxValue) return InventoryResult.RevisionExhausted;

        long currency = (long)current.Currency + reward.Currency;
        if (currency > InventoryLimits.MaxCurrency) return InventoryResult.CurrencyCap;

        List<InventorySlot> slots = current.Slots.Where(slot => slot.ItemId != ItemId.None).ToList();
        foreach (InventorySlot gain in reward.Items)
        {
            int index = slots.FindIndex(slot => slot.ItemId == gain.ItemId);
            int owned = index >= 0 ? slots[index].Count : 0;
            long count = (long)owned + gain.Count;
            if (count > InventoryLimits.MaxStack) return InventoryResult.InventoryFull;
            if (index < 0 && slots.Count == InventoryLimits.MaxSlots) return InventoryResult.InventoryFull;

            InventorySlot stack = new(gain.ItemId, (int)count);
            if (index >= 0)
                slots[index] = stack;
            else
                slots.Add(stack);
        }

        next = new InventoryState(current.Revision + 1, (int)currency, slots);
        return InventoryResult.Success;
    }

    internal static InventoryResult Use(InventoryState current, ItemId itemId, uint expectedRevision, out InventoryState next)
    {
        ArgumentNullException.ThrowIfNull(current);
        ItemDefinition definition = ItemCatalog.For(itemId);
        next = current;
        if (expectedRevision != current.Revision) return InventoryResult.Stale;
        if (current.Revision == uint.MaxValue) return InventoryResult.RevisionExhausted;
        if (current.GetCount(itemId) == 0) return InventoryResult.NotOwned;
        if (!definition.IsUsable) return InventoryResult.NotUsable;

        long currency = (long)current.Currency + definition.CurrencyOnUse;
        if (currency > InventoryLimits.MaxCurrency) return InventoryResult.CurrencyCap;

        InventorySlot[] slots = current.Slots.ToArray();
        int index = Array.FindIndex(slots, slot => slot.ItemId == itemId);
        int count = slots[index].Count - 1;
        slots[index] = count == 0 ? default : new InventorySlot(itemId, count);
        next = new InventoryState(current.Revision + 1, (int)currency, slots);
        return InventoryResult.Success;
    }
}
