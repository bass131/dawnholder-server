using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests
{
    // Goal T10: a session double that records what ClientConnectionLifetime sends. The existing
    // ControlledClientSession records class selects only, and the implementer rewrites it when the session
    // interface grows, so the login tests keep their own double.
    internal sealed class LoginRecordingSession : IClientConnectionSession
    {
        internal readonly List<string> Logins = new();
        internal readonly List<byte> CharacterSelects = new();
        internal int Disconnects;
        Socket _socket;

        public event Action HandshakeSucceeded;

        public event Action<LoginResult> LoginResultReceived;

        // Never raised: these tests end connections only through the owner.
        public event Action Closed
        {
            add { }
            remove { }
        }

        public bool IsClosed { get; private set; }

        public void Activate(Socket socket) => _socket = socket;

        public void Publish()
        {
        }

        public void SendLogin(string accountName) => Logins.Add(accountName);

        public void SendCharacterSelect(byte characterClass) => CharacterSelects.Add(characterClass);

        public void Disconnect()
        {
            Disconnects++;
            IsClosed = true;
            _socket?.Dispose();
        }

        public void Cleanup() => _socket?.Dispose();

        internal void Handshake() => HandshakeSucceeded?.Invoke();

        // As LoginResultHandler does on the main thread after checking the byte is 0..2.
        internal void DeliverLoginResult(LoginResult result) => LoginResultReceived?.Invoke(result);
    }

    // One connection owner wired like ConnectionHarness: completions post to a manual main-thread queue.
    internal sealed class LoginConnectionHarness : IDisposable
    {
        internal const string AccountName = "alice_01";
        internal static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 12345);
        internal readonly ManualConnectionQueue Queue = new();
        internal readonly List<LoginRecordingSession> Sessions = new();
        internal readonly List<LoginResult> Rejections = new();
        internal readonly ClientConnectionLifetime Owner;
        readonly Queue<ControlledAttempt> _attempts = new();

        internal LoginConnectionHarness()
        {
            Owner = new ClientConnectionLifetime(_ => _attempts.Dequeue(), Queue.Post, _ =>
            {
                var session = new LoginRecordingSession();
                Sessions.Add(session);
                return session;
            }, () => { });
            Owner.LoginRejected += Rejections.Add;
        }

        // An activated session whose handshake has not succeeded yet.
        internal LoginRecordingSession Connect(byte selectedClass)
        {
            var attempt = new ControlledAttempt();
            attempt.Complete(ConnectionStatus.Success);
            _attempts.Enqueue(attempt);
            Assert.IsTrue(Owner.Connect(Endpoint, selectedClass, AccountName), "fixture: the owner refused to connect");
            Queue.WaitThenDrain();
            return Sessions[Sessions.Count - 1];
        }

        public void Dispose()
        {
            Owner.Dispose();
            Queue.Drain();
            foreach (ControlledAttempt attempt in _attempts) attempt.Dispose();
        }
    }

    // Goal T10: the real MainMenu scene asset, opened next to the runner's scene and closed without saving.
    internal sealed class MainMenuSceneFixture : IDisposable
    {
        internal const string ScenePath = "Assets/Scenes/00.Menu/MainMenu.unity";
        readonly Scene _previous;
        readonly bool _replacedRunnerScene;

        internal MainMenuSceneFixture()
        {
            _previous = SceneManager.GetActiveScene();
            try
            {
                Scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                // As GameplayTestScene: Unity may refuse an additive scene next to an untitled one.
                TestContext.Out.WriteLine($"fixture: additive open refused ({error.Message}); opening the menu alone");
                Scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                _replacedRunnerScene = true;
            }
            try
            {
                Controller = FindController(Scene);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Scene Scene { get; }

        internal MainMenuController Controller { get; }

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

        static MainMenuController FindController(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<MainMenuController>(true);
                if (controller != null) return controller;
            }
            Assert.Fail($"fixture: {ScenePath} has no MainMenuController");
            return null;
        }
    }
}
