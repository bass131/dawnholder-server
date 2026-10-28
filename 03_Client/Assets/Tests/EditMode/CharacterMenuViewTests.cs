using System.Collections.Generic;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;
using Slot = Dawnholder.Client.UI.CharacterMenuViewState.Slot;

namespace Dawnholder.Client.Tests
{
    public sealed class CharacterMenuViewTests
    {
        readonly List<byte> _selections = new();
        readonly List<(byte Slot, byte ClassId, string Name)> _creations = new();
        GameObject _root;
        CharacterMenuView _view;
        int _refreshes;
        int _backs;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _selections.Clear();
            _creations.Clear();
            _refreshes = _backs = 0;
            _root = new GameObject("Character menu view under test", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _selections.Add(slot),
                (slot, classId, name) => _creations.Add((slot, classId, name)),
                () => _refreshes++, () => _backs++);
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void BeforeListArrives_SlotsAreUnknownAndNoAutomaticListIsRequested()
        {
            Assert.That(Label("Status").text, Does.Contain("연결"));
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(Label($"Slot{i}/Label").text, Does.Contain("목록 확인 전"));
                Assert.That(Label($"Slot{i}/Label").text, Does.Not.Contain("빈 슬롯"));
                Assert.IsFalse(Control($"Slot{i}").interactable);
            }
            Assert.IsFalse(Control("Submit").interactable);
            Assert.IsFalse(Control("Refresh").interactable);
            Assert.Zero(_refreshes, "the facade owns the first list request");
        }

        [Test]
        public void SuccessfulEmptyList_ShowsExactlyThreeEmptySlots()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));

            Assert.AreEqual("슬롯 1\n빈 슬롯", Label("Slot1/Label").text);
            Assert.AreEqual("슬롯 2\n빈 슬롯", Label("Slot2/Label").text);
            Assert.AreEqual("슬롯 3\n빈 슬롯", Label("Slot3/Label").text);
            Assert.IsTrue(Control("Slot3").interactable);
            Assert.IsFalse(Control("Submit").interactable, "an explicit slot choice is required");
        }

        [Test]
        public void OneKnight_ClassZeroIsOccupiedWhileNullIsEmpty()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));

            Assert.That(Label("Slot1/Label").text, Does.Contain("가람").And.Contain("Knight"));
            Assert.That(Label("Slot1/Label").text, Does.Not.Contain("빈 슬롯"));
            Assert.That(Label("Slot2/Label").text, Does.Contain("빈 슬롯"));
            Click("Slot1");
            Assert.IsFalse(Panel.Find("CreateForm").gameObject.activeSelf);
            Assert.That(Label("Submit/Label").text, Does.Contain("선택"));
        }

        [Test]
        public void FullList_PreservesSlotOrderNamesAndBothClasses()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready,
                new Slot("하나", 1), new Slot("둘", 0), new Slot("셋", 1)));

            Assert.That(Label("Slot1/Label").text, Does.Contain("하나").And.Contain("Mage"));
            Assert.That(Label("Slot2/Label").text, Does.Contain("둘").And.Contain("Knight"));
            Assert.That(Label("Slot3/Label").text, Does.Contain("셋").And.Contain("Mage"));
            Click("Slot3");
            Click("Submit");
            CollectionAssert.AreEqual(new byte[] { 3 }, _selections);
            Assert.IsEmpty(_creations);
        }

        [Test]
        public void UnknownClass_IsNeverDisplayedAsKnightOrAsAnEmptySlot()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("확인대상", 9)));

            Assert.That(Label("Slot1/Label").text, Does.Contain("알 수 없는 직업"));
            Assert.That(Label("Slot1/Label").text, Does.Not.Contain("Knight").And.Not.Contain("빈 슬롯"));
        }

        [Test]
        public void FailedList_IsNotPresentedAsAnEmptyAccountAndAllowsManualRefresh()
        {
            _view.Render(new CharacterMenuViewState(Phase.ListFailed, message: "목록을 불러오지 못했습니다."));

            Assert.That(Label("Slot1/Label").text, Does.Contain("확인 실패"));
            Assert.That(Label("Slot2/Label").text, Does.Not.Contain("빈 슬롯"));
            Assert.That(Label("Result").text, Does.Contain("목록을 불러오지 못했습니다."));
            Assert.IsFalse(Control("Slot1").interactable);
            Assert.IsFalse(Control("Submit").interactable);
            Assert.IsTrue(Control("Refresh").interactable);
        }

        [TestCase((int)Phase.LoadingList)]
        [TestCase((int)Phase.Creating)]
        [TestCase((int)Phase.Selecting)]
        [TestCase((int)Phase.Entering)]
        public void PendingOrEntryWait_BlocksAllMutationInputs(int phaseValue)
        {
            var phase = (Phase)phaseValue;
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            _view.Render(new CharacterMenuViewState(phase));

            Assert.IsFalse(Control("Slot2").interactable);
            Assert.IsFalse(Control("Submit").interactable);
            Assert.IsFalse(Control("Refresh").interactable);
            Assert.IsFalse(NameInput().interactable);
            Click("Slot2");
            Click("CreateForm/Mage");
            Click("Submit");
            Click("Refresh");
            Assert.IsEmpty(_creations);
            Assert.IsEmpty(_selections);
            Assert.Zero(_refreshes);
        }

        [Test]
        public void Create_PassesRawNameAndMageClassWithoutTrimmingOrNormalizing()
        {
            const string rawName = "  e\u0301가람  ";
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput().text = rawName;
            Click("CreateForm/Mage");
            Click("Submit");

            Assert.AreEqual(1, _creations.Count);
            Assert.AreEqual((byte)2, _creations[0].Slot);
            Assert.AreEqual((byte)1, _creations[0].ClassId);
            Assert.AreEqual(rawName, _creations[0].Name);
            Assert.IsEmpty(_selections);
        }

        [Test]
        public void EmptySlot_DefaultKnightIsZeroAndRepeatedSubmitIsLatchedImmediately()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            NameInput().text = "기사";
            Click("Submit");
            Click("Submit");
            Click("Refresh");
            Click("Slot3");

            Assert.AreEqual(1, _creations.Count);
            Assert.AreEqual((byte)1, _creations[0].Slot);
            Assert.AreEqual((byte)0, _creations[0].ClassId);
            Assert.Zero(_refreshes);
            Assert.IsFalse(Control("Submit").interactable);
        }

        [Test]
        public void ExistingSelection_DoesNotCreateAndCannotBeSubmittedTwice()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, slot2: new Slot("마법사", 1)));
            Click("Slot2");
            Click("Submit");
            Click("Submit");

            CollectionAssert.AreEqual(new byte[] { 2 }, _selections);
            Assert.IsEmpty(_creations);
        }

        [Test]
        public void CreateCompletion_RequiresAnotherExplicitChoiceBeforeSelection()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            NameInput().text = "새기사";
            Click("Submit");
            _view.Render(new CharacterMenuViewState(Phase.Creating));
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("새기사", 0),
                message: "슬롯 1: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요."));

            Assert.AreEqual(1, _creations.Count);
            Assert.IsEmpty(_selections, "a create reply must never enter the game");
            Assert.IsFalse(Control("Submit").interactable);
            Click("Slot1");
            Click("Submit");
            CollectionAssert.AreEqual(new byte[] { 1 }, _selections);
        }

        [Test]
        public void ChangingSlot_ClearsPreviousNameClassAndResult()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            NameInput().text = "이전이름";
            Click("CreateForm/Mage");
            Click("Submit");
            _view.Render(new CharacterMenuViewState(Phase.Ready, message: "슬롯 1: 이미 사용 중인 이름입니다."));
            Assert.That(Label("Result").text, Does.Contain("슬롯 1"));

            Click("Slot2");

            Assert.AreEqual(string.Empty, NameInput().text);
            Assert.AreEqual(string.Empty, Label("Result").text);
            Click("Submit");
            Assert.AreEqual((byte)2, _creations[1].Slot);
            Assert.AreEqual((byte)0, _creations[1].ClassId);
            Assert.AreEqual(string.Empty, _creations[1].Name);
        }

        [Test]
        public void Back_LocksOnceAndIgnoresLateReadySnapshots()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            Click("Back");
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("늦은응답", 0)));
            Click("Back");
            Click("Submit");
            Click("Refresh");

            Assert.AreEqual(1, _backs);
            Assert.IsEmpty(_creations);
            Assert.IsEmpty(_selections);
            Assert.Zero(_refreshes);
            Assert.IsFalse(Control("Back").interactable);
            Assert.IsFalse(_root.GetComponentInChildren<CanvasGroup>().blocksRaycasts);
        }

        [Test]
        public void Disconnected_BlocksOldReadyStateButKeepsTheMenuExitAvailable()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            _view.Render(new CharacterMenuViewState(Phase.Disconnected));
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Submit");
            Click("Refresh");

            Assert.IsFalse(Control("Submit").interactable);
            Assert.IsFalse(Control("Slot1").interactable);
            Assert.IsFalse(NameInput().interactable);
            Assert.IsEmpty(_creations);
            Assert.Zero(_refreshes);
            Assert.That(Label("Status").text, Does.Contain("연결이 종료"));
            Click("Back");
            Assert.AreEqual(1, _backs);
        }

        [Test]
        public void Dispose_RemovesOwnedCallbacksAndDoesNotDestroyItsCaller()
        {
            var refreshEvent = Control("Refresh").onClick;
            var backEvent = Control("Back").onClick;
            _view.Dispose();
            _view.Dispose();
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            refreshEvent.Invoke();
            backEvent.Invoke();

            Assert.Zero(_refreshes);
            Assert.Zero(_backs);
            Assert.IsTrue(_root != null);
            Assert.IsNull(_root.transform.Find("CharacterMenu"));
        }

        [Test]
        public void ViewUsesExistingSkinAndFontWithoutAddingNamePolicy()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Assert.IsNotNull(font, "fixture: existing runtime font with project Korean fallback");
            Assert.AreEqual("Panel_Default", Panel.GetComponent<Image>().sprite.name);
            Assert.IsNotNull(Panel.Find("Header").GetComponent<Image>().sprite);
            Assert.IsNotNull(Panel.Find("DetailsBackground").GetComponent<Image>().sprite);
            foreach (TMP_Text text in _root.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.AreSame(font, text.font, text.name);
                Assert.IsFalse(text.richText, text.name + " must show user names literally");
            }
            Assert.Zero(NameInput().characterLimit, "the server owns the name length policy");
            Assert.AreEqual(TMP_InputField.CharacterValidation.None, NameInput().characterValidation);
        }

        [Test]
        public void CreateForm_KeepsTheNameRuleUnderTheInputAndStillSendsARuleBreakingName()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput().text = "a";
            _view.Render(new CharacterMenuViewState(Phase.Ready,
                message: "슬롯 2: 이미 쓰는 이름입니다. 다른 이름을 입력해 주세요."));

            TMP_Text rule = Label("CreateForm/NameRule");
            Assert.IsTrue(rule.gameObject.activeInHierarchy, "the rule stays visible after a refused create");
            Assert.AreEqual("2~12자, 한글·영문·숫자. 영문은 대소문자만 다르면 같은 이름", rule.text.Replace('\n', ' '));
            var ruleCorners = new Vector3[4];
            var inputCorners = new Vector3[4];
            rule.rectTransform.GetWorldCorners(ruleCorners);
            ((RectTransform)NameInput().transform).GetWorldCorners(inputCorners);
            Assert.LessOrEqual(ruleCorners[1].y, inputCorners[0].y + 0.001f, "the rule's top is under the input's bottom");
            Click("Submit");
            Assert.AreEqual("a", _creations[0].Name, "the server, not the view, judges the name");
        }

        [Test]
        public void EntryWait_DetailsStopAskingForTheSelectButton()
        {
            var knight = new Slot("가람", 0);
            _view.Render(new CharacterMenuViewState(Phase.Ready, knight));
            Click("Slot1");
            Assert.AreEqual("가람\n직업: Knight\n입장하려면 선택 버튼을 누르세요.", Label("Details").text);
            Click("Submit");
            _view.Render(new CharacterMenuViewState(Phase.Selecting, knight));
            _view.Render(new CharacterMenuViewState(Phase.Entering, knight));

            Assert.AreEqual("가람\n직업: Knight\n이 캐릭터로 입장을 기다리는 중입니다.", Label("Details").text);
            Assert.IsFalse(Control("Submit").interactable);
        }

        Button Control(string path) => Panel.Find(path).GetComponent<Button>();

        TMP_Text Label(string path) => Panel.Find(path).GetComponent<TMP_Text>();

        TMP_InputField NameInput() => Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();

        void Click(string path) => Control(path).onClick.Invoke();
    }
}
