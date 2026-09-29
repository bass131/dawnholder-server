# NoSQL

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 저장 요구와 데이터 모델을 비교할 때.
- **핵심:** 접근 패턴·일관성·원자성·운영 비용에 맞춰 저장 방식을 선택한다.
- **주의:** 데이터 종류가 게임이라는 이유만으로 NoSQL을 선택하지 않는다. 현재 프로젝트의 SQL 선택을 이 참고자료만으로 변경하지 않는다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/08-nosql.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
