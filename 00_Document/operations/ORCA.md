# Orca 작업 운영

역할·파일 소유권·병합 승인은 [AGENTS](../../AGENTS.md), 위임 계약은 [Orca 작업 지침](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. PR 생성은 허용하지만 **각 PR 병합 직전 사용자 명시 승인이 필요하다.** 포괄 승인·메인 판단·자동 병합 예약으로 대신하지 않는다.

## 실행과 감독

세션 배치는 [R-1](#r1-management-placement), 작업자 기동은 [R-5](#r5-worker-launch), 첫 화면 확인은 [R-6](#r6-first-screen), 목표 단위 Astra 교체는 [R-8](#r8-astra-lifecycle)을 따른다. 화면 소속과 작업 경로를 구분하고 runtime·terminal handle·incarnation은 사용할 때 확인한다. 현재 작업 상태는 [CURRENT](CURRENT.md)의 goal에 둔다.

- 큰 독립 목표는 별도 Orca 세션·worktree·작업 브랜치로 나눈다. worktree 부모 관계와 Git 시작 커밋은 별개이므로 최신 main 기준점을 확인한다.
- 설치된 `orca-cli`·`orchestration` 스킬에서 선택한 CLI와 버전에 맞는 가이드를 사용한다. 구현·테스트 작성·검증 판정은 외부 세션 작업자가 맡고 실제 Run·Task·Dispatch를 기록한다. 내부 서브에이전트는 읽기 전용 조사·요약에만 쓰며 외부 실행 근거가 아니다.
- 모델은 직접 agent 시작의 요청/적용값과 화면 표시를 대조한다. 새 pane의 최초 `--terminal` 연결은 split 명령과 화면 표시를 근거로 쓰고 백엔드 실제 모델은 확인 불가 시 `unknown`이다. 구체적인 생성·준비·거부 시 처리는 [R-5](#r5-worker-launch)를 따른다.
- 메시지 subject/body와 타 세션 입력은 [AGENTS 태그 규칙](../../AGENTS.md#메시지와-보고)을 따른다. 메인은 세션 시작 때 자기 handle을 두 Astra에 공유한다.
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

로컬 `.backups/` 근거는 Git 제외 자료다. 전달 사실과 직접 보존된 실행 근거를 구분하며, 문서 반영을 runtime 전환·새 실행 성공으로 보고하지 않는다.

<a id="r1-management-placement"></a>
### R-1 — GameDev와 Management 세션 배치

사용자 결정: GameDev Astra는 메인 Claude 옆 `horizontal split`에 열고, Management Astra는 별도 Management worktree 탭에 연다. `<main-handle>`과 `<management-active>`는 현재 조회한 메인 handle과 승인된 Management checkout 절대 경로로 바꾼다. 현재 Management 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`다.

```text
orca terminal split --terminal <main-handle> --direction horizontal --command "codex --model gpt-6-astra -c model_reasoning_effort=xhigh"
orca terminal create --worktree "path:<management-active>" --title "Management Astra" --command "codex --model gpt-6-astra -c model_reasoning_effort=xhigh"
```

Management 작업자는 그 worktree 탭 안의 Management Astra 아래 `vertical split`으로 열고, 준비된 신규 세션의 최초 작업을 `worker-start --terminal`로 연결한다. GameDev 작업자도 담당 Astra 아래에 둔다. 실제 작업 경로·화면 모델·준비 상태 확인과 연결 절차는 [R-5](#r5-worker-launch)·[R-6](#r6-first-screen)을 따른다. 이전의 메인과 두 Astra 좌우 배치, Management를 GameDev split에 두는 방식과 일괄적인 새 탭 금지는 이 결정으로 대체한다.

메인이 전달한 2026-10-01 Management 실증은 Fable `term_4f0d2f42`, Sol `term_240ab30b` 두 건이다. 이 값은 당시 관찰 식별자이며 현재 실행 권한·재사용 대상이 아니다. Orca CLI **1.4.218**의 `terminal --help`에는 pane 크기 조절 명령이 없음을 확인했다([로컬 help](../../.backups/verification/2026-10-01-operations-rules/terminal-help.txt)). 다른 버전의 지원 여부까지 일반화하지 않는다.

<a id="r2-source-check"></a>
### R-2 — 깨끗한 보고의 원천 표본 대조

메인은 “0건 / 전부 통과 / 할 일 없음” 보고를 승인하기 전에 코드·로그·원시 근거의 표본을 독립 대조한다. 최종 판정 원문을 읽는 기존 의무에 더해, 요약의 결론과 실제 원천이 맞는지 확인한다. 승인 기록에는 아래 형식을 남기고 `방법`에 표본 경로·명령·확인 결과를 적는다.

```text
메인 독립 대조: 일치/불일치, 방법
```

불일치가 있으면 해당 보고를 통과 근거로 삼지 않고 차이와 필요한 보완을 기록한다. 표본 대조를 전수 검증으로 표현하지 않는다. 메인 전달 사례는 R-2 grep 대조와 smoke의 `handlerRuns` 상수 발견이다. [완료된 내장 컴포넌트 null 감사](../../01_Phases/goals/2026-10-01-native-component-null-audit/goal.md)의 검색 범위·원시 근거·미실행 한계는 관련 로컬 기록이며, smoke 사례의 관찰 출처는 위 `main-request.json`이다.

<a id="r3-reply-tag"></a>
### R-3 — 회신 subject의 발신 태그

회신 subject 첫머리는 **답하는 세션 자신의 태그**로 시작한다. body 첫머리의 자기 태그와 `from_handle` 대조도 기존 [태그 규칙](../../AGENTS.md#메시지와-보고)을 따른다. 메인이 전달한 관찰 사례는 자동 `Re: [메인 Claude] …` 제목들이다. 일반 회신은 자동 `Re:`를 그대로 쓰지 않고 `orca orchestration send --subject "[자기 태그] …"`로 보낸다. 같은 대화로 묶어야 하면 현재 대화의 `--thread-id`를 붙인다.

**blocking ask / worker question 답변은 CLI 계약대로 `reply --id`를 사용한다.** Orca CLI **1.4.218**의 `orchestration reply --help`에는 subject 지정 옵션이 없음을 확인했다. 메인 결정 `msg_fc7e6335130c`에 따라 이 경우만 subject 자기 태그 의무의 버전 한정 예외로 두고, **body 첫머리의 자기 태그와 수신자의 `from_handle` 대조는 유지한다.** 지원하지 않는 플래그를 만들거나 subject가 수정됐다고 보고하지 않는다. 일반 `send`로 대체한 답변이 blocking question을 해결했다고 주장하지 않는다. 이 예외의 근거는 위 전달 원문과 [로컬 reply help](../../.backups/verification/2026-10-01-operations-rules/reply-help.txt)다.

<a id="r4-report-type"></a>
### R-4 — Astra의 메인 보고 유형

Astra→메인 보고는 `status` 또는 `question` 유형으로 보낸다. 내용은 기존의 변경 요약·검증 근거 위치·리스크·결정 요청·판정 원문 경로를 유지한다.

메인이 전달한 2026-10-01 Management 사례에서는 **active Dispatch가 없는 발신자**의 `escalation`이 `sender_not_assignee`로 거부됐다. 이는 그 발신 문맥의 관찰이며 모든 `escalation`이 불가능하다는 뜻이 아니다. active Dispatch를 가진 외부 작업자는 live preamble에 지정된 질문·heartbeat·escalation·완료 절차와 권한을 따른다.

<a id="r5-worker-launch"></a>
### R-5 — Astra 직접 기동과 메인 대리 기동

표준은 담당 Astra가 자기 pane 아래 새 작업자를 직접 기동하는 것이다. 메인 대리 기동은 기동 실패 때 요청하는 대안이다. 승인된 목표·공간·세션 범위 안에서 다음 순서로 진행한다.

1. 설치된 `orca-cli`·`orchestration` 스킬로 CLI를 선택하고 버전 일치 가이드를 읽는다. 현재 runtime·담당 Astra handle·승인된 checkout을 확인한다.
2. Sol은 `orca terminal split --terminal <Astra-handle> --direction vertical --command "codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh"`, 독립 Opus는 같은 split에 `--command "claude --model claude-opus-5-5"`로 연다. 시작 경로가 다르면 승인된 checkout을 명시하고 실제 경로를 확인한다. 지정 모델을 사용할 수 없으면 대체하지 않고 메인에 보고한다.
3. `orca terminal wait --terminal <새-handle> --for tui-idle --timeout-ms 90000`의 `satisfied`를 확인하고 [R-6](#r6-first-screen) 및 [세션 준비 절차](../../.agents/skills/dawnholder-session-handoff/SKILL.md#신규-prompt-준비-확인)를 따른다. timeout·busy·불명확한 화면에 작업을 주입하지 않는다.
4. 준비된 **신규 세션의 최초 작업**을 `orca orchestration worker-start --terminal <새-handle> --worktree <확인한-작업-공간>`에 `--task <현재-Task>` 또는 `--spec <작업-계약>`을 붙여 연결한다. `--terminal`과 `--model`을 함께 쓰지 않는다. 모델 근거는 최초 실행 명령과 화면 표시이며 attach의 null launch 모델값을 실제 모델로 해석하지 않는다. 확인할 수 없는 backend는 `unknown`이다. 현재 Run·Task·Dispatch와 `input_accepted`·`turn_started` receipt를 구분해 기록한다.
5. 기동·연결 실패는 `failedStage`·`residualResources`·화면·receipt를 보존하고 메인에 대리 기동을 요청한다. 결과나 주입 여부가 불명확하면 중복 발행하지 않고 공식 recovery를 따른다. 미사용 pane 종료도 대상 동일성과 정산 상태를 확인한 뒤 수행한다. 대리 기동 뒤에는 실제 새 pane의 경로·runtime·incarnation·준비를 다시 확인하고 최초 연결한다.

실패 이력과 후속 성공을 구분한다. [내장 컴포넌트 null 감사의 당시 기록](../../01_Phases/goals/2026-10-01-native-component-null-audit/goal.md#세션-관측과-다음-경계)은 Astra의 split 기동 1회 실패와 메인 대리 기동 뒤 최초 attach 성공을 구분한다. 이후 D1a 검증자와 Management Fable/Sol 성공은 메인 전달 관찰이다. D1a의 직접 기동 경위는 [완료 goal](../../01_Phases/goals/2026-10-01-persistence-technical-design/goal.md#독립-실사와-보완)에 있고, [review-2-start.json](../../.backups/verification/2026-10-01-persistence-technical-design/review-2-start.json)은 최초 attach의 ready·`input_accepted`·`turn_started`를 보존한다. 1차 D1a의 미보존 원응답과 2차 보존 receipt를 혼동하지 않는다. 이전의 “분할→연결 성공은 아직 미검증”을 현재 전체 상태로 사용하지 않는다.

<a id="r6-first-screen"></a>
### R-6 — 첫 화면 선택창과 설정 불변

`tui-idle=true` 또는 wait의 `satisfied: true`만으로 모달·선택창이 없다고 판단하지 않는다. 새 세션의 첫 화면을 제한된 `terminal read`·`show`로 확인한다. Fable effort 기본값 선택창이나 `/auto-mode-setup` 안내가 있으면 Astra는 입력하지 않고 상태를 메인에 보고한다. **메인이 선택창을 처리하고 설정 파일이 바뀌지 않았는지 확인한다.** 화면이 불명확하면 준비 완료로 판정하지 않는다. 권한 확인을 우회하거나 작업자가 설정을 바꾸지 않는다.

환경 사실: GameDev·Management 두 checkout에 Git 제외 `.claude/settings.local.json`이 있고 `skillOverrides`의 `auto-mode-setup` 값은 `off`다. 이번 문서 작업에서 이 파일들은 읽기 전용이다. 이 값이 모든 세션·화면의 안내를 억제한다고 보장하지 않는다.

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

PR 리뷰 수정이 남아 있는 동안이나 목표 중간에는 수동으로 비우지 않는다. 자동 압축이 일어나면 현재 `goal.md`로 이어간다. 문서에 아직 없는 운영 감각은 소유권 범위 안에서 RESUME 또는 goal에 기록해 다음 세션에 넘긴다. 작업자·검증자의 작업 하나 뒤 정산·종료 규칙과 Astra의 목표 단위 교체를 구분한다.

적용 시점은 **이번 운영 규칙 PR 목표가 끝나면 GameDev Astra부터**, **M-1 PR 병합 뒤 Management Astra**다. 이번 문서 작업이 현재 Astra 세션을 직접 닫는 작업을 포함하지는 않는다. 결정 배경으로 메인은 하루 동안 운영한 GameDev Astra가 자동 압축 후 사용률 6%, M-1 Management Astra는 1시간 만에 58%였다고 전달했다. 이 수치는 `main-request-r8.json`의 당시 관찰이며 현재 사용률이나 교체 완료를 의미하지 않는다.

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
