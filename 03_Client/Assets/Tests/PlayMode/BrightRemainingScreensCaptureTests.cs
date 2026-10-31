using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // Current-look captures of the three bright PR4 screens that UiWindowLiveCaptureTests has no case for: the pause menu,
    // the quest granted popup and the ending hint, for the user's look check (goal 2026-10-29-inventory-character-select-look
    // 완료조건 11, main msg_f47b5a04fb65). Each screen opens the way the product opens it: Escape on the real pause
    // controller, the popup's own BuildRuntime/PlayThenCallback (the calls QuestIntroSequencer makes), and S_MapTransition
    // to map 3 from the scripted peer. That peer is loopback test authority, not GameServer. No click is used (main
    // msg_9cbc405339aa). Asserts are capture facts only: the frame was written at both sizes, is not one flat colour, and
    // the screen was up before and after each frame. The bright pieces' sprites, colours and contrast belong to the
    // EditMode BrightRemainingScreensTests and are only recorded here.
    // The helpers below mirror UiWindowLiveCaptureTests, whose copies are private and whose file this work may not change.
    // Runs only when the capture runner names its output folder, and needs the Editor's Game view (no -batchmode).
    public sealed class BrightRemainingScreensCaptureTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const int PrimaryWidth = 1920, PrimaryHeight = 1080;
        const int MinimumWidth = 1280, MinimumHeight = 720;
        // Once armed, EndingController pulses its hint between 0.55 and 1.0 (EndingController.Update).
        const float EndingHintLowestAlpha = 0.55f;
        static readonly string[] HudAreas = { "character_status", "Party_Info", "skill_panel", "Gold_bar", "MinimapPlaceholder", "Quest_Hud" };

        MapEntryPlayFixture _fixture;
        readonly List<string> _record = new();
        readonly List<string> _notShown = new();
        string _output;
        string _window;
#if UNITY_EDITOR
        GameViewTargetSize _gameView;
#endif

        // Mirrors UiWindowLiveCaptureTests.Setup: NUnit reuses one instance for every test of the class.
        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _record.Clear();
            _notShown.Clear();
            _output = null;
            _window = null;
#if UNITY_EDITOR
            _gameView = null;
#endif
        }

        // Mirrors UiWindowLiveCaptureTests.Teardown without its mouse restore: these cases never move the mouse.
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            string restore = "game view not changed";
            Exception restoreError = null;
#if UNITY_EDITOR
            try { if (_gameView != null) restore = _gameView.Restore(); }
            catch (Exception error) { restoreError = error; }
#endif
            yield return _fixture.Cleanup();
            // Reached only when the fixture's own unload, socket and runtime restoration finished without failing.
            if (!string.IsNullOrEmpty(_output) && _window != null)
            {
                string path = Path.Combine(_output, _window + "-teardown.txt");
                Assert.IsFalse(File.Exists(path), "teardown record must be new: " + path);
                File.WriteAllLines(path, new[]
                {
                    "utc: " + DateTime.UtcNow.ToString("o"),
                    "gameView restore: " + restore,
                    "gameView restore error: " + (restoreError?.ToString() ?? "none"),
                    "fixture cleanup: completed (peer disposed, owned connection closed, scenes unloaded, runtime state restored)",
                    "UnityClientSession.Instance after cleanup: " + (Dawnholder.Client.Network.UnityClientSession.Instance == null ? "null" : "alive"),
                    "time scale after cleanup: " + Time.timeScale,
                });
            }
            if (restoreError != null) throw restoreError;
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator PauseMenu_OpenedByEscapeInTown_CapturedFromTheGameView()
        {
            Begin("pause-menu");
            yield return SelectPrimarySize(approvedSizes: true);
            yield return ReadySyncedTown();
            // Town readiness does not wait for the additive UI scene; Escape goes in only after its real controller is active,
            // as HudGoldAndPanelToggleSceneTests.RealPauseMenu_KeepsIFromTogglingThePanel_UntilResumed waits.
            PauseMenuController pause = null;
            yield return MapEntryPlayFixture.Wait(() =>
            {
                pause = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
                return pause != null && pause.isActiveAndEnabled;
            }, "active additive pause menu controller", 10);
            // The object the controller itself switches on and off (UI.unity: MenuRoot under PauseMenuCanvas).
            GameObject menu = MapEntryPlayFixture.Field<GameObject>(pause, "_pauseCanvas");
            Assert.IsNotNull(menu, "fixture: the pause controller has its menu object");
            Note("pause controller", PartyInviteLayout.Path(pause.transform));
            Note("pause menu object", PartyInviteLayout.Path(menu.transform));
            Note("pause menu shown before Escape / time scale", $"{menu.activeInHierarchy} / {Time.timeScale}");

            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 0f, "Escape opens the pause menu", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Note("input path", "fixture keyboard Escape press/release -> UI/TogglePauseMenu -> PauseMenuController.Toggle");
            Transform canvasRoot = menu.GetComponentInParent<Canvas>(true).rootCanvas.transform;
            NoteGraphics("pause menu", canvasRoot);
            NoteHudAreas("primary", menu.transform.Find("MenuPanel"));

            var primary = new CaptureResult();
            var minimum = new CaptureResult();
            // Paused frames still render, and every wait here is realtime or frame counted, never scaled time.
            yield return CaptureBothSizes(() => PauseMenuShown(menu), primary, minimum,
                atFrame: stage => Note(stage + " pause at frame", $"menu active {menu.activeInHierarchy}, time scale {Time.timeScale}"));
            WriteRecord();

            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 1f, "Escape closes the pause menu", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Note("pause menu shown after the closing Escape / time scale", $"{menu.activeInHierarchy} / {Time.timeScale}");
            AssertCaptured(primary, minimum);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator QuestGrantedPopup_BuiltByItsRuntimeBuilderInTown_CapturedFromTheGameView()
        {
            Begin("quest-granted");
            yield return SelectPrimarySize(approvedSizes: true);
            yield return ReadySyncedTown();
            Note("background", "Town, not HuntingGround: the product shows this popup after QuestIntroSequencer's quest banner on "
                + "hunting ground entry; this case makes the same BuildRuntime and PlayThenCallback calls in Town");
            // Stands in for QuestIntroSequencer's parent (the CombatBootstrap transform); the scene unload in cleanup removes it.
            var parent = new GameObject("QuestGrantedPopupCaptureParent");
            Note("popup parent", $"{parent.name} in scene {parent.scene.name}");
            NoteScenesAndCameras();

            // The popup holds for 2 s and the size change waits up to 3 s, so each size gets its own new popup.
            var primary = new CaptureResult();
            yield return ShowPopupAndCapture(parent.transform, "primary", primary);
            var minimum = new CaptureResult();
            if (SelectMinimumSize())
            {
                yield return WaitForMinimumScreen();
                yield return ShowPopupAndCapture(parent.transform, "minimum", minimum);
            }
            NoteGameViewAfterCaptures();
            WriteRecord();
            AssertCaptured(primary, minimum);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator EndingHint_ShownAfterTheScriptedMoveToEnding_CapturedFromTheGameView()
        {
            Begin("ending");
            yield return SelectPrimarySize(approvedSizes: true);
            yield return ReadySyncedTown();
            _fixture.Peer.Send(new S_MapTransition { destMapId = 3, spawnX = 0, spawnY = 0 }.Write());
            yield return _fixture.WaitMap(3);
            EndingController ending = null;
            Transform hint = null;
            CanvasGroup group = null;
            yield return MapEntryPlayFixture.Wait(() =>
            {
                ending = UnityEngine.Object.FindAnyObjectByType<EndingController>();
                hint = ending == null ? null : ending.transform.Find("EndingHint");
                group = hint == null ? null : hint.GetComponent<CanvasGroup>();
                return group != null && group.alpha >= EndingHintLowestAlpha;
            }, "the ending hint is built and pulses at 0.55 or more", 10);
            // EndingController leaves for the main menu on any key, mouse or pad press, so nothing is pressed from here on.
            Note("input path", "scripted peer S_MapTransition destMapId 3 -> WaitMap(3); no key, mouse or pad press in Ending");
            Note("ending controller", PartyInviteLayout.Path(ending.transform));
            NoteGraphics("ending hint", hint);
            NoteHudAreas("primary", hint.Find("HintBand"));

            var primary = new CaptureResult();
            var minimum = new CaptureResult();
            yield return CaptureBothSizes(() => SceneManager.GetActiveScene().name == "Ending" && hint != null, primary, minimum,
                atFrame: stage => Note(stage + " ending at frame",
                    $"active scene {SceneManager.GetActiveScene().name}, hint alive {hint != null}, hint alpha {(group != null ? group.alpha.ToString("F3") : "none")}"));
            WriteRecord();
            AssertCaptured(primary, minimum);
        }

        static bool PauseMenuShown(GameObject menu) => menu != null && menu.activeInHierarchy && Time.timeScale == 0f;

        // One popup through the product's own fade: alpha 1, the frame, then its hold and fade out end before Destroy, as
        // QuestIntroSequencer does. The popup's alpha, sorting and size stay as BuildRuntime made them.
        IEnumerator ShowPopupAndCapture(Transform parent, string stage, CaptureResult result)
        {
            QuestGrantedPopup popup = QuestGrantedPopup.BuildRuntime(parent);
            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            bool done = false;
            float built = Time.realtimeSinceStartup;
            popup.PlayThenCallback(() => done = true);
            yield return MapEntryPlayFixture.Wait(() => group.alpha >= 1f, stage + " popup fades in to alpha 1", 3);
            Note(stage + " popup alpha 1 at realtime", $"{Time.realtimeSinceStartup:F3} (built at {built:F3})");
            NoteGraphics(stage + " popup", popup.transform);
            NoteHudAreas(stage, popup.transform.Find("Panel"));
            yield return CaptureShown(stage, () => popup != null && group.alpha >= 1f, result,
                () => Note(stage + " popup at frame", $"alpha {group.alpha:F3}, realtime {Time.realtimeSinceStartup:F3}"));
            yield return MapEntryPlayFixture.Wait(() => done, stage + " popup ends its hold and fade out", 5);
            Note(stage + " popup done at realtime", Time.realtimeSinceStartup.ToString("F3"));
            UnityEngine.Object.Destroy(popup.gameObject);
            yield return null;
        }

        // Mirrors UiWindowLiveCaptureTests.Begin, which is private to that class.
        void Begin(string window)
        {
            _output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(_output))
                Assert.Ignore($"live capture runs only when the capture runner sets {OutputVariable}");
            Assert.IsTrue(Directory.Exists(_output), "capture output folder exists: " + _output);
            _window = window;
            Note("utc", DateTime.UtcNow.ToString("o"));
            Note("Application.isPlaying / isBatchMode / unityVersion", $"{Application.isPlaying} / {Application.isBatchMode} / {Application.unityVersion}");
            Note("graphics device", $"{SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceName}");
        }

        // Mirrors UiWindowLiveCaptureTests.SelectPrimarySize, which is private to that class.
        IEnumerator SelectPrimarySize(bool approvedSizes = false)
        {
#if UNITY_EDITOR
            _gameView = GameViewTargetSize.Find(out string missing);
            if (approvedSizes && _gameView == null) throw new InvalidOperationException("Approved capture requires Game view: " + missing);
            if (_gameView == null) Note("game view", "not found: " + missing);
            else
            {
                if (approvedSizes) _gameView.EnableApprovedSizes();
                Note("game view before", _gameView.Describe());
                Note("game view built-in sizes", _gameView.ListBuiltIn());
                Note("game view select primary", _gameView.TrySelect(PrimaryWidth, PrimaryHeight));
            }
#else
            Note("game view", "not an Editor run");
#endif
            yield return ScreenUiProbe.WaitUntil(() => Screen.width == PrimaryWidth && Screen.height == PrimaryHeight, 3f,
                reached => Note("Screen reached primary size", reached));
            Note("Screen after primary selection", $"{Screen.width}x{Screen.height}");
        }

        // Mirrors UiWindowLiveCaptureTests.ReadySyncedTown, which is private to that class (dungeon goal fixture order:
        // Town ready, the client's own inventory query, then the server snapshot).
        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "C_InventoryRequest #1", 3);
            _fixture.Peer.Send(Snapshot(1, 1250, (ItemId.Material, 3), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Note("scripted peer", $"loopback 127.0.0.1:{_fixture.Peer.Port} (fixture stand-in, not GameServer; snapshot revision 1, currency 1250, Material x3, CoinPouch x1)");
            Note("session", $"handshake {_fixture.Session.HandshakeOk}, map {_fixture.Session.Entry.MapId}, state {_fixture.Session.Entry.State}, class {ClassLoadout.SessionSelectedClass}");
        }

        // Mirrors UiWindowLiveCaptureTests.CaptureBothSizes, which is private to that class. Its HUD notes and HUD overlap
        // assert are left out (an overlap is a recorded fact here, NoteHudAreas), and the screen being up is kept for the assert.
        IEnumerator CaptureBothSizes(Func<bool> stillShown, CaptureResult primary, CaptureResult minimum, Action<string> atFrame = null)
        {
            NoteScenesAndCameras();
            yield return CaptureShown("primary", stillShown, primary, () => atFrame?.Invoke("primary"));
            if (SelectMinimumSize())
            {
                yield return WaitForMinimumScreen();
                yield return CaptureShown("minimum", stillShown, minimum, () => atFrame?.Invoke("minimum"));
            }
            NoteGameViewAfterCaptures();
        }

        IEnumerator CaptureShown(string stage, Func<bool> shown, CaptureResult result, Action atFrame)
        {
            bool before = shown();
            Note($"window shown before {stage} capture", before);
            if (!before) _notShown.Add("before " + stage);
            yield return Capture(result, atFrame);
            bool after = shown();
            Note($"window still shown after {stage} capture", after);
            if (!after) _notShown.Add("after " + stage);
        }

        // The size step of UiWindowLiveCaptureTests.CaptureBothSizes, split out so the popup case can make a new popup in between.
        bool SelectMinimumSize()
        {
#if UNITY_EDITOR
            string minimum = _gameView != null ? _gameView.TrySelect(MinimumWidth, MinimumHeight) : "absent: no Game view";
            Note("game view select minimum", minimum);
            return minimum.StartsWith("selected");
#else
            Note("game view select minimum", "absent: not an Editor run");
            return false;
#endif
        }

        IEnumerator WaitForMinimumScreen()
        {
            yield return ScreenUiProbe.WaitUntil(() => Screen.width == MinimumWidth && Screen.height == MinimumHeight, 3f,
                reached => Note("Screen reached minimum size", reached));
            yield return _fixture.Frames(3);
        }

        void NoteGameViewAfterCaptures()
        {
#if UNITY_EDITOR
            if (_gameView != null) Note("game view after captures", _gameView.Describe());
#endif
        }

        // The asserts of UiWindowLiveCaptureTests.CaptureBothSizes, run after the record is written so a miss still leaves the
        // real frame and record for the report.
        void AssertCaptured(CaptureResult primary, CaptureResult minimum)
        {
            Assert.IsTrue(primary.Written, "the primary Game view frame was written");
            Assert.IsTrue(primary.SameSizeAsScreen, "the frame has the Game view's own size");
            Assert.Greater(primary.SampledColours, 1, "the frame is not one flat colour");
            Assert.AreEqual(PrimaryWidth, primary.Width, "primary frame width");
            Assert.AreEqual(PrimaryHeight, primary.Height, "primary frame height");
            Assert.IsTrue(minimum.Written, "the minimum Game view frame was written");
            Assert.IsTrue(minimum.SameSizeAsScreen, "the minimum frame has the Game view's own size");
            Assert.Greater(minimum.SampledColours, 1, "the minimum frame is not one flat colour");
            Assert.AreEqual(MinimumWidth, minimum.Width, "minimum frame width");
            Assert.AreEqual(MinimumHeight, minimum.Height, "minimum frame height");
            Assert.IsEmpty(_notShown, "the screen was up before and after each frame; missing at: " + string.Join(", ", _notShown));
        }

        // Every uGUI Image and TMP text under the screen's root, so the record lines up with the PR4 change table by path.
        // uGUI is reached by reflection because this assembly does not reference it (as PartyInviteLayout does).
        void NoteGraphics(string label, Transform root)
        {
            foreach (Component image in root.GetComponentsInChildren(PartyInviteLayout.UiType("Image"), true))
            {
                Note($"{label} image {PartyInviteLayout.Path(image.transform)}",
                    $"sprite {PartyInviteLayout.Describe(PartyInviteLayout.SpriteOf(image))}, color {PartyInviteLayout.Get(image, "color")}, "
                    + $"raycastTarget {PartyInviteLayout.Get(image, "raycastTarget")}, type {PartyInviteLayout.Get(image, "type")}, "
                    + $"shown {ScreenUiProbe.IsShown(image)}, screen rect {PartyInviteLayout.ScreenRectOf(image)}");
            }
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Note($"{label} text {PartyInviteLayout.Path(text.transform)}",
                    $"'{text.text.Replace("\n", "\\n")}' | color {text.color} | font {text.font?.name} size {text.fontSize} | "
                    + $"shown {ScreenUiProbe.IsShown(text)} | screen rect {PartyInviteLayout.ScreenRectOf(text)}");
            }
        }

        // Screen rectangles of the visible HUD areas next to the screen's own panel, recorded as facts and not asserted.
        void NoteHudAreas(string stage, Transform panel)
        {
            HudController hud = HudController.Instance;
            if (hud == null)
            {
                Note(stage + " HUD", "absent");
                return;
            }
            Rect? own = panel == null ? (Rect?)null : PartyInviteLayout.ScreenRectOf(panel.GetComponent<RectTransform>());
            Note(stage + " HUD", $"scene {hud.gameObject.scene.name}, screen panel {(panel == null ? "none" : PartyInviteLayout.Path(panel) + " " + own)}");
            foreach (string name in HudAreas)
            {
                Transform area = hud.transform.Find(name);
                if (area == null)
                {
                    Note($"{stage} HUD area {name}", "missing");
                    continue;
                }
                Rect rect = PartyInviteLayout.ScreenRectOf(area.GetComponent<RectTransform>());
                Note($"{stage} HUD area {name}", $"shown {ScreenUiProbe.IsShown(area)}, screen rect {rect}, "
                    + $"overlaps the screen panel {(own.HasValue ? rect.Overlaps(own.Value).ToString() : "n/a")}");
            }
        }

        // Mirrors UiWindowLiveCaptureTests.CaptureResult, which is private to that class.
        sealed class CaptureResult
        {
            internal bool Written;
            internal bool SameSizeAsScreen;
            internal int Width, Height, SampledColours;
        }

        // Mirrors UiWindowLiveCaptureTests.Capture, which is private to that class; atFrame records the screen's own state at
        // the frame (pause, popup alpha, ending hint alpha) and the capture note adds the realtime clock.
        // The Game view's final frame after every camera and Overlay canvas has drawn, saved as captured.
        IEnumerator Capture(CaptureResult result, Action atFrame)
        {
            yield return new WaitForEndOfFrame();
            atFrame?.Invoke();
            int screenWidth = Screen.width, screenHeight = Screen.height;
            float realtime = Time.realtimeSinceStartup;
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string path = Path.Combine(_output, $"{_window}-{frame.width}x{frame.height}.png");
                Assert.IsFalse(File.Exists(path), "capture output must be new: " + path);
                File.WriteAllBytes(path, frame.EncodeToPNG());
                byte[] written = File.ReadAllBytes(path);
                Color32[] pixels = frame.GetPixels32();
                int lit = pixels.Count(p => p.r > 8 || p.g > 8 || p.b > 8);
                var sampled = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i += 97) sampled.Add(pixels[i].r << 16 | pixels[i].g << 8 | pixels[i].b);
                result.Written = true;
                result.Width = frame.width;
                result.Height = frame.height;
                result.SameSizeAsScreen = frame.width == screenWidth && frame.height == screenHeight;
                result.SampledColours = sampled.Count;
                string hash = Sha256(written);
                Note("capture", $"{path} | method ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame | texture {frame.width}x{frame.height} {frame.format} | Screen {screenWidth}x{screenHeight} | realtime {realtime:F3} | png {written.Length} bytes sha256 {hash} | lit pixels {lit}/{pixels.Length} | sampled colours {sampled.Count} (every 97th pixel)");
                Debug.Log($"[Art 검증자 capture] {_window} {path} {frame.width}x{frame.height} sha256 {hash}");
            }
            finally { UnityEngine.Object.Destroy(frame); }
        }

        // Mirrors UiWindowLiveCaptureTests.NoteScenesAndCameras, which is private to that class.
        void NoteScenesAndCameras()
        {
            Note("active scene", SceneManager.GetActiveScene().name);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                Note("loaded scene " + i, $"{scene.name} loaded={scene.isLoaded} handle={scene.handle}");
            }
            foreach (Camera camera in Camera.allCameras)
                Note("camera " + camera.name, $"scene {camera.gameObject.scene.name}, enabled {camera.enabled}, depth {camera.depth}, targetTexture {(camera.targetTexture == null ? "none" : camera.targetTexture.name)}, targetDisplay {camera.targetDisplay}, pixelRect {camera.pixelRect}");
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>().Where(item => item.isRootCanvas && item.isActiveAndEnabled).OrderBy(item => item.sortingOrder))
                Note("root canvas " + canvas.name, $"scene {canvas.gameObject.scene.name}, renderMode {canvas.renderMode}, sortingOrder {canvas.sortingOrder}, scaleFactor {canvas.scaleFactor:F3}, worldCamera {(canvas.worldCamera == null ? "none" : canvas.worldCamera.name)}");
#if UNITY_EDITOR
            try
            {
                PlayModeWindow.GetRenderingResolution(out uint width, out uint height);
                Note("PlayModeWindow rendering resolution", $"{width}x{height}");
            }
            catch (Exception error) { Note("PlayModeWindow rendering resolution", "unavailable: " + error.GetType().Name + " " + error.Message); }
#endif
        }

        // Mirrors UiWindowLiveCaptureTests.Note, which is private to that class; the log tag is the one the runner extracts.
        void Note(string key, object value)
        {
            _record.Add($"{key}: {value}");
            Debug.Log($"[Art 검증자 capture] {_window} {key}: {value}");
        }

        // Mirrors UiWindowLiveCaptureTests.WriteRecord, which is private to that class.
        void WriteRecord()
        {
            string path = Path.Combine(_output, _window + "-record.txt");
            Assert.IsFalse(File.Exists(path), "record output must be new: " + path);
            File.WriteAllLines(path, _record);
        }

        static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }

        // Mirrors UiWindowLiveCaptureTests.Snapshot (itself the builder InventorySceneLifecycleTests keeps private).
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
    }
}
