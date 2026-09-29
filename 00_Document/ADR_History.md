# 결정 이력

이 표는 주요 선택 시점을 찾는 요약이다. 결정의 이유・대안・당시 수치는 해당 ADR과 [정비 전 이력 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/ADR_History.md)에 보존되어 있다. 현재 구현・운영 상태는 [ADR 색인](ADR/INDEX.md)과 현행 문서를 확인한다.

| 시점 | 주요 선택 |
|---|---|
| 2026-05-06 | 자체 PDL・코드 생성, 공유 DLL 구성과 기존 ServerDev 코드의 부분 채택 |
| 2026-05-10 | 클라이언트 전송 계층 분리 |
| 2026-05-14 | DB 설계를 SQL Server・EF Core로 정리 |
| 2026-05-17 | Unity UI Additive Scene 분리 |
| 2026-05-25 | 맵 이동 시 entity ID 유지 |
| 2026-05-29 | 코드 규칙・책 참고자료 분리 |
| 2026-06-07 | Windows 실행 차단 환경에서 WSL 실행 선택 |
| 2026-06-08 | 행동 상태・판정의 서버 권위 |
| 2026-09-29 | 현행 운영 지침과 과거 정책 분리, 영역별 요약과 고정 Git 원문 연결 |

2026-05〜06월의 리뷰・학습 기록・Claude 훅・루프 운영 변화는 [과거 운영 결정](archive/workflow/legacy-decisions.md)에 정리했다. 이력에 적힌 과거 검증 결과를 이번 변경의 실행 결과로 사용하지 않는다.
