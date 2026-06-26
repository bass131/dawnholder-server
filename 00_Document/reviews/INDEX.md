# reviews/ — 과거 리뷰 기록 인덱스

> 마일스톤 진행 중 수행한 **α/β/γ cross-review 스냅샷** (Claude pre-review + Codex review + γ 비교).
> 전부 *과거 기록*이라 사실상 동결 — 결정 자체는 [`../ADR/INDEX.md`](../ADR/INDEX.md)·[`../policies/INDEX.md`](../policies/INDEX.md)에 박혀 있고, 여기는 *그 결정에 이르는 검토 과정*입니다.
> 종류: `pre-review`(사전 점검) · `codex-review`/`codex-prompt`(Codex β) · `cross-review`(γ 비교) · `refactor-sweep`(SOLID 점검).

## 마일스톤별

### pre-M3 · M3 (2026-05-18~19)
- `2026-05-18-pre-m3-claude-review.md` · `2026-05-18-pre-m3-codex-review.md` — M3 진입 전 사전
- `2026-05-18-m3-phase-plan-codex-prompt.md` · `2026-05-18-m3-phase-plan-codex-review.md` — Phase 계획 검토
- `2026-05-18-m3-phase-02-codex-review.md` · `2026-05-18-m3-phase-03-04-codex-review.md` — Phase별
- `2026-05-19-m3-phase-06-codex-precommit-review.md` · `2026-05-19-m3-phase-06-pr38-body-draft.md` — Phase 06 + PR 본문

### 하네스 리뷰 (2026-05-19 · 05-24 · 06-26)
- `2026-05-19-harness-review-followup-1of5.md` · `2026-05-24-harness-review-all.md`
- `2026-06-26-harness-review-all.md` — solo 전환·M7.5 신규자산 doc-sync (🔴2 문서-현실 drift, 위험 0). 봉합 = 브랜치 `chore/harness-doc-sync-solo`

### M3.5 · M3.6 (2026-05-21~22)
- `2026-05-21-cross-review-m3.5-phase06.md` — 하네스 v1 Phase 06
- `2026-05-22-cross-review-m3.6-plan.md` — M3.6 계획

### pre-M4 · M4.1 (2026-05-23)
- `2026-05-23-pre-m4-cross-review-claude.md` · `2026-05-23-pre-m4-cross-review-codex.md` — M4 진입 전
- `2026-05-23-cross-review-m4.1-phase01.md`
- `2026-05-23-claude-pre-review-m4.1-phase02-04.md` · `2026-05-23-cross-review-m4.1-phase02-04.md` · `2026-05-23-cross-review-m4.1-phase02-04-codex.md` · `2026-05-23-m4.1-phase02-04-codex-prompt.md`

### M4.2 맵 전환 (2026-05-28)
- `2026-05-28-claude-pre-review-m4.2-map-transition.md` · `2026-05-28-cross-review-m4.2-map-transition.md`

### 전체 감사 (2026-05-29)
- `2026-05-29-cross-review-full-audit.md`

### M4.3 머지 (2026-05-30)
- `2026-05-30-claude-pre-review-pr56-m43-merge.md` · `2026-05-30-cross-review-pr56-m43-merge.md`

### M4.4 지형 (2026-06-06)
- `2026-06-06-claude-pre-review-m4.4-terrain-01-02.md` · `2026-06-06-cross-review-m4.4-terrain-01-02.md`

### M4.5 전투 v9 (2026-06-07)
- `2026-06-07-claude-pre-review-m4.5-phase04-combat-v9.md` · `2026-06-07-cross-review-m4.5-phase04-combat-v9.md` · `2026-06-07-m4.5-phase04-codex-prompt.md`

### refactor-sweep (2026-06-12~13)
- `2026-06-12-refactor-sweep-dryrun.md` — 첫 dry-run (Codex가 자기평가 편향 적발)
- `2026-06-13-refactor-sweep.md` — 본 sweep

### 아키텍처 논리 감사 (2026-06-19)
- `2026-06-19-architecture-logic-audit.html` — **UltraCode 전체 게임코드 감사**(34에이전트). 위치오류·개념종속·계층위반·SOLID 4-lens + cross-domain → adversarial 검증. 38발견→13통과(헌법위반 1). M8(DB영속화) 전 정리 = QuestRegistry 분리·치트 게이트. (HTML 단독)
