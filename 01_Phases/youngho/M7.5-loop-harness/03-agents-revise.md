---
owner: youngho
milestone: M7.5
phase: 03
title: agents 6 REVISE (루프 드라이버 인지)
status: pending
grade: 복잡
estimated: 2~3h
domain: cross
summary: coordinator·reviewer·plan-auditor·knowledge-gc·_routing·_escalation을 loop-driven 정합으로. P04와 병렬 가능.
---

# Phase 03: agents 6 REVISE

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 복잡 · **담당**: 메인 직접 · **병렬**: P04와 동시 가능

## 🎯 목표
9개 SubAgent를 loop 엔진의 Worker/checker로 *재사용*하도록 정의 보강. 권한·모델 표는 KEEP, 운영모드 인지만 REVISE.

## ⏪ 사전 조건
- [ ] P02 완료 (정책층 — agents가 참조)

## 📝 작업 내용
- [ ] `coordinator.md`: 호출자/통합반환 메인→"메인 또는 루프 드라이버" 일반화. Step5 통합보고 루프 소비형(done 신호+사람게이트 플래그). 비가역=버킷(c) 보존
- [ ] `reviewer.md`: 결과에 "사람게이트 도달 vs 루프 자율통과" 분기(시선=max(위험,학습가치)) + 신뢰졸업 개념
- [ ] `plan-auditor.md`: 자동호출자 "루프 드라이버" 병기 + 완료조건 정량성=loop done 자동판정 가능 형태
- [ ] `knowledge-gc.md`: "무인 loop에서도 자율 실행 X — 제안 누적, 사람 attended 게이트" 명시
- [ ] `_routing.md`: 트리거 호출 주체 "루프 드라이버" 병기 + 등급→버킷 열
- [ ] `_escalation.md`: 무인 분기(v1=attended 즉시 사람) + 비가역 동반 실패=정지

## ✅ 완료 조건
- [ ] 6 파일 REVISE 완료
- [ ] 권한·모델 표 보존(KEEP — 운영모드만 변경)
- [ ] dangling 0

## 📚 학습 포인트
- 기존 에이전트 = 루프 Worker/checker로 무손 재사용(새로 안 만듦)

## ⚠️ 함정
- 권한/모델 표를 건드리지 말 것 — ADR-022 부품 정의 보존 영역.
- v1은 attended라 "무인 분기"는 *문서화만*, 실 발동은 v2.

## ➡️ 다음 Phase
- P05 (hooks+settings+원장)
