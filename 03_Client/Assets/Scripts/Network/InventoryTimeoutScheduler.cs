#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>One cancellable response deadline per connection owner, driven only on the main thread.</summary>
    internal sealed class InventoryTimeoutScheduler
    {
        static readonly LinkedList<InventoryTimeoutScheduler> s_pending = new LinkedList<InventoryTimeoutScheduler>();
        readonly LinkedListNode<InventoryTimeoutScheduler> _node;
        Action? _callback;
        double _dueTime;

        internal InventoryTimeoutScheduler()
        {
            _node = new LinkedListNode<InventoryTimeoutScheduler>(this);
        }

        internal void Schedule(Action callback, float delaySeconds)
        {
            Cancel();
            _callback = callback;
            _dueTime = Time.realtimeSinceStartupAsDouble + delaySeconds;
            // Equal deadlines retain registration order. Only connection response timers enter this list.
            LinkedListNode<InventoryTimeoutScheduler>? previous = s_pending.Last;
            while (previous != null && previous.Value._dueTime > _dueTime) previous = previous.Previous;
            if (previous == null) s_pending.AddFirst(_node);
            else s_pending.AddAfter(previous, _node);
        }

        internal void Cancel()
        {
            if (_node.List != null) s_pending.Remove(_node);
            _callback = null;
        }

        internal static void DrainDue(double now)
        {
            // Remove before invocation so callbacks may cancel or reschedule without retaining expired owners.
            while (s_pending.First != null && s_pending.First.Value._dueTime <= now)
            {
                InventoryTimeoutScheduler timer = s_pending.First.Value;
                Action? callback = timer._callback;
                timer.Cancel();
                try { callback?.Invoke(); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }
    }
}
