using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    // Main-thread queue drained explicitly by the map entry owner after all required bindings.
    internal sealed class RosterTransitionBuffer
    {
        const int MaxSize = 100;
        readonly List<Action> _buffer = new List<Action>();
        readonly Func<bool> _canApply;
        bool _closed;
        bool _pending;
        long _revision;

        public RosterTransitionBuffer(Func<bool> canApply = null) => _canApply = canApply ?? (() => true);
        public bool IsPending => _pending;

        public void BeginTransition(string destSceneName)
        {
            if (_closed || !_canApply()) return;
            _revision++;
            _buffer.Clear();
            _pending = true;
        }

        // true means buffered or intentionally dropped, including overflow.
        public bool TryBuffer(string packetLabel, Action action)
        {
            if (_closed || !_canApply()) return true;
            if (!_pending) return false;
            if (_buffer.Count >= MaxSize)
            {
                Debug.LogWarning($"[Unity] RosterBuffer overflow (>{MaxSize}) — {packetLabel} dropped.");
                return true;
            }
            _buffer.Add(action);
            return true;
        }

        public void Drain()
        {
            if (_closed || !_canApply() || !_pending) return;
            _pending = false;
            long revision = _revision;
            Action[] pending = _buffer.ToArray();
            _buffer.Clear();
            foreach (Action action in pending)
            {
                if (_closed || !_canApply() || revision != _revision) break;
                action();
            }
        }

        public void Teardown()
        {
            _closed = true;
            _revision++;
            _pending = false;
            _buffer.Clear();
        }
    }
}
