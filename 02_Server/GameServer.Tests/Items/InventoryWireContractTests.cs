using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.GameServer.Tests.Maps;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Requirement tests for the server-authoritative economy, written before its implementation.
// Expected values come from the goal's fixed data (01_Phases/goals/2026-10-05-items-inventory-currency/goal.md,
// P-C) and the PR1 acceptance wire table referenced from that goal. Every input goes through the real
// GameSession receive path and the real combat/kill callback; nothing calls economy code directly.
// The economy packets are encoded and decoded here from the fixed table, never from generated code,
// so these tests cannot mirror the generator or the product's own calculation.
[Collection("GameWorldRegistryTests")]
public sealed class InventoryWireContractTests : IDisposable
{
    // Test allowance, not a contract value: a query/use reply may leave from the world tick or a map job,
    // and a kill push goes through SendToEntity, which sends on the next map tick.
    const int SettleTicks = 3;

    // Fixed item data (goal P-C): 0 = None, 1 = material, 2 = coin pouch.
    const int MaterialId = 1;
    const int CoinPouchId = 2;

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    long _tick;

    public void Dispose() => _world.Stop();

    // ── Required behaviour 1: first query ───────────────────────────────────────

    [Fact]
    public void InventoryRequest_AfterTickBoundary_ReturnsEmptyRevisionZeroSnapshot()
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        int mark = session.Packets.Count;

        session.OnRecvPacket(Wire.InventoryRequest());
        // The handler only queues; the reply belongs to the world tick.
        Assert.Empty(session.EconomyPacketsSince(mark));
        Settle();

        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision: 0, currency: 0);
        // Querying again must not advance the revision.
        AssertQueriedState(session, revision: 0, currency: 0);
    }

    // ── Required behaviour 2: one real normal kill ──────────────────────────────

    [Fact]
    public void NormalKill_ThroughRealMelee_PushesMaterialOneCurrencyTenAtRevisionOne()
    {
        EconomySession killer = JoinAndEnter(CharacterClass.Knight);
        EnemyEntity enemy = SpawnOneHitEnemy(EnemyKind.Normal, 1f);
        int mark = killer.Packets.Count;

        KillWithMelee(killer, enemy);
        Settle();

        AssertSnapshot(Assert.Single(killer.EconomyPacketsSince(mark)), revision: 1, currency: 10, (MaterialId, 1));
        AssertQueriedState(killer, revision: 1, currency: 10, (MaterialId, 1));
    }

    // ── Required behaviour 3: two attack jobs on one enemy in the same tick ─────

    [Fact]
    public void SameTickTwoKnights_PositiveControl_BothMeleeHitsReachTheSameLiveEnemy()
    {
        EconomySession first = JoinAndEnter(CharacterClass.Knight);
        EconomySession second = JoinAndEnter(CharacterClass.Knight);
        EnemyEntity enemy = _world.Map.SpawnEnemy(EnemyKind.Normal, 1f, 0f, maxHp: 100);
        int mark = first.Packets.Count;

        SubmitMelee(first, enemy);
        SubmitMelee(second, enemy);
        Tick();

        int[] attackers = Received<S_HitResult>(first, mark)
            .Where(hit => hit.targetEntityId == enemy.EntityId)
            .Select(hit => hit.attackerEntityId)
            .Order()
            .ToArray();
        Assert.Equal(new[] { first.EntityId, second.EntityId }.Order().ToArray(), attackers);
        Assert.True(_world.Map.Enemies.ContainsKey(enemy.EntityId));
    }

    [Fact]
    public void SameTickTwoKnights_LethalTarget_RewardsOnlyTheRealKillerOnce()
    {
        EconomySession first = JoinAndEnter(CharacterClass.Knight);
        EconomySession second = JoinAndEnter(CharacterClass.Knight);
        EnemyEntity enemy = SpawnOneHitEnemy(EnemyKind.Normal, 1f);
        Dictionary<EconomySession, int> marks = new() { [first] = first.Packets.Count, [second] = second.Packets.Count };

        SubmitMelee(first, enemy);
        SubmitMelee(second, enemy);
        Tick();

        // Fixture: both attack jobs ran in this tick (each player saw the other's swing),
        // yet only one hit landed because the first kill removed the enemy.
        Assert.Contains(Received<S_PlayerAttack>(first, marks[first]), swing => swing.attackerEntityId == second.EntityId);
        Assert.Contains(Received<S_PlayerAttack>(second, marks[second]), swing => swing.attackerEntityId == first.EntityId);
        S_HitResult lethal = Assert.Single(Received<S_HitResult>(first, marks[first]), hit => hit.targetEntityId == enemy.EntityId);
        Assert.False(_world.Map.Enemies.ContainsKey(enemy.EntityId));
        EconomySession killer = lethal.attackerEntityId == first.EntityId ? first : second;
        EconomySession other = ReferenceEquals(killer, first) ? second : first;

        Settle();

        AssertSnapshot(Assert.Single(killer.EconomyPacketsSince(marks[killer])), revision: 1, currency: 10, (MaterialId, 1));
        Assert.Empty(other.EconomyPacketsSince(marks[other]));
        AssertQueriedState(killer, revision: 1, currency: 10, (MaterialId, 1));
        AssertQueriedState(other, revision: 0, currency: 0);
    }

    // ── Required behaviour 4: a deferred impact that lands after the death ──────

    [Fact]
    public void MageProjectile_PositiveControl_DeferredImpactKillsALiveTargetLater()
    {
        EconomySession mage = JoinAndEnter(CharacterClass.Mage);
        EnemyEntity enemy = SpawnOneHitEnemy(EnemyKind.Normal, 1f);
        int mark = mage.Packets.Count;

        SubmitMelee(mage, enemy);
        Tick();
        S_ProjectileLaunch launch = Assert.Single(Received<S_ProjectileLaunch>(mage, mark));
        Assert.True(_world.Map.Enemies.ContainsKey(enemy.EntityId));
        Settle(launch.travelTicks);

        Assert.Contains(Received<S_HitResult>(mage, mark),
            hit => hit.attackerEntityId == mage.EntityId && hit.targetEntityId == enemy.EntityId);
        Assert.False(_world.Map.Enemies.ContainsKey(enemy.EntityId));
    }

    [Fact]
    public void DeferredImpactLandingAfterDeath_AddsNoSecondRewardOrRevision()
    {
        EconomySession mage = JoinAndEnter(CharacterClass.Mage);
        EconomySession knight = JoinAndEnter(CharacterClass.Knight);
        EnemyEntity enemy = SpawnOneHitEnemy(EnemyKind.Normal, 1f);
        int mageMark = mage.Packets.Count;
        int knightMark = knight.Packets.Count;

        // The real Mage attack queues a DeferredImpact; the Knight kills the enemy before it lands.
        SubmitMelee(mage, enemy);
        Tick();
        S_ProjectileLaunch launch = Assert.Single(Received<S_ProjectileLaunch>(knight, knightMark));
        Assert.True(launch.travelTicks > 1, "fixture: the projectile must still be in flight after the kill tick");
        KillWithMelee(knight, enemy);
        Settle(launch.travelTicks + SettleTicks);

        // Fixture: the existing target check skipped the late impact.
        Assert.DoesNotContain(Received<S_HitResult>(knight, knightMark), hit => hit.attackerEntityId == mage.EntityId);
        AssertSnapshot(Assert.Single(knight.EconomyPacketsSince(knightMark)), revision: 1, currency: 10, (MaterialId, 1));
        Assert.Empty(mage.EconomyPacketsSince(mageMark));
        AssertQueriedState(knight, revision: 1, currency: 10, (MaterialId, 1));
        AssertQueriedState(mage, revision: 0, currency: 0);
    }

    // ── Required behaviour 5: pouch use, replay after completion, in-flight duplicate ──

    [Fact]
    public void GolemPouchUse_ThenSameBytesAfterCompletion_IsStaleAndKeepsState()
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);
        byte[] use = Wire.ItemUse(CoinPouchId, expectedRevision: 1);

        int mark = session.Packets.Count;
        session.OnRecvPacket(use.ToArray());
        Settle();
        List<byte[]> firstReplies = session.EconomyPacketsSince(mark);
        Assert.Equal(2, firstReplies.Count);
        AssertItemUseResult(firstReplies[0], UseResult.Success, CoinPouchId, revision: 2);
        AssertSnapshot(firstReplies[1], revision: 2, currency: 60);

        // The first request has completed, so this is a replay rather than an in-flight duplicate.
        mark = session.Packets.Count;
        session.OnRecvPacket(use.ToArray());
        Settle();
        List<byte[]> replayReplies = session.EconomyPacketsSince(mark);
        Assert.Equal(2, replayReplies.Count);
        AssertItemUseResult(replayReplies[0], UseResult.Stale, CoinPouchId, revision: 2);
        AssertSnapshot(replayReplies[1], revision: 2, currency: 60);
    }

    [Fact]
    public void GolemPouchUse_DuplicateWhileFirstIsInFlight_IsDroppedWithoutReply()
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);
        byte[] use = Wire.ItemUse(CoinPouchId, expectedRevision: 1);

        int mark = session.Packets.Count;
        session.OnRecvPacket(use.ToArray());
        session.OnRecvPacket(use.ToArray());
        Settle();

        // One Success and its snapshot; the busy-guard drop leaves no Stale reply behind.
        List<byte[]> replies = session.EconomyPacketsSince(mark);
        Assert.Equal(2, replies.Count);
        AssertItemUseResult(replies[0], UseResult.Success, CoinPouchId, revision: 2);
        AssertSnapshot(replies[1], revision: 2, currency: 60);
    }

    // ── Required behaviour 6: stack cap rejects the whole next reward ───────────

    [Fact]
    public void MaterialStackAtCap_NextNormalReward_IsRejectedWithoutAnyStateChange()
    {
        EconomySession knight = JoinAndEnter(CharacterClass.Knight);
        // 99 one-hit enemies inside one Dash box: a single committed Dash kills all of them through
        // ApplyImmediateEnemyHit → HandleEnemyDeath → the world kill callback, in one tick.
        EnemyEntity[] pack = Enumerable.Range(0, 99)
            .Select(_ => SpawnOneHitEnemy(EnemyKind.Normal, CombatConstants.DashBoxHalfX))
            .ToArray();

        knight.OnRecvPacket(new C_SkillUse
        {
            skillId = (byte)SkillId.Dash,
            attackerClientTick = (int)(_tick + 1),
            facing = 1,
        }.Write());
        Tick();
        Assert.All(pack, enemy => Assert.False(_world.Map.Enemies.ContainsKey(enemy.EntityId)));
        Settle();
        AssertQueriedState(knight, revision: 99, currency: 990, (MaterialId, 99));

        // Let the Dash attack state end so the next melee is accepted by the action gate.
        Settle(Constants.DashTravelTicks);
        PlayerEntity player = _world.Map.GetPlayer(knight.EntityId)!;
        EnemyEntity hundredth = SpawnOneHitEnemy(EnemyKind.Normal, player.Position.X + 1f);
        int mark = knight.Packets.Count;
        KillWithMelee(knight, hundredth);
        Settle();

        Assert.DoesNotContain(knight.EconomyPacketsSince(mark),
            packet => Wire.IdOf(packet) == Wire.InventorySnapshotId && Wire.ReadSnapshot(packet).Revision > 99);
        AssertQueriedState(knight, revision: 99, currency: 990, (MaterialId, 99));
    }

    // ── Additional data/order contract ──────────────────────────────────────────

    [Fact]
    public void BossKill_GrantsPouchMaterialAndFiftyCurrencyInItemIdOrder()
    {
        EconomySession killer = JoinAndEnter(CharacterClass.Knight);
        int mark = killer.Packets.Count;

        KillWithMelee(killer, SpawnOneHitEnemy(EnemyKind.Boss, 1f));
        Settle();

        AssertSnapshot(Assert.Single(killer.EconomyPacketsSince(mark)), revision: 1, currency: 50,
            (MaterialId, 1), (CoinPouchId, 1));
    }

    [Fact]
    public void SnapshotSlots_FollowItemIdOrder_NotAcquisitionOrder()
    {
        EconomySession killer = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(killer);
        Settle(CombatConstants.MeleeCooldownTicks);

        int mark = killer.Packets.Count;
        KillWithMelee(killer, SpawnOneHitEnemy(EnemyKind.Normal, 1f));
        Settle();

        AssertSnapshot(Assert.Single(killer.EconomyPacketsSince(mark)), revision: 2, currency: 20,
            (MaterialId, 1), (CoinPouchId, 1));
    }

    [Fact]
    public void UsingAnUnownedPouch_ReturnsNotOwnedOnly_AndKeepsState()
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        int mark = session.Packets.Count;

        session.OnRecvPacket(Wire.ItemUse(CoinPouchId, expectedRevision: 0));
        Settle();

        AssertItemUseResult(Assert.Single(session.EconomyPacketsSince(mark)), UseResult.NotOwned, CoinPouchId, revision: 0);
        AssertQueriedState(session, revision: 0, currency: 0);
    }

    [Fact]
    public void UsingMaterial_ReturnsNotUsableOnly_AndKeepsState()
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        int mark = session.Packets.Count;
        KillWithMelee(session, SpawnOneHitEnemy(EnemyKind.Normal, 1f));
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision: 1, currency: 10, (MaterialId, 1));

        mark = session.Packets.Count;
        session.OnRecvPacket(Wire.ItemUse(MaterialId, expectedRevision: 1));
        Settle();

        AssertItemUseResult(Assert.Single(session.EconomyPacketsSince(mark)), UseResult.NotUsable, MaterialId, revision: 1);
        AssertQueriedState(session, revision: 1, currency: 10, (MaterialId, 1));
    }

    // ── Lifecycle: map transfer, close, reconnect ───────────────────────────────

    [Fact]
    public void MapTransfer_KeepsTheSameEconomyState()
    {
        EconomySession knight = JoinAndEnter(CharacterClass.Knight);
        int mark = knight.Packets.Count;
        KillWithMelee(knight, SpawnOneHitEnemy(EnemyKind.Normal, 1f));
        Settle();
        AssertSnapshot(Assert.Single(knight.EconomyPacketsSince(mark)), revision: 1, currency: 10, (MaterialId, 1));

        // Town portal 1 leads to HuntingGround (same placement as SessionCleanupTests).
        _world.Map.GetPlayer(knight.EntityId)!.Position = new Vector2(20, 0);
        knight.OnRecvPacket(new C_EnterPortal { portalId = 1 }.Write());
        Tick();
        Tick(); // 새 복사본은 다음 틱부터 입장 job을 실행한다(IM-16).
        Assert.True(_world.TryGetInstance(MapId.HuntingGround, InstanceKey.ForSolo(knight.EntityId), out GameMap? hunting));
        Assert.NotNull(hunting!.GetPlayer(knight.EntityId));

        AssertQueriedState(knight, revision: 1, currency: 10, (MaterialId, 1));
    }

    [Fact]
    public void KillerClosingInsideTheKillTick_ReceivesNoEconomyPacket()
    {
        EconomySession knight = JoinAndEnter(CharacterClass.Knight);
        EnemyEntity enemy = SpawnOneHitEnemy(EnemyKind.Normal, 1f);
        int mark = knight.Packets.Count;
        // Close on the death broadcast, inside the same map tick: the kill callback still fires,
        // and world cleanup removes the player before any later economy step can run.
        knight.OnPacket = packet =>
        {
            if (Wire.IdOf(packet) == (ushort)PacketID.S_EntityDeath) knight.OnDisconnected(Endpoint);
        };

        KillWithMelee(knight, enemy);
        Settle();

        Assert.Equal(-1, knight.EntityId);
        Assert.Empty(knight.EconomyPacketsSince(mark));
    }

    [Fact]
    public void RequestQueuedBeforeClose_IsUnanswered_AndReconnectStartsEmpty()
    {
        EconomySession first = JoinAndEnter(CharacterClass.Knight);
        int mark = first.Packets.Count;
        KillWithMelee(first, SpawnOneHitEnemy(EnemyKind.Normal, 1f));
        Settle();
        AssertSnapshot(Assert.Single(first.EconomyPacketsSince(mark)), revision: 1, currency: 10, (MaterialId, 1));

        mark = first.Packets.Count;
        first.OnRecvPacket(Wire.InventoryRequest());
        first.OnDisconnected(Endpoint);
        Settle();
        Assert.Equal(-1, first.EntityId);
        Assert.Empty(first.EconomyPacketsSince(mark));

        EconomySession reconnected = JoinAndEnter(CharacterClass.Knight);
        AssertQueriedState(reconnected, revision: 0, currency: 0);
    }

    // ── Boundary rejection of malformed economy input ───────────────────────────

    [Theory]
    [InlineData("request-header-only")]
    [InlineData("request-extra-byte")]
    [InlineData("request-reserved-nonzero")]
    [InlineData("use-short")]
    [InlineData("use-extra-byte")]
    [InlineData("use-item-zero")]
    [InlineData("use-item-negative")]
    [InlineData("use-item-undefined")]
    [InlineData("client-sends-snapshot")]
    [InlineData("client-sends-use-result")]
    public void MalformedEconomyInput_IsRejectedWithoutExceptionOrStateChange(string variant)
    {
        EconomySession session = JoinAndEnter(CharacterClass.Knight);
        GainGolemReward(session);

        int mark = session.Packets.Count;
        Exception? thrown = Record.Exception(() => session.OnRecvPacket(MalformedInput(variant)));
        Settle();

        Assert.Null(thrown);
        Assert.DoesNotContain(session.EconomyPacketsSince(mark),
            packet => Wire.IdOf(packet) == Wire.ItemUseResultId && Wire.ReadItemUseResult(packet).Result == UseResult.Success);
        // Assumption shared with the existing gameplay handlers: invalid input is dropped, not a disconnect.
        Assert.False(session.IsClosing, "invalid economy input should be dropped without closing the connection");
        AssertQueriedState(session, revision: 1, currency: 10, (CoinPouchId, 1));
    }

    static byte[] MalformedInput(string variant)
    {
        byte[] validUsePayload = Wire.ItemUse(CoinPouchId, expectedRevision: 1)[4..];
        return variant switch
        {
            "request-header-only" => Wire.Frame(Wire.InventoryRequestId, Array.Empty<byte>()),
            "request-extra-byte" => Wire.Frame(Wire.InventoryRequestId, new byte[] { 0, 0 }),
            "request-reserved-nonzero" => Wire.InventoryRequest(reserved: 1),
            "use-short" => Wire.Frame(Wire.ItemUseId, validUsePayload[..7]),
            "use-extra-byte" => Wire.Frame(Wire.ItemUseId, validUsePayload.Append((byte)0).ToArray()),
            "use-item-zero" => Wire.ItemUse(0, expectedRevision: 1),
            "use-item-negative" => Wire.ItemUse(-1, expectedRevision: 1),
            "use-item-undefined" => Wire.ItemUse(3, expectedRevision: 1),
            "client-sends-snapshot" => Wire.ForgedSnapshot(revision: 1000, currency: 999_999),
            "client-sends-use-result" => Wire.Frame(Wire.ItemUseResultId, new byte[] { UseResult.Success, 2, 0, 0, 0, 2, 0, 0, 0 }),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
    }

    // ── Fixture helpers ─────────────────────────────────────────────────────────

    void Tick() => LifecycleTestWorld.Tick(_world, ++_tick);

    void Settle(int ticks = SettleTicks)
    {
        for (int i = 0; i < ticks; i++) Tick();
    }

    // Real handshake and class selection bytes, then one world tick to enter Town at the origin.
    EconomySession JoinAndEnter(CharacterClass characterClass)
    {
        EconomySession session = new(_world);
        session.OnConnected(Endpoint);
        session.OnRecvPacket(new C_Handshake { clientVersion = ProtocolVersion.Current }.Write());
        session.OnRecvPacket(new C_CharacterSelect { characterClass = (byte)characterClass }.Write());
        Tick();
        Assert.True(session.EntityId >= 0, "fixture: the real handshake and class selection must enter the world");
        return session;
    }

    // maxHp 1 keeps every kill to one real hit; the economy path itself is untouched.
    EnemyEntity SpawnOneHitEnemy(EnemyKind kind, float x) => _world.Map.SpawnEnemy(kind, x, 0f, maxHp: 1);

    // The client tick equals the tick that will run the job, which the action gate accepts.
    void SubmitMelee(EconomySession attacker, EnemyEntity target)
        => attacker.OnRecvPacket(new C_Attack { targetEntityId = target.EntityId, attackerClientTick = (int)(_tick + 1) }.Write());

    void KillWithMelee(EconomySession attacker, EnemyEntity target)
    {
        SubmitMelee(attacker, target);
        Tick();
        Assert.False(_world.Map.Enemies.ContainsKey(target.EntityId), "fixture: the real melee path must kill the target");
    }

    // Golem reward (goal P-C): coin pouch 1 + currency 10, observed through the real kill push.
    void GainGolemReward(EconomySession session)
    {
        int mark = session.Packets.Count;
        KillWithMelee(session, SpawnOneHitEnemy(EnemyKind.Golem, 1f));
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision: 1, currency: 10, (CoinPouchId, 1));
    }

    void AssertQueriedState(EconomySession session, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        int mark = session.Packets.Count;
        session.OnRecvPacket(Wire.InventoryRequest());
        Settle();
        AssertSnapshot(Assert.Single(session.EconomyPacketsSince(mark)), revision, currency, occupied);
    }

    // Occupied slots are listed in ItemId order; the remaining fixed slots must be 0/0.
    static void AssertSnapshot(byte[] packet, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        Assert.Equal(Wire.InventorySnapshotId, Wire.IdOf(packet));
        Assert.Equal(Wire.InventorySnapshotLength, packet.Length);
        Assert.Equal(Wire.InventorySnapshotLength, Wire.SizeOf(packet));
        (uint actualRevision, int actualCurrency, (int ItemId, int Count)[] actualSlots) = Wire.ReadSnapshot(packet);
        Assert.Equal(revision, actualRevision);
        Assert.Equal(currency, actualCurrency);
        (int ItemId, int Count)[] expectedSlots = occupied
            .Concat(Enumerable.Repeat<(int ItemId, int Count)>((0, 0), Wire.SlotCount - occupied.Length))
            .ToArray();
        Assert.Equal(expectedSlots, actualSlots);
    }

    static void AssertItemUseResult(byte[] packet, byte result, int itemId, uint revision)
    {
        Assert.Equal(Wire.ItemUseResultId, Wire.IdOf(packet));
        Assert.Equal(Wire.ItemUseResultLength, packet.Length);
        Assert.Equal(Wire.ItemUseResultLength, Wire.SizeOf(packet));
        (byte actualResult, int actualItemId, uint actualRevision) = Wire.ReadItemUseResult(packet);
        Assert.Equal(result, actualResult);
        Assert.Equal(itemId, actualItemId);
        Assert.Equal(revision, actualRevision);
    }

    // Existing packets are read with their existing generated readers.
    static List<T> Received<T>(EconomySession session, int mark) where T : IPacket, new()
    {
        ushort id = new T().Protocol;
        List<T> packets = new();
        foreach (byte[] bytes in session.Packets.Skip(mark).Where(bytes => Wire.IdOf(bytes) == id))
        {
            T packet = new();
            packet.Read(new ArraySegment<byte>(bytes));
            packets.Add(packet);
        }
        return packets;
    }

    sealed class EconomySession : GameSession
    {
        internal readonly List<byte[]> Packets = new();
        internal Action<byte[]>? OnPacket;

        internal EconomySession(GameWorld world) : base(world) { }

        public override void Send(ArraySegment<byte> packet)
        {
            byte[] bytes = packet.ToArray();
            Packets.Add(bytes);
            OnPacket?.Invoke(bytes);
        }

        public override void Disconnect() => OnDisconnected(Endpoint);

        internal List<byte[]> EconomyPacketsSince(int mark)
            => Packets.Skip(mark)
                .Where(bytes => Wire.IdOf(bytes) is Wire.InventorySnapshotId or Wire.ItemUseResultId)
                .ToList();
    }

    // Result codes of the fixed wire table.
    static class UseResult
    {
        internal const byte Success = 0;
        internal const byte Stale = 1;
        internal const byte NotOwned = 2;
        internal const byte NotUsable = 3;
    }

    // Independent oracle for the fixed wire table: LittleEndian, 4-byte header (ushort size incl. header,
    // ushort id), IDs 35..38 appended after the existing 1..34.
    static class Wire
    {
        internal const ushort InventoryRequestId = 35;
        internal const ushort InventorySnapshotId = 36;
        internal const ushort ItemUseId = 37;
        internal const ushort ItemUseResultId = 38;
        internal const int InventorySnapshotLength = 76;
        internal const int ItemUseResultLength = 13;
        internal const int SlotCount = 8;

        internal static byte[] InventoryRequest(byte reserved = 0) => Frame(InventoryRequestId, new[] { reserved });

        internal static byte[] ItemUse(int itemId, uint expectedRevision)
        {
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), itemId);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), expectedRevision);
            return Frame(ItemUseId, payload);
        }

        internal static byte[] ForgedSnapshot(uint revision, int currency)
        {
            byte[] payload = new byte[InventorySnapshotLength - 4];
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), revision);
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), currency);
            return Frame(InventorySnapshotId, payload);
        }

        // The size field always matches the delivered length, as the framing layer guarantees on receive.
        internal static byte[] Frame(ushort id, byte[] payload)
        {
            byte[] packet = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), (ushort)packet.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), id);
            payload.CopyTo(packet, 4);
            return packet;
        }

        internal static ushort SizeOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(0, 2));

        internal static ushort IdOf(byte[] packet) => BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2));

        // Fields after the header: uint revision, int currency, then 8 × (int itemId, int count).
        internal static (uint Revision, int Currency, (int ItemId, int Count)[] Slots) ReadSnapshot(byte[] packet)
        {
            uint revision = BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4));
            int currency = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4));
            (int ItemId, int Count)[] slots = new (int ItemId, int Count)[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                int offset = 12 + (i * 8);
                slots[i] = (BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset + 4, 4)));
            }
            return (revision, currency, slots);
        }

        // Fields after the header: byte result, int itemId, uint revision.
        internal static (byte Result, int ItemId, uint Revision) ReadItemUseResult(byte[] packet)
            => (packet[4],
                BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(5, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(9, 4)));
    }
}
