using System.Buffers.Binary;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// I7 / G6 and the testable part of I10 (technical contract v1): the 43/45/46 handlers check the exact
// length, header and reserved byte before reading and drop malformed input, input before world entry
// and input of a closing connection without a reply. Each connection has one guild pending slot,
// separate from the existing inventory slot and released after every completion. Replies come from the
// World tick, never from the receive call. Every drop case ends with a valid request as a positive
// control, so a server that simply ignores 43..47 does not pass.
// Not covered here: the job-exception release path (no external trigger exists) and the tick-owner /
// no-I/O part of I10, which needs independent code review; neither is reported as tested.
[Collection("GameWorldRegistryTests")]
public sealed class GuildRequestBoundaryTests : IDisposable
{
    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    // I10: the receive call only queues; the 44 is produced by the World tick.
    [Fact]
    public void GuildQuery_IsAnsweredByTheWorldTick_NotByTheReceiveCall()
    {
        GuildClient member = _guild.JoinAndEnter(CharacterClass.Knight);
        int mark = member.Mark;

        member.OnRecvPacket(GuildWire.GuildRequest());
        Assert.Empty(member.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        _guild.Settle();

        byte[] reply = Assert.Single(member.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Equal(ChangeKind.Query, GuildTestWorld.ReadSnapshot(reply).ChangeKind);
    }

    // Each variant would be a valid, payable request if read leniently: an upgrade with treasury 30 and
    // the current revision, or an enhance with funds at an unlocked blacksmith.
    [Theory]
    [InlineData("query-header-only")]
    [InlineData("query-extra-byte")]
    [InlineData("query-reserved-nonzero")]
    [InlineData("query-size-field-mismatch")]
    [InlineData("upgrade-short")]
    [InlineData("upgrade-extra-byte")]
    [InlineData("upgrade-size-field-mismatch")]
    [InlineData("enhance-short")]
    [InlineData("enhance-extra-byte")]
    [InlineData("enhance-size-field-mismatch")]
    [InlineData("client-sends-snapshot")]
    [InlineData("client-sends-action-result")]
    public void MalformedGuildInput_IsDroppedWithoutReplyOrChange(string variant)
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient sender = members[0];
        if (variant.StartsWith("enhance", StringComparison.Ordinal))
        {
            _guild.UpgradeStronghold(members[1]);
            _guild.GainBossReward(sender);
        }
        GuildSnapshot before = _guild.Query(sender);
        InventorySnapshot inventoryBefore = _guild.QueryInventory(sender);
        int mark = sender.Mark;

        Exception? thrown = Record.Exception(() => sender.OnRecvPacket(Malformed(variant, before, inventoryBefore)));
        _guild.Settle();

        Assert.Null(thrown);
        Assert.Empty(sender.Since(mark, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        // Assumption shared with the existing economy handlers: invalid input is dropped, not a disconnect.
        Assert.False(sender.IsClosing, "invalid guild input should be dropped without closing the connection");
        GuildSnapshot after = _guild.Query(sender);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.StrongholdLevel, after.StrongholdLevel);
        Assert.Equal(before.WeaponLevel, after.WeaponLevel);
        Assert.Equal(inventoryBefore.Revision, after.InventoryRevision);
    }

    [Fact]
    public void GuildInputBeforeWorldEntry_IsDroppedWithoutReply()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildSnapshot before = _guild.Query(members[0]);
        GuildClient loggedInOnly = _guild.Connect(_guild.NewAccountName());
        GuildClient selectedNotEntered = _guild.Connect(_guild.NewAccountName());
        selectedNotEntered.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());

        foreach (GuildClient early in new[] { loggedInOnly, selectedNotEntered })
        {
            early.OnRecvPacket(GuildWire.GuildRequest());
            early.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
            early.OnRecvPacket(GuildWire.WeaponEnhance(0));
        }
        _guild.Settle();

        Assert.Empty(loggedInOnly.Since(0, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Empty(selectedNotEntered.Since(0, GuildWire.GuildActionResultId));
        Assert.DoesNotContain(selectedNotEntered.Since(0, GuildWire.GuildSnapshotId),
            packet => GuildTestWorld.ReadSnapshot(packet).ChangeKind == ChangeKind.Query);
        // Positive control: the class-selected connection has now entered, and its own query is answered.
        Assert.True(selectedNotEntered.EntityId >= 0, "fixture: the entry tick must have run");
        GuildSnapshot after = _guild.Query(selectedNotEntered);
        Assert.Equal(1, after.StrongholdLevel);
        Assert.Equal(7, after.MemberCount);
    }

    // D10: a close before the last owner check changes nothing; the same account's next connection
    // starts clean, and its own first request is answered normally.
    [Fact]
    public void UpgradeQueuedBeforeClose_IsUnanswered_AndChangesNothing()
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient buyer = members[0];
        GuildSnapshot before = _guild.Query(buyer);
        int mark = buyer.Mark;

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(before.GuildRevision));
        buyer.Close();
        _guild.Settle();

        Assert.Empty(buyer.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        GuildSnapshot seenByOther = _guild.Query(members[1]);
        Assert.Equal(1, seenByOther.StrongholdLevel);
        Assert.Equal(before.Treasury, seenByOther.Treasury);
        Assert.Equal(before.GuildRevision, seenByOther.GuildRevision);
        Assert.Equal(6, seenByOther.MemberCount);
        Assert.Equal(5, seenByOther.OnlineMemberCount);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, buyer.AccountName);
        GuildSnapshot afterReturn = _guild.Query(back);
        Assert.Equal(before.Treasury, afterReturn.Treasury);
        Assert.Equal(6, afterReturn.MemberCount);
        GuildActionResult result = GuildTestWorld.SingleResult(_guild.Act(back, GuildWire.StrongholdUpgrade(afterReturn.GuildRevision)));
        Assert.Equal(GuildCode.Success, result.Result);
    }

    // 43/45/46 share one pending slot per connection: a second request before the first completes is
    // dropped without a reply, and the slot is free again once the first has been answered.
    [Theory]
    [InlineData("query")]
    [InlineData("upgrade")]
    [InlineData("enhance")]
    public void SecondGuildRequestWhileOneIsPending_IsDropped_AndTheSlotIsReleasedAfterwards(string second)
    {
        GuildClient[] members = _guild.JoinMembers(6);
        GuildClient sender = members[0];
        GuildSnapshot before = _guild.Query(sender);
        int mark = sender.Mark;

        sender.OnRecvPacket(GuildWire.GuildRequest());
        sender.OnRecvPacket(second switch
        {
            "query" => GuildWire.GuildRequest(),
            "upgrade" => GuildWire.StrongholdUpgrade(before.GuildRevision),
            _ => GuildWire.WeaponEnhance(before.InventoryRevision),
        });
        _guild.Settle();

        byte[] only = Assert.Single(sender.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId));
        Assert.Equal(ChangeKind.Query, GuildTestWorld.ReadSnapshot(only).ChangeKind);
        Assert.Equal(1, _guild.Query(members[1]).StrongholdLevel);

        GuildActionResult afterRelease = GuildTestWorld.SingleResult(_guild.Act(sender, GuildWire.StrongholdUpgrade(before.GuildRevision)));
        Assert.Equal(GuildCode.Success, afterRelease.Result);
    }

    [Fact]
    public void Refusal_ReleasesThePendingSlot()
    {
        GuildClient[] members = _guild.JoinMembers(2);
        GuildSnapshot before = _guild.Query(members[0]);

        GuildActionResult refused = GuildTestWorld.SingleResult(_guild.Act(members[0], GuildWire.StrongholdUpgrade(before.GuildRevision)));

        Assert.Equal(GuildCode.InsufficientCurrency, refused.Result);
        Assert.Equal(before.Treasury, _guild.Query(members[0]).Treasury);
    }

    // The existing inventory slot stays separate: one 35 and one 43 in flight are both answered.
    [Fact]
    public void GuildAndInventoryRequests_HaveSeparatePendingSlots()
    {
        GuildClient member = _guild.JoinAndEnter(CharacterClass.Knight);
        int mark = member.Mark;

        member.OnRecvPacket(GuildWire.InventoryRequest());
        member.OnRecvPacket(GuildWire.GuildRequest());
        _guild.Settle();

        Assert.Single(member.Since(mark, GuildWire.InventorySnapshotId));
        Assert.Single(member.Since(mark, GuildWire.GuildSnapshotId));
    }

    static byte[] Malformed(string variant, GuildSnapshot guild, InventorySnapshot inventory)
    {
        byte[] upgradePayload = GuildWire.StrongholdUpgrade(guild.GuildRevision)[4..];
        byte[] enhancePayload = GuildWire.WeaponEnhance(inventory.Revision)[4..];
        return variant switch
        {
            "query-header-only" => GuildWire.Frame(GuildWire.GuildRequestId, Array.Empty<byte>()),
            "query-extra-byte" => GuildWire.Frame(GuildWire.GuildRequestId, new byte[] { 0, 0 }),
            "query-reserved-nonzero" => GuildWire.GuildRequest(reserved: 1),
            "query-size-field-mismatch" => WithSizeField(GuildWire.GuildRequest(), 6),
            "upgrade-short" => GuildWire.Frame(GuildWire.StrongholdUpgradeId, upgradePayload[..3]),
            "upgrade-extra-byte" => GuildWire.Frame(GuildWire.StrongholdUpgradeId, upgradePayload.Append((byte)0).ToArray()),
            "upgrade-size-field-mismatch" => WithSizeField(GuildWire.StrongholdUpgrade(guild.GuildRevision), 9),
            "enhance-short" => GuildWire.Frame(GuildWire.WeaponEnhanceId, enhancePayload[..3]),
            "enhance-extra-byte" => GuildWire.Frame(GuildWire.WeaponEnhanceId, enhancePayload.Append((byte)0).ToArray()),
            "enhance-size-field-mismatch" => WithSizeField(GuildWire.WeaponEnhance(inventory.Revision), 7),
            "client-sends-snapshot" => GuildWire.Frame(GuildWire.GuildSnapshotId, new byte[72]),
            "client-sends-action-result" => GuildWire.Frame(GuildWire.GuildActionResultId, new byte[22]),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
    }

    // A direct receive/handler caller can hand over a size field that disagrees with the buffer length.
    static byte[] WithSizeField(byte[] packet, ushort size)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), size);
        return packet;
    }
}
