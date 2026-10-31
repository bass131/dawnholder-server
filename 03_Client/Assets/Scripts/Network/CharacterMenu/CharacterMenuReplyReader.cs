using System;
using System.Buffers.Binary;
using Shared.Protocol;

namespace Dawnholder.Client.Network.CharacterMenu
{
    /// <summary>Strict 50/52/54 boundary from packet-contract-v1; generated Read does not validate malformed input.</summary>
    public static class CharacterMenuReplyReader
    {
        const int MaximumIdBytes = 72;
        const int MaximumNameBytes = 24;

        public static bool TryRead(ArraySegment<byte> frame, out CharacterMenuReply reply)
        {
            reply = null;
            if (frame.Array == null || frame.Count < 10 || frame.Count > 314) return false;
            var bytes = new ReadOnlySpan<byte>(frame.Array, frame.Offset, frame.Count);
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != bytes.Length) return false;
            ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2));
            int maximum;
            switch ((PacketID)packetId)
            {
                case PacketID.S_CharacterListResult: maximum = 314; break;
                case PacketID.S_CharacterCreateResult: maximum = 112; break;
                case PacketID.S_CharacterSelectResult: maximum = 84; break;
                default: return false;
            }
            if (bytes.Length > maximum) return false;
            uint requestId = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4));
            byte result = bytes[8];
            byte invalidField = bytes[9];
            if (requestId == 0 || result > (byte)CharacterMenuResult.LegacyFlowOnly ||
                invalidField > (byte)CharacterMenuInvalidField.CharacterId ||
                (result != (byte)CharacterMenuResult.InvalidInput && invalidField != 0)) return false;

            bool success = result == (byte)CharacterMenuResult.Success;
            int offset = 10;
            byte occupiedMask = 0;
            CharacterMenuCharacter[] slots = null;
            CharacterMenuCharacter character = null;
            string selectedId = string.Empty;
            switch ((PacketID)packetId)
            {
                case PacketID.S_CharacterListResult:
                    if (!TryReadByte(bytes, ref offset, out occupiedMask) || occupiedMask > 7 ||
                        (!success && occupiedMask != 0)) return false;
                    slots = new CharacterMenuCharacter[3];
                    for (int index = 0; index < slots.Length; index++)
                    {
                        bool occupied = (occupiedMask & (1 << index)) != 0;
                        if (!TryReadCharacter(bytes, ref offset, (byte)(index + 1), occupied, out slots[index]))
                            return false;
                        if (!occupied) continue;
                        for (int previous = 0; previous < index; previous++)
                        {
                            if (slots[previous] != null && slots[previous].CharacterIdD == slots[index].CharacterIdD)
                                return false;
                        }
                    }
                    break;
                case PacketID.S_CharacterCreateResult:
                    if (!TryReadByte(bytes, ref offset, out byte slot) ||
                        (success ? slot < 1 || slot > 3 : slot != 0) ||
                        !TryReadCharacter(bytes, ref offset, slot, success, out character)) return false;
                    break;
                case PacketID.S_CharacterSelectResult:
                    if (!TryReadText(bytes, ref offset, MaximumIdBytes, out selectedId) ||
                        (success ? !IsCanonicalId(selectedId) : selectedId.Length != 0)) return false;
                    break;
            }
            if (offset != bytes.Length) return false;
            reply = new CharacterMenuReply(packetId, requestId, result, invalidField,
                occupiedMask, slots, character, selectedId);
            return true;
        }

        // Shared only by the menu's outbound structure check and bounded inbound decoder, not Core name policy.
        internal static bool IsWellFormedUtf16(ReadOnlySpan<char> text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                char value = text[index];
                if (char.IsHighSurrogate(value))
                {
                    if (++index >= text.Length || !char.IsLowSurrogate(text[index])) return false;
                }
                else if (char.IsLowSurrogate(value)) return false;
            }
            return true;
        }

        static bool TryReadCharacter(ReadOnlySpan<byte> bytes, ref int offset, byte slot, bool occupied,
            out CharacterMenuCharacter character)
        {
            character = null;
            if (!TryReadText(bytes, ref offset, MaximumIdBytes, out string id) ||
                !TryReadText(bytes, ref offset, MaximumNameBytes, out string name) ||
                !TryReadByte(bytes, ref offset, out byte classId)) return false;
            if (!occupied) return id.Length == 0 && name.Length == 0 && classId == 0;
            if (!IsCanonicalId(id) || name.Length < 2 || name.Length > 12 || classId > 1) return false;
            character = new CharacterMenuCharacter(slot, id, name, classId);
            return true;
        }

        static bool IsCanonicalId(string text) => text.Length == 36 &&
            Guid.TryParseExact(text, "D", out Guid id) && id != Guid.Empty &&
            string.Equals(text, id.ToString("D"), StringComparison.Ordinal);

        static bool TryReadByte(ReadOnlySpan<byte> bytes, ref int offset, out byte value)
        {
            value = 0;
            if (offset >= bytes.Length) return false;
            value = bytes[offset++];
            return true;
        }

        static bool TryReadText(ReadOnlySpan<byte> bytes, ref int offset, int maximumBytes, out string text)
        {
            text = string.Empty;
            if (bytes.Length - offset < 2) return false;
            int byteCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset));
            offset += 2;
            if (byteCount > maximumBytes || (byteCount & 1) != 0 || byteCount > bytes.Length - offset)
                return false;

            // The wire prefix never sizes an allocation. Validate at most 36 code units before making a string.
            Span<char> characters = stackalloc char[MaximumIdBytes / 2];
            int units = byteCount / 2;
            for (int index = 0; index < units; index++)
                characters[index] = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset + index * 2));
            ReadOnlySpan<char> value = characters.Slice(0, units);
            if (!IsWellFormedUtf16(value)) return false;
            text = new string(value);
            offset += byteCount;
            return true;
        }
    }
}
