using System.Threading.Channels;
using Dawnholder.Server.GameServer.Persistence;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Test-only runtime repository. Every call is recorded with its request, the deadline budget seen at
/// call time and the cancellation token, then stays pending until the test completes it. Calls are
/// handed to the test in arrival order, so taking a call of the wrong kind fails the test. The double
/// never chooses outcomes or computes authority, sequence or tokens; each result is scripted by a test.
/// </summary>
public sealed class RecordingCharacterRepository : ICharacterRepository
{
    public const string Admission = nameof(ICharacterRepository.ReadAdmissionAsync);
    public const string Acquire = nameof(ICharacterRepository.AcquireAndLoadAsync);
    public const string Checkpoint = nameof(ICharacterRepository.WriteSafeCheckpointAsync);
    public const string Release = nameof(ICharacterRepository.ReleaseAsync);
    public const string Resolve = nameof(ICharacterRepository.ResolveOperationAsync);

    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    readonly object _gate = new();
    readonly List<string> _calls = new();
    readonly List<(int Count, TaskCompletionSource Signal)> _countWaiters = new();
    readonly Channel<object> _arrivals = Channel.CreateUnbounded<object>();
    int _inFlight;
    int _maxInFlight;
    int _taken;

    /// <summary>Method names in arrival order.</summary>
    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (_gate)
                return _calls.ToArray();
        }
    }

    /// <summary>Largest number of repository calls that were pending at the same time.</summary>
    public int MaxInFlight
    {
        get
        {
            lock (_gate)
                return _maxInFlight;
        }
    }

    public int Count(string method) => Calls.Count(call => call == method);

    public Task<ReadAdmissionResult> ReadAdmissionAsync(ReadAdmissionRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Record<ReadAdmissionRequest, ReadAdmissionResult>(Admission, request, deadline, cancellationToken);

    public Task<AcquireAndLoadResult> AcquireAndLoadAsync(AcquireAndLoadRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Record<AcquireAndLoadRequest, AcquireAndLoadResult>(Acquire, request, deadline, cancellationToken);

    public Task<WriteSafeCheckpointResult> WriteSafeCheckpointAsync(WriteSafeCheckpointRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Record<WriteSafeCheckpointRequest, WriteSafeCheckpointResult>(Checkpoint, request, deadline, cancellationToken);

    public Task<ReleaseResult> ReleaseAsync(ReleaseRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Record<ReleaseRequest, ReleaseResult>(Release, request, deadline, cancellationToken);

    public Task<ResolveOperationResult> ResolveOperationAsync(ResolveOperationRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Record<ResolveOperationRequest, ResolveOperationResult>(Resolve, request, deadline, cancellationToken);

    public Task<PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult>> NextAcquireAsync() => Next<AcquireAndLoadRequest, AcquireAndLoadResult>();

    public Task<PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult>> NextCheckpointAsync() => Next<WriteSafeCheckpointRequest, WriteSafeCheckpointResult>();

    public Task<PendingCall<ReleaseRequest, ReleaseResult>> NextReleaseAsync() => Next<ReleaseRequest, ReleaseResult>();

    public Task<PendingCall<ResolveOperationRequest, ResolveOperationResult>> NextResolveAsync() => Next<ResolveOperationRequest, ResolveOperationResult>();

    /// <summary>Takes the next call of any kind, for flows where the contract leaves the order open.</summary>
    public async Task<object> NextAnyAsync()
    {
        object call = await _arrivals.Reader.ReadAsync().AsTask().WaitAsync(Limit);
        lock (_gate)
            _taken++;
        return call;
    }

    /// <summary>Completes once a call has arrived that the test has not taken yet. Does not take it.</summary>
    public Task WhenUntakenCallAsync()
    {
        lock (_gate)
        {
            int count = _taken + 1;
            if (_calls.Count >= count)
                return Task.CompletedTask;
            TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _countWaiters.Add((count, signal));
            return signal.Task;
        }
    }

    Task<TResult> Record<TRequest, TResult>(string method, TRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        // Read the budget first so the record shows what the owner handed over at call time.
        TimeSpan remaining = deadline.GetRemaining();
        PendingCall<TRequest, TResult> call = new(this, request, deadline, remaining, cancellationToken);
        List<TaskCompletionSource> reached = new();
        lock (_gate)
        {
            _calls.Add(method);
            _inFlight++;
            _maxInFlight = Math.Max(_maxInFlight, _inFlight);
            for (int i = _countWaiters.Count - 1; i >= 0; i--)
            {
                if (_countWaiters[i].Count <= _calls.Count)
                {
                    reached.Add(_countWaiters[i].Signal);
                    _countWaiters.RemoveAt(i);
                }
            }
        }
        if (!_arrivals.Writer.TryWrite(call))
            throw new InvalidOperationException("Unbounded channel rejected a call.");
        foreach (TaskCompletionSource signal in reached)
            signal.TrySetResult();
        return call.ResultTask;
    }

    async Task<PendingCall<TRequest, TResult>> Next<TRequest, TResult>()
    {
        object call = await NextAnyAsync();
        return Assert.IsType<PendingCall<TRequest, TResult>>(call);
    }

    void Settled()
    {
        lock (_gate)
            _inFlight--;
    }

    public sealed class PendingCall<TRequest, TResult>
    {
        readonly RecordingCharacterRepository _owner;
        readonly TaskCompletionSource<TResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _settled;

        internal PendingCall(
            RecordingCharacterRepository owner,
            TRequest request,
            PersistenceDeadline deadline,
            TimeSpan remainingAtCall,
            CancellationToken cancellationToken)
        {
            _owner = owner;
            Request = request;
            Deadline = deadline;
            RemainingAtCall = remainingAtCall;
            CancellationToken = cancellationToken;
        }

        public TRequest Request { get; }

        public PersistenceDeadline Deadline { get; }

        public TimeSpan RemainingAtCall { get; }

        public CancellationToken CancellationToken { get; }

        public Task<TResult> ResultTask => _completion.Task;

        public void Complete(TResult result)
        {
            MarkSettled();
            _completion.SetResult(result);
        }

        public void Fail(Exception exception)
        {
            MarkSettled();
            _completion.SetException(exception);
        }

        /// <summary>Ends the call as a cancelled task, as a driver might after a cancellation request.</summary>
        public void Cancel()
        {
            MarkSettled();
            _completion.SetCanceled();
        }

        void MarkSettled()
        {
            if (Interlocked.Exchange(ref _settled, 1) != 0)
                throw new InvalidOperationException("A recorded call can be settled once.");
            _owner.Settled();
        }
    }
}
