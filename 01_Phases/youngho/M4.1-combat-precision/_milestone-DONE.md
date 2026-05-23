---
owner: youngho
milestone: M4.1
phase: milestone-closeout
title: Combat Integrity & Portfolio Hardening — 마일스톤 마감
status: done
grade: 복잡
summary: M3 응급 전투의 P0/P1 결함을 전수 봉합 + lag compensation 정밀화 + 포트폴리오 학습 자산화. Phase 01~06 ✅ 전부 마감. Codex β cross-review(γ 방식) + reviewer Tier 2-A 이중 안전망 통과. 다음 = M4.2 Map Transition.
---

# M4.1 — Combat Integrity & Portfolio Hardening (마일스톤 마감)

**마감 일자**: 2026-05-24
**Phase 수**: 6 (옵션 A' 재구성 — P0 신뢰도 4 Phase → P1 정밀도 2 Phase)
**등급**: 복잡 (마일스톤 마감 의례)

---

## TL;DR

M3 "응급(emergency) 전투"가 *동작은 하지만 권위·신뢰경계·정밀도가 덜 박힌* 상태였던 걸, pre-M4 Codex β 감사가 짚은 **P0/P1 결함 목록**을 기준으로 전수 봉합. P0(신뢰도) = 세션 상태 머신 / 클라 framing 대칭 / 빌드 산출물 위생 / 데미지 공식 분리. P1(정밀도) = lag compensation + AABB hitbox. **두 외부 시각(α reviewer Tier 2-A + β Codex cross-review γ 8회차)이 매 Phase 교차 검증**한 게 M4.1의 핵심 프로세스 자산. 본 마감으로 *2D 사이드스크롤 MMORPG의 서버 권위 전투가 면접에서 설명 가능한 수준*으로 올라섬.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 등급 | 마감 |
|---|---|---|---|---|
| 01 | Codex β cross-review + M3 응급 하드코딩 추가 발본 | P0/P1 결함 목록 확정 + 하드코딩 audit | 복잡 | ✅ |
| 02 | Session State Machine Hardening (P0-1+P0-2) | 캐릭터 선택 강제 + 월드 진입 게이트 (신뢰경계) | 복잡 | ✅ |
| 03 | ClientNet Trust Boundary Symmetry (P0-4) | 클라 framing 검증 대칭 + FrameValidatorSymmetryTests | 보통 | ✅ |
| 04 | Build Artifact Hygiene (P0-5) | Shared.dll/ProjectSettings dirty 봉합 + hash 비교 | 보통 | ✅ |
| 05 | Damage Formulas Extraction (P0-3+P1) | Formulas.cs 순수함수 분리 + PlayerStats 98_Shared 이동 + 진짜 데미지 반영 | 복잡 | ✅ |
| 06 | lag compensation 200ms rewind + AABB hitbox (P1) | ring buffer rewind + 시점인덱스 신뢰경계 + AABB + B1/B3 sweep | 복잡 | ✅ |

**머지 이력**: Phase 01~05 + 하네스(ADR-025) = PR #50 MERGED (merge `5b2392d`, 2026-05-24). Phase 06 = 본 마감 별도 PR 예정.

---

## AC 검증 결과

| 마일스톤 AC | 결과 | 검증 |
|---|---|---|
| Phase 01~06 전부 마감 | ✅ | 각 -DONE.md + CHANGELOG 2026-05-23/24 |
| P0 결함 4종 봉합 (세션 머신 / 클라 framing 대칭 / 빌드 위생 / 데미지 공식 분리) | ✅ | Phase 02·03·04·05 |
| P1 정밀도 (lag compensation 200ms rewind + AABB hitbox) | ✅ | Phase 06 (`fd0bf3f`) |
| 빌드 green (경고 0 오류 0) | ✅ | `dotnet build Dawnholder.slnx` |
| 테스트 green | ✅ | 221통과 / 0실패 / 3skip (`dotnet test` 02_Server.Tests) |
| 이중 리뷰 통과 (α reviewer Tier 2-A + β Codex cross-review γ) | ✅ | Phase 02~04 γ 8회차 + Phase 05·06 Tier 2-A PASS |
| ProtocolVersion 정합 (M3 v3 → M3.8 v4 → M4.1 v5) | ✅ | Phase 06 bump + 문서 B3 sweep |
| trust-boundary 위험 깃발 Phase 게이트 통과 | ✅ | Phase 02·06 reviewer 5축 + race audit |

---

## 결정 흐름

1. **옵션 A' 재구성** — 옛 M4.1 plan을 *P0 신뢰도 먼저 → P1 정밀도 나중* 순서로 재배치. lag comp(P1)는 세션/신뢰경계(P0)가 단단해진 베이스 위에서만 의미 있어서 Phase 02→05 / 03→06 rename. (2026-05-23)
2. **cross-review 이중 안전망 정착** — α(reviewer Tier 2-A, Opus, 내부 5축) + β(Codex CLI, 외부 시각, γ 비교). AI 자기 과신 영역을 외부가 잡는 패턴이 γ 8회차로 정착. 큰 PR 머지 전 신뢰도 ↑.
3. **중간 머지 분리** — Phase 01~05 + 하네스를 PR #50으로 먼저 main 반영(관리자 우회, 사유 박힘), Phase 06만 별도 PR. 큰 마일스톤을 한 번에 머지하는 위험 분산.
4. **Codex 호출 분담 default** — Claude Bash 직접 호출 X, 본인 별 세션 직접 호출 default (토큰 비용 + 학습 호흡 + sandbox 결함 + 과금 동일). memory `external-tool-call-user-direct-default`.

---

## 학습 일지 후보 키워드

1. **`lag-comp-trust-only-tick-index` (★★★)** — lag compensation 신뢰 경계는 *시점 인덱스만*. 클라는 위치가 아니라 "몇 tick 전이었나"만 보내고, 위치는 서버 ring buffer에서. 좌표를 받으면 텔레포트 핵 = 헌법 #1 위반. Valve/Quake/Mirror/NGO 공통 정석. (Phase 06)
2. **`option-b-variant-third-path-with-drift-guard` (★★★)** — 공유 vs 분리 갈래의 third path: 양쪽 helper 박되 contract test로 wire invariant 고정. 헌법 #4 vs 모듈 재사용성 갈등 해결. (Phase 03)
3. **`shared-code-discipline-relocation-pattern` (★★)** — "서버 권위 = 서버 전용 코드 위치"가 아님. 판정 책임은 서버, 코드 위치는 공유 OK (클라 hint 표시). (Phase 05)
4. **`drift-guard-asymmetry-is-incomplete-equivalence` (★★★)** + **`self-overconfidence-on-design-decisions-by-ai-assistant` (★★)** — AI 자기 과신 영역을 외부 시각(Codex β)이 잡음. γ 8회차 정착. (Phase 02~04 cross-review)
5. **`new-race-dimension-not-covered-by-old` (★★)** — "한 차원 race 봉합 ≠ 다른 차원 race 안전". Phase 06 ring buffer/rewind/network dispatch 새 race 차원 audit. (Phase 06 + M3.8 통찰 흡수)

---

## false-promise 점검 결과 (ADR-024 cadence — 마일스톤 마감 의무 섹션)

M4.1 누적 false-promise 발본 **7건** (마일스톤 마감 시점 집계):
- Codex `/cross-review` 슬래시 23번째 변종 (존재 안 하는 `--files`/`--context` 옵션) — Phase 01 진입 전 봉합
- `04_ClientNet/CLAUDE.md:38` stale — Phase 02-04 cross-review
- Codex β cross-review 4건 = `98_Shared/CLAUDE.md:19` ProtocolVersion stale / `CLAUDE.md:101` slnx 본문 / `02_Server/CLAUDE.md:11` ServerCore 위치 / `04_ClientNet/CLAUDE.md:12` FrameValidator Layout 누락 — Phase 02-04
- `98_Shared/CLAUDE.md` Formulas.cs "M4 예정" stale (26번째) — Phase 05
- **Phase 06 B3 sweep** = ProtocolVersion 문서 v3/v4 → v5 선제 정정 (신규 발본 0, 잠재 stale 선제 봉합)

**판정**: M4.1은 cross-review 이중 안전망 덕에 forward false-promise(약속만 박고 미구현)는 0건, 전부 *역방향*(코드는 됐는데 문서가 stale) 또는 *도구 옵션 환각*. 매 Phase cross-review가 sweep을 강제한 게 누적 차단 효과. ad-hoc +5건 트리거 미도달.

---

## ➡️ 다음

- **M4.1 완전 마감** = Phase 06 PR 머지 시점 (본 -DONE.md는 PR 묶음에 포함).
- **M4.2 — Map Transition** 진입 (캡스톤 1 후반 6/3~6/10). 맵 간 핸드오프 + 맵 서버 레지스트리.
- **별 시점 backlog** (work-pin 박제): target도 rewind(M4.3) / capsule hitbox(M4.3) / 봇 lag 종단간 실측(timing harness) / Unity 외관 3건(본인 분담).
