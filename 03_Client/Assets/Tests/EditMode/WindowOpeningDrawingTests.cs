using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Observe the real view at several times; Resize supplies the placement baseline, not a copied layout equation.
    public sealed class WindowOpeningDrawingTests
    {
        GameObject _root, _cameraObject;
        InventoryPanelView _view;
        RenderTexture _target;
        Texture2D _pixels;

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_cameraObject != null) UnityEngine.Object.DestroyImmediate(_cameraObject);
            if (_target != null) { _target.Release(); UnityEngine.Object.DestroyImmediate(_target); }
            if (_pixels != null) UnityEngine.Object.DestroyImmediate(_pixels);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpeningFrames_PreserveEveryExistingRectangle_AndOnlyTheDrawingChanges(bool interactive)
        {
            _root = new GameObject("Window drawing observation", typeof(RectTransform));
            _view = new InventoryPanelView(_root, _ => { }, () => { });
            _view.Render(null, null, false);
            Camera camera = AttachCamera();
            _view.SetVisibility(false, false);
            _view.Resize();
            foreach (TMP_Text text in _root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)_root.transform.Find("Inventory");
            RectTransform[] rects = _root.GetComponentsInChildren<RectTransform>(true);
            RectangleState[] placed = rects.Select(rect => new RectangleState(rect)).ToArray();
            Assert.Greater(rects.Length, 60, "observer includes the panel, slots, pictures, buttons and text");
            // A mutation of a real observed child must be rejected, then restored before the product runs.
            Vector3 oldPosition = rects[1].anchoredPosition3D;
            rects[1].anchoredPosition3D += Vector3.right;
            NegativeControl(() => RequirePlacement(placed, rects));
            rects[1].anchoredPosition3D = oldPosition;
            NegativeControl(() => RequireOwner(1f, !interactive, interactive, interactive));
            NegativeControl(() => RequireDrawing(0, true, 0f));

            _view.SetVisibility(true, interactive);
            var effect = panel.GetComponent<UiWindowOpenEffect>();
            var owner = _root.GetComponent<CanvasGroup>();
            var fade = panel.GetComponent<CanvasGroup>();
            float previousScale = 0f, previousAlpha = -1f;
            foreach (float elapsed in new[] { 0f, 0.04f, 0.08f })
            {
                typeof(UiWindowOpenEffect).GetField("_startTime", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(effect, Time.unscaledTime - elapsed);
                typeof(UiWindowOpenEffect).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(effect, null);
                Canvas.ForceUpdateCanvases();
                RequirePlacement(placed, rects);
                RequireOwner(owner.alpha, owner.interactable, owner.blocksRaycasts, interactive);
                Transform frame = panel.Find("OpeningFrame");
                Assert.IsNotNull(frame, "active effect creates actual drawing");
                Assert.AreEqual(1, panel.Cast<Transform>().Count(child => child.name == "OpeningFrame"));
                var image = frame.GetComponent<Image>();
                // CanvasRenderer owns this mesh; observation must not destroy or rewrite it.
                Mesh mesh = image.canvasRenderer.GetMesh();
                Assert.IsNotNull(mesh, "actual canvas renderer has a mesh");
                RequireDrawing(mesh.vertexCount, image.raycastTarget, image.color.a);
                TestContext.Out.WriteLine($"elapsed={elapsed:F2}; rects={rects.Length}; frame vertices={mesh.vertexCount}; frame scale={frame.localScale}; fade={fade.alpha}; root={owner.alpha}/{owner.interactable}/{owner.blocksRaycasts}");
                Assert.That(frame.localScale.x, Is.GreaterThan(previousScale).And.LessThan(1f));
                Assert.That(fade.alpha, Is.GreaterThan(previousAlpha).And.LessThan(1f));
                Assert.IsFalse(frame.GetComponent<CanvasGroup>().blocksRaycasts);
                previousScale = frame.localScale.x;
                previousAlpha = fade.alpha;
                Capture(camera, interactive, elapsed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            }

            typeof(UiWindowOpenEffect).GetField("_startTime", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(effect, Time.unscaledTime - 1f);
            typeof(UiWindowOpenEffect).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(effect, null);
            RequirePlacement(placed, rects);
            RequireOwner(owner.alpha, owner.interactable, owner.blocksRaycasts, interactive);
            Assert.IsFalse(effect.enabled);
            Assert.AreEqual(1f, fade.alpha);
            Assert.IsNull(panel.Find("OpeningFrame"), "completed drawing is removed");
            Capture(camera, interactive, "finished");
        }

        Camera AttachCamera()
        {
            _cameraObject = new GameObject("Window drawing camera") { hideFlags = HideFlags.DontSave };
            var camera = _cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.2f, 0.3f);
            camera.orthographic = true;
            camera.allowHDR = camera.allowMSAA = false;
            _target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            camera.targetTexture = _target;
            _pixels = new Texture2D(1280, 720, TextureFormat.RGBA32, false, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            Canvas.ForceUpdateCanvases();
            SessionTestTools.Call(_root.GetComponent<CanvasScaler>(), "Handle");
            Canvas.ForceUpdateCanvases();
            return camera;
        }

        void Capture(Camera camera, bool interactive, string state)
        {
            string directory = Environment.GetEnvironmentVariable("DAWNHOLDER_UI_KIT_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(directory)) return;
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase));
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0, false);
                _pixels.Apply(false);
            }
            finally { RenderTexture.active = previous; }
            string file = Path.Combine(full, $"opening-{interactive}-{state}.png");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] png = _pixels.EncodeToPNG();
                stream.Write(png, 0, png.Length);
            }
            TestContext.Out.WriteLine("Rendered opening capture: " + file);
        }

        readonly struct RectangleState
        {
            readonly Transform _parent;
            readonly int _sibling;
            readonly Vector3 _position, _scale;
            readonly Quaternion _rotation;
            readonly Vector2 _min, _max, _pivot, _size;
            public RectangleState(RectTransform rect)
            {
                _parent = rect.parent; _sibling = rect.GetSiblingIndex();
                _position = rect.anchoredPosition3D; _scale = rect.localScale; _rotation = rect.localRotation;
                _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot; _size = rect.sizeDelta;
            }
            public void RequireSame(RectTransform rect)
            {
                Assert.AreEqual(_parent, rect.parent, rect.name + " parent");
                Assert.AreEqual(_sibling, rect.GetSiblingIndex(), rect.name + " sibling");
                Assert.AreEqual(_position, rect.anchoredPosition3D, rect.name + " position");
                Assert.AreEqual(_scale, rect.localScale, rect.name + " scale");
                Assert.AreEqual(_rotation, rect.localRotation, rect.name + " rotation");
                Assert.AreEqual(_min, rect.anchorMin, rect.name + " anchorMin");
                Assert.AreEqual(_max, rect.anchorMax, rect.name + " anchorMax");
                Assert.AreEqual(_pivot, rect.pivot, rect.name + " pivot");
                Assert.AreEqual(_size, rect.sizeDelta, rect.name + " sizeDelta");
            }
        }
        static void RequirePlacement(RectangleState[] expected, RectTransform[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int index = 0; index < expected.Length; index++) expected[index].RequireSame(actual[index]);
        }
        static void RequireOwner(float alpha, bool interactive, bool raycasts, bool expectedInteractive)
        {
            Assert.AreEqual(1f, alpha);
            Assert.AreEqual(expectedInteractive, interactive);
            Assert.AreEqual(expectedInteractive, raycasts);
        }
        static void RequireDrawing(int vertices, bool raycasts, float alpha)
        {
            Assert.Greater(vertices, 0, "the outline has renderable vertices");
            Assert.IsFalse(raycasts, "drawing does not own input");
            Assert.Greater(alpha, 0f, "drawing is visible");
        }
        static void NegativeControl(TestDelegate wrongObservation)
        {
            var rejection = Assert.Throws<AssertionException>(wrongObservation);
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + rejection.Message);
        }
    }
}
