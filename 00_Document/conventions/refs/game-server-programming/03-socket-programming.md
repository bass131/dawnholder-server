# 소켓 프로그래밍

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 연결·송수신·종료 코드를 바꿀 때.
- **핵심:** 부분 수신·부분 송신, 버퍼 수명, 비동기 완료와 종료 경쟁을 다룬다.
- **주의:** 헤더 검증 전에 무제한 버퍼를 할당하지 않는다. 완료 콜백 시점의 연결 상태와 재사용 여부를 확인한다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/03-socket-programming.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
