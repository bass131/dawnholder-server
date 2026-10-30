using System;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // The 390×720 inventory and its placement, written before the implementation (goal
    // 2026-10-30-window-placement-inventory 완료조건 1·2·6; user decisions 66 C and 70 A; contract placement-red-contract-v1
    // T1·T2). The exact frame, slot and use rectangles are pinned by
    // UiWindowAppearanceContractTests.InventoryPanel_KeepsCanvasTextsPathsInputAreasCallbacksAndVisibility; this file adds
    // what the new frame must show inside a slot, in the title bar and below the slots, and where Resize puts the panel.
    // Expected values come from the contract's layout table and the goal (12-unit margin), never from product code.
    // The placement part is a new type the base commit does not have, so it is found by its full type name only.
    public sealed class InventoryWindowPlacementTests
    {
        // UI units; only absorbs float rounding through transforms (same as the contract tests).
        const float Tolerance = 0.5f;

        // Goal 「만들 것」: the default place keeps 12 UI units from the canvas left edge.
        const float LeftMargin = 12f;

        // The shared placement part (contract 「부품 API」), read by name so this file compiles before it exists.
        const string PlacementTypeName = "Dawnholder.Client.UI.UiWindowPlacement";

        // Goal 완료조건 2 of the bright look (2026-10-29): the icon each known item shows.
        static readonly Dictionary<ItemId, string> s_itemIcons = new Dictionary<ItemId, string>
        {
            [ItemId.Material] = "Icon_Material",
            [ItemId.CoinPouch] = "Icon_CurrencyPouch",
        };

        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        readonly List<Action> _cleanups = new List<Action>();

        [TearDown]
        public void Teardown()
        {
            for (int i = _cleanups.Count - 1; i >= 0; i--) _cleanups[i]();
            _cleanups.Clear();
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        // ---------------------------------------------------------------------------------------------------------
        // T1 — inside a slot, the title bar and the two lines below the slots
        // ---------------------------------------------------------------------------------------------------------

        // T1 칸 안: in every slot the item icon (occupied slots), the 「이름 ×수량」 text and the use button lie inside the
        // slot, stacked icon above text above button, all on the slot's horizontal centre. Two occupied and six empty slots.
        [Test]
        public void SlotContents_StayInsideTheSlot_StackedIconTextUse_OnTheSlotCentre()
        {
            Window window = Build();
            ItemId[] items = RenderTwoItems(window);

            var problems = new List<string>();
            int iconsFound = 0;
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                string slotName = $"Slot{i + 1}";
                Rect slot = FromTopLeft(window.Panel, window.Slots[i]);
                var text = (RectTransform)window.Slots[i].Find("Item");
                var use = (RectTransform)window.Slots[i].Find("Use");
                Assert.IsNotNull(text, $"fixture: Inventory/{slotName}/Item");
                Assert.IsNotNull(use, $"fixture: Inventory/{slotName}/Use");
                var parts = new List<(string name, Rect rect)>();
                if (items[i] != ItemId.None)
                {
                    Rect[] icons = VisibleImages(window.Panel).Where(image => IsPiece(image.sprite, s_itemIcons[items[i]]))
                        .Select(image => FromTopLeft(window.Panel, image.rectTransform)).ToArray();
                    if (icons.Length != 1)
                    {
                        problems.Add($"{slotName} shows {icons.Length} {s_itemIcons[items[i]]} images, expected one");
                        continue;
                    }
                    iconsFound++;
                    parts.Add(("icon", icons[0]));
                }
                parts.Add(("text", FromTopLeft(window.Panel, text)));
                parts.Add(("use", FromTopLeft(window.Panel, use)));
                TestContext.Out.WriteLine($"{slotName} slot={slot} {string.Join(" ", parts.Select(part => $"{part.name}={part.rect}"))}");

                foreach ((string name, Rect rect) in parts)
                {
                    if (!Inside(slot, rect)) problems.Add($"{slotName} {name} {rect} is not inside the slot {slot}");
                    if (Mathf.Abs(rect.center.x - slot.center.x) > Tolerance)
                        problems.Add($"{slotName} {name} centre x {rect.center.x} is not the slot centre x {slot.center.x}");
                }
                for (int p = 1; p < parts.Count; p++)
                {
                    // y grows downward in FromTopLeft, so the part above has the smaller centre.
                    if (parts[p - 1].rect.center.y >= parts[p].rect.center.y)
                        problems.Add($"{slotName} {parts[p - 1].name} (centre y {parts[p - 1].rect.center.y}) is not above " +
                            $"{parts[p].name} (centre y {parts[p].rect.center.y})");
                }
            }

            Assert.AreEqual(items.Count(item => item != ItemId.None), iconsFound, "precondition: every occupied slot's icon is found by its sprite");
            Assert.IsEmpty(problems, "T1 each slot holds its icon, 「이름 ×수량」 text and use button inside it, top to bottom on its centre");
        }

        // T1 제목 띠: the bright Header image is at the panel top above the slots, the refresh button lies inside it and
        // on its right side.
        [Test]
        public void TitleBar_IsAtThePanelTop_AndHoldsRefreshOnItsRightSide()
        {
            Window window = Build();
            RenderTwoItems(window);

            Rect panel = FromTopLeft(window.Panel, window.Panel);
            Rect[] bars = VisibleImages(window.Panel).Where(image => IsPiece(image.sprite, "Header"))
                .Select(image => FromTopLeft(window.Panel, image.rectTransform)).ToArray();
            var refresh = (RectTransform)window.Panel.Find("Refresh");
            Assert.IsNotNull(refresh, "fixture: Inventory/Refresh");
            Rect refreshRect = FromTopLeft(window.Panel, refresh);
            float slotsTop = window.Slots.Min(slot => FromTopLeft(window.Panel, slot).yMin);
            TestContext.Out.WriteLine($"panel={panel} bars=[{string.Join(";", bars)}] refresh={refreshRect} slotsTop={slotsTop}");

            Assert.AreEqual(1, bars.Length, "precondition: the panel shows one bright Header title bar");
            Rect bar = bars[0];
            Assert.IsTrue(Inside(panel, bar), $"T1 the title bar {bar} is inside the panel {panel}");
            Assert.LessOrEqual(bar.yMax, slotsTop + Tolerance, "T1 the title bar is at the panel top, above every slot");
            Assert.IsTrue(Inside(bar, refreshRect), $"T1 the refresh button {refreshRect} lies inside the title bar {bar}");
            Assert.Greater(refreshRect.center.x, bar.center.x, "T1 the refresh button sits on the title bar's right side");
        }

        // T1 아래 두 줄: the currency line and the use result text take separate lines, both below the eight slots. The
        // coin icon's place in the currency line stays with InventoryPanelViewAppearanceTests.CurrencyLine_ShowsTheBrightCoinIcon.
        [Test]
        public void CurrencyAndUseResult_TakeSeparateLines_BelowTheEightSlots()
        {
            Window window = Build();
            RenderTwoItems(window);

            var result = (RectTransform)window.Panel.Find("UseResult");
            Assert.IsNotNull(result, "fixture: Inventory/UseResult");
            Rect currencyLine = FromTopLeft(window.Panel, window.Currency);
            Rect resultLine = FromTopLeft(window.Panel, result);
            float slotsBottom = window.Slots.Max(slot => FromTopLeft(window.Panel, slot).yMax);
            bool separate = currencyLine.yMax <= resultLine.yMin + Tolerance || resultLine.yMax <= currencyLine.yMin + Tolerance;
            TestContext.Out.WriteLine($"currency={currencyLine} result={resultLine} slotsBottom={slotsBottom}");

            Assert.IsTrue(separate, $"T1 the currency line {currencyLine} and the use result {resultLine} share no vertical range");
            Assert.GreaterOrEqual(currencyLine.yMin, slotsBottom - Tolerance, "T1 the currency line is below the eight slots");
            Assert.GreaterOrEqual(resultLine.yMin, slotsBottom - Tolerance, "T1 the use result is below the eight slots");
        }

        // ---------------------------------------------------------------------------------------------------------
        // T2 — default place and the placement part
        // ---------------------------------------------------------------------------------------------------------

        // T2 기본 자리: after Resize the panel's left edge is 12 UI units right of the root canvas's left edge, and the
        // whole panel (with its scale) is inside the canvas. The top edge needs the HUD and is checked in PlayMode.
        [Test]
        public void Resize_PutsThePanelTwelveUnitsFromTheCanvasLeft_InsideTheCanvas()
        {
            Window window = Build();
            RenderTwoItems(window);
            Canvas.ForceUpdateCanvases();
            window.View.Resize();

            var canvasRect = (RectTransform)window.Root.transform;
            Rect canvas = canvasRect.rect;
            Rect panel = InSpaceOf(canvasRect, window.Panel);
            TestContext.Out.WriteLine($"canvas={canvas} panel={panel} scale={window.Panel.localScale.x}");

            Assert.Greater(canvas.width, 0f, "precondition: the root canvas has a width to place the panel in");
            Assert.Greater(canvas.height, 0f, "precondition: the root canvas has a height to place the panel in");
            Assert.AreEqual(canvas.xMin + LeftMargin, panel.xMin, Tolerance, "T2 the panel's left edge is 12 units from the canvas left");
            Assert.IsTrue(Inside(canvas, panel), $"T2 the whole panel {panel} is inside the canvas {canvas}");
        }

        // T2 부품 연결: the panel carries exactly one shared placement part, and the title bar holds its grab area.
        [Test]
        public void Panel_CarriesOnePlacementPart_AndTheTitleBarHoldsTheGrabArea()
        {
            Window window = Build();

            int parts = window.Panel.GetComponents<Component>().Count(component => component != null &&
                component.GetType().FullName == PlacementTypeName);
            Transform grabArea = window.Root.transform.Find("Inventory/Header/GrabArea");
            TestContext.Out.WriteLine($"placement parts={parts} grabArea={(grabArea != null ? "found" : "missing")}");

            Assert.AreEqual(1, parts, $"T2 the Inventory panel carries one {PlacementTypeName}");
            Assert.IsNotNull(grabArea, "T2 the title bar holds the grab area Inventory/Header/GrabArea");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Fixture and observers (the same shapes as InventoryPanelViewAppearanceTests, whose helpers are private)
        // ---------------------------------------------------------------------------------------------------------

        sealed class Window
        {
            internal GameObject Root;
            internal InventoryPanelView View;
            internal RectTransform Panel;
            internal RectTransform Currency;
            internal RectTransform[] Slots;
        }

        Window Build()
        {
            GameObject root = Own(new GameObject("Inventory placement under test", typeof(RectTransform)));
            var view = new InventoryPanelView(root, _ => { }, () => { });
            _cleanups.Add(view.Dispose);
            var panel = (RectTransform)root.transform.Find("Inventory");
            Assert.IsNotNull(panel, "fixture: the builder makes the Inventory panel");
            var currency = (RectTransform)panel.Find("Currency");
            Assert.IsNotNull(currency, "fixture: Inventory/Currency");
            RectTransform[] slots = Enumerable.Range(1, InventoryLimits.MaxSlots).Select(n => (RectTransform)panel.Find("Slot" + n)).ToArray();
            Assert.IsTrue(slots.All(slot => slot != null), "fixture: Inventory/Slot1~8");
            return new Window { Root = root, View = view, Panel = panel, Currency = currency, Slots = slots };
        }

        // Occupied and empty slots mixed, as the PR2 icon test makes them: material ×3 and a coin pouch ×2 first.
        ItemId[] RenderTwoItems(Window window)
        {
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, 3);
            slots[1] = new InventorySlot(ItemId.CoinPouch, 2);
            GameObject mirrorOwner = Own(new GameObject("Inventory mirror (fixture)"));
            mirrorOwner.SetActive(false);
            var mirror = mirrorOwner.AddComponent<InventoryState>();
            Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(1, 120, slots),
                "fixture: the snapshot is accepted by the mirror");
            window.View.Render(mirror, null, true);
            foreach (TMP_Text text in window.Root.GetComponentsInChildren<TMP_Text>(false)) text.ForceMeshUpdate();
            return slots.Select(slot => slot.ItemId).ToArray();
        }

        static Image[] VisibleImages(RectTransform panel) => panel.GetComponentsInChildren<Image>(false)
            .Where(image => image.isActiveAndEnabled && image.sprite != null && image.color.a > 0f).ToArray();

        // A rectangle in the space's local UI units, whatever the anchors, pivots or scale in between.
        static Rect InSpaceOf(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        // Offset of the target's top-left from the panel's top-left and its size, UI units with y downward.
        static Rect FromTopLeft(RectTransform panel, RectTransform target)
        {
            Rect r = InSpaceOf(panel, target), p = panel.rect;
            return new Rect(r.xMin - p.xMin, p.yMax - r.yMax, r.width, r.height);
        }

        static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - Tolerance && inner.xMax <= outer.xMax + Tolerance &&
            inner.yMin >= outer.yMin - Tolerance && inner.yMax <= outer.yMax + Tolerance;

        T Own<T>(T obj) where T : UnityEngine.Object
        {
            _owned.Add(obj);
            return obj;
        }
    }
}
