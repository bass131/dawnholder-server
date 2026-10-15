using System.Buffers.Binary;
using System.Text;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Independent oracle for the guild wire table of technical contract v1 (fixed goal copy
// .backups/verification/2026-10-14-guild-server/technical-contract-v1-goal.md, 「패킷 필드와 결과」):
// LittleEndian, a 4-byte header (ushort size including the header, ushort id), IDs 43..47 after
// the existing 1..42. Every constant is a literal from that table or the existing v17 economy
// table; nothing is read from PDL.xml, generated code or product helpers, so the guild tests
// cannot mirror the generator or the server's own calculation.
internal static class GuildWire
{
    // Existing v17 economy table, needed to read the personal state the guild actions consume.
    internal const ushort InventoryRequestId = 35;
    internal const ushort InventorySnapshotId = 36;
    internal const ushort ItemUseId = 37;
    internal const int InventorySnapshotLength = 76;
    internal const int InventorySlotCount = 8;

    internal const ushort GuildRequestId = 43;
    internal const ushort GuildSnapshotId = 44;
    internal const ushort StrongholdUpgradeId = 45;
    internal const ushort WeaponEnhanceId = 46;
    internal const ushort GuildActionResultId = 47;

    internal const int GuildRequestLength = 5;
    internal const int StrongholdUpgradeLength = 8;
    internal const int WeaponEnhanceLength = 8;
    internal const int GuildActionResultLength = 26;

    // 44 is 66 fixed bytes plus the UTF-16LE name; its UInt16 name byte length sits at offset 64.
    internal const int GuildSnapshotFixedLength = 66;
    internal const int GuildSnapshotNameLengthOffset = 64;

    // Default guild data the server sends, and the approved A limits and item IDs.
    internal const uint DefaultGuildId = 1;
    internal const string DefaultGuildName = "새벽 길드";
    internal const int MaxTreasury = 1_000_000_000;
    internal const int MaterialItemId = 1;
    internal const int CoinPouchItemId = 2;

    internal static byte[] GuildRequest(byte reserved = 0) => Frame(GuildRequestId, new[] { reserved });

    internal static byte[] StrongholdUpgrade(uint expectedGuildRevision)
        => Frame(StrongholdUpgradeId, UInt32Payload(expectedGuildRevision));

    internal static byte[] WeaponEnhance(uint expectedInventoryRevision)
        => Frame(WeaponEnhanceId, UInt32Payload(expectedInventoryRevision));

    internal static byte[] InventoryRequest() => Frame(InventoryRequestId, new byte[] { 0 });

    internal static byte[] ItemUse(int itemId, uint expectedRevision)
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), itemId);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), expectedRevision);
        return Frame(ItemUseId, payload);
    }

    // The size field matches the delivered length, as the framing layer guarantees on receive.
    internal static byte[] Frame(ushort id, byte[] payload)
    {
        byte[] packet = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), id);
        payload.CopyTo(packet, 4);
        return packet;
    }

    internal static ushort SizeOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(0, 2));

    internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

    // The expected revision a 45/46 request carried, for comparing with the 47 echo.
    internal static uint ReadUInt32Payload(byte[] request) => BinaryPrimitives.ReadUInt32LittleEndian(request.AsSpan(4, 4));

    // Validates the frame before reading any field, as the contract requires of every 44 reader:
    // minimum 66 bytes, header, an even name byte length of 2..32 at offset 64, and size = 66 + that length.
    internal static GuildSnapshot ReadSnapshot(byte[] packet)
    {
        Assert.Equal(GuildSnapshotId, IdOf(packet));
        Assert.True(packet.Length >= GuildSnapshotFixedLength, $"44 shorter than 66 bytes: {packet.Length}");
        Assert.Equal(packet.Length, SizeOf(packet));
        int nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(GuildSnapshotNameLengthOffset, 2));
        Assert.True(nameBytes is >= 2 and <= 32 && nameBytes % 2 == 0, $"44 name byte length out of contract: {nameBytes}");
        Assert.Equal(GuildSnapshotFixedLength + nameBytes, packet.Length);

        ReadOnlySpan<byte> s = packet;
        return new GuildSnapshot(
            Sequence: BinaryPrimitives.ReadInt64LittleEndian(s.Slice(4, 8)),
            ChangeKind: s[12],
            GuildId: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(13, 4)),
            GuildRevision: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(17, 4)),
            Treasury: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(21, 4)),
            StrongholdLevel: s[25],
            MaxStrongholdLevel: s[26],
            BlacksmithUnlocked: s[27],
            MemberCount: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(28, 4)),
            OnlineMemberCount: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(32, 4)),
            InventoryRevision: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(36, 4)),
            WeaponLevel: s[40],
            MaxWeaponLevel: s[41],
            AttackBonus: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(42, 4)),
            StrongholdCost: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(46, 4)),
            WeaponCurrencyCost: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(50, 4)),
            WeaponMaterialId: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(54, 4)),
            WeaponMaterialCount: BinaryPrimitives.ReadInt32LittleEndian(s.Slice(58, 4)),
            StrongholdAvailability: s[62],
            WeaponAvailability: s[63],
            GuildName: Encoding.Unicode.GetString(s.Slice(GuildSnapshotFixedLength, nameBytes)));
    }

    internal static GuildActionResult ReadActionResult(byte[] packet)
    {
        Assert.Equal(GuildActionResultId, IdOf(packet));
        Assert.Equal(GuildActionResultLength, packet.Length);
        Assert.Equal(GuildActionResultLength, SizeOf(packet));
        ReadOnlySpan<byte> s = packet;
        return new GuildActionResult(
            Action: s[4],
            Result: s[5],
            RequestedRevision: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(6, 4)),
            GuildRevision: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(10, 4)),
            InventoryRevision: BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(14, 4)),
            Sequence: BinaryPrimitives.ReadInt64LittleEndian(s.Slice(18, 8)));
    }

    // Existing 36: uint revision, int currency, then 8 × (int itemId, int count) in ItemId order.
    internal static InventorySnapshot ReadInventorySnapshot(byte[] packet)
    {
        Assert.Equal(InventorySnapshotId, IdOf(packet));
        Assert.Equal(InventorySnapshotLength, packet.Length);
        Assert.Equal(InventorySnapshotLength, SizeOf(packet));
        Dictionary<int, int> counts = new();
        for (int i = 0; i < InventorySlotCount; i++)
        {
            int offset = 12 + (i * 8);
            int itemId = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset, 4));
            int count = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset + 4, 4));
            if (itemId != 0) counts.Add(itemId, count);
        }
        return new InventorySnapshot(
            BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4)),
            counts);
    }

    // 「Snapshot의 범위」 of the contract, checked on every 44 the tests observe. Prices are the
    // approved A literals: stronghold 30 at level 1, weapon 20 + material 1 × 1 at level 0, 0 at max.
    internal static void AssertWithinContract(GuildSnapshot snapshot)
    {
        Assert.True(snapshot.Sequence >= 1, $"sequence starts at 1 per connection: {snapshot.Sequence}");
        Assert.InRange(snapshot.ChangeKind, ChangeKind.Query, ChangeKind.Presence);
        Assert.Equal(DefaultGuildId, snapshot.GuildId);
        Assert.Equal(DefaultGuildName, snapshot.GuildName);
        Assert.InRange(snapshot.Treasury, 0, MaxTreasury);
        Assert.InRange(snapshot.StrongholdLevel, (byte)1, (byte)2);
        Assert.Equal(2, snapshot.MaxStrongholdLevel);
        Assert.InRange(snapshot.BlacksmithUnlocked, (byte)0, (byte)1);
        Assert.Equal(snapshot.StrongholdLevel == 2, snapshot.BlacksmithUnlocked == 1);
        Assert.True(snapshot.OnlineMemberCount >= 0, "online member count is not negative");
        Assert.True(snapshot.MemberCount >= snapshot.OnlineMemberCount, "member count covers online members");
        Assert.InRange(snapshot.WeaponLevel, (byte)0, (byte)1);
        Assert.Equal(1, snapshot.MaxWeaponLevel);
        Assert.Equal(snapshot.WeaponLevel == 1 ? 5 : 0, snapshot.AttackBonus);
        Assert.Equal(snapshot.StrongholdLevel == 2 ? 0 : 30, snapshot.StrongholdCost);
        if (snapshot.WeaponLevel == 1)
        {
            Assert.Equal(0, snapshot.WeaponCurrencyCost);
            Assert.Equal(0, snapshot.WeaponMaterialCount);
        }
        else
        {
            Assert.Equal(20, snapshot.WeaponCurrencyCost);
            Assert.Equal(MaterialItemId, snapshot.WeaponMaterialId);
            Assert.Equal(1, snapshot.WeaponMaterialCount);
        }
        Assert.InRange(snapshot.StrongholdAvailability, GuildCode.Success, GuildCode.Unavailable);
        Assert.InRange(snapshot.WeaponAvailability, GuildCode.Success, GuildCode.Unavailable);
    }

    static byte[] UInt32Payload(uint value)
    {
        byte[] payload = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, value);
        return payload;
    }
}

// Shared result/availability codes; in a snapshot 0 means "possible now", never a success event.
internal static class GuildCode
{
    internal const byte Success = 0;
    internal const byte Stale = 1;
    internal const byte NotMember = 2;
    internal const byte NotInTown = 3;
    internal const byte Dead = 4;
    internal const byte FacilityLocked = 5;
    internal const byte MaxLevel = 6;
    internal const byte InsufficientCurrency = 7;
    internal const byte InsufficientMaterial = 8;
    internal const byte RevisionExhausted = 9;
    internal const byte Unavailable = 10;
}

internal static class GuildAction
{
    internal const byte StrongholdUpgrade = 1;
    internal const byte WeaponEnhance = 2;
}

internal static class ChangeKind
{
    internal const byte Query = 0;
    internal const byte Membership = 1;
    internal const byte TreasuryCredit = 2;
    internal const byte StrongholdUpgrade = 3;
    internal const byte WeaponEnhance = 4;
    internal const byte Presence = 5;
}

internal readonly record struct GuildSnapshot(
    long Sequence,
    byte ChangeKind,
    uint GuildId,
    uint GuildRevision,
    int Treasury,
    byte StrongholdLevel,
    byte MaxStrongholdLevel,
    byte BlacksmithUnlocked,
    int MemberCount,
    int OnlineMemberCount,
    uint InventoryRevision,
    byte WeaponLevel,
    byte MaxWeaponLevel,
    int AttackBonus,
    int StrongholdCost,
    int WeaponCurrencyCost,
    int WeaponMaterialId,
    int WeaponMaterialCount,
    byte StrongholdAvailability,
    byte WeaponAvailability,
    string GuildName);

internal readonly record struct GuildActionResult(
    byte Action,
    byte Result,
    uint RequestedRevision,
    uint GuildRevision,
    uint InventoryRevision,
    long Sequence);

internal sealed record InventorySnapshot(uint Revision, int Currency, IReadOnlyDictionary<int, int> Counts)
{
    internal int CountOf(int itemId) => Counts.TryGetValue(itemId, out int count) ? count : 0;
}
