# 게임 네트워킹

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 예측·보간·재조정·지연 보상을 바꿀 때.
- **핵심:** 로컬 입력 반응과 서버 권위를 연결하려면 틱·스냅샷·입력 기록의 기준 시간이 일치해야 한다.
- **주의:** 지연 보상에는 허용 시간 범위가 필요하다. 모든 객체를 예측하거나 늦은 입력을 무제한 과거로 적용하지 않는다.
- **프로젝트 연결:** [client 변경 계약](../../../domains/client.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/05-game-networking.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
