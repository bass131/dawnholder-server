---
owner: youngho
milestone: M4.13
phase: 02-server-impulse-model
title: 서버 임펄스 모델 통일 — dash/knockback/lunge ExternalVelX 일관화 + 대쉬 고정거리 등속
status: in-progress
grade: 복잡
slug: 02-server-impulse-model
created: 2026-06-13
domains: [server, shared]
prior_phases: [01-action-input-gate]
depends_on: [01-action-input-gate]
risk_flags: [trust-boundary]
---

# M4.13 Phase 02 — 서버 임펄스 모델 통일

> 계획서 = `_milestone-plan.md` Phase 분해 표 #2 + "확정 설계 — 대쉬 게임플레이". P1(상태 데이터 소유) 위에서 **대쉬·넉백·lunge를 하나의 ExternalVelX 임펄스 모델로 통일**한다. 대쉬는 모멘텀 감속 → **고정거리 등속**(영호 6결정 ②).
> **trust-boundary 인접**: 서버 임펄스 = 위치 권위. reviewer 재검증.

---

## Context (왜)

현재 대쉬는 **모멘텀 감속**이다 — 초기 vx에 매 틱 `*= 0.85` 곱셈 감속(메이플 러시 같은 "고정거리 등속"이 아님). 영호 결정 ② = **고정거리 + 빠른 등속**으로 거동을 바꾸되, 그 전에 **dash/knockback/lunge가 제각각인 임펄스 표현을 ExternalVelX 단일 모델로 일관화**해야 P5(클라 예측 B)가 "예측되는 외부 임펄스" 단일 기계를 만들 수 있다. 통일 없이 거동만 바꾸면 P5에서 각 임펄스마다 다른 replay 로직이 필요해진다.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, **P1 적용 후 브랜치 `feature/m4.13-impulse-class` 기준 재실측**)

> ⚠️ 옛 표는 main `2433ab5`(P1 전) 좌표라 stale이었음 — P1이 `ProcessDash`를 `DashAction`으로 이관하고 lunge/decay를 `AttackState.Enter` 소유로 옮기며 줄번호가 이동. 아래는 현재 브랜치 실측(carry-over "박제/추천 전 file:line 실측").

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 대쉬 진입(모멘텀 감속) | `Maps/Actions/DashAction.cs:22-24`(`EnterAttackState(DashLungeInitialVx×Facing, DashLungeDecayPerTick)`) | **P1에서 `ProcessDash`→`DashAction` 이관.** 대쉬 진입 시 초기 vx + 감쇠 계수를 AttackState에 위임. |
| 1b. 매 틱 곱셈 감속 | `Maps/States/PlayerCombatStates.cs:50-52`(AttackState.Tick: `AttackLungeVx *= LungeDecayPerTick` + ε 클램프) | 매 틱 vx 곱셈 → 모멘텀 감속(고정거리 아님). `StateTicksRemaining`=`AttackCommitWindowTicks`(8) 윈도우. |
| 2. 대쉬 상수(서버 전용) | `Combat/CombatConstants.cs:103`(DashLungeInitialVx=10.0f)/`:109`(DashLungeDecayPerTick=0.85f)/`:27`(AttackLungeInitialVx=3.0f)/`:114`(DashBoxHalfX=2.5f) | **02_Server 전용** — P4에서 공유 추출 대상. |
| 3. 넉백 임펄스 | `98_Shared/GameData/Constants.cs:79`(KnockbackInitialVx=7f)/`:93`(KnockbackDecayPerTick=0.75f)/`:87`(ExternalImpulseEpsilon=0.05f) · `PlayerCombatStates.cs:89-91`(HitState 넉백 감쇠+ε 클램프) | 넉백은 **이미 Shared 상수** + ε 클램프(M4.11 P2 force-adopt 계약). |
| 4. ExternalVelX 합성·채널 | `Maps/GameMap.cs:244`(`ExternalVelX = KnockbackVx + AttackLungeVx`) · `98_Shared/GameData/Physics.cs:148`(`vx = InputX*MoveSpeed + ExternalVelX`) · `PlayerEntity.cs:121`(AttackLungeVx)/`:128`(LungeDecayPerTick) | 임펄스 합성 + 물리 채널 **이미 존재**(통일의 토대). 두 필드는 상호배타 State(Hit vs Attack)라 합=활성값. |

---

## 확정 설계 (영호 GO — 2026-06-13)

### A. 임펄스 모델 통일 — 단일 필드 + 단일 감쇠 경로

현재 `KnockbackVx`(HitState) + `AttackLungeVx`(AttackState)는 **상호배타**(둘 다 0 또는 하나만 활성)인데도 필드가 2개라 `GameMap.Tick:244`에서 합으로 더해 쓴다. 셋(평타 lunge/대쉬/넉백)이 전부 "초기 vx → 매 틱 감쇠 → ε 미만이면 0"의 **동일 기계**다.

→ **두 필드를 단일 `ExternalImpulseVx` + `ImpulseDecayPerTick`으로 병합.** 감쇠 1틱 로직(`vx *= decay; if |vx| < ε → 0`)을 **단일 경로**(공유 헬퍼 또는 `PlayerEntity` 메서드)로 추출해 AttackState/HitState가 같은 것을 호출.
- **왜**: P5(클라 예측 B)가 서버 임펄스 궤적을 *비트단위 재현*해야 함 → replay 기계가 하나면 결정성 계약도 하나. 두 필드/두 경로면 재현 버그 위험 2배(★마일스톤 핵심 리스크).
- **DRY 정당성(§2.5)**: 두 감쇠는 *우연한 중복*이 아니라 **같은 임펄스 감쇠 계약**(같은 ε, 같은 force-adopt 계약) → 추출 정당.
- **넉백 ε 계약 비트단위 보존**: HitState의 `KnockbackDecayPerTick(0.75)` + `ExternalImpulseEpsilon(0.05)` 클램프 거동은 **M4.11 P2 클라 force-adopt 계약**이므로 통일 후에도 *동일 결과* 보장(grep/테스트로 거동 불변 입증).

### B. 대쉬 거동 — 모멘텀 감속 → 고정거리 등속 (영호 6결정 ②, 메이플 러시)

통일 모델에서 **감쇠계수 = 1.0**을 주면 자연히 "등속" 케이스 → 대쉬는 감쇠 대신 고정 틱 동안 같은 속도 유지 후 상태 종료 시 정지(Exit가 임펄스 0).

| 항목 | 값 | 근거 |
|---|---|---|
| 등속 속도 V | **10.0 u/s** (현행 `DashLungeInitialVx` 첫 틱 속도 그대로) | "감쇠를 빼고 첫 틱 속도를 끝까지 유지" = 직관적 전환 |
| 지속 틱 N | **8틱** (= `AttackCommitWindowTicks`, 0.4s) | 현행 대쉬 지속과 동일 |
| **고정 거리 D** | **= V × N × TickDuration = 10 × 8 × 0.05 = `4.0 unit`** (파생값, 매직넘버 아님) | 현행 모멘텀 ~2.43 → 4.0. 끝까지 안 느려지는 "확실히 꽂히는" 러시 |

- **상수화**: `DashLungeInitialVx(10)` → 의미 재정의(`DashSpeed`, 등속). `DashLungeDecayPerTick(0.85)` → **등속이라 1.0(또는 의미 소멸 정리)**. `D=4.0`은 V·N·TickDuration 파생이므로 별도 매직넘버 박지 않음(주석으로 도출 명시).
- **N 상수 분리 판단**: 대쉬 지속을 `AttackCommitWindowTicks`(commit window 의미)와 *결합 유지* vs *전용 `DashTravelTicks` 분리*는 Worker 판단(의미가 다르면 분리 — 클린코드 SRP). 단 D 도출식은 사용한 상수로 명시.
- **`DashBoxHalfX(2.5)` 타격 박스**: 거리 4.0로 늘어도 박스는 시전 시점 1회성 임팩트 판정이라 독립. 거리 변경에 맞춰 키울지는 **Play 튜닝 후속**(이 Phase 비차단).

### C. 불변식

- **wire 무변경** — 임펄스 결과는 `S_Snapshot.vx`로 이미 실림(서버 권위 위치). 필드 병합/상수 재정의 전부 서버 내부. PDL/ProtocolVersion **v12 무손상**(§2). 건드리면 STOP.
- **`LungeDecayPerTick` 호출자 직접 세팅 잔재** — P1에서 `AttackState.Enter` 소유로 이미 봉합. 대쉬 등속 전환 후 `DashLungeDecayPerTick` 잔재 0 확인.
- **단일 도메인** — 본 Phase는 **server만**(98_Shared 상수는 *읽기*만, 공유 추출은 P4). Shared write 0.

---

## 변경 대상 (파일별 — post-P1 실측 좌표)

1. **`Maps/PlayerEntity.cs:121/128`** — `AttackLungeVx` + `KnockbackVx` → 단일 `ExternalImpulseVx`, `LungeDecayPerTick` → `ImpulseDecayPerTick`. 리셋(`:261-262`) 동기화. 감쇠 1틱 헬퍼 메서드 후보 위치.
2. **`Maps/States/PlayerCombatStates.cs:43-44/50-52`(AttackState) + `:89-91`(HitState)** — 두 State가 단일 필드 + 단일 감쇠 경로 호출. AttackState=등속(대쉬)/감속(평타) 파라미터, HitState=넉백 감속(ε 계약 보존).
3. **`Maps/Actions/DashAction.cs:22-24`** — `EnterAttackState(DashSpeed×Facing, decay=1.0)` 등속 진입.
4. **`Combat/CombatConstants.cs:103/109/114`** — `DashLungeInitialVx(10)`→`DashSpeed`(등속 의미), `DashLungeDecayPerTick(0.85)`→1.0/정리, `D=4.0` 도출 주석. (`DashLungeInitialVx/Decay` 공유 추출은 P4 — 이 Phase는 서버 유지.)
5. **`Maps/GameMap.cs:244`** — `ExternalVelX = KnockbackVx + AttackLungeVx` → `ExternalVelX = ExternalImpulseVx`(단일 필드, 합 제거).
6. **`GameServer.Tests/Combat/KnightDashTests.cs`** — 필드명/거동 변경 반영 + 고정거리 도달 거리(±오차) 검증 테스트 추가(또는 신규 DashDistance 테스트).

---

## 완료 조건 / 게이트 (정량)

- [ ] 대쉬가 **고정 거리 D**(상수)를 등속으로 이동 — 서버 EditMode/통합 테스트로 도달 거리 ±오차 검증(모멘텀 감속의 가변 거리 제거).
- [ ] dash/knockback/lunge가 **같은 ExternalVelX 모델** 경유(grep으로 합성 경로 단일 제시).
- [ ] `LungeDecayPerTick` 직접 세팅 잔재 0(P1 봉합 위 — 대쉬 등속 전환 후 잔류 없음).
- [ ] 매직넘버 0 — 대쉬 거리/속도 상수화.
- [ ] **wire v12 무변경**(PDL/ProtocolVersion 무손상 grep).
- [ ] 회귀 green: WSL2 build+test 비감소 + 봇 DashSmoke(고정거리) + reviewer 재검증(서버 위치 권위 불변).

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 임펄스 = 서버 위치 권위. 거동 변경이 권위 모델을 흔들지 않음(클라는 P5에서 예측, 여기선 서버만).
- **§2 Protocol**: 모델 통일 = 내부 표현. wire 무변경 v12. 건드리면 STOP.
- **§4 공유 규율**: 이 Phase는 *서버 거동* 정의. 공식의 `98_Shared` 단일 출처화는 **P4가 수행** — 여기선 서버 상수로 진행하되 P4 추출을 염두에 둔 형태로.
- **클린코드**: 상태 소유 데이터(P1 위), 매직넘버 금지, SRP.

---

> Phase 완료 시 `02-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 03(대쉬 거동: 적 밀침 + 무적) — ⚠️P3는 *착수 직전 30분 스파이크* 선결.
