---
owner: youngho
milestone: M5
phase: 24
title: 전체 WSL2 회귀 + 마일스톤 마감
status: pending
grade: 복잡
risk: irreversible
domain: qa
estimated: 1~2h
---

# Phase 24: 전체 WSL2 회귀 + 마일스톤 마감

> **상태**: pending
> **마일스톤**: M5
> **등급**: 복잡 (마감 — 전 도메인 회귀 통합 + 박제 + PR 게이트). risk: irreversible (v15 wire-break + main 머지).
> **담당**: youngho (qa + 메인 세션)

---

## 🎯 목표

파티/퀘스트/게이트/포탈/콘텐츠 전 트랙을 WSL2 전체 회귀(xUnit + 봇 fresh) + Unity 컴파일 0err로 정량 증명하고, `_milestone-DONE.md` + HTML로 박제한 뒤, PR 게이트(영호 명시 GO)까지 마감한다. v15 wire-break라 PR/머지/디스코드 공지는 **영호 GO 게이트** 의무.

---

## ⏪ 사전 조건

- [ ] Phase 01~23 전부 완료 (전 트랙 A/Q/B/P/C/R).

---

## 📝 작업 내용

- [ ] WSL2 full 회귀 (ADR-029): `dotnet test` 전체 green + 봇 fresh 전건(`PartyQuestSmoke`/`BossGateSmoke` 포함).
- [ ] Unity 컴파일 0err (메인 세션 MCP).
- [ ] reviewer 자동 통합 점검 — 🔴 0.
- [ ] `ProtocolVersion.Current == 15` 정합 확인 (서버/클라 핸드셰이크 — A0/Phase 01 토대).
- [ ] 마일스톤 `_milestone-DONE.md` + **HTML 시각화** 박제 (ADR-031, 복잡 임계 — 5단계 보고 구조 내장). 복잡+ Phase 개별 `-DONE.md`도.
- [ ] CHANGELOG entry ([H] — `ProtocolVersion` v14→v15 bump = wire 변경 + 비가역. 파티/퀘스트/게이트/포탈/콘텐츠 거동 변경 인지. 영호 박제).
- [ ] work-pin 갱신 (마일스톤 마감 좌표).
- [ ] **PR 게이트** — `gh pr create` + 머지는 **영호 명시 GO** 의무. Shared.dll commit 포함 시 03_Client CODEOWNERS(정유현) co-review 트리거 → admin bypass(영호 GO) 또는 정유현 정식 리뷰 중 영호 결정.

---

## ✅ 완료 조건 (정량)

- [ ] WSL2 `dotnet test` 전체 green.
- [ ] 봇 fresh 전건 통과 (`PartyQuestSmoke`/`BossGateSmoke` 포함).
- [ ] Unity 컴파일 0err.
- [ ] reviewer 🔴 0.
- [ ] `ProtocolVersion.Current == 15` 정합.
- [ ] `_milestone-DONE.md` + HTML 박제 (phase-gate-validator 통과).
- [ ] CHANGELOG([H] v15 bump) + work-pin 갱신.
- [ ] PR 생성·머지 = 영호 명시 GO 후 (v15 비가역 + Shared.dll co-review 경로 결정).

---

## 🧪 테스트

**자동**: WSL2 full `dotnet test` + 봇 전 시나리오(기존 18 + PartyQuestSmoke + BossGateSmoke).
**수동**: 영호 Play-test 풀 루프 — 파티 초대/수락, 멤버 HUD, 40킬 카운터, 게이트 토스트, 양방향 포탈, 일반몹 공격, 이펙트, NPC 대사, StageClear 애니.

---

## 📚 학습 포인트

- **회귀 = 거동 보존의 정량 증명** — 신규 시스템(파티/퀘스트) 도입 후에도 기존 테스트 green = "추가했지만 기존 안 깼다"의 객관 근거.
- **v15 wire-break = 비가역 게이트** — `ProtocolVersion` bump는 v15 미재빌드 클라(유현/인규) 접속 불가를 의미. 그래서 PR/머지/공지는 자동이 아니라 영호 명시 GO (헌법 비가역 Stop①).

---

## ⚠️ 함정 / 주의사항

- **irreversible** — PR 생성/머지 + **디스코드 v15 wire-break 공지 = 영호 명시 GO**(AI 자율 X). 디스코드 공지는 영호 직접(메모리 규칙).
- **Shared.dll co-review** — Shared.dll을 commit에 포함하면 03_Client CODEOWNERS(정유현) co-review 트리거 → admin bypass(영호 GO). PR body에 보안 키워드 literal 박지 않기(풀어쓰기).
- **마감 경고 프레이밍 금지** — "마감인데 큰일나요" 식 X (메모리 규칙). 일정은 영호 컨트롤.
- HTML 박제는 `_milestone-DONE.md`보다 *먼저* (phase-gate-validator가 복잡 이상에 HTML 페어 의무 — ADR-031).
- 봇 연속 FAIL ≠ 회귀 — fresh 재검이 판정.

---

## ➡️ 다음 Phase

- 마일스톤 종료 → `/session:end` → `/session:log`.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `24-...-DONE.md` + 마일스톤 `_milestone-DONE.md` + HTML (ADR-031). 마일스톤 총 대규모 → 종합 박제에 전 트랙(A/Q/B/P/C/R) 포함.

---

## 작업 로그

- 2026-06-14: 생성.
