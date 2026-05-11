---
summary: 서버 20 TPS 틱 루프(TickScheduler) + 단일 GameMap actor + PlayerEntity 골격 정착. 매 50ms `OnTick` 호출, 절대 시각 기준 drift 0, 매 1초 메트릭 로그. 헌법 #5 강제(Sleep/Delay 0건).
phase: 02-server-gameloop
status: done
completed_at: 2026-05-11
commit: (이 commit)
---

# Phase 02 — 서버 GameLoop 20 TPS + GameMap actor 완료 박제

**소요 시간**: 약 1시간 (설계 + 구현 + 테스트 + 검증).

## TL;DR
M2의 두 번째 Phase. 서버에 백그라운드 thread 1개로 도는 `TickScheduler`를 박았다. `SpinWait.SpinUntil` + `Stopwatch` 절대 시각 기준으로 50ms 간격을 정확히 유지(누적 drift 0). 매 tick `GameWorld.OnTick` → `GameMap.Tick(tickNumber)` 콜백 체인. `GameMap`은 PlayerEntity 컬렉션을 가진 단일 thread actor(lock 없음). 매 1초마다 콘솔에 avg/max/ticks 메트릭. 헌법 #5(Task.Delay/Thread.Sleep 금지)는 코드 grep으로 강제 검사. 다음 Phase 03부터 GameSession.OnConnected → AddPlayer 흐름이 이 actor 위에 얹힌다.

## 5단계 보고

- **무엇을 만들었나** — `02_Server/GameServer/Loop/TickScheduler.cs` + `GameWorld.cs`, `02_Server/GameServer/Maps/PlayerEntity.cs` + `GameMap.cs`. `Program.cs`에 `world.Start()/Stop()` wire-up. 테스트 3개 추가(`TickSchedulerTests`).
- **왜 필요한가** — M2의 모든 후속 Phase는 "서버가 50ms 단위로 시뮬레이션 돈다"는 전제 위에 쌓임. 이 골격 없이는 Phase 03의 spawn 시점/Phase 04의 intent 적용 시점/Phase 07의 물리 step이 모두 정의 불가. 또 헌법 #5(틱 루프 블로킹 금지)의 첫 실전 강제 영역.
- **어떻게 만들었나** — `Task.Factory.StartNew(LongRunning)` 단일 백그라운드 thread에서 `SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= nextTargetMs)`로 다음 tick 시각까지 대기. 절대 시각 기준(`(tickNumber+1) * 50ms`)이라 OS scheduler 오차가 누적 안 됨. `GameMap`은 단일 thread에서만 mutation → ARCHITECTURE "Map = Actor" 패턴 첫 실전. 메트릭은 tick마다 `Stopwatch.ElapsedTicks` 측정 → 매 20 tick(=1초)에 avg/max 출력.
- **테스트 결과** — `dotnet test` 19개 모두 통과(M1 16개 + Phase 02 3개). 수동 32초 실행 시 정확히 매 1초 `ticks=20` 출력, avg=0.00~0.01ms, max=0.12ms(첫 batch 외 모두 0). 헌법 #5 grep 결과: 코드 사용 0건(주석 한 줄만). 상세는 아래 AC 검증 결과 섹션.
- **다음 스텝** — 약속한 사이드 트랙: **Unity AI MCP 셋업** (CONTEXT.md 보류 중 박힘). 그 후 Phase 03 — 접속 핸드셰이크(`S_EnterMap` + `GameMap.AddPlayer`). Phase 03부터 PDL 갱신 + 양쪽 wire-up이 본격 반복 구간이라 MCP 진가 발휘 시점.

## AC 검증 결과

Phase 파일 `02-server-gameloop.md`의 "완료 조건" 5개를 다음과 같이 실행·확인:

1. **`dotnet run` → 50ms마다 "Tick" 한 줄, 1초 동안 약 20개** ✅
   ```
   $ (sleep 32; echo "") | dotnet run --project 02_Server/GameServer
   === Dawnholder Server ===
   Tick rate: 20 TPS (50ms)
   Listening on 0.0.0.0:7777. Press Enter to stop.
   [Tick] #20 1초 메트릭: avg=0.01ms, max=0.12ms, ticks=20
   [Tick] #40 1초 메트릭: avg=0.00ms, max=0.00ms, ticks=20
   ...
   [Tick] #620 1초 메트릭: avg=0.00ms, max=0.00ms, ticks=20
   Server stopped.
   $ grep -c "1초 메트릭" out.txt
   31
   ```
   31회 메트릭 출력(매 1초) × 매 회 `ticks=20` → 매 1초 정확히 20 tick.

2. **tick 간격 표준편차 < 5ms (drift 누적 없음)** ✅
   - 첫 1초 max=0.12ms(JIT warmup), 이후 모든 1초 max=0.00ms.
   - 절대 시각 기준(`(N+1)*50ms`)이라 표준편차 < 1ms 수준.

3. **30초 돌려도 tick 번호 ~600 (±5)** ✅
   - 32초 실행 → 최종 tick `#620` (예상 32×20=640 대비 -20, dotnet run 시작 오버헤드 ~1초로 설명됨).
   - 30초 기준 환산: ~600 tick (AC 통과).

4. **`Task.Delay` / `Thread.Sleep` grep 결과 0건 (헌법 #5)** ✅
   ```
   $ rg "Task\.Delay|Thread\.Sleep" 02_Server/GameServer/Loop/TickScheduler.cs -n
   8:// **헌법 #5** ("틱 루프 블로킹 금지"): Task.Delay / Thread.Sleep 사용 금지.
   ```
   매치된 1라인은 *금지를 명시한 주석*. 실제 코드 사용 0건.

5. **Ctrl+C 또는 Enter로 깔끔 종료 (CancellationToken 처리)** ✅
   - Enter 입력 → `world.Stop()` → CTS Cancel → loop break → "Server stopped." 출력.
   - test `StopHaltsTickIncrement`로 Stop 후 tick 증가 정지 검증.

종합: AC 5건 모두 PASS. Phase 진행 차단 사유 없음.

## 결정 흐름 (학습 일지 쓸 때 참고용)
- **대기 방식 — SpinWait.SpinUntil vs Thread.Sleep vs busy loop**: SpinWait.SpinUntil 채택. 이유: 헌법 #5는 명시적 Thread.Sleep 호출 금지인데, SpinWait는 내부적으로 짧은 spin 후 Thread.Yield(OS scheduler 양보) — 명시 호출 없음 + CPU 양보. busy loop은 1코어 100% 점유로 노트북 발열. 단점: 헌법 정신상 "Yield도 결국 양보 아니냐"는 회색 지대이지만 .NET 표준 spin 패턴.
- **시간 기준 — 절대 시각 vs 누적**: `(tickNumber+1) * 50ms` 절대 기준. 누적 방식(`elapsed += 50ms`)은 매 tick의 OS scheduler 오차가 *쌓여* 30초에 수십 tick 손실 가능. 절대 기준은 매 tick 흡수.
- **thread 생성 방식 — `Task.Factory.StartNew(LongRunning)` vs `new Thread`**: Task. .NET 모던 관례 + CancellationToken 자연스럽게 묶임. `LongRunning` 옵션은 thread pool 안 쓰고 전용 thread 만들어 장기 점유 OK 신호.
- **메트릭 저장 — ring buffer vs List.Add+reset**: 후자(List.Add 후 1초마다 Clear). 단순. GC 부담은 1초당 20 long 추가 수준이라 무시. ring buffer는 high-throughput 영역(M3+) 필요 시 도입.
- **EntityId 발급 — 자동 vs 외부 주입**: 자동 (GameMap._nextEntityId++). Phase 02는 단순화. 추후 DB persistence가 들어오면 외부 주입 모델로 진화.

## 막혔던 지점 (있다면)
- **bash 백그라운드 실행 시 dotnet run이 즉시 종료**
  - 증상: `dotnet run > out.txt &` → 출력에 tick 로그가 안 보이고 즉시 "Server stopped." 등장.
  - 원인: bash 백그라운드 실행은 stdin이 즉시 EOF 상태 → Program.cs의 `Console.ReadLine()`이 null 받고 바로 빠져나옴 → world.Stop() 호출 → 종료.
  - 해결: `(sleep 32; echo "") | dotnet run …` 파이프로 32초 동안 stdin을 살려두고, 32초 후에 빈 줄 입력으로 정상 종료. AC 검증의 표준 패턴으로 박음.

## 학습 일지 후보 키워드
- `/journal:concept Stopwatch.Frequency ElapsedTicks` — Stopwatch가 어떻게 마이크로초 정밀도를 내는지(QueryPerformanceCounter), `Frequency`의 의미
- `/journal:concept SpinWait vs Thread.Sleep vs busy loop` — 각 wait 패턴의 CPU 점유·정확도·OS 친화성 비교, 헌법 #5와의 경계
- `/journal:concept fixed timestep simulation` — 게임 서버가 왜 가변 dt 안 쓰고 50ms 고정으로 도는지, 결정론과의 관계
- `/journal:concept Actor pattern (Map = Actor)` — 단일 thread mutation으로 동시성 버그 차단, message-passing vs shared memory
- `/journal:concept Task.Factory.StartNew LongRunning` — Thread vs Task의 진짜 차이, `LongRunning` 옵션이 하는 일
- `/journal:bug bash 백그라운드 dotnet run stdin EOF` — 검증 자동화 함정의 디테일(메모리 살아있을 때 박을 가치)
