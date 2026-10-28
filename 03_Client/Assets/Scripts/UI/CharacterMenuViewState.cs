#nullable enable
using System;

namespace Dawnholder.Client.UI
{
    /// <summary>Display input only. CharacterMenuPresenter owns the current client, character IDs and reply interpretation.</summary>
    internal sealed class CharacterMenuViewState
    {
        readonly Slot?[] _slots;

        internal CharacterMenuViewState(Phase phase, Slot? slot1 = null, Slot? slot2 = null,
            Slot? slot3 = null, string message = "")
        {
            CurrentPhase = phase;
            _slots = new[] { slot1, slot2, slot3 };
            Message = message ?? string.Empty;
        }

        internal Phase CurrentPhase { get; }

        // Supply localized user-facing text, never server diagnostics. A result must name its original slot.
        internal string Message { get; }

        internal Slot? GetSlot(byte slot)
        {
            if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return _slots[slot - 1];
        }

        internal enum Phase
        {
            Connecting,
            LoadingList,
            Ready,
            Creating,
            Selecting,
            ListFailed,
            Entering,
            Disconnected,
        }

        internal sealed class Slot
        {
            internal Slot(string name, byte classId)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                ClassId = classId;
            }

            internal string Name { get; }

            internal byte ClassId { get; }
        }
    }
}
