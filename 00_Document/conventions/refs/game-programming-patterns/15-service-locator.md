# Service Locator

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 구현 교체가 필요한 서비스를 공통 경로로 찾을 때.
- **핵심:** 소비자와 실제 서비스 구현의 연결을 등록 지점으로 모은다.
- **주의:** 의존성과 초기화 순서를 숨기기 쉽다. 등록 누락을 조용히 무시하지 않으며 명시적인 생성자 전달과 비교한다.
- **프로젝트 연결:** [client 변경 계약](../../../domains/client.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/15-service-locator.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
