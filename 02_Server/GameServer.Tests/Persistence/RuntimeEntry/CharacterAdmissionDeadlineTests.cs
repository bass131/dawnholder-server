using System.Buffers.Binary;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry.AdmissionTestWorld;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// Independent verification of two admission failure lifetimes (api-contract v2 「패킷과 공개 결과」 Unavailable row and
/// 「서버 입장·owner 출력·종료」; verification contract criteria 2 and 3):
/// an acquire that is still unanswered after every store budget of technical-spec §7 has passed emits the current
/// catalog OutcomeUnknown54 exactly once and closes without successful48, its late success spawns nothing, and the owner still settles it with one
/// conditional release when the connection ends; and a C_CharacterSelect frame of the wrong length is a malformed
/// client input that is silently dropped without any store call and without latching the one selection.
/// </summary>
[Collection("GameWorldRegistryTests")]
public sealed class CharacterAdmissionDeadlineTests
{
    // Longer than every budget technical-spec §7 names (Open 5 s, command 5 s, lock 2 s). Not a product constant.
    static readonly TimeSpan BeyondEveryStoreBudget = TimeSpan.FromMinutes(1);

    // The Host's existing default shutdown budget named by api-contract v2.
    static readonly TimeSpan HostBudget = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task AcquireUnansweredPastEveryBudget_RefusesOnce_AndTheLateAcquireIsReleasedOnDisconnect()
    {
        using AdmissionTestWorld fixture = new();
        (EntrySession session, Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> acquire) =
            await fixture.LoadingAsync(CharacterClass.Mage);

        fixture.Clock.Advance(BeyondEveryStoreBudget);
        await fixture.TickUntilAsync(() => FirstIndexOf(session, 54) >= 0, "the refusal after the store budgets");
        (byte refused, byte invalidField) = SingleSelectFailure(session);
        string stateAfterRefusal = RuntimeEntryApi.AdmissionState(session);

        acquire.Complete(Acquired(acquire.Request, CharacterClass.Mage, MageMaxHp));
        await fixture.TicksAsync(3);
        (byte resultAfterLateAcquire, _) = SingleSelectFailure(session);
        int enterMapIndex = FirstIndexOf(session, (ushort)PacketID.S_EnterMap);
        int playersAfterLateAcquire = fixture.Players.Count();

        session.Disconnect();
        Repo.PendingCall<ReleaseRequest, ReleaseResult> release = await fixture.NextReleaseAsync();
        release.Complete(Released(release.Request));
        PersistenceDeadline hostDeadline = PersistenceDeadline.FromBudget(fixture.Clock, HostBudget);
        Exception? flushFailure = await Record.ExceptionAsync(() =>
            PersistenceTestValues.Within(RuntimeEntryApi.FlushPersistenceClosuresAsync(fixture.World, hostDeadline)));

        Assert.Equal((byte)12, refused);
        Assert.Equal((byte)0, invalidField);
        Assert.Equal("Closed", stateAfterRefusal);
        Assert.Equal((byte)12, resultAfterLateAcquire);
        Assert.Equal(-1, FirstIndexOf(session, LoadResultId));
        Assert.Equal(-1, enterMapIndex);
        Assert.Equal(0, playersAfterLateAcquire);
        Assert.Null(flushFailure);
        Assert.Equal(new[] { Repo.Admission, Repo.Acquire, Repo.Release }, fixture.Repository.Calls);
        Assert.Equal(acquire.Request.Operation.OwnerId, release.Request.Operation.OwnerId);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public async Task CharacterSelectFrameOfAnotherLength_IsDroppedWithoutAStoreCall_AndLatchesNothing(int length)
    {
        using AdmissionTestWorld fixture = new();
        EntrySession session = await fixture.LogInAsync(PersistedName);
        byte[] frame = new byte[length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame, (ushort)length);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(2), (ushort)PacketID.C_CharacterSelect);
        if (length > 4)
            frame[4] = (byte)CharacterClass.Mage;

        session.OnRecvPacket(new ArraySegment<byte>(frame));
        await fixture.TicksAsync(3);
        string[] callsAfterMalformed = fixture.Repository.Calls.ToArray();
        int loadResultAfterMalformed = FirstIndexOf(session, LoadResultId);
        string stateAfterMalformed = RuntimeEntryApi.AdmissionState(session);

        fixture.SelectStored(session, CharacterClass.Mage);
        Repo.PendingCall<ReadAdmissionRequest, ReadAdmissionResult> probe = await fixture.NextReadAdmissionAsync();

        Assert.Empty(callsAfterMalformed);
        Assert.Equal(-1, loadResultAfterMalformed);
        Assert.Equal("Selecting", stateAfterMalformed);
        Assert.Equal(PersistedIdentity, probe.Request.Identity);
    }
}
