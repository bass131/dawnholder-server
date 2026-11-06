using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Demo;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Real scene Start/Update with an owned TCP endpoint that deliberately withholds the handshake.
    // No GameServer/DB and no existing fixture changes. A slow menu must remain distinguishable from Legacy.
    public sealed class DemoAutopilotVerifyConnectionTests
    {
        TcpListener _listener;
        TcpClient _accepted;
        NetworkService _network;
        Scene _scene;
        int _oldPort;
        string _oldAccount;
        float _oldScale;
        bool _captured;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_captured && _network != null) _network.Disconnect();
            _accepted?.Dispose();
            _listener?.Stop();
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "owned scene unload");
            }
            if (_captured)
            {
                if (_network != null) MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
                AccountLoadout.SessionAccountName = _oldAccount;
                Time.timeScale = _oldScale;
            }
        }

        [UnityTest]
        public IEnumerator OpenMenuBeforeHandshake_ExposesConnectingInsteadOfLegacyNull()
        {
            Assert.IsFalse(SceneManager.GetSceneByName("CharacterSelect").isLoaded);
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null, "NetworkService");
            _network = NetworkService.Instance;
            Assert.IsFalse(_network.IsConnected, "do not adopt another connection");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _oldAccount = AccountLoadout.SessionAccountName;
            _oldScale = Time.timeScale;
            _captured = true;
            Time.timeScale = 1f;
            AccountLoadout.SessionAccountName = "verify_slow";
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            MapEntryPlayFixture.Set(_network, "_serverPort", ((IPEndPoint)_listener.LocalEndpoint).Port);
            _network.ConnectCharacterMenu("127.0.0.1");
            yield return MapEntryPlayFixture.Wait(() => _listener.Pending(), "owned TCP connection arrives");
            _accepted = _listener.AcceptTcpClient();
            yield return MapEntryPlayFixture.Wait(() => _network.IsConnected, "TCP connected before scene Start");
            AsyncOperation load = SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "CharacterSelect load");
            _scene = SceneManager.GetSceneByName("CharacterSelect");
            CharacterSelectController controller = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CharacterSelectController>(true)).Single();
            yield return MapEntryPlayFixture.Wait(() => controller.transform.Find("CharacterMenu/Panel") != null,
                "actual controller creates menu");
            yield return new WaitForSecondsRealtime(1.2f); // Longer than EnterGame's one-second scene settle.
            Assert.IsTrue(_network.IsConnected, "TCP connected; handshake is deliberately pending");
            Assert.IsFalse(UnityClientSession.Instance.HandshakeOk, "fixture sent no handshake response");
            Assert.IsNull(_network.CharacterMenu, "no accepted login or menu reply yet");
            var projection = typeof(CharacterSelectController).GetProperty("MenuState",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            // Call the production step with the controller's real projection, just as EnterGame does.
            // Reflection is needed because PlayMode is not a friend assembly of the internal step.
            System.Type stepType = typeof(DemoAutopilot).Assembly.GetType("Dawnholder.Client.Demo.DemoCharacterMenuStep");
            object step = System.Activator.CreateInstance(stepType, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { DemoRole.Knight, "VerifySlow" }, null);
            object outcome = stepType.GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(step, new object[] { projection, controller.transform.Find("CharacterMenu/Panel"), 0f });
            TestContext.Out.WriteLine($"panel={controller.transform.Find("CharacterMenu/Panel") != null}; " +
                $"connected={_network.IsConnected}; handshake={UnityClientSession.Instance.HandshakeOk}; " +
                $"projectionNull={projection == null}; outcome={outcome}; receivedBytes={_accepted.Available}");
            Assert.IsNotNull(projection,
                "B2/B3: an open connecting menu must not be exposed as null, which Advance interprets as Legacy");
            Assert.AreEqual("Connecting", projection.GetType().GetProperty("CurrentPhase",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(projection).ToString());
        }
    }
}
