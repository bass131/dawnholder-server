---
owner: youngho
milestone: M5
phase: 18
title: 일반몹 피격 이펙트 wiring
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 18: 일반몹 피격 이펙트 wiring

> **상태**: pending
> **마일스톤**: M5
> **등급**: 단순 (unity-asset — 이펙트 prefab/Resources 바인딩)
> **담당**: youngho (client)

---

## 🎯 목표

Phase 17에서 일반몹이 `S_EnemyAttack`을 broadcast하면 클라가 이미 그 패킷을 받는다. 이 Phase는 그 공격에 맞춰 **일반몹 종류별 피격 이펙트**(slime / golem)를 재생하도록 결선한다. 에셋은 이미 보유(`Art/damage_effect/{slime,golem}/`)라 placeholder 불필요 — 매핑만 연결한다.

---

## ⏪ 사전 조건

- [ ] Phase 17 완료 — `S_EnemyAttack`의 `attackPattern` 값이 일반몹별로 채워져 들어옴 (분기 키).

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scripts/Network/Handlers/Combat/EnemyAttackHandler.cs` — `attackPattern` 값 → slime/golem 분기 추가.
- [ ] `Combat/Effects/BossAttackEffectSpawner.cs` — 매핑 확장(slime/golem `damage_effect` 추가).
- [ ] 에셋 바인딩은 **하드코딩 X** — `[SerializeField]` 슬롯 또는 well-known `Resources.Load` 경로로 노출 (swap-ready 규율).

---

## ✅ 완료 조건 (정량)

- [ ] slime 공격 시 slime `damage_effect` 재생 (육안).
- [ ] golem 공격 시 golem `damage_effect` 재생 (육안).
- [ ] Unity 컴파일 0err (MCP 자동 검증).
- [ ] 에셋: `Art/damage_effect/{slime,golem}/` 사용 (보유 확인).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err (기능 검증은 아침 육안).
**수동**: 영호 Play — slime/golem에 맞을 때 종류별 이펙트가 맞게 뜨는지.

---

## 📚 학습 포인트

- **이펙트 분기 = 데이터 키(attackPattern) 기반** — 클라 코드는 "어떤 이펙트"를 직접 결정하지 않고 서버가 보낸 `attackPattern`을 룩업 테이블 키로 쓴다 (서버 권위 정합, 클라는 렌더러).
- **에셋 바인딩 분리** — Resources 경로/슬롯으로 노출하면 아트 교체 시 코드 무변경 (swap-ready 정신, 보유 에셋도 동일 규율).

---

## ⚠️ 함정 / 주의사항

- 에셋 보유라 placeholder는 불필요. 단 **바인딩 규율**(Resources 경로/SerializeField 슬롯 분리)은 유지 — 추후 이펙트 교체 대비.
- 슬롯 분리: slime/golem 이펙트를 한 변수에 섞지 말고 패턴별 명확히 매핑.

---

## ➡️ 다음 Phase

- Phase 19 — 보스 찌르기(stab) 이펙트 wiring (에셋 있는데 매핑 누락).

---

## 📋 박제 (완료 후 -DONE.md)

- 단순 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
