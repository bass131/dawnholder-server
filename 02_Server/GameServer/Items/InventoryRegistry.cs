using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// World-owned connection lifetimes, independent of PlayerEntity recreation during map migration.
// Only enqueue methods run off-tick. GameWorld drains this actor after Quest and forgets closed
// connections before draining jobs; each job also rechecks the current session/entity owner.
internal sealed class InventoryRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();
    readonly Dictionary<GameSession, Entry> _states = new(ReferenceEqualityComparer.Instance);
    readonly Func<GameSession, int, bool> _isActiveSession;
    readonly Func<int, GameSession?> _findSession;
    readonly Action<int, ArraySegment<byte>> _sendToEntity;

    internal InventoryRegistry(
        Func<GameSession, int, bool> isActiveSession,
        Func<int, GameSession?> findSession,
        Action<int, ArraySegment<byte>> sendToEntity)
    {
        _isActiveSession = isActiveSession ?? throw new ArgumentNullException(nameof(isActiveSession));
        _findSession = findSession ?? throw new ArgumentNullException(nameof(findSession));
        _sendToEntity = sendToEntity ?? throw new ArgumentNullException(nameof(sendToEntity));
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
        // The existing lethal path fires once. Capture only values, without retaining a dead
        // EnemyEntity or installing receipt/TTL/GC state alongside the combat owner.
        if (killerId <= 0) return;
        _pendingJobs.Enqueue(() => ApplyKill(killerId, kind, enemyId));
    }

    internal void Forget(GameSession session)
    {
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

    void ApplyKill(int killerId, EnemyKind kind, int enemyId)
    {
        (int recipientId, InventoryReward reward) = KillRewardPolicy.Resolve(killerId, kind, enemyId);
        GameSession? session = _findSession(recipientId);
        if (session == null || !IsActive(session, recipientId)) return;

        Entry entry = GetOrCreate(session, recipientId);
        InventoryResult result = InventoryTransitions.Grant(entry.State, reward, out InventoryState next);
        if (result != InventoryResult.Success) return;
        ArraySegment<byte> snapshot = InventoryPackets.Snapshot(next);
        if (!IsActive(session, recipientId)) return;

        entry.State = next;
        _sendToEntity(recipientId, snapshot);
    }

    bool IsActive(GameSession session, int entityId)
        => session.EntityId == entityId && !session.IsClosing && _isActiveSession(session, entityId);

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
