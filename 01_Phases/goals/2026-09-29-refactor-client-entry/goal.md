# M1c — 클라이언트 씬 진입과 맵 전환

상태: 예정·구현 미착수. [M1b](../2026-09-29-refactor-client-connection/goal.md) 통합 후 최신 main의 별도 브랜치/PR에서 시작한다. [M1](../2026-09-29-refactor-lifecycle/goal.md)의 마지막 목표다.

범위: 씬 준비·서버 spawn/HP/roster 적용·입력 재개의 단일 owner, 겹친 맵 전환의 완료·취소와 epoch. 기존 씬 이름·프리팹·GUID·문자열 경로·게임 정책·wire 계약을 보존한다. SceneTransition의 busy 반환 전에 수행되는 부작용과 정적 pending 상태를 정리한다.

클라이언트 설계 근거: 로컬 `.backups/reviews/2026-09-29-m1-client-design.md`. 착수 시 선택 설계·인터페이스·정확한 파일·담당을 문서화하고 구현한다. 연결과 씬 진입의 공통 파일은 단일 writer로 둔다.

완료조건: 연결 종료/전환 중 패킷·겹친 전환·씬 실패/취소·HP/roster 준비·입력 재개를 독립 서브에이전트 테스트 코드로 검증한다. Town↔플레이 맵 왕복과 서버–Unity 통합 플레이 결과를 자동검증·실제 화면/입력/오디오 확인으로 구분한다. 미실행은 완료로 표시하지 않는다. 구현자와 검증자를 분리하고 각 PR 병합은 사용자 명시 승인 후 수행한다.

완료 후 [M2](../2026-09-29-refactor-domain-state/goal.md)에 수명주기 계약을 인계한다. 현재 브랜치·구현·검증·PR 없음.
