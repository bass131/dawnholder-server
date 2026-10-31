using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // The bright kit import for PR2, written before the import (goal 2026-10-29-inventory-character-select-look
    // 완료조건 1·6; contract pr2-red-contract-v4 T1 and the default-skin half of T2). Observed through the real folder,
    // .meta files, TextureImporter and loaded Sprite, the same way UiKitImportTests checks the brown B1 kit. Expected
    // names, GUIDs, importer settings, pixel sizes and borders are the approved spec rows in BrightUiKitTestSupport.
    // A check over the imported pieces first asserts that all 32 exist, so a missing folder cannot pass with no target.
    // PR3 (decision v3 A msg_79d2bf86419e, 완료조건 6·10) adds the seven kit v2 pieces the default skin shares with the
    // bright skin, the default-skin swap (T2) and the bright skin's former empty slots (T3).
    public sealed class BrightUiKitImportTests
    {
        const string BrownFolder = "Assets/Art/UI/Components";
        const string DefaultSkinFolder = "Assets/Resources/UI/Components";
        const int BrownPieceCount = 33;
        const string DefaultSkinPath = DefaultSkinFolder + "/UiWindowSkin.asset";
        const string BrightSkinPath = ResourcesFolder + "/UiWindowSkin.asset";
        const string BrightPortraitPrefabPath = ResourcesFolder + "/WindowPortrait.prefab";
        // pr3-bright-guids-v1.json (sha256 71f1c5cb…): the GUID the lead assigned to the new bright WindowPortrait.prefab.
        const string BrightPortraitPrefabGuid = "c1dccd76328bd322bf35ae63a2d582e2";
        const string BrownPortraitPrefabPath = DefaultSkinFolder + "/WindowPortrait.prefab";

        // The seven kit v2 pieces of PR3 (pr3-bright-guids-v1.json, the last 7 rows of BrightUiKitTestSupport.Pieces).
        static readonly string[] SharedSkinPieceNames =
        {
            "Panel_Dialog", "Panel_Emphasis", "Button_Decline_Normal", "Button_Decline_Highlighted",
            "Button_Decline_Pressed", "Button_Decline_Disabled", "PortraitFrame_128",
        };

        // The twelve UiWindowSkin fields the default skin shares with the bright skin after the swap (contract 「두 스킨
        // 자산」; pr3-asset-facts-41568d73-01 section 4). The two SpriteState blocks are compared field by field.
        static readonly string[] SharedSkinFields =
        {
            "_panelPrefab", "_headerPrefab", "_buttonPrefab", "_insetPrefab", "_portraitPrefab",
            "_dialogPanel", "_emphasisPanel", "_divider", "_acceptNormal", "_acceptStates", "_declineNormal", "_declineStates",
        };

        static readonly string[] SpriteStateFields = { "m_HighlightedSprite", "m_PressedSprite", "m_SelectedSprite", "m_DisabledSprite" };

        // The eight bright skin slots that were empty before PR3 and the piece each takes (contract 「두 스킨 자산」;
        // pr3-contract-facts-counts-41568d73-01 empty=8). _portraitPrefab is checked through its prefab below.
        static readonly (string Field, string Piece)[] FormerlyEmptySpriteSlots =
        {
            ("_dialogPanel", "Panel_Dialog"),
            ("_emphasisPanel", "Panel_Emphasis"),
            ("_declineNormal", "Button_Decline_Normal"),
            ("_declineStates.m_HighlightedSprite", "Button_Decline_Highlighted"),
            ("_declineStates.m_PressedSprite", "Button_Decline_Pressed"),
            ("_declineStates.m_SelectedSprite", "Button_Decline_Highlighted"),
            ("_declineStates.m_DisabledSprite", "Button_Decline_Disabled"),
        };

        // The default skin and the five brown Window*.prefab keep their paths and GUIDs (pr3-asset-facts-41568d73-01
        // section 1). The prefabs stay in the folder unreferenced after the swap (main 1 A: they are not deleted).
        static readonly (string Path, string Guid)[] KeptDefaultSkinAssets =
        {
            (DefaultSkinPath, "a7b7508336f3f3623912e0c8405497c8"),
            (DefaultSkinFolder + "/WindowButton.prefab", "8ee9b685ca8f1a14b978b0e2fc08c24e"),
            (DefaultSkinFolder + "/WindowHeader.prefab", "5470df5701dcd91ba1f8ebadfec0edb1"),
            (DefaultSkinFolder + "/WindowInset.prefab", "74e96bd6480e224f43101ef5f56029cb"),
            (DefaultSkinFolder + "/WindowPanel.prefab", "8052a591cd1fda1e2ae5b53ff42eda78"),
            (DefaultSkinFolder + "/WindowPortrait.prefab", "86fad0ca8a358a7dd09f21d45022d057"),
        };

        // pr3-contract-facts-counts-41568d73-01 bright_top_level=26: every serialized UiWindowSkin field.
        const int SkinFieldCount = 26;

        // The two HP bar pieces PR4 puts in the bright folder beside the approved table (main 「숨은 HUD A」
        // msg_8a141e965681, folder test A msg_5b7564e860b7): bright-kit-v2/kit-spec.json rows Bar_Track :258 and
        // Bar_Fill :288, imported from this folder by BrightRemainingScreensTests.cs :259. They are not Pieces rows.
        static readonly string[] BarPieceNames = { "Bar_Fill", "Bar_Track" };

        // kit-spec sprites[] (PR2) plus the seven kit v2 rows: 32 names, 24 sliced and 8 simple (emit-pr3-tables-01
        // section G), at the top of the bright folder. PR4 adds the two BarPieceNames there, so the folder holds 34.
        [Test]
        public void BrightFolder_HoldsExactlyTheApprovedPngs()
        {
            bool folderExists = Directory.Exists(ArtFolder);
            string[] actual = folderExists
                ? Directory.GetFiles(ArtFolder, "*.png", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension).OrderBy(name => name, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            string[] expected = Pieces.Select(piece => piece.Name).Concat(BarPieceNames)
                .OrderBy(name => name, StringComparer.Ordinal).ToArray();
            TestContext.Out.WriteLine($"folder={ArtFolder} exists={folderExists} pngs={actual.Length} [{string.Join(",", actual)}]");

            Assert.AreEqual(32, Pieces.Length, "spec table: 32 bright pieces");
            Assert.AreEqual(24, Pieces.Count(piece => piece.Sliced), "spec usage: 24 sliced pieces");
            Assert.AreEqual(8, Pieces.Count(piece => !piece.Sliced), "spec usage: 8 simple pieces");
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
            Assert.AreEqual(Pieces.Length, existing.Length, "precondition: all 32 bright PNGs exist before the re-import check");

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
            Assert.AreEqual(32, brightGuids.Distinct().Count(), "the 32 bright GUIDs are distinct");
            Assert.IsEmpty(malformed, "every bright GUID is 32 lowercase hex digits");
            Assert.IsEmpty(shared, "no bright GUID equals a brown kit GUID");
            Assert.IsEmpty(heldElsewhere, "no other asset holds a bright GUID");
        }

        // Same shape as UiWindowAppearanceContractTests.ResourcesFolders_HoldNoCopyOfAnApprovedPng (brown kit).
        [Test]
        public void ResourcesFolders_HoldNoCopyOfABrightPng()
        {
            string[] existing = Pieces.Select(piece => piece.AssetPath).Where(File.Exists).ToArray();
            Assert.AreEqual(Pieces.Length, existing.Length, "precondition: the 32 bright PNGs are found, so the copy search has targets");

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

        // PR3 T2 (decision v3 A msg_79d2bf86419e, goal 완료조건 10 and ③; replaces the PR2 brown pin): the default skin
        // that every shared window and the guild windows load points at the bright kit only, through the same objects as
        // the bright skin. The skin asset alone is the root: after the swap the five brown Window*.prefab stay in the
        // folder unreferenced (main 1 A), so enumerating them would bring brown back.
        [Test]
        public void DefaultSkin_ReferencesOnlyBrightKitPngs_AndSharesTheBrightSkinPieces()
        {
            string[] pngs = AssetDatabase.GetDependencies(DefaultSkinPath, true)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
            string[] notBright = pngs.Where(path => !Pieces.Any(piece => piece.AssetPath == path && AssetDatabase.AssetPathToGUID(path) == piece.Guid))
                .ToArray();
            string[] brown = pngs.Where(path => path.StartsWith(BrownFolder + "/", StringComparison.Ordinal)).ToArray();
            string[] missingShared = SharedSkinPieceNames.Where(name => !pngs.Contains(Named(name).AssetPath)).ToArray();
            string[] movedGuids = KeptDefaultSkinAssets.Where(asset => AssetDatabase.AssetPathToGUID(asset.Path) != asset.Guid)
                .Select(asset => $"{asset.Path} -> '{AssetDatabase.AssetPathToGUID(asset.Path)}', kept {asset.Guid}").ToArray();
            TestContext.Out.WriteLine($"default skin pngs={pngs.Length} [{string.Join(", ", pngs)}] brown={brown.Length} " +
                $"notBright=[{string.Join(", ", notBright)}] missingShared=[{string.Join(",", missingShared)}]");

            Assert.IsEmpty(movedGuids, "the default skin and the five brown Window*.prefab keep their paths and GUIDs");
            Assert.IsNotEmpty(pngs, "precondition: the default skin reaches kit PNGs");
            Assert.IsEmpty(notBright, "the default skin reaches bright kit PNGs only (no union with the brown kit)");
            Assert.IsEmpty(brown, "the default skin reaches no brown kit PNG");
            Assert.IsEmpty(missingShared, "the default skin reaches the seven PR3 pieces, the portrait frame through its prefab");
            List<string> notShared = FieldsNotShared(DefaultSkinPath, BrightSkinPath);
            Assert.IsEmpty(notShared, "the twelve shared fields are set and hold the bright skin's own objects");
        }

        // PR3 T1b (Content msg_e187a4454281 2, 「B1과 같은 계약」): each of the seven new sprites keeps the pixel rect and
        // border of the brown B1 sprite of the same name, so a window keeps its geometry when the skin is swapped.
        [Test]
        public void SharedSkinPieces_KeepTheRectAndBorderOfTheSameNamedBrownSprite()
        {
            (string Name, Sprite Bright, Sprite Brown)[] pairs = SharedSkinPieceNames
                .Select(name => (name, AssetDatabase.LoadAssetAtPath<Sprite>(Named(name).AssetPath),
                    AssetDatabase.LoadAssetAtPath<Sprite>(BrownFolder + "/" + name + ".png")))
                .ToArray();
            foreach ((string name, Sprite bright, Sprite brown) in pairs)
                TestContext.Out.WriteLine($"pair {name}: bright={(bright != null ? $"{bright.rect} {bright.border}" : "missing")} " +
                    $"brown={(brown != null ? $"{brown.rect} {brown.border}" : "missing")}");

            Assert.AreEqual(SharedSkinPieceNames.Length, pairs.Count(pair => pair.Brown != null),
                "precondition: the seven same-named brown sprites load");
            Assert.AreEqual(SharedSkinPieceNames.Length, pairs.Count(pair => pair.Bright != null),
                "precondition: the seven bright sprites load");
            string[] different = pairs.Where(pair => pair.Bright.rect.size != pair.Brown.rect.size || pair.Bright.border != pair.Brown.border)
                .Select(pair => pair.Name).ToArray();
            Assert.IsEmpty(different, "each bright sprite keeps the brown sprite's pixel size and (L,B,R,T) border");
        }

        // PR3 T3 (v3 A msg_79d2bf86419e, 완료조건 10): the bright skin fills all 26 fields; the eight slots that were empty
        // take the new pieces by name and GUID (BrightUiKitTestSupport rows from pr3-bright-guids-v1.json).
        [Test]
        public void BrightSkin_FillsEveryField_AndItsFormerlyEmptySlotsShowTheNewBrightPieces()
        {
            var skin = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(BrightSkinPath));
            List<string> fields = TopLevelFields(skin);
            string[] empty = fields.SelectMany(field => EmptyReferences(skin.FindProperty(field))).ToArray();
            string[] wrongSlots = FormerlyEmptySpriteSlots
                .Select(slot => (slot, sprite: skin.FindProperty(slot.Field)?.objectReferenceValue as Sprite))
                .Where(entry => entry.sprite == null || SourceGuid(entry.sprite) != Named(entry.slot.Piece).Guid ||
                    entry.sprite.name != entry.slot.Piece)
                .Select(entry => $"{entry.slot.Field} shows {(entry.sprite != null ? $"'{entry.sprite.name}' {SourceGuid(entry.sprite)}" : "nothing")}, " +
                    $"approved {entry.slot.Piece} {Named(entry.slot.Piece).Guid}")
                .ToArray();
            UnityEngine.Object portrait = skin.FindProperty("_portraitPrefab")?.objectReferenceValue;
            string portraitPath = portrait != null ? AssetDatabase.GetAssetPath(portrait) : null;
            TestContext.Out.WriteLine($"bright skin fields={fields.Count} empty=[{string.Join(", ", empty)}] " +
                $"wrongSlots=[{string.Join("; ", wrongSlots)}] portrait={portraitPath ?? "none"}");

            Assert.AreEqual(SkinFieldCount, fields.Count, "precondition: the bright skin serializes its 26 fields");
            Assert.IsEmpty(empty, "every bright skin field and SpriteState sub-field is set");
            Assert.IsEmpty(wrongSlots, "the formerly empty sprite slots show the new bright pieces");
            Assert.AreEqual(BrightPortraitPrefabPath, portraitPath, "_portraitPrefab is the new bright WindowPortrait.prefab");
        }

        // PR3 T3: the new bright WindowPortrait.prefab sits at its pre-assigned GUID, shows the bright PortraitFrame_128,
        // and keeps every Image setting of the brown WindowPortrait.prefab (both read here, not copied as literals).
        [Test]
        public void BrightPortraitPrefab_ShowsTheBrightFrame_AndKeepsTheBrownPrefabImageSettings()
        {
            var brown = AssetDatabase.LoadAssetAtPath<GameObject>(BrownPortraitPrefabPath)?.GetComponent<Image>();
            var bright = AssetDatabase.LoadAssetAtPath<GameObject>(BrightPortraitPrefabPath)?.GetComponent<Image>();
            string guid = AssetDatabase.AssetPathToGUID(BrightPortraitPrefabPath);
            TestContext.Out.WriteLine($"portrait prefab guid={guid} bright={(bright != null ? Settings(bright) : "missing")} " +
                $"brown={(brown != null ? Settings(brown) : "missing")}");

            Assert.IsNotNull(brown, "precondition: the brown WindowPortrait.prefab has an Image");
            Assert.AreEqual(BrightPortraitPrefabGuid, guid, "the bright WindowPortrait.prefab keeps its pre-assigned GUID");
            Assert.IsNotNull(bright, "the bright WindowPortrait.prefab has an Image");
            Assert.AreEqual(Named("PortraitFrame_128").Guid, SourceGuid(bright.sprite), "its Image shows the bright PortraitFrame_128");
            Assert.AreEqual(Settings(brown), Settings(bright), "its Image keeps the brown prefab's size, raycast, type, aspect and colour");
        }

        static string Settings(Image image) =>
            $"size={image.rectTransform.sizeDelta} raycast={image.raycastTarget} type={image.type} " +
            $"preserveAspect={image.preserveAspect} colour={image.color}";

        // The serialized fields of a UiWindowSkin asset below m_Script, in file order.
        static List<string> TopLevelFields(SerializedObject skin)
        {
            var fields = new List<string>();
            SerializedProperty property = skin.GetIterator();
            for (bool more = property.NextVisible(true); more; more = property.NextVisible(false))
                if (property.name != "m_Script") fields.Add(property.name);
            return fields;
        }

        // An object field that is empty, or the SpriteState sub-fields that are empty.
        static IEnumerable<string> EmptyReferences(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
                return property.objectReferenceValue == null ? new[] { property.propertyPath } : Array.Empty<string>();
            return SpriteStateFields.Where(name => property.FindPropertyRelative(name)?.objectReferenceValue == null)
                .Select(name => property.propertyPath + "." + name);
        }

        // Shared fields whose default-skin value is empty or is not the bright skin's object, SpriteState sub-fields
        // compared one by one.
        static List<string> FieldsNotShared(string defaultSkinPath, string brightSkinPath)
        {
            var defaults = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(defaultSkinPath));
            var bright = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(brightSkinPath));
            var problems = new List<string>();
            foreach (string field in SharedSkinFields)
            {
                SerializedProperty mine = defaults.FindProperty(field);
                string[] paths = mine != null && mine.propertyType == SerializedPropertyType.ObjectReference
                    ? new[] { field }
                    : SpriteStateFields.Select(name => field + "." + name).ToArray();
                foreach (string path in paths)
                {
                    UnityEngine.Object value = defaults.FindProperty(path)?.objectReferenceValue;
                    UnityEngine.Object expected = bright.FindProperty(path)?.objectReferenceValue;
                    if (value == null || value != expected)
                        problems.Add($"{path}: default {Name(value)}, bright skin {Name(expected)}");
                }
            }
            return problems;
        }

        static string Name(UnityEngine.Object value) => value == null ? "nothing" : AssetDatabase.GetAssetPath(value) + " " + value.name;

        static IEnumerable<string> AllPieceNames() => Pieces.Select(piece => piece.Name);

        static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
