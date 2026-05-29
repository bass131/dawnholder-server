---
title: Dirty Flag
source: Game Programming Patterns (Robert Nystrom)
category: Optimization Patterns
---
# [GPP-17] 더티 플래그 (Dirty Flag)

> 1차 데이터(primary data)가 바뀔 때마다 2차 데이터(derived data)를 즉시 재계산하는 대신, "오염됨" 플래그만 세워두고 실제로 필요할 때까지 계산을 미루는 패턴. 분류: Optimization Patterns

---

## 언제 참조하나 (트리거)

- 씬 그래프·트랜스폼 계층에서 월드 좌표 재계산이 불필요하게 반복 발생할 때
- 서버 틱 루프에서 "변경된 객체만" 처리해야 하는 선별 로직을 설계할 때
- 클라이언트 측에서 네트워크 상태가 변했는지 확인 후 UI를 업데이트하는 흐름을 최적화할 때
- 캐싱된 계산 결과를 언제 무효화(invalidate)할지 판단 기준이 필요할 때

---

## 핵심 내용

### 문제

계층 구조에서 상위 노드가 움직이면 그 아래 모든 자식의 월드 좌표가 무효화된다. 프레임 안에 여러 노드가 연속으로 움직일 때, 매 변경마다 자식 전체를 재계산하면 "결국 버려질" 중간 결과를 위해 CPU를 낭비한다.

예시: `해적선 → 까마귀 둥지 → 해적 → 앵무새` 계층에서 선이 프레임 안에 세 번 이동하면, 각 이동마다 앵무새 월드 좌표가 재계산된다. 하지만 렌더링은 프레임 끝에 딱 한 번만 필요하다.

### 해법 구조

각 노드에 불리언 플래그(`dirty_`)를 붙인다.

| 이벤트 | 동작 |
|--------|------|
| 1차 데이터(local transform) 변경 | `dirty_ = true` 만 세팅. 재계산 하지 않음 |
| 렌더링 요청(derived data 필요) | 플래그가 `true`이면 그때 재계산 + `dirty_ = false` |
| 플래그가 `false` | 캐시된 값을 그대로 사용 |

### 계층 전파 메커니즘

자식 재귀 마킹을 피하는 핵심 트릭: 렌더 함수에 `bool dirty` 매개변수를 전달한다.

```
void render(Transform parentWorld, bool dirty) {
    dirty |= dirty_;          // 부모 or 자신이 오염됐으면 true
    if (dirty) {
        world_ = local_.combine(parentWorld);
        dirty_ = false;
    }
    for (auto child : children_)
        child->render(world_, dirty);  // dirty 전파
}
```

`setTransform()` 시점에 자식을 순회하며 마킹할 필요가 없다. 대신 렌더 단계에서 상위의 dirty 여부를 OR로 받아 내려가므로, 조상이 더럽혀지면 후손 전체가 자동으로 재계산된다.

### 이 패턴이 유효한 네 조건

1. **1차 변경 횟수 > 2차 데이터 사용 횟수**: 매 변경 직후 결과가 필요하다면 미루는 의미가 없다.
2. **점진적 업데이트가 어렵다**: 합계처럼 델타만 더하면 되는 경우엔 이 패턴보다 단순 누산이 낫다.
3. **캐시 무효화 경계가 명확히 정의 가능**: 1차 데이터를 변경하는 모든 경로에서 플래그를 세울 수 있어야 한다.
4. **메모리 여유 있음**: 2차 데이터를 상시 보관해야 하므로 메모리를 추가로 점유한다.

### 플래그를 언제 청소(clean)할 것인가 — 세 전략

**① 결과가 필요한 순간에 청소 (On Demand)**
- 장점: 실제로 쓰이지 않으면 계산 자체가 발생 안 함
- 단점: 렌더 루프 안에서 긴 계산이 터지면 프레임 히치(hitch) 발생

**② 고정된 체크포인트에서 청소 (Fixed Checkpoint)**
- 예: 게임 저장 화면, 로딩 씬 전환 시점
- 장점: 유저 경험에 영향 없는 시점에 처리
- 단점: 플레이어가 체크포인트를 건너뛰거나 도달 전에 결과가 필요해질 수 있음

**③ 백그라운드 처리 (Background)**
- 타이머로 주기적으로 정리
- 장점: 빈도 조절 가능
- 단점: 스레딩 인프라 필요, 변경이 없는 데이터까지 훑는 낭비 가능

### 입도(Granularity) 트레이드오프

- **세립(Fine-grained)**: 작은 데이터 단위마다 플래그. 변경된 것만 처리하지만 플래그용 메모리와 per-chunk 오버헤드가 많음.
- **조립(Coarse-grained)**: 큰 덩어리 하나에 플래그. 메모리 적고 단순하지만 미변경 데이터도 재처리할 수 있음.

---

## 우리 프로젝트 적용

### 채택 후보

**`02_Server/GameServer/Maps/GameMap.cs` — 브로드캐스트 최적화**
- `GameMap`은 매 틱마다 모든 엔터티 상태를 브로드캐스트 후보로 처리할 가능성이 있음.
- `02_Server/GameServer/Combat/EnemyEntity.cs`, `02_Server/GameServer/Maps/PlayerEntity.cs`에 `dirty_` 플래그를 두어 "틱 안에서 위치·HP·FSM 상태가 변경된 엔터티만" 선별 브로드캐스트하면 패킷 수를 대폭 줄일 수 있음.
- 현재 `ProcessAttack`, `UpdateEnemies` 경로에서 상태 변경 후 즉시 전송하는지 캐시하는지 확인 후 도입 판단.

**`02_Server/GameServer/Network/GameSession.cs` — 캐릭터 스냅샷 더티 추적**
- 30초 주기 영속화(Persistence cadence) 구현 시, 변경이 없는 플레이어를 DB 쓰기에서 제외하는 데 dirty flag가 자연스러운 선택.
- `HP`, `Position`, `Inventory` 등 필드 그룹 단위로 플래그를 두어 변경된 필드만 EF Core partial update.

**`03_Client/Assets/Scripts/UI/HudController.cs` — HudController / UI 갱신**
- 서버 스냅샷 수신 시 HP·MP 등이 실제로 바뀐 경우에만 UI를 갱신하는 흐름에 이미 암묵적 dirty check가 존재할 것.
- 명시적 플래그로 바꾸면 의도가 문서화되고 불필요한 `SetText` / `Image.fillAmount` 호출 제거.

### 현재 무관

- **틱 루프 내 이동 처리**: 서버는 매 틱마다 position을 무조건 계산해야 하므로(prediction/reconcile 기반), dirty flag로 계산 자체를 미루는 건 맞지 않음.
- **씬 그래프 트랜스폼**: Unity 엔진 자체가 내부적으로 dirty flag를 사용해 월드 좌표를 캐싱. 우리가 직접 구현할 영역 아님.

---

## 함정 / 과용 경계

- **누락된 세터(Leaky setter)**: 1차 데이터를 변경하는 경로 하나라도 `dirty_ = true`를 빠뜨리면 오래된 캐시가 정상 값처럼 노출된다. 이 버그는 증상이 산발적이라 추적이 매우 어렵다. 수정 경로를 단일 API로 캡슐화하는 것이 최선 예방책.
- **결과가 항상 즉시 필요한 시스템**: "변경 직후 바로 읽힘"이 패턴이면 미루기 이점이 제로. 복잡도만 늘어남.
- **점진 업데이트가 가능한 경우**: 누산 값(총 무게, 스코어 합계 등)은 delta만 더하는 편이 코드도 단순하고 성능도 낫다.
- **메모리 제약 환경**: 파생 데이터를 상시 저장해야 하므로 임베디드·구형 콘솔처럼 메모리가 귀한 환경에선 역효과.
- **조기 최적화 경고**: 캐시 무효화 버그는 "두 가지 어려운 것(cache invalidation and naming things)" 중 하나. 실측 성능 문제가 없는데 이 패턴을 도입하면 복잡도만 올라가고 버그 리스크만 생긴다.

---

## 관련

- [[07-double-buffer]] — 더블 버퍼도 "언제 결과를 공개할까"를 제어하는 패턴. Dirty Flag는 "언제 계산할까"를 제어. 함께 쓰이는 경우 많음.
- [[09-update-method]] — `UpdateEnemies()` 리팩터링 시 dirty 엔터티만 Update를 호출하는 최적화로 연결.
- [[14-event-queue]] — 1차 데이터 변경을 이벤트 큐로 배치할 때, 큐에서 꺼내는 시점이 dirty 청소 체크포인트가 될 수 있음.
