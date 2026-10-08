# TDD 정본화와 작업자 임시 쓰기 경계

## 재개 지점

Rules의 목표이며 운영 셋업 2단계다. 사용자가 범위 v2를 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch·PR: 정본 PR은 branch `docs/tdd-canon-temp-boundary-20261008`(base main `c35279bc`, upstream 없음)이고 아직 PR이 없다.
- 근거 폴더 E: `.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/astra-context.md), 승인 범위는 [scope-draft-v2.md](../../../.backups/verification/2026-10-08-tdd-canon-temp-write-boundary/scope-draft-v2.md)(SHA256 `b93b6dbd…`)다. 받은 메시지 원시는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules Astra]`, handle `term_10beffe1-9c4b-4593-893a-af699b52791d`. Run은 `run_be5206d9a2af`, 회신 주소는 `run:run_be5206d9a2af`다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- **현재 위치**(2026-10-08T11:12:03Z): 범위 승인 뒤 정본 PR branch를 만들고 goal 시작 기록을 쓰는 중이다.
- **다음 할 일**: 리드가 작업자 기동 TEMP 지정의 기동 시험을 한다 → 정본 PR Sol 계약 → 문서 실사.
- 주의: rules-active에는 직전 goal의 분리 시험이 남긴 worktree 설정 세 줄이 있다. 사용자 결정 「8번 키 남김」으로 유지하며 원격 반영은 deploy key(SSH)로 나간다.
- 직전 goal 기록: [hook 차단 줄이기와 보조 세션 스킬](../2026-10-07-hook-friction-helper-session/goal.md)은 PR210 병합(`c35279bc`)으로 끝났다. 그 goal의 「현재 위치」·「다음 할 일」은 기록 PR 재실사 전 시점으로 남았다. 끝난 goal은 고치지 않으므로 그 사실만 여기 적는다(메인 진입 지시 `msg_83e2bc491fda`, 이전 리드 인계 `msg_4b8b51290c62` 2항).

## 진척 단계

- [x] 범위와 기준 확정
- [>] 정본 PR 작성·실사
- [ ] 정본 PR 병합
- [ ] 리드 태그 PR 실사
- [ ] 리드 태그 PR 병합
- [ ] 수신 helper 선행 시험
- [ ] 수신 helper 구현·검증
- [ ] 수신 helper PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 PR 병합
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
2. 리드가 R-5 새 명령으로 Claude 시험 세션 하나를 열어 임시 폴더·Git Bash `/tmp`·Write 결과가 지정 경로로 가는지와 경로 길이를 원시로 남긴다. 따르지 않으면 R-5 구절을 빼고 메인에 알린다.
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

1. 기동 때 TEMP 지정은 추론이다. Claude Code 임시 폴더와 Git Bash `/tmp`가 TEMP를 따르는지, 경로 길이가 괜찮은지 미측정이다. 완료조건 2의 기동 시험으로 확인하고, 실패하면 구절을 뺀다.
2. 기동 명령이 기본 셸 문법에 묶인다. `orca terminal create --help`(1.4.222)는 `--command`가 호스트 기본 셸에 타이핑된다고 적는다. split도 같다고 보며(추론) 기동 시험에서 확인한다.
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

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **다음 Rules goal과 합류점**(진입 지시 `msg_83e2bc491fda`): 「대시보드 결정 응답: 1) 다음 Rules goal - 운영 셋업 2단계와 자격 증명 분리 후속 중 무엇을 먼저 → A 운영 셋업 2단계 먼저」, 「대시보드 결정 응답: 1) main 합류점 - 어느 지점에 모아서 다시 갈래를 낼지 → A 게임 재개 관문을 합류점으로」. 운영 셋업 3단계 마지막 PR 병합 커밋이 합류점이고, 그 전에는 끝난 PR만 main에 받는다. 직전 goal의 「Core·Content 재개 시점 → A 3단계 뒤 재개」가 그대로 적용된다.
- **리드 태그**(`msg_628431238925`): 「그리고 각각 리드급 세션이 본인을 Astra라고 소개하는데, 그 부분도 좀 수정해야겠다, 지시사항에 낡은게 좀 남아있나보네」. 현황판 질문 「리드 태그 - 「Astra」를 「리드」로 바꾸는 일을 지금 Rules goal에 넣을지」(A 지금 Rules goal에 별도 PR로 / B BACKLOG 후보로 미룸)에 대한 답 「A로 가되, 모델명도 같이 표기」.
- **범위 수정 요청**(`msg_73e4421772c2`, 11:05:56Z): 「대시보드 결정 응답: 1) 계획 검토 - Rules 운영 셋업 2단계 범위 승인 + Q1~Q3(TDD·임시 쓰기·리드 태그) → B 수정 요청 (초안 msg_4c05bcfed7d9), 코멘트 「범위의 기준이 좀 모호한데 더 구체적으로 설명해줄래?」」.
- **범위 승인**(`msg_8dce3f61a14c`, 11:11:21Z, E/session/wait4.raw.json): 「대시보드 결정 응답: 1) 계획 검토 - Rules 운영 셋업 2단계 범위 v2 승인 + Q1~Q5 → A 승인 (초안 msg_31e28cdb79e5)」. **Q1~Q5를 모두 A로 읽은 것은 메인 해석이다.** 현황판 항목 제목이 「범위 v2 승인 + Q1~Q5」였고 초안 추천이 다섯 다 A였다. 메인이 이 해석을 사용자에게 알렸다. Q1 A 기동 때 TEMP 지정, Q2 A 리드만 모델명, Q3 A 「Opus」, Q4 A CURRENT 띄우기 넣음, Q5 A TDD 예외 구절 넣음이다.
- **직전 goal 종료 점검**(직전 goal 「적용 중인 사용자 결정」의 「종료 점검 둘」, 메인 전달 `msg_a73d4d5bbb3c`): 교정 층 「A 도구·양식 층」(기록 시각, 위임 계약 허용 실행의 읽기 기본값)과 ⑤ 「수신 helper의 Orca 1.4.218 고정은 다음 Rules 운영 goal에서 고친다」. 그때까지 리드가 공식 ask를 손으로 대조한다.
- **F2 교정 층의 메인 처리**(`msg_6031c2e0ecc8`, 사용자 결정 「A 도구·양식 층」의 메인 적용): BACKLOG ID 검사 helper(백틱 유무와 무관한 ID 추출과 fixture)를 다음 Rules 운영 goal의 기록 helper 후보로 넘겼다. 메인이 넣을지 판단을 리드에 맡겼고(진입 지시), 이 goal에서는 BACKLOG 새 행으로만 둔다(범위 v2 빼는 항목 3번).

## 현재 결과

### 착수

- 진입: 2026-10-08T10:47Z 진입 지시 수신, 10:49:45Z Run 생성(E/session/run-create.json), READY `msg_cdc2ad590f91`(10:50:34Z).
- 범위 근거 시험: E/probe-current-merge/에서 `git merge-file`로 CURRENT 줄 형식을 시험했다. 빈 줄 하나로 띄운 두 줄 변경은 exit 0으로 자동 병합됐고, 붙은 두 줄 변경은 exit 1로 충돌했다.
- 리드 이탈(첫 관찰, 리드 귀속): 진입 직후 리드가 Orca 수신·가이드 원시를 시스템 TEMP에 먼저 저장했다가 근거 폴더로 복사했다. 이 goal이 다루는 임시 쓰기 사례 10번으로 범위 근거에 넣었다.
- branch: 2026-10-08T11:12:03Z 최신 main `c35279bc`에서 `docs/tdd-canon-temp-boundary-20261008`을 만들었다.

## 다음 계획 후보

- 아직 없다.
