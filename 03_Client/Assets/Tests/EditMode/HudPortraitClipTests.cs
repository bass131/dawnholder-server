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
    // A1-A3: real HUD layout, literal square-cell boundaries, and rendered art-on/art-off pixels.
    // EditMode isolates presentation from HudController/network state; no scene is saved.
    public sealed class HudPortraitClipTests
    {
        const string ScenePath = "Assets/Scenes/99.UI/UI.unity";
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
        public void T1_PortraitPlace_Is112SquareCentredInStatusPanel(int index)
        {
            HudLayout hud = Open();
            RectTransform portrait = Portraits(hud)[index];
            SessionTestTools.Call(hud, "Awake");
            Assert.AreEqual("character_status", portrait.parent.name);
            // Contract design (a), A3: reference-canvas coordinates, independent of product layout constants.
            Assert.AreEqual(new Vector2(112f, 112f), portrait.rect.size);
            Assert.AreEqual(new Vector2(22f, -22f), portrait.anchoredPosition);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void T1_ArtIsClippedInsideTheSquareCell_AndFrameIsOutsideEveryClip(int index)
        {
            HudLayout hud = Open();
            RectTransform[] portraits = Portraits(hud);
            RectTransform portrait = portraits[index];
            Sprite head = portrait.GetComponent<Image>().sprite;
            Assert.IsNotNull(head, "fixture: scene portrait sprite");
            SessionTestTools.Call(hud, "Awake");
            foreach (RectTransform other in portraits) other.gameObject.SetActive(other == portrait);
            for (Transform node = portrait; node != null; node = node.parent) node.gameObject.SetActive(true);
            Assert.IsTrue(portrait.gameObject.activeInHierarchy, "fixture: the selected class portrait is active");
            Transform maskTransform = portrait.Find("PortraitClip");
            Assert.IsNotNull(maskTransform, "A2: direct PortraitClip child is missing");
            var mask = maskTransform.GetComponent<RectMask2D>();
            Assert.IsNotNull(mask, "A2: PortraitClip requires RectMask2D");
            Assert.IsTrue(mask.isActiveAndEnabled, "A2: PortraitClip must be enabled");
            Assert.IsNull(maskTransform.GetComponent<Mask>(), "A2: square clip has no stencil Mask");
            RectTransform maskRect = (RectTransform)maskTransform;
            Assert.AreEqual(98f, maskRect.rect.width, 0.5f, "A2: clip width");
            Assert.AreEqual(98f, maskRect.rect.height, 0.5f, "A2: clip height");
            Transform artTransform = maskTransform.Find("PortraitArt");
            Assert.IsNotNull(artTransform, "A2: PortraitArt must be a direct mask child");
            Image art = artTransform.GetComponent<Image>();
            Assert.IsNotNull(art, "A2: PortraitArt has no Image");
            Assert.AreSame(head, art.sprite, "A2: retain scene head sprite");
            Image[] enabledHeads = portrait.GetComponentsInChildren<Image>(true)
                .Where(image => image.enabled && image.sprite == head).ToArray();
            CollectionAssert.AreEqual(new[] { art }, enabledHeads, "A2: only PortraitArt renders the head sprite");
            Transform backdrop = maskTransform.Find("PortraitBackdrop");
            Assert.IsNotNull(backdrop, "A2: PortraitBackdrop must be a direct clip child");
            Assert.AreEqual(0, backdrop.GetSiblingIndex(), "A2: backdrop is first inside the clip");
            Assert.Less(backdrop.GetSiblingIndex(), artTransform.GetSiblingIndex(), "A2: backdrop is behind art");
            Image backdropImage = backdrop.GetComponent<Image>();
            Assert.IsNotNull(backdropImage, "A2: backdrop requires Image");
            Assert.IsNull(backdropImage.sprite, "A2: backdrop is a plain colour");
            Assert.AreEqual(new Color32(61, 44, 32, 255), (Color32)backdropImage.color, "A2: dark cell backdrop");
            Transform frame = portrait.Find("PortraitFrame");
            Assert.IsNotNull(frame, "A2: direct PortraitFrame missing");
            Assert.AreEqual(0, frame.GetSiblingIndex(), "A2: frame is first sibling behind the clipped art");
            for (Transform node = frame; node != null; node = node.parent)
            {
                Assert.IsNull(node.GetComponent<Mask>(), $"A2: frame is clipped by {node.name}");
                Assert.IsNull(node.GetComponent<RectMask2D>(), $"A2: frame is rect-clipped by {node.name}");
            }
            Vector2 centre = portrait.InverseTransformPoint(maskRect.TransformPoint(maskRect.rect.center));
            Assert.LessOrEqual(Vector2.Distance(centre, portrait.rect.center), 1f, "A2: mask and portrait centres");
            Vector3 size = portrait.InverseTransformVector(maskRect.TransformVector(maskRect.rect.size));
            Assert.AreEqual(7f, centre.x - Mathf.Abs(size.x) * 0.5f - portrait.rect.xMin, 0.5f, "A2: left inset");
            Assert.AreEqual(7f, portrait.rect.xMax - centre.x - Mathf.Abs(size.x) * 0.5f, 0.5f, "A2: right inset");
            Assert.AreEqual(7f, centre.y - Mathf.Abs(size.y) * 0.5f - portrait.rect.yMin, 0.5f, "A2: bottom inset");
            Assert.AreEqual(7f, portrait.rect.yMax - centre.y - Mathf.Abs(size.y) * 0.5f, 0.5f, "A2: top inset");
        }

        [TestCase(0, 1280, 720)]
        [TestCase(1, 1280, 720)]
        [TestCase(0, 1920, 1080)]
        [TestCase(1, 1920, 1080)]
        public void T2_HeadChangesZeroPixelsOutsideTheSquareCell(int index, int width, int height)
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
            Vector2 halfCell = (hi - lo) * (98f / 112f) * 0.5f + Vector2.one;
            Rect cellWithTolerance = Rect.MinMaxRect(centre.x - halfCell.x, centre.y - halfCell.y,
                centre.x + halfCell.x, centre.y + halfCell.y);
            int changed = 0;
            int outside = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int offset = y * width + x;
                bool differs = !on[offset].Equals(off[offset]);
                if (!differs) continue;
                changed++;
                if (!cellWithTolerance.Contains(new Vector2(x + 0.5f, y + 0.5f))) outside++;
            }
            TestContext.Out.WriteLine($"portrait={portrait.name} resolution={width}x{height} rect={lo}..{hi} " +
                $"squareCellWith1px={cellWithTolerance} changed={changed} outside={outside}");
            Assert.Greater(changed, 0, "fixture: toggling head art must change rendered pixels (no vacuous pass)");
            Assert.AreEqual(0, outside, "A1: head pixels outside the square cell");
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
