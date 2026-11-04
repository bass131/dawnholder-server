using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;
using Slot = Dawnholder.Client.UI.CharacterMenuViewState.Slot;

namespace Dawnholder.Client.Tests
{
    // Layout of the three-slot character select, written before the implementation (goal
    // 2026-10-31-demo-autopilot-character-select, PR-A completion row A2). The inner width is Panel/Header's width
    // (pr-a-red-contract-v2 「설계 고정」 「안쪽 폭」). The slot labels with 12-character Korean names and the create form's
    // NameRule, Create and Cancel texts must fit at 1920×1080 and 1280×720.
    // Resolution fixture as in CharacterMenuViewCaptureTests: the product's Overlay canvas is switched to
    // ScreenSpaceCamera on a camera drawing into a render texture of the target size, so its own CanvasScaler sees that
    // size. These are not the game screen and say nothing about pointer input, server replies or scene entry.
    // PNGs are written only when DAWNHOLDER_CHARACTER_SELECT_CAPTURE_DIR is set, outside the Unity project.
    public sealed class CharacterSelectWideLayoutTests
    {
        const string CaptureDirectoryVariable = "DAWNHOLDER_CHARACTER_SELECT_CAPTURE_DIR";
        // 설계 고정 「안쪽 폭」 and goal A2: the three slots run over at least 90% of the inner width.
        const float MinimumSpanRatio = 0.9f;
        // UI units; absorbs float layout without hiding a real overlap (CharacterMenuViewCaptureTests).
        const float Tolerance = 0.5f;
        // Contract T2: names at the longest the server allows, 12 Korean characters (Core contract v1.1, 2~12자).
        const string LongNameA = "가나다라마바사아자차카타";
        const string LongNameB = "하늘바다구름별빛노을새벽";
        const string LongNameC = "푸른숲속작은마을의기사단";
        const int LongestName = 12;
        static readonly string[] s_slotNames = { "Slot1", "Slot2", "Slot3" };
        static readonly Color s_background = new(0.08f, 0.09f, 0.11f, 1f);

        readonly List<string> _missing = new();
        readonly List<string> _unfit = new();
        readonly List<string> _fixture = new();
        GameObject _root;
        GameObject _cameraObject;
        CharacterMenuView _view;
        RenderTexture _target;
        Texture2D _readback;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _missing.Clear();
            _unfit.Clear();
            _fixture.Clear();
            _root = new GameObject("Character select wide layout under test", typeof(RectTransform));
            _view = new CharacterMenuView(_root, _ => { }, (_, _, _) => { }, () => { }, () => { });
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_cameraObject != null) UnityEngine.Object.DestroyImmediate(_cameraObject);
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.DestroyImmediate(_target);
            }
            if (_readback != null) UnityEngine.Object.DestroyImmediate(_readback);
        }

        // T2 (A2): the three slot rectangles do not overlap and, from the leftmost to the rightmost edge, cover at least
        // 90% of Panel/Header's width. Both are measured from world corners in Panel's local units.
        [Test]
        public void ThreeSlots_SpanNinetyPercentOfTheHeaderWidth_WithoutOverlap()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot(LongNameA, 0), new Slot(LongNameB, 1)));
            ForceLayout();

            Rect header = InPanel((RectTransform)Panel.Find("Header"));
            Rect[] slots = s_slotNames.Select(name => InPanel((RectTransform)Panel.Find(name))).ToArray();
            float span = slots.Max(rect => rect.xMax) - slots.Min(rect => rect.xMin);
            float ratio = span / header.width;
            var overlaps = new List<string>();
            for (int i = 0; i < slots.Length; i++)
                for (int j = i + 1; j < slots.Length; j++)
                    if (Overlaps(slots[i], slots[j])) overlaps.Add($"{s_slotNames[i]} {slots[i]} and {s_slotNames[j]} {slots[j]}");
            TestContext.Out.WriteLine($"header {header}; slots {string.Join("; ", slots)}; span {span:F1}; ratio {ratio:F3}");

            Assert.Greater(header.width, 0f, "precondition: Panel/Header has a width");
            Assert.GreaterOrEqual(ratio, MinimumSpanRatio,
                $"A2 the slots span {span:F1} of the {header.width:F1} inner width ({ratio:P1}), at least 90%");
            Assert.IsEmpty(overlaps, "A2 the three slot rectangles do not overlap");
        }

        // T2 (A2): at each goal resolution the slot labels (12-character names with their class, unchosen and chosen)
        // and the create form's NameRule, Create and Cancel texts neither overflow nor lose characters, and draw inside
        // their own rect. Captures: empty list, two characters with one chosen, the create form.
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void SlotLabelsAndCreateFormTexts_FitAtTheGoalResolutions(int width, int height)
        {
            Assert.AreEqual(LongestName, LongNameA.Length, "fixture: name A has 12 characters");
            Assert.AreEqual(LongestName, LongNameB.Length, "fixture: name B has 12 characters");
            Assert.AreEqual(LongestName, LongNameC.Length, "fixture: name C has 12 characters");
            Camera camera = AttachCamera(width, height);
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
            var knight = new Slot(LongNameA, 0);
            var mage = new Slot(LongNameB, 1);
            var thirdKnight = new Slot(LongNameC, 0);

            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Capture(camera, directory, "select-empty");

            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, mage, thirdKnight));
            CheckSlotLabels(new[] { knight, mage, thirdKnight }, "three names");
            Click("Slot2");
            CheckSlotLabels(new[] { knight, mage, thirdKnight }, "three names, slot 2 chosen");

            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, mage));
            Click("Slot1");
            CheckSlotLabels(new[] { knight, mage, null }, "two names, slot 1 chosen");
            Capture(camera, directory, "select-chosen");

            Click("Slot3");
            Transform form = Panel.Find("CreateForm");
            if (form == null || !form.gameObject.activeInHierarchy) _missing.Add("CreateForm is not open after the empty slot 3 click");
            CheckFormText("CreateForm/NameRule");
            CheckFormButton("CreateForm/Create");
            CheckFormButton("CreateForm/Cancel");
            Capture(camera, directory, "select-create");

            string label = $"{width}x{height}";
            TestContext.Out.WriteLine($"[character select wide] {label} missing: {string.Join("; ", _missing)}");
            TestContext.Out.WriteLine($"[character select wide] {label} unfit: {string.Join("; ", _unfit)}");
            Assert.IsEmpty(_fixture, $"fixture: {label} captures drew the menu");
            Assert.IsEmpty(_missing, $"exists: {label} the create form and its NameRule, Create and Cancel texts");
            Assert.IsEmpty(_unfit, $"A2 {label} every checked text is whole and inside its rect");
        }

        void CheckSlotLabels(Slot[] characters, string state)
        {
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < s_slotNames.Length; i++)
            {
                string path = s_slotNames[i] + "/Label";
                TMP_Text label = TextAt(path);
                if (label == null)
                {
                    _missing.Add($"{state}: no TMP_Text on Panel/{path}");
                    continue;
                }
                Slot character = characters[i];
                if (character != null)
                {
                    string className = character.ClassId == 0 ? "Knight" : "Mage";
                    if (!label.text.Contains(character.Name) || !label.text.Contains(className))
                        _unfit.Add($"{state}: {path} \"{label.text}\" does not show {character.Name} and {className}");
                }
                CheckFits(label, $"{state}: {path}");
            }
        }

        void CheckFormText(string path)
        {
            TMP_Text text = TextAt(path);
            if (text == null)
            {
                _missing.Add($"no TMP_Text on Panel/{path}");
                return;
            }
            Canvas.ForceUpdateCanvases();
            CheckFits(text, path);
        }

        void CheckFormButton(string path)
        {
            Transform node = Panel.Find(path);
            TMP_Text[] texts = node == null ? Array.Empty<TMP_Text>() : node.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
            {
                _missing.Add($"no button text under Panel/{path}");
                return;
            }
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in texts) CheckFits(text, path + "/" + text.name);
        }

        // Observed TMP values only: overflow, dropped characters, and drawn glyphs outside the text's own rect.
        void CheckFits(TMP_Text text, string label)
        {
            text.ForceMeshUpdate();
            if (text.isTextOverflowing) _unfit.Add($"{label} overflows: \"{text.text}\"");
            if (text.isTextTruncated) _unfit.Add($"{label} lost characters of \"{text.text}\"");
            if (!TryGlyphArea(text, out Vector2 min, out Vector2 max)) return;
            Rect own = text.rectTransform.rect;
            if (min.x < own.xMin - Tolerance || max.x > own.xMax + Tolerance ||
                min.y < own.yMin - Tolerance || max.y > own.yMax + Tolerance)
                _unfit.Add($"{label} draws {min}..{max} outside its rect {own}");
        }

        Camera AttachCamera(int width, int height)
        {
            _cameraObject = new GameObject("Capture camera (fixture)") { hideFlags = HideFlags.DontSave };
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

            Canvas canvas = _root.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            // Edit mode runs no CanvasScaler.Update; Handle applies the product's own scaler to the new surface.
            Canvas.ForceUpdateCanvases();
            SessionTestTools.Call(canvas.GetComponent<CanvasScaler>(), "Handle");
            Canvas.ForceUpdateCanvases();
            Vector2 seen = canvas.renderingDisplaySize;
            Vector2 units = ((RectTransform)canvas.transform).rect.size;
            TestContext.Out.WriteLine($"[character select wide] {width}x{height}: renderingDisplaySize {seen} " +
                $"scaleFactor {canvas.scaleFactor:F4} canvas {units}");
            Assert.IsTrue(Mathf.Abs(seen.x - width) < 0.5f && Mathf.Abs(seen.y - height) < 0.5f,
                $"fixture: the scaler sees a {seen} screen, the capture is {width}x{height}");
            Assert.IsTrue(Mathf.Abs(units.x * canvas.scaleFactor - width) < 1f && Mathf.Abs(units.y * canvas.scaleFactor - height) < 1f,
                $"fixture: the canvas {units} at scale {canvas.scaleFactor} fills the {width}x{height} capture");
            return camera;
        }

        void Capture(Camera camera, string directory, string state)
        {
            string label = $"{_target.width}x{_target.height} {state}";
            if (string.IsNullOrEmpty(directory))
            {
                TestContext.Out.WriteLine($"[character select wide] {label}: no capture directory");
                return;
            }
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase),
                $"fixture: {CaptureDirectoryVariable} must be outside the Unity project ({full})");
            foreach (TMP_Text text in _root.GetComponentsInChildren<TMP_Text>(false)) text.ForceMeshUpdate();
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
            // The panel covers most of the screen; a nearly empty frame means the fixture camera drew nothing.
            if (drawn < total / 2) _fixture.Add($"{label} drew {drawn} of {total} pixels");
            Directory.CreateDirectory(full);
            string file = Path.Combine(full, $"{state}-{_target.width}x{_target.height}.png");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] png = _readback.EncodeToPNG();
                stream.Write(png, 0, png.Length);
            }
            string record = $"[character select wide] {label}: drawn {drawn}/{total} px, {file}";
            TestContext.Out.WriteLine(record);
            Debug.Log(record);
        }

        // The glyph boxes as drawn (CharacterMenuViewCaptureTests.TryGlyphArea): textBounds spans the fallback font's
        // tall line box, which Midline alignment lets pass the rect where nothing is drawn.
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

        void ForceLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Panel);
            Canvas.ForceUpdateCanvases();
        }

        // A rectangle in Panel's local units, whatever the anchors, pivots or scale in between
        // (CharacterMenuViewAppearanceTests.InPanel).
        Rect InPanel(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = Panel.InverseTransformPoint(corners[0]);
            Vector3 c = Panel.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static bool Overlaps(Rect a, Rect b) => a.xMin < b.xMax - Tolerance && b.xMin < a.xMax - Tolerance &&
            a.yMin < b.yMax - Tolerance && b.yMin < a.yMax - Tolerance;

        TMP_Text TextAt(string path)
        {
            Transform node = Panel.Find(path);
            if (node == null) return null;
            TMP_Text text = node.GetComponent<TMP_Text>();
            return text != null ? text : null;
        }

        void Click(string path)
        {
            Transform node = Panel.Find(path);
            Assert.IsTrue(node != null, $"exists: Panel/{path}");
            Button button = node.GetComponent<Button>();
            Assert.IsTrue(button != null, $"exists: Button on Panel/{path}");
            button.onClick.Invoke();
        }
    }
}
