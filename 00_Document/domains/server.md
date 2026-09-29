# 서버

`02_Server/GameServer`는 권위 상태·게임 판정을, `02_Server/Network`는 전송·프레이밍을 담당한다. 공유 계약은 [프로토콜](protocol.md)에 있다.

## 수정 흐름

[GameSession](../../02_Server/GameServer/Sessions/GameSession.cs) → [Handlers](../../02_Server/GameServer/Handlers/) → 세션 제출 메서드 → [GameMap](../../02_Server/GameServer/Maps/GameMap.cs)의 큐 → [Systems](../../02_Server/GameServer/Maps/Systems/) 순으로 입력을 처리한다. 상태 변경은 소유한 틱 흐름 안에서 수행한다.

- 핸들러는 파싱·입력 검증·세션 메서드 호출을 맡는다. 세션의 핸드셰이크·entity ID·종료 상태를 직접 우회 변경하지 않는다.
- 새 입력에는 정상 경로와 잘못된 길이·범위·상태·권한의 거부 경로를 확인한다.
- 맵 이동은 [MapMigration](../../02_Server/GameServer/Maps/Transitions/MapMigration.cs), 파티·퀘스트 갱신 순서는 [GameWorld](../../02_Server/GameServer/Loop/GameWorld.cs)에서 확인한다.

## 종료와 broadcast

수신자의 연결 정리와 broadcast는 겹칠 수 있다. [MapPacketPublisher](../../02_Server/GameServer/Maps/MapPacketPublisher.cs)의 수신자 필터와 세션 `IsClosing`을 확인한다. 종료된 세션을 건너뛰어도 이미 시작된 전송의 수명주기까지 자동 해결되는 것은 아니다.

과거 `LifecycleRace_NewJoinBroadcastSkipsClosingSession` 사례는 `s2.OnConnected → s1.OnDisconnected → Tick` 순서로 닫히는 수신자가 목록에 남는 분기를 만들었다. 반대 순서에서는 큐가 먼저 정리되어 해당 분기를 거치지 않고도 테스트가 통과했다. 경쟁 조건 검증은 호출 순서와 실제 분기 진입을 함께 확인한다.

근거: [테스트 코드](../../02_Server/GameServer.Tests/), 과거 관찰 커밋 `5ea1123`, [이관 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/.claude/knowledge/server/_index.md). 당시 사례는 2회 관찰과 별도 리뷰를 기록했다.
