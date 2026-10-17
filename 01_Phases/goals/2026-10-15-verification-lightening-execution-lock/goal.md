# 검증 경량화 정본과 공용 실행 잠금

## 재개 지점

**다음 첫 일: 10-28 뒤 메인의 재개 지시를 받으면 현재 수정본을 새 계약에 고정하고, 자원 배정·새 직전 관문을 거쳐 native E 1회를 수행한 뒤 신규 독립 검증자가 강 재판정한다.** 사용자 결정 A에 따라 현재 수정 단위를 정산하고 보류했다. 새 수정자·검증자·PR·push는 열지 않으며 branch는 로컬 commit만 남긴다. push 여부는 재개 때 판단한다. PR243은 병합됐고 도구 PR은 아직 준비 전이며, 최신 독립 판정은 계속 **NOT PASS**다.

**현재 결과:** Astra의 runner/admission/README 1차 수정은 끝났고 고정 시험 41+6+11=58개가 자체 통과했다(skip 0·exit 0). 저장 실패·관문 거부 음성도 각 exit 3·명령 미시작·자기 잔여 0이다. native E는 Content Unity 관측과 메인 자원 배정 없음으로 미실행이며, 신규 독립 판정·CI도 미실행이다. 전체 계약 미완료로 `worker_done failed` 정산했으나 사용자 우선순위 보류에 따른 것이므로 제품 추가 확정 실패로 세지 않는다. 원문은 E/`pr2-fix2/report.md`·`closure.json`, 수신은 E/`laptop-resume-20261017/check-worker-closeout-wakeup.json`이다.

**정산과 유지 경계:** 해당 Task/Dispatch는 정산됐고 worker-release 뒤 정확한 작업자 pane을 닫았다(E/`pr2-fix2-release.json`·`pr2-fix2-close.json`·`pr2-fix2-after-close-terminals.json`). 리드 pane은 최종 status 뒤 메인이 닫는다. #1 확정 실패 2회·#5 1회, state-store 누적 5차/6차 전 메인 판단, 다른 산출물 3회 초과 체크포인트와 Windows transaction rename 설계를 유지한다. 유효 시계는 E/`pr2-fix2-reissue-clock-v3.json`이며 v2 변환 오류 원시는 보존한다. 작업자 완료는 기존 체크포인트 전이고, 리드의 사후 정산은 추가 제품 실행이 아니다. 도구 main 반영 전 수동 자원 조율을 유지한다.

**10-17 노트북 재개:** 메인 `msg_a6da409c8d2d`의 지시로 지정 원격 head를 확인하고 최신 main을 충돌 없이 통합했다. Rules 도구·시험·이 goal은 통합 전후 동일하며 근거는 E/`laptop-resume-20261017/integration-state.json`·`merge-main-result.json`이다. 과거 E는 이 노트북에 없어 이전을 요청했고, 메인 `msg_68bf1623f223`은 사용자 USB 이전 전까지 main 통합·재개 정리만 허용했다. 수신 원문은 같은 폴더의 `entry-mail-inbox.json`이다. 재검증 NOT PASS를 유지하고 새 Run·Task·Dispatch·수정자·검증자는 열지 않았다. 도구 main 반영 전 수동 조율을 유지하며 조건부 관리자 승인 정본 후보는 이번 범위에 넣지 않는다.

**10-17 근거 복원과 재개:** 메인 `msg_9b4fd9d60b2e`의 ZIP을 로컬에 복사해 SHA256·목록·겹침을 확인하고 Windows tar `-k`로 풀었다. 13,099파일 모두 ZIP과 hash가 같고 겹침·중복·경로 이탈·덮어쓰기는 없다(E/`laptop-import-20261017/import-preflight.json`·`import-result.json`). 최종 판정 hash도 같으며 입력 raw hash 차이 17건은 모두 노트북 checkout의 CRLF를 LF로 바꾸면 과거 hash와 일치했다. 과거 manifest를 보존하고 새 계약에는 현재 바이트를 고정한다(E/`laptop-resume-20261017/restored-inputs-check.json`·`line-ending-input-comparison.json`). 시간 상한 갱신은 사용자 결정이 아닌 메인 판단이며 원문은 같은 폴더 `check-after-import.json`이다. 새 Run의 회신 주소를 메인에 알렸고 #1 잔여·#5를 runner/admission 중심으로 수정하는 계약을 준비한다. 이전 NOT PASS는 새 독립 판정 전까지 유지한다.

**10-17 최초 계약 주입 폐기:** 입력 접수 뒤 시작이 미관측됐고 화면 기반 draft로 R-5의 전체 크기 대조 조건을 확인하지 못했다. 메인 `msg_4cc4e186ab74`의 명시 폐기 판단으로 Enter·재전송 없이 해당 Dispatch를 fence하고 정확한 pane을 닫았다. 이 폐기는 확정 실패·제품 수정 회차에 넣지 않으며 실제 수신이 확인된 새 Astra부터 60분을 센다. 같은 계약 v1·제품/시험 manifest를 보존해 신규 세션에 재발행한다. 이 행정 기록의 goal hash 차이는 별도 버전 고정 보충으로 명시하며 입력을 조용히 바꾸지 않는다. 관측·폐기 근거는 E/`pr2-fix2-start-status.json`·`pr2-fix2-before-discard-worker-read.json`·`pr2-fix2-main-discard-abandon.json`·`pr2-fix2-main-discard-close.json`이다. 시작 화면 애니메이션 원인설은 메인 추정이며 확정하지 않는다.

**10-17 재발행과 원문 대조:** 메인 `msg_80ab52be92bd` 뒤 같은 pane의 단일 placeholder 크기·동일성을 직접 확인하고 bare Enter 한 번으로 실제 시작을 관측했다. Orca 검색이 알려 준 정확한 세션 원문에서 자동 지침과 분리된 해당 Task user 입력을 찾았다. 원 v1 계약 전문은 정확히 같고 행정 JSON의 CR 28개가 LF로 바뀌어 빈 줄만 늘었으며 값은 같았다. 불일치 때 즉시 영향 수정/실행을 보류한 뒤 메인 `msg_6298739d67d1`은 이 차이를 명시하고 같은 Task·Dispatch의 기존 범위 재개를 승인했다. 원시와 전수 위치는 E/`pr2-fix2-reissue-post-submit-comparison.json`·`pr2-fix2-reissue-contract-difference-classification.json`에 보존한다. 전달 차이는 제품 결함·확정 실패·수정 회차로 세지 않는다.

**10-17 보류 처리 오류와 baseline:** 작업자가 보류 메시지를 반환한 check와 B 실행을 한 셸 호출로 묶어 메시지 해석 전에 B를 시작한 절차 오류를 보고했고 리드는 즉시 메인에 전달했다. 첫 Node 시험은 41통과·실패0·exit0이나 자기 집계 regex 오류로 하네스가 exit1 종료했고 두 후속 고정 명령은 미실행이다. 제품 수정은 없으며 원시는 E/`pr2-fix2/run/20261017T115857311Z-B/`다. 메인 `msg_c7dc9bff6f1c`은 진행 중 B의 유한 종료·원시 보존과 제품 확정 실패에서의 제외에 동의했다. 작업자 `msg_4ce3a3ce5e0a`의 읽기 전용 대기 보고와 실제 제품 diff 없음·종료 후 소유 잔여 0을 대조했다. 재개 지시는 check를 별도 호출로 해석한 뒤 다음 실행하도록 하며, 집계 오류/누락된 baseline을 먼저 보완한다.

아래 두 문단은 10-15 당시 마감 기록이며, 현재 상태는 위 10-17 정산을 따른다.

기록 시각은 `2026-10-15T07:46:59Z`(`date -u +%Y-%m-%dT%H:%M:%SZ` 실제 출력)이다. **열린 Task·Dispatch 없음**, 작업자·검증자 pane 0이다. 현재 리드 pane은 메인의 확인·종료를 기다린다. 정산 근거는 E/`session-wrapup-tasks.json`·`session-wrapup-workers.json`·`session-wrapup-pane-proof.json`이다.

최종 판정은 E/`pr2-reverify1/verdict.md`다. #2·#3·#4는 해소, P1 최종 사전 저장·저장 실패 시 미시작 조건은 충족했으며 과거 위반 이력은 보존했다. #1은 같은 결함의 잔여 경로로 확정 실패 2회째, 신규 #5는 1회째다. 기존 47개 통과와 신규 11개 중 7개 통과·4개 실패를 구분하고, 실패를 보존한 새 시험 3개 파일을 채택했다. 수정과 다음 독립 재검증은 다음 세션으로 넘긴다.

작업 공간은 `rules-active`, 현재 도구 PR 브랜치는 `feat/shared-execution-lock-20261015`다. 문서 PR 브랜치는 `docs/verification-lightening-20261015`였다. 최신 main에서 시작한 기준·현재 Run·모델 관측·receipt는 근거 폴더 E의 `lead-context.md`와 해당 진입 원시에 둔다. E는 `.backups/verification/2026-10-15-verification-lightening-execution-lock/`다.

## 적용 중인 사용자 결정

- **10-28 시연 우선 재정렬:** 메인 `msg_d069885010a1`이 전달한 사용자 원문은 「대시보드 결정 응답: 1) 10-28 시연 준비 - 길드 순환과 지금 있는 한 판 흐름의 실화면 리허설에 힘을 몰지 → A 시연 우선 재정렬」이다. 메인 pane에서 Enter 제출된 결정의 전달이며 이 pane의 사용자 직접 입력으로 격상하지 않는다. Rules는 지금 수정 단위까지만 수행하고 10-28 뒤로 보류한다. 수신 원문은 E/`laptop-resume-20261017/check-next-status-02.json`이다. 고정 입력은 작업자의 모든 쓰기 종료 뒤 해제해 이 기록을 추가했다.
- **현재 정산 방식은 메인 판단:** `msg_917222c01a43`은 Rules에 실행 창을 배정할 수 없어 native E를 열지 않고 지금 정산하도록 했다. 수정본·미실행·실패 집계·다음 첫 일을 기록하고 로컬 commit 뒤 최종 status를 보낸다. 새 독립 검증자·수정자·PR·push는 없으며 리드 pane 종료는 메인이 한다. 원문은 E/`laptop-resume-20261017/check-after-settlement-preparation.json`이다.

## 진척 단계

- [x] 이전 변경 보존
- [x] 범위 초안 작성
- [x] 사용자 범위 승인
- [x] 경량화 정본 반영
- [x] PR243 병합
- [x] 잠금 설계 확정
- [x] 선행 실패 시험
- [x] 잠금 도구 구현
- [>] 독립 강 검증 — 사용자 결정으로 10-28 뒤까지 보류
- [ ] 도구 병합
- [ ] 종료 점검과 기록

PR 생성 뒤 병합 단계 이름에 실제 PR 번호를 넣는다.

## 범위

### 만들 것

1. **실행 단계 일괄 승인 정본:** 새 계약 발행 때 빌드·시험·실제 진입의 목록·순서·입력과 자동 관문을 리드가 한 번 승인한다. 이미 발행한 계약은 남은 단계의 버전 고정 보충으로 묶을 수 있다. 관문 실패·계획 밖 실행은 공식 질문으로 올리고, 승인된 다음 단계마다 같은 허가를 다시 받지 않는다. 입력 hash·잔여 프로세스·Unity 수·직전 물리/commit 여유 등 관련 관문과 실행 근거는 보존한다.
2. **heartbeat와 goal 기록 경량화 정본:** 첫 신호는 기동 직후 보낸다. 300초 초과는 구간·이유 한 줄로 기록하고, 기존 정산 helper 집계와 메인 R-2 표본 대조를 유지한다. 다른 의심 근거가 있을 때만 해당 구간의 대화·도구 호출 원시를 추가 감사한다. goal에는 결정·단계 결과·결함과 근거 경로를 두며, 수치·handle·PID·hash 원시는 근거 폴더에 둔다. 재개 블록은 다음 첫 일을 먼저 보이고 기록 커밋은 단계가 끝날 때 묶는다. 기존 역사 기록은 고치지 않는다.
3. **공용 실행 잠금 helper:** 여러 worktree가 같은 기계의 공용 상태를 사용하는 명시적 실행 진입과 상태 조회를 제공한다. Unity는 한 번에 하나이며 대기 요청은 Content 우선, WSL 대기 요청은 Core 우선이다. 실행 중인 남의 프로세스를 선점·종료하지 않는다. Core WSL과 Unity 병행은 직전 물리 여유 6GiB와 commit 여유 4GiB를 모두 충족해야 한다. 메모리 관측 불가·잠금 충돌·소유 불명확은 실행 승인으로 바꾸지 않는다.
4. **수명과 검증:** 잠금 소유를 실제 실행 프로세스와 연결하고 정상 종료·실패·취소·소유자 소실을 구분한다. Unity import 자식은 `parentPid`로 같은 실행 묶음에 포함하고, 다른 소유자의 Unity를 같은 묶음으로 오인하지 않는다. 살아 있는 자식이 남았는데 부모의 종료만 보고 잠금을 풀지 않는다. PID 재사용·잔여 상태·동시 획득·대기/취소·잘못된 입력을 시험하고 독립 강 검증과 CI를 연결한다.

### 건드릴 곳

| PR | 허용 대상과 책임 |
|---|---|
| 1 - 검증 경량화 정본 | `.agents/skills/dawnholder-goal-loop/SKILL.md`의 계약·기록·정산, 관련 `references/orca-work.md`, `.agents/skills/dawnholder-task-context/SKILL.md`와 `references/templates.md`의 실행 승인·원시 연결, `00_Document/operations/ORCA.md`의 우편함/정산·R-2 관련 절. 중복된 상세 규칙은 정본을 참조한다. |
| 2 - 공용 실행 잠금 도구 | `99_Tools/ExecutionLock/`와 `99_Tools/ExecutionLock.Tests/`를 책임별 배치 후보로 삼는다. `99_Tools/README.md`, `00_Document/operations/DEVELOPMENT.md`, `00_Document/domains/tooling.md`, ORCA의 해당 실행 조율 절, `.github/workflows/code-rules.yml`의 도구 시험·근거 보존 단계만 연결한다. 실제 파일명/API/공용 상태 경로는 승인 후 설계 계약에서 고정한다. |
| 목표 관리 | 이 `goal.md`, `00_Document/operations/CURRENT.md`의 Rules 한 줄, 승인된 후보의 `BACKLOG.md` 상태/goal 링크, 할당된 E 근거. |

도구는 저장소 공통 식별자와 명시적 프로젝트 범위 설정으로 공용 상태를 찾도록 설계한다. worktree별 잠금을 공용 잠금이라고 보고하거나 머신 절대 경로를 제품 코드에 고정하지 않는다. 원시·잠금 상태는 Git 제외 실행 자료이며, 전역 설정을 바꾸지 않는다.

### 하지 않을 것

- 기존 PR2 병합 가드 재개·검증, 기존 병합 관문 hook/settings 변경, 다른 파트 제품 코드·Unity 자산 수정.
- 모든 셸·IDE·외부 실행을 자동 차단하는 시스템이나 기존 실행 wrapper의 일괄 개조. 도구의 명시적 진입과 관측 범위를 문서에 밝힌다.
- 게임 상태·DB·프로토콜·공유 DLL 변경, 실제 게임/DB 검증을 이 helper의 시험 성과로 보고하기.
- heartbeat 간격 변경·정산 helper/R-2 생략, 독립 세션·역할 분리·강약 등급·병합 승인 완화.
- 남의 실행·포트·컨테이너·보존 자원 종료/삭제, 전역 설정·설치·페이지 파일 변경.
- 미채택 Gardener 제안인 첫 맥락 메모 전 초기 원시 보존 위치 정비, 별도 후보 helper의 추가 구현.

### 관찰 가능한 완료조건

1. 문서 실사자가 실제 계약 작성·자동 관문 실패·heartbeat 초과 정산·재개 시나리오에서 새 규칙을 찾고, 기존 필수 대조·승인·역할 경계와 모순이 없음을 판정한다. 이 goal에서도 결정 1~3을 실제로 적용한다.
2. 서로 다른 worktree 경로에서 시작한 두 도구 진입이 하나의 공용 상태를 사용한다. Unity의 중복 admission을 막고 대기 중 Content Unity/Core WSL 우선순위를 확인한다. 실행 중 작업을 강제로 중단하지 않는다.
3. 메모리 관문 직전의 실제 관측·단위·시각·판정 근거를 저장한다. 6GiB/4GiB 병행 경계의 바로 아래·동일·바로 위와 관측 실패를 시험한다. 별도 단독 실행 임계값을 근거 없이 만들지 않는다.
4. 도구의 실제 진입점으로 무해한 자식 프로세스를 실행해 입력→잠금/관문→자식→exit/정리를 관측한다. 정상·nonzero·취소·부모/자식 수명·경합·잔여 잠금·소유 불명확을 검증하고, 다른 소유자의 자원을 건드리지 않는다. 실제 Unity import/WSL 제품 실행을 하지 않은 부분은 명시한다.
5. 신규 선행 시험의 구현 전 실패와 구현 후 같은 명령의 통과, 신규 독립 검증자의 자체 시험·실제 진입 실행·판정 원문, CI 결과가 연결된다. Linux CI의 정책/fixture 결과를 Windows 실제 자원 관측 성공으로 확대하지 않는다. 실제 환경/권한 때문에 필수 실행을 못 하면 미실행·판정 보류다.
6. 각 PR의 독립 판정과 정확 head·CI·남은 위험을 메인에 보고하고, 각 병합은 별도 사용자 명시 승인 뒤 메인이 실행한다. 도구가 main에 들어가기 전 기존 수동 조율 기준을 유지한다.

## PR 경계와 검증

- **1 - 검증 경량화 정본:** 문서 전용 PR이다. Sol xhigh 작성 → 신규 Astra xhigh 문서 실사로 내용·참조·권한 및 현실적 계약/정산 시나리오를 확인한다. 기존 helper나 hook의 코드 변경은 넣지 않는다. 파일/줄 수는 실제 diff가 생긴 뒤 원시로 저장한다.
- **2 - 공용 실행 잠금 도구:** PR1 병합 후 당시 최신 main에서 별도 브랜치를 만든다. 실행/I/O·동시성·수명·비용 관문이므로 **강**이다. 신규 Opus 선행 시험 → 신규 구현자 → 신규 Opus 강 독립 검증이며, 제품/시험 파일 소유를 분리한다. 구현 전 실패·실제 진입·CI를 생략하지 않는다.
- PR2 구현자는 **신규 Astra xhigh**다. 현행 `implementer-routing.md`의 신호2(동시성·프로세스 수명), 신호3(Windows/WSL 경계), 신호4(새 다파일 도구)에 해당한다. 메인 `msg_7fcc3dcf1084`이 첫 Sol 제안을 철회하고 이 배정을 확정했다(메인 판단).
- **R-7 비해당: 메인 판단, 이유는 시연 전 속도 우선과 강 등급 독립 검증 유지다**(`msg_7fcc3dcf1084`). 네 범주에 닿을 수 있으나 이번 goal은 한정 Fable 설계 검토 시범에 포함하지 않는다.
- 단계별 실행 목록·순서·입력·자동 관문·실행 자원·근거 경로를 계약 때 일괄 고정한다. 공용 상태 위치, 우선순위의 대기 처리, 관문 관측/실패/취소·자식 수명은 설계에서 확정한다. 다른 파트의 실제 Unity/WSL 중량 실행 권한은 별도 자원 소유 경계를 따른다.
- 도구 PR 병합·결과 기록 뒤 신규 Gardener → 독립 확인된 종료 기록 PR 하나 → 사용자/메인 종료 점검 → R-8 순서다. 첫 PR마다 Gardener를 추가하지 않는다.

## 결정과 단계 결과

- **기존 사용자 결정:** 메인 전달 `msg_b49fffe1ccaa`의 1~4 모두 A다. 계약 때 실행 단계 일괄 승인, heartbeat 초과 한 줄+기존 집계/R-2, goal 핵심 기록, 공용 잠금 도구다. 전체 원문과 상세 경계는 E/`verification-operating-simplification-original.json`에 보존했다. 메인 전달을 직접 사용자 입력으로 격상하지 않는다.
- **진입 지시:** 메인 `msg_27c6fd9cb341`이 rules-active 재사용·PR2 기계 보존·최신 main 새 브랜치·범위 초안을 지시했다. 별도 worktree 대신 재사용한 것은 두 PR·TDD·독립 강 검증의 작업량을 고려한 메인 판단이다. 기존 BACKLOG 후보는 `verification-operating-simplification-canon`이다.
- **이전 변경 보존 완료:** 기존 동결 16경로의 파일 목록·SHA256·원본 사본을 남겨 그대로 한 커밋/push에 보존하고 기존 goal에 중단 기록 한 줄을 추가했다. 원격 head와 원본 보존 대조는 E/`freeze-result.json`, 커밋/push 원시는 E/`freeze-commit.txt`·`freeze-push.txt`다. 제품 시험·독립 판정은 수행하지 않았고 PR2를 재개하지 않았다.
- **범위 승인:** 메인 `msg_7fcc3dcf1084`의 사용자 원문은 「대시보드 결정 응답: 1) 계획 검토 - Rules Rules 새 goal 범위 - 검증 경량화 규칙 문서 PR과 공용 실행 잠금 도구 PR → A 승인 (초안 msg_ddb68c0b4007)」이다. 메인이 전문·초안 hash를 대조하고 전달했다. 승인본은 E/`scope-approved-original.md`, 수신 원문은 E/`check-next-instruction.json`이다. PR2 Astra 배정과 R-7 비포함은 사용자가 함께 본 정리본에 적힌 메인 판단이며 사용자 결정으로 격상하지 않는다. PR1 문서 작업·독립 실사를 이어가고, 실사 중 PR2 선행 시험 계약의 정적 준비만 허용한다.
- **PR1 작성 단계:** 신규 Sol이 허용 5문서에 실행 단계 일괄 승인·heartbeat 정산·goal 핵심 기록과 정본 연결을 반영했다. 작성 보고·사전 메모·자체 정적 harness 원시는 E/`pr1-author/`이며 자체점검과 독립 판정을 구분한다. 쓰기 종료·정산/종료는 E/`check-author-update-02.json`·`pr1-author-release.json`·`pr1-author-close.json`, 생존 신호 정산은 E/`pr1-author-liveness-complete.json`으로 초과 구간이 없다. 신규 독립 문서 실사와 메인 R-2·별도 병합 승인은 남았다.
- **PR243 - 검증 실행 승인·정산·목표 기록 경량화:** [draft PR](https://github.com/bass131/dawnholder-server/pull/243)을 만들었다. 독립 문서 실사 전 상태이며 최종 판정·정확 head·CI는 E의 검증/승인 묶음으로 전달한다. 실사 고정 뒤 이 goal을 판정 결과 기록으로 바꾸지 않는다.
- **PR243 병합:** 신규 Astra 문서 실사 PASS·메인 R-2·정확 head CI 성공 뒤 사용자가 별도 병합 승인했고 메인이 병합했다. 승인 제출·관문 실행은 메인 `msg_4ae0c4249ce8` 전달이며, PR의 MERGED·병합 commit·시각은 E/`pr1-merge-observation.json`으로 읽기 확인했다. 판정/정산/최종 승인 묶음은 E/`pr1-review/`·`pr1-final-handoff.json`이다.
- **PR2 실행 계약:** 같은 저장소 공통 상태를 쓰는 JSON runner, Unity/WSL 대기 우선순위와 비선점, 직전 관측·병행 관문, PID/시작 identity·자식 수명과 실패/취소를 E/`pr2-execution-contract-v1.md`에 고정했다. 기본 상태는 Git common directory의 전용 Git 제외 경로이며 실제 관측과 시험용 주입을 구분한다. 대역의 무해한 Node 자식과 Windows 실제 진입의 확인 범위를 나누고, 새로운 단독 메모리 수치·전역 설정·직접 실행 전체 차단은 추가하지 않는다. 신규 Opus의 선행 RED 원시 뒤 Astra 구현을 발행한다.
- **선행 시험 단계:** 신규 Opus가 실행 계약 v1과 공식 질문/답을 연결한 v1.1 보충으로 요구 시험·fixture를 작성했다. 기준 RED는 제품 미존재로 제품 단언이 실행되지 않은 상태이며 fixture 자가점검과 구분했다. 원시는 E/`pr2-tdd/run-20261015T042311Z/`, 보고/충실도 한계는 E/`pr2-tdd/report.md`, 고정 시험은 E/`pr2-tdd-frozen-inputs.json`이다. 정산/종료와 heartbeat 초과 구간·이유 미확정 기록은 E/`pr2-tdd-release.json`·`pr2-tdd-close.json`·`pr2-tdd-liveness-accounting.md`에 둔다. 제품 보조 프로세스·spawn 직후 관측의 대역 한계는 구현자에게 알리고, 그 한계가 원인인 실패와 실제 제품 결함을 분리한다. 선행 RED는 제품 독립 PASS가 아니다.

- **구현 단계:** 신규 Astra가 공용 실행 잠금과 관련 문서·CI를 구현하고 쓰기를 종료했다. 자체 고정 시험과 무해한 Node의 실제 Windows 정상·실패·timeout 진입은 확인했으나 독립 PASS·CI는 아니다. 원문은 E/`pr2-implement/report.md`, 동결 입력은 E/`pr2-implementation-frozen-inputs.json`, 정산/종료는 E/`pr2-implement-release.json`·`pr2-implement-close.json`이다. timeout 뒤 보존 lease와 과거 원인 미확정 오류·사전 실행 근거 저장 누락은 원시 그대로 신규 독립 검증에 넘긴다. 범위 안 개발 수정 체크포인트와 메인의 회차·시간 상한은 E/`pr2-implement-operating-limit-v1.md`에 보존한다.
- **첫 독립 검증 단계:** 신규 Opus가 보고·diff·실행 원문, 기존·독립 시험과 실제 Windows 진입을 검증해 NOT PASS로 판정했다. 원문 E/`pr2-verify/verdict.md`, 실패 시험 동결 E/`pr2-verify-frozen-inputs.json`, 정산/종료 E/`pr2-verify-release.json`·`pr2-verify-close.json`을 보존한다. 메인 `msg_8bf391ee01ee`가 범위 안 신규 Astra 수정 → 신규 Opus 재판정에 동의했고 회차·시간 상한을 유지했다. 기존 자체 GREEN을 독립 통과로 취급하지 않는다.
- **사전 실행 근거 누락 P1:** 중간 실행의 하네스 해시·허용 부모 근거가 실행 뒤 저장된 실제 위반을 보존한다. 메인 `msg_8bf391ee01ee`는 사용자 결정과 구분한 메인 판단으로, 최종 수정본의 사전 관문 원시가 실행보다 먼저 저장되고 저장 실패 시 실행이 차단되는지를 새 독립 검증자가 확인해야 최종 차단 여부를 판단하도록 했다. 확인이 없으면 계속 차단이며 과거 위반의 삭제·면제는 아니다.
- **수정 1 단계:** 신규 Astra가 결함 4건과 관련 문서의 시험 명령 연결을 수정하고 쓰기를 종료했다. 고정 시험 불변·자체 통과와 무해한 Windows 정상·실패·timeout·후속 실행, P1 사전 저장과 저장 실패 음성 확인을 보고했으나 새 독립 판정은 아니다. 원문 E/`pr2-fix1/report.md`, 원시·최종 정산 E/`pr2-fix1/closure.json`, 동결 입력 E/`pr2-fix1-frozen-inputs.json`, 정산/종료 E/`pr2-fix1-release.json`·`pr2-fix1-close.json`을 새 검증에 넘긴다. 부모 PID 재사용은 현실 조건의 재사용 이후 대역 snapshot과 실제 Node를 연결한 재현이며 실제 OS PID 재사용·Unity 실행으로 확대하지 않는다. state-store는 누적 5차이고 기존 메인 상한을 유지한다.

- **수정 1 재검증 단계와 세션 마감:** 신규 Opus의 강 재검증은 NOT PASS다. #2·#3·#4는 해소했고 P1의 최종 사전 저장·저장 실패 미시작 조건은 원시로 충족했다. #1 잔여는 같은 공개 수명 계약·결함 번호를 유지해 확정 실패 2회째, 새 #5는 첫 확정 실패로 기록한다. 원문 E/`pr2-reverify1/verdict.md`, 입력 동결·시험 채택 E/`pr2-reverify1-frozen-inputs.json`, 정산/종료 E/`pr2-reverify1-release.json`·`pr2-reverify1-close.json`이다. 메인 `msg_fb60b1354d83`의 마감 지시에 따라 새 수정자·검증자·PR 준비 완료 전환은 열지 않았고 다음 세션으로 넘겼다. 생존 신호 초과와 이유 미확정은 E/`pr2-reverify1-liveness-accounting.md`에 보존한다.

- **수정 2와 사용자 우선순위 보류:** 신규 Astra가 #1 잔여·#5에 대해 runner/admission/README를 각 1차 수정했다(제품 3파일 +41/−7). 고정 시험10·state-store·observation은 불변이다. 같은 명령의 baseline 41/41·6/6·7/11에서 수정 후 41/41·6/6·11/11로 바뀌었으나 구현자 자체 실행이다. baseline 실패 중 3건은 #1/#5 제품 경로, 정상 종료 PID 재사용 1건은 실제 snapshot의 중복 PID fixture 입력으로 분류했고 원시·시험을 그대로 보존했다(E/`pr2-fix2-baseline-fixture-triage-v1.json`). D 원시는 E/`pr2-fix2/run/20261017T121426542Z-D/summary.json`이다. native E·신규 독립 강 판정/P1 확인·CI는 미실행이고 전체 완료를 차단한다. 메인 `msg_917222c01a43`에 따라 작업자를 정산·종료했으며 제품 추가 확정 실패는 없다. 생존 신호 helper는 최대 297초·초과 0이다(E/`pr2-fix2-liveness-complete.json`). 리드의 보고·diff·hash 대조는 E/`laptop-resume-20261017/settlement-context.md`·`accepted-worker-final-input-checks.json`·`accepted-worker-artifact-checks.json`이며 독립 판정이 아니다.
- **우편함 반환 분리의 메인 운영 판단:** `msg_4937c4792173`은 두 파트의 발생을 근거로 새 계약과 기존 작업자의 다음 지시에 「우편함 check·inbox의 반환은 그 결과만 받는 별도 도구 호출로 읽고 해석한 뒤에 다음 행동을 시작한다. check와 실행 명령을 한 셸 호출이나 한 batch로 묶지 않는다.」를 적용했다. 정본 반영은 보류 뒤 후속이며 이번에 규칙 파일을 고치지 않았다. 현재 작업자에게 공식 답 `msg_54ada7d8d315`로 전달했다. 작업자는 정산 중 선행 문서 patch와 후행 check를 한 batch에 넣은 추가 위반을 `msg_aa686d782d9d`로 보고했다. check 이후 실행/쓰기·launch는 없었다는 보고와 마지막 별도 check를 원문에 보존하며 제품 실패로 세지 않는다. 수신/답변 원시는 E/`laptop-resume-20261017/check-A-ack.json`·`reply-E-close-now.json`·`check-worker-closeout-wakeup.json`, 세부 절차·시계/하네스 교정은 E/`pr2-fix2/report.md`다.

## 결함과 다음 후보

첫 독립 판정 원문 E/`pr2-verify/verdict.md`와 최신 재검증 원문 E/`pr2-reverify1/verdict.md`를 함께 보존한다. 개발 중 자체 수정 회차와 확정 실패 횟수는 구분한다. 이번 수정본에 #1 잔여와 #5를 반영했지만 새 독립 판정이 없어 해소를 확정하지 않는다. 아래 판정·집계는 유지한다.

- **1 - timeout·취소 종료 정산:** 원래 정리 루프 재현은 해소했으나 자기 root 종료 직후 PID 재사용 시 종료 기록보다 identity conflict를 먼저 판정해 lease가 남는 경로가 차단이다. 같은 공개 계약·관찰 가능한 실패·결함 번호의 잔여로 확정 실패 **2회**다. 코드 원인 차이와 번호 분류 근거도 판정 원문·입력 동결 메모에 보존한다.
- **2 - 시작된 명령의 결과와 감시:** 해소. 확정 실패 이력 1회는 유지한다.
- **3 - 부모 PID 재사용의 실행 묶음 경계:** 해소. 재사용 이후 snapshot의 독립 판정이며 실제 OS PID 재사용·Unity 실행으로 확대하지 않는다. 확정 실패 이력 1회는 유지한다.
- **4 - 정상 쓰기 중 조회 일관성:** 해소. committed 조회와 손상·미완료 상태 보존을 함께 확인했다. 확정 실패 이력 1회는 유지한다.
- **5 - 다른 대기 요청의 종료 묶음 회수와 holder 결과:** 새 차단 결함이다. 대기 요청이 끝난 묶음을 먼저 회수하면 timeout·취소 holder가 계약 결과 대신 `tool-error/2 execution-state-lost`를 반환한다. 실제 Windows CLI에서도 2회 중 1회 재현했다. 확정 실패 **1회**다.

비차단 설계 관찰과 과거 원인 미확정 오류는 판정 원문에 보존하며 임의의 추가 구현으로 확대하지 않는다. 현재 범위 밖 후보는 기존 BACKLOG 정본에 두며 이 goal에 상세 후보 장부를 복제하지 않는다.

- **후속 후보:** 메인 전달 `msg_c0ce69d388b3`의 사용자 원문 「자체 오케스트레이션 검토는 동결 뒤 백로그로 넘겨줘」에 따라 10-28 기능 동결 뒤 검토 후보를 종료 기록 PR 또는 다음 정상 문서 PR에서 BACKLOG에 반영한다. 현재 착수·Orca 교체 결정·PR2 범위 확대는 아니며, 반영 뒤 ID를 메인에 회신한다.
