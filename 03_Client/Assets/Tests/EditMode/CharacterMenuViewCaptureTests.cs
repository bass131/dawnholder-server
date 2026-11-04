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
    // The character menu at the goal's 1920×1080 and 1280×720 in four states driven by fake replies (list ready,
    // empty-slot form, a refused name, entry wait). Requirement source: goal 2026-10-24-character-select-create-ui
    // 「10-28 승인으로 더한 것」 and completion row 6. Every visible text must keep all its characters, stay in its
    // own rect and on screen, and touch no other text or control; controls stay on screen and apart.
    // Fixture, as in UiWindowCompositionRenderTests: the product's Overlay canvas is switched to ScreenSpaceCamera
    // on a camera drawing into a render texture of the target size, so its own CanvasScaler sees that size. These
    // captures are not the game screen and say nothing about real pointer input, server replies or scene entry.
    // PNGs are written only when DAWNHOLDER_CHARACTER_MENU_CAPTURE_DIR is set.
    public sealed class CharacterMenuViewCaptureTests
    {
        const string CaptureDirectoryVariable = "DAWNHOLDER_CHARACTER_MENU_CAPTURE_DIR";
        // UI units; absorbs TMP float layout without hiding a real overlap.
        const float Tolerance = 0.5f;
        static readonly Color s_background = new(0.08f, 0.09f, 0.11f, 1f);

        readonly List<byte> _selections = new();
        readonly List<(byte Slot, byte ClassId, string Name)> _creations = new();
        readonly List<string> _problems = new();
        GameObject _root;
        GameObject _cameraObject;
        CharacterMenuView _view;
        RenderTexture _target;
        Texture2D _readback;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _selections.Clear();
            _creations.Clear();
            _problems.Clear();
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

        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void FourFakeReplyStates_KeepEveryTextWholeOnScreenAndApart(int width, int height)
        {
            var knight = new Slot("가람", 0);
            var mage = new Slot("한별", 1);
            _root = new GameObject("Character menu capture", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _selections.Add(slot),
                (slot, classId, name) => _creations.Add((slot, classId, name)), () => { }, () => { });
            Camera camera = AttachCamera(width, height);
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);

            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, mage));
            Check(camera, "1-list-ready", directory);

            Click("Slot3");
            NameInput().text = "새벽지기";
            Assert.IsTrue(Panel.Find("CreateForm/NameRule").gameObject.activeInHierarchy, "the name rule is shown");
            Check(camera, "2-create-form", directory);

            Click("CreateForm/Create");
            Assert.AreEqual(1, _creations.Count);
            _view.Render(new CharacterMenuViewState(Phase.Creating, knight, mage));
            // Fake NameTaken(6) reply for slot 3, through the product's wording table.
            Assert.IsTrue(CharacterMenuResultText.TryGetFailureText(6, 0, out string nameTaken));
            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, mage, message: "슬롯 3: " + nameTaken));
            Assert.AreEqual("슬롯 3: 이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.", Label("Result").text);
            Check(camera, "3-name-taken", directory);

            Click("CreateForm/Cancel");
            Click("Slot1");
            Click("Submit");
            CollectionAssert.AreEqual(new byte[] { 1 }, _selections);
            _view.Render(new CharacterMenuViewState(Phase.Selecting, knight, mage));
            _view.Render(new CharacterMenuViewState(Phase.Entering, knight, mage));
            Assert.AreEqual("선택이 승인되었습니다. 서버 입장을 기다리고 있습니다.", Label("Status").text);
            Assert.IsFalse(Control("Submit").interactable || Control("Slot2").interactable || Control("Refresh").interactable,
                "entry wait locks the menu");
            Check(camera, "4-entering", directory);

            Assert.IsEmpty(_problems, $"{width}x{height}: every text whole, inside its rect and the screen, and apart");
        }

        // The texts the connected menu adds (CharacterMenuPresenter): a created character, a send the client refused,
        // and the longest approved refusal, OutcomeUnknown, which ends the connection. Same checks as above.
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void ConnectedMenuTexts_StayWholeOnScreenAndApart(int width, int height)
        {
            var knight = new Slot("가람", 0);
            var created = new Slot("새벽지기", 1);
            _root = new GameObject("Character menu capture", typeof(RectTransform));
            _view = new CharacterMenuView(_root, slot => _selections.Add(slot),
                (slot, classId, name) => _creations.Add((slot, classId, name)), () => { }, () => { });
            Camera camera = AttachCamera(width, height);
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);

            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, slot3: created,
                message: "슬롯 3: 캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요."));
            Check(camera, "5-created", directory);

            Click("Slot2");
            NameInput().text = "새벽지기";
            Click("CreateForm/Create");
            _view.Render(new CharacterMenuViewState(Phase.Ready, knight, slot3: created,
                message: "슬롯 2: 요청을 보내지 못했습니다. 입력을 확인하고 다시 시도해 주세요."));
            Check(camera, "6-not-sent", directory);

            _view.Render(new CharacterMenuViewState(Phase.Disconnected, knight, slot3: created,
                message: "슬롯 2: 결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요."));
            Assert.AreEqual("연결이 종료되었습니다. 메뉴로 돌아가 다시 접속하세요.", Label("Status").text);
            Check(camera, "7-outcome-unknown", directory);

            Assert.IsEmpty(_problems, $"{width}x{height}: every text whole, inside its rect and the screen, and apart");
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
            TestContext.Out.WriteLine($"[character menu capture] {width}x{height}: renderingDisplaySize {seen} " +
                $"scaleFactor {canvas.scaleFactor:F4} canvas {units}");
            Assert.IsTrue(Mathf.Abs(seen.x - width) < 0.5f && Mathf.Abs(seen.y - height) < 0.5f,
                $"fixture: the scaler sees a {seen} screen, the capture is {width}x{height}");
            Assert.IsTrue(Mathf.Abs(units.x * canvas.scaleFactor - width) < 1f && Mathf.Abs(units.y * canvas.scaleFactor - height) < 1f,
                $"fixture: the canvas {units} at scale {canvas.scaleFactor} fills the {width}x{height} capture");
            return camera;
        }

        void Check(Camera camera, string state, string directory)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = (RectTransform)_root.GetComponentInChildren<Canvas>().transform;
            Rect screen = canvas.rect;
            string label = $"{_target.width}x{_target.height} {state}";
            var texts = new List<(Component Owner, Rect Area)>();
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text)) continue;
                text.ForceMeshUpdate();
                if (text.isTextTruncated) _problems.Add($"{label}: {PathOf(text)} lost characters of \"{text.text}\"");
                if (!TryGlyphArea(text, out Vector2 min, out Vector2 max)) continue;
                Rect own = text.rectTransform.rect;
                // The name input's text scrolls sideways inside its masked viewport; only its height must fit.
                bool scrolls = text == NameInput().textComponent;
                if (min.y < own.yMin - Tolerance || max.y > own.yMax + Tolerance ||
                    !scrolls && (min.x < own.xMin - Tolerance || max.x > own.xMax + Tolerance))
                    _problems.Add($"{label}: {PathOf(text)} draws {min}..{max} outside its rect {own}");
                texts.Add((text, InCanvas(canvas, text.rectTransform, min, max)));
            }
            var controls = canvas.GetComponentsInChildren<Selectable>(false)
                .Select(control => ((Component)control, InCanvas(canvas, (RectTransform)control.transform)))
                .ToList();
            foreach ((Component owner, Rect area) in texts.Concat(controls))
                if (area.xMin < screen.xMin - Tolerance || area.xMax > screen.xMax + Tolerance ||
                    area.yMin < screen.yMin - Tolerance || area.yMax > screen.yMax + Tolerance)
                    _problems.Add($"{label}: {PathOf(owner)} leaves the screen at {area}");
            for (int i = 0; i < texts.Count; i++)
            {
                for (int j = i + 1; j < texts.Count; j++)
                    if (Overlaps(texts[i].Area, texts[j].Area))
                        _problems.Add($"{label}: texts {PathOf(texts[i].Owner)} and {PathOf(texts[j].Owner)} overlap");
                foreach ((Component control, Rect area) in controls)
                    if (!texts[i].Owner.transform.IsChildOf(control.transform) && Overlaps(texts[i].Area, area))
                        _problems.Add($"{label}: text {PathOf(texts[i].Owner)} covers control {PathOf(control)}");
            }
            for (int i = 0; i < controls.Count; i++)
                for (int j = i + 1; j < controls.Count; j++)
                    if (Overlaps(controls[i].Item2, controls[j].Item2))
                        _problems.Add($"{label}: controls {PathOf(controls[i].Item1)} and {PathOf(controls[j].Item1)} overlap");

            string record = $"[character menu capture] {label}: {texts.Count} texts, {controls.Count} controls checked";
            if (!string.IsNullOrEmpty(directory)) record += "; " + Save(camera, directory, state);
            TestContext.Out.WriteLine(record);
            Debug.Log(record);
        }

        string Save(Camera camera, string directory, string state)
        {
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
            if (drawn < total / 2) _problems.Add($"fixture: {state} drew {drawn} of {total} pixels");
            Directory.CreateDirectory(full);
            string file = Path.Combine(full, $"character-menu-{_target.width}x{_target.height}-{state}.png");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] png = _readback.EncodeToPNG();
                stream.Write(png, 0, png.Length);
            }
            return $"drawn {drawn}/{total} px, {file}";
        }

        // The glyph boxes as drawn. textBounds spans the fallback font's tall line box, which Midline alignment lets
        // pass the rect where nothing is drawn; a line that does not fit is caught by isTextTruncated instead.
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

        static Rect InCanvas(RectTransform canvas, RectTransform rect) => InCanvas(canvas, rect, rect.rect.min, rect.rect.max);

        static Rect InCanvas(RectTransform canvas, RectTransform rect, Vector2 min, Vector2 max)
        {
            Vector3 a = canvas.InverseTransformPoint(rect.TransformPoint(min));
            Vector3 b = canvas.InverseTransformPoint(rect.TransformPoint(max));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static bool Overlaps(Rect a, Rect b) => a.xMin < b.xMax - Tolerance && b.xMin < a.xMax - Tolerance &&
            a.yMin < b.yMax - Tolerance && b.yMin < a.yMax - Tolerance;

        string PathOf(Component component)
        {
            var names = new List<string>();
            for (Transform node = component.transform; node != null && node != Panel; node = node.parent) names.Add(node.name);
            names.Reverse();
            return string.Join("/", names);
        }

        Button Control(string path) => Panel.Find(path).GetComponent<Button>();

        TMP_Text Label(string path) => Panel.Find(path).GetComponent<TMP_Text>();

        TMP_InputField NameInput() => Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();

        void Click(string path) => Control(path).onClick.Invoke();
    }
}
