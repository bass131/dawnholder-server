using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent PR2 checks for what the pre-implementation suite left open: bounded recovery of unanswered
    // queries/uses (R5), use offering and server-result ordering (R6/R7) and the panel's presentation and
    // subscription lifetime (R6/R8). Packets enter the real UnityClientSession.OnRecvPacket; readiness goes
    // through the session's MapEntryCoordinator barrier; the session's delayed callbacks are captured so a
    // test decides when their time has elapsed; client sends are read from a loopback peer. Expected bytes,
    // names and values are wire/catalog/server literals (pr1-acceptance.md, Shared ItemCatalog), never output
    // of the code under test. Real scenes, pointer input and Unity lifecycle are covered in PlayMode.
    public sealed class InventoryPresentationTests
    {
        const ushort ItemUseId = 37;

        readonly List<UnityClientSession> _sessions = new();
        readonly List<IDisposable> _fixtures = new();
        readonly List<(Action Callback, float Seconds)> _scheduled = new();
        readonly List<GameObject> _objects = new();
        ManualConnectionQueue _queue;
        InventoryMirrorProbe _mirror;
        UnityClientSession _session;
        TestSocketPair _sockets;
        EntryBindingFixture _views;
        int _latency;

        InventoryRequestController Requests => _session.Inventory;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0; // direct sends; every captured callback is an inventory timeout
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
                for (int i = _objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
                _objects.Clear();
                for (int i = _fixtures.Count - 1; i >= 0; i--) _fixtures[i].Dispose();
                _fixtures.Clear();
                _mirror?.Dispose();
                _scheduled.Clear();
                UnityClientSession.SimulatedLatencyMs = _latency;
            }
        }

        // R5 (client.md "응답이 없으면 … 한 회복 구간당 조회를 최대 3번 시도하고 멈춘다"): an unanswered query is
        // retried on a timer, never per frame, stops after the bounded round, and a user refresh starts a new one.
        [Test]
        public void UnansweredQuery_RetriesAtMostThreeTimesInOneReady_ThenRefreshStartsANewRound()
        {
            StartGameplaySession();
            ReadyMap(0);
            Assert.AreEqual(1, Count(Sent(), InventoryWire.InventoryRequestId), "R4 query on Ready");

            ElapseScheduled();
            int second = Count(Sent(), InventoryWire.InventoryRequestId);
            ElapseScheduled();
            int third = Count(Sent(), InventoryWire.InventoryRequestId);
            ElapseScheduled();
            int afterRound = Count(Sent(), InventoryWire.InventoryRequestId);
            bool nothingLeftScheduled = _scheduled.Count == 0;

            Assert.AreEqual(1, second, "R5 second query after the first timeout");
            Assert.AreEqual(1, third, "R5 third query after the second timeout");
            Assert.AreEqual(0, afterRound, "R5 the round stops after three queries");
            Assert.IsTrue(nothingLeftScheduled, "R5 no timer keeps running after the bounded round");
            Assert.IsFalse(_mirror.Require().HasSnapshot, "R5 sending is not synchronization");
            Assert.IsTrue(Requests.HasTimedOut, "R5 the missing answer is visible");
            Assert.IsFalse(Requests.IsWaiting, "R5 nothing is pending, so the panel can offer refresh");

            Assert.IsTrue(Requests.Refresh(), "R5 a user refresh is accepted after the round");
            Assert.AreEqual(1, Count(Sent(), InventoryWire.InventoryRequestId), "R5 refresh starts a new round");
            Answer(new ServerInventory(0, 0));
            Assert.IsTrue(Requests.IsSynchronized, "R1 revision 0 answer confirms the state");
            Assert.IsFalse(Requests.HasTimedOut, "R5 an answer clears the timeout");
        }

        // R5/R7 with the PR1 busy drop: a use whose answer never arrives is not sent again (it may have
        // committed); its timeout queries instead, and either server outcome is shown without a permanent lock.
        [TestCase(true)]
        [TestCase(false)]
        public void UseWithoutAnswer_IsNotResent_AndTheTimeoutQueryRecovers(bool committed)
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            Answer(new ServerInventory(1, 10, (ItemId.CoinPouch, 1)));

            bool attempted = Requests.TryUse(ItemId.CoinPouch);
            bool duplicate = Requests.TryUse(ItemId.CoinPouch);
            List<byte[]> useFrames = Sent();
            Assert.IsTrue(attempted, "R6 owned CoinPouch on a synchronized Ready entry");
            Assert.IsFalse(duplicate, "R6 a second click while the first waits is refused");
            Assert.AreEqual(1, Count(useFrames, ItemUseId), "R6 exactly one C_ItemUse");
            CollectionAssert.AreEqual(ItemUseWire(2, 1), Frame(useFrames, ItemUseId), "R6 C_ItemUse(CoinPouch, current revision 1)");

            ElapseScheduled();
            List<byte[]> recovery = Sent();
            Assert.AreEqual(0, Count(recovery, ItemUseId), "R5/R7 an unanswered use is never repeated");
            Assert.AreEqual(1, Count(recovery, InventoryWire.InventoryRequestId), "R5 the timeout queries the server state");
            Assert.IsFalse(Requests.IsUsing, "R5 the use no longer blocks input");
            Assert.IsFalse(Requests.CanUse(ItemId.CoinPouch), "R5 not usable before the server confirms the state");

            if (committed)
            {
                var used = new ServerInventory(2, 60);
                Answer(used);
                used.AssertShownBy(_mirror.Require(), "R6 the committed use is shown from the snapshot");
                Assert.IsTrue(Requests.IsSynchronized, "R5 recovered");
                Assert.IsFalse(Requests.CanUse(ItemId.CoinPouch), "R6 no CoinPouch left to use");
            }
            else
            {
                var unchanged = new ServerInventory(1, 10, (ItemId.CoinPouch, 1));
                Answer(unchanged);
                unchanged.AssertShownBy(_mirror.Require(), "R6 nothing changed on the server");
                Assert.IsTrue(Requests.IsSynchronized, "R5 the same revision confirms the state again");
                Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch), "R5 the next use is not locked");
                Assert.AreEqual(1, Count(Sent(), ItemUseId), "R5 the next use is sent");
            }
        }

        // R6/R7 (pr1-acceptance.md "성공 사용은 Success 결과 뒤 새 snapshot"): Success alone changes no shown
        // value and does not confirm the state; an older snapshot does not either; a lost snapshot is requeried.
        [Test]
        public void SuccessResult_ChangesNothingUntilItsSnapshot_AndALostSnapshotIsRequeried()
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            var owned = new ServerInventory(1, 10, (ItemId.CoinPouch, 1));
            Answer(owned);
            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch));
            Sent();

            Deliver(InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 2));
            Answer(owned); // a delayed push of the old revision 1
            owned.AssertShownBy(_mirror.Require(), "R6 no local -1 stack or +50 currency");
            Assert.AreEqual(InventoryResult.Success, Requests.LastResult, "R7 the server Success is shown");
            Assert.IsFalse(Requests.IsSynchronized, "R7 revision 1 cannot confirm a revision 2 result");
            Assert.IsFalse(Requests.CanUse(ItemId.CoinPouch), "R6 no use until the following snapshot");

            ElapseScheduled();
            Assert.AreEqual(1, Count(Sent(), InventoryWire.InventoryRequestId), "R5 lost snapshot is requeried");
            var used = new ServerInventory(2, 60);
            Answer(used);
            used.AssertShownBy(_mirror.Require(), "R6 the snapshot after Success");
            Assert.IsTrue(Requests.IsSynchronized, "R7 confirmed by the revision 2 snapshot");
            Assert.AreEqual(InventoryResult.Success, Requests.LastResult, "R7 Success stays shown");
        }

        // R7 (pr1-acceptance.md "Stale은 현재 revision 결과 뒤 현재 snapshot"): a kill push that overtakes a use
        // makes it Stale; the current snapshot re-enables use and the client does not repeat the use itself.
        [Test]
        public void StaleResultAfterAKillPush_ReenablesUseAtTheCurrentRevisionWithoutAutoRetry()
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            Answer(new ServerInventory(1, 10, (ItemId.CoinPouch, 1)));
            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch));
            Sent();

            var current = new ServerInventory(2, 20, (ItemId.Material, 1), (ItemId.CoinPouch, 1));
            Deliver(current.Wire()); // kill reward push before the use reached the server tick
            Deliver(InventoryWire.ItemUseResult(1, (int)ItemId.CoinPouch, 2));
            Deliver(current.Wire()); // the current snapshot that follows Stale
            _queue.Drain();

            current.AssertShownBy(_mirror.Require(), "R7 current server state");
            Assert.AreEqual(InventoryResult.Stale, Requests.LastResult, "R7 Stale is shown");
            Assert.IsTrue(Requests.IsSynchronized, "R7 the current snapshot confirms the state");
            Assert.IsTrue(Requests.CanUse(ItemId.CoinPouch), "R7 the user may choose again");
            Assert.AreEqual(0, Count(Sent(), ItemUseId), "R7 the client never repeats a use by itself");
        }

        // R7 (pr1-acceptance.md "CurrencyCap … 결과만 보내고 상태를 보존"): a denial needs no snapshot, keeps the
        // shown values, and the same denial for the next attempt at the unchanged revision is shown again.
        [Test]
        public void DenialResult_IsShownWithoutASnapshot_AndARepeatedDenialIsNotIgnored()
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            var full = new ServerInventory(7, 999_999_990, (ItemId.CoinPouch, 1));
            Answer(full);

            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch));
            Deliver(InventoryWire.ItemUseResult(4, (int)ItemId.CoinPouch, 7));
            _queue.Drain();
            Assert.AreEqual(InventoryResult.CurrencyCap, Requests.LastResult, "R7 first denial");
            Assert.IsFalse(Requests.IsUsing, "R7 the denial answered the use");
            Assert.IsTrue(Requests.IsSynchronized, "R7 a denial leaves the confirmed state as is");
            full.AssertShownBy(_mirror.Require(), "R6 denial changes nothing");

            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch), "R7 the next attempt is allowed");
            Deliver(InventoryWire.ItemUseResult(4, (int)ItemId.CoinPouch, 7));
            _queue.Drain();
            List<byte[]> frames = Sent();
            Assert.AreEqual(2, Count(frames, ItemUseId), "fixture: two uses were sent");
            Assert.AreEqual(InventoryResult.CurrencyCap, Requests.LastResult, "R7 second denial is shown");
            Assert.IsFalse(Requests.IsUsing, "R7 the second denial is not ignored as a duplicate");
            ElapseScheduled();
            Assert.AreEqual(0, Sent().Count, "R7 an answered denial starts no recovery traffic");
        }

        // #1 with R5 on the session's own deadline owner (no injected clock). The deadline stays off the send
        // channel and a snapshot removes it. A clock that has passed every deadline still yields only the three
        // queries of one round and then nothing. MainThreadDispatcher calls DrainDue with the real clock each
        // frame; the real 5 s spacing is checked in PlayMode (InventorySceneLifecycleTests).
        [Test]
        public void SessionOwnedDeadline_IsRemovedBySnapshot_AndAnExpiredRoundStopsAtThreeQueries()
        {
            StartGameplaySession(manualDeadlineClock: false);
            ReadyMap(0);
            int readyQuery = Count(Sent(), InventoryWire.InventoryRequestId);
            bool sendChannelEmpty = _scheduled.Count == 0;
            Answer(new ServerInventory(1, 10, (ItemId.CoinPouch, 1)));
            InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 3600);
            int afterAnswer = Count(Sent(), InventoryWire.InventoryRequestId);

            Assert.AreEqual(1, readyQuery, "R4 query on Ready");
            Assert.IsTrue(sendChannelEmpty, "#1 the deadline never enters the send channel");
            Assert.AreEqual(0, afterAnswer, "R5 the snapshot removed the pending deadline");
            Assert.IsTrue(Requests.IsSynchronized, "R5 answered");

            Assert.IsTrue(Requests.Refresh(), "R5 a refresh starts a new round");
            int refreshQuery = Count(Sent(), InventoryWire.InventoryRequestId);
            InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 3600);
            int expiredRound = Count(Sent(), InventoryWire.InventoryRequestId);
            InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 7200);
            int afterRound = Count(Sent(), InventoryWire.InventoryRequestId);

            Assert.AreEqual(1, refreshQuery, "R5 the refresh query");
            Assert.AreEqual(2, expiredRound, "R5 expired deadlines add the round's second and third query only");
            Assert.AreEqual(0, afterRound, "R5 no deadline is left after the bounded round");
            Assert.IsTrue(Requests.HasTimedOut, "R5 the missing answer is visible");
            Assert.IsFalse(Requests.IsWaiting, "R5 nothing is pending, so refresh is offered again");
        }

        // R3/R5 termination: a connection closed while its deadline is live leaves nothing that acts later.
        [Test]
        public void SessionOwnedDeadline_OfAClosedConnectionNeverActs()
        {
            StartGameplaySession(manualDeadlineClock: false);
            ReadyMap(0);
            int readyQuery = Count(Sent(), InventoryWire.InventoryRequestId);
            _session.Disconnect();
            _queue.Drain();
            _session.Cleanup();

            Assert.DoesNotThrow(() => InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 3600),
                "R5 a later drain is safe");
            int afterClose = Count(Sent(), InventoryWire.InventoryRequestId);
            Assert.AreEqual(1, readyQuery, "R4 query on Ready");
            Assert.AreEqual(0, afterClose, "R3/R5 a closed connection's deadline sends nothing");
            Assert.IsFalse(Requests.IsWaiting, "R5 cleanup leaves no pending deadline");
        }

        // #1 boundary itself (InventoryTimeoutScheduler): due callbacks run once in deadline order, each is
        // removed before it runs so it may reschedule without running again in the same drain, a throwing
        // callback is isolated, and a replacement or cancel leaves one or no registration for its owner.
        [Test]
        public void TimeoutScheduler_RunsDueCallbacksOnceInDeadlineOrder_AndIsolatesAFailure()
        {
            var calls = new List<string>();
            var owners = new List<InventoryTimeoutScheduler>();
            InventoryTimeoutScheduler Owner()
            {
                var owner = new InventoryTimeoutScheduler();
                owners.Add(owner);
                return owner;
            }
            try
            {
                InventoryTimeoutScheduler later = Owner(), sooner = Owner(), failing = Owner();
                InventoryTimeoutScheduler replaced = Owner(), cancelled = Owner();
                later.Schedule(() => calls.Add("later"), 0.4f);
                sooner.Schedule(() =>
                {
                    calls.Add("sooner");
                    sooner.Schedule(() => calls.Add("sooner again"), 60f);
                }, 0.1f);
                failing.Schedule(() =>
                {
                    calls.Add("failing");
                    throw new InvalidOperationException("deadline callback failure");
                }, 0.2f);
                replaced.Schedule(() => calls.Add("replaced old"), 0.05f);
                replaced.Schedule(() => calls.Add("replaced new"), 0.3f);
                cancelled.Schedule(() => calls.Add("cancelled"), 0.15f);
                cancelled.Cancel();
                LogAssert.Expect(LogType.Exception, new Regex("deadline callback failure"));

                InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 1);
                string[] firstDrain = calls.ToArray();
                InventoryTimeoutScheduler.DrainDue(Time.realtimeSinceStartupAsDouble + 1);

                CollectionAssert.AreEqual(new[] { "sooner", "failing", "replaced new", "later" }, firstDrain,
                    "deadline order; a replacement runs once with its new callback; a cancelled owner never runs");
                Assert.AreEqual(firstDrain.Length, calls.Count, "nothing runs twice and the 60 s reschedule is not due");
            }
            finally
            {
                foreach (InventoryTimeoutScheduler owner in owners) owner.Cancel(); // no registration outlives the test
            }
        }

        // R7: a result older than the shown one, or a late Success for an already completed use, does not
        // overwrite the current result or answer the newer pending use.
        [Test]
        public void LowerOrRepeatedResults_DoNotOverrideTheShownResultOrAnswerANewerUse()
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            Answer(new ServerInventory(3, 0, (ItemId.CoinPouch, 2)));
            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch));
            Deliver(InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 4));
            Answer(new ServerInventory(4, 50, (ItemId.CoinPouch, 1)));

            Deliver(InventoryWire.ItemUseResult(1, (int)ItemId.CoinPouch, 3));
            _queue.Drain();
            Assert.AreEqual(InventoryResult.Success, Requests.LastResult, "R7 a lower Stale does not replace Success");
            Assert.AreEqual(4u, Requests.ResultRevision, "R7 shown result revision");

            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch));
            Deliver(InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 4));
            _queue.Drain();
            Assert.IsTrue(Requests.IsUsing, "R7 the earlier use's Success cannot answer the use sent at revision 4");

            Deliver(InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 5));
            _queue.Drain();
            Assert.IsFalse(Requests.IsUsing, "R7 the newer Success answers it");
            Assert.AreEqual(5u, Requests.ResultRevision, "R7 newer result revision");
        }

        // R3/R7: a result queued by a connection that is replaced before the main drain is never shown.
        [Test]
        public void ResultQueuedByAReplacedConnection_IsNeverShown()
        {
            bool oldCurrent = true;
            var old = PublishedSession(() => oldCurrent);
            old.OnRecvPacket(new ArraySegment<byte>(InventoryWire.ItemUseResult(0, (int)ItemId.CoinPouch, 1)));
            oldCurrent = false;
            var next = PublishedSession(() => true);
            _queue.Drain();

            Assert.IsNull(old.Inventory.LastResult, "R3 apply gate of the replaced connection");
            Assert.IsNull(next.Inventory.LastResult, "R7 the new connection has no result of the old one");
        }

        // R6: use is offered only for an owned usable item on the current gameplay-Ready entry whose query was
        // answered; Material, missing items, entries in progress and unconfirmed Ready entries send nothing.
        [Test]
        public void UseIsOfferedOnlyForAnOwnedUsableItemOnTheConfirmedCurrentEntry()
        {
            StartGameplaySession();
            var both = new ServerInventory(1, 10, (ItemId.Material, 1), (ItemId.CoinPouch, 1));
            Answer(both); // a push may precede the first entry
            bool beforeEntry = Requests.CanUse(ItemId.CoinPouch);
            _views.Begin(0);
            _views.BindViews();
            bool entering = Requests.CanUse(ItemId.CoinPouch);
            _session.ReceivePlayerHp(7, 37, 100);
            bool readyUnconfirmed = Requests.CanUse(ItemId.CoinPouch);
            Assert.AreEqual(1, Count(Sent(), InventoryWire.InventoryRequestId), "fixture: Ready query");
            Answer(both);
            bool confirmedPouch = Requests.CanUse(ItemId.CoinPouch);
            bool confirmedMaterial = Requests.CanUse(ItemId.Material);
            bool materialAttempt = Requests.TryUse(ItemId.Material);

            Assert.IsFalse(beforeEntry, "R6 no entry yet");
            Assert.IsFalse(entering, "R6 entry not Ready");
            Assert.IsFalse(readyUnconfirmed, "R6 Ready but its query is unanswered");
            Assert.IsTrue(confirmedPouch, "R6 owned CoinPouch on the confirmed entry");
            Assert.IsFalse(confirmedMaterial, "R6 Material is never usable");
            Assert.IsFalse(materialAttempt, "R6 Material use is refused locally");
            Assert.AreEqual(0, Count(Sent(), ItemUseId), "R6 nothing sent for Material");

            _views.Begin(1);
            Assert.IsFalse(Requests.TryUse(ItemId.CoinPouch), "R6 no use while the next map is entering");
            _views.Ready(31);
            Sent();
            Answer(new ServerInventory(2, 60, (ItemId.Material, 1)));
            Assert.IsFalse(Requests.CanUse(ItemId.CoinPouch), "R6 CoinPouch no longer owned");
            Assert.AreEqual(0, Count(Sent(), ItemUseId), "R6 nothing sent for the transition or a missing item");
        }

        // R7: a transport failure while using is not shown as success; the connection closes, and the next
        // connection can query and use again (the lifetime owner resets the shared mirror in between).
        [Test]
        public void TransportFailureDuringUse_ClosesWithoutSuccess_AndTheNextConnectionCanUse()
        {
            StartGameplaySession();
            ReadyMap(0);
            Sent();
            Answer(new ServerInventory(1, 10, (ItemId.CoinPouch, 1)));
            UnityClientSession failed = _session;
            InventoryRequestController failedRequests = Requests;

            _sockets.Client.Close();
            failedRequests.TryUse(ItemId.CoinPouch);
            Assert.IsTrue(SpinWaitClosed(failed), "fixture: the transport failure closes the session");
            _queue.Drain();
            Assert.IsNull(failedRequests.LastResult, "R7 no result, in particular no Success, is shown");
            Assert.IsFalse(failedRequests.CanUse(ItemId.CoinPouch), "R7 the closed connection offers nothing");

            NetworkService.ResetGlobalSessionMirrors();
            _views.Dispose();
            _fixtures.Remove(_views);
            StartGameplaySession();
            ReadyMap(0);
            Assert.AreEqual(1, Count(Sent(), InventoryWire.InventoryRequestId), "R7 the next connection queries");
            Answer(new ServerInventory(1, 10, (ItemId.CoinPouch, 1)));
            Assert.IsTrue(Requests.TryUse(ItemId.CoinPouch), "R7 the next valid attempt is not blocked");
            Assert.AreEqual(1, Count(Sent(), ItemUseId), "R7 and it is sent");
        }

        // R6/R8: the view shows the server's catalog names, counts and currency, marks the unsynchronized
        // state, offers use only on the CoinPouch row, and its buttons report their own row.
        [Test]
        public void PanelView_ShowsServerNamesCountsAndCurrency_AndUseOnlyOnTheCoinPouchRow()
        {
            StartGameplaySession();
            var clicks = new List<int>();
            int refreshes = 0;
            GameObject root = Own(new GameObject("Inventory view under test", typeof(RectTransform)));
            var view = new InventoryPanelView(root, clicks.Add, () => refreshes++);

            view.Render(null, null, false);
            string unsyncedCurrency = Text(root, "Inventory/Currency");
            string unsyncedFirstSlot = Text(root, "Inventory/Slot1/Item");
            bool unsyncedUseShown = UseButton(root, 2).gameObject.activeSelf;

            ReadyMap(0);
            Sent();
            Answer(new ServerInventory(5, 120, (ItemId.Material, 3), (ItemId.CoinPouch, 2)));
            view.Render(InventoryState.Instance, Requests, true);

            Assert.AreEqual("재화 —", unsyncedCurrency, "R6 no currency before a snapshot");
            Assert.AreEqual("—", unsyncedFirstSlot, "R6 no slot values before a snapshot");
            Assert.IsFalse(unsyncedUseShown, "R6 no use offered before a snapshot");
            Assert.AreEqual("재화 120", Text(root, "Inventory/Currency"), "R6 server currency");
            Assert.AreEqual("재료 ×3", Text(root, "Inventory/Slot1/Item"), "R6 catalog name and server count");
            Assert.AreEqual("재화 주머니 ×2", Text(root, "Inventory/Slot2/Item"), "R6 catalog name and server count");
            for (int slot = 3; slot <= 8; slot++)
                Assert.AreEqual("빈 슬롯", Text(root, $"Inventory/Slot{slot}/Item"), $"R6 empty slot {slot}");
            Assert.IsFalse(UseButton(root, 1).gameObject.activeSelf, "R6 no use button for Material");
            Assert.IsTrue(UseButton(root, 2).gameObject.activeSelf, "R6 use button for CoinPouch");
            Assert.IsTrue(UseButton(root, 2).interactable, "R6 usable on a confirmed Ready entry");
            Assert.AreEqual("서버 확인 완료", Text(root, "Inventory/Synchronization"), "R6 confirmed state is marked");

            UseButton(root, 2).onClick.Invoke();
            CollectionAssert.AreEqual(new[] { 1 }, clicks, "R6 the CoinPouch row reports slot index 1");
            Assert.AreEqual(0, refreshes);
            view.Dispose();
        }

        // R8 (client.md: 실제 구독한 미러와 요청 controller를 보관해 … 같은 참조에서 해제): when the mirror or the
        // connection is replaced the panel leaves the old sources, and disabling it leaves the current ones.
        [Test]
        public void Panel_LeavesTheSameSourcesItSubscribed_OnReplacementAndDisable()
        {
            StartGameplaySession();
            GameObject parent = Own(new GameObject("Inventory panel parent (inactive)"));
            parent.SetActive(false); // EditMode: lifecycle messages are sent explicitly below
            InventoryPanel panel = InventoryPanel.BuildRuntime(parent.transform);
            InventoryState firstMirror = InventoryState.Instance;
            InventoryRequestController firstRequests = Requests;

            SessionTestTools.Call(panel, "OnEnable");
            int firstMirrorBound = Subscribers(firstMirror, "OnInventoryChanged");
            int firstRequestsBound = Subscribers(firstRequests, "Changed");

            _mirror.Dispose();
            _mirror = InventoryMirrorProbe.TryPublish();
            InventoryState secondMirror = InventoryState.Instance;
            UnityClientSession second = PublishedSession(() => true);
            SessionTestTools.Call(panel, "Update");
            int firstMirrorAfterSwap = Subscribers(firstMirror, "OnInventoryChanged");
            int firstRequestsAfterSwap = Subscribers(firstRequests, "Changed");
            int secondMirrorBound = Subscribers(secondMirror, "OnInventoryChanged");
            int secondRequestsBound = Subscribers(second.Inventory, "Changed");

            SessionTestTools.Call(panel, "OnDisable");
            int secondMirrorAfterDisable = Subscribers(secondMirror, "OnInventoryChanged");
            int secondRequestsAfterDisable = Subscribers(second.Inventory, "Changed");
            SessionTestTools.Call(panel, "OnDestroy");

            Assert.AreEqual(1, firstMirrorBound, "R8 one mirror subscription");
            Assert.AreEqual(1, firstRequestsBound, "R8 one request subscription");
            Assert.AreEqual(0, firstMirrorAfterSwap, "R8 the replaced (destroyed) mirror is left");
            Assert.AreEqual(0, firstRequestsAfterSwap, "R8 the replaced connection is left");
            Assert.AreEqual(1, secondMirrorBound, "R8 the new mirror is bound once");
            Assert.AreEqual(1, secondRequestsBound, "R8 the new connection is bound once");
            Assert.AreEqual(0, secondMirrorAfterDisable, "R8 disable leaves the current mirror");
            Assert.AreEqual(0, secondRequestsAfterDisable, "R8 disable leaves the current connection");
        }

        // C_ItemUse written from the PR1 wire table: size 12, id 37, int itemId, uint expectedRevision (LE).
        static byte[] ItemUseWire(int itemId, uint expectedRevision)
        {
            byte[] wire = { 0x0C, 0x00, 0x25, 0x00, 0, 0, 0, 0, 0, 0, 0, 0 };
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(4), itemId);
            BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(8), expectedRevision);
            return wire;
        }

        static int Count(List<byte[]> frames, ushort packetId) => PeerFrames.Count(frames, packetId);

        static byte[] Frame(List<byte[]> frames, ushort packetId) => frames.Find(frame => InventoryWire.Id(frame) == packetId);

        static string Text(GameObject root, string path) => root.transform.Find(path).GetComponent<TMP_Text>().text;

        static Button UseButton(GameObject root, int slotNumber) =>
            root.transform.Find($"Inventory/Slot{slotNumber}/Use").GetComponent<Button>();

        static int Subscribers(object owner, string eventField)
        {
            FieldInfo field = owner.GetType().GetField(eventField, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{owner.GetType().Name}.{eventField} event storage");
            return (field.GetValue(owner) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        static bool SpinWaitClosed(UnityClientSession session) =>
            System.Threading.SpinWait.SpinUntil(() => session.IsClosed, 3000);

        // R5: each captured callback is a scheduled response timeout; never a per-frame request.
        void ElapseScheduled()
        {
            var due = _scheduled.ToArray();
            _scheduled.Clear();
            foreach (var (callback, seconds) in due)
            {
                Assert.GreaterOrEqual(seconds, 1f, "R5 recovery is timed, not per frame");
                callback();
            }
        }

        List<byte[]> Sent() => PeerFrames.Take(_sockets);

        void Deliver(byte[] wire) => _session.OnRecvPacket(new ArraySegment<byte>(wire));

        void Answer(ServerInventory inventory)
        {
            Deliver(inventory.Wire());
            _queue.Drain();
        }

        void ReadyMap(byte map)
        {
            _views.Begin(map);
            _views.Ready();
        }

        T Track<T>(T fixture) where T : IDisposable
        {
            _fixtures.Add(fixture);
            return fixture;
        }

        GameObject Own(GameObject owned)
        {
            _objects.Add(owned);
            return owned;
        }

        UnityClientSession PublishedSession(Func<bool> isCurrent)
        {
            // The response deadline has its own boundary since PR2 fix 1; the same manual clock drives it.
            var session = new UnityClientSession(isCurrent, _queue.Post, (callback, seconds) => _scheduled.Add((callback, seconds)),
                (callback, seconds) => _scheduled.Add((callback, seconds)));
            _sessions.Add(session);
            session.Publish();
            SessionTestTools.Handshake(session, _queue);
            return session;
        }

        // A published, handshaken session on a real loopback socket with its entry fixture. Without the manual
        // deadline clock the session keeps its own InventoryTimeoutScheduler, as in the product.
        void StartGameplaySession(bool manualDeadlineClock = true)
        {
            _sockets = Track(new TestSocketPair());
            Action<Action, float> deadlines = null;
            if (manualDeadlineClock) deadlines = (callback, seconds) => _scheduled.Add((callback, seconds));
            _session = new UnityClientSession(() => true, _queue.Post, (callback, seconds) => _scheduled.Add((callback, seconds)),
                deadlines);
            _sessions.Add(_session);
            _session.Start(_sockets.Client);
            _session.Publish();
            SessionTestTools.Handshake(_session, _queue);
            _views = Track(new EntryBindingFixture(_session));
        }
    }
}
