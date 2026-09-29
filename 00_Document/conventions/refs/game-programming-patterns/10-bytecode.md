# Bytecode

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 콘텐츠 동작을 코드 재빌드 없이 구성할 필요가 있을 때.
- **핵심:** 작은 명령 집합과 실행기를 만들어 데이터를 동작으로 해석한다.
- **주의:** 명령·메모리·실행 횟수 제한과 디버깅 비용이 생긴다. 현재 저장소에 bytecode VM이 구현되어 있다는 뜻은 아니다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/10-bytecode.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
