using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// I1 / G1 (technical contract v1): every account that really enters the world joins the one default
// guild once per server run, seeding the treasury with 5 (approved A). Reconnecting, a duplicate login
// or a repeated class selection adds no member and no seed; closing lowers only the online count.
// Expected numbers are approved literals (seed 5, guild ID 1, name 「새벽 길드」) and counts of the
// accounts each test enters. Observation is the server's 44 to each connection (GuildWire).
[Collection("GameWorldRegistryTests")]
public sealed class GuildMembershipTests : IDisposable
{
    const int Seed = 5;

    readonly GuildTestWorld _guild = new();

    public void Dispose() => _guild.Dispose();

    [Fact]
    public void TwoAccounts_ShareTheDefaultGuild_WithOneSeedEach()
    {
        GuildClient first = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient second = _guild.JoinAndEnter(CharacterClass.Mage);

        GuildSnapshot seenByFirst = _guild.Query(first);
        GuildSnapshot seenBySecond = _guild.Query(second);

        foreach (GuildSnapshot snapshot in new[] { seenByFirst, seenBySecond })
        {
            Assert.Equal(GuildWire.DefaultGuildId, snapshot.GuildId);
            Assert.Equal(GuildWire.DefaultGuildName, snapshot.GuildName);
            Assert.Equal(2 * Seed, snapshot.Treasury);
            Assert.Equal(2, snapshot.MemberCount);
            Assert.Equal(2, snapshot.OnlineMemberCount);
            Assert.Equal(1, snapshot.StrongholdLevel);
            Assert.Equal(0, snapshot.BlacksmithUnlocked);
            Assert.Equal(0, snapshot.WeaponLevel);
            Assert.Equal(0, snapshot.AttackBonus);
        }
        Assert.Equal(seenByFirst.GuildRevision, seenBySecond.GuildRevision);
    }

    // A query reads state only: the economic revision and treasury stay, the per-connection sequence grows.
    [Fact]
    public void RepeatedQuery_ChangesNoGuildState_AndOnlyAdvancesTheSequence()
    {
        GuildClient member = _guild.JoinAndEnter(CharacterClass.Knight);

        GuildSnapshot first = _guild.Query(member);
        GuildSnapshot second = _guild.Query(member);

        Assert.Equal(Seed, first.Treasury);
        Assert.Equal(1, first.MemberCount);
        Assert.Equal(first.GuildRevision, second.GuildRevision);
        Assert.Equal(first.Treasury, second.Treasury);
        Assert.True(second.Sequence > first.Sequence, $"sequence must grow: {first.Sequence} → {second.Sequence}");
    }

    [Fact]
    public void SameAccountReconnect_KeepsOneMember_AndAddsNoSecondSeed()
    {
        GuildClient stays = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient leaves = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildSnapshot beforeClose = _guild.Query(stays);
        Assert.Equal(2 * Seed, beforeClose.Treasury);

        leaves.Close();
        _guild.Settle();
        GuildSnapshot whileAway = _guild.Query(stays);

        // G1: closing lowers the online count only; members and the shared treasury remain.
        Assert.Equal(2, whileAway.MemberCount);
        Assert.Equal(1, whileAway.OnlineMemberCount);
        Assert.Equal(2 * Seed, whileAway.Treasury);
        Assert.Equal(beforeClose.GuildRevision, whileAway.GuildRevision);

        GuildClient back = _guild.JoinAndEnter(CharacterClass.Knight, leaves.AccountName);
        GuildSnapshot afterReturn = _guild.Query(back);

        Assert.Equal(2, afterReturn.MemberCount);
        Assert.Equal(2, afterReturn.OnlineMemberCount);
        Assert.Equal(2 * Seed, afterReturn.Treasury);
        // Presence changes are ordered by the 44 sequence, not by the economic revision.
        Assert.Equal(beforeClose.GuildRevision, afterReturn.GuildRevision);
    }

    // The existing account registry rejects a second login of an online account; the rejected
    // connection must not become a member or seed, even if it still sends a class selection.
    [Fact]
    public void DuplicateLoginWhileOnline_AddsNoMemberOrSeed()
    {
        GuildClient online = _guild.JoinAndEnter(CharacterClass.Knight);

        GuildClient duplicate = _guild.Connect(online.AccountName);
        duplicate.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());
        _guild.Settle();

        Assert.False(duplicate.IsLoggedIn, "fixture: the existing registry must reject the duplicate login");
        GuildSnapshot snapshot = _guild.Query(online);
        Assert.Equal(1, snapshot.MemberCount);
        Assert.Equal(1, snapshot.OnlineMemberCount);
        Assert.Equal(Seed, snapshot.Treasury);
    }

    [Fact]
    public void RepeatedClassSelectionAfterEntry_AddsNoSecondSeed()
    {
        GuildClient member = _guild.JoinAndEnter(CharacterClass.Knight);
        GuildSnapshot before = _guild.Query(member);

        member.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)CharacterClass.Mage }.Write());
        _guild.Settle();
        GuildSnapshot after = _guild.Query(member);

        Assert.Equal(1, after.MemberCount);
        Assert.Equal(Seed, after.Treasury);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
    }

    // Contract 「snapshotSequence는 접속별1부터 증가」: every 44 to a connection carries a larger
    // sequence than the previous one, starting from 1, and a new connection starts again from 1.
    [Fact]
    public void SnapshotSequence_StartsAtOnePerConnection_AndOnlyGrows()
    {
        GuildClient first = _guild.JoinAndEnter(CharacterClass.Knight);
        _guild.Query(first);
        _guild.Query(first);
        long[] firstSequences = SequencesOf(first);

        first.Close();
        _guild.Settle();
        GuildClient reconnected = _guild.JoinAndEnter(CharacterClass.Knight, first.AccountName);
        _guild.Query(reconnected);
        long[] reconnectedSequences = SequencesOf(reconnected);

        Assert.True(firstSequences.Length >= 2, "two queries must produce at least two 44s");
        Assert.Equal(1, firstSequences[0]);
        Assert.True(firstSequences.Zip(firstSequences.Skip(1)).All(pair => pair.Second > pair.First),
            $"sequence must grow: {string.Join(",", firstSequences)}");
        Assert.Equal(1, reconnectedSequences[0]);
    }

    static long[] SequencesOf(GuildClient client)
        => client.Since(0, GuildWire.GuildSnapshotId).Select(packet => GuildTestWorld.ReadSnapshot(packet).Sequence).ToArray();
}
