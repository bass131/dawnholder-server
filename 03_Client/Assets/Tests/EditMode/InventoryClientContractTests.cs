using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // Requirement tests for the PR2 client inventory, written before the product (pr2-acceptance.md R1-R5,
    // R7 and the mirror side of R6; goal-review.md INV-19). Server packets enter the real
    // UnityClientSession.OnRecvPacket and its EnqueueApply gate (R3); ManualConnectionQueue stands in for
    // MainThreadDispatcher so a test decides when "main" drains. Snapshot tests therefore need no map entry.
    // Map readiness goes through the session's own MapEntryCoordinator barrier via EntryBindingFixture
    // without loading scenes; the real scene/UI path is PlayMode work after implementation. Client sends
    // are read back from a loopback peer. Expected values are server literals, never product output.
    public sealed class InventoryClientContractTests
    {
        static readonly Dictionary<string, Func<byte[]>> s_invalidSnapshots = new()
        {
            ["length 75"] = () => InventoryWire.Resized(NewerValidSnapshot(), 75),
            ["length 77"] = () => InventoryWire.Resized(NewerValidSnapshot(), 77),
            ["header size 80 on 76 bytes"] = () => InventoryWire.WithHeaderSize(NewerValidSnapshot(), 80),
            ["currency -1"] = () => InventoryWire.Snapshot(2, -1, (1, 2)),
            ["currency 1000000001"] = () => InventoryWire.Snapshot(2, 1_000_000_001, (1, 2)),
            ["count 100"] = () => InventoryWire.Snapshot(2, 20, (1, 100)),
            ["count -1"] = () => InventoryWire.Snapshot(2, 20, (1, -1)),
            ["undefined item 3"] = () => InventoryWire.Snapshot(2, 20, (3, 1)),
            ["item -1"] = () => InventoryWire.Snapshot(2, 20, (-1, 1)),
            ["empty slot with a count"] = () => InventoryWire.Snapshot(2, 20, (1, 2), (0, 5)),
            ["hole before a stack"] = () => InventoryWire.Snapshot(2, 20, (0, 0), (1, 2)),
            ["descending item order"] = () => InventoryWire.Snapshot(2, 20, (2, 1), (1, 2)),
            ["duplicate item stacks"] = () => InventoryWire.Snapshot(2, 20, (1, 1), (1, 2)),
        };

        // Result codes are the PR1 wire literals: 0 Success, 1 Stale, 3 NotUsable; 7 is undefined.
        static readonly Dictionary<string, Func<byte[]>> s_malformedResults = new()
        {
            ["length 12"] = () => InventoryWire.Resized(InventoryWire.ItemUseResult(0, 2, 3), 12),
            ["length 14"] = () => InventoryWire.Resized(InventoryWire.ItemUseResult(0, 2, 3), 14),
            ["header size 20 on 13 bytes"] = () => InventoryWire.WithHeaderSize(InventoryWire.ItemUseResult(0, 2, 3), 20),
            ["unknown result 7"] = () => InventoryWire.ItemUseResult(7, 2, 3),
        };

        readonly List<UnityClientSession> _sessions = new();
        readonly List<IDisposable> _fixtures = new();
        ManualConnectionQueue _queue;
        InventoryMirrorProbe _mirror;
        int _latency;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0; // direct sends, as in a normal Play/Release build
            _queue = new ManualConnectionQueue();
            _mirror = InventoryMirrorProbe.TryPublish();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                foreach (var session in _sessions) session.Disconnect();
                _queue.Drain();
            }
            finally
            {
                foreach (var session in _sessions) session.Cleanup();
                _sessions.Clear();
                for (int i = _fixtures.Count - 1; i >= 0; i--) _fixtures[i].Dispose();
                _fixtures.Clear();
                _mirror?.Dispose();
                UnityClientSession.SimulatedLatencyMs = _latency;
            }
        }

        // R1 / INV-19: "never synchronized" differs from "synchronized with an empty revision 0", and the
        // value changes only on the main-thread drain (R3).
        [Test]
        public void Mirror_StartsUnsynced_ThenShowsRevisionZeroEmptySnapshotAfterMainDrain()
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            AssertUnsyncedAndEmpty(mirror, "R1 before any snapshot");
            var empty = new ServerInventory(0, 0);

            Deliver(session, empty.Wire());
            bool appliedBeforeDrain = mirror.HasSnapshot;
            _queue.Drain();

            Assert.IsFalse(appliedBeforeDrain, "R3 receipt must not touch the mirror before the main drain");
            empty.AssertShownBy(mirror, "R1 revision 0 empty snapshot");
        }

        // R4 (required, msg_b554a4b55c29) with R1: the same connection keeps its mirror across a map move,
        // and a reward granted while its push could not reach the client is recovered only by the query
        // that the next completed entry sends. The stand-in server answers only queries it received.
        [Test]
        public void MapMove_KeepsMirror_AndReadyRequeryRecoversRewardMissedInTheGap()
        {
            var (session, sockets, views) = GameplaySession();
            views.Begin(map: 0);
            views.Ready();
            AssertOneInventoryQuery(sockets, "R4 initial gameplay entry");
            var beforeMove = new ServerInventory(1, 10, (ItemId.Material, 1));
            Deliver(session, beforeMove.Wire());
            _queue.Drain();
            InventoryMirrorProbe mirror = _mirror.Require();
            beforeMove.AssertShownBy(mirror, "R4 answer to the first query");

            views.Begin(map: 1);
            views.BindViews(); // scene, player and registries are bound; the new map's HP is still pending
            AssertNoInventoryQuery(sockets, "R4 entry not Ready yet");
            beforeMove.AssertShownBy(mirror, "R1 a map move keeps the same connection's mirror");

            // Server side during the move: CoinPouch +1 and currency +10 (revision 2); that push was lost.
            views.Ready(hp: 31);
            AssertOneInventoryQuery(sockets, "R4 entry completed after the move");
            var afterMove = new ServerInventory(2, 20, (ItemId.Material, 1), (ItemId.CoinPouch, 1));
            Deliver(session, afterMove.Wire());
            _queue.Drain();
            afterMove.AssertShownBy(mirror, "R4 requery shows the reward missed in the gap");
        }

        // R4: the query follows the real entry barrier (scene, player, registries and HP), not scene binding,
        // and repeated binding/HP callbacks on a Ready entry do not repeat it.
        [Test]
        public void InventoryQuery_WaitsForTheEntryBarrier_AndRepeatedCallbacksDoNotRepeatIt()
        {
            var (session, sockets, views) = GameplaySession();
            views.Begin(map: 0);
            views.BindViews();
            AssertNoInventoryQuery(sockets, "R4 waiting for the entry HP");

            session.ReceivePlayerHp(7, 37, 100);
            Assert.IsTrue(session.Entry.IsGameplayReady, "fixture: HP completes the entry barrier");
            AssertOneInventoryQuery(sockets, "R4 entry Ready");

            views.BindViews();
            session.TryBindEntryViews();
            session.ReceivePlayerHp(7, 30, 100);
            session.ReceivePlayerHp(7, 25, 100);
            _queue.Drain();
            AssertNoInventoryQuery(sockets, "R4 repeated callbacks on a Ready entry");
        }

        // R4: Ending has no gameplay economy, and an entry superseded before Ready never queries; only the
        // entry that actually becomes gameplay-Ready sends exactly one query.
        [Test]
        public void InventoryQuery_IsNotSentForEndingOrSupersededEntry_OnlyForTheNextGameplayReady()
        {
            var (session, sockets, views) = GameplaySession();
            views.Begin(map: 3);
            views.BindViews();
            Assert.AreEqual(MapEntryState.Ready, session.Entry.State, "fixture: Ending completes without a player");
            Assert.IsFalse(session.Entry.IsGameplayReady, "fixture: Ending is not gameplay");
            AssertNoInventoryQuery(sockets, "R4 Ending");

            views.Begin(map: 0);
            views.BindViews(); // superseded below before its HP arrives
            views.Begin(map: 1);
            views.Ready();
            AssertOneInventoryQuery(sockets, "R4 only the entry that became Ready");
        }

        // R4: a failed entry closes the connection and must not query. This negative guard already holds
        // before implementation; the positive query cases are in the tests above.
        [Test]
        public void FailedEntry_SendsNoInventoryQuery()
        {
            var (session, sockets, views) = GameplaySession();
            long epoch = views.Begin(map: 0);
            views.BindViews();
            LogAssert.Expect(LogType.Error, new Regex("Map entry failed; disconnecting"));

            session.Entry.Fail(epoch, new InvalidOperationException("injected scene failure"));
            session.ReceivePlayerHp(7, 37, 100);
            _queue.Drain();

            Assert.IsTrue(session.IsClosed, "fixture: entry failure closes the connection");
            AssertNoInventoryQuery(sockets, "R4 failed entry");
        }

        // R5 with R4: sending a query is not synchronization, and an unanswered query must not stop the next
        // completed entry from querying again.
        [Test]
        public void UnansweredQuery_LeavesMirrorUnsynced_AndTheNextReadyQueriesAgain()
        {
            var (_, sockets, views) = GameplaySession();
            views.Begin(map: 0);
            views.Ready();
            AssertOneInventoryQuery(sockets, "R4 initial gameplay entry");
            _queue.Drain();
            InventoryMirrorProbe mirror = _mirror.Require();
            Assert.IsFalse(mirror.HasSnapshot, "R5 a sent query does not synchronize the mirror");

            views.Begin(map: 1);
            views.Ready();
            AssertOneInventoryQuery(sockets, "R5 the next completed entry queries again");
            Assert.IsFalse(mirror.HasSnapshot, "R5 still no server answer");
        }

        // R2: the upper bounds of the contract (currency 1e9, stack 99) are valid and shown exactly.
        [Test]
        public void SnapshotAtTheContractBounds_IsShownExactly()
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var full = new ServerInventory(9, 1_000_000_000, (ItemId.Material, 99), (ItemId.CoinPouch, 99));

            Deliver(session, full.Wire());
            _queue.Drain();

            full.AssertShownBy(mirror, "R2 currency 1e9 and stack 99");
        }

        // R2: a malformed snapshot with a newer revision is rejected as a whole, without an exception, a
        // partial update or default values. The newer revision isolates validation from the revision guard.
        [TestCase("length 75")]
        [TestCase("length 77")]
        [TestCase("header size 80 on 76 bytes")]
        [TestCase("currency -1")]
        [TestCase("currency 1000000001")]
        [TestCase("count 100")]
        [TestCase("count -1")]
        [TestCase("undefined item 3")]
        [TestCase("item -1")]
        [TestCase("empty slot with a count")]
        [TestCase("hole before a stack")]
        [TestCase("descending item order")]
        [TestCase("duplicate item stacks")]
        public void InvalidSnapshot_IsRejectedWithoutExceptionOrPartialApply(string invalid)
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var baseline = new ServerInventory(1, 10, (ItemId.Material, 1));
            Deliver(session, baseline.Wire());
            _queue.Drain();
            baseline.AssertShownBy(mirror, "fixture baseline");

            DeliverAllowingDiagnostics(session, s_invalidSnapshots[invalid](), "R2");

            baseline.AssertShownBy(mirror, $"R2 rejected snapshot ({invalid}) keeps the previous mirror");
        }

        // R2 / INV-19: a delayed lower revision is ignored, the same revision neither repeats nor flips shown
        // values, and a newer revision is applied.
        [Test]
        public void OlderOrSameRevision_IsIgnored_NewerIsApplied()
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var current = new ServerInventory(5, 50, (ItemId.Material, 5));
            Deliver(session, current.Wire());
            _queue.Drain();
            current.AssertShownBy(mirror, "R2 revision 5");

            Deliver(session, new ServerInventory(4, 40, (ItemId.Material, 4)).Wire());
            _queue.Drain();
            current.AssertShownBy(mirror, "R2 delayed lower revision");
            Deliver(session, current.Wire());
            _queue.Drain();
            current.AssertShownBy(mirror, "R2 repeated identical revision");
            Deliver(session, new ServerInventory(5, 55, (ItemId.CoinPouch, 1)).Wire());
            _queue.Drain();
            current.AssertShownBy(mirror, "R2 same revision with other values");

            var newer = new ServerInventory(6, 60, (ItemId.Material, 6));
            Deliver(session, newer.Wire());
            _queue.Drain();
            newer.AssertShownBy(mirror, "R2 newer revision");
        }

        // R2 "외부 배열 변경": ClientNet passes OnRecvPacket a segment of its reusable receive buffer and the
        // apply runs later on main, so the shown values must be the bytes received, at any segment offset.
        [Test]
        public void SnapshotSegment_IsTakenAtReceipt_NotReadAfterTheReceiveBufferIsReused()
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var received = new ServerInventory(3, 30, (ItemId.Material, 3));
            byte[] laterBytes = new ServerInventory(4, 40, (ItemId.CoinPouch, 4)).Wire();
            byte[] receiveBuffer = new byte[160];
            int offset = 37;
            for (int i = 0; i < receiveBuffer.Length; i++) receiveBuffer[i] = 0xEE;
            Buffer.BlockCopy(received.Wire(), 0, receiveBuffer, offset, InventoryWire.SnapshotLength);

            session.OnRecvPacket(new ArraySegment<byte>(receiveBuffer, offset, InventoryWire.SnapshotLength));
            Buffer.BlockCopy(laterBytes, 0, receiveBuffer, offset, laterBytes.Length);
            _queue.Drain();
            received.AssertShownBy(mirror, "R2 bytes as received");

            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            received.AssertShownBy(mirror, "R2 stored mirror is independent of the buffer");
        }

        // R3: a snapshot already queued when its session is replaced or closes never reaches the mirror. The
        // next connection's lower revision is the positive control: it would be ignored had the old one landed.
        [TestCase(false)]
        [TestCase(true)]
        public void SnapshotQueuedBeforeSessionChange_NeverReachesTheMirror(bool closeInsteadOfReplace)
        {
            bool oldCurrent = true;
            var old = Session(() => oldCurrent);
            InventoryMirrorProbe mirror = _mirror.Require();
            Deliver(old, new ServerInventory(3, 30, (ItemId.Material, 3)).Wire());

            if (closeInsteadOfReplace) old.Disconnect();
            else oldCurrent = false;
            _queue.Drain();
            Assert.IsFalse(mirror.HasSnapshot, "R3 queued apply of an ended/replaced session");

            var next = Session();
            var fresh = new ServerInventory(1, 10, (ItemId.CoinPouch, 1));
            Deliver(next, fresh.Wire());
            _queue.Drain();
            fresh.AssertShownBy(mirror, "R3 the current connection still applies");
        }

        // R3: a late natural close of an already replaced session must not clear the new connection's mirror
        // (the session cleans only itself; the lifetime owner resets shared mirrors).
        [Test]
        public void LateCloseOfAReplacedSession_DoesNotClearTheNewConnectionsMirror()
        {
            bool oldCurrent = true;
            var old = Session(() => oldCurrent);
            oldCurrent = false; // the lifetime owner has moved on to a new connection
            var next = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var shown = new ServerInventory(2, 20, (ItemId.CoinPouch, 2));
            Deliver(next, shown.Wire());
            _queue.Drain();
            shown.AssertShownBy(mirror, "fixture: new connection snapshot");

            old.OnDisconnected(new IPEndPoint(IPAddress.Loopback, 1));
            _queue.Drain();

            Assert.IsTrue(old.IsClosed, "fixture: old session closed");
            shown.AssertShownBy(mirror, "R3 late close of the old session");
        }

        // R3 / INV-19 with R1: the real NetworkService.ResetGlobalSessionMirrors clears every inventory value
        // before any mirror observer runs (observed from an existing QuestState observer), and a failing
        // observer neither skips later observers nor leaves inventory values behind.
        [Test]
        public void ConnectionReset_ClearsInventoryBeforeObserversRun_AndIsolatesObserverFailure()
        {
            QuestState quest = Track(new PublishedSingleton<QuestState>()).Value;
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var shown = new ServerInventory(4, 40, (ItemId.Material, 2), (ItemId.CoinPouch, 1));
            Deliver(session, shown.Wire());
            _queue.Drain();
            shown.AssertShownBy(mirror, "fixture: synchronized before the reset");
            quest.ApplyUpdate(3, 5);
            var seen = new List<(bool Synced, uint Revision, int Currency, InventorySlot First)>();
            quest.OnQuestUpdated += () => throw new InvalidOperationException("injected quest observer failure");
            quest.OnQuestUpdated += () => seen.Add((mirror.HasSnapshot, mirror.Revision, mirror.Currency, mirror.Slot(0)));

            session.Disconnect();
            _queue.Drain(); // the session cleans itself before the owner resets shared mirrors
            var failure = Assert.Throws<AggregateException>(() => NetworkService.ResetGlobalSessionMirrors());

            Assert.AreEqual(1, failure.Flatten().InnerExceptions.Count, "only the injected observer failed");
            Assert.AreEqual(1, seen.Count, "the later observer still ran once");
            Assert.IsFalse(seen[0].Synced, "R3 inventory was unsynchronized before observers ran");
            Assert.AreEqual(0u, seen[0].Revision, "INV-19 revision was back to 0 before observers ran");
            Assert.AreEqual(0, seen[0].Currency, "R1 no previous currency before observers ran");
            Assert.AreEqual(ItemId.None, seen[0].First.ItemId, "R1 no previous slot item before observers ran");
            Assert.AreEqual(0, seen[0].First.Count, "R1 no previous slot count before observers ran");
            AssertUnsyncedAndEmpty(mirror, "R1 after the reset");
        }

        // R1 / INV-19: the revision guard belongs to one connection, so after the reset the next connection's
        // first revision 0 snapshot is applied instead of being ignored as older.
        [Test]
        public void NewConnectionAfterReset_AppliesItsRevisionZeroSnapshot()
        {
            var old = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var previous = new ServerInventory(7, 70, (ItemId.Material, 3));
            Deliver(old, previous.Wire());
            _queue.Drain();
            previous.AssertShownBy(mirror, "fixture: previous connection");
            old.Disconnect();
            _queue.Drain();

            NetworkService.ResetGlobalSessionMirrors();
            AssertUnsyncedAndEmpty(mirror, "R1 after the reset");
            var next = Session();
            var first = new ServerInventory(0, 0);
            Deliver(next, first.Wire());
            _queue.Drain();

            first.AssertShownBy(mirror, "INV-19 new connection revision 0");
        }

        // R6/R7 with INV-19: S_ItemUseResult is a result signal only. The shown inventory changes when the
        // server snapshot arrives: no local -1 stack, +50 currency or revision taken from a result.
        [Test]
        public void ItemUseResults_NeverChangeTheMirror_OnlyTheFollowingSnapshotDoes()
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var owned = new ServerInventory(2, 10, (ItemId.CoinPouch, 1));
            Deliver(session, owned.Wire());
            _queue.Drain();
            owned.AssertShownBy(mirror, "fixture: one CoinPouch owned");

            Deliver(session, InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 3));
            _queue.Drain();
            owned.AssertShownBy(mirror, "R6 Success result without its snapshot");
            Deliver(session, InventoryWire.ItemUseResult(1, (int)ItemId.CoinPouch, 2));
            Deliver(session, InventoryWire.ItemUseResult(3, (int)ItemId.Material, 2));
            _queue.Drain();
            owned.AssertShownBy(mirror, "R7 Stale and denial results");

            var used = new ServerInventory(3, 60);
            Deliver(session, used.Wire());
            _queue.Drain();
            used.AssertShownBy(mirror, "R7 snapshot following Success");
        }

        // R7: a result with a wrong length/header or an undefined code is dropped without an exception and
        // changes nothing.
        [TestCase("length 12")]
        [TestCase("length 14")]
        [TestCase("header size 20 on 13 bytes")]
        [TestCase("unknown result 7")]
        public void MalformedItemUseResult_IsDroppedWithoutException(string malformed)
        {
            var session = Session();
            InventoryMirrorProbe mirror = _mirror.Require();
            var owned = new ServerInventory(2, 10, (ItemId.CoinPouch, 1));
            Deliver(session, owned.Wire());
            _queue.Drain();
            owned.AssertShownBy(mirror, "fixture: one CoinPouch owned");

            DeliverAllowingDiagnostics(session, s_malformedResults[malformed](), "R7");

            owned.AssertShownBy(mirror, $"R7 malformed result ({malformed})");
        }

        // R2 at the receive boundary (opus-pr2-review O1, corrected in sol-pr2-fix1 report "O1"): OnRecvPacket
        // drops a default segment or one shorter than the 4-byte header before reading an id. One segment views
        // only the first 3 bytes of a whole valid snapshot, so reading past it would find id 36 and a full frame.
        // Nothing may throw, reach the main queue or change the mirror; the connection stays usable.
        [Test]
        public void ReceiveSegmentsShorterThanAHeader_AreDroppedBeforeDispatch_AndTheNextSnapshotApplies()
        {
            int posted = 0;
            var session = new UnityClientSession(() => true, action => { posted++; _queue.Post(action); });
            _sessions.Add(session);
            session.Publish();
            SessionTestTools.Handshake(session, _queue);
            InventoryMirrorProbe mirror = _mirror.Require();
            byte[] wholeSnapshot = InventoryWire.Snapshot(1, 10, (1, 2));
            var segments = new Dictionary<string, ArraySegment<byte>>
            {
                ["default segment"] = default,
                ["empty array"] = new ArraySegment<byte>(Array.Empty<byte>()),
                ["1-byte array"] = new ArraySegment<byte>(new byte[1]),
                ["3-byte array"] = new ArraySegment<byte>(new byte[3]),
                ["first 2 bytes of a snapshot"] = new ArraySegment<byte>(wholeSnapshot, 0, 2),
                ["first 3 bytes of a snapshot"] = new ArraySegment<byte>(wholeSnapshot, 0, 3),
                ["3 bytes at an inner offset"] = new ArraySegment<byte>(wholeSnapshot, 40, 3),
            };
            int postedBefore = posted;

            foreach (KeyValuePair<string, ArraySegment<byte>> item in segments)
            {
                Assert.DoesNotThrow(() => session.OnRecvPacket(item.Value), $"R2 {item.Key}: receive boundary must not throw");
            }
            int postedByShortSegments = posted - postedBefore;
            _queue.Drain();
            bool closed = session.IsClosed;
            bool synchronized = mirror.HasSnapshot;

            Assert.AreEqual(0, postedByShortSegments, "O1 no handler or unknown-id diagnostic runs for a segment without a header");
            Assert.IsFalse(closed, "R2 a short segment does not close the connection");
            Assert.IsFalse(synchronized, "R2 nothing from a short segment reaches the mirror");
            var next = new ServerInventory(1, 10, (ItemId.Material, 2));
            Deliver(session, next.Wire());
            _queue.Drain();
            next.AssertShownBy(mirror, "R2 the next whole snapshot still applies");
        }

        static byte[] NewerValidSnapshot() => InventoryWire.Snapshot(2, 20, (1, 2));

        static void Deliver(UnityClientSession session, byte[] wire) => session.OnRecvPacket(new ArraySegment<byte>(wire));

        static void AssertUnsyncedAndEmpty(InventoryMirrorProbe mirror, string requirement)
        {
            bool synced = mirror.HasSnapshot;
            Assert.IsFalse(synced, $"{requirement}: unsynchronized");
            Assert.AreEqual(0u, mirror.Revision, $"{requirement}: revision");
            Assert.AreEqual(0, mirror.Currency, $"{requirement}: currency");
            for (int slot = 0; slot < InventoryWire.SlotCount; slot++)
            {
                InventorySlot shown = mirror.Slot(slot);
                Assert.AreEqual(ItemId.None, shown.ItemId, $"{requirement}: slot {slot} item");
                Assert.AreEqual(0, shown.Count, $"{requirement}: slot {slot} count");
            }
        }

        static void AssertOneInventoryQuery(TestSocketPair sockets, string requirement)
        {
            List<byte[]> frames = PeerFrames.Take(sockets);
            int queries = PeerFrames.Count(frames, InventoryWire.InventoryRequestId);
            Assert.AreEqual(1, queries, $"{requirement}: C_InventoryRequest count on the current connection");
            byte[] query = frames.Find(frame => InventoryWire.Id(frame) == InventoryWire.InventoryRequestId);
            CollectionAssert.AreEqual(InventoryWire.InventoryRequest, query, $"{requirement}: reserved 0, ID 35, length 5");
        }

        static void AssertNoInventoryQuery(TestSocketPair sockets, string requirement)
        {
            int queries = PeerFrames.Count(PeerFrames.Take(sockets), InventoryWire.InventoryRequestId);
            Assert.AreEqual(0, queries, $"{requirement}: no C_InventoryRequest");
        }

        T Track<T>(T fixture) where T : IDisposable
        {
            _fixtures.Add(fixture);
            return fixture;
        }

        UnityClientSession Session(Func<bool> isCurrent = null)
        {
            var session = new UnityClientSession(isCurrent ?? (() => true), _queue.Post);
            _sessions.Add(session);
            session.Publish();
            SessionTestTools.Handshake(session, _queue);
            return session;
        }

        // A published, handshaken session on a real loopback socket with its entry fixture.
        (UnityClientSession Session, TestSocketPair Sockets, EntryBindingFixture Views) GameplaySession()
        {
            TestSocketPair sockets = Track(new TestSocketPair());
            var session = new UnityClientSession(() => true, _queue.Post);
            _sessions.Add(session);
            session.Start(sockets.Client);
            session.Publish();
            SessionTestTools.Handshake(session, _queue);
            EntryBindingFixture views = Track(new EntryBindingFixture(session));
            return (session, sockets, views);
        }

        // Malformed server input may be diagnosed with any log level, but must not throw at the receive
        // boundary or during the main-thread apply. Only this delivery window ignores failing log messages.
        void DeliverAllowingDiagnostics(UnityClientSession session, byte[] wire, string requirement)
        {
            bool previousLogPolicy = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => Deliver(session, wire), $"{requirement}: receive boundary must not throw");
                Assert.DoesNotThrow(() => _queue.Drain(), $"{requirement}: main-thread apply must not throw");
            }
            finally { LogAssert.ignoreFailingMessages = previousLogPolicy; }
        }
    }
}
