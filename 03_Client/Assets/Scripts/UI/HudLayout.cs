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
        const int PortraitMaskTextureSize = 128;
        // The 120px frame's inner diameter is 88.125px in the source alpha; keep the stencil just inside it.
        const float PortraitMaskDiameter = 86f;
        // 1280×720·1920×1080 캡처로 맞춤: HUD portrait captures under
        // .backups/verification/2026-11-06-hud-portrait-login-rerecord/impl-01/u2/captures/portrait-*.png.
        // These offsets centre the face rather than the helmet/hood's full sprite rectangle.
        const float PortraitArtSize = 112f;
        static readonly Vector2 s_knightFaceOffset = new(-14f, 18f);
        static readonly Vector2 s_mageFaceOffset = new(0f, 12f);

        [SerializeField] Image[] _panels = System.Array.Empty<Image>();
        [SerializeField] RectTransform[] _portraits = System.Array.Empty<RectTransform>();
        [SerializeField] Image[] _icons = System.Array.Empty<Image>();
        [SerializeField] RectTransform _qSlot = null!;
        [SerializeField] RectTransform _eSlot = null!;
        [SerializeField] RectTransform[] _unmappedSlots = System.Array.Empty<RectTransform>();

        Sprite?[] _renderedIcons = System.Array.Empty<Sprite?>();
        Vector2[] _iconSizes = System.Array.Empty<Vector2>();
        Vector2[] _iconOrigins = System.Array.Empty<Vector2>();
        Texture2D? _portraitMaskTexture;
        Sprite? _portraitMaskSprite;

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
                if (portrait == null) continue;
                AddClippedPortrait(portrait);
                UiWindowAppearance.AddPortraitFrame(portrait);
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

        void AddClippedPortrait(RectTransform portrait)
        {
            Image source = portrait.GetComponent<Image>();
            if (source == null || source.sprite == null) return;
            if (_portraitMaskSprite == null) CreatePortraitMaskSprite();

            var maskObject = new GameObject("PortraitMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObject.transform.SetParent(portrait, false);
            Image maskImage = maskObject.GetComponent<Image>();
            Centre(maskImage.rectTransform, PortraitMaskDiameter, Vector2.zero);
            maskImage.sprite = _portraitMaskSprite;
            maskImage.raycastTarget = false;
            maskObject.GetComponent<Mask>().showMaskGraphic = false;

            var artObject = new GameObject("PortraitArt", typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(maskObject.transform, false);
            Image art = artObject.GetComponent<Image>();
            art.sprite = source.sprite;
            art.color = source.color;
            art.preserveAspect = true;
            art.raycastTarget = false;
            Centre(art.rectTransform, PortraitArtSize,
                source.sprite.name == "knight_icon_0" ? s_knightFaceOffset : s_mageFaceOffset);
            // Keep the scene's sprite reference for inspection, but draw it only inside the stencil.
            // HudController still switches the same portrait GameObject for the authoritative class.
            source.enabled = false;
        }

        void CreatePortraitMaskSprite()
        {
            // One layout owns these runtime resources; both portraits share them until this UI scene unloads.
            _portraitMaskTexture = new Texture2D(PortraitMaskTextureSize, PortraitMaskTextureSize,
                TextureFormat.RGBA32, false) { name = "HUD portrait circle", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[PortraitMaskTextureSize * PortraitMaskTextureSize];
            float radius = PortraitMaskTextureSize * 0.5f;
            for (int y = 0; y < PortraitMaskTextureSize; y++)
            for (int x = 0; x < PortraitMaskTextureSize; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                pixels[y * PortraitMaskTextureSize + x] =
                    new Color32(255, 255, 255, dx * dx + dy * dy <= radius * radius ? (byte)255 : (byte)0);
            }
            _portraitMaskTexture.SetPixels32(pixels);
            _portraitMaskTexture.Apply(false, true);
            _portraitMaskSprite = Sprite.Create(_portraitMaskTexture,
                new Rect(0f, 0f, PortraitMaskTextureSize, PortraitMaskTextureSize), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            _portraitMaskSprite.name = "HUD portrait circle";
            _portraitMaskSprite.hideFlags = HideFlags.DontSave;
        }

        static void Centre(RectTransform rect, float size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = offset;
        }

        void OnDestroy()
        {
            // EditMode scene tests invoke Awake too; release the same owned resources without a delayed player frame.
            if (Application.isPlaying)
            {
                if (_portraitMaskSprite != null) Destroy(_portraitMaskSprite);
                if (_portraitMaskTexture != null) Destroy(_portraitMaskTexture);
            }
            else
            {
                if (_portraitMaskSprite != null) DestroyImmediate(_portraitMaskSprite);
                if (_portraitMaskTexture != null) DestroyImmediate(_portraitMaskTexture);
            }
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
