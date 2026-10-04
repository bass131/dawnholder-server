# 현재 목표

상태·결정·남은 일은 각 goal의 재개 안내에서 확인한다. 아직 병합되지 않은 goal은 아래 해당 worktree에서 읽는다. 다른 worktree의 로컬 기록이 이 checkout에도 있다고 가정하지 않는다.

| 파트 | 목표 상태와 재개 안내 |
|---|---|
| GameDev | [영속성 저장소](../../01_Phases/goals/2026-10-02-persistence-repository/goal.md#재개-지점) |
| Content | 아이템·인벤토리·재화 · 미통합 로컬 goal: `01_Phases/goals/2026-10-05-items-inventory-currency/goal.md#재개-지점`(해당 worktree 내부 경로). [Content worktree 안내](#content-worktree) |
| Rules | [운영 정본 반영](../../01_Phases/goals/2026-10-05-operating-canon/goal.md#재개-지점) · [보류된 하네스 원칙 채택과 문서 정비](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#운영-규칙-pr-재개와-설계) |
| CodeMap(Architecture) | [CodeGraph adapter 정비](../../01_Phases/goals/2026-10-03-codegraph-adapter-cleanup/goal.md#재개-실행) |
| Management | [시스템 카드](../../05_Management/goals/2026-10-02-system-cards/goal.md#재개-지점--2026-10-03-사용자-휴식) |

- GameDev: `C:/Dev/DawnHolder_Project` · `feat/persistence-repository-d1b-20261002`
- <a id="content-worktree"></a>Content: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active` · `feat/items-inventory-currency-20261005`
- Rules: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active` · `docs/operating-canon-20261005`
- CodeMap(Architecture): `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active` · `feat/codegraph-adapter-cleanup-20261003`
- Management: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active` · `feat/management-m2-system-cards`

Content goal은 미통합 로컬 기록으로, 위 worktree 내부 경로를 해당 Content worktree에서 연다. CodeMap은 표시 이름이며 경로·Architecture 태그는 유지한다. Rules와 확인한 Content 진입 외 다른 파트의 상태·branch를 새로 추정해 갱신하지 않는다.

[다음 세션 재개 절차](RESUME.md) · [정본 반영 전 적용 결정](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)
