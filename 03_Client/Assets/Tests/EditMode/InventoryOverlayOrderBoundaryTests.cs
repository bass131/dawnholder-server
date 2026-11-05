using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    public sealed class InventoryOverlayOrderBoundaryTests
    {
        GameObject _inventoryRoot;
        GameObject _coverRoot;
        InventoryPanelView _view;
        Canvas _inventoryCanvas;
        Canvas _coverCanvas;
        Image _cover;
        readonly InventoryOverlayOcclusion _occlusion = new InventoryOverlayOcclusion();

        [SetUp]
        public void SetUp()
        {
            _inventoryRoot = new GameObject("Inventory order boundary", typeof(RectTransform));
            _view = new InventoryPanelView(_inventoryRoot, _ => { }, () => { });
            _inventoryCanvas = _inventoryRoot.GetComponent<Canvas>();
            _coverRoot = new GameObject("Overlay boundary fixture", typeof(RectTransform), typeof(Canvas));
            _coverCanvas = _coverRoot.GetComponent<Canvas>();
            _coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var image = new GameObject("Visible cover", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(_coverRoot.transform, false);
            _cover = image.GetComponent<Image>();
            _cover.raycastTarget = false;
            _cover.rectTransform.anchorMin = Vector2.zero;
            _cover.rectTransform.anchorMax = Vector2.one;
            _cover.rectTransform.offsetMin = Vector2.zero;
            _cover.rectTransform.offsetMax = Vector2.zero;
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_coverRoot != null) Object.DestroyImmediate(_coverRoot);
            if (_inventoryRoot != null) Object.DestroyImmediate(_inventoryRoot);
        }

        // Approved ordering: HUD 10, guild 11-13, inventory 14, reward 15, menu 20, dialogue 920.
        // Equal-order ink is excluded by the existing higher-overlay contract.
        [TestCase(10, 0.3f, false)]
        [TestCase(11, 0.3f, false)]
        [TestCase(12, 0.3f, false)]
        [TestCase(13, 0.3f, false)]
        [TestCase(14, 0.3f, false)]
        [TestCase(15, 0.3f, true)]
        [TestCase(20, 0.3f, true)]
        [TestCase(920, 0.3f, true)]
        [TestCase(15, 1f, true)]
        [TestCase(15, 0f, false)]
        public void HigherVisibleWindowsCoverInventory_AndRemovingInkRecovers(int order, float alpha, bool expected)
        {
            SetCover(order, alpha);
            Observe(expected);
            _coverRoot.SetActive(false);
            Canvas.ForceUpdateCanvases();
            Observe(false);
        }

        [Test]
        public void BoundaryObserver_RejectsAnEqualOrderCoverClaim()
        {
            SetCover(14, 0.3f);
            // This intentionally wrong fixture/expectation must fail in the same observer as the positive rows.
            AssertionException rejection = Assert.Throws<AssertionException>(() => Observe(true));
            StringAssert.Contains("actual inventory coverage", rejection.Message);
            Debug.Log("[order boundary negative control] rejected equal-order cover claim: " + rejection.Message);
        }

        void SetCover(int order, float alpha)
        {
            _coverCanvas.sortingOrder = order;
            _cover.color = new Color(1f, 1f, 1f, alpha);
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(RectTransformUtility.RectangleContainsScreenPoint(_cover.rectTransform, Point, null),
                "fixture: overlay covers the observed screen point");
            if (alpha > 0f)
                Assert.IsFalse(_cover.canvasRenderer.cull, "fixture: visible overlay renderer");
        }

        Vector2 Point => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        void Observe(bool expected)
        {
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, _inventoryCanvas.renderMode, "actual inventory render mode");
            Assert.AreEqual(14, _inventoryCanvas.sortingOrder, "actual inventory order");
            bool actual = _occlusion.IsCovered(_inventoryCanvas, Point);
            Debug.Log("[order boundary] inventory=" + _inventoryCanvas.sortingOrder +
                " cover=" + _coverCanvas.sortingOrder + " alpha=" + _cover.color.a +
                " active=" + _coverRoot.activeSelf + " expected=" + expected + " actual=" + actual);
            Assert.AreEqual(expected, actual, "actual inventory coverage");
        }
    }
}
