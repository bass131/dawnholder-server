using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Q1 follow-up of I8 with D1 and D4 (technical contract v1): the communal treasury cap 1,000,000,000 and
// the communal revision limit uint.MaxValue. Before the Content clear server is merged, the only guild
// credit is the admission seed 5 of a new account, so the cap is reached through real admissions here; the
// clear credit +10 belongs to the follow-up contract after that merge and is neither written nor run.
//   - A seed that fits only partly fills the treasury to the cap and advances the revision once; a seed with
//     no room credits 0 and keeps the revision, even at uint.MaxValue (saturation is not a failure).
//   - A new account whose seed needs a revision above uint.MaxValue is not admitted: the connection closes,
//     and no member, seed, online count or push is published (internal exhaustion, not a business refusal).
//   - A stronghold purchase at uint.MaxValue is refused in the D4 order and changes nothing; the revision
//     never wraps, including after a real purchase moves it from uint.MaxValue - 1 to the limit.
// Preparation is the reflection of GuildBoundaryFixture (GuildRegistry._state only). Admission is the real
// handshake → login → class selection → entry tick, purchases are real 45 bytes, and all observations are
// the 44/47 the server sends. Expected values are approved literals and before/after relations.
[Collection("GameWorldRegistryTests")]
public sealed class GuildEconomyBoundaryTests : IDisposable
{
    const int Price = 30;
    const int Cap = GuildWire.MaxTreasury;
    const uint PreparedRevision = 7;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    // The room is at most one seed, so the approved min(seed, room) credit fills the treasury exactly to Cap.
    [Theory]
    [InlineData(5)]
    [InlineData(3)]
    public void NewAccountSeedWithRoomUpToOneSeed_FillsTheTreasuryToTheCap_AndAdvancesTheRevision(int room)
    {
        GuildClient watcher = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildBoundaryFixture.ReplaceCommunalState(_guild, PreparedRevision, Cap - room, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(watcher);
        Assert.Equal(Cap - room, before.Treasury);
        Assert.Equal(PreparedRevision, before.GuildRevision);
        int watcherMark = watcher.Mark;

        GuildClient newcomer = _guild.JoinAndEnter(CharacterClass.Knight);

        GuildSnapshot admitted = FirstSnapshot(newcomer);
        Assert.Equal(ChangeKind.Membership, admitted.ChangeKind);
        Assert.Equal(Cap, admitted.Treasury);
        Assert.True(admitted.GuildRevision > before.GuildRevision, "a real seed credit advances the guild revision");
        Assert.Equal(before.MemberCount + 1, admitted.MemberCount);
        Assert.Equal(before.OnlineMemberCount + 1, admitted.OnlineMemberCount);
        GuildSnapshot pushed = SinglePush(watcher, watcherMark, ChangeKind.Membership);
        Assert.Equal(admitted.GuildRevision, pushed.GuildRevision);
        Assert.Equal(Cap, pushed.Treasury);
        Assert.Equal(admitted.MemberCount, pushed.MemberCount);
    }

    // Saturation credits 0 and needs no revision, so it is admitted even when the revision is at its limit.
    [Theory]
    [InlineData(PreparedRevision)]
    [InlineData(uint.MaxValue)]
    public void NewAccountSeedAtTheCap_CreditsNothing_AndKeepsTheGuildRevision(uint revision)
    {
        GuildClient watcher = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildBoundaryFixture.ReplaceCommunalState(_guild, revision, Cap, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(watcher);
        int watcherMark = watcher.Mark;

        GuildClient newcomer = _guild.JoinAndEnter(CharacterClass.Knight);

        Assert.False(newcomer.IsClosing, "a saturated seed is a normal admission");
        GuildSnapshot admitted = FirstSnapshot(newcomer);
        Assert.Equal(ChangeKind.Membership, admitted.ChangeKind);
        Assert.Equal(Cap, admitted.Treasury);
        Assert.Equal(revision, admitted.GuildRevision);
        Assert.Equal(before.MemberCount + 1, admitted.MemberCount);
        Assert.Equal(before.OnlineMemberCount + 1, admitted.OnlineMemberCount);
        GuildSnapshot pushed = SinglePush(watcher, watcherMark, ChangeKind.Membership);
        Assert.Equal(revision, pushed.GuildRevision);
        Assert.Equal(Cap, pushed.Treasury);
        Assert.Equal(admitted.MemberCount, pushed.MemberCount);
    }

    // D1: the seed needs a new revision that does not exist. Nothing of the admission is published, the
    // revision does not wrap, and the closed connection's later guild input is dropped (requirement 6).
    [Fact]
    public void NewAccountWithRoomButRevisionAtUintMax_IsNotAdmitted_AndPublishesNothing()
    {
        GuildClient watcher = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildBoundaryFixture.ReplaceCommunalState(_guild, uint.MaxValue, 1_000, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(watcher);
        InventorySnapshot watcherInventory = _guild.QueryInventory(watcher);
        int watcherMark = watcher.Mark;

        GuildClient rejected = AttemptEntry();

        AssertNotAdmitted(rejected);
        Assert.Empty(watcher.Since(watcherMark, GuildWire.GuildSnapshotId));
        GuildSnapshot after = _guild.Query(watcher);
        AssertCommunalUnchanged(before, after);
        Assert.Equal(uint.MaxValue, after.GuildRevision);
        Assert.Equal(before.MemberCount, after.MemberCount);
        Assert.Equal(before.OnlineMemberCount, after.OnlineMemberCount);
        InventorySnapshot watcherInventoryAfter = _guild.QueryInventory(watcher);
        Assert.Equal(watcherInventory.Revision, watcherInventoryAfter.Revision);
        Assert.Equal(watcherInventory.Currency, watcherInventoryAfter.Currency);

        int lateMark = rejected.Mark;
        rejected.OnRecvPacket(GuildWire.GuildRequest());
        rejected.OnRecvPacket(GuildWire.StrongholdUpgrade(uint.MaxValue));
        _guild.Settle();
        Assert.Empty(rejected.Since(lateMark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        AssertCommunalUnchanged(before, _guild.Query(watcher));
    }

    // D1 / I1: a returning member already received its seed, so it needs no revision and is admitted at the limit.
    [Fact]
    public void ReturningMemberWithRevisionAtUintMax_IsAdmittedWithoutASecondSeed()
    {
        GuildClient watcher = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient leaver = _guild.JoinAndEnter(CharacterClass.Knight);
        leaver.Close();
        _guild.Settle();
        GuildBoundaryFixture.ReplaceCommunalState(_guild, uint.MaxValue, 1_000, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(watcher);
        Assert.Equal(2, before.MemberCount);
        Assert.Equal(1, before.OnlineMemberCount);
        int watcherMark = watcher.Mark;

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, leaver.AccountName);

        GuildSnapshot admitted = FirstSnapshot(back);
        Assert.Equal(ChangeKind.Presence, admitted.ChangeKind);
        Assert.Equal(before.Treasury, admitted.Treasury);
        Assert.Equal(uint.MaxValue, admitted.GuildRevision);
        Assert.Equal(2, admitted.MemberCount);
        Assert.Equal(2, admitted.OnlineMemberCount);
        GuildSnapshot pushed = SinglePush(watcher, watcherMark, ChangeKind.Presence);
        Assert.Equal(before.Treasury, pushed.Treasury);
        Assert.Equal(uint.MaxValue, pushed.GuildRevision);
    }

    // D4 / D8: a payable purchase at the revision limit is a business refusal 47 → current 44, shown in advance
    // as the snapshot availability, and no other member receives an upgrade push.
    [Fact]
    public void StrongholdUpgradeAtRevisionUintMax_IsRevisionExhausted_AndChangesNothing()
    {
        GuildClient[] members = _guild.JoinMembers(2);
        GuildClient buyer = members[0];
        GuildBoundaryFixture.ReplaceCommunalState(_guild, uint.MaxValue, 1_000, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(buyer);
        Assert.Equal(GuildCode.RevisionExhausted, before.StrongholdAvailability);
        int watcherMark = members[1].Mark;

        GuildActionResult result = AssertRefused(buyer, before, uint.MaxValue);

        Assert.Equal(GuildCode.RevisionExhausted, result.Result);
        Assert.Equal(uint.MaxValue, result.GuildRevision);
        Assert.Empty(members[1].Since(watcherMark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        AssertCommunalUnchanged(before, _guild.Query(members[1]));
    }

    // D4 order around the limit: expected revision, then highest level, then revision exhaustion, then currency.
    [Theory]
    [InlineData((byte)1, 1_000, uint.MaxValue - 1, GuildCode.Stale)]
    [InlineData((byte)2, 1_000, uint.MaxValue, GuildCode.MaxLevel)]
    [InlineData((byte)1, 10, uint.MaxValue, GuildCode.RevisionExhausted)]
    public void RefusalAtTheRevisionLimit_FollowsTheD4Order(byte level, int treasury, uint expectedRevision, byte expectedResult)
    {
        GuildClient buyer = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildBoundaryFixture.ReplaceCommunalState(_guild, uint.MaxValue, treasury, level);
        GuildSnapshot before = _guild.Query(buyer);

        GuildActionResult result = AssertRefused(buyer, before, expectedRevision);

        Assert.Equal(expectedResult, result.Result);
    }

    // The only uint above uint.MaxValue - 1 is the limit itself, so a committed purchase there reaches it;
    // after that a seed with room cannot be credited and the admission is refused without a wrap.
    [Fact]
    public void PurchaseOneBelowTheRevisionLimit_ReachesUintMax_ThenANewSeedIsRefusedWithoutWrap()
    {
        GuildClient[] members = _guild.JoinMembers(2);
        GuildClient buyer = members[0];
        const int Prepared = 40;
        GuildBoundaryFixture.ReplaceCommunalState(_guild, uint.MaxValue - 1, Prepared, strongholdLevel: 1);
        GuildSnapshot before = _guild.Query(buyer);

        List<byte[]> replies = _guild.Act(buyer, GuildWire.StrongholdUpgrade(uint.MaxValue - 1));

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.Equal(uint.MaxValue, result.GuildRevision);
        GuildSnapshot upgraded = _guild.Query(members[1]);
        Assert.Equal(2, upgraded.StrongholdLevel);
        Assert.Equal(Prepared - Price, upgraded.Treasury);
        Assert.Equal(uint.MaxValue, upgraded.GuildRevision);

        GuildClient rejected = AttemptEntry();

        AssertNotAdmitted(rejected);
        GuildSnapshot after = _guild.Query(members[1]);
        AssertCommunalUnchanged(upgraded, after);
        Assert.Equal(before.MemberCount, after.MemberCount);
        Assert.Equal(before.OnlineMemberCount, after.OnlineMemberCount);
    }

    // Real handshake → login → class selection → entry tick, for an account whose admission should fail.
    // GuildTestWorld.JoinAndEnter is not used because its entry check cannot tell a refused admission apart.
    GuildClient AttemptEntry()
    {
        GuildClient client = _guild.Connect(_guild.NewAccountName());
        Assert.True(client.IsLoggedIn, "fixture: the real account login must be accepted");
        client.OnRecvPacket(GuildBoundaryFixture.SelectKnight());
        _guild.Tick();
        _guild.Settle();
        return client;
    }

    // D1: the connection closes and receives neither the map entry nor any guild snapshot.
    static void AssertNotAdmitted(GuildClient client)
    {
        Assert.True(client.IsClosing, "an admission whose seed cannot be published closes the connection");
        Assert.Empty(client.Since(0, (ushort)PacketID.S_EnterMap));
        Assert.Empty(client.Since(0, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
    }

    // D8 business refusal: 47 with the request echoed and the current revisions, then the current 44.
    GuildActionResult AssertRefused(GuildClient buyer, GuildSnapshot before, uint requestedRevision)
    {
        List<byte[]> replies = _guild.Act(buyer, GuildWire.StrongholdUpgrade(requestedRevision));

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        GuildSnapshot current = GuildTestWorld.ReadSnapshot(replies[1]);
        Assert.Equal(GuildAction.StrongholdUpgrade, result.Action);
        Assert.Equal(requestedRevision, result.RequestedRevision);
        Assert.Equal(before.GuildRevision, result.GuildRevision);
        Assert.Equal(before.InventoryRevision, result.InventoryRevision);
        Assert.Equal(current.Sequence, result.Sequence);
        Assert.Equal(ChangeKind.Query, current.ChangeKind);
        AssertCommunalUnchanged(before, current);
        AssertCommunalUnchanged(before, _guild.Query(buyer));
        return result;
    }

    static GuildSnapshot FirstSnapshot(GuildClient client)
        => GuildTestWorld.ReadSnapshot(client.Since(0, GuildWire.GuildSnapshotId).First());

    static GuildSnapshot SinglePush(GuildClient client, int mark, byte kind)
        => Assert.Single(
            client.Since(mark, GuildWire.GuildSnapshotId).Select(GuildTestWorld.ReadSnapshot),
            snapshot => snapshot.ChangeKind == kind);

    static void AssertCommunalUnchanged(GuildSnapshot before, GuildSnapshot after)
    {
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.StrongholdLevel, after.StrongholdLevel);
        Assert.Equal(before.BlacksmithUnlocked, after.BlacksmithUnlocked);
    }
}
