#nullable enable
using System;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Runtime uGUI/TMP presentation. It neither sends packets nor computes item effects.</summary>
    internal sealed class InventoryPanelView : IDisposable
    {
        const float Width = 550f;
        const float Height = 354f;
        readonly Vector3[] _hudCorners = new Vector3[4];
        readonly RectTransform _canvasRect;
        readonly RectTransform _panel;
        readonly CanvasGroup _group;
        readonly TMP_Text _currency;
        readonly TMP_Text _status;
        readonly TMP_Text _result;
        readonly TMP_Text[] _slotLabels = new TMP_Text[InventoryLimits.MaxSlots];
        readonly Button[] _useButtons = new Button[InventoryLimits.MaxSlots];
        readonly Button _refreshButton;

        internal InventoryPanelView(GameObject root, Action<int> useSlot, Action refresh)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // Existing HUD(10), menu(20), dialogue, stage clear and fades stay above it.
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            _group = root.AddComponent<CanvasGroup>();
            _canvasRect = (RectTransform)root.transform;

            // LiberationSans uses the project's TMP global Pretendard fallback for Korean catalog names.
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            _panel = Rect("Inventory", root.transform, new Vector2(Width, Height), Vector2.zero);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            var background = _panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);
            background.raycastTarget = false;

            // TMP's Korean fallback has taller line metrics than the Latin font; reserve the full line height.
            TMP_Text title = Label("Title", _panel, new Vector2(150f, 44f), new Vector2(12f, -10f), font);
            title.text = "인벤토리";
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.92f, 0.2f);
            _currency = Label("Currency", _panel, new Vector2(235f, 44f), new Vector2(172f, -10f), font);
            _currency.alignment = TextAlignmentOptions.Right;
            _refreshButton = Button("Refresh", _panel, new Vector2(112f, 44f), new Vector2(426f, -10f),
                "새로고침", font);
            _refreshButton.onClick.AddListener(() => refresh());

            _status = Label("Synchronization", _panel, new Vector2(526f, 66f), new Vector2(12f, -60f), font);
            _status.fontSize = 17f;
            _status.color = new Color(0.78f, 0.83f, 0.91f);
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                int slot = i;
                float x = 12f + (i % 2) * 269f;
                float y = -134f - (i / 2) * 43f;
                RectTransform row = Rect("Slot" + (i + 1), _panel, new Vector2(257f, 35f), new Vector2(x, y));
                var rowBackground = row.gameObject.AddComponent<Image>();
                rowBackground.color = new Color(1f, 1f, 1f, 0.06f);
                rowBackground.raycastTarget = false;
                _slotLabels[i] = Label("Item", row, new Vector2(193f, 35f), new Vector2(6f, 0f), font);
                _slotLabels[i].fontSize = 18f;
                _useButtons[i] = Button("Use", row, new Vector2(50f, 35f), new Vector2(203f, 0f), "사용", font);
                _useButtons[i].onClick.AddListener(() => useSlot(slot));
            }
            _result = Label("UseResult", _panel, new Vector2(526f, 42f), new Vector2(12f, -306f), font);
            _result.fontSize = 17f;
            Resize();
        }

        public void Dispose()
        {
            _refreshButton.onClick.RemoveAllListeners();
            foreach (Button button in _useButtons) button.onClick.RemoveAllListeners();
        }

        internal void SetVisibility(bool visible, bool interactive)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = interactive;
            _group.blocksRaycasts = interactive;
            // Only the small button images are raycast targets; the screen and slot backgrounds pass input through.
        }

        internal void Resize()
        {
            float canvasWidth = _canvasRect.rect.width;
            float canvasHeight = _canvasRect.rect.height;
            if (canvasWidth <= 0f || canvasHeight <= 0f) return;
            float top = 150f;
            var hud = HudController.Instance;
            if (hud != null && hud.transform.Find("character_status") is RectTransform status &&
                status.gameObject.activeInHierarchy)
            {
                // The additive HUD is center-anchored; its bottom moves when the screen aspect ratio changes.
                status.GetWorldCorners(_hudCorners);
                float hudBottom = _canvasRect.InverseTransformPoint(_hudCorners[0]).y;
                top = Mathf.Max(top, _canvasRect.rect.yMax - hudBottom + 12f);
            }
            float scale = Mathf.Min(1f, (canvasWidth - 24f) / Width, (canvasHeight - top - 12f) / Height);
            scale = Mathf.Max(0.1f, scale);
            _panel.localScale = new Vector3(scale, scale, 1f);
            _panel.anchoredPosition = new Vector2(0f, -top);
        }

        internal void Render(InventoryState? mirror, InventoryRequestController? requests, bool gameplayReady)
        {
            bool hasSnapshot = mirror != null && mirror.HasSnapshot;
            _currency.text = hasSnapshot ? "재화 " + mirror!.Currency.ToString("N0") : "재화 —";
            if (!gameplayReady) _status.text = "입장 준비 중 · 서버 확인 후 사용할 수 있어요.";
            else if (requests?.LastSendError != null) _status.text = "요청을 보내지 못했습니다. 다시 시도하세요.";
            else if (requests != null && requests.HasTimedOut && !requests.IsWaiting)
                _status.text = "응답이 없습니다. 새로고침해 주세요.";
            else if (requests != null && requests.IsUsing) _status.text = "아이템 사용 결과를 기다리는 중입니다.";
            else if (requests == null || !requests.IsSynchronized)
                _status.text = hasSnapshot ? "보관된 목록 · 서버에서 최신 목록을 확인 중입니다." : "인벤토리를 불러오는 중입니다.";
            else _status.text = "서버 확인 완료";

            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                InventorySlot slot = hasSnapshot ? mirror!.GetSlot(i) : default;
                bool occupied = slot.ItemId != ItemId.None;
                _slotLabels[i].text = occupied
                    ? ItemCatalog.For(slot.ItemId).DisplayName + " ×" + slot.Count
                    : hasSnapshot ? "빈 슬롯" : "—";
                bool usable = occupied && ItemCatalog.For(slot.ItemId).IsUsable;
                _useButtons[i].gameObject.SetActive(usable);
                _useButtons[i].interactable = usable && requests != null && requests.CanUse(slot.ItemId);
            }
            _refreshButton.interactable = gameplayReady && requests != null && !requests.IsWaiting;
            _result.text = ResultText(requests);
        }

        static string ResultText(InventoryRequestController? requests)
        {
            if (requests == null || !requests.LastResult.HasValue) return string.Empty;
            switch (requests.LastResult.Value)
            {
                case InventoryResult.Success:
                    return requests.IsSynchronized ? "아이템 사용이 완료되었습니다." : "사용 승인 · 목록을 갱신 중입니다.";
                case InventoryResult.Stale:
                    return requests.IsSynchronized ? "목록이 바뀌었습니다. 다시 선택해 주세요." : "변경된 목록을 확인 중입니다.";
                case InventoryResult.NotOwned: return "보유하지 않은 아이템입니다.";
                case InventoryResult.NotUsable: return "사용할 수 없는 아이템입니다.";
                case InventoryResult.CurrencyCap: return "재화 보관 한도에 도달했습니다.";
                case InventoryResult.InventoryFull: return "인벤토리에 빈 공간이 없습니다.";
                default: return "현재 아이템을 사용할 수 없습니다.";
            }
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
            if (font != null) text.font = font;
            text.fontSize = 20f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        static Button Button(string name, Transform parent, Vector2 size, Vector2 position,
            string caption, TMP_FontAsset? font)
        {
            RectTransform rect = Rect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.35f, 0.48f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text label = Label("Label", rect, size, Vector2.zero, font);
            label.text = caption;
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
