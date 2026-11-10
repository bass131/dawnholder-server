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
    // Independent verification of PR2 in goal 2026-11-10-code-audit-client-fixes, items 7 and 6.
    // Requirement source: the goal's item 7 row (a valid result received before the close reaches the menu once,
    // before cleanup, and nothing is sent again) with fix-scope item 7's symptom (the received result is lost and only
    // a plain connection end shows), and the item 6 row (a failed first attempt leaves Connecting for the ended state).
    // Real parts: NetworkService's Awake subscription, getters and Disconnect, ClientConnectionLifetime,
    // UnityClientSession with its receive loop on a loopback socket pair, and the CharacterMenuPresenter that
    // CharacterSelectController.Update feeds with (CharacterMenu, IsConnected, IsConnecting). Controlled parts: the
    // connector (it hands over the socket pair and never dials an address) and the main-thread queue, which drains
    // where MainThreadDispatcher.Update would. The peer answers with CharacterMenuWire's independent contract bytes
    // and ends with a real EOF. Expected screen text is never copied from product code: an overlap run is compared
    // with the same run drained first.
    public sealed class VerifyPr2ConnectionFlowTests
    {
        static readonly byte[] LoginAccepted = { 5, 0, 42, 0, 0 };
        static readonly BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const int CreateResultId = 52;

        [Test]
        public void OutcomeUnknownThenEof_DrainedBeforeTheMenuLooks_ShowsTheResultOnceAndSendsNothing()
        {
            ScreenRun run = RunCreateOutcomeUnknownThenEof(menuLooksBeforeTheDrain: false);

            Assert.AreEqual(Phase.Disconnected, run.Phase, "item 7: the OutcomeUnknown result ends the menu");
            Assert.IsNotEmpty(run.Message, "item 7: the received result's text is on the screen, not a plain end");
            Assert.AreEqual(1, run.Notifications, "item 7: the result reaches the menu client exactly once");
            Assert.AreEqual(0, run.BytesAfterTheReply, "item 7: nothing is sent again after the result");
            Assert.IsTrue(run.OwnerEnded, "item 7: the same drain still ends the connection owner");
        }

        [Test]
        public void OutcomeUnknownThenEof_MenuSeesTheCloseBeforeTheDrain_StillShowsTheSameResult()
        {
            ScreenRun reference = RunCreateOutcomeUnknownThenEof(menuLooksBeforeTheDrain: false);
            ScreenRun overlap = RunCreateOutcomeUnknownThenEof(menuLooksBeforeTheDrain: true);

            Assert.IsNotEmpty(reference.Message, "fixture: the reference run shows the result text");
            Assert.AreEqual(1, overlap.Notifications, "item 7: the session still delivers the result once");
            Assert.AreEqual(0, overlap.BytesAfterTheReply, "item 7: nothing is sent again after the result");
            Assert.AreEqual(reference.Phase, overlap.Phase, "item 7: the overlap ends in the same phase");
            Assert.AreEqual(reference.Message, overlap.Message,
                "item 7: when the menu sees the closed transport before the queued result, the received result's " +
                "text must still be shown instead of a plain connection end");
        }

        [Test]
        public void ReplyStillQueued_WhenTheFacadeDisconnects_IsNotDelivered()
        {
            // Preserved behaviour: a local Disconnect cleans the session before its queued reply runs.
            using var probe = new FacadeProbe();
            UnityClientSession session = probe.ConnectAndAccept();
            CharacterMenuClient menu = probe.Service.CharacterMenu;
            probe.Sockets.Last().Peer.Send(CharacterMenuWire.List(menu.PendingRequestId, 0));
            probe.Queue.WaitThenDrain();
            Assert.IsTrue(menu.TryCreate(1, 0, "가나"), "fixture: the create request is sent");
            Assert.AreEqual(51, probe.Sockets.Last().ReadFrame()[2], "fixture: the create request is on the wire");
            uint requestId = menu.PendingRequestId;
            CharacterMenuReply listReply = menu.LastReply;
            int notifications = 0;
            menu.Changed += () => notifications++;

            session.OnRecvPacket(new ArraySegment<byte>(CharacterMenuWire.Failure(CreateResultId, requestId,
                (byte)CharacterMenuResult.OutcomeUnknown)));
            probe.Service.Disconnect();
            probe.Queue.Drain();

            Assert.AreSame(listReply, menu.LastReply, "preserved: a reply queued behind a local disconnect is not delivered");
            Assert.AreEqual(0, notifications, "preserved: nothing is announced after the local disconnect");
            Assert.IsNull(probe.Owner.CurrentSession, "fixture: the owner released the session");
        }

        [Test]
        public void FirstAttemptRefused_TheMenuLeavesConnectingForTheEndedState()
        {
            // Item 6 through the facade getters CharacterSelectController.Update reads. The controller connects in
            // Start, so its first Update already sees the attempt under way.
            using var probe = new FacadeProbe { Refuse = true };
            var states = new List<CharacterMenuViewState>();
            var legacyStates = new List<CharacterMenuViewState>();
            using var presenter = new CharacterMenuPresenter(states.Add, () => { }, _ => { });
            using var legacyCaller = new CharacterMenuPresenter(legacyStates.Add, () => { }, _ => { });
            probe.Service.ConnectCharacterMenu("127.0.0.2");
            Assert.IsTrue(probe.Service.IsConnecting, "fixture: the attempt is under way");
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            legacyCaller.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected);
            Assert.IsEmpty(states, "fixture: an attempt under way leaves the menu connecting");

            probe.Queue.WaitThenDrain();
            Assert.IsFalse(probe.Service.IsConnecting || probe.Service.IsConnected, "fixture: the refusal ended the attempt");
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            legacyCaller.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected);

            Assert.AreEqual(1, states.Count, "item 6: the refusal renders once");
            Assert.AreEqual(Phase.Disconnected, states.Last().CurrentPhase, "item 6: the menu ends instead of waiting");
            // Positive control: a caller without the attempt state keeps the pre-fix meaning and never renders.
            Assert.IsEmpty(legacyStates, "control: the two-argument Observe still treats the attempt as under way");
        }

        // One create that the server answers with OutcomeUnknown and then closes. menuLooksBeforeTheDrain puts one
        // CharacterSelectController.Update-style Observe between the receive thread's close and the queue drain: the
        // receive thread runs outside Unity's frame, so the close can land after the dispatcher's drain of a frame and
        // before the controller's Update, and the two components have no execution order between them.
        static ScreenRun RunCreateOutcomeUnknownThenEof(bool menuLooksBeforeTheDrain)
        {
            using var probe = new FacadeProbe();
            var states = new List<CharacterMenuViewState>();
            using var presenter = new CharacterMenuPresenter(states.Add, () => { }, _ => { });
            UnityClientSession session = probe.ConnectAndAccept();
            TestSocketPair wire = probe.Sockets.Last();
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            CharacterMenuClient menu = probe.Service.CharacterMenu;
            Assert.IsNotNull(menu, "fixture: the accepted login opened the menu client");
            wire.Peer.Send(CharacterMenuWire.List(menu.PendingRequestId, 0));
            probe.Queue.WaitThenDrain();
            Assert.AreEqual(Phase.Ready, states.Last().CurrentPhase, "fixture: the empty list makes the menu ready");

            presenter.Create(1, 0, "가나");
            Assert.AreEqual(51, wire.ReadFrame()[2], "fixture: the create request is on the wire");
            uint requestId = menu.PendingRequestId;
            int notifications = 0;
            menu.Changed += () => notifications++;

            wire.Peer.Send(CharacterMenuWire.Failure(CreateResultId, requestId, (byte)CharacterMenuResult.OutcomeUnknown));
            wire.Peer.Shutdown(SocketShutdown.Send);
            Assert.IsTrue(SpinWait.SpinUntil(() => session.IsClosed, 3000), "fixture: the receive thread saw the EOF");
            if (menuLooksBeforeTheDrain)
            {
                Assert.IsFalse(probe.Service.IsConnected, "fixture: the facade already reports the closed transport");
                Assert.AreSame(menu, probe.Service.CharacterMenu, "fixture: cleanup has not run yet");
                presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);
            }
            bool ownerEnded = probe.DrainUntilTheOwnerEnds(session);
            presenter.Observe(probe.Service.CharacterMenu, probe.Service.IsConnected, probe.Service.IsConnecting);

            return new ScreenRun(states.Last().CurrentPhase, states.Last().Message, notifications,
                BytesBeforeEof(wire), ownerEnded);
        }

        // Everything the client writes after the create request. The client closes its socket, so this ends at EOF;
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

        readonly struct ScreenRun
        {
            internal ScreenRun(Phase phase, string message, int notifications, int bytesAfterTheReply, bool ownerEnded)
            {
                Phase = phase;
                Message = message;
                Notifications = notifications;
                BytesAfterTheReply = bytesAfterTheReply;
                OwnerEnded = ownerEnded;
            }
            internal Phase Phase { get; }
            internal string Message { get; }
            internal int Notifications { get; }
            internal int BytesAfterTheReply { get; }
            internal bool OwnerEnded { get; }
        }

        // The facade keeps Awake's real CharacterMenuLoginAccepted delegate; only the lifetime behind it is replaced
        // with one on the controlled connector and queue (the same shape as CharacterMenuClientTests' facade probe).
        sealed class FacadeProbe : IDisposable
        {
            internal readonly ManualConnectionQueue Queue = new();
            internal readonly List<TestSocketPair> Sockets = new();
            internal readonly ClientConnectionLifetime Owner;
            internal readonly NetworkService Service;
            // The next attempt fails with a refused connection instead of handing over a socket pair.
            internal bool Refuse;
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
                _object = new GameObject("verify pr2 connection flow");
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
                        if (Refuse) return new RefusedAttempt();
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

        sealed class RefusedAttempt : IConnectionAttempt
        {
            public Task<ConnectionOutcome> Completion => Task.FromResult(new ConnectionOutcome(ConnectionStatus.Failed,
                new SocketException((int)SocketError.ConnectionRefused)));
            public bool TryTakeConnectedSocket(out Socket socket)
            {
                socket = null;
                return false;
            }
            public void Cancel() { }
            public void Dispose() { }
        }
    }
}
