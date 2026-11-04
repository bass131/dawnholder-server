#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.UI
{
    /// <summary>Connects the current mouse to the view; the view owns rendered values and pointer policy.</summary>
    [DisallowMultipleComponent]
    internal sealed class InventorySlotPointer : MonoBehaviour
    {
        Action<Vector2, bool>? _onPointer;
        Action? _onUnavailable;

        internal static InventorySlotPointer Attach(GameObject owner, Action<Vector2, bool> onPointer,
            Action onUnavailable)
        {
            var pointer = owner.AddComponent<InventorySlotPointer>();
            pointer._onPointer = onPointer;
            pointer._onUnavailable = onUnavailable;
            return pointer;
        }

        // Disconnect managed callbacks even when the owner survives Dispose (including EditMode's direct calls).
        internal void Disconnect()
        {
            _onPointer = null;
            _onUnavailable = null;
        }

        void LateUpdate()
        {
            if (_onPointer == null) return;
            Mouse? mouse = Mouse.current;
            if (mouse == null)
            {
                _onUnavailable?.Invoke();
                return;
            }
            _onPointer(mouse.position.ReadValue(), mouse.rightButton.wasPressedThisFrame);
        }
    }
}
