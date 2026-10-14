using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Request and operation identity invariants plus the runtime/recovery split from api-contract.md
/// 「저장소·복구 API」 and technical-spec §3.1(sequence/overflow)·§3.3(payload)·§5.4(resolver kinds).
/// </summary>
public sealed class PersistenceRequestContractTests
{
    [Fact]
    public void AcquireRequest_FixesOperationIdentityWithSequenceZero()
    {
        AcquireAndLoadRequest request = new(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Mage, Defaults());

        OperationIdentity operation = request.Operation;
        Assert.Equal(OperationA, operation.OperationId);
        Assert.Equal(PersistenceOperationKind.Acquire, operation.Kind);
        Assert.Equal(Identity(), operation.Identity);
        Assert.Equal(RuntimeOwnerId, operation.OwnerId);
        Assert.Equal(7, operation.ExpectedFence);
        Assert.Equal(0, operation.Sequence);
        Assert.Equal(CharacterClass.Mage, request.IntendedClass);
        Assert.Equal(Defaults(), request.SafeDefaults);
    }

    [Fact]
    public void AcquireRequest_RejectsMissingIdsNegativeFenceAndUndefinedClass()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AcquireAndLoadRequest(Identity(), Guid.Empty, RuntimeOwnerId, 0, CharacterClass.Knight, Defaults()));
        Assert.ThrowsAny<ArgumentException>(() => new AcquireAndLoadRequest(Identity(), OperationA, Guid.Empty, 0, CharacterClass.Knight, Defaults()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, -1, CharacterClass.Knight, Defaults()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 0, (CharacterClass)9, Defaults()));
        Assert.Throws<ArgumentNullException>(() => new AcquireAndLoadRequest(null!, OperationA, RuntimeOwnerId, 0, CharacterClass.Knight, Defaults()));
        Assert.Throws<ArgumentNullException>(() => new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 0, CharacterClass.Knight, null!));
    }

    [Fact]
    public void CheckpointRequest_BindsCurrentRuntimeOwnerFenceAndNextSequence()
    {
        PersistenceAuthority current = Runtime(8, 4);
        WriteSafeCheckpointRequest request = new(current, 5, OperationA, Token(1), Token(2), KnightSafe());

        OperationIdentity operation = request.Operation;
        Assert.Equal(PersistenceOperationKind.Checkpoint, operation.Kind);
        Assert.Equal(OperationA, operation.OperationId);
        Assert.Equal(RuntimeOwnerId, operation.OwnerId);
        Assert.Equal(8, operation.ExpectedFence);
        Assert.Equal(5, operation.Sequence);
        Assert.Equal(current, request.Authority);
        Assert.Equal(Token(1), request.ExpectedCharacterToken);
        Assert.Equal(Token(2), request.ExpectedProgressToken);
        Assert.Equal(KnightSafe(), request.Safe);
    }

    [Theory]
    [InlineData(4L)]
    [InlineData(6L)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void CheckpointRequest_RejectsAnySequenceOtherThanLastPlusOne(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriteSafeCheckpointRequest(Runtime(8, 4), sequence, OperationA, Token(1), Token(2), KnightSafe()));
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void MutationRequests_CannotFollowTheLargestSequence(long sequence)
    {
        PersistenceAuthority exhausted = Runtime(8, long.MaxValue);

        Assert.Throws<ArgumentOutOfRangeException>(() => new WriteSafeCheckpointRequest(exhausted, sequence, OperationA, Token(1), Token(2), KnightSafe()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseRequest(exhausted, sequence, OperationA));
    }

    [Fact]
    public void CheckpointRequest_RequiresRuntimeAuthority()
    {
        Assert.ThrowsAny<ArgumentException>(() => new WriteSafeCheckpointRequest(Recovery(8, 0), 1, OperationA, Token(1), Token(2), KnightSafe()));
        Assert.ThrowsAny<ArgumentException>(() => new WriteSafeCheckpointRequest(Free(8), 1, OperationA, Token(1), Token(2), KnightSafe()));
    }

    [Fact]
    public void CheckpointRequest_RequiresCharacterTokenAndSafe_ButAllowsMissingProgress()
    {
        WriteSafeCheckpointRequest missingProgress = new(Runtime(8, 0), 1, OperationA, Token(1), null, KnightSafe());

        Assert.Null(missingProgress.ExpectedProgressToken);
        Assert.Throws<ArgumentNullException>(() => new WriteSafeCheckpointRequest(Runtime(8, 0), 1, OperationA, null!, Token(2), KnightSafe()));
        Assert.Throws<ArgumentNullException>(() => new WriteSafeCheckpointRequest(Runtime(8, 0), 1, OperationA, Token(1), Token(2), null!));
        Assert.ThrowsAny<ArgumentException>(() => new WriteSafeCheckpointRequest(Runtime(8, 0), 1, Guid.Empty, Token(1), Token(2), KnightSafe()));
    }

    [Fact]
    public void CheckpointOperation_TreatsMissingProgressAndZeroTokenAsDifferentPayloads()
    {
        PersistenceToken zero = new(new byte[8]);
        WriteSafeCheckpointRequest missing = new(Runtime(8, 0), 1, OperationA, Token(1), null, KnightSafe());
        WriteSafeCheckpointRequest zeroProgress = new(Runtime(8, 0), 1, OperationA, Token(1), zero, KnightSafe());

        Assert.Null(missing.ExpectedProgressToken);
        Assert.Equal(zero, zeroProgress.ExpectedProgressToken);
        Assert.NotEqual(missing.Operation, zeroProgress.Operation);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("operationId")]
    [InlineData("characterToken")]
    [InlineData("progressToken")]
    [InlineData("safe")]
    public void CheckpointOperation_EqualityCoversIdAndTypedPayload(string differing)
    {
        WriteSafeCheckpointRequest original = new(Runtime(8, 0), 1, OperationA, Token(1), Token(2), KnightSafe());
        WriteSafeCheckpointRequest other = differing switch
        {
            "operationId" => new(Runtime(8, 0), 1, OperationB, Token(1), Token(2), KnightSafe()),
            "characterToken" => new(Runtime(8, 0), 1, OperationA, Token(3), Token(2), KnightSafe()),
            "progressToken" => new(Runtime(8, 0), 1, OperationA, Token(1), Token(4), KnightSafe()),
            "safe" => new(Runtime(8, 0), 1, OperationA, Token(1), Token(2), KnightSafeMoved()),
            _ => new(Runtime(8, 0), 1, OperationA, Token(1), Token(2), KnightSafe()),
        };

        bool equal = original.Operation.Equals(other.Operation);

        Assert.Equal(differing == "same", equal);
    }

    [Fact]
    public void ReleaseRequest_UsesReleaseRuntimeKind_AndRequiresRuntimeNextSequence()
    {
        ReleaseRequest request = new(Runtime(8, 2), 3, OperationA);

        Assert.Equal(PersistenceOperationKind.ReleaseRuntime, request.Operation.Kind);
        Assert.Equal(RuntimeOwnerId, request.Operation.OwnerId);
        Assert.Equal(8, request.Operation.ExpectedFence);
        Assert.Equal(3, request.Operation.Sequence);
        Assert.Equal(Runtime(8, 2), request.Authority);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseRequest(Runtime(8, 2), 2, OperationA));
        Assert.ThrowsAny<ArgumentException>(() => new ReleaseRequest(Recovery(8, 2), 3, OperationA));
        Assert.ThrowsAny<ArgumentException>(() => new ReleaseRequest(Free(8), 1, OperationA));
        Assert.ThrowsAny<ArgumentException>(() => new ReleaseRequest(Runtime(8, 2), 3, Guid.Empty));
    }

    [Theory]
    [InlineData(ResolveMode.QueryOnly)]
    [InlineData(ResolveMode.SealIfAbsent)]
    public void RuntimeResolver_AcceptsOnlyRuntimeOperationKinds(ResolveMode mode)
    {
        OperationIdentity acquire = new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 0, CharacterClass.Knight, Defaults()).Operation;
        OperationIdentity checkpoint = new WriteSafeCheckpointRequest(Runtime(8, 0), 1, OperationA, Token(1), Token(2), KnightSafe()).Operation;
        OperationIdentity release = new ReleaseRequest(Runtime(8, 0), 1, OperationA).Operation;
        OperationIdentity recover = new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime").Operation;
        OperationIdentity releaseRecovery = new ReleaseRecoveryRequest(Recovery(9, 0), 1, OperationB).Operation;

        foreach (OperationIdentity accepted in new[] { acquire, checkpoint, release })
        {
            ResolveOperationRequest request = new(accepted, mode);
            Assert.Equal(accepted, request.Operation);
            Assert.Equal(mode, request.Mode);
        }
        Assert.ThrowsAny<ArgumentException>(() => new ResolveOperationRequest(recover, mode));
        Assert.ThrowsAny<ArgumentException>(() => new ResolveOperationRequest(releaseRecovery, mode));
    }

    [Fact]
    public void RuntimeResolver_RejectsUndefinedModeAndMissingOperation()
    {
        OperationIdentity release = new ReleaseRequest(Runtime(8, 0), 1, OperationA).Operation;

        Assert.Throws<ArgumentOutOfRangeException>(() => new ResolveOperationRequest(release, (ResolveMode)7));
        Assert.Throws<ArgumentNullException>(() => new ResolveOperationRequest(null!, ResolveMode.QueryOnly));
    }

    [Fact]
    public void RecoverRequest_UsesObservedOwnerAndFence_WithNewAdminOwnerAndSequenceZero()
    {
        PersistenceAuthority observed = Runtime(8, 3);
        RecoverAndLoadRequest request = new(observed, RecoveryOwnerId, OperationB, "quiesced runtime");

        Assert.Equal(PersistenceOperationKind.Recover, request.Operation.Kind);
        Assert.Equal(RecoveryOwnerId, request.Operation.OwnerId);
        Assert.Equal(8, request.Operation.ExpectedFence);
        Assert.Equal(0, request.Operation.Sequence);
        Assert.Equal(OperationB, request.Operation.OperationId);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(128, false)]
    [InlineData(0, true)]
    [InlineData(129, true)]
    public void RecoverRequest_ReasonMustBeOneTo128Characters(int length, bool rejected)
    {
        string reason = new('r', length);

        Exception? error = Record.Exception(() => new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, OperationB, reason));

        if (rejected)
            Assert.IsType<ArgumentOutOfRangeException>(error);
        else
            Assert.Null(error);
    }

    [Fact]
    public void RecoverRequest_RejectsNullReasonAndEmptyIds()
    {
        Assert.Throws<ArgumentNullException>(() => new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, OperationB, null!));
        Assert.ThrowsAny<ArgumentException>(() => new RecoverAndLoadRequest(Runtime(8, 3), Guid.Empty, OperationB, "reason"));
        Assert.ThrowsAny<ArgumentException>(() => new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, Guid.Empty, "reason"));
        Assert.Throws<ArgumentNullException>(() => new RecoverAndLoadRequest(null!, RecoveryOwnerId, OperationB, "reason"));
    }

    [Fact]
    public void ReleaseRecoveryRequest_RequiresRecoveryAuthority()
    {
        ReleaseRecoveryRequest request = new(Recovery(9, 0), 1, OperationB);

        Assert.Equal(PersistenceOperationKind.ReleaseRecovery, request.Operation.Kind);
        Assert.Equal(1, request.Operation.Sequence);
        Assert.ThrowsAny<ArgumentException>(() => new ReleaseRecoveryRequest(Runtime(9, 0), 1, OperationB));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReleaseRecoveryRequest(Recovery(9, 0), 2, OperationB));
    }

    [Fact]
    public void RecoveryResolver_AcceptsOnlyRecoveryOperationKinds()
    {
        OperationIdentity recover = new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime").Operation;
        OperationIdentity releaseRecovery = new ReleaseRecoveryRequest(Recovery(9, 0), 1, OperationB).Operation;
        OperationIdentity checkpoint = new WriteSafeCheckpointRequest(Runtime(8, 0), 1, OperationA, Token(1), Token(2), KnightSafe()).Operation;
        OperationIdentity release = new ReleaseRequest(Runtime(8, 0), 1, OperationA).Operation;
        OperationIdentity acquire = new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 0, CharacterClass.Knight, Defaults()).Operation;

        Assert.Equal(recover, new ResolveRecoveryOperationRequest(recover, ResolveMode.QueryOnly).Operation);
        Assert.Equal(releaseRecovery, new ResolveRecoveryOperationRequest(releaseRecovery, ResolveMode.SealIfAbsent).Operation);
        Assert.ThrowsAny<ArgumentException>(() => new ResolveRecoveryOperationRequest(checkpoint, ResolveMode.QueryOnly));
        Assert.ThrowsAny<ArgumentException>(() => new ResolveRecoveryOperationRequest(release, ResolveMode.QueryOnly));
        Assert.ThrowsAny<ArgumentException>(() => new ResolveRecoveryOperationRequest(acquire, ResolveMode.QueryOnly));
    }

    [Fact]
    public void ReadOnlyRequests_KeepIdentityAndRejectNull()
    {
        Assert.Equal(Identity(), new ReadAdmissionRequest(Identity()).Identity);
        Assert.Equal(Identity(), new InspectRecoveryRequest(Identity()).Identity);
        Assert.Throws<ArgumentNullException>(() => new ReadAdmissionRequest(null!));
        Assert.Throws<ArgumentNullException>(() => new InspectRecoveryRequest(null!));
    }

    [Fact]
    public void RuntimeRepository_ExposesExactlyTheFiveRuntimeOperations()
    {
        (string Name, Type Request, Type Result)[] expected =
        {
            (nameof(ICharacterRepository.ReadAdmissionAsync), typeof(ReadAdmissionRequest), typeof(ReadAdmissionResult)),
            (nameof(ICharacterRepository.AcquireAndLoadAsync), typeof(AcquireAndLoadRequest), typeof(AcquireAndLoadResult)),
            (nameof(ICharacterRepository.WriteSafeCheckpointAsync), typeof(WriteSafeCheckpointRequest), typeof(WriteSafeCheckpointResult)),
            (nameof(ICharacterRepository.ReleaseAsync), typeof(ReleaseRequest), typeof(ReleaseResult)),
            (nameof(ICharacterRepository.ResolveOperationAsync), typeof(ResolveOperationRequest), typeof(ResolveOperationResult)),
        };

        AssertOperations(typeof(ICharacterRepository), expected);
    }

    [Fact]
    public void RecoveryRepository_ExposesExactlyTheFourAdminOperations()
    {
        (string Name, Type Request, Type Result)[] expected =
        {
            (nameof(ICharacterRecoveryRepository.InspectRecoveryAsync), typeof(InspectRecoveryRequest), typeof(InspectRecoveryResult)),
            (nameof(ICharacterRecoveryRepository.RecoverAndLoadAsync), typeof(RecoverAndLoadRequest), typeof(RecoverAndLoadResult)),
            (nameof(ICharacterRecoveryRepository.ReleaseRecoveryAsync), typeof(ReleaseRecoveryRequest), typeof(ReleaseRecoveryResult)),
            (nameof(ICharacterRecoveryRepository.ResolveRecoveryOperationAsync), typeof(ResolveRecoveryOperationRequest), typeof(ResolveRecoveryOperationResult)),
        };

        AssertOperations(typeof(ICharacterRecoveryRepository), expected);
    }

    [Fact]
    public void RuntimeAndRecoveryRepositories_DoNotInheritEachOther()
    {
        Assert.False(typeof(ICharacterRepository).IsAssignableFrom(typeof(ICharacterRecoveryRepository)));
        Assert.False(typeof(ICharacterRecoveryRepository).IsAssignableFrom(typeof(ICharacterRepository)));
    }

    static void AssertOperations(Type contract, (string Name, Type Request, Type Result)[] expected)
    {
        MethodInfo[] methods = contract.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Assert.True(contract.IsInterface);
        Assert.Equal(expected.Select(item => item.Name).OrderBy(name => name), methods.Select(method => method.Name).OrderBy(name => name));
        foreach ((string name, Type request, Type result) in expected)
        {
            MethodInfo method = Assert.Single(methods, candidate => candidate.Name == name);
            Assert.Equal(
                new[] { request, typeof(PersistenceDeadline), typeof(CancellationToken) },
                method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            Assert.Equal(typeof(Task<>).MakeGenericType(result), method.ReturnType);
        }
    }
}
