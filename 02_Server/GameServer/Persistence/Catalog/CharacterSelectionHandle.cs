using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Catalog;

/// <summary>Opaque gameplay access to one acquired lifetime; the owner rejects checkpoint work after close/fence loss.</summary>
public sealed class AdmissionLease
{
    readonly CharacterPersistenceOwner _owner;

    internal AdmissionLease(CharacterPersistenceOwner owner) => _owner = owner;

    public PersistenceSubmission Checkpoint(SafeCheckpoint checkpoint, PersistenceDeadline deadline)
        => _owner.TrySubmitCheckpoint(checkpoint, deadline);
}

/// <summary>Returned before I/O. Close and acquire share a gate; no continuation can register a game entity.</summary>
public sealed class CharacterSelectionHandle
{
    readonly object _gate = new();
    readonly TaskCompletionSource<CatalogResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly AccountContext? _account;
    readonly Guid _characterId;
    readonly Guid _operationId;
    readonly Guid _ownerId = Guid.NewGuid();
    readonly RequestContext _request;
    readonly PersistenceDeadline? _deadline;
    readonly CancellationToken _cancellation;
    readonly ICharacterCatalogRepository? _catalog;
    readonly ICharacterRepository? _repository;
    readonly SafeDefaults? _defaults;
    readonly TimeProvider _clock;
    CharacterPersistenceOwner? _owner;
    PersistenceIdentity? _identity;
    Task? _close;
    bool _closing;
    bool _selected;

    internal CharacterSelectionHandle(AccountContext account, Guid characterId, Guid operationId, RequestContext request,
        PersistenceDeadline deadline, CancellationToken cancellation, ICharacterCatalogRepository catalog,
        ICharacterRepository repository, SafeDefaults defaults, TimeProvider clock)
    {
        _account = account;
        _characterId = characterId;
        _operationId = operationId;
        _request = request;
        _deadline = deadline;
        _cancellation = cancellation;
        _catalog = catalog;
        _repository = repository;
        _defaults = defaults;
        _clock = clock;
    }

    CharacterSelectionHandle(CatalogResult refusal)
    {
        _request = refusal.Request;
        _clock = TimeProvider.System;
        _completion.SetResult(refusal);
        _closed.SetResult();
    }

    public Task<CatalogResult> Completion => _completion.Task;
    internal Task Closed => _closed.Task;
    internal bool IsSelected { get { lock (_gate) return _selected; } }

    public Task Close(SafeCheckpoint? finalProjection)
    {
        lock (_gate)
        {
            if (_close is not null) return _close;
            _closing = true;
            PersistenceDeadline budget = PersistenceDeadline.FromBudget(_clock, TimeSpan.FromSeconds(2));
            Task<PersistenceCloseResult>? ownerClose = _owner?.CloseAsync(
                new CloseCaptureTicket(_identity!, _ownerId, _selected ? finalProjection : null), budget);
            _close = FinishCloseAsync(ownerClose, budget);
            return _close;
        }
    }

    internal static CharacterSelectionHandle Rejected(CatalogResult refusal) => new(refusal);

    internal void Start() => _ = Task.Run(LoadAsync);

    async Task LoadAsync()
    {
        CatalogResult result;
        try { result = await SelectAsync().ConfigureAwait(false); }
        catch (Exception) { result = new(CatalogStatus.OutcomeUnknown, _request); }
        lock (_gate)
        {
            // A confirmed non-dispatch remains certain even when cancellation or close wins publication.
            if (result.Status != CatalogStatus.CancelledBeforeDispatch &&
                (_closing || _cancellation.IsCancellationRequested || _deadline!.IsExpired))
                result = new(CatalogStatus.OutcomeUnknown, _request);
            _selected = result.Status == CatalogStatus.Selected;
            _completion.TrySetResult(result);
        }
        if (result.Status != CatalogStatus.Selected) _ = CharacterCatalogService.ObserveAsync(Close(null));
    }

    async Task<CatalogResult> SelectAsync()
    {
        if (_cancellation.IsCancellationRequested || _deadline!.IsExpired)
            return new(CatalogStatus.CancelledBeforeDispatch, _request);
        Task<CatalogStorageResult> selection = Task.Run(() => _catalog!.ReadSelectionAsync(
            _account!, _characterId, _deadline, _cancellation));
        CatalogStorageResult read;
        try { read = await selection.WaitAsync(_deadline.GetRemaining(), _clock, _cancellation).ConfigureAwait(false); }
        catch (Exception)
        {
            _ = CharacterCatalogService.ObserveAsync(selection);
            return new(CatalogStatus.Unavailable, _request);
        }
        if (read is null) return new(CatalogStatus.Unavailable, _request);
        if (read.Status != CatalogStatus.Selected) return new(read.Status, _request);
        if (read.Character is not { } summary || summary.CharacterId != _characterId || !CatalogValidation.Summary(summary))
            return new(CatalogStatus.IntegrityFailure, _request);
        PersistenceIdentity identity = new(summary.Slot, _account!.AccountId, summary.CharacterId);
        Task<ReadAdmissionResult> probeTask = Task.Run(() => _repository!.ReadAdmissionAsync(
            new ReadAdmissionRequest(identity), _deadline, _cancellation));
        ReadAdmissionResult probe;
        try { probe = await probeTask.WaitAsync(_deadline.GetRemaining(), _clock, _cancellation).ConfigureAwait(false); }
        catch (Exception)
        {
            _ = CharacterCatalogService.ObserveAsync(probeTask);
            return new(CatalogStatus.Unavailable, _request);
        }
        if (probe?.Status is ReadAdmissionStatus.ReadFailed or ReadAdmissionStatus.NotDispatched)
            return new(CatalogStatus.Unavailable, _request);
        if (probe?.Status != ReadAdmissionStatus.Probe || probe.Admission is not { } admission ||
            admission.ObservedAuthority is not { } authority)
            return new(CatalogStatus.IntegrityFailure, _request);
        // API order is structural validation, then schema, then identity; none of these failures publish stored data.
        if (!admission.SchemaCompatible) return new(CatalogStatus.SchemaMismatch, _request);
        if (!admission.BindingMatches || !identity.Equals(authority.Identity))
            return new(CatalogStatus.IntegrityFailure, _request);
        if (authority.OwnerKind == PersistenceOwnerKind.Recovery) return new(CatalogStatus.RecoveryRequired, _request);
        if (authority.OwnerKind == PersistenceOwnerKind.Runtime) return new(CatalogStatus.Busy, _request);
        PersistenceSubmission submission;
        lock (_gate)
        {
            if (_closing || _cancellation.IsCancellationRequested || _deadline.IsExpired)
                return new(CatalogStatus.CancelledBeforeDispatch, _request);
            _identity = identity;
            _owner = new CharacterPersistenceOwner(identity, _ownerId, _repository!, _clock, queueCapacity: 4);
            submission = _owner.TrySubmitAcquire(authority.Fence, summary.Class, _defaults!, _deadline, _operationId);
        }
        if (submission.Completion is null) return new(CatalogStatus.Unavailable, _request);
        PersistenceWorkResult acquired = await submission.Completion.WaitAsync(
            _deadline.GetRemaining(), _clock, _cancellation).ConfigureAwait(false);
        if (acquired.Status != PersistenceWorkStatus.Acquired)
            return new(acquired.Status switch
            {
                PersistenceWorkStatus.Conflict => CatalogStatus.OperationConflict,
                PersistenceWorkStatus.Unresolved or PersistenceWorkStatus.FenceLost => CatalogStatus.OutcomeUnknown,
                PersistenceWorkStatus.NotDispatched => CatalogStatus.CancelledBeforeDispatch,
                _ => CatalogStatus.Busy
            }, _request);
        if (acquired.LoadedClass != summary.Class) return new(CatalogStatus.IntegrityFailure, _request);
        return new(CatalogStatus.Selected, _request, summary, lease: new AdmissionLease(_owner!));
    }

    async Task FinishCloseAsync(Task<PersistenceCloseResult>? close, PersistenceDeadline budget)
    {
        try
        {
            await Completion.WaitAsync(budget.GetRemaining(), _clock).ConfigureAwait(false);
            if (close is not null)
            {
                PersistenceCloseResult result = await close.WaitAsync(budget.GetRemaining(), _clock).ConfigureAwait(false);
                if (result.Status is not (PersistenceCloseStatus.Released or PersistenceCloseStatus.NotHeld))
                    throw new InvalidOperationException("Selection close remains incomplete.");
            }
            _closed.TrySetResult();
        }
        catch (Exception)
        {
            _closed.TrySetException(new InvalidOperationException("Selection close remains incomplete."));
            throw;
        }
    }
}
