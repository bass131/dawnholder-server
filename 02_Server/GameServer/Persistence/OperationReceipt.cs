using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>확정 Applied 이력. Authority와 token은 당시 값이며 현재 권위를 부여하지 않는다.</summary>
public sealed class OperationReceipt : IEquatable<OperationReceipt>
{
    public OperationReceipt(
        Guid operationId, PersistenceOperationKind kind, PersistenceResultCode code,
        PersistenceAuthority authority, CharacterClass? storedClass, SafeCheckpoint? safe,
        PersistenceToken? characterToken, PersistenceToken? progressToken)
    {
        PersistenceContractGuard.NonEmpty(operationId, nameof(operationId));
        PersistenceContractGuard.Defined(kind, nameof(kind));
        PersistenceContractGuard.Defined(code, nameof(code));
        ArgumentNullException.ThrowIfNull(authority);
        if (storedClass is { } characterClass)
            PersistenceContractGuard.Defined(characterClass, nameof(storedClass));

        bool validShape = kind switch
        {
            PersistenceOperationKind.Acquire =>
                code == PersistenceResultCode.Acquired &&
                authority.OwnerKind == PersistenceOwnerKind.Runtime && authority.LastSequence == 0 &&
                storedClass is not null && safe is not null && characterToken is not null &&
                safe.CharacterClass == storedClass,
            PersistenceOperationKind.Checkpoint =>
                (code is PersistenceResultCode.CheckpointApplied or PersistenceResultCode.CheckpointNoChange) &&
                authority.OwnerKind == PersistenceOwnerKind.Runtime && authority.LastSequence >= 1 &&
                storedClass is not null && safe is not null && characterToken is not null &&
                safe.CharacterClass == storedClass,
            PersistenceOperationKind.ReleaseRuntime or PersistenceOperationKind.ReleaseRecovery =>
                code == PersistenceResultCode.Released && authority.OwnerKind == PersistenceOwnerKind.Free &&
                storedClass is null && safe is null && characterToken is null && progressToken is null,
            PersistenceOperationKind.Recover =>
                authority.OwnerKind == PersistenceOwnerKind.Recovery && authority.LastSequence == 0 && safe is null &&
                (code == PersistenceResultCode.Recovered && storedClass is not null && characterToken is not null ||
                    code == PersistenceResultCode.RecoveredAbsent && storedClass is null && characterToken is null && progressToken is null),
            _ => false
        };
        if (!validShape)
            throw new ArgumentException("The Applied receipt has an incomplete or contradictory shape.");
        OperationId = operationId;
        Kind = kind;
        Code = code;
        Authority = authority;
        StoredClass = storedClass;
        Safe = safe;
        CharacterToken = characterToken;
        ProgressToken = progressToken;
    }

    public Guid OperationId { get; }
    public PersistenceOperationKind Kind { get; }
    public PersistenceResultCode Code { get; }
    public PersistenceAuthority Authority { get; }
    public CharacterClass? StoredClass { get; }
    public SafeCheckpoint? Safe { get; }
    public PersistenceToken? CharacterToken { get; }
    public PersistenceToken? ProgressToken { get; }

    public bool Equals(OperationReceipt? other) =>
        other is not null && OperationId == other.OperationId && Kind == other.Kind && Code == other.Code &&
        Authority.Equals(other.Authority) && StoredClass == other.StoredClass && Equals(Safe, other.Safe) &&
        Equals(CharacterToken, other.CharacterToken) && Equals(ProgressToken, other.ProgressToken);

    public override bool Equals(object? obj) => obj is OperationReceipt other && Equals(other);
    public override int GetHashCode() =>
        HashCode.Combine(OperationId, Kind, Code, Authority, StoredClass, Safe, CharacterToken, ProgressToken);
}
