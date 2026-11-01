#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Dawnholder.Client.Demo
{
    // Virtual keyboard for the demo run. Game code keeps reading PlayerInput actions and Keyboard.current,
    // so the autopilot presses keys exactly where a player would and no gameplay script needs a demo branch.
    sealed class DemoKeyboard
    {
        readonly Keyboard _keyboard;
        readonly HashSet<Key> _held = new HashSet<Key>();

        DemoKeyboard(Keyboard keyboard)
        {
            _keyboard = keyboard;
        }

        // Two players run side by side, so only one window can have focus; IgnoreFocus keeps the
        // unfocused window reading this device. Real keyboards are disabled so they never become current.
        internal static DemoKeyboard Install()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices.ToArray())
            {
                if (device is Keyboard) InputSystem.DisableDevice(device);
            }
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("DemoKeyboard");
            keyboard.MakeCurrent();
            return new DemoKeyboard(keyboard);
        }

        internal void Hold(Key key)
        {
            if (_held.Add(key)) Push();
        }

        internal void Release(Key key)
        {
            if (_held.Remove(key)) Push();
        }

        internal void ReleaseAll()
        {
            if (_held.Count == 0) return;
            _held.Clear();
            Push();
        }

        // Down for two frames, then up: wasPressedThisFrame readers see exactly one edge.
        internal IEnumerator Tap(Key key)
        {
            Hold(key);
            yield return null;
            yield return null;
            Release(key);
            yield return null;
        }

        void Push() => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(_held.ToArray()));
    }
}
