# 슬래시 커맨드 빠른 참조

총 **15개**. 4개 카테고리 폴더(`learn/` `journal/` `work/` `session/`)로 정리됨 (2026-05-11). 헷갈리면 아래 "비슷한 것끼리 차이" 섹션을 보세요.

호출 형식: `/<카테고리>:<이름>` (예: `/learn:why`, `/journal:phase`).

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
| [`/work:review`](../.claude/commands/work/review.md) | 최근 변경이 헌법/ADR/구조를 잘 따르는지 자동 점검 | (인풋 없음) |
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

## 📌 세션 기록 — 노션 협업 히스토리에 박제

| 커맨드 | 언제 쓰나 | 인풋 |
|--------|----------|------|
| [`/session:start`](../.claude/commands/session/start.md) | 새 세션 시작 시 첫 입력. CONTEXT.md 읽고 톤·현재 멈춤 지점·다음 액션을 짧게 인지 확인. | (인풋 없음) |
| [`/session:log`](../.claude/commands/session/log.md) | 세션 끝 무렵, 결정·토론·코드 변경이 있었을 때 노션 박제 트리거. Claude가 Bash로 Codex CLI 호출 → Codex가 STAR 형식으로 박음 (2026-05-11 deprecation 후 새 흐름). 명세는 Codex 환경 참조용. | (인풋 없음) |

---

## 비슷한 것끼리 차이 (헷갈리기 쉬운 것)

### `/learn:why` vs `/learn:concept` vs `/learn:explain`
- **`/learn:why <X>`** — "X가 **왜** 존재해야 하는가". 동기·목적·trade-off
- **`/learn:concept <키워드>`** — "X가 **무엇**인가". 개념 자체의 학부 수준 정의
- **`/learn:explain <코드>`** — "이 **코드**가 무슨 일을 하는가". 줄 단위 풀이

### `/journal:*` 3종 vs `/session:log`
- **`/journal:*`** — `00_Document/learning-journal/`에 **로컬 마크다운**으로 저장. 본인이 답을 채움. 면접 답변 연습용.
- **`/session:log`** — **노션 DB**에 STAR로 박제. **Codex가 작성** (2026-05-11 분업 정합 — Claude는 -DONE.md까지, Notion 박기는 Codex via Bash 위임). 결정·맥락 누적용.

→ 둘은 **상호 보완**. 큰 학습이 있었으면 `/journal:concept` 쓰고, 그날 세션 자체는 `/session:log`으로도 박는 식.

### `/learn:recap` vs `/session:log`
- **`/learn:recap`** — 지금 이 자리에서 "어디까지 왔지?" 자체 점검용. 어디에도 안 박힘.
- **`/session:log`** — 노션에 영구 박제. 6개월 뒤 재참조용.

### `/work:plan` vs Phase 파일
- **`/work:plan <목표>`** — 새 Phase 묶음(`01_Phases/M{N}-{slug}/`)을 생성. **만들기**.
- **Phase 파일들** — 이미 만들어진 작업 단위. **실행**.

---

## 보통 흐름 (참고)

```
새 세션 시작
  └─ /session:start          (CONTEXT.md 읽고 톤·멈춤 지점·다음 액션 인지 확인)
        └─ /learn:recap            (필요 시 현재 위치 더 깊이 짚기)
        └─ 큰 작업이면 /work:plan <목표>   (Phase 분해)
              └─ Phase 작업 진행
                    └─ 막히면 /learn:why, /learn:explain, /learn:concept, /learn:dumb-it-down
                    └─ 코드 점검: /work:review
                    └─ Phase 끝: /journal:phase 권유 (헌법에 박혀있음)
              └─ 세션 끝: /session:log  (노션 박제, 결정 있었을 때)
```

---

## 추가 정보

- 헌법(`CLAUDE.md`)의 "사용자 컨텍스트" 섹션에 학습용/작업용/일지 커맨드의 짧은 안내 있음
- `CONTEXT.md` "슬래시 커맨드 빠른 참조" 섹션에 카테고리 요약 있음
- 새 커맨드 추가 시: (1) 알맞은 카테고리 폴더(`learn/` `journal/` `work/` `session/`)에 파일 생성, (2) 이 인덱스의 표에 추가, (3) 헌법(`CLAUDE.md`) 짧은 안내 갱신.
