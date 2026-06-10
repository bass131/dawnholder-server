---
owner: youngho
milestone: M4.9
phase: 05
title: Mage Teleport 서버 — SkillId.Teleport + 위치 점프 + 맵 경계 clamp
status: pending
grade: 복잡
risk: trust-boundary (텔레포트 거리·경계 clamp 서버 권위)
estimated: 2.5h
domain: shared+server+qa
---

# Phase 05: Mage Teleport 서버

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 복잡 (shared enum/상수 + server 위치 점프 + qa 봇)
> **담당**: server Worker(Sonnet) + qa
> **의존**: Phase 02 (클래스 게이트/카탈로그). Phase 03·04(Dash)와 병렬 가능.

---

## 🎯 목표

Mage 전용 Teleport 스킬을 **서버 권위 위치 점프**로 구현한다. C_SkillUse(skillId=Teleport) 수신 → 쿨다운 검증 → facing 방향으로 고정 거리만큼 위치를 점프(맵 경계 clamp) → S_SkillCast(skillId=Teleport) broadcast. 이 S_SkillCast가 클라에게 "보간 끊고 스냅하라"는 신호가 된다(클라 처리는 Phase 06). 이 Phase가 끝나면 봇에서 Mage가 텔레포트 후 정확한 기대 위치로 점프한다.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 — SkillCatalog에 Teleport→Mage 매핑 + 서버 클래스 게이트 동작
- [ ] 맵 경계(좌우 한계) 좌표를 서버가 알 수 있는지 확인 (clamp 대상)
- [ ] PlayerEntity 위치 직접 set 경로 확인 (점프 = 위치 즉시 변경, 속도 채널 아님)

---

## 📝 작업 내용

**shared (98_Shared/GameData)**:
- [ ] `SkillId.Teleport = 3` **append** (enum 끝, append-only — 헌법 §2)
- [ ] Teleport 상수 — 텔레포트 거리 / 쿨다운. 쿨다운은 클라 거울용 `98_Shared/Constants.cs`, 거리는 서버 전용 `CombatConstants.cs` 검토

**server (02_Server)**:
- [ ] `SkillSystem.ProcessSkill`에 `SkillId.Teleport` case → `ProcessTeleport`
- [ ] `ProcessTeleport`: 쿨다운 권위 검증 — **Phase 02에서 확정된 스킬별 쿨다운 구조 사용**(이 Phase에서 자료구조 변경 금지, 올라타기만) + facing 방향 고정 거리 위치 점프. **맵 경계 clamp(범위 검증 필수 — 헌법 §3)** — 점프 목적지가 맵 밖이면 경계로 clamp(밖으로 못 나감)
- [ ] `S_SkillCast{caster, skillId=Teleport, facing}` broadcast — 클라 "보간 끊기" 신호. (목록 없음, 데미지 없음 — 순수 이동 스킬)
- [ ] 텔레포트는 데미지/타격 없음 — DeferredDamage/HitResult 경로 안 탐

**qa (99_Tools/headless-bot)**:
- [ ] `TeleportSmoke` 시나리오 — Mage 봇 텔레포트 → 위치 = 기대값(시작 + facing×거리, 경계 clamp 반영) 확인

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` green — 기존 회귀 **0**
- [ ] 단위 테스트: 텔레포트 후 위치 = 기대값 + **경계 clamp**(맵 끝에서 시전 시 밖으로 안 나감) + 쿨다운 중 재발동 silent drop + Knight가 Teleport 시도 시 거부(Phase 02 게이트)
- [ ] **TeleportSmoke 봇 PASS** — 텔레포트 후 위치 = 기대값, **경계 밖 위치 0건**
- [ ] ProtocolVersion 불변 확인 (`Current==11`, SkillId append = 모양 무변경)
- [ ] Shared.dll 재빌드 → Plugins 갱신

---

## 🧪 테스트

**자동**:
- `MageTeleportTests` — 위치 점프 정확 / 경계 clamp(좌·우 끝) / 쿨다운 차단 / 클래스 게이트(Knight 거부)
- ProtocolVersion Current==11 불변

**수동(봇)**:
- TeleportSmoke: fresh 서버 + Mage 봇, 맵 중앙/끝에서 텔레포트 → 위치 assert

---

## 📚 학습 포인트

- **위치 점프 = 속도 채널과 다름**: Dash는 ExternalVelX(속도)로 "빠르게 미끄러지는" 이동이지만, Teleport는 **위치를 즉시 set**하는 순간이동이다. 속도가 0인 채로 좌표만 바뀐다 — 그래서 클라 보간이 이걸 슬라이드로 오해하는 함정이 생긴다(Phase 06 핵심).
- **경계 clamp = 범위 검증의 한 형태(헌법 §3)**: 클라가 "텔레포트!"만 보내도 *목적지는 서버가 계산*하고, 맵 밖으로 나가는 좌표는 경계로 잘라낸다. 클라가 좌표를 보내게 두면 벽 뚫기/맵 탈출 핵이 된다 — 거리·방향만 서버가 정하고 결과를 clamp.
- **데미지 없는 스킬**: 모든 스킬이 타격을 하는 건 아니다. Teleport는 순수 이동 — DeferredDamage/HitResult 경로를 안 타고 S_SkillCast만 broadcast한다. 스킬 시스템이 "타격 스킬"과 "유틸 스킬"을 둘 다 담을 수 있음을 보여주는 사례.

---

## ⚠️ 함정 / 주의사항

- **trust-boundary**: 거리·방향·경계 clamp 전부 서버 단독. 클라는 skillId만 — 절대 목적지 좌표를 클라가 보내지 않음.
- **쿨다운 분리**: Phase 03과 동일 — 스킬별 쿨다운 분리는 **Phase 02에서 단일 소유로 이미 확정**(skillId 키 맵/스킬별 필드). 이 Phase는 그 구조를 **사용만** 하고 자료구조를 다시 건드리지 않는다(03·05 병렬 충돌 방지).
- 경계 clamp 후에도 텔레포트가 "성립"한 것으로 쿨다운 소비 + S_SkillCast 발사(허공 시전도 의도된 행동 — Thunderbolt 빈 박스 패턴 정합).

---

## ➡️ 다음 Phase

- Phase 06 (Mage Teleport 클라 — 보간 끊기가 이 위치 점프의 시각화 핵심)

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (위치 점프 vs 속도 채널 + 경계 clamp 키워드).

---

## 작업 로그

- 2026-06-10: 계획 작성
