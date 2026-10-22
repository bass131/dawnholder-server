#nullable enable
using Shared.GameData;

namespace Dawnholder.Client.State
{
    /// <summary>Connection-owned server view. Only GuildRequestController updates this stable object.</summary>
    public sealed class GuildClientState
    {
        GuildSnapshotData? _shared;
        GuildSnapshotData? _personal;

        public bool HasSnapshot => _shared != null;
        public long SnapshotSequence => _shared?.SnapshotSequence ?? 0;
        public GuildChangeKind ChangeKind => _shared?.ChangeKind ?? GuildChangeKind.Query;
        public uint GuildId => _shared?.GuildId ?? 0;
        public string GuildName => _shared?.GuildName ?? string.Empty;
        public uint GuildRevision => _shared?.GuildRevision ?? 0;
        public int Treasury => _shared?.Treasury ?? 0;
        public int StrongholdLevel => _shared?.StrongholdLevel ?? 0;
        public int MaxStrongholdLevel => _shared?.MaxStrongholdLevel ?? 0;
        public bool BlacksmithUnlocked => _shared?.BlacksmithUnlocked ?? false;
        public int MemberCount => _shared?.MemberCount ?? 0;
        public int OnlineMemberCount => _shared?.OnlineMemberCount ?? 0;
        public int StrongholdCost => _shared?.StrongholdCost ?? 0;
        public GuildResult StrongholdAvailability => _shared?.StrongholdAvailability ?? GuildResult.Unavailable;

        // A valid revision zero is distinct from having no personal snapshot. A newer 36 can make
        // the retained personal fields stale without rolling them back from a lower-revision 44.
        public uint InventoryRevision { get; private set; }
        public bool HasPersonalSnapshot => _personal != null;
        public uint PersonalSnapshotRevision => _personal?.InventoryRevision ?? 0;
        public bool IsPersonalCurrent => HasPersonalSnapshot && PersonalSnapshotRevision == InventoryRevision;
        public int WeaponLevel => _personal?.WeaponLevel ?? 0;
        public int MaxWeaponLevel => _personal?.MaxWeaponLevel ?? 0;
        public int AttackBonus => _personal?.AttackBonus ?? 0;
        public int WeaponCurrencyCost => _personal?.WeaponCurrencyCost ?? 0;
        public int WeaponMaterialId => _personal?.WeaponMaterialId ?? 0;
        public int WeaponMaterialCount => _personal?.WeaponMaterialCount ?? 0;
        public GuildResult WeaponAvailability => _personal?.WeaponAvailability ?? GuildResult.Unavailable;

        internal bool Apply(GuildSnapshotData snapshot)
        {
            if (snapshot.SnapshotSequence <= SnapshotSequence) return false;
            _shared = snapshot;
            if (snapshot.InventoryRevision >= InventoryRevision)
            {
                InventoryRevision = snapshot.InventoryRevision;
                _personal = snapshot;
            }
            return true;
        }

        internal bool ObserveInventoryRevision(uint revision)
        {
            if (revision <= InventoryRevision) return false;
            InventoryRevision = revision;
            return true;
        }

        internal void Clear()
        {
            _shared = null;
            _personal = null;
            InventoryRevision = 0;
        }
    }
}
