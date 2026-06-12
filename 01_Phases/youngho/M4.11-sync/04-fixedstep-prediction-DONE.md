---
summary: 로컬 예측을 가변 dt(매 프레임 delta-time)에서 50ms 고정 서브스텝 + 시각 보간으로 전환했다 — 마일스톤 도미노의 뿌리("클라 가변 dt vs 서버 고정 틱") 제거. `Predict(dt)` → `Predict()`로 dt 파라미터를 *물리적으로* 차단(illegal state unrepresentable)하고, `LocalPlayerMovement.Update`를 accumulator 고정 서브스텝 루프(입력 샘플링→Predict→C_MoveIntent→NotifySent 4종 1:1)로 재구조화 — 서버 "틱당 입력 1개 소비 → Step 1회"의 정확한 거울. 화면은 substep prev/curr 두 점 lerp로 프레임 Hz 부드럽게. wire 무변경(ProtocolVersion 12 유지)·클라 3파일 격리. WSL2 561 passed / EditMode 119 passed / 봇 desync (0.00, 0.00) / 영호 2클라 6항목 전부 이상 무.
owner: youngho
milestone: M4.11
phase: 04-fixedstep-prediction
work-id: phase04-m4.11-sync
status: done
grade: 복잡
slug: 04-fixedstep-prediction
created: 2026-06-12
completed_at: 2026-06-12
commit: 0f5977c
domains: [client, shared]
prior_phases: [01-remote-interp-servertick, 02-force-adopt-decouple, 03-regression-safety-net]
depends_on: [03-regression-safety-net]
---

# Phase 04 — 로컬 예측 고정스텝 전환 (가변 dt 뿌리 제거 + 시각 보간) 완료 박제

**소요 시간**: 1 세션

## TL;DR

마일스톤 도미노의 **뿌리**를 뽑았다 — "클라는 매 프레임 가변 dt로 예측하는데 서버는 고정 50ms 틱으로 시뮬레이션한다"는 적분 박자 불일치가 drift(누적 오차)의 구조적 원인이었고, 그 위에 SnapThreshold dead-zone과 force-adopt 게이트라는 잎사귀가 얹혀 있었다. P4는 클라 예측을 서버와 똑같은 **50ms 고정 서브스텝**으로 맞추고, 화면은 두 서브스텝 *사이*를 보간해 프레임 Hz로 부드럽게 그린다("Fix Your Timestep" 정석 — 계산 박자와 그림 부드러움 분리).

핵심 설계 = `Predict(dt)`의 dt 파라미터를 *물리적으로 제거*한 것이다. 시그니처에서 dt를 빼고 내부에서 `Constants.TickDuration`을 고정으로 먹이면, 가변 dt가 들어갈 *진입점 자체가 사라진다*(illegal state unrepresentable — 잘못된 상태를 표현 불가능하게 만드는 설계). 그 위에서 `LocalPlayerMovement.Update`를 accumulator 고정 서브스텝 루프로 재구조화 — {source-gating 입력 샘플링 → Predict → C_MoveIntent 송신 → NotifySent} 4종 세트를 1:1로 묶어 50ms마다 돌린다. 이게 서버 GameMap의 "틱당 입력 1개 소비 → Step 1회"의 정확한 거울이라, 클라 N번째 Predict와 서버 N번째 Step이 같은 입력 × 같은 dt를 먹는다 → drift 0.

wire 무변경(ProtocolVersion **12 유지**)·diff는 클라 3파일로 격리·송신 cadence 20/s 불변이라 프로토콜·서버는 손대지 않았다. WSL2 561 passed / Unity EditMode 119 passed(baseline 2건 갱신 포함) / 봇 M2BasicMovement desync (0.00, 0.00) / reviewer 6축 🔴0 🟡1 / 영호 2클라 실측 6항목 전부 "이상 무"(2026-06-12). STOP 포인트 3건은 전부 미발동 — 이 Phase 범위에서 자율 결정 안 함.

## 착수 게이트

P3(`03-regression-safety-net.md`)가 친 그물이 green인 *뒤에만* 착수했다. Phase 정의 = `04-fixedstep-prediction.md` (plan-auditor **조건부GO**: D2[완료조건에 조건부 §4 양쪽빌드 게이트 추가] / D3[`_milestone-plan.md` stale v11→v12 정정] 2건 봉합 후 착수). commit `db941ee`(Phase 정의 + D2/D3 봉합).

## AC 검증 결과

Phase 완료조건 = WSL2 561 유지 + EditMode 119 기준(baseline 2건 사유 기입 후 갱신 허용) + 봇 desync (0.00, 0.00) 유지 + Unity 컴파일 0 error + 2클라 6항목 영호 실측 "이상 무" + wire 무변경(v12 유지). 실제 실행 결과:

```bash
# 서버 단위/통합 테스트 (WSL2, ADR-029 — SAC가 Windows dotnet test 차단)
$ wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --no-build"
  Passed!  - Failed: 0, Passed: 561   (불변식 全: golden 궤적 / replay / 봇 연속성 / FacingSnap)

# 클라 EditMode 전체 (Unity TestRunnerApi 실행)
  scriptCompilationFailed=False
  EditMode: passed=119 failed=0   (baseline 2건 갱신 포함 — [P3 baseline 마커 슬롯에 변경 전→후 + 사유 기입)

# 봇 M2BasicMovement 1회 (WSL2)
  success=True intents=1000 snapshots=1051 desync=(0.00, 0.00)
  → 서버 측 연속성 불변 (P4는 클라 예측 박자만 바꿈)

# wire 무변경 — diff 클라 3파일 격리, ProtocolVersion 12 유지, C_MoveIntent 형상·송신 cadence 20/s 불변
```

영호 2클라 실측(2026-06-12): rubber-band / Dash lunge / 평타 lunge / 피격 넉백 / 창드래그(P1 desync) / 정지 떨림 **6항목 전부 "이상 무"**. 영호 코멘트: "나머지는 디테일(폴리싱) — 이후에 재작업"(잔여 폴리싱은 후속, P4 회귀 판정과 무관). ⚠️ 실측 환경: Editor Play = P4 신코드 / 기존 빌드 클라 = P3 구코드(P5에서 재빌드) — 로컬 체감 판정은 Editor 측.

reviewer 6축 **🔴 0 / 🟡 1**(SnapThreshold 축소 판단용 실측 메모 — 수정 불요).

## 구현 (7항목)

1. **`Predict(dt)` → `Predict()`** — dt 파라미터 제거, 내부 `Constants.TickDuration` 고정. 가변 dt 진입을 *물리적으로* 차단(illegal state unrepresentable — 시그니처에 가변 dt가 들어갈 자리 자체가 없음).
2. **accumulator 고정 서브스텝 루프** — `LocalPlayerMovement.Update`가 accumulator로 `TickDuration`을 넘을 때마다 {source-gating 입력 샘플링 → Predict → C_MoveIntent 송신 → NotifySent} 4종 세트를 1:1로 실행. 서버 GameMap "틱당 입력 1개 소비 → Step 1회"의 정확한 거울.
3. **시각 보간** — substep prev/curr 두 점을 `accumulator/TickDuration`으로 lerp. reconcile/spawn(`SetServerPosition`/`OnServerSnapshot`) 시 보간 버퍼 양쪽 리셋 — 텔레포트를 가로질러 보간 금지. 위치 점프 4경로(spawn / reconcile / 맵전환 / 사망리스폰) 전수 확인(reviewer).
4. **점프 latch** — substep 소비 시 클리어. 고fps에서 substep 0번 도는 프레임에 점프 유실 방지.
5. **타이머 박자** — 게이트 타이머(`_commitWindowRemaining`/`_hitGateRemaining`) = substep 박자 감쇠(잠금 연장 방향 = 안전 측). UI 쿨다운 4종 = frame dt 감쇠 유지.
6. **스파이크 cap** — `MaxPredictStep=0.1f` 은퇴 → `MaxSubstepsPerFrame=4` 클라 로컬 const. 초과분은 **통째 틱 `%=` 드랍**. (메인 세션 검수 정정 1건 — Worker 초안은 backlog 보존이었으나, 서버 입력 큐 `MaxInputQueue=6` drop-oldest[`PlayerEntity.cs:23-27`]라 추격 버스트는 어차피 서버에서 버려지고 reconcile 대상만 늘림 → 스펙대로 드랍.)
7. **주석·baseline 정정** — over-engineering 주석(`PlayerPredictor.cs:30`)을 재평가 사유로 교체. baseline 테스트 2건은 `[P3 baseline` 마커 슬롯에 "변경 전→후 + 사유" 기입 후 갱신(P3 설계 의도대로 작동 실증). 불변식 테스트는 무변경.

## 메인 세션 검수 정정 (1건)

`MaxSubstepsPerFrame` 초과분 처리 = Worker 초안의 backlog 보존을 **통째 틱 `%=` 드랍**으로 정정. 근거: 서버 입력 큐가 `MaxInputQueue=6` drop-oldest(`PlayerEntity.cs:23-27`)라, 저fps에서 밀린 추격 substep 버스트를 클라가 모아 송신해도 서버 큐에서 오래된 것부터 버려진다. backlog 보존은 *버려질 입력*을 만들고 reconcile 대상만 늘리는 역효과 → 스펙(서버 거동)에 맞춰 드랍하는 게 정합.

## STOP 포인트 (3건 전부 미발동)

이 Phase 범위에서 자율 결정하지 않고 영호 의논으로 미룬 항목 — 셋 다 발동 안 함:
1. **SnapThreshold 1.5f 축소** → 후속. Play 중 `[Reconcile]` 로그 카운트를 정량한 뒤 결정(고정스텝이 drift를 얼마나 줄였는지 측정해야 안전한 새 값이 나옴).
2. **새 공유 상수** → 불필요. `MaxSubstepsPerFrame`은 클라 로컬 const로 충분(서버 영향 없음 → §4 양쪽 게이트 불요).
3. **wire 변경** → 불필요. C_MoveIntent 형상·cadence 그대로 충분했음(ProtocolVersion 12 유지).

## 결정 흐름 (회고 참고용)

- **Predict 시그니처 dt 제거(1순위안 채택)** → 가변 dt 차단을 런타임 clamp(방어 코드)가 아니라 *시그니처 제거*로. 기존 테스트 호출부가 전부 `dt=TickDuration`을 쓰고 있어 어서션 값 불변 = 전환 비용 0이 확인돼 1순위안 그대로 확정.
- **`_sendAccumulator` 승격(새 accumulator 신설 기각)** → 송신 throttle이 이미 50ms cadence였으므로, 별도 예측 accumulator를 만들면 두 박자가 어긋날 수 있음. 하나로 합쳐 {입력→Predict→송신→NotifySent} 4종 1:1 = 서버 "틱당 입력 1개 소비" 거울이 구조적으로 보장됨.
- **타이머 이원 박자** → 게이트 타이머(commit window/hit gate)는 source-gating 판단과 같은 substep 박자(어긋나면 한 substep 안에서 잠금 깜빡임), UI 쿨다운 4종은 frame dt(substep 0번 프레임에도 표시 갱신). substep 0번 프레임에 게이트 타이머가 안 줄어드는 건 잠금 *연장* 방향 = 안전 측(reviewer 확인).
- **cap 초과 = 통째 틱 드랍(backlog 보존 기각, 메인 검수 정정)** → 서버 입력 큐 `MaxInputQueue=6` drop-oldest 실측이 결정 근거. backlog 추격 버스트는 서버에서 어차피 버려지고 reconcile 대상만 늘림 — freeze 복구는 reconcile 담당으로 일원화.
- **로컬 보간 = prev/curr 2점(RemoteEntity식 버퍼 기각)** → 원격은 네트워크 지연 흡수 버퍼가 필요하지만 로컬 예측은 방금 만든 인접 두 점이면 충분. 단 보간 버퍼는 위치 점프 4경로(spawn/reconcile/맵전환/사망리스폰)에서 양쪽 리셋 — 텔레포트 가로질러 보간 금지.
- **SnapThreshold 축소 비동승(STOP 분리)** → 같은 파일을 만지지만 "고정스텝 효과 실측"이라는 선행 데이터에 의존하는 독립 결정이라 분리(plan-auditor 축1 판정 정합). `[Reconcile]` 로그 정량 후 영호 의논.

## 막혔던 지점

- **Unity MCP RunCommand 중첩 클래스 호이스팅 → CS1527** → 중첩 클래스가 namespace 레벨로 복제돼 멤버 중복. 콜백을 별도 최상위 internal 클래스로 분리해 해결(P2와 동일 함정 — 재확인).
- **EditMode 실행 = TestRunnerApi 콜백 + 콘솔 마커 폴링** → MCP에서 EditMode 결과를 직접 회수하기 어려워, TestRunnerApi 콜백이 콘솔에 박는 마커를 폴링해 passed/failed를 회수하는 패턴.
- **WSL2 서버 종료 self-match** → `pkill -f 'GameServer\.[d]ll'` 브래킷 트릭으로 pkill 자기 명령줄 매치 회피.

## 학습 일지 후보 키워드

- illegal state unrepresentable — 가변 dt 차단을 런타임 검증이 아니라 *시그니처 제거*(`Predict(dt)` → `Predict()`)로. 잘못된 dt가 들어갈 자리 자체를 없애면 방어 코드가 불요.
- Fix Your Timestep — 계산 박자(고정 서브스텝)와 그림 부드러움(시각 보간)은 분리. accumulator 루프로 서버 틱 거울 + prev/curr lerp로 프레임 Hz 렌더.
- 보간 버퍼 리셋 = 텔레포트 가로질러 보간 금지 — reconcile/spawn/맵전환/사망리스폰 4경로 전부 prev/curr 양쪽 리셋. 안 하면 순간이동 사이를 lerp해 미끄러지는 잔상.
- 클라 cap = 서버 큐 정책에 맞춤 — backlog 보존보다 드랍이 옳을 수 있음. 서버 `MaxInputQueue` drop-oldest면 추격 버스트는 어차피 버려지므로 reconcile만 늘림.
- Unity MCP RunCommand 중첩 클래스 CS1527 (콜백은 별도 internal) / EditMode = TestRunnerApi 콜백 + 콘솔 마커 폴링 / pkill -f 브래킷 self-match 회피(`GameServer\.[d]ll`).

---

## Commits (feature/m4.11-p4-fixedstep — 0774695 기반 새 분기)

```
e4063ba tune(client): Knight Dash 이펙트 재생 속도 1.0 → 1.8 (영호 Unity 직접 튜닝)
db941ee docs(plan): M4.11 P4 고정스텝 Phase 정의 박제 (plan-auditor 조건부GO — D2/D3 봉합)
0f5977c refactor(sync): M4.11 P4 로컬 예측 고정스텝 전환 — 50ms 서브스텝 + 시각 보간
```

코드 3파일(`0f5977c`): `PlayerPredictor.cs` 156줄(-4) / `LocalPlayerMovement.cs` 385줄(+16+α) / `PlayerPredictorTests.cs`. 번외 `e4063ba`(Knight Dash FX 속도 1→1.8, 영호 Unity 직접 튜닝 — P4와 무관해 분리 commit).

## 백로그 (후속)

- **SnapThreshold 1.5f 축소 결정** — Play 중 `[Reconcile]` 로그 카운트 정량 후 영호 의논(STOP 포인트①).
- **`reviewer.md` → `REVIEW_CHECKLIST.md` 경로 drift** — 실재는 `00_Document/`. (P2~P3 이월)
- **봇 임펄스 여유 계수 2.0f 재측정** — 미래 시나리오 확장 시. (P3 이월)
- **`98_Shared/CLAUDE.md:19` ProtocolVersion 주석 stale** — Current=8→12 정정(영호 확인 후).
- **디테일 폴리싱** — 영호 지목, 구체 항목은 후속 세션에서 정의.

## 다음 스텝

- **P5** (serverTick + 고정스텝 반영 클라 **재빌드** + 전체 회귀 — 테스트 / 봇 / 2클라 / 콘솔 0 error). 이 마일스톤의 마지막 Phase. 재빌드 후 기존 빌드 클라(현재 P3 구코드)도 P4 신코드로 통일된다.
