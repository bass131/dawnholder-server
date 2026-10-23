namespace Dawnholder.Server.GameServer.Persistence.Catalog;

/// <summary>One dispatch and at most one five-second receipt reconciliation, separate from the original user budget.</summary>
internal sealed class PendingCreate
{
    readonly ICharacterCatalogRepository _repository;
    readonly SafeDefaults _defaults;
    readonly TimeProvider _clock;
    readonly TaskCompletionSource<CatalogStorageResult> _user = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _settlement = Task.CompletedTask;
    bool _terminal;

    internal PendingCreate(CreateDescriptor descriptor, Guid operationId, PersistenceDeadline deadline,
        ICharacterCatalogRepository repository, SafeDefaults defaults, TimeProvider clock)
    {
        Descriptor = descriptor;
        OperationId = operationId;
        Deadline = deadline;
        _repository = repository;
        _defaults = defaults;
        _clock = clock;
    }

    internal CreateDescriptor Descriptor { get; }
    internal Guid OperationId { get; }
    internal PersistenceDeadline Deadline { get; }
    internal Task<CatalogStorageResult> UserCompletion => _user.Task;
    internal Task Settlement => _settlement;
    internal bool IsTerminal => Volatile.Read(ref _terminal);

    internal void Start() => _settlement = Task.Run(RunAsync);

    async Task RunAsync()
    {
        CatalogStorageResult result;
        using CancellationTokenSource dispatch = new();
        Task<CatalogStorageResult>? operation = null;
        try
        {
            if (Deadline.IsExpired)
                result = new(CatalogStatus.CancelledBeforeDispatch);
            else
            {
                dispatch.CancelAfter(Deadline.GetRemaining());
                // Isolate an uncooperative synchronous repository prefix as well as its returned task.
                operation = Task.Run(() => _repository.CreateAsync(Descriptor, OperationId, _defaults, Deadline, dispatch.Token));
                result = await operation.WaitAsync(Deadline.GetRemaining(), _clock).ConfigureAwait(false)
                    ?? new(CatalogStatus.OutcomeUnknown);
            }
        }
        catch (Exception)
        {
            if (operation is not null) _ = CharacterCatalogService.ObserveAsync(operation);
            result = new(CatalogStatus.OutcomeUnknown);
        }
        _user.TrySetResult(result);
        if (result.Status is CatalogStatus.Created or CatalogStatus.SlotsFull or CatalogStatus.SlotOccupied or
            CatalogStatus.NameTaken or CatalogStatus.CancelledBeforeDispatch or CatalogStatus.OperationConflict or
            CatalogStatus.SchemaMismatch or CatalogStatus.InvalidInput or CatalogStatus.Unavailable)
        {
            Volatile.Write(ref _terminal, true);
            return;
        }

        PersistenceDeadline recovery = PersistenceDeadline.FromBudget(_clock, TimeSpan.FromSeconds(5));
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(5), _clock);
        Task<CatalogStorageResult> resolve = Task.Run(() => _repository.ResolveCreateAsync(
            Descriptor, OperationId, recovery, cancellation.Token));
        try
        {
            CatalogStorageResult resolved = await resolve.WaitAsync(recovery.GetRemaining(), _clock).ConfigureAwait(false);
            Volatile.Write(ref _terminal, resolved?.Status is CatalogStatus.Applied or CatalogStatus.NotApplied or CatalogStatus.OperationConflict);
        }
        catch (Exception) { _ = CharacterCatalogService.ObserveAsync(resolve); }
        // No new user success is published from this recovery lifetime.
    }
}
