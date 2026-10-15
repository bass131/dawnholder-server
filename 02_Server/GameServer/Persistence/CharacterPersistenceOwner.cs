using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>
/// Owns one character's runtime authority, bounded waiting queue and independent final hand-off.
/// The gate protects local state only; the single async drain invokes the repository outside it.
/// A close deadline ends the caller's wait, never ownership of an unfinished repository task.
/// </summary>
public sealed class CharacterPersistenceOwner
{
    readonly object _gate = new();
    readonly PersistenceIdentity _identity;
    readonly Guid _runtimeOwnerId;
    readonly ICharacterRepository _repository;
    readonly TimeProvider _timeProvider;
    readonly int _queueCapacity;
    readonly Queue<PersistenceWorkItem> _waiting = new();
    readonly Dictionary<Guid, OperationIdentity> _unfinished = new();
    PersistenceWorkItem? _activeWork;
    PersistenceOperationTracker? _activeCall;
    PersistenceCloseState? _close;
    PersistenceAuthority? _authority;
    PersistenceToken? _characterToken;
    PersistenceToken? _progressToken;
    CharacterClass? _storedClass;
    bool _running;
    bool _acquireSubmitted;
    // Frozen is temporary during resolution; stopped disallows new work even after a terminal refusal.
    bool _frozen;
    bool _stopped;
    bool _fenceLost;
    bool _recoveryRequired;
    bool _released;

    public CharacterPersistenceOwner(
        PersistenceIdentity identity,
        Guid runtimeOwnerId,
        ICharacterRepository repository,
        TimeProvider timeProvider,
        int queueCapacity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (runtimeOwnerId == Guid.Empty)
            throw new ArgumentException("A runtime lifetime is required.", nameof(runtimeOwnerId));
        if (queueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        _identity = identity;
        _runtimeOwnerId = runtimeOwnerId;
        _repository = repository;
        _timeProvider = timeProvider;
        _queueCapacity = queueCapacity;
    }

    /// <summary>Copies IDs whose dispatched operations have no terminal settlement yet.</summary>
    public IReadOnlyCollection<Guid> UnfinishedOperationIds
    {
        get
        {
            lock (_gate)
                return Array.AsReadOnly(_unfinished.Keys.ToArray());
        }
    }

    /// <summary>Captures the only acquire intent for this lifetime; no admission RPC is implied.</summary>
    public PersistenceSubmission TrySubmitAcquire(
        long expectedFence,
        CharacterClass intendedClass,
        SafeDefaults safeDefaults,
        PersistenceDeadline deadline)
    {
        lock (_gate)
        {
            PersistenceSubmitStatus status = AdmissionStatus(deadline);
            if (status != PersistenceSubmitStatus.Accepted)
                return new(status);
            if (_acquireSubmitted || expectedFence < 0 || !Enum.IsDefined(intendedClass) || safeDefaults is null)
                return new(PersistenceSubmitStatus.Invalid);
            AcquireAndLoadRequest request = new(_identity, Guid.NewGuid(), _runtimeOwnerId, expectedFence, intendedClass, safeDefaults);
            _acquireSubmitted = true;
            return Enqueue(new PersistenceWorkItem(request, deadline));
        }
    }

    /// <summary>Queues a safe payload; dispatch binds the authority and tokens confirmed by preceding work.</summary>
    public PersistenceSubmission TrySubmitCheckpoint(SafeCheckpoint checkpoint, PersistenceDeadline deadline)
    {
        lock (_gate)
        {
            PersistenceSubmitStatus status = AdmissionStatus(deadline);
            if (status != PersistenceSubmitStatus.Accepted)
                return new(status);
            if (!_acquireSubmitted || checkpoint is null || (_storedClass is not null && checkpoint.CharacterClass != _storedClass))
                return new(PersistenceSubmitStatus.Invalid);
            if (_authority?.LastSequence == long.MaxValue)
                return new(PersistenceSubmitStatus.Invalid);
            if (_running && _waiting.Count >= _queueCapacity)
                return new(PersistenceSubmitStatus.QueueFull);
            return Enqueue(new PersistenceWorkItem(checkpoint, deadline));
        }
    }

    /// <summary>The first valid ticket and its absolute deadline govern every close of this lifetime.</summary>
    public Task<PersistenceCloseResult> CloseAsync(CloseCaptureTicket ticket, PersistenceDeadline deadline)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(deadline);
        if (!_identity.Equals(ticket.Identity) || _runtimeOwnerId != ticket.RuntimeOwnerId)
            throw new ArgumentException("The close ticket belongs to another lifetime.", nameof(ticket));
        Task<PersistenceCloseResult> completion;
        PersistenceOperationTracker? cancel = null;
        lock (_gate)
        {
            if (_close is not null)
                return _close.Completion.Task;
            _close = new PersistenceCloseState(ticket, deadline);
            // Accepted waiting work is settled locally; the final capture never competes for queue capacity.
            CancelWaiting();
            _close.Watch = new PersistenceDeadlineWatch(_timeProvider, deadline, CloseDeadlineExpired);
            _close.Watch.Start();
            if (deadline.IsExpired)
            {
                CompleteClose(DeadlineCloseStatus());
                cancel = _activeCall;
            }
            StartDrain();
            completion = _close.Completion.Task;
        }
        cancel?.RequestCancellation();
        return completion;
    }

    PersistenceSubmitStatus AdmissionStatus(PersistenceDeadline? deadline)
    {
        if (_close is not null)
            return PersistenceSubmitStatus.Closing;
        if (_frozen || _stopped)
            return PersistenceSubmitStatus.Frozen;
        if (deadline is null)
            return PersistenceSubmitStatus.Invalid;
        return deadline.IsExpired ? PersistenceSubmitStatus.Expired : PersistenceSubmitStatus.Accepted;
    }

    PersistenceSubmission Enqueue(PersistenceWorkItem item)
    {
        if (!_running)
            _activeWork = item;
        else
            _waiting.Enqueue(item);
        StartDrain();
        return new PersistenceSubmission(item.Completion.Task);
    }

    void StartDrain()
    {
        if (_running)
            return;
        _running = true;
        // Even a repository with a blocking synchronous prefix cannot block a submitting game thread.
        _ = Task.Run(DrainAsync);
    }

    async Task DrainAsync()
    {
        while (true)
        {
            PersistenceWorkItem? work;
            PersistenceCloseState? closing = null;
            lock (_gate)
            {
                if (_activeWork is null)
                {
                    if (_close is not null && !_close.Started && !_close.Completion.Task.IsCompleted)
                    {
                        closing = _close;
                        closing.Started = true;
                    }
                    else if (_close is null && !_stopped && !_frozen && _waiting.Count != 0)
                    {
                        _activeWork = _waiting.Dequeue();
                    }
                    else
                    {
                        CancelWaiting();
                        _running = false;
                        return;
                    }
                }
                work = _activeWork;
            }

            if (closing is not null)
            {
                await ExecuteCloseAsync(closing).ConfigureAwait(false);
            }
            else if (work is not null)
            {
                OperationOutcome outcome = work.Acquire is not null
                    ? await ExecuteAcquireAsync(work.Acquire, work.Deadline).ConfigureAwait(false)
                    : await ExecuteCheckpointAsync(work.Checkpoint!, work.Deadline).ConfigureAwait(false);
                lock (_gate)
                {
                    _activeWork = null;
                    work.Completion.TrySetResult(new PersistenceWorkResult(ToWorkStatus(outcome)));
                }
            }
        }
    }

    Task<OperationOutcome> ExecuteAcquireAsync(AcquireAndLoadRequest request, PersistenceDeadline deadline)
        => ExecuteMutationAsync(request.Operation, deadline,
            (budget, token) => _repository.AcquireAndLoadAsync(request, budget, token), ClassifyAcquire);

    Task<OperationOutcome> ExecuteCheckpointAsync(SafeCheckpoint checkpoint, PersistenceDeadline deadline)
    {
        WriteSafeCheckpointRequest request;
        lock (_gate)
        {
            if (_frozen || _stopped || _authority is null || _characterToken is null || _authority.LastSequence == long.MaxValue)
                return Task.FromResult(OperationOutcome.NotDispatched);
            if (checkpoint.CharacterClass != _storedClass)
                return Task.FromResult(OperationOutcome.NotDispatched);
            request = new WriteSafeCheckpointRequest(_authority, _authority.LastSequence + 1, Guid.NewGuid(),
                _characterToken, _progressToken, checkpoint);
        }
        return ExecuteMutationAsync(request.Operation, deadline,
            (budget, token) => _repository.WriteSafeCheckpointAsync(request, budget, token), ClassifyCheckpoint);
    }

    async Task ExecuteCloseAsync(PersistenceCloseState closing)
    {
        lock (_gate)
        {
            if (TryCompleteCloseWithoutCall())
                return;
        }

        if (closing.Ticket.Checkpoint is not null)
        {
            OperationOutcome final = await ExecuteCheckpointAsync(closing.Ticket.Checkpoint, closing.Deadline).ConfigureAwait(false);
            lock (_gate)
            {
                if (final is not (OperationOutcome.Durable or OperationOutcome.CheckpointNoChange))
                {
                    CompleteClose(FailureCloseStatus());
                    return;
                }
            }
        }

        ReleaseRequest request;
        lock (_gate)
        {
            if (TryCompleteCloseWithoutCall())
                return;
            PersistenceAuthority authority = _authority!;
            if (authority.LastSequence == long.MaxValue || authority.Fence == long.MaxValue)
            {
                _recoveryRequired = true;
                CompleteClose(PersistenceCloseStatus.RecoveryRequired);
                return;
            }
            request = new ReleaseRequest(authority, authority.LastSequence + 1, Guid.NewGuid());
        }
        await ExecuteMutationAsync(request.Operation, closing.Deadline,
            (budget, token) => _repository.ReleaseAsync(request, budget, token), ClassifyRelease).ConfigureAwait(false);
        lock (_gate)
            CompleteClose(_released ? PersistenceCloseStatus.Released : FailureCloseStatus());
    }

    bool TryCompleteCloseWithoutCall()
    {
        if (_close!.Completion.Task.IsCompleted)
            return true;
        if (_close.Deadline.IsExpired)
            CompleteClose(DeadlineCloseStatus());
        else if (_fenceLost || _unfinished.Count != 0 || _recoveryRequired)
            CompleteClose(FailureCloseStatus());
        else if (_authority is null)
            CompleteClose(_released ? PersistenceCloseStatus.Released : PersistenceCloseStatus.NotHeld);
        else
            return false;
        return true;
    }

    async Task<OperationOutcome> ExecuteMutationAsync<T>(
        OperationIdentity operation,
        PersistenceDeadline deadline,
        Func<PersistenceDeadline, CancellationToken, Task<T>> invoke,
        Func<T, StoreResult> classify) where T : class
    {
        CallResult<T> call = await CallOnceAsync(operation, deadline, invoke).ConfigureAwait(false);
        if (!call.Started)
            return OperationOutcome.NotDispatched;
        StoreResult result = call.Value is null ? new(OperationOutcome.Unknown) : classify(call.Value);
        OperationOutcome outcome;
        lock (_gate)
        {
            outcome = Settle(operation, result);
            if (outcome != OperationOutcome.Unknown)
                return outcome;
            _frozen = true;
        }

        // Never replay a mutation: both resolver attempts retain the exact operation and typed payload.
        foreach (ResolveMode mode in new[] { ResolveMode.QueryOnly, ResolveMode.SealIfAbsent })
        {
            ResolveOperationRequest request = new(operation, mode);
            CallResult<ResolveOperationResult> resolution = await CallOnceAsync(operation, deadline,
                (budget, token) => _repository.ResolveOperationAsync(request, budget, token)).ConfigureAwait(false);
            if (!resolution.Started)
                break;
            result = resolution.Value is null ? new(OperationOutcome.Unknown) : ClassifyResolution(resolution.Value);
            lock (_gate)
            {
                outcome = Settle(operation, result);
                if (outcome != OperationOutcome.Unknown)
                    return outcome;
            }
        }
        lock (_gate)
        {
            _frozen = true;
            _stopped = true;
            CancelWaiting();
        }
        return OperationOutcome.Unknown;
    }

    async Task<CallResult<T>> CallOnceAsync<T>(
        OperationIdentity operation,
        PersistenceDeadline deadline,
        Func<PersistenceDeadline, CancellationToken, Task<T>> invoke) where T : class
    {
        PersistenceOperationTracker call;
        PersistenceDeadline budget;
        bool newlyTracked;
        lock (_gate)
        {
            budget = EarlierDeadline(deadline);
            if (budget.IsExpired || (_close?.Completion.Task.IsCompleted ?? false))
                return new(false, null);
            call = new PersistenceOperationTracker();
            _activeCall = call;
            newlyTracked = _unfinished.TryAdd(operation.OperationId, operation);
        }
        bool enteredRepository = false;
        try
        {
            call.Watch = new PersistenceDeadlineWatch(_timeProvider, budget, () => CallDeadlineExpired(call));
            call.Watch.Start();
            lock (_gate)
            {
                // Close may have been captured after the reservation; it can shorten, never extend this call.
                budget = EarlierDeadline(budget);
                if (budget.IsExpired || (_close?.Completion.Task.IsCompleted ?? false))
                {
                    if (newlyTracked)
                        _unfinished.Remove(operation.OperationId);
                    return new(false, null);
                }
            }
            enteredRepository = true;
            Task<T> task = invoke(budget, call.Token);
            lock (_gate)
                call.RepositoryTask = task;
            return new(true, await task.ConfigureAwait(false));
        }
        catch (Exception)
        {
            if (!enteredRepository)
            {
                lock (_gate)
                {
                    if (newlyTracked)
                        _unfinished.Remove(operation.OperationId);
                }
                return new(false, null);
            }
            // A throw or cancellation after entering the repository is not proof of rollback.
            return new(true, null);
        }
        finally
        {
            lock (_gate)
                _activeCall = null;
            call.Finish();
        }
    }

    PersistenceDeadline EarlierDeadline(PersistenceDeadline workDeadline)
        => _close is not null && _close.Deadline.GetRemaining() < workDeadline.GetRemaining()
            ? _close.Deadline
            : workDeadline;

    void CallDeadlineExpired(PersistenceOperationTracker call)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_activeCall, call))
                return;
            _frozen = true;
        }
        call.RequestCancellation();
    }

    void CloseDeadlineExpired()
    {
        PersistenceOperationTracker? active;
        lock (_gate)
        {
            if (_close is null || _close.Completion.Task.IsCompleted)
                return;
            _frozen = true;
            CompleteClose(DeadlineCloseStatus());
            active = _activeCall;
        }
        active?.RequestCancellation();
    }

    PersistenceCloseStatus DeadlineCloseStatus()
    {
        if (_activeCall is { } call && (call.RepositoryTask is null || !call.RepositoryTask.IsCompleted))
            return PersistenceCloseStatus.ShutdownIncomplete;
        return FailureCloseStatus();
    }

    PersistenceCloseStatus FailureCloseStatus()
    {
        if (_fenceLost)
            return PersistenceCloseStatus.FenceLost;
        if (_unfinished.Count != 0)
            return PersistenceCloseStatus.Unresolved;
        if (_released)
            return PersistenceCloseStatus.Released;
        return _authority is not null || _recoveryRequired
            ? PersistenceCloseStatus.RecoveryRequired
            : PersistenceCloseStatus.NotHeld;
    }

    void CompleteClose(PersistenceCloseStatus status)
    {
        if (_close!.Completion.TrySetResult(new PersistenceCloseResult(status, _close.Ticket.WriteDecision, _unfinished.Keys)))
            _close.Watch?.Dispose();
    }

    void CancelWaiting()
    {
        while (_waiting.TryDequeue(out PersistenceWorkItem? work))
            work.Completion.TrySetResult(new PersistenceWorkResult(PersistenceWorkStatus.NotDispatched));
    }

    // Only this method advances authority/tokens or removes an operation after a repository answer.
    OperationOutcome Settle(OperationIdentity operation, StoreResult result)
    {
        OperationOutcome outcome = result.Outcome;
        if (outcome == OperationOutcome.Unknown)
            return outcome;
        if (outcome is OperationOutcome.Acquired or OperationOutcome.Durable or OperationOutcome.CheckpointNoChange or OperationOutcome.Released)
        {
            if (!ReceiptMatches(operation, result.Receipt) || result.Current is null)
                return OperationOutcome.Unknown;
            if (!IsCurrent(result.Receipt!, result.Current, outcome == OperationOutcome.Released))
                outcome = OperationOutcome.FenceLost;
            else if (outcome == OperationOutcome.Released)
            {
                _authority = null;
                _released = true;
                _stopped = true;
            }
            else
            {
                _authority = result.Receipt!.Authority;
                _characterToken = result.Receipt.CharacterToken;
                _progressToken = result.Receipt.ProgressToken;
                if (outcome == OperationOutcome.Acquired)
                    _storedClass = result.Receipt.StoredClass;
            }
        }
        else if (outcome == OperationOutcome.NotApplied)
        {
            if (result.Current is null || !_identity.Equals(result.Current.Authority.Identity))
                return OperationOutcome.Unknown;
            bool unchanged = operation.Kind == PersistenceOperationKind.Acquire
                ? result.Current.Authority.OwnerKind == PersistenceOwnerKind.Free && result.Current.Authority.Fence == operation.ExpectedFence
                : _authority is not null && _authority.Equals(result.Current.Authority)
                    && Equals(_characterToken, result.Current.CharacterToken) && Equals(_progressToken, result.Current.ProgressToken);
            if (!unchanged)
                outcome = OperationOutcome.FenceLost;
            // Do not retry or resume mutations. Exact unchanged authority may still be conditionally released.
            _stopped = true;
        }
        else if (outcome == OperationOutcome.FenceLost)
        {
            if (result.Current is null || !_identity.Equals(result.Current.Authority.Identity)
                || (result.Receipt is not null && !ReceiptMatches(operation, result.Receipt)))
                return OperationOutcome.Unknown;
        }

        if (outcome == OperationOutcome.FenceLost)
        {
            _fenceLost = true;
            _authority = null;
            _stopped = true;
        }
        else if (outcome is OperationOutcome.Rejected or OperationOutcome.Conflict)
        {
            _stopped = true;
            _recoveryRequired = _authority is not null;
        }
        _unfinished.Remove(operation.OperationId);
        _frozen = _stopped;
        if (_stopped)
            CancelWaiting();
        return outcome;
    }

    bool ReceiptMatches(OperationIdentity operation, OperationReceipt? receipt)
    {
        if (receipt is null || receipt.OperationId != operation.OperationId || receipt.Kind != operation.Kind
            || !_identity.Equals(receipt.Authority.Identity))
            return false;
        PersistenceAuthority authority = receipt.Authority;
        return operation.Kind switch
        {
            PersistenceOperationKind.Acquire => operation.ExpectedFence < long.MaxValue
                && authority.OwnerKind == PersistenceOwnerKind.Runtime && authority.OwnerId == _runtimeOwnerId
                && authority.Fence == operation.ExpectedFence + 1 && authority.LastSequence == 0,
            PersistenceOperationKind.Checkpoint => authority.OwnerKind == PersistenceOwnerKind.Runtime
                && authority.OwnerId == _runtimeOwnerId && authority.Fence == operation.ExpectedFence
                && authority.LastSequence == operation.Sequence
                && (receipt.StoredClass is null || receipt.StoredClass == _storedClass),
            PersistenceOperationKind.ReleaseRuntime => operation.ExpectedFence < long.MaxValue
                && authority.OwnerKind == PersistenceOwnerKind.Free && authority.Fence == operation.ExpectedFence + 1,
            _ => false
        };
    }

    static bool IsCurrent(OperationReceipt receipt, PersistenceObservation current, bool release)
        => receipt.Authority.Equals(current.Authority)
            && (release || (Equals(receipt.CharacterToken, current.CharacterToken) && Equals(receipt.ProgressToken, current.ProgressToken)));

    static StoreResult ClassifyAcquire(AcquireAndLoadResult result) => result.Status switch
    {
        AcquireAndLoadStatus.Acquired => new(OperationOutcome.Acquired, result.Receipt, result.Current),
        AcquireAndLoadStatus.Rejected => new(OperationOutcome.Rejected),
        AcquireAndLoadStatus.Conflict => new(OperationOutcome.Conflict),
        AcquireAndLoadStatus.NotDispatched => new(OperationOutcome.NotDispatched),
        _ => new(OperationOutcome.Unknown)
    };

    static StoreResult ClassifyCheckpoint(WriteSafeCheckpointResult result) => result.Status switch
    {
        WriteSafeCheckpointStatus.Durable => new(OperationOutcome.Durable, result.Receipt, result.Current),
        WriteSafeCheckpointStatus.CheckpointNoChange => new(OperationOutcome.CheckpointNoChange, result.Receipt, result.Current),
        WriteSafeCheckpointStatus.Rejected => new(OperationOutcome.Rejected),
        WriteSafeCheckpointStatus.Conflict => new(OperationOutcome.Conflict),
        WriteSafeCheckpointStatus.NotDispatched => new(OperationOutcome.NotDispatched),
        _ => new(OperationOutcome.Unknown)
    };

    static StoreResult ClassifyRelease(ReleaseResult result) => result.Status switch
    {
        ReleaseStatus.Released => new(OperationOutcome.Released, result.Receipt, result.Current),
        ReleaseStatus.Rejected => new(OperationOutcome.Rejected),
        ReleaseStatus.NotDispatched => new(OperationOutcome.NotDispatched),
        _ => new(OperationOutcome.Unknown)
    };

    static StoreResult ClassifyResolution(ResolveOperationResult result) => result.Status switch
    {
        OperationResolutionStatus.Applied => new(result.Receipt?.Code switch
        {
            PersistenceResultCode.Acquired => OperationOutcome.Acquired,
            PersistenceResultCode.CheckpointApplied => OperationOutcome.Durable,
            PersistenceResultCode.CheckpointNoChange => OperationOutcome.CheckpointNoChange,
            PersistenceResultCode.Released => OperationOutcome.Released,
            _ => OperationOutcome.Unknown
        }, result.Receipt, result.Current),
        OperationResolutionStatus.NotApplied => new(OperationOutcome.NotApplied, null, result.Current),
        OperationResolutionStatus.FenceLost => new(OperationOutcome.FenceLost, result.Receipt, result.Current),
        _ => new(OperationOutcome.Unknown)
    };

    static PersistenceWorkStatus ToWorkStatus(OperationOutcome outcome) => outcome switch
    {
        OperationOutcome.Acquired => PersistenceWorkStatus.Acquired,
        OperationOutcome.Durable => PersistenceWorkStatus.Durable,
        OperationOutcome.CheckpointNoChange => PersistenceWorkStatus.CheckpointNoChange,
        OperationOutcome.Rejected => PersistenceWorkStatus.Rejected,
        OperationOutcome.Conflict => PersistenceWorkStatus.Conflict,
        OperationOutcome.NotDispatched => PersistenceWorkStatus.NotDispatched,
        OperationOutcome.NotApplied => PersistenceWorkStatus.NotApplied,
        OperationOutcome.FenceLost => PersistenceWorkStatus.FenceLost,
        _ => PersistenceWorkStatus.Unresolved
    };

    enum OperationOutcome
    {
        Unknown, Acquired, Durable, CheckpointNoChange, Released, Rejected, Conflict, NotDispatched, NotApplied, FenceLost
    }

    readonly record struct StoreResult(OperationOutcome Outcome, OperationReceipt? Receipt = null, PersistenceObservation? Current = null);
    readonly record struct CallResult<T>(bool Started, T? Value) where T : class;
}
