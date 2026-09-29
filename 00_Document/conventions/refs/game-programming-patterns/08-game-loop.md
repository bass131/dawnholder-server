# Game Loop

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 입력·시뮬레이션·렌더링의 시간 간격을 정할 때.
- **핵심:** 고정된 시뮬레이션 간격과 가변 렌더링을 분리하고 누적 시간으로 업데이트 횟수를 결정한다.
- **주의:** 밀린 시간을 무제한 따라잡으면 부하가 더 커진다. 시간 단위와 따라잡기 정책을 정하고 틱 내부 I/O 대기를 피한다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/08-game-loop.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
