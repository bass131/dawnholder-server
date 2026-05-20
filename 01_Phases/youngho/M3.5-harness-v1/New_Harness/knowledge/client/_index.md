---
name: knowledge-client-index
description: Client 도메인 (03_Client/ + 04_ClientNet/ Unity 패턴) 학습 캐시 인덱스
domain: client
maintainer: youngho
last_updated: 2026-05-20
---

# Client Knowledge — _index.md

> **누가 통독**: `client` SubAgent + `unity-bridge` SubAgent (둘 다 필수) + `coordinator` / `reviewer` / `plan-auditor` (R only)
> **함께 통독**: 본 캐시 + [`../cross-cutting/_index.md`](../cross-cutting/_index.md)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| _(Phase 04 (2/3)에서 시드 박힘 — 예정: `prefab-overwrite-untracked-disaster`, `unity-version-hash-pinning`)_ | — | — | — |

---

## 디테일 본문

_Phase 04 (2/3)에서 시드 항목별 ~30~50줄 박힘._

### 예정 시드

- `prefab-overwrite-untracked-disaster` — PrefabUtility.SaveAsPrefabAsset 백업 없이 덮어쓰기 (M3 Phase 08 BackGround 사고)
- `unity-version-hash-pinning` — 같은 라벨 다른 hash 가능, hash까지 통일이 정답

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *03_Client/ + 04_ClientNet/ Unity 측* 패턴을 담습니다:

- **포함**: prediction / reconciliation / remote entity registry / 보간 / Unity Editor MCP / prefab/scene 작업 패턴 / Unity 버전 함정 / Cloud ID 함정
- **제외**:
  - Protocol 모양 / PDL → [`../shared/`](../shared/_index.md)
  - 서버측 권위 / lifecycle → [`../server/`](../server/_index.md)
  - 환경 사고 / 툴 함정 (Unity와 무관한) → [`../cross-cutting/`](../cross-cutting/_index.md)

---

## 관련 자산

- 헌법: [`../../CLAUDE.md`](../../CLAUDE.md) "Server Authority" (클라 = 단순 렌더러)
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)
- SubAgent 정의: [`../../agents/client.md`](../../agents/client.md) + [`../../agents/unity-bridge.md`](../../agents/unity-bridge.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움.
