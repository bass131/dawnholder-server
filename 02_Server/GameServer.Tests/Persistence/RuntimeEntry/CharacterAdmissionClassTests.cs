using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 3 (runtime-entry pretest contract; api-contract v2 「패킷과 공개 결과」「서버 입장·owner 출력·종료」;
/// goal 「채택된 동작 기준」; technical-spec §5.1·§6): explicit Legacy makes no store call; a Stored host admits every
/// account through its owned catalog ID, with stored class authority and no class-only fallback. The current catalog
/// packet contract adds 54 before successful 48 → EnterMap → full HP. Refusals retain the no-entry requirement and
/// use their catalog meaning in 54; the unchanged six-byte 48 wire has its own packet tests.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionClassTests
{
    public static TheoryData<CharacterClass> MenuClasses => new() { CharacterClass.Knight, CharacterClass.Mage };

    public static TheoryData<CharacterClass, CharacterClass, int> OppositeStoredClasses => new()
    {
        { CharacterClass.Knight, CharacterClass.Mage, MageMaxHp },
        { CharacterClass.Mage, CharacterClass.Knight, KnightMaxHp },
    };

    // Current packet-contract-v1/Core catalog meanings. Refused ID selection sends 54,
    // with no successful 48 or memory entry; the old load-result wire is tested separately.
    public static TheoryData<string, byte> Refusals => new()
    {
        { "probe read failed", 11 },
        { "probe not dispatched", 11 },
        { "recovery owner holds the slot", 9 },
        { "acquire conflict", 15 },
        { "acquire rejected", 14 },
        { "acquire not dispatched", 10 },
        { "acquire unknown and unresolved", 12 },
        { "another runtime owner holds the slot", 8 },
        { "probe schema incompatible", 13 },
        { "acquired receipt is only historical", 12 },
    };

    [Theory]
    [MemberData(nameof(MenuClasses))]
    public async Task StoredOtherAccount_UsesItsCatalogClassWhileTheControlledStoreStaysFree(CharacterClass menuClass)
    {
        using AdmissionTestWorld fixture = new();

        EntrySession session = await fixture.EnterAsync(OtherName, menuClass);

        (byte result, byte loadedClass) = SingleLoadResult(session);
        bool classKnown = fixture.World.TryGetEntityClass(session.EntityId, out byte entityClass);
        Assert.Empty(fixture.Repository.Calls);
        Assert.Equal(1, fixture.Catalog.OtherSelections);
        Assert.Equal(Success, result);
        Assert.Equal((byte)menuClass, loadedClass);
        Assert.True(classKnown);
        Assert.Equal((byte)menuClass, entityClass);
        AssertEntryOrder(session);
        Assert.Equal("Active", RuntimeEntryApi.AdmissionState(session));
    }

    // The selector-bound services constructor stays supported (decision 28 A, Core msg_a4bad38aab8c 3b): its
    // non-persisted account makes no store call and enters with the menu class, without a 54 before 48.
    [Theory]
    [MemberData(nameof(MenuClasses))]
    public async Task NonPersistedAccount_EntersWithTheMenuClass_WithoutAnyStoreCall(CharacterClass menuClass)
    {
        using AdmissionTestWorld fixture = new(attachPersistence: false);
        object selector = RuntimeEntryApi.NewSelector(PersistedName, PersistedAccountId);
        RuntimeEntryApi.AttachPersistence(fixture.World,
            RuntimeEntryApi.NewServices(selector, PersistedIdentity, fixture.Repository, fixture.Clock));

        EntrySession session = await fixture.EnterAsync(OtherName, menuClass);

        (byte result, byte loadedClass) = SingleLoadResult(session);
        bool classKnown = fixture.World.TryGetEntityClass(session.EntityId, out byte entityClass);
        Assert.Empty(fixture.Repository.Calls);
        Assert.Equal(Success, result);
        Assert.Equal((byte)menuClass, loadedClass);
        Assert.True(classKnown);
        Assert.Equal((byte)menuClass, entityClass);
        AssertEntryOrder(session, stored: false);
        Assert.Equal("Active", RuntimeEntryApi.AdmissionState(session));
    }

    // Without AttachPersistence the existing memory path stays, and the client still gets the same one result packet.
    [Theory]
    [MemberData(nameof(MenuClasses))]
    public async Task WithoutAttachedPersistence_TheMenuEntryStays_AndSendsTheSameLoadResult(CharacterClass menuClass)
    {
        using AdmissionTestWorld fixture = new(attachPersistence: false);

        EntrySession session = await fixture.EnterAsync(OtherName, menuClass);

        bool classKnown = fixture.World.TryGetEntityClass(session.EntityId, out byte entityClass);
        Assert.True(classKnown);
        Assert.Equal((byte)menuClass, entityClass);
        (byte result, byte loadedClass) = SingleLoadResult(session);
        Assert.Equal(Success, result);
        Assert.Equal((byte)menuClass, loadedClass);
        AssertEntryOrder(session, stored: false);
    }

    [Theory]
    [MemberData(nameof(OppositeStoredClasses))]
    public async Task StoredClass_BeatsTheOppositeMenuIntent(CharacterClass menuClass, CharacterClass storedClass, int storedMaxHp)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        session.Receive(new C_CharacterSelect { characterClass = (byte)menuClass });
        await fixture.TickAsync();
        Assert.Equal((Unavailable, NoClass), SingleLoadResult(session));
        Assert.Empty(fixture.Repository.Calls);
        int acceptedSelection = session.Mark;
        var loading = await fixture.LoadingAsync(storedClass, session);
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire = loading.Acquire;
        long expectedFence = acquire.Request.Operation.ExpectedFence;
        CharacterClass intendedClass = acquire.Request.IntendedClass;

        acquire.Complete(Acquired(acquire.Request, storedClass, storedMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(session) is not null, "entry with the stored class");

        PlayerEntity player = fixture.PlayerOf(session)!;
        (byte result, byte loadedClass) = SingleLoadResult(session, acceptedSelection);
        Assert.Equal(ProbedFence, expectedFence);
        Assert.Equal(storedClass, intendedClass);
        Assert.Equal(storedClass, player.Stats.Class);
        Assert.Equal(storedMaxHp, player.MaxHp);
        Assert.Equal(player.MaxHp, player.Hp);
        Assert.Equal(Success, result);
        Assert.Equal((byte)storedClass, loadedClass);
        AssertEntryOrder(session, acceptedSelection);
        Assert.Equal("Active", RuntimeEntryApi.AdmissionState(session));
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);
    }

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task PersistedAccountFailure_RefusesEntry_WithoutMemoryFallback(string refusal, byte expectedStatus)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        fixture.SelectStored(session, CharacterClass.Knight);

        await AnswerAsRefusedAsync(fixture, refusal);
        await fixture.TickUntilAsync(() => FirstIndexOf(session, 54) >= 0, $"{refusal}: the select result");
        await fixture.TicksAsync(3);
        Assert.Equal(-1, FirstIndexOf(session, LoadResultId));
        string[] callsAtResult = fixture.Repository.Calls.ToArray();
        session.Receive(new C_CharacterSelect { characterClass = (byte)CharacterClass.Mage });
        await fixture.TicksAsync(3);

        (byte result, byte invalidField) = SingleSelectFailure(session);
        string state = RuntimeEntryApi.AdmissionState(session);
        Assert.Equal(expectedStatus, result);
        Assert.Equal((byte)0, invalidField);
        Assert.DoesNotContain(session.Sent(), packet => packet.Id == LoadResultId && packet.Bytes[4] == 0);
        Assert.Equal(-1, FirstIndexOf(session, (ushort)PacketID.S_EnterMap));
        Assert.Empty(fixture.Players);
        Assert.Equal(expectedStatus == 12 ? "Closed" : "Selecting", state);
        Assert.Equal(callsAtResult, fixture.Repository.Calls);
    }

    // Scripted store answers per refusal; fences, owners and tokens are literals, not owner calculations.
    static async Task AnswerAsRefusedAsync(AdmissionTestWorld fixture, string refusal)
    {
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();
        switch (refusal)
        {
            case "probe read failed":
                probe.Complete(ReadAdmissionResult.ReadFailed());
                return;
            case "probe not dispatched":
                probe.Complete(ReadAdmissionResult.NotDispatched());
                return;
            case "recovery owner holds the slot":
                probe.Complete(HeldProbe(PersistenceOwnerKind.Recovery, RecoveryOwnerId));
                return;
            case "another runtime owner holds the slot":
                probe.Complete(HeldProbe(PersistenceOwnerKind.Runtime, OtherOwnerId));
                return;
            case "probe schema incompatible":
                probe.Complete(ReadAdmissionResult.Probe(
                    new AdmissionProbe(FreeAuthority(ProbedFence), schemaCompatible: false, bindingMatches: true)));
                return;
        }

        probe.Complete(FreeProbe());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire = await fixture.NextAcquireAsync();
        switch (refusal)
        {
            case "acquire conflict":
                acquire.Complete(AcquireAndLoadResult.Conflict());
                return;
            case "acquire rejected":
                acquire.Complete(AcquireAndLoadResult.Rejected(PersistenceResultCode.IdentityMismatch));
                return;
            case "acquire not dispatched":
                acquire.Complete(AcquireAndLoadResult.NotDispatched());
                return;
            case "acquire unknown and unresolved":
                acquire.Complete(AcquireAndLoadResult.AcquireUnknown());
                foreach (ResolveMode mode in new[] { ResolveMode.QueryOnly, ResolveMode.SealIfAbsent })
                {
                    Repo.PendingCall<ResolveOperationRequest, ResolveOperationResult> resolve = await fixture.NextResolveAsync();
                    Assert.Equal(mode, resolve.Request.Mode);
                    Assert.Equal(acquire.Request.Operation, resolve.Request.Operation);
                    resolve.Complete(ResolveOperationResult.StillUnknown());
                }
                return;
            case "acquired receipt is only historical":
                acquire.Complete(HistoricalOnlyAcquired(acquire.Request));
                return;
        }
        throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "unknown refusal case");
    }

    // The receipt matches this request, but the store now shows another runtime owner at a later fence.
    static AcquireAndLoadResult HistoricalOnlyAcquired(AcquireAndLoadRequest request)
    {
        PersistenceAuthority held = HeldAuthority(PersistenceOwnerKind.Runtime, request.Operation.OwnerId, AcquiredFence);
        OperationReceipt receipt = new(
            request.Operation.OperationId, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired,
            held, CharacterClass.Mage, new SafeCheckpoint(CharacterClass.Mage, 3.5f, 0f, MageMaxHp), Token(1), Token(2));
        PersistenceObservation now = Observed(HeldAuthority(PersistenceOwnerKind.Runtime, OtherOwnerId, AcquiredFence + 1), Token(3), Token(4));
        return AcquireAndLoadResult.Acquired(receipt, now);
    }

    // api-contract v2 「패킷과 공개 결과」 Success row: LoadResult → EnterMap → full HP.
    static void AssertEntryOrder(EntrySession session, int fromMark = 0, bool stored = true)
    {
        int load = FirstIndexOf(session, LoadResultId, fromMark);
        int enter = FirstIndexOf(session, (ushort)PacketID.S_EnterMap, fromMark);
        int hp = FirstIndexOf(session, (ushort)PacketID.S_PlayerHp, fromMark);
        if (stored)
        {
            int select = FirstIndexOf(session, 54, fromMark);
            Assert.True(select >= 0 && select < load, "Stored ID success must precede the authoritative load result.");
        }
        Assert.True(load >= 0, "no S_CharacterLoadResult was sent");
        Assert.True(load < enter, $"S_CharacterLoadResult at {load} is not before S_EnterMap at {enter}");
        Assert.True(enter < hp, $"S_EnterMap at {enter} is not before S_PlayerHp at {hp}");
    }
}
