#nullable enable
using System;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>Main-thread, connection-owned economy requests. Only snapshots confirm displayed values.</summary>
    public sealed class InventoryRequestController : IDisposable
    {
        const float ResponseTimeoutSeconds = 5f;
        const int MaxQueryAttempts = 3;
        readonly UnityClientSession _session;
        readonly InventoryTimeoutScheduler _timeoutScheduler = new InventoryTimeoutScheduler();
        readonly Action<Action, float> _schedule;
        long _epoch = -1;
        long _timerVersion;
        int _queryAttempts;
        bool _timerScheduled;
        bool _waitingForSnapshot = true;
        bool _waitingForResult;
        bool _disposed;
        uint _requiredRevision;
        uint _useRevision;
        ItemId _useItem;

        internal InventoryRequestController(UnityClientSession session, Action<Action, float>? schedule)
        {
            _session = session;
            _schedule = schedule ?? _timeoutScheduler.Schedule;
        }

        public event Action? Changed;

        public bool IsSynchronized => IsCurrentGameplay && !_waitingForSnapshot &&
            InventoryState.Instance != null && InventoryState.Instance.HasSnapshot;
        public bool IsWaiting => _waitingForResult || _timerScheduled;
        public bool IsUsing => _waitingForResult;
        public bool HasTimedOut { get; private set; }
        public Exception? LastSendError { get; private set; }
        public InventoryResult? LastResult { get; private set; }
        public ItemId ResultItem { get; private set; }
        public uint ResultRevision { get; private set; }
        bool IsCurrentGameplay => !_disposed && _session.CanSendGameplay && _session.Entry.Epoch == _epoch;

        public bool CanUse(ItemId item)
        {
            if (!IsSynchronized || _waitingForResult || !ItemCatalog.IsDefined(item) ||
                !ItemCatalog.For(item).IsUsable) return false;
            var mirror = InventoryState.Instance;
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                InventorySlot slot = mirror.GetSlot(i);
                if (slot.ItemId == item && slot.Count > 0) return true;
            }
            return false;
        }

        public bool TryUse(ItemId item)
        {
            if (!CanUse(item)) return false;
            _useItem = item;
            _useRevision = InventoryState.Instance.Revision;
            _waitingForResult = true;
            LastResult = null;
            LastSendError = null;
            HasTimedOut = false;
            try
            {
                _session.SendIntent(new C_ItemUse { itemId = (int)item, expectedRevision = _useRevision }.Write());
                ScheduleTimeout();
            }
            catch (Exception error)
            {
                CancelTimer();
                _waitingForResult = false;
                LastSendError = error;
            }
            NotifyChanged();
            // This means an attempt, not transport acceptance or a server-confirmed use.
            return LastSendError == null;
        }

        public bool Refresh()
        {
            if (!IsCurrentGameplay || IsWaiting) return false;
            _queryAttempts = 0;
            _requiredRevision = RequiredSnapshotRevision();
            HasTimedOut = false;
            LastSendError = null;
            _waitingForSnapshot = true;
            SendQuery();
            return LastSendError == null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            ResetSessionValues();
            _disposed = true;
            Changed = null;
        }

        internal void OnGameplayReady(long epoch)
        {
            if (_disposed || !_session.CanSendGameplay || _session.Entry.Epoch != epoch || _epoch == epoch) return;
            CancelTimer();
            _epoch = epoch;
            _waitingForResult = false;
            _waitingForSnapshot = true;
            _requiredRevision = RequiredSnapshotRevision();
            _queryAttempts = 0;
            LastResult = null;
            HasTimedOut = false;
            LastSendError = null;
            SendQuery();
        }

        internal void ReceiveSnapshot(uint revision)
        {
            if (!IsCurrentGameplay || revision < _requiredRevision) return;
            _waitingForSnapshot = false;
            _queryAttempts = 0;
            HasTimedOut = false;
            LastSendError = null;
            if (!_waitingForResult) CancelTimer();
            NotifyChanged();
        }

        internal void ReceiveUseResult(InventoryResult result, ItemId item, uint revision)
        {
            var mirror = InventoryState.Instance;
            if (_disposed || revision < ResultRevision ||
                (mirror != null && mirror.HasSnapshot && revision < mirror.Revision) ||
                (LastResult.HasValue && revision <= ResultRevision)) return;
            if (_waitingForResult && (item != _useItem || revision < _useRevision ||
                (result == InventoryResult.Success && revision <= _useRevision))) return;

            bool wasUsing = _waitingForResult;
            _waitingForResult = false;
            LastResult = result;
            ResultItem = item;
            ResultRevision = revision;
            LastSendError = null;
            HasTimedOut = false;
            if (wasUsing) CancelTimer();

            if (result == InventoryResult.Success || result == InventoryResult.Stale)
            {
                // The server sends a following snapshot. If it is lost, requery after a bounded wait.
                _requiredRevision = revision;
                // A result cannot confirm a query/entry whose snapshot has not arrived.
                _waitingForSnapshot = _waitingForSnapshot || mirror == null || !mirror.HasSnapshot ||
                    mirror.Revision < revision || !IsCurrentGameplay;
                _queryAttempts = 0;
                if (_waitingForSnapshot && IsCurrentGameplay && !_timerScheduled) ScheduleTimeout();
            }
            NotifyChanged();
        }

        internal void ResetSessionValues()
        {
            CancelTimer();
            _epoch = -1;
            _queryAttempts = 0;
            _waitingForSnapshot = true;
            _waitingForResult = false;
            _requiredRevision = _useRevision = ResultRevision = 0;
            _useItem = ResultItem = ItemId.None;
            LastResult = null;
            LastSendError = null;
            HasTimedOut = false;
        }

        internal void NotifySessionReset() => NotifyChanged();

        void SendQuery()
        {
            if (!IsCurrentGameplay) return;
            _queryAttempts++;
            try
            {
                _session.SendIntent(new C_InventoryRequest { reserved = 0 }.Write());
                LastSendError = null;
            }
            catch (Exception error) { LastSendError = error; }
            ScheduleTimeout();
            NotifyChanged();
        }

        void ScheduleTimeout()
        {
            long version = ++_timerVersion;
            _timerScheduled = true;
            _schedule(() =>
            {
                if (version != _timerVersion || !IsCurrentGameplay) return;
                _timerScheduled = false;
                if (_waitingForResult)
                {
                    // Never repeat a use: it may have committed even though its answer was lost.
                    _waitingForResult = false;
                    _waitingForSnapshot = true;
                    _queryAttempts = 0;
                    HasTimedOut = true;
                    _requiredRevision = RequiredSnapshotRevision();
                }
                if (!_waitingForSnapshot) return;
                if (_queryAttempts < MaxQueryAttempts) SendQuery();
                else
                {
                    HasTimedOut = true;
                    NotifyChanged();
                }
            }, ResponseTimeoutSeconds);
            // The default scheduler replaces its one physical registration; the version also gates injected clocks.
            // Three queries per recovery round; no deadline callback can act on another epoch/session.
        }

        void CancelTimer()
        {
            _timeoutScheduler.Cancel();
            _timerVersion++;
            _timerScheduled = false;
        }

        uint RequiredSnapshotRevision() => Math.Max(ResultRevision,
            InventoryState.Instance != null ? InventoryState.Instance.Revision : 0);

        void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action notify in Changed.GetInvocationList())
            {
                try { notify(); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }
    }
}
