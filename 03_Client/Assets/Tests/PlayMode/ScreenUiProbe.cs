using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Tests.PlayMode
{
    // What a player sees and what a pointer hits, for product objects that have no agreed type name (the dungeon
    // reward window and entrance label, goal 「미리 정하는 공개 이름」). Tests find them by shown TMP text and uGUI
    // Button only. This assembly does not reference uGUI, so Button and EventSystem are reached by full type name
    // and reflection, as InventoryPanelProbe does for the panel's buttons.
    internal static class ScreenUiProbe
    {
        const string ButtonTypeName = "UnityEngine.UI.Button";

        // Active, enabled, not faded out by a parent CanvasGroup, under enabled canvases and not transparent.
        internal static bool IsShown(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy) return false;
            if (component is Behaviour behaviour && !behaviour.enabled) return false;
            if (component is TMP_Text text && text.color.a <= 0f) return false;
            Renderer renderer = component.GetComponent<Renderer>();
            if (renderer != null && !renderer.enabled) return false;
            if (component.GetComponentsInParent<CanvasGroup>().Any(group => group.alpha <= 0f)) return false;
            return component.GetComponentsInParent<Canvas>().All(canvas => canvas.enabled);
        }

        internal static TMP_Text[] ShownTexts(string phrase) => UnityEngine.Object.FindObjectsByType<TMP_Text>()
            .Where(text => IsShown(text) && text.text != null && text.text.Contains(phrase))
            .ToArray();

        internal static TMP_Text[] ShownTextsUnder(Transform root) =>
            root.GetComponentsInChildren<TMP_Text>().Where(IsShown).ToArray();

        // Shown uGUI buttons whose own label contains the phrase.
        internal static Component[] ShownButtons(string phrase) => ShownTexts(phrase)
            .Select(label => label.GetComponentsInParent<Component>().FirstOrDefault(IsButton))
            .Where(button => button != null && IsShown(button))
            .Distinct()
            .ToArray();

        // The button's onClick, as its click handler calls it.
        internal static void Press(Component button)
        {
            object onClick = button.GetType().GetProperty("onClick").GetValue(button);
            onClick.GetType().GetMethod("Invoke", Type.EmptyTypes).Invoke(onClick, null);
        }

        // Screen pixel of the object's centre, whatever the canvas mode (or none, for a world-space TextMeshPro).
        internal static Vector2 ScreenPoint(Component component)
        {
            Vector3 world = component.transform is RectTransform rect
                ? rect.TransformPoint(rect.rect.center)
                : component.transform.position;
            Canvas canvas = component.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return RectTransformUtility.WorldToScreenPoint(null, world);
            Camera camera = canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
                ? canvas.rootCanvas.worldCamera
                : Camera.main;
            Assert.IsNotNull(camera, "fixture: a camera renders " + component.name);
            return RectTransformUtility.WorldToScreenPoint(camera, world);
        }

        internal static bool OnScreen(Vector2 point) =>
            point.x >= 0f && point.x <= Screen.width && point.y >= 0f && point.y <= Screen.height;

        // The object's own graphic group: its topmost ancestor below the canvas, and everything under it. Outside any
        // canvas (a world-space TextMeshPro) only the object and its children count, so a portal it is attached to is
        // not mistaken for it.
        internal static HashSet<GameObject> GroupBelowCanvas(Component component)
        {
            Transform top = component.transform;
            if (component.GetComponentInParent<Canvas>() != null)
            {
                while (top.parent != null && top.parent.GetComponent<Canvas>() == null) top = top.parent;
            }
            return new HashSet<GameObject>(top.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject));
        }

        // Every object the current EventSystem's raycasters (uGUI graphics and any physics raycaster) hit at a point:
        // the same raycast the gameplay pointer gate uses to decide that a click belongs to the UI (client.md).
        internal static GameObject[] RaycastHits(Vector2 screenPoint)
        {
            Type eventSystemType = FindType("UnityEngine.EventSystems.EventSystem");
            Type pointerType = FindType("UnityEngine.EventSystems.PointerEventData");
            Type resultType = FindType("UnityEngine.EventSystems.RaycastResult");
            object eventSystem = eventSystemType.GetProperty("current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Assert.IsNotNull(eventSystem, "fixture: the gameplay scenes have a current EventSystem");
            object pointer = Activator.CreateInstance(pointerType, eventSystem);
            pointerType.GetProperty("position").SetValue(pointer, screenPoint);
            var results = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType));
            eventSystemType.GetMethod("RaycastAll").Invoke(eventSystem, new[] { pointer, results });
            PropertyInfo hit = resultType.GetProperty("gameObject");
            return results.Cast<object>().Select(result => (GameObject)hit.GetValue(result)).Where(item => item != null).ToArray();
        }

        // The EventSystem lives in the UI scene, which SceneBootstrap loads additively after a gameplay scene and which
        // is not a gameplay readiness barrier; the fixture also unloads it after every test. A pointer check waits for
        // it the way a player's first click would. A UI scene that never loads still fails here, after the timeout.
        internal static IEnumerator WaitForEventSystem(float seconds = 5f)
        {
            PropertyInfo current = FindType("UnityEngine.EventSystems.EventSystem")
                .GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            yield return MapEntryPlayFixture.Wait(() => current.GetValue(null) != null, "the UI scene's EventSystem", seconds);
        }

        // Waits without failing, so a test can still record an observation that never happened.
        internal static IEnumerator WaitUntil(Func<bool> condition, float seconds, Action<bool> done)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            done(condition());
        }

        static bool IsButton(Component component) => component != null && component.GetType().FullName == ButtonTypeName;

        // EventSystems ship in the uGUI package's UnityEngine.UI assembly.
        static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName + ", UnityEngine.UI");
            Assert.IsNotNull(type, "fixture: uGUI type " + fullName);
            return type;
        }
    }
}
