---
owner: youngho
milestone: M4.13
phase: 05-client-prediction-b
title: 클라 예측 통일 (방식 B 하이브리드) — 임펄스 예측 + forceAdopt 크러치 제거 + 보간 복원
status: in-progress
grade: 대규모
slug: 05-client-prediction-b
created: 2026-06-13
prior_phases: [01-action-input-gate, 02-server-impulse-model, 03-dash-behavior, 04-shared-extract]
depends_on: [04-shared-extract]
risk_flags: [client-prediction]
---

# M4.13 Phase 05 — 클라 예측 통일 (방식 B 하이브리드)

> 계획서 = `_milestone-plan.md` "확정 설계 — 네트워크 (방식 B)" + Phase 분해 표 #5. **이 마일스톤 성패의 핵심 계약**(plan-auditor ★핵심 리스크). P4 공유 공식 위에서 클라가 임펄스 궤적을 *직접 예측*해 forceAdopt 크러치를 제거하고 스터터를 없앤다.
> **확정 설계 = 2026-06-13 영호 GO** (아래 §확정 설계). 5a/5b 분해 + 하이브리드 방식 + 결정성 임계 확정.

---

## Context (왜 — 핵심 리스크)

대쉬·넉백·임펄스공격은 **클라가 예측 못 하는 서버 임펄스**라, 매 스냅샷 `forceAdopt`로 끌려오고 M4.11 P4 reconcile 보간 버퍼 리셋이 그걸 매번 지워 **50ms 간격 스냅(시각 스터터)**이 된다(증거: `[Reconcile]` 로그 대쉬 forceAdopt 499/500).

방식 B = 클라가 **임펄스를 예측**하면 forceAdopt 불필요 → 보간 복원 → 스터터 소멸. **단 핵심 리스크: forceAdopt 크러치 제거 = 안전망 제거.** 클라 replay가 서버 임펄스 궤적을 **비트단위 재현** 못하면(감속 공식/시작 틱 1틱 어긋남) 보험 뗀 자리에 **영구 offset 누적**(M4.11 P2 ε silent break 동류). **이 결정성이 마일스톤 성패의 핵심 계약** — P4 공유 공식이 그 안전망.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, main `16cd8b3` + P1-client 반영)

> ⚠️ plan 시점(main 2433ab5) 앵커는 P1-client(LocalPlayerMovement 행동잠금 게이트)로 시프트. 아래는 P5 착수 직전 재실측(메인 실측 게이트).

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. InputRecord 3필드 | `Prediction/InputHistory.cs:64-76`(`ClientTick uint, InputX sbyte, JumpPressed bool`) · Push `:21-24` · ReplayFrom `:46-55` | **임펄스 채널 없음**(평지 물리만 재현) → **`ExternalVelX` 추가 대상**. |
| 1b. replay가 임펄스 0 (발산 지점) | `Prediction/PlayerPredictor.cs:108-139`(OnSnapshot) → **`:127-132`**(`new PhysicsInput(rec.InputX, rec.JumpPressed, TickDuration)` — ExternalVelX 없는 3-arg) | replay가 평지만 재현 → 임펄스 구간 발산. **4-arg로 교체 대상.** |
| 2. Predict / 임펄스 진입 | `PlayerPredictor.cs:91-96`(Predict, 3-arg) · `:84-87`(NotifySent) · `:143-153`(To/ApplyPhysicsState) | live 예측에 임펄스 상태 전진 추가 + `StartImpulse` 신설. |
| 3. forceAdopt 크러치 (5b 제거) | `Prediction/LocalPlayerMovement.cs:247-255`(`ShouldForceAdopt`: teleportSnap·Hit·Attack&\|vx\|≥ε) → `:383-385`(OnSnapshot 호출) → `:396-397`(버퍼 리셋) | **Attack&vx≥ε 분기 제거**(클라가 대쉬/lunge 예측). teleportSnap만 잔류. |
| 3b. substep 보간 | `LocalPlayerMovement.cs:257-330`(Update fixed-step) + `:327-329`(prev/curr lerp) | forceAdopt 제거 시 자동 복원. |
| 3c. 임펄스 시작 트리거 | `LocalPlayerMovement.cs:199-202`(`NotifyDash`, 현재 쿨다운만) · `:179-184`(`NotifyAttack`) · `:215-218`(`NotifyHit`) | 5a: 여기서 `_predictor.StartImpulse(...)` 호출. |
| 3d. forceAdopt 빈도 측정 | `LocalPlayerMovement.cs:390-392`(`[Reconcile]` 로그) + `PlayerPredictor.SnapCount`(:35) | 2클라 Play 임계 실측 지점. |
| 4. 대쉬 핸들 | `Network/Handlers/Skill/SkillCastHandler.cs:94-112`(HandleDash) · `:43`(S_SkillCast facing) | 대쉬 연출 — 현재 "이동=force-adopt" 주석(:88-89)이 5b로 무효화. |
| 5. 테스트 | `Assets/Tests/EditMode/Prediction/PlayerPredictorTests.cs`(30) · `MovementGateTests.cs`(21) | Predict/replay 시그니처 확장 영향 + 결정성 테스트 추가. |

---

## ✅ 확정 설계 (2026-06-13 영호 GO)

### 방식 — 하이브리드
- **live 예측**: `PlayerPredictor`가 임펄스 상태(`_impulseVx`, `_impulseDecay`, 남은 틱)를 들고, 매 서브스텝 **P4 `Physics.DecayImpulse`로 한 틱 전진** + 그 vx를 `Physics.Step`의 ExternalVelX로 주입 = **서버 `AttackState/HitState.Tick`의 클라 거울**. (서버는 등속 대쉬 decay=1.0, durationTicks 후 0 정리 — 클라 동일.)
- **replay**: `InputRecord`에 그 틱의 `ExternalVelX`를 실어둠 → 되감기 재생은 저장값 그대로(4-arg `PhysicsInput`) = 단순·결정적.
- **결정성 보장의 뿌리**: "클라 live도 서버와 같은 P4 공식". replay는 그 산물 재생. ①(값 저장)/②(재계산) 중 *live=공식 계산(②, 강제), replay=저장값 재생(① 단순)* 하이브리드 — replay에서 임펄스 위상 재추적(off-by-one 자리) 회피.

### 5a — 안전망 깔기 (임펄스 예측 결정성 계약, ★위험 핵심)
- `InputRecord`에 **`ExternalVelX` 채널 추가** (Push / 생성자 4-arg / `NotifySent`에 동봉).
- `PlayerPredictor.StartImpulse(startVx, decayPerTick, durationTicks)` 신설 + `Predict`가 매 틱 임펄스 전진(`Physics.DecayImpulse`) 후 ExternalVelX 주입.
- replay 루프(`:127-132`) → **4-arg `PhysicsInput(rec.InputX, rec.JumpPressed, dt, rec.ExternalVelX)`**.
- 임펄스 시작 트리거(클라가 방향 앎) — **vx·decay·durationTicks 셋 다 `Constants.*` 심볼명으로 박음**(매직넘버 표류 금지, 헌법 §4):
  - `NotifyDash` → `StartImpulse(Constants.DashSpeed × facing, 1.0f, Constants.DashTravelTicks)`
  - `NotifyAttack`(non-Mage) → lunge `StartImpulse(Constants.AttackLungeInitialVx × facing, Constants.KnockbackDecayPerTick, Constants.AttackCommitWindowTicks)` (서버 `EnterAttackState` 평타 default durationTicks = `AttackCommitWindowTicks` — `PlayerEntity.cs:245-246` 실측 확인)
- **★임펄스 시작 틱 정렬 (plan-auditor 🟡 봉합 — 결정성 테스트가 *못 잡는* 결함)**: 서버는 대쉬 입력을 *소비한 틱*(`DashAction.Execute` 처리 틱, 같은 틱 `GameMap.Tick:244`가 vx 합성)에 임펄스 적용. 클라 `NotifyDash`는 *송신 성공 시점* 호출(`:196` 주석) — 송신 틱 ≠ 서버 소비 틱이면 RTT만큼 어긋나 영구 offset. **착수 시 "클라 예측 임펄스 시작 틱 = 서버 입력 소비 틱과 정렬"을 한 줄로 못박고**(C_SkillUse 송신 clientTick 기준 정렬 vs S_SkillCast serverTick 기준 — Worker가 서버 소비 경로 실측 후 택1), 그 정렬을 테스트로 고정.
- `PlayerPredictorTests` 결정성 테스트 추가: ① 임펄스 구간 예측 위치 == 시뮬 서버 위치 **오차 0**(비트 정확 — float이라도 곱셈·덧셈·비교만이라 IEEE 정확, 비트 다르면 로직 버그). ② **시작 틱 1틱 오프셋 주입 케이스** — 정렬 실패가 단위 테스트로 잡히는지(2차 그물 부담 ↓, plan-auditor 권장).
- **★저장값 = live 적용값 (재계산 금지)**: `NotifySent`에 동봉하는 ExternalVelX = *그 서브스텝 `Predict`가 실제 쓴 바로 그 vx*. replay가 별도 재계산하면 하이브리드 함정 부활 → 메인이 `file:line`로 "저장 = live 적용값" 확인(Worker 구현 게이트).

### 5b — forceAdopt 제거 + 보간 복원 (5a green 후)
- `ShouldForceAdopt`(:247-255)에서 **`Attack && |serverVx| ≥ ε` 분기 제거**(이제 클라가 대쉬/lunge 예측) → `teleportSnap`만 잔류.
- 진짜 mispredict(`SnapThreshold` :30, 벽 충돌 등 드묾)는 `OnSnapshot`에 이미 있음 — 유지.
- substep 보간 자동 복원(스터터 소멸).

### 스코프 (1차 = 대쉬 + lunge)
- **대쉬·평타 lunge**는 클라가 방향(자기 facing)을 알아 예측 가능 + **측정 게이트가 대쉬**(499/500) → 5a 1차 범위.
- **넉백(Hit)은 방향이 서버 권위**(`EnterHitState(FacingDir)`) — plan-auditor 실측: 방향은 **스냅샷 serverVx 부호로 이미 클라에 도달**(별도 패킷 필드 불요) → 예측이 생각보다 쉬울 수 있음. 그래도 측정 게이트가 대쉬라 **1차 범위 = 대쉬+lunge 유지**, 넉백은 5a green 후 여유 시 포함(force-adopt 잔류해도 대쉬 스터터=측정 통증은 해결). 과스코프 회피 YAGNI.

### 워크플로
- **한 브랜치(`feature/m4.13-shared-extract` 계속) / commit 5a·5b 분리 / PR 끝에 한 번**.
- **5a 결정성 테스트 green = 5b 착수 하드 게이트**(정의서 "5a→5b 강제"를 코드 게이트로 — 크러치 먼저 떼면 발산).
- 대규모 → **Opus 구현 Worker**(선택적 Opus B). 메인 file:line 실측 게이트 모델 무관 유지.

---

## 완료 조건 / 게이트 (정량)

- [ ] **(5a)** `InputRecord` `ExternalVelX` 채널 + replay 4-arg + `PlayerPredictor.StartImpulse` + live 임펄스 전진(P4 `Physics.DecayImpulse`). 저장값 = live 적용값(재계산 X — 메인 file:line 확인).
- [ ] **(5a)** replay 결정성 테스트 green — 임펄스 구간 클라 예측 위치 == 시뮬 서버 위치, **오차 0**(비트 정확). offset 누적 0.
- [ ] **(5a)** ★임펄스 시작 틱 = 서버 입력 소비 틱과 정렬(한 줄 명문화 + 테스트 고정) + 시작 틱 1틱 오프셋 주입 케이스로 정렬 실패 검출.
- [ ] **(5b)** 대쉬 중 forceAdopt 빈도 **대쉬당 ≤ 2회** — 2클라 Play `[Reconcile]` 로그(499/500 → ~0, 진짜 mispredict만). *일단 적용 후 실측 튜닝*.
- [ ] **(5b)** 보간 복원 — 대쉬 스터터(20Hz 뚝뚝) 소멸, 영호 손맛 실측.
- [ ] `PlayerPredictorTests`(30) + `MovementGateTests`(21) 확장·green.
- [ ] 회귀 green: WSL2 봇 + EditMode + Unity error 0 + reviewer Tier 2-A.

---

## 위험 / 헌법 게이트

- **★핵심 계약**: replay 비트단위 재현 실패 = 영구 offset 누적. P4 공유 공식 단일 출처가 안전망. 결정성 테스트(오차 0)가 1차 그물, 2클라 실측이 2차.
- **★임펄스 시작 틱 정렬**: 클라 `NotifyDash`(송신 시점) 시작 vs 서버 dash 시작 serverTick — 1틱 어긋나면 영구 offset. 결정성 테스트 + 2클라 Play가 그물.
- **§1 서버 권위**: 클라 예측은 *박자*만 앞당김 — reconcile 서버 진실 우위 불변. 진짜 mispredict 스냅 유지.
- **§2 Protocol**: 클라 내부 예측 = wire 무변경 v12(`InputRecord`는 클라 로컬 구조, 패킷 아님 — 확인 완료).
- **§4**: replay 공식 = `98_Shared` 단일 출처(P4) — 클라가 별도로 박으면 silent drift.
- **⚠️ 5a→5b 순서 강제** — 크러치 먼저 떼면 발산. 5a 결정성 green이 5b 하드 게이트.

---

> Phase 완료 시 `05-...-DONE.md` 박제(대규모 등급 — 5단계 보고 MD/HTML). 게이트 통과 후 Phase 06(클래스 통합 + 전체 회귀).
