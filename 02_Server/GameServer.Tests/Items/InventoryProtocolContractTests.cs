using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Independent protocol checks for the economy packets: existing IDs keep the order of PDL.xml at
// origin/main 11aa4b8 (before this goal), IDs 35..38 follow with the fixed lengths and
// LittleEndian fields of the PR1 acceptance table, and version 17 is enforced by the existing
// equality handshake. Expected bytes are literals, never produced by the generator under test.
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

        Assert.Equal(38, actual.Count);
        for (int i = 0; i < BasePacketOrder.Length; i++)
        {
            Assert.Equal(i + 1, actual[BasePacketOrder[i]]);
        }
        for (int i = 0; i < appended.Length; i++)
        {
            Assert.Equal(35 + i, actual[appended[i]]);
        }
    }

    [Fact]
    public void EconomyPackets_WriteTheFixedLittleEndianBytes()
    {
        byte[] request = { 0x05, 0x00, 0x23, 0x00, 0x00 };
        byte[] use = { 0x0C, 0x00, 0x25, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x03, 0x02, 0x01 };
        byte[] result = { 0x0D, 0x00, 0x26, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0xD4, 0xC3, 0xB2, 0xA1 };
        byte[] snapshot = new byte[76];
        new byte[]
        {
            0x4C, 0x00, 0x24, 0x00,
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
    public void ProtocolVersion_IsSeventeen_AndTheHandshakeRejectsSixteen()
    {
        ProbeSession previous = new(_world);
        ProbeSession current = new(_world);
        previous.OnConnected(Endpoint);
        current.OnConnected(Endpoint);

        previous.OnRecvPacket(new C_Handshake { clientVersion = 16 }.Write());
        current.OnRecvPacket(new C_Handshake { clientVersion = 17 }.Write());
        S_HandshakeResult rejected = ReadSingle<S_HandshakeResult>(previous);
        S_HandshakeResult accepted = ReadSingle<S_HandshakeResult>(current);

        Assert.Equal(17, ProtocolVersion.Current);
        Assert.False(rejected.ok);
        Assert.Equal(17, rejected.serverVersion);
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
