using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;
using Slot = Dawnholder.Client.UI.CharacterMenuViewState.Slot;

namespace Dawnholder.Client.Tests
{
    // The three-slot character select, written before the implementation (goal 2026-10-31-demo-autopilot-character-select,
    // PR-A completion rows A1, A3, A4 and A5). The tests aim only at the lead's fixed outer contract
    // (pr-a-red-contract-v2 「설계 고정」): no details box under CharacterMenu/Panel; one click on an empty slot opens the
    // direct child CreateForm while the slot row and Submit are hidden; CreateForm/Create creates, CreateForm/Cancel goes
    // back; Submit only selects. Inner layout, sizes and coordinates are the implementer's. View construction and the
    // Panel helpers follow CharacterMenuViewTests; callbacks are only recorded, no client or server answers them.
    public sealed class CharacterSelectWideViewTests
    {
        // 설계 고정 「없앨 것」, goal A1.
        static readonly string[] s_removedNames = { "DetailsBackground", "SelectedTitle", "Details" };
        // 설계 고정 「생성 화면」: direct children of Panel/CreateForm.
        static readonly string[] s_formChildren = { "Name", "NameRule", "Knight", "Mage", "Create", "Cancel" };
        static readonly string[] s_slotNames = { "Slot1", "Slot2", "Slot3" };
        // 설계 고정 「생성 화면」 wording of the two new buttons.
        const string CreateCaption = "캐릭터 생성";
        const string CancelCaption = "취소";
        // 설계 고정 「Submit」 wording.
        const string SubmitCaption = "선택하고 입장";
        // Contract T4: the raw name, leading and trailing spaces kept.
        const string RawName = "  Mage 1 ";

        readonly List<byte> _selections = new();
        readonly List<(byte Slot, byte ClassId, string Name)> _creations = new();
        GameObject _root;
        CharacterMenuView _view;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _selections.Clear();
            _creations.Clear();
            _root = new GameObject("Character select wide view under test", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _selections.Add(slot),
                (slot, classId, name) => _creations.Add((slot, classId, name)), () => { }, () => { });
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        // T1 (A1): once the list is shown there is no details box object anywhere under Panel.
        [Test]
        public void ListShown_HasNoDetailsBox()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            bool panelExists = Panel != null;
            Assert.IsTrue(panelExists, "precondition: CharacterMenu/Panel exists");

            string[] present = RemovedObjectsUnderPanel();

            Assert.IsEmpty(present, "A1 no DetailsBackground, SelectedTitle or Details under Panel once the list is shown");
        }

        // T1 (A1): choosing an occupied slot does not bring the details box back.
        [Test]
        public void ChosenCharacter_HasNoDetailsBox()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot1");

            string[] present = RemovedObjectsUnderPanel();

            Assert.IsEmpty(present, "A1 no DetailsBackground, SelectedTitle or Details under Panel after a character is chosen");
        }

        // T3 (A3): one click on an empty slot in Ready opens Panel/CreateForm with its controls; the slot row and Submit
        // are hidden while it is open.
        [Test]
        public void EmptySlot_OpensTheCreateForm_AndHidesTheSlotRowAndSubmit()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot2");

            Transform form = Panel.Find("CreateForm");
            bool formExists = form != null;
            Assert.IsTrue(formExists, "exists: Panel/CreateForm as a direct child of Panel");
            string[] missingChildren = s_formChildren.Where(name => form.Find(name) == null).ToArray();
            bool formOpen = form.gameObject.activeInHierarchy;
            string[] visibleSlots = s_slotNames.Where(name => Node(name).gameObject.activeInHierarchy).ToArray();
            bool submitVisible = Node("Submit").gameObject.activeInHierarchy;

            Assert.IsEmpty(missingChildren, "exists: CreateForm children Name, NameRule, Knight, Mage, Create, Cancel");
            Assert.IsTrue(formOpen, "A3 one click on an empty slot opens the create form");
            Assert.IsEmpty(visibleSlots, "A3 the slot row is hidden while the create form is open");
            Assert.IsFalse(submitVisible, "설계 고정 Submit is hidden while the create form is open");
        }

        // T3 (A3, 설계 고정 「생성 화면」): the form's controls are a TMP input, the two existing class buttons and two new
        // buttons worded 「캐릭터 생성」 and 「취소」 in the bright Accept and Default looks.
        [Test]
        public void CreateForm_ControlsHaveTheirKindWordingAndBrightLook()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");

            TMP_InputField nameInput = ComponentAt<TMP_InputField>("CreateForm/Name");
            Button knight = ComponentAt<Button>("CreateForm/Knight");
            Button mage = ComponentAt<Button>("CreateForm/Mage");
            Button create = ComponentAt<Button>("CreateForm/Create");
            Button cancel = ComponentAt<Button>("CreateForm/Cancel");
            bool nameIsInput = nameInput != null;
            bool classesAreButtons = knight != null && mage != null;
            bool createExists = create != null;
            bool cancelExists = cancel != null;
            Assert.IsTrue(nameIsInput, "exists: TMP_InputField on CreateForm/Name");
            Assert.IsTrue(classesAreButtons, "exists: Button on CreateForm/Knight and CreateForm/Mage");
            Assert.IsTrue(createExists, "exists: Button on CreateForm/Create");
            Assert.IsTrue(cancelExists, "exists: Button on CreateForm/Cancel");

            string[] createTexts = Captions(create);
            string[] cancelTexts = Captions(cancel);
            List<string> looks = ButtonLookProblems(create, "Accept").Select(problem => "Create: " + problem)
                .Concat(ButtonLookProblems(cancel, "Default").Select(problem => "Cancel: " + problem)).ToList();
            TestContext.Out.WriteLine($"Create texts=[{string.Join("|", createTexts)}] Cancel texts=[{string.Join("|", cancelTexts)}] " +
                string.Join("; ", looks));

            CollectionAssert.Contains(createTexts, CreateCaption, "설계 고정 Create is worded 「캐릭터 생성」");
            CollectionAssert.Contains(cancelTexts, CancelCaption, "설계 고정 Cancel is worded 「취소」");
            Assert.IsEmpty(looks, "설계 고정 Create has the bright Accept look and Cancel the bright Default look");
        }

        // T3 (A3, 설계 고정 「Cancel」): Cancel empties the name, puts the class draft back to Knight (0), drops the slot
        // choice and returns to the slot row without a request. The class draft is observed through the class the next
        // create hands over, a value the player sees take effect.
        [Test]
        public void Cancel_ClearsTheDraftAndReturnsToTheSlotRow_WithoutARequest()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            NameInput().text = "취소할이름";
            Click("CreateForm/Mage");
            Click("CreateForm/Cancel");

            bool formOpen = Node("CreateForm").gameObject.activeInHierarchy;
            string[] hiddenSlots = s_slotNames.Where(name => !Node(name).gameObject.activeInHierarchy).ToArray();
            string nameAfterCancel = NameInput().text;
            bool submitInteractable = Control("Submit").interactable;
            int requestsAfterCancel = _creations.Count + _selections.Count;

            Assert.IsFalse(formOpen, "A3 Cancel closes the create form");
            Assert.IsEmpty(hiddenSlots, "A3 Cancel returns to the slot row");
            Assert.AreEqual(string.Empty, nameAfterCancel, "A3 Cancel empties the name");
            Assert.IsFalse(submitInteractable, "설계 고정 Cancel drops the slot choice, so Submit stays locked");
            Assert.Zero(requestsAfterCancel, "설계 고정 Cancel sends no create or select request");

            Click("Slot1");
            NameInput().text = "다시연기사";
            Click("CreateForm/Create");

            Assert.AreEqual(1, _creations.Count, "precondition: one create after reopening the empty slot");
            Assert.AreEqual((byte)0, _creations[0].ClassId, "A3 Cancel put the class draft back to Knight (0)");
        }

        // T3 (A3, 설계 고정 「빈 칸」): clicking an occupied slot chooses it and leaves the create form closed.
        [Test]
        public void OccupiedSlot_IsChosenWithoutOpeningTheCreateForm()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot1");

            Transform form = Panel.Find("CreateForm");
            bool formOpen = form != null && form.gameObject.activeInHierarchy;
            string[] hiddenSlots = s_slotNames.Where(name => !Node(name).gameObject.activeInHierarchy).ToArray();
            bool submitInteractable = Control("Submit").interactable;

            Assert.IsFalse(formOpen, "A3 an occupied slot does not open the create form");
            Assert.IsEmpty(hiddenSlots, "the slot row stays while the create form is closed");
            Assert.IsTrue(submitInteractable, "설계 고정 the occupied slot is chosen and Submit can be pressed");
        }

        // T4 (A4, 설계 고정 「Create」): Create hands (slot, class, raw name) to the create callback once; a second press
        // before the reply is ignored.
        [Test]
        public void Create_SendsSlotClassAndRawNameOnce()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput().text = RawName;
            Click("CreateForm/Mage");
            Click("CreateForm/Create");
            Click("CreateForm/Create");

            Assert.AreEqual(1, _creations.Count, "A4 two presses before the reply send one create request");
            Assert.AreEqual((byte)2, _creations[0].Slot, "A4 the create names slot 2");
            Assert.AreEqual((byte)1, _creations[0].ClassId, "A4 the create names Mage (1)");
            Assert.AreEqual(RawName, _creations[0].Name, "A4 the raw name goes unchanged, spaces included");
            Assert.IsEmpty(_selections, "A4 creating never selects");
        }

        // T4 (A4, 설계 고정 「생성 성공 뒤」): when Ready arrives with the slot filled, the form closes, the slot row returns
        // and no slot is chosen, so entry needs another explicit choice.
        [Test]
        public void CreateSuccess_ReturnsToTheSlotRowWithNoChoice()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput().text = RawName;
            Click("CreateForm/Mage");
            Click("CreateForm/Create");
            _view.Render(new CharacterMenuViewState(Phase.Creating));
            _view.Render(new CharacterMenuViewState(Phase.Ready, slot2: new Slot("Mage 1", 1),
                message: "슬롯 2: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요."));

            int creates = _creations.Count;
            bool formOpen = Node("CreateForm").gameObject.activeInHierarchy;
            string[] hiddenSlots = s_slotNames.Where(name => !Node(name).gameObject.activeInHierarchy).ToArray();
            bool submitInteractable = Control("Submit").interactable;
            string[] glowing = s_slotNames.Where(Glows).ToArray();

            Assert.AreEqual(1, creates, "precondition: the create request was sent once");
            Assert.IsFalse(formOpen, "A4 the create form closes once the created slot arrives");
            Assert.IsEmpty(hiddenSlots, "A4 the slot row returns after the create");
            Assert.IsFalse(submitInteractable, "A4 no slot is chosen, so Submit stays locked");
            Assert.IsEmpty(glowing, "A4 no slot shows the chosen glow after the create");
            Assert.IsEmpty(_selections, "A4 a create reply never selects");
        }

        // T4 (A4, 설계 고정 「생성 거절」): a refused create comes back as Ready with the slot still empty; the form and the
        // typed name stay, the refusal text shows in Result and the create can be tried again.
        [Test]
        public void CreateRefused_KeepsTheFormAndNameAndShowsTheResult()
        {
            // The view shows the message it is given; this is the wording an earlier view test feeds for a taken name.
            const string refusal = "슬롯 2: 이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.";
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput().text = RawName;
            Click("CreateForm/Create");
            _view.Render(new CharacterMenuViewState(Phase.Creating));
            _view.Render(new CharacterMenuViewState(Phase.Ready, message: refusal));

            int creates = _creations.Count;
            bool formOpen = Node("CreateForm").gameObject.activeInHierarchy;
            string nameAfterRefusal = NameInput().text;
            string result = Label("Result").text;
            bool createInteractable = Control("CreateForm/Create").interactable;

            Assert.AreEqual(1, creates, "precondition: the create request was sent once");
            Assert.IsTrue(formOpen, "설계 고정 a refused create keeps the create form open");
            Assert.AreEqual(RawName, nameAfterRefusal, "설계 고정 a refused create keeps the typed name");
            Assert.AreEqual(refusal, result, "설계 고정 the refusal text shows in Result");
            Assert.IsTrue(createInteractable, "the kept form can send the create again");
        }

        // T5 (A5, 설계 고정 「Submit」): Submit reads 「선택하고 입장」, waits for an occupied slot choice in Ready, and two
        // presses send one select request for that slot.
        [Test]
        public void OccupiedSlot_SubmitSelectsAndEntersOnce()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0), slot3: new Slot("누리", 1)));
            bool submitBeforeChoice = Control("Submit").interactable;
            string[] captionsBeforeChoice = Captions(Control("Submit"));
            Click("Slot3");
            bool submitAfterChoice = Control("Submit").interactable;
            string[] captionsAfterChoice = Captions(Control("Submit"));
            Click("Submit");
            Click("Submit");

            Assert.IsFalse(submitBeforeChoice, "설계 고정 Submit waits for an occupied slot choice");
            Assert.IsTrue(submitAfterChoice, "A5 choosing an occupied slot unlocks Submit");
            CollectionAssert.Contains(captionsBeforeChoice, SubmitCaption, "설계 고정 Submit reads 「선택하고 입장」 before a choice");
            CollectionAssert.Contains(captionsAfterChoice, SubmitCaption, "설계 고정 Submit reads 「선택하고 입장」 after a choice");
            CollectionAssert.AreEqual(new byte[] { 3 }, _selections, "A5 two presses send one select request for slot 3");
            Assert.IsEmpty(_creations, "A5 selecting never creates");
        }

        string[] RemovedObjectsUnderPanel() => Panel.GetComponentsInChildren<Transform>(true)
            .Where(node => s_removedNames.Contains(node.name)).Select(PathUnderPanel).ToArray();

        string PathUnderPanel(Transform node)
        {
            var names = new List<string>();
            for (Transform current = node; current != null && current != Panel; current = current.parent) names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }

        // Selected look as the appearance tests judge it: SelectionGlow drawn on the slot.
        bool Glows(string slot)
        {
            Image glow = ComponentAt<Image>(slot + "/Glow");
            return glow != null && glow.isActiveAndEnabled && glow.color.a > 0f;
        }

        // The bright look as CharacterMenuViewAppearanceTests.ButtonViolations judges it: the approved kit piece names
        // (BrightUiKitTestSupport), never the product's own sprite choice.
        static List<string> ButtonLookProblems(Button button, string family)
        {
            var problems = new List<string>();
            var graphic = button.targetGraphic as Image;
            string normal = graphic != null ? PieceOf(graphic.sprite)?.Name : null;
            if (normal != $"Button_{family}_Normal") problems.Add($"shows {normal ?? "no bright piece"}, approved Button_{family}_Normal");
            if (button.transition != Selectable.Transition.SpriteSwap) problems.Add($"transition {button.transition}, approved SpriteSwap");
            SpriteState states = button.spriteState;
            ExpectPiece(states.highlightedSprite, $"Button_{family}_Highlighted", "highlighted", problems);
            ExpectPiece(states.pressedSprite, $"Button_{family}_Pressed", "pressed", problems);
            ExpectPiece(states.disabledSprite, $"Button_{family}_Disabled", "disabled", problems);
            return problems;
        }

        static void ExpectPiece(Sprite sprite, string expected, string state, List<string> problems)
        {
            string shown = PieceOf(sprite)?.Name;
            if (shown != expected) problems.Add($"{state} state shows {shown ?? "no bright piece"}, approved {expected}");
        }

        static string[] Captions(Button button) => button.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text).ToArray();

        Transform Node(string path)
        {
            Transform node = Panel.Find(path);
            Assert.IsTrue(node != null, $"exists: Panel/{path}");
            return node;
        }

        // Unity's own null check: a missing component is a destroyed-looking object, not a CLR null.
        T ComponentAt<T>(string path) where T : Component
        {
            Transform node = Panel.Find(path);
            if (node == null) return null;
            T component = node.GetComponent<T>();
            return component != null ? component : null;
        }

        Button Control(string path)
        {
            Button button = Node(path).GetComponent<Button>();
            Assert.IsTrue(button != null, $"exists: Button on Panel/{path}");
            return button;
        }

        TMP_Text Label(string path)
        {
            TMP_Text text = Node(path).GetComponent<TMP_Text>();
            Assert.IsTrue(text != null, $"exists: TMP_Text on Panel/{path}");
            return text;
        }

        TMP_InputField NameInput()
        {
            TMP_InputField input = Node("CreateForm/Name").GetComponent<TMP_InputField>();
            Assert.IsTrue(input != null, "exists: TMP_InputField on Panel/CreateForm/Name");
            return input;
        }

        void Click(string path) => Control(path).onClick.Invoke();
    }
}
