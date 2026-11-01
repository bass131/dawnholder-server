#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>Shared slot hit geometry and screen-bounded detail placement, independent of input lifetime.</summary>
    internal static class InventoryPointerGeometry
    {
        internal static bool Contains(RectTransform rect, Vector2 point, Camera? eye) =>
            RectTransformUtility.RectangleContainsScreenPoint(rect, point, eye);

        internal static int FindSlot(RectTransform[] slots, Vector2 point, Camera? eye)
        {
            for (int i = 0; i < slots.Length; i++)
                if (Contains(slots[i], point, eye)) return i;
            return -1;
        }

        // The detail is a top-left-pivoted direct canvas child, independent of the inventory's drag/scale.
        internal static bool PlaceDetail(RectTransform canvas, RectTransform detail, Vector2 screenPoint, Camera? eye)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPoint, eye, out Vector2 point))
                return false;
            const float Gap = 12f;
            Rect bounds = canvas.rect;
            float width = detail.rect.width;
            float height = detail.rect.height;
            float left = point.x + Gap;
            float top = point.y - Gap;
            if (left + width > bounds.xMax) left = point.x - Gap - width;
            if (top - height < bounds.yMin) top = point.y + Gap + height;

            // Clamp both axes after flipping: points beyond any edge follow the same path.
            left = Mathf.Clamp(left, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - width));
            top = Mathf.Clamp(top, Mathf.Min(bounds.yMax, bounds.yMin + height), bounds.yMax);
            detail.anchoredPosition = new Vector2(left - bounds.xMin, top - bounds.yMax);
            return true;
        }
    }
}
