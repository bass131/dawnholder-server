#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned view of the connection's latest dungeon result; confirmation is an intent, not a grant.</summary>
    [DisallowMultipleComponent]
    public sealed class DungeonClearRewardWindow : MonoBehaviour
    {
        const float Width = 520f;
        const float Height = 364f;
        DungeonClearRewardState? _source;
        CanvasGroup _group = null!;
        RectTransform _panel = null!;
        RectTransform _canvasRect = null!;
        TMP_Text _summary = null!;
        TMP_Text[] _items = null!;
        Button _confirm = null!;

        public static DungeonClearRewardWindow? Instance { get; private set; }

        public static DungeonClearRewardWindow BuildRuntime(Transform parent)
        {
            if (Instance != null) return Instance;
            var root = new GameObject("DungeonClearRewardWindow", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, worldPositionStays: false);
            var window = root.AddComponent<DungeonClearRewardWindow>();
            window.BuildView();
            root.SetActive(true);
            return window;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            BindSource();
            Render();
        }

        void OnDisable()
        {
            UnbindSource();
            SetVisibility(false);
        }

        void OnDestroy()
        {
            UnbindSource();
            if (_confirm != null) _confirm.onClick.RemoveListener(Confirm);
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (BindSource()) Render();
            UpdateVisibility();
            float scale = Mathf.Min(1f, (_canvasRect.rect.width - 24f) / Width,
                (_canvasRect.rect.height - 24f) / Height);
            _panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        bool BindSource()
        {
            var source = DungeonClearRewardState.EnsureInstance();
            if (ReferenceEquals(_source, source)) return false;
            UnbindSource();
            _source = source;
            source.Changed += Render;
            return true;
        }

        void UnbindSource()
        {
            // Destroyed Unity wrappers can still hold managed event subscriptions.
            if (!ReferenceEquals(_source, null)) _source!.Changed -= Render;
            _source = null;
        }

        // CombatBootstrap.CombatScenes selects gameplay scenes; only its installer creates this window.
        // UI loads additively without becoming active, so the window must belong to the active scene.
        bool IsInActiveScene() => gameObject.scene == SceneManager.GetActiveScene();

        void UpdateVisibility()
        {
            var session = UnityClientSession.Instance;
            bool visible = IsInActiveScene() && _source != null && _source.HasPendingFor(session);
            SetVisibility(visible);
            _confirm.interactable = visible && session != null && session.CanSendGameplay;
        }

        void SetVisibility(bool visible)
        {
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        void Render()
        {
            if (_source == null) return;
            bool success = _source.Result == InventoryResult.Success;
            _summary.text = success
                ? "받은 골드 " + _source.Currency.ToString("N0")
                : _source.Result == InventoryResult.CurrencyCap
                    ? "골드가 상한에 도달해 보상을 받지 못했습니다."
                    : "가방이 가득 차 보상을 받지 못했습니다.";
            for (int i = 0; i < _items.Length; i++)
            {
                InventorySlot item = i < _source.Items.Count ? _source.Items[i] : default;
                _items[i].text = success && item.ItemId != ItemId.None
                    ? ItemCatalog.For(item.ItemId).DisplayName + " ×" + item.Count
                    : string.Empty;
            }
            UpdateVisibility();
        }

        void Confirm()
        {
            if (!isActiveAndEnabled || !IsInActiveScene()) return;
            var session = UnityClientSession.Instance;
            var source = _source;
            if (session == null || source == null || !source.TryConsume(session)) return;
            try
            {
                session.SendIntent(new C_DungeonClearConfirm { reserved = 0 }.Write());
            }
            finally
            {
                // No automatic retry: SendIntent has no acknowledgement of server processing.
                source.NotifyChanged();
            }
        }

        void BuildView()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15; // Above HUD(10), below menu(20) and scene fades.
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();
            _canvasRect = (RectTransform)transform;
            SetVisibility(false);

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _panel = Rect("Rewards", transform, new Vector2(Width, Height), Vector2.zero);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            var background = _panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.95f);
            background.raycastTarget = false;

            TMP_Text title = Label("Title", _panel, new Vector2(472f, 50f), new Vector2(24f, -14f), font);
            title.text = "던전 클리어";
            title.fontSize = 30f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.90f, 0.35f);
            _summary = Label("Summary", _panel, new Vector2(472f, 76f), new Vector2(24f, -70f), font);
            _items = new TMP_Text[DungeonClearRewards.MaxItemKinds];
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i] = Label("Reward" + i, _panel, new Vector2(472f, 40f),
                    new Vector2(24f, -146f - i * 42f), font);
            }

            RectTransform buttonRect = Rect("Confirm", _panel, new Vector2(180f, 54f), new Vector2(170f, -292f));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.20f, 0.35f, 0.48f, 1f);
            _confirm = buttonRect.gameObject.AddComponent<Button>();
            _confirm.targetGraphic = buttonImage;
            TMP_Text caption = Label("Label", buttonRect, buttonRect.sizeDelta, Vector2.zero, font);
            caption.text = "확인";
            _confirm.onClick.AddListener(Confirm);
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

        static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position, TMP_FontAsset? font)
        {
            RectTransform rect = Rect(name, parent, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font; // Uses the project's global Korean fallback, like InventoryPanelView.
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
