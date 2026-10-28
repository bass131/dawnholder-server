using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Real window, TMP meshes and EventSystem raycasts in an isolated scene, with a supplied display model.
    // Run in each supported viewport through the existing execution lane; this fixture does not resize the game view
    // or claim a live Town/server check. uGUI remains reflection-only, as required by this assembly's references.
    public sealed class GuildHousingTests
    {
        static readonly string[] Labels =
        {
            "GuildName", "AutoJoinNotice", "MemberCount", "OnlineCount", "Treasury", "BaseLevel",
            "LevelUpCost", "BlacksmithStatus", "ActionStatus", "ActionResult", "LifetimeNotice",
        };

        Scene _previousScene;
        Scene _scene;
        GameObject _root;
        GuildDisplayModel _model;
        GuildWindow _window;
        int _intents;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(Application.isPlaying);
            _previousScene = SceneManager.GetActiveScene();
            _scene = SceneManager.CreateScene("GuildHousingFixture_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_scene);
            _root = new GameObject("GuildHousingTestRoot");
            var eventSystem = new GameObject("GuildHousingEventSystem",
                UiType("UnityEngine.EventSystems.EventSystem"),
                Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", throwOnError: true));
            eventSystem.transform.SetParent(_root.transform, false);
            if (!UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Any(listener => listener.isActiveAndEnabled))
            {
                var listener = new GameObject("GuildHousingOwnedListener", typeof(AudioListener));
                listener.transform.SetParent(_root.transform, false);
            }
            _model = new GuildDisplayModel();
            _window = GuildWindow.BuildRuntime(_root.transform);
            _window.Bind(_model);
            _window.SetOpen(true);
            _window.SetInputAllowed(true);
            _intents = 0;
            _window.LevelUpRequested += () => _intents++;
            yield return Frames();
            yield return ScreenUiProbe.WaitForEventSystem();
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            yield return null;
            if (_previousScene.IsValid() && _previousScene.isLoaded) SceneManager.SetActiveScene(_previousScene);
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "housing fixture unload", 5);
            }
        }

        [UnityTest]
        public IEnumerator BothConfirmedViews_PreserveAspectReadabilityAndRoomForTheBlacksmith()
        {
            BlacksmithWindow blacksmith = BlacksmithWindow.BuildRuntime(_root.transform);
            blacksmith.Bind(_model);
            blacksmith.SetOpen(true);
            foreach (int level in new[] { 1, 2 })
            {
                _model.SetGuild(Confirmed(level));
                yield return Frames();
                Canvas.ForceUpdateCanvases();
                Component scenery = Scenery();
                Sprite sprite = Property<Sprite>(scenery, "sprite");
                Assert.IsTrue(((Behaviour)scenery).enabled);
                Assert.AreSame(Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel" + level), sprite);
                Assert.IsTrue(Property<bool>(scenery, "preserveAspect"));
                Rect drawn = RenderedSceneryBounds(scenery);
                Assert.That(drawn.width / drawn.height, Is.EqualTo(sprite.rect.width / sprite.rect.height).Within(0.005f),
                    "the rendered quad has the source image's aspect, with no stretching or crop");
                AssertInsideScreen(drawn, "foreground");
                Rect panel = ScreenUiProbe.ScreenRect((RectTransform)_window.transform.Find("Guild"));
                Rect smithPanel = ScreenUiProbe.ScreenRect((RectTransform)blacksmith.transform.Find("Blacksmith"));
                AssertInsideScreen(panel, "guild panel");
                AssertInsideScreen(smithPanel, "blacksmith panel");
                Assert.IsFalse(panel.Overlaps(smithPanel), "opening both keeps the other window available");
                Assert.That(panel.xMax, Is.LessThanOrEqualTo(Screen.width * 0.5f + 1f));

                // Art's castle/flags, forge and middle yard lie above the foreground path. This source-relative
                // visibility requirement is independent of the product's pixel coordinates and resize formula.
                Rect buildingsAndYard = Rect.MinMaxRect(drawn.xMin + drawn.width * 0.12f, drawn.yMin + drawn.height * 0.20f,
                    drawn.xMin + drawn.width * 0.94f, drawn.yMin + drawn.height * 0.98f);
                foreach (string band in new[] { "GuildInfoBand", "TreasuryBand", "Inset" })
                {
                    Rect bounds = ScreenUiProbe.ScreenRect((RectTransform)Named(band));
                    Assert.IsFalse(bounds.Overlaps(buildingsAndYard), band + " must leave the buildings and middle yard visible");
                }
                AssertLabelsReadable();
                foreach (string name in new[] { "LevelUp", "Refresh", "Close" })
                {
                    Transform button = Named(name);
                    AssertInsideScreen(ScreenUiProbe.ScreenRect((RectTransform)button), name);
                    Rect label = ScreenUiProbe.ScreenRect(button.GetComponentInChildren<TMP_Text>().rectTransform);
                    Assert.IsFalse(label.Overlaps(buildingsAndYard), name + " does not cover the scenery's buildings");
                }
                Debug.Log($"[guild housing] screen={Screen.width}x{Screen.height}, level={level}, drawn={drawn}, guild={panel}, smith={smithPanel}");
            }
            Assert.AreEqual(0, _intents, "display synchronization does not request growth");
        }

        [UnityTest]
        public IEnumerator Artwork_IsNonInteractiveWhileTheOpenPanelShieldsAndTheClosedPanelReleasesRaycasts()
        {
            _model.SetGuild(Confirmed(1));
            yield return Frames();
            Component scenery = Scenery();
            Assert.IsFalse(Property<bool>(scenery, "raycastTarget"));
            Vector2 point = ScreenUiProbe.ScreenPoint(scenery);
            Assert.IsTrue(ScreenUiProbe.RaycastHits(point).Any(IsGuildObject), "the open background still shields gameplay");
            _window.SetInputAllowed(false);
            ScreenUiProbe.Press(Named("LevelUp").GetComponent(UiType("UnityEngine.UI.Button")));
            Assert.AreEqual(0, _intents);
            Assert.IsTrue(ScreenUiProbe.RaycastHits(point).Any(IsGuildObject), "blocked purchase input keeps background shielding");

            _window.SetOpen(false);
            yield return Frames();
            Assert.IsEmpty(ScreenUiProbe.ShownTextsUnder(_window.transform));
            Assert.IsFalse(ScreenUiProbe.RaycastHits(point).Any(IsGuildObject), "a closed housing view takes no pointer raycasts");
            _window.SetOpen(true);
            _model.SetGuild(null);
            yield return Frames();
            Assert.IsFalse(((Behaviour)Scenery()).enabled, "unsynchronized data has no confirmed scenery");
            Assert.IsNull(Property<Sprite>(Scenery(), "sprite"));
            Assert.IsTrue(ScreenUiProbe.RaycastHits(point).Any(IsGuildObject), "an unsynchronized open view still shields");
        }

        [UnityTest]
        public IEnumerator ReenableAndRebind_UseCurrentConfirmedDataAndKeepLongConnectionFailuresReadable()
        {
            _model.SetGuild(Confirmed(1));
            yield return Frames();
            _window.gameObject.SetActive(false);
            _model.SetGuild(Confirmed(2));
            _window.gameObject.SetActive(true);
            yield return Frames();
            Assert.IsTrue(((Behaviour)Scenery()).enabled, "reenabling uses the current confirmed display");
            Assert.AreSame(Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel2"), Property<Sprite>(Scenery(), "sprite"));
            var replacement = new GuildDisplayModel();
            _window.Bind(replacement);
            _model.SetGuild(Confirmed(1));
            yield return Frames();
            Assert.IsNull(Property<Sprite>(Scenery(), "sprite"), "old-source events cannot repopulate an unsynchronized new binding");
            replacement.SetGuild(Confirmed(2));
            const string status = "서버 동기화 대기 중 · 새로고침으로 조회하세요.";
            const string result = "최근 길드 요청 거부 · 상태가 변경되었습니다. 최신 정보를 확인하세요.";
            MethodInfo connection = typeof(GuildWindow).GetMethod("SetConnectionStatus", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(connection);
            connection.Invoke(_window, new object[] { status, result, true });
            yield return Frames();
            Assert.AreEqual(status, Named("ActionStatus").GetComponent<TMP_Text>().text);
            Assert.AreEqual(result, Named("ActionResult").GetComponent<TMP_Text>().text);
            AssertLabelsReadable();
            Assert.AreEqual(0, _intents);
        }

        static GuildStatusDisplay Confirmed(int level) => level == 1
            ? new GuildStatusDisplay("새벽 길드", 2, 2, 30, 1, 2, false, 30,
                GuildActionAvailability.Available, false, GuildActionResult.None)
            : new GuildStatusDisplay("새벽 길드", 2, 2, 0, 2, 2, true, 0,
                GuildActionAvailability.MaxLevel, false, GuildActionResult.Succeeded);

        static Type UiType(string name) => Type.GetType(name + ", UnityEngine.UI", throwOnError: true);

        static T Property<T>(Component component, string name) => (T)component.GetType().GetProperty(name).GetValue(component);

        static IEnumerator Frames()
        {
            for (int frame = 0; frame < 3; frame++) yield return null;
        }

        static Rect RenderedSceneryBounds(Component scenery)
        {
            CanvasRenderer renderer = scenery.GetComponent<CanvasRenderer>();
            Assert.IsNotNull(renderer);
            Mesh mesh = renderer.GetMesh();
            Assert.IsNotNull(mesh, "a visible confirmed foreground has a rendered mesh");
            Vector3[] vertices = mesh.vertices;
            Assert.IsNotEmpty(vertices);
            Vector3 min = vertices.Aggregate(Vector3.Min);
            Vector3 max = vertices.Aggregate(Vector3.Max);
            Rect bounds = ScreenUiProbe.Bounds(scenery.transform, new Bounds((min + max) * 0.5f, max - min));
            Assert.That(bounds.width, Is.GreaterThan(0f));
            Assert.That(bounds.height, Is.GreaterThan(0f));
            return bounds;
        }

        static void AssertInsideScreen(Rect rect, string name)
        {
            Assert.That(rect.width, Is.GreaterThan(0f), name);
            Assert.That(rect.height, Is.GreaterThan(0f), name);
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-1f), name);
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(-1f), name);
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(Screen.width + 1f), name);
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(Screen.height + 1f), name);
        }

        void AssertLabelsReadable()
        {
            foreach (string name in Labels)
            {
                TMP_Text text = Named(name).GetComponent<TMP_Text>();
                Assert.IsTrue(ScreenUiProbe.IsShown(text), name);
                if (name == "ActionResult" && string.IsNullOrEmpty(text.text)) continue;
                Assert.IsNotEmpty(text.text, name);
                text.ForceMeshUpdate();
                Assert.IsFalse(text.isTextOverflowing, name + ": " + text.text);
                string drawn = new string(text.textInfo.characterInfo.Take(text.textInfo.characterCount)
                    .Select(character => character.character).ToArray());
                Assert.AreEqual(text.text, drawn, name + " renders every supplied character");
                AssertInsideScreen(ScreenUiProbe.ScreenRect(text.rectTransform), name);
            }
        }

        bool IsGuildObject(GameObject item) => item != null && item.transform.IsChildOf(_window.transform);

        Transform Named(string name) => _window.GetComponentsInChildren<Transform>(true).Single(item => item.name == name);

        Component Scenery() => Named("HousingForeground").GetComponent(UiType("UnityEngine.UI.Image"));
    }
}
