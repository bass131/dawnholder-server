# Command

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 입력·행동을 큐에 저장하거나 다시 실행할 때.
- **핵심:** 호출 대상과 요청 데이터를 분리하면 입력 장치·AI·봇이 같은 행동 경로를 사용할 수 있다.
- **주의:** 명령 생성과 실행 사이에 대상이 사라질 수 있다. 대상 ID·실행 시점·취소와 소유권을 정한다. 모든 단순 호출을 객체로 바꿀 필요는 없다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/01-command.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
