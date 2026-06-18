---
name: knowledge-usage
description: SubAgent/사용자가 Knowledge를 어떻게 활용하는지 가이드 (입출력 패턴)
maintainer: youngho
last_updated: 2026-05-20
---

# Knowledge 활용 가이드 — AI/사용자 입출력 패턴

본 문서는 *각 SubAgent가 어느 _index.md를 언제 통독하고*, *새 학습은 어떻게 박는지*, *사용자가 어느 시점에 확인 요청을 받는지*를 정의합니다.

> **상위 정책**: [`../policies/knowledge-system.md`](../policies/knowledge-system.md)
> **충돌 시**: 헌법 > 정책 > 본 가이드.

---

## 1. SubAgent 자동 통독 매핑

각 SubAgent는 작업 시작 시 *자기 도메인 + cross-cutting _index.md*를 통독 (시스템 프롬프트에 박힘 — `../agents/<name>.md` 참조):

| SubAgent | 통독 대상 |
|---|---|
| `server` | `server/_index.md` + `shared/_index.md` + `cross-cutting/_index.md` |
| `shared` | `shared/_index.md` + `cross-cutting/_index.md` |
| `client` | `client/_index.md` + `cross-cutting/_index.md` |
| `qa` | `qa/_index.md` + (작업 도메인 _index 1개) + `cross-cutting/_index.md` |
| `unity-bridge` | `client/_index.md` + `cross-cutting/_index.md` |
| `reviewer` | 전체 _index.md (R only, 리뷰 시점) |
| `plan-auditor` | 전체 _index.md (R only, Phase 정의 검증 시) |
| `coordinator` | 전체 _index.md (R only, 분해 시점) |

**통독 비용**: _index.md는 활성 표 + 디테일 본문 합쳐 *목표 ~150줄 이하*. 200줄 초과 시 응축 또는 비활성 이동 (GC Collector 책임).

**적용 강제**: Phase 06 정합 마감 시 각 SubAgent 정의에 *"작업 시작 시 본인 도메인 + cross-cutting _index.md 통독"* 박음. M3.5 진행 중엔 새 SubAgent 정의가 격리 폴더 안 (옛 운영 영향 X).

---

## 2. 활성 ↔ 비활성 라이프사이클

```
[새 항목 박힘] → [활성 표 + 디테일 본문] → 3개월 무참조 → [비활성 섹션] → 6개월 무참조 → [완전 삭제]
                       ↑                                                              ↓
                       └──────── 재참조 시 활성 복귀 ────────────────────────────────┘
```

### 활성 (최근 3개월)

- 활성 표에 등재 (`_index.md` 상단)
- 디테일 본문 박힘 (~50줄/항목)
- SubAgent 통독 시 *반드시* 통과

### 비활성 / GC 대기

- "비활성 / GC 대기" 섹션으로 이동 (`_index.md` 하단)
- 디테일 본문은 *유지* (재참조 시 즉시 활성 복귀 가능)
- 통독 시 *skim* (제목만 인지)

### 완전 삭제

- 6개월 무참조 + 사용자 확인 → `_index.md`에서 한 줄 + 본문 삭제
- git history는 잔존 (옛 사고 추적 가능)

---

## 3. 박는 시점 (트리거 3종)

### 3-1. `-DONE.md` 박제 직후 (가장 흔함)

Phase 마감 시 `-DONE.md`에 *학습 일지 후보 키워드* 섹션 박힘 (옛 운영부터 정착). 그 중 *AI 활용에 직접 영향*있는 항목 → 트랙 A 후보.

**흐름**:
1. AI가 `-DONE.md` "학습 일지 후보 키워드" 섹션 검토
2. 각 키워드를 트랙 A / 트랙 B / 양쪽 / 양쪽 X 분류 제안
3. 사용자가 "트랙 A 박을 키워드" 명시 선택
4. AI가 박는 양식대로 `_index.md` 활성 표 + 디테일 본문 박음

### 3-2. CHANGELOG [H] / [M] 박힘 직후

행동 변경 또는 결정 뒤집기는 *AI 미래 작업에 영향*. 트랙 A 후보 자동 발생.

**흐름**:
1. 사용자가 CHANGELOG [H]/[M] 박은 직후
2. AI가 "이거 knowledge cross-cutting에 박을까요?" 제안
3. 사용자 결정 → 박음

### 3-3. 사용자 명시 요청

"이거 knowledge에 박아줘" 같은 명시 요청 → AI 즉시 박음. 별도 확인 X.

### 3-4. 무인 루프 발견 (loop-driven, M7.5)

루프 드라이버([`/engine:goal`](../commands/engine/goal.md))가 작업 중 학습 후보를 발견해도 **자율 박제 X** → [`pending-knowledge`](../../00_Document/ledgers/pending-knowledge.md) 큐에 *제안 누적*만. 영호 승인(아침 attended 게이트) 후 박제. ADR-025가 죽인 트랙 B "쌓이기만 하고 pull 안 됨" 함정을 pull-분리 + 가시 장부로 회피.

---

## 4. 박는 양식 (구조화 패턴)

```markdown
### `<keyword-kebab-case>`

**증상**: <한 줄, 무엇이 일어났는지>
**패턴**: <한 줄, 왜 일어나는지>
**봉합**: <한 줄, 어떻게 막는지>
**사례**: <Phase NN 또는 commit hash 1~3개>
**확신도**: 실측 N건 (Rule of Three 통과 = 3건 이상)
**관련 키워드**: [[다른 키워드 1]], [[다른 키워드 2]]
```

**원칙**:
- 한국어 회고체 X — AI 가독성으로 *구조화*. 학습 일지(트랙 B) 톤과 분리
- 항목당 ~30~50줄 목표 (200줄 한도)
- 관련 키워드 link로 그래프 형성 (옛 memory의 `[[...]]` 패턴 정합)

활성 표 한 줄 양식:
```
| `<keyword>` | <한 줄 요약> | <트리거 시점> | <검증 사례 1~3개> |
```

---

## 5. AI 자율 박제 금지 (가짜 학습 방지)

**원칙**: 사용자 확인 없이 박힌 캐시는 *AI 자기 강화 편향* 위험.

**시나리오**:
- AI가 박은 패턴 → 다음 세션 AI가 통독 → 자기가 박은 걸 인용 → 검증 없는 순환
- 사용자 확인 게이트 = 사실 검증 + 트랙 A vs B 분류 + 일반화 확신도 검토

**예외 (사용자 명시 요청)**: "이거 knowledge에 박아줘" 같은 명시 요청 시 AI 즉시 박음 — 명시 요청 자체가 확인 게이트.

**금지 사례**:
- AI가 *작업 중 발견한 패턴*을 작업 끝나자마자 자율 박제
- AI가 *반복되는 사용자 질문*을 자율 박제
- AI가 *옛 학습 일지*를 자율 흡수

---

## 6. GC 발동 시점

| 시점 | 트리거 | 책임 |
|---|---|---|
| **수동** | `/harness-review` 슬래시 호출 (Phase 05 산출물) | 사용자 결정 |
| **자동 권유** | 매 마일스톤 마감 후 `/session:end` 흐름 안 | AI 제안 + 사용자 결정 |

GC Collector 정책 4종(삭제/응축/승격/분해) → [`../agents/knowledge-gc.md`](../agents/knowledge-gc.md) (Phase 04 (2/3)에서 박힘).

---

## 7. 사용자가 직접 조회하는 경우

본인 또는 팀원이 *작업 시작 전* 도메인 캐시 직접 통독:

```bash
# 본인 작업 도메인 캐시 통독
cat .claude/knowledge/server/_index.md         # 서버 도메인
cat .claude/knowledge/cross-cutting/_index.md  # 횡단 (모든 도메인 영향)
```

학습 가치 있는 패턴 발견 시 → 메인 세션에 "이거 박아줘" 요청.

---

## 8. 함정 / 주의사항

- **AI 자율 박제 금지** — 4번 원칙 위반은 가짜 학습 누적의 가장 큰 원인
- **트랙 A에 회고 박지 마라** — 회고는 트랙 B(Notion). 트랙 A는 *구조화 패턴*만
- **GC 완전 삭제는 6개월 이후만** — 비활성 6개월 + 사용자 확인 필수. 즉시 삭제 금지
- **AI 캐시 ≠ ADR** — ADR은 *결정의 기록*. 캐시는 *작업 시점 활용 패턴*. 같은 사건이 양쪽에 박힐 수 있음
- **통독 부담 모니터링** — _index.md 200줄 초과 시 응축. 통독이 토큰 부담 큰가 1주차 실측 필요 ([`../policies/knowledge-system.md`](../policies/knowledge-system.md) 9번 항목)

---

## 9. 변경 시 동기화 책임

본 가이드 수정 시 *반드시* 함께 갱신:

- [`../policies/knowledge-system.md`](../policies/knowledge-system.md) — 상위 정책
- [`../agents/<각 SubAgent>.md`](../agents/) — 통독 약속 박힘
- [`../CLAUDE.md`](../CLAUDE.md) "📚 Knowledge 시스템" 섹션

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 박힘. 상위 정책(`knowledge-system.md`) 응축본 + 입출력 패턴 디테일.
