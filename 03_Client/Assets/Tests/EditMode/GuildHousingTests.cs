using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Uses the real runtime window and supplied display values. The fixed hashes identify the approved Art files;
    // import, rendering and live-server checks remain separate from this test's supplied model.
    public sealed class GuildHousingTests
    {
        GuildModelDriver _model;
        GuildWindowDriver _window;

        [SetUp]
        public void Setup()
        {
            _model = new GuildModelDriver();
            _window = new GuildWindowDriver(GuildWindowDriver.Guild);
            _window.Bind(_model);
            _window.SetOpen(true);
            _window.SetInputAllowed(true);
        }

        [TearDown]
        public void Teardown() => _window?.Dispose();

        [TestCase(1, "77f9f79dcce88612b553628019dae05f975a7ca17871559aff47cde02813837a")]
        [TestCase(2, "6bbc6e1ec3c4613aac9e8c38167b93ef902a6d0b111ac160cf9489d37046ed30")]
        public void Scenery_ImportsTheWholeUnmodifiedArtFile(int level, string expectedHash)
        {
            string resource = "UI/GuildHousing/GuildBaseLevel" + level;
            string asset = "Assets/Resources/" + resource + ".png";
            byte[] bytes = File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources", resource + ".png"));
            string actualHash;
            using (SHA256 hash = SHA256.Create())
                actualHash = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            Sprite sprite = Resources.Load<Sprite>(resource);
            var importer = AssetImporter.GetAtPath(asset) as TextureImporter;

            Assert.AreEqual(expectedHash, actualHash, "the approved source PNG remains byte-identical");
            Assert.AreEqual(8, bytes[24], "source PNG bit depth");
            Assert.AreEqual(2, bytes[25], "source PNG is opaque RGB, not a generated transparent cutout");
            Assert.IsNotNull(sprite, "the runtime resource path resolves to a Sprite");
            Assert.AreEqual(asset, AssetDatabase.GetAssetPath(sprite));
            Assert.AreEqual(new Vector2(1672f, 941f), sprite.rect.size, "no crop or import downscale");
            Assert.AreEqual(1672, sprite.texture.width);
            Assert.AreEqual(941, sprite.texture.height);
            Assert.IsNotNull(importer);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.IsFalse(importer.mipmapEnabled, "the foreground is screen UI");
        }

        [Test]
        public void UnsynchronizedWindow_DoesNotPresentLevelOneAsConfirmed()
        {
            Image scenery = Scenery();

            Assert.IsFalse(scenery.enabled);
            Assert.IsNull(scenery.sprite);
            Assert.IsEmpty(_window.ShownTextsWithDigits(), "null remains distinct from a confirmed level or zero");
            _window.PressAction();
            Assert.AreEqual(0, _window.Intents);
        }

        [Test]
        public void RequestedGrowth_ChangesSceneryOnlyWhenTheConfirmedDisplayChanges()
        {
            _model.SetGuild(GuildInput.Funded());
            Image scenery = Scenery();
            Vector2 originalSize = scenery.rectTransform.sizeDelta;
            Vector2 originalPosition = scenery.rectTransform.anchoredPosition;
            Sprite levelOne = Artwork(1);
            Sprite levelTwo = Artwork(2);
            Assert.AreSame(levelOne, scenery.sprite);
            Assert.IsTrue(scenery.enabled);

            _window.PressAction();
            Assert.AreEqual(1, _window.Intents);
            Assert.AreSame(levelOne, scenery.sprite, "a local request does not predict a new level");
            CollectionAssert.Contains(_window.Numbers("Treasury"), 30L);
            _model.SetGuild(GuildInput.Purchased());
            Assert.AreSame(levelTwo, scenery.sprite);
            Assert.AreEqual(originalSize, scenery.rectTransform.sizeDelta);
            Assert.AreEqual(originalPosition, scenery.rectTransform.anchoredPosition);
            Assert.IsTrue(scenery.preserveAspect);
            Assert.IsFalse(scenery.raycastTarget, "the existing panel owns shielding");
            _model.SetGuild(GuildInput.Purchased());
            Assert.AreSame(levelTwo, scenery.sprite, "a repeated query only selects the current still image");
            Assert.AreEqual(1, _window.Intents, "confirmed refreshes do not request another upgrade");
        }

        [Test]
        public void Rebind_ReleasesOldSourceAndClearsSceneryUntilTheNewSourceSynchronizes()
        {
            _model.SetGuild(GuildInput.Funded());
            var replacement = new GuildModelDriver();
            _window.Bind(replacement);
            Assert.AreEqual(0, _model.Subscribers);
            Assert.AreEqual(1, replacement.Subscribers);
            Assert.IsFalse(Scenery().enabled);
            Assert.IsNull(Scenery().sprite);

            _model.SetGuild(GuildInput.Purchased());
            Assert.IsNull(Scenery().sprite, "late updates from the old source cannot select scenery");
            replacement.SetGuild(GuildInput.Purchased());
            Assert.AreSame(Artwork(2), Scenery().sprite,
                "first synchronization after reconnect shows the currently confirmed level");
            Assert.AreEqual(0, _window.Intents);
            _window.Bind(null);
            Assert.AreEqual(0, replacement.Subscribers);
            Assert.IsFalse(Scenery().enabled);
            Assert.IsNull(Scenery().sprite);
        }

        [TestCase("pending")]
        [TestCase("input blocked")]
        [TestCase("closed")]
        public void UnavailableInput_PreservesTheConfirmedSceneryAndSendsNoIntent(string state)
        {
            _model.SetGuild(GuildInput.Funded().With(input => input.LevelUpPending = state == "pending"));
            if (state == "input blocked") _window.SetInputAllowed(false);
            if (state == "closed") _window.SetOpen(false);
            _window.PressAction();

            Assert.AreSame(Artwork(1), Scenery().sprite);
            Assert.AreEqual(0, _window.Intents, state + " rechecks the current request gate");
            CanvasGroup group = _window.Window.GetComponent<CanvasGroup>();
            Assert.AreEqual(state != "closed", group.blocksRaycasts, "an open unavailable window still shields");
            Assert.AreEqual(state == "closed" ? 0f : 1f, group.alpha);
        }

        [Test]
        public void ConnectionFailure_PreservesSuppliedTextAndRefreshAvailability()
        {
            _model.SetGuild(GuildInput.Funded());
            var window = (GuildWindow)_window.Window;
            int refreshes = 0;
            window.RefreshRequested += () => refreshes++;
            const string status = "전송 오류 · 서버 확인 대기 중입니다.";
            const string result = "최근 길드 요청 거부 · 상태가 변경되었습니다. 최신 정보를 확인하세요.";
            MethodInfo connection = typeof(GuildWindow).GetMethod("SetConnectionStatus", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(connection);
            connection.Invoke(window, new object[] { status, result, true });
            window.SetInputAllowed(false);
            Button refresh = _window.Find<Button>("Refresh");

            Assert.AreEqual(status, _window.Text("ActionStatus"));
            Assert.AreEqual(result, _window.Text("ActionResult"));
            Assert.IsTrue(refresh.interactable, "the supplied refresh gate is separate from purchase availability");
            refresh.onClick.Invoke();
            Assert.AreEqual(1, refreshes);
            window.SetOpen(false);
            refresh.onClick.Invoke();
            Assert.AreEqual(1, refreshes, "a closed window rejects even a direct onClick");
        }

        static Sprite Artwork(int level)
        {
            Sprite sprite = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel" + level);
            Assert.IsNotNull(sprite, "the expected confirmed-level resource exists");
            return sprite;
        }

        Image Scenery() => _window.Find<Image>("HousingForeground");
    }
}
