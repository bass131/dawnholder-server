using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Gameplay;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // Current-look captures of the inventory, NPC dialogue and party invite windows on the real Town and additive UI
    // scenes, for the user's look check (01_Phases/goals/2026-10-13-demo-video-art 「06」 실화면). Each window opens by
    // its normal entry: I after the wire inventory snapshot, E inside a scene NPC's trigger, S_PartyInviteRecv from
    // the scripted peer. That peer is loopback test authority, not GameServer. Canvases, cameras and Overlay modes
    // stay as the product builds them, and the Game view's final frame is saved without editing.
    // Runs only when the capture runner names its output folder, and needs the Editor's Game view (no -batchmode).
    public sealed class UiWindowLiveCaptureTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const int PrimaryWidth = 1920, PrimaryHeight = 1080;
        const int MinimumWidth = 1280, MinimumHeight = 720;

        MapEntryPlayFixture _fixture;
        readonly List<string> _record = new();
        string _output;
        string _window;
#if UNITY_EDITOR
        GameViewTargetSize _gameView;
#endif

        // NUnit reuses one instance for every test of the class, so each test starts its own record.
        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _record.Clear();
            _output = null;
            _window = null;
#if UNITY_EDITOR
            _gameView = null;
#endif
        }

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
                    "UnityClientSession.Instance after cleanup: " + (UnityClientSession.Instance == null ? "null" : "alive"),
                });
            }
            if (restoreError != null) throw restoreError;
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Inventory_OpenedByIAfterTheWireSnapshot_CapturedFromTheGameView()
        {
            Begin("inventory");
            yield return SelectPrimarySize();
            yield return ReadySyncedTown();
            bool shownBeforeI = InventoryPanelProbe.Group.alpha > 0f;
            yield return Press(Key.I);
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Group.alpha > 0f, "I opens the inventory panel", 3);
            yield return _fixture.Frames(3);

            CanvasGroup group = InventoryPanelProbe.Group;
            Note("input path", "fixture keyboard I press/release -> InventoryPanel.Update iKey.wasPressedThisFrame");
            Note("panel shown before I", shownBeforeI);
            Note("panel alpha / blocksRaycasts", $"{group.alpha} / {group.blocksRaycasts}");
            Note("panel status text", InventoryPanelProbe.Status);
            Note("panel currency text", InventoryPanelProbe.Currency);
            for (int i = 0; i < InventoryLimits.MaxSlots; i++) Note($"panel slot {i + 1} text", InventoryPanelProbe.Slot(i));
            NoteCanvas("panel", InventoryPanel.Instance);
            yield return CaptureBothSizes(() => InventoryPanelProbe.Group.alpha > 0f);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator NpcDialogue_OpenedByEInsideASceneNpcTrigger_CapturedFromTheGameView()
        {
            Begin("npc-dialog");
            yield return SelectPrimarySize();
            yield return ReadySyncedTown();
            LocalPlayerMovement player = _fixture.Player;
            NpcInteractable npc = UnityEngine.Object.FindObjectsByType<NpcInteractable>()
                .Where(item => item.gameObject.scene == SceneManager.GetActiveScene())
                .OrderBy(item => Mathf.Abs(item.transform.position.x - player.transform.position.x))
                .FirstOrDefault();
            Assert.IsNotNull(npc, "the Town scene has an NPC to talk to");
            Note("npc", $"{npc.name} at x={npc.transform.position.x:F2}, player x={player.transform.position.x:F2}");
            yield return WalkTo(npc.transform.position.x);
            yield return MapEntryPlayFixture.Wait(() => MapEntryPlayFixture.Field<bool>(npc, "_isPlayerNear"),
                "the player stands inside the NPC trigger", 3);
            bool shownBeforeE = NpcDialogPanel.IsShown;
            yield return Press(Key.E);
            yield return MapEntryPlayFixture.Wait(() => NpcDialogPanel.IsShown, "E shows the NPC dialogue", 3);
            yield return _fixture.Frames(3);

            NpcDialogPanel panel = NpcDialogPanel.Instance;
            string sceneText = MapEntryPlayFixture.Field<string>(npc, "_dialogText");
            Sprite scenePortrait = MapEntryPlayFixture.Field<Sprite>(npc, "_portrait");
            TMP_Text shownText = MapEntryPlayFixture.Field<TMP_Text>(panel, "_dialogText");
            // A uGUI Image; this assembly reaches uGUI members by reflection (see ScreenUiProbe).
            Component portrait = MapEntryPlayFixture.Field<Component>(panel, "_portraitImage");
            var shownPortrait = (Sprite)portrait.GetType().GetProperty("sprite").GetValue(portrait);
            Note("input path", "fixture keyboard D/A walk into the trigger, then E press/release -> NpcInteractable.OnInteractPerformed -> NpcDialogPanel.Show(text, portrait)");
            Note("player x at E", player.transform.position.x.ToString("F2"));
            Note("dialogue shown before E", shownBeforeE);
            Note("dialogue text equals the scene NPC text", shownText.text == sceneText);
            Note("dialogue text", shownText.text.Replace("\n", "\\n"));
            Note("portrait active / shown sprite / scene NPC portrait", $"{portrait.gameObject.activeInHierarchy} / {(shownPortrait == null ? "none" : shownPortrait.name)} / {(scenePortrait == null ? "none" : scenePortrait.name)}");
            NoteCanvas("dialogue", panel);
            yield return CaptureBothSizes(() => NpcDialogPanel.IsShown);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator PartyInvite_ShownByTheScriptedPeersInvite_CapturedFromTheGameView()
        {
            Begin("party-invite");
            yield return SelectPrimarySize();
            yield return ReadySyncedTown();
            bool shownBeforeInvite = ScreenUiProbe.ShownTexts("파티 초대").Length > 0;
            // Test authority stand-in: the inviter is not a spawned remote player and nobody answers the invite.
            _fixture.Peer.Send(new S_PartyInviteRecv { inviterEntityId = 701, inviterClass = (byte)CharacterClass.Mage }.Write());
            yield return MapEntryPlayFixture.Wait(() => ScreenUiProbe.ShownTexts("파티 초대").Length > 0,
                "S_PartyInviteRecv shows the invite popup", 3);
            yield return _fixture.Frames(3);

            PartyInvitePopup popup = PartyInvitePopup.Instance;
            CanvasGroup group = MapEntryPlayFixture.Field<CanvasGroup>(popup, "_group");
            Note("input path", "scripted peer S_PartyInviteRecv{inviterEntityId=701, inviterClass=Mage} -> PartyInviteRecvHandler -> PartyState.SetPendingInvite -> OnInviteReceived -> PartyInvitePopup.ShowPopup");
            Note("popup shown before invite", shownBeforeInvite);
            Note("popup alpha / interactable / blocksRaycasts", $"{group.alpha} / {group.interactable} / {group.blocksRaycasts}");
            Note("popup text", MapEntryPlayFixture.Field<TMP_Text>(popup, "_inviteText").text);
            Note("PartyState pending / inviter / class", $"{PartyState.Instance.HasPendingInvite} / {PartyState.Instance.PendingInviterEntityId} / {PartyState.Instance.PendingInviterClass}");
            Note("C_PartyRespond sent", _fixture.Peer.Count(PacketID.C_PartyRespond));
            NoteCanvas("popup", popup);
            yield return CaptureBothSizes(() => ScreenUiProbe.ShownTexts("파티 초대").Length > 0);
        }

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

        IEnumerator SelectPrimarySize()
        {
#if UNITY_EDITOR
            _gameView = GameViewTargetSize.Find(out string missing);
            if (_gameView == null) Note("game view", "not found: " + missing);
            else
            {
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

        // Dungeon goal fixture order: Town ready, the client's own inventory query, then the server snapshot.
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

        IEnumerator Press(Key key)
        {
            _fixture.Keys(key);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
        }

        // The fixture's portal walk with an NPC as the goal: the same virtual keyboard D/A until within 0.3.
        IEnumerator WalkTo(float x)
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (Mathf.Abs(_fixture.Player.transform.position.x - x) > 0.3f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "virtual keyboard could not reach the NPC");
                _fixture.Keys(_fixture.Player.transform.position.x < x ? Key.D : Key.A);
                yield return null;
            }
            _fixture.Keys();
            yield return _fixture.Frames(4);
        }

        IEnumerator CaptureBothSizes(Func<bool> stillShown)
        {
            NoteScenesAndCameras();
            Note("window shown before primary capture", stillShown());
            var primary = new CaptureResult();
            yield return Capture(primary);
            Note("window still shown after primary capture", stillShown());
#if UNITY_EDITOR
            string minimum = _gameView != null ? _gameView.TrySelect(MinimumWidth, MinimumHeight) : "absent: no Game view";
            Note("game view select minimum", minimum);
            if (minimum.StartsWith("selected"))
            {
                yield return ScreenUiProbe.WaitUntil(() => Screen.width == MinimumWidth && Screen.height == MinimumHeight, 3f,
                    reached => Note("Screen reached minimum size", reached));
                yield return _fixture.Frames(3);
                Note("window still shown at minimum size", stillShown());
                yield return Capture(new CaptureResult());
            }
            if (_gameView != null) Note("game view after captures", _gameView.Describe());
#endif
            WriteRecord();
            // The PNG and record exist before these checks, so a miss still leaves the real frame for the report.
            Assert.IsTrue(primary.Written, "the primary Game view frame was written");
            Assert.IsTrue(primary.SameSizeAsScreen, "the frame has the Game view's own size");
            Assert.Greater(primary.SampledColours, 1, "the frame is not one flat colour");
            Assert.AreEqual(PrimaryWidth, primary.Width, "primary frame width");
            Assert.AreEqual(PrimaryHeight, primary.Height, "primary frame height");
        }

        sealed class CaptureResult
        {
            internal bool Written;
            internal bool SameSizeAsScreen;
            internal int Width, Height, SampledColours;
        }

        // The Game view's final frame after every camera and Overlay canvas has drawn, saved as captured.
        IEnumerator Capture(CaptureResult result)
        {
            yield return new WaitForEndOfFrame();
            int screenWidth = Screen.width, screenHeight = Screen.height;
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
                string hash;
                using (var sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(written).Select(b => b.ToString("x2")));
                Note("capture", $"{path} | method ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame | texture {frame.width}x{frame.height} {frame.format} | Screen {screenWidth}x{screenHeight} | png {written.Length} bytes sha256 {hash} | lit pixels {lit}/{pixels.Length} | sampled colours {sampled.Count} (every 97th pixel)");
                Debug.Log($"[Art 검증자 capture] {_window} {path} {frame.width}x{frame.height} sha256 {hash}");
            }
            finally { UnityEngine.Object.Destroy(frame); }
        }

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

        void NoteCanvas(string label, Component window)
        {
            Canvas canvas = window.GetComponentInParent<Canvas>(true).rootCanvas;
            Note(label + " canvas", $"{canvas.name} renderMode {canvas.renderMode}, sortingOrder {canvas.sortingOrder}, worldCamera {(canvas.worldCamera == null ? "none" : canvas.worldCamera.name)}");
        }

        void Note(string key, object value)
        {
            _record.Add($"{key}: {value}");
            Debug.Log($"[Art 검증자 capture] {_window} {key}: {value}");
        }

        void WriteRecord()
        {
            string path = Path.Combine(_output, _window + "-record.txt");
            Assert.IsFalse(File.Exists(path), "record output must be new: " + path);
            File.WriteAllLines(path, _record);
        }

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
    }

#if UNITY_EDITOR
    // The Editor Game view's fixed size, picked only from the built-in list of the current platform group: adding a
    // custom size (PlayModeWindow.SetCustomRenderingResolution) would write the user's GameViewSizes preferences.
    // The first selection is remembered and put back by Restore. GameView members are internal Editor API.
    internal sealed class GameViewTargetSize
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        readonly EditorWindow _view;
        readonly PropertyInfo _selected;
        readonly object _group;
        readonly int _original;
        bool _changed;

        GameViewTargetSize(EditorWindow view, PropertyInfo selected, object group)
        {
            _view = view;
            _selected = selected;
            _group = group;
            _original = (int)selected.GetValue(view);
        }

        // Null with the reason when there is no Game view to size, e.g. under -batchmode.
        internal static GameViewTargetSize Find(out string missing)
        {
            Assembly editor = typeof(EditorWindow).Assembly;
            Type gameViewType = editor.GetType("UnityEditor.GameView");
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            missing = gameViewType == null ? "UnityEditor.GameView type" : sizesType == null ? "UnityEditor.GameViewSizes type" : null;
            if (missing != null) return null;
            Type playModeView = editor.GetType("UnityEditor.PlayModeView");
            var main = playModeView?.GetMethod("GetMainPlayModeView", Any)?.Invoke(null, null) as EditorWindow;
            EditorWindow view = main != null && gameViewType.IsInstanceOfType(main)
                ? main
                : Resources.FindObjectsOfTypeAll(gameViewType).Cast<EditorWindow>().FirstOrDefault();
            if (view == null)
            {
                missing = $"no open Game view (Application.isBatchMode {Application.isBatchMode})";
                return null;
            }
            object sizes = sizesType.GetProperty("instance", Any).GetValue(null);
            object group = sizesType.GetProperty("currentGroup", Any).GetValue(sizes);
            return new GameViewTargetSize(view, gameViewType.GetProperty("selectedSizeIndex", Any), group);
        }

        internal string Describe() =>
            $"selected {_selected.GetValue(_view)} ({Text((int)_selected.GetValue(_view))}), original {_original} ({Text(_original)}), changed {_changed}";

        internal string ListBuiltIn() => string.Join("; ", Enumerable.Range(0, BuiltInCount).Select(i => $"{i}={Text(i)}"));

        // "selected <index> <text> via <path>" or "absent: <reason>"; never adds a size.
        internal string TrySelect(int width, int height)
        {
            for (int i = 0; i < BuiltInCount; i++)
            {
                object size = Size(i);
                if (Read(size, "sizeType").ToString() != "FixedResolution") continue;
                if ((int)Read(size, "width") != width || (int)Read(size, "height") != height) continue;
                return $"selected {i} {Text(i)} via {Select(i)}";
            }
            return $"absent: no built-in fixed {width}x{height} in the current group, and a custom size would change user preferences";
        }

        internal string Restore()
        {
            if (!_changed) return "not changed";
            string path = Select(_original);
            return $"restored {_original} ({Text(_original)}) via {path}; now {_selected.GetValue(_view)}";
        }

        string Select(int index)
        {
            _changed = true;
            // The size menu's own callback also refits the view; fall back to the property when it is missing.
            MethodInfo callback = _view.GetType().GetMethods(Any)
                .FirstOrDefault(method => method.Name == "SizeSelectionCallback" && method.GetParameters().Length == 2);
            if (callback != null)
            {
                callback.Invoke(_view, new object[] { index, null });
                _view.Repaint();
                return "SizeSelectionCallback";
            }
            _selected.SetValue(_view, index);
            _view.Repaint();
            return "selectedSizeIndex";
        }

        int BuiltInCount => (int)_group.GetType().GetMethod("GetBuiltinCount", Any).Invoke(_group, null);
        object Size(int index) => _group.GetType().GetMethod("GetGameViewSize", Any).Invoke(_group, new object[] { index });
        string Text(int index) => index >= 0 && index < (int)_group.GetType().GetMethod("GetTotalCount", Any).Invoke(_group, null)
            ? $"{Read(Size(index), "baseText")} {Read(Size(index), "sizeType")} {Read(Size(index), "width")}x{Read(Size(index), "height")}"
            : "out of range";
        static object Read(object owner, string property) => owner.GetType().GetProperty(property, Any).GetValue(owner);
    }
#endif
}
