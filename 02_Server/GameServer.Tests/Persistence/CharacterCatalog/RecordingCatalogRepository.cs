using System.Collections.Concurrent;
using System.Threading.Channels;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Records public storage calls without choosing their outcome. Tests release each barrier explicitly.</summary>
sealed class RecordingCatalogRepository : ICharacterCatalogRepository
{
    readonly Channel<Call> _arrivals = Channel.CreateUnbounded<Call>();
    readonly ConcurrentQueue<Call> _calls = new();

    public IReadOnlyList<Call> Calls => _calls.ToArray();

    public Task<CatalogStorageResult> ListAsync(AccountContext account, PersistenceDeadline deadline, CancellationToken token)
        => Record("List", null, Guid.Empty, account, deadline, token);

    public Task<CatalogStorageResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        SafeDefaults defaults, PersistenceDeadline deadline, CancellationToken token)
        => Record("Create", descriptor, operationId, descriptor.Account, deadline, token);

    public Task<CatalogStorageResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        PersistenceDeadline deadline, CancellationToken token)
        => Record("Resolve", descriptor, operationId, descriptor.Account, deadline, token);

    public Task<CatalogStorageResult> ReadSelectionAsync(AccountContext account, Guid characterId,
        PersistenceDeadline deadline, CancellationToken token)
        => Record("Select", null, characterId, account, deadline, token);

    public async Task<Call> Next(string method)
    {
        Call call = await _arrivals.Reader.ReadAsync().AsTask().WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(method, call.Method);
        return call;
    }

    Task<CatalogStorageResult> Record(string method, CreateDescriptor? descriptor, Guid operationId,
        AccountContext account, PersistenceDeadline deadline, CancellationToken token)
    {
        Call call = new(method, descriptor, operationId, account, deadline, token);
        _calls.Enqueue(call);
        Assert.True(_arrivals.Writer.TryWrite(call));
        return call.Completion.Task;
    }

    internal sealed record Call(string Method, CreateDescriptor? Descriptor, Guid OperationId,
        AccountContext Account, PersistenceDeadline Deadline, CancellationToken Token)
    {
        public TaskCompletionSource<CatalogStorageResult> Completion { get; }
            = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Complete(CatalogStorageResult result) => Completion.SetResult(result);
    }
}
