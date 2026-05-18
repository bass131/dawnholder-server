# Phase 05: 클라 Remote Entity Registry + Local/Remote 분기 + Interpolation Buffer ★

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 3h *(가장 무거움 — Codex β 가장 큰 risk 1)*
> **담당 에이전트**: client

---

## 🎯 목표

멀티 캐릭터 클라 상태 구조 박음. 본인 entity는 reconcile, 타인 entity는 prefab spawn/despawn + 보간 buffer로 부드럽게 표시. M2.5에서 본인이 박은 1인 prediction/reconcile을 *멀티로 일반화*.

## ⏪ 사전 조건

- [ ] Phase 04 완료 (서버 broadcast 깔림)

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scripts/State/RemoteEntityRegistry.cs` 신설 — `Dictionary<uint entityId, RemoteEntity>`
- [ ] `UnityClientSession.cs:152` 들어온 `S_Snapshot` 분기:
  - 본인 entityId → 기존 reconcile flow (회귀 X 보장)
  - 다른 entityId → registry lookup → 없으면 prefab spawn, 보간 buffer push
- [ ] `S2C_PlayerJoin` → spawn remote entity prefab (placeholder 박스 — Phase 08 시각화 전)
- [ ] `S2C_PlayerLeave` → despawn + buffer 청소
- [ ] **Remote interpolation buffer** — 200ms 지연 보간 (jitter 흡수). extrapolation 최소화 (응급은 last-known 위치 유지)
- [ ] Remote entity prefab 단순 시안 (placeholder 박스 + entityId 라벨)
- [ ] 회귀 테스트 — M2 1인 prediction/reconcile 깨지지 않음

## ✅ 완료 조건

- [ ] 헤드리스 2 봇 접속 → 두 캐릭터 모두 본인 클라 화면에 표시
- [ ] 한 봇 움직이면 다른 봇 클라에서 보간 표시 (부드러움, jitter 시각적 X)
- [ ] 한 봇 disconnect → despawn
- [ ] 본인 reconcile은 기존 그대로 (1인 회귀 시나리오 통과)
- [ ] disconnect 시 buffer 청소 (메모리 누수 X)

---

## 🧪 테스트

**자동**: M2 1인 회귀 시나리오 (기존 테스트 그대로 통과)
**수동**: 헤드리스 2 봇 + 1 Unity 클라 = 두 봇 모두 표시 + 보간 부드러움 / 한 봇 끄기 → despawn / 본인 prediction 회귀 X

---

## 📚 학습 포인트

- **본인 vs 타인 분기** — prediction은 본인만, 타인은 *순수 보간* (`ARCHITECTURE.md` "핵심 포인트" 1번 정합)
- **Interpolation buffer** — 왜 *지연 보간*이 필요한가. packet jitter (네트워크 도착 시간 분산) 흡수 = 시각적 부드러움
- **Entity registry 패턴** — game dev 보편 (Unity NGO, Mirror 모두 비슷). Dictionary<id, entity> + spawn/despawn lifecycle
- **회귀 안전망 가치** — 새 기능 박을 때 기존 시나리오 깨지지 않는지 *명시적으로 테스트*

---

## ⚠️ 함정 / 주의사항

- **본인 entity를 타인으로 잘못 처리** → 본인 캐릭터가 lag으로 표시 (200ms 지연 보임)
- **보간 buffer 없으면** packet jitter로 표시 끊김 (특히 1초에 20틱 = 50ms 분산이 시각적으로 보임)
- **Spawn/despawn race** — PlayerJoin 도착 *전*에 Snapshot이 먼저 도착할 수 있음 → registry lookup 시 *지연 spawn* 또는 *Snapshot에서 unknown entity 무시* 선택
- **Disconnect 시 buffer 청소 누락** → 메모리 누수 (Phase 09 리허설에서 발견될 수 있지만 미리 박는 게 안전)
- **본인 reconcile 회귀** — 분기 로직 잘못 짜면 본인 entity도 보간 path로 가서 lag 발생

---

## ➡️ 다음 Phase

Phase 06 — 서버 응급 전투 인프라

---

## 작업 로그

- 2026-05-18: pending (Codex β 가장 큰 risk 1 = 멀티 캐릭터 클라 상태 구조)
