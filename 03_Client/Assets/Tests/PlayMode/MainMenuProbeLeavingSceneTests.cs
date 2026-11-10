using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 5: after a successful probe the start path stays locked until the
    // screen leaves, and a probe result that outlives its screen does nothing. Everything stays on loopback: the
    // listener answers the probe and EntryWirePeer stands in for the CharacterSelect connection the fade leads to.
    public sealed class MainMenuProbeLeavingSceneTests
    {
        const string Account = "leave_test"; // An accepted AccountNameRule example, not a product-derived value.
        const float SecondProbeObservationSeconds = 1.0f;
        static readonly string[] s_scenes = { "MainMenu", "CharacterSelect" };
        static readonly string[] s_prefsKeys = { "ServerHost", AccountLoadout.PrefsKey };

        readonly Dictionary<string, (bool Exists, string Value)> _prefs = new();
        MainMenuController _controller;
        NetworkService _network;
        SceneTransition _loader;
        Keyboard _keyboard;
        EntryWirePeer _peer;
        TcpListener _listener;
        Socket _closedPort;
        int _accepted;
        int _oldPort;
        string _oldAccount;
        float _oldVolume, _oldScale;
        bool _oldBackground;
        bool _captured;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (string name in s_scenes)
            {
                Scene scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            _captured = false;
            _accepted = 0;
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
            _network = NetworkService.Instance;
            _loader = SceneTransition.Instance;
            Assert.IsFalse(_network.IsConnected, "fixture cannot adopt another connection");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _oldAccount = AccountLoadout.SessionAccountName;
            _oldVolume = AudioListener.volume;
            _oldScale = Time.timeScale;
            _oldBackground = Application.runInBackground;
            foreach (string key in s_prefsKeys)
                _prefs[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key));
            _captured = true;

            // Set before the scene reads them: a success writes ServerHost and CharacterSelect then connects with it.
            PlayerPrefs.SetString("ServerHost", "127.0.0.1");
            AccountLoadout.SessionAccountName = null;
            AudioListener.volume = 0f;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("ProbeLeavingKeyboard");
            _keyboard.MakeCurrent();
            _peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(_network, "_serverPort", _peer.Port);
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _controller = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(_controller, "the shipped MainMenu scene owns the controller");
            MapEntryPlayFixture.Set(_controller, "_serverPort", ((IPEndPoint)_listener.LocalEndpoint).Port);
            MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField").text = Account;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                if (_controller != null)
                    yield return MapEntryPlayFixture.Wait(() => !MapEntryPlayFixture.Field<bool>(_controller, "_probing"),
                        "owned probe drains before owner unload", 10);
                if (_loader != null) yield return MapEntryPlayFixture.Wait(() => !_loader.IsTransitioning, "owned scene operation", 20);
                if (_captured) _network.Disconnect();
                Scene empty = SceneManager.GetSceneByName("ProbeLeaving-empty");
                if (!empty.IsValid()) empty = SceneManager.CreateScene("ProbeLeaving-empty");
                SceneManager.SetActiveScene(empty);
                foreach (string name in s_scenes)
                {
                    Scene scene = SceneManager.GetSceneByName(name);
                    if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                }
            }
            finally
            {
                _peer?.Dispose();
                _listener?.Stop();
                _closedPort?.Dispose();
                if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
                if (_captured)
                {
                    MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
                    AccountLoadout.SessionAccountName = _oldAccount;
                    AudioListener.volume = _oldVolume;
                    Time.timeScale = _oldScale;
                    Application.runInBackground = _oldBackground;
                    foreach (KeyValuePair<string, (bool Exists, string Value)> pair in _prefs)
                    {
                        if (pair.Value.Exists) PlayerPrefs.SetString(pair.Key, pair.Value.Value);
                        else PlayerPrefs.DeleteKey(pair.Key);
                    }
                    PlayerPrefs.Save();
                }
                _prefs.Clear();
            }
        }

        void PumpAccepted()
        {
            while (_listener.Pending())
            {
                _listener.AcceptTcpClient().Close();
                _accepted++;
            }
        }

        static IEnumerator Realtime(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator StartAgainDuringTheFade_DoesNotProbeAgain()
        {
            _controller.OnStartClicked();
            yield return MapEntryPlayFixture.Wait(() => _loader.IsTransitioning, "the first probe succeeds and the fade starts", 10);
            _controller.OnStartClicked();
            yield return Realtime(SecondProbeObservationSeconds);
            PumpAccepted();
            int probesAccepted = _accepted;
            Assert.AreEqual(1, probesAccepted, "item 5: a start during the fade makes no second probe");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ProbeResultAfterTheScreenIsGone_RequestsNoScene()
        {
            _controller.OnStartClicked();
            // The result reaches the main thread through the dispatcher, always after this synchronous removal.
            Object.DestroyImmediate(_controller);
            yield return Realtime(SecondProbeObservationSeconds);
            PumpAccepted();
            bool probeReachedListener = _accepted == 1;
            bool sceneRequested = _loader.IsTransitioning || SceneManager.GetSceneByName("CharacterSelect").isLoaded;
            Assert.IsTrue(probeReachedListener, "fixture: the probe completed while its screen was gone");
            Assert.IsFalse(sceneRequested, "item 5: a result that outlives its screen requests no scene");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FailedProbe_AllowsStartingAgain()
        {
            _closedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _closedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int workingPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            MapEntryPlayFixture.Set(_controller, "_serverPort", ((IPEndPoint)_closedPort.LocalEndPoint).Port);
            TMP_Text error = MapEntryPlayFixture.Field<TMP_Text>(_controller, "_errorMessageText");

            _controller.OnStartClicked();
            yield return MapEntryPlayFixture.Wait(() => !string.IsNullOrEmpty(error.text) &&
                !MapEntryPlayFixture.Field<bool>(_controller, "_probing"), "the refused probe reports its error", 10);
            Assert.IsFalse(_loader.IsTransitioning, "a failed probe stays on the screen");

            MapEntryPlayFixture.Set(_controller, "_serverPort", workingPort);
            _controller.OnStartClicked();
            yield return MapEntryPlayFixture.Wait(() =>
            {
                PumpAccepted();
                return _accepted == 1 && _loader.IsTransitioning;
            }, "item 5: after a failed probe the start path works again", 10);
        }
    }
}
