---
owner: youngho
milestone: M4.3
title: AI + Polish (enemy AI + boss behavior + jump Y mispredict 봉합 + PvP ADR + M4 마감 의례)
status: placeholder
grade: 복잡
risk: low
estimated: 5~10h (총합, 3 Phase 추정)
domain: server+client
---

# M4.3 — AI + Polish (placeholder)

> **상태**: placeholder — **본격 Phase 분해는 M4.2 마감 시점에**
> **시작 예정**: 2026-07월 (캡스톤 1 발표 후)
> **마감 목표**: 2026-10월 (본 마감 11/19 1달 전)

---

## 🎯 마일스톤 목표 (예정)

**M3 응급 박힌 패시브 dummy enemy를 AI 박힘 + 보스 behavior 패턴 박음** + M2 잔존 jump Y mispredict 봉합 + PvP 지원 여부 ADR 박제 + M4 전체 마감 의례 (5단계 보고 MD/HTML 캡스톤 평가 자산).

**예정 Phase 분해 (3개, M4.2 마감 시점에 확정)**:

1. **Phase 07** — enemy AI (Normal: patrol/chase FSM) + boss behavior (다단 attack pattern, 페이즈 1/2)
2. **Phase 08** — M2 Phase 05 jump Y mispredict 봉합 + PvP 지원 여부 ADR 박제
3. **Phase 09** — M4 전체 종합 마감 의례 (5단계 보고 MD + HTML, 본 마감 데모 영상 가능 상태)

---

## ⚠️ placeholder 사유

본 마일스톤은 **M4.2 마감 시점에 본격 Phase 분해**. 사유:

- **M4.2 결과 정합** — 4맵 분리 후 enemy AI scope가 *맵별 spawn pattern*까지 확장 가능. M4.2 마감 시점에 맵 인프라 봤을 때 AI scope 명확화
- **캡스톤 1 발표 결과 정합** — 6/10 발표 후 *발견된 문제점*이 본 마일스톤 Phase 정의에 반영 가능 (예: 발표 청중 피드백 = AI 부족, PvP 욕심 등)
- **본 마감 일정 정합** — 11/19까지 5~6개월 = 본 마일스톤 외 M5(영속화) + M6(길드) + M7(거점) + M8(부하 테스트) 균등 분담 필요. M4.3 scope는 *과하지 않게* 절제
- **stale 위험 ↓** — 옛 Phase 정의가 M4.2 마감 + 캡스톤 발표 후 변경된 인프라와 어긋날 위험. M3.7 stale hole 봉합 학습 정합

본 placeholder는 **목적 + 예정 Phase 개략 + 사유**만 박음. 본격 정의는 M4.2 마감 + 캡스톤 1 발표 직후 `/work:plan M4.3` 호출로 박음.

---

## 📋 예정 Phase 개략 (확정 X)

| # | Phase 예정 | 등급 추정 | 도메인 | 예상 |
|---|---|---|---|---|
| 07 | enemy AI + boss behavior | 복잡 | server | 3~5h |
| 08 | M2 jump Y mispredict 봉합 + PvP ADR | 보통 | server+client | 1~2h |
| 09 | M4 전체 마감 의례 (5단계 보고 MD/HTML) | 복잡 | meta | 2~3h |

**총 등급 = 복잡** (마일스톤 자체, 추정). 단 본 마일스톤 = *M4 전체 마감 마일스톤*이라 **5단계 보고 의무 (대규모 등급)** 가능 — M4.1 + M4.2 + M4.3 종합 보고.

---

## 🔗 예정 의존성 그래프

```
Phase 07 (enemy AI + boss behavior)
   │
   │  Phase 07 산출물이 본 마감 데모 영상에 핵심 어필
   ↓
Phase 08 (jump Y mispredict + PvP ADR)
   │
   │  Phase 08 = 잔존 결함 봉합 + 결정 박음
   ↓
Phase 09 (M4 전체 마감 의례)
```

병렬 가능 = Phase 07 ↔ Phase 08 (다른 영역 — 보스 vs 클라 정합).

---

## ✅ 예정 마일스톤 완료 조건 (확정 X)

- [ ] enemy Normal patrol/chase FSM 박힘 (헌법 #5 동기, tick thread)
- [ ] boss behavior 다단 attack pattern (페이즈 1/2)
- [ ] M2 Phase 05 jump Y mispredict 봉합 (잔존 결함 0)
- [ ] PvP 지원 여부 ADR 박힘 (헌법 #1 정합 — "지원 X" 또는 "지원, trust-boundary 강화")
- [ ] M4 전체 5단계 보고 MD + HTML 박음 (캡스톤 평가 자산)
- [ ] dotnet test green (회귀 0 + 신규 테스트)
- [ ] CHANGELOG entry ([M] or [H] — AI 도입 + PvP 결정 + 모든 팀원 영향)
- [ ] **본 마감 데모 영상 가능 상태** (M4 전체 + M5/M6/M7/M8 진행 중)

---

## ➡️ 다음 마일스톤

- **M5 — Persistence** (DB 연결 + 캐릭터/인벤토리 영속화 + 재접속 복원). PRD.md 정합.

---

## 갱신 이력

- 2026-05-22 — placeholder 박힘 (M4.1 plan 박는 시점, M4 3토막 분할 정합)
