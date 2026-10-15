using Dawnholder.Server.GameServer.Persistence;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Persistence;

/// <summary>
/// Literal fixture values for persistence contract tests. Every authority, token and receipt here is a
/// scripted "what the store reported" value chosen by the test; nothing is derived from product rules.
/// </summary>
static class PersistenceTestValues
{
    public static readonly Guid AccountId = new("6f1d0a52-0000-4000-8000-000000000001");
    public static readonly Guid CharacterId = new("6f1d0a52-0000-4000-8000-000000000002");
    public static readonly Guid RuntimeOwnerId = new("6f1d0a52-0000-4000-8000-000000000003");
    public static readonly Guid OtherOwnerId = new("6f1d0a52-0000-4000-8000-000000000004");
    public static readonly Guid RecoveryOwnerId = new("6f1d0a52-0000-4000-8000-000000000005");
    public static readonly Guid OperationA = new("6f1d0a52-0000-4000-8000-0000000000a1");
    public static readonly Guid OperationB = new("6f1d0a52-0000-4000-8000-0000000000b2");

    public static PersistenceIdentity Identity() => new(1, AccountId, CharacterId);

    public static PersistenceToken Token(byte last) => new(new byte[] { 0, 0, 0, 0, 0, 0, 0x10, last });

    public static SafeCheckpoint KnightSafe() => new(CharacterClass.Knight, 3.5f, 0f, 150);

    public static SafeCheckpoint KnightSafeMoved() => new(CharacterClass.Knight, 7.25f, 0f, 150);

    public static SafeDefaults Defaults() => new(3.5f, 0f, 150, 80);

    public static PersistenceAuthority Free(long fence) => new(Identity(), PersistenceOwnerKind.Free, null, fence, 0);

    public static PersistenceAuthority Runtime(long fence, long lastSequence, Guid? ownerId = null)
        => new(Identity(), PersistenceOwnerKind.Runtime, ownerId ?? RuntimeOwnerId, fence, lastSequence);

    public static PersistenceAuthority Recovery(long fence, long lastSequence = 0)
        => new(Identity(), PersistenceOwnerKind.Recovery, RecoveryOwnerId, fence, lastSequence);

    public static PersistenceObservation Observed(PersistenceAuthority authority, PersistenceToken? character, PersistenceToken? progress)
        => new(authority, character, progress);

    public static OperationReceipt AcquiredReceipt(Guid operationId, PersistenceAuthority authority, PersistenceToken character, PersistenceToken progress)
        => new(operationId, PersistenceOperationKind.Acquire, PersistenceResultCode.Acquired, authority, CharacterClass.Knight, KnightSafe(), character, progress);

    public static OperationReceipt CheckpointReceipt(
        Guid operationId,
        PersistenceResultCode code,
        PersistenceAuthority authority,
        PersistenceToken character,
        PersistenceToken progress)
        => new(operationId, PersistenceOperationKind.Checkpoint, code, authority, CharacterClass.Knight, KnightSafe(), character, progress);

    public static OperationReceipt ReleasedReceipt(Guid operationId, PersistenceAuthority freeAuthority)
        => new(operationId, PersistenceOperationKind.ReleaseRuntime, PersistenceResultCode.Released, freeAuthority, null, null, null, null);

    /// <summary>Acquired result whose current observation equals the historical receipt.</summary>
    public static AcquireAndLoadResult FreshAcquired(Guid operationId, long fence, PersistenceToken character, PersistenceToken progress)
    {
        PersistenceAuthority held = Runtime(fence, 0);
        return AcquireAndLoadResult.Acquired(
            AcquiredReceipt(operationId, held, character, progress),
            Observed(held, character, progress));
    }

    public static async Task<T> Within<T>(Task<T> task) => await task.WaitAsync(RecordingCharacterRepository.Limit);

    public static async Task Within(Task task) => await task.WaitAsync(RecordingCharacterRepository.Limit);
}
