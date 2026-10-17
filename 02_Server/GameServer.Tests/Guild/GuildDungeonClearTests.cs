using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Guild.GuildDungeonClearTestWorld;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// G2 and technical contract v1 「Content 클리어 성공과 공동 적립」 1~5 with I2/I3/I8/I9 (goal
// 2026-10-14-guild-server): each beneficiary whose personal clear reward really succeeds adds the approved +10
// to the treasury once, prepared and published together with that reward. Written before the credit exists
// (contract clear-tdd-01): every test drives the current product entry (real handshake/login/class selection,
// real portals and 40, the boss room's own death handler, the World tick) and reads only the 39/36/44 bytes each
// connection receives and the before/after queries. Expectations are approved literals (seed 5, credit 10, the
// kept Content reward 50 + Material 1 + CoinPouch 1, cap 1,000,000,000) and test inputs; no product rule or
// packet builder computes one. A case that also holds without any credit is named a protection case, and a case
// that a server never crediting could pass is paired with a beneficiary who must be credited.
// Fixture inputs, named at each use: the boss door through the quest credit entry, portal placement, the death
// handler run in the room job, limit states (GuildBoundaryFixture, ReplaceInventoryState), GuildFaultClient faults.
// Not covered: a real socket, the Unity reward window, bots, DB, and the instant after every preparation and
// right before the commit, which no existing boundary reaches without a product hook (contract v1.1).
[Collection("GameWorldRegistryTests")]
public sealed class GuildDungeonClearTests : IDisposable
{
    // Approved A (goal 「확정 기본값과 수명」) and the kept Content clear reward.
    const int Seed = 5;
    const int ClearCredit = 10;
    const int ClearCurrency = 50;

    // PortalTable inputs: where the hunting ground and the boss room are entered.
    const int TownToHuntingGroundPortalId = 1;
    const int HuntingGroundToBossRoomPortalId = 1;

    const ushort ClearResultId = GuildTestWorld.DungeonClearResultId;

    readonly GuildDungeonClearTestWorld _clear = new();

    GuildTestWorld Guild => _clear.Guild;

    public void Dispose() => _clear.Dispose();

    // G2 / I2 / I9 / contract items 1, 3 and 5: two accounts (seeds 5 + 5 = 10) clear one BossRoom copy once.
    // Each beneficiary keeps its own 39 → 36 with 50 + Material 1 + CoinPouch 1 and is then credited, so the
    // treasury goes 10 → 10 + 10 + 10 = 30 and both members read the same communal state.
    [Fact]
    public void OneClear_ByTwoBeneficiaries_PaysEachOnce_AndCreditsTenEach_FromTenToThirty()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        GuildSnapshot before = _clear.QueryGuild(a);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        Guild.KillBoss(room, a);
        Guild.Settle();

        Assert.Equal(2 * Seed, before.Treasury);
        AssertPaidAndCredited(a, aMark, aBefore);
        AssertPaidAndCredited(b, bMark, bBefore);
        GuildSnapshot aAfter = _clear.QueryGuild(a);
        GuildSnapshot bAfter = _clear.QueryGuild(b);
        Assert.Equal((2 * Seed) + (2 * ClearCredit), aAfter.Treasury);
        Assert.Equal(aAfter.Treasury, bAfter.Treasury);
        Assert.Equal(aAfter.GuildRevision, bAfter.GuildRevision);
        Assert.True(aAfter.GuildRevision > before.GuildRevision, "an actual credit advances the communal revision");
    }

    // D8 / contract item 5: an online member outside the room gets the credit as TreasuryCredit 44s carrying its
    // own personal state (two real Town kills: revision 2, weapon 0), no 39 or 36, and the treasury
    // 3 seeds + 2 credits = 35.
    [Fact]
    public void TheCredit_ReachesAnOnlineMemberOutsideTheRoom_WithThatMembersOwnPersonalState()
    {
        GuildClient observer = Guild.JoinAndEnter(CharacterClass.Knight);
        Guild.KillOneHit(observer, EnemyKind.Normal);
        Guild.KillOneHit(observer, EnemyKind.Normal);
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        InventorySnapshot own = _clear.QueryInventory(observer);
        int mark = MarkOf(observer);

        Guild.KillBoss(room, a);
        Guild.Settle();
        List<GuildSnapshot> pushed = SnapshotsSince(observer, mark);

        Assert.True(own.Revision == 2, "fixture: two real kills must have changed the observer's state twice");
        Assert.Empty(Since(observer, mark, ClearResultId, GuildWire.InventorySnapshotId));
        Assert.NotEmpty(pushed);
        Assert.All(pushed, snapshot =>
        {
            Assert.Equal(ChangeKind.TreasuryCredit, snapshot.ChangeKind);
            Assert.Equal(own.Revision, snapshot.InventoryRevision);
            Assert.Equal(0, snapshot.WeaponLevel);
        });
        AssertSequenceGrows(pushed);
        Assert.Equal((3 * Seed) + (2 * ClearCredit), pushed[^1].Treasury);
        AssertSameInventory(own, _clear.QueryInventory(observer));
        Assert.Equal((3 * Seed) + (2 * ClearCredit), _clear.QueryGuild(observer).Treasury);
    }

    // G2 「늦은 입장」 / I2 / Content C1: b waits in the party's hunting ground copy at the death, then walks into the
    // cleared room. Neither makes b a beneficiary: no 39 or 36, b unchanged, no credit for b. Only a is credited:
    // 10 + 10 = 20.
    [Fact]
    public void APartyMemberInTheHuntingGround_ThenLateIntoTheClearedRoom_AddsNoCredit()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        Guild.MoveThroughPortal(b, TownToHuntingGroundPortalId);
        GameMap room = Guild.EnterBossRoom(a);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        Guild.KillBoss(room, a);
        Guild.Settle();
        GameMap entered = Guild.EnterBossRoom(b);
        Guild.Settle(StayTicks);

        Assert.Same(room, entered);
        Assert.Empty(Since(b, bMark, ClearResultId, GuildWire.InventorySnapshotId));
        Assert.All(SnapshotsSince(b, bMark), snapshot => Assert.Equal(bBefore.Revision, snapshot.InventoryRevision));
        AssertSameInventory(bBefore, _clear.QueryInventory(b));
        AssertPaidAndCredited(a, aMark, aBefore);
        Assert.Equal((2 * Seed) + ClearCredit, _clear.QueryGuild(b).Treasury);
    }

    // G2 「다른 복사본」 / I2 / Content C1: two parties in two BossRoom copies (seeds 4 × 5 = 20). Each death pays
    // and credits only its own copy's two beneficiaries: 20 → 40 → 60, each member paid exactly once.
    [Fact]
    public void EachBossRoomCopy_CreditsOnlyItsOwnBeneficiaries()
    {
        (GuildClient a1, GuildClient a2) = _clear.JoinParty();
        (GuildClient b1, GuildClient b2) = _clear.JoinParty();
        GameMap roomA = _clear.EnterSameBossRoom(a1, a2);
        GameMap roomB = _clear.EnterSameBossRoom(b1, b2);
        GuildClient[] everyone = { a1, a2, b1, b2 };
        int[] marks = everyone.Select(MarkOf).ToArray();

        Guild.KillBoss(roomA, a1);
        Guild.Settle();
        int[] paidAfterA = ClearResultCounts(everyone, marks);
        int treasuryAfterA = _clear.QueryGuild(b1).Treasury;
        Guild.KillBoss(roomB, b1);
        Guild.Settle();
        int[] paidAfterB = ClearResultCounts(everyone, marks);
        int treasuryAfterB = _clear.QueryGuild(a1).Treasury;

        Assert.NotSame(roomA, roomB);
        Assert.Equal(new[] { 1, 1, 0, 0 }, paidAfterA);
        Assert.Equal(new[] { 1, 1, 1, 1 }, paidAfterB);
        Assert.Equal((4 * Seed) + (2 * ClearCredit), treasuryAfterA);
        Assert.Equal((4 * Seed) + (4 * ClearCredit), treasuryAfterB);
    }

    // G2 「종료된 이전 세션」 / D10 / I2 / I3 / contract item 2. Owner end point: b's connection closes inside the room
    // job right after the death, after the capture and before the World phases that settle it. The closed
    // beneficiary is neither paid nor credited; a is: 10 + 10 = 20. The same account reconnecting gets neither a
    // second seed nor a late credit, and starts with a new personal state.
    [Fact]
    public void ABeneficiaryClosedBeforeItsSettlement_IsNeitherPaidNorCredited_AndAReconnectAddsNothing()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        _clear.KillBossThen(room, a, _ => b.Close());
        Guild.Settle();
        InventorySnapshot aPaid = AssertPaidAndCredited(a, aMark, aBefore);
        int treasury = _clear.QueryGuild(a).Treasury;
        GuildClient back = Guild.JoinAndEnter(CharacterClass.Knight, b.AccountName);
        GuildSnapshot afterReconnect = _clear.QueryGuild(back);

        Assert.True(b.IsClosing, "fixture: the beneficiary's connection must be closed");
        Assert.Empty(Since(b, bMark, ClearResultId, GuildWire.InventorySnapshotId));
        Assert.True(aPaid.Revision > aBefore.Revision, "the other beneficiary is still paid");
        Assert.Equal((2 * Seed) + ClearCredit, treasury);
        Assert.Equal(treasury, afterReconnect.Treasury);
        Assert.Equal(0u, afterReconnect.InventoryRevision);
    }

    // D10 / I3 / contract items 3 and 4 「최종 동일 접속 검사」. Owner end point: f's socket closes inside the guild
    // work of this clear. During a World tick the only map read of a connection is the guild's location read
    // (GuildFaultClient), which f's credit 44 needs before any commit. So f is closed before its final owner
    // check: no Success 39, no 36, no credit for f. g, entering second, is the positive control: 10 + 10 = 20.
    [Fact]
    public void ABeneficiaryClosingWhileItsCreditIsPrepared_CommitsNeitherSide()
    {
        GuildFaultClient f = GuildBoundaryFixture.EnterFaultClient(Guild);
        GuildClient g = Guild.JoinAndEnter(CharacterClass.Knight);
        _clear.FormParty(f, g);
        Guild.UnlockBossRoom(f);
        GameMap room = _clear.EnterSameBossRoom(f, g);
        InventorySnapshot gBefore = _clear.QueryInventory(g);
        int fMark = MarkOf(f);
        int gMark = MarkOf(g);

        f.CloseAtNextGuildMapRead();
        Guild.KillBoss(room, g);
        Guild.Settle();

        Assert.DoesNotContain(Since(f, fMark, ClearResultId), IsSuccessResult);
        Assert.Empty(Since(f, fMark, GuildWire.InventorySnapshotId));
        AssertPaidAndCredited(g, gMark, gBefore);
        Assert.Equal((2 * Seed) + ClearCredit, _clear.QueryGuild(g).Treasury);
        Assert.True(f.ClosedByGuildMapRead, "fixture: f must close inside the guild work of this clear");
    }

    // G2 「가방/개인 재화 상한 거부」 / I2 / contract item 2: f's own reward does not apply. InventoryFull (Material at
    // MaxStack from one real Dash) and CurrencyCap answer only the existing refusal 39; a personal revision at
    // uint.MaxValue keeps its existing contract of no 39 at all. f's personal state is unchanged and adds no credit.
    // g is the positive control: 10 + 10 = 20. The last two rows place a named limit state (ReplaceInventoryState).
    [Theory]
    [InlineData("inventory-full")]
    [InlineData("currency-cap")]
    [InlineData("revision-exhausted")]
    public void ABeneficiaryWhoseRewardDoesNotApply_KeepsItsStateAndAddsNoCredit_WhileTheOtherIsCredited(string limit)
    {
        GuildClient f = Guild.JoinAndEnter(CharacterClass.Knight);
        if (limit == "inventory-full") _clear.FillMaterialToMaxStack(f);
        GuildClient g = Guild.JoinAndEnter(CharacterClass.Knight);
        _clear.FormParty(f, g);
        Guild.UnlockBossRoom(f);
        GameMap room = _clear.EnterSameBossRoom(f, g);
        _clear.QueryInventory(f);
        if (limit == "currency-cap")
        {
            _clear.ReplaceInventoryState(f, new InventoryState(1, InventoryLimits.MaxCurrency, Array.Empty<InventorySlot>()));
        }
        if (limit == "revision-exhausted")
        {
            _clear.ReplaceInventoryState(f, new InventoryState(uint.MaxValue, 0, Array.Empty<InventorySlot>()));
        }
        InventorySnapshot fBefore = _clear.QueryInventory(f);
        InventorySnapshot gBefore = _clear.QueryInventory(g);
        int fMark = MarkOf(f);
        int gMark = MarkOf(g);

        Guild.KillBoss(room, g);
        Guild.Settle();

        if (limit == "inventory-full")
        {
            Assert.True(fBefore.CountOf(GuildWire.MaterialItemId) == InventoryLimits.MaxStack,
                "fixture: Material must be at MaxStack");
        }
        List<byte[]> fResults = Since(f, fMark, ClearResultId);
        if (limit == "revision-exhausted")
        {
            Assert.Empty(fResults);
        }
        else
        {
            AssertRefusedResult(Assert.Single(fResults), RefusalOf(limit));
        }
        Assert.Empty(Since(f, fMark, GuildWire.InventorySnapshotId));
        Assert.All(SnapshotsSince(f, fMark), snapshot => Assert.Equal(fBefore.Revision, snapshot.InventoryRevision));
        AssertSameInventory(fBefore, _clear.QueryInventory(f));
        AssertPaidAndCredited(g, gMark, gBefore);
        Assert.Equal((2 * Seed) + ClearCredit, _clear.QueryGuild(g).Treasury);
    }

    // I8 / G2 / approved A 「공동 상한」: the credit is min(10, space left) and only an actual increase advances the
    // communal revision; both beneficiaries are paid normally either way. Space 5: the first credit adds 5, the
    // second 0, so the treasury reaches the cap with one revision step. A full treasury at revision uint.MaxValue
    // adds 0 and keeps the revision, since saturation is not a failure (protection row: it also holds without
    // any credit). Named limit state: GuildBoundaryFixture.ReplaceCommunalState.
    [Theory]
    [InlineData(5, 7u, 8u)]
    [InlineData(0, uint.MaxValue, uint.MaxValue)]
    public void TreasuryNearTheCap_PaysBothNormally_AndCreditsOnlyTheSpaceLeft(int spaceLeft, uint revision, uint expectedRevision)
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        GuildBoundaryFixture.ReplaceCommunalState(Guild, revision, GuildWire.MaxTreasury - spaceLeft, strongholdLevel: 1);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        Guild.KillBoss(room, a);
        Guild.Settle();

        AssertPaid(a, aMark, aBefore);
        AssertPaid(b, bMark, bBefore);
        GuildSnapshot after = _clear.QueryGuild(a);
        Assert.Equal(GuildWire.MaxTreasury, after.Treasury);
        Assert.Equal(expectedRevision, after.GuildRevision);
    }

    // I3 / I8 / contract item 3: a credit is needed (treasury 1,000, space left) but the communal revision is at
    // uint.MaxValue. Neither side commits and nothing is reported as success: no 39, no 36, both personal states
    // and the communal state unchanged. The server keeps each beneficiary's 40, which still moves it to Town
    // (this is not the UI's way out). Named limit state: GuildBoundaryFixture.ReplaceCommunalState.
    [Fact]
    public void ANeededCreditAtTheRevisionLimit_CommitsNeitherSide_AndKeepsTheConfirm()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        GuildBoundaryFixture.ReplaceCommunalState(Guild, uint.MaxValue, 1_000, strongholdLevel: 1);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        Guild.KillBoss(room, a);
        Guild.Settle();

        Assert.Empty(Since(a, aMark, ClearResultId, GuildWire.InventorySnapshotId));
        Assert.Empty(Since(b, bMark, ClearResultId, GuildWire.InventorySnapshotId));
        AssertSameInventory(aBefore, _clear.QueryInventory(a));
        AssertSameInventory(bBefore, _clear.QueryInventory(b));
        GuildSnapshot after = _clear.QueryGuild(a);
        Assert.Equal(1_000, after.Treasury);
        Assert.Equal(uint.MaxValue, after.GuildRevision);
        Guild.ConfirmToTown(a);
        Guild.ConfirmToTown(b);
    }

    // I3 / technical contract 「snapshotSequence」 and contract item 3: a's connection has already received
    // long.MaxValue 44s, so its credit 44 cannot be numbered and the server stops and closes that connection. That
    // is a preparation failure: neither side commits for a and nothing is reported as success. b is the positive
    // control: 10 + 10 = 20. Named limit state: GuildBoundaryFixture.SetSnapshotSequence.
    [Fact]
    public void ABeneficiaryWhoseCredit44CannotBeNumbered_CommitsNeitherSide_WhileTheOtherIsCredited()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);
        GuildBoundaryFixture.SetSnapshotSequence(Guild, a, long.MaxValue);

        Guild.KillBoss(room, b);
        Guild.Settle();

        Assert.DoesNotContain(Since(a, aMark, ClearResultId), IsSuccessResult);
        Assert.Empty(Since(a, aMark, GuildWire.InventorySnapshotId));
        AssertPaidAndCredited(b, bMark, bBefore);
        Assert.Equal((2 * Seed) + ClearCredit, _clear.QueryGuild(b).Treasury);
        Assert.True(a.IsClosing, "technical contract: a connection whose next 44 cannot be numbered is closed");
    }

    // I3 / D10 / contract item 5: a send to f fails after the commit (its 39, its 36 or its own 44). The committed
    // reward and credit stay, nothing is paid or credited again (no retry; a later 40, 43 and idle ticks add
    // nothing), and the other recipients, beneficiary g and online member o, still get their 44:
    // 3 seeds + 2 credits = 35.
    [Theory]
    [InlineData(GuildTestWorld.DungeonClearResultId)]
    [InlineData(GuildWire.InventorySnapshotId)]
    [InlineData(GuildWire.GuildSnapshotId)]
    public void ASendFailureAfterTheCommit_KeepsBothSides_PaysNothingAgain_AndOthersStillGetTheCredit(ushort failing)
    {
        GuildFaultClient f = GuildBoundaryFixture.EnterFaultClient(Guild);
        GuildClient g = Guild.JoinAndEnter(CharacterClass.Knight);
        GuildClient o = Guild.JoinAndEnter(CharacterClass.Knight);
        _clear.FormParty(f, g);
        Guild.UnlockBossRoom(f);
        GameMap room = _clear.EnterSameBossRoom(f, g);
        InventorySnapshot fBefore = _clear.QueryInventory(f);
        InventorySnapshot gBefore = _clear.QueryInventory(g);
        int gMark = MarkOf(g);
        int oMark = MarkOf(o);

        f.ThrowOnSendOf(failing);
        Guild.KillBoss(room, g);
        Guild.Settle();
        f.ClearFaults();
        InventorySnapshot fCommitted = _clear.QueryInventory(f);
        List<GuildSnapshot> toObserver = SnapshotsSince(o, oMark);

        AssertGained(fBefore, fCommitted);
        AssertPaidAndCredited(g, gMark, gBefore);
        Assert.Contains(toObserver, snapshot => snapshot.ChangeKind == ChangeKind.TreasuryCredit);
        Assert.Equal((3 * Seed) + (2 * ClearCredit), toObserver[^1].Treasury);
        int treasury = _clear.QueryGuild(o).Treasury;
        Assert.Equal((3 * Seed) + (2 * ClearCredit), treasury);

        int laterMark = MarkOf(f);
        Guild.Settle(StayTicks);
        Guild.ConfirmToTown(f);
        _clear.QueryGuild(f);
        Assert.Empty(Since(f, laterMark, ClearResultId));
        AssertSameInventory(fCommitted, _clear.QueryInventory(f));
        Assert.Equal(treasury, _clear.QueryGuild(o).Treasury);
        Assert.True(f.ThrownSends >= 1, "fixture: the injected send failure must have been reached");
    }

    // G2 「같은 보스 사망 중복·확인 두 번」 / I2 / contract item 5: the same boss death delivered twice in one room job
    // is one clear (Content's stage-clear guard). Then a's two 40s in one receive burst, b's 40, 43 queries and
    // idle ticks. Each beneficiary is paid once and the treasury stays 10 + 20 = 30.
    [Fact]
    public void RepeatedDeathConfirmAndQuery_PayAndCreditOnlyOnce()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        InventorySnapshot aBefore = _clear.QueryInventory(a);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        _clear.KillBossThen(room, a, boss => room.HandleEnemyDeath(boss, a.EntityId));
        Guild.Settle();
        InventorySnapshot aPaid = AssertPaidAndCredited(a, aMark, aBefore);
        InventorySnapshot bPaid = AssertPaidAndCredited(b, bMark, bBefore);
        int treasury = _clear.QueryGuild(a).Treasury;
        a.OnRecvPacket(GuildTestWorld.ClearConfirm());
        a.OnRecvPacket(GuildTestWorld.ClearConfirm());
        Guild.Settle();
        Guild.ConfirmToTown(b);
        _clear.QueryGuild(a);
        _clear.QueryGuild(b);
        Guild.Settle(StayTicks);

        Assert.Equal((2 * Seed) + (2 * ClearCredit), treasury);
        Assert.Equal(MapId.Town, Guild.MapOf(a).MapId);
        Assert.Single(Since(a, aMark, ClearResultId));
        Assert.Single(Since(b, bMark, ClearResultId));
        AssertSameInventory(aPaid, _clear.QueryInventory(a));
        AssertSameInventory(bPaid, _clear.QueryInventory(b));
        Assert.Equal(treasury, _clear.QueryGuild(b).Treasury);
    }

    // G2 「같은 복사본이 살아남아 보스가 재출현한 다음 정상 클리어는 다시 한 번 적립」 / I2, as
    // DungeonClearRepeatClearTests: a and b clear (10 → 30), b confirms to Town and stands at the boss room portal
    // of a newer hunting ground copy. In one World tick a's 40 empties the room, whose map tick respawns the boss,
    // and b's entry is queued, which keeps the empty room. b kills the new boss in the same copy: one more payment
    // for b and one more credit, 30 + 10 = 40. a, already in Town, gets nothing more.
    [Fact]
    public void ABossRespawnedInTheSameCopy_IsANewClear_ThatCreditsAgain()
    {
        (GuildClient a, GuildClient b) = _clear.JoinParty();
        GameMap room = _clear.EnterSameBossRoom(a, b);
        Guild.KillBoss(room, a);
        Guild.Settle();
        int firstTreasury = _clear.QueryGuild(a).Treasury;
        Guild.ConfirmToTown(b);
        GameMap hunting = Guild.MoveThroughPortal(b, TownToHuntingGroundPortalId);
        InventorySnapshot bBefore = _clear.QueryInventory(b);
        int aMark = MarkOf(a);
        int bMark = MarkOf(b);

        Portal toBossRoom = PortalTable.GetPortalsFor(MapId.HuntingGround).Single(p => p.PortalId == HuntingGroundToBossRoomPortalId);
        a.OnRecvPacket(GuildTestWorld.ClearConfirm());
        Guild.PlayerOf(b).Position = toBossRoom.Position;
        b.OnRecvPacket(new C_EnterPortal { portalId = toBossRoom.PortalId }.Write());
        Guild.Tick();
        int playersAfterTheTick = room.Players.Count;
        int bossesAfterTheTick = room.Enemies.Values.Count(enemy => enemy.Kind == EnemyKind.Boss);
        Guild.Settle();
        GameMap bArrivedIn = Guild.MapOf(b);
        Guild.KillBoss(room, b);
        Guild.Settle();

        Assert.Equal(MapId.HuntingGround, hunting.MapId);
        Assert.Equal(0, playersAfterTheTick);
        Assert.Equal(1, bossesAfterTheTick);
        Assert.Same(room, bArrivedIn);
        Assert.Equal(MapId.Town, Guild.MapOf(a).MapId);
        Assert.Empty(Since(a, aMark, ClearResultId));
        AssertPaidAndCredited(b, bMark, bBefore);
        Assert.Equal((2 * Seed) + (2 * ClearCredit), firstTreasury);
        Assert.Equal((2 * Seed) + (3 * ClearCredit), _clear.QueryGuild(a).Treasury);
    }

    // Content C2/C3 kept: exactly one Success 39 and one 36, the 36 directly after its 39, one personal revision
    // later with exactly the reward; the inventory query agrees.
    InventorySnapshot AssertPaid(GameSession member, int mark, InventorySnapshot before)
    {
        (_, _, InventorySnapshot paid) = AssertPaidIn(EconomySince(member, mark), before);
        AssertSameInventory(paid, _clear.QueryInventory(member));
        return paid;
    }

    // AssertPaid, then G2 and contract item 5: a TreasuryCredit 44 follows that 36 and carries the member's new
    // personal revision. A 44 pushed before its own 39 (another beneficiary's credit) carries its previous
    // revision, every 44 after the 36 carries the new one, and the connection's sequence only grows (I9).
    InventorySnapshot AssertPaidAndCredited(GameSession member, int mark, InventorySnapshot before)
    {
        List<byte[]> economy = EconomySince(member, mark);
        (int resultAt, int inventoryAt, InventorySnapshot paid) = AssertPaidIn(economy, before);
        List<GuildSnapshot> beforePayment = SnapshotsIn(economy.Take(resultAt));
        List<GuildSnapshot> afterPayment = SnapshotsIn(economy.Skip(inventoryAt + 1));

        Assert.Contains(afterPayment, snapshot => snapshot.ChangeKind == ChangeKind.TreasuryCredit);
        Assert.All(afterPayment, snapshot => Assert.Equal(paid.Revision, snapshot.InventoryRevision));
        Assert.All(beforePayment, snapshot => Assert.Equal(before.Revision, snapshot.InventoryRevision));
        AssertSequenceGrows(SnapshotsIn(economy));
        AssertSameInventory(paid, _clear.QueryInventory(member));
        return paid;
    }

    static (int ResultAt, int InventoryAt, InventorySnapshot Paid) AssertPaidIn(List<byte[]> economy, InventorySnapshot before)
    {
        ushort[] ids = economy.Select(GuildWire.IdOf).ToArray();
        Assert.Single(ids, id => id == ClearResultId);
        Assert.Single(ids, id => id == GuildWire.InventorySnapshotId);
        int resultAt = Array.IndexOf(ids, ClearResultId);
        int inventoryAt = Array.IndexOf(ids, GuildWire.InventorySnapshotId);
        Assert.Equal(resultAt + 1, inventoryAt);
        AssertSuccessResult(economy[resultAt]);
        InventorySnapshot paid = GuildWire.ReadInventorySnapshot(economy[inventoryAt]);
        AssertGained(before, paid);
        return (resultAt, inventoryAt, paid);
    }

    static void AssertSuccessResult(byte[] packet)
    {
        S_DungeonClearResult result = GuildTestWorld.ReadClearResult(packet);
        (int ItemId, int Count)[] rewards =
        {
            (result.reward0ItemId, result.reward0Count),
            (result.reward1ItemId, result.reward1Count),
            (result.reward2ItemId, result.reward2Count),
        };
        (int ItemId, int Count)[] expected = { (GuildWire.MaterialItemId, 1), (GuildWire.CoinPouchItemId, 1) };

        Assert.Equal((byte)InventoryResult.Success, result.result);
        Assert.Equal(ClearCurrency, result.currency);
        Assert.Equal(expected, rewards.Where(reward => reward != (0, 0)).OrderBy(reward => reward.ItemId).ToArray());
    }

    // Content C3: a refusal carries only its code, currency 0 and three empty reward slots.
    static void AssertRefusedResult(byte[] packet, InventoryResult expected)
    {
        S_DungeonClearResult result = GuildTestWorld.ReadClearResult(packet);
        (int ItemId, int Count)[] rewards =
        {
            (result.reward0ItemId, result.reward0Count),
            (result.reward1ItemId, result.reward1Count),
            (result.reward2ItemId, result.reward2Count),
        };

        Assert.Equal((byte)expected, result.result);
        Assert.Equal(0, result.currency);
        Assert.All(rewards, reward => Assert.Equal((0, 0), reward));
    }

    // The existing refusal code the two refusal rows expect in their 39.
    static InventoryResult RefusalOf(string limit)
        => limit == "inventory-full" ? InventoryResult.InventoryFull : InventoryResult.CurrencyCap;

    static bool IsSuccessResult(byte[] packet) => GuildTestWorld.ReadClearResult(packet).result == (byte)InventoryResult.Success;

    static void AssertGained(InventorySnapshot before, InventorySnapshot after)
    {
        Assert.Equal(before.Revision + 1, after.Revision);
        Assert.Equal(before.Currency + ClearCurrency, after.Currency);
        Assert.Equal(before.CountOf(GuildWire.MaterialItemId) + 1, after.CountOf(GuildWire.MaterialItemId));
        Assert.Equal(before.CountOf(GuildWire.CoinPouchItemId) + 1, after.CountOf(GuildWire.CoinPouchItemId));
    }

    static void AssertSameInventory(InventorySnapshot expected, InventorySnapshot actual)
    {
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.Currency, actual.Currency);
        Assert.Equal(expected.Counts.OrderBy(pair => pair.Key), actual.Counts.OrderBy(pair => pair.Key));
    }

    static void AssertSequenceGrows(List<GuildSnapshot> snapshots)
    {
        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.True(snapshots[i].Sequence > snapshots[i - 1].Sequence, "a connection's 44 sequence only grows");
        }
    }

    static List<GuildSnapshot> SnapshotsIn(IEnumerable<byte[]> packets)
        => packets.Where(packet => GuildWire.IdOf(packet) == GuildWire.GuildSnapshotId).Select(GuildTestWorld.ReadSnapshot).ToList();

    static int[] ClearResultCounts(GameSession[] members, int[] marks)
        => members.Select((member, i) => Since(member, marks[i], ClearResultId).Count).ToArray();
}
