---
title: Data Locality
source: Game Programming Patterns
category: Optimization Patterns
---
# [16] 데이터 지역성 (Data Locality)

> CPU 캐시 적중률을 높이기 위해 처리할 데이터를 메모리 상에서 연속 배치하는 최적화 패턴 — Optimization Patterns

---

## 언제 참조하나 (트리거)

- AI/Physics/Render 시스템 루프에서 프레임 타임이 예상보다 길고, 프로파일러에서 cache miss가 병목으로 잡힐 때.
- `GameMap::UpdateEnemies` 같이 수십~수백 개 엔티티를 매 틱 순회하는 루프를 최적화할 때.
- 컴포넌트 분리(Component 패턴) 설계 중 "컴포넌트를 어떻게 저장할까"를 결정할 때.
- 파티클/투사체처럼 활성/비활성 비율이 큰 오브젝트 풀을 구현할 때.

---

## 핵심 내용

### 문제: CPU는 RAM보다 수백 배 빠르다

CPU 연산 속도는 1980년대 이후 기하급수적으로 증가했지만, RAM 접근 속도는 그에 한참 못 미쳤다. RAM에서 데이터 한 바이트를 읽어오는 데 수백 사이클이 걸릴 수 있다. CPU가 데이터를 기다리며 멈추는(stall) 시간이 전체 실행 시간의 상당 부분을 차지한다.

### 해법: 캐시 라인을 최대한 활용한다

CPU가 메모리에서 한 바이트를 읽으면, 주변 64~128 바이트(캐시 라인 한 개 분량)를 통째로 L1 캐시에 올린다. 이 시점에 인접 데이터도 함께 캐시에 올라오므로, 바로 다음 처리 대상이 물리적으로 인접해 있으면 캐시 적중(cache hit)으로 수 사이클 안에 읽힌다. 그렇지 않으면 캐시 실패(cache miss)로 다시 수백 사이클을 대기한다.

**핵심 원칙: 처리 순서대로 데이터를 메모리에 연속 배치한다.**

### 구조 1 — 컴포넌트 배열 분리 (Component Arrays)

OOP 전통 방식:
```
GameEntity → AIComponent*   (힙 어딘가)
           → PhysicsComponent* (힙 어딘가)
```
매 프레임 `entity->ai()->update()` 호출 시 포인터를 두 번 역참조 → 매번 캐시 미스 가능성.

캐시-친화적 방식:
```
AIComponent       aiComponents[MAX];
PhysicsComponent  physicsComponents[MAX];
```
같은 타입을 연속 배열에 모아두고 타입별로 한 번에 순회. 책 벤치마크: **50배 빠름**.

### 구조 2 — 활성/비활성 분리 (Packed Active Array)

파티클·투사체는 활성 수가 수시로 바뀐다.
- 배열 앞부분에 활성 객체를 빽빽이 유지, `numActive_` 카운터로 경계 관리.
- 비활성화 시: 비활성 대상과 마지막 활성 객체를 **스왑** → 앞 구역은 항상 빈 슬롯 없이 꽉 찬 상태.
- 루프가 `[0, numActive_)` 범위만 순회 → 비활성 데이터를 캐시에 올리지 않음, 분기도 없음.

### 구조 3 — Hot/Cold 데이터 분리

컴포넌트 안에서도 매 프레임 쓰는 필드(hot)와 드물게 쓰는 필드(cold)가 섞여 있으면, cold 데이터가 캐시 라인 공간을 낭비한다.

- Hot 구조체: `position`, `velocity`, `animState` 등 매 틱 접근 필드만.
- Cold 구조체: `lootTable`, `questFlag`, `backstoryText` 등 → 포인터로 참조.
- 효과: 캐시 라인 하나에 담기는 객체 수가 늘어나 동일 캐시 라인으로 더 많은 엔티티 처리.

### 언제 쓰나

- 프로파일러로 **cache miss가 병목임을 확인한 후**.
- 매 프레임 수십~수백 개 동형 객체를 순회하는 tight loop.
- 싱글 스레드 혹은 코어별로 독립된 데이터 구역이 명확할 때.

### 언제 쓰지 않나

- 해당 코드가 성능 병목이 아닐 때 (측정 먼저).
- 객체 수가 적어서 캐시 미스 영향이 무시할 수 있을 때.
- 멀티스레드로 공유 캐시 라인을 여럿이 동시에 건드리는 구조(False Sharing 위험 역방향).

---

## 우리 프로젝트 적용

### 서버 — 이미 사용 중 (부분)

| 클래스 / 파일 | 현황 |
|---|---|
| `02_Server/GameServer/Maps/GameMap.cs` `UpdateEnemies()` | `_enemies` Dictionary를 Values 순회 → Dictionary 내부 버킷 구조는 연속 보장 X. 엔티티 수가 적어 현재는 문제없지만, 맵당 적 수가 늘면 List로 전환 + 활성 분리 고려 지점. |
| `02_Server/GameServer/Combat/EnemyEntity.cs` | 매 틱 접근하는 `Position/Velocity/State`와 드물게 접근하는 `DropTable/SpawnData` 가 같은 클래스에 혼재. 엔티티 수가 많아지면 Hot/Cold 분리 후보. |
| `02_Server/GameServer/Maps/PlayerEntity.cs` lag compensation ring buffer | 4틱 위치 히스토리를 배열로 유지 — 이미 연속 메모리. Data Locality 원칙에 부합. |

### 클라이언트 (Unity) — 채택 후보

| 클래스 / 파일 | 현황 |
|---|---|
| `03_Client/Assets/Scripts/Combat/EnemyRegistry.cs` | `Dictionary<uint, RemoteEntity>` → RemoteEntity 각각이 힙 여기저기 산재. 인터폴레이션 루프 매 프레임 포인터 chase. 엔티티 수 증가 시 `RemoteEntity[]` 배열 + index-by-id 테이블 구조로 전환 가치 있음. |
| Unity DOTS / Burst | Unity 6은 Entities + Burst Compiler가 Data Locality를 언어 레벨에서 강제하는 ECS 제공. 현재 프로젝트는 클래식 MonoBehaviour 기반이라 **현재 무관**이지만, 성능 병목 시 Burst Job으로 이동 경로 존재. |

### 공통 원칙 적용 포인트

- 서버 틱 루프(`TickScheduler` → `GameMap.Tick()`)는 20TPS fixed-rate. 루프 안에서의 alloc/pointer-chase 최소화가 헌법 절대원칙 #5(No Blocking)와 같은 방향.
- 지금 당장 리팩터 대상 아님 — God class 분리(M4.4 방향)와 컴포넌트 배열 패턴은 **설계 방향이 같다**. God class를 시스템별 클래스로 쪼갤 때 데이터도 시스템별 배열로 모으면 캐시 이득은 자연히 따라온다.

---

## 함정 / 과용 경계

- **측정 없이 먼저 바꾸지 않기.** 직관은 자주 틀린다. Cachegrind, dotnet-trace, Unity Profiler로 실측 후 적용.
- **OOP 추상화와 정면 충돌.** virtual 메서드 = vtable 포인터 역참조 = 캐시 미스. 다형성이 필요한 곳에서 이 패턴을 억지로 쓰면 코드 복잡도만 증가.
- **배열 내 스왑 = 참조 무효화.** Packed Active Array에서 스왑하면 인덱스로 참조하던 외부 코드가 깨진다. ID→index 간접 테이블이 필요하고 그 자체가 캐시 미스 원인이 될 수 있다.
- **조기 최적화 경계.** 엔티티 수십 개 수준에서는 50x 이득이 아니라 1.05x 이득일 수 있다. God class 분리·가독성·테스트 가능성이 현재 단계에서 더 중요.
- **멀티스레드 + 공유 캐시 라인 = False Sharing.** 두 스레드가 같은 캐시 라인의 다른 필드를 동시에 쓰면 오히려 느려진다. 서버 맵 Actor(단일스레드)는 이 문제가 없지만, 추후 병렬 맵 처리 시 주의.

---

## 관련

- [[13-component]] — 컴포넌트 패턴과 자연스럽게 결합. 컴포넌트를 타입별 배열에 저장하면 Data Locality 효과 직접 달성.
- [[09-update-method]] — Update 루프 구조가 Data Locality의 주요 적용 지점.
- [[12-type-object]] — 가상 함수 없는 다형성 → vtable miss 회피와 같은 방향.
- [[18-object-pool]] — Packed Active Array는 오브젝트 풀 + Data Locality의 결합.
