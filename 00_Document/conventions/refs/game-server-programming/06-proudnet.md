# 네트워크 미들웨어 참고

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 전송 계층과 게임 규칙의 경계를 비교할 때.
- **핵심:** 미들웨어가 제공하는 연결·메시지·동시성 기능과 게임이 책임질 권한·판정을 구분한다.
- **주의:** 현재 저장소는 자체 TCP·PDL을 사용한다. ProudNet을 설치했거나 그 기능을 그대로 제공한다고 해석하지 않는다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/06-proudnet.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
