using Dawnholder.Client.State;
using NUnit.Framework;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    public class RemoteInterpolationStateTests
    {
        [Test]
        public void EmptyBuffer_HasNoPosition_AndDoesNotInitializeClock()
        {
            var state = new RemoteInterpolationState();
            Assert.IsFalse(state.TryAdvance(100f, out _));
            AddLinearSamples(state);
            AssertPosition(state, 100f, 17f, -34f); // First sample ignores frame dt, targets 1s - 150ms.
        }

        [Test]
        public void OneSnapshot_AlwaysHoldsItsExactCoordinates()
        {
            var state = new RemoteInterpolationState();
            state.EnqueueSnapshot(123, -4.125f, 9.375f);
            AssertPosition(state, 0f, -4.125f, 9.375f);
            AssertPosition(state, 0.25f, -4.125f, 9.375f);
            AssertPosition(state, 10f, -4.125f, 9.375f);
        }

        [Test]
        public void ServerTicks_SelectInteriorSegment_AndInterpolateBothAxes()
        {
            var state = new RemoteInterpolationState();
            state.EnqueueSnapshot(0, -99f, -99f);
            state.EnqueueSnapshot(10, 2f, 10f);
            state.EnqueueSnapshot(20, 12f, -10f);
            // 850ms is 70% between 500ms and 1000ms.
            AssertPosition(state, 0f, 9f, -4f);
        }

        [Test]
        public void BeforeOldest_HoldsOldest_AndAfterNewestDoesNotExtrapolate()
        {
            var state = new RemoteInterpolationState();
            state.EnqueueSnapshot(19, 19f, -38f);
            state.EnqueueSnapshot(20, 20f, -40f);
            AssertPosition(state, 0f, 19f, -38f);
            AssertPosition(state, 0.3f, 20f, -40f);
        }

        [Test]
        public void Catchup_IsTenPercentPerFrame_NotDeltaTimeNormalized()
        {
            var oneFrame = new RemoteInterpolationState();
            var twoFrames = new RemoteInterpolationState();
            AddLinearSamples(oneFrame);
            AddLinearSamples(twoFrames);
            AssertPosition(oneFrame, 0f, 17f, -34f);
            AssertPosition(twoFrames, 0f, 17f, -34f);
            AssertPosition(oneFrame, 0.1f, 18.8f, -37.6f);
            AssertPosition(twoFrames, 0.05f, 17.9f, -35.8f);
            AssertPosition(twoFrames, 0.05f, 18.71f, -37.42f);
        }

        [TestCase(0.5f, 20f)]
        [TestCase(0.5001f, 17f)]
        public void Resync_UsesStrictHalfSecondThreshold(float dt, float expectedX)
        {
            var state = new RemoteInterpolationState();
            AddLinearSamples(state);
            AssertPosition(state, 0f, 17f, -34f);
            // Exactly 0.5s keeps smoothing (then newest clamp); greater than 0.5s snaps to 850ms.
            AssertPosition(state, dt, expectedX, -2f * expectedX);
        }

        [Test]
        public void PositiveDrift_AtThresholdSmooths_AboveThresholdResyncs()
        {
            var atBoundary = new RemoteInterpolationState();
            var aboveBoundary = new RemoteInterpolationState();
            AddLinearSamples(atBoundary);
            AddLinearSamples(aboveBoundary);
            AssertPosition(atBoundary, 0f, 17f, -34f);
            AssertPosition(aboveBoundary, 0f, 17f, -34f);
            atBoundary.EnqueueSnapshot(30, 30f, -60f);
            aboveBoundary.EnqueueSnapshot(31, 31f, -62f);
            AssertPosition(atBoundary, 0f, 18f, -36f);
            AssertPosition(aboveBoundary, 0f, 28f, -56f);
        }

        [TestCase(0, 17f)]
        [TestCase(-1, 20f)]
        public void Retention_KeepsExactOneSecondBoundary_RemovesOlder(int firstTick, float expectedX)
        {
            var state = new RemoteInterpolationState();
            state.EnqueueSnapshot(firstTick, 0f, 0f);
            state.EnqueueSnapshot(20, 20f, -40f);
            AssertPosition(state, 0f, expectedX, -2f * expectedX);
        }

        [TestCase(false, 87f)]
        [TestCase(true, 105f)]
        public void ClearBuffer_PreservesClock_WhileResetReinitializes(bool reset, float expectedX)
        {
            var state = new RemoteInterpolationState();
            AddLinearSamples(state);
            AssertPosition(state, 0f, 17f, -34f);
            if (reset) state.Reset();
            else state.ClearBuffer();
            Assert.IsFalse(state.TryAdvance(100f, out _)); // Empty updates cannot consume clock time.
            state.EnqueueSnapshot(16, 80f, 0f);
            state.EnqueueSnapshot(24, 120f, 0f);
            AssertPosition(state, 0f, expectedX, 0f);
        }

        [Test]
        public void LongPauseAndBurst_ResyncToDelayedServerTime()
        {
            var state = new RemoteInterpolationState();
            AddLinearSamples(state);
            AssertPosition(state, 0f, 17f, -34f);
            for (int tick = 24; tick <= 100; tick += 4)
                state.EnqueueSnapshot(tick, tick, -2f * tick);
            AssertPosition(state, 10f, 97f, -194f);
        }

        [Test]
        public void DuplicateTicks_DoNotReplaceEarlierSampleOrCreateNonFinitePosition()
        {
            var state = new RemoteInterpolationState();
            state.EnqueueSnapshot(0, 0f, 0f);
            state.EnqueueSnapshot(20, 20f, -40f);
            state.EnqueueSnapshot(20, 999f, 999f);
            AssertPosition(state, 0f, 17f, -34f);
        }

        static void AddLinearSamples(RemoteInterpolationState state)
        {
            state.EnqueueSnapshot(0, 0f, 0f);
            state.EnqueueSnapshot(10, 10f, -20f);
            state.EnqueueSnapshot(20, 20f, -40f);
        }

        static void AssertPosition(RemoteInterpolationState state, float dt, float x, float y)
        {
            Assert.IsTrue(state.TryAdvance(dt, out Vector2 position));
            Assert.That(position.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(y).Within(0.0001f));
        }
    }
}
