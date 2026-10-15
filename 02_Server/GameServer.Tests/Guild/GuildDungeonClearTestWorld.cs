using System.Collections;
using System.Reflection;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Preparation for the clear credit tests (GuildDungeonClearTests) on top of GuildTestWorld's real entry and
// real BossRoom clear. DungeonClearTestWorld cannot be composed here: it owns another GameWorld, and only one
// GameWorld may exist at a time. What this adds:
//   - a real two-member party, so one clear has two beneficiaries in the same BossRoom copy,
//   - the replies of any test connection (GuildClient, or GuildFaultClient where a transport fault is needed;
//     GuildTestWorld.Query/QueryInventory take only the sealed GuildClient),
//   - Material at MaxStack through real kills, and the named limit states no external input reaches.
// The communal limits and the snapshot sequence use the existing GuildBoundaryFixture helpers.
internal sealed class GuildDungeonClearTestWorld : IDisposable
{
    // Longer than a settle window: ticks in which nothing more may arrive (DungeonClearRewardTests.StayTicks).
    internal const int StayTicks = 40;

    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    internal GuildTestWorld Guild { get; } = new();

    public void Dispose() => Guild.Dispose();

    // Two new accounts (seed 5 each) in one party whose shared progress opens the boss door for both.
    internal (GuildClient First, GuildClient Second) JoinParty()
    {
        GuildClient first = Guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient second = Guild.JoinAndEnter(CharacterClass.Knight);
        FormParty(first, second);
        Guild.UnlockBossRoom(first);
        return (first, second);
    }

    // Real invite and accept, before any portal: a copy's key follows the membership at entry.
    internal void FormParty(GameSession inviter, GameSession invitee)
    {
        inviter.OnRecvPacket(new C_PartyInvite { targetEntityId = invitee.EntityId }.Write());
        Guild.Tick();
        invitee.OnRecvPacket(new C_PartyRespond { inviterEntityId = inviter.EntityId, accept = 1 }.Write());
        Guild.Tick();
        PartyState? party = Guild.World.Party.GetPartyByEntity(inviter.EntityId);
        Assert.True(party != null && party.Members.Contains(invitee.EntityId), "fixture: invite and accept must form a party");
        Guild.Settle();
    }

    // The first to enter is captured first: a map keeps its players in arrival order.
    internal GameMap EnterSameBossRoom(GameSession first, GameSession second)
    {
        GameMap room = Guild.EnterBossRoom(first);
        GameMap other = Guild.EnterBossRoom(second);
        Assert.True(ReferenceEquals(room, other), "fixture: both party members must be in the same boss room copy");
        return room;
    }

    // GuildTestWorld.KillBoss with one more step inside the same room job, right after the death: the clear has
    // captured its beneficiaries, and the World phases that settle them have not run yet.
    internal void KillBossThen(GameMap bossRoom, GameSession killer, Action<EnemyEntity> sameJobAfterDeath)
    {
        EnemyEntity boss = Assert.Single(bossRoom.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, killer.EntityId);
            sameJobAfterDeath(boss);
        });
        Guild.Tick();
        Assert.False(bossRoom.Enemies.ContainsKey(boss.EntityId), "fixture: the boss death handler must remove the boss");
    }

    // Same real 35 → 36 and 43 → 44 queries as GuildTestWorld, for any test connection.
    internal InventorySnapshot QueryInventory(GameSession session)
    {
        Guild.Settle();
        int mark = MarkOf(session);
        session.OnRecvPacket(GuildWire.InventoryRequest());
        Guild.Settle();
        return GuildWire.ReadInventorySnapshot(Assert.Single(Since(session, mark, GuildWire.InventorySnapshotId)));
    }

    internal GuildSnapshot QueryGuild(GameSession session)
    {
        Guild.Settle();
        int mark = MarkOf(session);
        session.OnRecvPacket(GuildWire.GuildRequest());
        Guild.Settle();
        GuildSnapshot snapshot = GuildTestWorld.ReadSnapshot(
            Assert.Single(Since(session, mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId)));
        Assert.Equal(ChangeKind.Query, snapshot.ChangeKind);
        return snapshot;
    }

    // No state is planted: one real Dash kills MaxStack one-hit normal enemies, each paying Material 1
    // (the method of DungeonClearTestWorld.FillMaterialToMaxStack and InventoryWireContractTests).
    internal void FillMaterialToMaxStack(GuildClient knight)
    {
        GameMap town = Guild.MapOf(knight);
        float x = Guild.PlayerOf(knight).Position.X + CombatConstants.DashBoxHalfX;
        EnemyEntity[] pack = Enumerable.Range(0, InventoryLimits.MaxStack)
            .Select(_ => town.SpawnEnemy(EnemyKind.Normal, x, 0f, maxHp: 1))
            .ToArray();
        knight.OnRecvPacket(new C_SkillUse
        {
            skillId = (byte)SkillId.Dash,
            attackerClientTick = (int)(Guild.CurrentTick + 1),
            facing = 1,
        }.Write());
        Guild.Tick();
        Assert.All(pack, enemy => Assert.False(town.Enemies.ContainsKey(enemy.EntityId), "fixture: one Dash must kill them all"));
        Guild.Settle(Constants.DashTravelTicks);
    }

    // Named limit precondition only, never funding: a personal currency at the cap or a revision at
    // uint.MaxValue needs about a billion real changes. The connection's registry entry gets a state built by
    // the existing InventoryState constructor, as WeaponEnhanceTests.ReplaceInventoryState does; query first so
    // the entry exists. The clear and every observation stay the real path.
    internal void ReplaceInventoryState(GameSession session, InventoryState state)
    {
        FieldInfo statesField = typeof(InventoryRegistry).GetField("_states", Private)
            ?? throw new InvalidOperationException("fixture: InventoryRegistry._states not found");
        IDictionary states = (IDictionary)statesField.GetValue(Guild.World.Inventory)!;
        object entry = states[session]
            ?? throw new InvalidOperationException("fixture: query the inventory first so the entry exists");
        PropertyInfo stateProperty = entry.GetType().GetProperty("State", Private)
            ?? throw new InvalidOperationException("fixture: InventoryRegistry entry has no State");
        stateProperty.SetValue(entry, state);
    }

    internal static int MarkOf(GameSession session) => PacketsOf(session).Count;

    internal static List<byte[]> Since(GameSession session, int mark, params ushort[] ids)
        => PacketsOf(session).Skip(mark).Where(packet => ids.Contains(GuildWire.IdOf(packet))).ToList();

    // One connection's clear economy replies in arrival order: 39, 36 and 44.
    internal static List<byte[]> EconomySince(GameSession session, int mark)
        => Since(session, mark, GuildTestWorld.DungeonClearResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId);

    internal static List<GuildSnapshot> SnapshotsSince(GameSession session, int mark)
        => Since(session, mark, GuildWire.GuildSnapshotId).Select(GuildTestWorld.ReadSnapshot).ToList();

    static List<byte[]> PacketsOf(GameSession session) => session switch
    {
        GuildClient client => client.Packets,
        GuildFaultClient fault => fault.Packets,
        _ => throw new ArgumentException("fixture: not a test connection", nameof(session)),
    };
}
