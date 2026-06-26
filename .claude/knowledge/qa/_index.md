---
name: knowledge-qa-index
description: QA 도메인 (99_Tools/ + 테스트 + 시뮬레이션) 학습 캐시 인덱스
domain: qa
maintainer: youngho
last_updated: 2026-05-20
---

# QA Knowledge — _index.md

> **누가 통독**: `qa` SubAgent (필수) + 작업 도메인 _index 1개 + `coordinator` / `reviewer` / `plan-auditor` (R only)
> **함께 통독**: 본 캐시 + (작업 도메인 _index) + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| _(아직 qa 전용 패턴 미발견 — 발견 시 박제)_ | — | — | — |

---

## 디테일 본문

_M7.7 완주 시점까지 qa 전용 직접 패턴 미발견. 발견 시 박제 — 헤드리스 봇/부하/퍼징 작업 진입 시 자연 누적 예상._

### 예정 후보 (M4+)

- 헤드리스 봇 시나리오 재현 패턴
- 부하 테스트 baseline 갱신 패턴
- 퍼징 입력 카탈로그

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *99_Tools/ + 테스트 코드 + 시뮬레이션* 패턴을 담습니다:

- **포함**: 헤드리스 봇 / 부하 / 퍼징 / xUnit 패턴 / deterministic 재현 / regression safety net
- **제외**:
  - 게임 코드 본체 패턴 (qa는 R only) → 각 도메인 _index
  - 환경 사고 / 툴 함정 (SAC dotnet test 등) → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../../CLAUDE.md) — qa는 게임 코드 R only
- 정책: [`../../policies/knowledge-system.md`](../../../00_Document/policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/qa.md`](../../agents/qa.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 검토 — 본 도메인은 M4 진입 후 자연 누적 예상.
- 2026-06-26 — GC 결함 정정: 플레이스홀더 "M4 진입 후 자연 박힘 예상" → "M7.7 완주까지 미발견, 발견 시 박제"로 현실화.
