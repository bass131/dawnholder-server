---
owner: youngho
milestone: M4.13
phase: 01-action-input-gate
title: 행동 입력 게이트 시스템화 — ActionGate 단일입구 + 풀 전략 패턴 + 클라 거울
status: done
completed: 2026-06-13
grade: 대규모
summary: M4.13 Phase 01 완료. 행동(평타/스킬) 진입을 서버 단일 입구 ActionGate(풀 전략 패턴)가 분기 없이 검사(①상태 허용 ②쿨다운 ③클래스 ④rewind) + 클라가 IsActionLocked로 헛입력 송신 차단. ActionKind enum(98_Shared) + IGameAction 4구현(Process* 1:1 이관) + ActionRegistry + ActorState.AcceptsAction + 쿨다운 ms→tick 통일 + LungeDecay를 AttackState.Enter 소유로 이관(0.85 잔류 버그 구조적 봉합). 클라 = IsMovementLocked 재사용(행동잠금=이동잠금 동일조건, 서버 교차검증 입증) + OnAttack/TrySendSkill 게이트. wire v12 무변경. 검증 = 서버 reviewer 🔴0 + WSL2 561/0 + KnightDashTests(Dash중 평타거부 증명), 클라 reviewer 🔴0 + 컴파일0 + EditMode 123/0 + client빌드 Succeeded + 2-client Play 통과(영호 실측). dll 제외(클라 ActionKind 미사용). Dash중 ghost는 P2 이월. 시각판 = 01-action-input-gate-DONE.html.
---

# Phase 01 박제: 행동 입력 게이트 시스템화

**소요**: P1-server(`11b5baf`, server Worker) → reviewer 🔴0 → WSL2 561/0 → P1-client(`b2a7677`, client Worker) → reviewer 🔴0 → 컴파일/EditMode/빌드/2-client Play. 확정설계 docs `3e9b8de`.
**시각 보고서**: [`01-action-input-gate-DONE.html`](01-action-input-gate-DONE.html) — ActionGate 4단계 흐름 + KPI (대규모 5단계 보고 HTML 박제)

## 5단계 보고

- 🎯 **무엇을 만들었나** — "지금 이 상태에서 이 행동을 받아도 되는가"의 단일 입구 `ActionGate`(풀 전략 패턴, 분기 0) + 클라 거울(`IsActionLocked`). 새 행동 = 구현 1개 + 레지스트리 1줄(OCP). 상세 = TL;DR.
- 🤔 **왜 필요한가** — 입력 차단이 세 곳(이동잠금/쿨다운/클래스)에 분산 → Dash commit window 중 평타 진입 → `LungeDecayPerTick 0.85` 잔류 버그. 호출자가 상태 내부값을 찔러 넣는 구조가 원인. 임펄스 재설계(P2~P6)의 서버 토대.
- 🛠️ **어떻게 만들었나** — 서버 = `ActionKind`/`IGameAction`+4구현/`ActionRegistry`/`ActionGate`/`ActorState.AcceptsAction` + 쿨다운 ms→tick 통일 + `LungeDecay` 상태소유. 클라 = `IsMovementLocked` 재사용 + 송신 게이트 2곳. 상세 = 박제 사실.
- 🧪 **테스트 결과** — 서버 reviewer 🔴0 + WSL2 561/0 + `KnightDashTests`(Dash중 평타거부 증명). 클라 reviewer 🔴0 + 컴파일0 + EditMode 123/0 + client빌드 Succeeded + 2-client Play 통과. 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — P2 서버 임펄스 모델 통일(dash/knockback/lunge `ExternalVelX` 일관화, 모멘텀감속 → 고정거리 등속). P1 "상태 데이터 소유" 위에 올라감.

## TL;DR (🎯 무엇 / 🤔 왜)

특정 동작 State가 끝나야 추가 입력을 받는 예외가 **기술마다 재발**했고, 입력 차단 장치가 **세 곳에 분산**(이동 잠금 / 쿨다운 / 클래스 게이트)돼 있었다. 그 틈으로 **Dash commit window 중 평타 진입** → `Attack→Attack` self-transition no-op(Exit 미실행) → Dash 전용값 `LungeDecayPerTick 0.85` 잔류 버그. 봉합이 "호출자가 평타 진입마다 기본값을 직접 세팅"이라 — **호출자가 상태 내부 파라미터를 찔러 넣는 구조 자체가 원인**.

해결 핵심 셋:
1. **단일 입구 + 풀 전략 패턴 (OCP)** — `ActionGate`가 분기 0으로 ①상태 ②쿨다운 ③클래스 ④rewind 검사. 행동은 다형성 객체(`IGameAction` + 4구현, Flyweight). 새 행동 = 구현 1개 + 레지스트리 1줄.
2. **상태가 자기 데이터 소유** — `LungeDecayPerTick`/`AttackLungeVx`를 `AttackState.Enter`가 세팅(호출자 직접 세팅 제거 = `0.85` 사고 구조적 봉합). **P2 임펄스 통일의 전제.**
3. **클라 거울 = 헛입력 차단 UX** — `IsActionLocked`(서버 `AcceptsAction` 거울). 서버 권위는 그대로(우회해도 서버가 막음, 헌법 #1).

## 박제 사실 (🛠️ 어떻게)

| 영역 | 산출 |
|---|---|
| P1-server `11b5baf` | `ActionKind` enum(98_Shared) + `ActionKindExtensions.FromSkillId` / `IGameAction` + Melee/Dash/Teleport/Thunderbolt 구현(기존 `Process*` 본체 1:1 이관, 거동 보존, Flyweight 정적 인스턴스) / `ActionRegistry`(Dictionary 단일 진실) / `ActionGate` 단일입구 분기0 / `ActorState.AcceptsAction`(베이스 `=>true`, Attack/Hit/Death override `=>false`) / 쿨다운 ms→tick 통일(`MeleeCooldownTicks=10`, 20 TPS 동등) / `LungeDecayPerTick`·`AttackLungeVx`를 `AttackState.Enter` 소유로 이관 |
| P1-client `b2a7677` | `LocalPlayerMovement.IsActionLocked` 프로퍼티(기존 `IsMovementLocked` 1줄 위임 — 행동잠금=이동잠금 동일조건) / `LocalPlayerInput.OnAttack`:91 + `TrySendSkill`:138 송신 게이트에 행동잠금 차단. dll 제외(클라 ActionKind 미사용) |
| trust-boundary | 핸들러(network thread)=빠른 거부+cheat-flag 1차 layer 유지 / `ActionGate`(tick thread)=상태·쿨다운·클래스·rewind 최종 권위. 클래스 검증 양쪽이면 defense-in-depth(다른 layer라 정당) |
| wire | **v12 무변경** — `ActionKind`/`IGameAction`/`Registry`/`ActionGate` 전부 서버 내부 + shared enum(직렬화 안 됨). `C_SkillUse` 여전히 skillId byte |

## AC 검증 결과

- **서버**: WSL2(ADR-029) build 0경고/0오류 + `dotnet test` **561/0** 비감소. `KnightDashTests` — Dash commit window 중 평타 거부 직접 증명(기존 허용되던 구멍 봉합). reviewer 통합 리뷰 🔴0 🟡3(namespace b 되돌림 + DRY ActionKindExtensions 단일화 봉합 완료 / Flyweight pending 필드 후속).
- **클라**: Unity 컴파일 0에러 + EditMode **123/0**(1.8s) + client 빌드 Succeeded(errors0, 7씬, `C:\Dev\Build\Client\03_Client.exe`). reviewer 🔴0 🟡1(wiring EditMode 한계 수용 — 서버 교차검증으로 "행동잠금=이동잠금 동일조건"=Attack/Hit/Death 셋 일치 입증, 헌법 #1 권위 유지). 송신 경로 전수(C_Attack/C_SkillUse/C_MoveIntent/C_EnterPortal) 게이트 누락 0.
- **통합**: 2-client Play 통과 — 평타 commit window 중 스킬/평타 헛입력이 서버로 송신 안 됨(Editor 콘솔 `[Skill]→` 로그 미출력, 영호 실측 OK).

## 결정 흐름 (회고 참고용)

- **B full 통합 + 풀 전략 패턴 vs 데이터 Registry** — 풀 전략 채택(영호). "미래 행동 多 예정 → 확장성 우선." 단순 분기/record보다 다형성 객체가 OCP 강함.
- **클라 `CanSubmitAction` 결합 순수함수 추출 기각** — CODE_CONVENTION §0.3 YAGNI + §2.5 우연한 중복(잠금=상태머신 기반/쿨다운=시간 기반, 변경 이유 다름). `IsActionLocked` 인스턴스 프로퍼티가 기존 `OnGround` 패턴과 동형 + `IsMovementLocked` 21케이스 재사용(새 테스트 부재 정당).
- **(2) ghost 진단 정정** — compact 전 "클라가 serverTick으로 commit window 끝 추정 필요 → P5 예측과 닿음"은 클라 코드 실측으로 **틀림**. 이동 입력은 이미 로컬 타이머로 ghost 없이 잠금 중(M4.11 인프라). 진짜 갭은 *행동 입력* 통합 게이트 부재였고 `IsMovementLocked` 재사용으로 P5와 무관하게 닫힘.
- **dll 제외** — 클라 ActionKind 미사용 → Shared.dll commit 불필요. 소스-dll sync는 P1 PR 때 재빌드 + co-review.

## 막혔던 지점 / 이월 (➡️ 다음)

- **Dash 중 ghost = P2 이월** — `NotifyDash`는 로컬 commit window 타이머를 안 세팅(Dash는 force-adopt 경로 흡수). Dash 중 행동 차단은 서버 AnimState=Attack 도착까지 lag만큼 ghost. Dash 지속(고정거리 등속)이 P2 임펄스 모델에서 재정의되므로 한 묶음(영호 확정 — P1에서 미접촉).
- **working tree dll 2개 미commit** — `03_Client/Assets/Plugins/{Shared,ClientNet}.dll`. P1 PR 시 98_Shared 최신 재빌드로 sync(`03_Client/Plugins/*.dll` 경로 = 정유현 co-review).
- **Flyweight pending 필드** — reviewer 🟡 후속(서버). 다음 손볼 때.

## 학습 일지 후보 키워드

단일 입구 게이트(분산 차단 → 시스템화) / 풀 전략 패턴 OCP(새 행동 = 1클래스 + 1줄) / 상태가 자기 데이터 소유(호출자 찔러넣기 = 사고 뿌리) / 클라 게이트 = 권위 아니라 최적화(우회해도 안전한가? Yes면 정당) / 행동잠금=이동잠금 동일조건(서버 정책 거울, 우연 아님) / 박제·추천 전 file:line 실측(ghost 진단 정정) / YAGNI 추출 기각(우연한 중복 §2.5)

## 다음 Phase

- **P2 — 서버 임펄스 모델 통일** (`02-server-impulse-model.md`, 복잡·trust-boundary). dash/knockback/lunge `ExternalVelX` 일관화 + 대쉬 모멘텀 감속(매틱 ×0.85) → 고정거리 등속. P1 "상태 데이터 소유" 구조 위. 단방향 P1→P2→P4→P5→P6, P3는 P2 위.
