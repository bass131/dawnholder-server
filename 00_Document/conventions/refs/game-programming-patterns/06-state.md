---
title: State
source: Game Programming Patterns
category: Design Patterns Revisited
---
# [GPP-06] State (상태 패턴)
> 객체의 내부 상태에 따라 행동이 달라져야 할 때, 상태를 클래스로 캡슐화하고 위임(delegation)으로 전환하는 패턴. FSM(Finite State Machine)의 OOP 구현.

---

## 언제 참조하나 (트리거)

- 입력 처리 코드에 `isJumping`, `isDucking` 같은 불리언 플래그가 2개 이상 얽혀 있어 잘못된 조합이 생길 때.
- 한 메서드에 상태별 `switch` + `if-else`가 길게 늘어나, 새 상태 추가 시 여러 곳을 동시에 고쳐야 할 때.
- 상태별 전용 데이터(예: 차징 시간, 쿨다운)를 메인 클래스에 억지로 붙여야 할 때.
- 적 AI FSM(EnemyState Idle/Patrol/Chase)이나 플레이어 상태 전환 로직을 추가·수정할 때.

---

## 핵심 내용

### 문제: 플래그 지옥
캐릭터 입력을 `isJumping_`, `isDucking_` 등 불리언으로 관리하면, 필드가 늘어날수록 "점프 중에 다이브"처럼 이론상 불가능한 조합이 코드 실수로 발생한다. 로직이 여러 메서드에 흩어지고, 새 상태를 추가할 때마다 기존 분기를 전부 검토해야 한다.

### FSM이란
- **고정된 상태 집합**: Standing / Jumping / Ducking / Diving 같이 열거 가능.
- **동시에 하나의 상태만 활성**: 잘못된 조합 자체가 불가능해진다.
- **전환(Transition)**: 각 상태는 "어떤 입력을 받으면 어떤 상태로 간다"를 정의한다.

### 1단계 — Enum + switch (단순 FSM)
하나의 `state_` 열거값으로 현재 상태를 표현하고, `handleInput` 메서드 안에서 상태별로 `switch`한다. 각 케이스가 한 상태의 로직을 완전히 담는다. 플래그 조합 문제는 사라지지만, 상태별 전용 데이터를 메인 클래스에 두어야 하는 한계가 남는다.

### 2단계 — State 인터페이스 (GoF State 패턴)
```
IHeroineState
  virtual void handleInput(Heroine&, Input)
  virtual void update(Heroine&)
  virtual void enter(Heroine&)   // 상태 진입 시 1회
  virtual void exit(Heroine&)    // 상태 이탈 시 1회
```
각 상태가 클래스가 된다. 상태 전용 데이터(`chargeTime_` 등)는 해당 상태 클래스의 필드로 들어간다. 메인 클래스는 `state_->handleInput(...)` 형태로 위임만 한다.

### 상태 객체 수명 관리
- **정적 싱글턴(Flyweight)**: 상태 클래스에 고유 필드가 없으면 `static` 인스턴스 하나를 재사용. 할당·해제 비용 0.
- **동적 생성**: 상태별 데이터가 있으면 전환 시 `new`로 생성, 이전 상태 `delete`. 메서드 실행 중에 자기 자신을 삭제하지 않도록, 핸들러는 새 상태 포인터를 **반환**하고 메인 클래스가 교체한다.

### Enter / Exit 액션
전환 경로가 여러 갈래여도 "이 상태에 들어올 때 항상 실행할 것"을 `enter()`에 한 번만 쓰면 된다(예: 스프라이트 전환, 파티클 시작). 중복 제거.

### 확장 1 — 계층적 FSM (Hierarchical SM)
공통 행동을 슈퍼스테이트로 추출해 상속. "지상 위" 슈퍼스테이트가 점프·덕 전환을 처리하고, Standing/Walking/Running 서브스테이트는 자신의 특수 동작만 처리한다. 입력을 서브스테이트가 처리하지 않으면 슈퍼스테이트에 위임 — OOP 메서드 오버라이드와 구조가 같다.

### 확장 2 — 병렬 FSM (Concurrent SM)
독립적인 관심사(예: "무엇을 하고 있나" vs "무엇을 들고 있나")를 별도 머신으로 분리. 하나의 머신에 n × m 조합을 욱여넣는 대신 n + m 상태로 관리한다.

### 확장 3 — Pushdown Automata
상태를 **스택**으로 쌓는다.
- Push: 새 상태 진입, 이전 상태는 스택에 보존.
- Pop: 현재 상태 종료 후 자동으로 직전 상태로 복귀.
총기 발사 상태처럼 "잠깐 했다가 돌아오는" 행동에 유용. FSM이 갖지 못한 "이력 기억"을 부여한다.

### 언제 쓰지 않나
FSM은 튜링 완전하지 않다. 상태 수가 폭발적으로 늘거나, 복잡한 계획·목표 기반 AI가 필요하면 Behavior Tree나 Planner로 전환을 고려해야 한다.

---

## 우리 프로젝트 적용

### 이미 사용 중

| 위치 | 내용 |
|---|---|
| `02_Server/GameServer/Combat/EnemyState.cs` | `Idle / Patrol / Chase` 열거값 기반 FSM. `UpdateEnemies()` 안 switch로 전환. |
| `02_Server/GameServer/Maps/GameMap.cs` | `UpdateEnemies()` — 상태 전환 로직이 GameMap에 인라인. |
| `03_Client/Assets/Scripts/State/RemoteEntity.cs` | 서버 브로드캐스트로 받은 상태를 클라이언트 보간 대상으로 쓰는 수준. 별도 클라 FSM은 없음. |

### 채택 후보 (M5 God class 분리 시)

- `GameMap.UpdateEnemies()`(`02_Server/GameServer/Maps/GameMap.cs`) → `EnemyFSM` 또는 `EnemyStateBase` 클래스 계층으로 추출. 각 상태(`IdleState`, `PatrolState`, `ChaseState`)가 `update(EnemyEntity&)` + `enter()` + `exit()`를 구현.
- `GameSession.cs`(`02_Server/GameServer/Network/GameSession.cs`) 핸드쉐이크·캐릭터 선택·포탈 마이그레이션 흐름 → 세션 상태 FSM (`Handshaking / CharSelect / InGame / Migrating`) 으로 분리하면 조건 분기 폭발을 막을 수 있다.
- 상태 클래스에 전용 데이터가 없는 경우(Idle, Patrol) Flyweight 정적 인스턴스 재사용으로 GC 압력 감소.

---

## 함정 / 과용 경계

- **과분할**: 상태가 2~3개뿐이고 전환 조건이 단순하면 Enum + switch가 더 읽기 쉽다. 상태 클래스 파일이 늘어나는 비용이 이득보다 클 수 있다.
- **상태 폭발**: 동시에 독립적인 두 축(행동 × 장비)을 하나의 FSM에 넣으면 n × m 조합이 생긴다. 병렬 FSM으로 분리해야 한다.
- **God class 잔존**: `GameMap`처럼 FSM 전환 로직이 God class 안에 인라인으로 남아 있으면 State 패턴의 이득(상태별 캡슐화)을 못 누린다. 추출이 필수.
- **Pushdown 남용**: 이력이 필요 없는 단순 전환에 스택을 쓰면 Pop 타이밍 버그가 생기기 쉽다. "돌아와야 할 명확한 이전 상태"가 있는 경우에만 쓴다.
- **tick 루프 내 동적 할당**: 20 TPS tick 안에서 상태 전환마다 `new`로 생성하면 GC spike 가능. 상태별 전용 데이터가 없으면 Flyweight로 재사용하거나, 오브젝트 풀을 검토한다(헌법 원칙 #5 No Blocking 연계).

---

## 관련

- `02_Server/GameServer/Combat/EnemyState.cs` — 현재 서버의 Idle/Patrol/Chase 열거형. 이 파일의 2단계 패턴 적용 대상.
- 계층적 FSM → OOP 상속과 구조 동일 → 코드 컨벤션 참조.
- Pushdown Automata → 대화 시스템, 메뉴 스택, 포탈 마이그레이션 흐름에 적용 가능.
