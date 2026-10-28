namespace Shared.Protocol;

/// <summary>
/// 캐릭터 메뉴의 공개 결과. 저장 계층의 내부 결과는 숫자 cast 대신 의미별로 매핑한다.
/// 선택 성공은 선택 ID 확인이며, 직업 권위와 실제 입장은 후속 패킷으로 확정한다.
/// </summary>
public enum CharacterMenuResult : byte
{
    Success = 0,
    InvalidInput = 1,
    // 진행 중인 원래 요청을 유지하며 새 작업을 시작하지 않는다.
    RequestInProgress = 2,
    InvalidState = 3,
    SlotOccupied = 4,
    SlotsFull = 5,
    NameTaken = 6,
    // 존재하지 않음과 다른 계정 소유를 같은 외부 결과로 표현한다.
    CharacterUnavailable = 7,
    // 다른 runtime owner를 강제로 빼앗지 않는다.
    Busy = 8,
    RecoveryRequired = 9,
    // 변경 작업의 dispatch 전 취소가 확실한 경우에만 사용한다.
    CancelledBeforeDispatch = 10,
    // 읽기 실패 또는 변경 작업 시작 전 실패가 확실한 경우에만 사용한다.
    Unavailable = 11,
    // 적용 여부가 미확정이다. 새 요청으로 변경 작업을 재생성하지 않는다.
    OutcomeUnknown = 12,
    SchemaMismatch = 13,
    // 서버가 만든 계정·작업 문맥의 입력 오류도 이 결과로 표현한다.
    IntegrityFailure = 14,
    // 같은 서버 논리 ID의 작업 종류 또는 payload가 다르다.
    OperationConflict = 15,
    // 호스트 시작 때 명시적으로 고정한 Legacy 모드에서만 사용한다.
    LegacyFlowOnly = 16
}
