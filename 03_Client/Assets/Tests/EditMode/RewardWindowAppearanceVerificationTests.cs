using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent verification of the dungeon reward window art (verifies task_f7615fd3886f), next to the six
    // RewardWindow_ checks of UiWindowAppearanceContractTests. Requirement sources:
    // - 01_Phases/goals/2026-10-13-demo-video-art/goal.md 「적용 중인 사용자 결정」 보상 창 여백 (Main msg_438dc8c80199,
    //   user A): the title and the three reward rows keep their text position and size, and the approved title band
    //   and row backdrops are laid behind those same areas. The exception covers only the title, its band and the
    //   three rows; no other layout, colour or wording is chosen.
    // - ui-window-application-scope.md 「보존 계약」: the approved PNGs stay at PPU 200, Single/FullRect, meta border in
    //   PNG px, and Canvas reference PPU 100 is kept apart from the PNG density 2; no tint.
    // Kept literals come from DungeonClearRewardWindow.BuildView before the art change (39e59d01) and approved values
    // from Art/UI/Components/import-spec.json and the .meta files. Nothing is computed from the code under test: the
    // observers read RectTransform corners, draw order, the TextureImporter and the mesh each Image builds for its own
    // canvas. The control test shows each observer reports the deviation it exists to see.
    public sealed class RewardWindowAppearanceVerificationTests
    {
        const string KitFolder = "Assets/Art/UI/Components/";
        // import-spec units.pixelDensity: PNG px per UI unit at Canvas referencePixelsPerUnit 100.
        const float Density = 2f;
        // UI units; the layout literals are whole units and this only absorbs float rounding through transforms.
        const float Tolerance = 0.5f;
        const float MeshTolerance = 0.01f;
        const string TitleText = "던전 클리어";
        const string ConfirmText = "확인";

        // One approved piece the reward window may show: import-spec name, .meta GUID, PNG SHA256, PNG size and the
        // equal L/R/T/B border in PNG px.
        sealed class Approved
        {
            internal readonly string Name;
            internal readonly string Guid;
            internal readonly string Sha256;
            internal readonly Vector2Int PixelSize;
            internal readonly int Border;

            internal Approved(string name, string guid, string sha256, int width, int height, int border)
            {
                Name = name;
                Guid = guid;
                Sha256 = sha256;
                PixelSize = new Vector2Int(width, height);
                Border = border;
            }

            internal string AssetPath => KitFolder + Name + ".png";
        }

        static readonly Approved[] Pieces =
        {
            new Approved("Panel_Emphasis", "7dec580f6ab94568af987e047f5a2c36",
                "42f83c6196753ca98e9827a4c3bbb7702f4f5305cafea65882cdb598ed2faeb2", 512, 384, 24),
            new Approved("Header", "1371d63dc50f40ebae0726a4d60203b5",
                "b102ddcb312d5471b7117a9dab26ab74a5e10091332ba04f16578192a9894cb8", 384, 80, 16),
            new Approved("Inset_Default", "baf557470603430e83015dc40371610a",
                "e6c0507eb16efd633011b73eb10ef743b100f27630e47998f3e040686ad02220", 192, 128, 12),
            new Approved("Inset_Selected", "e2c7e2ef02fc41d38527480b1023601b",
                "1a662ed0208c587c5cbfcdf8fbb8f50b5ef3710a6893d17290eff2133091d086", 192, 128, 12),
            new Approved("Inset_Disabled", "dd85577f51e84c7188a5a8d8a3b03517",
                "483b0e0110b8cc6c85b575a7a01e10612ae957564c10284dd95a88a1f69d3422", 192, 128, 12),
            new Approved("Button_Accept_Normal", "304984e555104bf5b7d8648af717c165",
                "337b52d28762a586832a08a7dbcf907f8a50bdf8aa23a89bbe730c1544258de8", 256, 96, 16),
            new Approved("Button_Accept_Highlighted", "ce276490b85b4f7fa5f674b0f2546e87",
                "c504ce98c7558038b2e0d3f1bd0dc5e7c27c8427a63e4e52687ef80fd4d393c9", 256, 96, 16),
            new Approved("Button_Accept_Pressed", "0525c0bba70a41ad8bf71baebe25bc9f",
                "6c62b138f3b631754425d9e68b81b0f8e07b8c22b4b8d4fe1ed227c59e994cf3", 256, 96, 16),
            new Approved("Button_Accept_Disabled", "296d9f9387434c4cbc07115da1352309",
                "dac87078c460d1165cc20d15a978ab76671821946e4e69af12339197b0848f3d", 256, 96, 16),
        };

        // Text areas BuildView placed before the art change (39e59d01 DungeonClearRewardWindow.cs BuildView): offset of
        // the top-left from the panel's top-left and size, UI units, y downward.
        static readonly Rect TitleArea = new Rect(24f, 14f, 472f, 50f);
        static readonly Rect SummaryArea = new Rect(24f, 70f, 472f, 76f);
        static readonly Rect[] RowAreas =
        {
            new Rect(24f, 146f, 472f, 40f), new Rect(24f, 188f, 472f, 40f), new Rect(24f, 230f, 472f, 40f),
        };
        static readonly Rect ConfirmArea = new Rect(170f, 292f, 180f, 54f);

        static readonly MethodInfo s_populateMesh = typeof(Graphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);

        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(DungeonClearRewardWindow.Instance, "fixture: another test left a reward window singleton");
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        // User A: same areas, decoration behind. Also the kept wording and text settings of 39e59d01 BuildView/Label.
        [Test]
        public void RewardWindow_TitleBandAndRowBackdrops_SitBehindTheKeptTextAreas()
        {
            RewardWindow window = BuildReward();
            Image[] headers = Decorations(window, piece => piece.Name == "Header");
            Image[] insets = Decorations(window, piece => piece.Name.StartsWith("Inset_", StringComparison.Ordinal))
                .OrderBy(inset => FromTopLeft(window.Panel, inset.rectTransform).y).ToArray();
            Log("reward decorations", headers.Concat(insets).Select(image =>
                $"{image.name} {PieceOf(image.sprite)?.Name} {FromTopLeft(window.Panel, image.rectTransform)}"));

            var problems = new List<string>();
            problems.AddRange(KeptAreaProblems(window.Panel, window.Title, TitleArea, "title"));
            problems.AddRange(KeptAreaProblems(window.Panel, window.Summary, SummaryArea, "summary"));
            for (int i = 0; i < RowAreas.Length; i++)
                problems.AddRange(KeptAreaProblems(window.Panel, window.Rows[i], RowAreas[i], "reward row " + (i + 1)));
            problems.AddRange(KeptAreaProblems(window.Panel, window.Caption, ConfirmArea, "「확인」 caption"));
            if (headers.Length == 1)
                problems.AddRange(BackdropProblems(window, window.Title, headers[0], TitleArea, "title band"));
            if (insets.Length == RowAreas.Length)
            {
                for (int i = 0; i < RowAreas.Length; i++)
                    problems.AddRange(BackdropProblems(window, window.Rows[i], insets[i], RowAreas[i], "reward row " + (i + 1) + " backdrop"));
            }
            Log("reward kept areas and backdrops", problems);

            Assert.AreEqual(1, headers.Length, "user A: one approved title band (Header) behind the title");
            Assert.AreEqual(RowAreas.Length, insets.Length, "user A: one approved Inset_* backdrop behind each of the three reward rows");
            Assert.IsEmpty(problems, "user A: the kept text areas are unchanged and each backdrop covers its area from behind");
            Assert.AreEqual(TitleText, window.Title.text, "C the title wording (39e59d01 BuildView)");
            Assert.AreEqual(30f, window.Title.fontSize, 1e-4f, "C the title size (39e59d01 BuildView)");
            Assert.AreEqual(FontStyles.Bold, window.Title.fontStyle, "C the bold title (39e59d01 BuildView)");
            Assert.IsTrue(SameColour(new Color(1f, 0.90f, 0.35f, 1f), window.Title.color), $"C the title colour (39e59d01 BuildView), actual {window.Title.color}");
            Assert.AreEqual(ConfirmText, window.Caption.text, "C the 「확인」 wording (39e59d01 BuildView)");
            foreach (TMP_Text label in new[] { window.Summary, window.Caption }.Concat(window.Rows))
                Assert.AreEqual(24f, label.fontSize, 1e-4f, $"C {label.name} keeps the Label size 24 (39e59d01 Label)");
            foreach (TMP_Text label in new[] { window.Title, window.Summary, window.Caption }.Concat(window.Rows))
            {
                Assert.AreEqual(TextAlignmentOptions.Center, label.alignment, $"C {label.name} stays centred (39e59d01 Label)");
                Assert.AreEqual(TextWrappingModes.Normal, label.textWrappingMode, $"C {label.name} keeps wrapping (39e59d01 Label)");
                Assert.AreEqual(TextOverflowModes.Overflow, label.overflowMode, $"C {label.name} keeps overflow (39e59d01 Label)");
                Assert.IsFalse(label.raycastTarget, $"C {label.name} passes the pointer (39e59d01 Label)");
            }
        }

        // The exception is the title, its band and three rows only, so the window shows exactly the contract's pieces:
        // the Emphasis background, one Header, three Inset_* and the Accept button graphic. Nothing else is drawn.
        [Test]
        public void RewardWindow_ShowsOnlyTheContractPieces_WithNoOtherDecorationOrPlainImage()
        {
            RewardWindow window = BuildReward();
            string[] inventory = DrawnInventory(window.Window.transform);
            Log("reward drawn inventory", inventory);

            CollectionAssert.AreEquivalent(
                new[] { "Panel_Emphasis", "Header", "Inset_*", "Inset_*", "Inset_*", "Button_Accept_Normal" }, inventory,
                "user A / contract A: only Panel_Emphasis, the title Header, three row Inset_* and the Accept button are drawn");
        }

        // A: every piece the window shows (and each Accept state sprite) is the approved PNG with its approved import,
        // and its Image draws the 9-slice border at PNG px / density 2 on the window's own canvas (reference PPU 100).
        [Test]
        public void RewardWindow_ShownPieces_KeepTheApprovedImport_AndDrawTheirBordersAtCanvasDensity()
        {
            RewardWindow window = BuildReward();
            var scaler = window.Window.GetComponent<CanvasScaler>();
            var problems = new List<string>();
            var seen = new List<string>();
            foreach (Image image in window.Window.GetComponentsInChildren<Image>(true).Where(candidate => candidate.enabled))
            {
                Approved piece = PieceOf(image.sprite);
                if (piece == null)
                {
                    problems.Add($"{image.name}: {Describe(image.sprite)} is not an approved piece");
                    continue;
                }
                seen.Add(piece.Name);
                problems.AddRange(ImportProblems(piece).Select(problem => $"{image.name} ({piece.Name}): {problem}"));
                problems.AddRange(DrawnBorderProblems(image, piece.Border / Density).Select(problem => $"{image.name} ({piece.Name}): {problem}"));
            }
            SpriteState states = window.Confirm.spriteState;
            foreach (Sprite state in new[] { states.highlightedSprite, states.pressedSprite, states.disabledSprite })
            {
                Approved piece = PieceOf(state);
                if (piece == null) problems.Add($"Confirm state {Describe(state)} is not an approved piece");
                else problems.AddRange(ImportProblems(piece).Select(problem => $"Confirm state {piece.Name}: {problem}"));
            }
            Log("reward pieces seen", seen);
            Log("reward import and border", problems);

            Assert.AreEqual(100f, scaler.referencePixelsPerUnit, 1e-4f, "A Canvas reference PPU 100, kept apart from the PNG PPU 200");
            Assert.IsEmpty(problems, "A each shown piece keeps its approved PNG and import and draws its border at PNG px / 2 UI");
        }

        // Controls on objects this test builds: a backdrop moved by 2 UI, a band in front of its text, an extra plain
        // Image, a meta border halved by the density and a multiplier that halves the drawn border are each reported,
        // while the approved arrangement of the same objects passes.
        [Test]
        public void Control_RewardObservers_ReportAMovedOrFrontBackdropAnExtraImageAHalvedBorderAndADenseImage()
        {
            RewardWindow window = BuildReward();
            Image header = Decorations(window, piece => piece.Name == "Header").Single();
            List<string> approvedBackdrop = BackdropProblems(window, window.Title, header, TitleArea, "title band");
            RectTransform headerRect = header.rectTransform;
            Vector2 kept = headerRect.anchoredPosition;
            headerRect.anchoredPosition = kept + new Vector2(2f, 0f);
            List<string> movedBackdrop = BackdropProblems(window, window.Title, header, TitleArea, "title band");
            headerRect.anchoredPosition = kept;
            int keptIndex = headerRect.GetSiblingIndex();
            headerRect.SetSiblingIndex(window.Title.transform.GetSiblingIndex());
            List<string> frontBackdrop = BackdropProblems(window, window.Title, header, TitleArea, "title band");
            headerRect.SetSiblingIndex(keptIndex);

            string[] cleanInventory = DrawnInventory(window.Window.transform);
            var plain = new GameObject("plain (control)", typeof(RectTransform)).AddComponent<Image>();
            plain.transform.SetParent(window.Panel, false);
            plain.color = new Color(0.06f, 0.07f, 0.10f, 0.95f);
            string[] dirtyInventory = DrawnInventory(window.Window.transform);

            Approved headerPiece = Pieces.Single(piece => piece.Name == "Header");
            var halvedMeta = new Approved(headerPiece.Name, headerPiece.Guid, headerPiece.Sha256, headerPiece.PixelSize.x,
                headerPiece.PixelSize.y, headerPiece.Border / 2);
            List<string> approvedImport = ImportProblems(headerPiece);
            List<string> halvedImport = ImportProblems(halvedMeta);
            List<string> approvedBorder = DrawnBorderProblems(header, headerPiece.Border / Density);
            header.pixelsPerUnitMultiplier = 2f;
            List<string> denseBorder = DrawnBorderProblems(header, headerPiece.Border / Density);
            header.pixelsPerUnitMultiplier = 1f;
            Log("control observers", movedBackdrop.Concat(frontBackdrop).Concat(halvedImport).Concat(denseBorder)
                .Concat(dirtyInventory.Select(entry => "inventory " + entry)));

            Assert.IsEmpty(approvedBackdrop, "control: the band as built passes the backdrop observer");
            Assert.IsNotEmpty(movedBackdrop, "control: a band moved by 2 UI off the title area is reported");
            Assert.IsNotEmpty(frontBackdrop, "control: a band drawn after (over) the title is reported");
            Assert.AreEqual(cleanInventory.Length + 1, dirtyInventory.Length, "control: an extra plain Image enters the drawn inventory");
            Assert.IsTrue(dirtyInventory.Any(entry => entry.StartsWith("plain colour", StringComparison.Ordinal)),
                "control: the extra Image is reported as a plain colour, not an approved piece");
            Assert.IsEmpty(approvedImport, "control: the approved Header import passes");
            Assert.IsNotEmpty(halvedImport, "control: a meta border divided by the density is reported");
            Assert.IsEmpty(approvedBorder, "control: the built Header draws its 16 px border as 8 UI");
            Assert.IsNotEmpty(denseBorder, "control: a multiplier that halves the drawn border is reported");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Window under test: built through the product entry point and found by what it shows
        // ---------------------------------------------------------------------------------------------------------

        sealed class RewardWindow
        {
            internal DungeonClearRewardWindow Window;
            internal RectTransform Panel;
            internal Button Confirm;
            internal TMP_Text Title;
            internal TMP_Text Summary;
            internal TMP_Text[] Rows;
            internal TMP_Text Caption;
        }

        RewardWindow BuildReward()
        {
            GameObject parent = Own(new GameObject("Reward window parent (verification)"));
            DungeonClearRewardWindow window = DungeonClearRewardWindow.BuildRuntime(parent.transform);
            Own(window.gameObject);
            Button confirm = One(window.GetComponentsInChildren<Button>(true)
                .Where(button => button.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text == ConfirmText)).ToArray(), "the 「확인」 button");
            TMP_Text[] texts = window.GetComponentsInChildren<TMP_Text>(true);
            // Summary and rows start empty, so they are found by the names BuildView gave them before and after the change.
            return new RewardWindow
            {
                Window = window,
                Panel = PanelOf(window.transform, confirm),
                Confirm = confirm,
                Title = One(texts.Where(text => text.text == TitleText).ToArray(), "the 「던전 클리어」 title"),
                Summary = One(texts.Where(text => text.name == "Summary").ToArray(), "Summary"),
                Rows = Enumerable.Range(0, RowAreas.Length).Select(i => One(texts.Where(text => text.name == "Reward" + i).ToArray(), "Reward" + i)).ToArray(),
                Caption = One(confirm.GetComponentsInChildren<TMP_Text>(true).Where(text => text.text == ConfirmText).ToArray(), "the 「확인」 caption"),
            };
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observers
        // ---------------------------------------------------------------------------------------------------------

        static Approved PieceOf(Sprite sprite)
        {
            if (sprite == null) return null;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _)) return null;
            return Pieces.FirstOrDefault(piece => piece.Guid == guid && piece.Name == sprite.name);
        }

        static string Describe(Sprite sprite)
        {
            if (sprite == null) return "no sprite";
            string guid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string found, out long _) ? found : "none";
            return $"sprite '{sprite.name}' (GUID {guid})";
        }

        // Enabled Images of the window showing an approved piece that is not a button graphic.
        static Image[] Decorations(RewardWindow window, Func<Approved, bool> kind) => window.Window.GetComponentsInChildren<Image>(true)
            .Where(image => image.enabled && !IsButtonGraphic(image))
            .Where(image => PieceOf(image.sprite) is Approved piece && kind(piece))
            .ToArray();

        static bool IsButtonGraphic(Graphic graphic)
        {
            var owner = graphic.GetComponentInParent<Selectable>(true);
            return owner != null && owner.targetGraphic == graphic;
        }

        // Every enabled non-text graphic of the window, by what it draws. Inset_* pieces count as one family, as the
        // approved row backdrop is any Inset_* piece; anything that is not an approved piece is named as such.
        static string[] DrawnInventory(Transform window) => window.GetComponentsInChildren<Graphic>(true)
            .Where(graphic => graphic.enabled && !(graphic is TMP_Text) && !(graphic is TMP_SubMeshUI))
            .Select(graphic =>
            {
                if (!(graphic is Image image)) return $"{graphic.GetType().Name} '{graphic.name}'";
                if (image.sprite == null) return $"plain colour '{image.name}' {image.color}";
                Approved piece = PieceOf(image.sprite);
                if (piece == null) return $"non-approved '{image.name}' {Describe(image.sprite)}";
                return piece.Name.StartsWith("Inset_", StringComparison.Ordinal) ? "Inset_*" : piece.Name;
            })
            .ToArray();

        static List<string> KeptAreaProblems(RectTransform panel, TMP_Text text, Rect area, string what)
        {
            var problems = new List<string>();
            Rect actual = FromTopLeft(panel, text.rectTransform);
            if (!SameRect(area, actual)) problems.Add($"{what} text area {actual}, kept {area}");
            return problems;
        }

        // A backdrop sits behind its text: the same panel-space area as the kept text area, under the same panel so the
        // window's own scaling moves both, and earlier in draw order so the text is drawn over it.
        static List<string> BackdropProblems(RewardWindow window, TMP_Text text, Image backdrop, Rect area, string what)
        {
            var problems = new List<string>();
            Rect actual = FromTopLeft(window.Panel, backdrop.rectTransform);
            if (!SameRect(area, actual)) problems.Add($"{what} covers {actual}, kept text area {area}");
            if (!backdrop.transform.IsChildOf(window.Panel)) problems.Add($"{what} is not under the panel that the window scales");
            List<Transform> drawOrder = window.Window.GetComponentsInChildren<Transform>(true).ToList();
            if (drawOrder.IndexOf(backdrop.transform) >= drawOrder.IndexOf(text.transform))
                problems.Add($"{what} is drawn after {text.name}, so it covers the text instead of lying behind it");
            if (backdrop.raycastTarget) problems.Add($"{what} takes raycasts");
            return problems;
        }

        // The approved import of one piece, read from the real TextureImporter, the PNG bytes and the loaded Sprite.
        static List<string> ImportProblems(Approved piece)
        {
            var problems = new List<string>();
            string path = AssetDatabase.GUIDToAssetPath(piece.Guid);
            if (path != piece.AssetPath)
            {
                problems.Add($"GUID {piece.Guid} resolves to '{path}', approved {piece.AssetPath}");
                return problems;
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                problems.Add("no TextureImporter");
                return problems;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var border = new Vector4(piece.Border, piece.Border, piece.Border, piece.Border);
            if (importer.textureType != TextureImporterType.Sprite) problems.Add($"texture type {importer.textureType}, approved Sprite");
            if (importer.spriteImportMode != SpriteImportMode.Single) problems.Add($"sprite mode {importer.spriteImportMode}, approved Single");
            if (settings.spriteMeshType != SpriteMeshType.FullRect) problems.Add($"mesh type {settings.spriteMeshType}, approved FullRect");
            if (Mathf.Abs(importer.spritePixelsPerUnit - 200f) > 1e-4f) problems.Add($"PPU {importer.spritePixelsPerUnit}, approved 200");
            if (importer.spriteBorder != border) problems.Add($"meta border {importer.spriteBorder}, approved {border} PNG px");
            if (!importer.alphaIsTransparency) problems.Add("alphaIsTransparency off, approved on");
            if (importer.mipmapEnabled) problems.Add("mipmaps on, approved off");
            if (!importer.sRGBTexture) problems.Add("sRGB off, approved on");
            if (importer.filterMode != FilterMode.Bilinear) problems.Add($"filter {importer.filterMode}, approved Bilinear");
            if (importer.wrapMode != TextureWrapMode.Clamp) problems.Add($"wrap {importer.wrapMode}, approved Clamp");
            if (importer.npotScale != TextureImporterNPOTScale.None) problems.Add($"npot {importer.npotScale}, approved None");
            if (importer.maxTextureSize != 2048) problems.Add($"max size {importer.maxTextureSize}, approved 2048");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) problems.Add($"compression {importer.textureCompression}, approved Uncompressed");
            if (importer.isReadable) problems.Add("read/write on, approved off");
            string sha = Sha256(path);
            if (!string.Equals(sha, piece.Sha256, StringComparison.OrdinalIgnoreCase)) problems.Add($"PNG SHA256 {sha}, approved {piece.Sha256}");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                problems.Add("does not load as a Sprite");
                return problems;
            }
            if (Mathf.Abs(sprite.pixelsPerUnit - 200f) > 1e-4f) problems.Add($"loaded Sprite PPU {sprite.pixelsPerUnit}, approved 200");
            if (sprite.border != border) problems.Add($"loaded Sprite border {sprite.border}, approved {border}");
            if (sprite.rect.size != (Vector2)piece.PixelSize) problems.Add($"loaded Sprite size {sprite.rect.size}, approved {piece.PixelSize}");
            return problems;
        }

        // The 9-slice the Image itself builds for its canvas: four grid lines per axis at the rect edges and at the
        // expected border thickness in UI units inside them, centre filled.
        static List<string> DrawnBorderProblems(Image image, float borderUi)
        {
            var problems = new List<string>();
            if (image.type != Image.Type.Sliced) problems.Add($"Image.type {image.type}, approved Sliced");
            if (!image.fillCenter) problems.Add("centre not filled");
            if (image.color != Color.white) problems.Add($"colour {image.color} tints the approved PNG");
            Rect rect = image.rectTransform.rect;
            List<float> xs, ys;
            int vertexCount;
            using (var helper = new VertexHelper())
            {
                s_populateMesh.Invoke(image, new object[] { helper });
                vertexCount = helper.currentVertCount;
                var vertex = new UIVertex();
                var xValues = new List<float>();
                var yValues = new List<float>();
                for (int i = 0; i < helper.currentVertCount; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    xValues.Add(vertex.position.x);
                    yValues.Add(vertex.position.y);
                }
                xs = Distinct(xValues);
                ys = Distinct(yValues);
            }
            float[] expectedXs = { rect.xMin, rect.xMin + borderUi, rect.xMax - borderUi, rect.xMax };
            float[] expectedYs = { rect.yMin, rect.yMin + borderUi, rect.yMax - borderUi, rect.yMax };
            if (!SameLines(expectedXs, xs)) problems.Add($"mesh columns [{string.Join(", ", xs)}], expected [{string.Join(", ", expectedXs)}] for {borderUi} UI borders");
            if (!SameLines(expectedYs, ys)) problems.Add($"mesh rows [{string.Join(", ", ys)}], expected [{string.Join(", ", expectedYs)}] for {borderUi} UI borders");
            // Nine filled cells of four vertices each.
            if (vertexCount != 36) problems.Add($"{vertexCount} mesh vertices, a filled 9-slice has 36");
            return problems;
        }

        static List<float> Distinct(List<float> values)
        {
            var result = new List<float>();
            foreach (float value in values.OrderBy(v => v))
                if (result.Count == 0 || Mathf.Abs(value - result[result.Count - 1]) > MeshTolerance) result.Add(value);
            return result;
        }

        static bool SameLines(float[] expected, List<float> actual) =>
            expected.Length == actual.Count && expected.Zip(actual, (e, a) => Mathf.Abs(e - a) <= MeshTolerance).All(match => match);

        // ---------------------------------------------------------------------------------------------------------
        // Geometry and lookup helpers
        // ---------------------------------------------------------------------------------------------------------

        static Rect InSpaceOf(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        // Offset of the target's top-left from the panel's top-left and its size, UI units with y downward.
        static Rect FromTopLeft(RectTransform panel, RectTransform target)
        {
            Rect r = InSpaceOf(panel, target), p = panel.rect;
            return new Rect(r.xMin - p.xMin, p.yMax - r.yMax, r.width, r.height);
        }

        static bool SameRect(Rect a, Rect b) =>
            Mathf.Abs(a.xMin - b.xMin) <= Tolerance && Mathf.Abs(a.xMax - b.xMax) <= Tolerance &&
            Mathf.Abs(a.yMin - b.yMin) <= Tolerance && Mathf.Abs(a.yMax - b.yMax) <= Tolerance;

        static bool SameColour(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) <= 1e-3f && Mathf.Abs(a.g - b.g) <= 1e-3f && Mathf.Abs(a.b - b.b) <= 1e-3f && Mathf.Abs(a.a - b.a) <= 1e-3f;

        // The window's top-level child holding the given element (the panel BuildView made under its canvas).
        static RectTransform PanelOf(Transform windowRoot, Component inside)
        {
            Transform t = inside.transform;
            while (t != null && t.parent != windowRoot) t = t.parent;
            Assert.IsNotNull(t, $"fixture: {inside.name} is not inside {windowRoot.name}");
            return (RectTransform)t;
        }

        static T One<T>(T[] found, string what)
        {
            Assert.AreEqual(1, found.Length, $"fixture: exactly one {what}");
            return found[0];
        }

        static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        T Own<T>(T owned) where T : UnityEngine.Object
        {
            _owned.Add(owned);
            return owned;
        }

        static void Log(string label, IEnumerable<string> lines)
        {
            foreach (string line in lines) TestContext.WriteLine($"[{label}] {line}");
        }
    }
}
