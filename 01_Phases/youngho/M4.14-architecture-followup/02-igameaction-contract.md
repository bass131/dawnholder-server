---
owner: youngho
milestone: M4.14
phase: 02-igameaction-contract
title: IGameAction 계약 통일 — ActionContext 도입, ExecuteWithTarget/return false 제거
status: planned
grade: 복잡
slug: 02-igameaction-contract
created: 2026-06-14
domains: [server]
prior_phases: [01-baseline-and-prep, 01b-attackstate-flyweight-fix]
depends_on: [01-baseline-and-prep]
risk_flags: [trust-boundary]
worker_model: opus
---

# M4.14 Phase 02 — IGameAction 계약 통일

> 계획서 = `_milestone-plan.md` Phase #2 (최우선 가치). 근거 = `_architecture-review-2026-06-13.md` §2 (발견 #1, 개선 권고 ⭐). **순수 구조 리팩토링 — 거동·wire format 절대 보존.** `MeleeAction.Execute()`가 `return false`로 죽어있고 실행은 `ExecuteWithTarget()`로 우회하는 LSP/OCP 약화를 `ActionContext` 도입으로 봉합.

---

## Context (왜)

`IGameAction.Execute(map, caster, clientTick)` 계약에서 **Melee만 일탈**: `Execute()`가 `return false`(죽은 코드) + 실제 실행은 `ExecuteWithTarget(..., targetEntityId, ...)`. `ActionGate`가 구체 `MeleeAction.Instance.ExecuteWithTarget`을 직접 알아야 함(OCP 훼손). 근본 원인: `C_Attack`만 `targetEntityId`를 싣고 3개 스킬은 공간질의로 타겟을 찾아 — 공통 시그니처에 target 자리가 없어 Melee만 우회 경로 발생. **현재 동작은 정상**(Melee는 절대 Execute() 안 통과) = 활성 버그 아닌 잠재 트랩(새 호출자가 Execute()를 믿으면 조용히 실패).

---

## 증거 사슬 (검토 §2, 착수 시 재실측 — file:line은 01b 후 Actions/Systems/ 불변)

| 링크 | `파일:줄` (main `f151e55` 실측) | 내용 |
|---|---|---|
| IGameAction 계약 | `Actions/IGameAction.cs:21` | `bool Execute(GameMap, PlayerEntity, long clientTick)` — "적용됐으면 true". |
| Melee 일탈 | `Actions/MeleeAction.cs:25` `return false` / `:30-123` `ExecuteWithTarget` | 자백 주석 "직접 호출 경로 없음". |
| 정상 3종 | `Dash/Teleport/Thunderbolt Action.cs:19` | `Execute()` 정상 구현. |
| ActionGate 구체 의존 | `Systems/ActionGate.cs:22` (`TryPerformMelee`→`ExecuteWithTarget`) vs `:42` (`TryPerformSkill`→다형 `Execute`) | Melee만 구체 직접. |
| trust-boundary | `Systems/ActionGate.cs:50-67` (`Validate()` 4단계) | 본문 verbatim 보존 대상. |
| 호출부 | `Systems/CombatSystem.cs:31` / `Systems/SkillSystem.cs:25` | 각 `_gate.TryPerform*`. |

---

## 설계 (안 a — ActionContext, 검토 §2.5 채택)

`readonly struct ActionContext { long ClientTick; int TargetEntityId; }` 도입, `Execute(map, caster, in ctx)`로 통일. b안(인터페이스 분리)·c안(현상유지) 기각(§0.3 — 타겟 행동 1개뿐인데 타입↑ = 과한 추상화). **표면이 줄어든다**(2개 진입 메서드 + 죽은 Execute → 1개 TryPerform + 죽은 코드 0).

### 단계 (검토 §2.6)
1. `ActionContext.cs` 신규 1파일 (`readonly struct`, `in` 전달 = 틱 new 0).
2. `IGameAction.Execute` → `Execute(GameMap, PlayerEntity, in ActionContext)`.
3. `MeleeAction`: `return false` 삭제 → `ExecuteWithTarget` 본체를 `Execute`로 흡수, `targetEntityId` → `ctx.TargetEntityId`.
4. `Dash/Teleport/Thunderbolt`: 시그니처 `in ctx` 추가, 본문 ctx 무시.
5. `ActionGate`: `TryPerformMelee` + `TryPerformSkill` → 단일 `TryPerform(map, player, kind, in ctx)`. **`Validate()` 본문 verbatim** (검증 순서·부등호 무변경). `SetLastActionTick → Execute` 순서 보존. `MeleeAction` 구체 의존 제거.
6. 호출부 2곳: `CombatSystem`(ctx.TargetEntityId = 패킷 target) / `SkillSystem`(ctx.TargetEntityId = -1 또는 0).

---

## 완료 조건 / 게이트 (정량)

- [ ] `IGameAction.Execute(GameMap, PlayerEntity, in ActionContext)` 단일 계약 — `MeleeAction.Execute` 정상 실행(`return false` 제거), `ExecuteWithTarget` 소멸, `ActionGate` 구체 `MeleeAction` 비의존(단일 `TryPerform`).
- [ ] **거동 보존 (trust-boundary 기계 검증)**: `ActionGate.Validate()` 메서드 본문 `git diff` **라인 0** (사람 눈보다 결정적).
- [ ] WSL2 `dotnet test` 회귀 0 (Phase 01 baseline 568 비감소) — `CombatSwingTests`/`KnightDashTests`/`MageTeleportTests`/`ThunderboltSkillTests`/`ClassSkillGateTests`/`ValidateRewindTests` 등 전부 green.
- [ ] EditMode 회귀 0.
- [ ] **wire 무변경**: PDL 변경 0, `Protocol.Version` v13 그대로.
- [ ] 죽은 코드 0 (grep: `ExecuteWithTarget` 잔존 0).
- [ ] reviewer Tier 2-A 헌법 hard 위반 0.

**검증 흐름**: server Worker(**Opus** — 복잡+trust-boundary, opus-routing B) 구현 → 메인 `Validate()` git diff 0 실측 + diff 거동 대조 → WSL2 build 0/0 + test 568↑ → reviewer 필수 → -DONE.md.

---

## 위험 / 헌법 게이트

- **§3 trust-boundary**: `ActionGate.Validate()`는 4단계 검증(상태·쿨다운·클래스·rewind)의 단일 접점. 검증 *로직* 한 줄도 안 바꾸고 시그니처 plumbing만. reviewer 필수 + Opus Worker.
- **§2 Protocol**: 계약 통일은 server 내부. PDL/wire 무변경 v13. (`IGameAction`은 `98_Shared` 아님 → Shared.dll co-review 미트리거.)
- **§5 틱 루프**: `ActionContext` = `readonly struct` + `in` = new 0.
- **§0.3**: 최소 필드만(`ClientTick`+`TargetEntityId`). 확장 hook·인터페이스 분리 X.

> 복잡 등급 — Phase 02 `-DONE.md` 박제. Phase 03(Convention report)으로 진행(병렬 가능, 도메인 다름).
