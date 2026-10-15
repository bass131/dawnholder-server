namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>Runtime 권한만 받는 저장 경계. command 진입 뒤 불완전 응답은 Unknown으로 반환한다.</summary>
public interface ICharacterRepository
{
    Task<ReadAdmissionResult> ReadAdmissionAsync(
        ReadAdmissionRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<AcquireAndLoadResult> AcquireAndLoadAsync(
        AcquireAndLoadRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<WriteSafeCheckpointResult> WriteSafeCheckpointAsync(
        WriteSafeCheckpointRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<ReleaseResult> ReleaseAsync(
        ReleaseRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<ResolveOperationResult> ResolveOperationAsync(
        ResolveOperationRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);
}
