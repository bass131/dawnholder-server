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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // G1/G4 observe the actual additive HUD and inventory with the existing finite loopback fixture.
    // GuildSizeSurface preserves CanvasScaler settings but uses a 1920x1080 camera target, not the game window.
    // Text ink is the per-pixel difference between the same frame with the label shown and transparent.
    public sealed class HudPanelSizingRenderTests
    {
        const int Width = 1920;
        const int Height = 1080;
        const int DrawnThreshold = 8;
        const float Tolerance = 0.05f;
        MapEntryPlayFixture _fixture;

        [SetUp]
        public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            yield return _fixture.Cleanup();
            Type placement = typeof(InventoryPanel).Assembly.GetType("Dawnholder.Client.UI.UiWindowPlacement", false);
            Assert.IsNotNull(placement, "fixture: placement type for session position cleanup");
            MethodInfo forget = placement.GetMethod("ForgetMovedPositions",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Assert.IsNotNull(forget, "fixture: placement cleanup entry point");
            forget.Invoke(null, null);
        }

        [UnityTest]
        public IEnumerator G1_Knight_PartyInkStaysInsideBothRowsWithFourPixelVerticalMargins() =>
            PartyInkAt(CharacterClass.Knight);

        [UnityTest]
        public IEnumerator G1_Mage_PartyInkStaysInsideBothRowsWithFourPixelVerticalMargins() =>
            PartyInkAt(CharacterClass.Mage);

        IEnumerator PartyInkAt(CharacterClass characterClass)
        {
            yield return EnterReadyTown(characterClass);
            yield return ShowParty();
            RectTransform party = PartyPanel();
            TMP_Text[] labels = MemberLabels(party);
            CollectionAssert.AreEquivalent(new[] { "기사 (2)", "마법사 (1)" }, labels.Select(label => label.text),
                "fixture: actual PartyMemberHud renders the supplied class/entity roster");
            using (GuildSizeSurface surface = GuildSizeSurface.Create(Width, Height))
            {
                surface.Attach(party.GetComponentInParent<Canvas>().rootCanvas);
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(surface.FixtureProblems(), "fixture: actual 1920x1080 render and unchanged scaler");
                TestContext.Out.WriteLine(surface.Describe());
                string stem = "hud-sizing-" + characterClass.ToString().ToLowerInvariant();
                Color32[] shown = Capture(surface, stem + ".png");
                var errors = new List<string>();
                Size(errors, new Vector2(520, 184), surface.PixelRect(party).size, "Party_Info rendered size");
                for (int index = 0; index < labels.Length; index++)
                {
                    TMP_Text label = labels[index];
                    Rect row = surface.PixelRect((RectTransform)label.transform.parent);
                    Size(errors, new Vector2(480, 52), row.size, "row " + index + " rendered size");
                    if (label.isTextOverflowing) errors.Add("row " + index + " reports text overflow");
                    float originalAlpha = label.alpha;
                    Color32[] hidden;
                    try
                    {
                        label.alpha = 0;
                        hidden = Capture(surface, stem + "-row" + index + "-hidden.png");
                    }
                    finally
                    {
                        label.alpha = originalAlpha;
                    }
                    Color32[] restored = Capture(surface, stem + "-row" + index + "-restored.png");
                    Rect ink = DifferenceBounds(shown, hidden, out int drawnPixels);
                    DifferenceBounds(shown, restored, out int unstablePixels);
                    TestContext.Out.WriteLine($"INK class={characterClass} row={index} text={label.text} " +
                        $"rowRect={row.ToString("F3")} inkRect={ink.ToString("F3")} pixels={drawnPixels} " +
                        $"topPx={row.yMax - ink.yMax:F3} bottomPx={ink.yMin - row.yMin:F3} " +
                        $"leftPx={ink.xMin - row.xMin:F3} rightPx={row.xMax - ink.xMax:F3} " +
                        $"restoredDifferencePixels={unstablePixels} threshold={DrawnThreshold}");
                    Assert.AreEqual(0, unstablePixels, "fixture: repeat render must be stable after restoring the label");
                    Assert.Greater(drawnPixels, 0, "fixture: hiding this label removes real drawn ink");
                    if (ink.xMin < row.xMin - Tolerance || ink.xMax > row.xMax + Tolerance ||
                        ink.yMin < row.yMin - Tolerance || ink.yMax > row.yMax + Tolerance)
                        errors.Add("row " + index + " drawn ink is outside its row");
                    if (row.yMax - ink.yMax < 4 - Tolerance)
                        errors.Add($"row {index} top margin {row.yMax - ink.yMax:F3}px is below 4px");
                    if (ink.yMin - row.yMin < 4 - Tolerance)
                        errors.Add($"row {index} bottom margin {ink.yMin - row.yMin:F3}px is below 4px");
                }
                Assert.IsEmpty(errors, string.Join("\n", errors));
            }
        }

        [UnityTest]
        public IEnumerator G4_InventoryOpensTwelvePixelsBelowThePartyPanel()
        {
            yield return EnterReadyTown(CharacterClass.Knight);
            yield return ShowParty();
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 1, currency = 1250 }.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료",
                "fixture: inventory snapshot applied", 3);
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            yield return HudOverlapProbe.WaitForHudAndSettledPanel();
            RectTransform party = PartyPanel();
            var inventory = InventoryPanelProbe.Root.Find("Inventory") as RectTransform;
            Assert.IsNotNull(inventory, "fixture: actual runtime inventory panel");
            using (GuildSizeSurface surface = GuildSizeSurface.Create(Width, Height))
            {
                surface.Attach(party.GetComponentInParent<Canvas>().rootCanvas);
                surface.Attach(inventory.GetComponentInParent<Canvas>().rootCanvas);
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(surface.FixtureProblems(), "fixture: HUD and inventory both see 1920x1080");
                Capture(surface, "hud-sizing-inventory.png");
                Rect partyPixels = surface.PixelRect(party);
                Rect inventoryPixels = surface.PixelRect(inventory);
                float gap = partyPixels.yMin - inventoryPixels.yMax;
                TestContext.Out.WriteLine($"INVENTORY party={partyPixels.ToString("F3")} " +
                    $"inventory={inventoryPixels.ToString("F3")} gapPx={gap:F3}");
                Assert.AreEqual(12, gap, Tolerance, "G4: actual inventory top is 12px below Party_Info bottom");
            }
        }

        IEnumerator ShowParty()
        {
            _fixture.Peer.Send(new S_PartyUpdate
            {
                partyId = 31,
                leaderEntityId = 2,
                member0EntityId = 2,
                member1EntityId = 1,
                member0Class = 0,
                member1Class = 1
            }.Write());
            yield return MapEntryPlayFixture.Wait(() => PartyState.Instance != null && PartyState.Instance.InParty &&
                PartyPanel().GetComponent<CanvasGroup>().alpha == 1, "fixture: party roster is visible", 3);
            yield return _fixture.Frames(2);
        }

        [UnityTest]
        public IEnumerator Portrait_Knight_PortraitCellContainsNoPanelParchment() => PortraitCellAt(CharacterClass.Knight);

        [UnityTest]
        public IEnumerator Portrait_Mage_PortraitCellContainsNoPanelParchment() => PortraitCellAt(CharacterClass.Mage);

        IEnumerator PortraitCellAt(CharacterClass characterClass)
        {
            yield return EnterReadyTown(characterClass);
            var panel = HudController.Instance.transform.Find("character_status") as RectTransform;
            string portraitName = characterClass == CharacterClass.Knight ? "status_icon1" : "status_icon2";
            var portrait = panel.Find(portraitName) as RectTransform;
            Assert.IsNotNull(portrait, "fixture: actual scene portrait " + portraitName);
            Assert.IsTrue(portrait.gameObject.activeInHierarchy, "fixture: the selected class portrait is active");
            using (GuildSizeSurface surface = GuildSizeSurface.Create(Width, Height))
            {
                surface.Attach(panel.GetComponentInParent<Canvas>().rootCanvas);
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(surface.FixtureProblems(), "fixture: actual 1920x1080 portrait render");
                Color32[] pixels = Capture(surface, "hud-cell-" + characterClass.ToString().ToLowerInvariant() + ".png");
                Rect panelPixels = surface.PixelRect(panel);
                Rect portraitPixels = surface.PixelRect(portrait);
                // Empty strip between the portrait and the status text, in this same rendered panel.
                int sampleX = Mathf.FloorToInt(panelPixels.xMin + 145);
                int sampleY = Mathf.FloorToInt(panelPixels.yMin + 78);
                Color32 parchment = pixels[sampleY * Width + sampleX];
                Vector2 centre = portraitPixels.center;
                Rect interior = new Rect(centre.x - 49 + 2, centre.y - 49 + 2, 94, 94);
                int compared = 0;
                int matches = 0;
                for (int y = Mathf.CeilToInt(interior.yMin); y < Mathf.FloorToInt(interior.yMax); y++)
                for (int x = Mathf.CeilToInt(interior.xMin); x < Mathf.FloorToInt(interior.xMax); x++)
                {
                    Color32 pixel = pixels[y * Width + x];
                    compared++;
                    if (Math.Abs(pixel.r - parchment.r) <= 8 && Math.Abs(pixel.g - parchment.g) <= 8 &&
                        Math.Abs(pixel.b - parchment.b) <= 8) matches++;
                }
                TestContext.Out.WriteLine($"PARCHMENT class={characterClass} portrait={portraitPixels.ToString("F3")} " +
                    $"interior={interior.ToString("F3")} sample=({sampleX},{sampleY}) " +
                    $"sampleRGBA=({parchment.r},{parchment.g},{parchment.b},{parchment.a}) " +
                    $"compared={compared} matchingPanelPixels={matches} channelTolerance=8");
                Assert.Greater(compared, 0, "fixture: nonempty inner portrait area is sampled");
                Assert.AreEqual(0, matches, "Portrait: the inset portrait interior contains zero panel-parchment pixels");
            }
        }

        // The same connection lifecycle used by HudSkillSlotsSceneTests. Each class is selected on its own connection.
        IEnumerator EnterReadyTown(CharacterClass characterClass)
        {
            yield return _fixture.EnterScriptedTown();
            if (characterClass == CharacterClass.Mage)
            {
                NetworkService.Instance.Disconnect();
                yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance == null &&
                    ClassLoadout.ServerClass == null && LocalPlayerMovement.Instance == null,
                    "fixture: previous connection cleanup", 5);
                EntryWirePeer previous = _fixture.Peer;
                previous.Dispose();
                yield return MapEntryPlayFixture.Wait(() => previous.Completion.IsCompleted,
                    "fixture: previous peer shutdown", 3);
                Assert.IsEmpty(previous.Errors, "fixture: previous transport errors");
                _fixture.Peer = EntryWirePeer.Listen();
                MapEntryPlayFixture.Set(NetworkService.Instance, "_serverPort", _fixture.Peer.Port);
                NetworkService.Instance.Connect("127.0.0.1", (int)characterClass);
                yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance != null &&
                    _fixture.Peer.Selected && ClassLoadout.ServerClass == characterClass,
                    "fixture: new connection selects Mage", 10);
                _fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = 2, spawnY = 0 }.Write());
                yield return MapEntryPlayFixture.Wait(() => _fixture.Session != null &&
                    _fixture.Session.Entry.MapId == 0 && _fixture.Session.Entry.State == MapEntryState.Entering &&
                    _fixture.Session.Entry.SceneReady && _fixture.Player != null, "fixture: Mage Town bound", 10);
                _fixture.PairInput();
            }
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null &&
                HudController.Instance.isActiveAndEnabled && SceneManager.GetSceneByName("UI").isLoaded,
                "fixture: additive HUD", 10);
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1,
                "fixture: initial inventory query", 3);
            yield return _fixture.Frames(3);
        }

        static RectTransform PartyPanel() => (RectTransform)HudController.Instance.transform.Find("Party_Info");

        static TMP_Text[] MemberLabels(RectTransform party)
        {
            TMP_Text[] labels = party.GetComponentsInChildren<TMP_Text>(true)
                .Where(label => label.name == "MemberInfo")
                .OrderByDescending(label => ((RectTransform)label.transform.parent).anchoredPosition.y).ToArray();
            Assert.AreEqual(2, labels.Length, "fixture: two actual member labels");
            Assert.IsTrue(labels.All(label => label.isActiveAndEnabled), "fixture: both labels are enabled");
            return labels;
        }

        static Color32[] Capture(GuildSizeSurface surface, string name)
        {
            Assert.IsNotEmpty(Environment.GetEnvironmentVariable(GuildSizeSurface.CaptureDirectoryVariable),
                "fixture: capture output directory supplied by harness");
            TestContext.Out.WriteLine(surface.Capture(name, out float drawnShare));
            Assert.Greater(drawnShare, 0, "fixture: the render contains visible pixels");
            var readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = surface.Texture;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                readback.Apply(false);
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(readback);
            }
        }

        // Bounds enclose whole pixel cells: using max+1 avoids overstating the right/top clear margin.
        static Rect DifferenceBounds(Color32[] shown, Color32[] hidden, out int count)
        {
            Assert.AreEqual(Width * Height, shown.Length, "fixture: shown pixel count");
            Assert.AreEqual(shown.Length, hidden.Length, "fixture: hidden pixel count");
            int minX = Width, minY = Height, maxX = -1, maxY = -1;
            count = 0;
            for (int index = 0; index < shown.Length; index++)
            {
                Color32 a = shown[index];
                Color32 b = hidden[index];
                if (Math.Abs(a.r - b.r) <= DrawnThreshold && Math.Abs(a.g - b.g) <= DrawnThreshold &&
                    Math.Abs(a.b - b.b) <= DrawnThreshold) continue;
                int x = index % Width;
                int y = index / Width;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                count++;
            }
            return count == 0 ? Rect.zero : Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        }

        static void Size(List<string> errors, Vector2 expected, Vector2 observed, string label)
        {
            TestContext.Out.WriteLine($"{label}: expected={expected} observed={observed}");
            if (Mathf.Abs(expected.x - observed.x) > Tolerance || Mathf.Abs(expected.y - observed.y) > Tolerance)
                errors.Add($"{label}: expected={expected} observed={observed}");
        }
    }
}
