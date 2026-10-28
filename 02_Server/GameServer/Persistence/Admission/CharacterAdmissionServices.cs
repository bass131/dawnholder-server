using System.Collections.Concurrent;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Admission;

/// <summary>Fixed store binding. Closing lifetimes outlive the world's account and entity registrations.</summary>
public sealed class CharacterAdmissionServices
{
    readonly PersistenceAccountSelector? _selector;
    readonly PersistenceIdentity? _identity;
    readonly ICharacterRepository? _repository;
    readonly TimeProvider _timeProvider;
    readonly int _queueCapacity;
    readonly ConcurrentDictionary<CharacterAdmissionLifetime, Task> _closures = new();

    public CharacterAdmissionServices(CharacterCatalogService catalog, TimeProvider timeProvider)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _queueCapacity = 4;
    }

    public CharacterAdmissionServices(PersistenceAccountSelector selector, PersistenceIdentity identity,
        ICharacterRepository repository, TimeProvider timeProvider, int queueCapacity = 4)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (selector.AccountId != identity.AccountId)
            throw new ArgumentException("The selector and store binding must identify the same account.", nameof(identity));
        if (queueCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        _selector = selector;
        _identity = identity;
        _repository = repository;
        _timeProvider = timeProvider;
        _queueCapacity = queueCapacity;
    }

    public CharacterStorageMode Mode => CharacterStorageMode.Stored;
    public CharacterCatalogService? Catalog { get; }

    internal TimeProvider TimeProvider => _timeProvider;

    internal bool IsPersistent(Guid accountId) => Catalog is not null || _selector!.IsPersistent(accountId);

    internal CharacterAdmissionLifetime CreateLifetime(CharacterClass intendedClass, SafeDefaults defaults)
        // Old class-only selection must never bypass the explicit stored-character selection API.
        => Catalog is not null ? CharacterAdmissionLifetime.Rejected(_timeProvider)
            : new(_identity!, _repository!, _timeProvider, _queueCapacity, intendedClass, defaults);

    internal void CaptureClose(CharacterAdmissionLifetime lifetime, SafeCheckpoint? projection)
    {
        Task completion = lifetime.Close(projection);
        _closures.TryAdd(lifetime, completion);
        // Only terminal close outcomes remove this retained lifetime; timeout leaves it owned here.
        _ = ForgetSettledAsync(lifetime, completion);
    }

    async Task ForgetSettledAsync(CharacterAdmissionLifetime lifetime, Task completion)
    {
        try
        {
            await completion.ConfigureAwait(false);
            _closures.TryRemove(lifetime, out _);
        }
        catch (Exception)
        {
            // Preserve an incomplete/failed close for the host and later diagnosis.
        }
    }

    internal Task FlushClosuresAsync(PersistenceDeadline deadline)
    {
        ArgumentNullException.ThrowIfNull(deadline);
        Task[] tasks = _closures.Values.ToArray();
        Task all = Catalog is null ? Task.WhenAll(tasks) : Task.WhenAll(tasks.Append(Catalog.FlushAsync(deadline)));
        return all.WaitAsync(deadline.GetRemaining(), _timeProvider);
    }
}
