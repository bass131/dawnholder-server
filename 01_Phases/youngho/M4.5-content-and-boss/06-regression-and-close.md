---
owner: youngho
milestone: M4.5
phase: 06
title: M4.5 회귀 + 마감 — cross-review + 발표 풀 루프
status: pending
grade: 보통
risk: irreversible
estimated: 1~2h
domain: qa
---

# Phase 06: M4.5 회귀 + 마감

> **상태**: pending
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

- [ ] 전체 회귀 (WSL2 = ADR-029 표준): 클린 빌드 + `dotnet test --no-build` + 헤드리스 봇 전 시나리오 (BossFightSmoke 포함)
- [ ] `/cross-review` — Phase 04 양방향 전투 + bump 묶음 외부 시각 재검증 (옛 09 plan-auditor 🟡 계승 권장)
- [ ] **발표 데모 풀 루프 Play 실측**: 메인 → 캐릭터 선택 → 마을(미니맵/맵 이름) → 사냥터(슬라임+골렘, HP 실감소) → 보스방(telegraph → 양방향 전투 → 사망/리스폰 → 처치 → StageClear) — 2클라(원격 직업 상호 확인) 포함
- [ ] ProtocolVersion == 9 최종 확인 (Phase 04 유일 bump — 04 외 bump 0 검증)
- [ ] CHANGELOG entry ([M] — v9 bump = 모든 팀원 pull 후 Shared.dll 재빌드 의무)
- [ ] PR 생성·머지 — **사용자 명시 GO 게이트** (irreversible)
- [ ] 마일스톤 5단계 보고 MD/HTML (`_milestone-DONE.md` + `.html` — 대규모. 헤더 정확 일치: `## AC 검증 결과`/`## 결정 흐름`, HTML 페어 선존재)
- [ ] work-pin 갱신 (M4.5 MERGED + 다음 마일스톤 트리거 — M5 영속화 진입 시 LocalDB Linux 결정 선행)

---

## ✅ 완료 조건

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
