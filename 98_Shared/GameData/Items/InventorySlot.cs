namespace Shared.GameData;

// A value copy; an external snapshot reader cannot change an authority-owned stack.
public readonly struct InventorySlot
{
    public InventorySlot(ItemId itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }

    public ItemId ItemId { get; }

    public int Count { get; }
}
