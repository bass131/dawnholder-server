---
owner: youngho
milestone: M4.13
phase: 01-action-input-gate
title: 행동 입력 게이트 시스템화 — AcceptsAction 단일 입구 + 상태 데이터 소유
status: planned
grade: 대규모
slug: 01-action-input-gate
created: 2026-06-13
domains: [shared, server, client]
prior_phases: []
depends_on: []
risk_flags: [trust-boundary]
---

# M4.13 Phase 01 — 행동 입력 게이트 시스템화

> 계획서 = `_milestone-plan.md` "Phase 1 토대" 섹션. 이 게이트가 **임펄스 재설계의 서버 토대** — *상태가 자기 행동·임펄스 데이터를 소유*하는 구조가 P2~P6(임펄스 모델 통일)의 전제다.
> **trust-boundary Phase**: `CombatSystem`/`Handlers` 신뢰 경계 인접 → 자동 등급 상향(대규모), reviewer 재검증 의무, 입구 검사는 서버 권위(§1·§3).

---

## Context (왜)

특정 동작 State가 끝나고 Idle로 복귀해야 추가 입력을 받는 **예외 처리가 기술(스킬)마다 재발**한다 — 시스템적 설계가 없어서다. 입력을 막는 장치가 **세 곳에 흩어져 서로 다른 방식**으로 동작한다(아래 증거). 결과적으로 **Dash commit window 중 평타 진입이 허용**되고, 그 평타가 `Attack→Attack` self-transition no-op(Exit 미실행)을 일으켜 `LungeDecayPerTick 0.85`(Dash 전용값) 잔류 버그를 냈다. **봉합이 "호출자가 평타 진입마다 기본값을 직접 세팅"이라 — 호출자가 상태 내부 파라미터를 찔러 넣는 구조 자체가 원인**(상태가 자기 데이터를 소유하지 못함).

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, main `2433ab5`)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 이동 잠금만 상태 선언식 | `Maps/States/ActorState.cs:18`(LocksMovement)/`:22`(InterruptibleByHit) · `PlayerCombatStates.cs:28`(AttackState.LocksMovement=true) | 베이스 기본 false → 틱 루프가 `LocksMovement`로 inputX=0 강제. **이동만** 상태가 막는다. |
| 1b. movement-gate | `Maps/GameMap.cs:232-238` | `LocksMovement` 체크 → `inputX=0`/`rawJump=false` 강제. |
| 2. 공격 진입은 상태 미검사 | `Maps/Systems/CombatSystem.cs:37-49` | `ProcessAttack`이 rate-limit·rewind만 보고 **현재 행동 State는 안 본다** → Dash 중 평타 허용. |
| 2b. LungeDecayPerTick 호출자 직접 세팅 | `CombatSystem.cs:75`(= `Constants.KnockbackDecayPerTick` 명시) · `PlayerEntity.cs:125`(프로퍼티 소유)/`:235`(Revive 리셋) | 호출자가 상태 파라미터를 직접 세팅 — `0.85` 잔류 사고의 구조적 뿌리. |
| 3. 클래스 게이트는 별도 경로(M4.9 P02) | `98_Shared/GameData/SkillCatalog.cs`(GetRequiredClass) · `Handlers/Skill/SkillUseHandler.cs:17-52`(3단계 검증) | *클래스 자격·쿨다운*만 검사. **"현재 상태가 이 행동을 받는가"는 어디에도 없다** — 이번에 메울 빈칸. |
| 4. AcceptsAction 부재 | `ActorState.cs`(멤버: AnimState/LocksMovement/InterruptibleByHit/Enter/Tick/Exit) | 행동 허용 선언 메서드 **없음** 확인(빈칸 정확). |

---

## 설계 방향 (착수 시 재실측 후 확정 — 골격)

- **상태가 입장 정책 선언** — `LocksMovement`와 동형으로 `AcceptsAction(액션종류)`: "이 상태에서 이 행동 허용?"의 답을 **상태가 소유**(`ActorState`에 추가, 베이스 기본 + 상태별 override).
- **서버 행동 요청 단일 입구**(`TryPerformAction` 류) — ①현재 상태 허용 ②쿨다운 ③클래스 게이트를 **한 곳에서** 검사. 새 기술 = 허용 표 한 줄.
- **기존 클래스 게이트(M4.9 P02)는 단일 입구에 흡수** — 중복 검증 제거.
- **상태 소유 데이터** — `LungeDecayPerTick`류를 상태가 Enter/Exit에서 소유·정리(호출자 직접 세팅 제거 = `0.85` 사고의 구조적 봉합). **이것이 P2 임펄스 모델 통일의 전제.**
- **클라는 같은 게이트 표를 거울** — `98_Shared` 공유(`SkillCatalog` 패턴을 행동 허용까지 확장). 서버 권위, 클라 거울은 UX(헛입력 즉시 차단).

---

## 확정 설계 (2026-06-13 — 영호 결정: B full 통합 + 풀 전략 패턴)

**범위 = B (full)**: 단일 입구가 ①상태 허용 ②쿨다운 ③클래스 ④rewind 전부 검사. 공격 ms 쿨다운도 tick 통일. **확장성 위해 풀 전략 패턴**(행동 = 다형성 객체) 채택 — 새 행동 = 구현 클래스 1개 + 레지스트리 한 줄(if-else 분기 0, OCP 만족). 영호: "미래 행동 많이 늘 예정 → 확장성 우선."

### 구조

1. **`ActionKind` enum** (98_Shared) — `Melee`/`Dash`/`Teleport`/`Thunderbolt`. 평타+스킬 통합 개념. 클라 거울 위해 shared. (직렬화 안 됨 — wire 무관)
2. **`IGameAction`** (02_Server) — 행동 1개 = 다형성 객체. 멤버: `ActionKind Kind` · `int CooldownTicks` · `CharacterClass? RequiredClass`(null=평타) · `bool Execute(GameMap, PlayerEntity, long clientTick)`.
3. **구현 클래스** (02_Server) — `MeleeAction`/`DashAction`/`TeleportAction`/`ThunderboltAction`. 각 `Execute`에 현재 `ProcessAttack`/`ProcessDash`/`ProcessTeleport`/`ProcessThunderbolt` **본체 1:1 이관**(거동 보존). Flyweight(상태 없는 정적 인스턴스 — 틱 루프 new 0, `PlayerCombatStates` 패턴 정합).
4. **`ActionRegistry`** (02_Server) — `IReadOnlyDictionary<ActionKind, IGameAction>` 단일 진실.
5. **`ActionGate`** (02_Server, 새 클래스/System) — 단일 입구, **분기 0**:
   - `action = Registry[kind]`
   - ① `player.ActionFsm.CurrentState.AcceptsAction(kind)` — 상태 허용
   - ② `currentTick - player.LastActionTick(kind) < action.CooldownTicks` — 쿨다운(tick)
   - ③ `action.RequiredClass is {} rc && player.Stats.Class != rc` — 클래스
   - ④ `CombatSystem.ValidateRewind(clientTick, currentTick)` — rewind
   - 통과 → `SetLastActionTick(kind)` + `action.Execute(...)`
6. **`ActorState.AcceptsAction(ActionKind)`** (02_Server) — 상태 정책. 베이스 `=> true`(이동 상태 전부 허용). `AttackState`/`HitState`/`DeathState` override `=> false`(commit window/hitstun/사망 중 행동 거부). 미래 세분(예: "공격 중 이동스킬만")은 비트마스크 확장 여지.
7. **쿨다운 tick 통일** (`PlayerEntity`) — `_lastSkillTick[]`(스킬) + `LastAttackTickMs`(공격 ms) → 통합 `_lastActionTick[ActionKind]`(tick). 공격 `AttackCooldownMs(500)` → `MeleeCooldownTicks(10)` 환산 상수. `GetLastSkillTick/SetLastSkillTick` → `LastActionTick(kind)/SetLastActionTick(kind)`. ⚠️ ms→tick = rate-limit 환산(평소 20TPS 동등, 테스트 갱신).
8. **`LungeDecayPerTick`/`AttackLungeVx` 상태 소유** — `EnterAttackState(float lungeVx, float decayPerTick)` 파라미터화. `Action.Execute`가 *값을 계산해 넘기되* `AttackState.Enter`가 *세팅 책임*. 호출자 직접 필드 세팅(`CombatSystem.cs:75,81`/`SkillSystem.cs:95,96`) **제거** = `0.85` 잔류 사고 구조적 봉합.

### trust-boundary 결정 (reviewer 점검 대상)

- **핸들러(network thread) 1차 검증 유지** — `SkillUseHandler`/`AttackHandler`의 `HasSelectedClass`·skillId 범위·cheat-flag 로그는 *빠른 거부 + 로깅* layer로 유지(헌법 §3). **decode + submit**.
- **`ActionGate`(tick thread) = 권위 검증** — 상태+쿨다운+클래스+rewind 최종 권위. `SkillSystem` 각 `Process*`에 흩어진 쿨다운/클래스 검증이 여기로 **흡수**(tick thread 내 중복 제거 = 위 단일 입구 흡수). 클래스 검증이 핸들러+게이트 양쪽이면 defense-in-depth(network 1차 / tick 권위 = 다른 layer라 정당) — reviewer가 과잉 판정 시 핸들러 축소 검토.

### wire / 거동

- **wire v12 무변경** — `ActionKind`/`IGameAction`/`Registry`/`ActionGate` 전부 서버 내부 + shared enum(직렬화 안 됨). `C_SkillUse`는 여전히 skillId byte. PDL/`ProtocolVersion` 무손상.
- **거동 보존** — `Execute` 본체는 현재 `Process*` 1:1 이관. 단 쿨다운 ms→tick은 *동등 환산*(평소 거동 동일).

### 단계 분할 (server 먼저, client 후)

- **P1-server** (이번): §1~8 + trust-boundary. server+shared 구현 → reviewer → 회귀.
- **P1-client** (server 권위 확정 후): 클라 입력 측이 `ActionKind` 허용 표 참조해 헛입력 차단(UX). shared 공유.

---

## 변경 대상 (파일별 — 착수 시 확정)

1. **`Maps/States/ActorState.cs`** — `AcceptsAction(ActionKind)` 정책 추가(베이스 기본). 상태별 override는 `PlayerCombatStates.cs`.
2. **서버 단일 입구** — `TryPerformAction`(위치: Systems 신설 또는 GameMap 경로 — 착수 시 SRP로 결정). `ProcessAttack`/`SkillSystem` 진입을 이 입구 뒤로.
3. **`Maps/Systems/CombatSystem.cs`** — 평타 진입을 단일 입구 경유로. `:75` LungeDecayPerTick 직접 세팅 제거(상태 Enter/Exit 소유로 이관).
4. **`98_Shared`** — 행동 허용 표(`SkillCatalog` 확장 또는 신설). **wire 무변경 점검(§2, v12 유지)** — 상수/표만 추가면 직렬화 형상 불변.
5. **클라 거울** — 클라 입력 측(`LocalPlayerInput`/`SkillCastHandler`)이 같은 표 참조해 헛입력 차단.

---

## 완료 조건 / 게이트 (정량)

- [ ] `ActorState.AcceptsAction` 존재 + 상태별(Attack/Hit/Dash/Idle) 허용 표 명시.
- [ ] 서버 행동 진입(평타·스킬)이 **단일 입구**를 거침 — grep으로 입구 1곳·우회 0건 제시.
- [ ] **Dash commit window 중 평타 거부** EditMode 테스트 green(현재 허용되던 구멍 봉합 증명).
- [ ] `LungeDecayPerTick` **호출자 직접 세팅 0건**(grep) — 상태 Enter/Exit 소유로 이관.
- [ ] 클래스 게이트(M4.9 P02) 중복 검증 제거 — 단일 입구에 흡수.
- [ ] 회귀 green: WSL2 build+test 비감소(baseline 561/0) + EditMode 기존 가드 통과 + 봇 회귀.
- [ ] reviewer 재검증(trust-boundary): §1 입구 서버 권위 / §3 클래스·쿨다운·소유권 검증 유지 / 신뢰 경계 우회 0.

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 행동 허용 판정 = 서버 단독. 클라 거울은 UX(헛입력 차단), 서버 진실 우위 불변.
- **§3 신뢰 경계 (trust-boundary)**: 단일 입구가 클래스/쿨다운/소유권/**상태 허용**을 서버에서 검증. 클라 입력은 untrusted — 입구가 모두 막는 게이트. `Handlers/`·`*Validation*` 인접이라 reviewer 재검증 의무.
- **§2 Protocol**: 행동 허용 표가 wire(패킷 형상) 건드리면 STOP → 영호 의논. 현재 v12. 상수/표 추가는 무변경 예상.
- **클린코드(v6.1)**: SRP — 상태(정책 선언)/입구(검사)/실행(mutation) 책임 분리, System 간 직접 호출 X. 상태 소유 데이터(호출자 직접 세팅 금지). 매직넘버 금지, public 1줄 책임 헤더.

---

> Phase 완료 시 `01-...-DONE.md` 박제(대규모 등급 — 5단계 보고 동반). 게이트 통과 후 Phase 02(서버 임펄스 모델 통일) 착수.
