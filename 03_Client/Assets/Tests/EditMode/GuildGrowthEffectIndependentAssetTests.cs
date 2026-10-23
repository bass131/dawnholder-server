using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // Independent check of the guild growth effect's committed assets (growth-effect-pr independent-preparation-01, verifying
    // implementation Task task_826d0b47be93). Every expected value is read from the files at run time: GUIDs from each .meta,
    // the atlas size and colour type from the PNG header, transparency from its decoded pixels and the hash from its bytes.
    // Nothing is copied from the implementation, its report or the pretest. Frames, lifetimes and resource release are judged
    // in PlayMode/GuildGrowthEffectIndependentLifecycleTests.
    public sealed class GuildGrowthEffectIndependentAssetTests
    {
        const string ScriptFolder = "Assets/Scripts/UI";
        const string EffectFolder = "Assets/Resources/UI/GuildGrowthEffect";
        const string AtlasAsset = EffectFolder + "/GuildGrowthEffectAtlas.png";
        const string SourceRecordAsset = EffectFolder + "/ASSET_SOURCE.md";
        const string ResourcesFolder = "Assets/Resources/";
        // PNG colour type 6 is truecolour with alpha (PNG specification, IHDR chunk).
        const byte PngTruecolourWithAlpha = 6;
        static readonly Regex MetaGuid = new Regex("^guid: ([0-9a-f]{32})\\s*$", RegexOptions.Multiline);
        static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // The new product assets of the effect PR (git status of the fixed inputs): two scripts, the asset folder, the atlas
        // and its source record.
        static readonly string[] NewAssets =
        {
            ScriptFolder + "/GuildGrowthEffectView.cs",
            ScriptFolder + "/GuildGrowthEffectPresentation.cs",
            EffectFolder,
            AtlasAsset,
            SourceRecordAsset,
        };

        // AGENTS 공학 조건: Unity assets keep their .meta GUID. An import that met a clashing GUID would give the asset
        // another one, so the database must answer each new asset with the GUID its own .meta carries, and resolve back.
        // The run harness separately proves that no .meta in the snapshot was rewritten during the run.
        [Test]
        public void NewEffectAssets_KeepTheGuidOfTheirMetaFiles_BothWays()
        {
            var differ = new List<string>();
            foreach (string asset in NewAssets)
            {
                string metaPath = FullPath(asset) + ".meta";
                Assert.IsTrue(File.Exists(metaPath), "the asset has a .meta file: " + metaPath);
                Match match = MetaGuid.Match(File.ReadAllText(metaPath));
                Assert.IsTrue(match.Success, "the .meta file names a GUID: " + metaPath);
                string written = match.Groups[1].Value;
                string imported = AssetDatabase.AssetPathToGUID(asset);
                string resolved = AssetDatabase.GUIDToAssetPath(written);
                Debug.Log($"[GuildGrowthEffectIndependent] guid {asset} meta={written} database={imported} resolves={resolved}");
                if (imported != written || resolved != asset)
                {
                    differ.Add($"{asset}: meta {written}, database {imported}, resolves {resolved}");
                }
            }
            CollectionAssert.IsEmpty(differ, "every new asset keeps the GUID of its .meta file");
        }

        // The effect draws transparent emblem and light images over the scene (approved plan 4절 재료: 투명 문장/광선/광점).
        // The imported atlas keeps the pixel size of its PNG file, which the effect's runtime sprites are cut from, and its
        // alpha. The PNG itself holds fully clear and partly clear pixels, so nothing was flattened onto an opaque square.
        [Test]
        public void Atlas_ImportsAtThePngFileSize_AndKeepsItsTransparency()
        {
            byte[] png = File.ReadAllBytes(FullPath(AtlasAsset));
            var (width, height, colourType) = PngHeader(png);
            Assert.AreEqual(PngTruecolourWithAlpha, colourType, "the PNG file stores colour with alpha");

            string key = ResourcesKey(AtlasAsset);
            var imported = Resources.Load<Texture2D>(key);
            Assert.IsNotNull(imported, "Resources loads the atlas by " + key);
            var importer = AssetImporter.GetAtPath(AtlasAsset) as TextureImporter;
            Assert.IsNotNull(importer, "the atlas is imported as a texture");
            Debug.Log($"[GuildGrowthEffectIndependent] atlas file {width}x{height} colourType {colourType}; imported " +
                $"{imported.width}x{imported.height} {imported.format}; importer type {importer.textureType} npot " +
                $"{importer.npotScale} max {importer.maxTextureSize} mip {importer.mipmapEnabled} alphaSource " +
                $"{importer.alphaSource} alphaIsTransparency {importer.alphaIsTransparency}");
            Assert.AreEqual(width, imported.width, "the imported width equals the PNG file width");
            Assert.AreEqual(height, imported.height, "the imported height equals the PNG file height");
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), "the importer reads the source alpha");
            Assert.AreNotEqual(TextureImporterAlphaSource.None, importer.alphaSource, "the import keeps an alpha source");

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(decoded.LoadImage(png, false), "the PNG bytes decode");
                Color32[] pixels = decoded.GetPixels32();
                int clear = pixels.Count(pixel => pixel.a == 0);
                int partial = pixels.Count(pixel => pixel.a > 0 && pixel.a < 255);
                Debug.Log($"[GuildGrowthEffectIndependent] atlas pixels {pixels.Length}: clear {clear}, partial {partial}");
                Assert.Greater(clear, 0, "the atlas has fully clear pixels around its images");
                Assert.Greater(partial, 0, "the atlas has partly clear pixels for its light and edges");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        // goal 완료조건 4 (새 자산 출처·원본/가공·메타): the source record beside the atlas names the consumed PNG's real
        // SHA256, its real pixel size and its Resources key. All three are computed from the files here.
        [Test]
        public void SourceRecord_NamesTheAtlasRealHashSizeAndKey()
        {
            byte[] png = File.ReadAllBytes(FullPath(AtlasAsset));
            string hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = string.Concat(sha.ComputeHash(png).Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
            var (width, height, _) = PngHeader(png);
            string record = File.ReadAllText(FullPath(SourceRecordAsset));
            string key = ResourcesKey(AtlasAsset);
            Debug.Log($"[GuildGrowthEffectIndependent] source record check: sha256 {hash}, size {width}x{height}, key {key}");

            StringAssert.Contains(hash, record, "the source record names the atlas file's SHA256");
            Assert.IsTrue(Regex.IsMatch(record, $"\\b{width}\\s*[x×]\\s*{height}\\b"), $"the source record names {width} x {height}");
            StringAssert.Contains(key, record, "the source record names the Resources key the effect loads");
        }

        // API-consumption.md: a null or destroyed parent is refused with ArgumentNullException before anything is made. The
        // pretest covers null; this is the stale Transform a caller still holds after its object was destroyed.
        [Test]
        public void BuildRuntime_RefusesADestroyedParent_AndMakesNothing()
        {
            var doomed = new GameObject("GuildGrowthEffectIndependentDoomedParent");
            Transform stale = doomed.transform;
            UnityEngine.Object.DestroyImmediate(doomed);
            HashSet<GameObject> before = SceneObjects();
            int spritesBefore = Resources.FindObjectsOfTypeAll<Sprite>().Length;

            Assert.Throws<ArgumentNullException>(() => GuildGrowthEffectView.BuildRuntime(stale), "a destroyed parent is refused");

            CollectionAssert.IsEmpty(SceneObjects().Except(before).Select(item => item.name), "no object is made for it");
            Assert.AreEqual(spritesBefore, Resources.FindObjectsOfTypeAll<Sprite>().Length, "no runtime sprite is made for it");
        }

        static string FullPath(string asset) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, asset);

        // Resources keys drop the Resources folder and the file extension: Assets/Resources/UI/A/B.png is UI/A/B.
        static string ResourcesKey(string asset)
        {
            Assert.IsTrue(asset.StartsWith(ResourcesFolder, StringComparison.Ordinal), "a Resources asset: " + asset);
            string relative = asset.Substring(ResourcesFolder.Length);
            return relative.Substring(0, relative.Length - Path.GetExtension(relative).Length);
        }

        // Width, height and colour type from the signature and the first chunk, IHDR, whose integers are big-endian.
        static (int Width, int Height, byte ColourType) PngHeader(byte[] png)
        {
            Assert.GreaterOrEqual(png.Length, 33, "the PNG file holds its IHDR chunk");
            CollectionAssert.AreEqual(PngSignature, png.Take(PngSignature.Length).ToArray(), "the PNG signature");
            Assert.AreEqual("IHDR", Encoding.ASCII.GetString(png, 12, 4), "the first chunk is IHDR");
            return (ReadBigEndian(png, 16), ReadBigEndian(png, 20), png[25]);
        }

        static int ReadBigEndian(byte[] bytes, int offset) =>
            bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];

        // Every GameObject of every loaded scene, hidden ones included.
        static HashSet<GameObject> SceneObjects() => new HashSet<GameObject>(
            Resources.FindObjectsOfTypeAll<GameObject>().Where(item => item.scene.IsValid()));
    }
}
