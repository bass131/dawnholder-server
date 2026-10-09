# 「다음 할 일」 27개 분류

[goal](goal.md) 「만들 것」 7항의 근거 표다. 대상은 `records/catalog.json`(revision `2026-10-01-r2`, asOf `2026-09-30T09:05:09Z`)의 nextSteps 27개다. 시스템에 21개, 기록에 6개가 있다. 원천은 2026-10-06 이 branch의 checkout(origin/main `9ee7ae1` 기준)에서 다시 대조했다. 승인 전 초안은 로컬 E0/`nextsteps-classification-draft.md`다.

PR2가 catalog에서 nextSteps 필드를 빼면 이 표가 27개의 처분 기록으로 남는다. 「다음 일」은 이후 [BACKLOG](../../../00_Document/operations/BACKLOG.md)와 PR3의 백로그 메뉴가 맡는다.

## 분류 기준

분류 이름은 [시스템 카드 goal의 백로그 후속 계약](../2026-10-02-system-cards/goal.md#후속-백로그--백로그-정본메뉴와-다음-일-정리)의 「완료 삭제·BACKLOG 이관·폐기」를 따른다.

| 분류 | 뜻 |
|---|---|
| 로드맵 연결 | 이미 예정된 [로드맵](../../../01_Phases/milestones/2026-09-30-contracts-persistence/roadmap.md) 단계가 맡는다. [목표 루프](../../../.agents/skills/dawnholder-goal-loop/SKILL.md)의 「이미 예정된 목표는 중복 후보로 등록하지 않는다」에 따라 BACKLOG에 다시 올리지 않는다. |
| 완료 삭제 | 기록된 goal에서 이미 끝났다. |
| BACKLOG 이관 | 아직 안 했고 맡은 단계도 없다. 메인을 거쳐 Rules에 행을 보낸다. |
| 폐기(변경 규칙 사본) | 할 일이 아니라 변경할 때 지킬 규칙이고, 같은 규칙이 정본 문서에 이미 있다. |
| 폐기(조건부 지침) | 촉발 조건이 생기기 전에는 할 일이 없는 주의이고, 같은 규칙은 정본에 없다. 경계 사실이 정본에 남아 있는지 근거 칸에 적는다. 원문은 이 표에 보존하며, 조건이 생기면 그 일을 맡는 goal의 계약에서 다룬다. |
| 폐기(사용자 결정) | 사용자 결정으로 대상이 없어졌다. |
| 애매 | 처분을 리드가 정할 수 없다. 메인을 거쳐 결정 요청으로 올린다. |

## 분류표

위치의 `S:`는 시스템, `R:`는 기록 ID다. 근거의 행 번호는 대조 당시 값이다.

| # | 위치 | 원문 요지 | 분류 | 근거 |
|---|---|---|---|---|
| 1 | S:connection | 영속 class 확정 뒤 게임 입장·공유 프로토콜 입장 계약 연결 | 로드맵 연결 P5 | 로드맵 P5 「Loading gate·권위 class·공유 프로토콜·Unity 적용」 |
| 2 | S:movement | 물리·입력 계약 변경 때 양쪽 소비자와 거부 경로 확인 | 폐기(변경 규칙 사본) | [AGENTS 공학 조건](../../../AGENTS.md#공학-조건)의 경계 입력 검증·공유 DLL 양쪽 검증, [client.md](../../../00_Document/domains/client.md) 8행 |
| 3 | S:combat | 공통 피해는 GameMap, 공격별 판정·넉백은 액션에서 수정 | 폐기(변경 규칙 사본) | [server.md](../../../00_Document/domains/server.md) 27행 `ApplyImmediateEnemyHit` 소유 경계 |
| 4 | S:combat | 계약 변경은 실제 액션·패킷 순서·사망 후속과 함께 검증 | 폐기(변경 규칙 사본) | server.md 27행 「Hit → Death → 보스 StageClear → 제거·리스폰 등록 → 처치 콜백 순서를 보존한다」 |
| 5 | S:skills | 스킬 변경 때 상태·입력 범위 거부·예측 타이머 소비자 확인 | 폐기(변경 규칙 사본) | [cross-cutting.md](../../../00_Document/domains/cross-cutting.md) 「입력 시점과 예측」 7행, AGENTS 공학 조건 |
| 6 | S:enemy-ai | AI 정책과 패킷 표현 변경 구분, 실제 적·보스 표시 확인 | 폐기(변경 규칙 사본) | server.md 「맵 패킷 표현」 31행 |
| 7 | S:character-state | 저장 대상·임시 상태를 구분해 영속 연동 | 로드맵 연결 P4~P5 | 로드맵 P4·P5, [D0 설계](../../../01_Phases/goals/2026-09-29-persistence-design/goal.md) 7행의 저장 대상 |
| 8 | S:map-entry | Ready 순서·entry 경계를 보존하며 영속 입장 계약 추가 | 로드맵 연결 P5 | 로드맵 P5 |
| 9 | S:party | UI 명령·수명 정리의 작은 목표부터 | 로드맵 연결 P1 | 로드맵 P1 「후속 조각은 착수 시 생성」 |
| 10 | S:quest | UI binding 변경에도 서버 권위·세션 reset 계약 보존 | 폐기(변경 규칙 사본) | [영역별 계약](../../../01_Phases/goals/2026-09-30-contracts-baseline/contracts.md) 15행 「세션 한정 … reset 값 갱신 뒤 알림 순서 보존」, AGENTS 공학 조건 「서버가 게임 상태와 판정을 소유한다」 |
| 11 | S:remote-rendering | 실제 원격 이동·순간 이동을 별도 플레이로 확인 | BACKLOG 이관(사용자 결정) | 아래 「사용자 결정」 1 |
| 12 | S:packet-publication | 표현 필드 변경은 PDL·버전·양쪽 소비자와 검토 | 폐기(변경 규칙 사본) | [protocol.md](../../../00_Document/domains/protocol.md) 3·7행의 변경 절차, AGENTS 공학 조건의 PDL·버전 항목 |
| 13 | S:transport | 진행 중 I/O 수명 변경은 별도 재현·계약 | 폐기(조건부 지침) | 원문은 「진행 중 I/O 수명 변경이 필요하면 별도 재현과 계약으로 다룬다」이다. 「별도 재현과 계약」을 요구하는 같은 규칙은 domains·ARCHITECTURE·CODE_CONVENTION·목표 루프에 없다. 경계 사실은 protocol.md 30행 「클라이언트 종료 정책과 진행 중 I/O의 전체 수명은 이 계약의 변경 범위가 아니다」로 남아 있다. I/O 수명 변경이 필요해지면 그 goal 계약에서 재현 방법을 정한다 |
| 14 | S:protocol | 새 입장 wire 확정 때 단일 writer가 PDL·생성물·버전·양쪽 변경 | 로드맵 연결 P5 | 로드맵 P5 「공유 프로토콜」 |
| 15 | S:persistence | 영속 단계를 설계→저장소→큐·수명→입장→종합 검증 순서로 진행 | 로드맵 연결 P2~P7 | 로드맵 P2~P7 |
| 16 | S:engineering | 평가 조건을 고정하고 실제 평가 수행 | 완료 삭제 | [기준선 goal](../../../01_Phases/goals/2026-09-30-contracts-baseline/goal.md) 19·20행 [x], T1~T6 실행·독립 채점 44/48(PR146 병합) |
| 17 | S:management-desktop | 새 경로 앱의 창 크기·클리핑 검증 | 완료 삭제 | [system-records 검증](../2026-09-30-system-records/verification.md#실제-electron-관찰)의 실제 Electron 관찰. 기본 창은 이후 [R-16](../../requirements.md#r-16)이 1600×900 DIP로 바꿨다 |
| 18 | S:management-operations | 운영툴·서버의 명령·조회·이벤트·권한·완료 의미 합의 | 로드맵 연결 P6 | 로드맵 P6. 메인 계획 35번(운영툴 큰 기능)과 겹치므로 따로 등록하지 않는다 |
| 19 | S:management-operations | 저장 완료 기반 종료는 영속 연동에 의존 | 로드맵 연결 P4·P6 | 로드맵 P6 「저장 완료 adapter는 P4에 의존」, [R-06](../../requirements.md#r-06) |
| 20 | S:management-records | 시스템 설명·공통 기록·원문 대응 독립 검증 | 완료 삭제 | [system-records 검증](../2026-09-30-system-records/verification.md#판정) PASS, Game Dev의 전투 서술 의미 검토 PASS, PR147 병합 |
| 21 | S:management-records | 항목별 서술 소유권·장기 조회 API·3D 표현 협의 | 나눔: 소유권은 폐기(사용자 결정), API는 완료 삭제, 3D는 BACKLOG 이관(사용자 결정) | 소유권: goal 「적용 중인 사용자 결정」의 기록 원본 일원화. API: [공동 조회 goal](../2026-10-01-shared-read-mcp/goal.md)의 읽기 전용 MCP(PR159). 3D: 아래 「사용자 결정」 2 |
| 22 | R:change-immediate-hit | 공통 메서드에 공격별 분기가 늘면 책임 경계 재검토 | 폐기(변경 규칙 사본) | [CODE_CONVENTION](../../../00_Document/conventions/CODE_CONVENTION.md) 9행 「변경 이유가 여러 도메인에 걸치면 책임 분리를 검토한다」 |
| 23 | R:change-interpolation | 실제 원격 플레이어·적 이동·순간 이동·맵 이동 확인 | BACKLOG 이관(사용자 결정, 11과 묶음) | 아래 「사용자 결정」 1 |
| 24 | R:decision-d0 | 기술 명세·격리 SQL·비동기 수명·입장 gate·실제 복구 순으로 진행 | 로드맵 연결 P2~P7 | 로드맵 P2~P7 |
| 25 | R:change-launcher | 새 경로 실제 실행과 1280×720·클리핑 검증 | 일부 완료 + BACKLOG 이관 | 창 확인은 #17과 같다. 실행 배치의 실제 더블클릭 성공은 [README](../../README.md) 34행에 「미검증」으로 남아 있다. 메인 계획 35번(운영툴 큰 기능)에 「실행 배치 실물 검증」이 들어 있어 대조했고, 겹치는 부분은 메인 결정 `msg_da1c987f7ff0`에 따라 별도 행으로 분리했다. 아래 「BACKLOG 이관 행」 |
| 26 | R:decision-management-backend | 운영툴·서버 계약 합의와 저장 작업 큐 연결 | 로드맵 연결 P6 | #18과 같다 |
| 27 | R:decision-record-migration | 항목별 서술 소유권 전환안 논의 | 폐기(사용자 결정) | #21의 소유권과 같다 |

## 집계

| 분류 | 개수 | 항목 |
|---|---:|---|
| 로드맵 연결 | 10 | 1, 7, 8, 9, 14, 15, 18, 19, 24, 26 |
| 폐기(변경 규칙 사본) | 8 | 2, 3, 4, 5, 6, 10, 12, 22 |
| 폐기(조건부 지침) | 1 | 13 |
| 폐기(사용자 결정) | 1 | 27 |
| 완료 삭제 | 3 | 16, 17, 20 |
| BACKLOG 이관(사용자 결정) | 2 | 11, 23(한 묶음) |
| 나눔 | 1 | 21 |
| 일부 완료 + BACKLOG 이관 | 1 | 25 |

합계는 27이다. 애매로 올린 두 묶음(#11·#23, #21의 3D)은 사용자 결정으로 BACKLOG 이관이 됐다. 초안에서 애매로 둔 #16은 기준선 goal의 실행·채점 기록을 찾아 완료 삭제로 바꿨다. #22는 조건부 지침이 아니라 CODE_CONVENTION 책임 분리 규칙의 사본으로 근거를 고쳤다. #13은 처음 「변경 규칙 사본」으로 분류했으나 PR1 문서 실사(W4)에서 인용한 protocol.md 30행이 범위 제외 문장일 뿐 같은 규칙이 아니라는 지적을 받아 「조건부 지침」으로 옮겼다.

## BACKLOG 이관 행

메인을 거쳐 Rules에 보낼 행이다. 이 문서는 BACKLOG를 직접 쓰지 않는다. 첫 행은 메인이 다음 Rules goal 입력에 넣었다(`msg_b1bf869e87fd`). 뒤의 두 행은 아래 「사용자 결정」에 따라 더했다.

| ID | 제목 | 이유 | 출처 | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `management-launcher-real-run` | 운영툴 실행 배치의 실제 더블클릭 확인 | README의 사용자 진입점이 「미검증」으로 남아 있다. 배치 분기 시험은 stub START였다 | Management 리드 제안·2026-10-06·`msg_33365ac784b2`(10:37:32Z, 같은 본문의 `msg_54535201beaa`)를 메인이 `msg_b1bf869e87fd`(10:38:19Z)로 받아 다음 Rules goal 입력에 넣음. 계획 35번과의 분리는 메인 결정 `msg_da1c987f7ff0`(10:54:00Z). 원문은 catalog nextSteps #25(`change-launcher`), 이 분류표 | 사람이 직접 더블클릭해야 한다. OS 합성 입력은 금지다 | Management | 대기 |
| `remote-play-check` | 실제 원격 플레이 확인 | 원격 보간 goal이 실서버 원격 플레이·시각 jitter·성능을 미실행으로 남겼고 이후 실행 기록이 없다 | 사용자 결정·2026-10-06·메인 전달 `msg_dfd3843e9464`, catalog nextSteps #11(`remote-rendering`)·#23(`change-interpolation`), 이 분류표 | 로드맵 P7 「게임 회귀」 단계와 겹치는지 확인 | Core | 대기 |
| `system-map-3d` | 운영툴 시스템 지도 3D 표현 | 9-30 이주 협의안의 Three.js 시스템 지도 후보가 처분 없이 남아 있다 | 사용자 결정·2026-10-06·메인 전달 `msg_dfd3843e9464`, catalog nextSteps #21(`management-records`), 이 분류표 | 색인 전환 뒤 표시할 관계 자료와 필요성 확인 | Management | 대기 |

## 사용자 결정

리드가 처분을 정하지 못한 두 건을 메인을 거쳐 물었다(`msg_33365ac784b2`, 같은 본문의 `msg_54535201beaa`). 메인 `msg_dfd3843e9464`(2026-10-06T10:40:27Z)가 전달한 사용자 원문은 다음과 같다. 메인 전달이므로 사용자 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) 원격 플레이 실제 확인 - 백로그로 옮길지 폐기할지 → A 백로그로 옮긴다 · 2) 시스템 지도 3D 표현 - 백로그로 옮길지 폐기할지 → A 백로그로 옮긴다

1. **실제 원격 플레이 확인(#11·#23).** [원격 보간 goal](../../../01_Phases/goals/2026-09-30-remote-interpolation/goal.md) 51행은 실서버 원격 플레이·시각 jitter·성능을 미실행으로 남겼다. 이후 goal에서 실행 기록을 찾지 못했다. 로드맵 P7 「게임 회귀」가 포함한다는 문장은 없다. 현황판의 A 선택지 글은 「담당 후보는 Core다. 선행 조건은 게임 회귀 단계와 겹치는지 확인하는 것이다.」였다.
2. **시스템 지도 3D 표현(#21의 3D).** 9-30 [이주 협의안](../2026-09-30-system-records/migration-consultation.md) 16행의 Three.js 시스템 지도 표현 후보다. 시스템 서술이 색인에서 빠지고 운영툴 역할이 「완성된 작업 기준과 이후 작업 정리」로 정해진 뒤 다시 확인한 적이 없었다. 현황판의 A 선택지 글은 「담당 후보는 Management다. 선행 조건은 색인 전환 뒤 표시할 관계 자료와 필요성을 확인하는 것이다.」였다.
