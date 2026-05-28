---
owner: youngho
milestone: M4.2
phase: 05
title: 통합 검증 + 봇 맵 이동 시나리오 + 마일스톤 마감
status: done
grade: 보통
estimated: 1.5~2h
domain: cross
---

# Phase 05: 통합 검증 + 봇 맵 이동 시나리오 + 마일스톤 마감

> **상태**: pending
> **마일스톤**: M4.2
> **등급**: 보통 (qa + server / 회귀 안전망 + 마감 의례)
> **담당**: qa SubAgent (봇 시나리오) + server SubAgent (회귀 점검)

---

## 🎯 목표

맵 전환 end-to-end를 **헤드리스 봇 시나리오 + 통합 테스트**로 회귀 안전망에 박고,
M4.2 마일스톤을 마감(`-DONE.md` + CHANGELOG)한다.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 — 서버 migration 로직 (봇이 검증할 대상)
- [ ] Phase 04 완료 — 클라 dispatch (수동 데모 흐름 확인용)

---

## 📝 작업 내용

- [ ] `99_Tools/headless-bot/Scenarios/`에 맵 이동 시나리오 추가
  - 결정론 시나리오: 접속 → Town → portal 이동 → HuntingGround → 공격 → portal → 복귀
  - state 보존 assert (이동 전후 HP/stats)
- [ ] **⚠️ Phase 01 Skip 처리한 smoke 2건 복구 (가짜 약속 방지)**: `LagSimIntegrationTests`의
  `CombatSmoke_ZeroLag_Succeeds` / `BossSmoke_ZeroLag_Succeeds`는 Phase 01 맵 분리(Town=빈 맵)로
  Skip됨. 본 Phase에서 봇이 portal로 HuntingGround/BossRoom 이동 후 전투하는 흐름으로 **Skip 해제 + 복구**.
  복구 못 하면 Skip 사유를 갱신(왜 못 했는지)하고 별 시점에 박음 — 조용히 Skip 유지 금지.
- [ ] `02_Server/GameServer.Tests/Integration/`에 맵 이동 통합 테스트 (ServerFixture port 0)
- [ ] **전체 회귀**: `dotnet test Dawnholder.slnx` green (M4.1 221통과 baseline 유지 + 신규)
- [ ] **reviewer SubAgent Tier 2-A 통합 점검** (plan-auditor 2026-05-25 🔴 — 비가역 PDL bump 동원 패턴):
      헌법/ADR/ARCHITECTURE/테스트/도메인 패턴 5축. PDL bump가 irreversible 깃발이라 한 사람 머리로 마감 X.
- [ ] `_milestone-DONE.md` 작성 (복잡 등급 — 사실 박제 + 학습 키워드)
- [ ] CHANGELOG entry — **[M]** (PDL bump 5→6 + 모든 팀원 빌드 영향: 다음 pull 시 Shared.dll 재빌드)
- [ ] work-pin 갱신 — M4.2 마감 → M4.3 진입 대기

---

## ✅ 완료 조건

- [ ] 헤드리스 봇 맵 이동 시나리오 PASS (결정론 — 매 실행 동일 결과)
- [ ] 맵 이동 통합 테스트 green (서버 핸드오프 end-to-end)
- [ ] **`dotnet test` 전체 green** — 회귀 0 (M4.1 baseline 대비 통과 수 ≥)
- [ ] `_milestone-DONE.md` 박힘 (phase-gate-validator.sh frontmatter 검사 통과)
- [ ] CHANGELOG [M] entry 박힘
- [ ] (수동) Unity Play 모드 4맵 전환 데모 흐름 1회 완주 확인

---

## 🧪 테스트

**자동**:
- `MapTransitionScenario` (headless-bot) — 결정론 왕복 이동 + state assert
- `MapTransitionIntegrationTests` — ServerFixture + 봇 시나리오 p99 회귀 0
- 전체 `dotnet test` 회귀

**수동**:
- Unity 4맵 전환 데모 영상 가능 상태 확인 (캡스톤 1 발표 자산)

---

## 📚 학습 포인트

- **회귀 안전망의 가치**: 맵 이동 같은 복합 흐름은 수동 테스트로 매번 확인하기 비쌈 →
  결정론 봇 시나리오로 자동화하면 이후 변경 시 깨짐을 즉시 포착.
- **통합 테스트 vs 단위 테스트**: 단위(Phase 03)는 migration 함수 1개, 통합은 소켓~핸드오프
  전체 경로. 둘 다 있어야 안전.

---

## ⚠️ 함정 / 주의사항

- **결정론 깨짐**: 봇 시나리오에 타이밍 의존(실시간 sleep) 박으면 flaky. tick 기반 결정론 유지.
- CHANGELOG 등급 — PDL bump는 모든 팀원 빌드에 영향이라 **[M] 이상** (단순 [L] 아님).
- 마감 시 `/session:end`로 commit + PR 게이트 — PR 생성/머지는 **사용자 명시 GO** 의무 (헌법).

---

## ➡️ 다음 마일스톤

- **M4.3 — AI + Polish** (enemy AI + boss behavior + jump Y mispredict 봉합 +
  **cheat-flag + Serilog 이월분** + PvP ADR). 캡스톤 1 발표 후.

---

## 📋 박제 (완료 후)

- **마일스톤 마감** — `_milestone-DONE.md` (복잡 등급, 사실 박제 + 학습 키워드).
  대규모 아니므로 5단계 HTML 보고는 선택 (캡스톤 자산 필요 시만).

---

## 작업 로그

- 2026-05-25: 계획 수립 (`/work:plan M4.2`)
- 2026-05-28: 마감 완료. qa SubAgent 위임으로 4 묶음(봇 portal 흐름 / `MapTransitionScenario` 신설 / smoke 2건 Skip 해제 / 통합 테스트) 박음. reviewer Tier 2-A 통합 점검 🔴0/🟡3 통과(🟡 1·2 ARCHITECTURE.md L212 stale + M4.2 결과 절 누락 → 본 마감 commit 동반 봉합, 🟡 3 봇 portal const 중복 → M4.3 backlog). `dotnet test` 300통과 / 0실패 / 4Skip (M4.1 baseline 221 → +79). `_milestone-DONE.md` 박제 완료.
</content>
