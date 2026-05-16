---
description: Phase 완료 마감 절차 — commit + PR + 노션 박제 + CONTEXT 자동 갱신 + 다음 액션 결정까지 한 흐름. /session:log 자동 호출.
---

사용자가 Phase 완료 마감을 요청했습니다. 본인 헌법의 "Phase 완료 시 두 액션 권유" 중 두 번째 액션을 실행합니다.

## 이 커맨드의 역할

`-DONE.md` 박제 직후 호출되어 commit + PR + 노션 박제 + CONTEXT.md 갱신 + 다음 액션 결정까지 한 흐름으로 진행. 학부생 백지 팀원이 PR/노션 누락하거나 본인이 매번 "CONTEXT 갱신해줘" 손으로 부탁하는 부담을 한 번에 자동화.

## 진행 절차

### 1. 사전 검증

**1-A. `-DONE.md` 박제 존재 확인**

```bash
git status --porcelain | grep -E '\-DONE\.md$'
```

- staged/untracked로 잡힘 → 정상, 다음으로
- 없음 → **STOP**:
  ```
  ⚠️ commit 대기 중인 -DONE.md 없음 — Phase 완료 = -DONE.md 박제 선행.
  /session:end 호출 전 -DONE.md Write → validate-phase-gate.sh 통과 절차 필요.
  단순 ad-hoc 마감이면 /session:log 직접 호출.
  ```

**1-B. 작업 봉투 검증**

본인 헌법: 코드 작업 봉투는 매 응답에 강제. -DONE.md 박을 때 봉투 박혀있어야 정상. 봉투 누락 의심되면 사용자에게 한 줄 확인.

### 2. 사용자 컨텍스트 수집

**2-A. 다음 액션 방향**

```
Phase 마감 진행할게요. 마감 후 다음 액션:
  1. 계속 — 바로 다음 Phase 진입
  2. 종료 — 오늘 작업 끝, 다음 세션에서 이어감
  3. 학습 일지 먼저 — /journal:phase로 회고 작성
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

**3-B. 사용자 미리보기 + 승인**

"다음 commit 메시지로 박을게요: [메시지] / 수정 있으면 알려주세요. 없으면 OK."

**3-C. commit 실행**

```bash
git add <변경 파일들>
git commit -m "<메시지>"
```

학부생 백지 팀원: "commit 박혔어요. 다음 push + PR. PR 처음이면 가이드 4번 섹션(`00_Document/team-guide.html`) 참조."

### 4. Push + PR 생성

**4-A. 현재 브랜치 확인**

```bash
git rev-parse --abbrev-ref HEAD
```

- main이면 → **STOP**:
  ```
  ⚠️ main 직접 작업. 협업 룰: feature/{slug}-{작업명} → PR.
  해결: `git checkout -b feature/{slug}-{phase-name}` → commit 자동 이동 → push + PR.
  본인이 처리할 거예요?
  ```
- feature/* → 다음으로

**4-B. push 실행**

```bash
git push -u origin <현재 브랜치>
```

출력의 PR URL 사용자에게 알림. 또는 GitHub repo → Pull requests → New.

**4-C. PR 제목/본문 초안**

```
제목: Phase {NN} — {phase-name}: {1줄 요약}

본문:
  ## Summary — {Phase 목표 한 줄}
  ## 변경 사항 — {파일 변경 요약}
  ## 검증 — [x] AC 체크박스 (Phase 완료 조건)
  ## 관련 ADR — ADR-{NNN}
  ## 학습 포인트 — {-DONE.md 학습 일지 후보 키워드}
```

GitHub PR 만들 때 위 복사. (학부생 백지면 가이드 4번 섹션 참조.) PR URL 알려주면 변수 `pr_url`에 박음.

### 5. 노션 박제 트리거

"PR 박혔어요. /session:log 자동 호출 (Codex 있으면 Codex, 없으면 Claude 단독 분기)."

`.claude/commands/session/log.md` 읽고 안내대로 진행. PR URL 인자로 넘김.

### 6. CHANGELOG 갱신 검토 (해당 시만)

본 Phase에서 헌법/ADR/하네스/공유 파일 변경 있었나 사용자에게 한 줄 확인:

```
이번 Phase에서 헌법/ADR/하네스/공유 파일 변경 있었어요?
(CLAUDE.md, 00_Document/ADR/, .claude/, .vscode/settings.json 등)
- 있으면 → CHANGELOG.md에 한 줄 추가하고 commit
- 없으면 → 스킵
```

### 7. 다음 액션 분기

2-A에서 받은 `next_action`에 따라 짧게 안내:

- **continue** → 다음 Phase 진입. `/work:start {Phase 번호}` 호출.
- **stop** → 오늘 마감. 다음 세션에서 `/session:start` → 핀이 현재 작업 좌표 알려줌. 5단계 보고는 -DONE.md에 박혔으니 별도 없음.
- **journal** → `/journal:phase` 호출하면 Phase 통째 회고 흐름.

### 7.5. CONTEXT.md 자동 갱신 (본인 부담 줄이기)

세션 마감 시점에 본인 핸드오프 노트(CONTEXT.md)도 같이 갱신해서 다음 세션 시작 시 컨텍스트 손실 0. 매 세션 마감마다 본인이 "CONTEXT 갱신해줘" 손으로 부탁하는 부담 자동화.

**자동 갱신 범위** (작은 갱신만):
- "⏸️ 현재 멈춤 지점" 한 문단 — 본 Phase 결과 + commit/PR/노션 URL + 다음 진입 지점
- "학습 일지 후보" — -DONE.md 학습 키워드 기반 1~2줄 추가 (별 ★ 평가 포함)
- `CONTEXT_History.md` — 갱신 이력 한 줄 추가

**자동 갱신 X** (사용자 의도 보호):
- CONTEXT.md 다른 섹션 (하드 일정/팀 구조/보류 중/핵심 결정 요약)
- 응축 (~200줄 임계 넘어도 큰 마일스톤 끝날 때만 사용자 결정으로 재작성)

**7.5-A. 본인 자산 확인** (협업 셋업 안전망)

```bash
git check-ignore CONTEXT.md CONTEXT_History.md
```

- 둘 다 ignored → 정상 (본인 머신만 갱신), 다음으로
- 한 쪽이라도 not ignored → STOP + 사용자에게 알림 (협업 셋업 어긋남 — 본인 자산은 각자 보유)

**7.5-B. 갱신 초안 작성**

세 곳에 박을 변경 내용 준비:
- **(A) CONTEXT.md "⏸️ 현재 멈춤 지점"** — 본 Phase 결과 한 문단 (commit hash + PR URL + 노션 URL + 다음 진입 지점). 옛 문단은 통째 교체 (누적 X — 응축본 원칙).
- **(B) CONTEXT.md "학습 일지 후보"** — -DONE.md/봉투의 학습 키워드 기반 1~2줄 추가. 별 ★ 평가 (★★★ 면접 결정타 / ★★ 가치 큼 / ★ 보완 자료) 박음.
- **(C) CONTEXT_History.md** — `| YYYY-MM-DD (Phase NN 완료) | {1줄 요약} |` 한 줄 추가.

**7.5-C. 미리보기 + 컨펌**

```
CONTEXT.md 자동 갱신 미리보기:

【⏸️ 현재 멈춤 지점】(갱신)
[새 문단]

【학습 일지 후보】(추가)
- [새 한 줄]

【CONTEXT_History.md】(이력 한 줄 추가)
| YYYY-MM-DD ... |

OK / 수정 / 스킵 — 알려주세요.
```

**7.5-D. 응축 임계 알림** (250줄+ 시만)

`wc -l CONTEXT.md`가 250줄 넘으면 미리보기 끝에 알림 한 줄:
```
⚠️ CONTEXT.md 현재 {N}줄. 다음 큰 마일스톤 끝날 때 처음부터 재작성 고려.
```

자동 응축 X — 사용자 결정 영역.

**7.5-E. 실행**

사용자 응답:
- "OK" → CONTEXT.md + CONTEXT_History.md Edit 실행
- "스킵" → 건너뛰고 8단계로
- 수정 요청 → 반영 후 다시 미리보기

### 8. 마감 보고

```
─────────────────────────────────────────
🎯 Phase 마감 완료
─────────────────────────────────────────

📍 Phase: {NN} — {phase-name}
📝 commit: {commit hash 짧은 형식}
🔗 PR: {pr_url}
📚 노션: {Notion 페이지 URL 또는 "/session:log 실행됨"}
📋 CONTEXT 갱신: {✅ / ⏭️ 스킵}
{CHANGELOG 갱신 있었으면: 📋 CHANGELOG.md 갱신됨}

➡️ 다음: {next_action에 맞는 안내}
```

---

## 중요 원칙

- **학부생 백지 팀원 가정**. 핵심만 명확히 — 상세 안내는 `00_Document/team-guide.html`에 위임 (본 커맨드 응축 정합).
- **막히면 STOP**. 그 자리에서 도움 요청 받고 다음 안내. 무리한 추측 안 함.
- **헌법 정합**: 5단계 보고는 -DONE.md에 박혀있으니 본 커맨드 끝에 별도 박지 않음. 마감 보고만 박음.
- **위임 패턴**: 노션 박제는 `/session:log`, 학습 일지는 `/journal:phase` — 본 커맨드는 *오케스트레이터*.
- **자동 갱신은 작은 갱신만**: CONTEXT 다른 섹션·응축은 사용자 결정 영역. 의도 보호.

## ad-hoc 케이스 (본 커맨드 호출 안 함)

Phase 완료 아닌 ad-hoc 종료(Unity 동기화·하네스 응축 등)는 본 커맨드가 안 잡힘. 같은 자동화 필요하면 `/session:log`에 비슷한 7.5 단계 추가 권유 — 본 변경 효과 본 후 실측 기반 결정.

## 변수 박힘 (단계 간 전달)

- `phase_info` — Phase NN + 이름 + 1줄 요약 (1-A에서 추출)
- `next_action` — continue / stop / journal (2-A에서 결정)
- `commit_hash` — 3-C 결과
- `pr_url` — 4-C에서 사용자가 답함
- `changelog_updated` — 6에서 갱신 여부
- `context_updated` — 7.5에서 갱신 여부 (✅ / ⏭️)
