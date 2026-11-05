#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// HUD 장식 프레임·초상·스킬칸 그림과 아이콘 크기 맞춤을 맡는다.
    /// 스킬칸 4칸의 자리는 UI 장면이 고정하고, 창이 떠도 옮기지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class HudLayout : MonoBehaviour
    {
        [SerializeField] Image[] _panels = System.Array.Empty<Image>();
        [SerializeField] RectTransform[] _portraits = System.Array.Empty<RectTransform>();
        [SerializeField] Image[] _icons = System.Array.Empty<Image>();
        [SerializeField] RectTransform _qSlot = null!;
        [SerializeField] RectTransform _eSlot = null!;
        [SerializeField] RectTransform[] _unmappedSlots = System.Array.Empty<RectTransform>();

        Sprite?[] _renderedIcons = System.Array.Empty<Sprite?>();
        Vector2[] _iconSizes = System.Array.Empty<Vector2>();
        Vector2[] _iconOrigins = System.Array.Empty<Vector2>();

        void Awake()
        {
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
            foreach (RectTransform slot in _unmappedSlots)
            {
                if (slot != null) ApplySlotArtwork(slot);
            }
            _renderedIcons = new Sprite?[_icons.Length];
            _iconSizes = new Vector2[_icons.Length];
            _iconOrigins = new Vector2[_icons.Length];
            for (int index = 0; index < _icons.Length; index++)
            {
                if (_icons[index] == null) continue;
                _iconSizes[index] = _icons[index].rectTransform.sizeDelta;
                _iconOrigins[index] = _icons[index].rectTransform.anchoredPosition;
            }
        }

        void LateUpdate()
        {
            FitChangedIcons();
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
            // SkillHudController가 먼저 정한 빈 칸 alpha를 늦게 실행되는 Awake가 덮지 않는다.
            image.color = new Color(1f, 1f, 1f, image.color.a);
            image.raycastTarget = false;
        }
    }
}
