#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Visible higher overlay ink blocks a slot even when that overlay takes no raycasts.</summary>
    internal sealed class InventoryOverlayOcclusion
    {
        readonly List<Graphic> _graphics = new List<Graphic>();

        internal bool IsCovered(Canvas inventory, Vector2 point)
        {
            // Query on each eligible hover so windows opened after the pointer arrived are observed immediately.
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == inventory || !canvas.isActiveAndEnabled || !canvas.isRootCanvas ||
                    canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder <= inventory.sortingOrder)
                    continue;
                _graphics.Clear();
                canvas.GetComponentsInChildren<Graphic>(false, _graphics);
                foreach (Graphic graphic in _graphics)
                {
                    if (!graphic.isActiveAndEnabled || graphic.canvasRenderer.cull ||
                        graphic.color.a * graphic.canvasRenderer.GetInheritedAlpha() <= 0f)
                        continue;
                    if (CoversPoint(graphic, point)) return true;
                }
            }
            return false;
        }

        static bool CoversPoint(Graphic graphic, Vector2 point)
        {
            if (!(graphic is TMP_Text text))
                return InventoryPointerGeometry.Contains(graphic.rectTransform, point, null);
            // A HUD text box can be much wider than its ink; empty text draws no covering area.
            if (string.IsNullOrEmpty(text.text) || text.textInfo.characterCount == 0) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, point, null, out Vector2 local))
                return false;
            Bounds ink = text.textBounds;
            return ink.size.x > 0f && ink.size.y > 0f &&
                local.x >= ink.min.x && local.x <= ink.max.x && local.y >= ink.min.y && local.y <= ink.max.y;
        }
    }
}
