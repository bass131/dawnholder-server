using System;
using System.IO;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Exercise source transfer with non-default inputs so hard-coded white/aspect values cannot pass.
    public sealed class DemoUiPr2VerifyPortraitSourceTests
    {
        const string ScenePath = "Assets/Scenes/99.UI/UI.unity";
        Scene _scene;
        Scene _previous;
        bool _single;
        byte[] _sceneBytes;

        [SetUp]
        public void Setup()
        {
            _sceneBytes = File.ReadAllBytes(ScenePath);
            _previous = SceneManager.GetActiveScene();
            _single = false;
            try
            {
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine("Additive open unavailable: " + error.Message);
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                _single = true;
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (_single)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
                if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
            }
            CollectionAssert.AreEqual(_sceneBytes, File.ReadAllBytes(ScenePath), "scene was not saved");
        }

        [TestCase("status_icon1", false)]
        [TestCase("status_icon1", true)]
        [TestCase("status_icon2", false)]
        [TestCase("status_icon2", true)]
        public void Awake_PreservesSourceAppearance_AndDecorationsDoNotInterceptInput(string name, bool aspect)
        {
            HudLayout hud = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).Single();
            Transform portrait = hud.transform.Find("character_status/" + name);
            Assert.IsNotNull(portrait, "actual authored portrait");
            Image source = portrait.GetComponent<Image>();
            Sprite sprite = source.sprite;
            Assert.IsNotNull(sprite, "authored source sprite");
            Color tint = new Color32(113, 171, 219, 203);
            source.color = tint;
            source.preserveAspect = aspect;

            SessionTestTools.Call(hud, "Awake");

            Image art = portrait.Find("PortraitClip/PortraitArt").GetComponent<Image>();
            Assert.AreSame(sprite, art.sprite, "head keeps the source asset");
            Assert.AreSame(sprite, source.sprite, "disabled source remains inspectable");
            Assert.AreEqual(tint, art.color, "head keeps source tint including alpha");
            Assert.AreEqual(aspect, art.preserveAspect, "head keeps source aspect setting");
            Assert.IsFalse(source.enabled, "source cannot draw an unclipped duplicate");
            Assert.IsTrue(art.enabled, "the clipped replacement is drawable");

            string[] decorationPaths = { "PortraitFrame", "PortraitClip/PortraitBackdrop", "PortraitClip/PortraitArt" };
            foreach (string path in decorationPaths)
            {
                Image decoration = portrait.Find(path).GetComponent<Image>();
                Assert.IsFalse(decoration.raycastTarget, path + " must not intercept HUD input");
            }
            Assert.IsEmpty(portrait.GetComponentsInChildren<Mask>(true), "no stencil mask survives under the portrait");
            TestContext.Out.WriteLine($"source={name} preserveAspect={aspect} tint={art.color} " +
                $"sprite={sprite.name} raycastDecorations=0 sourceEnabled={source.enabled}");
        }
    }
}
