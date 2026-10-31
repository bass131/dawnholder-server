using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.PlayMode.GuildSizeGeometry;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Every caption GuildWindowLayout.Button builds sits in its button's vertical middle with room above and below,
    // written before the fix (goal 2026-10-30-login-screen-guild-window PR-B; lead contract pr-b-testfix-contract-v1.md
    // S4). Judged in batchmode: the windows draw into 1920x1080 and 1280x720 textures (GuildSizeStage; GameViewSizeScope
    // needs the GUI editor). After TMP_Text.ForceMeshUpdate the glyph box is the union of the visible characters'
    // textInfo.characterInfo bottomLeft..topRight, as GuildWindowSizeTextFitTests.cs TryGlyphArea, turned into texture
    // pixels and compared with the button's RectTransform in the same pixels (y up).
    public sealed class GuildWindowButtonLabelTests
    {
        // Contract pr-b-testfix-contract-v1.md S4 「고정 기준」 (lead decision, fixed before the run): the glyph box centre
        // is at most max(CentreFloor px, CentreShare x button height) from the button's, and its bottom and top are each
        // at least MarginShare x button height inside the button's.
        const float CentreFloor = 2f;
        const float CentreShare = 0.06f;
        const float MarginShare = 0.15f;

        static readonly string[] s_guildButtons = { "Close", "LevelUp", "Refresh" };
        static readonly string[] s_smithButtons = { "Close", "Enhance", "Refresh" };

        readonly GuildSizeStage _stage = new GuildSizeStage();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _stage.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuildOnly_1920x1080_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1920, 1080, guild: true, smith: false);
        }

        [UnityTest]
        public IEnumerator GuildOnly_1280x720_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1280, 720, guild: true, smith: false);
        }

        [UnityTest]
        public IEnumerator BlacksmithOnly_1920x1080_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1920, 1080, guild: false, smith: true);
        }

        [UnityTest]
        public IEnumerator BlacksmithOnly_1280x720_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1280, 720, guild: false, smith: true);
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1920, 1080, guild: true, smith: true);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_EveryButtonCaption_IsInTheButtonsVerticalMiddle()
        {
            yield return JudgeButtons(1280, 720, guild: true, smith: true);
        }

        // Positive control (contract S4): a copy of the blacksmith Enhance caption made in the test, alike in everything
        // but Midline alignment (MainMenuLoginView.cs:169-170), passes the same measurement. The product caption is left
        // as built and the copy is destroyed afterwards.
        [UnityTest]
        public IEnumerator PositiveControl_1920x1080_MidlineCopyOfTheEnhanceCaption_IsJudgedCentred()
        {
            yield return _stage.Open(1920, 1080, GuildSizeValues.Level(1), GuildSizeValues.Smith(), guild: false, smith: true);
            Button[] found = _stage.Windows.SmithPanel.GetComponentsInChildren<Button>(true).Where(button => button.name == "Enhance").ToArray();
            Assert.AreEqual(1, found.Length, "fixture: one Enhance button in the blacksmith panel");
            TMP_Text[] captions = found[0].GetComponentsInChildren<TMP_Text>(true);
            Assert.AreEqual(1, captions.Length, "fixture: the Enhance button has one caption");
            TMP_Text copy = MidlineCopy(captions[0]);
            try
            {
                Measurement product = Measure("Blacksmith/Enhance (as built)", found[0], captions[0]);
                Measurement centred = Measure("Blacksmith/Enhance (Midline copy)", found[0], copy);
                Record($"1920x1080 positive control: {product}");
                Record($"1920x1080 positive control: {centred}");
                Assert.IsTrue(centred.Glyphs.HasValue, "the Midline copy draws visible characters");
                Assert.IsTrue(centred.Centred && centred.Clear, "the same measurement passes a caption set in the vertical middle: " + centred);
            }
            finally
            {
                UnityEngine.Object.Destroy(copy.gameObject);
            }
        }

        IEnumerator JudgeButtons(int width, int height, bool guild, bool smith)
        {
            yield return _stage.Open(width, height, GuildSizeValues.Level(1), GuildSizeValues.Smith(), guild, smith);
            string state = guild && smith ? "both open" : guild ? "guild only" : "blacksmith only";
            var panels = new List<(RectTransform Panel, string[] Names)>();
            if (guild) panels.Add((_stage.Windows.GuildPanel, s_guildButtons));
            if (smith) panels.Add((_stage.Windows.SmithPanel, s_smithButtons));
            var missing = new List<string>();
            var measured = new List<Measurement>();
            foreach ((RectTransform panel, string[] names) in panels)
            {
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                string[] found = buttons.Select(button => button.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                if (!found.SequenceEqual(names.OrderBy(name => name, StringComparer.Ordinal)))
                    missing.Add($"{panel.name} holds buttons [{string.Join(", ", found)}], not exactly [{string.Join(", ", names)}]");
                foreach (Button button in buttons.Where(button => names.Contains(button.name)))
                {
                    TMP_Text[] captions = button.GetComponentsInChildren<TMP_Text>(true);
                    if (captions.Length != 1)
                    {
                        missing.Add($"{panel.name}/{PathUnder(panel, button.transform)} has {captions.Length} captions, not 1");
                        continue;
                    }
                    measured.Add(Measure(panel.name + "/" + PathUnder(panel, button.transform), button, captions[0]));
                }
            }
            int expected = panels.Sum(panel => panel.Names.Length);
            foreach (Measurement measurement in measured) Record($"{width}x{height} {state}: {measurement}");

            string[] unshown = measured.Where(measurement => !measurement.Shown)
                .Select(measurement => measurement.Name + $" (active {measurement.Active}, text \"{measurement.Caption}\")")
                .ToArray();
            string[] results = measured.Where(measurement => measurement.Glyphs.HasValue && !measurement.Centred)
                .Select(measurement => "off centre: " + measurement)
                .Concat(measured.Where(measurement => measurement.Glyphs.HasValue && !measurement.Clear)
                    .Select(measurement => "too close to the edge: " + measurement))
                .ToArray();

            Assert.IsEmpty(missing, $"fixture: each shown panel holds exactly its GuildWindowLayout.Button buttons, each with one caption ({state})");
            Assert.AreEqual(expected, measured.Count, $"fixture: {expected} button captions measured ({state})");
            Assert.IsEmpty(unshown, $"every caption is active, has text and draws visible characters ({width}x{height} {state})");
            Assert.IsEmpty(results, $"S4 every caption's glyph box is centred within max({CentreFloor} px, {CentreShare} x height) " +
                $"and at least {MarginShare} x height inside the button's bottom and top ({width}x{height} {state})");
        }

        // One caption against its button in texture pixels; Glyphs is null when no character is visible.
        Measurement Measure(string name, Button button, TMP_Text caption)
        {
            GuildSizeSurface surface = _stage.Surface;
            caption.ForceMeshUpdate();
            Rect box = surface.PixelRect((RectTransform)button.transform);
            Rect? glyphs = null;
            if (TryGlyphArea(caption, out Vector2 min, out Vector2 max))
                glyphs = surface.PixelRect(caption.transform,
                    new[] { new Vector3(min.x, min.y), new Vector3(max.x, max.y), new Vector3(min.x, max.y), new Vector3(max.x, min.y) });
            return new Measurement(name, caption, box, glyphs);
        }

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

        // A sibling of the caption with its rect, font, sizes, wrapping, colour and text, set to Midline.
        static TMP_Text MidlineCopy(TMP_Text caption)
        {
            var source = (RectTransform)caption.transform;
            var holder = new GameObject(caption.name + " Midline copy (test)", typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition;
            rect.localScale = source.localScale;
            var copy = holder.AddComponent<TextMeshProUGUI>();
            copy.font = caption.font;
            copy.fontSharedMaterial = caption.fontSharedMaterial;
            copy.fontStyle = caption.fontStyle;
            copy.enableAutoSizing = caption.enableAutoSizing;
            copy.fontSizeMin = caption.fontSizeMin;
            copy.fontSizeMax = caption.fontSizeMax;
            copy.fontSize = caption.fontSize;
            copy.textWrappingMode = caption.textWrappingMode;
            copy.overflowMode = caption.overflowMode;
            copy.margin = caption.margin;
            copy.color = caption.color;
            copy.richText = caption.richText;
            copy.raycastTarget = false;
            copy.alignment = TextAlignmentOptions.Midline;
            copy.text = caption.text;
            return copy;
        }

        static void Record(string line)
        {
            TestContext.Out.WriteLine("[guild button label] " + line);
            Debug.Log("[guild button label] " + line);
        }

        sealed class Measurement
        {
            internal Measurement(string name, TMP_Text caption, Rect button, Rect? glyphs)
            {
                Name = name;
                Caption = caption.text;
                Active = caption.isActiveAndEnabled;
                FontSize = caption.fontSize;
                AutoSize = caption.enableAutoSizing;
                ButtonRect = button;
                Glyphs = glyphs;
                CentreLimit = Mathf.Max(CentreFloor, CentreShare * button.height);
                MarginLimit = MarginShare * button.height;
                if (!glyphs.HasValue) return;
                CentreOffset = glyphs.Value.center.y - button.center.y;
                Below = glyphs.Value.yMin - button.yMin;
                Above = button.yMax - glyphs.Value.yMax;
            }

            internal string Name { get; }

            internal string Caption { get; }

            internal bool Active { get; }

            internal float FontSize { get; }

            internal bool AutoSize { get; }

            internal Rect ButtonRect { get; }

            internal Rect? Glyphs { get; }

            internal float CentreLimit { get; }

            internal float MarginLimit { get; }

            // Glyph box centre minus button centre; negative sits low.
            internal float CentreOffset { get; }

            internal float Below { get; }

            internal float Above { get; }

            internal bool Shown => Active && !string.IsNullOrEmpty(Caption) && Glyphs.HasValue;

            internal bool Centred => Glyphs.HasValue && Mathf.Abs(CentreOffset) <= CentreLimit;

            internal bool Clear => Glyphs.HasValue && Below >= MarginLimit && Above >= MarginLimit;

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "{0} \"{1}\" font {2:F2} (auto size {3}) button {4} glyphs {5} centre offset {6:F2} (limit {7:F2}) " +
                "below {8:F2} above {9:F2} (limit {10:F2})",
                Name, Caption, FontSize, AutoSize, Format(ButtonRect), Glyphs.HasValue ? Format(Glyphs.Value) : "none",
                CentreOffset, CentreLimit, Below, Above, MarginLimit);
        }
    }
}
