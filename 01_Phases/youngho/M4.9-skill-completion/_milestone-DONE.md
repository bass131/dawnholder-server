---
owner: youngho
milestone: M4.9
phase: milestone-closeout-retroactive
title: 스킬 시스템 완성 — 비주얼 + 클래스 게이트 + Knight Dash + Mage Teleport + 쿨다운 UI
status: done
grade: 복잡
summary: M4.9(스킬 시스템 완성, 원래 대규모) 7 Phase를 마일스톤 -DONE 1장으로 사후 회수. 7 Phase 전부 구현 완료 — 비주얼 prefab 연결 / SkillCatalog 클래스 게이트 / Knight Dash 서버·클라 / Mage Teleport 서버·클라(VFX prefab 배치 포함, 실측 정정) / 쿨다운 UI(M4.12 P01) + 전체 회귀·재빌드(M4.12 P02). 보류 0.
created: 2026-06-12
completed: 2026-06-12
domains: [shared, server, client, qa]
---

# M4.9 — 스킬 시스템 완성 마일스톤 박제 (사후 회수)

**마감 일자**: 2026-06-12 (소급 박제 — 구현은 6/10~6/11 진행, M4.12에서 회수)
**Phase 수**: 7/7 완료 (01 비주얼 / 02 게이트 / 03 Dash서버 / 04 Dash클라 / 05 TP서버 / 06 TP클라 / 07 쿨다운UI+회귀)
**등급 표기**: 회수 박제 = 복잡(경량) — *M4.9 작업 자체는 대규모*(3+도메인 스킬 시스템 완성)였으나, 이미 전부 구현된 상태의 *사후 1장 요약*이라 5단계 보고는 발표 시연으로 갈음(영호 "마일스톤 1장 흡수" 결정).
**WORK-ID**: M4.9-skill-completion

## TL;DR

M4.8이 세운 스킬 뼈대(C_SkillUse/S_SkillCast/hitEffect) 위에, M4.9는 **두 클래스가 각자 스킬을 화면 이펙트와 함께 쓰고 서로의 스킬은 막히는** 완성 단계를 박았다. 7 Phase 전부 구현 완료 — -DONE이 0장이라 미박제였던 것을 M4.12 P02에서 마일스톤 -DONE 1장으로 회수. **실측 정정**: work-pin이 "Teleport VFX 미배치"로 알고 있었으나, 실측 결과 `TeleportDepart/Arrive.prefab`이 6/11 이미 배치 완료(빈 껍데기 아님) → 보류 항목 0으로 정정.

## AC 검증 결과

**Phase별 완료 회수 (file:line 실측):**

| # | Phase | 완료 증거 | 상태 |
|---|---|---|---|
| 01 | 스킬 비주얼 prefab 연결 | `Resources/Effects/`에 `LightningStrike`(낙뢰)·`Projectile`/`ProjectileImpact`(평타 투사체) prefab 연결 — placeholder warn 제거 | ✅ |
| 02 | 클래스↔스킬 게이트 | `98_Shared/GameData/SkillCatalog.cs` 단일 진실(skill→class) + 서버 silent drop + 클라 입력 거울. Knight→Thunderbolt 시전 차단 | ✅ |
| 03 | Knight Dash 서버 | `ExternalVelX` 채널 서버 권위 대쉬 + 봇 `DashSmoke` success=True | ✅ |
| 04 | Knight Dash 클라 연출 | `Resources/Effects/DashSkill.prefab`·`DashHit.prefab` 연결 (hitEffect=3 대쉬 임팩트) | ✅ |
| 05 | Mage Teleport 서버 | `ProcessTeleport`(SkillSystem) 서버 권위 위치 점프 + 경계 clamp(§3) + 봇 `TeleportSmoke` success=True | ✅ |
| 06 | Mage Teleport 클라 | 입력·핸들러·보간 끊기(force-adopt 스냅 + 원격 버퍼 reset) + **VFX prefab 배치 완료**(`TeleportDepart/Arrive.prefab` 3796B, SpriteRenderer+Animator+EffectLifetime, 6/11 18:19) | ✅ |
| 07 | 쿨다운 UI + 전체 회귀/마감 | `SkillHudController`(M4.12 P01) + 전체 회귀·발표 재빌드(M4.12 P02) — 본 Phase가 M4.12로 분리·회수됨 | ✅ |

**스킬 기능 회귀 검증 (M4.12 P02 봇 회귀로 갈음):**
- `DashSmoke` ✅ / `TeleportSmoke` ✅ / `ThunderboltAoeSmoke` ✅ / `FreezeSmoke` ✅(fresh) / `RangedHitSmoke` ✅ / `RangedWhiffSmoke` ✅ / `WhiffSwingSmoke` ✅ — 스킬 전 시나리오 green.
- WSL2 `dotnet test` 561 passed / 0 failed (클래스 게이트·Dash·Teleport 불변식 포함).
- `ProtocolVersion` = enum append-only(Dash=2/Teleport=3)로 bump 불필요 가정 유지 — 현재 **v12**(M4.11 P1에서 별도 사유로 11→12, M4.9 스킬은 wire 모양 무변경).

## 결정 흐름

- **회수 박제 입자 = 마일스톤 -DONE 1장 흡수**(영호 결정): 7 Phase가 이미 다 구현된 상태 → 개별 7장 사후 회수는 형식 채우기 부담만 큼. 1장에 7 Phase 결과 + 회귀 증거 흡수.
- **★Teleport VFX "미배치" → "배치 완료" 실측 정정**: work-pin·재구성 plan(6/12)이 "VFX 프리팹 미배치"로 박았으나, 박제 직전 실측에서 `TeleportDepart/Arrive.prefab`(6/11 배치, 완전한 컴포넌트 구성)을 발견 → 보류 항목 0으로 정정. "박제 = 지금 시점 실상태 정직" 원칙이 stale 정보의 거짓 박제를 막은 사례.
- **잔존 stale 1건(코드 무관)**: `SkillCastHandler.cs:27` 주석이 "영호가 2경로로 복제 배치 예정"으로 옛 상태 그대로 — 프리팹은 이미 배치됐으므로 로드 경로(`Effects/TeleportDepart`)는 정상 작동(warn 0). 주석만 정정 대상(사소, 코드 동작 무영향 — 영호 판단).
- **이동 스킬도 서버 권위(§1)**: Dash=ExternalVelX 재활용 / Teleport=서버 위치 점프 + 경계 clamp. 클라는 force-adopt 렌더만. 클래스 불일치 시전 = silent drop + cheat-flag(§3).

## 학습 일지 후보 키워드

- 박제 = 지금 시점 실상태 정직 — recalled/pin 정보가 stale일 수 있음, 박제 전 file:line 실측이 거짓 박제 차단
- 미박제 마일스톤 소급 회수 = 마일스톤 -DONE 1장 흡수(개별 Phase -DONE 7장 대신 경량)
- SkillId enum append-only(Dash=2/Teleport=3) = 패킷 모양 무변경 → ProtocolVersion bump 불필요
- 클래스↔스킬 게이트 = 98_Shared SkillCatalog 단일 진실, 서버 silent drop 권위 + 클라 입력 거울(§1/§3)
- Teleport 보간 끊기 = 로컬 force-adopt 즉시 스냅 + 원격 보간 버퍼 reset(슬라이드 뭉갬 방지)
- 코드 주석 stale = 프리팹 배치 후 "배치 예정" 주석 잔존 — 동작 무관하나 정직성 위해 정정 대상

---

> M4.9 마감 (사후 회수). 발표 데모 = 두 클래스 각자 스킬(Knight Dash / Mage Thunderbolt+Teleport) + 이펙트 + 쿨다운 HUD 완비.
