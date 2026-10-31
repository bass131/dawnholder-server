using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // The guild and blacksmith windows in the approved bright kit, written before the implementation (goal
    // 2026-10-30-login-screen-guild-window 「만들 것」 PR-B; lead contract pr-b-red-contract-v2 「설계 고정」 and T4). Both
    // windows are built as GuildUiController builds them (GuildWindow.BuildRuntime and BlacksmithWindow.BuildRuntime under
    // one parent, GuildUiController.cs:126 and :134) and every piece is told by the GUID of the asset it came from
    // (BrightUiKitTestSupport, as CharacterMenuViewAppearanceTests does). No new art: only the 25 approved pieces count.
    public sealed class GuildWindowsBrightSkinTests
    {
        // The brown kit the windows show today (UiWindowAppearanceContractTests.cs:39).
        const string BrownKitFolder = "Assets/Art/UI/Components/";
        // WCAG 2.x contrast for large text and interface parts. 4.5 would reject the approved bright look: the character
        // menu's secondary and result inks measure about 4.3 to 4.4 on Panel_Default (this file's control below).
        const float MinimumContrast = 3f;
        // The approved inks on the bright parchment (CharacterMenuView.cs:17-19).
        static readonly Color s_approvedInk = new Color(0.26f, 0.17f, 0.09f);
        static readonly Color s_approvedResult = new Color(0.6f, 0.25f, 0.08f);
        static readonly string[] s_buttonFamilies = { "Default", "Accept" };
        static readonly string[] s_bandKinds = { "Panel", "Header", "Inset", "Divider" };

        readonly Dictionary<string, Color> _pieceColours = new Dictionary<string, Color>();
        GameObject _holder;
        GuildWindow _guild;
        BlacksmithWindow _smith;

        IEnumerable<Component> Windows => new Component[] { _guild, _smith };

        [SetUp]
        public void Setup()
        {
            _holder = new GameObject("GuildBrightRoot");
            _guild = GuildWindow.BuildRuntime(_holder.transform);
            _smith = BlacksmithWindow.BuildRuntime(_holder.transform);
            var model = new GuildDisplayModel();
            model.SetGuild(new GuildStatusDisplay("새벽 길드", 2, 2, 30, 1, 2, false, 30, GuildActionAvailability.Available, false,
                GuildActionResult.None));
            model.SetBlacksmith(new BlacksmithDisplay(CharacterClass.Knight, 0, 1, 0, 20, 1, GuildActionAvailability.Available,
                false, GuildActionResult.None));
            _guild.Bind(model);
            _smith.Bind(model);
            _guild.SetOpen(true);
            _smith.SetOpen(true);
        }

        [TearDown]
        public void Teardown()
        {
            if (_holder != null) Object.DestroyImmediate(_holder);
            _pieceColours.Clear();
        }

        [Test]
        public void BothWindows_PanelHeaderAndEveryInset_AreTheBrightPieces()
        {
            var problems = new List<string>();
            var found = new Dictionary<string, int> { ["Panel"] = 0, ["Header"] = 0, ["Inset"] = 0 };
            foreach (Component window in Windows)
            {
                foreach (Image image in window.GetComponentsInChildren<Image>(true))
                {
                    string expected = image.name == "Guild" || image.name == "Blacksmith" ? "Panel_Default"
                        : image.name == "Header" ? "Header" : image.name == "Inset" ? "Inset_Default" : null;
                    if (expected == null) continue;
                    found[expected == "Panel_Default" ? "Panel" : expected == "Header" ? "Header" : "Inset"]++;
                    string shown = PieceOf(image.sprite)?.Name;
                    if (shown != expected) problems.Add($"{PathOf(image)} shows {Describe(image.sprite)}, expected bright {expected}");
                }
            }
            Debug.Log("[guild bright] pieces found: " + string.Join(", ", found.Select(pair => pair.Key + " " + pair.Value)));

            Assert.AreEqual(2, found["Panel"], "precondition: both panels were found");
            Assert.GreaterOrEqual(found["Header"], 2, "precondition: both header bands were found");
            Assert.GreaterOrEqual(found["Inset"], 2, "precondition: both windows' insets were found");
            Assert.IsEmpty(problems, "T4 the panels, headers and insets of both windows are the bright pieces");
        }

        [Test]
        public void GuildWindow_InfoAndTreasuryBands_AreBrightPieces()
        {
            var problems = new List<string>();
            foreach (string name in new[] { "GuildInfoBand", "TreasuryBand" })
            {
                Image[] bands = _guild.GetComponentsInChildren<Image>(true).Where(image => image.name == name).ToArray();
                Assert.AreEqual(1, bands.Length, $"precondition: one {name}");
                BrightPiece piece = PieceOf(bands[0].sprite);
                Debug.Log($"[guild bright] {name} shows {Describe(bands[0].sprite)}");
                if (piece == null || !s_bandKinds.Contains(piece.Kind))
                    problems.Add($"{name} shows {Describe(bands[0].sprite)}, expected a bright panel, header, inset or divider piece");
            }

            Assert.IsEmpty(problems, "T4 the guild information and treasury bands are bright pieces");
        }

        [Test]
        public void BothWindows_EveryButton_ShowsOneBrightButtonFamilyInEveryState()
        {
            var problems = new List<string>();
            Button[] buttons = Windows.SelectMany(window => window.GetComponentsInChildren<Button>(true)).ToArray();
            foreach (Button button in buttons)
            {
                var graphic = button.targetGraphic as Image;
                string normal = graphic != null ? PieceOf(graphic.sprite)?.Name : null;
                string family = s_buttonFamilies.FirstOrDefault(name => normal == $"Button_{name}_Normal");
                Debug.Log($"[guild bright] button {PathOf(button)} shows {Describe(graphic != null ? graphic.sprite : null)}");
                if (family == null)
                {
                    problems.Add($"{PathOf(button)} shows {Describe(graphic != null ? graphic.sprite : null)}, expected a bright button's normal piece");
                    continue;
                }
                if (button.transition != Selectable.Transition.SpriteSwap)
                    problems.Add($"{PathOf(button)} transition {button.transition}, expected SpriteSwap");
                SpriteState states = button.spriteState;
                ExpectState(button, states.highlightedSprite, $"Button_{family}_Highlighted", problems);
                ExpectState(button, states.pressedSprite, $"Button_{family}_Pressed", problems);
                ExpectState(button, states.disabledSprite, $"Button_{family}_Disabled", problems);
            }

            Assert.GreaterOrEqual(buttons.Length, 4, "precondition: the close, refresh, level-up and enhance buttons were found");
            Assert.IsEmpty(problems, "T4 every button of both windows is one bright button family in all its states");
        }

        [Test]
        public void BothWindows_NoBrownKitSpriteRemains()
        {
            var sprites = new List<(string Owner, Sprite Sprite)>();
            foreach (Component window in Windows)
            {
                sprites.AddRange(window.GetComponentsInChildren<Image>(true).Where(image => image.sprite != null)
                    .Select(image => (PathOf(image), image.sprite)));
                foreach (Selectable selectable in window.GetComponentsInChildren<Selectable>(true))
                {
                    SpriteState states = selectable.spriteState;
                    foreach ((string state, Sprite sprite) in new[] { ("highlighted", states.highlightedSprite),
                        ("pressed", states.pressedSprite), ("selected", states.selectedSprite), ("disabled", states.disabledSprite) })
                        if (sprite != null) sprites.Add(($"{PathOf(selectable)} {state}", sprite));
                }
            }
            string[] brown = sprites.Where(entry => AssetDatabase.GetAssetPath(entry.Sprite).StartsWith(BrownKitFolder))
                .Select(entry => $"{entry.Owner} shows {AssetDatabase.GetAssetPath(entry.Sprite)}").ToArray();
            int bright = sprites.Count(entry => PieceOf(entry.Sprite) != null);
            Debug.Log($"[guild bright] sprites inspected {sprites.Count}: bright {bright}, brown {brown.Length}");

            Assert.Greater(sprites.Count(entry => PieceOf(entry.Sprite) != null || AssetDatabase.GetAssetPath(entry.Sprite).StartsWith(BrownKitFolder)),
                0, "precondition: kit sprites were found to inspect");
            Assert.IsEmpty(brown, "T4 no brown kit sprite remains in either window");
        }

        // 「설계 고정」: dark text that reads on the bright parchment, and light text only where the piece under it is dark.
        // Measured as the contrast of the text colour with the mean opaque colour of the bright piece drawn under the
        // text's centre (its 9-slice centre, tinted by the image colour).
        [Test]
        public void BothWindows_EveryText_ReadsAtLeast3To1OnTheBrightPieceUnderIt()
        {
            var problems = new List<string>();
            int judged = 0;
            foreach (Component window in Windows)
            {
                Graphic[] drawOrder = window.GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < drawOrder.Length; i++)
                {
                    if (!(drawOrder[i] is TMP_Text text) || string.IsNullOrEmpty(text.text) || !text.gameObject.activeInHierarchy) continue;
                    judged++;
                    Image under = Under(drawOrder, i, text);
                    BrightPiece piece = under != null ? PieceOf(under.sprite) : null;
                    if (piece == null)
                    {
                        problems.Add($"{PathOf(text)} \"{text.text}\" lies on {(under == null ? "nothing" : Describe(under.sprite))}, not a bright piece");
                        continue;
                    }
                    Color background = PieceColour(piece) * under.color;
                    float contrast = Contrast(text.color, background);
                    Debug.Log($"[guild bright] {PathOf(text)} on {piece.Name}: contrast {contrast:F2}");
                    if (contrast < MinimumContrast)
                        problems.Add($"{PathOf(text)} \"{text.text}\" {contrast:F2}:1 on {piece.Name}");
                }
            }

            Assert.Greater(judged, 0, "precondition: texts were found to judge");
            Assert.IsEmpty(problems, "T4 every text reads at 3:1 or more on the bright piece under it");
        }

        // Controls of the measure above: the approved character menu inks pass on Panel_Default and white fails there.
        [Test]
        public void ContrastMeasure_ApprovedInksPassOnTheParchment_AndWhiteFails()
        {
            Color parchment = PieceColour(Named("Panel_Default"));
            float ink = Contrast(s_approvedInk, parchment);
            float result = Contrast(s_approvedResult, parchment);
            float white = Contrast(Color.white, parchment);
            Debug.Log($"[guild bright] Panel_Default mean {parchment}: ink {ink:F2}, result {result:F2}, white {white:F2}");

            Assert.GreaterOrEqual(ink, MinimumContrast, "positive control: the approved ink reads on the parchment");
            Assert.GreaterOrEqual(result, MinimumContrast, "positive control: the approved result ink reads on the parchment");
            Assert.Less(white, MinimumContrast, "negative control: white text does not read on the parchment");
        }

        // The last image before the text in draw order (hierarchy order) whose rectangle holds the text's centre.
        static Image Under(Graphic[] drawOrder, int textIndex, TMP_Text text)
        {
            Vector3 centre = text.rectTransform.TransformPoint(text.rectTransform.rect.center);
            for (int i = textIndex - 1; i >= 0; i--)
            {
                if (!(drawOrder[i] is Image image) || !image.gameObject.activeInHierarchy || !image.enabled) continue;
                if (image.rectTransform.rect.Contains((Vector2)image.rectTransform.InverseTransformPoint(centre))) return image;
            }
            return null;
        }

        static void ExpectState(Button button, Sprite sprite, string expected, List<string> problems)
        {
            string shown = PieceOf(sprite)?.Name;
            if (shown != expected) problems.Add($"{PathOf(button)} state shows {shown ?? Describe(sprite)}, expected {expected}");
        }

        // Mean opaque colour of the piece's 9-slice centre, read from the approved PNG itself.
        Color PieceColour(BrightPiece piece)
        {
            if (_pieceColours.TryGetValue(piece.Name, out Color known)) return known;
            string asset = AssetDatabase.GUIDToAssetPath(piece.Guid);
            Assert.IsFalse(string.IsNullOrEmpty(asset), $"fixture: the {piece.Name} asset");
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), asset))),
                    $"fixture: {asset} decodes");
                float scale = texture.width / (float)piece.Width;
                int left = Mathf.RoundToInt(piece.Left * scale), right = texture.width - Mathf.RoundToInt(piece.Right * scale);
                int bottom = Mathf.RoundToInt(piece.Bottom * scale), top = texture.height - Mathf.RoundToInt(piece.Top * scale);
                Color32[] pixels = texture.GetPixels32();
                double red = 0, green = 0, blue = 0;
                int count = 0;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                    {
                        Color32 pixel = pixels[y * texture.width + x];
                        if (pixel.a < 128) continue;
                        red += pixel.r;
                        green += pixel.g;
                        blue += pixel.b;
                        count++;
                    }
                Assert.Greater(count, 0, $"fixture: {piece.Name} has opaque centre pixels");
                var colour = new Color((float)(red / count / 255d), (float)(green / count / 255d), (float)(blue / count / 255d), 1f);
                _pieceColours[piece.Name] = colour;
                return colour;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // WCAG 2.x contrast ratio of two sRGB colours.
        static float Contrast(Color a, Color b)
        {
            float first = Luminance(a), second = Luminance(b);
            return (Mathf.Max(first, second) + 0.05f) / (Mathf.Min(first, second) + 0.05f);
        }

        static float Luminance(Color colour) => 0.2126f * Linear(colour.r) + 0.7152f * Linear(colour.g) + 0.0722f * Linear(colour.b);

        static float Linear(float channel) => channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

        static string Describe(Sprite sprite) => sprite == null ? "no sprite"
            : PieceOf(sprite)?.Name ?? $"{sprite.name} ({AssetDatabase.GetAssetPath(sprite)})";

        static string PathOf(Component component)
        {
            var names = new List<string>();
            for (Transform at = component.transform; at != null; at = at.parent) names.Insert(0, at.name);
            return string.Join("/", names.Skip(1));
        }
    }
}
