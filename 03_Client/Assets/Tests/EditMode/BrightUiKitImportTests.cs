using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // The bright kit import for PR2, written before the import (goal 2026-10-29-inventory-character-select-look
    // 완료조건 1·6; contract pr2-red-contract-v4 T1 and the default-skin half of T2). Observed through the real folder,
    // .meta files, TextureImporter and loaded Sprite, the same way UiKitImportTests checks the brown B1 kit. Expected
    // names, GUIDs, importer settings, pixel sizes and borders are the approved spec rows in BrightUiKitTestSupport.
    // A check over the imported pieces first asserts that all 25 exist, so a missing folder cannot pass with no target.
    public sealed class BrightUiKitImportTests
    {
        const string BrownFolder = "Assets/Art/UI/Components";
        const string DefaultSkinFolder = "Assets/Resources/UI/Components";
        const int BrownPieceCount = 33;

        // kit-spec sprites[]: 25 names, usage slicedCount 18 and simpleCount 7, at the top of the bright folder.
        [Test]
        public void BrightFolder_HoldsExactlyTheApprovedPngs()
        {
            bool folderExists = Directory.Exists(ArtFolder);
            string[] actual = folderExists
                ? Directory.GetFiles(ArtFolder, "*.png", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension).OrderBy(name => name, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            string[] expected = Pieces.Select(piece => piece.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            TestContext.Out.WriteLine($"folder={ArtFolder} exists={folderExists} pngs={actual.Length} [{string.Join(",", actual)}]");

            Assert.AreEqual(25, Pieces.Length, "spec table: 25 bright pieces");
            Assert.AreEqual(18, Pieces.Count(piece => piece.Sliced), "spec usage: 18 sliced pieces");
            Assert.AreEqual(7, Pieces.Count(piece => !piece.Sliced), "spec usage: 7 simple pieces");
            Assert.IsTrue(folderExists, $"{ArtFolder} must exist");
            CollectionAssert.AreEqual(expected, actual, "the bright folder must hold exactly the approved PNG names");
        }

        // bright-kit-guids-v1.json: each PNG keeps the GUID assigned before the import.
        [TestCaseSource(nameof(AllPieceNames))]
        public void BrightPng_MetaCarriesThePreassignedGuid(string name)
        {
            BrightPiece piece = Named(name);
            string metaPath = piece.AssetPath + ".meta";
            bool pngExists = File.Exists(piece.AssetPath);
            bool metaExists = File.Exists(metaPath);
            string metaGuid = metaExists
                ? File.ReadAllLines(metaPath).Where(line => line.StartsWith("guid: ", StringComparison.Ordinal))
                    .Select(line => line.Substring("guid: ".Length).Trim()).SingleOrDefault()
                : null;
            string databaseGuid = AssetDatabase.AssetPathToGUID(piece.AssetPath);
            TestContext.Out.WriteLine($"guid {name}: png={pngExists} meta={metaExists} metaGuid={metaGuid} " +
                $"database={databaseGuid} expected={piece.Guid}");

            Assert.IsTrue(pngExists, $"{piece.AssetPath} must exist");
            Assert.IsTrue(metaExists, $"{metaPath} must exist");
            Assert.AreEqual(piece.Guid, metaGuid, "the .meta guid must be the pre-assigned GUID");
            Assert.AreEqual(piece.Guid, databaseGuid, "the AssetDatabase must know the PNG under the pre-assigned GUID");
        }

        // kit-spec importer block.
        [TestCaseSource(nameof(AllPieceNames))]
        public void BrightImporter_UsesTheApprovedSpriteSettings(string name)
        {
            BrightPiece piece = Named(name);
            var importer = AssetImporter.GetAtPath(piece.AssetPath) as TextureImporter;
            Assert.IsNotNull(importer, $"{piece.AssetPath} must be imported by a TextureImporter");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            TestContext.Out.WriteLine($"importer {name}: type={importer.textureType} mode={importer.spriteImportMode} " +
                $"mesh={settings.spriteMeshType} pivot={importer.spritePivot} ppu={importer.spritePixelsPerUnit} " +
                $"border={importer.spriteBorder} mip={importer.mipmapEnabled} srgb={importer.sRGBTexture} " +
                $"alpha={importer.alphaIsTransparency} filter={importer.filterMode} " +
                $"wrap={importer.wrapModeU}/{importer.wrapModeV}/{importer.wrapModeW} npot={importer.npotScale} " +
                $"max={importer.maxTextureSize} compression={importer.textureCompression} readable={importer.isReadable}");

            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, "texture type must be Sprite (2D and UI)");
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, "sprite mode must be Single");
            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType, "sprite mesh must be Full Rect");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), importer.spritePivot, "pivot must be the centre");
            Assert.AreEqual(PixelsPerUnit, importer.spritePixelsPerUnit, "pixels per unit must be 200");
            Assert.IsFalse(importer.mipmapEnabled, "mipmaps must be off");
            Assert.IsTrue(importer.sRGBTexture, "colour texture must be sRGB");
            Assert.IsTrue(importer.alphaIsTransparency, "alpha must be treated as transparency");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, "filter must be Bilinear");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeU, "wrap U must be Clamp");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeV, "wrap V must be Clamp");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeW, "wrap W must be Clamp");
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "non-power-of-two size must be kept");
            Assert.AreEqual(2048, importer.maxTextureSize, "max size must be 2048");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "texture must be uncompressed");
            Assert.IsFalse(importer.isReadable, "CPU Read/Write must be off");
            Assert.AreEqual(piece.UnityBorder, importer.spriteBorder,
                "importer border must be the spec PNG-pixel border as Unity (L,B,R,T), not divided by density");
            Assert.IsFalse(importer.GetPlatformTextureSettings("Standalone").overridden,
                "spec platformOverrides is empty: Standalone must not override");
        }

        // kit-spec sprites[].pixelSize and borderLRTB on the loaded Sprite.
        [TestCaseSource(nameof(AllPieceNames))]
        public void BrightLoadedSprite_KeepsTheSpecPixelSizeAndBorder(string name)
        {
            BrightPiece piece = Named(name);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(piece.AssetPath).OfType<Sprite>().ToArray();
            TestContext.Out.WriteLine($"sprite {name}: count={sprites.Length} " +
                (sprites.Length == 1 ? $"rect={sprites[0].rect} ppu={sprites[0].pixelsPerUnit} border={sprites[0].border}" : string.Empty));

            Assert.AreEqual(1, sprites.Length, $"{name}: Single mode must yield exactly one Sprite");
            Sprite sprite = sprites[0];
            Assert.AreEqual(piece.Width, sprite.texture.width, "imported texture width must be the spec PNG width");
            Assert.AreEqual(piece.Height, sprite.texture.height, "imported texture height must be the spec PNG height");
            Assert.AreEqual(new Rect(0, 0, piece.Width, piece.Height), sprite.rect, "the sprite must cover the whole PNG");
            Assert.AreEqual(PixelsPerUnit, sprite.pixelsPerUnit, "loaded sprite must use 200 pixels per unit");
            Assert.AreEqual(new Vector2(piece.Width / 2f, piece.Height / 2f), sprite.pivot, "loaded sprite pivot must be the centre");
            Assert.AreEqual(piece.UnityBorder, sprite.border, "loaded sprite border must be the spec (L,B,R,T) PNG pixels");
        }

        // A forced re-import without setting changes keeps every GUID, .meta and source byte (UiKitImportTests B3 shape).
        [Test]
        public void BrightReimport_WithoutSettingChanges_KeepsGuidMetaAndSourceBytes()
        {
            string[] existing = Pieces.Select(piece => piece.AssetPath).Where(File.Exists).ToArray();
            Assert.AreEqual(Pieces.Length, existing.Length, "precondition: all 25 bright PNGs exist before the re-import check");

            var changed = new List<string>();
            foreach (string path in existing)
            {
                string guidBefore = AssetDatabase.AssetPathToGUID(path);
                byte[] metaBefore = File.ReadAllBytes(path + ".meta");
                byte[] sourceBefore = File.ReadAllBytes(path);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                string guidAfter = AssetDatabase.AssetPathToGUID(path);
                bool sameGuid = guidBefore == guidAfter && guidBefore.Length == 32;
                bool sameMeta = metaBefore.SequenceEqual(File.ReadAllBytes(path + ".meta"));
                bool sameSource = sourceBefore.SequenceEqual(File.ReadAllBytes(path));
                TestContext.Out.WriteLine($"reimport {path}: guid={guidBefore}->{guidAfter} meta={sameMeta} source={sameSource}");
                if (!sameGuid || !sameMeta || !sameSource) changed.Add(path);
            }

            Assert.That(changed, Is.Empty, "re-import without setting changes must keep GUID, .meta and source bytes");
        }

        // The pre-assigned GUIDs are well formed, distinct, not one of the brown kit's 33 and held by no other asset.
        [Test]
        public void BrightGuids_AreDistinctAndShareNothingWithTheBrownKit()
        {
            string[] brownPngs = Directory.GetFiles(BrownFolder, "*.png", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/')).ToArray();
            string[] brownGuids = brownPngs.Select(path => AssetDatabase.AssetPathToGUID(path)).Where(guid => guid.Length == 32).ToArray();
            string[] brightGuids = Pieces.Select(piece => piece.Guid).ToArray();
            string[] malformed = brightGuids.Where(guid => !Regex.IsMatch(guid, "^[0-9a-f]{32}$")).ToArray();
            string[] shared = brightGuids.Intersect(brownGuids).ToArray();
            string[] heldElsewhere = Pieces
                .Select(piece => (piece, path: AssetDatabase.GUIDToAssetPath(piece.Guid)))
                .Where(entry => entry.path.Length > 0 && entry.path != entry.piece.AssetPath)
                .Select(entry => $"{entry.piece.Name} -> {entry.path}").ToArray();
            TestContext.Out.WriteLine($"brown pngs={brownPngs.Length} guids={brownGuids.Length}; bright={brightGuids.Length} " +
                $"distinct={brightGuids.Distinct().Count()} shared=[{string.Join(",", shared)}] elsewhere=[{string.Join(",", heldElsewhere)}]");

            Assert.AreEqual(BrownPieceCount, brownGuids.Length, "precondition: the 33 brown PNGs and their GUIDs are found");
            Assert.AreEqual(25, brightGuids.Distinct().Count(), "the 25 bright GUIDs are distinct");
            Assert.IsEmpty(malformed, "every bright GUID is 32 lowercase hex digits");
            Assert.IsEmpty(shared, "no bright GUID equals a brown kit GUID");
            Assert.IsEmpty(heldElsewhere, "no other asset holds a bright GUID");
        }

        // Same shape as UiWindowAppearanceContractTests.ResourcesFolders_HoldNoCopyOfAnApprovedPng (brown kit).
        [Test]
        public void ResourcesFolders_HoldNoCopyOfABrightPng()
        {
            string[] existing = Pieces.Select(piece => piece.AssetPath).Where(File.Exists).ToArray();
            Assert.AreEqual(Pieces.Length, existing.Length, "precondition: the 25 bright PNGs are found, so the copy search has targets");

            Dictionary<string, string> brightByHash = Pieces.GroupBy(piece => Sha256(piece.AssetPath))
                .ToDictionary(group => group.Key, group => string.Join("/", group.Select(piece => piece.Name)));
            string[] copies = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories)
                .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                .Where(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Where(file => brightByHash.ContainsKey(Sha256(file)))
                .Select(file => file.Replace('\\', '/') + " = " + brightByHash[Sha256(file)]).ToArray();
            TestContext.Out.WriteLine($"bright PNG copies under Resources: [{string.Join(", ", copies)}]");

            Assert.IsEmpty(copies, "no Resources folder holds a byte copy of a bright PNG");
        }

        // T2 (goal 완료조건 1 「나머지 7개 창은 기존 브라운 조각을 그대로 쓴다」): the default skin and its Window*.prefab
        // reference brown kit PNGs only. No existing test pins this; UiWindowLiveCaptureTests only loads the skin.
        [Test]
        public void DefaultSkin_AndItsWindowPrefabs_ReferenceOnlyBrownKitPngs()
        {
            string[] assets = new[] { DefaultSkinFolder + "/UiWindowSkin.asset" }
                .Concat(Directory.GetFiles(DefaultSkinFolder, "Window*.prefab", SearchOption.TopDirectoryOnly)
                    .Select(path => path.Replace('\\', '/')))
                .ToArray();
            string[] pngs = assets.SelectMany(asset => AssetDatabase.GetDependencies(asset, false))
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
            string[] brown = pngs.Where(path => path.StartsWith(BrownFolder + "/", StringComparison.Ordinal)).ToArray();
            string[] notBrown = pngs.Except(brown).ToArray();
            string[] bright = pngs.Where(path => Pieces.Any(piece => AssetDatabase.AssetPathToGUID(path) == piece.Guid)).ToArray();
            TestContext.Out.WriteLine($"default skin assets=[{string.Join(", ", assets)}] pngs={pngs.Length} " +
                $"brown={brown.Length} notBrown=[{string.Join(", ", notBrown)}]");

            Assert.AreEqual(6, assets.Length, "precondition: UiWindowSkin.asset and its five Window*.prefab are found");
            Assert.IsNotEmpty(brown, "precondition: the default skin references brown kit PNGs");
            Assert.IsEmpty(bright, "the default skin references no bright piece");
            Assert.IsEmpty(notBrown, "the default skin references brown kit PNGs only");
        }

        static IEnumerable<string> AllPieceNames() => Pieces.Select(piece => piece.Name);

        static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
