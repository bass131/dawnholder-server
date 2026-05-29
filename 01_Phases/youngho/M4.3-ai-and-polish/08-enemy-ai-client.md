---
owner: youngho
milestone: M4.3
phase: 08
title: Enemy AI 클라 — 위치 보간 + 이동 렌더 + Animator
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~4h
domain: client
---

# Phase 08: Enemy AI 클라 — 위치 보간 + 이동 렌더

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (client 단일 도메인이나 prefab/Animator unity-asset 깃발)
> **담당**: client SubAgent (위치 wiring) + 본인 (Animator/Sprite 외관 분담)

---

## 🎯 목표

Phase 07에서 서버가 보내는 `S_EntityState`(적 위치/상태)를 클라가 받아, **적이 화면에서 부드럽게 순찰/추격하는 것을 렌더**한다. 기존 RemotePlayer 보간 패턴을 재사용해 enemy GameObject 위치를 보간하고, 이동 방향에 따라 좌우 flip + walk 애니메이션을 표시한다.

이 Phase가 끝나면 **Play에서 사냥터에 들어가면 적이 실제로 움직이는 것**이 보인다 (M4.3 발표 데모의 핵심 시각 요소).

---

## ⏪ 사전 조건

- [ ] **Phase 07 완료** — `S_EntityState` 패킷 + 서버 enemy FSM 동작 (받을 데이터가 있어야 함)
- [x] RemotePlayer 보간 인프라 (M3 Phase 05 remote entity registry) — 참조 패턴

---

## 📝 작업 내용

### 네트워크 (04_ClientNet)
- [ ] `S_EntityState` 핸들러 추가 — 받은 entityId/x/y/state를 enemy mirror에 전달
- [ ] 기존 `S_EntitySpawn`(스폰) / `S_EntityDeath`(제거) 와 lifecycle 정합 — spawn으로 생성, state로 갱신, death로 제거

### 클라 렌더 (03_Client)
- [ ] enemy GameObject 위치 **보간(interpolation)** — 서버 20 TPS 위치를 디스플레이 Hz로 부드럽게 (RemotePlayer 보간 로직 재사용/공유)
- [ ] 이동 방향에 따라 스프라이트 좌우 flip
- [ ] enemy Animator: Idle / Walk 전환 (state 또는 위치 델타 기반) — **본인 외관 분담**
- [ ] state(Patrol/Chase)에 따른 시각 차이(옵션) — 예: chase 시 살짝 강조 (발표 어필, 여유 시)

### 테스트
- [ ] Play 실측 — 사냥터 진입 시 enemy patrol → 접근 시 chase 따라옴, 보간 부드러움(끊김 없음)
- [ ] (가능 시) EditMode 보간 로직 단위 테스트

---

## ✅ 완료 조건

- [ ] Play에서 enemy가 화면에서 patrol(왕복) → 플레이어 접근 시 chase(따라옴) 시각 확인
- [ ] enemy 이동이 **보간되어 부드러움** (20 TPS 스냅 아닌 연속 이동)
- [ ] **객관 대조 기준** (plan-auditor 🔴 봉합): RemotePlayer와 동일 보간 버퍼 값 사용 + Phase 07 `EnemyAiSmoke`가 뽑은 서버 enemy 좌표열과 클라 렌더 좌표를 1:1 대조 (육안 또는 로그) — 클라가 서버 좌표를 임의 변형/외삽하지 않음 확인
- [ ] enemy 좌우 방향 flip 정상, walk 애니메이션 재생
- [ ] enemy 사망 시 깔끔히 사라짐 (S_EntityDeath 정합), 회귀 0
- [ ] 클라/서버 enemy 좌표 일치 (DLL stale 아님 — `dotnet build` 선행 확인)

---

## 🧪 테스트

**자동**:
- (가능 시) 보간 함수 EditMode 테스트 — 두 스냅샷 사이 보간 값 검증

**수동**:
- 서버 + Unity Play → 사냥터에서 적 움직임 관찰 (patrol/chase/사망)
- 2인(봇+클라)으로 enemy가 누구를 chase하는지 확인

---

## 📚 학습 포인트

- **보간(interpolation) vs 외삽(extrapolation)**: 서버 위치는 띄엄띄엄(50ms). 보간 = 받은 두 점 사이를 채움(살짝 과거를 그림). 외삽 = 미래 추정(틀리면 튐). MMORPG는 보통 보간 + 약간의 지연.
- **클라는 렌더러일 뿐 (헌법 #1)**: enemy 위치를 클라가 계산하지 않고 서버 값만 그림. AI 판단 0.
- **Animator 상태와 네트워크 상태 분리**: 서버 EnemyState(논리)와 Unity Animator state(시각)는 다른 레이어. state→애니 매핑은 클라 표현.

---

## ⚠️ 함정 / 주의사항

- **보간 지연 vs 반응성**: 보간 버퍼를 너무 키우면 적이 굼떠 보이고, 너무 줄이면 끊김. RemotePlayer와 같은 값으로 시작.
- **prefab 작업 = unity-asset 위험 깃발** (Phase 08 BackGround 사고 학습): enemy prefab 편집 시 백업 의무. scene/prefab YAML 직접 편집은 위험 — Unity Editor 직접 또는 메인 세션 MCP.
- **death 후 GameObject 누수**: S_EntityDeath 받고 Destroy 누락하면 죽은 적이 화면에 남음. lifecycle 철저히.
- **Animator/Sprite는 본인 분담** (memory `unity-visual-work-user-owned`): Animator 클립/전이 셋업은 본인 직접. AI는 위치 wiring + state 전달 코드만.

---

## ➡️ 다음 Phase

- Phase 09 — boss behavior (다단 attack 패턴 + 적→플레이어 공격)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `08-enemy-ai-client-DONE.md` 박음.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
