# 영역별 변경 계약

할당된 기능과 연결된 계약만 읽는다. 전체 구조는 [ARCHITECTURE](../ARCHITECTURE.md), 파일 위치는 [FEATURE_MAP](../FEATURE_MAP.md), 실행 방법은 [DEVELOPMENT](../operations/DEVELOPMENT.md)에 있다.

| 영역 | 담당 내용 |
|---|---|
| [서버](server.md) | 입력 검증, 맵·세션 상태, 종료와 broadcast |
| [클라이언트](client.md) | 예측·재조정, Unity 자산과 수명주기 |
| [프로토콜·공유 코드](protocol.md) | PDL·버전·DLL·런타임 호환성 |
| [도구·검증](tooling.md) | 봇·테스트의 진입점과 확인 범위 |
| [영역 간 경계](cross-cutting.md) | 입력 시점·판정 권한·환경에 걸친 문제 |

옛 knowledge의 기술 사례를 현재 진입점과 함께 정리했다. 과거 관찰 결과는 새로운 실행 결과를 대신하지 않으며, 원문 링크는 당시 사실과 숫자를 확인할 때 사용한다.
