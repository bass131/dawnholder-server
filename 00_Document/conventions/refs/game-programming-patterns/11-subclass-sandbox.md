# Subclass Sandbox

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 여러 행동이 제한된 공통 연산을 사용해야 할 때.
- **핵심:** 기반 클래스가 제공하는 보호된 연산으로 파생 동작의 접근 범위를 좁힌다.
- **주의:** 기반 클래스 변경이 모든 파생 클래스에 영향을 준다. 깊은 상속보다 조합이 적합한지 검토한다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/11-subclass-sandbox.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
