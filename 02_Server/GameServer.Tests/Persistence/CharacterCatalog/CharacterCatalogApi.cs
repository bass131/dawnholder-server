using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>API v2's fixed public types. Reflection is used only by shape assertions, never to mint a descriptor.</summary>
static class CharacterCatalogApi
{
    public const string Missing = "[API 부재] ";
    public const string ListOperation = "ListCharactersAsync";
    public const string CreateOperation = "CreateCharacterAsync";
    public const string SelectOperation = "BeginSelectCharacter";
    public const string ResolveOperation = "ResolveCreateAsync";

    public static Type ServerType(string name) => name switch
    {
        "AccountContext" => typeof(AccountContext),
        "RequestContext" => typeof(RequestContext),
        "CharacterSummary" => typeof(CharacterSummary),
        "CreateDescriptor" => typeof(CreateDescriptor),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    public static Type CatalogType() => typeof(CharacterCatalogService);

    public static MethodInfo Operation(string name)
        => typeof(CharacterCatalogService).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException(Missing + name);

    public static Built<object> Account(Guid accountId, string? normalizedAccountName)
        => new(new AccountContext(accountId, normalizedAccountName!), null);

    public static object Request(Guid sessionInstanceId, long generation) => new RequestContext(sessionInstanceId, generation);

    public static CatalogUnderTest Offline(StoreCallRecorder store, TimeProvider timeProvider)
        => new(new CharacterCatalogService(
            (ICharacterCatalogRepository)store.CreateProxy(typeof(ICharacterCatalogRepository)),
            (ICharacterRepository)store.CreateProxy(typeof(ICharacterRepository)),
            PersistenceTestValues.Defaults(), timeProvider));

    public static CatalogOutcome ReadOutcome(object result)
    {
        CatalogResult value = Assert.IsType<CatalogResult>(result);
        return new CatalogOutcome(value.Status.ToString(), value.InvalidField.ToString(), value.Rejection?.ToString(),
            value.Request, true, value.Character is null ? null : SummaryView.From(value.Character),
            value.Characters.Select(SummaryView.From).ToArray(), value, value.Lease);
    }
}

readonly record struct Built<T>(T? Value, string? Refused);

sealed record SummaryView(Guid CharacterId, int Slot, string Name, CharacterClass Class)
{
    public static SummaryView From(CharacterSummary summary)
        => new(summary.CharacterId, summary.Slot, summary.Name, summary.Class);
}

sealed record CatalogOutcome(
    string Status,
    string? InvalidField,
    string? NotAppliedCode,
    object? RequestContext,
    bool HasRequestContext,
    SummaryView? Character,
    IReadOnlyList<SummaryView>? Characters,
    object Raw,
    AdmissionLease? Lease = null);

sealed record SelectStart(string? StartRejection, AdmissionHandleView? Handle);

sealed class AdmissionHandleView(CharacterSelectionHandle handle)
{
    public object Raw => handle;
    public Task Completion => handle.Completion;

    public CatalogOutcome CompletedOutcome()
    {
        Assert.True(handle.Completion.IsCompletedSuccessfully, $"Completion is {handle.Completion.Status}");
        return CharacterCatalogApi.ReadOutcome(handle.Completion.GetAwaiter().GetResult());
    }

    public Task Close(SafeCheckpoint? projection) => handle.Close(projection);
}
