using System.Net;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;
using static Dawnholder.Server.GameServer.Tests.Guild.IndependentSpec;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Independent re-verification of the guild admission fix (D-01 entry order, D-02 no half admission) against
// technical contract v1 (snapshotSequence per connection, D1, D8) and the entry order of domains/server.md:
// CharacterLoadResult → EnterMap → HP → the roster captured before the add → the join to everyone else.
// The boundaries here are the
// ones the earlier tests leave open: a full roster with live enemies, two admissions in one tick, and a peer
// whose join send fails. Expectations are contract literals (IndependentSpec) or before/after relations.
// Entry: real handshake → C_Login → C_CharacterSelect → OnRecvPacket → HandlerRegistry → GameWorld.OnTick,
// observed at Send. Stand-ins: Send is a list (or a throwing peer) and OnTick is called directly instead of
// the TickScheduler. Not covered: a real socket or thread race, Content clear credit, Unity and bot consumers.
[Collection("GameWorldRegistryTests")]
public sealed class GuildAdmissionContractTests : IDisposable
{
    readonly AdmissionWorld _world = new();
    readonly ITestOutputHelper _output;

    public GuildAdmissionContractTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _world.Dispose();

    // D-01: with two members and two live enemies in Town, the entrant's unfiltered stream is the login replies,
    // then the authoritative class result and whole map entry block, and only then its single admission 44.
    // A 44 inside the roster cannot pass this full prefix. Each member sees the entrant join and one newer 44.
    [Fact]
    public void Entry_SendsTheWholeMapEntryBlock_BeforeTheOnlyAdmission44()
    {
        IndependentSession first = _world.Enter("adm_first");
        IndependentSession second = _world.Enter("adm_second");
        EnemyEntity[] enemies =
        {
            _world.Town.SpawnEnemy(EnemyKind.Normal, 300f, 0f, 100),
            _world.Town.SpawnEnemy(EnemyKind.Normal, 320f, 0f, 100),
        };
        IndependentSession[] members = { first, second };
        int[] marks = members.Select(member => member.Mark).ToArray();
        long[] lastSequences = members.Select(member => LastSnapshot(member).Sequence).ToArray();

        IndependentSession entrant = _world.LogIn("adm_entrant");
        entrant.Receive(AdmissionWorld.SelectKnight());
        _world.Tick();

        PacketID[] expectedOrder =
        {
            PacketID.S_HandshakeResult, PacketID.S_LoginResult, PacketID.S_CharacterLoadResult,
            PacketID.S_EnterMap, PacketID.S_PlayerHp,
            PacketID.S_PlayerJoin, PacketID.S_PlayerJoin, PacketID.S_EntitySpawn, PacketID.S_EntitySpawn,
            PacketID.S_GuildSnapshot,
        };
        byte[][] head = entrant.Sent.Take(expectedOrder.Length).ToArray();
        Assert.Equal(expectedOrder, head.Select(IdOf));
        byte[] classResult = Assert.Single(entrant.Since(0, (ushort)PacketID.S_CharacterLoadResult));
        // Fixed wire contract: size 6, packet 48, Success 0, selected Knight 0.
        Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 0 }, classResult);
        Assert.Equal(entrant.EntityId, Read<S_EnterMap>(head[3]).entityId);
        Assert.Equal(entrant.EntityId, Read<S_PlayerHp>(head[4]).entityId);
        Assert.Equal(new[] { first.EntityId, second.EntityId }.Order(),
            head[5..7].Select(packet => Read<S_PlayerJoin>(packet).entityId).Order());
        Assert.Equal(enemies.Select(enemy => enemy.EntityId).Order(),
            head[7..9].Select(packet => Read<S_EntitySpawn>(packet).entityId).Order());

        IndependentSnapshot admitted = IndependentWire.ReadSnapshot(head[9]);
        Assert.Single(entrant.Since(0, WireId.GuildSnapshot));
        Assert.Equal(1, admitted.Sequence);
        Assert.Equal(Kind.Membership, admitted.ChangeKind);
        Assert.Equal(3 * Approved.Seed, admitted.Treasury);
        Assert.Equal(3, admitted.MemberCount);
        Assert.Equal(3, admitted.OnlineMemberCount);

        for (int i = 0; i < members.Length; i++)
        {
            byte[] join = Assert.Single(members[i].Since(marks[i], (ushort)PacketID.S_PlayerJoin));
            Assert.Equal(entrant.EntityId, Read<S_PlayerJoin>(join).entityId);
            IndependentSnapshot pushed = IndependentWire.ReadSnapshot(
                Assert.Single(members[i].Since(marks[i], WireId.GuildSnapshot)));
            Assert.Equal(Kind.Membership, pushed.ChangeKind);
            Assert.True(pushed.Sequence > lastSequences[i], "a member's next 44 carries a larger sequence");
            Assert.Equal(admitted.GuildRevision, pushed.GuildRevision);
            Assert.Equal(3, pushed.MemberCount);
            Assert.Equal(3, pushed.OnlineMemberCount);
        }
    }

    // Contract 「snapshotSequence는 접속별1부터 증가… 클라이언트는 현재 연결의 더 큰 sequence만 적용」 with the
    // deferred admission notice: the sequence is fixed at commit and the send comes later. When two selections
    // are admitted in one tick, each connection must still receive its own admission (1) first and then only
    // larger sequences; otherwise a client drops its admission as stale. Both end at two seeds and two members.
    [Fact]
    public void TwoAdmissionsInOneTick_ArriveInIncreasingSequencePerConnection()
    {
        IndependentSession earlier = _world.LogIn("adm_earlier");
        IndependentSession later = _world.LogIn("adm_later");
        earlier.Receive(AdmissionWorld.SelectKnight());
        later.Receive(AdmissionWorld.SelectKnight());

        _world.Tick();

        foreach (IndependentSession session in new[] { earlier, later })
        {
            Assert.True(session.EntityId > 0, "both selections must be admitted by the same tick");
            IndependentSnapshot[] arrived = session.Since(0, WireId.GuildSnapshot)
                .Select(IndependentWire.ReadSnapshot).ToArray();
            Assert.NotEmpty(arrived);
            Assert.Equal(1, arrived[0].Sequence);
            Assert.Equal(Kind.Membership, arrived[0].ChangeKind);
            Assert.True(arrived.Zip(arrived.Skip(1)).All(pair => pair.Second.Sequence > pair.First.Sequence),
                $"sequences in arrival order: {string.Join(",", arrived.Select(snapshot => snapshot.Sequence))}");
            int enterMap = session.Sent.FindIndex(packet => IdOf(packet) == PacketID.S_EnterMap);
            int admission = session.Sent.FindIndex(packet => IdOf(packet) == PacketID.S_GuildSnapshot);
            Assert.InRange(enterMap, 0, admission - 1);
            Assert.Equal(2 * Approved.Seed, arrived[^1].Treasury);
            Assert.Equal(2, arrived[^1].MemberCount);
            Assert.Equal(2, arrived[^1].OnlineMemberCount);
        }
        Assert.Equal(LastSnapshot(earlier).GuildRevision, LastSnapshot(later).GuildRevision);
    }

    // D-01 / D-02 / D8 / D1: a member's join send throws while the entrant is being admitted. Either outcome is
    // allowed, but not a half admission: a kept entrant is in the map and known to the healthy observer; a closed
    // entrant is in no map and not left in that observer's roster. The account's seed counts once, the online
    // count equals the live connections, and the healthy member still receives the membership change.
    [Fact]
    public void PeerJoinSendFailure_LeavesNoHalfAdmission_AndCountsTheSeedOnce()
    {
        // Entered first, so the Town join broadcast reaches the faulting member before the healthy one.
        FaultingPeer faulty = _world.Enter(new FaultingPeer(_world.World), "adm_faulty");
        IndependentSession healthy = _world.Enter("adm_healthy");
        IndependentSnapshot before = _world.Query(healthy);
        int mark = healthy.Mark;
        faulty.ThrowOnNextPlayerJoin = true;

        IndependentSession entrant = _world.LogIn("adm_entrant");
        entrant.Receive(AdmissionWorld.SelectKnight());
        _world.Tick(IndependentGuildWorld.ReplyTicks);

        Assert.True(faulty.FaultRaised, "fixture: the peer's join send must fail inside the admission");
        int entrantId = Read<S_EnterMap>(Assert.Single(entrant.Since(0, (ushort)PacketID.S_EnterMap))).entityId;
        bool kept = !entrant.IsClosing;
        PlayerEntity[] owned = _world.World.AllLiveMaps.SelectMany(map => map.Players)
            .Where(player => ReferenceEquals(player.Owner, entrant)).ToArray();
        int joins = healthy.Since(mark, (ushort)PacketID.S_PlayerJoin)
            .Count(packet => Read<S_PlayerJoin>(packet).entityId == entrantId);
        int leaves = healthy.Since(mark, (ushort)PacketID.S_PlayerLeave)
            .Count(packet => Read<S_PlayerLeave>(packet).entityId == entrantId);
        _output.WriteLine(
            $"observed: entrant {(kept ? "kept" : "closed")}, healthy observer joins {joins}, leaves {leaves}");

        if (kept)
        {
            Assert.Equal(entrantId, Assert.Single(owned).EntityId);
            Assert.Equal(1, joins);
            Assert.Equal(0, leaves);
            Assert.Equal(Kind.Membership,
                IndependentWire.ReadSnapshot(Assert.Single(entrant.Since(0, WireId.GuildSnapshot))).ChangeKind);
        }
        else
        {
            Assert.Empty(owned);
            Assert.Equal(-1, entrant.EntityId);
            Assert.True(joins <= leaves, "a closed entrant must not stay in the healthy observer's roster");
        }

        int live = kept ? 3 : 2;
        Assert.Equal(live, _world.World.Accounts.OnlineCount);
        Assert.Contains(healthy.Since(mark, WireId.GuildSnapshot).Select(IndependentWire.ReadSnapshot),
            snapshot => snapshot.ChangeKind == Kind.Membership && snapshot.MemberCount == 3);
        IndependentSnapshot after = _world.Query(healthy);
        Assert.Equal(before.Treasury + Approved.Seed, after.Treasury);
        Assert.Equal(3, after.MemberCount);
        Assert.Equal(live, after.OnlineMemberCount);
    }

    static PacketID IdOf(byte[] packet) => (PacketID)IndependentWire.IdOf(packet);

    static T Read<T>(byte[] packet)
        where T : IPacket, new()
    {
        T read = new();
        read.Read(new ArraySegment<byte>(packet));
        return read;
    }

    static IndependentSnapshot LastSnapshot(IndependentSession session)
        => IndependentWire.ReadSnapshot(session.Since(0, WireId.GuildSnapshot).Last());

    // The same real entry and stand-ins as IndependentGuildWorld, with the GameWorld exposed so that two
    // selections can share one tick and a faulting peer can be admitted like any other connection.
    sealed class AdmissionWorld : IDisposable
    {
        static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
        long _tick;

        internal GameWorld World { get; } = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());

        internal GameMap Town => World.Map;

        public void Dispose() => World.Stop();

        internal static ArraySegment<byte> SelectKnight()
            => new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write();

        internal void Tick(int count = 1)
        {
            for (int i = 0; i < count; i++) LifecycleTestWorld.Tick(World, ++_tick);
        }

        // Handshake and an accepted login only; the selection, and so the admission tick, stay with the caller.
        internal IndependentSession LogIn(string accountName)
            => LogIn(new IndependentSession(World, accountName), accountName);

        internal TSession LogIn<TSession>(TSession session, string accountName)
            where TSession : GameSession
        {
            session.OnConnected(Endpoint);
            session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
            session.OnRecvPacket(new C_Login { accountName = accountName }.Write());
            Tick();
            Assert.True(session.IsLoggedIn, "fixture: the real account login must be accepted");
            return session;
        }

        internal IndependentSession Enter(string accountName)
            => Enter(new IndependentSession(World, accountName), accountName);

        internal TSession Enter<TSession>(TSession session, string accountName)
            where TSession : GameSession
        {
            LogIn(session, accountName);
            session.OnRecvPacket(SelectKnight());
            Tick(IndependentGuildWorld.ReplyTicks);
            Assert.True(session.EntityId > 0, "fixture: the real entry tick must admit the connection");
            return session;
        }

        // D8: a normal query is answered with exactly one 44 of kind Query and no 47.
        internal IndependentSnapshot Query(IndependentSession session)
        {
            int mark = session.Mark;
            session.Receive(IndependentWire.GuildRequest());
            Tick(IndependentGuildWorld.ReplyTicks);
            IndependentSnapshot snapshot = IndependentWire.ReadSnapshot(
                Assert.Single(session.Since(mark, WireId.GuildSnapshot, WireId.GuildActionResult)));
            Assert.Equal(Kind.Query, snapshot.ChangeKind);
            return snapshot;
        }
    }

    // A member whose next S_PlayerJoin send throws once armed; every other send succeeds without a record.
    sealed class FaultingPeer(GameWorld world) : GameSession(world)
    {
        static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

        internal bool ThrowOnNextPlayerJoin { get; set; }

        internal bool FaultRaised { get; private set; }

        public override void Send(ArraySegment<byte> packet)
        {
            if (!ThrowOnNextPlayerJoin) return;
            if (IndependentWire.IdOf(packet.ToArray()) != (ushort)PacketID.S_PlayerJoin) return;
            ThrowOnNextPlayerJoin = false;
            FaultRaised = true;
            throw new IOException("fixture: a member's join send failed");
        }

        public override void Disconnect() => OnDisconnected(Endpoint);
    }
}
