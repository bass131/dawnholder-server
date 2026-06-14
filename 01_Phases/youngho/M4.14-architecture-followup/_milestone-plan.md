---
owner: youngho
milestone: M4.14
title: 아키텍처 검토 후속 — Action 계약 통일 + Convention 자동강제
status: in_progress
grade: 복잡
risk: trust-boundary
estimated: 9~14h (총합, 6 Phase + 01b 파생)
domains: [server, cross, client]
---

# M4.14 — 아키텍처 검토 후속 (Architecture Followup)

> **상태**: planned — 2026-06-13 Codex cross-review + 메인 세션 ReadOnly 실측 기반 사전 작성
> **시작**: 미정 (M4.13 마감 + PR 착지 *후*)
> **목표 마감**: 미정 — 발표 마감 경고 없이 논리적 응집으로 가름 (일정 = 영호 컨트롤)
> **선행 근거 문서**: [`_architecture-review-2026-06-13.md`](./_architecture-review-2026-06-13.md) (발견별 분류·근거·설계안 전문)

---

## 🎯 마일스톤 목표

2026-06-13 Codex와의 아키텍처 cross-review를 메인 세션이 **ReadOnly 실측으로 전수 검증**한 결과를 코드에 반영한다. 핵심 두 갈래:

1. **`IGameAction` 계약 통일** (server) — `MeleeAction.Execute()`가 `return false`로 죽어있고 실제 실행은 `ExecuteWithTarget()`로 우회하는 LSP/OCP 약화를 `ActionContext` 도입으로 봉합. **순수 구조 리팩토링 — 거동·wire format 절대 보존.**
2. **Convention 자동강제 보강** (cross) — `CODE_CONVENTION §4`가 "M4.4+"로 의도적으로 미뤄둔 중괄호·casing 자동강제를 **analyzer report 모드로 먼저 켜 진짜 카운트를 받고**, 합의된 규칙만 기계 수정한다. **동작 변경과 기계 정리는 완전히 별 commit.**

부차적으로 (선택) 클라 `LocalPlayerMovement` 타이머 상태 미세 추출, 문서 stale 정정(부록 A GameMap 줄 수).

### ⚠️ 등급 = 복잡 사유

- **2 도메인** (server `IGameAction` + cross `.editorconfig`/스윕) + 선택 client = §grade-and-risk 복잡.
- **trust-boundary 깃발** (Phase 02): `ActionGate.Validate()`는 헌법 §3 4단계 검증(상태·쿨다운·클래스·rewind)의 단일 접점. **검증 *로직*은 한 줄도 안 바꾸고 시그니처 plumbing만** 하지만, 트랩 코드라 안전망 강화 + reviewer 필수 + (opus-routing 정책) 구현 Worker Opus 위임 대상.
- **비-대규모 근거**: 3+ 도메인 동시 아님, irreversible 깃발 없음(PDL/Protocol 변경 0, DB 0, push 0, ProtocolVersion bump 0).

### 핵심 원칙 (헌법 + Convention)

- **§2.2 컨테이너 + System** — Action 4종은 이미 Flyweight 전략(GPP Strategy)으로 잘 분리됨. 계약 통일은 *그 위에* 단일 진입점을 복원하는 것 (구조 후퇴 아님).
- **§0.3 과한 추상화 금지** — `ActionContext`는 현재 4개 행동에 필요한 최소 필드(`ClientTick` + `TargetEntityId`)만. 호출자 0개 확장 hook·인터페이스 분리(b안)는 채택 *안* 함.
- **헌법 #2/#4 wire format 신성** — Phase 02·05 어디서도 PDL 변경 0. `Protocol.Version` 그대로.
- **헌법 #5 틱 루프** — `ActionContext`는 `readonly struct` + `in` 전달 = 틱 루프 new 0.
- **헌법 §3 trust-boundary** — `ActionGate.Validate()` 본문 verbatim 보존. 검증 순서가 흩어지지 않게.
- **동작 보존** — 각 거동-영향 Phase 전후 WSL2(ADR-029) `dotnet test` 회귀 0 + Unity EditMode 0 회귀로 정량 증명 (Phase 01에서 베이스라인 스냅샷).
- **§4 기계 정리 분리** — Convention 스윕(Phase 04)은 동작 변경 0, 순수 포매팅 commit. report(Phase 03)와 apply(Phase 04) 분리.

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk | 근거(검토 발견) |
|---|---|---|---|---|---|---|
| 01 | 진입 게이트 확인 + 베이스라인 스냅샷 + 검토 결과 박제 + 문서 stale 정정 | 보통 | 메타+shared | 1~2h | — | 전 항목 선행 |
| 01b | **AttackState flyweight `Pending*` 제거 (병렬 테스트 race 근본수정)** — *계획 외 파생* (Phase 01 baseline 조사 중 flaky 발견) | 보통 | server | 1h | — | 영호 결정 2026-06-14, 별도 파일 |
| 02 | `IGameAction` 계약 통일 (`ActionContext` 도입, `ExecuteWithTarget`/`return false` 제거, `ActionGate` 단일 `TryPerform`) | 복잡 | server | 3~4h | trust-boundary | #1 (개선 권고 최우선) |
| 03 | Convention analyzer **report-only** 도입 + 진짜 카운트 측정 + diff 미리보기 | 보통 | cross | 1~2h | — | #4 (전반) |
| 04 | Convention **기계 수정 스윕** (중괄호/casing/#region) — 동작 0 순수 commit | 보통 | cross | 2~3h | — | #4 (apply) |
| 05 | (선택) `LocalPlayerMovement` 타이머 상태 추출 (`PlayerAbilityTimers` plain C# + EditMode 테스트) | 보통 | client | 1~2h | — | #2 (선택) |
| 06 | 회귀 + 마일스톤 마감 (WSL2 전 테스트 + CHANGELOG + -DONE.md) | 보통 | cross | 1h | — | 마감 |

**총 등급 = 복잡**. Phase 02(trust-boundary)는 reviewer 필수 + **구현 Worker도 Opus 위임**(opus-routing 정책 §5.5 — `복잡+trust-boundary`가 모델 티어 상향. Phase 02 정의 frontmatter에 `worker_model: opus` 명시). Phase 05는 *선택* — EditMode 테스트가 값을 못 만들면 건너뛰고 현 상태 유지(§0.3).

---

## 🚦 진입 게이트 (선행조건 — plan-auditor 🔴 봉합)

> **병행 세션 충돌 위험**: M4.14 Phase 02가 손대는 `02_Server/GameServer/Maps/{Actions,Systems}/`는 **현재 다른 세션의 M4.13 작업 디렉토리와 동일**. M4.13이 `DashAction.cs` 등에 미커밋 진단 코드를 들고 있어, `IGameAction.Execute` 시그니처 변경(광역 리팩터)은 머지 충돌이 자동 해소 안 됨. "마감 후"라는 *의도*만으로 부족 → **검증 가능한 게이트**로 박는다:

- [ ] M4.13 PR이 `main`에 **머지 완료** (또는 M4.14 시작 브랜치에 M4.13 변경 전부 반영).
- [ ] `git status 02_Server/GameServer/Maps/Actions/ 02_Server/GameServer/Maps/Systems/` = **clean** (미커밋 0).
- [ ] M4.13 임시 진단 코드(`DashAction.cs` 등 `Console.WriteLine` 디깅) **제거 확인**.
- [ ] M4.14는 M4.13 머지 후 **새 브랜치**에서 출발 (`feature/m4.14-architecture-followup` 등).

이 게이트는 Phase 01의 첫 체크 항목 — 통과 전 Phase 02 착수 금지.

---

## 🔗 의존성 그래프

```
Phase 01 (베이스라인 + 박제 + 문서 정정)
   │
   ├──→ Phase 01b (AttackState flyweight race 근본수정)   ←── 계획 외 파생 (baseline flaky 발견, server, 거동 보존)
   │
   ├──→ Phase 02 (IGameAction 계약 통일)   ←── 독립 (server, 거동 보존)
   │
   └──→ Phase 03 (analyzer report-only)
              │
              ↓
        Phase 04 (기계 수정 스윕)   ←── 03 후 (카운트·diff 승인 게이트 통과 필요)

Phase 05 (LocalPlayerMovement 타이머)   ←── 독립 (client, 선택)

Phase 06 (회귀 + 마감)   ←── 02·03·04·(05) 전부 후
```

- **01 → 전부**: 베이스라인 테스트 카운트가 회귀 0 증명의 기준선.
- **03 → 04**: report로 진짜 카운트·diff를 받아 *사용자 승인* 후에만 apply (Codex 원칙: "288건 일괄 수정 전 diff·위험 먼저 보고").
- **02 ∥ 03·05**: 도메인 다름 — 병렬 가능 (server ↔ cross ↔ client).
- **권장 순서**: 01 → 02 (최우선 가치) → 03 → 04 → (05) → 06.

---

## ✅ 마일스톤 완료 조건

- [ ] **진입 게이트**: 위 🚦 4개 체크 전부 통과 (M4.13 머지 + Actions/Systems clean) — Phase 01 첫 항목.
- [ ] **베이스라인 정량화 (Phase 01)**: WSL2 `dotnet test` 통과 카운트 숫자 + Unity EditMode 카운트를 Phase 01 `-DONE.md`에 박제 (예: `server 142 passed / EditMode 122 passed`) — "회귀 0" 비교의 객관 기준값. (실측: 서버 **568/0/5**, EditMode **122** — Phase 01b 후 안정값.)
- [ ] **race 근본수정 (Phase 01b)**: `PlayerCombatStates.Attack` flyweight의 mutable `Pending*` 3필드 제거 → `EnterAttackState`가 엔티티 직접 세팅(HitState 패턴). flyweight 무상태 복원. 풀 병렬 스위트 **3x green (568/0/5)**, 거동 보존(테스트 무변경). 헌법 "정적 mutable 게임 상태 금지" 정합.
- [ ] **#1**: `IGameAction.Execute(GameMap, PlayerEntity, in ActionContext)` 단일 계약 — `MeleeAction.Execute`가 정상 실행(`return false` 제거), `ExecuteWithTarget` 소멸, `ActionGate`가 구체 `MeleeAction` 타입 비의존 (단일 `TryPerform`).
- [ ] **거동 보존 (#1, trust-boundary 기계 검증)**: `ActionGate.Validate()` 메서드 **본문 `git diff` 라인 0** (시그니처 외 검증 로직 무변경 — 사람 눈보다 결정적). WSL2 `dotnet test` 회귀 0 (Phase 01 베이스라인 유지/증가). EditMode 회귀 0.
- [ ] **wire format 무변경**: PDL 변경 0, `Protocol.Version` 그대로.
- [ ] **#4 report**: analyzer가 중괄호/casing 위반 *진짜 카운트*를 산출 (Codex 추정 288/57/6/1 → 실측치로 교체). diff 미리보기 + 위험 평가가 사용자에게 보고됨.
- [ ] **#4 apply**: 합의된 규칙만 `warning` 승격 + 기계 수정 — **동작 변경 0 순수 commit** (회귀 0). production(02_Server/04_ClientNet/98_Shared)만, Tests·99_Tools·03_Client 범위 경계 유지.
- [ ] **(선택) #2**: `LocalPlayerMovement` 타이머 추출 시 EditMode 테스트 신규 + 회귀 0. 값 없으면 *명시적 skip 사유 박음*.
- [ ] **문서**: 부록 A GameMap 줄 수 stale(436→실측) 정정. CHANGELOG entry.
- [ ] Phase 02 reviewer 헌법 hard 위반 0.
- [ ] (복잡 마일스톤) 각 복잡 Phase -DONE.md. 5단계 보고는 대규모 아니라 *불요* (마일스톤 -DONE.md로 충분).

---

## 🚫 이번에 명시적으로 뺀 것 (사유 박음)

- **#1 옵션 (b) 인터페이스 분리** (`ITargetedAction : IGameAction`): 타겟 필요 행동이 Melee 1개뿐인데 타입이 늘어 §0.3 과한 추상화 위반 위험. (a) `ActionContext` 단일 struct가 더 적은 표면. → 채택 안 함.
- **#3 SRP 신호 4개** (GameMap/PlayerEntity/LocalPlayerInput/UnityClientSession): 실측 결과 전부 false positive — §2.2 의도된 컨테이너(이미 System/Handler 분리 완료, 책임 헤더 보유, 2+ 도메인 혼재 없음). 리팩토링 대상 아님. (GameMap은 부록 A "졸업 ✅" 상태 유지, 줄 수만 정정.)
- **#2 LocalPlayerMovement "8책임 분리"**: 예측 물리는 이미 `PlayerPredictor`(plain C#) + 순수 함수 4개(static, EditMode 테스트됨)로 추출 완료. 나머지는 §2.2 컨테이너(상태+tick엔진+경계). 482줄은 주석 과밀(체감 40%+). 타이머 1건 외 분리는 §0.3 위반("두 파일 동시에 열어야 이해" 트랩). → Phase 05만 선택, 나머지 현 상태 유지.
- **Convention 책임 헤더 자동강제** (§6.5): "헤더 *존재*"는 자동 검출 가능하나 "*좋은 헤더인지*"는 사람 판단 → reviewer 축 6 유지, analyzer 강제 대상 제외.
- **`#region` 강제 제거**: 4건뿐(ServerCore 레거시 `Session.cs`/`ClientSession.cs`). §7.1대로 SA1201/1202가 순서를 강제하므로 region 의존이 무해 — 스윕 시 *기회성* 제거만, 별 규칙 강제 안 함.
- **이력 주석 일괄 제거** (§6.2 b): `ProtocolVersion`처럼 역사가 계약 이해에 필요한 주석은 예외(Codex 지적 정당). 기계 일괄 삭제 X — 사람 판단 영역, M4.10/M4.3X에서 이미 대정리됨.

---

## 갱신 이력

- 2026-06-13 — **사전 작성** (메인 세션, M4.13 병행 중). Codex cross-review 4발견을 ReadOnly 실측 검증 → #1(개선 권고 최우선)·#4(로드맵 당김)만 작업화, #2·#3은 대부분 false positive로 분류. Codex 정적 카운트 체계적 과대(288→~90, 57→34, 6→5, 1→0) 실측 확인 = 검토 doc에 박제.
- 2026-06-13 — **plan-auditor 조건부 GO** → 봉합 완료. 🔴(M4.13 병행 충돌 — `DashAction.cs` 미커밋 진단 코드, Actions/ 디렉토리 광역 시그니처 리팩터 머지 충돌 위험) → 🚦 진입 게이트 섹션 신설(검증 가능 4체크). 🟡 4건 봉합: ①Phase 02 acceptance에 `Validate()` 본문 git diff 0 기계검증 ②`worker_model: opus` 명시 ③`ActionGate` 정확 경로(`Maps/Systems/`) 정정 ④Phase 01 베이스라인 테스트 카운트 박제. 설계 변경 0 — 전부 완료조건 정밀화.
- 2026-06-14 — **착수 + 개별 Phase 정의 파일 6개 생성** (`01`~`06`, 누락분 materialize — 영호 발견). **재실측**(현재 main `f151e55`)으로 검토 전제 전부 유효 확인. **Phase 01b 신설**: baseline 측정 중 `CommitWindowTests` flaky 발견 → 근본 원인 = `AttackState` flyweight `Pending*` 공유 mutable 채널(병렬 race) → 안 B(HitState 패턴 통일) 근본수정(영호 결정, 별도 파일 `01b-attackstate-flyweight-fix.md`). 3x 병렬 568/0/5 green. baseline = 서버 568/EditMode 122.
