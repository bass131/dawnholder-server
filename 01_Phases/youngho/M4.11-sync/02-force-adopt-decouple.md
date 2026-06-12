---
owner: youngho
milestone: M4.11
phase: 02-force-adopt-decouple
title: force-adopt 덤불 정리 — 서버 임펄스 클램프 ↔ 클라 게이트 결합 끊기 (공유 상수 명시화)
status: done
grade: 복잡
slug: 02-force-adopt-decouple
created: 2026-06-11
domains: [shared, server, client]
prior_phases: [01-remote-interp-servertick]
depends_on: [01-remote-interp-servertick]
---

# M4.11 Phase 02 — force-adopt 덤불 정리 (서버 임펄스 클램프 ↔ 클라 게이트 결합 끊기)

> 마일스톤 계획서 = `_milestone-plan.md` (도미노 1의 *매직넘버 결합* 부분 = 이 Phase). 도미노 1의 뿌리(가변 dt → SnapThreshold 1.5f dead-zone)는 P4 고위험 영역 — 본 Phase는 **건드리지 않는다.**
> 이 Phase는 force-adopt 덤불 중 **가장 국소적이고 가역적인 한 매듭** — 서버 임펄스 클램프 임계와 클라 force-adopt 게이트가 *공유 상수 없이 암묵 결합*된 silent-break 위험만 끊는다. 동작은 불변(게이트 = 동작 보존).

---

## Context (왜)

원격 보간(P1)을 봉합하니, 다음으로 드러난 부채는 **로컬 예측 측 force-adopt 덤불**이다. 그중 가장 명확한 결함은 **서버와 클라가 같은 약속(서버가 작은 임펄스를 0으로 정리한다)에 *각자 다른 매직넘버*로 의존**한다는 것이다 — 한쪽 숫자를 바꾸면 다른 쪽이 조용히 깨진다(silent break).

결합 메커니즘을 도미노로 풀면 이렇다:

- **(서버) 임펄스를 0으로 클램프.** lunge(돌진)/넉백 임펄스를 서버가 매 틱 지수 감쇠시키다가, `|velocity| < VelocityEpsilon(0.05f)` 미만이 되면 **정확히 0f로 클램프**한다. 즉 "살아남은 임펄스 vx는 항상 `>= 0.05f`"라는 약속이 서버 측에 성다.
- **(전달) 클램프된 vx가 스냅샷의 vx로 실린다.** `KnockbackVx + AttackLungeVx` → `Physics`의 `ExternalVelX` → `Player.Velocity.X` → `S_Snapshot.vx`. 클라는 이 vx를 그대로 본다.
- **(클라) vx로 "임펄스 진행 중"을 추측해 force-adopt 게이트.** `ShouldForceAdopt`가 Attack 상태에서 `Mathf.Abs(serverVx) > 0.0001f`(리터럴)이면 "Dash/lunge처럼 서버가 전방 임펄스를 줬다"고 보고 서버 위치를 즉시 채택한다. 평타(`serverVx≈0`)는 채택 안 함 — rubber-band 밀림 봉합.

**결합의 핵심**: 클라의 `0.0001f` 게이트는 "서버가 `0.05f` 미만을 0으로 잘라준다"는 약속에 *암묵적으로* 기댄다. 둘을 잇는 **공유 상수가 없다.** 따라서:

- 서버 임계(`0.05f`)를 낮추면 → 0과 게이트(`0.0001f`) 사이에 작은 잔류 vx가 살아남아 → 평타에서도 force-adopt 발동 → **rubber-band 재발.**
- 클라 게이트(`0.0001f`)를 서버 임계 위로 올리면 → lunge 초반/말미의 작은 vx 구간에서 force-adopt를 못 켜 → **lunge 중 위치 발산.**

양방향 silent break. 같은 약속을 두 곳에서 따로 박은 게 화근이다. **해법 = 시계 일치(P1)와 같은 정신 — 두 곳이 *하나의 공유 상수*를 보게 한다.**

---

## 증거 사슬 (현재 코드 실측 — 2026-06-11, HEAD 8a04ac3, client Worker 확정)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 서버 임펄스 클램프 임계 | `02_Server/GameServer/Combat/CombatConstants.cs:174` | `public const float VelocityEpsilon = 0.05f;` — **서버 전용**. lunge/넉백 감쇠 near-zero 종료 임계 |
| 1b. AttackState lunge 클램프 | `02_Server/GameServer/Maps/States/PlayerCombatStates.cs:40-41` | AttackLungeVx 감쇠 후 `< VelocityEpsilon`이면 0f 클램프 |
| 1c. HitState 넉백 클램프 | `02_Server/GameServer/Maps/States/PlayerCombatStates.cs:75-77` | KnockbackVx 감쇠 후 `< VelocityEpsilon`이면 0f 클램프 |
| 2. 임펄스 → vx 합산 | `02_Server/.../Maps/GameMap.cs:244` | `PhysicsInput(..., ExternalVelX: p.KnockbackVx + p.AttackLungeVx)` |
| 2b. vx 계산 | `98_Shared/GameData/Physics.cs:148,204` | `vx = inputX * MoveSpeed + ExternalVelX` |
| 2c. 스냅샷에 vx 적재 | `02_Server/.../Maps/GameMap.cs:289-300` | `S_Snapshot.vx = p.Velocity.X` |
| 3. 클라 force-adopt 게이트 | `03_Client/Assets/Scripts/Prediction/LocalPlayerMovement.cs:226` | Attack 상태에서 `return Mathf.Abs(serverVx) > 0.0001f;` — **리터럴**, 공유 상수 미참조 |
| 4. 공유 상수 현황 | `98_Shared/GameData/Constants.cs:79,85` | `KnockbackInitialVx(7f)`/`KnockbackDecayPerTick(0.75f)`는 *이미 공유* — `VelocityEpsilon`만 서버 전용에 남음 |

> 참고 임펄스 상수(서버 전용, 변경 대상 아님): `AttackLungeInitialVx=3.0f`(`CombatConstants.cs:27`), `DashLungeInitialVx=10.0f`(`:100`), `DashLungeDecayPerTick=0.85f`(`:106`).

---

## 결정 (영호 확정, 2026-06-11) — 옵션 A: 공유 상수 명시화

세 옵션을 놓고 **A 선택**. wire 무변경, ProtocolVersion **v12 유지**, 동작 불변.

| 옵션 | 내용 | 판정 | 사유 |
|---|---|---|---|
| **A** | 공유 상수 `Constants.ExternalImpulseEpsilon = 0.05f` 신설 → 서버 클램프 + 클라 게이트 둘 다 참조 | **채택** | wire 무변경(v12 유지) · 값 동일(0.05f)이라 동작 불변 · 결합을 *명시 계약*으로 승격 |
| B | `S_Snapshot`/`S_PlayerAttack`에 v13 명시 임펄스 플래그 추가 | 기각 | 가장 명시적이나 **breaking change** + P4 고정스텝에서 force-adopt 재설계 가능성 → 프로토콜 churn 2번 위험 |
| C | 본인 공격 `S_PlayerAttack` latch를 reconcile 게이트 출처로 사용 | 기각 | 본인 공격 `S_PlayerAttack`은 `except: attacker.Owner`로 **본인에게 미전송** → 로컬 게이트 출처로 사용 불가 |

> 계획서 `_milestone-plan.md` P2 행의 "명시 플래그로 도입 검토"는 옵션 B를 가리킨다 — 본 Phase는 P4 churn 위험을 근거로 **A로 정련**한다(영호 결정).

---

## 변경 대상 (파일별)

1. **`98_Shared/GameData/Constants.cs`** — 공유 상수 신설.
   - `public const float ExternalImpulseEpsilon = 0.05f;` 추가.
   - 의미 주석(계약 명시): "서버가 이 값 미만 임펄스를 0으로 클램프 — 클라 force-adopt 게이트와의 계약. 클라는 `|vx| >= ε`로 임펄스 활성 판정(서버 클램프의 보색)."

2. **`02_Server/GameServer/Combat/CombatConstants.cs` + 사용처** — 서버를 공유 상수로 교체.
   - `VelocityEpsilon` 사용처 전수 grep(`PlayerCombatStates`/`EnemyHitState` 등) → `Constants.ExternalImpulseEpsilon` 참조로 치환. 값 동일(0.05f) → 동작 불변.
   - `VelocityEpsilon` 정의는 **삭제** 권장: plan-auditor 실측상 사용처 3곳(`PlayerCombatStates.cs:40,76` + `EnemyStates.cs:174`)이 전부 동일 의미(임펄스 감쇠 near-zero 클램프) — Worker가 grep으로 재확인 후 삭제. 다른 의미 사용처가 새로 발견되면 분리 유지.

3. **`03_Client/Assets/Scripts/Prediction/LocalPlayerMovement.cs:226`** — 클라 게이트를 공유 상수로 교체.
   - `Mathf.Abs(serverVx) > 0.0001f` → `Mathf.Abs(serverVx) >= Constants.ExternalImpulseEpsilon`.
   - **부등호 방향 근거**: 서버 클램프는 `< ε → 0`. 따라서 살아남은 vx는 항상 `>= ε`. 클라 게이트가 `>= ε`이면 서버 클램프의 *정확한 보색*(complement) — 0 처리된 건 아래로, 살아남은 임펄스는 위로 떨어진다.
   - 계약 주석 1줄(서버 클램프 보색임을 명시).

4. **Shared.dll / ClientNet.dll 재빌드 + `03_Client/Assets/Plugins/` 갱신.**
   - shared 소스 *실변경*이라 dll 변경이 정당(drift 아님 — checkout 금지).

---

## 테스트

- **클라 EditMode `ShouldForceAdoptTests`** — ε 경계값 케이스 추가:
  - `serverVx = 0.049f` → `false` (서버가 0으로 정리했어야 할 구간, 게이트 미발동).
  - `serverVx = 0.05f`(= ε) → `true` (살아남은 최소 임펄스, 게이트 발동).
  - `serverVx = 0f` → `false`.
  - (기존 teleportSnap/Hit/평타 가드 케이스는 그대로 green 유지.)
- **서버 측** — lunge/넉백 감쇠 → 0 수렴은 기존 `HitKnockbackTests`가 커버. 본 Phase는 *임계 상수 치환*이라 값 불변 → 기존 테스트 그대로 통과해야 함.
- **범위 밖 명시**: lunge가 `S_Snapshot.vx`에 실리는 *end-to-end* 검증(서버 임펄스 → 스냅샷 vx → 클라 게이트 발동)은 본 Phase 범위 밖 — P3 안전망 후보로 넘긴다.

---

## Worker 검증 항목 (구현 중 확인 — 어긋나면 STOP, 메인 재논의)

- **상태 전이 시 넉백 잔류 합산 가능성**: 서버 AttackState 진입 시 KnockbackVx가 잔류해 AttackLungeVx와 합산·상쇄로 `0 < |vx| < ε`인 Attack 스냅샷이 나올 수 있는가? 가능하면 옛 게이트(`0.0001f`)와 새 게이트(`ε`) 사이 거동 차이가 발생한다 — 상태 전이에서 넉백이 정리되는지 *코드로 확정*. (보통은 AttackState 진입이 KnockbackVx를 0으로 정리할 것으로 예상하나 실측 필요.)
- **Attack 중 이동 성분 혼입 여부**: Attack 상태 동안 서버 `inputX` 잠금(commit window)으로 vx에 이동 성분이 섞이지 않는지 — `CommitWindowTests` 전제 재확인. 이동 성분이 섞이면 vx가 ε 위로 떠 게이트 오발동 가능.

---

## 완료 조건 / 게이트 (정량)

- [ ] **`LocalPlayerMovement.cs`의 force-adopt 게이트**에서 `0.0001f` 리터럴 제거 (해당 파일 게이트 줄 grep 0건). ※전역 grep 아님 — `0.0001f`는 `Physics.cs:98`(ground 임계)/`PlayerPredictor.cs:66,68`(금지구역)/테스트 tolerance 등 *다른 의미*로 다수 존재, 건드리지 않음 (plan-auditor D1).
- [ ] 클라/서버가 **같은 `Constants.ExternalImpulseEpsilon`** 참조 (grep으로 양쪽 사용처 제시).
- [ ] 기존 테스트 전부 green — WSL2 556+ passed + 클라 EditMode 기존 가드 테스트 통과.
- [ ] 신규 ε 경계 테스트 green.
- [ ] Unity 컴파일 clean (error 0).
- [ ] **영호 2클라 실측**: dash / 평타 lunge / 피격 넉백 거동이 **P1 마감 시점과 동일** (게이트 = 동작 불변 증명).

---

## 위험 / 헌법 약속 — 금지 항목

- **§2 프로토콜 wire 무변경**: PDL.xml / `ProtocolVersion` 손대지 않음 — **v12 유지**. 옵션 A는 공유 *상수*만 추가(직렬화 형상 무변경).
- **§4 공유 코드 규율**: `98_Shared/Constants.cs` 변경 → 양쪽(server + client) 컴파일 확인 의무. `shared-discipline-guard.sh` 통과.
- **§1 서버 권위 불변**: force-adopt는 reconcile *표현* 게이트 — 서버 권위 상태 변경 없음. 클라는 여전히 서버 vx의 미러.
- **⚠️ `PlayerPredictor.cs` 본체 무변경**: `SnapThreshold = 1.5f` 값 변경 **금지** — dead-zone 축소는 P4(고정스텝) 영역. 명료화 필요 시 주석만, 그것도 최소.
- **⚠️ 분기 구조 무변경**: `ShouldForceAdopt` / `IsMovementLocked`의 분기 *구조* 변경 금지 — **리터럴 → 상수 교체 + 계약 주석만**(동작 보존). dead-zone/매직넘버 결합의 *뿌리*(가변 dt)는 P4가 뽑는다.

---

> Phase 완료 시 `02-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 03(reconcile/보간 회귀 안전망) 착수 — P4 심장부 진입 전 그물.
