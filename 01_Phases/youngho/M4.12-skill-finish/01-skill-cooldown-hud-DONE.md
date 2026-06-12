---
owner: youngho
milestone: M4.12
phase: 01-skill-cooldown-hud
title: 스킬 쿨다운 UI HUD — 클래스별 Q/E 슬롯 fill + 데이터 주도 아이콘
status: done
grade: 복잡
slug: 01-skill-cooldown-hud
summary: 스킬 쿨다운을 HUD Q/E 슬롯의 Radial fill로 시각화 + 클래스별 아이콘 자동 전환(SkillIconSet). M4.9 Phase 07 회수.
created: 2026-06-12
completed: 2026-06-12
domains: [client]
risk_flags: [unity-asset]
---

# M4.12 Phase 01 — 스킬 쿨다운 UI HUD (DONE)

## TL;DR

스킬을 쓰면 HUD 슬롯의 **어두운 Radial 덮개가 쿨다운만큼 가득 찼다가 걷히고**, 슬롯 **아이콘은 클래스(Mage↔Knight) 따라 자동 전환**된다. M4.9 Phase 07(`07-client-skill-hud-and-regression`, -DONE 0개 미박제)의 회수. **§4·wire 무변경**(쿨다운 상수·SkillKeyMap 이미 클라/공유에 존재) — 새 패킷 0. 코드 2커밋(`442082f`·`9e8eb55`) + 씬 와이어링(AI, 영호 요청) + 영호 아이콘 에셋 + 영호 2클라 Play 통과("나이스 좋다").

- **코드(AI)**: `LocalPlayerMovement.GetCooldown(SkillId)→(remaining,total)` + `SkillHudController`(싱글톤, Update 폴링, `ComputeFill`) + `SkillIconSet` ScriptableObject(SkillId→Sprite).
- **씬(AI, 영호 명시 요청)**: `UI.unity` skill_slot_1·2를 3층(프레임 → `icon` → `cooldown_overlay`)으로 구성 + `SkillHudController`를 HUD에 부착·와이어링.
- **에셋(영호)**: `SkillIconSet.asset` + 3 매핑(임시 텍스처) + `_iconSet` 할당.

## AC 검증 결과

| 항목 | 결과 |
|---|---|
| reviewer (Tier 2-A) ×2 | 🔴 0 / 🟡 4(전부 비차단, 반영 또는 미래 메모) |
| Unity 컴파일 | `error CS` **0** (ForceSynchronousImport 강제 재컴파일 후 — stale 아님, 3회 검증) |
| EditMode `SkillHudControllerTests` | 4케이스 작성(3/3→1.0 / 0/3→0 / 5/2→1.0 clamp / 0/0→0). *실행 = P02 전체 회귀* |
| `SkillIconSet` 매핑 검증 | entries 3개 ✅ — Thunderbolt→mage_skill_2_0 / Dash→Knight_Dash_Skill_Effect_0 / Teleport→teleport2_sheet_0 |
| 씬 와이어링 검증 | `_qIconImage`·`_eIconImage` set / `_qCooldownImage`·`_eCooldownImage` set(FormerlySerializedAs 보존 확인) / `_iconSet` 할당 |
| wire 무변경 | PDL.xml / `ProtocolVersion` diff 0 — **v12 유지** |
| 영호 2클라 Play 육안 | ✅ 통과 — 아이콘 클래스별 전환 + 쿨다운 sweep ("나이스 좋다") |

**미완(정직 기록)**: ① `UI.unity` 씬은 **영호 WIP(skill_panel 5슬롯)와 섞여 미커밋** → 영호가 직접 커밋(백업 `/tmp/UI.unity.bak-442082f`). ② 아이콘은 **임시 텍스처** — 영호가 추후 정식 교체. ③ slot_3~5는 미사용(현재 스킬 2개). 이들은 P01 기능 완료에 영향 없음.

## 결정 흐름

- **fill 방향** = `remaining/total` (시전 시 덮개 가득 → 준비되며 걷히는 표준 쿨다운 sweep). `1−...`(준비도) flip은 `SkillHudController.cs:94` 한 줄 — 영호 외관 의도면 가능. → 표준 채택.
- **per-class 아이콘**: (A) static 직접 vs **(B) 컨트롤러 구동** → 영호 "시스템적으로 컨트롤 가능하게" → **B 채택**. 아이콘이 클래스 따라 자동 전환.
- **아이콘 매핑 위치**: `SkillCatalog`(skill→class, 98_Shared)와 평행하게 클라 `SkillIconSet`(skill→icon). 이유 = `Sprite`는 `UnityEngine` 의존이라 .NET Standard `98_Shared` *물리적 불가* + 아이콘은 순수 표시 데이터(서버 권위 무관, §1). → 클라 ScriptableObject가 정석.
- **3층 슬롯 구조**: 슬롯 본체 Image가 이미 *프레임*(`6_skill_slot_frame_0`)을 들고 있음을 **영호가 지적** → 본체에 아이콘 넣으면 프레임 소멸(Image 1개=sprite 1개). 프레임 → `icon`(신규 중간층) → `cooldown_overlay`(위)로 분리.
- **씬 와이어링 주체**: Unity 외관은 보통 영호 직접이나, **영호가 명시 요청**("skill_panel 슬롯 활용해서 만들어줄래") → "부를 때 보조" 정합으로 AI가 Unity MCP로 수행. 영호는 아이콘 에셋만.
- **필드 rename 안전**: `_qSlotImage`→`_qCooldownImage`(쿨다운 의미 명확화) 시 `[FormerlySerializedAs]`로 기존 씬 참조 보존(미사용 시 유실).

## 학습 일지 후보 키워드

- `FormerlySerializedAs`로 [SerializeField] rename 시 씬 와이어링 보존
- ScriptableObject 데이터 주도 매핑(SkillIconSet) — 디자이너가 Inspector 편집, 코드에서 분리
- §4 경계 판정: "클라가 읽기만 하나 / Sprite는 UnityEngine 의존" → 클라 전용(98_Shared 불가)
- Unity MCP RunCommand: `Shared.dll` 미참조 → 타입 직접 참조 대신 `SerializedProperty`(enumDisplayNames)로 introspection
- Unity MCP RunCommand: `Image`가 `Unity.AI.Image` 네임스페이스와 충돌 → `using UIImage = UnityEngine.UI.Image;` 별칭
- 쿨다운 HUD = 서버 푸시(HP) 아닌 **로컬 예측 타이머 폴링**(매 프레임)
- 서버 백그라운드 유지 = `tail -f /dev/null | dotnet GameServer.dll`(Console.ReadLine EOF 자가종료 방지)

---

> P01 완료. 다음 = **영호 `UI.unity` 커밋** → **Phase 02**(발표 재빌드 + 전체 회귀 + M4.9·M4.12 마감 박제).
