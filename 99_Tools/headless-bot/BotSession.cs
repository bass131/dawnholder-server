using System.Net;
using System.Buffers.Binary;
using System.Text;
using Dawnholder.Client.Net;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot;

// **상속 의도** (ADR-012 Y2 정합): 04_ClientNet의 PacketSession을 그대로 받음 →
// 프레이밍·SendBuffer·RecvBuffer는 클라와 100% 동일 코드. 봇은 게임 로직만 다름.
public class BotSession : PacketSession
{
    static readonly Encoding StrictUnicode = new UnicodeEncoding(false, false, true);
    public Action<EndPoint>? OnConnectedCallback;
    public Action<EndPoint>? OnDisconnectedCallback;
    public Action<ArraySegment<byte>>? OnPacketCallback;
    readonly object _loadGate = new();
    (CharacterLoadStatus Status, CharacterClass? LoadedClass)? _characterLoad;
    public (CharacterLoadStatus Status, CharacterClass? LoadedClass)? CharacterLoad
    {
        get { lock (_loadGate) return _characterLoad; }
    }

    public override void OnConnected(EndPoint endPoint)
    {
        OnConnectedCallback?.Invoke(endPoint);
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        OnDisconnectedCallback?.Invoke(endPoint);
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        if (TryReadCharacterLoad(buffer, out var load))
        {
            lock (_loadGate)
                _characterLoad ??= load;
        }
        OnPacketCallback?.Invoke(buffer);
    }

    public override void OnSend(int numOfBytes)
    {
    }

    internal static bool TryReadCharacterLoad(ArraySegment<byte> buffer,
        out (CharacterLoadStatus Status, CharacterClass? LoadedClass) load)
    {
        load = default;
        if (buffer.Array is null || buffer.Count != 6) return false;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes) != 6 ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2)) != (ushort)PacketID.S_CharacterLoadResult ||
            bytes[4] > (byte)CharacterLoadStatus.Unavailable ||
            (bytes[4] == (byte)CharacterLoadStatus.Success
                ? bytes[5] > (byte)CharacterClass.Mage : bytes[5] != 255)) return false;
        load = ((CharacterLoadStatus)bytes[4],
            bytes[4] == (byte)CharacterLoadStatus.Success ? (CharacterClass?)bytes[5] : null);
        return true;
    }

    // Generated Read trusts string lengths. Bound every field before decoding any 50/54 payload.
    internal static bool TryReadCharacterList(ArraySegment<byte> buffer, out S_CharacterListResult packet)
    {
        packet = new();
        if (!HasMenuHeader(buffer, PacketID.S_CharacterListResult, 26, 314)) return false;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        if (bytes[10] > 7 || (bytes[8] != (byte)CharacterMenuResult.Success && bytes[10] != 0))
            return false;
        int offset = 11;
        for (int slot = 0; slot < 3; slot++)
        {
            bool occupied = (bytes[10] & (1 << slot)) != 0;
            if (!SkipMenuText(bytes, ref offset, occupied ? 72 : 0) ||
                !SkipMenuText(bytes, ref offset, occupied ? 24 : 0) ||
                offset >= bytes.Length) return false;
            if (bytes[offset] > (byte)CharacterClass.Mage || (!occupied && bytes[offset] != 0)) return false;
            offset++;
        }
        if (offset != bytes.Length) return false;
        packet.Read(buffer);
        string[] ids = [packet.slot1CharacterIdD, packet.slot2CharacterIdD, packet.slot3CharacterIdD];
        string[] names = [packet.slot1Name, packet.slot2Name, packet.slot3Name];
        byte[] classes = [packet.slot1ClassId, packet.slot2ClassId, packet.slot3ClassId];
        for (int slot = 0; slot < 3; slot++)
        {
            bool occupied = (packet.occupiedMask & (1 << slot)) != 0;
            if (!occupied)
            {
                if (ids[slot].Length != 0 || names[slot].Length != 0 || classes[slot] != 0) return false;
                continue;
            }
            if (!IsCanonicalCharacterId(ids[slot]) || names[slot].Length is < 2 or > 12 ||
                classes[slot] > (byte)CharacterClass.Mage) return false;
            for (int previous = 0; previous < slot; previous++)
            {
                if (ids[previous] == ids[slot]) return false;
            }
        }
        return true;
    }

    // A refusal carries slot 0, two empty strings and class 0; success carries one canonical created summary.
    internal static bool TryReadCharacterCreate(ArraySegment<byte> buffer, out S_CharacterCreateResult packet)
    {
        packet = new();
        if (!HasMenuHeader(buffer, PacketID.S_CharacterCreateResult, 16, 112)) return false;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        bool success = bytes[8] == (byte)CharacterMenuResult.Success;
        int offset = 11;
        if (!SkipMenuText(bytes, ref offset, success ? 72 : 0) ||
            !SkipMenuText(bytes, ref offset, success ? 24 : 0) ||
            offset != bytes.Length - 1) return false;
        packet.Read(buffer);
        return success
            ? packet.slot is >= 1 and <= 3 && IsCanonicalCharacterId(packet.characterIdD) &&
                packet.name.Length is >= 2 and <= 12 && packet.classId <= (byte)CharacterClass.Mage
            : packet.slot == 0 && packet.classId == 0;
    }

    internal static bool TryReadCharacterSelection(ArraySegment<byte> buffer, out S_CharacterSelectResult packet)
    {
        packet = new();
        if (!HasMenuHeader(buffer, PacketID.S_CharacterSelectResult, 12, 84)) return false;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        int offset = 10;
        if (bytes.Length != (bytes[8] == (byte)CharacterMenuResult.Success ? 84 : 12)) return false;
        if (!SkipMenuText(bytes, ref offset, 72) || offset != bytes.Length) return false;
        packet.Read(buffer);
        return packet.result == (byte)CharacterMenuResult.Success
            ? IsCanonicalCharacterId(packet.selectedCharacterIdD) : packet.selectedCharacterIdD.Length == 0;
    }

    internal static bool IsCanonicalCharacterId(string text) =>
        Guid.TryParseExact(text, "D", out Guid id) && id != Guid.Empty && text == id.ToString("D");

    static bool HasMenuHeader(ArraySegment<byte> buffer, PacketID id, int minimumSize, int maximumSize)
    {
        if (buffer.Array is null || buffer.Count < minimumSize || buffer.Count > maximumSize) return false;
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        return BinaryPrimitives.ReadUInt16LittleEndian(bytes) == bytes.Length &&
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2)) == (ushort)id &&
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4)) != 0 &&
            bytes[8] <= (byte)CharacterMenuResult.LegacyFlowOnly &&
            bytes[9] <= (byte)CharacterMenuInvalidField.CharacterId &&
            (bytes[8] == (byte)CharacterMenuResult.InvalidInput || bytes[9] == 0);
    }

    static bool SkipMenuText(ReadOnlySpan<byte> bytes, ref int offset, int maximumBytes)
    {
        if (bytes.Length - offset < 2) return false;
        int byteCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset));
        offset += 2;
        if (byteCount > maximumBytes || byteCount % 2 != 0 || byteCount > bytes.Length - offset)
            return false;
        try
        {
            // Reject malformed UTF-16 rather than letting the generated reader replace it silently.
            StrictUnicode.GetCharCount(bytes.Slice(offset, byteCount));
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
        offset += byteCount;
        return true;
    }
}
