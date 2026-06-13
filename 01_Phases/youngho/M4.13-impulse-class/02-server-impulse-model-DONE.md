---
owner: youngho
milestone: M4.13
phase: 02-server-impulse-model
title: 서버 임펄스 모델 통일 — 단일 ExternalImpulseVx + 대쉬 고정거리 등속
status: done
completed: 2026-06-13
grade: 복잡
summary: M4.13 P2 완료. KnockbackVx + AttackLungeVx 두 필드를 단일 ExternalImpulseVx + ImpulseDecayPerTick으로 병합 + 감쇠 1틱 로직을 PlayerEntity.DecayImpulse() 단일 경로로 추출(AttackState/HitState 공유). 대쉬 모멘텀 감속(매틱 ×0.85) → decay=1.0 등속, 고정거리 D = DashSpeed(10) × DashTravelTicks(8) × TickDuration(0.05) = 4.0 unit. DashTravelTicks를 EnterAttackState durationTicks 파라미터로 AttackState 지속에 실제 배선(commit window와 독립 튜닝 가능). wire v12 무변경, 넉백 ε 계약(M4.11 P2 force-adopt) 비트단위 보존. 검증 = reviewer 🔴0(위치 권위 불변 + ε 계약 명시 논증) + WSL2 562/0(비감소, +Dash_Knight_FixedDistance_4Units) + 봇 DashSmoke success(advance=4.00 실측, 3.5f 게이트, class/cooldown gate). P5(클라 예측 B)가 올라갈 단일 replay 기계 토대.
---

# Phase 02 박제: 서버 임펄스 모델 통일 + 대쉬 고정거리 등속

**소요**: P2-server 구현(server Worker) → reviewer 🔴0 → WSL2 562/0 → DashTravelTicks 배선 결함 1건 후속수정(server Worker) → 🟡 stale 주석 2건 정정(qa Worker) → Release 빌드 + 봇 DashSmoke fresh(advance=4.00). 확정 설계 = `02-server-impulse-model.md` "확정 설계 (영호 GO)" 섹션.

## TL;DR

서버엔 "외부 힘으로 캐릭터를 미는" 임펄스가 셋(평타 lunge / 대쉬 / 넉백) 있었는데, 표현이 **두 필드로 갈라져**(`AttackLungeVx`=AttackState / `KnockbackVx`=HitState) `GameMap.Tick`에서 합으로 더해 쓰고 있었다. 셋 다 "초기 vx → 매 틱 `*= decay` → ε 미만이면 0"의 **동일 기계**인데도.

→ **두 필드를 단일 `ExternalImpulseVx` + `ImpulseDecayPerTick`으로 병합**하고, 감쇠 1틱 로직을 `PlayerEntity.DecayImpulse()` **단일 경로**로 추출했다(AttackState.Tick + HitState.Tick 공유). **왜**: P5(클라 예측 B)가 서버 임펄스 궤적을 *비트단위 재현*해야 하는데, replay 기계가 하나면 결정성 계약도 하나 — 둘이면 재현 버그 위험이 두 배(이번 마일스톤 ★핵심 리스크 = forceAdopt 크러치 제거).

그 위에서 **대쉬를 모멘텀 감속(×0.85, 가변거리 ~2.43) → 고정거리 등속**으로 바꿨다(영호 6결정 ②, 메이플 러시). 통일 모델에서 감쇠계수=1.0이 곧 "등속"이라, 대쉬는 `decay=1.0`으로 8틱 동안 같은 속도 유지 후 상태 종료 시 정지. **D = DashSpeed(10) × DashTravelTicks(8) × TickDuration(0.05) = 4.0 unit**(파생값, 매직넘버 0).

## 박제 사실 (어떻게)

| 영역 | 산출 |
|---|---|
| 필드 병합 | `PlayerEntity`: `KnockbackVx`+`AttackLungeVx` → 단일 `ExternalImpulseVx`, `LungeDecayPerTick` → `ImpulseDecayPerTick`. `EnterHitState`가 `ExternalImpulseVx`+`ImpulseDecayPerTick(=KnockbackDecayPerTick)` 둘 다 세팅. `Revive()`/`AttackState.Exit` 리셋 동기화 |
| 단일 감쇠 경로 | `PlayerEntity.DecayImpulse()`(`vx *= ImpulseDecayPerTick; if |vx|<ExternalImpulseEpsilon → 0`) — `AttackState.Tick`+`HitState.Tick`이 동일 호출(DRY §2.5, 같은 ε·force-adopt 계약이라 우연한 중복 아님) |
| 단일 합성 | `GameMap.Tick:244`: `ExternalVelX = KnockbackVx + AttackLungeVx` → `ExternalVelX = ExternalImpulseVx`(합 제거, 단일 필드) |
| 대쉬 등속 | `DashAction`: `EnterAttackState(DashSpeed×Facing, decayPerTick: 1.0f, durationTicks: DashTravelTicks)`. `CombatConstants`: `DashLungeInitialVx(10)`→`DashSpeed`, `DashLungeDecayPerTick(0.85)`→삭제, `DashTravelTicks=8` 신설 |
| DashTravelTicks 배선 | `AttackState.PendingDurationTicks`(sentinel→AttackCommitWindowTicks) + `EnterAttackState(... int durationTicks=-1)` + `Enter`가 `StateTicksRemaining = PendingDurationTicks`. 죽은 상수 아님 — `DashAction→EnterAttackState→PendingDurationTicks→Enter` 실제 경로. 평타 호출자는 sentinel 기본값으로 거동 불변 |
| wire | **v12 무변경** — 필드 병합/상수 전부 서버 내부. 임펄스 결과는 기존 `S_Snapshot.vx`로 실림. PDL/Generated 접촉 0 |

## AC 검증 결과

- **reviewer (Tier 2-A, trust-boundary 인접)**: 🔴0 / 🟡2(정정 완료) / PASS. 6축 명시 논증 — ①헌법#1 위치 권위 불변(ExternalImpulseVx는 서버 tick thread만 mutate, 클라 임펄스 결정 경로 0, 외부 입력 진입점 신설 0) ②헌법#2 v12 무손상(`ProtocolVersion.Current=12`, Generated 변경 0) ③**넉백 ε 계약 비트단위 보존**(통일 전 `KnockbackVx *= 0.75; if <0.05→0` vs 후 `DecayImpulse()`가 EnterHitState 세팅한 `ImpulseDecayPerTick=0.75` + 동일 ε·부등호 — 결과 비트 동일) ④상호배타 가정 안전(AttackState.InterruptibleByHit=false + HitState.AcceptsAction=false → 두 임펄스 동시 비-0 경로 없음) ⑤대쉬 등속 D=4.0 + DashTravelTicks 실배선 ⑥상태 데이터 소유(P1 위)/DRY 정당/매직넘버0/틱 blocking0.
- **WSL2 회귀 (ADR-029)**: `dotnet build -c Release` 0 warning/0 error. `dotnet test` **Passed 562 / Failed 0 / Skipped 4 / Total 566** — P1 baseline 561 대비 **비감소 + 신규 fail 0** (+1 = `Dash_Knight_FixedDistance_4Units`, 4.0±0.01 결정적 검증).
- **봇 DashSmoke (고정거리, fresh 서버)**: `success=True`. **position before=11.40 after=15.40 → advance=4.00 unit**(등속 고정거리 end-to-end 실측). 게이트 `MinPositionAdvanceX` 1.5f→**3.5f** 강화(옛 모멘텀 2.43 회귀 시 FAIL). skillCast(skillId=2)✓ / hitEffect3✓ / cooldownRejected✓ / mageGateBlocked✓(`[Trust] class mismatch ... silent drop`). 봇 명령: `99_Tools/run_dash_smoke.sh`(WSL2 Release).

## 결정 흐름

- **단일 필드 병합 (Option B) vs 대쉬만 등속 전환 (Option A)** — B 채택. 완료 조건 "dash/knockback/lunge 같은 ExternalVelX 모델(합성 경로 단일)"이 단일 필드를 요구 + P5 단일 replay 기계가 목적. 두 필드는 상호배타(Attack vs Hit)라 병합 안전(reviewer 교차검증).
- **대쉬 N 상수: AttackCommitWindowTicks 재사용 vs 전용 DashTravelTicks 분리** — 분리 채택(SRP). "대쉬가 앞으로 나아가는 시간"과 "행동 입력을 거부하는 commit window"는 의미가 다름. 둘 다 현재 8이지만 독립 튜닝 가능. **단 도출식 주석은 *실제 실행되는 상수*여야** — 1차 구현이 DashTravelTicks를 주석에만 쓰고 실제론 AttackCommitWindowTicks가 제어하는 죽은 상수로 남겨 후속 수정(EnterAttackState durationTicks 파라미터로 실배선).
- **대쉬 거리 D=4.0** — "감쇠를 빼고 첫 틱 속도(10)를 8틱 내내 유지" = 직관적 전환. 가변거리 ~2.43 → 고정 4.0. Play 튜닝 대상이나 시작점 영호 확정.
- **DashBoxHalfX(2.5) 미접촉** — 타격 박스는 시전 시점 1회성 임팩트 판정이라 이동거리(4.0)와 독립. 거리 늘려도 박스 무관(Play 튜닝 후속).

## 막혔던 지점 / 이월

- **DashTravelTicks 죽은 상수 (후속수정으로 봉합)** — 1차 구현이 상수를 주석에만 박고 실제 제어는 AttackCommitWindowTicks가 함 → 메인 실측에서 발견, EnterAttackState 파라미터화로 실배선. 교훈: "도출식 주석은 실제 실행되는 상수로"(carry-over file:line 실측의 한 형태).
- **Dash 중 ghost(클라)** — P1에서 P2 이월됐던 것. P2는 *서버 거동만* 재정의(고정거리 등속). 클라가 이 등속 궤적을 예측·복원하는 건 **P5(클라 예측 B)** 몫. P2는 서버 권위 위치만 — 클라는 여전히 force-adopt 렌더(헌법 #1).
- **working tree dll 잔여** — P1부터 누적된 `03_Client/Assets/Plugins/{Shared,ClientNet}.dll`. P2는 클라가 새 shared API 미사용이라 dll 무관 — sync는 PR 시점.

## 학습 일지 후보 키워드

상호배타 상태 → 단일 필드 병합(두 필드 합산 제거) / 단일 감쇠 경로 추출(DecayImpulse, DRY §2.5 = 같은 계약이라 정당) / **결정성 계약 비트단위 보존**(추출 리팩토링에서 "기능 같다"=대략 같다 아님, 연산순서·계수·ε 부등호까지 — diff로 증명 가능하게 / EnterHitState가 decay 명시세팅으로 디폴트 의존 제거) / 모멘텀 감속 → 고정거리 등속(감쇠계수 1.0 = 등속 케이스) / 도출식 주석은 실제 실행 상수로(죽은 상수 = 거짓 도출식) / 봇 게이트 의미화(하한을 새 모델에 맞춰 옛 회귀 검출) / trust-boundary 인접 = reviewer 위치권위 불변 명시 논증.

## 다음 Phase

- **P4 — 공유 추출** (`04-shared-extract.md`, 보통). 대쉬/임펄스 공식을 98_Shared 단일 출처로(헌법 §4, `CombatConstants` 서버전용 → `Constants`). wire v12 무변경. **★P5 안전망**(복붙 = silent drift 차단). depends:P2.
- **P3 — 대쉬 거동** (`03-dash-behavior.md`, 복잡·trust-boundary). 적 밀침(허딩) + 완전 무적. P2 위. ⚠️착수 전 30분 스파이크(EnemyEntity ExternalVelX 채널 유무). P5와 독립.
- 단방향: P1→P2→**P4**→P5→P6, P3는 P2 위. 순서·타이밍 영호.
