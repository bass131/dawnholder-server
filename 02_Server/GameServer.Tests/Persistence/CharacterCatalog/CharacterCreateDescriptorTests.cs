using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>API v2 preparation owns canonical intent; World only retains and forwards the returned object.</summary>
public sealed class CharacterCreateDescriptorTests
{
    [Fact]
    public void Descriptor_HasNoPublicConstructionMutationOrCorrelation()
    {
        Type type = typeof(CreateDescriptor);
        Assert.Empty(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(type.GetProperties().Where(p => p.SetMethod?.IsPublic == true));
        Assert.Empty(type.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(f => !f.IsInitOnly));
        Assert.Equal(new[] { "Account", "Class", "Name", "NameKey", "PayloadVersion", "Slot" },
            type.GetProperties().Select(p => p.Name).Order());
    }

    [Theory]
    [InlineData("가나aZ1", "가나aZ1", "가나AZ1")]
    [InlineData("Kab", "Kab", "KAB")]
    public void Preparation_ReturnsLiteralCanonicalValuesWithoutStorage(string raw, string display, string key)
    {
        using OfflineCatalog offline = new();
        CreatePreparationResult prepared = offline.Catalog.Prepare(offline.ValidCall() with { Name = raw });

        Assert.Equal(CatalogStatus.Prepared, prepared.Status);
        CreateDescriptor descriptor = Assert.IsType<CreateDescriptor>(prepared.Descriptor);
        Assert.Equal(OfflineCatalog.AliceAccount(), descriptor.Account);
        Assert.Equal((byte)1, descriptor.Slot);
        Assert.Equal(display, descriptor.Name);
        Assert.Equal(key, descriptor.NameKey);
        Assert.Equal(CharacterClass.Mage, descriptor.Class);
        Assert.True(descriptor.PayloadVersion > 0);
        Assert.Empty(offline.Store.Calls);
    }

    [Fact]
    public async Task PreparedObject_IsPassedUnchangedToCreateAndResolve()
    {
        RecordingCatalogRepository repository = new();
        RecordingCharacterRepository characters = new();
        ControllableTimeProvider clock = new();
        CharacterCatalogService service = new(repository, characters, PersistenceTestValues.Defaults(), clock);
        CreateDescriptor descriptor = service.PrepareCreate((AccountContext)OfflineCatalog.AliceAccount(),
            1, "가나aZ1", CharacterClass.Mage).Descriptor!;
        RequestContext request = new(OfflineCatalog.SessionA, 7);
        PersistenceDeadline deadline = PersistenceDeadline.FromBudget(clock, TimeSpan.FromSeconds(5));

        Task<CatalogResult> creating = service.CreateCharacterAsync(descriptor, OfflineCatalog.OperationA,
            request, deadline, CancellationToken.None);
        RecordingCatalogRepository.Call create = await repository.Next("Create");
        Task<CatalogResult> resolving = service.ResolveCreateAsync(descriptor, OfflineCatalog.OperationA,
            request with { Generation = 8 }, deadline, CancellationToken.None);
        RecordingCatalogRepository.Call resolve = await repository.Next("Resolve");
        Assert.Same(descriptor, create.Descriptor);
        Assert.Same(descriptor, resolve.Descriptor);
        Assert.Equal(OfflineCatalog.OperationA, resolve.OperationId);
        create.Complete(new(CatalogStatus.NameTaken));
        resolve.Complete(new(CatalogStatus.NotApplied, rejection: CatalogStatus.NameTaken));

        Assert.Equal(CatalogStatus.NameTaken, (await creating.WaitAsync(StoreCallRecorder.Limit)).Status);
        CatalogResult result = await resolving.WaitAsync(StoreCallRecorder.Limit);
        Assert.Equal(CatalogStatus.NotApplied, result.Status);
        Assert.Equal(CatalogStatus.NameTaken, result.Rejection);
        Assert.Equal(8, result.Request.Generation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullDescriptor_IsRejectedBeforeStorage(bool resolve)
    {
        using OfflineCatalog offline = new();
        RequestContext request = new(OfflineCatalog.SessionA, 7);
        CharacterCatalogService service = offline.Catalog.Raw;
        CatalogResult result = await (resolve
            ? service.ResolveCreateAsync(null!, OfflineCatalog.OperationA, request, offline.Deadline(), CancellationToken.None)
            : service.CreateCharacterAsync(null!, OfflineCatalog.OperationA, request, offline.Deadline(), CancellationToken.None));

        Assert.Equal(CatalogStatus.InvalidInput, result.Status);
        Assert.Equal(request, result.Request);
        Assert.Empty(offline.Store.Calls);
    }

    [Fact]
    public void Preparation_DoesNotDependOnSessionDeadlineOrOperation()
    {
        using OfflineCatalog offline = new();
        CatalogCall first = offline.ValidCall();
        CreateDescriptor original = offline.Catalog.Prepare(first).Descriptor!;
        CreateDescriptor next = offline.Catalog.Prepare(first with
        {
            Request = OfflineCatalog.Request(OfflineCatalog.SessionB, 999),
            OperationId = OfflineCatalog.OperationB,
            Deadline = PersistenceDeadline.FromBudget(offline.Time, TimeSpan.Zero),
        }).Descriptor!;

        Assert.Equal(original, next);
        Assert.Empty(offline.Store.Calls);
    }
}
