---
owner: youngho
milestone: M4.3
phase: 10
title: 움직임 체감 Polish — 클래스별 이동속도 + jump Y mispredict + reconcile drift
status: pending
grade: 복잡
risk: trust-boundary
estimated: 2~3h
domain: shared+server+client
---

# Phase 10: 움직임 체감 Polish

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 복잡 (3 도메인 — prediction/reconcile는 신뢰 경계 근처)
> **담당**: shared (속도 상수) + server (권위 이동) + client (prediction/reconcile)

---

## 🎯 목표

발표 데모에서 **손맛(체감)**을 망치는 세 가지를 봉합한다:
1. **β10 MoveSpeed dead** — `PlayerStats.MoveSpeed`(Warrior 4 / Ranger 6)가 정의돼 있지만 실제 이동에 적용 안 됨 → 클래스 차이가 체감 0
2. **M2 jump Y mispredict** — 점프 시 Y축 prediction이 서버와 어긋나 캐릭터가 살짝 튐(잔존 결함)
3. **reconcile drift** — 평상시 d≈±1.5 정도 snap이 보임 (가변 dt vs 고정 tick 불일치)

이 셋은 모두 **클라 prediction ↔ 서버 권위 ↔ reconcile** 삼각형의 정합 문제라 한 Phase로 묶는다.

---

## ⏪ 사전 조건

- [x] M2 Phase 06 input replay reconcile (prediction/reconcile 인프라)
- [x] M4.1 movement 정밀화
- [ ] 07~09와 독립 — **Phase 11과 병렬 가능**

---

## 📝 작업 내용

### β10 MoveSpeed dead 봉합 (shared+server+client)
- [ ] `PlayerStats.MoveSpeed`가 **서버 권위 이동 계산에 실제 반영**되는지 추적 — 서버 Physics.Step에서 클래스 MoveSpeed 사용 확인/봉합
- [ ] **클라 prediction도 같은 MoveSpeed 사용** — 서버/클라 결정론 정합 (안 맞으면 매 틱 reconcile 발생)
- [ ] Warrior(4) vs Ranger(6) 이동속도 차이가 Play에서 체감되는지 실측

### jump Y mispredict 봉합 (server+client)
- [ ] M2 Phase 05 jump 잔존 결함 추적 — 점프 시 Y prediction과 서버 Y의 차이 원인 (중력 적용 순서? dt? 초기 속도?)
- [ ] 서버/클라 점프 물리 결정론 정합 (같은 입력 → 같은 Y 궤적)

### reconcile drift 튜닝 (client)
- [ ] 평상시 d≈±1.5 snap 원인 — 가변 dt(디스플레이 Hz) vs 고정 tick(50ms) 누적 오차
- [ ] reconcile 임계/보정 방식 튜닝 — 큰 오차만 snap, 작은 오차는 부드럽게 흡수(smooth correction)
- [ ] **DLL stale 주의** (work-pin 습관 a): Shared/ClientNet 수정 후 Play 전 `dotnet build Dawnholder.slnx` 1회 — 안 하면 stale Physics로 reconcile 거짓 drift

### 테스트
- [ ] 결정론 단위 테스트 — 같은 입력 시퀀스로 서버/클라 Physics가 같은 위치 산출 (이동 + 점프)
- [ ] Play 실측 + 서버 로그 [Cheat]/[Trust] 0건 (prediction 정합 = 거짓 위반 0)

---

## ✅ 완료 조건

- [ ] Warrior/Ranger 이동속도 차이 Play 체감 (Ranger가 눈에 띄게 빠름)
- [ ] 점프 시 캐릭터 Y 튐 0 (부드러운 점프/착지)
- [ ] 평상시 reconcile snap 체감 거의 0 (d≈±1.5 같은 상시 drift 해소)
- [ ] `dotnet test --no-incremental` green — 결정론 테스트 통과, 회귀 0
- [ ] 서버 로그 [Cheat]/[Trust] 위반 0 (정상 이동이 거짓 플래그 안 뜸)

---

## 🧪 테스트

**자동**:
- 결정론 테스트 — 입력 시퀀스 → 서버 Physics vs 클라 prediction 위치 일치 (이동/점프)
- 회귀: 기존 movement/reconcile 테스트 0 실패

**수동**:
- Play — 클래스별 속도 체감, 점프 튐 관찰, 장시간 이동 시 drift 관찰
- 서버 콘솔 [Cheat]/[Trust] 로그 모니터

---

## 📚 학습 포인트

- **결정론(determinism)이 prediction의 전제**: 클라와 서버가 같은 입력에 같은 결과를 내야 reconcile이 조용함. 한쪽만 MoveSpeed 다르면 매 틱 어긋나 snap.
- **고정 tick vs 가변 프레임**: 서버는 50ms 고정, 클라는 프레임마다 dt 가변. 둘을 같은 물리로 맞추려면 클라도 고정 step 누적(accumulator) 패턴이 정석.
- **smooth correction vs hard snap**: 작은 오차를 즉시 텔레포트(snap)하면 떨림. 여러 프레임에 나눠 보정하면 부드럽지만 약간 늦음. trade-off.
- **거짓 양성(false positive) 경계**: 정상 prediction 오차가 cheat 플래그로 오인되지 않게. (β5/β12가 이미 false positive 확정 — work-pin)

---

## ⚠️ 함정 / 주의사항

- **DLL stale = 소리 없는 drift** (work-pin 핵심 학습): Shared/ClientNet 고치고 `dotnet build` 안 하면 Unity가 옛 dll로 prediction → 서버와 영구 어긋남. 이 Phase는 특히 취약 — 매 Play 전 빌드.
- **신뢰 경계(헌법 #3)**: MoveSpeed를 클라가 키워서 빨리 못 움직이게 — 서버가 권위 MoveSpeed로 검증. 클라 prediction은 표시용, 서버가 최종.
- **점프 봉합이 다른 걸 깨지 않게**: 중력/착지/이동이 한 Physics에 얽혀 있음. 점프만 고치다 수평 이동 회귀 위험 — 결정론 테스트로 가드.
- **β5/β12 롤백 검토 아님**: 그건 이미 false positive 확정(봉합 불필요). 이 Phase는 *실제* drift만 다룸.

---

## ➡️ 다음 Phase

- Phase 11 — RemotePlayer 외관 봉합 (병렬 가능했음) / Phase 12 — 회귀 + 마감

---

## 📋 박제 (완료 후)

- **복잡 등급** — `10-movement-feel-polish-DONE.md` 박음.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
