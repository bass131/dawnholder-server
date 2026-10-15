using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.CharacterPersistenceOwnerHarness;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;
using Repo = Dawnholder.Server.GameServer.Tests.Persistence.RecordingCharacterRepository;

namespace Dawnholder.Server.GameServer.Tests.Persistence.RuntimeEntry;

/// <summary>
/// The LoadedClass seam (api-contract v2 「서버 입장·owner 출력·종료」 and 「선행시험과 두 writer가 공유할 최소 타입
/// 접점(v2)」): writer2 adds one immutable PersistenceWorkResult.LoadedClass. It holds the stored class only when the
/// owner's existing checks confirmed a current acquire (Status Acquired); conflicts, refusals, undispatched work,
/// history-only receipts and checkpoint work leave it null. The owner harness and its literal store answers are
/// reused; the stored class comes from the scripted receipt, never from the intent the test submitted.
/// </summary>
public sealed class PersistenceWorkResultLoadedClassTests
{
    public static TheoryData<string> AnswersWithoutACurrentAcquire => new()
    {
        "conflict",
        "rejected",
        "not dispatched",
        "historical receipt with another current owner",
    };

    [Fact]
    public async Task CurrentAcquire_PublishesTheStoredKnight_NotTheMageIntent()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission submission = lane.Owner.TrySubmitAcquire(7, CharacterClass.Mage, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();

        // AcquiredReceipt reports Knight as the stored class.
        call.Complete(FreshAcquired(call.Request.Operation.OperationId, 8, Token(1), Token(2)));
        PersistenceWorkResult result = await Within(submission.Completion!);

        Assert.Equal(PersistenceWorkStatus.Acquired, result.Status);
        Assert.Equal(CharacterClass.Knight, RuntimeEntryApi.LoadedClass(result));
    }

    [Fact]
    public async Task CurrentAcquire_PublishesTheStoredMage_NotTheKnightIntent()
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission submission = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        PersistenceAuthority held = Runtime(8, 0);
        OperationReceipt receipt = new(
            call.Request.Operation.OperationId, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired,
            held, CharacterClass.Mage, new SafeCheckpoint(CharacterClass.Mage, 3.5f, 0f, 80), Token(1), Token(2));

        call.Complete(AcquireAndLoadResult.Acquired(receipt, Observed(held, Token(1), Token(2))));
        PersistenceWorkResult result = await Within(submission.Completion!);

        Assert.Equal(PersistenceWorkStatus.Acquired, result.Status);
        Assert.Equal(CharacterClass.Mage, RuntimeEntryApi.LoadedClass(result));
    }

    [Theory]
    [MemberData(nameof(AnswersWithoutACurrentAcquire))]
    public async Task AcquireAnswerWithoutACurrentAcquire_LeavesLoadedClassNull(string answer)
    {
        CharacterPersistenceOwnerHarness lane = new();
        PersistenceSubmission submission = lane.Owner.TrySubmitAcquire(7, CharacterClass.Knight, Defaults(), lane.Budget());
        Repo.PendingCall<AcquireAndLoadRequest, AcquireAndLoadResult> call = await lane.Repository.NextAcquireAsync();
        Guid operationId = call.Request.Operation.OperationId;

        call.Complete(answer switch
        {
            "conflict" => AcquireAndLoadResult.Conflict(),
            "rejected" => AcquireAndLoadResult.Rejected(PersistenceResultCode.IdentityMismatch),
            "not dispatched" => AcquireAndLoadResult.NotDispatched(),
            "historical receipt with another current owner" => AcquireAndLoadResult.Acquired(
                AcquiredReceipt(operationId, Runtime(8, 0), Token(1), Token(2)),
                Observed(Runtime(9, 0, OtherOwnerId), Token(3), Token(4))),
            _ => throw new ArgumentOutOfRangeException(nameof(answer), answer, "unknown answer case"),
        });
        PersistenceWorkResult result = await Within(submission.Completion!);

        Assert.NotEqual(PersistenceWorkStatus.Acquired, result.Status);
        Assert.Null(RuntimeEntryApi.LoadedClass(result));
    }

    [Fact]
    public async Task CheckpointWork_LeavesLoadedClassNull()
    {
        CharacterPersistenceOwnerHarness lane = new();
        await lane.AcquireAsync();
        (PersistenceSubmission submission, Repo.PendingCall<WriteSafeCheckpointRequest, WriteSafeCheckpointResult> call) =
            await lane.CheckpointInFlightAsync(KnightSafeMoved(), lane.Budget());

        call.Complete(Durable(call.Request.Operation.OperationId, 1, Token(3), Token(4)));
        PersistenceWorkResult result = await Within(submission.Completion!);

        Assert.Equal(PersistenceWorkStatus.Durable, result.Status);
        Assert.Null(RuntimeEntryApi.LoadedClass(result));
    }
}
