using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using Dawnholder.Client.Gameplay;
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
    // The pause menu's ownership of Time.timeScale and of new gameplay input, and the Move value when input resumes.
    // Requirement source: goal 2026-11-10-code-audit-client-fixes items 1, 2 and 10. Every "no packet" judgement first
    // sees the same packet arrive before the pause, so a broken stand-in cannot read as a blocked input.
    public sealed class PauseGameplayInputSceneTests
    {
        const int LocalId = 700;
        const int RemoteId = 702;
        // Longer than the invite (1 s) and portal (2 s) unscaled cooldowns, which would otherwise hide a paused press.
        const float InviteCooldownSeconds = 1.2f;
        const float PortalCooldownSeconds = 2.2f;
        const float PausedObservationSeconds = 0.6f;

        MapEntryPlayFixture _fixture;
        PauseMenuController _controller;

        [SetUp]
        public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown() => _fixture.Cleanup();

        IEnumerator EnterReadyTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() =>
            {
                _controller = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
                return _controller != null && _controller.isActiveAndEnabled;
            }, "the real additive UI pause controller", 10);
        }

        void Pause()
        {
            _controller.Toggle();
            Assert.AreEqual(0f, Time.timeScale, "fixture: the real controller paused the game");
        }

        void Resume()
        {
            _controller.Toggle();
            Assert.AreEqual(1f, Time.timeScale, "fixture: the real controller resumed the game");
        }

        static IEnumerator Realtime(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        static sbyte MoveX(LocalPlayerMovement player) => MapEntryPlayFixture.Field<sbyte>(player, "_currentMoveX");

        IEnumerator UnloadControllerScene()
        {
            Scene scene = _controller.gameObject.scene;
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            yield return MapEntryPlayFixture.Wait(() => unload.isDone && _controller == null,
                "the UI scene and its pause controller unload", 10);
        }

        // ---- item 1 ----

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedUiSceneUnload_ReturnsTimeScaleToOne()
        {
            yield return EnterReadyTown();
            Pause();
            yield return UnloadControllerScene();
            float timeScaleAfterUnload = Time.timeScale;
            Assert.AreEqual(1f, timeScaleAfterUnload, "item 1: a controller that goes away must release its own pause");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator UnpausedUiSceneUnload_LeavesAnotherTimeScaleAlone()
        {
            yield return EnterReadyTown();
            // 0.5 is a value this controller did not set: the release applies only to a pause it holds.
            Time.timeScale = 0.5f;
            yield return UnloadControllerScene();
            float timeScaleAfterUnload = Time.timeScale;
            Assert.AreEqual(0.5f, timeScaleAfterUnload, "item 1: an unpaused controller changes nothing when it goes away");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator MainMenuButtonWhilePaused_StillReturnsTimeScaleToOne()
        {
            yield return EnterReadyTown();
            Pause();
            _controller.OnMainMenuClicked();
            yield return MapEntryPlayFixture.Wait(() => SceneManager.GetSceneByName("MainMenu").isLoaded &&
                !_fixture.Loader.IsTransitioning, "the main menu button reaches MainMenu", 20);
            float timeScaleAfterButton = Time.timeScale;
            Assert.AreEqual(1f, timeScaleAfterButton, "item 1: the existing button path keeps returning to one");

            // MainMenu is outside the fixture's cleanup list; this name is inside it, so the fixture removes it.
            Scene empty = SceneManager.CreateScene("EntryTestCleanup_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            AsyncOperation unload = SceneManager.UnloadSceneAsync("MainMenu");
            yield return MapEntryPlayFixture.Wait(() => unload.isDone, "owned MainMenu unload", 10);
        }

        // ---- item 2: new packets ----

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedAttackKey_SendsNoAttack()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.LeftCtrl);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == 1,
                "control: Left Ctrl reaches the stand-in before the pause", 3);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => player.CanAttack && !player.IsActionLocked, "attack cooldown", 10);

            Pause();
            _fixture.Keys(Key.LeftCtrl);
            yield return Realtime(PausedObservationSeconds);
            _fixture.Keys();
            int attacksSentWhilePaused = _fixture.Peer.Count(PacketID.C_Attack) - 1;
            Assert.AreEqual(0, attacksSentWhilePaused, "item 2: Left Ctrl while paused sends no C_Attack");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedSkillKey_SendsNoSkill()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.Q);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_SkillUse) == 1,
                "control: Q reaches the stand-in before the pause", 3);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => player.CanUseDash && !player.IsActionLocked, "dash cooldown", 20);

            Pause();
            _fixture.Keys(Key.Q);
            yield return Realtime(PausedObservationSeconds);
            _fixture.Keys();
            int skillsSentWhilePaused = _fixture.Peer.Count(PacketID.C_SkillUse) - 1;
            Assert.AreEqual(0, skillsSentWhilePaused, "item 2: Q while paused sends no C_SkillUse");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedInviteKey_SendsNoInvite()
        {
            yield return EnterReadyTown();
            _fixture.Peer.Send(new S_PlayerJoin { entityId = RemoteId, characterClass = 0, spawnX = 6, spawnY = 0 }.Write());
            _fixture.Peer.Send(new S_Snapshot { entityId = RemoteId, serverTick = 2, x = 6, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance.TryGetTransform(RemoteId, out _),
                "remote player spawned", 5);
            _fixture.Keys(Key.P);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_PartyInvite) == 1,
                "control: P reaches the stand-in before the pause", 3);
            _fixture.Keys();
            yield return Realtime(InviteCooldownSeconds);

            Pause();
            _fixture.Keys(Key.P);
            yield return Realtime(PausedObservationSeconds);
            _fixture.Keys();
            int invitesSentWhilePaused = _fixture.Peer.Count(PacketID.C_PartyInvite) - 1;
            Assert.AreEqual(0, invitesSentWhilePaused, "item 2: P while paused sends no C_PartyInvite");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedUpArrowOnPortal_SendsNoEnterPortal()
        {
            yield return EnterReadyTown();
            PortalTrigger portal = UnityEngine.Object.FindObjectsByType<PortalTrigger>().First();
            float deadline = Time.realtimeSinceStartup + 20;
            while (Mathf.Abs(_fixture.Player.transform.position.x - portal.transform.position.x) > 0.3f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "fixture: the virtual keyboard could not reach the portal");
                _fixture.Keys(_fixture.Player.transform.position.x < portal.transform.position.x ? Key.D : Key.A);
                yield return null;
            }
            _fixture.Keys();
            yield return _fixture.Frames(4);
            Assert.IsTrue(MapEntryPlayFixture.Field<bool>(portal, "_isOverlapping"), "fixture: real physics reports the portal overlap");

            _fixture.Keys(Key.UpArrow);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_EnterPortal) == 1,
                "control: Up Arrow reaches the stand-in before the pause", 3);
            _fixture.Keys();
            yield return Realtime(PortalCooldownSeconds);

            Pause();
            _fixture.Keys(Key.UpArrow);
            yield return Realtime(PausedObservationSeconds);
            _fixture.Keys();
            int portalsSentWhilePaused = _fixture.Peer.Count(PacketID.C_EnterPortal) - 1;
            Assert.AreEqual(0, portalsSentWhilePaused, "item 2: Up Arrow while paused sends no C_EnterPortal");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PausedClient_StillAppliesServerPackets()
        {
            yield return EnterReadyTown();
            _fixture.Hp(51, 100);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 51, "control: server HP before the pause", 3);
            Pause();
            _fixture.Hp(52, 100);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == 52, "item 2: server packets keep applying while paused", 3);
        }

        // ---- item 2: the Move value across a pause ----

        [UnityTest, Timeout(120000)]
        public IEnumerator MoveKeyReleasedWhilePaused_IsZeroAfterResume()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => MoveX(player) == 1, "control: D sets the Move value before the pause", 3);
            Pause();
            _fixture.Keys();
            yield return _fixture.Frames(6);
            Resume();
            yield return _fixture.Frames(6);
            sbyte moveAfterResume = MoveX(player);
            Assert.AreEqual((sbyte)0, moveAfterResume, "item 2: a key released while paused moves nothing after resume");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator MoveKeyHeldThroughPause_IsOneAfterResume()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => MoveX(player) == 1, "control: D sets the Move value before the pause", 3);
            Pause();
            yield return _fixture.Frames(6);
            Resume();
            yield return _fixture.Frames(6);
            sbyte moveAfterResume = MoveX(player);
            Assert.AreEqual((sbyte)1, moveAfterResume, "item 2: a key held through the pause still moves after resume");
        }

        // ---- item 10 ----

        [UnityTest, Timeout(120000)]
        public IEnumerator MoveKeyHeldFromEntryWait_AppliesAfterReady_WithoutPressingAgain()
        {
            yield return _fixture.EnterScriptedTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Keyboard.dKey.isPressed, "synthetic keyboard D state", 3);
            yield return _fixture.Frames(8);
            Assert.AreEqual(MapEntryState.Entering, _fixture.Session.Entry.State, "fixture: still waiting for HP");
            Assert.AreEqual((sbyte)0, MoveX(player), "fixture: the waiting entry ignores the held key");

            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            float deadline = Time.realtimeSinceStartup + 3;
            while (MoveX(player) != 1 && Time.realtimeSinceStartup < deadline) yield return null;
            sbyte moveAfterReady = MoveX(player);
            Assert.AreEqual((sbyte)1, moveAfterReady, "item 10: the D held since before Ready moves after Ready within 3 s");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator OneShotKeysPressedBeforeReady_AreNotReplayedAfterReady()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Keys(Key.Space, Key.LeftCtrl, Key.Q);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Keyboard.spaceKey.isPressed, "synthetic keyboard state", 3);
            yield return _fixture.Frames(8);
            Assert.AreEqual(MapEntryState.Entering, _fixture.Session.Entry.State, "fixture: still waiting for HP");

            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return Realtime(PausedObservationSeconds);
            int attacks = _fixture.Peer.Count(PacketID.C_Attack);
            int skills = _fixture.Peer.Count(PacketID.C_SkillUse);
            int jumpIntents = JumpIntents();
            Assert.AreEqual(0, attacks, "item 10: an attack pressed before Ready is not replayed");
            Assert.AreEqual(0, skills, "item 10: a skill pressed before Ready is not replayed");
            Assert.AreEqual(0, jumpIntents, "item 10: a jump pressed before Ready is not replayed");

            _fixture.Keys();
            yield return _fixture.Frames(2);
            _fixture.Keys(Key.LeftCtrl);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_Attack) == 1,
                "control: a new press after Ready reaches the stand-in", 3);
            _fixture.Keys();
        }

        // EntryWirePeer exposes the last frame of a kind only, while a replayed jump is one frame among many intents.
        int JumpIntents()
        {
            var frames = MapEntryPlayFixture.Field<ConcurrentQueue<byte[]>>(_fixture.Peer, "_received");
            int count = 0;
            foreach (byte[] frame in frames)
            {
                if (BitConverter.ToUInt16(frame, 2) != (ushort)PacketID.C_MoveIntent) continue;
                var intent = new C_MoveIntent();
                intent.Read(new ArraySegment<byte>(frame));
                if (InputBits.Decode(intent.input).jumpPressed) count++;
            }
            return count;
        }
    }
}
