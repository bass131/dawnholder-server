using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Result shapes that the request/result contract tests leave open: token presence in the current check,
/// recovery diagnostics with explicit absence and no partial shape, historical versus current recovery
/// values and the runtime/recovery split of results.
/// Source: api-contract.md 「저장소·복구 API」, api-surface-addendum-1.md, technical-spec §3.2·§5.3~§5.5·§6.
/// </summary>
public sealed class PersistenceResultShapeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void IsCurrent_TreatsMissingProgressAndZeroTokenAsDifferent(bool receiptHasZeroProgress, bool currentHasZeroProgress)
    {
        PersistenceToken zero = new(new byte[8]);
        PersistenceAuthority held = Runtime(8, 0);
        OperationReceipt receipt = new(
            OperationA, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, held,
            CharacterClass.Knight, KnightSafe(), Token(1), receiptHasZeroProgress ? zero : null);

        AcquireAndLoadResult result = AcquireAndLoadResult.Acquired(receipt, Observed(held, Token(1), currentHasZeroProgress ? zero : null));

        Assert.Equal(receiptHasZeroProgress == currentHasZeroProgress, result.IsCurrent);
    }

    // Only refusal statuses carry a result Code: api-surface v2 「해당 상태가 아닌 property는 null」 and the
    // Rejected/NotApplied code arguments; Conflict keeps its code per main msg_b2124c0c72b9. Applied and
    // fence-lost results leave it null while the historical receipt keeps its own code.
    [Theory]
    [InlineData("acquired", null)]
    [InlineData("durable", null)]
    [InlineData("checkpointNoChange", null)]
    [InlineData("released", null)]
    [InlineData("resolveApplied", null)]
    [InlineData("fenceLostWithHistory", null)]
    [InlineData("fenceLostWithoutHistory", null)]
    [InlineData("acquireConflict", PersistenceResultCode.Conflict)]
    [InlineData("checkpointConflict", PersistenceResultCode.Conflict)]
    [InlineData("checkpointRejected", PersistenceResultCode.StaleFence)]
    [InlineData("resolveNotApplied", PersistenceResultCode.CancelledBeforeApply)]
    public void RuntimeResults_CarryCodeOnlyOnRefusals_WhileTheReceiptKeepsItsOwnCode(string path, PersistenceResultCode? expectedCode)
    {
        (Func<object> create, object expectedStatus, OperationReceipt? expectedReceipt, PersistenceResultCode? expectedReceiptCode,
            PersistenceObservation? expectedCurrent, bool? expectedIsCurrent) = RuntimeResultCase(path);

        object result = create();
        object again = create();
        (object status, PersistenceResultCode? code, OperationReceipt? receipt, PersistenceObservation? current, bool? isCurrent) = View(result);

        Assert.Equal(expectedStatus, status);
        Assert.Equal(expectedCode, code);
        Assert.Equal(expectedReceipt, receipt);
        Assert.Equal(expectedReceiptCode, receipt?.Code);
        Assert.Equal(expectedCurrent, current);
        Assert.Equal(expectedIsCurrent, isCurrent);
        Assert.Equal(result, again);
        Assert.Equal(result.GetHashCode(), again.GetHashCode());
    }

    [Fact]
    public void StoredState_RepresentsCompleteAbsence_WithoutInventingDefaults()
    {
        InspectRecoveryResult.StoredState absent = new(Identity(), null, null, null, null);

        InspectRecoveryResult inspected = InspectRecoveryResult.Inspected(Observed(Free(9), null, null), absent);

        Assert.Equal(InspectRecoveryStatus.Inspected, inspected.Status);
        Assert.False(inspected.Stored!.CharacterPresent);
        Assert.False(inspected.Stored.ProgressPresent);
        Assert.Null(inspected.Stored.StoredClass);
        Assert.Null(inspected.Stored.Progress);
    }

    [Theory]
    [InlineData("classWithoutToken")]
    [InlineData("tokenWithoutClass")]
    [InlineData("progressWithoutToken")]
    [InlineData("progressTokenWithoutProgress")]
    [InlineData("progressWithoutCharacter")]
    [InlineData("undefinedClass")]
    public void StoredState_RejectsPartialOrContradictoryShapes(string defect)
    {
        InspectRecoveryResult.StoredProgress raw = new(3, 10f, -2f, 0, 80, true);

        Exception? error = Record.Exception(() => defect switch
        {
            "classWithoutToken" => new InspectRecoveryResult.StoredState(Identity(), CharacterClass.Mage, null, null, null),
            "tokenWithoutClass" => new InspectRecoveryResult.StoredState(Identity(), null, Token(1), null, null),
            "progressWithoutToken" => new InspectRecoveryResult.StoredState(Identity(), CharacterClass.Mage, Token(1), null, raw),
            "progressTokenWithoutProgress" => new InspectRecoveryResult.StoredState(Identity(), CharacterClass.Mage, Token(1), Token(2), null),
            "progressWithoutCharacter" => new InspectRecoveryResult.StoredState(Identity(), null, null, Token(2), raw),
            _ => new InspectRecoveryResult.StoredState(Identity(), (CharacterClass)5, Token(1), null, null),
        });

        Assert.IsAssignableFrom<ArgumentException>(error);
    }

    [Fact]
    public void Inspected_RejectsDiagnosticsThatDisagreeWithTheCurrentObservation()
    {
        InspectRecoveryResult.StoredState stored = new(Identity(), CharacterClass.Mage, Token(1), Token(2), new(3, 10f, -2f, 0, 80, true));

        InspectRecoveryResult matching = InspectRecoveryResult.Inspected(Observed(Runtime(8, 3), Token(1), Token(2)), stored);

        Assert.Equal(InspectRecoveryStatus.Inspected, matching.Status);
        Assert.ThrowsAny<ArgumentException>(() => InspectRecoveryResult.Inspected(Observed(Runtime(8, 3), Token(1), Token(9)), stored));
        Assert.ThrowsAny<ArgumentException>(() => InspectRecoveryResult.Inspected(Observed(Runtime(8, 3), Token(1), null), stored));
    }

    [Fact]
    public void Recovered_KeepsRawStoredProgress_AndCarriesNoRuntimeSafe()
    {
        InspectRecoveryResult.StoredState stored = new(Identity(), CharacterClass.Mage, Token(1), Token(2), new(3, 10.5f, -2f, 0, 80, true));
        OperationReceipt receipt = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9),
            CharacterClass.Mage, null, Token(1), Token(2));

        RecoverAndLoadResult result = RecoverAndLoadResult.Recovered(receipt, Observed(Recovery(9), Token(1), Token(2)), stored);

        Assert.Equal(RecoverAndLoadStatus.Recovered, result.Status);
        Assert.True(result.IsCurrent);
        Assert.Null(result.Receipt!.Safe);
        InspectRecoveryResult.StoredProgress progress = result.Stored!.Progress!;
        Assert.Equal(3, progress.MapId);
        Assert.Equal(10.5f, progress.X);
        Assert.Equal(-2f, progress.Y);
        Assert.Equal(0, progress.Hp);
        Assert.Equal(80, progress.MaxHp);
        Assert.True(progress.BossUnlocked);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("characterToken")]
    [InlineData("progressToken")]
    public void Recovered_RejectsDiagnosticsThatDisagreeWithTheReceipt(string differing)
    {
        OperationReceipt receipt = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9),
            CharacterClass.Mage, null, Token(1), Token(2));
        InspectRecoveryResult.StoredProgress raw = new(0, 0f, 0f, 80, 80, false);
        InspectRecoveryResult.StoredState stored = differing switch
        {
            "class" => new(Identity(), CharacterClass.Knight, Token(1), Token(2), raw),
            "characterToken" => new(Identity(), CharacterClass.Mage, Token(5), Token(2), raw),
            _ => new(Identity(), CharacterClass.Mage, Token(1), Token(6), raw),
        };

        Assert.ThrowsAny<ArgumentException>(() => RecoverAndLoadResult.Recovered(receipt, Observed(Recovery(9), Token(1), Token(2)), stored));
    }

    [Fact]
    public void RecoveredAbsent_RequiresTheAbsentDiagnosticShape()
    {
        OperationReceipt absent = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.RecoveredAbsent, Recovery(9), null, null, null, null);
        InspectRecoveryResult.StoredState none = new(Identity(), null, null, null, null);
        InspectRecoveryResult.StoredState present = new(Identity(), CharacterClass.Mage, Token(1), null, null);
        PersistenceObservation current = Observed(Recovery(9), null, null);

        RecoverAndLoadResult result = RecoverAndLoadResult.RecoveredAbsent(absent, current, none);

        Assert.Equal(RecoverAndLoadStatus.RecoveredAbsent, result.Status);
        Assert.False(result.Stored!.CharacterPresent);
        Assert.ThrowsAny<ArgumentException>(() => RecoverAndLoadResult.RecoveredAbsent(absent, current, present));
        Assert.ThrowsAny<ArgumentException>(() => RecoverAndLoadResult.Recovered(absent, current, none));
    }

    [Fact]
    public void RecoveryReplay_KeepsHistoricalDiagnosticsApartFromTheMovedCurrentAuthority()
    {
        OperationReceipt receipt = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9),
            CharacterClass.Mage, null, Token(1), Token(2));
        InspectRecoveryResult.StoredState stored = new(Identity(), CharacterClass.Mage, Token(1), Token(2), new(0, 1f, 1f, 80, 80, false));
        PersistenceObservation moved = Observed(Runtime(11, 0, OtherOwnerId), Token(3), Token(4));

        RecoverAndLoadResult replay = RecoverAndLoadResult.Recovered(receipt, moved, stored);
        ResolveRecoveryOperationResult lost = ResolveRecoveryOperationResult.FenceLost(receipt, moved, stored);

        Assert.False(replay.IsCurrent);
        Assert.Equal(Recovery(9), replay.Receipt!.Authority);
        Assert.Equal(Token(1), replay.Stored!.CharacterToken);
        Assert.Equal(Runtime(11, 0, OtherOwnerId), replay.Current!.Authority);
        Assert.Equal(RecoveryOperationResolutionStatus.FenceLost, lost.Status);
        Assert.Equal(Token(1), lost.Stored!.CharacterToken);
        Assert.Equal(Token(3), lost.Current!.CharacterToken);
    }

    [Fact]
    public void RecoveryResolution_AttachesDiagnosticsOnlyToRecoverReceipts()
    {
        OperationReceipt recover = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9),
            CharacterClass.Mage, null, Token(1), Token(2));
        OperationReceipt releaseRecovery = new(
            OperationB, PersistenceOperationKind.ReleaseRecovery, PersistenceResultCode.Released, Free(10), null, null, null, null);
        InspectRecoveryResult.StoredState stored = new(Identity(), CharacterClass.Mage, Token(1), Token(2), new(0, 1f, 1f, 80, 80, false));
        PersistenceObservation free = Observed(Free(10), Token(1), Token(2));

        ResolveRecoveryOperationResult released = ResolveRecoveryOperationResult.Applied(releaseRecovery, free);

        Assert.Equal(RecoveryOperationResolutionStatus.Applied, released.Status);
        Assert.Null(released.Stored);
        Assert.ThrowsAny<ArgumentException>(() => ResolveRecoveryOperationResult.Applied(releaseRecovery, free, stored));
        Assert.ThrowsAny<ArgumentException>(() => ResolveRecoveryOperationResult.Applied(recover, Observed(Recovery(9), Token(1), Token(2))));
    }

    [Fact]
    public void Results_RefuseReceiptsFromTheOtherRepositoryDomain()
    {
        OperationReceipt recover = new(
            OperationB, PersistenceOperationKind.Recover, PersistenceResultCode.Recovered, Recovery(9),
            CharacterClass.Mage, null, Token(1), Token(2));
        OperationReceipt releaseRecovery = new(
            OperationB, PersistenceOperationKind.ReleaseRecovery, PersistenceResultCode.Released, Free(10), null, null, null, null);
        OperationReceipt checkpoint = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4));

        Assert.ThrowsAny<ArgumentException>(() => ResolveOperationResult.Applied(recover, Observed(Recovery(9), Token(1), Token(2))));
        Assert.ThrowsAny<ArgumentException>(() => ResolveOperationResult.FenceLost(recover, Observed(Runtime(10, 0, OtherOwnerId), null, null)));
        Assert.ThrowsAny<ArgumentException>(() => ResolveRecoveryOperationResult.Applied(checkpoint, Observed(Runtime(8, 1), Token(3), Token(4))));
        Assert.ThrowsAny<ArgumentException>(() => ReleaseResult.Released(releaseRecovery, Observed(Free(10), null, null)));
        Assert.ThrowsAny<ArgumentException>(() => ReleaseRecoveryResult.Released(ReleasedReceipt(OperationA, Free(9)), Observed(Free(9), null, null)));
    }

    public static TheoryData<Type> ContractTypesOutsideTheRequestResultList => new()
    {
        typeof(InspectRecoveryResult),
        typeof(InspectRecoveryResult.StoredState),
        typeof(InspectRecoveryResult.StoredProgress),
        typeof(RecoverAndLoadResult),
        typeof(ReleaseRecoveryResult),
        typeof(ResolveRecoveryOperationResult),
        typeof(CloseCaptureTicket),
        typeof(PersistenceCloseResult),
        typeof(PersistenceSubmission),
        typeof(PersistenceWorkResult),
    };

    // The same structural guard as PersistenceValueContractTests, applied to the recovery diagnostics and
    // the owner's public values that the pre-implementation list did not name.
    [Theory]
    [MemberData(nameof(ContractTypesOutsideTheRequestResultList))]
    public void ContractTypes_ExposeNoPublicMutation(Type type)
    {
        string[] settable = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();
        string[] writableFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => !field.IsInitOnly)
            .Select(field => field.Name)
            .ToArray();

        Assert.True(type.IsSealed, $"{type.Name} must be sealed.");
        Assert.Empty(settable);
        Assert.Empty(writableFields);
    }

    // Each case calls one runtime factory with scripted store values; the expectations are those inputs.
    static (Func<object> Create, object Status, OperationReceipt? Receipt, PersistenceResultCode? ReceiptCode,
        PersistenceObservation? Current, bool? IsCurrent) RuntimeResultCase(string path)
    {
        OperationReceipt acquired = AcquiredReceipt(OperationA, Runtime(8, 0), Token(1), Token(2));
        OperationReceipt applied = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointApplied, Runtime(8, 1), Token(3), Token(4));
        OperationReceipt noChange = CheckpointReceipt(OperationA, PersistenceResultCode.CheckpointNoChange, Runtime(8, 1), Token(1), Token(2));
        OperationReceipt released = ReleasedReceipt(OperationA, Free(9));
        PersistenceObservation afterAcquire = Observed(Runtime(8, 0), Token(1), Token(2));
        PersistenceObservation afterApplied = Observed(Runtime(8, 1), Token(3), Token(4));
        PersistenceObservation afterNoChange = Observed(Runtime(8, 1), Token(1), Token(2));
        PersistenceObservation afterRelease = Observed(Free(9), null, null);
        PersistenceObservation takenOver = Observed(Runtime(10, 0, OtherOwnerId), Token(5), Token(6));

        switch (path)
        {
            case "acquired":
                return (() => AcquireAndLoadResult.Acquired(acquired, afterAcquire), AcquireAndLoadStatus.Acquired,
                    acquired, PersistenceResultCode.Acquired, afterAcquire, true);
            case "durable":
                return (() => WriteSafeCheckpointResult.Durable(applied, afterApplied), WriteSafeCheckpointStatus.Durable,
                    applied, PersistenceResultCode.CheckpointApplied, afterApplied, true);
            case "checkpointNoChange":
                return (() => WriteSafeCheckpointResult.CheckpointNoChange(noChange, afterNoChange), WriteSafeCheckpointStatus.CheckpointNoChange,
                    noChange, PersistenceResultCode.CheckpointNoChange, afterNoChange, true);
            case "released":
                return (() => ReleaseResult.Released(released, afterRelease), ReleaseStatus.Released,
                    released, PersistenceResultCode.Released, afterRelease, null);
            case "resolveApplied":
                return (() => ResolveOperationResult.Applied(applied, afterApplied), OperationResolutionStatus.Applied,
                    applied, PersistenceResultCode.CheckpointApplied, afterApplied, null);
            case "fenceLostWithHistory":
                return (() => ResolveOperationResult.FenceLost(applied, takenOver), OperationResolutionStatus.FenceLost,
                    applied, PersistenceResultCode.CheckpointApplied, takenOver, null);
            case "fenceLostWithoutHistory":
                return (() => ResolveOperationResult.FenceLost(null, takenOver), OperationResolutionStatus.FenceLost,
                    null, null, takenOver, null);
            case "acquireConflict":
                return (() => AcquireAndLoadResult.Conflict(), AcquireAndLoadStatus.Conflict, null, null, null, false);
            case "checkpointConflict":
                return (() => WriteSafeCheckpointResult.Conflict(), WriteSafeCheckpointStatus.Conflict, null, null, null, false);
            case "checkpointRejected":
                return (() => WriteSafeCheckpointResult.Rejected(PersistenceResultCode.StaleFence), WriteSafeCheckpointStatus.Rejected,
                    null, null, null, false);
            case "resolveNotApplied":
                return (() => ResolveOperationResult.NotApplied(PersistenceResultCode.CancelledBeforeApply, afterApplied),
                    OperationResolutionStatus.NotApplied, null, null, afterApplied, null);
            default:
                throw new ArgumentOutOfRangeException(nameof(path), path, null);
        }
    }

    // The four runtime result types share no interface; IsCurrent exists only on acquire and checkpoint results.
    static (object Status, PersistenceResultCode? Code, OperationReceipt? Receipt, PersistenceObservation? Current, bool? IsCurrent) View(object result)
    {
        switch (result)
        {
            case AcquireAndLoadResult acquire:
                return (acquire.Status, acquire.Code, acquire.Receipt, acquire.Current, acquire.IsCurrent);
            case WriteSafeCheckpointResult checkpoint:
                return (checkpoint.Status, checkpoint.Code, checkpoint.Receipt, checkpoint.Current, checkpoint.IsCurrent);
            case ReleaseResult release:
                return (release.Status, release.Code, release.Receipt, release.Current, null);
            case ResolveOperationResult resolution:
                return (resolution.Status, resolution.Code, resolution.Receipt, resolution.Current, null);
            default:
                throw new ArgumentException($"Unexpected runtime result {result.GetType().Name}.", nameof(result));
        }
    }
}
