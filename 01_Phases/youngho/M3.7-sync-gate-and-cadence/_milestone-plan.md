---
owner: youngho
milestone: M3.7
title: Sync gate + drift hardening (work-pin/CONTEXT 진행 단계 stale hole 봉합 + false-promise cadence ADR)
status: in-progress
grade: 복잡
risk: low
estimated: 2~4h (총합)
domain: harness
---

# M3.7 — Sync gate + drift hardening

> **상태**: in-progress
> **시작**: 2026-05-22
> **마감 목표**: 본 세션 안 (단발 마일스톤, 학습 호흡 끊김 ↓)

---

## 🎯 마일스톤 목표

**work-pin/CONTEXT.md 좌표가 작업 진행 단계(commit/push/PR 생성/PR 머지)를 따라잡지 못해 stale로 박히는 결함을 봉합** + **false-promise 누적 12건+ Rule of Three 3회 통과 → 주기적 감사 cadence ADR로 정책 영구화**.

본 마일스톤은 *코드 변경 X, 하네스 변경*. M3.5/M3.6 트랙 (하네스 마일스톤) 정합.

**왜 본 마일스톤이 필요한가**:

- **5번째 stale 실측**: M3.6 마감 직후 본 세션 시작 시점에 work-pin이 "commit + push + PR 게이트 대기"라 박혔지만 실제 4단계 모두 박힘 (PR #44 MERGED). 옵션 C 게이트(M3.5 Phase 05 박힘)가 *세션 마감 시점*만 동기 → *세션 도중* 진행 단계 X. 5번 누적 = Rule of Three 통과 + 본질 봉합 트리거 ON.
- **false-promise 누적 12건+ cadence ADR 트리거**: M3 5건 + M3.6 7건 = Rule of Three 통과 후 4번째 변종 + 5번째 사례 누적. 별 시점 액션 #5 (false-promise 주기적 감사 cadence ADR 신설) + #8 (work-pin commit 시점 stale hole 봉합)이 *지금* 합쳐서 박을 시점.
- **단발 마일스톤**: 2 Phase 구성 (Phase 01 보통 + Phase 02 복잡), 본 세션 안 마감 가능. M4 진입 전 봉합.

---

## 📋 Phase 분해 (2개)

| # | Phase | 등급 | 도메인 | 예상 | 담당 |
|---|---|---|---|---|---|
| 01 | `/session:start` drift 발견 게이트 신설 | 보통 | harness | 30~60min | 메인 직접 (영호) |
| 02 | ADR 묶음 신설 (ADR-023 동기화 결함 + ADR-024 cadence) + pin-and-done.md 갱신 | 복잡 | harness | 1~2h | 메인 직접 (영호) |

**총 등급 = 복잡** (마일스톤 자체) — -DONE.md는 Phase 02만 (복잡 등급), HTML 박음 X (대규모만 의무), 5단계 보고 X.

---

## 🔗 의존성 그래프

```
Phase 01 (drift 발견 게이트)
   │
   │  Phase 01 산출물(/session:start 새 단계)이 ADR-023 *새 발견 게이트* 항목에 인용됨
   ↓
Phase 02 (ADR-023 + ADR-024 + pin-and-done.md 갱신)
```

**의존성 사유**: ADR은 *실측 후 정책* 정신 (M3.5/M3.6 학습 패턴 정합). Phase 01에서 게이트 구현 → 실제 작동 확인 → Phase 02에서 ADR에 박음. 옛 "ADR 먼저 / 구현 후" 순서는 *추측 정책* 위험.

---

## ✅ 마일스톤 완료 조건

- [ ] Phase 01 = `commands/session/start.md` drift 발견 단계 박힘 (git log + gh pr list 자동 호출 + work-pin 비교 + 차이 STOP) + `team-guide.html` "막혔을 때" 표 한 행 박제
- [ ] Phase 02 = ADR-023 + ADR-024 박힘 + `pin-and-done.md` §5 stale hole 봉합 인용 추가
- [ ] Phase 02 `-DONE.md` 박음 (복잡 등급)
- [ ] CHANGELOG [M] entry 박음 (슬래시 동작 변경 + ADR 2건 신설)
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점" = M3.7 완료 + M4 진입 대기로 갱신
- [ ] commit + push (사용자 명시 GO 후 PR)

---

## ⚠️ 주의할 약속

- **Hook은 알림 전용 정신 보존** — Phase 01 새 단계는 *발견*만, 갱신은 본인 인지 게이트. 자동 갱신 박지 않음 (헌법 정신 = pin-and-done.md §1 "갱신은 본인 수동" 정합)
- **ADR-023이 옵션 C 게이트 *대체 X, 보강***. M3.5 Phase 05 박힌 옵션 C 정신(세션 마감 시 동기)은 유지. 본 ADR은 *세션 도중 진행 단계 stale hole*만 봉합 (새 발견 게이트 = `/session:start` 시점만)
- **ADR-024 cadence는 운영 정책** — false-promise 발견 시 즉시 봉합 + 마일스톤 마감마다 자체 감사 + ad-hoc X건 누적 시 트리거. 본 ADR은 *정책 박음*만, 자동화 도구 신설 X (별 시점 작업)
- **본 마일스톤 자체가 stale hole 봉합 시범** — 본 세션 work-pin 갱신/CONTEXT 갱신을 매 단계 *명시적*으로 박음. ADR-023 본문에 *본 마일스톤 자체 사례* 인용

---

## 📚 학습 포인트 (마일스톤 차원)

- **옵션 C 게이트의 한계 실측** — 단방향 동기 게이트가 *세션 마감 시점*만 잡고 *진행 단계*는 못 잡음. 5번 실측 후 봉합 = Rule of Three 정합
- **Hook is for alert, not action** — Hook 자동 갱신 박지 않음 결정 = 학부생 인지 게이트 보호. pin-and-done.md "갱신은 본인 수동" 정신 확장
- **false-promise 주기적 감사의 정합** — 옛 운영 *발견 후 봉합* (즉시) → 새 운영 *주기적 감사 cadence* (마일스톤 마감마다 + ad-hoc 트리거). 면접 *자가 점검 사이클* 어필
- **단발 마일스톤의 가치** — M3.7 = 2 Phase 본 세션 마감. M3.5/M3.6 같은 *대규모 점검 마일스톤*과 대비. 양식 부담 ↓ + 학습 호흡 유지

---

## ➡️ 다음 마일스톤

- **M4 — Combat & Map Transition** (진짜 4맵 + 정밀 전투 + lag compensation + portal handoff + 몬스터/보스 정식). M3.7 마감 후 진입.

---

## 갱신 이력

- 2026-05-22 — 본 세션 사용자 의논 후 박힘. M3.6 마감 직후 PR 리스트 체크에서 5번째 stale 실측 발견 → 옵션 (C)+(D) 합의 → 본 마일스톤 신설. 단발 마일스톤 (2 Phase, 본 세션 마감 가능).
