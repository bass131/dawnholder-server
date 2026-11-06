using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // MainMenu.Start builds the real view. These literal layout requirements do not use the view's constants.
    // Only the invalid-account path is invoked: no probe, server, password persistence or live login is needed.
    public sealed class MainMenuLoginSizingSceneTests
    {
        const float Tolerance = 0.05f;
        MainMenuController _controller;
        string _oldAccount;
        float _oldVolume;
        bool _captured;
        bool _loaded;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _loaded = false;
            _captured = false;
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null && SceneTransition.Instance != null,
                "fixture: persistent menu services");
            Assert.IsFalse(NetworkService.Instance.IsConnected, "fixture: cannot adopt a live connection");
            Assert.IsFalse(SceneManager.GetSceneByName("MainMenu").isLoaded, "fixture: previous MainMenu unloaded");
            _oldAccount = AccountLoadout.SessionAccountName;
            _oldVolume = AudioListener.volume;
            _captured = true;
            AudioListener.volume = 0;
            MainMenuNotice.TryTake(out _);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            _loaded = true;
            yield return null;
            yield return null;
            MainMenuController[] controllers = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true)).ToArray();
            Assert.AreEqual(1, controllers.Length, "fixture: actual MainMenu controller");
            _controller = controllers[0];
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_captured)
            {
                AccountLoadout.SessionAccountName = _oldAccount;
                AudioListener.volume = _oldVolume;
                MainMenuNotice.TryTake(out _);
            }
            if (_loaded)
            {
                Scene empty = SceneManager.GetSceneByName("LoginSizingTests-empty");
                if (!empty.IsValid()) empty = SceneManager.CreateScene("LoginSizingTests-empty");
                SceneManager.SetActiveScene(empty);
                Scene menu = SceneManager.GetSceneByName("MainMenu");
                if (menu.IsValid() && menu.isLoaded) yield return SceneManager.UnloadSceneAsync(menu);
            }
            _controller = null;
            _captured = false;
            _loaded = false;
        }

        [UnityTest]
        public IEnumerator G3_CentredLoginLayoutAt1280x720() => LayoutAt(1280, 720);

        [UnityTest]
        public IEnumerator G3_CentredLoginLayoutAt1920x1080() => LayoutAt(1920, 1080);

        IEnumerator LayoutAt(int width, int height)
        {
            TMP_InputField account = MapEntryPlayFixture.Field<TMP_InputField>(_controller, "_accountNameInputField");
            Assert.IsNotNull(account, "fixture: scene account field");
            var window = account.transform.parent as RectTransform;
            Assert.AreEqual("LoginWindow", window.name, "fixture: runtime login window");
            RectTransform passwordRect = Child(window, "PasswordInput");
            TMP_InputField password = passwordRect.GetComponent<TMP_InputField>();
            Assert.IsNotNull(password, "fixture: actual password field");
            TMP_Text error = MapEntryPlayFixture.Field<TMP_Text>(_controller, "_errorMessageText");
            Assert.IsNotNull(error, "fixture: actual error label");
            RectTransform idLabel = Child(window, "AccountLabel");
            RectTransform pwLabel = Child(window, "PasswordLabel");
            RectTransform start = Child(window, "StartButton");
            RectTransform quit = Child(window, "QuitButton");
            Canvas canvas = account.GetComponentInParent<Canvas>().rootCanvas;
            RectTransform banner = Child(canvas.transform, "TITLE");
            account.SetTextWithoutNotify("centred_test");
            password.SetTextWithoutNotify("abcdefgh");
            using (GuildSizeSurface surface = GuildSizeSurface.Create(width, height))
            {
                surface.Attach(canvas);
                yield return GuildSizeSurface.Settle();
                Assert.IsEmpty(surface.FixtureProblems(), "fixture: actual requested login render size");
                var errors = new List<string>();
                Shape(errors, window, new Vector2(420, 230), new Vector2(0, 16));
                Shape(errors, idLabel, new Vector2(44, 30), new Vector2(54, -18));
                Shape(errors, pwLabel, new Vector2(44, 30), new Vector2(54, -60));
                Shape(errors, (RectTransform)account.transform, new Vector2(260, 30), new Vector2(106, -18));
                Shape(errors, passwordRect, new Vector2(260, 30), new Vector2(106, -60));
                Shape(errors, error.rectTransform, new Vector2(380, 30), new Vector2(20, -96));
                Shape(errors, start, new Vector2(200, 36), new Vector2(110, -132));
                Shape(errors, quit, new Vector2(200, 36), new Vector2(110, -176));
                Number(errors, 16, idLabel.GetComponent<TMP_Text>().fontSize, "ID label font size");
                Number(errors, 16, pwLabel.GetComponent<TMP_Text>().fontSize, "PW label font size");
                Number(errors, 18, account.pointSize, "ID pointSize");
                Number(errors, 18, password.pointSize, "PW pointSize");
                Number(errors, 14, error.fontSize, "error font size");
                Assert.AreEqual(TextAlignmentOptions.Left, account.textComponent.alignment,
                    "preservation: ID typed text alignment stays Left");
                Assert.AreEqual(TextAlignmentOptions.Left, password.textComponent.alignment,
                    "preservation: PW typed text alignment stays Left");
                Rect idArea = InWindow(window, (RectTransform)account.transform);
                Rect pwArea = InWindow(window, passwordRect);
                Rect labelArea = InWindow(window, idLabel);
                Rect startArea = InWindow(window, start);
                Rect quitArea = InWindow(window, quit);
                Rect errorArea = InWindow(window, error.rectTransform);
                Number(errors, 18, window.rect.yMax - idArea.yMax, "top margin");
                Number(errors, 18, quitArea.yMin - window.rect.yMin, "bottom margin");
                Number(errors, 54, labelArea.xMin - window.rect.xMin, "label left margin");
                Number(errors, 54, window.rect.xMax - idArea.xMax, "input right margin");
                Number(errors, window.rect.center.x, startArea.center.x, "START centre");
                Number(errors, window.rect.center.x, quitArea.center.x, "QUIT centre");
                if (errorArea.yMax > pwArea.yMin + Tolerance || errorArea.yMin < startArea.yMax - Tolerance)
                    errors.Add("error message must be below PW and above START without overlap");
                Rect[] controls = { surface.PixelRect((RectTransform)account.transform), surface.PixelRect(passwordRect),
                    surface.PixelRect(start), surface.PixelRect(quit) };
                Rect bannerPixels = surface.PixelRect(banner);
                for (int index = 0; index < controls.Length; index++)
                {
                    Assert.IsFalse(controls[index].Overlaps(bannerPixels), "B1 preservation: control/banner overlap");
                    for (int other = index + 1; other < controls.Length; other++)
                        Assert.IsFalse(controls[index].Overlaps(controls[other]), "B1 preservation: controls overlap");
                }
                TestContext.Out.WriteLine(surface.Capture($"login-centred-filled-{width}x{height}.png", out float drawn));
                Assert.Greater(drawn, 0, "fixture: actual filled login pixels");
                account.SetTextWithoutNotify("bad-name");
                _controller.OnStartClicked();
                yield return null;
                Assert.AreEqual(MainMenuNotice.InvalidAccountName, error.text, "preservation: actual invalid-name notice");
                Assert.IsFalse(MapEntryPlayFixture.Field<bool>(_controller, "_probing"),
                    "preservation: invalid account starts no server probe");
                TestContext.Out.WriteLine(surface.Capture($"login-centred-error-{width}x{height}.png", out drawn));
                Assert.Greater(drawn, 0, "fixture: actual error-state pixels");
                TestContext.Out.WriteLine(surface.Describe());
                Assert.IsEmpty(errors, string.Join("\n", errors));
            }
        }

        static RectTransform Child(Transform parent, string name)
        {
            var child = parent.Find(name) as RectTransform;
            Assert.IsNotNull(child, "fixture: actual scene/view child " + name);
            return child;
        }

        static Rect InWindow(RectTransform window, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Vector2 lo = window.InverseTransformPoint(corners[0]);
            Vector2 hi = window.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }

        static void Shape(List<string> errors, RectTransform rect, Vector2 size, Vector2 position)
        {
            Number(errors, size.x, rect.rect.width, rect.name + " width");
            Number(errors, size.y, rect.rect.height, rect.name + " height");
            Number(errors, position.x, rect.anchoredPosition.x, rect.name + " x");
            Number(errors, position.y, rect.anchoredPosition.y, rect.name + " y");
        }

        static void Number(List<string> errors, float expected, float actual, string label)
        {
            TestContext.Out.WriteLine($"{label}: expected={expected:F3} observed={actual:F3}");
            if (Mathf.Abs(expected - actual) > Tolerance)
                errors.Add($"{label}: expected={expected:F3} observed={actual:F3}");
        }
    }
}
