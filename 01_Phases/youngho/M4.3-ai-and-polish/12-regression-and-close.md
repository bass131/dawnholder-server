---
owner: youngho
milestone: M4.3
phase: 12
title: M4.3 회귀 테스트 + 가벼운 마감 (5단계 보고 X)
status: pending
grade: 보통
risk: irreversible
estimated: 1~2h
domain: qa
---

# Phase 12: M4.3 회귀 테스트 + 가벼운 마감

> **상태**: pending
> **마일스톤**: M4.3
> **등급**: 보통 (qa + 마감. PR 머지 시 irreversible 깃발)
> **담당**: qa SubAgent + 메인 세션 (마감 의례)

---

## 🎯 목표

M4.3 전체(Phase 07~11)를 통합 회귀 검증하고 **가볍게 마감**한다. **5단계 보고/M4 종합 마감 의례는 박지 않는다** (2026-05-29 의논 — M4.3는 "M4 마감"이 아니라 발표용 polish 마일스톤). 전체 테스트 green + 발표 데모 시나리오 한 번 통째로 돌려보고, PR 머지 + work-pin 갱신으로 닫는다.

---

## ⏪ 사전 조건

- [ ] **Phase 07~11 전부 완료** (enemy AI 서버/클라 + boss + 움직임 polish + RemotePlayer 외관)

---

## 📝 작업 내용

- [ ] 전체 회귀 — `dotnet build Dawnholder.slnx` + `dotnet test --no-incremental` (※ 증분빌드 거짓실패 주의 — work-pin 학습)
- [ ] 헤드리스 봇 시나리오 통합 점검 — `EnemyAiSmoke` + `BossFightSmoke` + 기존 시나리오 전부 PASS
- [ ] **발표 데모 시나리오 1회 풀 리허설** (Play): 마을 → 사냥터(적 patrol/chase 처치) → 보스방(보스 패턴전 → 페이즈 2 → 처치 → StageClear) → 엔딩. 클래스별 속도 + 점프 + 멀티 RemotePlayer 애니 체감
- [ ] `Protocol.Version` 최종 == 8 확인 (07: 6→7, 09: 7→8)
- [ ] CHANGELOG entry 박음 ([M] — enemy AI 도입 + boss behavior + PDL 6→8, 모든 팀원 영향)
- [ ] PR 생성 — **사용자 명시 GO 게이트** (irreversible, 헌법). PvP/cloud 등 보안 키워드 literal 금지
- [ ] work-pin 갱신 (M4.3 MERGED + M4.4 보안 마일스톤 진입 대기)

### 박지 않는 것 (명시)
- ❌ 5단계 보고 MD/HTML (대규모 마감 의례 — M4.3는 보통 마감)
- ❌ M4 전체 종합 보고 (보안 끝난 진짜 M4 마감 때)
- ❌ `_milestone-DONE.md` 5단계 — 가벼운 마감 요약만 (또는 생략, 사용자 결정)

---

## ✅ 완료 조건

- [ ] `dotnet test --no-incremental` 전부 green (회귀 0)
- [ ] 헤드리스 봇 enemy AI + boss fight 시나리오 PASS
- [ ] 발표 데모 풀 시나리오 Play 1회 무사고 (멈춤/크래시 0)
- [ ] CHANGELOG 갱신 + PR 머지 (사용자 GO 후)
- [ ] work-pin = M4.3 MERGED 반영

---

## 🧪 테스트

**자동**:
- 전체 `dotnet test` + 헤드리스 봇 전 시나리오

**수동**:
- 발표 데모 풀 루프 Play 리허설 (캡스톤 1 발표 6/10 대비)

---

## 📚 학습 포인트

- **마일스톤 마감의 무게 조절**: 모든 마일스톤에 5단계 보고를 박지 않는다. M4.3는 발표용 polish라 가볍게. 마감 의례 비용도 trade-off (헌법 등급별 보고 정신).
- **통합 회귀의 의미**: Phase별로는 통과해도 합쳐서 깨질 수 있음(특히 PDL 2회 bump + enemy/boss 상호작용). 마지막에 통째 한 번.

---

## ⚠️ 함정 / 주의사항

- **PR 머지 = irreversible + 사용자 GO 의무** (헌법 / pr-and-merge-gate): AI 자율 머지 금지. admin bypass 시 사유 박고 사후 Discord 공지 의무.
- **PDL 누적 bump 검증**: 07/09에서 각각 bump했으니 최종 Version 8 + 모든 신규 패킷 ID stable 확인. stale 클라 cutoff 정상 동작.
- **증분빌드 거짓실패** (work-pin): `dotnet test` 전 `--no-incremental` 클린빌드 — GameWorld 싱글톤 거짓 실패 회피.
- **발표 데모는 "지금 main"도 백업**: M4.3가 발표 직전 불안정하면, 안전하게 M4.2까지의 main으로 발표 가능 (Phase 07~11 필수/nice-to-have 분리가 보험).

---

## ➡️ 다음 마일스톤

- **M4.4(가칭) — 보안 hardening** (cheat-flag + Serilog + PvP ADR + γ10 잔여). 또는 발표 후 PRD 재정합으로 로드맵 확정.

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit message. `_milestone-DONE.md`는 가벼운 요약만(또는 생략 — 사용자 결정). 5단계 보고 X.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan M4.3`)
