#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Default areas the owners of placed windows pass to <see cref="UiWindowPlacement.Place"/> every frame, in the local
    /// units of the owner's canvas, so they follow the screen size and the HUD.
    /// </summary>
    internal static class UiWindowAreas
    {
        const float Margin = 12f;
        // The HUD's left column, top to bottom; a window below it never covers the character or party panels.
        static readonly string[] s_hudLeftPanels = { "character_status", "Party_Info" };
        static readonly Vector3[] s_corners = new Vector3[4];

        /// <summary>
        /// The canvas less a margin on every side, with its top lowered below the HUD's left panels; a window aligned
        /// top-left in it stands in the screen's left column. Party_Info counts while no party shows: it hides through
        /// its CanvasGroup alpha and stays active, so joining or leaving a party never moves the window.
        /// </summary>
        internal static Rect LeftColumnBelowHud(RectTransform canvasRect)
        {
            Rect canvas = canvasRect.rect;
            float top = canvas.yMax - Margin;
            var hud = HudController.Instance;
            if (hud != null)
            {
                foreach (string name in s_hudLeftPanels)
                {
                    if (hud.transform.Find(name) is RectTransform panel && panel.gameObject.activeInHierarchy)
                    {
                        // The HUD has its own canvas and scale; its world corners bring the bottom into this canvas.
                        panel.GetWorldCorners(s_corners);
                        top = Mathf.Min(top, canvasRect.InverseTransformPoint(s_corners[0]).y - Margin);
                    }
                }
            }
            return Rect.MinMaxRect(canvas.xMin + Margin, canvas.yMin + Margin, canvas.xMax - Margin, top);
        }
    }
}
