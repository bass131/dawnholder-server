---
title: Component
source: Game Programming Patterns (Robert Nystrom)
category: Decoupling Patterns
---
# [GPP-13] 컴포넌트 (Component)

> 한 엔티티가 여러 도메인(입력·물리·렌더·AI·사운드)에 걸칠 때, 각 도메인을 독립 컴포넌트 클래스로 분리해 단일 거대 클래스 없이 조합한다. — Decoupling Patterns

---

## 언제 참조하나 (트리거)

- `GameMap`(665줄) 또는 `UnityClientSession`(665줄)처럼 한 클래스가 전투/AI/물리/네트워크 등 여러 도메인을 동시에 건드리는 God class 분리 작업을 시작할 때.
- 새 오브젝트 타입을 만들 때 "이거 상속으로 해야 하나?"라는 고민이 드는 순간 — 인터페이스 상속 대신 컴포넌트 조합이 답인지 판단 기준이 필요할 때.
- 멀티스레드(맵별 Actor 스레드) 환경에서 도메인 간 불필요한 락 없이 분리된 시스템을 실행하고 싶을 때.
- AI(Idle/Patrol/Chase FSM)와 전투 로직이 같은 루프에서 뒤섞여 수정할 때마다 다른 쪽이 깨질 때.

---

## 핵심 내용

### 문제: 도메인 교차 God class

게임 오브젝트가 "입력 → 물리 → 렌더 → AI → 사운드"를 한 클래스 안에서 처리하면 세 가지 문제가 발생한다.

1. **규모**: 클래스가 수백~수천 줄로 비대해져 작은 변경도 위험해진다.
2. **결합**: 물리 코드가 렌더 상태를 참조하는 식으로 도메인들이 얽혀, 한 쪽을 바꾸려면 다른 쪽을 전부 이해해야 한다.
3. **병렬화 불가**: 도메인별로 스레드를 나누고 싶어도 공유 상태가 뒤섞여 있어 락 없이는 분리하기 어렵다.

### 해법: 도메인별 컴포넌트로 분해

컨테이너 객체는 "컴포넌트들을 묶는 얇은 껍데기"로만 남기고, 실제 로직과 데이터는 각 컴포넌트가 소유한다.

```
GameObject (얇은 껍데기 — velocity, x, y 등 공유 상태만)
  ├── InputComponent  — 컨트롤러 → velocity 변환
  ├── PhysicsComponent — velocity → position 변환 + 충돌
  └── GraphicsComponent — position + velocity → 스프라이트 선택·렌더
```

- 각 컴포넌트는 자기 도메인 데이터 + 동작을 캡슐화한다.
- 컨테이너는 `update()`에서 컴포넌트들을 순서대로 호출하기만 한다.

### 인터페이스 추상화로 교체 가능성 확보

컴포넌트를 순수 가상(인터페이스) 뒤에 숨기면 런타임에 교체할 수 있다.

```
InputComponent (abstract)
  ├── PlayerInputComponent  — 실제 조이스틱 입력
  └── DemoInputComponent    — AI가 조종하는 데모 모드
```

컨테이너는 `InputComponent*`만 알고, 어떤 구현체인지 모른다. 생성 시 어떤 구현을 주느냐로 동작이 완전히 바뀐다.

### 컴포넌트 간 통신 — 세 가지 전략

| 전략 | 방법 | 장점 | 단점 |
|---|---|---|---|
| **공유 컨테이너 상태** | 컨테이너의 공용 필드(velocity, x, y)를 읽고 씀 | 단순, 빠름 | 실행 순서 의존, 컨테이너에 도메인 상태 누적 |
| **직접 참조** | 컴포넌트가 형제 컴포넌트 포인터를 직접 보유 | 명시적, 직접 질의 가능 | 컴포넌트 쌍 간 결합 발생 |
| **메시지 패싱** | 컨테이너를 통해 이벤트 브로드캐스트 | 결합 없음, 수신자가 누군지 몰라도 됨 | 구현 복잡, 흐름 추적 어려움 |

현실에서는 세 가지를 혼용한다. 속도가 중요한 공유 데이터는 공유 상태, 드문 이벤트는 메시지, 꼭 직접 연결해야 할 쌍에만 직접 참조.

### 컴포넌트 조립 — 두 가지 방식

- **자체 생성 (Hard-coded)**: 컨테이너가 생성자에서 직접 컴포넌트를 new. 구성이 확실하지만 유연성 없음.
- **외부 주입**: 생성자 파라미터나 팩토리로 바깥에서 컴포넌트를 넘겨줌. 조합이 자유롭고 인터페이스 추상화와 시너지. 잘못된 조합을 막으려면 팩토리 메서드를 함께 쓴다.

### 상속보다 컴포넌트 조합이 나은 이유

```
(상속) Zone(충돌만) / Decoration(렌더만) / Prop(둘 다 필요)
  → Prop이 Zone과 Decoration 동시 상속 불가 — 다중 상속 또는 코드 중복
  
(컴포넌트) 모두 GameObject
  Zone       = GameObject + PhysicsComponent
  Decoration = GameObject + GraphicsComponent
  Prop       = GameObject + PhysicsComponent + GraphicsComponent
  → 중복 없음, 다중 상속 없음
```

### 언제 쓰나 / 언제 안 쓰나

**쓸 때**: 클래스가 2개 이상의 도메인을 건드리고, 각 도메인을 독립적으로 수정·테스트하고 싶을 때. 오브젝트 타입 조합이 폭발적으로 늘어나는 경우.

**쓰지 말 때**: 책임이 단순하고 도메인 교차가 없는 작은 클래스. 컴포넌트 포인터 역참조 오버헤드가 성능 임계 루프에서 허용 불가일 때(단, 데이터 지역성 패턴과 결합하면 완화 가능).

---

## 우리 프로젝트 적용

### 이미 사용 중 (Unity 클라이언트)

Unity의 GameObject + MonoBehaviour 시스템 자체가 컴포넌트 패턴의 구현체다. `PlayerController`, `HudController`, `PlayerPredictor`, `RemoteEntity` 모두 GameObject에 붙는 컴포넌트들이다.

### 채택 후보 — GameMap God class 분리 (서버, M5+ 우선 과제)

`02_Server/GameServer/Maps/GameMap.cs`가 현재 단일 클래스 안에 다음을 모두 포함한다.

| 현재 위치 | 분리 후보 클래스 | 담당 도메인 |
|---|---|---|
| `ProcessAttack()` | `CombatSystem` | 전투·히트 판정·데미지 |
| `UpdateEnemies()` + FSM | `EnemyAISystem` | AI FSM (Idle/Patrol/Chase) |
| `BroadcastXxx()` | `MapBroadcaster` | 패킷 브로드캐스트 |
| respawn 타이머 | `RespawnSystem` | 리스폰 스케줄링 |

분리 방향: `GameMap`은 컨테이너(얇은 껍데기)로 남기고, 각 System이 `GameMap` 레퍼런스(공유 상태)를 받아 독립적으로 `Update()`를 처리. 컴포넌트 간 통신은 우선 "공유 컨테이너 상태" 전략(GameMap 필드)으로 단순하게 시작, 이벤트가 필요한 시점에 메시지 패싱 추가.

### 채택 후보 — UnityClientSession God class 분리 (클라이언트)

`03_Client/Assets/Scripts/Network/UnityClientSession.cs` (665줄)도 패킷 핸들러 12개 인라인. 서버 `02_Server/GameServer/Handlers/` 폴더 패턴 미러가 목표. 핸들러 클래스를 컴포넌트처럼 분리하면 각 패킷 도메인(이동·전투·인벤토리·포털)이 독립적으로 수정 가능.

### 현재 무관

`02_Server/GameServer/Loop/TickScheduler.cs`, `02_Server/Network/RecvBuffer.cs`/`SendBuffer.cs`, `99_Tools/` PacketGenerator — 단일 책임을 이미 갖고 있어 추가 분리 불필요.

---

## 함정 / 과용 경계

- **과분할**: 컴포넌트 하나가 10줄짜리면 오히려 역추적 비용이 더 크다. 최소 기준: 컴포넌트 단독으로 테스트할 가치가 있는 논리 단위인가?
- **공유 상태 무한 팽창**: 컴포넌트들이 "조금씩만 쓰겠다"며 컨테이너에 필드를 계속 추가하면 컨테이너가 다시 God class가 된다. 공유 상태는 *컴포넌트 간 통화(currency)*이지 쓰레기통이 아님.
- **순서 의존 묵시**: 공유 상태 전략은 컴포넌트 실행 순서에 암묵적으로 의존한다. Input → Physics → Graphics 순서가 뒤집히면 같은 프레임에서 이전 프레임 데이터를 읽게 된다. 순서를 주석이나 상수로 명시할 것.
- **ECS 조기 도입**: Entity Component System(엔티티=ID만, 컴포넌트=별도 배열)은 데이터 지역성·병렬화 극대화지만 복잡도가 크게 오른다. `GameMap` God class 분리 수준에서는 간단한 "컨테이너 + System 클래스" 형태로 충분하다.
- **서버 틱 루프 헌법 #5 주의**: 분리된 System.Update()도 틱 루프 안에서 호출되면 `await`·`Thread.Sleep`·동기 DB 호출 금지 원칙이 그대로 적용된다.

---

## 관련

- [[14-event-queue]] — 컴포넌트 간 메시지 패싱을 비동기로 확장할 때 이벤트 큐와 결합.
- 데이터 지역성 (Data Locality) — 컴포넌트 배열을 타입별로 묶어 캐시 효율을 높이는 최적화(ECS의 기반).
- [[09-update-method]] — 컨테이너가 컴포넌트들의 Update()를 순서대로 호출하는 구조는 Update Method 패턴과 직접 연결.
