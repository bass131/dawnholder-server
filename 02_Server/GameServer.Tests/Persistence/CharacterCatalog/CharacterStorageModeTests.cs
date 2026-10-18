using System.Net;
using System.Reflection;
using Dawnholder.Server.GameServer.Hosting;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>Fixed startup configuration and rejected Stored startup; never opens SQL or starts a game session.</summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterStorageModeTests
{
    [Fact]
    public async Task NoProfile_SelectsLegacy()
    {
        Assert.Null(await Startup([]));
        GameWorld world = NewWorld();
        try
        {
            ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
            Assert.Equal(CharacterStorageMode.Legacy, host.StorageMode);
            Assert.Null(host.CharacterCatalog);
            Assert.Null(typeof(ServerHost).GetProperty(nameof(ServerHost.StorageMode))!.SetMethod);
        }
        finally { world.Stop(); }
    }

    [Fact]
    public void Stored_BindsTheSameCatalogAndRefusesReplacement()
    {
        using OfflineCatalog offline = new();
        CharacterAdmissionServices services = new(offline.Catalog.Raw, offline.Time);
        GameWorld world = NewWorld();
        try
        {
            world.AttachPersistence(services);
            ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
            Assert.Equal(CharacterStorageMode.Stored, host.StorageMode);
            Assert.Same(offline.Catalog.Raw, host.CharacterCatalog);
            Assert.Same(offline.Catalog.Raw, services.Catalog);
            Assert.Throws<InvalidOperationException>(() => world.AttachPersistence(services));
            Assert.Equal(CharacterStorageMode.Stored, host.StorageMode);
            Assert.Empty(offline.Store.Calls);
        }
        finally { world.Stop(); }
    }

    [Fact]
    public void StoredWithoutCatalog_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new CharacterAdmissionServices(null!, TimeProvider.System));
    }

    [Fact]
    public void StoredMissingCatalog_CannotStartThroughTheOldFixedBinding()
    {
        using OfflineCatalog offline = new();
        CharacterAdmissionServices oldBinding = new(
            new PersistenceAccountSelector("alice_01", OfflineCatalog.Alice01),
            new PersistenceIdentity(1, OfflineCatalog.Alice01, OfflineCatalog.CharacterA),
            new RecordingCharacterRepository(), offline.Time);
        GameWorld world = NewWorld();
        world.AttachPersistence(oldBinding);
        ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        try
        {
            Assert.Throws<InvalidOperationException>(() => host.Start());
            Assert.Null(host.LocalEndPoint);
        }
        finally { host.Stop(); }
    }

    [Fact]
    public async Task StoredProfileFailure_ThrowsAndCannotReturnLegacy()
    {
        // The empty path is rejected before any file or credential access. Failure must not return the Legacy null.
        await Assert.ThrowsAsync<ArgumentException>(() => Startup(["--persistence-profile", ""]));
    }

    [Fact]
    public async Task StoredClassOnlyEntry_CannotFallBackToLegacy()
    {
        using OfflineCatalog offline = new();
        CharacterAdmissionServices services = new(offline.Catalog.Raw, offline.Time);
        MethodInfo create = typeof(CharacterAdmissionServices).GetMethod("CreateLifetime",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        object lifetime = create.Invoke(services, [Shared.Protocol.CharacterClass.Knight, PersistenceTestValues.Defaults()])!;
        PropertyInfo completion = lifetime.GetType().GetProperty("Completion", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task task = (Task)completion.GetValue(lifetime)!;
        await task.WaitAsync(StoreCallRecorder.Limit);
        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.Equal("Unavailable", result.GetType().GetProperty("Status")!.GetValue(result)!.ToString());
        Assert.Empty(offline.Store.Calls);
    }

    static GameWorld NewWorld() => new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());

    // Startup is internal, so this single fixed binding tests the real configuration entry rather than duplicating it.
    static async Task<object?> Startup(string[] args)
    {
        Type type = typeof(ServerHost).Assembly.GetType(
            "Dawnholder.Server.GameServer.Hosting.PersistenceRuntime.PersistenceRuntimeStartup", throwOnError: true)!;
        MethodInfo create = type.GetMethod("CreateAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task task = (Task)create.Invoke(null, [args, CancellationToken.None, PersistenceTestValues.Defaults()])!;
        await task.WaitAsync(StoreCallRecorder.Limit);
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }
}
