using System.Buffers.Binary;
using System.Net;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Independent protocol checks for the economy packets: existing IDs keep the order of PDL.xml at
// origin/main 11aa4b8 (before this goal), IDs 35..38 follow with the fixed lengths and
// LittleEndian fields of the PR1 acceptance table, the dungeon goal's 39·40 come next (its P1), and
// login keeps 41·42, World reserves 43..47, character loading appends 48 and the character menu appends
// 49..54. Version 23 expands snapshot 36 to 24 slots and is enforced by the equality handshake. Expected bytes are literals, never
// produced by the generator under test.
[Collection("GameWorldRegistryTests")]
public sealed class InventoryProtocolContractTests : IDisposable
{
    static readonly string[] BasePacketOrder =
    {
        "C_Ping", "S_Pong", "S_EnterMap", "S_LeaveMap", "C_MoveIntent", "S_Snapshot", "C_Handshake",
        "S_HandshakeResult", "S_PlayerJoin", "S_PlayerLeave", "C_Attack", "S_EntitySpawn", "S_HitResult",
        "S_EntityDeath", "S_StageClear", "C_CharacterSelect", "C_EnterPortal", "S_MapTransition",
        "S_EntityState", "S_EnemyAttack", "S_PlayerHp", "S_PlayerAttack", "S_ProjectileLaunch", "C_SkillUse",
        "S_SkillCast", "C_PartyInvite", "C_PartyRespond", "C_PartyLeave", "S_PartyInviteRecv", "S_PartyUpdate",
        "S_PartyError", "S_QuestUpdate", "S_PortalLocked", "C_CheatCommand",
    };

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());

    public void Dispose() => _world.Stop();

    [Fact]
    public void ExistingPacketIds_KeepTheBaseOrder_AndEconomyIdsAreAppended()
    {
        Dictionary<string, int> actual = Enum.GetValues<PacketID>().ToDictionary(id => id.ToString(), id => (int)id);
        string[] appended = { "C_InventoryRequest", "S_InventorySnapshot", "C_ItemUse", "S_ItemUseResult" };
        // Looked up by name: a direct PacketID member would not compile before the generator runs.
        string[] dungeon = { "S_DungeonClearResult", "C_DungeonClearConfirm" };

        Assert.Equal(54, actual.Count);
        for (int i = 0; i < BasePacketOrder.Length; i++)
        {
            Assert.Equal(i + 1, actual[BasePacketOrder[i]]);
        }
        for (int i = 0; i < appended.Length; i++)
        {
            Assert.Equal(35 + i, actual[appended[i]]);
        }
        for (int i = 0; i < dungeon.Length; i++)
        {
            Assert.True(actual.TryGetValue(dungeon[i], out int id), $"dungeon goal P1: {dungeon[i]} is appended");
            Assert.Equal(39 + i, id);
        }
        Assert.Equal(41, actual["C_Login"]);
        Assert.Equal(42, actual["S_LoginResult"]);
        Assert.Equal(43, actual["C_GuildRequest"]);
        Assert.Equal(44, actual["S_GuildSnapshot"]);
        Assert.Equal(45, actual["C_StrongholdUpgrade"]);
        Assert.Equal(46, actual["C_WeaponEnhance"]);
        Assert.Equal(47, actual["S_GuildActionResult"]);
        Assert.Equal(48, actual["S_CharacterLoadResult"]);
    }

    [Fact]
    public void EconomyPackets_WriteTheFixedLittleEndianBytes()
    {
        byte[] request = { 0x05, 0x00, 0x23, 0x00, 0x00 };
        byte[] use = { 0x0C, 0x00, 0x25, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x03, 0x02, 0x01 };
        byte[] result = { 0x0D, 0x00, 0x26, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0xD4, 0xC3, 0xB2, 0xA1 };
        byte[] snapshot = new byte[204];
        new byte[]
        {
            0xCC, 0x00, 0x24, 0x00,
            0x44, 0x33, 0x22, 0x11,
            0x00, 0xCA, 0x9A, 0x3B,
            0x01, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
        }.CopyTo(snapshot, 0);
        new byte[] { 0x04, 0x03, 0x02, 0x01, 0x0D, 0x0C, 0x0B, 0x0A }.CopyTo(snapshot, 68);

        byte[] writtenRequest = new C_InventoryRequest { reserved = 0 }.Write().ToArray();
        byte[] writtenUse = new C_ItemUse { itemId = 2, expectedRevision = 0x01020304u }.Write().ToArray();
        byte[] writtenResult = new S_ItemUseResult { result = 6, itemId = 2, revision = 0xA1B2C3D4u }.Write().ToArray();
        byte[] writtenSnapshot = new S_InventorySnapshot
        {
            revision = 0x11223344u,
            currency = 1_000_000_000,
            slot0ItemId = 1,
            slot0Count = 99,
            slot1ItemId = 2,
            slot1Count = 1,
            slot7ItemId = 0x01020304,
            slot7Count = 0x0A0B0C0D,
        }.Write().ToArray();

        Assert.Equal(request, writtenRequest);
        Assert.Equal(use, writtenUse);
        Assert.Equal(result, writtenResult);
        Assert.Equal(snapshot, writtenSnapshot);
    }

    [Fact]
    public void InventorySnapshot_TwoOccupiedSlots_Writes204BytesAndZeroesSlots2Through23()
    {
        InventoryState state = new(0x11223344u, 123,
            new[] { new InventorySlot(ItemId.Material, 7), new InventorySlot(ItemId.CoinPouch, 9) });

        byte[] wire = InventoryPackets.Snapshot(state).ToArray();

        Assert.Equal(204, wire.Length);
        Assert.Equal(204, BinaryPrimitives.ReadUInt16LittleEndian(wire));
        Assert.Equal(36, BinaryPrimitives.ReadUInt16LittleEndian(wire.AsSpan(2)));
        Assert.Equal(0x11223344u, BinaryPrimitives.ReadUInt32LittleEndian(wire.AsSpan(4)));
        Assert.Equal(123, BinaryPrimitives.ReadInt32LittleEndian(wire.AsSpan(8)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(wire.AsSpan(12)));
        Assert.Equal(7, BinaryPrimitives.ReadInt32LittleEndian(wire.AsSpan(16)));
        Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(wire.AsSpan(20)));
        Assert.Equal(9, BinaryPrimitives.ReadInt32LittleEndian(wire.AsSpan(24)));
        Assert.All(wire.Skip(28), value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public void InventorySnapshot_Handwritten204Bytes_ReadsDistinctValuesInSlots8Through23()
    {
        // Independent v23 wire table: header4 + revision4 + currency4 + 24*(item4 + count4).
        // Slots8..23 begin at byte76. Undefined item IDs are intentional serializer probes.
        byte[] wire = new byte[204];
        new byte[] { 0xCC, 0, 0x24, 0 }.CopyTo(wire, 0);
        new byte[]
        {
            8, 0, 0, 0, 108, 0, 0, 0,
            9, 0, 0, 0, 109, 0, 0, 0,
            10, 0, 0, 0, 110, 0, 0, 0,
            11, 0, 0, 0, 111, 0, 0, 0,
            12, 0, 0, 0, 112, 0, 0, 0,
            13, 0, 0, 0, 113, 0, 0, 0,
            14, 0, 0, 0, 114, 0, 0, 0,
            15, 0, 0, 0, 115, 0, 0, 0,
            16, 0, 0, 0, 116, 0, 0, 0,
            17, 0, 0, 0, 117, 0, 0, 0,
            18, 0, 0, 0, 118, 0, 0, 0,
            19, 0, 0, 0, 119, 0, 0, 0,
            20, 0, 0, 0, 120, 0, 0, 0,
            21, 0, 0, 0, 121, 0, 0, 0,
            22, 0, 0, 0, 122, 0, 0, 0,
            23, 0, 0, 0, 123, 0, 0, 0,
        }.CopyTo(wire, 76);

        S_InventorySnapshot decoded = new();
        decoded.Read(new ArraySegment<byte>(wire));

        Assert.Equal(8, decoded.slot8ItemId);
        Assert.Equal(108, decoded.slot8Count);
        Assert.Equal(9, decoded.slot9ItemId);
        Assert.Equal(109, decoded.slot9Count);
        Assert.Equal(10, decoded.slot10ItemId);
        Assert.Equal(110, decoded.slot10Count);
        Assert.Equal(11, decoded.slot11ItemId);
        Assert.Equal(111, decoded.slot11Count);
        Assert.Equal(12, decoded.slot12ItemId);
        Assert.Equal(112, decoded.slot12Count);
        Assert.Equal(13, decoded.slot13ItemId);
        Assert.Equal(113, decoded.slot13Count);
        Assert.Equal(14, decoded.slot14ItemId);
        Assert.Equal(114, decoded.slot14Count);
        Assert.Equal(15, decoded.slot15ItemId);
        Assert.Equal(115, decoded.slot15Count);
        Assert.Equal(16, decoded.slot16ItemId);
        Assert.Equal(116, decoded.slot16Count);
        Assert.Equal(17, decoded.slot17ItemId);
        Assert.Equal(117, decoded.slot17Count);
        Assert.Equal(18, decoded.slot18ItemId);
        Assert.Equal(118, decoded.slot18Count);
        Assert.Equal(19, decoded.slot19ItemId);
        Assert.Equal(119, decoded.slot19Count);
        Assert.Equal(20, decoded.slot20ItemId);
        Assert.Equal(120, decoded.slot20Count);
        Assert.Equal(21, decoded.slot21ItemId);
        Assert.Equal(121, decoded.slot21Count);
        Assert.Equal(22, decoded.slot22ItemId);
        Assert.Equal(122, decoded.slot22Count);
        Assert.Equal(23, decoded.slot23ItemId);
        Assert.Equal(123, decoded.slot23Count);
    }

    [Fact]
    public void ProtocolVersion_IsTwentyThree_AndTheHandshakeRejectsTwentyTwo()
    {
        ProbeSession previous = new(_world);
        ProbeSession current = new(_world);
        previous.OnConnected(Endpoint);
        current.OnConnected(Endpoint);

        previous.OnRecvPacket(new C_Handshake { clientVersion = 22 }.Write());
        current.OnRecvPacket(new C_Handshake { clientVersion = 23 }.Write());
        S_HandshakeResult rejected = ReadSingle<S_HandshakeResult>(previous);
        S_HandshakeResult accepted = ReadSingle<S_HandshakeResult>(current);

        Assert.Equal(23, ProtocolVersion.Current);
        Assert.False(rejected.ok);
        Assert.Equal(23, rejected.serverVersion);
        Assert.True(previous.IsClosing);
        Assert.True(accepted.ok);
        Assert.False(current.IsClosing);
    }

    static T ReadSingle<T>(ProbeSession session) where T : IPacket, new()
    {
        ushort id = new T().Protocol;
        byte[] bytes = Assert.Single(session.Packets, packet => BitConverter.ToUInt16(packet, 2) == id);
        T packet = new();
        packet.Read(new ArraySegment<byte>(bytes));
        return packet;
    }

    sealed class ProbeSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();

        internal ProbeSession(GameWorld world) : base(world) { }

        public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

        public override void Disconnect() => OnDisconnected(Endpoint);
    }
}
