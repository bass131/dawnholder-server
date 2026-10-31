using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.PlayMode.GuildSizeGeometry;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Every text of the guild and blacksmith windows is whole and readable where it is drawn, written before the
    // implementation (goal 2026-10-30-login-screen-guild-window completion row 8; lead contract pr-b-red-contract-v2 T8,
    // supplement msg_107ba9cc8f94). Judged in batchmode without a game view, runner permit, DAWNHOLDER_GUILD_* variable or
    // server: the windows draw into 1920x1080 and 1280x720 textures (GuildSizeStage). For each shown text after
    // TMP_Text.ForceMeshUpdate: the visible characters are the supplied ones (textInfo.characterInfo isVisible, no
    // U+2026, isTextTruncated and isTextOverflowing false, no OnMissingCharacter), and the glyph box (characterInfo
    // bottomLeft..topRight, as CharacterMenuViewCaptureTests.cs:259-274) stays in its own rect and panel, apart from the
    // other texts, the drawn picture and every button that is not its own.
    public sealed class GuildWindowSizeTextFitTests
    {
        // As CharacterMenuViewCaptureTests.cs:27, in the text's own units.
        const float LocalTolerance = 0.5f;
        const float PixelTolerance = 1f;
        const char Ellipsis = '…';

        static readonly TextState[] s_guildOnly =
        {
            new TextState("level 1", () => GuildSizeValues.Level(1), GuildSizeValues.Smith, false),
            new TextState("level 2", () => GuildSizeValues.Level(2), GuildSizeValues.Smith, false),
            new TextState("longest at level 2", GuildSizeValues.LongLevelTwo, GuildSizeValues.LongSmith, true),
            new TextState("longest at level 254 of 255", GuildSizeValues.LongUpperLevel, GuildSizeValues.LongSmith, true),
        };

        static readonly TextState[] s_bothOpen =
        {
            new TextState("level 1 and blacksmith", () => GuildSizeValues.Level(1), GuildSizeValues.Smith, false),
            new TextState("longest at level 2 and longest blacksmith", GuildSizeValues.LongLevelTwo, GuildSizeValues.LongSmith, true),
            new TextState("longest at level 254 and longest blacksmith", GuildSizeValues.LongUpperLevel, GuildSizeValues.LongSmith, true),
        };

        readonly GuildSizeStage _stage = new GuildSizeStage();
        readonly List<(TMP_Text Text, int Unicode)> _missing = new List<(TMP_Text Text, int Unicode)>();

        [SetUp]
        public void Setup()
        {
            _missing.Clear();
            TMP_Text.OnMissingCharacter += RecordMissingCharacter;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            TMP_Text.OnMissingCharacter -= RecordMissingCharacter;
            _stage.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuildOnly_1920x1080_EveryStateText_IsWholeInsideItsRectAndApart()
        {
            yield return JudgeStates(1920, 1080, s_guildOnly, smith: false);
        }

        [UnityTest]
        public IEnumerator GuildOnly_1280x720_EveryStateText_IsWholeInsideItsRectAndApart()
        {
            yield return JudgeStates(1280, 720, s_guildOnly, smith: false);
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_EveryStateText_IsWholeInsideItsRectAndApart()
        {
            yield return JudgeStates(1920, 1080, s_bothOpen, smith: true);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_EveryStateText_IsWholeInsideItsRectAndApart()
        {
            yield return JudgeStates(1280, 720, s_bothOpen, smith: true);
        }

        // Negative controls (contract T8): each damage is found where it is made and gone once restored, so a clean
        // result above is the judgement's, not a judgement that sees nothing. Judged in the same frame as the damage, so
        // the windows' own LateUpdate cannot undo it first.
        [UnityTest]
        public IEnumerator NegativeControls_1920x1080_OverlappingCutAndCoveringTexts_AreFound()
        {
            yield return _stage.Open(1920, 1080, GuildSizeValues.Level(1), GuildSizeValues.Smith(), guild: true, smith: false);
            TMP_Text members = Label("MemberCount");
            TMP_Text online = Label("OnlineCount");
            TMP_Text treasury = Label("Treasury");
            TMP_Text name = Label("GuildName");
            var problems = new List<string>();

            Vector2 membersAt = members.rectTransform.anchoredPosition;
            Control("overlap-text", members, problems,
                () => members.rectTransform.anchoredPosition = online.rectTransform.anchoredPosition,
                () => members.rectTransform.anchoredPosition = membersAt);

            TextOverflowModes overflow = treasury.overflowMode;
            bool autoSize = treasury.enableAutoSizing;
            float fontSize = treasury.fontSize;
            TextWrappingModes wrapping = treasury.textWrappingMode;
            Vector2 treasurySize = treasury.rectTransform.sizeDelta;
            Control("cut", treasury, problems,
                () =>
                {
                    treasury.enableAutoSizing = false;
                    treasury.fontSize = 22f;
                    treasury.textWrappingMode = TextWrappingModes.NoWrap;
                    treasury.overflowMode = TextOverflowModes.Ellipsis;
                    treasury.rectTransform.sizeDelta = new Vector2(40f, treasurySize.y);
                },
                () =>
                {
                    treasury.rectTransform.sizeDelta = treasurySize;
                    treasury.overflowMode = overflow;
                    treasury.textWrappingMode = wrapping;
                    treasury.fontSize = fontSize;
                    treasury.enableAutoSizing = autoSize;
                });

            Vector2 nameAt = name.rectTransform.anchoredPosition;
            Control("over-picture", name, problems,
                () => name.rectTransform.position = _stage.Windows.Picture.rectTransform.position,
                () => name.rectTransform.anchoredPosition = nameAt);

            Assert.IsEmpty(problems, "T8 negative controls: every damage is found and gone once restored");
        }

        IEnumerator JudgeStates(int width, int height, TextState[] states, bool smith)
        {
            var findings = new List<string>();
            foreach (TextState state in states)
            {
                yield return _stage.Open(width, height, state.Guild(), state.Smith(), guild: true, smith: smith, longestLines: state.LongestLines);
                string label = $"{_stage.Surface.Size} {(smith ? "both open" : "guild only")} {state.Name}";
                List<Finding> found = Judge(out int judged);
                Record($"{label}: {judged} texts judged, {found.Count} findings");
                Assert.Greater(judged, 0, $"precondition: {label} has texts to judge");
                findings.AddRange(found.Select(finding => $"{label}: {finding}"));
            }
            foreach (string finding in findings) Record(finding);

            Assert.IsEmpty(findings, "T8 every text is whole, inside its rect and panel, and apart from texts, picture and other buttons");
        }

        // The judgement for one opened state; texts with nothing to show are not counted.
        List<Finding> Judge(out int judged)
        {
            Canvas.ForceUpdateCanvases();
            GuildWindowsUnderTest windows = _stage.Windows;
            GuildSizeSurface surface = _stage.Surface;
            var panels = new List<RectTransform>();
            if (GuildWindowsUnderTest.IsShown(windows.Guild)) panels.Add(windows.GuildPanel);
            if (GuildWindowsUnderTest.IsShown(windows.Smith)) panels.Add(windows.SmithPanel);
            Image picture = windows.Picture;
            Rect? drawn = panels.Contains(windows.GuildPanel) && picture.isActiveAndEnabled && picture.sprite != null
                ? DrawnRect(surface, picture) : (Rect?)null;
            var buttons = panels.SelectMany(panel => panel.GetComponentsInChildren<Button>(false))
                .Select(button => (Button: button, Rect: surface.PixelRect((RectTransform)button.transform))).ToList();

            var findings = new List<Finding>();
            var glyphs = new List<(TMP_Text Text, string Name, Rect Rect)>();
            judged = 0;
            foreach (RectTransform panel in panels)
            {
                Rect panelRect = surface.PixelRect(panel);
                foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text)) continue;
                    judged++;
                    text.ForceMeshUpdate();
                    string name = panel.name + "/" + PathUnder(panel, text.transform);
                    int expected = text.text.Count(character => !char.IsWhiteSpace(character));
                    TMP_TextInfo info = text.textInfo;
                    int visible = 0;
                    bool ellipsis = false;
                    for (int i = 0; i < info.characterCount; i++)
                    {
                        TMP_CharacterInfo character = info.characterInfo[i];
                        if (character.character == Ellipsis && text.text.IndexOf(Ellipsis) < 0) ellipsis = true;
                        else if (character.isVisible) visible++;
                    }
                    if (visible != expected || ellipsis || text.isTextTruncated)
                        findings.Add(new Finding("cut", name, $"{visible} of {expected} characters visible, ellipsis {ellipsis}, " +
                            $"isTextTruncated {text.isTextTruncated} in \"{text.text}\""));
                    if (text.isTextOverflowing)
                        findings.Add(new Finding("overflow", name, $"isTextOverflowing from character {text.firstOverflowCharacterIndex}"));
                    if (!TryGlyphArea(text, out Vector2 min, out Vector2 max)) continue;
                    Rect own = text.rectTransform.rect;
                    if (min.x < own.xMin - LocalTolerance || max.x > own.xMax + LocalTolerance ||
                        min.y < own.yMin - LocalTolerance || max.y > own.yMax + LocalTolerance)
                        findings.Add(new Finding("outside-own", name, $"glyphs {min}..{max} in rect {own}"));
                    Rect area = surface.PixelRect(text.transform,
                        new[] { new Vector3(min.x, min.y), new Vector3(max.x, max.y), new Vector3(min.x, max.y), new Vector3(max.x, min.y) });
                    if (!Inside(area, panelRect, PixelTolerance))
                        findings.Add(new Finding("outside-panel", name, $"glyphs {Format(area)} in panel {Format(panelRect)}"));
                    if (drawn.HasValue && Overlaps(area, drawn.Value, PixelTolerance))
                        findings.Add(new Finding("over-picture", name, $"glyphs {Format(area)} on picture {Format(drawn.Value)}"));
                    foreach ((Button button, Rect rect) in buttons)
                        if (!text.transform.IsChildOf(button.transform) && Overlaps(area, rect, PixelTolerance))
                            findings.Add(new Finding("over-button", name, $"glyphs {Format(area)} on button {button.name} {Format(rect)}"));
                    glyphs.Add((text, name, area));
                }
            }
            for (int i = 0; i < glyphs.Count; i++)
                for (int j = i + 1; j < glyphs.Count; j++)
                    if (Overlaps(glyphs[i].Rect, glyphs[j].Rect, PixelTolerance))
                    {
                        findings.Add(new Finding("overlap-text", glyphs[i].Name, "with " + glyphs[j].Name));
                        findings.Add(new Finding("overlap-text", glyphs[j].Name, "with " + glyphs[i].Name));
                    }
            foreach ((TMP_Text text, int unicode) in _missing.Distinct())
                if (text != null && text.transform.IsChildOf(windows.Holder.transform))
                    findings.Add(new Finding("missing", text.name, $"U+{unicode:X4} has no glyph"));
            _missing.Clear();
            return findings;
        }

        // Found after the damage and not before or after the restore; findings for the damaged text only.
        void Control(string kind, TMP_Text target, List<string> problems, Action damage, Action restore)
        {
            string at = $"{kind} on {target.name}";
            bool before = Has(kind, target);
            damage();
            bool damaged = Has(kind, target);
            restore();
            bool restored = Has(kind, target);
            Record($"negative control {at}: before {before}, damaged {damaged}, restored {restored}");
            if (before) problems.Add($"{at}: found before the damage");
            if (!damaged) problems.Add($"{at}: not found after the damage");
            if (restored) problems.Add($"{at}: still found after the restore");
        }

        bool Has(string kind, TMP_Text target) =>
            Judge(out _).Any(finding => finding.Kind == kind && finding.Text.EndsWith("/" + PathUnder(_stage.Windows.GuildPanel, target.transform), StringComparison.Ordinal));

        TMP_Text Label(string name)
        {
            TMP_Text[] found = _stage.Windows.GuildPanel.GetComponentsInChildren<TMP_Text>(true).Where(text => text.name == name).ToArray();
            Assert.AreEqual(1, found.Length, $"fixture: one {name} text in the guild panel");
            return found[0];
        }

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent) =>
            _missing.Add((textComponent, unicode));

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

        static void Record(string line)
        {
            TestContext.Out.WriteLine("[guild text fit] " + line);
            Debug.Log("[guild text fit] " + line);
        }

        readonly struct Finding
        {
            internal Finding(string kind, string text, string detail)
            {
                Kind = kind;
                Text = text;
                Detail = detail;
            }

            internal string Kind { get; }

            internal string Text { get; }

            internal string Detail { get; }

            public override string ToString() => $"{Kind} {Text}: {Detail}";
        }

        sealed class TextState
        {
            internal TextState(string name, Func<GuildStatusDisplay> guild, Func<BlacksmithDisplay> smith, bool longestLines)
            {
                Name = name;
                Guild = guild;
                Smith = smith;
                LongestLines = longestLines;
            }

            internal string Name { get; }

            internal Func<GuildStatusDisplay> Guild { get; }

            internal Func<BlacksmithDisplay> Smith { get; }

            internal bool LongestLines { get; }
        }
    }
}
