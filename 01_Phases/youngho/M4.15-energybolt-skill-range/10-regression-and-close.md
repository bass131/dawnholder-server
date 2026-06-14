---
owner: youngho
milestone: M4.15
phase: 09
title: 회귀 + 봇 시나리오 갱신 + 마일스톤 마감
status: pending
grade: 복잡
domain: qa
summary: FreezeSmoke 등 봇 시나리오 freeze-제거 정합 갱신 + 텔레포트(v14/4방향) 회귀 + WSL2 전 회귀 + -DONE.md/HTML 박제 + PR 게이트
---

# Phase 09: 회귀 + 봇 시나리오 갱신 + 마일스톤 마감

> **상태**: pending
> **마일스톤**: M4.15
> **등급**: 복잡 (마감 — 전 도메인 회귀 통합 + 박제 + PR 게이트). 마일스톤 총 등급은 워크스트림 D(텔레포트 v14) 합류로 **대규모**.
> **담당**: qa + 메인 세션

---

## 🎯 목표

freeze 제거·범위 변경·투사체 모델에 봇 시나리오를 정합시키고, WSL2 전체 회귀 + Unity 컴파일 0err로 마일스톤 거동을 정량 증명한 뒤, `-DONE.md` + HTML로 박제하고 PR 게이트(영호 GO)까지 마감한다.

---

## ⏪ 사전 조건

- [ ] Phase 01~08 전부 완료 (워크스트림 A/B/C + D 텔레포트 포함).

---

## 📝 작업 내용

- [ ] 봇 시나리오 갱신 (`99_Tools/headless-bot/Scenarios/`):
  - `FreezeSmoke` — "freeze 됨" 검증 → **"freeze 안 됨"(적 정상 이동)** 으로 전환 *또는* 은퇴 (사유 박음).
  - `RangedHitSmoke`/`RangedWhiffSmoke` — Y 범위·투사체 속도 변경 정합 (사거리 가정 갱신).
  - `ThunderboltAoe` — Y 재튜닝 정합.
  - (텔레포트) **봇 시나리오 신규 안 만듦** (plan-auditor 봉합 — 결정 확정). 사유: 4방향 텔레포트는 *결정론적 위치 산출*이라 `MageTeleportTests` 단위 테스트(P07: 4방향/거리/경계/whitelist)가 봇보다 강한 증명. 기존 텔레포트 봇 시나리오가 *있으면* 거리 5.0만 정합 갱신, 없으면 신규 X.
- [ ] WSL2 full 회귀 (ADR-029): `dotnet build` 0/0 + `dotnet test` green + 봇 시나리오 회귀 0 (fresh 재검 carry-over 적용).
- [ ] Unity 컴파일 0err + EditMode 회귀 0 (메인 세션 MCP).
- [ ] 마일스톤 `-DONE.md` + **HTML 시각화** 박제 (ADR-031, 복잡 임계 — 5단계 보고 구조 내장). 복잡+ Phase(02·06·07·09) 개별 `-DONE.md`도.
- [ ] CHANGELOG entry ([H] — **`Protocol.Version` v13→v14 bump**은 wire 변경 + 비가역이라 위험도 상향. 거동 변경(freeze 제거/범위/투사체/텔레포트 4방향) 인지. 영호 박제).
- [ ] work-pin 갱신 (마일스톤 마감 좌표).
- [ ] **PR 게이트** — `gh pr create` + 머지는 **영호 명시 GO** 의무 (irreversible: v14 bump + main 머지). **Shared.dll co-review**: v14로 `C_SkillUse` 변경 → 클라가 새 dll 필요 → Shared.dll commit 포함 → 03_Client CODEOWNERS(정유현) co-review 트리거. admin bypass(영호 GO + CLAUDE_ADMIN_BYPASS_REASON) 또는 정유현 정식 리뷰 중 영호 결정.

---

## ✅ 완료 조건

- [ ] 봇 시나리오 freeze/range/투사체 정합 갱신 + 회귀 0.
- [ ] WSL2 `dotnet test` green (baseline 570 기준 ± 신규/갱신 테스트 — 텔레포트 4방향/거리/경계 케이스 포함).
- [ ] Unity 컴파일 0err + EditMode 회귀 0.
- [ ] `ProtocolVersion.Current == 14` 정합 (서버/클라 핸드셰이크 — P06 토대).
- [ ] `-DONE.md` + HTML 박제 (phase-gate-validator 통과).
- [ ] CHANGELOG([H] v14 bump) + work-pin 갱신.
- [ ] PR 생성·머지 = 영호 명시 GO 후 (v14 비가역 + Shared.dll co-review 경로 결정).

---

## 🧪 테스트

**자동**: WSL2 full `dotnet test` + 봇 16+ 시나리오.
**수동**: 영호 데모 Play — 에너지 볼트 일정 속도 + Y범위 층 한정 + freeze 없음 풀 루프.

---

## 📚 학습 포인트

- **회귀 = 거동 보존의 정량 증명** — wire 무변경 + 테스트 green = "구조 바꿨지만 의도대로"의 객관 근거.
- **봇 시나리오는 살아있는 명세** — 거동을 바꾸면 *기대도* 바꿔야 함. FreezeSmoke "freeze 됨" → "안 됨" 전환이 그 예 (테스트가 새 계약을 박제).

---

## ⚠️ 함정 / 주의사항

- 봇 연속 FAIL ≠ 회귀 — `run_bot_fresh_recheck.sh` fresh 단독 재검이 판정 (carry-over).
- PR = irreversible — 영호 GO 없이 자율 생성/머지 금지. PR body에 보안 키워드 literal 금지.
- HTML 박제는 `-DONE.md`보다 *먼저* (phase-gate-validator가 복잡 이상에 HTML 페어 의무 — ADR-031).

---

## ➡️ 다음 Phase

- 마일스톤 종료 → `/session:end` → `/session:log`.

---

## 📋 박제 (완료 후)

- 복잡 등급 → `09-...-DONE.md` + 마일스톤 `_milestone-DONE.md` + HTML (ADR-031). 마일스톤 총 대규모 → 종합 박제에 워크스트림 A/B/C/D(텔레포트 v14) 전부 포함.

---

## 작업 로그

- 2026-06-14: 생성.
