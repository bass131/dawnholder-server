# M1b — 클라이언트 연결 수명과 지연 callback

상태: 예정·구현 미착수. [M1a](../2026-09-29-refactor-server-lifecycle/goal.md) 통합 후 최신 main의 별도 브랜치/PR에서 시작한다. [M1](../2026-09-29-refactor-lifecycle/goal.md)의 두 번째 목표다.

범위: NetworkService의 연결 시도·취소·종료·재시도 owner, UnityClientSession의 유효 세션 generation과 패킷 적용 callback 경계. 오래된 세션 callback이 새 세션 상태를 지우거나 적용하지 못하도록 한다. 자동 재접속·인증·DB·게임 정책은 추가하지 않는다.

설계 후보는 main-thread owner, 취소 가능한 connect attempt, 실행 직전 session-scoped callback 검증이다. 접속 adapter를 Unity 내부에 둘지 공용 Connector에 둘지는 DLL 양쪽 검증 비용과 중복 구현 비용을 비교해 착수 시 확정한다. 기존 GUID·직렬화 값은 보존한다. 새 파일/.meta 추가는 기존 GUID 재생성과 구분한다.

클라이언트 설계 근거: 로컬 `.backups/reviews/2026-09-29-m1-client-design.md`. 이는 초안이며 선택 설계·정확한 파일/인터페이스·검증자 소유권을 goal에 확정하기 전에는 구현하지 않는다.

완료조건: 자연 disconnect·거절·접속 실패·Connecting 취소·늦은 완료·재접속과 이전 세션 packet/delayed intent를 독립 서브에이전트 테스트 코드로 검증한다. 실제 Unity Play/화면 확인과 EditMode를 구분한다. 구현자와 테스트 작성·실행 담당을 분리한다. 각 PR 병합은 사용자 명시 승인 후 수행한다.

통합 후 [M1c](../2026-09-29-refactor-client-entry/goal.md)로 연결 수명 계약과 미실행 범위를 인계한다. 현재 브랜치·구현·검증·PR 없음.
