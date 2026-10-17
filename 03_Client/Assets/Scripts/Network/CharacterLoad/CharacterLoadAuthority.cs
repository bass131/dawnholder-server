using System;
using System.Buffers.Binary;
using Shared.Protocol;

namespace Dawnholder.Client.Network.CharacterLoad
{
    /// <summary>One connection's authority. Validation happens before any state or Unity mutation.</summary>
    public sealed class CharacterLoadAuthority
    {
        bool _closed;
        public CharacterLoadStatus? Status { get; private set; }
        public CharacterClass? LoadedClass { get; private set; }
        public bool IsSuccess => !_closed && Status == CharacterLoadStatus.Success && LoadedClass.HasValue;

        public static bool TryDecode(ArraySegment<byte> buffer, out CharacterLoadStatus status, out byte characterClass)
        {
            status = default;
            characterClass = byte.MaxValue;
            if (buffer.Array == null || buffer.Count != 6) return false;
            var bytes = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != 6 ||
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2)) != (ushort)PacketID.S_CharacterLoadResult ||
                bytes[4] > (byte)CharacterLoadStatus.Unavailable) return false;
            status = (CharacterLoadStatus)bytes[4];
            characterClass = bytes[5];
            return status == CharacterLoadStatus.Success
                ? characterClass == (byte)CharacterClass.Knight || characterClass == (byte)CharacterClass.Mage
                : characterClass == byte.MaxValue;
        }

        public bool TryApply(CharacterLoadStatus status, byte characterClass)
        {
            if (_closed || (byte)status > (byte)CharacterLoadStatus.Unavailable ||
                (status == CharacterLoadStatus.Success
                    ? characterClass > (byte)CharacterClass.Mage : characterClass != byte.MaxValue)) return false;
            if (Status.HasValue) return false; // Same result is idempotent; conflicting results never replace it.
            Status = status;
            LoadedClass = status == CharacterLoadStatus.Success ? (CharacterClass?)characterClass : null;
            return true;
        }

        public void Close()
        {
            _closed = true;
            Status = null;
            LoadedClass = null;
        }
    }
}
