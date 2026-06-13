---
owner: youngho
milestone: M4.13
phase: 02-server-impulse-model
title: 서버 임펄스 모델 통일 — dash/knockback/lunge ExternalVelX 일관화 + 대쉬 고정거리 등속
status: planned
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

## 증거 사슬 (현재 코드 실측 — 2026-06-13)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 대쉬 모멘텀 감속 | `Maps/Systems/SkillSystem.cs:74-140`(ProcessDash) · `:95`(LungeDecayPerTick = DashLungeDecayPerTick) | Dash 진입 시 초기 vx 세팅 + 감쇠 계수 부여. |
| 1b. 매 틱 곱셈 감속 | `Maps/States/PlayerCombatStates.cs:38-39`(AttackState.Tick: `*= LungeDecayPerTick`) | 매 틱 vx 곱셈 → 모멘텀 감속(고정거리 아님). |
| 2. 대쉬 상수(서버 전용) | `Combat/CombatConstants.cs:100`(DashLungeInitialVx=10.0f)/`:106`(DashLungeDecayPerTick=0.85f)/`:27`(AttackLungeInitialVx=3.0f) | **02_Server 전용** — P4에서 공유 추출 대상. |
| 3. 넉백 임펄스 | `Constants.cs:79`(KnockbackInitialVx=7f)/`:93`(KnockbackDecayPerTick=0.75f)/`:87`(ExternalImpulseEpsilon=0.05f) · `PlayerCombatStates.cs:72-77`(HitState 넉백 감쇠+클램프) | 넉백은 **이미 Shared 상수** + ε 클램프(M4.11 P2). |
| 4. ExternalVelX 합성·채널 | `Maps/GameMap.cs:244`(`ExternalVelX = KnockbackVx + AttackLungeVx`) · `98_Shared/GameData/Physics.cs:148`(`vx = InputX*MoveSpeed + ExternalVelX`) | 임펄스 합성 + 물리 채널 **이미 존재**(통일의 토대). |

---

## 설계 방향 (착수 시 확정 — 골격)

- **고정거리 등속(영호 ②)** — 대쉬를 "초기 vx × 0.85^t 감속"에서 **"고정 거리 D를 빠른 등속 V로 N틱"**으로. D/V 상수 신설(매직넘버 금지). 거리 도달 또는 틱 카운트로 종료(상태 소유 — P1 위).
- **임펄스 표현 일관화** — dash/knockback/lunge가 모두 `ExternalVelX` 채널 + "지속 틱/감속 정책"을 **상태가 소유**(P1 데이터 소유). 차이는 *시작 파라미터*(대쉬=등속 D/V, 넉백=감속 vx)만.
- **`LungeDecayPerTick` 호출자 직접 세팅 잔재 제거** — P1에서 상태 소유로 옮긴 것 위에서, 대쉬 등속 전환으로 `DashLungeDecayPerTick` 자체가 의미를 잃으면 정리(착수 시 확정).
- **wire 무변경** — 임펄스는 `S_Snapshot.vx`로 이미 실림(서버 권위 위치). 모델 통일이 패킷 형상 안 건드림(§2 v12 유지).

---

## 변경 대상 (파일별 — 착수 시 확정)

1. **`SkillSystem.cs:74-140`(ProcessDash)** — 모멘텀 감속 → 고정거리 등속 진입 로직.
2. **`Combat/CombatConstants.cs`** — `DashFixedDistance`/`DashSpeed`(또는 유사) 신설, `DashLungeInitialVx/Decay`는 P4 공유 추출까지 서버 유지(이 Phase는 거동, P4가 위치 이동).
3. **`PlayerCombatStates.cs:38-39`** — 대쉬 상태의 감속 곱셈 → 등속/거리 종료 로직(상태 소유).
4. **`GameMap.cs:244`** — ExternalVelX 합성 일관성 점검(대쉬 등속도 같은 채널 경유).

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
