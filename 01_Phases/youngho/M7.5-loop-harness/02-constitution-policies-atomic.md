---
owner: youngho
milestone: M7.5
phase: 02
title: 헌법 + policies 5개 강결합 atomic REVISE (운영모드 키스톤)
status: pending
grade: 복잡
estimated: 3~4h
domain: cross
summary: CLAUDE.md 6절 + 5정책 동시 REVISE + pr-and-merge-gate. 강결합이라 한 Phase atomic.
---

# Phase 02: 헌법 + policies 5개 강결합 atomic

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 복잡 · **담당**: 메인 직접 + reviewer

## 🎯 목표
loop-driven 운영모드를 헌법과 정책에 반영한다. 정책 5개가 §동기화 책임으로 순환 참조 → **한 Phase에서 atomic** 수정(하나만 고치면 drift).

## ⏪ 사전 조건
- [ ] P01 완료 (loop-driver/work-judge/review-throughput 존재 — 포인터 대상)

## 📝 작업 내용
- [ ] **CLAUDE.md 6절 REVISE**: 작업 보고 / 작업 좌표+Phase 박제 / 01_Phases Phase 진행 / 작업 등급+위험깃발 / SubAgent 풀 / Knowledge — loop-driven 운영모드 한 단락 + 새 정책 포인터
- [ ] **policies 5개 atomic**:
  - `reporting-format`: 루프 자율(a)=원장/배치 적재(pull) vs 사람 게이트(c)=즉시 surface 분기
  - `pin-and-done`: 핀 선택 필드 버킷(a/b/c)+사람대기 + 원장 참조 + 갱신 주체 루프 엔진
  - `review-tiering`: Tier 0(기계 게이트=무조건 자율) + throughput 절
  - `subagent-routing`: 진입 주체 메인→메인/루프 드라이버 병기
  - `grade-and-risk`: 판정자 3버킷 절 + 깃발→버킷 매핑
- [ ] `pr-and-merge-gate` REVISE: settings 권한 승격 ↔ §5 ask 매처 정합(약화 0)
- [ ] §동기화 책임 표 상호 정합 점검(5정책+헌법 링크)

## ✅ 완료 조건
- [ ] 헌법 6절 + 6정책 REVISE 완료
- [ ] 순환 참조 dangling 0(전수 grep)
- [ ] **ask(gh pr merge/create) 사람 게이트 약화 0** (pr-and-merge-gate git diff 확인)
- [ ] reviewer 헌법 위반 🔴 0

## 📚 학습 포인트
- 강결합 문서의 atomic 수정 — 부분 수정 = drift
- 동기화 책임 그래프(왜 5개가 한 묶음인가)

## ⚠️ 함정
- 5정책 중 하나만 고치고 멈추면 §동기화 책임 깨짐 → 반드시 한 묶음.
- 헌법 절대원칙 5개는 *건드리지 않음* — 운영 레이어만.

## ➡️ 다음 Phase
- P03 (agents) ∥ P04 (commands) — 둘 다 이 정책층 의존
