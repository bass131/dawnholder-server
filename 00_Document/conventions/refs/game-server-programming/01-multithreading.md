# 멀티스레딩

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 스레드·큐·잠금 경계를 변경할 때.
- **핵심:** 상태 소유권, 원자성, 메모리 가시성은 별개다. 공유 상태 접근과 잠금 순서를 명시한다.
- **주의:** I/O를 기다리는 동안 잠금을 보유하지 않는다. 원자 연산 하나가 여러 필드의 불변 조건을 보장하지는 않는다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/01-multithreading.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
