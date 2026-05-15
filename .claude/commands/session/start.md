---
description: 새 세션 시작 — git 안전 점검 + CONTEXT.md 읽고 톤·현재 멈춤 지점·다음 액션 + CHANGELOG 최근 변경 짧게 확인
---

사용자가 새 세션을 시작했습니다. 매번 "CONTEXT 읽고 시작해줘"를 문장으로 치는 부담을 없애기 위한 커맨드입니다.

**중요**: 이 커맨드는 `git pull`보다 **먼저** 호출돼야 합니다. 0단계에서 git 상태를 게이트로 점검하고, 안전이 확인되면 그때 사용자에게 `git pull` 안내. 이 순서는 작업물 유실 방지가 목적 (2026-05-15 보강).

다음을 수행하세요:

### 0. Git 안전 점검 (게이트) — 가장 먼저

**왜**: 가이드 흐름이 "git pull → /session:start"였을 때, 어제 작업하던 브랜치에 안 돌아가거나 commit 안 한 변경이 있는 상태에서 main에서 pull을 치면 충돌 발생. 당황한 팀원이 `git reset --hard` 같은 명령을 잘못 치면 작업물 증발. 이 게이트가 그 위험을 사전에 잡습니다.

**실행**:

```bash
git status --porcelain=v1 --branch
```

출력 첫 줄에서 현재 브랜치, 그 아래 줄들에서 워킹 디렉토리 변경 여부를 동시에 확인하세요.

**판정 — 셋 중 하나**:

**(A) 클리어 상태**: `feature/*` 브랜치 + uncommitted 변경 없음
→ 다음 응답으로 짧게 알리고 1단계 진행:
```
Git 상태 클리어 — feature 브랜치 + 워킹 디렉토리 깨끗.
지금 아침 첫 호출이면 `git pull origin main`으로 main 최신 받으세요.
(이미 받았으면 그대로 진행)

[그 뒤에 1단계 CONTEXT 읽기 결과]
```

**(B) main 브랜치에 있음** (uncommitted 변경 유무 무관)
→ **CONTEXT 읽기 진입 금지**. 다음을 출력하고 종료:
```
⚠️ STOP — main 브랜치에 있어요.
💡 처음 보는 STOP이면: .claude/CHANGELOG.md 최상단 또는 가이드 3번 섹션 "갑자기 STOP 떴어요?" 박스 한 번 보세요. 갑자기 동작이 바뀐 게 아니라 안전 장치가 박힌 거예요.

지금 git pull 치면 위험할 수 있어요. 다음 중 하나:

  1) 어제 작업하던 브랜치가 있으면 → git checkout feature/{그-브랜치}
  2) 새 작업이면 → git pull origin main 먼저 → git checkout -b feature/{slug}-{작업명}

해결 후 /session:start 다시 호출해주세요.
```

**(C) feature 브랜치에 있는데 uncommitted 변경 있음**
→ `ProjectSettings.asset` 변경 여부 추가 점검 후 세 갈래 분기 (각자 Cloud 정책 = "B+", 2026-05-15 결정). 점검 명령:

```bash
# 1) ProjectSettings.asset이 변경 목록에 있나
git status --porcelain 03_Client/ProjectSettings/ProjectSettings.asset

# 2) 그 안 변경이 cloud 라인만인지 (cloud 외 추가/삭제 라인이 잡히면 비어있지 않음)
git diff -U0 03_Client/ProjectSettings/ProjectSettings.asset 2>/dev/null \
  | grep -E "^[+-][^+-]" \
  | grep -vE "^[+-][[:space:]]*(cloudProjectId|organizationId):"

# 3) ProjectSettings.asset 외 변경 파일이 또 있나
git status --porcelain | grep -v "03_Client/ProjectSettings/ProjectSettings.asset"
```

**(C-1)** 변경 = `ProjectSettings.asset` *단독* + 변경 라인 = `cloudProjectId` / `organizationId` *만* (위 2번·3번 출력 둘 다 비어있음)
→ 각자 Cloud 정책 (Unity AI 토큰 분리). 자동 정리 후 1단계 진행:

```bash
git checkout 03_Client/ProjectSettings/ProjectSettings.asset
```

다음 응답:
```
감지: ProjectSettings.asset의 cloudProjectId / organizationId 라인만 변경됐어요.
각자 Cloud 정책으로 자동 정리했어요 (Unity 다음번에 자기 계정으로 자동 채워질 거예요).

[이어서 1단계 CONTEXT 읽기 결과]
```

**(C-2)** `ProjectSettings.asset` 변경 *있음* + cloud 외 변경도 있음 (같은 파일 다른 라인 또는 다른 파일들)
→ **CONTEXT 읽기 진입 금지**. STOP + 분리 옵션 안내:

```
⚠️ STOP — ProjectSettings.asset에 cloud 외 변경도 있어요.
💡 처음 보는 STOP이면: .claude/CHANGELOG.md 최상단 또는 가이드 3번 섹션 "갑자기 STOP 떴어요?" 박스 한 번 보세요. 갑자기 동작이 바뀐 게 아니라 안전 장치가 박힌 거예요.

cloud 라인 (자동 정리 대상):
  cloudProjectId / organizationId

cloud 외 변경 (확인 필요):
[변경 라인/파일 요약 2~5줄]

이건 본인이 의도한 변경인가요? Unity AI Assistant 패키지 만지면서
자동 추가됐을 가능성도 있어요 (예: scriptingDefineSymbols).

  1) 의도한 거면 → "맞아 두자" → cloud만 정리하는 명령 안내, 나머지는 그대로 stage 가능
  2) 모르고 추가된 거면 → "다 버려" → 전체 복원 명령 안내

응답 받은 후 처리하고 /session:start 다시 호출해주세요.
```

(C-2) 처리 — 사용자 응답에 따른 명령 안내 (Claude는 안내만, 실행은 사용자):
- "맞아 두자" → `git checkout -p 03_Client/ProjectSettings/ProjectSettings.asset` 안내. cloud hunk만 `y`로 버리고 나머지 `n`으로 살림. 다른 파일 변경은 그대로
- "다 버려" → `git checkout -- 03_Client/ProjectSettings/ProjectSettings.asset [다른 변경 파일들]` 안내. (C) 절대 금지 원칙대로 `reset --hard`는 금지

**(C-3)** `ProjectSettings.asset` 변경 X (다른 파일만 변경 — 기존 (C) 케이스)
→ **CONTEXT 읽기 진입 금지**. 다음을 출력하고 종료:

```
⚠️ STOP — 커밋 안 된 변경이 있어요.
💡 처음 보는 STOP이면: .claude/CHANGELOG.md 최상단 또는 가이드 3번 섹션 "갑자기 STOP 떴어요?" 박스 한 번 보세요. 갑자기 동작이 바뀐 게 아니라 안전 장치가 박힌 거예요.

[git status 출력의 변경 파일 목록 3~5줄]

지금 git pull 치면 충돌 위험. 다음 중 하나:

  1) 어제 작업 마무리 → git add . && git commit -m "wip: {요약}"
  2) 임시 보관 → git stash push -m "{요약}"
  3) 버려도 되는 변경이면 → git checkout -- {특정파일} (전체 reset --hard는 금지)

해결 후 /session:start 다시 호출해주세요.
```

**절대 금지**: 이 게이트에서 Claude가 `git reset --hard`, `git checkout .`, `git clean -fd` 같은 파괴적 명령을 자동으로 또는 사용자 요청으로도 실행하지 마세요. 작업물 유실의 단일 원인입니다. 사용자가 명시적으로 "이 변경 다 버려도 돼"라고 해도, 어느 파일을 어떻게 처리할지 한 단계씩 안내만 하고 실행은 사용자가.

**예외 (B+ 정책, 2026-05-15)**: (C-1) 케이스 — `ProjectSettings.asset` *단독* 변경 + `cloudProjectId`/`organizationId` 라인만 변경됨이 grep으로 *정확히 확인*된 경우에 한해 `git checkout 03_Client/ProjectSettings/ProjectSettings.asset` 1회 자동 실행 허용. Unity가 다음 켤 때 자동 채워주므로 비파괴적 (각자 Cloud 정책). 그 외 모든 ProjectSettings.asset 변경은 (C-2)/(C-3)으로 빠져 사용자 결정.

### 1. CONTEXT.md 통독

(0단계 클리어 후에만 진입)

`CONTEXT.md`를 끝까지 읽으세요 (`CLAUDE.md`는 이미 시스템 컨텍스트로 로드돼 있으니 재읽기 불필요 — 단 헌법과 충돌 시 헌법이 이김).

특히 다음 4곳을 빠뜨리지 마세요:
- **TL;DR 톤** (학부생 멘토링 / trade-off / 솔직함 / 5단계 보고 / Phase 완료 시 일지 권유)
- **하드 일정** (캡스톤 1, 11/19 본 마감)
- **⏸️ 현재 멈춤 지점** — 오늘 무엇부터 할지의 출발점
- **다음 작업 (결정됨)** 줄

### 2. CHANGELOG 최근 변경 확인 (협업 셋업 박힘)

`.claude/CHANGELOG.md` 파일의 **최신 3~5줄**(이력 표의 상단)을 빠르게 훑으세요.

**왜**: 본인이 마지막 작업 이후 팀장(또는 본인)이 헌법/ADR/하네스/공유 파일 변경했을 가능성. 그 변경 모르고 옛 결정 기반으로 작업하면 충돌 발생.

**판정**:
- CONTEXT.md의 "마지막 갱신" 날짜보다 새로운 [H] 또는 [M] 변경이 있으면 → 사용자에게 명시적으로 안내
- [L] 변경만 있거나 모두 옛것이면 → 인지만 하고 별도 안내 X

### 3. 짧은 인지 확인 응답

CONTEXT.md "다음 Claude를 위한 마지막 안내" #1을 따라 **짧게** 응답하세요. 길게 요약하지 마세요 — 사용자는 자기가 쓴 문서를 다시 읽고 싶은 게 아니라, Claude가 제대로 읽었는지 확인하고 싶을 뿐입니다.

기본 형식 (3~5줄, 이모지 X):

```
Git 상태 클리어 — [브랜치명] / [pull 안내 또는 "이미 최신"]
CONTEXT 읽었어요.

- 멈춤 지점: [현재 멈춤 지점을 한 줄로]
- 다음 액션: [결정된 다음 작업 한 줄]
- 톤: 학부생 멘토링 / 5단계 보고 / Phase 끝 시 일지 권유

이대로 [다음 액션 동사] 갈까요? 다른 거 먼저 할 거면 말해주세요.
```

### 3-부수. CHANGELOG에 [H] 또는 [M] 변경이 있으면 추가 안내

본인 CONTEXT.md 마지막 갱신 이후 새로운 [H] 또는 [M] 변경이 있으면 위 응답에 한 섹션 추가:

```
⚠️ 마지막 작업 이후 하네스 변경이 있어요:

- YYYY-MM-DD: [한 줄 요약] [위험도]
- YYYY-MM-DD: [한 줄 요약] [위험도]

작업 시작 전 한 번 보시거나, 본인 작업과 직접 관련 없으면 그냥 진행해도 됩니다.
영향 의심되면 알려주세요.
```

[H] 변경이 있으면 사용자에게 명시적으로 "이거 본인 작업과 관련 있나요?" 묻고 답 받아야 함.

### 4. 사용자 응답 대기

사용자가 GO 하면 그때 작업 시작. 다른 걸 하고 싶다고 하면 그 방향으로.

---

**중요**:
- **0단계 게이트는 우회 금지**. 사용자가 "그냥 진행해"라고 해도 (B)/(C) 상태에서는 CONTEXT 읽기로 못 넘어감. 작업물 유실 위험이 헌법보다 우선하는 안전 장치.
- CONTEXT.md가 헌법과 충돌하면 헌법이 이김 (CONTEXT 본문에도 명시).
- "현재 멈춤 지점"이 비어있거나 오래됐다 싶으면 (마지막 갱신 날짜와 오늘 차이 7일 이상) 사용자에게 알려주고 `/learn:recap`으로 재정렬을 제안하세요.
- 이 커맨드는 **상태 점검**이지 작업 실행이 아닙니다. 묻지도 않은 코드 변경·파일 작성 금지.
- CHANGELOG 확인은 **모든 팀원에게 자동 적용**. 팀장(유영호) 자신도 본인이 박은 변경을 다시 인지하는 데 유용.
