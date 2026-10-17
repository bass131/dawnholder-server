using System.Collections;
using System.Reflection;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Cross-domain boundaries: pending persistence cannot join the guild, and admission seed
// publication is distinguished from transport delivery. All requests enter through real wire/ticks.
[Collection("GameWorldRegistryTests")]
public sealed class GuildAdmissionCommitBoundaryTests
{
    [Fact]
    public async Task LoadingGuildInputsAreDropped_StoredClass48PrecedesEntryAndFirstSeed()
    {
        using AdmissionTestWorld fixture = new();
        EntrySession watcher = await fixture.EnterAsync(OtherName, CharacterClass.Knight);
        (EntrySession loading, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        int mark = loading.Mark;

        loading.OnRecvPacket(GuildWire.GuildRequest());
        loading.OnRecvPacket(GuildWire.StrongholdUpgrade(0));
        loading.OnRecvPacket(GuildWire.WeaponEnhance(0));
        await fixture.TickAsync();
        GuildSnapshot waiting = await QueryAsync(fixture, watcher);

        Assert.Null(fixture.PlayerOf(loading));
        Assert.Equal("Loading", RuntimeEntryApi.AdmissionState(loading));
        Assert.Empty(loading.Sent(mark));
        Assert.Equal(5, waiting.Treasury);
        Assert.Equal(1, waiting.MemberCount);
        Assert.Equal(1, waiting.OnlineMemberCount);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);

        acquire.Complete(Acquired(acquire.Request, CharacterClass.Mage, MageMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(loading) is not null, "stored-class guild admission");
        SentPacket[] admitted = loading.Sent(mark);
        Assert.Equal(new ushort[] { 48, 3, 21, 9, 44 }, admitted.Take(5).Select(packet => packet.Id));
        Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 1 },
            Assert.Single(admitted, packet => packet.Id == 48).Bytes);
        GuildSnapshot joined = GuildWire.ReadSnapshot(Assert.Single(admitted, packet => packet.Id == 44).Bytes);
        Assert.Equal(CharacterClass.Mage, fixture.PlayerOf(loading)!.Stats.Class);
        Assert.Equal(10, joined.Treasury);
        Assert.Equal(2, joined.MemberCount);
        Assert.Equal(2, joined.OnlineMemberCount);
        Assert.Equal(1, joined.Sequence);
        Assert.Equal(ChangeKind.Membership, joined.ChangeKind);
        Assert.Equal(0u, joined.InventoryRevision);
        Assert.Equal(0, joined.WeaponLevel);
        Assert.All(admitted.Where(packet => packet.Id is 48 or 3 or 21 or 44),
            packet => Assert.True(packet.InsideTick));
    }

    [Fact]
    public async Task LateAcquiredOwnerAfterClose_ReleasesWithoutGuildMemberSeedOrInventory()
    {
        using AdmissionTestWorld fixture = new();
        EntrySession watcher = await fixture.EnterAsync(OtherName, CharacterClass.Knight);
        (EntrySession closed, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        int mark = closed.Mark;

        closed.Disconnect();
        await fixture.TickAsync();
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Mage, MageMaxHp));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        release.Complete(Released(release.Request));
        await fixture.TicksAsync(3);
        GuildSnapshot after = await QueryAsync(fixture, watcher);

        Assert.Empty(closed.Sent(mark));
        Assert.Null(fixture.PlayerOf(closed));
        Assert.Equal(-1, closed.EntityId);
        Assert.Equal("Closed", RuntimeEntryApi.AdmissionState(closed));
        Assert.Equal(5, after.Treasury);
        Assert.Equal(1, after.MemberCount);
        Assert.Equal(1, after.OnlineMemberCount);
        Assert.Equal(1, fixture.World.Accounts.OnlineCount);
        AssertNoInventoryOwner(fixture.World.Inventory, closed);
        Assert.Equal(PersistenceOperationKind.ReleaseRuntime, release.Request.Operation.Kind);
        Assert.Equal(HeldAuthority(PersistenceOwnerKind.Runtime,
            acquire.Request.Operation.OwnerId, AcquiredFence), release.Request.Authority);
        Assert.Equal(1, release.Request.Operation.Sequence);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Repository.Calls);
    }

    [Theory]
    [InlineData((ushort)48, false)]
    [InlineData((ushort)3, true)]
    [InlineData((ushort)21, true)]
    public void ClosingAtAdmissionSend_RetainsOnlyAnAlreadyCommittedSeed(ushort closeAt, bool committed)
    {
        using GuildTestWorld guild = new();
        GuildClient watcher = guild.JoinAndEnter(CharacterClass.Knight);
        GuildFaultClient closing = GuildBoundaryFixture.ConnectFaultClient(guild, guild.NewAccountName());
        closing.CloseOnSendOf(closeAt);
        closing.OnRecvPacket(GuildBoundaryFixture.SelectKnight());
        guild.Tick();
        guild.Settle();
        GuildSnapshot after = guild.Query(watcher);

        Assert.True(closing.ClosedBySend, "the requested send boundary must actually close the connection");
        Assert.True(closing.IsClosing);
        Assert.Equal(-1, closing.EntityId);
        Assert.Single(guild.World.AllLiveMaps.SelectMany(map => map.Players));
        Assert.Equal(1, guild.World.Accounts.OnlineCount);
        AssertNoInventoryOwner(guild.World.Inventory, closing);
        Assert.Equal(committed ? 10 : 5, after.Treasury);
        Assert.Equal(committed ? 2 : 1, after.MemberCount);
        Assert.Equal(1, after.OnlineMemberCount);

        GuildClient returned = guild.JoinAndEnter(CharacterClass.Knight, closing.AccountName);
        GuildSnapshot joined = GuildWire.ReadSnapshot(Assert.Single(returned.Since(0, 44)));
        InventorySnapshot fresh = guild.QueryInventory(returned);
        Assert.Equal(10, joined.Treasury);
        Assert.Equal(2, joined.MemberCount);
        Assert.Equal(2, joined.OnlineMemberCount);
        Assert.Equal(committed ? ChangeKind.Presence : ChangeKind.Membership, joined.ChangeKind);
        Assert.Equal(0u, fresh.Revision);
        Assert.Equal(0, fresh.Currency);
        Assert.Empty(fresh.Counts);
    }

    static async Task<GuildSnapshot> QueryAsync(AdmissionTestWorld fixture, EntrySession session)
    {
        int mark = session.Mark;
        session.OnRecvPacket(GuildWire.GuildRequest());
        await fixture.TickAsync();
        return GuildWire.ReadSnapshot(Assert.Single(session.Sent(mark), packet => packet.Id == 44).Bytes);
    }

    static void AssertNoInventoryOwner(InventoryRegistry inventory, GameSession session)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        IDictionary registered = (IDictionary)typeof(InventoryRegistry)
            .GetField("_registeredSessions", flags)!.GetValue(inventory)!;
        IDictionary states = (IDictionary)typeof(InventoryRegistry)
            .GetField("_states", flags)!.GetValue(inventory)!;
        Assert.DoesNotContain(registered.Values.Cast<object>(), value => ReferenceEquals(value, session));
        Assert.False(states.Contains(session));
    }
}
