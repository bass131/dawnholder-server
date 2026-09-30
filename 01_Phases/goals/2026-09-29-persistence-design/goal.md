# D0 — 작은 범위의 DB 연동 설계와 구현 분할

상태: **설계·독립 리뷰 완료, PR/최신 CI/통합 준비**. [M3](../2026-09-29-refactor-regression/goal.md) PR #136 병합 후 최신 main `84d3566f3e73017bfb90b550c48477ad5f84ed63`에서 `docs/persistence-integration-design` 브랜치로 시작했다. 이번 목표는 설계이며 GameServer/SQL/프로토콜 구현·실행 검증은 미착수다. 상태·결정·결과는 이 파일에서 관리한다.

## 확정 범위와 결정 출처

**사용자 선택:** 개발 고정 계정1/캐릭터1, 최초 생성 클래스 유지, 재접속 Town 풀HP, quest/보스 해금 세션 한정. 영속 대상은 identity/class와 안전 checkpoint다. 전투 위치/rawHP/quest 저장, 주기 저장, 매 logout 동일 row UPDATE, 정식 인증/다중 캐릭터/경제 데이터는 제외한다.

**메인의 기술 기본안:**

- 메뉴와 저장 class가 다르면 저장 class를 안내하고 그 권위 class로 입장한다. 클래스 변경이 아니다.
- 동일 캐릭터의 신규 중복 접속을 거부한다. DB load 실패는 새 입장 거부이며 기존 Ready 세션은 유지한다.
- 정상 release 확인 뒤 재접속/정상 서버 재시작은 자동 load한다. crash/commit·release 미확정은 RecoveryRequired로 닫고 신뢰된 복구 절차를 요구한다. 자동 crash 복구·무손실을 약속하지 않는다.
- DB acquire+일관 load와 모든 mutation/release는 같은 캐릭터의 DB 경계에서 직렬화하고 현재 fence를 검사한다. 메모리 lease·rowversion·프로세스 종료·고정 대기는 이를 대신하지 않는다.
- 새 입장 결과로 권위 class를 먼저 확인하고 local spawn을 지연한다. 기존 EnterMap→HP→roster와 Ready barrier는 보존한다.
- 하나의 설정 가능한 전체 종료 deadline을 쓴다. 수치는 후속 측정/튜닝이며 사용자 직접 선택이나 저장 성공 보장이 아니다.

## 산출물·허용 범위·소유

- [design.md](design.md): schema 대응, DB 권위/실패·close 인계·서버/Unity 입장 계약.
- [implementation-plan.md](implementation-plan.md): D1–D4 의존성, 파일 소유 후보, 독립 TestCode·데이터 부작용.
- [MSSQL 안내](../../../00_Document/operations/MSSQL.md): 과거 snapshot/Stats/전체 저장 가정만 좁게 정정. 운영 설정/명령·001 SQL 보존.
- db_scope_review가 위 문서를 단독 작성하고 client_flow_audit가 별도 독립 설계 리뷰한다. 메인은 CURRENT/M3 결과·사용자 논의·Git/PR/실행을 관리한다. HTML은 메인 소유다.
- SQL 접속·비밀 읽기·서비스/계정/프로세스 변경, migration/데이터 쓰기, game/protocol/test 소스 변경·실행은 이번 범위가 아니다. D1–D4는 후속 계획이며 착수·실행 성공을 뜻하지 않는다.

## 완료조건

- [x] 기존 schema/접속의 과거 검증과 미구현 GameServer 연동을 분리했다.
- [x] 사용자 선택·메인 기술 기본안·후속 튜닝값을 구분했다.
- [x] DB acquire/fence/token·create unknown·safe DTO/close·입력 gate·권위 class 생성 계약을 작성했다.
- [x] D1–D4 선행 조건/소유 후보/반증 TestCode/데이터 부작용을 분할했다.
- [x] 독립 설계 리뷰의 보완과 문서 링크·범위 검토를 마쳤다.
- [ ] 설계 PR·최신 CI·승인/병합 결과를 기록했다.

## 실제 결과와 남은 작업

설계 문서를 작성하고 별도 작업자의 계약 리뷰를 통과했다. 사전 리뷰의 P2-D1(restart 권위), P2-D2(close/migration 인계), C1–C3(Character 읽기 token·initial Town·입력 gate)와 권위 클래스 생성 제안을 반영했다. 근거는 `.backups/reviews/2026-09-30-persistence-design-{proposal-v2,review}.md`와 `2026-09-30-persistence-entry-contract.md`다.

최종 독립 리뷰에서 발견한 P2 문구2건을 수정하고 재확인했다: process 재시작 때 runtime ID 숫자가 재사용될 수 있으므로 새 entity/session과 숫자 유일성을 구분했다. old generation 결과는 현재 연결을 닫지 않고 폐기/옛 자원만 정리하도록 명시했다. NoWriteNeeded의 게임 데이터 UPDATE0과 authority release transaction, D1–D4의 PR별 사용자 명시 승인도 구분했다. 잔여 P0–P2 없음(`.backups/reviews/2026-09-30-persistence-design-final-review.md`). 이는 문서 계약 검토이며 실제 장애 재현이나 실행 테스트 성공을 뜻하지 않는다.

메인의 문서 정적 확인: 7개 문서·상대 링크54개 누락0, HTML duplicate id/anchor 누락0, 실제 diff18개·CSS·JS 보존, game/test/protocol/SQL 파일 diff0(`.backups/verification/2026-09-30-persistence-design/documents.json`). HTML의 DB 흐름 추가 후 1440/430/320/768px 가로 overflow0, theme/details/print 확장·복원 통과(`.backups/report-preview/qa-design-layout.log`, `qa-results.json`). 문서 변경이므로 새 실행 TestCode는 추가하지 않았으며 D1–D4는 구현 뒤 별도 작성자가 요구사항 테스트를 수행한다.

초안의 상대 문서 링크 누락0을 읽기 전용으로 확인했다. goal/design/implementation-plan과 MSSQL의 지정 문구 쓰기를 종료하고 독립 리뷰에 인계한다. 추가 정책·API·ID·DDL을 실행 결과처럼 확정하지 않았다.

SQL/.NET 접속·repository·게임 저장/로드·장애·Unity/bot·migration·restore는 **이번 목표에서 미실행**이다. 남은 것은 설계 PR/통합이다. D1의 구체 driver/DDL/잠금·복구 권한, D3 packet ID/status/정확 UI 접점, 종료 시간값은 구현 전 기술 명세/측정 항목이며 사용자 제품 범위를 다시 미합의로 되돌리지 않는다. D1–D4 구현은 미착수이며 이번 설계 통합을 구현·외부 DB 변경 권한으로 확대하지 않는다.
