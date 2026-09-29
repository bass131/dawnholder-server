# Flyweight

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 여러 엔티티가 같은 정의 데이터를 반복 보유할 때.
- **핵심:** 공통의 불변 데이터와 인스턴스별 상태를 나누고 공통 데이터만 공유한다.
- **주의:** 공유 객체에 HP·쿨다운 같은 가변 상태를 넣으면 다른 엔티티가 함께 바뀐다. 메모리 절감과 간접 참조 비용을 함께 본다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/02-flyweight.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
