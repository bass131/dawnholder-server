# DB 설계 전 코드베이스 리팩토링 순서

| 순서 | 목표 | 선행 의존성 |
|---|---|---|
| M0 | [운영 문서·실행 기준선·평가 기준](../../goals/2026-09-29-refactor-baseline/goal.md) | 기존 문서 리팩토링 이후의 main |
| M1 | [서버 수명주기와 클라이언트 연결·맵 진입](../../goals/2026-09-29-refactor-lifecycle/goal.md) | M0 실행 기준선과 보존 계약 |
| M2 | [파티·퀘스트 소유권과 상태 모델](../../goals/2026-09-29-refactor-domain-state/goal.md) | M1 수명주기·전환 경계 |
| M3 | [설정·주석 정리, 전체 회귀와 재평가](../../goals/2026-09-29-refactor-regression/goal.md) | M1·M2 통합 결과와 M0 평가 조건 |
| 이후 | [DB 설계와 연동 작업 분할](../../goals/2026-09-29-persistence-design/goal.md) | M3 결과·남은 위험, 기존 schema/접속 인계 |

[평가 방법](assessment.md) · [마일스톤 운영](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md) · [현재 목표](../../../00_Document/operations/CURRENT.md)
