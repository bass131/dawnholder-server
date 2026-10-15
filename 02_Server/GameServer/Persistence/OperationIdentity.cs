using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>
/// 동일 ID의 조정에 필요한 원 typed payload를 보존한다. 시간·retry count는 payload에 포함하지 않는다.
/// 생성은 검증하는 요청 경계에 한정하며 resolver가 ID나 token만 바꿀 수 없다.
/// </summary>
public sealed class OperationIdentity : IEquatable<OperationIdentity>
{
    OperationIdentity(
        Guid operationId, PersistenceOperationKind kind, PersistenceIdentity identity,
        Guid ownerId, long expectedFence, long sequence)
    {
        ArgumentNullException.ThrowIfNull(identity);
        PersistenceContractGuard.NonEmpty(operationId, nameof(operationId));
        PersistenceContractGuard.NonEmpty(ownerId, nameof(ownerId));
        PersistenceContractGuard.NonNegative(expectedFence, nameof(expectedFence));
        PersistenceContractGuard.NonNegative(sequence, nameof(sequence));
        OperationId = operationId;
        Kind = kind;
        Identity = identity;
        OwnerId = ownerId;
        ExpectedFence = expectedFence;
        Sequence = sequence;
    }

    internal OperationIdentity(
        PersistenceIdentity identity, Guid operationId, Guid ownerId, long expectedFence,
        CharacterClass intendedClass, SafeDefaults safeDefaults)
        : this(operationId, PersistenceOperationKind.Acquire, identity, ownerId, expectedFence, 0)
    {
        PersistenceContractGuard.Defined(intendedClass, nameof(intendedClass));
        ArgumentNullException.ThrowIfNull(safeDefaults);
        IntendedClass = intendedClass;
        SafeDefaults = safeDefaults;
    }

    internal OperationIdentity(
        PersistenceAuthority authority, long sequence, Guid operationId,
        PersistenceToken expectedCharacterToken, PersistenceToken? expectedProgressToken, SafeCheckpoint safe)
        : this(operationId, PersistenceOperationKind.Checkpoint,
            RequiredAuthority(authority).Identity, authority.OwnerId ?? Guid.Empty, authority.Fence, sequence)
    {
        PersistenceContractGuard.NextSequence(authority, PersistenceOwnerKind.Runtime, sequence);
        ArgumentNullException.ThrowIfNull(expectedCharacterToken);
        ArgumentNullException.ThrowIfNull(safe);
        ExpectedCharacterToken = expectedCharacterToken;
        ExpectedProgressToken = expectedProgressToken;
        Safe = safe;
    }

    internal OperationIdentity(PersistenceAuthority authority, long sequence, Guid operationId, bool recovery)
        : this(operationId, recovery ? PersistenceOperationKind.ReleaseRecovery : PersistenceOperationKind.ReleaseRuntime,
            RequiredAuthority(authority).Identity, authority.OwnerId ?? Guid.Empty, authority.Fence, sequence)
    {
        PersistenceContractGuard.NextSequence(
            authority, recovery ? PersistenceOwnerKind.Recovery : PersistenceOwnerKind.Runtime, sequence);
    }

    internal OperationIdentity(
        PersistenceAuthority observedAuthority, Guid recoveryOwnerId, Guid operationId, string reason)
        : this(operationId, PersistenceOperationKind.Recover,
            RequiredAuthority(observedAuthority).Identity, recoveryOwnerId, observedAuthority.Fence, 0)
    {
        ArgumentNullException.ThrowIfNull(reason);
        if (reason.Length is < 1 or > 128)
            throw new ArgumentOutOfRangeException(nameof(reason), "The reason must contain 1 to 128 UTF-16 code units.");
        ExpectedOwnerKind = observedAuthority.OwnerKind;
        ExpectedOwnerId = observedAuthority.OwnerId;
        Reason = reason;
        // 관찰 LastSequence는 Recover canonical payload가 아니다. 새 관리 수명은 Sequence=0이다.
    }

    public Guid OperationId { get; }
    public PersistenceOperationKind Kind { get; }
    public PersistenceIdentity Identity { get; }
    public Guid OwnerId { get; }
    public long ExpectedFence { get; }
    public long Sequence { get; }
    public CharacterClass? IntendedClass { get; }
    public SafeDefaults? SafeDefaults { get; }
    public PersistenceToken? ExpectedCharacterToken { get; }
    public PersistenceToken? ExpectedProgressToken { get; }
    public SafeCheckpoint? Safe { get; }
    public PersistenceOwnerKind? ExpectedOwnerKind { get; }
    public Guid? ExpectedOwnerId { get; }
    public string? Reason { get; }

    public bool Equals(OperationIdentity? other) =>
        other is not null && OperationId == other.OperationId && Kind == other.Kind &&
        Identity.Equals(other.Identity) && OwnerId == other.OwnerId &&
        ExpectedFence == other.ExpectedFence && Sequence == other.Sequence &&
        IntendedClass == other.IntendedClass && Equals(SafeDefaults, other.SafeDefaults) &&
        Equals(ExpectedCharacterToken, other.ExpectedCharacterToken) &&
        Equals(ExpectedProgressToken, other.ExpectedProgressToken) && Equals(Safe, other.Safe) &&
        ExpectedOwnerKind == other.ExpectedOwnerKind && ExpectedOwnerId == other.ExpectedOwnerId &&
        string.Equals(Reason, other.Reason, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is OperationIdentity other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(OperationId);
        hash.Add(Kind);
        hash.Add(Identity);
        hash.Add(OwnerId);
        hash.Add(ExpectedFence);
        hash.Add(Sequence);
        hash.Add(IntendedClass);
        hash.Add(SafeDefaults);
        hash.Add(ExpectedCharacterToken);
        hash.Add(ExpectedProgressToken);
        hash.Add(Safe);
        hash.Add(ExpectedOwnerKind);
        hash.Add(ExpectedOwnerId);
        hash.Add(Reason, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    static PersistenceAuthority RequiredAuthority(PersistenceAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        return authority;
    }
}
