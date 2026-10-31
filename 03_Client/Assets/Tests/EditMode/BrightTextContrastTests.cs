using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // PR3 text colour (main decision 글자 색 A msg_77a7e9b3e05c, goal 2026-10-29-inventory-character-select-look): texts
    // that sat on the default skin's brown pieces now sit on the bright kit, so each must read as body text on the bright
    // piece drawn behind it. Body text needs a contrast ratio of at least 4.5:1 (WCAG 2.x SC 1.4.3 「Contrast (Minimum)」,
    // main msg_1f6fd458913d; the 3:1 large-text allowance is not used).
    // Observe is the one observation: it finds the Image drawn behind a text (BackdropOf), reads that piece's colour from
    // its own PNG file (MeanColour) and computes the WCAG ratio. The tests judge only the readings it returns. No current
    // or new text colour is an expectation; colours appear only as inputs of the control.
    // The combat HUD case opens the real Scenes/99.UI/UI.unity next to the runner's scene, runs only HudLayout.Awake and
    // PartyMemberHud.Awake (the HudFrameSkinTests and UiWindowCompositionRenderTests.InitPartyHud way) and closes it without
    // saving. The window cases call the product's public BuildRuntime builders without Awake, so no singleton, input
    // action, pointer binding or server is involved.
    public sealed class BrightTextContrastTests
    {
        const string UiScenePath = "Assets/Scenes/99.UI/UI.unity";
        // The brown B1 folder, read only by the control.
        const string BrownFolder = "Assets/Art/UI/Components";
        // WCAG 2.x SC 1.4.3 Contrast (Minimum): 4.5:1 for body text (main msg_1f6fd458913d).
        const double BodyTextContrast = 4.5;
        // Lead design (contract 「대비 기준」): a backdrop pixel counts when its alpha is at least 0.5, 128 of 255.
        const byte OpaqueAlpha = 128;

        // Contract 「공통 규칙」: the bright pieces these texts are meant to sit on.
        static readonly string[] s_brightBackdrops = { "Panel_Default", "Panel_Dialog", "Panel_Emphasis", "Inset_Default" };

        // Contract CT1 table: the HUD texts kept on the default skin, and how many texts carry each path.
        static readonly (string Path, int Count)[] s_hudTexts =
        {
            ("HUD/character_status/ClassText", 1),
            ("HUD/character_status/HpText", 1),
            ("HUD/Gold_bar/GoldText", 1),
            ("HUD/Party_Info/Info_Title", 1),
            ("HUD/Party_Info/MemberStatus/MemberInfo", 2),
            ("HUD/Quest_Hud/Title", 1),
            ("HUD/Quest_Hud/Explain", 1),
            ("HUD/Quest_Hud/Prograss", 1),
            ("HUD/skill_panel/skill_slot_1/QKey", 1),
            ("HUD/skill_panel/skill_slot_2/EKey", 1),
        };

        // Control inputs only (contract CT5), each from the line that uses it today:
        // the HUD cream, UI.unity fontColor of HpText and the other cream HUD texts;
        static readonly Color s_cream = new Color(1f, 0.94f, 0.8f);
        // white, NpcDialogPanel.cs:184 and PartyInvitePopup.cs:215;
        static readonly Color s_white = Color.white;
        // the bright windows' ink and quiet ink, InventoryPanelView.cs:20 and :21 (CharacterMenuView.cs:17 and :18 alike).
        static readonly Color s_ink = new Color(0.26f, 0.17f, 0.09f);
        static readonly Color s_quietInk = new Color(0.45f, 0.33f, 0.2f);

        readonly List<Action> _cleanups = new List<Action>();

        // One observed text: what is drawn behind it and the resulting contrast. Strings and values only, so a reading
        // outlives the scene it was taken from.
        sealed class Reading
        {
            internal string TextPath;
            internal Color Text;
            internal string Backdrop;   // path of the Image drawn behind the text; null when none is
            internal string Piece;      // that Image's sprite name
            internal string Asset;      // that sprite's asset path
            internal bool Bright;
            internal int OpaquePixels;
            internal Color Mean;        // mean opaque colour inside the sprite border × the Image colour
            internal double Ratio = double.NaN;
            internal string Problem;    // why the backdrop colour could not be read
        }

        [SetUp]
        public void Setup()
        {
            // The builders hand back an existing window instead of building one, which would hide the build under test.
            Assert.IsNull(NpcDialogPanel.Instance, "fixture: another test left the NPC dialog singleton");
            Assert.IsNull(PartyInvitePopup.Instance, "fixture: another test left the party invite popup");
            Assert.IsNull(DungeonClearRewardWindow.Instance, "fixture: another test left the dungeon reward window");
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

        // CT1: the combat HUD texts on the real UI scene once HudLayout.cs:37~:48 and PartyMemberHud.Awake have put the
        // default skin's pieces behind them. Texts that are switched off in the scene (the E slot a class hides) are
        // measured too.
        [Test]
        public void CombatHudTexts_OnTheRealUiScene_ReachBodyTextContrastOnTheirBrightPiece()
        {
            string sceneBefore = Sha256(UiScenePath);
            Scene scene = OpenUiScene();
            GameObject[] roots = scene.GetRootGameObjects();
            HudLayout[] layouts = roots.SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).ToArray();
            PartyMemberHud[] parties = roots.SelectMany(root => root.GetComponentsInChildren<PartyMemberHud>(true)).ToArray();
            Assert.AreEqual(1, layouts.Length, "fixture: one HudLayout in UI.unity");
            Assert.AreEqual(1, parties.Length, "fixture: one PartyMemberHud in UI.unity");
            SessionTestTools.Call(layouts[0], "Awake");
            SessionTestTools.Call(parties[0], "Awake");

            TMP_Text[] texts = roots.SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            Dictionary<string, TMP_Text[]> found = s_hudTexts.ToDictionary(target => target.Path,
                target => texts.Where(text => PathOf(text.transform) == target.Path).ToArray());
            string[] miscounted = s_hudTexts.Where(target => found[target.Path].Length != target.Count)
                .Select(target => $"{target.Path}: found {found[target.Path].Length}, table {target.Count}").ToArray();
            Reading[] readings = s_hudTexts.SelectMany(target => found[target.Path])
                .Select(text => Observe("combat hud", text)).ToArray();
            CloseScenes();

            Assert.IsEmpty(miscounted, "precondition: every HUD text of the contract table is in UI.unity as often as the table says");
            Assert.AreEqual(s_hudTexts.Sum(target => target.Count), readings.Length, "precondition: every table text is observed");
            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
            AssertMeasured(readings, requireVisible: false);
            Assert.IsEmpty(BelowBodyContrast(readings),
                "every combat HUD text reaches 4.5:1 on the bright piece behind it (WCAG 2.x SC 1.4.3)");
        }

        // CT2: the NPC dialog's line and close hint (NpcDialogPanel.cs:184 and :201) on the dialog panel piece.
        [Test]
        public void NpcDialogTexts_ReachBodyTextContrastOnTheBrightDialogPanel()
        {
            Transform window = Build(parent => NpcDialogPanel.BuildRuntime(parent).transform);
            Reading[] readings = ObserveNamed("npc dialog", window, ("DialogText", 1), ("CloseHintText", 1));

            AssertMeasured(readings, requireVisible: true);
            Assert.IsEmpty(BelowBodyContrast(readings),
                "the NPC dialog line and close hint reach 4.5:1 on the bright dialog panel (WCAG 2.x SC 1.4.3)");
        }

        // CT3: the party invite line (PartyInvitePopup.cs:215) on the popup's panel piece.
        [Test]
        public void PartyInviteText_ReachesBodyTextContrastOnTheBrightPanel()
        {
            Transform window = Build(parent => PartyInvitePopup.BuildRuntime(parent).transform);
            Reading[] readings = ObserveNamed("party invite", window, ("InviteText", 1));

            AssertMeasured(readings, requireVisible: false);
            Assert.IsEmpty(BelowBodyContrast(readings),
                "the party invite line reaches 4.5:1 on the bright panel (WCAG 2.x SC 1.4.3)");
        }

        // CT4: the dungeon reward summary on the emphasis panel and every reward row on its inset
        // (DungeonClearRewardWindow.cs:202 and :209; the rows keep TMP's default colour).
        [Test]
        public void DungeonRewardSummaryAndRows_ReachBodyTextContrastOnTheirBrightPieces()
        {
            Transform window = Build(parent => DungeonClearRewardWindow.BuildRuntime(parent).transform);
            (string Name, int Count)[] targets = new[] { ("Summary", 1) }
                .Concat(Enumerable.Range(0, DungeonClearRewards.MaxItemKinds).Select(row => ("Reward" + row, 1))).ToArray();
            Reading[] readings = ObserveNamed("dungeon reward", window, targets);

            AssertMeasured(readings, requireVisible: true);
            Assert.IsEmpty(BelowBodyContrast(readings),
                "the reward summary and every reward row reach 4.5:1 on the bright piece behind them (WCAG 2.x SC 1.4.3)");
        }

        // CT5 control: the observer itself. On the real bright Panel_Default the HUD cream and white stay below 4.5:1 and
        // both inks reach it; the brown piece of the same name fails the bright-piece precondition. It passes before and
        // after the text colour change.
        [Test]
        public void Control_ContrastObserver_FailsCreamAndWhiteAndPassesBothInksOnTheBrightPanel_AndRejectsTheBrownPanel()
        {
            Sprite bright = AssetDatabase.LoadAssetAtPath<Sprite>(Named("Panel_Default").AssetPath);
            Sprite brown = AssetDatabase.LoadAssetAtPath<Sprite>(BrownFolder + "/Panel_Default.png");
            Assert.IsNotNull(bright, "fixture: the bright Panel_Default loads");
            Assert.IsNotNull(brown, "fixture: the brown Panel_Default loads");
            Transform brightPanel = FixturePanel("Bright panel (fixture)", bright);
            Transform brownPanel = FixturePanel("Brown panel (fixture)", brown);

            Reading[] light =
            {
                Observe("control", FixtureText(brightPanel, "Cream", s_cream)),
                Observe("control", FixtureText(brightPanel, "White", s_white)),
            };
            Reading[] dark =
            {
                Observe("control", FixtureText(brightPanel, "Ink", s_ink)),
                Observe("control", FixtureText(brightPanel, "Quiet ink", s_quietInk)),
            };
            Reading onBrown = Observe("control", FixtureText(brownPanel, "Ink on brown", s_ink));

            Reading[] onBright = light.Concat(dark).ToArray();
            AssertMeasured(onBright, requireVisible: true);
            Assert.IsTrue(onBright.All(reading => reading.Backdrop == PathOf(brightPanel)),
                "control: the fixture panel is the backdrop found behind each of its texts");
            Assert.AreEqual(light.Length, BelowBodyContrast(light).Length,
                "control: the HUD cream and white stay below 4.5:1 on the bright Panel_Default");
            Assert.IsEmpty(BelowBodyContrast(dark), "control: the ink and the quiet ink reach 4.5:1 on the bright Panel_Default");
            Assert.AreEqual(PathOf(brownPanel), onBrown.Backdrop, "control: the brown panel is found behind its text");
            Assert.AreEqual(1, NotOnBrightPiece(new[] { onBrown }).Length,
                "control: the brown Panel_Default fails the bright-piece precondition");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observation
        // ---------------------------------------------------------------------------------------------------------

        // The one observation (contract 「공통 규칙」): the Image drawn behind the text, whether it is one of the bright
        // pieces, its colour, and the text's WCAG contrast over it. Every reading also goes to the test output, so the
        // report takes the measured values from the result file.
        static Reading Observe(string scope, TMP_Text text)
        {
            var reading = new Reading { TextPath = PathOf(text.transform), Text = text.color };
            var skipped = new List<string>();
            Image backdrop = BackdropOf(text, skipped);
            if (backdrop != null)
            {
                reading.Backdrop = PathOf(backdrop.transform);
                reading.Piece = backdrop.sprite.name;
                reading.Asset = AssetDatabase.GetAssetPath(backdrop.sprite);
                reading.Bright = Path.GetDirectoryName(reading.Asset)?.Replace('\\', '/') == ArtFolder
                    && s_brightBackdrops.Contains(reading.Piece);
                reading.Problem = MeanColour(backdrop, out reading.Mean, out reading.OpaquePixels);
                if (reading.Problem == null) reading.Ratio = ContrastRatio(Over(text.color, reading.Mean), reading.Mean);
            }
            foreach (string skip in skipped)
                TestContext.Out.WriteLine($"contrast-skipped | scope={scope} | text_path={reading.TextPath} | {skip}");
            TestContext.Out.WriteLine(string.Join(" | ", "contrast-reading", $"scope={scope}", $"text_path={reading.TextPath}",
                $"text={Rgba(reading.Text)}", $"backdrop={reading.Backdrop ?? "none"}", $"piece={reading.Piece ?? "none"}",
                $"asset={reading.Asset ?? "none"}", $"bright={reading.Bright}", $"opaque={reading.OpaquePixels}",
                $"mean={Rgba(reading.Mean)}", $"ratio={Number(reading.Ratio, "F4")}", $"problem={reading.Problem ?? "none"}"));
            return reading;
        }

        // Backdrop rule (one rule for every case): the last Image before the text in its canvas's depth-first draw order
        // (the UiWindowCompositionRenderTests.VisibleGraphics order, with switched-off objects included) that is drawn
        // together with the text and holds the centre of the text's rect (the PiecesUnder test of a rectangle holding a
        // point). The images it passes over on the way are written out with the reason.
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

        // An Image that leaves no pixel: switched off, without a sprite, fully transparent, filled to 0 (the skill slots'
        // cooldown overlay at rest), or under an object that is off while the text's own line is not.
        static string NotDrawn(Image image, Transform text)
        {
            if (!image.enabled) return "component disabled";
            if (image.sprite == null) return "no sprite";
            if (image.color.a <= 0f) return "alpha 0";
            if (image.type == Image.Type.Filled && image.fillAmount <= 0f) return "filled amount 0";
            string inactive = InactiveBetween(image.transform, text);
            return inactive == null ? null : $"inactive '{inactive}'";
        }

        // Every object from the Image up to, not including, its lowest common ancestor with the text must be on. The
        // shared part is left out, so a text whose slot is off in the scene is still measured on that slot, while a
        // switched-off sibling (a portrait without a portrait) does not count.
        static string InactiveBetween(Transform image, Transform text)
        {
            var textLine = new HashSet<Transform>();
            for (Transform at = text; at != null; at = at.parent) textLine.Add(at);
            for (Transform at = image; at != null && !textLine.Contains(at); at = at.parent)
                if (!at.gameObject.activeSelf) return at.name;
            return null;
        }

        // Placement in the canvas's own space from each object's local position, rotation and scale up to the canvas,
        // leaving out the canvas object itself: UI.unity stores the HUD canvas RectTransform at scale 0 and size 0, which
        // nothing updates without a screen, and a scale of 0 would fold every world point into one.
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

        // Lead design (contract 「대비 기준」): the mean sRGB colour of the pixels with alpha of at least 0.5 inside the
        // sprite border (the 9-slice centre), times the Image colour. The PNG file itself is decoded into a temporary
        // texture, so the import's Read/Write setting does not matter; the sprite rect and border are in imported pixels
        // and are scaled to the file's size. Returns why the colour could not be read, or null.
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

        // A text with alpha below 1 shows as its colour mixed with the backdrop (contract 「대비 기준」).
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

        // ---------------------------------------------------------------------------------------------------------
        // Judgements over readings, each its own list so a failure names what is missing
        // ---------------------------------------------------------------------------------------------------------

        static void AssertMeasured(Reading[] readings, bool requireVisible)
        {
            Assert.IsEmpty(WithoutBackdrop(readings), "precondition: an Image is drawn behind every text");
            Assert.IsEmpty(NotOnBrightPiece(readings), "precondition: the piece behind every text is a bright kit " +
                $"{string.Join(", ", s_brightBackdrops)} from {ArtFolder}; another piece is asked about, not fixed");
            Assert.IsEmpty(Listed(readings.Where(reading => reading.Backdrop != null && reading.Problem != null)),
                "precondition: the colour of every backdrop is read from its PNG file");
            if (requireVisible)
                Assert.IsEmpty(Listed(readings.Where(reading => reading.Text.a <= 0f)), "every text has an alpha above 0");
        }

        static string[] WithoutBackdrop(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.Backdrop == null));

        static string[] NotOnBrightPiece(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.Backdrop != null && !reading.Bright));

        static string[] BelowBodyContrast(IEnumerable<Reading> readings) =>
            Listed(readings.Where(reading => reading.Problem == null && !double.IsNaN(reading.Ratio) && reading.Ratio < BodyTextContrast));

        static string[] Listed(IEnumerable<Reading> readings) => readings.Select(Describe).ToArray();

        static string Describe(Reading reading) => reading.Backdrop == null
            ? $"{reading.TextPath}: text {Rgba(reading.Text)}, no Image drawn behind it"
            : $"{reading.TextPath}: text {Rgba(reading.Text)} on '{reading.Piece}' ({reading.Asset}) of {reading.Backdrop}, " +
              $"backdrop mean {Rgba(reading.Mean)}, ratio {Number(reading.Ratio, "F2")}" +
              (reading.Problem == null ? string.Empty : $", {reading.Problem}");

        // ---------------------------------------------------------------------------------------------------------
        // Fixtures
        // ---------------------------------------------------------------------------------------------------------

        // Texts by name under one built window, after a precondition that each name is built as often as the table says.
        static Reading[] ObserveNamed(string scope, Transform window, params (string Name, int Count)[] targets)
        {
            TMP_Text[] texts = window.GetComponentsInChildren<TMP_Text>(true);
            string[] miscounted = targets.Where(target => texts.Count(text => text.name == target.Name) != target.Count)
                .Select(target => $"{target.Name}: found {texts.Count(text => text.name == target.Name)}, table {target.Count}")
                .ToArray();
            Assert.IsEmpty(miscounted, $"precondition: every {scope} text of the contract table is built as often as the table says");
            return targets.SelectMany(target => texts.Where(text => text.name == target.Name))
                .Select(text => Observe(scope, text)).ToArray();
        }

        // The product builder under a fixture parent that teardown destroys with everything built under it.
        Transform Build(Func<Transform, Transform> builder)
        {
            var parent = new GameObject("Text contrast parent (fixture)");
            _cleanups.Add(() => UnityEngine.Object.DestroyImmediate(parent));
            return builder(parent.transform);
        }

        // A canvas holding one sliced, white Image of the given piece, the way the windows draw a panel.
        Transform FixturePanel(string name, Sprite sprite)
        {
            var root = new GameObject(name, typeof(RectTransform));
            _cleanups.Add(() => UnityEngine.Object.DestroyImmediate(root));
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            Image image = panel.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.rectTransform.sizeDelta = new Vector2(400f, 160f);
            return panel.transform;
        }

        static TMP_Text FixtureText(Transform panel, string name, Color colour)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(panel, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 20f);
            rect.offsetMax = new Vector2(-20f, -20f);
            TMP_Text text = child.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.color = colour;
            return text;
        }

        // Additive next to the runner's scene, single if Unity refuses; closed without saving in CloseScenes.
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
