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
    internal sealed class CharacterMenuView : IDisposable
    {
        // The goal's approved wording of the server's name rule. The server judges names; the view only states it.
        const string NameRule = "2~12자, 한글·영문·숫자.\n영문은 대소문자만 다르면 같은 이름";
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
        readonly TMP_Text _selectedTitle;
        readonly TMP_Text _details;
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
        readonly Button _submit;
        readonly TMP_Text _submitLabel;
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
                new Vector2(720f, 46f), new Vector2(20f, -8f));
            TMP_Text title = Label("Title", panel, new Vector2(696f, 46f), new Vector2(32f, -8f), font);
            title.text = "캐릭터 선택";
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            _status = Label("Status", panel, new Vector2(720f, 38f), new Vector2(20f, -58f), font);
            _status.color = s_ink;

            // The three slots stand side by side left of the details: glow, standing art on its pedestal, name plate.
            for (byte slot = 1; slot <= 3; slot++)
            {
                byte chosenSlot = slot;
                _slotButtons[slot - 1] = MakeSlot(slot - 1, panel, new Vector2(20f + (slot - 1) * 110f, -100f), font);
                _slotButtons[slot - 1].onClick.AddListener(() => OnSlotClicked(chosenSlot));
            }

            Place(UiWindowAppearance.CreateInset("DetailsBackground", panel, _skin).rectTransform,
                new Vector2(392f, 232f), new Vector2(348f, -100f));
            _selectedTitle = Label("SelectedTitle", panel, new Vector2(368f, 38f), new Vector2(360f, -104f), font);
            _selectedTitle.fontStyle = FontStyles.Bold;
            _selectedTitle.color = s_ink;
            _details = Label("Details", panel, new Vector2(368f, 182f), new Vector2(360f, -146f), font);
            _details.color = s_ink;
            RectTransform form = Rect("CreateForm", panel, new Vector2(368f, 184f), new Vector2(360f, -146f));
            _form = form.gameObject;
            TMP_Text nameLabel = Label("NameLabel", form, new Vector2(48f, 40f), Vector2.zero, font);
            nameLabel.text = "이름";
            nameLabel.color = s_ink;
            _nameInput = CreateNameInput(form, font, _skin);
            TMP_Text nameRule = Label("NameRule", form, new Vector2(368f, 66f), new Vector2(0f, -42f), font);
            nameRule.text = NameRule;
            nameRule.fontSize = 16f;
            nameRule.color = s_secondaryText;
            _knight = MakeButton("Knight", form, new Vector2(178f, 40f), new Vector2(0f, -110f),
                "Knight", font, out _knightLabel, _skin);
            _mage = MakeButton("Mage", form, new Vector2(178f, 40f), new Vector2(190f, -110f),
                "Mage", font, out _mageLabel, _skin);
            AddClassPicture(_knight, _knightLabel, 0);
            AddClassPicture(_mage, _mageLabel, 1);
            _knight.onClick.AddListener(() => OnClassClicked(0));
            _mage.onClick.AddListener(() => OnClassClicked(1));
            TMP_Text hint = Label("Hint", form, new Vector2(368f, 32f), new Vector2(0f, -152f), font);
            hint.text = "생성 후 입장할 캐릭터를 다시 선택해주세요.";
            hint.fontSize = 15f;
            hint.color = s_secondaryText;

            _result = Label("Result", panel, new Vector2(720f, 38f), new Vector2(20f, -336f), font);
            _result.color = s_resultText;
            _back = MakeButton("Back", panel, new Vector2(134f, 40f), new Vector2(20f, -378f),
                "뒤로", font, out _, _skin);
            _refresh = MakeButton("Refresh", panel, new Vector2(146f, 40f), new Vector2(164f, -378f),
                "목록 새로고침", font, out _, _skin);
            _submit = MakeButton("Submit", panel, new Vector2(214f, 40f), new Vector2(526f, -378f),
                "캐릭터 선택", font, out _submitLabel, _skin, UiWindowAppearance.ButtonStyle.Accept);
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
            if (!HasList(state.CurrentPhase) ||
                (_selectedSlot != 0 && !SameDisplaySlot(_state.GetSlot(_selectedSlot), state.GetSlot(_selectedSlot))))
                ResetSelection();
            _state = state;
            _localPending = false;
            _disconnected = state.CurrentPhase == Phase.Disconnected;
            _status.text = PhaseCaption(state.CurrentPhase);
            _result.text = state.Message;
            UpdateSlots();
            UpdateDetails();
            UpdateInteraction();
        }

        bool CanEdit() => !_disposed && !_closed && !_disconnected && !_localPending && _state.CurrentPhase == Phase.Ready;

        void OnSlotClicked(byte slot)
        {
            if (!CanEdit() || slot < 1 || slot > 3 || slot == _selectedSlot) return;
            _selectedSlot = slot;
            ResetDraft();
            _result.text = string.Empty;
            UpdateSlots();
            UpdateDetails();
            UpdateInteraction();
        }

        void OnClassClicked(byte classId)
        {
            if (!CanEdit() || _selectedSlot == 0 || _state.GetSlot(_selectedSlot) != null) return;
            _draftClass = classId;
            _result.text = string.Empty;
            UpdateInteraction();
        }

        void OnSubmitClicked()
        {
            if (!CanEdit() || _selectedSlot == 0) return;
            byte slot = _selectedSlot;
            bool creating = _state.GetSlot(slot) == null;
            // Capture the raw draft before a synchronous caller can render a replacement snapshot.
            string rawName = _nameInput.text;
            byte classId = _draftClass;
            _localPending = true;
            _result.text = string.Empty;
            _status.text = creating ? "캐릭터 생성 요청 중입니다." : "캐릭터 선택 요청 중입니다.";
            UpdateInteraction();
            if (creating) _createRequested?.Invoke(slot, classId, rawName);
            else _selectRequested?.Invoke(slot);
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
            UpdateInteraction();
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

        void UpdateDetails()
        {
            bool selected = _selectedSlot != 0;
            Slot? character = selected ? _state.GetSlot(_selectedSlot) : null;
            bool creating = selected && character == null;
            _form.SetActive(creating);
            _details.gameObject.SetActive(!creating);
            _selectedTitle.text = selected ? $"슬롯 {_selectedSlot} · {(creating ? "새 캐릭터" : "선택")}" : "캐릭터를 선택하세요";
            _details.text = character == null ? "기존 캐릭터를 고르거나\n빈 슬롯에 새 캐릭터를 만드세요." :
                character.Name + "\n직업: " + ClassCaption(character.ClassId) + (_state.CurrentPhase == Phase.Entering
                    ? "\n이 캐릭터로 입장을 기다리는 중입니다." : "\n입장하려면 선택 버튼을 누르세요.");
            _submitLabel.text = creating ? "캐릭터 생성" : "선택하고 입장";
        }

        void UpdateInteraction()
        {
            bool editing = CanEdit();
            foreach (Button button in _slotButtons) button.interactable = editing;
            bool creating = editing && _selectedSlot != 0 && _state.GetSlot(_selectedSlot) == null;
            _nameInput.interactable = _knight.interactable = _mage.interactable = creating;
            _knightLabel.text = _draftClass == 0 ? "Knight · 선택" : "Knight";
            _mageLabel.text = _draftClass == 1 ? "Mage · 선택" : "Mage";
            _submit.interactable = editing && _selectedSlot != 0;
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

        // Slot card: the button's own image is the name plate frame; glow, art and pedestal are children outside the raycast.
        Button MakeSlot(int index, Transform panel, Vector2 position, TMP_FontAsset font)
        {
            Image frame = UiWindowAppearance.CreateSliced("Slot" + (index + 1), panel, _skin.CharacterSlot);
            frame.raycastTarget = true;
            Place(frame.rectTransform, new Vector2(100f, 232f), position);
            Button button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            // The chosen frame is swapped by the view, so hover and press only tint the frame.
            button.transition = Selectable.Transition.ColorTint;
            _slotFrames[index] = frame;

            _slotGlows[index] = UiWindowAppearance.CreateSimple("Glow", frame.transform, _skin.SelectionGlow);
            Place(_slotGlows[index].rectTransform, new Vector2(72f, 120f), new Vector2(14f, -4f));
            Image pedestal = UiWindowAppearance.CreateSimple("Pedestal", frame.transform, _skin.Pedestal);
            Place(pedestal.rectTransform, new Vector2(84f, 42f), new Vector2(8f, -100f));
            _slotArts[index] = UiWindowAppearance.CreateSimple("ClassArt", frame.transform, _skin.KnightArt);
            RectTransform art = _slotArts[index].rectTransform;
            Place(art, new Vector2(88f, 100f), Vector2.zero);
            // Bottom-centre pivot: preserveAspect then keeps the figure's feet on the pedestal top.
            art.pivot = new Vector2(0.5f, 0f);
            art.anchoredPosition = new Vector2(50f, -118f);
            _slotBobs[index] = CharacterArtBob.Attach(art);

            // Korean fallback line box is about 1.9 em: three 13 pt lines (the heading may wrap once) fit in 90 UI.
            TMP_Text label = Label("Label", frame.transform, new Vector2(92f, 90f), new Vector2(4f, -138f), font);
            label.fontSize = 13f;
            label.color = s_ink;
            label.alignment = TextAlignmentOptions.Midline;
            _slotLabels[index] = label;
            return button;
        }

        static TMP_InputField CreateNameInput(Transform parent, TMP_FontAsset font, UiWindowSkin skin)
        {
            Image background = UiWindowAppearance.CreateSliced("Name", parent, skin.InputField);
            Place(background.rectTransform, new Vector2(312f, 40f), new Vector2(56f, 0f));
            background.raycastTarget = true;
            RectTransform viewport = Rect("Viewport", background.transform, new Vector2(292f, 40f), new Vector2(10f, 0f));
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text text = Label("Text", viewport, new Vector2(292f, 40f), Vector2.zero, font);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = s_ink;
            TMP_Text placeholder = Label("Placeholder", viewport, new Vector2(292f, 40f), Vector2.zero, font);
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
