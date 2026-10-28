using System.Reflection;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// I9 / G7 (technical contract v1, 「패킷 필드와 결과」): packets 1..42 keep their names and numbers, the
// five guild packets stay at 43..47, followed by the admission result at 48 and the character menu packets
// at 49..54, and writers/readers use the fixed LittleEndian layout. The new types are looked up by name
// through reflection, so this file compiles before the generator runs and a missing type is a failing
// assertion rather than a build break. Expected bytes are literals. Version 22 and the handshake rejection
// of older clients are owned by InventoryProtocolContractTests and BossBehaviorTests; the server's own
// 44/47 output is checked byte by byte through GuildWire in the behaviour tests of this folder.
public sealed class GuildProtocolContractTests
{
    static readonly string[] ExistingOrder =
    {
        "C_Ping", "S_Pong", "S_EnterMap", "S_LeaveMap", "C_MoveIntent", "S_Snapshot", "C_Handshake",
        "S_HandshakeResult", "S_PlayerJoin", "S_PlayerLeave", "C_Attack", "S_EntitySpawn", "S_HitResult",
        "S_EntityDeath", "S_StageClear", "C_CharacterSelect", "C_EnterPortal", "S_MapTransition",
        "S_EntityState", "S_EnemyAttack", "S_PlayerHp", "S_PlayerAttack", "S_ProjectileLaunch", "C_SkillUse",
        "S_SkillCast", "C_PartyInvite", "C_PartyRespond", "C_PartyLeave", "S_PartyInviteRecv", "S_PartyUpdate",
        "S_PartyError", "S_QuestUpdate", "S_PortalLocked", "C_CheatCommand", "C_InventoryRequest",
        "S_InventorySnapshot", "C_ItemUse", "S_ItemUseResult", "S_DungeonClearResult", "C_DungeonClearConfirm",
        "C_Login", "S_LoginResult",
    };

    static readonly string[] GuildOrder =
    {
        "C_GuildRequest", "S_GuildSnapshot", "C_StrongholdUpgrade", "C_WeaponEnhance", "S_GuildActionResult",
    };

    // Protective: passes before the change and must keep passing after it.
    [Fact]
    public void ExistingPacketIds_OneToFortyTwo_KeepTheirNamesAndNumbers()
    {
        Dictionary<string, int> actual = Enum.GetValues<PacketID>().ToDictionary(id => id.ToString(), id => (int)id);

        for (int i = 0; i < ExistingOrder.Length; i++)
        {
            Assert.True(actual.TryGetValue(ExistingOrder[i], out int id), $"{ExistingOrder[i]} must stay");
            Assert.Equal(i + 1, id);
        }
    }

    [Fact]
    public void GuildPacketIds_StayFortyThreeToFortySeven_BeforeCharacterLoadResult48()
    {
        Dictionary<string, int> actual = Enum.GetValues<PacketID>().ToDictionary(id => id.ToString(), id => (int)id);

        for (int i = 0; i < GuildOrder.Length; i++)
        {
            Assert.True(actual.TryGetValue(GuildOrder[i], out int id), $"{GuildOrder[i]} is appended");
            Assert.Equal(43 + i, id);
        }
        Assert.True(actual.TryGetValue("S_CharacterLoadResult", out int loadResultId));
        Assert.Equal(48, loadResultId);
        Assert.Equal(54, actual.Values.Max());
        Assert.Equal(54, actual.Count);
    }

    [Fact]
    public void GeneratedRequests_WriteTheFixedBytes()
    {
        IPacket query = NewPacket("C_GuildRequest", ("reserved", 0));
        IPacket upgrade = NewPacket("C_StrongholdUpgrade", ("expectedGuildRevision", 0x01020304u));
        IPacket enhance = NewPacket("C_WeaponEnhance", ("expectedInventoryRevision", 0xA1B2C3D4u));

        Assert.Equal(new byte[] { 0x05, 0x00, 0x2B, 0x00, 0x00 }, query.Write().ToArray());
        Assert.Equal(new byte[] { 0x08, 0x00, 0x2D, 0x00, 0x04, 0x03, 0x02, 0x01 }, upgrade.Write().ToArray());
        Assert.Equal(new byte[] { 0x08, 0x00, 0x2E, 0x00, 0xD4, 0xC3, 0xB2, 0xA1 }, enhance.Write().ToArray());
    }

    [Fact]
    public void GeneratedActionResult_WritesTheFixedBytes()
    {
        IPacket result = NewPacket(
            "S_GuildActionResult",
            ("action", 2),
            ("result", 9),
            ("requestedRevision", 0x11223344u),
            ("guildRevision", 0x55667788u),
            ("inventoryRevision", 0x99AABBCCu),
            ("snapshotSequence", 0x0102030405060708L));

        byte[] expected =
        {
            0x1A, 0x00, 0x2F, 0x00,
            0x02, 0x09,
            0x44, 0x33, 0x22, 0x11,
            0x88, 0x77, 0x66, 0x55,
            0xCC, 0xBB, 0xAA, 0x99,
            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
        };
        Assert.Equal(expected, result.Write().ToArray());
    }

    // The default name 「새벽 길드」 is 5 UTF-16 code units, so the whole packet is 66 + 10 = 76 bytes.
    [Fact]
    public void GeneratedSnapshot_WritesTheFixedBytesWithTheDefaultName()
    {
        IPacket snapshot = NewPacket("S_GuildSnapshot", SnapshotFields());

        byte[] expected =
        {
            0x4C, 0x00, 0x2C, 0x00,
            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, // snapshotSequence
            0x03,                                           // changeKind
            0x01, 0x00, 0x00, 0x00,                         // guildId
            0x2A, 0x00, 0x00, 0x00,                         // guildRevision
            0x00, 0xCA, 0x9A, 0x3B,                         // treasury 1,000,000,000
            0x02, 0x02, 0x01,                               // stronghold, max, blacksmith
            0x06, 0x00, 0x00, 0x00,                         // memberCount
            0x05, 0x00, 0x00, 0x00,                         // onlineMemberCount
            0xD4, 0xC3, 0xB2, 0xA1,                         // inventoryRevision
            0x00, 0x01,                                     // weaponLevel, maxWeaponLevel
            0x00, 0x00, 0x00, 0x00,                         // attackBonus
            0x00, 0x00, 0x00, 0x00,                         // strongholdCost
            0x14, 0x00, 0x00, 0x00,                         // weaponCurrencyCost
            0x01, 0x00, 0x00, 0x00,                         // weaponMaterialId
            0x01, 0x00, 0x00, 0x00,                         // weaponMaterialCount
            0x06, 0x00,                                     // availabilities
            0x0A, 0x00,                                     // name byte length at offset 64
            0xC8, 0xC0, 0xBD, 0xBC, 0x20, 0x00, 0x38, 0xAE, 0xDC, 0xB4,
        };
        Assert.Equal(expected, snapshot.Write().ToArray());
    }

    [Fact]
    public void GeneratedGuildPackets_ReadBackEveryWrittenField()
    {
        AssertRoundTrip("C_GuildRequest", ("reserved", 0));
        AssertRoundTrip("C_StrongholdUpgrade", ("expectedGuildRevision", 0xFFFFFFFEu));
        AssertRoundTrip("C_WeaponEnhance", ("expectedInventoryRevision", 7u));
        AssertRoundTrip(
            "S_GuildActionResult",
            ("action", 1),
            ("result", 10),
            ("requestedRevision", 3u),
            ("guildRevision", 4u),
            ("inventoryRevision", 5u),
            ("snapshotSequence", long.MaxValue));
        AssertRoundTrip("S_GuildSnapshot", SnapshotFields());
    }

    static (string Field, object Value)[] SnapshotFields() => new (string Field, object Value)[]
    {
        ("snapshotSequence", 0x0102030405060708L),
        ("changeKind", 3),
        ("guildId", 1u),
        ("guildRevision", 42u),
        ("treasury", 1_000_000_000),
        ("strongholdLevel", 2),
        ("maxStrongholdLevel", 2),
        ("blacksmithUnlocked", 1),
        ("memberCount", 6),
        ("onlineMemberCount", 5),
        ("inventoryRevision", 0xA1B2C3D4u),
        ("weaponLevel", 0),
        ("maxWeaponLevel", 1),
        ("attackBonus", 0),
        ("strongholdCost", 0),
        ("weaponCurrencyCost", 20),
        ("weaponMaterialId", 1),
        ("weaponMaterialCount", 1),
        ("strongholdAvailability", 6),
        ("weaponAvailability", 0),
        ("guildName", "새벽 길드"),
    };

    static void AssertRoundTrip(string typeName, params (string Field, object Value)[] fields)
    {
        IPacket written = NewPacket(typeName, fields);
        IPacket read = NewPacket(typeName);

        read.Read(written.Write());

        foreach ((string field, object value) in fields)
        {
            FieldInfo info = FieldOf(read, field);
            Assert.Equal(Convert.ChangeType(value, info.FieldType), info.GetValue(read));
        }
    }

    // Generated packets are public classes with public fields in Shared.Protocol.
    static IPacket NewPacket(string typeName, params (string Field, object Value)[] fields)
    {
        Type? type = typeof(PacketID).Assembly.GetType($"Shared.Protocol.{typeName}");
        Assert.True(type != null, $"generated type Shared.Protocol.{typeName} is missing");
        IPacket packet = (IPacket)Activator.CreateInstance(type!)!;
        foreach ((string field, object value) in fields)
        {
            FieldInfo info = FieldOf(packet, field);
            info.SetValue(packet, Convert.ChangeType(value, info.FieldType));
        }
        return packet;
    }

    static FieldInfo FieldOf(IPacket packet, string field)
    {
        FieldInfo? info = packet.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(info != null, $"{packet.GetType().Name}.{field} is missing");
        return info!;
    }
}
