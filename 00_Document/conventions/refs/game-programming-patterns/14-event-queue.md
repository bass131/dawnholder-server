---
title: Event Queue
source: Game Programming Patterns (Robert Nystrom)
category: Decoupling Patterns
---
# [GPP-14] 이벤트 큐 (Event Queue)

> 요청의 **발신 시점**과 **처리 시점**을 분리해 비동기 배치 처리, 집계, 멀티스레드 안전성을 확보하는 패턴. 분류: Decoupling Patterns.

---

## 언제 참조하나 (트리거)

- 게임 루프(tick loop)에서 어떤 호출이 현재 프레임을 블로킹하거나 예외 종료를 유발한다.
- 한 프레임 안에 같은 종류의 요청이 중복 발생해 집계(de-duplication)가 필요하다.
- 작업을 다른 스레드(예: 오디오, 네트워크 I/O)에 넘겨야 하는데 락 범위를 최소화하고 싶다.
- "누가 보냈는지"가 아니라 "언제 처리할지"를 제어해야 하는 상황.

---

## 핵심 내용

### 문제

동기 API(`playSound()` 같은 호출)는 세 가지 문제를 동시에 가진다.

1. **블로킹**: 리소스 로딩 등 무거운 작업이 호출자 스레드를 멈춘다 → 프레임 드랍.
2. **집계 불가**: 같은 프레임에 동일 사운드가 10번 요청되면 10배 볼륨으로 재생됨.
3. **스레드 불일치**: 처리에 적합한 스레드가 아닌, 호출자 스레드에서 실행된다.

근본 원인: **즉시성(immediacy)**. 발신자에게 편한 시점과 수신자가 처리하기 편한 시점이 다르다.

### 해법 구조

발신자는 **큐에 넣고 즉시 반환**. 처리자는 자신의 페이스로 큐에서 꺼내 처리.

```
[Sender] --enqueue--> [Ring Buffer Queue] --dequeue--> [Processor / update()]
```

**링 버퍼(Ring Buffer)** 구현:
- 고정 크기 배열 + `head`(읽기 위치) + `tail`(쓰기 위치) 두 포인터.
- 두 포인터 모두 배열 크기로 wrap-around (모듈로 연산).
- `(tail + 1) % MAX == head`이면 꽉 찬 상태 → assert 또는 드롭 처리.
- 동적 할당 없음, 연속 메모리 → 캐시 친화적.

### 집계(Aggregation) 예시

큐에 삽입 전 동일 ID 항목이 있으면 볼륨 최댓값으로 병합하고 반환. 중복 항목 자체가 큐에 들어가지 않는다. 단, 큐 전체를 순회하므로 큐가 길면 O(n) 비용 — 큰 큐에서는 해시테이블을 별도로 두는 게 낫다.

### 변형 분류

| 차원 | 옵션 | 특징 |
|---|---|---|
| 큐에 담는 것 | **이벤트**(Event, 과거 사실) | "몬스터 죽었다" — 다수 리스너, 브로드캐스트 |
| | **메시지**(Message, 미래 요청) | "사운드 재생해" — 단일 수신자, 캡슐화 강 |
| 읽는 쪽 | Single-cast | 지정된 서비스만 읽음 (오디오 예시) |
| | Broadcast | 모든 리스너에게 전달, 필터링 필요 |
| | Work queue | 리스너 여럿 중 하나에게만 라우팅 |
| 쓰는 쪽 | 단일 송신자 | 묵시적 발신자 정보 |
| | **다중 송신자** | 메시지 안에 발신자 정보 직접 포함 필요 |
| 객체 수명 | 소유권 이전 | unique_ptr 방식, 큐가 소멸 책임 |
| | 공유 소유권 | shared_ptr 방식 |
| | 큐 소유(오브젝트 풀) | 미리 할당된 슬롯에 발신자가 채움 |

---

## 우리 프로젝트 적용

### 이미 사용 중

**`02_Server/Network/JobQueue.cs` (서버 잡 큐)**
- 각 맵(Actor)이 외부에서 들어오는 작업을 `JobQueue`에 enqueue → 맵 틱 루프가 dequeue해서 처리.
- Event Queue 패턴의 **Work Queue 변형**. 다중 송신자(세션들) → 단일 처리자(맵 틱).
- 덕분에 맵 내부 lock 없이 단일 스레드 실행 보장 (헌법 원칙 #5 준수).

**`02_Server/GameServer/Network/GameSession.cs` — 패킷 수신 큐**
- IOCP RecvBuffer에서 완성된 패킷이 파싱되어 잡 큐로 전달 → 처리.
- 여기도 "수신 시점"과 "처리 시점" 분리 = Event Queue 철학 그대로.

### 채택 후보

**`02_Server/GameServer/Maps/GameMap.cs` — 브로드캐스트 이벤트 큐 (M4.3/M4.4 범위)**

현재 `GameMap`은 `ProcessAttack`, `UpdateEnemies`, `Broadcast` 로직이 한 메서드 안에 혼재. 개선 방향:

- `BroadcastQueue` 또는 `EventBus`를 맵 수준에 도입해 "HP 변경됨", "몬스터 죽었다", "포탈 진입" 등을 이벤트로 발행.
- 각 Handler(BroadcastHandler, DeathHandler, RespawnHandler)가 이벤트를 구독해 처리.
- 틱 loop 안에서 큐를 drain → 단일 스레드 유지, 헌법 원칙 #5 무위반.

**`02_Server/GameServer/Maps/GameMap.cs` — 리스폰 지연 큐**

현재 리스폰은 `DateTime` 비교로 매 틱 폴링. 리스폰 이벤트를 타임스탬프와 함께 큐에 삽입 → `update()`에서 만료된 것만 처리하면 폴링 비용 제거.

**`03_Client/Assets/Scripts/Network/UnityClientSession.cs` — 서버 패킷 이벤트 큐 (UnityClientSession 리팩토링 시)**

`UnityClientSession`(665줄 God class) 분리 시, 수신 패킷을 이벤트 큐에 넣고 각 도메인 Handler(HpChangedHandler, MoveHandler 등)가 Unity 메인 스레드에서 drain하는 구조로 전환 가능. 스레드 경계 안전성도 동시에 확보.

### 현재 무관

UI 이벤트(버튼 클릭 등) — Unity의 UnityEvent/C# event가 이미 Observer 패턴으로 처리. Event Queue 추가 레이어 불필요.

---

## 함정 / 과용 경계

**전역 상태 위험**: 중앙 이벤트 큐는 사실상 전역 변수. 코드베이스 어디서든 쓸 수 있어서 "모든 것이 모든 것과 결합"하는 거대한 의존 그래프가 생길 수 있다. Nystrom 본인도 "treat simplicity as a precious resource"라고 경고.

**이벤트 처리 중 이벤트 발행 — 피드백 루프**: 핸들러 A가 이벤트를 처리하면서 다시 이벤트를 발행 → B가 처리 → A 이벤트 재발행 → 무한 루프. 동기 방식이라면 스택 오버플로로 바로 터지지만, 큐가 있으면 게임이 계속 실행되면서 조용히 이벤트가 폭발한다. 대응: 핸들러 내부에서 새 이벤트 발행 금지 or 디버그 로그로 사이클 감지.

**상태 탈동기화(State Staleness)**: 이벤트가 큐에 머무는 동안 게임 월드가 바뀐다. 이벤트에서 참조하는 엔티티가 삭제되거나 위치가 달라질 수 있다. 이벤트 발행 시점의 스냅샷 데이터를 이벤트 구조체에 직접 포함시켜야 한다 (엔티티 포인터/참조 대신 ID + 필요 데이터 값).

**집계 비용**: 삽입마다 큐 전체를 순회하는 de-duplication은 큐가 크면 O(n). 큐 크기를 작게 유지하거나 별도 해시셋으로 O(1) 관리.

**Observer/Command와 혼동**: 단순히 "누가 수신하는지"를 분리하고 싶다면 Observer로 충분. Event Queue는 "언제 처리하는지"를 추가로 제어해야 할 때만 필요. 섣불리 도입하면 불필요한 비동기 복잡도.

**멀티스레드 큐 구현 복잡도**: `playSound()`(enqueue)와 `update()`(dequeue)가 다른 스레드에 있으면 락이 필요하다. enqueue는 짧게, dequeue는 condition variable로 busy-wait 방지. 잘못 구현하면 오히려 더 느려진다.

---

## 관련

- [[03-observer]] — 수신자 분리가 목적이면 이쪽이 더 단순.
- [[01-command]] — 요청을 객체로 만드는 것은 같지만, 실행 시점 제어 없음.
- [[08-game-loop]] — 큐를 drain하는 `update()` 호출 시점은 게임 루프가 결정.
- [[18-object-pool]] — 큐 내부 메시지 객체의 동적 할당 없애는 데 조합 가능.
