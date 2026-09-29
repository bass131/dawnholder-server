using System.Collections.Concurrent;
using System.Net;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Sessions;
using Dawnholder.Server.Network;

namespace Dawnholder.Server.GameServer.Hosting;

/// <summary>월드 준비, 접속 수신, 연결 해제, 틱 종료 순서를 소유한다.</summary>
public sealed class ServerHost : IDisposable
{
    readonly object _gate = new();
    readonly GameWorld _world;
    readonly Listener _listener = new();
    readonly IPEndPoint _endPoint;
    readonly ConcurrentDictionary<GameSession, byte> _sessions = new();
    bool _started;
    bool _worldStarted;
    bool _stopped;
    int _stopping;

    public ServerHost(GameWorld world, IPEndPoint endPoint)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
    }

    public EndPoint? LocalEndPoint => _listener.LocalEndPoint;

    public void Start()
    {
        lock (_gate)
        {
            if (_started || _stopped || _stopping != 0)
                throw new InvalidOperationException("ServerHost는 한 번만 시작할 수 있습니다.");
            _started = true;
            try
            {
                _world.Start();
                _worldStarted = true;
                _listener.Init(_endPoint, CreateSession);
            }
            catch (Exception startError)
            {
                try { StopCore(TimeSpan.FromSeconds(2)); }
                catch (Exception stopError) { throw new AggregateException(startError, stopError); }
                throw;
            }
        }
    }

    public void Stop(TimeSpan? timeout = null)
    {
        if (_world.Scheduler.IsTickThread)
            throw new InvalidOperationException("ServerHost.Stop은 틱 밖에서 호출해야 합니다.");
        lock (_gate) StopCore(timeout ?? TimeSpan.FromSeconds(2));
    }

    public void Dispose() => Stop();

    GameSession CreateSession()
    {
        if (Volatile.Read(ref _stopping) != 0)
            throw new InvalidOperationException("서버가 종료 중입니다.");
        GameSession session = new(_world);
        session.ConnectionClosed += RemoveSession;
        _sessions.TryAdd(session, 0);
        return session;
    }

    void RemoveSession(GameSession session)
    {
        session.ConnectionClosed -= RemoveSession;
        _sessions.TryRemove(session, out _);
    }

    void StopCore(TimeSpan timeout)
    {
        if (_stopped) return;
        Volatile.Write(ref _stopping, 1);
        Exception? acceptFailure = null;
        try { _listener.Stop(timeout); }
        catch (AggregateException ex) when (_listener.Completion.IsCompleted)
        {
            acceptFailure = ex;
        }

        GameSession[] sessions = _sessions.Keys.ToArray();
        List<Exception> errors = new();
        foreach (GameSession session in sessions)
        {
            try { session.Disconnect(); }
            catch (Exception ex) { errors.Add(ex); }
        }
        if (errors.Count != 0)
        {
            if (acceptFailure != null) errors.Add(acceptFailure);
            throw new AggregateException("연결 해제에 실패했습니다.", errors);
        }
        if (!Task.WhenAll(sessions.Select(session => session.DisconnectCompleted)).Wait(timeout))
            throw new TimeoutException("연결 해제가 아직 완료되지 않았습니다.");

        if (_worldStarted && !_world.FlushSessionClosuresAsync().Wait(timeout))
            throw new TimeoutException("월드의 세션 해제가 아직 완료되지 않았습니다.");
        _world.Stop(timeout);
        _worldStarted = false;
        _stopped = true;
        if (acceptFailure != null)
            throw new AggregateException("서버 정리는 완료했지만 accept 루프 오류가 있었습니다.", acceptFailure);
    }

}
