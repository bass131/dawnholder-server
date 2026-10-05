using System;

namespace Shared.GameData;

public sealed class ItemDefinition
{
    internal ItemDefinition(ItemId id, string displayName, int currencyOnUse)
    {
        if ((int)id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("An item needs a display name.", nameof(displayName));
        if (currencyOnUse < 0 || currencyOnUse > InventoryLimits.MaxCurrency)
            throw new ArgumentOutOfRangeException(nameof(currencyOnUse));

        Id = id;
        DisplayName = displayName;
        CurrencyOnUse = currencyOnUse;
    }

    public ItemId Id { get; }

    public string DisplayName { get; }

    public int CurrencyOnUse { get; }

    public bool IsUsable => CurrencyOnUse > 0;
}
