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

        // While closed, a mouse click where the Use button lies reaches the game world and attacks; I opens the panel
        // and the same click uses the pouch without an attack. A map move keeps it open and the next I closes it.
        [UnityTest]
        public IEnumerator ClosedPanel_LetsAClickOnItsUseSpotAttack_AndAfterIThatClickUses_AcrossAMapMove()
        {
            yield return ReadySyncedTown();
            LocalPlayerMovement player = _fixture.Player;
            Vector2 useSpot = InventoryPanelProbe.ScreenPoint(InventoryPanelProbe.Use(1));
            bool shownAtEntry = InventoryPanelProbe.Group.alpha > 0f;
            bool raycastsAtEntry = InventoryPanelProbe.Group.blocksRaycasts;

            yield return InventoryPanelProbe.AttackReady(player);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, useSpot);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == attacks + 1,
                "a click on the closed panel's Use spot attacks", 3);
            yield return _fixture.Frames(4);
            int usesWhileClosed = _fixture.Peer.Count(PacketID.C_ItemUse);

            yield return PressI();
            bool shownAfterI = InventoryPanelProbe.Group.alpha > 0f;
            yield return InventoryPanelProbe.AttackReady(player);
            int attacksBeforeOpenClick = _fixture.Peer.Count(PacketID.C_Attack);
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, useSpot);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_ItemUse) == 1, "the open panel's Use", 3);
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
