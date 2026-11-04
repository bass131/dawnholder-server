using System;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.InventoryIconGridTestSupport;

namespace Dawnholder.Client.Tests
{
    // Goal 2026-11-01-inventory-icon-grid T1–T5. Expectations are the lead's literal rectangle table and catalog
    // values. The old implementation should fail on grid geometry, Count, ItemDetail and InventorySlotPointer.
    public sealed class InventoryIconGridTests
    {
        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        readonly List<InventoryPanelView> _views = new List<InventoryPanelView>();
        Pointer _pointer;
        GameObject _root;
        InventoryPanelView _view;
        RectTransform Panel => Required(_root.transform, "Inventory");

        [SetUp]
        public void SetUp() => _pointer = new Pointer();

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (InventoryPanelView view in _views) view.Dispose();
                for (int i = _owned.Count - 1; i >= 0; i--)
                {
                    if (_owned[i] is RenderTexture target) target.Release();
                    if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
                }
            }
            finally { _pointer?.Dispose(); _views.Clear(); _owned.Clear(); }
        }

        void Build(Action<int> use = null)
        {
            Assert.That(InventoryLimits.MaxSlots, Is.InRange(3, 24), "fixture: three observable slots, table supports 24");
            _root = Own(new GameObject("Icon grid under test", typeof(RectTransform)));
            _view = new InventoryPanelView(_root, use ?? (_ => { }), () => { });
            _views.Add(_view);
            Canvas.ForceUpdateCanvases();
            _view.Resize();
        }

        void Render(int material = 2, int pouch = 1)
        {
            GameObject owner = Own(new GameObject("Grid mirror fixture"));
            owner.SetActive(false);
            InventoryState mirror = owner.AddComponent<InventoryState>();
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, material);
            slots[1] = new InventorySlot(ItemId.CoinPouch, pouch);
            Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(1, 120, slots), "fixture: valid compact unique snapshot");
            _view.Render(mirror, null, true);
            _view.SetVisibility(true, true);
            Canvas.ForceUpdateCanvases();
        }

        [Test]
        public void T1_ExactlyTheSharedNumberOfSlotsExists()
        {
            Build();
            Transform[] slots = Panel.Cast<Transform>().Where(t => t.name.StartsWith("Slot", StringComparison.Ordinal)).ToArray();
            CollectionAssert.AreEquivalent(Enumerable.Range(1, InventoryLimits.MaxSlots).Select(n => "Slot" + n).ToArray(), slots.Select(s => s.name).ToArray());
            Assert.IsNull(Panel.Find("Slot" + (InventoryLimits.MaxSlots + 1)), "no slot beyond the shared limit");
        }

        [Test]
        public void T1_FourColumnSquaresAndWindowHeight_MatchTheApprovedTable()
        {
            Build();
            int heightIndex = HeightIndex;
            Assert.AreEqual(new Vector2(390, Heights[heightIndex]), Panel.rect.size, "T1 approved panel size");
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
                AssertRect(SlotRects[i], FromTopLeft(Panel, Slot(_root.transform, i)), "T1 Slot" + (i + 1));
            Assert.AreEqual(Bottoms[heightIndex], Enumerable.Range(0, InventoryLimits.MaxSlots)
                .Max(i => FromTopLeft(Panel, Slot(_root.transform, i)).yMax), 0.1f, "T1 final row bottom");
        }

        [Test]
        public void T1_FooterObjects_KeepTheirDistancesFromTheWindowBottom()
        {
            Build();
            var expected = new[]
            {
                (name: "Divider", x: 20f, w: 350f, h: 6f, bottom: 112f),
                (name: "Coin", x: 22f, w: 26f, h: 26f, bottom: 95f),
                (name: "Currency", x: 54f, w: 324f, h: 36f, bottom: 100f),
                (name: "UseResult", x: 12f, w: 366f, h: 44f, bottom: 56f),
            };
            foreach (var e in expected)
            {
                Rect rect = FromTopLeft(Panel, Required(Panel, e.name));
                AssertRect(new Rect(e.x, Heights[HeightIndex] - e.bottom, e.w, e.h), rect, "T1 " + e.name);
                Assert.AreEqual(e.bottom, Panel.rect.height - rect.yMin, 0.1f, "T1 bottom distance " + e.name);
            }
        }

        [Test]
        public void T2_SlotsContainOnlyHoverIconBaseIconAndCount()
        {
            Build();
            Render(3);
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                RectTransform slot = Slot(_root.transform, i);
                CollectionAssert.AreEqual(new[] { "Hover", "IconBase", "Icon", "Count" }, slot.Cast<Transform>().Select(t => t.name).ToArray(), "T2 exact children in drawing order");
                Assert.IsNull(slot.Find("Item"));
                Assert.IsNull(slot.Find("Use"));
                Assert.IsEmpty(slot.GetComponentsInChildren<Button>(true), "T2 slots have no buttons");
                CollectionAssert.AreEqual(new[] { "Count" }, slot.GetComponentsInChildren<TMP_Text>(true).Select(t => t.name).ToArray(), "T2 only the numeric label");
            }
        }

        [Test]
        public void T2_ValidSnapshots_ShowCountsThreeAndNinetyNine_AndHideOneAndEmptyCounts()
        {
            Build();
            // msg_c0266d2f2bbf: repeated item IDs are illegal, so 3 and 99 are successive valid snapshots.
            GameObject owner = Own(new GameObject("Successive count fixture"));
            owner.SetActive(false);
            InventoryState mirror = owner.AddComponent<InventoryState>();
            uint revision = 0;
            foreach (int count in new[] { 3, 99 })
            {
                var slots = new InventorySlot[InventoryLimits.MaxSlots];
                slots[0] = new InventorySlot(ItemId.Material, count);
                slots[1] = new InventorySlot(ItemId.CoinPouch, 1);
                Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(++revision, 120, slots));
                _view.Render(mirror, null, true);
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < InventoryLimits.MaxSlots; i++) AssertSlotContents(i, slots[i]);
            }
        }

        void AssertSlotContents(int index, InventorySlot expected)
        {
            RectTransform slot = Slot(_root.transform, index);
            Image iconBase = Required(slot, "IconBase").GetComponent<Image>();
            Image icon = Required(slot, "Icon").GetComponent<Image>();
            TMP_Text count = Required(slot, "Count").GetComponent<TMP_Text>();
            Image[] itemIcons = slot.GetComponentsInChildren<Image>(true).Where(image => Visible(image) &&
                (BrightUiKitTestSupport.IsPiece(image.sprite, "Icon_Material") || BrightUiKitTestSupport.IsPiece(image.sprite, "Icon_CurrencyPouch"))).ToArray();
            if (expected.ItemId == ItemId.None)
            {
                Assert.IsEmpty(itemIcons, "T2 empty slot has no drawn item icon");
                Assert.IsFalse(Visible(iconBase), "T2 empty icon base off");
                Assert.IsFalse(count.gameObject.activeSelf, "T2 empty count off");
                return;
            }
            Assert.AreEqual(1, itemIcons.Length, "T2 exactly one drawn item icon");
            Assert.IsTrue(BrightUiKitTestSupport.IsPiece(icon.sprite, expected.ItemId == ItemId.Material ? "Icon_Material" : "Icon_CurrencyPouch"));
            AssertInside(slot.rect, InSpace(slot, icon.rectTransform), "T2 icon inside slot");
            Assert.That(Vector2.Distance(slot.rect.center, InSpace(slot, icon.rectTransform).center), Is.LessThanOrEqualTo(0.1f), "T2 icon centered");
            Assert.IsTrue(Visible(iconBase), "T2 occupied icon base on");
            AssertInside(slot.rect, InSpace(slot, iconBase.rectTransform), "T2 base inside slot");
            Assert.AreEqual(expected.Count >= 2, count.gameObject.activeSelf, "T2 count only for at least two");
            if (expected.Count < 2) return;
            Assert.AreEqual(expected.Count.ToString(), count.text, "T2 digits only");
            Rect countRect = InSpace(slot, count.rectTransform);
            AssertInside(slot.rect, countRect, "T2 count inside slot");
            Assert.Greater(countRect.center.x, slot.rect.center.x, "T2 count right of center");
            Assert.Less(countRect.center.y, slot.rect.center.y, "T2 count below center");
            count.ForceMeshUpdate();
            Assert.IsFalse(count.isTextOverflowing, "T2 full count fits");
            Assert.AreEqual(count.text.Length, count.textInfo.characterCount, "T2 every digit laid out");
            Assert.IsTrue(count.textInfo.characterInfo.Take(count.textInfo.characterCount).All(c => c.isVisible), "T2 every digit draws a glyph");
        }

        [Test]
        public void T3_DetailIsHiddenAfterBuildAndRender_AndUsesBrightNonBlockingGraphics()
        {
            Build();
            RectTransform detail = Detail(_root.transform);
            Assert.AreSame(_root.transform, detail.parent);
            Assert.Greater(detail.GetSiblingIndex(), Panel.GetSiblingIndex());
            Assert.IsFalse(detail.gameObject.activeSelf, "T3 starts hidden");
            Render();
            Assert.IsFalse(detail.gameObject.activeSelf, "T3 Render leaves detail hidden");
            foreach (string name in new[] { "Name", "Count", "Effect", "UseHint" })
            {
                TMP_Text label = DetailLabel(_root.transform, name);
                Assert.IsNotNull(label, "T3 detail label " + name);
                Assert.IsTrue(IsInk(label.color, new Color(.26f, .17f, .09f)) || IsInk(label.color, new Color(.45f, .33f, .2f)), "T3 approved ink: " + name);
            }
            Assert.That(detail.GetComponentsInChildren<Graphic>(true).Select(g => g.raycastTarget), Is.All.EqualTo(false), "T3 detail never takes raycasts");
            Assert.IsTrue(detail.GetComponentsInChildren<Image>(true).Any(image =>
            {
                BrightUiKitTestSupport.BrightPiece piece = BrightUiKitTestSupport.PieceOf(image.sprite);
                return piece != null && (piece.Kind == "Panel" || piece.Kind == "Inset");
            }), "T3 a bright Panel/Inset found by source GUID");
        }

        [Test]
        public void T4_HoverShowsCatalogDetails_AndHidesOutsideEmptyLockedAndClosedSlots()
        {
            Build(); Render();
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 0));
            AssertDetail("재료", "수량 2", false);
            Assert.AreEqual(50, ItemCatalog.For(ItemId.CoinPouch).CurrencyOnUse, "fixture: catalog effect");
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 1));
            AssertDetail("재화 주머니", "수량 1", true);
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 2));
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "T4 empty slot");
            _pointer.Step(_root.transform, new Vector2(-100, -100));
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "T4 outside panel");
            _view.SetVisibility(true, false);
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 1));
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "T4 locked");
            _view.SetVisibility(false, false);
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 1));
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "T4 closed");
            _view.SetVisibility(true, true);
            _pointer.Step(_root.transform, SlotPoint(_root.transform, 1));
            Assert.IsTrue(Detail(_root.transform).gameObject.activeSelf, "T4 reopened");
        }

        [Test]
        public void T4_RightPressReportsItsSlotOnce_AndAllIneligibleGesturesReportNothing()
        {
            var uses = new List<int>();
            Build(uses.Add); Render();
            Vector2 pouch = SlotPoint(_root.transform, 1);
            _pointer.Step(_root.transform, pouch);
            _pointer.Step(_root.transform, pouch, MouseButton.Right, expectRightPress: true);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "T4 right press reports index 1 once");
            _pointer.Step(_root.transform, pouch, MouseButton.Right);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "T4 held button is no new press");
            _pointer.RightClick(_root.transform, 0);
            _pointer.RightClick(_root.transform, 2);
            _pointer.Step(_root.transform, new Vector2(-100, -100));
            _pointer.Step(_root.transform, new Vector2(-100, -100), MouseButton.Right, expectRightPress: true);
            _pointer.Step(_root.transform, pouch);
            _pointer.Step(_root.transform, pouch, MouseButton.Left);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "T4 material, empty, outside and left press do not use");
            _view.SetVisibility(true, false);
            _pointer.RightClick(_root.transform, 1);
            _view.SetVisibility(false, false);
            _pointer.RightClick(_root.transform, 1);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "T4 locked and closed do not use");
        }

        [Test]
        public void T4_OpaqueHigherOverlayBlocksEvenWithoutRaycasts_TransparentAndLowerOverlaysDoNot()
        {
            var uses = new List<int>();
            Build(uses.Add); Render();
            _pointer.RightClick(_root.transform, 1);
            CollectionAssert.AreEqual(new[] { 1 }, uses, "T4 positive control before any occlusion");
            GameObject overlay = Own(new GameObject("Occlusion fixture", typeof(RectTransform), typeof(Canvas)));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            GameObject imageObject = Own(new GameObject("Opaque non-raycast cover", typeof(RectTransform), typeof(Image)));
            imageObject.transform.SetParent(overlay.transform, false);
            Image cover = imageObject.GetComponent<Image>();
            cover.raycastTarget = false;
            cover.color = Color.white;
            cover.rectTransform.anchorMin = Vector2.zero;
            cover.rectTransform.anchorMax = Vector2.one;
            cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            _pointer.RightClick(_root.transform, 1);
            Assert.AreEqual(1, uses.Count, "T4 visible overlay blocks right use");
            Assert.IsFalse(Detail(_root.transform).gameObject.activeSelf, "T4 occlusion hides detail");
            cover.color = new Color(1, 1, 1, 0);
            _pointer.RightClick(_root.transform, 1);
            Assert.AreEqual(2, uses.Count, "T4 transparent cover does not block");
            cover.color = Color.white;
            canvas.sortingOrder = 1;
            _pointer.RightClick(_root.transform, 1);
            Assert.AreEqual(3, uses.Count, "T4 lower canvas does not block");
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void T5_PanelAndHoveredDetailsStayInsideTheCameraCanvas(int width, int height)
        {
            Build(); Render();
            Camera camera = Own(new GameObject("Grid layout camera")).AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 1000f;
            RenderTexture target = Own(new RenderTexture(width, height, 24));
            target.Create();
            camera.targetTexture = target;
            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100;
            Canvas.ForceUpdateCanvases();
            SessionTestTools.Call(_root.GetComponent<CanvasScaler>(), "Handle");
            Canvas.ForceUpdateCanvases();
            camera.Render();
            _view.Resize();
            var canvasRect = (RectTransform)_root.transform;
            AssertInside(canvasRect.rect, InSpace(canvasRect, Panel), "T5 whole panel on camera canvas");
            if (width == 1920) Assert.GreaterOrEqual(Panel.localScale.x, 1f, "T5 no downscale at Full HD");
            RectTransform pouch = Slot(_root.transform, 1);
            Vector2 lowerRight = RectTransformUtility.WorldToScreenPoint(camera, pouch.TransformPoint(new Vector2(pouch.rect.xMax - 2, pouch.rect.yMin + 2)));
            foreach (Vector2 point in new[] { SlotPoint(_root.transform, 0, camera), SlotPoint(_root.transform, 1, camera), lowerRight })
            {
                _pointer.Step(_root.transform, point);
                Canvas.ForceUpdateCanvases();
                Assert.IsTrue(Detail(_root.transform).gameObject.activeSelf, "T5 detail on a reachable occupied point");
                AssertInside(canvasRect.rect, InSpace(canvasRect, Detail(_root.transform)), "T5 whole detail on camera canvas");
            }
            camera.targetTexture = null;
        }

        void AssertDetail(string name, string count, bool usable)
        {
            Assert.IsTrue(Detail(_root.transform).gameObject.activeSelf, "T4 hovered detail visible");
            Assert.AreEqual(name, DetailLabel(_root.transform, "Name").text);
            Assert.AreEqual(count, DetailLabel(_root.transform, "Count").text);
            Assert.AreEqual(usable, DetailLabel(_root.transform, "Effect").gameObject.activeInHierarchy);
            Assert.AreEqual(usable, DetailLabel(_root.transform, "UseHint").gameObject.activeInHierarchy);
            if (!usable) return;
            Assert.AreEqual("사용 효과: 금화 +50", DetailLabel(_root.transform, "Effect").text);
            Assert.AreEqual("우클릭: 사용", DetailLabel(_root.transform, "UseHint").text);
        }

        static bool IsInk(Color actual, Color expected) => Mathf.Abs(actual.r - expected.r) <= 1f / 255 &&
            Mathf.Abs(actual.g - expected.g) <= 1f / 255 && Mathf.Abs(actual.b - expected.b) <= 1f / 255;

        static void AssertRect(Rect expected, Rect actual, string message)
        {
            Assert.AreEqual(expected.x, actual.x, .1f, message + " x");
            Assert.AreEqual(expected.y, actual.y, .1f, message + " y");
            Assert.AreEqual(expected.width, actual.width, .1f, message + " width");
            Assert.AreEqual(expected.height, actual.height, .1f, message + " height");
        }

        T Own<T>(T item) where T : UnityEngine.Object { _owned.Add(item); return item; }
    }
}
