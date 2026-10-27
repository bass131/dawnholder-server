using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using System.Text;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Guild.IndependentSpec;

namespace Dawnholder.Server.GameServer.Tests.Guild;

// Independent strong verification of the guild foundation (technical contract v1: 「패킷 필드와 결과」,
// D4, D8, D9; G1, G3, G4, G7). The guild request bytes and the reading of every 36/44/47 the server
// sends follow the byte tables of the contract (IndependentWire), not the helpers of the earlier test
// author. Every expectation is a contract literal (IndependentSpec) or a before/after relation; no
// product rule, packet builder or damage formula computes one.
// Entry: real handshake → C_Login → C_CharacterSelect → GameSession.OnRecvPacket → HandlerRegistry →
// GameWorld.OnTick, observed at the session's Send. Stand-ins: Send is a list (no socket) and OnTick is
// called directly instead of the TickScheduler. Not covered: the Content clear credit (G2), the Unity
// and bot consumers, a real socket, DB.
[Collection("GameWorldRegistryTests")]
public sealed class GuildIndependentContractTests : IDisposable
{
    readonly IndependentGuildWorld _world = new();

    public void Dispose() => _world.Dispose();

    // The first real admission is one 76-byte 44 at the fixed offsets with the approved starting state.
    // Availability is D4 without the revision check: the treasury 5 cannot pay 30, and a level-0 weapon
    // at a locked blacksmith shows FacilityLocked with the display price 20 / Material ID 1 x1.
    [Fact]
    public void FirstAdmission_IsOneSeventySixByteSnapshot_WithTheApprovedStartingState()
    {
        IndependentSession first = _world.Enter();

        byte[] raw = Assert.Single(first.Since(0, WireId.GuildSnapshot));
        Assert.Equal(76, raw.Length);
        IndependentSnapshot admitted = IndependentWire.ReadSnapshot(raw);
        Assert.Equal(1, admitted.Sequence);
        Assert.Equal(Kind.Membership, admitted.ChangeKind);
        Assert.Equal(Approved.Seed, admitted.Treasury);
        Assert.Equal(1, admitted.StrongholdLevel);
        Assert.Equal(0, admitted.BlacksmithUnlocked);
        Assert.Equal(1, admitted.MemberCount);
        Assert.Equal(1, admitted.OnlineMemberCount);
        Assert.Equal(0u, admitted.InventoryRevision);
        Assert.Equal(0, admitted.WeaponLevel);
        Assert.Equal(0, admitted.AttackBonus);
        Assert.Equal(Approved.StrongholdPrice, admitted.StrongholdCost);
        Assert.Equal(Approved.WeaponPrice, admitted.WeaponCurrencyCost);
        Assert.Equal(1, admitted.WeaponMaterialId);
        Assert.Equal(1, admitted.WeaponMaterialCount);
        Assert.Equal(Code.InsufficientCurrency, admitted.StrongholdAvailability);
        Assert.Equal(Code.FacilityLocked, admitted.WeaponAvailability);

        int firstMark = first.Mark;
        IndependentSession second = _world.Enter();

        IndependentSnapshot secondAdmitted = IndependentWire.ReadSnapshot(Assert.Single(second.Since(0, WireId.GuildSnapshot)));
        IndependentSnapshot pushed = IndependentWire.ReadSnapshot(Assert.Single(first.Since(firstMark, WireId.GuildSnapshot)));
        Assert.Equal(1, secondAdmitted.Sequence);
        Assert.Equal(2 * Approved.Seed, secondAdmitted.Treasury);
        Assert.True(secondAdmitted.GuildRevision > admitted.GuildRevision, "a credited seed advances the communal revision");
        Assert.Equal(2, pushed.Sequence);
        Assert.Equal(Kind.Membership, pushed.ChangeKind);
        foreach (IndependentSnapshot snapshot in new[] { secondAdmitted, pushed })
        {
            Assert.Equal(secondAdmitted.GuildRevision, snapshot.GuildRevision);
            Assert.Equal(2 * Approved.Seed, snapshot.Treasury);
            Assert.Equal(2, snapshot.MemberCount);
            Assert.Equal(2, snapshot.OnlineMemberCount);
        }
    }

    // D8: a business refusal is 47 → the caller's current 44, with no 36 and no push to others. The 47
    // echoes the requested revision, reports the current revisions and points at that 44's sequence.
    // D4: the expected-revision check comes before the treasury check.
    [Fact]
    public void StrongholdRefusals_Answer47Then44_EchoingTheRequest_AndChangeNothing()
    {
        IndependentSession buyer = _world.Enter();
        IndependentSession other = _world.Enter();
        IndependentSnapshot before = _world.Query(buyer);
        int otherMark = other.Mark;
        uint staleRevision = unchecked(before.GuildRevision + 7);

        List<byte[]> poor = _world.Act(buyer, IndependentWire.StrongholdUpgrade(before.GuildRevision));
        List<byte[]> stale = _world.Act(buyer, IndependentWire.StrongholdUpgrade(staleRevision));

        IndependentWire.AssertRefusal(poor, Purchase.Stronghold, Code.InsufficientCurrency, before.GuildRevision, before);
        IndependentWire.AssertRefusal(stale, Purchase.Stronghold, Code.Stale, staleRevision, before);
        Assert.Empty(other.Since(otherMark, WireId.GuildSnapshot, WireId.GuildActionResult));
        IndependentSnapshot after = _world.Query(buyer);
        Assert.Equal(before.GuildRevision, after.GuildRevision);
        Assert.Equal(2 * Approved.Seed, after.Treasury);
        Assert.Equal(1, after.StrongholdLevel);
    }

    // D4 for the weapon (owner → movement → Town → alive → expected revision → highest level →
    // blacksmith → revision exhaustion → currency → material) and D8 (only Stale or success adds a 36).
    // The members have no personal funds, so a check misplaced after the expected one changes the code.
    [Fact]
    public void WeaponRefusals_FollowTheD4Order_AndOnlyStaleOrSuccessSendsTheInventory()
    {
        IndependentSession[] members = _world.EnterMany(6);
        IndependentSession poor = members[0];

        IndependentSnapshot locked = _world.Query(poor);
        IndependentWire.AssertRefusal(_world.Act(poor, IndependentWire.WeaponEnhance(locked.InventoryRevision)),
            Purchase.Weapon, Code.FacilityLocked, locked.InventoryRevision, locked);

        _world.UpgradeStronghold(members[1]);
        IndependentSnapshot unlocked = _world.Query(poor);
        Assert.Equal(Code.MaxLevel, unlocked.StrongholdAvailability);
        Assert.Equal(Code.InsufficientCurrency, unlocked.WeaponAvailability);
        IndependentWire.AssertRefusal(_world.Act(poor, IndependentWire.WeaponEnhance(unlocked.InventoryRevision)),
            Purchase.Weapon, Code.InsufficientCurrency, unlocked.InventoryRevision, unlocked);

        // Fixture limit, as StrongholdUpgradeTests: a lethal hit revives within the same call, so HP 0 is
        // written directly to observe the 「생존」 check alone. It comes before the expected revision.
        IndependentSession dead = members[2];
        IndependentSnapshot alive = _world.Query(dead);
        _world.PlayerOf(dead).Hp = 0;
        uint deadRequest = unchecked(alive.InventoryRevision + 999);
        List<byte[]> deadReplies = _world.Act(dead, IndependentWire.WeaponEnhance(deadRequest));
        IndependentWire.AssertRefusal(deadReplies, Purchase.Weapon, Code.Dead, deadRequest, alive);
        IndependentSnapshot deadShown = IndependentWire.ReadSnapshot(deadReplies[1]);
        Assert.Equal(Code.Dead, deadShown.StrongholdAvailability);
        Assert.Equal(Code.Dead, deadShown.WeaponAvailability);

        // Outside Town precedes alive, revision, highest level and funds; the query is answered anywhere.
        _world.MoveOutOfTown(poor);
        IndependentSnapshot away = _world.Query(poor);
        Assert.Equal(Code.NotInTown, away.StrongholdAvailability);
        Assert.Equal(Code.NotInTown, away.WeaponAvailability);
        uint awayRequest = unchecked(away.InventoryRevision + 999);
        IndependentWire.AssertRefusal(_world.Act(poor, IndependentWire.WeaponEnhance(awayRequest)),
            Purchase.Weapon, Code.NotInTown, awayRequest, away);
    }

    // G4 / D5 / D8: one success spends 20 + Material 1 together with weapon 0 → 1 and bonus 0 → 5, answered
    // 47 → 36 → 44 under one new personal revision. A later payable request with the current revision is
    // MaxLevel and consumes nothing (D4: the highest level precedes the funds).
    [Fact]
    public void WeaponSuccess_Answers47Then36Then44_AndALaterPayableRequestIsMaxLevel()
    {
        IndependentSession[] members = _world.EnterMany(6);
        IndependentSession smith = members[0];
        _world.UpgradeStronghold(members[1]);
        _world.KillBoss(smith);
        IndependentInventory funded = _world.QueryInventory(smith);
        Assert.Equal(Approved.BossCurrency, funded.Currency);
        Assert.Equal(1, funded.Materials);
        IndependentSnapshot guildBefore = _world.Query(smith);

        List<byte[]> replies = _world.Act(smith, IndependentWire.WeaponEnhance(funded.Revision));

        Assert.Equal(new[] { WireId.GuildActionResult, WireId.InventorySnapshot, WireId.GuildSnapshot }, IndependentWire.Ids(replies));
        IndependentResult result = IndependentWire.ReadResult(replies[0]);
        IndependentInventory spent = IndependentWire.ReadInventory(replies[1]);
        IndependentSnapshot grown = IndependentWire.ReadSnapshot(replies[2]);
        Assert.Equal(Purchase.Weapon, result.Action);
        Assert.Equal(Code.Success, result.Result);
        Assert.Equal(funded.Revision, result.RequestedRevision);
        Assert.True(spent.Revision > funded.Revision, "a committed enhance advances the personal revision");
        Assert.Equal(spent.Revision, result.InventoryRevision);
        Assert.Equal(spent.Revision, grown.InventoryRevision);
        Assert.Equal(guildBefore.GuildRevision, result.GuildRevision);
        Assert.Equal(grown.Sequence, result.Sequence);
        Assert.Equal(funded.Currency - Approved.WeaponPrice, spent.Currency);
        Assert.Equal(0, spent.Materials);
        Assert.Equal(funded.CoinPouches, spent.CoinPouches);
        Assert.Equal(1, grown.WeaponLevel);
        Assert.Equal(Approved.WeaponBonus, grown.AttackBonus);
        Assert.Equal(Code.MaxLevel, grown.WeaponAvailability);
        Assert.Equal(guildBefore.Treasury, grown.Treasury);

        _world.KillBoss(smith);
        IndependentInventory refunded = _world.QueryInventory(smith);
        IndependentSnapshot enhanced = _world.Query(smith);
        IndependentWire.AssertRefusal(_world.Act(smith, IndependentWire.WeaponEnhance(refunded.Revision)),
            Purchase.Weapon, Code.MaxLevel, refunded.Revision, enhanced);
        Assert.Equal(refunded, _world.QueryInventory(smith));
        Assert.Equal(1, _world.Query(smith).WeaponLevel);
    }

    // D8: a push carries the recipient's own personal values and location with the same communal state.
    // Treasury: 6 seeds − the stronghold 30 + the clear credit of the one beneficiary who funded the enhance
    // (G2, approved A) + the newcomer's seed = 15.
    [Fact]
    public void MembershipPush_CarriesEachRecipientsOwnWeaponAndLocation()
    {
        IndependentSession[] members = _world.EnterMany(6);
        IndependentSession enhanced = members[0];
        IndependentSession away = members[1];
        _world.UpgradeStronghold(members[2]);
        _world.KillBoss(enhanced);
        List<byte[]> success = _world.Act(enhanced, IndependentWire.WeaponEnhance(_world.QueryInventory(enhanced).Revision));
        uint enhancedRevision = IndependentWire.ReadInventory(
            Assert.Single(success, packet => IndependentWire.IdOf(packet) == WireId.InventorySnapshot)).Revision;
        _world.MoveOutOfTown(away);
        uint awayRevision = _world.QueryInventory(away).Revision;
        int enhancedMark = enhanced.Mark;
        int awayMark = away.Mark;

        IndependentSession newcomer = _world.Enter();

        IndependentSnapshot own = IndependentWire.ReadSnapshot(Assert.Single(newcomer.Since(0, WireId.GuildSnapshot)));
        IndependentSnapshot toEnhanced = IndependentWire.ReadSnapshot(Assert.Single(enhanced.Since(enhancedMark, WireId.GuildSnapshot)));
        IndependentSnapshot toAway = IndependentWire.ReadSnapshot(Assert.Single(away.Since(awayMark, WireId.GuildSnapshot)));
        foreach (IndependentSnapshot snapshot in new[] { own, toEnhanced, toAway })
        {
            Assert.Equal(Kind.Membership, snapshot.ChangeKind);
            Assert.Equal(own.GuildRevision, snapshot.GuildRevision);
            Assert.Equal((6 * Approved.Seed) - Approved.StrongholdPrice + Approved.ClearCredit + Approved.Seed, snapshot.Treasury);
            Assert.Equal(7, snapshot.MemberCount);
            Assert.Equal(7, snapshot.OnlineMemberCount);
        }
        Assert.Equal(0, own.WeaponLevel);
        Assert.Equal(1, toEnhanced.WeaponLevel);
        Assert.Equal(Approved.WeaponBonus, toEnhanced.AttackBonus);
        Assert.Equal(enhancedRevision, toEnhanced.InventoryRevision);
        Assert.Equal(Code.MaxLevel, toEnhanced.WeaponAvailability);
        Assert.Equal(0, toAway.WeaponLevel);
        Assert.Equal(awayRevision, toAway.InventoryRevision);
        Assert.Equal(Code.NotInTown, toAway.StrongholdAvailability);
        Assert.Equal(Code.NotInTown, toAway.WeaponAvailability);
    }
}

// Literals of technical contract v1 (approved A values, packet IDs, changeKind and result codes) and of
// the existing v18 Content clear reward. Kept apart from product constants on purpose.
internal static class IndependentSpec
{
    internal static class WireId
    {
        internal const ushort InventoryRequest = 35;
        internal const ushort InventorySnapshot = 36;
        internal const ushort DungeonClearResult = 39;
        internal const ushort GuildRequest = 43;
        internal const ushort GuildSnapshot = 44;
        internal const ushort StrongholdUpgrade = 45;
        internal const ushort WeaponEnhance = 46;
        internal const ushort GuildActionResult = 47;
    }

    internal static class Kind
    {
        internal const byte Query = 0;
        internal const byte Membership = 1;
        internal const byte StrongholdUpgrade = 3;
        internal const byte Presence = 5;
    }

    internal static class Code
    {
        internal const byte Success = 0;
        internal const byte Stale = 1;
        internal const byte NotInTown = 3;
        internal const byte Dead = 4;
        internal const byte FacilityLocked = 5;
        internal const byte MaxLevel = 6;
        internal const byte InsufficientCurrency = 7;
        internal const byte Unavailable = 10;
    }

    internal static class Purchase
    {
        internal const byte Stronghold = 1;
        internal const byte Weapon = 2;
    }

    internal static class Approved
    {
        internal const int Seed = 5;
        internal const int StrongholdPrice = 30;
        internal const int WeaponPrice = 20;
        internal const int WeaponBonus = 5;
        internal const int TreasuryCap = 1_000_000_000;
        internal const int ClearCredit = 10;
        internal const int BossCurrency = 50;
        internal const string GuildName = "새벽 길드";
    }
}

internal readonly record struct IndependentSnapshot(
    long Sequence, byte ChangeKind, uint GuildId, uint GuildRevision, int Treasury,
    byte StrongholdLevel, byte MaxStrongholdLevel, byte BlacksmithUnlocked, int MemberCount, int OnlineMemberCount,
    uint InventoryRevision, byte WeaponLevel, byte MaxWeaponLevel, int AttackBonus, int StrongholdCost,
    int WeaponCurrencyCost, int WeaponMaterialId, int WeaponMaterialCount,
    byte StrongholdAvailability, byte WeaponAvailability, string GuildName);

internal readonly record struct IndependentResult(
    byte Action, byte Result, uint RequestedRevision, uint GuildRevision, uint InventoryRevision, long Sequence);

internal readonly record struct IndependentInventory(uint Revision, int Currency, int Materials, int CoinPouches);

// Request bytes and reply reading by the offsets of the contract tables (LittleEndian, 4-byte size/id
// header included). Every 44 read here is also checked against 「Snapshot의 범위」.
internal static class IndependentWire
{
    const int SnapshotFixedBytes = 66;
    const int ResultBytes = 26;
    const int InventoryBytes = 76;
    const int MaterialItemId = 1;
    const int CoinPouchItemId = 2;

    internal static byte[] GuildRequest() => Frame(WireId.GuildRequest, new byte[] { 0 });

    internal static byte[] StrongholdUpgrade(uint expectedGuildRevision)
        => Frame(WireId.StrongholdUpgrade, LittleEndianUInt32(expectedGuildRevision));

    internal static byte[] WeaponEnhance(uint expectedInventoryRevision)
        => Frame(WireId.WeaponEnhance, LittleEndianUInt32(expectedInventoryRevision));

    internal static byte[] InventoryRequest() => Frame(WireId.InventoryRequest, new byte[] { 0 });

    internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

    internal static ushort[] Ids(List<byte[]> packets) => packets.Select(IdOf).ToArray();

    internal static IndependentSnapshot ReadSnapshot(byte[] packet)
    {
        Assert.True(packet.Length >= SnapshotFixedBytes, $"44 shorter than {SnapshotFixedBytes} bytes");
        Assert.Equal(WireId.GuildSnapshot, IdOf(packet));
        Assert.Equal(packet.Length, BinaryPrimitives.ReadUInt16LittleEndian(packet));
        int nameBytes = BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(64, 2));
        Assert.InRange(nameBytes, 2, 32);
        Assert.Equal(0, nameBytes % 2);
        Assert.Equal(SnapshotFixedBytes + nameBytes, packet.Length);

        ReadOnlySpan<byte> s = packet;
        IndependentSnapshot snapshot = new(
            Sequence: BinaryPrimitives.ReadInt64LittleEndian(s[4..]),
            ChangeKind: s[12],
            GuildId: BinaryPrimitives.ReadUInt32LittleEndian(s[13..]),
            GuildRevision: BinaryPrimitives.ReadUInt32LittleEndian(s[17..]),
            Treasury: BinaryPrimitives.ReadInt32LittleEndian(s[21..]),
            StrongholdLevel: s[25],
            MaxStrongholdLevel: s[26],
            BlacksmithUnlocked: s[27],
            MemberCount: BinaryPrimitives.ReadInt32LittleEndian(s[28..]),
            OnlineMemberCount: BinaryPrimitives.ReadInt32LittleEndian(s[32..]),
            InventoryRevision: BinaryPrimitives.ReadUInt32LittleEndian(s[36..]),
            WeaponLevel: s[40],
            MaxWeaponLevel: s[41],
            AttackBonus: BinaryPrimitives.ReadInt32LittleEndian(s[42..]),
            StrongholdCost: BinaryPrimitives.ReadInt32LittleEndian(s[46..]),
            WeaponCurrencyCost: BinaryPrimitives.ReadInt32LittleEndian(s[50..]),
            WeaponMaterialId: BinaryPrimitives.ReadInt32LittleEndian(s[54..]),
            WeaponMaterialCount: BinaryPrimitives.ReadInt32LittleEndian(s[58..]),
            StrongholdAvailability: s[62],
            WeaponAvailability: s[63],
            GuildName: Encoding.Unicode.GetString(packet, SnapshotFixedBytes, nameBytes));
        AssertWithinContract(snapshot);
        return snapshot;
    }

    internal static IndependentResult ReadResult(byte[] packet)
    {
        Assert.Equal(ResultBytes, packet.Length);
        Assert.Equal(ResultBytes, BinaryPrimitives.ReadUInt16LittleEndian(packet));
        Assert.Equal(WireId.GuildActionResult, IdOf(packet));
        ReadOnlySpan<byte> s = packet;
        return new IndependentResult(
            Action: s[4],
            Result: s[5],
            RequestedRevision: BinaryPrimitives.ReadUInt32LittleEndian(s[6..]),
            GuildRevision: BinaryPrimitives.ReadUInt32LittleEndian(s[10..]),
            InventoryRevision: BinaryPrimitives.ReadUInt32LittleEndian(s[14..]),
            Sequence: BinaryPrimitives.ReadInt64LittleEndian(s[18..]));
    }

    // v17 36: uint revision, int currency, then 8 × (int itemId, int count).
    internal static IndependentInventory ReadInventory(byte[] packet)
    {
        Assert.Equal(InventoryBytes, packet.Length);
        Assert.Equal(WireId.InventorySnapshot, IdOf(packet));
        int materials = 0;
        int pouches = 0;
        for (int slot = 0; slot < 8; slot++)
        {
            int itemId = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(12 + (8 * slot)));
            int count = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(16 + (8 * slot)));
            if (itemId == MaterialItemId) materials += count;
            if (itemId == CoinPouchItemId) pouches += count;
        }
        return new IndependentInventory(
            BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4)),
            BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8)),
            materials,
            pouches);
    }

    // D8 business refusal: exactly 47 → 44 (no 36), the request echoed, the current revisions unchanged, the
    // 47 pointing at the following 44, whose sequence is newer than the caller's previous one.
    internal static void AssertRefusal(List<byte[]> replies, byte purchase, byte code, uint requested, IndependentSnapshot current)
    {
        Assert.Equal(new[] { WireId.GuildActionResult, WireId.GuildSnapshot }, Ids(replies));
        IndependentResult result = ReadResult(replies[0]);
        IndependentSnapshot snapshot = ReadSnapshot(replies[1]);
        Assert.Equal(purchase, result.Action);
        Assert.Equal(code, result.Result);
        Assert.Equal(requested, result.RequestedRevision);
        Assert.Equal(current.GuildRevision, result.GuildRevision);
        Assert.Equal(current.InventoryRevision, result.InventoryRevision);
        Assert.Equal(snapshot.Sequence, result.Sequence);
        Assert.True(snapshot.Sequence > current.Sequence, "the refusal 44 must be newer than the caller's last 44");
        Assert.Equal(current.GuildRevision, snapshot.GuildRevision);
        Assert.Equal(current.Treasury, snapshot.Treasury);
        Assert.Equal(current.StrongholdLevel, snapshot.StrongholdLevel);
        Assert.Equal(current.InventoryRevision, snapshot.InventoryRevision);
        Assert.Equal(current.WeaponLevel, snapshot.WeaponLevel);
    }

    // 「Snapshot의 범위」 and the display rules of technical contract v1.
    static void AssertWithinContract(IndependentSnapshot s)
    {
        Assert.True(s.Sequence > 0, "snapshotSequence starts at 1 per connection");
        Assert.InRange(s.ChangeKind, (byte)0, (byte)5);
        Assert.Equal(1u, s.GuildId);
        Assert.Equal(Approved.GuildName, s.GuildName);
        Assert.InRange(s.Treasury, 0, Approved.TreasuryCap);
        Assert.InRange(s.StrongholdLevel, (byte)1, (byte)2);
        Assert.Equal(2, s.MaxStrongholdLevel);
        Assert.Equal(s.StrongholdLevel == 2 ? 1 : 0, s.BlacksmithUnlocked);
        Assert.True(s.MemberCount >= s.OnlineMemberCount && s.OnlineMemberCount >= 0, "members >= online >= 0");
        Assert.InRange(s.WeaponLevel, (byte)0, (byte)1);
        Assert.Equal(1, s.MaxWeaponLevel);
        Assert.Equal(s.WeaponLevel == 1 ? Approved.WeaponBonus : 0, s.AttackBonus);
        Assert.Equal(s.StrongholdLevel == 2 ? 0 : Approved.StrongholdPrice, s.StrongholdCost);
        Assert.Equal(s.WeaponLevel == 1 ? 0 : Approved.WeaponPrice, s.WeaponCurrencyCost);
        Assert.Equal(s.WeaponLevel == 1 ? 0 : 1, s.WeaponMaterialCount);
        if (s.WeaponLevel == 0) Assert.Equal(1, s.WeaponMaterialId);
        Assert.InRange(s.StrongholdAvailability, (byte)0, (byte)10);
        Assert.InRange(s.WeaponAvailability, (byte)0, (byte)10);
    }

    static byte[] Frame(ushort id, byte[] payload)
    {
        byte[] packet = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(packet, (ushort)packet.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2), id);
        payload.CopyTo(packet, 4);
        return packet;
    }

    static byte[] LittleEndianUInt32(uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }
}

// Real-entry world for the independent tests. Treasury comes only from real admissions (seed 5 each),
// personal funds only from a real BossRoom clear (the Content clear reward), and leaving Town only through
// the real portal request after a fixture placement next to the portal (PortalTable: Town x = 25 and
// hunting ground x = 25, portal 1 each).
internal sealed class IndependentGuildWorld : IDisposable
{
    // Test allowances, not contract values: replies leave from the World tick or a map job.
    internal const int ReplyTicks = 3;
    const int TownPortalId = 1;
    const int BossRoomPortalId = 1;
    const int BossUnlockKills = 20;
    static readonly Vector2 TownPortalPosition = new(25f, 0f);
    static readonly Vector2 BossRoomPortalPosition = new(56f, 1f);

    // Only the boss room has map data: its one boss, far from the arrival point x = 30. Town and the hunting
    // ground stay empty as before.
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>
    {
        [MapId.BossRoom] = (null, new MapContent(0f, 0f, new[] { new EnemySpawnPoint((byte)EnemyKind.Boss, 60f, 0f) })),
    });
    long _tick;
    int _accountSerial;

    public void Dispose() => _world.Stop();

    internal void Tick(int count = 1)
    {
        for (int i = 0; i < count; i++) LifecycleTestWorld.Tick(_world, ++_tick);
    }

    internal IndependentSession Enter(string? accountName = null)
    {
        IndependentSession session = new(_world, accountName ?? $"indep_{++_accountSerial:D3}");
        session.Connect();
        session.Receive(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.Receive(new C_Login { accountName = session.AccountName }.Write());
        Tick();
        Assert.True(session.IsLoggedIn, "fixture: the real account login must be accepted");
        session.Receive(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight }.Write());
        Tick(ReplyTicks);
        Assert.True(session.EntityId > 0, "fixture: the real entry tick must admit the connection");
        return session;
    }

    internal IndependentSession[] EnterMany(int count)
        => Enumerable.Range(0, count).Select(_ => Enter()).ToArray();

    // D8: a normal query is answered with exactly one 44 of kind Query and no 47.
    internal IndependentSnapshot Query(IndependentSession session)
    {
        int mark = session.Mark;
        session.Receive(IndependentWire.GuildRequest());
        Tick(ReplyTicks);
        IndependentSnapshot snapshot = IndependentWire.ReadSnapshot(
            Assert.Single(session.Since(mark, WireId.GuildSnapshot, WireId.GuildActionResult)));
        Assert.Equal(Kind.Query, snapshot.ChangeKind);
        return snapshot;
    }

    internal IndependentInventory QueryInventory(IndependentSession session)
    {
        int mark = session.Mark;
        session.Receive(IndependentWire.InventoryRequest());
        Tick(ReplyTicks);
        return IndependentWire.ReadInventory(Assert.Single(session.Since(mark, WireId.InventorySnapshot)));
    }

    // One request; the caller's 36/44/47 replies in arrival order.
    internal List<byte[]> Act(IndependentSession session, byte[] request)
    {
        int mark = session.Mark;
        session.Receive(request);
        Tick(ReplyTicks);
        return session.Since(mark, WireId.InventorySnapshot, WireId.GuildSnapshot, WireId.GuildActionResult);
    }

    internal void UpgradeStronghold(IndependentSession buyer)
    {
        IndependentSnapshot before = Query(buyer);
        List<byte[]> replies = Act(buyer, IndependentWire.StrongholdUpgrade(before.GuildRevision));
        IndependentResult result = IndependentWire.ReadResult(
            Assert.Single(replies, packet => IndependentWire.IdOf(packet) == WireId.GuildActionResult));
        Assert.Equal(Code.Success, result.Result);
        Assert.Equal(2, Query(buyer).StrongholdLevel);
    }

    // The Content clear reward (currency 50 + Material 1 + CoinPouch 1 to each beneficiary of a BossRoom copy;
    // since C5 a boss killer gets no kill reward), then the real 40 back to Town. Fixture inputs: the boss room
    // gate through the quest credit entry (20 real kills would add their own rewards), the portal placements,
    // and the room's death handler run in its job as DungeonClearTestWorld.KillBoss does. The clear capture,
    // the settlement and the move to Town are the product path.
    internal void KillBoss(IndependentSession attacker)
    {
        for (int i = 0; i < BossUnlockKills; i++) _world.Quest.OnKill(attacker.EntityId);
        MoveOutOfTown(attacker);
        PlayerOf(attacker).Position = BossRoomPortalPosition;
        attacker.Receive(new C_EnterPortal { portalId = BossRoomPortalId }.Write());
        Tick(ReplyTicks);
        GameMap bossRoom = MapOf(attacker);
        Assert.True(bossRoom.MapId == MapId.BossRoom, "fixture: the real portals must lead to a boss room copy");

        EnemyEntity boss = Assert.Single(bossRoom.Enemies.Values, enemy => enemy.Kind == EnemyKind.Boss);
        int mark = attacker.Mark;
        bossRoom.EnqueueJob(() =>
        {
            boss.Hp = 0;
            bossRoom.HandleEnemyDeath(boss, attacker.EntityId);
        });
        Tick(ReplyTicks);
        byte[] clear = Assert.Single(attacker.Since(mark, WireId.DungeonClearResult));
        Assert.True(clear[4] == Code.Success, "fixture: the real clear must pay this beneficiary");

        attacker.Receive(new C_DungeonClearConfirm { reserved = 0 }.Write());
        Tick(ReplyTicks);
        Assert.True(MapOf(attacker).MapId == MapId.Town, "fixture: the beneficiary's 40 must move it to Town");
    }

    internal void PlaceAtTownPortal(IndependentSession session) => PlayerOf(session).Position = TownPortalPosition;

    internal static byte[] EnterTownPortal() => new C_EnterPortal { portalId = TownPortalId }.Write().ToArray();

    internal void MoveOutOfTown(IndependentSession session)
    {
        PlaceAtTownPortal(session);
        session.Receive(EnterTownPortal());
        Tick(ReplyTicks);
        Assert.NotEqual(MapId.Town, MapOf(session).MapId);
    }

    internal GameMap MapOf(IndependentSession session)
        => _world.AllLiveMaps.Single(map => map.GetPlayer(session.EntityId) != null);

    internal PlayerEntity PlayerOf(IndependentSession session) => MapOf(session).GetPlayer(session.EntityId)!;
}

// The only transport stand-in: every packet the server sends to this connection is kept in order.
internal sealed class IndependentSession : GameSession
{
    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    internal IndependentSession(GameWorld world, string accountName) : base(world) => AccountName = accountName;

    internal string AccountName { get; }

    internal List<byte[]> Sent { get; } = new();

    internal int Mark => Sent.Count;

    public override void Send(ArraySegment<byte> packet) => Sent.Add(packet.ToArray());

    public override void Disconnect() => OnDisconnected(Endpoint);

    internal void Connect() => OnConnected(Endpoint);

    internal void Receive(ArraySegment<byte> packet) => OnRecvPacket(packet);

    internal void Receive(byte[] packet) => OnRecvPacket(new ArraySegment<byte>(packet));

    internal List<byte[]> Since(int mark, params ushort[] ids)
        => Sent.Skip(mark).Where(packet => ids.Contains(IndependentWire.IdOf(packet))).ToList();
}
