using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using Xunit.Sdk;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// The admission services seam (api-contract v2 「선행시험과 두 writer가 공유할 최소 타입 접점(v2)」): constructing
/// CharacterAdmissionServices opens nothing, and GameWorld.AttachPersistence injects it once, before Start. A refused
/// second attachment leaves the first services in effect rather than silently switching stores.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionServicesTests
{
    [Fact]
    public async Task CreatingAndAttachingServices_CallsNoStore()
    {
        using AdmissionTestWorld fixture = new();

        await fixture.TicksAsync(3);

        Assert.NotNull(fixture.Services);
        Assert.Empty(fixture.Repository.Calls);
    }

    [Fact]
    public async Task SecondAttach_IsRejected_AndTheFirstServicesStayInEffect()
    {
        using AdmissionTestWorld fixture = new();
        Repo secondStore = new();
        object second = RuntimeEntryApi.NewCatalogServices(fixture.Catalog, secondStore, fixture.Clock);

        Exception? refused = Record.Exception(() => RuntimeEntryApi.AttachPersistence(fixture.World, second));
        EntrySession session = await fixture.LogInAsync(PersistedName);
        fixture.SelectStored(session, CharacterClass.Knight);
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();

        Assert.False(refused is XunitException, refused?.Message);
        Assert.NotNull(refused);
        Assert.Equal(PersistedIdentity, probe.Request.Identity);
        Assert.Empty(secondStore.Calls);
    }

    [Fact]
    public void AttachAfterStart_IsRejected()
    {
        using AdmissionTestWorld fixture = new(attachPersistence: false);
        object services = RuntimeEntryApi.NewCatalogServices(fixture.Catalog, fixture.Repository, fixture.Clock);
        fixture.World.Start();

        Exception? refused = Record.Exception(() => RuntimeEntryApi.AttachPersistence(fixture.World, services));

        Assert.False(refused is XunitException, refused?.Message);
        Assert.NotNull(refused);
        Assert.Empty(fixture.Repository.Calls);
    }
}
