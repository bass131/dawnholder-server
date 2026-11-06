using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Demo;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // T3-T6 / B1-B6: shipped MainMenu.Start, virtual keys, actual loopback probe and captured wire frames.
    // The fixture owns ephemeral endpoints only. It restores preferences, input settings and persistent network state.
    public sealed class MainMenuLoginIdPwSceneTests
    {
        const string Account = "idpw_test"; // B5: accepted AccountNameRule example, not a product-derived expected value.
        const string PasswordSentinel = "pw-sentinel-Z7k4m9"; // B3: deliberately absent from account/host/fixture logs.
        readonly List<string> _logs = new();
        readonly List<UnityEngine.Object> _owned = new();
        readonly Dictionary<string, (bool Exists, string Value)> _prefs = new();
        MainMenuController _controller;
        NetworkService _network;
        Keyboard _keyboard;
        InputSettings _oldInput;
        InputSettings _oldInputCopy;
        InputSettings _runtimeInput;
        bool _oldBackground;
        float _oldVolume;
        string _oldSessionAccount;
        int _oldPort;
        bool _captured;
        Socket _closedPort;
        TcpListener _probeListener;
        TcpClient _probeClient;
        EntryWirePeer _peer;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsTrue(Application.isPlaying);
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null && SceneTransition.Instance != null,
                "persistent services");
            _network = NetworkService.Instance;
            Assert.IsFalse(_network.IsConnected, "fixture cannot adopt another connection");
            Assert.IsFalse(SceneManager.GetSceneByName("MainMenu").isLoaded, "fixture: previous menu was unloaded");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _oldSessionAccount = AccountLoadout.SessionAccountName;
            _oldVolume = AudioListener.volume;
            _oldBackground = Application.runInBackground;
            _oldInput = InputSystem.settings;
            _oldInputCopy = UnityEngine.Object.Instantiate(_oldInput);
            _oldInputCopy.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInput = UnityEngine.Object.Instantiate(_oldInput);
            _runtimeInput.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _runtimeInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _runtimeInput;
            foreach (string key in new[] { "ServerHost", AccountLoadout.PrefsKey })
                _prefs[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key));
            _captured = true;
            PlayerPrefs.SetString("ServerHost", "127.0.0.1");
            AccountLoadout.SessionAccountName = null;
            AudioListener.volume = 0f;
            Application.runInBackground = true;
            MainMenuNotice.TryTake(out _);
            _logs.Clear();
            Application.logMessageReceived += Record;
            _keyboard = InputSystem.AddDevice<Keyboard>("IdPwTestKeyboard");
            _keyboard.MakeCurrent();
            yield return LoadMenu();
        }

        IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _controller = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true)).Single();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_controller != null)
                yield return MapEntryPlayFixture.Wait(() => !MapEntryPlayFixture.Field<bool>(_controller, "_probing"),
                    "owned probe drains before owner unload", 10);
            Application.logMessageReceived -= Record;
            if (_captured) _network.Disconnect();
            _peer?.Dispose();
            _probeClient?.Dispose();
            _probeListener?.Stop();
            _closedPort?.Dispose();
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_captured)
            {
                InputSystem.settings = _oldInput != null ? _oldInput : _oldInputCopy;
                AudioListener.volume = _oldVolume;
                Application.runInBackground = _oldBackground;
                MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
                AccountLoadout.SessionAccountName = _oldSessionAccount;
                foreach (var pair in _prefs)
                {
                    if (pair.Value.Exists) PlayerPrefs.SetString(pair.Key, pair.Value.Value);
                    else PlayerPrefs.DeleteKey(pair.Key);
                }
                PlayerPrefs.Save();
            }
            foreach (InputSettings settings in new[] { _runtimeInput, _oldInputCopy })
                if (settings != null && settings != InputSystem.settings) UnityEngine.Object.Destroy(settings);
            foreach (UnityEngine.Object owned in _owned)
                if (owned != null) UnityEngine.Object.Destroy(owned);
            _owned.Clear();
            _prefs.Clear();
            MainMenuNotice.TryTake(out _);
            Scene empty = SceneManager.GetSceneByName("IdPwTests-empty");
            if (!empty.IsValid()) empty = SceneManager.CreateScene("IdPwTests-empty");
            SceneManager.SetActiveScene(empty);
            foreach (string name in new[] { "MainMenu", "CharacterSelect" })
            {
                Scene scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            _captured = false;
            _controller = null;
            _peer = null;
            _probeListener = null;
            _probeClient = null;
            _closedPort = null;
        }

        void Record(string message, string stack, LogType kind) => _logs.Add(message);
        int Probes => _logs.Count(line => line.StartsWith("[MainMenu] Server probe 시도", StringComparison.Ordinal));
        T Field<T>(string name) => MapEntryPlayFixture.Field<T>(_controller, name);
        TMP_InputField Id => Field<TMP_InputField>("_accountNameInputField");
        TMP_InputField Host => Field<TMP_InputField>("_serverHostInputField");
        Button StartButton => Field<Button>("_startButton");
        TMP_Text Error => Field<TMP_Text>("_errorMessageText");
        Transform Window => Id.transform.parent;
        TMP_InputField Password()
        {
            Transform child = Window.Find("PasswordInput");
            Assert.IsNotNull(child, "B1/B3: PasswordInput missing from LoginWindow");
            var field = child.GetComponent<TMP_InputField>();
            Assert.IsNotNull(field, "B3: PasswordInput requires TMP_InputField");
            return field;
        }

        [UnityTest] public IEnumerator T3_LayoutAt800x450() => Layout(800, 450);
        [UnityTest] public IEnumerator T3_LayoutAt1280x720() => Layout(1280, 720);
        [UnityTest] public IEnumerator T3_LayoutAt1920x1080() => Layout(1920, 1080);

        IEnumerator Layout(int width, int height)
        {
            Assert.AreEqual("LoginWindow", Window.name, "B1: scene fields moved into LoginWindow");
            var cameraObject = new GameObject("IdPw capture camera (fixture)");
            _owned.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            camera.allowHDR = camera.allowMSAA = false;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _owned.Add(target);
            target.Create();
            camera.targetTexture = target;
            Canvas canvas = Id.GetComponentInParent<Canvas>().rootCanvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(new Vector2(width, height), canvas.renderingDisplaySize, "fixture: scaler sees target size");
            var problems = new List<string>();
            TMP_InputField pw = Window.Find("PasswordInput")?.GetComponent<TMP_InputField>();
            Id.text = "";
            if (pw != null) pw.text = "";
            yield return null;
            Save(camera, target, $"login-idpw-empty-{width}x{height}.png");
            if (pw == null) problems.Add("B1/B3: PasswordInput missing");
            else
            {
                if (pw.contentType != TMP_InputField.ContentType.Password) problems.Add("B3: password contentType");
                if (pw.asteriskChar != '*') problems.Add("B3: password asteriskChar");
                if (pw.characterLimit != 32) problems.Add("B3: password characterLimit must be 32");
                if (pw.placeholder is TMP_Text placeholder && placeholder.text != "") problems.Add("B3: placeholder not empty");
                if (pw.GetComponent<Image>().sprite != Id.GetComponent<Image>().sprite) problems.Add("B1: field backgrounds differ");
            }
            foreach (string name in new[] { "Header", "Title" })
                if (Window.GetComponentsInChildren<Transform>(true).Any(node => node.name == name)) problems.Add($"B1: {name} remains");
            Transform hostLabel = Window.Find("HostLabel");
            if (hostLabel != null && hostLabel.gameObject.activeInHierarchy) problems.Add("B2: HostLabel is visible");
            if (Host.gameObject.activeInHierarchy) problems.Add("B2: ServerHostInput is visible");
            if (Host.text != "127.0.0.1") problems.Add("B2: saved host lost");
            var named = new Dictionary<string, RectTransform>();
            foreach (string name in new[] { "AccountNameInput", "PasswordInput", "StartButton", "QuitButton", "ErrorMessageText" })
            {
                Transform node = Window.Find(name);
                if (node == null) { problems.Add($"B1: missing {name}"); continue; }
                if (!node.gameObject.activeInHierarchy) problems.Add($"B1: hidden {name}");
                named[name] = (RectTransform)node;
            }
            var words = new[] { ("AccountLabel", "ID"), ("PasswordLabel", "PW"),
                ("StartButton/Label", "START"), ("QuitButton/Label", "QUIT") };
            foreach (var word in words)
            {
                TMP_Text text = Window.Find(word.Item1)?.GetComponent<TMP_Text>();
                if (text == null || !text.isActiveAndEnabled) { problems.Add($"B1: missing visible {word.Item1}"); continue; }
                if (text.text != word.Item2) problems.Add($"B1: {word.Item1} text={text.text}, expected {word.Item2}");
                text.ForceMeshUpdate();
                if (text.isTextOverflowing || text.isTextTruncated) problems.Add($"B1: {word.Item1} text overflows/truncates");
            }
            foreach (var pair in new[] { ("AccountLabel", "AccountNameInput"), ("PasswordLabel", "PasswordInput") })
            {
                RectTransform label = Window.Find(pair.Item1) as RectTransform;
                if (label == null || !named.TryGetValue(pair.Item2, out RectTransform input)) continue;
                Rect fieldRect = Area(camera, input);
                float labelY = Area(camera, label).center.y;
                if (labelY < fieldRect.yMin || labelY > fieldRect.yMax) problems.Add($"B1: {pair.Item1} not on field row");
            }
            var areas = named.ToDictionary(pair => pair.Key, pair => Area(camera, pair.Value));
            string[] order = { "AccountNameInput", "PasswordInput", "StartButton", "QuitButton" };
            for (int i = 1; i < order.Length; i++)
                if (areas.ContainsKey(order[i - 1]) && areas.ContainsKey(order[i]) &&
                    areas[order[i - 1]].center.y <= areas[order[i]].center.y) problems.Add($"B1: order {order[i - 1]} -> {order[i]}");
            var entries = areas.ToArray();
            Rect windowRect = Area(camera, (RectTransform)Window);
            for (int i = 0; i < entries.Length; i++)
            {
                Rect rect = entries[i].Value;
                if (rect.xMin < windowRect.xMin || rect.xMax > windowRect.xMax ||
                    rect.yMin < windowRect.yMin || rect.yMax > windowRect.yMax) problems.Add($"B1: {entries[i].Key} outside window");
                for (int j = i + 1; j < entries.Length; j++)
                    if (rect.Overlaps(entries[j].Value)) problems.Add($"B1: overlap {entries[i].Key}/{entries[j].Key}");
            }
            RectTransform title = canvas.transform.Find("TITLE") as RectTransform;
            Assert.IsNotNull(title, "fixture: scene TITLE banner");
            if (windowRect.Overlaps(Area(camera, title))) problems.Add("B1: LoginWindow overlaps TITLE");
            CheckSkin(StartButton, "Button_Accept_Normal", "fdaa805e70f5533488dae81f7234282a", problems);
            Button quit = Window.Find("QuitButton")?.GetComponent<Button>();
            if (quit == null) problems.Add("B4: QuitButton missing");
            else CheckSkin(quit, "Button_Default_Normal", "218e99a5632015261381128b2909788b", problems);
            Id.text = Account;
            if (pw != null) pw.text = PasswordSentinel;
            yield return null;
            Save(camera, target, $"login-idpw-filled-{width}x{height}.png");
            TestContext.Out.WriteLine($"resolution={width}x{height}; " + string.Join("; ", areas.Select(pair => $"{pair.Key}={pair.Value}")));
            camera.targetTexture = null;
            target.Release();
            Assert.IsEmpty(problems, "B1-B3: fixed ID/PW/START/QUIT design");
        }

        [UnityTest]
        public IEnumerator T3_PasswordStartsEmpty_AndHostUsesSavedOrDefaultValue()
        {
            Assert.IsEmpty(Password().text, "B3: initial PW is empty before fixture writes it");
            PlayerPrefs.SetString("ServerHost", "192.0.2.27");
            yield return LoadMenu();
            Assert.AreEqual("192.0.2.27", Host.text, "B2: saved host");
            PlayerPrefs.DeleteKey("ServerHost");
            yield return LoadMenu();
            Assert.AreEqual("127.0.0.1", Host.text, "B2: default host");
            Assert.IsFalse(Host.gameObject.activeInHierarchy, "B2: host remains hidden");
        }

        [UnityTest]
        public IEnumerator T4_TabFromId_SelectsPassword()
        {
            yield return Select(Id);
            yield return Press(Key.Tab);
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected, "B4: selection survives Tab");
            Assert.AreEqual("PasswordInput", selected.name, "B4: Tab must select PW");
        }

        [UnityTest] public IEnumerator T4_EnterInId_StartsOneProbe() => Enter(false);
        [UnityTest] public IEnumerator T4_EnterInPassword_StartsOneProbe() => Enter(true);
        IEnumerator Enter(bool password)
        {
            Id.text = Account;
            BindClosedProbe();
            yield return Select(password ? Password() : Id);
            _logs.Clear();
            yield return Press(Key.Enter);
            Assert.AreEqual(1, Probes, "B4: actual Enter input starts one probe");
        }

        [UnityTest]
        public IEnumerator T4_StartClickStartsOneProbe_AndQuitKeepsItsControllerCall()
        {
            Id.text = Account;
            BindClosedProbe();
            _logs.Clear();
            StartButton.onClick.Invoke();
            Assert.AreEqual(1, Probes, "B4: START click invokes start once");
            StartButton.onClick.Invoke();
            Assert.AreEqual(1, Probes, "B5: pending probe rejects duplicate click");
            Button quit = Window.Find("QuitButton").GetComponent<Button>();
            Assert.AreEqual(1, quit.onClick.GetPersistentEventCount(), "B4: one quit binding");
            Assert.AreSame(_controller, quit.onClick.GetPersistentTarget(0));
            Assert.AreEqual("OnQuitClicked", quit.onClick.GetPersistentMethodName(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator T5_EmptyPasswordProbes_BadAccountKeepsTheNotice()
        {
            TMP_InputField pw = Password();
            pw.text = "";
            Id.text = "bad-name";
            BindClosedProbe();
            _logs.Clear();
            StartButton.onClick.Invoke();
            Assert.AreEqual(MainMenuNotice.InvalidAccountName, Error.text, "B5: original invalid-name notice");
            Assert.AreEqual(0, Probes, "B5: invalid account does not probe");
            Id.text = Account;
            StartButton.onClick.Invoke();
            Assert.AreEqual(1, Probes, "B3: empty password still probes");
            Assert.IsEmpty(Error.text, "B3: empty PW shows no error before result");
            yield break;
        }

        [UnityTest]
        public IEnumerator T5_PasswordIsAbsentFromPrefsLogsAndWireThroughCharacterSelect()
        {
            Password().text = PasswordSentinel;
            Id.text = Account;
            Host.text = "127.0.0.1";
            _probeListener = new TcpListener(IPAddress.Loopback, 0);
            _probeListener.Start();
            MapEntryPlayFixture.Set(_controller, "_serverPort", ((IPEndPoint)_probeListener.LocalEndpoint).Port);
            _peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(_network, "_serverPort", _peer.Port);
            _logs.Clear();
            StartButton.onClick.Invoke();
            yield return MapEntryPlayFixture.Wait(() => _probeListener.Pending(), "owned probe connection");
            _probeClient = _probeListener.AcceptTcpClient();
            yield return MapEntryPlayFixture.Wait(() => SceneManager.GetActiveScene().name == "CharacterSelect", "probe success scene", 15);
            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_Login) > 0, "first login wire frame", 15);
            byte[][] frames = MapEntryPlayFixture.Field<ConcurrentQueue<byte[]>>(_peer, "_received").ToArray();
            Assert.IsNotEmpty(frames, "B3: wire must be observed, not an empty collection");
            foreach (byte[] frame in frames)
            foreach (Encoding encoding in new[] { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode })
                Assert.IsFalse(Contains(frame, encoding.GetBytes(PasswordSentinel)), $"B3: password leaked in {encoding.WebName} wire bytes");
            foreach (string key in new[] { "ServerHost", AccountLoadout.PrefsKey })
                StringAssert.DoesNotContain(PasswordSentinel, PlayerPrefs.GetString(key), $"B3: password leaked into {key}");
            Assert.AreEqual("127.0.0.1", PlayerPrefs.GetString("ServerHost"), "B5: successful host saved");
            Assert.AreEqual(Account, PlayerPrefs.GetString(AccountLoadout.PrefsKey), "B5: selected account saved");
            StringAssert.DoesNotContain(PasswordSentinel, string.Join("\n", _logs), "B3: password leaked into Unity logs");
            Assert.IsEmpty(_peer.Errors, "fixture: wire peer errors");
            TestContext.Out.WriteLine($"wire frames observed={frames.Length}; bytes={frames.Sum(frame => frame.Length)}; prefs keys=ServerHost,AccountName; logs={_logs.Count}");
        }

        [UnityTest]
        public IEnumerator T6_AutopilotLogin_FillsEightCharacterPasswordAndOptionAccount()
        {
            BindClosedProbe();
            var go = new GameObject("IdPw demo step fixture");
            go.SetActive(false); // Run only EnterGame; Start/Run must not install a second keyboard or start the full demo.
            _owned.Add(go);
            DemoAutopilot pilot = go.AddComponent<DemoAutopilot>();
            object options = typeof(DemoLaunchOptions).GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { "Dawnholder.exe", "-demo-role", "knight", "-demo-account", Account } });
            MapEntryPlayFixture.Set(pilot, "_options", options);
            IEnumerator step = (IEnumerator)typeof(DemoAutopilot).GetMethod("EnterGame", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pilot, null);
            _logs.Clear();
            bool loginLogged = false;
            for (int i = 0; i < 6 && step.MoveNext(); i++)
            {
                loginLogged = _logs.Any(line => Regex.IsMatch(line, @"\[Demo\] step \d+ login "));
                if (loginLogged) break;
                yield return step.Current;
            }
            (step as IDisposable)?.Dispose();
            Assert.IsTrue(loginLogged, "B6 test half: production EnterGame logs login");
            Assert.AreEqual(Account, Id.text, "B6: option account reaches ID field");
            Assert.AreEqual(8, Password().text.Length, "B6: demo fills fixed eight-character PW");
        }

        void BindClosedProbe()
        {
            _closedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _closedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            MapEntryPlayFixture.Set(_controller, "_serverPort", ((IPEndPoint)_closedPort.LocalEndPoint).Port);
            Host.text = "127.0.0.1";
        }

        IEnumerator Select(TMP_InputField field)
        {
            EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
            yield return null;
            yield return null;
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        static Rect Area(Camera camera, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = camera.WorldToScreenPoint(corners[0]);
            Vector2 b = camera.WorldToScreenPoint(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        static void CheckSkin(Button button, string name, string guid, List<string> problems)
        {
            Sprite sprite = (button.targetGraphic as Image)?.sprite;
            if (sprite == null || sprite.name != name) { problems.Add($"B1: {button.name} skin is not {name}"); return; }
#if UNITY_EDITOR
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string actual, out long _) || actual != guid)
                problems.Add($"B1: {button.name} sprite is not the bright asset");
#endif
        }

        void Save(Camera camera, RenderTexture target, string filename)
        {
            string directory = Environment.GetEnvironmentVariable("DAWNHOLDER_LOGIN_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) return;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, false);
            _owned.Add(texture);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                texture.Apply(false);
            }
            finally { RenderTexture.active = previous; }
            Directory.CreateDirectory(directory);
            using var output = new FileStream(Path.Combine(directory, filename), FileMode.CreateNew);
            byte[] png = texture.EncodeToPNG();
            output.Write(png, 0, png.Length);
        }

        static bool Contains(byte[] source, byte[] needle)
        {
            for (int i = 0; i <= source.Length - needle.Length; i++)
            {
                bool same = true;
                for (int j = 0; j < needle.Length && same; j++) same = source[i + j] == needle[j];
                if (same) return true;
            }
            return false;
        }
    }
}
