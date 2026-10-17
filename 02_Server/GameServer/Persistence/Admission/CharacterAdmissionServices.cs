using System.Collections.Concurrent;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence.Admission;

/// <summary>Fixed store binding. Closing lifetimes outlive the world's account and entity registrations.</summary>
public sealed class CharacterAdmissionServices
{
    readonly PersistenceAccountSelector _selector;
    readonly PersistenceIdentity _identity;
    readonly ICharacterRepository _repository;
    readonly TimeProvider _timeProvider;
    readonly int _queueCapacity;
    readonly ConcurrentDictionary<CharacterAdmissionLifetime, Task> _closures = new();

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

    internal bool IsPersistent(Guid accountId) => _selector.IsPersistent(accountId);

    internal CharacterAdmissionLifetime CreateLifetime(CharacterClass intendedClass, SafeDefaults defaults)
        => new(_identity, _repository, _timeProvider, _queueCapacity, intendedClass, defaults);

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
        Task all = Task.WhenAll(_closures.Values.ToArray());
        return all.WaitAsync(deadline.GetRemaining(), _timeProvider);
    }
}
