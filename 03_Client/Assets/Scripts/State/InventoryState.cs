#nullable enable
using System;
using System.Collections.Generic;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.State
{
    /// <summary>Connection mirror root: survives map scenes and is cleared by NetworkService's owner.</summary>
    [DisallowMultipleComponent]
    public sealed class InventoryState : MonoBehaviour
    {
        readonly InventorySnapshotState _snapshot = new InventorySnapshotState();

        public event Action? OnInventoryChanged;

        public static InventoryState Instance { get; private set; } = null!;
        public bool HasSnapshot => _snapshot.HasSnapshot;
        public uint Revision => _snapshot.Revision;
        public int Currency => _snapshot.Currency;

        public static InventoryState EnsureInstance()
        {
            // A push may precede the scene bootstrap. Retain it even when its panel is not created yet.
            if (Instance == null) new GameObject("_InventoryState").AddComponent<InventoryState>();
            return Instance;
        }

        public InventorySlot GetSlot(int index) => _snapshot.GetSlot(index);

        public void ResetSession()
        {
            ResetSessionValues();
            NotifySessionReset();
        }

        internal InventorySnapshotApplyResult ApplySnapshot(uint revision, int currency, InventorySlot[] slots) =>
            _snapshot.Apply(revision, currency, slots);

        internal void ResetSessionValues() => _snapshot.Reset();

        internal void NotifySessionReset() => NotifyChanged();

        internal void NotifyChanged()
        {
            if (OnInventoryChanged == null) return;
            var errors = new List<Exception>();
            foreach (Action notify in OnInventoryChanged.GetInvocationList())
            {
                try { notify(); }
                catch (Exception error) { errors.Add(error); }
            }
            if (errors.Count != 0) throw new AggregateException("Inventory notification failed.", errors);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null!;
            OnInventoryChanged = null;
        }
    }
}
