# TDD 정본화와 작업자 임시 쓰기 경계

## 재개 지점

Rules의 목표이며 운영 셋업 2단계다. 사용자가 범위 v2를 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch·PR: 정본 PR은 [PR211](https://github.com/bass131/dawnholder-server/pull/211)(merge commit `127cc5a1`), 리드 태그 PR은 [PR212](https://github.com/bass131/dawnholder-server/pull/212)(merge commit `5a1752eb`), 수신 helper PR은 [PR213](https://github.com/bass131/dawnholder-server/pull/213)(merge commit `08028f1b`)로 병합됐다. 종료 기록은 branch `docs/tdd-canon-closeout-20261009`(base main `08028f1b`)이고 아직 PR이 없다.
- 근거 폴더 E: `.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/astra-context.md), 수신 helper PR은 [pr2/astra-context.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/pr2/astra-context.md), 승인 범위는 [scope-draft-v2.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/scope-draft-v2.md)(SHA256 `b93b6dbd…`)다. 받은 메시지 원시는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules Astra]`(PR212 병합 전에 연 세션이라 종료까지 유지, 메인 `msg_84fb999941ce`). Claude 세션 ID `64f22d75-946a-4b21-a275-d2e10254b75a`. 재진입 뒤 handle `term_73e94f29-d9a7-47bf-91d3-50f3676de874`, Run은 `run_be5206d9a2af`(run-use로 인수), 회신 주소는 `run:run_be5206d9a2af`다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- **현재 위치**(2026-10-09T01:51:32Z): 종료 기록 좁힌 재실사가 통과했다(아래 「종료 기록 문서 실사」). 종료 기록 PR을 만들고 메인 승인 묶음을 보낸다.
- **다음 할 일**: 종료 기록 PR과 메인 승인 묶음 → 사용자 병합 승인 → 메인·사용자의 종료 점검(결과·남은 위험·BACKLOG·로드맵 초안) → R-8. 다음 goal은 자동 착수하지 않는다.
- 재진입: 같은 Claude 세션을 `--resume`으로 이으면 handle이 바뀐다. 같은 Run을 run-use로 인수하고 메인에 새 handle을 알린다. 이 세션은 `[Rules Astra]`를 유지하고, 새로 연 리드 세션은 `[Rules 리드 Opus]`를 쓴다.
- 주의: rules-active에는 직전 goal의 분리 시험이 남긴 worktree 설정 세 줄이 있다. 사용자 결정 「8번 키 남김」으로 유지하며 원격 반영은 deploy key(SSH)로 나간다.
- 직전 goal 기록: [hook 차단 줄이기와 보조 세션 스킬](../2026-10-07-hook-friction-helper-session/goal.md)은 PR210 병합(`c35279bc`)으로 끝났다. 그 goal의 「현재 위치」·「다음 할 일」은 기록 PR 재실사 전 시점으로 남았다. 끝난 goal은 고치지 않으므로 그 사실만 여기 적는다(메인 진입 지시 `msg_83e2bc491fda`, 이전 리드 인계 `msg_4b8b51290c62` 2항).

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 PR 작성·실사
- [x] PR211 병합
- [x] 리드 태그 PR 실사
- [x] 리드 태그 PR 병합
- [x] 수신 helper 선행 시험
- [x] 수신 helper 구현·검증
- [x] PR213 병합
- [x] 결과 기록·Gardener
- [>] 종료 기록 PR 병합
- [ ] 종료 점검과 R-8

PR 단계 이름은 PR이 생기면 「PR### 병합」으로 바꾼다.

## 범위

승인 범위는 초안 v2다. 넣는 기준은 (가) 운영 셋업 2단계 씨앗, (나) 이전 사용자 결정이 「다음 Rules 운영 goal」로 지정한 것, (다) 이번 사용자 추가 요청, (라) 리드 판단으로 지금 하지 않으면 비용이 커지는 것(사용자가 Q4·Q5로 승인)이다. 항목별 원천·문제·빼면 생기는 일은 v2의 표에 있다.

### 만들 것

정본 PR(문서)

1. TDD 정본화(가): goal-loop 「구현·검증·수정」에 한 문단. 코드(제품·도구·hook·helper)를 바꾸는 새 goal은 신규 Opus 선행 시험 작성자가 요구사항 시험과 구현 전 실패 원시를 두고, 쓰기 종료 확인 뒤 새 Sol이 구현해 같은 명령의 통과 원시를 남기고, 시범 배정의 신규 검증자가 독립 판정한다. 선행 단계 실패는 제품 확정 실패로 세지 않는다. 문서만 바꾸는 goal은 대상이 아니다.
2. TDD 예외 구절(라, Q5 A): 시험으로 나타낼 수 없는 요구(화면 모양·에셋 등)는 goal 범위 절에 이유와 대신할 실측을 적는다.
3. 임시 쓰기 허용 위치(가): goal-loop 67행의 「TEMP 또는 목표 산출물 경로」를 근거 폴더 아래로 고치고, 작업 맥락 양식 「위임 계약」에 허용 쓰기 위치 줄을 둔다.
4. 작업자 기동 때 TEMP 지정(가, Q1 A): ORCA R-5 2단계에 Claude 작업자의 TEMP·TMP를 짧은 임시 경로로 지정해 여는 구절. 리드의 기동 시험 결과로 문안을 정한다.
5. 자기 쓰기 감사 줄(가): 양식 「맥락 메모」 준수 연결과 「검증 판정」 결론에 이 세션이 쓴 경로와 허용 밖 쓰기를 하나씩 위반으로 나열하는 칸.
6. 기록 시각 양식(나): 양식 머리말과 「맥락 메모」 시각 칸에 `date -u` 출력을 옮기고 어림하지 않는다는 문장.
7. 허용 실행의 읽기 기본값(나): 양식 「위임 계약」 허용 실행 칸에 읽기 전용 파일 읽기 기본값.
8. CURRENT 줄 사이 띄우기(라, Q4 A): 파트마다 goal 링크·경로·branch를 한 줄로 모으고 빈 줄로 띄운다. goal-loop 16행 표현을 맞춘다. 다른 파트 줄은 현재 값을 그대로 옮긴다.
9. BACKLOG: `new-goal-tdd-canon-link`·`record-timestamp-from-clock` 승격, `contract-context-check`·`goal-state-drift`·`credential-separation-followup` 출처 덧붙임, BACKLOG ID 검사 helper 새 행.
10. goal 시작 기록: 이 문서와 CURRENT의 Rules 줄.

리드 태그 PR(문서, 다, Q2 A·Q3 A)

1. AGENTS 태그 목록·모델 라우팅의 리드 태그를 `[<파트> 리드 Opus]`로 바꾸고 「모델 계열이 바뀌면 태그도 바뀐다」를 둔다. Sol·검증자 태그는 그대로다.
2. 전환 규칙은 AGENTS의 Core 태그 전환과 같은 모양이다. 이 리드는 goal이 끝날 때까지 `[Rules Astra]`를 쓴다.
3. 산문 호칭 「리드 Astra」·「Astra」를 「리드」로 바꾼다. 앵커 id는 그대로 둔다.
4. CLAUDE.md는 바꿀 줄 목록과 문안을 먼저 메인에 보내고 메인이 이 branch에 쓴다. 리드가 커밋한다(M1).
5. `05_Management/RESUME.md`는 Management 리드와 소유를 나눈 뒤 고친다.

수신 helper PR(코드, 나)

1. `99_Tools/Orca/message-policy.mjs`의 공식 ask 예외 조건 `cliVersion === '1.4.218'`을 coordinator가 확인한 ask/reply help의 subject 옵션 부재 근거로 바꾼다. 세 identity와 질문 receipt 대조는 그대로다.
2. 선행 시험(신규 Opus)에 거절 사례 셋을 반드시 넣는다: 근거 누락, subject 옵션이 있는 help 근거, identity 불일치(M2).
3. ORCA 「활성 Dispatch 메시지 수신 보조」와 75행의 1.4.218 문장을 맞추고 `operating-reference-maintenance` 출처에 반영을 잇는다.

### 건드릴 곳

- 정본 PR: goal-loop SKILL, 작업 맥락 양식(templates), ORCA R-5, CURRENT, BACKLOG, 이 goal.
- 리드 태그 PR: AGENTS, ORCA, RESUME, goal-loop SKILL·milestones·orca-work, session-handoff SKILL, task-context SKILL·templates, REPORTING, `05_Management/RESUME.md`(조율 뒤), CLAUDE.md(메인), 이 goal.
- 수신 helper PR: `99_Tools/Orca/`, `99_Tools/Orca.Tests/`, ORCA 수신 보조, BACKLOG 한 칸, 이 goal.

### 하지 않을 것

- 정산 감사 helper(Q1 A), 기록 시각 helper. 기동 시험이 실패하거나 양식 뒤에도 같은 실수가 나면 다음 후보로 올린다.
- BACKLOG ID 검사 helper 구현. 3단계 「넘긴 후보 도착 확인」도 BACKLOG 표를 읽으므로 그 범위 때 같이 본다.
- 그물 오탐 줄이기·자격 증명 분리 후속(`credential-separation-followup`).
- 진행 중인 Core·Content·Management·CodeMap goal의 TDD 소급, 다른 파트 상태의 추정 갱신.
- 외부 팀원 태그 `[GameDesign Astra]`, 코드 주석·시험 자료의 과거 「Astra msg_…」 인용.
- Codex 작업자의 TEMP 지정. 사례 10건이 모두 Claude 세션이다.
- 직전 goal이 넘긴 다른 Rules 대기 후보 6행, 일부만 반영하는 BACKLOG 행의 나머지.
- 3단계·4단계, 현황판 코드, 전역 Claude·Codex·Git·gh 설정, 끝난 goal 문서.

### 관찰 가능한 완료조건

1. 정본 PR이 신규 `gpt-6-astra` xhigh 문서 실사를 통과한다. 현실 시나리오 셋을 대조한다: 새 코드 goal의 계약을 정본만 보고 선행 시험 → Sol → 독립 검증 순서로 쓸 수 있다. Claude 작업자 계약의 허용 쓰기·자기 쓰기 감사·기동 TEMP 지정이 서로 맞는다. 기록 시각 칸과 허용 실행의 읽기 기본값을 양식에서 바로 찾는다.
2. 리드가 R-5 새 명령으로 Claude 시험 세션 하나를 열어 임시 폴더·Git Bash `/tmp`·Write 결과가 지정 경로로 가는지와 경로 길이를 원시로 남긴다. 시험 결과 `/tmp`는 따르지 않았고, 사용자 결정(아래 「기동 TEMP 지정 구절」)으로 구절을 남기고 `/tmp`는 양식의 금지와 자기 쓰기 감사로 막는다. 처음 승인 문구 「따르지 않으면 R-5 구절을 빼고 메인에 알린다」는 이 결정으로 바뀌었다.
3. 리드 태그 PR이 문서 실사를 통과한다. 시나리오: 새 리드가 새 태그로 진입한다. 전환기 수신 측이 두 태그를 같은 파트로 인정하고 identity 대조는 계속한다. CLAUDE.md 문안과 AGENTS가 맞는다.
4. 수신 helper 선행 시험이 구현 전에 실패하고 구현 뒤 통과한다. 기존 Orca 시험이 모두 통과하고, 바뀐 기존 단정은 전수 분류 표에 남긴다. 신규 Opus 독립 검증자가 `node 99_Tools/Orca/check-message.mjs`를 이 goal에서 받은 실제 공식 ask 원시로 한 번 실행한다.
5. ORCA는 250줄 이하다. 여섯 파일 묶음(AGENTS·ORCA·goal-loop·RESUME·세션 인계 스킬·orca-work)은 PR마다 bytes가 늘지 않거나 같은 PR에서 줄인 원시 수치를 남긴다. 기준은 main `c35279bc`의 102,658 bytes다. task-context·templates·REPORTING은 묶음 밖에서 따로 잰다. CLAUDE.md는 메인 몫으로 따로 잰다.
6. 각 PR은 메인 창의 승인 줄로 병합한다.
7. 결과 기록 → Gardener → 종료 기록 PR → 종료 점검 → R-8 순서로 끝낸다. 3단계는 자동으로 시작하지 않는다.

## PR 경계와 검증

| PR | 변경 경계 | 등급과 세션 |
|---|---|---|
| 정본 | 문서만 | 문서 실사. 작성 Sol max, 실사 신규 `gpt-6-astra` xhigh(시범). R-7 비해당. 기동 시험은 리드가 R-5 절차로 한다 |
| 리드 태그 | 문서만 | 문서 실사. 작성 Sol max, 실사 신규 `gpt-6-astra` xhigh. 권한 문장은 바뀌지 않고 태그 이름만 바뀐다 |
| 수신 helper | 코드·시험·ORCA 한 절 | 강. 선행 시험 신규 Opus → 구현 Sol max → 독립 검증 신규 Opus. R-7 비해당(메인 `msg_e6c8eb971f30`) |

- 순서: 정본 → 리드 태그 → 수신 helper. rules-active 한 곳에서 branch를 동시에 둘 수 없고, 셋 다 ORCA·이 goal을 고치므로 각 branch는 앞 PR 병합 뒤 최신 main에서 만든다.
- 적용 시점: 정본의 TDD 문단은 정본 PR 병합 뒤 범위 승인을 받는 goal부터 적용한다. 진행 중인 goal은 지금 방식을 유지한다. 이 goal은 새 goal이므로 코드를 바꾸는 수신 helper PR에 선행 시험을 적용한다. 문서 PR 둘은 대상이 아니다. 임시 쓰기·기록 시각·읽기 기본값 양식은 정본 PR 병합 뒤 내는 계약부터 쓰고, 그 전의 이 goal 계약에는 같은 문장을 직접 넣는다. 새 리드 태그는 리드 태그 PR 병합 뒤 새로 여는 세션부터 쓴다.
- 규칙 문서 bytes: 늘면 같은 PR에서 지난 적용 기록 문장을 이 goal로 옮겨 상쇄한다. 리드 태그 PR에서 모자라면 실사 전에 원시 수치와 함께 메인에 올린다(메인 `msg_e6c8eb971f30` M3).

### 위험

1. 기동 때 TEMP 지정은 범위 승인 때 추론이었다. 기동 시험(아래 「기동 시험」)에서 Claude Code 임시 폴더와 `$TEMP`는 지정 경로를 따랐고, 쓴 파일 경로는 206자였다. Git Bash `/tmp`는 따르지 않았다. 처음 대응 「실패하면 구절을 뺀다」는 사용자 결정 「A 구절 남김」으로 바뀌었다. 남은 위험은 작업자가 Git Bash `/tmp`에 쓰는 것이다. 위임 계약 양식의 금지와 자기 쓰기 감사로 잡는다.
2. 기동 명령이 기본 셸 문법에 묶인다. 기동 시험에서 명령 없이 split한 pane의 기본 셸은 PowerShell이었다. 시험은 split 뒤 명령을 직접 입력했다. `--command`가 기본 셸에 타이핑된다는 것은 `orca terminal create --help`(1.4.222)의 설명이고, split에서는 아직 추론이다. 기본 셸이 바뀌면 R-5 구절의 PowerShell 문법이 맞지 않는다.
3. CURRENT 형식 변경은 CURRENT를 고친 열린 branch에 충돌을 한 번 더 만들 수 있다. 정본 PR 승인 묶음 직전에 열린 PR의 CURRENT 상태를 다시 보고 병합 순서를 메인과 정한다.
4. 태그 전환기에 두 태그가 함께 쓰인다. 현황판의 리드 인식은 메인이 고친다.
5. 병합 관문 그물: gh·merge 또는 push·main 낱말이 함께 든 명령은 막힌다. 긴 본문은 Write 도구로 파일에 쓰고 넘긴다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다.

- 진입 지시: 메인 `msg_83e2bc491fda`(2026-10-08T10:47:34Z, E/session/entry-check-msg_83e2bc491fda.json). 범위 씨앗은 직전 goal 「하지 않을 것」의 「최소 운영 셋업 2단계(검증자 임시 쓰기 경계·TDD 정본화)」, BACKLOG `new-goal-tdd-canon-link`, 메인 개인 도구의 외부 계획 9·13·15번이다.
- 범위 추가: 메인 `msg_628431238925`(10:52:43Z, E/session/wait1.raw.json). 리드 태그 변경을 별도 PR로 넣는다.
- 범위 초안 v1: 리드 `msg_4c05bcfed7d9`(11:02:00Z, E/scope-draft-v1.md). 메인 판단 `msg_e6c8eb971f30`(11:03:40Z, E/session/wait2.raw.json, 사용자 결정 아님): M1 CLAUDE.md는 메인이 리드 태그 PR branch에 직접 쓰고 리드가 커밋, 바꿀 줄 목록과 문안을 먼저 보냄. M2 수신 helper PR은 R-7 비해당, 선행 시험에 거절 사례 셋 필수, 독립 검증 신규 Opus. M3 bytes는 같은 PR에서 상쇄, 리드 태그 PR에서 모자라면 실사 전 원시 수치와 함께 메인에 올림.
- 범위 초안 v2: 리드 `msg_31e28cdb79e5`(11:09:15Z, E/scope-draft-v2.md). 사용자 수정 요청에 따라 범위 기준 절과 항목 표를 더했다. 범위와 PR 경계는 v1과 같다.
- TDD 원문: 메인 개인 도구 `main-notes/2026-10-04/HANDOFF.md` 123행 「TDD: 새 goal부터 적용한다. 진행 중인 영속화 goal은 기존 방식이다. 문구는 격차표에서 다듬는다. 「A」」. 적용 문안의 바탕은 [Content goal](../2026-10-05-items-inventory-currency/goal.md) 59행 4항이다.
- 정본 PR 승인 묶음의 메인 결정: 메인 `msg_e7dd27f2bb2d`(2026-10-08T13:21:19Z, E/session/main-decision-msg_e7dd27f2bb2d.raw.json, 사용자 결정 아님). 리드 묶음 `msg_b923e30dea54`에 대한 답이다.
  - 안건 1 A: 재실사 R1·R2(heartbeat 418·373·411초 이탈)를 비차단 운영 기록으로 둔다. 근거는 재실사 판정의 해소 조건과, heartbeat가 리드가 받는 생존 신호 규약이며 산출물에 영향이 없다는 점이다. 한계는 이탈 동안 리드가 작업자 생존을 5분 단위로 확인하지 못한 것이다. 같은 이탈이 다시 나오면 교정 층을 정한다(후보: 「장문 판정 작성 전 heartbeat 한 번」 같은 계약 문장). 이번은 첫 발생 기록이다.
  - 안건 2 A: PR211을 PR207보다 먼저 병합한다. PR207 쪽이 CURRENT 한 줄을 새 형식으로 다시 쓰고, 메인이 Management에 알린다. PR191은 3단계 뒤 재개 때 맞춘다.
- 리드 태그 PR heartbeat 재발의 메인 결정: 메인 `msg_a8e661983ce5`(2026-10-08T15:14:26Z, E/session/main-decision-msg_a8e661983ce5.raw.json, 사용자 결정 아님). 리드 요청 `msg_89901c25a9d3`·덧붙임 `msg_38c94d5b87f3`·승인 묶음 `msg_84b18a708a21`에 대한 답이다.
  - A: R1(작성 Sol 363초)·R2(실사자 337초)를 비차단 운영 기록으로 둔다. 교정은 문서보다 높은 도구 층으로 고르고, 후보를 아래 「다음 계획 후보」에 둔다. 종료 때 BACKLOG로 넘긴다.
  - 근거: 두 계약 모두 「긴 작성 직전 heartbeat 한 번」이 있었고 둘 다 지켰다. 그래도 한 번의 긴 작성이 5분을 넘었다. 같은 문장을 양식 정본으로 올리는 B로는 막지 못한다([교정 층과 반복 규칙](../../../00_Document/conventions/CODE_CONVENTION.md#교정-층과-반복-규칙)의 두 번째 발생).
  - 한계: 두 공백 동안 리드는 작업자 생존을 5분 단위로 확인하지 못했다. 산출물에는 영향이 없었다(리드 R-2, 실사 판정).

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **다음 Rules goal과 합류점**(진입 지시 `msg_83e2bc491fda`): 「대시보드 결정 응답: 1) 다음 Rules goal - 운영 셋업 2단계와 자격 증명 분리 후속 중 무엇을 먼저 → A 운영 셋업 2단계 먼저」, 「대시보드 결정 응답: 1) main 합류점 - 어느 지점에 모아서 다시 갈래를 낼지 → A 게임 재개 관문을 합류점으로」. 운영 셋업 3단계 마지막 PR 병합 커밋이 합류점이고, 그 전에는 끝난 PR만 main에 받는다. 직전 goal의 「Core·Content 재개 시점 → A 3단계 뒤 재개」가 그대로 적용된다.
- **리드 태그**(`msg_628431238925`): 「그리고 각각 리드급 세션이 본인을 Astra라고 소개하는데, 그 부분도 좀 수정해야겠다, 지시사항에 낡은게 좀 남아있나보네」. 현황판 질문 「리드 태그 - 「Astra」를 「리드」로 바꾸는 일을 지금 Rules goal에 넣을지」(A 지금 Rules goal에 별도 PR로 / B BACKLOG 후보로 미룸)에 대한 답 「A로 가되, 모델명도 같이 표기」.
- **범위 수정 요청**(`msg_73e4421772c2`, 11:05:56Z): 「대시보드 결정 응답: 1) 계획 검토 - Rules 운영 셋업 2단계 범위 승인 + Q1~Q3(TDD·임시 쓰기·리드 태그) → B 수정 요청 (초안 msg_4c05bcfed7d9), 코멘트 「범위의 기준이 좀 모호한데 더 구체적으로 설명해줄래?」」.
- **범위 승인**(`msg_8dce3f61a14c`, 11:11:21Z, E/session/wait4.raw.json): 「대시보드 결정 응답: 1) 계획 검토 - Rules 운영 셋업 2단계 범위 v2 승인 + Q1~Q5 → A 승인 (초안 msg_31e28cdb79e5)」. **Q1~Q5를 모두 A로 읽은 것은 메인 해석이다.** 현황판 항목 제목이 「범위 v2 승인 + Q1~Q5」였고 초안 추천이 다섯 다 A였다. 메인이 이 해석을 사용자에게 알렸다. Q1 A 기동 때 TEMP 지정, Q2 A 리드만 모델명, Q3 A 「Opus」, Q4 A CURRENT 띄우기 넣음, Q5 A TDD 예외 구절 넣음이다.
- **직전 goal 종료 점검**(직전 goal 「적용 중인 사용자 결정」의 「종료 점검 둘」, 메인 전달 `msg_a73d4d5bbb3c`): 교정 층 「A 도구·양식 층」(기록 시각, 위임 계약 허용 실행의 읽기 기본값)과 ⑤ 「수신 helper의 Orca 1.4.218 고정은 다음 Rules 운영 goal에서 고친다」. 그때까지 리드가 공식 ask를 손으로 대조한다.
- **기동 TEMP 지정 구절**(`msg_31c442dd7c2f`, 11:22:37Z, E/session/wait6.raw.json): 「대시보드 결정 응답: … 2) Rules - 작업자 임시 폴더 지정이 반만 먹혔는데, 운영 규칙에 그 구절을 남길지 → A 구절 남김」. 같은 제출의 1번은 Management 안건이라 메인이 생략했다. 리드 요청 `msg_d35743169f36`의 A대로 R-5 구절을 남기고 「Git Bash `/tmp`는 바뀌지 않는다」를 넣는다. 위임 계약 양식은 `/tmp`를 따로 금지하고 자기 쓰기 감사로 잡는다. 이 결정이 완료조건 2의 처음 문구를 대신한다. 메인은 lead-check.txt를 읽고 메인 셸에서도 TEMP를 바꿔도 `cygpath -w /tmp`가 그대로임을 재현했다.
- **F2 교정 층의 메인 처리**(`msg_6031c2e0ecc8`, 사용자 결정 「A 도구·양식 층」의 메인 적용): BACKLOG ID 검사 helper(백틱 유무와 무관한 ID 추출과 fixture)를 다음 Rules 운영 goal의 기록 helper 후보로 넘겼다. 메인이 넣을지 판단을 리드에 맡겼고(진입 지시), 이 goal에서는 BACKLOG 새 행으로만 둔다(범위 v2 빼는 항목 3번).

## 현재 결과

### 착수

- 진입: 2026-10-08T10:47Z 진입 지시 수신, 10:49:45Z Run 생성(E/session/run-create.json), READY `msg_cdc2ad590f91`(10:50:34Z).
- 범위 근거 시험: E/probe-current-merge/에서 `git merge-file`로 CURRENT 줄 형식을 시험했다. 빈 줄 하나로 띄운 두 줄 변경은 exit 0으로 자동 병합됐고, 붙은 두 줄 변경은 exit 1로 충돌했다.
- 리드 이탈(첫 관찰, 리드 귀속): 진입 직후 리드가 Orca 수신·가이드 원시를 시스템 TEMP에 먼저 저장했다가 근거 폴더로 복사했다. 이 goal이 다루는 임시 쓰기 사례 10번으로 범위 근거에 넣었다.
- 리드 이탈(첫 관찰, 리드 귀속): 「재개 지점」의 현재 위치 시각을 시계 출력 없이 「11:23:38Z」로 적었다가 `date -u`(11:23:16Z)를 보고 곧바로 고쳤다. 직전 goal `record-timestamp-from-clock`과 같은 실수이며 이 goal의 양식 몫이 막으려는 대상이다.
- branch: 2026-10-08T11:12:03Z 최신 main `c35279bc`에서 `docs/tdd-canon-temp-boundary-20261008`을 만들었다. goal 시작 기록은 commit `f5b2e033`이다.

### 기동 시험

- 근거: 완료조건 2. 원시는 E/launch-probe/다.
- 기동: 리드 pane을 명령 없이 split 해 기본 셸이 PowerShell(프롬프트 `PS C:\…\rules-active>`)임을 확인했다. 그 pane에 `$env:TEMP="…\rules-active\.backups\tmp\p1"; $env:TMP=$env:TEMP; …; claude --model claude-opus-5-5`를 넣었다. R-5의 `--command`가 기본 셸에 타이핑하는 것과 같은 글자다. 첫 화면은 선택창 없이 「Claude Code v2.1.293 · Opus 5.5 with xhigh effort」였다(E/launch-probe/first-screen.json). backend는 unknown이다.
- 작업: 태그 `[Rules 기동 시험 Opus]`, Task `task_1b2e9ec20194`, Dispatch `ctx_aa2942bd58f9`, receipt turnStart observed. 계약은 E/launch-probe/contract.md다. worker_done `msg_4869bf6a11d8`(11:16:24Z, succeeded)을 수신 helper가 허용했다.
- 결과:
  - 따랐다: Claude Code 임시 폴더(scratchpad)가 `.backups\tmp\p1\claude\<경로 slug>\<세션>\scratchpad`에 생겼다. Bash 안의 `$TEMP`·`$TMP`도 지정 경로였다. Write 도구로 쓴 probe.txt 절대 경로는 206자였다. Claude Code 내부 임시 파일(bash-edit-diff)도 그 아래로 갔다.
  - 따르지 않았다: Git Bash `/tmp`는 `cygpath -w` 기준 `C:\Users\bass1\AppData\Local\Temp` 그대로였다(작업자 11:16:07Z). 리드 재확인에서 `mount`가 `/tmp`를 `usertemp`로 보여 줬다(E/launch-probe/lead-check.txt, 11:17:51Z).
- 처리: 사용자 결정 「A 구절 남김」(위 「적용 중인 사용자 결정」).
- 정산: release `retained`(`external_terminal`), 빈 prompt와 「done」 표시 확인 뒤 pane을 닫았다(ptyKilled true). rules-active에는 리드만 남았다(E/launch-probe/release.json, idle.json, close.json, list-after-close.json).

### 정본 PR 작성

- 계약: v1(E/pr1-contract.md, SHA256 `c215bb21…`, 고정 HEAD `353d88ab`)과 보충 v1.1(E/pr1-contract-supplement-v1.1.md)이다. 작성자는 신규 `gpt-6.1-sol` max(태그 `[Rules Sol]`, Task `task_8a7a6a8e0665`, Dispatch `ctx_c1e8312629fb`, backend unknown)다. worker-start가 turnStart unobserved여서 draft 글자 수(계약 10,065자 + 머리 약 4,787자)를 확인하고 Enter 한 번으로 시작했다(E/pr1-draft-recovery.md).
- 공식 질문 1회(`msg_e9ea9a930108`): 새 문장만 줄여서는 묶음이 1,223 bytes 넘었다. 리드가 보충 v1.1로 답했다. 지난 적용 기록 네 문장을 아래 「정본에서 옮긴 적용 기록」으로 옮기고 ORCA 250행의 TEMP 허용을 없앴다. ORCA 250행은 R2와 같은 규칙의 다른 자리라 범위 안 결함으로 봤다.
- 수신 helper 한계: 공식 질문을 수신 helper에 넣으면 `official-ask-proof` input-error(exit 2)였다. helper가 CLI 1.4.218에 고정돼 있기 때문이다. 리드가 손으로 대조해 출처를 인정했다(E/lead-check/pr1-ask1-manual-check.md). 이 원시는 수신 helper PR의 실제 진입 입력 후보다.
- 결과: worker_done `msg_6966e339484f`(2026-10-08T12:01:10Z, succeeded)을 수신 helper가 허용했다. 보고는 E/pr1/report.md다. 묶음은 102,658 → 102,592 bytes, ORCA는 249줄이다.
- 리드 R-2(2026-10-08T12:04:42Z): 옮긴 네 문장의 글자 일치, CURRENT 링크·경로·branch 보존, BACKLOG 바뀐 칸, bytes·줄 수가 보고와 같았다(E/lead-check/pr1-r2.md). 차단 아닌 관찰 넷은 문서 실사에 넘긴다.
- heartbeat(종료 Gardener가 찾은 누락, 리드 R-2도 놓침): 작성 Sol의 공백이 질문 답 뒤 420초(11:39:08Z → 11:46:08Z)와 774초(11:46:08Z → 11:59:02Z)였다. 이 goal의 5분 초과 가운데 가장 이르고 가장 길다. heartbeat subject는 `[Rules Sol] alive` 4통이었다(E/session/all1.raw.json).
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt 확인 뒤 pane을 닫았다(E/pr1-release.json, pr1-idle.json, pr1-close.json).

### 정본 PR 문서 실사

- 커밋: CURRENT 형식은 `d9c053a7`, 본문과 goal 기록은 `384739c5`로 나눴고 branch를 원격에 반영했다.
- 실사자: 신규 `gpt-6-astra` xhigh(태그 `[Rules 검증자]`, Task `task_af30ca5a3c50`, Dispatch `ctx_17955ac87a7e`, 첫 화면 「GPT-6-Astra xhigh」, backend unknown). 계약은 E/pr1-review-contract.md(SHA256 `184520fa…`)이고 turnStart observed였다.
- 공식 질문 1회(`msg_2d0a1460ab70`): E 밖 근거 링크의 처리와 CURRENT 비교 기준을 물었다. 리드가 손 대조 뒤 보충 v1.1로 답했다(`msg_f1130cc21cbd`, E/pr1-review-answer1.md).
- 수신 관찰: 실사자 heartbeat 세 통이 subject `[Rules 검증자] alive`와 빈 body로 왔다. identity는 맞았지만 수신 helper는 `body-tag`로 막았다. 빈 heartbeat 예외가 subject를 빈 값이나 정확한 `alive`로만 인정하기 때문이다. 처리할 내용은 없었다(E/lead-check/pr1-review-wait10.md). 수신 helper PR에서 볼 사례다.
- heartbeat(종료 Gardener가 찾은 누락, 리드 R-2도 놓침): 실사자의 heartbeat 공백이 547초(12:16:06Z → 12:25:13Z)였고, 사이 escalation 12:17:44Z부터는 449초다(E/session/all1.raw.json).
- 판정(`msg_972725b54b98`, 2026-10-08T12:28:42Z): **차단 D1·D2**(E/pr1-review/verdict.md).
  - D1(리드 기록): 「위험」 1·2번이 기동 시험 전 문구로 남았다. 실사 중 escalation `msg_a85c87acd426`으로 먼저 왔다.
  - D2(작성자 귀속, 리드 계약의 700 bytes 목표가 영향): goal-loop 「구현·검증·수정」 첫 문단에 모델 정본 링크가 없어 시나리오 S1을 그 문단과 링크만으로 따라갈 수 없다.
  - 비차단 관찰: O1 ORCA R-5 근거 링크에 앵커가 없다. O2 templates 허용 쓰기 위치 줄에서 지정 TEMP 아래 scratchpad 포함이 바로 드러나지 않는다.
  - 수치·형식·보존은 일치했다. 묶음 102,592 bytes, ORCA 249줄, CURRENT 병합 시험 충돌 0, BACKLOG ID 69·중복 0. E 밖 기존 링크 12개는 보충 v1.1대로 미검토다.
- 리드 R-2(2026-10-08T12:29:46Z): 판정 표본이 원시와 맞았다(E/lead-check/pr1-review-r2.md).
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt 확인 뒤 pane을 닫았다(E/pr1-review-release.json, pr1-review-idle.json, pr1-review-close.json).
- 처리: D1은 리드가 「위험」 1·2번을 고쳤다. D2·O1·O2는 새 Sol 세션이 고친다. 그 뒤 새 `gpt-6-astra` xhigh가 바뀐 부분을 좁혀 재실사한다. 같은 산출물의 첫 수정이다.

### 실사 결함 수정

- 계약: E/pr1-fix-contract.md(SHA256 `cdaffb6c…`, 고정 HEAD `20734f4c`). 작성자는 신규 `gpt-6.1-sol` max(태그 `[Rules Sol]`, Task `task_2308e033188f`, Dispatch `ctx_164a9f605c57`, backend unknown)이고 turnStart observed였다.
- 결과: worker_done `msg_873d5f6c4c0a`(2026-10-08T12:55:48Z, succeeded)을 수신 helper가 허용했다. 보고는 E/pr1-fix/report.md다.
  - D2: TDD 문단에서 선행 시험 작성자를 AGENTS 「모델 라우팅」으로, 검증자를 「검증자 모델 시범」으로 연결했다.
  - O2: templates 허용 쓰기 위치 줄에 지정 TEMP 아래 scratchpad가 허용 안이라고 적었다.
  - O1: ORCA R-5 구절의 goal 링크에 「기동 시험」 앵커를 붙였다.
  - bytes: 묶음 102,656(상한 102,658), ORCA 249줄이다. goal-loop 「위임 계약」의 원본 로그 줄을 줄여 상쇄했다.
- 리드 R-2(2026-10-08T12:56:30Z): 바뀐 줄 넷과 수치가 보고와 같았다(E/lead-check/pr1-fix-r2.md).
- 작업자 이탈(작성자 자기 보고. 리드가 처음 기록한 이탈이며, 원시상 첫 발생은 「정본 PR 작성」의 작성 Sol이다): heartbeat 간격이 두 번 5분을 넘었다(12:34:23Z → 12:41:21Z → 12:47:34Z). 산출물에는 영향이 없다.
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt 확인 뒤 pane을 닫았다(E/pr1-fix-release.json, pr1-fix-idle.json, pr1-fix-close.json).

### 좁힌 재실사

- 실사자: 신규 `gpt-6-astra` xhigh(태그 `[Rules 검증자]`, Task `task_47b933be78e4`, Dispatch `ctx_7e5ebddbdd28`, 첫 화면 「GPT-6-Astra xhigh」, backend unknown). 계약은 E/pr1-rereview-contract.md(SHA256 `498c12e1…`, 고정 HEAD `3d8b00da`)다. worker-start가 turnStart unobserved여서 draft 글자 수(계약 11,037자 + 머리 4,787자 = 15,824자)를 확인하고 Enter 한 번으로 시작했다(E/pr1-rereview-draft-recovery.md).
- 공식 질문 2회, 둘 다 리드가 손 대조 뒤 답했다.
  - `msg_7bc2de9cef06`: 수정 Sol heartbeat 이탈에 메인 결정이 있는지 물었다. 리드 답(`msg_868002ad8202`)은 메인 결정이 없다는 것과, 리드 해석으로는 산출물 규칙이 아닌 생존 신호 규약이라 비차단이라는 것이다. 독립적으로 차단이라 보면 근거와 함께 따로 적으라고 했다.
  - `msg_5ec97c4936eb`: 재실사자 자기 heartbeat 두 통의 원시 보존을 요청했다. 리드가 E/lead-check/pr1-rereview-heartbeats.json에 뽑아 두고 경로를 보냈다(`msg_ebc5e129a807`).
- 판정(`msg_406c94ce989e`, 2026-10-08T13:16:43Z): 문서 K1~K8은 **지적 없음**이다. D1·D2 수정과 O1·O2 반영을 확인했다. 전체 통과는 **R1·R2 메인 결정 전 차단**이다(E/pr1-rereview/verdict.md). 문서 재수정 요구는 없다.
  - R1: 수정 Sol heartbeat 간격 418초·373초(작성자 귀속).
  - R2: 재실사자 heartbeat 간격 411초(검증자 귀속).
  - 실사자는 차단 문구에 산출물 규칙만이라는 한정이 없다고 보고 리드 해석을 받지 않았다. 메인이 비차단 운영 기록으로 처리한다고 명시하면 풀린다고 적었다.
- 리드 R-2(2026-10-08T13:17:21Z): 표본이 원시와 맞았다(E/lead-check/pr1-rereview-r2.md).
- 리드 이탈(첫 관찰, 리드 귀속): 첫 질문에 답한 foreground 명령 끝에 다음 우편함 대기를 `&`로 열었다. 대기 정본은 `run_in_background`로 하나만 연다. 리드가 그 고아 대기 프로세스를 끝내고 정상 대기를 다시 열었다(E/lead-check/pr1-rereview-wait15.md).
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt 확인 뒤 pane을 닫았다(E/pr1-rereview-release.json, pr1-rereview-idle.json, pr1-rereview-close.json).
- 열린 PR의 CURRENT(위험 3): PR207(Management 소개 페이지)과 PR191(Content 인벤토리 화면)이 옛 표 형식의 CURRENT를 고친다. 정본 PR 위에 PR207을 합치는 모의 병합은 충돌 1곳이었다(E/lead-check/open-prs-current.md). 병합 순서는 메인 결정(「요구사항 원천과 적용 결정」)대로 PR211이 먼저다.
- PR과 처리: PR211을 만들었다. 메인 결정 `msg_e7dd27f2bb2d`(「요구사항 원천과 적용 결정」)로 R1·R2는 비차단 운영 기록이 됐다. 메인이 CI 뒤 사용자에게 병합 승인을 요청한다.

### PR211 병합

- 사용자 원문(메인 창 Enter 제출, 메인 전달 `msg_1d15bef8ded2`): 「병합 승인: PR211 head 2dfd7cf15aa487f7bdf8dfea63b0e7c1048e4dac」.
- 메인이 head·MERGEABLE·CLEAN을 다시 조회한 뒤 병합했다. merge commit `127cc5a197a527c437412657c8dcfa38c1354862`, mergedAt 2026-10-08T13:41:27Z다.

### 리드 태그 PR 착수

- branch: 2026-10-08T13:42:30Z 최신 main `127cc5a1`에서 `docs/lead-tag-model-name-20261008`을 만들었다.
- 조사: 범위 파일 열둘의 「Astra」를 종류별로 나눴다(태그·「리드 Astra」·앵커 id·산문, E/pr3/survey-astra.json). 도구 코드에는 리드 태그 목록이 없고 MergeGate 시험 자료 한 줄만 있다(하지 않을 것의 시험 자료 인용).
- 열린 PR207·PR191은 이 PR의 대상 파일을 건드리지 않는다(E/pr3/open-prs.json).
- M1: CLAUDE.md 26·27·28·30·37행의 바꿀 문안을 메인에 보냈다(`msg_71ec4060ed2d`, E/pr3/main-m1-claude-md.md).
- `05_Management/RESUME.md` 7·27·29행 소유 요청을 Management 리드에 보냈다(`msg_ec828ef7dbff`). 다른 Run의 리드에게는 `--run`에 상대 Run을 적어야 보내졌다(`run_not_found`·`recipient_run_mismatch` 원시 E/pr3/mgmt-ownership-receipt*.json).
- Management 동의: `msg_9918f34657f6`(13:48:29Z, E/pr3/mgmt-consent-wait24.raw.json). 세 줄만 Rules Sol이 고치고, Management는 이 PR 병합 때까지 그 파일을 쓰지 않는다. 지금 Management 리드는 끝날 때까지 옛 태그를 쓴다.
- CLAUDE.md: 메인이 이 branch에 쓰고 리드가 `b3028154`로 커밋했다(M1).

### 리드 태그 PR 작성

- 계약: 신규 `[Rules Sol]`(`gpt-6.1-sol` max)에 v1(E/pr3-contract.md, SHA256 `2daeeaa3…`, 경로 확인 E/pr3-contract-pathcheck.txt)을 냈다. Task `task_df85be9762bd`, Dispatch `ctx_cb5ce542e50a`다. `turn_start_unobserved`는 draft 크기 대조 뒤 Enter 한 번으로 풀었다(E/pr3-draft-recovery.md).
- 보충: Sol 질문 `msg_fcb32bda0afe`(bytes 초과)에 v1.1 `msg_a8bcbe4c1d0d`(E/pr3/answer1.md)로 답했다. 전환 문단 둘을 합치고, 그래도 넘으면 이관 기록을 가리키기만 하는 ORCA 문장만 옮긴다. 지금 운영 판단에 쓰는 도구 관측 문장(ORCA 90·94·132행)은 옮기지 않는다. Management 동의는 `msg_c0f7e5d3fa63`로 넘겼다.
- 결과: worker_done `msg_e3fee478e51f`(14:22:04Z, 수신 helper allowed). 열두 파일 +98/−74, 보고 E/pr3/report.md, 메모 E/pr3/context.md다. 여섯 파일 묶음은 102,656 → 102,655 bytes, ORCA는 249줄이다. ORCA 75·146·201행 끝 문장을 아래 「정본에서 옮긴 적용 기록」으로 옮겼다.
- 리드 R-2(E/lead-check/pr3-r2.md): bytes·줄 수·남은 「Astra」 분류·앵커·바뀐 범위·바뀐 줄 링크 80개·옮긴 문장을 스크립트로 다시 쟀다. 보고와 실제가 다른 곳은 없다. 실사에 넘길 관찰은 Core 전환 문구를 「종료까지」와 「Core 태그」로 줄인 두 곳이다.
- heartbeat 이탈(두 번째 발생): Sol heartbeat 간격이 14:13:08Z → 14:19:11Z, 363초였다(E/session/all2.raw.json). 메인 결정 `msg_e7dd27f2bb2d`의 「같은 이탈이 다시 나오면 교정 층을 정한다」에 닿았으므로 승인 묶음에서 메인에 올린다.
- 정산: release `retained`(`external_terminal`), tui-idle 확인 뒤 14:23:35Z에 pane을 닫았다(E/pr3-release.json, pr3-idle.json, pr3-close.json).

### 리드 태그 PR 문서 실사

- 계약: 신규 `[Rules 검증자]`(`gpt-6-astra` xhigh)에 v1(E/pr3-review-contract.md, SHA256 `dd329e22…`, 경로 확인 E/pr3-review-contract-pathcheck.txt)을 냈다. Task `task_80f54881ec9b`, Dispatch `ctx_f1c3a890c258`, 고정 HEAD `cbcf0e35`다. `turn_start_unobserved`는 draft 크기 대조(18,315 = 13,528 + 4,787) 뒤 Enter 한 번으로 풀었다(E/pr3-review-draft-recovery.md).
- 판정(E/pr3-review/verdict.md, worker_done `msg_4cd492a5fe2c` 14:51:00Z): 문서 내용·참조·권한과 시나리오 S1~S3, 정적 C1~C10은 지적 없음이다. 묶음 102,655 bytes와 ORCA 249줄을 재현했다. 새 태그 빈 heartbeat를 수신 helper에 넣은 합성 입력은 allowed였다.
- 차단 R1·R2: R1은 작성 Sol heartbeat 363초(14:13:08Z → 14:19:11Z), R2는 실사자 자신의 337초(14:40:55Z → 14:46:32Z, 사이 escalation부터 302초, E/session/all3.raw.json)다. 둘 다 장문 작성 직전 heartbeat 문장이 계약에 있었는데도 났다. 메인에 교정 층 결정을 `msg_89901c25a9d3`·덧붙임 `msg_38c94d5b87f3`로 올렸다.
- R1·R2 처리: 메인 결정 `msg_a8e661983ce5`(「요구사항 원천과 적용 결정」)로 비차단 운영 기록이 됐다. 판정은 「메인이 원문·영향·한계를 명시하면 연결해 차단을 해소할 수 있다」고 적었다. 실사 세션은 계약대로 작업 하나 뒤 닫혀 재사용하지 않으므로, 위 결정 원문·영향·한계를 이 goal에 연결해 해소한다. 문서 재수정은 없다.
- 비차단 관찰: O1 AGENTS 전환 문단의 「종료까지」를 다음 문안 정비 때 「세션 종료까지」로 밝힐지, O2 BACKLOG 65행의 「Astra 갱신 책임」 호칭, O3 다른 파트 진행 goal의 옛 세션명(소유 파트 몫, 일괄 치환 안 함). O1·O2는 아래 「다음 계획 후보」에 둔다.
- 리드 이탈(첫 관찰, 리드 귀속): 실사자 escalation에 대한 일반 접수 회신을 `reply`로 보내 subject가 `Re: [Rules 검증자] …`가 됐다(`msg_95f44ddf4517`). R-3은 공식 질문 답만 `reply`로 하고 일반 회신은 자기 태그 subject의 `send`로 보낸다. 실사자가 지시로 처리하지 않고 교정을 요청했고(`msg_cf7326e63a9b`), 리드가 `send`로 다시 보냈다(`msg_513f0a49b0d3`, E/lead-check/pr3-review-r3-deviation.md).
- 운영 관찰: 리드의 백그라운드 우편함 대기가 14:35Z쯤 메모리 부족으로 꺼졌다(E/session/wait29.raw.json 0 bytes). 다시 열지 않고 Orca 터미널 알림으로 받았다.
- 리드 R-2(E/lead-check/pr3-review-r2.md): 판정 표본 아홉(CLAUDE 기준 bytes, 옮긴 문장 행, numstat, 공백 검사, helper 입출력, BACKLOG 65행, R2 간격, 자기 쓰기 감사, 맥락 메모)이 원천과 같았다.
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt 확인 뒤 pane을 닫았다(E/pr3-review-release.json, pr3-review-idle.json, pr3-review-close.json). 첫 release는 `--run`을 붙여 `invalid_argument`로 거부됐고, 플래그를 빼고 다시 냈다(E/lead-check/pr3-review-r2.md 19행).
- PR: [PR212](https://github.com/bass131/dawnholder-server/pull/212)를 만들었다(본문 E/session/pr3-body.md). 메인 승인 묶음 `msg_84b18a708a21`에 R1·R2 교정 층 결정을 함께 올렸고, 메인이 A로 정했다. 메인은 현황판이 옛 리드 태그와 새 리드 태그를 같은 리드로 읽도록 고쳤다고 알렸다(goal 위험 4).


### PR212 병합과 재진입

- 재시작: 사용자 요청(메인 전달 `msg_e54f163b20ea`)으로 재개 지점을 커밋하고(`21be0b97`) 세션을 멈췄다. 그 커밋으로 PR212 head가 바뀌어 메인에 새 head를 알렸다(`msg_d4598a929855`). 처음 쓴 알림에 승인 문장 형식 한 줄을 넣자 병합 관문 hook이 `merge-gate:approval-injection`으로 막아 head만 적었다. 원인 조사는 하지 않는다(메인 `msg_582539756e9d`).
- 재진입: 메인이 사용자 지시 「중단된 부분부터 이어서 진행하자, 셋업 진행해줘」로 이 대화를 `--resume`으로 다시 열었다(`msg_582539756e9d`). 첫 check는 `consumer_fenced`였고 run-use로 같은 Run을 인수했다(E/session/check42.raw.json, reentry-run-use.json). Orca CLI는 1.4.223이 됐다.
- resume 태그: 리드 판단은 정본에 따로 적지 않는 것이다(`msg_842b874b65e9`). 병합 전에 연 리드 세션이 끝날 때까지만 생기는 일시적 경우이고, `--resume`은 같은 세션을 잇는 것이라 AGENTS 전환 문단의 「종료까지」로 풀린다. 메인도 Core 명칭 전환 선례처럼 보고 이 세션은 종료까지 `[Rules Astra]`를 쓴다고 정했다(`msg_84fb999941ce`).
- 병합: 사용자 원문(메인 창 Enter 제출, 메인 전달 `msg_84fb999941ce`)은 PR212 head `21be0b97ede94d77b4ad3de6002ee274bb5aab56` 승인 줄이다. 메인이 CI 네 항목 SUCCESS·CLEAN·head 일치를 다시 보고 병합했다. merge commit `5a1752ebb73df0ab84ac899812cdd4872ab469cd`, mergedAt 2026-10-08T23:04:21Z다.

### 수신 helper PR 착수

- branch: 2026-10-08T23:05:17Z 최신 main `5a1752eb`에서 `fix/receive-helper-ask-proof-20261009`를 만들었다. 리드 맥락 메모는 E/pr2/astra-context.md다.
- 인터페이스(리드 결정, 계약에 적음): `expected.officialAsk`를 `{ messageId, cliVersion, askHelp, replyHelp }`로 바꾼다. askHelp·replyHelp는 coordinator가 같은 CLI에서 받은 ask/reply `--help` 원문이고, 둘 다 `--subject`가 없을 때만 공식 ask 예외를 연다. 버전 상수를 불리언 자기 선언으로 바꾸지 않고 원문을 helper가 직접 본다.
- 실제 근거: Orca 1.4.222·1.4.223의 ask/reply help에 `--subject`가 0건이다(E/lead-check/ask-help-1.4.222.txt·ask-help-1.4.223.txt, reply-help 둘).
- 범위 메모: `99_Tools/README.md` 23행은 「건드릴 곳」의 `99_Tools/Orca/` 밖이지만 같은 인터페이스 설명이라 함께 맞춘다. 태그 붙은 subject heartbeat는 정본대로 내용 있는 heartbeat라 동작을 바꾸지 않는다.

### 수신 helper 선행 시험

- 계약: 신규 `[Rules 검증자]`(`claude-opus-5-5`, R-5대로 TEMP·TMP를 `.backups/tmp/pt/`로 지정)에 v1(E/pr2-tdd-contract.md, SHA256 `9bd7f896…`)을 냈다. Task `task_315c27807b1b`, Dispatch `ctx_a38458d832d0`이다. 첫 화면은 선택창 없이 「Claude Code v2.1.295 · Opus 5.5 with xhigh effort」였고 TEMP 아래에 Claude 임시 폴더가 생겼다.
- 결과(worker_done `msg_2678f30b5330`, E/pr2/tdd/report.md): 새 시험 4개를 더하고 기존 공식 ask 시험 4개의 근거 입력을 새 모양으로 옮겼다. 1.4.219 거절 단정 1개는 「cliVersion은 상수와 비교하지 않는다」에 따라 지웠다. 바뀐 기존 단정은 전수 분류 표에 있다. 같은 명령이 기준선 22/22에서 26개 중 19 통과·7 실패(exit 1)로 바뀌었다. 7개 모두 1.4.218 상수가 새 근거를 거절한 요구 부재였다. 커밋 `40e5fcf6`.
- 리드 R-2(E/lead-check/pr2-tdd-r2.md): 같은 명령으로 19/7을 재현했다. 리드 첫 재현은 TEMP 폴더를 만들지 않아 26개 모두 ENOENT로 실패했고, 폴더를 만든 뒤 다시 실행했다(리드 실수, 원시 보존).
- 비차단 기록: 작성자 heartbeat 381초 한 번(메인 결정 `msg_a8e661983ce5`대로 운영 기록), 허용 목록 밖 형식의 읽기 전용 `node -e` 2회.
- 결함 #1 정정(독립 검증 발견, 리드도 놓침): 보고서 16행은 「전 줄 CRLF」와 CR 줄 수 658/658·372/372를 적었다. 실제로 두 파일의 CR 바이트는 커밋 blob과 작업 트리 모두 0이고 `git ls-files --eol`은 i/lf w/lf다(E/pr2-review/work/v9-check.json). 리드 R-2는 LF를 관측하고도 보고서 문장과 대조하지 않았다. 「numstat에 줄 끝 변경이 섞이지 않았다」는 결론은 LF 기준으로도 참이고, 시험·완료조건 판정에 쓰인 수치가 아니다.
- 리드 이탈(첫 관찰, 리드 귀속): 이 기록을 처음 넣을 때 JS `String.replace`의 치환 문자열 안 `$'`가 특수 패턴으로 해석돼 문서 뒷부분이 한 번 더 붙었다. 커밋 전에 발견해 파일을 마지막 커밋으로 되돌리고, 치환을 함수형 스크립트(E/lead-check/pr2-goal-record.mjs)로 다시 했다.

### 수신 helper 구현

- 계약: 신규 `[Rules Sol]`(`gpt-6.1-sol` max)에 v1(E/pr2-impl-contract.md, SHA256 `31b3c323…`)을 냈다. Task `task_7845136f2c39`, Dispatch `ctx_0b2d88fd401d`다. `turn_start_unobserved`는 draft 크기 대조(22,408 = 17,621 + 4,787) 뒤 Enter 한 번으로 풀었다(E/pr2-impl-draft-recovery.md).
- 결과(worker_done `msg_6177c70b2ff5`, E/pr2/impl/report.md): `message-policy.mjs`가 `{ messageId, cliVersion, askHelp, replyHelp }` 근거를 받는다. 근거 완전성을 먼저, subject 옵션을 다음으로 본다. exception은 `official-blocking-ask`다. ORCA 75·103·121행, `99_Tools/README.md` 23행, BACKLOG `operating-reference-maintenance` 출처 칸을 맞췄다. ORCA 73행(reply 예외)은 그대로다. 커밋 `bc029e1b`.
- 리드 R-2(E/lead-check/pr2-impl-r2.md): 같은 명령 26/26, exit 0, stderr 0 bytes. 실제 공식 ask 입력은 CLI exit 0·`official-blocking-ask`, 옛 모양 입력은 exit 2·`official-ask-proof`. 여섯 파일 묶음 102,655 → 102,601 bytes, ORCA 249줄, 바뀐 줄 링크 6개 정상. Sol heartbeat 최대 229초.

### 수신 helper 독립 검증

- 계약: 신규 `[Rules 검증자]`(`claude-opus-5-5`, TEMP `.backups/tmp/rv/`)에 v1(E/pr2-review-contract.md, SHA256 `09845da6…`)을 냈다. Task `task_7ed1efa945fb`, Dispatch `ctx_f0e1d6320807`, 고정 HEAD `bc029e1b`다.
- 판정(E/pr2-review/verdict.md, worker_done `msg_90ea985c6135`): **통과**, 차단 결함 없음.
  - TDD 순서: `40e5fcf6` 트리에서 26/19/7을 재현했다. 구현 커밋은 시험 파일을 바꾸지 않았다.
  - 실제 진입(완료조건 4): fixture에 없는 실제 공식 ask 둘(`msg_fcb32bda0afe`, `msg_deaf09879e34`)로 확인했다. identity는 receipt에서, 근거는 1.4.223 help에서 가져와 `check-message.mjs`에 넣었고, 둘 다 exit 0·`official-blocking-ask`였다. 근거 누락·subject 옵션·다른 Dispatch·다른 질문 변형은 각각 exit 2·2·1·1이었다.
  - 경계 시험 보탬: null·객체 아닌 근거와 공백 cliVersion 시험을 더해 27/27이 됐다. null을 근거 없음으로 보거나 trim을 빼는 변이는 기존 26개로 잡히지 않았고 새 시험이 잡았다. 리드가 27/27을 재현하고 커밋했다(`1f73a67f`).
  - 비차단 결함 #1은 위 「수신 helper 선행 시험」의 정정이다. 설계 관찰 O1~O7은 아래 「다음 계획 후보」에 둔다.
- 관찰: Claude 작업자의 큰 도구 출력(30.8KB 초과)은 TEMP 지정과 무관하게 Claude Code 하네스가 `~/.claude/projects/…/tool-results/`에 저장했다. 명령의 명시 쓰기가 아니며, 위임 계약의 「홈은 허용 밖」과 맞지 않는 하네스 동작이다.
- 정산: 세 작업자 모두 release `retained`(`external_terminal`), tui-idle 확인 뒤 pane을 닫았다(E/pr2-tdd-close.json, pr2-impl-close.json, pr2-review-close.json).

### PR213 병합

- 승인과 병합: 메인 `msg_e46e9b3d5ce3`(2026-10-09T00:48:59Z, E/session/wait-pr213-1.raw.json)에 따르면 사용자가 메인 창에 PR213 head `4efd121e87e286d6041b67e8856b94e61b1eb2fd` 승인 줄을 Enter로 제출했다. 메인이 head 일치·CI 네 항목 SUCCESS·CLEAN을 다시 보고 병합 관문의 단독 명령으로 병합했다. merge commit `08028f1bcb98a8612dfc1a5b6b6c8ef06286030b`, mergedAt 2026-10-09T00:48:31Z.
- 메인 R-2: 판정 전문을 읽었다. 결함 #1 표본(선행 시험 보고서 16행 「전 줄 CRLF」 대 PR head 두 파일의 CR 바이트 0)으로 불일치를 확인했다. 결론이 LF blob으로도 참이라 비차단에 동의했다. `1f73a67f`의 시험 파일 hash가 판정의 `aeff6f68`과 같고, 판정 뒤 변경은 goal.md뿐임도 확인했다.
- 메인 지시: 다음 후보(O1~O7, 하네스 tool-results 관찰)는 BACKLOG 후보로만 남긴다. 메인이 사용자에게 묻는 「작업자가 사용자에게 차례를 넘길 때 리드→메인 알림의 정본화」는 이 goal 범위 밖이다.
- 종료 branch: 2026-10-09T00:50:36Z 최신 main `08028f1b`에서 `docs/tdd-canon-closeout-20261009`를 만들었다. 리드 맥락 메모는 E/closeout-astra-context.md다.

### 종료 Gardener

- 근거: 완료조건 7과 메인 `msg_e46e9b3d5ce3`의 순서다. 메인에 기동을 알렸다(`msg_a446ca3c6dd8`).
- Gardener: 신규 `claude-opus-5-5`, 태그 `[Rules 검증자]`다. 리드 pane을 split해 R-5 명령(TEMP `.backups/tmp/gd/`)으로 열었다(2026-10-09T00:53:58Z, E/gardener-split.json). 첫 화면은 선택창 없이 「Claude Code v2.1.295 · Opus 5.5 with xhigh effort」였다(E/gardener-first-screen.json). backend는 unknown이다. Task `task_8aa0bcb10eab`, Dispatch `ctx_ec2dd5166879`이고 turnStart observed였다.
- 계약: E/gardener-contract.md v1(SHA256 `263a1cc4…`, 고정 HEAD `ecdb8ae3`)이다. 쓰기는 E/gardener/report.md 한 파일이다. 고정 입력 29개의 경로·hash는 E/gardener-inputs.json, 경로 확인은 E/gardener-contract-pathcheck.txt다. Gardener는 네트워크를 쓰지 않으므로 리드가 계약 전에 CI 원시를 받아 두었다(E/session/ci-final-pr211.json·ci-runs-pr211.json, PR212·PR213도 같은 이름).
- 완료: worker_done `msg_8483ef4b00c6`(01:13:43Z, succeeded)을 수신 helper가 허용했다(E/gardener-worker-done-check-output.json). 보고서는 [E/gardener/report.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/gardener/report.md)(SHA256 `738ad4c0…`, 380줄)다. 고정 입력 29개의 hash와 HEAD·status는 시작과 끝이 같았고, 범위 밖 명시 쓰기는 0이다.
  - 집계: 확정 실패 3회 도달 0, 번호 매긴 독립 결함 7(차단 6은 문서 수정 2·메인 결정 4로 해소, 비차단 1은 기록 정정), CI 실패·재실행 0(36회 실행), 승인 없는 경고 억제·설정 완화 0, 검사를 끈 우회 0이다.
  - 드리프트 G2-a(새 발견, 낮음): ORCA 73행의 reply 예외가 아직 「1.4.218 버전 한정」이다. 바로 아래 75행의 ask 예외는 help 근거로 바뀌었다. 이 goal의 공식 질문 답 여섯은 1.4.222에서 `reply`로 보냈고, reply help에 `--subject`가 없다는 캡처는 있었다. 운용은 맞고 문장이 낡았다.
  - 드리프트 G2-b(새 발견, 낮음): 지정 TEMP(`.backups/tmp/<이름>/`)를 ORCA R-5·templates·goal-loop가 다르게 부르고, 작업자가 receipt를 거기 써도 되는지 정하지 않는다. 그래서 선행 시험 작성자와 독립 검증자의 receipt가 근거 폴더 밖 `.backups/tmp/pt/`·`.backups/tmp/rv/`에 남았다. 계약 위반은 아니다.
  - 반복(원시 기준): heartbeat 5분 초과 9구간·7세션, 공식 계약 draft 미제출 5건(모두 Codex), 규칙 묶음 측정 스크립트 11개, PowerShell `date -u` 별칭 실패 4세션, 리드 R-2 누락 3건, 리드 원시 형식 때문의 첫 파싱 실패 2건이다. 리드 이탈 7건은 종류별 첫 발생이다.
  - 정리 후보 둘(제안): ① heartbeat 간격 원시 집계 helper와 fixture 시험. 아래 「다음 계획 후보」의 작업자 생존 확인 줄에 근거를 더하는 형태다. ② 공식 계약 draft 대조 helper와 fixture 시험(새 후보). 손으로 하던 크기 대조와 Enter 뒤 전문 대조를 도구로 옮긴다. 채택은 메인을 거쳐 사용자가 정한다.
- 리드 확인:
  - 표본: heartbeat 세 구간(답 뒤 420초, 774초, 547초)과 태그 붙은 heartbeat를 원시에서 다시 쟀고 보고서와 같았다(E/lead-check/gardener-sample.mjs).
  - Gardener가 요청한 확인: 01:10:21Z~01:11:02Z 사이 리드가 이 Dispatch로 보낸 메시지는 없다. 버린 check 출력에 리드 후속은 없었다.
  - Enter 뒤 계약 전문 대조: Gardener가 「기록 0건」으로 짚었다. 리드가 Codex 세션 기록에서 다섯 Dispatch의 제출 입력을 찾아 계약 전문과 대조했다. 다섯 모두 입력 하나가 계약 전문을 담았고, 계약 밖 글자는 머리말 4,787자였다. 계약을 담은 입력은 Dispatch마다 하나라 중복 제출도 없다(E/lead-check/draft-postcheck.mjs, 출력 draft-postcheck.out.txt의 mtime 2026-10-09T01:15:13Z).
- 기록 정정(Gardener G5): 「정본 PR 작성」에 작성 Sol heartbeat 420·774초와 태그 붙은 heartbeat 4통을, 「정본 PR 문서 실사」에 실사자 547초를 더했다. 「실사 결함 수정」의 「첫 관찰」은 리드가 처음 기록한 시점이고, 원시상 첫 발생은 정본 PR 작성 Sol이다. 메인 결정 `msg_e7dd27f2bb2d`의 「첫 발생」도 그 기록에 기댔다. 뒤 결정 `msg_a8e661983ce5`가 이미 도구 층을 골라 결론은 같다. 「리드 태그 PR 문서 실사」 정산에는 첫 release 거부를 더했다.
- 리드 이탈(리드 귀속, 같은 일 다섯 번): ORCA 「공식 계약 draft 복구」의 Enter 뒤 계약 전문 대조를 그때 하지 않았다. 다섯 번 모두 크기 대조와 Enter로 끝냈다. 이번 사후 대조로 결과는 확인했다. 정리 후보 ②가 이 일의 도구 층 교정이다.
- 작업자 이탈(Gardener 자기 보고, 첫 관찰): `/tmp` 측정 중 check 출력 한 번을 `/dev/null`로 버렸다. 위 확인대로 잃은 후속은 없다. BACKLOG `mailbox-output-loss-hook`과 같은 갈래다.
- 관찰: Gardener가 도는 동안 Git Bash `/tmp`에 빈 `.tmp*` 디렉터리 40개가 생겼다. Gardener 명령 앞뒤로는 개수가 변하지 않아 생성 주체는 미확인이다.
- 정산: release `retained`(`external_terminal`), tui-idle과 빈 prompt를 확인하고 01:14:37Z에 pane을 닫았다(ptyKilled). 재조회에서 rules-active에는 리드만 있었다(E/gardener-release.json, gardener-idle.json, gardener-close.json, gardener-list-after-close.json).

### 종료 점검 자료

메인이 사용자와 결과·남은 위험·BACKLOG·다음 계획을 확인할 때 쓰는 자료다. 다음 goal은 자동으로 시작하지 않는다.

- 목표 상태: 완료조건 1~6을 충족했다. 근거는 1은 「좁힌 재실사」, 2는 「기동 시험」, 3은 「리드 태그 PR 문서 실사」, 4는 「수신 helper 독립 검증」, 5는 각 PR의 bytes 기록, 6은 「PR211 병합」·「PR212 병합과 재진입」·「PR213 병합」이다. 7은 종료 기록 PR 병합·종료 점검·R-8이 남았다.
- PR: PR211(`127cc5a1`), PR212(`5a1752eb`), PR213(`08028f1b`)이 병합됐다. 세 PR 모두 메인 창 승인 줄로 병합됐고, 최종 head의 CI 네 항목이 SUCCESS였다(실패·재실행 0). 종료 기록 PR은 branch `docs/tdd-canon-closeout-20261009`(base `08028f1b`)다.
- 보존할 정책: TDD 문단은 정본 PR 병합 뒤 범위 승인을 받는 goal부터 적용한다. 기동 TEMP 구절은 남기고 Git Bash `/tmp`는 양식의 금지와 자기 쓰기 감사로 막는다. 병합 전에 연 리드 세션은 종료까지 옛 태그를 쓴다. 공식 ask 예외는 같은 CLI의 help 근거로 판정한다.
- 검증: `node --test 99_Tools/Orca.Tests/message-policy.test.mjs` 27/27(리드 재현, 같은 명령이 CI code-rules에 있다). 서버 빌드·게임·DB는 이 goal이 건드리지 않아 해당 없음이다. 미실행은 각 절의 「미실행」 줄과 Gardener 보고서 「미실행과 한계」에 있다.
- 남은 위험: 「위험」 1(Git Bash `/tmp`는 지정을 따르지 않음)과 2(`split --command`가 기본 셸에 타이핑된다는 전제는 추론)는 그대로다. 문안 드리프트 둘(Gardener G2-a·G2-b)과 Claude 하네스의 홈 자동 쓰기는 BACKLOG 후보로 넘겼다. 크리티컬 위험은 없다.
- 로드맵 초안(사용자 확인 필요): 최소 운영 셋업 순서([병합 관문 goal 「다음 계획 후보」](../2026-10-06-merge-gate-canon-refresh/goal.md#다음-계획-후보))상 다음은 3단계 「계획 경계」다. 넘긴 후보가 BACKLOG에 실제로 도착했는지 확인하는 장치이고, BACKLOG `backlog-id-extraction-check`를 같은 범위 판단에서 본다. 3단계 마지막 PR의 병합 커밋이 게임 재개 합류점이다(「적용 중인 사용자 결정」). 그 뒤 순서는 우편함 대기 출력 유실 hook → 하네스 마무리 → 보고 방식 → 자기 개선 루프 그대로다. 순서를 바꾸자는 제안은 없다. Gardener 후보 둘이 채택되면 같은 Orca 도구 층인 우편함 대기 출력 유실 hook 단계에서 함께 보길 권한다.

### 종료 기록 문서 실사

- 근거: goal-loop 「검증 강도 4주 시범」의 「독립 세션 자체는 생략하지 않는다」와 hook goal 「종료 기록 문서 실사」 선례다. 리드는 처음 메모(E/closeout-astra-context.md)에 기록 PR 실사를 넣지 않았다. PR 생성 전에 정본을 다시 읽어 더했다(메모 「양식 보완」).
- 실사(첫 회, 차단): 신규 `gpt-6-astra` xhigh, 태그 `[Rules 검증자]`이고 리드 pane을 split해 열었다(01:22:02Z). 첫 화면은 선택창 없이 「GPT-6-Astra xhigh · Full Access · never」(Codex v0.162.0)였다(E/closeout-review-first-screen.json). Task `task_0667ee0ce323`, Dispatch `ctx_25786438ec47`, 계약 E/closeout-review-contract.md v1(SHA256 `5ce203c8…`, 고정 HEAD `ea1bd5d6`)이다.
  - draft 복구: turnStart가 unobserved였다. draft 18,530자가 계약 13,743자와 머리말 4,787자의 합과 같아 Enter를 한 번 보냈다. 곧바로 Codex 세션 기록에서 제출 입력 하나가 계약 전문을 담은 것을 대조했다(E/closeout-review-draft-recovery.md, E/lead-check/closeout-review-draft-postcheck.out.txt).
  - 실사 중 escalation `msg_1b8f43fcebb3`(01:28:04Z)이 왔다. 리드는 지적이 사실이라고 답하고 판정 전까지 입력을 고치지 않았다(`msg_27d1f9fa4555`).
  - 판정 E/closeout-review/verdict.md(SHA256 `49ccaa38…`, 254줄), worker_done `msg_7b4b298cdddd`(01:35:40Z, succeeded), 수신 helper 허용이다.
    - **#1(차단, 리드 등록 귀속)**: BACKLOG 새 행 넷(`official-draft-check-helper`·`designated-temp-wording`·`lead-tag-transition-wording`·`powershell-clock-command`)의 출처 칸에 전달 시각과 메시지 ID가 없다.
    - **#2(차단, 리드 등록 귀속)**: `worker-liveness-tool-check` 이유 칸이 heartbeat 9구간 전체를 「긴 생성 중」의 일로 적었다. Gardener G3은 간격만 집계했고 goal은 그 원인을 「추론, 미실증」으로 두었다.
    - 그 밖: C1 표본 23개 일치, BACKLOG ID 75개·중복 0, 여섯 파일 묶음 102,601 bytes 불변, 후보 도착 전부 확인, 종료 점검 자료와 메모는 지적 없음이다. 비차단 관찰은 O1(goal 후보 줄마다 BACKLOG ID)과 O2(완료조건 7의 남은 항목에 종료 점검)다.
  - 정산: release `retained`(`external_terminal`), 「Worked for 12m 19s」 뒤 빈 prompt를 확인하고 01:36:25Z에 pane을 닫았다(ptyKilled). rules-active에는 리드만 남았다(E/closeout-review-release.json, closeout-review-idle.json, closeout-review-close.json, closeout-review-list-after-close.json).
- 리드 수정(한 회차, 같은 산출물의 첫 수정): #1은 네 행 출처 칸에 발신자·완료 메시지 ID·UTC 시각을 넣었다. 시각은 각 worker_done 원시의 created_at이다(E/gardener-worker-done-check-input.json, pr2-review-worker-done-check-input.json, pr3-review-worker-done-check-input.json). `receive-helper-review-followup` 출처에도 같은 형식으로 시각을 보탰다. #2는 이유 칸을 관측(9구간·7세션)과 추론(긴 생성 중 송신 불가)으로 나눴다. O1·O2도 반영했다.
- 리드 이탈(리드 귀속, 같은 종류의 두 번째 발생): 기록으로 옮기며 원천보다 확인 수준을 올렸다(#2). hook goal 「종료 기록 문서 실사」 F3(그물 차단 관측을 확인된 오탐으로 적음)과 같은 종류다. 교정 층은 종료 점검에서 메인 판단으로 올린다. 출처 칸 누락(#1)은 첫 관찰이다.
- 좁힌 재실사(통과): 신규 `gpt-6-astra` xhigh, 태그 `[Rules 검증자]`이고 리드 pane split으로 열었다. 첫 화면은 같은 표시였다(E/closeout-rereview-first-screen.json). Task `task_237ae4f88b7c`, Dispatch `ctx_17e05615a6e1`, 계약 E/closeout-rereview-contract.md(SHA256 `6de2207c…`, 고정 HEAD `90239791`)다.
  - draft 복구: turnStart가 unobserved였다. draft 17,775자가 계약 12,988자와 머리말 4,787자의 합과 같아 Enter를 한 번 보냈다. 곧바로 세션 기록에서 제출 입력 하나가 계약 전문을 담은 것을 대조했다(E/closeout-rereview-draft-recovery.md, E/lead-check/closeout-rereview-draft-postcheck.out.txt).
  - 공식 질문 `msg_d500d2a4d4bb`(01:43:46Z): 이 절의 기동 시각 01:22:02Z와 재개 지점 01:37:56Z의 시계 출력 위치를 물었다. 두 값은 리드 Bash의 `date -u` 출력이었지만 E에 저장하지 않았다. 리드가 대화 기록에서 명령과 출력을 발췌해 새 파일로 두고 답했다(`msg_47b29cd090df`, E/lead-check/clock-transcript-excerpt.json SHA256 `faa5f63d…`). 수신 helper는 이 질문을 1.4.223 help 근거로 `official-blocking-ask` 예외 허용했다(E/closeout-rereview-ask1-check-output.json). PR213 판정이 실제 질문에 쓰인 첫 사례다.
  - 판정 E/closeout-rereview/verdict.md(SHA256 `36ee093a…`, 174줄), worker_done `msg_80a4274fbd0c`(01:50:42Z, succeeded), 수신 helper 허용이다. **통과**: #1·#2 해소, O1·O2 반영, 새 차단 0이다. 시계 두 값은 리드의 사후 발췌와 대조했다는 한계를 판정에 남겼다.
  - 리드 R-2: BACKLOG ID 75개·중복 0, 새 행 여섯이 7칸·메시지 ID·UTC 시각을 갖춤, 두 계약(E/pr3-contract.md 18행, E/pr3-review-contract.md 15행)의 긴 작성 전 heartbeat 문장을 원천과 다시 확인했다.
  - 정산: release `retained`(`external_terminal`), 「Worked for 10m 28s」 뒤 빈 prompt를 확인하고 01:51:10Z에 pane을 닫았다(ptyKilled). rules-active에는 리드만 남았다(E/closeout-rereview-release.json, closeout-rereview-idle.json, closeout-rereview-close.json, closeout-rereview-list-after-close.json).
- 리드 이탈(리드 귀속, 첫 관찰): goal에 적은 시각의 시계 출력을 E에 저장하지 않았다. 출력은 리드 대화 기록에만 있었고 재실사 질문 뒤 발췌했다.

## 정본에서 옮긴 적용 기록

규칙 문서 bytes를 상쇄하려고 메인 판단 `msg_e6c8eb971f30` M3와 계약 보충 v1.1에 따라 아래 원문을 옮겼다.

원래 위치의 행은 base `c35279bc0ce3dc2a63ed97e5db34beffcb88f25c` 기준이다.

- (가) [goal-loop 「위임 계약」](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#위임-계약), `.agents/skills/dawnholder-goal-loop/SKILL.md` 60행.

```text
반복 규칙의 검사화·수리 안내, 정본 helper의 짧은 경로, 수기 목록 존재 검사, warning→실측→사용자 승격/강등, 실행불가/위반 구분과 구조/동작 별도 커밋은 [하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙)을 다음 계약부터 승인 범위에 적용한다. 이 하네스 원칙만으로 주석 삭제·작은 작업 예외·검증 차등·가지치기를 도입하지 않는다. 이미 진행 중인 계약과 검증의 고정 입력은 조용히 바꾸지 않고 [버전 인계](references/orca-work.md#역할과-작업-계약)로 연결한다.
```

이관 이유: 첫 문장의 「다음 계약부터」는 지난 적용 시점이다. 둘째 문장은 [CODE_CONVENTION 「하네스 원칙」](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 끝 문단에 같은 규칙이 있다. 셋째 문장은 [goal-loop 36행](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증-강도-4주-시범)과 [orca-work 26행](../../../.agents/skills/dawnholder-goal-loop/references/orca-work.md#역할과-작업-계약)에 같은 규칙이 있다.

- (나) [goal-loop 「기준과 상태」](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#기준과-상태), `.agents/skills/dawnholder-goal-loop/SKILL.md` 16행 끝 문장.

```text
형식·갱신 주체의 원천은 [진척 적용 결정](../../../01_Phases/goals/2026-10-05-operating-canon/goal.md#pr183-제출-뒤-적용한-사용자-결정)이다.
```

- (다) [ORCA R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle), `00_Document/operations/ORCA.md` 213행.

```text
R-8의 당시 적용 시점과 두 관찰 기록은 [이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 보존한다.
```

- (라) [RESUME](../../../00_Document/operations/RESUME.md), `00_Document/operations/RESUME.md` 3행 끝 문장.

```text
이전 정정·checkpoint·라우팅 서술은 [이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 보존한다.
```

### 리드 태그 PR

규칙 문서 bytes를 상쇄하려고 메인 판단 `msg_e6c8eb971f30` M3와 계약 보충 v1.1에 따라 아래 원문을 옮겼다.

원래 위치의 행은 base `127cc5a197a527c437412657c8dcfa38c1354862` 기준이다.

- `00_Document/operations/ORCA.md` 75행 끝 문장.

```text
종료된 1.4.217 확장·복귀 근거는 [이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 있다.
```

- `00_Document/operations/ORCA.md` 146행 끝 문장.

```text
실패 이력과 후속 성공의 원문은 [이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 보존한다.
```

- `00_Document/operations/ORCA.md` 201행 끝 문장.

```text
M-1 관찰·출처와 미검증 구분은 [이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 보존한다.
```

## 다음 계획 후보

- 리드 태그 PR 실사 O1(Rules): AGENTS 전환 문단의 「종료까지」를 다음 문안 정비 때 「세션 종료까지」처럼 주체를 밝힐지 본다. 여섯 파일 묶음 여유가 3 bytes라 바꾸면 다시 잰다. BACKLOG `lead-tag-transition-wording`.
- 리드 태그 PR 실사 O2(Rules): BACKLOG 65행 「Astra 갱신 책임」의 호칭을 그 행을 손볼 때 정리할지 본다. 지금은 AGENTS 전환 문단이 당시 이름으로 해석하게 한다. BACKLOG `lead-tag-transition-wording`.
- 작업자 생존 확인의 도구 층 교정(Rules, 메인 결정 `msg_a8e661983ce5`): 예를 들어 리드 대기 중 heartbeat 공백이 5분을 넘으면 작업자 화면 상태를 읽어 기록하는 helper를 둘 수 있다. 장문 작성 중 heartbeat를 대신 보내는 방법도 검토한다. 5분 간격 자체가 긴 작성에 맞는지도 함께 본다. 문서로 끝내지 않는 이유: 「긴 작성 직전 heartbeat 한 번」 계약 문장을 두 작업자가 지켰는데도 한 번의 긴 작성이 5분을 넘었다(R1 363초, R2 337초). 작업자는 한 번의 긴 생성 도중에는 heartbeat를 보내지 못하는 것으로 보인다(추론, 미실증). 종료 Gardener 후보 ①이 원시 9구간·7세션 근거와 간격 집계 helper·fixture 검사화 방법을 더했다. BACKLOG `worker-liveness-tool-check`.
- 수신 helper 검증 관찰(Rules, BACKLOG `receive-helper-review-followup`):
  - O1 `official-ask-proof` 진단 path를 필드별로 낸다.
  - O2 `official-ask-mismatch`의 수리 안내에서 help 재확인을 빼고 receipt·메시지 대조로 좁힌다.
  - O3 근거 완전성 조건을 이름 있는 술어로 뺀다.
  - O4 help 명령 확인을 첫 줄 비교로 좁힌다(계약 변경 필요).
  - O5 ORCA 103·121행의 같은 문장 중복을 줄인다.
  - O6 AGENTS 47행 「버전 한정 subject 예외」를 「help 근거 한정」으로 고친다. 지금 읽기로도 틀리지 않으나 상수가 남았다고 오해할 수 있다.
  - O7 BACKLOG 120행 덧붙인 출처의 KST 날짜를 UTC 시각과 Task ID로 바꾼다.
- Claude 작업자 하네스 쓰기(Rules): 큰 도구 출력은 하네스가 `~/.claude/projects/…/tool-results/`에 자동 저장한다. 위임 계약의 허용 쓰기 위치에서 이를 어떻게 다룰지 정한다. 작업자가 막을 수 없는 하네스 동작이다. BACKLOG `designated-temp-wording`.
- 종료 Gardener 후보 ②(Rules, 새 후보): 공식 계약 draft 대조 helper와 fixture 시험. Enter 전 placeholder 크기 판정과 Enter 뒤 세션 기록의 계약 전문 대조를 도구로 옮긴다. 이 goal에서 다섯 번 손으로 계산했고 Enter 뒤 대조는 사후에 했다(「종료 Gardener」). BACKLOG `official-draft-check-helper`.
- 종료 Gardener 드리프트(Rules): G2-a ORCA 73행 reply 예외를 75행과 같은 help 근거로 맞춘다(수신 helper O6과 함께). G2-b 지정 TEMP의 용도(하네스 임시 파일만인지, receipt도 되는지)를 ORCA R-5·templates·goal-loop에서 한 뜻으로 맞춘다. BACKLOG는 G2-a가 `receive-helper-review-followup`, G2-b가 `designated-temp-wording`이다.
- PowerShell 시계 명령(Rules): Codex PowerShell 세션 넷에서 `date -u`가 Get-Date 별칭으로 실패했고, 모두 Git `date.exe`로 다시 실행했다. 양식의 「`date -u` 같은 시계 명령」 안내에 PowerShell 대안을 적을지 본다. BACKLOG `powershell-clock-command`.

종료 기록에서 위 후보를 BACKLOG 「TDD·임시 쓰기 goal에서 연결한 후보」에 등록했다. 등록은 채택·착수 권한이 아니다.
