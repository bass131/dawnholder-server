using System.Reflection;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Test-only stand-in for whatever storage interfaces the catalog constructor asks for. API v1 does not name them, so
/// one runtime proxy answers every interface. A storage call is recorded in arrival order and then stays pending; the
/// recorder never returns a stored value, a receipt or any success, so no test can pass on the recorder's behalf.
/// <see cref="Mode.BlockCaller"/> additionally blocks the calling thread inside the call, standing in for a synchronous
/// connection-open prefix that must never run on the game tick (API v1 「책임과 호출 경계」). Dispose faults every pending
/// call and releases blocked callers so no product continuation waits past the test.
/// </summary>
public sealed class StoreCallRecorder : IDisposable
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    public enum Mode
    {
        Pending,
        BlockCaller,
    }

    readonly object _gate = new();
    readonly Mode _mode;
    readonly List<string> _calls = new();
    readonly List<Action<Exception>> _pending = new();
    readonly ManualResetEventSlim _unblock = new(false);
    readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _disposed;

    public StoreCallRecorder(Mode mode = Mode.Pending) => _mode = mode;

    /// <summary>Storage method names in arrival order. Disposal of the storage objects is not a storage call.</summary>
    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (_gate)
                return _calls.ToArray();
        }
    }

    public Task FirstCall => _firstCall.Task;

    public object CreateProxy(Type storageInterface)
    {
        object proxy = DispatchProxy.Create(storageInterface, typeof(RecordingProxy));
        ((RecordingProxy)proxy).Recorder = this;
        return proxy;
    }

    /// <summary>Lets callers blocked by <see cref="Mode.BlockCaller"/> continue; their calls then stay pending.</summary>
    public void Unblock() => _unblock.Set();

    public void Dispose()
    {
        Action<Exception>[] pending;
        lock (_gate)
        {
            _disposed = true;
            pending = _pending.ToArray();
            _pending.Clear();
        }
        _unblock.Set();
        foreach (Action<Exception> fault in pending)
            fault(new StoreRefusedException());
    }

    internal object? Handle(MethodInfo method)
    {
        if (method.Name is nameof(IDisposable.Dispose) or nameof(IAsyncDisposable.DisposeAsync))
            return method.ReturnType == typeof(ValueTask) ? ValueTask.CompletedTask : null;
        lock (_gate)
            _calls.Add(method.Name);
        _firstCall.TrySetResult();
        if (_mode == Mode.BlockCaller)
            _unblock.Wait(Limit);
        return PendingReturn(method.ReturnType);
    }

    object? PendingReturn(Type returnType)
    {
        if (returnType == typeof(void))
            return null;
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            TaskCompletionSource source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Track(error => source.TrySetException(error));
            return returnType == typeof(Task) ? source.Task : new ValueTask(source.Task);
        }
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() is { } shape &&
            (shape == typeof(Task<>) || shape == typeof(ValueTask<>)))
        {
            Type result = returnType.GetGenericArguments()[0];
            object source = Activator.CreateInstance(
                typeof(TaskCompletionSource<>).MakeGenericType(result), TaskCreationOptions.RunContinuationsAsynchronously)!;
            MethodInfo fault = source.GetType().GetMethod("TrySetException", new[] { typeof(Exception) })!;
            Track(error => fault.Invoke(source, new object[] { error }));
            object task = source.GetType().GetProperty("Task")!.GetValue(source)!;
            return shape == typeof(Task<>) ? task : Activator.CreateInstance(returnType, task);
        }
        // A synchronous storage answer would have to invent a value; the recorder refuses instead.
        throw new StoreRefusedException();
    }

    void Track(Action<Exception> fault)
    {
        bool disposed;
        lock (_gate)
        {
            disposed = _disposed;
            if (!disposed)
                _pending.Add(fault);
        }
        if (disposed)
            fault(new StoreRefusedException());
    }
}

/// <summary>The recorder's only answer: no storage result exists in an offline test.</summary>
public sealed class StoreRefusedException : Exception
{
    public StoreRefusedException() : base("Offline test storage never answers.")
    {
    }
}

/// <summary>DispatchProxy base for <see cref="StoreCallRecorder"/>; it must be public and unsealed.</summary>
public class RecordingProxy : DispatchProxy
{
    internal StoreCallRecorder? Recorder { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => Recorder!.Handle(targetMethod!);
}
