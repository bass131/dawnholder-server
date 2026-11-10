using System;
using System.Collections;
using System.Collections.Concurrent;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent verifier's judgement tests for the input/display PR: three pause cases the implementation's tests leave
    // open. Requirement source: goal 2026-11-10-code-audit-client-fixes item 2 (no new jump or move input becomes a
    // packet or a prediction while the menu is open; Escape stays as it was) and the shared design with item 10 (read the
    // Move value again when input is accepted again; one-shot input is not replayed). Each "nothing sent" judgement first sees the same input
    // reach the loopback stand-in before the pause, so a broken stand-in or keyboard cannot read as a blocked input.
    public sealed class VerifyPr1PauseInputGapTests
    {
        const float PausedObservationSeconds = 0.6f;
        // Lets intents already on the loopback socket arrive before the paused count is taken.
        const float InFlightDrainSeconds = 0.3f;

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

        // Before the fix a grounded Space while paused set the jump latch, and the first tick after resume sent it.
        [UnityTest, Timeout(120000)]
        public IEnumerator PausedJumpKey_IsNotReplayedAfterResume()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            yield return MapEntryPlayFixture.Wait(() => player.OnGround, "fixture: grounded before the control jump", 5);
            _fixture.Keys(Key.Space);
            yield return MapEntryPlayFixture.Wait(() => JumpIntents() >= 1,
                "control: Space reaches the stand-in as a jump intent before the pause", 3);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => player.OnGround, "fixture: landed after the control jump", 5);
            yield return Realtime(InFlightDrainSeconds);
            Assert.IsTrue(player.OnGround, "fixture: grounded at the paused press, so the ground gate alone would pass it");
            int jumpsBeforePause = JumpIntents();

            Pause();
            _fixture.Keys(Key.Space);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Keyboard.spaceKey.isPressed,
                "fixture: the paused Space reaches the virtual keyboard", 3);
            yield return Realtime(PausedObservationSeconds);
            _fixture.Keys();
            yield return _fixture.Frames(2);
            Resume();
            yield return Realtime(PausedObservationSeconds);
            int jumpsFromPausedPress = JumpIntents() - jumpsBeforePause;
            Assert.AreEqual(0, jumpsFromPausedPress, "item 2: Space pressed while paused is not sent as a jump after resume");
        }

        // A press that starts during the pause makes no intent or movement while paused; at resume the held key is read
        // again. The implementation's tests cover a key held through the pause and a key released during it only.
        [UnityTest, Timeout(120000)]
        public IEnumerator MoveKeyPressedOnlyWhilePaused_SendsNothingWhilePaused_AndMovesAfterResume()
        {
            yield return EnterReadyTown();
            LocalPlayerMovement player = _fixture.Player;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => MoveX(player) == 1, "control: D sets the Move value before the pause", 3);
            _fixture.Keys();
            yield return MapEntryPlayFixture.Wait(() => MoveX(player) == 0, "control: releasing D clears the Move value", 3);

            Pause();
            yield return Realtime(InFlightDrainSeconds);
            int intentsAtPause = _fixture.Peer.Count(PacketID.C_MoveIntent);
            float xAtPause = player.transform.position.x;
            _fixture.Keys(Key.D);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Keyboard.dKey.isPressed,
                "fixture: the paused D reaches the virtual keyboard", 3);
            yield return Realtime(PausedObservationSeconds);
            int intentsWhilePaused = _fixture.Peer.Count(PacketID.C_MoveIntent) - intentsAtPause;
            float xMovedWhilePaused = player.transform.position.x - xAtPause;
            Assert.AreEqual(0, intentsWhilePaused, "item 2: D pressed while paused sends no C_MoveIntent");
            Assert.AreEqual(0f, xMovedWhilePaused, 1e-4f, "item 2: D pressed while paused predicts no movement");

            Resume();
            float deadline = Time.realtimeSinceStartup + 3;
            while (MoveX(player) != 1 && Time.realtimeSinceStartup < deadline) yield return null;
            sbyte moveAfterResume = MoveX(player);
            _fixture.Keys();
            Assert.AreEqual((sbyte)1, moveAfterResume, "item 2 design: the D still held at resume is read again and moves");
        }

        // Item 2 keeps Escape as it was: the new gameplay gate must not swallow the key that closes the menu while paused.
        // The existing appearance test opens the menu with Escape and closes it with the pointer only.
        [UnityTest, Timeout(120000)]
        public IEnumerator EscapeWhilePaused_StillResumesTheGame()
        {
            yield return EnterReadyTown();
            _fixture.Keys(Key.Escape);
            yield return MapEntryPlayFixture.Wait(() => Time.timeScale == 0f, "control: Escape pauses the game", 3);
            _fixture.Keys();
            yield return _fixture.Frames(2);

            _fixture.Keys(Key.Escape);
            float deadline = Time.realtimeSinceStartup + 3;
            while (Time.timeScale != 1f && Time.realtimeSinceStartup < deadline) yield return null;
            float timeScaleAfterSecondEscape = Time.timeScale;
            _fixture.Keys();
            Assert.AreEqual(1f, timeScaleAfterSecondEscape, "item 2: Escape while paused still resumes the game");
        }

        // Same reading as the implementation's test: EntryWirePeer keeps only the last frame of a kind, and a jump is one
        // intent among many, so the raw received frames are decoded here.
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
