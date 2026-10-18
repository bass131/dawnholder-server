#nullable enable
using System;
using System.Buffers.Binary;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Client.Network.Handlers.Guild
{
    /// <summary>Guild wire boundary. Generated Read is called only after the frame and string are bounded.</summary>
    internal static class GuildPacketReader
    {
        const int SnapshotFixedLength = 66;
        const int ResultLength = 26;
        const int MaxNameBytes = 32;
        const int MaxTreasury = 1_000_000_000;

        internal static bool TryReadSnapshot(ArraySegment<byte> buffer, out GuildSnapshotData? snapshot)
        {
            snapshot = null;
            if (!HasHeader(buffer, PacketID.S_GuildSnapshot, SnapshotFixedLength)) return false;
            var wire = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            int nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(wire.Slice(64));
            if (nameBytes < 2 || nameBytes > MaxNameBytes || (nameBytes & 1) != 0 ||
                wire.Length != SnapshotFixedLength + nameBytes ||
                !IsValidUtf16(wire.Slice(SnapshotFixedLength))) return false;

            var packet = new S_GuildSnapshot();
            packet.Read(buffer);
            if (!HasValidValues(packet)) return false;
            snapshot = new GuildSnapshotData(packet);
            return true;
        }

        internal static bool TryReadResult(ArraySegment<byte> buffer, out S_GuildActionResult? result)
        {
            result = null;
            if (!HasHeader(buffer, PacketID.S_GuildActionResult, ResultLength) || buffer.Count != ResultLength)
                return false;
            var packet = new S_GuildActionResult();
            packet.Read(buffer);
            if (packet.action < (byte)GuildAction.StrongholdUpgrade ||
                packet.action > (byte)GuildAction.WeaponEnhance ||
                !IsResultCode(packet.result) || packet.snapshotSequence <= 0) return false;
            // This new packet owns scalars only; no receive-buffer reference escapes into the queue.
            result = packet;
            return true;
        }

        static bool HasHeader(ArraySegment<byte> buffer, PacketID id, int minimumLength)
        {
            if (buffer.Array == null || buffer.Offset < 0 || buffer.Count < minimumLength ||
                buffer.Offset > buffer.Array.Length || buffer.Count > buffer.Array.Length - buffer.Offset)
                return false;
            var wire = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
            return BinaryPrimitives.ReadUInt16LittleEndian(wire) == buffer.Count &&
                BinaryPrimitives.ReadUInt16LittleEndian(wire.Slice(2)) == (ushort)id;
        }

        static bool HasValidValues(S_GuildSnapshot packet)
        {
            if (packet.snapshotSequence <= 0 || packet.changeKind > (byte)GuildChangeKind.Presence ||
                packet.treasury < 0 || packet.treasury > MaxTreasury ||
                packet.strongholdLevel < 1 || packet.strongholdLevel > 2 || packet.maxStrongholdLevel != 2 ||
                packet.blacksmithUnlocked > 1 || (packet.blacksmithUnlocked == 1) != (packet.strongholdLevel == 2) ||
                packet.onlineMemberCount < 0 || packet.memberCount < packet.onlineMemberCount ||
                packet.weaponLevel > 1 || packet.maxWeaponLevel != 1 ||
                (packet.weaponLevel == 0 ? packet.attackBonus != 0 : packet.attackBonus != 5)) return false;

            // Prices and availability are supplied by the server. Validate their representation and
            // terminal-level invariants, without reproducing the purchase rules or calculating prices.
            return packet.strongholdCost >= 0 && packet.weaponCurrencyCost >= 0 && packet.weaponMaterialCount >= 0 &&
                (packet.strongholdLevel != packet.maxStrongholdLevel || packet.strongholdCost == 0) &&
                (packet.weaponLevel != packet.maxWeaponLevel ||
                    (packet.weaponCurrencyCost == 0 && packet.weaponMaterialCount == 0)) &&
                packet.weaponMaterialId == (int)ItemId.Material &&
                IsResultCode(packet.strongholdAvailability) && IsResultCode(packet.weaponAvailability);
        }

        static bool IsResultCode(byte value) => value <= (byte)GuildResult.Unavailable;

        static bool IsValidUtf16(ReadOnlySpan<byte> bytes)
        {
            for (int offset = 0; offset < bytes.Length; offset += 2)
            {
                char unit = (char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset));
                if (char.IsLowSurrogate(unit)) return false;
                if (!char.IsHighSurrogate(unit)) continue;
                offset += 2;
                if (offset >= bytes.Length ||
                    !char.IsLowSurrogate((char)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset))))
                    return false;
            }
            return true;
        }
    }
}
