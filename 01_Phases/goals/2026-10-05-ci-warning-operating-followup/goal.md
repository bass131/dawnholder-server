# CI npm 경고와 운영 후속 정본화

## 재개 지점

Rules의 새 목표다. 메인 `msg_251c879ef46a`(2026-10-05T11:56:05Z)가 한 goal·본 PR 두 개의 범위를 확인했다. 최신 `origin/main` `cf9f69571e6c5833d79c56f5467cc55c62b0859c`에서 `ci/npm-engine-warning-20261005`를 만들었다. 현재는 PR1 계약 준비이며 구현·독립 검증·CI·PR·병합은 아직 수행하지 않았다. 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- 근거 폴더 E: `.backups/verification/2026-10-05-ci-warning-operating-followup/`(Git 제외).
- Astra 사전 메모: [astra-context.md](../../../.backups/verification/2026-10-05-ci-warning-operating-followup/astra-context.md). 메인 범위 원문: [main-scope-confirmation.json](../../../.backups/verification/2026-10-05-ci-warning-operating-followup/main-scope-confirmation.json).
- 현재 Run `run_993867656376`, 회신 주소 `run:run_993867656376`. 이전 목표의 Run·Task·Dispatch는 재사용하지 않는다.

## 진척 단계

- [x] 범위 확인과 최신 main 기준 확정
- [>] 새 목표·소유 경계·PR1 계약 준비
- [ ] PR1 npm 경고 표시 구현
- [ ] PR1 독립 강 검증과 실제 CI 확인
- [ ] PR1 사용자 승인과 병합
- [ ] PR2 운영 문서·Core 명칭 정비
- [ ] 메인 CLAUDE.md 반영과 독립 문서 실사
- [ ] PR2 사용자 승인과 병합
- [ ] 결과 기록과 Gardener
- [ ] 메인 종료 점검과 R-8 인계

## 범위

### 만들 것

1. 기존 code-rules CI의 npm EBADENGINE을 warning annotation과 job summary에 표시한다. npm/node 버전·설치 종료값·원시 로그를 남긴다. 경고 자체는 실패시키지 않고 기존 설치·checker·독립 회귀 실패는 보존한다.
2. 목표 루프의 정확한 `## 진척 단계`, 완료 `[x]`·진행 `[>]` 최대 하나·남음 `[ ]`, Astra 갱신 책임과 새 goal 착수 시 CURRENT 자기 줄/진척 갱신을 정본화한다.
3. R-5에 필요한 세션만 Unity MCP opt-in, 메인에게 시트 요청·보유 표시 후 기동하는 절차와 메인이 전달한 연결 재승인 관찰을 반영한다.
4. BACKLOG의 npm 경고 후보 승격·작업 현황 후보 폐기와 승인된 네 기록 대상만 원천/현재 상태를 대조해 정리한다.
5. GameDev의 현행 명칭과 신규 태그를 Core로 정비한다. 전환 시점과 기존 세션의 태그 보존은 아래 메인 결정을 따른다.

### 건드릴 곳

- PR1 제품: `.github/workflows/code-rules.yml` 한 파일. 독립 harness·fixture·판정은 E의 지정 하위 폴더.
- PR2: `AGENTS.md`, `00_Document/operations/{ORCA,RESUME,CURRENT,BACKLOG}.md`, `.agents/skills/dawnholder-goal-loop/SKILL.md`, `.agents/skills/dawnholder-session-handoff/SKILL.md`, `DEVELOPMENT.md`의 현행 담당명 한 곳.
- `CLAUDE.md`는 메인만 작성한다. Rules는 메인 쓰기 종료 뒤 같은 PR에 통합하고 신규 Opus가 함께 검토한다.
- 목표·계약·근거 기록과 CURRENT의 Rules 자기 줄은 담당 Astra 소유다. PR별 제품·테스트 쓰기는 별도 외부 세션에 맡긴다.

### 하지 않을 것

- `architecture-tests.yml` 및 다른 workflow, Management engines/lockfile·설치 정책, 전역 설정·Unity 실행/시트/relay 조작, 게임·DB·DLL·프로토콜 변경.
- 대시보드 코드와 worktree 실제 경로 변경. `C:/Dev/DawnHolder_Project`와 Architecture 태그를 보존한다.
- 과거 goal·인용·메시지 원문 일괄 치환, 현재 영속화 통합 계약의 태그 중간 변경.
- Gardener의 새 helper·드리프트 검사·자기 개선 도구 구현, workflow lint 신규 도입, warning의 error 승격 또는 engines 경고 해소.

### 관찰 가능한 완료조건

1. PR1의 실제 workflow 진입을 독립 실행한다. 경고 포함/미포함·npm 실패에 따른 출력/종료값과 버전을 원시로 대조하며 실제 PR CI의 summary/annotation·artifact·기존 검사를 확인한다.
2. PR1은 신규 Opus 강 검증(실사·독립 테스트·실제 경로)을 통과한다. 환경 대역과 실제 GitHub 실행, 대상 0건/N/A, 게임/DB/Unity 미실행을 구분한다.
3. PR2는 신규 Opus가 내용·링크·권한·현행 명칭·출처와 새 goal/Unity opt-in/태그 전환의 현실적인 문서 시나리오를 대조한다. 메인 작성 CLAUDE.md도 diff 실사 대상이다.
4. 두 PR은 각각 정확 head의 검증·CI 뒤 사용자 개별 병합 승인을 받는다. 전체 결과 기록·Gardener·메인 종료 점검 뒤 R-8이며 다음 goal을 자동 시작하지 않는다.

## PR 경계와 검증

| PR | 변경 경계 | 검증·중간 점검 |
|---|---|---|
| 1 - npm engines 경고 노출 | code-rules 제품 한 파일 + 이 goal/CURRENT 연결 기록 | 강: 설치·실행/I/O 도구. 실제 diff의 파일/줄 수는 작성 종료 때 원시 numstat로 기록. 메인 R-7 비대상 확인. 사용자 병합 승인 뒤 PR2 진행 |
| 2 - 운영 후속 결정과 Core 명칭 정본화 | 확정된 현행 운영 문서, 메인 CLAUDE 작성분, 같은 goal 결과 | 문서 실사·realistic task. 과거 원문 보존, CLAUDE 동시 쓰기 금지. 전체 종료 점검은 이 PR/결과와 Gardener 뒤 |

설계 선택: 기존 npm 로그/캐시/업로드 구조 안에서 경고 표시를 소유한다. 별도 범용 parser·설치 wrapper 대안은 제품 파일/유지 책임을 늘리므로 현재 범위에서 채택하지 않는다. npm 오류 분류나 설치 정책 변경이 필요하면 메인에게 발행 전 판단을 요청한다. 문서 변경은 합의된 운영 주체·권한을 설명하므로 문서 층에 반영하며 기술적 준수/미래 자동 선택을 보장하지 않는다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 이번 착수는 메인 `msg_251c879ef46a`가 초안 `msg_8507a54ee050`을 확인한 범위다. 메인이 재전달한 두 사용자 결정 원문은 다음과 같다.

> 「Rules·CodeMap 종료 뒤 다음 작업을 병렬로 → A 두 파트 병렬 착수」

> 「파트 이름 - GameDev의 새 이름 (적용은 다음 계획의 Rules 목표에서) → A Core」

이전 목표의 [다음 goal 사전 결정](../2026-10-05-operating-canon/goal.md#다음-goal-사전-결정)에는 메인 `msg_a1fe33623cbe`(09:36:42Z)의 원문과 작업 현황 후보 폐기 결정을 보존했다. [PR183 제출 뒤 적용 결정](../2026-10-05-operating-canon/goal.md#pr183-제출-뒤-적용한-사용자-결정)의 `msg_1f0a928c2d90`는 Unity opt-in과 PR 자동+goal 체크리스트 선택의 원천이다. 신규 진입 `msg_7389741195ba`(11:51:38Z)는 현황판의 각 worktree CURRENT 자기 줄/진척 읽기 변경, 시트 활성화 후 Unity의 Edit > Project Settings > AI > Unity MCP 재승인 관찰을 전달했다. 이는 메인의 관찰이며 이번 Rules의 Unity 실행 실증이 아니다. 새 연결마다 재승인을 물을 수 있다는 범위로 기록한다.

메인 `msg_251c879ef46a`의 **운영 결정**:

- PR2 병합 뒤 새로 여는 세션과 새 계약은 `[Core Astra]`·`[Core Sol]`·`[Core 검증자]`를 사용한다. PR2 이전에 연 GameDev 세션과 진행 계약은 그 세션이 끝날 때까지 `[GameDev …]`를 유지한다. 전환기 수신자는 두 태그를 같은 파트로 인정하되 현재 from_handle·Task·Dispatch 대조를 계속한다.
- 진행 중 영속화 통합 goal을 중간 변경하지 않으며 그 goal의 R-8로 새 Astra를 열 때부터 Core 태그를 쓴다.
- PR2 문서가 확정되면 Rules가 「CLAUDE.md 반영 요청」을 보낸다. 메인이 rules-active의 CLAUDE.md만 쓴 뒤 쓰기 종료를 알려야 Rules가 통합한다. Sol·검증자는 이 파일을 쓰지 않는다.
- 대시보드는 이미 Core/GameDev를 같은 Core 파트로 읽는다는 메인 설명이며 코드·실행 검증은 이번 범위 밖이다.

현재 목표 우선/범위 초과를 다음 계획으로 넘기는 사용자 지시는 이전 목표의 [다음 계획 후보](../2026-10-05-operating-canon/goal.md#다음-계획-후보)에 있다. 이미 goal-loop의 기준과 상태에 대응 문구가 있어 중복 정본을 만들지 않고 차이만 대조한다.

## 소유 조율과 다음 후보

- CodeMap에 `msg_ce97ba45575a`로 code-rules/architecture-tests 경계를 보냈고 `msg_7d12081ffe82`(12:01:11Z)가 동의했다. CodeMap은 architecture-tests.yml만 제품 수정하며 다른 workflow/기존 테스트·engines/lockfile을 보존한다. 회신 주소는 `run:run_3abaa3ef999c`다.
- Management에 `msg_93d86c093c94`로 engines/lockfile/npm argv 보존 경계를 알렸고 `msg_91bbcdbf4b17`(12:01:03Z)이 bytes·argv·실패 정책 보존 조건에서 추가 충돌 없음을 확인했다. 과거 CI 수치는 이번 실적이 아니다. PR179 병합은 gh 읽기 조회로 확인했다.
- BACKLOG 정리 대상은 `work-status-view`, `npm-engine-warning`, 중복 제외 완료 작업, Architecture 기능 테스트 CI 연결, `verification-depth-policy`의 등록 이유, `representative-platform-fixtures`의 근거다. 정확 ID·원천·현재 소유 goal을 대조한 뒤 최소 처분한다.
- `goal-loop-improvement`의 넓은 자기 개선 범위는 현재 진척 형식과 중복만 대조한다. 기존 `goal-state-drift`, `contract-context-check`, `powershell-all-evidence`와 이전 Gardener 후보는 원천 링크를 보존하고 새 도구를 채택하지 않는다.

## 현재 결과

2026-10-05: 메인 범위 확인, 최신 main·PR185/PR179 병합 읽기 대조, 새 branch와 Run 생성, 사전 맥락 작성 완료. 구현 Task와 독립 판정은 아직 없다. PR별 승인 요청은 완성된 diff·판정·CI를 준비한 뒤 메인에게 보낸다.
