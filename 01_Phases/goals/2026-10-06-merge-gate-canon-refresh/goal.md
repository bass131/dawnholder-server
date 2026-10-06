# 병합 관문과 운영 정본 현행화

## 재개 지점

Rules의 목표다. 사용자가 범위 초안을 승인했고(아래 「요구사항 원천과 적용 결정」), 메인 `msg_e02ee97c2a7e`(2026-10-06T14:01:38Z)가 그 원문을 전달했다. 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: PR1은 `feat/merge-gate-20261006`(base `a47a027`)이다. PR2 branch는 PR1 병합 뒤 최신 main에서 만든다(제안 이름 `docs/canon-refresh-20261006`).
- 근거 폴더 E: `.backups/verification/2026-10-06-merge-gate-canon-refresh/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/astra-context.md)다. 받은 메시지 원시와 범위 초안 사본은 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh, 태그 `[Rules Astra]`, handle `term_af8ac4fc-29c5-4c97-8671-0279b10a9688`. Run은 `run_93a27bd491a9`, 회신 주소는 `run:run_93a27bd491a9`다. 이전 Rules goal의 Run·Task·Dispatch는 실행 권한이 아니다.
- **현재 위치**: 설계가 확정됐다(아래 「실측 뒤 설계」). 동작 계약은 [E/merge-gate-behavior-spec.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/merge-gate-behavior-spec.md) v2다. 선행 시험(`597839c`), 정본 문장(`4ae9622`), CLAUDE.md(`00d180b`, 메인)가 끝났다. 구현(`26cb8b3`)과 독립 검증이 끝났고 판정은 차단(D1·D2)이다(아래 「PR1 독립 검증」). 동작 계약 v2.1로 신규 Sol이 고쳤다(코드 `335bce6`, 문서 `56c29a8`, 아래 「PR1 결함 수정」). 한계 문구 보정(`97fa7b8`)도 끝났다. 재검증 판정은 차단(R1·R2)이고 D1~D5는 해결됐다(아래 「PR1 재검증」). 동작 계약 v2.2 수정이 끝났다(코드 `b9e1d2a`, 문서 `178ab0b`, 아래 「PR1 재검증 결함 수정」). 다음은 신규 Opus 두 번째 재검증 → PR1이다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 병합 관문 설계 실측
- [x] 병합 관문 선행 시험
- [>] 병합 관문 구현·검증
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

- PR1: `.claude/settings.json`(신규), `99_Tools/<목적 이름>/`·`99_Tools/<목적 이름>.Tests/`(신규), `.github/workflows/code-rules.yml`(시험 단계 하나), `99_Tools/README.md`(진입 한 줄), 필요하면 `.gitignore` 한 줄, AGENTS.md 「Git 권한」과 「공학 조건」의 hook 예외 문장, ORCA.md, goal-loop SKILL.md 「통합과 보고」, R-1 배치 변경의 파급인 RESUME.md 「세션 진입」 2단계와 session-handoff SKILL.md 25행(메인 `msg_162e0fefc66b`), CLAUDE.md(메인), 이 goal과 CURRENT Rules 줄.
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

### 설계 실측 결과

신규 `claude-opus-5-5` 작업자(Task `task_f860d83833a9`, Dispatch `ctx_73d3b8c57caa`)가 headless `claude -p` 6회로 쟀다. 보고서는 [E/design-probe/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/design-probe/report.md)다. 리드가 r2·r4·r5·r6 원시를 표본 대조했고 보고서와 일치했다. 모두 Claude Code 2.1.291, auto 모드, headless 조건의 관측이다. headless에는 확인 창이 없어서 ask가 이기면 거부로 끝난다. 대화형 확인 창의 실제 생략 여부는 완료조건 5에서 다시 본다.

| 실측 | 관측 |
|---|---|
| 1 PreToolUse allow 대 전역 ask | 넘지 못했다. debug에 「Hook returned 'allow' for Bash, but an ask rule … requires the full permission pipeline」가 찍힌 뒤 PermissionRequest 단계로 갔고 권한 거부로 끝났다(r2). deny 대조군은 hook에서 막혔다(r3). |
| 1 (d) PermissionRequest allow(계약 v1.1) | 실행됐다(r6, `permission_denials: []`). PermissionRequest 입력에는 `tool_use_id`가 없다. |
| 2 입력·환경 | hook은 Git Bash로 돌고 node가 실행된다. `CLAUDE_PROJECT_DIR`와 입력 `cwd`는 세션을 띄운 폴더다. 하위 폴더에서 띄우면 그 폴더의 `.claude/`에서만 프로젝트 settings를 찾는다(r4). UserPromptSubmit `prompt`는 입력 원문과 바이트 단위로 같다. |
| 3 하위 에이전트 | 하위 에이전트의 Bash에도 PreToolUse가 돈다. session_id는 상위와 같고 `agent_id`·`agent_type`으로 구분된다. 하위 에이전트 보고는 같은 prompt_id의 UserPromptSubmit(`<agent-message …>`로 시작)으로 상위에 다시 들어온다(r5). |
| 4 표식 | `--settings`의 env, 띄울 때의 셸 env, cwd 폴더의 표식 파일이 모두 hook에 닿는다. |

계약 보충 v1.1: 작업자의 공식 ask `msg_c6abad2711b8`(14:17:34Z)에 리드가 `msg_ed0a900bdce4`(14:18:06Z)로 조건 (d) 1회를 허용했다. 같은 명령·같은 세션 한정 방식이고 금지 조건은 그대로다. Orca CLI 1.4.221이라 질문은 수신 helper의 공식 ask 예외 밖이어서 사람 대조로 처리했다(E/design-probe-question1-manual-check.md). worker_done `msg_f23a3ecc67f2`는 수신 helper allowed/exit 0이다. 정산은 release retained/external_terminal/none이고, 같은 handle의 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/design-probe-*.json).

### 실측 뒤 설계

승인 범위 안에서 리드가 정한 것이다. 「메인 확인 대기」 줄은 메인 답 뒤에 확정한다.

- 메인 전용 폴더는 저장소의 별도 checkout(worktree) **루트**로 둔다. 프로젝트 settings를 세션 폴더의 `.claude/`에서만 찾기 때문이다(실측 2). 하위 폴더에서 띄운 세션에는 hook이 걸리지 않는다. 이 한계를 정본 문장에 적는다.
- 메인 식별은 메인 전용 checkout의 Git 제외 표식 파일(`.claude/state/` 아래)로 한다. `CLAUDE_PROJECT_DIR` 기준으로 읽는다(실측 4). 경로 비교는 구분자(`/`와 `\`)를 맞춘다. 표식은 사용자가 설정 단계에서 만든다. hook은 에이전트가 표식·기록 폴더에 쓰는 것을 막는다.
- 하위 에이전트 호출(`agent_id` 있음)은 메인 세션 안이어도 병합을 통과시키지 않는다(실측 3).
- 승인 기록 조건: prompt **전체**가 승인 문장 한 줄과 정확히 일치할 때만 기록한다. 앞뒤 공백만 허용하고, 여러 줄 중 한 줄이 맞는 경우는 받지 않는다(메인 `msg_2442c561dd9f` 3항). 형식은 `병합 승인: PR<번호> head <40자 hex>`로 고정한다(같은 메시지 2항). 근거는 r5의 실제 재진입 입력이다. 하위 에이전트 보고의 prompt 원문은 `<agent-message from="a3a3f93dba75a3113">`로 시작하는 10줄, 648자였다. 본문 줄은 두 칸씩 들여써져 있었고 `</agent-message>`로 끝났다(E/design-probe/raw/hooklog/r5.jsonl의 두 번째 UserPromptSubmit). 그래서 이 조건이면 그 안의 승인 줄은 기록을 만들지 못한다. background task 알림의 재진입 모양은 이번 실측에서 재지 않았다. 시험 반례로 덮는다. 입력 출처를 가르는 필드는 관측되지 않았다.
- 통과는 단독 병합 명령 형태만 받는다. 복합 명령·heredoc·`bash -c` 안의 병합 시도는 기록이 있어도 막는다. 복합 명령은 ask 규칙 매칭이 최선 노력이라, 통과를 허용하면 확인 단계 없이 실행될 수 있다.
- 확인 창 대신 승인: 질문 2 A(확인 창 없이 통과)는 PreToolUse allow만으로는 되지 않는다(실측 1). 그래서 PermissionRequest hook이 승인 기록과 정확히 맞는 단독 병합 명령에만 allow를 내고, 그 밖에는 결정을 내지 않는다(실측 1 d). 리드 질문은 `msg_7dbb6b6e9a89`였다. 메인 `msg_2442c561dd9f` 1항이 사용자에게 물었다. 이유는 이 hook이 확인 창을 대신 승인하는 장치라 AGENTS 공학 조건의 예외가 되기 때문이다. 사용자는 A를 골랐다(아래 「적용 중인 사용자 결정」). 병합이 아닌 다른 ask 대상 명령에는 어떤 경우에도 allow를 내지 않는다.
- 메인 확인: 위 설계(메인 식별 표식, 하위 에이전트 병합 차단, 단독 명령만 통과, 메인 폴더는 worktree 루트)는 승인 범위 안이다. 「하위 폴더에서 띄운 세션에는 hook이 없다」 한계는 정본 문장에 적는다(`msg_2442c561dd9f` 4항).
- 메인 전용 폴더와 병합 형태: 리드 질문 `msg_388f710eb4ba`에 메인 `msg_729105efa64c`(14:43:33Z, E/session/wait10-msg_729105efa64c.raw.txt)가 답했다.
  - 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/main-active`다. Core는 `C:/Dev/DawnHolder_Project`에 남고 별도 탭으로 옮긴다. 폴더·표식 생성과 첫 세션의 작업 공간 신뢰 창은 PR1 병합 뒤 사용자·메인이 한다.
  - 병합 명령은 `gh pr merge <번호> --merge --match-head-commit <40자>` 단독 형태다. `--delete-branch`는 붙이지 않는다.
  - R-1 파급: RESUME 「세션 진입」 2단계와 session-handoff SKILL 25행도 PR1에서 맞춘다. 작업 현황 탭의 `--worktree`는 main-active로 바꾼다. 같은 승인 항목(R-1 배치 변경)의 결과라 범위 확대가 아니라고 메인이 판단했다(`msg_162e0fefc66b`, 14:46:25Z). CLAUDE.md 「메인 세션 진입」의 Core 분할 문장은 메인이 PR1 CLAUDE.md 변경에 넣는다.
- 병합 실패 뒤 기록: 「설계 기본값」에서 미뤘던 질문의 답은 「되살리지 않는다」다. 동작 계약 v2는 PreToolUse 통과 때 `usedAt`을 쓰고 되돌리는 경로가 없다. 그래서 병합 명령이 실패하면(head 변경으로 409 등) 새 승인 문장이 필요하다. 정본 문장에 이 점을 적는다.
- PR1 병합 경로의 위험과 대비안: 구현이 rules-active에 `.claude/settings.json`을 만든 뒤 이 리드 세션에 hook이 바로 실리는지는 모른다(완료조건 5의 실측 항목). 바로 실리면 이 checkout에는 표식이 없어 리드 pane의 PR1 병합이 `not-main-checkout`으로 막힌다. 그러면 리드는 우회하지 않고 메인에 올린다. 메인이 `C:/Dev/DawnHolder_Project`에서 같은 단독 명령으로 병합한다. 사용자 승인과 head 재대조는 그대로다(메인 `msg_729105efa64c` 2항). 리드는 settings 생성 직후 무해한 명령 한 번으로 반영 여부를 재서 이 goal에 남긴다.

### 선행 시험

- 작성자: 신규 `claude-opus-5-5`(질문 4 A), 태그 `[Rules 검증자]`. 리드 pane 아래 vertical split이고 handle은 `term_c9566fb0-430e-4940-a546-c64f60c1d0e1`이다. 첫 화면은 Opus 5.5 xhigh, auto mode on, 선택창 없음이었다(E/tdd-first-screen.json). backend는 unknown이다.
- 계약: [E/tdd-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/tdd-contract.md) v1, SHA256 `5d9b0db3…`, 고정 입력은 HEAD `f1a75fd`와 동작 계약 v2(`d16918f6…`)다. 쓰기는 `99_Tools/MergeGate.Tests/`와 E/tdd/뿐이다. 경로 기계 확인은 E/tdd-contract-pathcheck.txt다.
- Task `task_788b4ccc4532`, Dispatch `ctx_6871e9ac3b77`. worker-start receipt는 input_accepted, turnStart observed다. receipt 파일 저장 시각은 14:36:18Z다(E/tdd-worker-start.json).
- 질문 1: 작업자 공식 ask `msg_93f732ba921f`(14:42:58Z)에 리드가 `msg_eaa7a91f2cee`(14:43:48Z)로 답했다. 출처는 사람 대조로 확인했다(E/tdd-question1-manual-check.md, CLI 1.4.221).
  - workflow 시험은 단계 본문을 실제 bash로 돌린다. 기존 Orca 시험과 같은 방식이다. 단계는 시험 부재·load 실패·실패에서 nonzero로 끝나고, command·stdout·stderr·exit를 `$RULES_OUTPUT/merge-gate-independent-tests/`에 남긴다. 동작 계약 밖의 리드 결정이라 구현 계약에도 넣는다.
  - PermissionRequest는 정확한 단독 병합 형태가 아닌 명령에 허용을 내지 않는다. `usedCommand`가 같은 위조 기록이 있어도 마찬가지다.
  - `state-write-failed`와 기록 쓰기 실패는 권한·파일 배치로 시험한다. root 환경은 이유를 출력하고 skip한다.
  - Node 24.15에서 `node --test <디렉터리>`는 시험을 찾지 않는다(작업자 실측). 실행 명령과 CI 단계는 `*.test.mjs` glob 형태로 쓴다.
- 질문 2: 작업자 공식 ask `msg_9b8170d7ed56`(14:55:39Z)에 리드가 `msg_6e39f24eb8d9`(14:56:01Z)로 답했다. 출처는 사람 대조로 확인했다. 동작 계약 v2 4절 Bash 3의 해석을 정했다. refspec 인자마다 목적지를 본다. 목적지는 맨 앞 `+`를 뗀 뒤 마지막 `:` 뒤이고, `:`가 없으면 인자 전체다. 목적지가 `main`이나 `refs/heads/main`이면 `push-main`이다. 그래서 `HEAD:refs/heads/main`도 막힌다. main을 포함한 다른 이름(`feat/main-fix` 등)은 결정 없음이다. 구현 계약에도 같은 문장을 넣는다.

- 결과: 작업자 worker_done `msg_7d8b46d20e3c`(15:02:44Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/tdd-worker-done-check-*.json). 보고서는 [E/tdd/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/tdd/report.md)다.
  - 시험 다섯 파일과 공용 fixture 한 파일, 합계 1,273줄, 74건이다. 동작 계약 §1~§6과 리드 답 두 건을 덮고 차단 코드 17개를 모두 덮는다.
  - 빨간 실행(`node --test 99_Tools/MergeGate.Tests/*.test.mjs`)은 통과 2, 실패 72, exit 1이다. 실패 원인은 hook 진입 부재 66, settings 부재 3, workflow 단계 부재 3이고, 시험 자체 오류는 0이다.
  - 덮지 않은 경우: 계약이 정하지 않아 기대값을 만들지 않은 경우가 있다(대문자 hex 명령, `--match-head-commit=<값>` 형태, 30분·120초 정확 경계, 손상된 기록 파일 등). 보고서 「미실행과 덮지 않은 것」에 있다. 초록 확인과 Linux CI는 미실행이다.
- 리드 R-2 표본 대조: 시험 파일 여섯 개의 SHA256이 red-run.txt 머리와 같았다. 같은 명령을 리드가 다시 돌려 74/2/72, exit 1, 진입 부재 66을 확인했다(E/lead-red-run-check.txt). `blocked-commands` 머리 주석과 `merge-command`의 compound 시험을 읽었다. 기대값은 동작 계약 문자열이고, 제품 판정을 복제한 계산은 없었다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/tdd-release.json, tdd-before-close.json, tdd-close.json). 시험은 리드가 `597839c`로 checkpoint commit했다. 구현 계약은 시험을 이 commit과 `git diff --exit-code`로 고정한다. 이 checkout은 autocrlf라 작업 파일 hash보다 Git 대조가 정확하다.

### PR1 정본 문장

- 작성자: 신규 `gpt-6.1-sol` max, 태그 `[Rules Sol]`. 리드 pane 아래 vertical split이고 handle은 `term_712a7b91-c89c-4a9f-b5d6-1ebb4983bae4`이다. 첫 화면은 Codex v0.160.1, GPT-6.1-Sol max였다. 권한 표시는 기존 Codex 설정 그대로인 Full Access였고 선택창은 없었다(E/docs-first-screen.json). backend는 unknown이다.
- 계약: [E/docs-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/docs-contract.md) v1, SHA256 `71ba479e…`, 고정 입력은 HEAD `e37ac7a`와 동작 계약 v2다. 경로 기계 확인은 E/docs-contract-pathcheck.txt(14:49:26Z)다. 선행 시험 작성자와 쓰는 파일이 겹치지 않아 병렬로 돌린다.
- Task `task_171a81f24c1b`, Dispatch `ctx_0b8d6f802321`. receipt는 input_accepted, turnStart observed다(E/docs-worker-start.json).
- CLAUDE.md: 메인에 쓰기 창을 열었다(`msg_f7e308d60e6d`, 14:50:10Z). 메인이 `msg_fa7b6383881e`(14:52:44Z)로 쓰기 종료를 알렸다. 리드가 diff를 확인하고 `00d180b`로 CLAUDE.md만 커밋했다.
  - 바뀐 곳은 셋이다. 메인 역할에 병합 실행(ORCA `#merge-gate` 링크)을 넣었다. 메인 세션 진입의 배치를 main-active와 Core 별도 탭으로 바꿨다. 대시보드 결정 응답이 승인 기록을 만들지 않는다는 점과 병합 절차를 적었다.
  - bytes는 8199 → 8197이고 numstat은 9/9다. sha256 앞 16자 `e257703e080e5886`은 리드가 다시 재서 일치했다. 늘어난 만큼 같은 파일의 중복 문장을 줄였다. 줄인 문장은 외부 팀원 줄의 절차 비적용 반복, 터미널 알림의 작업 중 금지 반복, 대시보드 필드 나열이다. 같은 뜻은 링크한 AGENTS·session-handoff·대시보드 README에 있다.
  - 메인의 맥락 메모는 저장소 밖 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-06/claude-md-merge-gate-context.md`다.
  - `#merge-gate` anchor는 정본 문장 작성자가 ORCA에 만든다. 생기는지 결과와 대조하고, CLAUDE.md도 독립 검증 대상에 넣는다(메인 요청).
- 질문 1: 작업자 공식 ask `msg_e68397743197`(14:50:56Z)에 리드가 `msg_c8cd55372204`(14:51:22Z)로 답했다. 출처는 사람 대조로 확인했다(E/docs-question1-manual-check.md). 답의 요지는 hook 사실과 운영 규칙을 갈라 쓰는 것이다. hook은 방식 플래그 셋 중 하나를 형태로 받는다(동작 계약 v2 4절). 운영 명령은 `--merge` 하나다. 동작 계약 보정은 필요 없다.
- 질문 2: 작업자 공식 ask `msg_a683a99bb68e`(14:57:17Z)가 길이 초과를 알렸다. 첫 초안은 ORCA 258줄이었다. 규칙 문서 합계는 95,188 bytes로 착수 때 92,500보다 2,688 bytes 많았다. R-8 적용 시점과 관찰 기록 두 절을 이미 옮긴 뒤의 수치다. 작업자는 PR2 후보 세 문단도 옮기자고 물었다. 리드는 `msg_36d4e1629d5b`(14:57:56Z)로 답했다.
  - 먼저 중복을 줄인다. 상세는 ORCA `#merge-gate` 한 곳에 두고, 다른 문서는 짧은 문장과 링크만 둔다. 공학 조건 예외의 조건은 줄이지 않는다.
  - 그래도 넘치면 후보를 R-6 D1a → R-3 1.4.217 → R-5 실패 이력 순서로 필요한 만큼만 쓴다.
  - 셋을 다 써도 넘치면 다시 묻는다.
  - 이유는 세 후보가 PR2에도 필요한 감축 여지이기 때문이다.
- 중간 보고 `msg_21fe57cd4ac1`(15:03:59Z, 수신 helper allowed): 중복을 줄인 뒤에도 넘쳐 세 후보를 모두 썼다. 순서는 R-6 → R-3 → R-5였다. 결과는 ORCA 249줄, 다섯 문서 92,478 bytes(착수 92,500, -22)다. 수치는 완료 보고 뒤 리드가 다시 잰다.
  - **PR2 영향**: 범위 초안이 정한 ORCA 감축 후보(R-3 1.4.217, R-5 실패 이력, R-8 적용 시점, R-6 D1a, 관찰 기록 두 절)는 PR1이 모두 썼다. PR2는 ORCA에 Orca 절차 다섯 가지와 대기 정책 등을 더한다. 그래서 PR2 계획 때 새 감축 방법을 메인과 정해야 한다. 완료조건 7은 그대로 둔다. PR1 보고에 함께 올린다.
- 결과: worker_done `msg_55d1b979a7ca`(15:20:48Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/docs-worker-done-check-*.json). 보고서는 [E/docs/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/docs/report.md)다. 리드가 `4ae9622`로 커밋했다.
  - 바뀐 곳: AGENTS 「Git 권한」(병합 주체·승인 문장·금지 대상)과 「공학 조건」(PermissionRequest hook 예외 한 문장), ORCA 새 절 `#merge-gate`와 R-1 배치, goal-loop 「통합과 보고」 한 구절, RESUME 2단계, 세션 인계 스킬 25행.
  - 수치(리드 재측정): ORCA 249줄이다. 다섯 문서 합계는 92,500 bytes로 착수 때와 같다. 파일별로는 AGENTS +798, goal-loop +116, RESUME +41, 세션 인계 스킬 -4, ORCA -951이다. CLAUDE.md는 메인 몫으로 따로 재서 8,199 → 8,197이다.
  - 옮긴 원문: 여섯 블록이 기준 `e37ac7a`의 ORCA 행과 글자까지 같다(E/lead-moved-check.txt). 리드가 아래 「ORCA에서 옮긴 서술」에 넣었다. 링크 11개 중 8개가 열린다. 나머지 3개는 Git 제외 로컬 백업 파일의 부재이고, 원래 ORCA에서도 이 checkout 기준으로 없던 것이다(E/lead-goal-moved-links.txt).
  - 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/docs-release.json, docs-before-close.json, docs-close.json).
- 리드 실사에서 독립 검증으로 넘길 점:
  1. R-3 축약: 1.4.217 문단을 옮기면서 현행 규칙 일부를 한 문장으로 줄였다. 「help 확인과 실제 호출 실증의 구분」, 「다른 버전 지원을 주장하지 않음」 같은 세부가 빠졌다. 현행 규칙이 보존됐는지 대조한다.
  2. ORCA `#merge-gate`의 「세션을 다시 열면 이전 기록을 쓸 수 없다」는 리드 계약에서 나온 문장이다. `--resume`가 session_id를 유지하는지는 실측하지 않았다. 실측하거나, 표현을 「다른 session_id」 기준으로 고친다.
  3. ORCA 새 절은 하위 에이전트 병합 차단을 직접 적지 않는다. AGENTS 「Git 권한」에는 있다. 30초 탐색으로 찾을 수 있는지 본다.
  4. 공학 조건 예외 문장의 조건이 hook 실제 동작과 같은지 대조한다(메인 `msg_895f7ffa4512`).

### 구현

- 작업자: 신규 `gpt-6.1-sol` max, 태그 `[Rules Sol]`. 리드 pane 아래 vertical split이고 handle은 `term_7d282bd9-bc45-4cfc-a10f-558f23845cfa`다. 첫 화면은 Codex v0.160.1, GPT-6.1-Sol max였다. 권한 표시는 기존 Codex 설정 그대로인 Full Access였고 선택창은 없었다(E/impl-first-screen.json). backend는 unknown이다.
- 계약: [E/impl-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/impl-contract.md) v1, SHA256 `ad1bf52c…`이다.
  - 고정 입력은 HEAD `8a28713`, 동작 계약 v2, 선행 시험 commit `597839c`다. 시험은 `git diff --exit-code`로 대조한다.
  - 명세 보충(리드 결정)도 계약에 넣었다. 손상된 기록은 기록 없음으로 본다. 예상하지 못한 예외는 PreToolUse에서 `invalid-input`으로 막는다. main push 목적지 판정과 PermissionRequest의 단독 병합 형태 조건도 넣었다.
  - `.claude/settings.json`은 마지막에 쓰고 쓴 즉시 리드에게 알린다. 이 리드 세션에 hook이 바로 실릴 수 있어서다. 미완성 hook이 리드 명령을 막지 않게 하고, 반영 여부를 그 시점에 재기 위한 조건이다.
  - 경로 기계 확인은 E/impl-contract-pathcheck.txt(15:23:54Z)다.
- Task `task_414ac0f46898`, Dispatch `ctx_0fde254166f7`이다. receipt는 input_accepted, turnStart observed다(15:24:17Z, E/impl-worker-start.json).
- 질문 1(동시 호출): 작업자 공식 ask `msg_53de60036b2e`(15:33:03Z)는 hook 시험 67건이 통과했다고 알렸다. 그리고 같은 세션의 병합 PreToolUse 두 개가 동시에 돌 때를 물었다. 읽은 뒤 rename하는 방식만으로는 같은 승인이 두 번 통과할 수 있다. 출처는 사람 대조로 확인했다(E/impl-question1-manual-check.md). 리드는 `msg_603a060484d9`(15:33:36Z)로 잠금 없이 순차 호출 계약으로 두라고 답했다.
  - 이유 1: 두 번 통과해도 같은 PR·같은 head의 병합이 두 번 시도될 뿐이다. 두 번째는 GitHub이 이미 병합된 PR이라 거부한다.
  - 이유 2: 잠금이 남으면 모든 병합이 막힌다. 상태 폴더는 에이전트 쓰기가 막힌 곳이라 사람이 손으로 치워야 한다.
  - 코드에 이유 주석을 두고 보고서 남은 위험에 적게 했다. 독립 검증이 이 판단을 다시 본다.
- 질문 2(동시 호출의 다른 PR): 작업자 공식 ask `msg_7d91dc57ccce`(15:35:57Z)가 더 넓은 경우를 짚었다. 기록 파일은 세션별 배열을 통째로 rename한다. 그래서 다른 PR 두 개가 동시에 통과하면 늦게 쓴 쪽이 다른 PR의 `usedAt`을 null로 되돌릴 수 있다. 리드는 `msg_a504a6a2452c`(15:36:25Z)로 잠금 없는 순차 계약을 유지하고 한계를 정확히 적으라고 답했다.
  - 영향 범위는 사용자가 승인한 그 PR·head의 기록뿐이다. 그 PR이 이미 병합됐으면 GitHub이 재시도를 거부한다. 첫 병합이 실패한 PR이라면 30분 안에 새 승인 없이 한 번 더 시도될 수 있다. 정본 「실패하면 새 승인이 필요하다」와 어긋나는 유일한 경로다.
  - 적을 곳은 코드 주석, 보고서 남은 위험, README 한 줄이다. ORCA 한계 목록에 넣을지는 독립 검증 뒤 리드가 정한다.
- settings 작성과 세션 중 반영 실측(완료조건 5의 실측 항목 첫 관측): 작업자가 hook 시험 67건 통과 뒤 마지막으로 `.claude/settings.json`을 썼다(mtime 15:38:28Z, 알림 `msg_f332a932d61e`, 수신 helper allowed).
  - 리드 세션은 settings가 없을 때 시작했다. 1분 안에 상태 폴더 경로가 든 무해한 `ls`가 `merge-gate:protected-path`로 막혔다. Claude Code 2.1.291에서 프로젝트 hook이 세션을 다시 시작하지 않아도 실렸다.
  - 원시는 [E/hot-reload-probe.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/hot-reload-probe.md)다. 한 세션 한 번의 관측이다.
  - 결과로 이 리드 pane의 PR1 병합은 `not-main-checkout`으로 막힌다. 메인 대비안(`msg_729105efa64c` 2항)으로 간다.
  - 메인에 알렸다(`msg_3a305afc406c`). 순서 주의도 함께 보냈다. 메인 checkout(Core branch)이 PR1 뒤 main을 받으면 메인 세션에도 hook이 실린다. 그런데 그 checkout에는 표식이 없다. 그래서 main-active와 표식을 먼저 만든 뒤 병합해야 한다.
  - 리드 운영: 이 세션의 Bash에 상태 폴더 경로나 병합 명령 문자열을 넣지 않는다. 검색은 Grep·Read, 그런 문자열이 든 기록은 Write 도구로 쓴다.
  - 메인 확인 `msg_2b0047c0429e`(15:39:57Z): PR1은 대비안으로 병합한다. 메인 checkout은 PR1 병합 전까지 main을 받지 않는다. 병합 뒤 순서는 다섯 단계다.
    1. main-active를 만든다.
    2. 표식을 만든다.
    3. 메인 세션을 다시 연다.
    4. 완료조건 5의 적용 확인을 한다.
    5. 그 뒤에 다른 checkout들이 main을 받는다.
  - 리드 진입 안내에 한 줄을 더할지는 PR2에서 판단한다. 세션 중 반영이 한 번의 관측이라는 한계는 그대로 둔다.
- 결과: worker_done `msg_25136896f256`(15:45:20Z, outcome succeeded)은 수신 helper allowed/exit 0이다(E/impl-worker-done-check-*.json). 보고서는 [E/impl/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/impl/report.md)다. 리드가 `26cb8b3`으로 커밋했다.
  - 제품 파일과 줄 수: `99_Tools/MergeGate/merge-policy.mjs` 211(순수 판정), `approval-store.mjs` 45(기록 읽기·임시 파일+rename 쓰기), `claude-hook.mjs` 76(진입), `.claude/settings.json` 36, workflow +25, README +18이다.
  - 선행 시험은 `597839c`와 같다(`git diff --exit-code`).
- 리드 R-2 표본 대조:
  - 같은 명령을 다시 돌렸다. MergeGate 74/74, Orca 22/22, 둘 다 exit 0이었다(E/lead-green-run-check.txt).
  - 세 제품 파일과 README·workflow diff를 읽었다. 순수 판정·기록·진입이 나뉘어 있다. 판정 순서는 동작 계약 4절 순서다. 동시 호출 한계 주석은 rename 옆에 있다.
  - **독립 검증으로 넘길 의심점(리드 넘김 5)**: 병합 시도 판정은 `gh`·`pr`·`merge`가 공백만 두고 붙어 있을 때만 잡는다. 그래서 `gh pr -R <repo> merge …`처럼 사이에 플래그가 낀 형태는 병합 시도로 보지 않는다. main push 판정도 같아서 `git -C . push origin main`을 잡지 않는다. 두 형태 모두 전역 ask 규칙(`gh pr merge*` 접두 일치)에도 걸리지 않는다. 동작 계약 4절의 「세 낱말이 차례로 나옴」이 「붙어 있음」인지 「순서대로 나옴」인지 애매하다. 검증자가 재현하고 판정한다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/impl-release.json, impl-before-close.json, impl-close.json).

### PR1 독립 검증

- 검증자: 신규 `claude-opus-5-5`, 태그 `[Rules 검증자]`. 검증자 모델 시범의 보안 경계 예외에 해당한다. 리드 pane 아래 vertical split이고 handle은 `term_d461bb14-d265-4aad-a8f8-be1bfbef4bfe`다. 첫 화면은 Claude Code v2.1.291, Opus 5.5 xhigh, auto mode on이었고 선택창은 없었다(E/verify-first-screen.json). backend는 unknown이다.
- 계약: [E/verify-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/verify-contract.md) v1, SHA256 `a7d44cfe…`, 고정 입력 HEAD는 `658e640`이다. 강 등급이다. 필수 항목은 세 가지다.
  - diff·근거 실사.
  - 독립 보완 시험 작성·실행.
  - 실제 진입 실행. 막히는 경로는 검증자 세션에서 없는 PR 번호로 잰다. 통과 경로는 원격 없는 임시 저장소에서 headless 두 턴으로 잰다. 두 번째 턴은 `--resume`으로 이어서, session_id 유지 여부도 함께 잰다.
  - 리드 넘김 다섯 가지, 동시 호출 결정, 정본 문장과 CLAUDE.md 대조도 포함한다. 경로 기계 확인은 E/verify-contract-pathcheck.txt다.
- Task `task_219e1ceb840d`, Dispatch `ctx_38275325f17c`다. receipt는 input_accepted, turnStart observed다(15:47:42Z, E/verify-worker-start.json).
- 판정: worker_done `msg_d696162ce32a`(16:11:32Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/verify-worker-done-check-output.json). 판정 원문은 [E/verify/verdict.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/verify/verdict.md)이고 결론은 **차단(FAIL), 결함 D1·D2**다.
  - 수행 범위: 고정 시험 74/74와 Orca 22/22를 다시 돌렸다. 보완 시험 두 파일 12건을 새로 써서 86/86이 됐다. 실제 진입은 검증자 세션의 차단 2건(`not-main-checkout`, `push-main`)이다. 원격 없는 임시 저장소의 headless 두 턴으로 통과 경로도 쟀다. 기록 생성, PreToolUse allow, ask 규칙, PermissionRequest allow, 실행 순서였고 gh는 원격이 없어 실패했다.
  - D1(차단): 낱말 사이에 옵션이 낀 형태를 놓친다. `git -C <경로> push origin main`, `git -c k=v push …`, `git --no-pager push …`, `gh pr -R <repo> merge …`, `gh pr --repo=<repo> merge … --admin`이 모두 결정 없음이다. 전역 ask 규칙도 접두 일치라 못 잡는다. ORCA 「모든 세션 main push 차단」과 어긋난다. 리드 넘김 5가 재현됐다.
  - D2(차단): `--resume`은 session_id를 유지한다. 1턴 기록을 2턴이 소비하고 명령이 실행됐다(E/verify/raw/real-entry-log.txt). ORCA 「세션을 다시 열면 이전 기록을 쓸 수 없다」가 사실과 다르다.
  - D3(비차단): 파일 도구 경로의 `./`·`../`·`//`를 정규화하지 않아 보호 폴더 Write가 결정 없음이다.
  - D4(비차단): R-3 축약이 현행 세부 세 가지를 잃었다. 「ask/reply help 양쪽 확인」, 「help 확인과 실제 호출 실증 구분」, 「다른 버전 지원 주장 안 함」이다. 「위 … receipt 조건」은 위에 없는 조건을 가리킨다.
  - D5(비차단): 한계 문장이 경로 하나를 빠뜨렸다. 승인 문장 제출과 병합 통과가 겹치면 소비된 기록이 되살아난다(임시 폴더 20회 중 9회).
  - 관찰: O3 `git push origin @`(main 위) 결정 없음, O8 hook 실행 실패 시 보호 없음(추론), O9 ORCA 절에 「(하위 에이전트 제외)」, O10 SendMessage 전달 모양 미측정, O11 RESUME 전환 문구. 가독성 지적은 둘이다. 「맨 앞이 아니면 compound」 규칙이 특수문자 검사와 한 조건식에 섞였다. 같은 형태 판정을 두 번 부른다.
- 리드 R-2 표본 대조: observe.jsonl의 A행(낱말 사이 옵션)과 real-entry-log.txt의 2턴 블록을 직접 읽었고 판정문과 같았다. 검증자 시험 두 파일은 리드가 `5a43d42`로 커밋했다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/verify-release.json, verify-before-close.json, verify-close.json).
- **리드 결정(범위 안 결함, 루프에서 수정)**: D1~D5는 모두 승인된 PR1 범위와 완료조건 3 안의 결함이다. 그래서 메인 결정을 기다리지 않고 고치며 메인에는 알린다. 같은 산출물의 수정은 이번이 첫 번째다.
  - D1은 판정을 넓힌다(검증자 권장안). 한계 수용안은 쓰지 않는다. `git -C`는 절대 경로를 권하는 환경의 보통 관용구라서다. 넓힌 판정에 걸린 형태는 형태 조건에서 `bad-form`으로 막히므로 메인의 정식 명령에는 영향이 없다. 같은 비용으로 대문자·경로·`.exe` 명령 낱말과 O3 `@`도 덮는다.
  - 동작 계약은 [E/merge-gate-behavior-spec-v2.1.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/merge-gate-behavior-spec-v2.1.md)다(SHA256 `ce113621…`). v2는 고정 입력 이력으로 그대로 둔다. 바뀐 곳은 4절 명령 낱말·하위 명령 정의(D1), push `@`(O3), 통과 조건 1·3·4의 명시, 파일 도구 경로 정규화(D3), 6절 동시 호출 한계(D5)다.
  - 문서는 D2 문장, D4 세 구절 복원, D5와 O8 한계, O9, 넓힌 판정에 맞춘 우회 한계 문구를 고친다. 늘어난 bytes는 같은 PR의 다섯 문서에서 현행 규칙을 잃지 않고 줄인다(완료조건 7). 못 줄이면 작업자가 멈추고 리드가 메인에 올린다.
  - 순서: 신규 Sol 한 명이 코드·문서를 고친다. 기존 시험 86건은 바꾸지 않고 통과해야 한다. 그다음 신규 Opus 재검증자가 v2.1 시험을 쓰고 D1~D5와 실제 진입을 다시 판정한다.
  - O10은 완료조건 5 적용 확인 때 잰다. O11·O2·O6은 PR2 후보로 둔다.
- 메인 알림: `msg_daf6153a050b`(16:19:33Z, status). 메인 pane이 빈 prompt여서 확인 안내를 한 번 넣었다.

### PR1 결함 수정

- 작업자: 신규 `gpt-6.1-sol` max, 태그 `[Rules Sol]`. 리드 pane 아래 vertical split이고 handle은 `term_1e768a4f-a1c8-418a-8595-73ffb74e7bc3`다. 첫 화면은 Codex v0.160.1, GPT-6.1-Sol max, Full Access·never로 이전 작업자들과 같았다. 선택창은 없었다(E/fix-first-screen.json). backend는 unknown이다.
- 계약: [E/fix-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix-contract.md) v1, SHA256 `c1058cce…`이다. 고정 입력은 HEAD `eb10c80`, 동작 계약 v2.1, 판정 E/verify/verdict.md다.
  - 쓰기 허용은 MergeGate 세 제품 파일, README 「병합 관문」 절, ORCA다. 시험 폴더는 쓰지 않는다. 기존 86건이 바뀌지 않고 통과해야 한다.
  - bytes는 ORCA 41,629 이하로 맞춘다. 같은 절의 중복 합치기 → 역사 서술 옮기기 순서다. 그래도 모자라면 멈추고 묻는다.
  - 경로 기계 확인은 E/fix-contract-pathcheck.txt(16:22:32Z)다.
- Task `task_64f9b3228b87`, Dispatch `ctx_96a40f8ffe73`이다. receipt는 input_accepted, turn_started다(E/fix-worker-start.json).
- 중간 보고 `msg_b5eb2054ebd7`(16:37:03Z, status, 수신 helper allowed): 같은 절 축약 뒤에도 ORCA가 227 bytes 넘어 R-7 M-1 관찰 두 문장을 옮겼다. 리드가 아래 「ORCA에서 옮긴 서술」에 넣었다(대조 E/lead-moved-check-fix.txt).
- 결과: worker_done `msg_69dc5a4e3d2f`(16:51:05Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/fix-worker-done-check-output.json). 보고서는 [E/fix/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix/report.md)다. 리드가 코드 `335bce6`(MergeGate 세 파일·README)와 문서 `56c29a8`(ORCA)로 나눠 커밋했다.
  - 코드: 명령 낱말·하위 명령 판정(`commandWords`·`findSubcommands`), 파일 도구 경로 정규화(`protectedFilePath`), `@` 조회, 맨 앞 명령 낱말 조건의 분리와 이유 주석, 값 옵션 목록 주석, `=` 형태 사유, 중복 호출 이유 주석, 동시성 주석 확장이다.
  - 문서: ORCA 229(D2), 227(O9), 233(`HEAD`·`@`), 235(D1·D5·O8), 83(D4)과 bytes를 맞춘 79·81·83·201의 축약이다. README 한계 줄과 코드 표를 v2.1에 맞췄다.
- 리드 R-2 표본 대조:
  - 같은 명령을 다시 돌렸다. MergeGate 86/86, Orca 22/22, 둘 다 exit 0이었다. 기존 시험은 `eb10c809`와 같다. 커밋된 ORCA는 249줄·41,627 bytes이고 다섯 문서 합은 92,498이다(E/lead-fix-green-check.txt).
  - 코드 diff 전체와 문서 word-diff를 읽었다. 보고서 「축약·이관 대조」 표의 원문·바꾼 문장·남은 위치가 실제 diff와 같았다. 79·81·83행 축약에서 조건·주체·예외·링크가 빠진 곳은 찾지 못했다. D4의 세 구절과 receipt 대조가 83행에 있다.
  - 자체 점검 원시 E/fix/self-check.jsonl 89행을 읽었다. 첫 판정 A행 가운데 결정 없음이던 11개가 모두 차단(`bad-form`·`forbidden-flag`·`push-main`)이 됐다. 정식 단독 명령의 통과·소비·확인 창 허용·두 번째 사용 차단은 그대로였다.
  - **리드 발견 1(문서, 보정 필요)**: 235행 한계의 일반 문구 「문자열 판정의 의도적 우회」가 「별칭·스크립트 파일·변수 속 명령 미판정」으로 바뀌었다. 정본 어디에도 일반 문구가 남지 않아 완료조건 3의 「문자열 판정의 의도적 우회 가능성」을 채우지 못한다. 원인은 수정 계약 「문서」 3의 문장이 맞추기와 바꾸기 중 어느 쪽인지 모호했던 것이다(리드 귀속).
  - **리드 발견 2(재검증 넘김)**: 낱말은 공백으로 나눈다(동작 계약 v2.1 4절). 그래서 `git -C "공백 든 경로" push …`처럼 공백이 든 따옴표 값은 쪼개져 push로 보이지 않는다. 따옴표를 존중하게 나누면 `bash -c "…"` 안의 명령을 잃으므로 이번 PR에서는 계약 범위의 한계로 두고, 위 일반 문구의 예시에 넣는다. 재검증자가 이 판단을 다시 본다.
  - 리드 넘김(재검증): 231행이 「운영은 `--merge`만 쓴다」를 따로 적지 않고 운영 명령 예시에만 남겼다. 읽는 사람에게 충분한지 판정을 받는다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/fix-release.json, fix-before-close.json, fix-close.json).
- 리드 결정: 발견 1은 신규 Sol의 짧은 문서 보정으로 고친다. 같은 산출물(ORCA 병합 관문 절)의 두 번째 수정이다. 코드와 동작 계약 v2.1은 바꾸지 않는다. 그 뒤 재검증으로 간다.
- 한계 문구 보정 작업자: 신규 `gpt-6.1-sol` max, 태그 `[Rules Sol]`, handle `term_33fe7dc4-c19e-4111-b628-3f9c53df39fd`(리드 pane 아래 vertical split). 첫 화면은 이전 작업자와 같았고 선택창은 없었다(E/fix2-first-screen.json). backend는 unknown이다.
  - 계약: [E/fix2-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix2-contract.md) v1, SHA256 `d72f8cc6…`, 고정 HEAD `8317762`. 쓰기는 ORCA뿐이다. 235행에 일반 한계와 예시(별칭·스크립트·변수, 공백 든 따옴표 값)를 쓴다. ORCA는 41,629 bytes 이하로 맞추고, 이미 축약한 행은 다시 줄이지 않는다. 경로 기계 확인은 E/fix2-contract-pathcheck.txt(16:55:07Z)다.
  - Task `task_951d23eb65d4`, Dispatch `ctx_4177b04981c9`다. receipt는 input_accepted, turnStart observed다(E/fix2-worker-start.json).
  - 결과: worker_done `msg_e9dc1fad1691`(17:09:58Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/fix2-worker-done-check-output.json). 보고서는 [E/fix2/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix2/report.md)다.
    - 235행 한계가 「문자열 판정의 의도적 우회 가능(별칭·스크립트 파일·변수 속 명령, 공백 든 따옴표 값의 낱말 분리 …)」이 됐다. bytes는 R-2 66·74행의 같은 절 중복을 합쳐 맞췄다. ORCA는 249줄·41,623 bytes이고 다섯 문서 합은 92,494다.
    - 리드 표본 대조: ORCA word-diff를 읽었다. 바뀐 행은 66·74·235뿐이다. R-2의 조건(보고 승인 전 판정 원문 읽기와 원천 표본 대조, 기록 형식, 전부/없음 주장, 독립 열거, 시범 등급 대조, 실패 분류 표본, 검증자 전수 분류 대체 금지, 불일치 즉시 보고)과 링크가 남아 있다.
  - 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/fix2-release.json, fix2-before-close.json, fix2-close.json).
  - 리드가 문서 `97fa7b8`로 커밋했다.

### PR1 재검증

- 검증자: 신규 `claude-opus-5-5`, 태그 `[Rules 검증자]`(검증자 모델 시범의 보안 경계 예외). 리드 pane 아래 vertical split이고 handle은 `term_94817359-840a-4465-816b-3c962b8cf99b`다. 첫 화면은 Claude Code v2.1.291, Opus 5.5 xhigh, auto mode on으로 첫 검증자와 같았다. 선택창은 없었다(E/reverify-first-screen.json). backend는 unknown이다.
- 계약: [E/reverify-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/reverify-contract.md) v1, SHA256 `5d6223b7…`, 고정 HEAD `d1b9038`이다. 강 등급이다.
  - 대상은 첫 판정 D1~D5와 O3·O8·O9, 가독성 지적 둘의 해결 여부와 PR1 전체의 보존이다.
  - 필수 항목은 v2.1 회귀 시험(새 파일), 실제 진입 4a(낱말 사이 옵션 형태 포함)·4b(headless 두 턴), bytes를 줄인 곳의 현행 규칙 보존 대조다.
  - 리드 넘김 셋: 공백 든 따옴표 값의 한계 판단, 231행 「운영은 `--merge`만」, 235행 일반 한계 문구.
  - 경로 기계 확인은 E/reverify-contract-pathcheck.txt(17:11:35Z)다.
- Task `task_d3d0d79e85b5`, Dispatch `ctx_f3b9568303f7`이다. receipt는 input_accepted, turnStart observed다(E/reverify-worker-start.json).
- 질문 1: 공식 ask `msg_fb5d3aea0d05`(17:18:35Z). 출처는 사람 대조로 확인했다(E/session/reverify-question1-manual-check.md). 계약 허용 실행 밖 무해 관측 두 건을 물었다.
  - (1) Monitor 도구로 상태 폴더 경로만 든 echo 한 번. hook 등록 matcher(Bash와 파일 도구) 밖의 셸 실행 도구가 관문을 거치는지 보는 관측이다.
  - (2) 값 옵션이 pr과 merge 사이에 낀 형태의 로컬 `--help` 한 번. gh가 그 형태를 merge 하위 명령으로 해석하는지 보는 관측이다.
  - 리드 답 `msg_895bc2d0d224`(17:19:46Z): 둘 다 조건부 허용(계약 v1 보충). (1)은 다른 셸 실행 도구(예: PowerShell 도구)에도 같은 echo를 도구마다 한 번 허용했다. 병합·push·gh는 그 길로 실행하지 않는다. hook을 거치지 않으면 결함 후보로 판정한다. (2)는 실행 전 remote 원시를 남기고, help가 아니면 즉시 멈춘다.
- 판정: worker_done `msg_9e7e4184b6e4`(17:34:57Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/reverify-worker-done-check-output.json). 판정 원문은 [E/reverify/verdict.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/reverify/verdict.md)이고 결론은 **차단(FAIL), 새 결함 R1·R2**다.
  - 해결 확인: 첫 판정 D1~D5, O3·O8·O9, 가독성 지적 둘이 모두 해결됐다. 고정 86건에 새 회귀 시험 8건(`command-words.test.mjs`, 수정 전 제품에서 6/8 실패)을 더해 94/94, Orca 22/22다. 실제 진입 4a 세 건(`not-main-checkout`·`bad-form`·`push-main`)이 막혔고 4b headless 두 턴이 통과 경로대로 돌았다. 이관 원문 일곱 블록이 goal과 글자까지 같다. ORCA 249줄·다섯 문서 92,494 bytes다.
  - R1(차단): 셸 도구 `Monitor`가 hook matcher 밖이다. 같은 echo가 Bash에서는 `protected-path`로 막히고 Monitor에서는 실행됐다. 같은 길로 병합·main push도 hook을 비켜 간다고 추론한다. 이 세션에 PowerShell 도구는 없었다.
  - R2(차단): subshell·명령 치환 안과 줄 이음 뒤의 main push를 놓친다. `(cd r && git push origin main)`, `out=$(git push origin main)`, `(git push --all)`, 줄 이음 refspec이 결정 없음이다. push 인자 읽기가 `)`와 줄 이음을 다루지 않아서다.
  - R3(비차단): `-R`·`--repo` 밖의 값 옵션이 `pr`과 `merge` 사이에 끼면 놓친다. gh 2.92.0은 그 형태를 merge로 해석한다(로컬 help 관측). git `--attr-source`도 같다. 검증자는 의도적 우회 문구에 든다고 봤다.
  - R4(비차단): `gh api` 판정만 넓힌 명령 낱말을 쓰지 않는다(`GH api`, `gh.exe api`).
  - 리드 넘김 판정: 공백 든 따옴표 값은 문서화된 한계와 동작이 맞다. 다만 값 건너뛰기에서만 따옴표를 존중하면 `bash -c` 안을 잃지 않는다(O-R1). 231행은 충분하다. 235행 일반 문구는 들어갔다.
  - 관찰: O-R2 Bash의 상태 폴더 문자열 비교가 `//`·`/./`를 접지 않는다. O-R6 hook·settings 파일 자체를 고치면 관문이 바뀐다. O-R7 예약 prompt(CronCreate·ScheduleWakeup)가 줄 그대로 UserPromptSubmit에 오면 에이전트가 스스로 승인 기록을 만들 수 있다.
- 리드 R-2 표본 대조: E/reverify/real-entry-blocked.md 「보충 관측 1」과 observe.jsonl의 P·G·GA행을 직접 읽었고 판정문과 같았다. 재검증자 시험은 리드가 `b2e3718`로 커밋했다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/reverify-release.json, reverify-before-close.json, reverify-close.json).
- **리드 결정(범위 안 결함, 루프에서 수정)**: R1·R2는 완료조건 3의 「정본 문장이 hook 동작과 맞음」을 깨는 범위 안 결함이라 고친다. 메인에는 알린다.
  - 코드로 막는 것: R1(matcher에 `Monitor`, Monitor의 병합은 늘 `non-bash-merge`), R2(줄 이음 접기와 단순 명령 경계를 쓰는 낱말 규칙 하나), R4(gh api에 명령 낱말), O-R1(값 옵션의 따옴표 값 건너뛰기), O-R2(겹친 구분자·`/./` 접기), git `--attr-source`, O-R7(prompt 도구 CronCreate·ScheduleWakeup·SendMessage·RemoteTrigger의 입력에 `병합 승인:`이 있으면 `approval-injection`).
  - O-R7을 넣은 이유: 이 관문이 막으려는 위험이 에이전트의 스스로 승인이다. 기존 「다른 터미널로 승인 문장 주입 차단」을 같은 층에서 넓히는 일이라 비용이 작다. 실제 전달 모양은 완료조건 5에서 그대로 잰다.
  - 한계로 적는 것: R3의 gh 쪽(값 옵션을 `merge` 앞으로 옮기는 형태는 의도적 우회), matcher 밖 도구, hook·settings 자체 수정(O-R6). `pr` 뒤 아무 곳의 `merge`를 병합 시도로 보는 안은 `gh pr create --title merge` 같은 정상 명령을 막아서 쓰지 않는다(재검증자 시험의 경계 사례).
  - 동작 계약은 [E/merge-gate-behavior-spec-v2.2.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/merge-gate-behavior-spec-v2.2.md)다(SHA256 `67d63f4b…`). 수정은 신규 Sol 한 명이 코드·README·ORCA를 고치고, 신규 Opus가 재검증한다.
  - 수정 횟수: 코드는 두 번째, ORCA 병합 관문 절은 세 번째 수정이다. 한 번 더 고치게 되면 3회를 넘으므로 메인 체크포인트를 알린다.
- 메인 알림: `msg_c3c6c9cdf6c0`(17:40:51Z, status). 메인 pane이 빈 prompt여서 확인 안내를 한 번 넣었다.

### PR1 재검증 결함 수정

- 작업자: 신규 `gpt-6.1-sol` max, 태그 `[Rules Sol]`, handle `term_b9dc49d8-69f6-4ae8-af6e-c3905fbf406c`(리드 pane 아래 vertical split). 첫 화면은 이전 작업자와 같았고 선택창은 없었다(E/fix3-first-screen.json). backend는 unknown이다.
- 계약: [E/fix3-contract.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix3-contract.md) v1, SHA256 `0a93ad59…`, 고정 HEAD `047e0db`, 동작 계약 v2.2.
  - 쓰기 허용은 MergeGate 세 제품 파일, `.claude/settings.json`의 PreToolUse matcher, README 「병합 관문」 절, ORCA다. settings는 마지막에 쓰고 즉시 리드에게 알린다(리드 세션에 바로 실림).
  - 기존 시험 94건이 바뀌지 않고 통과해야 한다. ORCA는 41,629 bytes 이하로 맞추고 이미 축약한 행은 다시 줄이지 않는다.
  - 경로 기계 확인은 E/fix3-contract-pathcheck.txt(17:42:21Z)다.
- Task `task_ac622f2a804c`, Dispatch `ctx_53c8fd49a1ed`다. receipt는 input_accepted, turnStart observed다(E/fix3-worker-start.json).
- settings 갱신 알림 `msg_e8eacf171f92`(17:54:01Z, status, 수신 helper allowed). 리드가 곧바로 이 세션에서 Monitor로 상태 폴더 경로 echo를 시도했고 `protected-path`로 막혔다. matcher 변경이 약 20초 안에 진행 중인 세션에 실렸다(E/hot-reload-probe-2.md, 완료조건 5 실측의 둘째 관측).
- 결과: worker_done `msg_d5ef48a05373`(18:00:24Z, outcome succeeded)는 수신 helper allowed/exit 0이다(E/fix3-worker-done-check-output.json). 보고서는 [E/fix3/report.md](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/fix3/report.md)다. 리드가 코드 `b9e1d2a`(merge-policy·settings·README)와 문서 `178ab0b`(ORCA)로 나눠 커밋했다.
  - 코드: `commandWords`가 줄 이음 접기·단순 명령 경계(괄호·중괄호 포함)·따옴표 값 건너뛰기를 한 곳에서 맡고, 병합·gh api·push 판정이 이를 함께 쓴다. Monitor 병합은 `non-bash-merge`, prompt 도구는 중첩 문자열의 `병합 승인:`을 `approval-injection`으로 막는다. `--attr-source`, 상태 경로의 겹친 구분자·`/./` 접기, 모든 도구의 `tool_input` 객체 조건이 들어갔다.
  - 문서: ORCA 233(Bash·Monitor, 예약·전달 prompt 주입)과 235(공백 따옴표 예시 제거, R3 예시, 등록 밖 도구·hook/settings 자체 수정)를 고쳤다. README에 matcher·`non-bash-merge`·판정 규칙·한계를 맞췄다.
- 리드 R-2 표본 대조:
  - 같은 명령을 다시 돌렸다. MergeGate 94/94, Orca 22/22, 둘 다 exit 0이었다. 기존 시험은 `047e0db0`과 같다. ORCA는 249줄·41,626 bytes, 다섯 문서 합은 92,497이다(E/lead-fix3-green-check.txt).
  - 코드 diff와 ORCA·README·settings word-diff를 읽었다. 진입(`claude-hook.mjs`)은 바뀌지 않았고 branch 조회는 도구와 무관하게 `branch` 결과를 처리한다.
  - 자체 점검 E/fix3/self-check.jsonl 97행(점검 96건 모두 통과, 임시 폴더 잔류 없음)을 읽었다. 재검증 P·H1·GA·B행의 결정 없음 형태가 모두 차단이 됐다. G행(R3)은 계약대로 결정 없음이다. 경계 사례와 정식 통과·소비·확인 창 허용·두 번째 사용 차단은 그대로였다. 승인 기록이 있어도 Monitor 병합은 막히고 기록을 쓰지 않았다.
- 정산: release는 retained/external_terminal/none이었다. 빈 prompt를 확인한 뒤 close(ptyKilled=true)했다(E/fix3-release.json, fix3-before-close.json, fix3-close.json).

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
- **확인 창 대신 승인**: 메인 `msg_af033fe88521`(2026-10-06T14:33:33Z, E/session/wait7-msg_af033fe88521.raw.txt)가 전달한 사용자 원문은 **「대시보드 결정 응답: 1) 병합 관문 - 승인이 맞을 때 확인 창을 hook이 대신 승인할지 → A hook이 대신 승인한다」**다. PermissionRequest hook이 승인 기록과 정확히 맞는 단독 병합 명령에만 allow를 낸다. 메인은 AGENTS 공학 조건 「Claude 권한 확인을 건너뛰는 플래그·설정 변경은 금지」에 이 hook의 예외 문장(사용자 결정 원문 링크)을 넣으라고 했다. 넣는 곳은 PR1이다. 리드 제안 `msg_043ef990dff0`에 메인 `msg_895f7ffa4512`(14:36:43Z, E/session/wait8-msg_895f7ffa4512.raw.txt)가 동의했다. PR1 병합 때 hook이 살아나므로 정본과 동작이 어긋나는 구간을 없애기 위해서다. 예외 문장에는 사용자 결정 원문과 이 절의 링크를 단다. 독립 검증 계약에는 「예외 문장의 조건이 hook 실제 동작과 일치」 대조를 넣는다.
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

<a id="orca-moved-history"></a>

## ORCA에서 옮긴 서술

ORCA 250줄 상한(완료조건 7)을 지키려고 PR1에서 ORCA의 역사 서술을 이 절로 옮겼다. 정본 문장 작업자가 원문을 `E/docs/moved-from-orca.md`에 보존했고, 리드가 기준 `e37ac7a`의 ORCA 행과 글자까지 같은지 기계로 대조했다(E/lead-moved-check.txt, 6블록 모두 같음). 이 절에 옮기면서 두 가지만 바꿨다. 상대 링크는 이 파일 위치에서 열리게 고쳤고, 원문의 `##` 제목은 굵은 글씨로 바꿨다.

PR1 결함 수정에서 한 블록을 더 옮겼다(「R-7 M-1 관찰」). 수정 작업자가 같은 절 축약 뒤에도 ORCA가 상한을 227 bytes 넘는다고 보고했고(`msg_b5eb2054ebd7`), 계약의 두 번째 방법을 썼다. 원문은 `E/fix/moved-from-orca.md`에 있다. 리드가 기준 `eb10c80`의 ORCA 201행과 글자까지 같은지, 남은 행이 현행 평가 기준과 Fable 기동 경계를 지키는지 기계로 대조했다(E/lead-moved-check-fix.txt, 모두 같음). 바꾼 것은 상대 링크뿐이다.

### R-3 Orca 1.4.217 임시 확장(종료)과 1.4.220 복귀

원래 위치: ORCA 85행(`e37ac7a`).

**Orca 1.4.217 임시 확장(종료):** 당시 이 버전인 동안 위 공식 blocking `ask`의 고정 `Question`과 공식 질문에 대한 `reply --id`만 같은 조건의 subject 예외로 인정했다. body 첫머리 자기 태그·현재 `from_handle`·Task·Dispatch와 공식 receipt를 대조하고, 불일치는 처리하지 않고 메인에 보고한다. 일반 `send`는 제외다. **1.4.218 이상으로 올리거나 해당 ask/reply 명령이 subject 옵션을 지원하면 이 1.4.217 확장은 종료**하며 위 1.4.218 근거·예외는 별도 범위로 유지한다. 메인 결정·시각·help 근거는 [1.4.217 승인 출처](../2026-10-05-operating-canon/goal.md#orca-14217-source)에 보존한다. 현재 **1.4.220 복귀로 임시 확장은 종료**됐다. [복귀 결정·시점과 실제 help](../2026-10-05-operating-canon/goal.md#orca-14220-return)에 따라 현재 ask/reply help 양쪽의 subject 옵션 부재를 확인한 공식 ask/reply에만 위 body·identity·receipt 조건으로 적용한다. 해당 명령이 subject 옵션을 지원하면 그 예외는 종료하며 일반 send에는 적용하지 않는다. help 확인은 실제 ask/reply 호출 실증과 구분하고 다른 버전 지원을 주장하지 않는다.

### R-5 실패 이력과 후속 성공

원래 위치: ORCA 148행(`e37ac7a`).

실패 이력과 후속 성공을 구분한다. [내장 컴포넌트 null 감사의 당시 기록](../2026-10-01-native-component-null-audit/goal.md#세션-관측과-다음-경계)은 Astra의 split 기동 1회 실패와 메인 대리 기동 뒤 최초 attach 성공을 구분한다. 이후 D1a 검증자와 Management Fable/Sol 성공은 메인 전달 관찰이다. D1a의 직접 기동 경위는 [완료 goal](../2026-10-01-persistence-technical-design/goal.md#독립-실사와-보완)에 있고, [review-2-start.json](../../../.backups/verification/2026-10-01-persistence-technical-design/review-2-start.json)은 최초 attach의 ready·`input_accepted`·`turn_started`를 보존한다. 1차 D1a의 미보존 원응답과 2차 보존 receipt를 혼동하지 않는다. 이전의 “분할→연결 성공은 아직 미검증”을 현재 전체 상태로 사용하지 않는다.

### R-6 D1a 종료 화면 관찰

원래 위치: ORCA 193행(`e37ac7a`).

D1a verification-2의 [종료 전 화면](../../../.backups/verification/2026-10-01-persistence-technical-design/review-2-before-close-read.json)은 전체 49행(`limited=false`)에서 `/auto-mode-setup` 안내 창이 관측되지 않고 일반 `auto mode on` 상태줄이 보인 기록이다. [판정 원문](../../../.backups/verification/2026-10-01-persistence-technical-design/verification-2/verdict.md)과 [해당 완료 goal](../2026-10-01-persistence-technical-design/goal.md)은 문서 실사 범위를 제공한다. 안내 미관측은 **해당 종료 화면에 한정한 관찰**이며 첫 화면이나 전역 설정 효과의 검증이 아니다.

### R-7 M-1 관찰

원래 위치: ORCA 201행 가운데 두 문장(`eb10c80`). 같은 행의 평가 기준과 Fable 기동 경계는 ORCA에 남았다.

메인 전달 관찰에 따르면 1회차 M-1에서 의미 있는 지적 7건을 찾았고, 그중 #3(Windows rename 간섭)은 구현에서 실측됐다. 이는 [main-request.json](../2026-10-05-operating-canon/goal.md#orca-source-table-1)에 보존한 메인의 관찰 보고이며 이번 문서 작업자가 M-1 구현·실측을 직접 검증한 결과가 아니다.

### R-8 적용 시점 두 문단

원래 위치: ORCA 218~220행(`e37ac7a`).

적용 시점은 **이번 운영 규칙 PR 목표가 끝나면 GameDev Astra부터**, **M-1 PR 병합 뒤 Management Astra**다. 이번 문서 작업이 현재 Astra 세션을 직접 닫는 작업을 포함하지는 않는다. 결정 배경으로 메인은 하루 동안 운영한 GameDev Astra가 자동 압축 후 사용률 6%, M-1 Management Astra는 1시간 만에 58%였다고 전달했다. 이 수치는 [main-request-r8.json](../2026-10-05-operating-canon/goal.md#orca-source-table-2)의 당시 관찰이며 현재 사용률이나 교체 완료를 의미하지 않는다.

위 적용 시점은 2026-10-01 결정 당시 목표를 가리킨 역사 기록이다. 현재 하네스 goal의 첫 BACKLOG PR 병합을 전체 goal 종료나 R-8 시점으로 해석하지 않는다.

### 관찰 기록: 2026-09-29

원래 위치: ORCA 245~249행(`e37ac7a`).

**관찰 기록: 2026-09-29**

두 시도는 시작 방식과 확인 범위가 다르다. 연결·작업 주입·완료 수신을 각각 구분한다.

당시 두 시도의 관측·식별자 원문은 [2026-09-29 관찰 기록](../2026-10-05-operating-canon/goal.md#orca-20260929-history)에 보존했다.

### 관찰 기록: 2026-10-04~05

원래 위치: ORCA 251~253행(`e37ac7a`).

**관찰 기록: 2026-10-04~05**

메인이 전달한 이 PC의 당시 관측·출처·시각·확인 한계는 [환경·복구 기록](../2026-10-05-operating-canon/goal.md#pr1-environment-observations)에서 확인한다.

## 관찰 기록

- heartbeat 깨움(PR2 항목 6의 근거): 설계 실측 동안 우편함 대기는 `--types`로 heartbeat를 뺐다. 그래도 Orca가 작업자 heartbeat(14:11:42Z, 14:16:17Z, 14:18:53Z, 14:24:03Z)마다 리드 터미널에 「You have 1 orchestration message」 안내를 넣었다. 리드는 그때마다 깨어나 소비하지 않는 `inbox` 조회로 heartbeat임을 확인했다. 대기를 겹쳐 열지는 않았다.
