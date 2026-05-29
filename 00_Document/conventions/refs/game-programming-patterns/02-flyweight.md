---
title: Flyweight
source: Game Programming Patterns — Robert Nystrom
category: Design Patterns Revisited
---

# [GPP-02] Flyweight (플라이웨이트)

> 수천 개 객체가 공유할 수 있는 데이터를 단 하나의 인스턴스로 분리해 메모리와 전송 대역폭을 절약하는 패턴. 분류: 구조 패턴(GoF 재방문).

---

## 언제 참조하나 (트리거)

- 유사한 객체가 수천 개 이상 생성되고, 각 객체가 동일한 데이터를 중복 보유할 때.
- 클라이언트→서버 또는 CPU→GPU 전송 시 "같은 모양의 데이터를 왜 매번 다시 보내나?" 고민이 생길 때.
- enum + switch 분기로 속성을 조회하던 코드를 객체 테이블로 리팩터하려 할 때 (타일/지형/몬스터 종류 등).
- State 패턴 객체를 여러 FSM이 공유해도 되는지 판단이 필요할 때.

---

## 핵심 내용

### 문제

숲(Forest)을 그린다고 하자. 나무 한 그루가 메시(mesh), 텍스처 2장, 위치, 높이, 두께, 색조를 모두 가지면, 나무 10,000그루가 그 데이터를 **10,000번 복제**한다. 메모리뿐 아니라 CPU→GPU 전송량도 폭발한다.

### 핵심 통찰: 상태를 두 종류로 쪼개기

| 종류 | 정의 | 예 |
|------|------|----|
| **Intrinsic(고유/공유)** | 모든 인스턴스가 동일한 값 — 컨텍스트 무관 | 나무 메시, 나무껍질 텍스처, 잎 텍스처 |
| **Extrinsic(외재/인스턴스별)** | 각 인스턴스마다 다른 값 | 위치, 높이, 두께, 색조 |

Intrinsic 데이터는 `TreeModel` 같은 단일 객체 하나에 담고, 각 나무 인스턴스는 **그 공유 객체를 가리키는 포인터 하나 + Extrinsic 값들**만 보유한다.

### GPU 인스턴싱과의 연결

현대 그래픽스 API(OpenGL/DX의 instanced draw call)는 이 패턴을 **하드웨어 수준**에서 지원한다. `TreeModel`을 GPU에 한 번 업로드하고, 인스턴스별 파라미터(위치·색조)를 별도 스트림으로 보낸 뒤 단일 draw call로 수만 그루를 렌더링한다. Nystrom은 "GoF 패턴 중 실제 하드웨어 지원이 있는 유일한 패턴"이라 표현한다.

### 타일/지형 예시 — 덜 명백한 적용

타일맵에서 지형 속성(이동 비용, 수상 여부, 텍스처)을 enum + switch로 조회하던 패턴 →
`Terrain` 클래스(이동비용, 수상여부, 텍스처 보유) 인스턴스를 지형 종류당 하나만 만들고,
타일 격자는 해당 `Terrain*` 포인터 배열만 유지한다. 맵 전체가 세 개의 `Terrain` 객체를 공유.

### 공유 객체는 반드시 불변(immutable)

공유 객체가 변하면 그것을 참조하는 모든 인스턴스에 즉시 영향을 준다. 이 부작용을 피하려면 Flyweight는 **읽기 전용**으로 설계해야 한다. "Flyweight 객체는 거의 언제나 불변이다."

### 팩토리 + 풀로 중복 생성 방지

Flyweight를 즉시 생성하기 어려운 경우 팩토리 메서드에서 기존 인스턴스를 먼저 조회하고, 없으면 생성한다. 생성한 Flyweight 자체의 수명 관리는 Object Pool 패턴과 자연스럽게 연결된다.

### 캐시 미스 우려 — 그냥 측정해라

포인터 한 단계 더 역참조하는 비용이 이론상 존재하지만, Nystrom이 직접 프로파일한 결과 페널티가 없거나 오히려 개선되는 경우도 있었다. **"최적화의 황금 법칙은 먼저 측정하는 것."** 이론적 우려로 더 나쁜 구조를 택하지 말 것.

---

## 우리 프로젝트 적용

### 이미 사용 중

| 영역 | 파일 | 내용 |
|------|------|------|
| PDL 생성 패킷 클래스 | `98_Shared/Protocol/` | 패킷 **정의**(필드 레이아웃)는 코드 생성기가 한 번 만든 정적 클래스. 인스턴스마다 값만 채운다 — Intrinsic = 필드 오프셋·ID, Extrinsic = 실제 페이로드 값 |

### 채택 후보

| 영역 | 현황 | 제안 |
|------|------|------|
| 몬스터 종 데이터 | 현재 미적용 — `02_Server/GameServer/Combat/EnemyKind.cs`의 enum이 초보 형태. 종별 스탯 Flyweight 클래스는 미존재 | 채택 후보: `EnemyKind` enum을 Flyweight 타입 객체(`EnemyBreed`)로 승격 시 교과서 적용. 지금은 적 종류가 적어 불필요 |
| 타일맵 지형 속성 | 하드코딩 좌표·switch 분기 (미래 맵 에디터 마일스톤 예정) | `TileType` enum → `TileData` Flyweight 객체 테이블로 전환 시 Flyweight 교과서 적용 |
| Unity 트리/파티클/배경 오브젝트 | 씬에 직접 배치 | 같은 프리팹 대량 배치 시 GPU instancing 활성화 = Flyweight + 하드웨어 지원 |
| `EnemyState` FSM 전이 로직 | 각 `EnemyEntity`(`02_Server/GameServer/Combat/EnemyEntity.cs`)가 `EnemyState` enum 보유 | State 패턴 도입 시 Idle/Patrol/Chase 상태 객체를 전체 적이 공유하는 Flyweight로 구현 가능 (상태 객체에 데이터 없고 행동만 있으므로) |

### 현재 무관

클라이언트(`UnityClientSession`, `PlayerPredictor`)는 플레이어 단위 고유 상태가 대부분이라 즉각 적용 대상 아님.

---

## 함정 / 과용 경계

- **가변 공유 객체**: 공유 Flyweight에 mutable 필드를 두면 레이스 컨디션 및 예측 불가 부작용. 반드시 immutable 설계 선행.
- **Intrinsic/Extrinsic 경계가 불명확한 경우**: 어떤 데이터를 공유할지 모호하다면 억지로 적용하지 말 것. 잘못된 분리는 오히려 버그 온상.
- **객체 수가 적을 때 조기 최적화**: 몬스터 종류가 5~10개라면 Flyweight 없이도 메모리 문제 없음. 수백~수천 인스턴스가 되는 시점에 적용.
- **State 패턴과 혼동**: State 객체는 Flyweight일 수 있지만, 상태가 컨텍스트별 데이터를 내부에 담기 시작하면 Flyweight 가정이 깨진다.
- **포인터 추적 비용을 이론으로만 거부하지 말 것**: 실측 없이 "캐시 미스 때문에 안 된다"고 포기하면 더 나쁜 설계(enum switch 난립)를 선택하게 된다.

---

## 관련

- [[06-state]] — State 객체가 컨텍스트 무관할 때 Flyweight로 공유 가능.
- [[18-object-pool]] — Flyweight 풀 관리에 Object Pool 조합.
- [[16-data-locality]] — Intrinsic 데이터를 연속 배열로 배치하면 캐시 효율 추가 확보 (Data Locality 패턴과 시너지).
