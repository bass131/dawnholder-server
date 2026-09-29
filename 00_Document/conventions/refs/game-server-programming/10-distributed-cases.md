# 분산 사례

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 샤딩·방·지역 서버 대안을 비교할 때.
- **핵심:** 사용자 이동·공유 상태·핫스팟·배포 비용을 기준으로 경계를 비교한다.
- **주의:** 다른 서비스의 규모와 요구를 그대로 가져오지 않는다. 목표 부하와 실패 시나리오 없이 분산 요소를 추가하지 않는다.
- **프로젝트 연결:** [cross-cutting 변경 계약](../../../domains/cross-cutting.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/10-distributed-cases.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
