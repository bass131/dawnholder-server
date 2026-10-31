#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Tests
{
    public sealed class PlayerAttackControlIsolationTests
    {
        InputActionAsset? _asset;
        Keyboard? _keyboard;
        Mouse? _mouse;

        [SetUp]
        public void ResolvePlayerControlsOnOwnedDevices()
        {
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.IsNotNull(source, "the production input asset must import");
            // Resolve the imported bindings on isolated devices without enabling or modifying the shared asset.
            _asset = Object.Instantiate(source);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _asset.devices = new InputDevice[] { _keyboard, _mouse };
            _asset.FindActionMap("Player", throwIfNotFound: true).Enable();
        }

        [TearDown]
        public void ReleaseOwnedAssetAndDevices()
        {
            if (_asset != null)
            {
                _asset.Disable();
                Object.DestroyImmediate(_asset);
            }
            if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        }

        [Test]
        public void Attack_ResolvesLeftCtrlAndEnter_WithoutRightCtrlOrMouseControls()
        {
            var controls = _asset!.FindAction("Player/Attack", throwIfNotFound: true).controls.ToArray();

            CollectionAssert.Contains(controls, _keyboard!.leftCtrlKey, "Left Ctrl must resolve to Attack");
            CollectionAssert.Contains(controls, _keyboard.enterKey, "Enter remains an Attack control");
            CollectionAssert.DoesNotContain(controls, _keyboard.rightCtrlKey, "only the left Control key attacks");
            CollectionAssert.DoesNotContain(controls, _mouse!.leftButton, "left mouse no longer attacks");
            CollectionAssert.DoesNotContain(controls, _mouse.rightButton, "right mouse must not attack");
            CollectionAssert.DoesNotContain(controls, _mouse.middleButton, "middle mouse must not attack");
            Assert.IsFalse(controls.Any(control => control.device == _mouse), "no mouse control resolves to Attack");
        }

        [Test]
        public void LeftCtrl_ResolvesOnlyToAttack_InThePlayerMap()
        {
            string[] consumers = _asset!.FindActionMap("Player", throwIfNotFound: true).actions
                .Where(action => action.controls.Contains(_keyboard!.leftCtrlKey))
                .Select(action => action.name)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "Attack" }, consumers,
                "Left Ctrl must not also trigger movement, skills, inventory, or another Player action");
        }
    }
}
