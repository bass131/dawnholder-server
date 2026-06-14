---
owner: youngho
milestone: M4.14
phase: 01-baseline-and-prep
title: 진입 게이트 + 베이스라인 스냅샷 + 검토 박제 + 문서 stale 정정
status: done
grade: 보통
slug: 01-baseline-and-prep
created: 2026-06-14
completed: 2026-06-14
domains: [meta, shared]
prior_phases: []
depends_on: []
risk_flags: []
---

# M4.14 Phase 01 — 베이스라인 + 박제 + 문서정정

> 계획서 = `_milestone-plan.md` Phase 분해 표 #1. 근거 = `_architecture-review-2026-06-13.md`. 모든 거동-영향 Phase(02·01b)의 **회귀 0 비교 기준값**을 이 Phase에서 정량화하고, 검토 결과 문서를 git에 박제, 문서 stale 1건을 정정한다.

---

## Context (왜)

검토 문서(`_architecture-review`)는 브랜치 `feature/m4.13-shared-extract` `3c825e2` 근처에서 작성됐고 "코드가 바뀌면 file:line drift 가능"을 스스로 경고. 현재 작업 출발점은 M4.13 P4~P6(#108)까지 머지된 **최신 main `f151e55`**. carry-over 학습 1번("★박제/추천 전 file:line 실측 — 브랜치 전환 시 stale")대로 전제를 현재 main에서 **재실측**한 뒤 베이스라인을 고정한다.

---

## 진입 게이트 (Phase 02 착수 전 — 전부 통과 확인 2026-06-14)

- [x] M4.13 PR main 머지 완료 (#108 `f151e55`).
- [x] `02_Server/GameServer/Maps/{Actions,Systems}/` git clean (미커밋 0).
- [x] M4.13 임시 진단 코드 없음 (`Console.WriteLine`은 `RespawnSystem.cs:58` 운영 로그뿐 — 디버그 잔재 아님).
- [x] M4.14 새 브랜치 `feature/m4.14-architecture-followup` 출발 (knowledge cherry-pick `c6b3395` 포함).

## 재실측 결과 (현재 main `f151e55`, 2026-06-14) — 검토 전제 전부 유효

- **#1 (IGameAction ↔ MeleeAction)**: `MeleeAction.cs:25` `return false` + `:30` `ExecuteWithTarget`, `ActionGate.cs:22` Melee 구체 직접 호출. M4.13이 새 Action 타입 추가 안 함 → 여전히 정확히 4종(Melee/Dash/Teleport/Thunderbolt).
- **호출 사슬 양쪽 그대로**, `ExecuteWithTarget` 직접 호출자 0.
- **micro-drift 2건**: GameMap 499→**498**, `RespawnSystem.cs:58` M4.13 운영 로그 1줄.

---

## 베이스라인 정량값 (회귀 0 기준)

| 축 | 값 | 측정 |
|---|---|---|
| 서버 `dotnet test` (WSL2, ADR-029) | **568 passed / 0 failed / 5 skipped** (573 total) | Phase 01b(flyweight race 수정) **후** 3x 병렬 green. |
| Unity EditMode | **122 passed** (M4.13 머지 기준) | 이 Phase 재실행 X — Phase 06 마감 시 재확인. |

> ⚠️ baseline 측정 중 서버 풀 스위트에서 flaky 1건(`CommitWindowTests`) 발견 → 근본 원인 추적 + 수정이 **Phase 01b**로 분기(별도 파일). 위 568은 그 수정 후 안정값. 수정 전은 567 passed / 1 flaky(격리 시 통과).

---

## 문서 stale 정정 (검토 §4 부수 발견)

- `00_Document/conventions/CODE_CONVENTION.md:150` 부록 A — GameMap 줄 수 **436 → 498** 정정 (M4.13 Skill/Action 추가분 반영). 셀은 "졸업 ✅" 상태 유지(리팩토링 대상 아님, 줄 수만 stale였음).

## 박제 (git add)

- `_architecture-review-2026-06-13.md` + `_milestone-plan.md` (현재 untracked) → 추적 편입.

---

## 완료 조건 / 게이트 (정량)

- [x] 진입 게이트 4개 통과.
- [x] 베이스라인 정량값 박제 (서버 568/0/5 + EditMode 122).
- [x] CODE_CONVENTION 부록 A GameMap 줄 수 정정 (436→498).
- [x] 검토 문서 2종 git add + Phase 01/01b commit.

---

## 위험 / 헌법 게이트

- **거동 무변경**: 이 Phase는 측정·문서·박제만 (코드 0). 단, baseline 조사가 Phase 01b(코드 수정)를 파생 — 그 거동 보존은 01b 게이트.
- **§4**: 문서·계획만 변경, 양쪽 컴파일 무관.

---

> Phase 01b(flyweight race 근본수정) → Phase 02(IGameAction 계약 통일)로 진행. 보통 등급 — work-pin + commit. baseline 정량값이 02·01b 회귀 0 증명의 기준선.
