#nullable enable
using System;
using System.Collections.Generic;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// Main-thread guild requests and server events, owned by one connection rather than its views or map.
    /// A purchase remains pending until its result and pointed snapshot agree; there is no retry timer.
    /// </summary>
    public sealed class GuildRequestController : IDisposable
    {
        readonly UnityClientSession _session;
        PendingPurchase? _purchase;
        long? _querySequence;
        object? _requestToken;
        bool _disposed;

        internal GuildRequestController(UnityClientSession session) => _session = session;

        public event Action? Changed;
        public event Action<GuildChangeKind>? GrowthConfirmed;

        public GuildClientState State { get; } = new GuildClientState();
        public bool IsPending => _purchase != null || _querySequence.HasValue;
        public bool IsSynchronized => !_disposed && _session.CanSendGameplay && State.HasSnapshot;
        public GuildResult? LastResult { get; private set; }
        public Exception? LastSendError { get; private set; }

        /// <summary>Returns a send attempt, not transport acceptance or server success.</summary>
        public bool Refresh()
        {
            if (_disposed || !_session.CanSendGameplay || IsPending) return false;
            _querySequence = State.SnapshotSequence;
            return SendRequest(new C_GuildRequest { reserved = 0 });
        }

        public bool TryUpgradeStronghold()
        {
            if (!IsSynchronized || IsPending || State.StrongholdAvailability != GuildResult.Success) return false;
            _purchase = new PendingPurchase(GuildAction.StrongholdUpgrade, State.GuildRevision, State.SnapshotSequence);
            return SendRequest(new C_StrongholdUpgrade { expectedGuildRevision = State.GuildRevision });
        }

        public bool TryEnhanceWeapon()
        {
            if (!IsSynchronized || IsPending || !State.HasPersonalSnapshot ||
                State.WeaponAvailability != GuildResult.Success) return false;
            _purchase = new PendingPurchase(GuildAction.WeaponEnhance, State.InventoryRevision, State.SnapshotSequence);
            return SendRequest(new C_WeaponEnhance { expectedInventoryRevision = State.InventoryRevision });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _purchase = null;
            _querySequence = null;
            _requestToken = null;
            LastResult = null;
            LastSendError = null;
            State.Clear();
            NotifyChanged();
            Changed = null;
            GrowthConfirmed = null;
        }

        internal void ReceiveInventoryRevision(uint revision)
        {
            if (!_disposed && State.ObserveInventoryRevision(revision)) NotifyChanged();
        }

        internal void ReceiveSnapshot(GuildSnapshotData snapshot)
        {
            if (_disposed) return;
            bool strongholdGrew = State.HasSnapshot && State.StrongholdLevel == 1 &&
                snapshot.StrongholdLevel == 2 && snapshot.ChangeKind == GuildChangeKind.StrongholdUpgrade;
            if (!State.Apply(snapshot)) return;

            if (_querySequence.HasValue && snapshot.SnapshotSequence > _querySequence.Value &&
                snapshot.ChangeKind == GuildChangeKind.Query)
            {
                _querySequence = null;
                _requestToken = null;
            }
            _purchase?.Observe(snapshot);
            bool weaponGrew = CompletePurchase();
            NotifyChanged();
            if (strongholdGrew) NotifyGrowth(GuildChangeKind.StrongholdUpgrade);
            if (weaponGrew) NotifyGrowth(GuildChangeKind.WeaponEnhance);
        }

        internal void ReceiveResult(S_GuildActionResult result)
        {
            if (_disposed || _purchase == null || !_purchase.Observe(result)) return;
            bool weaponGrew = CompletePurchase();
            NotifyChanged();
            if (weaponGrew) NotifyGrowth(GuildChangeKind.WeaponEnhance);
        }

        bool SendRequest(IPacket packet)
        {
            var token = new object();
            _requestToken = token;
            LastResult = null;
            LastSendError = null;
            try
            {
                _session.SendIntent(packet.Write(), error => ObserveSendError(token, error));
            }
            catch (Exception error)
            {
                ObserveSendError(token, error);
            }
            NotifyChanged();
            return LastSendError == null;
        }

        void ObserveSendError(object token, Exception error)
        {
            if (_disposed || !_session.IsCurrentConnection || !ReferenceEquals(token, _requestToken)) return;
            // A failed send is not proof that the server did not receive it. Preserve pending to
            // prevent an uncertain purchase from being retried, and expose the transport failure.
            LastSendError = error;
            NotifyChanged();
        }

        bool CompletePurchase()
        {
            if (_purchase == null || !_purchase.TryMatch(out var snapshot, out var result)) return false;
            bool weaponGrew = _purchase.Action == GuildAction.WeaponEnhance && result == GuildResult.Success &&
                snapshot!.ChangeKind == GuildChangeKind.WeaponEnhance;
            LastResult = result;
            _purchase = null;
            _requestToken = null;
            return weaponGrew;
        }

        void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action notify in Changed.GetInvocationList())
            {
                try { notify(); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }

        void NotifyGrowth(GuildChangeKind kind)
        {
            if (_disposed || !_session.IsCurrentConnection || GrowthConfirmed == null) return;
            foreach (Action<GuildChangeKind> notify in GrowthConfirmed.GetInvocationList())
            {
                try { notify(kind); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }

        sealed class PendingPurchase
        {
            // Only this request owns the bounded history. If a delayed result points beyond the
            // retained evidence, stay pending; a newer display is never substituted for its 44.
            const int SnapshotCapacity = 16;
            readonly uint _requestedRevision;
            readonly long _startSequence;
            readonly Queue<GuildSnapshotData> _snapshots = new Queue<GuildSnapshotData>();
            S_GuildActionResult? _result;

            internal PendingPurchase(GuildAction action, uint requestedRevision, long startSequence)
            {
                Action = action;
                _requestedRevision = requestedRevision;
                _startSequence = startSequence;
            }

            internal GuildAction Action { get; }

            internal void Observe(GuildSnapshotData snapshot)
            {
                if (snapshot.SnapshotSequence <= _startSequence) return;
                if (_snapshots.Count == SnapshotCapacity) _snapshots.Dequeue();
                _snapshots.Enqueue(snapshot);
            }

            internal bool Observe(S_GuildActionResult result)
            {
                if (_result != null || result.action != (byte)Action ||
                    result.requestedRevision != _requestedRevision || result.snapshotSequence <= _startSequence)
                    return false;
                _result = result;
                return true;
            }

            internal bool TryMatch(out GuildSnapshotData? matched, out GuildResult result)
            {
                matched = null;
                result = default;
                if (_result == null) return false;
                foreach (var snapshot in _snapshots)
                {
                    if (snapshot.SnapshotSequence != _result.snapshotSequence ||
                        snapshot.GuildRevision != _result.guildRevision ||
                        snapshot.InventoryRevision != _result.inventoryRevision) continue;
                    matched = snapshot;
                    result = (GuildResult)_result.result;
                    return true;
                }
                return false;
            }
        }
    }
}
