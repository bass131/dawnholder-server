---
owner: youngho
milestone: M4.15
phase: 04
title: 투사체 일정 속도 (서버 travelTicks 모델)
status: done
grade: 보통
domain: server
summary: travelTicks MaxTravelTicks(10) 상한 artifact 제거 → ceil(2D거리÷고정속도), 속도 폭증 봉합
---

# Phase 04: 투사체 일정 속도 (서버 travelTicks 모델)

> **상태**: done (2026-06-14, server Worker + 메인 게이트, reviewer 스킵=보통)
> **마일스톤**: M4.15
> **등급**: 보통
> **담당**: server (Sonnet Worker)

---

## 🎯 목표

서버가 Mage 투사체 비행 시간을 산출하는 `travelTicks` 공식의 **`MaxTravelTicks`(10) 상한 artifact**를 제거해, 비행 시간이 거리에 *비례*하도록(= 투사체 속도 일정) 만든다. "멀면 너무 빨리 날아감"의 근본(상한 고정 + 거리 폭증)을 서버 측에서 봉합한다.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (Mage 박스 X 사거리 제한 = 투사체 최대 거리 bound 확보).
- [ ] Phase 03 완료 (`MeleeAction.cs` 동일 파일 직렬화 + freeze-travelTicks 결합 해소).

---

## 📝 작업 내용

- [ ] `MeleeAction.cs:64-68` — `travelTicks = clamp(round(dx/ProjectileSpeedPerTick), Min, Max)`에서 **`MaxTravelTicks` 상한 제거 또는 무해화**: 사거리(Phase 02 Mage X)가 bound라 상한 안 닿게 충분히 높이거나, `ceil(거리/ProjectileSpeedPerTick)`로 단조. `MinTravelTicks`(2)는 발사 연출 최소 보장 위해 유지.
- [ ] `CombatConstants.cs` — `MaxTravelTicks` 제거 시 주석 정리 / 유지 시 사거리 기반 재산정. `ProjectileSpeedPerTick`(2.0 = 40u/s @20TPS)는 영호 승인 속도값.
- [ ] `DeferredImpact.ImpactTick = CurrentTick + travelTicks` 정합 확인 (지연 데미지 도착 = 투사체 시각 도착과 일치 의도).
- [ ] (검토) 서버는 `dx`(수평), 클라는 2D 거리 — Y가 작아져(Phase 02) 수렴하나, 일관성 위해 서버도 필요 시 2D 거리 고려(선택).
- [ ] 테스트 갱신: `MageRangedCombatTests` `travelTicks` 기대값(상한 의존 케이스) 재계산.

---

## ✅ 완료 조건

- [ ] `travelTicks`가 거리 단조증가 (상한 폭증 케이스 0 — 사거리 내에서 클램프 안 닿음).
- [ ] 단위 테스트: 가까운/먼 거리 모두 `travelTicks ∝ 거리` (속도 일정 검증).
- [ ] `ImpactTick` 정합 (지연 데미지 도착 틱 = travelTicks 후).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).
- [ ] wire 무변경 — `S_ProjectileLaunch.travelTicks` 필드 유지 (`Protocol.Version` v13).

---

## 🧪 테스트

**자동**: `MageRangedCombatTests` — 거리별 `travelTicks` 단조성 + `ImpactTick` 정합.
**수동**: Phase 05(클라) 후 영호 Play — 먼 적도 일정 속도.

---

## 📚 학습 포인트

- **clamp가 만든 숨은 비선형** — 상한이 "안전 캡"처럼 보여도, 종속 계산(클라 속도=거리/시간)이 그 상한을 나누면 *상한 너머에서 종속변수가 폭증*. 캡은 입력이 아니라 *결과*를 봐야.
- **사거리 bound가 캡을 대체** — Phase 02에서 사거리를 물리적으로 제한하면 인위적 틱 캡이 불필요해짐 (자연 bound).
- **틱 기반 투사체** (헌법 #5) — 비행을 ms 타이머가 아닌 정수 틱 카운트다운(`DeferredDamageSystem`)으로 = blocking 0.

---

## ⚠️ 함정 / 주의사항

- `MaxTravelTicks` 완전 제거 시: 사거리 밖 발사가 *이론상* 가능하면 비행이 길어질 수 있음 → Phase 02 사거리 게이트가 먼저 miss 처리하므로 안전하나, 방어적으로 사거리 기반 상한(높게)을 두는 것도 정석.
- `round` vs `ceil` — `MinTravelTicks=2` 하한과 함께 0틱 즉시 도착(연출 소실) 방지.
- freeze 제거(Phase 03) 후라 `travelTicks` 변경이 freeze 지속에 영향 0 (결합 끊김 확인).

---

## ➡️ 다음 Phase

- Phase 05 — 투사체 클라 정합 + 호밍 polish.

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
- 2026-06-14: 완료. `MeleeAction.cs` Mage 분기 travelTicks: `clamp(round(dx/2),2,10)` → `max(MinTravelTicks, ceil(2D dist / ProjectileSpeedPerTick))`. 수평 dx→2D distance(클라 distance와 정합), round→ceil(단조), `MaxTravelTicks`(10) 제거(사거리 bound라 안전, 클라 속도 폭증 원인 봉합). ImpactTick/S_ProjectileLaunch.travelTicks/데미지 보존, wire 0. 테스트 `ExpectedTravelTicks` 헬퍼 갱신(ceil+2D+optional Y) + 신규 `Mage_TravelTicks_MonotonicallyIncreasing_NoUpperBoundSpike`. WSL2 build 0/0 + test **570/0/5**.
