# 병합 관문과 운영 정본 현행화

## 재개 지점

Rules의 목표다. 사용자가 범위 초안을 승인했고(아래 「요구사항 원천과 적용 결정」), 메인 `msg_e02ee97c2a7e`(2026-10-06T14:01:38Z)가 그 원문을 전달했다. 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: PR1은 `feat/merge-gate-20261006`(base `a47a027`)이다. PR2 branch는 PR1 병합 뒤 최신 main에서 만든다(제안 이름 `docs/canon-refresh-20261006`).
- 근거 폴더 E: `.backups/verification/2026-10-06-merge-gate-canon-refresh/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/astra-context.md)다. 받은 메시지 원시와 범위 초안 사본은 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh, 태그 `[Rules Astra]`, handle `term_af8ac4fc-29c5-4c97-8671-0279b10a9688`. Run은 `run_93a27bd491a9`, 회신 주소는 `run:run_93a27bd491a9`다. 이전 Rules goal의 Run·Task·Dispatch는 실행 권한이 아니다.
- **현재 위치**: 범위가 확정됐다. 다음은 설계 실측(아래 「설계 기본값과 실측 대기」)이다. 실측 결과로 설계를 확정한 뒤 선행 시험 → 구현 → 독립 검증 → PR1 순서로 간다.

## 진척 단계

- [x] 범위와 기준 확정
- [>] 병합 관문 설계 실측
- [ ] 병합 관문 선행 시험
- [ ] 병합 관문 구현·검증
- [ ] 관문 PR 병합
- [ ] 관문 적용 확인
- [ ] 정본 현행화 작성·실사
- [ ] 정본 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 점검과 R-8 인계

PR 단계 이름은 PR이 생기면 「PR### 병합」으로 바꾼다.

## 범위

승인 범위는 초안 v1(`msg_e492ad1756dd`)과 보정(`msg_a63c25f682e1`)을 합친 것이다. 사용자 질문 1~4의 답은 모두 A다. 아래는 그 답을 반영한 범위다.

### 만들 것

PR1 — 병합 관문

1. Claude Code 프로젝트 hook과 시험.
   - 승인 기록: 사용자가 메인 창에 Enter로 제출한 승인 문장 한 줄로만 기록을 만든다. 형식은 전용 머리말 + PR 번호 + head 40자의 줄 전체 일치다. 「대시보드 결정 응답」의 다른 안건이나 「(head …)」 표기는 기록을 만들지 않는다. 태그 `[`로 시작하는 입력도 기록하지 않는다. 대시보드가 이 문장을 만들게 하는 일은 메인 몫이다.
   - 병합 차단: 모든 Claude Code 세션의 Bash 명령 문자열 전체에서 병합 시도를 찾는다. 대상은 `gh pr merge`(단독·`&&`·heredoc·`bash -c` 안), `gh api`의 PR 병합 호출(REST merge, GraphQL mergePullRequest), main으로의 push(`main`, `HEAD:main`, `refs/heads/main`), `--auto`·`--admin` 병합이다.
   - 통과 조건: 메인 세션, 같은 세션의 기록, PR 번호와 head 40자 일치, `--match-head-commit` 포함, 유효 시간 안, 한 번만 사용. main push와 `--auto`·`--admin`은 기록이 있어도 막는다. 조건을 모두 채우면 확인 창 없이 통과시킨다(질문 2 A). 이 동작이 전역 ask 규칙을 넘는지는 설계 실측에서 확인한다.
   - 기록 보호: 승인 기록 폴더(Git 제외 `.claude/state/` 아래)에 쓰는 Write·Edit 호출과 그 경로를 담은 Bash 명령을 막는다. 다른 세션이 메인 터미널에 승인 문장을 넣는 `orca terminal send` 명령도 막는다. 이 완화책은 Claude Code 세션에만 걸린다.
   - 메인 세션 식별: 메인 전용 폴더(질문 3 A)로 알아본다. 구체 방법은 설계 실측 뒤 정한다.
   - 설정은 저장소의 `.claude/settings.json`(새로 추적하는 프로젝트 범위)에 둔다. 스크립트와 시험은 `99_Tools/<목적 이름>/`·`99_Tools/<목적 이름>.Tests/`에 둔다. 시험은 code-rules CI의 독립 회귀 단계에 연결한다.
2. 병합 절차를 바꾸는 정본 문장: AGENTS 「Git 권한」(병합 실행은 메인 세션, 승인 문장 형식), ORCA(병합 절차·hook 한계·적용 확인, 메인 전용 폴더와 R-1 배치 변경), goal-loop 「통합과 보고」, CLAUDE.md(메인 작성).

PR2 — 나머지 정본 현행화 (요청서 항목 1·3~8과 BACKLOG 등록)

- 리드 모델 라우팅: AGENTS 모델 라우팅·역할, ORCA R-1 기동 명령과 Core 리드 pane 닫기 주의, R-5 검증자 기동, R-8 재진입과 목표 중간 리드 교체의 `run-use` 인수, RESUME 진입. 근거는 아래 「사용자 관측과 메인 측정」을 나눠 쓴다. CLAUDE.md 세션 역할은 메인이 쓴다.
- 검증자 모델 시범 조항: goal-loop 「검증 강도」 옆에 2026-10-31까지 시범을 넣는다. 선행 시험 작성자는 시범 대상이 아니다(질문 4 A). 관찰과 토큰 관측은 10-31 평가 자료로 이 goal에 둔다.
- 진입 링크: RESUME·CURRENT의 「정본 반영 전 적용 결정」 링크를 이 goal의 「적용 중인 사용자 결정」으로 바꾼다.
- 메모리에만 있는 Orca 절차 다섯 가지: 닫은 pane 재출현, split 시간 초과 뒤 늦게 생기는 pane, 활성 Dispatch 없는 escalation의 실제 도착(R-4 서술 정정), 메모리 회수로 꺼진 대기의 자동 재시작 금지, 크래시·업데이트 뒤 복구. ORCA에 한 절로 줄여 넣는다.
- 우편함 대기 정책: 리드 대기 명령과 `&`·`/dev/null` 금지, Codex 세션이 대기를 맡지 않는 배치가 비용의 핵심이라는 근거, heartbeat 알림이 Claude 리드를 깨우는 비용.
- 병합·종료 운영 규칙: 충돌로 head가 바뀐 PR, 좁힌 재실사, 재실사 결과의 기록 위치, Gardener 순서, 공식 계약 draft 복구의 크기 조건 정정(사전 추정과 사후 대조를 나눔).
- 정본이 낡는 문제: 목표 사이 리드 작업 폴더를 최신 main으로 옮기는 절차를 R-8에 넣는다(Management 제안 5 포함).
- 문서 지도: 00_Document/INDEX에 운영툴 진입 한 줄과 「Management goal은 05_Management/goals」 한 줄(Management 제안 2·3 일부).
- BACKLOG: 계획 25~37번 한 줄씩, Management 이관 3행, 이 goal에서 나온 후보. 기존 행과 같은 일은 기존 행에 근거를 단다. 35번은 실행 배치 문구를 빼고 `management-launcher-real-run`을 가리킨다. 38번은 이미 반영돼 등록하지 않는다.

### 건드릴 곳

- PR1: `.claude/settings.json`(신규), `99_Tools/<목적 이름>/`·`99_Tools/<목적 이름>.Tests/`(신규), `.github/workflows/code-rules.yml`(시험 단계 하나), `99_Tools/README.md`(진입 한 줄), 필요하면 `.gitignore` 한 줄, AGENTS.md 「Git 권한」, ORCA.md, goal-loop SKILL.md 「통합과 보고」, CLAUDE.md(메인), 이 goal과 CURRENT Rules 줄.
- PR2: AGENTS.md(모델 라우팅·역할), ORCA.md, RESUME.md, CURRENT.md, BACKLOG.md, goal-loop SKILL.md, orca-work.md, 00_Document/INDEX.md, CLAUDE.md(메인), 이 goal.
- CLAUDE.md는 메인만 쓴다. 리드가 쓰기 창을 열면 메인이 rules-active에서 고치고 리드가 커밋한다.

### 하지 않을 것

- 전역 Claude·Codex·Git·gh 설정 변경, Codex 프로젝트 설정 추가(질문 1 A), GitHub 계정·ruleset 변경.
- PR 생성 확인 창(전역 ask 규칙) 변경, 대시보드 코드 변경.
- 우편함 대기 `&`·`/dev/null` 차단 hook(묶음 2, 계획 12).
- 묶음 2(임시 쓰기 경계, TDD 문구, heartbeat 자동화, 인용 검사, Management 테스트 CI)와 묶음 3(CURRENT·goal 상태 형식). 같은 문단을 고치게 되면 경계만 밝힌다.
- 계획 25~37번의 구현, 보고서 폴더 이동(Management 제안 1), 색인 검사 CI(제안 4), FEATURE_MAP의 Management 행.
- 메인 전용 폴더 생성과 Orca 배치의 실제 변경. 정본 문장만 쓰고 실행은 PR1 병합 뒤 사용자·메인이 한다.
- 과거 goal 원문 수정, 명칭 잔여의 일괄 정리. PR2가 같은 문단을 고칠 때만 함께 맞춘다.

### 관찰 가능한 완료조건

1. PR1 선행 시험이 구현 전에 실패하고 구현 뒤 통과한다. 시험은 병합 시도의 형태(단독, `&&`, heredoc, `bash -c`, `gh api` REST·GraphQL, main push 세 형태, `--auto`, `--admin`)와 통과 조건의 반례(기록 없음, 다른 세션, PR 번호 불일치, head 불일치, 짧은 head, `--match-head-commit` 없음, 시간 초과, 두 번째 사용, 메인 아닌 세션)를 덮는다. 기록 경로 쓰기와 승인 문장 주입 명령도 덮는다.
2. 독립 검증자가 실제 Claude Code 세션에서 hook 진입을 한 번 실행한다. 대상은 안전한 것만 쓴다(없는 PR 번호, push는 dry-run이나 없는 원격). 실제 병합과 main push는 하지 않는다.
3. 정본 문장이 hook 동작과 맞고, 한계(입력 출처 미구분, Codex 미적용, 문자열 판정의 의도적 우회 가능성)를 적는다.
4. PR1은 정확한 head의 CI·독립 검증 뒤 사용자 개별 승인으로 병합한다. 이 병합이 리드 pane에서 지금 방식으로 하는 마지막 병합이다.
5. 적용 확인: PR1 병합 뒤 메인 세션이 메인 전용 폴더에서 새 설정으로 열린다. 메인 창과 리드 pane에서 기록 없는 병합 시도(없는 PR 번호)가 막히는 것을 각각 1회 확인하고 원시를 남긴다. 세션 중 설정 변경이 바로 반영되는지도 이때 실측한다.
6. PR2는 신규 검증자의 문서 실사를 통과한다. 내용·링크·권한·원천과 현실적 시나리오(새 리드 기동, 목표 중간 리드 교체, 충돌 재통합, draft 복구, 새 관문으로 병합)를 대조한다. 메인이 쓴 CLAUDE.md도 대상이다.
7. ORCA는 250줄 이하다. 다른 규칙 문서는 bytes가 늘지 않거나, 늘린 만큼 같은 PR에서 줄인 원시 수치를 남긴다.
8. PR2는 새 관문(메인 창 승인 문장)으로 병합한다. 그 전에 완료조건 5가 끝나 있어야 하며, 안 됐으면 메인에 올린다.
9. 전체 결과 기록 → Gardener → 종료 점검 → R-8 순서로 끝낸다. 다음 goal은 자동으로 시작하지 않는다.

## PR 경계와 검증

| PR | 변경 경계 | 등급과 세션 |
|---|---|---|
| 1 - 병합 관문 | hook·설정·시험·CI 단계·도구 README, 병합 절차 정본 문장, CLAUDE.md(메인) | 강: 보안 경계와 실행 도구. 선행 시험은 신규 Opus(질문 4 A), 구현은 Sol max, 독립 검증은 신규 Opus(시범의 보안 경계 예외). R-7은 메인 확인으로 비적용 |
| 2 - 정본 현행화 | 문서만 | 문서 실사. 작성은 Sol max, 독립 검증은 신규 `gpt-6-astra` xhigh(시범) |

두 PR이 같은 정본 파일(ORCA·AGENTS·goal-loop·CLAUDE.md)을 고치므로 PR2 branch는 PR1 병합 뒤 최신 main에서 만든다. PR1 승인을 기다리는 동안 PR2 계약 준비(읽기)는 한다. 실제 diff의 파일·줄 수는 작성 종료 때 numstat 원시로 남긴다. 강 등급은 diff·근거 실사, 독립 시험 보완·실행, 실제 진입 1회 실행을 요구한다.

R-7: 리드는 PR1이 보호 집합·오류 분류·실패 수명에 닿아 해당이라고 판단했다. 메인 `msg_bc4cea4b161f` 1항은 시범 자리 2~3개가 Content 첫 goal과 Core 다음 두 작업에 이미 배정돼 있어 넓히지 않는다고 확인했다. 그래서 비적용이다.

### 설계 기본값과 실측 대기

승인 범위 안에서 리드가 정한 기본값이다. 실측 결과에 따라 바뀌면 이 절에 이유와 함께 고친다.

- 승인 문장: `병합 승인: PR<번호> head <40자 hex>` 한 줄이 입력 전체와 일치할 때만 기록한다. 머리말 문구는 메인과 맞춘다.
- 유효 시간 30분, 한 번만 사용. 병합 명령이 실패했을 때 기록을 되살릴지는 설계 확정 때 정한다.
- 병합 시도로 판정된 명령은 판정 오류·입력 해석 실패 때 막는다(실패 시 차단). 병합 시도가 아닌 명령은 통과시킨다.
- 구조는 기존 99_Tools/Orca를 따른다. 판정은 Node 표준 라이브러리만 쓰는 순수 함수에 두고, hook 입출력은 얇은 진입 스크립트가 맡는다.
- 설계 실측 항목(신규 Opus 한 세션, 쓰기는 E의 실측 폴더만):
  1. PreToolUse hook의 allow가 전역 ask 규칙(`Bash(gh pr merge*)`)을 넘어 확인 창 없이 실행되는가. 무해한 명령(`gh pr merge --help`)으로 잰다(메인 `msg_bc4cea4b161f` 3항).
  2. UserPromptSubmit·PreToolUse 입력의 실제 필드(session_id, cwd, prompt)와 hook 실행 환경(`CLAUDE_PROJECT_DIR`, Git Bash에서 node 실행 가능 여부).
  3. Agent 도구로 띄운 하위 에이전트의 Bash 호출에도 PreToolUse가 돌고 같은 session_id가 오는가.
  4. 폴더 단위 표식(Git 제외 로컬 파일 또는 `.claude/settings.local.json`의 env)이 hook에서 읽히는가.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 이번 착수의 원천은 다음과 같다.

- 범위 승인: 메인 `msg_e02ee97c2a7e`(2026-10-06T14:01:38Z, E/session/wait2-msg_e02ee97c2a7e.raw.txt)가 전달한 사용자 원문은 **「대시보드 결정 응답: 1) 계획 검토 - Rules 병합 관문과 운영 정본 현행화 (v1 보정) → A 승인 (초안 msg_a63c25f682e1)」**이다. 메인 해석은 v1과 보정을 합친 범위를 그대로 승인했고 질문 1~4가 추천대로 1A 2A 3A 4A라는 것이다. 코멘트는 없었다.
- 메인 확인 `msg_bc4cea4b161f`(13:56:41Z, E/session/wait1-msg_bc4cea4b161f.raw.txt): R-7 비적용, 리드 판단 승인(대기 차단 hook은 묶음 2, draft 복구 크기 조건의 추정·사후 대조 분리, R-4 서술 정정, heartbeat 5분 유지, Management 제안 2·3·5 포함과 1·4·FEATURE_MAP 백로그행, ORCA 줄 수 감축 방식), allow 대 ask 실측 요청이다. 메인 운영 판단이다.
- 목표 요청서: 메인 `msg_3902e180080c`(13:41:54Z, E/session/inbox-rules-lead-20261006T1404Z.json). 요청서가 전달한 사용자 원문은 아래다. 모두 메인 전달이며 직접 입력으로 격상하지 않는다.
  - 처리 계획 승인(18:2x KST): 「OK A로 가자, 현황판도 업데이트 해줘」. 직전 질문은 「규칙·운영 미반영 38건 - 항목별 목록과 처리 순서」였고 A는 「이 묶음·순서로 시작」이다. 이 목표가 묶음 1이다.
  - 병합 관문(17:19 KST): 「1안건 A, 2안건 A로 가자」. 1안건 A는 「메인 창 병합 + 승인 기록 hook」이다. 걱정 원문(15:12 KST): 「이게 좀 불편한데, Hook에서 Allow하면 PR과 Merge를 또 에이전트 스스로 먼저 해버리는 환각이 발생할 수 있으니까 고민되네」. 우선순위 원문(17:1x KST): 「PR이랑 Merge 컨트롤을 메인세션에서 어떻게 할지인데, 이거 먼저 해결하고 넘어가자」.
  - 검증자 모델 시범(18:1x KST): 「대시보드 결정 응답: 3) 검증자 모델 - Astra 검증을 시범으로 시작할지 → A 10-31까지 시범으로 시작」.
  - 리드 Opus 전환: 2026-10-05의 세 결정(`msg_2a9682a1728b`, `msg_22cb1701a2cf`, `msg_25102e277345`). 원문은 [직전 Rules goal의 적용 결정](../2026-10-05-ci-warning-operating-followup/goal.md#적용-중인-사용자-결정)에 있다.
- 범위 초안과 보정: `msg_e492ad1756dd`(13:53:23Z, 사본 E/session/scope-draft-v1.md), `msg_a63c25f682e1`(13:57:53Z).

### 사용자 관측과 메인 측정

항목 1(리드 라우팅)·3(검증자 시범)·6(대기 정책)의 근거다. 둘을 섞어 쓰지 않는다.

- 사용자 관측(2026-10-06 19:4x KST, 메인 pane 제출, 요청서가 전달): 「일단 사용량 확인해보니까 확실히 리드에 Astra를 사용할때보다,토큰 소모량이 훨씬 경제적이야, 관측내용 관련내용이나 문서에 기재해줘」, 「Astra가 활동하는 Codex Program Harness가 입력토큰을 낭비하는 주 원인인거 같아, 대기 Terminal을 활용하지않고 Cron Job으로 수시로 입력토큰으로 다른 세션의 작업을 확인하는게 원인같아」, 「Astra를 검증자로 쓰는게 제일 좋네, 토큰경제적으로」. 사용자가 본 사용량 화면의 수치는 메인이 받지 못했다.
- 메인 측정(요청서 요지, 원문은 저장소 밖 메인 노트 token-observation-lead-switch.md): 리드가 Opus인 최근 24시간에 리드 11세션의 캐시 읽기 658.4M, 캐시 쓰기 8.0M, 출력 2.1M 토큰이었다. 리드가 Astra였던 24시간(10-05 측정)에는 Codex 입력 1,219M 중 리드가 88%였고, 리드 호출의 약 60%가 우편함 대기·확인이었다. 확인된 원인은 Codex가 한 번의 차단 대기를 60초 이하로 막아 다시 기다릴 때마다 문맥 전체를 실은 호출이 생긴다는 점이다. Claude Code의 백그라운드 실행은 끝날 때만 모델을 깨운다. 한계는 날짜·작업량·집계 방식이 다르고 토큰 수가 한도 차감률과 같지 않다는 점이다. Rules의 실측이 아니다.

## 적용 중인 사용자 결정

정본 반영 전까지 적용하는 결정이다. PR2가 정본에 넣으면 각 줄에 반영 위치를 적는다. 모두 메인 전달이며 직접 입력으로 격상하지 않는다.

- **검증자 모델 시범**: 위 원문(18:1x KST). 메인 진입 지시 `msg_3902e180080c`의 임시 규칙은 다음과 같다. 문서 실사와 코드 검증(강·약)의 독립 검증자는 신규 `gpt-6-astra` xhigh다(split에 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`). DB·영속 데이터, 프로토콜·공유 DLL, 보안 경계를 바꾸는 작업과 해당 여부가 애매한 작업은 신규 `claude-opus-5-5`가 검증한다. Gardener, 확정 실패 뒤 Advisor, R-7 설계 검토는 대상이 아니다. 선행 시험 작성자도 대상이 아니다(질문 4 A). 구현자·검증자 분리, 작업 하나 뒤 정산·종료, 파트당 검증자 동시 하나, 테스트 파일만 쓰기, 판정 양식과 통과 차단 사유, 태그는 그대로다. 판정의 지정 모델·관찰 모델 칸을 채운다.
- **리드 Opus**: 다섯 리드는 `claude-opus-5-5` xhigh다(`msg_22cb1701a2cf`, 교체 시점은 `msg_25102e277345`). 원문은 직전 Rules goal에 있다. 정본의 「파트 리드 Astra `gpt-6-astra`」와 R-1 기동 명령은 PR2 전까지 낡은 문장이다.
- **병합 관문**: 1안건 A(위 원문). PR1 병합과 적용 확인 전까지 병합은 지금 방식(리드 pane, 사용자 확인 창, 메인의 병합 신호)으로 한다.
- **후속 계획의 일괄 검토**: 메인 `msg_bf63c20c8abe`가 전달한 원문 「오케이 후속 계획은 일단 현재 해야하는 작업들 먼저 진행하고, 나중에 계획 한번에 몰아서 검토하자.」. 현재 goal 밖 후보는 BACKLOG로 모으고 개별 승인을 받지 않는다.
- 메인 운영 지시(사용자 결정 아님): 우편함 대기는 `--types "status,dispatch,worker_done,merge_ready,escalation,handoff,decision_gate,question"`로 heartbeat 단독 깨움을 뺀다(`msg_20663b7c7598`). 대기는 Bash 백그라운드로 한 번에 하나만 열고 `&`·`/dev/null`로 출력을 버리지 않는다(`msg_3902e180080c`).

## 현재 결과

아직 없다.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. PR2에서 BACKLOG에 같은 규칙으로 기록한다.

- Codex 세션의 병합 차단: 저장소 `.codex/` 프로젝트 hook으로 같은 판정을 거는 방법. 신뢰한 프로젝트에서만 읽고 hook 내용이 바뀔 때마다 사용자 검토가 필요하다(초안 세부 근거 4). 사용자 질문 1 A로 이번에는 하지 않는다.
- 에이전트용 GitHub 계정 분리와 ruleset 보강: 서버 쪽에서 모든 세션을 막는 대안. 비용은 계정·classic 토큰·이 PC의 gh·git 로그인 전환이다(초안 세부 근거 3). ruleset 관리자 우회를 「PR로만」으로 바꾸는 더 싼 중간안은 문서 확인 전이다.
- 우편함 대기 `&`·`/dev/null` 차단 hook: 묶음 2 계획 12에서 같은 PreToolUse 층으로 다룬다.
