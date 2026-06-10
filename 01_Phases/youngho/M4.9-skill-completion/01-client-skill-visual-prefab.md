---
owner: youngho
milestone: M4.9
phase: 01
title: 스킬 비주얼 prefab 연결 — ThunderVolt 낙뢰 + EnergyVolt 평타 투사체
status: pending
grade: 보통
risk: unity-asset (→복잡 상향 인지)
estimated: 2h
domain: client
---

# Phase 01: 스킬 비주얼 prefab 연결 (ThunderVolt + EnergyVolt)

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 보통 (단 prefab 변경 = unity-asset 위험 깃발 → 복잡 상향 인지)
> **담당**: 영호 직접(에디터 prefab/애니) + 메인 세션 MCP 보조/검증
> **의존**: 없음 — 즉시 시작 가능, Phase 02와 병렬.

---

## 🎯 목표

영호가 이미 제작한 비주얼 에셋(ThunderVolt / EnergyVolt / Hit_Effect)을 **prefab으로 포장해 코드가 로드할 수 있게 연결**한다. 이 Phase가 끝나면 Q 썬더볼트 시전 시 **캐스팅 연출 + 각 적 위치의 낙뢰 이펙트**가 화면에 실제로 보이고, Mage 평타가 투사체+임팩트 이펙트로 보인다. 지금은 prefab이 없어 warn만 찍히고 데미지만 들어가는 상태다.

---

## ⏪ 사전 조건

- [ ] M4.8 마감 — C_SkillUse(24)/S_SkillCast(25)/S_HitResult.hitEffect 패킷 + 서버 썬더볼트 로직 동작 중
- [ ] 영호 제작 에셋 존재 확인: `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/` (ThunderVolt, EnergyVolt+Hit_Effect)
- [ ] 클라가 참조하는 로드 경로 = `Resources/Effects/SkillCast`(캐스팅), `Resources/Effects/LightningStrike`(낙뢰), `Resources/Effects/ProjectileImpact`(임팩트) — 코드 상수 정합 확인

---

## 📝 작업 내용

> prefab 포장 = 에디터 작업(영호 직접). 코드 상수 경로와 1:1 맞추는 게 핵심.

- [ ] **캐스팅 연출** `Resources/Effects/SkillCast.prefab` 신설 — ThunderVolt 캐스팅 단계 anim 포장 + `EffectLifetime` 컴포넌트 부착 (`SkillCastHandler.CastEffectPath` 정합)
- [ ] **낙뢰** `Resources/Effects/LightningStrike.prefab` 신설 — ThunderVolt 낙뢰 anim 포장 + `EffectLifetime` 부착 (`HitResultHandler.LightningVfxPath` 정합, hitEffect=2)
- [ ] **Mage 평타 투사체** — `EnergyVolt`를 `ProjectileVisual`이 로드하는 투사체 prefab(로컬/원격)에 연결
- [ ] **평타 임팩트** `Resources/Effects/ProjectileImpact.prefab` — `EnergyVolt_Hit_Effect` 포장 (hitEffect=1)
- [ ] **Channeling 애니메이션** — 캐스팅 commit window 동안 재생되는 Channeling 클립을 Mage Visual animator에 할당 (LocalPlayerInput이 `NotifyChannel`로 트리거하는 모션)
- [ ] 코드 상수 경로 ↔ 실제 Resources 경로 1:1 점검 (오타 = silent warn → 연출 누락)

---

## ✅ 완료 조건 (정량 — "잘 작동" 금지)

- [ ] Q 썬더볼트 시전 시 **캐스팅 연출 + 낙뢰 이펙트**가 화면에 표시됨
- [ ] **Mage 평타 시 투사체가 EnergyVolt 비주얼로 날아가고 적중 시 EnergyVolt_Hit_Effect 임팩트 표시** (warn 0 = 로드 성공일 뿐, 시각 확인 별도)
- [ ] Unity 콘솔에 `[SkillCastHandler] 캐스팅 VFX 미존재` warn **0건**
- [ ] Unity 콘솔에 `[ProjectileLaunchHandler] 투사체 prefab 미존재` warn **0건**
- [ ] Unity 콘솔 `error CS` **0건** (에셋/빌드 안전 게이트)
- [ ] **2클라 실측**: 원격 화면에서도 캐스터의 캐스팅+낙뢰 연출이 동일하게 보임 (한쪽만 보이면 실패)

---

## 🧪 테스트

**자동**:
- 없음 (순수 에셋 연결 — 코드 변경 최소). Unity 콘솔 0에러가 게이트.

**수동**:
- 1클라: Q 눌러 캐스팅 모션 + 낙뢰 + (적 있으면) 평타 투사체+임팩트 확인
- 2클라: A가 Q 시전 → B 화면에서 A의 캐스팅+낙뢰 보이는지 확인 (S_SkillCast broadcast 정합)

---

## 📚 학습 포인트

- **Resources.Load 경로 = 문자열 약속**: prefab 폴더/이름이 코드 상수와 정확히 안 맞으면 컴파일 에러 없이 **런타임에 조용히 null** → warn 후 연출만 빠짐. "에러 없는데 안 보임"의 전형. 경로 오타가 가장 흔한 함정.
- **prefab = anim+controller+컴포넌트의 묶음**: png/anim/controller만으로는 코드가 Instantiate할 수 없다. prefab으로 묶고 `EffectLifetime`(수명 후 자동 Destroy)을 붙여야 메모리 누수 없이 1회성 이펙트가 된다.
- **placeholder 패턴**: M4.8이 일부러 prefab 없이 warn+skip하도록 짜둔 이유 = "연출 없어도 게임 로직(데미지)은 정상". 비주얼은 나중에 끼우는 분리 설계. 이번 Phase가 그 "나중".

---

## ⚠️ 함정 / 주의사항

- **prefab 변경 = unity-asset 위험 깃발** → 등급 자동 상향(보통→복잡 인지). 영호 직접 작업이 원칙, AI는 MCP 보조/검증만.
- 캐스팅 이펙트는 **caster 위치**(`EffectAnchor.ResolvePosition`)에 스폰, 낙뢰는 **각 target 위치**에 스폰 — 두 경로가 다르다. 헷갈려 한 prefab에 몰면 연출이 어긋난다.
- `EffectLifetime` 누락 시 이펙트가 영원히 안 사라져 화면에 쌓인다.

---

## ➡️ 다음 Phase

- Phase 02 (클래스 게이트, 병렬 진행) / Phase 04·06 (Dash·Teleport 비주얼이 같은 포장 패턴 재사용)

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message로 충분 (-DONE.md 생략). 단 unity-asset 상향 시 간단 -DONE 고려.

---

## 작업 로그

- 2026-06-10: 계획 작성
