using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Test-only access to GuildGrowthEffectView for GuildGrowthEffectLifecycleTests (growth-effect-pr pretest-contract-v2,
    // pretest/api-contract.md). The view does not exist before the implementation, so it is reached by the contract's names
    // the way the EditMode GuildProduct reaches the guild windows (that class is EditMode-only); a missing type or member
    // fails the calling test with "RED unimplemented". Product exceptions surface as themselves, not reflection wrappers.
    internal static class GuildGrowthEffectProduct
    {
        const string ViewTypeName = "Dawnholder.Client.UI.GuildGrowthEffectView";

        internal static Type ViewType
        {
            get
            {
                Type type = typeof(GuildWindow).Assembly.GetType(ViewTypeName);
                if (type == null) Assert.Fail($"RED unimplemented: product type {ViewTypeName} (api-contract.md)");
                return type;
            }
        }

        internal static Component BuildRuntime(Transform parent) =>
            (Component)Invoke(Method("BuildRuntime", typeof(Transform)), null, parent);

        internal static bool TryPlayConfirmed(Component view, GuildChangeKind kind, int confirmedLevel) =>
            (bool)Invoke(Method("TryPlayConfirmed", typeof(GuildChangeKind), typeof(int)), view, kind, confirmedLevel);

        internal static void CancelAll(Component view) => Invoke(Method("CancelAll"), view);

        static MethodInfo Method(string name, params Type[] parameters)
        {
            Type view = ViewType;
            MethodInfo method = view.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
            if (method == null)
            {
                Assert.Fail($"RED unimplemented: public {view.Name}.{name}({string.Join(", ", parameters.Select(p => p.Name))}) (api-contract.md)");
            }
            return method;
        }

        static object Invoke(MethodInfo method, object target, params object[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }

    // One uGUI graphic under the effect that a player sees this frame (GuildGrowthEffectScreen.Drawn decides which).
    internal sealed class GuildGrowthEffectGraphic
    {
        static readonly Regex Digits = new Regex("\\d+");

        internal GuildGrowthEffectGraphic(Component graphic, Transform[] lineage, string path, Rect screenRect, float alpha, Color color, string image,
            string text)
        {
            Graphic = graphic;
            Key = graphic.GetEntityId().ToString();
            Lineage = lineage;
            Path = path;
            ScreenRect = screenRect;
            Alpha = alpha;
            Color = color;
            Image = image;
            Text = text;
            Numbers = text == null
                ? Array.Empty<long>()
                : Digits.Matches(text).Cast<Match>().Select(match => long.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        }

        internal Component Graphic { get; }
        // Read when sampled, so a graphic stays identifiable after the product destroys it (destroyed Unity objects compare
        // equal to each other).
        internal string Key { get; }
        // Its own transform and every ancestor below the trace scope, as references that survive destruction.
        internal Transform[] Lineage { get; }
        internal string Path { get; }
        internal Rect ScreenRect { get; }
        // The graphic's colour alpha times its CanvasRenderer alpha and every CanvasGroup above it.
        internal float Alpha { get; }
        internal Color Color { get; }
        // The sprite or texture it draws; null for a text or a plain quad.
        internal string Image { get; }
        // Its text without rich-text tags; null for a non-text graphic.
        internal string Text { get; }
        internal long[] Numbers { get; }
        internal float Area => ScreenRect.width * ScreenRect.height;

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "{0} a={1:F3} rect=({2:F0},{3:F0},{4:F0},{5:F0}) col=#{6} {7}", Path, Alpha, ScreenRect.x, ScreenRect.y, ScreenRect.width,
            ScreenRect.height, ColorUtility.ToHtmlStringRGBA(Color), Text != null ? "text='" + Text + "'" : "img=" + (Image ?? "<none>"));

        internal bool IsUnder(Transform area) => Lineage.Any(item => ReferenceEquals(item, area));
    }

    // One frame's reading: when (real time) and what was drawn.
    internal sealed class GuildGrowthEffectSample
    {
        internal GuildGrowthEffectSample(float at, float frameDelta, GuildGrowthEffectGraphic[] drawn)
        {
            At = at;
            FrameDelta = frameDelta;
            Frame = Time.frameCount;
            Drawn = drawn;
            LevelTexts = drawn.Where(graphic => graphic.Numbers.Length != 0).ToArray();
            Numbers = LevelTexts.SelectMany(graphic => graphic.Numbers).Distinct().OrderBy(number => number).ToArray();
        }

        internal float At { get; }
        internal float FrameDelta { get; }
        internal int Frame { get; }
        internal GuildGrowthEffectGraphic[] Drawn { get; }
        // Drawn texts that show a number: the result level the caller confirmed.
        internal GuildGrowthEffectGraphic[] LevelTexts { get; }
        internal long[] Numbers { get; }
        internal bool Any => Drawn.Length != 0;
    }

    // Every frame's drawn graphics under one scope, timed by real time between Unity's own frames. Requests and state
    // changes are marked with their real time, and a raw "[GuildGrowthEffect]" log line is written whenever what is drawn
    // changes (at most every LogEverySeconds while it animates), so each verdict's numbers stay in the Unity log and the
    // later two-resolution capture can find the same moments (requirement 3).
    internal sealed class GuildGrowthEffectTrace
    {
        const float LogEverySeconds = 0.1f;

        readonly string _label;
        readonly Transform _scope;
        readonly List<GuildGrowthEffectSample> _samples = new List<GuildGrowthEffectSample>();
        float _previous;
        float _lastLogged = float.NegativeInfinity;
        string _lastShape;

        internal GuildGrowthEffectTrace(string label, Transform scope)
        {
            _label = label;
            _scope = scope;
            _previous = Time.realtimeSinceStartup;
        }

        internal IReadOnlyList<GuildGrowthEffectSample> Samples => _samples;

        internal static IEnumerator Watch(float seconds, params GuildGrowthEffectTrace[] traces)
        {
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                yield return null;
                foreach (GuildGrowthEffectTrace trace in traces) trace.Sample();
            }
            while (Time.realtimeSinceStartup < until);
        }

        internal static long[] NumbersIn(IEnumerable<GuildGrowthEffectSample> samples) =>
            samples.SelectMany(sample => sample.Numbers).Distinct().OrderBy(number => number).ToArray();

        internal float Mark(string moment)
        {
            float now = Time.realtimeSinceStartup;
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffect] {0} mark '{1}' real={2:F4} frame={3} time={4:F4} " +
                "unscaled={5:F4} timeScale={6} screen={7}x{8}", _label, moment, now, Time.frameCount, Time.time, Time.unscaledTime,
                Time.timeScale, Screen.width, Screen.height));
            return now;
        }

        // A reading logged but not kept, such as the same frame as a cancel (only later frames are judged).
        internal void LogNow(string moment)
        {
            GuildGrowthEffectGraphic[] drawn = GuildGrowthEffectScreen.Drawn(_scope);
            TestContext.Out.WriteLine($"[GuildGrowthEffect] {_label} now '{moment}' frame={Time.frameCount} drawn={drawn.Length} [{string.Join("; ", drawn.Select(g => g.ToString()))}]");
        }

        internal GuildGrowthEffectSample Sample()
        {
            float now = Time.realtimeSinceStartup;
            var sample = new GuildGrowthEffectSample(now, now - _previous, GuildGrowthEffectScreen.Drawn(_scope));
            _previous = now;
            _samples.Add(sample);
            string shape = string.Join("|", sample.Drawn.Select(graphic => graphic.Path + (graphic.Text ?? string.Empty)));
            if (shape != _lastShape || (sample.Any && now - _lastLogged >= LogEverySeconds)) Log(sample);
            _lastShape = shape;
            return sample;
        }

        internal IEnumerator NextFrame()
        {
            yield return null;
            Sample();
        }

        internal IEnumerator Frames(float seconds) => Watch(seconds, this);

        // Until nothing has been drawn for quietSeconds after something was, or until capSeconds pass.
        internal IEnumerator UntilQuiet(float quietSeconds, float capSeconds)
        {
            float start = Time.realtimeSinceStartup;
            bool seen = false;
            float quietSince = float.NaN;
            while (true)
            {
                yield return null;
                GuildGrowthEffectSample sample = Sample();
                if (sample.Any)
                {
                    seen = true;
                    quietSince = float.NaN;
                }
                else if (seen && float.IsNaN(quietSince))
                {
                    quietSince = sample.At;
                }
                if (seen && !float.IsNaN(quietSince) && sample.At - quietSince >= quietSeconds) yield break;
                if (sample.At - start >= capSeconds) yield break;
            }
        }

        internal GuildGrowthEffectSample[] After(float time) => _samples.Where(sample => sample.At > time).ToArray();

        internal GuildGrowthEffectSample[] Between(float from, float to) =>
            _samples.Where(sample => sample.At > from && sample.At <= to).ToArray();

        void Log(GuildGrowthEffectSample sample)
        {
            _lastLogged = sample.At;
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffect] {0} frame={1} real={2:F4} dt={3:F4} time={4:F4} " +
                "unscaled={5:F4} delta={6:F4} udelta={7:F4} drawn={8} numbers=[{9}] {10}", _label, sample.Frame, sample.At,
                sample.FrameDelta, Time.time, Time.unscaledTime, Time.deltaTime, Time.unscaledDeltaTime, sample.Drawn.Length,
                string.Join(",", sample.Numbers), string.Join("; ", sample.Drawn.Select(graphic => graphic.ToString()))));
        }
    }

    // What a player sees of the effect and what a pointer does through it. uGUI types are reached by full name because this
    // assembly does not reference uGUI (as ScreenUiProbe and InventorySceneLifecycleTests do).
    internal static class GuildGrowthEffectScreen
    {
        // Below one 8-bit colour step nothing shows on screen.
        internal const float MinimumDrawnAlpha = 1f / 255f;
        static readonly Regex Tags = new Regex("<[^>]*>");

        internal static Type UiType(string fullName)
        {
            Type type = Type.GetType(fullName + ", UnityEngine.UI");
            Assert.IsNotNull(type, "fixture: uGUI type " + fullName);
            return type;
        }

        // Active graphics under the scope that are shown (ScreenUiProbe.IsShown), not culled, at least one colour step
        // opaque, with an on-screen rectangle of some area: Image quads by their rectangle, TMP texts by their laid-out
        // glyph bounds. TMP's own sub-mesh graphics belong to their text and are not counted apart.
        internal static GuildGrowthEffectGraphic[] Drawn(Transform scope)
        {
            var drawn = new List<GuildGrowthEffectGraphic>();
            if (scope == null) return drawn.ToArray();
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            foreach (CanvasRenderer renderer in scope.GetComponentsInChildren<CanvasRenderer>())
            {
                Component graphic = renderer.GetComponents<Component>().FirstOrDefault(IsGraphic);
                if (graphic == null || renderer.cull || !ScreenUiProbe.IsShown(graphic) || !(renderer.transform is RectTransform rect)) continue;
                var label = graphic as TMP_Text;
                if (label == null && graphic.GetComponentInParent<TMP_Text>() != null) continue;
                Color color = ColorOf(graphic);
                float alpha = color.a * renderer.GetAlpha() * GroupAlpha(rect);
                Rect box = ScreenUiProbe.ScreenRect(rect);
                string text = null;
                if (label != null)
                {
                    text = Tags.Replace(label.text ?? string.Empty, string.Empty).Trim();
                    bool laidOut = text.Length != 0 && label.textInfo != null && label.textInfo.characterCount > 0;
                    box = laidOut ? ScreenUiProbe.Bounds(rect, label.textBounds) : Rect.zero;
                }
                if (alpha < MinimumDrawnAlpha || box.width <= 0f || box.height <= 0f || !box.Overlaps(screen)) continue;
                drawn.Add(new GuildGrowthEffectGraphic(graphic, LineageOf(rect, scope), ScreenUiProbe.PathOf(rect, scope), box, alpha, color,
                    label == null ? ImageOf(graphic) : null, text));
            }
            return drawn.ToArray();
        }

        // Every uGUI graphic under the root, drawn or not, inactive ones included.
        internal static Component[] Graphics(Transform root) =>
            root.GetComponentsInChildren<Component>(true).Where(IsGraphic).ToArray();

        internal static bool RaycastTarget(Component graphic) => (bool)graphic.GetType().GetProperty("raycastTarget").GetValue(graphic);

        internal static Component[] Raycasters(Transform root)
        {
            Type raycaster = UiType("UnityEngine.EventSystems.BaseRaycaster");
            return root.GetComponentsInChildren(raycaster, true);
        }

        // A real uGUI button covering the screen on its own Overlay canvas at the given order: the control a click that
        // passes through the effect must reach.
        internal static Component ButtonBelow(int order, Action clicked)
        {
            var canvasObject = new GameObject("GuildGrowthEffectTestButtonCanvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            canvasObject.AddComponent(UiType("UnityEngine.UI.GraphicRaycaster"));
            var buttonObject = new GameObject("GuildGrowthEffectTestButton", typeof(RectTransform));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Component image = buttonObject.AddComponent(UiType("UnityEngine.UI.Image"));
            Assert.IsTrue(RaycastTarget(image), "fixture: the button's image takes the pointer");
            Component button = buttonObject.AddComponent(UiType("UnityEngine.UI.Button"));
            ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).AddListener(() => clicked());
            return button;
        }

        // The pointer click the EventSystem's input module sends to the topmost raycast hit: up the hierarchy to the
        // first click handler. Returns the object that handled it, or null.
        internal static GameObject Click(GameObject topHit, Vector2 point)
        {
            Type eventSystemType = UiType("UnityEngine.EventSystems.EventSystem");
            Type pointerType = UiType("UnityEngine.EventSystems.PointerEventData");
            Type executeEvents = UiType("UnityEngine.EventSystems.ExecuteEvents");
            Type clickHandler = UiType("UnityEngine.EventSystems.IPointerClickHandler");
            object eventSystem = eventSystemType.GetProperty("current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Assert.IsNotNull(eventSystem, "fixture: a current EventSystem");
            object pointer = Activator.CreateInstance(pointerType, eventSystem);
            pointerType.GetProperty("position").SetValue(pointer, point);
            object callback = executeEvents.GetProperty("pointerClickHandler", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            MethodInfo execute = executeEvents.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(method => method.Name == "ExecuteHierarchy" && method.IsGenericMethodDefinition && method.GetParameters().Length == 3)
                .MakeGenericMethod(clickHandler);
            return (GameObject)execute.Invoke(null, new[] { topHit, pointer, callback });
        }

        // "ScaleWithScreenSize 960x540 match 0.5"-style reading of a canvas's CanvasScaler, or null without one.
        internal static string ScalerSettings(Canvas canvas)
        {
            Component scaler = canvas.GetComponents<Component>().FirstOrDefault(c => c.GetType().FullName == "UnityEngine.UI.CanvasScaler");
            if (scaler == null) return null;
            object Read(string property) => scaler.GetType().GetProperty(property).GetValue(scaler);
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} match {2}", Read("uiScaleMode"), Read("referenceResolution"),
                Read("matchWidthOrHeight"));
        }

        internal static Rect Intersect(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
            Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        internal static float OverlapArea(Rect a, Rect b)
        {
            Rect both = Intersect(a, b);
            return both.width > 0f && both.height > 0f ? both.width * both.height : 0f;
        }

        // The on-screen part of everything drawn in one frame.
        internal static float ScreenArea(IEnumerable<GuildGrowthEffectGraphic> drawn)
        {
            Rect[] rects = drawn.Select(graphic => Intersect(graphic.ScreenRect, new Rect(0f, 0f, Screen.width, Screen.height))).ToArray();
            if (rects.Length == 0) return 0f;
            Rect union = Rect.MinMaxRect(rects.Min(r => r.xMin), rects.Min(r => r.yMin), rects.Max(r => r.xMax), rects.Max(r => r.yMax));
            return union.width * union.height;
        }

        internal static Vector2 ScreenCentre(GuildGrowthEffectGraphic graphic) =>
            Intersect(graphic.ScreenRect, new Rect(0f, 0f, Screen.width, Screen.height)).center;

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
            {
                if (type.FullName == "UnityEngine.UI.Graphic") return true;
            }
            return false;
        }

        static Transform[] LineageOf(Transform transform, Transform scope)
        {
            var lineage = new List<Transform>();
            for (Transform current = transform; current != null && current != scope; current = current.parent) lineage.Add(current);
            return lineage.ToArray();
        }

        static Color ColorOf(Component graphic) =>
            graphic is TMP_Text text ? text.color : (Color)graphic.GetType().GetProperty("color").GetValue(graphic);

        // A CanvasGroup's alpha multiplies everything below it until a group that ignores its parents.
        static float GroupAlpha(Transform transform)
        {
            float alpha = 1f;
            for (Transform current = transform; current != null; current = current.parent)
            {
                foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
                {
                    if (!group.enabled) continue;
                    alpha *= group.alpha;
                    if (group.ignoreParentGroups) return alpha;
                }
            }
            return alpha;
        }

        // An Image's sprite (with its texture and rectangle, so unnamed runtime sprites still differ) or a RawImage's texture.
        static string ImageOf(Component graphic)
        {
            PropertyInfo sprite = graphic.GetType().GetProperty("sprite", BindingFlags.Public | BindingFlags.Instance);
            if (sprite != null && sprite.PropertyType == typeof(Sprite))
            {
                var value = (Sprite)sprite.GetValue(graphic);
                if (value == null) return null;
                return string.Format(CultureInfo.InvariantCulture, "sprite '{0}' of '{1}' {2}", value.name,
                    value.texture != null ? value.texture.name : "no texture", value.rect);
            }
            PropertyInfo texture = graphic.GetType().GetProperty("texture", BindingFlags.Public | BindingFlags.Instance);
            if (texture != null && typeof(Texture).IsAssignableFrom(texture.PropertyType))
            {
                var value = (Texture)texture.GetValue(graphic);
                return value == null ? null : $"texture '{value.name}' {value.width}x{value.height}";
            }
            return null;
        }
    }
}
