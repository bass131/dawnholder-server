using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // The approved UI pieces in Assets/Art/UI/Components, observed through the real TextureImporter, the loaded
    // Sprite, the uGUI Image mesh and a GPU render of that mesh with the imported texture.
    // Expected borders come from the approved import scope table (01_Phases/goals/2026-10-13-demo-video-art/
    // unity-import-scope.md, "import와 사용 계약"), never from the product import-spec.json. Pixel sizes are read
    // from each PNG header and the logical UI size is pixels / density 2 at Canvas referencePixelsPerUnit 100.
    // Evidence PNGs are written only when DAWNHOLDER_UI_KIT_EVIDENCE_DIR names a directory outside the project.
    public sealed class UiKitImportTests
    {
        const string ComponentsFolder = "Assets/Art/UI/Components";
        const string EvidenceDirectoryVariable = "DAWNHOLDER_UI_KIT_EVIDENCE_DIR";
        const float PngDensity = 2f;
        const float SpritePixelsPerUnit = 200f;
        const float CanvasReferencePixelsPerUnit = 100f;
        const float DividerHeightUi = 6f;
        const float GeometryTolerance = 0.001f;
        const float UvTolerance = 0.0001f;
        const int RenderMargin = 4;
        const byte VisibleAlpha = 8;
        const int ChannelTolerance = 2;
        const double BandMeanTolerance = 8.0;
        const int SeamAlphaDrop = 24;
        const int ZoomFactor = 8;

        static readonly Piece[] s_pieces = BuildPieces();
        static readonly float[] s_enlargements = { 1.5f, 2f };
        static readonly TextureFormat[] s_uncompressedFormats =
        {
            TextureFormat.RGBA32, TextureFormat.ARGB32, TextureFormat.BGRA32, TextureFormat.RGB24,
        };
        static readonly MethodInfo s_populateMesh = typeof(Graphic).GetMethod("OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);

        // A1: the folder holds exactly the approved 33 PNG names, grouped 22 sliced / 1 divider / 10 simple.
        [Test]
        public void ComponentsFolder_HoldsExactlyTheApprovedPngs()
        {
            string[] expected = s_pieces.Select(piece => piece.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] actual = Directory.GetFiles(ComponentsFolder, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            TestContext.Out.WriteLine($"pngs={actual.Length} [{string.Join(",", actual)}]");

            CollectionAssert.AreEqual(expected, actual, "Components must hold exactly the approved PNG names, no extra or missing piece");
            Assert.AreEqual(22, s_pieces.Count(piece => piece.Use == PieceUse.Sliced), "requirement table: 22 sliced pieces");
            Assert.AreEqual(1, s_pieces.Count(piece => piece.Use == PieceUse.Divider), "requirement table: 1 divider");
            Assert.AreEqual(10, s_pieces.Count(piece => piece.Use == PieceUse.Simple), "requirement table: 10 simple pieces");
        }

        // B1: the real TextureImporter of every piece carries the approved sprite settings and no platform override.
        [TestCaseSource(nameof(AllPieceNames))]
        public void Importer_UsesTheApprovedSpriteSettings(string name)
        {
            Piece piece = Find(name);
            var importer = AssetImporter.GetAtPath(piece.AssetPath) as TextureImporter;
            Assert.IsNotNull(importer, $"{piece.AssetPath} must be imported by a TextureImporter");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            TestContext.Out.WriteLine($"importer {name}: type={importer.textureType} mode={importer.spriteImportMode} " +
                $"mesh={settings.spriteMeshType} alignment={settings.spriteAlignment} pivot={importer.spritePivot} " +
                $"ppu={importer.spritePixelsPerUnit} border={importer.spriteBorder} mip={importer.mipmapEnabled} " +
                $"srgb={importer.sRGBTexture} alpha={importer.alphaSource}/{importer.alphaIsTransparency} " +
                $"filter={importer.filterMode} wrap={importer.wrapModeU}/{importer.wrapModeV}/{importer.wrapModeW} " +
                $"npot={importer.npotScale} max={importer.maxTextureSize} compression={importer.textureCompression} " +
                $"readable={importer.isReadable}");

            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, "texture type must be Sprite (2D and UI)");
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, "sprite mode must be Single");
            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, "sprite mesh must be Full Rect");
            Assert.AreEqual((int)SpriteAlignment.Center, settings.spriteAlignment, "pivot alignment must be Center");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), importer.spritePivot, "pivot must be the centre");
            Assert.AreEqual(SpritePixelsPerUnit, importer.spritePixelsPerUnit, "pixels per unit must be 200 (2x density)");
            Assert.IsFalse(importer.mipmapEnabled, "mipmaps must be off");
            Assert.IsTrue(importer.sRGBTexture, "colour texture must be sRGB");
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource, "alpha must come from the PNG");
            Assert.IsTrue(importer.alphaIsTransparency, "alpha must be treated as transparency");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, "filter must be Bilinear");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeU, "wrap U must be Clamp");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeV, "wrap V must be Clamp");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeW, "wrap W must be Clamp");
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "non-power-of-two size must be kept");
            Assert.AreEqual(2048, importer.maxTextureSize, "default max size must be 2048");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "default must be uncompressed");
            Assert.IsFalse(importer.isReadable, "CPU Read/Write must be off");
            Assert.AreEqual(piece.UnityBorder, importer.spriteBorder,
                "importer border must be the approved PNG-pixel border as Unity (L,B,R,T), not divided by density");
            // Only names this editor certainly knows are queried; an unknown platform name can log an error. The
            // serialized meta (one DefaultTexturePlatform entry, no override) is audited outside Unity.
            TextureImporterPlatformSettings defaults = importer.GetDefaultPlatformTextureSettings();
            Assert.AreEqual(2048, defaults.maxTextureSize, "default platform max size must be 2048");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, defaults.textureCompression,
                "default platform must stay uncompressed");
            Assert.IsFalse(importer.GetPlatformTextureSettings("Standalone").overridden,
                "Standalone must not override size or compression");
        }

        // B2: the loaded Sprite and its texture keep the PNG size, PPU 200, centre pivot, approved border and full rect.
        [TestCaseSource(nameof(AllPieceNames))]
        public void LoadedSprite_KeepsThePngSizeAndTheApprovedBorder(string name)
        {
            Piece piece = Find(name);
            Vector2Int png = ReadPngSize(piece.AssetPath);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(piece.AssetPath).OfType<Sprite>().ToArray();
            Assert.AreEqual(1, sprites.Length, $"{name}: Single mode must yield exactly one Sprite sub-asset");
            Sprite sprite = sprites[0];
            Texture2D texture = sprite.texture;
            TestContext.Out.WriteLine($"sprite {name}: png={png} texture={texture.width}x{texture.height} " +
                $"format={texture.format}/{texture.graphicsFormat} mips={texture.mipmapCount} filter={texture.filterMode} " +
                $"wrap={texture.wrapModeU}/{texture.wrapModeV} readable={texture.isReadable} rect={sprite.rect} " +
                $"ppu={sprite.pixelsPerUnit} pivot={sprite.pivot} border={sprite.border} vertices={sprite.vertices.Length} " +
                $"triangles={sprite.triangles.Length} bounds={sprite.bounds.size}");

            Assert.AreEqual(png.x, texture.width, "imported texture width must equal the PNG width (no resize)");
            Assert.AreEqual(png.y, texture.height, "imported texture height must equal the PNG height (no resize)");
            Assert.AreEqual(1, texture.mipmapCount, "imported texture must have no mip chain");
            Assert.AreEqual(FilterMode.Bilinear, texture.filterMode, "texture sampler must be Bilinear");
            Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapModeU, "texture wrap U must be Clamp");
            Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapModeV, "texture wrap V must be Clamp");
            Assert.IsFalse(texture.isReadable, "texture must not keep a CPU copy");
            Assert.That(s_uncompressedFormats, Does.Contain(texture.format), "texture must stay uncompressed");
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Assert.IsTrue(UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat),
                    "a linear-space project must sample this colour texture through an sRGB format");
            }
            Assert.AreEqual(new Rect(0, 0, png.x, png.y), sprite.rect, "Single sprite must cover the whole PNG");
            Assert.AreEqual(SpritePixelsPerUnit, sprite.pixelsPerUnit, "loaded sprite must use 200 pixels per unit");
            Assert.AreEqual(new Vector2(png.x / 2f, png.y / 2f), sprite.pivot, "loaded sprite pivot must be the centre");
            Assert.AreEqual(piece.UnityBorder, sprite.border, "loaded sprite border must be the approved (L,B,R,T) PNG pixels");
            Assert.AreEqual(4, sprite.vertices.Length, "Full Rect sprite mesh must be one quad");
            Assert.AreEqual(6, sprite.triangles.Length, "Full Rect sprite mesh must be two triangles");
            Assert.AreEqual(png.x / SpritePixelsPerUnit, sprite.bounds.size.x, GeometryTolerance, "sprite width in units");
            Assert.AreEqual(png.y / SpritePixelsPerUnit, sprite.bounds.size.y, GeometryTolerance, "sprite height in units");
        }

        // B3: forcing a re-import without setting changes keeps every GUID, meta and source byte, and the border.
        [Test]
        public void Reimport_WithoutSettingChanges_KeepsGuidMetaAndSourceBytes()
        {
            string[] paths = s_pieces.Select(piece => piece.AssetPath)
                .Concat(new[] { $"{ComponentsFolder}/README.md", $"{ComponentsFolder}/import-spec.json" })
                .ToArray();
            var changed = new List<string>();
            foreach (string path in paths)
            {
                string guidBefore = AssetDatabase.AssetPathToGUID(path);
                byte[] metaBefore = File.ReadAllBytes(path + ".meta");
                byte[] sourceBefore = File.ReadAllBytes(path);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                string guidAfter = AssetDatabase.AssetPathToGUID(path);
                byte[] metaAfter = File.ReadAllBytes(path + ".meta");
                byte[] sourceAfter = File.ReadAllBytes(path);
                bool sameGuid = guidBefore == guidAfter && guidBefore.Length == 32;
                bool sameMeta = metaBefore.SequenceEqual(metaAfter);
                bool sameSource = sourceBefore.SequenceEqual(sourceAfter);
                TestContext.Out.WriteLine($"reimport {path}: guid={guidBefore}->{guidAfter} meta={Sha256(metaBefore)}->" +
                    $"{Sha256(metaAfter)} source={Sha256(sourceBefore)}->{Sha256(sourceAfter)}");
                if (!sameMeta)
                {
                    // Unity rewrote a product meta: keep both versions as evidence and report; never restore here.
                    WriteEvidenceBytes($"reimport__{Path.GetFileName(path)}.meta.before", metaBefore);
                    WriteEvidenceBytes($"reimport__{Path.GetFileName(path)}.meta.after", metaAfter);
                }
                if (!sameGuid || !sameMeta || !sameSource) changed.Add(path);
            }
            foreach (Piece piece in s_pieces)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(piece.AssetPath);
                Assert.IsNotNull(sprite, $"{piece.Name} must still load as a Sprite after re-import");
                Assert.AreEqual(piece.UnityBorder, sprite.border, $"{piece.Name} border must survive the re-import");
                Assert.AreEqual(SpritePixelsPerUnit, sprite.pixelsPerUnit, $"{piece.Name} PPU must survive the re-import");
            }

            Assert.That(changed, Is.Empty, "re-import without setting changes must keep GUID, meta and source bytes");
        }

        // C1: sliced pieces keep border thickness (PNG px / 2 UI) at the spec size, 1.5x and 2x; quads tile the rect.
        [TestCaseSource(nameof(SlicedPieceNames))]
        public void SlicedImageMesh_KeepsBorderThicknessAtSpecAndLargerSizes(string name)
        {
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2 spec = SpecUiSize(piece);
            using var fixture = new UiImageFixture();
            foreach (float scale in new[] { 1f }.Concat(s_enlargements))
            {
                Vector2 size = spec * scale;
                List<string> problems = ObserveSlicedMesh(fixture.MeshOf(sprite, Image.Type.Sliced, size, false), piece,
                    size, ReadPngSize(piece.AssetPath), true);
                TestContext.Out.WriteLine($"mesh {name} {scale}x size={size} problems={problems.Count}");
                Assert.That(problems, Is.Empty, $"{name} at {scale}x: {string.Join("; ", problems)}");
            }
        }

        // C2: the minimum legal size (border sum / 2) keeps full borders; a smaller rect shrinks them (not normal use).
        [TestCaseSource(nameof(StretchablePieceNames))]
        public void SlicedImageMesh_SeparatesMinimumLegalSizeFromSmallerRects(string name)
        {
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2Int png = ReadPngSize(piece.AssetPath);
            float minimumWidth = (piece.Left + piece.Right) / PngDensity;
            float minimumHeight = piece.Use == PieceUse.Divider ? DividerHeightUi : (piece.Top + piece.Bottom) / PngDensity;
            var minimum = new Vector2(minimumWidth, minimumHeight);
            var smaller = new Vector2(minimumWidth * 0.5f, piece.Use == PieceUse.Divider ? DividerHeightUi : minimumHeight * 0.5f);
            using var fixture = new UiImageFixture();

            List<string> atMinimum = ObserveSlicedMesh(fixture.MeshOf(sprite, Image.Type.Sliced, minimum, false), piece,
                minimum, png, false);
            List<string> belowMinimum = ObserveSlicedMesh(fixture.MeshOf(sprite, Image.Type.Sliced, smaller, false), piece,
                smaller, png, false);
            TestContext.Out.WriteLine($"minimum {name}: legal={minimum} problems={atMinimum.Count}; " +
                $"smaller={smaller} problems={belowMinimum.Count} [{string.Join("; ", belowMinimum)}]");

            Assert.That(atMinimum, Is.Empty, $"{name} at the minimum legal size must keep full borders: {string.Join("; ", atMinimum)}");
            Assert.That(belowMinimum, Is.Not.Empty, $"{name} below the border sum must be reported as shrunken borders, not normal use");
        }

        // C3: the divider stretches only horizontally at 6 UI height; only its 16 px left/right ends stay fixed.
        [Test]
        public void DividerImageMesh_StretchesOnlyHorizontallyAtSixUiHeight()
        {
            Piece piece = Find("Divider_Horizontal");
            Sprite sprite = LoadSprite(piece);
            Vector2 spec = SpecUiSize(piece);
            Assert.AreEqual(DividerHeightUi, spec.y, GeometryTolerance, "divider PNG height / density must be 6 UI");
            using var fixture = new UiImageFixture();
            foreach (float scale in new[] { 1f }.Concat(s_enlargements))
            {
                var size = new Vector2(spec.x * scale, DividerHeightUi);
                List<string> problems = ObserveSlicedMesh(fixture.MeshOf(sprite, Image.Type.Sliced, size, false), piece,
                    size, ReadPngSize(piece.AssetPath), true);
                TestContext.Out.WriteLine($"divider mesh {scale}x size={size} problems={problems.Count}");
                Assert.That(problems, Is.Empty, $"divider at {scale}x width: {string.Join("; ", problems)}");
            }
        }

        // C4: simple pieces have no border and keep the PNG aspect inside stretched rects with preserveAspect on.
        [TestCaseSource(nameof(SimplePieceNames))]
        public void SimpleImageMesh_KeepsThePngAspectInStretchedRects(string name)
        {
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2Int png = ReadPngSize(piece.AssetPath);
            Vector2 spec = SpecUiSize(piece);
            Assert.AreEqual(Vector4.zero, sprite.border, $"{name} is a simple piece and must have no sprite border");
            using var fixture = new UiImageFixture();
            foreach (Vector2 rect in StretchedRects(spec))
            {
                List<string> problems = ObserveSimpleMesh(fixture.MeshOf(sprite, Image.Type.Simple, rect, true), png, rect);
                TestContext.Out.WriteLine($"simple mesh {name} rect={rect} problems={problems.Count}");
                Assert.That(problems, Is.Empty, $"{name} in {rect}: {string.Join("; ", problems)}");
            }
            var wide = new Vector2(spec.x * 2f, spec.y);
            List<string> control = ObserveSimpleMesh(fixture.MeshOf(sprite, Image.Type.Simple, wide, false), png, wide);
            Assert.That(control, Is.Not.Empty, $"{name}: the same observer must catch preserveAspect off in a stretched rect");
        }

        // C5: control - the same mesh observer catches a temporary border-0 copy and a halved-border copy
        // (border divided by density by mistake) built from the same imported texture.
        [TestCaseSource(nameof(StretchablePieceNames))]
        public void SlicedMeshObserver_CatchesBorderZeroAndHalvedBorderControls(string name)
        {
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2Int png = ReadPngSize(piece.AssetPath);
            Vector2 spec = SpecUiSize(piece);
            Vector2 size = piece.Use == PieceUse.Divider ? new Vector2(spec.x * 2f, DividerHeightUi) : spec * 2f;
            Sprite borderZero = CreateControlSprite(sprite, Vector4.zero);
            Sprite halvedBorder = CreateControlSprite(sprite, piece.UnityBorder / PngDensity);
            try
            {
                using var fixture = new UiImageFixture();
                List<string> zeroProblems = ObserveSlicedMesh(fixture.MeshOf(borderZero, Image.Type.Sliced, size, false),
                    piece, size, png, true);
                List<string> halvedProblems = ObserveSlicedMesh(fixture.MeshOf(halvedBorder, Image.Type.Sliced, size, false),
                    piece, size, png, true);
                TestContext.Out.WriteLine($"mesh control {name}: border0 problems={zeroProblems.Count} " +
                    $"halved problems={halvedProblems.Count} [{string.Join("; ", halvedProblems)}]");

                Assert.That(zeroProblems, Is.Not.Empty, $"{name}: a border-0 control must not pass as a sliced piece");
                Assert.That(halvedProblems, Is.Not.Empty, $"{name}: a halved-border control must not pass as the approved border");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(borderZero);
                UnityEngine.Object.DestroyImmediate(halvedBorder);
            }
        }

        // D1: GPU render of the real Image mesh with the imported texture. At the spec size the sliced render equals
        // the whole PNG drawn as Simple; at 1.5x and 2x the corners, edge-band profiles, outer margins and slice seams
        // match that whole-PNG reference.
        [TestCaseSource(nameof(StretchablePieceNames))]
        public void SlicedImageRender_KeepsCornersBandsAndOutlineWhenEnlarged(string name)
        {
            RequireGraphicsDevice();
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2 spec = SpecUiSize(piece);
            using var fixture = new UiImageFixture();
            Rendered reference = fixture.Render(sprite, Image.Type.Simple, spec, false);
            AssertVisible(reference, name);
            SaveEvidence(name, "reference-simple-1x", reference);

            Rendered atSpec = fixture.Render(sprite, Image.Type.Sliced, spec, false);
            SaveEvidence(name, "sliced-1x", atSpec);
            int specDifference = MaxChannelDifference(reference, atSpec);
            TestContext.Out.WriteLine($"render {name} 1x: size={spec} visible={reference.VisibleRatio():F3} " +
                $"maxDifferenceToWholePng={specDifference}");
            Assert.LessOrEqual(specDifference, ChannelTolerance, $"{name}: at the spec size slicing must not change any pixel");

            foreach (float scale in s_enlargements)
            {
                Vector2 size = piece.Use == PieceUse.Divider ? new Vector2(spec.x * scale, spec.y) : spec * scale;
                Rendered enlarged = fixture.Render(sprite, Image.Type.Sliced, size, false);
                SaveEvidence(name, $"sliced-{scale:0.0}x", enlarged);
                List<string> problems = ObserveSlicedRender(reference, enlarged, piece);
                TestContext.Out.WriteLine($"render {name} {scale}x: size={size} problems={problems.Count} " +
                    $"[{string.Join("; ", problems)}]");
                Assert.That(problems, Is.Empty, $"{name} at {scale}x: {string.Join("; ", problems)}");
                if (scale == 2f) SaveCornerZooms(name, "sliced-2.0x", enlarged, piece);
            }
            SaveCornerZooms(name, "reference-simple-1x", reference, piece);
        }

        // D2: control - a temporary border-0 copy of the same texture rendered Sliced at 2x is caught by the same
        // render observer. The approved asset is not touched.
        [TestCaseSource(nameof(StretchablePieceNames))]
        public void SlicedRenderObserver_CatchesBorderZeroControl(string name)
        {
            RequireGraphicsDevice();
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2 spec = SpecUiSize(piece);
            Vector2 size = piece.Use == PieceUse.Divider ? new Vector2(spec.x * 2f, spec.y) : spec * 2f;
            Sprite borderZero = CreateControlSprite(sprite, Vector4.zero);
            try
            {
                using var fixture = new UiImageFixture();
                Rendered reference = fixture.Render(sprite, Image.Type.Simple, spec, false);
                AssertVisible(reference, name);
                Rendered control = fixture.Render(borderZero, Image.Type.Sliced, size, false);
                SaveEvidence(name, "control-border0-2.0x", control);
                SaveCornerZooms(name, "control-border0-2.0x", control, piece);
                List<string> problems = ObserveSlicedRender(reference, control, piece);
                TestContext.Out.WriteLine($"render control {name}: problems={problems.Count} [{string.Join("; ", problems)}]");

                Assert.That(problems, Is.Not.Empty, $"{name}: the render observer must catch a border-0 sprite stretched to 2x");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(borderZero);
            }
        }

        // D3: simple pieces keep their drawn shape aspect in stretched rects (circles stay round); the same observer
        // catches preserveAspect off.
        [TestCaseSource(nameof(SimplePieceNames))]
        public void SimpleImageRender_KeepsShapeAspectInStretchedRects(string name)
        {
            RequireGraphicsDevice();
            Piece piece = Find(name);
            Sprite sprite = LoadSprite(piece);
            Vector2 spec = SpecUiSize(piece);
            using var fixture = new UiImageFixture();
            Rendered reference = fixture.Render(sprite, Image.Type.Simple, spec, true);
            AssertVisible(reference, name);
            SaveEvidence(name, "reference-simple-1x", reference);
            foreach (Vector2 rect in StretchedRects(spec).Skip(1))
            {
                Rendered stretched = fixture.Render(sprite, Image.Type.Simple, rect, true);
                SaveEvidence(name, $"preserve-{rect.x:0}x{rect.y:0}", stretched);
                List<string> problems = ObserveSimpleRender(reference, stretched);
                TestContext.Out.WriteLine($"simple render {name} rect={rect}: problems={problems.Count} " +
                    $"[{string.Join("; ", problems)}]");
                Assert.That(problems, Is.Empty, $"{name} in {rect}: {string.Join("; ", problems)}");
            }
            var wide = new Vector2(spec.x * 2f, spec.y);
            Rendered control = fixture.Render(sprite, Image.Type.Simple, wide, false);
            SaveEvidence(name, "control-no-preserve-wide", control);
            List<string> controlProblems = ObserveSimpleRender(reference, control);
            TestContext.Out.WriteLine($"simple render control {name}: problems={controlProblems.Count}");
            Assert.That(controlProblems, Is.Not.Empty, $"{name}: the observer must catch preserveAspect off in a wide rect");
        }

        static IEnumerable<string> AllPieceNames() => s_pieces.Select(piece => piece.Name);

        static IEnumerable<string> SlicedPieceNames() =>
            s_pieces.Where(piece => piece.Use == PieceUse.Sliced).Select(piece => piece.Name);

        static IEnumerable<string> StretchablePieceNames() =>
            s_pieces.Where(piece => piece.Use != PieceUse.Simple).Select(piece => piece.Name);

        static IEnumerable<string> SimplePieceNames() =>
            s_pieces.Where(piece => piece.Use == PieceUse.Simple).Select(piece => piece.Name);

        // Requirement table: approved scope "import와 사용 계약" (PNG px, L/R/T/B) and the approved piece names.
        static Piece[] BuildPieces()
        {
            var pieces = new List<Piece>();
            foreach (string panel in new[] { "Panel_Default", "Panel_Emphasis", "Panel_Dialog" })
            {
                pieces.Add(new Piece(panel, PieceUse.Sliced, 24, 24, 24, 24));
            }
            foreach (string inset in new[] { "Inset_Default", "Inset_Selected", "Inset_Disabled" })
            {
                pieces.Add(new Piece(inset, PieceUse.Sliced, 12, 12, 12, 12));
            }
            pieces.Add(new Piece("Header", PieceUse.Sliced, 16, 16, 16, 16));
            foreach (string family in new[] { "Default", "Accept", "Decline" })
            {
                foreach (string state in new[] { "Normal", "Highlighted", "Pressed", "Disabled" })
                {
                    pieces.Add(new Piece($"Button_{family}_{state}", PieceUse.Sliced, 16, 16, 16, 16));
                }
            }
            foreach (string slot in new[] { "ItemSlot_Empty", "ItemSlot_Filled", "ItemSlot_Selected" })
            {
                pieces.Add(new Piece(slot, PieceUse.Sliced, 16, 16, 16, 16));
            }
            pieces.Add(new Piece("Divider_Horizontal", PieceUse.Divider, 16, 16, 0, 0));
            string[] simple =
            {
                "IconBase_64", "IconBase_96", "CircleButton_Normal", "CircleButton_Highlighted", "CircleButton_Pressed",
                "CircleButton_Disabled", "NumberBadge", "PortraitFrame_128", "Ornament_Corner", "Ornament_HeaderEnd",
            };
            foreach (string piece in simple)
            {
                pieces.Add(new Piece(piece, PieceUse.Simple, 0, 0, 0, 0));
            }
            return pieces.ToArray();
        }

        static Piece Find(string name)
        {
            Piece piece = s_pieces.FirstOrDefault(candidate => candidate.Name == name);
            Assert.IsNotNull(piece, $"fixture: {name} is not in the requirement table");
            return piece;
        }

        static Sprite LoadSprite(Piece piece)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(piece.AssetPath);
            Assert.IsNotNull(sprite, $"{piece.AssetPath} must load as a Sprite");
            return sprite;
        }

        // Logical UI size from the measured PNG header and the 2x density; independent of the product spec file.
        static Vector2 SpecUiSize(Piece piece)
        {
            Vector2Int png = ReadPngSize(piece.AssetPath);
            return new Vector2(png.x / PngDensity, png.y / PngDensity);
        }

        static Vector2Int ReadPngSize(string assetPath)
        {
            byte[] bytes = File.ReadAllBytes(assetPath);
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.IsTrue(bytes.Length > 24 && bytes.Take(8).SequenceEqual(signature), $"{assetPath} must be a PNG");
            Assert.AreEqual("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4), $"{assetPath} must start with IHDR");
            return new Vector2Int(ReadBigEndian(bytes, 16), ReadBigEndian(bytes, 20));
        }

        static int ReadBigEndian(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

        static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        static IEnumerable<Vector2> StretchedRects(Vector2 spec)
        {
            yield return spec;
            yield return new Vector2(spec.x * 2f, spec.y);
            yield return new Vector2(spec.x, spec.y * 2f);
            yield return spec * 2f;
        }

        static Sprite CreateControlSprite(Sprite approved, Vector4 border)
        {
            Texture2D texture = approved.texture;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                SpritePixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }

        // Expected grid on one axis: [0, low, extent - high, extent] in UI units, or just [0, extent] when the axis
        // has no border. The quads must tile the rect without gaps or overlaps.
        static List<string> ObserveSlicedMesh(List<UIVertex> vertices, Piece piece, Vector2 size, Vector2Int png, bool checkUv)
        {
            var problems = new List<string>();
            float[] xs = DistinctSorted(vertices.Select(vertex => vertex.position.x));
            float[] ys = DistinctSorted(vertices.Select(vertex => vertex.position.y));
            CheckAxis(problems, "x", xs, size.x, piece.Left / PngDensity, piece.Right / PngDensity);
            CheckAxis(problems, "y", ys, size.y, piece.Bottom / PngDensity, piece.Top / PngDensity);

            // A zero-size cell of the approved grid (an axis without border, or the minimum legal size) draws
            // nothing, so only cells and quads with a positive width and height are counted.
            int expectedQuads = PositiveSpans(size.x, piece.Left / PngDensity, piece.Right / PngDensity) *
                PositiveSpans(size.y, piece.Bottom / PngDensity, piece.Top / PngDensity);
            int drawnQuads = 0;
            float area = 0f;
            for (int quad = 0; quad + 3 < vertices.Count; quad += 4)
            {
                IEnumerable<UIVertex> corners = vertices.Skip(quad).Take(4).ToArray();
                float width = corners.Max(v => v.position.x) - corners.Min(v => v.position.x);
                float height = corners.Max(v => v.position.y) - corners.Min(v => v.position.y);
                if (width > GeometryTolerance && height > GeometryTolerance) drawnQuads++;
                area += width * height;
            }
            if (vertices.Count % 4 != 0 || drawnQuads != expectedQuads)
            {
                problems.Add($"expected {expectedQuads} drawn quads on the approved grid, found {drawnQuads} " +
                    $"({vertices.Count} vertices)");
            }
            if (Mathf.Abs(area - size.x * size.y) > size.x * size.y * GeometryTolerance)
            {
                problems.Add($"quads cover {area:F3} UI^2, rect is {size.x * size.y:F3} (gap or overlap)");
            }

            if (checkUv)
            {
                int wrongUv = 0;
                foreach (UIVertex vertex in vertices)
                {
                    float expectedU = ExpectedUv(vertex.position.x, size.x, piece.Left, piece.Right, png.x);
                    float expectedV = ExpectedUv(vertex.position.y, size.y, piece.Bottom, piece.Top, png.y);
                    // NaN means the vertex is off the approved grid, which is itself a wrong sample position.
                    bool uOff = float.IsNaN(expectedU) || Mathf.Abs(vertex.uv0.x - expectedU) > UvTolerance;
                    bool vOff = float.IsNaN(expectedV) || Mathf.Abs(vertex.uv0.y - expectedV) > UvTolerance;
                    if (uOff || vOff) wrongUv++;
                }
                if (wrongUv > 0) problems.Add($"{wrongUv} vertices sample outside the approved border UV grid");
            }
            return problems;
        }

        static void CheckAxis(List<string> problems, string axis, float[] values, float extent, float low, float high)
        {
            if (values.Length == 0)
            {
                problems.Add($"{axis}: no vertices");
                return;
            }
            if (Mathf.Abs(values[0]) > GeometryTolerance || Mathf.Abs(values[values.Length - 1] - extent) > GeometryTolerance)
            {
                problems.Add($"{axis}: outline {values[0]:F3}..{values[values.Length - 1]:F3}, rect is 0..{extent:F3}");
            }
            if (low == 0f && high == 0f)
            {
                if (values.Length != 2) problems.Add($"{axis}: axis without border must not be sliced, grid [{string.Join(",", values)}]");
                return;
            }
            if (values.Length < 3)
            {
                problems.Add($"{axis}: no slice lines, grid [{string.Join(",", values)}]");
                return;
            }
            float lowWidth = values[1] - values[0];
            float highWidth = values[values.Length - 1] - values[values.Length - 2];
            if (Mathf.Abs(lowWidth - low) > GeometryTolerance) problems.Add($"{axis}: low border {lowWidth:F3} UI, approved {low:F3}");
            if (Mathf.Abs(highWidth - high) > GeometryTolerance) problems.Add($"{axis}: high border {highWidth:F3} UI, approved {high:F3}");
        }

        // Cells with a positive size along one axis of the approved grid [0, low, extent - high, extent].
        static int PositiveSpans(float extent, float low, float high)
        {
            float[] edges = { 0f, low, extent - high, extent };
            int spans = 0;
            for (int i = 0; i + 1 < edges.Length; i++)
            {
                if (edges[i + 1] - edges[i] > GeometryTolerance) spans++;
            }
            return spans;
        }

        // FullRect single sprite: outer UV 0..1, inner UV at the approved PNG-pixel border.
        static float ExpectedUv(float position, float extent, int lowPixels, int highPixels, int pngPixels)
        {
            float low = lowPixels / PngDensity;
            float high = highPixels / PngDensity;
            if (Mathf.Abs(position) <= GeometryTolerance) return 0f;
            if (Mathf.Abs(position - extent) <= GeometryTolerance) return 1f;
            if (Mathf.Abs(position - low) <= GeometryTolerance) return (float)lowPixels / pngPixels;
            if (Mathf.Abs(position - (extent - high)) <= GeometryTolerance) return 1f - (float)highPixels / pngPixels;
            return float.NaN;
        }

        static float[] DistinctSorted(IEnumerable<float> values)
        {
            var sorted = values.OrderBy(value => value).ToList();
            var distinct = new List<float>();
            foreach (float value in sorted)
            {
                if (distinct.Count == 0 || value - distinct[distinct.Count - 1] > GeometryTolerance) distinct.Add(value);
            }
            return distinct.ToArray();
        }

        static List<string> ObserveSimpleMesh(List<UIVertex> vertices, Vector2Int png, Vector2 rect)
        {
            var problems = new List<string>();
            if (vertices.Count != 4)
            {
                problems.Add($"simple piece must be one quad, found {vertices.Count} vertices");
                return problems;
            }
            float minX = vertices.Min(v => v.position.x);
            float maxX = vertices.Max(v => v.position.x);
            float minY = vertices.Min(v => v.position.y);
            float maxY = vertices.Max(v => v.position.y);
            float drawnRatio = (maxX - minX) / (maxY - minY);
            float pngRatio = (float)png.x / png.y;
            if (Mathf.Abs(drawnRatio - pngRatio) > pngRatio * GeometryTolerance)
            {
                problems.Add($"drawn aspect {drawnRatio:F4} differs from the PNG aspect {pngRatio:F4}");
            }
            if (minX < -GeometryTolerance || minY < -GeometryTolerance || maxX > rect.x + GeometryTolerance || maxY > rect.y + GeometryTolerance)
            {
                problems.Add($"drawn quad {minX:F2},{minY:F2}..{maxX:F2},{maxY:F2} leaves the rect {rect}");
            }
            return problems;
        }

        static List<string> ObserveSlicedRender(Rendered reference, Rendered enlarged, Piece piece)
        {
            var problems = new List<string>();
            int left = piece.Left / 2;
            int right = piece.Right / 2;
            int top = piece.Top / 2;
            int bottom = piece.Bottom / 2;
            int m = RenderMargin;

            // Corners, extended outward through the transparent margin: unscaled pixels must be identical.
            CompareBlock(problems, "bottom-left corner", reference, enlarged, 0, 0, 0, 0, m + left, m + bottom);
            CompareBlock(problems, "bottom-right corner", reference, enlarged, reference.Width - m - right, 0,
                enlarged.Width - m - right, 0, m + right, m + bottom);
            CompareBlock(problems, "top-left corner", reference, enlarged, 0, reference.Height - m - top, 0,
                enlarged.Height - m - top, m + left, m + top);
            CompareBlock(problems, "top-right corner", reference, enlarged, reference.Width - m - right,
                reference.Height - m - top, enlarged.Width - m - right, enlarged.Height - m - top, m + right, m + top);

            // An axis without border is not stretched, so the fixed ends (the divider's ends) span the whole height
            // and must stay pixel-identical over it, not only in the corner blocks that then hold just the margin.
            if (piece.Top == 0 && piece.Bottom == 0)
            {
                if (enlarged.Height != reference.Height)
                {
                    problems.Add($"height {enlarged.Height}px changed, whole PNG render is {reference.Height}px");
                }
                else
                {
                    CompareBlock(problems, "left end", reference, enlarged, 0, 0, 0, 0, m + left, reference.Height);
                    CompareBlock(problems, "right end", reference, enlarged, reference.Width - m - right, 0,
                        enlarged.Width - m - right, 0, m + right, reference.Height);
                }
            }

            // Edge bands: the profile across each band (averaged along it) must keep the same thickness.
            CompareColumnMeans(problems, "left band", reference, enlarged, 0, 0, m + left, m + bottom, m + top);
            CompareColumnMeans(problems, "right band", reference, enlarged, reference.Width - m - right,
                enlarged.Width - m - right, m + right, m + bottom, m + top);
            CompareRowMeans(problems, "bottom band", reference, enlarged, 0, 0, m + bottom, m + left, m + right);
            CompareRowMeans(problems, "top band", reference, enlarged, reference.Height - m - top,
                enlarged.Height - m - top, m + top, m + left, m + right);

            // Outline: transparent margins inside the rect keep their size and nothing is drawn outside the rect.
            RectInt referenceBounds = reference.VisibleBounds();
            RectInt enlargedBounds = enlarged.VisibleBounds();
            int[] referenceMargins = reference.MarginsInsideRect(referenceBounds);
            int[] enlargedMargins = enlarged.MarginsInsideRect(enlargedBounds);
            for (int side = 0; side < 4; side++)
            {
                if (enlargedMargins[side] < 0) problems.Add($"outline: drawn outside the rect on side {side}");
                if (Math.Abs(enlargedMargins[side] - referenceMargins[side]) > 1)
                {
                    problems.Add($"outline: margin side {side} is {enlargedMargins[side]}px, whole PNG has {referenceMargins[side]}px");
                }
            }

            // Seams: no transparent line exactly at a slice line inside the stretched region.
            int seams = CountSeams(enlarged, piece);
            if (seams > 0) problems.Add($"{seams} seam pixels at slice lines");
            return problems;
        }

        static void CompareBlock(List<string> problems, string label, Rendered reference, Rendered enlarged,
            int referenceX, int referenceY, int enlargedX, int enlargedY, int width, int height)
        {
            int mismatched = 0;
            int worst = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int difference = ChannelDifference(reference.Pixel(referenceX + x, referenceY + y),
                        enlarged.Pixel(enlargedX + x, enlargedY + y));
                    worst = Math.Max(worst, difference);
                    if (difference > ChannelTolerance) mismatched++;
                }
            }
            if (mismatched > 0) problems.Add($"{label}: {mismatched} pixels differ from the whole PNG (max {worst})");
        }

        static void CompareColumnMeans(List<string> problems, string label, Rendered reference, Rendered enlarged,
            int referenceX, int enlargedX, int width, int lowSkip, int highSkip)
        {
            double worst = 0;
            for (int x = 0; x < width; x++)
            {
                double[] referenceMean = reference.MeanOfColumn(referenceX + x, lowSkip, reference.Height - highSkip);
                double[] enlargedMean = enlarged.MeanOfColumn(enlargedX + x, lowSkip, enlarged.Height - highSkip);
                worst = Math.Max(worst, MaxDifference(referenceMean, enlargedMean));
            }
            if (worst > BandMeanTolerance) problems.Add($"{label}: profile differs by {worst:F1} from the whole PNG");
        }

        static void CompareRowMeans(List<string> problems, string label, Rendered reference, Rendered enlarged,
            int referenceY, int enlargedY, int height, int lowSkip, int highSkip)
        {
            double worst = 0;
            for (int y = 0; y < height; y++)
            {
                double[] referenceMean = reference.MeanOfRow(referenceY + y, lowSkip, reference.Width - highSkip);
                double[] enlargedMean = enlarged.MeanOfRow(enlargedY + y, lowSkip, enlarged.Width - highSkip);
                worst = Math.Max(worst, MaxDifference(referenceMean, enlargedMean));
            }
            if (worst > BandMeanTolerance) problems.Add($"{label}: profile differs by {worst:F1} from the whole PNG");
        }

        static int CountSeams(Rendered render, Piece piece)
        {
            int m = RenderMargin;
            int seams = 0;
            var columns = new List<int>();
            if (piece.Left > 0) columns.AddRange(new[] { m + piece.Left / 2 - 1, m + piece.Left / 2 });
            if (piece.Right > 0) columns.AddRange(new[] { render.Width - m - piece.Right / 2 - 1, render.Width - m - piece.Right / 2 });
            foreach (int x in columns)
            {
                for (int y = m + piece.Bottom / 2; y < render.Height - m - piece.Top / 2; y++)
                {
                    int neighbours = Math.Min(render.Pixel(x - 1, y).a, render.Pixel(x + 1, y).a);
                    if (render.Pixel(x, y).a + SeamAlphaDrop < neighbours) seams++;
                }
            }
            var rows = new List<int>();
            if (piece.Bottom > 0) rows.AddRange(new[] { m + piece.Bottom / 2 - 1, m + piece.Bottom / 2 });
            if (piece.Top > 0) rows.AddRange(new[] { render.Height - m - piece.Top / 2 - 1, render.Height - m - piece.Top / 2 });
            foreach (int y in rows)
            {
                for (int x = m + piece.Left / 2; x < render.Width - m - piece.Right / 2; x++)
                {
                    int neighbours = Math.Min(render.Pixel(x, y - 1).a, render.Pixel(x, y + 1).a);
                    if (render.Pixel(x, y).a + SeamAlphaDrop < neighbours) seams++;
                }
            }
            return seams;
        }

        // Simple pieces drawn with preserveAspect keep the aspect of their visible shape; preserveAspect off does not.
        static List<string> ObserveSimpleRender(Rendered reference, Rendered stretched)
        {
            var problems = new List<string>();
            RectInt referenceBounds = reference.VisibleBounds();
            RectInt stretchedBounds = stretched.VisibleBounds();
            float referenceRatio = (float)referenceBounds.width / referenceBounds.height;
            float stretchedRatio = (float)stretchedBounds.width / stretchedBounds.height;
            // One pixel of anti-aliasing on either side is the allowed measurement error.
            float tolerance = referenceRatio * (2f / Math.Min(referenceBounds.width, referenceBounds.height));
            if (Mathf.Abs(stretchedRatio - referenceRatio) > tolerance)
            {
                problems.Add($"visible shape aspect {stretchedRatio:F3} ({stretchedBounds.width}x{stretchedBounds.height}) " +
                    $"differs from {referenceRatio:F3} ({referenceBounds.width}x{referenceBounds.height})");
            }
            int[] margins = stretched.MarginsInsideRect(stretchedBounds);
            if (margins.Any(margin => margin < 0)) problems.Add("visible shape is drawn outside the rect");
            return problems;
        }

        static void RequireGraphicsDevice()
        {
            TestContext.Out.WriteLine($"graphics device={SystemInfo.graphicsDeviceType} name={SystemInfo.graphicsDeviceName} " +
                $"colorSpace={PlayerSettings.colorSpace}");
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "render observation needs a real graphics device; a -nographics run cannot pass these tests");
        }

        static void AssertVisible(Rendered render, string name)
        {
            // An empty render would make every comparison trivially equal; refuse it before comparing.
            Assert.Greater(render.VisibleRatio(), 0.02, $"{name}: the whole-PNG reference render must show the piece");
        }

        static int MaxChannelDifference(Rendered a, Rendered b)
        {
            Assert.AreEqual(a.Width, b.Width, "fixture: compared renders must have the same width");
            Assert.AreEqual(a.Height, b.Height, "fixture: compared renders must have the same height");
            int worst = 0;
            for (int i = 0; i < a.Pixels.Length; i++) worst = Math.Max(worst, ChannelDifference(a.Pixels[i], b.Pixels[i]));
            return worst;
        }

        static int ChannelDifference(Color32 a, Color32 b) =>
            Math.Max(Math.Max(Math.Abs(a.r - b.r), Math.Abs(a.g - b.g)), Math.Max(Math.Abs(a.b - b.b), Math.Abs(a.a - b.a)));

        static double MaxDifference(double[] a, double[] b)
        {
            double worst = 0;
            for (int i = 0; i < a.Length; i++) worst = Math.Max(worst, Math.Abs(a[i] - b[i]));
            return worst;
        }

        static string EvidenceDirectory()
        {
            string directory = Environment.GetEnvironmentVariable(EvidenceDirectoryVariable);
            if (string.IsNullOrEmpty(directory)) return null;
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase),
                $"fixture: {EvidenceDirectoryVariable} must be outside the Unity project ({full})");
            Directory.CreateDirectory(full);
            return full;
        }

        static void WriteEvidenceBytes(string fileName, byte[] bytes)
        {
            string directory = EvidenceDirectory();
            if (directory == null) return;
            File.WriteAllBytes(Path.Combine(directory, fileName), bytes);
        }

        static void SaveEvidence(string name, string label, Rendered render)
        {
            WriteEvidenceBytes($"{name}__{label}.png", render.Png);
        }

        // Nearest-neighbour 8x crops of the top-left and bottom-right corner regions (including the outer margin).
        static void SaveCornerZooms(string name, string label, Rendered render, Piece piece)
        {
            if (EvidenceDirectory() == null) return;
            int m = RenderMargin;
            int width = m + Math.Max(piece.Left / 2, 4) + 4;
            int height = m + Math.Max(piece.Top / 2, 3) + 3;
            var topLeft = new RectInt(0, render.Height - height, width, height);
            var bottomRight = new RectInt(render.Width - width, 0, width, Math.Min(render.Height, m + Math.Max(piece.Bottom / 2, 3) + 3));
            WriteEvidenceBytes($"{name}__{label}__zoom{ZoomFactor}-top-left.png", render.ZoomPng(topLeft, ZoomFactor));
            WriteEvidenceBytes($"{name}__{label}__zoom{ZoomFactor}-bottom-right.png", render.ZoomPng(bottomRight, ZoomFactor));
        }

        enum PieceUse
        {
            Sliced,
            Divider,
            Simple,
        }

        sealed class Piece
        {
            internal Piece(string name, PieceUse use, int left, int right, int top, int bottom)
            {
                Name = name;
                Use = use;
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }

            internal string Name { get; }

            internal PieceUse Use { get; }

            internal int Left { get; }

            internal int Right { get; }

            internal int Top { get; }

            internal int Bottom { get; }

            internal string AssetPath => $"{ComponentsFolder}/{Name}.png";

            // Unity stores a sprite border as (L, B, R, T) in source pixels.
            internal Vector4 UnityBorder => new Vector4(Left, Bottom, Right, Top);

            public override string ToString() => Name;
        }

        // One render target worth of pixels; (0,0) is the bottom-left pixel and the rect starts at RenderMargin.
        sealed class Rendered
        {
            internal Rendered(int width, int height, Vector2 rectSize, Color32[] pixels, byte[] png)
            {
                Width = width;
                Height = height;
                RectSize = rectSize;
                Pixels = pixels;
                Png = png;
            }

            internal int Width { get; }

            internal int Height { get; }

            internal Vector2 RectSize { get; }

            internal Color32[] Pixels { get; }

            internal byte[] Png { get; }

            internal Color32 Pixel(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height) return new Color32(0, 0, 0, 0);
                return Pixels[y * Width + x];
            }

            internal double VisibleRatio()
            {
                int visible = Pixels.Count(pixel => pixel.a >= VisibleAlpha);
                return (double)visible / (RectSize.x * RectSize.y);
            }

            internal RectInt VisibleBounds()
            {
                int minX = Width;
                int minY = Height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (Pixels[y * Width + x].a < VisibleAlpha) continue;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
                return maxX < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }

            // Left, bottom, right, top distance from the rect edge to the visible bounds; negative means outside.
            internal int[] MarginsInsideRect(RectInt bounds)
            {
                int rectRight = RenderMargin + Mathf.CeilToInt(RectSize.x);
                int rectTop = RenderMargin + Mathf.CeilToInt(RectSize.y);
                return new[]
                {
                    bounds.xMin - RenderMargin,
                    bounds.yMin - RenderMargin,
                    rectRight - bounds.xMax,
                    rectTop - bounds.yMax,
                };
            }

            internal double[] MeanOfColumn(int x, int yFrom, int yTo) => Mean(Enumerable.Range(yFrom, Math.Max(0, yTo - yFrom))
                .Select(y => Pixel(x, y)));

            internal double[] MeanOfRow(int y, int xFrom, int xTo) => Mean(Enumerable.Range(xFrom, Math.Max(0, xTo - xFrom))
                .Select(x => Pixel(x, y)));

            internal byte[] ZoomPng(RectInt region, int factor)
            {
                var texture = new Texture2D(region.width * factor, region.height * factor, TextureFormat.RGBA32, false, false);
                try
                {
                    var pixels = new Color32[texture.width * texture.height];
                    for (int y = 0; y < texture.height; y++)
                    {
                        for (int x = 0; x < texture.width; x++)
                        {
                            pixels[y * texture.width + x] = Pixel(region.x + x / factor, region.y + y / factor);
                        }
                    }
                    texture.SetPixels32(pixels);
                    texture.Apply(false);
                    return texture.EncodeToPNG();
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            static double[] Mean(IEnumerable<Color32> pixels)
            {
                var sum = new double[4];
                int count = 0;
                foreach (Color32 pixel in pixels)
                {
                    sum[0] += pixel.r;
                    sum[1] += pixel.g;
                    sum[2] += pixel.b;
                    sum[3] += pixel.a;
                    count++;
                }
                return count == 0 ? sum : sum.Select(value => value / count).ToArray();
            }
        }

        // Owns one temporary Canvas (referencePixelsPerUnit 100, scale 1) with one Image, plus every mesh, material,
        // render target and readback texture it creates. Nothing is saved; Dispose destroys the Canvas.
        sealed class UiImageFixture : IDisposable
        {
            readonly GameObject _canvasObject;
            readonly Image _image;

            internal UiImageFixture()
            {
                _canvasObject = new GameObject("UiKitImportTestsCanvas", typeof(RectTransform), typeof(Canvas));
                _canvasObject.hideFlags = HideFlags.DontSave;
                try
                {
                    var canvas = _canvasObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var imageObject = new GameObject("UiKitImportTestsImage", typeof(RectTransform), typeof(Image));
                    imageObject.hideFlags = HideFlags.DontSave;
                    imageObject.transform.SetParent(_canvasObject.transform, false);
                    _image = imageObject.GetComponent<Image>();
                    Assert.AreEqual(CanvasReferencePixelsPerUnit, canvas.referencePixelsPerUnit, "fixture: Canvas reference PPU must be 100");
                    Assert.AreEqual(1f, canvas.scaleFactor, "fixture: Canvas scale must be 1");
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(_canvasObject);

            internal List<UIVertex> MeshOf(Sprite sprite, Image.Type type, Vector2 size, bool preserveAspect)
            {
                using VertexHelper helper = Populate(sprite, type, size, preserveAspect);
                var vertices = new List<UIVertex>(helper.currentVertCount);
                var vertex = new UIVertex();
                for (int i = 0; i < helper.currentVertCount; i++)
                {
                    helper.PopulateUIVertex(ref vertex, i);
                    vertices.Add(vertex);
                }
                return vertices;
            }

            // Draws the Image's own mesh with the UI shader and the imported texture into a transparent render
            // target, one UI unit per pixel, then reads it back. The previous active target is restored.
            internal Rendered Render(Sprite sprite, Image.Type type, Vector2 size, bool preserveAspect)
            {
                int width = Mathf.CeilToInt(size.x) + 2 * RenderMargin;
                int height = Mathf.CeilToInt(size.y) + 2 * RenderMargin;
                RenderTexture previous = RenderTexture.active;
                var mesh = new Mesh();
                Shader shader = Shader.Find("UI/Default");
                Assert.IsNotNull(shader, "fixture: the built-in UI/Default shader must exist");
                var material = new Material(shader);
                var target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var readback = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                try
                {
                    using (VertexHelper helper = Populate(sprite, type, size, preserveAspect))
                    {
                        helper.FillMesh(mesh);
                    }
                    material.mainTexture = _image.mainTexture;
                    target.Create();
                    RenderTexture.active = target;
                    GL.Clear(true, true, Color.clear);
                    GL.PushMatrix();
                    GL.LoadPixelMatrix(0, width, 0, height);
                    Assert.IsTrue(material.SetPass(0), "fixture: UI/Default pass 0 must be drawable");
                    Graphics.DrawMeshNow(mesh, Matrix4x4.Translate(new Vector3(RenderMargin, RenderMargin, 0f)));
                    GL.PopMatrix();
                    readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    readback.Apply(false);
                    return new Rendered(width, height, size, readback.GetPixels32(), readback.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                    UnityEngine.Object.DestroyImmediate(readback);
                    UnityEngine.Object.DestroyImmediate(material);
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            // The real uGUI mesh generation: Image.OnPopulateMesh for the current rect, sprite, type and canvas PPU.
            VertexHelper Populate(Sprite sprite, Image.Type type, Vector2 size, bool preserveAspect)
            {
                Assert.IsNotNull(s_populateMesh, "fixture: Graphic.OnPopulateMesh(VertexHelper) must exist in this uGUI");
                RectTransform rect = _image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = size;
                _image.sprite = sprite;
                _image.type = type;
                _image.fillCenter = true;
                _image.preserveAspect = preserveAspect;
                _image.pixelsPerUnitMultiplier = 1f;
                Assert.AreEqual(SpritePixelsPerUnit / CanvasReferencePixelsPerUnit, _image.pixelsPerUnit, GeometryTolerance,
                    "fixture: Image must see sprite PPU 200 over Canvas reference 100 (density 2)");
                var helper = new VertexHelper();
                s_populateMesh.Invoke(_image, new object[] { helper });
                return helper;
            }
        }
    }
}
