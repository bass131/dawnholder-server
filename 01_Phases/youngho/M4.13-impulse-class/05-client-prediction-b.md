---
owner: youngho
milestone: M4.13
phase: 05-client-prediction-b
title: 클라 예측 통일 (방식 B) — InputHistory 임펄스 저장 + forceAdopt 크러치 제거 + 보간 복원
status: planned
grade: 대규모
slug: 05-client-prediction-b
created: 2026-06-13
domains: [client, qa]
prior_phases: [01-action-input-gate, 02-server-impulse-model, 03-dash-behavior, 04-shared-extract]
depends_on: [04-shared-extract]
risk_flags: [client-prediction]
---

# M4.13 Phase 05 — 클라 예측 통일 (방식 B)

> 계획서 = `_milestone-plan.md` "확정 설계 — 네트워크 (방식 B)" + Phase 분해 표 #5. **이 마일스톤 성패의 핵심 계약**(plan-auditor ★핵심 리스크). P4 공유 공식 위에서 클라가 임펄스 궤적을 *직접 예측*해 forceAdopt 크러치를 제거하고 스터터를 없앤다.
> **⚠️ 5a/5b 분해 검토**(plan-auditor 봉합 ①): "InputHistory 임펄스 저장 = 예측 결정성 계약 확장". 착수 시 분해 확정.

---

## Context (왜 — 핵심 리스크)

대쉬·넉백·임펄스공격은 **클라가 예측 못 하는 서버 임펄스**라, 매 스냅샷 `forceAdopt`로 끌려오고 M4.11 P4 reconcile 보간 버퍼 리셋이 그걸 매번 지워 **50ms 간격 스냅(시각 스터터)**이 된다(증거: `[Reconcile]` 500 로그 대쉬 forceAdopt 499/500).

방식 B = 클라가 **임펄스를 예측**하면 forceAdopt 불필요 → 보간 복원 → 스터터 소멸. **단 핵심 리스크: forceAdopt 크러치 제거 = 안전망 제거.** 클라 replay가 서버 임펄스 궤적을 **비트단위 재현** 못하면(감속 공식/시작 틱 1틱 어긋남) 보험 뗀 자리에 **영구 offset 누적**(M4.11 P2 ε silent break 동류). **이 결정성이 마일스톤 성패의 핵심 계약** — P4 공유 공식이 그 안전망.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, main `2433ab5`)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. InputRecord 3필드 | `03_Client/.../Prediction/InputHistory.cs:64-75` | `(ClientTick uint, InputX sbyte, JumpPressed bool)` — **임펄스 채널 없음**(평지 물리만 재현). |
| 1b. replay가 임펄스 0 | `Prediction/PlayerPredictor.cs:108-139`(OnSnapshot) → `:127-132`(`new PhysicsInput(rec.InputX, rec.JumpPressed, dt)` — ExternalVelX 없이) | replay가 ExternalVelX=0(3-arg ctor)으로 평지만 재현 → 임펄스 구간 발산. |
| 2. Predict / 시그니처 | `PlayerPredictor.cs:91-96`(Predict) · `InputHistory.cs:46-55`(ReplayFrom)/`:84-86`(NotifySent) | 방식 B가 시그니처 확장할 메서드(임펄스 채널 추가). |
| 3. forceAdopt 크러치 | `Prediction/LocalPlayerMovement.cs:241-249`(ShouldForceAdopt) → `:379`(OnSnapshot forceAdopt 적용) → `:390-391`(버퍼 리셋) | 매 스냅샷 서버 위치 채택 → 제거 대상. teleportSnap/Hit/Attack+serverVx≥ε 조건. |
| 3b. substep 보간 | `LocalPlayerMovement.cs:251-324`(Update fixed-step) + `:319-323`(prev/curr lerp) | M4.11 P4 보간 — forceAdopt 제거 시 자동 복원. |
| 4. 대쉬 핸들 | `Network/Handlers/Skill/SkillCastHandler.cs:94-112`(HandleDash) | 시전 시점(예측 시작점). |
| 5. 테스트 | `Assets/Tests/EditMode/Prediction/PlayerPredictorTests.cs`(30케이스) · `MovementGateTests.cs`(21케이스) | Predict/replay 시그니처 확장 영향. |

---

## 설계 방향 (5a/5b 분해 — 착수 시 확정)

**5a — 임펄스 예측 결정성 계약 (위험 핵심)**:
- `InputRecord`에 **임펄스 채널 추가**(ExternalVelX 또는 임펄스 상태).
- `Push`/`NotifySent`/`ReplayFrom` 시그니처 확장.
- replay가 매 틱 임펄스를 **시간 전진**(P4 공유 공식으로 등속/감속)시키며 재현 — *서버 궤적 비트단위 재현*.
- `PlayerPredictorTests`(30케이스) replay 결정성 테스트 추가(임펄스 구간 예측 = 서버 일치).

**5b — forceAdopt 제거 + 보간 복원**:
- `ShouldForceAdopt` 임펄스 분기 제거(teleportSnap·진짜 mispredict만 남김).
- substep 보간 자동 복원(스터터 소멸).
- 진짜 mispredict(벽 충돌 등 드묾)에만 reconcile 스냅 유지.

> **5a 없이 5b 먼저 하면 위험** — 예측 없는데 크러치만 떼면 즉시 발산. 5a(결정성 확보) → 5b(크러치 제거) 순서 강제.

---

## 완료 조건 / 게이트 (정량)

- [ ] **(5a)** `InputRecord` 임펄스 채널 + replay가 P4 공유 공식으로 임펄스 시간 전진 재현.
- [ ] **(5a)** replay 결정성 테스트 green — 임펄스 구간 클라 예측 위치 == 서버 위치(허용 오차 명시, offset 누적 0).
- [ ] **(5b)** 대쉬 중 forceAdopt 빈도 **급감** — 2클라 Play `[Reconcile]` 로그 분석(499/500 → 근접 0, 진짜 mispredict만).
- [ ] **(5b)** 보간 복원 — 대쉬 스터터(20Hz 뚝뚝) 소멸, 영호 손맛 실측.
- [ ] `PlayerPredictorTests`(30) + `MovementGateTests`(21) 확장·green.
- [ ] 회귀 green: WSL2 봇 + EditMode + Unity error 0.

---

## 위험 / 헌법 게이트

- **★핵심 계약**: replay 비트단위 재현 실패 = 영구 offset 누적. P4 공유 공식 단일 출처가 안전망. 결정성 테스트가 1차 그물, 2클라 실측이 2차.
- **§1 서버 권위**: 클라 예측은 *박자*만 앞당김 — reconcile 서버 진실 우위 불변. 진짜 mispredict 스냅 유지.
- **§2 Protocol**: 클라 내부 예측 = wire 무변경 v12(InputRecord는 클라 로컬 구조, 패킷 아님 — 착수 시 확인).
- **§4**: replay 공식 = `98_Shared` 단일 출처(P4) — 클라가 별도로 박으면 silent drift.
- **⚠️ 5a→5b 순서 강제** — 크러치 먼저 떼면 발산.

---

> Phase 완료 시 `05-...-DONE.md` 박제(대규모 등급 — 5단계 보고). 게이트 통과 후 Phase 06(클래스 통합 + 전체 회귀).
