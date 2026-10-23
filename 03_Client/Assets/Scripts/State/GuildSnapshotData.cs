using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.State
{
    /// <summary>Validated, owned packet values shared by presentation and request correlation.</summary>
    internal sealed class GuildSnapshotData
    {
        internal GuildSnapshotData(S_GuildSnapshot packet)
        {
            SnapshotSequence = packet.snapshotSequence;
            ChangeKind = (GuildChangeKind)packet.changeKind;
            GuildId = packet.guildId;
            GuildName = packet.guildName;
            GuildRevision = packet.guildRevision;
            Treasury = packet.treasury;
            StrongholdLevel = packet.strongholdLevel;
            MaxStrongholdLevel = packet.maxStrongholdLevel;
            BlacksmithUnlocked = packet.blacksmithUnlocked == 1;
            MemberCount = packet.memberCount;
            OnlineMemberCount = packet.onlineMemberCount;
            InventoryRevision = packet.inventoryRevision;
            WeaponLevel = packet.weaponLevel;
            MaxWeaponLevel = packet.maxWeaponLevel;
            AttackBonus = packet.attackBonus;
            StrongholdCost = packet.strongholdCost;
            WeaponCurrencyCost = packet.weaponCurrencyCost;
            WeaponMaterialId = packet.weaponMaterialId;
            WeaponMaterialCount = packet.weaponMaterialCount;
            StrongholdAvailability = (GuildResult)packet.strongholdAvailability;
            WeaponAvailability = (GuildResult)packet.weaponAvailability;
        }

        internal long SnapshotSequence { get; }
        internal GuildChangeKind ChangeKind { get; }
        internal uint GuildId { get; }
        internal string GuildName { get; }
        internal uint GuildRevision { get; }
        internal int Treasury { get; }
        internal int StrongholdLevel { get; }
        internal int MaxStrongholdLevel { get; }
        internal bool BlacksmithUnlocked { get; }
        internal int MemberCount { get; }
        internal int OnlineMemberCount { get; }
        internal uint InventoryRevision { get; }
        internal int WeaponLevel { get; }
        internal int MaxWeaponLevel { get; }
        internal int AttackBonus { get; }
        internal int StrongholdCost { get; }
        internal int WeaponCurrencyCost { get; }
        internal int WeaponMaterialId { get; }
        internal int WeaponMaterialCount { get; }
        internal GuildResult StrongholdAvailability { get; }
        internal GuildResult WeaponAvailability { get; }
    }
}
