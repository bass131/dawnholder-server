# 규칙·운영 V1.x 1단계 — 세션 쓰기 가드와 생존 신호 간격 도구

- 만드는 것 1: Claude 세션이 도구를 실행하기 직전에 세 실수(우편함 출력 버림, 임시 폴더 쓰기, 맥락 메모보다 앞선 쓰기)를 막고 고치는 법을 보여 주는 hook(세션 쓰기 가드).
- 만드는 것 2: 작업자가 5분마다 보내는 생존 신호(heartbeat)가 끊긴 구간을 우편함 원시에서 찾아 주는 helper(생존 신호 간격 도구).
- 덧붙여 종료 기록 PR에서 규칙 문서 두 줄(시계 출력은 근거 폴더 파일로, Core 근거 폴더 허용 예외)과 우편함 안내 한 문장을 더한다.

## 재개 지점

Rules의 V1.x 로드맵 1단계 goal이다. 사용자가 범위를 승인했다(「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: 도구 PR은 `feat/session-guard-liveness-20261010`(base main `cc20d428`, 선행 시험 뒤 main `bd4dbb5f`를 받음)이다. 정확한 head는 원격 branch와 승인 묶음에서 확인한다.
- 근거 폴더 E: `.backups/verification/2026-10-10-operating-tool-guards/`(Git 제외). 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-10-operating-tool-guards/scope-draft-v1.md)(SHA256 `caf31c40…`)와 넓힌 규칙 3 경계(메인 `msg_27aefcdfcd45`)다. 리드 맥락 메모는 E/lead-context.md(범위 초안)와 E/pr1-lead-context.md(착수)다. 메인 판단·사용자 결정 원시 목록은 E/main-decisions-log.md다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules 리드 Opus]`, handle `term_dee0b834-2c69-4fb0-8ebe-a32e4e9af95f`, Run `run_d8372ac2ca97`(회신 주소 `run:run_d8372ac2ca97`). 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- **현재 위치**: 구현 둘이 끝났다(「현재 결과」). README·CI 작성을 위임한다.
- **남은 순서**: README·CI 작성 → 독립 검증 둘(차례로) → 도구 PR·CI → 승인 묶음 → 사용자 병합 승인 → 결과 기록·Gardener → 종료 기록 PR → 종료 점검 → R-8.
- **사용자 차례**: 도구 PR 병합 승인 줄과 종료 기록 PR 병합 승인 줄이다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 선행 시험 작성
- [>] 가드 hook 구현·검증(구현 끝, 검증 전)
- [>] 간격 도구 구현·검증(구현 끝, 검증 전)
- [ ] 도구 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 PR 병합
- [ ] 종료 점검과 R-8

PR 단계 이름은 PR이 생기면 「PR### 병합」으로 바꾼다.

## 범위

승인 범위는 초안 v1 그대로이고, 사용자 질문은 A(처음부터 막기)다. 규칙 3의 막는 경계는 메인 결정 `msg_27aefcdfcd45`로 넓혔고 사용자가 읽은 간결판에 들어 있었다. 넣는 기준은 (가) 사용자 결정 20 → A의 효과 문안 네 후보, (나) 메인 결정 「시계 출력 미저장」 두 번째 발생의 반복 규칙, (다) 사용자 결정 23의 기록 위치와 정본 반영이다.

### 만들 것

도구 PR(코드, 선행 시험 대상)

1. 세션 쓰기 가드 hook(`99_Tools/SessionGuard/`). PreToolUse hook 하나에 규칙 셋을 둔다. 판정 세부는 「설계」 절이다.
   - 규칙 1 우편함 출력 유실(BACKLOG `mailbox-output-loss-hook`).
   - 규칙 2 쓰기 위치(BACKLOG `shell-write-destination-guard`).
   - 규칙 3 메모 순서(BACKLOG `contract-context-check`의 메모 선행 부분). 이 세션이 맥락 메모를 쓰기 전에는 메모 파일 말고 어떤 파일도 명시적으로 쓰지 못한다(git·하네스 자동 저장 제외).
2. 생존 신호 간격 helper(`99_Tools/Orca/`, BACKLOG `worker-liveness-tool-check`).
3. 시험: `99_Tools/SessionGuard.Tests/`(새 폴더), `99_Tools/Orca.Tests/`(새 시험과 fixture).
4. 설정과 CI: `.claude/settings.json`에 PreToolUse 그룹 하나. CI `code-rules`에 SessionGuard 단계 하나, Orca 단계는 새 시험까지 넓힌다.
5. 진입 문서: `99_Tools/README.md`의 「세션 쓰기 가드」·「생존 신호 간격」 절.
6. 기록: 이 goal, CURRENT의 Rules 줄, BACKLOG 승격 셋과 출처 덧붙임.

종료 기록 PR(문서)

1. 결과 기록, Gardener 결과, 새 후보의 BACKLOG 등록, 후보 도착 검사 실행(위반 0).
2. 시계 출력 저장 반복 규칙: 작업 맥락 양식 머리말의 시계 문장에 「그 출력은 근거 폴더 파일로 저장하고 기록에 경로를 단다. PowerShell에서는 Git의 `date.exe -u`를 쓴다」를 더한다. BACKLOG `powershell-clock-command`를 함께 닫는다.
3. 결정 23 정본 반영: AGENTS 「공학 조건」의 권한 예외 문장에 core-active 근거 폴더 한 경로 허용을 더한다.
4. ORCA 「Orca 도구 관측과 우편함 대기」에 한 문장: 출력 버림은 세션 쓰기 가드가 막고, 작업자 정산 때 리드는 간격 helper로 잰다.

### 건드릴 곳

- 도구 PR: `99_Tools/SessionGuard/`·`99_Tools/SessionGuard.Tests/`(새), `99_Tools/Orca/`·`99_Tools/Orca.Tests/`(새 파일만), `.claude/settings.json`(그룹 하나), `.github/workflows/code-rules.yml`(단계 하나와 Orca 단계 넓힘), `99_Tools/README.md`, 이 goal, CURRENT Rules 줄, BACKLOG.
- 종료 기록 PR: 이 goal, BACKLOG, CURRENT Rules 줄, `.agents/skills/dawnholder-task-context/references/templates.md` 머리말 한 문장, AGENTS 「공학 조건」 한 문장, ORCA 한 문장.
- 병합 관문 파일(`99_Tools/MergeGate/`)은 바꾸지 않는다(「설계」의 재사용 판단).

### 하지 않을 것

- 공식 계약 draft 대조 helper(`official-draft-check-helper`), 위임 계약 칸 lint(`contract-context-check`의 나머지), CI 시험 0개 통과 막기(`ci-zero-test-guard`), 후보 도착 검사 후속 셋, 로드맵 2~4단계.
- heartbeat 5분 간격 변경, Codex 세션용 hook, `cp`·`mv`·스크립트 내부 쓰기 판정, Write 도구의 checkout 밖 일반 쓰기(임시 폴더만 막는다), 메모 내용의 품질 판정.
- 하네스 자동 쓰기(`designated-temp-wording`), 시계 helper, 합류점 태그명 문구(`junction-tag-wording`), 끝난 goal의 CURRENT 줄 규칙(`goal-state-drift`).
- core-active 설정 파일, 다른 파트의 goal·CURRENT 줄·코드, 전역 Claude·Codex·Git 설정, 현황판 코드, 태그 `ops-v1.0`(메인 몫).

### 관찰 가능한 완료조건

1. 선행 시험이 고정 HEAD에서 실패한 원시를 남기고, 구현 뒤 같은 명령으로 통과한다. 기존 MergeGate·Orca·Backlog 시험은 그대로 통과한다.
2. 가드 hook의 실제 진입: 독립 검증자가 도구 PR branch 설정으로 새 Claude Code 세션을 띄운다. 그 세션에서 우편함 출력 버림, TEMP·`/tmp` 쓰기, 메모 전 파일 쓰기가 실행 전에 막히고 고치는 법이 보인다. 근거 폴더 쓰기(메모 뒤), `run_in_background` 대기, git 명령은 통과한다. 병합 관문의 기존 차단 하나도 같은 세션에서 그대로 막힌다.
3. 실제 이탈 fixture(「설계」의 사건 목록)가 모두 규칙대로 판정된다. core-active junction 경우는 fixture로 확인한다.
4. 간격 helper가 지난 goal 원시에서 손으로 잰 300초 초과 구간을 같은 값으로 내고, 질문 대기를 따로 낸다. 이 goal의 작업자 정산마다 리드가 helper를 실행해 원시를 E에 둔다.
5. 도구 PR head의 CI `code-rules`가 새 시험 단계를 실제로 실행해 통과한다. 로그의 시험 수로 0개 통과가 아님을 남긴다.
6. 종료 기록 PR의 정본 문장 셋이 문서 실사를 통과하고 후보 도착 검사 위반이 0이다.
7. 각 PR은 메인 창 승인 줄로 병합한다. 결과 기록 → Gardener → 종료 기록 PR → 종료 점검 → R-8 순서로 끝낸다. 다음 goal은 자동으로 시작하지 않는다.

## 설계

시험은 진입점(`claude-hook.mjs`, `check-liveness.mjs`)을 블랙박스로 실행해 판정한다. 그 밖의 모듈 이름과 나눔은 구현자가 정하고 보고에 적는다. 같은 방식이 병합 관문 시험(`99_Tools/MergeGate.Tests/hook-fixture.mjs`)이다.

선행 시험 작성자의 질문에 리드가 답한 보충 1(`msg_0b1796f0384e`)·보충 2(`msg_b0b5e30ef372`)를 아래 문장에 반영했다. 원문은 E/tdd/work/ask1-answer.txt·ask2-answer.txt다.

### 세션 쓰기 가드

- 진입: `node "$CLAUDE_PROJECT_DIR/99_Tools/SessionGuard/claude-hook.mjs"`. stdin은 Claude Code PreToolUse JSON(`session_id`·`cwd`·`tool_name`·`tool_input`·`hook_event_name`, 하위 에이전트는 `agent_id`)이다. 관측 입력 모양은 [병합 관문 설계 실측](../../../.backups/verification/2026-10-06-merge-gate-canon-refresh/design-probe/raw/hooklog/)에 있다.
- 출력: 막을 때만 `{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"session-guard:<code> <이유와 고치는 법>"}}` 한 줄이다. 그 밖에는 stdout이 비어 있다. `allow`·`ask`는 내지 않는다(허용을 내면 권한 확인을 건너뛴다). exit는 언제나 0이다.
- 결과 코드: `mailbox-output-loss`, `write-outside-checkout`, `temp-write`, `memo-first`. 한 호출에 여럿이 걸리면 규칙 1 → 2 → 3 순으로 처음 것 하나만 낸다.
- 대상 도구: Bash는 규칙 1·2·3, Write·Edit·MultiEdit·NotebookEdit는 규칙 2(임시 폴더)·3. 다른 도구는 결정 없음. 하위 에이전트(`agent_id`) 호출도 면제 없이 같은 규칙과 같은 `session_id` 상태를 쓴다.
- 메인 checkout: `$CLAUDE_PROJECT_DIR/.claude/state/merge-gate/main-checkout`이 파일이면 규칙 2·3을 적용하지 않는다. 규칙 1은 적용한다.
- 입력 오류·내부 예외: 결정 없음으로 둔다. 이 hook은 실수 방지 그물이지 보안 경계가 아니다. 가드가 고장 나 모든 도구를 막는 일을 피한다. 가능하면 세션 상태에 오류를 남긴다.

규칙 1 우편함 출력 유실

- 대상은 `orca`(경로·`.exe` 포함)의 `orchestration <하위 명령>`이다. 읽기 전용 하위 명령(`inbox`·`run-current`·`run-list`·`run-show`·`task-list`·`worker-show`·`worker-read`·`worker-list`·`dispatch-show`·`gate-list`·`request-show`), `--peek`·`--all`이 붙은 `check`, `--help`가 든 명령은 대상이 아니다.
- 막는 모양: (가) 표준 출력을 `/dev/null`·`NUL`로 보냄(`>`·`1>`·`>>`·`&>`·`>|`), (나) 그 명령 뒤의 홀로 선 `&`(배경 실행), (다) 표준 출력을 파이프(`|`·`|&`)로 다음 명령에 넘김. 다만 다음 명령이 그 파이프의 마지막 `tee`면 (다)가 아니다. `tee`의 파일은 규칙 2·3이 본다.
- 고치는 법: 출력을 근거 폴더 파일로 저장(`> <근거 폴더>/….json`)하고 따로 읽는다. 대기는 Bash `run_in_background`로 연다.

규칙 2 쓰기 위치

- Bash 쓰기 목적지: 출력 리다이렉트(`>`·`>>`·`>|`·`&>`·`&>>`·`N>`·`N>>`)의 대상과 `tee`의 파일 인자다. `>&N` 같은 fd 복제는 목적지가 아니다. `/dev/null`·`/dev/stdout`·`/dev/stderr`·`/dev/fd/*`·`NUL`은 파일이 아니다. heredoc 본문은 목적지로 읽지 않는다. heredoc을 연 명령의 리다이렉트는 목적지다.
- 경로 풀기: 따옴표를 벗긴다. `$NAME`·`${NAME}`은 같은 명령에서 앞서 준 `NAME=값`(`export` 포함)으로 먼저, 다음에 hook 프로세스 환경으로 푼다. `~`는 HOME이다. Git Bash 드라이브 모양 `/c/…`는 `C:/…`로 읽는다. 상대 경로는 입력 `cwd`(없으면 `CLAUDE_PROJECT_DIR`) 기준이다. 같은 명령 안의 `cd`는 따라가지 않는다(한계, 리드 R-2 표본). `.`·`..`는 글자로 접는다. 드라이브 경로는 대소문자를 가리지 않는다. 풀리지 않는 변수가 남으면 「미해결」이다.
- 허용 뿌리: `CLAUDE_PROJECT_DIR`, 그리고 `<checkout>/.backups`가 junction·심볼릭 링크면 그 실제 대상이다(core-active). 목적지가 허용 뿌리 밖이면 `write-outside-checkout`이다. 미해결 목적지는 규칙 2가 판정하지 않는다(한계).
- 쓰기 도구: `file_path`(Write·Edit·MultiEdit)나 `notebook_path`(NotebookEdit)가 임시 뿌리(`os.tmpdir()`, 환경 TEMP·TMP·TMPDIR, `/tmp`) 아래면 `temp-write`다. Claude Code scratchpad도 TEMP 아래라 여기에 든다. 그 밖의 checkout 밖 경로(메모리 폴더 등)는 규칙 2가 보지 않는다.
- 허용 뿌리가 먼저다: 경로가 허용 뿌리 안이면 임시 뿌리 안이어도 통과한다. R-5가 Claude 작업자의 TEMP·TMP를 checkout 안 `.backups/tmp/<이름>/`으로 정하기 때문이다.
- 고치는 법: 근거 폴더(`.backups/verification/<goal>/`) 아래로 쓴다.

규칙 3 메모 순서

- 세션 상태: `$CLAUDE_PROJECT_DIR/.claude/state/session-guard/<session_id>.json`(Git 제외 폴더). 형식은 `{"version":1,"memoWrittenAt":"<UTC>"|null,"denials":[{"at":"<UTC>","tool":"<도구>","code":"<결과 코드>","target":"<경로>"|null}],"errors":[{"at":"<UTC>","message":"<문구>"}]}`이다. 저장은 병합 관문처럼 같은 폴더 임시 파일 + rename이다. `session_id`는 병합 관문과 같은 파일 이름 한 조각 형식만 받는다. `target`은 규칙 2·3에서 목적지 문자열(모양은 정하지 않음), 규칙 1에서 문자열 또는 null이다.
- 메모 경로: 허용 뿌리의 `.backups/verification/` 아래이고 파일 이름에 `context`가 들고 `.md`로 끝나는 파일(대소문자 무시)이다. `.backups` junction의 실제 대상 경로로 쓴 같은 모양 파일도 메모다. 실제 관례 근거는 E/session/memo-naming-count.txt(다섯 작업 공간 618개)다.
- 판정: 한 호출의 쓰기를 글자 순서대로 본다. 쓰기 도구는 그 경로 하나, Bash는 규칙 2의 목적지(미해결 포함, 파일 아닌 것 제외)다. 세션 메모 기록이 없을 때, 첫 쓰기가 메모 경로면 메모 기록을 남기고 통과한다. 메모보다 앞에 다른 쓰기가 있으면 `memo-first`로 막는다. 메모 기록이 생긴 뒤에는 규칙 3이 통과한다. 기존 메모를 Edit로 고치는 것도 메모 쓰기다(재개한 세션의 길).
- 세지 않는 것: 리다이렉트·`tee`가 없는 git·mkdir·cp·mv·스크립트 내부 쓰기, 하네스 자동 저장, 가드 자신의 상태 쓰기.
- 고치는 법: 이 세션의 맥락 메모(`.backups/verification/<goal>/…context….md`)를 먼저 쓴다. 메모의 시각은 시계 출력을 메모 안에 그대로 적는다(별도 시계 파일을 먼저 쓰면 막힌다).

막은 기록: 막을 때마다 세션 상태에 시각·도구·코드·대상을 더한다. 기록 저장이 실패해도 판정은 그대로 낸다. 10월 31일 평가 자료다.

실제 이탈 사건 목록(시험 fixture의 원천, 원문이 남은 것은 원문을 쓴다)

| 사건 | 원천 |
|---|---|
| 우편함 대기를 `&`·`> /dev/null 2>&1`로 실행 | BACKLOG `mailbox-output-loss-hook` 출처(Management `msg_9f655bb20a2c`) |
| `--wait` 없는 `check --ack` 출력을 `/dev/null`로 버림 | 같은 행(Rules 2026-10-07T06:57Z) |
| `tee <파일>` 뒤 `head -c`·`grep -m` 파이프로 송신 결과가 잘림 | 같은 행(Management `msg_f0bc3d74e103`) |
| Git Bash `/tmp/x`, `/tmp/pr217files.txt` | BACKLOG `shell-write-destination-guard` 출처 |
| 저장소 상위 폴더 `.backups-tmp-ignore` | 같은 행 |
| Windows TEMP에 받은 메시지·Run receipt 리다이렉트(Rules 착수·재진입) | 같은 행 |
| Claude Code scratchpad에 진입 지시 리다이렉트(이 goal 리드, 07:29Z대) | E/lead-context.md 「정정」 |
| CodeMap 리드가 진입 근거 10개를 scratchpad에 쓴 뒤 메모 | architecture-active E `review/verdict.md` #1, 메인 `msg_e4651539d5a2` |
| 근거 폴더 receipt·READY 본문을 메모보다 먼저 씀(이 goal 리드) | E/lead-context.md 「정정」 |

허용 모양: 근거 폴더로의 리다이렉트(메모 뒤), `run_in_background`로 연 `check --wait`(끝에 `&` 없음), `check --peek ... > E/x.json`, git 명령, `2>/dev/null`만 붙은 명령, 메인 checkout의 임시 폴더 쓰기.

진입 순서 변경 후보(메인 `msg_27aefcdfcd45`): 규칙 3이 실리면 리드·작업자 진입은 「읽기 → 메모 → Run·receipt·READY」다. RESUME·세션 인계 스킬의 진입 순서 문장을 맞출지는 도구 PR 결과를 보고 종료 기록 PR 계획 때 정한다. 그 전까지 hook의 고치는 법 문구가 이 순서를 알려 준다.

재사용 판단: 병합 관문의 셸 읽기(`merge-policy.mjs`의 `simpleCommands`)는 `;&|<>(){}`에서 잘라 리다이렉트 대상과 파이프·배경 연산자를 버린다. 목적지 판정에는 다른 읽기가 필요해 새로 쓴다. 같은 책임의 복제가 아니다. 상태 저장 방식·deny 출력 모양·표식 파일 경로는 관례로 따른다. 병합 관문 파일은 바꾸지 않는다.

### 생존 신호 간격 helper

- 진입: `node 99_Tools/Orca/check-liveness.mjs <파일>... [--threshold-seconds <n>]`. 기본 300초다.
- 입력: 리드가 저장한 Orca CLI `--json` 출력이다. `inbox --json`·`check --json`(`result.messages`), 대기 출력(앞의 `_keepalive` JSON 줄은 건너뜀), 메시지 객체 배열을 받는다. 여러 파일의 메시지는 id로 합친다. 읽었지만 Dispatch가 하나도 없으면 `input-error`다.
- 묶기: payload(객체 또는 JSON 문자열)의 `dispatchId`로 묶는다. dispatchId가 없고 `from_handle`이 `dispatch:<id>`면 그 id다. 그 밖의 메시지는 질문 답 찾기에만 쓴다.
- 신호와 간격: 그 Dispatch가 보낸 모든 메시지(heartbeat·status·question·escalation·worker_done)를 송신 시각(`created_at`) 순으로 놓고 이웃 간격을 잰다. 같은 시각이면 `sequence` 오름차순, `sequence`가 없으면 입력 순이다. 마지막은 worker_done까지다. worker_done 뒤 메시지 처리는 구현 재량이다(미시험).
- 질문 대기: Dispatch의 `question` Q 뒤에, `thread_id`가 Q의 id인 다른 발신자의 답 R이 그 Dispatch의 다음 신호 N보다 먼저(같은 시각 포함) 있으면 Q→R은 질문 대기이고 R→N은 보통 간격(시작은 R)이다. 답이 없거나 N보다 늦으면 Q→N이 보통 간격이다. 답이 여럿이면 가장 이른 답을 쓴다. 질문 대기는 길이와 무관하게 모두 내고 최대 간격·초과에 넣지 않는다. 작업자는 blocking ask 중에는 신호를 보낼 수 없지만 답을 받은 뒤에는 5분 안에 다시 보내야 하기 때문이다.
- 출력: JSON 하나. 키는 `status`(`within`·`over`·`input-error`), `thresholdSeconds`, `dispatches[]`(`dispatchId`·`signalCount`·`firstAt`·`lastAt`·`maxGapSeconds`·`overGaps[]`·`questionWaits[]`), `diagnostics[]`(`code`·`path`·`message`·`repair`)다. `overGaps`의 항목은 `{from:{id,at,type}, to:{id,at,type}, seconds}`, `questionWaits`의 항목은 `{from, to, seconds, replyId}`다. `dispatches` 순서와 진단 code 값은 정하지 않는다. exit 0 `within`, 1 `over`, 2 `input-error`. 파일 쓰기·네트워크·상태 변경은 없다.
- 시험 기대값은 지난 goal 리드·Gardener가 손으로 잰 기록값이다(제품 계산과 다른 원천). 예: 종료 기록 실사 303초(05:46:55Z→05:51:58Z, 지난 E `lead-check/closeout-review-hb-intervals.json`), J 문장 Sol 391초(지난 E `lead-check/closeout-j-hb-intervals.txt`). 원시가 남은 구간만 쓴다.

## PR 경계와 검증

| PR | 변경 경계 | 등급과 세션 |
|---|---|---|
| 도구 | hook·helper 코드, 시험, 설정 그룹 하나, CI 단계, README, 시작 기록 | 강(설치·실행·I/O 도구, 제품+시험 50줄 이상 예상). R-7 비해당(메인 `msg_39e69295de62`) |
| 종료 기록 | 결과 기록과 정본 문장 셋 | 문서 실사. 작성 `gpt-6.1-sol` max(신호 해당 없음: 명세가 정해진 문서 문장), 실사 신규 `gpt-6-astra` xhigh |

도구 PR 계약(차례)

1. 선행 시험: 신규 `claude-opus-5-5` 하나가 두 도구의 요구 시험을 쓴다. 고정 HEAD에서 실패 원시를 E에 둔다.
2. 구현 둘(동시, 파일 겹침 없음): 가드 hook `gpt-6-astra` xhigh(신호 1 셸 명령 해석, 신호 4 설계 판단이 남은 새 다파일 검사기), 간격 helper `gpt-6-astra` xhigh(신호 2 시간·순서·수명, 애매하면 Astra). 가드 구현자가 `.claude/settings.json` 그룹을 쓴다.
3. README·CI: `gpt-6.1-sol` max(신호 해당 없음: 구현과 설계가 정한 명세의 문서·CI 단계). README와 code-rules.yml은 한 파일씩이라 구현 둘이 끝난 뒤 한 작업자가 쓴다.
4. 독립 검증 둘(차례로, 파트당 검증자 하나): Astra 구현이라 신규 `claude-opus-5-5`다. 가드 검증은 설정·README 가드 절·CI SessionGuard 단계를, helper 검증은 README helper 절·CI Orca 단계를 포함한다.

- 순서: 도구 PR → 결과 기록·Gardener → 종료 기록 PR. 종료 기록 branch는 도구 PR 병합 뒤 최신 main에서 만든다.
- 규칙 문서 bytes: 늘어난 값을 원시로 남기고 상쇄 의무는 두지 않는다(메인 `msg_39e69295de62`).
- 다른 파트 영향: 병합 뒤 `.claude/settings.json`의 새 hook이 각 파트에 실린다. 각 파트는 [main 맞춤과 합류점](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md#main-맞춤과-합류점)대로 다음 안전 지점에 받는다.

### 위험

1. hook 오탐이 맞춤 뒤 모든 worktree의 Claude 세션에 걸린다. 게임 파트 리드가 명령을 고쳐 다시 내야 할 수 있다. 실제 사건 fixture와 고치는 법 안내, 막은 기록으로 줄인다.
2. 셸 판정 한계는 병합 관문과 같다. 별칭·스크립트 파일·변수 속 명령, 스크립트 내부 쓰기, `cp`·`mv` 목적지는 보지 못한다.
3. 메모 순서는 Claude 세션만 막는다. Codex 구현자·실사자는 지금처럼 계약 문구와 독립 판정으로 본다. `claude --resume`이 같은 `session_id`를 잇는지는 실제 진입에서 본다.
4. 규칙 3이 넓어 진입 직후 오탐 여지가 크다. 진입 순서가 「읽기 → 메모 → Run·receipt·READY」로 바뀐다. 진입 문서(RESUME·session-handoff 스킬) 변경은 다음 계획 후보로 둔다.
5. 도구 호출마다 node hook이 둘 돈다. 지연을 실제 진입 실행에서 잰다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 메인 판단은 사용자 결정이 아니다. 원시 목록은 E/main-decisions-log.md다.

- 진입 지시: 메인 `msg_9e5cda450042`(2026-10-10T07:29:08Z, E/session/inbox-peek-entry.json). 네 후보 묶음, 시계 미저장 반복 규칙 후보(메인 결정, PR219 승인 묶음 결정 요청 2의 답 `msg_a3f7388fa42d`), 「메모보다 앞선 쓰기」 근거.
- 범위 초안 v1: 리드 `msg_94712317aded`(07:43Z대). 메인 판단 `msg_39e69295de62`(07:44:08Z): R-7 비해당, 결정 23 정본 문장은 종료 기록 PR, bytes 상쇄 의무 없음.
- 근거 추가: 메인 `msg_e4651539d5a2`(07:47:34Z) CodeMap 정리 PR 실사 #1. 리드 자기 정정 `msg_e83283bd3bfe`. 메인 결정 `msg_27aefcdfcd45`(07:48:58Z): 사후 기록 수용, 오늘 「메모보다 앞선 쓰기」 다섯(Content 둘, Rules 둘, CodeMap 하나)·세션 임시 폴더 쓰기와 겹침 셋, 규칙 3 경계 넓힘.
- 시계 출력 미저장: 첫 발생은 [TDD goal 종료 기록 재실사](../2026-10-08-tdd-canon-temp-write-boundary/goal.md)의 리드 이탈(첫 관찰), 두 번째는 PR219 재실사 O3(지난 Rules E `lead-check/closeout-rereview-r2.md` 54~55행). BACKLOG `record-timestamp-from-clock`은 시계 출력 없이 어림한 실수이고 이번 것은 시계 출력을 근거 폴더에 저장하지 않은 실수라 다르다. 층은 문서(양식)다. hook은 시계 명령이 기록용인지 알 수 없고 모든 `date`에 저장을 강제하면 잡음이 크다. 저장 명령이 한 줄이라 helper도 이득이 작다.

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **다음 Rules goal**(진입 지시 `msg_9e5cda450042`, 2026-10-10 16:24 KST): 「8) 20 - Rules 다음 goal → A V1.x 1단계 작게」. 효과 문안: 우편함 출력 유실 hook, 오늘 채택한 생존 신호 간격 도구와 쓰기 위치 hook, 메모 순서 검사를 묶은 작은 goal 범위 초안을 받는다.
- **마일스톤 우선순위**(같은 지시): 「5) 17 - 마일스톤 우선순위(기능 동결 10월 28일까지) → A 인스턴스 던전 먼저」. 이 goal은 게임 goal을 막지 않는 크기로 둔다.
- **Core 근거 폴더 쓰기 확인 창**(메인 전달 `msg_f811cc326415`, 2026-10-10 16:33 KST): 「대시보드 결정 응답: 1) 23 - Core 근거 폴더 쓰기 확인 창을 없앨지 → A 그 폴더만 허용」.
  - 메인이 core-active `.claude/settings.local.json`(Git 제외)에 `permissions.additionalDirectories = ["C:/Dev/DawnHolder_Project/.backups"]` 한 항목만 더했다. 원인은 core-active `.backups`가 원래 clone의 `.backups`를 가리키는 junction이라 파일 도구 쓰기가 작업 폴더 밖으로 보인 것이다.
  - AGENTS 「공학 조건」의 「Claude 권한 확인을 건너뛰는 플래그·설정 변경은 금지」에 대한 사용자 예외다. 정본 반영 전까지 이 기록으로 적용한다. 정본 문장은 종료 기록 PR에서 넣는다(메인 `msg_39e69295de62`).
- **범위 수정 요청**(메인 전달 `msg_981fc90013d0`, 2026-10-10T07:47:59Z): 「어떤걸 만들고 싶은지 이해가 잘 안되는데, 텍스트가 너무 많아, 간결하게 설명해줘」. 범위 변경이 아니다. 메인이 간결판으로 다시 올렸다. 메인 지시: goal 문서와 사용자 문안은 첫 화면 세 줄 안에 무엇을 만드는지를 둔다.
- **범위 승인**(메인 전달 `msg_4de2cd3a96d2`, 2026-10-10T07:50:11Z): 「대시보드 결정 응답: 1) 계획 검토 - Rules Rules 실수 막는 검사기와 신호 간격 도구(간결판, 질문 하나) → A 승인 (초안 msg_94712317aded)」. 초안 v1 그대로, 질문은 A(처음부터 막기), 넓힌 규칙 3 경계를 포함한다. 하네스 원칙 4(새 검사는 경고 시범부터)의 사용자 예외다. 승인 직전 보조 세션 알림과 섞여 들어온 C 코멘트 입력은 메인이 결정으로 쓰지 않았다.

## 현재 결과

### 착수

- 진입(2026-10-10T07:29Z~07:31Z): Run `run_d8372ac2ca97` 생성(E/session/run-create-receipt.json), READY `msg_f8f3f4f80d40`. 기준 HEAD `cc20d428` = origin/main, 미커밋 0.
- 리드 이탈(리드 귀속, 메인 `msg_27aefcdfcd45`로 사후 기록 수용): 진입 지시 peek 출력을 Claude Code scratchpad에 리다이렉트로 썼고, 맥락 메모(07:37:04Z) 전에 scratchpad 1개와 E 15개를 썼다(E/lead-context.md 「정정」). 리드의 근거 폴더 밖 쓰기는 사건 단위로 여섯 번째다. 이 goal 규칙 2·3의 fixture 원천이다.
- branch `feat/session-guard-liveness-20261010`을 base main `cc20d428`에서 만들었다(07:5xZ). 착수 맥락 메모는 E/pr1-lead-context.md(07:52:35Z)다.

### 선행 시험

- 작성자: 신규 `claude-opus-5-5`(화면 「Opus 5.5 ⚡xhigh」, backend unknown), Task `task_41e39f212554`, Dispatch `ctx_ffacc0131e85`, 08:02:37Z~08:38:24Z, worker_done `msg_baab03eb8b8f`(succeeded). 계약 E/tdd-contract.md(SHA256 `d335b0c5…`), 보고 E/tdd/report.md.
- 결과: 가드 시험 164개(`99_Tools/SessionGuard.Tests/`), helper 시험 25개(`99_Tools/Orca.Tests/check-liveness.test.mjs`와 실제 원시를 줄인 fixture 5개). 고정 HEAD `852f88ca`에서 188개가 진입점 부재로 실패했고, 진입점을 부르지 않는 fixture 정합 점검 1개가 통과했다. 기존 Orca 27·MergeGate 126·Backlog 60은 통과했다. Linux 실행은 하지 않았고 CI가 구현 뒤 처음 돌린다.
- 리드 R-2(E/lead-r2/r2-verdict.md): 같은 명령 재실행 결과가 보고와 같다. 손 측정 기대값 303초·61초를 지난 원본 원시에서 따로 계산해 맞췄다. 기대값은 시험 안의 리터럴이고 시험이 시각을 계산하지 않는다. 작성자 대화 기록에서 첫 쓰기가 메모(08:11:28Z)임을 확인했다.
- 생존 신호(손 계산, E/lead-r2/tdd-dispatch-inbox-raw.json): 18개, 가장 긴 보통 간격 267초, 300초 초과 0. helper 완성 뒤 같은 원시로 다시 잰다.
- 정산: `worker-release`는 retained(리드가 만든 split), 빈 프롬프트 확인 뒤 `terminal close`로 닫았다. 커밋 `b3644797`, 이어 main `bd4dbb5f`를 받았다(CURRENT 충돌은 main의 정리에 Rules 줄을 더해 풀었다).

### 구현

- 두 구현자: 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown) 둘을 08:47Z대에 동시에 기동했다. 두 `worker-start`가 `turn_start_unobserved`였고, draft 크기가 계약 크기와 1자 차이라 R-5 복구대로 텍스트 없는 Enter를 한 번씩 보냈다(E/draft-recovery-check.txt).
- 간격 helper(Task `task_9d785ebacdc3`, Dispatch `ctx_7ec71181fbbe`, worker_done `msg_cfa234d74359`): `99_Tools/Orca/check-liveness.mjs`(CLI 56줄)·`liveness-policy.mjs`(순수 모듈 203줄), 커밋 `d30db136`. 선행 시험 25/25, 기존 수신 helper 시험 27/27. 리드 R-2(E/lead-r2-liveness/r2-verdict.md): 선행 시험 작성자 원시에서 helper 결과가 리드 손 계산(신호 18, 최대 267초, 질문 대기 46초)과 같다.
- 세션 쓰기 가드(Task `task_d6dc5e93dc0b`, Dispatch `ctx_f117ca736701`, worker_done `msg_4788cf612783`): `99_Tools/SessionGuard/` 다섯 파일(447줄)과 `.claude/settings.json` 그룹 하나, 커밋 `bb283f58`. 가드 시험 164/164, 기존 MergeGate 126·Orca 27·Backlog 60 통과. 리드 R-2(E/lead-r2-guard/r2-verdict.md): 재실행 수가 같다. 표본 실행에서 같은 명령 안의 `cd`를 따라가지 않는 한계를 확인했고 「설계」 문장에 적었다.
- 생존 신호(helper로 잼): helper 구현자 신호 4·최대 144초, 가드 구현자 신호 7·최대 201초, 둘 다 초과 0. 선행 시험 작성자는 helper로 다시 재어 신호 18·최대 267초였다.
- 실행 중 세션에 실림: settings.json 변경(08:55:12Z, 미커밋) 뒤 이 리드 세션의 다음 Bash 리다이렉트가 `memo-first`로 막혔다. 세션 시작 뒤 바뀐 hook이 바로 실렸고 가드 상태에 이 세션의 메모 기록이 없었다. 리드 메모(E/pr1-lead-context.md)를 고쳐 써서 풀렸다. 병합 뒤 main을 받는 파트의 실행 중 Claude 세션도 같은 일을 겪으므로 README 가드 절에 고치는 법을 적는다.

### 첫 발생 기록

교정 정본에 따라 첫 발생을 기록만 한다. 같은 일이 다시 나면 반복 규칙 후보다. 이 goal 범위를 넓히지 않는다.

- 보조 터미널 알림과 메인 입력창 초안이 섞여 제출됨(메인 전달 `msg_53bb8fc67cff`, 2026-10-10T07:59:43Z): 16:4x KST 보조 메인 Opus가 메인 pane에 `orca terminal send --enter`로 「[보조 메인 Opus] Orca 메시지를 확인하라」를 넣는 순간 메인 입력창의 대시보드 결정 응답 초안과 섞여 제출됐다. 메인은 표식이 붙은 입력이라 결정으로 쓰지 않았고 사용자가 A 승인을 다시 제출했다(「적용 중인 사용자 결정」 범위 승인). 보조 진술 `msg_89cd1162a8bb`: 보내기 직전 terminal read tail의 마지막 줄은 「❯」뿐이고 draft 필드도 없었다. 메인은 보조에게 메인 pane terminal send를 금지했다(`msg_5946e443c356`). CLAUDE.md·보조 세션 스킬의 「빈 프롬프트일 때만 터미널 안내」는 이 경쟁 상태를 막지 못한다. 반복 때 후보 예: 메인 pane을 터미널 입력 대상에서 빼기.
- 작업자의 허용 실행 방식 이탈(선행 시험 작성자 자기 감사, 리드 판정 E/lead-r2/r2-verdict.md): 인라인 `node - <<'EOF'` 스크립트로 허용 수정 파일인 시험 파일을 한 번 고쳤다. 계약의 허용 실행 목록은 「E/tdd/work/의 node 점검 스크립트」였다. 쓰기 위치는 허용 안이고 결과 파일은 독립 검증 대상이라 통과 차단은 아니다. 축약 스크립트가 fixture를 직접 쓴 것은 E/tdd/work/의 node 스크립트이고 재현 근거가 남아 허용 안으로 판정했다. 반복 때 후보 예: 계약 허용 실행에 「허용 수정 파일의 쓰기는 Write·Edit 도구」를 명시.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID나 기존 goal 링크를 단다.

- 진입 순서 문서(Rules): 규칙 3이 실리면 리드·작업자 진입은 「읽기 → 메모 → Run·receipt·READY」여야 한다. RESUME과 세션 인계 스킬의 진입 순서 문장을 맞출지 본다. BACKLOG `contract-context-check`.
