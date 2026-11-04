using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // T6/T7: real scene/input/connection path with the existing scripted TCP authority. No state injection and no
    // assumed mouse-to-attack binding. Every negative-use scenario has a positive use in the same test.
    public sealed class InventoryIconGridSceneTests
    {
        MapEntryPlayFixture _fixture;
        readonly List<GameObject> _owned = new List<GameObject>();
        int Uses => _fixture.Peer.Count(PacketID.C_ItemUse);

        [SetUp]
        public void SetUp() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject owner in _owned) if (owner != null) UnityEngine.Object.Destroy(owner);
            _owned.Clear();
            yield return _fixture.Cleanup();
            Type placement = typeof(InventoryPanel).Assembly.GetType("Dawnholder.Client.UI.UiWindowPlacement", false);
            Assert.IsNotNull(placement, "fixture: existing window placement type");
            MethodInfo forget = placement.GetMethod("ForgetMovedPositions", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            Assert.IsNotNull(forget, "UiWindowPlacement.ForgetMovedPositions");
            forget.Invoke(null, null);
        }

        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            Assert.AreEqual("재화 —", InventoryPanelProbe.Currency, "fixture: no snapshot yet");
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "initial inventory query", 3);
            _fixture.Peer.Send(Snapshot(1));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "valid compact snapshot", 3);
            Assert.AreEqual(1u, InventoryState.Instance.Revision, "fixture: snapshot applied");
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
        }

        static ArraySegment<byte> Snapshot(uint revision)
        {
            var packet = new S_InventorySnapshot { revision = revision, currency = 10 };
            typeof(S_InventorySnapshot).GetField("slot0ItemId").SetValue(packet, (int)ItemId.Material);
            typeof(S_InventorySnapshot).GetField("slot0Count").SetValue(packet, 2);
            typeof(S_InventorySnapshot).GetField("slot1ItemId").SetValue(packet, (int)ItemId.CoinPouch);
            typeof(S_InventorySnapshot).GetField("slot1Count").SetValue(packet, 1);
            return packet.Write();
        }

        IEnumerator PositiveUse(int expected)
        {
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, 1);
            yield return MapEntryPlayFixture.Wait(() => Uses == expected, "positive right-click use", 3);
            Assert.AreEqual(expected, Uses, "positive control: one use per right click");
        }

        IEnumerator ClearPendingUse()
        {
            // A normal denial settles the one positive-control request without changing inventory values.
            _fixture.Peer.Send(new S_ItemUseResult { result = (byte)InventoryResult.NotUsable,
                itemId = (int)ItemId.CoinPouch, revision = 1 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Inventory.CanUse(ItemId.CoinPouch), "positive control settled", 3);
            Assert.AreEqual("재화 10", InventoryPanelProbe.Currency, "fixture: denial preserved server values");
        }

        [UnityTest]
        public IEnumerator T6_HoverShowsCompleteCatalogText_AndEmptyAndWorldPointsHideIt()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(0));
            AssertDetail("재료", "수량 2", false);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(1));
            Assert.AreEqual(50, ItemCatalog.For(ItemId.CoinPouch).CurrencyOnUse);
            AssertDetail("재화 주머니", "수량 1", true);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(2));
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "T6 empty slot hides detail");
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.PlayerScreenPoint(_fixture.Player));
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "T6 world point hides detail");
        }

        [UnityTest]
        public IEnumerator T6_RightPressSendsOnePouchUse_NoAttackOnEveryFrame_AndNoRepeatWhilePending()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.AttackReady(_fixture.Player);
            Vector2 point = InventoryPanelProbe.SlotPoint(1);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, point);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = point }.WithButton(MouseButton.Right));
            for (int frame = 0; frame < 6; frame++)
            {
                yield return null;
                Assert.AreEqual(attacks, _fixture.Peer.Count(PacketID.C_Attack), "T6 no attack on press frame " + frame);
                Assert.IsTrue(_fixture.Player.CanAttack, "T6 no predicted cooldown on frame " + frame);
                Assert.IsFalse(_fixture.Player.IsActionLocked, "T6 no attack commit lock on frame " + frame);
                if (frame == 0) InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = point });
            }
            yield return MapEntryPlayFixture.Wait(() => Uses == 1, "positive use after right press", 3);
            Assert.AreEqual(1, Uses, "T6 positive use exactly once");
            C_ItemUse sent = _fixture.Peer.Last<C_ItemUse>(PacketID.C_ItemUse);
            Assert.AreEqual((int)ItemId.CoinPouch, sent.itemId);
            Assert.AreEqual(1u, sent.expectedRevision);
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, 1);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, Uses, "T6 pending request is not sent again");
        }

        [UnityTest]
        public IEnumerator T6_MaterialEmptyAndLeftPressSendNothing_AfterAConfirmedPositiveControl()
        {
            yield return ReadySyncedTown();
            yield return PositiveUse(1);
            yield return ClearPendingUse();
            foreach (int index in new[] { 0, 2 })
            {
                yield return InventoryPanelProbe.RightClick(_fixture.Mouse, index);
                yield return _fixture.Frames(6);
                Assert.AreEqual(1, Uses, "T6 zero additional use for ineligible slot " + index);
            }
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(1));
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, Uses, "T6 left click sends no use");
            Assert.AreEqual(attacks, _fixture.Peer.Count(PacketID.C_Attack), "T6 left click sends no attack");
        }

        [UnityTest]
        public IEnumerator T6_OpaqueNonRaycastOverlayBlocksUse_AndRemovingItRestoresUse()
        {
            yield return ReadySyncedTown();
            yield return PositiveUse(1);
            yield return ClearPendingUse();
            GameObject cover = CoverScreen();
            yield return _fixture.Frames(2);
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, 1);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, Uses, "T6 covering graphic with raycastTarget false blocks use");
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "T6 cover hides detail");
            UnityEngine.Object.Destroy(cover);
            yield return _fixture.Frames(2);
            yield return PositiveUse(2);
        }

        [UnityTest]
        public IEnumerator T6_ClosedAndPausedPanelIgnoreUse_AndResumeRestoresUse()
        {
            yield return ReadySyncedTown();
            yield return PositiveUse(1);
            yield return ClearPendingUse();
            Vector2 point = InventoryPanelProbe.SlotPoint(1);
            yield return PressKey(Key.I);
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "fixture: I closed the panel");
            yield return InventoryPanelProbe.RightClickAt(_fixture.Mouse, point);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, Uses, "T6 closed use adds no packet");
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "T6 closed detail off");
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 0f, "pause", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Assert.IsFalse(InventoryPanelProbe.Group.blocksRaycasts, "fixture: paused panel locked");
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, 1);
            yield return _fixture.Frames(6);
            Assert.AreEqual(1, Uses, "T6 pause adds no use");
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "T6 paused detail off");
            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 1f, "resume", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            yield return PositiveUse(2);
        }

        [UnityTest]
        public IEnumerator T6_AfterDraggingToTheRightEdge_AllReachableDetailsStayOnScreen()
        {
            yield return ReadySyncedTown();
            yield return ScreenUiProbe.WaitForEventSystem();
            var panel = (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
            var grab = (RectTransform)panel.Find("Header/GrabArea");
            Assert.IsNotNull(grab, "fixture: title bar grab area");
            Vector2 from = InventoryPanelProbe.ScreenPoint(grab);
            Vector2 destination = new Vector2(Screen.width - 1, 1);
            yield return Drag(from, destination - from);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect moved = ScreenUiProbe.ScreenRect(panel);
            AssertInsideScreen(moved, "T6 moved panel");
            Assert.AreEqual(Screen.width, moved.xMax, 1f, "fixture: actual drag reached the right screen edge");
            var pouch = (RectTransform)panel.Find("Slot2");
            Vector2 lowerRight = RectTransformUtility.WorldToScreenPoint(null, pouch.TransformPoint(new Vector2(pouch.rect.xMax - 2, pouch.rect.yMin + 2)));
            var points = new[] { (Name: "Slot1 가운데", Index: 0, Point: InventoryPanelProbe.SlotPoint(0)),
                (Name: "Slot2 가운데", Index: 1, Point: InventoryPanelProbe.SlotPoint(1)),
                (Name: "Slot2 오른쪽 아래 안쪽", Index: 1, Point: lowerRight) };
            foreach (var sample in points)
            {
                Vector2 point = sample.Point;
                yield return InventoryPanelProbe.PointAt(_fixture.Mouse, point);
                string diagnostic = PointDiagnostics(sample.Name, sample.Index, point, moved);
                Debug.Log("[T6 point] " + diagnostic);
                Assert.IsTrue(InventoryPanelProbe.DetailShown, "T6 occupied point after dragging: " + diagnostic);
                AssertInsideScreen(ScreenUiProbe.ScreenRect(InventoryPanelProbe.Detail), "T6 whole detail after dragging: " + diagnostic);
                InventoryPanelProbe.AssertEveryLabelIsDrawn();
            }
        }

        [UnityTest]
        public IEnumerator T7_TransitionAndPreSnapshotFramesHideDetailAndRejectUse_ThenSnapshotAllowsOne()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, InventoryPanelProbe.SlotPoint(1));
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "fixture: transition starts while hovering the pouch");
            Vector2 oldPoint = InventoryPanelProbe.SlotPoint(1);
            InventoryPanel oldPanel = InventoryPanel.Instance;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1, "transition begins", 10);
            int sampledFrames = 0;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!_fixture.Session.Entry.SceneReady || InventoryPanel.Instance == null || InventoryPanel.Instance == oldPanel || sampledFrames < 6)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "fixture: new scene loads before deadline");
                Assert.IsFalse(_fixture.Session.Entry.IsGameplayReady, "fixture: fresh HP is still withheld");
                Vector2 point = InventoryPanel.Instance != null ? InventoryPanelProbe.SlotPoint(1) : oldPoint;
                MouseState state = new MouseState { position = point };
                if (sampledFrames % 2 == 0) state = state.WithButton(MouseButton.Right);
                InputSystem.QueueStateEvent(_fixture.Mouse, state);
                yield return null;
                sampledFrames++;
                if (InventoryPanel.Instance != null) Assert.IsFalse(InventoryPanelProbe.DetailShown, "T7 no detail on pre-Ready frame " + sampledFrames);
                Assert.AreEqual(0, Uses, "T7 repeated pre-Ready clicks send nothing");
            }
            Assert.GreaterOrEqual(sampledFrames, 6, "fixture: multiple pre-Ready presses were sampled");
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = InventoryPanelProbe.SlotPoint(1) });
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 2, "map inventory requery", 3);
            Assert.AreEqual("보관된 목록 · 서버에서 최신 목록을 확인 중입니다.", InventoryPanelProbe.Status);
            yield return InventoryPanelProbe.RightClick(_fixture.Mouse, 1);
            yield return _fixture.Frames(6);
            Assert.AreEqual(0, Uses, "T7 Ready but snapshot still pending");
            _fixture.Peer.Send(Snapshot(2));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "map snapshot applied", 3);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            // This positive is deliberately last: all earlier zero observations need this same-test control.
            yield return PositiveUse(1);
        }

        [UnityTest]
        public IEnumerator T6_PointDiagnostics_ReportCoverAndRemoveIt_WhenDetailsRecover()
        {
            yield return ReadySyncedTown();
            GameObject cover = CoverScreen();
            yield return _fixture.Frames(2);
            Vector2 point = InventoryPanelProbe.SlotPoint(1);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, point);
            Assert.IsFalse(InventoryPanelProbe.DetailShown, "H9 fixture: the opaque cover hides detail");
            var panel = (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
            string covered = PointDiagnostics("Slot2 가운데", 1, point, ScreenUiProbe.ScreenRect(panel));
            StringAssert.Contains("Opaque cover", covered, "H9 positive diagnostic: visible cover named");
            StringAssert.Contains("Slot2", covered, "H9 diagnostic identifies the occupied slot");
            UnityEngine.Object.Destroy(cover);
            yield return _fixture.Frames(2);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, point);
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "H9 positive control: same point recovers detail without the cover");
            string clear = PointDiagnostics("Slot2 가운데", 1, point, ScreenUiProbe.ScreenRect(panel));
            StringAssert.DoesNotContain("Opaque cover", clear, "H9 removed cover is absent from diagnostic candidates");
        }

        // Observation only: candidates explain branches but never replace the product's detail/use decision.
        string PointDiagnostics(string name, int index, Vector2 point, Rect moved)
        {
            Transform root = InventoryPanelProbe.Root;
            var slot = (RectTransform)root.Find("Inventory/Slot" + (index + 1));
            Rect slotRect = ScreenUiProbe.ScreenRect(slot);
            Canvas canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            CanvasGroup group = InventoryPanelProbe.Group;
            var candidates = new List<string>();
            foreach (CanvasRenderer renderer in UnityEngine.Object.FindObjectsByType<CanvasRenderer>())
            {
                if (!renderer.gameObject.activeInHierarchy || renderer.transform.IsChildOf(root) || renderer.cull ||
                    renderer.GetAlpha() * renderer.GetInheritedAlpha() <= 0f) continue;
                Canvas higher = renderer.GetComponentInParent<Canvas>();
                if (higher == null) continue;
                higher = higher.rootCanvas;
                if (!higher.isActiveAndEnabled || higher.renderMode != RenderMode.ScreenSpaceOverlay || higher.sortingOrder <= canvas.sortingOrder) continue;
                var rect = renderer.transform as RectTransform;
                if (rect == null) continue;
                Rect box = ScreenUiProbe.ScreenRect(rect);
                if (!box.Contains(point)) continue;
                string candidate = $"{higher.name}#{higher.sortingOrder}/{ScreenUiProbe.PathOf(rect, higher.transform)} {box}";
                var text = renderer.GetComponent<TMPro.TMP_Text>();
                if (text != null) candidate += " ink=" + ScreenUiProbe.Bounds(rect, text.textBounds);
                candidates.Add(candidate);
            }
            candidates.Sort(StringComparer.Ordinal);
            return $"{name}; Slot{index + 1}; point={point}; screen={Screen.width}x{Screen.height}; moved={moved}; " +
                $"mouse={_fixture.Mouse.position.ReadValue()}; group alpha={group.alpha} blocksRaycasts={group.blocksRaycasts}; " +
                $"slotRect={slotRect} contains={slotRect.Contains(point)} display='{InventoryPanelProbe.Slot(index)}'; " +
                $"canvas={ScreenUiProbe.ScreenRect((RectTransform)canvas.transform)} scale={canvas.scaleFactor} order={canvas.sortingOrder}; " +
                $"detailSizeDelta={InventoryPanelProbe.Detail.sizeDelta}; covers=[{string.Join("; ", candidates)}]";
        }


        void AssertDetail(string name, string count, bool usable)
        {
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "T6 hover detail visible");
            Assert.AreEqual(name, InventoryPanelProbe.DetailLabel("Name").text);
            Assert.AreEqual(count, InventoryPanelProbe.DetailLabel("Count").text);
            Assert.AreEqual(usable, InventoryPanelProbe.DetailLabel("Effect").gameObject.activeInHierarchy);
            Assert.AreEqual(usable, InventoryPanelProbe.DetailLabel("UseHint").gameObject.activeInHierarchy);
            if (usable)
            {
                Assert.AreEqual("사용 효과: 금화 +50", InventoryPanelProbe.DetailLabel("Effect").text);
                Assert.AreEqual("우클릭: 사용", InventoryPanelProbe.DetailLabel("UseHint").text);
            }
            InventoryPanelProbe.AssertEveryLabelIsDrawn();
        }

        GameObject CoverScreen()
        {
            var root = new GameObject("T6 non-raycast occluding overlay", typeof(RectTransform), typeof(Canvas));
            _owned.Add(root);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            Type imageType = Type.GetType("UnityEngine.UI.Image, UnityEngine.UI", false);
            Assert.IsNotNull(imageType, "fixture: runtime uGUI Image");
            var imageObject = new GameObject("Opaque cover", typeof(RectTransform));
            imageObject.transform.SetParent(root.transform, false);
            Component image = imageObject.AddComponent(imageType);
            imageType.GetProperty("raycastTarget").SetValue(image, false);
            imageType.GetProperty("color").SetValue(image, Color.white);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return root;
        }

        IEnumerator PressKey(Key key)
        {
            _fixture.Keys(key); yield return _fixture.Frames(2);
            _fixture.Keys(); yield return _fixture.Frames(2);
        }

        IEnumerator Drag(Vector2 from, Vector2 delta)
        {
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = from });
            yield return _fixture.Frames(2);
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = from }.WithButton(MouseButton.Left));
            yield return _fixture.Frames(2);
            for (int step = 1; step <= 6; step++)
            {
                InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = from + delta * step / 6 }.WithButton(MouseButton.Left));
                yield return _fixture.Frames(2);
            }
            InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = from + delta });
            yield return _fixture.Frames(2);
        }

        static void AssertInsideScreen(Rect rect, string message) => Assert.IsTrue(rect.xMin >= -.5f && rect.yMin >= -.5f &&
            rect.xMax <= Screen.width + .5f && rect.yMax <= Screen.height + .5f, $"{message}: {rect} in {Screen.width}x{Screen.height}");
    }
}
