using System.Collections.Concurrent;
using System.Diagnostics;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Admission;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Network.CharacterMenuRequestWire;

namespace Dawnholder.Server.GameServer.Tests.Network;

// Exercise the real session/handlers, Core facade and World tick. Only the two repository
// boundaries are scripted; packet codes, registration counts and authority expectations are literals.
[Collection("GameWorldRegistryTests")]
public sealed class CharacterMenuCatalogSessionTests
{
    static readonly CharacterSummary Mage = new(new Guid("52000000-0000-4000-8000-000000000001"),
        2, "저장마법사", CharacterClass.Mage);
    static readonly Guid OtherCharacter = new("52000000-0000-4000-8000-000000000005");

    [Fact]
    public async Task EmptyListAndCreate_UseServerAccountAndDoNotEnter()
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 1 }.Write());
        await fixture.PumpUntilAsync(() => session.Packets.Any(packet => Id(packet) == 50));
        Assert.Equal((byte)0, Assert.Single(session.Packets)[8]);
        Assert.Equal((byte)0, Assert.Single(session.Packets)[10]);
        Assert.Equal(session.Account!.Value.AccountId, Assert.Single(fixture.Catalog.ListAccounts).AccountId);
        Assert.Equal("stored_menu", Assert.Single(fixture.Catalog.ListAccounts).NormalizedAccountName);

        fixture.Catalog.CreateResult = new(CatalogStatus.Created, Mage);
        session.Clear();
        session.OnRecvPacket(new C_CharacterCreateRequest
        {
            requestId = 2, slot = 2, classId = 1, rawName = "저장마법사",
        }.Write());
        await fixture.PumpUntilAsync(() => session.Packets.Any(packet => Id(packet) == 52));
        S_CharacterCreateResult created = new();
        created.Read(new ArraySegment<byte>(Assert.Single(session.Packets)));
        Assert.Equal((byte)0, created.result);
        Assert.Equal(Mage.CharacterId.ToString("D"), created.characterIdD);
        Assert.Equal((byte)2, created.slot);
        Assert.Equal((byte)1, created.classId);
        Assert.Equal(session.Account!.Value.AccountId, Assert.Single(fixture.Catalog.Creates).Descriptor.Account.AccountId);
        fixture.AssertNoEntry(session);
    }

    [Theory]
    [InlineData(1, (byte)2)]
    [InlineData(3, (byte)7)]
    public async Task List_UsesPersistedSlotsAndClassZeroWithoutEntering(int count, byte mask)
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterSummary knight = new(new Guid("52000000-0000-4000-8000-000000000003"),
            1, "저장기사", CharacterClass.Knight);
        fixture.Catalog.ListResult = new(CatalogStatus.Listed, characters: count == 1
            ? new[] { Mage }
            : new[] { Mage with { CharacterId = new Guid("52000000-0000-4000-8000-000000000004"), Slot = 3 }, Mage, knight });
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 1 }.Write());
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);
        S_CharacterListResult listed = new();
        listed.Read(new ArraySegment<byte>(Assert.Single(session.Packets)));
        Assert.Equal((byte)0, listed.result);
        Assert.Equal(mask, listed.occupiedMask);
        Assert.Equal(Mage.CharacterId.ToString("D"), listed.slot2CharacterIdD);
        Assert.Equal((byte)1, listed.slot2ClassId);
        Assert.Equal(count == 1 ? string.Empty : knight.CharacterId.ToString("D"), listed.slot1CharacterIdD);
        Assert.Equal((byte)0, listed.slot1ClassId);
        fixture.AssertNoEntry(session);
    }

    [Theory]
    [InlineData((byte)0, (byte)1, "정상이름", (byte)1)]
    [InlineData((byte)1, (byte)2, "정상이름", (byte)3)]
    [InlineData((byte)1, (byte)1, "!", (byte)2)]
    public async Task InvalidCreate_UsesCorePreparationWithoutStorageDispatch(byte slot, byte classId, string name, byte field)
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterCreateRequest
        {
            requestId = 1, slot = slot, classId = classId, rawName = name,
        }.Write());
        fixture.Tick();
        byte[] reply = Assert.Single(session.Packets);
        Assert.Equal((ushort)52, Id(reply));
        Assert.Equal((byte)1, reply[8]);
        Assert.Equal(field, reply[9]);
        Assert.Empty(fixture.Catalog.Creates);
        fixture.AssertNoEntry(session);
    }

    [Fact]
    public async Task StoredSelection_Sends54ThenStored48ThenEntryAndOneAccountSeedAcrossCharacters()
    {
        using CatalogMenuTestWorld fixture = new();
        CharacterMenuRequestSession first = fixture.LoggedIn();
        fixture.Catalog.Selected = Mage;
        first.OnRecvPacket(new C_CharacterSelect { characterClass = 0 }.Write());
        fixture.Tick();
        Assert.Equal((byte)3, Assert.Single(first.Packets)[4]);
        fixture.AssertNoEntry(first);
        first.Clear();
        first.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => first.Packets.Any(packet => Id(packet) == 44));
        AssertAdmission(first, Mage.CharacterId);
        Assert.Equal(CharacterClass.Mage, fixture.Storage.AcquiredClass);
        S_GuildSnapshot firstGuild = new();
        firstGuild.Read(new ArraySegment<byte>(first.Packets.Single(packet => Id(packet) == 44)));
        Assert.Equal(5, firstGuild.treasury);
        Assert.Equal(1, firstGuild.memberCount);

        first.Disconnect();
        fixture.Tick();
        await fixture.PumpUntilAsync(() => fixture.Storage.ReleaseCount == 1);
        CharacterMenuRequestSession second = fixture.LoggedIn();
        Guid otherCharacter = new("52000000-0000-4000-8000-000000000002");
        fixture.Catalog.Selected = Mage with { CharacterId = otherCharacter, Slot = 3, Name = "다른마법사" };
        second.OnRecvPacket(Select(1, otherCharacter));
        await fixture.PumpUntilAsync(() => second.Packets.Any(packet => Id(packet) == 44));
        AssertAdmission(second, otherCharacter);
        S_GuildSnapshot secondGuild = new();
        secondGuild.Read(new ArraySegment<byte>(second.Packets.Single(packet => Id(packet) == 44)));
        Assert.Equal(5, secondGuild.treasury);
        Assert.Equal(1, secondGuild.memberCount);
        second.Disconnect();
        fixture.Tick();
        await fixture.PumpUntilAsync(() => fixture.Storage.ReleaseCount == 2);
    }

    [Fact]
    public async Task PendingAndDuplicateIds_DoNotDispatchOrReplaceOriginalRequest()
    {
        using CatalogMenuTestWorld fixture = new();
        TaskCompletionSource<CatalogStorageResult> read = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Catalog.ListPending = read.Task;
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 10 }.Write());
        fixture.Tick();
        await fixture.PumpUntilAsync(() => fixture.Catalog.ListAccounts.Count == 1);
        session.OnRecvPacket(new C_CharacterCreateRequest
        {
            requestId = 11, slot = 1, classId = 0, rawName = "새캐릭터",
        }.Write());
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 10 }.Write());
        fixture.Tick();
        Assert.Equal(Failure(51, 11, 2), Assert.Single(session.Packets));
        Assert.Empty(fixture.Catalog.Creates);
        Assert.Single(fixture.Catalog.ListAccounts);
        read.SetResult(new(CatalogStatus.Listed));
        await fixture.PumpUntilAsync(() => session.Packets.Count == 2);
        Assert.Equal((ushort)50, Id(session.Packets[1]));
        Assert.Equal((byte)0, session.Packets[1][8]);
        session.Clear();
        session.OnRecvPacket(new C_CharacterListRequest { requestId = 11 }.Write());
        fixture.Tick();
        Assert.Empty(session.Packets);
        fixture.AssertNoEntry(session);
    }

    [Theory]
    [InlineData(CatalogStatus.CharacterNotFound)]
    [InlineData(CatalogStatus.NotOwned)]
    public async Task SelectionRefusal_HasOnePublicMeaningAndNoAdmission(CatalogStatus refusal)
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.SelectionResult = new(refusal);
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);
        Assert.Equal(Failure(53, 1, 7), Assert.Single(session.Packets));
        Assert.Equal(0, fixture.Storage.ProbeCount);
        fixture.AssertNoEntry(session);
    }

    [Fact]
    public async Task CloseBeforeSelectionReturns_LateSelectedCannotRegisterAnything()
    {
        using CatalogMenuTestWorld fixture = new();
        TaskCompletionSource<CatalogStorageResult> selection = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Catalog.SelectionPending = selection.Task;
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => fixture.Catalog.SelectionCount == 1);
        session.Disconnect();
        fixture.Tick();
        selection.SetResult(new(CatalogStatus.Selected, Mage));
        await fixture.CatalogService.FlushAsync(PersistenceDeadline.FromBudget(TimeProvider.System, TimeSpan.FromSeconds(2)));
        fixture.Tick();
        Assert.Empty(session.Packets);
        Assert.Equal(0, fixture.Storage.AcquireCount);
        fixture.AssertNoEntry(session);
    }

    [Fact]
    public async Task UnknownCreate_ReconcilesSameIntentAndIdWithinOriginalBudgetThenCloses()
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.CreateResult = new(CatalogStatus.OutcomeUnknown);
        fixture.Catalog.ResolveResult = new(CatalogStatus.StillUnknown);
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterCreateRequest
        {
            requestId = 1, slot = 1, classId = 1, rawName = "미확정이름",
        }.Write());
        await fixture.PumpUntilAsync(() => session.IsClosing);
        Assert.Equal(Failure(51, 1, 12), Assert.Single(session.Packets));
        var create = Assert.Single(fixture.Catalog.Creates);
        Assert.NotEmpty(fixture.Catalog.Resolutions);
        Assert.All(fixture.Catalog.Resolutions, resolution =>
        {
            Assert.Same(create.Descriptor, resolution.Descriptor);
            Assert.Equal(create.OperationId, resolution.OperationId);
        });
        // Host recovery has its own deadline; the one caller reconciliation retains the original.
        Assert.Single(fixture.Catalog.Resolutions, resolution => ReferenceEquals(create.Deadline, resolution.Deadline));
        fixture.AssertNoEntry(session);
    }

    [Theory]
    [InlineData(CatalogStatus.Applied, (byte)0)]
    [InlineData(CatalogStatus.NotApplied, (byte)6)]
    public async Task CreateReconciliation_UsesOriginalSummaryOrRejection(CatalogStatus resolution, byte expected)
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.CreateResult = new(CatalogStatus.OutcomeUnknown);
        fixture.Catalog.ResolveResult = resolution == CatalogStatus.Applied
            ? new(CatalogStatus.Applied, Mage)
            : new(CatalogStatus.NotApplied, rejection: CatalogStatus.NameTaken);
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(new C_CharacterCreateRequest
        {
            requestId = 1, slot = 2, classId = 1, rawName = "저장마법사",
        }.Write());
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);
        S_CharacterCreateResult reply = new();
        reply.Read(new ArraySegment<byte>(Assert.Single(session.Packets)));
        Assert.Equal(expected, reply.result);
        Assert.Equal(resolution == CatalogStatus.Applied ? Mage.CharacterId.ToString("D") : string.Empty, reply.characterIdD);
        Assert.False(session.IsClosing);
        Assert.Single(fixture.Catalog.Creates);
        fixture.AssertNoEntry(session);
    }

    [Fact]
    public async Task Failed54Send_ClosesAcquiredOwnerWithout48OrRegistration()
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.Selected = Mage;
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.FailSendFor = 54;
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => session.IsClosing && fixture.Storage.ReleaseCount == 1);
        Assert.Empty(session.Packets);
        fixture.AssertNoEntry(session);
    }

    // goal 「다음 검증의 경계」 (D-CAT-READ-01/SCHEMA-02): the real Core probe guard, not the mapper alone,
    // decides 11/13/14 before any acquire. packet-contract-v1 §모드·선택·입장·종료: a refusal ends with 54 only.
    [Theory]
    [InlineData("typed read failure", (byte)11)]
    [InlineData("probe not dispatched", (byte)11)]
    [InlineData("valid probe, schema incompatible", (byte)13)]
    [InlineData("no probe structure", (byte)14)]
    [InlineData("binding mismatch", (byte)14)]
    [InlineData("authority for another character", (byte)14)]
    public async Task ProbeGuard_RefusesWithCoreMeaningBeforeAcquireOrEntry(string probe, byte expected)
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.Selected = Mage;
        fixture.Storage.Probe = request => Task.FromResult(ProbeAnswer(probe, request));
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);
        Assert.Equal(Failure(53, 1, expected), Assert.Single(session.Packets));
        Assert.Equal(1, fixture.Storage.ProbeCount);
        Assert.Equal(0, fixture.Storage.AcquireCount);
        Assert.False(session.IsClosing);
        fixture.AssertNoEntry(session);
    }

    // An identity violation already in the catalog answer stops before the store probe.
    [Fact]
    public async Task SelectedSummaryForAnotherCharacter_IsIntegrityFailureBeforeProbe()
    {
        using CatalogMenuTestWorld fixture = new();
        fixture.Catalog.Selected = Mage with { CharacterId = OtherCharacter };
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => session.Packets.Count == 1);
        Assert.Equal(Failure(53, 1, 14), Assert.Single(session.Packets));
        Assert.Equal(0, fixture.Storage.ProbeCount);
        Assert.Equal(0, fixture.Storage.AcquireCount);
        fixture.AssertNoEntry(session);
    }

    // packet-contract-v1 §모드·선택·입장·종료: a pending close invalidates the request; even a late Free
    // probe for the right identity must not start an acquire or register anything.
    [Fact]
    public async Task CloseWhileProbePending_LateFreeProbeCannotAcquireOrRegister()
    {
        using CatalogMenuTestWorld fixture = new();
        TaskCompletionSource late = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Catalog.Selected = Mage;
        fixture.Storage.Probe = async request =>
        {
            await late.Task;
            return CatalogMenuTestStorage.FreeProbe(request.Identity, schemaCompatible: true, bindingMatches: true);
        };
        CharacterMenuRequestSession session = fixture.LoggedIn();
        session.OnRecvPacket(Select(1, Mage.CharacterId));
        await fixture.PumpUntilAsync(() => fixture.Storage.ProbeCount == 1);
        session.Disconnect();
        fixture.Tick();
        late.SetResult();
        await fixture.CatalogService.FlushAsync(PersistenceDeadline.FromBudget(TimeProvider.System, TimeSpan.FromSeconds(2)));
        fixture.Tick();
        Assert.Empty(session.Packets);
        Assert.Equal(0, fixture.Storage.AcquireCount);
        fixture.AssertNoEntry(session);
    }

    // Scripted store facts per case. A Probe result must carry an admission, so the only
    // structural violation a store can still produce is a missing result.
    static ReadAdmissionResult ProbeAnswer(string probe, ReadAdmissionRequest request) => probe switch
    {
        "typed read failure" => ReadAdmissionResult.ReadFailed(),
        "probe not dispatched" => ReadAdmissionResult.NotDispatched(),
        "valid probe, schema incompatible" =>
            CatalogMenuTestStorage.FreeProbe(request.Identity, schemaCompatible: false, bindingMatches: true),
        "no probe structure" => null!,
        "binding mismatch" =>
            CatalogMenuTestStorage.FreeProbe(request.Identity, schemaCompatible: true, bindingMatches: false),
        "authority for another character" => CatalogMenuTestStorage.FreeProbe(
            new PersistenceIdentity(request.Identity.SlotId, request.Identity.AccountId, OtherCharacter),
            schemaCompatible: true, bindingMatches: true),
        _ => throw new ArgumentOutOfRangeException(nameof(probe), probe, "unknown probe case"),
    };

    static ArraySegment<byte> Select(uint requestId, Guid characterId)
        => new C_CharacterSelectRequest { requestId = requestId, characterIdD = characterId.ToString("D") }.Write();

    static void AssertAdmission(CharacterMenuRequestSession session, Guid characterId)
    {
        ushort[] ids = session.Packets.Select(Id).ToArray();
        Assert.Equal(new ushort[] { 54, 48, 3 }, ids.Take(3));
        Assert.True(Array.IndexOf(ids, (ushort)44) > 2);
        S_CharacterSelectResult selected = new();
        selected.Read(new ArraySegment<byte>(session.Packets[0]));
        Assert.Equal((byte)0, selected.result);
        Assert.Equal(characterId.ToString("D"), selected.selectedCharacterIdD);
        Assert.Equal(new byte[] { 6, 0, 48, 0, 0, 1 }, session.Packets[1]);
        Assert.True(session.Selected);
        Assert.True(session.EntityId > 0);
    }
}

internal sealed class CatalogMenuTestWorld : IDisposable
{
    readonly LoginTestWorld _login = new();

    internal CatalogMenuTestWorld()
    {
        var spawn = _login.World.Map.PlayerSpawnPosition;
        CatalogService = new(Catalog, Storage, new SafeDefaults(spawn.X, spawn.Y, 150, 80), TimeProvider.System);
        _login.World.AttachPersistence(new CharacterAdmissionServices(CatalogService, TimeProvider.System));
    }

    internal CatalogMenuTestRepository Catalog { get; } = new();
    internal CatalogMenuTestStorage Storage { get; } = new();
    internal CharacterCatalogService CatalogService { get; }

    public void Dispose() => _login.Dispose();

    internal CharacterMenuRequestSession LoggedIn()
    {
        CharacterMenuRequestSession session = new(_login.World);
        session.OnRecvPacket(new C_Handshake { clientVersion = 22 }.Write());
        session.OnRecvPacket(new C_Login { accountName = "stored_menu" }.Write());
        Tick();
        Assert.True(session.IsLoggedIn);
        session.Clear();
        return session;
    }

    internal void Tick() => _login.Tick();

    internal async Task PumpUntilAsync(Func<bool> reached)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (!reached() && elapsed.Elapsed < TimeSpan.FromSeconds(3))
        {
            Tick();
            await Task.Delay(2);
        }
        Assert.True(reached(), "The actual session/Core/World path did not reach the expected observation.");
    }

    internal void AssertNoEntry(CharacterMenuRequestSession session)
    {
        Assert.Equal(-1, session.EntityId);
        Assert.False(session.Selected);
        Assert.DoesNotContain(_login.Players, player => ReferenceEquals(player.Owner, session));
        Assert.Empty(CharacterMenuRequestWorld.Registrations(_login.World.Inventory, "_registeredSessions"));
        Assert.Empty(CharacterMenuRequestWorld.Registrations(_login.World.Guild, "_members"));
        Assert.DoesNotContain(session.Packets, packet => Id(packet) is 3 or 44);
    }
}

internal sealed class CatalogMenuTestRepository : ICharacterCatalogRepository
{
    internal int SelectionCount;

    internal ConcurrentQueue<AccountContext> ListAccounts { get; } = new();
    internal ConcurrentQueue<(CreateDescriptor Descriptor, Guid OperationId, PersistenceDeadline Deadline)> Creates { get; } = new();
    internal ConcurrentQueue<(CreateDescriptor Descriptor, Guid OperationId, PersistenceDeadline Deadline)> Resolutions { get; } = new();
    internal Task<CatalogStorageResult>? ListPending { get; set; }
    internal Task<CatalogStorageResult>? SelectionPending { get; set; }
    internal CatalogStorageResult CreateResult { get; set; } = new(CatalogStatus.Unavailable);
    internal CatalogStorageResult ResolveResult { get; set; } = new(CatalogStatus.StillUnknown);
    internal CatalogStorageResult SelectionResult { get; set; } = new(CatalogStatus.CharacterNotFound);
    internal CatalogStorageResult ListResult { get; set; } = new(CatalogStatus.Listed);
    internal CharacterSummary? Selected { get; set; }

    public Task<CatalogStorageResult> ListAsync(AccountContext account, PersistenceDeadline deadline, CancellationToken token)
    {
        ListAccounts.Enqueue(account);
        return ListPending ?? Task.FromResult(ListResult);
    }

    public Task<CatalogStorageResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        SafeDefaults defaults, PersistenceDeadline deadline, CancellationToken token)
    {
        Creates.Enqueue((descriptor, operationId, deadline));
        return Task.FromResult(CreateResult);
    }

    public Task<CatalogStorageResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        PersistenceDeadline deadline, CancellationToken token)
    {
        Resolutions.Enqueue((descriptor, operationId, deadline));
        return Task.FromResult(ResolveResult);
    }

    public Task<CatalogStorageResult> ReadSelectionAsync(AccountContext account, Guid characterId,
        PersistenceDeadline deadline, CancellationToken token)
    {
        Interlocked.Increment(ref SelectionCount);
        return SelectionPending ?? Task.FromResult(Selected is null ? SelectionResult : new(CatalogStatus.Selected, Selected));
    }
}

// Literal repository facts. Opaque identity/operation/owner values are echoed for correlation;
// the expected wire order, class, seed, fences and tokens do not come from product calculations.
internal sealed class CatalogMenuTestStorage : ICharacterRepository
{
    static readonly PersistenceToken Token = new(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });
    internal int ProbeCount;
    internal int AcquireCount;
    internal int ReleaseCount;
    internal CharacterClass? AcquiredClass;

    // Null keeps the store's default answer: Free at fence 7, schema-compatible and bound.
    internal Func<ReadAdmissionRequest, Task<ReadAdmissionResult>>? Probe { get; set; }

    public Task<ReadAdmissionResult> ReadAdmissionAsync(ReadAdmissionRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref ProbeCount);
        return Probe?.Invoke(request)
            ?? Task.FromResult(FreeProbe(request.Identity, schemaCompatible: true, bindingMatches: true));
    }

    internal static ReadAdmissionResult FreeProbe(PersistenceIdentity identity, bool schemaCompatible, bool bindingMatches)
        => ReadAdmissionResult.Probe(new AdmissionProbe(
            new PersistenceAuthority(identity, PersistenceOwnerKind.Free, null, 7, 0), schemaCompatible, bindingMatches));

    public Task<AcquireAndLoadResult> AcquireAndLoadAsync(AcquireAndLoadRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref AcquireCount);
        AcquiredClass = request.IntendedClass;
        PersistenceAuthority held = new(request.Operation.Identity, PersistenceOwnerKind.Runtime, request.Operation.OwnerId, 8, 0);
        OperationReceipt receipt = new(request.Operation.OperationId, PersistenceOperationKind.Acquire,
            PersistenceResultCode.Acquired, held, CharacterClass.Mage, new SafeCheckpoint(CharacterClass.Mage, 0, 0, 80), Token, Token);
        return Task.FromResult(AcquireAndLoadResult.Acquired(receipt, new PersistenceObservation(held, Token, Token)));
    }

    public Task<WriteSafeCheckpointResult> WriteSafeCheckpointAsync(WriteSafeCheckpointRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        PersistenceAuthority held = new(request.Operation.Identity, PersistenceOwnerKind.Runtime, request.Operation.OwnerId, 8, 1);
        OperationReceipt receipt = new(request.Operation.OperationId, PersistenceOperationKind.Checkpoint,
            PersistenceResultCode.CheckpointApplied, held, CharacterClass.Mage, request.Safe, Token, Token);
        return Task.FromResult(WriteSafeCheckpointResult.Durable(receipt, new PersistenceObservation(held, Token, Token)));
    }

    public Task<ReleaseResult> ReleaseAsync(ReleaseRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref ReleaseCount);
        PersistenceAuthority free = new(request.Operation.Identity, PersistenceOwnerKind.Free, null, 9, 0);
        OperationReceipt receipt = new(request.Operation.OperationId, PersistenceOperationKind.ReleaseRuntime,
            PersistenceResultCode.Released, free, null, null, null, null);
        return Task.FromResult(ReleaseResult.Released(receipt, new PersistenceObservation(free, Token, Token)));
    }

    public Task<ResolveOperationResult> ResolveOperationAsync(ResolveOperationRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Task.FromResult(ResolveOperationResult.StillUnknown());
}
