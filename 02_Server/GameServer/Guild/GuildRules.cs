using Dawnholder.Server.GameServer.Items;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Guild;

// Server-owned demo economy. Clients display the prices and limits in S_GuildSnapshot;
// they do not independently grant funds, unlock facilities, or decide purchases.
internal static class GuildRules
{
    internal const uint GuildId = 1;
    internal const string GuildName = "새벽 길드";
    internal const int JoinSeed = 5;
    internal const int DungeonClearCredit = 10;
    internal const int TreasuryCap = 1_000_000_000;
    internal const byte MaxStrongholdLevel = 2;
    internal const int StrongholdCost = 30;
    internal const byte MaxWeaponLevel = 1;
    internal const int WeaponCurrencyCost = 20;
    internal const ItemId WeaponMaterial = ItemId.Material;
    internal const int WeaponMaterialCount = 1;
    internal const int WeaponAttackBonus = 5;

    internal static int AttackBonus(byte weaponLevel) => weaponLevel == 0 ? 0 : WeaponAttackBonus;

    // Owner/location/alive precede these economic checks. Null revision is used only for
    // displayed availability, which follows the same order without the stale check.
    internal static GuildResult StrongholdAvailability(GuildState state, uint? expectedRevision)
    {
        if (expectedRevision.HasValue && expectedRevision.Value != state.Revision) return GuildResult.Stale;
        if (state.StrongholdLevel == MaxStrongholdLevel) return GuildResult.MaxLevel;
        if (state.Revision == uint.MaxValue) return GuildResult.RevisionExhausted;
        if (state.Treasury < StrongholdCost) return GuildResult.InsufficientCurrency;
        return GuildResult.Success;
    }

    internal static GuildResult WeaponAvailability(
        InventoryRegistry.GuildView inventory, bool facilityUnlocked, uint? expectedRevision)
    {
        if (expectedRevision.HasValue && expectedRevision.Value != inventory.Revision) return GuildResult.Stale;
        if (inventory.WeaponLevel == MaxWeaponLevel) return GuildResult.MaxLevel;
        if (!facilityUnlocked) return GuildResult.FacilityLocked;
        if (inventory.Revision == uint.MaxValue) return GuildResult.RevisionExhausted;
        if (inventory.Currency < WeaponCurrencyCost) return GuildResult.InsufficientCurrency;
        if (inventory.MaterialCount < WeaponMaterialCount) return GuildResult.InsufficientMaterial;
        return GuildResult.Success;
    }
}
