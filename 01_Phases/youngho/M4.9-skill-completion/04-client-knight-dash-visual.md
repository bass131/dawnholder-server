---
owner: youngho
milestone: M4.9
phase: 04
title: Knight Dash 클라 연출 — 찌르기 대쉬 모션 + force-adopt 렌더 + hitEffect=3
status: pending
grade: 보통
risk: unity-asset
estimated: 2h
domain: client
---

# Phase 04: Knight Dash 클라 연출

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 보통 (prefab/anim = unity-asset 위험 깃발 인지)
> **담당**: 영호(모션/이펙트) + client Worker(wiring)
> **의존**: Phase 03 (Dash 서버 로직 + S_SkillCast(Dash) + S_HitResult hitEffect=3)

---

## 🎯 목표

Phase 03이 서버에서 굴린 Knight Dash를 **클라 화면에 보이게** 한다. Knight_Attack1 모션을 재활용한 찌르기 대쉬 + Dash 이펙트 + hitEffect=3 임팩트를 연결하고, 대쉬 이동이 **force-adopt 렌더**(기존 lunge 채널)로 부드럽게 보이는지 확인한다. 이 Phase가 끝나면 2클라에서 시전자와 원격 양쪽이 대쉬 이동+이펙트를 일치하게 본다.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 — 서버가 S_SkillCast(skillId=Dash) + S_HitResult(hitEffect=3) 송신
- [ ] 영호 제작 에셋 확인: `03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/` (Dash Skill + Hit Effect)
- [ ] M4.7 Knight lunge force-adopt 렌더 경로 확인 (대쉬 이동이 같은 채널로 들어옴)

---

## 📝 작업 내용

- [ ] **찌르기 대쉬 모션** — Knight_Attack1 모션 재활용해 대쉬 시 재생 (S_SkillCast(Dash) 수신 시 트리거). 캐스팅 commit window 연동
- [ ] **Dash 이펙트 prefab** — `Dash_Skill_Effect` 포장(`Resources/Effects/...` 경로 + `EffectLifetime`), S_SkillCast(Dash) 수신 시 caster 위치 재생
- [ ] **임팩트 prefab** — `Dash_Hit_Effect` 포장, hitEffect=3 디코드 시 target 위치 재생 (HitResultHandler에 hitEffect==3 분기 추가)
- [ ] **SkillCastHandler** — skillId=Dash 분기 추가 (현재 Thunderbolt만 처리). caster 모션/이펙트 라우팅
- [ ] **force-adopt 렌더 확인** — 대쉬 이동이 서버 위치를 force-adopt로 따라가는지(기존 lunge 채널 재활용) + rubber-band 없는지 검증

---

## ✅ 완료 조건 (정량)

- [ ] **2클라 실측** — 시전자 화면: 대쉬 모션 + 전방 이동 + Dash 이펙트 + (적 있으면) 임팩트
- [ ] **2클라 실측** — 원격 화면: 같은 대쉬 이동+이펙트가 동일하게 보임 (한쪽만 보이면 실패)
- [ ] **rubber-band 0** — 대쉬 중/후 위치가 튀거나 되돌아오지 않음 (force-adopt 정상)
- [ ] Unity 콘솔 error CS 0 + Dash 관련 "prefab 미존재" warn 0

---

## 🧪 테스트

**자동**:
- 없음 (에셋 연결 + 핸들러 분기). Unity 콘솔 0에러가 게이트.

**수동**:
- 2클라: A(Knight)가 Dash 키 → A·B 화면에서 대쉬 이동+이펙트 일치 + 경로 적 임팩트 + HP 감소

---

## 📚 학습 포인트

- **force-adopt = 권위 위치 강제 수용**: 로컬 플레이어의 일반 이동은 prediction+reconcile이지만, 대쉬처럼 "서버가 갑자기 큰 속도를 주입한" 이동은 클라가 예측 못 한다(서버 권위 채널). 그래서 클라는 예측 대신 서버 위치를 **그대로 받아 렌더(force-adopt)**한다. M4.7 lunge가 같은 패턴 — 대쉬는 더 큰 값일 뿐.
- **모션 재활용 vs 신규 제작**: Knight_Attack1 찌르기 모션을 대쉬에 빌려 쓰는 trade-off — 신규 애니 제작 비용 0, 단점은 공격과 대쉬가 비슷해 보일 수 있음. 발표 일정상 재활용이 합리적.
- **hitEffect byte 디코드 분기**: 같은 S_HitResult 패킷이 effect 값(0~3)으로 다른 VFX를 라우팅한다. 패킷을 늘리지 않고 byte 하나로 연출을 분기 — Phase 01 ThunderVolt와 동형 패턴.

---

## ⚠️ 함정 / 주의사항

- **unity-asset 위험 깃발** → 등급 상향 인지. 영호 직접(모션/이펙트), AI는 wiring + 검증.
- 대쉬 이동을 클라가 **예측 스폰**하면 서버 force-adopt와 충돌해 rubber-band가 난다 — 클라는 서버 위치만 따라간다(예측 X).
- SkillCastHandler가 현재 Thunderbolt만 처리하므로 Dash 분기를 빼먹으면 모션/이펙트가 조용히 안 나온다(데미지는 들어옴).

---

## ➡️ 다음 Phase

- Phase 05/06 (Teleport) 병렬 진행 중 → 둘 다 끝나면 Phase 07(쿨다운 UI + 마감)

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message. unity-asset 상향 시 간단 -DONE 고려.

---

## 작업 로그

- 2026-06-10: 계획 작성
