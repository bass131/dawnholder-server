using System.Reflection;
using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;
using static Dawnholder.Server.GameServer.Tests.Persistence.PersistenceTestValues;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// The resolver re-sends the original operation, so equality must cover every canonical payload field of
/// its kind and nothing outside that payload; fields of other kinds stay empty.
/// Source: api-contract.md 「공통 값과 소유」 OperationIdentity, api-surface-addendum-1.md, technical-spec §3.3.
/// </summary>
public sealed class OperationIdentityContractTests
{
    [Fact]
    public void Operation_HasNoPublicConstructor_SoOnlyValidatedRequestsCreateIt()
    {
        Assert.Empty(typeof(OperationIdentity).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Theory]
    [InlineData("same")]
    [InlineData("operationId")]
    [InlineData("owner")]
    [InlineData("fence")]
    [InlineData("intendedClass")]
    [InlineData("safeDefaults")]
    public void AcquireOperation_EqualityCoversOwnerFenceIntendedClassAndDefaults(string differing)
    {
        OperationIdentity original = new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Knight, Defaults()).Operation;
        AcquireAndLoadRequest other = differing switch
        {
            "operationId" => new(Identity(), OperationB, RuntimeOwnerId, 7, CharacterClass.Knight, Defaults()),
            "owner" => new(Identity(), OperationA, OtherOwnerId, 7, CharacterClass.Knight, Defaults()),
            "fence" => new(Identity(), OperationA, RuntimeOwnerId, 8, CharacterClass.Knight, Defaults()),
            "intendedClass" => new(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Mage, Defaults()),
            "safeDefaults" => new(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Knight, new SafeDefaults(3.5f, 0f, 150, 81)),
            _ => new(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Knight, Defaults()),
        };

        bool equal = original.Equals(other.Operation);

        Assert.Equal(differing == "same", equal);
        if (equal)
            Assert.Equal(original.GetHashCode(), other.Operation.GetHashCode());
    }

    // §3.3 Recover payload: observed ExpectedOwnerKind/Id, expected fence, new admin owner and reason.
    // The observed LastSequence is not part of it, so it must not split one operation into two.
    [Theory]
    [InlineData("same")]
    [InlineData("observedSequenceOnly")]
    [InlineData("observedOwnerId")]
    [InlineData("observedOwnerKind")]
    [InlineData("observedFence")]
    [InlineData("recoveryOwner")]
    [InlineData("reason")]
    public void RecoverOperation_IgnoresObservedSequence_ButKeepsPreviousOwnerFenceNewOwnerAndReason(string differing)
    {
        RecoverAndLoadRequest original = new(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime");
        RecoverAndLoadRequest other = differing switch
        {
            "observedSequenceOnly" => new(Runtime(8, 5), RecoveryOwnerId, OperationB, "quiesced runtime"),
            "observedOwnerId" => new(Runtime(8, 3, OtherOwnerId), RecoveryOwnerId, OperationB, "quiesced runtime"),
            "observedOwnerKind" => new(new PersistenceAuthority(Identity(), PersistenceOwnerKind.Recovery, RuntimeOwnerId, 8, 3), RecoveryOwnerId, OperationB, "quiesced runtime"),
            "observedFence" => new(Runtime(9, 3), RecoveryOwnerId, OperationB, "quiesced runtime"),
            "recoveryOwner" => new(Runtime(8, 3), OtherOwnerId, OperationB, "quiesced runtime"),
            "reason" => new(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime."),
            _ => new(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime"),
        };

        bool equal = original.Operation.Equals(other.Operation);

        Assert.Equal(differing is "same" or "observedSequenceOnly", equal);
        if (equal)
            Assert.Equal(original.Operation.GetHashCode(), other.Operation.GetHashCode());
    }

    [Fact]
    public void RecoverOperation_SeparatesTheObservedPreviousOwnerFromTheNewAdminOwner()
    {
        OperationIdentity operation = new RecoverAndLoadRequest(Runtime(8, 3), RecoveryOwnerId, OperationB, "quiesced runtime").Operation;

        Assert.Equal(RecoveryOwnerId, operation.OwnerId);
        Assert.Equal(PersistenceOwnerKind.Runtime, operation.ExpectedOwnerKind);
        Assert.Equal(RuntimeOwnerId, operation.ExpectedOwnerId);
        Assert.Equal(8, operation.ExpectedFence);
        Assert.Equal(0, operation.Sequence);
        Assert.Equal("quiesced runtime", operation.Reason);
        Assert.Null(operation.SafeDefaults);
        Assert.Null(operation.Safe);
        Assert.Null(operation.IntendedClass);
    }

    [Fact]
    public void RuntimeOperations_LeaveFieldsOfOtherKindsEmpty()
    {
        OperationIdentity acquire = new AcquireAndLoadRequest(Identity(), OperationA, RuntimeOwnerId, 7, CharacterClass.Knight, Defaults()).Operation;
        OperationIdentity checkpoint = new WriteSafeCheckpointRequest(Runtime(8, 0), 1, OperationA, Token(1), null, KnightSafe()).Operation;
        OperationIdentity release = new ReleaseRequest(Runtime(8, 1), 2, OperationA).Operation;

        Assert.Equal(CharacterClass.Knight, acquire.IntendedClass);
        Assert.Equal(Defaults(), acquire.SafeDefaults);
        Assert.Null(acquire.ExpectedCharacterToken);
        Assert.Null(acquire.Safe);
        Assert.Null(acquire.ExpectedOwnerKind);
        Assert.Null(acquire.Reason);

        Assert.Null(checkpoint.IntendedClass);
        Assert.Null(checkpoint.SafeDefaults);
        Assert.Equal(Token(1), checkpoint.ExpectedCharacterToken);
        Assert.Null(checkpoint.ExpectedProgressToken);
        Assert.Equal(KnightSafe(), checkpoint.Safe);
        Assert.Null(checkpoint.ExpectedOwnerId);
        Assert.Null(checkpoint.Reason);

        Assert.Null(release.IntendedClass);
        Assert.Null(release.SafeDefaults);
        Assert.Null(release.ExpectedCharacterToken);
        Assert.Null(release.ExpectedProgressToken);
        Assert.Null(release.Safe);
        Assert.Null(release.ExpectedOwnerKind);
        Assert.Null(release.Reason);
    }

    [Fact]
    public void CheckpointOperation_KeepsItsTokensWhenCallerArraysChangeLater()
    {
        byte[] characterBytes = { 1, 2, 3, 4, 5, 6, 7, 8 };
        WriteSafeCheckpointRequest request = new(Runtime(8, 0), 1, OperationA, new PersistenceToken(characterBytes), null, KnightSafe());
        OperationIdentity untouched = new WriteSafeCheckpointRequest(
            Runtime(8, 0), 1, OperationA, new PersistenceToken(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }), null, KnightSafe()).Operation;

        characterBytes[0] = 0xFF;
        byte[] exposed = request.ExpectedCharacterToken.ToArray();
        exposed[1] = 0xEE;

        Assert.Equal(untouched, request.Operation);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, request.Operation.ExpectedCharacterToken!.ToArray());
    }
}
