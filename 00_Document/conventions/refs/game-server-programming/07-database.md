# 데이터베이스

참고 범위: 게임 서버 프로그래밍 교과서. 기존 독서 메모의 핵심을 정리한 설명 자료이며 프로젝트 채택 규칙은 [CODE_CONVENTION](../../CODE_CONVENTION.md)에 있다.

- **읽을 때:** 영속화·트랜잭션·복원 설계를 시작할 때.
- **핵심:** 식별자·제약·트랜잭션 경계와 실패 후 재처리를 먼저 정하고 게임 틱의 상태와 저장 상태를 구분한다.
- **주의:** DB I/O를 틱 안에서 기다리지 않는다. 큐를 쓴다는 이유만으로 유실·중복·종료 시 저장 문제가 해결되지는 않는다. 현재 main의 DB 구현 완료를 뜻하지 않는다.
- **프로젝트 연결:** [server 변경 계약](../../../domains/server.md). 구현 여부는 연결된 현재 코드에서 확인한다.

세부 예시·당시 해석은 [정비 전 메모](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/refs/game-server-programming/07-database.md)에 보존했다. 전체 패턴을 필수로 읽거나 모든 후보를 구현할 필요는 없다.
