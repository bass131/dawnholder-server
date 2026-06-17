---
owner: youngho
milestone: M7.5
phase: 05
title: hooks + settings 권한(#4) + 원장 3종(#5) — 강제·상태 층
status: pending
grade: 복잡
risk: trust-boundary
estimated: 2~3h
domain: cross
summary: circuit halt + 신규 가드 + settings 권한 승격(ask(pr) 보존) + pending-art/comprehension/knowledge 원장.
---

# Phase 05: hooks + settings + 원장 3종

> **상태**: pending · **마일스톤**: M7.5 · **등급**: 복잡 · **risk**: trust-boundary · **담당**: 메인 직접 + **reviewer 필수**

## 🎯 목표
루프의 강제(hook)·상태(원장)·권한(settings) 층을 v1 수준으로 정착. **사람 게이트(ask(pr))는 절대 보존.**

## ⏪ 사전 조건
- [ ] P02 완료 (정책층)
- [ ] P04 완료 (드라이버가 원장 소비)

## 📝 작업 내용
- [ ] `circuit-breaker.sh`: 임계 도달 시 `.claude/state/circuit-tripped.txt`(타임스탬프+도구+카운트) 기록 → 드라이버 폴링용 halt 신호. "사용자 판단"→"attended 알림 / (v2)halt 기록"
- [ ] `hooks/README.md`: Hook 표 8→10 정정 (convention-size-guard *등재* — 파일은 2026-05-29 기존, "추가" 아님) + 3버킷 judge 매핑 절 + 설정 경로 현행화
- [ ] 신규 가드 hook 또는 `risk-detector.sh` 확장: loop-done-gate(루프 done 선언 시 WSL 회귀 증적 요구) — risk-detector 확장으로 충분하면 신규 X
- [ ] **미결 #4 결정**: settings 권한 승격 범위 — v1 필요 최소만(ask(pr) 보존)
- [ ] `settings.json`/`settings.local.json` REVISE: 임시 마커 정리, 무인 commit allow는 **defer(v2)**
- [ ] **미결 #5 결정**: 원장 위치(.claude/state 라이브 vs policies 명세) + .gitignore 여부
- [ ] `pending-art.md`/`pending-comprehension.md`/`pending-knowledge.md` 원장 신설(스키마: 항목/상태/요청자)
- [ ] `knowledge/{README,_usage}.md` REVISE (plan-auditor 축6): 무인 루프 발견 → pending-knowledge 큐 적재(자율 박제 X, 아침 사람 승인) — pending-knowledge 원장과 짝

## ✅ 완료 조건
- [ ] **ask(gh pr merge/create) 매처 보존** — `settings.json` git diff로 기계 검증 (trust-boundary)
- [ ] hook 정합 smoke: 기존 hook 발동 유지 + 신규 가드 exit 코드 정상(부록 A 마커 패턴)
- [ ] 원장 3종 신설 + #4·#5 결정 박음
- [ ] dangling 0 · reviewer 🔴 0

## 📚 학습 포인트
- hook = 기계 judge / 무인 halt는 "신호 파일 + 드라이버 폴링"(hook은 직접 못 죽임)
- risk-detector advisory 한계(차단 X) — v2 무인의 trust-boundary 구멍 근원

## ⚠️ 함정
- settings 권한 승격이 ask(pr) 게이트를 깨면 헌법 §3 위반 — diff 필수 검증.
- circuit-breaker는 알림만 — 무인 폭주 차단은 v2 드라이버 폴링 선결(여기선 신호 기록까지).

## ➡️ 다음 Phase
- P06 (카탈로그 + drift 마감)
