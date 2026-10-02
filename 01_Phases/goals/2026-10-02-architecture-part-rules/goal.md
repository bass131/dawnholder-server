# Architecture 0단계 — 세 번째 파트 운영 규칙

## 목표와 범위

메인 Claude가 전달한 세 번째 Architecture Astra 파트 결정을 현재 운영 규칙에 반영한다. 출처는 2026-10-02 메시지 `msg_39f5b5bcb525`이며 사용자 직접 입력과 구분한다. 복구한 원문은 로컬 `.backups/verification/2026-10-02-architecture-part-rules/main-source-recovered.json`에 보존한다. 이전에 원문 위치로 기재한 `main-messages.json`은 CLI 오류 응답만 담고 있었으며 아래 재개 기록에서 정정한다.

- `AGENTS.md`: Architecture Astra·Sol·검증자 태그를 추가한다.
- `00_Document/operations/ORCA.md`: Architecture 전용 worktree 탭과 그 아래 vertical split 작업자 배치를 R-1에 추가한다. 기존 R-1 anchor와 다른 파트 배치는 보존한다.
- `00_Document/operations/RESUME.md`: 세 번째 파트의 세션 진입과 handle 공유를 반영한다.
- `.agents/skills/dawnholder-session-handoff/SKILL.md`: 메인 추가 승인 `msg_8df3b885d77f`에 따라 handle 공유의 “두 Astra” 한 줄만 세 Astra로 수정한다. 다른 스킬 문장은 범위 밖이다.
- `CLAUDE.md`: 메인 Claude 단독 작성이며 쓰기 종료 통보 후 같은 커밋에 포함한다.
- 이 goal과 CURRENT 링크는 담당 Astra가 관리한다.

기존 모델 라우팅·승인·파일 소유·정산 규칙을 바꾸지 않는다. 위 스킬 한 줄 이외의 스킬 변경, 제품 코드, 의존성 설치, 전역 설정, `.claude/settings.local.json`, Management frontend와 A-1 추출기 구현은 범위 밖이다. 작은 작업 예외를 신설하지 않는다.

## 완료조건과 소유권

1. 세 운영 문서와 메인 작성 CLAUDE가 세 파트 배치·태그·역할에서 일관된다.
2. Sol 외부 세션이 세 운영 문서와 승인된 스킬 한 줄만 수정하고 쓰기 종료를 보고한다. 작업자는 동시에 하나만 연다.
3. 모든 작성자의 쓰기 종료 뒤 신규 Opus가 실제 diff·요구사항·링크·권한 보존을 독립 실사한다. 문서 작업이므로 제품 실행 테스트는 요구하지 않으며 미실행으로 기록한다.
4. Astra가 판정 원문과 실행 근거를 대조하고 commit/push/PR을 수행한다. 사용자 병합 승인 전에는 병합하지 않는다.
5. PR 병합과 결과 기록 후 Astra 종료·재진입은 메인이 R-8에 따라 수행한다.

## 작업 기준과 현재 상태

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`.
- 브랜치: `docs/architecture-part-rules-20261002`.
- 기준 main: `333fe20211260ef230cd7d4ef9555cb4d5999c08`. 진입 시 원격 main을 `git ls-remote`로 대조했고 기존 tracked 변경은 없었다.
- 현재 Architecture Astra handle: `term_423a4e96-28cd-4f84-9ac8-d6d1853917fb`, runtime `8a673084-6819-45b9-a551-347226cdce9b`, incarnation `afdd8e9d-8a7d-4a94-9264-e2c822f010e7`. 화면 `GPT-6-Astra xhigh`, backend 실제 모델 `unknown`. 업데이트 전 handle은 `term_366eb418-ef60-48df-9d08-e6b3efa11c08`, incarnation `8a047d4d-9dfc-4dec-bd15-3b9dd56acb28`이었다. ID는 이 목표의 관찰 기록이며 다음 세션의 권한이 아니다.
- 현재: **문서 구현·독립 실사·보완 재검증 완료, PR 준비**. 1차 전체 실사 PASS 뒤 비차단 Low A-01~A-04를 보완했고 신규 Opus의 첫 재검증에서 네 건 모두 해소됐다. 필수 결함·새 비차단 발견은 0건이다. 사용자 병합 승인 전에는 병합하지 않는다. A-1 설치는 조건부 사용자 승인 전달을 받았으나 0단계 병합과 정식 A-1 goal 검토 전에는 실행하지 않는다.
- 검증 원문과 receipt 보존: `.backups/verification/2026-10-02-architecture-part-rules/`.

## 세션 관측과 재개

- 새 Run `run_a98ca1c7a511`. Task/Dispatch는 아직 만들지 않았다.
- Sol을 Astra 아래 vertical split으로 기동했다. 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, terminal `term_3d97b561-ab40-438d-8ae5-b966e42248e4`, incarnation `c9c1c96d-a651-4f22-b36d-3da17d6cec62`다. 요청 모델만 있으며 모델 화면은 아직 확인하지 못했다. backend 실제 모델도 `unknown`이다.
- 첫 화면은 Codex `0.159.3 → 0.160.0` 업데이트 선택창이다. `tui-idle`의 `satisfied=false`, `blockedReason=agent-update-prompt`를 확인했다. `sol-split.json`, `sol-ready.json`, `sol-first-screen.json`에 근거를 보존했다. 입력·업데이트·설정 변경·작업 attach는 하지 않았다.
- R-6에 따라 메인에 선택창 처리 요청 `msg_70f2d05c4151`을 보냈다. 메인 처리 후 runtime/handle/incarnation·실제 모델 화면·준비 상태를 다시 확인하고 `sol-task.txt`의 작업 계약을 최초 attach한다. timeout이나 무응답만으로 재기동·추가 작업자를 만들지 않는다.
- `.claude/settings.local.json`의 시작 hash는 `settings-before.json`에 보존했다. 메인의 CLAUDE 쓰기 종료를 받은 뒤 전체 변경을 신규 Opus에게 실사하도록 넘긴다.

### 기동 상태 변경과 메인 작성 종료

- 메인 `msg_8d241fb180ba` 수신 후 최초 Sol이 `operator_close`/orphaned로 종료된 것을 확인했다. 당시 누가 닫았는지는 이 세션의 근거로 확정하지 않는다. 다른 handle `term_2bc3cb61-382d-4d25-b5f7-849571453948`(incarnation `45adefbb-8381-4234-a41a-385b1dc5dcc5`)은 최초 split receipt와 일치하지 않고 생성 원인은 `unknown`이다.
- 메인이 명시한 정리 범위에 따라 새 대상의 빈 prompt·Context 0%·GPT-6-Astra 표시와 현 Run의 Task/Dispatch 0건을 확인해 그 pane만 닫았다. `unexpected-astra-before-close.json`, `unexpected-astra-identity.json`, `unexpected-astra-close.json`에 근거가 있고 close는 `ptyKilled=true`다. 담당 Astra는 유지했다.
- R-5의 동일 Sol 명령으로 새로 생성한 terminal은 `term_77060733-7859-4bb2-a280-09ad2d8ba476`, incarnation `7ead133f-7182-4626-a89b-20173c760e2d`다. `sol-split-2.json`과 대조했고 다시 업데이트 선택창에서 `satisfied=false`/`agent-update-prompt`를 관측했다. `msg_91caeda07852`로 메인 일회 Skip 처리를 요청했으며 아직 attach하지 않았다.
- 메인 `msg_9df5b46ba4ff`로 CLAUDE 쓰기 종료를 받았다. Architecture worktree 탭 추가와 두→세 Astra 문구의 두 줄 diff를 확인했으며 같은 commit에 포함할 수 있다. 원문은 `main-claude-write-end.json`이다. R-1 anchor는 유지한다.

### 업데이트 전 정지와 다음 진입

- 중지 원문은 `main-update-stop.json`에 보존했다. 메인의 `terminal_handle_stale` 관측과 달리 이 세션의 직후 list/show에서는 `term_77060733…`가 동일 runtime·incarnation으로 연결되어 있었다. 관측 차이의 원인은 확정하지 않는다.
- 남은 Sol의 동일성과 이 Run의 Task/Dispatch 0건, 업데이트 선택창만 떠 있는 상태를 대조한 뒤 메인의 정리 지시에 따라 해당 pane만 닫았다. `sol-2-before-update-close.json`, `sol-2-update-close.json`에 보존했으며 close는 `ptyKilled=true`다. Skip·attach·업데이트를 실행하지 않았다.
- 미커밋 보존 파일: `00_Document/operations/CURRENT.md`, 메인 작성 `CLAUDE.md`, 이 goal 폴더. 로컬 `.backups/verification/2026-10-02-architecture-part-rules/`에는 위임 spec·receipt·A-1 초안·의존성 승인안·Management 접점 원문을 보존한다. 파일을 지우거나 되돌리지 않는다.
- 다음 세션은 현재 branch `docs/architecture-part-rules-20261002`와 HEAD `333fe20211260ef230cd7d4ef9555cb4d5999c08`, 보존된 diff를 확인한다. 업데이트 완료와 사용자/메인 재개 지시 후 R-5대로 신규 Sol을 명시해 기동하고 준비·모델·새 runtime/handle/incarnation을 확인한 다음 `sol-task.txt`의 좁은 구현을 새 Run/Task/Dispatch로 발행한다. 이전 세션 식별자는 재사용하지 않는다.
- 당시 다음 절차는 Sol 쓰기 종료 후 신규 Opus가 세 운영 문서·CLAUDE·goal/CURRENT 전체를 실사하고, 이후 Astra commit/push/PR과 사용자 병합 승인 순서였다. A-1은 초안과 설치 승인안에 대한 메인/사용자 판단 및 0단계 결과를 확인한 뒤 별도 목표로 재개한다. 업데이트 전 정지 당시에는 설치 승인을 받지 않았으며, 업데이트 뒤 승인 전달은 아래 A-1 절에 기록한다.
- 업데이트 후 개발 방향으로, 메인 `msg_870cb0736cfa`가 전달한 사용자의 “4번 항목에 전체 시스템 도식표” 요청을 보존한다. 메인 해석은 운영툴 사이드바 네 번째 메뉴를 옛 Architecture visualizer의 동적 도식처럼 전체 시스템 흐름을 한눈에 보는 아키텍처 뷰어의 진입점으로 두는 것이다. A-1 계약은 클래스 그래프와 함께 M-2의 1단 7개·하위 39개 카드를 `codeReference`로 묶는 시스템 개요를 뒷받침하도록 검토한다. 카드 수는 메인 전달 계획이며 구현 실사 결과가 아니다. 뷰어 구현은 M-2 병합 뒤 frontend 소유를 조율해 시작한다. 구체 맥락은 로컬 `a1-goal-draft.md`, 전달 원문은 `main-system-overview-direction.json`에 있다. 이번에는 기록만 했으며 정지 상태와 미커밋 변경을 유지한다.

## 다음 경계

### 2026-10-02 업데이트 뒤 재개 관측

- 재개 원문은 `main-resume-20261002.json`이다. 현재 Astra는 handle `term_423a4e96-28cd-4f84-9ac8-d6d1853917fb`, incarnation `afdd8e9d-8a7d-4a94-9264-e2c822f010e7`, runtime `8a673084-6819-45b9-a551-347226cdce9b`이며 화면은 `GPT-6-Astra xhigh`다. Codex CLI `0.160.0`, backend 실제 모델은 `unknown`이다. 기존 branch/HEAD와 CURRENT·goal·메인 CLAUDE 두 줄 diff를 확인해 READY `msg_ee4809acf621`을 회신했다. 과거 식별자를 실행 권한으로 재사용하지 않았다.
- 새 Run `run_fda5df2ca5b9`를 생성했다. 신규 Sol은 Astra 아래 vertical split, terminal `term_ff127234-5ef8-4d86-99f2-38588e6285b5`, incarnation `51c7b8af-0c5b-483a-bbc2-e22fe09ee186`이다. 시작 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 화면 `GPT-6.1-Sol xhigh`, backend 실제 모델은 `unknown`이다. 첫 화면에서 업데이트·선택창 없이 빈 prompt를 확인했고 `tui-idle`은 `satisfied=true`였다.
- 최초 작업 연결은 Task `task_0b9f37a120f6`, Dispatch `ctx_4eafa266dc88`이며 `input_accepted`와 `turn_started`를 모두 확인했다. 근거는 `resume-run.json`, `resume-sol-split.json`, `resume-sol-ready.json`, `resume-sol-first-screen.json`, `resume-sol-identity.json`, `resume-sol-start.json`이다. attach receipt의 null launch 모델값과 화면 모델을 구분한다.
- GameDev에 `msg_73b3a0a54b3c`로 이 checkout의 운영 문서 쓰기 소유를 알렸다. 목표 기준 main은 중지 전 확인한 `333fe20211260ef230cd7d4ef9555cb4d5999c08`이며 재개 시 origin/main은 PR162 병합 `27f1f57`을 가리켰다. 기존 목표 브랜치를 보존하며 새로운 목표를 시작한 것으로 해석하지 않는다.
- Sol 질문 `msg_f50bd7334d0b`로 기존 `main-messages.json`이 원문 대신 `invalid_argument` 오류만 담았음을 발견했고 직접 대조했다. 메인에 `msg_f19629e7be6e`로 즉시 불일치를 보고한 뒤, 과거 수신 handle의 읽기 전용 `orchestration inbox`에서 `msg_39f5b5bcb525`를 복구했다. `recovery-inbox.json`과 `main-source-recovered.json`에 id·from_handle·body를 보존하고 기존 오류 파일도 유지했다. 작업자에게 blocking `reply`로 복구 경로를 전달했다. 이는 원문 보존 기록의 정정이며 과거 handle을 실행 권한으로 재사용한 것이 아니다.
- 메인 `msg_8df3b885d77f`가 세션 인계 스킬의 handle 공유 한 줄을 범위에 추가했고, ORCA R-6에 Architecture checkout의 기존 설정 복사 사실도 반영하도록 지시했다. `main-scope-addition.json`에 원문을 보존하고 진행 중 Sol에 동일 작업의 범위 추가로 전달했다. 설정 파일을 변경하는 권한은 아니다. PR 전 최신 main과의 충돌·범위 대조도 수행한다.
- `git ls-remote origin refs/heads/main`으로 최신 main `27f1f57374c91b723127830cf16f6613ea32ebf1`을 확인했다. 기존 기준과 최신 main의 실제 파일 차이는 readability-baseline·readability-format-ci의 goal 두 개이며 이번 수정 파일과 겹치지 않았다. 명령과 출력은 `latest-main-comparison.txt`다.
- Sol의 `worker_done` `msg_8509fbf11ddd`를 수신해 정확한 Task/Dispatch·쓰기 종료·`implementation.md`와 실제 diff를 대조했다. 자체 `git diff --check`와 승인 범위 대조는 통과했고, Python 부재로 스킬 자동 검사는 미실행이다. 제품 build/test·Unity·DB도 미실행이며 독립 실사와 구분한다. `worker-release`는 external terminal로 `retained`를 반환했고, 동일 incarnation의 완료 pane만 `terminal close`로 정리했다. 근거는 `resume-sol-done.json`, `resume-sol-release.json`, `resume-sol-before-close.json`, `resume-sol-close.json`이다. Opus 실사 중에는 모든 운영 문서·goal/CURRENT 쓰기를 멈춘다.

### 독립 실사와 보완

- 신규 Opus `claude --model claude-opus-5-5`를 Astra 아래 vertical split으로 기동했다. terminal `term_6fec29f9-2f3d-4aeb-a4da-fb35f3a7bdd4`, incarnation `53d3063c-1805-46d6-8ecf-6aee419c93eb`, 화면 `Opus 5.5 with xhigh effort`, backend `unknown`이다. 준비 true와 선택창 없는 첫 화면 확인 뒤 Task `task_c6aabc1f3c48`, Dispatch `ctx_19e0d065ed89`에 최초 연결했고 접수·턴 시작을 확인했다.
- 1차 판정 원문은 `verification/verdict.md`, 실행 대조 메모는 `verification/raw-checks.txt`다. `msg_32ba731adfcc`로 PASS·필수 결함 0·비차단 Low A-01~A-04를 받았다. Astra는 원문·실제 diff·동결 7파일 hash를 대조했고 모두 불변이었다. release는 `retained/external_terminal`, 동일 pane의 close는 `ptyKilled=true`다. 근거는 `resume-review-*.json`, `review-frozen-check.json`이다.
- A-01(RESUME 배치 상세 중복)·A-04(Architecture 배치 출처 표현)는 신규 Sol에 기존 두 문장의 보완만 맡긴다. A-02(goal 과거 handle·설치 승인 시점 혼재)·A-03(GameDev 비충돌 범위 과장)는 goal 소유자 Astra가 현재/당시를 구분하고 네 운영 문서만 비충돌임을 명시해 정정했다. 후속 신규 Opus가 이 네 발견과 보존 범위를 재검증한다.
- A-03은 Opus가 GameDev D1b `dfa9526`과 CURRENT 사본을 3-way 대조해 exit 1 충돌을 관측한 사항이다. 메인에게 `msg_62bbd7990110`로 즉시 보고했고 GameDev `msg_f15f0d712624`가 Architecture 선병합 시 D1b의 쓰기 종료·clean checkpoint 뒤 최신 main을 반영해 두 링크를 보존하기로 회신했다. 반대 순서라도 나중 PR에서 두 링크를 보존한다. 현재 origin/main과의 비충돌 확인과 미병합 다른 branch의 충돌 위험은 구분한다. 원문은 `review-source-finding.json`, `current-merge-order.json`이다.
- 보완 Sol은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`로 신규 기동했다. terminal `term_a21bbd8a-67c8-4d07-a3dd-242aedb19892`, incarnation `46378984-5e44-4b4e-886d-45a0c43ac557`, 화면 `GPT-6.1-Sol xhigh`, backend `unknown`이다. 첫 화면·준비 true 확인 뒤 Task `task_955627f98905`, Dispatch `ctx_a550884be98f` 최초 연결에서 `input_accepted`·`turn_started`를 확인했다. 근거는 `fix-sol-*.json`이다.
- 보완 Sol의 `msg_9224a1bd9f78`과 `implementation-fix.md`를 실제 두 문장에 대조했다. A-01의 중복 상세는 R-1 링크로 축소됐고 A-04의 Architecture 배치는 메인 지시로 명시됐다. `git diff --check` exit 0, 쓰기 종료를 확인했다. release `retained/external_terminal` 뒤 같은 incarnation만 close했고 `ptyKilled=true`다. A-02/A-03 goal 정정도 끝났으며 이 시점부터 운영 문서·goal/CURRENT 쓰기를 멈추고 신규 Opus의 재검증을 받는다.
- 신규 재검증자는 `claude --model claude-opus-5-5`로 vertical split에 기동했다. terminal `term_948a205c-71da-4456-8bd7-969d2d783816`, incarnation `df673f1d-69b5-4b41-8e95-59b5e2774de4`, 화면 `Opus 5.5 with xhigh effort`, backend `unknown`이다. 준비와 첫 화면을 확인한 뒤 Task `task_11b5f6ab888e`, Dispatch `ctx_fa3104b43e68` 최초 연결의 접수·턴 시작을 확인했다. `msg_9268e4018ea4`와 `verification-2/verdict.md`는 A-01~A-04 모두 해소·필수 결함 0·새 비차단 발견 0의 PASS다. 전체 실사를 반복한 판정이 아니라 네 보완과 영향·보존의 재검증이다.
- Astra는 재검증 원문을 직접 읽고 수정 문장·원천·동결 7파일 hash 불변을 대조했다(`recheck-frozen-check.json`). `git diff --check` exit 0과 미실행 범위를 확인했다. release `retained/external_terminal` 뒤 같은 pane만 close해 `ptyKilled=true`, 현재 Run의 reclaimable 0건을 확인했다. 근거는 `recheck-*.json`이다. 선택 관찰 R-O2에 따라 Astra→메인 위험 보고 `msg_62bbd7990110` 원문도 `current-risk-main-notice.json`에 추가 보존했다. 제품 build/test·Unity·DB·스킬 자동 검사·독립적인 실제 배치 재현은 미실행이다. 검증 종료 뒤 이 결과·Git 상태 기록만 Astra가 갱신한다.

### A-1 별도 목표

업데이트 뒤 메인 `msg_0eca5c1e009c`가 사용자 원문 “CodeGraph 승인”을 전달했다. 승인 범위는 `a1-dependency-approval.md`의 `@colbymchenry/codegraph@1.6.1` 및 같은 버전 Linux x64 번들의 `99_Tools/Architecture/CodeGraph` 한정 설치, 승인된 npm 옵션·프로세스 한정 실행 설정과 SDK 동봉 Roslyn이다. 0단계 PR 병합 뒤 최신 main의 A-1 정식 branch와 메인 검토를 받은 goal에서 신규 Sol이 실행해야 한다. 전역 설치·설정 변경·MCP 연결·상주 실행·다른 버전·추가 의존성은 허용하지 않는다. 이 승인 전달과 GameDev 회신 `msg_7bf483f3aa4a`는 `resume-followups.json`에 보존했다. GameDev가 변경하지 않는다고 한 파일은 AGENTS·ORCA·RESUME·CLAUDE 네 개이며, CURRENT는 양쪽에서 새 goal 링크를 추가하므로 병합 충돌 가능성이 있다. 아래 승인 요청 이력은 승인 전달 전 관측이다.

A-1은 CodeGraph와 Roslyn을 독립 수기 정답표 10~15개 관계로 비교하는 별도 목표다. 서버·Shared·ClientNet과 Unity 클라이언트를 나눠 정밀도·재현율·실행 비용을 측정하고 버전·commit SHA가 있는 스냅샷 JSON 계약을 작성한다. 도구 선택은 결과 후 사용자 결정이며 CodeGraph 설치와 새 Roslyn 패키지는 메인을 통한 사용자 승인 전 수행하지 않는다. Management M-2와 코드 경로 계약만 직접 조율하며 화면 구현은 포함하지 않는다. 별도 goal 초안에서 범위와 실행 전제를 확정한다.

로컬 `a1-goal-draft.md`를 준비하고 메인에 경로를 보냈다. 이 초안은 0단계 PR 대상이 아니다. CodeGraph는 같은 이름의 npm 배포물이 여러 개라 정확한 package 식별을 메인에 요청했다. Management Astra의 `msg_a94f4d24fd2a`와 회신 `msg_56dfb357c1ef`로 `codeReference:{commitSha,mappings:[{path,kind,namespace?,role?}]}` 접점에 동의했다. 같은 SHA에서 repo-relative 경로를 연결하며 미구현 카드는 빈 매핑과 사유를 사용한다. 구체 조건·보류 사항은 다음 goal 초안에 있고 접점 합의를 구현·검증 완료로 해석하지 않는다.

후속 `msg_16bc081dfb3a`로 Git tree의 정확한 경로 대소문자를 보존하고 OS별 소문자화를 하지 않는 조건까지 양측 합의했다. 원문은 `m2-path-agreement.json`에 보존했다.

메인이 `@colbymchenry/codegraph`를 식별했다. `a1-dependency-approval.md`에 CodeGraph `1.6.1` 프로젝트 한정 설치와 SDK 동봉 Roslyn(새 NuGet 없음) 비교안을 준비해 `msg_34819df4a1a6`으로 사용자 승인 요청을 전달했다. 패키지 메타데이터와 파일 목록만 조회했으며 설치·새 package/lockfile 추가는 미실행이다.
