using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    public sealed class MapEntrySceneLifecycleTests
    {
        MapEntryPlayFixture _fixture;
        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator PacketBeforePlayer_RealTownWaitsForHpThenAcceptsKeyboardInput() => InitialEntry(false);

        [UnityTest]
        public IEnumerator PlayerBeforePacket_RealTownWaitsForHpThenAcceptsKeyboardInput() => InitialEntry(true);

        IEnumerator InitialEntry(bool sceneFirst)
        {
            yield return _fixture.EnterScriptedTown(sceneFirst);
            var player = _fixture.Player;
            Vector3 before = player.transform.position;
            _fixture.Keys(Key.D, Key.Enter, Key.Q);
            yield return _fixture.Frames(8);
            Assert.AreEqual(MapEntryState.Entering, _fixture.Session.Entry.State);
            Assert.AreEqual(before, player.transform.position, "waiting entry cannot predict movement");
            Assert.AreEqual(0, _fixture.Peer.Count(PacketID.C_MoveIntent));
            Assert.AreEqual(0, _fixture.Peer.Count(PacketID.C_Attack));
            Assert.AreEqual(0, _fixture.Peer.Count(PacketID.C_SkillUse));
            Assert.IsTrue(player.CanAttack);
            Assert.IsTrue(player.CanUseDash);
            _fixture.Keys();
            yield return _fixture.Frames(2);
            _fixture.Hp(29, 110);
            yield return _fixture.WaitMap(0);
            Assert.AreEqual(700, _fixture.Session.LocalEntityId);
            Assert.AreEqual(2, _fixture.Session.Entry.SpawnX);
            Assert.AreEqual(0, _fixture.Session.Entry.SpawnY);
            yield return _fixture.AssertHud();
            float x = player.transform.position.x;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Keyboard.dKey.isPressed, "synthetic keyboard D state", 3);
            var playerInput = player.GetComponent<PlayerInput>();
            yield return MapEntryPlayFixture.Wait(() => playerInput.actions["Move"].ReadValue<Vector2>().x > 0.5f, "PlayerInput Move action", 3);
            yield return MapEntryPlayFixture.Wait(() => MapEntryPlayFixture.Field<sbyte>(player, "_currentMoveX") == 1, "movement input callback", 3);
            yield return MapEntryPlayFixture.Wait(() =>
            {
                var move = _fixture.Peer.Last<C_MoveIntent>(PacketID.C_MoveIntent);
                return move != null && InputBits.Decode(move.input).inputX == 1;
            }, "rightward intent through real TCP", 3);
            yield return MapEntryPlayFixture.Wait(() => player.transform.position.x > x + 0.2f, "actual keyboard movement");
            _fixture.Keys(Key.Enter);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == 1, "actual keyboard attack");
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => player.CanAttack && !player.IsActionLocked, "attack cooldown");
            _fixture.Keys(Key.Q);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_SkillUse) == 1, "actual keyboard skill");
            _fixture.Keys();
            Assert.IsFalse(player.CanUseDash, "accepted input commits the normal cooldown");
            Assert.Greater(_fixture.Peer.Count(PacketID.C_MoveIntent), 0);
        }

        [UnityTest]
        public IEnumerator BufferedRosterSpawnChangeDeath_AndTownHuntingGroundRoundTrip_KeepAuthority()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Peer.Send(new S_PlayerJoin { entityId = 701, characterClass = 0, spawnX = 5, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_Snapshot { entityId = 701, serverTick = 2, x = 6, y = 0 }.Write());
            _fixture.Peer.Send(new S_PlayerLeave { entityId = 701 }.Write());
            _fixture.Peer.Send(new S_PlayerJoin { entityId = 702, characterClass = 1, spawnX = 8, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_Snapshot { entityId = 702, serverTick = 4, x = 9, y = 0 }.Write());
            foreach (int id in new[] { 800, 801 })
            {
                _fixture.Peer.Send(new S_EntitySpawn { entityId = id, entityKind = 0, x = 10, y = 0, currentHp = 100, maxHp = 100 }.Write());
                _fixture.Peer.Send(new S_EntityState { entityId = id, serverTick = 3, x = 11, y = 0 }.Write());
                _fixture.Peer.Send(new S_HitResult { attackerEntityId = 700, targetEntityId = id, damage = 75, currentHp = 25, maxHp = 100 }.Write());
            }
            _fixture.Peer.Send(new S_EntityDeath { entityId = 800 }.Write());
            yield return _fixture.Frames(6);
            Assert.IsFalse(RemoteEntityRegistry.Instance.TryGetTransform(701, out _));
            Assert.IsFalse(RemoteEntityRegistry.Instance.TryGetTransform(702, out _));
            Assert.IsFalse(EnemyRegistry.Instance.TryGetTransform(801, out _));
            _fixture.Hp(45, 100);
            yield return _fixture.WaitMap(0);
            Assert.IsFalse(RemoteEntityRegistry.Instance.TryGetTransform(701, out _), "leave after spawn must remain removed");
            Assert.IsTrue(RemoteEntityRegistry.Instance.TryGetTransform(702, out Transform remote), "positive roster control");
            Assert.IsFalse(EnemyRegistry.Instance.TryGetTransform(800, out _), "death after buffered spawn cannot be lost");
            Assert.IsTrue(EnemyRegistry.Instance.TryGetTransform(801, out Transform survivor));
            Assert.AreEqual(25, survivor.GetComponent<RemoteEnemy>().CurrentHp, "hit after buffered spawn must apply");
            yield return MapEntryPlayFixture.Wait(() => Mathf.Abs(remote.position.x - 9) < 0.01f &&
                Mathf.Abs(survivor.position.x - 11) < 0.01f, "remote Snapshot and enemy EntityState coordinates");
            Assert.AreEqual(0, remote.position.y, 0.01f);
            Assert.AreEqual(survivor.GetComponent<RemoteEnemy>().VisualFootOffset, survivor.position.y, 0.01f);
            ulong townHandle = _fixture.Session.Entry.SceneHandle;
            long townEpoch = _fixture.Session.Entry.Epoch;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady,
                "HuntingGround scene before fresh HP");
            Assert.IsFalse(_fixture.Session.CanSendGameplay, "old Town HP cannot release new map");
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            Assert.Greater(_fixture.Session.Entry.Epoch, townEpoch);
            Assert.AreNotEqual(townHandle, _fixture.Session.Entry.SceneHandle);
            Assert.AreEqual(700, _fixture.Session.LocalEntityId);
            Assert.IsFalse(RemoteEntityRegistry.Instance.TryGetTransform(702, out _));
            yield return _fixture.AssertHud();
            _fixture.Peer.Send(new S_MapTransition { destMapId = 0, spawnX = 17, spawnY = 0 }.Write());
            _fixture.Hp(28, 100);
            yield return _fixture.WaitMap(0);
            Assert.AreEqual(17, _fixture.Session.Entry.SpawnX);
            Assert.AreEqual(28, _fixture.Session.Entry.CurrentHp);
            Assert.AreEqual(700, _fixture.Session.LocalEntityId);
            yield return _fixture.AssertHud();
            AssertOverlayReleased();
        }

        [UnityTest]
        public IEnumerator MissingAdditiveUi_LogsFailureButDoesNotBlockGameplay_AndLaterHudBindsLatestHp()
        {
            yield return _fixture.EnterScriptedTown();
            yield return MapEntryPlayFixture.Wait(() => SceneManager.GetSceneByName("UI").isLoaded, "initial UI load");
            AsyncOperation unload = SceneManager.UnloadSceneAsync("UI");
            yield return MapEntryPlayFixture.Wait(() => unload.isDone && HudController.Instance == null, "remove runtime HUD");
            var errors = new List<string>();
            void Capture(string message, string _, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); }
            var go = new GameObject("Test missing additive UI");
            go.SetActive(false);
            var bootstrap = go.AddComponent<SceneBootstrap>();
            MapEntryPlayFixture.Set(bootstrap, "_uiSceneName", "EntryTest_MissingUI");
            bool previousLogPolicy = LogAssert.ignoreFailingMessages;
            Application.logMessageReceived += Capture;
            try
            {
                LogAssert.ignoreFailingMessages = true; // only this intentional failure window; errors are asserted below
                go.SetActive(true);
                yield return _fixture.Frames(2);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousLogPolicy;
                Application.logMessageReceived -= Capture;
                UnityEngine.Object.Destroy(go);
            }
            Assert.IsTrue(errors.Any(message => message.Contains("EntryTest_MissingUI") &&
                message.IndexOf("gameplay continues", StringComparison.OrdinalIgnoreCase) >= 0), string.Join("\n", errors));
            Assert.IsTrue(errors.All(message => message.Contains("EntryTest_MissingUI")), string.Join("\n", errors));
            _fixture.Hp(21, 120);
            yield return _fixture.WaitMap(0);
            Assert.IsNull(HudController.Instance);
            _fixture.Hp(33, 120);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 33, "latest HP without UI");
            AsyncOperation load = SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "late additive UI");
            yield return _fixture.AssertHud();
        }

        [UnityTest]
        public IEnumerator SameSceneNewEntry_UsesNewSceneIdentity_ThenEndingHasNoPlayerOrHpBarrier()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            ulong previousHandle = _fixture.Session.Entry.SceneHandle;
            long previousEpoch = _fixture.Session.Entry.Epoch;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 0, spawnX = 6, spawnY = 0 }.Write());
            _fixture.Hp(17, 100);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.Epoch != previousEpoch, "new Town epoch");
            yield return _fixture.WaitMap(0);
            Assert.AreNotEqual(previousHandle, _fixture.Session.Entry.SceneHandle);
            Assert.AreEqual(6, _fixture.Session.Entry.SpawnX);
            Assert.AreEqual(17, _fixture.Session.Entry.CurrentHp);
            _fixture.Peer.Send(new S_MapTransition { destMapId = 3, spawnX = 0, spawnY = 0 }.Write());
            yield return _fixture.WaitMap(3);
            Assert.IsNull(LocalPlayerMovement.Instance);
            Assert.IsFalse(_fixture.Session.Entry.HasHp);
            Assert.IsFalse(_fixture.Session.CanSendGameplay);
            AssertOverlayReleased();
        }

        [UnityTest]
        public IEnumerator RequestsDuringFade_OnlyLatestDestinationCompletes_AndOverlayUnlocks()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            MapEntryPlayFixture.Set(_fixture.Loader, "_fadeDuration", 0.15f);
            var results = new List<SceneLoadResult>();
            var a = _fixture.Loader.RequestScene("HuntingGround", results.Add);
            var b = _fixture.Loader.RequestScene("Town", results.Add);
            var c = _fixture.Loader.RequestScene("Ending", results.Add);
            yield return MapEntryPlayFixture.Wait(() => c.IsFinished && !_fixture.Loader.IsTransitioning, "latest destination");
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(SceneLoadStatus.Superseded, results.Single(r => r.RequestId == a.Id).Status);
            Assert.AreEqual(SceneLoadStatus.Superseded, results.Single(r => r.RequestId == b.Id).Status);
            Assert.AreEqual(SceneLoadStatus.Completed, results.Single(r => r.RequestId == c.Id).Status);
            Assert.AreEqual("Ending", SceneManager.GetActiveScene().name);
            AssertOverlayReleased();
        }

        [UnityTest]
        public IEnumerator AlreadyLoadedPhysicalA_BThenCBeforeCompletion_OnlyCAppliesDomainSuccess()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            var results = new List<SceneLoadResult>();
            SceneLoadRequest b = null, c = null;
            var physicallyLoadedScenes = new List<string>();
            bool observedPhysicalA = false;
            void OnLoaded(Scene scene, LoadSceneMode _)
            {
                physicallyLoadedScenes.Add(scene.name);
                if (scene.name != "HuntingGround") return;
                observedPhysicalA = true;
                // sceneLoaded runs before the coroutine consumes this operation's
                // completion. Thus A really loaded; this is not only a fade race.
                b = _fixture.Loader.RequestScene("Town", results.Add);
                c = _fixture.Loader.RequestScene("Ending", results.Add);
            }
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                var a = _fixture.Loader.RequestScene("HuntingGround", results.Add);
                yield return MapEntryPlayFixture.Wait(() => c != null && c.IsFinished && !_fixture.Loader.IsTransitioning,
                    "physical A followed by latest C");
                Assert.IsTrue(observedPhysicalA);
                Assert.AreEqual(3, results.Count);
                Assert.AreEqual(SceneLoadStatus.Superseded, results.Single(r => r.RequestId == a.Id).Status);
                Assert.AreEqual(SceneLoadStatus.Superseded, results.Single(r => r.RequestId == b.Id).Status);
                Assert.AreEqual(SceneLoadStatus.Completed, results.Single(r => r.RequestId == c.Id).Status);
                CollectionAssert.Contains(physicallyLoadedScenes, "HuntingGround");
                CollectionAssert.Contains(physicallyLoadedScenes, "Ending");
                CollectionAssert.DoesNotContain(physicallyLoadedScenes, "Town", "superseded pending B must never physically load");
                Assert.AreEqual("Ending", SceneManager.GetActiveScene().name);
                AssertOverlayReleased();
            }
            finally { SceneManager.sceneLoaded -= OnLoaded; }
        }

        [UnityTest]
        public IEnumerator RemoteTeleport_RealSkillHandlerCallback_IsDiscardedAcrossEntry_AndNewEntryStillArrives()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Peer.Send(new S_PlayerJoin { entityId = 710, characterClass = 1, spawnX = 4, spawnY = 0 }.Write());
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(710, out _), "remote mage");
            RemoteEntityRegistry.Instance.TryGetTransform(710, out Transform oldTransform);
            var oldRemote = oldTransform.GetComponent<RemoteEntity>();
            _fixture.Peer.Send(new S_SkillCast { casterEntityId = 710, skillId = (byte)SkillId.Teleport, facing = 1 }.Write());
            yield return MapEntryPlayFixture.Wait(() => MapEntryPlayFixture.Field<Action>(oldRemote, "_teleportArriveCallback") != null,
                "actual remote SkillCast callback registration");
            // Retain the actual callback as a delayed work item; never replace the
            // production callback or readiness state with test-owned values.
            Action staleArrival = MapEntryPlayFixture.Field<Action>(oldRemote, "_teleportArriveCallback");
            UnityClientSession sameSession = _fixture.Session;
            long previousEpoch = sameSession.Entry.Epoch;
            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_PlayerJoin { entityId = 710, characterClass = 1, spawnX = 8, spawnY = 0 }.Write());
            _fixture.Hp();
            yield return _fixture.WaitMap(1);
            Assert.AreSame(sameSession, _fixture.Session);
            Assert.Greater(sameSession.Entry.Epoch, previousEpoch);
            Assert.IsTrue(oldTransform == null, "old registry-owned object is destroyed with the entry");
            Assert.IsTrue(RemoteEntityRegistry.Instance.TryGetTransform(710, out Transform current));
            Assert.IsNull(current.Find("TeleportArrive(Clone)"));
            staleArrival();
            Assert.IsNull(current.Find("TeleportArrive(Clone)"), "old callback cannot attach an effect to the reused remote ID");
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<Transform>().Any(t => t.name == "TeleportArrive(Clone)"));
            var remote = current.GetComponent<RemoteEntity>();
            _fixture.Peer.Send(new S_SkillCast { casterEntityId = 710, skillId = (byte)SkillId.Teleport, facing = 1 }.Write());
            yield return MapEntryPlayFixture.Wait(() => MapEntryPlayFixture.Field<Action>(remote, "_teleportArriveCallback") != null,
                "new entry callback registration");
            _fixture.Peer.Send(new S_Snapshot { entityId = 710, serverTick = 9, x = 12, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => current.Find("TeleportArrive(Clone)") != null, "normal remote arrival VFX");
            Assert.IsNull(MapEntryPlayFixture.Field<Action>(remote, "_teleportArriveCallback"), "arrival callback is consumed once");
            yield return MapEntryPlayFixture.Wait(() => Mathf.Abs(current.position.x - 12) < 0.01f, "remote teleport position");
        }

        [UnityTest]
        public IEnumerator DisconnectOnActualSceneLoaded_LatePhysicalCompletionCannotBindClosedEntry()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            UnityClientSession old = _fixture.Session;
            bool sawPhysicalDestination = false;
            void OnLoaded(Scene scene, LoadSceneMode _)
            {
                if (scene.name != "HuntingGround") return;
                sawPhysicalDestination = true;
                NetworkService.Instance.Disconnect();
            }
            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 12, spawnY = 0 }.Write());
                _fixture.Hp();
                yield return MapEntryPlayFixture.Wait(() => sawPhysicalDestination && !_fixture.Loader.IsTransitioning, "physical completion after disconnect");
                Assert.AreEqual(MapEntryState.Closed, old.Entry.State);
                Assert.IsFalse(old.Entry.SpawnApplied);
                Assert.IsFalse(old.CanSendGameplay);
                Assert.IsNull(UnityClientSession.Instance);
                Assert.IsTrue(SceneManager.GetSceneByName("HuntingGround").isLoaded, "physical loading was not rolled back");
                AssertOverlayReleased();
            }
            finally { SceneManager.sceneLoaded -= OnLoaded; }
        }

        [UnityTest]
        public IEnumerator PhysicalLoaderFailure_DisconnectsEntry_AndRestoresFadeAndRaycast()
        {
            yield return _fixture.EnterScriptedTown();
            var session = _fixture.Session;
            long epoch = session.Entry.Epoch;
            var errors = new List<string>();
            void Capture(string message, string _, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); }
            bool previousLogPolicy = LogAssert.ignoreFailingMessages;
            SceneLoadResult result = null;
            Application.logMessageReceived += Capture;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                // Invalid scene is runtime-only. Feed its actual adapter result through
                // the session's normal completion boundary; no BuildSettings edit.
                _fixture.Loader.RequestScene("EntryTest_MissingGameplay", completed =>
                {
                    result = completed;
                    typeof(UnityClientSession).GetMethod("OnEntrySceneLoaded", MapEntryPlayFixture.Private)
                        .Invoke(session, new object[] { epoch, completed });
                });
                yield return MapEntryPlayFixture.Wait(() => result != null && !_fixture.Loader.IsTransitioning, "failed scene cleanup");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousLogPolicy;
                Application.logMessageReceived -= Capture;
            }
            Assert.AreEqual(SceneLoadStatus.Failed, result.Status);
            Assert.IsNotNull(result.Error);
            Assert.IsTrue(session.IsClosed);
            Assert.IsFalse(session.CanSendGameplay);
            Assert.IsTrue(errors.Any(message => message.Contains("Map entry failed; disconnecting")));
            Assert.IsTrue(errors.All(message => message.Contains("EntryTest_MissingGameplay") || message.Contains("Map entry failed")), string.Join("\n", errors));
            AssertOverlayReleased();
        }

        // Independent check of the fixture's InputSettings save/restore across repeated tests. Expected values are the
        // global settings captured before the first Prepare, not anything the fixture computes.
        [UnityTest]
        public IEnumerator RepeatedFixtureCycles_RestoreGlobalInputSettingsValues_WithoutGrowingSettingsObjects()
        {
            const int cycles = 3;
            Assert.IsTrue(InputSystem.settings != null, "precondition: Input System has live global settings");
            string originalValues = JsonUtility.ToJson(InputSystem.settings);
            HideFlags originalHideFlags = InputSystem.settings.hideFlags;
            int originalObjectCount = Resources.FindObjectsOfTypeAll<InputSettings>().Length;
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                InputSettings beforePrepare = InputSystem.settings;
                HideFlags beforePrepareFlags = beforePrepare.hideFlags;
                _fixture = new MapEntryPlayFixture();
                yield return _fixture.Prepare(false);
                Assert.AreNotEqual(originalValues, JsonUtility.ToJson(InputSystem.settings), $"cycle {cycle}: fixture must swap in its runtime settings");
                // Diagnostic only: whether the replaced settings object survives depends on the Input System version.
                Debug.Log($"[InputSettings cycle] cycle={cycle} hideFlags={beforePrepareFlags} destroyedByPrepare={beforePrepare == null}");
                MapEntryPlayFixture prepared = _fixture;
                _fixture = new MapEntryPlayFixture(); // TearDown then has nothing left to clean.
                yield return prepared.Cleanup();
                yield return null; // Object.Destroy completes at the end of the frame.
                Assert.IsTrue(InputSystem.settings != null, $"cycle {cycle}: global settings must stay alive");
                Assert.AreEqual(originalValues, JsonUtility.ToJson(InputSystem.settings), $"cycle {cycle}: settings values must be restored");
                Assert.AreEqual(originalHideFlags, InputSystem.settings.hideFlags, $"cycle {cycle}: ownership flags must be restored");
                Assert.AreEqual(originalObjectCount, Resources.FindObjectsOfTypeAll<InputSettings>().Length, $"cycle {cycle}: InputSettings objects must not grow");
            }
        }

        // The other restore branch: an original the Input System does not treat as temporary (like a project
        // InputSettings asset) survives the swap, so the fixture must restore that object itself and destroy both copies.
        [UnityTest]
        public IEnumerator SurvivingOriginalSettings_IsRestoredAsItself_AndBothFixtureCopiesAreDestroyed()
        {
            InputSettings previousGlobal = InputSystem.settings;
            Assert.IsTrue(previousGlobal != null, "precondition: Input System has live global settings");
            int otherObjectsBefore = Resources.FindObjectsOfTypeAll<InputSettings>().Count(item => item != previousGlobal);
            var persistentLike = UnityEngine.Object.Instantiate(previousGlobal);
            // Any value other than HideAndDontSave is kept by the 1.20.0 setter; DontUnloadUnusedAsset keeps it loaded.
            persistentLike.hideFlags = HideFlags.DontUnloadUnusedAsset;
            InputSystem.settings = persistentLike;
            string persistentValues = JsonUtility.ToJson(persistentLike);

            yield return _fixture.Prepare(false);
            Assert.AreNotSame(persistentLike, InputSystem.settings, "precondition: fixture must swap in its runtime settings");
            MapEntryPlayFixture prepared = _fixture;
            _fixture = new MapEntryPlayFixture(); // TearDown then has nothing left to clean.
            yield return prepared.Cleanup();
            yield return null; // Object.Destroy completes at the end of the frame.

            Assert.AreSame(persistentLike, InputSystem.settings, "a surviving original must be restored as itself");
            Assert.AreEqual(persistentValues, JsonUtility.ToJson(persistentLike), "the original's values must stay untouched");
            int otherObjectsAfter = Resources.FindObjectsOfTypeAll<InputSettings>().Count(item => item != persistentLike && item != previousGlobal);
            Assert.AreEqual(otherObjectsBefore, otherObjectsAfter, "both fixture copies must be destroyed");

            // Hand the Input System a temporary settings object again, as the project has no InputSettings asset.
            var temporary = UnityEngine.Object.Instantiate(persistentLike);
            temporary.hideFlags = HideFlags.HideAndDontSave;
            InputSystem.settings = temporary;
            UnityEngine.Object.Destroy(persistentLike);
            if (previousGlobal != null) UnityEngine.Object.Destroy(previousGlobal);
        }

        void AssertOverlayReleased()
        {
            Assert.IsFalse(_fixture.Loader.IsTransitioning);
            var fade = MapEntryPlayFixture.Field<CanvasGroup>(_fixture.Loader, "_fadeGroup");
            Assert.IsNotNull(fade, "real prefab fade overlay must participate");
            Assert.AreEqual(0, fade.alpha);
            Assert.IsFalse(fade.blocksRaycasts);
        }
    }
}
