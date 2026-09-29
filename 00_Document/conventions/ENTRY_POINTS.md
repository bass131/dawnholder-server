# 증상별 진입점

먼저 증상이 생긴 입력·시점·엔티티를 확인하고 아래 심볼에서 관련 조건을 따라간다. 모든 거부가 silent drop인 것은 아니므로 실제 핸들러·응답 동작을 확인한다. 상세 파일 지도는 [FEATURE_MAP](../FEATURE_MAP.md)에 있다.

| 증상 | 시작 파일·심볼 | 이어서 볼 경계 |
|---|---|---|
| 접속 직후 끊김 | [GameSession.OnRecvPacket](../../02_Server/GameServer/Sessions/GameSession.cs) | 프레임 길이, 최초 패킷, 프로토콜 버전, 클래스 선택 |
| 이동 입력이 적용되지 않음 | [GameSession.SubmitMoveIntent](../../02_Server/GameServer/Sessions/GameSession.cs) | 입력 큐·속도·이동 중 상태, PlayerPhysicsSystem |
| 로컬 위치가 튐 | [PlayerPredictor](../../03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs) | 스냅샷 시점·ack·입력 기록·고정 시간 간격 |
| 공격 피해가 없음 | [CombatSystem.ProcessAttack](../../02_Server/GameServer/Maps/Systems/CombatSystem.cs) | 행동 가능 조건, 클라이언트 틱, 거리·hitbox·대상 생존 |
| 대시·텔레포트 방향 이상 | [ActionGate](../../02_Server/GameServer/Maps/Systems/ActionGate.cs), [SkillSystem](../../02_Server/GameServer/Maps/Systems/SkillSystem.cs) | facing·verticalDir 정규화와 로컬 예측 |
| 맵 전환 실패 | [MapMigration.Execute](../../02_Server/GameServer/Maps/Transitions/MapMigration.cs) | 포탈 ID·거리·해금 조건·이동 플래그 |
| 파티 UI와 서버 상태 불일치 | [PartyFlow](../../02_Server/GameServer/Party/PartyFlow.cs) | 초대 만료·세션 ID·PartyNotifier·클라이언트 State |
| 퀘스트 진행도 불일치 | [QuestRegistry.OnKill](../../02_Server/GameServer/Quest/QuestRegistry.cs) | 처치자·파티 소유 상태·Party→Quest 갱신 순서 |
| 종료 중 broadcast 오류 | [MapPacketPublisher](../../02_Server/GameServer/Maps/MapPacketPublisher.cs) | IsClosing과 receiver 소유 상태; [서버 사례](../domains/server.md) |
| Windows 빌드는 되지만 실행 차단 | [개발 안내](../operations/DEVELOPMENT.md) | WSL 명령, 실제 오류 코드·로그; 시스템 설정을 추측으로 변경하지 않음 |
