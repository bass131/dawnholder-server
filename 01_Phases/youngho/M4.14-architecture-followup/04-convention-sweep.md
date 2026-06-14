---
owner: youngho
milestone: M4.14
phase: 04-convention-sweep
title: Convention 기계 수정 스윕 (중괄호/casing/#region) — 동작 0 순수 commit
status: planned
grade: 보통
slug: 04-convention-sweep
created: 2026-06-14
domains: [cross]
prior_phases: [01-baseline-and-prep, 03-convention-report]
depends_on: [03-convention-report]
risk_flags: []
---

# M4.14 Phase 04 — Convention 기계 수정 스윕

> 계획서 = `_milestone-plan.md` Phase #4. 근거 = `_architecture-review` §5.3 ⓒⓓ. Phase 03 report로 받은 진짜 카운트·diff를 **영호 승인 후에만** 기계 수정. **동작 변경 0 순수 포매팅 commit** (report와 완전히 별 commit).

---

## Context (왜)

§4 "기계적이라 미뤄도 부채 아님"으로 보류했던 중괄호·casing을 이제 당긴다. 핵심 규율: **동작 변경(Phase 02·01b)과 기계 정리(이 Phase)는 완전 분리**. 한 commit에 섞으면 diff 리뷰 불가 + 회귀 원인 추적 불가.

---

## 설계 (안전 절차 ⓒⓓ — 검토 §5.3)

- ⓒ Phase 03에서 **합의된 규칙만** `warning` 승격.
- ⓓ 기계 수정 = 동작 변경과 완전히 분리된 단독 commit, **WSL2 게이트 통과분만**.
- `refactor-sweep` 스킬 흐름과 정합 (단, self-bias 회피 위해 결정적 analyzer 출력 기준).

## 변경 대상 (Phase 03 승인 규칙에 따라 — 착수 시 확정)

1. 루트 `.editorconfig` — 합의 규칙 `suggestion`→`warning` 승격.
2. production 코드 기계 수정 — 중괄호 보강(IDE0011, ~90건 추정) / casing(IDE1006) / `#region` 기회성 제거(4건).
3. 범위: `02_Server`/`04_ClientNet`/`98_Shared`만. **Tests·`99_Tools`·`03_Client` 경계 유지** (per-dir `.editorconfig` `none` 확인됨).

---

## 완료 조건 / 게이트 (정량)

- [ ] 합의된 규칙만 `warning` 승격 + 기계 수정 — **동작 변경 0**.
- [ ] **순수성 증명**: 기계 수정 commit 전후 WSL2 test 카운트 **동일** (568 불변).
- [ ] 범위 경계 유지 (Tests·99_Tools·03_Client 미접촉 — git diff 범위 확인).
- [ ] `98_Shared` 수정 포함 시 양쪽 컴파일 확인 (§4). Shared.dll 재빌드는 소스 실변경 시만.
- [ ] 빌드 0 warning(승격 규칙 위반 0) / 0 error.

**검증 흐름**: cross Worker(Sonnet) 기계 수정 → 메인 diff가 순수 포매팅인지 실측(거동 영향 라인 0) → WSL2 build 0/0 + test 568 동일 → reviewer.

---

## 위험 / 헌법 게이트

- **동작 0 순수 commit**: 포매팅 외 라인 0. 의미 변경 발견 시 즉시 중단·분리.
- **§4 (98_Shared 포함 시)**: server+client 둘 다 컴파일. 이력 주석 일괄삭제 금지(`ProtocolVersion` 역사=계약 예외).
- **범위 경계**: 03_Client(Unity NuGet 비호환)·Tests·99_Tools는 의도적 제외.

> Phase 05(선택) 또는 Phase 06(마감)으로 진행.
