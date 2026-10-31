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
    // Independent coverage of the create screen's input boundary (A3/A4/A6).
    // Invoke listeners even when a control is hidden or disabled: the view must enforce the same boundary as the UI.
    public sealed class CharacterSelectWideVerifyInteractionTests
    {
        readonly List<(byte Slot, byte ClassId, string Name)> _creations = new();
        readonly List<byte> _selections = new();
        GameObject _root;
        CharacterMenuView _view;
        int _refreshes;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");
        TMP_InputField NameInput => Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();
        bool FormOpen => Panel.Find("CreateForm").gameObject.activeInHierarchy;

        [SetUp]
        public void SetUp()
        {
            _creations.Clear();
            _selections.Clear();
            _refreshes = 0;
            _root = new GameObject("Character select independent input verification", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _selections.Add(slot),
                (slot, classId, name) => _creations.Add((slot, classId, name)), () => _refreshes++, () => { });
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void OpenForm_HiddenSlotAndSubmitListenersCannotChangeItsTargetOrDraft()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("기존기사", 0)));
            Click("Slot2");
            NameInput.text = "  Mage 1 ";
            Click("CreateForm/Mage");

            Click("Slot1");
            Click("Slot3");
            Click("Submit");

            Assert.IsTrue(FormOpen, "A3 the original create screen remains open");
            Assert.AreEqual("  Mage 1 ", NameInput.text, "hidden slot listeners cannot discard the draft");
            Assert.IsEmpty(_selections, "Submit cannot select while creating");
            Assert.IsEmpty(_creations, "hidden controls cannot create");
            Click("CreateForm/Create");
            Assert.AreEqual(1, _creations.Count);
            Assert.AreEqual((byte)2, _creations[0].Slot, "the form remembers the slot that opened it");
            Assert.AreEqual((byte)1, _creations[0].ClassId);
            Assert.AreEqual("  Mage 1 ", _creations[0].Name);
        }

        [Test]
        public void PendingCreate_CancelAndOtherListenersKeepTheDraftAndSendNoSecondRequest()
        {
            OpenMageDraft();
            Click("CreateForm/Create");

            Assert.IsFalse(Control("CreateForm/Cancel").interactable, "A6 cancel locks immediately before a reply");
            Click("CreateForm/Cancel");
            Click("CreateForm/Knight");
            Click("Slot1");
            Click("Refresh");
            Click("Submit");
            Click("CreateForm/Create");

            Assert.IsTrue(FormOpen);
            Assert.AreEqual("검증마법사", NameInput.text);
            Assert.AreEqual(1, _creations.Count);
            Assert.Zero(_refreshes);
            Assert.IsEmpty(_selections);

            _view.Render(new CharacterMenuViewState(Phase.Ready, message: "이름 거절"));
            Click("CreateForm/Create");
            Assert.AreEqual(2, _creations.Count, "a refusal releases the latch for an explicit retry");
            Assert.AreEqual((byte)2, _creations[1].Slot);
            Assert.AreEqual((byte)1, _creations[1].ClassId, "blocked class input did not mutate the draft");
            Assert.AreEqual("검증마법사", _creations[1].Name);
        }

        [TestCase((int)Phase.Creating)]
        [TestCase((int)Phase.Selecting)]
        [TestCase((int)Phase.Entering)]
        public void ServerPendingPhase_CancelCannotDiscardTheOpenForm(int phaseValue)
        {
            OpenMageDraft();
            _view.Render(new CharacterMenuViewState((Phase)phaseValue));
            Assert.IsFalse(Control("CreateForm/Cancel").interactable);
            Click("CreateForm/Cancel");
            Click("CreateForm/Create");

            Assert.IsTrue(FormOpen, "A6 a disabled cancel listener cannot discard the pending screen");
            Assert.AreEqual("검증마법사", NameInput.text);
            Assert.IsEmpty(_creations);
            Assert.IsEmpty(_selections);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalExit_CancelAndLateReadyCannotReopenMutationInputs(bool disconnect)
        {
            OpenMageDraft();
            if (disconnect) _view.Render(new CharacterMenuViewState(Phase.Disconnected));
            else Click("Back");
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("CreateForm/Cancel");
            Click("Slot1");
            Click("CreateForm/Create");
            Click("Submit");

            Assert.IsFalse(FormOpen);
            Assert.IsFalse(Control("CreateForm/Cancel").interactable);
            Assert.IsFalse(Control("CreateForm/Create").interactable);
            Assert.IsFalse(Control("Slot1").interactable);
            Assert.IsEmpty(_creations);
            Assert.IsEmpty(_selections);
        }

        [Test]
        public void RefusalThenCancel_ClearsTheVisibleResultAndRequiresANewSlotChoice()
        {
            OpenMageDraft();
            Click("CreateForm/Create");
            _view.Render(new CharacterMenuViewState(Phase.Ready, message: "이름 거절"));
            Assert.AreEqual("이름 거절", Panel.Find("Result").GetComponent<TMP_Text>().text);
            Click("CreateForm/Cancel");

            Assert.IsFalse(FormOpen);
            Assert.AreEqual(string.Empty, NameInput.text);
            Assert.AreEqual(string.Empty, Panel.Find("Result").GetComponent<TMP_Text>().text,
                "the completed draft's refusal is cleared when leaving its screen");
            Assert.IsFalse(Control("Submit").interactable);
            Click("CreateForm/Create");
            Click("Submit");
            Assert.AreEqual(1, _creations.Count, "hidden Create and unchosen Submit send nothing");
            Assert.IsEmpty(_selections);
        }

        void OpenMageDraft()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot2");
            NameInput.text = "검증마법사";
            Click("CreateForm/Mage");
            Assert.IsTrue(FormOpen, "fixture: an empty slot opened the form");
        }

        Button Control(string path)
        {
            Transform node = Panel.Find(path);
            Assert.IsTrue(node != null, "exists: " + path);
            Button button = node.GetComponent<Button>();
            Assert.IsTrue(button != null, "button: " + path);
            return button;
        }

        void Click(string path) => Control(path).onClick.Invoke();
    }
}
