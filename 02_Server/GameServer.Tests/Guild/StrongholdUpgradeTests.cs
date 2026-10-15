namespace Dawnholder.Server.GameServer.Tests.Guild;

// I4 / G3 (technical contract v1): a living member in Town spends treasury 30 once to raise the
// stronghold 1 → 2 and unlock the blacksmith, against the expected guild revision. Refusals follow
// D4 and change nothing; replies follow D8 (buyer 47 → 44, the same revision 44 to other members,
// a business refusal 47 → current 44). Treasury comes from seeds only (accounts × 5), see GuildTestWorld.
// Expected numbers are approved literals (price 30, levels 1/2) and the revision the server reported.
[Collection("GameWorldRegistryTests")]
public sealed class StrongholdUpgradeTests : IDisposable
{
    const int Price = 30;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    [Fact]
    public void ThirtyInTreasury_UpgradeSpendsThirtyOnce_AndEveryOnlineMemberSeesLevelTwo()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient buyer = members[0];
        GuildSnapshot before = _guild.Query(buyer);
        Assert.Equal(Price, before.Treasury);
        Assert.Equal(GuildCode.Success, before.StrongholdAvailability);
        Dictionary<GuildClient, int> marks = members.ToDictionary(member => member, member => member.Mark);

        List<byte[]> replies = _guild.Act(buyer, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        GuildSnapshot buyerView = GuildTestWorld.ReadSnapshot(replies[1]);
        Assert.Equal(GuildAction.StrongholdUpgrade, result.Action);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.Equal(before.GuildRevision, result.RequestedRevision);
        Assert.True(result.GuildRevision > before.GuildRevision, "a committed purchase advances the guild revision");
        Assert.Equal(before.InventoryRevision, result.InventoryRevision);
        Assert.Equal(buyerView.Sequence, result.Sequence);
        Assert.Equal(ChangeKind.StrongholdUpgrade, buyerView.ChangeKind);
        AssertUpgraded(buyerView, result.GuildRevision, treasury: 0);

        foreach (GuildClient other in members.Skip(1))
        {
            byte[] push = Assert.Single(other.Since(marks[other], GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
            GuildSnapshot otherView = GuildTestWorld.ReadSnapshot(push);
            Assert.Equal(ChangeKind.StrongholdUpgrade, otherView.ChangeKind);
            AssertUpgraded(otherView, result.GuildRevision, treasury: 0);
        }
        AssertUpgraded(_guild.Query(members[5]), result.GuildRevision, treasury: 0);
    }

    // Treasury 60 could pay twice, so a double commit would be visible as treasury 0.
    [Fact]
    public void TwoMembersSameRevisionSameTick_OnlyOneSpends()
    {
        GuildClient[] members = _guild.JoinMembers(12);
        GuildSnapshot before = _guild.Query(members[0]);
        Assert.Equal(2 * Price, before.Treasury);
        int firstMark = members[0].Mark;
        int secondMark = members[1].Mark;

        members[0].OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        members[1].OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        _guild.Settle();

        byte[] firstResult = Assert.Single(members[0].Since(firstMark, GuildWire.GuildActionResultId));
        byte[] secondResult = Assert.Single(members[1].Since(secondMark, GuildWire.GuildActionResultId));
        byte[] results = new[] { firstResult, secondResult }
            .Select(packet => GuildWire.ReadActionResult(packet).Result)
            .Order()
            .ToArray();
        // D4 checks the expected revision before the level, so the loser is Stale.
        Assert.Equal(new[] { GuildCode.Success, GuildCode.Stale }, results);
        GuildSnapshot after = _guild.Query(members[2]);
        Assert.Equal(Price, after.Treasury);
        Assert.Equal(2, after.StrongholdLevel);
    }

    [Fact]
    public void InsufficientTreasury_IsRefused_WithoutAnyChange()
    {
        GuildClient[] members = _guild.JoinMembers(2);
        GuildSnapshot before = _guild.Query(members[0]);
        Assert.Equal(GuildCode.InsufficientCurrency, before.StrongholdAvailability);

        GuildActionResult result = AssertRefused(members[0], before, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(GuildCode.InsufficientCurrency, result.Result);
    }

    [Fact]
    public void AtMaxLevel_AnotherUpgradeIsMaxLevel_WithoutAnyChange()
    {
        GuildClient[] members = _guild.JoinMembers(12);
        _guild.UpgradeStronghold(members[0]);
        GuildSnapshot before = _guild.Query(members[1]);
        Assert.Equal(GuildCode.MaxLevel, before.StrongholdAvailability);

        GuildActionResult result = AssertRefused(members[1], before, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(GuildCode.MaxLevel, result.Result);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(uint.MaxValue)]
    public void StaleRevision_IsRefused_WithoutAnyChange(uint offset)
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildSnapshot before = _guild.Query(members[0]);

        GuildActionResult result = AssertRefused(members[0], before, GuildWire.StrongholdUpgrade(unchecked(before.GuildRevision + offset)));

        Assert.Equal(GuildCode.Stale, result.Result);
    }

    [Fact]
    public void OutsideTown_IsNotInTown_WithoutAnyChange()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        _guild.MoveToHuntingGround(members[0]);
        GuildSnapshot before = _guild.Query(members[0]);
        // D4 without the revision check: NotInTown comes before every later refusal.
        Assert.Equal(GuildCode.NotInTown, before.StrongholdAvailability);

        GuildActionResult result = AssertRefused(members[0], before, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(GuildCode.NotInTown, result.Result);
    }

    // Fixture limit: today a lethal hit revives the player inside the same call, so no external input
    // leaves a lasting dead player. HP 0 is written directly to observe only the D4 「생존」 check.
    [Fact]
    public void DeadMember_IsDead_WithoutAnyChange()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildSnapshot before = _guild.Query(members[0]);
        _guild.PlayerOf(members[0]).Hp = 0;

        GuildActionResult result = AssertRefused(members[0], before, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(GuildCode.Dead, result.Result);
    }

    // D3/D4: the request is accepted while the buyer is in Town, then the same tick's map job starts
    // the portal move before the guild queue drains, so the guild job finds the buyer in transit.
    [Fact]
    public void RequestDrainedWhileMovingBetweenMaps_IsUnavailable_WithoutAnyChange()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient buyer = members[0];
        GuildSnapshot before = _guild.Query(buyer);
        _guild.PlaceAtTownPortal(buyer);
        _guild.Settle();
        int mark = buyer.Mark;

        buyer.OnRecvPacket(GuildTestWorld.EnterTownPortalToHuntingGround());
        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        _guild.Settle();

        GuildActionResult result = GuildTestWorld.SingleResult(
            buyer.Since(mark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Equal(GuildCode.Unavailable, result.Result);
        AssertUnchanged(before, _guild.Query(members[1]));
    }

    // D8 business refusal: 47 with the request echoed and the current revisions, then the current 44.
    GuildActionResult AssertRefused(GuildClient buyer, GuildSnapshot before, byte[] request)
    {
        uint requested = GuildWire.ReadUInt32Payload(request);
        List<byte[]> replies = _guild.Act(buyer, request);

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        GuildSnapshot current = GuildTestWorld.ReadSnapshot(replies[1]);
        Assert.Equal(GuildAction.StrongholdUpgrade, result.Action);
        Assert.NotEqual(GuildCode.Success, result.Result);
        Assert.Equal(requested, result.RequestedRevision);
        Assert.Equal(before.GuildRevision, result.GuildRevision);
        Assert.Equal(before.InventoryRevision, result.InventoryRevision);
        Assert.Equal(current.Sequence, result.Sequence);
        AssertUnchanged(before, current);
        AssertUnchanged(before, _guild.Query(buyer));
        return result;
    }

    static void AssertUnchanged(GuildSnapshot before, GuildSnapshot after)
    {
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.StrongholdLevel, after.StrongholdLevel);
        Assert.Equal(before.BlacksmithUnlocked, after.BlacksmithUnlocked);
    }

    static void AssertUpgraded(GuildSnapshot snapshot, uint guildRevision, int treasury)
    {
        Assert.Equal(guildRevision, snapshot.GuildRevision);
        Assert.Equal(treasury, snapshot.Treasury);
        Assert.Equal(2, snapshot.StrongholdLevel);
        Assert.Equal(1, snapshot.BlacksmithUnlocked);
        Assert.Equal(0, snapshot.StrongholdCost);
    }
}
