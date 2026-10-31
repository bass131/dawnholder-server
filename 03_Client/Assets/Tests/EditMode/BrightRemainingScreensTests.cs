using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // PR4 (goal 2026-10-29-inventory-character-select-look ④, 완료조건 11): the visible screens that do not draw through the
    // shared skin move to the bright kit. Main decision 숨은 HUD A (msg_8a141e965681) limits PR4 to the pause menu, the map
    // name band, the HP bar, the quest granted popup and the ending hint; the MP and EXP bars and skill slots 3 to 5 stay
    // as they are, hidden in game, so nothing here reads them.
    // Each text must reach the WCAG 2.x SC 1.4.3 「Contrast (Minimum)」 ratio for its drawn size on the bright piece drawn
    // behind it (main 버튼 글자 A msg_faeee7b36f9e, Content 조건 A msg_1f6fd458913d). The drawn size comes from the real
    // font size, weight, auto sizing and Canvas scaler; the backdrop colour comes from the piece's own PNG file. No current
    // or new colour, size or ratio is an expectation (main 글자 색 A msg_77a7e9b3e05c 조건 3); colours appear only as the
    // controls' inputs and as the white Image colour main set for the pause buttons, the map band and the HP track.
    // The scene cases open the real Scenes/99.UI/UI.unity next to the runner's scene and close it without saving; no
    // product code rewrites these Images' sprite or colour while the game runs (pr4-red-01/raw/runtime-overwrites-01.txt),
    // so the saved values are what the screen draws. The code cases call the product builders: the public static
    // QuestGrantedPopup.BuildRuntime and EndingController's private BuildHint, which Update calls after _armDelay.
    // CanvasGroup alpha (the popup's fade, the hint's pulse) dims a text and the band behind it alike and is left out.
    public sealed class BrightRemainingScreensTests
    {
        const string UiScenePath = "Assets/Scenes/99.UI/UI.unity";
        // The brown B1 folder, read only by the control.
        const string BrownFolder = "Assets/Art/UI/Components";

        // WCAG 2.x SC 1.4.3 Contrast (Minimum): 4.5:1 for text, 3:1 for large-scale text.
        const double BodyTextContrast = 4.5;
        const double LargeTextContrast = 3.0;
        // WCAG 2.x definition 「large scale (text)」: at least 18 point, or 14 point bold, with 1 pt = 4/3 CSS px; main
        // msg_faeee7b36f9e sets the bounds at 24 px and, bold, 18.66 px of drawn size.
        const double LargeTextPixels = 24.0;
        const double LargeBoldTextPixels = 18.66;
        // Main msg_faeee7b36f9e 조건 1: a scale-with-screen canvas is judged at the smaller scale of its reference
        // resolution and the smallest supported screen, 1280×720.
        static readonly Vector2 s_smallestScreen = new Vector2(1280f, 720f);
        // Lead design (contract 「대비와 크기 기준」): a backdrop pixel counts when its alpha is at least 0.5, 128 of 255.
        const byte OpaqueAlpha = 128;

        // Contract 「대상과 바꿀 모양」: the bright piece each target is meant to draw.
        const string ButtonPiece = "Button_Default_Normal";
        const string PanelPiece = "Panel_Default";
        const string HeaderPiece = "Header";
        const string InsetPiece = "Inset_Default";
        const string BarTrackPiece = "Bar_Track";
        const string BarFillPiece = "Bar_Fill";

        const string MenuPanelPath = "PauseMenuCanvas/MenuRoot/MenuPanel";
        static readonly string[] s_pauseButtons =
        {
            MenuPanelPath + "/ResumeButton",
            MenuPanelPath + "/MainMenuButton",
            MenuPanelPath + "/QuitButton",
        };
        const string MapNamePath = "HUD/MinimapPlaceholder/Map_Name";
        const string MapNameTextPath = MapNamePath + "/Text";
        const string HpTrackPath = "HUD/character_status/HpSlider/Background";
        const string HpFillPath = "HUD/character_status/HpSlider/Fill Area/Fill";

        // The new bar pieces as the approved kit spec gives them (contract RS4), from
        // .backups/verification/2026-10-29-inventory-character-select-look/bright-kit-v2/kit-spec.json
        // (sha256 7853b3a5fd89afcf8156f18169a97a676e58281d772f9049de95e3bead4bfae2): Bar_Track :258, pixelsPerUnit :269,
        // borderLRTB :272~:276; Bar_Fill :288, pixelsPerUnit :299, borderLRTB :302~:306 (pr4-red-01/raw/kit-spec-lines-01.txt).
        // Unity's spriteBorder is (left, bottom, right, top) in PNG pixels.
        static readonly (string Piece, float PixelsPerUnit, Vector4 Border)[] s_barPieces =
        {
            (BarTrackPiece, 200f, new Vector4(12f, 12f, 12f, 12f)),
            (BarFillPiece, 200f, new Vector4(6f, 6f, 6f, 6f)),
        };

        // Control inputs only (contract RS7), each from the line that uses it today: white, QuestGrantedPopup.cs:120 and
        // EndingController.cs:116; the bright windows' ink, InventoryPanelView.cs:20.
        static readonly Color s_white = Color.white;
        static readonly Color s_ink = new Color(0.26f, 0.17f, 0.09f);

        readonly List<Action> _cleanups = new List<Action>();

        // One observed text: what is drawn behind it, its drawn size and the resulting contrast. Strings and values only,
        // so a reading outlives the scene it was taken from.
        sealed class Reading
        {
            internal string Scope;
            internal string TextPath;
            internal Color Text;
            internal string ExpectedPiece;
            internal string Backdrop;   // path of the Image drawn behind the text; null when none is
            internal string Piece;      // that Image's sprite name
            internal string Asset;      // that sprite's asset path
            internal bool Bright;       // the asset sits in the bright folder under the expected piece name
            internal int OpaquePixels;
            internal Color Mean;        // mean opaque colour inside the sprite border × the Image colour
            internal double Ratio = double.NaN;
            internal string Problem;    // why the backdrop colour could not be read
            internal float FontSize;    // fontSizeMin when auto sizing is on
            internal bool Bold;
            internal string Scaler;
            internal double Scale = double.NaN;
            internal double DrawnPixels = double.NaN;
            internal double Threshold = double.NaN;
            internal string ScaleProblem; // why the canvas scale could not be read
        }

        // One target Image as the scene or the builder left it.
        sealed class ImageState
        {
            internal string Path;
            internal string ExpectedPiece;
            internal bool Present;
            internal string Piece;
            internal string Asset;
            internal bool Bright;
            internal Color Colour;
            internal Image.Type Type;
            internal bool Raycast;
        }

        [TearDown]
        public void Teardown()
        {
            Exception first = null;
            for (int i = _cleanups.Count - 1; i >= 0; i--)
            {
                try { _cleanups[i](); }
                catch (Exception error) { first ??= error; }
            }
            _cleanups.Clear();
            if (first != null) throw first;
        }

        // RS1 preserved: the three pause buttons keep the ColorTint transition on their own Image, whose tint values the
        // change leaves alone (main msg_8a141e965681 조건 2, msg_faeee7b36f9e 조건 3).
        [Test]
        public void PauseButtons_KeepTheColorTintTransitionOnTheirOwnImage()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            Dictionary<string, Transform[]> found = FindPaths(scene, s_pauseButtons);
            AssertFoundOnce(found, "pause menu button");
            string[] rows = s_pauseButtons.Select(path => TintOf(found[path][0])).ToArray();
            foreach (string row in rows) TestContext.Out.WriteLine($"remaining-tint | {row}");
            CloseScenes();

            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            Assert.IsEmpty(rows.Where(row => !row.EndsWith("ok", StringComparison.Ordinal)).ToArray(),
                "every pause button keeps a ColorTint transition aimed at its own Image");
        }

        // RS1: the three pause buttons draw the bright Button_Default_Normal in white, and each label reaches the ratio
        // for its drawn size on the piece found behind it.
        [Test]
        public void PauseButtons_DrawTheBrightNormalButtonInWhite_AndTheirTextsReachTheirSizeThreshold()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            Dictionary<string, Transform[]> found = FindPaths(scene, s_pauseButtons);
            AssertFoundOnce(found, "pause menu button");
            ImageState[] images = s_pauseButtons.Select(path => StateOf("pause menu", found[path][0], ButtonPiece)).ToArray();
            Reading[] readings = s_pauseButtons
                .SelectMany(path => TextsUnder("pause menu", found[path][0], 1, ButtonPiece)).ToArray();
            CloseScenes();

            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            Assert.AreEqual(s_pauseButtons.Length, readings.Length, "precondition: every pause button label is observed");
            Assert.IsEmpty(NotBright(images),
                $"every pause button Image draws the bright {ButtonPiece} from {BrightUiKitTestSupport.ArtFolder}");
            Assert.IsEmpty(NotWhite(images), "every pause button Image colour is white (main msg_faeee7b36f9e 조건 3)");
            AssertMeasured(readings);
            Assert.IsEmpty(BelowThreshold(readings),
                "every pause button label reaches the WCAG 2.x SC 1.4.3 ratio for its drawn size on the piece behind it");
        }

        // RS2: the menu panel gains the bright panel behind the buttons: sliced, white, outside the raycast (main
        // msg_8a141e965681 조건 3), and drawn before the three buttons, which are its children.
        [Test]
        public void PauseMenuPanel_DrawsTheBrightPanelInWhite_OutsideTheRaycast_BeforeItsButtons()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            Dictionary<string, Transform[]> found = FindPaths(scene, s_pauseButtons.Prepend(MenuPanelPath));
            AssertFoundOnce(found, "pause menu object");
            Transform panel = found[MenuPanelPath][0];
            ImageState state = StateOf("pause menu", panel, PanelPiece);
            string[] order = state.Present
                ? s_pauseButtons.Select(path => DrawnAfter(panel.GetComponent<Image>(), found[path][0])).ToArray()
                : Array.Empty<string>();
            foreach (string row in order) TestContext.Out.WriteLine($"remaining-order | {row}");
            CloseScenes();

            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            Assert.IsTrue(state.Present, $"{MenuPanelPath} has an Image ({Describe(state)})");
            Assert.IsEmpty(NotBright(new[] { state }), $"the menu panel draws the bright {PanelPiece}");
            Assert.AreEqual(Image.Type.Sliced, state.Type, $"the menu panel Image is sliced ({Describe(state)})");
            Assert.IsEmpty(NotWhite(new[] { state }), "the menu panel Image colour is white");
            Assert.IsFalse(state.Raycast,
                $"the menu panel Image takes no raycast (main msg_8a141e965681 조건 3; {Describe(state)})");
            Assert.AreEqual(s_pauseButtons.Length, order.Length, "precondition: the draw order of every pause button is read");
            Assert.IsEmpty(order.Where(row => !row.EndsWith("ok", StringComparison.Ordinal)).ToArray(),
                "the menu panel Image is drawn before every pause button, each of them under the panel");
        }

        // RS3: the map name band draws the bright Header in white and its text reaches the ratio for its drawn size. The
        // HUD canvas scales with the screen, so the size is judged at the smaller of 1920×1080 and 1280×720.
        [Test]
        public void MapNameBand_DrawsTheBrightHeaderInWhite_AndItsTextReachesItsSizeThreshold()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            Dictionary<string, Transform[]> found = FindPaths(scene, new[] { MapNamePath, MapNameTextPath });
            AssertFoundOnce(found, "map name object");
            ImageState band = StateOf("map name", found[MapNamePath][0], HeaderPiece);
            Reading[] readings = TextsUnder("map name", found[MapNameTextPath][0], 1, HeaderPiece);
            CloseScenes();

            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            Assert.IsEmpty(NotBright(new[] { band }), $"the map name band draws the bright {HeaderPiece}");
            Assert.IsEmpty(NotWhite(new[] { band }), "the map name band Image colour is white");
            AssertMeasured(readings);
            Assert.IsEmpty(BelowThreshold(readings),
                "the map name reaches the WCAG 2.x SC 1.4.3 ratio for its drawn size on the piece behind it");
        }

        // RS4 scene: the HP bar's track draws the new bright Bar_Track in white and its fill the new bright Bar_Fill,
        // both sliced (kit-spec mode :271 and :301). The fill keeps its scene colour (kit-spec usage.sceneColor :44), so
        // its colour is not an expectation here.
        [Test]
        public void HpBar_DrawsTheNewBrightTrackInWhite_AndTheNewBrightFill()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            Dictionary<string, Transform[]> found = FindPaths(scene, new[] { HpTrackPath, HpFillPath });
            AssertFoundOnce(found, "HP bar object");
            ImageState track = StateOf("hp bar", found[HpTrackPath][0], BarTrackPiece);
            ImageState fill = StateOf("hp bar", found[HpFillPath][0], BarFillPiece);
            CloseScenes();

            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            Assert.IsEmpty(NotBright(new[] { track, fill }),
                $"the HP track and fill draw the new bright {BarTrackPiece} and {BarFillPiece}");
            string[] notSliced = new[] { track, fill }.Where(state => state.Type != Image.Type.Sliced)
                .Select(state => Describe(state)).ToArray();
            Assert.IsEmpty(notSliced, "the HP track and fill Images are sliced");
            Assert.IsEmpty(NotWhite(new[] { track }), "the HP track Image colour is white");
        }

        // RS4 import: the two new bar pieces exist in the bright folder and are imported as the kit spec says (sprite,
        // pixels per unit 200, the sliced border). The expectations are the spec numbers above, not today's import.
        [Test]
        public void BarPieces_AreImportedAsTheKitSpecSays()
        {
            var missing = new List<string>();
            var wrong = new List<string>();
            foreach ((string piece, float pixelsPerUnit, Vector4 border) in s_barPieces)
            {
                string path = BrightUiKitTestSupport.ArtFolder + "/" + piece + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                TestContext.Out.WriteLine(importer == null
                    ? $"remaining-import | piece={piece} | path={path} | file={File.Exists(path)} | importer=none"
                    : $"remaining-import | piece={piece} | path={path} | file={File.Exists(path)} | " +
                      $"type={importer.textureType} | ppu={Number(importer.spritePixelsPerUnit, "F2")} | " +
                      $"border={importer.spriteBorder}");
                if (!File.Exists(path) || importer == null)
                {
                    missing.Add($"{piece}: piece missing, no {path} imported by a TextureImporter");
                    continue;
                }
                if (importer.textureType != TextureImporterType.Sprite)
                    wrong.Add($"{piece}: texture type {importer.textureType}, not Sprite");
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
                    wrong.Add($"{piece}: pixels per unit {importer.spritePixelsPerUnit}, spec {pixelsPerUnit}");
                if (importer.spriteBorder != border)
                    wrong.Add($"{piece}: sprite border {importer.spriteBorder}, spec {border} (L, B, R, T)");
            }

            Assert.IsEmpty(missing, $"precondition: both new bar pieces are in {BrightUiKitTestSupport.ArtFolder} (piece missing)");
            Assert.IsEmpty(wrong, "both bar pieces are imported as kit-spec.json says");
        }

        // RS5: the quest granted popup draws the bright panel; its title reaches the ratio for its drawn size on the bright
        // header band behind it and its body on the bright panel.
        [Test]
        public void QuestGrantedPopup_DrawsTheBrightPanel_AndItsTitleAndBodyReachTheirSizeThreshold()
        {
            Transform popup = Build(parent => QuestGrantedPopup.BuildRuntime(parent).transform);
            Transform[] panels = popup.GetComponentsInChildren<Transform>(true).Where(child => child.name == "Panel").ToArray();
            Assert.AreEqual(1, panels.Length, "precondition: the popup builds one Panel");
            ImageState panel = StateOf("quest popup", panels[0], PanelPiece);
            Reading[] title = TextsNamed("quest popup", popup, "Title", 1, HeaderPiece);
            Reading[] body = TextsNamed("quest popup", popup, "Body", 1, PanelPiece);
            Reading[] readings = title.Concat(body).ToArray();

            Assert.IsEmpty(NotBright(new[] { panel }), $"the popup panel draws the bright {PanelPiece}");
            AssertMeasured(readings);
            Assert.IsEmpty(BelowThreshold(readings),
                "the popup title and body reach the WCAG 2.x SC 1.4.3 ratio for their drawn size on the pieces behind them");
        }

        // RS6: the ending hint reaches the ratio for its drawn size on the bright inset band behind it. BuildHint is the
        // method Update calls once _armDelay has passed (EndingController.cs:37); calling it directly, the way
        // HudAndMapTests calls MapNameDisplay's private Start, keeps input devices and the clock out of the test.
        [Test]
        public void EndingHint_DrawsOnTheBrightInset_AndReachesItsSizeThreshold()
        {
            var host = new GameObject("Ending controller (fixture)");
            _cleanups.Add(() => UnityEngine.Object.DestroyImmediate(host));
            var controller = host.AddComponent<Dawnholder.Client.Scenes.EndingController>();
            SessionTestTools.Call(controller, "BuildHint");
            Reading[] readings = TextsNamed("ending hint", host.transform, "HintText", 1, InsetPiece);

            AssertMeasured(readings);
            Assert.IsEmpty(BelowThreshold(readings),
                "the ending hint reaches the WCAG 2.x SC 1.4.3 ratio for its drawn size on the piece behind it");
        }

        // RS7 control, size: the WCAG large-text bounds on both scale modes. At a fixed scale of 1, regular 24 and bold 19
        // are large (3:1) while regular 23 and bold 18 are not (4.5:1). A 1920×1080, match 0.5 screen-scaled canvas draws
        // a 30 text at 1280×720 two thirds as large, below 24, so it is judged at 4.5:1. It passes before and after PR4.
        [Test]
        public void Control_SizeThreshold_FollowsTheWcagLargeTextBounds_OnBothScaleModes()
        {
            Transform fixedCanvas = FixtureCanvas("Fixed scale canvas (fixture)", scaler =>
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            });
            Transform screenCanvas = FixtureCanvas("Screen scaled canvas (fixture)", scaler =>
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            });
            (TMP_Text Text, double Expected)[] cases =
            {
                (FixtureText(fixedCanvas, "Regular 24", s_ink, 24f, FontStyles.Normal), LargeTextContrast),
                (FixtureText(fixedCanvas, "Regular 23", s_ink, 23f, FontStyles.Normal), BodyTextContrast),
                (FixtureText(fixedCanvas, "Bold 19", s_ink, 19f, FontStyles.Bold), LargeTextContrast),
                (FixtureText(fixedCanvas, "Bold 18", s_ink, 18f, FontStyles.Bold), BodyTextContrast),
                (FixtureText(screenCanvas, "Regular 30 screen scaled", s_ink, 30f, FontStyles.Normal), BodyTextContrast),
            };
            // The same observation as every RS; these texts have no backdrop, only their size is judged.
            Reading[] readings = cases.Select(entry => Observe("size control", entry.Text, "none")).ToArray();

            Assert.IsEmpty(ScaleUnknown(readings), "control: the scale of both fixture canvases is read");
            string[] misjudged = cases.Zip(readings, (entry, reading) => (entry.Expected, reading))
                .Where(pair => pair.reading.Threshold != pair.Expected)
                .Select(pair => $"{pair.reading.TextPath}: expected {Number(pair.Expected, "F1")}:1, judged " +
                    $"{Number(pair.reading.Threshold, "F1")}:1 at {Number(pair.reading.DrawnPixels, "F2")} px " +
                    $"(scale {Number(pair.reading.Scale, "F4")}, bold {pair.reading.Bold})")
                .ToArray();
            Assert.IsEmpty(misjudged, "control: each size is judged by the WCAG large-scale text bounds");
        }

        // RS7 control, contrast and piece: on the real bright Panel_Default white stays below 4.5:1 and the ink reaches
        // it; the brown Panel_Default of the same name fails the bright-piece precondition. It passes before and after PR4.
        [Test]
        public void Control_ContrastObserver_FailsWhiteAndPassesTheInkOnTheBrightPanel_AndRejectsTheBrownPanel()
        {
            Sprite bright = AssetDatabase.LoadAssetAtPath<Sprite>(BrightUiKitTestSupport.ArtFolder + "/" + PanelPiece + ".png");
            Sprite brown = AssetDatabase.LoadAssetAtPath<Sprite>(BrownFolder + "/" + PanelPiece + ".png");
            Assert.IsNotNull(bright, "fixture: the bright Panel_Default loads");
            Assert.IsNotNull(brown, "fixture: the brown Panel_Default loads");
            Transform brightPanel = FixturePanel("Bright panel (fixture)", bright);
            Transform brownPanel = FixturePanel("Brown panel (fixture)", brown);

            // 20 px regular text is body text (4.5:1) at scale 1.
            Reading white = Observe("contrast control",
                FixtureText(brightPanel, "White", s_white, 20f, FontStyles.Normal), PanelPiece);
            Reading ink = Observe("contrast control",
                FixtureText(brightPanel, "Ink", s_ink, 20f, FontStyles.Normal), PanelPiece);
            Reading onBrown = Observe("contrast control",
                FixtureText(brownPanel, "Ink on brown", s_ink, 20f, FontStyles.Normal), PanelPiece);

            Reading[] onBright = { white, ink };
            AssertMeasured(onBright);
            Assert.IsTrue(onBright.All(reading => reading.Backdrop == PathOf(brightPanel)),
                "control: the fixture panel is the backdrop found behind each of its texts");
            Assert.IsTrue(onBright.All(reading => reading.Threshold == BodyTextContrast),
                "control: both texts are judged as body text");
            Assert.AreEqual(1, BelowThreshold(new[] { white }).Length,
                "control: white stays below 4.5:1 on the bright Panel_Default");
            Assert.IsEmpty(BelowThreshold(new[] { ink }), "control: the ink reaches 4.5:1 on the bright Panel_Default");
            Assert.AreEqual(PathOf(brownPanel), onBrown.Backdrop, "control: the brown panel is found behind its text");
            Assert.AreEqual(1, NotOnBrightPiece(new[] { onBrown }).Length,
                "control: the brown Panel_Default fails the bright-piece precondition");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observation
        // ---------------------------------------------------------------------------------------------------------

        // The one observation (contract 「공통 규칙」): the Image drawn behind the text, whether it is the expected bright
        // piece, its colour, the text's WCAG contrast over it, and the text's drawn size and threshold. Every reading also
        // goes to the test output, so the report takes the measured values from the result file.
        static Reading Observe(string scope, TMP_Text text, string expectedPiece)
        {
            Reading reading = MeasureSize(scope, text);
            reading.ExpectedPiece = expectedPiece;
            var skipped = new List<string>();
            Image backdrop = BackdropOf(text, skipped);
            if (backdrop != null)
            {
                reading.Backdrop = PathOf(backdrop.transform);
                reading.Piece = backdrop.sprite.name;
                reading.Asset = AssetDatabase.GetAssetPath(backdrop.sprite);
                reading.Bright = InBrightFolder(reading.Asset) && reading.Piece == expectedPiece;
                reading.Problem = MeanColour(backdrop, out reading.Mean, out reading.OpaquePixels);
                if (reading.Problem == null) reading.Ratio = ContrastRatio(Over(text.color, reading.Mean), reading.Mean);
            }
            foreach (string skip in skipped)
                TestContext.Out.WriteLine($"remaining-skipped | scope={scope} | text_path={reading.TextPath} | {skip}");
            TestContext.Out.WriteLine(string.Join(" | ", "remaining-reading", $"scope={scope}", $"text_path={reading.TextPath}",
                $"text={Rgba(reading.Text)}", $"expected_piece={expectedPiece}", $"backdrop={reading.Backdrop ?? "none"}",
                $"piece={reading.Piece ?? "none"}", $"asset={reading.Asset ?? "none"}", $"bright={reading.Bright}",
                $"opaque={reading.OpaquePixels}", $"mean={Rgba(reading.Mean)}", $"font_size={Number(reading.FontSize, "F2")}",
                $"bold={reading.Bold}", $"scaler={reading.Scaler}", $"scale={Number(reading.Scale, "F4")}",
                $"drawn_px={Number(reading.DrawnPixels, "F2")}", $"threshold={Number(reading.Threshold, "F1")}",
                $"ratio={Number(reading.Ratio, "F4")}", $"problem={reading.Problem ?? "none"}",
                $"scale_problem={reading.ScaleProblem ?? "none"}"));
            return reading;
        }

        // Drawn size (main msg_faeee7b36f9e 조건 1): the font size the text is set to draw at, fontSizeMin when auto sizing
        // may shrink it, times the canvas scale; then the WCAG threshold for that size and weight.
        static Reading MeasureSize(string scope, TMP_Text text)
        {
            var reading = new Reading { Scope = scope, TextPath = PathOf(text.transform), Text = text.color };
            reading.FontSize = text.enableAutoSizing ? text.fontSizeMin : text.fontSize;
            reading.Bold = (text.fontStyle & FontStyles.Bold) != 0;
            reading.ScaleProblem = CanvasScale(text, out reading.Scale, out reading.Scaler);
            if (reading.ScaleProblem == null)
            {
                reading.DrawnPixels = reading.FontSize * reading.Scale;
                bool large = reading.DrawnPixels >= LargeTextPixels || reading.Bold && reading.DrawnPixels >= LargeBoldTextPixels;
                reading.Threshold = large ? LargeTextContrast : BodyTextContrast;
            }
            return reading;
        }

        // The scale the outermost canvas above the text applies, read from its CanvasScaler's real settings (a builder's
        // unset AddComponent<CanvasScaler>() included). Constant pixel size uses scaleFactor. Scale with screen size and
        // match width or height uses CanvasScaler's log-2 mix of the width and height ratios at the reference resolution
        // and at 1280×720, the smaller one. Any other mode has no rule in this contract and is reported, not guessed.
        static string CanvasScale(TMP_Text text, out double scale, out string scaler)
        {
            scale = double.NaN;
            scaler = "none";
            Canvas[] canvases = text.GetComponentsInParent<Canvas>(true);
            if (canvases.Length == 0) return "no canvas above the text";
            Canvas root = canvases[canvases.Length - 1];
            if (!root.TryGetComponent(out CanvasScaler component))
                return $"no CanvasScaler on the root canvas {PathOf(root.transform)}";
            string at = PathOf(root.transform);
            if (component.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                scale = component.scaleFactor;
                scaler = $"{at} ConstantPixelSize scaleFactor={Number(component.scaleFactor, "F4")}";
                return null;
            }
            if (component.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return $"{at} scale mode {component.uiScaleMode} has no rule in this contract";
            if (component.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                return $"{at} screen match mode {component.screenMatchMode} has no rule in this contract";
            Vector2 reference = component.referenceResolution;
            double atReference = MatchScale(reference, reference, component.matchWidthOrHeight);
            double atSmallest = MatchScale(s_smallestScreen, reference, component.matchWidthOrHeight);
            scale = Math.Min(atReference, atSmallest);
            scaler = $"{at} ScaleWithScreenSize reference={Number(reference.x, "F0")}x{Number(reference.y, "F0")} " +
                $"match={Number(component.matchWidthOrHeight, "F2")} scale_at_reference={Number(atReference, "F4")} " +
                $"scale_at_1280x720={Number(atSmallest, "F4")}";
            return null;
        }

        static double MatchScale(Vector2 screen, Vector2 reference, float match)
        {
            double width = Math.Log(screen.x / reference.x, 2);
            double height = Math.Log(screen.y / reference.y, 2);
            return Math.Pow(2, width + (height - width) * match);
        }

        // Backdrop rule (one rule for every case, BrightTextContrastTests.cs:249~:273): the last Image before the text in
        // its canvas's depth-first draw order, switched-off objects included, that is drawn together with the text and
        // holds the centre of the text's rect. A missing or reordered band is found as whatever lies behind instead.
        static Image BackdropOf(TMP_Text text, List<string> skipped)
        {
            Canvas canvas = text.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                skipped.Add("skipped=none | reason=no canvas above the text");
                return null;
            }
            Graphic[] order = canvas.GetComponentsInChildren<Graphic>(true);
            int at = Array.IndexOf<Graphic>(order, text);
            RectTransform textRect = text.rectTransform;
            Vector3 centre = InCanvas(textRect, canvas.transform).MultiplyPoint3x4(textRect.rect.center);
            for (int i = at - 1; i >= 0; i--)
            {
                if (!(order[i] is Image image) || !Holds(image.rectTransform, centre, canvas.transform)) continue;
                string reason = NotDrawn(image, text.transform);
                if (reason == null) return image;
                skipped.Add($"skipped={PathOf(image.transform)} | reason={reason}");
            }
            return null;
        }

        // BrightTextContrastTests.cs:275~:285: an Image that leaves no pixel.
        static string NotDrawn(Image image, Transform text)
        {
            if (!image.enabled) return "component disabled";
            if (image.sprite == null) return "no sprite";
            if (image.color.a <= 0f) return "alpha 0";
            if (image.type == Image.Type.Filled && image.fillAmount <= 0f) return "filled amount 0";
            string inactive = InactiveBetween(image.transform, text);
            return inactive == null ? null : $"inactive '{inactive}'";
        }

        // BrightTextContrastTests.cs:287~:297: the part shared with the text is left out, so the pause menu, saved under a
        // switched-off MenuRoot, is still measured.
        static string InactiveBetween(Transform image, Transform text)
        {
            var textLine = new HashSet<Transform>();
            for (Transform at = text; at != null; at = at.parent) textLine.Add(at);
            for (Transform at = image; at != null && !textLine.Contains(at); at = at.parent)
                if (!at.gameObject.activeSelf) return at.name;
            return null;
        }

        // BrightTextContrastTests.cs:299~:308: placement in the canvas's own space, leaving out the canvas object, which
        // UI.unity stores at scale 0 and size 0 without a screen.
        static Matrix4x4 InCanvas(Transform from, Transform canvas)
        {
            Matrix4x4 placement = Matrix4x4.identity;
            for (Transform at = from; at != null && at != canvas; at = at.parent)
                placement = Matrix4x4.TRS(at.localPosition, at.localRotation, at.localScale) * placement;
            return placement;
        }

        static bool Holds(RectTransform rect, Vector3 pointInCanvas, Transform canvas)
        {
            Vector3 local = InCanvas(rect, canvas).inverse.MultiplyPoint3x4(pointInCanvas);
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }

        // BrightTextContrastTests.cs:316~:366 (lead design, contract 「대비와 크기 기준」): the mean sRGB colour of the pixels
        // with alpha of at least 0.5 inside the sprite border, times the Image colour. The PNG file itself is decoded into
        // a temporary texture, so the import's Read/Write setting does not matter. Returns why the colour could not be
        // read, or null.
        static string MeanColour(Image image, out Color mean, out int opaque)
        {
            mean = Color.clear;
            opaque = 0;
            Sprite sprite = image.sprite;
            string file = AssetDatabase.GetAssetPath(sprite.texture);
            string own = AssetDatabase.GetAssetPath(sprite);
            if (file != own) return $"'{sprite.name}' is drawn from '{file}', not from its own file '{own}'";
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return $"'{sprite.name}' has no PNG file ('{file}')";
            var png = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(png, File.ReadAllBytes(file), false)) return $"'{file}' does not decode";
                float scaleX = png.width / (float)sprite.texture.width;
                float scaleY = png.height / (float)sprite.texture.height;
                Rect rect = sprite.rect;
                Vector4 border = sprite.border; // left, bottom, right, top
                int xMin = Mathf.Max(0, Mathf.RoundToInt((rect.xMin + border.x) * scaleX));
                int xMax = Mathf.Min(png.width, Mathf.RoundToInt((rect.xMax - border.z) * scaleX));
                int yMin = Mathf.Max(0, Mathf.RoundToInt((rect.yMin + border.y) * scaleY));
                int yMax = Mathf.Min(png.height, Mathf.RoundToInt((rect.yMax - border.w) * scaleY));
                if (xMax <= xMin || yMax <= yMin) return $"'{sprite.name}' has no area inside its border";
                Color32[] pixels = png.GetPixels32();
                double red = 0, green = 0, blue = 0;
                for (int y = yMin; y < yMax; y++)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        Color32 pixel = pixels[y * png.width + x];
                        if (pixel.a < OpaqueAlpha) continue;
                        red += pixel.r;
                        green += pixel.g;
                        blue += pixel.b;
                        opaque++;
                    }
                }
                if (opaque == 0) return $"'{sprite.name}' has no pixel with alpha of at least 0.5 inside its border";
                Color tint = image.color;
                mean = new Color((float)(red / opaque / 255.0) * tint.r, (float)(green / opaque / 255.0) * tint.g,
                    (float)(blue / opaque / 255.0) * tint.b, 1f);
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(png);
            }
        }

        // A text with alpha below 1 shows as its colour mixed with the backdrop (contract 「대비와 크기 기준」).
        static Color Over(Color text, Color backdrop)
        {
            float a = Mathf.Clamp01(text.a);
            return new Color(text.r * a + backdrop.r * (1f - a), text.g * a + backdrop.g * (1f - a),
                text.b * a + backdrop.b * (1f - a), 1f);
        }

        // WCAG 2.x relative luminance and contrast ratio, the definitions SC 1.4.3 refers to.
        static double ContrastRatio(Color first, Color second)
        {
            double a = Luminance(first);
            double b = Luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        static double Luminance(Color colour) =>
            0.2126 * Linear(colour.r) + 0.7152 * Linear(colour.g) + 0.0722 * Linear(colour.b);

        static double Linear(float channel) =>
            channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        static bool InBrightFolder(string asset) =>
            !string.IsNullOrEmpty(asset) && Path.GetDirectoryName(asset)?.Replace('\\', '/') == BrightUiKitTestSupport.ArtFolder;

        // A target Image's sprite, colour, type and raycast as found; the expected piece is judged by folder and name.
        static ImageState StateOf(string scope, Transform owner, string expectedPiece)
        {
            var state = new ImageState { Path = PathOf(owner), ExpectedPiece = expectedPiece };
            if (owner.TryGetComponent(out Image image))
            {
                state.Present = true;
                state.Piece = image.sprite != null ? image.sprite.name : null;
                state.Asset = image.sprite != null ? AssetDatabase.GetAssetPath(image.sprite) : null;
                state.Bright = InBrightFolder(state.Asset) && state.Piece == expectedPiece;
                state.Colour = image.color;
                state.Type = image.type;
                state.Raycast = image.raycastTarget;
            }
            TestContext.Out.WriteLine($"remaining-image | scope={scope} | {Describe(state)}");
            return state;
        }

        // A Button's transition and target graphic, ending in "ok" when it is ColorTint on the button's own Image.
        static string TintOf(Transform owner)
        {
            if (!owner.TryGetComponent(out Button button)) return $"{PathOf(owner)}: no Button";
            bool own = owner.TryGetComponent(out Image image) && button.targetGraphic == image;
            bool tint = button.transition == Selectable.Transition.ColorTint;
            string target = button.targetGraphic != null ? PathOf(button.targetGraphic.transform) : "none";
            return $"{PathOf(owner)}: transition={button.transition} target={target} {(tint && own ? "ok" : "changed")}";
        }

        // Whether the panel Image comes before the button's own Image in the canvas draw order, the button under the panel.
        static string DrawnAfter(Image panel, Transform button)
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>(true);
            Graphic[] order = canvas.GetComponentsInChildren<Graphic>(true);
            int panelAt = Array.IndexOf<Graphic>(order, panel);
            int buttonAt = button.TryGetComponent(out Image image) ? Array.IndexOf<Graphic>(order, image) : -1;
            bool under = button.IsChildOf(panel.transform);
            bool ok = panelAt >= 0 && buttonAt > panelAt && under;
            return $"{PathOf(button)}: panel_at={panelAt} button_at={buttonAt} under_panel={under} {(ok ? "ok" : "changed")}";
        }

        // ---------------------------------------------------------------------------------------------------------
        // Judgements, each its own list so a failure names what is missing
        // ---------------------------------------------------------------------------------------------------------

        static void AssertMeasured(Reading[] readings)
        {
            Assert.IsEmpty(Listed(readings.Where(reading => reading.ScaleProblem != null)),
                "precondition: the drawn size of every text is read from its canvas scaler");
            Assert.IsEmpty(Listed(readings.Where(reading => reading.Backdrop == null)),
                "precondition: an Image is drawn behind every text");
            Assert.IsEmpty(NotOnBrightPiece(readings), "precondition: the piece behind every text is its expected bright " +
                $"piece from {BrightUiKitTestSupport.ArtFolder}; another piece is asked about, not fixed");
            Assert.IsEmpty(Listed(readings.Where(reading => reading.Backdrop != null && reading.Problem != null)),
                "precondition: the colour of every backdrop is read from its PNG file");
        }

        static string[] ScaleUnknown(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.ScaleProblem != null));

        static string[] NotOnBrightPiece(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.Backdrop != null && !reading.Bright));

        static string[] BelowThreshold(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.Problem == null && reading.ScaleProblem == null &&
                !double.IsNaN(reading.Ratio) && reading.Ratio < reading.Threshold));

        static string[] NotBright(IEnumerable<ImageState> states) =>
            states.Where(state => !state.Present || !state.Bright).Select(state => Describe(state)).ToArray();

        static string[] NotWhite(IEnumerable<ImageState> states) =>
            states.Where(state => !state.Present || state.Colour != Color.white).Select(state => Describe(state)).ToArray();

        static string[] Listed(IEnumerable<Reading> readings) => readings.Select(reading => Describe(reading)).ToArray();

        static string Describe(Reading reading)
        {
            string size = reading.ScaleProblem ?? $"{Number(reading.FontSize, "F2")} × {Number(reading.Scale, "F4")} = " +
                $"{Number(reading.DrawnPixels, "F2")} px{(reading.Bold ? " bold" : string.Empty)}, " +
                $"threshold {Number(reading.Threshold, "F1")}:1";
            return reading.Backdrop == null
                ? $"{reading.TextPath}: text {Rgba(reading.Text)}, {size}, expected piece '{reading.ExpectedPiece}', " +
                  "no Image drawn behind it"
                : $"{reading.TextPath}: text {Rgba(reading.Text)}, {size}, on '{reading.Piece}' ({reading.Asset}) of " +
                  $"{reading.Backdrop}, expected piece '{reading.ExpectedPiece}', backdrop mean {Rgba(reading.Mean)}, " +
                  $"ratio {Number(reading.Ratio, "F2")}" + (reading.Problem == null ? string.Empty : $", {reading.Problem}");
        }

        static string Describe(ImageState state) => !state.Present
            ? $"{state.Path}: no Image, expected piece '{state.ExpectedPiece}'"
            : $"{state.Path}: sprite '{state.Piece ?? "none"}' ({state.Asset ?? "none"}), expected piece '{state.ExpectedPiece}', " +
              $"bright={state.Bright}, colour {Rgba(state.Colour)}, type {state.Type}, raycast {state.Raycast}";

        // ---------------------------------------------------------------------------------------------------------
        // Fixtures
        // ---------------------------------------------------------------------------------------------------------

        // Objects by full path in the open scene, switched-off ones included (MenuRoot is saved switched off).
        static Dictionary<string, Transform[]> FindPaths(Scene scene, IEnumerable<string> paths)
        {
            Transform[] all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            return paths.ToDictionary(path => path, path => all.Where(item => PathOf(item) == path).ToArray());
        }

        // Contract 「공통 규칙」: every table target is found exactly once before anything is judged (main msg_e873edba3d2d).
        static void AssertFoundOnce(Dictionary<string, Transform[]> found, string what)
        {
            string[] miscounted = found.Where(entry => entry.Value.Length != 1)
                .Select(entry => $"{entry.Key}: found {entry.Value.Length}, table 1").ToArray();
            Assert.IsEmpty(miscounted, $"precondition: every {what} of the contract table is in UI.unity once");
        }

        static Reading[] TextsUnder(string scope, Transform owner, int count, string expectedPiece)
        {
            TMP_Text[] texts = owner.GetComponentsInChildren<TMP_Text>(true);
            Assert.AreEqual(count, texts.Length, $"precondition: {PathOf(owner)} holds {count} text(s) as the contract table says");
            return texts.Select(text => Observe(scope, text, expectedPiece)).ToArray();
        }

        static Reading[] TextsNamed(string scope, Transform window, string name, int count, string expectedPiece)
        {
            TMP_Text[] texts = window.GetComponentsInChildren<TMP_Text>(true).Where(text => text.name == name).ToArray();
            Assert.AreEqual(count, texts.Length, $"precondition: the {scope} builds {count} text(s) named {name}");
            return texts.Select(text => Observe(scope, text, expectedPiece)).ToArray();
        }

        // The product builder under a fixture parent that teardown destroys with everything built under it.
        Transform Build(Func<Transform, Transform> builder)
        {
            var parent = new GameObject("Remaining screens parent (fixture)");
            _cleanups.Add(() => UnityEngine.Object.DestroyImmediate(parent));
            return builder(parent.transform);
        }

        // An overlay canvas with its own CanvasScaler set by the caller.
        Transform FixtureCanvas(string name, Action<CanvasScaler> setScaler)
        {
            var root = new GameObject(name, typeof(RectTransform));
            _cleanups.Add(() => UnityEngine.Object.DestroyImmediate(root));
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            setScaler(root.AddComponent<CanvasScaler>());
            return root.transform;
        }

        // A fixed-scale canvas holding one sliced, white Image of the given piece, the way the windows draw a panel.
        Transform FixturePanel(string name, Sprite sprite)
        {
            Transform canvas = FixtureCanvas(name, scaler =>
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            });
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.rectTransform.sizeDelta = new Vector2(400f, 160f);
            return panel.transform;
        }

        static TMP_Text FixtureText(Transform parent, string name, Color colour, float size, FontStyles style)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 20f);
            rect.offsetMax = new Vector2(-20f, -20f);
            TMP_Text text = child.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.color = colour;
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.fontStyle = style;
            return text;
        }

        // BrightTextContrastTests.cs:477~:504: additive next to the runner's scene, single if Unity refuses; closed
        // without saving in CloseScenes or teardown.
        Scene OpenUiScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene;
            bool replaced = false;
            try
            {
                scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine($"fixture: additive open refused ({error.Message}); opening the UI scene alone");
                scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
                replaced = true;
            }
            _cleanups.Add(() =>
            {
                if (replaced)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    return;
                }
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            });
            return scene;
        }

        void CloseScenes()
        {
            for (int i = _cleanups.Count - 1; i >= 0; i--) _cleanups[i]();
            _cleanups.Clear();
        }

        static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform at = transform; at != null; at = at.parent) names.Add(at.name);
            names.Reverse();
            return string.Join("/", names);
        }

        static string Rgba(Color colour) => string.Join(",",
            new[] { colour.r, colour.g, colour.b, colour.a }.Select(value => value.ToString("F4", CultureInfo.InvariantCulture)));

        static string Number(double value, string format) =>
            double.IsNaN(value) ? "none" : value.ToString(format, CultureInfo.InvariantCulture);

        static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
