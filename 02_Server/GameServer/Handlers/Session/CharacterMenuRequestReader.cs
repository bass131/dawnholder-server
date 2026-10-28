using System.Buffers.Binary;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Handlers.Session;

// Validate the entire offset-relative frame before queuing any request. Generated Read trusts
// string prefixes and replaces malformed UTF-16, so it cannot establish this trust boundary.
internal static class CharacterMenuRequestReader
{
    const int MaxTextBytes = 72;

    internal static bool TryRead(ArraySegment<byte> buffer, PacketID expectedId, out uint requestId)
    {
        requestId = 0;
        ReadOnlySpan<byte> frame = buffer.AsSpan();
        int minimumLength;
        int reservedOffset;
        switch (expectedId)
        {
            case PacketID.C_CharacterListRequest:
                minimumLength = 9;
                reservedOffset = 8;
                break;
            case PacketID.C_CharacterCreateRequest:
                minimumLength = 13;
                reservedOffset = 10;
                break;
            case PacketID.C_CharacterSelectRequest:
                minimumLength = 11;
                reservedOffset = 8;
                break;
            default:
                return false;
        }

        if (frame.Length < minimumLength ||
            BinaryPrimitives.ReadUInt16LittleEndian(frame) != frame.Length ||
            BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(2, 2)) != (ushort)expectedId ||
            frame[reservedOffset] != 0)
        {
            return false;
        }

        if (expectedId == PacketID.C_CharacterListRequest)
        {
            if (frame.Length != minimumLength) return false;
        }
        else if (!HasExactUtf16Text(frame, reservedOffset + 1))
        {
            return false;
        }

        // Current menu processing is state/Legacy guidance only: validate text without
        // allocating it or treating slot/class/name/ID values as business input before policy.
        requestId = BinaryPrimitives.ReadUInt32LittleEndian(frame.Slice(4, 4));
        return requestId != 0;
    }

    static bool HasExactUtf16Text(ReadOnlySpan<byte> frame, int prefixOffset)
    {
        int byteCount = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(prefixOffset, 2));
        int textOffset = prefixOffset + 2;
        if (byteCount > MaxTextBytes || byteCount % 2 != 0 || frame.Length - textOffset != byteCount)
            return false;

        ReadOnlySpan<byte> text = frame.Slice(textOffset, byteCount);
        for (int offset = 0; offset < text.Length; offset += 2)
        {
            char unit = (char)BinaryPrimitives.ReadUInt16LittleEndian(text.Slice(offset, 2));
            if (char.IsLowSurrogate(unit)) return false;
            if (!char.IsHighSurrogate(unit)) continue;
            offset += 2;
            if (offset >= text.Length ||
                !char.IsLowSurrogate((char)BinaryPrimitives.ReadUInt16LittleEndian(text.Slice(offset, 2))))
            {
                return false;
            }
        }
        return true;
    }
}
