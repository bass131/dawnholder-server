using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Client.Net;

public enum ConnectionStatus { Success, Failed, Canceled }

/// <summary>The attempt owns even a connected socket until it is taken once.</summary>
public interface IConnectionAttempt : IDisposable
{
    Task<ConnectionOutcome> Completion { get; }
    bool TryTakeConnectedSocket(out Socket? socket);
    void Cancel();
}

public sealed class ConnectionOutcome
{
    public ConnectionOutcome(ConnectionStatus status, Exception? error = null)
    {
        Status = status;
        Error = error;
    }

    public ConnectionStatus Status { get; }
    public Exception? Error { get; }
}

internal sealed class ConnectionAttempt : IConnectionAttempt
{
    readonly object _gate = new();
    readonly TaskCompletionSource<ConnectionOutcome> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    Socket? _socket;
    SocketAsyncEventArgs? _args;
    bool _pending;
    bool _finished;
    bool _connected;

    internal ConnectionAttempt(IPEndPoint endPoint)
    {
        try
        {
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _args = new SocketAsyncEventArgs { RemoteEndPoint = endPoint };
            _args.Completed += OnCompleted;
            _pending = true;
            if (!_socket.ConnectAsync(_args))
                Finish(_args.SocketError);
        }
        catch (Exception error)
        {
            Finish(SocketError.SocketError, error);
        }
    }

    public Task<ConnectionOutcome> Completion => _completion.Task;

    public bool TryTakeConnectedSocket(out Socket? socket)
    {
        lock (_gate)
        {
            socket = _connected ? _socket : null;
            if (socket == null) return false;
            _socket = null;
            return true;
        }
    }

    public void Cancel()
    {
        Socket? close;
        SocketAsyncEventArgs? args = null;
        bool complete;
        lock (_gate)
        {
            close = _socket;
            _socket = null;
            _connected = false;
            complete = !_finished;
            _finished = true;
            // A pending operation still owns its event args until OnCompleted.
            if (!_pending)
            {
                args = _args;
                _args = null;
            }
        }
        close?.Dispose();
        args?.Dispose();
        if (complete) _completion.TrySetResult(new ConnectionOutcome(ConnectionStatus.Canceled));
    }

    public void Dispose() => Cancel();

    void OnCompleted(object? sender, SocketAsyncEventArgs args) => Finish(args.SocketError);

    void Finish(SocketError error, Exception? exception = null)
    {
        Socket? close = null;
        SocketAsyncEventArgs? args;
        ConnectionOutcome? outcome = null;
        lock (_gate)
        {
            _pending = false;
            args = _args;
            _args = null;
            if (!_finished)
            {
                _finished = true;
                _connected = error == SocketError.Success && exception == null;
                outcome = new ConnectionOutcome(
                    _connected ? ConnectionStatus.Success : ConnectionStatus.Failed,
                    exception ?? (_connected ? null : new SocketException((int)error)));
                if (!_connected)
                {
                    close = _socket;
                    _socket = null;
                }
            }
        }
        if (args != null)
        {
            args.Completed -= OnCompleted;
            args.Dispose();
        }
        close?.Dispose();
        if (outcome != null) _completion.TrySetResult(outcome);
    }
}
