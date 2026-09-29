# 서버

GameServer는 권위 상태·게임 판정을, Network는 전송·프레임 처리를 담당한다.

- [Program](GameServer/Program.cs): 맵 로드·listener·world 시작.
- [GameSession](GameServer/Sessions/GameSession.cs): 최초 패킷·세션 상태·입력 제출.
- [GameWorld](GameServer/Loop/GameWorld.cs): 틱·맵·파티·퀘스트 소유.
- [GameMap](GameServer/Maps/GameMap.cs): 맵 상태·작업 큐·시스템 실행.
- [GameServer.Tests](GameServer.Tests/): 단위·통합 검증.

변경 계약·종료 경쟁은 [서버 문서](../00_Document/domains/server.md), Shared·클라이언트 영향은 [기능 지도](../00_Document/FEATURE_MAP.md)를 본다. [개발 안내](../00_Document/operations/DEVELOPMENT.md)의 WSL 명령으로 검증한다. 서버 실행은 7777 포트를 사용하며 Windows 솔루션 빌드는 Unity DLL을 갱신할 수 있다.
