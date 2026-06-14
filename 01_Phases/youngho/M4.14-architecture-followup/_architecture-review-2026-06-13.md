# 아키텍처 검토 기록 — Codex Cross-Review 실측 검증 (2026-06-13)

> **성격**: M4.14 마일스톤의 *근거 문서*. Codex와의 아키텍처 cross-review 4발견을 메인 세션이 **ReadOnly로 전수 실측 검증**한 전문. 분류·근거(file:line)·설계 trade-off·회귀 매트릭스를 박제한다.
> **단일 진실 우선순위**: 헌법 > ADR > CODE_CONVENTION > 본 문서. 본 문서는 *측정 스냅샷* — 코드가 바뀌면 file:line은 drift할 수 있다(작성 시점 = 브랜치 `feature/m4.13-shared-extract`, commit `3c825e2` 근처).
> **작업 방식 계약**: 이 검토는 *분석만* — 코드 수정은 M4.14 Phase에서. 동작 변경과 기계 정리는 분리. 서버 권위·Map=Actor·tick-thread·protocol append-only 보존.

---

## 0. 검토 배경

영호가 Codex와 ClaudeDev 아키텍처 개선점을 논의하고, 그 결과를 메인 세션(Claude)에게 **"같이 ReadOnly로 실측해보자"**며 가져왔다. Codex의 요청 핵심:

- 각 발견을 **확정 문제 / 개선 권고 / false positive**로 분류
- "정적 분석이 틀릴 수 있다"는 전제로 함께 판단
- 코드 즉시 수정 금지 — 분류·근거·우선순위·단계별 계획·테스트·회귀 위험·자동강제 가능 여부 구분

Codex가 보고한 정적 분석 범위: 파일 157 / 타입 216 / 메서드 602 / 관계 1,129 / 검토 신호 109 / 높은 우선순위 5.

---

## 1. ⚠️ 메타 발견 — 정적 분석 카운트가 체계적으로 과대

검토에서 가장 중요한 발견. Codex가 보고한 위반 카운트를 ripgrep으로 production 코드(02_Server `Tests/`·`obj/` 제외, 04_ClientNet, 98_Shared, 생성코드 제외)에서 실측한 결과 **일관된 과대 측정**:

| 항목 | Codex 주장 | 실측 | 판정 |
|---|---|---|---|
| 책임 헤더 누락 public 클래스 | 6 | 5 | 근사 (+1) |
| private field `_camelCase` 위반 | 1 | **0** | ❌ 거짓 |
| `m_` prefix 사용 | 0 | 0 | ✅ 정확 |
| 단일문장 중괄호 생략 | **288** | **~90** | ❌ 3.2배 과대 |
| `#region` 사용 | 5 | 4 | 근사 (+1) |
| Phase/이력 주석 | 57 | 34 | ❌ 1.68배 과대 |

**해석**:
- 중괄호 288 → ~90: Codex가 *다음 줄에 `{`가 오는 정상 Allman 케이스*(`if (x)\n{`)와 `else if`, 한 줄 람다, `=>` expression-bodied, switch arm을 위반으로 합산한 것으로 추정. 진짜 위반(`if (x) DoThing();` 한 줄)은 ~90.
- field 위반 1 → 0: M4.3R Phase 06·07 네이밍 스윕이 이미 정리 완료. Codex의 정적 모델이 stale 했거나 테스트/주석의 `m_Socket` 류 문자열을 오탐.

**교훈 (캡스톤 서사 자산)**: "정적 분석 신호 = 출발점이지 결론이 아니다"를 Codex 카운트 *자신*이 증명했다. 그래서 M4.14 Phase 03은 사람·AI 추정 대신 **analyzer를 report 모드로 켜 결정적 카운트**를 받는다 (CODE_CONVENTION §4 "도구가 강제한다" 정합).

> ⚠️ 실측치 ~90도 근사다 — 정밀 카운트는 analyzer(IDE0011) 출력이 단일 진실. Phase 03의 존재 이유.

---

## 2. 발견 #1 — `IGameAction` 계약 ↔ `MeleeAction` 불일치 (개선 권고 ⭐ 최우선)

### 2.1 사실 (전부 확인)

`02_Server/GameServer/Maps/Actions/`:

- `IGameAction.Execute(GameMap, PlayerEntity caster, long clientTick)` → "실제로 행동이 적용됐으면 true" 계약 (`IGameAction.cs:19-21`).
- **Dash / Teleport / Thunderbolt**: `Execute()` 정상 구현 (각 `*.Action.cs:19`).
- **Melee만 일탈**: `MeleeAction.Execute()`가 `return false` + 주석이 스스로 자백 — `"직접 호출 경로 없음 — ExecuteWithTarget 사용"` (`MeleeAction.cs:25`). 실제 실행은 별도 `ExecuteWithTarget(map, attacker, targetEntityId, clientTick)` (`MeleeAction.cs:30`).
- `ActionGate`(`Maps/Systems/ActionGate.cs` — Actions/ 아님)가 구체 타입을 직접 앎: `TryPerformMelee` → `MeleeAction.Instance.ExecuteWithTarget(...)` (`ActionGate.cs:22`, Melee만 구체 직접 호출) vs `TryPerformSkill` → `ActionRegistry` 다형 dict 경유 `action.Execute(...)` (`ActionGate.cs:33`).

### 2.2 실측한 전체 호출 사슬

```
C_Attack   → AttackHandler  → GameSession.SubmitAttack   → map.EnqueueJob(ProcessAttack)
             → GameMap.ProcessAttack  → CombatSystem.ProcessAttack(_gate)
             → ActionGate.TryPerformMelee → MeleeAction.ExecuteWithTarget   ← targetEntityId 必

C_SkillUse → SkillUseHandler → GameSession.SubmitSkillUse → map.EnqueueJob(ProcessSkill)
             → GameMap.ProcessSkill   → SkillSystem.ProcessSkill(_gate)
             → ActionGate.TryPerformSkill → action.Execute               ← target 不要, 다형
```

`CombatSystem`/`SkillSystem`이 각각 `ActionGate _gate = new()`를 보유 (`CombatSystem.cs:19`, `SkillSystem.cs:15`).

### 2.3 근본 원인

**`C_Attack`만 `targetEntityId`를 싣고, 3개 스킬은 타겟을 스스로 공간질의로 찾는다** (Dash/Thunderbolt = `CombatSystem.ResolveImpactTargets`, Teleport = 타겟 없음). 그래서 공통 계약 시그니처에 target을 담을 자리가 없어 Melee만 우회 경로가 생겼다.

### 2.4 분류 근거 — "확정 버그"가 아니라 "개선 권고"인 이유

지금 시스템은 **정상 동작**한다. Melee는 절대 `Execute()`를 통과하지 않으므로(`TryPerformMelee`가 `ExecuteWithTarget` 직접 호출) `return false`는 **현재 호출자 0개인 죽은 코드**다. 즉 활성 버그가 아니라 *잠재 트랩*:

- **LSP 약화**: `IGameAction` 구현체를 동일하게 못 다룸 (Melee만 `Execute()`가 거짓 실패).
- **OCP 이점 훼손**: `ActionGate`가 구체 `MeleeAction`을 알아야 함.
- **미래 트랩**: 새 호출자가 `Execute()`를 정상 계약으로 믿고 호출하면 조용히 실패.

→ Codex가 #1로 꼽은 판단에 **동의**. 가장 가치 높고 contained.

### 2.5 설계안 3개 + trade-off

| 안 | 내용 | 장점 | 단점 | 채택 |
|---|---|---|---|---|
| **(a) ActionContext** | `readonly struct ActionContext { long ClientTick; int TargetEntityId; }` 도입, `Execute(map, caster, in ctx)`로 통일 | 단일 진입점 복원, 죽은 코드·`ExecuteWithTarget` 제거, `ActionGate` 구체 타입 비의존, struct+in=틱 new 0 | 3개 행동은 `TargetEntityId` 무시(약한 ISP 냄새) | ✅ |
| (b) 인터페이스 분리 | `IGameAction` + `ITargetedAction : IGameAction`, `ActionGate`가 타입 분기 | ISP 깔끔 | 타겟 행동 1개뿐인데 타입↑ = §0.3 과한 추상화 | ❌ |
| (c) 현상 유지 | 그대로 둠 | 리스크 0 | 죽은 코드·트랩 잔존 | ❌ |

**(a) 추천 근거**: 현재 *2개* 진입 메서드(`TryPerformMelee`/`TryPerformSkill`) + 죽은 `Execute`를 *1개* `TryPerform` + 죽은 코드 0으로 줄인다 → **표면이 줄어든다**(과한 추상화의 반대, §0.3 정합). `TargetEntityId`가 3개 행동에서 "있지만 안 씀"인 건 4개 구체 행동 규모에선 허용 가능한 trade-off.

### 2.6 단계별 계획 (M4.14 Phase 02) + acceptance criteria

```
1. ActionContext struct 신규 1파일 (readonly struct, in 전달)
2. IGameAction.Execute 시그니처 → Execute(GameMap, PlayerEntity, in ActionContext)
3. MeleeAction: return false 삭제 → ExecuteWithTarget 본체를 Execute로 흡수,
   targetEntityId 인자 → ctx.TargetEntityId
4. Dash/Teleport/Thunderbolt: 시그니처만 in ctx 추가, 본문에서 ctx 무시
5. ActionGate: TryPerformMelee + TryPerformSkill → 단일 TryPerform(map, player, kind, in ctx).
   Validate() 본문은 verbatim 보존(검증 순서·부등호 한 줄도 안 바꿈).
   SetLastActionTick → Execute 순서 보존.
6. CombatSystem/SkillSystem 호출부 2곳을 TryPerform(ctx)로 갱신
   (CombatSystem: ctx.TargetEntityId = 패킷의 target / SkillSystem: ctx.TargetEntityId = -1 또는 0)
```

**Acceptance**: 거동 불변 + wire format 무변경 + `Validate()` 4단계 순서 보존 + 틱 스레드 invariant 유지 + 죽은 코드 0 + `ActionGate`의 `MeleeAction` 구체 의존 제거.

### 2.7 회귀 위험 + 안전망

**기존 테스트 (전부 green 유지 = acceptance)**: `CombatSwingTests`(melee), `KnightDashTests`, `MageTeleportTests`, `ThunderboltSkillTests`, `ClassSkillGateTests`(클래스 게이트), `ValidateRewindTests`(rewind 범위), `HitboxTests`, `FacingSnapTests`, `CommitWindowTests`, `HitKnockbackTests`, `MageRangedCombatTests`, `DeferredDamageSystemTests`, `StateMachineTests`, `LagCompensationTests`.

| 위험 | 완화 |
|---|---|
| `Validate()` 검증 순서 뒤바뀜 (trust-boundary) | 본문 verbatim 이동 — `ClassSkillGateTests`+`ValidateRewindTests`가 잡음 |
| 쿨다운 latch 시점 변동 | `SetLastActionTick` → `Execute` 순서 보존 |
| 테스트가 `ExecuteWithTarget` 직접 호출 | 대부분 `GameMap.ProcessAttack` 경유 — 직접 호출 테스트 있으면 시그니처 갱신 (착수 시 grep 확인) |
| Smart App Control이 Windows `dotnet test` 차단 | **WSL2(ADR-029)에서 회귀** (메모리 정합) |

---

## 3. 발견 #2 — `LocalPlayerMovement` 책임 분리 (대부분 false positive, 선택 추출 1건)

### 3.1 실측

`03_Client/Assets/Scripts/Prediction/LocalPlayerMovement.cs` = **482줄** (Codex "~460" 근사). 단 §3.1("순수 로직은 plain C#로 추출, MonoBehaviour는 생명주기+호출만")을 **이미 상당 부분 지킴**:

- 예측 물리 본체 → `PlayerPredictor`(plain C#, EditMode 테스트 가능) 위임: `_predictor.Predict/StartImpulse/OnSnapshot/NotifySent`.
- 순수 함수 4개 이미 `static` 추출 + EditMode 테스트됨: `IsMovementLocked`(`:278`), `ResolveGatedInput`(`:290`), `ShouldForceAdopt`(`:304`), `ResolveClassMoveParams`(`:473`).

### 3.2 482줄의 정체

코드보다 주석이 압도적(체감 40%+). 실코드 ~250줄. MonoBehaviour 잔류 = ① 타이머 상태(쿨다운 4 + commit window + hit-gate) ② 고정 서브스텝 Update 루프 ③ Notify* 콜백 ④ reconcile 오케스트레이션 — 이는 §2.2 **"컨테이너 = 상태 + tick 엔진 + 경계"** 그 자체.

### 3.3 분류 근거

§2.2 분리 트리거("2개 이상 도메인")에 안 걸림 — 전부 "로컬 플레이어 예측" 단일 도메인. Codex 본인이 경고한 함정 — "분리 후 두 파일을 둘 다 열어야 이해되면 잘못 쪼갠 것"(§0.3) — 에 정확히 해당. 서브스텝 루프 + 임펄스 latch + reconcile은 한 호흡으로 읽어야 의미.

→ **유일하게 방어 가능한 추출** = 쿨다운/window 타이머 상태를 plain C# `PlayerAbilityTimers`로 (EditMode 테스트 부착 가능, 필드 수↓). 그러나 "해야 한다"가 아니라 "원하면" 수준 = **M4.14 Phase 05 선택**.

---

## 4. 발견 #3 — 정적 SRP 신호 5개 재분류

실측 라인 수 + 이미 분리된 System/Handler + 책임 헤더(§6.5) 유무:

| 신호 | 실측 줄 | 분리 현황 | 책임헤더 | 판정 |
|---|---|---|---|---|
| GameMap | 499 | CombatSystem 등 **6 System** 위임, 컨테이너+조율만 | ✅ | **false positive** (부록 A "졸업 ✅") |
| PlayerEntity | 298 | 단일 entity 상태, 로직은 외부 System | ✅ | **false positive** |
| LocalPlayerInput | 193 | 입력→의도 번역만, 이동/공격 위임 | ✅ | **false positive** |
| UnityClientSession | 214 | dispatch 16핸들러 + main-thread 마샬링 위임 | ✅ | **false positive** |
| LocalPlayerMovement | 482 | =#2, 미세 추출 1건 선택 | ✅ | 약한 개선 권고 |

Codex 예비 판단(GameMap/PlayerEntity/LocalPlayerInput = 의도/false positive, UnityClientSession = 핸들러 분리 후 정리됨, LocalPlayerMovement만 진짜)이 실측과 **완전 일치**.

> **부수 발견 (문서 stale)**: CODE_CONVENTION 부록 A가 GameMap을 **436줄**로 기재하나 실측 **499줄** (M4.13 Skill/Action 추가분 +63). M4.14 Phase 01에서 1줄 정정.

---

## 5. 발견 #4 — Convention 자동강제 보강 (개선 권고, 이미 로드맵에 있음)

### 5.1 현재 상태

- `CODE_CONVENTION §4`가 중괄호·casing 자동강제를 **"M4.4+, 기계적이라 미뤄도 부채 아님"**으로 *의도적으로* 보류.
- 루트 `.editorconfig`는 §7.1대로 **SA1201/SA1202(멤버 정렬)만** `warning`. 나머지 StyleCop 카테고리 전부 `none`. production만 적용 (Tests·99_Tools 하위 `.editorconfig`로 `none` 완화, 03_Client는 Unity NuGet 비호환으로 미적용).
- 즉 Codex 제안 = "새 발견"이 아니라 **"미뤄둔 §4를 이제 당기자"**.

### 5.2 자동강제 가능 vs 사람 판단 필요 (핵심 구분)

| Convention | 강제 가능? | 도구/방법 | 비고 |
|---|---|---|---|
| 멤버 정렬 (§7.1) | ✅ 완전 | SA1201/1202 (이미 켜짐) | — |
| 중괄호 유지 (§4) | ✅ 완전 | IDE0011 / SA1503 | ~90건 기계 수정, 별 commit |
| casing public/지역 (§4) | ✅ 완전 | IDE1006 | — |
| `_camelCase` field prefix (§3.3) | ⚠️ 부분 | dotnet_naming_rule | 위반 0건 = 강제 ROI 낮음 |
| `#region` 금지 (§7.1) | ✅ 가능 | IDE0079 계열/커스텀 | 4건뿐, 기회성 제거만 |
| 책임 헤더 누락 (§6.5) | ⚠️ *존재*만 | 커스텀 | "*좋은 헤더인지*"는 사람 → reviewer 축 6 유지 |
| 이력 주석 제거 (§6.2) | ❌ 사람 | — | `ProtocolVersion`처럼 역사=계약인 주석은 예외 |

### 5.3 안전 절차 (M4.14 Phase 03 → 04)

```
ⓐ analyzer를 severity = suggestion 으로 켠다 (빌드 실패 X) → 진짜 카운트 확보
ⓑ 생성 diff 미리보기 + 위험 평가를 사용자에게 보고 (Codex 원칙: "288건 일괄 전 diff 먼저")
ⓒ 합의된 규칙만 warning 승격
ⓓ 기계 수정 = 동작 변경과 완전히 분리된 단독 commit, WSL2 게이트 통과분만
```

`refactor-sweep` 스킬 흐름과 정확히 정합.

---

## 6. 우선순위 결론

1. **#1 IGameAction 계약 통일** — 최우선. contained, 죽은 코드+트랩 제거, OCP 복원. (M4.14 Phase 02)
2. **#4 analyzer report-only** — config 한 줄로 Codex 부풀린 추정 대신 진짜 카운트. (Phase 03 → 04)
3. **#2 타이머 추출** — EditMode 테스트가 값을 만들 때만. 아니면 skip. (Phase 05 선택)
4. **#3** — 작업 없음, 문서 줄 수만 정정. (Phase 01)

### 타이밍

M4.13(대쉬 reconcile 정렬)이 다른 세션에서 진행 중. 이 검토는 *별 트랙*. **M4.13 마감 + PR 착지 후** M4.14 진입 — 대쉬 손맛 디버깅과 리팩터 diff가 섞이지 않게 (Codex 원칙 "동작 변경과 기계 정리 분리" 정합).

---

## 부록. 원본 Codex 검토 프롬프트 요약

Codex가 가져온 4발견 (원문은 영호 메시지):
1. `IGameAction` 계약과 `MeleeAction` 불일치 — `Execute()` always false + `ExecuteWithTarget` 우회.
2. `LocalPlayerMovement` 책임 분리 (~460줄, 8책임) — §3.1 비교.
3. 정적 SRP 신호 재검토 (GameMap/PlayerEntity/LocalPlayerInput/UnityClientSession/LocalPlayerMovement).
4. Code Convention 자동 강제 보강 — 헤더 6 / field 1 / m_ 0 / 중괄호 288 / #region 5 / 이력주석 57.

Codex 작업 방식 요청: CLAUDE.md + conventions 우선 기준 / 분류 먼저 / 테스트·호출관계 확인 / 작은 단계 + acceptance criteria / 동작 변경과 기계 정리 분리 / 서버권위·Map=Actor·tick-thread·append-only 보존 / 의견 다르면 방어 말고 근거+trade-off (정적분석도 틀릴 수 있다는 전제).
