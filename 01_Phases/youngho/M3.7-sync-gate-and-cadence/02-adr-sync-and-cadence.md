---
owner: youngho
milestone: M3.7
phase: 02
title: ADR 묶음 신설 (ADR-023 동기화 결함 + ADR-024 false-promise cadence) + pin-and-done.md 갱신
status: done
grade: 복잡
estimated: 1~2h
domain: harness
---

# Phase 02: ADR 묶음 신설 (ADR-023 + ADR-024) + pin-and-done.md 갱신

> **상태**: pending
> **마일스톤**: M3.7
> **등급**: 복잡 (2 ADR 신설 + 정책 1건 갱신 / ~150~250줄 / 정책 영구화)
> **담당**: 메인 직접 (영호)

---

## 🎯 목표

**Phase 01에서 박힌 새 발견 게이트를 ADR로 정책 영구화** + **false-promise 누적 12건+ Rule of Three 3회 통과 → 주기적 감사 cadence ADR 박음**.

본 Phase는 *정책 박음*이지 *자동화 도구 신설 X*. cadence 자동화 (예: cron 또는 슬랙 알림)는 별 시점 작업.

**왜 본 Phase가 필요한가**:

- **옵션 C 게이트 한계 명시 + 보강 박음**: M3.5 Phase 05 박힌 옵션 C(세션 마감 시 단방향 동기)가 5번 실측 결과 *세션 도중 진행 단계*는 못 잡음. ADR-023이 그 한계 + 새 발견 게이트(Phase 01 산출물) 박음
- **false-promise cadence 정책 영구화**: M3 5건 + M3.6 7건 = 12건+ 누적. Rule of Three 3회 통과 후 4번째 변종 + 5번째 사례 = 트리거 ON. 옛 운영 *발견 후 즉시 봉합*만 박혀있고 *주기적 감사* 정책 X. 본 ADR이 그 정책

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (drift 발견 게이트 박힘 + 작동 시연)

---

## 📝 작업 내용

### ADR-023 신설: `work-pin/CONTEXT 동기화 결함 — 진행 단계 stale hole 봉합`

경로: `00_Document/ADR/harness/ADR-023-sync-gate-progress-stale-hole.md`

본문 골격:
- **컨텍스트** — M3.5 Phase 05 옵션 C 게이트 박힘 (work-pin → CONTEXT 단방향 동기, `/session:end` 단일 게이트). 5번 실측 후 한계 발견.
- **5번 실측 사례** — Phase 06 (자기 work-pin 약속 가짜화) + 본 세션 시작 시점 (commit/push/PR 생성/PR 머지 4단계 stale) 등 누적. 본 ADR 본문에 *본 마일스톤 자체 사례* 인용.
- **결정** — 옵션 C 게이트는 *유지* (세션 마감 시 동기). 새 발견 게이트 *보강* (`/session:start` 시점 자동 발견 + 본인 수동 갱신, Phase 01 산출물). 자동 갱신 박지 않음 결정 (Hook is for alert).
- **고려한 대안 + 보류 사유**:
  - 옵션 (A) Hook 신설 (post-commit/post-push/PR 시점 자동 갱신) → hook 복잡도 ↑ + 본인 인지 게이트 우회 = pin-and-done.md "갱신은 본인 수동" 정신 위반. **보류**
  - 옵션 (B) work-pin 양식 다이어트 (진행 단계 박지 않기) → work-pin 정보 ↓ + 매 세션 시작 비용 ↑ + 압축 양식 가치와 충돌. **보류**
  - 옵션 (C) `/session:start` 보강 → 본 ADR 채택 (실측 후 정책 정신 + 학부생 인지 게이트 보호)
  - 옵션 (D) 본 ADR 묶음 = 정책 영구화. **본 Phase에서 실행**
- **영향** — `commands/session/start.md` + `team-guide.html` (Phase 01 박힘) / `pin-and-done.md` §5 (본 Phase에서 stale hole 봉합 인용 추가)
- **모니터링** — 다음 마일스톤 마감(M4)에서 stale hole 0건이면 본 ADR 성공. 1건+ 발견 시 ADR-023 후속 봉합 (옵션 A 재논의)

### ADR-024 신설: `false-promise 주기적 감사 cadence`

경로: `00_Document/ADR/harness/ADR-024-false-promise-cadence.md`

본문 골격:
- **컨텍스트** — false-promise 패턴 = *주석/문서에 약속 박혀있는데 코드/실행에는 없음*. 옛 운영 발견 시리즈 = M3 Phase 02 (헌법 #2 ProtocolVersion handshake) + Phase 03 (헌법 #4 Shared Code Discipline) + Phase 04 (Handlers/ 폴더) + M3.6 누적 7건 = 총 12건+.
- **Rule of Three 통과 + 4번째 변종 + 5번째 사례** — 옛 운영 *발견 후 즉시 봉합*은 박혔으나 *주기적 감사* 정책 X. 발견이 본인 ad-hoc 시점(점검 마일스톤 또는 우연)에 의존. 본 ADR이 cadence 박음
- **결정 — cadence 박음**:
  - **마일스톤 마감마다 자체 감사**: 본인 + (옵션) plan-auditor SubAgent 자동 호출. 검사 = "약속 박힌 .md 파일 × 실재 코드 grep". 결과 박음 = `-DONE.md` "false-promise 점검 결과" 섹션 의무
  - **ad-hoc 트리거**: 누적 X건 박힘 시 트리거 ON. 본 ADR 박힘 시점 = 12건+ 누적 → 다음 ad-hoc 감사 별 마일스톤 후보
  - **자동화 도구**: 본 ADR 박음 X (별 시점 작업). 옵션 = plan-auditor SubAgent에 false-promise 감사 책임 추가 (M5+) 또는 별도 슬래시 `/audit:false-promise` 신설 (M5+)
- **고려한 대안 + 보류 사유**:
  - 즉시 자동화 (Hook 또는 SubAgent) → 본 마일스톤 단발성 + 자동화 복잡도 ↑. cadence 정책 박음만 본 Phase
  - cadence 없이 ad-hoc 유지 → 본인 의존 = 빈도 ↓ (12건+ 누적이 그 증거). **보류**
- **영향** — 본 ADR 박음 후 다음 마감(M3.7 자체 -DONE.md)에 false-promise 점검 결과 섹션 박음 시범
- **모니터링** — 다음 마일스톤(M4) 마감에서 cadence 작동 (점검 결과 섹션 박힘) 확인. M4 마감 시 false-promise 누적 = 12건+ → +N건 추세로 본 cadence 효과 정량

### `pin-and-done.md` §5 갱신

기존 §5(work-pin ↔ CONTEXT 정합 옵션 C 게이트) 본문에 **§5.1 진행 단계 stale hole 봉합** 신설:

- 옵션 C 게이트의 한계 (세션 도중 진행 단계 X) 명시
- 새 발견 게이트 = `/session:start` 시점 자동 발견 인용 (ADR-023 박힘)
- 자동 갱신 X 정신 강조 (Hook is for alert, not action)
- 동기화 룰 표 한 행 추가 가능 ("진행 단계 stale" 행)

### ADR INDEX 갱신

`00_Document/ADR/INDEX.md`에 ADR-023 + ADR-024 한 줄씩 추가.

---

## ✅ 완료 조건

- [ ] `ADR-023-sync-gate-progress-stale-hole.md` 박힘 (컨텍스트 + 5번 실측 사례 + 결정 + 고려 대안 + 영향 + 모니터링)
- [ ] `ADR-024-false-promise-cadence.md` 박힘 (컨텍스트 + Rule of Three 통과 사유 + cadence 정책 + 자동화 보류 사유 + 영향 + 모니터링)
- [ ] `pin-and-done.md` §5 갱신 (§5.1 stale hole 봉합 인용 추가)
- [ ] `ADR/INDEX.md` 두 줄 추가
- [ ] `-DONE.md` 박음 (복잡 등급, 4 필수 섹션 = TL;DR + AC 검증 결과 + 학습 일지 후보 키워드)
- [ ] phase-gate-validator.sh 통과 (복잡 등급 검사)

---

## 🧪 테스트

**자동**:
- `phase-gate-validator.sh` 작동 시 frontmatter 5 필드 + 필수 H2 섹션 검사 통과
- `risk-detector.sh` Hook 작동 확인 (헌법/ADR/policies Edit이라 harness 깃발 잡힘 예상)

**수동**:
- ADR 본문 × 실측 사례 정합 (본 마일스톤 stale 발견 = 5번째 사례 본문 박힘)
- pin-and-done.md §5 ↔ ADR-023 cross-reference 정합

---

## 📚 학습 포인트

- **실측 후 정책 정신** — M3.5/M3.6 학습 패턴 = 옛 운영 *추측 정책* → 새 운영 *실측 후 정책*. 본 Phase = Phase 01 게이트 구현 → 실측 → ADR 박음 정합
- **Hook is for alert, not action** — 자동 갱신 박지 않음 결정. 학부생 인지 게이트 보호 (pin-and-done.md §1 "갱신은 본인 수동" 정신 확장)
- **Rule of Three 후 정책 영구화 패턴** — 옛 운영 ad-hoc 3회+ 누적 → 새 운영 정책 박음. M3.5 Phase 05 `cross-review-rule-of-three` 학습 정합
- **단발 cadence vs 자동화 cadence** — 본 ADR-024 = 정책 박음만 (단발). 자동화는 별 시점. 양식 부담 ↓ + 본인 호흡 유지

---

## ⚠️ 함정 / 주의사항

- **ADR 본문이 본 마일스톤 자체 사례 인용** — 자기 참조 X 주의. ADR-023이 *옵션 C 한계 명시* 시 옛 M3.5 Phase 05 옵션 C 박힘 인용 + 본 5번째 실측 인용 (각 한 줄). 본 마일스톤 -DONE.md 박힘 후엔 *완료된 사례*로 인용 가능
- **cadence 자동화 보류 사유 명확화** — ADR-024가 *cadence 정책*만 박음 + *자동화 도구는 별 시점* 명시. 옛 false-promise 자동화 약속 박지 않음 = 본 ADR이 false-promise되지 않게 (자기 참조 함정)
- **pin-and-done.md 갱신 시 헌법 인용 표 영향 점검** — §5 갱신만이라 헌법 인용 표 변경 X 예상, 단 확인 의무

---

## ➡️ 다음 Phase

- 본 Phase가 M3.7 마지막 Phase. 본 Phase 완료 후 = CHANGELOG [M] + CONTEXT.md 갱신 + commit + push → 사용자 명시 GO 후 PR

---

## 📋 박제 (완료 후)

- 등급 복잡 → **`-DONE.md` 박음** (4 필수 섹션 = TL;DR + AC 검증 결과 + 학습 일지 후보 키워드 + ★ false-promise 점검 결과 섹션 본 Phase에서 시범 박음)
- 5단계 보고 X (대규모만 의무)
- HTML 박음 X (대규모만 의무)
