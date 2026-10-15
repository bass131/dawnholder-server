using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Guild;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Items;

// World-tick owner of connection registrations and lazy economy state. Register runs after an
// actual map admission; Forget runs during World close cleanup. Map migration changes neither.
// Requests enqueue off-tick; kill/clear callbacks capture their recipients on-tick. GameWorld
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

    // Guild consumes a value view, never the mutable Entry or its State setter. Read-only guild
    // sync preserves lazy Inventory allocation; only a personal economy operation needs a holder.
    internal bool TryReadGuildView(GameSession session, int entityId, out GuildView view)
    {
        view = default;
        if (!IsActive(session, entityId)) return false;
        InventoryState state = _states.TryGetValue(session, out Entry? entry) ? entry.State : InventoryState.Empty;
        view = GuildView.From(state);
        return true;
    }

    internal int ReadAttackBonus(GameSession session, int entityId)
    {
        if (!IsActive(session, entityId) || !_states.TryGetValue(session, out Entry? entry)) return 0;
        return GuildRules.AttackBonus(entry.State.WeaponLevel);
    }

    internal ArraySegment<byte> PrepareCurrentSnapshot(GameSession session, int entityId)
    {
        if (!IsActive(session, entityId)) throw new InvalidOperationException("Inventory owner is no longer active.");
        return InventoryPackets.Snapshot(GetOrCreate(session, entityId).State);
    }

    internal GuildResult PrepareEnhancement(GameSession session, int entityId, bool facilityUnlocked,
        uint expectedRevision, out PreparedEnhancement? prepared)
    {
        prepared = null;
        if (!IsActive(session, entityId)) return GuildResult.NotMember;
        Entry entry = GetOrCreate(session, entityId);
        GuildResult result = InventoryTransitions.Enhance(entry.State, facilityUnlocked, expectedRevision,
            out InventoryState next);
        if (result == GuildResult.Success) prepared = new PreparedEnhancement(this, session, next);
        return result;
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

    internal void EnqueueDungeonClear(IReadOnlyList<(int EntityId, GameSession Session)> beneficiaries)
    {
        foreach (var recipient in beneficiaries)
            _pendingJobs.Enqueue(() => ApplyDungeonClear(recipient.Session, recipient.EntityId));
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

    void ApplyDungeonClear(GameSession session, int entityId)
    {
        if (!IsActive(session, entityId)) return;

        Entry entry = GetOrCreate(session, entityId);
        InventoryReward reward = new(DungeonClearRewards.Currency, DungeonClearRewards.Items.ToArray());
        InventoryResult result = InventoryTransitions.Grant(entry.State, reward, out InventoryState next);
        if (result == InventoryResult.RevisionExhausted)
        {
            // This internal outcome has no v18 wire value. Keep the map's pending confirmation
            // so the player can still leave; never turn an uncommitted grant into Success.
            Console.WriteLine($"[InventoryRegistry] dungeon clear revision exhausted: entity={entityId}");
            return;
        }

        ArraySegment<byte> reply = Dungeon.DungeonClearPackets.Result(result);
        ArraySegment<byte> snapshot = default;
        if (result == InventoryResult.Success) snapshot = InventoryPackets.Snapshot(next);
        if (!IsActive(session, entityId)) return;

        // Prepare both packets before the single commit. Direct sends preserve the result even
        // during migration, when SendToEntity would skip a player without a current map.
        if (result == InventoryResult.Success) entry.State = next;
        session.Send(reply);
        if (result == InventoryResult.Success && IsActive(session, entityId)) session.Send(snapshot);
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

    internal readonly record struct GuildView(uint Revision, int Currency, byte WeaponLevel, int MaterialCount)
    {
        internal static GuildView From(InventoryState state)
            => new(state.Revision, state.Currency, state.WeaponLevel, state.GetCount(GuildRules.WeaponMaterial));
    }

    // A same-tick, single-use prepared write. Guild must recheck the connection immediately
    // before Commit; no callback, lookup, allocation, or I/O occurs inside Commit itself.
    internal sealed class PreparedEnhancement
    {
        readonly Entry _entry;
        readonly InventoryState _next;

        internal PreparedEnhancement(InventoryRegistry owner, GameSession session, InventoryState next)
        {
            _entry = owner._states[session];
            _next = next;
            View = GuildView.From(next);
            Snapshot = InventoryPackets.Snapshot(next);
        }

        internal GuildView View { get; }

        internal ArraySegment<byte> Snapshot { get; }

        internal void Commit() => _entry.State = _next;
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
