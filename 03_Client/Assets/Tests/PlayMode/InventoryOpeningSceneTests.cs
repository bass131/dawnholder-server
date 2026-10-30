using System.Collections;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // I6's documented unscaled opening policy, through the shipped Town and the real I-key input branch.
    // Timing is a generous completion bound, not a copy of the product's easing equation.
    public sealed class InventoryOpeningSceneTests
    {
        MapEntryPlayFixture _fixture;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_fixture != null) yield return _fixture.Cleanup();
        }

        [UnityTest]
        public IEnumerator RealTown_OpeningFinishesAfterPause_InputStaysLocked_AndIClosesAfterResume()
        {
            _fixture = new MapEntryPlayFixture();
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => InventoryPanel.Instance != null, "Town's real inventory owner");
            Transform panel = InventoryPanelProbe.Root.Find("Inventory");
            var fade = panel.GetComponent<CanvasGroup>();
            var effect = panel.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "UiWindowOpenEffect");
            Assert.IsNotNull(fade, "precondition: product's drawing group exists");
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "precondition: closed before I");
            _fixture.Keys(Key.I);
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Group.alpha == 1f, "I opens before pausing");
            Assert.IsTrue(effect.enabled, "precondition: opening is still running when pause begins");
            _fixture.Keys();
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.35f);
            var rejection = Assert.Throws<AssertionException>(() => RequireFinished(0.5f, true));
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + rejection.Message);
            TestContext.Out.WriteLine($"Town I branch: timeScale={Time.timeScale}; rootAlpha={InventoryPanelProbe.Group.alpha}; drawingAlpha={fade.alpha}; effectEnabled={effect.enabled}");
            RequireFinished(fade.alpha, effect.enabled);
            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha, "the owner's open decision remains intact");
            Assert.IsFalse(InventoryPanelProbe.Group.blocksRaycasts, "pause keeps the existing input lock");
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha, "I while paused cannot bypass the existing input lock");
            Time.timeScale = 1f;
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "I closes immediately after resume");
            Assert.IsFalse(InventoryPanelProbe.Group.blocksRaycasts);
            Assert.IsFalse(effect.enabled);
        }

        static void RequireFinished(float alpha, bool enabled)
        {
            Assert.AreEqual(1f, alpha, "opening reaches full opacity while paused");
            Assert.IsFalse(enabled, "the completed effect stops running");
        }
    }
}
