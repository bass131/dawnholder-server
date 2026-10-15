namespace Dawnholder.Server.GameServer.Persistence;

/// <summary>관리자 전용 경계. Runtime API를 상속하지 않으며 복구는 진단값만 반환한다.</summary>
public interface ICharacterRecoveryRepository
{
    Task<InspectRecoveryResult> InspectRecoveryAsync(
        InspectRecoveryRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<RecoverAndLoadResult> RecoverAndLoadAsync(
        RecoverAndLoadRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<ReleaseRecoveryResult> ReleaseRecoveryAsync(
        ReleaseRecoveryRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);

    Task<ResolveRecoveryOperationResult> ResolveRecoveryOperationAsync(
        ResolveRecoveryOperationRequest request, PersistenceDeadline deadline, CancellationToken cancellationToken);
}
