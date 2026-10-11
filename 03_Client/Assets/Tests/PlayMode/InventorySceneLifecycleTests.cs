using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Reads the runtime panel through its scene objects only. This assembly cannot see the internal
    // view and does not reference uGUI, so buttons are reached as components and pressed with a pointer.
    internal static class InventoryPanelProbe
    {
        internal static Transform Root
        {
            get
            {
                Assert.IsNotNull(InventoryPanel.Instance, "a gameplay scene owns the inventory panel");
                return InventoryPanel.Instance.transform;
            }
        }
        internal static string Currency => Text("Currency");
        internal static string Status => Text("Synchronization");
        internal static string Result => Text("UseResult");
        internal static string Slot(int index) => Text($"Slot{index + 1}/Item");
        internal static CanvasGroup Group => Root.GetComponent<CanvasGroup>();
        internal static Component Use(int index)
        {
            Transform row = Root.Find($"Inventory/Slot{index + 1}/Use");
            Assert.IsNotNull(row, $"Use button of slot {index + 1}");
            return row.GetComponents<Component>().Single(c => c.GetType().FullName == "UnityEngine.UI.Button");
        }
        internal static bool Interactable(Component button) =>
            (bool)button.GetType().GetProperty("interactable").GetValue(button);
        internal static int LivePanels => Resources.FindObjectsOfTypeAll<InventoryPanel>()
            .Count(panel => panel.gameObject.scene.IsValid());

        static string Text(string path)
        {
            Transform label = Root.Find("Inventory/" + path);
            Assert.IsNotNull(label, "panel label " + path);
            return label.GetComponent<TMP_Text>().text;
        }

        // R6/R8 "텍스트가 읽히며": the source string is not enough, every character must be laid out on screen.
        internal static void AssertEveryLabelIsDrawn()
        {
            foreach (TMP_Text label in Root.GetComponentsInChildren<TMP_Text>())
            {
                if (!label.gameObject.activeInHierarchy || label.text.Length == 0) continue;
                label.ForceMeshUpdate();
                Assert.IsFalse(label.isTextOverflowing,
                    $"'{label.text}' ({label.transform.parent.name}/{label.name}) is cut off: needs {label.preferredHeight:F1} in {((RectTransform)label.transform).rect.height}");
                Assert.AreEqual(label.text.Length, label.textInfo.characterCount, $"'{label.text}' laid-out characters");
            }
        }

        internal static Component Refresh
        {
            get
            {
                Transform button = Root.Find("Inventory/Refresh");
                Assert.IsNotNull(button, "Refresh button");
                return button.GetComponents<Component>().Single(c => c.GetType().FullName == "UnityEngine.UI.Button");
            }
        }

        // R8 real click path: screen point -> Input System mouse -> EventSystem raycast -> Button.onClick.
        internal static IEnumerator Click(Mouse mouse, Component button) => ClickAt(mouse, ScreenPoint(button));

        internal static Vector2 ScreenPoint(Component button)
        {
            var rect = (RectTransform)button.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, Screen.width), "button must be on screen");
            Assert.That(point.y, Is.InRange(0f, Screen.height), "button must be on screen");
            return point;
        }

        // The same left-button press/release at any screen point, e.g. on the game world outside the panel.
        internal static IEnumerator ClickAt(Mouse mouse, Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            for (int i = 0; i < 2; i++) yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            for (int i = 0; i < 2; i++) yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            for (int i = 0; i < 2; i++) yield return null;
        }

        // Moves the pointer without pressing, so a later keyboard attack happens while it rests on that point.
        internal static IEnumerator PointAt(Mouse mouse, Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            for (int i = 0; i < 2; i++) yield return null;
        }

        // A world point a player would click to swing: the local player's own position on screen.
        internal static Vector2 PlayerScreenPoint(Component player)
        {
            Assert.IsNotNull(Camera.main, "gameplay camera");
            Vector3 point = Camera.main.WorldToScreenPoint(player.transform.position);
            Assert.That(point.x, Is.InRange(0f, Screen.width), "player must be on screen");
            Assert.That(point.y, Is.InRange(0f, Screen.height), "player must be on screen");
            return point;
        }

        // Waits for the client's attack cooldown/commit mirror to allow a new swing, so a missing swing is not
        // mistaken for a cooldown and a started cooldown can be observed as an attack prediction.
        internal static IEnumerator AttackReady(LocalPlayerMovement player) =>
            MapEntryPlayFixture.Wait(() => player.CanAttack && !player.IsActionLocked, "attack cooldown ready", 3);

        internal static int Subscribers(object owner, string eventField)
        {
            var handler = (Delegate)owner.GetType()
                .GetField(eventField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
            return handler == null ? 0 : handler.GetInvocationList().Length;
        }

        // The objects an event calls back. The HUD also follows the mirror (dungeon goal R1), so the panel's own
        // subscription is told apart from the HUD's by target.
        internal static object[] Targets(object owner, string eventField)
        {
            var handler = (Delegate)owner.GetType()
                .GetField(eventField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
            return handler == null ? Array.Empty<object>() : handler.GetInvocationList().Select(d => d.Target).ToArray();
        }

        // A connection's first map starts with the panel closed and I opens it (dungeon goal R5, R4), so checks of
        // the open panel's display, clicks and layout open it as a player does, after gameplay Ready.
        internal static IEnumerator OpenWithI(MapEntryPlayFixture fixture)
        {
            bool shownBefore = Group.alpha > 0f;
            bool raycastsBefore = Group.blocksRaycasts;
            fixture.Keys(Key.I);
            yield return fixture.Frames(2);
            fixture.Keys();
            yield return fixture.Frames(2);
            Assert.IsFalse(shownBefore, "dungeon R5 the panel is closed before I");
            Assert.IsFalse(raycastsBefore, "dungeon R6 the closed panel takes no raycasts");
            Assert.AreEqual(1f, Group.alpha, "dungeon R4 I opens the panel");
            Assert.IsTrue(Group.blocksRaycasts, "dungeon R4 the open panel's buttons take raycasts");
        }
    }

    // R8 "기존 HUD … 겹침·입력 차단이 없는지" (opus-pr2-fix1 #4) on the live scene objects. Screen rectangles are read
    // from the RectTransform corners of the real overlay canvases; nothing is derived from the panel's own layout code.
    // A HUD graphic counts while it is active, even when its CanvasGroup alpha hides it now (Party_Info and
    // Quest_Hud toggle that way and return later), so a hidden overlap still fails and is labelled as hidden.
    internal static class HudOverlapProbe
    {
        internal readonly struct HudGraphic
        {
            internal readonly string Path;
            internal readonly Rect Drawn;
            internal readonly Rect Box;
            internal readonly bool Shown;
            internal readonly bool RaycastTarget;

            internal HudGraphic(string path, Rect drawn, Rect box, bool shown, bool raycastTarget)
            {
                Path = path;
                Drawn = drawn;
                Box = box;
                Shown = shown;
                RaycastTarget = raycastTarget;
            }

            public override string ToString() => $"{Path} drawn {Drawn} {(Shown ? "shown" : "hidden by alpha")}";
        }

        static Canvas HudCanvas => HudController.Instance.GetComponentInParent<Canvas>().rootCanvas;
        static RectTransform Panel => (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
        static Rect ScreenArea => new Rect(0f, 0f, Screen.width, Screen.height);

        static RectTransform CharacterStatus => HudCanvas.GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(rect => rect.name == "character_status");

        // The scene must really show the HUD first: a check over zero HUD graphics would pass vacuously.
        internal static IEnumerator WaitForHudAndSettledPanel()
        {
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null && HudController.Instance.isActiveAndEnabled &&
                CharacterStatus != null && CharacterStatus.gameObject.activeInHierarchy &&
                Area(Intersect(ScreenRect(CharacterStatus), ScreenArea)) > 0f, "additive HUD with character_status on screen", 10);
            // The panel follows the HUD in its own Update; wait until its rectangle holds still for two frames.
            Rect previous = ScreenRect(Panel);
            int still = 0;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (still < 2)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "the inventory panel never settled");
                yield return null;
                Rect current = ScreenRect(Panel);
                still = current == previous ? still + 1 : 0;
                previous = current;
            }
        }

        internal static IEnumerator AssertPanelClearOfHud(string moment)
        {
            yield return WaitForHudAndSettledPanel();
            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha, $"{moment}: the panel is shown");
            Rect panel = ScreenRect(Panel);
            HudGraphic[] hud = HudGraphics();
            HudGraphic[] statusGraphics = hud.Where(graphic => graphic.Path.StartsWith("character_status")).ToArray();
            HudGraphic[] statusOnScreen = statusGraphics
                .Where(graphic => graphic.Shown && Area(Intersect(graphic.Drawn, ScreenArea)) > 0f).ToArray();
            Rect status = ScreenRect(CharacterStatus);

            // Positive control in the same frame: with the panel's top centre moved onto a drawn character_status
            // graphic, the same detector must report that graphic. The panel's own placement is restored at once.
            string[] controlHits = Array.Empty<string>();
            bool controlRestored = true;
            if (statusOnScreen.Length > 0)
            {
                Vector2 placed = Panel.anchoredPosition;
                Vector2 target = statusOnScreen[0].Drawn.center;
                Panel.position = new Vector3(target.x, target.y, Panel.position.z);
                controlHits = Overlapping(ScreenRect(Panel), hud).Select(graphic => graphic.Path).ToArray();
                Panel.anchoredPosition = placed;
                controlRestored = Same(panel, ScreenRect(Panel));
            }

            string[] drawnOverlaps = Overlapping(panel, hud).Select(graphic => graphic.ToString()).ToArray();
            Rect[] buttons = PanelButtons().Select(ScreenRect).ToArray();
            string[] blockedButtons = hud.Where(graphic => graphic.RaycastTarget &&
                buttons.Any(button => Area(Intersect(graphic.Box, button)) > 0f)).Select(graphic => graphic.Path).ToArray();
            Rect[] labels = InventoryPanelProbe.Root.GetComponentsInChildren<TMP_Text>()
                .Where(label => label.gameObject.activeInHierarchy && !string.IsNullOrEmpty(label.text))
                .Select(label => ScreenRect((RectTransform)label.transform)).ToArray();
            int slotRows = Enumerable.Range(1, InventoryLimits.MaxSlots)
                .Count(slot => Contains(ScreenArea, ScreenRect((RectTransform)InventoryPanelProbe.Root.Find($"Inventory/Slot{slot}"))));
            Debug.Log($"[PR2 verifier] {moment}: screen {Screen.width}x{Screen.height}; " +
                $"HUD canvas sort {HudCanvas.sortingOrder} scale {HudCanvas.scaleFactor:F4} {ScalerSettings(HudCanvas)}; " +
                $"panel canvas sort {Panel.GetComponentInParent<Canvas>().rootCanvas.sortingOrder} " +
                $"scale {Panel.GetComponentInParent<Canvas>().rootCanvas.scaleFactor:F4} {ScalerSettings(Panel.GetComponentInParent<Canvas>().rootCanvas)}; " +
                $"panel {panel} localScale {Panel.localScale.x:F4}; character_status {status}; gap panel top to status bottom {status.yMin - panel.yMax:F2} px; " +
                $"HUD graphics {hud.Length} (character_status {statusGraphics.Length}, shown {hud.Count(graphic => graphic.Shown)}, " +
                $"raycast {hud.Count(graphic => graphic.RaycastTarget)}); " +
                $"top-level HUD {string.Join("; ", TopLevelHud())}; control hits {controlHits.Length}; " +
                $"drawn overlaps {drawnOverlaps.Length}: {string.Join("; ", drawnOverlaps)}; blocked buttons {blockedButtons.Length}; " +
                $"labels {labels.Length}, slot rows on screen {slotRows}");

            Assert.Greater(hud.Length, 0, $"{moment}: the real HUD has drawn graphics to compare");
            Assert.Greater(statusOnScreen.Length, 0, $"{moment}: character_status is visibly drawn on screen");
            Assert.Contains(statusOnScreen[0].Path, controlHits, $"{moment}: control - a panel placed on character_status is detected");
            Assert.IsTrue(controlRestored, $"{moment}: fixture - the control restored the panel rectangle");
            Assert.IsEmpty(drawnOverlaps, $"{moment}: #4/R8 no HUD graphic shares screen area with the inventory panel");
            Assert.IsEmpty(blockedButtons, $"{moment}: R8 no HUD raycast target lies over a panel button");
            Assert.IsTrue(Contains(ScreenArea, panel), $"{moment}: the whole panel {panel} is on the {Screen.width}x{Screen.height} screen");
            Assert.AreEqual(InventoryLimits.MaxSlots, slotRows, $"{moment}: all eight slot rows are on screen");
            Assert.IsTrue(labels.All(label => Contains(ScreenArea, label)), $"{moment}: every non-empty panel label is on screen");
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
        }

        // Every active graphic under the HUD canvas that draws something: Image quads by their rectangle and TMP texts
        // by their laid-out glyph bounds, so an empty or oversized text box is not mistaken for drawn ink.
        static HudGraphic[] HudGraphics()
        {
            Transform root = HudCanvas.transform;
            var graphics = new List<HudGraphic>();
            foreach (CanvasRenderer renderer in root.GetComponentsInChildren<CanvasRenderer>())
            {
                Component graphic = renderer.GetComponents<Component>().FirstOrDefault(IsGraphic);
                if (graphic == null || !((Behaviour)graphic).isActiveAndEnabled || renderer.cull) continue;
                var rect = (RectTransform)renderer.transform;
                Color color = graphic is TMP_Text text ? text.color : (Color)graphic.GetType().GetProperty("color").GetValue(graphic);
                bool raycastTarget = (bool)graphic.GetType().GetProperty("raycastTarget").GetValue(graphic) && BlocksRaycasts(rect);
                Rect box = ScreenRect(rect);
                Rect drawn = box;
                if (graphic is TMP_Text label)
                {
                    bool laidOut = !string.IsNullOrEmpty(label.text) && label.textInfo != null && label.textInfo.characterCount > 0;
                    drawn = laidOut ? Bounds(rect, label.textBounds) : Rect.zero;
                }
                if (color.a <= 0f || Area(drawn) <= 0f)
                {
                    if (raycastTarget) graphics.Add(new HudGraphic(PathOf(rect, root), Rect.zero, box, false, true));
                    continue;
                }
                bool shown = renderer.GetInheritedAlpha() > 0f;
                graphics.Add(new HudGraphic(PathOf(rect, root), drawn, box, shown, raycastTarget));
            }
            return graphics.ToArray();
        }

        static IEnumerable<HudGraphic> Overlapping(Rect panel, IEnumerable<HudGraphic> hud) =>
            hud.Where(graphic => Area(Intersect(graphic.Drawn, panel)) > 0f);

        static IEnumerable<RectTransform> PanelButtons() => InventoryPanelProbe.Root.GetComponentsInChildren<RectTransform>()
            .Where(rect => rect.gameObject.activeInHierarchy && rect.GetComponents<Component>().Any(c => c.GetType().FullName == "UnityEngine.UI.Button"));

        static IEnumerable<string> TopLevelHud()
        {
            foreach (Transform child in HudCanvas.transform)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                yield return child is RectTransform rect ? $"{child.name} {ScreenRect(rect)}" : $"{child.name} (plain Transform)";
            }
        }

        // A CanvasGroup with blocksRaycasts off (a hidden Party_Info, for example) lets clicks through its children.
        static bool BlocksRaycasts(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                var group = current.GetComponent<CanvasGroup>();
                if (group == null || !group.enabled) continue;
                if (!group.blocksRaycasts) return false;
                if (group.ignoreParentGroups) return true;
            }
            return true;
        }

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
                if (type.FullName == "UnityEngine.UI.Graphic") return true;
            return false;
        }

        // CanvasScaler lives in uGUI, which this assembly does not reference; its settings are only logged.
        static string ScalerSettings(Canvas canvas)
        {
            Component scaler = canvas.GetComponents<Component>().FirstOrDefault(c => c.GetType().FullName == "UnityEngine.UI.CanvasScaler");
            if (scaler == null) return "no CanvasScaler";
            object Read(string property) => scaler.GetType().GetProperty(property).GetValue(scaler);
            return $"CanvasScaler {Read("uiScaleMode")} reference {Read("referenceResolution")} match {Read("matchWidthOrHeight")}";
        }

        static string PathOf(Transform transform, Transform root)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null && current != root; current = current.parent) names.Insert(0, current.name);
            return string.Join("/", names);
        }

        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        static Rect Bounds(Transform owner, Bounds local)
        {
            Vector3 min = owner.TransformPoint(local.min);
            Vector3 max = owner.TransformPoint(local.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        static Rect Intersect(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
            Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        static float Area(Rect rect) => rect.width > 0f && rect.height > 0f ? rect.width * rect.height : 0f;

        static bool Same(Rect a, Rect b) => Mathf.Abs(a.xMin - b.xMin) < 0.01f && Mathf.Abs(a.yMin - b.yMin) < 0.01f &&
            Mathf.Abs(a.xMax - b.xMax) < 0.01f && Mathf.Abs(a.yMax - b.yMax) < 0.01f;

        // Half a pixel absorbs float corners of a rectangle that ends exactly on the screen edge.
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 0.5f && inner.yMin >= outer.yMin - 0.5f &&
            inner.xMax <= outer.xMax + 0.5f && inner.yMax <= outer.yMax + 0.5f;
    }

    // Scripted authority on a real TCP socket and the real Town/HuntingGround/Ending scenes.
    // Values are fixed wire inputs; the real GameServer path is InventoryServerIntegrationTests.
    public sealed class InventorySceneLifecycleTests
    {
        MapEntryPlayFixture _fixture;
        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        static ArraySegment<byte> Snapshot(uint revision, int currency, params (ItemId item, int count)[] stacks)
        {
            var packet = new S_InventorySnapshot { revision = revision, currency = currency };
            for (int i = 0; i < stacks.Length; i++)
            {
                typeof(S_InventorySnapshot).GetField($"slot{i}ItemId").SetValue(packet, (int)stacks[i].item);
                typeof(S_InventorySnapshot).GetField($"slot{i}Count").SetValue(packet, stacks[i].count);
            }
            return packet.Write();
        }

        IEnumerator WaitQueries(int count) => MapEntryPlayFixture.Wait(
            () => _fixture.Peer.Count(PacketID.C_InventoryRequest) == count, $"C_InventoryRequest #{count}", 3);

        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            Assert.AreEqual(1, InventoryPanelProbe.LivePanels, "R8 one panel in Town");
            Assert.AreEqual("재화 —", InventoryPanelProbe.Currency, "nothing is shown before a server snapshot");
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return WaitQueries(1);
            _fixture.Peer.Send(Snapshot(1, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
        }

        [UnityTest]
        public IEnumerator RealTown_ShowsTheSnapshot_AndAPointerClickUsesThePouchOnceUntilItsSnapshot()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            Debug.Log($"[PR2 verifier] Town screen {Screen.width}x{Screen.height}");
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency);
            Assert.AreEqual("재료 ×2", InventoryPanelProbe.Slot(0));
            Assert.AreEqual("재화 주머니 ×1", InventoryPanelProbe.Slot(1));
            Assert.AreEqual("빈 슬롯", InventoryPanelProbe.Slot(2));
            Assert.IsFalse(InventoryPanelProbe.Use(0).gameObject.activeSelf, "R6 Material is not usable");
            Component use = InventoryPanelProbe.Use(1);
            Assert.IsTrue(use.gameObject.activeSelf && InventoryPanelProbe.Interactable(use), "R6 owned CoinPouch is usable");
            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha);
            Assert.IsTrue(InventoryPanelProbe.Group.blocksRaycasts);

            int attacksBefore = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, use);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "C_ItemUse from a pointer click", 3);
            var sent = _fixture.Peer.Last<C_ItemUse>(PacketID.C_ItemUse);
            Assert.AreEqual((int)ItemId.CoinPouch, sent.itemId);
            Assert.AreEqual(1u, sent.expectedRevision, "R6 the use names the shown revision");
            int extraAttacks = _fixture.Peer.Count(PacketID.C_Attack) - attacksBefore;
            Debug.Log($"[PR2 verifier] pointer click on Use also sent C_Attack x{extraAttacks}");
            Assert.AreEqual(0, extraAttacks, "#2/R8 a pointer click on Use sends no C_Attack (C_Attack precedes the release's C_ItemUse on TCP)");
            Assert.AreEqual("아이템 사용 결과를 기다리는 중입니다.", InventoryPanelProbe.Status);
            Assert.IsFalse(InventoryPanelProbe.Interactable(use), "R6 no second use while one is outstanding");
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency, "R6 the client never adds the +50 itself");

            yield return InventoryPanelProbe.Click(_fixture.Mouse, use);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_ItemUse), "R6 a repeated click is not resent");

            _fixture.Peer.Send(new S_ItemUseResult { result = (byte)InventoryResult.Success, itemId = (int)ItemId.CoinPouch, revision = 2 }.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "사용 승인 · 목록을 갱신 중입니다.", "Success before its snapshot", 3);
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency, "R6/R7 a result alone confirms no value");
            Assert.AreEqual("재화 주머니 ×1", InventoryPanelProbe.Slot(1));

            _fixture.Peer.Send(Snapshot(2, 60, (ItemId.Material, 2)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Currency == "재화 60", "the following snapshot", 3);
            Assert.AreEqual("재료 ×2", InventoryPanelProbe.Slot(0));
            Assert.AreEqual("빈 슬롯", InventoryPanelProbe.Slot(1));
            Assert.IsFalse(InventoryPanelProbe.Use(1).gameObject.activeSelf, "the spent pouch row offers no use");
            Assert.AreEqual("아이템 사용이 완료되었습니다.", InventoryPanelProbe.Result);
            Assert.AreEqual("서버 확인 완료", InventoryPanelProbe.Status);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_InventoryRequest), "a delivered snapshot needs no requery");
        }

        [UnityTest]
        public IEnumerator RealTown_EveryPanelLabelIsDrawnOnScreen()
        {
            yield return ReadySyncedTown();
            yield return _fixture.Frames(2);
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency, "fixture: the synchronized snapshot is the source text");
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
        }

        [UnityTest]
        public IEnumerator MapTransition_RebuildsOnePanelWithTheStoredMirror_RequeriesAfterReady_AndEndingHasNone()
        {
            yield return ReadySyncedTown();
            InventoryState mirror = InventoryState.Instance;
            InventoryRequestController requests = _fixture.Session.Inventory;
            InventoryPanel townPanel = InventoryPanel.Instance;

            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady &&
                InventoryPanel.Instance != null && InventoryPanel.Instance != townPanel, "HuntingGround panel before fresh HP", 10);
            yield return _fixture.Frames(2);
            Assert.AreEqual(1, InventoryPanelProbe.LivePanels, "R8 the old scene panel is gone and one new panel exists");
            Assert.AreSame(mirror, InventoryState.Instance, "R8 the same connection keeps its mirror");
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency, "stored mirror is rebound into the new scene");
            Assert.AreEqual("입장 준비 중 · 서버 확인 후 사용할 수 있어요.", InventoryPanelProbe.Status);
            Assert.IsFalse(InventoryPanelProbe.Interactable(InventoryPanelProbe.Use(1)), "R6 no use before the new entry is Ready");
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_InventoryRequest), "R4 no query while the entry waits");

            // R4: the kill reward push of the move gap is lost; only the requery after Ready can reveal it.
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return WaitQueries(2);
            Assert.AreEqual("보관된 목록 · 서버에서 최신 목록을 확인 중입니다.", InventoryPanelProbe.Status);
            _fixture.Peer.Send(Snapshot(2, 20, (ItemId.Material, 3), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "requery answer", 3);
            Assert.AreEqual("재화 20", InventoryPanelProbe.Currency);
            Assert.AreEqual("재료 ×3", InventoryPanelProbe.Slot(0));
            Assert.IsTrue(InventoryPanelProbe.Interactable(InventoryPanelProbe.Use(1)));
            object[] mirrorTargets = InventoryPanelProbe.Targets(mirror, "OnInventoryChanged");
            Assert.AreEqual(1, mirrorTargets.Count(target => target is InventoryPanel), "R8 the old panel left the mirror");
            Assert.Contains(InventoryPanel.Instance, mirrorTargets, "R8 the new panel follows the mirror");
            Assert.AreEqual(1, mirrorTargets.Count(target => target is HudController), "dungeon R1 the map's HUD follows the mirror once");
            Assert.AreEqual(2, mirrorTargets.Length, "nothing else follows the mirror");
            Assert.AreEqual(1, InventoryPanelProbe.Subscribers(requests, "Changed"), "R8 the old panel left the requests");

            _fixture.Peer.Send(new S_MapTransition { destMapId = 3, spawnX = 0, spawnY = 0 }.Write());
            yield return _fixture.WaitMap(3);
            yield return _fixture.Frames(6);
            Assert.IsNull(InventoryPanel.Instance, "R8 Ending has no economy UI");
            Assert.AreEqual(0, InventoryPanelProbe.LivePanels);
            Assert.AreEqual(0, InventoryPanelProbe.Subscribers(mirror, "OnInventoryChanged"));
            Assert.AreEqual(0, InventoryPanelProbe.Subscribers(requests, "Changed"));
            Assert.AreEqual(2, _fixture.Peer.Count(PacketID.C_InventoryRequest), "R4 Ending sends no query");
            Assert.AreEqual("Ending", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator PauseMenu_StopsPanelInput_AndResumeRestoresIt()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 0f, "Escape opens the pause menu", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Assert.IsFalse(InventoryPanelProbe.Group.interactable, "R8 the paused panel takes no input");
            Assert.IsFalse(InventoryPanelProbe.Group.blocksRaycasts);
            // No click while paused: the pause menu's own buttons may sit under the same point.

            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 1f, "Escape closes the pause menu", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Assert.IsTrue(InventoryPanelProbe.Group.interactable);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Use(1));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "use after resume", 3);
        }

        // Addendum check: the editor latency simulation shares MainThreadDispatcher's delayed queue, which
        // only drains its head and assumes one common delay. The inventory response timeout is 5 s.
        [UnityTest]
        public IEnumerator SimulatedLatencyIntents_AreNotHeldBehindTheInventoryTimeout()
        {
            yield return ReadySyncedTown();
            UnityClientSession.SimulatedLatencyMs = 50; // restored by the fixture
            int moves = _fixture.Peer.Count(PacketID.C_MoveIntent);
            float pressed = Time.realtimeSinceStartup;
            _fixture.Keys(Key.D);
            float deadline = pressed + 6f;
            while (_fixture.Peer.Count(PacketID.C_MoveIntent) == moves && Time.realtimeSinceStartup < deadline) yield return null;
            float waited = Time.realtimeSinceStartup - pressed;
            _fixture.Keys();
            Debug.Log($"[PR2 verifier] 50 ms simulated-latency move intent arrived after {waited:F2} s");
            Assert.Greater(_fixture.Peer.Count(PacketID.C_MoveIntent), moves, "the move intent was never sent");
            Assert.Less(waited, 1f, "a 50 ms simulated intent must not wait for the 5 s inventory timeout");
        }

        // #1 with R5 on the real MainThreadDispatcher (opus-pr2-review #1). Unlike the synced case above, the
        // query stays unanswered, so a live 5 s deadline is pending while 50 ms intents are sent. The deadline
        // must still retry about every 5 s (client.md "5초 간격 … 최대 3번"), stop after three queries, show the
        // timeout, and an answer must remove it; losing the timer would fail the spacing checks.
        [UnityTest]
        public IEnumerator UnansweredQuery_RealDispatcherRetriesEveryFiveSeconds_WhileFiftyMsIntentsStayPrompt()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return WaitQueries(1);
            float firstQuery = Time.realtimeSinceStartup;
            UnityClientSession.SimulatedLatencyMs = 50; // restored by the fixture

            float firstMove = -1f;
            yield return MoveIntentDelay(Key.D, waited => firstMove = waited);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 2, "second query", 8);
            float secondQuery = Time.realtimeSinceStartup;
            float secondMove = -1f;
            yield return MoveIntentDelay(Key.A, waited => secondMove = waited);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 3, "third query", 8);
            float thirdQuery = Time.realtimeSinceStartup;
            yield return RealSeconds(7f);
            int afterRound = _fixture.Peer.Count(PacketID.C_InventoryRequest);
            string timedOutStatus = InventoryPanelProbe.Status;
            Debug.Log($"[PR2 verifier] unanswered query spacing {secondQuery - firstQuery:F2} s, {thirdQuery - secondQuery:F2} s; " +
                $"50 ms intents with a live deadline arrived after {firstMove:F2} s and {secondMove:F2} s; queries after the round {afterRound}");
            InventoryPanelProbe.AssertEveryLabelIsDrawn();

            _fixture.Peer.Send(Snapshot(1, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "late answer", 3);
            yield return RealSeconds(6f);
            int afterAnswer = _fixture.Peer.Count(PacketID.C_InventoryRequest);

            Assert.That(firstMove, Is.InRange(0f, 1f), "#1 a 50 ms intent is not held behind the live 5 s deadline");
            Assert.That(secondMove, Is.InRange(0f, 1f), "#1 the rescheduled deadline does not hold intents either");
            Assert.That(secondQuery - firstQuery, Is.InRange(4.5f, 6.5f), "R5 the first deadline retries after about 5 s");
            Assert.That(thirdQuery - secondQuery, Is.InRange(4.5f, 6.5f), "R5 the second deadline retries after about 5 s");
            Assert.AreEqual(3, afterRound, "R5 at most three queries in one recovery round");
            Assert.AreEqual("응답이 없습니다. 새로고침해 주세요.", timedOutStatus, "R5 the missing answer is shown");
            Assert.AreEqual(3, afterAnswer, "R5 the answer removed the deadline; nothing fires later");
        }

        // #2/R8 with R9 positive controls (opus-pr2-review #2). A pointer click on Use or Refresh performs only
        // that UI action: no C_Attack and no local attack prediction, cooldown or commit lock. The same mouse on
        // the game world and Enter while the pointer rests on a panel button still attack, so the check cannot
        // pass because attacks are disabled.
        [UnityTest]
        public IEnumerator PanelPointerClicks_StartNoAttack_WhileWorldClicksAndKeyboardOverThePanelStillAttack()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            LocalPlayerMovement player = _fixture.Player;
            yield return InventoryPanelProbe.AttackReady(player);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);

            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Use(1));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "C_ItemUse from the pointer", 3);
            int attacksByUse = _fixture.Peer.Count(PacketID.C_Attack) - attacks;
            bool cooldownByUse = !player.CanAttack;
            bool lockByUse = player.IsActionLocked;
            Assert.AreEqual(0, attacksByUse, "#2 a Use click sends no C_Attack");
            Assert.IsFalse(cooldownByUse, "#2 a Use click starts no predicted attack cooldown");
            Assert.IsFalse(lockByUse, "#2 a Use click starts no attack commit lock");

            _fixture.Peer.Send(new S_ItemUseResult { result = (byte)InventoryResult.Success, itemId = (int)ItemId.CoinPouch, revision = 2 }.Write());
            _fixture.Peer.Send(Snapshot(2, 60, (ItemId.Material, 2)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Currency == "재화 60" &&
                InventoryPanelProbe.Status == "서버 확인 완료", "use answered", 3);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Refresh);
            yield return WaitQueries(2);
            int attacksByRefresh = _fixture.Peer.Count(PacketID.C_Attack) - attacks;
            bool cooldownByRefresh = !player.CanAttack;
            Assert.AreEqual(0, attacksByRefresh, "#2 a Refresh click sends no C_Attack");
            Assert.IsFalse(cooldownByRefresh, "#2 a Refresh click starts no predicted attack cooldown");
            _fixture.Peer.Send(Snapshot(2, 60, (ItemId.Material, 2)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "refresh answered", 3);

            // Positive control 1: the same mouse on the game world (the player's own position) still swings.
            Vector2 world = InventoryPanelProbe.PlayerScreenPoint(player);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, world);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1, "world click attack", 3);
            bool cooldownByWorldClick = !player.CanAttack;

            // Positive control 2: Enter while the pointer rests on the Refresh button still swings.
            yield return InventoryPanelProbe.AttackReady(player);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.ScreenPoint(InventoryPanelProbe.Refresh));
            _fixture.Keys(Key.Enter);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 2, "keyboard attack over the panel", 3);
            yield return _fixture.Frames(6);
            Debug.Log($"[PR2 verifier] world click point {world}; panel clicks sent C_Attack x{attacksByRefresh}; " +
                $"world click and Enter-over-panel sent x{_fixture.Peer.Count(PacketID.C_Attack) - attacks}");

            Assert.IsTrue(cooldownByWorldClick, "R9 control: a world click predicts its attack cooldown");
            Assert.AreEqual(2, _fixture.Peer.Count(PacketID.C_InventoryRequest), "R9 Enter over the panel does not press Refresh");
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_ItemUse), "R9 no extra use from the controls");
        }

        // #3/R6/R8 (opus-pr2-review #3): the longest texts the panel can show are laid out in full, not only set
        // as strings. Covered: the entry-wait status, PR1 maximum currency 1,000,000,000 with both catalog items
        // at the 99 stack limit, and every server result text including the default one. The panel's screen
        // rectangle and the other drawn UI it covers are logged for the screen review.
        [UnityTest]
        public IEnumerator LongestTexts_MaximumValuesAndEveryResult_AreDrawnOnScreen()
        {
            yield return _fixture.EnterScriptedTown();
            yield return _fixture.Frames(2);
            Assert.AreEqual("입장 준비 중 · 서버 확인 후 사용할 수 있어요.", InventoryPanelProbe.Status);
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return WaitQueries(1);
            _fixture.Peer.Send(Snapshot(1, 1_000_000_000, (ItemId.Material, 99), (ItemId.CoinPouch, 99)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "maximum snapshot", 3);
            Assert.AreEqual("재화 1,000,000,000", InventoryPanelProbe.Currency);
            Assert.AreEqual("재료 ×99", InventoryPanelProbe.Slot(0));
            Assert.AreEqual("재화 주머니 ×99", InventoryPanelProbe.Slot(1));
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
            LogPanelCoverage();

            var denials = new (InventoryResult Result, string Text)[]
            {
                (InventoryResult.NotOwned, "보유하지 않은 아이템입니다."),
                (InventoryResult.NotUsable, "사용할 수 없는 아이템입니다."),
                (InventoryResult.CurrencyCap, "재화 보관 한도에 도달했습니다."),
                (InventoryResult.InventoryFull, "인벤토리에 빈 공간이 없습니다."),
                (InventoryResult.RevisionExhausted, "현재 아이템을 사용할 수 없습니다."),
            };
            foreach (var (result, text) in denials)
            {
                yield return UseAndAnswer(new S_ItemUseResult { result = (byte)result, itemId = (int)ItemId.CoinPouch, revision = 1 });
                yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == text, $"{result} text", 3);
                InventoryPanelProbe.AssertEveryLabelIsDrawn();
            }

            yield return UseAndAnswer(new S_ItemUseResult { result = (byte)InventoryResult.Stale, itemId = (int)ItemId.CoinPouch, revision = 2 });
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "변경된 목록을 확인 중입니다.", "Stale before its snapshot", 3);
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
            _fixture.Peer.Send(Snapshot(2, 1_000_000_000, (ItemId.Material, 99), (ItemId.CoinPouch, 99)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "목록이 바뀌었습니다. 다시 선택해 주세요.", "Stale after its snapshot", 3);
            InventoryPanelProbe.AssertEveryLabelIsDrawn();

            yield return UseAndAnswer(new S_ItemUseResult { result = (byte)InventoryResult.Success, itemId = (int)ItemId.CoinPouch, revision = 3 });
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "사용 승인 · 목록을 갱신 중입니다.", "Success before its snapshot", 3);
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
            _fixture.Peer.Send(Snapshot(3, 999_999_950, (ItemId.Material, 99), (ItemId.CoinPouch, 98)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "아이템 사용이 완료되었습니다.", "Success after its snapshot", 3);
            Assert.AreEqual("재화 999,999,950", InventoryPanelProbe.Currency);
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
        }

        // #4/R8 (opus-pr2-fix1 #4): with the real additive HUD present, neither Town nor HuntingGround puts any HUD
        // graphic over the panel, the whole panel with the longest values and a result stays on the screen this run
        // has, and its Use/Refresh buttons still take real pointer clicks there without starting an attack.
        [UnityTest]
        public IEnumerator RealTownAndHuntingGround_HudGraphicsShareNoAreaWithThePanel_AndItsButtonsStillClick()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return WaitQueries(1);
            _fixture.Peer.Send(Snapshot(1, 1_000_000_000, (ItemId.Material, 99), (ItemId.CoinPouch, 99)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "maximum snapshot", 3);
            yield return HudOverlapProbe.AssertPanelClearOfHud("Town synced");

            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Use(1));
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "pointer Use beside the HUD", 3);
            _fixture.Peer.Send(new S_ItemUseResult { result = (byte)InventoryResult.Success, itemId = (int)ItemId.CoinPouch, revision = 2 }.Write());
            _fixture.Peer.Send(Snapshot(2, 999_999_950, (ItemId.Material, 99), (ItemId.CoinPouch, 98)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Result == "아이템 사용이 완료되었습니다.", "use confirmed", 3);
            yield return HudOverlapProbe.AssertPanelClearOfHud("Town use result");
            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Refresh);
            yield return WaitQueries(2);
            _fixture.Peer.Send(Snapshot(2, 999_999_950, (ItemId.Material, 99), (ItemId.CoinPouch, 98)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "refresh answered", 3);
            Assert.AreEqual(attacks, _fixture.Peer.Count(PacketID.C_Attack), "#2 the two panel clicks beside the HUD sent no C_Attack");

            InventoryPanel townPanel = InventoryPanel.Instance;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady &&
                InventoryPanel.Instance != null && InventoryPanel.Instance != townPanel, "HuntingGround panel before fresh HP", 10);
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return WaitQueries(3);
            _fixture.Peer.Send(Snapshot(2, 999_999_950, (ItemId.Material, 99), (ItemId.CoinPouch, 98)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "HuntingGround requery answer", 3);
            Assert.AreEqual("HuntingGround", SceneManager.GetActiveScene().name);
            yield return HudOverlapProbe.AssertPanelClearOfHud("HuntingGround synced");
        }

        IEnumerator UseAndAnswer(S_ItemUseResult answer)
        {
            int uses = _fixture.Peer.Count(PacketID.C_ItemUse);
            Component use = InventoryPanelProbe.Use(1);
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Interactable(use), "usable pouch", 3);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, use);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == uses + 1, "pointer use", 3);
            _fixture.Peer.Send(answer.Write());
        }

        // Real-time measurement of one move intent after a key press; the key is released afterwards.
        IEnumerator MoveIntentDelay(Key key, Action<float> report)
        {
            int moves = _fixture.Peer.Count(PacketID.C_MoveIntent);
            float pressed = Time.realtimeSinceStartup;
            _fixture.Keys(key);
            float deadline = pressed + 6f;
            while (_fixture.Peer.Count(PacketID.C_MoveIntent) == moves && Time.realtimeSinceStartup < deadline) yield return null;
            report(_fixture.Peer.Count(PacketID.C_MoveIntent) > moves ? Time.realtimeSinceStartup - pressed : float.PositiveInfinity);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        static IEnumerator RealSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        // Screen review aid only (no assertion): the panel rectangle and every other drawn overlay element it
        // overlaps, with its canvas order, read from RectTransform corners of ScreenSpaceOverlay canvases.
        static void LogPanelCoverage()
        {
            var panel = (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
            Rect panelRect = OverlayRect(panel);
            var covered = new System.Collections.Generic.List<string>();
            foreach (CanvasRenderer renderer in UnityEngine.Object.FindObjectsByType<CanvasRenderer>())
            {
                if (!renderer.gameObject.activeInHierarchy || renderer.transform.IsChildOf(InventoryPanelProbe.Root)) continue;
                Canvas canvas = renderer.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || !(renderer.transform is RectTransform rect)) continue;
                if (renderer.GetAlpha() <= 0f || renderer.cull) continue;
                Rect other = OverlayRect(rect);
                if (other.Overlaps(panelRect)) covered.Add($"{canvas.rootCanvas.name}#{canvas.rootCanvas.sortingOrder}/{rect.name} {other}");
            }
            Debug.Log($"[PR2 verifier] screen {Screen.width}x{Screen.height}; inventory panel {panelRect}; overlapping drawn overlay UI " +
                $"{covered.Count}: {string.Join("; ", covered)}");
        }

        static Rect OverlayRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
