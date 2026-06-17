---
owner: youngho
milestone: M7.5
phase: 06
title: 카탈로그 + 옛 ADR 상태줄 + stale drift 마감 + reviewer 통합
status: pending
grade: 보통
estimated: 1~2h
domain: cross
summary: 카탈로그 3종 atomic + ADR-032 등록 + 옛 ADR 상태줄 + drift 정정 + 통합 점검 + -DONE.md/HTML.
---

# Phase 06: 카탈로그 + drift 마감 + 통합

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 보통 · **담당**: 메인 + reviewer

## 🎯 목표
내용이 다 정착된 뒤 *마지막에 atomic 등록*. 카탈로그·상태줄·drift를 한 번에 정합화하고 마일스톤 마감.

## ⏪ 사전 조건
- [ ] P01~P05 전부 완료 (등록할 내용이 모두 존재)

## 📝 작업 내용
- [ ] **카탈로그 3종 atomic**: `policies/INDEX.md`(새 정책 행) · `ADR/INDEX.md`(harness 행 + 016/023 supersede 표기) · `commands-index.md`(새 슬래시 + 10 vs 11 drift 봉합)
- [ ] **ADR-032 등록**: ADR/INDEX harness 행 + `ADR.md` 후보표/카운트(:18 tech-stack ADR-026 누락→10, :20 harness "8개"→17개 번호 나열) + `ADR_History.md` 한 줄
- [ ] **옛 ADR 상태줄**: 022/019/016/023 본문 끝에 "(부분 superseded — ADR-032)" 한 줄(append-only)
- [ ] **stale drift**: `.claude/templates/{done-md-template,pin-template}` (ADR-031/025 잔재) · `.claude/setup-steps/04-finalize.md` (옛 work-pin 시드)
- [ ] **전수 dangling grep** + **게임 코드 git diff 0 확인**
- [ ] reviewer 통합 점검(헌법/ADR/도메인 패턴)
- [ ] `_milestone-DONE.md` + HTML 시각화(대규모, 5단계 보고 구조)

## ✅ 완료 조건
- [ ] 카탈로그 3종 정합 + ADR-032 정식 등록 + 옛 ADR 상태줄 표기
- [ ] dangling 참조 0(전수 grep) · 게임 코드 변경 0
- [ ] reviewer 🔴 0
- [ ] `_milestone-DONE.{md,html}` 박제(phase-gate-validator 통과)

## 📚 학습 포인트
- 마일스톤 마감 = 사실 박제 + 캡스톤 평가 자산(HTML)
- 카탈로그 정합(왜 3종 atomic인가)

## ⚠️ 함정
- 카탈로그 3종 중 하나만 고치면 drift — atomic.
- ADR.md 카운트는 매직넘버 말고 *번호 나열*(다음 ADR이 또 틀리지 않게 — plan-auditor 학습 포인트).

## ➡️ 다음
- 마일스톤 마감 → PR 생성/머지 = **영호 명시 GO**(비가역 게이트)
