using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // The bottom HUD while a real bottom window stays open and the HUD is disabled and enabled, rebuilt with the additive
    // UI scene, or left to another window. Windows open by their normal entries: E inside a Town NPC trigger, and the
    // scripted peer's S_DungeonClearResult closed by the real Confirm pointer (loopback test authority, not GameServer).
    // Expected values are observations, never the layout's own arithmetic: where the bars were before a window opened,
    // where the same HUD put them with only that window open, overlap of shown screen rectangles, the fixture's literal
    // HP/class/gold, and which live objects the sources still call.
    public sealed class HudBottomWindowLayoutSceneTests
    {
        const string Tag = "[HUD bottom window]";
        const string ConfirmedHp = "HP 37 / 100";
        const string ConfirmedGold = "Gold: 1250";
        const float Tolerance = 0.01f;
        const string CaptureVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const int DockedWidth = 1280, DockedHeight = 720;

        MapEntryPlayFixture _fixture;
        string _captures;
        string _captureCase;
#if UNITY_EDITOR
        GameViewTargetSize _gameView;
#endif

        // NUnit reuses one instance for every test of the class, so each test starts without a Game view or capture case.
        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _captures = null;
            _captureCase = null;
#if UNITY_EDITOR
            _gameView = null;
#endif
        }

        // Only the GUI docked cases change the Game view; its selection and in-memory size scope go back before the
        // fixture unloads the scenes, as in UiWindowLiveCaptureTests.
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Exception restoreError = null;
#if UNITY_EDITOR
            try { if (_gameView != null) Debug.Log($"{Tag} game view restore: {_gameView.Restore()}"); }
            catch (Exception error) { restoreError = error; }
#endif
            yield return _fixture.Cleanup();
            if (restoreError != null) throw restoreError;
        }

        [UnityTest]
        public IEnumerator OpenNpcDialogue_HudDisabledAndEnabled_ReturnsToTheOpenPlace_AndAClosedWhileDisabledDialogueLeavesTheDefault()
        {
            yield return ReadySyncedTown();
            yield return WalkToTheNearestNpc();
            Placement idle = Observe("npc idle", null);
            AssertShownWithTheFixtureValues(idle);
            yield return PressEToOpenTheNpcDialogue();
            Placement open = Observe("npc open", NpcWindow());
            AssertShownWithTheFixtureValues(open);
            AssertReactsToCover(idle, open, coverRequired: true);

            GameObject hud = HudController.Instance.gameObject;
            for (int cycle = 0; cycle < 2; cycle++)
            {
                hud.SetActive(false);
                yield return _fixture.Frames(2);
                Assert.IsTrue(NpcShown(), "the dialogue stays open while the HUD is disabled");
                Observe($"npc open, HUD disabled {cycle}", NpcWindow());
                hud.SetActive(true);
                yield return _fixture.Frames(3);
                Placement again = Observe($"npc open, HUD enabled {cycle}", NpcWindow());
                AssertSamePlace(open, again, "after enabling with the dialogue still open");
                AssertShownWithTheFixtureValues(again);
                AssertNotMoreCovered(open, again);
            }

            hud.SetActive(false);
            yield return _fixture.Frames(2);
            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(() => !NpcDialogPanel.IsShown, "E closes the dialogue while the HUD is disabled", 3);
            hud.SetActive(true);
            yield return _fixture.Frames(3);
            AssertSamePlace(idle, Observe("npc closed while HUD disabled, HUD enabled", null), "after the dialogue closed meanwhile");

            yield return PressEToOpenTheNpcDialogue();
            AssertSamePlace(open, Observe("npc reopened", NpcWindow()), "when the same dialogue opens again");
            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(() => !NpcDialogPanel.IsShown, "E closes the dialogue", 3);
            yield return _fixture.Frames(3);
            AssertSamePlace(idle, Observe("npc closed", null), "after the ordinary close");
        }

        [UnityTest]
        public IEnumerator OpenNpcDialogue_UiRebuilt_NewHudTakesTheOpenPlaceWithOnlyLiveSources_AndADialogueClosedMeanwhileLeavesTheDefault()
        {
            yield return ReadySyncedTown();
            yield return WalkToTheNearestNpc();
            Placement idle = Observe("npc idle", null);
            AssertShownWithTheFixtureValues(idle);
            yield return PressEToOpenTheNpcDialogue();
            Placement open = Observe("npc open", NpcWindow());
            AssertShownWithTheFixtureValues(open);
            AssertReactsToCover(idle, open, coverRequired: true);
            int frames = PortraitFrames(HudController.Instance.transform);

            yield return UnloadUiAndAssertNothingOfTheOldHudRemains();
            Assert.IsTrue(NpcShown(), "the Town dialogue stays open without the UI scene");
            yield return LoadUi();
            Placement rebuilt = Observe("npc open, UI rebuilt", NpcWindow());
            AssertSamePlace(open, rebuilt, "on the rebuilt HUD with the dialogue still open");
            AssertShownWithTheFixtureValues(rebuilt);
            AssertNotMoreCovered(open, rebuilt);
            AssertOneLayoutWithTheSameFrames(frames);
            AssertSourcesCallOnlyTheLiveHud("npc open, UI rebuilt");

            yield return UnloadUiAndAssertNothingOfTheOldHudRemains();
            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(() => !NpcDialogPanel.IsShown, "E closes the dialogue without the UI scene", 3);
            yield return LoadUi();
            AssertSamePlace(idle, Observe("npc closed without UI, UI rebuilt", null), "after the dialogue closed meanwhile");
            AssertOneLayoutWithTheSameFrames(frames);
            AssertSourcesCallOnlyTheLiveHud("npc closed without UI, UI rebuilt");
        }

        [UnityTest]
        public IEnumerator OpenReward_HudDisabledAndEnabled_ReturnsToTheOpenPlace_AndAConfirmWhileDisabledLeavesTheDefault()
        {
            yield return ReadySyncedTown();
            Placement idle = Observe("reward idle", null);
            AssertShownWithTheFixtureValues(idle);
            yield return ShowTheReward();
            Placement open = Observe("reward open", RewardWindow());
            AssertShownWithTheFixtureValues(open);
            AssertReactsToCover(idle, open, coverRequired: false);

            GameObject hud = HudController.Instance.gameObject;
            for (int cycle = 0; cycle < 2; cycle++)
            {
                hud.SetActive(false);
                yield return _fixture.Frames(2);
                Assert.IsTrue(RewardShown(), "the reward stays open while the HUD is disabled");
                Observe($"reward open, HUD disabled {cycle}", RewardWindow());
                hud.SetActive(true);
                yield return _fixture.Frames(3);
                Placement again = Observe($"reward open, HUD enabled {cycle}", RewardWindow());
                AssertSamePlace(open, again, "after enabling with the reward still open");
                AssertShownWithTheFixtureValues(again);
                AssertNotMoreCovered(open, again);
            }

            hud.SetActive(false);
            yield return _fixture.Frames(2);
            yield return ClickConfirm();
            yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "the real Confirm pointer closes the reward while the HUD is disabled", 3);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_DungeonClearConfirm), "the Confirm click reached the wire once");
            hud.SetActive(true);
            yield return _fixture.Frames(3);
            AssertSamePlace(idle, Observe("reward confirmed while HUD disabled, HUD enabled", null), "after the reward closed meanwhile");
        }

        [UnityTest]
        public IEnumerator OpenReward_UiRebuilt_NewHudTakesTheOpenPlaceWithOnlyLiveSources_AndConfirmRestoresTheDefault()
        {
            yield return ReadySyncedTown();
            Placement idle = Observe("reward idle", null);
            AssertShownWithTheFixtureValues(idle);
            yield return ShowTheReward();
            Placement open = Observe("reward open", RewardWindow());
            AssertShownWithTheFixtureValues(open);
            AssertReactsToCover(idle, open, coverRequired: false);
            int frames = PortraitFrames(HudController.Instance.transform);

            yield return UnloadUiAndAssertNothingOfTheOldHudRemains();
            Debug.Log($"{Tag} reward shown without the UI scene (EventSystem lives there): {RewardShown()}");
            yield return LoadUi();
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return MapEntryPlayFixture.Wait(RewardShown, "the pending reward is shown again on the rebuilt UI", 3);
            yield return _fixture.Frames(3);
            Placement rebuilt = Observe("reward open, UI rebuilt", RewardWindow());
            AssertSamePlace(open, rebuilt, "on the rebuilt HUD with the reward still open");
            AssertShownWithTheFixtureValues(rebuilt);
            AssertNotMoreCovered(open, rebuilt);
            AssertOneLayoutWithTheSameFrames(frames);
            AssertSourcesCallOnlyTheLiveHud("reward open, UI rebuilt");

            yield return ClickConfirm();
            yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "the real Confirm pointer closes the reward", 3);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_DungeonClearConfirm), "the Confirm click reached the wire once");
            yield return _fixture.Frames(3);
            AssertSamePlace(idle, Observe("reward confirmed on the rebuilt UI", null), "after Confirm");
        }

        // Window replacement: the HUD follows the window that is still open, and returns only when none is.
        [UnityTest]
        public IEnumerator RewardThenNpcDialogue_ClosingTheDialogueLeavesTheRewardPlace_AndConfirmRestoresTheDefault()
        {
            yield return ReadySyncedTown();
            yield return WalkToTheNearestNpc();
            Placement idle = Observe("idle near npc", null);
            AssertShownWithTheFixtureValues(idle);
            yield return ShowTheReward();
            Placement rewardOnly = Observe("reward open", RewardWindow());
            AssertShownWithTheFixtureValues(rewardOnly);

            yield return PressEToOpenTheNpcDialogue();
            Assert.IsTrue(RewardShown(), "the reward stays open under the dialogue");
            Placement both = Observe("reward and npc open", NpcWindow());
            AssertShownWithTheFixtureValues(both);
            AssertReactsToCover(idle, both, coverRequired: true);

            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(() => !NpcDialogPanel.IsShown, "E closes the dialogue over the reward", 3);
            yield return _fixture.Frames(3);
            Assert.IsTrue(RewardShown(), "closing the dialogue leaves the reward open");
            Placement afterDialogue = Observe("reward open after npc closed", RewardWindow());
            AssertSamePlace(rewardOnly, afterDialogue, "with only the reward left open");
            AssertNotMoreCovered(rewardOnly, afterDialogue);

            yield return ClickConfirm();
            yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "the real Confirm pointer closes the reward", 3);
            yield return _fixture.Frames(3);
            AssertSamePlace(idle, Observe("reward confirmed after npc", null), "with no window left open");
        }

        // The batch screen (640x480) leaves the reward clear of the bars, so the cases above reach only the undocked branch.
        // These two run in the GUI capture runner's approved 1280x720 Game view and first prove on this run that the shown
        // reward docked the bars; a frame that does not dock fails instead of testing the default place.
        [UnityTest, Timeout(240000)]
        public IEnumerator OpenReward_ActuallyDocked_HudDisabledAndEnabled_AndConfirmWhileDisabledRestoresDefault()
        {
            yield return UseTheApprovedGameView("reward-docked-disable");
            yield return ReadySyncedTown();
            yield return WaitForShaderCompilation();
            Placement idle = Observe("docked idle", null);
            Sprite q = KnightQIcon();
            yield return Capture("01-idle");
            AssertShownWithTheFixtureValues(idle);
            AssertKnightQOnly(idle.Stage, q);
            yield return ShowTheReward();
            Placement open = Observe("docked open", RewardWindow());
            yield return Capture("02-open");
            AssertActuallyDocked(idle, open);
            AssertShownWithTheFixtureValues(open);
            AssertKnightQOnly(open.Stage, q);
            int frames = PortraitFrames(HudController.Instance.transform);
            AssertSourcesCallOnlyTheLiveHud(open.Stage);

            GameObject hud = HudController.Instance.gameObject;
            for (int cycle = 0; cycle < 2; cycle++)
            {
                hud.SetActive(false);
                yield return _fixture.Frames(2);
                Assert.IsTrue(RewardShown(), "the docked reward stays open while the HUD is disabled");
                Observe($"docked open, HUD disabled {cycle}", RewardWindow());
                if (cycle == 0) yield return Capture("03-hud-disabled");
                AssertSourcesReleaseTheDisabledHud($"docked open, HUD disabled {cycle}");
                hud.SetActive(true);
                yield return _fixture.Frames(3);
                Placement again = Observe($"docked open, HUD enabled {cycle}", RewardWindow());
                if (cycle == 0) yield return Capture("04-hud-enabled");
                AssertSamePlace(open, again, "after enabling with the reward still docked");
                AssertShownWithTheFixtureValues(again);
                AssertKnightQOnly(again.Stage, q);
                AssertNotMoreCovered(open, again);
                AssertOneHudAndOneReward(again.Stage, frames);
                AssertSourcesCallOnlyTheLiveHud(again.Stage);
            }

            hud.SetActive(false);
            yield return _fixture.Frames(2);
            AssertSourcesReleaseTheDisabledHud("docked open, HUD disabled before Confirm");
            yield return ClickConfirm();
            yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "the real Confirm pointer closes the docked reward while the HUD is disabled", 3);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_DungeonClearConfirm), "the Confirm click reached the wire once");
            hud.SetActive(true);
            yield return _fixture.Frames(3);
            Placement restored = Observe("docked reward confirmed while HUD disabled, HUD enabled", null);
            yield return Capture("05-confirmed-default");
            AssertSamePlace(idle, restored, "after the docked reward closed meanwhile");
            AssertShownWithTheFixtureValues(restored);
            AssertKnightQOnly(restored.Stage, q);
            AssertOneHudAndOneReward(restored.Stage, frames);
            AssertSourcesCallOnlyTheLiveHud(restored.Stage);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator OpenReward_ActuallyDocked_UiRebuilt_WithOnlyLiveSources_AndConfirmRestoresDefault()
        {
            yield return UseTheApprovedGameView("reward-docked-rebuilt");
            yield return ReadySyncedTown();
            yield return WaitForShaderCompilation();
            Placement idle = Observe("docked idle", null);
            Sprite q = KnightQIcon();
            yield return Capture("01-idle");
            AssertShownWithTheFixtureValues(idle);
            AssertKnightQOnly(idle.Stage, q);
            yield return ShowTheReward();
            Placement open = Observe("docked open", RewardWindow());
            yield return Capture("02-open");
            AssertActuallyDocked(idle, open);
            AssertShownWithTheFixtureValues(open);
            AssertKnightQOnly(open.Stage, q);
            int frames = PortraitFrames(HudController.Instance.transform);
            AssertSourcesCallOnlyTheLiveHud(open.Stage);

            yield return UnloadUiAndAssertNothingOfTheOldHudRemains();
            Debug.Log($"{Tag} docked reward shown without the UI scene (EventSystem lives there): {RewardShown()}");
            yield return Capture("03-ui-unloaded");
            yield return LoadUi();
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return MapEntryPlayFixture.Wait(RewardShown, "the pending reward is shown again on the rebuilt UI", 3);
            yield return _fixture.Frames(3);
            Placement rebuilt = Observe("docked open, UI rebuilt", RewardWindow());
            yield return Capture("04-rebuilt");
            AssertSamePlace(open, rebuilt, "on the rebuilt HUD with the reward still docked");
            AssertShownWithTheFixtureValues(rebuilt);
            AssertKnightQOnly(rebuilt.Stage, q);
            AssertNotMoreCovered(open, rebuilt);
            AssertOneHudAndOneReward(rebuilt.Stage, frames);
            AssertSourcesCallOnlyTheLiveHud(rebuilt.Stage);

            yield return ClickConfirm();
            yield return MapEntryPlayFixture.Wait(() => !RewardShown(), "the real Confirm pointer closes the docked reward", 3);
            Assert.AreEqual(1, _fixture.Peer.Count(PacketID.C_DungeonClearConfirm), "the Confirm click reached the wire once");
            yield return _fixture.Frames(3);
            Placement restored = Observe("docked reward confirmed on the rebuilt UI", null);
            yield return Capture("05-confirmed-default");
            AssertSamePlace(idle, restored, "after Confirm");
            AssertShownWithTheFixtureValues(restored);
            AssertKnightQOnly(restored.Stage, q);
            AssertOneHudAndOneReward(restored.Stage, frames);
            AssertSourcesCallOnlyTheLiveHud(restored.Stage);
        }

        // Same order as the live capture's Town: Town ready, the client's own inventory query, then the server snapshot.
        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "C_InventoryRequest #1", 3);
            _fixture.Peer.Send(new S_InventorySnapshot { revision = 1, currency = 1250 }.Write());
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);

            // Recorded for the D-1 review: how far the additive UI lags the synchronized Town in this run.
            bool pauseAtTown = PauseControllerActive();
            int townFrame = Time.frameCount;
            float townTime = Time.realtimeSinceStartup;
            bool pauseReached = false;
            yield return ScreenUiProbe.WaitUntil(PauseControllerActive, 10f, reached => pauseReached = reached);
            Debug.Log($"{Tag} additive UI after the synchronized Town: pause controller active at Town {pauseAtTown}, "
                + $"active {pauseReached} after {Time.frameCount - townFrame} frames / {Time.realtimeSinceStartup - townTime:F3}s, Screen {Screen.width}x{Screen.height}");
            yield return WaitHud();
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return _fixture.Frames(3);
        }

        // The live capture's NPC walk: the fixture's virtual keyboard D/A until within 0.3 of the nearest Town NPC.
        IEnumerator WalkToTheNearestNpc()
        {
            LocalPlayerMovement player = _fixture.Player;
            NpcInteractable npc = UnityEngine.Object.FindObjectsByType<NpcInteractable>()
                .Where(item => item.gameObject.scene == SceneManager.GetActiveScene())
                .OrderBy(item => Mathf.Abs(item.transform.position.x - player.transform.position.x))
                .FirstOrDefault();
            Assert.IsNotNull(npc, "fixture: the Town scene has an NPC to talk to");
            float deadline = Time.realtimeSinceStartup + 20;
            while (Mathf.Abs(player.transform.position.x - npc.transform.position.x) > 0.3f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "fixture: the virtual keyboard could not reach the NPC");
                _fixture.Keys(player.transform.position.x < npc.transform.position.x ? Key.D : Key.A);
                yield return null;
            }
            _fixture.Keys();
            yield return _fixture.Frames(4);
            yield return MapEntryPlayFixture.Wait(() => MapEntryPlayFixture.Field<bool>(npc, "_isPlayerNear"),
                "fixture: the player stands inside the NPC trigger", 3);
        }

        IEnumerator PressEToOpenTheNpcDialogue()
        {
            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(NpcShown, "E shows the NPC dialogue", 3);
            yield return _fixture.Frames(3);
        }

        IEnumerator ShowTheReward()
        {
            var result = new S_DungeonClearResult { result = (byte)InventoryResult.Success, currency = DungeonClearRewards.Currency };
            for (int i = 0; i < DungeonClearRewards.Items.Count; i++)
            {
                typeof(S_DungeonClearResult).GetField($"reward{i}ItemId").SetValue(result, (int)DungeonClearRewards.Items[i].ItemId);
                typeof(S_DungeonClearResult).GetField($"reward{i}Count").SetValue(result, DungeonClearRewards.Items[i].Count);
            }
            _fixture.Peer.Send(result.Write());
            yield return MapEntryPlayFixture.Wait(RewardShown, "S_DungeonClearResult shows the reward window", 3);
            yield return _fixture.Frames(3);
        }

        IEnumerator ClickConfirm()
        {
            Component confirm = MapEntryPlayFixture.Field<Component>(DungeonClearRewardWindow.Instance, "_confirm");
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, PartyInviteLayout.ScreenRectOf(confirm).center);
        }

        IEnumerator Press(Key key)
        {
            _fixture.Keys(key);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        IEnumerator UnloadUiAndAssertNothingOfTheOldHudRemains()
        {
            HudController oldHud = HudController.Instance;
            HudLayout oldLayout = oldHud.GetComponent<HudLayout>();
            yield return SceneManager.UnloadSceneAsync("UI");
            yield return _fixture.Frames(2);
            Assert.IsTrue(oldHud == null, "the unloaded UI destroyed its HUD");
            Assert.IsTrue(oldLayout == null, "the unloaded UI destroyed its layout");
            Assert.IsEmpty(UnityEngine.Object.FindObjectsByType<HudLayout>(), "no layout outlives its UI scene");
            Assert.IsTrue(ReferenceEquals(MapEntryPlayFixture.Field<HudController>(_fixture.Session, "_hud"), null),
                "the session released the unloaded HUD as its HP target");
            Assert.IsEmpty(Subscribers(typeof(InventoryState), InventoryState.Instance, nameof(InventoryState.OnInventoryChanged), typeof(HudController)),
                "the inventory mirror holds no unloaded HUD");
            Assert.IsEmpty(Subscribers(typeof(PartyState), PartyState.Instance, nameof(PartyState.OnPartyUpdated), typeof(PartyMemberHud)),
                "the party mirror holds no unloaded party HUD");
            Assert.IsEmpty(Subscribers(typeof(ClassLoadout), null, nameof(ClassLoadout.AuthorityChanged), typeof(SkillHudController)),
                "class authority holds no unloaded skill HUD");
        }

        IEnumerator LoadUi()
        {
            yield return SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            yield return WaitHud();
            yield return _fixture.Frames(3);
        }

        // GUI capture runner only: its permit opens the existing in-memory size scope, and Teardown restores the selection.
        // Without the runner (batch) there is no Game view to size, as for UiWindowLiveCaptureTests.
        IEnumerator UseTheApprovedGameView(string captureCase)
        {
            _captures = Environment.GetEnvironmentVariable(CaptureVariable);
            if (string.IsNullOrEmpty(_captures))
                Assert.Ignore($"the docked reward runs only when the GUI capture runner sets {CaptureVariable}");
            Assert.IsTrue(Directory.Exists(_captures), "capture output folder exists: " + _captures);
            _captureCase = captureCase;
            Debug.Log($"{Tag} {captureCase}: isBatchMode {Application.isBatchMode}, unity {Application.unityVersion}, graphics {SystemInfo.graphicsDeviceType}");
#if UNITY_EDITOR
            _gameView = GameViewTargetSize.Find(out string missing);
            Assert.IsNotNull(_gameView, "the docked reward needs the Editor Game view: " + missing);
            _gameView.EnableApprovedSizes();
            string selection = _gameView.TrySelect(DockedWidth, DockedHeight);
            Debug.Log($"{Tag} game view select {DockedWidth}x{DockedHeight}: {selection}; {_gameView.Describe()}");
            StringAssert.StartsWith("selected", selection, $"the approved {DockedWidth}x{DockedHeight} Game view size");
#else
            Assert.Fail("the docked reward needs the Editor Game view");
#endif
            bool reached = false;
            yield return ScreenUiProbe.WaitUntil(() => Screen.width == DockedWidth && Screen.height == DockedHeight, 3f, done => reached = done);
            Assert.IsTrue(reached, $"the Game view reached {DockedWidth}x{DockedHeight} (Screen {Screen.width}x{Screen.height})");
        }

        // For a readable frame only: the Editor compiles shaders on first use. It runs before the reward opens.
        IEnumerator WaitForShaderCompilation()
        {
#if UNITY_EDITOR
            bool compiling = UnityEditor.ShaderUtil.anythingCompiling;
            bool finished = false;
            yield return ScreenUiProbe.WaitUntil(() => !UnityEditor.ShaderUtil.anythingCompiling, 60f, done => finished = done);
            Debug.Log($"{Tag} shader compilation before the first frame: compiling {compiling}, finished within 60 s {finished}");
            yield return _fixture.Frames(3);
#else
            yield break;
#endif
        }

        // The Game view's final frame, written before the stage's assertions so a miss still leaves what the player saw.
        IEnumerator Capture(string stage)
        {
            yield return new WaitForEndOfFrame();
            int screenWidth = Screen.width, screenHeight = Screen.height;
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string path = Path.Combine(_captures, $"{_captureCase}-{stage}-{frame.width}x{frame.height}.png");
                Assert.IsFalse(File.Exists(path), "capture output must be new: " + path);
                File.WriteAllBytes(path, frame.EncodeToPNG());
                byte[] written = File.ReadAllBytes(path);
                string hash;
                using (var sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(written).Select(b => b.ToString("x2")));
                Debug.Log($"{Tag} capture {path} | ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame | "
                    + $"texture {frame.width}x{frame.height} | Screen {screenWidth}x{screenHeight} | png {written.Length} bytes sha256 {hash}");
                Assert.AreEqual(DockedWidth, frame.width, stage + ": frame width");
                Assert.AreEqual(DockedHeight, frame.height, stage + ": frame height");
            }
            finally { UnityEngine.Object.Destroy(frame); }
        }

        static IEnumerator WaitHud() => MapEntryPlayFixture.Wait(() => HudController.Instance != null &&
            SceneManager.GetSceneByName("UI").isLoaded, "the additive HUD scene", 10);

        static bool PauseControllerActive()
        {
            PauseMenuController pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            return pause != null && pause.isActiveAndEnabled;
        }

        static bool NpcShown() => NpcDialogPanel.IsShown && ScreenUiProbe.IsShown(NpcWindow());

        static RectTransform NpcWindow() =>
            MapEntryPlayFixture.Field<GameObject>(NpcDialogPanel.Instance, "_root").GetComponent<RectTransform>();

        // Shown as the player sees it: the window publishes its CanvasGroup in LateUpdate once the input module is ready.
        static bool RewardShown()
        {
            DungeonClearRewardWindow window = DungeonClearRewardWindow.Instance;
            CanvasGroup group = window == null ? null : window.GetComponent<CanvasGroup>();
            return group != null && group.alpha > 0f;
        }

        static RectTransform RewardWindow() => MapEntryPlayFixture.Field<RectTransform>(DungeonClearRewardWindow.Instance, "_panel");

        static Placement Observe(string stage, RectTransform window)
        {
            Transform hud = HudController.Instance.transform;
            var placement = new Placement(stage, (RectTransform)hud.Find("skill_panel"), (RectTransform)hud.Find("Gold_bar"), window);
            Debug.Log($"{Tag} {placement}");
            return placement;
        }

        static void AssertShownWithTheFixtureValues(Placement placement)
        {
            HudController hud = HudController.Instance;
            TMP_Text hp = MapEntryPlayFixture.Field<TMP_Text>(hud, "_hpText");
            TMP_Text confirmedClass = MapEntryPlayFixture.Field<TMP_Text>(hud, "_classText");
            TMP_Text gold = MapEntryPlayFixture.Field<TMP_Text>(hud, "_goldText");
            Assert.IsTrue(placement.ActionShown, placement.Stage + ": the Q action bar is shown");
            Assert.IsTrue(placement.GoldShown, placement.Stage + ": the gold plate is shown");
            Assert.IsTrue(ScreenUiProbe.IsShown(hp), placement.Stage + ": HP text is shown");
            Assert.AreEqual(ConfirmedHp, hp.text, placement.Stage + ": the fixture sent exactly 37/100");
            Assert.AreEqual("Knight", confirmedClass.text, placement.Stage + ": the scripted peer confirmed Knight");
            Assert.IsTrue(ScreenUiProbe.IsShown(gold), placement.Stage + ": gold text is shown");
            Assert.AreEqual(ConfirmedGold, gold.text, placement.Stage + ": the snapshot sent exactly 1250");
        }

        // The Q icon's real Sprite before any window opens; later stages compare with it, not with the catalog.
        static Sprite KnightQIcon()
        {
            Sprite sprite = PartyInviteLayout.SpriteOf(MapEntryPlayFixture.Field<Component>(SkillHudController.Instance, "_qIconImage"));
            Debug.Log($"{Tag} Knight Q icon before any window: {(sprite == null ? "none" : sprite.name)}");
            Assert.IsNotNull(sprite, "fixture: Knight's Q slot shows a real Sprite");
            return sprite;
        }

        // The scripted Knight's actual slots: Q shown with the Sprite seen before any window, E entirely unused.
        static void AssertKnightQOnly(string stage, Sprite q)
        {
            SkillHudController skills = SkillHudController.Instance;
            Component qIcon = MapEntryPlayFixture.Field<Component>(skills, "_qIconImage");
            Assert.IsTrue(MapEntryPlayFixture.Field<GameObject>(skills, "_qSlotRoot").activeInHierarchy, stage + ": Knight's Q slot is active");
            Assert.IsTrue(ScreenUiProbe.IsShown(qIcon), stage + ": Knight's Q icon is shown");
            Assert.AreSame(q, PartyInviteLayout.SpriteOf(qIcon), stage + ": the Q icon keeps the Sprite seen before any window");
            Assert.IsFalse(MapEntryPlayFixture.Field<GameObject>(skills, "_eSlotRoot").activeSelf, stage + ": Knight has no E slot");
        }

        // A window over the bars' default place must move them, and the move must not leave more of them covered.
        static void AssertReactsToCover(Placement idle, Placement open, bool coverRequired)
        {
            Assert.IsTrue(open.Window.HasValue, open.Stage + ": the window is shown");
            float atDefault = idle.Covered(open.Window.Value);
            Debug.Log($"{Tag} {open.Stage}: bars covered {atDefault:F1} px² at the default place, {open.Covered(open.Window.Value):F1} px² with the window open");
            if (coverRequired)
                Assert.Greater(atDefault, 0f, $"fixture: the {open.Stage} window covers the default bottom bars at Screen {Screen.width}x{Screen.height}");
            if (atDefault <= 0f) return;
            Assert.IsFalse(SamePlace(idle, open), open.Stage + ": a window over the default place moves the bottom bars");
            Assert.LessOrEqual(open.Covered(open.Window.Value), atDefault + 0.5f, open.Stage + ": moving leaves no more of the bars covered");
        }

        // The docking precondition, observed on this run: at the approved size the shown reward covers the bars' default
        // place, and with it open the HUD moved them so less of them is covered.
        static void AssertActuallyDocked(Placement idle, Placement open)
        {
            Assert.AreEqual(DockedWidth, Screen.width, open.Stage + ": Screen width");
            Assert.AreEqual(DockedHeight, Screen.height, open.Stage + ": Screen height");
            Assert.IsTrue(open.Window.HasValue, open.Stage + ": the reward is shown");
            float atDefault = idle.Covered(open.Window.Value);
            float docked = open.Covered(open.Window.Value);
            Debug.Log($"{Tag} {open.Stage}: bars covered {atDefault:F1} px² at the default place, {docked:F1} px² docked");
            Assert.Greater(atDefault, 0f, open.Stage + ": precondition: the shown reward covers the default bottom bars");
            Assert.IsFalse(SamePlace(idle, open), open.Stage + ": the bars left their default place");
            Assert.Less(docked, atDefault, open.Stage + ": docking leaves less of the bars covered");
        }

        static void AssertNotMoreCovered(Placement reference, Placement actual)
        {
            Assert.IsTrue(actual.Window.HasValue, actual.Stage + ": the window is shown");
            Assert.LessOrEqual(actual.Covered(actual.Window.Value), reference.Covered(reference.Window.Value) + 0.5f,
                $"{actual.Stage}: no more of the bars covered than at {reference.Stage}");
        }

        static void AssertSamePlace(Placement expected, Placement actual, string because)
        {
            Assert.AreEqual(expected.Action.x, actual.Action.x, Tolerance, $"{actual.Stage}: action bar x {because} (as at {expected.Stage})");
            Assert.AreEqual(expected.Action.y, actual.Action.y, Tolerance, $"{actual.Stage}: action bar y {because} (as at {expected.Stage})");
            Assert.AreEqual(expected.Gold.x, actual.Gold.x, Tolerance, $"{actual.Stage}: gold x {because} (as at {expected.Stage})");
            Assert.AreEqual(expected.Gold.y, actual.Gold.y, Tolerance, $"{actual.Stage}: gold y {because} (as at {expected.Stage})");
        }

        static bool SamePlace(Placement a, Placement b) =>
            Vector2.Distance(a.Action, b.Action) <= Tolerance && Vector2.Distance(a.Gold, b.Gold) <= Tolerance;

        static void AssertOneLayoutWithTheSameFrames(int frames)
        {
            HudLayout[] layouts = UnityEngine.Object.FindObjectsByType<HudLayout>();
            Assert.AreEqual(1, layouts.Length, "one layout for the rebuilt UI");
            Assert.AreEqual("UI", layouts[0].gameObject.scene.name);
            Assert.AreEqual(frames, PortraitFrames(layouts[0].transform), "scene-owned portrait frames are not duplicated");
        }

        static void AssertOneHudAndOneReward(string stage, int frames)
        {
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<HudController>().Length, stage + ": one live HUD");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<DungeonClearRewardWindow>().Length, stage + ": one reward window");
            AssertOneLayoutWithTheSameFrames(frames);
        }

        // A destroyed Unity object keeps its managed subscriptions, so each source must call only the live HUD parts.
        void AssertSourcesCallOnlyTheLiveHud(string stage)
        {
            HudController hud = HudController.Instance;
            Assert.IsTrue(ReferenceEquals(hud, MapEntryPlayFixture.Field<HudController>(_fixture.Session, "_hud")),
                stage + ": the session's HP target is the live HUD");
            object[] inventory = Subscribers(typeof(InventoryState), InventoryState.Instance, nameof(InventoryState.OnInventoryChanged), typeof(HudController));
            Assert.AreEqual(1, inventory.Length, stage + ": one HUD listens to the inventory mirror");
            Assert.IsTrue(ReferenceEquals(hud, inventory[0]), stage + ": the inventory mirror calls the live HUD");
            object[] party = Subscribers(typeof(PartyState), PartyState.Instance, nameof(PartyState.OnPartyUpdated), typeof(PartyMemberHud));
            Assert.AreEqual(1, party.Length, stage + ": one party HUD listens to the party mirror");
            Assert.IsTrue((UnityEngine.Object)party[0] != null, stage + ": the party mirror calls a live party HUD");
            Assert.AreEqual("UI", ((Component)party[0]).gameObject.scene.name);
            object[] skills = Subscribers(typeof(ClassLoadout), null, nameof(ClassLoadout.AuthorityChanged), typeof(SkillHudController));
            Assert.AreEqual(1, skills.Length, stage + ": one skill HUD listens to class authority");
            Assert.IsTrue(ReferenceEquals(SkillHudController.Instance, skills[0]), stage + ": class authority calls the live skill HUD");
        }

        // A disabled HUD is released by every source it subscribed to; UI.unity keeps all three parts under the HUD root.
        void AssertSourcesReleaseTheDisabledHud(string stage)
        {
            Assert.IsFalse(HudController.Instance.gameObject.activeInHierarchy, stage + ": fixture: the HUD is disabled");
            Assert.IsTrue(ReferenceEquals(MapEntryPlayFixture.Field<HudController>(_fixture.Session, "_hud"), null),
                stage + ": the session released the disabled HUD as its HP target");
            Assert.IsEmpty(Subscribers(typeof(InventoryState), InventoryState.Instance, nameof(InventoryState.OnInventoryChanged), typeof(HudController)),
                stage + ": the inventory mirror holds no disabled HUD");
            Assert.IsEmpty(Subscribers(typeof(PartyState), PartyState.Instance, nameof(PartyState.OnPartyUpdated), typeof(PartyMemberHud)),
                stage + ": the party mirror holds no disabled party HUD");
            Assert.IsEmpty(Subscribers(typeof(ClassLoadout), null, nameof(ClassLoadout.AuthorityChanged), typeof(SkillHudController)),
                stage + ": class authority holds no disabled skill HUD");
        }

        // Event subscribers of one type, destroyed or not; a null owner reads a static event.
        static object[] Subscribers(Type declaring, object owner, string eventField, Type subscriber)
        {
            BindingFlags flags = BindingFlags.NonPublic | (owner == null ? BindingFlags.Static : BindingFlags.Instance);
            FieldInfo storage = declaring.GetField(eventField, flags);
            Assert.IsNotNull(storage, $"fixture: event storage {declaring.Name}.{eventField}");
            var callbacks = storage.GetValue(owner) as Delegate;
            return callbacks == null ? Array.Empty<object>() :
                callbacks.GetInvocationList().Select(callback => callback.Target).Where(subscriber.IsInstanceOfType).ToArray();
        }

        static int PortraitFrames(Transform hud) => hud.Find("character_status").GetComponentsInChildren<Transform>(true)
            .Count(node => node.name == "PortraitFrame");

        static float OverlapArea(Rect a, Rect b)
        {
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0f && height > 0f ? width * height : 0f;
        }

        // One observation of the two bottom bars and, when a window is given, that window's shown screen rectangle.
        sealed class Placement
        {
            internal Placement(string stage, RectTransform action, RectTransform gold, RectTransform window)
            {
                Stage = stage;
                Action = action.anchoredPosition;
                Gold = gold.anchoredPosition;
                ActionRect = PartyInviteLayout.ScreenRectOf(action);
                GoldRect = PartyInviteLayout.ScreenRectOf(gold);
                ActionShown = ScreenUiProbe.IsShown(action);
                GoldShown = ScreenUiProbe.IsShown(gold);
                if (window != null && ScreenUiProbe.IsShown(window)) Window = PartyInviteLayout.ScreenRectOf(window);
            }

            internal string Stage { get; }
            internal Vector2 Action { get; }
            internal Vector2 Gold { get; }
            internal Rect ActionRect { get; }
            internal Rect GoldRect { get; }
            internal bool ActionShown { get; }
            internal bool GoldShown { get; }
            internal Rect? Window { get; }

            internal float Covered(Rect window) => OverlapArea(ActionRect, window) + OverlapArea(GoldRect, window);

            public override string ToString() =>
                $"{Stage}: Screen {Screen.width}x{Screen.height}, action {Action} {ActionRect} shown {ActionShown}, "
                + $"gold {Gold} {GoldRect} shown {GoldShown}, window {(Window.HasValue ? Window.Value.ToString() : "none")}";
        }
    }
}
