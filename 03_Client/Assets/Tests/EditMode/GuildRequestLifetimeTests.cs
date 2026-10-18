using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // C7/C9: real session requests and peer bytes, correlated server results, current-connection queue.
    // GrowthConfirmed observes server events only; it defines no Art component or effect method.
    public sealed class GuildRequestLifetimeTests
    {
        GuildClientHarness _h;

        [SetUp] public void Setup() => _h = new GuildClientHarness();
        [TearDown] public void Teardown() => _h?.Dispose();

        [TestCase("Refresh")]
        [TestCase("TryUpgradeStronghold")]
        [TestCase("TryEnhanceWeapon")]
        public void BeforeGameplayReady_NoGuildRequestIsSent(string request)
        {
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, request));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void Query_UsesReservedZero_AndOnlyLargerQuerySnapshotCompletesIt()
        {
            _h.Synchronize();
            Assert.IsTrue(GuildClientProduct.Request(_h.Session, "Refresh"));
            CollectionAssert.AreEqual(new byte[] { 5, 0, 43, 0, 0 }, _h.Sockets.ReadFrame());
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, kind: 5));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "Presence cannot acknowledge Query");
            _h.Deliver(GuildClientWire.Snapshot(sequence: 1));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "old Query cannot acknowledge a new request");
            _h.Deliver(GuildClientWire.Snapshot(sequence: 3));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Purchase_UsesExactCurrentRevision_AndDoesNotSpendOrGrowLocally(bool enhance)
        {
            _h.Synchronize(unlocked: enhance);
            BeginPurchase(enhance);
            byte[] expected = enhance ? new byte[] { 8, 0, 46, 0, 3, 0, 0, 0 } : new byte[] { 8, 0, 45, 0, 7, 0, 0, 0 };
            CollectionAssert.AreEqual(expected, _h.Sockets.ReadFrame());
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(40, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            Assert.AreEqual(0, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            Assert.AreEqual(71, InventoryState.Instance.Currency);
            Assert.AreEqual(4, InventoryState.Instance.GetSlot(0).Count);
            foreach (string request in new[] { "Refresh", "TryUpgradeStronghold", "TryEnhanceWeapon" })
                Assert.IsFalse(GuildClientProduct.Request(_h.Session, request), "purchase owns all guild request channels");
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothMatching47AndPointed44_AreRequired_InEitherArrivalOrder(bool snapshotFirst)
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            byte[] result = GuildClientWire.Result();
            byte[] snapshot = GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true);
            _h.Deliver(snapshotFirst ? snapshot : result);
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "a single half cannot complete the purchase");
            _h.Deliver(snapshotFirst ? result : snapshot);
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(GuildResult.Success, GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [TestCase("action")]
        [TestCase("requested_revision")]
        [TestCase("pointed_sequence")]
        [TestCase("shared_revision")]
        public void MismatchedResultOrSnapshot_DoesNotReleasePurchase(string mismatch)
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            byte[] result = GuildClientWire.Result();
            if (mismatch == "action") result[4] = 2;
            if (mismatch == "requested_revision") GuildClientWire.U32(result, 6, 6);
            if (mismatch == "pointed_sequence") GuildClientWire.I64(result, 18, 3);
            if (mismatch == "shared_revision") GuildClientWire.U32(result, 10, 9);
            _h.Deliver(result);
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void Enhancement_PointedSnapshotWithDifferentPersonalRevision_DoesNotComplete()
        {
            _h.Synchronize(unlocked: true);
            BeginPurchase(enhance: true);
            _h.Sockets.ReadFrame();
            _h.Deliver(GuildClientWire.Result(action: 2, requested: 3, guildRevision: 7, inventoryRevision: 5));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, inventoryRevision: 4, kind: 4, unlocked: true, enhanced: true));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void OldCompletedResult_CannotCompleteANewPurchaseAtTheSameRequestedRevision()
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            byte[] oldResult = GuildClientWire.Result(result: 3, guildRevision: 7);
            byte[] oldSnapshot = GuildClientWire.Snapshot(sequence: 2);
            _h.Deliver(oldResult);
            _h.Deliver(oldSnapshot);
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            BeginPurchase();
            CollectionAssert.AreEqual(new byte[] { 8, 0, 45, 0, 7, 0, 0, 0 }, _h.Sockets.ReadFrame());
            _h.Deliver(oldResult);
            _h.Deliver(oldSnapshot);
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "same action/revision alone cannot match a completed old sequence");
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            _h.Deliver(GuildClientWire.Result(result: 3, guildRevision: 7, sequence: 3));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 3));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
        }

        [Test]
        public void UnrelatedBroadcast_MapTransitionAndElapsedCallbacks_DoNotReleaseOrResendPurchase()
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, kind: 2, treasury: 50));
            _h.Session.Entry.Begin(1, 2, 3, true);
            _h.RunDelayed();
            _h.RunDelayed();
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "Refresh"));
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
            Assert.AreEqual(50, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
        }

        [Test]
        public void ControllerReacquisition_DoesNotReplacePendingOwner_WhenViewsComeAndGo()
        {
            _h.Synchronize();
            object original = GuildClientProduct.Controller(_h.Session);
            BeginPurchase();
            _h.Sockets.ReadFrame();
            Assert.AreSame(original, GuildClientProduct.Controller(_h.Session), "window lifetime does not own the controller");
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [UnityTest]
        public IEnumerator Purchase_RemainsPendingAcrossARealSixSecondDispatcherInterval()
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            var root = new GameObject("Guild pending time fixture");
            root.SetActive(false);
            var dispatcher = root.AddComponent<MainThreadDispatcher>();
            double started = Time.realtimeSinceStartupAsDouble;
            try
            {
                while (Time.realtimeSinceStartupAsDouble - started < 6.0)
                {
                    SessionTestTools.Call(dispatcher, "Update");
                    _h.Queue.Drain();
                    yield return null;
                }
                TestContext.Out.WriteLine("observed pending interval seconds=" + (Time.realtimeSinceStartupAsDouble - started));
                Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
                Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"));
                Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Result_CapturesOffsetBufferBeforeReuse_AndAppliesOnlyOnMain()
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            byte[] raw = GuildClientWire.Result();
            byte[] reused = new byte[38];
            Buffer.BlockCopy(raw, 0, reused, 5, raw.Length);
            _h.Session.OnRecvPacket(new ArraySegment<byte>(reused, 5, raw.Length));
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Array.Clear(reused, 0, reused.Length);
            _h.Queue.Drain();
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(GuildResult.Success, GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        public void AllDefinedResultCodes_CompleteOnlyWithThePointedSnapshot(int code)
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            _h.Deliver(GuildClientWire.Result(result: (byte)code, guildRevision: 7));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual((GuildResult)code, GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.AreEqual(40, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
        }

        static IEnumerable<TestCaseData> InvalidResults()
        {
            for (int size = 0; size < 26; size++)
                yield return new TestCaseData(GuildClientWire.Result().Take(size).ToArray()).SetName("Result_RejectsLength_" + size);
            yield return ResultCase("long", GuildClientWire.Result().Concat(new byte[] { 0 }).ToArray());
            byte[] bad = GuildClientWire.Result(); bad[0] = 25;
            yield return ResultCase("header", bad);
            bad = GuildClientWire.Result(); bad[2] = 44;
            yield return ResultCase("id", bad);
            bad = GuildClientWire.Result(); bad[4] = 0;
            yield return ResultCase("action_zero", bad);
            bad = GuildClientWire.Result(); bad[4] = 3;
            yield return ResultCase("action_three", bad);
            bad = GuildClientWire.Result(); bad[5] = 11;
            yield return ResultCase("code", bad);
            bad = GuildClientWire.Result(sequence: 0);
            yield return ResultCase("sequence_zero", bad);
            yield return ResultCase("sequence_negative", GuildClientWire.Result(sequence: -1));
        }

        static TestCaseData ResultCase(string name, byte[] raw) => new TestCaseData(raw).SetName("Result_Rejects_" + name);

        [TestCaseSource(nameof(InvalidResults))]
        public void MalformedResult_CannotAcknowledgeTheMatchingSnapshot(byte[] raw)
        {
            _h.Synchronize();
            BeginPurchase();
            _h.Sockets.ReadFrame();
            Assert.DoesNotThrow(() => GuildClientProduct.HandleAs(_h.Session, 47, new ArraySegment<byte>(raw)));
            _h.Queue.Drain();
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session));
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.IsFalse(_h.Session.IsClosed);
        }

        [Test]
        public void Enhancement_Result36And44_ConfirmServerValues_WithoutDuplicateGrowth()
        {
            _h.Synchronize(unlocked: true);
            var growth = new List<GuildChangeKind>();
            using var subscription = GuildClientProduct.ObserveGrowth(_h.Session, growth);
            BeginPurchase(enhance: true);
            _h.Sockets.ReadFrame();
            byte[] result = GuildClientWire.Result(action: 2, requested: 3, guildRevision: 7, inventoryRevision: 4);
            byte[] snapshot = GuildClientWire.Snapshot(sequence: 2, inventoryRevision: 4, kind: 4, unlocked: true, enhanced: true);
            _h.Deliver(result);
            _h.Deliver(InventoryWire.Snapshot(4, 51, (1, 3)));
            Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "36 cannot substitute for the pointed44");
            _h.Deliver(snapshot);
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.AreEqual(51, InventoryState.Instance.Currency);
            Assert.AreEqual(3, InventoryState.Instance.GetSlot(0).Count);
            Assert.AreEqual(1, GuildClientProduct.Value<int>(_h.Session, "WeaponLevel"));
            CollectionAssert.AreEqual(new[] { GuildChangeKind.WeaponEnhance }, growth);
            _h.Deliver(result);
            _h.Deliver(snapshot);
            CollectionAssert.AreEqual(new[] { GuildChangeKind.WeaponEnhance }, growth);
        }

        [Test]
        public void FirstSynchronizationAndQuery_NeverReplayGrowth_ButNewStrongholdEventDoesOnce()
        {
            var growth = new List<GuildChangeKind>();
            using var subscription = GuildClientProduct.ObserveGrowth(_h.Session, growth);
            _h.Deliver(GuildClientWire.Snapshot(kind: 3, unlocked: true));
            _h.Deliver(GuildClientWire.Snapshot(sequence: 2, unlocked: true));
            Assert.IsEmpty(growth);
            // A separate connection begins at level1: an already-grown first snapshot above is history.
            _h.Dispose();
            _h = new GuildClientHarness();
            using var nextSubscription = GuildClientProduct.ObserveGrowth(_h.Session, growth);
            _h.Deliver(GuildClientWire.Snapshot());
            byte[] grown = GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, treasury: 10, unlocked: true);
            _h.Deliver(grown);
            _h.Deliver(grown);
            _h.Deliver(GuildClientWire.Snapshot(sequence: 3, guildRevision: 8, unlocked: true));
            CollectionAssert.AreEqual(new[] { GuildChangeKind.StrongholdUpgrade }, growth);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QueuedResultAndDelayedIntent_AfterReplacementOrCleanup_HaveNoEffects(bool cleanup)
        {
            _h.Synchronize();
            UnityClientSession.SimulatedLatencyMs = 10;
            BeginPurchase();
            _h.Session.OnRecvPacket(new ArraySegment<byte>(GuildClientWire.Result()));
            _h.Session.OnRecvPacket(new ArraySegment<byte>(GuildClientWire.Snapshot(sequence: 2, guildRevision: 8, kind: 3, unlocked: true)));
            if (cleanup) _h.Session.Cleanup();
            else _h.Current = false;
            _h.RunDelayed();
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.IsFalse(GuildClientProduct.Read<bool>(GuildClientProduct.Controller(_h.Session), "IsSynchronized"));
            if (!cleanup)
            {
                Assert.IsTrue(GuildClientProduct.Pending(_h.Session), "an obsolete callback cannot acknowledge an old pending request");
                Assert.AreEqual(1L, GuildClientProduct.Value<long>(_h.Session, "SnapshotSequence"));
            }
            if (cleanup)
            {
                Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
                Assert.IsFalse(GuildClientProduct.Value<bool>(_h.Session, "HasSnapshot"));
            }
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "Refresh"));
        }

        [Test]
        public void SendException_IsObservable_AndNeverClaimsPurchaseSuccess()
        {
            _h.Synchronize();
            UnityClientSession.SimulatedLatencyMs = 10;
            _h.ThrowOnSchedule = true;
            Assert.IsFalse(GuildClientProduct.Request(_h.Session, "TryUpgradeStronghold"));
            Assert.IsInstanceOf<InvalidOperationException>(GuildClientProduct.Read<Exception>(GuildClientProduct.Controller(_h.Session), "LastSendError"));
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
            Assert.AreEqual(40, GuildClientProduct.Value<int>(_h.Session, "Treasury"));
            Assert.IsEmpty(PeerFrames.Take(_h.Sockets));
        }

        [Test]
        public void CleanupThenNewConnection_StartsEmpty_AndOldCallbackCannotChangeNewOwner()
        {
            _h.Synchronize();
            UnityClientSession old = _h.Session;
            var oldQueue = _h.Queue;
            old.OnRecvPacket(new ArraySegment<byte>(GuildClientWire.Snapshot(sequence: 9, treasury: 999)));
            old.Cleanup();
            _h.Dispose();
            _h = new GuildClientHarness();
            object fresh = GuildClientProduct.State(_h.Session);
            oldQueue.Drain();
            old.OnRecvPacket(new ArraySegment<byte>(GuildClientWire.Result()));
            oldQueue.Drain();
            Assert.IsFalse(GuildClientProduct.Read<bool>(fresh, "HasSnapshot"));
            Assert.IsFalse(GuildClientProduct.Pending(_h.Session));
            Assert.IsNull(GuildClientProduct.Read<GuildResult?>(GuildClientProduct.Controller(_h.Session), "LastResult"));
        }

        void BeginPurchase(bool enhance = false) => Assert.IsTrue(GuildClientProduct.Request(_h.Session,
            enhance ? "TryEnhanceWeapon" : "TryUpgradeStronghold"));
    }
}
