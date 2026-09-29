# Observer

참고 범위: Game Programming Patterns — Robert Nystrom. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 상태 변경을 여러 소비자에게 알릴 때.
- **핵심:** 발행자가 수신자의 구체적인 동작을 몰라도 알림을 전달하게 한다.
- **주의:** 동기 알림은 발행 흐름을 막을 수 있다. 구독 해제·수신자 수명·호출 순서·재진입을 정하고 비동기 큐와 구분한다.
- **프로젝트 연결:** [client 변경 계약](../../../domains/client.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-programming-patterns/03-observer.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
