---
owner: youngho
milestone: M4.11
phase: 03-regression-safety-net
title: reconcile·보간 회귀 안전망 — P4 고정스텝 수술 전 그물
status: done
grade: 복잡
slug: 03-regression-safety-net
created: 2026-06-11
domains: [qa]
prior_phases: [01-remote-interp-servertick, 02-force-adopt-decouple]
depends_on: [02-force-adopt-decouple]
---

# M4.11 Phase 03 — reconcile·보간 회귀 안전망 (P4 고정스텝 수술 전 그물)

> 마일스톤 계획서 = `_milestone-plan.md` P3 행(#66). 위험 오름차순 게이트식의 **세 번째 매듭** — P4(로컬 예측 고정스텝 전환, 고위험 심장부) 착수 *전 필수*.
> 이 Phase는 게임 코드를 **한 줄도 바꾸지 않는다.** 오직 *현재 거동*을 green으로 박제하는 그물(테스트·봇 assert·체크리스트)만 친다. P4의 회귀 판정은 이 그물이 기준이 된다.

---

## Context (왜)

P4 = `PlayerPredictor`의 가변 dt(delta-time, 프레임 간 경과 시간)를 **50ms 고정 서브스텝 + 시각 보간**으로 전환한다. 이건 M4.9에서 갓 봉합한 reconcile/force-adopt와 *바로 옆에 붙은* 심장부 수술이다. 그물 없이 손대면 reconcile 발산을 다시 부른다.

그래서 마일스톤 계획의 P3 게이트 = **"★P4 착수 전 필수 — 안전망이 *현재 거동*을 green으로 고정."** P3은 거동을 바꾸지 않는다. 거동을 박제할 뿐이다. 박제된 거동이 P4 후에도 유지되는지가 P4의 합격 기준이 된다.

---

## 실측 확정 사실 — P4 폭발 반경 (2026-06-11, qa Worker가 현 HEAD에서 확정)

> 이 절은 P4가 *무엇을 건드리고 무엇을 안 건드리는지*를 코드로 못 박은 것이다. 안전망을 어디에 쳐야 하는지가 여기서 나온다 — 재탐색 불필요.

- **가변 dt 진입점은 단 하나**: `PlayerPredictor.Predict(inputX, jumpPressed, dt)`(`03_Client/Assets/Scripts/Prediction/PlayerPredictor.cs:96`). 호출자는 `LocalPlayerMovement.Update()`가 `Mathf.Min(Time.deltaTime, MaxPredictStep 0.1f)`로 클램프한 뒤 전달한다. P4가 고정스텝으로 바꿀 곳은 **이 경로뿐.**
- **`OnSnapshot` 내부 replay(`PlayerPredictor.cs:135`)는 이미 `Constants.TickDuration` 고정 dt**다 — P4가 바꿀 게 아니다. 즉 reconcile replay는 *이미* 결정론적이고, P4는 Predict(라이브 예측) 경로만 고정스텝화한다.
- **`Physics.Step`(98_Shared)은 순수 static 함수**다 — 인스턴스 상태도 글로벌 상태도 없다. 같은 입력 → 같은 출력. 서버는 고정 0.05f로, 클라 Predict만 가변 dt로 *같은 함수*를 호출한다. P4 = 클라 측 dt를 0.05f 서브스텝으로 맞추는 것.
- **기존 안전망 인벤토리**:
  - 클라 EditMode Prediction 53케이스: `PlayerPredictorTests` 24 / `InputHistoryTests` 13 / `MovementGateTests` 16.
  - 서버 xUnit: `PhysicsTests`(Step 결정론) / `MoveIntentTests`(고정 dt wire) / `CommitWindow` / `HitKnockback` / `AnimState` / `Broadcast` / `RoundTrip`.
  - 봇 `M2BasicMovement`(1000 intent 최종 desync < 5px).
- **구조적 한계**: 봇은 클라 `PlayerPredictor` 내부에 도달하지 못한다(서버 상태만 관찰). 따라서 **Prediction EditMode가 P4 회귀의 유일한 자동 1차 방어선**이다. 봇은 서버 측 연속성만 본다.

---

## 안전망 설계 — 두 종류를 구분해 박는다 (이 구분이 P3의 핵심 설계)

P4는 "절대 바뀌면 안 되는 것"과 "의도적으로 바꿀 것"을 동시에 건드린다. 그물도 둘로 나눠 친다. 한 바구니에 담으면 P4에서 *의도된 변경*과 *몰래 깨진 회귀*를 구분 못 한다.

| 종류 | 정의 | P4 후 의무 | 표기 |
|---|---|---|---|
| **불변식(invariant)** | P4 후에도 *절대* green 유지. 거동이 바뀌면 그건 회귀(버그). | 무조건 green. red = P4 STOP. | (표기 없음) |
| **baseline(현재 거동 박제)** | P4가 *의도적으로* 바꿀 수 있는 거동(가변 dt Predict 궤적 등). 몰래 바뀌는 걸 막는 용도. | P4에서 바뀌면 *의식적 갱신* + 사유 박제 의무. | 주석에 `[P3 baseline — P4 재검토 대상]` |

- **불변식 테스트**:
  - ① `Physics.Step` golden 궤적 — 스크립트 입력 시퀀스 × 고정 50ms × N스텝 → 위치 golden. *서버 궤적*은 P4가 안 건드리니 불변 증명.
  - ② replay 경로(고정 dt) 결과 = 서버 모사 궤적 동일 — replay는 이미 고정 dt(`:135`)라 P4 무관.
  - ③ 봇 스냅샷 간 위치 연속성(순간이동 탐지) — 서버 측 연속성은 P4와 무관해야 함.
- **baseline 테스트**:
  - 가변 dt Predict 궤적 golden — P4가 바로 이걸 고정스텝으로 바꾼다. 지금 출력을 박아 두고, P4에서 *왜* 바뀌었는지 사유를 남기게 강제.

---

## 작업 항목 5건 (qa 도메인 — 게임 코드 전부 READ-ONLY)

| # | 항목 | 위치 | 규모 | 종류 |
|---|---|---|---|---|
| 1 | `Physics.Step` golden 궤적 1~2케이스 | `PhysicsTests.cs` (xUnit/WSL2) | ~20줄 | 불변식 |
| 2 | Predict 궤적 확장 3~5케이스 | `PlayerPredictorTests.cs` (EditMode) | ~50줄 | baseline + 불변식 |
| 3 | 봇 스냅샷 간 위치 점프 탐지 assert | `M2BasicMovement` | ~5줄 | 불변식 |
| 4 | facing 스냅 회귀 테스트 (P1 백로그① 회수) | `CombatSystem.ProcessAttack` 단위 (xUnit) | ~30줄 | 불변식 |
| 5 | 2클라 수동 체크리스트 문서 | `_p4-2client-checklist.md` | 문서 | 수동(자동 아님) |

**1. `Physics.Step` golden 궤적** — 스크립트 입력 시퀀스(예: inputX=+1 N틱 → jump → 낙하)를 고정 50ms × N스텝으로 돌려 *최종 위치 + 중간 키프레임*을 golden 값으로 박는다. 서버 궤적 불변 증명. 불변식.

**2. `PlayerPredictorTests.cs` 확장** — EditMode:
- (baseline) 스크립트 dt 시퀀스(명시값, `Time.deltaTime` 금지)로 `Predict` 궤적 golden. P4가 의도적으로 바꿀 거동. 주석에 `[P3 baseline — P4 재검토 대상]` + **사유 기입 슬롯**(plan-auditor D2): 주석 골격에 "P4에서 갱신 시: 변경 전 값 → 후 값 + 사유 1줄을 아래에 기입" 빈 슬롯을 같이 박아, P4 작업자가 마커를 찾는 순간 기입 의무가 물리적으로 보이게 한다.
- (불변식) snapshot + replay 후 최종 위치 = 서버 모사(`Physics.Step` 고정 dt) 일치. replay는 이미 고정 dt라 P4 무관 → 불변.

**3. 봇 `M2BasicMovement` 위치 점프 탐지** — 연속 스냅샷 간 위치 델타가 물리적 상한(고정 dt × 최대 속도 + 임펄스 여유)을 넘으면 순간이동으로 assert fail. 서버 측 연속성은 P4와 무관해야 함. 불변식.

**4. facing 스냅 회귀 테스트** — P1 reviewer 🟡 백로그①(영호 지목 공백) 회수. `CombatSystem.ProcessAttack` 단위 2~3케이스(xUnit):
- live target → `FacingDir`이 타겟 방향으로 스냅.
- 허공 스윙(sentinel·stale target) → `FacingDir` 유지(스냅 안 함).
- 데미지/히트 판정은 검증 범위 밖(facing latch만). 불변식.

**5. 2클라 수동 체크리스트 문서** — `01_Phases/youngho/M4.11-sync/_p4-2client-checklist.md`:
- rubber-band / Dash lunge / 넉백 / 창드래그(P1 desync) 항목 × "P4 전 거동" / "P4 후 거동" 비교 컬럼.
- 자동화 불가 맹점(봇은 클라 Predict 도달 불가)을 사람 눈으로 메우는 보완. 문서 머리에 **"이건 자동 회귀가 아니다 — 영호 수동 수행 전용"** 명시.

---

## 완료 조건 / 게이트 (정량)

- [ ] 신규 테스트 전부 *현재 코드* 기준 green: WSL2 xUnit(기존 556 + 신규) 0 fail + EditMode(기존 116 + 신규) 0 fail.
- [ ] 불변식 vs baseline 구분이 테스트 주석에 박힘 — `[P3 baseline — P4 재검토 대상]` grep으로 baseline 케이스 식별 가능.
- [ ] 체크리스트 문서 `_p4-2client-checklist.md` 존재 + 영호 1회 수행으로 "현재 거동" 컬럼 기입 가능한 형태(항목·컬럼 골격 완성).
- [ ] Unity 컴파일 clean (error 0).
- [ ] **게임 코드 diff 0 — 기계 판정 (plan-auditor D1)**: 변경 허용 화이트리스트 = `02_Server/GameServer.Tests/` · `99_Tools/` · `03_Client/Assets/Tests/` · `01_Phases/` **만**. 판정 명령이 빈 출력이면 통과, 한 줄이라도 나오면 게이트 fail (사람 눈 분류 금지):
  ```bash
  git diff --name-only HEAD | grep -vE '^(02_Server/GameServer\.Tests/|99_Tools/|03_Client/Assets/Tests/|01_Phases/)'
  ```

---

## 위험 / 헌법 약속 — 금지 항목

- **게임플레이 코드 일체 무변경**: `02_Server`(非테스트) / `03_Client`(非테스트) / `98_Shared` 한 줄도 안 건드린다. 안전망은 *현재 거동*을 박제하는 것 — 거동을 바꾸면 그물이 아니라 수술이다. **golden 값이 "이상해 보여도" 현재 출력 그대로 박고, 주석으로 의문만 남긴다**(수정은 P4 또는 별도 Phase).
- **⚠️ `PlayerPredictor.cs` / `LocalPlayerMovement.cs` 무변경**: 읽기만. P4가 건드릴 심장부 — P3는 관찰만.
- **§2 프로토콜 wire 무변경**: PDL.xml / `ProtocolVersion` 손대지 않음 — **v12 유지**. 신규 패킷·필드 없음.
- **결정론 강제**: 테스트가 `Time.deltaTime` 등 비결정 소스를 dt로 쓰지 않는다 — dt는 전부 스크립트 명시값. 비결정 dt를 박으면 golden 자체가 flaky해져 그물이 무의미.
- **golden 부동소수 재현성 (plan-auditor D3)**: golden 비교는 *허용 오차(epsilon, 예 1e-4f) 기반* — 정확 일치(exact equality) 금지. 같은 golden을 두 런타임(WSL2 CoreCLR xUnit vs Unity Mono EditMode)에 걸쳐 비교하지 않는다 — 런타임 간 부동소수 재현성은 미보장이므로 각 golden은 자기 런타임 안에 가둔다(작업항목 1=xUnit 단독, 2=EditMode 단독).
- **§1 서버 권위 불변**: 테스트는 서버/공식의 *기존 출력*을 관찰할 뿐, 권위 상태를 만들거나 바꾸지 않는다.

---

> Phase 완료 시 `03-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 04(로컬 예측 고정스텝 전환 — 고위험 심장부) 착수 — 이 그물이 P4 회귀 판정 기준이 된다.
