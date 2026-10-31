using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Shared fixture of the PR-B guild size tests, written before the implementation (goal
    // 2026-10-30-login-screen-guild-window PR-B; lead contract pr-b-red-contract-v2, supplement msg_107ba9cc8f94 and
    // answer msg_978f10e2d973). The product builds ScreenSpaceOverlay canvases. Here, as CharacterMenuViewCaptureTests.cs
    // :138-171 does, a canvas is switched to ScreenSpaceCamera on a camera drawing into a RenderTexture of the goal's size,
    // so the canvas's own CanvasScaler sees that size; nothing else of the canvas changes. Rectangles are pixels of that
    // texture. This judges the same scaler, not the Overlay game screen, pointer input or the server.
    internal sealed class GuildSizeSurface : IDisposable
    {
        internal const string CaptureDirectoryVariable = "DAWNHOLDER_GUILD_SIZE_CAPTURE_DIR";
        static readonly Color s_background = new Color(0.08f, 0.09f, 0.11f, 1f);

        readonly GameObject _cameraObject;
        readonly List<Attached> _attached = new List<Attached>();
        Texture2D _readback;

        GuildSizeSurface(int width, int height)
        {
            _cameraObject = new GameObject("GuildSizeCaptureCamera (fixture)");
            Camera = _cameraObject.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = s_background;
            Camera.cullingMask = ~0;
            Camera.orthographic = true;
            Camera.allowHDR = false;
            Camera.allowMSAA = false;
            Texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture.Create();
            Camera.targetTexture = Texture;
        }

        internal Camera Camera { get; }

        internal RenderTexture Texture { get; }

        internal int Width => Texture.width;

        internal int Height => Texture.height;

        internal Rect Whole => new Rect(0f, 0f, Width, Height);

        internal string Size => Width + "x" + Height;

        internal static GuildSizeSurface Create(int width, int height) => new GuildSizeSurface(width, height);

        // Only renderMode, worldCamera and planeDistance change; the scaler's settings are recorded to show they did not.
        internal void Attach(Canvas canvas)
        {
            Assert.IsNotNull(canvas, "fixture: the window has its own canvas");
            Assert.IsTrue(canvas.isRootCanvas, $"fixture: {canvas.name} is a root canvas, so its own scaler sizes it");
            var attached = new Attached(canvas);
            _attached.Add(attached);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera;
            canvas.planeDistance = 1f;
        }

        // The product's LateUpdate resize and the scaler's Update run in real frames.
        internal static IEnumerator Settle()
        {
            for (int i = 0; i < 3; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < 2; i++) yield return null;
            Canvas.ForceUpdateCanvases();
        }

        // Supplement msg_107ba9cc8f94 (1): the scaler sees the texture's size, the canvas fills it, and the scaler is as built.
        internal string[] FixtureProblems()
        {
            var problems = new List<string>();
            foreach (Attached attached in _attached)
            {
                Canvas canvas = attached.Canvas;
                Vector2 seen = canvas.renderingDisplaySize;
                Vector2 units = ((RectTransform)canvas.transform).rect.size;
                if (Mathf.Abs(seen.x - Width) > 0.5f || Mathf.Abs(seen.y - Height) > 0.5f)
                    problems.Add($"{canvas.name}: the scaler sees {seen}, the texture is {Size}");
                if (Mathf.Abs(units.x * canvas.scaleFactor - Width) > 1f || Mathf.Abs(units.y * canvas.scaleFactor - Height) > 1f)
                    problems.Add($"{canvas.name}: canvas {units} at scale {canvas.scaleFactor} does not fill {Size}");
                string now = ScalerState(canvas.GetComponent<CanvasScaler>());
                if (now != attached.Scaler) problems.Add($"{canvas.name}: scaler {now}, as built {attached.Scaler}");
            }
            if (_attached.Count == 0) problems.Add("no canvas attached");
            return problems.ToArray();
        }

        internal string Describe() => string.Join("; ", _attached.Select(attached =>
            $"{attached.Canvas.name} scaler [{attached.Scaler}] display {attached.Canvas.renderingDisplaySize} " +
            $"canvas {((RectTransform)attached.Canvas.transform).rect.size} scale {attached.Canvas.scaleFactor.ToString("F4", CultureInfo.InvariantCulture)}"));

        // The texture-pixel rectangle of a RectTransform's four corners.
        internal Rect PixelRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Bounds(corners);
        }

        // The texture-pixel rectangle of a box given in the owner's local space (glyph boxes, mesh vertices).
        internal Rect PixelRect(Transform owner, IEnumerable<Vector3> localPoints) =>
            Bounds(localPoints.Select(point => owner.TransformPoint(point)));

        // Drawn on demand, then written only when the capture variable is set (supplement msg_107ba9cc8f94 (2)).
        // Returns the record line; the drawn share is checked by the caller so that the judgement never depends on it.
        internal string Capture(string fileName, out float drawnShare)
        {
            foreach (Attached attached in _attached)
                foreach (TMP_Text text in attached.Canvas.GetComponentsInChildren<TMP_Text>(false)) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            string method = Render();
            if (_readback == null) _readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = Texture;
                _readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                _readback.Apply(false);
            }
            finally
            {
                RenderTexture.active = previous;
            }
            Color32 clear = s_background;
            int drawn = _readback.GetPixels32().Count(pixel =>
                Math.Abs(pixel.r - clear.r) > 2 || Math.Abs(pixel.g - clear.g) > 2 || Math.Abs(pixel.b - clear.b) > 2);
            drawnShare = drawn / (float)(Width * Height);
            string record = $"{fileName}: {method}, drawn {drawn}/{Width * Height} px";
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
            if (string.IsNullOrEmpty(directory)) return record + ", not written (no " + CaptureDirectoryVariable + ")";
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase),
                $"fixture: {CaptureDirectoryVariable} must be outside the Unity project ({full})");
            Directory.CreateDirectory(full);
            string file = Path.Combine(full, fileName);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] png = _readback.EncodeToPNG();
                stream.Write(png, 0, png.Length);
            }
            return record + ", " + file;
        }

        public void Dispose()
        {
            foreach (Attached attached in _attached) attached.Restore();
            _attached.Clear();
            if (Camera != null) Camera.targetTexture = null;
            Texture.Release();
            UnityEngine.Object.Destroy(Texture);
            if (_readback != null) UnityEngine.Object.Destroy(_readback);
            UnityEngine.Object.Destroy(_cameraObject);
        }

        Rect Bounds(IEnumerable<Vector3> worldPoints)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 world in worldPoints)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(Camera, world);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // As GuildWindowIndependentSceneTests.cs:1020-1050: the pipeline's render request where it supports one, else
        // Camera.Render. Reached by name so this file does not bind to one pipeline API version.
        string Render()
        {
            Type pipeline = Type.GetType("UnityEngine.Rendering.RenderPipeline, UnityEngine.CoreModule");
            Type requestType = Type.GetType("UnityEngine.Rendering.RenderPipeline+StandardRequest, UnityEngine.CoreModule");
            MethodInfo supports = pipeline?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "SupportsRenderRequest" && method.IsGenericMethodDefinition);
            MethodInfo submit = pipeline?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "SubmitRenderRequest" && method.IsGenericMethodDefinition);
            string outcome = "no render request API";
            if (requestType != null && supports != null && submit != null)
            {
                try
                {
                    object request = Activator.CreateInstance(requestType);
                    requestType.GetField("destination")?.SetValue(request, Texture);
                    if ((bool)supports.MakeGenericMethod(requestType).Invoke(null, new[] { Camera, request }))
                    {
                        submit.MakeGenericMethod(requestType).Invoke(null, new[] { Camera, request });
                        return "RenderPipeline.SubmitRenderRequest(StandardRequest)";
                    }
                    outcome = "render request not supported";
                }
                catch (Exception error)
                {
                    outcome = "render request failed: " + (error.InnerException ?? error).GetType().Name;
                }
            }
            Camera.Render();
            return "Camera.Render (" + outcome + ")";
        }

        static string ScalerState(CanvasScaler scaler) => scaler == null ? "no CanvasScaler" :
            $"{scaler.uiScaleMode} {scaler.referenceResolution} match {scaler.matchWidthOrHeight.ToString(CultureInfo.InvariantCulture)} " +
            $"{scaler.screenMatchMode} ppu {scaler.referencePixelsPerUnit.ToString(CultureInfo.InvariantCulture)}";

        sealed class Attached
        {
            readonly RenderMode _mode;
            readonly Camera _camera;
            readonly float _planeDistance;

            internal Attached(Canvas canvas)
            {
                Canvas = canvas;
                Scaler = ScalerState(canvas.GetComponent<CanvasScaler>());
                _mode = canvas.renderMode;
                _camera = canvas.worldCamera;
                _planeDistance = canvas.planeDistance;
            }

            internal Canvas Canvas { get; }

            internal string Scaler { get; }

            internal void Restore()
            {
                if (Canvas == null) return;
                Canvas.renderMode = _mode;
                Canvas.worldCamera = _camera;
                Canvas.planeDistance = _planeDistance;
            }
        }
    }

    // The two guild windows built as GuildUiController builds them: GuildWindow.BuildRuntime and
    // BlacksmithWindow.BuildRuntime under the same parent (GuildUiController.cs:126 and :134, both transform.parent),
    // each bound to one GuildDisplayModel and opened only through its own public SetOpen.
    internal sealed class GuildWindowsUnderTest : IDisposable
    {
        internal const string GuildPanelName = "Guild";
        internal const string SmithPanelName = "Blacksmith";
        internal const string PictureName = "HousingForeground";

        GuildWindowsUnderTest(string label)
        {
            Holder = new GameObject("GuildSizeRoot_" + label);
            Guild = GuildWindow.BuildRuntime(Holder.transform);
            Smith = BlacksmithWindow.BuildRuntime(Holder.transform);
            Guild.Bind(Model);
            Smith.Bind(Model);
            Guild.SetInputAllowed(true);
            Smith.SetInputAllowed(true);
        }

        internal GameObject Holder { get; }

        internal GuildDisplayModel Model { get; } = new GuildDisplayModel();

        internal GuildWindow Guild { get; }

        internal BlacksmithWindow Smith { get; }

        internal RectTransform GuildPanel => Required(Guild.transform.Find(GuildPanelName), "the guild window's panel " + GuildPanelName);

        internal RectTransform SmithPanel => Required(Smith.transform.Find(SmithPanelName), "the blacksmith window's panel " + SmithPanelName);

        internal Image Picture
        {
            get
            {
                Image[] found = Guild.GetComponentsInChildren<Image>(true).Where(image => image.name == PictureName).ToArray();
                Assert.AreEqual(1, found.Length, $"fixture: exactly one {PictureName} image in the guild window");
                return found[0];
            }
        }

        internal static GuildWindowsUnderTest Build(string label) => new GuildWindowsUnderTest(label);

        internal static bool IsShown(Component window)
        {
            var group = window.GetComponent<CanvasGroup>();
            return window.gameObject.activeInHierarchy && group != null && group.alpha > 0f;
        }

        // The status, result and balance lines GuildUiController supplies (GuildUiController.cs:364-368); internal, so by name.
        internal void SetConnection(string status, string result, string balance)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo guild = typeof(GuildWindow).GetMethod("SetConnectionStatus", flags, null,
                new[] { typeof(string), typeof(string), typeof(bool) }, null);
            MethodInfo smith = typeof(BlacksmithWindow).GetMethod("SetConnectionStatus", flags, null,
                new[] { typeof(string), typeof(string), typeof(bool), typeof(string) }, null);
            Assert.IsNotNull(guild, "fixture: GuildWindow.SetConnectionStatus(string, string, bool)");
            Assert.IsNotNull(smith, "fixture: BlacksmithWindow.SetConnectionStatus(string, string, bool, string)");
            guild.Invoke(Guild, new object[] { status, result, true });
            smith.Invoke(Smith, new object[] { status, result, true, balance ?? "보유 골드 · 재료 확인 중" });
        }

        public void Dispose()
        {
            if (Holder != null) UnityEngine.Object.Destroy(Holder);
        }

        static RectTransform Required(Transform found, string what)
        {
            Assert.IsNotNull(found, "fixture: " + what);
            return (RectTransform)found;
        }
    }

    // One opened state of both windows drawn into one surface: built, given their values, opened only through SetOpen,
    // settled, and the fixture asserted before any measurement (supplement msg_107ba9cc8f94 (1)).
    internal sealed class GuildSizeStage : IDisposable
    {
        internal GuildWindowsUnderTest Windows { get; private set; }

        internal GuildSizeSurface Surface { get; private set; }

        // longestLines: the status, result and balance lines at their longest instead of the values' own (GuildSizeValues).
        internal IEnumerator Open(int width, int height, GuildStatusDisplay guildValue, BlacksmithDisplay smithValue,
            bool guild, bool smith, bool longestLines = false)
        {
            Dispose();
            Windows = GuildWindowsUnderTest.Build($"{width}x{height}");
            Windows.Model.SetGuild(guildValue);
            Windows.Model.SetBlacksmith(smithValue);
            if (longestLines)
                Windows.SetConnection(GuildSizeValues.LongestStatus, GuildSizeValues.LongestResult, GuildSizeValues.LongestBalance);
            Surface = GuildSizeSurface.Create(width, height);
            Surface.Attach(Windows.Guild.GetComponent<Canvas>());
            Surface.Attach(Windows.Smith.GetComponent<Canvas>());
            Windows.Guild.SetOpen(guild);
            Windows.Smith.SetOpen(smith);
            yield return GuildSizeSurface.Settle();
            Debug.Log($"[guild size] surface {Surface.Size}: {Surface.Describe()}");
            Assert.IsEmpty(Surface.FixtureProblems(), $"fixture: both canvases draw into the {Surface.Size} texture with their scalers as built");
            Assert.AreEqual(guild, GuildWindowsUnderTest.IsShown(Windows.Guild), "fixture: the guild window is shown as opened");
            Assert.AreEqual(smith, GuildWindowsUnderTest.IsShown(Windows.Smith), "fixture: the blacksmith window is shown as opened");
        }

        public void Dispose()
        {
            Surface?.Dispose();
            Surface = null;
            Windows?.Dispose();
            Windows = null;
        }
    }

    // Supplied display values. Approved scenario as GuildWindowIndependentSceneTests.cs:1567-1578 (World goal
    // 2026-10-14-guild-server 「승인 A」: one guild of two online members, level 1 of 2, a 30 level-up).
    internal static class GuildSizeValues
    {
        // Longest supplied texts. Name: 16 UTF-16 code units, the World wire limit (goal 2026-10-14-guild-server :378
        // 「새 송신 문자열은1~16자」); counts, treasury and costs are int and levels byte (same goal :383, S_GuildSnapshot).
        internal const string LongestName = "가나다라마바사아자차카타파하거너";
        // GuildDisplayProjection.cs:32, one of its two longest status lines (27 characters, as :31).
        internal const string LongestStatus = "서버 동기화 대기 중 · 새로고침으로 조회하세요.";
        // GuildDisplayProjection.cs:45 prefix with its longest reason, :79.
        internal const string LongestResult = "최근 길드 요청 거부 · 상태가 변경되었습니다. 최신 정보를 확인하세요.";
        // GuildDisplayProjection.cs:59: currency is int (InventorySnapshotState.cs:15) and the material count is one slot's,
        // at most InventoryLimits.MaxStack 99 (98_Shared/GameData/Items/InventoryLimits.cs:7).
        internal static readonly string LongestBalance = "보유 골드 " + int.MaxValue.ToString("N0") + " · 재료 99";

        internal static GuildStatusDisplay Level(int level) => level == 1
            ? new GuildStatusDisplay("새벽 길드", 2, 2, 30, 1, 2, false, 30, GuildActionAvailability.Available, false,
                GuildActionResult.None)
            : new GuildStatusDisplay("새벽 길드", 2, 2, 0, 2, 2, true, 0, GuildActionAvailability.MaxLevel, false,
                GuildActionResult.Succeeded);

        // Level 2 so the picture still shows, every other value at its upper bound.
        internal static GuildStatusDisplay LongLevelTwo() =>
            new GuildStatusDisplay(LongestName, int.MaxValue, int.MaxValue, int.MaxValue, 2, byte.MaxValue, true,
                int.MaxValue, GuildActionAvailability.Available, true, GuildActionResult.Refused);

        // The longest level text the byte allows; no picture is chosen for it (GuildHousingLayout.cs:47).
        internal static GuildStatusDisplay LongUpperLevel() =>
            new GuildStatusDisplay(LongestName, int.MaxValue, int.MaxValue, int.MaxValue, byte.MaxValue - 1, byte.MaxValue,
                true, int.MaxValue, GuildActionAvailability.Available, true, GuildActionResult.Refused);

        internal static BlacksmithDisplay Smith() =>
            new BlacksmithDisplay(CharacterClass.Knight, 0, 1, 0, 20, 1, GuildActionAvailability.Available, false,
                GuildActionResult.None);

        internal static BlacksmithDisplay LongSmith() =>
            new BlacksmithDisplay(CharacterClass.Mage, byte.MaxValue - 1, byte.MaxValue, int.MaxValue, int.MaxValue,
                int.MaxValue, GuildActionAvailability.Available, true, GuildActionResult.Refused);
    }

    internal static class GuildSizeGeometry
    {
        // GuildBaseLevel1/2.png, imported whole (EditMode/GuildHousingTests.cs:51).
        internal const float ArtWidth = 1672f;
        internal const float ArtHeight = 941f;

        // Unity's own drawing of the image: its OnPopulateMesh (preserveAspect, pivot and sprite padding included), read
        // back as vertices in the image's local space. Reached by name because it is protected.
        internal static Rect DrawnRect(GuildSizeSurface surface, Image image)
        {
            MethodInfo populate = typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            Assert.IsNotNull(populate, "fixture: Image.OnPopulateMesh(VertexHelper)");
            using (var helper = new VertexHelper())
            {
                populate.Invoke(image, new object[] { helper });
                var points = new List<Vector3>();
                var vertex = new UIVertex();
                for (int i = 0; i < helper.currentVertCount; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    points.Add(vertex.position);
                }
                Assert.Greater(points.Count, 0, "fixture: the picture builds a mesh");
                return surface.PixelRect(image.rectTransform, points);
            }
        }

        internal static float Area(Rect rect) => rect.width * rect.height;

        // As HudBottomWindowLayoutSceneTests.cs:696-701.
        internal static float OverlapArea(Rect a, Rect b)
        {
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0f && height > 0f ? width * height : 0f;
        }

        // Overlap deeper than the slack on both axes; touching edges are apart.
        internal static bool Overlaps(Rect a, Rect b, float slack) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > slack &&
            Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > slack;

        internal static bool Inside(Rect inner, Rect outer, float tolerance) =>
            inner.xMin >= outer.xMin - tolerance && inner.xMax <= outer.xMax + tolerance &&
            inner.yMin >= outer.yMin - tolerance && inner.yMax <= outer.yMax + tolerance;

        internal static bool SameRect(Rect a, Rect b, float tolerance) =>
            Mathf.Abs(a.xMin - b.xMin) <= tolerance && Mathf.Abs(a.xMax - b.xMax) <= tolerance &&
            Mathf.Abs(a.yMin - b.yMin) <= tolerance && Mathf.Abs(a.yMax - b.yMax) <= tolerance;

        internal static string Format(Rect rect) => string.Format(CultureInfo.InvariantCulture,
            "x {0:F2}..{1:F2} y {2:F2}..{3:F2} ({4:F2}x{5:F2})", rect.xMin, rect.xMax, rect.yMin, rect.yMax, rect.width, rect.height);

        internal static string PathUnder(Transform root, Transform node)
        {
            var names = new List<string>();
            for (Transform at = node; at != null && at != root; at = at.parent) names.Insert(0, at.name);
            return string.Join("/", names);
        }
    }
}
