using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Server.GameServer.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Connection construction failure precedes command dispatch; it is distinct from corrupted SQL results.</summary>
public sealed class SqlCharacterCatalogBoundaryTests
{
    [Theory]
    [InlineData("List", "operation", CatalogStatus.Unavailable)]
    [InlineData("Create", "operation", CatalogStatus.Unavailable)]
    [InlineData("Resolve", "operation", CatalogStatus.StillUnknown)]
    [InlineData("Select", "operation", CatalogStatus.Unavailable)]
    [InlineData("List", "data", CatalogStatus.Unavailable)]
    [InlineData("Create", "data", CatalogStatus.Unavailable)]
    [InlineData("Resolve", "data", CatalogStatus.StillUnknown)]
    [InlineData("Select", "data", CatalogStatus.Unavailable)]
    [InlineData("List", "json", CatalogStatus.Unavailable)]
    [InlineData("Create", "json", CatalogStatus.Unavailable)]
    [InlineData("Resolve", "json", CatalogStatus.StillUnknown)]
    [InlineData("Select", "json", CatalogStatus.Unavailable)]
    [InlineData("List", "format", CatalogStatus.Unavailable)]
    [InlineData("Create", "format", CatalogStatus.Unavailable)]
    [InlineData("Resolve", "format", CatalogStatus.StillUnknown)]
    [InlineData("Select", "format", CatalogStatus.Unavailable)]
    public async Task ConnectionFactoryFailure_PreservesPredispatchCertainty(
        string operation, string failure, CatalogStatus expected)
    {
        int connectionRequests = 0;
        SqlConnection RefuseConnection()
        {
            Interlocked.Increment(ref connectionRequests);
            // Matching parser exception types still originate before dispatch in this test.
            throw FactoryFailure(failure);
        }

        ControllableTimeProvider clock = new();
        RecordingCatalogRepository preparationRepository = new();
        CharacterCatalogService preparation = new(preparationRepository, new RecordingCharacterRepository(),
            PersistenceTestValues.Defaults(), clock);
        AccountContext account = (AccountContext)OfflineCatalog.AliceAccount();
        CreateDescriptor descriptor = Assert.IsType<CreateDescriptor>(
            preparation.PrepareCreate(account, 1, "Dawn01", CharacterClass.Knight).Descriptor);
        SqlCharacterCatalogRepository repository = new(
            new PersistenceOptions(2, "CatalogBoundaryProbe", "tcp:127.0.0.1,14330"), RefuseConnection);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));

        CatalogStorageResult result = await (operation switch
        {
            "List" => repository.ListAsync(account, budget, CancellationToken.None),
            "Create" => repository.CreateAsync(descriptor, OfflineCatalog.OperationA,
                PersistenceTestValues.Defaults(), budget, CancellationToken.None),
            "Resolve" => repository.ResolveCreateAsync(descriptor, OfflineCatalog.OperationA, budget, CancellationToken.None),
            "Select" => repository.ReadSelectionAsync(account, OfflineCatalog.CharacterA, budget, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        }).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(expected, result.Status);
        Assert.Equal(1, connectionRequests);
        Assert.Empty(preparationRepository.Calls);
        Assert.Null(result.Character);
        Assert.Empty(result.Characters);
    }

    [Theory]
    [InlineData("List", false, CatalogStatus.CancelledBeforeDispatch)]
    [InlineData("Create", false, CatalogStatus.CancelledBeforeDispatch)]
    [InlineData("Resolve", false, CatalogStatus.StillUnknown)]
    [InlineData("Select", false, CatalogStatus.CancelledBeforeDispatch)]
    [InlineData("List", true, CatalogStatus.CancelledBeforeDispatch)]
    [InlineData("Create", true, CatalogStatus.CancelledBeforeDispatch)]
    [InlineData("Resolve", true, CatalogStatus.StillUnknown)]
    [InlineData("Select", true, CatalogStatus.CancelledBeforeDispatch)]
    public async Task CancelledOrExpiredBeforeDispatch_DoesNotConstructAConnection(
        string operation, bool expired, CatalogStatus expected)
    {
        int connectionRequests = 0;
        SqlConnection RefuseConnection()
        {
            Interlocked.Increment(ref connectionRequests);
            throw new InvalidOperationException("The pre-dispatch gate must prevent this factory call.");
        }

        ControllableTimeProvider clock = new();
        CharacterCatalogService preparation = new(new RecordingCatalogRepository(), new RecordingCharacterRepository(),
            PersistenceTestValues.Defaults(), clock);
        AccountContext account = (AccountContext)OfflineCatalog.AliceAccount();
        CreateDescriptor descriptor = Assert.IsType<CreateDescriptor>(
            preparation.PrepareCreate(account, 1, "Dawn01", CharacterClass.Knight).Descriptor);
        SqlCharacterCatalogRepository repository = new(
            new PersistenceOptions(2, "CatalogBoundaryProbe", "tcp:127.0.0.1,14330"), RefuseConnection);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));
        using CancellationTokenSource cancellation = new();
        if (expired) clock.Advance(TimeSpan.FromSeconds(5));
        else cancellation.Cancel();

        CatalogStorageResult result = await (operation switch
        {
            "List" => repository.ListAsync(account, budget, cancellation.Token),
            "Create" => repository.CreateAsync(descriptor, OfflineCatalog.OperationA,
                PersistenceTestValues.Defaults(), budget, cancellation.Token),
            "Resolve" => repository.ResolveCreateAsync(descriptor, OfflineCatalog.OperationA, budget, cancellation.Token),
            "Select" => repository.ReadSelectionAsync(account, OfflineCatalog.CharacterA, budget, cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        }).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(expected, result.Status);
        Assert.Equal(0, connectionRequests);
        Assert.Null(result.Character);
        Assert.Empty(result.Characters);
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("data")]
    [InlineData("json")]
    [InlineData("format")]
    public async Task PreflightFactoryFailure_BlocksStartup(string failure)
    {
        int connectionRequests = 0;
        SqlConnection RefuseConnection()
        {
            Interlocked.Increment(ref connectionRequests);
            throw FactoryFailure(failure);
        }

        ControllableTimeProvider clock = new();
        SqlCharacterCatalogRepository repository = new(
            new PersistenceOptions(2, "CatalogBoundaryProbe", "tcp:127.0.0.1,14330"), RefuseConnection);
        PersistenceDeadline budget = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));

        bool ready = await repository.PreflightAsync(budget, CancellationToken.None)
            .WaitAsync(StoreCallRecorder.Limit);

        Assert.False(ready);
        Assert.Equal(1, connectionRequests);
    }

    static Exception FactoryFailure(string failure) => failure switch
    {
        "operation" => new InvalidOperationException("Factory refused before creating a connection."),
        "data" => new InvalidDataException("Factory failure is not a received catalog list."),
        "json" => new JsonException("Factory failure is not a received JSON response."),
        "format" => new FormatException("Factory failure is not a parsed character field."),
        _ => throw new ArgumentOutOfRangeException(nameof(failure)),
    };
}
