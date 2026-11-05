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
        const float Width = 390f;
        const int Columns = 4;
        const float SlotSize = 81f;
        const float SlotGap = 14f;
        const float SlotsTop = 134f;
        const int Rows = (InventoryLimits.MaxSlots + Columns - 1) / Columns;
        const float Height = SlotsTop + Rows * SlotSize + (Rows - 1) * SlotGap + 120f;
        // Grow past the 1920x1080 design size on large screens; the HUD and screen bounds still cap it.
        const float MaxScale = 1.3f;
        // Dark ink on the bright parchment; white stays only on the wood buttons and the dark title band.
        static readonly Color s_ink = new Color(0.26f, 0.17f, 0.09f);
        static readonly Color s_quietInk = new Color(0.45f, 0.33f, 0.2f);
        readonly RectTransform _canvasRect;
        readonly RectTransform _panel;
        readonly CanvasGroup _group;
        readonly UiWindowSkin _skin;
        readonly UiWindowOpenEffect _openEffect;
        readonly UiWindowPlacement _placement;
        readonly TMP_Text _currency;
        readonly TMP_Text _status;
        readonly TMP_Text _result;
        readonly Image[] _slotBackgrounds = new Image[InventoryLimits.MaxSlots];
        readonly Image[] _iconBases = new Image[InventoryLimits.MaxSlots];
        readonly Image[] _icons = new Image[InventoryLimits.MaxSlots];
        readonly TMP_Text[] _slotCounts = new TMP_Text[InventoryLimits.MaxSlots];
        readonly InventorySlot[] _renderedSlots = new InventorySlot[InventoryLimits.MaxSlots];
        readonly RectTransform[] _slots = new RectTransform[InventoryLimits.MaxSlots];
        readonly RectTransform _detail;
        readonly TMP_Text _detailName;
        readonly TMP_Text _detailCount;
        readonly TMP_Text _detailEffect;
        readonly TMP_Text _detailUseHint;
        readonly Canvas _canvas;
        readonly InventorySlotPointer _pointer;
        readonly InventoryOverlayOcclusion _occlusion = new InventoryOverlayOcclusion();
        readonly Button _refreshButton;
        Action<int>? _useSlot;
        bool _shown;

        internal InventoryPanelView(GameObject root, Action<int> useSlot, Action refresh)
        {
            _skin = UiWindowAppearance.BrightSkin;
            _useSlot = useSlot;
            var canvas = _canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the HUD(10) and guild entrance, windows and effect(11-13), so skill slots never cover an inventory slot.
            // Dungeon rewards(15), menu(20), dialogue, stage clear and fades stay above it.
            canvas.sortingOrder = 14;
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
            _panel = UiWindowAppearance.CreatePanel("Inventory", root.transform, skin: _skin).rectTransform;
            Place(_panel, new Vector2(Width, Height), Vector2.zero);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            RectTransform header = UiWindowAppearance.CreateHeader("Header", _panel, _skin).rectTransform;
            Place(header, new Vector2(366f, 44f), new Vector2(12f, -10f));

            // TMP's Korean fallback has taller line metrics than the Latin font; reserve the full line height.
            // The title starts 14 UI inside the band so it clears the band's left border.
            TMP_Text title = Label("Title", _panel, new Vector2(150f, 44f), new Vector2(26f, -10f), font);
            title.text = "인벤토리";
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.92f, 0.2f);
            _refreshButton = Button("Refresh", _panel, new Vector2(112f, 44f), new Vector2(262f, -10f),
                "새로고침", font, _skin);
            _refreshButton.onClick.AddListener(() => refresh());

            _status = Label("Synchronization", _panel, new Vector2(366f, 66f), new Vector2(12f, -60f), font);
            _status.fontSize = 17f;
            _status.color = s_quietInk;
            var highlights = new Image[InventoryLimits.MaxSlots];
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                float x = 12f + (i % Columns) * (SlotSize + SlotGap);
                float y = -SlotsTop - (i / Columns) * (SlotSize + SlotGap);
                _slotBackgrounds[i] = UiWindowAppearance.CreateSliced("Slot" + (i + 1), _panel, _skin.ItemSlotEmpty);
                RectTransform row = _slots[i] = _slotBackgrounds[i].rectTransform;
                Place(row, new Vector2(SlotSize, SlotSize), new Vector2(x, y));
                // First child, so the hover border stays under the icon and corner count.
                highlights[i] = UiWindowAppearance.CreateSliced("Hover", row, _skin.ItemSlotSelected);
                Place(highlights[i].rectTransform, new Vector2(SlotSize, SlotSize), Vector2.zero);
                highlights[i].enabled = false;
                _iconBases[i] = UiWindowAppearance.CreateSimple("IconBase", row, _skin.IconBase);
                Place(_iconBases[i].rectTransform, new Vector2(57f, 57f), new Vector2(12f, -12f));
                _icons[i] = UiWindowAppearance.CreateSimple("Icon", row, _skin.MaterialIcon);
                Place(_icons[i].rectTransform, new Vector2(49f, 49f), new Vector2(16f, -16f));
                TMP_Text count = _slotCounts[i] = Label("Count", row, new Vector2(34f, 24f), new Vector2(44f, -54f), font);
                count.fontSize = count.fontSizeMax = 18f;
                count.fontSizeMin = 10f;
                count.enableAutoSizing = true;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.color = s_ink;
                count.alignment = TextAlignmentOptions.BottomRight;
                count.gameObject.SetActive(false);
                ShowItem(i, ItemId.None);
            }
            Place(UiWindowAppearance.CreateDivider("Divider", _panel, _skin).rectTransform,
                new Vector2(350f, 6f), new Vector2(20f, -(Height - 112f)));
            // Under the divider the coin and amount take one line and the last use result the line below it.
            Place(UiWindowAppearance.CreateSimple("Coin", _panel, _skin.CoinIcon).rectTransform,
                new Vector2(26f, 26f), new Vector2(22f, -(Height - 95f)));
            _currency = Label("Currency", _panel, new Vector2(324f, 36f), new Vector2(54f, -(Height - 100f)), font);
            _currency.fontSize = 18f;
            _currency.fontStyle = FontStyles.Bold;
            _currency.color = s_ink;
            _result = Label("UseResult", _panel, new Vector2(366f, 44f), new Vector2(12f, -(Height - 56f)), font);
            _result.fontSize = 16f;
            _result.color = s_ink;
            _detail = UiWindowAppearance.CreatePanel("ItemDetail", root.transform, skin: _skin).rectTransform;
            Place(_detail, new Vector2(320f, 196f), Vector2.zero);
            _detailName = Label("Name", _detail, new Vector2(284f, 44f), new Vector2(18f, -12f), font);
            _detailName.fontStyle = FontStyles.Bold;
            _detailCount = Label("Count", _detail, new Vector2(284f, 40f), new Vector2(18f, -56f), font);
            _detailEffect = Label("Effect", _detail, new Vector2(284f, 40f), new Vector2(18f, -100f), font);
            _detailUseHint = Label("UseHint", _detail, new Vector2(284f, 40f), new Vector2(18f, -142f), font);
            _detailName.color = _detailCount.color = _detailEffect.color = s_ink;
            _detailUseHint.color = s_quietInk;
            _detailUseHint.fontSize = 18f;
            _detailUseHint.text = "우클릭: 사용";
            foreach (Graphic graphic in _detail.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            HideItemDetail();
            InventorySlotHoverHighlight.Attach(_panel.gameObject, canvas, _group, _slots, highlights);
            _pointer = InventorySlotPointer.Attach(_panel.gameObject, UpdatePointer, HideItemDetail);
            _openEffect = UiWindowOpenEffect.Attach(_panel);
            _placement = UiWindowPlacement.Attach(_panel, header, "Inventory", MaxScale, UiWindowAlignment.TopLeft);
            _shown = _group.alpha > 0f;
            Resize();
        }

        public void Dispose()
        {
            _refreshButton.onClick.RemoveAllListeners();
            _pointer.Disconnect();
            _useSlot = null;
            HideItemDetail();
            // The root stays the owner's to destroy; only the opening's own drawing must not outlive the view.
            if (_openEffect != null) _openEffect.Stop();
        }

        internal void SetVisibility(bool visible, bool interactive)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = interactive;
            _group.blocksRaycasts = interactive;
            if (!visible || !interactive) HideItemDetail();
            // Only the small button images and the title bar's grab area are raycast targets; the screen and slot
            // backgrounds pass input through.
            if (visible && !_shown) _openEffect.Play();
            else if (!visible && _shown) _openEffect.Stop();
            _shown = visible;
        }

        // InventoryPanel calls this every frame; the shared part fits the panel below the HUD and keeps the player's move.
        internal void Resize() => _placement.Place(UiWindowAreas.LeftColumnBelowHud(_canvasRect));

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
                InventorySlot slot = _renderedSlots[i] = hasSnapshot ? mirror!.GetSlot(i) : default;
                bool occupied = slot.ItemId != ItemId.None;
                ShowItem(i, occupied ? slot.ItemId : ItemId.None);
                _slotCounts[i].text = occupied ? slot.Count.ToString() : string.Empty;
                _slotCounts[i].gameObject.SetActive(occupied && slot.Count >= 2);
            }
            HideItemDetail();
            _refreshButton.interactable = gameplayReady && requests != null && !requests.IsWaiting;
            _result.text = ResultText(requests);
        }

        void UpdatePointer(Vector2 point, bool usePressed)
        {
            if (_useSlot == null || _group.alpha <= 0f || !_group.blocksRaycasts)
            {
                HideItemDetail();
                return;
            }
            Camera? eye = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            int index = InventoryPointerGeometry.FindSlot(_slots, point, eye);
            if (index < 0 || _renderedSlots[index].ItemId == ItemId.None || _occlusion.IsCovered(_canvas, point))
            {
                HideItemDetail();
                return;
            }

            // Details follow exactly the values last rendered into the grid, without reading a second state source.
            InventorySlot slot = _renderedSlots[index];
            ItemDefinition item = ItemCatalog.For(slot.ItemId);
            _detailName.text = item.DisplayName;
            _detailCount.text = "수량 " + slot.Count;
            _detailEffect.text = "사용 효과: 금화 +" + item.CurrencyOnUse;
            _detailEffect.gameObject.SetActive(item.IsUsable);
            _detailUseHint.gameObject.SetActive(item.IsUsable);
            _detail.sizeDelta = new Vector2(320f, item.IsUsable ? 196f : 112f);
            bool placed = InventoryPointerGeometry.PlaceDetail(_canvasRect, _detail, point, eye);
            _detail.gameObject.SetActive(placed);
            if (placed && usePressed && item.IsUsable) _useSlot(index);
        }

        void HideItemDetail()
        {
            if (_detail != null) _detail.gameObject.SetActive(false);
        }

        // An occupied slot stands on the filled piece with its item's icon; an empty one shows only the pale slot.
        void ShowItem(int index, ItemId item)
        {
            Sprite? icon = item == ItemId.None ? null : IconFor(item);
            bool occupied = item != ItemId.None;
            _slotBackgrounds[index].sprite = occupied ? _skin.ItemSlotFilled : _skin.ItemSlotEmpty;
            _iconBases[index].enabled = occupied;
            if (icon != null) _icons[index].sprite = icon;
            _icons[index].enabled = icon != null;
        }

        // A catalog item without an icon here shows none; the appearance tests fail on that instead of hiding it.
        Sprite? IconFor(ItemId item) => item switch
        {
            ItemId.Material => _skin.MaterialIcon,
            ItemId.CoinPouch => _skin.CurrencyPouchIcon,
            _ => null,
        };

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
            string caption, TMP_FontAsset? font, UiWindowSkin skin)
        {
            Button button = UiWindowAppearance.CreateButton(name, parent, skin: skin);
            var rect = (RectTransform)button.transform;
            Place(rect, size, position);
            TMP_Text label = Label("Label", rect, size, Vector2.zero, font);
            label.text = caption;
            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
