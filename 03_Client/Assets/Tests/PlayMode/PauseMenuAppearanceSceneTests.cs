using System;
using System.Collections;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dawnholder.Client.Tests.PlayMode
{
    // Observe the bright menu on its real input path. The loopback peer supplies map readiness;
    // Escape and Resume still pass through the product's Input System and EventSystem handlers.
    public sealed class PauseMenuAppearanceSceneTests
    {
        const string BrightFolder = "Assets/Art/UI/BrightComponents/";
        static readonly string[] s_buttonNames = { "ResumeButton", "MainMenuButton", "QuitButton" };
        MapEntryPlayFixture _fixture;

        [SetUp]
        public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest, Timeout(120000)]
        public IEnumerator EscapeShowsBrightMenu_AndResumePointerRestoresGameplay()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return ScreenUiProbe.WaitForEventSystem();

            PauseMenuController controller = null;
            yield return MapEntryPlayFixture.Wait(() =>
            {
                controller = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
                return controller != null && controller.isActiveAndEnabled;
            }, "the real additive UI pause controller", 10);
            GameObject menu = MapEntryPlayFixture.Field<GameObject>(controller, "_pauseCanvas");
            Assert.IsNotNull(menu, "the real controller owns a menu");
            Assert.IsFalse(menu.activeInHierarchy, "the menu starts closed");

            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => menu.activeInHierarchy && Time.timeScale == 0f,
                "Escape displays the menu and pauses gameplay", 3);
            _fixture.Keys();
            yield return _fixture.Frames(3);
            Canvas.ForceUpdateCanvases();

            Transform panel = menu.transform.Find("MenuPanel");
            Assert.IsNotNull(panel, "the controller opens the scene's MenuPanel");
            Component panelImage = ImageOn(panel);
            AssertVisibleBrightImage(panelImage, "Panel_Default");
            RejectMissingSprite(panelImage, "Panel_Default");

            foreach (string name in s_buttonNames)
            {
                Transform button = panel.Find(name);
                Assert.IsNotNull(button, "the unchanged menu button exists: " + name);
                Component image = ImageOn(button);
                AssertVisibleBrightImage(image, "Button_Default_Normal");
                RejectMissingSprite(image, "Button_Default_Normal");
            }

            Transform resume = panel.Find("ResumeButton");
            Vector2 point = ScreenUiProbe.ScreenPoint(resume);
            Assert.IsTrue(ScreenUiProbe.OnScreen(point), "the real Resume centre lies on screen");
            GameObject[] hits = ScreenUiProbe.RaycastHits(point);
            AssertPanelOutsideRaycast(panelImage, hits);
            Assert.IsTrue(hits.Any(hit => hit.transform == resume || hit.transform.IsChildOf(resume)),
                "the EventSystem actually hits Resume at its centre");

            // A real raycast through a deliberately blocking panel must fail the same observer.
            var raycastProperty = panelImage.GetType().GetProperty("raycastTarget");
            object originalRaycast = raycastProperty.GetValue(panelImage);
            try
            {
                raycastProperty.SetValue(panelImage, true);
                GameObject[] blockedHits = ScreenUiProbe.RaycastHits(point);
                AssertionException rejected = Assert.Throws<AssertionException>(() =>
                    AssertPanelOutsideRaycast(panelImage, blockedHits));
                TestContext.Out.WriteLine("negative-control panel-raycast: " + rejected.Message);
            }
            finally
            {
                raycastProperty.SetValue(panelImage, originalRaycast);
            }
            AssertPanelOutsideRaycast(panelImage, ScreenUiProbe.RaycastHits(point));

            TestContext.Out.WriteLine("actual-entry: scripted Town -> additive UI -> Escape -> bright menu; "
                + "Resume screen=" + point + "; hits=" + string.Join(",", hits.Select(hit => hit.name)));
            yield return InventoryPanelProbe.ClickAt(_fixture.Mouse, point);
            yield return MapEntryPlayFixture.Wait(() => !menu.activeInHierarchy && Time.timeScale == 1f,
                "the real Resume pointer handler closes the menu and resumes gameplay", 3);
            TestContext.Out.WriteLine("actual-output: menu active=" + menu.activeInHierarchy + "; timeScale=" + Time.timeScale);
        }

        static Component ImageOn(Transform owner)
        {
            Component image = owner.GetComponent(PartyInviteLayout.UiType("Image"));
            Assert.IsNotNull(image, "the scene object has its Image: " + owner.name);
            return image;
        }

        static void AssertVisibleBrightImage(Component image, string expectedName)
        {
            Assert.IsTrue(ScreenUiProbe.IsShown(image), "the Image is live and visible: " + image.name);
            Sprite sprite = PartyInviteLayout.SpriteOf(image);
            Assert.IsNotNull(sprite, "the visible Image must draw its bright sprite: " + image.name);
#if UNITY_EDITOR
            Assert.AreEqual(BrightFolder + expectedName + ".png", AssetDatabase.GetAssetPath(sprite),
                "the live Image uses the approved bright piece: " + image.name);
#else
            Assert.Fail("This asset identity observer requires the Editor test runner.");
#endif
            TestContext.Out.WriteLine("live-piece: " + image.name + " -> " + sprite.name);
        }

        static void RejectMissingSprite(Component image, string expectedName)
        {
            var property = image.GetType().GetProperty("sprite");
            object original = property.GetValue(image);
            try
            {
                property.SetValue(image, null);
                AssertionException rejected = Assert.Throws<AssertionException>(() =>
                    AssertVisibleBrightImage(image, expectedName));
                TestContext.Out.WriteLine("negative-control missing-sprite " + image.name + ": " + rejected.Message);
            }
            finally
            {
                property.SetValue(image, original);
            }
            AssertVisibleBrightImage(image, expectedName);
        }

        static void AssertPanelOutsideRaycast(Component panelImage, GameObject[] hits)
        {
            bool receivesRaycast = (bool)PartyInviteLayout.Get(panelImage, "raycastTarget");
            bool appearsInHits = hits.Contains(panelImage.gameObject);
            Assert.IsFalse(receivesRaycast, "the decorative MenuPanel has raycastTarget disabled");
            Assert.IsFalse(appearsInHits, "the decorative MenuPanel is absent from actual pointer hits");
        }
    }
}
