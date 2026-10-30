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
    // The larger guild base picture, written before the implementation (goal 2026-10-30-login-screen-guild-window
    // 「만들 것」 PR-B and completion rows 3 and 8; lead contract pr-b-red-contract-v2 「설계 고정」 T1, T2 and T6,
    // supplement msg_107ba9cc8f94, answer msg_978f10e2d973). Both windows are built and opened as GuildUiController
    // builds them (GuildWindowsUnderTest) and drawn into 1920x1080 and 1280x720 RenderTextures (GuildSizeSurface), so
    // every rectangle is in texture pixels. Expected values are the goal's numbers and snapshots of base 7bc75898
    // computed from that commit's constants (E/pr-b-red-01/scripts/base-snapshot.mjs, output base-snapshot-01.json),
    // never this commit's layout code; the picture is measured from Unity's own mesh of the image.
    public sealed class GuildWindowSizeSceneTests
    {
        const float PixelTolerance = 1f;
        // 「설계 고정」: the picture box keeps the art's aspect within 1%.
        const float AspectTolerance = 0.01f;
        // Base 7bc75898 drawn picture at 1920x1080, guild window only: GuildWindowLayout.cs:11-13, :22, :41-42, :98-102 and
        // GuildHousingLayout.cs:25, :29-32 at that commit, art 1672x941. The lead's scripts/base-picture-size.mjs gives the
        // same size (580.44x326.67).
        static readonly Rect s_basePicture1920 = Rect.MinMaxRect(112.98f, 345.218f, 693.42f, 671.89f);
        const float BasePictureArea1920 = 189613.3f;
        // 「설계 고정」 and completion row 3: at least 2.5 times the base area, and the lead's width and height floors.
        const float TargetArea1920 = 474033.3f;
        const float TargetWidth1920 = 917.8f;
        const float TargetHeight1920 = 516.5f;
        const float TargetWidth1280 = 600f;
        // 「설계 고정」: with the blacksmith also open the picture is no smaller than now at 1920x1080.
        const float BothOpenWidth1920 = 580f;
        // Base 7bc75898 blacksmith panel: the right half of s_windowArea (GuildWindowLayout.cs:17, :32-35 at that commit),
        // 440x310 scaled by the same Resize.
        static readonly Rect s_baseSmith1920 = Rect.MinMaxRect(803.226f, 183.6f, 1462.374f, 648.0f);
        static readonly Rect s_baseSmith1280 = Rect.MinMaxRect(535.484f, 122.4f, 974.916f, 432.0f);
        // 「설계 고정」: with the blacksmith open the guild window shrinks into today's left column, s_housingArea at 7bc75898
        // (GuildWindowLayout.cs:22) as screen fractions.
        static readonly Rect s_leftColumn = Rect.MinMaxRect(0.02f, 0.02f, 0.40f, 0.82f);
        // This writer's measures of 「가운데 쪽」 and 「안쪽 폭 가득」 (report pr-b-red-01/report.md): the panel centre in
        // the screen's middle third, and at most 5% of the panel width left on either side of the picture.
        const float SideGapShare = 0.05f;

        readonly GuildSizeStage _stage = new GuildSizeStage();

        GuildWindowsUnderTest Windows => _stage.Windows;

        GuildSizeSurface Surface => _stage.Surface;

        static IEnumerable<Vector2Int> Sizes => new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) };

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Release();
            yield return null;
        }

        // T1: completion row 3, 1920x1080 at least 2.5 times the base picture area (and the lead's width and height).
        [UnityTest]
        public IEnumerator GuildOnly_1920x1080_LevelsOneAndTwo_PictureAreaIsAtLeast2_5TimesTheBase()
        {
            var problems = new List<string>();
            for (int level = 1; level <= 2; level++)
            {
                yield return Open(1920, 1080, GuildSizeValues.Level(level), guild: true, smith: false);
                AssertPictureShows(level);
                Rect drawn = DrawnRect(Surface, Windows.Picture);
                Record($"guild only 1920x1080 level {level}: picture drawn {Format(drawn)} area {Area(drawn):F0} " +
                    $"= {Area(drawn) / BasePictureArea1920:F3} x base");
                if (Area(drawn) < TargetArea1920) problems.Add($"level {level}: area {Area(drawn):F0} < {TargetArea1920:F0}");
                if (drawn.width < TargetWidth1920) problems.Add($"level {level}: width {drawn.width:F1} < {TargetWidth1920}");
                if (drawn.height < TargetHeight1920) problems.Add($"level {level}: height {drawn.height:F1} < {TargetHeight1920}");
                Release();
            }

            Assert.IsEmpty(problems, "T1 1920x1080: the picture is drawn at least 2.5 times the base area, 917.8x516.5 or more");
        }

        // T1: 1280x720, at least 600 pixels wide.
        [UnityTest]
        public IEnumerator GuildOnly_1280x720_LevelsOneAndTwo_PictureIsAtLeast600PixelsWide()
        {
            var problems = new List<string>();
            for (int level = 1; level <= 2; level++)
            {
                yield return Open(1280, 720, GuildSizeValues.Level(level), guild: true, smith: false);
                AssertPictureShows(level);
                Rect drawn = DrawnRect(Surface, Windows.Picture);
                Record($"guild only 1280x720 level {level}: picture drawn {Format(drawn)}");
                if (drawn.width < TargetWidth1280) problems.Add($"level {level}: width {drawn.width:F1} < {TargetWidth1280}");
                Release();
            }

            Assert.IsEmpty(problems, "T1 1280x720: the picture is drawn at least 600 pixels wide");
        }

        // T1 「설계 고정」: the picture box has the art's aspect within 1%, so the box and the drawn picture are the same.
        [UnityTest]
        public IEnumerator GuildOnly_BothSizes_PictureBoxKeepsTheArtAspectWithinOnePercent()
        {
            var problems = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                for (int level = 1; level <= 2; level++)
                {
                    yield return Open(size.x, size.y, GuildSizeValues.Level(level), guild: true, smith: false);
                    AssertPictureShows(level);
                    Image picture = Windows.Picture;
                    Rect box = Surface.PixelRect(picture.rectTransform);
                    Rect drawn = DrawnRect(Surface, picture);
                    float art = picture.sprite.rect.width / picture.sprite.rect.height;
                    float off = Mathf.Abs(box.width / box.height / art - 1f);
                    Record($"guild only {Surface.Size} level {level}: box {Format(box)} aspect off {off:P2}, drawn {Format(drawn)}, " +
                        $"drawn/box area {Area(drawn) / Area(box):F4}");
                    if (off > AspectTolerance) problems.Add($"{Surface.Size} level {level}: box aspect is {off:P2} off the art's");
                    if (Area(drawn) < Area(box) * (1f - AspectTolerance))
                        problems.Add($"{Surface.Size} level {level}: drawn {Area(drawn):F0} of the box's {Area(box):F0} px²");
                    Release();
                }
            }

            Assert.IsEmpty(problems, "T1 the picture box keeps the 1672:941 aspect within 1% and the picture fills it");
        }

        // T1 「설계 고정」: the picture spans the panel's inner width at its top: the information rows and the action
        // buttons lie below it, and the buttons below the rows (goal 「만들 것」 PR-B :57). The title and Close are window chrome
        // and are not ordered here.
        [UnityTest]
        public IEnumerator GuildOnly_BothSizes_PictureSpansThePanelAboveTheRows_AndTheButtonsAreLast()
        {
            var width = new List<string>();
            var order = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                yield return Open(size.x, size.y, GuildSizeValues.Level(1), guild: true, smith: false);
                AssertPictureShows(1);
                Look look = Look.Of(Surface, Windows.GuildPanel, Windows.Picture);
                Record(look.Describe("guild only " + Surface.Size));
                Rect picture = look.Picture.Value;
                float left = picture.xMin - look.Panel.xMin;
                float right = look.Panel.xMax - picture.xMax;
                if (left > look.Panel.width * SideGapShare || right > look.Panel.width * SideGapShare)
                    width.Add($"{Surface.Size}: {left:F1} px left and {right:F1} px right of the picture in a {look.Panel.width:F1} px panel");
                foreach ((string name, Rect rect) in look.InfoRows.Concat(look.ActionButtons))
                    if (rect.yMax > picture.yMin + PixelTolerance)
                        order.Add($"{Surface.Size}: {name} top {rect.yMax:F1} is above the picture bottom {picture.yMin:F1}");
                float lowestRow = look.InfoRows.Length == 0 ? float.MaxValue : look.InfoRows.Min(row => row.Rect.yMin);
                foreach ((string name, Rect rect) in look.ActionButtons)
                    if (rect.yMax > lowestRow + PixelTolerance)
                        order.Add($"{Surface.Size}: button {name} top {rect.yMax:F1} is above the lowest row bottom {lowestRow:F1}");
                Assert.Greater(look.InfoRows.Length, 0, "precondition: information rows were found");
                Assert.Greater(look.ActionButtons.Length, 0, "precondition: action buttons were found");
                Release();
            }

            Assert.IsEmpty(width, "T1 the picture spans the panel's inner width (at most 5% of the panel left on a side)");
            Assert.IsEmpty(order, "T1 the information rows and action buttons lie below the picture, the buttons last");
        }

        // T1 「설계 고정」: alone, the guild window takes a wide place towards the screen centre, wholly on screen, and is
        // landscape (goal 「만들 것」 PR-B :57 「창을 가로형으로」).
        [UnityTest]
        public IEnumerator GuildOnly_BothSizes_WindowIsLandscapeInTheMiddleThirdOnScreen()
        {
            var centre = new List<string>();
            var screen = new List<string>();
            var landscape = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                yield return Open(size.x, size.y, GuildSizeValues.Level(1), guild: true, smith: false);
                Rect panel = Surface.PixelRect(Windows.GuildPanel);
                Record($"guild only {Surface.Size}: panel {Format(panel)}, centre x {panel.center.x / Surface.Width:P1} of the width");
                if (panel.center.x < Surface.Width / 3f || panel.center.x > Surface.Width * 2f / 3f)
                    centre.Add($"{Surface.Size}: panel centre x {panel.center.x:F1} outside {Surface.Width / 3f:F1}..{Surface.Width * 2f / 3f:F1}");
                if (!Inside(panel, Surface.Whole, PixelTolerance)) screen.Add($"{Surface.Size}: panel {Format(panel)} leaves the screen");
                if (panel.width <= panel.height) landscape.Add($"{Surface.Size}: panel {panel.width:F1}x{panel.height:F1}");
                Release();
            }

            Assert.IsEmpty(screen, "T1 the guild window is wholly on screen");
            Assert.IsEmpty(centre, "T1 the guild window alone sits towards the screen centre (its centre in the middle third)");
            Assert.IsEmpty(landscape, "T1 the guild window alone is landscape (wider than tall)");
        }

        // T1: the picture, the text rows and the buttons are inside the panel and do not overlap one another (their
        // RectTransform rectangles; glyph overlap and clipping are T8, GuildWindowSizeTextFitTests).
        [UnityTest]
        public IEnumerator GuildOnly_BothSizes_PictureRowsAndButtonsStayInsideThePanelApart()
        {
            var inside = new List<string>();
            var apart = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                for (int level = 1; level <= 2; level++)
                {
                    yield return Open(size.x, size.y, GuildSizeValues.Level(level), guild: true, smith: false);
                    AssertPictureShows(level);
                    Look look = Look.Of(Surface, Windows.GuildPanel, Windows.Picture);
                    var parts = new List<(string Name, Rect Rect)> { ("picture", look.Picture.Value) };
                    parts.AddRange(look.Rows);
                    parts.AddRange(look.Buttons);
                    string at = $"{Surface.Size} level {level}";
                    foreach ((string name, Rect rect) in parts)
                        if (!Inside(rect, look.Panel, PixelTolerance)) inside.Add($"{at}: {name} {Format(rect)} leaves the panel");
                    for (int i = 0; i < parts.Count; i++)
                        for (int j = i + 1; j < parts.Count; j++)
                            if (Overlaps(parts[i].Rect, parts[j].Rect, PixelTolerance))
                                apart.Add($"{at}: {parts[i].Name} and {parts[j].Name} overlap");
                    Assert.Greater(parts.Count, 3, "precondition: the picture, rows and buttons were found");
                    Release();
                }
            }
            Record("inside: " + string.Join("; ", inside));
            Record("apart: " + string.Join("; ", apart));

            Assert.IsEmpty(inside, "T1 the picture, rows and buttons stay inside the guild panel");
            Assert.IsEmpty(apart, "T1 the picture, rows and buttons do not overlap one another");
        }

        // T1: shows whether the base snapshot above is what Unity draws. Before the enlargement the measured picture must
        // be that snapshot within a pixel; once the picture meets the 2.5x target this has nothing left to show.
        [UnityTest]
        public IEnumerator GuildOnly_1920x1080_BeforeTheEnlargement_PictureIsTheBaseSnapshot()
        {
            yield return Open(1920, 1080, GuildSizeValues.Level(1), guild: true, smith: false);
            AssertPictureShows(1);
            Rect drawn = DrawnRect(Surface, Windows.Picture);
            bool enlarged = Area(drawn) >= TargetArea1920 && drawn.width >= TargetWidth1920 && drawn.height >= TargetHeight1920;
            Record($"snapshot check: drawn {Format(drawn)}, snapshot {Format(s_basePicture1920)}, enlarged {enlarged}");

            if (!enlarged)
                Assert.IsTrue(SameRect(drawn, s_basePicture1920, PixelTolerance),
                    $"T1 snapshot: before the enlargement the picture is drawn at base {Format(s_basePicture1920)}, measured {Format(drawn)}");
        }

        // T2 「설계 고정」: opened in either order, the two panels are apart and on screen, the guild window lies in today's
        // left column, and at 1920x1080 its picture stays at least 580 pixels wide.
        [UnityTest]
        public IEnumerator BothOpen_BothSizes_EitherOrder_PanelsApartOnScreen_GuildInTheLeftColumn_PictureAtLeast580Wide()
        {
            var apart = new List<string>();
            var screen = new List<string>();
            var column = new List<string>();
            var picture = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                foreach (bool guildFirst in new[] { true, false })
                {
                    yield return OpenBoth(size.x, size.y, guildFirst);
                    Rect guild = Surface.PixelRect(Windows.GuildPanel);
                    Rect smith = Surface.PixelRect(Windows.SmithPanel);
                    Rect drawn = DrawnRect(Surface, Windows.Picture);
                    string at = $"{Surface.Size} {(guildFirst ? "guild first" : "blacksmith first")}";
                    Rect allowed = Rect.MinMaxRect(s_leftColumn.xMin * Surface.Width, s_leftColumn.yMin * Surface.Height,
                        s_leftColumn.xMax * Surface.Width, s_leftColumn.yMax * Surface.Height);
                    Record($"both open {at}: guild {Format(guild)}, blacksmith {Format(smith)}, picture {Format(drawn)}");
                    if (Overlaps(guild, smith, 0f)) apart.Add($"{at}: overlap {OverlapArea(guild, smith):F0} px²");
                    if (!Inside(guild, Surface.Whole, PixelTolerance) || !Inside(smith, Surface.Whole, PixelTolerance))
                        screen.Add($"{at}: a panel leaves the screen");
                    if (!Inside(guild, allowed, PixelTolerance)) column.Add($"{at}: guild {Format(guild)} outside {Format(allowed)}");
                    if (size.x == 1920 && drawn.width < BothOpenWidth1920) picture.Add($"{at}: picture {drawn.width:F1} px wide");
                    Release();
                }
            }

            Assert.IsEmpty(apart, "T2 the two panels do not overlap whichever opened first");
            Assert.IsEmpty(screen, "T2 both panels are wholly on screen");
            Assert.IsEmpty(column, "T2 with the blacksmith open the guild window lies in the left column (0.02..0.40 x 0.02..0.82)");
            Assert.IsEmpty(picture, "T2 at 1920x1080 the guild picture stays at least 580 pixels wide with both open");
        }

        // T2 「설계 고정」: the blacksmith window keeps its size and place: alone, with the guild window opened before or after
        // it, and as at base 7bc75898.
        [UnityTest]
        public IEnumerator BothOpen_BothSizes_BlacksmithKeepsItsAloneRect_AndTheBaseSnapshot()
        {
            var snapshot = new List<string>();
            var kept = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                Rect baseRect = size.x == 1920 ? s_baseSmith1920 : s_baseSmith1280;
                yield return Open(size.x, size.y, GuildSizeValues.Level(1), guild: false, smith: true);
                Rect alone = Surface.PixelRect(Windows.SmithPanel);
                Release();
                Record($"blacksmith alone {size.x}x{size.y}: {Format(alone)}, base {Format(baseRect)}");
                if (!SameRect(alone, baseRect, PixelTolerance)) snapshot.Add($"{size.x}x{size.y}: alone {Format(alone)}, base {Format(baseRect)}");
                foreach (bool guildFirst in new[] { true, false })
                {
                    yield return OpenBoth(size.x, size.y, guildFirst);
                    Rect both = Surface.PixelRect(Windows.SmithPanel);
                    Record($"blacksmith both open {Surface.Size} guild first {guildFirst}: {Format(both)}");
                    if (!SameRect(both, alone, PixelTolerance))
                        kept.Add($"{Surface.Size} guild first {guildFirst}: {Format(both)}, alone {Format(alone)}");
                    Release();
                }
            }

            Assert.IsEmpty(snapshot, "T2 the blacksmith window alone is where base 7bc75898 put it");
            Assert.IsEmpty(kept, "T2 the blacksmith window keeps its alone rectangle when the guild window is also open");
        }

        // T2 (answer msg_978f10e2d973 3): with both open, closing the blacksmith returns the guild window to the place and
        // size it has alone, picture included.
        [UnityTest]
        public IEnumerator BothOpen_BothSizes_ClosingTheBlacksmith_ReturnsTheGuildToItsAlonePlaceAndSize()
        {
            var problems = new List<string>();
            foreach (Vector2Int size in Sizes)
            {
                yield return Open(size.x, size.y, GuildSizeValues.Level(1), guild: true, smith: false);
                Rect alonePanel = Surface.PixelRect(Windows.GuildPanel);
                Rect alonePicture = DrawnRect(Surface, Windows.Picture);
                Windows.Smith.SetOpen(true);
                yield return GuildSizeSurface.Settle();
                Windows.Smith.SetOpen(false);
                yield return GuildSizeSurface.Settle();
                Rect backPanel = Surface.PixelRect(Windows.GuildPanel);
                Rect backPicture = DrawnRect(Surface, Windows.Picture);
                Record($"guild {Surface.Size}: alone {Format(alonePanel)} / {Format(alonePicture)}, after the blacksmith closed " +
                    $"{Format(backPanel)} / {Format(backPicture)}");
                if (!SameRect(backPanel, alonePanel, PixelTolerance)) problems.Add($"{Surface.Size}: panel {Format(backPanel)}, alone {Format(alonePanel)}");
                if (!SameRect(backPicture, alonePicture, PixelTolerance))
                    problems.Add($"{Surface.Size}: picture {Format(backPicture)}, alone {Format(alonePicture)}");
                Release();
            }

            Assert.IsEmpty(problems, "T2 closing the blacksmith returns the guild window to its guild-only place and size");
        }

        // T6 (completion row 8): the six captures. PNGs are written only when DAWNHOLDER_GUILD_SIZE_CAPTURE_DIR is set;
        // the only check is that the camera drew the windows, so the captures are left whatever the other tests find.
        [UnityTest]
        public IEnumerator Captures_GuildLevelsBlacksmithAndBoth_AreDrawn()
        {
            var empty = new List<string>();
            var shots = new (string File, int Width, int Height, int Level, bool Guild, bool Smith)[]
            {
                ("guild-level1-1920x1080.png", 1920, 1080, 1, true, false),
                ("guild-level2-1920x1080.png", 1920, 1080, 2, true, false),
                ("guild-level1-1280x720.png", 1280, 720, 1, true, false),
                ("guild-level2-1280x720.png", 1280, 720, 2, true, false),
                ("guild-and-smith-1920x1080.png", 1920, 1080, 1, true, true),
                ("smith-1920x1080.png", 1920, 1080, 1, false, true),
            };
            foreach (var shot in shots)
            {
                yield return Open(shot.Width, shot.Height, GuildSizeValues.Level(shot.Level), shot.Guild, shot.Smith);
                yield return new WaitForSecondsRealtime(0.25f);
                string record = Surface.Capture(shot.File, out float drawn);
                Record("capture " + record);
                if (drawn < 0.02f) empty.Add($"{shot.File}: {drawn:P2} of the frame drawn");
                Release();
            }

            Assert.IsEmpty(empty, "T6 fixture: the camera drew the windows in every capture");
        }

        IEnumerator Open(int width, int height, GuildStatusDisplay value, bool guild, bool smith) =>
            _stage.Open(width, height, value, GuildSizeValues.Smith(), guild, smith);

        // Answer msg_978f10e2d973 3: both orders through each window's own SetOpen only.
        IEnumerator OpenBoth(int width, int height, bool guildFirst)
        {
            yield return Open(width, height, GuildSizeValues.Level(1), guild: guildFirst, smith: !guildFirst);
            if (guildFirst) Windows.Smith.SetOpen(true);
            else Windows.Guild.SetOpen(true);
            yield return GuildSizeSurface.Settle();
            Assert.IsTrue(GuildWindowsUnderTest.IsShown(Windows.Guild) && GuildWindowsUnderTest.IsShown(Windows.Smith),
                "fixture: both windows are shown");
            AssertPictureShows(1);
        }

        void AssertPictureShows(int level)
        {
            Sprite expected = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel" + level);
            Assert.IsNotNull(expected, $"fixture: the GuildBaseLevel{level} resource");
            Assert.AreEqual(new Vector2(ArtWidth, ArtHeight), expected.rect.size, "fixture: the art is 1672x941");
            Image picture = Windows.Picture;
            Assert.IsTrue(picture.isActiveAndEnabled, $"precondition: the level {level} picture is shown");
            Assert.AreSame(expected, picture.sprite, $"precondition: the picture shows GuildBaseLevel{level}");
        }

        void Release() => _stage.Dispose();

        static void Record(string line)
        {
            TestContext.Out.WriteLine("[guild size] " + line);
            Debug.Log("[guild size] " + line);
        }

        // One observation of a panel: its rectangle, the drawn picture and box, the text rows (non-empty texts that are
        // not a button's own label) and the buttons, all in texture pixels. Title and Close are the window chrome
        // (GuildWindowLayout.cs:54 and :73).
        sealed class Look
        {
            Look(Rect panel, Rect? picture, Rect? box, (string Name, Rect Rect)[] rows, (string Name, Rect Rect)[] buttons)
            {
                Panel = panel;
                Picture = picture;
                Box = box;
                Rows = rows;
                Buttons = buttons;
            }

            internal Rect Panel { get; }

            internal Rect? Picture { get; }

            internal Rect? Box { get; }

            internal (string Name, Rect Rect)[] Rows { get; }

            internal (string Name, Rect Rect)[] Buttons { get; }

            internal (string Name, Rect Rect)[] InfoRows => Rows.Where(row => row.Name != "Title").ToArray();

            internal (string Name, Rect Rect)[] ActionButtons => Buttons.Where(button => button.Name != "Close").ToArray();

            internal static Look Of(GuildSizeSurface surface, RectTransform panel, Image picture)
            {
                bool shown = picture != null && picture.isActiveAndEnabled && picture.sprite != null;
                var rows = panel.GetComponentsInChildren<TMP_Text>(false)
                    .Where(text => text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text) && text.GetComponentInParent<Button>(true) == null)
                    .Select(text => (PathUnder(panel, text.transform), surface.PixelRect(text.rectTransform))).ToArray();
                var buttons = panel.GetComponentsInChildren<Button>(false)
                    .Select(button => (button.name, surface.PixelRect((RectTransform)button.transform))).ToArray();
                return new Look(surface.PixelRect(panel), shown ? DrawnRect(surface, picture) : (Rect?)null,
                    shown ? surface.PixelRect(picture.rectTransform) : (Rect?)null, rows, buttons);
            }

            internal string Describe(string state) =>
                $"{state}: panel {Format(Panel)}, picture {(Picture.HasValue ? Format(Picture.Value) : "none")}, box " +
                $"{(Box.HasValue ? Format(Box.Value) : "none")}, rows [{string.Join("; ", Rows.Select(row => row.Name + " " + Format(row.Rect)))}], " +
                $"buttons [{string.Join("; ", Buttons.Select(button => button.Name + " " + Format(button.Rect)))}]";
        }
    }
}
