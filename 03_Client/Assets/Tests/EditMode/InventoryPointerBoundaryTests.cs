using System;
using System.Collections.Generic;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.InventoryIconGridTestSupport;

namespace Dawnholder.Client.Tests
{
    // Independent boundary observations. The negative run supplies invalid observations to the same assertions.
    public sealed class InventoryPointerBoundaryTests
    {
        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        InventoryPanelView _view;
        GameObject _root;
        Pointer _pointer;
        Camera _camera;
        RenderTexture _target;

        static bool Negative => Environment.GetEnvironmentVariable("DAWNHOLDER_INVENTORY_OBSERVER_NEGATIVE") == "1";

        [TearDown]
        public void TearDown()
        {
            try
            {
                _view?.Dispose();
                if (_camera != null) _camera.targetTexture = null;
                if (_target != null) _target.Release();
                for (int i = _owned.Count - 1; i >= 0; i--)
                    if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
                _owned.Clear();
            }
            finally
            {
                _pointer?.Dispose();
            }
        }

        void Build(Action<int> use)
        {
            _root = Own(new GameObject("Inventory pointer boundaries", typeof(RectTransform)));
            _view = new InventoryPanelView(_root, use, () => { });
            Canvas.ForceUpdateCanvases();
            _view.Resize();
        }

        [TestCase(1920, 1080, "left")]
        [TestCase(1920, 1080, "right")]
        [TestCase(1920, 1080, "top")]
        [TestCase(1920, 1080, "bottom")]
        [TestCase(1280, 720, "left")]
        [TestCase(1280, 720, "right")]
        [TestCase(1280, 720, "top")]
        [TestCase(1280, 720, "bottom")]
        public void DetailPlacement_ContainsAllCorners_AfterEachScreenEdgeOverflow(int width, int height, string edge)
        {
            Build(_ => { });
            _camera = Own(new GameObject("Boundary camera")).AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.nearClipPlane = .3f;
            _camera.farClipPlane = 1000f;
            _target = Own(new RenderTexture(width, height, 24));
            _target.Create();
            _camera.targetTexture = _target;
            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _camera;
            canvas.planeDistance = 100;
            Canvas.ForceUpdateCanvases();
            SessionTestTools.Call(_root.GetComponent<CanvasScaler>(), "Handle");
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            var area = (RectTransform)_root.transform;
            RectTransform detail = Detail(_root.transform);
            Vector2 screenPoint = new Vector2(width / 2f, height / 2f);
            switch (edge)
            {
                case "left": screenPoint.x = -200f; break;
                case "right": screenPoint.x = width + 200f; break;
                case "top": screenPoint.y = height + 200f; break;
                case "bottom": screenPoint.y = -200f; break;
                default: Assert.Fail("Unknown fixture edge"); break;
            }

            bool placed = InventoryPointerGeometry.PlaceDetail(area, detail, screenPoint, _camera);
            Assert.IsTrue(placed, "The actual camera projection accepted the screen point");
            if (Negative) detail.anchoredPosition += new Vector2(area.rect.width + detail.rect.width, 0f);
            Rect actual = InSpace(area, detail);
            TestContext.Out.WriteLine($"edge={edge} input={screenPoint} canvas={area.rect} detail={actual} negative={Negative}");
            AssertInside(area.rect, actual, "All detail corners stay within the camera canvas");
        }

        [TestCase("hidden")]
        [TestCase("locked")]
        [TestCase("disposed")]
        public void UnavailableView_RejectsRealRightPress_AfterTheSameViewAcceptedOne(string gate)
        {
            _pointer = new Pointer();
            var uses = new List<int>();
            Build(uses.Add);
            GameObject owner = Own(new GameObject("Inventory snapshot fixture"));
            owner.SetActive(false);
            InventoryState mirror = owner.AddComponent<InventoryState>();
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, 2);
            slots[1] = new InventorySlot(ItemId.CoinPouch, 1);
            Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(1, 10, slots));
            _view.Render(mirror, null, true);
            _view.SetVisibility(true, true);
            Canvas.ForceUpdateCanvases();
            _pointer.RightClick(_root.transform, 1);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "The same view accepted a real right press before the gate");
            if (!Negative)
            {
                switch (gate)
                {
                    case "hidden": _view.SetVisibility(false, true); break;
                    case "locked": _view.SetVisibility(true, false); break;
                    case "disposed": _view.Dispose(); break;
                    default: Assert.Fail("Unknown fixture gate"); break;
                }
            }
            _pointer.RightClick(_root.transform, 1);
            TestContext.Out.WriteLine($"gate={gate} mouse={_pointer.Mouse.deviceId} callbacks={uses.Count} negative={Negative}");
            CollectionAssert.AreEqual(new[] { 1 }, uses, "An unavailable view must add no callback");
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "Unavailable detail stays hidden");
        }

        T Own<T>(T value) where T : UnityEngine.Object
        {
            _owned.Add(value);
            return value;
        }
    }
}
