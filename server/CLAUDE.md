# Server — 권위(Authoritative) 게임 서버

## Layout

```
server/
├── GameServer/
│   ├── Network/        TCP listener, session, framing
│   ├── Loop/           Tick scheduler, world simulation
│   ├── Maps/           맵별 actor, spatial query
│   ├── Combat/         데미지 해석, hitbox 검사
│   ├── Persistence/    DB writer queue, EF context
│   ├── Handlers/       PacketId → handler dispatch
│   └── Program.cs
└── GameServer.Tests/   xUnit, 주로 handler + 공식 테스트
```

## 컨벤션

- **Async**: 핸들러는 `async Task`, 단 틱 루프는 동기.
  무거운 작업은 백그라운드 channel로 보냄.
- **Logging**: Serilog, 구조화. Info 레벨에서 패킷 페이로드 로깅 금지
  (PII / 스팸). Trace 레벨에서만.
- **Locking**: 각 맵은 단일 스레드. 맵 안에서는 lock 없음.
  맵 간 통신은 message channel만.
- **DI**: Microsoft.Extensions.DependencyInjection. 생성자 주입.
- **Tests**: 새 패킷 핸들러는 최소 happy path 1개 + rejection 테스트
  (validation/auth 실패) 1개.

## 금지 사항

- 틱 루프 안에서 `Task.Run`.
- 맵 상태에 `lock`/`Monitor` (actor 모델 사용).
- 정적 mutable 게임 상태. 싱글톤은 readonly 설정만 허용.
- 검증 없이 클라이언트 입력을 다른 클라에게 echo.

## 새 packet handler를 추가할 때

1. `shared/Protocol/`에 request/response 정의.
2. `server/GameServer/Handlers/`에 핸들러 추가.
3. dispatch 테이블에 등록.
4. 최소: happy 테스트 1, invalid input 테스트 1, auth 테스트 1 작성.
