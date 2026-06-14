---
owner: youngho
milestone: M4.14
phase: _milestone (아키텍처 검토 후속)
title: M4.14 마일스톤 마감 — IGameAction 계약 통일 + Convention 자동강제 (순수 내부 품질)
status: done
completed: 2026-06-14
grade: 복잡
summary: M4.14 완전 마감. 2026-06-13 영호↔Codex 아키텍처 cross-review의 메인 ReadOnly 실측 검증 결과를 코드 반영 — 순수 내부 품질(유저 비가시), wire v13 불변·거동 불변. ① IGameAction 계약 통일(P02, 복잡·trust-boundary·Opus): Melee만 Execute() return false+ExecuteWithTarget 우회 일탈 → ActionContext(readonly struct {ClientTick,TargetEntityId,Facing}) 도입 → 단일 Execute(in ctx) + ActionGate 단일 TryPerform(구체 MeleeAction 비의존). Validate() git diff 0. 실측 보정=계획 2필드→3(Dash facing 권위). ② Convention 자동강제(P03 measure→P04 apply): analyzer 실측 중괄호 always production 168(Codex 288·검토 "~90" 둘 다 빗나감) → 영호 정책재고 → when_multiline 채택(한 줄 가드절 허용, multi-line 무중괄호만 강제, prod 15) → 15곳 수정 + EnforceCodeStyleInBuild + IDE0011 빌드강제(Tests/99_Tools none) + CODE_CONVENTION §4 v6.2. casing/Allman 위반 0. ③ 01b: CommitWindowTests flaky 근본수정(AttackState flyweight Pending* 공유 mutable race 제거). ④ P05(선택, client): LocalPlayerMovement 타이머 7개 → plain C# PlayerAbilityTimers 추출(§3.1) + 25 EditMode. 최종 회귀 WSL2 568/0/5 + EditMode 147/0/0 + reviewer P02 🔴0. commit 8건 미push(영호 GO 대기). 시각판 = _milestone-DONE.html.
---

# M4.14 마일스톤 마감: 아키텍처 검토 후속

**기간/구성**: 2026-06-14 단일 세션. Phase 01·01b·02·03·04·05·06. commit 8건(`71e7237`/`228b82a`/`4322677`/`b261864`/`e3b92a1`/`2e65619`/`e91897a` + knowledge cherry-pick `c6b3395`), 브랜치 `feature/m4.14-architecture-followup` **미push (PR = 영호 GO 대기)**.
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 5단계 보고 + Phase 타임라인 + KPI (복잡 등급 HTML 박제, ADR-031)
**근거**: `_architecture-review-2026-06-13.md`(Codex cross-review 실측검증) + `_milestone-plan.md`(6 Phase, plan-auditor GO). P02 상세 = `02-igameaction-contract-DONE.{md,html}`.

## 5단계 보고

- 🎯 **무엇을 만들었나** — 아키텍처 검토 발견(① IGameAction Melee 일탈 ② 미뤄둔 §4 중괄호 강제)을 코드에 반영한 순수 내부 품질 마일스톤. 새 기능 0, wire v13 불변, 거동 불변. + 부수입(01b flaky 근본수정, P05 client 타이머 추출).
- 🤔 **왜 필요한가** — ① `MeleeAction`만 계약 `Execute()`를 `return false`로 빠져나가 우회 = LSP/OCP 잠재 트랩(새 호출자 silent fail + 게이트의 구체 의존). ② Codex 정적 카운트가 체계적 과대 → "추정 말고 결정적 도구로 카운트"를 카운트 자신이 증명.
- 🛠️ **어떻게 만들었나** — P02: `ActionContext` 도입으로 단일 `Execute(in ctx)` 계약 + `ActionGate` 단일 `TryPerform`. P03: analyzer report로 중괄호 168/casing·Allman 0 실측. P04: 영호 정책재고로 `when_multiline` 채택(15건만) + 빌드 강제. 01b: flyweight 무상태 복원. P05: 타이머 plain C# 추출. 상세 = 박제 사실.
- 🧪 **테스트 결과** — WSL2 server build 0/0 + test 568/0/5(불변) + Unity EditMode 147/0/0(122+신규 25) + `Validate()` git diff 0(trust-boundary) + IDE0011 production 잔존 0 + reviewer P02 🔴0. 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — PR(영호 GO, irreversible) → main. 이월: ADR-029 rsync config 동기 봉합 후보 / M4.12 #104 Unity 육안확인(영호).

## TL;DR (🎯 무엇 / 🤔 왜)

M4.14는 **유저에게 안 보이는 부채 정리** 마일스톤이다. 2026-06-13 영호↔Codex 아키텍처 리뷰를 메인 세션이 ReadOnly로 전수 실측 검증한 뒤, 두 갈래를 코드에 박았다:

1. **`IGameAction` 계약 통일** — 전략 패턴(4행동)에서 Melee만 인터페이스를 일탈(죽은 `Execute()` + 우회 `ExecuteWithTarget`). `targetEntityId`가 평타에만 있는 입력이라 공통 시그니처에 자리가 없던 게 뿌리. `ActionContext`(값 객체)로 per-action 입력을 묶어 단일 계약 복원. trust-boundary(`ActionGate.Validate()`)는 git diff 0으로 보존.
2. **Convention 자동강제** — "M4.4+로 미뤄둔" §4 중괄호를 analyzer로 강제. 핵심은 **추정이 아니라 결정적 카운트**: 중괄호 always가 production 168건(Codex 288·검토 "~90" 둘 다 틀림), casing/Allman은 이미 0. 영호가 churn 대비 가치를 재고해 `when_multiline`(한 줄 가드절은 살리고 multi-line 무중괄호 = goto-fail류 위험만 강제, 15건)을 채택.

곁가지로 P01 baseline 중 발견한 flaky를 근본수정(01b: flyweight 공유 mutable 제거)했고, P05에서 client 타이머를 테스트 가능한 plain C#로 추출했다.

## 박제 사실 (🛠️ 어떻게)

| Phase | 등급 | 산출 | commit |
|---|---|---|---|
| 01 + 01b | 보통 | baseline(WSL2 568/0/5·EditMode 122) + CODE_CONVENTION 줄수 정정 + `PlayerCombatStates.Attack` flyweight `Pending*` 제거(병렬 race 근본수정, HitState 패턴 통일) | `71e7237`·`228b82a` |
| 02 | 복잡·tb | `ActionContext.cs` 신규 + `IGameAction.Execute(in ctx)` + `MeleeAction` 죽은코드 삭제·`ExecuteWithTarget` 흡수 + Dash/Teleport/Thunderbolt plumbing + `ActionGate` 단일 `TryPerform`(`Validate()` verbatim) + CombatSystem/SkillSystem 호출부 | `4322677`·`b261864` |
| 03 + 04 | 보통 | analyzer 실측(중괄호 168/casing·Allman 0) → `when_multiline` 채택 → 15곳 중괄호 + `EnforceCodeStyleInBuild=true` + `.editorconfig`(IDE0011 when_multiline, Tests/99_Tools none) + CODE_CONVENTION §4 v6.2 | `e3b92a1`·`2e65619` |
| 05 | 보통·client | `PlayerAbilityTimers.cs` 신규(타이머 7 + TickFrame/TickSubstep + On*/getter) + `LocalPlayerMovement` 위임 + 25 EditMode | `e91897a` |
| wire | — | **v13 불변** — PDL 0, ProtocolVersion bump 0. P02=server 내부, P04 production 중괄호, P05 client만. 98_Shared 소스 변경(MapDataFile 중괄호)은 Shared.dll 재빌드만(거동 동일) | — |

## AC 검증 결과

- **서버(WSL2, ADR-029)**: 통합 최종 상태 build 0경고/0오류 + `dotnet test` **568/0/5**(P01 baseline 불변). IDE0011 production 잔존 0 + 빌드 노이즈 0(EnforceCodeStyleInBuild이 다른 IDE 룰 안 깨움).
- **클라(Unity EditMode)**: 컴파일 0err + **147/0/0**(baseline 122 + 신규 PlayerAbilityTimers 25, 기존 무회귀). `MovementGateTests`(LocalPlayerMovement) green = P05 거동 보존.
- **trust-boundary(P02)**: `ActionGate.Validate()` 4단계 검증 본문 `git diff` 라인 0(메인 + reviewer 독립 2회). reviewer Tier 2-A 헌법/ADR/거동 위반 0.
- **wire**: PDL 변경 0, ProtocolVersion v13 그대로. Shared.dll co-review = dll commit 포함 시만(현재 source-only).

## 결정 흐름 (회고 참고용)

- **P02 ActionContext 필드 = 3 (계획 2 보정)** — Dash facing 권위(클라 화면 방향 채택)가 통합 `TryPerform` 시그니처에 facing 자리를 강제. Facing은 speculative hook이 아니라 패킷 구체 입력(target과 동급)이라 §0.3 정합. 채택 로직은 trust-boundary 권위라 게이트 유지(액션 분산 X).
- **P04 중괄호 = when_multiline (always 기각)** — 영호 정책재고. always는 production 168 churn 대비 가치 낮음(한 줄 가드절은 goto-fail 함정 자체가 불가능). when_multiline이 위험 케이스(15)만 잡고 가독성 보존. 결정 근거 = 추정 아닌 analyzer 실측(추정 288·"~90" 모두 빗나감을 측정으로 교정).
- **P05 = 지금 진행 (미루기/skip 대신)** — 타이머가 순수 감쇠 로직이라 EditMode 테스트 가치 real(§3.1). 영호 GO.

## 막혔던 지점 / 이월 (➡️ 다음)

- **P05 Unity 재컴파일 헤맴(가장 큰 시간 소모)** — Worker가 unknown SkillId sentinel로 `(SkillId)999`를 썼는데 SkillId는 byte enum이라 byte 초과 = CS0221 컴파일 에러 → 테스트 어셈블리 빌드 실패 → Unity가 직전 DLL(stale, 신규 파일 없음) 유지. MCP `ReadConsole "error CS"`는 deferred(백그라운드) 컴파일이라 못 잡았고, **DLL mtime이 .cs보다 오래된 것**이 결정 단서였다. 영호 Test Runner Run All이 에러를 표면화 → `(SkillId)99`로 수정 후 147 green.
- **이월**: ① ADR-029 rsync에 `Directory.Build.props`+루트 `.editorconfig` 추가(WSL↔CI analyzer drift 봉합, 영호 승인 대기) ② M4.12 #104 정유현 NPC Village Props Unity 육안확인(영호 직접).
- **PR**: feature/m4.14 → main, knowledge cherry-pick `c6b3395` + M4.14 8 commit 포함. irreversible = 영호 명시 GO 후.

## 학습 일지 후보 키워드

계약 일탈(인터페이스 `return false`+우회 오버로드 = LSP/OCP 잠재 트랩) / per-action 입력을 값 컨텍스트로 묶기 / trust-boundary 입력 채택은 권위 게이트 책임 / 정적 분석 추정 ≠ 결론(결정적 analyzer 카운트로 교정 — 288·"~90" 둘 다 빗나감) / when_multiline = goto-fail 위험만(한 줄 가드절 가독성 보존) / flyweight 무상태(공유 mutable 파라미터 채널 = 병렬 race) / §3.1 MonoBehaviour에서 순수 로직 추출 = EditMode 테스트 / **Unity 컴파일 에러 진단 = DLL mtime이 결정 단서**(MCP ReadConsole는 deferred 컴파일 못 잡음, editor-driven Run All이 표면화) / byte enum 캐스팅 overflow(CS0221) / ADR-029 rsync config 미동기 → WSL analyzer drift

## 다음 마일스톤

- **PR + 영호 가닥** — M4.14 마감 PR(영호 GO) 후 다음 마일스톤은 영호 결정(순서·타이밍). 후보: M5 Persistence(LocalDB Linux 결정 선행) / 외관·연출 / future SOLID 리팩토링 울트라코드(M4.9 후 예정, memory `future-solid-refactor-ultracode`).
