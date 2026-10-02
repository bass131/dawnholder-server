# 다음 세션 재개

이 문서는 읽는 순서와 재개 절차다. 현재 상태·결정·결과는 [CURRENT](CURRENT.md)가 가리키는 goal을 정본으로 삼는다. [기록 정정](../../01_Phases/goals/2026-10-01-refactor-record-corrections/goal.md)은 PR150으로 병합됐다. [P1a 종료 기록](../../01_Phases/goals/2026-09-30-party-invite-command/goal.md)과 [로드맵](../../01_Phases/milestones/2026-09-30-contracts-persistence/roadmap.md)은 이전 완료와 남은 의존성을 제공한다. 라우팅 시범은 성공했고 계층형 라우팅은 사용자 결정으로 전역 채택됐다. PR·규칙 문서 반영 상태는 goal에서 확인하며 P1b·DB·게임 정책 구현을 자동 시작하지 않는다.

## 최소 읽기

1. 루트 [AGENTS](../../AGENTS.md)와 [CURRENT](CURRENT.md)의 goal을 읽고 완료·보류·미실행을 구분한다.
2. 실제 다단계 작업을 재개할 때만 [goal-loop](../../.agents/skills/dawnholder-goal-loop/SKILL.md)와 관련 마일스톤 지침을 읽는다.
3. 선택한 다음 조각의 [P0 계약](../../01_Phases/goals/2026-09-30-contracts-baseline/contracts.md) 행과 [기능 지도](../FEATURE_MAP.md) 진입점만 확인한다. 전체 과거 대화·소스·로그를 수집하지 않는다.
4. 실행 전에 [DEVELOPMENT](DEVELOPMENT.md)의 Unity·WSL·DLL 복사·포트 소유 조건을 확인한다. 이전 테스트 성공을 새 변경의 실행 결과로 재사용하지 않는다.

## Git와 문서 보존

GameDev 작업 경로는 `C:/Dev/DawnHolder_Project`다. `git status --short`, 현재 branch/HEAD, 원격 main과의 차이를 먼저 확인한다. 현재 branch·기준 commit은 CURRENT의 goal, 이전 준비 branch는 P1a 종료 절에 있다. branch 이름만 보고 다음 구현이 시작됐다고 판단하지 않는다.

2026-09-30 맥락 문서 checkpoint `b3cf78a`는 기존 준비 branch에 보존됐으며, 2026-10-01 정정 branch에 `147ef1c`로 가져왔다. 이후 push·PR·병합 상태는 정정 goal에서 확인한다. 기존 변경이나 이 문서 commit을 버리거나 무조건 main으로 전환하지 않는다. 새 checkout에 이 문서가 없다면 기존 작업 경로의 [로컬 인계](../../.backups/handoffs/2026-09-30-game-dev-next-session.md)와 checkpoint를 확인한다. `CLAUDE.md`는 PR149로 main에 병합됐고 소유자는 Claude 메인이다. 상세 출처는 정정 goal에 있다. PR 병합에는 해당 PR에 대한 사용자 명시 승인이 필요하다.

## 세션 진입 배치

현재 운영 규칙의 정본은 [ORCA R-1~R-8](ORCA.md#2026-10-01-운영-규칙-정본)이다. 이전 시범 goal은 당시 결과의 근거로만 읽고 현재 배치·기동 절차는 아래 정본 링크를 따른다.

1. 새 Astra(GameDev·Management·Architecture)의 배치는 [R-1](ORCA.md#r1-management-placement), 작업자 기동은 [R-5](ORCA.md#r5-worker-launch), 첫 화면 확인은 [R-6](ORCA.md#r6-first-screen)을 따른다. 이전 handle/Run/Task/Dispatch를 재사용하지 않는다.
2. Astra의 목표 종료·재진입·중간 재개·인계 기록은 [R-8](ORCA.md#r8-astra-lifecycle)을 따른다.
3. 메인은 시작 시 GameDev·Management·Architecture 세 Astra에 자기 handle을 알린다. 모든 세션 간 메시지 subject/body와 입력은 자기 발신 태그를 붙이고 `from_handle`과 대조한다. 회신 subject는 [R-3](ORCA.md#r3-reply-tag)을 따른다. 타 세션 터미널에는 태그와 Orca 메시지 확인 안내만 넣으며, 지시는 orchestration으로 전달한다. 표식 없는 터미널 입력만 사용자 직접 지시다.
4. 작업자·검증자 세션은 작업 하나 후 정산·종료하고 재사용하지 않는다. 정상 완료는 `worker_done`·Astra 대조 후, 비정상 종료·막힘·무응답은 진단 기록 후 정산·종료한다. 수정·재검증에는 새 세션을 연다.

## 다음 조각을 시작하는 순서

- 먼저 [계층형 모델 라우팅 시범과 코드 기준 재정립](../../01_Phases/goals/2026-10-01-hierarchical-routing-pilot/goal.md)의 구현·검증 결과와 PR 상태를 읽는다. 대상 4곳은 동작 보존 정리로 판정됐다. 계층형 라우팅 채택은 확정이고 C(작은 작업 예외 포함)는 미확정이다. 현재 Management 배치는 [R-1](ORCA.md#r1-management-placement)을 따른다.
- [후속 후보와 판단 근거](../../01_Phases/goals/2026-10-01-refactor-record-corrections/open-items.md)의 메뉴 연결 probe·RegisterSend·HUD·UnityClientSession 후보는 유지한다. 메뉴 작업은 `MainMenuController`와 `ConnectionProbe`의 입력 캡처·요청 수명·실패 정리·늦은 callback 및 기존 fixture부터 설계하며 후보를 구현 완료나 확정된 UX로 해석하지 않는다.
- source 교체·화면 종료 뒤 표시·재시도 같은 정책이 달라져야 하면 관찰 결과와 선택지를 사용자에게 올린다. 이후 표시 전용 HUD의 source binding을 별도 작은 조각으로 다룬다.
- 범위·보존 계약·완료조건·파일 소유를 goal에 명시하고 최신 사용자 결정과 규칙 문서 반영 상태를 함께 확인한다. 실제 모델 확인 불가는 `unknown`으로 기록한다.
- DB 상세 설계는 P1과 독립적으로 준비할 수 있다. [D0](../../01_Phases/goals/2026-09-29-persistence-design/design.md)를 다시 결정하지 말고 schema/transaction/실패·복구 기술 계약을 구체화한다. SQL schema 접근 가능과 GameServer 저장·복원 연동 완료를 구분한다.

## Management와의 경계

운영툴 문서는 Management 메인이 `05_Management`에서 관리한다. 기존 별도 worktree는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`이며 해당 작업 공간의 README/재개 문서를 확인한다. root의 05 사본이 상대 세션의 로컬 문서 갱신까지 포함한다고 가정하지 않는다.

첫 개발기록 공동 조회는 파티 UI·DB 구현에 의존하지 않는다. 실제 서버 등록·로그 조회 전에 GameDev의 `02_Server`/실행 wrapper와 Management의 `05` 등록·조회·보존 계약을 조율한다. 합의 범위와 현재 한계는 P1a goal의 종료 절과 상대 세션의 정식 goal을 따른다. 조회 성공을 서버 제어·DB 저장·로그 삭제 권한으로 확대하지 않는다.

Orca 연결은 현재 runtime과 정확한 worktree/handle/incarnation을 다시 확인한다. 이전 인계의 ID는 당시 관측값이며 실행 권한이 아니다. 새 세션이 필요하면 [session-handoff](../../.agents/skills/dawnholder-session-handoff/SKILL.md)를 사용하되 세션 생성·종료는 사용자 범위 안에서만 한다. 기존 Run/Task/Dispatch를 재사용하지 않는다.

## 새 세션 시작 문장

> AGENTS와 CURRENT → RESUME를 읽고 실제 Git 상태와 완료 goal을 확인해 주세요. 기존 로컬 문서 변경을 보존하고, 필요한 짧은 문맥만 읽은 뒤 현재 상태와 다음 조각을 요약해 주세요. 후속 구현은 이번 세션의 사용자 재개 지시에 따라 설계부터 진행하고, 미합의 게임 정책은 먼저 논의해 주세요.
