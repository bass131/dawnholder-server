# Review Tiering — 3-Tier 리뷰 + Tier 2 자동 SubAgent 2종

> **헌법 참조**: 본 정책은 새 헌법 v1 "🤖 SubAgent 풀 / 자동 호출 트리거" 섹션에서 링크됩니다.
> 충돌 시 헌법이 이깁니다.
>
> **⚠️ 명세 신선도 주의**: 본 정책 M3.5 갱신 시점(2026-05-20)에 *Tier 2 reviewer 자동 발동 실측 1건* + *plan-auditor 신설로 실측 0건*. M4 진입 후 첫 1주 안에 두 SubAgent의 false positive·누락·트리거 조건 재조정 예정.

본 문서는 코드 변경 후 리뷰 단계를 3개 Tier로 나누고, 그 중 **Tier 2 자동 호출**의 트리거·약속·결과 처리를 정의합니다. M3.5에서 Tier 2 = `reviewer` + `plan-auditor` 두 자동 SubAgent로 확장.

---

## 1. 3-Tier 리뷰 구조

| Tier | 누가 | 언제 | 무엇을 | 도입 상태 |
|---|---|---|---|---|
| **Tier 1** 도메인 셀프리뷰 | 도메인 SubAgent 자기 자신 | 코드 변경 직후, 결과 반환 전 | 자기 영역 헌법 위반 + 도메인 _index.md 패턴 점검 | **M4 진입 후 도입 예정** (미구현) |
| **Tier 2-A** 자동 통합 리뷰 | `reviewer` SubAgent (Opus) | 메인 세션이 트리거 조건 충족 시 자동 호출 | [`../REVIEW_CHECKLIST.md`](../REVIEW_CHECKLIST.md) 기준 5축 점검 | **실측 1건 (2026-05-18 ad-hoc 모드)** |
| **Tier 2-B** Phase 정의 사전 검증 | `plan-auditor` SubAgent (Opus) | `_milestone-plan.md` / Phase 정의 `.md` Write 후 자동 | 분해 적정성·의존성·완료 조건 명확성·등급 산정 | **M3.5 신설 (실측 0건)** |
| **Tier 3** 수동 깊은 리뷰 | `/harness-review` 슬래시 (Phase 05 산출물) | 사용자 명시 호출 | 하네스 자체 점검 (헌법/정책/SubAgent 정합) | **신설 슬래시 (옛 `/work:review` 강화 rename)** |

---

## 2. Tier 2-A `reviewer` — 트리거 조건

도메인 SubAgent 코드 변경 후 메인 세션(또는 coordinator)이 다음을 *순서대로* 평가:

### 2-1. 무조건 호출 (조건 무시)
- `98_Shared/` 변경 포함 → 호출
- 새 핸들러/패킷/공식 추가 → 호출
- 사용자가 *"리뷰 돌려줘"* 명시 → 호출
- **위험 깃발 발동**(trust-boundary/irreversible/unity-asset) → 호출 (M3.5 신규)

### 2-2. 조건부 호출
- 실질 코드 변경 ≥ 10줄 + 등급 ≥ 보통 → 호출
- 단순 등급은 호출 X (위임 비용 > 가치)

### 2-3. 무조건 스킵
- 테스트 파일만 변경 → 스킵 (회귀 안전망 강화는 리뷰 우선순위 낮음)
- 주석/오타/rename만 → 스킵
- 사용자가 *"리뷰 스킵해줘 — <사유>"* 명시 + 사유 첨부 → 스킵, work-pin에 사유 기록

---

## 3. Tier 2-B `plan-auditor` — 트리거 조건

### 3-1. 무조건 호출
- `_milestone-plan.md` Write/Edit → 호출
- `01_Phases/**/NN-{slug}.md` Write/Edit (Phase 정의) → 호출
- 사용자가 *"plan 점검해줘"* 명시 → 호출

### 3-2. 점검 대상
- Phase 분해 적정성 (5~7개 / 마일스톤, M3 9개는 과했음)
- 의존성 그래프 사이클 없음
- 완료 조건 명확성·정량성
- 등급 산정 적정성 ([`grade-and-risk.md`](grade-and-risk.md))
- 헌법 절대 원칙 위반 위험 사전 식별

### 3-3. γ 흡수

옛 Codex γ 방식(4~7회차 실측)에서 *코드 박기 전 설계 검증* 가치 증명 → `plan-auditor`로 내부 흡수. γ 6/7회차 HIGH 2 + MEDIUM 3 봉합 시간 절감이 가치 베이스라인.

외부 Codex β cross-check는 *대규모 등급 + 비가역 변경* 시 별도로 사용자 호출 (`/cross-review` 슬래시, Phase 05 산출물).

---

## 4. 입력 약속 (메인 세션 → SubAgent)

### Tier 2-A `reviewer` 호출 시

| 키 | 내용 |
|---|---|
| `range` | 변경 범위 식별자 (Phase slug 또는 ad-hoc id, WORK-ID와 동일) |
| `files` | 변경된 파일 절대 경로 목록 |
| `diff_summary` | 메인 세션이 작성한 자연어 diff 요약 |
| `grade` (M3.5 신규) | 작업 등급 (위험 깃발 박힌 상태) |

**3개 핵심 키 누락 시** reviewer가 *추측 없이 즉시 종료*. 메인 세션은 호출 전 다 준비.

### Tier 2-B `plan-auditor` 호출 시

| 키 | 내용 |
|---|---|
| `plan_files` | 변경된 plan/Phase 정의 `.md` 경로 |
| `milestone_context` | 어느 마일스톤의 일부인지 |
| `prior_phases` | 같은 마일스톤에서 이미 마감된 Phase의 -DONE.md 경로 (의존성 검증용) |

---

## 5. 결과 처리 (메인 세션 책임)

### Tier 2-A `reviewer` 반환

| 결과 | 다음 액션 |
|---|---|
| 🔴 **위반 있음** | 사용자에게 "고칠까요?" 확인 → 도메인 SubAgent 재위임. 사용자 *"패스"*면 work-pin에 `리뷰 패스 사유: <한 줄>` |
| 🟡 **개선 제안만** | 그대로 보여주고 통과. work-pin엔 별도 기록 X |
| 🟢 **위반 0개** | 통과. 메인 세션이 work-pin 마무리 후 사용자에게 최종 제시 |

### Tier 2-B `plan-auditor` 반환

| 결과 | 다음 액션 |
|---|---|
| 🔴 **결함 발견** | 사용자에게 결함 리스트 + 옵션 A(즉시 봉합) / 옵션 B(현 상태 진행) |
| 🟡 **개선 제안** | 그대로 보여주고 사용자 결정 |
| 🟢 **이상 없음** | Phase 진행 GO |

---

## 6. 우회 메커니즘 (S-1: 사유 명시 후 허용)

사용자가 *"리뷰 스킵해줘"* 또는 *"plan 점검 스킵해줘"* 명시 시 메인 세션은 *사유*를 요청. 사유 받으면:

1. SubAgent 호출 스킵
2. work-pin에 다음 줄 추가:

```
리뷰 스킵 사유: <사용자가 제공한 한 줄>
```

이 흔적은 `grep "리뷰 스킵 사유"`로 한 방에 회수 — 우회 *습관화* 감지 가능.

---

## 7. 범위 (Scope)

| | 대상 | 비고 |
|---|---|---|
| ✅ **점검** | 헌법 / ADR / ARCHITECTURE / 테스트 커버리지 / 도메인 패턴 / 등급 산정 | 5축 매핑 = [`../REVIEW_CHECKLIST.md`](../REVIEW_CHECKLIST.md) |
| ❌ **점검 X** | 코드 스타일 (네이밍/들여쓰기/포매팅) | Roslyn analyzer + .editorconfig 위임 (M4 후속 후보) |

**왜 코드 스타일 제외인가**: 합의 없는 1~3명 단계에서 *스타일 일관성*은 도구(analyzer + .editorconfig)에 위임이 옳음. 시니어 피드백 *"코드 컨벤션 깔쌈하게"*를 우리 현실에 맞게 재해석 (길 D).

---

## 8. 실측 기록 + 재조정 예정 항목

### 실측 기록

- [x] **ad-hoc 명시 호출 모드 1회 발동** (2026-05-18) — γ 방식(α + β + 비교)에서 `reviewer`가 *Tier 2 자동 트리거 외* 사용자 명시 호출. α 14건 + β α 동의 + 6건 추가 → M2.5 마일스톤 신설 결정. *Tier 2 정의 외 ad-hoc 모드 작동 검증*. 산출물: `00_Document/reviews/2026-05-18-pre-m3-{claude,codex}-review.md`.
- [ ] **Tier 2-A `reviewer` 자동 발동 실측** — M4 진입 후 첫 도메인 SubAgent 코드 변경 시 관찰
- [ ] **Tier 2-B `plan-auditor` 자동 발동 실측** — M4 진입 시 `_milestone-plan.md` Write 자동 발동 관찰

### 재조정 항목

본 정책은 *추측 기반*. M4 진입 후 첫 1주 안에 다음 관찰 → 명세 갱신:

- [ ] **`reviewer` 트리거 false positive** — "10줄 미만이지만 호출됐어야 했던" 케이스
- [ ] **`reviewer` 트리거 누락** — "10줄 이상이지만 트리비얼해서 스킵 권장" 케이스
- [ ] **`plan-auditor` 가치** — 사전 검증으로 봉합한 결함 vs 후속 사고 비율 (γ 가치 정량화)
- [ ] **결과 처리 마찰** — 🔴 시 *재 도메인 위임*이 자연스러운가, 메인 직접 고치는 게 빠른가
- [ ] **우회 습관화** — `grep "리뷰 스킵 사유"` 주간 카운트. 3회 초과 시 트리거 조건 재설계 신호
- [ ] **Tier 1 도메인 셀프리뷰 도입 시점** — Tier 2 안정화 후 1주 더 관찰 후 판단

재조정 결과는 ADR-019 후속 또는 본 정책 직접 수정 (변경 폭에 따라).

---

## 9. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 갱신:

- [`../REVIEW_CHECKLIST.md`](../REVIEW_CHECKLIST.md) (5축 점검 기준 — M3.5 시점 갱신 검토)
- [`../agents/reviewer.md`](../agents/reviewer.md) (Phase 02 산출물 — reviewer SubAgent 명세)
- [`../agents/plan-auditor.md`](../agents/plan-auditor.md) (Phase 02 산출물 — 신설)
- [`../../00_Document/ADR/harness/ADR-019-reviewer-agent.md`](../../00_Document/ADR/harness/ADR-019-reviewer-agent.md) (결정 박제 — M3.5 갱신 후속 또는 ADR-023 신설)
- [`subagent-routing.md`](subagent-routing.md) (SubAgent 자동 호출 트리거 정합)
- [`pin-and-done.md`](pin-and-done.md) (work-pin에 *리뷰 스킵 사유* / *리뷰 패스 사유* 라인 박는 정합)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 01 (2/2)에서 재작성. Tier 2를 `reviewer` + `plan-auditor` 두 자동 SubAgent로 확장 (γ 방식 내부 흡수). 새 SubAgent 풀 8 정합. `/harness-review`(Tier 3) rename. 옛 124줄 → ~150줄(plan-auditor 절 신설로 약간 증가).
- 2026-05-15 — 헌법에서 외부화. 실측 0건 상태 명시 + 합류 후 재조정 항목 5개 박음.
