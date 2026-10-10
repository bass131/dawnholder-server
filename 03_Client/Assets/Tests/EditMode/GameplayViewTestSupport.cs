using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Dawnholder.Client.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests
{
    // One connection as the HUD and the inventory panel see it: a published, handshaken session on a loopback
    // socket whose map entry passes the real Ready barrier (EntryBindingFixture). Snapshots enter through
    // UnityClientSession.OnRecvPacket. End() follows the connection owner's order on a lost connection, as
    // ClientSessionMirrorResetTests does: transport close, session cleanup, then
    // NetworkService.ResetGlobalSessionMirrors. No static value is cleared by hand.
    internal sealed class GameplayConnection : IDisposable
    {
        readonly ManualConnectionQueue _queue = new();
        readonly TestSocketPair _sockets = new();
        readonly EntryBindingFixture _views;
        bool _ended;

        internal GameplayConnection()
        {
            // Send delays and inventory response deadlines are captured and never elapse in these tests.
            Session = new UnityClientSession(() => true, _queue.Post, NeverElapses, NeverElapses);
            Session.Start(_sockets.Client);
            Session.Publish();
            SessionTestTools.Handshake(Session, _queue);
            _views = new EntryBindingFixture(Session);
        }

        internal UnityClientSession Session { get; }

        public void Dispose() => End();

        // S_EnterMap: the next map's entry begins; its scene and views follow.
        internal void BeginMap(byte map) => _views.Begin(map);

        // The scene's views are bound and the server HP arrived: gameplay Ready, which sends the inventory query.
        internal void Ready() => _views.Ready();

        internal void Deliver(ServerInventory inventory)
        {
            Session.OnRecvPacket(new ArraySegment<byte>(inventory.Wire()));
            _queue.Drain();
        }

        internal List<byte[]> Sent() => PeerFrames.Take(_sockets);

        internal void End()
        {
            if (_ended) return;
            _ended = true;
            try
            {
                Session.Disconnect();
                _queue.Drain();
            }
            finally
            {
                try
                {
                    Session.Cleanup();
                    NetworkService.ResetGlobalSessionMirrors();
                }
                finally
                {
                    _views.Dispose();
                    _sockets.Dispose();
                }
            }
        }

        static void NeverElapses(Action callback, float seconds)
        {
        }
    }

    // EditMode sends no Unity messages to ordinary MonoBehaviours. These send them in Unity's order to every
    // Dawnholder.Client component under one root, including components a view adds to itself, so the views run
    // their own Awake/OnEnable/Start, a frame's Update/LateUpdate, and OnDisable/OnDestroy.
    internal static class ViewLifecycle
    {
        const BindingFlags DeclaredInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        internal static void Begin(GameObject root)
        {
            List<MonoBehaviour> views = Views(root);
            foreach (MonoBehaviour view in views)
            {
                Send(view, "Awake");
                Send(view, "OnEnable");
            }
            foreach (MonoBehaviour view in views) Send(view, "Start");
        }

        internal static void Frame(GameObject root)
        {
            List<MonoBehaviour> views = Views(root);
            foreach (MonoBehaviour view in views) Send(view, "Update");
            foreach (MonoBehaviour view in views) Send(view, "LateUpdate");
        }

        internal static void Disable(GameObject root)
        {
            foreach (MonoBehaviour view in Views(root)) Send(view, "OnDisable");
        }

        // A scene unload: OnDisable (unless already disabled), OnDestroy, then the objects are gone.
        internal static void Destroy(GameObject root, bool alreadyDisabled = false)
        {
            List<MonoBehaviour> views = Views(root);
            if (!alreadyDisabled)
            {
                foreach (MonoBehaviour view in views) Send(view, "OnDisable");
            }
            foreach (MonoBehaviour view in views) Send(view, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(root);
        }

        static List<MonoBehaviour> Views(GameObject root)
        {
            var views = new List<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour.GetType().Assembly == typeof(UnityClientSession).Assembly) views.Add(behaviour);
            }
            return views;
        }

        static void Send(MonoBehaviour view, string message)
        {
            for (Type type = view.GetType(); type != typeof(MonoBehaviour); type = type.BaseType)
            {
                MethodInfo method = type.GetMethod(message, DeclaredInstance, null, Type.EmptyTypes, null);
                if (method == null) continue;
                try
                {
                    method.Invoke(view, null);
                }
                catch (TargetInvocationException error)
                {
                    ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                }
                return;
            }
        }
    }

    internal static class EventSubscribers
    {
        // A field-like event keeps its delegate in a private field of the same name (as InventoryPresentationTests reads it).
        internal static int Count(object owner, string eventName)
        {
            FieldInfo field = owner.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"fixture: {owner.GetType().Name}.{eventName} event storage");
            return (field.GetValue(owner) as Delegate)?.GetInvocationList().Length ?? 0;
        }
    }

    // InventoryPanel draws itself only while the active scene and its own scene are gameplay maps. EditMode cannot
    // load a map without its runtime bootstrap, so an unsaved empty scene takes the product's test map name
    // (CombatBootstrap.CombatScenes) and becomes active. Nothing is saved to disk.
    internal sealed class GameplayTestScene : IDisposable
    {
        internal const string Name = "GameplayTest";
        readonly Scene _previous;
        readonly bool _replacedRunnerScene;

        internal GameplayTestScene()
        {
            _previous = SceneManager.GetActiveScene();
            try
            {
                Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            catch (Exception error)
            {
                // Unity may refuse an additive scene next to an untitled one; the runner's empty scene is replaced then.
                TestContext.Out.WriteLine($"fixture: additive scene refused ({error.Message}); using a single scene");
                Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                _replacedRunnerScene = true;
            }
            try
            {
                MethodInfo rename = typeof(Scene).GetProperty("name")?.GetSetMethod();
                Assert.IsNotNull(rename, "fixture: Scene.name has no public setter in this Unity version");
                rename.Invoke(Scene, new object[] { Name }); // the name is native state behind the scene handle
                SceneManager.SetActiveScene(Scene);
                Assert.AreEqual(Name, SceneManager.GetActiveScene().name, "fixture: the active scene has the gameplay map name");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Scene Scene { get; }

        public void Dispose()
        {
            if (_replacedRunnerScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }
            if (_previous.IsValid()) SceneManager.SetActiveScene(_previous);
            if (Scene.IsValid()) EditorSceneManager.CloseScene(Scene, true);
        }
    }

    // A virtual keyboard made Keyboard.current for one test, as MapEntryPlayFixture does in PlayMode.
    // Edit mode normally runs only editor input updates. Their buffer flip never stamps the device's update step
    // (InputManager.FlipBuffersForDeviceIfNecessary in Input System 1.20), so wasPressedThisFrame cannot turn true
    // there (runs editmode-red-1 and -2). The Input System's own edit-mode override, InputManager.runPlayerUpdatesInEditMode
    // (the RUN_PLAYER_UPDATES_IN_EDIT_MODE feature), makes InputSystem.Update() run a player update as in Play Mode.
    // The flag is set on the internal manager rather than through an InputSettings copy: feature flags are not
    // serialized, so restoring a settings copy would leave the flag on. InputSystem.settings is not changed; the flag
    // and the previously current keyboard are restored.
    internal sealed class VirtualKeyboard : IDisposable
    {
        readonly object _manager;
        readonly PropertyInfo _playerUpdatesInEditMode;
        readonly bool _previousPlayerUpdatesInEditMode;
        readonly Keyboard _previous = Keyboard.current;
        readonly Keyboard _device;

        internal VirtualKeyboard()
        {
            _manager = typeof(InputSystem).GetField("s_Manager", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            Assert.IsNotNull(_manager, "fixture: InputSystem keeps its manager in s_Manager");
            _playerUpdatesInEditMode = _manager.GetType().GetProperty("runPlayerUpdatesInEditMode", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(_playerUpdatesInEditMode, "fixture: InputManager.runPlayerUpdatesInEditMode exists");
            _previousPlayerUpdatesInEditMode = (bool)_playerUpdatesInEditMode.GetValue(_manager);
            _playerUpdatesInEditMode.SetValue(_manager, true);
            try
            {
                _device = InputSystem.AddDevice<Keyboard>("InventoryTestKeyboard");
                _device.MakeCurrent();
                InputSystem.Update();
                Assert.IsFalse(_device.anyKey.isPressed, "fixture: the virtual keyboard starts with every key up");
            }
            catch
            {
                Dispose(); // a constructor failure never reaches the test's using block
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_device != null && _device.added) InputSystem.RemoveDevice(_device);
                if (_previous != null && _previous.added) _previous.MakeCurrent();
            }
            finally
            {
                _playerUpdatesInEditMode?.SetValue(_manager, _previousPlayerUpdatesInEditMode);
            }
        }

        // One input update in which the key goes down. The device is checked here, so a later failure belongs to
        // the code under test rather than to the virtual keyboard.
        internal void Press(Key key)
        {
            InputSystem.QueueStateEvent(_device, new KeyboardState(key));
            InputSystem.Update();
            bool current = ReferenceEquals(Keyboard.current, _device);
            bool down = _device[key].isPressed;
            bool pressedThisUpdate = _device[key].wasPressedThisFrame;
            Assert.IsTrue(current, "fixture: the virtual keyboard is Keyboard.current");
            Assert.IsTrue(down, $"fixture: {key} is down after the input update");
            Assert.IsTrue(pressedThisUpdate, $"fixture: {key} reads as pressed in this input update");
        }

        // One input update without a new event: a pressed key stays held.
        internal void Hold() => InputSystem.Update();

        internal void Release()
        {
            InputSystem.QueueStateEvent(_device, new KeyboardState());
            InputSystem.Update();
            Assert.IsFalse(_device.anyKey.isPressed, "fixture: every key is up after the input update");
        }
    }
}
