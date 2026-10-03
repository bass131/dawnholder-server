# Orca 작업 운영

역할·파일 소유권·병합 승인은 [AGENTS](../../AGENTS.md), 위임 계약은 [Orca 작업 지침](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. PR 생성은 허용하지만 **각 PR 병합 직전 사용자 명시 승인이 필요하다.** 포괄 승인·메인 판단·자동 병합 예약으로 대신하지 않는다.

## 실행과 감독

세션 배치는 [R-1](#r1-management-placement), 작업자 기동은 [R-5](#r5-worker-launch), 첫 화면 확인은 [R-6](#r6-first-screen), 목표 단위 Astra 교체는 [R-8](#r8-astra-lifecycle)을 따른다. 화면 소속과 작업 경로를 구분하고 runtime·terminal handle·incarnation은 사용할 때 확인한다. 현재 작업 상태는 [CURRENT](CURRENT.md)의 goal에 둔다.

- 큰 독립 목표는 별도 Orca 세션·worktree·작업 브랜치로 나눈다. worktree 부모 관계와 Git 시작 커밋은 별개이므로 최신 main 기준점을 확인한다.
- 설치된 `orca-cli`·`orchestration` 스킬에서 선택한 CLI와 버전에 맞는 가이드를 사용한다. 구현·테스트 작성·검증 판정은 외부 세션 작업자가 맡고 실제 Run·Task·Dispatch를 기록한다. 내부 서브에이전트는 읽기 전용 조사·요약에만 쓰며 외부 실행 근거가 아니다.
- 모델은 직접 agent 시작의 요청/적용값과 화면 표시를 대조한다. 새 pane의 최초 `--terminal` 연결은 split 명령과 화면 표시를 근거로 쓰고 백엔드 실제 모델은 확인 불가 시 `unknown`이다. 구체적인 생성·준비·거부 시 처리는 [R-5](#r5-worker-launch)를 따른다.
- 지시는 해당 메시지와 연결된 goal·계약만으로 이해되게 쓴다. 압축 뒤에는 현재 goal·계약·고정 입력을 다시 확인하고 결정과 보고를 대조한다. 특정 모델의 컨텍스트 크기 숫자를 모든 Codex 모델의 불변 사양으로 쓰지 않는다. 현재 Codex 제품 사실을 주장할 때는 로컬 근거를 먼저 확인하고 필요하면 공식 자료를 확인하며 추측을 실행 근거로 기록하지 않는다.
- 메시지 subject/body와 타 세션 입력은 [AGENTS 태그 규칙](../../AGENTS.md#메시지와-보고)을 따른다. 메인은 세션 시작 때 자기 handle을 GameDev·Management·Architecture 세 Astra에 공유한다.
- worker는 live preamble의 확인·heartbeat·결과 절차를 따른다. 작업 하나가 끝나면 `worker_done`과 원문 대조 → release → 정확한 pane 확인·close로 정리하고 재사용하지 않는다. 실패·막힘·무응답은 진단을 보존한 뒤 공식 정산/중단·종료를 수행한다. 수정·재검증은 새 세션으로 발행하고 전체 delivery 처리 뒤 acknowledge한다.
- 실패 시 `failedStage`·`residualResources`와 공식 recovery 명령을 따른다. 타임아웃은 종료 증거가 아니다. 해당 작업의 자원만 정리하고 사용자·메인 터미널은 유지한다.
- 실행 정책·권한 우회 기준은 [AGENTS 공학 조건](../../AGENTS.md#공학-조건)을 따른다.

## 2026-10-01 운영 규칙 정본

R-1~R-8의 상세는 이 절에만 둔다. 다른 현재 운영 문서·프로젝트 스킬은 아래 고정 anchor를 참조한다. 출처는 메인 Claude가 전달한 사용자 결정과 관측이며 사용자 직접 입력으로 격상하지 않는다.

| 전달 원문 | 식별자·시각 | 보존 위치 |
|---|---|---|
| R-1~R-7 | `msg_0f0b14870182`, 2026-10-01 13:14:47 UTC | [main-request.json](../../.backups/verification/2026-10-01-operations-rules/main-request.json) |
| R-8 추가 | `msg_339a1cb74839`, 2026-10-01 13:15:57 UTC | [main-request-r8.json](../../.backups/verification/2026-10-01-operations-rules/main-request-r8.json) |
| R-3 CLI 제약의 회신 예외 | `msg_fc7e6335130c`, 2026-10-01 13:29:31 UTC | [main-r3-decision.json](../../.backups/verification/2026-10-01-operations-rules/main-r3-decision.json) |
| R-1 Architecture 파트 추가 | `msg_39f5b5bcb525`, 2026-10-02 03:14:21 UTC | [main-source-recovered.json](../../.backups/verification/2026-10-02-architecture-part-rules/main-source-recovered.json) |
| R-1 목표 한정 추가 파트 | `msg_9d5215e34c70`, 2026-10-02 메인 전달 | [현재 목표의 승인 결정](../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md#현재-상태와-승인된-결정) |

로컬 `.backups/` 근거는 Git 제외 자료다. 전달 사실과 직접 보존된 실행 근거를 구분하며, 문서 반영을 runtime 전환·새 실행 성공으로 보고하지 않는다.

<a id="r1-management-placement"></a>
### R-1 — GameDev·Management·Architecture 세션 배치

사용자 결정: GameDev Astra는 메인 Claude 옆 `horizontal split`에 열고, Management Astra는 별도 Management worktree 탭에 연다. Architecture Astra는 사용자의 세 번째 파트 결정에 따른 메인 지시로 별도 Architecture worktree 탭에 연다. `<main-handle>`, `<management-active>`, `<architecture-active>`는 현재 조회한 메인 handle과 승인된 각 checkout 절대 경로로 바꾼다. 현재 Management 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`, Architecture 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`다.

```text
orca terminal split --terminal <main-handle> --direction horizontal --command "codex --model gpt-6-astra -c model_reasoning_effort=xhigh"
orca terminal create --worktree "path:<management-active>" --title "Management Astra" --command "codex --model gpt-6-astra -c model_reasoning_effort=xhigh"
orca terminal create --worktree "path:<architecture-active>" --title "Architecture Astra" --command "codex --model gpt-6-astra -c model_reasoning_effort=xhigh"
```

Management·Architecture 작업자는 각 worktree 탭 안의 담당 Astra 아래 `vertical split`으로 열고, 준비된 신규 세션의 최초 작업을 `worker-start --terminal`로 연결한다. GameDev 작업자도 담당 Astra 아래에 둔다. 세 파트 모두 실제 작업 경로·화면 모델·준비 상태 확인과 연결 절차는 [R-5](#r5-worker-launch)·[R-6](#r6-first-screen)을 따른다. 이전의 메인과 두 Astra 좌우 배치, Management를 GameDev split에 두는 방식과 일괄적인 새 탭 금지는 이 결정으로 대체한다.

pane의 소속 worktree와 shell의 실제 cwd는 별도로 확인한다. GameDev 소속 pane을 Management 작업자로 연결해 `terminal_worktree_mismatch`가 난 과거 사례처럼 cwd만 옮겨 소속 불일치를 우회하지 않는다. 현재 조회한 worktree/handle/identity를 기준으로 승인된 배치를 대조한다.

메인이 전달한 2026-10-01 Management 실증은 Fable `term_4f0d2f42`, Sol `term_240ab30b` 두 건이다. 이 값은 당시 관찰 식별자이며 현재 실행 권한·재사용 대상이 아니다. Orca CLI **1.4.218**의 `terminal --help`에는 pane 크기 조절 명령이 없음을 확인했다([로컬 help](../../.backups/verification/2026-10-01-operations-rules/terminal-help.txt)). 다른 버전의 지원 여부까지 일반화하지 않는다.

기본 파트는 GameDev·Management·Architecture 세 Astra로 유지한다. **추가 파트는 사용자 승인 시 메인이 별도 worktree 탭에 열고 목표 종료 때 닫는다.** 추가 파트의 작업자·검증자도 담당 Astra 아래 `vertical split`과 R-5·R-6을 따르며, 태그는 [AGENTS](../../AGENTS.md#메시지와-보고)의 `[<파트> Astra]`/`[<파트> Sol]`/`[<파트> 검증자]` 형식을 쓴다. 추가 파트 승인이나 태그가 기존 모델·쓰기·Git·병합 권한을 넓히지 않는다.

추가 파트의 목표 종료 판정은 해당 goal의 완료조건과 [R-8](#r8-astra-lifecycle)을 따른다. 기본 세 파트의 배치와 R-8 교체 규칙은 유지한다.

<a id="r2-source-check"></a>
### R-2 — 깨끗한 보고의 원천 표본 대조

메인은 “0건 / 전부 통과 / 할 일 없음” 보고를 승인하기 전에 코드·로그·원시 근거의 표본을 독립 대조한다. 최종 판정 원문을 읽는 기존 의무에 더해, 요약의 결론과 실제 원천이 맞는지 확인한다. 승인 기록에는 아래 형식을 남기고 `방법`에 표본 경로·명령·확인 결과를 적는다.

```text
메인 독립 대조: 일치/불일치, 방법
```

불일치가 있으면 해당 보고를 통과 근거로 삼지 않고 차이와 필요한 보완을 기록한다. 표본 대조를 전수 검증으로 표현하지 않는다. 메인 전달 사례는 R-2 grep 대조와 smoke의 `handlerRuns` 상수 발견이다. [완료된 내장 컴포넌트 null 감사](../../01_Phases/goals/2026-10-01-native-component-null-audit/goal.md)의 검색 범위·원시 근거·미실행 한계는 관련 로컬 기록이며, smoke 사례의 관찰 출처는 위 `main-request.json`이다.

독립 검증자의 “전부 / 없음” 주장과 **대상 0건으로 얻은 PASS**도 대조 대상이다. 대상 수·언어·OS와 실제 실행 명령/원시 출력에서 무엇을 검사했는지 확인하고, 구현자 목록을 재사용하지 않고 같은 범위를 독립 열거했는지 본다. 기존 테스트를 고쳤다면 [실패 전수 분류 표](../../.agents/skills/dawnholder-task-context/references/templates.md#검증-판정)에서 표본을 골라 요구사항 원문과 대조한다. 이 메인 표본은 검증자에게 할당된 전수 분류/대조를 대신하지 않는다. 보고와 수행 불일치·미실행을 통과로 기록한 경우는 의도와 무관하게 즉시 메인에 보고한다.

<a id="r3-reply-tag"></a>
### R-3 — 회신 subject의 발신 태그

회신 subject 첫머리는 **답하는 세션 자신의 태그**로 시작한다. body 첫머리의 자기 태그와 `from_handle` 대조도 기존 [태그 규칙](../../AGENTS.md#메시지와-보고)을 따른다. 메인이 전달한 관찰 사례는 자동 `Re: [메인 Claude] …` 제목들이다. 일반 회신은 자동 `Re:`를 그대로 쓰지 않고 `orca orchestration send --subject "[자기 태그] …"`로 보낸다. 같은 대화로 묶어야 하면 현재 대화의 `--thread-id`를 붙인다.

**blocking ask / worker question 답변은 CLI 계약대로 `reply --id`를 사용한다.** Orca CLI **1.4.218**의 `orchestration reply --help`에는 subject 지정 옵션이 없음을 확인했다. 메인 결정 `msg_fc7e6335130c`에 따라 이 경우만 subject 자기 태그 의무의 버전 한정 예외로 두고, **body 첫머리의 자기 태그와 수신자의 `from_handle` 대조는 유지한다.** 지원하지 않는 플래그를 만들거나 subject가 수정됐다고 보고하지 않는다. 일반 `send`로 대체한 답변이 blocking question을 해결했다고 주장하지 않는다. 이 예외의 근거는 위 전달 원문과 [로컬 reply help](../../.backups/verification/2026-10-01-operations-rules/reply-help.txt)다.

**Orca 1.4.218 공식 blocking `ask`가 생성한 고정 subject `Question`**도 subject 태그의 한정 예외다. 같은 버전 `ask --help`에는 subject 옵션이 없다. body 첫머리 태그와 현재 `from_handle`·Task·Dispatch 대조는 유지하며 하나라도 어긋나면 처리하지 않고 메인에 보고한다. 일반 `send`와 `reply`에 이 새 ask 예외를 적용하지 않는다. 위 reply 고유 예외는 별도 근거와 범위대로 유지한다. **ask가 subject 옵션을 지원하면 ask 예외는 없어진다.** 근거는 메인 범위 판정 `msg_09a19a463a74`(2026-10-03T18:06:16Z)의 [원문](../../.backups/verification/2026-10-03-harness-principles/operating-rules/main-official-ask-decision.json), [ask help](../../.backups/verification/2026-10-03-harness-principles/operating-rules/orca-ask-help.txt), [실제 질문](../../.backups/verification/2026-10-03-harness-principles/operating-rules/sol-heartbeat-boundary-question.json)이다. 값만으로 공식 출처를 증명했다고 주장하지 않으며 [수신 helper](#dispatch-message-policy)의 별도 expected 근거를 확인한다.

<a id="run-reply-address"></a>
### Run 회신 주소와 receipt 확인

Astra는 새 Run을 만들거나 바인딩한 **즉시 메인에게 `run:<현재 run_id>` 회신 주소**를 알린다. Run 우편함과 terminal-only 우편함은 다른 수신 문맥이며 과거 term 주소로 간 우편이 현재 Run의 check에 보이지 않았던 사례가 있다. 바인딩 전 우편은 처리/보존한 뒤 전환한다. 발신 전에 현재 Run의 coordinator handle과 상대가 알려준 회신 주소를 대조하고, 과거 handle을 실행 권한으로 재사용하지 않는다.

못 받았다는 보고에는 현재 CLI로 원 메시지·수신 주소·receipt를 확인한다. **동일 request ID/동일 payload의 공식 receipt replay**는 접수 결과 재확인이며 새 입력/메시지 재전송과 구분한다. runtime·target·incarnation을 다시 대조하고 해당 버전 가이드가 중복 입력 없는 replay를 보장할 때만 사용한다. 재전달이 필요하면 원문 ID와 재전달 관계를 기록하되 accepted 뒤 침묵만으로 텍스트를 다시 보내지 않는다. 확인 불가/미지원은 원문을 보존해 메인에 보고한다.

<a id="dispatch-message-policy"></a>
### 활성 Dispatch 메시지 수신 보조

현재 receipt/worker-show와 질문 원시 상태를 확인한 coordinator가 expected `fromHandle`, `taskId`, `dispatchId`, `tag`를 별도로 제공한다. 메시지에서 기대 identity를 유도하거나 태그만으로 권한을 인정하지 않는다. 메시지의 `from_handle`과 payload의 두 ID가 **모두 정확히 일치**해야 한다. 불일치는 처리하지 않고 메인에 보고한다. JSON 문자열 payload와 이미 파싱한 객체는 같은 순수 검증을 거친다.

- **빈 heartbeat:** type이 `heartbeat`, 세 identity 일치, body가 빈 값이고 subject가 빈 값 또는 정확한 `alive`일 때만 태그 예외다. payload는 두 ID와 선택적 phase(문자열/null/누락) 생존 메타데이터만 가진다. 다른 payload 내용은 태그 검사를 받으며 비문자 phase는 입력 오류다. `alive`는 현재 공식 preamble의 생존 표식으로 좁게 인정한다. subject/body의 누락·`null`·빈 문자열·공백 문자열은 빈 값이며 숫자·배열·객체는 입력 오류다. identity 누락·잘못된 형식은 빈 생존 신호로 허용하지 않는다. 이 경우 교정 메시지를 보내지 않는다.
- 내용 있는 heartbeat와 일반 status/question/worker_done/escalation은 subject/body **첫 글자부터** 자기 태그가 있어야 한다. 앞 공백이나 `Re:`를 태그 앞에 넣지 않는다.
- 공식 ask의 `Question`만 coordinator가 실제 질문 receipt에서 확인한 `expected.officialAsk = { "messageId": "확인한 질문 ID", "cliVersion": "1.4.218" }`를 추가했을 때 예외 판정한다. actual type=question, id=thread_id=확인한 messageId, from_handle=`dispatch:<현재 Dispatch>`, payload.question=body도 대조한다. 이 출처 주소는 term sender와 다르므로 해당 수신 문맥의 기대 발신 주소를 질문 receipt와 독립 대조한다. 일반 send도 thread/payload를 지정할 수 있으므로 모양이나 `official=true` 같은 자기 선언만으로 공식성을 인정하지 않는다. 별도 receipt 근거가 없으면 예외를 열지 않고 현재 CLI/사람 대조로 돌아간다.

입력 파일은 `{ "message": 실제 Orca 메시지 객체, "expected": 독립 확인한 기대값 객체 }`다. 로컬 JSON 파일을 읽고 JSON 판정만 출력한다.

```powershell
node 99_Tools/Orca/check-message.mjs .backups/수신입력.json
```

| 출력 status / exit | 의미와 조치 |
|---|---|
| `allowed` / 0 | 위 수신 정책 허용. 내용 없는 heartbeat 또는 공식 ask 예외는 exception에 표시한다. |
| `policy-violation` / 1 | 실제 identity/태그/확인한 ask 근거 불일치. 처리하지 않고 원문과 수정·보고 안내를 확인한다. |
| `input-error` / 2 | 형식·필수 identity·파일·JSON·지원 문맥/도구 실패. 입력/실행을 복구하거나 수동 원천 대조한다. |

진단은 `code`, `path`, `message`, `repair`를 제공한다. 순수 API는 [message-policy.mjs](../../99_Tools/Orca/message-policy.mjs)의 `evaluateMessage(input)`, 파일 읽기/UTF-8 JSON 출력은 [check-message.mjs](../../99_Tools/Orca/check-message.mjs)다. 상태 변이·메시지 전송·ack·lifecycle 처리·프로세스 강제 종료·파일/네트워크 쓰기는 하지 않는다. 실행 명령과 도구 진입은 [Tools README](../../99_Tools/README.md#orca-메시지-수신-판정)에서도 찾는다.

지원 대상은 **활성 Dispatch의 작업자 메시지 수신 정책**이다. 일반 Main terminal-only 메시지, blocking reply의 기존 subject 예외, 다른 message type, 원격 진위 검증은 지원하지 않는다. 이 보조의 허용이 런타임 진위·현재 작업 완료·수명주기 전환을 증명하지 않는다. 현재 CLI와 receipt/사람 대조가 계속 필요하다. 자체 smoke는 독립 회귀와 구분하며 신규 Opus가 정상·각 ID 불일치·오래된 Dispatch·내용 있는 heartbeat·payload/CLI 실패와 공식 Question의 무태그 body/틀린 발신/일반 send 모방 반례를 독립 검증한다.

**Orca1.4.218의 2026-10-03 관측:** worker-show의 `lastHeartbeatAt=null`과 일반 Run check의 빈 결과에도 공개 inbox에는 해당 Run·Task·Dispatch·from_handle이 일치하는 heartbeat 5건과 `read=1`/`delivered_at`이 있었다. 저장·전달 처리는 원문·주소·identity·시각·receipt로 대조하고 null이나 빈 check만으로 송신 누락·프로세스 종료를 확정하지 않는다. 전송/저장, 정책 허용, 현재 liveness, 완료/정산을 구분하며 inbox 표시를 coordinator의 실제 본문 검토로 확대하지 않는다. 형식·runtime 버그·자동 소비 중 무엇이 원인인지는 미확정이다. 근거는 [메인 정정 요청](../../.backups/verification/2026-10-03-harness-principles/operating-rules/main-heartbeat-observation-decision.json) `msg_29e3012b0274`(18:29:20Z), [inbox 원시](../../.backups/verification/2026-10-03-harness-principles/operating-rules/heartbeat-worker-message-inspection.json), [worker-show 원시](../../.backups/verification/2026-10-03-harness-principles/operating-rules/sol-heartbeat-current-show.json)다. 앞 4건의 태그 있는 subject/빈 body 불일치와 cadence 공백, Astra의 앞 송신 누락 안내 정정은 수행 보고에 따로 보존하며 새 실패 집계나 runtime 수정으로 확대하지 않는다.

<a id="r4-report-type"></a>
### R-4 — Astra의 메인 보고 유형

Astra→메인 보고는 `status` 또는 `question` 유형으로 보낸다. 내용은 기존의 변경 요약·검증 근거 위치·리스크·결정 요청·판정 원문 경로를 유지한다.

메인이 전달한 2026-10-01 Management 사례에서는 **active Dispatch가 없는 발신자**의 `escalation`이 `sender_not_assignee`로 거부됐다. 이는 그 발신 문맥의 관찰이며 모든 `escalation`이 불가능하다는 뜻이 아니다. active Dispatch를 가진 외부 작업자는 live preamble에 지정된 질문·heartbeat·escalation·완료 절차와 권한을 따른다.

<a id="r5-worker-launch"></a>
### R-5 — Astra 직접 기동과 메인 대리 기동

표준은 담당 Astra가 자기 pane 아래 새 작업자를 직접 기동하는 것이다. 메인 대리 기동은 기동 실패 때 요청하는 대안이다. 승인된 목표·공간·세션 범위 안에서 다음 순서로 진행한다.

1. 설치된 `orca-cli`·`orchestration` 스킬로 CLI를 선택하고 버전 일치 가이드를 읽는다. 현재 runtime·담당 Astra handle·승인된 checkout을 확인한다.
2. Sol은 `orca terminal split --terminal <Astra-handle> --direction vertical --command "codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh"`, 독립 Opus는 같은 split에 `--command "claude --model claude-opus-5-5"`로 연다. 시작 경로가 다르면 승인된 checkout을 명시하고 실제 경로를 확인한다. 지정 모델 부재는 대체하지 않고 메인에 보고한다. capacity 관측에 한정한 재시도/신규 작업자 예외는 [capacity 정본](#capacity-retry)을 따른다.
3. `orca terminal wait --terminal <새-handle> --for tui-idle --timeout-ms 90000`의 `satisfied`를 확인하고 [R-6](#r6-first-screen) 및 [세션 준비 절차](../../.agents/skills/dawnholder-session-handoff/SKILL.md#신규-prompt-준비-확인)를 따른다. timeout·busy·불명확한 화면에 작업을 주입하지 않는다.
4. 준비된 **신규 세션의 최초 작업**을 `orca orchestration worker-start --terminal <새-handle> --worktree <확인한-작업-공간>`에 `--task <현재-Task>` 또는 `--spec <작업-계약>`을 붙여 연결한다. `--terminal`과 `--model`을 함께 쓰지 않는다. 모델 근거는 최초 실행 명령과 화면 표시이며 attach의 null launch 모델값을 실제 모델로 해석하지 않는다. 확인할 수 없는 backend는 `unknown`이다. 현재 Run·Task·Dispatch와 `input_accepted`·`turn_started` receipt를 구분해 기록한다.
   `turn_start_unobserved`이면 화면 tail만으로 판정하지 않고 [공식 계약 draft 복구](#official-contract-draft)의 JSON draft·계약·동일성 조건을 확인한다.
5. 기동·연결 실패는 `failedStage`·`residualResources`·화면·receipt를 보존하고 메인에 대리 기동을 요청한다. 결과나 주입 여부가 불명확하면 중복 발행하지 않고 공식 recovery를 따른다. 미사용 pane 종료도 대상 동일성과 정산 상태를 확인한 뒤 수행한다. 대리 기동 뒤에는 실제 새 pane의 경로·runtime·incarnation·준비를 다시 확인하고 최초 연결한다.

실패 이력과 후속 성공을 구분한다. [내장 컴포넌트 null 감사의 당시 기록](../../01_Phases/goals/2026-10-01-native-component-null-audit/goal.md#세션-관측과-다음-경계)은 Astra의 split 기동 1회 실패와 메인 대리 기동 뒤 최초 attach 성공을 구분한다. 이후 D1a 검증자와 Management Fable/Sol 성공은 메인 전달 관찰이다. D1a의 직접 기동 경위는 [완료 goal](../../01_Phases/goals/2026-10-01-persistence-technical-design/goal.md#독립-실사와-보완)에 있고, [review-2-start.json](../../.backups/verification/2026-10-01-persistence-technical-design/review-2-start.json)은 최초 attach의 ready·`input_accepted`·`turn_started`를 보존한다. 1차 D1a의 미보존 원응답과 2차 보존 receipt를 혼동하지 않는다. 이전의 “분할→연결 성공은 아직 미검증”을 현재 전체 상태로 사용하지 않는다.

<a id="capacity-retry"></a>
### capacity 재시도와 신규 작업자 예외

작업자 화면에 `Selected model is at capacity`가 관측되면 **같은 세션·같은 task에서 1→2→5→10분 간격**으로 재시도한다. 첫 capacity 관측부터 누적 30분에도 `gpt-6.1-sol`이 계속 실패하면 기존 작업자를 공식 정산·종료하고 신규 `gpt-6-astra xhigh` **작업자**를 열 수 있다. 기존 세션을 실행 중에 모델 변경하지 않는다. 파트 리드 직접 구현은 금지이고, 독립 Opus는 재시도만 하며 대체하지 않는다. 다른 모델 부재·권한 확인을 이 예외로 우회하지 않는다.

요청 모델·최초 실행/launch·화면 관측·backend 실제 모델 또는 `unknown`, 최초 관측/재시도 시각·누적 시간·전환 사유·사용자 결정 출처를 기록한다. 이는 메인이 전달한 `msg_bc5d8b721551`(2026-10-03T11:15:30Z)의 승인 예외이며 [현재 하네스 goal의 원문 결정](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)에서 찾는다.

<a id="confirmed-failures"></a>
### 확정 실패 집계와 Fable Advisor

**같은 계약·같은 결함 번호**의 Sol `FAILED` 또는 독립 `NOT PASS`가 3회 확정되면 네 번째 시도를 신규 `gpt-6.1-sol xhigh`와 신규 읽기 전용 `claude-fable-5-1` Advisor로 한다. 같은 산출물의 FAILED/NOT PASS는 이중 집계하지 않고 조사 전용 세션·개발 중 자체 smoke 수리는 제외한다. 실패 세션은 작업 하나 뒤 정산·종료하며 재사용하지 않는다.

담당 Astra는 Advisor 기동 전에 실패 이유와 **실패 원문 세 건 경로를 메인 status로 보고**한다. 사용자 사전 승인 규칙이므로 재승인을 기다리지 않는다. Advisor는 조언 파일 하나만 쓰고 제품·테스트·판정은 쓰지 않는다. 새 Sol은 구현 전에 Orca로 Advisor에게 **직접 질문**하고 조언·채택/거절 이유를 수행 보고에 남긴다. 리드 Astra가 구현을 떠맡지 않는다. 기동/모델/선택창은 R-5·R-6을 따른다. 네 번째도 실패하면 **다섯 번째 전에 메인 question으로 판단**을 받는다.

이 규칙의 출처는 메인이 전달한 사용자 최종 결정 `msg_22a9b4109ee7`(2026-10-03T07:08:40Z)이며 [하네스 goal의 적용 결정](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)으로 연결한다. [R-7 goal 검토 시범](#r7-fable-pilot)과 역할·시점·파일 권한이 다르다. 같은 부류 BACKLOG 출처 결함의 특정 사례를 모든 번호별 집계의 대체 규칙으로 쓰지 않는다.

<a id="official-contract-draft"></a>
### 공식 계약 draft 복구

accepted 뒤 침묵만으로 새 텍스트를 전송하거나 abandon하지 않는다. `turn_start_unobserved`이면 현재 runtime·target·incarnation·Dispatch receipt와 **terminal read JSON의 draft 필드**를 확인한다. 화면 tail의 빈 prompt만으로 입력 소실을 단정하지 않는다.

담당 Astra가 직접 **텍스트 없는 Enter 한 번**을 제출할 수 있는 조건은 새 pane이라 다른 입력이 없고, JSON draft가 공식 계약 크기와 맞는 붙여넣기 placeholder라는 두 조건이다. 공식 payload/계약과 그 placeholder를 대조한 근거를 보존한다. 원문이 아닌 placeholder를 보았으면 그 한계도 적는다. 조건·동일성을 확인할 수 없으면 메인에 보고하고 일반 사용자 작성 prompt를 대신 제출하지 않는다.

조건을 충족했을 때 현재 CLI의 `terminal send --terminal <확인한 handle> --enter`로 Enter만 한 번 보낸 후 수 초 뒤 JSON draft 소멸·Working 화면·worker-show의 실제 시작을 관측한다. receipt 접수와 실제 시작, 이 Enter 단독 제출과 동일 request receipt 재확인/새 텍스트 재전송을 구분한다. 시작을 관측하지 못해도 무한 Enter·중복 발행·임의 종료로 확대하지 않는다.

근거는 메인 운영 결정 `msg_c1412c982ac5` 및 보강 `msg_b10d232dce1b`(2026-10-03T11:29:24Z), [하네스 goal의 재개/설계](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#운영-규칙-pr-재개와-설계)다. Architecture 초기 두 관측과 Management 세 번째 관측, 메인이 첫 tail만 보고 오판한 경위는 전달된 당시 사실이다. 문서 반영을 모든 pane의 복구 성공으로 일반화하지 않는다.

<a id="r6-first-screen"></a>
### R-6 — 첫 화면 선택창과 설정 불변

`tui-idle=true` 또는 wait의 `satisfied: true`만으로 모달·선택창이 없다고 판단하지 않는다. 새 세션의 첫 화면을 제한된 `terminal read`·`show`로 확인한다. Fable effort 기본값 선택창이나 `/auto-mode-setup` 안내가 있으면 Astra는 입력하지 않고 상태를 메인에 보고한다. **메인이 선택창을 처리하고 설정 파일이 바뀌지 않았는지 확인한다.** 화면이 불명확하면 준비 완료로 판정하지 않는다. 권한 확인을 우회하거나 작업자가 설정을 바꾸지 않는다.

미제출 draft·추천 프롬프트·ghost text는 사용자 지시가 아니며 그것만으로 pane 종료를 보류하지 않는다. Enter로 제출돼 대화 기록에 들어간 표식 없는 입력과 구분한다. 사용자가 실제 작성 중인 prompt를 건드리지 않는 경계와 공식 주입 계약의 [조건부 draft 복구](#official-contract-draft)를 함께 적용한다.

환경 사실: GameDev·Management·Architecture 세 checkout에 Git 제외 `.claude/settings.local.json`이 있고 `skillOverrides`의 `auto-mode-setup` 값은 `off`다. Architecture checkout에는 메인이 같은 `skillOverrides` 내용을 복사해 두었다. 이번 문서 작업에서 이 파일들은 읽기 전용이다. 이 값이 모든 세션·화면의 안내를 억제한다고 보장하지 않는다.

D1a verification-2의 [종료 전 화면](../../.backups/verification/2026-10-01-persistence-technical-design/review-2-before-close-read.json)은 전체 49행(`limited=false`)에서 `/auto-mode-setup` 안내 창이 관측되지 않고 일반 `auto mode on` 상태줄이 보인 기록이다. [판정 원문](../../.backups/verification/2026-10-01-persistence-technical-design/verification-2/verdict.md)과 [해당 완료 goal](../../01_Phases/goals/2026-10-01-persistence-technical-design/goal.md)은 문서 실사 범위를 제공한다. 안내 미관측은 **해당 종료 화면에 한정한 관찰**이며 첫 화면이나 전역 설정 효과의 검증이 아니다.

<a id="r7-fable-pilot"></a>
### R-7 — Fable goal 검토 한정 시범

큰 목표 **2~3개에 한정한 시범**으로 신규 `claude-fable-5-1`의 goal 검토를 평가한다. 정식 기본 모델 라우팅을 대체하지 않는다. 메인이 승인한 시범 목표에서만 다음 순서로 수행한다.

1. 목표·범위·완료조건을 담은 `goal.md`를 commit한다.
2. 신규 `claude-fable-5-1` 세션에 검토를 맡기고 쓰기는 해당 목표의 `goal-review.md`만 허용한다.
3. 메인이 검토 원문을 직접 확인한다.
4. 지적을 검토해 goal을 보완한다.
5. 메인이 보완 goal을 승인한다.
6. Sol 구현을 발행한다.

평가 기준은 **“메인이 놓친 문제를 실제로 찾았나”**다. 메인 전달 관찰에 따르면 1회차 M-1에서 의미 있는 지적 7건을 찾았고, 그중 #3(Windows rename 간섭)은 구현에서 실측됐다. 이는 `main-request.json`에 보존한 메인의 관찰 보고이며 이번 문서 작업자가 M-1 구현·실측을 직접 검증한 결과가 아니다. 이번 운영 규칙 문서 목표에서는 Fable 세션을 열거나 새 시범 목표를 시작하지 않는다.

<a id="r8-astra-lifecycle"></a>
### R-8 — 목표 단위 Astra 세션 교체

사용자 결정: **PR 병합과 goal 결과 기록이 모두 끝나면** 메인이 해당 Astra pane을 닫고 새로 연다. 새 Astra는 세션 진입과 같은 절차로 현재 handle을 공유하고 READY를 확인하며, [RESUME](RESUME.md#세션-진입-배치)과 새 goal로 시작한다. 실제 경로·runtime·handle·incarnation을 새로 확인하고 이전 목표의 Run·Task·Dispatch·가정을 실행 권한으로 재사용하지 않는다.

여러 PR로 나눈 goal은 **전체 goal의 PR 병합·결과 기록 → Gardener → 종료 점검 → R-8** 순서다. 첫 PR마다 신규 Gardener를 자동으로 추가하지 않는다. 메인/사용자가 결과·남은 위험·BACKLOG·다음 계획을 점검한 뒤 재개하며 다음 goal 자동 착수는 금지한다. [목표 루프](../../.agents/skills/dawnholder-goal-loop/SKILL.md#통합과-보고)와 [마일스톤 운영](../../.agents/skills/dawnholder-goal-loop/references/milestones.md)을 따른다.

PR 리뷰 수정이 남아 있는 동안이나 목표 중간에는 수동으로 비우지 않는다. 자동 압축이 일어나면 현재 `goal.md`로 이어간다. 문서에 아직 없는 운영 감각은 소유권 범위 안에서 RESUME 또는 goal에 기록해 다음 세션에 넘긴다. 작업자·검증자의 작업 하나 뒤 정산·종료 규칙과 Astra의 목표 단위 교체를 구분한다.

적용 시점은 **이번 운영 규칙 PR 목표가 끝나면 GameDev Astra부터**, **M-1 PR 병합 뒤 Management Astra**다. 이번 문서 작업이 현재 Astra 세션을 직접 닫는 작업을 포함하지는 않는다. 결정 배경으로 메인은 하루 동안 운영한 GameDev Astra가 자동 압축 후 사용률 6%, M-1 Management Astra는 1시간 만에 58%였다고 전달했다. 이 수치는 `main-request-r8.json`의 당시 관찰이며 현재 사용률이나 교체 완료를 의미하지 않는다.

위 적용 시점은 2026-10-01 결정 당시 목표를 가리킨 역사 기록이다. 현재 하네스 goal의 첫 BACKLOG PR 병합을 전체 goal 종료나 R-8 시점으로 해석하지 않는다.

<a id="goal-gardener"></a>
### 목표 종료 Gardener 4주 파일럿

전체 goal의 모든 PR 병합과 결과 기록 뒤, **R-8 직전** 담당 Astra가 자기 pane 아래 신규 `claude-opus-5-5`를 읽기 전용으로 연다. 쓰기는 점검 보고서 **한 파일**만 허용한다. 입력은 끝난 goal의 독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사 중 담당 범위다.

반복 빈도와 원시 근거를 바탕으로 정리 후보를 **최대 두 개** 제안하고 후보마다 lint·테스트·fixture·정본 helper 등 검사화 방법을 적는다. 없으면 없음으로 마친다. 검사 실행불가와 실제 위반을 나누며 직접 수정·규칙 채택·다음 goal 발행 권한은 없다. 후보 채택은 메인을 거쳐 사용자가 결정하고 수정은 일반 목표 루프로 한다. **2026-10-31 무렵 비용·잡음으로 지속 여부를 평가**한다. 작은 목표 예외는 미합의이며 첫 PR마다 새 점검을 의무화하지 않는다.

출처는 `msg_c9bc79f8ec43`의 사용자 채택을 보존한 [하네스 goal](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)이다. Gardener의 제안과 채택/실제 구현 완료를 구분한다.

## 공유 자원

| 자원 | 확인할 경계 |
|---|---|
| Shared 프로토콜·생성 코드·DLL | 담당자 하나를 지정해 동시 생성·복사를 피한다. 소비자 양쪽 빌드와 호환성을 확인한다. |
| Unity | checkout 간 Library·Editor 프로젝트를 공유하지 않는다. 필요한 검증만 지정한 작업자가 실행한다. |
| WSL | [실행 안내](DEVELOPMENT.md)의 원본 경로 해시별 복제 공간·소유 marker·lock을 사용한다. override도 목표 전용 Linux 경로여야 한다. |
| 게임 포트 7777 | 단일 실행 소유자를 정하고 전역 lock·listener 검사를 따른다. 다른 프로세스를 임의 종료하지 않는다. |
| DB | 대상 DB와 마이그레이션 담당자를 정한다. worktree 분리만으로 DB가 격리되지는 않는다. |

원문 로그는 목표 evidence 또는 TEMP에 보관한다. 메인에는 변경·검증 요약과 근거 위치를 전달한다. 이 지침은 운영 규약이며 실행·컨텍스트 격리를 기술적으로 강제하는 별도 시스템이 아니다.

## 관찰 기록: 2026-09-29

두 시도는 시작 방식과 확인 범위가 다르다. 연결·작업 주입·완료 수신을 각각 구분한다.

| 시도 | 실제 관찰 | 남은 확인 |
|---|---|---|
| 초기 새 worker 자동 시작 | Orca 1.4.216 연결, 요청·실제 모델 `gpt-6-astra` 일치. Codex TUI는 열렸으나 `agent_readiness`에서 60초 timeout. Task 주입과 `worker_done` 없음. 공식 release 후 `reclaimable=0`, 기존 메인 터미널 유지. | 새 worker 자동 시작·지침 선택·완료 수신은 미검증. 정확한 readiness 실패 원인은 미확정. |
| 사용자 기존 터미널 재사용, MSSQL 목표 | 실제 작업 주입·heartbeat·질문 응답 후 기본 테이블 구현과 독립 Windows 테스트 40개 통과. 사용자 승인 하 관리자 설정 적용 후 네이티브 WSL 로그인·저장·rowversion·rollback·최소 권한 검사 통과. | 기록 시점 최종 push·CI와 `worker_done` 수신 마감 중. 서버 런타임 저장 통합과 관리자 Restore 실행은 미검증. 새 worker 자동 시작 성공으로 해석하지 않는다. |

초기 시도: Run `run_85f26e01d393`, Task `task_d783e29c2337`, Dispatch `ctx_7e635e1b1d19`. 로컬 원본은 `%TEMP%/dawnholder-orca-smoke-20260929/`에 있다.

기존 터미널 재사용: Run `run_af1e4581e074`, Task `task_8973cd06c2c8`, Dispatch `ctx_cbd3c2c78d02`, Terminal `term_103c9e14-2792-432e-8a0e-b00215dbe0fd`. 로컬 상태 증거는 `.backups/mssql-orca-coordination-20260929/worker-show.json`이다. 작업 공간은 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema`, 브랜치는 `bass131/feat-mssql-game-schema`로 문서 정비 공간과 분리했다. 결과와 당시 인계는 별도 [PR127](https://github.com/bass131/dawnholder-server/pull/127)에 기록했다. SQL 테스트의 트랜잭션 rollback 통과와 관리자 설정 Restore 실행 검증은 구분한다. 당시 관찰 시점에는 PR 병합 승인이 없었다.
