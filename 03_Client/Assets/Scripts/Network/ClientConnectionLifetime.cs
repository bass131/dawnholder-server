using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dawnholder.Client.Net;

namespace Dawnholder.Client.Network
{
    public enum ClientConnectionState { Disconnected, Connecting, Connected }

    /// <summary>Only Closed may be raised by a transport thread. Other operations run on main.</summary>
    public interface IClientConnectionSession
    {
        bool IsClosed { get; }
        event Action Closed;
        event Action HandshakeSucceeded;
        void Activate(Socket socket);
        void Publish();
        void SendCharacterSelect(byte characterClass);
        void Disconnect();
        void Cleanup();
    }

    /// <summary>Main-thread owner. Transport completions only post back to this owner.</summary>
    public sealed class ClientConnectionLifetime : IDisposable
    {
        readonly Func<IPEndPoint, IConnectionAttempt> _beginConnect;
        readonly Action<Action> _post;
        readonly Func<Func<bool>, IClientConnectionSession> _createSession;
        readonly Action _resetMirrors;
        IConnectionAttempt _attempt;
        IClientConnectionSession _session;
        Action _closedHandler;
        Action _handshakeHandler;
        byte _selectedClass;
        bool _characterSelectSent;
        bool _disposed;

        public ClientConnectionState State { get; private set; }
        public long Generation { get; private set; }
        public Exception LastError { get; private set; }
        public IClientConnectionSession CurrentSession => _session;
        public bool IsConnected => State == ClientConnectionState.Connected && _session != null && !_session.IsClosed;

        public ClientConnectionLifetime(Func<IPEndPoint, IConnectionAttempt> beginConnect,
            Action<Action> post, Func<Func<bool>, IClientConnectionSession> createSession, Action resetMirrors)
        {
            _beginConnect = beginConnect ?? throw new ArgumentNullException(nameof(beginConnect));
            _post = post ?? throw new ArgumentNullException(nameof(post));
            _createSession = createSession ?? throw new ArgumentNullException(nameof(createSession));
            _resetMirrors = resetMirrors ?? throw new ArgumentNullException(nameof(resetMirrors));
        }

        public bool Connect(IPEndPoint endPoint, byte selectedClass)
        {
            if (_disposed || State != ClientConnectionState.Disconnected) return false;
            Generation++;
            long generation = Generation;
            _selectedClass = selectedClass;
            _characterSelectSent = false;
            LastError = null;
            State = ClientConnectionState.Connecting;
            try
            {
                _attempt = _beginConnect(endPoint);
                _ = ObserveCompletion(_attempt, generation);
                return true;
            }
            catch (Exception error)
            {
                LastError = error;
                Disconnect();
                return false;
            }
        }

        async Task ObserveCompletion(IConnectionAttempt attempt, long generation)
        {
            ConnectionOutcome outcome;
            try { outcome = await attempt.Completion.ConfigureAwait(false); }
            catch (Exception error) { outcome = new ConnectionOutcome(ConnectionStatus.Failed, error); }
            _post(() => CompleteConnection(attempt, generation, outcome));
        }

        void CompleteConnection(IConnectionAttempt attempt, long generation, ConnectionOutcome outcome)
        {
            if (_disposed || generation != Generation || !ReferenceEquals(_attempt, attempt))
            {
                attempt.Dispose();
                return;
            }
            if (outcome.Status != ConnectionStatus.Success)
            {
                LastError = outcome.Error;
                Disconnect();
                return;
            }

            Socket socket = null;
            try
            {
                if (!attempt.TryTakeConnectedSocket(out socket))
                {
                    Disconnect();
                    return;
                }
                _attempt = null;
                attempt.Dispose();
                IClientConnectionSession session = null;
                session = _createSession(() => IsCurrent(generation, session));
                _session = session;
                _closedHandler = () => _post(() => HandleClosed(generation, session));
                _handshakeHandler = () => HandleHandshake(generation, session);
                session.Closed += _closedHandler;
                session.HandshakeSucceeded += _handshakeHandler;
                session.Activate(socket);
                socket = null;
                if (!IsCurrent(generation, session))
                {
                    Disconnect();
                    return;
                }
                State = ClientConnectionState.Connected;
                session.Publish();
            }
            catch (Exception error)
            {
                LastError = error;
                Disconnect();
            }
            finally { socket?.Dispose(); }
        }

        bool IsCurrent(long generation, IClientConnectionSession session) =>
            !_disposed && generation == Generation && session != null &&
            ReferenceEquals(_session, session) && !session.IsClosed;

        void HandleHandshake(long generation, IClientConnectionSession session)
        {
            if (!IsCurrent(generation, session) || _characterSelectSent) return;
            _characterSelectSent = true;
            try { session.SendCharacterSelect(_selectedClass); }
            catch (Exception error)
            {
                LastError = error;
                Disconnect();
            }
        }

        void HandleClosed(long generation, IClientConnectionSession session)
        {
            if (generation == Generation && ReferenceEquals(_session, session)) Disconnect();
            else session.Cleanup(); // Only its own buffer/subscriptions; never global mirrors.
        }

        public void Disconnect()
        {
            if (State == ClientConnectionState.Disconnected && _attempt == null && _session == null) return;
            Generation++; // Invalidate queued work before invoking any external cleanup.
            var attempt = _attempt;
            var session = _session;
            var closed = _closedHandler;
            var handshake = _handshakeHandler;
            _attempt = null;
            _session = null;
            _closedHandler = null;
            _handshakeHandler = null;
            _characterSelectSent = false;
            State = ClientConnectionState.Disconnected;
            if (session != null)
            {
                session.Closed -= closed;
                session.HandshakeSucceeded -= handshake;
            }
            if (attempt != null)
            {
                CleanupSafely(attempt.Cancel);
                CleanupSafely(attempt.Dispose);
            }
            if (session != null)
            {
                CleanupSafely(session.Disconnect);
                CleanupSafely(session.Cleanup);
            }
            CleanupSafely(_resetMirrors);
        }

        void CleanupSafely(Action cleanup)
        {
            try { cleanup(); }
            catch (Exception error) { LastError = error; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
        }
    }
}
