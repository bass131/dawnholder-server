---
title: Observer (관찰자)
source: Game Programming Patterns
category: Design Patterns Revisited
---
# [GPP-03] Observer (관찰자 패턴)
> 한 객체(Subject)가 상태를 변경할 때, 그 객체에 등록된 다른 객체들(Observers)에게 자동으로 알림을 보내는 패턴. 서로 무관한 시스템끼리 직접 의존 없이 소통하게 해준다.

---

## 언제 참조하나 (트리거)

- "업적 시스템이 전투/물리/AI 등 여러 시스템 이벤트를 감지해야 한다"는 요구가 생길 때
- A 시스템이 B 시스템의 내부를 직접 참조하지 않고도 B의 상태 변화를 받아야 할 때
- 한 이벤트를 여러 시스템(사운드, HUD, 로그, 업적)이 동시에 처리해야 할 때
- God class가 "이벤트 발생 → 직접 호출"을 여러 도메인에 걸쳐 하드코딩하고 있을 때

---

## 핵심 내용

### 해결하는 문제
물리 엔진 안에 업적 코드를, 전투 코드 안에 UI 갱신 코드를 박으면 두 시스템이 강하게 결합된다. 하나를 바꿀 때 다른 것도 건드려야 하고, 테스트도 함께 끌려온다.

### 구조

- **Observer 인터페이스**: `onNotify(entity, event)` 순수 가상 메서드 하나. 알림을 받는 쪽이 구현.
- **Subject 클래스**: Observer 포인터 목록을 보유. `addObserver()` / `removeObserver()`로 구독 관리. 이벤트 발생 시 `notify()`를 호출해 목록을 순회하며 각 Observer의 `onNotify()`를 동기 호출.
- **알림 흐름**: Subject → (목록 순회) → Observer.onNotify() → (각자 처리). 큐나 힙 할당 없이 가상 함수 호출만 발생.

### 주요 변형

1. **연결 리스트(Intrusive Linked List)**: Observer 객체 자신이 `next_` 포인터를 들고 리스트 노드가 된다. 동적 할당 제로, 단 Observer 하나가 Subject 하나만 관찰 가능.
2. **노드 풀(Non-intrusive Node Pool)**: 별도 노드 객체가 Observer 포인터를 감싼다. 동적 할당 없이 Observer 하나가 여러 Subject 관찰 가능. 구현 복잡도 증가.
3. **상속 대신 컴포지션**: Subject를 상속하지 말고 멤버로 보유. `physics.entityFell().addObserver(this)` 형태로 Subject를 이벤트별로 세분화.
4. **함수 포인터 / 클로저**: Observer 인터페이스 대신 함수 포인터, delegate, 람다로 교체. C# `event` / `Action<>` 가 이에 해당.

### 성능 현실

- **"너무 느리다" — 대부분 오해**: 알림 전송은 목록 순회 + 가상 함수 호출뿐. 핫 루프(틱 루프)가 아니라면 무시 가능한 비용.
- **동기 차단**: 메시지 큐와 달리 Subject가 Observer 처리 완료까지 블로킹된다. Observer는 빠르게 끝내거나 무거운 작업을 큐에 위임해야 한다.
- **등록 단계만 할당**: 알림 발송 시 할당 없음. 게임 초기화 때 Observer 목록을 미리 구성하면 런타임 단편화 없음.

### 함정: 수명 관리

- **댕글링 포인터**: Observer 삭제 후 Subject 목록에 포인터가 남으면 다음 알림에서 크래시.
- **좀비 관찰자**: Subject 삭제 후 Observer가 계속 알림을 기대.
- **Lapsed Listener**: GC 언어에서도 Subject 목록에 참조가 남아 있으면 UI 등 이미 닫힌 객체가 GC 안 되고 계속 이벤트를 처리.

### 정적 분석 불가 문제

Observer 호출은 동적이라 IDE가 "어떤 Observer가 호출될지" 정적 추적 불가. 디버깅 시 런타임에 목록 내용을 직접 확인해야 한다.

---

## 우리 프로젝트 적용

### 이미 사용 중 (암묵적)

- **GameSession → 패킷 핸들러 인라인**: 현재 `02_Server/GameServer/Network/GameSession.cs`가 수신 패킷마다 switch-case로 직접 로직을 인라인 호출. 이는 Observer 패턴 없이 Subject(세션)가 Observer 역할(핸들러 로직)을 직접 들고 있는 God class 형태.
- **Unity 클라이언트 `UnityClientSession`(665줄)**: 서버 Handlers/ 패턴 미러가 아직 안 된 상태. 동일 문제.

### 채택 후보 — God class 분리 작업 시 핵심 수단

**GameMap → System 추출 구조에서**:
- `GameMap`이 `ProcessAttack` / `UpdateEnemies` / `RespawnEnemy` 등을 직접 호출하는 대신, 이벤트(예: `EnemyDied`, `PlayerAttacked`)를 `notify()`해서 `CombatSystem`, `RespawnSystem`, `AchievementSystem`(미래)이 구독하는 방식으로 분리 가능.
- 단, 20TPS 틱 루프 안이면 Observer 순회 비용 + 동기 차단 주의. 이벤트 발생이 틱당 소수면 무시 가능.

**업적 시스템 (미래 M5+)**:
- 책의 예시와 동일 구조. `PlayerEntity`, `EnemyEntity`, `GameMap`이 Subject가 되고 `AchievementSystem`이 Observer로 등록. 현재 코드에 achievement 로직이 전혀 없기 때문에 도입 시점에 자연스럽게 적용 가능.

**HUD / 피해 수치 표시 (클라이언트)**:
- `PlayerEntity`나 `RemoteEntity`가 HP 변화를 Subject로 notify → `HudController`가 Observer로 갱신. 현재 `HudController`는 mock이므로 실제 HP wiring 시 적용.

### C# 언어 수준 지원

C# `event` + `Action<>` / `EventHandler<>` 가 Observer 패턴의 언어 내장 구현이다. 새 시스템 작성 시 인터페이스 직접 구현보다 C# event를 우선 고려 (delegate 기반이라 타입 안전 + IDE 추적 가능).

---

## 함정 / 과용 경계

- **틱 루프 안 남발 금지**: 매 틱마다 수십 개 이벤트를 notify하면 가상 함수 오버헤드가 쌓인다. 틱 내부 빈번 상태 변화는 직접 호출이 낫고, Observer는 틱 경계나 게임 이벤트(사망, 레벨업, 포털) 단위로만.
- **순환 알림 경계**: Observer의 onNotify 안에서 Subject를 다시 notify하면 무한 루프. 재진입 방지 플래그 필요.
- **Observer 수명 = Subject 수명**: Observer를 등록하면 반드시 해제 책임을 명확히 해야 한다. Destructor에서 unregister 안 하면 댕글링 크래시.
- **"긴밀한 한 기능 내부"에선 과설계**: 같은 클래스/모듈 내부에서 자기 자신의 메서드 여러 개를 연결하려고 Observer를 쓰는 것은 과분할. 같은 기능 안이면 직접 호출이 훨씬 단순.
- **디버깅 비용 과소평가 금지**: 콜스택에 Observer 체인이 끼면 "어디서 왔는지" 추적이 어렵다. 이벤트 ID + 송신자 로깅을 Observer 인프라에 처음부터 넣어두면 디버깅 비용이 크게 줄어든다.

---

## 관련

- [[01-command]] — Command 패턴과 함께 쓰면 "무엇이 일어났는지"를 큐에 쌓고 나중에 Observer에 재생 가능 (리플레이, 언두).
- [[14-event-queue]] — Observer가 동기 블로킹이라면 Event Queue는 비동기 디커플링. 틱 루프 안에서 무거운 Observer 로직이 생기면 Event Queue로 전환 고려.
