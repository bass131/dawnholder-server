namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>여러 생성 경계가 공유하는 값·권위·결과 shape 검증만 모은다.</summary>
static class PersistenceContractGuard
{
    internal static void NonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("The identifier must not be empty.", parameterName);
    }

    internal static void Defined<T>(T value, string parameterName) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Undefined enum value.");
    }

    internal static void NonNegative(long value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, "The value must be nonnegative.");
    }

    internal static void Positive(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "The value must be positive.");
    }

    internal static void Finite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, "The coordinate must be finite.");
    }

    internal static void NextSequence(PersistenceAuthority authority, PersistenceOwnerKind kind, long sequence)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (authority.OwnerKind != kind)
            throw new ArgumentException("The authority belongs to a different owner kind.", nameof(authority));
        if (authority.LastSequence == long.MaxValue || sequence != authority.LastSequence + 1)
            throw new ArgumentOutOfRangeException(nameof(sequence), "The sequence must be the next nonoverflowing value.");
    }

    internal static void OperationDomain(OperationIdentity operation, bool recovery)
    {
        ArgumentNullException.ThrowIfNull(operation);
        KindDomain(operation.Kind, recovery);
    }

    internal static void KindDomain(PersistenceOperationKind kind, bool recovery)
    {
        bool matches = recovery
            ? kind is PersistenceOperationKind.Recover or PersistenceOperationKind.ReleaseRecovery
            : kind is PersistenceOperationKind.Acquire or PersistenceOperationKind.Checkpoint or PersistenceOperationKind.ReleaseRuntime;
        if (!matches)
            throw new ArgumentException("The operation belongs to a different repository domain.", nameof(kind));
    }

    internal static void NotApplied(PersistenceResultCode code)
    {
        Defined(code, nameof(code));
        if (code < PersistenceResultCode.Busy)
            throw new ArgumentException("A confirmed rejection cannot carry an Applied code.", nameof(code));
    }

    internal static void Receipt(
        OperationReceipt receipt, PersistenceObservation current,
        PersistenceOperationKind kind, PersistenceResultCode code)
    {
        SameIdentity(receipt, current);
        if (receipt.Kind != kind || receipt.Code != code)
            throw new ArgumentException("The receipt kind/code does not match this result.", nameof(receipt));
    }

    internal static void SameIdentity(OperationReceipt receipt, PersistenceObservation current)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(current);
        if (!receipt.Authority.Identity.Equals(current.Authority.Identity))
            throw new ArgumentException("Historical and current observations must refer to the same identity.", nameof(current));
    }

    internal static bool IsCurrent(OperationReceipt? receipt, PersistenceObservation? current) =>
        receipt is not null && current is not null &&
        receipt.Authority.Equals(current.Authority) &&
        Equals(receipt.CharacterToken, current.CharacterToken) &&
        Equals(receipt.ProgressToken, current.ProgressToken);
}
