using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // A1-A3: real HUD layout, independent PNG alpha measurements, and rendered art-on/art-off pixels.
    // EditMode isolates presentation from HudController/network state; no scene is saved.
    public sealed class HudPortraitClipTests
    {
        const string ScenePath = "Assets/Scenes/99.UI/UI.unity";
        const string FramePath = "Assets/Art/UI/BrightComponents/PortraitFrame_128.png";
        readonly List<UnityEngine.Object> _owned = new();
        Scene _scene;
        Scene _previous;
        bool _single;

        HudLayout Open()
        {
            _previous = SceneManager.GetActiveScene();
            try { _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive); }
            catch (Exception error)
            {
                TestContext.Out.WriteLine($"Additive open unavailable: {error.Message}; single scene fixture");
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                _single = true;
            }
            return _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).Single();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
            if (_single) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
                if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
            }
            _single = false;
        }

        static RectTransform[] Portraits(HudLayout hud)
        {
            SerializedProperty list = new SerializedObject(hud).FindProperty("_portraits");
            Assert.IsNotNull(list, "fixture: serialized HUD portraits");
            var portraits = Enumerable.Range(0, list.arraySize)
                .Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform).ToArray();
            Assert.AreEqual(2, portraits.Length, "A3: knight and mage portraits");
            CollectionAssert.AreEquivalent(new[] { "status_icon1", "status_icon2" }, portraits.Select(p => p.name));
            return portraits;
        }

        [TestCase(0)]
        [TestCase(1)]
        public void T1_PortraitPlace_Remains120SquareAtStatusInset(int index)
        {
            HudLayout hud = Open();
            RectTransform portrait = Portraits(hud)[index];
            SessionTestTools.Call(hud, "Awake");
            Assert.AreEqual("character_status", portrait.parent.name);
            // Contract design (a), A3: reference-canvas coordinates, independent of product layout constants.
            Assert.AreEqual(new Vector2(120f, 120f), portrait.rect.size);
            Assert.AreEqual(new Vector2(16f, -16f), portrait.anchoredPosition);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void T1_ArtIsClippedInsideTheRing_AndFrameIsOutsideEveryMask(int index)
        {
            HudLayout hud = Open();
            RectTransform portrait = Portraits(hud)[index];
            Sprite head = portrait.GetComponent<Image>().sprite;
            Assert.IsNotNull(head, "fixture: scene portrait sprite");
            SessionTestTools.Call(hud, "Awake");
            Transform maskTransform = portrait.Find("PortraitMask");
            Assert.IsNotNull(maskTransform, "A2: direct PortraitMask child is missing");
            var mask = maskTransform.GetComponent<Mask>();
            Assert.IsNotNull(mask, "A2: PortraitMask requires UI.Mask");
            Assert.IsNotNull(maskTransform.GetComponent<Graphic>(), "A2: mask has no Graphic");
            Assert.IsFalse(mask.showMaskGraphic, "A2: mask graphic must not draw over art");
            Transform artTransform = maskTransform.Find("PortraitArt");
            Assert.IsNotNull(artTransform, "A2: PortraitArt must be a direct mask child");
            Image art = artTransform.GetComponent<Image>();
            Assert.IsNotNull(art, "A2: PortraitArt has no Image");
            Assert.AreSame(head, art.sprite, "A2: retain scene head sprite");
            Image[] enabledHeads = portrait.GetComponentsInChildren<Image>(true)
                .Where(image => image.enabled && image.sprite == head).ToArray();
            CollectionAssert.AreEqual(new[] { art }, enabledHeads, "A2: only PortraitArt renders the head sprite");
            Transform frame = portrait.Find("PortraitFrame");
            Assert.IsNotNull(frame, "A2: direct PortraitFrame missing");
            Assert.AreEqual(portrait.childCount - 1, frame.GetSiblingIndex(), "A2: frame is last sibling");
            for (Transform node = frame; node != null; node = node.parent)
                Assert.IsNull(node.GetComponent<Mask>(), $"A2: frame is clipped by {node.name}");
            RectTransform maskRect = (RectTransform)maskTransform;
            Vector2 centre = portrait.InverseTransformPoint(maskRect.TransformPoint(maskRect.rect.center));
            Assert.LessOrEqual(Vector2.Distance(centre, portrait.rect.center), 1f, "A2: mask and portrait centres");
            (float inner, _) = FrameRadii();
            Vector3 size = portrait.InverseTransformVector(maskRect.TransformVector(maskRect.rect.size));
            Assert.LessOrEqual(Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y)), inner * 2f * 120f,
                "A2: mask diameter must fit inside PNG alpha's inner ring");
        }

        // Read the source PNG, not imported unreadable texture data or a product radius helper.
        (float Inner, float Outer) FrameRadii()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _owned.Add(texture);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(FramePath)), "fixture: source frame PNG loads");
            Assert.AreEqual(texture.width, texture.height, "fixture: square source frame");
            Color32[] pixels = texture.GetPixels32();
            float centre = (texture.width - 1) * 0.5f;
            var inner = new List<float>();
            var outer = new List<float>();
            foreach (Vector2 direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                var opaque = new List<float>();
                for (int r = 0; r < texture.width / 2; r++)
                {
                    int x = Mathf.RoundToInt(centre + direction.x * r);
                    int y = Mathf.RoundToInt(centre + direction.y * r);
                    if (pixels[y * texture.width + x].a >= 128) opaque.Add(r);
                }
                Assert.IsNotEmpty(opaque, "fixture: each cardinal ray crosses the ring");
                inner.Add(opaque.Min());
                outer.Add(opaque.Max() + 1f); // Include the outermost opaque pixel's extent.
            }
            TestContext.Out.WriteLine($"Frame PNG alpha>=128 inner radii={string.Join(",", inner)} outer={string.Join(",", outer)}");
            return (inner.Min() / texture.width, outer.Max() / texture.width);
        }

        [TestCase(0, 1280, 720)]
        [TestCase(1, 1280, 720)]
        [TestCase(0, 1920, 1080)]
        [TestCase(1, 1920, 1080)]
        public void T2_HeadChangesZeroPixelsOutsideTheGoldRing(int index, int width, int height)
        {
            HudLayout hud = Open();
            RectTransform[] portraits = Portraits(hud);
            RectTransform portrait = portraits[index];
            Sprite head = portrait.GetComponent<Image>().sprite;
            SessionTestTools.Call(hud, "Awake");
            foreach (RectTransform other in portraits) other.gameObject.SetActive(other == portrait);
            for (Transform node = portrait; node != null; node = node.parent) node.gameObject.SetActive(true);
            Image[] art = portrait.GetComponentsInChildren<Image>(true)
                .Where(image => image.enabled && image.sprite == head).ToArray();
            Assert.IsNotEmpty(art, "fixture: enabled head Image before on/off rendering");
            (_, float outer) = FrameRadii();

            var cameraObject = new GameObject("Portrait capture camera (fixture)");
            _owned.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.2f, 0.16f, 1f);
            camera.allowHDR = camera.allowMSAA = false;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _owned.Add(target);
            target.Create();
            camera.targetTexture = target;
            var readback = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            _owned.Add(readback);
            Canvas canvas = portrait.GetComponentInParent<Canvas>().rootCanvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            foreach (CanvasScaler scaler in canvas.GetComponentsInChildren<CanvasScaler>(true))
                SessionTestTools.Call(scaler, "Handle");
            Canvas.ForceUpdateCanvases();
            TestContext.Out.WriteLine("S6 resolution target=(" + ((float)width).ToString("R", CultureInfo.InvariantCulture) + "," + ((float)height).ToString("R", CultureInfo.InvariantCulture) + "), actual=(" + canvas.renderingDisplaySize.x.ToString("R", CultureInfo.InvariantCulture) + "," + canvas.renderingDisplaySize.y.ToString("R", CultureInfo.InvariantCulture) + ")");
            Assert.IsTrue(Mathf.Abs(canvas.renderingDisplaySize.x - width) < 0.5f && Mathf.Abs(canvas.renderingDisplaySize.y - height) < 0.5f, "fixture: target resolution observed: target=(" + ((float)width).ToString("R", CultureInfo.InvariantCulture) + "," + ((float)height).ToString("R", CultureInfo.InvariantCulture) + "), actual=(" + canvas.renderingDisplaySize.x.ToString("R", CultureInfo.InvariantCulture) + "," + canvas.renderingDisplaySize.y.ToString("R", CultureInfo.InvariantCulture) + ")");
            Color32[] on = Capture(camera, target, readback);
            string directory = Environment.GetEnvironmentVariable("DAWNHOLDER_HUD_PORTRAIT_CAPTURE_DIR");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                using var output = new FileStream(Path.Combine(directory,
                    $"portrait-{(index == 0 ? "knight" : "mage")}-{width}x{height}.png"), FileMode.CreateNew);
                byte[] png = readback.EncodeToPNG();
                output.Write(png, 0, png.Length);
            }
            foreach (Image image in art) image.enabled = false;
            Color32[] off = Capture(camera, target, readback);
            var corners = new Vector3[4];
            portrait.GetWorldCorners(corners);
            Vector2 lo = camera.WorldToScreenPoint(corners[0]);
            Vector2 hi = camera.WorldToScreenPoint(corners[2]);
            Vector2 centre = (lo + hi) * 0.5f;
            float radius = outer * Mathf.Min(hi.x - lo.x, hi.y - lo.y) + 1f;
            int changed = 0;
            int outside = 0;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(lo.y)); y < Mathf.Min(height, Mathf.CeilToInt(hi.y)); y++)
            for (int x = Mathf.Max(0, Mathf.FloorToInt(lo.x)); x < Mathf.Min(width, Mathf.CeilToInt(hi.x)); x++)
            {
                int offset = y * width + x;
                bool differs = !on[offset].Equals(off[offset]);
                if (!differs) continue;
                changed++;
                if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) > radius) outside++;
            }
            TestContext.Out.WriteLine($"portrait={portrait.name} resolution={width}x{height} rect={lo}..{hi} " +
                $"outerRadiusWith1px={radius} changed={changed} outside={outside}");
            Assert.Greater(changed, 0, "fixture: toggling head art must change rendered pixels (no vacuous pass)");
            Assert.AreEqual(0, outside, "A1: head pixels outside gold ring");
            camera.targetTexture = null;
            target.Release();
        }

        static Color32[] Capture(Camera camera, RenderTexture target, Texture2D readback)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                readback.Apply(false);
                return readback.GetPixels32();
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
