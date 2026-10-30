using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // The root deliberately survives Dispose so delayed Unity destruction cannot be hidden by owner destruction.
    // Reflection follows the existing PlayMode boundary: internal views and uGUI are not exposed to this assembly.
    public sealed class WindowOpeningLifetimeTests
    {
        GameObject _root;
        object _view;
        Transform _panel;
        MonoBehaviour _effect;
        float _oldTimeScale;
        const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _oldTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _root = new GameObject("Opening lifetime observation", typeof(RectTransform));
            Type viewType = typeof(InventoryPanel).Assembly.GetType("Dawnholder.Client.UI.InventoryPanelView", true);
            _view = Activator.CreateInstance(viewType, InstanceMembers, null,
                new object[] { _root, new Action<int>(_ => { }), new Action(() => { }) }, null);
            _panel = _root.transform.Find("Inventory");
            _effect = _panel.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "UiWindowOpenEffect");
            Visibility(false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            (_view as IDisposable)?.Dispose();
            if (_root != null) UnityEngine.Object.Destroy(_root);
            Time.timeScale = _oldTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisposeDuringOpening_RemovesDrawingWithOwnerStillAlive()
        {
            OpenDrawing();
            ((IDisposable)_view).Dispose();
            yield return null;
            ObserveStopped("Dispose");
        }

        [UnityTest]
        public IEnumerator HideDuringOpening_RemovesDrawingAndKeepsClosedInputState()
        {
            OpenDrawing();
            Visibility(false);
            yield return null;
            ObserveStopped("Hide");
            CanvasGroup owner = _root.GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, owner.alpha);
            Assert.IsFalse(owner.interactable);
            Assert.IsFalse(owner.blocksRaycasts);
        }

        [UnityTest]
        public IEnumerator ReopenInSameFrame_LeavesNoOrphanDrawingAfterUnscaledCompletion()
        {
            OpenDrawing();
            Visibility(false);
            OpenDrawing();
            yield return new WaitForSecondsRealtime(0.35f);
            ObserveStopped("same-frame reopen and unscaled completion");
            Assert.AreEqual(1f, _root.GetComponent<CanvasGroup>().alpha);
        }

        void Visibility(bool visible) => _view.GetType().GetMethod("SetVisibility", InstanceMembers)
            .Invoke(_view, new object[] { visible, false });
        void OpenDrawing()
        {
            Visibility(true);
            _effect.GetType().GetMethod("LateUpdate", InstanceMembers).Invoke(_effect, null);
            Assert.IsTrue(_effect.enabled, "actual opening is active before the lifetime event");
            Assert.IsNotNull(_panel.Find("OpeningFrame"), "actual drawing exists before the lifetime event");
        }
        void ObserveStopped(string operation)
        {
            Assert.IsTrue(_root != null && _panel != null, "the owner remains alive for this observation");
            int drawings = _panel.Cast<Transform>().Count(child => child.name == "OpeningFrame");
            float alpha = _panel.GetComponent<CanvasGroup>().alpha;
            var rejection = Assert.Throws<AssertionException>(() => RequireStopped(1, false, 1f));
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + rejection.Message);
            TestContext.Out.WriteLine($"{operation}: timeScale={Time.timeScale}, ownerAlive={_root != null}, drawings={drawings}, effectEnabled={_effect.enabled}, fade={alpha}");
            RequireStopped(drawings, _effect.enabled, alpha);
        }
        static void RequireStopped(int drawings, bool enabled, float alpha)
        {
            Assert.AreEqual(0, drawings, "no effect-owned drawing survives the lifetime event");
            Assert.IsFalse(enabled);
            Assert.AreEqual(1f, alpha);
        }
    }
}
