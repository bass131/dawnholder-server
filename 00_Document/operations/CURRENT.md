# 현재 목표

상태·결정·남은 일은 각 goal의 재개 안내에서 확인한다. 아직 병합되지 않은 goal은 아래 해당 worktree에서 읽는다. 다른 worktree의 로컬 기록이 이 checkout에도 있다고 가정하지 않는다.

- Core: [흐름 전체 DB 표와 캐릭터 저장 연결](../../01_Phases/goals/2026-10-17-character-storage-foundation/goal.md#재개-지점) · `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-active` · `docs/character-storage-closeout-20261023` · PR259 병합·종료 기록 진행

- <a id="content-worktree"></a>Content: [길드 현황·거점 성장·대장간 화면](../../01_Phases/goals/2026-10-15-guild-client-ui/goal.md#재개-지점) · [Content worktree 안내](#content-worktree) · `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active` · `feat/content-guild-client-integration-20261018`

- World: [길드 서버 — 클리어 보상에서 거점·무기 성장까지](../../01_Phases/goals/2026-10-14-guild-server/goal.md#재개-지점) · `C:/Users/bass1/orca/workspaces/DawnHolder_Project/world-active` · `docs/guild-server-closeout-20261018`

- Management: [운영툴 V1.0 — 서버 운영 기본](../../05_Management/goals/2026-10-10-server-operations/goal.md#재개-지점) · `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active` · `feat/server-operations-screen-20261010` · 게임 소개 페이지 goal은 보류(PR207 `feat/intro-site-20261008`)

- Art: [석재 지형·환경물 자체 교체와 움직임7종](../../01_Phases/goals/2026-10-28-stone-terrain-environment-replacement/goal.md#재개-지점) · C:/Users/bass1/orca/workspaces/DawnHolder_Project/art-active · feat/stone-environment-replacement-20261025 — v2 사용자A 승인, A/B 정적원화 계약 진행. 프레임B 포함 상한15회·지형생성0, C는42frame·실제재생끊김실패기준/봉인reference뒤. Level후속배치·촬영뒤제품병합/독립검증·기존견본별도정산.

- Rules: [Art·Level 정식 파트 운영 규칙](../../01_Phases/goals/2026-10-22-art-level-formal-parts/goal.md#재개-지점) · `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active` · `docs/art-level-formal-parts-closeout-20261022` · PR257 병합·Gardener 완료, 종료 기록 실사 중 · [공용 실행 잠금은 10-28 뒤까지 보류·기존 NOT PASS 유지](../../01_Phases/goals/2026-10-15-verification-lightening-execution-lock/goal.md#재개-지점) · [기존 PR2 가드는 동결까지 보류](../../01_Phases/goals/2026-10-13-astra-lead-mailbox-hotfix/goal.md#재개-지점)

Content goal의 상태·결정·남은 일은 위 상대 링크의 재개 지점에서 확인하며, 작업은 해당 Content worktree에서 이어간다. CodeMap은 표시 이름이며 경로·Architecture 태그는 유지한다. Rules와 확인한 Content 진입 외 다른 파트의 상태·branch를 새로 추정해 갱신하지 않는다.

[다음 세션 재개 절차](RESUME.md) · 적용 중인 결정은 각 goal의 「적용 중인 사용자 결정」 절에 있고, 정본 반영을 기다리는 결정은 [BACKLOG](BACKLOG.md) 행으로 찾는다.
