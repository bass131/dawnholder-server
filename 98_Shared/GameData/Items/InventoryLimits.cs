namespace Shared.GameData;

// Temporary content values, shared by authority and presentation. The v23 fixed wire has 24 slots.
public static class InventoryLimits
{
    public const int MaxSlots = 24;
    public const int MaxStack = 99;
    public const int MaxCurrency = 1_000_000_000;
}
