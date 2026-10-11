using System;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // R4~R8 of the defect PR (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「결함 PR의 요구」): the
    // inventory panel opens and closes with I. Real paths: InventoryPanel.BuildRuntime as CombatBootstrap calls it,
    // the session's map entry and Ready barrier, snapshots through OnRecvPacket, the connection owner's cleanup and
    // a virtual keyboard's I through the Input System. Stand-ins: the map is an unsaved empty scene named like the
    // product's test map (GameplayTestScene); Unity messages go in Unity's order to the objects BuildRuntime creates
    // (ViewLifecycle); a pause is Time.timeScale 0 as PauseMenuController sets it; a scene transition is
    // SceneTransition's unfinished active load during a map entry. "Open" is what the player gets: the panel drawn
    // (CanvasGroup alpha, Canvas, body) and taking raycasts and clicks.
    public sealed class InventoryPanelToggleTests
    {
        GameplayTestScene _scene;
        VirtualKeyboard _keyboard;
        InventoryMirrorProbe _mirror;
        GameplayConnection _connection;
        GameObject _map;
        InventoryPanel _panel;
        UnityEngine.Object _timeManager;
        bool _timeManagerWasDirty;
        float _timeScale;
        int _latency;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            Assert.IsNull(InventoryPanel.Instance, "another test left an inventory panel");
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            _timeScale = Time.timeScale;
            _timeManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0];
            _timeManagerWasDirty = EditorUtility.IsDirty(_timeManager);
            _scene = new GameplayTestScene();
            _keyboard = new VirtualKeyboard();
            _mirror = InventoryMirrorProbe.TryPublish();
            _connection = new GameplayConnection();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Time.timeScale != _timeScale) Time.timeScale = _timeScale;
                // In EditMode Time.timeScale is the TimeManager project setting; setting it marks that asset dirty and
                // Unity rewrote ProjectSettings/TimeManager.asset on exit in run editmode-red-1. The value is restored
                // above, so a mark this test made is cleared.
                if (!_timeManagerWasDirty) EditorUtility.ClearDirty(_timeManager);
                if (_map != null) UnloadMap();
                _connection?.Dispose();
            }
            finally
            {
                _mirror?.Dispose();
                _keyboard?.Dispose();
                if (!ReferenceEquals(InventoryPanel.Instance, null))
                {
                    // Only when the panel's OnDestroy failed; BuildRuntime would otherwise return a stale panel.
                    typeof(InventoryPanel).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
                }
                UnityClientSession.SimulatedLatencyMs = _latency;
                _scene?.Dispose();
            }
        }

        // R4: in a gameplay map I opens the panel and the next I closes it. I held for two frames is one press,
        // whether the implementation reacts to the key going down or coming up.
        [Test]
        public void IKey_OpensThePanel_AndTheNextPressClosesIt()
        {
            EnterFirstMap();
            PressI();
            bool openedByFirstPress = Read().Shown;
            PressI();
            bool shownAfterSecondPress = Read().Shown;
            _keyboard.Press(Key.I);
            Frame();
            _keyboard.Hold();
            Frame();
            _keyboard.Release();
            Frame();
            bool shownAfterHeldPress = Read().Shown;

            Assert.IsTrue(openedByFirstPress, "R4 I opens the panel");
            Assert.IsFalse(shownAfterSecondPress, "R4 the next I closes the panel");
            Assert.IsTrue(shownAfterHeldPress, "R4 I held for two frames toggles once: the closed panel opens");
        }

        // R5: each connection's first gameplay map starts closed, even after an earlier connection left it open.
        // The earlier connection ends through the connection owner's cleanup, then its map scene goes.
        [Test]
        public void Panel_StartsClosed_OnTheFirstMapOfEachConnection()
        {
            EnterFirstMap();
            bool shownOnFirstConnection = Read().Shown;
            PressI();
            bool openedBeforeTheEnd = Read().Shown;
            _connection.End();
            UnloadMap();
            _connection = new GameplayConnection();
            EnterFirstMap();
            bool shownOnNextConnection = Read().Shown;

            Assert.IsFalse(shownOnFirstConnection, "R5 the first map after connecting starts with the panel closed");
            Assert.IsTrue(openedBeforeTheEnd, "R4 precondition: I opened the panel on the first connection");
            Assert.IsFalse(shownOnNextConnection, "R5 the next connection's first map starts closed again");
        }

        // R5: a map move of the same connection replaces the panel; the new one keeps the last open or closed state.
        [Test]
        public void Panel_KeepsTheLastOpenOrClosedState_AcrossMapMovesOfOneConnection()
        {
            EnterFirstMap();
            InventoryPanel firstPanel = _panel;
            PressI();
            bool openBeforeMove = Read().Shown;
            MoveToMap(1);
            InventoryPanel secondPanel = _panel;
            bool openAfterMove = Read().Shown;
            PressI();
            bool shownBeforeSecondMove = Read().Shown;
            MoveToMap(2);
            bool shownAfterSecondMove = Read().Shown;

            Assert.AreNotSame(firstPanel, secondPanel, "fixture: the map move built a new panel");
            Assert.IsTrue(openBeforeMove, "R4 precondition: I opened the panel");
            Assert.IsTrue(openAfterMove, "R5 the next map's new panel is open, as it was left");
            Assert.IsFalse(shownBeforeSecondMove, "R4 precondition: I closed the panel");
            Assert.IsFalse(shownAfterSecondMove, "R5 the next map's new panel is closed, as it was left");
        }

        // R6: the closed panel is not drawn and takes no raycasts or clicks, so pointer clicks and attacks pass
        // through. Opening restores the existing behavior (drawn, buttons take raycasts and clicks).
        [Test]
        public void ClosedPanel_IsNotDrawn_AndTakesNoRaycastsOrClicks()
        {
            EnterFirstMap();
            PanelView closedAtStart = Read();
            PressI();
            PanelView opened = Read();
            PressI();
            PanelView closedByI = Read();

            Assert.IsFalse(closedAtStart.Shown, "R6 the closed panel is not drawn");
            Assert.IsFalse(closedAtStart.TakesRaycasts, "R6 the closed panel takes no raycasts, so clicks and attacks pass");
            Assert.IsFalse(closedAtStart.TakesClicks, "R6 the closed panel's buttons cannot be pressed");
            Assert.IsTrue(opened.Shown, "R8 the open panel is drawn as before");
            Assert.IsTrue(opened.TakesRaycasts, "R8 the open panel's buttons take raycasts as before");
            Assert.IsTrue(opened.TakesClicks, "R8 the open panel's buttons can be pressed as before");
            Assert.IsFalse(closedByI.Shown, "R6 closing with I hides the panel");
            Assert.IsFalse(closedByI.TakesRaycasts, "R6 closing with I stops raycasts");
            Assert.IsFalse(closedByI.TakesClicks, "R6 closing with I stops clicks");
        }

        // R7: while paused (PauseMenuController sets Time.timeScale 0) I changes nothing, open or closed; after
        // resuming, I works again.
        [Test]
        public void IKey_ChangesNothing_WhilePaused()
        {
            EnterFirstMap();
            bool shownBeforePause = Read().Shown;
            Time.timeScale = 0f;
            PressI();
            bool shownWhilePaused = Read().Shown;
            Time.timeScale = 1f;
            PressI();
            bool shownAfterResume = Read().Shown;
            Time.timeScale = 0f;
            PressI();
            bool shownWhilePausedAgain = Read().Shown;
            Time.timeScale = 1f;

            Assert.AreEqual(shownBeforePause, shownWhilePaused, "R7 I changes nothing while paused");
            Assert.AreNotEqual(shownWhilePaused, shownAfterResume, "R7 control: after resuming, I toggles the panel");
            Assert.AreEqual(shownAfterResume, shownWhilePausedAgain, "R7 I changes nothing while paused, in the other state too");
        }

        // R7: while the next map is loading (map entry begun, SceneTransition's load still active) I changes nothing.
        // After the new map's panel is Ready, I works again.
        [Test]
        public void IKey_ChangesNothing_DuringASceneTransition()
        {
            EnterFirstMap();
            PressI();
            bool shownBeforeTransition = Read().Shown;
            _connection.BeginMap(1);
            bool shownDuringTransition;
            using (var loading = new SceneLoadInFlight("HuntingGround"))
            {
                PressI();
                shownDuringTransition = Read().Shown;
                loading.Finish();
            }
            UnloadMap();
            LoadMap();
            _connection.Ready();
            Frame();
            bool shownOnTheNewMap = Read().Shown;
            PressI();
            bool shownAfterPressOnTheNewMap = Read().Shown;

            Assert.IsTrue(shownBeforeTransition, "R4 precondition: I opened the panel");
            Assert.IsTrue(shownDuringTransition, "R7 I changes nothing during the scene transition");
            Assert.IsTrue(shownOnTheNewMap, "R5 the new map keeps the open panel");
            Assert.IsFalse(shownAfterPressOnTheNewMap, "R7 control: after the transition, I closes the panel");
        }

        // R8: while closed the panel keeps its mirror and request subscriptions and the Ready query still goes out;
        // opening shows the latest snapshot at once, and the open panel shows and offers use as before.
        [Test]
        public void ClosedPanel_KeepsSubscriptionsAndQuery_AndOpeningShowsTheLatestSnapshot()
        {
            EnterFirstMap();
            bool shownAtStart = Read().Shown;
            int readyQueries = PeerFrames.Count(_connection.Sent(), InventoryWire.InventoryRequestId);
            int mirrorSubscribers = EventSubscribers.Count(InventoryState.Instance, nameof(InventoryState.OnInventoryChanged));
            int requestSubscribers = EventSubscribers.Count(_connection.Session.Inventory, nameof(InventoryRequestController.Changed));
            _connection.Deliver(new ServerInventory(2, 100));
            _connection.Deliver(new ServerInventory(3, 250, (ItemId.CoinPouch, 2)));
            Frame();
            PressI();
            bool opened = Read().Shown;
            string currency = Text("Inventory/Currency");
            string firstSlot = Text("Inventory/Slot1/Item");
            string status = Text("Inventory/Synchronization");
            Button use = _panel.transform.Find("Inventory/Slot1/Use").GetComponent<Button>();
            bool useOffered = use.gameObject.activeInHierarchy && use.interactable;

            Assert.IsFalse(shownAtStart, "R5/R6 precondition: the panel is closed");
            Assert.AreEqual(1, readyQueries, "R8 the Ready query is sent while the panel is closed");
            Assert.AreEqual(1, mirrorSubscribers, "R8 the closed panel keeps its mirror subscription");
            Assert.AreEqual(1, requestSubscribers, "R8 the closed panel keeps its request subscription");
            Assert.IsTrue(opened, "R4 precondition: I opened the panel");
            Assert.AreEqual("재화 250", currency, "R8 opening shows the latest currency");
            Assert.AreEqual("재화 주머니 ×2", firstSlot, "R8 opening shows the latest slots");
            Assert.AreEqual("서버 확인 완료", status, "R8 the answered Ready query still confirms the state");
            Assert.IsTrue(useOffered, "R8 the open panel offers use of the owned CoinPouch as before");
        }

        // CombatBootstrap on a map load: one panel under the scene's bootstrap object, then Unity starts it.
        void LoadMap()
        {
            _map = new GameObject("_CombatBootstrap");
            _panel = InventoryPanel.BuildRuntime(_map.transform);
            ViewLifecycle.Begin(_map);
        }

        // The map scene unloads with its panel.
        void UnloadMap()
        {
            ViewLifecycle.Destroy(_map);
            _map = null;
            _panel = null;
        }

        void Frame() => ViewLifecycle.Frame(_map);

        // A connection's first map: its entry begins, the map loads, the entry reaches gameplay Ready, a frame runs.
        void EnterFirstMap()
        {
            _connection.BeginMap(0);
            LoadMap();
            _connection.Ready();
            Frame();
        }

        // A map move of the same connection: the old map's panel goes and the new map builds its own.
        void MoveToMap(byte map)
        {
            _connection.BeginMap(map);
            UnloadMap();
            LoadMap();
            _connection.Ready();
            Frame();
        }

        // One press and release of I, each in its own input update followed by a frame.
        void PressI()
        {
            _keyboard.Press(Key.I);
            Frame();
            _keyboard.Release();
            Frame();
        }

        PanelView Read() => new PanelView(_panel);

        string Text(string path)
        {
            Transform label = _panel.transform.Find(path);
            Assert.IsNotNull(label, $"fixture: panel label {path}");
            return label.GetComponent<TMP_Text>().text;
        }

        // What the player gets from the panel now. Hiding by alpha, by a disabled Canvas or by an inactive body all
        // count as not drawn; raycasts need the raycaster and the group; clicks also need the group interactable.
        readonly struct PanelView
        {
            internal PanelView(InventoryPanel panel)
            {
                GameObject root = panel.gameObject;
                var canvas = root.GetComponent<Canvas>();
                var raycaster = root.GetComponent<GraphicRaycaster>();
                var group = root.GetComponent<CanvasGroup>();
                Transform body = root.transform.Find("Inventory");
                bool live = root.activeInHierarchy && body != null && body.gameObject.activeInHierarchy;
                bool drawn = live && canvas != null && canvas.enabled && (group == null || group.alpha > 0f);
                bool raycasts = live && raycaster != null && raycaster.enabled && (group == null || group.blocksRaycasts);
                Shown = drawn;
                TakesRaycasts = raycasts;
                TakesClicks = raycasts && (group == null || group.interactable);
            }

            internal bool Shown { get; }

            internal bool TakesRaycasts { get; }

            internal bool TakesClicks { get; }
        }

        // A map load in flight as the existing panel input rule reads it: SceneTransition's active request is
        // unfinished and its loader is running. The loader's coroutine cannot run in EditMode, so its queue and
        // running flag are set here and restored on dispose.
        sealed class SceneLoadInFlight : IDisposable
        {
            readonly GameObject _owner;
            readonly SceneTransition _loader;
            readonly SceneLoadQueue _loads;
            readonly SceneLoadRequest _request;

            internal SceneLoadInFlight(string sceneName)
            {
                Assert.IsNull(SceneTransition.Instance, "fixture cannot replace a scene loader");
                _owner = new GameObject("Scene loader fixture");
                _owner.SetActive(false);
                _loader = _owner.AddComponent<SceneTransition>();
                SetInstance(_loader);
                _loads = EntryBindingFixture.Field<SceneLoadQueue>(_loader, "_loads");
                _loads.Request(sceneName);
                Assert.IsTrue(_loads.TryStartNext(out _request), "fixture: the load is active");
                EntryBindingFixture.SetField(_loader, "_running", true);
            }

            public void Dispose()
            {
                Finish();
                SetInstance(null);
                UnityEngine.Object.DestroyImmediate(_owner);
            }

            internal void Finish()
            {
                _loads.Complete(_request.Id, SceneLoadStatus.Completed);
                EntryBindingFixture.SetField(_loader, "_running", false);
            }

            static void SetInstance(SceneTransition loader) =>
                typeof(SceneTransition).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { loader });
        }
    }
}
