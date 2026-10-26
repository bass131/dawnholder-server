using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent Play Mode checks of the guild growth effect (growth-effect-pr independent-preparation-01, verifying
    // implementation Task task_826d0b47be93). They aim at what the pretest's 18 cases leave open: a request after a cancel,
    // a disable or a deactivation gets its whole window; refused requests leave a running effect alone; a request made while
    // inactive is not kept for later; the owning scene's unload, repeated builds and partial destroys release the runtime
    // objects and sprites but keep the shared atlas; the largest level stays whole on screen. Expected values come from the
    // requirement (1.8-2.0 s shared, 1.0-1.2 s personal, kind-wise latest one, no queue or replay, cleanup on cancel, disable,
    // inactive and destroy) and from counts observed before each action, never from the product's fields or formulas. Real
    // two-resolution frames, recordings and frame or memory cost are GuildGrowthEffectIndependentCaptureTests.
    public sealed class GuildGrowthEffectIndependentLifecycleTests
    {
        // Requirement: the shared growth about 1.8-2.0 s, the personal enhancement about 1.0-1.2 s.
        const float SharedShortest = 1.8f;
        const float SharedLongest = 2.0f;
        const float PersonalShortest = 1.0f;
        const float PersonalLongest = 1.2f;
        // Into an effect, shorter than either kind, when an interruption is made.
        const float IntoTheEffect = 0.4f;
        // Watching goes on this long after the screen clears, so a replay or a restarted timer would show inside it.
        const float AfterClear = 0.5f;
        // Longer than the longest effect with margin: a kept request or a late callback would show inside it.
        const float Quiet = SharedLongest + 0.8f;
        const float WatchCap = 4.5f;
        // Build-and-destroy rounds after one warm-up round: a per-build leak would add at least this many objects.
        const int Rounds = 12;

        static readonly GuildChangeKind SharedKind = GuildChangeKind.StrongholdUpgrade;
        static readonly GuildChangeKind PersonalKind = GuildChangeKind.WeaponEnhance;

        readonly List<string> _missingCharacters = new List<string>();
        Scene _previousActive;
        Scene _scene;
        GameObject _parent;
        GameObject _ownedAudioListener;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(Application.isPlaying, "this lane must execute real Unity Play Mode");
            Assert.AreEqual(1f, Time.timeScale, "fixture: real and scaled seconds agree");
            _previousActive = SceneManager.GetActiveScene();
            // Keep our fallback in the previous scene so unloading the effect's scene does not remove it mid-test.
            if (!UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Any(listener => listener.isActiveAndEnabled))
            {
                _ownedAudioListener = new GameObject("GuildGrowthEffectIndependentFixtureAudioListener", typeof(AudioListener));
            }
            _scene = SceneManager.CreateScene("GuildGrowthEffectIndependent_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_scene);
            _parent = new GameObject("GuildGrowthEffectIndependentParent");
            _missingCharacters.Clear();
            TMP_Text.OnMissingCharacter += RecordMissingCharacter;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            var errors = new List<Exception>();
            void Restore(Action action)
            {
                try { action(); }
                catch (Exception error) { errors.Add(error); }
            }
            Restore(() => TMP_Text.OnMissingCharacter -= RecordMissingCharacter);
            Restore(() => { if (_parent != null) UnityEngine.Object.Destroy(_parent); });
            Restore(() =>
            {
                if (_ownedAudioListener != null) UnityEngine.Object.Destroy(_ownedAudioListener);
                _ownedAudioListener = null;
            });
            yield return null;
            Restore(() => { if (_previousActive.IsValid() && _previousActive.isLoaded) SceneManager.SetActiveScene(_previousActive); });
            // The scene-unload case has already unloaded its scene.
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "independent fixture unload", 5);
            }
            if (errors.Count != 0) throw new AggregateException("Independent guild growth effect fixture restoration failed", errors);
        }

        // Requirement "cancel ... 정리" with "queue/replay 0": after CancelAll the screen clears, and a new request then runs
        // its own whole shared window from its own request time with only its own level.
        [UnityTest]
        public IEnumerator Cancel_ThenANewRequest_GetsItsWholeSharedWindow()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("cancel-then-new", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2), "the first shared request is accepted");
            watch.Mark("shared 2");
            yield return watch.For(IntoTheEffect);
            Assert.IsTrue(watch.Frames.Last().Any, "the first effect is drawn before the cancel");
            view.CancelAll();
            float cancelled = watch.Mark("CancelAll");
            yield return watch.For(AfterClear);
            CollectionAssert.IsEmpty(watch.After(cancelled).Where(frame => frame.Any).Select(frame => frame.Index),
                "nothing is drawn after the cancel");

            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 5), "a new shared request after the cancel is accepted");
            float requested = watch.Mark("shared 5");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.AreEqual(new long[] { 5 }, Numbers(watch.After(requested)), "only the new level is shown");
            watch.AssertEndsInside(requested, SharedShortest, SharedLongest, "the new shared request");
            AssertNoMissingGlyphs();
        }

        // Requirement "disable ... 정리 / 재활성 새요청": a view disabled mid-effect shows nothing when enabled again until it is
        // asked, and the next personal request then gets its whole window.
        [UnityTest]
        public IEnumerator ReenabledView_ShowsNothingUntilAsked_ThenAFreshPersonalWindow()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("reenabled-view", view);
            Assert.IsTrue(view.TryPlayConfirmed(PersonalKind, 3), "the first personal request is accepted");
            watch.Mark("personal 3");
            yield return watch.For(IntoTheEffect);
            view.enabled = false;
            watch.Mark("view disabled");
            yield return watch.For(0.2f);
            view.enabled = true;
            float enabled = watch.Mark("view enabled");
            yield return watch.For(Quiet);
            CollectionAssert.IsEmpty(watch.After(enabled).Where(frame => frame.Any).Select(frame => frame.Index),
                "the enabled view replays nothing");

            Assert.IsTrue(view.TryPlayConfirmed(PersonalKind, 4), "a request to the enabled view is accepted");
            float requested = watch.Mark("personal 4");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.AreEqual(new long[] { 4 }, Numbers(watch.After(requested)), "only the new level is shown");
            watch.AssertEndsInside(requested, PersonalShortest, PersonalLongest, "the request after enabling");
            AssertNoMissingGlyphs();
        }

        // Requirement "inactive 정리 / 재활성 새요청" for the caller's parent: reactivated, it shows nothing until asked, and the
        // next shared request then gets its whole window.
        [UnityTest]
        public IEnumerator ReactivatedParent_ShowsNothingUntilAsked_ThenAFreshSharedWindow()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("reactivated-parent", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2), "the first shared request is accepted");
            watch.Mark("shared 2");
            yield return watch.For(IntoTheEffect);
            _parent.SetActive(false);
            watch.Mark("parent inactive");
            yield return watch.For(0.2f);
            _parent.SetActive(true);
            float active = watch.Mark("parent active");
            yield return watch.For(Quiet);
            CollectionAssert.IsEmpty(watch.After(active).Where(frame => frame.Any).Select(frame => frame.Index),
                "the reactivated parent replays nothing");

            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 6), "a request after reactivation is accepted");
            float requested = watch.Mark("shared 6");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.AreEqual(new long[] { 6 }, Numbers(watch.After(requested)), "only the new level is shown");
            watch.AssertEndsInside(requested, SharedShortest, SharedLongest, "the request after reactivation");
            AssertNoMissingGlyphs();
        }

        // Requirement "허용 2 kind / 양의 레벨" with "kind별 최신 1": a refused request is not the latest of anything, so a
        // zero or negative level or another kind sent mid-effect neither restarts nor ends the running shared effect.
        [UnityTest]
        public IEnumerator RefusedRequests_DuringAnEffect_NeitherRestartNorEndIt()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("refused-mid-effect", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2), "the shared request is accepted");
            float requested = watch.Mark("shared 2");
            yield return watch.For(IntoTheEffect);
            var refused = new List<string>();
            foreach (var (kind, level) in new[] { (SharedKind, 0), (SharedKind, -1), (GuildChangeKind.Membership, 7),
                (GuildChangeKind.Query, 8), ((GuildChangeKind)200, 9) })
            {
                if (view.TryPlayConfirmed(kind, level)) refused.Add($"{kind} {level} was accepted");
            }
            watch.Mark("five refused requests");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.IsEmpty(refused, "zero, negative and other-kind requests are refused");
            CollectionAssert.AreEqual(new long[] { 2 }, Numbers(watch.After(requested)), "only the accepted level is ever shown");
            watch.AssertEndsInside(requested, SharedShortest, SharedLongest, "the shared effect with refused requests mid-way");
        }

        // Requirement "queue/replay 0" with "inactive": a request to a view built under an inactive parent is not kept and
        // shown once the parent becomes active; the first request after activation runs its whole personal window.
        [UnityTest]
        public IEnumerator RequestWhileTheParentIsInactive_IsNotKeptForActivation()
        {
            _parent.SetActive(false);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("inactive-build", view);
            bool whileInactive = view.TryPlayConfirmed(PersonalKind, 7);
            watch.Mark($"personal 7 while inactive -> {whileInactive}");
            yield return watch.For(0.2f);
            _parent.SetActive(true);
            float active = watch.Mark("parent active");
            yield return watch.For(Quiet);
            CollectionAssert.IsEmpty(watch.After(active).Where(frame => frame.Any).Select(frame => frame.Index),
                $"the request made while inactive (returned {whileInactive}) is not shown after activation");

            Assert.IsTrue(view.TryPlayConfirmed(PersonalKind, 3), "the first request after activation is accepted");
            float requested = watch.Mark("personal 3");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.AreEqual(new long[] { 3 }, Numbers(watch.After(requested)), "only the new level is shown");
            watch.AssertEndsInside(requested, PersonalShortest, PersonalLongest, "the first request after activation");
            AssertNoMissingGlyphs();
        }

        // Requirement "scene-owned / destroy 정리": unloading the scene that owns the parent removes the effect, releases every
        // runtime sprite it cut, raises nothing later and leaves the shared atlas and the text's font loaded.
        [UnityTest]
        public IEnumerator UnloadingTheOwningScene_ReleasesObjectsAndSprites_AndKeepsTheAtlas()
        {
            Texture2D atlas = GuildGrowthEffectIndependentProbe.Atlas();
            int spritesBefore = GuildGrowthEffectIndependentProbe.SpritesOf(atlas);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            GameObject root = view.gameObject;
            var watch = Watch("scene-unload", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2) & view.TryPlayConfirmed(PersonalKind, 3), "both kinds are accepted");
            yield return watch.For(IntoTheEffect);
            TMP_FontAsset[] fonts = watch.Frames.SelectMany(frame => frame.Parts).Where(part => part.Label != null)
                .Select(part => part.Label.font).Where(font => font != null).Distinct().ToArray();
            CollectionAssert.IsNotEmpty(fonts, "the level text is drawn with a font before the unload");
            int spritesWhilePlaying = GuildGrowthEffectIndependentProbe.SpritesOf(atlas);
            Assert.Greater(spritesWhilePlaying, spritesBefore, "the effect cuts runtime sprites from the atlas");

            SceneManager.SetActiveScene(_previousActive);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
            Assert.IsNotNull(unload, "the owning scene can be unloaded");
            yield return MapEntryPlayFixture.Wait(() => unload.isDone, "owning scene unload", 5);
            float unloaded = watch.Mark("owning scene unloaded");
            yield return watch.For(Quiet);

            TestContext.Out.WriteLine($"[GuildGrowthEffectIndependent] scene-unload sprites before={spritesBefore} playing={spritesWhilePlaying} " +
                $"after={GuildGrowthEffectIndependentProbe.SpritesOf(atlas)} fonts=[{string.Join(", ", fonts.Select(font => font.name))}]");
            Assert.IsTrue(root == null, "the effect root is gone with its scene");
            CollectionAssert.IsEmpty(watch.After(unloaded).Where(frame => frame.Any).Select(frame => frame.Index), "nothing is drawn later");
            Assert.AreEqual(spritesBefore, GuildGrowthEffectIndependentProbe.SpritesOf(atlas), "every runtime sprite is released");
            Assert.IsTrue(atlas != null && atlas.width > 0, "the shared atlas stays loaded");
            Assert.IsTrue(fonts.All(font => font != null), "the text's font asset stays loaded");
            LogAssert.NoUnexpectedReceived();
        }

        // Requirement "destroy 정리" over many builds: after a warm-up round, twelve build-play-destroy rounds bring the scene's
        // objects and the atlas's sprites back to their counts before them, add fewer meshes, materials and textures than
        // rounds (a per-build leak would add at least one each), keep the atlas, and a new view still plays.
        [UnityTest]
        public IEnumerator RepeatedBuildAndDestroy_ReturnsToTheBaseline_AndANewViewStillPlays()
        {
            Texture2D atlas = GuildGrowthEffectIndependentProbe.Atlas();
            yield return BuildPlayDestroy();
            var before = Counts(atlas);
            for (int round = 0; round < Rounds; round++) yield return BuildPlayDestroy();
            var after = Counts(atlas);
            Debug.Log($"[GuildGrowthEffectIndependent] rounds={Rounds} before=[{Describe(before)}] after=[{Describe(after)}]");

            Assert.AreEqual(before["GameObject"], after["GameObject"], "scene objects return to their count");
            Assert.AreEqual(before["Sprite"], after["Sprite"], "atlas sprites return to their count");
            foreach (string kind in new[] { "Mesh", "Material", "Texture2D", "Canvas" })
            {
                Assert.Less(after[kind] - before[kind], Rounds, kind + " objects do not grow with every build");
            }
            Assert.IsTrue(atlas != null && atlas.width > 0, "the shared atlas stays loaded");

            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var watch = Watch("after-rounds", view);
            Assert.IsTrue(view.TryPlayConfirmed(PersonalKind, 9), "a new view accepts a request");
            float requested = watch.Mark("personal 9");
            yield return watch.UntilClear(AfterClear, WatchCap);
            CollectionAssert.AreEqual(new long[] { 9 }, Numbers(watch.After(requested)), "the new view shows its level");
            watch.AssertEndsInside(requested, PersonalShortest, PersonalLongest, "the view built after the rounds");
        }

        // Requirement "양의 레벨 / 문장·가독성": the largest accepted level is shown whole, not cut, inside the screen for each
        // kind, the two kinds' drawn footprints do not overlap, and both levels are on screen together when both play.
        [UnityTest]
        public IEnumerator LargestLevel_IsShownWhole_InsideTheScreen_AndTheKindsDoNotOverlap()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            var shared = Watch("largest/shared", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, int.MaxValue), "the largest shared level is accepted");
            yield return shared.UntilClear(AfterClear, WatchCap);
            var personal = Watch("largest/personal", view);
            Assert.IsTrue(view.TryPlayConfirmed(PersonalKind, int.MaxValue), "the largest personal level is accepted");
            yield return personal.UntilClear(AfterClear, WatchCap);
            var both = Watch("largest/both", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, int.MaxValue) & view.TryPlayConfirmed(PersonalKind, int.MaxValue - 1),
                "both kinds are accepted together");
            yield return both.UntilClear(AfterClear, WatchCap);

            foreach (GuildGrowthEffectIndependentWatch watch in new[] { shared, personal })
            {
                GuildGrowthEffectIndependentPart[] texts = watch.Frames.SelectMany(frame => frame.Parts)
                    .Where(part => part.LaidOut && part.Numbers.Contains(int.MaxValue)).ToArray();
                CollectionAssert.IsNotEmpty(texts, "the largest level is drawn as laid-out text");
                CollectionAssert.IsEmpty(texts.Where(part => part.Label.isTextTruncated).Select(part => part.ToString()),
                    "the largest level is never cut short");
                CollectionAssert.IsEmpty(texts.Where(part => !GuildGrowthEffectIndependentProbe.Inside(part.Rect,
                    GuildGrowthEffectIndependentProbe.ScreenBox())).Select(part => part.ToString()), "the level text stays inside the screen");
            }
            Rect sharedFootprint = shared.Footprint();
            Rect personalFootprint = personal.Footprint();
            float overlap = GuildGrowthEffectIndependentProbe.OverlapArea(sharedFootprint, personalFootprint);
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffectIndependent] largest footprints shared={0} personal={1} " +
                "overlap={2:F0} screen={3}x{4}", sharedFootprint, personalFootprint, overlap, Screen.width, Screen.height));
            Assert.AreEqual(0f, overlap, "the two kinds' drawn footprints do not overlap");
            Assert.IsTrue(both.Frames.Any(frame => frame.Numbers.Contains(int.MaxValue) && frame.Numbers.Contains(int.MaxValue - 1)),
                "both levels are on screen in the same frame");
            AssertNoMissingGlyphs();
        }

        // Requirement "destroy 정리" when a caller destroys the view component but keeps its object: the screen clears, nothing
        // comes back, and the runtime sprites are released. What stays in the hierarchy is logged as a design observation.
        [UnityTest]
        public IEnumerator DestroyingOnlyTheViewComponent_ClearsTheScreen_AndReleasesSprites()
        {
            Texture2D atlas = GuildGrowthEffectIndependentProbe.Atlas();
            int spritesBefore = GuildGrowthEffectIndependentProbe.SpritesOf(atlas);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            Transform root = view.transform;
            var watch = new GuildGrowthEffectIndependentWatch("view-component-destroyed", () => root);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2) & view.TryPlayConfirmed(PersonalKind, 3), "both kinds are accepted");
            yield return watch.For(IntoTheEffect);
            UnityEngine.Object.Destroy(view);
            float destroyed = watch.Mark("view component destroyed");
            yield return watch.For(Quiet);

            string left = root == null ? "root gone" : string.Join(", ", root.GetComponentsInChildren<Transform>(true)
                .Select(item => ScreenUiProbe.PathOf(item, root, true)));
            TestContext.Out.WriteLine($"[GuildGrowthEffectIndependent] view-component-destroyed left in hierarchy: {left}");
            CollectionAssert.IsEmpty(watch.After(destroyed).Where(frame => frame.Any).Select(frame => frame.Index),
                "nothing is drawn after the view is destroyed");
            Assert.AreEqual(spritesBefore, GuildGrowthEffectIndependentProbe.SpritesOf(atlas), "every runtime sprite is released");
            LogAssert.NoUnexpectedReceived();
        }

        // Requirement "inactive / destroy 정리" together: a parent deactivated mid-effect and then destroyed leaves no effect
        // object, no runtime sprite and no error.
        [UnityTest]
        public IEnumerator DestroyingAnInactiveParent_LeavesNoObjectsSpritesOrErrors()
        {
            Texture2D atlas = GuildGrowthEffectIndependentProbe.Atlas();
            int spritesBefore = GuildGrowthEffectIndependentProbe.SpritesOf(atlas);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            GameObject root = view.gameObject;
            var watch = Watch("inactive-parent-destroyed", view);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2), "the shared request is accepted");
            yield return watch.For(IntoTheEffect);
            _parent.SetActive(false);
            yield return null;
            UnityEngine.Object.Destroy(_parent);
            watch.Mark("inactive parent destroyed");
            yield return watch.For(0.3f);

            Assert.IsTrue(root == null, "the effect root is gone with its parent");
            Assert.AreEqual(spritesBefore, GuildGrowthEffectIndependentProbe.SpritesOf(atlas), "every runtime sprite is released");
            LogAssert.NoUnexpectedReceived();
        }

        static GuildGrowthEffectIndependentWatch Watch(string label, GuildGrowthEffectView view)
        {
            Transform root = view.transform;
            return new GuildGrowthEffectIndependentWatch(label, () => root);
        }

        static long[] Numbers(IEnumerable<GuildGrowthEffectIndependentFrame> frames) =>
            frames.SelectMany(frame => frame.Numbers).Distinct().OrderBy(number => number).ToArray();

        IEnumerator BuildPlayDestroy()
        {
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_parent.transform);
            Assert.IsTrue(view.TryPlayConfirmed(SharedKind, 2) & view.TryPlayConfirmed(PersonalKind, 3), "both kinds are accepted");
            yield return null;
            yield return null;
            UnityEngine.Object.Destroy(view.gameObject);
            // Destroy takes effect at the end of the frame.
            yield return null;
            yield return null;
        }

        static Dictionary<string, int> Counts(Texture2D atlas) => new Dictionary<string, int>
        {
            ["GameObject"] = GuildGrowthEffectIndependentProbe.Live<GameObject>(),
            ["Sprite"] = GuildGrowthEffectIndependentProbe.SpritesOf(atlas),
            ["Mesh"] = GuildGrowthEffectIndependentProbe.Live<Mesh>(),
            ["Material"] = GuildGrowthEffectIndependentProbe.Live<Material>(),
            ["Texture2D"] = GuildGrowthEffectIndependentProbe.Live<Texture2D>(),
            ["Canvas"] = GuildGrowthEffectIndependentProbe.Live<Canvas>(),
        };

        static string Describe(Dictionary<string, int> counts) => string.Join(", ", counts.Select(item => item.Key + "=" + item.Value));

        void AssertNoMissingGlyphs() => CollectionAssert.IsEmpty(_missingCharacters, "every character of the effect's text has a glyph");

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            // The event is global; only texts under this test's parent count.
            if (_parent == null || textComponent == null || !textComponent.transform.IsChildOf(_parent.transform)) return;
            _missingCharacters.Add(string.Format(CultureInfo.InvariantCulture, "U+{0:X4} at {1} in '{2}' ({3})", unicode, stringIndex,
                text, fontAsset != null ? fontAsset.name : "no font"));
        }
    }
}
