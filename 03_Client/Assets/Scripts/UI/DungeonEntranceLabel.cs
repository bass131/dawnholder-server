#nullable enable
using Dawnholder.Client.Gameplay;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>World-space caption owned by Town's existing outbound portal; no input or portal rules.</summary>
    public static class DungeonEntranceLabel
    {
        public static void BuildRuntime(Transform parent)
        {
            if (parent.gameObject.scene.name != "Town") return;
            PortalTrigger? portal = null;
            foreach (PortalTrigger candidate in Object.FindObjectsByType<PortalTrigger>())
            {
                if (candidate.gameObject.scene != parent.gameObject.scene) continue;
                // Town currently has one outbound portal. Do not guess a destination if that contract changes.
                if (portal != null) return;
                portal = candidate;
            }
            if (portal == null || portal.transform.Find("DungeonEntranceLabel") != null) return;

            var root = new GameObject("DungeonEntranceLabel", typeof(RectTransform));
            root.transform.SetParent(portal.transform, worldPositionStays: false);
            Collider2D bounds = portal.GetComponent<Collider2D>();
            float top = bounds != null ? bounds.bounds.max.y : portal.transform.position.y + 1f;
            root.transform.position = new Vector3(portal.transform.position.x, top + 0.6f, portal.transform.position.z);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(5f, 1.2f);
            var text = root.AddComponent<TextMeshPro>();
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) text.font = font;
            text.text = "던전 입구";
            text.fontSize = 4f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = new Color(1f, 0.90f, 0.35f);
            text.raycastTarget = false;
            text.GetComponent<Renderer>().sortingOrder = 100;
        }
    }
}
