using Dawnholder.Server.GameServer.Entities;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 3 (runtime-entry pretest contract; api-contract v2 「패킷과 공개 결과」「서버 입장·owner 출력·종료」;
/// goal 「채택된 동작 기준」; technical-spec §5.1·§6): a non-persisted account makes no store call and enters with its
/// menu class; the persisted account's stored class beats the opposite menu intent; a persisted-account failure refuses
/// entry with class 255 and never falls back to memory play. Every success follows LoadResult → EnterMap → full HP.
/// Where technical-spec §5.1 and api-contract v2 name different public statuses, the goal's fixed api-contract v2 mapping
/// applies (lead msg_4b303c153958, contract-supplement-2): another current runtime owner is Busy, a historical-only
/// acquired receipt and an incompatible schema are Unavailable.
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

    // Expected public status per api-contract v2 「패킷과 공개 결과」.
    public static TheoryData<string, byte> Refusals => new()
    {
        { "probe read failed", Unavailable },
        { "probe not dispatched", Unavailable },
        { "recovery owner holds the slot", RecoveryRequired },
        { "acquire conflict", Busy },
        { "acquire rejected", Unavailable },
        { "acquire not dispatched", Unavailable },
        { "acquire unknown and unresolved", Unavailable },
        { "another runtime owner holds the slot", Busy },
        { "probe schema incompatible", Unavailable },
        { "acquired receipt is only historical", Unavailable },
    };

    [Theory]
    [MemberData(nameof(MenuClasses))]
    public async Task NonPersistedAccount_EntersWithTheMenuClass_WithoutAnyStoreCall(CharacterClass menuClass)
    {
        using AdmissionTestWorld fixture = new();

        EntrySession session = await fixture.EnterAsync(OtherName, menuClass);

        (byte result, byte loadedClass) = SingleLoadResult(session);
        bool classKnown = fixture.World.TryGetEntityClass(session.EntityId, out byte entityClass);
        Assert.Empty(fixture.Repository.Calls);
        Assert.Equal(Success, result);
        Assert.Equal((byte)menuClass, loadedClass);
        Assert.True(classKnown);
        Assert.Equal((byte)menuClass, entityClass);
        AssertEntryOrder(session);
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
        AssertEntryOrder(session);
    }

    [Theory]
    [MemberData(nameof(OppositeStoredClasses))]
    public async Task StoredClass_BeatsTheOppositeMenuIntent(CharacterClass menuClass, CharacterClass storedClass, int storedMaxHp)
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(menuClass);
        long expectedFence = acquire.Request.Operation.ExpectedFence;
        CharacterClass intendedClass = acquire.Request.IntendedClass;

        acquire.Complete(Acquired(acquire.Request, storedClass, storedMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(session) is not null, "entry with the stored class");

        PlayerEntity player = fixture.PlayerOf(session)!;
        (byte result, byte loadedClass) = SingleLoadResult(session);
        Assert.Equal(ProbedFence, expectedFence);
        Assert.Equal(menuClass, intendedClass);
        Assert.Equal(storedClass, player.Stats.Class);
        Assert.Equal(storedMaxHp, player.MaxHp);
        Assert.Equal(player.MaxHp, player.Hp);
        Assert.Equal(Success, result);
        Assert.Equal((byte)storedClass, loadedClass);
        AssertEntryOrder(session);
        Assert.Equal("Active", RuntimeEntryApi.AdmissionState(session));
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);
    }

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task PersistedAccountFailure_RefusesEntry_WithoutMemoryFallback(string refusal, byte expectedStatus)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        session.Receive(new C_CharacterSelect { characterClass = (byte)CharacterClass.Knight });

        await AnswerAsRefusedAsync(fixture, refusal);
        await fixture.TickUntilAsync(() => FirstIndexOf(session, LoadResultId) >= 0, $"{refusal}: the load result");
        await fixture.TicksAsync(3);
        string[] callsAtResult = fixture.Repository.Calls.ToArray();
        session.Receive(new C_CharacterSelect { characterClass = (byte)CharacterClass.Mage });
        await fixture.TicksAsync(3);

        (byte result, byte loadedClass) = SingleLoadResult(session);
        string state = RuntimeEntryApi.AdmissionState(session);
        Assert.Equal(expectedStatus, result);
        Assert.Equal(NoClass, loadedClass);
        Assert.Equal(-1, FirstIndexOf(session, (ushort)PacketID.S_EnterMap));
        Assert.Empty(fixture.Players);
        Assert.Contains(state, new[] { "Rejected", "Closed" });
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
    static void AssertEntryOrder(EntrySession session)
    {
        int load = FirstIndexOf(session, LoadResultId);
        int enter = FirstIndexOf(session, (ushort)PacketID.S_EnterMap);
        int hp = FirstIndexOf(session, (ushort)PacketID.S_PlayerHp);
        Assert.True(load >= 0, "no S_CharacterLoadResult was sent");
        Assert.True(load < enter, $"S_CharacterLoadResult at {load} is not before S_EnterMap at {enter}");
        Assert.True(enter < hp, $"S_EnterMap at {enter} is not before S_PlayerHp at {hp}");
    }
}
