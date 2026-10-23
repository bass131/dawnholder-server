namespace Dawnholder.Server.GameServer.Tests.Persistence.CharacterCatalog;

/// <summary>
/// API v1 BeginSelectCharacter and 「선택·취소·종료 상태 계약」 without a database. The handle is returned before storage
/// work ends so World can close it at once; Close returns the same finalization however often it is called; a second
/// select on the same RequestContext starts no storage work (RequestInProgress or InvalidState); a cancelled token
/// ends the completion as CancelledBeforeDispatch with no storage call; and a closed or silent select ends within the
/// budget without reporting Selected. Late success after a real acquire is a database-tier test.
/// </summary>
public sealed class CharacterSelectLifetimeTests
{
    [Fact]
    public async Task Handle_IsReturnedBeforeStorageAnswers()
    {
        using OfflineCatalog offline = new();

        SelectStart start = offline.Catalog.BeginSelect(offline.ValidCall());
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);
        bool completedEarly = start.Handle?.Completion.IsCompleted ?? true;

        Assert.Null(start.StartRejection);
        Assert.NotNull(start.Handle);
        Assert.False(completedEarly, "Completion ended although storage has not answered");
    }

    [Fact]
    public void Close_ReturnsTheSameFinalizationEveryTime()
    {
        using OfflineCatalog offline = new();
        AdmissionHandleView handle = RequiredHandle(offline.Catalog.BeginSelect(offline.ValidCall()));

        Task first = handle.Close(null);
        Task second = handle.Close(null);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task SecondSelectOnTheSameRequest_StartsNoStorageWork()
    {
        using OfflineCatalog offline = new();
        CatalogCall first = offline.ValidCall();
        AdmissionHandleView running = RequiredHandle(offline.Catalog.BeginSelect(first));
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);
        int callsBefore = (await offline.CallsAfterSettling()).Count;

        SelectStart second = offline.Catalog.BeginSelect(first with { OperationId = OfflineCatalog.OperationB });
        string? rejection = await StartRejectionOf(second);
        int callsAfter = (await offline.CallsAfterSettling()).Count;

        Assert.Contains(rejection, new[] { "RequestInProgress", "InvalidState" });
        Assert.Equal(callsBefore, callsAfter);
        Assert.False(running.Completion.IsCompleted, "the running select was ended by the rejected second select");
    }

    [Fact]
    public async Task TokenCancelledBeforeDispatch_EndsWithoutStorage()
    {
        using OfflineCatalog offline = new();
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        SelectStart start = offline.Catalog.BeginSelect(offline.ValidCall(cancelled.Token));
        // Cancellation is a completion result; v2's immediate handle does not promise synchronous completion.
        string? status = start.StartRejection ?? await CompletedStatus(start.Handle!);
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("CancelledBeforeDispatch", status);
        Assert.Empty(storageCalls);
    }

    [Fact]
    public async Task EmptyCharacterId_IsRejectedBeforeStorage()
    {
        using OfflineCatalog offline = new();

        SelectStart start = offline.Catalog.BeginSelect(offline.ValidCall() with { CharacterId = Guid.Empty });
        string? status = await StartRejectionOf(start);
        IReadOnlyList<string> storageCalls = await offline.CallsAfterSettling();

        Assert.Equal("InvalidInput", status);
        Assert.Empty(storageCalls);
    }

    // 「완료 전 취소/연결 종료」 and 「예산 만료」: the waiting completion ends bounded; it never reports Selected because the
    // handle was closed and storage never answered. Core keeps owning the dispatched call, so Close is not awaited here.
    [Fact]
    public async Task CloseBeforeStorageAnswers_EndsBoundedWithoutSelected()
    {
        using OfflineCatalog offline = new();
        AdmissionHandleView handle = RequiredHandle(offline.Catalog.BeginSelect(offline.ValidCall()));
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);

        _ = handle.Close(null);
        await offline.ExpireBudgetUntil(handle.Completion);
        string status = await CompletedStatus(handle);

        Assert.Contains(status, new[] { "OutcomeUnknown", "Unavailable", "CancelledBeforeDispatch" });
    }

    [Fact]
    public async Task StorageSilence_EndsTheSelectBoundedWithoutSelected()
    {
        using OfflineCatalog offline = new();
        AdmissionHandleView handle = RequiredHandle(offline.Catalog.BeginSelect(offline.ValidCall()));
        await offline.Store.FirstCall.WaitAsync(StoreCallRecorder.Limit);

        await offline.ExpireBudgetUntil(handle.Completion);
        string status = await CompletedStatus(handle);

        Assert.Contains(status, new[] { "OutcomeUnknown", "Unavailable" });
    }

    static AdmissionHandleView RequiredHandle(SelectStart start)
    {
        Assert.True(start.Handle is not null, $"expected an admission handle, got start rejection {start.StartRejection}");
        return start.Handle!;
    }

    /// <summary>A start rejection is either returned directly or as a handle whose completion already ended.</summary>
    static async Task<string?> StartRejectionOf(SelectStart start)
    {
        if (start.StartRejection is not null)
            return start.StartRejection;
        bool immediate = start.Handle!.Completion.IsCompleted;
        Assert.True(immediate, "a start rejection must not wait for storage");
        return await CompletedStatus(start.Handle);
    }

    static async Task<string> CompletedStatus(AdmissionHandleView handle)
    {
        await handle.Completion.WaitAsync(StoreCallRecorder.Limit);
        return handle.CompletedOutcome().Status;
    }
}
