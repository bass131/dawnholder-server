---
owner: youngho
milestone: M4.1
phase: 01
title: Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본
status: done
grade: 복잡
risk: low
summary: α 22건 + β 보정 4건 + β 추가 4건 γ 통합. plan 변경 트리거 X (양쪽 일치) + Phase 03 옵션 A 흡수 (B1 + B3). 등급 자동 상향 (보통 → 복잡, 발견 8건+ 및 즉시 봉합 결정 트리거 ON).
---

# Phase 01 — DONE

**완료 일자**: 2026-05-23
**소요**: ~1.5h (Claude α 전수조사 + Codex prompt 박음 + 본인 별 세션 β 호출 + γ 비교 + Phase 03 plan 흡수)

---

## TL;DR

M3 응급 코드 전수조사 22건 발견 + 외부 시각 β 검증 통과 + plan 변경 트리거 X 판정 (α/β 일치) + Phase 03에 B1/B3 즉시 봉합 흡수. **다음 액션은 사용자 명시 재정렬 가닥에 따라 변경 가능** (본 Phase 마감 시점 직후 "포트폴리오 방어선 관점 재정렬" 요청 박힘 — `_milestone-plan.md` 재구성 후 결정).

---

## 📦 산출물 3건

- [`00_Document/reviews/2026-05-23-pre-m4-cross-review-claude.md`](../../../00_Document/reviews/2026-05-23-pre-m4-cross-review-claude.md) — α 22건 발견 + β 보정 4건 흡수 (사후 갱신)
- [`00_Document/reviews/2026-05-23-pre-m4-cross-review-codex.md`](../../../00_Document/reviews/2026-05-23-pre-m4-cross-review-codex.md) — 본인 직접 호출 결과 (분담 첫 정식 실측)
- [`00_Document/reviews/2026-05-23-cross-review-m4.1-phase01.md`](../../../00_Document/reviews/2026-05-23-cross-review-m4.1-phase01.md) — γ 비교 산출물 + 결정 권유

---

## AC 검증 결과

| AC | 결과 | 검증 |
|---|---|---|
| pre-review-claude.md 박힘 (α 22건 + Codex 입력 자료) | ✅ | `ls 00_Document/reviews/2026-05-23-pre-m4-cross-review-claude.md` 통과 |
| pre-review-codex.md 박힘 (본인 직접 호출 결과) | ✅ | β가 자체 박음 (본인 별 세션 호출 정합) |
| 발견 항목별 처리 결정 4 분류 표 박힘 | ✅ | `cross-review-m4.1-phase01.md` §5 박힘 (즉시 봉합 2 / M4.2 이관 6 / M4.3 이관 6 / M5+ 또는 별 시점 4) |
| M4.1 Phase 02·03 plan 갱신 결정 | ✅ | Phase 03 plan 2단계 + 3단계 + 완료 조건 2건 갱신 박힘 (B1 sweep + B3 sweep) |
| 등급 자동 상향 트리거 ON 시 -DONE.md 박음 | ✅ | 본 파일 (Phase 01 정의 트리거 정합) |

---

## 🔍 β 추가 실질 결함 2건 (Phase 03 흡수 박힘)

- **B1**: `LocalPlayerController.AttackRangeSq = 9.0f` 클라 측 중복 (서버 `CombatConstants.AttackRangeSquared = 9.0f`와 복사 박힘). Phase 03 AABB 전환 시 클라 target 추천이 옛 원형 range 붙들면 데모 체감 어긋남 → Phase 03 3단계 sweep 의무.
- **B3**: `98_Shared/CLAUDE.md:19` + `00_Document/ARCHITECTURE.md:213` ProtocolVersion `Current=3` 문서 잔재 (실제 코드 = 4). false-promise 변종 (M3.6 Phase 04 학습 정합) → Phase 03 ProtocolVersion 4→5 bump 시 동시 sweep 의무.

기타 β 발견 B2/B4 = M5+ / M4.3 이관 (즉시 봉합 X).

---

## 🔧 α 분류 보정 4건 (β 정밀화)

- C1/C2 = "M3.8 봉합 완료" → "host만 봉합 완료 / port·timeout 잔여 (M4.2 이관)" 정정
- Sh1 + 신규 Sh2 (`Physics.cs`) = 설계 의도 박힘 (tuning/table 후보, runtime config X)
- C12 = "수정 불필요" → "서버 hitbox와 별개 (Phase 03 서버 측 AABB 박음, Unity collider 신뢰 X)" 정정

---

## 결정 흐름

1. **plan 변경 트리거 판정** = α/β 일치 → "plan 변경 X" 결정. 정량 임계 8건+ 초과지만 *내용 본질* 점검 결과 별 Phase 신설 X + 별 마일스톤 신설 X.
2. **Phase 03 옵션 A 즉시 봉합** = B1 + B3 흡수만 plan 갱신 박음 (옛 Phase 03 작업 안에 자연 흡수, 별 Phase 신설 X).
3. **등급 자동 상향** = 옛 보통 → 복잡 (발견 8건+ + 즉시 봉합 결정 트리거 ON, 운영 의무 = -DONE.md 박음).
4. **다음 액션 결정 보류** = 본 Phase 마감 직후 사용자 "포트폴리오 방어선 관점 재정렬" 요청 박힘 → M4.1 plan 자체 재구성 후 다음 Phase 진입 결정.

---

## 학습 일지 후보 키워드

- `external-tool-call-user-direct-default` — Codex β 본인 직접 호출 분담 *첫 정식 실측* 가치 실증 (β만 잡은 실질 결함 B1 = 분담 본질 정합)
- `cross-review-content-vs-quantity-judgment` — 발견 양 정량 임계 *기계적 적용* 회피, *내용 본질* 점검 (22건이지만 plan 변경 X) = 학부생 학습 정합
- `auto-grade-promotion-with-trigger-honoring` — 등급 자동 상향 트리거 ON 시 운영 의무 (-DONE.md 박음) = false-promise 변종 잠복 방지

---

## ➡️ 다음 액션 (재정렬 풀세트 박힘 2026-05-23)

**M4.1 재구성 옵션 A' GO 박힘** — 마일스톤 제목 = `Combat Precision` → `Combat Integrity & Portfolio Hardening` (P0 신뢰도 ≫ P1 정밀도 정신).

**Phase 풀세트 6 Phase 박힘**:
- Phase 02 (신규) — Session State Machine Hardening (P0-1 + P0-2, 보정 1 = 신규 패킷 X)
- Phase 03 (신규) — ClientNet Trust Boundary Symmetry (P0-4)
- Phase 04 (신규) — Build Artifact Hygiene (P0-5, 보정 2 = hash 비교)
- Phase 05 (옛 02 rename) — Formulas.cs 분리 + PlayerStats 진짜 반영 (P0-3 + P1)
- Phase 06 (옛 03 rename) — lag comp + AABB + B1/B3 sweep (P1)

**다음 Phase = Phase 02 진입** (Session State Machine Hardening, 복잡 등급 server SubAgent).
