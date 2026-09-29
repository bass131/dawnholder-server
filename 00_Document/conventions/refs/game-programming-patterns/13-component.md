# Component

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 하나의 객체에 여러 도메인 책임이 섞일 때.
- **핵심:** 책임별 로직을 분리하고 상태 소유자·실행 순서·통신 방식을 명시한다.
- **주의:** 컴포넌트 수 자체가 목적은 아니다. 단순 수정에 여러 파일이 필요해지거나 순환 참조가 생기면 경계를 다시 검토한다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/13-component.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
