---
owner: youngho
milestone: M4.11
phase: milestone-closeout
title: 클라-서버 동기화 정돈 — 공유 시계 일치 + force-adopt 덤불 정리
status: done
completed: 2026-06-12
grade: 대규모
summary: M4.11 완전 마감 (5 Phase, P1~P3 = PR #100 머지[0774695] / P4~P5 = feature/m4.11-p4-fixedstep 브랜치 PR 대기). 발표 전 기반 정돈 시퀀스(M4.10→M4.11→M4.12)의 2번. 동기화를 **갈아엎지 않고 시계를 일치** — 도미노(뿌리=가변 dt, 도미노1=dead-zone/매직넘버, 도미노2=원격 보간 벽시계 재도장, 도미노3=시계 2원화)를 위험 오름차순 게이트식 5 Phase로 역순 해체. ① P1 원격 보간 serverTick 전환 + clock smoothing(RemoteEntity _renderTime 연속전진 + drift*0.1 흡수 + 0.5s snap) + 적 S_EntityState serverTick append → **ProtocolVersion 11→12**(마일스톤 유일 wire 변경) + snapshot 20Hz + facing 정돈 → 창드래그 desync(백로그#5) + stutter 봉합, ② P2 Constants.ExternalImpulseEpsilon=0.05f 공유 상수 신설(서버 클램프↔클라 force-adopt 게이트 결합 봉합, wire 무변경·동작 불변), ③ P3 회귀 그물 5건 = 불변식(Physics.Step golden/replay/봇 연속성/FacingSnap) vs baseline 이원 설계, ④ P4(심장부) 고정스텝 전환 = Predict(dt)→Predict() 가변 dt 물리 차단 + accumulator 고정 서브스텝 + 시각 보간, ⑤ P5 재빌드+전체 회귀(보통 — 본 문서 흡수). 검증: WSL2 561 + EditMode 119 + 봇 16(13 연속 + 3 fresh PASS) + BuildPlayer errors=0 7씬 + Unity 0err + reviewer 🔴0 + plan-auditor GO + 영호 2클라 최종 실측(양쪽 P4 신코드) 전부 이상 무. 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.11 — 클라-서버 동기화 정돈(sync) 마일스톤 박제

**마감 일자**: 2026-06-12
**Phase 수**: 5/5 완료 (P1 원격 보간 serverTick + facing / P2 force-adopt 공유 상수 / P3 회귀 안전망 이원 설계 / P4 고정스텝 전환[심장부] / P5 재빌드+전체 회귀)
**등급**: 대규모 (client+shared+qa 3도메인 관통 — 도미노 역순 해체, 단 P1만 wire 변경[v11→12])
**WORK-ID**: m4.11-sync
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제
**브랜치/PR**: P1~P3 = PR #100 머지(`0774695`) / P4~P5 = `feature/m4.11-p4-fixedstep` (PR 대기, 영호 GO 게이트)

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — 동기화를 **갈아엎지 않고 시계를 일치**시킨 마일스톤. ① **P1** 원격 보간 `serverTick` 시간축 전환 + clock smoothing(`RemoteEntity._renderTime` 연속 전진 + `CatchupRate 0.1`로 drift 흡수 + `ResyncThreshold 0.5` 갭 snap) + 적 `S_EntityState`에 serverTick append → **ProtocolVersion 11→12**(마일스톤 유일 wire 변경) + snapshot 20Hz(`SnapshotTickInterval 2→1`) + facing 정돈(서버 타겟 스냅 + 클라 우선순위 피격>공격>이동) → **창드래그 desync(백로그 #5) + RemotePlayer stutter 봉합**. ② **P2** `Constants.ExternalImpulseEpsilon=0.05f` 공유 상수 신설 — 서버 클램프(`CombatConstants.VelocityEpsilon`)↔클라 force-adopt 게이트(`0.0001f` 리터럴)의 숨은 결합을 한 상수로 봉합(wire 무변경·동작 불변). ③ **P3** 회귀 그물 5건 = **불변식(P4 후 절대 green: `Physics.Step` golden 궤적/replay 동치/봇 연속성/FacingSnap) vs baseline(`[P3 baseline` 마커 + 사유 기입 슬롯) 이원 설계**, computed-expectation으로 cross-runtime float 함정 회피. ④ **P4**(고위험 심장부) **고정스텝 전환** — `Predict(dt)`→`Predict()`(가변 dt 물리적 차단) + accumulator 고정 서브스텝 루프({입력→Predict→송신→NotifySent} 4종 1:1 = 서버 거울) + 시각 보간(prev/curr lerp + 위치 점프 4경로 버퍼 리셋) + `MaxPredictStep` 은퇴→`MaxSubstepsPerFrame=4` + 통째 틱 드랍. ⑤ **P5** 재빌드 + 전체 회귀(보통 등급 — -DONE 생략, 본 문서 흡수).
- 🤔 **왜 필요한가** — 발표 전 정돈 시퀀스(M4.10 기반 → M4.11 동기화 → M4.12 스킬 마감)의 2번. 동기화 토대(서버 권위·클라 예측·reconcile·보간)는 **건강했으나** 그 위에 누적된 패치들이 **덤불(thicket)**을 이뤘고, 근본 원인은 단 하나 — **"클라-서버 공유 시계가 없다."** 도미노를 끝까지 따라가면: **(뿌리)** 클라 가변 `Time.deltaTime` vs 서버 고정 50ms 틱(20 TPS) → 적분 박자 불일치 → 구조적 drift. **(도미노 1)** drift를 덮으려 `SnapThreshold`를 1.5f로 키워 1.5유닛 dead-zone(불감대) 생성, 그 위에 `serverVx==0.0001f` 가드가 서버 lunge 임펄스 `0.05f`에 묶임(한쪽 숫자 바꾸면 다른 쪽 조용히 깨짐). **(도미노 2, 가장 구체적)** `RemoteEntity.EnqueueSnapshot`이 serverTick을 버리고 `Time.realtimeSinceStartup` 벽시계로 재도장 → freeze(창 드래그) 후 밀린 스냅샷이 한 프레임에 몰려 같은 벽시계 값을 받음 → 옛 위치부터 재생 → **백로그 #5 범인**. **(도미노 3)** 로컬은 `ackedClientTick`(틱 시계), 원격은 벽시계 — 같은 화면 두 객체가 다른 시간 축에서 놂. 서버가 이미 serverTick을 실어 보내니 **클라가 버리지 않고 쓰면** 공유 시계가 서고, 그 위에서 예측을 고정스텝으로 바꾸면 dt-drift 뿌리가 뽑히고, drift가 사라지면 임계를 줄일 수 있고, 임계가 줄면 dead-zone·덤불이 해소된다 — 도미노를 **역순으로** 무너뜨림.
- 🛠️ **어떻게 만들었나** — 전 Phase 공통 = **위험 오름차순 게이트식**. 저위험(국소 봉합)부터 시작해, 안전망을 깐 뒤에야 심장부(로컬 예측)를 건드림. 핵심 기법: (a) **P3 이원 회귀 설계** — **불변식**(`Physics.Step` golden 궤적/replay 동치/봇 연속성/FacingSnap — P4 후 절대 green)과 **baseline**(`[P3 baseline` 마커 + 사유 기입 슬롯 — P4가 의도적으로 갱신)을 마커로 분리. computed-expectation(하드코딩 기대값이 아니라 런타임 계산)으로 cross-runtime float 함정 회피. (b) **illegal state unrepresentable**(P4 핵심) — 가변 dt 차단을 런타임 clamp(방어 코드)가 아니라 `Predict(dt)`→`Predict()` *시그니처 제거*로. 잘못된 dt가 들어갈 자리 자체를 없앰. (c) **서버 거울** — accumulator 고정 서브스텝 루프에서 {source-gating 입력 샘플링 → Predict → C_MoveIntent 송신 → NotifySent} 4종 세트를 1:1로 50ms마다 실행 = 서버 GameMap "틱당 입력 1개 소비 → Step 1회"의 정확한 거울 → 클라 N번째 Predict와 서버 N번째 Step이 같은 입력×같은 dt → drift 0. (d) **Fix Your Timestep** — 계산 박자(고정 서브스텝)와 그림 부드러움(시각 보간 prev/curr lerp)을 분리, 위치 점프 4경로(spawn/reconcile/맵전환/사망리스폰)에서 보간 버퍼 양쪽 리셋(텔레포트 가로질러 보간 금지). (e) **cap 정책 = 서버 큐에 맞춤** — `MaxPredictStep=0.1f` 은퇴 → `MaxSubstepsPerFrame=4` 클라 로컬 const, 초과분은 통째 틱 `%=` 드랍(메인 검수 정정 1건 — 서버 `MaxInputQueue=6` drop-oldest[`PlayerEntity.cs:23-27`]라 추격 버스트는 어차피 서버에서 버려지고 reconcile 대상만 늘림 → 스펙대로 드랍). (f) **러너 파일화** — wsl 중첩 따옴표 변수 전개 함정 회피를 위해 봇 회귀를 `99_Tools/run_bot_regression.sh`(16 연속) + `run_bot_fresh_recheck.sh`(fresh 단독 재검) 2벌 스크립트 파일로 박음.
- 🧪 **테스트 결과** — 전 Phase 검증 사슬: WSL2 `dotnet test` **561 passed / 0 failed**(불변식 全: golden 궤적/replay/봇 연속성/FacingSnap) / Unity EditMode **119 passed**(baseline 2건 사유 기입 후 갱신 — P3 이원 설계 작동 실증) / 헤드리스 봇 **16 시나리오 = 13 연속 PASS + 3 fresh PASS**(BossFight·HpSync = M4.10 전례의 연속실행 보스 상태 누적 한계, **Freeze = 동류 한계 신규 관측**[fresh PASS = 회귀 아님] — 정직 기록) / BuildPlayer **Succeeded errors=0** 7씬(`C:/Dev/Build/Client/03_Client.exe`, Managed DLL mtime 신선) / Unity 콘솔 **error 0** / 봇 M2BasicMovement desync **(0.00, 0.00)** / reviewer 6축 **🔴 0**(P4 🟡 1 = SnapThreshold 축소 판단용 실측 메모, 수정 불요) / plan-auditor(P4 **조건부GO**→D2/D3 봉합, P5 **GO**) / **영호 2클라 최종 실측**(양쪽 P4 신코드 — Editor + 새 빌드 exe) **전부 이상 무**(2026-06-12) — 창드래그 백로그 #5 봉합이 빌드 클라에서도 유지 / **ProtocolVersion 12 유지**(P1 bump 후 무손, PacketRoundTrip 안전망).
- ➡️ **다음 스텝** — ① **PR**(영호 GO 게이트 — P4~P5분, `feature/m4.11-p4-fixedstep`). irreversible 경로라 사용자 명시 GO 후만 진행. ② **M4.12-skill-finish**: 행동 입력 게이트 시스템화(클린코드 의무) + 쿨다운 UI + 발표 재빌드. ③ **백로그(이월)**: SnapThreshold 1.5f 축소(`[Reconcile]` 로그 정량 후 영호 의논 — STOP①) / `reviewer.md`→`REVIEW_CHECKLIST.md` 경로 drift(실재 `00_Document/`) / 봇 임펄스 여유 계수 2.0f 재측정 / `98_Shared/CLAUDE.md:19` ProtocolVersion 주석 Current=8→12 stale 정정(영호 확인 후) / 디테일 폴리싱(영호 지목 — 구체 항목 후속 정의).

---

## TL;DR

M4.11은 **"동기화를 갈아엎지 않고 *시계를 일치*시킨"** 마일스톤이다. 동기화 토대는 건강했고, 진짜 병은 그 위에 누적된 덤불 — 그 덤불의 근본 원인은 단 하나, **클라-서버 공유 시계의 부재**다. 클라는 가변 `Time.deltaTime`로 예측하는데 서버는 고정 50ms 틱으로 시뮬레이션하니 적분 박자가 달라 구조적 drift가 생기고, 그걸 덮으려 키운 `SnapThreshold` dead-zone 위에 force-adopt 매직넘버 결합과 원격 보간 벽시계 재도장이 잎사귀처럼 얹혔다.

**도미노 역순 해체**: 뿌리(가변 dt)를 직접 치지 않고, **위험 오름차순 게이트식**으로 잎사귀부터 무너뜨렸다 — P1 원격 보간 벽시계 제거(가장 구체적인 백로그 #5 봉합) → P2 force-adopt 매직넘버 결합 끊기 → P3 안전망(불변식 vs baseline 이원) → P4 심장부(고정스텝) → P5 재빌드. **그물 없이 심장부를 건드리면 reconcile 발산을 다시 부른다**는 게 게이트 순서의 이유다.

**P4 = 칼날**: `PlayerPredictor`/`LocalPlayerMovement`는 방금(M4.9) force-adopt·dash·reconcile을 봉합한 따끈한 심장부다. 가변 dt 차단을 런타임 방어 코드가 아니라 `Predict(dt)`→`Predict()` **시그니처 제거**(illegal state unrepresentable)로 했고, accumulator 고정 서브스텝 루프 {입력→Predict→송신→NotifySent} 4종 1:1로 **서버 "틱당 입력 1개 소비"를 정확히 거울**로 삼아 drift를 0으로 만들었다. 화면은 substep prev/curr 두 점을 lerp해 프레임 Hz로 부드럽게 — "Fix Your Timestep"의 정석(계산 박자와 그림 부드러움 분리).

**P3 이원 설계가 실증되다**: P4가 **baseline 2건만** 사유 기입 후 갱신하고 불변식은 전부 green을 유지 — "절대 깨지면 안 되는 것"과 "의도적으로 바뀔 것"을 마커로 분리한 이원 설계가 정확히 의도대로 작동했다(STOP 3건도 전부 미발동 = P4가 자율 결정 안 함).

---

## AC 검증 결과

| 마일스톤 게이트 | 검증 | 결과 |
|---|---|---|
| 백로그 #5(창드래그 desync) 봉합 | P1 2클라 실측 + P5 빌드 클라 재확인 | ✅ serverTick 시간축 + clock smoothing — 빌드 클라에서도 유지 |
| RemotePlayer stutter/떨림/facing | P1 2클라 실측 | ✅ broadcast 20Hz + 서버 vx facing + 타겟 스냅 + 우선순위 거울 |
| force-adopt 매직넘버 결합 봉합 | P2 양쪽 빌드 + 동작 불변 | ✅ `Constants.ExternalImpulseEpsilon=0.05f` 단일 상수, wire 무변경 |
| 회귀 안전망(불변식 vs baseline 이원) | P3 설계 + P4 작동 실증 | ✅ baseline 2건만 갱신, 불변식 전부 green |
| 로컬 예측 고정스텝 전환 | P4 — Predict(dt) 제거 + 서브스텝 루프 | ✅ 가변 dt 물리 차단, 봇 desync (0.00, 0.00) |
| WSL2 서버 테스트 green | WSL2 `dotnet test --no-build` | ✅ **561 passed / 0 failed**(불변식 全) |
| Unity EditMode green | TestRunnerApi 콜백 폴링 | ✅ **119 passed**(baseline 2건 갱신 포함) |
| 봇 전 시나리오 회귀 | 16 연속 + fresh 재검 | ✅ 13 연속 PASS + 3 fresh PASS(BossFight/HpSync 기존 한계 + Freeze 신규 동류 한계 — 정직 기록) |
| BuildPlayer Succeeded errors=0 | Unity MCP BuildPlayer | ✅ 7씬, `03_Client.exe`, DLL mtime 신선 |
| Unity 콘솔 error 0 | MCP ReadConsole Error 필터 | ✅ 0건 |
| reviewer 통과 | P4 6축 | ✅ 🔴 0 / 🟡 1(SnapThreshold 실측 메모) |
| plan-auditor 통과 | P4/P5 사전 검증 | ✅ P4 조건부GO→D2/D3 봉합 / P5 GO |
| 영호 2클라 최종 실측 | 양쪽 P4 신코드 | ✅ 전부 이상 무(6항목 거동 + 백로그 #5 봉합 유지) |
| ProtocolVersion | P1 PDL append + RoundTrip assert | ✅ **11 → 12**(P1 유일 wire 변경, 이후 12 유지) |
| DONE 박제 | Phase별 -DONE(P1~P4 4벌) + 본 문서 + HTML | ✅ P5는 보통 등급 — 본 문서 흡수 |

---

## Phase 박제 요약

| Phase | 제목 | 위험 | 핵심 | 경로 |
|---|---|---|---|---|
| P1 | 원격 보간 serverTick + facing 정돈 | 저위험 | 벽시계 재도장 → `serverTick` 시간축 + clock smoothing(`_renderTime` 연속전진 + drift×0.1 + 0.5s snap). 적 `S_EntityState` serverTick append → **v11→12**. snapshot 20Hz. facing 서버 타겟 스냅 + 우선순위. 백로그 #5 + stutter 봉합 | PR #100 (`0774695`) |
| P2 | force-adopt 덤불 정리 | 저위험 | `Constants.ExternalImpulseEpsilon=0.05f` 공유 상수 — 서버 클램프(`CombatConstants.VelocityEpsilon`)↔클라 force-adopt 게이트(`0.0001f` 리터럴) 숨은 결합 봉합. wire 무변경·동작 불변 | PR #100 (`0774695`) |
| P3 | reconcile/보간 회귀 안전망 | 중위험 | 회귀 5건 — **불변식(`Physics.Step` golden/replay/봇 연속성/FacingSnap) vs baseline(`[P3 baseline` 마커) 이원 설계**. computed-expectation(cross-runtime float 함정 회피). WSL2 561/EditMode 119 기준점 + 2클라 "P4 전 거동" 박제 | PR #100 (`0774695`) |
| P4 ★ | 로컬 예측 고정스텝 전환 (심장부) | **고위험** | `Predict(dt)`→`Predict()`(가변 dt 물리 차단). accumulator 고정 서브스텝 {입력→Predict→송신→NotifySent} 4종 1:1 = 서버 거울. 시각 보간(prev/curr lerp + 점프 4경로 버퍼 리셋). 점프 latch substep 소비. `MaxPredictStep` 은퇴→`MaxSubstepsPerFrame=4` + 통째 틱 드랍. baseline 2건 갱신(이원 설계 실증). STOP 3건 미발동 | `0f5977c` |
| P5 | 재빌드 + 전체 회귀 | 중위험(보통) | BuildPlayer Succeeded errors=0 7씬 + 봇 16(13 연속 + 3 fresh PASS, Freeze 신규 한계 정직 기록) + WSL2 561 재실행 + Unity 0err + 영호 2클라 최종 실측(양쪽 P4 신코드) 이상 무. 러너 2벌 신설. -DONE 생략(본 문서 흡수) | `dcb3fd8` + `65a6a97` |

**P4~P5 commit 사슬**(`feature/m4.11-p4-fixedstep`, 0774695 기반): `e4063ba`(번외 Knight Dash FX 속도 1→1.8) → `db941ee`(P4 정의 + D2/D3 봉합) → `0f5977c`(P4 코드 3파일) → `ebfbfdc`(P4 -DONE) → `dcb3fd8`(P5 정의) → `65a6a97`(봇 러너 2벌).

---

## 결정 흐름 (회고 참고용)

1. **도미노 역순 해체 = 위험 오름차순 게이트** (마일스톤 골격) — 뿌리(가변 dt)를 직접 안 치고, 잎사귀(원격 보간 벽시계)부터 시작해 안전망을 깐 뒤에야 심장부(고정스텝)를 마지막에. P4는 안전망(P3) 통과 후에만 착수 — 그물 없이 심장부를 건드리면 reconcile 발산 재발.
2. **적 보간도 같이 고침 = v11→12 bump** (P1, 영호 GO) — `RemoteEntity`가 플레이어/적 공용이라 공용 시그니처를 바꾸면 적 경로가 깨짐. 적만 옛 시간축으로 두면 반쪽 봉합 + 컴파일 에러. append-only + PacketRoundTrip 안전망으로 v12 안전. *마일스톤 유일 wire 변경.*
3. **stutter 봉합 = clock smoothing(벽시계 복귀 기각)** (P1) — 옛 벽시계는 부드러웠지만 freeze 뭉침이 백로그 #5 원인. serverTick 유지하되 재생 시점만 연속 시계(`_renderTime` 연속 전진 + drift catch-up + 큰 갭 snap) = 두 마리 토끼.
4. **facing = 서버 타겟 스냅(클라만 기각)** (P1, 영호 선택) — 클라만 고치면 Local `FaceToward(타겟)`와 서버 broadcast(이동 방향)가 계속 어긋남. 서버가 타겟 방향으로 스냅해야 3자(Local·서버·Remote) 일치. trade-off: 이동 반대편 적 칠 때 lunge가 적 쪽으로(의도된 "몬스터 따라가기").
5. **force-adopt 결합 = 공유 상수로 봉합** (P2) — `serverVx==0.0001f` 클라 게이트가 서버 lunge `0.05f`에 암묵 결합. `Constants.ExternalImpulseEpsilon=0.05f` 단일 상수로 양쪽이 같은 출처를 보게 — wire 무변경·동작 불변. §4 양쪽 빌드 확인.
6. **불변식 vs baseline 이원 회귀 설계** (P3, P4 실증) — 절대 깨지면 안 되는 것(`Physics.Step` golden/replay/봇 연속성/FacingSnap)과 의도적으로 바뀔 것(`[P3 baseline` 마커)을 분리. P4가 baseline 2건만 사유 기입 후 갱신 = 설계 의도대로 작동 실증. computed-expectation으로 cross-runtime float 함정 회피.
7. **Predict 시그니처 dt 제거(런타임 clamp 기각)** (P4) — 가변 dt 차단을 방어 코드가 아니라 *시그니처 제거*로(illegal state unrepresentable). 기존 테스트 호출부가 전부 `dt=TickDuration`을 써서 어서션 값 불변 = 전환 비용 0 확인 후 1순위안 확정.
8. **cap 초과 = 통째 틱 드랍(backlog 보존 기각, 메인 검수 정정)** (P4) — 서버 입력 큐 `MaxInputQueue=6` drop-oldest 실측이 근거. backlog 추격 버스트는 서버에서 어차피 버려지고 reconcile 대상만 늘림 — freeze 복구는 reconcile 담당으로 일원화.
9. **봇 연속실행 한계 = 정직 기록** (P5) — Freeze가 BossFight·HpSync에 이어 동류 "연속실행 보스 상태 누적" 한계로 신규 관측됨. fresh 단독 PASS = 회귀 아님을 확인하고, 회피하지 않고 **신규 관측을 그대로 박음**(정직 기록 — M4.10 전례 패턴 확장).

---

## 학습 일지 후보 키워드

- **도미노 역순 해체(위험 오름차순 게이트식)**: 뿌리를 직접 안 치고 잎사귀부터, 안전망을 깐 뒤 심장부를 마지막에. 그물 없이 심장부를 건드리면 reconcile 발산 재발.
- **불변식 vs baseline 이원 회귀 설계**: 절대 깨지면 안 되는 것과 의도적으로 바뀔 것을 마커(`[P3 baseline`)로 분리. P4가 baseline만 갱신 = 설계 의도대로 작동 실증.
- **illegal state unrepresentable**: 가변 dt 차단을 런타임 검증이 아니라 시그니처 제거(`Predict(dt)`→`Predict()`)로. 잘못된 상태가 들어갈 자리 자체를 없애면 방어 코드가 불요.
- **Fix Your Timestep**: 계산 박자(고정 서브스텝)와 그림 부드러움(시각 보간 prev/curr lerp)은 분리. accumulator 루프로 서버 틱 거울 + lerp로 프레임 Hz 렌더.
- **clock smoothing**: serverTick 시간축 유지 + 재생 시점만 연속 시계(drift catch-up vs snap-on-discontinuity). 벽시계 복귀하면 백로그 #5 재발.
- **보간 버퍼 리셋 = 텔레포트 가로질러 보간 금지**: spawn/reconcile/맵전환/사망리스폰 4경로 전부 prev/curr 양쪽 리셋. 안 하면 순간이동 사이를 lerp해 미끄러지는 잔상.
- **클라 cap = 서버 큐 정책에 맞춤**: backlog 보존보다 드랍이 옳을 수 있음. 서버 `MaxInputQueue` drop-oldest면 추격 버스트는 어차피 버려지므로 reconcile만 늘림.
- **연속 봇 실행 = 몬스터 상태 누적 한계(fresh 재검 패턴)**: BossFight·HpSync에 이어 Freeze도 동류 한계 신규 관측. fresh 단독 PASS면 회귀 아님 — 정직 기록.
- **Unity MCP RunCommand 중첩 클래스 CS1527**: 콜백을 별도 최상위 internal 클래스로 분리. **wsl 변수 전개 함정 = 스크립트 파일 실행식**: 중첩 따옴표 변수 전개가 깨지므로 봇 러너를 `99_Tools/*.sh` 파일로 박아 실행.

---

## 헌법 정합

- **§1 (서버 권위)**: 고정스텝은 예측 박자만 변경 — reconcile 시 서버 진실 우위는 불변(클라는 여전히 단순 렌더러 + 입력 전달자). facing도 서버 타겟 스냅으로 *서버 권위 강화*.
- **§2 (프로토콜 신성)**: P1에서 `S_EntityState` serverTick append(v11→12, 영호 GO 거침) 후 무손 — PacketRoundTrip 안전망 + append-only 규율. P2 이후는 v12 유지.
- **§4 (공유 코드 규율)**: P2 `Constants.ExternalImpulseEpsilon` 공유 상수 양쪽(server+client) 빌드 확인.
- **§5 (틱 루프 블로킹 금지)**: 서버는 P4~P5에서 무변경(고정스텝은 클라 예측 박자 변경). P1 broadcast 20Hz 부하는 틱예산 p99 ~1ms 실측으로 정합 확인.

---

## pull 시 영향 (팀 안내용)

① **ProtocolVersion 12 불변** — 이번 PR분(P4~P5)은 wire 무변경(P1의 v11→12는 이미 PR #100으로 main에 머지됨). P4~P5만 받는다면 Shared.dll 재빌드 불필요.
② **클라 로컬 이동 체감** = 고정스텝 + 시각 보간으로 변경(거동 동일 실측 완료 — 영호 2클라 6항목 이상 무). 가변 dt 예측이 50ms 고정 서브스텝 + prev/curr lerp 렌더로 바뀜.
③ **봇 회귀**는 `99_Tools/run_bot_regression.sh`(16 연속) 표준 러너 사용. 연속실행 한계 2~3건(BossFight/HpSync/Freeze)은 `run_bot_fresh_recheck.sh`로 fresh 단독 재검.
