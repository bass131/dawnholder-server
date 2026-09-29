# 도구와 검증

[GameServer.Tests](../../02_Server/GameServer.Tests/)는 xUnit 단위·통합 검증을, [headless-bot](../../99_Tools/headless-bot/)은 ClientNet·Shared를 사용한 통신 시나리오를 제공한다. 실행 명령과 환경 전제는 [DEVELOPMENT](../operations/DEVELOPMENT.md)를 따른다.

| 변경 | 검증 출발점 |
|---|---|
| 패킷·입력 검증 | 패킷 왕복, 길이·범위·권한 거부 테스트와 해당 봇 |
| 이동·전투 | 물리·판정 단위 테스트, 관련 Integration/시나리오 |
| 맵·파티·퀘스트 | 상태 이전·ID·진행도 단위 검사와 해당 봇 |
| Unity 표현 | Unity 컴파일·플레이 확인; .NET 결과로 대체하지 않음 |
| 문서 | 경로·심볼·권한 일치, 링크와 원문 근거, HTML 렌더 |

테스트가 성공한 분기·실패한 조건·Skip 지정·검증하지 않은 영역을 구분한다. 봇 이름만으로 검증 범위를 확대하지 않는다. 예를 들어 PartyQuest standalone의 파티 결성·맵 이동·해산 성공은 공동 처치 카운트를 확인했다는 뜻이 아니다.

WSL 실행은 원본 경로별 복사 공간과 잠금을 사용한다. 7777 포트가 다른 작업에 속하면 종료하지 않고 충돌을 보고한다. 시나리오와 원본 로그 위치를 결과에 남긴다.

과거 QA knowledge는 전용 검증 사례가 없는 색인이었다. 이 문서는 존재하지 않았던 실적을 추가하지 않고 현재 테스트·도구 진입점으로 대체했다. [원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/qa/_index.md).
