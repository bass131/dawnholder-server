---
owner: youngho
milestone: M4.4
phase: 05
title: 직업 장착 구조 — ClassConfig SO + IAttackStrategy + AnimatorDriver 교체
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~5h
domain: client
summary: 캐릭터 선택값으로 Animator·이동값·공격 전략이 데이터 장착되는 구조 + Mage 투사체 시각 연출
---

# Phase 05: 직업 장착 구조

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 복잡 (1 도메인이지만 prefab+SO 자산 동반 — unity-asset 깃발)
> **담당**: client SubAgent (코드) + 본인 (ClassConfig 에셋 생성·prefab 연결·체감 튜닝 — Unity 외관 분담)

---

## 🎯 목표

캐릭터 선택(PlayerPrefs) 값으로 LocalPlayer에 **Animator controller + 이동 파라미터 + 공격 전략이 데이터로 장착**된다 (조작 코드 if-분기 0). 전사는 Knight 모션+근접, 원거리는 Mage 모션+투사체 연출. M4.3 Phase 11 wiring(Knight 점프체인/공격랜덤)을 실제 게임 흐름에서 처음 관측한다.

---

## ⏪ 사전 조건

- [ ] Phase 04 — LPC 분할 + `IAttackStrategy` 인터페이스 + 이동값 주입 통로

---

## 📝 작업 내용

**코드 (AI)**
- [ ] `ClassConfig` ScriptableObject 정의 — CharacterClass / AnimatorController 참조 / 공격 전략 prefab 또는 타입 / (이동값은 98_Shared PlayerStats factory 조회 — SO에 중복 보유 금지, drift 차단)
- [ ] spawn 시 장착 로직 — PlayerPrefs class → ClassConfig lookup → `Animator.runtimeAnimatorController` 교체 + 전략 컴포넌트 장착 + Movement에 이동값 주입
- [ ] `KnightMeleeAttack` — 현 nearest-target `C_Attack` 그대로
- [ ] `MageRangedAttack` — 같은 `C_Attack` + 투사체 **시각 연출만** (target까지 날아가는 스프라이트 + 도달 소멸. 판정/데미지는 서버 — 헌법 #1. 물리·관통·다단히트 없음)
- [ ] `PlayerAnimatorSync` 제거 → `LocalPlayerMotion`+`AnimatorDriver` 부착 (08b 계약 완성 — 중복 해소)
- [ ] RemotePlayer prefab에 Knight controller 기본 연결 (원격 직업 구분은 M4.5 S_PlayerJoin append 후 — 임시 Knight 고정 명시)

**자산 (본인)**
- [ ] ClassConfig 에셋 2개 생성 (Knight/Mage) + controller 참조 연결
- [ ] LocalPlayer/RemotePlayer prefab 컴포넌트 정리 + 백업
- [ ] Mage 투사체 스프라이트/연출 (본인 제작 or 기존 자산)

---

## ✅ 완료 조건

- [ ] 캐릭터 선택 → Town 진입 시 직업별 외관/모션 Play 정상 (전사=Knight, 원거리=Mage)
- [ ] **M4.3 이월 관측 체크리스트 통과**: Knight 점프 Start→Peek 유지(핑퐁 0) / 공격 두 모션 랜덤 혼합 / 피격·사망 모션
- [ ] Mage 기본 공격 시 투사체 연출 + 서버 판정 정상 (S_HitResult 데미지 일치)
- [ ] 조작 코드 전체에 직업 if/switch 분기 0 (grep 검증)
- [ ] RemotePlayer 애니 동작 (animState byte 수신 — Knight 고정)

---

## 🧪 테스트

**자동**: 장착 로직 EditMode 테스트 (class → config lookup 정합)
**수동**: 직업 2종 각각 풀 플레이 (이동/점프/공격/피격/사망) + 2클라 원격 표시

---

## 📚 학습 포인트

- **데이터 주도 분기** — if-분기를 ScriptableObject lookup으로 치환 (OCP: 새 직업 = SO 1개 추가)
- **시각 연출과 권위 판정의 분리** — 투사체가 "이펙트"일 뿐 판정이 아닌 이유 (서버 lag-comp가 이미 판정 완료)
- ScriptableObject 운영 — 에셋 참조 vs 코드 상수의 trade-off

---

## ⚠️ 함정 / 주의사항

- **이동값을 SO에 중복 보유 금지** — 98_Shared `PlayerStats`가 단일 출처. SO엔 시각 자산 참조만 (drift = 영구 mispredict)
- prefab 수정 = unity-asset 깃발 — 백업 의무
- `Resources.Load` 경로 박을 거면 폴더 규약 명시 (또는 Bootstrap에서 SerializeField 참조 — ADR-027 코드 주도 Bootstrap 정합 검토)
- 투사체 연출이 lag로 도달 전에 S_HitResult 먼저 올 수 있음 — 데미지 텍스트는 기존 흐름 유지, 연출은 독립 (동기화 시도 금지)

---

## ➡️ 다음 Phase

- Phase 06 — 회귀 + 마감

---

## 📋 박제 (완료 후)

- **복잡 등급** — -DONE.md 박음

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
