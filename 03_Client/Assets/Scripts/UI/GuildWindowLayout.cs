#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Bright-kit guild window geometry and visibility; artwork belongs to UiWindowAppearance.</summary>
    internal sealed class GuildWindowLayout
    {
        const float Width = 440f;
        const float CompactHeight = 310f;
        // The housing window is landscape: its picture spans the inner width under the title (GuildHousingLayout).
        const float HousingWidth = 484f;
        const float HousingHeight = 474f;
        // Clear UI kept between a panel and either side of its slot.
        const float SlotMargin = 12f;
        // Content reserves lower-left/central panels; the growth effect plays over them in the screen centre
        // (user request 10-27: level-ups read as a centred celebration, and the effect never takes input).
        // These are layout proportions, not measurements of or a query into the HUD's private hierarchy.
        static readonly Rect s_windowArea = Rect.MinMaxRect(0.02f, 0.17f, 0.78f, 0.60f);
        static readonly Rect s_growthArea = Rect.MinMaxRect(0.15f, 0.12f, 0.85f, 0.88f);
        // Alone, the housing window stands between the character status at the top left (to 28.3% of a 16:9 width)
        // and the minimap at the top right (from 79.0%), so it can run the full height its picture needs.
        static readonly Rect s_housingAloneArea = Rect.MinMaxRect(0.2781f, 0.02f, 0.7948f, 0.98f);
        // Beside an open blacksmith it steps into the left column instead and leaves the blacksmith its own place.
        // The column runs down into the empty lower-left corner and stops below the character status.
        static readonly Rect s_housingArea = Rect.MinMaxRect(0.02f, 0.02f, 0.40f, 0.82f);
        // Dark ink on the bright parchment; light text only on the dark header pieces and the wood buttons.
        static readonly Color s_ink = new Color(0.26f, 0.17f, 0.09f);
        static readonly Color s_quietInk = new Color(0.45f, 0.33f, 0.2f);
        static readonly Color s_bandText = new Color(1f, 0.92f, 0.2f);
        readonly RectTransform _canvasRect;
        readonly RectTransform _panel;
        readonly CanvasGroup _group;
        readonly TMP_FontAsset? _font;
        readonly Rect _slot;
        readonly Rect _sharedSlot;
        readonly Vector2 _size;
        readonly bool _housing;
        // The housing rows are 20 UI high, so their autosize may go further down than the compact rows'.
        readonly float _minFontSize;

        internal GuildWindowLayout(GameObject root, string panelName, string title, bool right, bool housing = false)
        {
            float half = s_windowArea.width * 0.5f;
            _housing = housing;
            _slot = housing ? s_housingAloneArea
                : new Rect(s_windowArea.xMin + (right ? half : 0f), s_windowArea.yMin, half, s_windowArea.height);
            _sharedSlot = housing ? s_housingArea : _slot;
            _size = housing ? new Vector2(HousingWidth, HousingHeight) : new Vector2(Width, CompactHeight);
            _minFontSize = housing ? 8f : 14f;
            Skin = UiWindowAppearance.BrightSkin;
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
            _panel = UiWindowAppearance.CreatePanel(panelName, root.transform, raycastTarget: true, skin: Skin)
                .rectTransform;
            Place(_panel, _size, Vector2.zero);
            _panel.anchorMin = _panel.anchorMax = Vector2.zero;
            _panel.pivot = new Vector2(0.5f, 0.5f);
            // The housing title band runs along the whole top above the picture; the compact one keeps its corner.
            TMP_Text heading;
            if (housing)
            {
                Place(UiWindowAppearance.CreateHeader("Header", _panel, Skin).rectTransform,
                    new Vector2(464f, 28f), new Vector2(10f, -10f));
                heading = Label("Title", 10f, 28f, left: 22f, width: 220f);
            }
            else
            {
                Place(UiWindowAppearance.CreateHeader("Header", _panel, Skin).rectTransform,
                    new Vector2(132f, 40f), new Vector2(12f, -10f));
                heading = Label("Title", 12f, 36f, width: 132f);
            }
            heading.text = title;
            heading.fontStyle = FontStyles.Bold;
            heading.color = s_bandText;
            SetVisibility(false);
            Resize();
        }

        internal UiWindowSkin Skin { get; }

        internal TMP_Text Label(string name, float top, float height, float left = 16f, float width = 408f)
        {
            return Text(name, _panel, new Vector2(width, height), new Vector2(left, -top));
        }

        // A value standing on a dark header piece reads in the title's light colour.
        internal TMP_Text BandLabel(string name, float top, float height, float left, float width)
        {
            TMP_Text text = Label(name, top, height, left, width);
            text.color = s_bandText;
            return text;
        }

        // A fixed explanatory line, quieter than the values around it.
        internal TMP_Text Notice(string name, float top, float height, string caption,
            float left = 16f, float width = 408f)
        {
            TMP_Text notice = Label(name, top, height, left, width);
            notice.fontSizeMax = 17f;
            notice.color = s_quietInk;
            notice.text = caption;
            return notice;
        }

        internal Button ActionButton(string name, string caption)
        {
            // Share the header with the title/close control to free effect height without shrinking body text.
            return Button(name, caption, new Vector2(152f, 42f), new Vector2(160f, -8f),
                UiWindowAppearance.ButtonStyle.Accept);
        }

        internal Button CloseButton() => _housing
            ? Button("Close", "닫기", new Vector2(70f, 24f), new Vector2(398f, -12f))
            : Button("Close", "닫기", new Vector2(100f, 40f), new Vector2(324f, -10f));

        internal Button RefreshButton() => Button("Refresh", "새로고침", new Vector2(130f, 34f), new Vector2(294f, -240f));

        internal void Inset(float top, float height, float left = 12f, float width = 416f)
        {
            RectTransform rect = UiWindowAppearance.CreateInset("Inset", _panel, Skin).rectTransform;
            Place(rect, new Vector2(width, height), new Vector2(left, -top));
            rect.SetSiblingIndex(1);
        }

        internal void SetVisibility(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            // Unavailable/pending windows still shield gameplay clicks and allow closing.
            // Purchase and refresh buttons have their own current-state gate.
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        // Sized by this canvas's own scaled rect, never Screen. besideOpenWindow moves the housing window into its
        // shared column; a compact window keeps its slot either way.
        internal void Resize(bool besideOpenWindow = false)
        {
            float width = _canvasRect.rect.width;
            float height = _canvasRect.rect.height;
            if (width <= 0f || height <= 0f) return;
            Rect slot = besideOpenWindow ? _sharedSlot : _slot;
            float slotWidth = width * slot.width;
            float scale = Mathf.Max(0.01f, Mathf.Min(1f, (slotWidth - SlotMargin) / _size.x,
                height * slot.height / _size.y));
            _panel.localScale = new Vector3(scale, scale, 1f);
            _panel.anchoredPosition = new Vector2(width * slot.center.x, height * slot.center.y);
        }

        internal static bool TryGetGrowthArea(RectTransform effectRoot, out Rect area)
        {
            area = default;
            Rect root = effectRoot.rect;
            if (Screen.width <= 0 || Screen.height <= 0 || !ValidRect(root)) return false;
            Rect screen = Rect.MinMaxRect(Screen.width * s_growthArea.xMin, Screen.height * s_growthArea.yMin,
                Screen.width * s_growthArea.xMax, Screen.height * s_growthArea.yMax);

            // Screen pixels -> this actual canvas's local coordinates -> normalized local rect.
            // In particular, neither CanvasScaler.referenceResolution nor a fixed pixel divisor is the canvas size.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screen.min, null, out Vector2 min) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screen.max, null, out Vector2 max))
                return false;
            float left = Mathf.Clamp01((min.x - root.xMin) / root.width);
            float bottom = Mathf.Clamp01((min.y - root.yMin) / root.height);
            float right = Mathf.Clamp01((max.x - root.xMin) / root.width);
            float top = Mathf.Clamp01((max.y - root.yMin) / root.height);
            area = Rect.MinMaxRect(left, bottom, right, top);
            return ValidRect(area) && (double)area.x + area.width <= 1d && (double)area.y + area.height <= 1d;
        }

        static bool ValidRect(Rect rect)
        {
            return Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) &&
                rect.width > 0f && rect.height > 0f;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

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
            RectTransform rect = CreateRect(name, parent, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = 22f;
            text.enableAutoSizing = true;
            text.fontSizeMin = _minFontSize;
            text.fontSizeMax = 22f;
            text.color = s_ink;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            // Rows reserve Korean fallback line height; auto-sizing also accommodates long supplied names/numbers.
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false; // Supplied names are literal text, never TMP markup.
            text.raycastTarget = false;
            return text;
        }

        Button Button(string name, string caption, Vector2 size, Vector2 position,
            UiWindowAppearance.ButtonStyle style = UiWindowAppearance.ButtonStyle.Default)
        {
            Button button = UiWindowAppearance.CreateButton(name, _panel, style, Skin);
            RectTransform rect = (RectTransform)button.transform;
            Place(rect, size, position);
            TMP_Text label = Text("Label", rect, size, Vector2.zero);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.text = caption;
            return button;
        }

        internal static Button Entrance(Transform parent, string name, string caption, Vector2 position)
        {
            Button button = UiWindowAppearance.CreateButton(name, parent);
            var rect = (RectTransform)button.transform;
            Place(rect, new Vector2(144f, 44f), position);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            RectTransform labelRect = CreateRect("Label", rect, rect.sizeDelta, Vector2.zero);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            label.fontSize = 21f;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
            label.text = caption;
            return button;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, worldPositionStays: false);
            var rect = (RectTransform)child.transform;
            Place(rect, size, position);
            return rect;
        }

        static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
