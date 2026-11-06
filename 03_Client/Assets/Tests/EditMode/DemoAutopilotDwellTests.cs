#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Demo;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.Tests
{
    public sealed class DemoAutopilotDwellTests
    {
        // Independent requirement: scope-draft-v3 §3/§6, goal F2, approved by msg_1f83e3674ddf.
        // Never read the product's duration to decide when an action is allowed.
        const float RequiredDwell = 2.5f;
        const float VisibleAt = 10f;
        const string CharacterName = "여명기사";
        readonly List<string> _clicks = new();
        readonly List<string> _requests = new();
        readonly List<string> _steps = new();
        GameObject _root = null!;
        CharacterMenuView _view = null!;
        CharacterMenuViewState _state = null!;
        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _clicks.Clear();
            _requests.Clear();
            _steps.Clear();
            _root = new GameObject("Dwell menu", typeof(RectTransform));
            _view = new CharacterMenuView(_root,
                slot =>
                {
                    _requests.Add("select:" + slot);
                    Render(new CharacterMenuViewState(Phase.Selecting));
                },
                (slot, role, name) =>
                {
                    _requests.Add($"create:{slot}:{role}:{name}");
                    Render(new CharacterMenuViewState(Phase.Creating));
                },
                () => _requests.Add("refresh"), () => _requests.Add("back"));
            foreach (string path in new[] { "Slot1", "Slot2", "Slot3", "Submit", "CreateForm/Knight",
                "CreateForm/Mage", "CreateForm/Create" })
            {
                string clickedPath = path;
                Panel.Find(path).GetComponent<Button>().onClick.AddListener(() => _clicks.Add(clickedPath));
            }
            Render(new CharacterMenuViewState(Phase.Ready));
        }

        [TearDown]
        public void TearDown()
        {
            _view.Dispose();
            Object.DestroyImmediate(_root);
        }

        [TestCase(DemoRole.Knight, "Knight")]
        [TestCase(DemoRole.Mage, "Mage")]
        public void EmptyList_WaitsBeforeOpeningAndFillingTheForm(DemoRole role, string classButton)
        {
            DemoCharacterMenuStep step = Step(role);

            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting, step.Advance(_state, Panel, VisibleAt));
            step.Advance(_state, Panel, VisibleAt + RequiredDwell - 0.001f);
            Assert.IsEmpty(_clicks, "F2: no empty slot click before the list has been visible for 2.5s");
            Assert.IsFalse(Panel.Find("CreateForm").gameObject.activeInHierarchy);
            Assert.IsEmpty(_requests);

            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting,
                step.Advance(_state, Panel, VisibleAt + RequiredDwell));
            CollectionAssert.AreEqual(new[] { "Slot1", "CreateForm/" + classButton }, _clicks);
            Assert.IsTrue(Panel.Find("CreateForm").gameObject.activeInHierarchy);
            Assert.AreEqual(CharacterName, Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>().text);
            Assert.IsEmpty(_requests, "opening and filling the form is not a create request");
            CollectionAssert.AreEqual(new[] { "menu empty shown", "menu empty advance", "menu create shown" }, _steps);
        }

        [TestCase(DemoRole.Knight, "Knight", 0)]
        [TestCase(DemoRole.Mage, "Mage", 1)]
        public void FilledCreationForm_WaitsItsOwnDurationThenCreatesOnce(DemoRole role, string classButton, int classId)
        {
            DemoCharacterMenuStep step = Step(role);
            step.Advance(_state, Panel, VisibleAt);
            float formShownAt = VisibleAt + RequiredDwell;
            step.Advance(_state, Panel, formShownAt);

            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting,
                step.Advance(_state, Panel, formShownAt + RequiredDwell - 0.001f));
            CollectionAssert.AreEqual(new[] { "Slot1", "CreateForm/" + classButton }, _clicks);
            Assert.IsEmpty(_requests, "F2: the filled form has a separate 2.5s dwell");

            Assert.AreEqual(DemoCharacterMenuOutcome.Creating,
                step.Advance(_state, Panel, formShownAt + RequiredDwell));
            CollectionAssert.AreEqual(new[] { "Slot1", "CreateForm/" + classButton, "CreateForm/Create" }, _clicks);
            CollectionAssert.AreEqual(new[] { $"create:1:{classId}:{CharacterName}" }, _requests);
            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting,
                step.Advance(_state, Panel, formShownAt + RequiredDwell + 20f));
            Assert.AreEqual(1, _requests.Count, "pending server reply does not repeat creation");
            Assert.AreEqual("menu create advance", _steps[_steps.Count - 1]);
        }

        [TestCase(DemoRole.Knight, 0)]
        [TestCase(DemoRole.Mage, 1)]
        public void ReconnectionList_WaitsBeforeTheSameSlotAndSubmitSequence(DemoRole role, byte classId)
        {
            Render(new CharacterMenuViewState(Phase.Ready,
                slot3: new CharacterMenuViewState.Slot("기존캐릭터", classId)));
            DemoCharacterMenuStep step = Step(role);
            step.Advance(_state, Panel, VisibleAt);
            step.Advance(_state, Panel, VisibleAt + RequiredDwell - 0.001f);
            Assert.IsEmpty(_clicks, "F2: neither occupied slot nor Submit is clicked before 2.5s");
            Assert.IsEmpty(_requests);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting,
                step.Advance(_state, Panel, VisibleAt + RequiredDwell));
            CollectionAssert.AreEqual(new[] { "Slot3", "Submit" }, _clicks);
            CollectionAssert.AreEqual(new[] { "select:3" }, _requests);
            Assert.AreEqual(3, step.SelectedSlot);
            step.Advance(_state, Panel, VisibleAt + 20f);
            Assert.AreEqual(1, _requests.Count);
            CollectionAssert.AreEqual(new[] { "menu occupied shown", "menu occupied advance" }, _steps);
        }

        [Test]
        public void CreationReply_StartsANewListDwellBeforeSelectingTheCreatedCharacter()
        {
            DemoCharacterMenuStep step = Step(DemoRole.Knight);
            step.Advance(_state, Panel, VisibleAt);
            step.Advance(_state, Panel, VisibleAt + RequiredDwell);
            step.Advance(_state, Panel, VisibleAt + RequiredDwell * 2f);
            Render(new CharacterMenuViewState(Phase.Ready,
                new CharacterMenuViewState.Slot(CharacterName, 0)));
            const float replyAt = 30f;
            step.Advance(_state, Panel, replyAt);
            step.Advance(_state, Panel, replyAt + RequiredDwell - 0.001f);
            CollectionAssert.AreEqual(new[] { $"create:1:0:{CharacterName}" }, _requests);

            Assert.AreEqual(DemoCharacterMenuOutcome.Selecting,
                step.Advance(_state, Panel, replyAt + RequiredDwell));
            CollectionAssert.AreEqual(new[] { "Slot1", "CreateForm/Knight", "CreateForm/Create", "Slot1", "Submit" }, _clicks);
            CollectionAssert.AreEqual(new[] { $"create:1:0:{CharacterName}", "select:1" }, _requests);
        }

        DemoCharacterMenuStep Step(DemoRole role) => new(role, CharacterName)
        {
            LogStep = (name, _) => _steps.Add(name)
        };

        void Render(CharacterMenuViewState state)
        {
            _state = state;
            _view.Render(state);
        }
    }
}
