# Management 다음 세션 진입

정본 worktree는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`, 소유 영역은 `05_Management`다. 과거 root05나 management-foundation을 정본으로 사용하지 않는다. 이 문서는 읽기 순서와 다음 행동만 제공하며 상태를 별도 집계하지 않는다.

이 Management 터미널은 Game Dev 화면에서 분할되어 Orca에는 Game Dev 소유로 표시된다. 실제 파일·Git 작업은 위 `management-active`에서만 수행하며 `C:/Dev/DawnHolder_Project`를 사용하지 않는다. `[메인 Claude]`로 시작하는 터미널 입력은 메인의 알림이고 실제 지시는 Orca 메시지에서 확인한다.

## 최소 읽기 순서

1. [현재 세션 종료 정리 goal](goals/2026-10-01-session-closeout/goal.md). PR151 종료 근거는 [문맥 정정 goal](goals/2026-10-01-context-corrections/goal.md), PR147 병합과 후속 구현 보류·당시 검증은 [기존 goal](goals/2026-09-30-system-records/goal.md#현재-상태)에서 확인한다.
2. [공동 조회·로그 합의](goals/2026-09-30-system-records/shared-read-agreements.md): 사용자와 정한 순서, 첫 구현 경계, 후속 로그 계약과 남은 결정.
3. 실제 재개할 때만 [AGENTS](../AGENTS.md), [목표 루프](../.agents/skills/dawnholder-goal-loop/SKILL.md), [실행 안내](../00_Document/operations/DEVELOPMENT.md)의 영향 범위를 읽는다. 과거 전체 소스·로그·대화를 모으지 않는다.

## 재개 첫 행동

사용자의 작업 재개 지시 여부를 먼저 확인한다. 이어 `git status --short --branch`와 `git log -1 --format="%H %s" -- 05_Management`로 실제 로컬 변경·문서 커밋을 확인하고 위 goal의 관찰과 비교한다. 맥락 갱신만으로 후속 보류가 해제되지는 않는다.

과거 문서 checkpoint `8c6fbd57` 이식과 문맥 정정은 PR151 병합으로 끝났다. [당시 종료 기록](goals/2026-09-30-system-records/goal.md#오늘-작업-종료다음-세션-재개)의 이식 절차를 다시 실행하지 않는다. MCP·서버 등 후속 구현은 계속 보류하며, 별도 재개 지시와 goal 범위가 정해지면 실제 최신 main과 로컬 변경을 확인해 보존한다.

## 소유권과 다음 결정

Game Dev의 root CURRENT/goal·게임 코드·실행 wrapper는 해당 메인 소유다. Management는 05의 기록 조회와 후속 서버 등록·조회·보존을 담당한다. 첫 공동 조회 이후에는 Game Dev와 로그의 실행 식별·시각/수준·cursor·보존 계약을 맞춘다. [합의 문서의 다음 결정](goals/2026-09-30-system-records/shared-read-agreements.md#다음-결정)을 기준으로 이미 정한 질문을 반복하지 않는다.

계층형 라우팅은 합의됐지만 Management 적용은 Game Dev 시범 성공 후다. 그전에는 기존 [AGENTS](../AGENTS.md)의 구현 `gpt-6.1-sol`, 메인·독립 TestCode/검증·보고 `gpt-6-astra` 라우팅을 유지하고 실제 runtime 모델 확인 불가는 `unknown`으로 기록한다. `--no-daemon`은 더 이상 필수가 아니다. 같은 파일 동시 쓰기 금지, 후속 PR마다 개별 명시 병합 승인을 유지한다. 작업 종료는 터미널/프로세스 종료·정리 권한을 뜻하지 않는다.
