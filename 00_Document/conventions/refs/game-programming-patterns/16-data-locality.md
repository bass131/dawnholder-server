# Data Locality

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 프로파일링에서 순회·메모리 접근 비용이 확인됐을 때.
- **핵심:** 함께 읽는 데이터를 가까이 두고 자주 쓰는 값과 드문 값을 나눈다.
- **주의:** 구조 변경은 가독성과 참조 안정성 비용을 만든다. 추측한 캐시 효과만으로 전체 엔티티를 재설계하지 않는다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/16-data-locality.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
