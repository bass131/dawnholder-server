#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// The arithmetic of <see cref="UiWindowPlacement"/> in the local units of the window's canvas: the fit scale, where
    /// an alignment puts the scaled window, how far it must be pulled to stay inside the canvas, and a pointer move.
    /// </summary>
    internal static class UiWindowPlacementGeometry
    {
        const float MinimumScale = 0.1f;

        internal static float FitScale(Vector2 area, Vector2 window, float maxScale)
        {
            float scale = maxScale;
            if (window.x > 0f) scale = Mathf.Min(scale, area.x / window.x);
            if (window.y > 0f) scale = Mathf.Min(scale, area.y / window.y);
            return Mathf.Max(MinimumScale, scale);
        }

        // The scaled window's bottom-left corner in the area, before the player's move and any pull.
        internal static Vector2 AlignedMin(Rect area, Vector2 size, UiWindowAlignment alignment) =>
            alignment == UiWindowAlignment.Center
                ? area.center - size * 0.5f
                : new Vector2(area.xMin, area.yMax - size.y);

        // How far a window with this bottom-left corner must move to lie inside the bounds. A window larger than the
        // bounds keeps its left and top edges inside, so its title bar stays where the pointer can take it again.
        internal static Vector2 PullInside(Rect bounds, Vector2 min, Vector2 size)
        {
            float x = size.x > bounds.width ? bounds.xMin : Mathf.Clamp(min.x, bounds.xMin, bounds.xMax - size.x);
            float y = size.y > bounds.height ? bounds.yMax - size.y : Mathf.Clamp(min.y, bounds.yMin, bounds.yMax - size.y);
            return new Vector2(x, y) - min;
        }

        // The anchoredPosition that puts the scaled window's bottom-left corner on min inside its parent's rect.
        // anchoredPosition is the pivot's offset from the anchor reference point: the point between the anchors that
        // the pivot picks, so the result holds for any anchors and pivot.
        internal static Vector2 AnchoredPosition(Rect parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 size, Vector2 min)
        {
            Vector2 anchorPoint = parent.min + Vector2.Scale(parent.size, anchorMin + Vector2.Scale(anchorMax - anchorMin, pivot));
            return min + Vector2.Scale(pivot, size) - anchorPoint;
        }

        // A pointer move between two screen points in the canvas's local units. The camera is the one the canvas is
        // drawn with, none for an overlay canvas.
        internal static Vector2 CanvasMove(RectTransform canvas, Camera? eye, Vector2 fromScreen, Vector2 toScreen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, fromScreen, eye, out Vector2 from) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, toScreen, eye, out Vector2 to))
                return Vector2.zero;
            return to - from;
        }
    }
}
