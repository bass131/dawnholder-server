---
owner: youngho
milestone: M4.15
phase: 02
title: 히트박스 X/Y 분리 + 전 스킬 Y 재튜닝
status: pending
grade: 복잡
domain: server
summary: GetAttackHitbox 정사각→비정사각(X/Y), Mage/Knight 박스 X/Y 분리 + Thunderbolt/Dash Y 재튜닝
---

# Phase 02: 히트박스 X/Y 분리 + 전 스킬 Y 재튜닝

> **상태**: pending
> **마일스톤**: M4.15
> **등급**: 복잡 (구조 키스톤 — 공유 헬퍼 시그니처 변경 + 다중 호출부/테스트)
> **담당**: server (Sonnet Worker)

---

## 🎯 목표

`CombatSystem.GetAttackHitbox`가 만드는 **정사각형 AABB**(`new Vector2(half, half)`)를 **X/Y 별도 half-extent**로 분리해, 사이드스크롤에 맞게 공격 박스를 *납작하게*(X 넓고 Y 얇게) 만든다. Mage 평타가 위/아래 층 적을 때리는 문제를 근본 봉합하고, 같은 원칙을 전 스킬(Thunderbolt/Dash) Y에 적용한다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (baseline green + 시작값 표 영호 승인).

---

## 📝 작업 내용

- [ ] `CombatConstants.cs`: `MageAttackHalfExtent`(단일 8.0) → `MageAttackHalfX`/`MageAttackHalfY` 분리. Knight도 `AttackHalfExtent`(1.5) → `KnightAttackHalfX`/`KnightAttackHalfY`(또는 `AttackHalfX/Y`). 값 = Phase 01 영호 승인값.
- [ ] `CombatSystem.GetAttackHitbox(origin, cls)` 시그니처: 정사각 `(half, half)` → 클래스별 `(halfX, halfY)` 반환. 주석 `(4.0f)` stale도 정정(실제 8.0).
- [ ] `ThunderboltBoxHalfY`(3.0) → 영호 승인값으로 재튜닝. X(13)는 광역 유지.
- [ ] `DashBoxHalfY`(1.5) → 영호 승인값으로 재튜닝(필요 시).
- [ ] 호출부 정합 확인: `MeleeAction.cs:28`(GetAttackHitbox), `ThunderboltAction.cs:23-26`, `DashAction.cs:38-41` — `ResolveImpactTargets`는 이미 `(x,y)` halfExtents 인자라 호출만 갱신.
- [ ] 테스트 갱신: `HitboxTests`, `MageRangedCombatTests`(사거리 ±8 가정 → 신규 X/Y), `KnightDashTests`, `ThunderboltSkillTests`(박스 Y 가정). **Y범위 회귀 테스트 신규**: 위층 적(같은 X, Y=층간격 초과)이 평타에 *miss* 되는지.

---

## ✅ 완료 조건

- [ ] `GetAttackHitbox`가 X≠Y 박스 반환 (정사각 소멸).
- [ ] 신규 테스트: Mage 평타가 X 사거리 내 *같은 층* 적은 hit, Y 층간격 초과 적은 miss.
- [ ] Thunderbolt/Dash Y 재튜닝값 반영 + 기존 hit 테스트 갱신 green.
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 회귀 0).
- [ ] reviewer 헌법 hard 위반 0 (특히 헌법 #1 — 서버 권위 판정 로직 보존).

---

## 🧪 테스트

**자동**:
- `HitboxTests` — 비정사각 박스 ∩ 적 1×1.
- `MageRangedCombatTests` — X 사거리 hit / Y 층 초과 miss (신규).
- `ThunderboltSkillTests`/`KnightDashTests` — Y 재튜닝 반영.

**수동**: 영호 Play — Mage 평타가 위/아래 층 적 안 때리는지 육안.

---

## 📚 학습 포인트

- **사이드스크롤 히트박스 = 비대칭** — 정사각은 탑다운 발상. 2D 횡스크롤은 X(사거리)와 Y(층 분리)가 전혀 다른 의미라 분리가 정석.
- **AABB 교차 판정** — `box.Intersects(enemy.Hitbox)`에서 enemy는 1×1(`HitboxHalfExtent=0.5`). 공격 박스 Y가 클수록 위층 1×1과도 겹침 → Y가 층 분리의 키.
- **공유 헬퍼 시그니처 변경 파급** — `GetAttackHitbox` 한 줄 바꾸면 호출부·테스트가 줄줄이. 키스톤 변경의 ripple 관리.

---

## ⚠️ 함정 / 주의사항

- **헌법 #1** — 박스 *크기*만 바꾸고 명중 *판정 로직*(rewind, AABB 교차, 서버 권위)은 1줄도 안 바꿈. 데미지 적용 경로 보존.
- 박스 origin은 그대로(중심=rewind 위치). Y 비대칭 시 origin Y가 캐릭터 중심인지 발밑인지 확인 — 적이 발판 위에 서면 Y 중심 정렬 점검.
- `MageAttackHalfExtent` 참조처 전수 grep (테스트 주석 포함) — 단일 상수 분리 시 dangling 참조 0.

---

## ➡️ 다음 Phase

- Phase 03 — freeze 적용 제거.

---

## 📋 박제 (완료 후)

- 복잡 등급 → `02-hitbox-xy-split-DONE.md` (요약 + 사실 박제 + 학습 키워드). HTML은 마일스톤 마감에서 종합.

---

## 작업 로그

- 2026-06-14: 생성.
