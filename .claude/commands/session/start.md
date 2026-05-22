---
description: 새 세션 시작 — git 안전 점검 + CONTEXT.md 읽고 톤·현재 멈춤 지점·다음 액션 + CHANGELOG 최근 변경 짧게 확인
---

사용자가 새 세션을 시작했습니다. 매번 "CONTEXT 읽고 시작해줘"를 문장으로 치는 부담을 없애기 위한 커맨드.

**중요**: 이 커맨드는 `git pull`보다 **먼저** 호출돼야 합니다. 0단계에서 git 상태를 게이트로 점검하고, 안전 확인 후에만 사용자에게 `git pull` 안내. 작업물 유실 방지 (2026-05-15 보강).

---

### 0. Git 안전 점검 (게이트) — 가장 먼저

**왜**: "git pull → /session:start" 순서면 어제 작업 브랜치 안 돌아가거나 commit 안 한 변경이 있는 상태에서 main pull 시 충돌 → 학부생 패닉 → `git reset --hard` 잘못 치면 작업물 증발. 게이트가 그 위험을 사전에 잡음.

**실행**:

```bash
git status --porcelain=v1 --branch
```

판정 — 셋 중 하나:

#### (A) 클리어 상태: feature 브랜치 + uncommitted 변경 없음

→ 다음 응답으로 짧게 알리고 1단계 진행:

```
Git 상태 클리어 — feature 브랜치 + 워킹 디렉토리 깨끗.
지금 아침 첫 호출이면 `git pull origin main`으로 main 최신 받으세요.
(이미 받았으면 그대로 진행)
```

#### (B) main 브랜치에 있음 (uncommitted 변경 유무 무관)

→ **CONTEXT 읽기 진입 금지**. STOP:

```
⚠️ STOP — main 브랜치에 있어요.
💡 처음 보는 STOP이면: .claude/CHANGELOG.md 최상단 또는 가이드 3번 섹션 "갑자기 STOP 떴어요?" 박스 한 번 보세요.

지금 git pull 치면 위험할 수 있어요. 다음 중 하나:

  1) 어제 작업하던 브랜치가 있으면 → git checkout feature/{그-브랜치}
  2) 새 작업이면 → git pull origin main 먼저 → git checkout -b feature/{slug}-{작업명}

해결 후 /session:start 다시 호출해주세요.
```

#### (C) feature 브랜치 + uncommitted 변경 있음

`ProjectSettings.asset` 변경 여부 추가 점검 후 세 갈래 분기 (각자 Cloud 정책 "B+", 2026-05-15):

```bash
# 1) ProjectSettings.asset 변경 있나
git status --porcelain 03_Client/ProjectSettings/ProjectSettings.asset

# 2) 변경이 cloud 라인만인지 확인
git diff -U0 03_Client/ProjectSettings/ProjectSettings.asset 2>/dev/null \
  | grep -E "^[+-][^+-]" \
  | grep -vE "^[+-][[:space:]]*(cloudProjectId|organizationId):"

# 3) ProjectSettings.asset 외 다른 파일 변경 있나
git status --porcelain | grep -v "03_Client/ProjectSettings/ProjectSettings.asset"
```

**(C-1)** `ProjectSettings.asset` 단독 + cloud 라인만 → 자동 정리 후 1단계 진행:
```bash
git checkout 03_Client/ProjectSettings/ProjectSettings.asset
```

**(C-2)** `ProjectSettings.asset` + cloud 외 변경 → STOP + 분리 옵션 안내 (`git checkout -p`)

**(C-3)** `ProjectSettings.asset` 변경 X + 다른 파일 변경 → STOP + commit/stash/discard 옵션 안내

**절대 금지**: 게이트에서 Claude가 `git reset --hard`, `git checkout .`, `git clean -fd` 같은 파괴적 명령 자동/요청 실행 X. 사용자가 명시적으로 "버려도 돼"라 해도 한 단계씩 안내만, 실행은 사용자가.

**예외 (B+ 정책)**: (C-1) 케이스만 cloud 라인 자동 정리 비파괴적 허용.

---

### 0-부수. work-pin drift 발견 게이트 — 0단계 (A) 통과 후만

**왜**: work-pin "현재 작업 / 다음 액션"이 실제 git/gh 진행 단계와 어긋난 채 박혀있을 수 있어요 (commit/push/PR 생성/PR 머지 진행 후 work-pin 갱신 누락). 옛 옵션 C 게이트(`/session:end` 단일 동기, ADR-022)는 *세션 마감 시점*만 잡고 *세션 도중 진행 단계*는 못 잡음 → 다음 세션 시작 시 stale 출발 위험. 본 게이트가 그 stale을 시작 시점에 발견 (ADR-023 박힘, 5번 누적 후 Rule of Three 통과).

**핵심 정신**: 본 게이트는 *발견*만, *갱신은 본인 수동* (헌법 정신 = pin-and-done.md §1 "갱신은 본인 수동" / Hook is for alert, not action). 자동 갱신 박지 않음.

**실행**:

```bash
git log -3 --oneline
gh pr list --state all --head $(git branch --show-current) --limit 3
git status -sb
```

**비교**: `.claude/state/current-pin.txt` "현재 작업" / "다음 액션" 줄 키워드 vs 실제 상태. **대략 매칭만** (work-pin은 자유 양식이라 정확 매칭 X — 본인 인지 게이트가 최종 판단):

| work-pin 키워드 | 실제 상태 → stale 판정 |
|---|---|
| "commit 박을 예정" / "commit 대기" / "본 commit 대기" | 최근 commit가 그 작업 commit이면 stale |
| "push 대기" / "origin 미푸시" | `git status -sb`가 origin과 sync (ahead 0)면 stale |
| "PR 생성 대기" / "PR 게이트 대기" | `gh pr list`에 본 브랜치 PR 박혀있으면 stale |
| "PR 머지 대기" | PR state == MERGED면 stale |
| "M{N} 진입 대기" + 옛 마일스톤 본문 | 본 브랜치명이 새 마일스톤 슬러그면 stale |

**판정 — 둘 중 하나**:

#### (정합) 차이 없음 → 무음 통과, 1단계 진행

#### (drift) 차이 있음 → STOP:

```
⚠️ STOP — work-pin이 실제 진행 단계와 어긋났어요 (drift 발견).
💡 처음 보는 STOP이면: ADR-023 또는 가이드 8번 섹션 "막혔을 때" 표 한 번 보세요.

work-pin "현재 작업/다음 액션" 박힌 단계:
  [키워드]

실제 git/gh 상태:
  - 최근 commit: [hash + 메시지]
  - PR 상태: [번호 + state]
  - 브랜치 sync: [ahead/behind 또는 sync]

다음 중 본인이 결정 (자동 갱신 X):
  1) work-pin 갱신 (.claude/state/current-pin.txt) — 실제 상태 반영
  2) CONTEXT.md "⏸️ 현재 멈춤 지점" 갱신 (다음 세션 동기)
  3) 둘 다 갱신 (보통 묶음)

해결 후 /session:start 다시 호출해주세요.
```

**절대 금지**: Claude가 work-pin/CONTEXT.md 자동 갱신 X. 사용자가 "그냥 너가 고쳐줘"라 해도 한 단계씩 안내만, 본인이 결정·실행 (학부생 인지 게이트 보호).

**예외 — 사용자 명시 위임**: 사용자가 명시적으로 "drift 봉합해줘"라 요청하면 Claude가 갱신 박음 OK. 본 예외는 *사용자 의도 명확*에만, default는 안내만.

---

### 1. CONTEXT.md 통독

(0단계 클리어 후에만 진입)

`CONTEXT.md`를 끝까지 읽으세요 (`CLAUDE.md`는 시스템 컨텍스트로 자동 로드, 재읽기 불필요).

특히 4곳:
- **TL;DR 톤** (학부생 멘토링 / trade-off / 솔직함 / 등급별 보고 / Phase 완료 시 일지 권유)
- **하드 일정** (캡스톤 1, 11/19 본 마감)
- **⏸️ 현재 멈춤 지점** — 오늘 출발점
- **다음 작업** 줄

---

### 2. CHANGELOG 최근 변경 확인

`.claude/CHANGELOG.md` 최신 3~5줄 (이력 표 상단) 빠르게 훑음.

**왜**: 본인이 마지막 작업 이후 팀장(또는 본인)이 헌법/ADR/하네스/공유 파일 변경했을 가능성. 그 변경 모르고 옛 결정 기반 작업하면 충돌.

**판정**:
- CONTEXT.md "마지막 갱신" 날짜보다 새로운 [H] 또는 [M] 변경 → 명시적 안내
- [L] 변경만 / 모두 옛것 → 인지만, 별도 안내 X

---

### 3. 짧은 인지 확인 응답 (work-pin 압축 양식 정합)

CONTEXT.md "다음 Claude를 위한 마지막 안내" #1 따라 **짧게** 응답. 길게 요약 X.

기본 형식 (3~5줄, 이모지 X):

```
Git 상태 클리어 — [브랜치명] / [pull 안내 또는 "이미 최신"]
CONTEXT 읽었어요.

- 멈춤 지점: [현재 멈춤 지점 한 줄]
- 다음 액션: [결정된 다음 작업 한 줄]
- 톤: 학부생 멘토링 / 등급별 보고 / Phase 끝 시 일지 권유

이대로 [다음 액션 동사] 갈까요? 다른 거 먼저 할 거면 말해주세요.
```

---

### 3-부수. CHANGELOG에 [H]/[M] 변경 있으면 추가 안내

본인 CONTEXT.md 마지막 갱신 이후 새로운 [H]/[M] 변경 있으면 응답에 한 섹션 추가:

```
⚠️ 마지막 작업 이후 하네스 변경이 있어요:

- YYYY-MM-DD: [한 줄 요약] [위험도]
- YYYY-MM-DD: [한 줄 요약] [위험도]

작업 시작 전 한 번 보시거나, 본인 작업과 직접 관련 없으면 그냥 진행해도 됩니다.
영향 의심되면 알려주세요.
```

[H] 변경 있으면 명시적으로 "이거 본인 작업과 관련 있나요?" 묻고 답 받기.

---

### 4. 사용자 응답 대기

사용자 GO 하면 작업 시작. 다른 거 하고 싶다고 하면 그 방향으로.

---

### 중요

- **0단계 게이트는 우회 금지** — 사용자가 "그냥 진행해"라 해도 (B)/(C) 상태에서는 CONTEXT 읽기 진입 X. 작업물 유실 위험이 헌법보다 우선
- CONTEXT.md가 헌법과 충돌하면 헌법이 이김
- "현재 멈춤 지점"이 비어있거나 오래됐으면 (마지막 갱신 7일 이상) 사용자에게 알리고 재정렬 제안
- 이 커맨드는 **상태 점검**이지 작업 실행 X. 묻지 않은 코드 변경/파일 작성 금지
- CHANGELOG 확인은 **모든 팀원 자동 적용** — 팀장(유영호) 본인도 본인이 박은 변경 다시 인지

---

### M3.5 새 하네스 변경 (옛 대비)

- **work-pin 양식**: 옛 ~80줄 → 새 30~40줄 압축 ([`../../policies/pin-and-done.md`](../../policies/pin-and-done.md) 정합). 인지 확인 응답도 짧게
- **등급별 보고**: 옛 "매 코드 응답마다 work-envelope" → 새 "단순/보통 = work-pin + commit, 복잡 = -DONE.md, 대규모만 5단계 보고" ([`../../policies/reporting-format.md`](../../policies/reporting-format.md))
- **CHANGELOG 확인은 동일** — 본 절차 변경 X (이미 검증된 협업 셋업)
