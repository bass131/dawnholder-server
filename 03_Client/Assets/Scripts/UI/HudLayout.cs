#nullable enable
using Dawnholder.Client.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// HUD artwork and active-slot packing. Static geometry lives on UI.unity's RectTransforms;
    /// value, class and input state stay with their existing controllers.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class HudLayout : MonoBehaviour
    {
        [SerializeField] Image[] _panels = System.Array.Empty<Image>();
        [SerializeField] RectTransform[] _portraits = System.Array.Empty<RectTransform>();
        [SerializeField] Image[] _icons = System.Array.Empty<Image>();
        [SerializeField] RectTransform _actionBar = null!;
        [SerializeField] RectTransform _goldBar = null!;
        [SerializeField] RectTransform _qSlot = null!;
        [SerializeField] RectTransform _eSlot = null!;

        int _packedSlots = -1;
        Sprite?[] _renderedIcons = System.Array.Empty<Sprite?>();
        Vector2[] _iconSizes = System.Array.Empty<Vector2>();
        Vector2[] _iconOrigins = System.Array.Empty<Vector2>();
        Vector2 _actionOrigin;
        Vector2 _goldOrigin;
        bool _layoutReady;
        readonly Vector3[] _corners = new Vector3[4];

        void Awake()
        {
            if (_actionBar != null) _actionOrigin = _actionBar.anchoredPosition;
            if (_goldBar != null) _goldOrigin = _goldBar.anchoredPosition;
            foreach (Image panel in _panels)
            {
                if (panel == null) continue;
                UiWindowAppearance.ApplyPanel(panel);
                panel.raycastTarget = false;
            }
            foreach (RectTransform portrait in _portraits)
            {
                if (portrait != null) UiWindowAppearance.AddPortraitFrame(portrait);
            }
            ApplySlotArtwork(_qSlot);
            ApplySlotArtwork(_eSlot);
            _renderedIcons = new Sprite?[_icons.Length];
            _iconSizes = new Vector2[_icons.Length];
            _iconOrigins = new Vector2[_icons.Length];
            for (int index = 0; index < _icons.Length; index++)
            {
                if (_icons[index] == null) continue;
                _iconSizes[index] = _icons[index].rectTransform.sizeDelta;
                _iconOrigins[index] = _icons[index].rectTransform.anchoredPosition;
            }
            _layoutReady = true;
        }

        void OnDisable()
        {
            if (!_layoutReady) return;
            if (_actionBar != null) _actionBar.anchoredPosition = _actionOrigin;
            if (_goldBar != null) _goldBar.anchoredPosition = _goldOrigin;
            _packedSlots = -1;
        }

        void LateUpdate()
        {
            FitChangedIcons();
            if (_actionBar == null || _qSlot == null || _eSlot == null) return;
            // activeSelf expresses the controller's slot choice even while the whole bar is hidden.
            int slots = (_qSlot.gameObject.activeSelf ? 1 : 0) + (_eSlot.gameObject.activeSelf ? 1 : 0);
            if (_packedSlots != slots)
            {
                _packedSlots = slots;
                _actionBar.gameObject.SetActive(slots != 0);
                if (slots != 0)
                {
                    _actionBar.sizeDelta = new Vector2(28f + slots * 124f, _actionBar.sizeDelta.y);
                    if (_qSlot.gameObject.activeSelf) _qSlot.anchoredPosition = new Vector2(slots == 2 ? -62f : 0f, 0f);
                    if (_eSlot.gameObject.activeSelf) _eSlot.anchoredPosition = new Vector2(slots == 2 ? 62f : 0f, 0f);
                }
            }
            PlaceBesideBottomWindow();
        }

        void PlaceBesideBottomWindow()
        {
            if (_goldBar == null || _actionBar.parent is not RectTransform parent) return;
            NpcDialogPanel? npc = NpcDialogPanel.Instance;
            bool npcShown = npc != null && npc.isActiveAndEnabled && NpcDialogPanel.IsShown;
            DungeonClearRewardWindow? reward = DungeonClearRewardWindow.Instance;
            bool rewardShown = reward != null && reward.isActiveAndEnabled &&
                reward.TryGetComponent(out CanvasGroup group) && group.alpha > 0f;
            bool dock = false;
            if (npcShown || rewardShown)
            {
                Rect actionDefault = ScreenRect(_actionBar, _actionOrigin);
                Rect goldDefault = ScreenRect(_goldBar, _goldOrigin);
                if (npcShown)
                    dock = CoversDefault(npc!.transform.Find("DialogPanel") as RectTransform, actionDefault, goldDefault);
                if (rewardShown)
                    dock |= CoversDefault(reward!.transform.Find("Rewards") as RectTransform, actionDefault, goldDefault);
            }
            const float edge = 24f;
            Vector2 action = dock ? new Vector2(-parent.rect.width * 0.5f + edge +
                _actionBar.rect.width * _actionBar.pivot.x, _actionOrigin.y) : _actionOrigin;
            Vector2 gold = dock ? new Vector2(parent.rect.width * 0.5f - edge -
                _goldBar.rect.width * (1f - _goldBar.pivot.x), _goldOrigin.y) : _goldOrigin;
            if (_actionBar.anchoredPosition != action) _actionBar.anchoredPosition = action;
            if (_goldBar.anchoredPosition != gold) _goldBar.anchoredPosition = gold;
        }

        bool CoversDefault(RectTransform? popup, Rect action, Rect gold)
        {
            if (popup == null || !popup.gameObject.activeInHierarchy) return false;
            Rect screen = ScreenRect(popup, popup.anchoredPosition);
            return screen.Overlaps(action) || screen.Overlaps(gold);
        }

        Rect ScreenRect(RectTransform rect, Vector2 anchoredPosition)
        {
            rect.GetWorldCorners(_corners);
            Vector3 translation = rect.parent.TransformVector(anchoredPosition - rect.anchoredPosition);
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera? camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(camera, _corners[0] + translation);
            Vector2 maximum = minimum;
            for (int index = 1; index < _corners.Length; index++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, _corners[index] + translation);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        void FitChangedIcons()
        {
            for (int index = 0; index < _icons.Length; index++)
            {
                Image image = _icons[index];
                if (image == null || image.sprite == _renderedIcons[index]) continue;
                Sprite sprite = image.sprite;
                _renderedIcons[index] = sprite;
                RectTransform rect = image.rectTransform;
                rect.sizeDelta = _iconSizes[index];
                rect.anchoredPosition = _iconOrigins[index];
                if (sprite == null) continue;

                // Effect frames contain transparent margins. Keep the actual Sprite/UVs and fit its mesh once
                // per icon change; no texture readback, generated Sprite or Material is owned by this layout.
                Vector2[] vertices = sprite.vertices;
                if (vertices.Length < 3) continue;
                Vector2 minimum = vertices[0], maximum = vertices[0];
                foreach (Vector2 vertex in vertices)
                {
                    minimum = Vector2.Min(minimum, vertex);
                    maximum = Vector2.Max(maximum, vertex);
                }
                Vector2 size = maximum - minimum;
                Vector2 bounds = sprite.bounds.size;
                if (size.x <= 0f || size.y <= 0f || bounds.x <= 0f || bounds.y <= 0f) continue;
                float scale = Mathf.Min(_iconSizes[index].x / size.x, _iconSizes[index].y / size.y);
                rect.sizeDelta = bounds * scale;
                Vector2 spritePivot = sprite.pivot / sprite.rect.size;
                Vector2 drawingOffset = Vector2.Scale(rect.pivot - spritePivot, rect.sizeDelta);
                rect.anchoredPosition = _iconOrigins[index] - (minimum + maximum) * (0.5f * scale) + drawingOffset;
                image.type = Image.Type.Simple;
                image.useSpriteMesh = true;
                image.preserveAspect = false;
                image.raycastTarget = false;
            }
        }

        static void ApplySlotArtwork(RectTransform slot)
        {
            if (slot == null || !slot.TryGetComponent(out Image image)) return;
            UiWindowSkin skin = Resources.Load<UiWindowSkin>("UI/Components/UiWindowSkin");
            if (skin == null) throw new System.InvalidOperationException("Missing UI/Components/UiWindowSkin.");
            Image template = skin.InsetPrefab;
            image.sprite = template.sprite;
            image.type = template.type;
            image.pixelsPerUnitMultiplier = template.pixelsPerUnitMultiplier;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }
}
