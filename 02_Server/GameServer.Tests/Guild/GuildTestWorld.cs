using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Real-entry fixture for the guild lead tests (technical contract v1). It drives the actual GameWorld
// tick without a scheduler and enters each connection through the real handshake, C_Login with a fixed
// account name, and C_CharacterSelect bytes. Guild input is hand-written wire (GuildWire) into
// GameSession.OnRecvPacket; the only observation is what the server sends to each connection.
//
// Preparation that does not go through a guild purchase is limited to, and named at each use:
//   - treasury: N distinct accounts × the approved seed 5; a real BossRoom clear may add the guild's
//     clear credit, which technical contract v1 owns and the clear tests observe (GuildDungeonClearTests),
//   - personal funds: a real solo BossRoom clear (Content C1/C5: currency 50 + material 1 + pouch 1 to
//     each beneficiary, no boss kill reward) or a real normal/golem kill (normal 10 + material 1, golem
//     10 + pouch 1); nothing writes inventory,
//   - placement next to a portal, as InventoryWireContractTests does; the move is the real portal path,
//   - the boss room gate through the quest credit entry, as InstanceMapTestWorld.UnlockBossRoom does.
// The guild store belongs to the GameWorld (D1), so each test's new world starts with an empty guild.
internal sealed class GuildTestWorld : IDisposable
{
    // Test allowances, not contract values: a reply may leave from the world tick or a map job, and a
    // Mage projectile or kill push needs a few more ticks.
    internal const int SettleTicks = 3;
    internal const int DeferredSettleTicks = 8;

    // Existing v18 Content wire: 39 is the clear result each beneficiary receives.
    internal const ushort DungeonClearResultId = (ushort)PacketID.S_DungeonClearResult;

    const int TownToHuntingGroundPortalId = 1;
    const int HuntingGroundToBossRoomPortalId = 1;
    static readonly Vector2 TownPortalPosition = new(20, 0);
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    readonly GameWorld _world = new(MapData());
    long _tick;
    int _accountSerial;

    internal GameWorld World => _world;

    internal long CurrentTick => _tick;

    public void Dispose() => _world.Stop();

    internal void Tick() => LifecycleTestWorld.Tick(_world, ++_tick);

    internal void Settle(int ticks = SettleTicks)
    {
        for (int i = 0; i < ticks; i++) Tick();
    }

    // Account names follow AccountNameRule (3..16 of a-z, 0-9, _).
    internal string NewAccountName() => $"guild_member_{++_accountSerial:D2}";

    // Handshake and account login only: no class is selected and no map is entered yet.
    internal GuildClient Connect(string accountName)
    {
        GuildClient client = new(_world, accountName);
        client.OnConnected(Endpoint);
        client.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        client.OnRecvPacket(new C_Login { accountName = accountName }.Write());
        _world.Accounts.Tick();
        return client;
    }

    // Real handshake → login → class selection, then the entry tick in Town.
    internal GuildClient JoinAndEnter(CharacterClass characterClass, string? accountName = null)
    {
        GuildClient client = Connect(accountName ?? NewAccountName());
        Assert.True(client.IsLoggedIn, "fixture: the real account login must be accepted");
        client.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)characterClass }.Write());
        Tick();
        Assert.True(client.EntityId >= 0, "fixture: the real handshake, login and class selection must enter Town");
        Settle();
        return client;
    }

    // Treasury preparation: `count` distinct accounts, each seeding 5 on first entry (approved A).
    internal GuildClient[] JoinMembers(int count, CharacterClass characterClass = CharacterClass.Knight)
        => Enumerable.Range(0, count).Select(_ => JoinAndEnter(characterClass)).ToArray();

    // D8: a normal query is answered with exactly one 44, of kind Query, and no 47.
    internal GuildSnapshot Query(GuildClient client)
    {
        Settle();
        int mark = client.Mark;
        client.OnRecvPacket(GuildWire.GuildRequest());
        Settle();
        byte[] reply = Assert.Single(client.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        GuildSnapshot snapshot = ReadSnapshot(reply);
        Assert.Equal(ChangeKind.Query, snapshot.ChangeKind);
        return snapshot;
    }

    // Existing 35 → 36 query of the personal economy state.
    internal InventorySnapshot QueryInventory(GuildClient client)
    {
        Settle();
        int mark = client.Mark;
        client.OnRecvPacket(GuildWire.InventoryRequest());
        Settle();
        return GuildWire.ReadInventorySnapshot(Assert.Single(client.Since(mark, GuildWire.InventorySnapshotId)));
    }

    // Sends one guild request and returns the 36/44/47 replies this connection received, in order.
    internal List<byte[]> Act(GuildClient client, byte[] request)
    {
        int mark = client.Mark;
        client.OnRecvPacket(request);
        Settle();
        return client.Since(mark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId);
    }

    // Real 45 with the revision the buyer last saw; later steps start from stronghold level 2.
    internal void UpgradeStronghold(GuildClient buyer)
    {
        GuildSnapshot before = Query(buyer);
        List<byte[]> replies = Act(buyer, GuildWire.StrongholdUpgrade(before.GuildRevision));
        GuildActionResult result = SingleResult(replies);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.Equal(2, Query(buyer).StrongholdLevel);
    }

    // Real 46 with the revision of the client's latest 36.
    internal List<byte[]> Enhance(GuildClient client)
        => Act(client, GuildWire.WeaponEnhance(QueryInventory(client).Revision));

    internal void EnhanceSucceeds(GuildClient client)
    {
        Assert.Equal(GuildCode.Success, SingleResult(Enhance(client)).Result);
        Assert.Equal(1, Query(client).WeaponLevel);
    }

    // Personal funds through the real melee and kill callback: a one-hit enemy placed in front of the
    // attacker. A Mage melee is a projectile, so the kill lands a few ticks later.
    internal void KillOneHit(GuildClient attacker, EnemyKind kind)
    {
        GameMap map = MapOf(attacker);
        PlayerEntity player = PlayerOf(attacker);
        EnemyEntity enemy = map.SpawnEnemy(kind, player.Position.X + 1f, player.Position.Y, maxHp: 1);
        attacker.OnRecvPacket(new C_Attack { targetEntityId = enemy.EntityId, attackerClientTick = (int)(_tick + 1) }.Write());
        Settle(DeferredSettleTicks);
        Assert.False(map.Enemies.ContainsKey(enemy.EntityId), "fixture: the real melee path must kill the one-hit enemy");
        Settle(CombatConstants.MeleeCooldownTicks);
    }

    // Personal funds for one enhance from the real Content clear: currency 50 + material 1 + coin pouch 1
    // to each beneficiary of a BossRoom copy. Since Content C5 a boss killer gets no kill reward, so a
    // boss in Town cannot fund it. The client clears a solo copy and returns to Town with its real 40.
    internal void GainBossReward(GuildClient client)
    {
        UnlockBossRoom(client);
        GameMap bossRoom = EnterBossRoom(client);
        int mark = client.Mark;
        KillBoss(bossRoom, client);
        Settle();
        S_DungeonClearResult result = ReadClearResult(Assert.Single(client.Since(mark, DungeonClearResultId)));
        Assert.True(result.result == (byte)InventoryResult.Success, "fixture: the real clear must pay this beneficiary");
        ConfirmToTown(client);
    }

    // Boss room gate input (MapMigration reads the quest kill count) through the quest credit entry, as
    // InstanceMapTestWorld.UnlockBossRoom does; 20 real kills would add their own rewards to the funds.
    // A party shares the progress, so one member's credit opens the door for every member.
    internal void UnlockBossRoom(GameSession member)
    {
        for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++) _world.Quest.OnKill(member.EntityId);
    }

    // Town → hunting ground copy → boss room copy of the same solo or party key, by the real portals.
    internal GameMap EnterBossRoom(GameSession member)
    {
        if (MapOf(member).MapId == MapId.Town) MoveThroughPortal(member, TownToHuntingGroundPortalId);
        return MoveThroughPortal(member, HuntingGroundToBossRoomPortalId);
    }

    // The portal position from PortalTable is where to stand (an input, not an expectation); the move
    // itself is the real C_EnterPortal path, and the arrival job runs on a later tick.
    internal GameMap MoveThroughPortal(GameSession member, int portalId)
    {
        Portal portal = PortalTable.GetPortalsFor(MapOf(member).MapId).Single(p => p.PortalId == portalId);
        PlayerOf(member).Position = portal.Position;
        member.OnRecvPacket(new C_EnterPortal { portalId = portalId }.Write());
        Settle();
        GameMap arrived = MapOf(member);
        Assert.True(arrived.MapId == portal.Dest, $"fixture: portal {portalId} must move the player to {portal.Dest}");
        return arrived;
    }

    // As DungeonClearTestWorld.KillBoss: the subject is the work after the death (clear capture,
    // settlement, results), so the room's product death handler runs inside that room's job. The kill
    // callback and the World phases then follow the product path in the same World tick.
    internal void KillBoss(GameMap bossRoom, GameSession killer)
    {
        EnemyEntity boss = Assert.Single(bossRoom.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, killer.EntityId);
        });
        Tick();
        Assert.False(bossRoom.Enemies.ContainsKey(boss.EntityId), "fixture: the boss death handler must remove the boss");
    }

    // A beneficiary's real 40 moves it from the cleared boss room to Town (Content C6).
    internal void ConfirmToTown(GameSession beneficiary)
    {
        beneficiary.OnRecvPacket(ClearConfirm());
        Settle();
        Assert.True(MapOf(beneficiary).MapId == MapId.Town, "fixture: the beneficiary's 40 must move it to Town");
    }

    internal GameMap MapOf(GameSession session)
        => _world.AllLiveMaps.Single(map => map.GetPlayer(session.EntityId) != null);

    internal PlayerEntity PlayerOf(GameSession session) => MapOf(session).GetPlayer(session.EntityId)!;

    // Fixture placement only; the request that follows is the real C_EnterPortal path.
    internal void PlaceAtTownPortal(GuildClient client) => PlayerOf(client).Position = TownPortalPosition;

    internal static byte[] EnterTownPortalToHuntingGround()
        => new C_EnterPortal { portalId = TownToHuntingGroundPortalId }.Write().ToArray();

    internal static byte[] ClearConfirm() => new C_DungeonClearConfirm { reserved = 0 }.Write().ToArray();

    internal GameMap MoveToHuntingGround(GuildClient client)
    {
        PlaceAtTownPortal(client);
        client.OnRecvPacket(EnterTownPortalToHuntingGround());
        Tick();
        Tick(); // a new copy runs its arrival job from the next tick (IM-16)
        Assert.True(
            _world.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(client.EntityId), out GameMap? hunting),
            "fixture: the real portal path must create the solo HuntingGround copy");
        Assert.NotNull(hunting!.GetPlayer(client.EntityId));
        return hunting;
    }

    internal static GuildSnapshot ReadSnapshot(byte[] packet)
    {
        GuildSnapshot snapshot = GuildWire.ReadSnapshot(packet);
        GuildWire.AssertWithinContract(snapshot);
        return snapshot;
    }

    internal static GuildActionResult SingleResult(List<byte[]> replies)
        => GuildWire.ReadActionResult(Assert.Single(replies, packet => GuildWire.IdOf(packet) == GuildWire.GuildActionResultId));

    // Order of guild-relevant replies, e.g. [47, 36, 44] for a successful enhance (D8).
    internal static ushort[] Ids(List<byte[]> replies) => replies.Select(GuildWire.IdOf).ToArray();

    // The existing 39 is read with its existing generated reader.
    internal static S_DungeonClearResult ReadClearResult(byte[] packet)
    {
        S_DungeonClearResult result = new();
        result.Read(new ArraySegment<byte>(packet));
        return result;
    }

    // Town and the hunting ground keep no map data, as before (spawn at the origin, no enemies). Only the
    // boss room has the one boss a real clear needs, out of its detection range as in InstanceMapTestWorld.
    static IReadOnlyDictionary<MapId, (MapTerrain?, MapContent?)> MapData()
        => new Dictionary<MapId, (MapTerrain?, MapContent?)>
        {
            [MapId.BossRoom] = (null, new MapContent(0f, 0f, new[]
            {
                new EnemySpawnPoint((byte)EnemyKind.Boss, InstanceMapTestWorld.BossX, 0f),
            })),
        };
}

// Captures every packet the server sends to one connection; Send never fails here.
internal sealed class GuildClient : GameSession
{
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal readonly List<byte[]> Packets = new();

    internal GuildClient(GameWorld world, string accountName) : base(world) => AccountName = accountName;

    internal string AccountName { get; }

    internal int Mark => Packets.Count;

    public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

    public override void Disconnect() => OnDisconnected(Endpoint);

    internal void Close() => OnDisconnected(Endpoint);

    internal List<byte[]> Since(int mark, params ushort[] ids)
        => Packets.Skip(mark).Where(packet => ids.Contains(GuildWire.IdOf(packet))).ToList();
}
