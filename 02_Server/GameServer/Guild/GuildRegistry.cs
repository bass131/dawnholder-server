using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Items;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Guild;

// World-tick owner. A retained key means this account already received its one admission seed;
// a null value means offline. Reconnect replaces only that connection, never the member key.
// The shared economy is an immutable value; presence and per-connection sequence are separate.
internal sealed class GuildRegistry
{
    readonly ConcurrentQueue<Action> _pendingJobs = new();
    readonly InventoryRegistry _inventory;
    readonly Func<GameSession, int, bool> _ownsJoiningPlayer;
    Dictionary<Guid, Connection?> _members = new();
    GuildState _state = new(0, 0, 1);
    int _onlineCount;

    internal GuildRegistry(InventoryRegistry inventory, Func<GameSession, int, bool> ownsJoiningPlayer)
    {
        _inventory = inventory;
        _ownsJoiningPlayer = ownsJoiningPlayer;
    }

    internal bool Register(GameSession session, int entityId, out Action? publishAdmission)
    {
        publishAdmission = null;
        if (session.Account is not { } account || !CanAdmit(session, entityId, account.AccountId)) return false;
        Connection connection;
        PreparedSnapshot snapshot;
        GuildChangeKind kind;
        try
        {
            bool memberExists = _members.TryGetValue(account.AccountId, out Connection? previous);
            if (previous != null)
            {
                if (ReferenceEquals(previous.Session, session) && previous.EntityId == entityId) return true;
                throw new InvalidOperationException("Guild account already has an online owner.");
            }

            // Copy before publishing: dictionary growth, seed exhaustion, snapshot allocation,
            // or a closing owner must not leave a member without its seed (or vice versa).
            GuildState next = memberExists ? _state : _state.WithJoinSeed();
            connection = new Connection(session, entityId, account.AccountId);
            Dictionary<Guid, Connection?> members = new(_members) { [account.AccountId] = connection };
            int online = checked(_onlineCount + 1);
            kind = memberExists ? GuildChangeKind.Presence : GuildChangeKind.Membership;
            if (!_inventory.TryReadGuildView(session, entityId, out InventoryRegistry.GuildView inventory))
                throw new InvalidOperationException("Guild admission has no inventory owner.");
            snapshot = PrepareSnapshot(connection, next, inventory, members.Count, online, kind,
                ReadLocation(connection));
            // Prepare the notification before commit; the session publishes it only after its
            // initial map packets. A failed map send must still notify the other members.
            Action publish = () =>
            {
                SendSnapshotIsolated(connection, snapshot);
                Broadcast(kind, connection);
            };
            if (!CanAdmit(session, entityId, account.AccountId)) return false;

            _members = members;
            _onlineCount = online;
            _state = next;
            connection.Sequence = snapshot.Sequence;
            publishAdmission = publish;
        }
        catch (Exception ex)
        {
            Diagnose("admission preparation", ex);
            session.Disconnect();
            return false;
        }

        return true;
    }

    internal void EnqueueQuery(GameSession session, int entityId)
        => EnqueueRequest(session, entityId, Query);

    internal void EnqueueStrongholdUpgrade(GameSession session, int entityId, uint expectedRevision)
        => EnqueueRequest(session, entityId,
            connection => Act(connection, GuildAction.StrongholdUpgrade, expectedRevision));

    internal void EnqueueWeaponEnhance(GameSession session, int entityId, uint expectedRevision)
        => EnqueueRequest(session, entityId,
            connection => Act(connection, GuildAction.WeaponEnhance, expectedRevision));

    // Clear eligibility comes from Content's captured recipient and these connection owners.
    // Town/alive checks belong only to purchase availability, never to receiving a clear reward.
    internal bool OwnsDungeonClear(GameSession session, int entityId)
        => FindDungeonClearOwner(session, entityId) != null;

    internal PreparedDungeonClearCredit PrepareDungeonClearCredit(GameSession session, int entityId,
        InventoryRegistry.GuildView rewardedInventory)
        => new(this, session, entityId, rewardedInventory);

    internal void Forget(GameSession session)
    {
        session.CompleteGuildRequest();
        if (session.Account is not { } account || !_members.TryGetValue(account.AccountId, out Connection? owner) ||
            owner == null || !ReferenceEquals(owner.Session, session))
        {
            return;
        }
        // Existing key assignment allocates no membership storage. Retain the account seed record.
        _members[account.AccountId] = null;
        _onlineCount--;
        Broadcast(GuildChangeKind.Presence);
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
                // Internal/transport failures are never converted to a business refusal. A
                // popped job is not retried, whether preparation or post-commit send failed.
                Diagnose("request", ex);
            }
        }
    }

    void EnqueueRequest(GameSession session, int entityId, Action<Connection> apply)
    {
        Guid? accountId = session.Account?.AccountId;
        _pendingJobs.Enqueue(() =>
        {
            try
            {
                if (!accountId.HasValue || !_members.TryGetValue(accountId.Value, out Connection? connection) ||
                    connection == null || !ReferenceEquals(connection.Session, session) ||
                    connection.EntityId != entityId || !IsActive(connection)) return;
                apply(connection);
            }
            finally
            {
                session.CompleteGuildRequest();
            }
        });
    }

    void Query(Connection connection)
    {
        if (!_inventory.TryReadGuildView(connection.Session, connection.EntityId, out InventoryRegistry.GuildView inventory))
            return;
        PreparedSnapshot snapshot = PrepareSnapshot(connection, _state, inventory, _members.Count, _onlineCount,
            GuildChangeKind.Query, ReadLocation(connection));
        if (!IsActive(connection)) return;
        connection.Sequence = snapshot.Sequence;
        connection.Session.Send(snapshot.Payload);
    }

    void Act(Connection connection, GuildAction action, uint expectedRevision)
    {
        if (!_inventory.TryReadGuildView(connection.Session, connection.EntityId, out InventoryRegistry.GuildView inventory))
            return;
        GuildResult result = ReadLocation(connection);
        GuildState next = _state;
        InventoryRegistry.PreparedEnhancement? enhancement = null;
        if (result == GuildResult.Success)
        {
            if (action == GuildAction.StrongholdUpgrade)
            {
                result = GuildRules.StrongholdAvailability(_state, expectedRevision);
                if (result == GuildResult.Success) next = _state.WithStrongholdUpgrade();
            }
            else
            {
                result = _inventory.PrepareEnhancement(connection.Session, connection.EntityId,
                    _state.BlacksmithUnlocked, expectedRevision, out enhancement);
                if (enhancement != null) inventory = enhancement.View;
            }
        }

        GuildChangeKind kind = result == GuildResult.Success
            ? (action == GuildAction.StrongholdUpgrade ? GuildChangeKind.StrongholdUpgrade : GuildChangeKind.WeaponEnhance)
            : GuildChangeKind.Query;
        PreparedSnapshot snapshot = PrepareSnapshot(connection, next, inventory, _members.Count, _onlineCount,
            kind, ReadLocation(connection));
        ArraySegment<byte> reply = GuildPackets.ActionResult(action, result, expectedRevision,
            next.Revision, inventory.Revision, snapshot.Sequence);
        // Only a weapon request reconciles personal inventory. A stale stronghold revision
        // concerns communal state and must keep the result -> guild snapshot response shape.
        bool sendInventory = action == GuildAction.WeaponEnhance &&
            (result == GuildResult.Stale || enhancement != null);
        ArraySegment<byte> inventorySnapshot = enhancement?.Snapshot ?? (sendInventory
            ? _inventory.PrepareCurrentSnapshot(connection.Session, connection.EntityId) : default);

        // Final connection check is the linearization boundary with socket close. The following
        // commit only publishes prepared references/values; all packets and holders exist now.
        if (!IsActive(connection)) return;
        _state = next;
        enhancement?.Commit();
        connection.Sequence = snapshot.Sequence;

        try
        {
            if (IsActive(connection)) connection.Session.Send(reply);
            if (sendInventory && IsActive(connection)) connection.Session.Send(inventorySnapshot);
            if (IsActive(connection)) connection.Session.Send(snapshot.Payload);
        }
        finally
        {
            // A caller Send failure after commit must not suppress other members' shared update.
            if (action == GuildAction.StrongholdUpgrade && result == GuildResult.Success)
                Broadcast(GuildChangeKind.StrongholdUpgrade, connection);
        }
    }

    void Broadcast(GuildChangeKind kind, Connection? except = null)
    {
        foreach (Connection? connection in _members.Values)
        {
            if (connection == null || ReferenceEquals(connection, except) || !IsActive(connection)) continue;
            try
            {
                if (!_inventory.TryReadGuildView(connection.Session, connection.EntityId,
                    out InventoryRegistry.GuildView inventory)) continue;
                PreparedSnapshot snapshot = PrepareSnapshot(connection, _state, inventory, _members.Count,
                    _onlineCount, kind, ReadLocation(connection));
                if (!IsActive(connection)) continue;
                connection.Sequence = snapshot.Sequence;
                connection.Session.Send(snapshot.Payload);
            }
            catch (Exception ex)
            {
                Diagnose("broadcast recipient", ex);
            }
        }
    }

    void SendSnapshotIsolated(Connection connection, PreparedSnapshot snapshot)
    {
        try
        {
            if (IsActive(connection)) connection.Session.Send(snapshot.Payload);
        }
        catch (Exception ex)
        {
            Diagnose("admission send", ex);
        }
    }

    bool CanAdmit(GameSession session, int entityId, Guid accountId)
        => entityId > 0 && session.EntityId == entityId && !session.IsClosing &&
            session.Account?.AccountId == accountId && _ownsJoiningPlayer(session, entityId);

    Connection? FindDungeonClearOwner(GameSession session, int entityId)
    {
        if (session.Account is not { } account ||
            !_members.TryGetValue(account.AccountId, out Connection? connection) || connection == null ||
            !ReferenceEquals(connection.Session, session) || connection.EntityId != entityId || !IsActive(connection))
        {
            return null;
        }
        return connection;
    }

    bool IsActive(Connection connection)
        => !connection.Session.IsClosing && connection.Session.EntityId == connection.EntityId &&
            connection.Session.Account?.AccountId == connection.AccountId &&
            _members.TryGetValue(connection.AccountId, out Connection? owner) && ReferenceEquals(owner, connection);

    static GuildResult ReadLocation(Connection connection)
    {
        GameSession session = connection.Session;
        if (session.IsClosing || session.EntityId != connection.EntityId || session.Account?.AccountId != connection.AccountId)
            return GuildResult.NotMember;
        GameMap? map = session.ReadGuildMap();
        if (session.ReadMigrating() != 0 || map == null) return GuildResult.Unavailable;
        PlayerEntity? player = map.GetPlayer(connection.EntityId);
        if (player == null || !ReferenceEquals(player.Owner, session)) return GuildResult.Unavailable;
        if (map.MapId != MapId.Town) return GuildResult.NotInTown;
        return player.IsDead ? GuildResult.Dead : GuildResult.Success;
    }

    static PreparedSnapshot PrepareSnapshot(Connection connection, GuildState state,
        InventoryRegistry.GuildView inventory, int members, int online, GuildChangeKind kind, GuildResult location)
    {
        if (connection.Sequence == long.MaxValue)
        {
            connection.Session.Disconnect();
            throw new InvalidOperationException("Guild snapshot sequence exhausted; connection closed.");
        }
        long sequence = connection.Sequence + 1;
        return new PreparedSnapshot(sequence,
            GuildPackets.Snapshot(state, inventory, members, online, sequence, kind, location));
    }

    static void Diagnose(string phase, Exception exception)
        => Console.WriteLine($"[GuildRegistry] {phase} failed: {exception}");

    // Same-tick, single-use partner of Inventory's prepared clear reward. All snapshots and
    // recipient storage are built before either economy changes. Only the beneficiary's failure
    // aborts its reward; a different member's failed preparation is isolated from the settlement.
    internal sealed class PreparedDungeonClearCredit
    {
        readonly GuildRegistry _owner;
        readonly GuildState _next;
        readonly Connection _beneficiary;
        readonly PreparedSnapshot _snapshot;
        readonly (Connection Connection, PreparedSnapshot Snapshot)[] _others;

        internal PreparedDungeonClearCredit(GuildRegistry owner, GameSession session, int entityId,
            InventoryRegistry.GuildView rewardedInventory)
        {
            _owner = owner;
            _beneficiary = owner.FindDungeonClearOwner(session, entityId)
                ?? throw new InvalidOperationException("Dungeon clear guild owner is no longer active.");
            _next = owner._state.WithDungeonClearCredit();
            _snapshot = PrepareSnapshot(_beneficiary, _next, rewardedInventory,
                owner._members.Count, owner._onlineCount, GuildChangeKind.TreasuryCredit, ReadLocation(_beneficiary));

            List<(Connection, PreparedSnapshot)> others = new();
            foreach (Connection? connection in owner._members.Values)
            {
                if (connection == null || ReferenceEquals(connection, _beneficiary) || !owner.IsActive(connection)) continue;
                try
                {
                    if (!owner._inventory.TryReadGuildView(connection.Session, connection.EntityId,
                        out InventoryRegistry.GuildView inventory)) continue;
                    PreparedSnapshot snapshot = PrepareSnapshot(connection, _next, inventory,
                        owner._members.Count, owner._onlineCount, GuildChangeKind.TreasuryCredit, ReadLocation(connection));
                    if (owner.IsActive(connection)) others.Add((connection, snapshot));
                }
                catch (Exception ex)
                {
                    Diagnose("dungeon clear recipient preparation", ex);
                }
            }
            _others = others.ToArray();
        }

        internal bool IsOwnerActive => _owner.IsActive(_beneficiary);

        // Inventory calls this immediately after its own reference assignment. Do not move any
        // preparation or owner checks here: the entire two-owner commit is value publication.
        internal void Commit()
        {
            _owner._state = _next;
            _beneficiary.Sequence = _snapshot.Sequence;
            foreach (var recipient in _others)
                recipient.Connection.Sequence = recipient.Snapshot.Sequence;
        }

        internal void SendToBeneficiary()
        {
            if (IsOwnerActive) _beneficiary.Session.Send(_snapshot.Payload);
        }

        internal void SendToOtherMembers()
        {
            foreach (var recipient in _others)
            {
                try
                {
                    if (_owner.IsActive(recipient.Connection)) recipient.Connection.Session.Send(recipient.Snapshot.Payload);
                }
                catch (Exception ex)
                {
                    Diagnose("dungeon clear recipient send", ex);
                }
            }
        }
    }

    readonly record struct PreparedSnapshot(long Sequence, ArraySegment<byte> Payload);

    sealed class Connection
    {
        internal Connection(GameSession session, int entityId, Guid accountId)
        {
            Session = session;
            EntityId = entityId;
            AccountId = accountId;
        }

        internal GameSession Session { get; }

        internal int EntityId { get; }

        internal Guid AccountId { get; }

        internal long Sequence { get; set; }
    }
}
