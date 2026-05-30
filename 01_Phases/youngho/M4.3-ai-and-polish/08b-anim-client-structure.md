---
owner: youngho
milestone: M4.3
phase: 08b
title: 애니 상태머신 — 클라 구조 (IMotionState + AnimatorDriver + 소스 3종 + enemy 위치 보간)
status: done
grade: 복잡
risk: unity-asset
estimated: 3~4h
domain: client
summary: 서버 animState를 Animator로 렌더하는 공통 구조(전략 패턴) + enemy 위치 보간을 기존 RemoteEntity 재사용으로 구축
---

# Phase 08b: 애니 상태머신 — 클라 구조

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (client 단일 도메인이나 컴포넌트 구조 + unity-asset 깃발)
> **담당**: client SubAgent (코드 구조/wiring) — Animator 클립/전이는 11 (본인 외관 분담)

---

## 🎯 목표

08a가 서버에서 보내주는 `animState`(byte)를, 클라가 **공통 구조로 Animator에 반영**하는 골격을 세운다. "Animator를 구동하는 일"은 공통(`AnimatorDriver`), "상태를 *판단/공급*하는 소스"만 객체별로 갈아끼우는 **전략 패턴**(`IMotionState` 구현 3종)이다. 동시에 enemy 위치를 **기존 `RemoteEntity` 보간 컴포넌트 재사용**으로 부드럽게 만든다.

이 Phase가 끝나면 **placeholder Animator로도 서버 상태가 화면에 반영**(파라미터 set 확인)되고, **적이 보간되어 부드럽게 이동**한다. 실제 클립/전이 셋업은 11(본인)에서 얹는다.

### 핵심 설계 (사용자 의논 2026-05-30)
- **종속 최소화**: 잘 도는 LocalPlayer/RemotePlayer 코드를 *건드려 공유 클래스로 추출하지 않는다*. Unity "한 책임 + 조합" 정신으로, 기존 `RemoteEntity`(보간) 컴포넌트를 enemy GameObject에 **그대로 부착**한다 (코드 0 수정, 복붙 0).
- **공통/분기 경계**: 이동(Walk/Idle/flip)은 transform 델타로 추론 가능 → 공통. Jump/Attack/Hit/Death는 위치로 안 드러남 → 소스가 서버 animState를 읽음.

---

## ⏪ 사전 조건

- [ ] **Phase 08a 완료** — `AnimState` enum(Shared.dll) + `S_Snapshot`/`S_EntityState`에 animState byte (받을 데이터 존재)
- [x] `RemoteEntity`(보간 buffer) / `PlayerAnimatorSync`(IsMoving+flip 참조 패턴) / `EnemyRegistry`+`EnemyViewFactory`(enemy GO 조립)

---

## 📝 작업 내용

### 클라 구조 (03_Client)
- [ ] **`IMotionState` interface** (`Scripts/Rendering/`) — Animator를 구동할 "상태 공급원" 추상화:
  ```
  AnimState CurrentAnimState { get; }   // Shared.GameData.AnimState
  int Facing { get; }                   // -1 / +1 (좌우 flip)
  ```
- [ ] **`AnimatorDriver` 공통 컴포넌트** (MonoBehaviour) — 매 프레임 `IMotionState`를 읽어 `Animator.SetInteger("AnimState", ...)` + `SpriteRenderer.flipX` 적용. Animator/SpriteRenderer는 옵셔널(enemy placeholder엔 Animator 없을 수 있음 → flip만). **Animator 파라미터 계약**(키 이름 `"AnimState"` 등)을 주석/상수로 명시 → 11에서 본인이 Animator 셋업 시 참조
- [ ] **소스 3종** (`IMotionState` 구현):
  - `LocalPlayerMotion` — 입력/prediction 기반 (반응성). 기존 `PlayerAnimatorSync` 로직을 이 구현으로 흡수/대체 (단 LocalPlayer 동작 회귀 0 — 기존 IsMoving+flip 보존)
  - `RemotePlayerMotion` — `S_Snapshot.animState` 읽어 그대로 노출 (서버 권위)
  - `EnemyMotion` — `S_EntityState.animState` 읽어 노출. Facing은 보간 위치 델타로
- [ ] **enemy 위치 보간 (RemoteEntity 재사용)**:
  - `EnemyViewFactory.BuildPlaceholder`에서 enemy GameObject에 `RemoteEntity` + `AnimatorDriver` + `EnemyMotion` **AddComponent**
  - `EnemyRegistry.UpdatePosition` → 직접 transform 세팅 제거, `RemoteEntity.EnqueueSnapshot(x, y)` 호출로 전환 (보간)

### 네트워크 wiring (04_ClientNet / 핸들러)
- [ ] `S_EntityState` 핸들러 — animState를 `EnemyMotion`(또는 registry 경유)에 전달 + 위치는 `EnqueueSnapshot`
- [ ] `S_Snapshot` 핸들러(타인 분기) — animState를 `RemotePlayerMotion`에 전달
- [ ] lifecycle 정합 — spawn(생성)/state(갱신)/death(제거) 그대로, 보간 buffer는 despawn 시 `ClearBuffer`

### 테스트
- [ ] (가능 시) EditMode — `AnimatorDriver`가 `IMotionState.CurrentAnimState`를 `SetInteger`로 올바로 전달하는지 (mock Animator/소스)
- [ ] Play 실측 — placeholder Animator(파라미터만)로 서버 animState 변화가 `SetInteger`에 반영되는지 로그 확인. 적 보간 부드러움

---

## ✅ 완료 조건

- [ ] `IMotionState` + `AnimatorDriver` + 소스 3종 컴파일 + Unity 로드 0 error
- [ ] **적이 보간되어 부드럽게 이동** (20 TPS 스냅 아님) — 기존 RemoteEntity 보간 값(150ms) 그대로 사용, `EnemyRegistry.UpdatePosition`이 더 이상 직접 transform 세팅 안 함
- [ ] **객관 대조**: 서버 `S_EntityState` 좌표열(08a 로그/EnemyAiSmoke)과 클라 렌더 좌표 1:1 대조 — 클라가 임의 외삽 0 (RemoteEntity 보간만)
- [ ] 서버 animState 변화가 클라 `Animator.SetInteger("AnimState")`에 반영됨 (placeholder Animator + 로그로 검증 — 실제 클립은 11)
- [ ] enemy 좌우 flip 정상 (이동 방향)
- [ ] **회귀 0**: 기존 LocalPlayer 이동 애니(IsMoving+flip) + RemotePlayer 위치 보간 + enemy spawn/death lifecycle 그대로
- [ ] `dotnet build`로 Shared.dll stale 아님 확인 후 Play (animState enum 최신)

---

## 🧪 테스트

**자동**:
- (가능 시) `AnimatorDriver` EditMode — IMotionState → SetInteger 전달 검증

**수동**:
- 서버 + Unity Play → 사냥터 적이 보간 이동 + flip + (placeholder)animState 파라미터 반영 로그
- 기존 LocalPlayer/RemotePlayer 애니·보간 회귀 관찰

---

## 📚 학습 포인트

- **전략 패턴(Strategy)**: 같은 일(Animator 구동)을 하되 "상태를 어디서 얻나"만 갈아끼움. `AnimatorDriver`는 소스를 모르고 `IMotionState`만 의존 → LocalPlayer/RemotePlayer/Enemy가 같은 driver 재사용.
- **조합 > 상속/추출 (종속 최소)**: 보간이 필요하면 `RemoteEntity`를 *붙이면* 됨. 잘 도는 코드를 공유 클래스로 뜯어고치는 것보다 결합도가 낮음 (Unity 컴포넌트 철학).
- **보간 vs 외삽 복습**: enemy도 RemotePlayer와 동일하게 받은 두 점 사이만 채움(150ms 지연). 미래 추정(외삽) 안 함 — 적이 갑자기 튀지 않음.
- **표현/데이터 레이어 분리**: 위치 갱신(EnqueueSnapshot)과 애니(SetInteger)는 별 경로. mirror 객체는 둘 다 필요 — 위치만 옮기고 애니를 빼먹으면 "미끄러짐".

---

## ⚠️ 함정 / 주의사항

- **LocalPlayer 회귀 주의**: `PlayerAnimatorSync` 로직을 `LocalPlayerMotion`으로 옮길 때 기존 IsMoving+flip 동작 100% 보존. 발표 핵심인 본인 캐릭터 애니가 깨지면 안 됨. 옮기지 않고 *공존*시키는 안도 검토(과도한 손대기 금지).
- **Animator 없는 enemy**: 현재 enemy는 placeholder(SpriteRenderer만). `AnimatorDriver`가 Animator null이어도 `flipX`는 동작하게 (null 가드). 실제 Animator는 11에서 본인이 부착.
- **unity-asset 위험 깃발** (Phase 08 BackGround 사고 학습): prefab/scene YAML 직접 편집 금지. enemy는 런타임 코드 생성(EnemyViewFactory)이라 prefab 무관하나, RemoteEntity prefab 건드릴 일 있으면 백업 + Unity Editor 경유.
- **death 후 보간 buffer 누수**: despawn 시 `ClearBuffer` 호출 보존 (RemoteEntity 함정 #4). enemy도 동일.
- **Animator 파라미터 계약 drift**: `AnimatorDriver`가 쓰는 키(`"AnimState"`, `int`)와 11에서 본인이 Animator Controller에 만드는 파라미터 이름/타입이 *정확히 일치*해야 함. 계약을 상수/주석으로 박아 11에 전달.

---

## ➡️ 다음 Phase

- **Phase 11** — 애니 외관 완성 (본인이 Animator 6상태 클립 + 전이를 `AnimatorDriver` 계약에 맞춰 셋업)
- (병렬) Phase 09 — boss behavior가 08a/08b 위에 boss attack animState 얹음

---

## 📋 박제 (완료 후)

- **복잡 등급** — `08b-anim-client-structure-DONE.md` 박음. (전략 패턴 구조 결정 기록 가치)

---

## 작업 로그

- 2026-05-30: 계획 수립 (`/work:plan` 애니 상태머신 재편 — 기존 08 enemy-ai-client를 08a/08b로 분리)
- 2026-05-30: 구현 완료 (client SubAgent). 신규 5 + 수정 5. dotnet build 0/0, reviewer 🔴0, Unity Play 실측(enemy 보간+flip 정상). 박제 → `08b-anim-client-structure-DONE.md`. 로컬 플레이어 rubber-band(선재)는 Phase 10 이월.
