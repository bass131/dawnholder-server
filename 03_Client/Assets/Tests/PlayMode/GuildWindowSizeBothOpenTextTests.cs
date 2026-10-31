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
    // With both windows open, each guild text is drawn as large on screen as the blacksmith texts of the same role,
    // written before the fix (goal 2026-10-30-login-screen-guild-window PR-B; lead contract pr-b-testfix-contract-v1.md S5,
    // its 본문 line changed by the lead's answer msg_336daf54e3fe). Judged in batchmode: the windows draw into 1920x1080
    // and 1280x720 textures (GuildSizeStage). A text's screen size is its fontSize after TMP_Text.ForceMeshUpdate (under
    // auto size the size TMP applied) times its local-to-pixel scale: the texture-pixel height of a ScaleSpan-unit
    // vertical span in the text's own space (GuildSizeSurface.PixelRect, RectTransformUtility.WorldToScreenPoint), which
    // carries the canvas scale and every parent's localScale. Roles: Title, the caption of a Button, a *Notice line
    // (GuildWindowLayout.Notice), and Body for every other shown text. This does not replace the RED cases of
    // GuildWindowSizeSceneTests.
    public sealed class GuildWindowSizeBothOpenTextTests
    {
        // Contract pr-b-testfix-contract-v1.md S5 「역할과 기준」 (main decision msg_0ef90a8302b0 condition 1, fixed before
        // the run): |guild - reference| <= SizeTolerance x reference, the reference being the blacksmith role's median.
        const float SizeTolerance = 0.05f;
        // Any span gives the same ratio; 100 keeps the rounding of the pixel corners small.
        const float ScaleSpan = 100f;

        enum Role
        {
            Title,
            Body,
            Notice,
            Button,
        }

        readonly GuildSizeStage _stage = new GuildSizeStage();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _stage.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_GuildTitle_IsTheBlacksmithTitleSizeOnScreen()
        {
            yield return Compare(1920, 1080, Role.Title);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_GuildTitle_IsTheBlacksmithTitleSizeOnScreen()
        {
            yield return Compare(1280, 720, Role.Title);
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_GuildBodyTexts_AreTheBlacksmithBodySizeOnScreen()
        {
            yield return Compare(1920, 1080, Role.Body);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_GuildBodyTexts_AreTheBlacksmithBodySizeOnScreen()
        {
            yield return Compare(1280, 720, Role.Body);
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_GuildNotices_AreTheBlacksmithNoticeSizeOnScreen()
        {
            yield return Compare(1920, 1080, Role.Notice);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_GuildNotices_AreTheBlacksmithNoticeSizeOnScreen()
        {
            yield return Compare(1280, 720, Role.Notice);
        }

        [UnityTest]
        public IEnumerator BothOpen_1920x1080_GuildButtonCaptions_AreTheBlacksmithButtonCaptionSizeOnScreen()
        {
            yield return Compare(1920, 1080, Role.Button);
        }

        [UnityTest]
        public IEnumerator BothOpen_1280x720_GuildButtonCaptions_AreTheBlacksmithButtonCaptionSizeOnScreen()
        {
            yield return Compare(1280, 720, Role.Button);
        }

        IEnumerator Compare(int width, int height, Role role)
        {
            yield return _stage.Open(width, height, GuildSizeValues.Level(1), GuildSizeValues.Smith(), guild: true, smith: true);
            ScreenSize[] guild = Sizes(_stage.Windows.GuildPanel, role);
            ScreenSize[] smith = Sizes(_stage.Windows.SmithPanel, role);
            float reference = smith.Length > 0 ? Median(smith.Select(size => size.Pixels)) : 0f;
            string at = $"{width}x{height} {role}";
            Record(string.Format(CultureInfo.InvariantCulture, "{0}: blacksmith reference {1:F2} px, the median of {2} text(s){3}",
                at, reference, smith.Length, smith.Length == 1 ? " (one text, so its own size)" : string.Empty));
            foreach (ScreenSize size in smith) Record($"{at}: blacksmith {size}");
            foreach (ScreenSize size in guild)
                Record(string.Format(CultureInfo.InvariantCulture, "{0}: guild {1}, {2:F3} of the reference", at, size, size.Pixels / reference));
            string[] outside = guild.Where(size => Mathf.Abs(size.Pixels - reference) > SizeTolerance * reference)
                .Select(size => string.Format(CultureInfo.InvariantCulture, "{0}, {1:F3} of {2:F2} px", size, size.Pixels / reference, reference))
                .ToArray();

            Assert.Greater(guild.Length, 0, $"fixture: the guild window shows at least one {role} text ({at})");
            Assert.Greater(smith.Length, 0, $"fixture: the blacksmith window shows at least one {role} text ({at})");
            Assert.IsEmpty(outside, string.Format(CultureInfo.InvariantCulture,
                "S5 every guild {0} text is within {1:P0} of the blacksmith {0} median {2:F2} px on screen ({3})",
                role, SizeTolerance, reference, at));
        }

        // The shown, non-empty texts of one role in a panel with their screen sizes.
        ScreenSize[] Sizes(RectTransform panel, Role role)
        {
            GuildSizeSurface surface = _stage.Surface;
            return panel.GetComponentsInChildren<TMP_Text>(false)
                .Where(text => text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text) && RoleOf(text) == role)
                .Select(text =>
                {
                    text.ForceMeshUpdate();
                    Rect span = surface.PixelRect(text.transform,
                        new[] { Vector3.zero, new Vector3(0f, ScaleSpan, 0f), new Vector3(ScaleSpan, 0f, 0f) });
                    return new ScreenSize(panel.name + "/" + PathUnder(panel, text.transform), text, span.height / ScaleSpan,
                        span.width / ScaleSpan);
                })
                .ToArray();
        }

        static Role RoleOf(TMP_Text text)
        {
            if (text.GetComponentInParent<Button>(true) != null) return Role.Button;
            if (text.name == "Title") return Role.Title;
            return text.name.EndsWith("Notice", StringComparison.Ordinal) ? Role.Notice : Role.Body;
        }

        static float Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(value => value).ToArray();
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2f;
        }

        static void Record(string line)
        {
            TestContext.Out.WriteLine("[guild both-open text size] " + line);
            Debug.Log("[guild both-open text size] " + line);
        }

        sealed class ScreenSize
        {
            internal ScreenSize(string name, TMP_Text text, float scale, float scaleAcross)
            {
                Name = name;
                Caption = text.text;
                FontSize = text.fontSize;
                AutoSize = text.enableAutoSizing;
                SizeMin = text.fontSizeMin;
                SizeMax = text.fontSizeMax;
                Scale = scale;
                ScaleAcross = scaleAcross;
            }

            internal string Name { get; }

            internal string Caption { get; }

            // Under auto size, the size TMP applied in the last mesh update.
            internal float FontSize { get; }

            internal bool AutoSize { get; }

            internal float SizeMin { get; }

            internal float SizeMax { get; }

            // Texture pixels per local unit, measured up the text's own y axis (ScaleAcross: along x, recorded only).
            internal float Scale { get; }

            internal float ScaleAcross { get; }

            internal float Pixels => FontSize * Scale;

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "{0} \"{1}\" font {2:F2} (auto size {3}, {4:F1}..{5:F1}) x scale {6:F4} (across {7:F4}) = {8:F2} px",
                Name, Caption, FontSize, AutoSize, SizeMin, SizeMax, Scale, ScaleAcross, Pixels);
        }
    }
}
