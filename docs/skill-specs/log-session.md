# Skill Spec: `/log-session` (가칭)

> **목적**: AI-사용자 협업 세션을 노션 "Dawnholder 협업 히스토리" DB에 자동 정리하는 슬래시 커맨드.
>
> **상태**: 구현 완료 + 첫 실호출 검증됨 (2026-05-09).
> 슬래시 커맨드: `.claude/commands/log-session.md`. 트리거 A (수동) 채택.
>
> **첫 사례 (STAR 변경 전 참고용)**: https://www.notion.so/35776ceccb7881998409f95be0e06b28
> **첫 실호출 결과**: https://www.notion.so/35b76ceccb788145bb64c26642cbcd3b (4번째 세션 머지·정리)

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
| `Date` | DATE | 세션 날짜. **expanded key 필수**: `"date:Date:start": "YYYY-MM-DD"` |
| `Topic` | RICH_TEXT | 한 줄 주제 |
| `Tags` | MULTI_SELECT | **JSON 배열 문자열**: `"Tags": "[\"GitHub\", \"Harness\"]"`. 옵션: `ADR`, `팀미팅`, `GitHub`, `Phase`, `학습`, `결정`, `코드분석`, `Harness` |
| `Status` | SELECT | `in progress`, `completed`, `archived` (보통 `completed`) |
| `PR Link` | URL | 관련 GitHub PR (있으면). 없으면 키 자체를 생략 |

### 실제 동작하는 `properties` 예시 (실호출 검증됨)

```json
{
  "Title": "2026-05-09 — 두 워크트리를 main으로 정리한 날",
  "date:Date:start": "2026-05-09",
  "Topic": "두 워크트리를 main으로 안전하게 머지하고 정리",
  "Tags": "[\"GitHub\", \"Harness\", \"결정\"]",
  "Status": "completed"
}
```

⚠️ `"Date": "2026-05-09"` (flat) 또는 `"Tags": "GitHub,Harness"` (콤마) 는 둘 다 validation 에러. 위 expanded key + JSON 배열 문자열만 동작함.

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

## 페이지 본문 템플릿 (STAR 형식)

> **2026-05-06 결정**: 회고글 + 면접 자료 톤이라 **STAR 형식 채택** (4섹션, 영어 약어는 한국어 헤더로). 처음엔 6섹션으로 짰다가 너무 무거워서 사용자 피드백("다 거슬림") 받고 갈아끼움. 첫 사례 페이지가 새 템플릿의 기준.

페이지 제목 형식: `YYYY-MM-DD — 한 줄 요약` (예: `2026-05-06 — 시나리오 B로 가기로 한 날`).

```markdown
## 상황
[어떤 맥락이었나. 시점, 외부 조건, 책상 위에 뭐가 있었는지. 1~2 단락]

## 정해야 했던 것
[이 세션에서 답해야 할 핵심 질문 1개. 1~2 문장]

## 한 행동
[조사/토론/선택. 옵션 나열 + 채택 + 채택 이유. 부수 결정도 같이 짧게]

## 결과
- [구체적 산출물 1]
- [구체적 산출물 2]
- [다음 세션 조건]

**배운 것 3가지**:

1. **[개념/사실]** — 한두 줄 풀이. 면접에서 답할 수 있게.
2. **[개념/사실]** — ...
3. **[개념/사실]** — ...

---
*다음: [핵심 다음 액션 1~2개]*
```

### 분량 가이드
- 전체 30~50줄 (기존 460줄 시도는 실패. 짧을수록 6개월 후 가치 큼)
- "배운 것"은 **정확히 3개**. 적으면 가벼움, 많으면 산만
- 코드 디테일/명령어/ID는 본문에 박지 않음 — 6개월 뒤 안 떠오를 정도면 그건 본문 없어도 됨

---

## 분석 휴리스틱

대화에서 추출할 신호 + STAR 어디로 박을지 매핑:

| 신호 종류 | 신호 예시 | STAR 어디로 |
|---|---|---|
| **세션의 외부 맥락** | 미팅 결과, 마감 임박, 팀 상황 변동 | **상황** |
| **풀어야 했던 질문** | "X를 어떻게 할지" / "A·B·C 중 뭐?" | **정해야 했던 것** |
| **옵션/트레이드오프** | 표 비교, "vs", 옵션 A/B/C 나열, "장단점" | **한 행동** |
| **결정** | 사용자: "X로 가자", "OK". Claude: "추천: X", "옵션 X 채택" | **한 행동** (끝 부분: "X로 결정. 이유: …") |
| **코드/문서 변경** | Edit/Write 도구 호출, git commit, PR URL, ADR-NNN | **결과** (구체 산출물) |
| **학습** | 사용자 `/why`/`/explain`/`/concept` 사용, Claude 개념 풀이, "오 그럼 X도 돼?" 깨달음 | **결과**의 "배운 것 3가지" |
| **다음 액션** | "다음 세션", "머지 후", "남은 것" | 하단 italics |

### 학습 추출 시 주의

"배운 것 3가지"는 가장 중요한 섹션 (면접 자료 핵심). 그래서:
- **단편 사실 나열 X** — "PDB가 뭐다" (사전적) 보다 "PDB Embedded Source 한 줄로 .cs 임베드 → Unity ReadOnly + F12로 원본까지 다 됨" (왜 이게 가치인지)
- **3개 우선순위 매기기** — 5~10개 후보 중 가장 큰 통찰만 남기기
- **"내가 처음 알게 된 것"** vs "이미 알던 것" 구분 — 후자는 빼기

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
- [ ] STAR 4섹션(상황/정해야 했던 것/한 행동/결과) + "배운 것 3가지" 다 박혀있음
- [ ] PR Link 자동 채워짐 (대화에 PR URL 있으면)
- [ ] 한글 정상 (잘못된 unicode escape 없음 — 첫 사례에서 `\uec커` 같은 잘못된 escape 으로 실패한 적 있음, 한글 직접 박기 권장)

---

## 알려진 함정

1. **JSON escape** (첫 사례): tool call 시 한글을 잘못 escape하면 (`\uec커` 같은) 파싱 깨짐. 한글 직접 박는 게 안전.
2. **content 길이** (첫 사례): 너무 길면 일부 시스템에서 `expected string to have <=100 characters` 에러. 분량 제어.
3. **parent 옵션** (첫 사례): `gh repo create --source=.`는 worktree에서 .git이 gitlink여서 실패. 노션 페이지 생성도 비슷 — parent 명시가 안 되면 workspace level로 떨어뜨림 (사용자 의도 따라).
4. **`Date` 컬럼 형식** (첫 실호출 발견, 2026-05-09): flat `"Date": "..."` ❌ → expanded `"date:Date:start": "..."` ✅. 에러 메시지에 expanded key 명시됨.
5. **`Tags` (multi_select) 형식** (첫 실호출 발견): 콤마 구분 `"GitHub,Harness"` ❌, expanded 인덱스 `"multi_select:Tags:0"` ❌ → **JSON 배열 문자열** `"[\"GitHub\", \"Harness\"]"` ✅. 데이터 소스 fetch 시 SQLite 스키마에 `JSON array` 명시돼있음 — 막히면 fetch부터.

---

## 미해결 / 다음 세션에서 결정

**해결됨 (2026-05-09 구현 + 첫 실호출)**:
- ✅ 트리거 — A (수동) 채택
- ✅ 짧은 잡담은 박지 않음 — 슬래시 커맨드 안의 "적합성 체크" 단계에서 거름
- ✅ 본문 분량 — STAR 30~50줄로 제약

**남은 것**:
- [ ] `docs/learning-journal/` (로컬 학습 일지)와 노션 로그의 관계 — 둘 다? 노션이 메인?
- [ ] STAR 4섹션이 정말 충분한가 — 몇 번 더 써본 뒤 재평가

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
