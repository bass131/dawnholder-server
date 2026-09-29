# 네트워크

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 지연·유실·처리량을 해석할 때.
- **핵심:** TCP의 바이트 스트림과 응용 패킷을 구분하고 왕복 지연·대역폭·혼잡을 별도로 측정한다.
- **주의:** 한 번의 send와 recv가 일대일이라고 가정하지 않는다. 평균 지연만으로 지터나 긴 꼬리 지연을 설명하지 않는다.
- **프로젝트 연결:** [protocol 변경 계약](../../../domains/protocol.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/02-computer-network.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
