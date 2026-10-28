using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 5 (runtime-entry pretest contract; api-contract v2 「서버 입장·owner 출력·종료」; technical-spec §6·§7):
/// a result that arrives after the connection closed spawns nothing and sends nothing, while the owner still settles
/// the late acquire with one conditional release; a late probe starts no acquire; an unchanged player closes as
/// NoWriteNeeded (checkpoint RPC 0, one release); and the Host flush shares one absolute deadline instead of giving
/// each step a new full budget. The 2 second budget is the Host's existing default named by api-contract v2.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionCloseTests
{
    static readonly TimeSpan HostBudget = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task CloseDuringLoading_LateAcquireSpawnsNothing_AndTheOwnerStillReleases()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        int mark = session.Mark;

        session.Disconnect();
        await fixture.TickAsync();
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Mage, MageMaxHp));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        release.Complete(Released(release.Request));
        await fixture.TicksAsync(3);

        Assert.Empty(session.IdsSince(mark));
        Assert.Empty(fixture.Players);
        Assert.Equal(PersistenceOperationKind.ReleaseRuntime, release.Request.Operation.Kind);
        Assert.Equal(HeldAuthority(PersistenceOwnerKind.Runtime, acquire.Request.Operation.OwnerId, AcquiredFence), release.Request.Authority);
        Assert.Equal(1, release.Request.Operation.Sequence);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Repository.Calls);
        Assert.Equal("Closed", RuntimeEntryApi.AdmissionState(session));
    }

    [Fact]
    public async Task CloseWhileReadingAdmission_LateProbeStartsNoAcquire()
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        fixture.SelectStored(session, CharacterClass.Knight);
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();
        int mark = session.Mark;

        session.Disconnect();
        await fixture.TickAsync();
        probe.Complete(FreeProbe());
        await fixture.TicksAsync(3);

        Assert.Equal(new[] { Repo.Admission }, fixture.Repository.Calls);
        Assert.Empty(session.IdsSince(mark));
        Assert.Empty(fixture.Players);
        Assert.Equal("Closed", RuntimeEntryApi.AdmissionState(session));
    }

    [Fact]
    public async Task UnchangedPlayerClose_SendsNoCheckpoint_AndOneConditionalRelease()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Knight, KnightMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(session) is not null, "entry before the close");

        session.Disconnect();
        await fixture.TickAsync();
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        release.Complete(Released(release.Request));
        await fixture.TicksAsync(3);

        Assert.Equal(0, fixture.Repository.Count(Repo.Checkpoint));
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Repository.Calls);
        Assert.Equal(HeldAuthority(PersistenceOwnerKind.Runtime, acquire.Request.Operation.OwnerId, AcquiredFence), release.Request.Authority);
        Assert.Equal(1, release.Request.Operation.Sequence);
        Assert.Empty(fixture.Players);
    }

    [Fact]
    public async Task HostFlush_WaitsForTheOwnersRelease_WithinItsBudget()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Knight, KnightMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(session) is not null, "entry before the shutdown");

        session.Disconnect();
        await fixture.TickAsync();
        Task flush = RuntimeEntryApi.FlushPersistenceClosuresAsync(fixture.World, PersistenceDeadline.FromBudget(fixture.Clock, HostBudget));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        await fixture.TicksAsync(2);
        bool doneBeforeRelease = flush.IsCompleted;
        release.Complete(Released(release.Request));
        await fixture.TickUntilAsync(() => flush.IsCompleted, "the host flush after the release");

        Assert.False(doneBeforeRelease, "the flush reported the owner's work finished while its release was pending");
        Assert.True(flush.IsCompletedSuccessfully, $"flush ended as {flush.Status}");
    }

    // One budget across acquire settlement and release: 1.5 s spent on the late acquire leaves 0.5 s, not a new 2 s.
    [Fact]
    public async Task HostFlush_EndsAtItsOneAbsoluteDeadline_WhileStoreWorkIsStillPending()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        session.Disconnect();
        await fixture.TickAsync();

        Task flush = RuntimeEntryApi.FlushPersistenceClosuresAsync(fixture.World, PersistenceDeadline.FromBudget(fixture.Clock, HostBudget));
        await fixture.TicksAsync(2);
        bool doneWhileAcquiring = flush.IsCompleted;
        fixture.Clock.Advance(TimeSpan.FromSeconds(1.5));
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Knight, KnightMaxHp));
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        await fixture.TicksAsync(2);
        bool doneWhileReleasing = flush.IsCompleted;
        fixture.Clock.Advance(TimeSpan.FromSeconds(0.5));
        await fixture.TickUntilAsync(() => flush.IsCompleted, "the host flush at its single 2 second deadline");
        bool releaseStillPending = !release.ResultTask.IsCompleted;

        // The owner keeps tracking the release after the host stopped waiting; its late answer is still accepted.
        release.Complete(Released(release.Request));
        await fixture.TicksAsync(2);

        Assert.False(doneWhileAcquiring);
        Assert.False(doneWhileReleasing, "the flush ended before its deadline while the release was pending");
        Assert.True(releaseStillPending);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Repository.Calls);
        Assert.DoesNotContain(session.Sent(), sent => sent.Id is 54 or LoadResultId || sent.Id == (ushort)PacketID.S_EnterMap);
    }
}
