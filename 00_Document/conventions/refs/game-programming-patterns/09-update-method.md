---
title: Update Method
source: Game Programming Patterns (Robert Nystrom)
category: Sequencing Patterns
---
# [GPP-09] Update Method (업데이트 메서드)
> 게임 세계의 각 엔티티에 "매 프레임 한 조각씩 행동"을 위임하는 분류: 시퀀싱 패턴.

## 언제 참조하나 (트리거)
- 복수의 엔티티(몬스터, 플레이어, 이펙트)를 게임 루프 안에서 독립적으로 매 틱 갱신해야 할 때
- 새 적 AI FSM(Idle/Patrol/Chase) 또는 상태 머신을 추가할 때 — `UpdateEnemies` 구조 결정 시
- `GameMap.cs` 또는 `EnemyEntity`의 틱 처리 로직을 리팩토링·분리할 때
- 엔티티 활성/비활성 관리나 리스트 수정 중 iteration 버그를 추적할 때

## 핵심 내용
- **문제**: 여러 엔티티가 각자의 행동(순찰, 공격, 애니메이션)을 동시에 수행하는 것처럼 보여야 한다. 하지만 엔티티별로 `while(true)` 무한 루프를 쓰면 다른 모든 처리(입력, 렌더링)가 블로킹된다.
- **해법**: 각 엔티티가 `Update()` 메서드 하나를 구현한다. 이 메서드는 "한 프레임 분량의 행동"만 수행하고 즉시 반환한다. 게임 루프가 매 프레임 전체 엔티티 컬렉션을 순회하며 각각의 `Update()`를 호출한다.
- **상태 보존**: 행동 진행 상태(순찰 방향, 쿨다운 카운터 등)는 엔티티 인스턴스 필드에 보관한다. `Update()`가 돌아올 때 스택이 사라지기 때문에 지역 변수로는 프레임 간 상태를 유지할 수 없다.
- **순차 실행 vs. 동시 실행처럼 보이기**: 실제로는 A → B → C 순으로 순차 호출되지만, 매 프레임 전부가 갱신되므로 플레이어 눈에는 동시에 움직이는 것처럼 보인다. 단, A가 업데이트될 때는 B의 이전 프레임 상태를, B가 업데이트될 때는 A의 현재 프레임 상태를 보는 비대칭이 존재한다.
- **`update()` 배치 위치 선택지**:
  - Entity 기반 클래스 직접 — 가장 단순하지만 상속 계층이 깊어지면 관리 어려움.
  - Component 클래스 위임 — 조합 우선 설계, 현대적 권장 방식.
  - State/Type Object 위임 — 행동을 교체 가능한 객체로 분리, 런타임 전환 가능.
- **가변 타임스텝**: `Update(double elapsed)`처럼 경과 시간을 넘기면 프레임률 독립적 이동이 가능하지만, 경계 오버슈팅 같은 엣지케이스 처리가 필요해진다.
- **리스트 수정 위험**: `Update()` 도중 엔티티 추가/제거 시 이터레이션이 깨진다. 루프 시작 전 카운트를 캐싱하거나 역방향 순회로 방어한다.
- **비활성 엔티티 비용**: 단일 컬렉션에 활성/비활성 혼합이면 매 프레임 플래그 체크 + 캐시 미스 유발. 활성 전용 별도 컬렉션이 비활성 엔티티가 많을 때 유리하다.
- **관련 패턴 의존**: Game Loop가 없으면 동작하지 않는다. Data Locality 패턴으로 배열-of-struct 레이아웃 개선 시 캐시 효율이 극대화된다.

## 우리 프로젝트 적용

### 이미 사용 중

| 위치 | 현황 |
|---|---|
| `02_Server/GameServer/Maps/GameMap.cs` | `UpdateEnemies()` 내부에서 `EnemyEntity.Update()`를 순회 호출 — Update Method의 서버측 구현체 |
| `02_Server/GameServer/Combat/EnemyEntity.cs` | `EnemyState` FSM(Idle/Patrol/Chase)이 Update Method로 구동됨 |
| `02_Server/GameServer/Loop/TickScheduler.cs` | 20TPS 틱마다 `GameMap.Update()` 호출 — Game Loop 역할 |
| `03_Client/Assets/Scripts/` | `MonoBehaviour.Update()` — Unity 엔진이 동일 패턴을 프레임마다 자동 호출 |

### 개선 후보

- `02_Server/GameServer/Maps/GameMap.cs`의 `UpdateEnemies`, `ProcessAttack`, `respawn` 로직은 현재 한 메서드에 혼재. Update Method 책임을 제대로 분리하면 `EnemyAISystem.Update()`, `CombatSystem.Update()`, `RespawnSystem.Update()` 등 System 단위로 추출 가능 (M4.3 God class 분리 과제).
- `target rewind` 미적용 상태(M4.4 이월). lag compensation에서 공격자 rewind는 완료, 피격 대상 rewind도 `02_Server/GameServer/Combat/EnemyEntity.cs`의 `Update()` 맥락에서 position history를 활용해야 함.
- 클라이언트 `03_Client/Assets/Scripts/Network/UnityClientSession.cs`(665줄)도 패킷 핸들러가 inline — 서버 `02_Server/GameServer/Handlers/` 패턴 미러 시 Update Method 책임 분리 함께 검토.

## 함정 / 과용 경계

- **turn-based 게임에서 강제 적용 금지**: 이벤트 기반으로 움직이는 시스템(체스, 텍스트 어드벤처)에 매 프레임 `Update()`를 돌리면 CPU를 낭비하는 busy-wait이 된다. 우리 프로젝트에서도 UI 전용 객체나 DB 작업 큐처럼 틱과 무관한 객체에는 적용하지 않는다.
- **`Update()` 안에서 블로킹 금지 (헌법 #5)**: 우리 틱 루프는 50ms 예산을 가진다. `await Task.Delay`, 동기 DB 호출, `Thread.Sleep`은 전체 틱을 멈추므로 절대 금지. 비동기 작업은 큐에 올리고 틱 밖에서 처리.
- **단일 Update()에 너무 많은 책임 집중 금지**: `GameMap.UpdateEnemies()`처럼 AI + 전투 + 리스폰이 한 메서드에 들어가면 God class 증상이다. 역할별 System으로 분리해 각 System이 자신의 Update()만 담당하게 한다.
- **Update 순서 의존 버그**: 엔티티 A의 Update 결과를 B가 같은 프레임에 즉시 읽어야 하는 로직은 순서가 바뀌면 버그가 된다. 이 패턴은 "같은 프레임의 이전 상태 기준 갱신"을 암묵적 계약으로 한다 — 순서 의존이 생기면 이중 버퍼(Double Buffer) 패턴 검토.
- **리스트 순회 중 수정 버그**: 몬스터가 죽으면서 리스트에서 제거될 때 인덱스 skipping이 발생한다. 역방향 순회 또는 "삭제 예약 → 루프 후 일괄 제거" 패턴으로 방어한다.
- **과도한 컴포넌트 분해**: 모든 행동을 개별 Component로 쪼개면 각 컴포넌트가 Update() 호출을 유발해 가상 함수 오버헤드가 누적된다. 학습 목적 프로젝트에서는 단순성과 캐시 효율 균형을 맞추는 것이 우선.

## 관련
- [[08-game-loop]] — Update Method가 실행되는 외부 프레임워크. 없으면 Update Method 자체가 동작하지 않는다.
- [[13-component]] — `update()` 책임을 Entity 상속 계층 대신 Component로 위임하는 현대적 대안.
- [[06-state]] — FSM 상태 객체 안에 `update()` 로직을 캡슐화. `EnemyState`가 이 조합을 사용 중.
- [[07-double-buffer]] — Update 순서 의존이 생길 때 "이번 프레임 쓰기 / 이전 프레임 읽기"로 일관성 보장.
- [[16-data-locality]] — 엔티티 배열을 SOA(Structure-of-Arrays)로 정렬해 Update 루프의 캐시 효율 극대화.
