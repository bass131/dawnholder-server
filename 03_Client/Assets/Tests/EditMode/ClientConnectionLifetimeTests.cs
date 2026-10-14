using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    internal sealed class ManualConnectionQueue
    {
        readonly ConcurrentQueue<Action> _items = new();
        internal void Post(Action action) => _items.Enqueue(action);
        internal void Drain()
        {
            while (_items.TryDequeue(out Action action)) action();
        }
        internal void WaitThenDrain()
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => !_items.IsEmpty, 3000), "completion did not post");
            Drain();
        }
    }

    internal sealed class ControlledAttempt : IConnectionAttempt
    {
        readonly TaskCompletionSource<ConnectionOutcome> _completion = new();
        Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        internal int Takes;
        internal bool Canceled;
        public Task<ConnectionOutcome> Completion => _completion.Task;
        internal void Complete(ConnectionStatus status) => _completion.TrySetResult(new ConnectionOutcome(status,
            status == ConnectionStatus.Failed ? new SocketException((int)SocketError.ConnectionRefused) : null));
        public bool TryTakeConnectedSocket(out Socket socket)
        {
            socket = null;
            if (Canceled || _socket == null) return false;
            Takes++;
            socket = _socket;
            _socket = null;
            return true;
        }
        public void Cancel() { Canceled = true; _socket?.Dispose(); _socket = null; }
        public void Dispose() => Cancel();
    }

    internal sealed class ControlledClientSession : IClientConnectionSession
    {
        internal Func<bool> Current;
        internal bool CloseDuringActivation;
        internal bool ThrowDuringActivation;
        internal int Activations, Publications, Cleanups, Disconnects;
        internal readonly List<byte> SelectedClasses = new();
        internal readonly List<string> Logins = new();
        internal Action OnDisconnect;
        Socket _socket;
        bool _cleaned;
        public bool IsClosed { get; private set; }
        public event Action Closed;
        public event Action HandshakeSucceeded;
        public event Action<LoginResult> LoginResultReceived;
        public void Activate(Socket socket)
        {
            _socket = socket;
            Activations++;
            if (ThrowDuringActivation) throw new InvalidOperationException("injected activation failure");
            if (CloseDuringActivation) NaturalClose();
        }
        public void Publish() { Publications++; }
        public void SendLogin(string accountName) => Logins.Add(accountName);
        public void SendCharacterSelect(byte selectedClass) => SelectedClasses.Add(selectedClass);
        internal void Handshake() => HandshakeSucceeded?.Invoke();
        internal void AcceptLogin() => LoginResultReceived?.Invoke(LoginResult.Accepted);
        internal Action CaptureLoginResult(LoginResult result)
        {
            var receiver = LoginResultReceived;
            return () => receiver?.Invoke(result);
        }
        internal void NaturalClose() { IsClosed = true; Closed?.Invoke(); }
        public void Disconnect()
        {
            Disconnects++;
            OnDisconnect?.Invoke();
            IsClosed = true;
            _socket?.Dispose();
        }
        public void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            Cleanups++;
            _socket?.Dispose();
        }
    }

    internal sealed class ConnectionHarness : IDisposable
    {
        internal readonly ManualConnectionQueue Queue = new();
        internal readonly Queue<ControlledAttempt> Attempts = new();
        internal readonly List<ControlledClientSession> Sessions = new();
        internal readonly ClientConnectionLifetime Owner;
        internal int BeginCalls, Resets;
        internal IPEndPoint LastEndpoint;
        internal bool CloseDuringActivation, ThrowDuringActivation;
        internal Action ResetObserver;
        internal static readonly IPEndPoint Endpoint = new(IPAddress.Loopback, 12345);
        internal ConnectionHarness()
        {
            Owner = new ClientConnectionLifetime(endpoint => { BeginCalls++; LastEndpoint = endpoint; return Attempts.Dequeue(); },
                Queue.Post, current =>
                {
                    var session = new ControlledClientSession
                    {
                        Current = current, CloseDuringActivation = CloseDuringActivation,
                        ThrowDuringActivation = ThrowDuringActivation,
                    };
                    Sessions.Add(session);
                    return session;
                }, () => { Resets++; ResetObserver?.Invoke(); });
        }
        internal ControlledAttempt Add(ConnectionStatus? completed = ConnectionStatus.Success)
        {
            var attempt = new ControlledAttempt();
            if (completed.HasValue) attempt.Complete(completed.Value);
            Attempts.Enqueue(attempt);
            return attempt;
        }
        internal ControlledClientSession Connect(byte selectedClass = 0)
        {
            Add();
            Assert.IsTrue(Owner.Connect(Endpoint, selectedClass, "test_account"));
            Queue.WaitThenDrain();
            return Sessions[Sessions.Count - 1];
        }
        public void Dispose()
        {
            Owner.Dispose();
            Queue.Drain();
            foreach (var attempt in Attempts) attempt.Dispose();
        }
    }

    public sealed class ClientConnectionLifetimeTests
    {
        [Test]
        public void DuplicateConnect_IsNoOpDuringAttemptAndActiveSession()
        {
            using var h = new ConnectionHarness();
            var attempt = h.Add(null);
            Assert.IsTrue(h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account"));
            Assert.AreEqual(ClientConnectionState.Connecting, h.Owner.State);
            Assert.IsFalse(h.Owner.Connect(ConnectionHarness.Endpoint, 1, "test_account"));
            attempt.Complete(ConnectionStatus.Success);
            h.Queue.WaitThenDrain();
            Assert.IsTrue(h.Owner.IsConnected);
            Assert.IsFalse(h.Owner.Connect(ConnectionHarness.Endpoint, 1, "test_account"));
            Assert.AreEqual(1, h.BeginCalls);
            Assert.AreEqual(1, attempt.Takes);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelOrDisposeBeforePostedSuccess_NeverActivates(bool dispose)
        {
            using var h = new ConnectionHarness();
            var attempt = h.Add();
            h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account");
            if (dispose) h.Owner.Dispose(); else h.Owner.Disconnect();
            h.Queue.WaitThenDrain();
            Assert.IsTrue(attempt.Canceled);
            Assert.AreEqual(0, attempt.Takes);
            Assert.IsEmpty(h.Sessions);
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
        }

        [Test]
        public void LateCompletionOfCanceledAttempt_CannotReplaceNewConnection()
        {
            using var h = new ConnectionHarness();
            var old = h.Add(null);
            h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account");
            h.Owner.Disconnect();
            var current = h.Connect(1);
            old.Complete(ConnectionStatus.Success);
            h.Queue.WaitThenDrain();
            Assert.AreSame(current, h.Owner.CurrentSession);
            Assert.IsTrue(h.Owner.IsConnected);
            Assert.AreEqual(0, old.Takes);
            Assert.AreEqual(1, current.Publications);
        }

        [TestCase(ConnectionStatus.Failed)]
        [TestCase(ConnectionStatus.Canceled)]
        public void UnsuccessfulAttempt_AllowsNextExplicitConnect(ConnectionStatus status)
        {
            using var h = new ConnectionHarness();
            h.Add(status);
            h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account");
            h.Queue.WaitThenDrain();
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
            if (status == ConnectionStatus.Failed) Assert.IsInstanceOf<SocketException>(h.Owner.LastError);
            h.Connect();
            Assert.IsTrue(h.Owner.IsConnected);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActivationTerminalFailure_IsNotPublishedAsConnected(bool throws)
        {
            using var h = new ConnectionHarness { CloseDuringActivation = !throws, ThrowDuringActivation = throws };
            var session = h.Connect();
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
            Assert.IsNull(h.Owner.CurrentSession);
            Assert.AreEqual(0, session.Publications);
            Assert.AreEqual(1, session.Cleanups);
        }

        [Test]
        public void AcceptedLogin_UsesCapturedClass_ExactlyOnce()
        {
            using var h = new ConnectionHarness();
            byte selected = 1;
            var session = h.Connect(selected);
            selected = 0;
            Assert.IsFalse(h.Owner.Connect(ConnectionHarness.Endpoint, selected, "test_account"));
            session.Handshake();
            session.Handshake();
            CollectionAssert.IsEmpty(session.SelectedClasses);
            session.AcceptLogin();
            session.AcceptLogin();
            CollectionAssert.AreEqual(new byte[] { 1 }, session.SelectedClasses);
        }

        [TestCase(LoginResult.InvalidName)]
        [TestCase(LoginResult.AlreadyOnline)]
        public void RejectionQueuedBeforeRemoteClose_ReportsOnceAndDisconnects(LoginResult result)
        {
            using var h = new ConnectionHarness();
            var session = h.Connect();
            session.Handshake();
            var rejected = new List<LoginResult>();
            h.Owner.LoginRejected += rejected.Add;
            Action pending = session.CaptureLoginResult(result);
            h.Queue.Post(pending);
            h.Queue.Post(pending);
            session.NaturalClose();
            Assert.IsEmpty(rejected);

            h.Queue.Drain();

            CollectionAssert.AreEqual(new[] { result }, rejected);
            Assert.AreEqual(1, session.Disconnects);
            Assert.AreEqual(1, session.Cleanups);
            Assert.IsEmpty(session.SelectedClasses);
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
            Assert.IsNull(h.Owner.CurrentSession);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OldQueuedRejection_AfterDisconnectOrReconnect_IsIgnored(bool reconnect)
        {
            using var h = new ConnectionHarness();
            var old = h.Connect();
            old.Handshake();
            Action pending = old.CaptureLoginResult(LoginResult.AlreadyOnline);
            var rejected = new List<LoginResult>();
            h.Owner.LoginRejected += rejected.Add;
            h.Owner.Disconnect();
            ControlledClientSession current = reconnect ? h.Connect(1) : null;
            current?.Handshake();

            h.Queue.Post(pending);
            h.Queue.Drain();

            Assert.IsEmpty(rejected);
            Assert.AreEqual(1, old.Disconnects);
            Assert.IsEmpty(old.SelectedClasses);
            if (current == null)
            {
                Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
                Assert.IsNull(h.Owner.CurrentSession);
            }
            else
            {
                Assert.AreSame(current, h.Owner.CurrentSession);
                Assert.AreEqual(ClientConnectionState.Connected, h.Owner.State);
                Assert.AreEqual(0, current.Disconnects);
                Assert.IsEmpty(current.SelectedClasses);
                current.AcceptLogin();
                CollectionAssert.AreEqual(new byte[] { 1 }, current.SelectedClasses);
            }
        }

        [Test]
        public void AcceptedQueuedBeforeRemoteClose_SelectsNoClass()
        {
            using var h = new ConnectionHarness();
            var session = h.Connect(1);
            session.Handshake();
            h.Queue.Post(session.CaptureLoginResult(LoginResult.Accepted));
            session.NaturalClose();

            h.Queue.Drain();

            Assert.IsEmpty(session.SelectedClasses);
            Assert.AreEqual(1, session.Disconnects);
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
        }

        [TestCase(LoginResult.InvalidName)]
        [TestCase(LoginResult.AlreadyOnline)]
        public void UnityDispatch_RejectionBeforeRemoteClose_ReachesMain(LoginResult result)
        {
            var queue = new ManualConnectionQueue();
            var session = new UnityClientSession(() => true, queue.Post);
            var received = new List<LoginResult>();
            session.LoginResultReceived += received.Add;
            try
            {
                session.OnRecvPacket(new S_LoginResult { result = (byte)result }.Write());
                session.OnDisconnected(ConnectionHarness.Endpoint);
                Assert.IsTrue(session.IsClosed);
                Assert.IsEmpty(received);

                queue.Drain();

                CollectionAssert.AreEqual(new[] { result }, received);
            }
            finally { session.Cleanup(); }
        }

        [Test]
        public void UnityDispatch_AcceptedBeforeRemoteClose_DoesNotApply()
        {
            var queue = new ManualConnectionQueue();
            var session = new UnityClientSession(() => true, queue.Post);
            var received = new List<LoginResult>();
            session.LoginResultReceived += received.Add;
            try
            {
                session.OnRecvPacket(new S_LoginResult { result = (byte)LoginResult.Accepted }.Write());
                session.OnDisconnected(ConnectionHarness.Endpoint);
                queue.Drain();
                Assert.IsEmpty(received);
            }
            finally { session.Cleanup(); }
        }

        [TestCase(LoginResult.Accepted)]
        [TestCase(LoginResult.InvalidName)]
        [TestCase(LoginResult.AlreadyOnline)]
        public void UnityDispatch_ResultAfterPriorCleanup_IsAnObsoleteConnectionResult(LoginResult result)
        {
            var queue = new ManualConnectionQueue();
            var session = new UnityClientSession(() => true, queue.Post);
            var received = new List<LoginResult>();
            session.LoginResultReceived += received.Add;
            try
            {
                // A close already queued by another path owns cleanup; this late result is obsolete.
                session.OnDisconnected(ConnectionHarness.Endpoint);
                session.OnRecvPacket(new S_LoginResult { result = (byte)result }.Write());
                Assert.DoesNotThrow(queue.Drain);
                Assert.IsEmpty(received);
            }
            finally { session.Cleanup(); }
        }

        [Test]
        public void NaturalCloseThenReconnect_OldQueuedCloseCannotResetNewOwner()
        {
            using var h = new ConnectionHarness();
            var old = h.Connect();
            old.NaturalClose();
            Assert.IsFalse(h.Owner.IsConnected);
            h.Owner.Disconnect();
            var current = h.Connect(1); // drains old notification alongside current activation
            int resets = h.Resets;
            old.NaturalClose();
            old.Handshake();
            h.Queue.Drain();
            Assert.AreSame(current, h.Owner.CurrentSession);
            Assert.IsTrue(h.Owner.IsConnected);
            Assert.AreEqual(resets, h.Resets);
            Assert.IsEmpty(old.SelectedClasses);
        }

        [Test]
        public void NaturalClose_AloneResetsOwner_AndAllowsExplicitReconnect()
        {
            using var h = new ConnectionHarness();
            var old = h.Connect();
            old.NaturalClose();
            h.Queue.Drain();
            Assert.AreEqual(ClientConnectionState.Disconnected, h.Owner.State);
            Assert.IsNull(h.Owner.CurrentSession);
            Assert.AreEqual(1, h.Resets);
            h.Connect();
            Assert.IsTrue(h.Owner.IsConnected);
        }

        [Test]
        public void Facade_UsesExplicitHostAndClassOverride_CapturedBeforeHandshake()
        {
            using var h = new ConnectionHarness();
            var go = new GameObject("connection override test");
            var previous = Dawnholder.Client.Bootstrap.ClassLoadout.SessionSelectedClass;
            var previousAccount = Dawnholder.Client.Bootstrap.AccountLoadout.SessionAccountName;
            try
            {
                var facade = go.AddComponent<NetworkService>();
                typeof(NetworkService).GetField("_lifetime", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(facade, h.Owner);
                Dawnholder.Client.Bootstrap.ClassLoadout.SessionSelectedClass = Shared.Protocol.CharacterClass.Knight;
                Dawnholder.Client.Bootstrap.AccountLoadout.SessionAccountName = "test_account";
                h.Add();
                facade.Connect("127.0.0.2", 1);
                h.Queue.WaitThenDrain();
                Assert.AreEqual(IPAddress.Parse("127.0.0.2"), h.LastEndpoint.Address);
                h.Sessions[0].Handshake();
                h.Sessions[0].AcceptLogin();
                CollectionAssert.AreEqual(new byte[] { 1 }, h.Sessions[0].SelectedClasses);
                Assert.IsTrue(facade.IsConnected);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                Dawnholder.Client.Bootstrap.ClassLoadout.SessionSelectedClass = previous;
                Dawnholder.Client.Bootstrap.AccountLoadout.SessionAccountName = previousAccount;
            }
        }

        [Test]
        public void DisconnectInvalidatesBeforeExternalCleanup_AndIsIdempotent()
        {
            using var h = new ConnectionHarness();
            var session = h.Connect();
            long activeGeneration = h.Owner.Generation;
            bool observedInvalid = false;
            session.OnDisconnect = () => observedInvalid = !session.Current() &&
                h.Owner.CurrentSession == null && h.Owner.Generation > activeGeneration;
            h.Owner.Disconnect();
            h.Owner.Disconnect();
            Assert.IsTrue(observedInvalid);
            Assert.AreEqual(1, session.Disconnects);
            Assert.AreEqual(1, h.Resets);
        }

        [TestCase("OnDestroy")]
        [TestCase("OnApplicationQuit")]
        public void FacadeLifecycle_DisposesAttemptBeforeLateCompletion(string callback)
        {
            using var h = new ConnectionHarness();
            var go = new GameObject("connection facade test");
            try
            {
                var facade = go.AddComponent<NetworkService>();
                typeof(NetworkService).GetField("_lifetime", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).SetValue(facade, h.Owner);
                var attempt = h.Add();
                h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account");
                typeof(NetworkService).GetMethod(callback, System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(facade, null);
                h.Queue.WaitThenDrain();
                Assert.AreEqual(0, attempt.Takes);
                Assert.IsEmpty(h.Sessions);
                Assert.IsFalse(h.Owner.Connect(ConnectionHarness.Endpoint, 0, "test_account"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
