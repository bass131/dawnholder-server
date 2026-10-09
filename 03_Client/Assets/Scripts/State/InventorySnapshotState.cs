using System;
using Shared.GameData;

namespace Dawnholder.Client.State
{
    internal enum InventorySnapshotApplyResult { Rejected, Unchanged, Applied }

    /// <summary>Owned server values and validation, independent of Unity and request lifetimes.</summary>
    public sealed class InventorySnapshotState
    {
        InventorySlot[] _slots = new InventorySlot[InventoryLimits.MaxSlots];

        public bool HasSnapshot { get; private set; }
        public uint Revision { get; private set; }
        public int Currency { get; private set; }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= InventoryLimits.MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _slots[index];
        }

        internal static bool IsValidSnapshot(int currency, InventorySlot[] slots)
        {
            if (currency < 0 || currency > InventoryLimits.MaxCurrency ||
                slots == null || slots.Length != InventoryLimits.MaxSlots) return false;

            int previousItem = 0;
            bool reachedEmpty = false;
            foreach (InventorySlot slot in slots)
            {
                if (slot.ItemId == ItemId.None)
                {
                    if (slot.Count != 0) return false;
                    reachedEmpty = true;
                    continue;
                }
                // The authority sends compact, sorted stacks; a hole or repeated ID is not a new inventory.
                if (reachedEmpty || !ItemCatalog.IsDefined(slot.ItemId) ||
                    (int)slot.ItemId <= previousItem || slot.Count < 1 || slot.Count > InventoryLimits.MaxStack)
                    return false;
                previousItem = (int)slot.ItemId;
            }
            return true;
        }

        internal InventorySnapshotApplyResult Apply(uint revision, int currency, InventorySlot[] slots)
        {
            if (!IsValidSnapshot(currency, slots) || (HasSnapshot && revision < Revision))
                return InventorySnapshotApplyResult.Rejected;

            if (HasSnapshot && revision == Revision)
            {
                if (currency != Currency) return InventorySnapshotApplyResult.Rejected;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].ItemId != slots[i].ItemId || _slots[i].Count != slots[i].Count)
                        return InventorySnapshotApplyResult.Rejected;
                }
                return InventorySnapshotApplyResult.Unchanged;
            }

            _slots = (InventorySlot[])slots.Clone();
            Currency = currency;
            Revision = revision;
            HasSnapshot = true; // An initial empty revision zero is still a confirmed snapshot.
            return InventorySnapshotApplyResult.Applied;
        }

        internal void Reset()
        {
            Array.Clear(_slots, 0, _slots.Length);
            HasSnapshot = false;
            Revision = 0;
            Currency = 0;
        }
    }
}
