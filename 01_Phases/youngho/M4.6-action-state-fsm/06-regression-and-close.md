---
owner: youngho
milestone: M4.6
phase: 06
title: 회귀 + 마감 (cross-review + 봇 시나리오 + Play 실측 + PR + 5단계 보고)
status: pending
risk: irreversible
grade: 보통
estimated: 1~2h
domain: qa
---

# Phase 06: 회귀 + 마감

> **상태**: pending
> **마일스톤**: M4.6 — ActionState FSM
> **등급**: 보통 + **irreversible 깃발**(PR 머지) — 마일스톤 마감
> **담당**: qa (회귀) + 메인 세션 (PR 게이트 + 5단계 보고)

---

## 🎯 목표

마일스톤 전체를 **회귀 검증**하고 마감한다: 통일 State 구조가 셋(플레이어/몬스터/보스) 모두에서 회귀 없이 동작하는지 클린빌드 + 테스트 + 봇 + Play로 입증하고, cross-review(외부 시각)로 봉합 잔여를 잡고, PR 머지(사용자 GO) + 5단계 보고(대규모) 박제.

---

## ⏪ 사전 조건

- [ ] Phase 03 (클라 미러) + Phase 05 (보스) 완료 — 즉 전 Phase 종료

---

## 📝 작업 내용

- [ ] 클린빌드 (Windows `dotnet build` + WSL2 검증) — 경고 0 / 오류 0
- [ ] `dotnet test` 전수 (ADR-029 WSL2) — CI와 동일 수치 확인
- [ ] 헤드리스 봇 전 시나리오 — smoke/M2/MultiRoster/EmergencyCombat/BossFightSmoke/EnemyAi (desync 0.00). FSM 회귀 단언 추가 검토(qa)
- [ ] Play 실측 매트릭스 — 직업 2종 × 세 씬: **공격 중 멈춤 체감** + 몬스터 순찰/추격 + 보스 양방향 전투
- [ ] **cross-review** (Codex β 또는 외부) — 봉합이 새 결함 도입했는지 재실측 (carry-over 의무)
- [ ] **ProtocolVersion 9 불변** 최종 확인
- [ ] `_milestone-DONE.md` + `.html` 5단계 보고 (🎯/🤔/🛠️/🧪/➡️) — **HTML 페어 선존재** (phase-gate-validator)
- [ ] CHANGELOG 1줄 (pull 영향 — Shared.dll 재빌드 필요 여부 / v9 불변 명시)
- [ ] PR 생성 + 머지 (**사용자 명시 GO** — irreversible 게이트)

---

## ✅ 완료 조건

- [ ] 클린빌드 0/0 + `dotnet test` green (CI 동일 수치)
- [ ] 봇 전 시나리오 PASS (desync 0.00)
- [ ] Play 매트릭스 이상 무 — 특히 공격 중 이동 잠금 + rubber-band 0 체감
- [ ] cross-review 수렴 (🔴 0 / P0~P2 0)
- [ ] `_milestone-DONE.md` + `.html` 박제 (5단계, 대규모)
- [ ] PR MERGED (사용자 GO) + CHANGELOG 갱신

---

## 🧪 테스트

**자동**: 전체 회귀 (빌드 + 테스트 + 봇)
**수동**: Play 풀 루프 — 마을 → 사냥터(몬스터) → 보스방(양방향) — 공격 commit 체감 집중

---

## 📚 학습 포인트

- **회귀 안전망의 누적 가치** — 대규모 리팩토링을 desync/테스트/cross-review 3중으로 검증
- **봉합이 새 결함 도입** (carry-over 연속 실증) — 마감 전 β 재실측이 왜 의무인지

---

## ⚠️ 함정 / 주의사항

- 대규모 -DONE.md는 **HTML 페어가 선존재**해야 phase-gate-validator 통과 (carry-over)
- PR body에 admin bypass 같은 보안 키워드 literal 금지 (풀어쓰기)
- 보스 봇 시나리오 서버당 1회 — 회귀 돌릴 때 서버 재기동 순서 주의

---

## ➡️ 다음 마일스톤

- 외관/연출 디테일 또는 v10 구조급(HP 동기화 + 공격 이벤트 패킷) — 사용자 가닥

---

## 📋 박제 (완료 후)

- 보통이지만 **마일스톤 마감** → `_milestone-DONE.md` + `.html` 5단계 보고 (대규모 마일스톤 캡스톤 자산)

---

## 작업 로그

- 2026-06-08: 신설 (plan)
