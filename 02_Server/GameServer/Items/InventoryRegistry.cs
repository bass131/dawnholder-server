using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// World-tick owner of connection registrations and lazy economy state. Register runs after an
// actual map admission; Forget runs during World close cleanup. Map migration changes neither.
// Requests enqueue off-tick; kill callbacks capture the registered owner on-tick. GameWorld
// drains jobs after Quest, and each job rechecks that connection's ownership and closing state.
internal sealed class InventoryRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();
    readonly Dictionary<int, GameSession> _registeredSessions = new();
    readonly Dictionary<GameSession, Entry> _states = new(ReferenceEqualityComparer.Instance);
    readonly Func<GameSession, int, bool> _ownsJoiningPlayer;
    readonly Action<int, ArraySegment<byte>> _sendToEntity;

    internal InventoryRegistry(
        Func<GameSession, int, bool> ownsJoiningPlayer,
        Action<int, ArraySegment<byte>> sendToEntity)
    {
        _ownsJoiningPlayer = ownsJoiningPlayer ?? throw new ArgumentNullException(nameof(ownsJoiningPlayer));
        _sendToEntity = sendToEntity ?? throw new ArgumentNullException(nameof(sendToEntity));
    }

    internal void Register(GameSession session, int entityId)
    {
        // Map presence proves initial admission only. The economic lifetime remains registered
        // across the gap between source removal and destination arrival during a portal move.
        if (entityId <= 0 || session.EntityId != entityId || session.IsClosing ||
            !_ownsJoiningPlayer(session, entityId))
        {
            return;
        }

        if (_registeredSessions.TryGetValue(entityId, out GameSession? owner))
        {
            if (!ReferenceEquals(owner, session))
                throw new InvalidOperationException("Inventory entity already belongs to another connection.");
            return;
        }
        if (_registeredSessions.Values.Any(registered => ReferenceEquals(registered, session)))
            throw new InvalidOperationException("Inventory connection owner changed.");
        _registeredSessions.Add(entityId, session);
    }

    internal void EnqueueInventoryRequest(GameSession session, int entityId)
    {
        EnqueueRequest(session, entityId, entry =>
        {
            ArraySegment<byte> snapshot = InventoryPackets.Snapshot(entry.State);
            if (IsActive(session, entityId)) session.Send(snapshot);
        });
    }

    internal void EnqueueItemUse(GameSession session, int entityId, ItemId itemId, uint expectedRevision)
    {
        EnqueueRequest(session, entityId, entry =>
        {
            InventoryResult result = InventoryTransitions.Use(entry.State, itemId, expectedRevision, out InventoryState next);
            ArraySegment<byte> reply = InventoryPackets.UseResult(result, itemId, next.Revision);
            ArraySegment<byte> snapshot = default;
            bool sendSnapshot = result == InventoryResult.Success || result == InventoryResult.Stale;
            if (sendSnapshot) snapshot = InventoryPackets.Snapshot(next);
            if (!IsActive(session, entityId)) return;

            // All state/packet allocation precedes this single reference commit. Send failures
            // leave the committed revision in place, so a replay cannot consume the item twice.
            if (result == InventoryResult.Success) entry.State = next;
            session.Send(reply);
            if (sendSnapshot && IsActive(session, entityId)) session.Send(snapshot);
        });
    }

    internal void EnqueueKill(int killerId, EnemyKind kind, int enemyId)
    {
        // The lethal callback runs on the World tick. Capture its current connection owner as
        // well as values so a late job cannot reward another lifetime that acquires the same ID.
        // No dead EnemyEntity or receipt/TTL/GC state is retained alongside the combat owner.
        if (killerId <= 0 || !_registeredSessions.TryGetValue(killerId, out GameSession? session)) return;
        _pendingJobs.Enqueue(() => ApplyKill(session, killerId, kind, enemyId));
    }

    internal void Forget(GameSession session)
    {
        if (_registeredSessions.TryGetValue(session.EntityId, out GameSession? owner) &&
            ReferenceEquals(owner, session))
        {
            _registeredSessions.Remove(session.EntityId);
        }
        _states.Remove(session);
        session.CompleteInventoryRequest();
    }

    internal void Tick()
    {
        while (_pendingJobs.TryDequeue(out Action? job))
        {
            try
            {
                job();
            }
            catch (Exception ex)
            {
                // Internal/data/transport failures are diagnostic failures, never Success or a
                // replacement empty state. A popped job is not retried after a send failure.
                Console.WriteLine($"[InventoryRegistry] job failed: {ex}");
            }
        }
    }

    void EnqueueRequest(GameSession session, int entityId, Action<Entry> apply)
    {
        _pendingJobs.Enqueue(() =>
        {
            try
            {
                if (!IsActive(session, entityId)) return;
                apply(GetOrCreate(session, entityId));
            }
            finally
            {
                session.CompleteInventoryRequest();
            }
        });
    }

    void ApplyKill(GameSession session, int killerId, EnemyKind kind, int enemyId)
    {
        (int recipientId, InventoryReward reward) = KillRewardPolicy.Resolve(killerId, kind, enemyId);
        if (!IsActive(session, recipientId)) return;

        Entry entry = GetOrCreate(session, recipientId);
        InventoryResult result = InventoryTransitions.Grant(entry.State, reward, out InventoryState next);
        if (result != InventoryResult.Success) return;
        ArraySegment<byte> snapshot = InventoryPackets.Snapshot(next);
        if (!IsActive(session, recipientId)) return;

        entry.State = next;
        _sendToEntity(recipientId, snapshot);
    }

    bool IsActive(GameSession session, int entityId)
        => session.EntityId == entityId && !session.IsClosing &&
            _registeredSessions.TryGetValue(entityId, out GameSession? owner) && ReferenceEquals(owner, session);

    Entry GetOrCreate(GameSession session, int entityId)
    {
        if (_states.TryGetValue(session, out Entry? entry))
        {
            if (entry.EntityId != entityId) throw new InvalidOperationException("Inventory connection owner changed.");
            return entry;
        }

        // Holder/dictionary allocation happens before transitions. No fallible collection insert
        // is hidden in the economic commit; its only mutation is Entry.State = preparedNext.
        entry = new Entry(entityId);
        _states.Add(session, entry);
        return entry;
    }

    sealed class Entry
    {
        internal Entry(int entityId)
        {
            EntityId = entityId;
            State = InventoryState.Empty;
        }

        internal int EntityId { get; }

        internal InventoryState State { get; set; }
    }
}
