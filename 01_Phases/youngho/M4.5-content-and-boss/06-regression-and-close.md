---
owner: youngho
milestone: M4.5
phase: 06
title: M4.5 회귀 + 마감 — cross-review + 발표 풀 루프
status: done
grade: 보통
risk: irreversible
estimated: 1~2h
domain: qa
---

# Phase 06: M4.5 회귀 + 마감

> **상태**: done (2026-06-07 세션23 — PR 머지만 사용자 GO 대기)
> **마일스톤**: M4.5
> **등급**: 보통 (qa + 마감. PR 머지 시 irreversible 깃발)
> **담당**: qa SubAgent + 메인 세션

---

## 🎯 목표

M4.5 전체(콘텐츠 2 + UI 1 + 보스 2)를 통합 회귀 검증하고 PR 머지로 닫는다. 마일스톤 등급이 대규모이므로 `_milestone-DONE.md` + 5단계 보고 MD/HTML을 마일스톤 단위로 박는다. **발표 데모 풀 루프**가 본 마일스톤의 최종 수확.

---

## ⏪ 사전 조건

- [ ] Phase 01~05 전부 완료

---

## 📝 작업 내용

- [x] **보스 공격 모션 Start→End 미전이 봉합** (`897a90d`) — 원인 = Start 클립 1.333s > AttackLatch 1.2s(16+8틱) → Any State→Idle이 Start를 끊음. Animator 속도 정합(Start 1.6667/End 3.3333) + ComputeBossAnimState 우선순위 Death>Attack>Hit(보스 한정) + 테스트 2개. Play 실측 통과
- [x] 전체 회귀 (WSL2 = ADR-029 표준): 클린빌드 0/0 + `dotnet test --no-build` 419/0/4 + 봇 7종 PASS (캐비앗: 보스 무리스폰 → 보스 시나리오는 서버당 1회)
- [x] `/cross-review` — α 🔴0/🟡1 + β 1차 6건 봉합(`2271025` — 핵심: 사망 HUD 0 고착) + β 2차 신규 1건(같은 프레임 덮어쓰기) 봉합 + β 3차 수렴 GO. 산출물 3건 = `00_Document/reviews/2026-06-07-cross-review-m4.5-phase04-combat-v9.md` 외
- [x] **발표 데모 풀 루프 Play 실측** — 2클라 전 구간 이상 무 (사망 HP 0 표시 → 암전 → 부활 full 포함)
- [x] ProtocolVersion == 9 최종 확인 — 본 브랜치 98_Shared diff 0 (04 유일 bump 약속 이행)
- [x] CHANGELOG entry [M]
- [ ] PR 생성·머지 — **사용자 명시 GO 게이트** (irreversible)
- [x] 마일스톤 5단계 보고 MD/HTML (`_milestone-DONE.md` + `.html`)
- [x] work-pin 갱신 (마감 흐름에서 지속 갱신 — 머지 후 M4.5 MERGED 최종 반영)

---

## ✅ 완료 조건

- [ ] 보스 공격 모션 Start→End 전이가 이펙트 타이밍과 정합 (Play 실측)
- [ ] `dotnet test` 전부 green (회귀 0) + 봇 전 시나리오 PASS
- [ ] 발표 데모 풀 루프 무사고 (2클라 포함)
- [ ] ProtocolVersion == 9 + CHANGELOG + PR 머지 (사용자 GO) + 5단계 보고 박힘
- [ ] work-pin = M4.5 MERGED 반영

---

## 🧪 테스트

**자동**: 전체 `dotnet test` + 봇 전 시나리오
**수동**: 발표 데모 풀 루프 (위 Play 실측 시나리오)

---

## 📚 학습 포인트

- **데모 풀 루프 = 통합의 최종 시험** — 단위/봇이 못 잡는 "이어 붙인 경험"의 결함(전환 타이밍, 연출 겹침)은 풀 루프에서만 드러남

---

## ⚠️ 함정 / 주의사항

- PR 머지 = 사용자 GO 의무 + 예외 경로 절차 (사유 코멘트 + 환경변수 — PR #67 검증 경로)
- 증분빌드 거짓실패 — 클린빌드 후 test
- v9 bump 마일스톤 — CHANGELOG에 팀원 재빌드 의무 명시 (M4.1 v5 선례)

---

## ➡️ 다음 마일스톤

- **M5 Persistence** — 정식 분해는 본 Phase 마감 + LocalDB Linux 부재 결정(ADR-029 트레이드오프 ④) 후 `/work:plan`

---

## 📋 박제 (완료 후)

- **마일스톤 대규모** — `_milestone-DONE.md` + 5단계 보고 MD/HTML

---

## 작업 로그

- 2026-06-07: 계획 수립 (`/work:plan M4.5`, 세션18)
- 2026-06-07: Phase 05 Play 실측 발견분 흡수 (세션22) — 보스 모션 Start→End 미전이 봉합 항목 추가 (사용자 지시 "다음 phase에서 잡자")
- 2026-06-07: 본대 실행 (세션23) — 보스 모션 봉합(`897a90d`, Play 통과) → 회귀 풀세트(419/0/4 + 봇 7종) → cross-review γ 3라운드(β 발견 사망 HUD 0 고착 봉합 `2271025`) → 데모 풀 루프 2클라 통과 → 박제(MD/HTML) + CHANGELOG [M]. 남은 것 = PR 머지(사용자 GO)
