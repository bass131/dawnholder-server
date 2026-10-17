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
using Dawnholder.Client.Rendering;
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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // Current-look captures of the inventory, NPC dialogue, party invite and dungeon reward windows on the real Town and
    // additive UI scenes, for the user's look check (01_Phases/goals/2026-10-13-demo-video-art 「06」 실화면). Each window
    // opens by its normal entry: I after the wire inventory snapshot, E inside a scene NPC's trigger, S_PartyInviteRecv
    // and S_DungeonClearResult from the scripted peer. That peer is loopback test authority, not GameServer. Canvases,
    // cameras and Overlay modes stay as the product builds them, and the Game view's final frame is saved without editing.
    // Runs only when the capture runner names its output folder, and needs the Editor's Game view (no -batchmode).
    public sealed class UiWindowLiveCaptureTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const int PrimaryWidth = 1920, PrimaryHeight = 1080;
        const int MinimumWidth = 1280, MinimumHeight = 720;
        const CharacterClass InviterClass = CharacterClass.Mage;
        // Centre of the bright 21-pixel dot in the earlier party capture (bottom-left Game view pixels). Across that run's
        // three frames it moved with the foreground world as the camera moved, so it is not drawn at a fixed screen point.
        static readonly Vector2 EarlierWhiteDot = new(1558f, 525f);

        MapEntryPlayFixture _fixture;
        readonly List<string> _record = new();
        string _output;
        string _window;
        Vector2? _mouseOriginal;
        string _mouseRestore;
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
            _mouseOriginal = null;
            _mouseRestore = null;
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
            // The fixture removes its virtual mouse in Cleanup; the position moved off screen goes back first.
            if (_mouseOriginal.HasValue)
            {
                bool queued = false;
                try
                {
                    if (_fixture.Mouse != null && _fixture.Mouse.added)
                    {
                        InputSystem.QueueStateEvent(_fixture.Mouse, new MouseState { position = _mouseOriginal.Value });
                        queued = true;
                    }
                    else _mouseRestore = "not queued: the fixture mouse is no longer added";
                }
                catch (Exception error) { _mouseRestore = "not queued: " + error.Message; }
                if (queued)
                {
                    yield return null;
                    _mouseRestore = $"{_mouseOriginal.Value} requested, read back {_fixture.Mouse.position.ReadValue()}";
                }
            }
            yield return _fixture.Cleanup();
            // Reached only when the fixture's own unload, socket and runtime restoration finished without failing.
            if (!string.IsNullOrEmpty(_output) && _window != null)
            {
                string path = Path.Combine(_output, _window + "-teardown.txt");
                Assert.IsFalse(File.Exists(path), "teardown record must be new: " + path);
                var lines = new List<string>
                {
                    "utc: " + DateTime.UtcNow.ToString("o"),
                    "gameView restore: " + restore,
                    "gameView restore error: " + (restoreError?.ToString() ?? "none"),
                    "fixture cleanup: completed (peer disposed, owned connection closed, scenes unloaded, runtime state restored)",
                    "UnityClientSession.Instance after cleanup: " + (UnityClientSession.Instance == null ? "null" : "alive"),
                };
                if (_mouseRestore != null) lines.Add("fixture mouse position restore: " + _mouseRestore);
                File.WriteAllLines(path, lines);
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

        // Also the party invite layout requirement (goal 06 「추가 사용자 결정 — 파티 초대 배치 수정」): both answer buttons'
        // whole input areas inside the panel and apart, and a title, a non-empty body naming the inviter and the buttons in
        // separate regions, with the B1 artwork and normal input kept. Every value is read from the live popup. The same
        // observer first shows on throwaway windows that it tells a well-formed window from each defect.
        [UnityTest, Timeout(240000)]
        public IEnumerator PartyInvite_ShownByTheScriptedPeersInvite_CapturedFromTheGameView()
        {
            Begin("party-invite");
            yield return SelectPrimarySize();
            yield return ReadySyncedTown();
            var failures = new List<string>();
            Assert.IsNotNull(PartyInvitePopup.Instance, "Town builds the hidden invite popup");
            TMP_Text inviteText = MapEntryPlayFixture.Field<TMP_Text>(PartyInvitePopup.Instance, "_inviteText");
            yield return PartyInviteLayoutControls.Run(InviterClass.ToString(), inviteText.font, Note, failures);
            NotePointers("pointers before invite");
            yield return MoveFixtureMouseOffScreen();
            bool shownBeforeInvite = ScreenUiProbe.ShownTexts("파티 초대").Length > 0;
            // Test authority stand-in: the inviter is not a spawned remote player. Answers are exercised after capture.
            _fixture.Peer.Send(new S_PartyInviteRecv { inviterEntityId = 701, inviterClass = (byte)InviterClass }.Write());
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
            UiWindowSkin skin = PartyInviteLayout.LoadSkin();
            PartyInviteLayout layout = PartyInviteLayout.Observe(popup.transform, PartyInviteLayout.PrefabSprite(skin, "_headerPrefab"), InviterClass.ToString());
            layout.NoteFacts("popup", Note);
            NoteChecks(layout.Checks, failures);
            NoteChecks(PopupWindowChecks(popup, group, layout, skin), failures);
            NotePointers("pointers at capture");
            NoteRenderersAround(EarlierWhiteDot, 80f);
            NoteScenesAndCameras();
            var frame = new CaptureResult();
            yield return Capture(frame);
            Note("minimum capture", "not run: this Party contract captures only current 1920x1080");
            string captureRecord = WriteRecord();
            byte[] captureRecordBytes = File.ReadAllBytes(captureRecord);
            Note("capture record", $"{captureRecord} | {captureRecordBytes.Length} bytes sha256 {Sha256(captureRecordBytes)}");
            Assert.IsTrue(frame.Written && frame.SameSizeAsScreen && frame.SampledColours > 1,
                "the actual Game view frame was saved at its own size and is not flat");
            Assert.AreEqual(PrimaryWidth, frame.Width);
            Assert.AreEqual(PrimaryHeight, frame.Height);
            yield return ObservePartyResponses(popup, group, layout, failures);
            string responsesRecord = WriteRecord(phase: "responses");
            // Read back after both writes, so these checks and the next line reach Editor.log rather than either record.
            NoteChecks(RecordChecks(captureRecord, captureRecordBytes, responsesRecord), failures);
            Note("final requirement assertion", $"reached with {failures.Count} failure(s)");
            // After the PNG and both records exist, so a failing requirement still leaves the frame and every measured value.
            Assert.IsEmpty(failures, "party invite layout requirements:\n" + string.Join("\n", failures));
        }

        // The reward window frame the user judges for goal 06 「보상 창 여백」 A: the title and reward rows keep their text
        // areas and the artwork lies behind them. The scripted peer sends the server's own reward table, which
        // DungeonClearResultHandler, DungeonClearRewardState and CombatBootstrap's Town window show. The same test runs on
        // the current project and on the before snapshot that differs only in the window's source, so it asserts the
        // opening and both frame sizes; artwork and placement are recorded for the user, not judged here.
        [UnityTest, Timeout(240000)]
        public IEnumerator DungeonClearReward_ShownByTheScriptedPeersResult_CapturedFromTheGameView()
        {
            Begin("reward");
            NoteRewardWindowSource();
            yield return SelectPrimarySize();
            yield return ReadySyncedTown();
            yield return MoveFixtureMouseOffScreen();
            bool shownBeforeResult = RewardWindowShown();
            ArraySegment<byte> wire = ServerRewardResult();
            Note("input path", "scripted peer S_DungeonClearResult -> DungeonClearResultHandler -> DungeonClearRewardState.Apply -> DungeonClearRewardWindow.LateUpdate publishes it");
            Note("result data", $"DungeonClearRewards table: Success, currency {DungeonClearRewards.Currency}, items "
                + string.Join(", ", DungeonClearRewards.Items.Select(item => $"{item.ItemId} x{item.Count}")));
            Note("result wire", $"{wire.Count} bytes {BitConverter.ToString(wire.Array, wire.Offset, wire.Count)}");
            Note("reward window shown before result", shownBeforeResult);
            _fixture.Peer.Send(wire);
            yield return MapEntryPlayFixture.Wait(RewardWindowShown, "S_DungeonClearResult shows the reward window", 3);
            yield return _fixture.Frames(3);
            yield return WaitForShaderCompilation();

            NotePointers("pointers at capture");
            NoteRewardWindow("primary");
            NoteCamerasAndPlayer("primary");
            var minimum = new CaptureResult();
            yield return CaptureBothSizes(RewardWindowShown, () =>
            {
                NoteRewardWindow("minimum");
                NoteCamerasAndPlayer("minimum");
            }, minimum);
            // The goal's minimum legal screen is part of this requirement, so a missing 1280x720 frame fails the case.
            Assert.IsTrue(minimum.Written, "the minimum Game view frame was written (see 'game view select minimum')");
            Assert.IsTrue(minimum.SameSizeAsScreen, "the minimum frame has the Game view's own size");
            Assert.AreEqual(MinimumWidth, minimum.Width, "minimum frame width");
            Assert.AreEqual(MinimumHeight, minimum.Height, "minimum frame height");
        }

        // Use the existing fixture mouse through the live input module; do not invoke listeners directly. The peer
        // observes the wire intent only, not server membership. The first invite's unmodified frame already exists.
        IEnumerator ObservePartyResponses(PartyInvitePopup popup, CanvasGroup group, PartyInviteLayout layout, List<string> failures)
        {
            foreach ((Component button, byte answer) in new[] { (layout.Accept, (byte)1), (layout.Decline, (byte)0) })
            {
                if (button == null) continue; // The missing-button requirement is already recorded.
                if (answer == 0)
                {
                    _fixture.Peer.Send(new S_PartyInviteRecv { inviterEntityId = 702, inviterClass = (byte)InviterClass }.Write());
                    yield return ScreenUiProbe.WaitUntil(() => group.alpha == 1f, 3f,
                        shown => NoteChecks(new[] { new PartyInviteLayout.Check("response.second-invite-shown", shown, $"alpha {group.alpha}") }, failures));
                }
                int before = _fixture.Peer.Count(PacketID.C_PartyRespond);
                Vector2 point = PartyInviteLayout.ScreenRectOf(button).center;
                yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, point);
                yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_PartyRespond) > before, 3f,
                    received => Note("response observed on wire", received));
                yield return _fixture.Frames(3);
                int after = _fixture.Peer.Count(PacketID.C_PartyRespond);
                C_PartyRespond packet = _fixture.Peer.Last<C_PartyRespond>(PacketID.C_PartyRespond);
                bool correctPacket = after == before + 1 && packet != null && packet.accept == answer
                    && packet.inviterEntityId == (answer == 1 ? 701 : 702);
                GameObject[] hiddenHits = new[] { layout.Accept, layout.Decline }
                    .Where(target => target != null).SelectMany(target => ScreenUiProbe.RaycastHits(PartyInviteLayout.ScreenRectOf(target).center))
                    .Where(hit => hit.transform.IsChildOf(popup.transform)).ToArray();
                NoteChecks(new[]
                {
                    new PartyInviteLayout.Check("response.one-correct-intent-" + answer, correctPacket,
                        $"before {before}, after {after}, wire inviter {packet?.inviterEntityId}, answer {packet?.accept}"),
                    new PartyInviteLayout.Check("response.hidden-input-off-" + answer,
                        group.alpha == 0f && !group.interactable && !group.blocksRaycasts && !PartyState.Instance.HasPendingInvite && hiddenHits.Length == 0,
                        $"alpha {group.alpha}, interactable {group.interactable}, blocksRaycasts {group.blocksRaycasts}, pending {PartyState.Instance.HasPendingInvite}, own hits {hiddenHits.Length}"),
                }, failures);
                yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, point);
                yield return _fixture.Frames(3);
                int afterHiddenClick = _fixture.Peer.Count(PacketID.C_PartyRespond);
                NoteChecks(new[] { new PartyInviteLayout.Check("response.hidden-click-sends-none-" + answer,
                    afterHiddenClick == after, $"before hidden click {after}, after {afterHiddenClick}") }, failures);
            }
        }

        // The capture-time record stays as written and the responses record is a separate new file. WriteRecord's own guard
        // is shown refusing an existing output on a control file this test made, so neither real record is its target.
        List<PartyInviteLayout.Check> RecordChecks(string capturePath, byte[] captureBytes, string responsesPath)
        {
            string controlPath = RecordPath("existing-output-control");
            bool controlWasNew = !File.Exists(controlPath);
            if (controlWasNew) File.WriteAllText(controlPath, "control: exists before WriteRecord targets this path\n");
            byte[] controlBefore = File.ReadAllBytes(controlPath);
            string refusal = RefusalOf(() => WriteRecord("existing-output-control"));
            byte[] controlAfter = File.ReadAllBytes(controlPath);
            byte[] captureAfter = File.ReadAllBytes(capturePath);
            string[] captureLines = File.ReadAllLines(capturePath);
            string[] responseLines = File.ReadAllLines(responsesPath);
            bool captureLeads = responseLines.Length > captureLines.Length && responseLines.Take(captureLines.Length).SequenceEqual(captureLines);
            int responseChecksInCapture = captureLines.Count(line => line.StartsWith("check response."));
            bool accepted = responseLines.Any(line => line.StartsWith("check response.one-correct-intent-1: "));
            bool declined = responseLines.Any(line => line.StartsWith("check response.one-correct-intent-0: "));
            return new List<PartyInviteLayout.Check>
            {
                new("record.separate-new-files", capturePath != responsesPath && File.Exists(capturePath) && File.Exists(responsesPath),
                    $"{capturePath} | {responsesPath}"),
                new("record.capture-unchanged", captureAfter.SequenceEqual(captureBytes),
                    $"{captureBytes.Length} bytes sha256 {Sha256(captureBytes)} -> {captureAfter.Length} bytes sha256 {Sha256(captureAfter)}"),
                new("record.capture-is-the-earlier-snapshot", captureLeads && responseChecksInCapture == 0,
                    $"capture {captureLines.Length} lines lead the responses record's {responseLines.Length}: {captureLeads}; response checks in capture {responseChecksInCapture}"),
                new("record.responses-carry-both-answers", accepted && declined,
                    $"accept intent line {accepted}, decline intent line {declined}, response checks {responseLines.Count(line => line.StartsWith("check response."))}"),
                new("record.existing-output-refused-unchanged", controlWasNew && refusal != null && controlAfter.SequenceEqual(controlBefore),
                    $"control new {controlWasNew}, refusal {refusal ?? "none"}, control {controlBefore.Length} -> {controlAfter.Length} bytes same {controlAfter.SequenceEqual(controlBefore)}"),
            };
        }

        // com.unity.ext.nunit is NUnit 3.5 based: a failed assertion is thrown, not recorded, so catching it here leaves the
        // test's outcome to the final requirement assertion.
        static string RefusalOf(Action write)
        {
            try
            {
                write();
                return null;
            }
            catch (AssertionException refusal) { return refusal.Message.Replace("\r", "").Trim().Replace("\n", " / "); }
        }

        // Kept apart from the layout observer: the live window's canvas, shown state and B1 artwork compared with the shared
        // skin's own references, and that showing the invite sends no answer.
        List<PartyInviteLayout.Check> PopupWindowChecks(PartyInvitePopup popup, CanvasGroup group, PartyInviteLayout layout, UiWindowSkin skin)
        {
            Canvas canvas = popup.GetComponentInParent<Canvas>(true).rootCanvas;
            int responses = _fixture.Peer.Count(PacketID.C_PartyRespond);
            Sprite panelSprite = PartyInviteLayout.PrefabSprite(skin, "_panelPrefab");
            Sprite livePanel = layout.Panel == null ? null : PartyInviteLayout.SpriteOf(layout.Panel);
            return new List<PartyInviteLayout.Check>
            {
                new("window.overlay-order-1200", canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder == 1200,
                    $"{canvas.renderMode} order {canvas.sortingOrder}"),
                new("window.shown-and-interactive", group.alpha == 1f && group.interactable && group.blocksRaycasts,
                    $"alpha {group.alpha} interactable {group.interactable} blocksRaycasts {group.blocksRaycasts}"),
                new("window.no-answer-on-show", responses == 0, $"C_PartyRespond {responses}"),
                new("artwork.panel-sprite", livePanel != null && livePanel == panelSprite,
                    $"live {PartyInviteLayout.Describe(livePanel)}, skin panel {PartyInviteLayout.Describe(panelSprite)}"),
                new("artwork.header-sprite", layout.Headers.Count > 0,
                    $"{layout.Headers.Count} graphic(s) carry the skin header sprite {PartyInviteLayout.Describe(PartyInviteLayout.PrefabSprite(skin, "_headerPrefab"))}"),
                PartyInviteLayout.ButtonArtwork("artwork.accept-sprites", layout.Accept, skin, "_acceptNormal", "_acceptStates"),
                PartyInviteLayout.ButtonArtwork("artwork.decline-sprites", layout.Decline, skin, "_declineNormal", "_declineStates"),
            };
        }

        void NoteChecks(IEnumerable<PartyInviteLayout.Check> checks, List<string> failures)
        {
            foreach (PartyInviteLayout.Check check in checks)
            {
                Note("check " + check.Id, (check.Pass ? "PASS" : "FAIL") + " | " + check.Detail);
                if (!check.Pass) failures.Add(check.Id + ": " + check.Detail);
            }
        }

        // Main's white dot question: the virtual and real pointers and Cursor state, read only.
        void NotePointers(string when)
        {
            Note(when + ": Cursor.visible / lockState", $"{Cursor.visible} / {Cursor.lockState}");
            Note(when + ": Mouse.current", Mouse.current == null ? "none" : Describe(Mouse.current));
            foreach (Mouse mouse in InputSystem.devices.OfType<Mouse>())
                Note($"{when}: mouse device {mouse.deviceId}", Describe(mouse) + (mouse == _fixture.Mouse ? " (fixture)" : ""));
        }

        static string Describe(Mouse mouse) =>
            $"{mouse.name} layout {mouse.layout}, added {mouse.added}, enabled {mouse.enabled}, native {mouse.native}, position {mouse.position.ReadValue()}";

        // Only the fixture's own virtual mouse moves; the OS pointer and global Cursor settings stay as they are. Teardown
        // puts the remembered position back before the fixture removes the device.
        IEnumerator MoveFixtureMouseOffScreen()
        {
            Mouse mouse = _fixture.Mouse;
            _mouseOriginal = mouse.position.ReadValue();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(-64f, -64f) });
            yield return _fixture.Frames(2);
            Note("fixture mouse moved off screen", $"{_mouseOriginal.Value} -> {mouse.position.ReadValue()}");
        }

        // What renders around a Game view pixel now, smallest on-screen bounds first. Read only: nothing is hidden or
        // changed. Gizmos the Editor draws into the Game view are not visible to this code.
        void NoteRenderersAround(Vector2 pixel, float radius)
        {
            Camera camera = Camera.main;
            Note("white dot probe", $"pixel {pixel} radius {radius}; Main Camera " + (camera == null ? "none"
                : $"position {camera.transform.position}, orthographic {camera.orthographic} size {camera.orthographicSize:F3}, cullingMask {camera.cullingMask}"));
            if (camera == null) return;
            var area = new Rect(pixel - Vector2.one * radius, Vector2.one * radius * 2f);
            var near = UnityEngine.Object.FindObjectsByType<Renderer>()
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .Select(renderer => (renderer, screen: ScreenBounds(camera, renderer.bounds)))
                .Where(item => item.screen.Overlaps(area))
                .OrderBy(item => item.screen.width * item.screen.height)
                .Take(12)
                .ToArray();
            Note("white dot candidates", near.Length);
            for (int i = 0; i < near.Length; i++)
            {
                Renderer renderer = near[i].renderer;
                Material material = renderer.sharedMaterial;
                string sprite = renderer is SpriteRenderer spriteRenderer
                    ? $", sprite {(spriteRenderer.sprite == null ? "none" : spriteRenderer.sprite.name)} color {spriteRenderer.color}"
                    : "";
                bool drawn = (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0;
                Note($"white dot candidate {i + 1}", $"{PartyInviteLayout.Path(renderer.transform)} [{renderer.GetType().Name}] scene {renderer.gameObject.scene.name}, layer {LayerMask.LayerToName(renderer.gameObject.layer)} (Main Camera {(drawn ? "draws" : "culls")} it), sorting {renderer.sortingLayerName}/{renderer.sortingOrder}, material {(material == null ? "none" : material.name + " / " + (material.shader == null ? "no shader" : material.shader.name))}{sprite}, screen {near[i].screen}");
            }
        }

        static Rect ScreenBounds(Camera camera, Bounds bounds)
        {
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? bounds.min.x : bounds.max.x, i / 2 % 2 == 0 ? bounds.min.y : bounds.max.y, i / 4 == 0 ? bounds.min.z : bounds.max.z);
                Vector2 point = camera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Which window source this run's project holds on disk: the current project or the before snapshot.
        void NoteRewardWindowSource()
        {
            string source = Path.Combine(Application.dataPath, "Scripts", "UI", "DungeonClearRewardWindow.cs");
            Note("project Assets path", Application.dataPath);
            Note("reward window source", File.Exists(source) ? $"{source} sha256 {Sha256(File.ReadAllBytes(source))}" : "missing: " + source);
        }

        // The server's reward table as the wire result; slot fields are set the way Snapshot sets inventory slots.
        static ArraySegment<byte> ServerRewardResult()
        {
            var packet = new S_DungeonClearResult { result = (byte)InventoryResult.Success, currency = DungeonClearRewards.Currency };
            for (int i = 0; i < DungeonClearRewards.Items.Count; i++)
            {
                typeof(S_DungeonClearResult).GetField($"reward{i}ItemId").SetValue(packet, (int)DungeonClearRewards.Items[i].ItemId);
                typeof(S_DungeonClearResult).GetField($"reward{i}Count").SetValue(packet, DungeonClearRewards.Items[i].Count);
            }
            return packet.Write();
        }

        // Shown as the player sees it: the window publishes its CanvasGroup in LateUpdate once the input module is ready.
        static bool RewardWindowShown()
        {
            DungeonClearRewardWindow window = DungeonClearRewardWindow.Instance;
            CanvasGroup group = window == null ? null : window.GetComponent<CanvasGroup>();
            return group != null && group.alpha > 0f;
        }

        // A project with an empty Library (the before snapshot) compiles shaders asynchronously and the Editor draws a
        // placeholder until they finish. Both variants wait the same way; the outcome is recorded, not asserted.
        IEnumerator WaitForShaderCompilation()
        {
#if UNITY_EDITOR
            bool compilingWhenShown = ShaderUtil.anythingCompiling;
            yield return ScreenUiProbe.WaitUntil(() => !ShaderUtil.anythingCompiling, 60f,
                finished => Note("shader compilation", $"compiling when shown {compilingWhenShown}, finished within 60 s {finished}"));
            yield return _fixture.Frames(3);
#else
            Note("shader compilation", "not an Editor run");
            yield break;
#endif
        }

        // The live reward window as drawn: canvas and group, then every object in draw order with its Game view rectangle,
        // its piece or text (with the glyphs' drawn bounds) and the button state, then what the pointer first reaches at each
        // text and the button. Read only; uGUI is reached by type name and reflection as in PartyInviteLayout.
        void NoteRewardWindow(string stage)
        {
            DungeonClearRewardWindow window = DungeonClearRewardWindow.Instance;
            if (window == null)
            {
                Note(stage + " reward window", "none");
                return;
            }
            Canvas canvas = window.GetComponent<Canvas>();
            CanvasGroup group = window.GetComponent<CanvasGroup>();
            Note(stage + " reward window", $"scene {window.gameObject.scene.name} (active {SceneManager.GetActiveScene().name}), Screen {Screen.width}x{Screen.height}, "
                + $"canvas {canvas.renderMode} order {canvas.sortingOrder} scaleFactor {canvas.scaleFactor:F3}, group alpha {group.alpha} interactable {group.interactable} blocksRaycasts {group.blocksRaycasts}");
            Transform[] drawOrder = window.GetComponentsInChildren<Transform>(true);
            for (int i = 1; i < drawOrder.Length; i++)
            {
                if (!(drawOrder[i] is RectTransform rect)) continue;
                var parts = new List<string>
                {
                    $"order {i}", $"active {rect.gameObject.activeInHierarchy}", $"screen {PartyInviteLayout.ScreenRectOf(rect)}",
                    $"local size {rect.rect.size}", $"scale {rect.localScale.x:F3}",
                };
                parts.AddRange(rect.GetComponents<Component>().Select(RewardPart).Where(part => part != null));
                Note($"{stage} reward {ScreenUiProbe.PathOf(rect, window.transform)}", string.Join(", ", parts));
            }
            Component[] targets = window.GetComponentsInChildren<TMP_Text>().Where(ScreenUiProbe.IsShown).Cast<Component>()
                .Concat(window.GetComponentsInChildren<Component>().Where(component => component.GetType().FullName == "UnityEngine.UI.Button"))
                .ToArray();
            foreach (Component target in targets)
            {
                Vector2 point = PartyInviteLayout.ScreenRectOf(target).center;
                GameObject[] hits = ScreenUiProbe.RaycastHits(point);
                Note($"{stage} raycast at {ScreenUiProbe.PathOf(target.transform, window.transform)} centre {point}",
                    hits.Length == 0 ? "no hit" : $"first {PartyInviteLayout.Path(hits[0].transform)} of {hits.Length} hit(s)");
            }
        }

        static string RewardPart(Component component)
        {
            if (component is TMP_Text text)
                return $"text '{text.text.Replace("\n", "\\n")}' fontSize {text.fontSize} font {(text.font == null ? "none" : text.font.name)} color {text.color}, "
                    + $"drawn glyphs {ScreenUiProbe.Bounds(text.transform, text.textBounds)}, raycastTarget {text.raycastTarget}";
            switch (component.GetType().FullName)
            {
                case "UnityEngine.UI.Image":
                    return $"image sprite {PartyInviteLayout.Describe(PartyInviteLayout.SpriteOf(component))} drawing {PartyInviteLayout.Describe((Sprite)PartyInviteLayout.Get(component, "overrideSprite"))} "
                        + $"type {PartyInviteLayout.Get(component, "type")} color {PartyInviteLayout.Get(component, "color")} raycastTarget {PartyInviteLayout.Get(component, "raycastTarget")}";
                case "UnityEngine.UI.Button":
                    return $"button interactable {PartyInviteLayout.Get(component, "interactable")} transition {PartyInviteLayout.Get(component, "transition")}";
                default:
                    return component is RectTransform || component is CanvasRenderer ? null : component.GetType().Name;
            }
        }

        // The view both source variants must share for a fair comparison. CameraFollow eases toward the player every
        // LateUpdate, so the pose is read at capture time rather than assumed.
        void NoteCamerasAndPlayer(string stage)
        {
            Note(stage + " frame", $"frameCount {Time.frameCount}, realtime {Time.realtimeSinceStartup:F3}");
            foreach (Camera camera in Camera.allCameras)
                Note($"{stage} camera {camera.name}", $"root {camera.transform.root.name}, position {camera.transform.position.ToString("F4")}, rotation {camera.transform.eulerAngles.ToString("F2")}, "
                    + $"orthographic {camera.orthographic} size {camera.orthographicSize:F4}, depth {camera.depth}, cullingMask {camera.cullingMask}, clear {camera.clearFlags} {camera.backgroundColor}, pixelRect {camera.pixelRect}");
            foreach (CameraFollow follow in UnityEngine.Object.FindObjectsByType<CameraFollow>())
            {
                Transform target = MapEntryPlayFixture.Field<Transform>(follow, "_target");
                Note($"{stage} camera follow {follow.name}", $"target {(target == null ? "none" : PartyInviteLayout.Path(target) + " at " + target.position.ToString("F4"))}, "
                    + $"offset {MapEntryPlayFixture.Field<Vector3>(follow, "_offset").ToString("F4")}, smoothing {MapEntryPlayFixture.Field<float>(follow, "_smoothing")}");
            }
            Note(stage + " player", _fixture.Player == null ? "none" : _fixture.Player.transform.position.ToString("F4"));
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

        // noteAtMinimum adds a case's own readings at the minimum size and minimumResult receives that frame's result. Both are
        // optional; the inventory and NPC dialogue cases call this without them.
        IEnumerator CaptureBothSizes(Func<bool> stillShown, Action noteAtMinimum = null, CaptureResult minimumResult = null)
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
                noteAtMinimum?.Invoke();
                yield return Capture(minimumResult ?? new CaptureResult());
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

        string RecordPath(string phase) => Path.Combine(_output, (phase == null ? _window : _window + "-" + phase) + "-record.txt");

        string WriteRecord(string phase = null)
        {
            string path = RecordPath(phase);
            Assert.IsFalse(File.Exists(path), "record output must be new: " + path);
            File.WriteAllLines(path, _record);
            return path;
        }

        static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
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

    // Where a party invite window's live uGUI objects sit on screen and what a pointer reaches there, read from the
    // generated RectTransforms, graphics, buttons and the EventSystem raycast. No expected coordinates are laid out: each
    // check compares observed rectangles with each other, plus the requirement's own fixed sizes. uGUI and the skin's
    // internal fields are reached by reflection, as ScreenUiProbe does. Buttons are told apart by their existing labels
    // 수락/거절, the panel is the largest graphic, and a title is any text centred on a graphic with the header sprite.
    internal sealed class PartyInviteLayout
    {
        // Screen pixels: edges that only touch are neither outside nor overlapping.
        const float Slack = 0.5f;

        internal sealed class Check
        {
            internal readonly string Id, Detail;
            internal readonly bool Pass;
            internal Check(string id, bool pass, string detail) { Id = id; Pass = pass; Detail = detail; }
        }

        internal readonly List<Check> Checks = new();
        internal readonly List<Component> Headers = new();
        internal Component Panel { get; private set; }
        internal Component Accept { get; private set; }
        internal Component Decline { get; private set; }
        readonly List<string> _facts = new();

        internal static PartyInviteLayout Observe(Transform root, Sprite headerSprite, string inviter)
        {
            var layout = new PartyInviteLayout();
            layout.Read(root, headerSprite, inviter);
            return layout;
        }

        internal void NoteFacts(string label, Action<string, object> note)
        {
            foreach (string fact in _facts) note(label + " " + fact.Substring(0, fact.IndexOf('|')).Trim(), fact.Substring(fact.IndexOf('|') + 1).Trim());
        }

        void Read(Transform root, Sprite headerSprite, string inviter)
        {
            Transform[] drawOrder = root.GetComponentsInChildren<Transform>(true);
            int Order(Component component) => Array.IndexOf(drawOrder, component.transform);
            Component[] all = root.GetComponentsInChildren<Component>(true);
            Component[] buttons = all.Where(c => Is(c, "UnityEngine.UI.Button") && ScreenUiProbe.IsShown(c)).ToArray();
            Component ButtonOf(Component c) => buttons.FirstOrDefault(button => c.transform.IsChildOf(button.transform));
            string Label(Component button) => string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true).Where(ScreenUiProbe.IsShown).Select(text => text.text));
            Component[] graphics = all.Where(c => !IsTextRenderer(c) && Is(c, "UnityEngine.UI.Graphic") && ScreenUiProbe.IsShown(c) && ButtonOf(c) == null).ToArray();
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true).Where(text => ScreenUiProbe.IsShown(text) && ButtonOf(text) == null).ToArray();

            Panel = graphics.OrderByDescending(graphic => Area(ScreenRectOf(graphic))).FirstOrDefault();
            Headers.AddRange(graphics.Where(graphic => graphic != Panel && headerSprite != null && SpriteOf(graphic) == headerSprite));
            Component[] decorations = graphics.Where(graphic => graphic != Panel).ToArray();
            TMP_Text[] titles = texts.Where(text => Headers.Any(header => ScreenRectOf(header).Contains(ScreenRectOf(text).center))).ToArray();
            TMP_Text[] bodies = texts.Except(titles).ToArray();
            Accept = buttons.FirstOrDefault(button => Label(button).Contains("수락"));
            Decline = buttons.FirstOrDefault(button => Label(button).Contains("거절"));

            if (Panel != null) _facts.Add($"panel | {Path(Panel.transform)} screen {ScreenRectOf(Panel)} local {Size(Panel)} sprite {Describe(SpriteOf(Panel))} raycastTarget {Get(Panel, "raycastTarget")}");
            foreach (Component header in Headers) _facts.Add($"header {Path(header.transform)} | screen {ScreenRectOf(header)} order {Order(header)}");
            foreach (TMP_Text text in texts) _facts.Add($"{(titles.Contains(text) ? "title" : "body")} text {Path(text.transform)} | '{text.text.Replace("\n", "\\n")}' screen {ScreenRectOf(text)} order {Order(text)}");
            foreach (TMP_SubMeshUI subMesh in all.OfType<TMP_SubMeshUI>().Where(ScreenUiProbe.IsShown))
                _facts.Add($"text renderer {Path(subMesh.transform)} | owner {(subMesh.textComponent == null ? "none" : Path(subMesh.textComponent.transform))}, classified as text {IsTextRenderer(subMesh)}, raycastTarget {subMesh.raycastTarget}");
            foreach (Component button in buttons) _facts.Add($"button {Path(button.transform)} | label '{Label(button)}' screen {ScreenRectOf(button)} local {Size(button)} interactable {Get(button, "interactable")} order {Order(button)}");
            foreach (Component decoration in decorations) _facts.Add($"decoration {Path(decoration.transform)} | {decoration.GetType().Name} raycastTarget {Get(decoration, "raycastTarget")} colour alpha {((Color)Get(decoration, "color")).a} screen {ScreenRectOf(decoration)} order {Order(decoration)}");

            Add("input.two-buttons", buttons.Length == 2 && Accept != null && Decline != null && Accept != Decline,
                $"{buttons.Length} shown button(s): " + string.Join(", ", buttons.Select(button => $"'{Label(button)}'")));
            if (Panel == null || Accept == null || Decline == null)
            {
                Add("layout.observable", false, $"panel {(Panel == null ? "missing" : "found")}, accept {(Accept == null ? "missing" : "found")}, decline {(Decline == null ? "missing" : "found")}");
                return;
            }
            Rect panel = ScreenRectOf(Panel), accept = ScreenRectOf(Accept), decline = ScreenRectOf(Decline);

            Add("window.panel-width-400", Mathf.Abs(Size(Panel).x - 400f) < 0.01f, $"panel local size {Size(Panel)} (height is recorded, not required)");
            Add("input.button-size-120x40", IsSize(Accept, 120f, 40f) && IsSize(Decline, 120f, 40f), $"accept {Size(Accept)}, decline {Size(Decline)}");
            Add("input.buttons-interactable", (bool)Get(Accept, "interactable") && (bool)Get(Decline, "interactable"),
                $"accept {Get(Accept, "interactable")}, decline {Get(Decline, "interactable")}");
            Add("layout.accept-inside-panel", Inside(accept, panel), $"accept {accept}, panel {panel}");
            Add("layout.decline-inside-panel", Inside(decline, panel), $"decline {decline}, panel {panel}");
            bool buttonsSeparated = accept.xMax < decline.xMin - Slack || decline.xMax < accept.xMin - Slack
                || accept.yMax < decline.yMin - Slack || decline.yMax < accept.yMin - Slack;
            Add("layout.buttons-apart", buttonsSeparated, $"positive gap required; accept {accept}, decline {decline}");

            TMP_Text[] shownTitles = titles.Where(text => !string.IsNullOrWhiteSpace(text.text)).ToArray();
            TMP_Text[] shownBodies = bodies.Where(text => !string.IsNullOrWhiteSpace(text.text)).ToArray();
            TMP_Text[] panelBodies = shownBodies.Where(text => Inside(ScreenRectOf(text), panel)).ToArray();
            Add("layout.title-shown", shownTitles.Length > 0,
                $"{Headers.Count} header graphic(s); text centred on a header: " + Quote(titles));
            Add("layout.body-shown", panelBodies.Length > 0, "non-empty text inside the panel and off the header: " + Quote(panelBodies) + "; other non-title text: " + Quote(bodies.Except(panelBodies)));
            Add("layout.body-names-inviter", panelBodies.Any(text => text.text.Contains(inviter)), $"inviter '{inviter}' in body text: " + Quote(panelBodies));

            var regions = new List<(string kind, string name, Rect rect)>();
            regions.AddRange(Headers.Select(header => ("title", "header " + header.name, ScreenRectOf(header))));
            regions.AddRange(shownTitles.Select(text => ("title", $"title '{text.text}'", ScreenRectOf(text))));
            regions.AddRange(shownBodies.Select(text => ("body", $"body '{text.text}'", ScreenRectOf(text))));
            regions.Add(("accept", "accept button", accept));
            regions.Add(("decline", "decline button", decline));
            var clashes = new List<string>();
            for (int i = 0; i < regions.Count; i++)
                for (int j = i + 1; j < regions.Count; j++)
                    if (regions[i].kind != regions[j].kind && !(IsButtonKind(regions[i].kind) && IsButtonKind(regions[j].kind)) && Overlaps(regions[i].rect, regions[j].rect))
                        clashes.Add($"{regions[i].name} {regions[i].rect} x {regions[j].name} {regions[j].rect}");
            Add("layout.regions-apart", clashes.Count == 0, clashes.Count == 0 ? "title, body and button regions are disjoint" : string.Join("; ", clashes));
            Rect[] textRegions = shownTitles.Concat(shownBodies).Select(ScreenRectOf).ToArray();
            Add("layout.buttons-below-text", textRegions.Length > 0 && textRegions.All(region => accept.yMax <= region.yMin + Slack && decline.yMax <= region.yMin + Slack),
                $"button tops {accept.yMax}/{decline.yMax}; text bottoms {string.Join(", ", textRegions.Select(region => region.yMin))}");

            Component[] catching = decorations.Where(decoration => (bool)Get(decoration, "raycastTarget")).ToArray();
            Add("decor.raycast-off", catching.Length == 0, catching.Length == 0 ? $"{decorations.Length} decoration graphic(s), none catches the pointer"
                : "catching the pointer: " + string.Join(", ", catching.Select(decoration => Path(decoration.transform))));
            var targets = new List<(string name, Component component)> { ("accept", Accept), ("decline", Decline) };
            targets.AddRange(shownTitles.Concat(shownBodies).Select(text => ($"text '{text.text}'", (Component)text)));
            var covering = (from decoration in decorations
                            where ((Color)Get(decoration, "color")).a > 0f
                            from target in targets
                            where Order(decoration) > Order(target.component) && !decoration.transform.IsChildOf(target.component.transform)
                                && Overlaps(ScreenRectOf(decoration), ScreenRectOf(target.component))
                            select $"{Path(decoration.transform)} drawn over {target.name}").ToArray();
            Add("decor.not-covering", covering.Length == 0, covering.Length == 0 ? "no decoration is drawn over a button or text" : string.Join("; ", covering));

            var unreached = new List<string>();
            var centres = new List<string>();
            foreach ((string name, Component button) in new[] { ("accept", Accept), ("decline", Decline) })
            {
                Rect rect = ScreenRectOf(button);
                foreach (Vector2 point in SamplePoints(rect))
                {
                    GameObject[] hits = ScreenUiProbe.RaycastHits(point);
                    if (point == rect.center) centres.Add($"{name} centre first hit {(hits.Length == 0 ? "none" : Path(hits[0].transform))}");
                    if (hits.Length == 0 || !hits[0].transform.IsChildOf(button.transform))
                        unreached.Add($"{name} at {point}: first hit {(hits.Length == 0 ? "none" : Path(hits[0].transform))}");
                }
            }
            Add("input.buttons-reached-by-raycast", unreached.Count == 0,
                (unreached.Count == 0 ? "centre and four inner corners of both buttons hit that button first; " : string.Join("; ", unreached) + "; ") + string.Join("; ", centres));
        }

        void Add(string id, bool pass, string detail) => Checks.Add(new Check(id, pass, detail));

        // TMP creates fallback-material submeshes as children owned by one TextMeshProUGUI. They render that text,
        // not decoration; an arbitrary Graphic under a text is still checked. All remain in real raycast results.
        static bool IsTextRenderer(Component component) => component is TMP_Text ||
            component is TMP_SubMeshUI subMesh && subMesh.textComponent != null
                && subMesh.transform.IsChildOf(subMesh.textComponent.transform);

        static bool IsButtonKind(string kind) => kind == "accept" || kind == "decline";

        static IEnumerable<Vector2> SamplePoints(Rect rect)
        {
            float inset = Mathf.Min(4f, rect.width / 4f, rect.height / 4f);
            yield return rect.center;
            yield return new Vector2(rect.xMin + inset, rect.yMin + inset);
            yield return new Vector2(rect.xMax - inset, rect.yMin + inset);
            yield return new Vector2(rect.xMin + inset, rect.yMax - inset);
            yield return new Vector2(rect.xMax - inset, rect.yMax - inset);
        }

        static string Quote(IEnumerable<TMP_Text> texts)
        {
            string[] items = texts.Select(text => $"'{text.text.Replace("\n", "\\n")}' {ScreenRectOf(text)}").ToArray();
            return items.Length == 0 ? "none" : string.Join(", ", items);
        }

        static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - Slack && inner.xMax <= outer.xMax + Slack && inner.yMin >= outer.yMin - Slack && inner.yMax <= outer.yMax + Slack;

        static bool Overlaps(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > Slack && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > Slack;

        static float Area(Rect rect) => rect.width * rect.height;

        static Vector2 Size(Component component) => ((RectTransform)component.transform).rect.size;

        static bool IsSize(Component component, float width, float height) =>
            Mathf.Abs(Size(component).x - width) < 0.01f && Mathf.Abs(Size(component).y - height) < 0.01f;

        // Screen pixels of the RectTransform's four corners, whatever the canvas mode.
        internal static Rect ScreenRectOf(Component component)
        {
            var corners = new Vector3[4];
            ((RectTransform)component.transform).GetWorldCorners(corners);
            Canvas canvas = component.GetComponentInParent<Canvas>(true);
            Camera camera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        internal static string Path(Transform transform) => transform.parent == null ? transform.name : Path(transform.parent) + "/" + transform.name;

        internal static UiWindowSkin LoadSkin()
        {
            var skin = Resources.Load<UiWindowSkin>("UI/Components/UiWindowSkin");
            Assert.IsNotNull(skin, "fixture: the shared UI skin resource");
            return skin;
        }

        internal static Sprite PrefabSprite(UiWindowSkin skin, string prefabField) => SpriteOf(MapEntryPlayFixture.Field<Component>(skin, prefabField));

        internal static Sprite SpriteOf(Component graphic) => graphic.GetType().GetProperty("sprite")?.GetValue(graphic) as Sprite;

        // The button's normal sprite and its four state sprites equal the skin's references for that style.
        internal static Check ButtonArtwork(string id, Component button, UiWindowSkin skin, string normalField, string statesField)
        {
            if (button == null) return new Check(id, false, "button missing");
            Sprite normal = MapEntryPlayFixture.Field<Sprite>(skin, normalField);
            object expected = skin.GetType().GetField(statesField, MapEntryPlayFixture.Private).GetValue(skin);
            object actual = Get(button, "spriteState");
            string[] states = { "highlightedSprite", "pressedSprite", "selectedSprite", "disabledSprite" };
            Sprite live = SpriteOf((Component)Get(button, "targetGraphic"));
            string[] different = states.Where(state => (Sprite)Get(actual, state) != (Sprite)Get(expected, state))
                .Select(state => $"{state} live {Describe((Sprite)Get(actual, state))} skin {Describe((Sprite)Get(expected, state))}").ToArray();
            return new Check(id, live == normal && different.Length == 0,
                $"normal live {Describe(live)} skin {Describe(normal)}; states " + (different.Length == 0 ? "all four equal the skin's" : string.Join("; ", different)));
        }

        internal static string Describe(Sprite sprite)
        {
            if (sprite == null) return "none";
#if UNITY_EDITOR
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _)) return $"{sprite.name} ({guid})";
#endif
            return sprite.name;
        }

        internal static Type UiType(string name)
        {
            Type type = Type.GetType($"UnityEngine.UI.{name}, UnityEngine.UI");
            Assert.IsNotNull(type, "fixture: uGUI type " + name);
            return type;
        }

        internal static object Get(object owner, string property) => owner.GetType().GetProperty(property).GetValue(owner);

        internal static void Set(object owner, string property, object value) => owner.GetType().GetProperty(property).SetValue(owner, value);

        static bool Is(Component component, string fullName)
        {
            for (Type type = component == null ? null : component.GetType(); type != null; type = type.BaseType)
                if (type.FullName == fullName) return true;
            return false;
        }
    }

    // Throwaway invite-shaped windows that show PartyInviteLayout telling a well-formed window from each defect it must
    // catch. Built from the shared skin prefabs on their own top canvas before the real invite, never captured, and
    // destroyed before the popup is shown. The product popup is not touched.
    internal static class PartyInviteLayoutControls
    {
        sealed class Spec
        {
            internal string Name;
            internal string[] MustFail = Array.Empty<string>();
            internal Vector2 Accept = new(-70f, -52f), Decline = new(70f, -52f);
            internal Vector2 BodyPosition = new(0f, 4f), BodySize = new(360f, 36f);
            internal bool Title = true, EmptyBody, Blocker, BlockerUnderText;
        }

        // Panel 400x180 centred; header and title (0,52) 304x48; body (0,4) 360x36; buttons 120x40 at (-70,-52)/(70,-52).
        static readonly Spec[] Specs =
        {
            new() { Name = "well-formed" },
            new() { Name = "accept below the panel", Accept = new(-70f, -140f), MustFail = new[] { "layout.accept-inside-panel" } },
            new() { Name = "buttons overlapping", Decline = new(-30f, -52f), MustFail = new[] { "layout.buttons-apart" } },
            new() { Name = "buttons touching", Decline = new(50f, -52f), MustFail = new[] { "layout.buttons-apart" } },
            new() { Name = "body over the buttons", BodyPosition = new(0f, -40f), BodySize = new(360f, 60f), MustFail = new[] { "layout.regions-apart" } },
            new() { Name = "empty body", EmptyBody = true, MustFail = new[] { "layout.body-shown", "layout.body-names-inviter" } },
            new() { Name = "no title", Title = false, MustFail = new[] { "layout.title-shown" } },
            new() { Name = "pointer-catching decoration over accept", Blocker = true,
                MustFail = new[] { "decor.raycast-off", "decor.not-covering", "input.buttons-reached-by-raycast" } },
            new() { Name = "pointer-catching decoration under text", Blocker = true, BlockerUnderText = true,
                MustFail = new[] { "decor.raycast-off", "decor.not-covering", "input.buttons-reached-by-raycast" } },
            new() { Name = "buttons above the text", Accept = new(-70f, 63f), Decline = new(70f, 63f),
                MustFail = new[] { "layout.buttons-below-text" } },
        };

        // The well-formed control must pass every check; each defect control must fail at least its own checks.
        internal static IEnumerator Run(string inviter, TMP_FontAsset font, Action<string, object> note, List<string> failures)
        {
            UiWindowSkin skin = PartyInviteLayout.LoadSkin();
            Sprite headerSprite = PartyInviteLayout.PrefabSprite(skin, "_headerPrefab");
            foreach (Spec spec in Specs)
            {
                GameObject root = Build(spec, inviter, font, skin);
                PartyInviteLayout layout;
                try
                {
                    // Graphics get their raycast depth when the canvas first draws.
                    yield return null;
                    yield return null;
                    layout = PartyInviteLayout.Observe(root.transform, headerSprite, inviter);
                }
                finally { UnityEngine.Object.Destroy(root); }
                string[] failing = layout.Checks.Where(check => !check.Pass).Select(check => check.Id).ToArray();
                layout.NoteFacts("control " + spec.Name, note);
                bool expected = spec.MustFail.Length == 0 ? failing.Length == 0 : spec.MustFail.All(id => failing.Contains(id));
                note("control " + spec.Name, $"{(expected ? "as expected" : "UNEXPECTED")} | must fail [{string.Join(", ", spec.MustFail)}] | failing [{string.Join(", ", failing)}]");
                if (!expected)
                    failures.Add($"control '{spec.Name}' must fail [{string.Join(", ", spec.MustFail)}] but failing [{string.Join(", ", failing)}]: "
                        + string.Join(" / ", layout.Checks.Select(check => $"{check.Id} {(check.Pass ? "PASS" : "FAIL")} {check.Detail}")));
            }
            yield return null;
        }

        static GameObject Build(Spec spec, string inviter, TMP_FontAsset font, UiWindowSkin skin)
        {
            var root = new GameObject("PartyInviteLayoutControl " + spec.Name);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            root.AddComponent(PartyInviteLayout.UiType("GraphicRaycaster"));
            Component panel = UnityEngine.Object.Instantiate(MapEntryPlayFixture.Field<Component>(skin, "_panelPrefab"), root.transform, false);
            PartyInviteLayout.Set(panel, "raycastTarget", true);
            Place(panel, Vector2.zero, new Vector2(400f, 180f));
            Place(UnityEngine.Object.Instantiate(MapEntryPlayFixture.Field<Component>(skin, "_headerPrefab"), panel.transform, false), new Vector2(0f, 52f), new Vector2(304f, 48f));
            if (spec.Title) Place(Text(panel.transform, "파티 초대", font), new Vector2(0f, 52f), new Vector2(304f, 48f));
            Place(Text(panel.transform, spec.EmptyBody ? "" : inviter + " 파티 초대", font), spec.BodyPosition, spec.BodySize);
            Component buttonPrefab = MapEntryPlayFixture.Field<Component>(skin, "_buttonPrefab");
            foreach ((string label, Vector2 position) in new[] { ("수락", spec.Accept), ("거절", spec.Decline) })
            {
                Component button = UnityEngine.Object.Instantiate(buttonPrefab, panel.transform, false);
                Place(button, position, new Vector2(120f, 40f));
                var labelRect = (RectTransform)Text(button.transform, label, font).transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
            if (spec.Blocker)
            {
                var blocker = new GameObject("Blocker", typeof(RectTransform));
                // Keep this late-drawn graphic above the button even when a TMP object owns its transform.
                Transform parent = spec.BlockerUnderText ? Text(panel.transform, "", font).transform : panel.transform;
                if (spec.BlockerUnderText) Place(parent, Vector2.zero, new Vector2(400f, 180f));
                blocker.transform.SetParent(parent, false);
                Place(blocker.AddComponent(PartyInviteLayout.UiType("Image")), spec.Accept, new Vector2(120f, 40f));
            }
            return root;
        }

        static TMP_Text Text(Transform parent, string value, TMP_FontAsset font)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static void Place(Component component, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)component.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
