# Object Pool

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 반복 생성·해제 비용이나 GC가 측정된 병목일 때.
- **핵심:** 객체를 재사용하되 획득·반납·초기화와 용량 초과 정책을 정한다.
- **주의:** 이전 사용자의 상태·이벤트 구독·참조를 남기지 않는다. 풀은 메모리를 계속 잡으므로 항상 할당보다 낫지는 않다.
- **프로젝트 연결:** [tooling 변경 계약](../../../domains/tooling.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/18-object-pool.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
