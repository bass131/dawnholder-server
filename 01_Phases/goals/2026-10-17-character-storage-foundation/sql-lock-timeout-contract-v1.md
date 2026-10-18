# 잠금 대기 의미 계약 — 메인 위임 결정
기준: 사용자 위임 전달 msg_d2315f2aecbc, 사용자 위임에 따른 메인 결정 **msg_a35ecf1fc8eb**(2026-10-18T17:14:08Z). 사용자 직접 리드 지시로 격상하지 않는다.

`dh.LockAndReadAuthority`와 `dh.CharacterCatalog`의 row/table 잠금 대기는 인자와 무관하게 **2000ms 고정(상한)**이다. `sp_getapplock` 대기는 기존 `@LockTimeoutMs` 인자대로이며 1..2000 검증을 유지한다. 1..1999 입력이 row/table 잠금 대기까지 줄인다는 보장은 제공하지 않는다. 현재 제품 호출 옵션은2000이며 공개 C# API signature, fence/sequence/idempotency·원래 작업 deadline은 바꾸지 않는다.

실제 SQL17에서 변수 SET LOCK_TIMEOUT이 SQL102로 거부된 것이 수정 계기다. 상수2000 실제 적용은 LocalDB 실측으로 확인하고 대역/정적 근거와 구분한다. 기존 시험 중 더 짧은 row/table 대기를 보장한다고 단정하는 것은 전수분류·원천/새단정을 남겨 독립 검증 대상으로 삼는다. 이 문서는 구현·실측·독립 통과를 뜻하지 않는다.

대안인 caller connection의 정확 timeout 설정은 현재 쓰기/시험범위 확대를 피하기 위해 채택하지 않았다. 후속 후보: **1..1999의 짧은 row 대기가 실제로 필요해지면** 연결 경계에서 정확 timeout을 설정하는 별도 작업을 검토한다. 자동 착수하지 않는다.

API v1/v2 고정본 bytes는 유지하며 이 SQL 잠금 의미 보충이 해당 세부에 우선한다. 메인은 아침 사용자 보고에서 위임으로 정한 결정임을 명시하고 재검토 가능하게 전달한다.
