using Dawnholder.Server.GameServer.Persistence;
using Microsoft.Data.SqlClient;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog.CatalogDatabase;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog.CharacterCreateDatabaseTests;

namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// Real Core API → SQL → result for select, ownership and lifetime (goal 완료조건 4·5·6·7, API v1 「공개 작업과 결과」,
/// 「선택·취소·종료 상태 계약」). Selection loads only an existing character of the same account with its stored class;
/// another account's character is NotOwned and an unknown ID is CharacterNotFound; a held character is Busy for a second
/// owner until the first handle is closed; distinct characters do not wait on the old global Slot.1 lock; a handle
/// closed before or after its completion leaves the character selectable again; listing never creates rows; created
/// characters are read back by a new catalog instance. Operation IDs of different kinds never reuse each other.
/// </summary>
[Trait("Category", "CharacterCatalogDatabase")]
[Collection(CatalogDatabase.Collection)]
public sealed class CharacterSelectDatabaseTests
{
    [CatalogDatabaseFact]
    public async Task CreateListSelect_CloseSavesCheckpoint_ThenReconnectReadsStoredValues()
    {
        object alice;
        SummaryView created;
        using (CatalogDatabase db = Open())
        {
            (alice, _) = db.NewAccount();
            CatalogUnderTest catalog = db.NewCatalog();
            created = await Created(catalog, alice, 2, db.NewName("save"), CharacterClass.Mage);
            Assert.Equal(created, Assert.Single(await Listed(catalog, alice)));

            (CatalogOutcome selected, AdmissionHandleView handle) = await Select(catalog, alice, created.CharacterId);
            try
            {
                Assert.Equal("Selected", selected.Status);
                Assert.Equal(created, selected.Character);
            }
            finally
            {
                await handle.Close(new SafeCheckpoint(CharacterClass.Mage, 7.25f, -2.5f, 123))
                    .WaitAsync(StoreCallRecorder.Limit);
            }
        }

        // The first fixture has disposed its pool. Read the committed projection before a new acquire can affect it.
        using CatalogDatabase reconnected = Open();
        await using (SqlConnection connection = reconnected.OpenRawConnection())
        {
            await using SqlCommand command = new(
                "SELECT MapId, PositionX, PositionY, Hp, MaxHp, BossUnlocked " +
                "FROM dh.CharacterProgress WHERE CharacterId = @character;", connection);
            command.Parameters.AddWithValue("@character", created.CharacterId);
            await using SqlDataReader row = await command.ExecuteReaderAsync();
            Assert.True(await row.ReadAsync());
            Assert.Equal((byte)0, row.GetByte(0));
            Assert.Equal(7.25f, row.GetFloat(1));
            Assert.Equal(-2.5f, row.GetFloat(2));
            Assert.Equal(123, row.GetInt32(3));
            Assert.Equal(123, row.GetInt32(4));
            Assert.False(row.GetBoolean(5));
            Assert.False(await row.ReadAsync());
        }

        CatalogUnderTest after = reconnected.NewCatalog();
        Assert.Equal(created, Assert.Single(await Listed(after, alice)));
        (CatalogOutcome again, AdmissionHandleView next) = await Select(after, alice, created.CharacterId);
        try
        {
            Assert.Equal("Selected", again.Status);
            Assert.Equal(created, again.Character);
        }
        finally
        {
            await next.Close(null).WaitAsync(StoreCallRecorder.Limit);
        }
    }

    [CatalogDatabaseFact]
    public async Task Select_OwnCharacter_LoadsTheStoredSummaryAndClass()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        SummaryView mage = await Created(catalog, alice, 2, db.NewName("m"), CharacterClass.Mage);

        (CatalogOutcome selected, AdmissionHandleView handle) = await Select(catalog, alice, mage.CharacterId);
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal("Selected", selected.Status);
        Assert.Equal(mage, selected.Character);
        Assert.NotNull(selected.Lease);
    }

    [CatalogDatabaseFact]
    public async Task Select_AnotherAccountsCharacterOrAnUnknownId_IsRefusedWithoutDisclosure()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        var (bob, _) = db.NewAccount();
        SummaryView aliceCharacter = await Created(catalog, alice, 1, db.NewName("o"), CharacterClass.Knight);

        (CatalogOutcome notOwned, AdmissionHandleView first) = await Select(catalog, bob, aliceCharacter.CharacterId);
        (CatalogOutcome notFound, AdmissionHandleView second) = await Select(catalog, bob, Guid.NewGuid());
        await Task.WhenAll(first.Close(null), second.Close(null)).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal("NotOwned", notOwned.Status);
        Assert.Null(notOwned.Character);
        Assert.Equal("CharacterNotFound", notFound.Status);
        Assert.Empty(await Listed(catalog, bob));
    }

    [CatalogDatabaseFact]
    public async Task HeldCharacter_IsBusyForASecondOwner_UntilTheFirstHandleCloses()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        SummaryView knight = await Created(catalog, alice, 1, db.NewName("k"), CharacterClass.Knight);

        (CatalogOutcome held, AdmissionHandleView owner) = await Select(catalog, alice, knight.CharacterId);
        (CatalogOutcome second, AdmissionHandleView refused) = await Select(catalog, alice, knight.CharacterId);
        await Task.WhenAll(owner.Close(null), refused.Close(null)).WaitAsync(StoreCallRecorder.Limit);
        (CatalogOutcome third, AdmissionHandleView next) = await Select(catalog, alice, knight.CharacterId);
        await next.Close(null).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal("Selected", held.Status);
        Assert.Equal("Busy", second.Status);
        Assert.Equal("Selected", third.Status);
    }

    // goal 완료조건 6: different characters are not serialized by the existing global resource Dawnholder.Persistence.Slot.1.
    [CatalogDatabaseFact]
    public async Task DistinctCharacter_DoesNotWaitForTheOldGlobalSlotLock()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        SummaryView knight = await Created(catalog, alice, 1, db.NewName("g"), CharacterClass.Knight);
        await using SqlConnection blocker = db.OpenRawConnection();
        await using SqlTransaction hold = (SqlTransaction)await blocker.BeginTransactionAsync();
        await using (SqlCommand takeGlobal = new(
            "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1', " +
            "@DbPrincipal = 'public', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; SELECT @r;",
            blocker, hold))
            Assert.InRange(Convert.ToInt32(await takeGlobal.ExecuteScalarAsync()), 0, 1);

        (CatalogOutcome selected, AdmissionHandleView handle) = await Select(catalog, alice, knight.CharacterId);
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);
        await hold.RollbackAsync();

        Assert.Equal("Selected", selected.Status);
    }

    // 「선택·취소·종료」: a handle closed at any point leaves no stale owner; the next select of the same character wins.
    [CatalogDatabaseFact]
    public async Task HandleClosedAtAnyPoint_LeavesTheCharacterSelectableAgain()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        SummaryView knight = await Created(catalog, alice, 1, db.NewName("c"), CharacterClass.Knight);

        for (int delayMs = 0; delayMs <= 40; delayMs += 10)
        {
            SelectStart start = catalog.BeginSelect(Call(alice, characterId: knight.CharacterId));
            Assert.Null(start.StartRejection);
            if (delayMs > 0)
                await Task.Delay(delayMs);
            await start.Handle!.Close(null).WaitAsync(StoreCallRecorder.Limit);

            (CatalogOutcome again, AdmissionHandleView next) = await Select(catalog, alice, knight.CharacterId);
            await next.Close(null).WaitAsync(StoreCallRecorder.Limit);

            Assert.True(again.Status == "Selected", $"after a close at {delayMs} ms the next select was {again.Status}");
        }
    }

    [CatalogDatabaseFact]
    public async Task OperationIdOfASelect_IsNotReusedByACreateOrItsResolution()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (alice, _) = db.NewAccount();
        SummaryView knight = await Created(catalog, alice, 1, db.NewName("i"), CharacterClass.Knight);
        CatalogCall selectCall = Call(alice, characterId: knight.CharacterId);
        (_, AdmissionHandleView handle) = await Select(catalog, selectCall);
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);

        CatalogCall reused = Call(alice, slot: 2, name: db.NewName("j"), operationId: selectCall.OperationId);
        CatalogOutcome create = await catalog.CreateAsync(reused);
        CatalogOutcome resolve = await catalog.ResolveCreateAsync(reused);

        Assert.Equal("OperationConflict", create.Status);
        Assert.Equal("OperationConflict", resolve.Status);
        Assert.Single(await Listed(catalog, alice));
    }

    [CatalogDatabaseFact]
    public async Task Listing_ForAnAccountWithoutRows_IsEmptyAndCreatesNoRow()
    {
        using CatalogDatabase db = Open();
        CatalogUnderTest catalog = db.NewCatalog();
        var (carol, carolId) = db.NewAccount();

        IReadOnlyList<SummaryView> listed = await Listed(catalog, carol);
        await using SqlConnection probe = db.OpenRawConnection();
        const string countAccount = "SELECT COUNT(*) FROM dh.Account WHERE AccountId = @account;";
        long accountRows = await Scalar(probe, countAccount, ("@account", carolId));

        Assert.Empty(listed);
        Assert.Equal(0, accountRows);
    }

    [CatalogDatabaseFact]
    public async Task CreatedCharacters_AreReadBackByANewCatalogInstance()
    {
        using CatalogDatabase db = Open();
        var (alice, _) = db.NewAccount();
        CatalogUnderTest before = db.NewCatalog();
        SummaryView first = await Created(before, alice, 1, db.NewName("s"), CharacterClass.Mage);
        SummaryView second = await Created(before, alice, 3, db.NewName("t"), CharacterClass.Knight);

        CatalogUnderTest after = db.NewCatalog();
        IReadOnlyList<SummaryView> listed = await Listed(after, alice);
        (CatalogOutcome selected, AdmissionHandleView handle) = await Select(after, alice, second.CharacterId);
        await handle.Close(null).WaitAsync(StoreCallRecorder.Limit);

        Assert.Equal(new[] { first, second }, listed);
        Assert.Equal(second, selected.Character);
    }

    static async Task<SummaryView> Created(
        CatalogUnderTest catalog, object account, int slot, string name, CharacterClass characterClass)
    {
        CatalogOutcome outcome = await catalog.CreateAsync(Call(account, slot, name, characterClass));
        Assert.True(outcome.Status == "Created", $"fixture create of {name} ended as {outcome.Status}");
        return outcome.Character!;
    }

    static Task<(CatalogOutcome, AdmissionHandleView)> Select(CatalogUnderTest catalog, object account, Guid characterId)
        => Select(catalog, Call(account, characterId: characterId));

    static async Task<(CatalogOutcome, AdmissionHandleView)> Select(CatalogUnderTest catalog, CatalogCall call)
    {
        SelectStart start = catalog.BeginSelect(call);
        Assert.True(start.Handle is not null, $"select start was rejected as {start.StartRejection}");
        await start.Handle!.Completion.WaitAsync(StoreCallRecorder.Limit);
        return (start.Handle.CompletedOutcome(), start.Handle);
    }
}
