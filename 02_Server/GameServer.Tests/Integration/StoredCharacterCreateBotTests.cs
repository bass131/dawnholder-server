using System.Net;
using System.Text.RegularExpressions;
using Dawnholder.Server.GameServer.Hosting;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Dawnholder.Tools.HeadlessBot.Scenarios.Persistence;
using Shared.GameData;
using Shared.Protocol;
using Xunit.Abstractions;

namespace Dawnholder.Server.GameServer.Tests.Integration;

// Runs the Stored create bot scenario against in-process Stored hosts (loopback, port 0). Before and after
// use two hosts over one memory store, so a restart reads what the first host wrote, without SQL. Expected
// IDs, slots, names, classes and call counts are literals here, never values recomputed by the catalog.
[Collection("GameWorldRegistryTests")]
public sealed class StoredCharacterCreateBotTests
{
    const string Account = "alice_01";
    const string ExistingName = "Wexisting";
    const string NewName = "Wcreated";
    static readonly Guid AccountId = new("d0064115-982b-56a6-ae70-528b3cde4a09");
    static readonly Guid ExistingId = new("5c3d9a1e-7b2f-4c6d-8e0a-1f2b3c4d5e6f");
    readonly ITestOutputHelper _output;

    public StoredCharacterCreateBotTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task CreatedCharacter_KeepsItsIdAndClass_AcrossAHostRestartOverTheSameStore()
    {
        InMemoryCharacterStore store = new();
        store.Seed(AccountId, ExistingId, 2, ExistingName, "WEXISTING", CharacterClass.Mage);

        (PersistenceReconnectScenario.Result before, string beforeLog) = await RunPhaseAsync(store, "before", "");
        string[] printedIds = Regex.Matches(beforeLog, @"createdCharacterId=(\S+)")
            .Select(match => match.Groups[1].Value)
            .ToArray();
        IReadOnlyList<CharacterSummary> storedAfterBefore = store.Characters(AccountId);
        int heldAfterBefore = store.HeldCount;

        Assert.True(before.Success, before.Reason);
        string createdId = Assert.Single(printedIds);
        Assert.Equal(2, storedAfterBefore.Count);
        Assert.Equal(createdId, storedAfterBefore[0].CharacterId.ToString("D"));
        Assert.Equal((byte)1, storedAfterBefore[0].Slot);
        Assert.Equal(NewName, storedAfterBefore[0].Name);
        Assert.Equal(CharacterClass.Knight, storedAfterBefore[0].Class);
        Assert.Equal(new CharacterSummary(ExistingId, 2, ExistingName, CharacterClass.Mage), storedAfterBefore[1]);
        Assert.Equal(0, heldAfterBefore);

        (PersistenceReconnectScenario.Result after, string afterLog) = await RunPhaseAsync(store, "after", createdId);
        IReadOnlyList<CharacterSummary> storedAfterAfter = store.Characters(AccountId);

        Assert.True(after.Success, after.Reason);
        Assert.Contains($"phase=after session=A account={Account} characterId={ExistingId:D} " +
            "event=listed-character storedClass=Mage", afterLog);
        Assert.Contains($"event=listed-expected listedCharacterId={createdId} slot=1 name={NewName} class=Knight",
            afterLog);
        Assert.Contains($"phase=after session=B account={Account} characterId={createdId} " +
            "event=character-load packet=48 result=Success storedClass=Knight", afterLog);
        Assert.DoesNotContain("createdCharacterId=", afterLog);
        Assert.Equal(storedAfterBefore, storedAfterAfter);
        Assert.Equal(1, store.CreateCalls);
        Assert.Equal(4, store.AcquireCalls);
        Assert.Equal(0, store.HeldCount);
    }

    [Fact]
    public async Task FullAccount_StopsBeforeCreate_WithoutSelectingAnyCharacter()
    {
        InMemoryCharacterStore store = new();
        store.Seed(AccountId, new("0b7e5d3c-1a2f-4e6d-9c8b-7a6f5e4d3c2b"), 1, "Wfirst", "WFIRST",
            CharacterClass.Knight);
        store.Seed(AccountId, ExistingId, 2, ExistingName, "WEXISTING", CharacterClass.Mage);
        store.Seed(AccountId, new("e2d4c6b8-a0f1-4e3d-8c5b-9a7f1e3d5c7b"), 3, "Wthird", "WTHIRD",
            CharacterClass.Mage);

        (PersistenceReconnectScenario.Result result, string log) = await RunPhaseAsync(store, "before", "");

        Assert.False(result.Success);
        Assert.Equal("Session A: No empty slot is listed for the new character.", result.Reason);
        Assert.DoesNotContain("createdCharacterId=", log);
        Assert.Equal(0, store.CreateCalls);
        Assert.Equal(0, store.AcquireCalls);
        Assert.Equal(3, store.Characters(AccountId).Count);
    }

    // The other account's "WCREATED" shares the name key of the requested "Wcreated".
    [Fact]
    public async Task TakenName_StopsAtTheCreateRefusal_WithoutRetryOrSelection()
    {
        InMemoryCharacterStore store = new();
        Guid otherAccountId = new("8a4f2c1d-6e3b-4d5a-9c7e-0b1a2d3c4e5f");
        store.Seed(AccountId, ExistingId, 2, ExistingName, "WEXISTING", CharacterClass.Mage);
        store.Seed(otherAccountId, new("3e5f7a9b-1c2d-4e6f-8a0b-2c4d6e8f0a1b"), 1, "WCREATED", "WCREATED",
            CharacterClass.Mage);

        (PersistenceReconnectScenario.Result result, string log) = await RunPhaseAsync(store, "before", "");

        Assert.False(result.Success);
        Assert.Equal("Session A: Create request refused: NameTaken.", result.Reason);
        Assert.DoesNotContain("createdCharacterId=", log);
        Assert.Equal(1, store.CreateCalls);
        Assert.Equal(0, store.AcquireCalls);
        Assert.Equal(new[] { new CharacterSummary(ExistingId, 2, ExistingName, CharacterClass.Mage) },
            store.Characters(AccountId));
    }

    // One host lifetime per phase. Stop flushes every persistence close, so the store is settled on return.
    async Task<(PersistenceReconnectScenario.Result Result, string Log)> RunPhaseAsync(
        InMemoryCharacterStore store, string phase, string newCharacterIdD)
    {
        GameWorld world = new(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        var spawn = world.Map.PlayerSpawnPosition;
        SafeDefaults defaults = new(spawn.X, spawn.Y, PlayerStats.Knight().MaxHp, PlayerStats.Mage().MaxHp);
        CharacterCatalogService catalog = new(store, store, defaults, TimeProvider.System);
        world.AttachPersistence(new CharacterAdmissionServices(catalog, TimeProvider.System));
        using ServerHost host = new(world, new IPEndPoint(IPAddress.Loopback, 0));
        TextWriter original = Console.Out;
        StringWriter capture = new();
        PersistenceReconnectScenario.Result result;
        Console.SetOut(TextWriter.Synchronized(capture));
        try
        {
            host.Start();
            int port = Assert.IsType<IPEndPoint>(host.LocalEndPoint).Port;
            result = await PersistenceRestartScenario.RunStoredCreate("127.0.0.1", port, Account,
                ExistingId.ToString("D"), NewName, "Knight", phase, newCharacterIdD);
        }
        finally
        {
            host.Stop(TimeSpan.FromSeconds(10));
            Console.SetOut(original);
        }

        string log = capture.ToString();
        _output.WriteLine(log);
        return (result, log);
    }
}
