# Update Method

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 여러 엔티티를 매 틱 갱신할 때.
- **핵심:** 공통 루프가 각 객체의 한 단계 동작을 호출하고 지속 상태는 객체가 보유한다.
- **주의:** 실행 순서가 결과를 바꿀 수 있다. 순회 중 추가·삭제와 한 틱에 두 번 갱신되는 경우를 확인한다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/09-update-method.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
