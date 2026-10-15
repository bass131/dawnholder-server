using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Historical receipt versus current authority, result shapes and receipt invariants from api-contract.md
/// 「저장소·복구 API」 34행 and technical-spec §3.2(code·snapshot)·§5.4·§6.
/// </summary>
public sealed class PersistenceResultContractTests
{
    [Fact]
    public void Acquired_FreshReceiptMatchingCurrent_IsCurrent()
    {
        PersistenceAuthority held = Runtime(8, 0);
        OperationReceipt receipt = AcquiredReceipt(OperationA, held, Token(1), Token(2));
        PersistenceObservation current = Observed(held, Token(1), Token(2));

        AcquireAndLoadResult result = AcquireAndLoadResult.Acquired(receipt, current);

        Assert.Equal(AcquireAndLoadStatus.Acquired, result.Status);
        Assert.Equal(receipt.Authority, result.Receipt!.Authority);
        Assert.Equal(current.Authority, result.Current!.Authority);
        Assert.Null(result.Code);
        Assert.True(result.IsCurrent);
    }

    [Theory]
    [InlineData("otherOwner")]
    [InlineData("laterFence")]
    [InlineData("sequenceAdvanced")]
    [InlineData("released")]
    [InlineData("characterToken")]
    [InlineData("progressToken")]
    public void Acquired_ReplayWhoseCurrentDiffers_StaysHistoricalOnly(string change)
    {
        PersistenceAuthority historical = Runtime(8, 0);
        OperationReceipt receipt = AcquiredReceipt(OperationA, historical, Token(1), Token(2));
        PersistenceObservation current = change switch
        {
            "otherOwner" => Observed(Runtime(9, 0, OtherOwnerId), Token(1), Token(2)),
            "laterFence" => Observed(Runtime(9, 0), Token(1), Token(2)),
            "sequenceAdvanced" => Observed(Runtime(8, 1), Token(1), Token(2)),
            "released" => Observed(Free(9), Token(1), Token(2)),
            "characterToken" => Observed(historical, Token(5), Token(2)),
            _ => Observed(historical, Token(1), Token(6)),
        };

        AcquireAndLoadResult result = AcquireAndLoadResult.Acquired(receipt, current);

        Assert.Equal(AcquireAndLoadStatus.Acquired, result.Status);
        Assert.False(result.IsCurrent);
        Assert.Equal(historical, result.Receipt!.Authority);
        Assert.Equal(Token(1), result.Receipt!.CharacterToken);
        Assert.Equal(current.Authority, result.Current!.Authority);
    }

    [Fact]
    public void Acquired_RejectsWrongReceiptKindIdentityMismatchAndNulls()
    {
        PersistenceAuthority held = Runtime(8, 1);
        OperationReceipt checkpoint = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, held, Token(1), Token(2));
        OperationReceipt acquired = AcquiredReceipt(OperationA, Runtime(8, 0), Token(1), Token(2));
        PersistenceIdentity otherCharacter = new(1, AccountId, OtherOwnerId);
        PersistenceObservation foreign = new(new PersistenceAuthority(otherCharacter, PersistenceOwnerKind.Runtime, RuntimeOwnerId, 8, 0), Token(1), Token(2));

        Assert.ThrowsAny<ArgumentException>(() => AcquireAndLoadResult.Acquired(checkpoint, Observed(held, Token(1), Token(2))));
        Assert.ThrowsAny<ArgumentException>(() => AcquireAndLoadResult.Acquired(acquired, foreign));
        Assert.Throws<ArgumentNullException>(() => AcquireAndLoadResult.Acquired(null!, Observed(held, Token(1), Token(2))));
        Assert.Throws<ArgumentNullException>(() => AcquireAndLoadResult.Acquired(acquired, null!));
    }

    [Fact]
    public void CheckpointResults_SeparateGameUpdateFromNoChange_ByCode()
    {
        PersistenceAuthority afterOne = Runtime(8, 1);
        OperationReceipt applied = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, afterOne, Token(3), Token(4));
        OperationReceipt noChange = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointNoChange, afterOne, Token(1), Token(2));

        WriteSafeCheckpointResult durable = WriteSafeCheckpointResult.Durable(applied, Observed(afterOne, Token(3), Token(4)));
        WriteSafeCheckpointResult unchanged = WriteSafeCheckpointResult.CheckpointNoChange(noChange, Observed(afterOne, Token(1), Token(2)));

        Assert.Equal(WriteSafeCheckpointStatus.Durable, durable.Status);
        Assert.Equal(WriteSafeCheckpointStatus.CheckpointNoChange, unchanged.Status);
        Assert.True(durable.IsCurrent);
        Assert.True(unchanged.IsCurrent);
        Assert.Equal(1, durable.Receipt!.Authority.LastSequence);
        Assert.Equal(Token(3), durable.Receipt!.CharacterToken);
        Assert.ThrowsAny<ArgumentException>(() => WriteSafeCheckpointResult.Durable(noChange, Observed(afterOne, Token(1), Token(2))));
        Assert.ThrowsAny<ArgumentException>(() => WriteSafeCheckpointResult.CheckpointNoChange(applied, Observed(afterOne, Token(3), Token(4))));
    }

    [Fact]
    public void Durable_WhoseCurrentOwnerChanged_IsHistoryOnly()
    {
        OperationReceipt applied = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4));
        PersistenceObservation takenOver = Observed(Runtime(10, 0, OtherOwnerId), Token(3), Token(4));

        WriteSafeCheckpointResult result = WriteSafeCheckpointResult.Durable(applied, takenOver);

        Assert.Equal(WriteSafeCheckpointStatus.Durable, result.Status);
        Assert.False(result.IsCurrent);
        Assert.Equal(Runtime(8, 1), result.Receipt!.Authority);
        Assert.Equal(Runtime(10, 0, OtherOwnerId), result.Current!.Authority);
    }

    [Fact]
    public void RejectedAndNotApplied_RefuseAppliedCodes()
    {
        PersistenceObservation current = Observed(Runtime(8, 1), Token(1), Token(2));

        Assert.ThrowsAny<ArgumentException>(() => AcquireAndLoadResult.Rejected(PersistenceResultCode.Acquired));
        Assert.ThrowsAny<ArgumentException>(() => WriteSafeCheckpointResult.Rejected(PersistenceResultCode.CheckpointApplied));
        Assert.ThrowsAny<ArgumentException>(() => ReleaseResult.Rejected(PersistenceResultCode.Released));
        Assert.ThrowsAny<ArgumentException>(() => ResolveOperationResult.NotApplied(PersistenceResultCode.CheckpointNoChange, current));
    }

    [Fact]
    public void Rejected_CarriesConfirmedCodeWithoutReceipt()
    {
        AcquireAndLoadResult busy = AcquireAndLoadResult.Rejected(PersistenceResultCode.Busy);
        WriteSafeCheckpointResult stale = WriteSafeCheckpointResult.Rejected(PersistenceResultCode.StaleFence);
        ReleaseResult mismatch = ReleaseResult.Rejected(PersistenceResultCode.SequenceMismatch);

        Assert.Equal(AcquireAndLoadStatus.Rejected, busy.Status);
        Assert.Equal(PersistenceResultCode.Busy, busy.Code);
        Assert.Null(busy.Receipt);
        Assert.False(busy.IsCurrent);
        Assert.Equal(WriteSafeCheckpointStatus.Rejected, stale.Status);
        Assert.Equal(PersistenceResultCode.StaleFence, stale.Code);
        Assert.Equal(ReleaseStatus.Rejected, mismatch.Status);
        Assert.Equal(PersistenceResultCode.SequenceMismatch, mismatch.Code);
    }

    [Fact]
    public void NotDispatchedAndUnknown_AreDistinctStatusesWithoutReceipts()
    {
        AcquireAndLoadResult acquireNotSent = AcquireAndLoadResult.NotDispatched();
        AcquireAndLoadResult acquireUnknown = AcquireAndLoadResult.AcquireUnknown();
        WriteSafeCheckpointResult checkpointNotSent = WriteSafeCheckpointResult.NotDispatched();
        WriteSafeCheckpointResult commitUnknown = WriteSafeCheckpointResult.CommitUnknown();
        WriteSafeCheckpointResult conflict = WriteSafeCheckpointResult.Conflict();
        ReleaseResult releaseNotSent = ReleaseResult.NotDispatched();
        ReleaseResult releaseUnknown = ReleaseResult.ReleaseUnknown();

        Assert.Equal(AcquireAndLoadStatus.NotDispatched, acquireNotSent.Status);
        Assert.Equal(AcquireAndLoadStatus.AcquireUnknown, acquireUnknown.Status);
        Assert.Equal(AcquireAndLoadStatus.Conflict, AcquireAndLoadResult.Conflict().Status);
        Assert.Equal(WriteSafeCheckpointStatus.NotDispatched, checkpointNotSent.Status);
        Assert.Equal(WriteSafeCheckpointStatus.CommitUnknown, commitUnknown.Status);
        Assert.Equal(WriteSafeCheckpointStatus.Conflict, conflict.Status);
        Assert.Equal(ReleaseStatus.NotDispatched, releaseNotSent.Status);
        Assert.Equal(ReleaseStatus.ReleaseUnknown, releaseUnknown.Status);
        Assert.All(new object?[] { acquireNotSent.Receipt, acquireUnknown.Receipt, checkpointNotSent.Receipt, commitUnknown.Receipt, releaseNotSent.Receipt, releaseUnknown.Receipt }, receipt => Assert.Null(receipt));
        Assert.False(acquireUnknown.IsCurrent);
        Assert.False(commitUnknown.IsCurrent);
    }

    [Fact]
    public void Released_HistoryGivesNoAuthorityOverTheNewOwner()
    {
        OperationReceipt released = ReleasedReceipt(OperationA, Free(9));
        PersistenceObservation newOwner = Observed(Runtime(10, 0, OtherOwnerId), Token(1), Token(2));

        ReleaseResult result = ReleaseResult.Released(released, newOwner);

        Assert.Equal(ReleaseStatus.Released, result.Status);
        Assert.Equal(Free(9), result.Receipt!.Authority);
        Assert.Equal(Runtime(10, 0, OtherOwnerId), result.Current!.Authority);
    }

    [Fact]
    public void Resolution_SeparatesHistoricalOutcomeFromCurrentAuthority()
    {
        OperationReceipt applied = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4));
        PersistenceObservation same = Observed(Runtime(8, 1), Token(3), Token(4));
        PersistenceObservation other = Observed(Runtime(10, 0, OtherOwnerId), Token(3), Token(4));

        ResolveOperationResult stillUnknown = ResolveOperationResult.StillUnknown();
        ResolveOperationResult sealedOff = ResolveOperationResult.NotApplied(PersistenceResultCode.CancelledBeforeApply, same);
        ResolveOperationResult appliedResult = ResolveOperationResult.Applied(applied, same);
        ResolveOperationResult fenceLost = ResolveOperationResult.FenceLost(applied, other);

        Assert.Equal(OperationResolutionStatus.StillUnknown, stillUnknown.Status);
        Assert.Null(stillUnknown.Receipt);
        Assert.Null(stillUnknown.Current);
        Assert.Equal(OperationResolutionStatus.NotApplied, sealedOff.Status);
        Assert.Equal(PersistenceResultCode.CancelledBeforeApply, sealedOff.Code);
        Assert.Null(sealedOff.Receipt);
        Assert.Equal(OperationResolutionStatus.Applied, appliedResult.Status);
        Assert.Equal(Runtime(8, 1), appliedResult.Receipt!.Authority);
        Assert.Equal(OperationResolutionStatus.FenceLost, fenceLost.Status);
        Assert.Equal(Runtime(8, 1), fenceLost.Receipt!.Authority);
        Assert.Equal(Runtime(10, 0, OtherOwnerId), fenceLost.Current!.Authority);
    }

    [Fact]
    public void AdmissionProbe_IsAnObservationNotAnAcquireReceipt()
    {
        AdmissionProbe probe = new(Free(7), true, true);

        ReadAdmissionResult result = ReadAdmissionResult.Probe(probe);

        Assert.Equal(ReadAdmissionStatus.Probe, result.Status);
        Assert.Equal(Free(7), result.Admission!.ObservedAuthority);
        Assert.True(result.Admission!.SchemaCompatible);
        Assert.True(result.Admission!.BindingMatches);
        Assert.Equal(ReadAdmissionStatus.ReadFailed, ReadAdmissionResult.ReadFailed().Status);
        Assert.Null(ReadAdmissionResult.ReadFailed().Admission);
        Assert.Equal(ReadAdmissionStatus.NotDispatched, ReadAdmissionResult.NotDispatched().Status);
        Assert.Null(ReadAdmissionResult.NotDispatched().Admission);
    }

    [Fact]
    public void Receipt_KeepsSnapshotValues()
    {
        OperationReceipt receipt = AcquiredReceipt(OperationA, Runtime(8, 0), Token(1), Token(2));

        Assert.Equal(OperationA, receipt.OperationId);
        Assert.Equal(PersistenceOperationKind.Acquire, receipt.Kind);
        Assert.Equal(PersistenceResultCode.Acquired, receipt.Code);
        Assert.Equal(Runtime(8, 0), receipt.Authority);
        Assert.Equal(CharacterClass.Knight, receipt.StoredClass);
        Assert.Equal(KnightSafe(), receipt.Safe);
        Assert.Equal(Token(1), receipt.CharacterToken);
        Assert.Equal(Token(2), receipt.ProgressToken);
    }

    [Fact]
    public void Receipt_RejectsNonAppliedCodeKindMismatchAndEmptyId()
    {
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.StaleFence, Runtime(8, 0), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.CheckpointApplied, Runtime(8, 0), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.ReleaseRuntime, PersistenceResultCode.Acquired, Free(9), null, null, null, null));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.Recover, PersistenceResultCode.Released, Recovery(9), null, null, null, null));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(Guid.Empty, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 0), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)));
    }

    [Theory]
    [InlineData("recoveryOwner")]
    [InlineData("sequenceOne")]
    [InlineData("missingCharacterToken")]
    [InlineData("missingSafe")]
    [InlineData("missingClass")]
    [InlineData("safeClassMismatch")]
    public void AcquireReceipt_RejectsPartialOrContradictoryFields(string defect)
    {
        SafeCheckpoint mageSafe = new(CharacterClass.Mage, 3.5f, 0f, 80);

        Exception? error = Record.Exception(() => defect switch
        {
            "recoveryOwner" => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Recovery(8), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)),
            "sequenceOne" => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 1), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)),
            "missingCharacterToken" => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 0), CharacterClass.Knight, KnightSafe(), null, Token(2)),
            "missingSafe" => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 0), CharacterClass.Knight, null, Token(1), Token(2)),
            "missingClass" => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 0), null, KnightSafe(), Token(1), Token(2)),
            _ => new OperationReceipt(OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, Runtime(8, 0), CharacterClass.Knight, mageSafe, Token(1), Token(2)),
        });

        Assert.IsAssignableFrom<ArgumentException>(error);
    }

    [Fact]
    public void CheckpointAndReleaseReceipts_RequireTheirAuthorityShape()
    {
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.Checkpoint, PersistenceResultCode.CheckpointApplied, Runtime(8, 0), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.Checkpoint, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), CharacterClass.Knight, KnightSafe(), null, Token(2)));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationA, PersistenceOperationKind.ReleaseRuntime, PersistenceResultCode.Released, Runtime(8, 1), null, null, null, null));
    }

    [Fact]
    public void RecoverReceipt_NeverCarriesRuntimeSafeProjection()
    {
        OperationReceipt recovered = new(OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9), CharacterClass.Knight, null, Token(1), Token(2));

        Assert.Null(recovered.Safe);
        Assert.Equal(PersistenceOwnerKind.Recovery, recovered.Authority.OwnerKind);
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9), CharacterClass.Knight, KnightSafe(), Token(1), Token(2)));
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Runtime(9, 0), CharacterClass.Knight, null, Token(1), Token(2)));
    }

    [Fact]
    public void RecoveredAbsentReceipt_HasNoStoredCharacterValues()
    {
        OperationReceipt absent = new(OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.RecoveredAbsent, Recovery(9), null, null, null, null);

        Assert.Null(absent.StoredClass);
        Assert.Null(absent.CharacterToken);
        Assert.ThrowsAny<ArgumentException>(() => new OperationReceipt(OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.RecoveredAbsent, Recovery(9), CharacterClass.Knight, null, Token(1), null));
    }

    [Fact]
    public void Observation_KeepsCurrentValues_AndRequiresAuthority()
    {
        PersistenceObservation observation = new(Runtime(8, 2), Token(1), null);

        Assert.Equal(Runtime(8, 2), observation.Authority);
        Assert.Equal(Token(1), observation.CharacterToken);
        Assert.Null(observation.ProgressToken);
        Assert.Throws<ArgumentNullException>(() => new PersistenceObservation(null!, Token(1), Token(2)));
    }
}
