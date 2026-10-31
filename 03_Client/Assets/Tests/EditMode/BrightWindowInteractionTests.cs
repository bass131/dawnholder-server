using System;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent observations of PR2 contract I5/I6. Expected art is pinned to the approved source GUIDs;
    // placement is sampled after Resize, never reconstructed from the implementation's layout formula.
    public sealed class BrightWindowInteractionTests
    {
        const string KnightGuid = "3746a00054fb99846ba1375428e1d37a";
        const string MageGuid = "3bc44f695dc715e45bf56d950d3a0052";
        GameObject _root;
        IDisposable _view;
        Mouse _mouse;
        Mouse _previousMouse;

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            if (_previousMouse != null && _previousMouse.added) _previousMouse.MakeCurrent();
        }

        [TestCase("Knight", KnightGuid)]
        [TestCase("Mage", MageGuid)]
        public void CreationClassButtons_ShowTheirClassPicture(string buttonName, string expectedGuid)
        {
            _root = new GameObject("Create form observation", typeof(RectTransform));
            var menu = new CharacterMenuView(_root, _ => { }, (_, _, _) => { }, () => { }, () => { });
            _view = menu;
            menu.Render(new CharacterMenuViewState(CharacterMenuViewState.Phase.Ready));
            Transform panel = _root.transform.Find("CharacterMenu/Panel");
            panel.Find("Slot1").GetComponent<Button>().onClick.Invoke();
            Transform button = panel.Find("CreateForm/" + buttonName);
            Assert.IsNotNull(button, "fixture: the existing class choice button");
            Assert.IsTrue(button.gameObject.activeInHierarchy, "fixture: empty slot opens the creation form");
            Image[] pictures = button.GetComponentsInChildren<Image>(false)
                .Where(image => image != button.GetComponent<Image>() && image.enabled && image.color.a > 0f).ToArray();
            string[] actual = pictures.Select(image => GuidOf(image.sprite)).ToArray();
            TestContext.Out.WriteLine($"{buttonName} picture GUIDs=[{string.Join(",", actual)}]");
            NegativeControl(() => RequirePicture(new[] { expectedGuid == KnightGuid ? MageGuid : KnightGuid }, expectedGuid));
            RequirePicture(actual, expectedGuid);
            Assert.IsTrue(pictures.All(image => !image.raycastTarget), "class art must leave input to the existing button");
        }

        [Test]
        public void ActiveOpening_LeavesResizePlacementAndInputLockUnchanged()
        {
            _root = new GameObject("Opening observation", typeof(RectTransform));
            var view = new InventoryPanelView(_root, _ => { }, () => { });
            _view = view;
            var panel = (RectTransform)_root.transform.Find("Inventory");
            view.SetVisibility(false, false);
            view.Resize();
            Vector3 expectedScale = panel.localScale;
            Vector2 expectedPosition = panel.anchoredPosition;
            view.SetVisibility(true, false);
            var effect = panel.GetComponent<UiWindowOpenEffect>();
            Assert.IsTrue(effect.enabled, "precondition: opening was actually started by SetVisibility");
            InvokeLateUpdate(effect);
            var rootGroup = _root.GetComponent<CanvasGroup>();
            Assert.AreEqual(1f, rootGroup.alpha, "opening does not change the owner's visibility decision");
            Assert.IsFalse(rootGroup.interactable, "opening does not unlock input");
            Assert.IsFalse(rootGroup.blocksRaycasts, "opening does not unlock raycasts");
            NegativeControl(() => RequirePlacement(expectedScale + Vector3.right, expectedPosition, expectedScale, expectedPosition));
            TestContext.Out.WriteLine($"active={effect.enabled} resize={expectedScale}/{expectedPosition} afterLateUpdate={panel.localScale}/{panel.anchoredPosition}");
            RequirePlacement(panel.localScale, panel.anchoredPosition, expectedScale, expectedPosition);
        }

        [Test]
        public void HidingDuringOpening_StopsDrawingAndPreservesClosedInputState()
        {
            _root = new GameObject("Closing observation", typeof(RectTransform));
            var view = new InventoryPanelView(_root, _ => { }, () => { });
            _view = view;
            var panel = (RectTransform)_root.transform.Find("Inventory");
            view.SetVisibility(false, false);
            view.Resize();
            Vector3 placed = panel.localScale;
            view.SetVisibility(true, true);
            var effect = panel.GetComponent<UiWindowOpenEffect>();
            InvokeLateUpdate(effect);
            view.SetVisibility(false, false);
            var group = _root.GetComponent<CanvasGroup>();
            NegativeControl(() => RequireClosed(1f, true, true, true));
            RequireClosed(group.alpha, group.interactable, group.blocksRaycasts, effect.enabled);
            Assert.AreEqual(placed, panel.localScale, "closing restores the placement sampled before opening");
        }

        [Test]
        public void ActualMouseHover_ShowsOnlyTheHitSlot_AndRespectsWindowLock()
        {
            _root = new GameObject("Mouse hover observation", typeof(RectTransform));
            var view = new InventoryPanelView(_root, _ => { }, () => { });
            _view = view;
            view.SetVisibility(false, false);
            view.SetVisibility(true, true);
            var panel = (RectTransform)_root.transform.Find("Inventory");
            panel.GetComponent<UiWindowOpenEffect>().Stop();
            view.Resize();
            Canvas.ForceUpdateCanvases();
            var hover = panel.GetComponent<InventorySlotHoverHighlight>();
            Assert.IsNotNull(hover, "precondition: real view's polling component");
            var highlights = Enumerable.Range(1, 8).Select(index => panel.Find("Slot" + index)
                .GetComponentsInChildren<Image>(true).Single(image => image.sprite == UiWindowAppearance.BrightSkin.ItemSlotSelected)).ToArray();
            _previousMouse = Mouse.current;
            _mouse = InputSystem.AddDevice<Mouse>("WindowObservationMouse");
            _mouse.MakeCurrent();
            var row = (RectTransform)panel.Find("Slot1");
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, row.TransformPoint(row.rect.center));
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screenPoint });
            InputSystem.Update();
            InvokeLateUpdate(hover);
            TestContext.Out.WriteLine($"mouse={_mouse.position.ReadValue()} highlights=[{string.Join(",", highlights.Select(image => image.enabled))}]");
            NegativeControl(() => RequireHover(new bool[8], 0));
            RequireHover(highlights.Select(image => image.enabled).ToArray(), 0);
            Assert.IsTrue(highlights.All(image => !image.raycastTarget), "hover art never takes raycasts");
            view.SetVisibility(true, false);
            InvokeLateUpdate(hover);
            RequireHover(highlights.Select(image => image.enabled).ToArray(), -1);
            view.SetVisibility(false, false);
            InvokeLateUpdate(hover);
            RequireHover(highlights.Select(image => image.enabled).ToArray(), -1);
        }

        // UiWindowSkin.Required: an empty slot throws where it is asked for and never falls back to another kit's piece.
        // Observed on an empty skin because PR3 fills these bright slots (main msg_c364b75ed5de A); the bright skin's
        // filled slots are asserted by BrightUiKitImportTests (PR3 T3).
        [TestCase("PortraitPrefab")]
        [TestCase("DialogPanel")]
        [TestCase("EmphasisPanel")]
        [TestCase("DeclineNormal")]
        public void EmptySkin_MissingSlotsThrowInsteadOfFallingBack(string property)
        {
            PropertyInfo getter = typeof(UiWindowSkin).GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(getter, "fixture: requested skin slot exists");
            NegativeControl(() => RequireMissingSlot(() => UiWindowAppearance.BrightSkin.PanelPrefab));
            var emptySkin = ScriptableObject.CreateInstance<UiWindowSkin>();
            try
            {
                RequireMissingSlot(() =>
                {
                    try { return getter.GetValue(emptySkin); }
                    catch (TargetInvocationException error) { throw error.InnerException; }
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptySkin);
            }
        }

        static string GuidOf(Sprite sprite) => sprite == null ? "none" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
        static void InvokeLateUpdate(MonoBehaviour component) => component.GetType()
            .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
        static void RequirePicture(string[] actual, string expected) => CollectionAssert.Contains(actual, expected, "I5: the creation class choice contains its approved class picture");
        static void RequirePlacement(Vector3 scale, Vector2 position, Vector3 expectedScale, Vector2 expectedPosition)
        {
            Assert.AreEqual(expectedScale, scale, "I6: active drawing must not change Resize's scale");
            Assert.AreEqual(expectedPosition, position, "I6: active drawing must not change Resize's position");
        }
        static void RequireClosed(float alpha, bool interactive, bool raycasts, bool effectRunning)
        {
            Assert.AreEqual(0f, alpha);
            Assert.IsFalse(interactive);
            Assert.IsFalse(raycasts);
            Assert.IsFalse(effectRunning);
        }
        static void RequireHover(bool[] actual, int selected)
        {
            Assert.AreEqual(8, actual.Length);
            for (int i = 0; i < actual.Length; i++) Assert.AreEqual(i == selected, actual[i], "hover slot " + (i + 1));
        }
        static void RequireMissingSlot(Func<object> get) => Assert.Throws<InvalidOperationException>(() => get());
        static void NegativeControl(TestDelegate wrongObservation)
        {
            var error = Assert.Throws<AssertionException>(wrongObservation, "the observer must reject a deliberately wrong observation");
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + error.Message);
        }
    }
}
