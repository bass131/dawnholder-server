---
owner: youngho
milestone: M4.13
title: 임펄스 동작 클래스 재설계 — 행동 입력 게이트 + 대쉬 거동 재설계 + 서버 모델 통일 + 클라 예측 통일
status: planned
grade: 대규모
slug: M4.13-impulse-class
created: 2026-06-12
revised: 2026-06-12
domains: [shared, server, client, qa]
---

# M4.13 — 임펄스 동작 클래스 재설계

> 대쉬는 이 시스템의 **첫 적용 사례**. 마일스톤의 핵심은 *대쉬 하나*가 아니라 **"클라가 예측 못 하는 서버 임펄스 동작" 클래스(대쉬·넉백·임펄스공격)를 시스템으로 통일**하는 것. 그 토대로 **행동 입력 게이트**(상태가 행동·임펄스 데이터를 소유)를 먼저 깐다.
>
> **2026-06-12 재구성** — 행동 입력 게이트가 옛 M4.12에서 본 마일스톤 **Phase 1로 합류**(작업 응집으로 가름 — 게이트는 임펄스 토대지 발표 폴리시가 아님). 이로써 **M4.12 ⟂ M4.13 독립**(옛 `depends_on: M4.12` 해소). 본 마일스톤은 게이트를 자기 안에 품어 자족적.

---

## Context (왜)

M4.11 백로그 ① SnapThreshold Play 실측 중, 복합 동작(대쉬)에서 **시각 스터터**(20Hz 뚝뚝 끊김)가 발견됐다. 근본 원인은 대쉬 하나가 아니라 **클래스 공통 문제**다 — 대쉬·넉백·임펄스공격이 모두 *클라가 예측 못 하는 서버 임펄스*라, 매 스냅샷 `forceAdopt`로 끌려오고 M4.11 P4의 reconcile 보간 버퍼 리셋이 그걸 매번 지워 50ms 간격 스냅이 된다.

증거: 영호 Play + Unity Console `[Reconcile]` 500 로그 분석 — 단순 이동은 예측 오차 평균 0.2칸(깨끗), 대쉬는 매 스냅샷 forceAdopt(499/500)로 보간이 죽음.

이 클래스를 **시스템으로 재설계**한다. 먼저 **행동 입력 게이트**(상태가 자기 행동·임펄스 데이터를 소유 — `LungeDecayPerTick` 잔류 사고의 구조적 봉합)를 깔고, 거동(게임플레이)을 새로 정의하고(영호 6결정), 그 위에 네트워크(클라 예측 B)를 통일한다.

---

## Phase 1 토대 — 행동 입력 게이트 시스템화 (옛 M4.12에서 이관)

> 이 게이트가 **임펄스 재설계의 서버 토대**다. 여기서 *상태가 자기 행동·임펄스 데이터를 소유*하게 만드는 구조가, Phase 2~6이 임펄스 모델(`ExternalVelX`)을 일관 모델로 묶는 전제가 된다.

### 문제 (영호 지목)

특정 동작 State가 끝나고 Idle로 복귀해야 추가 입력을 받는 **예외 처리가 기술(스킬)마다 재발**한다 — 시스템적 설계가 없어서다. 새 스킬마다 상태 복귀 타이밍·입력 차단을 손으로 끼워 넣는다.

### 증거 (현재 코드 — 실측)

입력을 막는 장치가 **세 곳에 흩어져 서로 다른 방식**으로 동작한다:

1. **이동 잠금만 상태 선언식** — `AttackState.LocksMovement => true`([`PlayerCombatStates.cs:28`](../../../02_Server/GameServer/Maps/States/PlayerCombatStates.cs)), 베이스 기본 false([`ActorState.cs:18`](../../../02_Server/GameServer/Maps/States/ActorState.cs)). 틱 루프가 이 플래그로 `inputX=0`/`rawJump=false` 강제([`GameMap.cs:233`](../../../02_Server/GameServer/Maps/GameMap.cs)). 즉 *이동만* 상태가 막는다.
2. **공격/스킬 진입은 상태가 안 막고 개별 쿨다운만** — `CombatSystem.ProcessAttack`은 attacker 존재·rate-limit·rewind만 보고 **현재 행동 State는 안 본다**([`CombatSystem.cs:37-58`](../../../02_Server/GameServer/Maps/Systems/CombatSystem.cs)). `SkillSystem`도 스킬별 쿨다운(`GetLastSkillTick`)만. 결과 → **Dash commit window 중 평타 진입이 허용**된다.
3. **클래스 게이트는 별도 경로로 이미 봉합됨(M4.9 P02)** — `SkillCatalog` 단일 진실 + 서버 3단계 silent drop + 클라 거울. 다만 게이트가 *클래스 자격*·*쿨다운*에만 있고 **"현재 상태가 이 행동을 받는가"는 어디에도 없다** — 이번에 메울 빈칸.

**실사고 선례** — Dash 중 평타 → `Attack→Attack` self-transition no-op(Exit 미실행) → `LungeDecayPerTick 0.85`(Dash 전용값) 잔류 버그. 봉합은 *호출자가 평타 진입마다 기본값 명시 세팅*([`CombatSystem.cs:75`](../../../02_Server/GameServer/Maps/Systems/CombatSystem.cs) · `PlayerEntity.cs:235` · `PlayerCombatStates.cs:51`). **호출자가 상태 내부 파라미터를 직접 찔러 넣는 구조 자체가 원인** — 상태가 자기 데이터를 소유하지 못해서다.

### 설계 방향 (착수 시 재실측 후 확정 — 골격)

- **상태가 입장 정책 선언** — `LocksMovement`와 동형으로 `AcceptsAction(액션종류)`: "이 상태에서 이 행동 허용?"의 답을 **상태가 소유**(`ActorState`에 추가).
- **서버 행동 요청 단일 입구**(`TryPerformAction` 류) — ①현재 상태 허용 ②쿨다운 ③클래스 게이트를 **한 곳에서** 검사. 새 기술 = 허용 표 한 줄.
- **클라는 같은 게이트 표를 거울** — `98_Shared` 공유(`SkillCatalog` 패턴을 행동 허용까지 확장).
- **기존 클래스 게이트(M4.9 P02)는 단일 입구에 흡수** — 중복 검증 제거.
- **상태 소유 데이터** — `LungeDecayPerTick`류를 상태가 Enter/Exit에서 소유·정리(호출자 직접 세팅 제거 = `0.85` 사고의 구조적 봉합). 이것이 Phase 2 임펄스 모델 통일의 전제.

### 제약 (영호 확정 — 클린코드 v6.1 준수 의무)

- **SRP**(§2.2) — 상태(정책 선언)/게이트(검사)/실행(mutation) 책임 분리. System 간 직접 호출 X.
- **상태 소유 데이터** — 호출자가 상태 내부 파라미터 직접 세팅 금지.
- **매직넘버 금지**(§2.5), **의도 드러나는 naming**(§6.1), **public 1줄 책임 헤더**(§6.5).
- **헌법 §1**(서버 권위 — 입구 검사 서버 권위, 클라 거울은 UX) · **§3**(클래스/소유권/rate-limit 서버 단독 검증 그대로).

---

## 확정 설계 — 대쉬 게임플레이 (영호 6결정, 첫 적용 사례)

| 항목 | 결정 | 현재 대비 |
|------|------|----------|
| ① 정체성 | 하이브리드 (이동+공격) | 유사 |
| ② 거리·속도 | **고정거리 + 빠른 등속** (메이플 러시) | 🔄 현재 모멘텀 감속(vx 10 × 0.85/틱) |
| ②b 적 밀침 | **O — 경로 적 앞으로 밀기(허딩)** | 🆕 현재 데미지만, 몹 제자리 |
| ③ 무적 | **완전 무적 (피격 데미지 0)** | 🔄 현재 넉백만 무시(InterruptibleByHit=false), 데미지 적용 여부 착수 시 확인 |
| ④ 취소 | 불가 (끝까지 커밋) | 동일 |
| ⑤ 방향 | 전방만 (바라보는 쪽) | 동일 |

## 확정 설계 — 네트워크 (방식 B = 클라 예측)

핵심: **대쉬 전용이 아니라 "예측되는 외부 임펄스" 단일 기계**를 만들어 대쉬·넉백·임펄스공격에 공통 적용.

- **임펄스 예측 기계**: `PlayerPredictor`에 ExternalVelX 채널 + **InputHistory에 임펄스 상태 저장**(reconcile replay 시 정확 재현 — 핵심 비용). 시작점만 다름: 대쉬=시전 시 / 넉백=피격 수신 시 / 임펄스공격=공격 시.
- **forceAdopt 크러치 제거**: 예측이 맞으니 매 스냅샷 당김 불필요 → 기존 substep 보간 복원(스터터 소멸). 진짜 mispredict(벽 충돌 등 드묾)에만 reconcile 스냅.
- **보간**: B 덕에 자동 — 기존 `LocalPlayerMovement` substep prev/curr lerp 그대로.
- **적 밀침**: 서버 권위. 클라는 서버가 준 적 위치를 기존 원격 보간으로 미러(내 캐릭터만 예측).

---

## Phase 분해 (예정 — 착수 시 /work:plan으로 개별 .md + plan-auditor)

| # | Phase (예정) | 위험 | 도메인 | 핵심 |
|---|---|---|---|---|
| 1 | **행동 입력 게이트** (`AcceptsAction` 단일 입구) | **trust-boundary** | server+shared+client | 위 "Phase 1 토대" 섹션. `ActorState.AcceptsAction` 정책(상태 소유) + 서버 `TryPerformAction` 단일 입구(상태 허용+쿨다운+클래스 한 곳) + 클라 거울 + `LungeDecayPerTick` 직접 세팅 봉합(상태 Enter/Exit 소유). **임펄스 재설계의 서버 토대.** |
| 2 | **서버 임펄스 모델 통일** | trust-boundary | server + shared | dash/knockback/lunge ExternalVelX 모델 일관화(P1 데이터 소유 위). 대쉬: 모멘텀 감속 → 고정거리 등속. `LungeDecayPerTick` 호출자 직접 세팅 잔재 제거. |
| 3 | **대쉬 거동 재설계 (서버 게임플레이)** | trust-boundary | server | 적 밀침(허딩 — 서버 적 변위 신규) + 완전 무적(대쉬 중 데미지 게이트 0). ⚠️*착수 직전 적 변위 경로 30분 스파이크 선결*(plan-auditor 봉합 ②). |
| 4 | **공유 모델 추출** | — | shared | 대쉬/임펄스 이동 공식 → `98_Shared`(헌법 §4). 현재 `DashLungeInitialVx/Decay`는 서버 전용 → 공유로. wire 무변경 점검(§2). |
| 5 | **클라 예측 통일 (방식 B)** | client prediction | client + qa | `PlayerPredictor` ExternalVelX + InputHistory 임펄스 저장 + forceAdopt 재작업 + 보간 복원. `PlayerPredictorTests` 확장. ⚠️*"InputHistory 임펄스 저장"=예측 결정성 계약 확장 → 5a/5b 분해 검토*(plan-auditor 봉합 ①). |
| 6 | **클래스 통합 + 전체 회귀** | — | client + qa | 넉백/임펄스공격에 같은 기계 적용 + 봇/EditMode/2클라 Play 회귀. |

> Phase 수·순서·완료조건은 착수 시 /work:plan + plan-auditor가 확정. 단방향 의존: P1(게이트)→P2(모델)→P4(공유추출)→P5(클라예측). P3(거동)는 P2 위, P6은 마지막. 착수 순서·타이밍은 영호 컨트롤.

---

## 핵심 구현 앵커 (조사 완료 — file:line)

**게이트(P1)**: `ActorState.cs:18`(LocksMovement)/`:22`(InterruptibleByHit) · `PlayerCombatStates.cs:28`(AttackState.LocksMovement) · `GameMap.cs:233`(movement-gate) · `CombatSystem.cs:37-58`(ProcessAttack 상태 미검사)/`:75`(LungeDecayPerTick 직접 세팅) · `PlayerEntity.cs:125`(LungeDecayPerTick 프로퍼티 소유)/`:170`(GetLastSkillTick)/`:235`(리셋) · `SkillCatalog.cs`(98_Shared 클래스 게이트) · `SkillUseHandler.cs`(3단계 검증) · `LocalPlayerInput.cs:133-178`(클라 거울).
**임펄스(P2~)**: `SkillSystem.cs:74-140`(ProcessDash) · `CombatConstants.cs`(DashLungeInitialVx/Decay, `02_Server/GameServer/Combat/`, 서버 전용) · `PlayerCombatStates.cs:38-39`(AttackState lunge 감쇠)/`64-82`(HitState 넉백) · `GameMap.cs:244`(임펄스 합성) · `PlayerEntity.cs:218-225`(EnterHitState).
**공유**: `Physics.cs:148`(`vx=InputX*MoveSpeed+ExternalVelX` — 채널 존재) · `PhysicsInput`(4-arg ctor) · `Constants.cs`(Knockback 상수 이미 Shared).
**클라**: `PlayerPredictor.cs:91-96`(Predict)/`108-139`(OnSnapshot) · `LocalPlayerMovement.cs:234-307`(Update substep)/`335-380`(forceAdopt+리셋)/`224-232`(ShouldForceAdopt) · `SkillCastHandler.cs:94-112`(HandleDash).
**테스트**: `PlayerPredictorTests.cs`(~55케이스, Predict 시그니처 영향) · `MovementGateTests.cs`.
**미조사**: 적 밀침(허딩) = 서버 적 변위 신규 — 착수 시 `EnemyEntity`(`02_Server/GameServer/Combat/EnemyEntity.cs`)/CombatSystem 경로 확인(P3 스파이크).

---

## 위험 / 헌법 게이트

- **§1 서버 권위**: 클라 예측은 *박자*만 앞당김 — reconcile 서버 진실 우위 불변. 게이트 검사·적 밀침·데미지·무적 판정 = 서버 단독.
- **§2 Protocol**: 게이트/임펄스/대쉬 모델 변경이 wire 건드리면 ProtocolVersion bump → STOP → 영호 의논. 현재 v12.
- **§3 신뢰 경계**: 클래스/쿨다운/소유권/상태허용 검증 서버 단독. **§4**: 임펄스 모델 공식 = `98_Shared` 단일 출처(클라/서버 동일).
- **클린코드(CODE_CONVENTION v6.1)**: 상태가 행동·임펄스 데이터 소유(호출자 직접 세팅 금지 — `0.85` 잔류 사고의 뿌리), 매직넘버 금지, SRP.

## 검증 방법

- **게이트(P1)**: 서버 EditMode(상태별 AcceptsAction 표 + Dash window 중 평타 거부) + 봇 회귀.
- **서버 임펄스**: 봇 DashSmoke 확장(고정거리·적 밀침·무적) + 서버 EditMode/통합 테스트.
- **클라 예측**: `PlayerPredictorTests` 확장(ExternalVelX 예측·replay) + WSL2 봇 회귀.
- **시각**: 2클라 Play + Unity Console `[Reconcile]` 분석(대쉬 중 forceAdopt 빈도 급감 = 예측 성공) + 영호 손맛 실측.

---

## plan-auditor 검증 (2026-06-12) — 🟡 조건부 GO (게이트 합류 전 골격 기준 유효)

골격 견고 + 헌법 정합(§1 유지·§2 wire 위험 낮음·§4 위반을 P4가 봉합). 비가역 위험 없음 → 즉시 봉합 강제 아님. **착수(/work:plan) 시 봉합 2건** (게이트 합류로 Phase 번호 갱신):

1. **[Phase 5 재분해] "InputHistory 임펄스 저장"은 단순 필드 추가가 아니라 *예측 결정성 계약 확장*.** 실측: 현 `InputRecord`=`(clientTick,inputX,jumpPressed)` 3필드뿐, `OnSnapshot` replay 루프는 `PhysicsInput`에 ExternalVelX를 **0**으로 넣어 평지 물리만 재현. 방식 B = ① `InputRecord`에 임펄스 채널 추가 ② `Push`/`NotifySent`/`ReplayFrom` 시그니처 변경 ③ replay가 매 틱 임펄스를 *시간 전진(감속/소진)*시키며 재현. 착수 시 Phase 5를 5a/5b로 쪼갤지 검토(테스트 ~55케이스 시그니처 영향 포함).
2. **[Phase 3 선결 스파이크] 적 밀침(허딩) 미조사 → 완료 조건 정량 불가.** 착수 직전 적 변위 경로 스파이크(≈30분): `EnemyEntity`가 ExternalVelX 채널 갖는지 / 충돌 해석 주체 / 밀림 기댓값(칸수·벽 끼임·S_EntityState 브로드캐스트 영향). "신규 충돌 시스템 필요"로 나오면 Phase 3 분리 후보.

**★핵심 리스크(§1) — forceAdopt 크러치 제거 = 안전망 제거.** 현 forceAdopt는 "예측 못 하는 임펄스를 매 스냅샷 서버 위치로 끌어와 *sub-threshold 누적 어긋남* 방지"하는 보험. 방식 B 등식("클라가 예측하니 보험 불필요")은 **클라 replay가 서버 임펄스 궤적을 비트단위 재현**해야 성립 — 감속 공식/시작 틱이 1틱이라도 어긋나면 보험 뗀 자리에 *영구 offset 누적* 재발(M4.11 P2 ε 공유상수 silent break와 동류 함정). **Phase 5 결함1의 결정적 재현 = 이 마일스톤 성패의 핵심 계약.**

---

> **본 문서는 마일스톤 계획서** (설계 박제). Phase 개별 정의 `.md`는 **M4.13 착수 시점에 /work:plan으로 분해**(plan-auditor 동반). **M4.12와 독립** — 순서는 영호 컨트롤.
