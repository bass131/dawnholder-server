#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    internal enum UiWindowAlignment { TopLeft, Center }

    /// <summary>
    /// Places a window its owner lays out every frame: fitted into the owner's default area, aligned in it, moved by the
    /// player's drags on the title bar and kept inside the canvas. The player's move is remembered per key while the game
    /// runs, so a window rebuilt after closing or a map change opens where it was left. Only the window's localScale and
    /// anchoredPosition are written; its anchors, pivot and size stay the owner's.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class UiWindowPlacement : MonoBehaviour
    {
        // The player's moves in canvas units by memory key. Nothing is saved, so a restarted game opens every window
        // at its default place.
        static readonly Dictionary<string, Vector2> s_movedOffsets = new Dictionary<string, Vector2>();

        RectTransform _window = null!;
        RectTransform _canvasRect = null!;
        Canvas _canvas = null!;
        string _memoryKey = string.Empty;
        float _maxScale;
        UiWindowAlignment _alignment;
        Rect _area;
        bool _placed;

        internal static UiWindowPlacement Attach(RectTransform window, RectTransform titleBar, string memoryKey,
            float maxScale, UiWindowAlignment alignment)
        {
            // The owner's area and the window's anchoredPosition then share one space, the canvas's local units. Every
            // window here is built directly under its canvas root.
            var canvasRect = window.parent as RectTransform;
            Canvas? canvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
            if (canvasRect == null || canvas == null)
                throw new ArgumentException("The window must sit directly under its canvas.", nameof(window));
            var placement = window.gameObject.AddComponent<UiWindowPlacement>();
            placement._window = window;
            placement._canvasRect = canvasRect;
            placement._canvas = canvas;
            placement._memoryKey = memoryKey;
            placement._maxScale = maxScale;
            placement._alignment = alignment;
            placement.GrabArea = CreateGrabArea(titleBar, placement);
            return placement;
        }

        internal RectTransform GrabArea { get; private set; } = null!;

        Vector2 MovedOffset => s_movedOffsets.TryGetValue(_memoryKey, out Vector2 moved) ? moved : Vector2.zero;

        Camera? EventCamera => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        internal void Place(Rect defaultArea)
        {
            _area = defaultArea;
            _placed = true;
            // A pull here only changes what is shown. The stored move stays, so a canvas that grows back after a
            // resolution change returns the window to where the player left it.
            Show(MovedOffset);
        }

        // SubsystemRegistration runs on every Play entry, also with domain reload off, before the first scene's windows
        // read the table.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ForgetMovedPositions() => s_movedOffsets.Clear();

        // One drag step of the pointer between two screen points. A pull back inside the canvas is taken off the stored
        // move, so dragging back from past a screen edge moves the window at once.
        internal void MoveBy(Vector2 fromScreen, Vector2 toScreen)
        {
            if (!_placed) return;
            Vector2 moved = MovedOffset + UiWindowPlacementGeometry.CanvasMove(_canvasRect, EventCamera, fromScreen, toScreen);
            s_movedOffsets[_memoryKey] = moved + Show(moved);
        }

        // Writes the scale and anchoredPosition for the last area and the given move and returns the pull that kept the
        // window inside the canvas. Nothing it reads was written by an earlier call, so the same inputs write the same
        // values every frame.
        Vector2 Show(Vector2 moved)
        {
            Rect bounds = _canvasRect.rect;
            if (bounds.width <= 0f || bounds.height <= 0f) return Vector2.zero;
            Vector2 size = _window.rect.size;
            float scale = UiWindowPlacementGeometry.FitScale(_area.size, size, _maxScale);
            Vector2 scaled = size * scale;
            Vector2 min = UiWindowPlacementGeometry.AlignedMin(_area, scaled, _alignment) + moved;
            Vector2 pull = UiWindowPlacementGeometry.PullInside(bounds, min, scaled);
            _window.localScale = new Vector3(scale, scale, 1f);
            _window.anchoredPosition = UiWindowPlacementGeometry.AnchoredPosition(bounds, _window.anchorMin,
                _window.anchorMax, _window.pivot, scaled, min + pull);
            return pull;
        }

        // An invisible image filling the title bar as its last child, over the bar's own art. Buttons the owner made
        // after the title bar are later siblings, so they still take the pointer first.
        static RectTransform CreateGrabArea(RectTransform titleBar, UiWindowPlacement placement)
        {
            var grab = new GameObject("GrabArea", typeof(RectTransform));
            grab.transform.SetParent(titleBar, worldPositionStays: false);
            var rect = (RectTransform)grab.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var image = grab.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            // With cullTransparentMesh the canvas drops an alpha-0 mesh, and a dropped graphic takes no raycast.
            image.canvasRenderer.cullTransparentMesh = false;
            UiWindowGrabArea.Attach(grab, placement);
            return rect;
        }
    }
}
