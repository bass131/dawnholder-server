using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Admission;

/// <summary>One probe/acquire and final immutable capture. No continuation touches game entities.</summary>
internal sealed class CharacterAdmissionLifetime
{
    readonly object _gate = new();
    readonly PersistenceIdentity _identity;
    readonly Guid _runtimeOwnerId = Guid.NewGuid();
    readonly CharacterPersistenceOwner _owner;
    readonly ICharacterRepository _repository;
    readonly TimeProvider _timeProvider;
    readonly SafeDefaults _defaults;
    readonly CharacterClass _intendedClass;
    readonly Task<CharacterAdmissionResult> _admission;
    bool _closing;
    SafeCheckpoint? _baseline;
    Task? _close;

    CharacterAdmissionLifetime(TimeProvider timeProvider)
    {
        _identity = null!;
        _owner = null!;
        _repository = null!;
        _defaults = null!;
        _timeProvider = timeProvider;
        _admission = Task.FromResult(new CharacterAdmissionResult(CharacterLoadStatus.Unavailable));
    }

    internal CharacterAdmissionLifetime(PersistenceIdentity identity, ICharacterRepository repository,
        TimeProvider timeProvider, int queueCapacity, CharacterClass intendedClass, SafeDefaults defaults)
    {
        _identity = identity;
        _repository = repository;
        _timeProvider = timeProvider;
        _intendedClass = intendedClass;
        _defaults = defaults;
        _owner = new CharacterPersistenceOwner(identity, _runtimeOwnerId, repository, timeProvider, queueCapacity);
        // A blocking repository prefix must not execute on the game tick.
        _admission = Task.Run(LoadAsync);
    }

    internal Task<CharacterAdmissionResult> Completion => _admission;

    internal static CharacterAdmissionLifetime Rejected(TimeProvider timeProvider) => new(timeProvider);

    async Task<CharacterAdmissionResult> LoadAsync()
    {
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(_timeProvider, TimeSpan.FromSeconds(5));
        try
        {
            ReadAdmissionResult read = await _repository.ReadAdmissionAsync(new ReadAdmissionRequest(_identity),
                deadline, CancellationToken.None).WaitAsync(deadline.GetRemaining(), _timeProvider).ConfigureAwait(false);
            PersistenceSubmission submission;
            lock (_gate)
            {
                if (_closing || deadline.IsExpired) return new(CharacterLoadStatus.Unavailable);
                AdmissionProbe? probe = read.Admission;
                if (read.Status != ReadAdmissionStatus.Probe || probe is null || !probe.SchemaCompatible ||
                    !probe.BindingMatches || !_identity.Equals(probe.ObservedAuthority.Identity))
                    return new(CharacterLoadStatus.Unavailable);
                PersistenceAuthority observed = probe.ObservedAuthority;
                if (observed.OwnerKind == PersistenceOwnerKind.Recovery)
                    return new(CharacterLoadStatus.RecoveryRequired);
                if (observed.OwnerKind == PersistenceOwnerKind.Runtime)
                    return new(CharacterLoadStatus.Busy);
                if (observed.OwnerKind != PersistenceOwnerKind.Free)
                    return new(CharacterLoadStatus.Unavailable);
                submission = _owner.TrySubmitAcquire(observed.Fence, _intendedClass, _defaults, deadline);
            }
            if (submission.Completion is null) return new(CharacterLoadStatus.Unavailable);
            PersistenceWorkResult acquired = await submission.Completion.WaitAsync(
                deadline.GetRemaining(), _timeProvider).ConfigureAwait(false);
            if (acquired.Status == PersistenceWorkStatus.Acquired && acquired.LoadedClass is CharacterClass loaded &&
                (loaded == CharacterClass.Knight || loaded == CharacterClass.Mage))
            {
                lock (_gate)
                    _baseline = Projection(loaded);
                return new(CharacterLoadStatus.Success, loaded);
            }
            return new(acquired.Status == PersistenceWorkStatus.Conflict
                ? CharacterLoadStatus.Busy : CharacterLoadStatus.Unavailable);
        }
        catch (Exception)
        {
            // Public admission failure never contains repository exception details or secrets.
            return new(CharacterLoadStatus.Unavailable);
        }
    }

    SafeCheckpoint Projection(CharacterClass loaded)
        => new(loaded, _defaults.TownX, _defaults.TownY, PlayerStats.ForClass(loaded).MaxHp);

    internal Task Close(SafeCheckpoint? projection)
    {
        lock (_gate)
        {
            if (_close is not null) return _close;
            _closing = true;
            if (_owner is null) return _close = Task.CompletedTask;
            PersistenceDeadline deadline = PersistenceDeadline.FromBudget(_timeProvider, TimeSpan.FromSeconds(2));
            SafeCheckpoint? changed = projection is not null && !projection.Equals(_baseline) ? projection : null;
            Task<PersistenceCloseResult> ownerClose = _owner.CloseAsync(
                new CloseCaptureTicket(_identity, _runtimeOwnerId, changed), deadline);
            _close = SettleCloseAsync(ownerClose, deadline);
            return _close;
        }
    }

    async Task SettleCloseAsync(Task<PersistenceCloseResult> ownerClose, PersistenceDeadline deadline)
    {
        // Read-only probes still belong to this lifetime until they return or the waiter budget ends.
        await _admission.WaitAsync(deadline.GetRemaining(), _timeProvider).ConfigureAwait(false);
        PersistenceCloseResult result = await ownerClose.ConfigureAwait(false);
        if (result.Status is not (PersistenceCloseStatus.Released or PersistenceCloseStatus.NotHeld))
            throw new InvalidOperationException($"Persistence close remains {result.Status}.");
    }
}
