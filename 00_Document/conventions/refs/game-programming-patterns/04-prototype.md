# Prototype

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 설정이 비슷한 객체를 기준 객체에서 만들 때.
- **핵심:** 객체의 복제로 초기 구성을 재사용한다. 상속 계층 대신 데이터 차이를 표현할 수 있다.
- **주의:** 얕은 복사가 가변 하위 객체를 공유하지 않는지 확인한다. Unity prefab variant의 직렬화와 일반 객체 복제를 혼동하지 않는다.
- **프로젝트 연결:** [client 변경 계약](../../../domains/client.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/04-prototype.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
