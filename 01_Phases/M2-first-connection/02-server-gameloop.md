# Phase 02: 서버 GameLoop (20 TPS) + 단일 GameMap actor

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2~3시간
> **담당 에이전트**: gameplay

---

## 🎯 목표

서버에 **20 TPS 틱 루프**(50ms 간격)를 만들고, 단일 `GameMap` 안에 `PlayerEntity` 목록을 보관하는 골격을 세운다. 매 틱마다 콘솔에 "Tick #N (Δ=Xms)" 로그가 안정적으로 찍힌다. 네트워크 패킷은 아직 안 붙임.

---

## ⏪ 사전 조건

- [x] M1 완료 (ServerCore, Listener 동작)
- [ ] Phase 01 완료 (병렬 가능. 두 Phase는 독립.)

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Loop/TickScheduler.cs` — 백그라운드 Task가 50ms 간격으로 `OnTick(long tickNumber)` 호출. **`Task.Delay` 금지, `Thread.Sleep` 금지** (헌법 #5). `Stopwatch` + busy-wait/SpinWait + 절대 시각 기준 drift 보정.
- [ ] `02_Server/GameServer/Maps/PlayerEntity.cs` — `record class` 또는 `class`. 필드: `EntityId(int)`, `Position(Vector2)`, `Owner(GameSession?)`.
- [ ] `02_Server/GameServer/Maps/GameMap.cs` — 단일 인스턴스 단순화. `List<PlayerEntity> _players`, `Tick(long tickNumber)` 메서드. **잠금 없음** (Map = 단일 thread actor, ARCHITECTURE 패턴).
- [ ] `02_Server/GameServer/Loop/GameWorld.cs` — `GameMap` 1개를 들고 TickScheduler에서 받은 콜백을 `GameMap.Tick`으로 forward.
- [ ] `Program.cs` 변경 — GameWorld 시작 + 종료 처리.
- [ ] tick duration 측정 코드 (Stopwatch로 매 tick 소요 ms 로깅, 1초마다 평균/최대 출력).

---

## ✅ 완료 조건

- [ ] `dotnet run --project 02_Server` → 콘솔에 50ms마다 "Tick #N" 한 줄. 1초 동안 약 20개.
- [ ] tick 간격 표준편차 < 5ms (drift 누적 없음)
- [ ] 30초 돌려도 tick 번호가 정확히 ~600 (오차 ±5 이내)
- [ ] `Task.Delay` / `Thread.Sleep` 코드 grep 결과 0건 (헌법 #5 검사)
- [ ] Ctrl+C로 깔끔히 종료 (CancellationToken 처리)

---

## 🧪 테스트

**자동 테스트:**
- `GameServer.Tests/Loop/TickSchedulerTests.cs` — 1초 동안 콜백 호출 횟수가 19~21 사이.
- TickScheduler 단위 테스트는 시간 의존적이라 flaky 위험 — 허용 오차 두기.

**수동 테스트:**
- 콘솔 30초 실행 → tick 카운트가 ~600, 최대 tick duration < 5ms.

---

## 📚 학습 포인트

- **20 TPS의 의미**: 1초에 20번 시뮬레이션. 게임 로직 시간 단위 = 1 tick = 50ms.
- **Fixed vs variable timestep**: 게임 서버는 fixed가 표준 — 결정론, 재현성, 양쪽 동기 수월.
- **Drift 보정**: `Task.Delay(50)`을 단순 반복하면 50.x ms씩 늦어져 누적. 대신 "절대 시각 T0 + N×50ms" 기준으로 다음 tick 시점을 계산.
- **Map = Actor 패턴**: 한 맵을 단일 thread가 도맡으면 내부에 lock 불필요. 동시성 버그의 90%를 차단.
- **헌법 #5의 무게**: 틱 루프에서 await 한 번이면 다른 모든 플레이어가 지연됨.

---

## ⚠️ 함정 / 주의사항

- `Thread.Sleep(50)`은 OS scheduler 정확도에 의존(Windows 기본 ~15ms 해상도). `timeBeginPeriod`로 해상도 올리면 배터리 소모 — busy-wait + Stopwatch로 대신.
- `Task.Delay`는 awaitable이지만 헌법 #5 위반. 차후 async 함정 발생 위험으로 *지금* 차단.
- `GameMap.Tick` 안에서 LINQ `ToList()` 자주 호출하면 GC 부담 — 다음 Phase에서 보일 수 있지만 지금은 신경 X (조기 최적화 X).
- TickScheduler 자체 unit test는 시간 의존이라 CI에서 가끔 실패할 수 있음 → 허용 오차 넉넉히.

---

## ➡️ 다음 Phase

- Phase 03: 접속 핸드셰이크 — 클라가 connect하면 PlayerEntity가 map에 들어가고, S_EnterMap 받음

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
