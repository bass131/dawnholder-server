#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;
using Slot = Dawnholder.Client.UI.CharacterMenuViewState.Slot;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned menu geometry and temporary input. No connection, character identity or entry authority.</summary>
    /// <remarks>
    /// Under the status line the panel shows one screen at a time: the slot row with Submit, or the create form in the
    /// slot row's place while the chosen slot is empty. Submit only selects an occupied slot; the form's Create creates
    /// and its Cancel goes back to the slot row.
    /// </remarks>
    internal sealed class CharacterMenuView : IDisposable
    {
        // The goal's approved wording of the server's name rule. The server judges names; the view only states it.
        const string NameRule = "2~12자, 한글·영문·숫자.\n영문은 대소문자만 다르면 같은 이름";
        // The panel's inner width is the header's; the status, slot row, create form and result lines all span it.
        const float InnerLeft = 20f;
        const float InnerWidth = 720f;
        // Three equal slot cards and two gaps fill the inner width. The create form takes the same rectangle.
        const float SlotGap = 12f;
        const float SlotWidth = (InnerWidth - 2f * SlotGap) / 3f;
        const float SlotHeight = 232f;
        const float SlotRowTop = -100f;
        // The create form's controls stand in one centred column; paired buttons split it with the slot gap.
        const float FormColumnWidth = 480f;
        const float FormColumnLeft = (InnerWidth - FormColumnWidth) / 2f;
        const float FormHalfWidth = (FormColumnWidth - SlotGap) / 2f;
        const float FormRightHalfLeft = FormColumnLeft + FormHalfWidth + SlotGap;
        // The 「이름」 caption and its gap; the input and the rule text under it start after them.
        const float NameCaptionWidth = 56f;
        // Dark ink on the bright parchment; white stays only on the wood buttons and the dark title band.
        static readonly Color s_ink = new(0.26f, 0.17f, 0.09f);
        static readonly Color s_secondaryText = new(0.45f, 0.33f, 0.2f);
        static readonly Color s_resultText = new(0.6f, 0.25f, 0.08f);
        // Unchosen characters stand slightly darker so the chosen one reads first.
        static readonly Color s_restingArt = new(0.78f, 0.78f, 0.78f, 1f);
        readonly UiWindowSkin _skin;
        readonly GameObject _canvasRoot;
        readonly CanvasGroup _group;
        readonly TMP_Text _status;
        readonly TMP_Text _result;
        readonly GameObject _form;
        readonly TMP_InputField _nameInput;
        readonly Button[] _slotButtons = new Button[3];
        readonly TMP_Text[] _slotLabels = new TMP_Text[3];
        readonly Image[] _slotFrames = new Image[3];
        readonly Image[] _slotGlows = new Image[3];
        readonly Image[] _slotArts = new Image[3];
        readonly CharacterArtBob[] _slotBobs = new CharacterArtBob[3];
        readonly Button _knight;
        readonly Button _mage;
        readonly TMP_Text _knightLabel;
        readonly TMP_Text _mageLabel;
        readonly Button _create;
        readonly Button _cancel;
        readonly Button _submit;
        readonly Button _refresh;
        readonly Button _back;
        Action<byte>? _selectRequested;
        Action<byte, byte, string>? _createRequested;
        Action? _refreshRequested;
        Action? _backRequested;
        CharacterMenuViewState _state = new(Phase.Connecting);
        byte _selectedSlot;
        byte _draftClass;
        bool _localPending;
        bool _disconnected;
        bool _closed;
        bool _disposed;

        internal CharacterMenuView(GameObject owner, Action<byte> selectRequested,
            Action<byte, byte, string> createRequested, Action refreshRequested, Action backRequested)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            _selectRequested = selectRequested ?? throw new ArgumentNullException(nameof(selectRequested));
            _createRequested = createRequested ?? throw new ArgumentNullException(nameof(createRequested));
            _refreshRequested = refreshRequested ?? throw new ArgumentNullException(nameof(refreshRequested));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null) throw new InvalidOperationException("Missing project menu font.");
            _skin = UiWindowAppearance.BrightSkin;

            _canvasRoot = new GameObject("CharacterMenu", typeof(RectTransform));
            _canvasRoot.transform.SetParent(owner.transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            // The existing minimum windows remain close to one UI unit per pixel.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 450f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRoot.AddComponent<GraphicRaycaster>();
            _group = _canvasRoot.AddComponent<CanvasGroup>();

            // Heights reserve the Korean fallback font's line box (about 1.9 em per line, 3.9 em for two):
            // under Ellipsis, TMP drops a line that does not fit instead of clipping it.
            RectTransform panel = UiWindowAppearance.CreatePanel("Panel", _canvasRoot.transform, raycastTarget: true,
                skin: _skin).rectTransform;
            Place(panel, new Vector2(760f, 430f), Vector2.zero);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            Place(UiWindowAppearance.CreateHeader("Header", panel, _skin).rectTransform,
                new Vector2(InnerWidth, 46f), new Vector2(InnerLeft, -8f));
            TMP_Text title = Label("Title", panel, new Vector2(696f, 46f), new Vector2(32f, -8f), font);
            title.text = "캐릭터 선택";
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            _status = Label("Status", panel, new Vector2(InnerWidth, 38f), new Vector2(InnerLeft, -58f), font);
            _status.color = s_ink;

            // The slot row: three cards side by side, each with glow, standing art on its pedestal and name plate.
            for (byte slot = 1; slot <= 3; slot++)
            {
                byte chosenSlot = slot;
                var position = new Vector2(InnerLeft + (slot - 1) * (SlotWidth + SlotGap), SlotRowTop);
                _slotButtons[slot - 1] = MakeSlot(slot - 1, panel, position, font);
                _slotButtons[slot - 1].onClick.AddListener(() => OnSlotClicked(chosenSlot));
            }

            // The create form, in the slot row's rectangle: name, rule, class, hint, then Cancel and Create.
            RectTransform form = Rect("CreateForm", panel, new Vector2(InnerWidth, SlotHeight),
                new Vector2(InnerLeft, SlotRowTop));
            _form = form.gameObject;
            const float nameLeft = FormColumnLeft + NameCaptionWidth;
            const float nameWidth = FormColumnWidth - NameCaptionWidth;
            var halfButton = new Vector2(FormHalfWidth, 40f);
            TMP_Text nameLabel = Label("NameLabel", form, new Vector2(48f, 40f), new Vector2(FormColumnLeft, 0f), font);
            nameLabel.text = "이름";
            nameLabel.color = s_ink;
            _nameInput = CreateNameInput(form, new Vector2(nameWidth, 40f), new Vector2(nameLeft, 0f), font, _skin);
            TMP_Text nameRule = Label("NameRule", form, new Vector2(nameWidth, 64f), new Vector2(nameLeft, -44f), font);
            nameRule.text = NameRule;
            nameRule.fontSize = 16f;
            nameRule.color = s_secondaryText;
            _knight = MakeButton("Knight", form, halfButton, new Vector2(FormColumnLeft, -112f),
                "Knight", font, out _knightLabel, _skin);
            _mage = MakeButton("Mage", form, halfButton, new Vector2(FormRightHalfLeft, -112f),
                "Mage", font, out _mageLabel, _skin);
            AddClassPicture(_knight, _knightLabel, 0);
            AddClassPicture(_mage, _mageLabel, 1);
            _knight.onClick.AddListener(() => OnClassClicked(0));
            _mage.onClick.AddListener(() => OnClassClicked(1));
            TMP_Text hint = Label("Hint", form, new Vector2(FormColumnWidth, 32f),
                new Vector2(FormColumnLeft, -156f), font);
            hint.text = "생성 후 입장할 캐릭터를 다시 선택해주세요.";
            hint.fontSize = 15f;
            hint.color = s_secondaryText;
            _cancel = MakeButton("Cancel", form, halfButton, new Vector2(FormColumnLeft, -192f),
                "취소", font, out _, _skin);
            _create = MakeButton("Create", form, halfButton, new Vector2(FormRightHalfLeft, -192f),
                "캐릭터 생성", font, out _, _skin, UiWindowAppearance.ButtonStyle.Accept);
            _cancel.onClick.AddListener(OnCancelClicked);
            _create.onClick.AddListener(OnCreateClicked);

            _result = Label("Result", panel, new Vector2(InnerWidth, 38f), new Vector2(InnerLeft, -336f), font);
            _result.color = s_resultText;
            _back = MakeButton("Back", panel, new Vector2(134f, 40f), new Vector2(20f, -378f),
                "뒤로", font, out _, _skin);
            _refresh = MakeButton("Refresh", panel, new Vector2(146f, 40f), new Vector2(164f, -378f),
                "목록 새로고침", font, out _, _skin);
            _submit = MakeButton("Submit", panel, new Vector2(214f, 40f), new Vector2(526f, -378f),
                "선택하고 입장", font, out _, _skin, UiWindowAppearance.ButtonStyle.Accept);
            _back.onClick.AddListener(OnBackClicked);
            _refresh.onClick.AddListener(OnRefreshClicked);
            _submit.onClick.AddListener(OnSubmitClicked);
            Render(_state);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = _closed = true;
            _selectRequested = null;
            _createRequested = null;
            _refreshRequested = _backRequested = null;
            if (_canvasRoot == null) return;
            _group.alpha = 0f;
            _group.interactable = _group.blocksRaycasts = false;
            foreach (Button button in _canvasRoot.GetComponentsInChildren<Button>(true))
                button.onClick.RemoveAllListeners();
            _canvasRoot.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(_canvasRoot);
            else UnityEngine.Object.DestroyImmediate(_canvasRoot);
        }

        /// <summary>
        /// Main-thread display projection. Render immediately after each request attempt, including rejection.
        /// Pending comes from the client; the local latch only closes the gap before that projection arrives.
        /// </summary>
        internal void Render(CharacterMenuViewState state)
        {
            // Back and transport closure are terminal for this view. Reconnection uses a new scene-owned view.
            if (_disposed || _closed || _disconnected) return;
            if (state == null) throw new ArgumentNullException(nameof(state));
            // A choice, and with it an open create form, lasts only while its slot shows the same thing: a created
            // character closes the form with no slot chosen, a refusal (slot still empty) keeps the form and its draft.
            if (!HasList(state.CurrentPhase) ||
                (_selectedSlot != 0 && !SameDisplaySlot(_state.GetSlot(_selectedSlot), state.GetSlot(_selectedSlot))))
                ResetSelection();
            _state = state;
            _localPending = false;
            _disconnected = state.CurrentPhase == Phase.Disconnected;
            _status.text = PhaseCaption(state.CurrentPhase);
            _result.text = state.Message;
            Redraw();
        }

        bool CanEdit() => !_disposed && !_closed && !_disconnected && !_localPending && _state.CurrentPhase == Phase.Ready;

        // The form is open while the chosen slot is empty; the choice is the view's memory of which slot it fills.
        bool CreateFormOpen => _selectedSlot != 0 && _state.GetSlot(_selectedSlot) == null;

        void OnSlotClicked(byte slot)
        {
            // The slot row is hidden while the form is open, so nothing may change the slot the form fills.
            if (!CanEdit() || CreateFormOpen || slot < 1 || slot > 3 || slot == _selectedSlot) return;
            _selectedSlot = slot;
            ResetDraft();
            _result.text = string.Empty;
            Redraw();
        }

        void OnClassClicked(byte classId)
        {
            if (!CanEdit() || !CreateFormOpen) return;
            _draftClass = classId;
            _result.text = string.Empty;
            UpdateInteraction();
        }

        void OnCreateClicked()
        {
            if (!CanEdit() || !CreateFormOpen) return;
            byte slot = _selectedSlot;
            // Capture the raw draft before a synchronous caller can render a replacement snapshot.
            string rawName = _nameInput.text;
            byte classId = _draftClass;
            _localPending = true;
            _result.text = string.Empty;
            _status.text = "캐릭터 생성 요청 중입니다.";
            UpdateInteraction();
            _createRequested?.Invoke(slot, classId, rawName);
        }

        // Leaves the form without a request. Dropping the choice also clears the last result, as choosing a slot does.
        void OnCancelClicked()
        {
            if (!CanEdit() || !CreateFormOpen) return;
            ResetSelection();
            _result.text = string.Empty;
            Redraw();
        }

        void OnSubmitClicked()
        {
            if (!CanEdit() || _selectedSlot == 0 || CreateFormOpen) return;
            byte slot = _selectedSlot;
            _localPending = true;
            _result.text = string.Empty;
            _status.text = "캐릭터 선택 요청 중입니다.";
            UpdateInteraction();
            _selectRequested?.Invoke(slot);
        }

        void OnRefreshClicked()
        {
            if (_disposed || _closed || _disconnected || _localPending ||
                (_state.CurrentPhase != Phase.Ready && _state.CurrentPhase != Phase.ListFailed)) return;
            _localPending = true;
            _status.text = "캐릭터 목록을 불러오는 중입니다.";
            _result.text = string.Empty;
            UpdateInteraction();
            _refreshRequested?.Invoke();
        }

        void OnBackClicked()
        {
            if (_disposed || _closed) return;
            _closed = true;
            _status.text = "메뉴로 돌아가는 중입니다.";
            ResetSelection();
            Redraw();
            _backRequested?.Invoke();
        }

        void ResetSelection()
        {
            _selectedSlot = 0;
            ResetDraft();
        }

        void ResetDraft()
        {
            _draftClass = 0;
            _nameInput.SetTextWithoutNotify(string.Empty);
        }

        void Redraw()
        {
            UpdateSlots();
            UpdateScreen();
            UpdateInteraction();
        }

        void UpdateSlots()
        {
            for (byte slot = 1; slot <= 3; slot++)
            {
                string heading = slot == _selectedSlot ? $"선택 · 슬롯 {slot}" : $"슬롯 {slot}";
                if (!HasList(_state.CurrentPhase))
                {
                    string unknown = _state.CurrentPhase == Phase.ListFailed ? "확인 실패" :
                        _state.CurrentPhase == Phase.Disconnected ? "연결 종료" : "목록 확인 전";
                    _slotLabels[slot - 1].text = heading + "\n" + unknown;
                    continue;
                }
                Slot? character = _state.GetSlot(slot);
                _slotLabels[slot - 1].text = character == null ? heading + "\n빈 슬롯" :
                    heading + " · " + ClassCaption(character.ClassId) + "\n" + character.Name;
            }
            for (byte slot = 1; slot <= 3; slot++)
                ShowSlotArt(slot - 1, slot == _selectedSlot, HasList(_state.CurrentPhase) ? _state.GetSlot(slot) : null);
        }

        // An empty or unknown slot keeps only its pedestal; the chosen slot glows and its character moves.
        void ShowSlotArt(int index, bool chosen, Slot? character)
        {
            _slotFrames[index].sprite = chosen ? _skin.CharacterSlotSelected : _skin.CharacterSlot;
            _slotGlows[index].enabled = chosen;
            Sprite? art = character == null ? null : ClassArt(character.ClassId);
            if (art != null) _slotArts[index].sprite = art;
            _slotArts[index].enabled = art != null;
            _slotArts[index].color = chosen ? Color.white : s_restingArt;
            _slotBobs[index].SetPlaying(chosen && art != null);
        }

        Sprite? ClassArt(byte classId) => classId == 0 ? _skin.KnightArt : classId == 1 ? _skin.MageArt : null;

        // The open form replaces the slot row and Submit; otherwise the row and Submit show and the form hides.
        void UpdateScreen()
        {
            bool formOpen = CreateFormOpen;
            _form.SetActive(formOpen);
            foreach (Button slotButton in _slotButtons) slotButton.gameObject.SetActive(!formOpen);
            _submit.gameObject.SetActive(!formOpen);
        }

        void UpdateInteraction()
        {
            bool editing = CanEdit();
            bool formOpen = CreateFormOpen;
            bool creating = editing && formOpen;
            foreach (Button button in _slotButtons) button.interactable = editing;
            _nameInput.interactable = _knight.interactable = _mage.interactable = creating;
            _create.interactable = _cancel.interactable = creating;
            _knightLabel.text = _draftClass == 0 ? "Knight · 선택" : "Knight";
            _mageLabel.text = _draftClass == 1 ? "Mage · 선택" : "Mage";
            // Submit only selects: it needs an occupied slot chosen in Ready.
            _submit.interactable = editing && _selectedSlot != 0 && !formOpen;
            _refresh.interactable = !_closed && !_disposed && !_disconnected && !_localPending &&
                (_state.CurrentPhase == Phase.Ready || _state.CurrentPhase == Phase.ListFailed);
            _back.interactable = !_closed && !_disposed;
            _group.interactable = _group.blocksRaycasts = !_closed && !_disposed;
        }

        static bool HasList(Phase phase) => phase == Phase.Ready || phase == Phase.Creating ||
            phase == Phase.Selecting || phase == Phase.Entering;

        static bool SameDisplaySlot(Slot? previous, Slot? next) => previous == null ? next == null :
            next != null && previous.Name == next.Name && previous.ClassId == next.ClassId;

        static string ClassCaption(byte classId) => classId == 0 ? "Knight" : classId == 1 ? "Mage" : "알 수 없는 직업";

        static string PhaseCaption(Phase phase) => phase switch
        {
            Phase.Connecting => "서버에 연결하는 중입니다.",
            Phase.LoadingList => "캐릭터 목록을 불러오는 중입니다.",
            Phase.Ready => "입장할 캐릭터 또는 빈 슬롯을 선택하세요.",
            Phase.Creating => "캐릭터를 만드는 중입니다.",
            Phase.Selecting => "캐릭터 선택을 확인하는 중입니다.",
            Phase.ListFailed => "목록을 확인하지 못했습니다. 새로고침을 눌러주세요.",
            Phase.Entering => "선택이 승인되었습니다. 서버 입장을 기다리고 있습니다.",
            Phase.Disconnected => "연결이 종료되었습니다. 메뉴로 돌아가 다시 접속하세요.",
            _ => "화면 상태를 확인할 수 없습니다. 메뉴로 돌아가주세요.",
        };

        // Slot card: the button's own image is the name plate frame; glow, art and pedestal are children outside the
        // raycast, centred on the card.
        Button MakeSlot(int index, Transform panel, Vector2 position, TMP_FontAsset font)
        {
            const float centre = SlotWidth / 2f;
            Image frame = UiWindowAppearance.CreateSliced("Slot" + (index + 1), panel, _skin.CharacterSlot);
            frame.raycastTarget = true;
            Place(frame.rectTransform, new Vector2(SlotWidth, SlotHeight), position);
            Button button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            // The chosen frame is swapped by the view, so hover and press only tint the frame.
            button.transition = Selectable.Transition.ColorTint;
            _slotFrames[index] = frame;

            _slotGlows[index] = UiWindowAppearance.CreateSimple("Glow", frame.transform, _skin.SelectionGlow);
            Place(_slotGlows[index].rectTransform, new Vector2(72f, 120f), new Vector2(centre - 36f, -4f));
            Image pedestal = UiWindowAppearance.CreateSimple("Pedestal", frame.transform, _skin.Pedestal);
            Place(pedestal.rectTransform, new Vector2(84f, 42f), new Vector2(centre - 42f, -100f));
            _slotArts[index] = UiWindowAppearance.CreateSimple("ClassArt", frame.transform, _skin.KnightArt);
            RectTransform art = _slotArts[index].rectTransform;
            Place(art, new Vector2(88f, 100f), Vector2.zero);
            // Bottom-centre pivot: preserveAspect then keeps the figure's feet on the pedestal top.
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = new Vector2(centre, -118f);
            _slotBobs[index] = CharacterArtBob.Attach(art);

            // The wide card keeps the heading and a 12-character name on one line each, two 15 pt lines in 90 UI;
            // a heading that still wrapped would need three lines of about 1.9 em, which also fit.
            TMP_Text label = Label("Label", frame.transform, new Vector2(SlotWidth - 8f, 90f),
                new Vector2(4f, -138f), font);
            label.fontSize = 15f;
            label.color = s_ink;
            label.alignment = TextAlignmentOptions.Midline;
            _slotLabels[index] = label;
            return button;
        }

        static TMP_InputField CreateNameInput(Transform parent, Vector2 size, Vector2 position, TMP_FontAsset font,
            UiWindowSkin skin)
        {
            Image background = UiWindowAppearance.CreateSliced("Name", parent, skin.InputField);
            Place(background.rectTransform, size, position);
            background.raycastTarget = true;
            // 10 UI of the input frame stays clear on each side of the typed text.
            var textSize = new Vector2(size.x - 20f, size.y);
            RectTransform viewport = Rect("Viewport", background.transform, textSize, new Vector2(10f, 0f));
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text text = Label("Text", viewport, textSize, Vector2.zero, font);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = s_ink;
            TMP_Text placeholder = Label("Placeholder", viewport, textSize, Vector2.zero, font);
            placeholder.text = "캐릭터 이름";
            placeholder.color = s_secondaryText;
            var input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.characterLimit = 0;
            input.richText = false;
            return input;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Place(rect, size, position);
            return rect;
        }

        static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position, TMP_FontAsset font)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 18f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        static Button MakeButton(string name, Transform parent, Vector2 size, Vector2 position,
            string caption, TMP_FontAsset font, out TMP_Text label, UiWindowSkin skin,
            UiWindowAppearance.ButtonStyle style = UiWindowAppearance.ButtonStyle.Default)
        {
            Button button = UiWindowAppearance.CreateButton(name, parent, style, skin);
            Place((RectTransform)button.transform, size, position);
            label = Label("Label", button.transform, size, Vector2.zero, font);
            label.text = caption;
            // Midline centres the drawn glyphs; Center would centre the fallback's tall line box and sit Korean low.
            label.alignment = TextAlignmentOptions.Midline;
            return button;
        }

        // Creation class choice: the class's own art (the same art the slots show) in a column left of the caption.
        // Unlike the slot art it has no chosen or empty state, so it does not share the slot's art helpers.
        void AddClassPicture(Button button, TMP_Text caption, byte classId)
        {
            Image picture = UiWindowAppearance.CreateSimple("ClassPicture", button.transform, ClassArt(classId)!);
            Place(picture.rectTransform, new Vector2(36f, 40f), new Vector2(4f, 0f));
            // Drawn first, under the caption; the caption's left margin keeps its glyphs out of the picture column.
            picture.transform.SetAsFirstSibling();
            caption.margin = new Vector4(42f, 0f, 0f, 0f);
        }
    }
}
