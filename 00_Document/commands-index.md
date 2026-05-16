# 슬래시 커맨드 빠른 참조

총 **16개**. 4개 카테고리 폴더(`learn/` `journal/` `work/` `session/`) + 단독 진입점(`setup.md`)으로 정리됨 (2026-05-14 협업 셋업 작업으로 갱신). 헷갈리면 아래 "비슷한 것끼리 차이" 섹션을 보세요.

호출 형식: `/<카테고리>:<이름>` (예: `/learn:why`, `/journal:phase`) 또는 단독 진입점 (`/setup`).

---

## 🎓 학습용 — 막혔거나 이해가 부족할 때

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/learn:why`](../.claude/commands/learn/why.md) | "이게 왜 필요해?"가 떠오를 때. 어떤 개념·파일·결정의 존재 이유 처음부터 | `<주제>` |
| [`/learn:explain`](../.claude/commands/learn/explain.md) | 특정 코드 블록을 한 줄 한 줄 풀어 듣고 싶을 때 | `<코드>` |
| [`/learn:concept`](../.claude/commands/learn/concept.md) | 개념 자체(코드 외 일반 지식)를 학부 수준으로 설명 받고 싶을 때 | `<키워드>` |
| [`/learn:dumb-it-down`](../.claude/commands/learn/dumb-it-down.md) | 직전 Claude 답변이 어려웠을 때, 더 쉬운 말로 재설명 | (인풋 없음) |
| [`/learn:recap`](../.claude/commands/learn/recap.md) | 지금까지 진행 + 다음 할 일을 한 번에 정리해보고 싶을 때 | (인풋 없음) |

---

## 🛠️ 작업용 — 실제 코드·구조 변경

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/work:plan`](../.claude/commands/work/plan.md) | 큰 목표를 1~3시간짜리 Phase들로 쪼개고 싶을 때 | `<목표>` |
| [`/work:review`](../.claude/commands/work/review.md) | **Tier 3** 수동 깊은 리뷰 — Phase 단위로 최근 변경이 헌법/ADR/구조를 잘 따르는지 자세한 점검. *Tier 2 자동 리뷰(reviewer 에이전트, 코드 변경당 호출)과는 별도* — Tier 3는 *사용자 명시 호출*이고 더 상세함. | (인풋 없음) |
| [`/work:new-packet`](../.claude/commands/work/new-packet.md) | 새 패킷을 클라/서버 양쪽 wiring까지 한 번에 추가 | `<C2S\|S2C> <name>` |
| [`/work:new-monster`](../.claude/commands/work/new-monster.md) | 새 몬스터 데이터 추가 (엔진 코드 변경 없음) | `<name> <level> <map>` |
| [`/work:load-test`](../.claude/commands/work/load-test.md) | 헤드리스 봇 부하 테스트 시나리오 실행 + 리포트 | `<scenario> <bots> [duration]` |

---

## 📚 학습 일지 — 면접 무기로 누적 (인터뷰 형식, 본인이 채움)

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/journal:phase`](../.claude/commands/journal/phase.md) | Phase 하나 끝났을 때. 디테일 잊히기 전에 작성 (15~20분) | (인풋 없음) |
| [`/journal:concept`](../.claude/commands/journal/concept.md) | 깊이 학습한 개념을 본인 말로 정리할 때 | `<키워드>` |
| [`/journal:bug`](../.claude/commands/journal/bug.md) | 막혔다가 풀린 사건을 트러블슈팅 일지로 박을 때 | (인풋 없음) |

⚠️ Claude가 답을 채우지 않습니다. 인터뷰 질문만 던지고 본인이 답함 (가짜 학습 방지).

---

## 📌 세션 관리 — 시작·마감·박제

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/session:start`](../.claude/commands/session/start.md) | 새 세션 시작 시 첫 입력. CONTEXT.md 읽고 톤·현재 멈춤 지점·다음 액션 + CHANGELOG 최근 변경 짧게 인지 확인. | (인풋 없음) |
| [`/session:end`](../.claude/commands/session/end.md) | Phase 완료 마감 절차. -DONE.md 박제 후 호출 → commit + PR + `/session:log` 자동 호출 + **CONTEXT.md 자동 갱신**(멈춤 지점·학습 일지 후보·History 한 줄, 미리보기 컨펌) + 다음 액션 결정까지 한 흐름. 학부생 상세 안내는 `team-guide.html` 위임. | (인풋 없음) |
| [`/session:log`](../.claude/commands/session/log.md) | 노션 박제 트리거. 보통 `/session:end`가 자동 호출. 실행자 분기: Codex 있으면 Codex가 박음 (본인 유영호 흐름), Codex 없으면 Claude가 mcp__notion 직접 호출 (인규/유현 fallback). | (인풋 없음) |

---

## 🚀 협업 셋업 — 단독 진입점

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/setup`](../.claude/commands/setup.md) | 팀원 첫 합류 시 호출. 자기소개 → 환경 검증 → 역할별 셋업 → 자산 초기화 → 첫 작업 안내까지 차근차근. 한 번에 한 단계씩 떠먹임. | (인풋 없음) |

(내부 단계 파일은 `.claude/setup-steps/`에 박혀있으나 직접 호출 안 함. `/setup`이 흐름 제어.)

---

## 비슷한 것끼리 차이 (헷갈리기 쉬운 것)

### `/work:review` vs Tier 2 자동 리뷰 (reviewer 에이전트) — ADR-019
- **`/work:review` (Tier 3)** — 사용자가 *명시 호출*. Phase 완료 시점 등 큰 단위 재점검. 상세 보고서 출력.
- **Tier 2 reviewer 에이전트** — 메인 세션이 *자동 호출* (코드 변경 후 트리거 조건 충족 시). 요약만 출력. 사용자가 명시 조작 불필요 (우회는 가능 — 헌법 "Tier 2 자동 리뷰" 섹션 참조).

→ 둘은 **상호 보완**. 호출 방식과 출력 폭이 다름. 아키텍쳐 점검 기준은 둘 다 동일하게 [`REVIEW_CHECKLIST.md`](REVIEW_CHECKLIST.md).

### `/learn:why` vs `/learn:concept` vs `/learn:explain`
- **`/learn:why <X>`** — "X가 **왜** 존재해야 하는가". 동기·목적·trade-off
- **`/learn:concept <키워드>`** — "X가 **무엇**인가". 개념 자체의 학부 수준 정의
- **`/learn:explain <코드>`** — "이 **코드**가 무슨 일을 하는가". 줄 단위 풀이

### `/journal:*` 3종 vs `/session:log`
- **`/journal:*`** — `00_Document/learning-journal/{본인-네임스페이스}/`에 **로컬 마크다운**으로 저장. 본인이 답을 채움. 면접 답변 연습용.
- **`/session:log`** — **본인 노션**에 STAR로 박제. 실행자 분기 (Codex/Claude). 결정·맥락 누적용. **각자 자기 노션 페이지/DB** (협업 셋업 결정 — 팀 공유 X, 포트폴리오용).

→ 둘은 **상호 보완**. 큰 학습이 있었으면 `/journal:concept` 쓰고, 그날 세션 자체는 `/session:log`으로도 박는 식.

### `/session:start` vs `/session:end` vs `/session:log`
- **`/session:start`** — 세션 **시작**. CONTEXT 인지 + CHANGELOG 최근 변경 확인. 작업 시작 전 항상.
- **`/session:end`** — **Phase 완료** 마감 절차. commit + PR + 박제 + **CONTEXT 자동 갱신** + 다음 액션. Phase 단위로 호출.
- **`/session:log`** — 노션 **박제만**. 보통 `/session:end`가 호출. 본인이 직접 호출하는 경우는 Phase 외 큰 결정 박을 때.

### `/learn:recap` vs `/session:log`
- **`/learn:recap`** — 지금 이 자리에서 "어디까지 왔지?" 자체 점검용. 어디에도 안 박힘.
- **`/session:log`** — 노션에 영구 박제. 6개월 뒤 재참조용.

### `/work:plan` vs Phase 파일
- **`/work:plan <목표>`** — 새 Phase 묶음(`01_Phases/<본인 네임스페이스>/M{N}-{slug}/`)을 생성. **만들기**.
- **Phase 파일들** — 이미 만들어진 작업 단위. **실행**.

### `/setup` vs `/session:start`
- **`/setup`** — 팀원 **첫 합류**. 환경 검증 + 자산 초기화. **단 한 번** 호출.
- **`/session:start`** — 매 세션 **시작**. 인지 확인. **매번** 호출.

---

## 보통 흐름 (참고)

### 첫 합류 (단 한 번)
```
clone + Claude Code 설치 후 첫 호출
  └─ /setup                    (자기소개 → 환경 검증 → 역할별 → 자산 초기화 → 첫 작업 안내)
```

### 일상 작업 흐름
```
새 세션 시작
  └─ /session:start            (CONTEXT 인지 + CHANGELOG 최근 변경 확인)
        └─ /learn:recap            (필요 시 현재 위치 더 깊이 짚기)
        └─ 큰 작업이면 /work:plan <목표>   (Phase 분해)
              └─ Phase 작업 진행
                    └─ 막히면 /learn:why, /learn:explain, /learn:concept, /learn:dumb-it-down
                    └─ 코드 점검: /work:review
                    └─ Phase 끝: -DONE.md 박제 + 5단계 보고
                          └─ 학습 일지: /journal:phase 권유 (선택)
                          └─ 마감: /session:end  (commit + PR + /session:log 자동 호출)
```

---

## 추가 정보

- 헌법(`CLAUDE.md`)의 "사용자 컨텍스트" 섹션에 학습용/작업용/일지 커맨드의 짧은 안내 있음
- `CONTEXT.md` "슬래시 커맨드 빠른 참조" 섹션에 카테고리 요약 있음
- 새 커맨드 추가 시: (1) 알맞은 카테고리 폴더(`learn/` `journal/` `work/` `session/`)에 파일 생성, (2) 이 인덱스의 표에 추가, (3) 헌법(`CLAUDE.md`) 짧은 안내 갱신, (4) `.claude/CHANGELOG.md` 한 줄 박기.
