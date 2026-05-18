# Phase 10: Session lifecycle race 제거

> **상태**: pending
> **마일스톤**: M2.5 Hardening
> **예상 소요**: 1.5~2시간
> **담당 에이전트**: netcode

---

## 🎯 목표

connect 직후 tick 전에 disconnect가 와도 ghost player가 맵에 남지 않도록 한다. `_closing` 플래그 + disconnect 시 항상 map job 보내기로 *race window* 봉합. M3 두 명 같은 맵 진입 시 ghost entity 시각화 사고 사전 방지.

γ 감사 Codex β 발견: `GameSession.OnConnected`의 queued AddPlayer job과 `OnDisconnected`의 `_entityId<0` early-return이 race 가능. accept 직후 tick 전에 끊기면 cleanup이 누락되고, queued AddPlayer가 닫힌 세션 owner로 player를 맵에 박는다 (`GameSession.cs:41~62`, `:70~78`).

---

## ⏪ 사전 조건

- [ ] Phase 09 완료 (decode 예외 시 Disconnect가 박힌 후라야 race 테스트가 의미 있음)
- [ ] 본 작업용 feature 브랜치 분기 (예: `feature/youngho-m25-phase10-lifecycle`)

---

## 📝 작업 내용

### 1. `_closing` 플래그 추가

- [ ] `02_Server/GameServer/Network/GameSession.cs`에 `int _closing = 0;` 필드 추가 (Interlocked로 갱신).
- [ ] 의미 주석: "Phase 10(M2.5): connect job과 disconnect handler가 race할 때 idempotent cleanup 보장. 0=open, 1=closing/closed."

### 2. OnConnected map job — `_closing` 체크

- [ ] `OnConnected` 안의 `map.EnqueueJob(() => { ... })` 첫 줄에:
  ```csharp
  if (Volatile.Read(ref self._closing) == 1)
  {
      Console.WriteLine($"[Map] AddPlayer skipped — session already closing");
      return;
  }
  ```
- [ ] AddPlayer + entityId 저장 + Send도 같은 `_closing` 체크 안에서 수행 (closing 중이면 socket 닫혀 SocketException).
- [ ] **함정**: `_entityId = entity.EntityId` 박는 시점과 `_closing = 1` 박는 시점의 순서가 중요. 메모리 배리어 확보 — `Interlocked` API 사용으로 release/acquire 의미 보장.

### 3. OnDisconnected — 항상 map job + idempotent

- [ ] `OnDisconnected` 통째 재작성:
  ```csharp
  public override void OnDisconnected(EndPoint endPoint)
  {
      // 이미 닫혔으면 두 번째 호출 — enqueue 안 함 (Codex β 권장).
      if (Interlocked.Exchange(ref _closing, 1) == 1) return;

      Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

      GameMap map = GameWorld.Instance.Map;
      GameSession self = this;
      map.EnqueueJob(() =>
      {
          // entityId 모를 수도 있음 (race window 안: connect job이 아직 안 돌았다면 _entityId == -1).
          // 그래서 owner 기준 cleanup. AddPlayer가 이미 들어왔으면 그 entity도 제거됨.
          bool removed = map.RemovePlayerBySession(self);
          Console.WriteLine($"[Map] Session cleanup (removed={removed})");
      });
  }
  ```
- [ ] 핵심: *항상 enqueue* (early return X), 하지만 `_closing` 플래그가 *이중 enqueue 차단*. 단순 "항상 큐" + 멱등성 함수 결합.

### 4. GameMap.RemovePlayerBySession helper

- [ ] `02_Server/GameServer/Maps/GameMap.cs`에 추가:
  ```csharp
  // tick thread에서만 호출.
  public bool RemovePlayerBySession(GameSession owner)
      => _players.RemoveAll(p => ReferenceEquals(p.Owner, owner)) > 0;
  ```
- [ ] `RemovePlayer(int entityId)` 기존 시그니처는 유지 (호환). 신규 helper는 추가.
- [ ] 멱등성: owner가 없으면 0 반환, 두 번 호출 안전.

### 5. 테스트 인프라 — 결정론적 race injection

- [ ] **Codex β 검토 지적 반영**: rapid 100회는 *smoke*에 좋지만 race 재현이 결정론적 X. fake/제어 가능한 scheduler 필요.
- [ ] 옵션 A: `GameMap.Tick` 호출을 테스트에서 직접 제어 (이미 가능 — `_pendingJobs.TryDequeue` 패턴) → connect job push → *tick 호출 전* disconnect → 1회 tick → players=0 확인.
- [ ] 옵션 B: `GameWorld.Instance` 정적 의존성을 피해 `GameSession`에 `GameMap` 주입 옵션 추가? — over-engineering 가능, 본 Phase scope 밖. 옵션 A 채택.

### 6. 테스트 — GameSessionLifecycleTests

- [ ] 신설 `02_Server/GameServer.Tests/Network/GameSessionLifecycleTests.cs`:
  - **시나리오 A** (race window — 결정론적): 
    - 빈 `GameMap` 생성 → `GameSession` 인스턴스 → `OnConnected` 호출 (job 1개 enqueued)
    - tick *호출 전* `OnDisconnected` 호출 (job 1개 추가 enqueued, 총 2개)
    - `Tick(1)` 호출 → 두 job 순차 처리 → `Players.Count == 0` 확증
  - **시나리오 B** (회귀): 
    - `OnConnected` → tick 1회 → AddPlayer 적용 → `OnDisconnected` → tick 1회 → `Players.Count == 0`
  - **시나리오 C** (이중 OnDisconnected 멱등성): 
    - `OnDisconnected` 2회 호출 → enqueue job 1개만 (2번째 early return) → tick 후 `Players.Count == 0`, 예외 X
  - **시나리오 D** (smoke): rapid 100회 connect/disconnect → 누적 `Players.Count == 0`. *deterministic*은 아니지만 회귀 안전망.

### 7. 통합 테스트 회귀 확인

- [ ] `M2BasicMovementIntegrationTests` — 정상 흐름(connect → tick → move → disconnect) 영향 X 확증.

---

## ✅ 완료 조건

- [ ] 시나리오 A~D xUnit 통과
- [ ] 시나리오 A가 **결정론적** (10회 실행 모두 동일 결과 — flaky X) — Codex β 추가 발견 반영
- [ ] 시나리오 C에서 `OnDisconnected` 2회 호출이 예외/이중 enqueue 없이 통과 (idempotent 증명)
- [ ] `dotnet test` 전체 통과 (회귀 0)
- [ ] 콘솔 로그: rapid 100회 후 `[Map] AddPlayer skipped` 또는 `[Map] Session cleanup` 합산 = 100. ghost player 흔적 0.
- [ ] `/work:review` 위반 0건
- [ ] `10-session-lifecycle-race-DONE.md` 작성 + Post-flight 게이트 통과

---

## 🧪 테스트

**자동 테스트:**
- `GameSessionLifecycleTests.cs` — 시나리오 A~D
- 기존 `GameMapTests` (`EnqueueJob_MarshalsToTickThread` 등) 회귀 통과
- `M2BasicMovementIntegrationTests` 회귀 통과

**수동 테스트:**
- 서버 켜고, 연속 `nc localhost 7777` connect 즉시 Ctrl+C 50회 → 서버 콘솔 player count 0 확증.
- 정상 클라 접속 → 정상 동작 (race fix가 정상 흐름 깨지 않음).

---

## 📚 학습 포인트

- **분산 시스템의 "두 이벤트 사이"** — 학습 일지 ★★★ "Connect/disconnect race = 분산 시스템 첫 표적" 직접 시연 재료. connect job과 disconnect handler가 *서로 다른 thread*에서 진행되는 비동기 흐름의 일반 패턴 (Discord 봇, Slack 봇, gRPC 스트림 등 모든 long-lived 세션의 공통 함정).
- **EntityId 기반 cleanup의 한계** — entityId가 아직 -1인 race window에서는 entityId 기반 cleanup 무효. *owner reference* 기반 cleanup이 race-safe.
- **`_closing` flag + idempotent cleanup** — "항상 큐에 넣되 함수는 멱등"이 락 없이 race를 봉합하는 표준 패턴.
- **`Interlocked.Exchange` 반환값** — 박기 전의 값. `Exchange(ref _closing, 1) == 1`이면 *이미* 1이었다는 뜻 (이중 호출 가드).
- **`ReferenceEquals` vs `==`** — owner는 GameSession 참조, `==` 오버로딩 가능성 차단. `ReferenceEquals`로 신원 비교 명시.
- **테스트의 결정론성** — `Thread.Sleep` + race "기대"는 flaky test 1순위. job queue 직접 제어로 *실제 race 조건을 재현* (smoke 100회는 보조).

---

## ⚠️ 함정 / 주의사항

- **`Interlocked.Exchange(ref _closing, 1) == 1`이 truthy 분기** — 헷갈리기 쉬움. 박기 *전*의 값이 1이면 이미 닫힌 상태 = early return.
- **map job 안 `Send` 호출** — `_closing == 1`일 때 socket이 닫혀 있어 `SocketException`. base `Session.Send` 안 `m_disconnected == 1` 체크가 있으면 자동 무시 — 코드 흐름 확인 필요.
- **`Volatile.Read` vs `Interlocked.CompareExchange(ref f, 0, 0)`** — 둘 다 acquire 의미. `Volatile.Read`가 더 가벼움. 본 Phase는 `Volatile.Read` 채택.
- **broadcast loop `p.Owner.Send`** — 본 Phase 변경 후에도 `Owner == null` 체크 안전 (`GameMap.cs:76`). `_closing` 추가 체크는 over-engineering — M3 broadcast 진입 후 결정.
- **테스트 시나리오 A의 결정론성** — `_pendingJobs` `ConcurrentQueue`는 enqueue 순서 보존. 단일 thread 테스트에서 `OnConnected` → `OnDisconnected` 순차 enqueue 후 `Tick(1)` 호출하면 job 순서 결정론적.
- **`GameSession`의 base class 호출 순서** — `OnDisconnected`는 base `Session.Disconnect()`에서 호출됨. 본 Phase에서 GameSession의 OnDisconnected가 base보다 먼저 또는 나중에 호출되는지 확인 (`Session.cs:131`: `OnDisconnected(...)` 먼저, 그 다음 socket Shutdown/Close).
- **`GameWorld.Instance` 정적 의존성** — 테스트에서 `GameWorld` 리셋 가능? Phase 02 코드 확인 후 처리. 안 되면 `Map` 직접 주입 옵션 추가 검토 (Phase scope 밖이면 시나리오 A를 `GameMap` 단위로만 검증).

---

## ➡️ 다음 Phase

- Phase 11 (M2.5 정리) — 헌법 우선순위 표 + CONTEXT 응축 + UnitTest1 삭제. 또는 *바로 M3 진입* (`/work:plan M3` 호출, Phase 11 정리는 M3 첫 Phase 직전 또는 ad-hoc로 분리 가능).

---

## 작업 로그

- 2026-05-18: Phase 분해 완료 (γ 방식 2회 적용). 시작 대기. Phase 09 완료 후 진입.
