#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.Demo;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.Tests
{
    // Independent contract boundaries absent from the RED set. Real view controls own UI transitions;
    // fixture states are explicit inputs, never a second copy of the step's selection algorithm.
    public sealed class DemoAutopilotVerifyBoundaryTests
    {
        GameObject _root = null!;
        CharacterMenuView _view = null!;
        readonly List<string> _requests = new();
        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        float _testTime;

        // Call adaptation for the screen-dwell contract: cross each delay, preserving every original outcome/packet assertion.
        DemoCharacterMenuOutcome AdvancePastDwell(DemoCharacterMenuStep step, CharacterMenuViewState? state, Transform? panel)
        {
            DemoCharacterMenuOutcome outcome = DemoCharacterMenuOutcome.Waiting;
            for (int frame = 0; frame < 3; frame++)
            {
                outcome = step.Advance(state, panel, _testTime);
                _testTime += 3f;
                if (outcome != DemoCharacterMenuOutcome.Waiting) break;
            }
            return outcome;
        }
        [SetUp]
        public void SetUp()
        {
            _requests.Clear();
            _root = new GameObject("Independent demo boundary", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _requests.Add("select:" + slot),
                (slot, role, name) => _requests.Add($"create:{slot}:{role}:{name}"),
                () => _requests.Add("refresh"), () => _requests.Add("back"));
        }

        [TearDown]
        public void TearDown()
        {
            _view.Dispose();
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [TestCase((int)Phase.Connecting)]
        [TestCase((int)Phase.LoadingList)]
        [TestCase((int)Phase.Creating)]
        [TestCase((int)Phase.Selecting)]
        [TestCase((int)Phase.Entering)]
        public void PendingPhase_DoesNotNeedPanelOrProduceRequest(int phase)
        {
            var step = new DemoCharacterMenuStep(DemoRole.Knight, "VerifyKnight");
            var result = AdvancePastDwell(step, new CharacterMenuViewState((Phase)phase), null);
            Assert.AreEqual(DemoCharacterMenuOutcome.Waiting, result);
            Assert.IsEmpty(step.StopReason);
            Assert.IsEmpty(_requests);
        }

        [TestCase((int)Phase.Disconnected)]
        [TestCase((int)Phase.ListFailed)]
        public void FailedPhase_IsTerminalAndPreservesVisibleReason(int phase)
        {
            var step = new DemoCharacterMenuStep(DemoRole.Mage, "VerifyMage");
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped,
                AdvancePastDwell(step, new CharacterMenuViewState((Phase)phase, message: "visible failure"), Panel));
            StringAssert.Contains("visible failure", step.StopReason);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, AdvancePastDwell(step, null, null));
            Assert.IsEmpty(_requests);
        }

        [Test]
        public void ReadyWithoutPanel_StopsWithItsPath()
        {
            var step = new DemoCharacterMenuStep(DemoRole.Knight, "VerifyKnight");
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped,
                AdvancePastDwell(step, new CharacterMenuViewState(Phase.Ready), null));
            StringAssert.Contains("CharacterMenu/Panel", step.StopReason);
            Assert.IsEmpty(_requests);
        }

        [TestCase("Slot1", "missing")]
        [TestCase("Slot1", "inactive")]
        [TestCase("Slot1", "locked")]
        [TestCase("Submit", "missing")]
        [TestCase("Submit", "inactive")]
        [TestCase("Submit", "locked")]
        public void UnavailableSelectionControl_StopsBeforeRequest(string path, string fault)
        {
            var state = new CharacterMenuViewState(Phase.Ready, new CharacterMenuViewState.Slot("Knight", 0));
            _view.Render(state);
            if (path == "Submit")
                Panel.Find("Slot1").GetComponent<Button>().onClick.AddListener(() => Damage(path, fault));
            else Damage(path, fault);
            var step = new DemoCharacterMenuStep(DemoRole.Knight, null);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, AdvancePastDwell(step, state, Panel));
            StringAssert.Contains(path, step.StopReason);
            Assert.IsEmpty(_requests);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, AdvancePastDwell(step, state, Panel));
        }

        [TestCase("CreateForm/Name", "missing")]
        [TestCase("CreateForm/Name", "inactive")]
        [TestCase("CreateForm/Name", "locked")]
        [TestCase("CreateForm/Knight", "missing")]
        [TestCase("CreateForm/Knight", "inactive")]
        [TestCase("CreateForm/Knight", "locked")]
        [TestCase("CreateForm/Create", "missing")]
        [TestCase("CreateForm/Create", "inactive")]
        [TestCase("CreateForm/Create", "locked")]
        public void UnavailableCreationControl_StopsBeforeRequest(string path, string fault)
        {
            var state = new CharacterMenuViewState(Phase.Ready);
            _view.Render(state);
            // The view unlocks form controls while handling Slot1/Knight, so damage after that real transition.
            string trigger = path == "CreateForm/Create" ? "CreateForm/Knight" : "Slot1";
            Panel.Find(trigger).GetComponent<Button>().onClick.AddListener(() => Damage(path, fault));
            var step = new DemoCharacterMenuStep(DemoRole.Knight, "VerifyKnight");
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, AdvancePastDwell(step, state, Panel));
            StringAssert.Contains(path, step.StopReason);
            Assert.IsEmpty(_requests);
            Assert.AreEqual(DemoCharacterMenuOutcome.Stopped, AdvancePastDwell(step, null, null));
        }

        [Test]
        public void CloseMenu_ClearsLastProjection()
        {
            var controller = _root.AddComponent<CharacterSelectController>();
            var property = typeof(CharacterSelectController).GetProperty("MenuState", BindingFlags.Instance | BindingFlags.NonPublic)!;
            property.SetValue(controller, new CharacterMenuViewState(Phase.Ready));
            typeof(CharacterSelectController).GetMethod("CloseMenu", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(controller, null);
            Assert.IsNull(controller.MenuState);
        }

        [Test]
        public void BootWithoutDemoRole_CreatesNoAutopilotObject()
        {
            Assert.IsNull(DemoLaunchOptions.Parse(Environment.GetCommandLineArgs()), "test runner has no demo role");
            int before = UnityEngine.Object.FindObjectsByType<DemoAutopilot>(FindObjectsSortMode.None).Length;
            typeof(DemoAutopilot).GetMethod("Boot", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
            Assert.AreEqual(before, UnityEngine.Object.FindObjectsByType<DemoAutopilot>(FindObjectsSortMode.None).Length);
        }

        [Test]
        public void KeyboardReleaseAll_ClearsEveryHeldKey()
        {
            Keyboard? previous = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var keys = (DemoKeyboard)Activator.CreateInstance(typeof(DemoKeyboard),
                    BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { keyboard }, null)!;
                keys.Hold(Key.D);
                keys.Hold(Key.F9);
                InputSystem.Update();
                Assert.IsTrue(keyboard.dKey.isPressed && keyboard.f9Key.isPressed, "fixture observes actual key down");
                keys.ReleaseAll();
                InputSystem.Update();
                Assert.IsFalse(keyboard.dKey.isPressed);
                Assert.IsFalse(keyboard.f9Key.isPressed);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                previous?.MakeCurrent();
            }
        }

        void Damage(string path, string fault)
        {
            Transform control = Panel.Find(path);
            // Rename leaves the view's own references valid while making the specified lookup unavailable.
            if (fault == "missing") control.name = "UnavailableControl";
            else if (fault == "inactive") control.gameObject.SetActive(false);
            else control.GetComponent<Selectable>().interactable = false;
        }
    }
}
