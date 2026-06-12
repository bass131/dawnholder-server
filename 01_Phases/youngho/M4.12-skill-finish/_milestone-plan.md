---
owner: youngho
milestone: M4.12
title: 발표 데모 빌드 준비 — 스킬 쿨다운 UI + 재빌드 + 마감 박제 (경량)
status: planned
grade: 복잡
slug: M4.12-skill-finish
created: 2026-06-11
revised: 2026-06-12
domains: [client, qa]
---

# M4.12 — 발표 데모 빌드 준비 (경량)

> **2026-06-12 재구성** — 옛 제목 "M4.9 스킬 잔여 마무리 + 핸들러 분리 + 발표 재빌드"(5 Phase)는 *착수 직전 실측*에서 전제가 무너져 폐기. M4.9 스킬은 이미 거의 다 돼 있었다(아래 "실측" 참조). 이질적으로 남은 조각 중 **행동 입력 게이트는 M4.13으로 이관**(임펄스 재설계와 응집)하고, M4.12는 **발표 나갈 빌드를 다듬어 박는 경량 마일스톤**으로 좁힌다. (디렉토리·브랜치 슬러그 `skill-finish`는 churn 회피로 유지 — 본 `title`이 실제 목적의 단일 진실.)

---

## Context (왜 가벼워졌나)

직전 = M4.11(동기화 정돈). 본래 M4.12는 "M4.9 스킬 잔여 마감"이 골자였는데, **착수 전 실측("plan은 현재 코드 실측 먼저")** 결과 잔여가 거의 증발했다:

| 옛 가정 (5 Phase) | 실측 (2026-06-12) | 판정 |
|---|---|---|
| 클라 패킷 핸들러 분리 (909줄 모놀리스) | **이미 완료** — `360c640`로 도메인 폴더(Combat/Skill/Sync/Zone…) + `IClientPacketHandler` 분리. 최대 클라 파일 393줄 | ❌ 증발 |
| 클래스↔스킬 게이트 | **이미 완료** — M4.9 P02(`SkillCatalog` 단일 진실 + 서버 3단계 silent drop + 클라 거울) | ❌ 증발 |
| 쿨다운 자료구조 단일화 | **이미 완료** — `GetLastSkillTick(byte skillId)` 스킬별 | ❌ 증발 |
| M4.9 dash/teleport "연출 실측 게이트" | Dash = 코드+VFX 완성(이번 세션 속도 튜닝까지). **Teleport = 코드 ✅ / VFX 프리팹 미배치**(영호 외관 영역) — *측정*할 게 아니라 *셀지 결정*할 항목 | ❌ 프레이밍 오류 |
| 행동 입력 게이트 시스템화 | 진짜 신규(미존재). 단 **임펄스 재설계의 서버 토대** = M4.13과 응집 | → **M4.13 P1 이관** |

남은 진짜 일은 **발표 폴리시(쿨다운 UI) + 릴리스(재빌드 + 마감 박제)** 둘뿐 → 경량 2 Phase.

> **회수 원본 확인 (plan-auditor 2026-06-12)**: M4.12 두 Phase는 사실상 **M4.9 Phase 07**([`../M4.9-skill-completion/07-client-skill-hud-and-regression.md`](../M4.9-skill-completion/07-client-skill-hud-and-regression.md) — *client skill HUD + regression*)을 회수한다. M4.9는 Phase 정의 01~07이 있으나 **-DONE.md 0개**(미박제) — 본 마일스톤이 그 잔여를 정식 마감한다. (즉 쿨다운 UI는 *신규 발명*이 아니라 M4.9가 계획만 하고 안 박은 분량의 회수.)

---

## 실측 지반 (file:line — 착수 시 재탐색 불필요)

- **쿨다운 UI 토대 (전부 존재, §4 무변경)**: 클라가 쿨다운 잔여를 float로 추적(`03_Client/.../Prediction/LocalPlayerMovement.cs:58-60` `_{thunderbolt,dash,teleport}CooldownRemaining`, 프레임 박자 감쇠 `:239-246`), 길이 = `Constants.{Dash,Teleport,Thunderbolt}CooldownTicks × TickDuration`(`:166-188`). 쿨다운 상수는 **이미 `98_Shared/GameData/Constants.cs:54-76`**(서버 `CombatConstants`가 re-export) → `fill = 1 − 잔여/총길이` 즉시 계산, **새 패킷·공유코드 변경 0**. 현재 외부 노출은 bool(`CanUseDash` 등)뿐 → getter 추가 필요.
- **HUD 인프라**: `03_Client/.../UI/HudController.cs`(싱글톤, `Assets/Scenes/99.UI/UI.unity` Additive 캔버스, HP/MP/Gold). 같은 캔버스에 쿨다운 슬롯 추가 + 같은 싱글톤 패턴.
- **스킬↔키 매핑**: `LocalPlayerInput.cs:42-47` — Mage (Q:Thunderbolt, E:Teleport) / Knight (Q:Dash). HUD 슬롯 구성에 재사용.
- **Teleport 실상태**: 서버 `ProcessTeleport`(`SkillSystem.cs:30-72`)+테스트(`MageTeleportTests`)+봇(`TeleportSmokeScenario`) ✅ / 클라 입력·핸들러·예측 ✅ / **VFX `Effects/TeleportDepart`·`TeleportArrive` 미존재**(`SkillCastHandler.cs:27` 주석 "에셋을 영호가 배치 예정"). → **보류·정직 기록**(영호 결정 2026-06-12).

---

## Phase 분해 (예정 — 개별 .md는 착수 시 /work:plan)

| # | Phase (예정) | 위험 | 도메인 | 핵심 |
|---|---|---|---|---|
| 1 | **스킬 쿨다운 UI HUD** | unity-asset | client (+영호 외관) | `LocalPlayerMovement` 쿨다운 잔여 getter 노출(현재 bool만) + `SkillHudController` 신설 — 클래스별 Q/E 슬롯 + `fill = 1 − 잔여/총길이`(`Constants` 상수 사용). HUD 슬롯 프리팹/스프라이트/레이아웃 = **영호 외관 직접**(코드 wiring = AI). §4·wire 무변경. |
| 2 | **발표 재빌드 + 전체 회귀 + 마감 박제** | **irreversible** | qa | `C:\Dev\Build` 클라 재빌드(M4.10/11/12 전부 포함 + DLL mtime 신선도 확인) + 전체 회귀(**M4.11 baseline 대비 비감소** — WSL2 ≥561 / EditMode ≥119 / 봇 16 시나리오 PASS / Unity 콘솔 0err) + dry-run(발표 리허설). **M4.9·M4.12 마감 박제**(`_milestone-DONE.md`) — Teleport 실상태(코드 ✅ / VFX 미배치) **정직하게 기록**. ⚠️*박제 입자(M4.9 정의 7개 → -DONE 7장 vs 마일스톤 -DONE 1장 흡수)는 착수 시 영호와 범위 확정*(plan-auditor 🟡). |

> 경량이라 Phase 2개로 충분(5~7 권장은 굵직한 마일스톤 기준). Phase 1 끝 = 2클라 Play에서 Q/E 쿨다운 슬롯이 차오르는 것 육안 확인. Phase 2 끝 = 발표 나갈 빌드 + 마감 박제 완료.

---

## 의존 / 관계

- **들어오는 의존 — M4.10·M4.11 후.** 동기화 정돈된 토대 위에서 발표 빌드를 박는다.
- **M4.13과는 독립 (옛 의존 해소).** 행동 입력 게이트가 M4.13 P1로 들어가면서 **M4.12 ⟂ M4.13**가 됐다(옛 "M4.13 ⟵ M4.12 게이트" 의존 소멸). M4.13(임펄스 동작 클래스 재설계, [`../M4.13-impulse-class/_milestone-plan.md`](../M4.13-impulse-class/_milestone-plan.md))은 자기 안에 게이트를 품어 자족적. **두 마일스톤 순서는 영호 컨트롤**(발표 타이밍상 M4.12 먼저 예상이나 강제 아님 — 일정은 영호).

## 위험 / 헌법 게이트

- **Phase 2 = irreversible**(재빌드 + 이후 PR/머지). 발표 게이트 = 마지막 — Phase 1이 green이어야 재빌드가 의미.
- **Phase 1 = unity-asset**(쿨다운 HUD 프리팹). **외관 = 영호 직접**(스프라이트/레이아웃), 코드 wiring만 AI.
- **§2 ProtocolVersion v12 유지** — 쿨다운 UI는 *기존 공유 상수를 읽어 그리는* 클라 작업, wire 무변경. 만약 서버가 쿨다운 정보를 보내야 할 필요가 생기면 STOP → 영호 의논(현 설계는 불요 — 상수 이미 공유).
- **§1 서버 권위** — 쿨다운 UI는 클라 *예측치*를 그릴 뿐, 권위는 서버. (시전 거부/쿨다운 판정은 서버 단독 그대로.)

---

> **본 문서는 경량 마일스톤 계획서.** Phase 개별 정의 `.md`는 **M4.12 착수 시점에 /work:plan으로 분해**(위 표는 예정 골격).
