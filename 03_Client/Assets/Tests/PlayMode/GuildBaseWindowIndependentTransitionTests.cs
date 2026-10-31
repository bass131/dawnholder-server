using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.PlayMode.GuildSizeGeometry;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-10-30-login-screen-guild-window: requirements 3/4, and the both-open text decision.
    // Compare observable before/after geometry through real LateUpdate frames, without reproducing slot formulas.
    // The existing scene tests cover one close; these cover repeated transitions, sibling lifetime and rebinding.
    public sealed class GuildBaseWindowIndependentTransitionTests
    {
        readonly GuildSizeStage _stage = new GuildSizeStage();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _stage.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedTransitions_1920x1080_RestoreEveryPartAndKeepSmithFixed()
        {
            yield return RoundTrips(1920, 1080);
        }

        [UnityTest]
        public IEnumerator RepeatedTransitions_1280x720_RestoreEveryPartAndKeepSmithFixed()
        {
            yield return RoundTrips(1280, 720);
        }

        [UnityTest]
        public IEnumerator SiblingDisabledDestroyedAndRebuilt_GuildReturnsAndFindsTheNewWindow()
        {
            yield return _stage.Open(1920, 1080, GuildSizeValues.Level(1), GuildSizeValues.Smith(), true, false);
            Dictionary<string, Rect> alone = Snapshot(_stage.Windows.GuildPanel);
            BlacksmithWindow smith = _stage.Windows.Smith;
            smith.SetOpen(true);
            yield return GuildSizeSurface.Settle();
            Dictionary<string, Rect> together = Snapshot(_stage.Windows.GuildPanel);
            Assert.IsFalse(SameRect(alone[""], together[""], 0.1f), "opening the sibling actually changed the layout");

            smith.enabled = false;
            yield return GuildSizeSurface.Settle();
            AssertSnapshot(alone, "disabled sibling");
            smith.enabled = true;
            yield return GuildSizeSurface.Settle();
            AssertSnapshot(together, "re-enabled sibling");
            Object.Destroy(smith.gameObject);
            yield return GuildSizeSurface.Settle();
            AssertSnapshot(alone, "destroyed sibling");

            BlacksmithWindow replacement = BlacksmithWindow.BuildRuntime(_stage.Windows.Holder.transform);
            replacement.Bind(_stage.Windows.Model);
            _stage.Surface.Attach(replacement.GetComponent<Canvas>());
            replacement.SetOpen(true);
            yield return GuildSizeSurface.Settle();
            AssertSnapshot(together, "new sibling");
            Record("sibling disabled/enabled/destroyed/rebuilt: every guild part restored to its observed layout");
        }

        [UnityTest]
        public IEnumerator LayoutChangesAndRebinding_PreserveValuesAndSingleGuardedIntent()
        {
            yield return _stage.Open(1280, 720, GuildSizeValues.Level(1), GuildSizeValues.Smith(), true, false);
            GuildWindow guild = _stage.Windows.Guild;
            Button action = _stage.Windows.GuildPanel.Find("LevelUp").GetComponent<Button>();
            TMP_Text name = _stage.Windows.GuildPanel.Find("GuildName").GetComponent<TMP_Text>();
            int intents = 0;
            guild.LevelUpRequested += () => intents++;
            Sprite levelOne = _stage.Windows.Picture.sprite;
            Assert.IsNotNull(levelOne, "confirmed level one has its picture");
            _stage.Windows.Smith.SetOpen(true);
            yield return GuildSizeSurface.Settle();
            action.onClick.Invoke();
            Assert.AreEqual(1, intents, "one intent after the layout change");
            guild.SetInputAllowed(false);
            action.onClick.Invoke();
            Assert.AreEqual(1, intents, "input gate also guards direct onClick");
            guild.SetInputAllowed(true);

            var replacement = new GuildDisplayModel();
            replacement.SetGuild(GuildSizeValues.Level(2));
            guild.Bind(replacement);
            guild.Bind(replacement);
            yield return GuildSizeSurface.Settle();
            Sprite levelTwo = _stage.Windows.Picture.sprite;
            Assert.IsNotNull(levelTwo, "confirmed level two has its picture");
            Assert.AreNotSame(levelOne, levelTwo, "level two changes the still picture");
            Assert.IsFalse(action.interactable, "maximum level is unavailable");
            action.onClick.Invoke();
            Assert.AreEqual(1, intents, "maximum level cannot emit an intent");
            _stage.Windows.Model.SetGuild(GuildSizeValues.LongLevelTwo());
            Assert.AreEqual("새벽 길드", name.text, "old source cannot change the rebound window");
            replacement.SetGuild(null);
            Assert.IsFalse(_stage.Windows.Picture.enabled, "unsynchronized data hides the picture");
            Assert.IsFalse(action.interactable, "unsynchronized data disables level-up");
            replacement.SetGuild(GuildSizeValues.Level(1));
            _stage.Windows.Smith.SetOpen(false);
            yield return GuildSizeSurface.Settle();
            Assert.AreSame(levelOne, _stage.Windows.Picture.sprite, "rebound level one restores its original art");
            action.onClick.Invoke();
            Assert.AreEqual(2, intents, "rebinding and layout changes do not duplicate the action listener");
            guild.SetOpen(false);
            action.onClick.Invoke();
            Assert.AreEqual(2, intents, "closed window cannot emit an intent");
            Record("intent counts 1 -> gated 1 -> max 1 -> rebound 2 -> closed 2; old model ignored");
        }

        IEnumerator RoundTrips(int width, int height)
        {
            yield return _stage.Open(width, height, GuildSizeValues.Level(1), GuildSizeValues.Smith(), true, false);
            Dictionary<string, Rect> alone = Snapshot(_stage.Windows.GuildPanel);
            Rect smith = _stage.Surface.PixelRect(_stage.Windows.SmithPanel);
            Assert.Greater(alone.Count, 10, "observe actual content parts as well as the panel");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                _stage.Windows.Smith.SetOpen(true);
                yield return GuildSizeSurface.Settle();
                Rect guild = _stage.Surface.PixelRect(_stage.Windows.GuildPanel);
                Assert.IsFalse(SameRect(alone[""], guild, 0.1f), "opening smith moves the guild");
                Assert.AreEqual(0f, OverlapArea(guild, smith), "the two panels do not overlap");
                Assert.IsTrue(SameRect(smith, _stage.Surface.PixelRect(_stage.Windows.SmithPanel), 0.1f),
                    "smith geometry stays fixed");
                Assert.IsTrue(Inside(guild, _stage.Surface.Whole, 0.1f), "guild remains on screen");
                Rect picture = DrawnRect(_stage.Surface, _stage.Windows.Picture);
                if (width == 1920) Assert.GreaterOrEqual(picture.width, 580f, "both-open picture minimum from goal");
                if (cycle == 0)
                {
                    Record(_stage.Surface.Capture($"independent-both-{width}x{height}.png", out float drawn));
                    Assert.Greater(drawn, 0.05f, "capture contains rendered windows");
                }
                _stage.Windows.Smith.SetOpen(false);
                yield return GuildSizeSurface.Settle();
                AssertSnapshot(alone, $"{width}x{height} round trip {cycle}");
                if (width == 1920)
                    Assert.GreaterOrEqual(Area(DrawnRect(_stage.Surface, _stage.Windows.Picture)), 474033.3f,
                        "the original 189613.3 square-pixel picture grows by at least 2.5 times");
            }
            Record($"{width}x{height}: three round trips, {alone.Count} content rectangles restored each time");
        }

        Dictionary<string, Rect> Snapshot(RectTransform panel) => panel.GetComponentsInChildren<RectTransform>(true)
            .ToDictionary(rect => PathUnder(panel, rect), rect => _stage.Surface.PixelRect(rect));

        void AssertSnapshot(Dictionary<string, Rect> expected, string reason)
        {
            Dictionary<string, Rect> actual = Snapshot(_stage.Windows.GuildPanel);
            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys, reason + ": parts are preserved");
            foreach (KeyValuePair<string, Rect> entry in expected)
                Assert.IsTrue(SameRect(entry.Value, actual[entry.Key], 0.1f), reason + ": " + entry.Key +
                    " before " + Format(entry.Value) + " after " + Format(actual[entry.Key]));
        }

        static void Record(string message) => TestContext.Out.WriteLine("[guild independent] " + message);
    }

    // Existing HUD coverage tests open the guild alone. The narrow arrangement must also clear both HUD areas.
    public sealed class GuildBaseWindowIndependentHudTests
    {
        MapEntryPlayFixture _fixture;
        GuildSizeSurface _surface;

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _surface?.Dispose();
            _surface = null;
            if (_fixture != null) yield return _fixture.Cleanup();
            _fixture = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Town_BothWindowsAtBothSizes_KeepStatusAndMinimapUncovered()
        {
            _fixture = new MapEntryPlayFixture();
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null &&
                SceneManager.GetSceneByName("UI").isLoaded, "additive HUD", 10);
            yield return GuildUiConnectionTestTools.Synchronize(_fixture);
            Assert.AreNotEqual(7777, _fixture.Peer.Port, "scripted loopback peer, not GameServer");
            GuildWindow[] guilds = Object.FindObjectsByType<GuildWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            BlacksmithWindow[] smiths = Object.FindObjectsByType<BlacksmithWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, guilds.Length, "one scene guild window");
            Assert.AreEqual(1, smiths.Length, "one scene blacksmith window");
            yield return GuildGameplayWindowTestTools.KeyFrame(_fixture, Key.G);
            Assert.IsTrue(guilds[0].IsOpen, "actual G input opened the guild window");
            smiths[0].SetOpen(true);
            Assert.IsTrue(smiths[0].IsOpen, "the blacksmith is open beside the guild");
            Transform hud = HudController.Instance.transform;
            foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                _surface = GuildSizeSurface.Create(size.x, size.y);
                _surface.Attach(hud.GetComponentInParent<Canvas>(true).rootCanvas);
                _surface.Attach(guilds[0].GetComponent<Canvas>());
                _surface.Attach(smiths[0].GetComponent<Canvas>());
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(_surface.FixtureProblems(), "each original scaler sees the capture size");
                foreach (string areaName in new[] { "character_status", "MinimapPlaceholder" })
                {
                    Transform area = hud.Find(areaName);
                    Assert.IsNotNull(area, "HUD area exists");
                    Assert.IsTrue(ScreenUiProbe.IsShown(area), "HUD area is shown");
                    Rect hudRect = _surface.PixelRect((RectTransform)area);
                    foreach (Transform window in new[] { guilds[0].transform, smiths[0].transform })
                    {
                        string panelName = window == guilds[0].transform ? "Guild" : "Blacksmith";
                        Assert.IsTrue(ScreenUiProbe.IsShown(window.Find(panelName)), "the measured panel is shown");
                        Rect panel = _surface.PixelRect((RectTransform)window.Find(panelName));
                        float overlap = OverlapArea(panel, hudRect);
                        TestContext.Out.WriteLine($"[guild independent hud] {size} {panelName} {Format(panel)} / " +
                            $"{areaName} {Format(hudRect)} overlap {overlap}");
                        Assert.AreEqual(0f, overlap, $"{size}: {panelName} leaves {areaName} uncovered");
                    }
                }
                _surface.Dispose();
                _surface = null;
                yield return null;
            }
        }
    }
}
