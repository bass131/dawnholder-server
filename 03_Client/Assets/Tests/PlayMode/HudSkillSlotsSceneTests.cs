using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
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
using UnityEngine.UI;

namespace Dawnholder.Client.Tests.PlayMode
{
    // PR2 H1/H3-H6 use the real Town, additive HUD and existing finite TCP/input fixture.
    // Layout expectations are the lead's literal table; no product packing formula is reproduced here.
    public sealed class HudSkillSlotsSceneTests
    {
        const float Tolerance = .01f;
        const string IconSetPath = "Assets/Prefabs/Characters/ClassVisuals/SkillIconSet.asset";
        static readonly Vector2[] Centers = { new Vector2(60, 60), new Vector2(168, 60), new Vector2(276, 60), new Vector2(384, 60) };
        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            yield return _fixture.Cleanup();
            Type placement = typeof(InventoryPanel).Assembly.GetType("Dawnholder.Client.UI.UiWindowPlacement", false);
            Assert.IsNotNull(placement, "fixture: existing window placement type");
            MethodInfo forget = placement.GetMethod("ForgetMovedPositions", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            Assert.IsNotNull(forget, "fixture: ForgetMovedPositions");
            forget.Invoke(null, null);
        }

        [UnityTest]
        public IEnumerator H1_TownHasNoHudGold_BeforeAndAfterSnapshot_WhilePanelShows1250()
        {
            yield return EnterReadyTown(CharacterClass.Knight);
            string[] before = GoldLabels();
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 1, currency = 1250 }.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "H1 snapshot applied", 3);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return _fixture.Frames(2);
            string[] after = GoldLabels();
            {
                Assert.AreEqual("재화 1,250", InventoryPanelProbe.Currency, "H1 positive control: panel shows the server's currency");
                Assert.IsEmpty(before, "H1 no visible HUD gold label before a snapshot");
                Assert.IsEmpty(after, "H1 no visible HUD gold label after a snapshot");
                Assert.IsNull(HudController.Instance.transform.Find("Gold_bar"), "H1 no HUD gold object");
            }
        }

        [UnityTest] public IEnumerator H3_Knight_FourSlotsAtLowerLeft_1920x1080() => LayoutAt(CharacterClass.Knight, 1920, 1080);
        [UnityTest] public IEnumerator H3_Knight_FourSlotsAtLowerLeft_1280x720() => LayoutAt(CharacterClass.Knight, 1280, 720);
        [UnityTest] public IEnumerator H3_Mage_FourSlotsAtLowerLeft_1920x1080() => LayoutAt(CharacterClass.Mage, 1920, 1080);
        [UnityTest] public IEnumerator H3_Mage_FourSlotsAtLowerLeft_1280x720() => LayoutAt(CharacterClass.Mage, 1280, 720);

        IEnumerator LayoutAt(CharacterClass characterClass, int width, int height)
        {
            yield return EnterReadyTown(characterClass);
            RectTransform panel = SkillPanel();
            using (GuildSizeSurface surface = GuildSizeSurface.Create(width, height))
            {
                surface.Attach(panel.GetComponentInParent<Canvas>().rootCanvas);
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(surface.FixtureProblems(), "H3 fixture must actually render at " + surface.Size);
                {
                    AssertVector(Vector2.zero, panel.anchorMin, "H3 anchorMin");
                    AssertVector(Vector2.zero, panel.anchorMax, "H3 anchorMax");
                    AssertVector(Vector2.zero, panel.pivot, "H3 pivot");
                    AssertVector(new Vector2(24, 24), panel.anchoredPosition, "H3 lower-left margin");
                    AssertVector(new Vector2(444, 120), panel.sizeDelta, "H3 four-slot panel size");
                    for (int index = 0; index < Centers.Length; index++)
                    {
                        RectTransform slot = Slot(panel, index + 1);
                        Assert.IsNotNull(slot, "H3 slot " + (index + 1) + " exists");
                        if (slot == null) continue;
                        Assert.IsTrue(slot.gameObject.activeInHierarchy, "H3 slot " + (index + 1) + " stays active");
                        AssertVector(new Vector2(96, 96), slot.rect.size, "H3 slot size " + (index + 1));
                        Vector2 center = panel.InverseTransformPoint(slot.TransformPoint(slot.rect.center));
                        AssertVector(Centers[index], center - panel.rect.min, "H3 literal center " + (index + 1));
                        Rect pixels = surface.PixelRect(slot);
                        Rect whole = surface.Whole;
                        Assert.IsTrue(pixels.xMin >= whole.xMin - Tolerance && pixels.yMin >= whole.yMin - Tolerance &&
                            pixels.xMax <= whole.xMax + Tolerance && pixels.yMax <= whole.yMax + Tolerance,
                            $"H3 slot {index + 1}: {pixels} inside {whole} for {characterClass}");
                    }
                    RectTransform fifth = Slot(panel, 5);
                    Assert.IsTrue(fifth == null || !fifth.gameObject.activeSelf, "H3 fifth slot is absent or disabled");
                }
            }
        }

        [UnityTest] public IEnumerator H4_Knight_DashAndThreeDimEmptySlots() => ContentsFor(CharacterClass.Knight);
        [UnityTest] public IEnumerator H4_Mage_ThunderboltTeleportAndTwoDimEmptySlots() => ContentsFor(CharacterClass.Mage);

        IEnumerator ContentsFor(CharacterClass characterClass)
        {
            yield return EnterReadyTown(characterClass);
#if UNITY_EDITOR
            SkillIconSet icons = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillIconSet>(IconSetPath);
#else
            SkillIconSet icons = null;
#endif
            Assert.IsNotNull(icons, "fixture: actual SkillIconSet asset");
            SkillId[] expected = characterClass == CharacterClass.Knight ?
                new[] { SkillId.Dash, SkillId.None, SkillId.None, SkillId.None } :
                new[] { SkillId.Thunderbolt, SkillId.Teleport, SkillId.None, SkillId.None };
            {
                for (int index = 0; index < expected.Length; index++)
                {
                    RectTransform slot = Slot(SkillPanel(), index + 1);
                    Assert.IsNotNull(slot, "H4 slot " + (index + 1));
                    if (slot == null) continue;
                    Assert.IsTrue(slot.gameObject.activeInHierarchy, "H4 every filled or empty slot is active: " + slot.name);
                    Image artwork = slot.GetComponent<Image>();
                    Assert.IsNotNull(artwork, "H4 slot artwork: " + slot.name);
                    if (artwork == null) continue;
                    bool empty = expected[index] == SkillId.None;
                    Assert.AreEqual(empty ? .3f : 1f, artwork.color.a * artwork.canvasRenderer.GetInheritedAlpha(), Tolerance,
                        "H4 effective artwork alpha: " + slot.name);
                    Image icon = slot.Find("icon")?.GetComponent<Image>();
                    Image cooldown = slot.Find("cooldown_overlay")?.GetComponent<Image>();
                    TMP_Text[] labels = slot.GetComponentsInChildren<TMP_Text>(true);
                    if (empty)
                    {
                        Assert.IsFalse(Drawn(icon), "H4 empty slot has no drawn icon: " + slot.name);
                        Assert.IsEmpty(labels.Where(text => Drawn(text) && !string.IsNullOrEmpty(text.text)), "H4 empty slot has no key or remaining seconds");
                        Assert.IsFalse(Drawn(cooldown) && cooldown.fillAmount > 0, "H4 empty slot has no cooldown fill");
                    }
                    else
                    {
                        Assert.IsTrue(Drawn(icon), "H4 filled icon is drawn");
                        Assert.IsNotNull(icons.GetIcon(expected[index]), "fixture: icon asset contains the fixed expected skill");
                        Assert.AreSame(icons.GetIcon(expected[index]), icon == null ? null : icon.sprite, "H4 fixed class-to-skill icon");
                        Assert.IsTrue(labels.Any(text => Drawn(text) && text.text == (index == 0 ? "Q" : "E")), "H4 assigned key is drawn");
                        Assert.IsNotNull(cooldown, "H4 filled slot retains its cooldown overlay");
                        if (cooldown != null) Assert.AreEqual(0f, cooldown.fillAmount, Tolerance, "H4 ready skill has no remaining cooldown fill");
                    }
                    foreach (Graphic graphic in slot.GetComponentsInChildren<Graphic>(true))
                        Assert.IsFalse(graphic.raycastTarget, "H4 HUD decoration never receives a raycast: " + graphic.name);
                }
            }
        }

        [UnityTest]
        public IEnumerator H5_Knight_EmptyEAddsNoSkillPacket_ThenQPositivelySendsDash()
        {
            yield return EnterReadyTown(CharacterClass.Knight);
            int before = Skills;
            yield return GuildGameplayWindowTestTools.KeyFrame(_fixture, Key.E);
            Assert.AreEqual(before, Skills, "H5 empty E sends no C_SkillUse");
            yield return SendSkill(Key.Q, SkillId.Dash, before + 1);
        }

        [UnityTest]
        public IEnumerator H5_Mage_QSendsThunderbolt_AndESendsTeleport()
        {
            yield return EnterReadyTown(CharacterClass.Mage);
            int before = Skills;
            yield return SendSkill(Key.Q, SkillId.Thunderbolt, before + 1);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Player.CanUseTeleport && !_fixture.Player.IsActionLocked,
                "H5 mage E waits for the Q action lock to clear", 8);
            yield return SendSkill(Key.E, SkillId.Teleport, before + 2);
        }

        IEnumerator SendSkill(Key key, SkillId expected, int count)
        {
            yield return GuildGameplayWindowTestTools.KeyFrame(_fixture, key);
            yield return MapEntryPlayFixture.Wait(() => Skills == count, "H5 positive " + key + " packet", 5);
            Assert.AreEqual(count, Skills, "H5 exactly one C_SkillUse per assigned key");
            Assert.AreEqual((int)expected, _fixture.Peer.Last<C_SkillUse>(PacketID.C_SkillUse).skillId, "H5 fixed skill id");
        }

        [UnityTest]
        public IEnumerator H6_InventoryDraggedUnderTheSkillSlots_SlotsStayDrawnInPlace_AndUncoveredFilledSlotsStillWork()
        {
            yield return EnterReadyTown(CharacterClass.Knight);
            _fixture.Peer.Send(OverlapSnapshot());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "H6 valid compact snapshot", 3);
            Assert.AreEqual(1u, InventoryState.Instance.Revision, "H6 snapshot applied");
            RectTransform skills = SkillPanel();
            Vector2 anchoredBefore = skills.anchoredPosition;
            Rect screenBefore = ScreenUiProbe.ScreenRect(skills);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            yield return ScreenUiProbe.WaitForEventSystem();
            var panel = (RectTransform)InventoryPanelProbe.Root.Find("Inventory");
            var grab = (RectTransform)panel.Find("Header/GrabArea");
            Assert.IsNotNull(grab, "fixture: title bar grab area");
            Vector2 from = InventoryPanelProbe.ScreenPoint(grab);
            yield return Drag(from, new Vector2(1, 1) - from);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            RectTransform filled = Slot(skills, 1);
            Assert.IsNotNull(filled, "H6 filled skill slot exists");
            Rect panelRect = ScreenUiProbe.ScreenRect(panel);
            Rect filledRect = ScreenUiProbe.ScreenRect(filled);
            Assert.Greater(OverlapArea(panelRect, filledRect), 0f,
                $"H6 inventory intersects skill_slot_1; inventory={panelRect}, skill={filledRect}, Screen={Screen.width}x{Screen.height}");

            Rect skillsRect = ScreenUiProbe.ScreenRect(skills);
            AssertVector(anchoredBefore, skills.anchoredPosition, "H6 skill panel anchored position stays unchanged");
            AssertVector(screenBefore.min, skillsRect.min, "H6 skill panel screen origin stays unchanged");
            AssertVector(screenBefore.size, skillsRect.size, "H6 skill panel screen size stays unchanged");
            Canvas skillCanvas = skills.GetComponentInParent<Canvas>().rootCanvas;
            Canvas inventoryCanvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, skillCanvas.renderMode, "H6 skills use an Overlay root canvas");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, inventoryCanvas.renderMode, "H6 inventory uses an Overlay root canvas");
            Assert.Greater(inventoryCanvas.sortingOrder, skillCanvas.sortingOrder, "H6 user 125 B: inventory is drawn above the skill slots");
            Assert.IsTrue(Drawn(filled.GetComponent<Image>()), "H6 filled skill artwork is actually drawn");
            RectTransform empty = Slot(skills, 3);
            Assert.IsNotNull(empty, "H6 empty skill slot exists");
            Assert.IsTrue(Drawn(empty.GetComponent<Image>()), "H6 empty skill artwork is actually drawn");

            Vector2 materialPoint = InventoryPanelProbe.SlotPoint(0);
            Vector2 pouchPoint = InventoryPanelProbe.SlotPoint(1);
            Assert.IsFalse(skillsRect.Contains(materialPoint), $"H6 material point {materialPoint} is outside skills {skillsRect}");
            Assert.IsFalse(skillsRect.Contains(pouchPoint), $"H6 pouch point {pouchPoint} is outside skills {skillsRect}");
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, materialPoint);
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "H6 uncovered material slot shows detail while inventory overlaps skills");
            yield return InventoryPanelProbe.PointAt(_fixture.Mouse, pouchPoint);
            Assert.IsTrue(InventoryPanelProbe.DetailShown, "H6 uncovered pouch slot shows detail while inventory overlaps skills");
            int before = Uses;
            yield return InventoryPanelProbe.RightClickAt(_fixture.Mouse, pouchPoint);
            yield return MapEntryPlayFixture.Wait(() => Uses == before + 1, "H6 uncovered pouch positive use", 3);
            Assert.AreEqual(before + 1, Uses, "H6 exactly one use while inventory overlaps skills");
            C_ItemUse sent = _fixture.Peer.Last<C_ItemUse>(PacketID.C_ItemUse);
            Assert.AreEqual((int)ItemId.CoinPouch, sent.itemId, "H6 uses the uncovered pouch");
            Assert.AreEqual(1u, sent.expectedRevision, "H6 use carries the observed snapshot revision");
        }

        static ArraySegment<byte> OverlapSnapshot()
        {
            var packet = new S_InventorySnapshot { revision = 1, currency = 10 };
            typeof(S_InventorySnapshot).GetField("slot0ItemId").SetValue(packet, (int)ItemId.Material);
            typeof(S_InventorySnapshot).GetField("slot0Count").SetValue(packet, 2);
            typeof(S_InventorySnapshot).GetField("slot1ItemId").SetValue(packet, (int)ItemId.CoinPouch);
            typeof(S_InventorySnapshot).GetField("slot1Count").SetValue(packet, 1);
            return packet.Write();
        }

        static float OverlapArea(Rect a, Rect b)
        {
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0f && height > 0f ? width * height : 0f;
        }

        IEnumerator EnterReadyTown(CharacterClass characterClass)
        {
            yield return _fixture.EnterScriptedTown();
            if (characterClass == CharacterClass.Mage)
            {
                NetworkService.Instance.Disconnect();
                yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance == null && ClassLoadout.ServerClass == null &&
                    LocalPlayerMovement.Instance == null, "connection owner cleanup", 5);
                EntryWirePeer previous = _fixture.Peer;
                previous.Dispose();
                yield return MapEntryPlayFixture.Wait(() => previous.Completion.IsCompleted, "previous scripted peer shutdown", 3);
                Assert.IsEmpty(previous.Errors, "fixture: previous scripted transport failures");
                _fixture.Peer = EntryWirePeer.Listen();
                MapEntryPlayFixture.Set(NetworkService.Instance, "_serverPort", _fixture.Peer.Port);
                NetworkService.Instance.Connect("127.0.0.1", (int)characterClass);
                yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance != null && _fixture.Peer.Selected &&
                    ClassLoadout.ServerClass == characterClass, "new connection loads " + characterClass, 10);
                _fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
                yield return MapEntryPlayFixture.Wait(() => _fixture.Session != null && _fixture.Session.Entry.MapId == 0 &&
                    _fixture.Session.Entry.State == MapEntryState.Entering && _fixture.Session.Entry.SceneReady && _fixture.Player != null,
                    "new connection's Town binding before HP", 10);
                _fixture.PairInput();
            }
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null && HudController.Instance.isActiveAndEnabled &&
                SceneManager.GetSceneByName("UI").isLoaded, "additive HUD", 10);
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "initial inventory query", 3);
            yield return _fixture.Frames(3);
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

        int Skills => _fixture.Peer.Count(PacketID.C_SkillUse);
        int Uses => _fixture.Peer.Count(PacketID.C_ItemUse);
        static RectTransform SkillPanel() => (RectTransform)HudController.Instance.transform.Find("skill_panel");
        static RectTransform Slot(RectTransform panel, int number) => (RectTransform)panel.Find("skill_slot_" + number);
        static bool Drawn(Graphic graphic) => graphic != null && graphic.isActiveAndEnabled && graphic.canvas != null &&
            graphic.color.a * graphic.canvasRenderer.GetInheritedAlpha() > 0f && !graphic.canvasRenderer.cull;
        static string[] GoldLabels() => HudController.Instance.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => Drawn(text) && ((text.text ?? string.Empty).TrimStart().StartsWith("Gold", StringComparison.Ordinal) ||
                (text.text ?? string.Empty).Contains("골드"))).Select(text => text.text).ToArray();
        static void AssertVector(Vector2 expected, Vector2 actual, string message)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, message + " x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, message + " y");
        }
    }
}
