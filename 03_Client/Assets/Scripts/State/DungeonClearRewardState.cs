#nullable enable
using System;
using System.Collections.Generic;
using Dawnholder.Client.Network;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.State
{
    /// <summary>
    /// Latest validated result for one connection, retained while gameplay scenes replace their windows.
    /// The handler applies it on main; the window consumes it; NetworkService clears it on disconnect.
    /// This is presentation state and never changes the inventory mirror.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DungeonClearRewardState : MonoBehaviour
    {
        UnityClientSession? _owner;
        bool _pending;

        internal event Action? Changed;

        public static DungeonClearRewardState? Instance { get; private set; }
        internal InventoryResult Result { get; private set; }
        internal int Currency { get; private set; }
        internal IReadOnlyList<InventorySlot> Items { get; private set; } = Array.Empty<InventorySlot>();

        internal static DungeonClearRewardState EnsureInstance()
        {
            if (Instance == null) new GameObject("_DungeonClearRewardState").AddComponent<DungeonClearRewardState>();
            return Instance!;
        }

        internal bool HasPendingFor(UnityClientSession? session) => _pending && session != null &&
            !session.IsClosed && ReferenceEquals(_owner, session);

        internal void Apply(UnityClientSession session, InventoryResult result, int currency, InventorySlot[] items)
        {
            _owner = session;
            Result = result;
            Currency = currency;
            Items = Array.AsReadOnly(items);
            _pending = true;
            NotifyChanged();
        }

        internal bool TryConsume(UnityClientSession session)
        {
            if (!HasPendingFor(session) || !session.CanSendGameplay) return false;
            // Consume before the send boundary: a second/reentrant click cannot send this result again.
            ResetSessionValues();
            return true;
        }

        internal void ResetSessionValues()
        {
            _owner = null;
            _pending = false;
            Result = default;
            Currency = 0;
            Items = Array.Empty<InventorySlot>();
        }

        internal void NotifyChanged() => Changed?.Invoke();

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
            ResetSessionValues();
            Changed = null;
            if (Instance == this) Instance = null;
        }
    }
}
