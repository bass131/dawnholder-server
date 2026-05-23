# 옛 16 → 새 10 슬래시 매핑 (Phase 05 최종)

> **⚠️ ADR-025 (2026-05-24)**: 본 문서가 가리키는 *트랙 B (학습/일지)* 자체가 은퇴됨. 아래 "트랙 B 이관" 내용은 이제 *역사 기록*. 학습은 knowledge(트랙 A)만 유지, 잔존 `learning-journal/{본인}/`는 보존.
>
> **상태**: M3.5 Phase 05 산출물. Phase 06 전환 시점에 본 폴더 안 .md → `.claude/commands/`로 일괄 mv + 옛 8개 슬래시 삭제 + `00_Document/commands-index.md` 재작성.
>
> **본 문서의 역할**: 옛 운영 슬래시 17개(학습 5 + 일지 3 + 작업 5 + 세션 3 + setup 1)를 새 운영 슬래시 10개로 다이어트 + rename + 신설하는 결정 표. 이관 처도 명시.

---

## 다이어트의 정신 (5/20 의논 결과 정합)

옛 슬래시 17개 → 새 10개. **이유**:

- **트랙 A/B 분리** — 학습 5 + 일지 3 = *학습 트랙 B*로 이관 ([`../knowledge/README.md`](../knowledge/README.md) 트랙 분리 정신 정합). 작업 KPI 전환 = 슬래시는 *작업 중심*만 유지
- **양식 수 감소 = 발견 비용 ↓** — 옛 16개는 학부생이 *언제 어느 슬래시 쓰는지* 결정 부담 ↑
- **트랙 B 이관 처** = Notion 본인 페이지 + 잔존 `00_Document/learning-journal/{본인}/` 디렉토리 (잔존분은 그대로 유지, 옛 자산 보존)
- **새 슬래시 = 옛 기능 *흡수* + 강화** — `/work:review` → `/harness-review` (하네스 자체 점검 강화) / 옛 `/work:audit` (보류 상태) → `/cross-review` 신설 (Rule of Three 통과: 5/18 pre-m3 감사 + γ 방식 4/5/6/7회차)

---

## 최종 매핑 표

### 학습 카테고리 (5개) — 트랙 B 이관 (제거)

| 옛 슬래시 | 새 위치 | 이관 처 |
|---|---|---|
| `/learn:concept` | (제거) | 본인 노션 "Dawnholder 학습 일지" DB 또는 잔존 `learning-journal/{본인}/concepts/` |
| `/learn:dumb-it-down` | (제거) | 대화 안에서 자연스럽게 처리 (Claude 직접 응답) |
| `/learn:explain` | (제거) | 대화 안에서 자연스럽게 처리 (Claude 직접 응답) |
| `/learn:recap` | (제거) | `/session:start`가 흡수 (work-pin 자동 주입) |
| `/learn:why` | (제거) | 대화 안에서 자연스럽게 처리 + ADR/policies 참조 |

**왜 제거 가능한가**: 옛 5개는 *학습 토큰 절약 슬래시*였음. 새 모델은 Claude 응답 안에서 자연스럽게 *질문하면 풀어 설명* 정신 그대로 적용. 슬래시 양식이 *학습을 더 잘하게* 만들지 않음. 노션 박제는 *기록 차원*이지 *학습 활동* 자체가 아님.

### 일지 카테고리 (3개) — 트랙 B 이관 (제거)

| 옛 슬래시 | 새 위치 | 이관 처 |
|---|---|---|
| `/journal:bug` | (제거) | 본인 노션 "Dawnholder 트러블슈팅" 페이지 (또는 잔존 `learning-journal/{본인}/bugs/`) |
| `/journal:concept` | (제거) | 본인 노션 "Dawnholder 학습 일지" DB |
| `/journal:phase` | (제거) | 본인 노션 (Phase 완료 시 권유 = `-DONE.md` 학습 키워드 + 본인 회고체 박음) |

**왜 제거 가능한가**: 옛 3개는 *질문지 던지기 + 본인 답 채움* 구조. 새 모델은 본인 노션에서 직접 자유 양식으로 박음 (학부생 회고체 + 면접 무기 누적). AI 인터뷰 형식 자체는 그대로 유지 가능 — 사용자가 "노션에 박을 회고 도와줘" 요청 시 Claude가 인터뷰 질문 던지면 됨.

### 작업 카테고리 (5개) — 4 유지 + 1 rename

| 옛 슬래시 | 새 슬래시 | 변경 |
|---|---|---|
| `/work:plan` | `work/plan.md` | 유지 + 정합 갱신 (4등급 명시 + `plan-auditor` SubAgent 자동 호출 + Phase 입자 5~7개/마일스톤 권장) |
| `/work:new-packet` | `work/new-packet.md` | 유지 + 정합 갱신 (옛 `netcode` → 새 `shared`+`server` SubAgent 분담 + `shared-discipline-guard` Hook 자동 발동) |
| `/work:new-monster` | `work/new-monster.md` | 유지 + 정합 갱신 (옛 `content` SubAgent 삭제 → `qa`(데이터 값) + `shared`(스키마) 분담) |
| `/work:load-test` | `work/load-test.md` | 유지 + 정합 갱신 (`qa` SubAgent 명시 — 옛 `qa-sim`에서 rename) |
| `/work:review` | `harness-review.md` | **rename + 강화** — 본인 하네스 자체 점검 슬래시로 책임 *확장*. Tier 3 수동 깊은 리뷰 + 하네스 정합 점검 통합 |

### 세션 카테고리 (3개) — 3 유지

| 옛 슬래시 | 새 슬래시 | 변경 |
|---|---|---|
| `/session:start` | `session/start.md` | 유지 + 정합 갱신 (work-pin 압축 양식 30~40줄 정합 + CHANGELOG 확인 절차 + git 게이트 (B+) 정책 유지) |
| `/session:end` | `session/end.md` | 유지 + 정합 갱신 (Phase 완료 권유 정합 + work-pin 갱신 흐름 — ADR-025로 CONTEXT 동기 은퇴 + 등급별 마감 절차 분기) |
| `/session:log` | `session/log.md` | 유지 + 정합 갱신 (ADR-016 그대로 + 트랙 A/B 분리 정신 박힘) |

### 진입점 (1개) — 1 유지

| 옛 슬래시 | 새 슬래시 | 변경 |
|---|---|---|
| `/setup` | `setup.md` | 유지 + 정합 갱신 (팀 namespace 영호/유현/인규 + 합류 시점 정합) |

### 신규 2개 — 점검 카테고리

| 새 슬래시 | 책임 | 동원 SubAgent | 산출물 |
|---|---|---|---|
| `harness-review.md` | 본인 하네스 자체 점검 (헌법 / SubAgent / Hook / Knowledge / 슬래시) | `reviewer` + `plan-auditor` + (옵션) `knowledge-gc` | `00_Document/reviews/YYYY-MM-DD-harness-review-{scope}.md` |
| `cross-review.md` | 외부 시각 cross-check (큰 PR 박기 전, 비가역 변경 전) | `reviewer` + (옵션) Codex β cross-check | `00_Document/reviews/YYYY-MM-DD-cross-review-{slug}.md` + (있으면) Codex 출력 |

---

## 새 슬래시 10개 합계

| 카테고리 | 개수 | 슬래시 목록 |
|---|---|---|
| 작업 | 4 | `work/plan` + `work/new-packet` + `work/new-monster` + `work/load-test` |
| 세션 | 3 | `session/start` + `session/end` + `session/log` |
| 점검 | 2 | `harness-review` + `cross-review` |
| 셋업 | 1 | `setup` |
| **합계** | **10** | (옛 16/17 → 새 10, ~38% 다이어트) |

---

## 변환 트리거 (Phase 06 전환 시)

Phase 06 정합 마감 commit에서:

```bash
# 1. 옛 슬래시 삭제
git rm -r .claude/commands/learn/        # 5개
git rm -r .claude/commands/journal/      # 3개
git rm .claude/commands/work/review.md   # → harness-review로 이동

# 2. 새 슬래시 이동
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/work/*.md .claude/commands/work/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/session/*.md .claude/commands/session/
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/setup.md .claude/commands/setup.md
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/harness-review.md .claude/commands/harness-review.md
git mv 01_Phases/youngho/M3.5-harness-v1/New_Harness/commands/cross-review.md .claude/commands/cross-review.md

# 3. commands-index.md 재작성
# 옛 16 → 새 10 반영, "비슷한 것끼리 차이" 섹션 갱신

# 4. CHANGELOG [H] entry 박음 — 모든 팀원 슬래시 동작 변경
```

---

## 트랙 B 이관 안내 (제거된 8개 슬래시 사용자 안내)

학부생/팀원이 옛 슬래시 호출 시 *"이 슬래시는 사라졌어요"* 메시지 + 트랙 B 안내:

```
/learn:* 또는 /journal:* 호출 시:

⚠️ 이 슬래시는 M3.5 새 하네스 v1에서 *제거*됐어요.
   학습/일지는 트랙 B (Notion + 잔존 learning-journal/) 분리 결정 결과.

대체:
  - 학습 풀이 → 대화 안에서 "이거 왜 이래?" 같이 자연스럽게 물어보세요
  - 회고 박음 → 본인 노션 "Dawnholder 학습 일지" 페이지에 자유 양식
  - Phase 회고 → -DONE.md "학습 일지 후보 키워드" + 본인 노션
```

위 안내는 Phase 06 전환 commit 시점에 본인이 디스코드/슬랙으로 한 번 박음.

---

## 학습 키워드 후보

본 매핑 결정에서 박힌 학습 (Phase 05 -DONE.md에서 트랙 B 후보로):

- `slash-diet-via-track-split` — 슬래시 다이어트는 *트랙 분리*가 핵심. 옛 학습 5 + 일지 3 = 트랙 B 분리 결정으로 자연 제거. 무리한 통합 X
- `rename-vs-strengthen` — 옛 `/work:review` → 새 `/harness-review` rename이지만 *책임 확장* (코드 리뷰 + 하네스 자체 점검). 단순 이름 변경 X
- `cross-review-rule-of-three` — 옛 `/work:audit` 보류 (Rule of Three 미통과) → 5/18 + γ 4~7회차 실측 누적 후 `/cross-review`로 박힘. 시점 판단 정신
- `track-b-migration-without-asset-loss` — 옛 `learning-journal/` 잔존분 *유지* + 노션 본인 페이지 *추가* = 자산 보존 + 트랙 분리 동시

---

## 갱신 이력

- 2026-05-21 — M3.5 Phase 05 (1/N) 신설. 옛 16/17 → 새 10 매핑 최종본 박음.
