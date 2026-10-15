using Dawnholder.Server.GameServer.Maps;
using static Dawnholder.Server.GameServer.Tests.Guild.IndependentSpec;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Independent strong verification of the guild lifetimes (technical contract v1: G1, G3, G6; D1, D3, D4,
// D8, D10; I4, I7). Same real entry, stand-ins and expectation rules as GuildIndependentContractTests,
// whose IndependentGuildWorld / IndependentWire this file uses. Each test pairs the reply shape with a
// state check that a wrong commit (a second spend, a processed extra request, a consumed enhance) breaks.
// Not covered: a real socket close race, the job-exception path, Content clear credit, bots.
[Collection("GameWorldRegistryTests")]
public sealed class GuildIndependentLifetimeTests : IDisposable
{
    readonly IndependentGuildWorld _world = new();

    public void Dispose() => _world.Dispose();

    // G3 / I4 / D4 / D9: two members send the same revision before one tick (treasury 60 could pay twice).
    // Exactly one spends 30 once; the other is Stale, because the revision check precedes the highest
    // level. Every member, the loser included, sees exactly one StrongholdUpgrade-marked 44.
    [Fact]
    public void SameRevisionRace_OneSpendsOnce_TheOtherIsStale_AndEachMemberSeesOneUpgrade()
    {
        IndependentSession[] members = _world.EnterMany(12);
        IndependentSnapshot before = _world.Query(members[0]);
        Assert.Equal(12 * Approved.Seed, before.Treasury);
        int[] marks = members.Select(member => member.Mark).ToArray();

        members[0].Receive(IndependentWire.StrongholdUpgrade(before.GuildRevision));
        members[1].Receive(IndependentWire.StrongholdUpgrade(before.GuildRevision));
        _world.Tick(IndependentGuildWorld.ReplyTicks);

        IndependentResult[] results = { SingleResult(members[0], marks[0]), SingleResult(members[1], marks[1]) };
        IndependentResult winner = Assert.Single(results, result => result.Result == Code.Success);
        IndependentResult loser = Assert.Single(results, result => result.Result == Code.Stale);
        Assert.Equal(before.GuildRevision, loser.RequestedRevision);
        Assert.True(winner.GuildRevision > before.GuildRevision, "the purchase advances the communal revision");
        Assert.Equal(winner.GuildRevision, loser.GuildRevision);
        for (int i = 0; i < members.Length; i++)
        {
            Assert.Single(members[i].Since(marks[i], WireId.GuildSnapshot),
                packet => IndependentWire.ReadSnapshot(packet).ChangeKind == Kind.StrongholdUpgrade);
        }
        IndependentSnapshot after = _world.Query(members[5]);
        Assert.Equal((12 * Approved.Seed) - Approved.StrongholdPrice, after.Treasury);
        Assert.Equal(2, after.StrongholdLevel);
        Assert.Equal(winner.GuildRevision, after.GuildRevision);
    }

    // G1 / D1: a close lowers only the online count; treasury and revision stay. The returning account is
    // a new connection that starts again at sequence 1 and receives no second seed.
    [Fact]
    public void CloseAndReturn_ChangeOnlyPresence_AndTheReturnStartsAtSequenceOne()
    {
        IndependentSession stays = _world.Enter();
        IndependentSession leaves = _world.Enter();
        IndependentSnapshot before = _world.Query(stays);
        int mark = stays.Mark;

        leaves.Disconnect();
        _world.Tick(IndependentGuildWorld.ReplyTicks);

        IndependentSnapshot gone = IndependentWire.ReadSnapshot(Assert.Single(stays.Since(mark, WireId.GuildSnapshot)));
        Assert.Equal(Kind.Presence, gone.ChangeKind);
        Assert.True(gone.Sequence > before.Sequence, "a later 44 carries a larger sequence");
        Assert.Equal(before.GuildRevision, gone.GuildRevision);
        Assert.Equal(before.Treasury, gone.Treasury);
        Assert.Equal(2, gone.MemberCount);
        Assert.Equal(1, gone.OnlineMemberCount);

        mark = stays.Mark;
        IndependentSession back = _world.Enter(leaves.AccountName);

        IndependentSnapshot returned = IndependentWire.ReadSnapshot(Assert.Single(back.Since(0, WireId.GuildSnapshot)));
        IndependentSnapshot seen = IndependentWire.ReadSnapshot(Assert.Single(stays.Since(mark, WireId.GuildSnapshot)));
        Assert.Equal(1, returned.Sequence);
        Assert.Equal(Kind.Presence, seen.ChangeKind);
        foreach (IndependentSnapshot snapshot in new[] { returned, seen })
        {
            Assert.Equal(before.GuildRevision, snapshot.GuildRevision);
            Assert.Equal(before.Treasury, snapshot.Treasury);
            Assert.Equal(2, snapshot.MemberCount);
            Assert.Equal(2, snapshot.OnlineMemberCount);
        }
    }

    // G6 / I7 / D3: a burst of guild input before the tick is bounded by the connection's one guild slot:
    // only the first request is answered, even after many more ticks, while the separate inventory slot
    // answers its own request. Treasury 30 makes any processed extra 45 visible as a 47 or level 2.
    [Fact]
    public void BurstOfGuildInput_IsAnsweredOnce_AndTheSlotIsFreeAfterwards()
    {
        IndependentSession[] members = _world.EnterMany(6);
        IndependentSession sender = members[0];
        IndependentSnapshot before = _world.Query(sender);
        int mark = sender.Mark;

        sender.Receive(IndependentWire.GuildRequest());
        sender.Receive(IndependentWire.InventoryRequest());
        for (int i = 0; i < 200; i++)
        {
            sender.Receive(i % 2 == 0
                ? IndependentWire.StrongholdUpgrade(before.GuildRevision)
                : IndependentWire.WeaponEnhance(before.InventoryRevision));
        }
        _world.Tick(IndependentGuildWorld.ReplyTicks * 4);

        byte[] answered = Assert.Single(sender.Since(mark, WireId.GuildSnapshot, WireId.GuildActionResult));
        Assert.Equal(Kind.Query, IndependentWire.ReadSnapshot(answered).ChangeKind);
        Assert.Single(sender.Since(mark, WireId.InventorySnapshot));
        IndependentSnapshot after = _world.Query(sender);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(1, after.StrongholdLevel);

        List<byte[]> purchase = _world.Act(sender, IndependentWire.StrongholdUpgrade(after.GuildRevision));
        Assert.Equal(Code.Success, IndependentWire.ReadResult(purchase[0]).Result);
    }

    // D3 / D4 / I7: an enhance accepted in Town but drained while that tick's portal move is in progress is
    // Unavailable and consumes nothing; after arrival the same payable request is NotInTown.
    [Fact]
    public void EnhanceDrainedDuringAPortalMove_IsUnavailable_ThenNotInTown_AndConsumesNothing()
    {
        IndependentSession[] members = _world.EnterMany(6);
        IndependentSession mover = members[0];
        _world.UpgradeStronghold(members[1]);
        _world.KillBoss(mover);
        IndependentInventory funded = _world.QueryInventory(mover);
        IndependentSnapshot inTown = _world.Query(mover);
        Assert.Equal(Code.Success, inTown.WeaponAvailability);
        int mark = mover.Mark;

        _world.PlaceAtTownPortal(mover);
        mover.Receive(IndependentWire.WeaponEnhance(funded.Revision));
        mover.Receive(IndependentGuildWorld.EnterTownPortal());
        _world.Tick(IndependentGuildWorld.ReplyTicks);

        List<byte[]> moving = mover.Since(mark, WireId.InventorySnapshot, WireId.GuildSnapshot, WireId.GuildActionResult);
        IndependentWire.AssertRefusal(moving, Purchase.Weapon, Code.Unavailable, funded.Revision, inTown);
        Assert.Equal(Code.Unavailable, IndependentWire.ReadSnapshot(moving[1]).WeaponAvailability);
        Assert.NotEqual(MapId.Town, _world.MapOf(mover).MapId);
        Assert.Equal(funded, _world.QueryInventory(mover));

        IndependentSnapshot away = _world.Query(mover);
        IndependentWire.AssertRefusal(_world.Act(mover, IndependentWire.WeaponEnhance(funded.Revision)),
            Purchase.Weapon, Code.NotInTown, funded.Revision, away);
        Assert.Equal(funded, _world.QueryInventory(mover));
        Assert.Equal(0, _world.Query(mover).WeaponLevel);
    }

    static IndependentResult SingleResult(IndependentSession session, int mark)
        => IndependentWire.ReadResult(Assert.Single(session.Since(mark, WireId.GuildActionResult)));
}
