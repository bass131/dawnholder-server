using System.Diagnostics;
using System.Net;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Guild.GuildDungeonClearTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Independent final review (goal 2026-10-14-guild-server, G2/G3/G4/G6, D3/D4/D5/D8/D10, I3/I4/I5) of the paths
// the earlier clear and purchase tests leave open:
//   - the beneficiary closing after every preparation of its clear settlement, right before the final owner
//     check (contract 「Content 클리어 성공과 공동 적립」 4), and a different member closing at that point (D8),
//   - a purchase or an enhance queued in the same World tick as an Inventory change: D3 drains Inventory first,
//     so the request is judged against the already changed revision and must refuse without spending,
//   - a credit reaching a member in the middle of a portal move (D4: Unavailable precedes Town).
// Entry, portals, the boss room's own death handler and all observations are the real product path through
// GuildTestWorld / GuildDungeonClearTestWorld. Expectations are the approved A literals (seed 5, clear credit
// 10, stronghold 30, enhance 20 gold + Material 1, +5), the kept Content clear reward and before/after values the
// same connection observed; no product rule, constant or packet builder computes an expected value.
[Collection("GameWorldRegistryTests")]
public sealed class GuildClearSettlementTests : IDisposable
{
    // Approved A (goal 「확정 기본값과 수명」) and the kept Content clear reward.
    const int Seed = 5;
    const int ClearCredit = 10;
    const int ClearCurrency = 50;
    const int StrongholdCost = 30;
    const int EnhanceCurrency = 20;
    const int EnhanceMaterial = 1;
    const int EnhanceAttackBonus = 5;

    const ushort ClearResultId = GuildTestWorld.DungeonClearResultId;

    readonly GuildDungeonClearTestWorld _clear = new();

    GuildTestWorld Guild => _clear.Guild;

    public void Dispose() => _clear.Dispose();

    // D10 / I3 / contract item 4: the solo beneficiary a is closed at the last fallible step of its own settlement:
    // the other member's location read inside a's prepared credit, after a's reward, a's credit 44 and the
    // communal next value already exist. Only the final owner check stands between that instant and the commit.
    // Neither side may commit: no 39/36/44 for a, the treasury and its revision stay, the watcher's prepared
    // sequence is not consumed, and a's account returning later gets no second seed and a new personal state.
    [Fact]
    public void ABeneficiaryClosedAfterEveryPreparation_RightBeforeTheFinalOwnerCheck_CommitsNeitherSide()
    {
        GuildClient a = Guild.JoinAndEnter(CharacterClass.Knight);
        LocationReadCloserClient watcher = LocationReadCloserClient.Enter(Guild);
        Guild.UnlockBossRoom(a);
        GameMap room = Guild.EnterBossRoom(a);
        GuildSnapshot before = QueryAs(watcher);
        int aMark = a.Mark;
        int watcherMark = watcher.Mark;

        watcher.CloseAtOwnNextGuildMapRead(a);
        Guild.KillBoss(room, a);
        Guild.Settle();
        List<GuildSnapshot> watcherPushes = SnapshotsOf(watcher.Since(watcherMark, GuildWire.GuildSnapshotId));
        GuildSnapshot after = QueryAs(watcher);
        GuildClient back = Guild.JoinAndEnter(CharacterClass.Knight, a.AccountName);
        GuildSnapshot returned = GuildTestWorld.ReadSnapshot(Assert.Single(back.Since(0, GuildWire.GuildSnapshotId)));
        InventorySnapshot fresh = Guild.QueryInventory(back);

        AssertFiredInsideTheClearCredit(watcher, "the watcher's location read must close a inside a's prepared credit");
        Assert.True(a.IsClosing, "fixture: the beneficiary must be closed");
        Assert.Empty(a.Since(aMark, ClearResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId));
        Assert.Equal(2 * Seed, before.Treasury);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.DoesNotContain(watcherPushes, snapshot => snapshot.ChangeKind == ChangeKind.TreasuryCredit);
        AssertContiguousSequences(SnapshotsOf(watcher.Since(0, GuildWire.GuildSnapshotId)));
        Assert.Equal(ChangeKind.Presence, returned.ChangeKind);
        Assert.Equal(2 * Seed, returned.Treasury);
        Assert.Equal(2, returned.MemberCount);
        Assert.Equal(0u, fresh.Revision);
        Assert.Equal(0, fresh.Currency);
    }

    // D8 / D5 / I3: the other member closes at its own location read inside a's prepared credit. Its 44 is
    // dropped, but a's settlement is not: a gets 39 → 36 → its TreasuryCredit 44 and the treasury goes 10 → 20.
    // The member's account returning sees the credited treasury and no second seed.
    [Fact]
    public void AnotherMemberClosingWhileTheCreditIsPrepared_DoesNotStopTheBeneficiarysSettlement()
    {
        GuildClient a = Guild.JoinAndEnter(CharacterClass.Knight);
        LocationReadCloserClient member = LocationReadCloserClient.Enter(Guild);
        Guild.UnlockBossRoom(a);
        GameMap room = Guild.EnterBossRoom(a);
        InventorySnapshot aBefore = Guild.QueryInventory(a);
        int aMark = a.Mark;
        int memberMark = member.Mark;

        member.CloseAtOwnNextGuildMapRead(member);
        Guild.KillBoss(room, a);
        // Read in the settling tick itself: the member's close cleanup later pushes a Presence 44 to a.
        List<byte[]> economy = EconomySince(a, aMark);
        Guild.Settle();
        GuildClient back = Guild.JoinAndEnter(CharacterClass.Knight, member.AccountName);
        GuildSnapshot returned = GuildTestWorld.ReadSnapshot(Assert.Single(back.Since(0, GuildWire.GuildSnapshotId)));

        AssertFiredInsideTheClearCredit(member, "the member must close inside a's prepared credit");
        Assert.True(member.IsClosing, "fixture: the other member must be closed");
        Assert.Equal(
            new[] { ClearResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId },
            GuildTestWorld.Ids(economy));
        Assert.True(
            GuildTestWorld.ReadClearResult(economy[0]).result == (byte)InventoryResult.Success,
            "a's 39 is Success");
        InventorySnapshot paid = GuildWire.ReadInventorySnapshot(economy[1]);
        Assert.Equal(aBefore.Revision + 1, paid.Revision);
        Assert.Equal(aBefore.Currency + ClearCurrency, paid.Currency);
        GuildSnapshot credit = GuildTestWorld.ReadSnapshot(economy[2]);
        Assert.Equal(ChangeKind.TreasuryCredit, credit.ChangeKind);
        Assert.Equal((2 * Seed) + ClearCredit, credit.Treasury);
        Assert.Equal(paid.Revision, credit.InventoryRevision);
        Assert.Empty(member.Since(memberMark, GuildWire.GuildSnapshotId));
        Assert.Equal(ChangeKind.Presence, returned.ChangeKind);
        Assert.Equal((2 * Seed) + ClearCredit, returned.Treasury);
        Assert.Equal(2, returned.MemberCount);
    }

    // D3 / D4 / G3 / I4: the buyer's 45 carries the revision it last saw and is queued before the tick in which a
    // party clear credits twice. The World drains Inventory before Guild, so the 45 meets the credited revision:
    // Stale with nothing spent, after both TreasuryCredit 44s; its 47 points at the following 44, which shows the
    // credited 3 × 5 + 2 × 10 = 35 as purchasable. The fresh revision then buys once: 35 − 30 = 5, level 2, and both
    // beneficiaries see the upgrade.
    [Fact]
    public void AnUpgradeQueuedInTheTickOfAClear_IsJudgedAfterBothCredits_AsStale_ThenTheFreshRevisionBuysOnce()
    {
        GuildClient buyer = Guild.JoinAndEnter(CharacterClass.Knight);
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        GuildSnapshot seen = Guild.Query(buyer);
        int mark = buyer.Mark;

        buyer.OnRecvPacket(GuildWire.StrongholdUpgrade(seen.GuildRevision));
        Guild.KillBoss(room, a);
        Guild.Settle();
        List<byte[]> tick = GuildReplies(buyer.Since, mark);
        int aMark = a.Mark;
        int bMark = b.Mark;

        Assert.Equal(3 * Seed, seen.Treasury);
        Assert.DoesNotContain(GuildWire.InventorySnapshotId, GuildTestWorld.Ids(tick));
        int resultAt = Assert.Single(
            Enumerable.Range(0, tick.Count),
            i => GuildWire.IdOf(tick[i]) == GuildWire.GuildActionResultId);
        List<GuildSnapshot> credits = SnapshotsOf(tick.Take(resultAt));
        Assert.Equal(new[] { ChangeKind.TreasuryCredit, ChangeKind.TreasuryCredit }, credits.Select(s => s.ChangeKind));
        Assert.Equal(
            new[] { (3 * Seed) + ClearCredit, (3 * Seed) + (2 * ClearCredit) },
            credits.Select(s => s.Treasury));
        GuildActionResult stale = GuildWire.ReadActionResult(tick[resultAt]);
        Assert.Equal(GuildAction.StrongholdUpgrade, stale.Action);
        Assert.Equal(GuildCode.Stale, stale.Result);
        Assert.Equal(seen.GuildRevision, stale.RequestedRevision);
        Assert.Equal(credits[^1].GuildRevision, stale.GuildRevision);
        Assert.Equal(seen.InventoryRevision, stale.InventoryRevision);
        GuildSnapshot resync = GuildTestWorld.ReadSnapshot(Assert.Single(tick.Skip(resultAt + 1)));
        Assert.Equal(ChangeKind.Query, resync.ChangeKind);
        Assert.Equal(stale.Sequence, resync.Sequence);
        Assert.Equal((3 * Seed) + (2 * ClearCredit), resync.Treasury);
        Assert.Equal(stale.GuildRevision, resync.GuildRevision);
        Assert.Equal(1, resync.StrongholdLevel);
        Assert.Equal(GuildCode.Success, resync.StrongholdAvailability);
        AssertContiguousSequences(SnapshotsOf(tick));

        List<byte[]> buy = Guild.Act(buyer, GuildWire.StrongholdUpgrade(resync.GuildRevision));
        Assert.Equal(new[] { GuildWire.GuildActionResultId, GuildWire.GuildSnapshotId }, GuildTestWorld.Ids(buy));
        Assert.Equal(GuildCode.Success, GuildWire.ReadActionResult(buy[0]).Result);
        GuildSnapshot bought = GuildTestWorld.ReadSnapshot(buy[1]);
        Assert.Equal(ChangeKind.StrongholdUpgrade, bought.ChangeKind);
        Assert.Equal((3 * Seed) + (2 * ClearCredit) - StrongholdCost, bought.Treasury);
        Assert.Equal(2, bought.StrongholdLevel);
        foreach ((GuildClient member, int memberMark) in new[] { (a, aMark), (b, bMark) })
        {
            GuildSnapshot pushed = Assert.Single(SnapshotsOf(member.Since(memberMark, GuildWire.GuildSnapshotId)));
            Assert.Equal(ChangeKind.StrongholdUpgrade, pushed.ChangeKind);
            Assert.Equal(bought.Treasury, pushed.Treasury);
            Assert.Equal(bought.GuildRevision, pushed.GuildRevision);
        }
    }

    // D3 / D8 / G4 / I5: a's 46 carries the personal revision of its latest 36 and is queued before the tick in
    // which a real normal kill pays a. Inventory runs first, so the 46 is Stale: 47 → the current 36 (the kill
    // already in it) → 44, nothing consumed. The fresh revision then enhances once: exactly 20 gold and Material 1
    // leave, weapon 0 → 1 with +5, answered 47 → 36 → 44. The partner is not enhanced.
    [Fact]
    public void AnEnhanceQueuedInTheTickOfAKillReward_IsStaleAndConsumesNothing_ThenTheFreshRevisionEnhancesOnce()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        Guild.KillBoss(room, a);
        Guild.Settle();
        Guild.ConfirmToTown(a);
        Guild.UpgradeStronghold(a);
        InventorySnapshot funded = Guild.QueryInventory(a);
        InventorySnapshot partner = _clear.QueryInventory(b);
        GuildSnapshot communal = Guild.Query(a);
        GameMap town = Guild.MapOf(a);
        PlayerEntity player = Guild.PlayerOf(a);
        EnemyEntity enemy = town.SpawnEnemy(EnemyKind.Normal, player.Position.X + 1f, player.Position.Y, maxHp: 1);
        int mark = a.Mark;

        // As GuildTestWorld.KillBoss: the death handler runs in the map's own job, so the kill callback, the
        // Inventory reward and the queued guild job follow the product order inside one World tick.
        town.EnqueueJob(() =>
        {
            enemy.Hp = 0;
            town.HandleEnemyDeath(enemy, a.EntityId);
        });
        a.OnRecvPacket(GuildWire.WeaponEnhance(funded.Revision));
        Guild.Tick();
        Guild.Settle();
        List<byte[]> replies = GuildReplies(a.Since, mark);

        Assert.Equal(ClearCurrency, funded.Currency);
        Assert.Equal(1, funded.CountOf(GuildWire.MaterialItemId));
        Assert.Equal(1, communal.BlacksmithUnlocked);
        int resultAt = Assert.Single(
            Enumerable.Range(0, replies.Count),
            i => GuildWire.IdOf(replies[i]) == GuildWire.GuildActionResultId);
        int snapshotAt = Assert.Single(
            Enumerable.Range(0, replies.Count),
            i => GuildWire.IdOf(replies[i]) == GuildWire.GuildSnapshotId);
        Assert.True(resultAt < snapshotAt, "D8: the action result precedes its guild snapshot");
        byte[] currentInventory = Assert.Single(replies.Skip(resultAt + 1).Take(snapshotAt - resultAt - 1));
        Assert.Equal(GuildWire.InventorySnapshotId, GuildWire.IdOf(currentInventory));

        InventorySnapshot killed = GuildWire.ReadInventorySnapshot(currentInventory);
        // The kept normal-kill contract pays 10 gold and Material 1 once. A stale enhance spends neither.
        Assert.Equal(funded.Revision + 1, killed.Revision);
        Assert.Equal(funded.Currency + 10, killed.Currency);
        Assert.Equal(funded.CountOf(GuildWire.MaterialItemId) + 1, killed.CountOf(GuildWire.MaterialItemId));
        Assert.Equal(funded.CountOf(GuildWire.CoinPouchItemId), killed.CountOf(GuildWire.CoinPouchItemId));
        GuildActionResult stale = GuildWire.ReadActionResult(replies[resultAt]);
        Assert.Equal(GuildAction.WeaponEnhance, stale.Action);
        Assert.Equal(GuildCode.Stale, stale.Result);
        Assert.Equal(funded.Revision, stale.RequestedRevision);
        Assert.Equal(killed.Revision, stale.InventoryRevision);
        Assert.Equal(communal.GuildRevision, stale.GuildRevision);
        // The kill push uses a later map job; it is not ordered before the direct 47→36→44 response.
        // Both 36s must describe the same committed reward, not a second reward or a hidden spend.
        List<byte[]> inventoryReplies = replies
            .Where(packet => GuildWire.IdOf(packet) == GuildWire.InventorySnapshotId)
            .ToList();
        Assert.Equal(2, inventoryReplies.Count);
        Assert.All(inventoryReplies, packet => AssertSameInventory(killed, GuildWire.ReadInventorySnapshot(packet)));
        GuildSnapshot resync = GuildTestWorld.ReadSnapshot(replies[snapshotAt]);
        Assert.Equal(ChangeKind.Query, resync.ChangeKind);
        Assert.Equal(stale.Sequence, resync.Sequence);
        Assert.Equal(killed.Revision, resync.InventoryRevision);
        Assert.Equal(communal.GuildRevision, resync.GuildRevision);
        Assert.Equal(communal.Treasury, resync.Treasury);
        Assert.Equal(0, resync.WeaponLevel);
        Assert.Equal(0, resync.AttackBonus);
        AssertSameInventory(killed, Guild.QueryInventory(a));

        List<byte[]> enhance = Guild.Act(a, GuildWire.WeaponEnhance(killed.Revision));
        Assert.Equal(
            new[] { GuildWire.GuildActionResultId, GuildWire.InventorySnapshotId, GuildWire.GuildSnapshotId },
            GuildTestWorld.Ids(enhance));
        GuildActionResult success = GuildWire.ReadActionResult(enhance[0]);
        Assert.Equal(GuildCode.Success, success.Result);
        Assert.Equal(killed.Revision, success.RequestedRevision);
        InventorySnapshot paid = GuildWire.ReadInventorySnapshot(enhance[1]);
        Assert.Equal(killed.Revision + 1, paid.Revision);
        Assert.Equal(paid.Revision, success.InventoryRevision);
        Assert.Equal(killed.Currency - EnhanceCurrency, paid.Currency);
        Assert.Equal(
            killed.CountOf(GuildWire.MaterialItemId) - EnhanceMaterial,
            paid.CountOf(GuildWire.MaterialItemId));
        Assert.Equal(killed.CountOf(GuildWire.CoinPouchItemId), paid.CountOf(GuildWire.CoinPouchItemId));
        GuildSnapshot enhanced = GuildTestWorld.ReadSnapshot(enhance[2]);
        Assert.Equal(ChangeKind.WeaponEnhance, enhanced.ChangeKind);
        Assert.Equal(1, enhanced.WeaponLevel);
        Assert.Equal(EnhanceAttackBonus, enhanced.AttackBonus);
        Assert.Equal(paid.Revision, enhanced.InventoryRevision);
        AssertSameInventory(paid, Guild.QueryInventory(a));
        AssertSameInventory(partner, _clear.QueryInventory(b));
        GuildSnapshot partnerView = _clear.QueryGuild(b);
        Assert.Equal(0, partnerView.WeaponLevel);
        Assert.Equal(0, partnerView.AttackBonus);
    }

    // D4 / D8: the walker's portal move starts in Town's job of the very tick in which a's clear is credited. The
    // TreasuryCredit 44 it receives during the move shows both purchases as Unavailable (the move is checked
    // before Town). After the arrival a query shows NotInTown for both, and the credited treasury 10 + 10 = 20.
    [Fact]
    public void ACreditReachingAMemberInAPortalMove_ShowsUnavailable_AndTheArrivalShowsNotInTown()
    {
        GuildClient walker = Guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient a = Guild.JoinAndEnter(CharacterClass.Knight);
        Guild.UnlockBossRoom(a);
        GameMap room = Guild.EnterBossRoom(a);
        Guild.PlaceAtTownPortal(walker);
        int mark = walker.Mark;

        walker.OnRecvPacket(GuildTestWorld.EnterTownPortalToHuntingGround());
        Guild.KillBoss(room, a);
        GuildSnapshot duringMove = Assert.Single(SnapshotsOf(walker.Since(mark, GuildWire.GuildSnapshotId)));
        Guild.Settle();
        GuildSnapshot arrived = Guild.Query(walker);

        Assert.Equal(ChangeKind.TreasuryCredit, duringMove.ChangeKind);
        Assert.Equal((2 * Seed) + ClearCredit, duringMove.Treasury);
        Assert.Equal(GuildCode.Unavailable, duringMove.StrongholdAvailability);
        Assert.Equal(GuildCode.Unavailable, duringMove.WeaponAvailability);
        Assert.True(
            Guild.MapOf(walker).MapId == MapId.HuntingGround,
            "fixture: the real portal move must arrive in the hunting ground");
        Assert.Equal(GuildCode.NotInTown, arrived.StrongholdAvailability);
        Assert.Equal(GuildCode.NotInTown, arrived.WeaponAvailability);
        Assert.Equal((2 * Seed) + ClearCredit, arrived.Treasury);
    }

    // GuildTestWorld.Query takes the sealed GuildClient; the same real 43 → Query 44 for the closer connection.
    GuildSnapshot QueryAs(LocationReadCloserClient client)
    {
        Guild.Settle();
        int mark = client.Mark;
        client.OnRecvPacket(GuildWire.GuildRequest());
        Guild.Settle();
        GuildSnapshot snapshot = GuildTestWorld.ReadSnapshot(
            Assert.Single(client.Since(mark, GuildWire.GuildSnapshotId, GuildWire.GuildActionResultId)));
        Assert.Equal(ChangeKind.Query, snapshot.ChangeKind);
        return snapshot;
    }

    // The fault must have fired once, inside the beneficiary's prepared clear credit that Inventory's clear
    // settlement builds; otherwise the test did not reach the instant it is about.
    static void AssertFiredInsideTheClearCredit(LocationReadCloserClient client, string because)
    {
        Assert.True(client.Fired == 1, $"fixture: {because} (fired {client.Fired} times)");
        int credit = client.FiredFrames.ToList().IndexOf("PreparedDungeonClearCredit..ctor");
        int settlement = client.FiredFrames.ToList().IndexOf("InventoryRegistry.ApplyDungeonClear");
        Assert.True(
            credit >= 0 && settlement > credit,
            $"fixture: {because}; frames: {string.Join(" < ", client.FiredFrames)}");
    }

    // A skipped number would be a sequence consumed without its 44 (I9: per connection, from 1, only growing).
    static void AssertContiguousSequences(List<GuildSnapshot> snapshots)
    {
        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.Equal(snapshots[i - 1].Sequence + 1, snapshots[i].Sequence);
        }
    }

    static List<GuildSnapshot> SnapshotsOf(IEnumerable<byte[]> packets)
        => packets
            .Where(packet => GuildWire.IdOf(packet) == GuildWire.GuildSnapshotId)
            .Select(GuildTestWorld.ReadSnapshot)
            .ToList();

    // The 36/44/47 replies of one connection since a mark, in arrival order.
    static List<byte[]> GuildReplies(Func<int, ushort[], List<byte[]>> since, int mark)
        => since(mark, new[]
        {
            GuildWire.InventorySnapshotId,
            GuildWire.GuildSnapshotId,
            GuildWire.GuildActionResultId,
        });

    static void AssertSameInventory(InventorySnapshot expected, InventorySnapshot actual)
    {
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.Currency, actual.Currency);
        Assert.Equal(expected.Counts.OrderBy(pair => pair.Key), actual.Counts.OrderBy(pair => pair.Key));
    }
}

// Test-only connection for the instant GuildFaultClient cannot reach. At its own next guild location read (during a
// World tick the only map read of a connection, GameSession.ReadGuildMap → GetMap) it closes a chosen connection:
// another member, or itself. It records the frames of that instant so a test can show where the close landed.
// GuildFaultClient closes only itself and keeps no location, and GuildClient promises that nothing fails.
internal sealed class LocationReadCloserClient : GameSession
{
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal readonly List<byte[]> Packets = new();

    GameSession? _closeTarget;

    LocationReadCloserClient(GameWorld world, string accountName) : base(world) => AccountName = accountName;

    internal string AccountName { get; }

    internal int Mark => Packets.Count;

    internal int Fired { get; private set; }

    // "DeclaringType.Method" from the innermost frame outwards, captured when the close fired.
    internal IReadOnlyList<string> FiredFrames { get; private set; } = Array.Empty<string>();

    // Same real handshake, C_Login and class selection as GuildTestWorld.JoinAndEnter; the admission 44 proves
    // the guild membership.
    internal static LocationReadCloserClient Enter(GuildTestWorld guild)
    {
        LocationReadCloserClient client = new(guild.World, guild.NewAccountName());
        client.OnConnected(Endpoint);
        client.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        client.OnRecvPacket(new C_Login { accountName = client.AccountName }.Write());
        guild.World.Accounts.Tick();
        Assert.True(client.IsLoggedIn, "fixture: the real account login must be accepted");
        client.OnRecvPacket(GuildBoundaryFixture.SelectKnight());
        guild.Tick();
        Assert.True(client.EntityId >= 0, "fixture: the real class selection must enter Town");
        Assert.NotEmpty(client.Since(0, GuildWire.GuildSnapshotId));
        guild.Settle();
        return client;
    }

    public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

    public override void Disconnect() => OnDisconnected(Endpoint);

    // Arm after the receive call that precedes the tick: receive-time handlers also read the map.
    internal void CloseAtOwnNextGuildMapRead(GameSession target) => _closeTarget = target;

    internal List<byte[]> Since(int mark, params ushort[] ids)
        => Packets.Skip(mark).Where(packet => ids.Contains(GuildWire.IdOf(packet))).ToList();

    protected override GameMap? GetMap()
    {
        GameMap? map = base.GetMap();
        GameSession? target = _closeTarget;
        if (target != null)
        {
            _closeTarget = null;
            Fired++;
            FiredFrames = new StackTrace(false).GetFrames()
                .Select(frame => frame.GetMethod())
                .Where(method => method?.DeclaringType != null)
                .Select(method => $"{method!.DeclaringType!.Name}.{method.Name}")
                .ToArray();
            target.Disconnect();
        }
        return map;
    }
}
