---
owner: youngho
milestone: M4.9
title: 스킬 시스템 완성 — 비주얼 연결 + 클래스 게이트 + Knight Dash + Mage Teleport + 쿨다운 UI
status: done
grade: 대규모
slug: M4.9-skill-completion
created: 2026-06-10
deadline: 2026-06-17 발표 전
protocol_bump: 불필요 예상 (enum append-only, 패킷 모양 무변경 — 아래 §프로토콜 참조)
branch: feature/skill-visual
domains: [shared, server, client, qa]
---

# M4.9 — 스킬 시스템 완성 (비주얼 + 클래스 게이트 + Dash/Teleport + 쿨다운 UI)

> 직전 main = M4.8 ProtocolVersion 11 마감(썬더볼트 AoE + 평타 원거리 + 지연 데미지 큐).
> 6/17 발표 영상 퀄리티가 이 마일스톤의 1순위 기준이다. "스킬이 그림으로 보이고, 클래스마다 다른 스킬을 쓰는" 데모.

---

## Context (왜)

M4.8은 **스킬 시스템의 뼈대**만 세웠다. C_SkillUse(24)/S_SkillCast(25)/S_HitResult.hitEffect(byte) 패킷이 박혔고, 서버 권위 박스 스캔 + 지연 데미지 + freeze가 돈다. 하지만 두 가지가 비어 있다:

1. **비주얼이 placeholder다.** 클라는 `Resources/Effects/SkillCast`·`LightningStrike` 경로로 prefab을 로드하려 하지만 **prefab이 존재하지 않아 warn만 찍고 연출 없이 데미지만 들어간다**(`SkillCastHandler` ~742줄, `HitResultHandler` ~274줄). 영호가 이미 에셋을 제작해 뒀다 — 이걸 prefab으로 포장해 연결하는 게 이번 마일스톤의 출발점이다.
2. **스킬이 Thunderbolt(1) 하나뿐이고, 클래스 게이트가 없다.** 현재 `SkillId` enum은 `None=0 / Thunderbolt=1`만 있고, `C_SkillUseHandler`는 `HasSelectedClass`(클래스 선택 여부)만 검증할 뿐 **"이 클래스가 이 스킬을 쓸 자격이 있는가"는 아무 데서도 확인하지 않는다.** 즉 전사(Knight)가 Mage 전용 썬더볼트를 그대로 시전할 수 있다 — 헌법 §3(신뢰 경계) 구멍이다. 원래 발표 후로 연기했으나, 이번에 Dash(Knight 전용)·Teleport(Mage 전용) 두 스킬을 추가하면서 **클래스 게이트가 전제 조건**이 되어 M4.9로 회수했다.

M4.9가 끝나면: **두 클래스가 각자의 스킬(Knight=Dash, Mage=Thunderbolt+Teleport)을 화면에 보이는 이펙트와 함께 쓰고, 서로의 스킬은 시전 자체가 막힌다.**

### 영호가 제작한 비주얼 에셋 (이미 존재 — prefab 포장만 남음)

- `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/`
  - `ThunderVolt` — 썬더볼트 낙뢰 (anim + controller + png)
  - `EnergyVolt` + `Hit_Effect` — Mage 평타 투사체 + 임팩트 (hitEffect=1)
  - `Teleport` — 텔레포트 연출
- `03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/`
  - `Dash Skill` + `Hit Effect` — Knight 대쉬 + 임팩트 (hitEffect=3)
- **공통 함정**: 모두 anim/controller/png까지만 있고 **prefab 포장이 없다.** prefab으로 묶고 `EffectLifetime`/`ProjectileVisual` 컴포넌트를 부착해야 코드가 로드할 수 있다.

---

## 설계 결정 (확정)

### 1. SkillId enum append — 패킷 모양 무변경 (wire-safe)
`SkillId.Dash=2`, `SkillId.Teleport=3`을 enum 끝에 **append**. `SkillId`는 `byte` 직렬화이고 C_SkillUse/S_SkillCast의 **필드 모양은 그대로**(byte 1칸)다 — 새 enum 값은 기존 byte 범위 안에서 의미만 늘어나므로 **ProtocolVersion bump 불필요**(예상). 단 enum이 바뀌었으니 `Shared.dll`은 재빌드해 `03_Client/Assets/Plugins/`로 갱신해야 한다. (헌법 §2: 값 영원히 고정, 은퇴 ID 재사용 금지 — append-only 약속 준수.)

### 2. 클래스↔스킬 매핑 = 98_Shared 단일 진실
`SkillId → 요구 CharacterClass` 매핑을 `98_Shared/GameData`에 단일 진실로 둔다(예: `SkillCatalog`). 클라(입력 게이트)·서버(시전 거부)가 같은 매핑을 거울로 사용 — 평타 쿨다운이 양쪽 단일 진실인 패턴과 동형. 서버가 최종 권위(헌법 §1): 클래스 불일치 시전은 **silent drop + cheat-flag 로깅**(헌법 §3).

### 3. 이동 스킬도 서버 권위 (헌법 §1)
Dash·Teleport 둘 다 **위치 이동**이지만 클라가 직접 좌표를 바꾸지 않는다.
- **Knight Dash** = M4.7 Knight lunge가 쓰던 `ExternalVelX` 채널(`PlayerEntity.AttackLungeVx` → `Physics.Step`)을 재활용한 **서버 권위 전방 대쉬**. 클라는 결과 위치를 **force-adopt**로 렌더만 한다(lunge 값 자체는 모름).
- **Mage Teleport** = 서버 권위 **위치 점프**. facing 방향 고정 거리, 맵 경계 clamp(범위 검증 필수 — 헌법 §3). 핵심 함정 = **클라 보간이 순간이동을 슬라이드로 뭉개는 것.** 로컬은 force-adopt 즉시 스냅, 원격은 보간 버퍼를 reset 후 스냅해야 한다.

### 4. hitEffect byte 의미 추가 (모양 무변경)
Knight Dash 경로 타격은 `S_HitResult.hitEffect=3`(신규 값)으로 디코드한다. hitEffect는 이미 byte 필드라 **값 의미만 추가**(0=근접 / 1=투사체 도착 / 2=낙뢰 / 3=대쉬 임팩트) — 패킷 모양 무변경, ProtocolVersion 불변.

---

## Phase 분해 (7개)

| # | Phase | 등급 | 도메인 | 담당 | 의존 | 완료 조건(정량) |
|---|---|---|---|---|---|---|
| 01 | 스킬 비주얼 prefab 연결 (ThunderVolt + EnergyVolt) | 보통(→복잡 상향 인지) | client | 영호 직접 + 메인 MCP 보조 | — | Q 썬더볼트 시 캐스팅+낙뢰 화면 표시 · "캐스팅 VFX 미존재" warn 0 · 콘솔 error CS 0 · 2클라 실측(원격도 동일) |
| 02 | 클래스↔스킬 게이트 + 클래스별 스킬 키 라우팅 | 복잡 | cross(shared+server+client) | Worker(Sonnet)×3 + reviewer | — | Knight가 Thunderbolt 시전 불가(서버 드랍 로그 + 클라 입력 차단) · 기존 회귀 0 · 신규 테스트 green |
| 03 | Knight Dash 서버 | 복잡 | shared+server+qa | server Worker + qa | 02 | dotnet test green · DashSmoke 봇 PASS · ProtocolVersion 불변 확인 |
| 04 | Knight Dash 클라 연출 | 보통 | client | 영호(모션/이펙트) + client Worker | 03 | 2클라 실측 — 시전자/원격 대쉬 이동+이펙트 일치 · rubber-band 0 |
| 05 | Mage Teleport 서버 | 복잡 | shared+server+qa | server Worker + qa | 02 | dotnet test green(경계 clamp 포함) · TeleportSmoke PASS(위치=기대값, 경계 밖 0) |
| 06 | Mage Teleport 클라 (보간 끊기 + 연출) | 복잡 | client | client Worker + 영호(이펙트) | 05 | 2클라 실측 — 양쪽 순간이동(슬라이드 0) · 이펙트 양 지점 재생 |
| 07 | 스킬 슬롯 쿨다운 UI + 전체 회귀/마감 | 복잡 | client+qa | client Worker + qa | 01~06 | 전체 dotnet test + 봇 전 시나리오 + 콘솔 0에러 + 2클라 4스킬 데모 + 발표용 재빌드 + DONE 박제 |

### 의존성 / 병렬

```
01(비주얼) ∥ 02(게이트)            ← 출발 (병렬 가능)
                02 → 03(Dash서버) → 04(Dash클라)  ─┐
                02 → 05(TP서버)   → 06(TP클라)    ─┤
                                          01~06 ─┴→ 07(쿨다운UI+마감)
```

- **01 ∥ 02**: 비주얼 연결(에디터 작업)과 게이트(코드)는 서로 무관 → 동시 출발.
- **(03→04) ∥ (05→06)**: 게이트(02) 완료 후 Dash 라인과 Teleport 라인은 독립 → 병렬.
- **07은 전체 후**: 쿨다운 UI는 모든 스킬이 박힌 뒤라야 슬롯 매핑이 확정된다.

---

## 프로토콜 (변경 최소)

- **SkillId enum append만**: `Dash=2`, `Teleport=3`. 패킷 필드 모양 무변경 → **ProtocolVersion bump 불필요(예상)**. 단 `Shared.dll` 재빌드 → `03_Client/Assets/Plugins/` 갱신 의무.
- **hitEffect 값 추가**: `3=대쉬 임팩트`. byte 의미만 추가, 모양 무변경.
- **C_SkillUse(24)/S_SkillCast(25)/S_HitResult 필드 불변.** 새 패킷 추가 없음.
- ⚠️ 각 서버 Phase 완료 시 **재생성 후 PacketID enum 시프트 0 + ProtocolVersion==11 불변 assert**로 확인. (만약 구현 중 새 필드가 정말 필요해지면 그 시점에 사용자 의논 후 bump.)

---

## 검증

1. **dotnet test**(`GameServer.Tests/`): 클래스 게이트(Knight→Thunderbolt 거부 · Mage→Dash 거부) · Dash(쿨다운·경로 타격·ExternalVelX) · Teleport(거리·경계 clamp) · 기존 회귀 0.
2. **헤드리스 봇**(Scenarios/): DashSmoke(Knight 대쉬 → 경로 적 타격 + 위치 전진) / TeleportSmoke(Mage 텔레포트 → 위치=기대값, 경계 밖 0) + 기존 봇(RangedHitSmoke/ThunderboltAoeSmoke 등) 회귀 0.
3. **2클라 Play(수동)**: 클래스별 스킬 키 동작 · Knight가 썬더볼트 시전 불가 · Dash/Teleport 시전자·원격 화면 일치(rubber-band/슬라이드 0) · 4스킬 이펙트 전부 보임 · 쿨다운 UI fill 갱신.
4. **발표용 재빌드**: `C:\Dev\Build` 클라를 PR #96 + M4.9 전부 포함해 재빌드(백로그 회수 — Phase 07).

---

## 리스크 / 범위 밖

- **unity-asset 위험 깃발**(Phase 01/04/06): prefab/anim 변경 = 에디터 작업. 영호 직접이 원칙, AI는 MCP 보조/검증(콘솔 0에러 게이트). prefab 변경은 등급 자동 상향.
- **trust-boundary**(Phase 02/03/05): 클래스 게이트·쿨다운·대쉬 경로·텔레포트 거리·경계 clamp 전부 서버 단독. 클라는 skillId 힌트뿐. 클래스 불일치 = silent drop + cheat-flag.
- **보간 끊기 함정**(Phase 06): Teleport가 가장 까다롭다. 원격 보간 버퍼를 reset 안 하면 순간이동이 부드러운 슬라이드로 보여 **연출 실패**. 로컬 force-adopt 스냅 + 원격 버퍼 reset 둘 다 필요.
- **enum spike = Phase 02 첫 액션**(비가역 가정 초입 확정): "ProtocolVersion bump 불필요" 가정을 Dash=2/Teleport=3 빈 append + PacketRoundTrip + Current==11 assert로 ~5분 안에 확정. 깨지면 즉시 STOP → plan 재조정(irreversible 깃발 경로) — 늦게 발견하면 03·05 작업이 다 무효화됨.
- **쿨다운 구조 전환 = Phase 02 단일 소유**(03·05 병렬 충돌 방지): `LastSkillTick` 단일 필드 → 스킬별 쿨다운(skillId 키 맵/스킬별 필드)으로의 공유 자원 변경을 Phase 02에서 한 번만. 03(Dash)·05(Teleport)는 병렬이라 각자 건드리면 충돌 — 이 구조에 올라타기만 한다.
- **범위 밖 명시**: 마나/MP(쿨다운만) / 스킬 리바인딩 UI(임시 키 매핑) / 다단 히트 / 추가 AoE 스킬 / 4번째+ 스킬 / 장애물 차폐.

---

## 승인 후 절차
1. Phase def 7개 작성 → **plan-auditor 자동 호출**(Tier 2-B).
2. 통과 후 01(비주얼=영호)·02(게이트=Worker)부터 시작 → reviewer → 메인 직접 실측(Worker 보고 불신) → commit.
3. PR — 클래스 게이트(서버 신뢰 경계) + 신규 스킬 묶음. 98_Shared 변경 → Shared.dll → 03_Client CODEOWNERS(정유현) co-review → admin 머지 예상. **각 PR 사용자 명시 GO.**
4. 마감 시 `_milestone-DONE.md`+`.html` 5단계 보고(대규모) + CHANGELOG[M].
