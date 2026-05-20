---
name: knowledge-shared-index
description: Shared 도메인 (98_Shared/ Protocol/공식/공유 상수) 학습 캐시 인덱스
domain: shared
maintainer: youngho
last_updated: 2026-05-20
---

# Shared Knowledge — _index.md

> **누가 통독**: `shared` SubAgent (필수) + `server` (함께 통독) + `coordinator` / `reviewer` / `plan-auditor` (R only)
> **함께 통독**: 본 캐시 + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| _(Phase 04 (2/3)에서 시드 박힘 — 예정: `false-promise-pattern`)_ | — | — | — |

---

## 디테일 본문

_Phase 04 (2/3)에서 시드 항목별 ~30~50줄 박힘._

### 예정 시드

- `false-promise-pattern` — 주석/문서에 박힌 약속이 코드에 없음 (헌법 #4 + #2 봉합 패턴, Rule of Three 통과)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *98_Shared/ Protocol + 공식 + 공유 상수* 패턴을 담습니다:

- **포함**: PDL 정의·생성기 / Protocol.Version bump / 공유 enum / 공식 (데미지 계산 등) / cross-platform 직렬화 / Shared.dll 동기화
- **제외**:
  - 서버측 실행 로직 (handler / lifecycle) → [`../server/`](../server/_index.md)
  - 클라측 dispatch / prediction → [`../client/`](../client/_index.md)
  - 환경 사고 / 툴 함정 → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../CLAUDE.md) "Protocol is Sacred" + "Shared Code Discipline" 절대 원칙
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/shared.md`](../../agents/shared.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
