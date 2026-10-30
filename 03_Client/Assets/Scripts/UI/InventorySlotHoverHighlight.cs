#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Shows a slot's highlight border while the mouse is over the slot row. Slot backgrounds take no raycast (only
    /// the use buttons do), so pointer events never reach a row; the mouse position is compared with each row instead.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class InventorySlotHoverHighlight : MonoBehaviour
    {
        Canvas _canvas = null!;
        CanvasGroup _window = null!;
        RectTransform[] _rows = null!;
        Image[] _highlights = null!;

        internal static InventorySlotHoverHighlight Attach(GameObject owner, Canvas canvas, CanvasGroup window,
            RectTransform[] rows, Image[] highlights)
        {
            var hover = owner.AddComponent<InventorySlotHoverHighlight>();
            hover._canvas = canvas;
            hover._window = window;
            hover._rows = rows;
            hover._highlights = highlights;
            return hover;
        }

        void LateUpdate()
        {
            Mouse? mouse = Mouse.current;
            // A closed or non-interactive window shows no hover, matching the buttons that take no input then.
            bool active = mouse != null && _window.alpha > 0f && _window.blocksRaycasts;
            Vector2 point = active ? mouse!.position.ReadValue() : Vector2.zero;
            Camera? eye = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool hovered = active && RectTransformUtility.RectangleContainsScreenPoint(_rows[i], point, eye);
                if (_highlights[i].enabled != hovered) _highlights[i].enabled = hovered;
            }
        }
    }
}
