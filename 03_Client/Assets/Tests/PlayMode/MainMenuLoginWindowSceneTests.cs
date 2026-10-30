using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
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
    // The MainMenu login screen as the shipped scene starts in PlayMode. Requirement source: goal
    // 2026-10-30-login-screen-guild-window 「만들 것」 PR-A and completion rows 1·2; the outside contract is the lead's
    // 「설계 고정」 in pr-a-red-contract-v2.md, the resolution fixture and capture variable the lead's answer
    // msg_cfe6f4d27fc7. The scene loads as LoginRejectionMenuTests leaves it (MainMenu alone and active) and is left
    // for an empty scene afterwards. Keys go through a virtual Input System keyboard as MapEntryPlayFixture drives one;
    // the EventSystem's own InputSystemUIInputModule turns Enter into a submit on the selected field.
    public sealed class MainMenuLoginWindowSceneTests
    {
        const string MenuScene = "MainMenu";
        const string WindowName = "LoginWindow";
        // Contract v2 「설계 고정」, third bullet.
        const string TitleText = "로그인";
        const string AccountLabelText = "계정 이름";
        const string HostLabelText = "서버 주소";
        const string StartText = "게임 시작";
        const string QuitText = "종료";
        // Contract v2 「설계 고정」, fifth bullet; MainMenu.unity at 7bc75898 line 528 (TITLE's sprite).
        const string LogoGuid = "0af51ea856cc44f49847c67cf6b32bb7";
        // AccountNameRule examples: LoginRejectionMenuTests.cs line 28 (accepted) and line 48 (a hyphen breaks the rule).
        const string ValidAccountName = "alice_01";
        const string InvalidAccountName = "bad-name";
        // MainMenuController.cs line 103, written once per probe the Start path begins.
        const string ProbeLogPrefix = "[MainMenu] Server probe 시도";
        // Lead answer msg_cfe6f4d27fc7 (2): only this variable decides whether PNGs are written; the checks run either way.
        const string CaptureDirectoryVariable = "DAWNHOLDER_LOGIN_CAPTURE_DIR";
        // Screen pixels; absorbs float layout without hiding a real overlap.
        const float Tolerance = 1f;
        static readonly Color s_background = new(0.08f, 0.09f, 0.11f, 1f);

        // Bright pieces by name and GUID, copied from Tests/EditMode/BrightUiKitTestSupport.cs (the PlayMode assembly
        // cannot reference EditMode tests). Line numbers are that file's at b76841f1.
        static readonly Dictionary<string, string> s_brightGuids = new()
        {
            ["Panel_Default"] = "387817131a7ee7abd35357ff32e6cd2a", // line 56
            ["Button_Default_Normal"] = "218e99a5632015261381128b2909788b", // line 60
            ["Button_Default_Highlighted"] = "8c51a6fac51dd8f61ef4cede5690f59c", // line 61
            ["Button_Default_Pressed"] = "09e130da54a609f68834547b2363b2a6", // line 62
            ["Button_Default_Disabled"] = "0438a51907e9b5791382a7beecdf53ad", // line 63
            ["Button_Accept_Normal"] = "fdaa805e70f5533488dae81f7234282a", // line 64
            ["Button_Accept_Highlighted"] = "b19f5a6d0300e566d0fffea8534ac8ca", // line 65
            ["Button_Accept_Pressed"] = "819be5b406aaa976f19a17fb72ef6fae", // line 66
            ["Button_Accept_Disabled"] = "d14f5c1a8a0197176bd076a96fb0009e", // line 67
            ["InputField"] = "38f801c703515178c835f249a4db2983", // line 76
        };

        readonly List<string> _probeLogs = new();
        readonly List<(Canvas Canvas, RenderMode Mode, Camera Camera)> _switchedCanvases = new();
        Scene _menu;
        MainMenuController _controller;
        Keyboard _keyboard;
        InputSettings _originalInputSettings;
        InputSettings _originalInputSettingsCopy;
        InputSettings _runtimeInputSettings;
        Socket _closedPort;
        GameObject _cameraObject;
        RenderTexture _target;
        Texture2D _readback;
        float _oldListenerVolume;
        bool _oldRunInBackground;
        bool _captured;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsTrue(Application.isPlaying, "this check must execute real Unity PlayMode");
            Assert.IsFalse(SceneManager.GetSceneByName(MenuScene).isLoaded, "fixture: another test left MainMenu loaded");
            yield return MapEntryPlayFixture.Wait(
                () => NetworkService.Instance != null && SceneTransition.Instance != null, "persistent services");
            Assert.IsFalse(NetworkService.Instance.IsConnected, "fixture must not adopt another session");
            _probeLogs.Clear();
            _oldListenerVolume = AudioListener.volume;
            _oldRunInBackground = Application.runInBackground;
            _originalInputSettings = InputSystem.settings;
            _captured = true;
            // Test play stays silent, as MapEntryPlayFixture does; a runtime value, never a project setting.
            AudioListener.volume = 0f;
            // As MapEntryPlayFixture.Prepare: a batchmode Editor has no focus, so input must ignore focus to arrive.
            _originalInputSettingsCopy = UnityEngine.Object.Instantiate(_originalInputSettings);
            _originalInputSettingsCopy.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings = UnityEngine.Object.Instantiate(_originalInputSettings);
            _runtimeInputSettings.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _runtimeInputSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _runtimeInputSettings;
            Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>("MainMenuLoginTestKeyboard");
            _keyboard.MakeCurrent();
            MainMenuNotice.TryTake(out _);
            Application.logMessageReceived += RecordProbeLog;

            AsyncOperation load = SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "MainMenu load");
            _menu = SceneManager.GetSceneByName(MenuScene);
            Assert.AreEqual(MenuScene, SceneManager.GetActiveScene().name, "fixture: MainMenu is the active scene");
            // Awake and Start of every scene object have run after the first two frames.
            yield return null;
            yield return null;
            MainMenuController[] controllers = _menu.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true)).ToArray();
            Assert.AreEqual(1, controllers.Length, "fixture: MainMenu has one MainMenuController");
            _controller = controllers[0];
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var errors = new List<Exception>();
            void Restore(Action action)
            {
                try { action(); }
                catch (Exception error) { errors.Add(error); }
            }

            Application.logMessageReceived -= RecordProbeLog;
            foreach ((Canvas canvas, RenderMode mode, Camera camera) in _switchedCanvases)
            {
                Restore(() =>
                {
                    if (canvas == null) return;
                    canvas.renderMode = mode;
                    canvas.worldCamera = camera;
                });
            }
            _switchedCanvases.Clear();
            Restore(() => { if (_cameraObject != null) UnityEngine.Object.Destroy(_cameraObject); });
            Restore(() =>
            {
                if (_target == null) return;
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            });
            Restore(() => { if (_readback != null) UnityEngine.Object.Destroy(_readback); });
            Restore(() => _closedPort?.Dispose());
            Restore(() => { if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard); });
            if (_captured)
            {
                // Unity null check, not ??: a destroyed original reads as null, and its copy takes its place.
                Restore(() =>
                {
                    InputSettings restored = _originalInputSettings != null ? _originalInputSettings : _originalInputSettingsCopy;
                    if (restored != null) InputSystem.settings = restored;
                });
                Restore(() => Application.runInBackground = _oldRunInBackground);
                Restore(() => AudioListener.volume = _oldListenerVolume);
                foreach (InputSettings copy in new[] { _runtimeInputSettings, _originalInputSettingsCopy })
                    Restore(() => { if (copy != null && copy != InputSystem.settings) UnityEngine.Object.Destroy(copy); });
            }
            Restore(() => MainMenuNotice.TryTake(out _));
            _captured = false;
            _controller = null;

            // Leave an empty active scene, as the runner starts, for the PlayMode classes that follow.
            const string emptyName = "MainMenuLoginWindowSceneTests-empty";
            Scene empty = SceneManager.GetSceneByName(emptyName);
            if (!empty.IsValid()) empty = SceneManager.CreateScene(emptyName);
            SceneManager.SetActiveScene(empty);
            Scene menu = SceneManager.GetSceneByName(MenuScene);
            if (menu.IsValid() && menu.isLoaded) yield return SceneManager.UnloadSceneAsync(menu);
            if (errors.Count != 0) throw new AggregateException("MainMenu login test restoration failed", errors);
        }

        // T1 (completion 1): one LoginWindow on the bright Panel holds the scene's own two fields, two buttons and error
        // text with the fixed words, the bright Accept/Default buttons and InputField backgrounds; one title logo.
        [UnityTest]
        public IEnumerator T1_MenuStart_BuildsOneBrightLoginWindowAroundTheSceneControls()
        {
            LoginParts parts = FindParts();
            Assert.IsTrue(parts.Window.gameObject.activeInHierarchy, "the LoginWindow is inactive after the menu starts");

            var problems = new List<string>();
            var window = parts.Window.GetComponent<Image>();
            if (window == null) problems.Add("LoginWindow has no Image");
            else if (!IsPiece(window.sprite, "Panel_Default")) problems.Add($"LoginWindow shows {Describe(window.sprite)}, the bright Panel_Default");

            foreach ((string name, Component owner) in parts.WindowContents)
                if (!owner.transform.IsChildOf(parts.Window)) problems.Add($"{name} {PathOf(owner)} is outside the LoginWindow");

            // The scene's own fields, not copies: the controller's two are the only input fields in the window.
            TMP_InputField[] fields = parts.Window.GetComponentsInChildren<TMP_InputField>(true);
            if (fields.Length != 2 || !fields.Contains(parts.Account) || !fields.Contains(parts.Host))
                problems.Add($"the LoginWindow holds input fields [{string.Join(", ", fields.Select(PathOf))}], the controller's two only");

            ExpectLabel(parts.Window, TitleText, "title", problems);
            ExpectLabel(parts.Window, AccountLabelText, "account field name", problems);
            ExpectLabel(parts.Window, HostLabelText, "server address field name", problems);
            ExpectButtonText(parts.Start, StartText, "start button", problems);
            ExpectButtonText(parts.Quit, QuitText, "quit button", problems);
            problems.AddRange(ButtonViolations(parts.Start, "Accept").Select(problem => "start button: " + problem));
            problems.AddRange(ButtonViolations(parts.Quit, "Default").Select(problem => "quit button: " + problem));
            ExpectFieldBackground(parts.Account, "account field", problems);
            ExpectFieldBackground(parts.Host, "server address field", problems);

            Assert.IsEmpty(problems, "T1 the LoginWindow follows the fixed design");
            yield break;
        }

        // T2 (completion 1) at 1920×1080: every part on screen, the logo above and apart from the window, the window's
        // controls apart, the fixed words whole; captures login-1920x1080.png and login-error-1920x1080.png.
        [UnityTest]
        public IEnumerator T2_At1920x1080_TheLogoWindowAndControlsStayOnScreenApartAndWhole() => Layout(1920, 1080, true);

        // T2 (completion 1) at 1280×720: the same checks; captures login-1280x720.png.
        [UnityTest]
        public IEnumerator T2_At1280x720_TheLogoWindowAndControlsStayOnScreenApartAndWhole() => Layout(1280, 720, false);

        // T4 (completion 2): Enter in the selected account field does what one Start press does.
        [UnityTest]
        public IEnumerator T4_EnterInTheAccountField_WithABadName_ShowsTheNameRule() => EnterWithBadName(useAccountField: true);

        // T4 (completion 2): Enter in the selected server address field does what one Start press does.
        [UnityTest]
        public IEnumerator T4_EnterInTheServerAddressField_WithABadName_ShowsTheNameRule() => EnterWithBadName(useAccountField: false);

        // T4 (completion 2): two quick Enters with an accepted name start one server probe; the second meets the probe
        // lock. The probe goes to a loopback port this test holds bound without listening, so nothing answers it.
        [UnityTest]
        public IEnumerator T4_TwoQuickEntersInTheAccountField_StartOneServerProbe()
        {
            var account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField");
            var host = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_serverHostInputField");
            var start = MapEntryPlayFixture.Field<Button>(_controller, "_startButton");
            Assert.IsNotNull(account, "fixture: the controller has its account field");
            Assert.IsNotNull(host, "fixture: the controller has its server address field");
            Assert.IsNotNull(start, "fixture: the controller has its start button");
            _closedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _closedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            int port = ((IPEndPoint)_closedPort.LocalEndPoint).Port;
            // A scene object's runtime value only; the scene is never saved.
            MapEntryPlayFixture.Set(_controller, "_serverPort", port);
            host.text = "127.0.0.1";
            account.text = ValidAccountName;
            _probeLogs.Clear();

            yield return Select(account);
            yield return Press(Key.Enter);
            int probesAfterFirstEnter = _probeLogs.Count;
            bool probeRunningAtSecondEnter = !start.interactable;
            yield return Press(Key.Enter);
            int probesAfterBothEnters = _probeLogs.Count;
            // The refused probe reports back within its 3000 ms connect timeout and unlocks the start button.
            yield return MapEntryPlayFixture.Wait(() => start.interactable, "the probe to finish", 10);

            Assert.AreEqual(1, probesAfterFirstEnter,
                $"the first Enter in the account field started {probesAfterFirstEnter} server probes, one Start press starts one");
            Assert.IsTrue(probeRunningAtSecondEnter,
                "fixture: the first probe had already ended at the second Enter, so the quick second press was not tested");
            Assert.AreEqual(1, probesAfterBothEnters,
                $"two quick Enters started {probesAfterBothEnters} server probes; the second must meet the running probe's lock");
            Assert.AreEqual(1, _probeLogs.Count, "a server probe started after the two Enters");
            Assert.AreEqual(MenuScene, SceneManager.GetActiveScene().name, "the probe to a closed loopback port left the menu");
        }

        // T5 (completion 2): Tab in the selected account field selects the server address field.
        [UnityTest]
        public IEnumerator T5_TabInTheAccountField_SelectsTheServerAddressField()
        {
            var account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField");
            var host = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_serverHostInputField");
            Assert.IsNotNull(account, "fixture: the controller has its account field");
            Assert.IsNotNull(host, "fixture: the controller has its server address field");

            yield return Select(account);
            GameObject before = EventSystem.current.currentSelectedGameObject;
            Assert.AreSame(account.gameObject, before, "fixture: the account field is selected before Tab");
            yield return Press(Key.Tab);
            GameObject after = EventSystem.current.currentSelectedGameObject;

            Assert.AreSame(host.gameObject, after,
                $"Tab in the account field left {(after != null ? after.name : "nothing")} selected, the server address field expected");
        }

        IEnumerator EnterWithBadName(bool useAccountField)
        {
            var account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField");
            var host = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_serverHostInputField");
            var error = MapEntryPlayFixture.Field<TMP_Text>(_controller, "_errorMessageText");
            Assert.IsNotNull(account, "fixture: the controller has its account field");
            Assert.IsNotNull(host, "fixture: the controller has its server address field");
            Assert.IsNotNull(error, "fixture: the controller has its error text");
            account.text = InvalidAccountName;
            host.text = "127.0.0.1";
            error.text = "";
            _probeLogs.Clear();
            TMP_InputField field = useAccountField ? account : host;

            yield return Select(field);
            Assert.AreSame(field.gameObject, EventSystem.current.currentSelectedGameObject, "fixture: the field is selected before Enter");
            yield return Press(Key.Enter);
            string shown = error.text;

            Assert.AreEqual(MainMenuNotice.InvalidAccountName, shown,
                $"Enter in the {(useAccountField ? "account" : "server address")} field with \"{InvalidAccountName}\" " +
                "did not show the account name rule a Start press shows");
            Assert.IsEmpty(_probeLogs, "a rule-breaking name started a server probe");
        }

        IEnumerator Layout(int width, int height, bool captureError)
        {
            LoginParts parts = FindParts();
            Camera camera = AttachCamera(width, height, parts);
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
            var problems = new List<string>();

            yield return Settle();
            Check(camera, parts, $"{width}x{height}", false, problems);
            if (!string.IsNullOrEmpty(directory)) Save(camera, directory, $"login-{width}x{height}.png", problems);

            // The error line as a player meets it: a rule-breaking name, then Start.
            parts.Account.text = InvalidAccountName;
            parts.Start.onClick.Invoke();
            yield return Settle();
            Assert.AreEqual(MainMenuNotice.InvalidAccountName, parts.Error.text, "fixture: Start with a rule-breaking name shows the rule");
            Check(camera, parts, $"{width}x{height} error", true, problems);
            if (captureError && !string.IsNullOrEmpty(directory)) Save(camera, directory, $"login-error-{width}x{height}.png", problems);

            Assert.IsEmpty(problems, $"T2 {width}x{height}: logo, window and controls on screen, apart and whole");
        }

        // Lead answer msg_cfe6f4d27fc7 (1), the fixture of CharacterMenuViewCaptureTests.cs lines 138-171: each root
        // canvas holding a part draws through a camera into a render texture of the target size. Only renderMode and
        // worldCamera change; the scene's CanvasScaler keeps its values and must see the target size by itself. This
        // measures that scaler on a render texture, not the Overlay game screen.
        Camera AttachCamera(int width, int height, LoginParts parts)
        {
            _cameraObject = new GameObject("Login capture camera (fixture)");
            var camera = _cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = s_background;
            camera.cullingMask = ~0;
            camera.orthographic = true;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            camera.targetTexture = _target;
            _readback = new Texture2D(width, height, TextureFormat.RGBA32, false, false);

            Canvas[] canvases = parts.All.Select(owner => owner.GetComponentInParent<Canvas>(true))
                .Where(canvas => canvas != null).Select(canvas => canvas.rootCanvas).Distinct().ToArray();
            Assert.IsNotEmpty(canvases, "fixture: the login parts are drawn by a canvas");
            foreach (Canvas canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                string before = ScalerValues(scaler);
                _switchedCanvases.Add((canvas, canvas.renderMode, canvas.worldCamera));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                Assert.AreEqual(before, ScalerValues(scaler), $"fixture: switching {canvas.name} changed its CanvasScaler");
            }
            return camera;
        }

        // Two frames let each CanvasScaler.Update see the new surface; then the layout is rebuilt.
        IEnumerator Settle()
        {
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var switched in _switchedCanvases)
            {
                Canvas canvas = switched.Canvas;
                Vector2 seen = canvas.renderingDisplaySize;
                Vector2 units = ((RectTransform)canvas.transform).rect.size;
                TestContext.Out.WriteLine($"[login capture] {_target.width}x{_target.height} {canvas.name}: renderingDisplaySize {seen} " +
                    $"scaleFactor {canvas.scaleFactor:F4} canvas {units} scaler {ScalerValues(canvas.GetComponent<CanvasScaler>())}");
                Assert.IsTrue(Mathf.Abs(seen.x - _target.width) < 0.5f && Mathf.Abs(seen.y - _target.height) < 0.5f,
                    $"fixture: {canvas.name} sees a {seen} screen, the capture is {_target.width}x{_target.height}");
                Assert.IsTrue(Mathf.Abs(units.x * canvas.scaleFactor - _target.width) < 1f &&
                    Mathf.Abs(units.y * canvas.scaleFactor - _target.height) < 1f,
                    $"fixture: {canvas.name} {units} at scale {canvas.scaleFactor} fills the {_target.width}x{_target.height} capture");
            }
        }

        void Check(Camera camera, LoginParts parts, string label, bool errorShown, List<string> problems)
        {
            Canvas.ForceUpdateCanvases();
            var screen = new Rect(0, 0, _target.width, _target.height);
            var areas = parts.Named.ToDictionary(part => part.Name, part => OnScreen(camera, (RectTransform)part.Owner.transform));
            foreach (KeyValuePair<string, Rect> area in areas)
            {
                if (area.Value.width < 1f || area.Value.height < 1f) problems.Add($"{label}: {area.Key} has no area ({area.Value})");
                if (!Inside(area.Value, screen)) problems.Add($"{label}: {area.Key} leaves the screen at {area.Value}");
            }

            Rect window = areas["LoginWindow"];
            Rect logo = areas["logo"];
            if (Overlaps(logo, window)) problems.Add($"{label}: logo {logo} and LoginWindow {window} overlap");
            if (logo.yMin < window.yMax - Tolerance) problems.Add($"{label}: logo {logo} is not above the LoginWindow {window}");
            string[] inWindow = { "account field", "server address field", "start button", "quit button", "error text" };
            foreach (string name in inWindow)
                if (!Inside(areas[name], window)) problems.Add($"{label}: {name} {areas[name]} is drawn outside the LoginWindow {window}");
            for (int i = 0; i < inWindow.Length; i++)
            {
                for (int j = i + 1; j < inWindow.Length; j++)
                    if (Overlaps(areas[inWindow[i]], areas[inWindow[j]]))
                        problems.Add($"{label}: {inWindow[i]} {areas[inWindow[i]]} and {inWindow[j]} {areas[inWindow[j]]} overlap");
            }

            var words = new List<(string Name, TMP_Text Text)>
            {
                ("title", FindLabel(parts.Window, TitleText)),
                ("account field name", FindLabel(parts.Window, AccountLabelText)),
                ("server address field name", FindLabel(parts.Window, HostLabelText)),
                ("start button text", FindButtonText(parts.Start, StartText)),
                ("quit button text", FindButtonText(parts.Quit, QuitText)),
            };
            if (errorShown) words.Add(("error text", parts.Error));
            foreach ((string name, TMP_Text text) in words)
            {
                if (text == null)
                {
                    problems.Add($"{label}: no {name} to measure");
                    continue;
                }
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) problems.Add($"{label}: {name} \"{text.text}\" overflows its rect");
                if (text.isTextTruncated) problems.Add($"{label}: {name} \"{text.text}\" lost characters");
                if (TryGlyphArea(text, out Vector2 min, out Vector2 max) && !Inside(Rect.MinMaxRect(min.x, min.y, max.x, max.y), text.rectTransform.rect))
                    problems.Add($"{label}: {name} \"{text.text}\" draws {min}..{max} outside its rect {text.rectTransform.rect}");
            }

            string record = $"[login capture] {label}: " + string.Join(", ", areas.Select(area => $"{area.Key} {area.Value}"));
            TestContext.Out.WriteLine(record);
            Debug.Log(record);
        }

        void Save(Camera camera, string directory, string file, List<string> problems)
        {
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase),
                $"fixture: {CaptureDirectoryVariable} must be outside the Unity project ({full})");
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
                _readback.Apply(false);
            }
            finally
            {
                RenderTexture.active = previous;
            }
            Color32 clear = s_background;
            int drawn = _readback.GetPixels32().Count(pixel =>
                Math.Abs(pixel.r - clear.r) > 2 || Math.Abs(pixel.g - clear.g) > 2 || Math.Abs(pixel.b - clear.b) > 2);
            int total = _target.width * _target.height;
            // The background covers the screen; a nearly empty frame means the fixture camera drew nothing.
            if (drawn < total / 2) problems.Add($"fixture: {file} drew {drawn} of {total} pixels");
            Directory.CreateDirectory(full);
            string path = Path.Combine(full, file);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] png = _readback.EncodeToPNG();
                stream.Write(png, 0, png.Length);
            }
            string record = $"[login capture] saved {path}: drawn {drawn}/{total} px";
            TestContext.Out.WriteLine(record);
            Debug.Log(record);
        }

        // The parts the design fixes, each found once; a missing one fails here with its own message.
        LoginParts FindParts()
        {
            RectTransform[] windows = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(rect => rect.name == WindowName).ToArray();
            Assert.AreEqual(1, windows.Length,
                $"MainMenu has {windows.Length} RectTransforms named {WindowName} after it starts, the design fixes one");
            Assert.AreEqual(_menu, windows[0].gameObject.scene, $"the {WindowName} is outside the MainMenu scene");

            var parts = new LoginParts
            {
                Window = windows[0],
                Account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField"),
                Host = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_serverHostInputField"),
                Start = MapEntryPlayFixture.Field<Button>(_controller, "_startButton"),
                Error = MapEntryPlayFixture.Field<TMP_Text>(_controller, "_errorMessageText"),
            };
            Assert.IsNotNull(parts.Account, "MainMenuController lost its account field");
            Assert.IsNotNull(parts.Host, "MainMenuController lost its server address field");
            Assert.IsNotNull(parts.Start, "MainMenuController lost its start button");
            Assert.IsNotNull(parts.Error, "MainMenuController lost its error text");

            Button[] starts = ListenerButtons(nameof(MainMenuController.OnStartClicked));
            Assert.AreEqual(1, starts.Length, $"MainMenu has {starts.Length} buttons calling OnStartClicked, one expected");
            Assert.AreSame(parts.Start, starts[0], "the button calling OnStartClicked is not the controller's start button");
            Button[] quits = ListenerButtons(nameof(MainMenuController.OnQuitClicked));
            Assert.AreEqual(1, quits.Length, $"MainMenu has {quits.Length} buttons calling OnQuitClicked, one expected");
            parts.Quit = quits[0];

            Image[] logos = _menu.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Image>(true))
                .Where(image => image.isActiveAndEnabled && SourceGuid(image.sprite) == LogoGuid).ToArray();
            Assert.AreEqual(1, logos.Length,
                $"MainMenu shows {logos.Length} Images of the title banner {LogoGuid}, the design fixes one logo");
            parts.Logo = logos[0];
            return parts;
        }

        Button[] ListenerButtons(string method) => _menu.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Button>(true))
            .Where(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index =>
                ReferenceEquals(button.onClick.GetPersistentTarget(index), _controller) &&
                button.onClick.GetPersistentMethodName(index) == method))
            .ToArray();

        // Selecting a field as the EventSystem does on a click; the field activates itself on select.
        IEnumerator Select(TMP_InputField field)
        {
            Assert.IsNotNull(EventSystem.current, "fixture: MainMenu has an EventSystem");
            EventSystem.current.SetSelectedGameObject(field.gameObject);
            yield return null;
            yield return null;
        }

        // One key down for a frame and up again, through the Input System as a player's keyboard sends it.
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return null;
        }

        void RecordProbeLog(string message, string stackTrace, LogType type)
        {
            if (message.StartsWith(ProbeLogPrefix, StringComparison.Ordinal)) _probeLogs.Add(message);
        }

        static void ExpectLabel(RectTransform window, string words, string name, List<string> problems)
        {
            if (FindLabel(window, words) == null)
                problems.Add($"the LoginWindow shows no {name} \"{words}\" outside its input fields");
        }

        static void ExpectButtonText(Button button, string words, string name, List<string> problems)
        {
            if (FindButtonText(button, words) == null)
            {
                string[] shown = button.GetComponentsInChildren<TMP_Text>(true).Select(text => $"\"{text.text}\"").ToArray();
                problems.Add($"the {name} shows [{string.Join(", ", shown)}], \"{words}\" expected");
            }
        }

        static void ExpectFieldBackground(TMP_InputField field, string name, List<string> problems)
        {
            var background = field.GetComponent<Image>();
            if (background == null) problems.Add($"the {name} has no background Image");
            else if (!IsPiece(background.sprite, "InputField")) problems.Add($"the {name} background shows {Describe(background.sprite)}, the bright InputField");
        }

        // A visible label is a TMP text of its own: placeholders and typed text inside an input field do not count.
        static TMP_Text FindLabel(RectTransform window, string words) => window.GetComponentsInChildren<TMP_Text>(false)
            .FirstOrDefault(text => text.isActiveAndEnabled && text.text == words && text.GetComponentInParent<TMP_InputField>(true) == null);

        static TMP_Text FindButtonText(Button button, string words) => button.GetComponentsInChildren<TMP_Text>(false)
            .FirstOrDefault(text => text.isActiveAndEnabled && text.text == words);

        // Normal sprite on the target graphic and SpriteSwap states of one bright family, as
        // CharacterMenuViewAppearanceTests.ButtonViolations (lines 341-354) checks the character select buttons.
        static List<string> ButtonViolations(Button button, string family)
        {
            var problems = new List<string>();
            var graphic = button.targetGraphic as Image;
            if (graphic == null || !IsPiece(graphic.sprite, $"Button_{family}_Normal"))
                problems.Add($"shows {Describe(graphic != null ? graphic.sprite : null)}, the bright Button_{family}_Normal");
            if (button.transition != Selectable.Transition.SpriteSwap) problems.Add($"transition {button.transition}, SpriteSwap expected");
            SpriteState states = button.spriteState;
            if (!IsPiece(states.highlightedSprite, $"Button_{family}_Highlighted")) problems.Add($"highlighted state shows {Describe(states.highlightedSprite)}");
            if (!IsPiece(states.pressedSprite, $"Button_{family}_Pressed")) problems.Add($"pressed state shows {Describe(states.pressedSprite)}");
            if (!IsPiece(states.disabledSprite, $"Button_{family}_Disabled")) problems.Add($"disabled state shows {Describe(states.disabledSprite)}");
            return problems;
        }

        // The bright piece is the imported asset under its own name and GUID, as BrightUiKitTestSupport.PieceOf.
        static bool IsPiece(Sprite sprite, string name) =>
            sprite != null && sprite.name == name && SourceGuid(sprite) == s_brightGuids[name];

        static string Describe(Sprite sprite) => sprite == null ? "no sprite" : $"{sprite.name} ({SourceGuid(sprite) ?? "no asset"})";

        // The GUID of the asset file a sprite was imported from; null for runtime sprites without one.
        static string SourceGuid(Sprite sprite)
        {
#if UNITY_EDITOR
            return sprite != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _) ? guid : null;
#else
            Assert.Fail("the bright piece checks read sprite GUIDs through the Editor AssetDatabase");
            return null;
#endif
        }

        static string ScalerValues(CanvasScaler scaler) => scaler == null ? "no CanvasScaler" :
            $"{scaler.uiScaleMode} scale {scaler.scaleFactor} reference {scaler.referenceResolution} match {scaler.screenMatchMode} " +
            $"{scaler.matchWidthOrHeight} ppu {scaler.referencePixelsPerUnit}";

        static Rect OnScreen(Camera camera, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        // The glyph boxes as drawn, as CharacterMenuViewCaptureTests.TryGlyphArea (lines 259-274).
        static bool TryGlyphArea(TMP_Text text, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            bool visible = false;
            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                visible = true;
                min = Vector2.Min(min, character.bottomLeft);
                max = Vector2.Max(max, character.topRight);
            }
            return visible;
        }

        static bool Inside(Rect inner, Rect outer) => inner.xMin >= outer.xMin - Tolerance && inner.xMax <= outer.xMax + Tolerance &&
            inner.yMin >= outer.yMin - Tolerance && inner.yMax <= outer.yMax + Tolerance;

        static bool Overlaps(Rect a, Rect b) => a.xMin < b.xMax - Tolerance && b.xMin < a.xMax - Tolerance &&
            a.yMin < b.yMax - Tolerance && b.yMin < a.yMax - Tolerance;

        static string PathOf(Component component)
        {
            var names = new List<string>();
            for (Transform node = component.transform; node != null; node = node.parent) names.Add(node.name);
            names.Reverse();
            return string.Join("/", names);
        }

        sealed class LoginParts
        {
            internal RectTransform Window;
            internal TMP_InputField Account;
            internal TMP_InputField Host;
            internal Button Start;
            internal Button Quit;
            internal TMP_Text Error;
            internal Image Logo;

            internal IEnumerable<(string Name, Component Owner)> WindowContents => new (string, Component)[]
            {
                ("account field", Account), ("server address field", Host), ("start button", Start),
                ("quit button", Quit), ("error text", Error),
            };

            internal IEnumerable<(string Name, Component Owner)> Named =>
                new (string, Component)[] { ("LoginWindow", Window), ("logo", Logo) }.Concat(WindowContents);

            internal IEnumerable<Component> All => Named.Select(part => part.Owner);
        }
    }
}
