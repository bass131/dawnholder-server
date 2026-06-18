---
description: Phase 완료 마감 절차 — commit + PR + (선택)노션 박제 + work-pin 갱신 + 다음 액션 결정.
---

사용자가 Phase 완료 마감을 요청. 본인 헌법 "Phase 완료 시 두 액션 권유"의 두 번째 액션.

---

### 이 커맨드의 역할

`-DONE.md` 박제 직후 호출 → commit + PR + (선택)노션 박제 + work-pin 갱신 + 다음 액션까지 한 흐름. 학부생 백지 팀원이 PR 누락하는 부담 자동화. (ADR-025로 옛 CONTEXT.md 자동 갱신 단계 은퇴 — work-pin이 단일 핸드오프.)

> **루프 마감 경로 (loop-driven, M7.5)**: 본 커맨드 = 작업 세션([`/session:start`](start.md))의 마감 축. 마감 시 `pending-*` 원장(art/comprehension/knowledge, P05 신설) 잔여 항목을 점검 — 깊은 학습 미뤄둔 것은 `pending-comprehension`에 적재(추후 [`/session:review`](review.md) pull). PR 생성/머지는 **버킷 (c) 영호 GO 게이트 보존** ([`pr-and-merge-gate.md`](../../../00_Document/policies/pr-and-merge-gate.md)).

---

### 1. 사전 검증

#### 1-A. `-DONE.md` 박제 존재 확인 (등급별 분기)

작업 등급에 따라 -DONE.md 필요 여부 다름 ([`../../policies/grade-and-risk.md`](../../policies/grade-and-risk.md)):

| 등급 | -DONE.md | 5단계 보고 |
|---|---|---|
| 단순 | ❌ | ❌ |
| 보통 | ❌ | ❌ |
| 복잡 | ✅ | ❌ |
| 대규모 | ✅ | ✅ MD + HTML |

**등급 = 복잡/대규모인 경우만** -DONE.md 강제:

```bash
git status --porcelain | grep -E '\-DONE\.md$'
```

- staged/untracked로 잡힘 → 정상
- 없음 → **STOP** (등급 복잡/대규모인 경우만):
  ```
  ⚠️ commit 대기 중인 -DONE.md 없음 — 등급 <복잡/대규모> = -DONE.md 박제 의무.
  /session:end 호출 전 -DONE.md Write → phase-gate-validator.sh 통과 필요.
  단순/보통 ad-hoc 마감이면 commit message만으로 충분 (이 검증 스킵).
  ```

#### 1-B. 단순/보통 마감 (-DONE.md X)

work-pin 갱신 + commit message만으로 마감. 본 절차 2~7단계 진행 (5단계 보고만 스킵).

---

### 2. 사용자 컨텍스트 수집

#### 2-A. 다음 액션 방향

```
Phase 마감 진행. 마감 후 다음 액션:
  1. 계속 — 바로 다음 Phase 진입
  2. 종료 — 오늘 작업 끝, 다음 세션에서 이어감
```

응답 받아 변수 `next_action`에 박음 (continue / stop).

---

### 3. Commit 진행

#### 3-A. commit 메시지 초안 (등급별 분기)

**복잡/대규모** (5단계 보고 또는 -DONE.md 기반):
```
Phase {NN} — {phase-name}: {1줄 요약}

work-pin 박제:
- WORK-ID: {핀에서 가져옴}
- 등급: {grade}
- 검증: {-DONE.md ✅ 조건 줄임}
- 학습: {-DONE.md 학습 키워드 줄임}

🎯 무엇: {5단계 보고에서, 대규모만}
🛠️ 어떻게: {5단계 보고에서, 대규모만}
🧪 검증: {5단계 보고에서, 대규모만}
```

**단순/보통**:
```
<도메인>: <한 줄 요약>

- 변경: <간단 요약>
- 검증: <build/test 결과 1줄>
```

#### 3-B. 사용자 미리보기 + 승인

"다음 commit 메시지로 박을게요: [메시지] / 수정 있으면 알려주세요. 없으면 OK."

#### 3-C. commit 실행

```bash
git add <변경 파일들>
git commit -m "<메시지>"
```

학부생 백지 팀원: "commit 박혔어요. 다음 push + PR. PR 처음이면 가이드 4번 섹션(`00_Document/team-guide.html`) 참조."

---

### 4. Push + PR 생성 (irreversible 깃발 — 사용자 명시 GO 게이트)

> **헌법 정합**: `gh pr create/merge` = irreversible 깃발 ([`../../policies/pr-and-merge-gate.md`](../../policies/pr-and-merge-gate.md)). AI 자율 진행 X. 본 절차 모든 단계에서 *사용자 명시 GO*.

#### 4-A. 현재 브랜치 확인

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

#### 4-B. push 실행

```bash
git push -u origin <현재 브랜치>
```

PR URL 사용자에게 알림.

#### 4-C. PR 제목/본문 초안

```
제목: Phase {NN} — {phase-name}: {1줄 요약}

본문:
  ## Summary — {Phase 목표 한 줄}
  ## 변경 사항 — {파일 변경 요약}
  ## 검증 — [x] AC 체크박스 (Phase 완료 조건)
  ## 관련 ADR — ADR-{NNN}
  ## 학습 포인트 — {-DONE.md 학습 일지 후보 키워드}
```

**PR body 안전 표현 (자동 검증)**:
- ❌ 보안 키워드 literal 박지 않기: `gh pr merge --admin`, `git push --force`, `rm -rf` 등
- ✅ 풀어쓰기: "관리자 우회 머지", "강제 push", "재귀 삭제"
- 사유: Auto Mode classifier가 *bypass 정상화*로 분류 거절 + 학습 자산 모방 위험

#### 4-D. PR 생성 게이트 (AskUserQuestion)

AI가 `gh pr create` 호출 *직전*에 명시 GO:

```
🚨 PR 생성 = irreversible 깃발
   브랜치: <현재 브랜치> → main
   제목: <위 초안>
   본문 요약: <첫 3줄>
   
   진행 OK?
     1. 진행 (AI가 gh pr create)
     2. 사용자 직접 생성 (GitHub Web/외부 셸 — body 복붙)
     3. 본문 수정 후 재확인
     4. 중단
```

응답 = 1 → AI 진행. 2 → 링크(`https://github.com/<repo>/pull/new/<branch>`) + body 텍스트 출력 후 사용자 직접. 3 → 본문 수정 루프.

#### 4-E. CODEOWNERS 통과 점검 + admin bypass 예외 (있을 때만)

PR 생성 후 GitHub state 확인:

```bash
gh pr view <num> --json mergeStateStatus,reviewDecision,reviewRequests
```

- `mergeStateStatus = MERGEABLE` + 리뷰 충족 → 정상 머지 가능
- `mergeStateStatus = BLOCKED` + CODEOWNERS 매칭 → **admin bypass 사유 평가 게이트**:

```
🚨 PR 머지 차단 = CODEOWNERS 거절
   거절자: <팀원 이름 또는 @placeholder>
   매칭 파일: <경로> (예: 03_Client/Assets/Plugins/Shared/Shared.dll)
   사유 평가:
     - 단독 통제 영역? (M3.5 약속 .claude/, 00_Document/, 01_Phases/youngho/)
     - 자동 빌드 산출물? (Shared.dll = 98_Shared/ 빌드 부산물)
     - 시급한 봉합? (안전망 무력화 / prod 사고)
     - 본인 변경 영향 X?
   
   진행 옵션:
     1. 정상 경로 (팀원 ack 대기)
     2. admin bypass (사유 PR body + work-pin 박음 후 진행)
     3. PR 본문 수정 (사유 명시)
     4. 중단
```

응답 = 2 admin bypass → AI가 `gh pr merge --admin` 호출 + 사유를 PR body 추가 commit 또는 PR comment로 박음 + work-pin 한 줄.

#### 4-F. 머지 게이트 (AskUserQuestion)

AI가 `gh pr merge` 호출 *직전*에 명시 GO (CODEOWNERS 통과한 정상 케이스도 게이트):

```
🚨 PR 머지 = irreversible (main history 변경)
   PR: #<번호>
   방식: <merge/squash/rebase>
   머지 후: work-pin 갱신 (§7.5)
   
   진행 OK?
     1. 진행
     2. 방식 변경
     3. 중단
```

---

### 5. 노션 박제 트리거 (선택)

"PR 박혔어요. 협업 기록 남기려면 /session:log."

`.claude/commands/session/log.md` 안내대로 진행 (선택). PR URL 인자로 넘김.

**ADR-025 정합**: 노션 박제는 *선택* (프로젝트 협업 기록). knowledge 캐시(트랙 A) 박을지는 별도 사용자 확인 게이트. (본인 회고 트랙 B는 은퇴.)

---

### 6. CHANGELOG 갱신 검토 (해당 시만)

본 Phase에서 헌법/ADR/하네스/공유 파일 변경 있었나 한 줄 확인:

```
이번 Phase에서 헌법/ADR/하네스/공유 파일 변경 있었어요?
(CLAUDE.md, 00_Document/ADR/, .claude/, .vscode/settings.json 등)
- 있으면 → CHANGELOG.md에 한 줄 추가하고 commit
- 없으면 → 스킵
```

---

### 7. 다음 액션 분기

2-A 받은 `next_action`에 따라 짧게 안내:

- **continue** → 다음 Phase 진입
- **stop** → 오늘 마감. 다음 세션에서 `/session:start` → work-pin이 좌표 알려줌

---

### 7.5. work-pin 최신 확인 (ADR-025 — CONTEXT 동기 은퇴)

work-pin(`.claude/state/current-pin.txt`)이 방금 마감한 상태(완료 Phase + 다음 액션)를 반영하는지 확인. 작업 중 갱신했으면 이미 최신. 안 했으면 "현재 작업 / 다음 액션"만 갱신 (자동 X — 본인 결정 또는 명시 위임 시 AI).

**핵심**: work-pin이 *유일한* 세션 간 핸드오프 표면이므로, 마감 시점에 실제 상태를 반영해야 다음 `/session:start` drift 게이트가 통과. 단 핀 비대 주의 — 마감 commit 이력은 핀이 아니라 CHANGELOG/`-DONE.md`로.

(옛 CONTEXT.md 단방향 동기 + CONTEXT_History 한 줄 + 응축 알림은 ADR-025로 은퇴.)

---

### 8. 마감 보고

```
─────────────────────────────────────────
🎯 Phase 마감 완료
─────────────────────────────────────────

📍 Phase: {NN} — {phase-name}
🏷️ 등급: {grade}
📝 commit: {commit hash 짧은 형식}
🔗 PR: {pr_url}
📚 노션: {Notion 페이지 URL 또는 "/session:log 실행됨"}
📋 work-pin: {✅ 최신}
{CHANGELOG 갱신 있었으면: 📋 CHANGELOG.md 갱신됨}

➡️ 다음: {next_action에 맞는 안내}
```

---

### 중요 원칙

- **학부생 백지 팀원 가정** — 핵심만 명확히, 상세 안내는 `00_Document/team-guide.html` 위임
- **막히면 STOP** — 그 자리에서 도움 요청, 무리한 추측 X
- **헌법 정합**: 5단계 보고는 대규모 -DONE.md에 박혀있으니 본 커맨드 끝에 별도 X
- **위임 패턴**: 노션 박제는 `/session:log` (선택). 본 커맨드는 *오케스트레이터*
- **work-pin이 단일 핸드오프** (ADR-025) — 마감 시점에 실제 상태 반영. 옛 CONTEXT 동기는 은퇴

---

### M3.5 새 하네스 변경 (옛 대비)

- **등급별 -DONE.md 의무**: 옛 "Phase 완료 시 항상 -DONE.md" → 새 "복잡/대규모만 -DONE.md, 단순/보통은 commit message로 충분"
- **5단계 보고 = 대규모만**: 옛 "Phase 완료 시 항상 5단계 보고" → 새 "대규모 등급만 5단계 보고 MD/HTML 이중 박음"
- **`/journal:phase` 슬래시 제거**: 옛 호출 → 학습 일지 트랙 B는 ADR-025로 은퇴 (knowledge 트랙 A만)
- **work-pin 압축**: 옛 ~80줄 → 새 30~40줄. 8단계 마감 보고도 짧게
- **7.5 단계 전환 (ADR-025)**: 옛 "CONTEXT.md 단방향 동기 게이트" → 새 "work-pin 최신 확인" (CONTEXT 3종 은퇴, work-pin 단일 핸드오프)
