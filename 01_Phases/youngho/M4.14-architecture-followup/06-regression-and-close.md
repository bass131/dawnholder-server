---
owner: youngho
milestone: M4.14
phase: 06-regression-and-close
title: 회귀 + 마일스톤 마감 (WSL2 전 테스트 + EditMode + CHANGELOG + -DONE)
status: planned
grade: 보통
slug: 06-regression-and-close
created: 2026-06-14
domains: [cross]
prior_phases: [01b-attackstate-flyweight-fix, 02-igameaction-contract, 03-convention-report, 04-convention-sweep, 05-localplayermovement-timers]
depends_on: [02-igameaction-contract, 03-convention-report, 04-convention-sweep]
risk_flags: []
---

# M4.14 Phase 06 — 회귀 + 마일스톤 마감

> 계획서 = `_milestone-plan.md` Phase #6. 모든 작업 Phase(01b·02·03·04·(05)) 후 전체 회귀 + 마감 박제.

---

## Context (왜)

복잡 마일스톤(아키텍처 후속)의 최종 게이트. 각 Phase는 자기 회귀를 통과했으나, 마지막에 **전체 풀 스위트 + EditMode + 봇**을 한 번 더 돌려 누적 회귀 0을 정량 증명하고 마일스톤 -DONE을 박제한다.

---

## 변경 대상 / 산출

1. WSL2 `dotnet test` 풀 스위트 + Unity EditMode 풀 회귀 실행·기록.
2. 봇 회귀(`BossStageClearSmoke` 등) — 서버 누적 상태 FAIL ≠ 회귀(`run_bot_fresh_recheck.sh` fresh 단독 재검 판정).
3. `.claude/CHANGELOG.md` entry (모든 팀원 영향 — IGameAction 계약 변경 + Convention 강제 보강).
4. `_milestone-DONE.md` (복잡 마일스톤 — 마일스톤 -DONE로 충분, 5단계 보고는 대규모 아니라 불요).

---

## 완료 조건 / 게이트 (정량)

- [ ] WSL2 `dotnet test` 회귀 0 — Phase 01 baseline **568 비감소** (01b·02 추가 테스트 반영 시 증가 가능).
- [ ] Unity EditMode 회귀 0 — baseline 122 비감소.
- [ ] 봇 회귀 PASS (desync 0).
- [ ] **wire 무변경 최종 확인**: PDL 변경 0, `Protocol.Version` v13.
- [ ] `Validate()` git diff 0 (Phase 02 trust-boundary 기계 검증 최종 재확인).
- [ ] CHANGELOG entry + `_milestone-DONE.md` 박제.
- [ ] 각 복잡 Phase(02) `-DONE.md` 완비 확인.

**검증 흐름**: 메인이 전체 회귀 실행 → 정량값 박제 → CHANGELOG + 마일스톤 -DONE → (PR = 영호 GO).

---

## 위험 / 헌법 게이트

- **누적 회귀 0**: 개별 Phase green ≠ 전체 green 보장 안 함. 마지막 통합 회귀 필수.
- **PR = irreversible = 영호 GO**: 머지·push는 영호 명시 GO 게이트. 98_Shared 변경 commit에 Shared.dll 포함 시 03_Client CODEOWNERS(정유현) co-review.

> 마일스톤 완료 → work-pin M4.14 PHASE 블록 갱신 + 다음 마일스톤(영호 결정).
