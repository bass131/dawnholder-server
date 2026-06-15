---
owner: youngho
milestone: M5
phase: 19
title: 보스 찌르기(stab) 이펙트 wiring
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5h
---

# Phase 19: 보스 찌르기(stab) 이펙트 wiring

> **상태**: pending
> **마일스톤**: M5
> **등급**: 단순 (unity-asset — prefab 매핑)
> **담당**: youngho (client)

---

## 🎯 목표

보스 찌르기(stab) 이펙트 prefab(`Boss_Stabbing_Effect.prefab`)은 이미 에셋으로 있는데 스포너 매핑이 누락돼 안 뜬다. 이 Phase는 그 **매핑 한 줄을 추가**해 보스가 찌르기 공격할 때 이펙트가 재생되게 한다.

---

## ⏪ 사전 조건

- [ ] 없음 (독립 Phase). `NotifyStrike`는 이미 결선 — prefab 매핑만 빠짐.

---

## 📝 작업 내용

- [ ] `Combat/Effects/BossAttackEffectSpawner.cs` — `Boss_Stabbing_Effect.prefab` 매핑 추가 (stab attackPattern → 해당 prefab).
- [ ] prefab 참조는 `[SerializeField]` 슬롯 또는 well-known Resources 경로 (바인딩 분리 규율).

---

## ✅ 완료 조건 (정량)

- [ ] 보스 stab attack 시 Stabbing 이펙트 표시 (육안).
- [ ] Unity 컴파일 0err.
- [ ] 에셋: `Prefabs/Effect/Boss_Stabbing_Effect.prefab` 사용 (보유 확인).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동**: 영호 Play — 보스 찌르기 시 Stabbing 이펙트 확인.

---

## 📚 학습 포인트

- **"에셋 있는데 안 뜸" = 결선 누락** — 기능이 죽어 보여도 로직(`NotifyStrike`)은 살아있고 매핑만 빠진 경우가 흔하다. 신규 구현 전에 결선 상태부터 실측 (Plan 에이전트 실측 정정 정신).

---

## ⚠️ 함정 / 주의사항

- `NotifyStrike`는 이미 결선됨 — 새 로직 추가 X, **prefab 매핑만**. 로직을 건드리면 보스 공격 흐름이 회귀할 수 있음.

---

## ➡️ 다음 Phase

- Phase 20 — 마을 NPC 배치 + E키 대사.

---

## 📋 박제 (완료 후 -DONE.md)

- 단순 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
