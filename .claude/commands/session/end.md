---
description: Phase 완료 마감 절차 — commit + PR + 노션 박제 + 다음 액션 결정까지 한 흐름. /session:log 자동 호출.
---

사용자가 Phase 완료 마감을 요청했습니다. 본인 헌법의 "Phase 완료 시 두 액션 권유" 중 두 번째 액션을 실행합니다.

## 이 커맨드의 역할

`-DONE.md` 박제 직후 호출되어 commit + PR + 노션 박제 + 다음 액션 결정까지 한 흐름으로 진행. 학부생 백지 팀원이 PR 만들기 깜빡하거나 노션 박제 누락하는 사고 방지.

## 진행 절차

### 1. 사전 검증

다음 두 가지 확인 후 진행:

**1-A. `-DONE.md` 박제 존재 확인**

```bash
git status --porcelain | grep -E '\-DONE\.md$'
```

결과 분석:
- `-DONE.md` 끝나는 파일이 staged 또는 untracked로 잡힘 → 정상, 다음으로
- `-DONE.md` 없음 → **STOP**:
  ```
  ⚠️ commit 대기 중인 -DONE.md가 없어요.

  Phase 완료 = -DONE.md 박제가 먼저 박혀야 합니다.
  본인 헌법: `-DONE.md` Write/Edit → validate-phase-gate.sh 통과 → 그 다음 /session:end.

  지금 상황:
  - Phase 진행 중인데 마감하려는 건가요? → -DONE.md 박제부터
  - 단순 ad-hoc 작업 마감? → /session:log 직접 호출하면 됩니다 (Phase 단위 아닌 일반 박제)
  - 잘못 호출? → 그냥 종료
  ```

**1-B. 작업 봉투 검증**

본인 헌법: 코드 작업 봉투는 매 응답에 강제. -DONE.md 박을 때 봉투 박혀있어야 정상. 봉투 누락 의심되면 사용자에게 한 줄 확인.

### 2. 사용자 컨텍스트 수집

다음 정보를 사용자에게 묻기 (한 번에 다 묻지 말고 차근차근):

**2-A. 다음 액션 방향**

```
Phase 마감 진행할게요. 마감 후 다음 액션 어떻게 할 거예요?

1. 계속 — 바로 다음 Phase 진입
2. 종료 — 오늘 작업 끝, 다음 세션에서 이어감
3. 학습 일지 먼저 — 마감 후 /journal:phase로 회고 작성

답 듣고 그에 맞춰 진행할게요.
```

응답 받아 변수 `next_action`에 박음 (continue / stop / journal).

### 3. Commit 진행

**3-A. commit 메시지 초안 작성**

작업 봉투 내용 + Phase 정보 기반으로 commit 메시지 초안:

```
Phase {NN} — {phase-name}: {1줄 요약}

작업 봉투 박제:
- WORK-ID: {핀에서 가져옴}
- 검증: {봉투의 검증 항목 줄임}
- 학습: {봉투의 학습 포인트 줄임}

🎯 무엇: {5단계 보고에서}
🛠️ 어떻게: {5단계 보고에서}
🧪 검증: {5단계 보고에서}
```

**3-B. 사용자에게 미리보기 + 승인**

```
다음 commit 메시지로 박을게요:

[메시지 미리보기]

수정/추가하고 싶은 부분 있나요? 없으면 "OK"로 박을게요.
```

응답 받고 수정하거나 OK면 진행.

**3-C. commit 실행**

```bash
git add <변경 파일들>
git commit -m "<메시지>"
```

⚠️ 학부생 백지 팀원 안내:
```
commit 박혔어요. 다음으로 GitHub에 push하고 PR 만들 거예요.
PR이 뭔지 모르면 알려주세요 — 안내할게요.
```

### 4. Push + PR 생성

**4-A. 현재 브랜치 확인**

```bash
git rev-parse --abbrev-ref HEAD
```

- main 브랜치면 → **STOP**:
  ```
  ⚠️ main 브랜치에서 직접 작업하셨네요.

  본인 협업 룰: 작업은 항상 feature/{slug}-{작업명} 브랜치에서 → PR로 머지.
  main 브랜치는 보호돼 있어서 직접 push 안 됩니다.

  해결:
  1. 새 브랜치 만들기: git checkout -b feature/{slug}-{phase-name}
  2. 그 브랜치로 commit 자동 이동됨
  3. push + PR 진행

  본인이 직접 처리하거나, 진행 도와드릴까요?
  ```
- feature/* 브랜치면 → 다음으로

**4-B. push 실행**

```bash
git push -u origin <현재 브랜치>
```

push 결과에서 GitHub PR 생성 URL이 출력되면 사용자에게 알림:
```
push 박혔어요. 다음 URL로 PR 만들면 됩니다:
{URL}

또는 GitHub repo 페이지 → "Pull requests" → "New pull request"에서 만들 수도 있어요.
```

**4-C. PR 생성 안내** (학부생 백지 팀원 대응)

사용자에게 PR 제목/본문 초안 제시:

```
PR 제목/본문 초안:

제목: Phase {NN} — {phase-name}: {1줄 요약}

본문:
## Summary
{Phase 목표 한 줄}

## 변경 사항
- {파일 변경 요약}

## 검증
- [{x}] AC 1: {Phase 완료 조건 1}
- [{x}] AC 2: {Phase 완료 조건 2}
...

## 관련 ADR
- ADR-{NNN}: {관련 결정}

## 학습 포인트
{-DONE.md의 학습 일지 후보 키워드}

위 내용 GitHub PR 만들 때 복사해서 쓰면 됩니다.
PR 만들고 URL 알려주세요. 다음 단계로 갈게요.
```

사용자가 PR URL 답하면 변수 `pr_url`에 박음.

### 5. 노션 박제 트리거

```
PR 박혔어요. 이제 노션 박제 진행할게요.
/session:log를 자동으로 호출합니다 — 본인이 따로 호출 안 해도 됩니다.

(노션 박제는 본인 컴퓨터에 Codex 있으면 Codex가 박고, 없으면 Claude가 직접 박아요.
어느 쪽인지 /session:log 안에서 자동으로 분기합니다.)
```

`.claude/commands/session/log.md` 읽고 그 안내대로 진행. PR URL을 인자로 넘김.

### 6. CHANGELOG 갱신 검토 (해당 시만)

본 Phase에서 헌법/ADR/하네스/공유 파일 변경이 있었다면 `.claude/CHANGELOG.md` 갱신 필요. 사용자에게 한 줄 확인:

```
이번 Phase에서 헌법/ADR/하네스/공유 파일 변경 있었어요?
(예: CLAUDE.md, 00_Document/ADR/, .claude/, .vscode/settings.json 등)

- 있으면 → CHANGELOG.md에 한 줄 추가하고 commit
- 없으면 → 스킵

답해주세요.
```

답이 "있어요"면 CHANGELOG.md 갱신 안내. 없으면 다음으로.

### 7. 다음 액션 분기

2-A에서 받은 `next_action`에 따라:

**`next_action == "continue"`**:
```
다음 Phase 진입할게요. /work:start {다음 Phase 번호 또는 이름} 호출하면 됩니다.

또는 현재 마일스톤 다음 Phase로 자동 진입 원하면 알려주세요.
```

**`next_action == "stop"`**:
```
오늘 작업 마감. 수고하셨어요.

다음 세션에서 이어갈 때:
1. /session:start 호출 (CONTEXT 인지 + CHANGELOG 최근 변경 확인)
2. 핀(.claude/state/current-pin.txt)이 현재 작업 좌표 알려줄 거예요.

5단계 보고는 이미 -DONE.md에 박혔으니 별도 박을 거 없어요.
```

**`next_action == "journal"`**:
```
학습 일지 작성하시려는군요. /journal:phase 호출하면 Phase 통째 회고 흐름이 시작돼요.

학습 일지 마감 후 다음 Phase 진입 또는 종료 결정하면 됩니다.
```

### 8. 마감 보고

```
─────────────────────────────────────────
🎯 Phase 마감 완료
─────────────────────────────────────────

📍 Phase: {NN} — {phase-name}
📝 commit: {commit hash 짧은 형식}
🔗 PR: {pr_url}
📚 노션: {Notion 페이지 URL 또는 "/session:log 실행됨"}
{CHANGELOG 갱신 있었으면: 📋 CHANGELOG.md 갱신됨}

➡️ 다음: {next_action에 맞는 안내}
```

---

## 중요 원칙

- **학부생 백지 팀원 가정**. PR 처음 만드는 사람일 수 있음. 매 단계 명확한 안내.
- **막히면 STOP**. 그 자리에서 도움 요청 받고 다음 안내. 무리한 추측 안 함.
- **본인 헌법 정합**: 5단계 보고는 이미 -DONE.md에 박혀있으니 본 커맨드 끝에 별도 박지 않음. 마감 보고만 박음.
- **노션 박제는 위임**: 본 커맨드는 트리거만. 박제 자체는 /session:log가 책임. DRY 원칙.

## 변수 박힘 (단계 간 전달)

- `phase_info` — Phase NN + 이름 + 1줄 요약 (1-A에서 추출)
- `next_action` — continue / stop / journal (2-A에서 결정)
- `commit_hash` — 3-C 결과
- `pr_url` — 4-C에서 사용자가 답함
- `changelog_updated` — 6에서 갱신 여부
