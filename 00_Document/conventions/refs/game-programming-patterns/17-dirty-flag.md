# Dirty Flag

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 파생값을 매 변경마다 다시 계산하는 비용이 클 때.
- **핵심:** 원본 변경을 표시하고 파생값이 필요한 시점에 계산한다.
- **주의:** 모든 변경 경로에서 플래그가 설정되어야 한다. 저장 완료와 새 변경이 겹칠 때 플래그를 잘못 지우지 않도록 한다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/17-dirty-flag.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
