---
owner: youngho
milestone: M4.6
phase: 04
title: 몬스터 AI를 통일 State 베이스로 이주 + 적 HitState(넉백) + 선공/후공 + 클라 거울
status: done
grade: 복잡
risk: trust-boundary (CombatSystem 변경)
summary: EnemyAISystem(enum+switch Patrol/Chase)을 플레이어와 공유하는 제네릭 State 베이스(ActorState<TActor>/StateMachine<TActor>)로 이주(행동 비트 보존 = desync 0). Play 피드백으로 적 HitState(AI멈춤+넉백, 신규 기능) + 선공/후공(AggroOnSight) + 클라 거울(피격 facing/죽음 VFX) 확장. 죽음=서버 즉시 권위 유지(헌법#1), 연출만 클라 코스메틱. v9 불변.
---

# Phase 04 DONE — 몬스터 AI 통일 State 이주 + 적 행동 확장

> 브랜치 `feature/m4.6-04-monster-ai` · 커밋 `5a18242`(서버+Shared) / `8a67bcf`(클라) / `4018f2f`(봇) · 2026-06-08 (세션26)

## TL;DR

플레이어로 **검증된** 제네릭 State 베이스(`ActorState<TActor>` / `StateMachine<TActor>`)에
몬스터 AI(`EnemyAISystem` enum+switch, Patrol/Chase)를 **이주**했다. 핵심은 *행동 비트 보존* —
옛 절차형 코드가 전환 틱에 이미 한 발 이동하던 trajectory를 State.Tick이 그대로 재현(순수 FSM처럼
"전환만, 이동은 다음 틱"이면 추격 1틱 영구지연 → desync ≠ 0).

> **★ 정직 박제 (reviewer 지적)**: 본 Phase 계획은 "순수 구조 이주만"이었으나, Play 실측 피드백을
> 반복하며 **적 행동 전반으로 스코프가 확장**됐다. 특히 **적 HitState(넉백)는 *신규 기능*** —
> 옛 적은 피격 시 latch(애니 래치)만, 넉백·AI멈춤 없었음. "순수 이주(행동 100% 보존)"가 아니다.

- **제네릭 베이스 일반화**: `ActorState`/`StateMachine`을 `<TActor>` 제네릭으로 → 플레이어·몬스터가
  **같은 베이스 재사용**(추상화 값어치가 actor 2종으로 실증). 플레이어 상태는 타입 스레딩만(로직 불변).
- **적 FSM**: `EnemyStates`(Patrol/Chase/EnemyHitState) **Flyweight 정적 인스턴스**(틱루프 0-allocation, 헌법#5).
  `GameMap.SpawnEnemy`가 `Fsm` 와이어링(**Boss 제외** — BossBehaviorSystem 전담 유지). `enemy.State` byte는
  각 State.Enter가 쓰는 **wire 미러**로 유지 → 프로토콜 불변.
- **적 HitState(신규)**: 피격 시 AI 일시정지 + 넉백 감쇠, latch 소진 후 `ResolveAfterHit`로 복귀.
- **선공/후공**: `EnemyStats.AggroOnSight`(슬라임=false 후공 / 골렘=true 선공). `PatrolState`가 시야 게이트,
  후공은 **피격이 aggro 트리거**(`CombatSystem`이 `TargetEntityId=attacker` 세팅 → `ResolveAfterHit`가 Chase).
  종류별 **코드 분기 0**(데이터 플래그로 행동 분기). aggro 6→4 튜닝.
- **클라 거울**: 피격 중 facing=공격자 방향(`EnemyMotion`, LocalPlayerMotion과 동형) + 죽음 VFX
  (`EnemyRegistry.Despawn` → `ForceDeathState` → 0.8s 코루틴 destroy). **게임플레이 상태 무접촉 = 코스메틱**.
- **죽음 = 서버 즉시 권위 유지**(헌법#1): 서버 DeathState(0.8s 지연) 시도가 테스트 5개+통합 깨서 →
  사용자 결정으로 **클라 코스메틱 VFX**로 전환(플레이어 사망 페이드 패턴과 동형). 서버 death-delay revert.
- **ProtocolVersion 9 불변**: `EnemyStats`는 패킷 아닌 struct, `AggroOnSight`는 가산 필드 → wire 무관.

## AC 검증 결과

| 완료 조건 (계획) | 결과 | 근거 |
|---|---|---|
| 몬스터 순찰/추격 동일 — desync 0 | ✅ | 행동보존: 전환 틱에 결정된 상태 이동까지 같은 틱. 회귀 테스트 `Aggro_TransitionTick_MovesAsChase_SameTick` / `DeAggro_TransitionTick_MovesAsPatrol_SameTick` |
| aggro/de-aggro 경계 회귀 0 | ✅(조정) | 단위 테스트로 진입/이탈 틱 고정. **단 aggro 6→4는 의도적 튜닝**(회귀 0이 아니라 의도 변경 — Play 피드백) |
| 골렘(Kind=2) 동일 베이스 동작 | ✅ | `GolemTests` + `Proactive_AggrosOnSight`. 종류 분기는 `AggroOnSight` 플래그(코드 분기 0 유지) |
| `dotnet test` green + 회귀 0 | ✅ | 풀 테스트 **461/0/4skip** |
| reviewer 🔴 0 | ✅ | 🔴0/🟡0, 7 핵심 불변식 통과 |
| **(스코프 확장) 적 HitState 신규** | ✅ | `EnterHitState`=latch+넉백, `Hit_PausesAiMovement_AndKnocksBack`, `Boss_EnterHitState_LatchOnly` |
| **(스코프 확장) 선공/후공** | ✅ | `Reactive_DoesNotAggroOnSight` / `Reactive_AggrosAfterHit` / `Proactive_AggrosOnSight` |
| **(스코프 확장) 클라 facing + 죽음 VFX** | ✅ | `EnemyMotion`/`EnemyRegistry`, Play 실측 사용자 확인 |

**검증 경계**: 서버 로직(이주/HitState/선공후공/죽음권위)은 단위 테스트 + reviewer로 구조 보장.
클라 거울(facing/죽음 연출)은 Play 실측(사용자 영역, `unity-visual-work-user-owned`). 죽음 모션은
슬라임/골렘 Animator에 Death 클립 연결 필요(코드는 `animState=Death` 세팅만 — 미연결 시 0.8s 포즈 홀드).

## Play 실측 결과 (WSL2 서버 + Unity Play, 사용자 검증 완료)

- **슬라임 후공 / 골렘 선공 / 피격 시 공격자 바라보기 / 죽음 연출** 모두 Play 확인 ✅ ("OK 실측 성공").
- **봇 EnemyAiSmoke (Option A, GREEN)**:
  - **슬라임 후공 = hard GREEN**: 접근해도 Patrol 유지(`stayedPatrolAfterApproach=True`) — Phase 04 핵심
    행동 변화가 end-to-end 실증.
  - **골렘 선공 + 슬라임 hit→Chase = soft-deferred**: 라이브 환경에서 flaky라 단위 테스트에 위임.
    - 골렘: roster 부재(앞선 플레이/봇에 죽고 **respawn 대기**) → `Proactive_AggrosOnSight` 위임.
    - hit→Chase: 봇 공격이 **헛스윙**(움직이는 patrol 슬라임에 AABB 위치 못 맞춤 = *공격-타겟 결합* 증상,
      서버 로그상 `C_Attack` 수신했으나 데미지 이벤트 0) → `Reactive_AggrosAfterHit` 위임. **서버 버그 아님.**
  - **no silent caps**: soft 강등 항목은 콘솔에 "왜 검증 못 했고 어디 위임했는지" 명시 출력.

## 결정 흐름

1. **제네릭 베이스 vs 적 전용 추상화**: 진짜 통일(플레이어·몬스터·보스 한 베이스) 위해 `ActorState<TActor>`
   선택. trade-off: 제네릭 타입 복잡도 ↑ vs 코드 중복 0 + 베이스가 actor 2종으로 검증. 후자 우선.
2. **행동 비트 보존 (#1 함정)**: 옛 EnemyAISystem은 전이 블록이 State 변경 → 같은 루프 movement 블록이
   바뀐 State로 이동(전환 틱에 한 발). State.Tick이 "전환 결정 → 결정된 상태 이동까지 그 자리"로 재현.
   "더 우아한 순수 FSM(전환만)"의 유혹을 *비트 동일성 + 고정 테스트*로 거부 — desync 0의 진짜 열쇠.
3. **wire enum 유지**: `enemy.State` byte(Idle/Patrol/Chase)를 각 State.Enter가 씀 → 프로토콜 불변(v9).
   클라는 여전히 animState만 소비. AI State(서버 내부) ↔ AnimState(시각) 분리 유지.
4. **HitState 도입(Play 피드백)**: 옛 적 피격=latch만 → AI 멈춤 + 넉백 추가(신규 기능). `EnterHitState`는
   Boss(`Fsm==null`)면 latch만, 일반 적이면 `KnockbackVx` + `Fsm.ChangeState(Hit)`. latch 감소는 System 담당.
5. **선공/후공 = 데이터 플래그**: `EnemyStats.AggroOnSight`(가산 필드)로 종류별 코드 분기 0 유지. 후공은
   PatrolState 시야 게이트를 안 지나고, 피격 시 `ResolveAfterHit`이 hit-set 타겟 우선으로 Chase 결정.
6. **죽음 = 클라 VFX (서버 DeathState 포기)**: 서버 DeathState로 0.8s 지연 시도 → S_EntityDeath 즉시→0.8s가
   테스트 5개(KillBroadcast/DuplicateDeath/NormalEnemy_NoStageClear/LagSim/Respawn) + 통합 깨뜨림. 사용자
   결정으로 **클라 코스메틱**(플레이어 사망 페이드 동형)으로 전환. 서버 즉시사망 권위 유지(헌법#1). death-delay revert.
7. **봇 Option A**: 라이브 flaky 행동(골렘 respawn/공격 명중)은 결정론적 단위 테스트에 위임, 봇은 안정 관측
   가능한 "슬라임 후공"만 hard. 공격-타겟 결합 명중 강제는 연기된 작업과 충돌하므로 fight 안 함.
8. **Plugins DLL 규율**: `Shared.dll`만 정식 갱신(`AggroOnSight` 반영), 빌드 동반 drift `ClientNet.dll`은
   복원(소스 무변경). Debug DLL 커밋(CI 기본 = Debug).

## 잔여 / 후속

- **(연기 확정)** 공격이 타겟 근접에 결합된 구조 → **허공 스윙 + 서버 AABB 별도 판정**(Knight+Mage). 다음
  phase 또는 별도 마일스톤. memory `future-attack-decouple-swing-from-hitdetection`. 봇 hit→Chase 검증도 이게 풀려야 안정.
- **죽음 모션 연결**: 슬라임/골렘 Animator에 Death 클립 연결(사용자 영역, unity-bridge) — 코드는 준비됨.
- **봇 골렘 선공 라이브 검증**: respawn 타이밍 + 공격 명중 안정화 후 Phase 06(회귀)에서 hard 승격 검토.
- 🟡 (reviewer, 전부 선택): ① 적 HitState 넉백 = 신규 기능(본 DONE에 명시로 정정 완료). ② HitState 복귀 첫 틱
  정지 = 의도적 비대칭(주석 박힘). ③ stun 진입 책임 위치 비대칭(`EnemyEntity.EnterHitState` vs 플레이어 `HitState.Enter`).
- 다음 = **Phase 05** 보스 State + telegraph 상수(server+shared).

## 학습 일지 후보 키워드

- State 패턴 이주에서 **행동 비트 보존**이 desync 0의 열쇠 — "우아한 순수 FSM" < 비트 동일성 + 그걸 고정하는 회귀 테스트
- 제네릭 베이스(`ActorState<TActor>`) = 추상화 값어치 실증 — player/enemy/boss가 같은 베이스 재사용
- 선공/후공 = **데이터 플래그**(`AggroOnSight`)로 종류 분기 0 — 행동 차이를 코드 if가 아니라 데이터로
- 게임플레이 상태(HP/죽음 판정)=서버 권위 / **코스메틱(죽음 연출·facing)=클라** — 서버 death-delay가 테스트 깨면 클라로 옮기는 게 정석
- 봇 soft-deferred: 라이브 flaky 행동(움직이는 타겟 명중/respawn 타이밍)은 **결정론적 단위 테스트에 위임**, no silent caps로 위임 사실 로깅
- `EnemyStats` struct 가산 필드 = 프로토콜 무관(패킷 아님) — wire 영향 없이 서버 행동 데이터 확장
- false-promise 정정: 계획 "순수 이주"가 Play로 무효화될 때 DONE.md가 *정직하게* "신규 기능"으로 박제(reviewer 게이트)
