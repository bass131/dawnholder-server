using System;
using System.Buffers.Binary;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // The inventory's default place, title bar drag and remembered place in the real Town/HuntingGround/UI scenes, written
    // before the implementation (goal 2026-10-30-window-placement-inventory 완료조건 2·3·4·5; user decisions 68 C, 69 A,
    // 70 A; contract placement-red-contract-v1 T2·T4·T5·T6). Scripted authority on a real TCP socket, the fixture's
    // Input System mouse and keyboard, and live ScreenSpaceOverlay rectangles (HudOverlapProbe's convention); nothing is
    // derived from the products' placement code. The shared placement part is a new type the base commit does not have,
    // and this assembly sees no internals, so the part, the grab area and the attack gate are reached by name and reflection.
    public sealed class InventoryWindowPlacementSceneTests
    {
        // The shared placement part (contract 「부품 API」).
        const string PlacementTypeName = "Dawnholder.Client.UI.UiWindowPlacement";
        const string GrabAreaPath = "Inventory/Header/GrabArea";

        // Goal 「만들 것」: the default place keeps 12 UI units from the screen's left edge.
        const float LeftMargin = 12f;

        // Half a pixel absorbs float corners of rectangles that end exactly on an edge (HudOverlapProbe's Contains).
        const float EdgeTolerance = 0.5f;

        // A drag a player makes on the title bar, in screen pixels, over DragSteps moves. Each step (30, 6) is longer than
        // the EventSystem's default 10-pixel drag threshold, so the first move already counts as dragging.
        static readonly Vector2 s_drag = new Vector2(120f, 24f);
        const int DragSteps = 4;

        // The dungeon reward result (goal P2's wire, written independently as DungeonClearRewardOverlapSceneTests does).
        const ushort ClearResultId = 39;
        const int ClearResultLength = 33;
        const string ConfirmLabel = "확인";

        MapEntryPlayFixture _fixture;

        static Rect ScreenArea => new Rect(0f, 0f, Screen.width, Screen.height);
        static RectTransform Panel => (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
        static Rect PanelRect => ScreenUiProbe.ScreenRect(Panel);
        static float PanelScale => Panel.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;

        static RectTransform CharacterStatus => HudController.Instance == null ? null : HudController.Instance
            .GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(rect => rect.name == "character_status");

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();

        // Moved places live in a static table for the whole game session (69 A); every test starts from the default place.
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Exception forgetFailure = null;
            try { ForgetMovedPositions(required: false); }
            catch (Exception error) { forgetFailure = error; }
            yield return _fixture.Cleanup();
            if (forgetFailure != null) throw forgetFailure;
        }

        // ---------------------------------------------------------------------------------------------------------
        // T2 — default place
        // ---------------------------------------------------------------------------------------------------------

        // T2: the panel I opens in Town starts 12 scaled pixels from the screen's left edge, below the HUD's
        // character_status measured in the same frame, and wholly on screen.
        [UnityTest]
        public IEnumerator TownPanel_OpensTwelveScaledUnitsFromTheScreenLeft_BelowCharacterStatus_OnScreen()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();

            AssertDefaultPlace("Town after I");
        }

        // ---------------------------------------------------------------------------------------------------------
        // T4 — grab area against the attack gate
        // ---------------------------------------------------------------------------------------------------------

        // T4 (가): the pointer-over-UI check the attack path uses (GameplayPointerInput.IsOverUi through the player's
        // LocalPlayerInput) reads the grab area as UI and the player's own spot on the game world as not UI.
        [UnityTest]
        public IEnumerator GrabArea_IsUiForTheAttackGate_AndTheGameWorldIsNot()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            RectTransform grabArea = GrabArea();

            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, Centre(grabArea));
            bool overGrabArea = PointerIsOverUi();
            Vector2 world = InventoryPanelProbe.PlayerScreenPoint(_fixture.Player);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, world);
            bool overWorld = PointerIsOverUi();
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; grab area {ScreenUiProbe.ScreenRect(grabArea)}; " +
                $"world point {world}; over UI on the grab area {overGrabArea}, on the world {overWorld}");

            Assert.IsTrue(overGrabArea, "T4 (가) the attack gate reads the pointer on the grab area as UI");
            Assert.IsFalse(overWorld, "T4 (가) control: the attack gate reads the player's spot on the game world as not UI");
        }

        // T4 (나) and T5 끌기: a real mouse press, drag and release on the grab area moves the panel by the drag on screen,
        // and on every frame of it sends no C_Attack, starts no predicted attack cooldown and no attack commit lock.
        [UnityTest]
        public IEnumerator TitleBarDrag_MovesThePanelByTheDrag_AndStartsNoAttackOnAnyFrame()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            LocalPlayerMovement player = _fixture.Player;
            yield return InventoryPanelProbe.AttackReady(player);
            RectTransform grabArea = GrabArea();
            Rect before = PanelRect;
            AssertDragTargetOnScreen(before, s_drag);

            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            int frames = 0, attackFrames = 0, cooldownFrames = 0, lockFrames = 0;
            void Sample()
            {
                frames++;
                if (_fixture.Peer.Count(PacketID.C_Attack) != attacks) attackFrames++;
                if (!player.CanAttack) cooldownFrames++;
                if (player.IsActionLocked) lockFrames++;
            }
            yield return Drag(Centre(grabArea), s_drag, DragSteps, Sample);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                Sample();
            }
            Rect after = PanelRect;
            Vector2 moved = after.position - before.position;
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; panel {before} -> {after}, moved {moved} for a " +
                $"{s_drag} drag; frames {frames}, with a new C_Attack {attackFrames}, cooldown {cooldownFrames}, lock {lockFrames}");

            Assert.Greater(frames, DragSteps, "fixture: the drag was sampled on every frame");
            Assert.AreEqual(0, attackFrames, "T4 (나) a grab area drag sends no C_Attack");
            Assert.AreEqual(0, cooldownFrames, "T4 (나) a grab area drag starts no predicted attack cooldown");
            Assert.AreEqual(0, lockFrames, "T4 (나) a grab area drag starts no attack commit lock");
            Assert.AreEqual(s_drag.x, moved.x, EdgeTolerance, "T5 the panel moves right on screen by the drag's x");
            Assert.AreEqual(s_drag.y, moved.y, EdgeTolerance, "T5 the panel moves up on screen by the drag's y");
            Assert.AreEqual(before.size.x, after.size.x, EdgeTolerance, "T5 dragging keeps the panel's width on screen");
            Assert.AreEqual(before.size.y, after.size.y, EdgeTolerance, "T5 dragging keeps the panel's height on screen");
        }

        // T4 (다) positive control: with the pointer resting on the grab area, the keyboard control the player's
        // PlayerInput binds to "Attack" (found at run time, no key named here) still swings once.
        [UnityTest]
        public IEnumerator KeyboardAttack_WithThePointerOnTheGrabArea_StillSwingsOnce()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            LocalPlayerMovement player = _fixture.Player;
            RectTransform grabArea = GrabArea();
            InputAction attack = player.GetComponent<PlayerInput>().currentActionMap?.FindAction("Attack");
            Assert.IsNotNull(attack, "fixture: the player's PlayerInput has the Attack action");
            KeyControl key = attack.controls.OfType<KeyControl>().FirstOrDefault(control => control.device == _fixture.Keyboard);
            Assert.IsNotNull(key, "fixture: a key of the fixture keyboard is bound to Attack");

            yield return InventoryPanelProbe.AttackReady(player);
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, Centre(grabArea));
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            _fixture.Keys(key.keyCode);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            bool swung = false;
            yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1, 3f, met => swung = met);
            yield return _fixture.Frames(6);
            int sent = _fixture.Peer.Count(PacketID.C_Attack) - attacks;
            Debug.Log($"[placement RED] keyboard Attack control {key.path} over the grab area sent C_Attack x{sent}");

            Assert.IsTrue(swung, "T4 (다) the keyboard Attack over the grab area sends C_Attack");
            Assert.AreEqual(1, sent, "T4 (다) one key press is one attack");
        }

        // T4 버튼: after the panel is dragged, a Refresh click still asks the server again and the coin pouch's Use click
        // still sends one item use, the way InventorySceneLifecycleTests observes them.
        [UnityTest]
        public IEnumerator DraggedPanel_RefreshAndUseStillSendTheirRequests()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            RectTransform grabArea = GrabArea();
            Rect before = PanelRect;
            AssertDragTargetOnScreen(before, s_drag);
            yield return Drag(Centre(grabArea), s_drag, DragSteps, null);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect after = PanelRect;
            Assert.AreNotEqual(before.position, after.position, "precondition: the drag moved the panel");

            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Refresh);
            bool requeried = false;
            yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 2, 3f, met => requeried = met);
            // A newer revision answers the refresh, as InventorySceneLifecycleTests does (:718).
            _fixture.Peer.Send(Snapshot(2, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "refresh answered", 3);
            yield return InventoryPanelProbe.Click(_fixture.Mouse, InventoryPanelProbe.Use(1));
            bool used = false;
            yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, 3f, met => used = met);
            Debug.Log($"[placement RED] panel {before} -> {after}; queries {_fixture.Peer.Count(PacketID.C_InventoryRequest)}, " +
                $"item uses {_fixture.Peer.Count(PacketID.C_ItemUse)}");

            Assert.IsTrue(requeried, "T4 a Refresh click on the dragged panel sends C_InventoryRequest");
            Assert.IsTrue(used, "T4 a Use click on the dragged panel sends C_ItemUse");
            Assert.AreEqual((int)ItemId.CoinPouch, _fixture.Peer.Last<C_ItemUse>(PacketID.C_ItemUse).itemId, "T4 the use names the clicked slot's item");
        }

        // ---------------------------------------------------------------------------------------------------------
        // T5 — screen edges and the remembered place
        // ---------------------------------------------------------------------------------------------------------

        // T5 화면 끝: dragging the title bar to the screen's bottom-left corner, far enough that the panel would leave the
        // screen if it followed, stops it with all four corners on screen.
        [UnityTest]
        public IEnumerator FarDrag_StopsWithTheWholePanelOnScreen()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            RectTransform grabArea = GrabArea();
            Rect before = PanelRect;
            Vector2 from = Centre(grabArea);
            Vector2 far = new Vector2(1f, 1f) - from;
            Rect followed = new Rect(before.position + far, before.size);
            Assert.IsFalse(Contains(ScreenArea, followed), $"precondition: a panel following the drag would leave the screen ({followed})");

            yield return Drag(from, far, 6, null);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect after = PanelRect;
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; far drag {far}; panel {before} -> {after}");

            Assert.AreNotEqual(before.position, after.position, "T5 the far drag moved the panel");
            Assert.IsTrue(Contains(ScreenArea, after), $"T5 all four corners of the panel {after} stay on the {Screen.width}x{Screen.height} screen");
        }

        // T5 자리 기억 (69 A): the dragged place stays when I closes and reopens the panel, and when the panel is opened
        // again after a map transition; the stand-in for restarting the game (ForgetMovedPositions) brings back the T2
        // default place.
        [UnityTest]
        public IEnumerator DraggedPlace_StaysThroughIAndAMapTransition_AndForgettingReturnsTheDefaultPlace()
        {
            yield return ReadySyncedTown();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            RectTransform grabArea = GrabArea();
            AssertDragTargetOnScreen(PanelRect, s_drag);
            yield return Drag(Centre(grabArea), s_drag, DragSteps, null);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect dragged = PanelRect;

            yield return PressI();
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "precondition: I closed the panel");
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect reopened = PanelRect;

            // The scripted authority moves the connection as InventorySceneLifecycleTests does (:455~:470): S_MapTransition,
            // fresh HP, then the requery after Ready answered with a newer revision. The scripted peer does not answer a
            // portal walk, which needs the real server (lead msg_9d17df8ce52c). Once the new scene, its HUD and its
            // inventory are ready, I closes the panel that stayed open and opens it again (HudGoldAndPanelToggleSceneTests:109~:116).
            InventoryPanel townPanel = InventoryPanel.Instance;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady &&
                InventoryPanel.Instance != null && InventoryPanel.Instance != townPanel, "HuntingGround panel", 10);
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 2,
                "C_InventoryRequest #2 after the map transition", 3);
            _fixture.Peer.Send(Snapshot(2, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "HuntingGround requery answer", 3);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha, "precondition: the next map's panel is open, as it was left");
            Rect stillOpen = PanelRect;
            yield return PressI();
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "precondition: I closed the next map's panel");
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Rect afterMove = PanelRect;
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; dragged {dragged}; reopened {reopened}; " +
                $"after the map transition still open {stillOpen}, reopened with I {afterMove}");

            Assert.IsTrue(Same(dragged, reopened), $"T5 closing and reopening with I keeps the dragged place {dragged}, got {reopened}");
            Assert.IsTrue(Same(dragged, afterMove), $"T5 reopening after the map transition keeps the dragged place {dragged}, got {afterMove}");

            ForgetMovedPositions(required: true);
            yield return PressI();
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            AssertDefaultPlace("HuntingGround after ForgetMovedPositions and I");
        }

        // ---------------------------------------------------------------------------------------------------------
        // T6 — the reward window does not read the inventory's place
        // ---------------------------------------------------------------------------------------------------------

        // T6: with the reward window up, its panel and 「확인」 keep the same screen rectangles whether the InventoryPanel
        // component is enabled or not, so the reward window no longer reserves the inventory's space.
        [UnityTest]
        public IEnumerator RewardWindow_KeepsItsPlace_WhetherTheInventoryPanelComponentIsEnabledOrNot()
        {
            yield return EnterReadyTown();
            yield return OpenSuccessWindow(13579);
            InventoryPanel inventory = InventoryPanel.Instance;
            Assert.IsNotNull(inventory, "fixture: Town owns the inventory panel");
            Assert.IsTrue(inventory.enabled, "precondition: the InventoryPanel component starts enabled");
            yield return WaitSettled(() => RewardRects().panel, "the reward window");
            (Rect panel, Rect confirm) enabled = RewardRects();

            inventory.enabled = false;
            try
            {
                yield return WaitSettled(() => RewardRects().panel, "the reward window with the inventory component disabled");
            }
            finally { inventory.enabled = true; }
            (Rect panel, Rect confirm) disabled = RewardRects();
            yield return WaitSettled(() => RewardRects().panel, "the reward window after the inventory component is enabled again");
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; reward panel enabled {enabled.panel} disabled " +
                $"{disabled.panel}; 「확인」 enabled {enabled.confirm} disabled {disabled.confirm}");

            Assert.IsTrue(Same(enabled.panel, disabled.panel),
                $"T6 the reward panel {enabled.panel} does not move when the InventoryPanel component is disabled ({disabled.panel})");
            Assert.IsTrue(Same(enabled.confirm, disabled.confirm),
                $"T6 「확인」 {enabled.confirm} does not move when the InventoryPanel component is disabled ({disabled.confirm})");
        }

        // T6 보존: with the reward window up, I opening and closing the inventory leaves 「확인」 where it was.
        [UnityTest]
        public IEnumerator RewardConfirm_StaysPut_WhileIOpensAndClosesTheInventory()
        {
            yield return EnterReadyTown();
            yield return OpenSuccessWindow(24680);
            yield return WaitSettled(() => RewardRects().panel, "the reward window");
            Rect closed = RewardRects().confirm;
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return WaitSettled(() => RewardRects().panel, "the reward window with the inventory open");
            Rect open = RewardRects().confirm;
            yield return PressI();
            yield return WaitSettled(() => RewardRects().panel, "the reward window after the inventory closed");
            Rect closedAgain = RewardRects().confirm;
            Debug.Log($"[placement RED] screen {Screen.width}x{Screen.height}; 「확인」 closed {closed} open {open} closed again {closedAgain}");

            Assert.IsTrue(Same(closed, open), $"T6 opening the inventory leaves 「확인」 {closed} in place, got {open}");
            Assert.IsTrue(Same(closed, closedAgain), $"T6 closing the inventory leaves 「확인」 {closed} in place, got {closedAgain}");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Scene setup (the same shapes as InventorySceneLifecycleTests and DungeonClearRewardOverlapSceneTests, whose
        // helpers are private)
        // ---------------------------------------------------------------------------------------------------------

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

        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "C_InventoryRequest #1", 3);
            _fixture.Peer.Send(Snapshot(1, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
        }

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(40, 100);
            yield return _fixture.WaitMap(0);
        }

        // One press and release of I through the fixture keyboard, each followed by two frames.
        IEnumerator PressI()
        {
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        // A success result with gold and both items, until exactly one 「확인」 is shown.
        IEnumerator OpenSuccessWindow(int gold)
        {
            _fixture.Peer.Send(new ArraySegment<byte>(ClearResult((byte)InventoryResult.Success, gold,
                ((int)ItemId.Material, 3), ((int)ItemId.CoinPouch, 7))));
            yield return MapEntryPlayFixture.Wait(() => ScreenUiProbe.ShownButtons(ConfirmLabel).Length == 1,
                "the reward window with one 「확인」", 3);
        }

        // [ushort size incl. header][ushort id 39], byte result, int gold, then three (int itemId, int count) slots, all
        // LittleEndian. Unused slots stay 0/0.
        static byte[] ClearResult(byte result, int gold, params (int itemId, int count)[] slots)
        {
            var wire = new byte[ClearResultLength];
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(0, 2), ClearResultLength);
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(2, 2), ClearResultId);
            wire[4] = result;
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(5, 4), gold);
            for (int i = 0; i < slots.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(9 + (i * 8), 4), slots[i].itemId);
                BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(13 + (i * 8), 4), slots[i].count);
            }
            return wire;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observers and the real mouse drag
        // ---------------------------------------------------------------------------------------------------------

        // T2's default place, judged on live rectangles of the same frame: left edge 12 UI units (scaled to pixels) from
        // the screen's left, top edge below character_status's bottom, the whole panel on screen.
        static void AssertDefaultPlace(string moment)
        {
            RectTransform status = CharacterStatus;
            Assert.IsNotNull(status, $"{moment}: precondition: the HUD shows character_status");
            Rect panel = PanelRect;
            Rect statusRect = ScreenUiProbe.ScreenRect(status);
            float scale = PanelScale;
            Debug.Log($"[placement RED] {moment}: screen {Screen.width}x{Screen.height}; panel {panel}; canvas scale {scale:F4}; " +
                $"character_status {statusRect}");

            Assert.AreEqual(1f, InventoryPanelProbe.Group.alpha, $"{moment}: precondition: the panel is shown");
            Assert.AreEqual(LeftMargin * scale, panel.xMin, EdgeTolerance, $"{moment}: T2 the panel's left edge is 12 scaled units from the screen left");
            Assert.LessOrEqual(panel.yMax, statusRect.yMin + EdgeTolerance,
                $"{moment}: T2 the panel's top {panel.yMax} is below character_status's bottom {statusRect.yMin}");
            Assert.IsTrue(Contains(ScreenArea, panel), $"{moment}: T2 the whole panel {panel} is on the {Screen.width}x{Screen.height} screen");
        }

        // The grab area under the title bar (contract 「부품 API」), on screen and hit first by a pointer at its centre.
        static RectTransform GrabArea()
        {
            var grabArea = (RectTransform)InventoryPanelProbe.Root.Find(GrabAreaPath);
            Assert.IsNotNull(grabArea, $"the title bar holds the grab area {GrabAreaPath}");
            Vector2 centre = Centre(grabArea);
            Assert.IsTrue(ScreenUiProbe.OnScreen(centre), $"fixture: the grab area centre {centre} is on screen");
            GameObject[] hits = ScreenUiProbe.RaycastHits(centre);
            Assert.IsTrue(hits.Length > 0 && hits[0] == grabArea.gameObject,
                $"fixture: a pointer at the grab area centre hits it first, hits [{string.Join(",", hits.Select(hit => hit.name))}]");
            return grabArea;
        }

        static Vector2 Centre(RectTransform rect) => ScreenUiProbe.ScreenRect(rect).center;

        // The drag must be judged on a move the screen leaves room for, so a stop at the edge is not read as a short drag.
        static void AssertDragTargetOnScreen(Rect panel, Vector2 drag)
        {
            var target = new Rect(panel.position + drag, panel.size);
            Assert.IsTrue(Contains(ScreenArea, target),
                $"fixture: the {drag} drag keeps the panel {panel} on the {Screen.width}x{Screen.height} screen ({target})");
        }

        // Left button down at `from`, `steps` moves to `from + delta` with the button held, then release there; two frames
        // after each event, as InventoryPanelProbe.ClickAt does. `onFrame` sees every frame of the drag.
        IEnumerator Drag(Vector2 from, Vector2 delta, int steps, Action onFrame)
        {
            Mouse mouse = _fixture.Mouse;
            IEnumerator Frames()
            {
                for (int i = 0; i < 2; i++)
                {
                    yield return null;
                    onFrame?.Invoke();
                }
            }
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from });
            yield return Frames();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left));
            yield return Frames();
            for (int step = 1; step <= steps; step++)
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = from + delta * step / steps }.WithButton(MouseButton.Left));
                yield return Frames();
            }
            InputSystem.QueueStateEvent(mouse, new MouseState { position = from + delta });
            yield return Frames();
        }

        // The attack path's own pointer-over-UI check: the player's LocalPlayerInput keeps a GameplayPointerInput in
        // _pointerInput (LocalPlayerInput.cs:31) and asks IsOverUi with the attack's active control (:76).
        bool PointerIsOverUi()
        {
            var input = _fixture.Player.GetComponent<Dawnholder.Client.Input.LocalPlayerInput>();
            Assert.IsNotNull(input, "fixture: the local player has LocalPlayerInput");
            object gate = MapEntryPlayFixture.Field<object>(input, "_pointerInput");
            MethodInfo isOverUi = gate.GetType().GetMethod("IsOverUi", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(InputControl) }, null);
            Assert.IsNotNull(isOverUi, "fixture: GameplayPointerInput.IsOverUi(InputControl)");
            return (bool)isOverUi.Invoke(gate, new object[] { _fixture.Mouse.leftButton });
        }

        // ForgetMovedPositions through reflection: before the part exists (the base commit) there is nothing to forget;
        // once the type exists, the method must too.
        static void ForgetMovedPositions(bool required)
        {
            Type part = typeof(InventoryPanel).Assembly.GetType(PlacementTypeName, false);
            if (part == null)
            {
                Assert.IsFalse(required, $"T5 {PlacementTypeName} exists to forget moved places");
                return;
            }
            MethodInfo forget = part.GetMethod("ForgetMovedPositions", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            Assert.IsNotNull(forget, $"{PlacementTypeName}.ForgetMovedPositions()");
            forget.Invoke(null, null);
        }

        // The reward panel is the top object below the reward canvas that holds the shown 「확인」.
        static (Rect panel, Rect confirm) RewardRects()
        {
            Component[] buttons = ScreenUiProbe.ShownButtons(ConfirmLabel);
            Assert.AreEqual(1, buttons.Length, "fixture: the reward window shows one 「확인」");
            Transform panel = buttons[0].transform;
            while (panel.parent != null && panel.parent.GetComponent<Canvas>() == null) panel = panel.parent;
            return (ScreenUiProbe.ScreenRect((RectTransform)panel), ScreenUiProbe.ScreenRect((RectTransform)buttons[0].transform));
        }

        static IEnumerator WaitSettled(Func<Rect> read, string label)
        {
            Rect previous = read();
            int still = 0;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (still < 2)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, label + " never settled");
                yield return null;
                Rect current = read();
                still = current == previous ? still + 1 : 0;
                previous = current;
            }
        }

        static bool Same(Rect a, Rect b) => Mathf.Abs(a.xMin - b.xMin) <= EdgeTolerance && Mathf.Abs(a.yMin - b.yMin) <= EdgeTolerance &&
            Mathf.Abs(a.xMax - b.xMax) <= EdgeTolerance && Mathf.Abs(a.yMax - b.yMax) <= EdgeTolerance;

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - EdgeTolerance && inner.yMin >= outer.yMin - EdgeTolerance &&
            inner.xMax <= outer.xMax + EdgeTolerance && inner.yMax <= outer.yMax + EdgeTolerance;
    }
}
