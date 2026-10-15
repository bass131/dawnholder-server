using Dawnholder.Server.GameServer.Items;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Guild;

internal static class GuildPackets
{
    internal static ArraySegment<byte> Snapshot(GuildState state, InventoryRegistry.GuildView inventory,
        int members, int online, long sequence, GuildChangeKind kind, GuildResult location)
    {
        // Generated string Write/Read has no domain bounds. Validate server-owned values before
        // invoking it; consumers must also check the byte count at offset 64 before Read.
        string name = GuildRules.GuildName;
        if (name.Length < 1 || name.Length > 16 || sequence <= 0 || members < online || online < 0 ||
            inventory.WeaponLevel > GuildRules.MaxWeaponLevel || !Enum.IsDefined(kind) || !Enum.IsDefined(location))
        {
            throw new InvalidOperationException("Guild snapshot invariant violated.");
        }

        bool strongholdMax = state.StrongholdLevel == GuildRules.MaxStrongholdLevel;
        bool weaponMax = inventory.WeaponLevel == GuildRules.MaxWeaponLevel;
        return new S_GuildSnapshot
        {
            snapshotSequence = sequence,
            changeKind = (byte)kind,
            guildId = GuildRules.GuildId,
            guildRevision = state.Revision,
            treasury = state.Treasury,
            strongholdLevel = state.StrongholdLevel,
            maxStrongholdLevel = GuildRules.MaxStrongholdLevel,
            blacksmithUnlocked = state.BlacksmithUnlocked ? (byte)1 : (byte)0,
            memberCount = members,
            onlineMemberCount = online,
            inventoryRevision = inventory.Revision,
            weaponLevel = inventory.WeaponLevel,
            maxWeaponLevel = GuildRules.MaxWeaponLevel,
            attackBonus = GuildRules.AttackBonus(inventory.WeaponLevel),
            strongholdCost = strongholdMax ? 0 : GuildRules.StrongholdCost,
            weaponCurrencyCost = weaponMax ? 0 : GuildRules.WeaponCurrencyCost,
            weaponMaterialId = (int)GuildRules.WeaponMaterial,
            weaponMaterialCount = weaponMax ? 0 : GuildRules.WeaponMaterialCount,
            strongholdAvailability = (byte)(location == GuildResult.Success
                ? GuildRules.StrongholdAvailability(state, null) : location),
            weaponAvailability = (byte)(location == GuildResult.Success
                ? GuildRules.WeaponAvailability(inventory, state.BlacksmithUnlocked, null) : location),
            guildName = name,
        }.Write();
    }

    internal static ArraySegment<byte> ActionResult(GuildAction action, GuildResult result,
        uint requestedRevision, uint guildRevision, uint inventoryRevision, long sequence)
    {
        if (!Enum.IsDefined(action) || !Enum.IsDefined(result) || sequence <= 0)
            throw new InvalidOperationException("Guild result invariant violated.");
        return new S_GuildActionResult
        {
            action = (byte)action,
            result = (byte)result,
            requestedRevision = requestedRevision,
            guildRevision = guildRevision,
            inventoryRevision = inventoryRevision,
            snapshotSequence = sequence,
        }.Write();
    }
}
