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

- **P1-server** ✅완료 (`11b5baf`): §1~8 + trust-boundary. server+shared 구현 → reviewer 🔴0 → WSL2 561/0.
- **P1-client** (이번 — 아래 "P1-client 확정 범위" 참조): 행동 입력 통합 게이트 = 서버 `AcceptsAction`의 클라 거울.

---

## 변경 대상 (파일별 — 착수 시 확정)

1. **`Maps/States/ActorState.cs`** — `AcceptsAction(ActionKind)` 정책 추가(베이스 기본). 상태별 override는 `PlayerCombatStates.cs`.
2. **서버 단일 입구** — `TryPerformAction`(위치: Systems 신설 또는 GameMap 경로 — 착수 시 SRP로 결정). `ProcessAttack`/`SkillSystem` 진입을 이 입구 뒤로.
3. **`Maps/Systems/CombatSystem.cs`** — 평타 진입을 단일 입구 경유로. `:75` LungeDecayPerTick 직접 세팅 제거(상태 Enter/Exit 소유로 이관).
4. **`98_Shared`** — 행동 허용 표(`SkillCatalog` 확장 또는 신설). **wire 무변경 점검(§2, v12 유지)** — 상수/표만 추가면 직렬화 형상 불변.
5. **클라 거울** (P1-client) — `LocalPlayerInput` 송신 게이트(`OnAttack`/`TrySendSkill`)에 행동 잠금 판정 추가. 상세 = 아래 "P1-client 확정 범위".

---

## P1-client 확정 범위 (2026-06-13 — 실측 후 영호 확정)

> P1-server 완료 후 클라 거울 범위를 정하려 클라 입력/예측 코드 실측(`LocalPlayerInput.cs`·`LocalPlayerMovement.cs`). **compact 전 "(2) ghost = serverTick 추정 필요 → P5 예측과 닿을 수 있음" 진단은 실측으로 정정됨** — 이동 게이트가 이미 로컬 타이머로 ghost 없이 작동 중이라 그 영역을 안 건드린다.

### 실측 — 이미 작동 중인 거울

- **클래스 거울** ✅ — `SkillCatalog.CanCast` @ `LocalPlayerInput.cs:136` (내 클래스 못 쓰는 스킬 송신 차단).
- **쿨다운 거울** ✅ — 평타 `CanAttack`(`LocalPlayerMovement.cs:54`) + 스킬별 `CanUseDash/Skill/Teleport`(`:63-65`), 로컬 타이머 감쇠(`Constants` 거울).
- **이동 잠금 거울** ✅ — `_commitWindowRemaining`(`:42`, `NotifyAttack`이 즉시 세팅 `:175`) + `IsMovementLocked(localLock, serverAnimState)`(`:218-224`) **로컬 타이머 OR 서버 AnimState 하이브리드**. source-gating(`:281-286`)이 잠금 시 moveX/jumpEdge를 근원 0. **로컬 타이머가 즉시 잠그므로 ghost 없음** — 서버 AnimState는 "더 길면 연장"하는 보정용.

### 진짜 갭 — 행동 입력 통합 게이트 부재

- `OnAttack`(`LocalPlayerInput.cs:90`) = **평타 쿨다운(`CanAttack`)만** 검사.
- `TrySendSkill`(`:139-150`) = **스킬별 쿨다운만** 검사 — 각 독립.
- → **"지금 행동 중이니 _모든_ 행동입력 차단"** 통합 게이트가 없다. 평타 commit window 중 스킬이 나가고(`NotifyAttack`/`NotifyChannel`이 다른 쿨다운 미소비), 채널링 중 평타가 나간다. 서버 `ActionGate.AcceptsAction(kind)`의 클라 거울 부재.

### 해법 (확정) — 기존 이동 잠금 조건 재사용

- `LocalPlayerMovement`가 "행동 잠금 중?" 단일 판정을 프로퍼티로 노출(= `IsMovementLocked`와 **동일 조건**: 로컬 타이머 OR 서버 AnimState Attack/Hit/Death).
- `OnAttack` + `TrySendSkill` 송신 게이트에 그 판정 추가 → 잠금 중이면 행동입력도 차단.
- **ghost 없음**(평타/스킬 친 순간 `NotifyAttack`/`NotifyChannel`이 로컬 타이머 즉시 세팅), **wire 무변경**, **P5 위치 예측과 무관**(위치 reconciliation이 아니라 입력 게이팅 — 인프라는 M4.11에서 검증됨).
- 서버 `AcceptsAction`이 현재 Attack/Hit/Death에서 _모든_ kind에 false → 클라 "잠금 중이면 모든 행동 차단"이 1:1 거울. `ActionKind`별 세분 게이트는 불필요(미래 비트마스크 확장 여지).

### P2로 이월 — Dash 중 ghost

- `NotifyDash`(`:193-196`)는 로컬 commit window 타이머를 **안 세팅**(주석: Dash는 서버 force-adopt 경로로 흡수). → Dash 중 행동입력 차단은 서버 AnimState=Attack 도착까지 lag만큼 ghost.
- Dash 지속(고정거리 등속)이 **P2 임펄스 모델에서 재정의**되므로, Dash 로컬 잠금 타이머는 P2와 한 묶음(P1에서 안 건드림 — 영호 확정).

---

## 완료 조건 / 게이트 (정량)

### P1-server ✅완료 (`11b5baf`)

- [x] `ActorState.AcceptsAction` 존재 + 상태별(Attack/Hit/Death override `=> false`) 허용 표.
- [x] 서버 행동 진입(평타·스킬)이 **단일 입구**(`ActionGate`)를 거침 — 우회 0건.
- [x] **Dash commit window 중 평타 거부** — `KnightDashTests` green(구멍 봉합 증명).
- [x] `LungeDecayPerTick` **호출자 직접 세팅 0건** — `AttackState.Enter` 소유로 이관.
- [x] 클래스 게이트(M4.9 P02) 중복 검증 제거 — `ActionGate`에 흡수.
- [x] 회귀 green: WSL2 561/0 비감소. reviewer 재검증 🔴0(trust-boundary §1·§3 유지).

### P1-client (이번)

- [ ] `LocalPlayerMovement`가 "행동 잠금 중?" 단일 판정을 프로퍼티로 노출(= `IsMovementLocked` 동일 조건: 로컬 타이머 OR 서버 AnimState Attack/Hit/Death).
- [ ] `OnAttack`/`TrySendSkill` 송신 게이트에 행동 잠금 판정 추가 — 잠금 중 행동입력(평타+스킬) 차단(grep으로 **두 게이트 모두** 적용 확인).
- [ ] EditMode 테스트 — 행동 잠금 중 행동입력 차단(`ResolveGatedInput`처럼 순수 함수 추출, MovementGate 테스트와 동형).
- [ ] 2-client Play 검증 — 평타 commit window 중 스킬/평타 헛입력이 서버로 **안 나감**(헛입력 즉시 차단 UX).
- [ ] 클라 컴파일 green(헌법 §4 양쪽 컴파일) + `Shared.dll`/`ClientNet.dll` sync(co-review 판단은 PR 단계 — [[shared-dll-triggers-client-co-review]]).

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 행동 허용 판정 = 서버 단독. 클라 거울은 UX(헛입력 차단), 서버 진실 우위 불변.
- **§3 신뢰 경계 (trust-boundary)**: 단일 입구가 클래스/쿨다운/소유권/**상태 허용**을 서버에서 검증. 클라 입력은 untrusted — 입구가 모두 막는 게이트. `Handlers/`·`*Validation*` 인접이라 reviewer 재검증 의무.
- **§2 Protocol**: 행동 허용 표가 wire(패킷 형상) 건드리면 STOP → 영호 의논. 현재 v12. 상수/표 추가는 무변경 예상.
- **클린코드(v6.1)**: SRP — 상태(정책 선언)/입구(검사)/실행(mutation) 책임 분리, System 간 직접 호출 X. 상태 소유 데이터(호출자 직접 세팅 금지). 매직넘버 금지, public 1줄 책임 헤더.

---

> Phase 완료 시 `01-...-DONE.md` 박제(대규모 등급 — 5단계 보고 동반). 게이트 통과 후 Phase 02(서버 임펄스 모델 통일) 착수.
