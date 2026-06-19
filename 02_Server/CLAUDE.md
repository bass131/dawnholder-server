# Server — 권위(Authoritative) 게임 서버

## Layout

> M3 Phase 03 (헌법 #4 봉합): 본 표는 *실제 폴더 구조와 1:1 정합*. 폴더가 약속으로만 박혀
> 있고 실재 X면 가짜 약속 — 코드 변경 + 본 표 갱신은 *동일 commit*.

```
02_Server/
├── Network/            ServerCore — TCP socket 인프라 (M2 박힘, GameServer와 별 csproj)
│   ├── Session.cs            PacketSession abstract base (length-prefixed framing)
│   ├── Listener.cs           accept loop + race window fail-closed (M3.8 Phase 05 봉합)
│   ├── Connector.cs          client connect (헤드리스 봇 재사용)
│   ├── RecvBuffer.cs         수신 링 버퍼
│   ├── SendBuffer.cs         송신 링 버퍼
│   ├── FrameValidator.cs     frame 헤더 검증 helper (M4.1 Phase 03 — 04_ClientNet과 동기화 약속)
│   ├── JobQueue.cs           작업 직렬화 큐
│   └── PriorityQueue.cs      우선순위 큐 자리잡이
├── GameServer/
│   ├── Network/        Game 도메인 session — PacketSession 상속
│   │   └── GameSession.cs    socket 콜백 + first-packet 게이트 + Dictionary dispatch +
│   │                          캡슐화된 lifecycle/state 메서드 (CompleteHandshakeAndEnter
│   │                          / RejectHandshake / SubmitMoveIntent / RespondPong)
│   ├── Handlers/       IPacketHandler 단위 + dispatch 테이블 (M3 Phase 03 신설)
│   │   ├── IPacketHandler.cs       internal 인터페이스 (decode + 검증 + session 호출)
│   │   ├── HandlerRegistry.cs      Dictionary<PacketID, IPacketHandler> (한 줄 등록)
│   │   ├── Session/                연결 수명주기 관련 핸들러
│   │   │   ├── HandshakeHandler.cs     C_Handshake → version 검증 → session 캡슐화 메서드
│   │   │   ├── PingHandler.cs          C_Ping → session 캡슐화 메서드
│   │   │   └── CharacterSelectHandler.cs  C_CharacterSelect → 클래스 선택 + session 상태
│   │   ├── Movement/               이동 입력 핸들러
│   │   │   └── MoveIntentHandler.cs    C_MoveIntent → InputBits.Decode → session 캡슐화 메서드
│   │   ├── Combat/                 전투 입력 핸들러
│   │   │   └── AttackHandler.cs        C_Attack → targetEntityId 전달 (M3 Phase 06 신설)
│   │   ├── Skill/                  스킬 입력 핸들러
│   │   │   └── SkillUseHandler.cs      C_SkillUse → 신뢰 경계 3단계 검증 → session 캡슐화 메서드
│   │   └── Zone/                   맵 이동 핸들러
│   │       └── EnterPortalHandler.cs   C_EnterPortal → 포털 진입 처리
│   ├── Loop/           Tick scheduler, world simulation
│   ├── Maps/           맵별 actor, spatial query, PlayerEntity
│   ├── Combat/         M3 응급 단순화 (CombatConstants/EnemyKind/EnemyEntity) — M4 정밀화 대기
│   ├── Party/          파티 전역 actor (PartyRegistry/PartyState/PartyNotifier) — M5 박힘
│   ├── Quest/          퀘스트 전역 actor (QuestRegistry/QuestConstants) — M7.6 P01 Party에서 분리
│   ├── Persistence/    (예정 — M5 진입 시 박힘) DB writer queue, EF context
│   └── Program.cs
└── GameServer.Tests/   xUnit
    ├── Network/        핸들러 단위 + lifecycle/rate-limit/length 검증
    ├── Loop/           TickScheduler/Metrics
    ├── Integration/    M2 movement 통합
    └── *.cs            InputBits / MoveIntent(GameMap 단위) / Physics / PacketRoundTrip
```

> M4+ 예정: `Combat/` (데미지 해석, hitbox), `Persistence/` 본격 구현.

## 컨벤션

- **Async**: 핸들러는 *현재 sync* `void Handle(GameSession, ArraySegment<byte>)` (Phase 03 박힘).
  파싱·검증·session 메서드 호출 수준이라 async 불필요. 틱 루프도 동기.
  IO / long-running work는 tick loop 밖으로 격리 (백그라운드 channel).
  ※ 분기 많은 handler (DB 호출 포함 등) 들어오면 `IPacketHandler` + dispatcher를 `Task` 기반으로 승격 검토.
- **Logging**: 현재 `Console.WriteLine` 직접 사용 (응급 데모 단순화). **M5 진입 시 Serilog 도입 예정** — 구조화 + Info 레벨 패킷 페이로드 로깅 금지 (PII / 스팸), Trace 레벨에서만. M3.6 Phase 04 점검 발견 = 옛 본문이 "Serilog" 박혔으나 실재 미박힘 정정 (false-promise 7번째 발본 봉합).
- **Locking**: 각 맵은 단일 스레드. 맵 안에서는 lock 없음.
  맵 간 통신은 message channel만.
- **DI**: 현재 미박힘 (응급 데모 단순화). **M5 진입 시 Microsoft.Extensions.DependencyInjection 도입 예정** — 생성자 주입. M3.6 Phase 04 점검에서 옛 본문 stale 정정.
- **Tests**: 새 패킷 핸들러는 최소 happy path 1개 + rejection 테스트
  (validation/auth 실패) 1개.

## 금지 사항

- 틱 루프 안에서 `Task.Run`.
- 맵 상태에 `lock`/`Monitor` (actor 모델 사용).
- 정적 mutable 게임 상태. 싱글톤은 readonly 설정만 허용.
- 검증 없이 클라이언트 입력을 다른 클라에게 echo.
- 핸들러가 GameSession 내부 state(`_handshakeCompleted` / `_entityId` /
  rate-limit window)를 *직접* 만짐 — session 캡슐화 메서드(internal)만 호출.

## 새 packet handler를 추가할 때

1. `98_Shared/Protocol/` PDL XML에 request/response 정의 + 재생성.
2. `02_Server/GameServer/Handlers/XxxHandler.cs` 신설 (`IPacketHandler` 구현).
   핸들러는 *decode + 검증 + session 메서드 호출*만 — lifecycle state는 session 안.
3. `HandlerRegistry._handlers` Dictionary에 *한 줄* 등록 (`{ PacketID.X, new XxxHandler() }`).
4. 최소 테스트: happy 1, invalid input 1, auth(handshake 미완료 또는 권한 부재) 1.
