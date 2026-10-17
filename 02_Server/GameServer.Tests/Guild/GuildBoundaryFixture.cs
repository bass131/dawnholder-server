using System.Collections;
using System.Net;
using System.Reflection;
using Dawnholder.Server.GameServer.Guild;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Preparation for the guild limits and failure paths that no external input can reach in a test
// (technical contract v1, Q1 follow-up). It adds no product API: the World-owned guild store keeps its
// single owner, and each helper below only places one extreme value before a real request is sent.
//   - Communal economy: GuildRegistry._state is replaced with a value from the existing GuildState
//     constructor, which still enforces treasury 0..1,000,000,000 and stronghold level 1..2. A real cap or
//     uint.MaxValue revision would need about two hundred million admissions or four billion changes.
//   - Snapshot sequence: the current connection's private Connection.Sequence (inside _members) is set,
//     standing for a connection that has already received long.MaxValue snapshots.
// Requests, ticks and observations stay the real GameSession → World queue → wire path (GuildTestWorld).
internal static class GuildBoundaryFixture
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal static void ReplaceCommunalState(GuildTestWorld guild, uint revision, int treasury, byte strongholdLevel)
    {
        FieldInfo state = typeof(GuildRegistry).GetField("_state", Private)
            ?? throw new InvalidOperationException("fixture: GuildRegistry._state not found");
        state.SetValue(guild.World.Guild, new GuildState(revision, treasury, strongholdLevel));
    }

    internal static void SetSnapshotSequence(GuildTestWorld guild, GameSession member, long sequence)
    {
        object connection = CurrentConnection(guild, member);
        PropertyInfo property = connection.GetType().GetProperty("Sequence", Private)
            ?? throw new InvalidOperationException("fixture: guild Connection.Sequence not found");
        property.SetValue(connection, sequence);
    }

    // Same real handshake and C_Login as GuildTestWorld.Connect, for a connection whose transport can fail.
    internal static GuildFaultClient ConnectFaultClient(GuildTestWorld guild, string accountName)
    {
        GuildFaultClient client = new(guild.World, accountName);
        client.OnConnected(Endpoint);
        client.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        client.OnRecvPacket(new C_Login { accountName = accountName }.Write());
        guild.World.Accounts.Tick();
        Assert.True(client.IsLoggedIn, "fixture: the real account login must be accepted");
        return client;
    }

    // Same real entry as GuildTestWorld.JoinAndEnter; the admission 44 proves guild membership.
    internal static GuildFaultClient EnterFaultClient(GuildTestWorld guild)
    {
        GuildFaultClient client = ConnectFaultClient(guild, guild.NewAccountName());
        client.OnRecvPacket(SelectKnight());
        guild.Tick();
        Assert.True(client.EntityId >= 0, "fixture: the real class selection must enter Town");
        Assert.NotEmpty(client.Since(0, GuildWire.GuildSnapshotId));
        guild.Settle();
        return client;
    }

    internal static byte[] SelectKnight()
        => new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write().ToArray();

    // GuildTestWorld.Query and Act take the sealed GuildClient; these are the same steps for a fault client.
    internal static GuildSnapshot QueryAs(GuildTestWorld guild, GuildFaultClient client)
    {
        guild.Settle();
        int mark = client.Mark;
        client.OnRecvPacket(GuildWire.GuildRequest());
        guild.Settle();
        byte[] reply = Assert.Single(client.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        GuildSnapshot snapshot = GuildTestWorld.ReadSnapshot(reply);
        Assert.Equal(ChangeKind.Query, snapshot.ChangeKind);
        return snapshot;
    }

    internal static List<byte[]> ActAs(GuildTestWorld guild, GuildFaultClient client, byte[] request)
    {
        int mark = client.Mark;
        client.OnRecvPacket(request);
        guild.Settle();
        return client.Since(mark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId);
    }

    static object CurrentConnection(GuildTestWorld guild, GameSession member)
    {
        FieldInfo membersField = typeof(GuildRegistry).GetField("_members", Private)
            ?? throw new InvalidOperationException("fixture: GuildRegistry._members not found");
        IDictionary members = (IDictionary)membersField.GetValue(guild.World.Guild)!;
        Guid accountId = member.Account?.AccountId
            ?? throw new InvalidOperationException("fixture: the member must be logged in");
        object connection = members[accountId]
            ?? throw new InvalidOperationException("fixture: the account has no online guild connection");
        object? owner = connection.GetType().GetProperty("Session", Private)?.GetValue(connection);
        if (!ReferenceEquals(owner, member))
            throw new InvalidOperationException("fixture: the online guild connection belongs to another session");
        return connection;
    }
}

// Test-only connection for the transport failures GuildClient cannot produce: GuildClient promises that
// Send never fails, so it stays unchanged and this separate type is used where a fault is needed.
//   - ThrowOnSendOf: Send throws for that packet ID (a send exception; the connection stays open).
//   - CloseOnSendOf: the socket closes at that Send and the bytes never arrive.
//   - CloseAtNextGuildMapRead: the socket closes inside the next guild job. During a World tick the only map
//     read of a connection is GuildRegistry's location check (GameSession.ReadGuildMap → GetMap); receive-time
//     handlers also read it, so the test arms this after the receive call.
// Each fired fault is counted so a test can show that the failing step was actually reached.
internal sealed class GuildFaultClient : GameSession
{
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal readonly List<byte[]> Packets = new();

    readonly HashSet<ushort> _throwOnSend = new();
    readonly HashSet<ushort> _closeOnSend = new();
    bool _closeAtNextGuildMapRead;

    internal GuildFaultClient(GameWorld world, string accountName) : base(world) => AccountName = accountName;

    internal string AccountName { get; }

    internal int Mark => Packets.Count;

    internal int ThrownSends { get; private set; }

    internal bool ClosedBySend { get; private set; }

    internal bool ClosedByGuildMapRead { get; private set; }

    public override void Send(ArraySegment<byte> packet)
    {
        byte[] bytes = packet.ToArray();
        ushort id = GuildWire.IdOf(bytes);
        if (_closeOnSend.Contains(id))
        {
            ClosedBySend = true;
            Close();
            return;
        }
        if (_throwOnSend.Contains(id))
        {
            ThrownSends++;
            throw new IOException($"fixture: injected send failure for packet {id}");
        }
        Packets.Add(bytes);
    }

    public override void Disconnect() => OnDisconnected(Endpoint);

    internal void Close() => OnDisconnected(Endpoint);

    internal void ThrowOnSendOf(params ushort[] ids) => _throwOnSend.UnionWith(ids);

    internal void CloseOnSendOf(ushort id) => _closeOnSend.Add(id);

    internal void CloseAtNextGuildMapRead() => _closeAtNextGuildMapRead = true;

    internal void ClearFaults()
    {
        _throwOnSend.Clear();
        _closeOnSend.Clear();
        _closeAtNextGuildMapRead = false;
    }

    internal List<byte[]> Since(int mark, params ushort[] ids)
        => Packets.Skip(mark).Where(packet => ids.Contains(GuildWire.IdOf(packet))).ToList();

    protected override GameMap? GetMap()
    {
        GameMap? map = base.GetMap();
        if (_closeAtNextGuildMapRead)
        {
            _closeAtNextGuildMapRead = false;
            ClosedByGuildMapRead = true;
            Close();
        }
        return map;
    }
}
