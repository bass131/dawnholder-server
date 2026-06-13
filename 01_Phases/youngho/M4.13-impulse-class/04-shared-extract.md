---
owner: youngho
milestone: M4.13
phase: 04-shared-extract
title: 공유 모델 추출 — 대쉬/임펄스 이동 공식을 98_Shared 단일 출처로 (헌법 §4)
status: done
grade: 보통
slug: 04-shared-extract
created: 2026-06-13
completed: 2026-06-13
status_note: done — 보통 등급(work-pin + commit, -DONE.md 불요). 거동 불변 추출, build 0/0 + test 568/0 + 봇 advance=4.00.
domains: [shared]
prior_phases: [01-action-input-gate, 02-server-impulse-model]
depends_on: [02-server-impulse-model]
risk_flags: []
---

# M4.13 Phase 04 — 공유 모델 추출 (98_Shared 단일 출처)

> 계획서 = `_milestone-plan.md` Phase 분해 표 #4. P2에서 통일한 서버 임펄스 모델의 **공식·상수를 `98_Shared`로 추출**해, P5(클라 예측 B)가 *서버와 동일한 공식*으로 임펄스 궤적을 replay할 수 있게 한다. 헌법 §4(공유 코드 규율) 정합.
> **P5의 전제**: 클라 replay가 서버 임펄스 궤적을 비트단위 재현하려면 공식이 **단일 출처**여야 한다(복붙 금지 = silent drift 차단).

---

## Context (왜)

대쉬 상수(`DashLungeInitialVx`/`DashLungeDecayPerTick`, P2 전환 후엔 `DashFixedDistance`/`DashSpeed`)는 현재 **`02_Server/GameServer/Combat/CombatConstants.cs` 서버 전용**이다. P5 방식 B의 핵심 리스크는 "**클라 replay가 서버 임펄스 궤적을 1틱이라도 어긋나면 영구 offset 누적**"(M4.11 P2 ε 공유상수 silent break와 동류). 그 함정을 막는 유일한 구조적 안전망이 **공식의 단일 출처화** — 클라/서버가 같은 `98_Shared` 코드를 컴파일해 보게 한다.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 대쉬 상수 서버 전용 | `02_Server/GameServer/Combat/CombatConstants.cs:100/106/27` | DashLungeInitialVx/Decay/AttackLungeInitialVx — **02_Server 전용** → 추출 대상. |
| 2. 넉백은 이미 공유 | `98_Shared/GameData/Constants.cs:79/87/93` | KnockbackInitialVx/ExternalImpulseEpsilon/KnockbackDecayPerTick — **이미 Shared**(선례·패턴). |
| 3. 물리 채널 공유 | `98_Shared/GameData/Physics.cs:148`(`vx = InputX*MoveSpeed + ExternalVelX`) + `:42-52`(PhysicsInput 3/4-arg ctor) | 임펄스 합성 물리 = 이미 결정론 Shared. |
| 4. 공식 순수성 | `98_Shared/GameData/Formulas.cs`(순수 함수) | 추출된 임펄스 공식도 순수(DateTime/seed 없는 RNG 금지) — 클라/서버 동일 출력. |

---

## 설계 방향 (착수 시 확정 — 골격)

- **대쉬/임펄스 공식·상수를 `98_Shared`로** — `Constants.cs`(상수) + 필요 시 `Physics.cs`/신설 헬퍼(이동 한 틱 전진 공식). 넉백이 이미 Shared인 패턴을 대쉬·lunge로 확장.
- **순수 함수만** — "임펄스 한 틱 전진"(등속 거리 누적 또는 감속)을 클라/서버가 같은 입력→같은 출력으로. seed 없는 RNG·`DateTime` 금지.
- **wire 무변경 점검(§2)** — 공식 이동은 *내부 표현*. 패킷 형상 무손상 v12.
- **Shared.dll 재빌드** — shared 소스 실변경이라 `03_Client/Assets/Plugins/Shared.dll` 갱신 정당(drift 아님). 양쪽 컴파일 확인(§4).

---

## 변경 대상 (파일별 — 착수 시 확정)

1. **`98_Shared/GameData/Constants.cs`** — 대쉬 거리/속도 상수 이전(서버 → 공유).
2. **`98_Shared/GameData/Physics.cs` 또는 신설** — 임펄스 한 틱 전진 순수 함수(클라 replay·서버 틱 공용).
3. **`02_Server/.../CombatConstants.cs`** — 이전된 상수 참조를 Shared로 치환(서버 전용 잔재 정리).
4. **Shared.dll / ClientNet.dll 재빌드 + Plugins 갱신.**

---

## 완료 조건 / 게이트 (정량) — ✅ 전부 통과 (2026-06-13)

- [x] 대쉬/임펄스 공식·상수가 **`98_Shared` 단일 출처** — `DashSpeed`/`DashTravelTicks`/`AttackLungeInitialVx` 정의가 `Constants.cs` 단 한 곳(grep: 서버 전용 중복 정의 0, reviewer 재확인).
- [x] 클라/서버가 **같은 공식** 참조 — 서버는 `Constants.X` + `Physics.DecayImpulse` 호출. P5 클라 replay가 동일 함수 호출 예정(이 Phase가 그 전제 충족).
- [x] 임펄스 전진 공식 **순수 함수** — `Physics.DecayImpulse(vx, decayPerTick)`: 입력만으로 결정, DateTime/seed RNG/부작용 0(reviewer 명시 확인).
- [x] **wire v12 무변경** — Protocol/Generated/ProtocolVersion 미접촉(공식 추출=내부 표현). WSL2 build 0/0(Shared+GameServer+ClientNet). Unity Plugins dll 재빌드+sync는 PR 시점(소스만 commit).
- [x] 회귀 green: WSL2 **test 568/0/4**(P3 baseline 568 비감소) + 봇 **DashSmoke PASS advance=4.00**(D=DashSpeed×DashTravelTicks×TickDuration=4.0 정확 — 궤적 비트 보존).

**검증 흐름**: server Worker(Sonnet) 구현 → 메인 diff 실측(거동 불변·ε 비트동일 대조) → WSL2 build 0/0 + test 568/0 → 봇 advance=4.00 → reviewer Tier 2-A 🔴0/🟡0.

---

## 위험 / 헌법 게이트

- **§4 공유 코드 규율**: `98_Shared` 변경 → server + client **둘 다 컴파일** 확인 의무. Shared.dll 재빌드(checkout 금지 — 실변경).
- **§2 Protocol**: 공식 추출 = 내부. wire 무변경 v12.
- **§1 서버 권위**: 공유 공식은 *동일 계산* 보장용 — 권위는 여전히 서버(클라는 예측 후 reconcile).
- **함정(M4.11 P2 동류)**: 공유 안 하고 클라/서버가 각자 박으면 silent drift → P5 영구 offset. **이 Phase가 그 안전망**.

---

> Phase 완료 시 `04-...-DONE.md` 박제(보통 등급 — work-pin + commit). 게이트 통과 후 Phase 05(클라 예측 B) — P5가 이 공유 공식으로 replay 결정성 확보.
