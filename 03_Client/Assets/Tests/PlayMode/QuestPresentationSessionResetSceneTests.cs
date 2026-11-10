using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Goal 2026-11-10-code-audit-client-fixes item 4: the three once-per-session presentation values last one connection.
    // Judgement uses public behaviour only (Install, Run, the live HUD's CanvasGroup). Reflection is limited to returning
    // the three static values afterwards (user-approved answer 4), so this file cannot leak state into other tests.
    public sealed class QuestPresentationSessionResetSceneTests
    {
        const string WatcherName = "_QuestCompleteWatcher";
        const string SequencerName = "_QuestIntroSequencer";
        const int TargetCount = 3;

        static readonly (Type Owner, string Field)[] s_presentationValues =
        {
            (typeof(QuestCompleteWatcher), "_shown"),
            (typeof(QuestIntroSequencer), "_introShown"),
            (typeof(QuestProgressHud), "_revealed"),
        };

        readonly List<GameObject> _parents = new();
        MapEntryPlayFixture _fixture;
        bool[] _valuesBefore;
        float _oldVolume;
        float _oldScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _fixture = new MapEntryPlayFixture();
            yield return MapEntryPlayFixture.Wait(() => QuestState.Instance != null && NetworkService.Instance != null, "persistent services");
            _valuesBefore = new bool[s_presentationValues.Length];
            for (int i = 0; i < s_presentationValues.Length; i++)
                _valuesBefore[i] = (bool)PresentationField(i).GetValue(null);
            _oldVolume = AudioListener.volume;
            _oldScale = Time.timeScale;
            AudioListener.volume = 0f;
            QuestState.Instance.ResetSession();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                foreach (GameObject parent in _parents)
                    if (parent != null) UnityEngine.Object.Destroy(parent);
                _parents.Clear();
                yield return _fixture.Cleanup();
            }
            finally
            {
                if (QuestState.Instance != null) QuestState.Instance.ResetSession();
                AudioListener.volume = _oldVolume;
                Time.timeScale = _oldScale;
                for (int i = 0; i < s_presentationValues.Length; i++)
                    PresentationField(i).SetValue(null, _valuesBefore[i]);
            }
            for (int i = 0; i < s_presentationValues.Length; i++)
                Assert.AreEqual(_valuesBefore[i], (bool)PresentationField(i).GetValue(null),
                    "cleanup: " + s_presentationValues[i].Field + " returned to its value before this test");
        }

        static FieldInfo PresentationField(int index)
        {
            (Type owner, string name) = s_presentationValues[index];
            FieldInfo field = owner.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "cleanup needs " + owner.Name + "." + name);
            return field;
        }

        Transform NewParent(string name)
        {
            var parent = new GameObject(name);
            _parents.Add(parent);
            return parent.transform;
        }

        static bool HasChild(Transform parent, string name) => parent.Find(name) != null;
        static bool HasAlert(Transform parent) => parent.GetComponentInChildren<QuestAlert>(true) != null;

        // Public path to the defect state: a completed quest makes the watcher play its one presentation.
        Transform SeeCompletion()
        {
            Transform first = NewParent("Quest completion first");
            QuestState.Instance.ApplyUpdate(0, TargetCount);
            QuestCompleteWatcher.Install(first);
            Assert.IsTrue(HasChild(first, WatcherName), "fixture: the first Install of this test creates a watcher");
            QuestState.Instance.ApplyUpdate(TargetCount, TargetCount);
            Assert.IsTrue(HasAlert(first), "fixture: the completion presentation played");
            return first;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator AfterSessionReset_CompleteWatcherInstallsAgain()
        {
            SeeCompletion();
            Transform beforeReset = NewParent("Quest completion before reset");
            QuestCompleteWatcher.Install(beforeReset);
            Assert.IsFalse(HasChild(beforeReset, WatcherName), "fixture: without a reset the presentation is already spent");

            NetworkService.ResetGlobalSessionMirrors();
            yield return null;

            Transform afterReset = NewParent("Quest completion after reset");
            QuestCompleteWatcher.Install(afterReset);
            bool watcherInstalled = HasChild(afterReset, WatcherName);
            Assert.IsTrue(watcherInstalled, "item 4: a new session's completion can be presented again");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator WithoutSessionReset_CompleteWatcherDoesNotInstallAgain()
        {
            SeeCompletion();
            yield return null;
            Transform revisit = NewParent("Quest completion map round trip");
            QuestCompleteWatcher.Install(revisit);
            bool watcherInstalled = HasChild(revisit, WatcherName);
            Assert.IsFalse(watcherInstalled, "item 4: the same session's map round trip does not replay the presentation");
        }

        IEnumerator EnterTownAndSeeIntro()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => QuestProgressHud.Instance != null, "the UI scene's quest HUD", 10);
            CanvasGroup firstHudGroup = QuestProgressHud.Instance.GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, firstHudGroup.alpha, "fixture: the HUD starts hidden in this session");

            Transform first = NewParent("Quest intro first");
            QuestIntroSequencer.Run(first);
            yield return MapEntryPlayFixture.Wait(() => HasAlert(first), "fixture: the first intro plays its alert", 10);
            yield return MapEntryPlayFixture.Wait(() => !HasChild(first, SequencerName), "fixture: the first intro finishes", 20);
            yield return MapEntryPlayFixture.Wait(() => firstHudGroup.alpha >= 0.99f, "fixture: the first intro reveals the HUD", 5);
        }

        IEnumerator ResetSessionAndReloadUi()
        {
            NetworkService.ResetGlobalSessionMirrors();
            Scene ui = QuestProgressHud.Instance.gameObject.scene;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(ui);
            yield return MapEntryPlayFixture.Wait(() => unload.isDone && QuestProgressHud.Instance == null, "UI scene unload", 10);
            AsyncOperation load = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone && QuestProgressHud.Instance != null, "UI scene reload", 10);
            yield return _fixture.Frames(2);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AfterSessionReset_NewHudStartsHidden()
        {
            yield return EnterTownAndSeeIntro();
            yield return ResetSessionAndReloadUi();
            float hudAlpha = QuestProgressHud.Instance.GetComponent<CanvasGroup>().alpha;
            Assert.AreEqual(0f, hudAlpha, "item 4: a new session's HUD starts hidden");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AfterSessionReset_IntroPlaysItsAlertAgain()
        {
            yield return EnterTownAndSeeIntro();
            yield return ResetSessionAndReloadUi();

            Transform second = NewParent("Quest intro after reset");
            QuestIntroSequencer.Run(second);
            float deadline = Time.realtimeSinceStartup + 8;
            while (!HasAlert(second) && Time.realtimeSinceStartup < deadline) yield return null;
            bool alertPlayed = HasAlert(second);
            Assert.IsTrue(alertPlayed, "item 4: a new session's intro plays its alert within 8 s");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator WithoutSessionReset_IntroSkipsItsAlert_AndKeepsTheHud()
        {
            yield return EnterTownAndSeeIntro();
            Transform revisit = NewParent("Quest intro map round trip");
            QuestIntroSequencer.Run(revisit);
            yield return MapEntryPlayFixture.Wait(() => !HasChild(revisit, SequencerName), "the repeated intro finishes", 8);
            bool alertPlayed = HasAlert(revisit);
            float hudAlpha = QuestProgressHud.Instance.GetComponent<CanvasGroup>().alpha;
            Assert.IsFalse(alertPlayed, "item 4: the same session's map round trip plays no second alert");
            Assert.AreEqual(1f, hudAlpha, "item 4: the same session keeps the HUD revealed");
        }
    }
}
