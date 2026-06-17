---
owner: youngho
milestone: M7.5
phase: 01
title: 신규 정책 3종 토대 (단일 진실) + 미결 #2·#6 결정
status: pending
grade: 복잡
estimated: 2~3h
domain: cross
summary: loop-driver/work-judge/review-throughput 작성 — REVISE가 가리킬 SSOT. 독립 vs 흡수(#2)·신뢰졸업 N(#6) 결정.
---

# Phase 01: 신규 정책 3종 토대

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 복잡 · **담당**: 메인 직접

## 🎯 목표
ADR-032가 가리키는 운영 스펙을 단일 진실(SSOT) 문서로 *먼저* 만든다. 이후 모든 REVISE(헌법·정책·에이전트)가 이 파일들을 *포인터로 참조*하므로 토대가 먼저 존재해야 dangling 0.

## ⏪ 사전 조건
- [x] ADR-032 accepted (2026-06-18)
- [x] ultracode 전면 파악 triage 완료

## 📝 작업 내용
- [ ] **미결 #2 결정**: `work-judge`/`review-throughput`을 독립 파일로 둘지, `grade-and-risk.md §3` / `review-tiering.md` 확장 흡수할지 — 220줄 임계 *실측 후* 결정 (영호 게이트, AI는 줄수 추천)
- [ ] `policies/loop-driver.md` 작성: 엔진(/goal 조건충족까지 + Workflow 오케스트레이션) / 기동 v1 attended(터미널·Remote Control) / PC-on·WSL2 게이트(ADR-029)=done 판사 / 버킷별 SubAgent 구동 / refactor-sweep=첫 인스턴스 참조
- [ ] `work-judge`(독립 or grade-and-risk §3): 3버킷(a 기계/b 취향·육안/c 판단·비가역) + risk-detector 3깃발 매핑
- [ ] `review-throughput`(독립 or review-tiering 확장): 예외기반·통합고도·신뢰졸업·시선=max(위험,학습가치) + **미결 #6 신뢰졸업 N 초안**
- [ ] (독립 채택 시) `policies/INDEX.md` 등록

## ✅ 완료 조건
- [ ] 정책(들) 작성 완료 + 각 220줄 임계 준수(doc-thresholds)
- [ ] #2·#6 결정이 문서에 박힘
- [ ] 참조 경로 전부 유효(dangling 0)

## 📚 학습 포인트
- 단일 진실(SSOT) 문서 설계 — "한 곳만 고치면 되는" 구조
- 220줄 외부화 임계(ADR-014)의 실전 적용

## ⚠️ 함정
- 헌법/정책이 가리킬 *경로·파일명*을 여기서 확정 안 하면 P02에서 전부 dangling.
- "흡수"를 골랐는데 분량이 220줄 넘으면 다시 분리 — 실측 먼저.

## ➡️ 다음 Phase
- P02 (헌법 + policies 5 atomic) — 이 토대를 가리킴
