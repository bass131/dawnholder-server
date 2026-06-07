---
owner: youngho
milestone: M4.5
phase: 05
title: 보스 클라 연출 + 원격 직업 표시 + HP 실연결 + Mage 투사체
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~4h
domain: client
---

# Phase 05: 보스 클라 연출 + 원격 직업 표시

> **상태**: pending
> **마일스톤**: M4.5
> **등급**: 복잡 (client 단일 + prefab/씬 — unity-asset 상향 인지)
> **담당**: client SubAgent + unity-bridge (이펙트/prefab) + 메인 검수

---

## 🎯 목표

Phase 04 패킷을 클라가 소비한다: `S_EnemyAttack` 핸들러(피격 연출 + **HP 바 실연결 — mock 은퇴**), 보스 telegraph/패턴별 이펙트, `S_PlayerJoin.characterClass`로 **원격 플레이어가 상대 직업 모습**으로 보이게, 그리고 이월된 **Mage(Ranger) 투사체 연출**. 끝나면 보스방 양방향 전투가 화면에서 완성된다 — 발표 데모 클라이맥스.

---

## ⏪ 사전 조건

- [ ] Phase 04 완료 (S_EnemyAttack + characterClass + v9)
- [ ] Phase 01 완료 (보스 prefab — 이펙트 부착 지점)
- [ ] Phase 03 완료 (HudController 정비 — UpdateHP 호출처)

---

## 📝 작업 내용

- [ ] `S_EnemyAttack` 핸들러: `HudController.UpdateHP(targetCurrentHp, max)` 호출 — **mock 초기값 은퇴** (`_mockHpCurrent` Start 호출 제거, 서버 값 단일 출처) + 피격 플래시/넉백 없는 시각 표시
- [ ] 보스 공격 이펙트: `attackPattern(byte)` 분기로 패턴별 연출 + telegraph 표시 (서버 animState 예고 틱 소비) — 서버는 논리, 클라는 표현
- [ ] 플레이어 사망→리스폰 연출: 리스폰 스냅 시 짧은 페이드(이질감 봉합 — M4.2 lifecycle 학습 재사용)
- [ ] 원격 플레이어 직업 표시: `S_PlayerJoin.characterClass` → `ClassConfig` lookup으로 원격 prefab Animator 장착 (M4.4-05 로컬 패턴의 원격 버전, 기존 원격 = 단일 모습 은퇴)
- [ ] **Mage 투사체 연출** (M4.4-05 이월): Ranger 공격 시 투사체 시각 효과 — *시각 전용*, 판정은 기존 서버 AABB 그대로 (헌법 #1 — AttackVariant 랜덤 선례)
- [ ] EditMode: characterClass → ClassConfig lookup 분기 + 미유효 폴백

---

## ✅ 완료 조건

- [ ] 보스에게 맞으면 HP 바 실시간 감소 (mock 코드 0줄 — `_mockHp*` 필드 은퇴)
- [ ] 보스 패턴별 이펙트 + telegraph 표시 Play 실측
- [ ] 플레이어 사망 → 리스폰 → 전투 재개 데모 무중단 Play 실측
- [ ] 2클라 실측: 상대가 상대 직업(Knight/Mage) 모습 + 모션으로 보임
- [ ] Mage 공격 시 투사체 연출 (판정 변화 0 — 서버 코드 diff 0)
- [ ] 클라에 데미지/HP 계산 코드 0 (표시만 — 헌법 #1) + EditMode green

---

## 🧪 테스트

**자동**: EditMode — characterClass lookup/폴백 + 기존 UI 회귀
**수동**: 2클라 Play — 보스전 풀 루프(telegraph → 피격 → HP 감소 → 사망/리스폰 → 처치 → StageClear) + 원격 직업 상호 확인

---

## 📚 학습 포인트

- **패킷의 표현 힌트** — `attackPattern(byte)`: 서버는 "무슨 공격인지"만 알리고 어떻게 보일지는 클라 자유. 논리/표현 분리의 프로토콜 설계
- **mock의 수명** — Phase 03(M3)에 박힌 mock이 세 마일스톤을 살았다. "임시"는 은퇴 시점을 명시해야 임시다
- **시각 전용 연출의 경계** — 투사체가 날아가는 동안 판정은 이미 끝나 있음(서버 즉시 AABB). 연출과 판정의 시간 분리를 체감

---

## ⚠️ 함정 / 주의사항

- **투사체에 판정 욕심 금지** — "투사체가 닿을 때 데미지"로 바꾸면 서버 판정 재설계(발사체 엔티티 + 틱 추적)가 필요한 다른 마일스톤급 작업. 이번엔 시각만
- **mock 은퇴 시 첫 입장 초기값** — 서버 첫 snapshot/입장 패킷의 HP로 초기화 (mock 제거가 "빈 바"로 보이는 사고 방지)
- **원격 직업 장착은 컴포넌트 보존** — 유현 M3 약속(비주얼 교체 시 핵심 컴포넌트 보존) 준수, RemoteEntity/보간 건드리지 않기
- prefab/이펙트 작업 전 백업 의무

---

## ➡️ 다음 Phase

- Phase 06 — 회귀 + 마감

---

## 📋 박제 (완료 후)

- **복잡 등급** — `05-boss-client-and-remote-class-DONE.md` 박음

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18 — HP 실연결 위치를 본 Phase로 확정[S_EnemyAttack 의존] + Mage 투사체 이월 흡수)
