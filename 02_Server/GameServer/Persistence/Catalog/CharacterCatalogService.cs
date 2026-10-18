using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Catalog;

/// <summary>Host-owned catalog. Registration precedes dispatch; caller cancellation never abandons database work.</summary>
public sealed class CharacterCatalogService
{
    readonly object _gate = new();
    readonly ICharacterCatalogRepository _catalog;
    readonly ICharacterRepository _characters;
    readonly TimeProvider _clock;
    readonly SafeDefaults _defaults;
    readonly int _capacity;
    readonly Dictionary<Guid, PendingCreate> _pending = new();
    readonly Dictionary<Guid, CharacterSelectionHandle> _selections = new();
    bool _closing;

    public CharacterCatalogService(ICharacterCatalogRepository catalog, ICharacterRepository characters,
        SafeDefaults defaults, TimeProvider timeProvider, int capacity = 128)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        _clock = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        if (capacity is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public CreatePreparationResult PrepareCreate(AccountContext account, byte slot, string rawName, CharacterClass characterClass)
    {
        if (!CatalogValidation.Account(account)) return new(CatalogStatus.InvalidInput, InvalidField: InvalidField.Account);
        if (slot is < 1 or > 3) return new(CatalogStatus.InvalidInput, InvalidField: InvalidField.Slot);
        if (!CatalogValidation.Name(rawName, out string name, out string key))
            return new(CatalogStatus.InvalidInput, InvalidField: InvalidField.Name);
        if (characterClass is not (CharacterClass.Knight or CharacterClass.Mage))
            return new(CatalogStatus.InvalidInput, InvalidField: InvalidField.Class);
        return new(CatalogStatus.Prepared, new CreateDescriptor(account, slot, name, key, characterClass));
    }

    public async Task<CatalogResult> ListCharactersAsync(AccountContext account, RequestContext request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        CatalogResult? refusal = Validate(account, request, deadline);
        if (refusal is not null) return refusal;
        if (cancellationToken.IsCancellationRequested || deadline.IsExpired)
            return new(CatalogStatus.CancelledBeforeDispatch, request);
        lock (_gate)
            if (_closing) return new(CatalogStatus.Unavailable, request);
        Task<CatalogStorageResult> read = Task.Run(() => _catalog.ListAsync(account, deadline, cancellationToken));
        CatalogStorageResult result = await WaitReadAsync(read, deadline, cancellationToken).ConfigureAwait(false);
        if (result.Status == CatalogStatus.Listed && (result.Characters.Count > 3 ||
            result.Characters.Any(c => !CatalogValidation.Summary(c)) ||
            result.Characters.Select(c => c.Slot).Distinct().Count() != result.Characters.Count ||
            result.Characters.Select(c => c.CharacterId).Distinct().Count() != result.Characters.Count))
            return new(CatalogStatus.IntegrityFailure, request);
        return new(result.Status, request, characters: result.Characters.OrderBy(c => c.Slot));
    }

    public async Task<CatalogResult> CreateCharacterAsync(CreateDescriptor descriptor, Guid operationId,
        RequestContext request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        CatalogResult? refusal = Validate(descriptor?.Account, request, deadline, operationId);
        if (refusal is not null) return refusal;
        if (cancellationToken.IsCancellationRequested || deadline.IsExpired)
            return new(CatalogStatus.CancelledBeforeDispatch, request);
        PendingCreate pending;
        lock (_gate)
        {
            if (_pending.TryGetValue(operationId, out PendingCreate? existing))
            {
                if (!existing.Descriptor.Equals(descriptor)) return new(CatalogStatus.OperationConflict, request);
                pending = existing;
            }
            else
            {
                if (_closing || _pending.Count >= _capacity) return new(CatalogStatus.Unavailable, request);
                pending = new PendingCreate(descriptor!, operationId, deadline, _catalog, _defaults, _clock);
                _pending.Add(operationId, pending);
                // Start only after the host has taken ownership. No game state callback exists here.
                pending.Start();
                _ = ForgetTerminalCreateAsync(pending);
            }
        }
        CatalogStorageResult result;
        try
        {
            TimeSpan remaining = Min(deadline.GetRemaining(), pending.Deadline.GetRemaining());
            result = await pending.UserCompletion.WaitAsync(remaining, _clock, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) { return new(CatalogStatus.OutcomeUnknown, request); }
        return Map(result, request, descriptor);
    }

    public async Task<CatalogResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        RequestContext request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        CatalogResult? refusal = Validate(descriptor?.Account, request, deadline, operationId);
        if (refusal is not null) return refusal;
        lock (_gate)
        {
            if (_pending.TryGetValue(operationId, out PendingCreate? pending))
            {
                if (!pending.Descriptor.Equals(descriptor)) return new(CatalogStatus.OperationConflict, request);
                // A new generation is not a new user budget. Host recovery cannot grant a fresh success.
                if (pending.Deadline.IsExpired) return new(CatalogStatus.StillUnknown, request);
                deadline = pending.Deadline;
            }
            if (_closing) return new(CatalogStatus.StillUnknown, request);
        }
        if (deadline.IsExpired || cancellationToken.IsCancellationRequested) return new(CatalogStatus.StillUnknown, request);
        Task<CatalogStorageResult> read = Task.Run(() => _catalog.ResolveCreateAsync(
            descriptor!, operationId, deadline, cancellationToken));
        CatalogStorageResult result = await WaitReadAsync(read, deadline, cancellationToken).ConfigureAwait(false);
        if (result.Status == CatalogStatus.Unavailable) result = new(CatalogStatus.StillUnknown);
        return Map(result, request, descriptor);
    }

    public CharacterSelectionHandle BeginSelectCharacter(AccountContext account, Guid characterId, Guid operationId,
        RequestContext request, PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        CatalogResult? refusal = Validate(account, request, deadline, operationId);
        if (refusal is null && characterId == Guid.Empty)
            refusal = new(CatalogStatus.InvalidInput, request, invalidField: InvalidField.CharacterId);
        lock (_gate)
        {
            if (refusal is null && (_closing || _selections.Count >= _capacity))
                refusal = new(CatalogStatus.InvalidState, request);
            if (refusal is null && _selections.TryGetValue(request.SessionInstanceId, out CharacterSelectionHandle? previous))
                refusal = new(previous.IsSelected ? CatalogStatus.InvalidState : CatalogStatus.RequestInProgress, request);
            if (refusal is not null) return CharacterSelectionHandle.Rejected(refusal);
            CharacterSelectionHandle handle = new(account, characterId, operationId, request, deadline,
                cancellationToken, _catalog, _characters, _defaults, _clock);
            _selections.Add(request.SessionInstanceId, handle);
            handle.Start();
            _ = ForgetClosedSelectionAsync(request.SessionInstanceId, handle);
            return handle;
        }
    }

    /// <summary>Stops admission first, then awaits the same bounded work and selection closures. Unknown stays incomplete.</summary>
    public async Task FlushAsync(PersistenceDeadline deadline)
    {
        PendingCreate[] creates;
        CharacterSelectionHandle[] selections;
        lock (_gate)
        {
            _closing = true;
            creates = _pending.Values.ToArray();
            selections = _selections.Values.ToArray();
        }
        Task[] closures = selections.Select(h => h.Close(null)).ToArray();
        await Task.WhenAll(closures.Concat(creates.Select(p => p.Settlement)))
            .WaitAsync(deadline.GetRemaining(), _clock).ConfigureAwait(false);
        if (creates.Any(p => !p.IsTerminal)) throw new InvalidOperationException("Catalog recovery remains incomplete.");
    }

    async Task ForgetTerminalCreateAsync(PendingCreate pending)
    {
        await pending.Settlement.ConfigureAwait(false);
        if (pending.IsTerminal)
            lock (_gate) _pending.Remove(pending.OperationId);
    }

    async Task ForgetClosedSelectionAsync(Guid session, CharacterSelectionHandle handle)
    {
        try
        {
            await handle.Closed.ConfigureAwait(false);
            lock (_gate) _selections.Remove(session);
        }
        catch (Exception) { /* Incomplete closure remains host-owned and consumes bounded capacity. */ }
    }

    async Task<CatalogStorageResult> WaitReadAsync(Task<CatalogStorageResult> task,
        PersistenceDeadline deadline, CancellationToken token)
    {
        try { return await task.WaitAsync(deadline.GetRemaining(), _clock, token).ConfigureAwait(false) ?? new(CatalogStatus.Unavailable); }
        catch (Exception)
        {
            _ = ObserveAsync(task);
            return new(CatalogStatus.Unavailable);
        }
    }

    static CatalogResult? Validate(AccountContext? account, RequestContext request,
        PersistenceDeadline deadline, Guid? operation = null)
    {
        if (!CatalogValidation.Account(account)) return new(CatalogStatus.InvalidInput, request, invalidField: InvalidField.Account);
        if (!CatalogValidation.Request(request) || deadline is null)
            return new(CatalogStatus.InvalidInput, request, invalidField: InvalidField.Request);
        return operation == Guid.Empty ? new(CatalogStatus.InvalidInput, request, invalidField: InvalidField.OperationId) : null;
    }

    static CatalogResult Map(CatalogStorageResult result, RequestContext request, CreateDescriptor? descriptor)
    {
        if (result.Status is CatalogStatus.Created or CatalogStatus.Applied &&
            (result.Character is not { } c || !CatalogValidation.Summary(c) || descriptor is null ||
            c.Slot != descriptor.Slot || c.Name != descriptor.Name || c.Class != descriptor.Class))
            return new(CatalogStatus.IntegrityFailure, request);
        return new(result.Status, request, result.Character, rejection: result.Rejection);
    }

    internal static async Task ObserveAsync(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (Exception) { /* Observed; no raw repository error crosses the public boundary. */ }
    }

    static TimeSpan Min(TimeSpan left, TimeSpan right) => left < right ? left : right;
}
