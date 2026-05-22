---
owner: youngho
milestone: M3.8
phase: 01
title: PRD 갱신 + 마일스톤 표 정합
status: pending
grade: 단순
risk: irreversible
estimated: 0.5~1h
domain: meta
summary: PRD.md 마일스톤 표에 M3.8/M4.1/M4.2/M4.3 행 추가 + MVP 제외 항목 정정 (직업/스킬 트리, 퀘스트/NPC 정정) + CHANGELOG [H] entry 박음
---

# Phase 01: PRD 갱신 + 마일스톤 표 정합

> **상태**: pending
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 단순 (위험 깃발 `irreversible` 자동 상향 후보 — PRD 결정 뒤집기 [H])
> **담당**: 메인 직접 (1 파일, 결정 박제)
>
> **등급 격차 결정** (plan-auditor 개선 제안 봉합): 위험 깃발 `irreversible` 박혀있지만 단순 유지. 사유 = *작업 차원 단순* (1 도메인 × 1 파일 × ~10줄, PRD.md 갱신 + CHANGELOG entry), *위험 깃발은 결정 박제 차원*이라 양식 부담 격차 의무 X. grade-and-risk.md §8 "실측 후 재조정" 후보 — M3.8 마감 시점에 본 격차 재검토.

---

## 🎯 목표

`PRD.md`의 *마일스톤 표*가 옛 M4 한 줄만 박혀있고 M3.5/M3.6/M3.7/M3.8/M4.1/M4.2/M4.3 박힘이 누락된 stale 상태 봉합 + *MVP 제외 항목*에 박혀있는 "직업/스킬 트리, 퀘스트/NPC"가 M3.8 Phase 03 캐릭터 선택 + Phase 04 NPC 도입과 충돌하는 점 정정 + CHANGELOG [H] entry 박음.

본 Phase 끝나면 = PRD가 *현재 진행 상태와 정합* + MVP 제외 항목이 *데모용 단순 흡수 항목 인지*.

---

## ⏪ 사전 조건

- [ ] M3.8 `_milestone-plan.md` 박혀있음 (본 Phase 호출 트리거)
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점"에 M3.8 진입 명시 (본 세션 직전 갱신 박힘)

---

## 📝 작업 내용

- [ ] `PRD.md` 마일스톤 표(line 110~120) 갱신:
  - `M3` 행 = 그대로 (응급 데모 마감)
  - **신설** `M3.5` (Harness v1) / `M3.6` (하네스+코드 점검) / `M3.7` (Sync gate + cadence) / `M3.8` (Capstone-1 Demo Infrastructure) 4 행 추가
  - `M4` 행 = `M4.1` (Combat Precision) / `M4.2` (Map Transition) / `M4.3` (AI + Polish) 3 행으로 분할
  - 각 행 = `데모할 수 있는 것` 한 줄 박음
- [ ] `PRD.md` "MVP에 의도적으로 뺀 것" (line 70) 정정:
  - 옛 = "직업/스킬 트리, 퀘스트/NPC"
  - 새 = "**스킬 트리** (기본 직업 2종 = 전사/원거리는 캡스톤 데모용 흡수, M3.8), **퀘스트** (NPC 대화는 캡스톤 데모용 단순 흡수, M3.8)"
- [ ] `PRD.md` 변경 이력 표 (line 152~158) 마지막 줄 추가:
  - `2026-05-22 | 캡스톤 1 발표 데모 인프라 도입 (M3.8) — 마일스톤 표 세분화 + MVP 제외 항목 정정 (직업/스킬 트리·퀘스트/NPC). 본 마감 후 일부 제거 가능 (NPC hardcoded → M6 길드 진입 시 정식화). [H] CHANGELOG entry 동반.`
- [ ] `.claude/CHANGELOG.md` 이력 표 최상단에 [H] entry 박음:
  - 한 줄 요약 = "M3.8 Capstone-1 Demo Infrastructure 마일스톤 신설 — PRD 갱신 동반 (마일스톤 표 세분화 + MVP 제외 항목 정정). 캐릭터 선택 (전사/원거리, 스탯 분기) + NPC 대화 (단순 hardcoded) = MVP 제외 항목과 충돌하는 *데모용 단순 흡수* 패턴 박음. 영향 = 옛 PRD 결정 일부 뒤집기 = [H]. 모든 팀원 영향 (PRD 정합 + 본 마감 후 제거 가능 항목 인지)."
- [ ] commit message = `plan(M3.8): Phase 01 마감 — PRD 갱신 + 마일스톤 표 정합 + CHANGELOG [H] entry`

---

## ✅ 완료 조건

- [ ] `PRD.md` 마일스톤 표에 M3.8 행 포함 7개 행 (M3.5/M3.6/M3.7/M3.8 + M4.1/M4.2/M4.3) 박힘
- [ ] `PRD.md` "MVP에 의도적으로 뺀 것" 두 항목 (직업/스킬 트리, 퀘스트/NPC) 정정 박힘
- [ ] `PRD.md` 변경 이력에 2026-05-22 줄 박힘
- [ ] `.claude/CHANGELOG.md` 최상단 [H] entry 박힘
- [ ] commit 박힘 + push 박힘 (사용자 명시 GO 후) — *PR 게이트 = 본 Phase 단독 PR 분리 X*. M3.8 전체 마감 시점에 한 PR로 묶음

---

## 🧪 테스트

**자동**: 본 Phase = 메타/문서 차원이라 dotnet test 영향 X. 회귀 확인만 (`dotnet test` green 유지).

**수동**:
- `PRD.md` 마일스톤 표 시각적 확인 (마크다운 렌더링 깨짐 X)
- `.claude/CHANGELOG.md` 최상단 entry 박힘 시각적 확인
- 변경 후 본 Phase 02 진입 시 *PRD 정합 인지* 확인 (Phase 02·03·04 진행 중 PRD 위배 X)

---

## 📚 학습 포인트

- **PRD ≠ 헌법** — PRD는 *결정 갱신 가능* 문서 (헌법은 절대 원칙 박힘). PRD 결정 뒤집기 = [H] 위험도지만 *허용*. 단 결정 박제 의례 (CHANGELOG entry + 변경 이력 줄) 거치기.
- **시연 마일스톤 ↔ MVP 충돌 패턴** — 캡스톤 1 같은 *중간 시연*에서 MVP 제외 항목 일부 도입 자주 발생. *데모용 단순 흡수* 절 신설 패턴 = 본 마감 후 제거 가능 명시.
- **변경 이력 한 줄 = 면접 자산** — "왜 캐릭터 선택을 캡스톤 1에 넣었지?" 질문 받을 때 PRD 변경 이력 한 줄로 답변 가능. 결정 추적 = 시니어 어필.

---

## ⚠️ 함정 / 주의사항

- **마일스톤 표 정합 빠뜨림** — M3.5/M3.6/M3.7/M3.8 모두 박을 것. *M3.8만 박고 M3.5~M3.7 누락* 시 stale 다시 박힘.
- **CHANGELOG [H] vs [M] 판정** — 캐릭터 선택 신설 자체는 [M]지만, *PRD 결정 뒤집기*가 동반되면 [H]. 영향 = 모든 팀원이 PRD 정합 인지 의무.
- **본 Phase 단독 PR 분리 X** — Phase 01 = 메타 차원, 단독으로 PR 박으면 PR 게이트 비용 ↑. M3.8 전체 마감 시점에 한 PR로 묶음. commit 박고 *push만 분리 박을 수 있음* (push도 묶을지는 본인 판단).
- **위험 깃발 `irreversible` 발동** — `risk-detector.sh` Hook이 `PRD.md` Write 또는 `gh pr merge`를 깃발로 박을 수 있음. work-pin 등급 상향 검토 (단순 → 보통, 단 본 Phase는 *결정 박제*라 단순 유지 OK).

---

## ➡️ 다음 Phase

- Phase 02 — 메인화면 UI + 엔딩 화면 (병렬 가능, client 도메인)

---

## 📋 박제

본 Phase = 단순 등급 → work-pin 갱신 + commit message만 박음. -DONE.md 박지 않음.

work-pin "현재 작업" → "Phase 01 ✅ 마감, Phase 02 미진입" 갱신.

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
