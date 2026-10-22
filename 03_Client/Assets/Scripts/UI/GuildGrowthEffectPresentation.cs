#nullable enable
using System;
using System.Globalization;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Owns only the effect's layout, two bounded animation slots, four runtime sprites and the stronghold label material.
    /// GuildGrowthEffectView supplies Unity lifecycle and validated values; no server or session state is retained here.
    /// </summary>
    internal sealed class GuildGrowthEffectPresentation : IDisposable
    {
        const string AtlasPath = "UI/GuildGrowthEffect/GuildGrowthEffectAtlas";
        const int AtlasSize = 1254;
        readonly RectTransform _canvasRect;
        readonly Sprite[] _sprites = new Sprite[4];
        Material? _sharedLevelMaterial;
        EffectArea? _shared;
        EffectArea? _personal;
        bool _disposed;

        GuildGrowthEffectPresentation(GameObject root)
        {
            _canvasRect = (RectTransform)root.transform;
        }

        internal static GuildGrowthEffectPresentation Build(GameObject root)
        {
            var presentation = new GuildGrowthEffectPresentation(root);
            try
            {
                presentation.BuildView(root);
                return presentation;
            }
            catch
            {
                presentation.Dispose();
                throw;
            }
        }

        internal void Play(GuildChangeKind kind, int confirmedLevel, double now)
        {
            // Each slot replaces its own start time and text; the other kind has no shared timer or queue.
            EffectArea? area = kind == GuildChangeKind.StrongholdUpgrade ? _shared : _personal;
            Resize();
            area?.Play(confirmedLevel, now);
        }

        internal void Tick(double now)
        {
            if (_disposed) return;
            Resize();
            _shared?.Tick(now);
            _personal?.Tick(now);
        }

        internal void CancelAll()
        {
            _shared?.Cancel();
            _personal?.Cancel();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelAll();
            // Resources owns the atlas and font assets; we own only the runtime sprites and outlined material.
            foreach (Sprite sprite in _sprites)
            {
                if (sprite == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(sprite);
                else UnityEngine.Object.DestroyImmediate(sprite);
            }
            if (_sharedLevelMaterial != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_sharedLevelMaterial);
                else UnityEngine.Object.DestroyImmediate(_sharedLevelMaterial);
                _sharedLevelMaterial = null;
            }
        }

        void BuildView(GameObject root)
        {
            Texture2D atlas = Resources.Load<Texture2D>(AtlasPath);
            if (atlas == null || atlas.width != AtlasSize || atlas.height != AtlasSize)
            {
                throw new InvalidOperationException("Guild growth effect atlas is missing or its imported size changed.");
            }

            // Actual generated PNG: 1254x1254. Rects use Unity's bottom-left origin, not the prompt's requested grid.
            // The inspected artwork/gutters and matching top-left rects are recorded in the adjacent asset provenance.
            _sprites[0] = Slice(atlas, "SunStronghold", new Rect(32f, 646f, 600f, 584f));
            _sprites[1] = Slice(atlas, "WeaponCompletion", new Rect(672f, 646f, 552f, 584f));
            _sprites[2] = Slice(atlas, "AmberHalo", new Rect(0f, 0f, 672f, 646f));
            _sprites[3] = Slice(atlas, "GoldenSparks", new Rect(688f, 30f, 544f, 568f));

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 13; // Guild 12 < this independent effect < rewards 15.
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            var group = root.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            // Deliberately no GraphicRaycaster: the scene's actual UI remains the pointer target.

            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null && font.material != null)
            {
                // Own the outlined copy here so partial builds and scene teardown release it without changing the font asset.
                _sharedLevelMaterial = new Material(font.material) { hideFlags = HideFlags.HideAndDontSave };
                _sharedLevelMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                _sharedLevelMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                _sharedLevelMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(40, 20, 0, 255));
            }
            _shared = new EffectArea(_canvasRect, true, _sprites[0], _sprites[2], _sprites[3], font, _sharedLevelMaterial);
            _personal = new EffectArea(_canvasRect, false, _sprites[1], _sprites[2], _sprites[3], font);
        }

        void Resize()
        {
            Vector2 size = _canvasRect.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            // Smaller viewports shrink the two separated regions together, including text and light bounds.
            float scale = Mathf.Min(1f, size.x / 960f, size.y / 540f);
            _shared?.SetScale(scale);
            _personal?.SetScale(scale);
        }

        static Sprite Slice(Texture2D atlas, string name, Rect rect)
        {
            Sprite sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, worldPositionStays: false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        // A slot owns exactly one kind's display/time and reuses its three Images and one label for every request.
        sealed class EffectArea
        {
            readonly RectTransform _root;
            readonly Image _halo;
            readonly Image _emblem;
            readonly Image _sparks;
            readonly TMP_Text _level;
            readonly bool _shared;
            readonly float _duration;
            bool _playing;
            double _startedAt;

            internal EffectArea(Transform parent, bool shared, Sprite emblem, Sprite halo, Sprite sparks, TMP_FontAsset? font,
                Material? levelMaterial = null)
            {
                _shared = shared;
                _duration = shared ? 1.9f : 1.1f;
                _root = Rect(shared ? "StrongholdGrowth" : "WeaponEnhancement", parent,
                    shared ? new Vector2(380f, 400f) : new Vector2(260f, 250f), Vector2.zero);
                _root.gameObject.SetActive(false);
                _root.anchorMin = _root.anchorMax = shared ? new Vector2(0.43f, 0.62f) : new Vector2(0.80f, 0.28f);
                _halo = Image("PreLight", halo, shared ? 312f : 164f);
                _emblem = Image("Emblem", emblem, shared ? 216f : 132f);
                _sparks = Image("RaysAndMotes", sparks, shared ? 304f : 176f);
                RectTransform labelRect = Rect("ConfirmedLevel", _root,
                    shared ? new Vector2(336f, 56f) : new Vector2(228f, 48f),
                    new Vector2(0f, shared ? -148f : -104f));
                _level = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) _level.font = font;
                if (levelMaterial != null) _level.fontSharedMaterial = levelMaterial;
                // Same project font and global Korean fallback as GuildWindowLayout; supplied numbers are literal text.
                _level.fontSize = shared ? 28f : 24f;
                _level.fontStyle = FontStyles.Bold;
                _level.enableAutoSizing = true;
                _level.fontSizeMin = 16f;
                _level.fontSizeMax = shared ? 28f : 24f;
                _level.alignment = TextAlignmentOptions.Center;
                _level.textWrappingMode = TextWrappingModes.NoWrap;
                _level.overflowMode = TextOverflowModes.Overflow;
                _level.richText = false;
                _level.raycastTarget = false;
                _level.text = string.Empty;
            }

            internal void Play(int confirmedLevel, double now)
            {
                _startedAt = now;
                _playing = true;
                _level.text = (_shared ? "거점 레벨 " : "무기 강화 ")
                    + confirmedLevel.ToString(CultureInfo.InvariantCulture);
                Apply(0f);
                _root.gameObject.SetActive(true);
            }

            internal void Tick(double now)
            {
                if (!_playing) return;
                float age = (float)(now - _startedAt);
                if (age >= _duration) Cancel();
                else Apply(age);
            }

            internal void Cancel()
            {
                _playing = false;
                _startedAt = 0d;
                if (_root != null) _root.gameObject.SetActive(false);
                if (_level != null) _level.text = string.Empty;
            }

            internal void SetScale(float scale)
            {
                if (_root != null) _root.localScale = new Vector3(scale, scale, 1f);
            }

            void Apply(float age)
            {
                float progress = Mathf.Clamp01(age / _duration);
                float fadeSeconds = _shared ? 0.28f : 0.20f;
                float fade = 1f - Ease((age - (_duration - fadeSeconds)) / fadeSeconds);
                float revealDelay = _shared ? 0.18f : 0.14f;
                float reveal = Ease((age - revealDelay) / 0.18f);
                float burstDelay = _shared ? 0.48f : 0.32f;
                float burst = Ease((age - burstDelay) / 0.14f);
                float sparkle = burst * Mathf.Exp(-Mathf.Max(0f, age - burstDelay) * 1.6f);

                _halo.color = new Color(1f, 0.89f, 0.65f, fade * (0.16f + 0.48f * Ease(age / 0.24f)));
                float haloScale = Mathf.Lerp(0.65f, 1.08f, Ease(progress));
                _halo.rectTransform.localScale = new Vector3(haloScale, haloScale, 1f);
                _halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 10f, progress));

                _emblem.color = new Color(1f, 1f, 1f, reveal * fade);
                float emblemScale = Mathf.Lerp(0.78f, 1f, reveal);
                _emblem.rectTransform.localScale = new Vector3(emblemScale, emblemScale, 1f);
                _emblem.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-12f, 4f, reveal));

                _sparks.color = new Color(1f, 0.93f, 0.76f, sparkle * fade);
                float sparkScale = Mathf.Lerp(0.60f, 1.12f, Ease((age - burstDelay) / 0.40f));
                _sparks.rectTransform.localScale = new Vector3(sparkScale, sparkScale, 1f);
                _sparks.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 14f, progress));
                _level.color = new Color(1f, 0.94f, 0.73f, reveal * fade);
            }

            Image Image(string name, Sprite sprite, float size)
            {
                RectTransform rect = Rect(name, _root, new Vector2(size, size), Vector2.zero);
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return image;
            }

            static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
        }
    }
}
