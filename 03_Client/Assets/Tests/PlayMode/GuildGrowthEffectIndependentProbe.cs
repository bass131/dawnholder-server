using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Dawnholder.Client.Tests.PlayMode
{
    // One uGUI graphic of the guild growth effect that a player sees this frame (GuildGrowthEffectIndependentProbe.Drawn).
    internal sealed class GuildGrowthEffectIndependentPart
    {
        static readonly Regex Digits = new Regex("\\d+");

        internal GuildGrowthEffectIndependentPart(string path, Rect rect, float alpha, string image, TMP_Text label, string text,
            bool laidOut)
        {
            Path = path;
            Rect = rect;
            Alpha = alpha;
            Image = image;
            Label = label;
            Text = text;
            LaidOut = laidOut;
            Numbers = text == null
                ? Array.Empty<long>()
                : Digits.Matches(text).Cast<Match>().Select(item => long.Parse(item.Value, CultureInfo.InvariantCulture)).ToArray();
        }

        // Below the effect root; the first name is the effect's own area for one kind.
        internal string Path { get; }
        internal string Area => Path.Split('/')[0];
        internal Rect Rect { get; }
        // Its colour alpha times its CanvasRenderer alpha and every CanvasGroup above it.
        internal float Alpha { get; }
        // The sprite it draws, with texture and rectangle so unnamed runtime sprites still differ; null for a text.
        internal string Image { get; }
        internal TMP_Text Label { get; }
        internal string Text { get; }
        // A text whose glyphs were laid out this frame, so its rectangle is its glyph bounds rather than its box.
        internal bool LaidOut { get; }
        internal long[] Numbers { get; }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0} a={1:F3} ({2:F0},{3:F0},{4:F0},{5:F0}) {6}",
            Path, Alpha, Rect.xMin, Rect.yMin, Rect.width, Rect.height, Text != null ? "text='" + Text + "'" : Image ?? "<no image>");
    }

    // One frame's reading, timed by real time.
    internal sealed class GuildGrowthEffectIndependentFrame
    {
        internal GuildGrowthEffectIndependentFrame(float at, float delta, GuildGrowthEffectIndependentPart[] parts)
        {
            At = at;
            Delta = delta;
            Index = Time.frameCount;
            Parts = parts;
        }

        internal float At { get; }
        internal float Delta { get; }
        internal int Index { get; }
        internal GuildGrowthEffectIndependentPart[] Parts { get; }
        internal bool Any => Parts.Length != 0;
        internal long[] Numbers => Parts.SelectMany(part => part.Numbers).Distinct().OrderBy(number => number).ToArray();
        // How much light the frame shows: each drawn part's on-screen area times its alpha. The capture picks its emphasis
        // frame by this observed sum, not by the product's own stage timing.
        internal float Weight => Parts.Sum(part => GuildGrowthEffectIndependentProbe.OnScreen(part.Rect) * part.Alpha);
    }

    // Every frame's drawn parts under one effect root. A raw "[GuildGrowthEffectIndependent]" log line is written for each
    // mark and whenever what is drawn changes, so the numbers behind each verdict stay in the Unity log.
    internal sealed class GuildGrowthEffectIndependentWatch
    {
        const float LogEverySeconds = 0.25f;

        readonly string _label;
        readonly Func<Transform> _scope;
        readonly List<GuildGrowthEffectIndependentFrame> _frames = new List<GuildGrowthEffectIndependentFrame>();
        float _previous;
        float _lastLogged = float.NegativeInfinity;
        string _lastShape;

        // The scope is read every frame, so a destroyed root simply reads as nothing drawn.
        internal GuildGrowthEffectIndependentWatch(string label, Func<Transform> scope)
        {
            _label = label;
            _scope = scope;
            _previous = Time.realtimeSinceStartup;
        }

        internal IReadOnlyList<GuildGrowthEffectIndependentFrame> Frames => _frames;

        internal float Mark(string moment)
        {
            float now = Time.realtimeSinceStartup;
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffectIndependent] {0} mark '{1}' real={2:F4} frame={3} " +
                "timeScale={4} screen={5}x{6}", _label, moment, now, Time.frameCount, Time.timeScale, Screen.width, Screen.height));
            return now;
        }

        internal GuildGrowthEffectIndependentFrame Sample()
        {
            float now = Time.realtimeSinceStartup;
            var frame = new GuildGrowthEffectIndependentFrame(now, now - _previous, GuildGrowthEffectIndependentProbe.Drawn(_scope()));
            _previous = now;
            _frames.Add(frame);
            string shape = string.Join("|", frame.Parts.Select(part => part.Path + part.Text));
            if (shape != _lastShape || (frame.Any && now - _lastLogged >= LogEverySeconds))
            {
                _lastLogged = now;
                TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffectIndependent] {0} frame={1} real={2:F4} dt={3:F4} " +
                    "drawn={4} numbers=[{5}] {6}", _label, frame.Index, frame.At, frame.Delta, frame.Parts.Length,
                    string.Join(",", frame.Numbers), string.Join("; ", frame.Parts.Select(part => part.ToString()))));
            }
            _lastShape = shape;
            return frame;
        }

        internal IEnumerator For(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            do
            {
                yield return null;
                Sample();
            }
            while (Time.realtimeSinceStartup < until);
        }

        // Until nothing has been drawn for afterEnd seconds after something was, or until cap seconds pass.
        internal IEnumerator UntilClear(float afterEnd, float cap)
        {
            float start = Time.realtimeSinceStartup;
            bool seen = false;
            float clearSince = float.NaN;
            while (true)
            {
                yield return null;
                GuildGrowthEffectIndependentFrame frame = Sample();
                if (frame.Any)
                {
                    seen = true;
                    clearSince = float.NaN;
                }
                else if (seen && float.IsNaN(clearSince))
                {
                    clearSince = frame.At;
                }
                if (seen && !float.IsNaN(clearSince) && frame.At - clearSince >= afterEnd) yield break;
                if (frame.At - start >= cap) yield break;
            }
        }

        internal GuildGrowthEffectIndependentFrame[] After(float time) => _frames.Where(frame => frame.At > time).ToArray();

        // The visible end lies after the last frame that drew something and no later than the first clear frame after it,
        // both measured from the request. A bound outside the window on the certain side fails; a frame gap that leaves the
        // end on both sides of a bound is inconclusive, with the raw numbers, rather than a pass or a failure.
        internal void AssertEndsInside(float requestedAt, float shortest, float longest, string what)
        {
            GuildGrowthEffectIndependentFrame[] after = After(requestedAt);
            GuildGrowthEffectIndependentFrame lastDrawn = after.LastOrDefault(frame => frame.Any);
            Assert.IsNotNull(lastDrawn, what + ": something is drawn after the request");
            GuildGrowthEffectIndependentFrame firstClear = after.FirstOrDefault(frame => !frame.Any && frame.At > lastDrawn.At);
            Assert.IsNotNull(firstClear, what + ": the screen clears inside the watch");
            float earliest = lastDrawn.At - requestedAt;
            float latest = firstClear.At - requestedAt;
            string numbers = string.Format(CultureInfo.InvariantCulture, "{0}: visible end between {1:F4} and {2:F4} s after the request " +
                "(last drawn frame {3}, first clear frame {4}), target {5:F1}-{6:F1} s", what, earliest, latest, lastDrawn.Index,
                firstClear.Index, shortest, longest);
            TestContext.Out.WriteLine("[GuildGrowthEffectIndependent] " + numbers);
            Assert.GreaterOrEqual(latest, shortest, numbers + ": ended too early");
            Assert.LessOrEqual(earliest, longest, numbers + ": still drawn too late");
            if (earliest < shortest || latest > longest) Assert.Inconclusive(numbers + ": a frame gap straddles a bound");
        }

        // Every on-screen rectangle the watch saw drawn, joined into one.
        internal Rect Footprint() => GuildGrowthEffectIndependentProbe.Union(_frames.SelectMany(frame => frame.Parts)
            .Select(part => GuildGrowthEffectIndependentProbe.Clip(part.Rect)));
    }

    // Reads what the effect draws from live scene objects only: active, enabled graphics that are not culled, at least one
    // 8-bit colour step opaque after CanvasGroups, under enabled canvases and on screen; texts by their laid-out glyph bounds.
    // It is written apart from the pretest's GuildGrowthEffectTestAdapter so the independent verdicts do not rest on the
    // observation code under review, and it never reads the product's fields or repeats its timing, alpha or placement.
    // uGUI types are reached by name because this assembly does not reference uGUI (as ScreenUiProbe does).
    internal static class GuildGrowthEffectIndependentProbe
    {
        // The atlas the effect PR commits at Assets/Resources/UI/GuildGrowthEffect/GuildGrowthEffectAtlas.png.
        internal const string AtlasKey = "UI/GuildGrowthEffect/GuildGrowthEffectAtlas";
        // Below one 8-bit colour step nothing shows on screen.
        const float MinimumAlpha = 1f / 255f;
        static readonly Regex Tags = new Regex("<[^>]*>");

        internal static Type UiType(string fullName)
        {
            Type type = Type.GetType(fullName + ", UnityEngine.UI");
            Assert.IsNotNull(type, "fixture: uGUI type " + fullName);
            return type;
        }

        internal static GuildGrowthEffectIndependentPart[] Drawn(Transform scope)
        {
            var drawn = new List<GuildGrowthEffectIndependentPart>();
            if (scope == null) return drawn.ToArray();
            foreach (CanvasRenderer renderer in scope.GetComponentsInChildren<CanvasRenderer>())
            {
                var graphic = renderer.GetComponents<Component>().FirstOrDefault(IsGraphic) as Behaviour;
                if (graphic == null || !graphic.isActiveAndEnabled || renderer.cull || !(renderer.transform is RectTransform rect)) continue;
                if (!graphic.GetComponentsInParent<Canvas>().All(canvas => canvas.enabled)) continue;
                var label = graphic as TMP_Text;
                // TMP's sub-mesh graphics belong to their text and are not counted apart.
                if (label == null && graphic.GetComponentInParent<TMP_Text>() != null) continue;
                float alpha = ColourOf(graphic).a * renderer.GetAlpha() * GroupAlpha(rect);
                Rect box = ScreenUiProbe.ScreenRect(rect);
                string text = null;
                bool laidOut = false;
                if (label != null)
                {
                    text = Tags.Replace(label.text ?? string.Empty, string.Empty).Trim();
                    laidOut = text.Length != 0 && label.textInfo != null && label.textInfo.characterCount > 0;
                    if (laidOut) box = ScreenUiProbe.Bounds(rect, label.textBounds);
                    else if (text.Length == 0) continue;
                }
                if (alpha < MinimumAlpha || OnScreen(box) <= 0f) continue;
                drawn.Add(new GuildGrowthEffectIndependentPart(ScreenUiProbe.PathOf(rect, scope), box, alpha,
                    label == null ? ImageOf(graphic) : null, label, text, laidOut));
            }
            return drawn.ToArray();
        }

        internal static Texture2D Atlas()
        {
            var atlas = Resources.Load<Texture2D>(AtlasKey);
            Assert.IsNotNull(atlas, "fixture: the committed atlas loads by " + AtlasKey);
            return atlas;
        }

        // Sprites cut from the texture that exist now, runtime ones included.
        internal static int SpritesOf(Texture texture) => Resources.FindObjectsOfTypeAll<Sprite>().Count(sprite => sprite.texture == texture);

        // Objects of the type that live in a loaded scene or in no asset file: what a leak of runtime objects would add.
        internal static int Live<T>() where T : UnityEngine.Object => Resources.FindObjectsOfTypeAll<T>().Count(item =>
            item is GameObject gameObject ? gameObject.scene.IsValid() : item is Component component ? component.gameObject.scene.IsValid()
                : !IsPersistentAsset(item));

        internal static float OnScreen(Rect rect)
        {
            Rect clipped = Clip(rect);
            return clipped.width > 0f && clipped.height > 0f ? clipped.width * clipped.height : 0f;
        }

        internal static Rect Clip(Rect rect) => Rect.MinMaxRect(Mathf.Max(rect.xMin, 0f), Mathf.Max(rect.yMin, 0f),
            Mathf.Min(rect.xMax, Screen.width), Mathf.Min(rect.yMax, Screen.height));

        internal static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin && inner.yMin >= outer.yMin && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

        internal static Rect ScreenBox() => new Rect(0f, 0f, Screen.width, Screen.height);

        internal static float OverlapArea(Rect a, Rect b)
        {
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0f && height > 0f ? width * height : 0f;
        }

        internal static Rect Union(IEnumerable<Rect> rects)
        {
            Rect[] all = rects.Where(rect => rect.width > 0f && rect.height > 0f).ToArray();
            return all.Length == 0
                ? Rect.zero
                : Rect.MinMaxRect(all.Min(rect => rect.xMin), all.Min(rect => rect.yMin), all.Max(rect => rect.xMax), all.Max(rect => rect.yMax));
        }

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
            {
                if (type.FullName == "UnityEngine.UI.Graphic") return true;
            }
            return false;
        }

        // Runtime objects have no asset file; Resources.FindObjectsOfTypeAll also returns loaded assets, which a leak does
        // not add to. A player has no such test, so there every loaded object counts.
        static bool IsPersistentAsset(UnityEngine.Object item)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(item);
#else
            return false;
#endif
        }

        static Color ColourOf(Component graphic) =>
            graphic is TMP_Text text ? text.color : (Color)graphic.GetType().GetProperty("color").GetValue(graphic);

        // A CanvasGroup's alpha multiplies everything below it until a group that ignores its parents (uGUI's own rule).
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

        static string ImageOf(Component graphic)
        {
            PropertyInfo property = graphic.GetType().GetProperty("sprite", BindingFlags.Public | BindingFlags.Instance);
            if (property == null || property.PropertyType != typeof(Sprite)) return null;
            var sprite = (Sprite)property.GetValue(graphic);
            return sprite == null ? null : string.Format(CultureInfo.InvariantCulture, "sprite '{0}' of '{1}' {2}", sprite.name,
                sprite.texture != null ? sprite.texture.name : "no texture", sprite.rect);
        }
    }
}
