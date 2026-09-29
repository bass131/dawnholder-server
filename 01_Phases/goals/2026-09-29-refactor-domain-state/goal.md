# M2 — 파티·퀘스트 소유권과 상태 모델

[M1](../2026-09-29-refactor-lifecycle/goal.md) 통합 이후 두 개의 순차 목표/PR로 나눈다. 상태·결과는 각 goal에서만 관리한다.

| 순서 | 목표 | 선행 의존성 |
|---|---|---|
| M2a | [파티 멤버십과 퀘스트 진행·통보](../2026-09-30-refactor-party-quest/goal.md) | M1c 통합 |
| M2b | [기본 스탯과 현재 상태·캡처·이동 값](../2026-09-30-refactor-player-state/goal.md) | M2a 통합 |

GameWorld 접점과 검증을 공유하므로 순차로 진행한다. 멤버십/진행·통보 경계와 Shared 상태 모델의 변경을 한 PR에 섞지 않는다. 게임 정책은 보존하고 DB 구현·새 장비/성장·무관한 에셋 변경은 제외한다. 구현 후 별도 agent가 TestCode를 작성·검증하고 독립 리뷰 및 최신 CI를 확인한다. 후속 [M3](../2026-09-29-refactor-regression/goal.md)에 통합 결과·남은 결합/주석 문제를 인계한다.
