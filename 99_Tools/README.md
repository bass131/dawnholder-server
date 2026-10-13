# 개발·검증 도구

패킷 생성, 헤드리스 통신 시나리오와 WSL 실행을 제공한다.

- [sync-wsl.sh](sync-wsl.sh): 원본별 동기화·빌드·테스트·서버·봇 실행.
- [PacketGenerator/Program](PacketGenerator/Program.cs): PDL에서 패킷 코드 생성.
- [PDL](PacketGenerator/PDL.xml): 패킷 정의 원본.
- [headless-bot/Program](headless-bot/Program.cs): 시나리오 선택·실행.
- [Scenarios](headless-bot/Scenarios/): 기능별 통신 검사.
- [세션 쓰기 가드](#세션-쓰기-가드): Claude Code의 우편함 출력·쓰기 위치·메모 순서 검사.
- [생존 신호 간격](#생존-신호-간격): 저장한 Orca 메시지의 Dispatch별 송신 간격 검사.

[도구·검증 계약](../00_Document/domains/tooling.md)과 [정확한 명령](../00_Document/operations/DEVELOPMENT.md)을 따른다. 생성기는 Shared 소스를 변경하고 서버·봇은 7777 포트를 사용한다. 봇은 ClientNet·Shared에 의존하므로 계약 변경 후 함께 확인한다.

## Orca 메시지 수신 판정

[Orca/message-policy.mjs](Orca/message-policy.mjs)의 `evaluateMessage(input)`은 활성 Dispatch 작업자 메시지의 순수 판정이고 [Orca/check-message.mjs](Orca/check-message.mjs)는 로컬 UTF-8 JSON 파일을 읽어 JSON stdout을 내는 CLI다. Node 표준 라이브러리만 쓰며 외부 의존성 설치가 없다.

```powershell
node 99_Tools/Orca/check-message.mjs .backups/수신입력.json
```

입력은 `message`(원시 Orca message 객체)와 `expected`(coordinator가 현재 receipt/worker-show로 별도 확인한 `fromHandle`, `taskId`, `dispatchId`, `tag`)다. payload는 JSON 문자열이나 객체를 그대로 제공한다. identity를 메시지에서 유도하지 않는다. subject/body의 누락·null·공백은 빈 텍스트이고 비문자 값은 입력 오류다. 빈 heartbeat의 `alive` 표식, 내용 있는 메시지의 태그와 지원 문맥은 [ORCA 정본](../00_Document/operations/ORCA.md#dispatch-message-policy)을 따른다.

공식 blocking ask의 `Question` 예외를 쓸 때만 coordinator가 확인한 `expected.officialAsk = { messageId, cliVersion, askHelp, replyHelp }`를 추가한다. messageId는 질문 receipt ID, cliVersion은 실제 버전, askHelp·replyHelp는 같은 CLI의 ask/reply help 원문이다. 실제 id/thread/dispatch 발신/payload.question도 대조하지만 **일반 send가 모양을 흉내낼 수 있으므로** 이 근거를 메시지에서 만들지 않는다. 근거가 없거나 형식이 틀리거나 help에 subject 옵션이 있으면 예외를 열지 않고 사람 대조로 돌아간다. body 태그와 세 identity 일치는 계속 필수다.

결과는 `{ status, exitCode, exception, diagnostics }`이며 진단마다 `{ code, path, message, repair }`가 있다. exit0 `allowed`, exit1 `policy-violation`, exit2 `input-error`(형식/필수 identity/지원 문맥·파일/JSON/도구 실패)로 구분한다. stdout JSON은 기계 소비용이고 수정·보고 안내를 담는다. 파일/네트워크 쓰기·메시지 전송·ack·Orca 수명주기 변경·프로세스 강제 종료는 없다.

일반 Main terminal-only 메시지, blocking reply의 기존 subject 예외, 다른 message type과 원격 진위 검증은 이 helper의 범위 밖이다. 허용 결과만으로 런타임 진위나 worker_done 정산을 증명하지 않는다. 독립 회귀는 신규 Opus 소유 `99_Tools/Orca.Tests/message-policy.test.mjs`이며 기존 [code-rules workflow](../.github/workflows/code-rules.yml)의 별도 단계가 부재/load 실패/nonzero도 실패 처리하고 stdout/stderr/exit를 기존 artifact 폴더에 보존한다. 자체 smoke와 신규 Opus 판정·원격 CI는 별도 근거다.

## 세션 쓰기 가드

[SessionGuard/claude-hook.mjs](SessionGuard/claude-hook.mjs)는 인자 없이 stdin의 Claude Code PreToolUse JSON을 읽는다. 저장소 [settings](../.claude/settings.json)의 PreToolUse 그룹이 matcher `Bash|Write|Edit|MultiEdit|NotebookEdit`와 명령 `node "$CLAUDE_PROJECT_DIR/99_Tools/SessionGuard/claude-hook.mjs"`를 등록한다. Node 표준 라이브러리만 사용한다. 같은 입력을 파일로 보존했을 때의 실행 명령은 다음과 같다.

```bash
node "$CLAUDE_PROJECT_DIR/99_Tools/SessionGuard/claude-hook.mjs" < .backups/verification/hook-input.json
```

입력은 `session_id`·`cwd`·`tool_name`·`tool_input`·`hook_event_name`이며 하위 에이전트 호출에는 `agent_id`가 있다. Bash의 `tool_input.command`, Write·Edit·MultiEdit의 `file_path`, NotebookEdit의 `notebook_path`를 판정한다. Bash는 우편함 출력 → 쓰기 위치 → 메모 순서, 쓰기 도구는 임시 위치 → 메모 순서로 처음 위반 하나만 낸다. 다른 도구는 결정 없음이며 하위 에이전트도 같은 규칙과 같은 `session_id` 상태를 쓴다.

| 결과·코드 | 의미와 고치는 법 |
|---|---|
| `mailbox-output-loss` | 상태를 소비하거나 바꾸는 `orca orchestration` 명령의 stdout을 `/dev/null`·`NUL`로 버림, 홀로 선 `&`로 배경 실행, 마지막 `tee` 이외의 파이프로 넘김. 출력을 근거 폴더에 보존하고 따로 읽으며 대기는 Bash `run_in_background`로 연다 |
| `write-outside-checkout` | Bash 리다이렉트·`tee` 파일 목적지가 허용 뿌리 밖. `.backups/verification/` 아래로 쓴다 |
| `temp-write` | 쓰기 도구의 경로가 허용 뿌리 밖 임시 폴더(`os.tmpdir()`·TEMP·TMP·TMPDIR·`/tmp`, Claude scratchpad 포함). 근거 폴더로 옮긴다 |
| `memo-first` | 이 세션의 메모 기록 전에 다른 명시적 쓰기 목적지가 먼저 나옴. 맥락 메모를 먼저 쓰거나 기존 메모를 Edit로 고친다 |
| 결정 없음 | 통과·대상 밖·입력 오류·내부 예외는 빈 stdout. 입력/내부 오류는 가능하면 세션 상태에 남긴다 |

모든 결과의 exit는 0이다. 차단 때만 `hookSpecificOutput`의 `hookEventName: "PreToolUse"`, `permissionDecision: "deny"`, `permissionDecisionReason: "session-guard:<code> <이유와 고치는 법>"`를 담은 JSON 한 줄을 stdout에 낸다. `allow`·`ask`는 내지 않는다. 규칙 1의 읽기 전용 하위 명령, `--peek`·`--all`이 붙은 check, `--help`는 대상 밖이며 목록과 셸 연산자 계약은 [goal 설계](../01_Phases/goals/2026-10-10-operating-tool-guards/goal.md#설계)에 있다.

허용 뿌리는 `CLAUDE_PROJECT_DIR`와 checkout의 `.backups`가 junction·심볼릭 링크일 때 그 실제 대상이다. **허용 뿌리가 임시 뿌리보다 먼저**이므로 checkout 안 `.backups/tmp/`의 TEMP·TMP 쓰기는 위치 규칙을 통과한다. 상대 목적지는 입력 `cwd`(없으면 `CLAUDE_PROJECT_DIR`) 기준이며 따옴표·`.`·`..`와 Git Bash `/c/…`를 정규화한다. `~`는 HOME으로 푼다. 미해결 목적지는 위치 판정에서 빠지지만 메모 순서에서는 쓰기로 센다. 쓰기 도구는 임시 경로만 위치 규칙으로 본다.

메모는 허용 뿌리의 `.backups/verification/` 아래에서 파일 이름에 `context`가 들고 `.md`로 끝나는 경로다(대소문자 무시, junction 실제 대상도 포함). Bash 리다이렉트·`tee` 목적지를 글자 순서대로 보며 첫 쓰기가 메모면 기록하고 통과한다. `$CLAUDE_PROJECT_DIR/.claude/state/merge-gate/main-checkout`이 파일인 메인 checkout은 위치·메모 규칙을 적용하지 않으며 우편함 출력 규칙은 적용한다.

상태·막은 기록은 Git 제외 `$CLAUDE_PROJECT_DIR/.claude/state/session-guard/<session_id>.json`에 있다. `version: 1`, `memoWrittenAt`(UTC 또는 null), `denials[]`의 `at`·`tool`·`code`·`target`, `errors[]`의 `at`·`message`를 저장한다. 같은 폴더 임시 파일 + rename으로 저장하며 기록 저장 실패에도 차단 결정은 유지한다. 메모 기록은 PreToolUse 시점에 남긴다.

settings가 바뀌면 실행 중인 Claude 세션에도 다음 도구 호출부터 hook이 실린다. 가드 상태에 메모 기록이 없는 그 세션의 다음 쓰기는 `memo-first`로 막힌다. 그 세션의 맥락 메모를 Write·Edit 도구로 한 번 고쳐 쓰면 풀린다([goal 현재 결과 / 구현](../01_Phases/goals/2026-10-10-operating-tool-guards/goal.md#구현)).

셸 한계는 별칭·스크립트 파일·변수 속 명령, 스크립트 내부 쓰기와 `cp`·`mv` 목적지다. 규칙 1은 `orca orchestration` 명령 자신의 리다이렉트와, 그 명령 또는 그 명령에서 이어지는 파이프라인의 마지막 명령 바로 뒤의 `&`만 본다(파이프 판정은 위 결과 코드 표에 있다). `orca … && echo done &`, `( orca … ) &`, `{ orca …; } &`, `{ orca …; } > /dev/null`처럼 목록·묶음으로 감싼 출력 버림은 보지 않는다. `>& 파일`에서 파일이 fd 번호나 `-`가 아닌 단어여도 fd 복제로 보아 쓰기 목적지로 세지 않는다.

변수는 같은 명령 앞의 대입(`export` 포함), hook 환경 순으로 푼다. `CLAUDE_PROJECT_DIR`처럼 hook 환경에만 있는 변수는 Bash 셸에서는 빈 값일 수 있어 판정 경로와 실제 쓰기 경로가 다를 수 있다.

호출 사이의 `cd`는 다음 호출의 입력 `cwd`에 반영된다. **같은 명령 안의 `cd`는 따라가지 않는다**. 그래서 메모는 Write 도구를 사용하거나 절대 경로나 입력 `cwd` 기준 경로로 쓰고, 같은 명령 안 `cd` 뒤의 상대 경로나 같은 명령 안에서 대입하지 않은 셸 변수 경로로 쓰지 않는다. 입력 `cwd` 기준으로 메모 경로가 아니거나 변수가 풀리지 않는 쓰기는 메모로 기록되지 않는다.

메모 순서는 Claude 세션만 막으며 Codex는 계약과 독립 판정으로 확인한다. `claude --resume`의 같은 `session_id` 연결 여부는 실제 진입 확인 대상이다([goal 위험 2·3](../01_Phases/goals/2026-10-10-operating-tool-guards/goal.md#위험)).

독립 회귀는 `node --test 99_Tools/SessionGuard.Tests/*.test.mjs`(PowerShell에서는 glob을 따옴표로 감싼다)다. [code-rules workflow](../.github/workflows/code-rules.yml)의 `Run independent SessionGuard regressions` 단계는 시험 부재/load 실패/nonzero를 실패 처리하고 `$RULES_OUTPUT/session-guard-independent-tests/`에 command/stdout/stderr/exit를 보존한다. 로컬 회귀·실제 Claude 세션 진입·원격 CI는 각각의 실행 근거로 확인한다.

## 생존 신호 간격

[Orca/liveness-policy.mjs](Orca/liveness-policy.mjs)의 `evaluateLiveness(inputs, thresholdSeconds)`는 순수 판정이고 [Orca/check-liveness.mjs](Orca/check-liveness.mjs)는 저장된 로컬 UTF-8 JSON 파일 하나 이상을 읽는 CLI다. Node 표준 라이브러리만 사용한다. 저장소 루트에서 실행한다.

```powershell
node 99_Tools/Orca/check-liveness.mjs .backups/verification/우편함.json
node 99_Tools/Orca/check-liveness.mjs .backups/verification/우편함.json .backups/verification/추가-우편함.json --threshold-seconds 300
```

`--threshold-seconds`는 한 번만 쓰는 유한한 0 이상 초 값이며 기본은 300초다. 입력은 리드가 저장한 Orca `inbox --json`·`check --json`의 `result.messages`, 앞의 `_keepalive` JSON 줄을 건너뛴 대기 결과, 또는 메시지 객체 배열이다. 각 메시지는 `id`·`type`·`from_handle`·유효한 송신 시각 `created_at`을 보존한다. `payload`는 객체·JSON 객체 문자열·null이며 선택 `sequence`는 안전한 정수, `thread_id`는 답의 원래 질문 id다. `to_handle`은 생략·null 또는 문자열이며 다른 형식은 입력 오류다. 여러 파일은 메시지 id로 합치며 중복 id는 먼저 읽은 원문을 쓴다.

`payload.dispatchId`로 묶고 없으면 `from_handle`의 `dispatch:<id>`를 쓴다. Dispatch 없는 메시지도 질문 답을 찾는 데 사용하며 Dispatch가 하나도 없으면 입력 오류다. heartbeat뿐 아니라 status·question·escalation·worker_done 등 그 Dispatch가 보낸 모든 메시지가 신호다. `to_handle`이 `dispatch:`로 시작하는 후속·답은 payload에 `dispatchId`가 있어도 어떤 Dispatch의 신호에도 넣지 않으며, `thread_id`로 질문 답을 찾는 데는 계속 쓴다. `to_handle`이 없거나 null이면 기존 묶기 규칙을 따른다. `created_at` 순으로 이웃 간격을 재며 같은 시각 묶음은 모두 sequence가 있으면 오름차순, 누락이 있으면 입력 순이다. 첫 worker_done까지 측정하고 그 뒤는 제외한다. 신호 하나의 최대 간격은 0이며 첫 신호 전·마지막 신호 후는 재지 않는다.

질문 Q의 id를 `thread_id`로 가진 다른 발신자의 가장 이른 답 R이 다음 신호 N 이전(같은 시각 포함)에 있으면 Q→R은 질문 대기, R→N은 보통 간격이다. 답이 없거나 N보다 늦으면 Q→N이 보통 간격이다. 질문 대기는 길이와 관계없이 따로 내고 최대 간격·초과에서는 뺀다. 답 뒤 간격은 계속 검사한다.

| status·exit | 의미 |
|---|---|
| `within` · 0 | 모든 보통 간격이 기준 이하(300초도 기본 기준 안) |
| `over` · 1 | 보통 간격이 기준을 넘은 Dispatch가 하나 이상 |
| `input-error` · 2 | 인자·UTF-8·파일·JSON·메시지 필드·도구 오류 또는 Dispatch 0개 |

stdout은 `status`·`thresholdSeconds`·`dispatches[]`·`diagnostics[]`를 담은 JSON 하나다. Dispatch별로 `dispatchId`·`signalCount`·`firstAt`·`lastAt`·`maxGapSeconds`·`overGaps[]`·`questionWaits[]`를 낸다. 간격 항목은 `{ from: { id, at, type }, to: { id, at, type }, seconds }`이고 질문 대기에는 `replyId`도 있다. 진단은 `{ code, path, message, repair }`로 원인·입력 위치·고치는 법을 알린다. 자체 상태/출력 파일을 만들지 않으며 stdout 보존 위치는 호출자가 지정한다.

저장 원시에 있는 신호 간격만 분석하며 파일 쓰기·네트워크·Orca 상태 변경은 없다. 실시간 감시나 메시지 수신 판정은 하지 않는다. 저장 창(`inbox --limit` 등) 밖의 앞 신호는 빠지므로, 정산 원시는 Dispatch 시작 전부터 담거나 여러 원시를 함께 넣어 id로 합친다. 독립 회귀는 `node --test 99_Tools/Orca.Tests/*.test.mjs`(PowerShell에서는 glob을 따옴표로 감싼다)다. [code-rules workflow](../.github/workflows/code-rules.yml)의 기존 Orca 단계가 `node --test 99_Tools/Orca.Tests/*.test.mjs`로 수신·간격 시험 전부를 실행하고 `$RULES_OUTPUT/orca-independent-tests/`에 command/stdout/stderr/exit를 보존한다. 저장 fixture 회귀와 실제 작업자 정산의 원시 분석·원격 CI는 별도 근거다.

## 후보 도착 검사

BACKLOG ID의 중복과 goal 후보가 BACKLOG 또는 기존 goal에 도착했는지 검사한다.

[Backlog/candidate-policy.mjs](Backlog/candidate-policy.mjs)의 `extractBacklogIds(text)`와 `checkCandidates(input)`은 순수 판정이며 [Backlog/check-candidates.mjs](Backlog/check-candidates.mjs)는 로컬 UTF-8 Markdown을 읽는 CLI다. Node 표준 라이브러리만 사용한다. 저장소 루트에서 다음처럼 실행한다.

```powershell
node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md
node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-09-plan-boundary-and-junction/goal.md
```

`--backlog`는 필수이고 `--goal`은 선택이며 각 옵션은 한 번만 쓴다. LF·CRLF를 모두 받으며 경로의 `\`는 `/`로 바꾼다. ID 표는 첫 머리 칸이 정확히 `ID`이고 바로 다음 줄이 `|`·`-`·`:`·공백으로 된 구분 줄인 표다. 데이터 첫 칸의 둘레 공백과 양끝 백틱 한 쌍을 벗기므로 백틱 없는 ID도 센다. `extractBacklogIds`는 파일 순서대로 `{ id, line }`을 반환하고 빈 ID·중복 행도 보존한다.

goal에는 정확한 `## 다음 계획 후보` 제목이 하나 있어야 한다(끝 공백 무시). 다음 `# `·`## ` 제목 전까지 열 0의 `- `·`* `·`+ `·`1. ` 꼴 목록마다 후보 하나다. 이어지는 들여쓴 줄과 빈 줄은 같은 후보이며 들여쓰지 않은 문단·제목·앵커나 다음 후보에서 끝난다. 후보 줄에는 ``BACKLOG `<id>` `` 또는 `[예정 goal](../예정-goal/goal.md)`처럼 기존 goal 링크를 둔다. 영숫자에 붙지 않은 `BACKLOG` 바로 뒤의 백틱 ID와 `·`·`,`·`/`로 이어진 백틱 ID만 인용이다. 다른 자리의 백틱은 인용으로 세지 않는다.

goal 링크는 대상의 `#` 뒤를 뗀 파일명이 `goal.md`이고 검사 중인 goal과 다른 기존 파일이어야 한다. 경로는 goal 폴더 기준으로 정규화하며 `http(s)://` 링크와 자기 자신은 제외한다. 다른 절·후보 밖 문단과 링크 anchor의 존재는 검사하지 않는다. 모듈 입력은 `{ backlogText, backlogPath, goalText, goalPath, isExistingGoal }`이며 goal을 생략하면 BACKLOG만 검사한다. `isExistingGoal(path)`에는 정규화한 `/` 경로를 전달하고 파일 존재 판단은 호출자가 제공한다.

stdout은 `{ status, exitCode, backlog, goal, counts, diagnostics }` JSON 한 개다. `allowed`는 exit0, `policy-violation`은 exit1, `input-error`는 exit2다. `backlog`·`goal`은 받은 경로(`/` 표기)이고 goal 생략 시 null이다. 입력 오류이면 오류 진단만 내고 `counts`는 null이다. 나머지 결과의 counts는 다음과 같다.

| counts 필드 | 뜻 |
|---|---|
| `backlogIds` | ID 표 데이터 행 수(빈 ID·중복 포함) |
| `duplicateIds` | 두 번 이상 나온 서로 다른 ID 수(빈 ID 제외) |
| `candidates` | 후보 수 |
| `candidatesWithoutReference` | 인용도 유효한 goal 링크도 없는 후보 수 |
| `unknownIds` | BACKLOG에 없는 ID의 인용 건수(같은 ID 재인용도 별도) |

goal 생략 시 후보 관련 세 값은 null이며 후보가 없으면 0이다. 각 진단은 `{ code, path, line, message, repair }`로 원인·수리 안내를 담는다. 줄은 1부터 세며 파일/인자 단위 진단은 null이다.

| 진단 code | 상태와 위치 |
|---|---|
| `duplicate-backlog-id` | 정책 위반, 같은 ID의 두 번째 이후 각 행 |
| `empty-backlog-id` | 정책 위반, 빈 ID의 각 행 |
| `missing-reference` | 정책 위반, 참조 없는 후보의 첫 줄 |
| `unknown-backlog-id` | 정책 위반, 도착하지 않은 각 ID 인용 줄(message에 ID 포함) |
| `no-id-table` | 입력 오류, BACKLOG 파일·line null |
| `missing-section` | 입력 오류, goal 파일·line null |
| `duplicate-section` | 입력 오류, goal의 두 번째 후보 제목 줄 |
| `usage` | 입력 오류, 인자 누락·중복·알 수 없는 옵션·값 없음; path/line null |
| `unreadable-file` | 입력 오류, 읽을 수 없는 파일 경로·line null |

인용 ID가 하나라도 있으면 그 ID의 도착 여부를 판정하고 `missing-reference`는 내지 않는다. CLI는 파일 쓰기·네트워크 접근·Git 변경을 하지 않는다. [goal-loop](../.agents/skills/dawnholder-goal-loop/SKILL.md#기준과-상태)의 종료 기록 전 검사에 쓴다. 독립 회귀는 `node --test 99_Tools/Backlog.Tests/*.test.mjs`(PowerShell에서는 glob을 따옴표로 감싼다)다. [code-rules workflow](../.github/workflows/code-rules.yml)의 `Run independent Backlog candidate regressions` 단계는 시험 부재/load 실패/nonzero도 실패 처리하고 `$RULES_OUTPUT/backlog-independent-tests/`에 command/stdout/stderr/exit를 보존한다. 이 CI 단계는 회귀 시험을 실행하며 PR의 goal 후보 위반을 자동 차단하는 단계는 아니다.

## Codex 병합 금지 가드

[CodexGuard/codex-hook.mjs](CodexGuard/codex-hook.mjs)는 인자 없이 stdin의 Codex PreToolUse JSON을 읽는다. `hook_event_name: "PreToolUse"`, `tool_name: "Bash"`, 문자열 `tool_input.command`와 실행 위치 `cwd`가 공개 입력이다. 저장 입력의 직접 회귀 진입은 다음과 같다.

```bash
node 99_Tools/CodexGuard/codex-hook.mjs < hook-input.json
node --test "99_Tools/CodexGuard.Tests/*.test.mjs"
```

[프로젝트 등록](../.codex/hooks.json)은 `^Bash$`에 동기 command 하나를 둔다. POSIX `command`와 Windows 기본 `cmd /C`용 `commandWindows`는 `git rev-parse --show-toplevel`의 로컬 결과로 정확한 진입 파일을 찾는다. 공백 root·하위 cwd의 인용을 유지하며 root 조회 실패·빈 결과·entry 부재는 이유를 stderr에 쓰고 exit2로 막는다. Node stdin·출력·exit는 그대로 전달한다. session cwd가 저장소 밖이거나 중첩 별도 저장소면 그 위치의 Git root를 사용하므로 프로젝트 선언 위치와 달라질 수 있다. 별도 hook 셸 설정의 Windows 실행은 별도 확인 대상이다.

판정 책임은 [command-policy](CodexGuard/command-policy.mjs)의 순회, [shell-command](CodexGuard/shell-command.mjs)의 명시 셸 읽기, [gh-policy](CodexGuard/gh-policy.mjs)의 gh 인자/API, [git-push-policy](CodexGuard/git-push-policy.mjs)의 로컬 Git 읽기·목적지 정책으로 나뉜다. Node 표준 라이브러리만 쓰고 입력 명령을 실행하지 않는다. Git 읽기는 셸 없이 고정 read-only 하위 명령과 분리된 인자로 실행한다. 한 번의 Git 읽기는 최대3초·출력2MiB, 전체 판정은12초·명령2048개·중첩32단계, stdin은1MiB를 상한으로 한다. handler timeout20초는 내부 판정 오류를 deny로 돌려줄 여유를 둔다.

| 구분 | 지원 동작 |
|---|---|
| gh 차단 | 모든 역할의 `gh pr merge`(auto·disable-auto 포함), REST PR merge endpoint의 비GET/필드 기본POST, inline GraphQL `mergePullRequest`·`enablePullRequestAutoMerge`·`enqueuePullRequest` mutation |
| gh 보존 | PR create/view/checks, 일반 조회·명시 GET merge 조회·`disablePullRequestAutoMerge`, 다른 REST 쓰기와 데이터 인자 속 낱말 |
| Git 차단 | 모든 원격의 main·그 원격의 로컬 근거로 확인한 기본 branch로 향하는 push. refspec·복수·force/delete·wildcard·all/mirror, 기본 push·remote.push·push.default·원격 선택 설정을 검사 |
| Git 보존 | 로컬 merge·조회, 확인된 작업 branch 목적지, branch와 구분되는 명시 tag ref. 출발 branch 이름만으로 막지 않음 |
| 셸 | 명시 compound·pipe·줄바꿈·기본 POSIX/PowerShell 제어 형태, `bash/sh -c`·`pwsh/powershell -Command`·`cmd /c`, `env`·`command`·`exec`, 명시 cd/Set-Location·git -C, 실행되는 명령치환·한 리터럴 문자열 eval/iex |
| 데이터 | 인용된 메시지·PR제목·검색문구·주석·heredoc 본문 자체는 명령으로 취급하지 않음. 비인용 POSIX heredoc의 명령치환은 검사 |

원격 기본 branch 근거는 `refs/remotes/<remote>/HEAD`의 유효한 로컬 symbolic ref다. 단일 추적 branch 이름으로 기본 branch를 추정하지 않는다. 근거가 없으면 대상 원격을 밝힌 **Error deny**와 사람이 검증한 기본 branch로 `git remote set-head <remote> <verified-default-branch>`를 수행하는 복구 안내를 낸다(그 추적 ref가 이미 있어야 함). 가드는 네트워크 조회나 refs/config 수리를 하지 않는다. 명시 main 갱신은 이 근거가 없어도 **Policy deny**다. 원격 URL은 일치하는 로컬 remote 설정으로 대응하며 대응되지 않으면 기본 branch 확인 오류다. matching/wildcard 판단에 사용하는 로컬 refs는 실제 원격과 다를 수 있다.

정책 차단과 잘못된 JSON/대상 입력·잡힌 내부 예외·필요한 Git 읽기 실패 모두 exit0의 `hookSpecificOutput: { hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason }` 한 객체를 반환하며 이유의 Policy/Error로 구별한다. 정상·비대상 사건/도구는 stdout 없이 exit0으로 기존 권한 판단에 넘긴다. 명시 allow/ask·명령 rewrite·Claude 승인 소비·메인 예외는 없다. stdout에는 진단 로그를 섞지 않는다.

설치 Codex0.162.1의 공식 문서/대응 소스 기준 등록이다. 정상 TUI의 프로젝트 신뢰와 hook 신뢰는 필요하며 최초/변경 picker는 [R-6](../00_Document/operations/ORCA.md#r6-first-screen)에 따라 메인에게 넘긴다. 신뢰는 사용자 설정의 source 경로·event·group/handler 위치별 선언 해시 상태다. 같은 경로·정의의 새 세션에서 유지될 수 있다는 것은 소스상 추론이며 외부 스크립트 본문까지 해시로 보호한다고 주장하지 않는다. 에이전트가 전역 신뢰 설정을 쓰거나 우회하지 않는다. headless `codex exec`는 미신뢰 hook을 건너뛸 수 있다.

이것은 명시 명령 검사이며 우회 불가능한 권한 시스템이 아니다. 변수/계산된 문자열·별칭/함수·외부 script 내부·EncodedCommand 본문, 재사용 셸 `write_stdin`(PreToolUse 재호출 없음), 다른 클라이언트/MCP는 보호 범위 밖이다. `gh api --input`·`field=@file` 본문은 읽지 않지만 명시 REST merge endpoint의 변경 호출은 막는다. hook/등록 자체 변경, 미기동·timeout·강제종료·출력 전달 실패는 잡힌 오류 deny와 다르며 Codex가 계속할 수 있다. 셸 조건·파이프의 실제 실행 여부와 외부 명령이 바꿀 미래 저장소 상태는 실행해 예측하지 않는다.

독립 회귀 CI는 [code-rules](../.github/workflows/code-rules.yml)의 `Run independent CodexGuard regressions` 한 단계다. 시험 부재/load 실패/nonzero를 실패 처리하고 기존 artifact의 `$RULES_OUTPUT/codex-guard-independent-tests/`에 command/stdout/stderr/exit를 보존한다. stdin 회귀와 등록 wrapper의 로컬 자체검사는 실제 Codex 발동 검증이 아니다. 정상 최초 trust·새 세션 유지·exec_command/code-mode·headless 신뢰 전후·timeout, Linux 원격 CI는 각각 후속 독립 실행 근거로 확인한다.

## 병합 관문

[MergeGate/claude-hook.mjs](MergeGate/claude-hook.mjs)는 인자 없이 stdin hook JSON을 읽는다. `.claude/settings.json`이 `node "$CLAUDE_PROJECT_DIR/99_Tools/MergeGate/claude-hook.mjs"`를 세 사건에 등록한다. PreToolUse matcher는 `Bash|Monitor|Write|Edit|MultiEdit|NotebookEdit|CronCreate|ScheduleWakeup|SendMessage|RemoteTrigger`, PermissionRequest matcher는 `Bash`다. 상태는 `$CLAUDE_PROJECT_DIR/.claude/state/merge-gate/`의 `main-checkout`과 `approvals/<session_id>.json`이며 Git 제외다. Node 표준 라이브러리만 사용하고 모든 결과의 exit는 0이다.

| 결과·코드 | 의미 |
|---|---|
| `approved PR<번호>` / PermissionRequest allow / 결정 없음 | 사용 기록 저장 뒤 통과 JSON / 같은 명령의 확인 허용 JSON / 빈 stdout |
| `not-main-checkout` · `subagent` · `no-approval` | 메인 표식 없음 · 하위 에이전트 · 해당 세션/PR 승인 없음 |
| `head-mismatch` · `expired` · `already-used` | 승인 head 불일치 · 30분 만료 · 이미 소비한 기록 |
| `compound-command` · `forbidden-flag` · `missing-match-head-commit` · `bad-form` | 복합·감싼 명령 또는 다른 명령·환경 변수 할당 뒤 병합 · 금지 플래그 · 별도 head 인자 없음(`=` 형태 불가) · 맨 앞 gh 명령 낱말의 단독 형태 불일치(낱말 사이 `-R`·`--repo` 옵션·대문자·경로·`.exe` 등; 그 밖의 값 옵션은 마지막 그물로 차단) |
| `non-bash-merge` | Monitor의 병합 시도는 승인 기록이 있어도 차단; Bash 단독 명령만 통과 경로가 있음 |
| `api-merge` · `push-main` · `branch-lookup-failed` | API 병합 · main 목적지 push · 입력 cwd의 branch 조회 실패 |
| `protected-path` · `approval-injection` | 상태 경로 접근 · 다른 터미널이나 예약·전달 prompt 도구의 입력(중첩 문자열 포함)에 승인 문장 주입 |
| `suspect-words` | 단독 병합 형태가 아닌 셸 명령 원문의 병합·main push 의심 낱말 동반; 병합은 단독 명령으로, 다른 명령은 나눠 실행하거나 문구를 파일로 넘김 |
| `invalid-input` · `state-write-failed` | 입력/도구 해석 실패 · 사용 기록 저장 실패; 모두 deny JSON과 원인·수리 안내 |

순차 호출 계약이다. 같은 세션의 기록 쓰기가 겹치면(병합 시도끼리, 승인 문장 제출과 병합 시도) 두 번 통과하거나 소비된 기록을 되살릴 수 있어, 실패한 병합을 30분 안에 새 승인 없이 재시도할 여지가 있다. 되살아나는 기록은 원래 승인된 PR·head에 한정된다.

셸 판정은 줄 이음을 접고 경계 읽기와 `${…}`·`$(…)`·역따옴표 치환을 자리 낱말로 묶은 읽기의 단순 명령을 모두 본다. 따옴표 안도 판정하며 건너뛰는 옵션·값 낱말은 따옴표가 닫히는 낱말까지 묶되, 닫히지 않으면 처음 한 낱말만 건너뛴다. 정밀 차단이 먼저고, 정확한 단독 병합 형태는 마지막 그물을 건너뛴다. 그 밖에는 원문의 gh·merge, gh·api·병합 낱말, push·main 대상 낱말 동반을 branch 조회 전에 막는다. 제목의 `merge`(`gh pr create --title merge --body text`), 같은 줄의 main과 작업 branch push(`git fetch origin main && git push origin feat/x`), 메시지의 push·main(`git commit -m "fix(gate): push to main"`)도 막히므로 나눠 실행하거나 문구를 파일로 넘긴다. 한계는 입력 출처 미구분·Codex 미적용·문자열 판정의 의도적 우회(별칭·스크립트 파일·변수 속 명령, 펼쳐질 변수·치환 값 자체)다. 등록 밖 도구와 hook·settings 파일 자체 수정, 하위 폴더·settings 부재나 hook 미실행은 보호하지 않는다. 셸 상태 경로의 구분자·`/./`는 접지만 `..`는 풀지 않는다.

독립 회귀는 `node --test 99_Tools/MergeGate.Tests/*.test.mjs`다. [code-rules workflow](../.github/workflows/code-rules.yml)의 `Run independent MergeGate regressions` 단계는 시험 부재/load 실패/nonzero도 실패 처리하고 `$RULES_OUTPUT/merge-gate-independent-tests/`에 command/stdout/stderr/exit를 보존한다. 운영 절차·지원 한계는 [병합 관문 정본](../00_Document/operations/ORCA.md#merge-gate)을 따른다.
