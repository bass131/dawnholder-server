using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.Network.CharacterMenu;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.Tests
{
    // Independent re-verification of the PR2 fix for defect #1 (goal 2026-11-10-code-audit-client-fixes, item 7).
    // The fix lets CharacterMenuPresenter.Observe keep a closed connection's menu open while the session can still
    // deliver a reply received before the close. Requirement source: the main's judgement item in the re-verification
    // contract (a remote close with no result, an overlapping list result and an overlapping select result must each
    // end the menu in Disconnected at the first Observe after session cleanup, and an overlapping select success must
    // not accept the selection), with the goal's item 7 row (a result received before the close reaches the menu
    // once and nothing is sent again).
    // Real parts and controlled parts are the same as VerifyPr2ConnectionFlowTests: the real NetworkService getters,
    // ClientConnectionLifetime and UnityClientSession on a loopback socket pair, a connector that never dials and a
    // main-thread queue drained where MainThreadDispatcher.Update would. That file's probe is a private nested type
    // and the committed file is not edited here, so its setup is repeated below. Expected screens are never copied
    // from product text: each overlap run is compared with the same run where the menu looks only after the drain.
    public sealed class VerifyPr2FixMenuEndTests
    {
        static readonly byte[] LoginAccepted = { 5, 0, 42, 0, 0 };
        static readonly BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Public because it is a public test method's parameter.
        public enum LastExchange
        {
            // Ready with nothing pending, then the server closes.
            IdleThenClose,
            // A create is pending and the server closes without answering it.
            PendingCreateThenClose,
            // The first list's success arrives and the server closes right behind it.
            ListSuccessThenClose,
            // A select's success arrives and the server closes right behind it.
            SelectSuccessThenClose,
        }

        [TestCase(LastExchange.IdleThenClose)]
        [TestCase(LastExchange.PendingCreateThenClose)]
        public void RemoteCloseWithNoResult_TheFirstObserveAfterCleanupEndsTheMenu(LastExchange exchange)
        {
            CloseRun reference = RunRemoteClose(exchange, menuLooksBeforeTheDrain: false);
            CloseRun overlap = RunRemoteClose(exchange, menuLooksBeforeTheDrain: true);
            TestContext.Out.WriteLine($"{exchange}: renders while the close waited for cleanup = " +
                $"{overlap.RendersWhileWaiting}, phases after the close = {string.Join(",", overlap.PhasesAfterClose)}");

            Assert.IsTrue(overlap.ClosedBeforeCleanup, "fixture: the menu looked at a closed, not yet cleaned session");
            Assert.IsTrue(overlap.OwnerEnded, "fixture: the drain ended the connection owner");
            // Existence: the end is rendered at the first Observe after cleanup, not later.
            Assert.AreEqual(1, overlap.PhasesAfterClose.Length, "the close renders exactly one new screen");
            Assert.AreEqual(Phase.Disconnected, overlap.PhaseAtFirstObserveAfterCleanup,
                "the first Observe after cleanup ends the menu");
            // Result: the same screen as when the menu first looks after the drain.
            Assert.AreEqual(reference.PhasesAfterClose, overlap.PhasesAfterClose, "same phases as the drained-first run");
            Assert.AreEqual(reference.FinalMessage, overlap.FinalMessage, "same text as the drained-first run");
            // State: nothing more happens afterwards.
            Assert.AreEqual(0, overlap.RendersAfterTheEnd, "a later Observe renders nothing more");
            Assert.AreEqual(0, overlap.BytesAfterTheClose, "nothing is sent after the close");
            Assert.AreEqual(0, overlap.Exits + overlap.PresenterDisconnects, "the menu does not leave or disconnect itself");
        }

        [Test]
        public void ListSuccessOverlappingTheClose_IsShownThenTheFirstObserveAfterCleanupEndsTheMenu()
        {
            CloseRun reference = RunRemoteClose(LastExchange.ListSuccessThenClose, menuLooksBeforeTheDrain: false);
            CloseRun overlap = RunRemoteClose(LastExchange.ListSuccessThenClose, menuLooksBeforeTheDrain: true);
            TestContext.Out.WriteLine($"list overlap: phases after the close = {string.Join(",", overlap.PhasesAfterClose)}");

            Assert.IsTrue(overlap.ClosedBeforeCleanup, "fixture: the menu looked at a closed, not yet cleaned session");
            Assert.IsTrue(overlap.OwnerEnded, "fixture: the drain ended the connection owner");
            CollectionAssert.Contains(reference.PhasesAfterClose, Phase.Ready, "fixture: the drained-first run shows the list");
            // Existence: the list received before the close is shown although the menu saw the close first.
            Assert.AreEqual(reference.PhasesAfterClose, overlap.PhasesAfterClose,
                "the overlap shows the same screens as the drained-first run");
            Assert.AreEqual("가나", overlap.FirstSlotNameShown, "the listed character from the wire is on the screen");
            // State: the menu then ends at the first Observe after cleanup.
            Assert.AreEqual(Phase.Disconnected, overlap.PhaseAtFirstObserveAfterCleanup,
                "the first Observe after cleanup ends the menu");
            Assert.AreEqual(reference.FinalMessage, overlap.FinalMessage, "same final text as the drained-first run");
            Assert.AreEqual(0, overlap.RendersAfterTheEnd, "a later Observe renders nothing more");
            Assert.AreEqual(0, overlap.BytesAfterTheClose, "nothing is sent after the close");
        }

        [Test]
        public void SelectSuccessOverlappingTheClose_IsShownWithoutLeavingThenTheFirstObserveAfterCleanupEndsTheMenu()
        {
            CloseRun reference = RunRemoteClose(LastExchange.SelectSuccessThenClose, menuLooksBeforeTheDrain: false);
            CloseRun overlap = RunRemoteClose(LastExchange.SelectSuccessThenClose, menuLooksBeforeTheDrain: true);
            TestContext.Out.WriteLine($"select overlap: phases after the close = {string.Join(",", overlap.PhasesAfterClose)}");

            Assert.IsTrue(overlap.ClosedBeforeCleanup, "fixture: the menu looked at a closed, not yet cleaned session");
            Assert.IsTrue(overlap.OwnerEnded, "fixture: the drain ended the connection owner");
            CollectionAssert.Contains(reference.PhasesAfterClose, Phase.Entering,
                "fixture: the drained-first run shows the select");
            // Existence: the select result received before the close is shown although the menu saw the close first.
            Assert.AreEqual(reference.PhasesAfterClose, overlap.PhasesAfterClose,
                "the overlap shows the same screens as the drained-first run");
            // Result: the closed connection gets no follow-up from the select, and the menu does not leave.
            Assert.AreEqual(0, overlap.BytesAfterTheClose, "nothing is sent after the close");
            Assert.AreEqual(0, overlap.Exits, "the menu does not leave for another screen");
            Assert.AreEqual(0, overlap.PresenterDisconnects, "the menu does not disconnect itself");
            // State: the menu then ends at the first Observe after cleanup.
            Assert.AreEqual(Phase.Disconnected, overlap.PhaseAtFirstObserveAfterCleanup,
                "the first Observe after cleanup ends the menu");
            Assert.AreEqual(reference.FinalMessage, overlap.FinalMessage, "same final text as the drained-first run");
            Assert.AreEqual(0, overlap.RendersAfterTheEnd, "a later Observe renders nothing more");
        }

        [Test]
        public void ClickWhileTheCloseWaitsForCleanup_SendsNothingAndTheMenuStillEnds()
        {
            // The implementer's remaining concern: a click in the frames between the close and cleanup. What the menu
            // shows in between is only recorded; the requirement is that nothing is sent and the end still comes.
            CloseRun reference = RunRemoteClose(LastExchange.IdleThenClose, menuLooksBeforeTheDrain: false);
            CloseRun overlap = RunRemoteClose(LastExchange.IdleThenClose, menuLooksBeforeTheDrain: true,
                clickWhileWaiting: true);
            TestContext.Out.WriteLine($"click while waiting: renders while waiting = {overlap.RendersWhileWaiting}, " +
                $"phases after the close = {string.Join(",", overlap.PhasesAfterClose)}, " +
                $"texts after the close = {string.Join(" | ", overlap.MessagesAfterClose)}");

            Assert.IsTrue(overlap.ClosedBeforeCleanup, "fixture: the menu looked at a closed, not yet cleaned session");
            Assert.IsTrue(overlap.OwnerEnded, "fixture: the drain ended the connection owner");
            Assert.AreEqual(0, overlap.BytesAfterTheClose, "the click sends nothing on the closed connection");
            Assert.AreEqual(Phase.Disconnected, overlap.PhaseAtFirstObserveAfterCleanup,
                "the first Observe after cleanup still ends the menu");
            Assert.AreEqual(reference.FinalMessage, overlap.FinalMessage, "the end screen is the same as without the click");
            Assert.AreEqual(0, overlap.RendersAfterTheEnd, "a later Observe renders nothing more");
            Assert.AreEqual(0, overlap.Exits + overlap.PresenterDisconnects, "the menu does not leave or disconnect itself");
        }

        [Test]
        public void SelectSuccessAfterTheSendGateCloses_IsDeliveredButNotAccepted()
        {
            // The client's own contract with both gates under test control: the session closes sending first and
            // receiving only at cleanup, which disposes the client.
            bool canSend = true;
            bool canReceive = true;
            var accepted = new List<string>();
            int disconnects = 0;
            var client = new CharacterMenuClient(() => canSend, _ => { }, () => disconnects++, accepted.Add,
                () => canReceive);
            int notifications = 0;
            client.Changed += () => notifications++;
            Assert.IsTrue(client.TrySelect(CharacterMenuWire.Id), "fixture: the select is pending");
            Assert.AreEqual(1, notifications, "fixture: the send notified once");
            bool waitingBeforeTheClose = client.IsClosedAwaitingSessionCleanup;

            canSend = false;
            bool waitingAfterTheClose = client.IsClosedAwaitingSessionCleanup;
            bool delivered = client.TryReceive(Reply(CharacterMenuWire.Select(client.PendingRequestId)));
            client.Dispose();
            bool waitingAfterCleanup = client.IsClosedAwaitingSessionCleanup;

            Assert.IsFalse(waitingBeforeTheClose, "an open connection is not waiting for cleanup");
            Assert.IsTrue(waitingAfterTheClose, "closed for sending but not cleaned: the client waits");
            Assert.IsTrue(delivered, "the reply received before the close is delivered");
            Assert.AreEqual(2, notifications, "the delivery notifies the menu once");
            Assert.IsEmpty(accepted, "a select success on a closed connection is not accepted");
            Assert.AreEqual(0, disconnects, "the closed connection is not disconnected again");
            Assert.IsFalse(waitingAfterCleanup, "cleanup (disposal) ends the wait");

            // Positive control: the same reply on an open connection is accepted once.
            var openAccepted = new List<string>();
            var open = new CharacterMenuClient(() => true, _ => { }, () => { }, openAccepted.Add, () => true);
            Assert.IsTrue(open.TrySelect(CharacterMenuWire.Id), "control: the select is pending");
            Assert.IsTrue(open.TryReceive(Reply(CharacterMenuWire.Select(open.PendingRequestId))), "control: delivered");
            Assert.AreEqual(1, openAccepted.Count, "control: an open connection accepts it once");
            Assert.AreEqual(CharacterMenuWire.Id, openAccepted[0], "control: the accepted ID is the selected one");
        }

        [Test]
        public void FourDelegateClient_NeverWaitsForCleanup()
        {
            // Preserved meaning: with one gate for both directions a closed client neither waits nor receives.
            bool open = true;
            var client = new CharacterMenuClient(() => open, _ => { }, () => { }, _ => { });
            Assert.IsTrue(client.TryList(), "fixture: the list is pending");
            uint requestId = client.PendingRequestId;

            open = false;
            bool waiting = client.IsClosedAwaitingSessionCleanup;
            bool delivered = client.TryReceive(Reply(CharacterMenuWire.List(requestId, 0)));

            Assert.IsFalse(waiting, "the four-delegate client is never waiting for cleanup");
            Assert.IsFalse(delivered, "the four-delegate client delivers nothing once its gate closes");
            Assert.IsNull(client.LastReply, "no reply was recorded");
        }

        static CharacterMenuReply Reply(byte[] frame)
        {
            Assert.IsTrue(CharacterMenuReplyReader.TryRead(new ArraySegment<byte>(frame), out CharacterMenuReply reply),
                "fixture: valid reply bytes");
            return reply;
        }

        // One connection whose server ends with a real EOF after the given exchange. menuLooksBeforeTheDrain puts one
        // CharacterSelectController.Update-style Observe between the receive thread's close and the queue drain, the
        // order VerifyPr2ConnectionFlowTests explains (the receive thread runs outside Unity's frame).
        static CloseRun RunRemoteClose(LastExchange exchange, bool menuLooksBeforeTheDrain, bool clickWhileWaiting = false)
        {
            using var probe = new FacadeProbe();
            var states = new List<CharacterMenuViewState>();
            int exits = 0;
            int presenterDisconnects = 0;
            using var presenter = new CharacterMenuPresenter(states.Add, () => presenterDisconnects++, _ => exits++);
            UnityClientSession session = probe.ConnectAndAccept();
            TestSocketPair wire = probe.Sockets.Last();
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            CharacterMenuClient menu = probe.Service.CharacterMenu;
            Assert.IsNotNull(menu, "fixture: the accepted login opened the menu client");

            byte[] lastReply = null;
            if (exchange == LastExchange.ListSuccessThenClose)
            {
                lastReply = CharacterMenuWire.List(menu.PendingRequestId, 1);
            }
            else
            {
                wire.Peer.Send(CharacterMenuWire.List(menu.PendingRequestId, 1));
                probe.Queue.WaitThenDrain();
                Assert.AreEqual(Phase.Ready, states.Last().CurrentPhase, "fixture: the one-character list makes it ready");
                if (exchange == LastExchange.PendingCreateThenClose)
                {
                    presenter.Create(2, 0, "다라");
                    Assert.AreEqual(51, wire.ReadFrame()[2], "fixture: the create request is on the wire");
                }
                else if (exchange == LastExchange.SelectSuccessThenClose)
                {
                    presenter.Select(1);
                    Assert.AreEqual(53, wire.ReadFrame()[2], "fixture: the select request is on the wire");
                    lastReply = CharacterMenuWire.Select(menu.PendingRequestId);
                }
            }
            int rendersBeforeClose = states.Count;

            if (lastReply != null) wire.Peer.Send(lastReply);
            wire.Peer.Shutdown(SocketShutdown.Send);
            Assert.IsTrue(SpinWait.SpinUntil(() => session.IsClosed, 3000), "fixture: the receive thread saw the EOF");
            bool closedBeforeCleanup = !probe.Service.IsConnected && ReferenceEquals(menu, probe.Service.CharacterMenu);
            if (menuLooksBeforeTheDrain)
                presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            // A create in the empty third slot, clicked in the same frame.
            if (clickWhileWaiting) presenter.Create(3, 0, "마바");
            int rendersWhileWaiting = states.Count - rendersBeforeClose;

            bool ownerEnded = probe.DrainUntilTheOwnerEnds(session);
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            Phase phaseAtFirstObserveAfterCleanup = states.Last().CurrentPhase;
            int rendersAtTheEnd = states.Count;
            probe.Queue.Drain();
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);

            CharacterMenuViewState[] afterClose = states.Skip(rendersBeforeClose).ToArray();
            string firstSlotNameShown = afterClose.Select(state => state.GetSlot(1)?.Name).FirstOrDefault(name => name != null);
            return new CloseRun
            {
                ClosedBeforeCleanup = closedBeforeCleanup,
                OwnerEnded = ownerEnded,
                RendersWhileWaiting = rendersWhileWaiting,
                PhasesAfterClose = afterClose.Select(state => state.CurrentPhase).ToArray(),
                MessagesAfterClose = afterClose.Select(state => state.Message).ToArray(),
                PhaseAtFirstObserveAfterCleanup = phaseAtFirstObserveAfterCleanup,
                FinalMessage = states.Last().Message,
                FirstSlotNameShown = firstSlotNameShown,
                RendersAfterTheEnd = states.Count - rendersAtTheEnd,
                BytesAfterTheClose = BytesBeforeEof(wire),
                Exits = exits,
                PresenterDisconnects = presenterDisconnects,
            };
        }

        // Everything the client writes after the last request. The client closes its socket, so this ends at EOF;
        // a client that stayed open would time out here and fail the test instead of passing quietly.
        static int BytesBeforeEof(TestSocketPair wire)
        {
            var buffer = new byte[256];
            int total = 0;
            try
            {
                int read;
                while ((read = wire.Peer.Receive(buffer)) > 0) total += read;
            }
            catch (SocketException error) when (error.SocketErrorCode == SocketError.ConnectionReset) { }
            return total;
        }

        sealed class CloseRun
        {
            internal bool ClosedBeforeCleanup;
            internal bool OwnerEnded;
            internal int RendersWhileWaiting;
            internal Phase[] PhasesAfterClose;
            internal string[] MessagesAfterClose;
            internal Phase PhaseAtFirstObserveAfterCleanup;
            internal string FinalMessage;
            internal string FirstSlotNameShown;
            internal int RendersAfterTheEnd;
            internal int BytesAfterTheClose;
            internal int Exits;
            internal int PresenterDisconnects;
        }

        // Same setup as VerifyPr2ConnectionFlowTests.FacadeProbe: Awake's real CharacterMenuLoginAccepted delegate stays,
        // only the lifetime behind the facade is replaced with one on the controlled connector and queue. Reflection
        // builds this fixture only; every judgement reads public or internal behavior.
        sealed class FacadeProbe : IDisposable
        {
            internal readonly ManualConnectionQueue Queue = new();
            internal readonly List<TestSocketPair> Sockets = new();
            internal readonly ClientConnectionLifetime Owner;
            internal readonly NetworkService Service;
            readonly GameObject _object;
            readonly string _previousAccount;
            readonly CharacterClass? _previousIntent;
            readonly string _previousNotice;
            bool _destroyed;

            internal FacadeProbe()
            {
                Assert.IsNull(NetworkService.Instance, "fixture: no existing service owner");
                _previousAccount = AccountLoadout.SessionAccountName;
                _previousIntent = ClassLoadout.SessionSelectedClass;
                MainMenuNotice.TryTake(out _previousNotice);
                _object = new GameObject("verify pr2 fix menu end");
                try
                {
                    // Inactive, so EditMode cannot also deliver automatic lifecycle callbacks.
                    _object.SetActive(false);
                    Service = _object.AddComponent<NetworkService>();
                    FieldInfo field = typeof(NetworkService).GetField("_lifetime", InstanceMembers);
                    if (field.GetValue(Service) == null) SessionTestTools.Call(Service, "Awake");
                    var original = (ClientConnectionLifetime)field.GetValue(Service);
                    var subscription = (Action<IClientConnectionSession>)typeof(ClientConnectionLifetime)
                        .GetField("CharacterMenuLoginAccepted", InstanceMembers).GetValue(original);
                    Assert.IsNotNull(subscription, "fixture: Awake subscribes CharacterMenuLoginAccepted");
                    Action resetMirrors = ClassLoadout.ResetServerClass;
                    Owner = new ClientConnectionLifetime(_ =>
                    {
                        var sockets = new TestSocketPair();
                        Sockets.Add(sockets);
                        return new ConnectedAttempt(sockets.Client);
                    }, Queue.Post, current => new UnityClientSession(current, Queue.Post, (_, __) => { }, (_, __) => { }),
                        resetMirrors);
                    Owner.CharacterMenuLoginAccepted += subscription;
                    original.Dispose();
                    field.SetValue(Service, Owner);
                    AccountLoadout.SessionAccountName = "verify_account";
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            // Handshake and login replies go straight into the receive table; the menu replies later come over the wire.
            internal UnityClientSession ConnectAndAccept()
            {
                Service.ConnectCharacterMenu("127.0.0.2");
                Queue.WaitThenDrain();
                Assert.IsInstanceOf<UnityClientSession>(Owner.CurrentSession, "fixture: the attempt became a session");
                var session = (UnityClientSession)Owner.CurrentSession;
                TestSocketPair wire = Sockets.Last();
                Assert.AreEqual(7, wire.ReadFrame()[2], "fixture: the real Activate sends the handshake first");
                SessionTestTools.Handshake(session, Queue);
                Assert.AreEqual(41, wire.ReadFrame()[2], "fixture: the login follows the handshake");
                session.OnRecvPacket(new ArraySegment<byte>(LoginAccepted));
                Queue.Drain();
                Assert.AreEqual(49, wire.ReadFrame()[2], "fixture: the accepted login requests the first list");
                return session;
            }

            // The receive thread posts the close after the reply; drain until the owner has handled both.
            internal bool DrainUntilTheOwnerEnds(UnityClientSession session) => SpinWait.SpinUntil(() =>
            {
                Queue.Drain();
                return Owner.State == ClientConnectionState.Disconnected && Owner.CurrentSession == null &&
                    session.CharacterMenu == null;
            }, 3000);

            public void Dispose()
            {
                try
                {
                    if (!_destroyed)
                    {
                        _destroyed = true;
                        // This never-active component needs the real product teardown before its object is removed.
                        if (Service != null) SessionTestTools.Call(Service, "OnDestroy");
                    }
                }
                finally
                {
                    if (_object != null) UnityEngine.Object.DestroyImmediate(_object);
                    try
                    {
                        Owner?.Dispose();
                        Queue.Drain();
                    }
                    finally
                    {
                        foreach (TestSocketPair sockets in Sockets) sockets.Dispose();
                        AccountLoadout.SessionAccountName = _previousAccount;
                        ClassLoadout.ResetServerClass();
                        ClassLoadout.SessionSelectedClass = _previousIntent;
                        MainMenuNotice.TryTake(out _);
                        MainMenuNotice.Set(_previousNotice);
                    }
                }
            }
        }

        sealed class ConnectedAttempt : IConnectionAttempt
        {
            Socket _socket;
            internal ConnectedAttempt(Socket socket) => _socket = socket;
            public Task<ConnectionOutcome> Completion => Task.FromResult(new ConnectionOutcome(ConnectionStatus.Success));
            public bool TryTakeConnectedSocket(out Socket socket)
            {
                socket = _socket;
                _socket = null;
                return socket != null;
            }
            public void Cancel() { _socket?.Dispose(); _socket = null; }
            public void Dispose() => Cancel();
        }
    }
}
