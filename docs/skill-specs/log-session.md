# Skill Spec: `/log-session` (가칭)

> **목적**: AI-사용자 협업 세션을 노션 "Dawnholder 협업 히스토리" DB에 자동 정리하는 슬래시 커맨드.
>
> **상태**: 명세만 작성됨 (2026-05-06). 다음 새 세션에서 이 파일을 인풋으로 슬래시 커맨드 구현.
>
> **첫 사례 (참고용)**: https://www.notion.so/35776ceccb7881998409f95be0e06b28

---

## 만드는 이유

11월 본 마감까지 매 세션마다 결정/토론/학습이 누적됨. 손으로 정리하면 번거로움 → 자동화하면:

1. **면접 자료 자동 누적** — 6개월 뒤 "이 결정은 왜?"에 답할 1차 자료
2. **학습 누적** — 트레이드오프 토론이 휘발 안 됨
3. **다음 세션 핸드오프 보강** — `CONTEXT.md` 단일 진실 + 노션 누적 = 이중 안전망

---

## 사용 시나리오

세션 끝 무렵, 사용자가 명시적으로 호출:
- 슬래시: `/log-session`
- 또는 자연어: "이 세션 노션에 정리해줘", "히스토리화해줘"

---

## 노션 자산 (이미 만들어진 것)

### 부모 페이지 "Dawnholder 협업 히스토리"
- URL: https://www.notion.so/35776ceccb78810ea516f743e5028110
- ID: `35776cec-cb78-810e-a516-f743e5028110`

### DB "세션 로그"
- URL: https://www.notion.so/4184f5a9764e46229a1cb70b5a15ff51
- **Data Source ID** (페이지 생성 시 사용): `7f1c9432-674a-4df9-b151-3c5ffeb335f3`
- 위치: 부모 페이지 아래

⚠️ 위 ID들은 사용자 워크스페이스 종속이라 다른 환경에서 재사용 불가. 노션이 이전됐거나 DB 재생성됐으면 ID 갱신 필요.

---

## DB 스키마

| 컬럼 | 타입 | 설명 / 값 |
|---|---|---|
| `Title` | TITLE | `YYYY-MM-DD — 한 줄 요약` 형식. 예: `2026-05-06 — 팀 미팅 결과 + 시나리오 B + ADR 정렬 + PR #1` |
| `Date` | DATE | 세션 날짜. expanded property: `date:Date:start` |
| `Topic` | RICH_TEXT | 한 줄 주제 |
| `Tags` | MULTI_SELECT | JSON 배열 string. 옵션: `ADR`, `팀미팅`, `GitHub`, `Phase`, `학습`, `결정`, `코드분석`, `Harness` |
| `Status` | SELECT | `in progress`, `completed`, `archived` (보통 `completed`) |
| `PR Link` | URL | 관련 GitHub PR (있으면) |

---

## 동작 흐름

1. **세션 대화 분석** (아래 휴리스틱)
2. **본문 생성** (아래 템플릿)
3. **노션 페이지 생성** — `notion-create-pages` 도구
   - parent: `{ "type": "data_source_id", "data_source_id": "7f1c9432-674a-4df9-b151-3c5ffeb335f3" }`
   - properties: 위 스키마대로
   - icon: 적절한 emoji (📌 / 🛠️ / 📚 / 🎯 등 세션 성격에 따라)
4. **사용자 보고**: 페이지 URL + 한 줄 요약

---

## 페이지 본문 템플릿

```markdown
## 한 줄 요약
[1~2 문장. 세션의 핵심 결과]

## 결정 사항

### 1. [결정 제목]
- [세부 1]
- [세부 2]

### 2. [결정 제목]
- ...

## 핵심 토론 (트레이드오프)

### [토론 제목]
- 옵션 비교
- 채택 이유
- 학습 포인트

## 코드/문서 변경

**커밋**:
- [hash] — [subject]

**변경 파일**:
- [경로] — [짧은 설명]

**GitHub** (있으면):
- repo: [repo]
- PR #N: [URL]

## 학습 포인트
- [개념 1]: 한 줄 풀이
- [개념 2]: 한 줄 풀이

## 다음 액션

**머지 후 / 다음 세션**:
- [작업 1]

**중기**:
- [작업 2]
```

---

## 분석 휴리스틱

대화에서 추출할 신호:

### 결정 신호
- 사용자: "X로 가자", "확정", "추천대로", "그렇게 가자", "OK", "B로 결정"
- Claude: "**추천: X**", "**옵션 X 채택**", "결정: ..."

### 트레이드오프 신호
- 표 형식 비교 (`| ... | ... |`)
- "vs", "장단점", "트레이드오프"
- "이유: / 단점:"
- 옵션 A/B/C 나열

### 코드/문서 변경 신호
- `Edit` / `Write` 도구 호출 흔적
- `git commit` 메시지
- PR URL (`#NNN` 또는 `pull/NNN`)
- ADR 번호 (`ADR-NNN`)

### 학습 포인트 신호
- 사용자가 `/why`, `/explain`, `/concept` 슬래시 호출
- Claude가 전문 용어 첫 사용 시 한 줄 풀어쓰기
- 사용자가 "오 그럼 X도 되는 거야?" 같은 깨달음 표현
- ".NET", "Unity", "git", "TCP" 같은 기술 키워드 + 풀이

### 다음 액션 신호
- 마지막 부분의 "다음 액션", "다음 세션", "남은 것"
- 헌법 톤 가이드의 "다음 스텝" 섹션

### 태그 자동 매핑
| 세션 내용 | 태그 |
|---|---|
| ADR 작성/갱신 | `ADR` |
| 팀원 미팅 결과 | `팀미팅` |
| GitHub repo/PR/git | `GitHub` |
| Phase 작업 | `Phase` |
| `/why`, `/concept` 사용 / 개념 풀이 | `학습` |
| 큰 결정 (시나리오 선택, 스택 변경) | `결정` |
| 기존 코드 분석 | `코드분석` |
| 헌법/Harness/CLAUDE.md/CONTEXT.md 변경 | `Harness` |

---

## 슬래시 커맨드 파일 위치

- `.claude/commands/log-session.md`
- 본인 기존 `.claude/commands/journal-bug.md`, `journal-phase.md` 등과 형식 일관

다음 세션에서 만들 때:
1. `.claude/commands/journal-*.md` 하나 먼저 읽고 형식 파악
2. 그 형식대로 `log-session.md` 작성
3. 본 명세를 인풋으로 사용

---

## 트리거 옵션 (다음 세션에서 결정)

### A. 수동 (추천 — 노이즈 적음)
사용자가 세션 끝에 명시적 `/log-session` 호출

### B. 자동 (헌법 hook)
헌법의 Phase 완료 시 학습 일지 권유와 비슷하게, 코드 작업 끝나면 권유:
> "이 세션 노션에도 박을까요? `/log-session` 추천."

### C. 하이브리드
큰 결정/PR 발생 시에만 자동 권유 + 평소엔 수동

---

## 작성 후 검증 (테스트)

- [ ] DB에 row 정상 생성됨
- [ ] Tags / Status 정확히 매핑됨
- [ ] 페이지 본문 6개 섹션(요약/결정/토론/변경/학습/액션) 다 박혀있음
- [ ] PR Link 자동 채워짐 (대화에 PR URL 있으면)
- [ ] 한글 정상 (잘못된 unicode escape 없음 — 첫 사례에서 `\uec커` 같은 잘못된 escape 으로 실패한 적 있음, 한글 직접 박기 권장)

---

## 알려진 함정 (첫 사례에서 발견)

1. **JSON escape**: tool call 시 한글을 잘못 escape하면 (`\uec커` 같은) 파싱 깨짐. 한글 직접 박는 게 안전.
2. **content 길이**: 너무 길면 일부 시스템에서 `expected string to have <=100 characters` 에러. 분량 제어.
3. **parent 옵션**: `gh repo create --source=.`는 worktree에서 .git이 gitlink여서 실패. 노션 페이지 생성도 비슷 — parent 명시가 안 되면 workspace level로 떨어뜨림 (사용자 의도 따라).

---

## 미해결 / 다음 세션에서 결정

- [ ] 자동 호출 vs 수동 (트리거 A / B / C)
- [ ] 본문 길이 limit (예: 200줄 압축?)
- [ ] `docs/learning-journal/` (로컬 학습 일지)와 노션 로그의 관계 — 둘 다? 노션이 메인?
- [ ] 세션 도중 짧은 잡담만 한 경우 (예: 5분 대화) — 그것도 박을지?

---

## 다음 세션 시작 예시

```
사용자: "log-session.md 보고 슬래시 커맨드 만들자"

Claude:
1. docs/skill-specs/log-session.md 읽기
2. .claude/commands/journal-bug.md 같은 기존 슬래시 커맨드 형식 확인
3. .claude/commands/log-session.md 작성 (본 명세 기반)
4. 다음 협업 세션 끝에서 첫 자동 테스트
```

---

*작성일*: 2026-05-06
*작성자*: Claude Opus 4.7 (1M context)
*트리거*: 사용자가 "이 협업 과정 노션에 히스토리화 + 스킬화" 요청
