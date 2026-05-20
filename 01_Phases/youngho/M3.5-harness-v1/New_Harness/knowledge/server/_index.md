---
name: knowledge-server-index
description: Server 도메인 (02_Server/ + 98_Shared/ 서버측) 학습 캐시 인덱스
domain: server
maintainer: youngho
last_updated: 2026-05-20
---

# Server Knowledge — _index.md

> **누가 통독**: `server` SubAgent (필수) + `coordinator` / `reviewer` / `plan-auditor` (R only, 필요 시)
> **함께 통독**: 본 캐시 + [`../shared/_index.md`](../shared/_index.md) + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| _(Phase 04 (2/3)에서 시드 박힘 — 예정: `lifecycle-race-broadcast-skip`)_ | — | — | — |

---

## 디테일 본문

_Phase 04 (2/3)에서 시드 항목별 ~30~50줄 박힘._

### 예정 시드

- `lifecycle-race-broadcast-skip` — N-1 fan-out broadcast 시 IsClosing session skip deterministic 재현 (Phase 04 + Phase 10)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *02_Server/ + 98_Shared/ 서버측* 패턴을 담습니다:

- **포함**: 권위 검증 / lifecycle / broadcast / handler dispatch / tick loop / 영속화 / cheat-flag
- **제외**:
  - Protocol 모양 / PDL 정의 → [`../shared/`](../shared/_index.md)
  - Unity 측 prediction / reconcile → [`../client/`](../client/_index.md)
  - 헤드리스 봇 / 부하 / 퍼징 → [`../qa/`](../qa/_index.md)
  - 환경 사고 / 툴 함정 / 마이그 패턴 → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../CLAUDE.md) "Server Authority" + "Trust Boundary" + "No Blocking Calls" 절대 원칙
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/server.md`](../../agents/server.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
