using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Q1 follow-up of D1, D3, D5, D8, D10 and I7 (technical contract v1): what a failed guild job leaves behind.
// Every request really goes receive → GameSession pending slot → World queue → guild tick. Faults enter
// only at the transport (GuildFaultClient) or through the reflection preparation of GuildBoundaryFixture:
//   - a send exception, or a socket close at a given packet ID,
//   - a socket close that lands inside the guild job at its location read, i.e. after the job's first
//     owner check and before the last one (the close that races the World tick),
//   - Connection.Sequence at long.MaxValue, so the caller's or a peer's next 44 cannot be numbered.
// Observed contract: no commit before the last owner check, no rollback or retry after the commit, one
// failing peer neither undoes a purchase nor blocks other peers, the guild pending slot is released after
// every outcome while the inventory slot stays separate, and nothing carries over to a later connection.
// Reach limits, reported and not tested: (1) a preparation exception that keeps the connection open has
// no injection point within the allowed preparation; the reachable one, sequence exhaustion, closes the
// connection by contract. (2) A 47 NotMember (2) has no public trigger: input of a closing, old or not yet
// entered connection is dropped before the guild job (GameSession receive gate, EnqueueRequest owner
// check), and a close inside the job stops before the send. The tests therefore expect a drop, not a 47.
// The internal NotMember branches (GuildRegistry.ReadLocation, InventoryRegistry.PrepareEnhancement) are
// covered only by reading the code, never by calling them directly.
[Collection("GameWorldRegistryTests")]
public sealed class GuildFailureLifetimeTests : IDisposable
{
    const int Seed = 5;
    const int Price = 30;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    // D3: the slot shared by 43/45/46 is free again after a success, a refusal and a send exception alike.
    [Theory]
    [InlineData("success")]
    [InlineData("refusal")]
    [InlineData("query-send-exception")]
    [InlineData("result-send-exception")]
    public void EveryGuildJobOutcome_ReleasesThePendingSlot_ForTheNextRequest(string outcome)
    {
        GuildFaultClient actor = GuildBoundaryFixture.EnterFaultClient(_guild);
        _guild.JoinMembers(5);
        GuildSnapshot before = GuildBoundaryFixture.QueryAs(_guild, actor);
        Assert.Equal(6 * Seed, before.Treasury);
        int mark = actor.Mark;

        switch (outcome)
        {
            case "success":
                Assert.Equal(GuildCode.Success, ResultOf(GuildBoundaryFixture.ActAs(_guild, actor, GuildWire.StrongholdUpgrade(before.GuildRevision))));
                break;
            case "refusal":
                Assert.Equal(GuildCode.Stale, ResultOf(GuildBoundaryFixture.ActAs(_guild, actor, GuildWire.StrongholdUpgrade(before.GuildRevision + 1))));
                break;
            case "query-send-exception":
                actor.ThrowOnSendOf(GuildWire.GuildSnapshotId);
                actor.OnRecvPacket(GuildWire.GuildRequest());
                _guild.Settle();
                Assert.Equal(1, actor.ThrownSends);
                break;
            case "result-send-exception":
                actor.ThrowOnSendOf(GuildWire.GuildActionResultId);
                actor.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
                _guild.Settle();
                Assert.Equal(1, actor.ThrownSends);
                Assert.Empty(actor.Since(mark, GuildWire.GuildActionResultId));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }
        actor.ClearFaults();

        GuildSnapshot next = GuildBoundaryFixture.QueryAs(_guild, actor);

        Assert.False(actor.IsClosing, "a send exception or a refusal does not close the connection");
        Assert.True(next.Sequence > before.Sequence, "the next query is answered on the same connection");
    }

    // D3 / I7: a send exception on one economy slot releases that slot and does not hold the other one.
    [Theory]
    [InlineData(GuildWire.GuildSnapshotId, GuildWire.InventorySnapshotId)]
    [InlineData(GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId)]
    public void SendExceptionOnOneEconomySlot_LeavesTheOtherAnswered_AndBothSlotsReusable(ushort failing, ushort other)
    {
        GuildFaultClient member = GuildBoundaryFixture.EnterFaultClient(_guild);
        _guild.Settle();
        member.ThrowOnSendOf(failing);
        int mark = member.Mark;

        member.OnRecvPacket(GuildWire.InventoryRequest());
        member.OnRecvPacket(GuildWire.GuildRequest());
        _guild.Settle();

        Assert.Equal(1, member.ThrownSends);
        Assert.Empty(member.Since(mark, failing));
        Assert.Single(member.Since(mark, other));

        member.ClearFaults();
        int retryMark = member.Mark;
        member.OnRecvPacket(GuildWire.InventoryRequest());
        member.OnRecvPacket(GuildWire.GuildRequest());
        _guild.Settle();

        Assert.Single(member.Since(retryMark, GuildWire.InventorySnapshotId));
        Assert.Single(member.Since(retryMark, GuildWire.GuildSnapshotId));
    }

    // D5 / D8: the buyer's next 44 cannot be numbered, so the purchase is not committed and the connection
    // closes. The next job of the same drain is still answered, and the account's next connection starts at
    // sequence 1 and buys normally, without the old job being applied again.
    [Fact]
    public void CallerSequenceAtLongMax_ClosesBeforeCommit_AndLaterJobsAndConnectionsAreServed()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient buyer = members[0];
        GuildClient bystander = members[1];
        GuildSnapshot before = _guild.Query(bystander);
        _guild.Settle();
        GuildBoundaryFixture.SetSnapshotSequence(_guild, buyer, long.MaxValue);
        int buyerMark = buyer.Mark;
        int bystanderMark = bystander.Mark;

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        bystander.OnRecvPacket(GuildWire.GuildRequest());
        _guild.Tick(); // both jobs are drained by this World tick, the buyer's first

        Assert.True(buyer.IsClosing, "an exhausted snapshot sequence closes the caller's connection");
        Assert.Empty(buyer.Since(buyerMark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        GuildSnapshot answered = GuildTestWorld.ReadSnapshot(Assert.Single(bystander.Since(bystanderMark, GuildWire.GuildSnapshotId)));
        Assert.Equal(ChangeKind.Query, answered.ChangeKind);
        AssertCommunalUnchanged(before, answered);

        _guild.Settle();
        Assert.DoesNotContain(Snapshots(bystander, bystanderMark), snapshot => snapshot.ChangeKind == ChangeKind.StrongholdUpgrade);
        GuildSnapshot after = _guild.Query(bystander);
        AssertCommunalUnchanged(before, after);
        Assert.Equal(6, after.MemberCount);
        Assert.Equal(5, after.OnlineMemberCount);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, buyer.AccountName);
        Assert.Equal(1, Snapshots(back, 0)[0].Sequence);
        GuildActionResult result = GuildTestWorld.SingleResult(_guild.Act(back, GuildWire.StrongholdUpgrade(after.GuildRevision)));
        Assert.Equal(GuildCode.Success, result.Result);
    }

    // D8 / I9: a peer whose next 44 cannot be numbered is closed; the buyer keeps its committed purchase and
    // every later peer still receives the upgrade. Peers are visited in admission order, so members[2..5]
    // come after the exhausted members[1].
    [Fact]
    public void PeerSequenceAtLongMax_DoesNotUndoThePurchase_OrBlockLaterPeers()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient buyer = members[0];
        GuildClient exhausted = members[1];
        GuildSnapshot before = _guild.Query(buyer);
        _guild.Settle();
        GuildBoundaryFixture.SetSnapshotSequence(_guild, exhausted, long.MaxValue);
        Dictionary<GuildClient, int> marks = members.ToDictionary(member => member, member => member.Mark);

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        _guild.Tick(); // the purchase job and its broadcast run in this World tick

        List<byte[]> replies = buyer.Since(marks[buyer], GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId);
        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.True(exhausted.IsClosing, "an exhausted peer sequence closes only that peer");
        Assert.Empty(exhausted.Since(marks[exhausted], GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        foreach (GuildClient peer in members.Skip(2))
            AssertUpgradePush(peer, marks[peer], result.GuildRevision);

        _guild.Settle();
        GuildSnapshot after = _guild.Query(members[5]);
        AssertUpgraded(after, result.GuildRevision, treasury: (6 * Seed) - Price);
        Assert.Equal(6, after.MemberCount);
        Assert.Equal(5, after.OnlineMemberCount);
    }

    // D8: a send exception to one peer is isolated; later peers receive the push, the failed push is not
    // retried, and the peer's own next query shows the committed state.
    [Fact]
    public void PeerSendException_DoesNotUndoThePurchase_OrBlockLaterPeers_AndIsNotRetried()
    {
        GuildClient buyer = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildFaultClient failingPeer = GuildBoundaryFixture.EnterFaultClient(_guild);
        GuildClient[] laterPeers = _guild.JoinMembers(4);
        GuildSnapshot before = _guild.Query(buyer);
        failingPeer.ThrowOnSendOf(GuildWire.GuildSnapshotId);
        int peerMark = failingPeer.Mark;
        Dictionary<GuildClient, int> marks = laterPeers.ToDictionary(peer => peer, peer => peer.Mark);

        List<byte[]> replies = _guild.Act(buyer, GuildWire.StrongholdUpgrade(before.GuildRevision));

        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(replies));
        GuildActionResult result = GuildWire.ReadActionResult(replies[0]);
        Assert.Equal(GuildCode.Success, result.Result);
        Assert.Equal(1, failingPeer.ThrownSends);
        Assert.False(failingPeer.IsClosing, "a send exception to a peer is isolated, not a close");
        foreach (GuildClient peer in laterPeers)
            AssertUpgradePush(peer, marks[peer], result.GuildRevision);

        failingPeer.ClearFaults();
        _guild.Settle();
        Assert.Empty(failingPeer.Since(peerMark, GuildWire.GuildSnapshotId));
        AssertUpgraded(GuildBoundaryFixture.QueryAs(_guild, failingPeer), result.GuildRevision, treasury: (6 * Seed) - Price);
    }

    // D5 / D10: the buyer's 47 fails after the commit. The purchase stays, other members are still told,
    // nothing is retried or refunded, and a second purchase meets the highest level.
    [Fact]
    public void BuyerResultSendException_KeepsTheCommittedPurchase_WithoutRetryOrSecondSpend()
    {
        GuildFaultClient buyer = GuildBoundaryFixture.EnterFaultClient(_guild);
        GuildClient[] peers = _guild.JoinMembers(5);
        GuildSnapshot before = GuildBoundaryFixture.QueryAs(_guild, buyer);
        buyer.ThrowOnSendOf(GuildWire.GuildActionResultId);
        int buyerMark = buyer.Mark;
        Dictionary<GuildClient, int> marks = peers.ToDictionary(peer => peer, peer => peer.Mark);

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        _guild.Settle();

        Assert.Equal(1, buyer.ThrownSends);
        foreach (GuildClient peer in peers)
            AssertUpgradePush(peer, marks[peer], revisionAfter: null);
        uint committedRevision = Snapshots(peers[0], marks[peers[0]])
            .Single(snapshot => snapshot.ChangeKind == ChangeKind.StrongholdUpgrade).GuildRevision;

        buyer.ClearFaults();
        _guild.Settle();
        Assert.Empty(buyer.Since(buyerMark, GuildWire.GuildActionResultId));
        GuildSnapshot buyerView = GuildBoundaryFixture.QueryAs(_guild, buyer);
        AssertUpgraded(buyerView, committedRevision, treasury: (6 * Seed) - Price);

        List<byte[]> second = GuildBoundaryFixture.ActAs(_guild, buyer, GuildWire.StrongholdUpgrade(buyerView.GuildRevision));
        Assert.Equal(GuildCode.MaxLevel, ResultOf(second));
        AssertUpgraded(_guild.Query(peers[0]), committedRevision, treasury: (6 * Seed) - Price);
    }

    // D10: the buyer's socket closes at the 47 after the commit. The committed purchase stays, other members
    // are told, and the returning account sees the same level 2 and treasury without a refund.
    [Fact]
    public void BuyerClosingAtTheResultSend_KeepsThePurchase_AndTheReturningAccountGetsNoRefund()
    {
        GuildFaultClient buyer = GuildBoundaryFixture.EnterFaultClient(_guild);
        GuildClient[] peers = _guild.JoinMembers(5);
        GuildSnapshot before = GuildBoundaryFixture.QueryAs(_guild, buyer);
        buyer.CloseOnSendOf(GuildWire.GuildActionResultId);
        Dictionary<GuildClient, int> marks = peers.ToDictionary(peer => peer, peer => peer.Mark);

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        _guild.Settle();

        Assert.True(buyer.ClosedBySend, "fixture: the close must happen at the result send");
        Assert.True(buyer.IsClosing);
        foreach (GuildClient peer in peers)
            AssertUpgradePush(peer, marks[peer], revisionAfter: null);
        GuildSnapshot seen = _guild.Query(peers[0]);
        AssertUpgraded(seen, seen.GuildRevision, treasury: (6 * Seed) - Price);
        Assert.True(seen.GuildRevision > before.GuildRevision, "the committed purchase advanced the revision");
        Assert.Equal(5, seen.OnlineMemberCount);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, buyer.AccountName);
        GuildSnapshot afterReturn = _guild.Query(back);
        AssertUpgraded(afterReturn, seen.GuildRevision, treasury: (6 * Seed) - Price);
        Assert.Equal(6, afterReturn.MemberCount);
        Assert.Equal(6, afterReturn.OnlineMemberCount);
    }

    // D10: a close inside the purchase job, after its first owner check and before the last one, commits
    // nothing and sends nothing; the returning account then buys with the same revision.
    [Fact]
    public void CloseInsideThePurchaseJobBeforeTheLastOwnerCheck_ChangesNothing()
    {
        GuildFaultClient buyer = GuildBoundaryFixture.EnterFaultClient(_guild);
        GuildClient[] peers = _guild.JoinMembers(5);
        GuildSnapshot before = GuildBoundaryFixture.QueryAs(_guild, buyer);
        int buyerMark = buyer.Mark;
        int peerMark = peers[0].Mark;

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        buyer.CloseAtNextGuildMapRead();
        _guild.Settle();

        Assert.True(buyer.ClosedByGuildMapRead, "fixture: the close must land inside the guild job");
        Assert.Empty(buyer.Since(buyerMark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.DoesNotContain(Snapshots(peers[0], peerMark), snapshot => snapshot.ChangeKind == ChangeKind.StrongholdUpgrade);
        GuildSnapshot after = _guild.Query(peers[0]);
        AssertCommunalUnchanged(before, after);
        Assert.Equal(5, after.OnlineMemberCount);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, buyer.AccountName);
        GuildActionResult result = GuildTestWorld.SingleResult(_guild.Act(back, GuildWire.StrongholdUpgrade(before.GuildRevision)));
        Assert.Equal(GuildCode.Success, result.Result);
    }

    // D1: a close inside the admission job publishes no member, seed, online count or push. The account was
    // never recorded, so its next real entry is a first admission with exactly one seed.
    [Fact]
    public void CloseInsideTheAdmissionJob_PublishesNoMemberSeedOrPresence()
    {
        GuildClient watcher = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildSnapshot before = _guild.Query(watcher);
        string account = _guild.NewAccountName();
        GuildFaultClient closing = GuildBoundaryFixture.ConnectFaultClient(_guild, account);
        int watcherMark = watcher.Mark;

        closing.OnRecvPacket(GuildBoundaryFixture.SelectKnight());
        closing.CloseAtNextGuildMapRead();
        _guild.Tick();
        _guild.Settle();

        Assert.True(closing.ClosedByGuildMapRead, "fixture: the close must land inside the admission job");
        Assert.Empty(closing.Since(0, (ushort)PacketID.S_EnterMap));
        Assert.Empty(closing.Since(0, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Empty(watcher.Since(watcherMark, GuildWire.GuildSnapshotId));
        GuildSnapshot after = _guild.Query(watcher);
        AssertCommunalUnchanged(before, after);
        Assert.Equal(1, after.MemberCount);
        Assert.Equal(1, after.OnlineMemberCount);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, account);
        GuildSnapshot admitted = Snapshots(back, 0)[0];
        Assert.Equal(ChangeKind.Membership, admitted.ChangeKind);
        Assert.Equal(2 * Seed, admitted.Treasury);
        Assert.Equal(2, admitted.MemberCount);
        Assert.Equal(2, admitted.OnlineMemberCount);
    }

    // Requirement 6 / D10: after the account returns, guild input on the old connection object is dropped,
    // reaches neither connection and changes nothing; the current connection acts normally.
    [Fact]
    public void OldOwnerInputAfterReconnect_IsDropped_AndOnlyTheCurrentConnectionActs()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient old = members[0];
        old.Close();
        _guild.Settle();
        GuildClient current = _guild.JoinAndEnter(CharacterClass.Knight, old.AccountName);
        GuildSnapshot before = _guild.Query(members[1]);
        int oldMark = old.Mark;
        int currentMark = current.Mark;

        Exception? thrown = Record.Exception(() =>
        {
            old.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
            old.OnRecvPacket(GuildWire.GuildRequest());
            old.OnRecvPacket(GuildWire.WeaponEnhance(before.InventoryRevision));
        });
        _guild.Settle();

        Assert.Null(thrown);
        Assert.Empty(old.Since(oldMark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Empty(current.Since(currentMark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        GuildSnapshot after = _guild.Query(members[1]);
        AssertCommunalUnchanged(before, after);

        GuildActionResult result = GuildTestWorld.SingleResult(_guild.Act(current, GuildWire.StrongholdUpgrade(after.GuildRevision)));
        Assert.Equal(GuildCode.Success, result.Result);
    }

    static byte ResultOf(List<byte[]> replies) => GuildTestWorld.SingleResult(replies).Result;

    static List<GuildSnapshot> Snapshots(GuildClient client, int mark)
        => client.Since(mark, GuildWire.GuildSnapshotId).Select(GuildTestWorld.ReadSnapshot).ToList();

    // D8: exactly one upgrade push, level 2 and the committed revision when the caller knows it.
    static void AssertUpgradePush(GuildClient peer, int mark, uint? revisionAfter)
    {
        GuildSnapshot push = Assert.Single(Snapshots(peer, mark), snapshot => snapshot.ChangeKind == ChangeKind.StrongholdUpgrade);
        Assert.Equal(2, push.StrongholdLevel);
        Assert.Equal(1, push.BlacksmithUnlocked);
        if (revisionAfter.HasValue) Assert.Equal(revisionAfter.Value, push.GuildRevision);
    }

    static void AssertUpgraded(GuildSnapshot snapshot, uint guildRevision, int treasury)
    {
        Assert.Equal(guildRevision, snapshot.GuildRevision);
        Assert.Equal(treasury, snapshot.Treasury);
        Assert.Equal(2, snapshot.StrongholdLevel);
        Assert.Equal(1, snapshot.BlacksmithUnlocked);
    }

    static void AssertCommunalUnchanged(GuildSnapshot before, GuildSnapshot after)
    {
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.StrongholdLevel, after.StrongholdLevel);
        Assert.Equal(before.BlacksmithUnlocked, after.BlacksmithUnlocked);
    }
}
