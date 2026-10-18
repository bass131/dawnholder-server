using System;
using System.Collections.Generic;

namespace Dawnholder.Client.Network.CharacterMenu
{
    /// <summary>Validated menu result. Only the reader constructs these values; preview class is not entry authority.</summary>
    public sealed class CharacterMenuReply
    {
        static readonly IReadOnlyList<CharacterMenuCharacter> s_emptySlots =
            Array.AsReadOnly(Array.Empty<CharacterMenuCharacter>());

        internal CharacterMenuReply(ushort packetId, uint requestId, byte result, byte invalidField,
            byte occupiedMask, CharacterMenuCharacter[] slots, CharacterMenuCharacter character,
            string selectedCharacterIdD)
        {
            PacketId = packetId;
            RequestId = requestId;
            Result = result;
            InvalidField = invalidField;
            OccupiedMask = occupiedMask;
            Slots = slots == null ? s_emptySlots : Array.AsReadOnly((CharacterMenuCharacter[])slots.Clone());
            Character = character;
            SelectedCharacterIdD = selectedCharacterIdD;
        }

        public ushort PacketId { get; }
        public uint RequestId { get; }
        public byte Result { get; }
        public byte InvalidField { get; }
        public byte OccupiedMask { get; }
        public IReadOnlyList<CharacterMenuCharacter> Slots { get; }
        public CharacterMenuCharacter Character { get; }
        public string SelectedCharacterIdD { get; }
    }

    public sealed class CharacterMenuCharacter
    {
        internal CharacterMenuCharacter(byte slot, string characterIdD, string name, byte classId)
        {
            Slot = slot;
            CharacterIdD = characterIdD;
            Name = name;
            ClassId = classId;
        }

        public byte Slot { get; }
        public string CharacterIdD { get; }
        public string Name { get; }
        public byte ClassId { get; }
    }
}
