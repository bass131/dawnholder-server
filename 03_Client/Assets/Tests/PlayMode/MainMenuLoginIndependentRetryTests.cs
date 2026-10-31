using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Exercise the shipped scene's pointer-click -> controller -> ConnectionProbe -> failed result, then
    // retry through the view's field submit. The peer is a bound, non-listening ephemeral loopback socket;
    // no GameServer, handshake or database runs. The baseline test covers virtual keyboard delivery.
    public sealed class MainMenuLoginIndependentRetryTests
    {
        MainMenuController _controller;
        Socket _closedPort;
        int _probes;
        float _listenerVolume;
        bool _started;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsTrue(Application.isPlaying, "the actual scene must run in PlayMode");
            yield return MapEntryPlayFixture.Wait(
                () => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
            Assert.IsFalse(NetworkService.Instance.IsConnected, "fixture must not adopt a live connection");
            Assert.IsFalse(SceneManager.GetSceneByName("MainMenu").isLoaded, "fixture: no previous menu");
            _listenerVolume = AudioListener.volume;
            _started = true;
            AudioListener.volume = 0f;
            MainMenuNotice.TryTake(out _);
            _probes = 0;
            Application.logMessageReceived += RecordProbe;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _controller = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true)).Single();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Drain the short probe before destroying its scene owner, including an assertion-failure path.
            if (_controller != null)
                yield return MapEntryPlayFixture.Wait(
                    () => !MapEntryPlayFixture.Field<bool>(_controller, "_probing"), "probe teardown", 10);
            Application.logMessageReceived -= RecordProbe;
            _closedPort?.Dispose();
            _closedPort = null;
            if (_started) AudioListener.volume = _listenerVolume;
            MainMenuNotice.TryTake(out _);
            Scene empty = SceneManager.CreateScene("MainMenuLoginIndependentRetryTests-empty");
            SceneManager.SetActiveScene(empty);
            Scene menu = SceneManager.GetSceneByName("MainMenu");
            if (menu.IsValid() && menu.isLoaded) yield return SceneManager.UnloadSceneAsync(menu);
            _controller = null;
        }

        [UnityTest]
        public IEnumerator PointerStart_FailureUnlocksAndFieldSubmitCanRetryWithoutSaving()
        {
            TMP_InputField account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField");
            TMP_InputField host = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_serverHostInputField");
            Button start = MapEntryPlayFixture.Field<Button>(_controller, "_startButton");
            TMP_Text error = MapEntryPlayFixture.Field<TMP_Text>(_controller, "_errorMessageText");
            Assert.AreEqual("LoginWindow", start.transform.parent.name, "use the view's actual login window");
            bool hadHost = PlayerPrefs.HasKey("ServerHost");
            bool hadAccount = PlayerPrefs.HasKey("AccountName");
            string savedHost = PlayerPrefs.GetString("ServerHost");
            string savedAccount = PlayerPrefs.GetString("AccountName");
            account.text = "alice_01";
            host.text = "127.0.0.1";
            _closedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _closedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int port = ((IPEndPoint)_closedPort.LocalEndPoint).Port;
            Assert.AreNotEqual(7777, port, "fixture must not use the game server port");
            MapEntryPlayFixture.Set(_controller, "_serverPort", port);
            MapEntryPlayFixture.Set(_controller, "_connectTimeoutMs", 500);
            Assert.IsNotNull(EventSystem.current, "fixture: the scene supplies its EventSystem");
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };

            bool delivered = ExecuteEvents.Execute(start.gameObject, click, ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(delivered, "the pointer click must reach the real scene button");
            Assert.IsFalse(start.interactable, "the pending probe locks the button synchronously");
            account.onSubmit.Invoke(account.text);
            ExecuteEvents.Execute(start.gameObject, click, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, _probes, "click and submit during the first probe must not start another");
            yield return MapEntryPlayFixture.Wait(() => start.interactable, "first failed probe", 10);
            Assert.IsNotEmpty(error.text, "the failed probe must appear in the reparented error line");
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "failure must keep the menu");
            string firstError = error.text;

            account.onSubmit.Invoke(account.text);
            Assert.AreEqual(2, _probes, "a later submit must start a fresh probe after failure");
            Assert.IsFalse(start.interactable, "retry must acquire the same pending lock");
            Assert.IsEmpty(error.text, "retry must clear the old error while pending");
            yield return MapEntryPlayFixture.Wait(() => start.interactable, "retry failed probe", 10);
            Assert.IsNotEmpty(error.text, "retry must publish its failure and unlock the button");
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.AreEqual(hadHost, PlayerPrefs.HasKey("ServerHost"));
            Assert.AreEqual(hadAccount, PlayerPrefs.HasKey("AccountName"));
            Assert.AreEqual(savedHost, PlayerPrefs.GetString("ServerHost"), "a failed probe must not save the host");
            Assert.AreEqual(savedAccount, PlayerPrefs.GetString("AccountName"), "a failed probe must not save the account");
            TestContext.Out.WriteLine($"MainMenu pointer click -> 127.0.0.1:{port} non-listening socket -> " +
                $"error '{firstError}'; field submit retry -> probes={_probes}, error '{error.text}', " +
                $"button interactable={start.interactable}, scene={SceneManager.GetActiveScene().name}");
        }

        void RecordProbe(string message, string stack, LogType kind)
        {
            if (message.StartsWith("[MainMenu] Server probe 시도", StringComparison.Ordinal)) _probes++;
        }
    }
}
