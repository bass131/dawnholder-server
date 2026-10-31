#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Tests
{
    // Decision 65 A (01_Phases/goals/2026-10-30-attack-key-left-ctrl/goal.md 「만들 것」): the keyboard and mouse
    // attack keys are Left Ctrl and Enter, the mouse is not an attack binding, and UI clicks stay on the UI map's
    // Click. The product asset is read through Unity's importer; every expected value is the requirement's literal
    // string, never a value read back from the asset.
    public sealed class PlayerAttackBindingTests
    {
        const string AssetPath = "Assets/InputSystem_Actions.inputactions";
        const string KeyboardAndMouseGroup = "Keyboard&Mouse";

        InputActionAsset _asset = null!;

        [SetUp]
        public void LoadAsset()
        {
            InputActionAsset? asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.IsNotNull(asset, "fixture: " + AssetPath + " imports as an InputActionAsset");
            _asset = asset!;
        }

        [Test]
        public void PlayerAttack_KeyboardAndMouseBindings_AreExactlyLeftCtrlAndEnter()
        {
            string[] paths = Bindings("Player/Attack")
                .Where(binding => InGroup(binding, KeyboardAndMouseGroup))
                .Select(binding => binding.path)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "<Keyboard>/leftCtrl", "<Keyboard>/enter" }, paths,
                "65 A the Keyboard&Mouse attack bindings are Left Ctrl and Enter only");
        }

        [Test]
        public void PlayerAttack_HasNoMouseBinding()
        {
            string[] mousePaths = Bindings("Player/Attack")
                .Select(binding => binding.path)
                .Where(path => path.StartsWith("<Mouse>/", StringComparison.Ordinal))
                .ToArray();

            CollectionAssert.IsEmpty(mousePaths, "65 A no Player/Attack binding is on the mouse");
        }

        // The changed binding keeps its id so nothing that names the binding needs a new value.
        [Test]
        public void PlayerAttack_ChangedBinding_KeepsItsIdAndGroupOnLeftCtrl()
        {
            InputBinding[] matches = Bindings("Player/Attack")
                .Where(binding => binding.id == new Guid("05f6913d-c316-48b2-a6bb-e225f14c7960"))
                .ToArray();

            Assert.AreEqual(1, matches.Length, "65 A the binding id 05f6913d-c316-48b2-a6bb-e225f14c7960 stays on Player/Attack");
            Assert.AreEqual("<Keyboard>/leftCtrl", matches[0].path, "65 A that binding is Left Ctrl");
            Assert.AreEqual(";Keyboard&Mouse", matches[0].groups, "65 A that binding keeps its Keyboard&Mouse group");
        }

        [Test]
        public void PlayerAttack_GamepadTouchJoystickAndXrBindings_Stay()
        {
            string[] paths = Bindings("Player/Attack").Select(binding => binding.path).ToArray();

            CollectionAssert.IsSubsetOf(
                new[] { "<Gamepad>/buttonWest", "<Touchscreen>/primaryTouch/tap", "<Joystick>/trigger", "<XRController>/{PrimaryAction}" },
                paths,
                "65 A the other device attack bindings stay");
        }

        [Test]
        public void UiClick_StaysOnTheMouseLeftButton()
        {
            string[] paths = Bindings("UI/Click").Select(binding => binding.path).ToArray();

            CollectionAssert.Contains(paths, "<Mouse>/leftButton", "65 A UI clicks stay on the mouse left button");
        }

        static bool InGroup(InputBinding binding, string group) =>
            binding.groups != null && binding.groups.Split(InputBinding.Separator).Contains(group);

        InputBinding[] Bindings(string actionPath) =>
            _asset.FindAction(actionPath, throwIfNotFound: true).bindings.ToArray();
    }
}
