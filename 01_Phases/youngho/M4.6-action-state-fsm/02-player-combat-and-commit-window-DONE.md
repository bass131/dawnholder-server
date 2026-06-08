---
owner: youngho
milestone: M4.6
phase: 02
title: 플레이어 전투 상태 + commit window 규칙 + 공유 상수 (+ HitState 잠금/넉백)
status: done
grade: 복잡
risk: trust-boundary
summary: 플레이어 전투를 누더기 latch 카운터에서 단일 ActionFsm State 스왑으로 통일하고, 서버 권위 commit window(공격 중 이동 잠금) + HitState 잠금/넉백을 도입. v9 불변, 회귀 0.
---

# Phase 02 DONE — 플레이어 전투 State + commit window + HitState 넉백

> 브랜치 `feature/m4.6-02-player-combat-and-commit-window` · 커밋 `34d443e` · 2026-06-08 (세션24)

## TL;DR

플레이어의 Attack/Hit/Death가 **latch 카운터 3개 + 우선순위 덮어쓰기(누더기)**였던 것을,
이동(Idle/Move/Jump)과 합쳐 **단일 `ActionFsm`**(6개 State 클래스 스왑)으로 통일했다.
이 마일스톤의 핵심 규칙 **"공격은 commit window 동안 이동 잠금(서버 권위)"**을 도입하고,
사용자 요청으로 **HitState 이동 잠금 + 넉백**까지 흡수했다.

- `AttackState` = commit window: 이동 잠금(`LocksMovement`) + **불가침**(`InterruptibleByHit=false`, Death만 인터럽트). 지속 = `Constants.AttackCommitWindowTicks(8)` **98_Shared 신규**(클라 Phase 03 예측 동일값).
- `HitState` = 잠금 + **넉백**: 무관성 물리(`vx=inputX×MoveSpeed`) 때문에 `Physics`에 좁은 임펄스 채널 `ExternalVelX`(기본 0) 추가. 공격자 반대 방향 초기 속도 → hitstun 동안 감쇠 → 지형 충돌은 Physics가 처리.
- `DeathState` = terminal(부활 = `Revive()`로 Idle 복귀). 플레이어 사망=즉시부활 보존(구조 완성용).
- `ComputePlayerAnimState`는 FSM에 완전 위임(우선순위 분기 삭제). 플레이어 latch 필드 3개 제거.
- **ProtocolVersion 9 불변**(신규 패킷 0). 적(enemy) latch 시스템 완전 무손상(Phase 04 영역).

## AC 검증 결과

| 완료 조건 | 결과 | 근거 |
|---|---|---|
| 공격 중 이동 잠금이 서버에서 강제 | ✅ | `CommitWindowTests.AttackState_BlocksMovement_InputXIgnored` — AttackState 중 위치 델타 0 |
| 우회 불가 입증 (조작 대량 입력) | ✅ | `AttackState_BlocksMovement_MultipleInputs` — 연속 입력 주입에도 위치 불변 |
| 경계 틱(off-by-one) | ✅ | `AttackState_ExpiresAtExactBoundaryTick` — 정확히 8틱 잠금, 9틱째 복귀 |
| commit window = 98_Shared 단일 상수 | ✅ | `Constants.AttackCommitWindowTicks`(서버 전용 `AnimLatchTicks`와 의미 분리 주석) |
| HitState 이동 잠금 + 넉백 (사용자 추가) | ✅ | `HitKnockbackTests` 7종 — 입력 무시/방향/감쇠/지형 벽 막힘/불가침 no-op/ExternalVelX=0 불변 |
| 행동 보존 (단일 상태 animState 바이트) | ✅ | `AnimState_EquivalenceMatrix_MatchesLegacyPriority` (옛 우선순위 매핑 == FSM) + epsilon 경계 |
| dotnet test green + 기존 전투 회귀 0 | ✅ | **448 통과 / 0 결정론 실패 / 4 skip** (WSL2, ADR-029) |
| ProtocolVersion 9 불변 | ✅ | 패킷/enum diff 0, 신규 패킷 0 |
| 봇 desync 0 (이동 보존 end-to-end) | ✅ | M2BasicMovement: `bot=(5.97,0.00) server=(5.97,0.00) desync=0.00` |
| reviewer 🔴 0 (trust-boundary) | ✅ | 5축+trust-boundary 7항목 통과, 🟡 2건(선택) |

**flaky 1건 명시**: `MapTransition_EntityIdPreserved`(통합 테스트)는 풀 스위트 부하에서 25s 소켓 타임아웃, **isolation 단독 실행 시 17s로 통과**(2회 확인). assertion 실패가 아니라 부하성 타임아웃이며 본 변경(포털/마이그레이션 무관)과 무관. 테스트 13개 증가(440→453)로 borderline 타임아웃을 넘긴 것. Phase 06(회귀/마감)에서 timeout 상향/skip-gating 검토 대상.

## 결정 흐름

1. **누더기 진단 → 단일 ActionFsm (사용자 확신)**: latch 카운터는 State가 아님. "State를 Class 단위로 Action화 후 스왑" 구조로 전환. latch 필드 완전 제거(브리지 안 남김).
2. **멈춤 감각 ① 입력 게이트(콱 정지)**: `Physics` 실측 결과 무관성(`vx=inputX×MoveSpeed`). 입력만 0으로 막으면 즉시 정지가 공짜 → 별도 vx=0 불필요. 전면 관성(②안)은 이동 감각 전용 별도 마일스톤으로 주차. (근거 = `_attack-stop-feel-comparison.html`)
3. **불가침 commit (사용자 선택)**: AttackState는 Death만 인터럽트. 피격해도 HP는 깎이고 `S_EnemyAttack`은 가되 스윙은 끝까지(animState Attack 유지). 옛 Hit>Attack 우선순위와 의도적 분기(단일 상태는 등가성 보존).
4. **HitState 잠금 + 넉백 (사용자 추가)**: 무관성 물리라 `Velocity.X` 직접 세팅 불가(덮어써짐) → `Physics.PhysicsInput.ExternalVelX`(기본 0, 3인자 ctor 위임) 임펄스 채널. 지형 X-스윕이 vx를 쓰므로 넉백도 자동으로 벽에 막힘(충돌 로직 복붙 회피). 넉백 상수 98_Shared(클라 Phase 03 예측).
5. **상수 분리 유지**: `AttackCommitWindowTicks`(공유, 게임플레이) ≠ `CombatConstants.AnimLatchTicks`(서버, 적 시각 latch). 같은 8이라도 의미 달라 합치지 않음(drift 방지).
6. **dirX 의미 봉합**: 신규 테스트 2개가 dirX 의미를 "공격 온 방향"으로 오해 → 코드/`SameAsDirX` 테스트는 "넉백 날아갈 방향(공격자 반대)". 후자로 통일 + PlayerEntity 주석 정정.

## 잔여 / 후속 (reviewer 🟡, 선택)

- 🟡 `HitState.Enter`가 hitstun 길이를 `CombatConstants.AnimLatchTicks`(적 시각 latch)에 의존 → `HitstunTicks` 전용 서버 상수 분리 검토(지금 동작 무해, 적 latch 튜닝 시 플레이어 hitstun 의도치 않게 따라 변하는 drift 방지).
- 🟡 넉백 감쇠 곡선(`0.75^8≈0.1`) 중간 틱 검증 테스트 1개 추가(현재 종단 0 수렴만 검증).
- Phase 03(클라 미러): commit window/넉백을 클라가 같은 상수로 예측 → rubber-band 0. 본 Phase가 그 단일 진실 상수를 깔았다.

## 학습 일지 후보 키워드

- State 패턴 = 카운터+우선순위(누더기) vs 클래스 스왑(타입으로 "지금 무슨 상태"가 드러남)
- Flyweight: 무필드 State 싱글톤 + 엔티티가 가변 데이터 소유 → 틱 루프 new 0 (헌법 #5)
- 서버 권위 게임플레이 규칙 vs 시각 효과: 이동 잠금=규칙(서버), Animator Exit Time=거울(클라)
- 무관성 물리에서 넉백 = 임펄스 채널(외부 속도 가산) — 일반 이동(관성 0)과 분리
- 단일 진실 상수: commit window/넉백을 98_Shared에 → 클라 예측이 같은 값으로 게이트
- 통합 테스트 flaky 진단: isolation 통과 + 풀 스위트 부하 타임아웃 = 로직 OK, 인프라 타이밍
