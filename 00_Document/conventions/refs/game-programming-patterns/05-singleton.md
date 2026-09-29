# Singleton

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 하나의 서비스에 어디서든 접근하려는 설계를 검토할 때.
- **핵심:** 단일 인스턴스 보장과 전역 접근은 서로 다른 요구다. 필요한 책임만 선택한다.
- **주의:** 전역 가변 상태는 테스트 순서·초기화·스레드 소유권을 숨긴다. 명시적인 전달이나 수명주기 소유자로 대체할 수 있는지 먼저 본다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/05-singleton.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
