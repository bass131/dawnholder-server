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
    // Pretest of the guild growth effect in real Play Mode frames (growth-effect-pr pretest-contract-v2 requirements 2-6),
    // written before the product. GuildGrowthEffectProduct reaches the view by its contract names, so on the baseline every
    // case fails with "RED unimplemented". Expected values come from the inputs each case chooses (kinds by name, levels 2
    // and 3, changed results 4-7) and from the contract and the approved plan, kept as the named constants below. Verdicts
    // read what is drawn and what a pointer hits, never the product's fields, and the thresholds were fixed before any run
    // (pretest/api-contract.md). The fixture is an unsaved scene with an EventSystem and a plain scene-owned parent; it does
    // not replace InputSettings or add input devices, because no case sends device input. Two-resolution screenshots,
    // recordings and frame or memory cost are later capture work (requirement 7); the raw "[GuildGrowthEffect]" log lines
    // mark the moments to capture.
    public sealed class GuildGrowthEffectLifecycleTests
    {
        // Requirement 3 design targets: the shared growth about 1.8-2.0 s, the personal enhancement about 1.0-1.2 s.
        const float SharedShortest = 1.8f;
        const float SharedLongest = 2.0f;
        const float PersonalShortest = 1.0f;
        const float PersonalLongest = 1.2f;
        // Watching stops this long after the last drawn frame: a replay or a queued request would show inside it.
        const float AfterEndSeconds = 0.6f;
        // Longer than either effect: a late callback after a cancel, disable or destroy would show inside it.
        const float QuietSeconds = 2.6f;
        const float WatchCapSeconds = 4f;
        const float PlayingSeconds = 0.5f;
        const float BothPlayingSeconds = 0.4f;
        const float MidwaySeconds = 0.9f;
        // What a player notices: a tenth of full alpha or of the drawn size, and two images appearing or peaking a tenth of
        // a second apart (the plan's pre-light, emblem, rays and afterglow come one after another).
        const float NoticeableChange = 0.1f;
        const float StageGapSeconds = 0.1f;
        // Dungeon rewards' canvas order in the approved plan's layer table (HUD 10, guild 12, rewards 15, menu 20).
        const int RewardCanvasOrder = 15;
        // Bounds the pointer points probed after an effect ends and the overlap lines reported, so a failure stays readable.
        const int MaxBlockerPoints = 32;

        // The two kinds the contract accepts, by name: the shared stronghold growth and the personal weapon enhancement.
        static readonly GuildChangeKind SharedKind = GuildChangeKind.StrongholdUpgrade;
        static readonly GuildChangeKind PersonalKind = GuildChangeKind.WeaponEnhance;

        // TMP raises this when neither the font nor a fallback has a character and draws the missing-glyph square instead.
        readonly List<string> _missingCharacters = new List<string>();
        Scene _previousActive;
        Scene _scene;
        GameObject _eventSystem;
        GameObject _parent;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(Application.isPlaying, "this lane must execute real Unity Play Mode");
            Assert.AreEqual(1f, Time.timeScale, "fixture: scaled and real seconds agree");
            Assert.IsTrue(Screen.width > 0 && Screen.height > 0, "fixture: a screen to draw on");
            _previousActive = SceneManager.GetActiveScene();
            _scene = SceneManager.CreateScene("GuildGrowthEffectFixture_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_scene);
            _eventSystem = new GameObject("GuildGrowthEffectTestEventSystem");
            _eventSystem.AddComponent(GuildGrowthEffectScreen.UiType("UnityEngine.EventSystems.EventSystem"));
            _parent = new GameObject("GuildGrowthEffectTestParent");
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
            Restore(() => { if (_eventSystem != null) UnityEngine.Object.Destroy(_eventSystem); });
            yield return null;
            Restore(() => { if (_previousActive.IsValid() && _previousActive.isLoaded) SceneManager.SetActiveScene(_previousActive); });
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "guild growth effect fixture unload", 5);
            }
            if (errors.Count != 0) throw new AggregateException("Guild growth effect fixture restoration failed", errors);
        }

        // Requirements 2 and 3 for the shared growth: a confirmed StrongholdUpgrade 2 is accepted and shows 2 and no other
        // number in readable text, with at least two drawn images (emblem and light) that animate and come in stages, ends
        // inside its 1.8-2.0 s target and leaves nothing drawn and nothing a pointer hits.
        [UnityTest]
        public IEnumerator SharedGrowth_ShowsOnlyItsLevel_AsStagedAnimatedImages_AndEndsInItsWindow()
        {
            yield return ShowsAloneInItsWindow(SharedKind, 2, SharedShortest, SharedLongest);
        }

        // The same for the personal enhancement: WeaponEnhance 3, inside its 1.0-1.2 s target.
        [UnityTest]
        public IEnumerator PersonalEnhancement_ShowsOnlyItsLevel_AsStagedAnimatedImages_AndEndsInItsWindow()
        {
            yield return ShowsAloneInItsWindow(PersonalKind, 3, PersonalShortest, PersonalLongest);
        }

        // Requirement 2 between the kinds: each draws an image the other never does (its own emblem), and the personal
        // enhancement covers less of the screen than the shared growth (plan: the personal one is the smaller effect).
        [UnityTest]
        public IEnumerator TheTwoKinds_DrawTheirOwnEmblems_AndThePersonalOneIsSmaller()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var sharedTrace = new GuildGrowthEffectTrace("two-kinds/shared", _parent.transform);
            bool shared = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 2);
            sharedTrace.Mark($"TryPlayConfirmed({SharedKind}, 2) -> {shared}");
            yield return sharedTrace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);
            var personalTrace = new GuildGrowthEffectTrace("two-kinds/personal", _parent.transform);
            bool personal = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 3);
            personalTrace.Mark($"TryPlayConfirmed({PersonalKind}, 3) -> {personal}");
            yield return personalTrace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            Assert.IsTrue(shared && personal, $"both requests are accepted ({shared}, {personal})");
            string[] sharedImages = ImagesIn(sharedTrace.Samples);
            string[] personalImages = ImagesIn(personalTrace.Samples);
            Debug.Log($"[GuildGrowthEffect] two-kinds images shared=[{string.Join("; ", sharedImages)}] personal=[{string.Join("; ", personalImages)}]");
            CollectionAssert.IsNotEmpty(sharedImages.Except(personalImages), "the shared growth draws an image of its own (its emblem)");
            CollectionAssert.IsNotEmpty(personalImages.Except(sharedImages), "the personal enhancement draws an image of its own (its emblem)");
            float sharedArea = LargestScreenArea(sharedTrace.Samples);
            float personalArea = LargestScreenArea(personalTrace.Samples);
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffect] two-kinds largest drawn area shared={0:F0} personal={1:F0} " +
                "screen={2}x{3}", sharedArea, personalArea, Screen.width, Screen.height));
            Assert.Less(personalArea, sharedArea, "the personal enhancement covers less of the screen than the shared growth");
        }

        // Requirement 2's refusals: kinds other than the two growth kinds (each named, and one value outside the enum) and
        // a zero, negative or minimum level of a growth kind are refused, and nothing is drawn for longer than either effect
        // lasts; a valid request afterwards still shows exactly its level.
        [UnityTest]
        public IEnumerator OtherKindsAndNonPositiveLevels_AreRefused_AndDrawNothing()
        {
            var undefined = (GuildChangeKind)byte.MaxValue;
            Assert.IsFalse(Enum.IsDefined(typeof(GuildChangeKind), undefined), "fixture: 255 is no GuildChangeKind");
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("refused", _parent.transform);
            var accepted = new List<string>();
            foreach (GuildChangeKind kind in new[]
                { GuildChangeKind.Query, GuildChangeKind.Membership, GuildChangeKind.TreasuryCredit, GuildChangeKind.Presence, undefined })
            {
                if (GuildGrowthEffectProduct.TryPlayConfirmed(view, kind, 2)) accepted.Add($"{kind} 2");
            }
            foreach (GuildChangeKind kind in new[] { SharedKind, PersonalKind })
            {
                foreach (int level in new[] { 0, -1, int.MinValue })
                {
                    if (GuildGrowthEffectProduct.TryPlayConfirmed(view, kind, level)) accepted.Add($"{kind} {level}");
                }
            }
            float refusedAt = trace.Mark("refused requests sent");
            yield return trace.Frames(QuietSeconds);

            CollectionAssert.IsEmpty(accepted, "only a growth kind with a positive confirmed level is accepted");
            AssertNothingDrawn(trace.After(refusedAt), "after refused requests");
            yield return AcceptsAgain(view, SharedKind, 2, "refused/control");
        }

        // Requirement 4 for one kind: StrongholdUpgrade 2 then 4 in one frame and 6 the next are all accepted; only the newest
        // level is ever drawn (never 2, at most 4 until 6 arrives, then only 6), one level text at a time, and the effect
        // lasts its whole window from the last request with no replay of the older ones after it ends.
        [UnityTest]
        public IEnumerator SameKindAgain_ReplacesTheShownLevel_AndTimesFromTheLatestRequest()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("same-kind-again", _parent.transform);
            bool first = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 2);
            bool second = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 4);
            float pairAt = trace.Mark($"{SharedKind} 2 then 4 in one frame");
            yield return trace.NextFrame();
            bool third = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 6);
            float latestAt = trace.Mark($"{SharedKind} 6");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            Assert.IsTrue(first && second && third, $"each request of a growth kind is accepted ({first}, {second}, {third})");
            CollectionAssert.IsSubsetOf(GuildGrowthEffectTrace.NumbersIn(trace.Between(pairAt, latestAt)), new long[] { 4 },
                "the frame after 2 then 4 draws 4 at most, never the replaced 2");
            GuildGrowthEffectSample[] latest = trace.After(latestAt);
            CollectionAssert.AreEqual(new long[] { 6 }, GuildGrowthEffectTrace.NumbersIn(latest), "after 6 only 6 is drawn");
            Assert.LessOrEqual(MostLevelTexts(trace.Samples), 1, "one level text per frame for one kind");
            AssertLifetime(latest, latestAt, SharedShortest, SharedLongest, "the replaced shared growth", sample => sample.Any);
        }

        // Requirement 4's late overwrite: StrongholdUpgrade 4 arriving 0.9 s into StrongholdUpgrade 2 replaces it, and the
        // first request's timer neither cuts the new display short nor brings 2 back: 4 lasts its whole window from its
        // own request.
        [UnityTest]
        public IEnumerator SameKindMidway_KeepsTheNewLevelForItsWholeWindow_WithoutTheOldTimer()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("same-kind-midway", _parent.transform);
            bool first = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 2);
            float firstAt = trace.Mark($"{SharedKind} 2");
            yield return trace.Frames(MidwaySeconds);
            bool second = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 4);
            float secondAt = trace.Mark($"{SharedKind} 4 midway");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            Assert.IsTrue(first && second, $"both requests are accepted ({first}, {second})");
            CollectionAssert.AreEqual(new long[] { 2 }, GuildGrowthEffectTrace.NumbersIn(trace.Between(firstAt, secondAt)),
                "before the second request 2 is drawn");
            GuildGrowthEffectSample[] latest = trace.After(secondAt);
            CollectionAssert.AreEqual(new long[] { 4 }, GuildGrowthEffectTrace.NumbersIn(latest), "after it only 4 is drawn");
            AssertLifetime(latest, secondAt, SharedShortest, SharedLongest, "the shared growth replaced midway", sample => sample.Any);
        }

        // Requirement 4 across kinds: StrongholdUpgrade 2 and WeaponEnhance 3 in one frame are drawn together, one level text
        // per kind and two in all, in separate screen areas. WeaponEnhance 5 at 0.4 s replaces 3, which is never drawn again,
        // while the shared growth keeps playing through its own window.
        [UnityTest]
        public IEnumerator BothKinds_ShowTogetherInSeparateAreas_AndEachKeepsOnlyItsLatestLevel()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("both-kinds", _parent.transform);
            bool shared = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 2);
            bool personal = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 3);
            float bothAt = trace.Mark($"{SharedKind} 2 and {PersonalKind} 3");
            yield return trace.Frames(BothPlayingSeconds);
            bool replaced = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 5);
            float replacedAt = trace.Mark($"{PersonalKind} 5");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            Assert.IsTrue(shared && personal && replaced, $"every request is accepted ({shared}, {personal}, {replaced})");
            CollectionAssert.IsEmpty(_missingCharacters, "every drawn character exists in the font or its fallbacks");
            GuildGrowthEffectSample[] before = trace.Between(bothAt, replacedAt);
            GuildGrowthEffectSample[] after = trace.After(replacedAt);
            GuildGrowthEffectSample together = before.FirstOrDefault(sample => sample.Numbers.Contains(2) && sample.Numbers.Contains(3));
            Assert.IsNotNull(together, "2 and 3 are drawn in the same frame");
            CollectionAssert.IsSubsetOf(GuildGrowthEffectTrace.NumbersIn(before), new long[] { 2, 3 }, "before the replacement only 2 and 3");
            CollectionAssert.IsSubsetOf(GuildGrowthEffectTrace.NumbersIn(after), new long[] { 2, 5 }, "after it 3 is never drawn again");
            Assert.IsTrue(after.Any(sample => sample.Numbers.Contains(5)), "the replacing 5 is drawn");
            Assert.LessOrEqual(MostLevelTexts(trace.Samples), 2, "at most one level text per kind, two in all");

            AssertSeparateAreas(before, 2, 3);
            AssertSeparateAreas(after, 2, 5);
            Transform sharedArea = KindAreas(together, 2, 3).Item1;
            GuildGrowthEffectSample firstAfter = after.FirstOrDefault();
            Assert.IsNotNull(firstAfter, "a frame follows the replacement");
            Assert.IsTrue(firstAfter.Drawn.Any(graphic => graphic.IsUnder(sharedArea)),
                "the shared growth is still drawn in the first frame after the personal replacement: " + Describe(firstAfter));
            AssertLifetime(trace.After(bothAt), bothAt, SharedShortest, SharedLongest, "the shared growth beside the personal one",
                sample => sample.Drawn.Any(graphic => graphic.IsUnder(sharedArea)));
        }

        // Requirement 4's caps under a burst: six requests alternating the two kinds in one frame draw at most one level text
        // per kind and two in all, show only each kind's last level, and nothing older comes back after both end.
        [UnityTest]
        public IEnumerator ABurstOfBothKinds_ShowsOnlyEachKindsLastLevel_AndReplaysNothing()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("burst", _parent.transform);
            var refused = new List<string>();
            foreach ((GuildChangeKind kind, int level) in new[] { (SharedKind, 2), (PersonalKind, 3), (SharedKind, 4), (PersonalKind, 5), (SharedKind, 6), (PersonalKind, 7) })
            {
                if (!GuildGrowthEffectProduct.TryPlayConfirmed(view, kind, level)) refused.Add($"{kind} {level}");
            }
            float burstAt = trace.Mark("burst of six requests");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            CollectionAssert.IsEmpty(refused, "every request of a growth kind is accepted");
            GuildGrowthEffectSample[] shown = trace.After(burstAt);
            CollectionAssert.AreEqual(new long[] { 6, 7 }, GuildGrowthEffectTrace.NumbersIn(shown), "only each kind's last level is drawn");
            Assert.LessOrEqual(MostLevelTexts(shown), 2, "at most one level text per kind, two in all");
            Assert.IsFalse(shown.Last().Any, "both effects end and nothing older replays");
        }

        // Requirement 5: CancelAll while both kinds play clears them by the next frame, repeats safely and nothing comes back
        // for longer than either effect lasts (a late callback would show or log); the view then accepts and shows a new
        // request.
        [UnityTest]
        public IEnumerator CancelAll_ClearsBothKinds_IsRepeatable_AndNothingComesBack()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("cancel", _parent.transform);
            yield return PlayBoth(view, trace);
            GuildGrowthEffectProduct.CancelAll(view);
            float cancelAt = trace.Mark("CancelAll");
            trace.LogNow("same frame as CancelAll");
            Assert.DoesNotThrow(() =>
            {
                GuildGrowthEffectProduct.CancelAll(view);
                GuildGrowthEffectProduct.CancelAll(view);
            }, "CancelAll again in the same frame is safe");
            yield return trace.Frames(QuietSeconds);
            Assert.DoesNotThrow(() => GuildGrowthEffectProduct.CancelAll(view), "CancelAll with nothing playing is safe");

            AssertClearedAtOnce(trace.After(cancelAt), "CancelAll");
            LogAssert.NoUnexpectedReceived();
            yield return AcceptsAgain(view, PersonalKind, 3, "cancel/after");
        }

        // Requirement 5: disabling the view component clears both kinds by the next frame and refuses requests while it is
        // disabled; enabled again it replays nothing for longer than either effect lasts, then accepts a new request.
        [UnityTest]
        public IEnumerator DisabledView_Clears_RefusesMeanwhile_AndReplaysNothingOnceEnabled()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("disabled-view", _parent.transform);
            yield return PlayBoth(view, trace);
            var behaviour = (Behaviour)view;
            behaviour.enabled = false;
            float disabledAt = trace.Mark("view component disabled");
            bool sharedMeanwhile = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 4);
            bool personalMeanwhile = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 5);
            yield return trace.Frames(PlayingSeconds);
            behaviour.enabled = true;
            trace.Mark("view component enabled again");
            yield return trace.Frames(QuietSeconds);

            Assert.IsFalse(sharedMeanwhile || personalMeanwhile, $"a disabled view refuses requests ({sharedMeanwhile}, {personalMeanwhile})");
            AssertClearedAtOnce(trace.After(disabledAt), "disabling the view");
            LogAssert.NoUnexpectedReceived();
            yield return AcceptsAgain(view, PersonalKind, 5, "disabled-view/after");
        }

        // Requirement 5: the same when the scene-owned parent goes inactive and comes back.
        [UnityTest]
        public IEnumerator InactiveParent_Clears_RefusesMeanwhile_AndReplaysNothingOnceActive()
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("inactive-parent", _parent.transform);
            yield return PlayBoth(view, trace);
            _parent.SetActive(false);
            float inactiveAt = trace.Mark("parent inactive");
            bool sharedMeanwhile = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 4);
            bool personalMeanwhile = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 5);
            yield return trace.Frames(PlayingSeconds);
            _parent.SetActive(true);
            trace.Mark("parent active again");
            yield return trace.Frames(QuietSeconds);

            Assert.IsFalse(sharedMeanwhile || personalMeanwhile, $"an inactive view refuses requests ({sharedMeanwhile}, {personalMeanwhile})");
            AssertClearedAtOnce(trace.After(inactiveAt), "deactivating the parent");
            LogAssert.NoUnexpectedReceived();
            yield return AcceptsAgain(view, PersonalKind, 5, "inactive-parent/after");
        }

        // Requirement 5 and the BuildRuntime clause: everything two BuildRuntime calls make stays in the caller's scene under
        // its parent (no DontDestroyOnLoad). Destroying one root mid-effect leaves nothing of it in any scene and no late
        // callback, the other root keeps playing through its own window, and a new root plays on its own.
        [UnityTest]
        public IEnumerator DestroyedRoot_LeavesNothingBehind_WhileAnotherRootKeepsPlaying()
        {
            HashSet<GameObject> before = SceneObjects();
            Component doomed = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            Component other = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            Transform doomedRoot = RootOf(doomed);
            Transform otherRoot = RootOf(other);
            Assert.AreNotSame(doomedRoot, otherRoot, "each BuildRuntime call makes its own root");
            GameObject[] made = SceneObjects().Except(before).ToArray();
            CollectionAssert.IsEmpty(made.Where(item => item.scene != _scene || !item.transform.IsChildOf(_parent.transform)).Select(PathOf),
                "everything BuildRuntime made is in the caller's scene under its parent");
            var doomedTrace = new GuildGrowthEffectTrace("destroyed-root/doomed", doomedRoot);
            var otherTrace = new GuildGrowthEffectTrace("destroyed-root/other", otherRoot);
            bool shared = GuildGrowthEffectProduct.TryPlayConfirmed(doomed, SharedKind, 2);
            bool personal = GuildGrowthEffectProduct.TryPlayConfirmed(other, PersonalKind, 3);
            doomedTrace.Mark($"{SharedKind} 2 -> {shared}");
            float personalAt = otherTrace.Mark($"{PersonalKind} 3 -> {personal}");
            yield return GuildGrowthEffectTrace.Watch(PlayingSeconds, doomedTrace, otherTrace);
            Assert.IsTrue(shared && personal && doomedTrace.Samples.Last().Any && otherTrace.Samples.Last().Any,
                "both roots are playing before one is destroyed");

            GameObject[] doomedObjects = made.Where(item => item.transform.IsChildOf(doomedRoot)).ToArray();
            UnityEngine.Object.Destroy(doomedRoot.gameObject);
            float destroyedAt = otherTrace.Mark("first root destroyed");
            yield return GuildGrowthEffectTrace.Watch(QuietSeconds, otherTrace);

            Assert.IsTrue(doomedObjects.All(item => item == null), "every object of the destroyed root is gone");
            CollectionAssert.IsEmpty(SceneObjects().Except(before).Where(item => !item.transform.IsChildOf(otherRoot)).Select(PathOf),
                "nothing of the destroyed root survives in any scene");
            GuildGrowthEffectSample firstAfter = otherTrace.After(destroyedAt).FirstOrDefault();
            Assert.IsTrue(firstAfter != null && firstAfter.Any, "the other root is still drawn in the first frame after the destroy");
            LogAssert.NoUnexpectedReceived();
            yield return AcceptsAgain(GuildGrowthEffectProduct.BuildRuntime(_parent.transform), SharedKind, 4, "destroyed-root/new root");
            AssertLifetime(otherTrace.After(personalAt), personalAt, PersonalShortest, PersonalLongest, "the other root's personal enhancement",
                sample => sample.Any);
        }

        // Requirement 5: destroying the scene-owned parent mid-effect leaves no effect object in any loaded scene, the
        // DontDestroyOnLoad scene included, at any frame for longer than either effect lasts, and no late callback logs.
        [UnityTest]
        public IEnumerator DestroyedParent_LeavesNoObjectInAnyScene_AndNoLateCallback()
        {
            HashSet<GameObject> before = SceneObjects();
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("destroyed-parent", _parent.transform);
            yield return PlayBoth(view, trace);
            UnityEngine.Object.Destroy(_parent);
            _parent = null;
            trace.Mark("parent destroyed");
            var survivors = new HashSet<string>();
            float until = Time.realtimeSinceStartup + QuietSeconds;
            do
            {
                yield return null;
                survivors.UnionWith(SceneObjects().Except(before).Select(PathOf));
            }
            while (Time.realtimeSinceStartup < until);

            CollectionAssert.IsEmpty(survivors, "no effect object outlives its parent in any scene");
            LogAssert.NoUnexpectedReceived();
        }

        // Requirement 6 by behaviour: while both kinds play over a real uGUI button on an Overlay canvas at the real guild
        // window's order, every drawn effect graphic's on-screen centre raycasts to that button first and to nothing of the
        // effect, and the click the input module would send at the level text presses it. The settings the contract names
        // (no raycaster of the effect's own, raycastTarget off, CanvasGroup blocksRaycasts off) are checked after that.
        [UnityTest]
        public IEnumerator PlayingEffect_PassesPointerClicksToTheButtonBelow()
        {
            int guildOrder = 0;
            yield return ReadGuildCanvas((order, scaler) => guildOrder = order);
            int clicks = 0;
            Component button = GuildGrowthEffectScreen.ButtonBelow(guildOrder, () => clicks++);
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("pointer", _parent.transform);
            yield return PlayBoth(view, trace);

            GuildGrowthEffectGraphic[] drawn = GuildGrowthEffectScreen.Drawn(_parent.transform);
            Assert.IsNotEmpty(drawn, "the effect is drawn while the pointer is checked");
            var blocked = new List<string>();
            foreach (GuildGrowthEffectGraphic graphic in drawn)
            {
                Vector2 point = GuildGrowthEffectScreen.ScreenCentre(graphic);
                GameObject[] hits = ScreenUiProbe.RaycastHits(point);
                bool passes = hits.Length != 0 && hits[0] == button.gameObject && !hits.Any(hit => hit.transform.IsChildOf(_parent.transform));
                if (!passes) blocked.Add($"{graphic.Path} at {point}: [{string.Join(", ", hits.Select(hit => hit.name))}]");
            }
            CollectionAssert.IsEmpty(blocked, "at every drawn effect graphic the pointer reaches the button below first");
            GuildGrowthEffectGraphic level = drawn.FirstOrDefault(graphic => graphic.Numbers.Length != 0) ?? drawn[0];
            Vector2 clickPoint = GuildGrowthEffectScreen.ScreenCentre(level);
            GameObject top = ScreenUiProbe.RaycastHits(clickPoint).FirstOrDefault();
            Assert.IsNotNull(top, "something takes the pointer at " + level.Path);
            GameObject handler = GuildGrowthEffectScreen.Click(top, clickPoint);
            Assert.AreEqual(1, clicks, $"a click at {level.Path} {clickPoint} presses the button below once (handled by {(handler != null ? handler.name : "nothing")})");

            Transform root = RootOf(view);
            CollectionAssert.IsEmpty(GuildGrowthEffectScreen.Raycasters(root).Select(item => PathOf(item.gameObject)), "the effect has no raycaster of its own");
            CollectionAssert.IsEmpty(GuildGrowthEffectScreen.Graphics(root).Where(GuildGrowthEffectScreen.RaycastTarget).Select(item => PathOf(item.gameObject)),
                "every effect graphic has raycastTarget off");
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<CanvasGroup>(true).Where(group => group.blocksRaycasts).Select(group => PathOf(group.gameObject)),
                "every CanvasGroup of the effect has blocksRaycasts off");
        }

        // Requirement 6 premises: the effect draws on one root canvas of its own, a ScreenSpaceOverlay above the real guild
        // window's canvas and below dungeon rewards (the plan's order 13 candidate), with the guild window's own scaler
        // settings, and its level texts lie fully on the current screen. These read settings as premises only; drawing and
        // pointers are judged by the other cases and the two goal resolutions by the later capture.
        [UnityTest]
        public IEnumerator EffectCanvas_IsAnOverlayBetweenTheGuildWindowAndRewards_WithTheGuildScaler()
        {
            int guildOrder = 0;
            string guildScaler = null;
            yield return ReadGuildCanvas((order, scaler) =>
            {
                guildOrder = order;
                guildScaler = scaler;
            });
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace("canvas", _parent.transform);
            yield return PlayBoth(view, trace);

            GuildGrowthEffectGraphic[] drawn = GuildGrowthEffectScreen.Drawn(_parent.transform);
            Assert.IsNotEmpty(drawn, "the effect is drawn while its canvas is read");
            Canvas[] canvases = drawn.Select(graphic => graphic.Graphic.GetComponentInParent<Canvas>().rootCanvas).Distinct().ToArray();
            Assert.AreEqual(1, canvases.Length, "the effect draws on one root canvas: " + string.Join(", ", canvases.Select(item => item.name)));
            Canvas canvas = canvases[0];
            string scalerSettings = GuildGrowthEffectScreen.ScalerSettings(canvas);
            Debug.Log($"[GuildGrowthEffect] canvas {canvas.name} {canvas.renderMode} order {canvas.sortingOrder} scaler {scalerSettings}; " +
                $"guild order {guildOrder} scaler {guildScaler}; screen {Screen.width}x{Screen.height}");
            Assert.IsTrue(canvas.transform.IsChildOf(RootOf(view)), "the canvas is the effect's own, under its root");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "a ScreenSpaceOverlay canvas");
            Assert.Greater(canvas.sortingOrder, guildOrder, "above the guild window");
            Assert.Less(canvas.sortingOrder, RewardCanvasOrder, "below dungeon rewards");
            Assert.AreEqual(guildScaler, scalerSettings, "the guild window's canvas scaler settings");
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            string[] offScreen = trace.Samples.SelectMany(sample => sample.LevelTexts)
                .Where(text => !(screen.Contains(text.ScreenRect.min) && screen.Contains(text.ScreenRect.max)))
                .Select(text => text.ToString())
                .Distinct()
                .ToArray();
            CollectionAssert.IsEmpty(offScreen, "every drawn level text lies fully on the screen");
        }

        IEnumerator ShowsAloneInItsWindow(GuildChangeKind kind, int level, float shortest, float longest)
        {
            Component view = GuildGrowthEffectProduct.BuildRuntime(_parent.transform);
            var trace = new GuildGrowthEffectTrace($"alone/{kind}", _parent.transform);
            bool accepted = GuildGrowthEffectProduct.TryPlayConfirmed(view, kind, level);
            float requestedAt = trace.Mark($"TryPlayConfirmed({kind}, {level}) -> {accepted}");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);

            Assert.IsTrue(accepted, "a confirmed positive result of a growth kind is accepted");
            GuildGrowthEffectSample[] shown = trace.After(requestedAt);
            CollectionAssert.AreEqual(new long[] { level }, GuildGrowthEffectTrace.NumbersIn(shown), "the drawn level text shows the confirmed level and no other number");
            Assert.LessOrEqual(MostLevelTexts(shown), 1, "one level text per frame for one request");
            CollectionAssert.IsEmpty(_missingCharacters, "every drawn character exists in the font or its fallbacks");
            string[] imageKeys = shown.SelectMany(sample => sample.Drawn).Where(graphic => graphic.Image != null)
                .Select(graphic => graphic.Key).Distinct().ToArray();
            Assert.GreaterOrEqual(imageKeys.Length, 2, "an emblem and its light are drawn images, not only a text popup");
            Assert.IsTrue(imageKeys.Any(key => Animates(shown, key)), "a drawn image changes its alpha or size noticeably while shown");
            AssertLifetime(shown, requestedAt, shortest, longest, $"{kind} {level}", sample => sample.Any);
            Assert.IsTrue(Staged(shown, imageKeys), "the images come in stages, appearing or peaking at least " + StageGapSeconds + " s apart");
            AssertNothingLeftAfterTheEnd(shown, $"{kind} {level}");
        }

        IEnumerator PlayBoth(Component view, GuildGrowthEffectTrace trace)
        {
            bool shared = GuildGrowthEffectProduct.TryPlayConfirmed(view, SharedKind, 2);
            bool personal = GuildGrowthEffectProduct.TryPlayConfirmed(view, PersonalKind, 3);
            trace.Mark($"{SharedKind} 2 -> {shared}, {PersonalKind} 3 -> {personal}");
            yield return trace.Frames(PlayingSeconds);
            Assert.IsTrue(shared && personal && trace.Samples.Last().Any, "both kinds are accepted and drawn before the change under test");
        }

        // After a cancel, a disable or a destroy, a view still takes a new request and shows exactly its level.
        IEnumerator AcceptsAgain(Component view, GuildChangeKind kind, int level, string label)
        {
            var trace = new GuildGrowthEffectTrace(label, _parent.transform);
            bool accepted = GuildGrowthEffectProduct.TryPlayConfirmed(view, kind, level);
            float requestedAt = trace.Mark($"TryPlayConfirmed({kind}, {level}) -> {accepted}");
            yield return trace.UntilQuiet(AfterEndSeconds, WatchCapSeconds);
            Assert.IsTrue(accepted, $"{label}: a new {kind} {level} is accepted");
            CollectionAssert.AreEqual(new long[] { level }, GuildGrowthEffectTrace.NumbersIn(trace.After(requestedAt)), $"{label}: it shows {level} only");
        }

        // The real guild window's root canvas, built by GuildWindow.BuildRuntime under a holder of its own and destroyed
        // again, so the premises compare with the product's live order and scaler instead of a copied number.
        IEnumerator ReadGuildCanvas(Action<int, string> read)
        {
            var holder = new GameObject("GuildGrowthEffectTestGuildHolder");
            GuildWindow window = GuildWindow.BuildRuntime(holder.transform);
            Canvas canvas = window.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas, "fixture: the guild window has a canvas");
            read(canvas.rootCanvas.sortingOrder, GuildGrowthEffectScreen.ScalerSettings(canvas.rootCanvas));
            UnityEngine.Object.Destroy(holder);
            yield return null;
        }

        // The end lies between the last drawn frame and the first undrawn one after it, and the start between the request
        // and the first frame after it. Only a certain breach fails: gone before the shortest time even from the earliest
        // start, or still drawn past the longest time even from the latest start. Frames slower than half the target window
        // cannot bracket it, so the case is then inconclusive rather than passed or failed; the raw numbers are logged.
        static void AssertLifetime(GuildGrowthEffectSample[] samples, float requestedAt, float shortest, float longest, string what,
            Func<GuildGrowthEffectSample, bool> drawn)
        {
            GuildGrowthEffectSample first = samples.FirstOrDefault();
            Assert.IsNotNull(first, what + ": a frame follows the request");
            GuildGrowthEffectSample lastDrawn = samples.LastOrDefault(drawn);
            Assert.IsNotNull(lastDrawn, what + " is drawn");
            GuildGrowthEffectSample end = samples.FirstOrDefault(sample => sample.At > lastDrawn.At);
            Assert.IsNotNull(end, what + " ends inside the watch");
            float slowest = samples.Where(sample => sample.At <= end.At).Max(sample => sample.FrameDelta);
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[GuildGrowthEffect] lifetime '{0}': requested={1:F4} firstFrame={2:F4} " +
                "lastDrawn={3:F4} firstUndrawn={4:F4} -> shown more than {5:F4}s and at most {6:F4}s, target {7}-{8}s, slowest frame {9:F4}s",
                what, requestedAt, first.At, lastDrawn.At, end.At, lastDrawn.At - first.At, end.At - requestedAt, shortest, longest, slowest));
            if (slowest > (longest - shortest) / 2f)
            {
                Assert.Inconclusive($"{what}: a {slowest:F3} s frame is too slow to bracket the {shortest}-{longest} s target (raw lifetime line in the log)");
            }
            Assert.GreaterOrEqual(end.At - requestedAt, shortest, $"{what} is still drawn {shortest} s after the request");
            Assert.LessOrEqual(lastDrawn.At - first.At, longest, $"{what} is gone {longest} s after the first frame");
        }

        static void AssertClearedAtOnce(GuildGrowthEffectSample[] after, string change)
        {
            GuildGrowthEffectSample first = after.FirstOrDefault();
            Assert.IsNotNull(first, $"a frame follows {change}");
            Assert.IsFalse(first.Any, $"nothing is drawn in the first frame after {change}: " + Describe(first));
            AssertNothingDrawn(after, "after " + change);
        }

        static void AssertNothingDrawn(IEnumerable<GuildGrowthEffectSample> samples, string when)
        {
            GuildGrowthEffectSample drawn = samples.FirstOrDefault(sample => sample.Any);
            Assert.IsNull(drawn, drawn == null ? when : $"nothing is drawn {when}, but frame {drawn.Frame} drew {Describe(drawn)}");
        }

        void AssertNothingLeftAfterTheEnd(GuildGrowthEffectSample[] shown, string what)
        {
            Assert.IsEmpty(GuildGrowthEffectScreen.Drawn(_parent.transform), $"{what}: nothing is drawn once it ended");
            Vector2[] points = shown.SelectMany(sample => sample.Drawn).Select(GuildGrowthEffectScreen.ScreenCentre)
                .Select(point => new Vector2(Mathf.Round(point.x), Mathf.Round(point.y)))
                .Distinct()
                .Take(MaxBlockerPoints)
                .ToArray();
            string[] blockers = points.SelectMany(point => ScreenUiProbe.RaycastHits(point)
                    .Where(hit => hit.transform.IsChildOf(_parent.transform))
                    .Select(hit => $"{hit.name} at {point}"))
                .Distinct()
                .ToArray();
            CollectionAssert.IsEmpty(blockers, $"{what}: nothing of the ended effect takes the pointer where it was drawn");
        }

        // In every frame where both levels are drawn, no graphic of one kind's area overlaps a graphic of the other's, and
        // each area holds at least one drawn image besides its text (api-contract.md observation premise for kind areas).
        static void AssertSeparateAreas(IEnumerable<GuildGrowthEffectSample> samples, long sharedLevel, long personalLevel)
        {
            var overlaps = new List<string>();
            bool imagesInBoth = false;
            GuildGrowthEffectSample[] together = samples.Where(item => item.Numbers.Contains(sharedLevel) && item.Numbers.Contains(personalLevel)).ToArray();
            Assert.IsNotEmpty(together, $"{sharedLevel} and {personalLevel} are drawn in the same frame");
            foreach (GuildGrowthEffectSample sample in together)
            {
                (Transform sharedArea, Transform personalArea) = KindAreas(sample, sharedLevel, personalLevel);
                GuildGrowthEffectGraphic[] shared = sample.Drawn.Where(graphic => graphic.IsUnder(sharedArea)).ToArray();
                GuildGrowthEffectGraphic[] personal = sample.Drawn.Where(graphic => graphic.IsUnder(personalArea)).ToArray();
                imagesInBoth |= shared.Any(graphic => graphic.Image != null) && personal.Any(graphic => graphic.Image != null);
                foreach (GuildGrowthEffectGraphic a in shared)
                {
                    foreach (GuildGrowthEffectGraphic b in personal)
                    {
                        if (GuildGrowthEffectScreen.OverlapArea(a.ScreenRect, b.ScreenRect) > 0f) overlaps.Add($"frame {sample.Frame}: {a} over {b}");
                    }
                }
            }
            Assert.IsTrue(imagesInBoth, $"each kind's area holds its drawn images beside its level text ({sharedLevel} and {personalLevel})");
            CollectionAssert.IsEmpty(overlaps.Distinct().Take(MaxBlockerPoints), $"the two kinds' areas do not overlap ({sharedLevel} and {personalLevel})");
        }

        // Below the lowest common ancestor of the two kinds' level texts, each kind owns the child that holds its text.
        static (Transform, Transform) KindAreas(GuildGrowthEffectSample sample, long sharedLevel, long personalLevel)
        {
            GuildGrowthEffectGraphic[] sharedTexts = sample.LevelTexts.Where(text => text.Numbers.Contains(sharedLevel)).ToArray();
            GuildGrowthEffectGraphic[] personalTexts = sample.LevelTexts.Where(text => text.Numbers.Contains(personalLevel)).ToArray();
            Assert.AreEqual(1, sharedTexts.Length, $"one drawn text shows {sharedLevel} in frame {sample.Frame}");
            Assert.AreEqual(1, personalTexts.Length, $"one drawn text shows {personalLevel} in frame {sample.Frame}");
            Assert.AreNotEqual(sharedTexts[0].Key, personalTexts[0].Key, "each kind's level has a text of its own");
            Transform[] a = sharedTexts[0].Lineage;
            Transform[] b = personalTexts[0].Lineage;
            int common = Array.FindIndex(a, item => b.Any(other => ReferenceEquals(other, item)));
            Assert.AreNotEqual(0, common, "one level text is not inside the other");
            Transform sharedArea = common < 0 ? a[a.Length - 1] : a[common - 1];
            int personalCommon = common < 0 ? b.Length : Array.FindIndex(b, item => ReferenceEquals(item, a[common]));
            Assert.AreNotEqual(0, personalCommon, "one level text is not inside the other");
            Transform personalArea = b[personalCommon - 1];
            return (sharedArea, personalArea);
        }

        static bool Animates(GuildGrowthEffectSample[] samples, string key)
        {
            GuildGrowthEffectGraphic[] frames = samples.SelectMany(sample => sample.Drawn).Where(graphic => graphic.Key == key).ToArray();
            if (frames.Length < 2) return false;
            float alphaChange = frames.Max(graphic => graphic.Alpha) - frames.Min(graphic => graphic.Alpha);
            float sizeChange = Mathf.Sqrt(frames.Max(graphic => graphic.Area) / frames.Min(graphic => graphic.Area)) - 1f;
            return alphaChange >= NoticeableChange || sizeChange >= NoticeableChange;
        }

        // When each image first appears and first reaches its highest alpha; two of them must differ by StageGapSeconds.
        static bool Staged(GuildGrowthEffectSample[] samples, string[] keys)
        {
            var moments = new List<(string Key, float First, float Peak)>();
            foreach (string key in keys)
            {
                (float At, GuildGrowthEffectGraphic Graphic)[] frames = samples
                    .Select(sample => (sample.At, sample.Drawn.FirstOrDefault(graphic => graphic.Key == key)))
                    .Where(frame => frame.Item2 != null)
                    .ToArray();
                float peak = frames.OrderByDescending(frame => frame.Graphic.Alpha).ThenBy(frame => frame.At).First().At;
                moments.Add((key, frames[0].At, peak));
            }
            Debug.Log("[GuildGrowthEffect] stages " + string.Join("; ", moments.Select(moment =>
                string.Format(CultureInfo.InvariantCulture, "{0} first={1:F4} peak={2:F4}", moment.Key, moment.First, moment.Peak))));
            for (int i = 0; i < moments.Count; i++)
            {
                for (int j = i + 1; j < moments.Count; j++)
                {
                    if (Mathf.Abs(moments[i].First - moments[j].First) >= StageGapSeconds) return true;
                    if (Mathf.Abs(moments[i].Peak - moments[j].Peak) >= StageGapSeconds) return true;
                }
            }
            return false;
        }

        static int MostLevelTexts(IEnumerable<GuildGrowthEffectSample> samples) =>
            samples.Select(sample => sample.LevelTexts.Length).DefaultIfEmpty(0).Max();

        static string[] ImagesIn(IEnumerable<GuildGrowthEffectSample> samples) =>
            samples.SelectMany(sample => sample.Drawn).Select(graphic => graphic.Image).Where(image => image != null).Distinct().ToArray();

        static float LargestScreenArea(IEnumerable<GuildGrowthEffectSample> samples) =>
            samples.Select(sample => GuildGrowthEffectScreen.ScreenArea(sample.Drawn)).DefaultIfEmpty(0f).Max();

        static string Describe(GuildGrowthEffectSample sample) => string.Join("; ", sample.Drawn.Select(graphic => graphic.ToString()));

        // Every GameObject of every loaded scene, hidden ones and the DontDestroyOnLoad scene included.
        static HashSet<GameObject> SceneObjects() => new HashSet<GameObject>(
            Resources.FindObjectsOfTypeAll<GameObject>().Where(item => item.scene.IsValid()));

        static string PathOf(GameObject item)
        {
            var names = new List<string>();
            for (Transform current = item.transform; current != null; current = current.parent) names.Insert(0, current.name);
            return item.scene.name + ":" + string.Join("/", names);
        }

        Transform RootOf(Component view)
        {
            Transform parent = _parent.transform;
            Assert.IsTrue(view.transform != parent && view.transform.IsChildOf(parent), "the view is under the given parent");
            Transform root = view.transform;
            while (root.parent != parent) root = root.parent;
            return root;
        }

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            // The event is global; only texts under this test's parent count.
            if (_parent == null || textComponent == null || !textComponent.transform.IsChildOf(_parent.transform)) return;
            _missingCharacters.Add(string.Format(CultureInfo.InvariantCulture, "U+{0:X4} at {1} in {2} '{3}' ({4})", unicode,
                stringIndex, textComponent.name, text, fontAsset != null ? fontAsset.name : "no font"));
        }
    }
}
