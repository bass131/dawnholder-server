---
owner: youngho
milestone: M4.2
title: Map Transition (진짜 4맵 분리 + portal handoff + 클라 dispatch + cheat-flag + Serilog)
status: placeholder
grade: 복잡
risk: trust-boundary
estimated: 5~10h (총합, 3 Phase 추정)
domain: server+shared+client
---

# M4.2 — Map Transition (placeholder)

> **상태**: placeholder — **본격 Phase 분해는 M4.1 마감 시점에**
> **시작 예정**: 2026-06-03 (캡스톤 1 발표 1주 전, M4.1 마감 6/2 직후)
> **마감 목표**: 2026-06-09 (캡스톤 1 발표 1일 전, 6/3 슬라이드 초안 마감 + 6/10 발표 정합)
> **사전 조건 (신설)**: M3.8 마감 + M4.1 마감 (2026-05-22 진행 순서 갱신 정합 — M3.8 → M4.1 → M4.2)

---

## 🎯 마일스톤 목표 (예정)

**M3 응급 박힌 단일 맵 3-zone trick을 진짜 4맵 분리로 승격** + 맵 간 portal handoff + 클라 측 scene 전환. 캡스톤 1 발표 데모 후반 (M4.1 정밀 전투 + M4.2 4맵 분리 = "정밀화된 멀티플레이어 RPG" 어필).

**예정 Phase 분해 (3개, M4.1 마감 시점에 확정)**:

1. **Phase 04** — 서버 4맵 분리 + portal entity + `S_MapTransition` 패킷 + 맵 간 player state 이전
2. **Phase 05** — 클라 4맵 dispatch + portal UX (정유현 협업 — UI Scene 분리 ADR-021 정합)
3. **Phase 06** — cheat-flag table + Serilog 도입 (헌법 #3 정합 강화 + M5 약속 일부 forward)

---

## ⚠️ placeholder 사유

본 마일스톤은 **M4.1 마감 시점에 본격 Phase 분해**. 사유:

- **M4.1 Phase 01 Codex 크로스 리뷰 결과**에 따라 본 마일스톤 scope 변경 가능 (예: 발견된 추가 하드코딩이 본 마일스톤 흡수)
- **M4.1 진행 중 발견된 인프라 약속**이 본 마일스톤 Phase 정의에 반영 필요 (예: lag compensation rewind 패턴이 portal handoff player state 이전에 영향)
- **정유현 협업 일정** (5/16 합류, M3 Phase 08a/08b 진행 중) 정합 — M4.1 마감 시점에 영역 분리 의논 + Phase 05 분담 명시
- **stale 위험 ↓** — 옛 Phase 정의가 M4.1 마감 시점 변경된 인프라와 어긋날 위험. M3.7 stale hole 봉합 학습 정합

본 placeholder는 **목적 + 예정 Phase 개략 + 사유**만 박음. 본격 정의는 M4.1 마감 직후 `/work:plan M4.2` 호출로 박음.

---

## 📋 예정 Phase 개략 (확정 X)

| # | Phase 예정 | 등급 추정 | 도메인 | 예상 |
|---|---|---|---|---|
| 04 | 서버 4맵 분리 + portal handoff | 복잡 | server+shared | 3~5h |
| 05 | 클라 4맵 dispatch + portal UX | 복잡 | client+unity-bridge | 3~5h |
| 06 | cheat-flag table + Serilog 도입 | 보통 | server | 1~2h |

**총 등급 = 복잡** (마일스톤 자체, 추정). 단 Phase 04·05 trust-boundary 위험 깃발 가능성 → **대규모 자동 상향** 가능.

---

## 🔗 예정 의존성 그래프

```
Phase 04 (서버 4맵 + portal handoff)
   │
   │  Phase 04 산출물(S_MapTransition 패킷)이 Phase 05 클라 dispatch 진입
   ↓
Phase 05 (클라 4맵 dispatch + portal UX) — 정유현 협업
   │
   │  병렬 가능: Phase 06 (cheat-flag) ↔ Phase 04/05 (별 영역)
   ↓
Phase 06 (cheat-flag + Serilog)
```

병렬 가능 = Phase 06 ↔ Phase 04/05 (다른 영역).

---

## ✅ 예정 마일스톤 완료 조건 (확정 X)

- [ ] 4맵 정의 (마을 / 사냥터 / 보스 / 종료) 박힘
- [ ] portal entity + `S_MapTransition` PDL 패킷
- [ ] 맵 간 player state 이전 (HP / 인벤토리 / 위치)
- [ ] 클라 4 scene 전환 + portal UX
- [ ] cheat-flag table (in-memory 또는 file) 박힘
- [ ] Serilog 도입 (Console + File sink, ARCHITECTURE 정합)
- [ ] dotnet test green (회귀 0 + Phase별 신규 테스트)
- [ ] M4.2-마감 별 -DONE.md (복잡 등급)
- [ ] CHANGELOG entry ([M] or [H] — PDL 변경 + Serilog 도입 + 모든 팀원 영향)
- [ ] **캡스톤 1 발표 데모 영상 가능 상태** (M4.1 + M4.2 종합)

---

## ➡️ 다음 마일스톤

- **M4.3 — AI + Polish** (enemy AI + boss behavior + jump Y mispredict 봉합 + PvP ADR + 마감 의례). 캡스톤 1 후 7월~10월.

---

## 갱신 이력

- 2026-05-22 — placeholder 박힘 (M4.1 plan 박는 시점, M4 3토막 분할 정합)
- 2026-05-22 — M3.8 신설 흡수 일정 재정렬. 시작 예정 6/3 동일, 마감 목표 6/10 → 6/9 (캡스톤 1 발표 1일 전, 6/3 슬라이드 초안 마감 + 6/10 발표 정합). 사전 조건 = "M3.8 마감 + M4.1 마감" 추가. Phase 정의 자체는 변경 X (M4.1 마감 시점에 본격 분해 패턴 유지).
