#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Dawnholder.Client.Input
{
    /// <summary>Current-position uGUI hit testing for the pointer that actually triggered a gameplay action.</summary>
    internal sealed class GameplayPointerInput
    {
        readonly List<RaycastResult> _hits = new List<RaycastResult>();
        EventSystem? _eventSystem;
        PointerEventData? _pointerEvent;

        internal bool IsOverUi(InputControl? control)
        {
            if (control == null || !(control.device is Pointer)) return false;
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            try
            {
                Vector2 position;
                if (control.device is Touchscreen)
                {
                    InputControl? touchControl = control;
                    while (touchControl != null && !(touchControl is TouchControl)) touchControl = touchControl.parent;
                    if (!(touchControl is TouchControl touch)) return false;
                    position = touch.position.ReadValue();
                }
                else position = ((Pointer)control.device).position.ReadValue();

                if (_eventSystem != eventSystem || _pointerEvent == null)
                {
                    _eventSystem = eventSystem;
                    _pointerEvent = new PointerEventData(eventSystem);
                }
                _pointerEvent.Reset();
                _pointerEvent.position = position;
                // Input callbacks precede EventSystem.Update; its cached pointer-over state can be one frame old.
                eventSystem.RaycastAll(_pointerEvent, _hits);
                foreach (RaycastResult hit in _hits)
                {
                    if (hit.module is GraphicRaycaster) return true;
                }
                return false;
            }
            catch (Exception error)
            {
                // An unknown UI result must not turn a known pointer click into an attack/commit.
                Debug.LogException(error);
                return true;
            }
            finally { _hits.Clear(); }
        }
    }
}
