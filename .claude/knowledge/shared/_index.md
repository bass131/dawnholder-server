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
| `false-promise-pattern` | 주석/문서/CLAUDE.md에 박힌 약속이 코드에 없음 — 코드+문서 동시 commit으로 봉합 | 헌법 / 도메인 README / Layout 표 갱신 시 / "약속 박기"라는 문구 | M3 Phase 02/03/04 (Rule of Three 통과 ★★★) |

---

## 디테일 본문

### `false-promise-pattern`

**증상**: 헌법 #4 ("Shared Code Discipline — 복사-붙여넣기 금지") 또는 `02_Server/CLAUDE.md` Layout에 박힌 약속(`Handlers/` 폴더 등)이 *실재로는 없음*. AI가 "여기 있을 거예요" 답변 → 실측해보면 가짜.

**패턴**: 문서 약속은 *작성 시점*의 의도. 코드 변경이 동시 commit되지 않으면 *약속만 박히고 코드는 안 박혀* 시간 지나면 가짜화. AI가 다음 세션 그 약속을 *사실*로 인용 → 검증 없는 순환.

**봉합 (코드 + 문서 동시 commit 정신)**:
- 약속 박는 commit이 *반드시* 코드 + 문서 *동시* 변경
- 코드만 박고 문서 미갱신 = 다음 세션 헷갈림
- 문서만 박고 코드 미구현 = 가짜 약속 누적
- 후속 ad-hoc 감사로 "약속 vs 코드" 격차 점검

**Rule of Three 누적 사례**:
- M3 Phase 02 (commit `e91XXX`) — 헌법 #2 "ProtocolVersion 핸드셰이크" 가짜 약속 1번째 봉합 (PDL 신설 + 코드 동시)
- M3 Phase 03 (commit `4065616`) — 헌법 #4 "Handlers/ 폴더" 가짜 약속 2번째 봉합 (02_Server/CLAUDE.md Layout *동시* commit)
- M3 Phase 04 (commit `5ea1123`) — 헌법 #4 3번째 봉합, Rule of Three 통과 → 패턴 정착

**사례**: M3 Phase 02 (1번째) → Phase 03 (2번째) → Phase 04 (3번째). Rule of Three 통과 후 M3.5에서 본 캐시에 박힘.
**확신도**: 실측 3건 (★★★). 한국 게임 회사 면접 *문서/코드 정합* 어필 결정타. 헌법 변경 시 *반드시* 코드+문서 동시 commit 강제.
**관련 키워드**: [[gamma-pre-validation-pattern]] (사전 검증으로 가짜 약속 차단 가능), [[prefab-overwrite-untracked-disaster]] (절차 박혀도 안 지키면 가짜)

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
