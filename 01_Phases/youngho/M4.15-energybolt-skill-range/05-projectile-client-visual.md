---
owner: youngho
milestone: M4.15
phase: 05
title: 투사체 클라 정합 + 호밍 polish
status: done
grade: 보통
domain: client
summary: ProjectileVisual 일정 속도 검증 + 움직이는 타겟 호밍 jank 완화 (서버 travelTicks 계약 정합)
---

# Phase 05: 투사체 클라 정합 + 호밍 polish

> **상태**: done (2026-06-14, 클라 무변경 — P04 서버 캐스케이드가 클라 속도 정합. 영호 Play-test "투사체 이상무" 통과)
> **마일스톤**: M4.15
> **등급**: 보통
> **담당**: client (Sonnet Worker)

---

## 🎯 목표

서버의 일정 속도 `travelTicks` 모델(Phase 04)에 클라 투사체 연출을 정합시킨다. `MaxTravelTicks` 상한이 사라지면 클라 거리역산 속도(`distance / (travelTicks·TickDuration)`)가 *자동으로* 일정해지므로 — 이를 **검증**하고, 추가로 움직이는 타겟 호밍의 곡선 jank("엉성한 이동")를 완화한다.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (서버 travelTicks 일정 속도 모델 = 클라 정합 기준).
- [ ] **연출 분기 영호 택1** (진입 전 게이트 — 워커 감 구현 후 재작업 차단): 호밍 jank 완화를 **(a) 발사 시점 방향 고정(직선, 메이플식)** vs **(b) 호밍 보간 부드럽게** 중 영호 선택. "부드러움"은 주관이라 Unity 외관 = 영호 육안 권위.

---

## 📝 작업 내용

- [ ] `ProjectileVisual.SetTravelDuration`(L28-35) 검증: clamp 제거 후 `_speed = distance / duration`이 거리 무관 ~일정인지 확인 (서버 `travelTicks ∝ 거리`라 자동 일정). 필요 시 고정 속도 상수로 명시화(거리역산 의존 축소).
- [ ] `ProjectileLaunchHandler`(L37-105) — spawn 위치/방향/도착 동기 검토. 서버 도착 틱(travelTicks)과 클라 시각 도착 정합.
- [ ] **호밍 jank 완화** (`ProjectileVisual.Update` L53-83): 움직이는 타겟 매 프레임 재조준의 곡선 흔들림 → 발사 시점 방향 고정(직선) 또는 호밍 보간 부드럽게(영호 Play 느낌 기준).
- [ ] (선택) 도착↔`S_HitResult` 이펙트 타이밍 동기 — 투사체 destroy 시점과 impact VFX 정합.
- [ ] EditMode 테스트(가능 시) 또는 컴파일 + Play 검증.

---

## ✅ 완료 조건

- [ ] Unity 컴파일 0err (메인 세션 MCP).
- [ ] 클라 투사체 속도가 거리 무관 ~일정 (먼 적도 순간이동 X) — 영호 Play 육안.
- [ ] 호밍 이동이 부드러움 (엉성한 곡선 흔들림 완화) — 영호 Play 육안.
- [ ] 헌법 #1 보존 — 투사체는 서버 `S_ProjectileLaunch` 확정 후 스폰(선예측 스폰 부활 X, M4.8 기둥1 유지).

---

## 🧪 테스트

**자동**: EditMode (`ProjectileVisual` 속도/도착 순수 로직 분리 가능 시).
**수동**: 영호 Play — 근/원거리 투사체 일정 속도 + 부드러운 비행 체감.

---

## 📚 학습 포인트

- **종속 자동 봉합** — 서버에서 근본(상한)을 고치면 클라 종속 계산이 *코드 변경 없이* 정상화되는 경우가 있음. "어디를 고쳐야 최소 변경으로 최대 효과인가" 사고.
- **호밍 vs 직선** — 유도 투사체의 곡선은 타겟 추적의 부작용. 발사 시점 방향 고정(메이플식)이 더 깔끔할 수 있음 — 연출 trade-off.
- **클라=렌더러** (헌법 #1) — 투사체는 순수 연출, 명중/데미지는 서버. 클라가 도착을 *표현*만.

---

## ⚠️ 함정 / 주의사항

- 선예측 스폰 부활 금지 — M4.8에서 "그림은 맞았는데 데미지 0" 제거 위해 서버 확정 후 스폰으로 바꿈. 그 기둥 유지.
- 원격 entity는 보간 중이라 호밍 타겟 위치가 흔들림 — 서버 도착 틱 기준 정합이 핵심.
- Unity 테스트 stale 함정(carry-over): 신규 EditMode 안 잡히면 DLL mtime 확인 + deferred 컴파일 에러 의심 (영호 Test Runner Run All).

---

## ➡️ 다음 Phase

- Phase 06 — 회귀 + 봇 시나리오 갱신 + 마일스톤 마감.

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message만. (client unity-asset 깃발 미발동 — 스크립트만, prefab 무변경.)

---

## 작업 로그

- 2026-06-14: 생성.
- 2026-06-14: 완료(클라 무변경). 실측 결론 = `ProjectileVisual.SetTravelDuration`이 속도를 `distance/(travelTicks×TickDuration)`로 서버 travelTicks 역산 → P04(ceil+2D)가 travelTicks를 정리하자 **클라 속도 자동 정합**(도착 틱 일치). truly-constant은 ProjectileSpeedPerTick의 98_Shared 이전 필요(co-review 마찰 + 클라 헌법 "balance 하드코딩 금지")라 플랜 스코프 밖 보류. 영호 Play-test "투사체 이상무" 통과 → 클라 코드 손 안 댐(YAGNI). 호밍 reliable 유지.
