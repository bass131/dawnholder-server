---
owner: youngho
milestone: M4.12
phase: milestone-closeout
title: 발표 데모 빌드 준비 — 스킬 쿨다운 HUD + 발표 재빌드 + 마감 박제
status: done
grade: 복잡
summary: M4.12(경량 발표 빌드 준비) 2 Phase 완전 마감. P01 스킬 쿨다운 UI HUD(SkillHudController + 데이터 주도 아이콘 SkillIconSet) + P02 발표 재빌드(7씬 Succeeded) + 전체 회귀 baseline 비감소(WSL2 561 / EditMode 123 / 봇 16) + M4.9 사후 회수 박제. M4.9⟂M4.13 독립 재구성의 경량 예외 마일스톤.
created: 2026-06-12
completed: 2026-06-12
domains: [client, qa]
---

# M4.12 — 발표 데모 빌드 준비 마일스톤 박제

**마감 일자**: 2026-06-12
**Phase 수**: 2/2 완료 (P01 스킬 쿨다운 HUD / P02 발표 재빌드+전체 회귀+마감 박제)
**등급**: 복잡 (의도된 경량 마일스톤 — 직전 마일스톤 마감 + 발표 빌드 준비)
**WORK-ID**: m4.12-skill-finish
**브랜치**: `feature/m4.12-skill-finish` (PR/머지 = 별도 영호 GO 게이트, irreversible)

## TL;DR

M4.12는 원래 "M4.9 스킬 잔여 마감"으로 5 Phase 잡혀 있었으나, 착수 직전 실측에서 전제가 붕괴(M4.9 스킬이 이미 거의 다 구현됨) → **경량 2 Phase(발표 빌드 준비)로 재구성**하고 행동 입력 게이트는 M4.13으로 이관(작업 응집). P01에서 스킬 쿨다운을 HUD Q/E 슬롯의 Radial fill로 시각화 + 클래스별 아이콘 자동 전환을 박고, P02에서 발표 클라를 재빌드 + 전체 회귀 baseline 비감소 확인 + 미박제였던 M4.9를 사후 회수했다.

## AC 검증 결과

**P01 — 스킬 쿨다운 UI HUD** (`01-skill-cooldown-hud-DONE.md`):
- `LocalPlayerMovement.GetCooldown(SkillId)→(remaining,total)` + `SkillHudController`(싱글톤, Update 폴링, `ComputeFill`) + `SkillIconSet` ScriptableObject(SkillId→Sprite, 데이터 주도 클래스별 아이콘).
- 씬: `UI.unity` 3층 슬롯(프레임→icon→cooldown_overlay) + 컨트롤러 부착·와이어링 (AI, 영호 명시 요청).
- reviewer ×2 🔴0 / Unity error CS 0 / 영호 2클라 Play 통과("나이스 좋다").
- §4·wire 무변경(쿨다운 상수 이미 98_Shared 공유) — 새 패킷 0, v12 유지.

**P02 — 발표 재빌드 + 전체 회귀 + 마감 박제** (`02-rebuild-and-finalize-DONE.md`):
| 항목 | 결과 |
|---|---|
| WSL2 서버 테스트 | 561 passed / 0 failed (=baseline 561 비감소) |
| Unity EditMode | 123 passed / 0 failed (119 +4 SkillHud) |
| 봇 16시나리오 | 16/16 green (연속 13 + fresh 재검 3) |
| Unity 콘솔 | error CS 0 |
| BuildPlayer | Succeeded errors=0 7씬 → `C:\Dev\Build` |
| DLL mtime 신선도 | Managed DLL 20:57 > 소스 20:04 (신선) |
| wire 무변경 | ProtocolVersion v12 + 워킹트리 clean |
| M4.9 회수 박제 | `_milestone-DONE.md` 1장 (7 Phase + Teleport VFX 완료 실측 정정) |

## 결정 흐름

- **5 Phase → 2 Phase 경량 재구성**: 착수 직전 실측("plan은 현재 코드 실측 먼저")에서 M4.12 옛 전제 4건 증발(핸들러 분리 `360c640` / 클래스 게이트 M4.9 P02 / 쿨다운 단일화 `GetLastSkillTick` / 스킬 거의 완성). 남은 진짜 일은 ① 쿨다운 HUD(발표 폴리시) ② 발표 재빌드 ③ 행동 입력 게이트(임펄스 재설계 토대). ③은 M4.13 P1로 이관(작업 논리 응집).
- **M4.12 ⟂ M4.13 독립**: 게이트가 M4.13으로 들어가며 옛 `depends_on: M4.12` 해소. 순서는 영호 컨트롤.
- **로드맵 원칙(영호)**: 마일스톤 = 잔여 청소가 아니라 *기능 응집*으로 슬라이스. M4.12 = 의도된 경량 예외(발표 빌드). M4.13부터 = 굵직한 기능 설계·보완.
- **per-class 아이콘 = 컨트롤러 구동(B)**: 영호 "시스템적으로 컨트롤 가능하게" → `SkillIconSet` 데이터 주도. Sprite는 UnityEngine 의존이라 98_Shared 불가 → 클라 전용 ScriptableObject가 정석(SkillCatalog와 평행).
- **M4.9 회수 시 Teleport VFX 실측 정정**: work-pin "미배치" → 실측 "배치 완료"(6/11) 발견. 박제 직전 실측이 stale 정보의 거짓 박제를 막음.

## 학습 일지 후보 키워드

- 마일스톤 슬라이스 = 잔여 청소 아닌 기능 응집 — 응집 안 맞는 조각은 응집되는 마일스톤으로 이관
- plan 착수 직전 실측이 옛 전제 붕괴를 잡음 (계획서 6/11 → 실측 6/12 시 4건 증발)
- 쿨다운 HUD = 서버 푸시(HP) 아닌 로컬 예측 타이머 폴링(매 프레임)
- 데이터 주도 클래스별 아이콘 = SkillIconSet ScriptableObject(Sprite는 98_Shared 불가, 클라 전용)
- 경량 마일스톤도 -DONE 박제 + 회귀 baseline 비감소 게이트는 동일 적용
- 박제 직전 실측 = recalled/pin 정보 stale 차단(Teleport VFX 미배치→배치 완료 정정)

---

> M4.12 완전 마감. 다음 = 발표 또는 M4.13(임펄스 동작 클래스 재설계, 6 Phase) — 순서 영호 결정.
