using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Requirement 2 (runtime-entry pretest contract; api-contract v2 「서버 입장·owner 출력·종료」; CODE 「상태와 책임」):
/// the controlled store (RecordingCharacterRepository, each call a TaskCompletionSource the test completes) holds
/// ReadAdmission and then AcquireAndLoad pending while unrelated tick work keeps progressing. Every tick runs through
/// the existing ReturnsPromptly, so a tick that waits on store I/O (Wait/Result) fails instead of hanging; no sleep,
/// timing threshold or timer is added. A completed store answer reaches game state only inside a later tick.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionTickTests
{
    [Fact]
    public async Task PendingReadAdmission_LeavesTheTickFreeForOtherWork()
    {
        using AdmissionTestWorld fixture = new();
        EntrySession persisted = await fixture.LogInAsync(PersistedName);
        fixture.SelectStored(persisted, CharacterClass.Knight);
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();
        int mapJobs = 0;
        fixture.World.Map.EnqueueJob(() => mapJobs++);

        await fixture.TickAsync();
        EntrySession other = await fixture.EnterAsync(OtherName, CharacterClass.Mage);

        bool otherKnown = fixture.World.TryGetEntityClass(other.EntityId, out byte otherClass);
        Assert.Equal(1, mapJobs);
        Assert.True(otherKnown, "an unrelated login and entry did not progress while the probe was pending");
        Assert.Equal((byte)CharacterClass.Mage, otherClass);
        Assert.Null(fixture.PlayerOf(persisted));
        Assert.Equal("Loading", RuntimeEntryApi.AdmissionState(persisted));
        Assert.Equal(new[] { Repo.Admission }, fixture.Repository.Calls);
        Assert.False(probe.ResultTask.IsCompleted);
    }

    [Fact]
    public async Task PendingAcquire_LeavesTheTickFree_AndTheResultEntersOnlyInsideATick()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession persisted, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Knight);
        int mapJobs = 0;
        fixture.World.Map.EnqueueJob(() => mapJobs++);

        await fixture.TickAsync();
        EntrySession other = await fixture.EnterAsync(OtherName, CharacterClass.Mage);
        int jobsWhilePending = mapJobs;
        bool enteredWhilePending = fixture.PlayerOf(persisted) is not null;
        acquire.Complete(Acquired(acquire.Request, CharacterClass.Knight, KnightMaxHp));
        await fixture.TickUntilAsync(() => fixture.PlayerOf(persisted) is not null, "entry after the acquire");

        SentPacket[] enterMaps = persisted.Sent().Where(sent => sent.Id == (ushort)PacketID.S_EnterMap).ToArray();
        Assert.Equal(1, jobsWhilePending);
        Assert.False(enteredWhilePending);
        Assert.NotNull(fixture.PlayerOf(other));
        SentPacket enterMap = Assert.Single(enterMaps);
        Assert.True(enterMap.InsideTick, "S_EnterMap was sent outside a world tick: the store continuation entered directly");
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire }, fixture.Repository.Calls);
    }
}
