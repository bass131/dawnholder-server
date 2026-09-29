# State

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 행동 조건이 여러 플래그와 분기로 얽힐 때.
- **핵심:** 서로 배타적인 상태와 전이 조건을 드러내고 진입·종료 동작을 구분한다.
- **주의:** 상태 객체를 공유할 때 엔티티별 가변 데이터를 넣지 않는다. 단순한 enum으로 충분한 흐름을 과도한 클래스 계층으로 늘리지 않는다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/06-state.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
