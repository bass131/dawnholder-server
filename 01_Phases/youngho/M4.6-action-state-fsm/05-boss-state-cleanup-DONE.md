---
owner: youngho
milestone: M4.6
phase: 05
title: 보스를 명시적 3-State 머신으로 정리 + telegraph 상수 98_Shared 단일화
status: done
grade: 복잡
risk: trust-boundary (보스 데미지 판정 경로 — 비트 보존)
summary: 필드 조건분기(IsPhase2/Telegraph/Cooldown)로 된 BossBehaviorSystem을 명시적 3-State(BossIdleState/BossTelegraphState/BossAttackState)로 이주 — 옛 동작과 비트 동일(회귀 0). telegraph 예고 상수(P1=16/P2=10)를 98_Shared로 단일 출처화(cooldown/damage/range는 서버 전용 유지). 플레이어(02)·몬스터(04)에 이어 셋이 같은 제네릭 StateMachine<TActor> 베이스 = 통일 구조 완성. v9 불변, 클라 무접촉.
---

# Phase 05 DONE — 보스 3-State 이주 + telegraph 단일화

> 브랜치 `feature/m4.6-05-boss-state` · 커밋 `4365551`(server+shared+tests+Shared.dll) · 2026-06-08 (세션27)

## TL;DR

보스가 마지막으로 **암묵적 상태**(두 필드 `TelegraphTicksRemaining`/`AttackCooldownTicks`의 조합으로만
"지금 예고 중? 쿨다운 중?"이 표현되던 if-else)로 남아 있었다. 이를 **명시적 3-State**
(`BossIdleState`/`BossTelegraphState`/`BossAttackState`)로 이주 — 옛 조건분기와 **타이밍 비트 동일**(회귀 0).
이로써 플레이어·몬스터·보스 셋이 모두 같은 제네릭 `StateMachine<TActor>` 베이스에서 돌아 **통일 구조 완성**.

추가로 telegraph 예고 상수(P1=16틱/P2=10틱)를 `98_Shared/Constants.cs`로 **단일 출처화**. 변조 위험은
서버유지/shared 동일(상수는 wire 전송 X + 서버가 자기 카운터로 판정 = 클라 복사본 변조해도 권위 영향 0)임을
확인한 뒤 결정 — telegraph는 플레이어向 공정성 신호라 노출 무해, cooldown/damage/range는 서버 전용 유지(least-exposure).

## 핵심 함정 (비트 보존)

- **★ off-by-one (3-State의 비-자명 보정)**: 제네릭 `StateMachine.Tick`은 *전환 시 새 상태의 Tick을 그 틱에
  재호출하지 않음*(Tick 1회 → ChangeState만). 그래서 공격을 별도 `BossAttackState`로 빼면 Attack→Idle 전환이
  틱 1개를 "소비"해 쿨다운 감소가 1틱 밀려 **다음 telegraph가 영구 1틱 지연(누적 drift)**. 보정: `BossAttackState.Tick`이
  `cooldown--`을 **1회 수행**한 뒤 Idle 복귀 → 옛 코드(공격 틱엔 감소 X, 다음 틱부터 else 분기 감소)와 비트 일치.
  → 주석(`BossStates.cs`) + 회귀 테스트(`BossAttack_OffByOneCooldown_ResetsThenDecrementsNextTick`)로 박제.
- **★ EnterHitState 가드**: 보스에 Fsm을 주는 순간 옛 `if (Fsm == null) return` 가드가 뚫려 보스가
  `EnemyStates.Hit`(넉백+AI멈춤)로 전환됨 → telegraph 중 피격 시 공격 끊김(회귀). 가드를 `Kind==EnemyKind.Boss`로
  교체해 보스를 latch-only 보존.
- **wire enum 불변**: 보스 `enemy.State`는 Idle 고정(Telegraph/Attack은 신규 enum값 X). 보스 시각은 `animState`(AttackLatch)가 구동.

## ★ 검수 박제 (메인 검수 의무 실증 — 정직 기록)

`server` Worker 1차 산출물에서 메인 검수가 **결함 3건 + 설계 일탈 1건**을 잡아 직접 정정했다. "Worker commit 금지
+ 메인 검수 의무"가 실제로 결함을 걸러낸 사례:

| 구분 | Worker 산출 | 정정 |
|---|---|---|
| 🔴 버그 | telegraph "이동"이 실은 **복제**(CombatConstants에서 미제거) → 양쪽 중복 정의, 프로덕션은 옛 출처 참조 | CombatConstants에서 제거 → 98_Shared 단일 출처 확정(grep 검증, 중복 0) |
| 🔴 버그 | non-boss Fsm **이중 생성**(ctor + SpawnEnemy 둘 다) | SpawnEnemy 단일 생성으로 정리(ctor에서 Fsm 생성 제거, State만 세팅) |
| 🟡 주석 | stale 2-State 잔재 주석 2건(`BossAttackState.Enter` 참조가 그 클래스 부재 시점에 박힘 등) | reviewer 지적 흡수 후 정정 |
| 일탈 | 승인된 3-State를 **2-State**(attack fold)로 임의 변경 | 사용자에게 surface → "3-State 유지" 선택 → 메인이 비트 정확 재구성 |

> **교훈**: 깔끔해 보이는 일탈(2-State)도 *승인된 설계*를 말없이 바꾸면 안 됨 — surface 후 사용자 결정. 또한
> "이동을 옮긴다"는 작업은 *옛 위치 제거*까지 한 묶음(추가만 하면 복제). reviewer가 단일출처/비트보존을 독립 점검.

## AC 검증 결과

| 완료 조건 (Phase 정의) | 결과 | 근거 |
|---|---|---|
| 보스가 명시적 State로 동작 — BossFight 회귀 0 | ✅ | 3-State 그래프, 옛 조건분기와 비트 동일. `BossStateTests` 5종(전환 그래프 + off-by-one) |
| P2 telegraph 상수 **98_Shared 단일 출처**(중복 0) | ✅ | CombatConstants 제거 + grep 검증(98_Shared에만 정의, BossStates/테스트가 Constants.* 참조) |
| HP50% 페이즈 전환 정확히 1회(idempotent) | ✅ | 기존 `BossBehaviorTests` Phase2 4종 회귀 0 |
| 범위 내 플레이어만 데미지(서버 권위) | ✅ | `BossStates.ApplyBossAttack` player.Position만 사용, 본문 로직 1:1 이전 |
| `dotnet test` green + ProtocolVersion 9 불변 | ✅ | **466/0/4skip**(기존 461 + BossStateTests 5) · `ProtocolVersion.Current==9` assert |
| reviewer 🔴 0 | ✅ | 🔴0 · 🟡1(주석) 봉합. 7축 + 비트보존 직접 대조 |

## Play 실측

Phase 05는 *옛 고정형 보스의 비트 동일 리팩토링*이라 동작이 M4.5(Play 검증 완료)와 같음 — 단위 테스트(비트 정확)
+ reviewer가 회귀 0을 보장. Play는 선택(고정형 보스 = 예고→공격→HP50% 가속이 이주 전과 동일한지 체감 확인용).

> **다음(Stage 2, 별도 phase)**: 사용자 요청으로 **보스 이동 행동**(Idle→Move→탐지→접근→공격) 설계 완료
> (plan 저장). 클라 무수정 + v9 불변(S_EntityState 재사용)으로 *순수 서버 작업*임을 탐색으로 확정. Phase 05 PR 후 착수.

## 결정 흐름

1. **2-State vs 3-State**: 공격은 순간 1틱 이벤트라 2-State(telegraph에 fold)가 더 깔끔하나, Phase 정의 + 사용자가
   명시적 `BossAttackState`(미래 다중공격 확장 여지 + "지금 무슨 상태?"가 타입으로 드러남)를 선택 → 3-State + off-by-one 보정.
2. **telegraph 위치 (서버 vs 98_Shared)**: 변조 위험 동일(상수는 권위에 영향 0) 확인 후, 사용자가 Phase 정의대로
   98_Shared 선택. cooldown/damage/range는 least-exposure로 서버 잔류.
3. **Fsm 생성 위치**: ctor(Worker안) vs SpawnEnemy(plan). OwningMap 세팅 후 생성하는 깔끔한 불변식 위해 SpawnEnemy 단일화.
4. **wire enum**: 보스에 신규 EnemyState값 추가 X — 클라가 `state`를 시각에 안 쓰고 `animState` 사용하므로 Idle 고정이 안전.

## 회귀 안전망

- `BossStateTests`(신규 5): Idle→Telegraph→Attack→Idle 전환 그래프 / P1·P2 틱 / off-by-one / EnterHitState 가드.
- `BossBehaviorTests`(기존, 상수 참조만 rename): 페이즈 전환·쿨다운·데미지·리스폰·animState 우선순위 회귀 0.
- `EnemyStateTests.Boss_EnterHitState_LatchOnly`: 보스 Fsm 존재 + latch-only 보존.

## 학습 일지 후보 키워드

- **암묵적 상태 → 명시적 State**: 두 필드 조합(`TelegraphTicksRemaining`/`AttackCooldownTicks`)으로만
  표현되던 상태를 타입으로 드러냄. "지금 무슨 상태?"가 코드에 명시됨.
- **off-by-one (전환이 틱을 소비)**: 조건분기 → State 이주 시 *전환 자체가 1틱 소비*하는지 놓치면 누적 drift.
  비-자명 보정(`AttackState.Tick`의 `cooldown--`)은 반드시 주석 + 회귀 테스트로 박제(미래의 본인이 안 지우게).
- **"이동"은 추가+제거 한 묶음**: 상수를 옮길 땐 *옛 위치 제거*까지 해야 단일 출처. 추가만 하면 복제(이번 Worker 결함).
- **메인 검수 의무 실증**: Worker 일탈(2-State) + 결함(중복/이중생성)을 메인 검수 + reviewer가 독립 검출.
- **least-exposure**: telegraph(공정성 신호)만 shared, cooldown/damage/range는 서버 전용. 변조 위험은 위치 아닌 권위가 결정.
- **헌법 #1 설계 배당금**: 보스 이동(Stage 2)이 클라 0줄 + v9 불변으로 가능한 건 서버가 x/y/animState만 쏘고 클라는 렌더만 하기 때문.
