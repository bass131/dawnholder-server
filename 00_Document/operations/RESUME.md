# 다음 세션 재개

이 문서는 읽는 순서와 재개 절차다. 현재 상태·결정·결과는 [CURRENT](CURRENT.md)가 가리키는 goal을 정본으로 삼는다.

## 최소 읽기

1. 루트 [AGENTS](../../AGENTS.md)와 [CURRENT](CURRENT.md)의 goal을 읽고 완료·보류·미실행을 구분한다.
2. 실제 다단계 작업을 재개할 때만 [goal-loop](../../.agents/skills/dawnholder-goal-loop/SKILL.md)와 관련 마일스톤 지침을 읽는다.
3. 선택한 다음 조각의 [P0 계약](../../01_Phases/goals/2026-09-30-contracts-baseline/contracts.md) 행과 [기능 지도](../FEATURE_MAP.md) 진입점만 확인한다. 전체 과거 대화·소스·로그를 수집하지 않는다.
4. 실행 전에 [DEVELOPMENT](DEVELOPMENT.md)의 Unity·WSL·DLL 복사·포트 소유 조건을 확인한다. 이전 테스트 성공을 새 변경의 실행 결과로 재사용하지 않는다.

## Git와 문서 보존

Core 작업 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-active`다. 기존 명칭과 세션·계약의 태그 전환은 [AGENTS Core 전환 정본](../../AGENTS.md#core-tag-transition)을 따른다. `git status --short`, 현재 branch/HEAD, 원격 main과의 차이를 먼저 확인한다. 현재 branch·기준 commit은 CURRENT의 goal, 이전 준비 branch는 P1a 종료 절에 있다. branch 이름만 보고 다음 구현이 시작됐다고 판단하지 않는다. 기존 변경을 버리거나 무조건 main으로 전환하지 않는다.

## 세션 진입 배치

현재 운영 규칙의 정본은 [ORCA R-1~R-8](ORCA.md#2026-10-01-운영-규칙-정본)이다. 이전 시범 goal은 당시 결과의 근거로만 읽고 현재 배치·기동 절차는 아래 정본 링크를 따른다.

1. 새 리드의 배치는 [R-1의 현재 리드 배치](ORCA.md#r1-management-placement), 작업자 기동은 [R-5](ORCA.md#r5-worker-launch), 첫 화면 확인은 [R-6](ORCA.md#r6-first-screen)을 따른다. 이전 handle/Run/Task/Dispatch를 재사용하지 않는다.
   현재 리드와 승인된 목표 한정 추가 파트의 배치·종료 권한은 R-1 정본에서 확인한다. 현재 goal의 종료조건도 확인하며 이 문서에 상세 규칙을 복제하지 않는다.
2. 새 메인은 [R-1의 메인 전용 checkout](ORCA.md#r1-management-placement)에서 열고 리드를 연 뒤 작업 현황 탭을 다시 띄운다. 현재는 Claude Code 쪽의 저장소 밖 개인 도구 `C:/Dev/DawnHolder_Dashboard`로 운영한다.

   ```powershell
   orca terminal create --worktree path:C:/Dev/DawnHolder_Project --title "작업 현황" --command "node C:/Dev/DawnHolder_Dashboard/dashboard.mjs"
   ```

   탭을 띄운 뒤 `C:/Dev/DawnHolder_Dashboard/board.json`의 결정 항목이 현재 상태와 맞도록 메인이 갱신한다. 세부 결정 운영 규칙은 [CLAUDE 「메인의 기록과 알림」](../../CLAUDE.md#메인의-기록과-알림)을 따른다.
3. 리드의 목표 종료·재진입·중간 재개·인계 기록은 [R-8](ORCA.md#r8-astra-lifecycle)을 따른다.
4. 메인은 시작 시 [R-1의 현재 리드](ORCA.md#r1-management-placement)에 자기 handle을 알린다. 모든 세션 간 메시지 subject/body와 입력은 자기 발신 태그를 붙이고 `from_handle`과 대조한다. 회신 subject는 [R-3](ORCA.md#r3-reply-tag)을 따른다. 타 세션 터미널에는 태그와 Orca 메시지 확인 안내만 넣으며, 지시는 orchestration으로 전달한다. 표식 없는 터미널 입력만 사용자 직접 지시다.
5. 작업자·검증자 세션은 작업 하나 후 정산·종료하고 재사용하지 않는다. 정상 완료는 `worker_done`·리드 대조 후, 비정상 종료·막힘·무응답은 진단 기록 후 정산·종료한다. 수정·재검증에는 새 세션을 연다.

## 다음 조각을 시작하는 순서

- [CURRENT](CURRENT.md)의 해당 파트 worktree·branch를 확인하고 그 goal의 「재개 지점」을 따른다. 상태와 다음 작업을 이 문서에 복제하지 않는다. 아직 병합되지 않은 다른 파트 goal은 해당 worktree에서 읽는다.
- goal 종료 뒤에는 다음 goal을 자동으로 시작하지 않는다. 메인과 사용자가 결과·남은 위험·BACKLOG·마일스톤의 다음 순서를 점검한 뒤 다음 계획을 정하고 재개한다.
- 정본 규칙에 아직 반영되지 않았지만 적용 중인 결정은 [Rules goal의 적용 결정](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#적용-중인-사용자-결정)을 확인한다. 사용자 결정과 메인 결정을 구분하고, 운영 규칙 PR 병합 뒤에는 그 절이 가리키는 정본을 따른다.

## Management와의 경계

운영툴 문서는 Management 메인이 `05_Management`에서 관리한다. 기존 별도 worktree는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`이며 해당 작업 공간의 README/재개 문서를 확인한다. root의 05 사본이 상대 세션의 로컬 문서 갱신까지 포함한다고 가정하지 않는다.

첫 개발기록 공동 조회는 파티 UI·DB 구현에 의존하지 않는다. 실제 서버 등록·로그 조회 전에 Core의 `02_Server`/실행 wrapper와 Management의 `05` 등록·조회·보존 계약을 조율한다. 합의 범위와 현재 한계는 P1a goal의 종료 절과 상대 세션의 정식 goal을 따른다. 조회 성공을 서버 제어·DB 저장·로그 삭제 권한으로 확대하지 않는다.

Orca 연결은 현재 runtime과 정확한 worktree/handle/incarnation을 다시 확인한다. 이전 인계의 ID는 당시 관측값이며 실행 권한이 아니다. 새 세션이 필요하면 [session-handoff](../../.agents/skills/dawnholder-session-handoff/SKILL.md)를 사용하되 세션 생성·종료는 사용자 범위 안에서만 한다. 기존 Run/Task/Dispatch를 재사용하지 않는다.

## 새 세션 시작 문장

> AGENTS와 CURRENT → RESUME를 읽고 실제 Git 상태와 완료 goal을 확인해 주세요. 기존 로컬 문서 변경을 보존하고, 필요한 짧은 문맥만 읽은 뒤 현재 상태와 다음 조각을 요약해 주세요. 후속 구현은 이번 세션의 사용자 재개 지시에 따라 설계부터 진행하고, 미합의 게임 정책은 먼저 논의해 주세요.
