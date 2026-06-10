---
owner: youngho
milestone: M4.9
phase: 03
title: Knight Dash 서버 — SkillId.Dash + ExternalVelX 전방 대쉬 + 경로 타격
status: pending
grade: 복잡
risk: trust-boundary (대쉬 거리·경로 판정·쿨다운 서버 권위)
estimated: 3h
domain: shared+server+qa
---

# Phase 03: Knight Dash 서버

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 복잡 (shared enum/상수 + server 로직 + qa 봇)
> **담당**: server Worker(Sonnet) + qa
> **의존**: Phase 02 (클래스 게이트/카탈로그). Phase 05·06(Teleport)과 병렬 가능.

---

## 🎯 목표

Knight 전용 Dash 스킬을 **서버 권위로 구현**한다. 클라가 C_SkillUse(skillId=Dash)를 보내면 서버가 쿨다운 검증 → M4.7 Knight lunge가 쓰던 `ExternalVelX` 채널로 **전방 대쉬** → 대쉬 경로 위 적을 AABB 타격 → S_SkillCast broadcast + S_HitResult(hitEffect=3). 이 Phase가 끝나면 헤드리스 봇에서 Knight가 대쉬로 전진하며 경로 적을 때린다(클라 연출은 Phase 04).

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 — SkillCatalog에 Dash→Knight 매핑 + 서버 클래스 게이트 동작
- [ ] `PlayerEntity.AttackLungeVx`(ExternalVelX 채널) + `Physics.Step`의 ExternalVelX 적용 경로 확인 (M4.7 lunge 재활용 지점)
- [ ] `CombatSystem.ResolveImpactTargets`(박스 스캔 헬퍼) 재사용 가능 확인

---

## 📝 작업 내용

**shared (98_Shared/GameData)**:
- [ ] `SkillId.Dash = 2` **append** (enum 끝, append-only — 헌법 §2)
- [ ] Dash 상수 — 쿨다운 / 대쉬 거리(또는 초기 ExternalVelX) / 지속 틱 / 데미지 계수. 쿨다운은 클라 게이트 거울용 → `98_Shared/Constants.cs`(Thunderbolt 쿨다운 패턴 정합), range/damage는 서버 전용 `CombatConstants.cs`(least-exposure)

**server (02_Server)**:
- [ ] `SkillSystem.ProcessSkill`에 `SkillId.Dash` case 추가 → `ProcessDash`
- [ ] `ProcessDash`: 쿨다운 권위 검증 — **Phase 02에서 확정된 스킬별 쿨다운 구조 사용**(이 Phase에서 자료구조 변경 금지, 올라타기만) + `ExternalVelX`(=AttackLungeVx 채널)로 facing 전방 대쉬 부여
- [ ] 경로 타격: 대쉬 진행 경로를 `ResolveImpactTargets`(전방 박스)로 스캔 → 각 적 데미지 + `S_HitResult{hitEffect=3}` (즉시 또는 도착 시점 — 대쉬는 짧으니 즉시 적용 검토)
- [ ] `S_SkillCast{caster, skillId=Dash, facing}` broadcast (캐스팅/대쉬 연출 신호)

**qa (99_Tools/headless-bot)**:
- [ ] `DashSmoke` 시나리오 — Knight 봇이 C_SkillUse(Dash) 송신 → 위치 전진 확인 + 경로 적 HP 감소 확인

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` green — 기존 회귀 **0**
- [ ] 단위 테스트: Dash 시 caster ExternalVelX 부여 + 경로 적 데미지 + S_HitResult.hitEffect==3 + 쿨다운 중 재발동 silent drop
- [ ] **DashSmoke 봇 PASS** — Knight 대쉬 후 위치 전진(Δx > 임계) + 경로 적 HP 감소
- [ ] **ProtocolVersion 불변 확인** — 재생성 후 `Current==11` assert + PacketID enum 시프트 0 (SkillId append는 패킷 모양 무변경)
- [ ] Shared.dll 재빌드 → Plugins 갱신

---

## 🧪 테스트

**자동**:
- `KnightDashTests` — ExternalVelX 부여 / 경로 적 타격(hitEffect=3) / 쿨다운 차단 / Mage가 Dash 시도 시 거부(Phase 02 게이트 정합)
- `ProtocolVersionTests` 또는 PacketRoundTrip — Current==11 불변

**수동(봇)**:
- DashSmoke: fresh 서버 + Knight 봇 1, 경로에 적 배치 → 대쉬 → 위치/HP assert

---

## 📚 학습 포인트

- **ExternalVelX 채널 재활용**: M4.7에서 Knight 근접 스윙의 "전방 짧은 lunge"를 위해 만든 `AttackLungeVx`(units/s, 매 틱 감쇠) 채널을 Dash가 그대로 빌린다. 새 이동 시스템을 또 만들 필요 없이 **기존 서버 권위 속도 채널에 더 큰 값을 주입**하면 대쉬가 된다. 클라는 결과 위치만 force-adopt — 채널 값은 모름(헌법 §1).
- **enum append = wire 호환**: `SkillId.Dash=2`를 추가해도 byte 직렬화 모양이 안 바뀐다. 기존 값(0/1)은 그대로, 새 의미만 byte 범위 안에서 늘어난다 → ProtocolVersion bump 불필요. 이게 "append-only가 안전한 변경"인 이유.
- **즉시 적용 vs 지연 데미지 선택**: 썬더볼트는 낙뢰 도착까지 지연(DeferredDamage)했지만, 대쉬는 거리가 짧아 즉시 적용이 자연스러울 수 있다. 둘의 trade-off(연출 타이밍 vs 단순함)를 구현 중 결정.

---

## ⚠️ 함정 / 주의사항

- **trust-boundary**: 대쉬 거리·경로 박스·쿨다운 전부 서버 단독. 클라는 skillId + attackerClientTick만. rewind 범위 검증(음수/미래/200ms 초과)은 Thunderbolt 동형으로 유지.
- **쿨다운 충돌**: 스킬별 쿨다운 분리는 **Phase 02에서 단일 소유로 이미 확정**(`LastSkillTick` 단일 필드 → skillId 키 맵/스킬별 필드). 이 Phase는 그 구조를 **사용만** 하고 자료구조를 다시 건드리지 않는다(03·05 병렬 충돌 방지).
- ExternalVelX 부여 후 감쇠(KnockbackDecayPerTick)가 lunge와 같으면 대쉬가 너무 짧을 수 있음 — 거리 튜닝 Play 대상.

---

## ➡️ 다음 Phase

- Phase 04 (Knight Dash 클라 연출 — 이 서버 로직의 시각화)

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (ExternalVelX 재활용 + enum append 키워드).

---

## 작업 로그

- 2026-06-10: 계획 작성
