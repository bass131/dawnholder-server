# 서버와 클라이언트

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 역할과 상태의 소유자를 정할 때.
- **핵심:** 클라이언트는 요청·표현을 맡고 서버는 공유 게임 상태와 판정을 소유한다.
- **주의:** 클라이언트가 보낸 ID·좌표·결과를 소유권 검증 없이 신뢰하지 않는다. 화면 예측과 서버 확정을 구분한다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/04-server-and-client.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
