using System;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // The bright inventory, written before the implementation (goal 2026-10-29-inventory-character-select-look 완료조건
    // 2·5; user decision 50 B; contract pr2-red-contract-v4 T5·T8). The layout pinned by
    // UiWindowAppearanceContractTests.InventoryPanel_KeepsCanvasTextsPathsInputAreasCallbacksAndVisibility stays as it
    // is; this file adds what the new look must show on top of it. Images are found by the GUID of their sprite's
    // source asset, never by object name, and states are made the way the render tests make them (InventoryState
    // ApplySnapshot, then View.Render). Expected pieces come from the approved spec rows, not from product code.
    public sealed class InventoryPanelViewAppearanceTests
    {
        // UI units; only absorbs float rounding through transforms (same as the contract tests).
        const float Tolerance = 0.5f;

        // Goal 완료조건 2 「아이콘이 재료·재화 주머니에 대응」.
        static readonly Dictionary<ItemId, string> s_itemIcons = new Dictionary<ItemId, string>
        {
            [ItemId.Material] = "Icon_Material",
            [ItemId.CoinPouch] = "Icon_CurrencyPouch",
        };

        // Pieces that are a slot's own art rather than an item icon drawn on it.
        static readonly string[] s_nonIconKinds = { "Panel", "Header", "Inset", "Divider", "Button", "ItemSlot", "IconBase", "NumberBadge" };

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

        // T5 칸 조각: an empty slot stands on the bright ItemSlot_Empty, an occupied slot on the bright ItemSlot_Filled.
        [Test]
        public void Slots_StandOnTheBrightEmptyOrFilledSlotPiece_ByOccupancy()
        {
            Window window = Build();
            RenderTwoItems(window);

            var problems = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                string[] under = BrightPiecesUnder(window.Panel, Centre(window.Panel, window.Slots[i]));
                string expected = i < 2 ? "ItemSlot_Filled" : "ItemSlot_Empty";
                string unexpected = i < 2 ? "ItemSlot_Empty" : "ItemSlot_Filled";
                TestContext.Out.WriteLine($"Slot{i + 1} under=[{string.Join(",", under)}]");
                if (!under.Contains(expected)) problems.Add($"Slot{i + 1} does not stand on the bright {expected}");
                if (under.Contains(unexpected)) problems.Add($"Slot{i + 1} also shows the bright {unexpected}");
            }

            Assert.IsEmpty(problems, "T5 empty slots show ItemSlot_Empty and occupied slots ItemSlot_Filled");
        }

        // T5 아이콘: the material slot shows Icon_Material, the coin pouch slot Icon_CurrencyPouch, empty slots no icon.
        [Test]
        public void ItemSlots_ShowTheirItemIcon_AndEmptySlotsShowNone()
        {
            Window window = Build();
            RenderTwoItems(window);

            string[] materialIcons = ItemIconNames(window, 0);
            string[] pouchIcons = ItemIconNames(window, 1);
            string[] emptyWithIcons = Enumerable.Range(2, 6)
                .Where(i => ItemIconNames(window, i).Length > 0)
                .Select(i => $"Slot{i + 1}: {string.Join(",", ItemIconNames(window, i))}").ToArray();
            TestContext.Out.WriteLine($"material=[{string.Join(",", materialIcons)}] pouch=[{string.Join(",", pouchIcons)}]");

            CollectionAssert.AreEqual(new[] { "Icon_Material" }, materialIcons, "T5 the material slot shows the bright Icon_Material");
            CollectionAssert.AreEqual(new[] { "Icon_CurrencyPouch" }, pouchIcons, "T5 the coin pouch slot shows the bright Icon_CurrencyPouch");
            Assert.IsEmpty(emptyWithIcons, "T5 an empty slot shows no item icon");
        }

        // T5 (lead decision in the contract): every defined item has an icon. Adding an ItemId to ItemCatalog without
        // an icon fails here; the two known items must show their own approved icon.
        [Test]
        public void EveryDefinedItem_ShowsAnIconInItsSlot()
        {
            ItemId[] defined = ((ItemId[])Enum.GetValues(typeof(ItemId)))
                .Where(id => id != ItemId.None && ItemCatalog.IsDefined(id)).ToArray();
            var problems = new List<string>();
            foreach (ItemId item in defined)
            {
                Window window = Build();
                var slots = new InventorySlot[InventoryLimits.MaxSlots];
                slots[0] = new InventorySlot(item, 1);
                Render(window, 1, slots);
                string[] icons = ItemIconNames(window, 0);
                TestContext.Out.WriteLine($"{item}: icons=[{string.Join(",", icons)}]");
                if (icons.Length != 1) problems.Add($"{item} shows {icons.Length} item icons, expected one");
                else if (s_itemIcons.TryGetValue(item, out string expected) && icons[0] != expected)
                    problems.Add($"{item} shows {icons[0]}, approved {expected}");
            }

            Assert.IsNotEmpty(defined, "precondition: the catalog defines at least one item");
            Assert.IsEmpty(problems, "T5 every defined item shows one icon in its slot");
        }

        // T5 재화 줄 (user decision 50 B 「재화 줄은 아래」): the top of Inventory/Currency is below the lowest slot bottom,
        // both measured in the same panel with y downward as the layout test's FromTopLeft.
        [Test]
        public void CurrencyLine_SitsBelowTheEightSlots()
        {
            Window window = Build();
            RenderTwoItems(window);

            Rect currency = FromTopLeft(window.Panel, window.Currency);
            float slotsBottom = window.Slots.Max(slot => FromTopLeft(window.Panel, slot).yMax);
            TestContext.Out.WriteLine($"currency={currency} slotsBottom={slotsBottom}");

            Assert.AreSame(window.Panel, window.Currency.parent, "fixture: Inventory/Currency is a direct child of the panel");
            Assert.GreaterOrEqual(currency.yMin, slotsBottom - Tolerance, "T5 the currency line starts below the eight slots");
        }

        // T5 재화 아이콘: a bright Icon_Coin image inside the panel, its vertical centre within the currency line.
        [Test]
        public void CurrencyLine_ShowsTheBrightCoinIcon()
        {
            Window window = Build();
            RenderTwoItems(window);

            Rect panel = FromTopLeft(window.Panel, window.Panel);
            Rect currency = FromTopLeft(window.Panel, window.Currency);
            Rect[] coins = VisibleImages(window.Panel).Where(image => IsPiece(image.sprite, "Icon_Coin"))
                .Select(image => FromTopLeft(window.Panel, image.rectTransform)).ToArray();
            Rect[] beside = coins.Where(coin => coin.center.y >= currency.yMin - Tolerance && coin.center.y <= currency.yMax + Tolerance)
                .ToArray();
            Rect[] outside = coins.Where(coin => !Inside(panel, coin)).ToArray();
            TestContext.Out.WriteLine($"currency={currency} coins=[{string.Join(";", coins)}]");

            Assert.IsNotEmpty(coins, "T5 the panel shows a bright Icon_Coin");
            Assert.IsNotEmpty(beside, "T5 a coin icon's vertical centre is within the currency line");
            Assert.IsEmpty(outside, "T5 the coin icon is inside the panel");
        }

        // T5: the new icon images take no raycast. The two item icons and the coin must be found first.
        [Test]
        public void NewIconImages_TakeNoRaycast()
        {
            Window window = Build();
            RenderTwoItems(window);

            Image[] icons = VisibleImages(window.Panel).Where(image => PieceOf(image.sprite)?.Kind == "Icon").ToArray();
            string[] names = icons.Select(image => PieceOf(image.sprite).Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] raycasting = icons.Where(image => image.raycastTarget).Select(image => image.name).ToArray();
            TestContext.Out.WriteLine($"icons=[{string.Join(",", names)}]");

            CollectionAssert.IsSupersetOf(names, new[] { "Icon_Coin", "Icon_CurrencyPouch", "Icon_Material" },
                "precondition: the two item icons and the coin icon are shown");
            Assert.IsEmpty(raycasting, "T5 icon images are not raycast targets");
        }

        // T8: the slot hover highlight must not make slot backgrounds raycast targets (the layout test's 「only the
        // buttons take raycasts」 stays true while the pointer is over a slot and its use button).
        [Test]
        public void SlotHover_KeepsOnlyButtonsAsRaycastTargets()
        {
            Window window = Build();
            RenderTwoItems(window);
            GameObject eventSystem = Own(new GameObject("Event system (fixture)", typeof(EventSystem)));
            var pointer = new PointerEventData(eventSystem.GetComponent<EventSystem>());

            var outside = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                GameObject row = window.Slots[i].gameObject;
                Button use = row.GetComponentInChildren<Button>(true);
                ExecuteEvents.ExecuteHierarchy(row, pointer, ExecuteEvents.pointerEnterHandler);
                if (use != null) ExecuteEvents.ExecuteHierarchy(use.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                int slot = i + 1;
                outside.AddRange(RaycastTargetsOutsideButtons(window.Root.transform).Select(path => $"Slot{slot} hovered: {path}"));
                if (use != null) ExecuteEvents.ExecuteHierarchy(use.gameObject, pointer, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.ExecuteHierarchy(row, pointer, ExecuteEvents.pointerExitHandler);
            }

            Assert.IsEmpty(outside, "T8 a hovered slot keeps its background and art out of the raycast");
        }

        // T8: the opening effect leaves the panel scale and position that Resize sets, right after building and after
        // Render. The reference is the product's own Resize, called again here, not a copy of its formula.
        [Test]
        public void OpeningEffect_KeepsThePanelScaleAndPositionThatResizeSets()
        {
            Window window = Build();
            Vector3 builtScale = window.Panel.localScale;
            Vector2 builtPosition = window.Panel.anchoredPosition;
            RenderTwoItems(window);
            Vector3 renderedScale = window.Panel.localScale;
            Vector2 renderedPosition = window.Panel.anchoredPosition;
            window.View.Resize();
            Vector3 resizedScale = window.Panel.localScale;
            Vector2 resizedPosition = window.Panel.anchoredPosition;
            TestContext.Out.WriteLine($"canvas={((RectTransform)window.Root.transform).rect} built={builtScale}/{builtPosition} " +
                $"rendered={renderedScale}/{renderedPosition} resized={resizedScale}/{resizedPosition}");

            Assert.AreEqual(resizedScale, builtScale, "T8 right after building, the panel scale is the Resize scale");
            Assert.AreEqual(resizedPosition, builtPosition, "T8 right after building, the panel position is the Resize position");
            Assert.AreEqual(resizedScale, renderedScale, "T8 after Render, the panel scale is the Resize scale");
            Assert.AreEqual(resizedPosition, renderedPosition, "T8 after Render, the panel position is the Resize position");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Fixture and observers
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
            GameObject root = Own(new GameObject("Inventory appearance under test", typeof(RectTransform)));
            var view = new InventoryPanelView(root, _ => { }, () => { });
            _cleanups.Add(view.Dispose);
            var panel = (RectTransform)root.transform.Find("Inventory");
            Assert.IsNotNull(panel, "fixture: the builder makes the Inventory panel");
            var currency = (RectTransform)panel.Find("Currency");
            Assert.IsNotNull(currency, "fixture: Inventory/Currency");
            RectTransform[] slots = Enumerable.Range(1, 8).Select(n => (RectTransform)panel.Find("Slot" + n)).ToArray();
            Assert.IsTrue(slots.All(slot => slot != null), "fixture: Inventory/Slot1~8");
            return new Window { Root = root, View = view, Panel = panel, Currency = currency, Slots = slots };
        }

        // Same state as the render tests' pointer case: material ×3 and a coin pouch ×2 in the first two slots.
        void RenderTwoItems(Window window)
        {
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, 3);
            slots[1] = new InventorySlot(ItemId.CoinPouch, 2);
            Render(window, 120, slots);
        }

        void Render(Window window, int currency, InventorySlot[] slots)
        {
            GameObject mirrorOwner = Own(new GameObject("Inventory mirror (fixture)"));
            mirrorOwner.SetActive(false);
            var mirror = mirrorOwner.AddComponent<InventoryState>();
            Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(1, currency, slots),
                "fixture: the snapshot is accepted by the mirror");
            window.View.Render(mirror, null, true);
        }

        static Image[] VisibleImages(RectTransform panel) => panel.GetComponentsInChildren<Image>(false)
            .Where(image => image.isActiveAndEnabled && image.sprite != null && image.color.a > 0f).ToArray();

        static string[] BrightPiecesUnder(RectTransform panel, Vector2 point) => VisibleImages(panel)
            .Where(image => PieceOf(image.sprite) != null && ContainsPoint(InSpaceOf(panel, image.rectTransform), point))
            .Select(image => PieceOf(image.sprite).Name).ToArray();

        // Item icons drawn on a slot: visible images centred inside the slot row that are neither a button graphic nor
        // the slot's own art. A sprite outside the bright table still counts, so a new item's own icon is seen.
        static string[] ItemIconNames(Window window, int slot)
        {
            Rect row = InSpaceOf(window.Panel, window.Slots[slot]);
            return VisibleImages(window.Panel)
                .Where(image => !IsButtonGraphic(image) && ContainsPoint(row, InSpaceOf(window.Panel, image.rectTransform).center))
                .Where(image => PieceOf(image.sprite) == null || !s_nonIconKinds.Contains(PieceOf(image.sprite).Kind))
                .Select(image => PieceOf(image.sprite)?.Name ?? image.sprite.name).ToArray();
        }

        static bool IsButtonGraphic(Graphic graphic)
        {
            var owner = graphic.GetComponentInParent<Selectable>(true);
            return owner != null && owner.targetGraphic == graphic;
        }

        // Same observer as the layout test: enabled raycast graphics that are not inside any Button.
        static string[] RaycastTargetsOutsideButtons(Transform window) => window.GetComponentsInChildren<Graphic>(true)
            .Where(graphic => graphic.enabled && graphic.raycastTarget && !(graphic is TMPro.TMP_SubMeshUI) &&
                graphic.GetComponentInParent<Button>(true) == null)
            .Select(graphic => graphic.transform.name).ToArray();

        // A rectangle in the panel's local UI units, whatever the anchors, pivots or scale in between.
        static Rect InSpaceOf(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static Vector2 Centre(RectTransform panel, RectTransform target) => InSpaceOf(panel, target).center;

        // Offset of the target's top-left from the panel's top-left and its size, UI units with y downward.
        static Rect FromTopLeft(RectTransform panel, RectTransform target)
        {
            Rect r = InSpaceOf(panel, target), p = panel.rect;
            return new Rect(r.xMin - p.xMin, p.yMax - r.yMax, r.width, r.height);
        }

        static bool ContainsPoint(Rect r, Vector2 p) =>
            p.x >= r.xMin - Tolerance && p.x <= r.xMax + Tolerance && p.y >= r.yMin - Tolerance && p.y <= r.yMax + Tolerance;

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
