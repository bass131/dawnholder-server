using Dawnholder.Server.GameServer.Maps;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Guild.GuildDungeonClearTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// A real close differs from a Send exception: later personal sends are suppressed and close
// cleanup removes that connection's inventory. Communal credit remains committed exactly once.
[Collection("GameWorldRegistryTests")]
public sealed class GuildClearPublicationBoundaryTests
{
    [Theory]
    [InlineData((ushort)39)]
    [InlineData((ushort)36)]
    [InlineData((ushort)44)]
    public void CloseDuringClearPublication_PreservesBothCreditsAndResetsOnlyReconnectingPersonalState(ushort closeAt)
    {
        using GuildDungeonClearTestWorld clear = new();
        GuildTestWorld guild = clear.Guild;
        GuildFaultClient closing = GuildBoundaryFixture.EnterFaultClient(guild);
        GuildClient beneficiary = guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient observer = guild.JoinAndEnter(CharacterClass.Knight);
        clear.FormParty(closing, beneficiary);
        guild.UnlockBossRoom(closing);
        GameMap room = clear.EnterSameBossRoom(closing, beneficiary);
        InventorySnapshot before = clear.QueryInventory(beneficiary);
        GuildSnapshot communalBefore = guild.Query(observer);
        int closingMark = closing.Mark;
        int beneficiaryMark = beneficiary.Mark;
        int observerMark = observer.Mark;

        closing.CloseOnSendOf(closeAt);
        guild.KillBoss(room, beneficiary);
        List<byte[]> beneficiaryReplies = EconomySince(beneficiary, beneficiaryMark);
        List<byte[]> closingReplies = EconomySince(closing, closingMark);
        GuildSnapshot[] credits = SnapshotsSince(observer, observerMark)
            .Where(snapshot => snapshot.ChangeKind == ChangeKind.TreasuryCredit).ToArray();

        Assert.True(closing.ClosedBySend, "the selected 39/36/44 boundary must have been reached");
        Assert.True(closing.IsClosing);
        ushort[] deliveredPrefix = closeAt switch
        {
            39 => Array.Empty<ushort>(),
            36 => new ushort[] { 39 },
            44 => new ushort[] { 39, 36 },
            _ => throw new ArgumentOutOfRangeException(nameof(closeAt)),
        };
        Assert.Equal(deliveredPrefix, GuildTestWorld.Ids(closingReplies));
        Assert.Equal(15, communalBefore.Treasury);
        Assert.Equal(new[] { 25, 35 }, credits.Select(snapshot => snapshot.Treasury));
        Assert.Equal(new[] { communalBefore.GuildRevision + 1, communalBefore.GuildRevision + 2 },
            credits.Select(snapshot => snapshot.GuildRevision));
        Assert.All(credits, snapshot => Assert.Equal(0u, snapshot.InventoryRevision));

        int resultIndex = beneficiaryReplies.FindIndex(packet => GuildWire.IdOf(packet) == 39);
        Assert.True(resultIndex >= 0, "the surviving beneficiary must receive the actual clear result");
        byte[][] ownPublication = beneficiaryReplies.Skip(resultIndex).Take(3).ToArray();
        Assert.Equal(new ushort[] { 39, 36, 44 }, ownPublication.Select(GuildWire.IdOf));
        S_DungeonClearResult result = GuildTestWorld.ReadClearResult(ownPublication[0]);
        Assert.Equal((byte)0, result.result);
        Assert.Equal(50, result.currency);
        InventorySnapshot paid = GuildWire.ReadInventorySnapshot(ownPublication[1]);
        Assert.Equal(before.Revision + 1, paid.Revision);
        Assert.Equal(before.Currency + 50, paid.Currency);
        Assert.Equal(1, paid.CountOf(1));
        Assert.Equal(1, paid.CountOf(2));
        GuildSnapshot ownCredit = GuildWire.ReadSnapshot(ownPublication[2]);
        Assert.Equal(ChangeKind.TreasuryCredit, ownCredit.ChangeKind);
        Assert.Equal(35, ownCredit.Treasury);
        Assert.Equal(paid.Revision, ownCredit.InventoryRevision);

        guild.Settle();
        GuildSnapshot afterClose = guild.Query(observer);
        Assert.Equal(-1, closing.EntityId);
        Assert.Equal(35, afterClose.Treasury);
        Assert.Equal(3, afterClose.MemberCount);
        Assert.Equal(2, afterClose.OnlineMemberCount);

        GuildClient returned = guild.JoinAndEnter(CharacterClass.Knight, closing.AccountName);
        GuildSnapshot returnedView = GuildWire.ReadSnapshot(Assert.Single(returned.Since(0, 44)));
        InventorySnapshot fresh = guild.QueryInventory(returned);
        Assert.Equal(ChangeKind.Presence, returnedView.ChangeKind);
        Assert.Equal(35, returnedView.Treasury);
        Assert.Equal(3, returnedView.MemberCount);
        Assert.Equal(3, returnedView.OnlineMemberCount);
        Assert.Equal(0u, fresh.Revision);
        Assert.Equal(0, fresh.Currency);
        Assert.Empty(fresh.Counts);
        Assert.Equal(0, returnedView.WeaponLevel);

        int laterMark = observer.Mark;
        beneficiary.OnRecvPacket(GuildTestWorld.ClearConfirm());
        beneficiary.OnRecvPacket(GuildTestWorld.ClearConfirm());
        returned.OnRecvPacket(GuildTestWorld.ClearConfirm());
        guild.Settle();
        GuildSnapshot final = guild.Query(observer);
        InventorySnapshot stillPaid = guild.QueryInventory(beneficiary);
        Assert.Equal(35, final.Treasury);
        Assert.Equal(afterClose.GuildRevision, final.GuildRevision);
        Assert.DoesNotContain(SnapshotsSince(observer, laterMark),
            snapshot => snapshot.ChangeKind == ChangeKind.TreasuryCredit);
        Assert.Equal(paid.Revision, stillPaid.Revision);
        Assert.Equal(paid.Currency, stillPaid.Currency);
        Assert.Equal(paid.CountOf(1), stillPaid.CountOf(1));
        Assert.Equal(paid.CountOf(2), stillPaid.CountOf(2));
        Assert.Empty(returned.Since(0, 39));
    }
}
