---
description: Phase 완료 마감 절차 — commit + PR + 노션 박제 + CONTEXT 자동 갱신 + 다음 액션 결정. /session:log 자동 호출.
---

사용자가 Phase 완료 마감을 요청. 본인 헌법 "Phase 완료 시 두 액션 권유"의 두 번째 액션.

---

### 이 커맨드의 역할

`-DONE.md` 박제 직후 호출 → commit + PR + 노션 박제 + CONTEXT.md 갱신 + 다음 액션까지 한 흐름. 학부생 백지 팀원이 PR/노션 누락하거나 매번 "CONTEXT 갱신해줘" 손으로 부탁하는 부담 자동화.

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
  3. 학습 일지 먼저 — 본인 노션 또는 잔존 learning-journal/ (트랙 B)
```

응답 받아 변수 `next_action`에 박음 (continue / stop / journal).

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
   머지 후 자동: work-pin/CONTEXT 동기 (§7.5)
   
   진행 OK?
     1. 진행
     2. 방식 변경
     3. 중단
```

---

### 5. 노션 박제 트리거 (트랙 A/B 분리 정합)

"PR 박혔어요. /session:log 자동 호출."

`.claude/commands/session/log.md` 안내대로 진행. PR URL 인자로 넘김.

**M3.5 정합**: 노션 박제 = *트랙 B* (본인 회고). 트랙 A (knowledge 캐시 박을지)는 별도 사용자 확인 게이트.

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
- **journal** → 본인 노션 트랙 B 박음 또는 잔존 `learning-journal/{본인}/` (M3.5 옛 `/journal:phase` 슬래시 제거됨, 본 절차에서 직접 안내)

---

### 7.5. CONTEXT.md 동기화 (work-pin ↔ CONTEXT 정합 게이트)

`/session:end` = 작업 완료 시점 = work-pin은 이미 최신 = **CONTEXT 단방향 동기 트리거**.

본 단계는 *양식 부담 줄이기* 자동화가 아니라 ***work-pin ↔ CONTEXT 정합 보장 게이트***입니다 (옵션 C, [`../../policies/pin-and-done.md`](../../policies/pin-and-done.md) §5). 두 곳 어긋나면 다음 세션 `/session:start` 시 AI가 옛 멈춤 지점 읽음 → 옛 결정 기반 작업 위험 (Claude 혼선).

**동기 룰** (등급 무관 — 정합이 양식 부담보다 우선):

| 항목 | 동기 분기 |
|---|---|
| **⏸️ 현재 멈춤 지점** | ✅ **매 마감 자동** (등급 무관, work-pin 압축본 → CONTEXT 풀어쓴 한 문단으로 박음) |
| **학습 일지 후보** | ✅ 콘텐츠 유무 분기 — -DONE.md / work-pin에 학습 키워드 *실제 있으면* 추가 (★ 평가 포함). 키워드 없으면 스킵 |
| **CONTEXT_History.md** | ✅ 매 마감 한 줄 자동 (비용 0) |
| **응축 평가** | ⚠️ 250줄+ 알림만, 자동 응축 X — 큰 마일스톤 끝 사용자 명시 결정 |
| **다른 섹션** (하드 일정/팀 구조/보류 중/핵심 결정) | ❌ 자동 갱신 X (사용자 의도 보호) |

**핵심 정신**: 옛 운영은 "단순/보통 등급 = CONTEXT 갱신 스킵" 분기 고려했으나, work-pin과 정합 깨지면 Claude 혼선 비용 > 양식 부담 ↓. 따라서 *등급별 분기*는 *콘텐츠 깊이*만 (학습 후보 유무 등). "현재 멈춤 지점" 자체는 *등급 무관 항상 동기*.

#### 7.5-A. 본인 자산 확인

```bash
git check-ignore CONTEXT.md CONTEXT_History.md
```

- 둘 다 ignored → 정상 (본인 머신만 갱신)
- 한 쪽이라도 not ignored → STOP + 알림 (협업 셋업 어긋남)

#### 7.5-B. 동기 초안 + 미리보기 + 컨펌

work-pin 최신 내용 읽어서 CONTEXT 동기 초안 박음:

```
CONTEXT.md 동기 미리보기 (work-pin → CONTEXT):

【⏸️ 현재 멈춤 지점】(work-pin 정합 동기, 항상)
[work-pin 압축본 → CONTEXT 풀어쓴 한 문단]

【학습 일지 후보】(키워드 있을 때만 추가)
- [새 한 줄, ★ 평가 포함]
  또는 "이번 마감 학습 키워드 없음 → 스킵"

【CONTEXT_History.md】(이력 한 줄 자동)
| YYYY-MM-DD (Phase NN 완료, 등급 X) | 1줄 요약 |

OK (기본) / 수정 / 스킵 — 알려주세요.
```

**기본 OK 디폴트**: 사용자가 명시적으로 "스킵" 또는 "수정" 말하지 않으면 OK로 진행. 양식 부담 ↓ + 정합 보장.

#### 7.5-C. 응축 임계 알림 (250줄+ 시만)

`wc -l CONTEXT.md`가 250줄 넘으면 미리보기 끝에 알림:
```
⚠️ CONTEXT.md 현재 {N}줄. 다음 큰 마일스톤 끝날 때 처음부터 재작성 고려.
```

자동 응축 X — 사용자 결정 영역.

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
📋 CONTEXT 갱신: {✅ / ⏭️ 스킵}
{CHANGELOG 갱신 있었으면: 📋 CHANGELOG.md 갱신됨}

➡️ 다음: {next_action에 맞는 안내}
```

---

### 중요 원칙

- **학부생 백지 팀원 가정** — 핵심만 명확히, 상세 안내는 `00_Document/team-guide.html` 위임
- **막히면 STOP** — 그 자리에서 도움 요청, 무리한 추측 X
- **헌법 정합**: 5단계 보고는 대규모 -DONE.md에 박혀있으니 본 커맨드 끝에 별도 X
- **위임 패턴**: 노션 박제는 `/session:log`, 학습 일지는 본인 노션 (트랙 B). 본 커맨드는 *오케스트레이터*
- **CONTEXT 동기는 정합 게이트** — 옛 운영 "양식 부담 줄이기 자동화"가 아니라 *work-pin ↔ CONTEXT 정합 보장*. "현재 멈춤 지점" 등급 무관 항상 동기 ([`../../policies/pin-and-done.md`](../../policies/pin-and-done.md) §5)
- **다른 섹션·응축은 사용자 결정 영역** — 자동 갱신 X (의도 보호)

---

### M3.5 새 하네스 변경 (옛 대비)

- **등급별 -DONE.md 의무**: 옛 "Phase 완료 시 항상 -DONE.md" → 새 "복잡/대규모만 -DONE.md, 단순/보통은 commit message로 충분"
- **5단계 보고 = 대규모만**: 옛 "Phase 완료 시 항상 5단계 보고" → 새 "대규모 등급만 5단계 보고 MD/HTML 이중 박음"
- **`/journal:phase` 슬래시 제거**: 옛 호출 → 새 본 커맨드 7단계 분기에서 *본인 노션 직접 안내* (트랙 B)
- **work-pin 압축**: 옛 ~80줄 → 새 30~40줄. 8단계 마감 보고도 짧게
- **7.5 단계 정신 전환**: 옛 "CONTEXT.md 자동 갱신 = 양식 부담 줄이기" → 새 "**work-pin ↔ CONTEXT 정합 게이트**" (옵션 C). 등급 무관 "현재 멈춤 지점" 항상 동기 / 학습 후보는 콘텐츠 유무 분기 / 기본 OK 디폴트로 양식 부담 ↓
