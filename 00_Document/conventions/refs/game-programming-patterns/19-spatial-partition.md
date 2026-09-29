# Spatial Partition

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 근처 대상 검색이 실제 병목일 때.
- **핵심:** 공간 구역으로 후보 수를 줄인 뒤 실제 거리·충돌 조건을 검사한다.
- **주의:** 이동 시 구역 갱신, 경계에 걸친 객체, 밀집 구역 비용을 확인한다. 작은 개체 수에는 단순 순회가 충분할 수 있다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/19-spatial-partition.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
