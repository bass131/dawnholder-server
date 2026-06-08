---
owner: youngho
milestone: M4.6
phase: milestone-closeout
title: ActionState FSM — 플레이어·몬스터·보스 행동을 통일 State 패턴으로 마감
status: done
completed: 2026-06-08
grade: 대규모
summary: M4.6 완전 마감 (6 Phase + 보스 이동 동승, PR #75~#81 + 본 마감 PR). 플레이어·몬스터·보스의 서버 권위 행동을 하나의 제네릭 State 패턴(ActorState<TActor>/StateMachine<TActor>)으로 통일 — 옛 누더기(플레이어 latch 카운터 / 몬스터 enum+switch / 보스 필드 분기)를 셋 다 같은 베이스 위 상태별 클래스로 이주. 핵심 게임플레이 규칙 = 공격 중 이동 잠금(commit window)을 서버 권위로 신설하고 클라 예측이 같은 98_Shared 상수로 따라가는 거울로 정합(source-gating → rubber-band 0). 종착점 C(완전 통일) + 길 A(수직 슬라이스)로 설익은 추상화 회피 — 플레이어로 베이스 실증 후 몬스터/보스로 일반화. Play 피드백으로 적 HitState(넉백)·선공후공·보스 이동/탐지까지 유기적 확장(보스 = 고정 포탑 → 살아있는 보스). 행동 비트 보존(전환 틱에 이동까지)이 desync 0의 열쇠. ProtocolVersion 9 불변(신규 패킷 0) — wire State 고정 + animState 1byte로 보스 이동까지 클라 0줄. 최종 회귀 = 클린빌드 0/0 + test 471/0/4 + 봇 6종 PASS + cross-review 🔴0/🟡1. 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.6 — ActionState FSM 통일 마일스톤 박제

**마감 일자**: 2026-06-08 (세션27, Phase 06 회귀 + 마감)
**Phase 수**: 6/6 완료 (01~05 개별 PR + 보스 이동 05b 동승 + 06 본 마감 PR)
**등급**: 대규모 (server 3축 관통 + 신뢰 경계 이동 게이트 + 클라 미러 + unity-asset 깃발)
**WORK-ID**: m4.6-action-state-fsm
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — 플레이어·몬스터·보스 **세 행위자의 서버 권위 행동을 하나의 제네릭 State 패턴**(`ActorState<TActor>` + `StateMachine<TActor>`)으로 통일. 옛 누더기(플레이어 latch 카운터 / 몬스터 enum+switch 2상태 / 보스 필드 분기)를 셋 다 같은 베이스 위 상태별 클래스로 이주. 핵심 게임플레이 규칙 = **공격 중 이동 잠금(commit window)**을 서버에 신설하고 클라 예측이 같은 98_Shared 상수로 따라가는 거울로 정합. Play 피드백으로 적 HitState(넉백)·선공/후공(AggroOnSight)·보스 이동/탐지까지 유기적 확장(보스 = 고정 포탑 → 살아있는 보스).
- 🤔 **왜 필요한가** — 실측 결과 공격 중 이동이 `SubmitMoveIntent()` 0줄 체크로 가능했고(commit window 부재), 적 AI는 2상태 enum+switch, 보스는 필드 분기라 셋이 제각각이었다. 새 행동(방어/구르기/보스 다중 패턴) 추가마다 세 곳을 다르게 만져야 했고, "공격은 끝까지 커밋" 같은 전투 감각의 기본 규칙이 없었다. 통일 베이스 = 앞으로 행동 추가가 **상태 클래스 1개**로 떨어지는 토대 + M5(영속화) 전 전투 계약 확정.
- 🛠️ **어떻게 만들었나** — 종착점은 C(완전 통일), 길은 A(수직 슬라이스): 추상 베이스를 셋 동시에 추측하지 않고 플레이어로 실증(01 골격 → 02 전투+commit window) → 클라 거울(03) → **검증된 베이스**를 몬스터(04)·보스(05)로 일반화 → 보스 이동(05b) → 회귀+마감(06). 핵심 선택은 본문 "결정 흐름" 6건 — 특히 행동 비트 보존(전환 틱에 이동까지 = desync 0), commit window의 98_Shared 단일 상수 + 클라 source-gating, 보스 wire State 고정으로 v9 불변.
- 🧪 **테스트 결과** — 최종 회귀(세션27, WSL2 = ADR-029): 클린빌드 `--no-incremental` **0 경고/0 오류** + `dotnet test` **471 통과/0 실패/4 skip** + 헤드리스 봇 **6종 전부 PASS**(smoke / M2 desync 0.00 / MultiRoster / EnemyAi 골렘·슬라임 FSM / EmergencyCombat rate-limit / BossFight 탐지·공격·처치) + `ProtocolVersion == 9` 불변(신규 패킷 0) + **cross-review 🔴0 / 🟡1**(`ActionFsm↔Fsm` 네이밍 이월). 보스 이동+공격 facing+strike 동기는 05b에서 Play 직접 봉합, 직업 2종×3씬 풀 매트릭스는 봇+보스 Play 커버리지로 **스킵 결정**(사용자).
- ➡️ **다음 스텝** — 통일 베이스가 굳었으니 신규 행동(방어/구르기/보스 다중 패턴)이 상태 클래스 추가로 떨어진다. 이월: `ActionFsm↔Fsm` 네이밍 통일(차기) / 미스 시 허공 찌르기(서버 miss 신호 = v10 후보) / v10 구조급(플레이어 HP 동기화 + 공격 이벤트 패킷) / 외관·연출 마일스톤 또는 M5 Persistence는 사용자 가닥. 상세는 본문 "이월 명시".

---

## TL;DR (🎯 무엇 / 🤔 왜)

M4.6은 게임의 **세 행위자(플레이어·몬스터·보스) 행동을 하나의 구조로 통일**하고, **"공격은 끝까지 커밋"**이라는 전투 기본 규칙을 서버 권위로 도입한 마일스톤이다.

**통일 State 패턴**: 옛 코드는 플레이어 전투가 latch 카운터 누더기, 몬스터 AI가 enum+switch 2상태, 보스가 필드 조건분기로 셋이 제각각이었다. 이제 셋 다 제네릭 `StateMachine<TActor>` + `ActorState<TActor>` 베이스 위 상태별 클래스(Flyweight static 인스턴스 — 헌법 #5 zero-alloc)로 돈다. 새 행동을 추가하려면 상태 클래스 1개만 더하면 된다.

**commit window (서버 권위)**: 옛 `SubmitMoveIntent()`는 공격 중인지 0줄도 안 봤다 — 공격하면서 걸을 수 있었다. 이제 공격(AttackState)·피격(HitState) 중에는 서버가 이동을 잠근다(헌법 #1). 클라 예측은 같은 98_Shared 상수로 이동을 게이트하고, 입력이 Predict/송신/replay로 갈라지기 전 **한 곳에서(source-gating)** 0으로 만들어 reconcile rubber-band가 0이다.

**비트 보존으로 desync 0**: enum+switch → State 이주의 함정은 "전환만, 이동은 다음 틱"이라는 순수 FSM 직관이다. 옛 코드는 Patrol→Chase로 바뀌는 그 틱에 이미 Chase를 한 발 갔는데, 순수 FSM이면 추격이 영구히 1틱 늦어 trajectory가 어긋나고 desync가 0이 아니게 된다. 각 `State.Tick`이 "전환 결정 → 결정된 상태 이동까지 그 자리"로 옛 비트를 그대로 보존했다.

**프로토콜 규율**: ProtocolVersion은 M4.6 전체에서 **9 불변**(신규 패킷 0). commit window는 서버 내부 규칙 + 기존 reconcile로 전달, 보스 이동조차 wire `State`를 Idle로 고정하고 걷는 시각은 `animState=Walk` 한 byte로만 실어 클라 0줄·v9를 지켰다.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 머지 |
|---|---|---|---|
| 01 | State 머신 골격 + 플레이어 이동 | 서버 행동 프레임워크(`ActorState`/`StateMachine`) 신설 + 플레이어 Idle/Move/Jump 이주, **행동 비트 불변** 입증 | PR #75 |
| 02 ★ | 플레이어 전투 + commit window | 누더기 latch 카운터 → 단일 `ActionFsm` 상태 스왑 + **서버 권위 commit window**(공격 중 이동 잠금) + HitState 잠금/넉백. v9 불변 [trust-boundary] | PR #76 |
| 03 | 클라 미러 정합 | 서버 이동 잠금(Attack/Hit)을 클라 예측이 같은 98_Shared 상수로 거울 → **rubber-band 0**. source-gating으로 Predict/송신/replay 일관, 넉백 force-adopt [unity-asset] | PR #77 |
| — | (동승) 직업명 통일 | Warrior→Knight, Ranger→Mage 전면 통일(wire-safe — 직렬화 영향 0). 03↔04 사이 가벼운 리네임 | PR #78 |
| 04 | 몬스터 AI State 이주 | enum+switch Patrol/Chase → 공유 제네릭 `State<TActor>` 이주(비트 보존 = desync 0) + 적 HitState(넉백, 신규) + 선공/후공(AggroOnSight) + 클라 거울 [trust-boundary] | PR #79 |
| 05 | 보스 3-State 정리 | 필드 분기(IsPhase2/Telegraph/Cooldown) → 명시적 3-State(Idle/Telegraph/Attack), 옛 동작과 비트 동일(회귀 0). telegraph 예고 상수(P1=16/P2=10) 98_Shared 단일화 [trust-boundary] | PR #80 |
| 05b | 보스 이동/탐지 구동 | 고정 포탑(blind-timer) → 이동/탐지 4-State(+BossMoveState). 몬스터 부품(MoveChase/MovePatrol/FindClosestInAggro) 재사용. **wire State=Idle 고정**로 클라 0줄·v9 불변. Play 봉합 2건(facing + strike 동기) | PR #81 |
| 06 | 회귀 + 마감 | 마일스톤 전체 회귀(빌드/test/봇/v9/cross-review) + 본 박제 + PR | 본 마감 PR |

**Phase 06 포함분 (세션27)**:
- **마일스톤 전체 회귀 입증** — 통일 State(플레이어/몬스터/보스 = `StateMachine<TActor>`)가 셋 다 한 베이스 위에서 돌고, 옛 enum+switch/필드 분기 잔재가 0임을 cross-review(reviewer SubAgent)로 확인. 봇 6종이 FSM 구동 경로(EnemyAi 골렘·슬라임 / BossFight 탐지·공격·처치)를 전수 PASS.
- **봇 교차오염 캐비앗 박제** — 전투 시나리오(EnemyAi + EmergencyCombat)가 같은 서버의 몬스터 풀을 공유하면 오염(EnemyAi가 슬라임 약화 → EmergencyCombat burst가 즉사시켜 false fail). 봉합 = 전투/보스 봇은 **fresh 서버 단독** 실행. 보스 시나리오는 무리스폰 설계라 서버당 1회.

---

## 결정 흐름 (🛠️ 어떻게 — 회고 참고용)

1. **종착점 C + 길 A (수직 슬라이스)** — 종착 그림은 셋 다 통일된 State지만 *구현*은 플레이어 → 몬스터 → 보스 순차. 추상 베이스를 셋 동시에 추측하면 **설익은 추상화**(아직 안 본 케이스에 맞춘 잘못된 일반화)가 된다. 플레이어 02에서 `StateMachine<TActor>`를 실증해 굳힌 뒤 몬스터(04)·보스(05)가 *검증된* 베이스를 재사용 — 추측 0.
2. **행동 비트 보존 = desync 0의 열쇠** — 옛 코드는 *전환되는 그 틱에 새 상태 이동까지* 수행(Patrol→Chase 전환 틱에 이미 Chase 한 발). 순수 FSM처럼 "전환만, 이동은 다음 틱"이면 추격이 영구 1틱 지연 → trajectory 어긋남 → desync≠0. 각 `State.Tick`이 "전환 결정 → 결정된 상태 이동까지 그 자리"로 비트 보존. (이주 회귀 테스트의 진짜 관문이 이 한 틱이었다.)
3. **commit window = 서버 진실 + 98_Shared 단일 상수** — "Exit Time"(공격 모션이 끝나야 다음 입력)의 진짜 정체는 클라 Animator 설정이 아니라 게임플레이 규칙(헌법 #1)이다. 서버가 진실의 원천이고 클라 Exit Time은 시각 거울. 클라 예측이 같은 98_Shared 상수로 이동을 게이트하고 **source-gating**(입력이 Predict/송신/replay로 갈라지기 전 한 곳에서 0)해야 셋이 정의상 일치 → rubber-band 0. (rubber-band 근본원인 = 세 입력 경로의 미세 불일치.)
4. **제네릭 `StateMachine<TActor>` Flyweight** — 상태 인스턴스를 static 단일 공유(상태는 무상태 = 행위자를 인자로 받아 동작)로 두어 틱마다 할당 0(헌법 #5 zero-alloc). 플레이어/몬스터/보스가 같은 베이스 타입을 `TActor`만 바꿔 공유.
5. **보스는 Fsm 통일하되 wire State=Idle 고정** — 보스 이동(05b) 중에도 `enemy.State` enum은 건드리지 않음 → 신규 enum값 0 → v9 불변. 걷는 시각은 `animState=Walk` 한 byte로만(이미 쏘는 `S_EntityState`에 실림) → 클라 0줄. "보스가 움직이면 v10급"이라던 초기 추정을 탐색으로 뒤집어 순수 서버 작업으로 확정.
6. **회귀 0 리팩토링 vs 신규 행동 Phase 분리** — 순수 구조 이주로 시작했으나 적 HitState(넉백)·선공후공·보스 이동이 Play 피드백으로 유기적으로 자랐다. "회귀 0"(05 보스 정리)과 "신규 행동"(05b 보스 이동)을 별도 Phase/PR로 갈라 history를 깨끗하게 유지 — 회귀0 단위와 신규행동 단위가 섞이면 "이 PR이 회귀를 안 냈나"의 검증 스토리가 흐려진다.

---

## AC 검증 결과

마일스톤 완료 조건 대조 (2026-06-08 세션27, WSL2 = ADR-029 표준 경로):

- [x] 플레이어·몬스터·보스 행동이 **공통 제네릭 State 베이스 + 상태별 클래스**로 구동 — cross-review(reviewer)로 옛 enum+switch/필드 분기 잔재 0 확인
- [x] **공격 중 이동 잠금**이 서버에서 강제 — 02에서 도입, 조작된 이동 입력으로 우회 불가(commit window 테스트 입증)
- [x] commit window 지속이 **98_Shared 단일 상수** — 클라 예측이 같은 값으로 게이트 → 공격 시 rubber-band 0 ([Reconcile] count=0 실증, 03)
- [x] 몬스터 AI(Patrol/Chase)가 동일 State 베이스 위 동작 — aggro 히스테리시스/순찰 경계 회귀 없음, **M2 desync 0.00**
- [x] 보스가 명시적 State로 동작 — 페이즈 1/2 + telegraph + 쿨다운 회귀 없음(05), P2 telegraph 단일 출처. + 이동/탐지로 살아있는 보스(05b)
- [x] AnimState(시각) ↔ EnemyState(AI) 경계 유지 — 클라는 여전히 AI 상태 모름(`animState`만 사용)
- [x] **`ProtocolVersion.Current == 9`** 불변 — 신규 패킷 0 (본 마감 브랜치 98_Shared Protocol diff 0)
- [x] `dotnet test` green — 클린빌드 0/0 + **471 통과/0 실패/4 skip**
- [x] 봇 전 시나리오 PASS — **6종**(smoke / M2 desync 0.00 / MultiRoster / EnemyAi 골렘·슬라임 FSM / EmergencyCombat rate-limit / BossFight 탐지·공격·처치). 캐비앗: 전투/보스 봇은 fresh 서버 단독(교차오염 회피), 보스 시나리오 서버당 1회
- [x] cross-review — reviewer 🔴0 / 🟡1(`ActionFsm↔Fsm` 네이밍, 비차단 이월)
- [~] Play 실측 — 직업 2종 × 3씬 풀 매트릭스는 **커버리지로 스킵**(사용자 결정). 봇 6종(FSM 구동 전수) + 05b 보스 Play 봉합(이동/탐지/공격 facing/strike 동기 직접 확인)으로 행동 입증
- [ ] CHANGELOG entry + PR 생성·머지 — **사용자 명시 GO 게이트** (본 박제 commit 후 진행)
- [x] work-pin 갱신 — 본 마감 흐름에서 지속 갱신

---

## 이월 명시 (➡️ 다음)

- **네이밍 통일 (차기 마일스톤)**: 플레이어 `ActionFsm` ↔ 몬스터/보스 `Fsm` — 구조는 동일, 이름만 갈림. cross-review 🟡(비차단)로 박힘
- **공격 스윙/피격판정 분리 (별도 phase/마일스톤)**: 미스 시 허공 찌르기 — 현재 타겟 근접 필수라 빗나가도 찌르기 모션이 안 남. 서버 miss 신호 필요(= v10 후보). de-aggro MovePatrol snap 미세 보정 동승
- **v10 구조급 (별도 묶음)**: 플레이어 HP 동기화 전용 패킷 + 원격 공격 이벤트 패킷 — 원격 Mage 투사체 표시의 뿌리. 본 마일스톤 v9 불변 유지로 이월
- **신규 행동 (통일 베이스 위)**: 방어/구르기 등 새 상태 — 베이스가 굳었으니 상태 클래스 추가로 떨어짐
- **다음 마일스톤 가닥 (사용자)**: 외관·연출(배경/컷신/NPC) 또는 v10 구조급 또는 M5 Persistence(LocalDB Linux 결정 + GenPackets Write 풀링 + Serilog/DI)
- **harness 봉합 후보**: `.claude/agents/REVIEW_CHECKLIST.md`가 실재하지 않는데 `reviewer.md`가 4곳 참조(false-promise) — 마일스톤과 무관, 별도 봉합

---

## 학습 일지 후보 키워드

제네릭 `StateMachine<TActor>` 단일 베이스(플레이어/몬스터/보스 3행위자) / 행동 비트 보존 = desync 0 열쇠(전환 틱에 이동까지, 순수 FSM 1틱 지연 함정) / commit window = 서버 진실 + 98_Shared 단일 상수 + 클라 source-gating(rubber-band 근본원인 = 세 입력 미세 불일치) / 수직 슬라이스(C 목표 + A 길)로 설익은 추상화 회피(플레이어 실증 후 일반화) / wire State 고정 + animState 1byte로 v9 불변 유지(신규 enum 회피 = "보스 이동 v10급" 추정 반증) / 회귀0 리팩토링 vs 신규행동 Phase 분리(검증 스토리 깨끗) / Flyweight static state zero-alloc(헌법 #5) / 클라 봉합 보수적 예측 방향(먼저 멈춤이면 틀려도 rubber-band 0) / 봇 교차오염(전투 시나리오 같은 몬스터풀 공유 → fresh 서버 단독)
