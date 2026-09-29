# 분산 서버

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 단일 프로세스 한계를 측정하고 분리를 검토할 때.
- **핵심:** 소유권·라우팅·장애·재시도·중복 처리와 관찰 가능성을 프로세스 경계에 맞춰 설계한다.
- **주의:** 네트워크 실패를 로컬 함수 실패처럼 다루지 않는다. 현재는 단일 프로세스 구조이며 분산 구성이 구현됐다고 가정하지 않는다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/09-distributed-server.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
