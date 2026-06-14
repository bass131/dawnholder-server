---
owner: youngho
milestone: M4.14
phase: 03-convention-report
title: Convention analyzer report-only 도입 + 진짜 카운트 측정 + diff 미리보기
status: planned
grade: 보통
slug: 03-convention-report
created: 2026-06-14
domains: [cross]
prior_phases: [01-baseline-and-prep]
depends_on: [01-baseline-and-prep]
risk_flags: []
---

# M4.14 Phase 03 — Convention analyzer report-only

> 계획서 = `_milestone-plan.md` Phase #3. 근거 = `_architecture-review` §1(메타 발견) + §5(발견 #4). `CODE_CONVENTION §4`가 "M4.4+"로 의도적으로 미뤄둔 중괄호·casing 자동강제를 **analyzer report 모드로 먼저 켜 결정적 카운트**를 받는다. **report ↔ apply(Phase 04) 완전 분리.**

---

## Context (왜)

검토의 가장 중요한 발견: **Codex 정적 분석 카운트가 체계적으로 과대** (중괄호 288→실측 ~90 = 3.2배, 이력주석 57→34, field 위반 1→0). "정적 분석 신호 = 출발점이지 결론이 아니다"를 Codex 카운트 *자신*이 증명. 그래서 사람·AI 추정 대신 **analyzer(IDE0011 등) report 모드 = 단일 진실 카운트**를 받는다. carry-over 학습("★self-bias = 자기 진단 시 외부 cross-check")과도 정합 — 결정적 도구가 카운트.

---

## 현재 상태 (검토 §5.1)

- 루트 `.editorconfig` = §7.1대로 **SA1201/SA1202(멤버 정렬)만** `warning`, 나머지 StyleCop `none`. production만(Tests·99_Tools per-dir `none` 완화, 03_Client는 Unity NuGet 비호환 미적용).
- Codex 제안 = "새 발견"이 아니라 **"미뤄둔 §4를 당기자"**.

## 자동강제 가능 vs 사람 판단 (검토 §5.2)

| Convention | 강제 | 도구 |
|---|---|---|
| 중괄호 유지(§4) | ✅ | IDE0011 / SA1503 — ~90건, 별 commit |
| casing(§4) | ✅ | IDE1006 |
| `_camelCase` field(§3.3) | ⚠️ ROI 낮음 | 위반 0건 |
| `#region` 금지 | ✅ 기회성 | 4건뿐 |
| 책임 헤더(§6.5) | ⚠️ 존재만 | "좋은 헤더"는 사람 → reviewer 축 6 유지 |
| 이력 주석(§6.2) | ❌ 사람 | `ProtocolVersion` 역사=계약 예외 |

---

## 설계 (안전 절차 ⓐⓑ — 검토 §5.3)

- ⓐ analyzer를 `severity = suggestion`으로 켠다 (**빌드 실패 X**) → 진짜 카운트 확보.
- ⓑ 생성 diff 미리보기 + 위험 평가를 영호에게 보고 (Codex 원칙: "288건 일괄 전 diff 먼저").

## 변경 대상

1. 루트 `.editorconfig` — 중괄호(IDE0011/SA1503)·casing(IDE1006) `suggestion` 추가.
2. (산출) 위반 진짜 카운트 표 + 규칙별 diff 샘플.

---

## 완료 조건 / 게이트 (정량)

- [ ] analyzer가 중괄호/casing 위반 **진짜 카운트** 산출 (Codex 추정 288/57 → 실측치 교체).
- [ ] diff 미리보기 + 위험 평가가 영호에게 보고됨.
- [ ] **영호 승인 게이트** — 합의된 규칙만 Phase 04에서 `warning` 승격. 승인 전 Phase 04 착수 금지.
- [ ] 빌드 실패 0 (suggestion이라 CI green 유지).

**검증 흐름**: cross Worker(Sonnet) report 도입 → 메인이 카운트·diff 영호 보고 → 영호 승인 → Phase 04 게이트 개방. apply 전 cross-check 권유(self-bias 회피).

---

## 위험 / 헌법 게이트

- **승인 게이트 의무**: report→apply 분리. 카운트만 보고, 기계 수정 0.
- **범위 경계**: production만(02_Server/04_ClientNet/98_Shared). Tests·99_Tools·03_Client 경계 유지.

> Phase 04(기계 수정 스윕) — 승인된 규칙만. 도메인 cross, Phase 02와 병렬 가능.
