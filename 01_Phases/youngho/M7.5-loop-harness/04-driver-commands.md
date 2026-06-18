---
owner: youngho
milestone: M7.5
phase: 04
title: 드라이버 슬래시(loop/goal) + commands 4 REVISE + 미결 #3
status: pending
grade: 복잡
estimated: 2~3h
domain: cross
summary: 신규 loop/goal 슬래시 + refactor-sweep 일반화 + session/work:plan 정합. P03과 병렬 가능.
---

# Phase 04: 드라이버 슬래시 + commands

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 복잡 · **담당**: 메인 직접 · **병렬**: P03과 동시 가능

## 🎯 목표
루프 엔진을 실제 슬래시로 구현(v1). refactor-sweep을 범용 드라이버의 "refactor 프리셋"으로 재정의.

## ⏪ 사전 조건
- [ ] P01 완료 (loop-driver 정책)
- [ ] P02 완료 (정책층)

## 📝 작업 내용
- [ ] **미결 #3 결정**: `loop`+`goal` 두 슬래시 vs 통합 하나 (인터벌 반복 vs 목표 도달 운영 차이가 실제 필요한가)
- [ ] `loop.md`/`goal.md` 신규(또는 통합): /goal=조건(WSL2 green+reviewer🔴0)까지 자율, /loop=인터벌. done 판사=WSL2 게이트
- [ ] `refactor-sweep.md` REVISE: "드라이버 refactor 프리셋"으로 재정의 — Step0~5 골격이 범용 드라이버로 추출됨 명시 + **G1~G9 안전 가드 보존**
- [ ] `session/start.md`·`session/end.md`: 루프 기동/마감 경로 정합
- [ ] `work/plan.md`: frontmatter loop-track 필드(auto-gate/human-visual/human-gate)

## ✅ 완료 조건
- [ ] 드라이버 슬래시 존재 + #3 결정 박음
- [ ] refactor-sweep가 드라이버 인스턴스로 재정의(안전 가드 무손)
- [ ] dangling 0

## 📚 학습 포인트
- /goal done 조건 설계(기계 판정 가능한 문장)
- 드라이버 = 엔진, 슬래시 = 기동

## ⚠️ 함정
- refactor-sweep의 G1~G9(전용 브랜치·회귀 게이트·push 금지 등) 한 줄도 약화 X.
- /goal 평가자는 트랜스크립트만 봄 → done은 WSL 게이트 출력이 트랜스크립트에 박히게.

## ✅ 실행 결정 (2026-06-18, 영호 게이트 통과)

- **미결 #3 결정**: 내장 `/loop`(간격·self-pace)·`Workflow`(도구)가 *이미 존재* → 중복 제작 X. 어긋나는 핵심(**외부 done 심판** — 내장 self-pace는 AI 자기판단이라 편향 위험)만 커스텀 신규. **별도 슬래시 + 폴더 네임스페이스로 내장과 구분** (영호 결정).
- **신설**: `.claude/commands/engine/goal.md` → **`/engine:goal`** (목표 도달형 드라이버, Step0~5 골격 + WSL2/dangling 게이트 = 외부 done 심판, v1 attended). `/engine:drive`는 생략(goal이 곧 범용 드라이버 — 군더더기).
- **REVISE**: `refactor-sweep` = `/engine:goal`의 *refactor 프리셋*(G1~G9 보존) / `loop-driver.md §2`(#3 반영) / `session/start`(세션 2종 포인터) / `session/end`(루프 마감 경로) / `work/plan`(frontmatter `loop_track` 필드).
- **신설(세션 2종 보강)**: `.claude/commands/session/review.md` → **`/session:review`** (pull 세션, pending-comprehension 소비).
- **P06 이월**: commands 카탈로그 카운트 — `/engine:goal` + `/session:review` 반영 (commands-index 10→12 + 옛 10 vs 11 drift 봉합).

## ➡️ 다음 Phase
- P05 (hooks+settings+원장)
