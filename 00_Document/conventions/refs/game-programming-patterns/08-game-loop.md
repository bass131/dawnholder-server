---
title: Game Loop
source: Game Programming Patterns — Robert Nystrom
category: Sequencing Patterns
---

# [GPP-08] 게임 루프 (Game Loop)

> 게임 시간의 흐름을 실제 CPU 속도와 사용자 입력으로부터 분리(decouple)하는 메인 실행 구조. 모든 게임의 심장.

## 언제 참조하나 (트리거)

- TickScheduler, 서버 틱, 클라이언트 프레임 루프 코드를 수정할 때.
- "느린 기기에서 게임이 빨리/느리게 돌아간다" 문제가 보고될 때.
- lag compensation, 물리 시뮬레이션의 determinism(재현성) 문제를 논의할 때.
- 클라이언트 보간(interpolation) 로직 또는 prediction/reconcile 타임스텝 설계를 볼 때.

---

## 핵심 내용

### 해결하는 문제

일반 프로그램은 입력을 기다리며 블록한다. 게임은 입력이 없어도 세계가 흘러야 하고, 입력이 들어오더라도 렌더링은 멈추지 않아야 한다. 이 세 가지를 동시에 처리하면서 **하드웨어 속도와 무관하게 일정한 게임플레이 속도**를 보장하는 것이 게임 루프의 존재 이유다.

기본 뼈대는 단 세 단계다:
```
while (gameRunning) {
    processInput();   // 블록 없이 입력 수집
    update();         // 게임 상태 전진
    render();         // 현재 상태 표시
}
```

### 변형 1 — 동기화 없는 고정 루프 (쓰지 말 것)

`update()`와 `render()`를 지연 없이 반복. 게임 속도가 CPU 성능에 직결된다. 빠른 기기에서 게임이 초당 수백 틱으로 돌고, 느린 기기에서 슬로우 모션이 된다. 교육용 예시 외에 쓸 이유가 없다.

### 변형 2 — 슬립 기반 고정 루프 (단순 게임)

한 프레임 처리 후 목표 프레임 시간(예: 16ms)까지 `sleep()`으로 대기. 빠른 기기에서 CPU를 낭비하지 않고, 모바일 배터리 절약에 유리하다. 단, **한 프레임이 목표 시간을 초과하면 대처할 방법이 없다** — 슬로우다운이 그대로 노출된다.

### 변형 3 — 가변 타임스텝 (쓰지 말 것)

경과 실시간(`elapsed`)을 `update(elapsed)`에 넘겨 "벽 시간만큼 전진"시키는 방식. 직관적으로 보이지만 두 가지 이유로 위험하다:

- **비결정론(non-determinism)**: 동일한 입력이 다른 기기에서 다른 결과를 낸다. 부동소수점 오차가 타임스텝마다 다르게 누적된다.
- **물리 불안정**: 물리 엔진의 감쇠 계수는 특정 스텝 크기를 가정한다. 스텝이 들쑥날쑥하면 물체가 폭발적으로 튀어 나간다.

네트워크 게임에서는 두 클라이언트가 다른 elapsed를 갖기 때문에 동기화 자체가 불가능해진다.

### 변형 4 — 고정 업데이트 + 가변 렌더링 (권장)

```
double lag = 0.0;
double previous = now();
while (gameRunning) {
    double current = now();
    lag += current - previous;
    previous = current;

    processInput();

    while (lag >= MS_PER_UPDATE) {   // 실제 시간을 따라잡는 고정 스텝 루프
        update();
        lag -= MS_PER_UPDATE;
    }

    render(lag / MS_PER_UPDATE);     // 보간 계수(0.0 ~ 1.0)
}
```

핵심 아이디어 세 가지:

1. **시뮬레이션은 고정 스텝**으로만 전진 — determinism 보장, 물리 안정.
2. **렌더링은 실시간**으로 호출 — 빠른 기기는 더 많이 그려 더 부드럽고, 느린 기기는 덜 그려도 시뮬레이션 속도는 유지.
3. **보간 계수**를 렌더러에 넘겨, 현재 업데이트와 다음 업데이트 사이 어느 지점에 있는지 외삽(extrapolation)해서 그린다. 예측이 틀려도 다음 프레임에서 조용히 교정되며 눈에 거의 띄지 않는다.

단점: 구현 복잡도가 높고, 느린 기기에서 inner loop가 무한히 돌지 않도록 최대 반복 횟수 가드가 필요하다. 렌더러도 보간을 지원해야 한다.

### 게임 루프 소유권 3가지 선택

| 소유자 | 특징 | 적합 상황 |
|---|---|---|
| 플랫폼/프레임워크 이벤트 루프 | 단순하나 제어 불가 | 브라우저 게임, UI 앱 |
| 엔진 루프 (Unity, Unreal 등) | 검증됨, 커스텀 어려움 | 상용 엔진 프로젝트 |
| 자체 루프 | 완전한 제어, 높은 복잡도 | 전용 서버, 특수 요건 |

### 프레임레이트 정책

- **무제한(PC 플러그인)**: 최고 품질, 전력 소모 최대.
- **30/60 FPS 캡**: 배터리 절약, 예측 가능한 성능 범위. 모바일 표준.

---

## 우리 프로젝트 적용

### 서버 측 — 이미 사용 중

`02_Server/GameServer/Loop/TickScheduler.cs`가 **변형 4 원리**를 서버 도메인에 적용한 구현이다:

- 20 TPS = `MS_PER_UPDATE` 50ms 고정 스텝.
- 각 `MapActor`가 독립 틱 루프를 가지며, 틱 안에서 `update()` 역할을 한다 (`GameMap.Tick()`).
- 헌법 원칙 #5 "틱 루프 블로킹 금지"는 곧 **"고정 스텝 루프를 망가뜨리지 마라"**의 구체화다. `await Task.Delay`나 동기 DB 호출이 inner update loop의 ms 예산을 초과하면 lag가 무한 누적되어 서버가 멈추는 것과 같다.
- `02_Server/GameServer/Maps/GameMap.cs` God class 분리 작업(M4.4 이후)에서 `ProcessAttack`, `UpdateEnemies`, `Respawn`을 별도 System으로 추출할 때도, 이 고정 스텝 계약은 그대로 유지해야 한다.

### 클라이언트 측 — 이미 사용 중 (Unity 루프 위에서)

Unity는 `Update()`(가변 프레임)와 `FixedUpdate()`(고정 물리 스텝)를 분리 제공 — 변형 4의 엔진 수준 구현이다.

- `PlayerPredictor`의 prediction 로직이 입력을 고정 스텝 단위로 처리 + 서버 확인 후 reconcile하는 구조는 "고정 업데이트 + 가변 렌더링" 원리와 동일하다.
- `RemoteEntity` 150ms 버퍼 보간이 `render(lag / MS_PER_UPDATE)` 외삽과 같은 역할을 한다.
- 클라이언트 입력 송신 50ms cadence = 서버 틱 50ms와 정렬 — 두 루프가 같은 MS_PER_UPDATE를 공유한다.

### lag compensation — 확장 포인트

`PlayerEntity` 4-tick ring buffer rewind가 "과거 상태 재현"을 구현한다. 이는 고정 스텝 루프가 없으면 불가능한 기능 — rewind할 "틱 번호"가 결정론적으로 존재해야 한다. M4.4 target rewind 구현도 같은 전제를 따른다.

---

## 함정 / 과용 경계

- **가변 타임스텝 유혹**: `Time.deltaTime`을 `update`에 직접 넘기는 방식은 단순해 보이지만, 느린 기기나 GC 스파이크 한 번에 물리가 폭발한다. 클라이언트에서도 physics-sensitive 계산에는 쓰지 말 것.
- **inner loop 무한 스핀**: 느린 기기에서 `lag`가 계속 쌓이면 `while (lag >= MS_PER_UPDATE)` 안에서 무한루프처럼 돌 수 있다. 반드시 최대 반복 횟수 가드(`MAX_UPDATES_PER_FRAME`) 적용.
- **보간 없는 고정 스텝 렌더링**: `render()` 에 보간 계수를 안 넘기면 고정 스텝 경계에서 뚝뚝 끊기는 스터터링이 보인다. 특히 60Hz 이상 모니터에서 20TPS 서버 상태를 그대로 그리면 명확히 보임.
- **틱 안에서 블로킹**: DB 조회, 동기 파일 IO, `Thread.Sleep` 한 번이 50ms 예산 전체를 잡아먹는다. lag 누적 → 서버가 "갈증"나서 프레임을 건너뛰는 것과 같다.
- **MS_PER_UPDATE 너무 작게**: 스텝이 짧을수록 시뮬레이션 정밀도가 높아지지만 CPU 비용이 올라가고, 각 틱에서 "하나라도 느린 연산" 시 lag 누적이 심해진다. 게임 장르·하드웨어 기준으로 튜닝.

---

## 관련

- `02_Server/GameServer/Loop/TickScheduler.cs` — 고정 스텝 구현체
- `02_Server/GameServer/Maps/PlayerEntity.cs` lag compensation ring buffer — 고정 스텝이 전제인 기능
- [[09-update-method]] — 각 `update()` 안에서 오브젝트별 상태를 전진시키는 패턴 (게임 루프의 짝)
- [ADR-004-tickrate.md] — 20 TPS 결정 근거
