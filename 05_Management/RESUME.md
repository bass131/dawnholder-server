# Management 다음 세션 진입

정본 worktree는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`, 소유 영역은 `05_Management`다. 과거 root05나 management-foundation을 정본으로 사용하지 않는다. 이 문서는 읽기 순서와 다음 행동만 제공하며 상태를 별도 집계하지 않는다.

## 최소 읽기 순서

1. [기존 goal의 현재 상태·종료/재개 절](goals/2026-09-30-system-records/goal.md#현재-상태): 병합·보류, 로컬 branch와 문서 보존, 검증/미실행의 정본.
2. [공동 조회·로그 합의](goals/2026-09-30-system-records/shared-read-agreements.md): 사용자와 정한 순서, 첫 구현 경계, 후속 로그 계약과 남은 결정.
3. 실제 재개할 때만 [AGENTS](../AGENTS.md), [목표 루프](../.agents/skills/dawnholder-goal-loop/SKILL.md), [실행 안내](../00_Document/operations/DEVELOPMENT.md)의 영향 범위를 읽는다. 과거 전체 소스·로그·대화를 모으지 않는다.

## 재개 첫 행동

사용자의 작업 재개 지시 여부를 먼저 확인한다. 이어 `git status --short --branch`와 `git log -1 --format="%H %s" -- 05_Management`로 실제 로컬 변경·문서 커밋을 확인하고 위 goal의 관찰과 비교한다. 맥락 갱신만으로 후속 보류가 해제되지는 않는다.

재개 지시가 있으면 로컬 문서 커밋·미커밋 변경을 보존한 뒤 최신 main을 fetch한다. 이 맥락 정리 커밋은 원격에 push하지 않았으므로 원격만 checkout하면 재개 문서가 빠질 수 있다. 현재 branch를 그대로 보관하고 최신 main 기반 새 branch를 만든 뒤 필요한 문서 정리 커밋을 이식(cherry-pick)하는 방법을 우선 검토한다. dirty 상태는 먼저 별도 보존하고, 충돌은 실제 최신 상태와 대조한다. reset/삭제로 정리하지 않는다. 그 뒤 합의된 공동 조회 범위를 새 goal에 정식화한다. 오늘은 이 전환을 실행하지 않는다.

## 소유권과 다음 결정

Game Dev의 root CURRENT/goal·게임 코드·실행 wrapper는 해당 메인 소유다. Management는 05의 기록 조회와 후속 서버 등록·조회·보존을 담당한다. 첫 공동 조회 이후에는 Game Dev와 로그의 실행 식별·시각/수준·cursor·보존 계약을 맞춘다. [합의 문서의 다음 결정](goals/2026-09-30-system-records/shared-read-agreements.md#다음-결정)을 기준으로 이미 정한 질문을 반복하지 않는다.

실제 구현은 gpt-6.1-sol, 메인·독립 TestCode/검증·보고는 gpt-6-astra로 분리하고 실제 runtime 모델 확인 불가는 unknown으로 기록한다. 새 CLI는 --no-daemon, 같은 파일 동시 쓰기 금지, 후속 PR마다 개별 명시 병합 승인을 유지한다. 작업 종료는 터미널/프로세스 종료·정리 권한을 뜻하지 않는다.
