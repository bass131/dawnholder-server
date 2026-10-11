using System;
using System.Collections.Generic;

namespace Shared.GameData;

// Temporary values from the 10-08 decision 2 A; the server owns eligibility and atomic granting.
public static class DungeonClearRewards
{
    public const int Currency = 50;
    public const int MaxItemKinds = 3;

    public static IReadOnlyList<InventorySlot> Items { get; } = Array.AsReadOnly(new[]
    {
        new InventorySlot(ItemId.Material, 1),
        new InventorySlot(ItemId.CoinPouch, 1),
    });
}
