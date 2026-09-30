# 유지보수 기준 적용 로드맵

| 순서 | 범위 | 선행 의존성 | 목표 |
|---|---|---|---|
| S0 | 코드 기준·목표 루프 정비 | 사용자 합의 | [기준 정비](../../goals/2026-09-30-maintainability-standards/goal.md) |
| S1 | 평타·Dash 공통 즉시 피해 처리와 첫 적용 CI | S0 통합 | [즉시 피해 처리](../../goals/2026-09-30-immediate-enemy-hit/goal.md) |
| S2 | 서버 영역별 적용 | S1 파일럿 성공 조건 충족 | [패킷 표현 책임](../../goals/2026-09-30-server-packet-publication/goal.md) |
| S3 | 공유·전송·네트워크 적용 | S2 관련 계약 확정 | [종료 자원 정리](../../goals/2026-09-30-session-disconnect-cleanup/goal.md) |
| S4 | Unity 클라이언트 적용 | S3 공유·프로토콜 계약 확정 | [원격 보간 상태](../../goals/2026-09-30-remote-interpolation/goal.md) |
| S5 | 도구와 전체 회귀 확인 | S2–S4 적용 결과 | [종합 회귀·보고](../../goals/2026-09-30-generator-exit-and-rollout-report/goal.md) |

각 영역은 제한적 조사 → 계약·책임 확인 → 필요한 작은 리팩토링 목표 순서로 분할한다. 기준을 이미 충족한 범위는 변경 불필요 근거를 해당 목표에 남긴다. 단계별 기준·상태·결과·병합 승인과 검증 근거는 각 goal.md 한 곳에서 관리한다.
