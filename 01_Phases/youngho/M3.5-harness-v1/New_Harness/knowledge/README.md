---
name: knowledge-readme
description: Knowledge 폴더 진입점 — 도메인 5 + _usage 가이드
maintainer: youngho
last_updated: 2026-05-20
---

# Knowledge — AI 캐시 (트랙 A)

각 SubAgent가 작업 시작 시 자동 통독하는 도메인별 *학습 캐시*. 옛 운영의 함정(매 세션 백지 시작 → 같은 사고 반복)을 차단하기 위한 인프라.

> **헌법 참조**: [`../CLAUDE.md`](../CLAUDE.md) "📚 Knowledge 시스템" 섹션
> **정책**: [`../policies/knowledge-system.md`](../policies/knowledge-system.md)
> **활용 가이드**: [`_usage.md`](_usage.md)

---

## 폴더 구조

| 도메인 | 누가 통독 | 용도 |
|---|---|---|
| [`server/`](server/_index.md) | `server` SubAgent | 02_Server/ + 98_Shared/ 서버측 패턴 |
| [`shared/`](shared/_index.md) | `shared` SubAgent + `server` (통독) | 98_Shared/ Protocol/공식/공유 상수 |
| [`client/`](client/_index.md) | `client` + `unity-bridge` (통독) | 03_Client/ + 04_ClientNet/ Unity 패턴 |
| [`qa/`](qa/_index.md) | `qa` SubAgent | 99_Tools/ 테스트/봇/퍼징 패턴 |
| [`cross-cutting/`](cross-cutting/_index.md) | **전 SubAgent 통독** | 도메인 횡단 (보안/툴 함정/마이그/환경 사고) |

---

## 트랙 A ↔ 트랙 B (가짜 학습 방지)

| 트랙 | 위치 | 용도 | 작성자 |
|---|---|---|---|
| **트랙 A — Knowledge** | 본 폴더 | AI가 직접 조회·활용 (구조화 패턴) | AI 박제 (사용자 확인 후) |
| **트랙 B — 학습 일지** | Notion + `00_Document/learning-journal/` 잔존분 | 본인 회고·면접 무기 | 본인 작성 (AI는 인터뷰만) |

**경계**: 트랙 A = *코드 패턴·도메인 결함·재현 시나리오*. 트랙 B = *왜 그렇게 결정했나·내가 무엇을 배웠나*. 같은 사건이 양쪽에 박힐 수 있으나 시각이 다름.

---

## 박제 시점 (AI 자율 박제 금지)

| 시점 | 트리거 | 책임 |
|---|---|---|
| `-DONE.md` 박제 직후 | `학습 일지 후보 키워드` 섹션 검토 → 트랙 A 후보 추출 | AI 제안 + 사용자 결정 |
| CHANGELOG [H]/[M] 박힘 직후 | 행동 변경 또는 결정 뒤집기 = 트랙 A 후보 | AI 제안 + 사용자 결정 |
| 사용자 명시 요청 | "이거 knowledge에 박아줘" | AI 즉시 박제 |

**원칙**: 사용자 확인 없이 박힌 캐시 = *AI 자기 강화 편향* 위험. 사용자 확인 게이트 필수.

---

## GC Collector

오래된·중복·결함 캐시 정리. 디테일 → [`../agents/knowledge-gc.md`](../agents/knowledge-gc.md) (Phase 04 (2/3)에서 박힘).

- **자동 비활성화**: 3개월 무참조 → 비활성 섹션 이동
- **완전 삭제**: 6개월 무참조 + 사용자 확인 → 삭제 (git history 잔존)
- **결함 정정**: 후속 실측에서 false 판명 → 즉시 정정 또는 삭제
- **승격**: 사용 빈도 ↑↑ + 학습 가치 ★★★ → ADR 박제 권유 (사용자 결정)

---

## 새 항목 박는 양식

```markdown
### `<keyword-kebab-case>`

**증상**: <한 줄, 무엇이 일어났는지>
**패턴**: <한 줄, 왜 일어나는지>
**봉합**: <한 줄, 어떻게 막는지>
**사례**: <Phase NN 또는 commit hash 1~3개>
**확신도**: 실측 N건 (Rule of Three 통과 = 3건 이상)
**관련 키워드**: [[다른 키워드 1]], [[다른 키워드 2]]
```

활성 표 한 줄 양식:
```
| `<keyword>` | <한 줄 요약> | <트리거 시점> | <검증 사례 1~3개> |
```

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 5/20 의논 결과(Knowledge 시스템 풀세트 + GC + 트랙 A/B) 시드.
