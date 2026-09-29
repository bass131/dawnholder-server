# Event Queue

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 요청을 다른 실행 흐름이나 나중 시점에 처리할 때.
- **핵심:** 발행과 처리 시간을 분리하고 필요한 데이터를 큐에 보관한다.
- **주의:** 큐 길이·처리 예산·순서·재시도·오래된 대상을 정한다. 지연 실행 시 현재 상태를 다시 검증해야 할 수 있다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/14-event-queue.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
