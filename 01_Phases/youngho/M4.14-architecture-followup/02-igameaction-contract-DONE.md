---
owner: youngho
milestone: M4.14
phase: 02-igameaction-contract
title: IGameAction 계약 통일 — ActionContext 도입, ExecuteWithTarget/return false 제거
status: done
completed: 2026-06-14
grade: 복잡
summary: M4.14 Phase 02 완료. IGameAction 계약에서 Melee만 return false(죽은 코드)+ExecuteWithTarget 우회로 일탈하던 LSP/OCP 약화를 ActionContext(readonly struct {long ClientTick; int TargetEntityId; sbyte Facing}) 도입으로 봉합. 단일 Execute(map, caster, in ActionContext) 계약 + ActionGate가 TryPerformMelee/TryPerformSkill 2개 → 단일 TryPerform(분기 0, 구체 MeleeAction 비의존). 순수 구조 리팩토링 — 거동·wire format 1:1 보존. 실측 발견: 계획서 2필드(ClientTick+TargetEntityId)였으나 Dash facing 권위 로직이 통합 시그니처에 facing 자리 강제 → Facing 3번째 필드 추가(speculative 아닌 구체 입력, §0.3 정합). facing 채택은 게이트 유지(trust-boundary 권위는 게이트 책임). 검증 = Validate() git diff 라인 0(메인+reviewer 독립 2회) + WSL2 build 0/0 + test 568/0/5(baseline 동일) + 98_Shared/Shared.dll drift 0(server-only→EditMode 불변) + ExecuteWithTarget grep 0 + reviewer Tier 2-A PASS(🔴0). wire v13 무변경. server Worker Opus(복잡+trust-boundary). 시각판 = 02-igameaction-contract-DONE.html.
---

# Phase 02 박제: IGameAction 계약 통일

**소요**: 메인 file:line 재실측(01b 후 Actions/Systems 불변 확인 + 설계 결정 Facing 필드) → server Worker(Opus, 8 modified + ActionContext.cs) → 메인 Validate() git diff 0 + diff 거동 대조 → WSL2 build 0/0 + test 568/0/5 → reviewer Tier 2-A PASS → 박제. **미push (영호 GO 대기)**.
**시각 보고서**: [`02-igameaction-contract-DONE.html`](02-igameaction-contract-DONE.html) — 계약 통일 before/after + 게이트 KPI (복잡 등급 HTML 박제, ADR-031)

## 5단계 보고

- 🎯 **무엇을 만들었나** — `IGameAction.Execute`의 단일 계약 복원. Melee만 `return false`(죽은 코드)로 계약을 빠져나가 `ExecuteWithTarget`로 우회하던 구멍을 `ActionContext`(타겟·rewind·facing을 담는 readonly struct)로 메워, 4개 행동 전부 `Execute(map, caster, in ActionContext)` 하나로 통일. `ActionGate`도 진입 메서드 2개 → 1개(`TryPerform`).
- 🤔 **왜 필요한가** — `C_Attack`만 `targetEntityId`를 싣고 3개 스킬은 공간질의로 타겟을 찾아서, 공통 시그니처에 target 자리가 없어 Melee만 우회 경로가 생김. 현재 동작은 정상(Melee는 절대 죽은 `Execute()`를 안 통과)이지만 **잠재 트랩** — 새 호출자가 `Execute()`를 믿으면 조용히 실패(`return false`). 게이트가 구체 `MeleeAction.Instance.ExecuteWithTarget`을 직접 아는 OCP 훼손도 동반.
- 🛠️ **어떻게 만들었나** — `ActionContext.cs`(신규) + `IGameAction.Execute` 시그니처 `in ActionContext` + `MeleeAction`의 죽은 `Execute` 삭제·`ExecuteWithTarget` 본체 흡수(`targetEntityId`→`ctx.TargetEntityId`) + Dash/Teleport/Thunderbolt 시그니처 plumbing(`clientTick`→`ctx.ClientTick`) + `ActionGate` 단일 `TryPerform`(분기 0, 다형 `action.Execute`) + 호출부 2곳(`CombatSystem`/`SkillSystem`)이 `new ActionContext(...)` 구성. 상세 = 박제 사실.
- 🧪 **테스트 결과** — `Validate()`(trust-boundary 4단계 검증) 본문 `git diff` **라인 0**(메인 + reviewer 독립 2회). WSL2 build 0경고/0오류 + `dotnet test` **568/0/5**(Phase 01 baseline 동일, 비감소). 98_Shared/Shared.dll drift 0 → server-only → EditMode 122 논리적 불변. `ExecuteWithTarget`/`TryPerformMelee`/`TryPerformSkill` grep 잔존 0. reviewer Tier 2-A 🔴0. 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — Phase 03 Convention analyzer report-only(`.editorconfig` severity=suggestion으로 진짜 카운트 산출 → 영호 승인 게이트). 02∥03 도메인 다름. P02는 P03~04와 독립(부채 정리 두 갈래).

## TL;DR (🎯 무엇 / 🤔 왜)

전략 패턴(`IGameAction` + 4구현, M4.13 Phase 01 박힘)에서 **Melee만 계약을 일탈**했다. 인터페이스는 `bool Execute(map, caster, long clientTick)`인데 `MeleeAction.Execute()`는 `return false`(자백 주석 "직접 호출 경로 없음")로 죽어 있고, 실제 실행은 계약 밖 오버로드 `ExecuteWithTarget(..., int targetEntityId, ...)`로 우회. 근본 원인은 **`targetEntityId`가 평타에만 있는 입력**이라 공통 시그니처에 자리가 없던 것 — 그래서 Melee만 샛길.

이게 활성 버그는 아니다(`ActionGate.TryPerformMelee`가 항상 `ExecuteWithTarget`로 보내 죽은 `Execute()`는 안 닿음). 하지만 **LSP/OCP 약화 + 잠재 트랩**: ① 새 코드가 다형 `Execute()`를 믿고 Melee를 호출하면 조용히 `false`(silent fail) ② `ActionGate`가 구체 `MeleeAction.Instance`를 직접 알아야 함(추상 위로 새는 의존).

해결 = **per-action 입력을 컨텍스트 1개로 묶기**:
1. **`ActionContext` (readonly struct)** — `ClientTick`(rewind, 공통) + `TargetEntityId`(평타 전용) + `Facing`(Dash 방향 권위). 패킷에서 추출된 입력 힌트를 담는 값 객체. `in` 전달 = 틱 루프 힙 alloc 0(헌법 #5).
2. **단일 계약** — `Execute(map, caster, in ActionContext)` 하나로 4행동 통일. 죽은 코드 0, `ExecuteWithTarget` 소멸.
3. **게이트 단일화** — `TryPerformMelee` + `TryPerformSkill` → `TryPerform(map, player, kind, in ctx)`. 구체 `MeleeAction` 비의존(다형 `action.Execute`). **표면이 줄었다**(진입 2 + 죽은 Execute → 진입 1 + 죽은 코드 0).

## 박제 사실 (🛠️ 어떻게)

| 영역 | 산출 |
|---|---|
| 신규 `Actions/ActionContext.cs` | `internal readonly struct ActionContext { long ClientTick; int TargetEntityId; sbyte Facing; }` + 3-arg ctor. `in` 전달 전용 값 객체. |
| `Actions/IGameAction.cs` | `Execute` 시그니처 → `bool Execute(GameMap, PlayerEntity caster, in ActionContext ctx)`. 주석 무변경. |
| `Actions/MeleeAction.cs` | 죽은 `Execute`(`return false`) **삭제** + `ExecuteWithTarget` 본체를 `public bool Execute(..., in ActionContext ctx)`로 흡수. `targetEntityId`→`ctx.TargetEntityId`(2곳: `GetEnemyById`, `S_PlayerAttack.targetEntityId` 필드), `clientTick`→`ctx.ClientTick`(1곳). 자백 주석 제거. `Instance` static 유지(Registry용). |
| `Actions/{Dash,Teleport,Thunderbolt}Action.cs` | 시그니처 `in ActionContext ctx`. Dash/Thunderbolt `GetPositionAtTick(clientTick)`→`ctx.ClientTick`. Teleport 본체 무변경(clientTick 미사용). |
| `Systems/ActionGate.cs` | `TryPerformMelee`+`TryPerformSkill` → 단일 `TryPerform(map, player, kind, in ctx)`. `Validate(map, player, action, ctx.ClientTick, out ...)` 호출(인자만 ctx 경유). Dash facing 권위 게이트 유지(`if (kind==Dash) player.FacingDir = ctx.Facing`, 사유 주석 4줄 보존). 구체 `MeleeAction.Instance` 의존 제거 → 다형 `action.Execute(map, player, in ctx)`. |
| `Systems/CombatSystem.cs` | `using ...Maps.Actions` 추가 + `_gate.TryPerform(map, attacker, ActionKind.Melee, new ActionContext(attackerClientTick, targetEntityId, 0))`. facing=0 = Melee 미사용 sentinel. |
| `Systems/SkillSystem.cs` | `_gate.TryPerform(map, caster, kind.Value, new ActionContext(attackerClientTick, -1, facing))`. TargetEntityId=-1 = 스킬 무타겟 sentinel. |
| trust-boundary | `ActionGate.Validate()` 4단계(상태·쿨다운·클래스·rewind) 본문 verbatim. 시그니처(`long clientTick`)·부등호·경계·순서 무변경. git diff 라인 0. |
| wire | **v13 무변경** — `IGameAction`/`ActionContext`/`ActionGate` 전부 02_Server 내부(98_Shared 아님). PDL 0 변경. `S_PlayerAttack.targetEntityId` 필드값 1:1 동일. Shared.dll co-review 미트리거. |

## 실측 발견 — Facing 필드 (계획서 보정)

계획서는 `ActionContext`를 **2필드**(`ClientTick` + `TargetEntityId`)로 적었으나, 메인 실측에서 **`ActionGate`의 Dash facing 권위 로직**(`TryPerformSkill`의 `sbyte facing` → `player.FacingDir`, M4.13 v13 dash-facing-client-authority)이 통합 `TryPerform` 시그니처에 facing 입력 자리를 요구함을 발견. 계획서가 이 갈래를 빠뜨림.

**판단**: `Facing`은 추상화가 아니라 패킷에서 오는 **구체 입력 필드**(target과 동급) → `ActionContext` 3번째 필드가 정합(§0.3 위반 아님 — speculative hook이 아니라 실제 소비되는 데이터). facing **채택 로직은 게이트에 유지**(trust-boundary 권위 검증은 "서버 행동 단일 입구"인 게이트 책임, 액션으로 밀어넣지 않음). 거동 100% 보존 + `Validate()` git diff 0 유지. 승인된 설계(ActionContext 도입) 안의 강제 보정이라 ADR-031대로 자동 진행 + 본 문서 명시.

## AC 검증 결과

- **trust-boundary 기계 검증**: `git --no-pager diff ActionGate.cs` — `Validate()` 메서드 hunk **0개**(diff가 `// 4단계 검증` 주석 위에서 종료). 메인 + reviewer 독립 2회 확인. 검증 4단계 부등호(`<`)·경계·순서 verbatim.
- **WSL2 회귀**(ADR-029 rsync `~/dawnholder-poc` → build → test --no-build): build 0경고/0오류 + `dotnet test` **Failed 0, Passed 568, Skipped 5, Total 573** — Phase 01 baseline 568/0/5 정확히 동일. `CombatSwingTests`/`KnightDashTests`/`MageTeleportTests`/`ThunderboltSkillTests`/`ClassSkillGateTests`/`ValidateRewindTests` 전부 green.
- **server-only 범위**: `git status` = 02_Server 8 modified + ActionContext.cs untracked. **98_Shared 0 / 03_Client Shared.dll drift 0** → Unity 클라 무변경 → EditMode 122 논리적 불변(별도 실행 불요 — server 내부 계약, Shared.dll 미포함). 01b의 Shared.dll 비결정 재복사 사고 재발 0(Worker가 Windows 빌드 안 함, WSL2 격리만).
- **죽은 코드 0**: `grep ExecuteWithTarget|TryPerformMelee|TryPerformSkill 02_Server/*.cs` = 0건. `MeleeAction.Instance` = ActionRegistry 등록 + static 선언 2건만(ActionGate에서 소멸).
- **reviewer Tier 2-A**: 헌법 hard 위반 0 / ADR 위반 0 / 거동 비보존 0. sentinel 안전성 독립 검증(`GetEnemyById(-1)`→항상 null, 음수 entityId 부재 fail-safe). OCP 개선·Flyweight 무상태·§0.3 정합 확인. PASS.

## 결정 흐름 (회고 참고용)

- **안 a (ActionContext) vs b(인터페이스 분리) vs c(현상유지)** — a 채택(검토 §2.5 + 계획서). 타겟 싣는 행동이 1개(Melee)뿐인데 인터페이스를 가르면 타입↑ = 과한 추상화(§0.3). 값 객체 1개가 최소 표면.
- **Facing 필드 배치 — 게이트 vs DashAction** — 게이트 유지. facing 채택은 "클라가 신고한 방향을 권위로 삼는" trust-boundary 입력 검증 → 권위 단일 입구(게이트)에 두는 게 원칙. 액션으로 밀면 trust-boundary 로직이 분산. 거동·`Validate` diff 0은 둘 다 동일하지만 게이트 유지가 churn↓ + 책임 정합.
- **sentinel 값(facing=0, target=-1)** — 각 행동이 안 읽는 필드(Melee는 Facing 무시, Skill은 TargetEntityId 무시)라 거동 영향 0. reviewer가 `GetEnemyById(-1)`→null fail-safe까지 독립 확인.

## 막혔던 지점 / 이월 (➡️ 다음)

- **막힘 0** — 01b 후 Actions/Systems 불변이라 재실측 전제 전부 유효, Facing 발견 외 surprise 없음.
- **EditMode 미실행 = 의도적** — server-only(Shared.dll drift 0)라 Unity 클라가 보는 변화 0 → 논리적 불변. 영호 육안/Play 검증 불요(유저 비가시 내부 리팩토링).

## 학습 일지 후보 키워드

계약 일탈(인터페이스 메서드가 `return false`로 죽고 우회 오버로드 = LSP/OCP 약화 잠재 트랩) / per-action 입력을 값 컨텍스트로 묶기(공통 시그니처에 자리 없는 입력 = 일탈의 뿌리) / `readonly struct` + `in` = 틱 루프 alloc 0(방어 복사까지 제거) / trust-boundary 입력 채택은 권위 게이트 책임(액션 분산 X) / 계획서 file:line 보정(2필드→3, Facing 강제 — 실측이 plan 갱신) / git diff 라인 0 = 사람 눈보다 결정적인 trust-boundary 게이트 / server-only = Shared.dll drift 0이면 EditMode 논리적 불변

## 다음 Phase

- **Phase 03 — Convention analyzer report-only** (`03-convention-report.md`, 보통·cross Worker). 루트 `.editorconfig` 중괄호(IDE0011/SA1503)·casing(IDE1006) `severity=suggestion`으로 켜 **진짜 카운트** 산출(Codex 추정 288/57 → 실측 교체) → 영호 **승인 게이트**(통과 전 Phase 04 apply 금지). self-bias 회피 = analyzer 결정적 도구 카운트. 02∥03 도메인 다름(병렬 가능).
