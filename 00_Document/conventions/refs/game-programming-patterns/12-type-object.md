# Type Object

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 유사한 게임 종류를 데이터 차이로 표현할 때.
- **핵심:** 종류별 정의와 개체별 실행 상태를 분리해 클래스 증가를 줄인다.
- **주의:** 공유 정의를 개별 상태처럼 변경하지 않는다. 종류마다 행동이 크게 다르면 데이터만으로 표현하려고 복잡한 분기를 만들 수 있다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/12-type-object.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
