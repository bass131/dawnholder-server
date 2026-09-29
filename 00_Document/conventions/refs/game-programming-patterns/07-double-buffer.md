# Double Buffer

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 읽는 도중 중간 갱신 상태가 보이면 안 될 때.
- **핵심:** 현재 읽기용 상태와 다음 상태를 나눈 뒤 완료된 시점에 교체한다.
- **주의:** 버퍼 두 개만으로 스레드 안전이 보장되지 않는다. 교체 시점과 메모리 가시성, 복사 비용, 이전 버퍼의 수명을 정한다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/07-double-buffer.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
