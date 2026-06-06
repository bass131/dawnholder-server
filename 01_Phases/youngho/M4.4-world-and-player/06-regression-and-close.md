---
owner: youngho
milestone: M4.4
phase: 06
title: M4.4 회귀 + 마감
status: pending
grade: 보통
risk: irreversible
estimated: 1~2h
domain: qa
summary: 지형+직업 통합 회귀 + 체감 실측 + PR 머지로 M4.4를 닫는다
---

# Phase 06: M4.4 회귀 + 마감

> **상태**: pending
> **마일스톤**: M4.4
> **등급**: 보통 (qa + 마감. PR 머지 시 irreversible 깃발)
> **담당**: qa SubAgent + 메인 세션

---

## 🎯 목표

M4.4 전체(지형 3 + 직업 2)를 통합 회귀 검증하고 PR 머지로 닫는다. 마일스톤 등급이 대규모이므로 **-DONE.md + 5단계 보고**는 마일스톤 단위로 박는다.

---

## ⏪ 사전 조건

- [ ] Phase 01~05 전부 완료

---

## 📝 작업 내용

- [ ] 전체 회귀 — `dotnet build Dawnholder.slnx --no-incremental` + `dotnet test --no-build`
- [ ] 헤드리스 봇 전 시나리오 PASS (지형 맵 이동 포함)
- [ ] 통합 실측 (Play): 직업 2종 × 세 씬 지형 — 이동/점프/공격/맵 전환
- [ ] ProtocolVersion == 8 최종 확인 (bump 0 검증)
- [ ] CHANGELOG entry ([M] — 지형 충돌 + 직업 분리, Physics.Step 시그니처 변경 = 모든 팀원 pull 후 재빌드)
- [ ] PR 생성·머지 — **사용자 명시 GO 게이트** (irreversible)
- [ ] 마일스톤 5단계 보고 MD/HTML (대규모 — 캡스톤 평가 자산: 지형 bake 파이프라인 + 직업 장착 구조가 어필 포인트)
- [ ] work-pin 갱신 (M4.4 MERGED + M4.5 정식 분해 트리거 — 유현 UI 문서 입수 확인)

---

## ✅ 완료 조건

- [ ] `dotnet test` 전부 green (회귀 0)
- [ ] 봇 시나리오 전부 PASS
- [ ] 직업 2종 × 세 씬 Play 무사고
- [ ] CHANGELOG + PR 머지 (사용자 GO) + 5단계 보고 박힘
- [ ] work-pin = M4.4 MERGED 반영

---

## 🧪 테스트

**자동**: 전체 `dotnet test` + 봇 전 시나리오
**수동**: 직업 2종 풀 플레이 매트릭스

---

## 📚 학습 포인트

- **통합 회귀의 층위** — 단위(물리) → 봇(서버 일치) → Play(체감)의 3단 검증이 각각 잡는 결함이 다름

---

## ⚠️ 함정 / 주의사항

- PR 머지 = 사용자 GO 의무. admin bypass 시 사유 코멘트 + `CLAUDE_ADMIN_BYPASS_REASON` 절차 (PR #60 검증 경로)
- 증분빌드 거짓실패 — 클린빌드 후 test
- Discord 공지 누적분 같이 처리 권유 (M4.1~ 미발송 의무)

---

## ➡️ 다음 마일스톤

- **M4.5 content-and-boss** — 정식 분해는 본 Phase 마감 + 유현 UI 문서 입수 후 `/work:plan`

---

## 📋 박제 (완료 후)

- **마일스톤 대규모** — `_milestone-DONE.md` + 5단계 보고 MD/HTML

---

## 작업 로그

- 2026-06-06: 계획 수립 (`/work:plan M4.4`)
