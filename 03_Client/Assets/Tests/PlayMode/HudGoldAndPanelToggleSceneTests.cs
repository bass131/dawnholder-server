using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Dungeon goal R1, R2, R4~R7 on the real scenes (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md
    // 「결함 PR의 요구」), where the EditMode tests use stand-ins: the additive UI scene's own HUD and gold label with
    // its scene font, the real Town/HuntingGround panels, the Input System keyboard and mouse through the real
    // EventSystem, and the real PauseMenuController. Authority is the scripted TCP peer; values are server literals.
    public sealed class HudGoldAndPanelToggleSceneTests
    {
        MapEntryPlayFixture _fixture;
        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // The UI scene's HUD shows no number until the first snapshot, then the server currency, the same as the
        // panel. Every character it draws is in the label's font (the HUD font atlas is static).
        [UnityTest]
        public IEnumerator RealUiHud_ShowsNoGoldUntilTheFirstSnapshot_ThenTheSameCurrencyAsThePanel()
        {
            yield return _fixture.EnterScriptedTown();
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null && HudController.Instance.isActiveAndEnabled,
                "additive HUD", 10);
            TMP_Text gold = MapEntryPlayFixture.Field<TMP_Text>(HudController.Instance, "_goldText");
            Assert.IsNotNull(gold, "fixture: the UI scene's HUD has its gold label");
            yield return _fixture.Frames(2);
            string beforeReady = Digits(gold.text);
            string[] missingBeforeSnapshot = MissingGlyphs(gold);
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return WaitQueries(1);
            string whileQueryWaits = Digits(gold.text);

            _fixture.Peer.Send(Snapshot(1, 1234));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "first snapshot", 3);
            string hudFirst = Digits(gold.text);
            string panelFirst = Digits(InventoryPanelProbe.Currency);
            _fixture.Peer.Send(Snapshot(2, 1_000_000_000));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Currency == "재화 1,000,000,000", "second snapshot", 3);
            string hudSecond = Digits(gold.text);
            string panelSecond = Digits(InventoryPanelProbe.Currency);
            string[] missingWithSnapshot = MissingGlyphs(gold);
            bool labelDrawn = gold.isActiveAndEnabled && gold.gameObject.activeInHierarchy;
            Debug.Log($"[PR1 verifier] HUD gold '{gold.text}' panel '{InventoryPanelProbe.Currency}' font '{gold.font.name}'");

            Assert.AreEqual(string.Empty, beforeReady, "R2 the real HUD shows no number before the first snapshot");
            Assert.AreEqual(string.Empty, whileQueryWaits, "R2 still no number while the Ready query waits");
            Assert.IsEmpty(missingBeforeSnapshot, "R2 the empty gold text uses only glyphs the HUD font has");
            Assert.AreEqual("1234", hudFirst, "R1 the real HUD shows the server currency");
            Assert.AreEqual(panelFirst, hudFirst, "R1 the HUD and the panel show the same currency");
            Assert.AreEqual("1000000000", hudSecond, "R1 the real HUD follows the next snapshot");
            Assert.AreEqual(panelSecond, hudSecond, "R1 the HUD and the panel still agree");
            Assert.IsEmpty(missingWithSnapshot, "R1 the gold text with a number uses only glyphs the HUD font has");
            Assert.IsTrue(labelDrawn, "R1 the gold label is drawn");
        }

        // While closed, nothing takes a raycast where the Use button lies, so a mouse click there presses no button,
        // and Left Ctrl with the pointer there still attacks (the mouse is not an attack binding, decision 65 A).
        // I opens the panel and the same click uses the pouch without an attack. A map move keeps it open and the
        // next I closes it.
        [UnityTest]
        public IEnumerator ClosedPanel_TakesNoClickOnItsUseSpotAndLeftCtrlAttacks_AndAfterIThatClickUses_AcrossAMapMove()
        {
            yield return ReadySyncedTown();
            LocalPlayerMovement player = _fixture.Player;
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            Vector2 useSpot = InventoryPanelProbe.ScreenPoint(InventoryPanelProbe.Use(1));
            LogPanelClickState("coordinate captured", useSpot);
            bool shownAtEntry = InventoryPanelProbe.Group.alpha > 0f;
            bool raycastsAtEntry = InventoryPanelProbe.Group.blocksRaycasts;

            LogPanelClickState("before closed click", useSpot);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, useSpot);
            yield return _fixture.Frames(4);
            int raycastHitsWhileClosed = ScreenUiProbe.RaycastHits(useSpot).Length;
            int usesWhileClosed = _fixture.Peer.Count(PacketID.C_ItemUse);
            yield return InventoryPanelProbe.AttackReady(player);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            _fixture.Keys(Key.LeftCtrl);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1,
                "Left Ctrl attacks with the pointer on the closed panel's Use spot", 3);

            yield return PressI();
            bool shownAfterI = InventoryPanelProbe.Group.alpha > 0f;
            yield return InventoryPanelProbe.AttackReady(player);
            int attacksBeforeOpenClick = _fixture.Peer.Count(PacketID.C_Attack);
            LogPanelClickState("before open click", useSpot);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, useSpot);
            try
            {
                yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "the open panel's Use", 3);
            }
            finally { LogPanelClickState("open Use wait ended, including a timeout", useSpot); }
            yield return _fixture.Frames(4);
            int attacksByOpenClick = _fixture.Peer.Count(PacketID.C_Attack) - attacksBeforeOpenClick;
            _fixture.Peer.Send(new S_ItemUseResult { result = (byte)InventoryResult.Success, itemId = (int)ItemId.CoinPouch, revision = 2 }.Write());
            _fixture.Peer.Send(Snapshot(2, 60, (ItemId.Material, 2)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Currency == "재화 60", "use answered", 3);

            InventoryPanel townPanel = InventoryPanel.Instance;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady &&
                InventoryPanel.Instance != null && InventoryPanel.Instance != townPanel, "HuntingGround panel", 10);
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return _fixture.Frames(2);
            bool shownAfterMove = InventoryPanelProbe.Group.alpha > 0f;
            yield return PressI();
            bool shownAfterClosingI = InventoryPanelProbe.Group.alpha > 0f;
            bool raycastsAfterClosingI = InventoryPanelProbe.Group.blocksRaycasts;

            Assert.IsFalse(shownAtEntry, "R5 the first map of the connection starts with the panel closed");
            Assert.IsFalse(raycastsAtEntry, "R6 the closed panel takes no raycasts");
            Assert.AreEqual(0, raycastHitsWhileClosed, "R6 nothing takes a raycast at the closed panel's Use spot");
            Assert.AreEqual(0, usesWhileClosed, "R6 a click on the closed panel presses no Use button");
            Assert.IsTrue(shownAfterI, "R4 I opens the panel");
            Assert.AreEqual(0, attacksByOpenClick, "R8 a click on the open panel's Use sends no C_Attack, as before");
            Assert.IsTrue(shownAfterMove, "R5 the next map's new panel is open, as it was left");
            Assert.IsFalse(shownAfterClosingI, "R4 the next I closes the panel");
            Assert.IsFalse(raycastsAfterClosingI, "R6 the panel closed by I takes no raycasts");
        }

        // The real pause menu (Escape, Time.timeScale 0) keeps I from toggling the panel; after resuming, I works.
        [UnityTest]
        public IEnumerator RealPauseMenu_KeepsIFromTogglingThePanel_UntilResumed()
        {
            yield return ReadySyncedTown();
            // Town readiness does not wait for additive UI; press Escape only after its real controller is active.
            yield return MapEntryPlayFixture.Wait(() =>
            {
                PauseMenuController pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
                return pause != null && pause.isActiveAndEnabled;
            }, "fixture: active additive pause menu controller", 10);
            LogPauseInput("before Escape");
            try
            {
                bool shownBeforePause = InventoryPanelProbe.Group.alpha > 0f;
                _fixture.Keys(Key.Escape);
                yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 0f, "Escape opens the pause menu", 3);
                _fixture.Keys();
                yield return _fixture.Frames(3);
                yield return PressI();
                bool shownWhilePaused = InventoryPanelProbe.Group.alpha > 0f;

                _fixture.Keys(Key.Escape);
                yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 1f, "Escape closes the pause menu", 3);
                _fixture.Keys();
                yield return _fixture.Frames(3);
                yield return PressI();
                bool shownAfterResume = InventoryPanelProbe.Group.alpha > 0f;

                Assert.IsFalse(shownBeforePause, "R5 precondition: the panel starts closed");
                Assert.IsFalse(shownWhilePaused, "R7 I changes nothing while the pause menu is open");
                Assert.IsTrue(shownAfterResume, "R7 control: after resuming, I opens the panel");
            }
            finally { LogPauseInput("after Escape scenario, including a timeout"); }
        }

        // Observe the original click without settling layout or changing its stored position. Logging can still
        // affect wall-clock timing; queried raycast hits are observations, not proof of a dispatched button event.
        void LogPanelClickState(string stage, Vector2 capturedSpot)
        {
            try
            {
                HudController hud = HudController.Instance;
                InventoryPanel panel = InventoryPanel.Instance;
                CanvasGroup group = panel == null ? null : panel.GetComponent<CanvasGroup>();
                Transform use = panel == null ? null : panel.transform.Find("Inventory/Slot2/Use");
                Component button = use == null ? null : use.GetComponents<Component>()
                    .FirstOrDefault(component => component != null && component.GetType().FullName == "UnityEngine.UI.Button");
                PlayerInput input = _fixture.Player == null ? null : _fixture.Player.GetComponent<PlayerInput>();
                string actions = input?.currentActionMap == null ? "unavailable: no current map" :
                    string.Join(", ", input.currentActionMap.actions.Select(action => $"{action.name}:{action.enabled}/{action.phase}"));
                string devices = input == null ? "unavailable: no PlayerInput" :
                    string.Join(", ", input.devices.Select(device => $"{device.name}#{device.deviceId}"));
                var ui = UnityEngine.SceneManagement.SceneManager.GetSceneByName("UI");
                Debug.Log($"[HUD panel click] {stage}: frame {Time.frameCount}, realtime {Time.realtimeSinceStartupAsDouble:R}, "
                    + $"screen {Screen.width}x{Screen.height}, stored {capturedSpot.ToString("F3")}, "
                    + $"UI valid {ui.IsValid()} loaded {ui.isLoaded}, HUD exists {hud != null} active {hud?.isActiveAndEnabled}, "
                    + $"HUD id {hud?.GetEntityId()}, panel id {panel?.GetEntityId()} active {panel?.isActiveAndEnabled}; "
                    + $"Use [{PanelClickRect(use as RectTransform, capturedSpot)}], "
                    + $"panel [{PanelClickRect(panel == null ? null : panel.transform.Find("Inventory") as RectTransform, capturedSpot)}], "
                    + $"character_status [{PanelClickRect(hud == null ? null : hud.transform.Find("character_status") as RectTransform, capturedSpot)}]; "
                    + $"group alpha {group?.alpha} interactable {group?.interactable} blocksRaycasts {group?.blocksRaycasts}, "
                    + $"button interactable {(button == null ? "unavailable: no button" : InventoryPanelProbe.Interactable(button).ToString())}, "
                    + $"session panelOpen unavailable: internal property, canSend {_fixture.Session?.CanSendGameplay} "
                    + $"entry {_fixture.Session?.Entry.State}, transition {_fixture.Loader?.IsTransitioning}, "
                    + $"player canAttack {_fixture.Player?.CanAttack} actionLocked {_fixture.Player?.IsActionLocked}; "
                    + $"fixture mouse {_fixture.Mouse?.name}#{_fixture.Mouse?.deviceId} added {_fixture.Mouse?.added} "
                    + $"enabled {_fixture.Mouse?.enabled} position {_fixture.Mouse?.position.ReadValue()} left {_fixture.Mouse?.leftButton.isPressed}, "
                    + $"current mouse {Mouse.current?.name}#{Mouse.current?.deviceId} position {Mouse.current?.position.ReadValue()} "
                    + $"left {Mouse.current?.leftButton.isPressed}, "
                    + $"fixture keyboard {_fixture.Keyboard?.name}#{_fixture.Keyboard?.deviceId} I {_fixture.Keyboard?.iKey.isPressed}, "
                    + $"current keyboard {Keyboard.current?.name}#{Keyboard.current?.deviceId} I {Keyboard.current?.iKey.isPressed}, "
                    + $"player map {input?.currentActionMap?.name} scheme {input?.currentControlScheme} devices [{devices}] actions [{actions}], "
                    + $"input update {InputSystem.settings.updateMode} focus {Application.isFocused} timeScale {Time.timeScale}; "
                    + $"packets attack {_fixture.Peer?.Count(PacketID.C_Attack)} use {_fixture.Peer?.Count(PacketID.C_ItemUse)}; "
                    + $"queried hits [{PanelClickHits(capturedSpot)}]");
            }
            catch (Exception error)
            {
                // A missing diagnostic must not replace the original scenario's assertion or timeout.
                Debug.Log($"[HUD panel click] {stage}: observation unavailable: {error.GetType().Name}: {error.Message}");
            }
        }

        static string PanelClickRect(RectTransform rect, Vector2 capturedSpot)
        {
            if (rect == null) return "unavailable: no RectTransform";
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return $"unavailable: expected overlay canvas, actual {canvas?.rootCanvas.renderMode}";
            Rect screen = ScreenUiProbe.ScreenRect(rect);
            return $"active {rect.gameObject.activeInHierarchy}, screen {screen.ToString("F3")}, "
                + $"center {screen.center.ToString("F3")}, containsStored {screen.Contains(capturedSpot)}";
        }

        static string PanelClickHits(Vector2 capturedSpot)
        {
            Type eventSystemType = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            object current = eventSystemType?.GetProperty("current")?.GetValue(null);
            if (current == null) return "unavailable: no current EventSystem";
            try
            {
                // ScreenUiProbe.RaycastHits asserts fixture readiness; this diagnostic must also observe its absence.
                Type pointerType = Type.GetType("UnityEngine.EventSystems.PointerEventData, UnityEngine.UI");
                Type resultType = Type.GetType("UnityEngine.EventSystems.RaycastResult, UnityEngine.UI");
                if (pointerType == null || resultType == null) return "unavailable: uGUI raycast types";
                object pointer = Activator.CreateInstance(pointerType, current);
                pointerType.GetProperty("position").SetValue(pointer, capturedSpot);
                var results = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType));
                eventSystemType.GetMethod("RaycastAll").Invoke(current, new[] { pointer, results });
                var gameObject = resultType.GetProperty("gameObject");
                GameObject[] hits = results.Cast<object>().Select(result => (GameObject)gameObject.GetValue(result))
                    .Where(hit => hit != null).ToArray();
                return $"EventSystem {((Component)current).GetEntityId()}, count {hits.Length}, "
                    + string.Join(" | ", hits.Select(hit => $"{hit.scene.name}:{ScreenUiProbe.PathOf(hit.transform, hit.transform.root, true)}#{hit.GetEntityId()}"));
            }
            catch (Exception error)
            {
                return $"unavailable: raycast {error.GetType().Name}: {error.Message}";
            }
        }

        void LogPauseInput(string stage)
        {
            PauseMenuController pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            InputActionReference reference = pause == null ? null : MapEntryPlayFixture.Field<InputActionReference>(pause, "_togglePauseAction");
            InputAction action = reference == null ? null : reference.action;
            PlayerInput player = _fixture.Player == null ? null : _fixture.Player.GetComponent<PlayerInput>();
            string controls = action == null ? "none" : string.Join(", ", action.controls.Select(control => control.path));
            Debug.Log($"[HUD pause input] {stage}: controller {pause != null}, active {pause?.isActiveAndEnabled}, "
                + $"action {action?.actionMap?.name}/{action?.name}, enabled {action?.enabled}, phase {action?.phase}, controls [{controls}], "
                + $"fixture keyboard {_fixture.Keyboard?.name} Escape {_fixture.Keyboard?.escapeKey.isPressed}, "
                + $"current keyboard {Keyboard.current?.name}, same PlayerInput asset {action != null && player != null && ReferenceEquals(action.actionMap?.asset, player.actions)}, "
                + $"player map {player?.currentActionMap?.name}, scheme {player?.currentControlScheme}, update {InputSystem.settings.updateMode}, "
                + $"NPC dialogue {Dawnholder.Client.Gameplay.NpcDialogPanel.IsShown}, timeScale {Time.timeScale}");
        }

        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return WaitQueries(1);
            _fixture.Peer.Send(Snapshot(1, 10, (ItemId.Material, 2), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
        }

        // One press and release of I through the fixture keyboard, each followed by two frames.
        IEnumerator PressI()
        {
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        IEnumerator WaitQueries(int count) => MapEntryPlayFixture.Wait(
            () => _fixture.Peer.Count(PacketID.C_InventoryRequest) == count, $"C_InventoryRequest #{count}", 3);

        // The same wire snapshot InventorySceneLifecycleTests builds; its builder is private to that class.
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

        // What a player reads as an amount: the digits of the text, whatever its format.
        static string Digits(string text)
        {
            var digits = new StringBuilder();
            foreach (char character in text ?? string.Empty)
            {
                if (char.IsDigit(character)) digits.Append(character);
            }
            return digits.ToString();
        }

        // Characters of the label's text found neither in its font nor in that font's or TMP's fallbacks.
        static string[] MissingGlyphs(TMP_Text label)
        {
            var fonts = new List<TMP_FontAsset> { label.font };
            if (label.font.fallbackFontAssetTable != null) fonts.AddRange(label.font.fallbackFontAssetTable);
            if (TMP_Settings.fallbackFontAssets != null) fonts.AddRange(TMP_Settings.fallbackFontAssets);
            return label.text.Distinct()
                .Where(character => !fonts.Any(font => font != null && font.characterLookupTable.ContainsKey(character)))
                .Select(character => $"U+{(int)character:X4}")
                .ToArray();
        }
    }
}
