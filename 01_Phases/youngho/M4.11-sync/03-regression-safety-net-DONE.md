---
summary: P4(PlayerPredictor 가변 dt→50ms 고정 서브스텝, 고위험 심장부) 착수 전 회귀 그물 5건을 쳤다 — 핵심 설계는 불변식(P4 후 절대 green)과 baseline(`[P3 baseline — P4 재검토 대상]` 마커+사유 기입 슬롯, P4가 의도적 변경 가능)의 이원 구분으로, P4의 의도된 변경과 몰래 깨진 회귀를 가른다. golden은 computed-expectation(Predict == Physics.Step fold, 같은 런타임 안 비교)으로 cross-runtime float 재현성 함정을 원천 회피. 게임 코드 diff 0(기계 판정 빈 출력 증명).
owner: youngho
milestone: M4.11
phase: 03-regression-safety-net
work-id: phase03-m4.11-sync
status: done
grade: 복잡
slug: 03-regression-safety-net
created: 2026-06-11
completed_at: 2026-06-12
commit: cf52134
domains: [qa]
prior_phases: [01-remote-interp-servertick, 02-force-adopt-decouple]
depends_on: [02-force-adopt-decouple]
---

# Phase 03 — reconcile·보간 회귀 안전망 (P4 고정스텝 수술 전 그물) 완료 박제

**소요 시간**: 1 세션

## TL;DR

P4 = `PlayerPredictor`의 가변 dt(delta-time)를 50ms 고정 서브스텝 + 시각 보간으로 전환하는 **고위험 심장부 수술**이다. 그물 없이 손대면 M4.9에서 갓 봉합한 reconcile/force-adopt 발산을 다시 부른다. 그래서 P4 착수 *전* 회귀 안전망 5건(Physics.Step golden 궤적·Predict baseline+replay 불변식·봇 스냅샷 연속성·facing 회귀·2클라 수동 체크리스트)을 쳤다.

이 Phase의 **핵심 설계 = 불변식(invariant) vs baseline 이원 구분**이다. P4는 "절대 바뀌면 안 되는 것"(서버 궤적·replay 동치·봇 연속성)과 "의도적으로 바꿀 것"(가변 dt Predict 궤적)을 동시에 건드린다 — 한 바구니에 담으면 P4에서 *의도된 변경*과 *몰래 깨진 회귀*를 구분 못 한다. baseline 케이스에는 `[P3 baseline — P4 재검토 대상]` 마커 + "변경 전 값 → 후 값 + 사유 1줄 기입" 빈 슬롯을 박아, P4 작업자가 마커를 찾는 순간 기입 의무가 물리적으로 보이게 했다.

그물은 *거동을 박제할 뿐 바꾸지 않는다* — 게임 코드 diff 0(기계 판정 빈 출력로 증명). WSL2 561 passed(+5: golden 2 + facing 3), Unity EditMode 119 passed(+3: baseline 2 + replay 불변식 1), 봇 M2BasicMovement 연속성 정상, reviewer 6축 통과(🔴 0), plan-auditor 조건부GO 3건 봉합 후 착수.

## AC 검증 결과

Phase 완료조건 = 신규 테스트 전부 현재 코드 기준 green(WSL2 + EditMode 0 fail) + 불변식/baseline 구분이 주석에 박힘 + 체크리스트 문서 골격 완성 + Unity 컴파일 clean + 게임 코드 diff 0(기계 판정). 실제 실행 결과:

```bash
# 서버 단위/통합 테스트 (WSL2, ADR-029 — SAC가 Windows dotnet test 차단)
$ wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build"
  Passed!  - Failed: 0, Passed: 561, Skipped: 4   (+5: Physics.Step golden 궤적 2 + facing 회귀 3)

# 클라 EditMode 전체 (Unity TestRunnerApi 실행)
  scriptCompilationFailed=False
  EditMode: passed=119 failed=0   (+3: Predict baseline 2 + replay 불변식 1)

# 봇 M2BasicMovement 1회 (WSL2, 서버 P2 빌드)
  success=True intents=1000 snapshots=1051 desync=(0.00,0.00)
  → 신규 위치 점프 탐지 assert 미발동 (서버 측 연속성 정상)

# baseline 마커 grep (불변식/baseline 구분 식별 가능 — AC)
$ grep -c "P3 baseline" 03_Client/Assets/Tests/EditMode/Prediction/PlayerPredictorTests.cs
  5

# ★게임 코드 diff 0 — 기계 판정 (plan-auditor D1, 사람 눈 분류 금지)
$ (git diff --name-only HEAD; git ls-files --others --exclude-standard) \
    | grep -vE '^(02_Server/GameServer\.Tests/|99_Tools/|03_Client/Assets/Tests/|01_Phases/)'
  (빈 출력)  → 변경 전부 화이트리스트 안 = 게임 코드 diff 0 기계 증명
```

체크리스트 문서 `_p4-2client-checklist.md` = 항목(rubber-band / Dash lunge / 넉백 / 창드래그 desync) × "P4 전 거동" / "P4 후 거동" 비교 컬럼 골격 완성. 머리에 "자동 회귀 아님 — 영호 수동 수행 전용" 명시. 영호가 P4 전 서버 실행 중 "P4 전 거동" 컬럼 1회 기입 가능한 형태.

plan-auditor 조건부GO → D1(diff 화이트리스트 기계 판정)/D2(baseline 사유 기입 슬롯)/D3(golden epsilon + 런타임 격리) 3건 봉합 후 착수. reviewer 6축 통과 🔴 0 / 🟡 2: ①FacingSnapTests dead-setup 주석 → 반영 완료, ②봇 임펄스 여유 계수 2.0f 재측정 → 미래 시나리오 확장 시(백로그).

## 결정 흐름 (회고 참고용)

- **불변식 vs baseline 이원 설계(P3의 핵심)** → P4 회귀 판정의 전제. 한 바구니면 P4에서 의도된 변경과 회귀를 구분 못 함. 불변식(P4 후 절대 green) = Physics.Step golden·replay 동치·봇 연속성 / baseline(P4가 의도적 변경 가능) = 가변 dt Predict 궤적. baseline에 `[P3 baseline — P4 재검토 대상]` 마커 + 사유 기입 빈 슬롯.
- **computed-expectation 채택(golden 하드코딩 기각)** → `Predict == Physics.Step fold`를 *같은 런타임 안에서* 비교. 하드코딩 golden은 런타임 간(WSL2 CoreCLR xUnit vs Unity Mono EditMode) float 재현성 미보장이라 flaky 위험 — 동치 비교로 그 함정을 원천 회피(plan-auditor D3: 각 golden은 자기 런타임 안에 가둔다). reviewer가 두 경로(Predict·Physics.Step) 모두 `StepFlat`으로 수렴함을 확인 → vacuous(양변 같은 코드라 무의미)가 아닌 진짜 박제이며, P4가 고정 서브스텝으로 바꾸면 정확히 이 동치가 깨져 변화를 알린다.
- **facing 백로그① P3 흡수** → P1 reviewer 🟡 백로그①(영호 지목 공백, `ProcessAttack` facing 스냅 미검증)을 P3로 회수. plan-auditor "혼합 아닌 응집" 판정 — 현재 미검증 서버 거동의 불변식 박제 범주라 P3 안전망 응집에 부합(별 Phase로 쪼갤 잡탕 아님).
- **FacingSnapTests = `Players[0]` 직접 접근** → live target / 허공 스윙 시나리오 구성 시 EnemyId 선점으로 AllocId 순서 의존을 회피하고 `Players[0]` 직접 접근으로 결정론 확보.

## 막혔던 지점

- **xUnit `Assert.Equal(sbyte, sbyte, string)` overload 부재** → facing 회귀 테스트에서 메시지 동반 비교가 컴파일 실패. `Assert.True(actual == expected, msg)` 우회로 메시지 보존.
- **봇(99_Tools)이 서버 전용 상수(`DashLungeInitialVx`) 참조 불가** → 99_Tools는 서버 어셈블리 미참조. 위치 점프 탐지 상한 계산에 리터럴 + 출처 주석(어느 서버 상수에서 왔는지)이 올바른 타협(헌법 #4 복붙 금지 vs 도달 불가 의존성 사이).
- **Grep 도구 출력이 `//` 주석을 `\`로 렌더 → 문법 오류 허위 경보** → 실제 파일 Read로 확인하니 정상. 도구 출력 렌더링 아티팩트지 소스 문제 아님.

## 학습 일지 후보 키워드

- regression safety net (invariant vs baseline) — 수술 전 그물은 두 종류로 나눠 쳐야 의도된 변경과 몰래 깨진 회귀를 가른다. baseline 마커 + 사유 기입 슬롯으로 갱신 의무를 물리적으로 가시화.
- computed-expectation vs hardcoded golden — `Predict == Physics.Step fold` 동치 비교가 하드코딩 golden보다 강한 박제(같은 런타임 안, float 재현성 함정 회피, P4 변경 시 정확히 깨짐). 단 두 변이 같은 코드면 vacuous — 두 경로가 진짜 수렴하는지 reviewer 확인.
- float cross-runtime reproducibility — WSL2 CoreCLR vs Unity Mono 간 부동소수 재현성 미보장 → golden은 자기 런타임 안에 가둔다(xUnit 단독 / EditMode 단독).
- falsifiable AC(diff whitelist 기계 판정) — "게임 코드 diff 0"을 사람 눈 분류가 아니라 화이트리스트 grep 빈 출력으로 기계 증명. 검증 가능한 완료조건.
- 안전망은 거동을 박제하지 변경하지 않는다 — golden이 이상해 보여도 현재 출력 그대로 박고 의문은 주석으로만. 수정은 P4 또는 별도 Phase.
- 도달 불가 의존성에서의 타협 — 봇(99_Tools)이 서버 상수 참조 불가 시 리터럴 + 출처 주석 / xUnit overload 부재 시 `Assert.True` 우회.

---

## Commits (feature/m4.11-sync)

```
cf52134 test(qa): M4.11 P3 회귀 안전망 — golden 궤적·Predict baseline·봇 연속성·facing 회귀
```

## 다음 스텝

- **P4 착수 전**: 영호가 `_p4-2client-checklist.md` "P4 전 거동" 컬럼을 서버 실행 중 1회 기입 → P4(고정스텝, 고위험 심장부) GO 결정.
- **P4** (로컬 예측 고정스텝 전환 — 가변 dt → 50ms 고정 서브스텝 + 시각 보간). ⚠️ `LocalPlayerMovement`/`PlayerPredictor` 금지 해제 조건 = P3 그물 green 유지.
- **P5** (재빌드).
- 백로그: 봇 임펄스 여유 계수 2.0f 재측정(미래 시나리오 확장 시, reviewer 🟡②) / `reviewer.md` 체크리스트 경로 drift.
