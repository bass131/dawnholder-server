using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 6: a refused first connection ends the menu's Connecting state. The
    // shipped scene's Start makes the connection itself, so ServerHost and the service port are loopback before it runs.
    public sealed class CharacterMenuFirstConnectFailureSceneTests
    {
        const string ServerHostKey = "ServerHost";
        const float FailureLimitSeconds = 10f;

        NetworkService _network;
        EntryWirePeer _peer;
        Scene _scene;
        int _oldPort;
        string _oldAccount;
        float _oldVolume, _oldScale;
        bool _hadHost;
        string _oldHost;
        bool _captured;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsFalse(SceneManager.GetSceneByName("CharacterSelect").isLoaded, "fixture owns only the scene it loads");
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null, "persistent NetworkService");
            _network = NetworkService.Instance;
            Assert.IsFalse(_network.IsConnected, "do not adopt another connection");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _oldAccount = AccountLoadout.SessionAccountName;
            _oldVolume = AudioListener.volume;
            _oldScale = Time.timeScale;
            _hadHost = PlayerPrefs.HasKey(ServerHostKey);
            _oldHost = PlayerPrefs.GetString(ServerHostKey);
            _captured = true;
            PlayerPrefs.SetString(ServerHostKey, "127.0.0.1");
            AccountLoadout.SessionAccountName = "first_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            AudioListener.volume = 0f;
            Time.timeScale = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                _peer?.Dispose();
                if (_captured) _network.Disconnect();
                if (_peer != null) yield return MapEntryPlayFixture.Wait(() => _peer.Completion.IsCompleted, "menu peer shutdown", 3);
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                    if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "owned CharacterSelect unload", 10);
                }
            }
            finally
            {
                if (_captured)
                {
                    MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
                    AccountLoadout.SessionAccountName = _oldAccount;
                    AudioListener.volume = _oldVolume;
                    Time.timeScale = _oldScale;
                    if (_hadHost) PlayerPrefs.SetString(ServerHostKey, _oldHost);
                    else PlayerPrefs.DeleteKey(ServerHostKey);
                    PlayerPrefs.Save();
                }
            }
        }

        IEnumerator LoadCharacterSelect(Action<CharacterSelectController> found)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "CharacterSelect load", 20);
            _scene = SceneManager.GetSceneByName("CharacterSelect");
            CharacterSelectController controller = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CharacterSelectController>(true)).Single();
            yield return MapEntryPlayFixture.Wait(() => MenuPhase(controller) != null, "the scene's Start opened the menu", 5);
            found(controller);
        }

        // PlayMode is not a friend assembly of the internal menu projection (as in DemoAutopilotVerifyConnectionTests).
        static string MenuPhase(CharacterSelectController controller)
        {
            object state = typeof(CharacterSelectController).GetProperty("MenuState",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            return state?.GetType().GetProperty("CurrentPhase",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(state).ToString();
        }

        static int ClosedLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RefusedFirstConnection_LeavesConnectingWithinTheLimit()
        {
            MapEntryPlayFixture.Set(_network, "_serverPort", ClosedLoopbackPort());
            CharacterSelectController controller = null;
            yield return LoadCharacterSelect(item => controller = item);

            float deadline = Time.realtimeSinceStartup + FailureLimitSeconds;
            while (MenuPhase(controller) == "Connecting" && Time.realtimeSinceStartup < deadline) yield return null;
            string phaseAtLimit = MenuPhase(controller);
            bool connected = _network.IsConnected;
            Assert.IsFalse(connected, "fixture: nothing accepted the closed port");
            Assert.AreNotEqual("Connecting", phaseAtLimit, $"item 6: a refused first connection leaves Connecting within {FailureLimitSeconds} s");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator AcceptedFirstConnection_MovesPastConnecting()
        {
            _peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(_network, "_serverPort", _peer.Port);
            CharacterSelectController controller = null;
            yield return LoadCharacterSelect(item => controller = item);

            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_CharacterListRequest) > 0, "control: the list request reaches the stand-in", 10);
            yield return MapEntryPlayFixture.Wait(() => MenuPhase(controller) != "Connecting", "control: an accepted connection leaves Connecting", 5);
            Assert.IsEmpty(_peer.Errors, "loopback stand-in delivered valid frames");
        }
    }
}
