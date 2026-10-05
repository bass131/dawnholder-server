using System.Buffers.Binary;
using System.Collections;
using System.Net;
using System.Reflection;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Items;

// Fixture-only boundaries of InventoryRegistry's connection registration. The normal receive path
// does not reach these states within the current ID range: GameWorld.NextEntityId hands out IDs
// with Interlocked.Increment and a GameSession enters the world once. Goal INV-02 does not assume
// the int entityId never wraps, so the registry must compare the session reference together with
// the ID (01_Phases/goals/2026-10-05-items-inventory-currency/goal.md, INV-02/05/15). Each case
// builds a standalone registry with stub delegates and sets the private GameSession._entityId by
// reflection to check that rule directly: another owner is rejected, and a late kill job never
// rewards a later lifetime holding the same ID.
// Expected values are PR1 acceptance literals (normal kill: material 1 + currency 10).
[Collection("GameWorldRegistryTests")]
public sealed class InventoryRegistrationOwnershipTests : IDisposable
{
    const int MaterialId = 1;
    const int EntityId = 7;
    const int EnemyId = 100;

    static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 0);

    // GameSession binds to a world at construction; this world is never ticked.
    readonly GameWorld _world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
    readonly List<(int EntityId, byte[] Payload)> _pushes = new();

    public void Dispose() => _world.Stop();

    // Positive control: the stub push path observes a grant to the registered killer.
    [Fact]
    public void RegisteredKiller_IsRewardedOnceThroughThePushPath()
    {
        InventoryRegistry registry = NewRegistry(confirmsOwner: true);
        FixtureSession owner = SessionWithEntityId(EntityId);
        registry.Register(owner, EntityId);

        registry.EnqueueKill(EntityId, EnemyKind.Normal, EnemyId);
        registry.Tick();

        (int pushedTo, byte[] payload) = Assert.Single(_pushes);
        Assert.Equal(EntityId, pushedTo);
        AssertSnapshot(payload, revision: 1, currency: 10, (MaterialId, 1));
    }

    // The kill job captured the first lifetime; that lifetime closes and a later connection is
    // registered under the same ID before the job drains.
    [Fact]
    public void LateKillJob_DoesNotRewardALaterConnectionGivenTheSameEntityId()
    {
        InventoryRegistry registry = NewRegistry(confirmsOwner: true);
        FixtureSession earlier = SessionWithEntityId(EntityId);
        FixtureSession later = SessionWithEntityId(EntityId);
        registry.Register(earlier, EntityId);

        registry.EnqueueKill(EntityId, EnemyKind.Normal, EnemyId);
        registry.Forget(earlier);
        registry.Register(later, EntityId);
        registry.Tick();
        int pushesAfterTheLateJob = _pushes.Count;
        registry.EnqueueInventoryRequest(later, EntityId);
        registry.Tick();

        Assert.Equal(0, pushesAfterTheLateJob);
        AssertSnapshot(Assert.Single(later.Packets), revision: 0, currency: 0);
    }

    [Fact]
    public void RegisteringAnIdHeldByAnotherLiveConnection_IsRejected_AndTheOwnerKeepsIt()
    {
        InventoryRegistry registry = NewRegistry(confirmsOwner: true);
        FixtureSession owner = SessionWithEntityId(EntityId);
        FixtureSession intruder = SessionWithEntityId(EntityId);
        registry.Register(owner, EntityId);

        Exception? rejection = Record.Exception(() => registry.Register(intruder, EntityId));
        registry.EnqueueKill(EntityId, EnemyKind.Normal, EnemyId);
        registry.EnqueueInventoryRequest(intruder, EntityId);
        registry.Tick();

        Assert.IsType<InvalidOperationException>(rejection);
        Assert.Equal(EntityId, Assert.Single(_pushes).EntityId);
        Assert.Empty(intruder.Packets);
    }

    [Fact]
    public void RegisteringOneConnectionUnderASecondId_IsRejected()
    {
        const int secondId = EntityId + 1;
        InventoryRegistry registry = NewRegistry(confirmsOwner: true);
        FixtureSession session = SessionWithEntityId(EntityId);
        registry.Register(session, EntityId);
        SetEntityId(session, secondId);

        Exception? rejection = Record.Exception(() => registry.Register(session, secondId));
        registry.EnqueueKill(secondId, EnemyKind.Normal, EnemyId);
        registry.Tick();

        Assert.IsType<InvalidOperationException>(rejection);
        Assert.Equal(1, RegistrationCount(registry));
        Assert.Empty(_pushes);
    }

    // Forget is keyed by the closing connection, so a connection that never owned the ID cannot
    // end the owner's registration.
    [Fact]
    public void ForgettingAConnectionThatDoesNotOwnTheId_KeepsTheOwnerRegistered()
    {
        InventoryRegistry registry = NewRegistry(confirmsOwner: true);
        FixtureSession owner = SessionWithEntityId(EntityId);
        FixtureSession stranger = SessionWithEntityId(EntityId);
        registry.Register(owner, EntityId);

        registry.Forget(stranger);
        registry.EnqueueKill(EntityId, EnemyKind.Normal, EnemyId);
        registry.Tick();

        Assert.Equal(1, RegistrationCount(registry));
        Assert.Equal(EntityId, Assert.Single(_pushes).EntityId);
    }

    // Register accepts only a live connection whose own entity ID matches and whose map owner was
    // confirmed; otherwise nothing is registered and a later kill for that ID queues no reward.
    [Theory]
    [InlineData("closing")]
    [InlineData("owner not confirmed")]
    [InlineData("other entity id")]
    [InlineData("non-positive id")]
    public void Register_RefusesAConnectionThatIsNotTheConfirmedLiveOwner(string refusal)
    {
        InventoryRegistry registry = NewRegistry(confirmsOwner: refusal != "owner not confirmed");
        int registeredId = refusal == "non-positive id" ? 0 : EntityId;
        FixtureSession session = SessionWithEntityId(refusal == "other entity id" ? EntityId + 1 : registeredId);
        if (refusal == "closing") session.OnDisconnected(Endpoint);

        registry.Register(session, registeredId);
        int registrations = RegistrationCount(registry);
        if (registeredId > 0) registry.EnqueueKill(registeredId, EnemyKind.Normal, EnemyId);
        registry.Tick();

        Assert.Equal(0, registrations);
        Assert.Empty(_pushes);
    }

    // ── Fixture helpers ─────────────────────────────────────────────────────────

    InventoryRegistry NewRegistry(bool confirmsOwner)
        => new(
            ownsJoiningPlayer: (_, _) => confirmsOwner,
            sendToEntity: (entityId, payload) => _pushes.Add((entityId, payload.ToArray())));

    FixtureSession SessionWithEntityId(int entityId)
    {
        FixtureSession session = new(_world);
        SetEntityId(session, entityId);
        return session;
    }

    // State fixture: the product sets this field only in the admission job and close cleanup.
    static void SetEntityId(GameSession session, int entityId)
    {
        FieldInfo? field = typeof(GameSession).GetField("_entityId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field != null, "fixture: GameSession._entityId was not found");
        field!.SetValue(session, entityId);
        Assert.Equal(entityId, session.EntityId);
    }

    static int RegistrationCount(InventoryRegistry registry)
    {
        FieldInfo? field = typeof(InventoryRegistry).GetField("_registeredSessions", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(field != null, "fixture: the registry field _registeredSessions was not found");
        return ((ICollection)field!.GetValue(registry)!).Count;
    }

    // Snapshot oracle from the PR1 acceptance wire table: header 4 bytes, uint revision, int currency,
    // then eight (int itemId, int count) slots in ascending ItemId order, empty slots 0/0.
    static void AssertSnapshot(byte[] packet, uint revision, int currency, params (int ItemId, int Count)[] occupied)
    {
        const int slotCount = 8;
        (int ItemId, int Count)[] expectedSlots = occupied
            .Concat(Enumerable.Repeat((0, 0), slotCount - occupied.Length))
            .ToArray();
        (int ItemId, int Count)[] actualSlots = Enumerable.Range(0, slotCount)
            .Select(i => (BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(12 + (i * 8), 4)),
                BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(16 + (i * 8), 4))))
            .ToArray();

        Assert.Equal(76, packet.Length);
        Assert.Equal(36, BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(2, 2)));
        Assert.Equal(revision, BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(4, 4)));
        Assert.Equal(currency, BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(8, 4)));
        Assert.Equal(expectedSlots, actualSlots);
    }

    sealed class FixtureSession : GameSession
    {
        internal readonly List<byte[]> Packets = new();

        internal FixtureSession(GameWorld world) : base(world) { }

        public override void Send(ArraySegment<byte> packet) => Packets.Add(packet.ToArray());

        public override void Disconnect() => OnDisconnected(Endpoint);
    }
}
