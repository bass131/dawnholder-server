#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Only the two guild windows' fallback layout. Owns geometry and visibility, never display state.</summary>
    internal sealed class GuildWindowLayout
    {
        const float Width = 440f;
        const float Height = 492f;
        readonly RectTransform _canvasRect;
        readonly RectTransform _panel;
        readonly CanvasGroup _group;
        readonly TMP_FontAsset? _font;

        internal GuildWindowLayout(GameObject root, string panelName, string title, bool right)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12; // Above HUD(10), below dungeon rewards(15), menu(20) and fades.
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            _group = root.AddComponent<CanvasGroup>();
            _canvasRect = (RectTransform)root.transform;
            // Use the same project font/global Korean fallback as InventoryPanelView.
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _panel = Rect(panelName, root.transform, new Vector2(Width, Height), Vector2.zero);
            _panel.anchorMin = _panel.anchorMax = new Vector2(right ? 0.75f : 0.25f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            var background = _panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.96f);
            background.raycastTarget = false;
            TMP_Text heading = Label("Title", 12f, 40f);
            heading.text = title;
            heading.fontStyle = FontStyles.Bold;
            heading.color = new Color(1f, 0.90f, 0.35f);
            SetVisibility(false, false);
            Resize();
        }

        internal TMP_Text Label(string name, float top, float height, float left = 16f, float width = 408f)
        {
            return Text(name, _panel, new Vector2(width, height), new Vector2(left, -top));
        }

        internal Button ActionButton(string name, string caption)
        {
            RectTransform rect = Rect(name, _panel, new Vector2(240f, 52f), new Vector2(100f, -424f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.35f, 0.48f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text label = Text("Label", rect, rect.sizeDelta, Vector2.zero);
            label.alignment = TextAlignmentOptions.Center;
            label.text = caption;
            return button;
        }

        internal void SetVisibility(bool visible, bool interactive)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible && interactive;
            _group.blocksRaycasts = visible && interactive;
        }

        internal void Resize()
        {
            float width = _canvasRect.rect.width;
            float height = _canvasRect.rect.height;
            if (width <= 0f || height <= 0f) return;
            // Reserve half the screen for each window so opening both does not cover either action.
            float scale = Mathf.Max(0.01f, Mathf.Min(1f, (width * 0.5f - 24f) / Width, (height - 24f) / Height));
            _panel.localScale = new Vector3(scale, scale, 1f);
        }

        internal static string AvailabilityText(GuildActionAvailability availability)
        {
            switch (availability)
            {
                case GuildActionAvailability.Available: return "진행할 수 있습니다.";
                case GuildActionAvailability.Locked: return "잠김 · 해금 후 이용할 수 있습니다.";
                case GuildActionAvailability.Insufficient: return "재화가 부족합니다.";
                case GuildActionAvailability.MaxLevel: return "최고 단계에 도달했습니다.";
                default: return "현재 이용할 수 없습니다.";
            }
        }

        internal static string ResultText(GuildActionResult result)
        {
            switch (result)
            {
                case GuildActionResult.Succeeded: return "요청 완료 · 확정된 상태입니다.";
                case GuildActionResult.Refused: return "요청 거부 · 현재 상태를 확인하세요.";
                default: return string.Empty;
            }
        }

        TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position)
        {
            RectTransform rect = Rect(name, parent, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = 22f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 22f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            // Rows reserve Korean fallback line height; auto-sizing also accommodates long supplied names/numbers.
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false; // Supplied names are literal text, never TMP markup.
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, worldPositionStays: false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
