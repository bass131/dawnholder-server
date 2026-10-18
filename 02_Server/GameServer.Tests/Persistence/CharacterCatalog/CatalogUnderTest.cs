using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog.CharacterCatalogApi;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Raw test inputs; only Core PrepareCreate may turn them into a descriptor.</summary>
sealed record CatalogCall(
    object Account,
    object Request,
    PersistenceDeadline Deadline,
    CancellationToken Token,
    int Slot = 1,
    string? Name = null,
    CharacterClass Class = CharacterClass.Knight,
    Guid OperationId = default,
    Guid CharacterId = default,
    CreateDescriptor? Descriptor = null);

/// <summary>Typed API v2 binding. Explicit descriptors are forwarded by reference, with no payload reconstruction.</summary>
sealed class CatalogUnderTest(CharacterCatalogService catalog)
{
    public CharacterCatalogService Raw => catalog;

    public Task<CatalogOutcome> ListAsync(CatalogCall call) => AwaitOutcome(ListOperation, call);
    public Task<CatalogOutcome> CreateAsync(CatalogCall call) => AwaitOutcome(CreateOperation, call);
    public Task<CatalogOutcome> ResolveCreateAsync(CatalogCall call) => AwaitOutcome(ResolveOperation, call);
    public SelectStart BeginSelect(CatalogCall call)
        => new(null, new AdmissionHandleView((CharacterSelectionHandle)Invoke(SelectOperation, call)!));

    public CreatePreparationResult Prepare(CatalogCall call)
        => catalog.PrepareCreate((AccountContext)call.Account, checked((byte)call.Slot), call.Name!, call.Class);

    public object? Invoke(string operation, CatalogCall call)
    {
        AccountContext account = (AccountContext)call.Account;
        RequestContext request = (RequestContext)call.Request;
        if (operation == ListOperation)
            return catalog.ListCharactersAsync(account, request, call.Deadline, call.Token);
        if (operation == SelectOperation)
            return catalog.BeginSelectCharacter(account, call.CharacterId, call.OperationId, request, call.Deadline, call.Token);
        CreateDescriptor? descriptor = call.Descriptor;
        if (descriptor is null)
        {
            CreatePreparationResult prepared = Prepare(call);
            if (prepared.Status != CatalogStatus.Prepared)
            {
                // Raw-input v1 tests now observe preparation; no mutation is submitted on refusal.
                return Task.FromResult(new CatalogResult(prepared.Status, request, invalidField: prepared.InvalidField));
            }
            descriptor = prepared.Descriptor;
        }
        Assert.NotNull(descriptor);
        return operation switch
        {
            CreateOperation => catalog.CreateCharacterAsync(descriptor!, call.OperationId, request, call.Deadline, call.Token),
            ResolveOperation => catalog.ResolveCreateAsync(descriptor!, call.OperationId, request, call.Deadline, call.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    async Task<CatalogOutcome> AwaitOutcome(string operation, CatalogCall call)
        => ReadOutcome(await ((Task<CatalogResult>)Invoke(operation, call)!).WaitAsync(StoreCallRecorder.Limit));
}
