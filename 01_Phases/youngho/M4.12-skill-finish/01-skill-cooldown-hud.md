---
owner: youngho
milestone: M4.12
phase: 01-skill-cooldown-hud
title: 스킬 쿨다운 UI HUD — 클래스별 Q/E 슬롯 fill
status: done
grade: 복잡
slug: 01-skill-cooldown-hud
created: 2026-06-12
completed: 2026-06-12
domains: [client]
risk_flags: [unity-asset]
depends_on: []
---

# M4.12 Phase 01 — 스킬 쿨다운 UI HUD

> 마일스톤 계획서 = `_milestone-plan.md` P1. **이건 신규 발명이 아니라 M4.9 Phase 07**([`../M4.9-skill-completion/07-client-skill-hud-and-regression.md`](../M4.9-skill-completion/07-client-skill-hud-and-regression.md) — *client skill HUD*)**의 회수**다(M4.9 -DONE 0개 = 미박제). 발표 데모를 더 완성돼 보이게 하는 폴리시.

---

## 🎯 목표

플레이어가 스킬을 쓰면 HUD의 **Q/E 슬롯이 쿨다운만큼 차올랐다 풀리는** 시각 피드백을 본다. 슬롯 구성은 **클래스별** — Mage (Q:Thunderbolt, E:Teleport) / Knight (Q:Dash). 쿨다운 중엔 슬롯이 "사용 불가"로 보이고, 다 차면 "사용 가능"으로 돌아온다.

> 핵심 제약: **§4·wire 무변경**. 쿨다운 잔여·총길이가 *이미 클라에 다 있고*(아래 실측), 쿨다운 상수도 *이미 `98_Shared` 공유*라 — 새 패킷·공유코드 변경이 0이다. 순수 클라 작업(코드) + 영호 외관(프리팹).

---

## ⏪ 사전 조건

- [x] M4.11 마감(고정스텝 클라) — 쿨다운 타이머가 이 위에서 frame dt 감쇠.
- [x] 쿨다운 잔여 타이머 이미 존재 (`LocalPlayerMovement`).
- [x] HUD 인프라 존재 (`HudController` 싱글톤 + `Assets/Scenes/99.UI/UI.unity` Additive 캔버스).
- [ ] Unity 에디터 컴파일 0 error 상태.

---

## 현재 형상 실측 (2026-06-12 확정 — file:line)

| 좌표 | 증거 | 내용 |
|---|---|---|
| 쿨다운 잔여 (private) | `LocalPlayerMovement.cs:58-60` | `_thunderboltCooldownRemaining` / `_dashCooldownRemaining` / `_teleportCooldownRemaining` (float, 초). **현재 외부 노출 X.** |
| 노출은 bool뿐 | `:63-65` | `CanUseSkill`(=Thunderbolt, 레거시 네이밍) / `CanUseDash` / `CanUseTeleport`. fill엔 부족 → **float getter 추가 필요.** |
| 총길이 출처 | `:166-188` | `NotifyChannel/Dash/Teleport`가 `_xxxCooldownRemaining = Constants.{Thunderbolt,Dash,Teleport}CooldownTicks × Constants.TickDuration`로 세팅. **총길이 = 이 식.** |
| 감쇠 박자 | `:239-246` | Update에서 **frame dt** 감쇠("UI·쿨다운 타이머는 표시용"). → HUD 폴링도 frame 박자라 정합. |
| 쿨다운 상수 (공유) | `98_Shared/GameData/Constants.cs:54-76` | Thunderbolt=40 / Dash=20 / Teleport=30 틱. **이미 양쪽 공유** → §4 무변경. |
| HUD 싱글톤 패턴 | `UI/HudController.cs:20,41-55` | `static Instance` + Awake 중복가드 + OnDestroy 클리어 + `[SerializeField]` Inspector 와이어. **SkillHudController가 미러.** |
| 클래스→Q/E 매핑 | `Input/LocalPlayerInput.cs:42-47` | Mage (Q:Thunderbolt, E:Teleport) / Knight (Q:Dash, E:None). **HUD 슬롯 구성에 재사용.** |
| 선택 클래스 조회 | `HudController.cs:61` | `Bootstrap.ClassLoadout.GetSelectedClassValue(...)` — SkillHudController도 같은 경로로 내 클래스 판별. |

---

## 📝 작업 내용

**[AI — 코드]**
- [ ] `LocalPlayerMovement`에 **쿨다운 잔여·총길이 읽기 API** 추가 (현재 bool만). 권장: `public (float remaining, float total) GetCooldown(SkillId skill)` 한 메서드 — skill→해당 `_xxxCooldownRemaining` + `Constants.XxxCooldownTicks × TickDuration` 매핑. (float getter 3종 + HUD가 Constants 직접 읽기도 가능하나, 매핑을 movement에 두면 SRP·매직넘버 회피에 유리.)
- [ ] `SkillHudController.cs` 신설 (`03_Client/Assets/Scripts/UI/`) — `HudController` 싱글톤 패턴 미러. `Update()`에서 내 클래스(`ClassLoadout`)의 Q/E 스킬을 `LocalPlayerInput.SkillKeyMap`(또는 `SkillCatalog`)으로 결정 → 각 슬롯에 `LocalPlayerMovement.Instance.GetCooldown(skill)` 폴링 → `slotImage.fillAmount = remaining/total`(쿨다운 중 채워짐) 또는 `1 − remaining/total`(준비도 — 영호 외관 의도에 맞춰 택1).
- [ ] `SkillId.None` 슬롯(Knight E)은 비활성/숨김 처리.

**[영호 — 외관 (unity-asset)]**
- [ ] `UI.unity` 캔버스에 Q/E 슬롯 프리팹/스프라이트/레이아웃 배치 + 스킬 아이콘.
- [ ] `SkillHudController`의 `[SerializeField]` 슬롯 `Image` 참조 Inspector 와이어링.
- [ ] **프리팹 변경 전 백업**(unity-asset 위험 — Phase 08 BackGround 사고 교훈).

---

## ✅ 완료 조건 (정량)

- [ ] **2클라 Play 육안**: Mage Q(Thunderbolt) 시전 → 슬롯이 ~2.0s 동안 fill 변화 후 복귀 / E(Teleport) ~1.5s / Knight Q(Dash) ~1.0s. 쿨다운 길이 = `Constants` 값 × 0.05.
- [ ] **쿨다운 중 슬롯이 "사용 불가" 시각 구분**(fill 또는 dim) — 다 차면 "사용 가능" 복귀.
- [ ] **클래스 전환 시 슬롯 구성 변화**: Mage = Q+E 2슬롯 / Knight = Q 1슬롯(E 숨김).
- [ ] **Unity 컴파일·콘솔 0 error/warning**(scriptCompilationFailed=False).
- [ ] **wire 무변경**: PDL.xml / `ProtocolVersion` diff 0 — **v12 유지**. `dotnet test` 영향 0(클라 전용 변경).

---

## 🧪 테스트

**자동**:
- (옵션) `SkillHudControllerTests` EditMode — fill 계산 순수 함수(`remaining/total → fill`) 경계값(0=막 시전, total=준비완료, remaining>total 방어).

**수동**:
- 2클라 Play: 각 스킬 시전 → 슬롯 fill 애니메이션 육안. 쿨다운 끝나기 전 재시전 차단되는지(`CanUseXxx` 게이트와 HUD 일치).
- 클래스 전환(Mage↔Knight) 후 슬롯 구성 변화.

---

## 📚 학습 포인트

- **서버 푸시(HP) vs 클라 폴링(쿨다운) HUD의 차이**: HP는 `S_PlayerHp` 도착 시 `UpdateHP()` 푸시 / 쿨다운은 *로컬 예측 타이머*라 매 프레임 폴링이 정석. 같은 HUD라도 데이터 성격에 따라 갱신 패턴이 다르다.
- **§1 정합**: HUD는 *예측치*를 그릴 뿐 — 시전 허용/거부의 권위는 서버. 클라 쿨다운이 어긋나도 서버가 silent drop으로 최종 판정.
- **fillAmount 0~1**: Unity `Image.type = Filled`의 fillAmount로 원형·바형 쿨다운 표현.

---

## ⚠️ 함정 / 주의사항

- **`CanUseSkill` = Thunderbolt 쿨다운**(레거시 네이밍, `:63`) — 슬롯 매핑 시 "CanUseSkill이 무슨 스킬?" 혼동 주의. Thunderbolt로 명확히 매핑.
- **매직넘버 금지**(CODE_CONVENTION §2.5): 쿨다운 총길이를 HUD에 하드코딩 X — `Constants.XxxCooldownTicks` 경유.
- **unity-asset 위험**: 프리팹/씬 변경은 영호 직접 + 백업. AI는 `[SerializeField]` 필드 *선언*만, 실제 와이어링은 Inspector(영호).
- **폴링 null 가드**: `LocalPlayerMovement.Instance` / 슬롯 `Image`가 씬 로드 타이밍에 null일 수 있음 — HudController처럼 null 체크.

---

## ➡️ 다음 Phase

- Phase 02 — 발표 재빌드 + 전체 회귀 + 마감 박제 (쿨다운 HUD 포함된 클라를 발표 빌드로).

---

## 📋 박제 (완료 후)

복잡 등급 → `01-skill-cooldown-hud-DONE.md` 박제. `phase-gate-validator.sh` Hook이 frontmatter + 의무 섹션 검사.
