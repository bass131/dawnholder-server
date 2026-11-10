# 전체 코드 감사 병합 관문 도구 묶음 수정과 실행 검증

- 파트: Rules · 리드 `[Rules 리드 Opus]`(claude-opus-5-5 xhigh)
- 상태: **초안**, 메인 대조와 사용자 계획 검토 대기. 작업자는 아직 띄우지 않았다.
- 기준: main `7a724f18012ecfc27253ed9fcd9e8886ab8a0fc9` · branch `fix/rules-audit-gate-tools-20261110`
- 근거 폴더: `.backups/verification/2026-11-10-code-audit-gate-tools/`
- 초안 작성 시각: 2026-11-10T11:31Z(`date -u` 관측, 11-10 20:31 KST)

## 재개 지점

**다음 첫 일:** 메인의 착수 회신을 받으면 선행 시험 계약을 발행한다. 그 전에는 작업자를 띄우지 않는다.

막힌 지점: 없음. 맥락 메모는 근거 폴더의 `rules-lead-context.md`, 승인 범위 문서는 `fix-scope-rules.md`(SHA256 `ee30d35dd614b1c405f01749c0ebf78c121c18afbac29969f766684682c8fa59`, 메인 원본과 일치)다.

## 적용 중인 사용자 결정

전부 메인 pane에서 Enter로 제출된 문장을 메인 `msg_8dcc7476ee66`이 그대로 전달한 것이다. 메인 전달이며 사용자 직접 입력으로 격상하지 않는다.

- 「대시보드 결정 응답: 1) 174 - 전체 코드 감사 1차 수정 범위(심각도 높은 결함) → A 여섯 묶음 34건 · 2) 175 - 감사 수정 착수 시점(지금 또는 20:00 리셋 뒤) → A 지금 서버 묶음부터」
- 「대시보드 결정 응답: 2) 183 - Codex가 GPT Astra 모델을 거부함: 계정을 살릴지, Codex 없이 갈지 → C 코멘트 「구독이 끝났나보네, claude Opus로 해야할 거 같은데」」
- 「대시보드 결정 응답: 1) 185 - Rules 폴더에 남은 10-26 미커밋 작업: 남겨 둘지 버릴지 → A 커밋으로 남기고 시작」

174의 선택지 글과 진행 방식은 범위 문서에 있다. 핵심은 세 가지다. 실패하는 시험을 먼저 만든 뒤 고치고 재현하지 못한 항목은 빼고 기록한다. 고친 뒤 실제 실행으로 확인한다. PR마다 병합 승인 한 줄을 따로 받는다.

185의 결과인 보존 브랜치 `feat/delegation-contract-preflight-20261026`(`1ad20027b06de92ab4567ee62f2ee7076f904396`)은 이 goal의 재료가 아니며 지우거나 고치지 않는다.

## 적용 중인 메인 결정

메인 `msg_8dcc7476ee66`의 판단이다. 사용자 문장이 아니다.

| 역할 | 기동 명령 | 태그 |
|---|---|---|
| 선행 시험 작성자 | `claude --model claude-opus-5-5 --effort high` | `[Rules 시험 Opus]` |
| 구현자 | `claude --model claude-opus-5-5 --effort high` | `[Rules 구현 Opus]` |
| 같은 계약 두 번째 독립 불합격 뒤 다음 구현 시도 | `claude --model claude-opus-5-5 --effort xhigh` | `[Rules 구현 Opus]` |
| 독립 검증자 | `claude --model claude-opus-5-5 --effort xhigh --autocompact 250000` | `[Rules 검증자]` |
| 목표 종료 Gardener | `claude --model claude-opus-5-5 --effort high` | `[Rules Gardener Opus]` |

- 이 표가 checkout의 CLAUDE.md 배정 표와 AGENTS 라우팅보다 먼저다. Codex 세션은 열지 않는다.
- 사용량 제약 때문에 한 번에 작업자 하나, 끝난 뒤 검증자 하나를 기본으로 한다.
- 시험과 재현은 hook 입력 대역으로만 한다. 실제 병합 명령, 원격 main으로 가는 push, `gh api`의 병합 호출을 재현 목적으로 실행하지 않는다.
- 막아야 할 명령의 낱말을 문자열 쪼개기로 숨겨 쓰지 않는다. 시험 입력 문구는 파일로 둔다.
- 정상 단독 병합 명령과 리드의 평소 push를 막지 않는다는 것을 보존 동작 시험으로 함께 확인한다.
- `.claude/settings.json`의 hook 등록을 바꿔야 할 것 같으면 구현 전에 메인에 올린다.
- 발표는 11-11 16:00 KST다. 메인 checkout, 사용자 서버 창, 열려 있는 Unity Editor와 게임 클라이언트, 운영툴 앱을 건드리지 않는다.

## 범위

### 만들 것

| 번호 | 결함 | 고친 뒤의 동작 |
|---|---|---|
| 1 | 저장소를 바꾸는 push를 호출 위치의 브랜치로 판정한다 | branch 조회가 필요한 push는 그 push가 실제로 가리키는 저장소의 HEAD로 판정한다. 대상을 정할 수 없으면 `branch-lookup-failed`로 막는다. |
| 2 | `gh api` 차단이 병합 endpoint 한 꼴과 mutation 둘만 본다 | 브랜치 병합 endpoint, main 참조를 바꾸는 호출, `mergeBranch`·`updateRef`·`createCommitOnBranch`를 `api-merge`로 막는다. 읽기 호출은 통과한다. |
| 3 | 인용부호 밖 특수 공백에서 셸 명령 분석이 전진하지 않는다 | Bash가 낱말 일부로 읽는 문자는 낱말 일부로 남고, 분석은 항상 끝난다. |

세 건 모두 실패 시험으로 재현되지 않으면 범위에서 빼고 이 문서에 시도와 관측을 적는다.

### 건드릴 곳

- `99_Tools/MergeGate/merge-policy.mjs`, `99_Tools/MergeGate/claude-hook.mjs`.
- `99_Tools/SessionGuard/shell-command.mjs`.
- `99_Tools/MergeGate.Tests/`와 `99_Tools/SessionGuard.Tests/`의 새 시험 파일. 기존 시험 기대값은 바꾸지 않는다.
- 동작이 바뀌어 틀려지는 서술만: `99_Tools/README.md`의 병합 관문 결과 코드 표와 한계 문단, `00_Document/operations/ORCA.md`의 「병합 관문」.
- 이 goal과 `00_Document/operations/CURRENT.md`의 Rules 줄. CURRENT는 기존 Rules 항목을 보존하고 추가만 한다.

### 하지 않을 것

- 감사의 다른 다섯 묶음과 중간·낮은 심각도 발견.
- `.claude/settings.json`의 hook 등록 변경.
- 11-10 결정 183의 배정 표를 AGENTS에 반영하는 일 등 그 밖의 정본 정리.
- MergeGate의 낱말 나누기(`\s+`)를 Bash 의미로 바꾸는 일. 기존 후보 `merge-gate-code-followup`에 있다.
- 감사가 지목한 꼴 밖의 쓰기 API. 예를 들면 contents API의 branch 지정 쓰기와 `merge-upstream`이다. README 한계에 적고 막지 않는다.
- 문자열 판정의 의도적 우회 차단(별칭·스크립트 파일·변수 속 명령, 경로 인코딩). 기존 한계 그대로다.

### 관찰 가능한 완료조건

1. 범위에 남은 항목마다 수정 전 코드에서 실패한 시험과, 수정 뒤 같은 명령의 통과 원시가 근거 폴더에 있다.
2. 호출 위치가 작업 브랜치이고 `-C`·`--git-dir`·같은 명령의 `cd`가 main 저장소를 가리키는 push가 `push-main`으로 막힌다. 반대 배치의 정상 push는 결정 없음으로 통과한다.
3. 대상을 정할 수 없는 저장소 전환이 있는 조회형 push는 `branch-lookup-failed`로 막힌다. 목적지를 명시한 push의 판정은 그대로다.
4. 범위의 쓰기형 `gh api` 호출이 `api-merge`로 막히고, 읽기 호출은 결정 없음이다.
5. 인용부호 밖의 해당 문자가 든 명령에서 SessionGuard hook이 메모리·시간 상한 안에 정상 결정을 낸다. 그 낱말 뒤 리다이렉트 목적지 판정도 유지된다.
6. 보존 동작이 통과한다. 단독 병합 명령 경로, `git push -u origin HEAD`, 명시적 작업 branch push, 따옴표 안 특수 공백, 기존 시험 전부가 대상이다.
7. 아래 실행 검증 표대로 실제 실행했고, 실행하지 않은 것은 이유와 함께 미실행으로 적혀 있다.
8. PR마다 새 독립 검증자의 PASS 판정 원문이 있고, 사용자 병합 승인 한 줄을 PR마다 따로 받는다.

## 설계 선택

구현 계약에는 요구와 판정 기준으로 넣고, 아래 방식은 권장으로 둔다. 구현자가 다른 방식을 고르면 이유를 보고한다.

- **1번의 조회.** `inspectPushes`가 조회가 필요한 push마다 저장소 선택 정보를 함께 돌려주고, hook이 push별로 조회한다. 저장소 선택은 같은 명령의 `cd` 누적 경로와 git 전역 옵션 `-C`·`--git-dir`·`--work-tree`를 **쓰인 순서대로** 모은 인자 목록이다. hook은 `git <그 인자들> symbolic-ref --quiet --short HEAD`를 입력 `cwd`에서 실행한다. git이 상대 경로 합성을 직접 하므로 경로 계산을 복제하지 않는다. 대안은 merge-policy에서 절대 경로를 계산해 넘기는 것인데, git의 합성 규칙을 복제하게 된다. 조회 대상 중 하나라도 main이면 `push-main`, 하나라도 실패하면 `branch-lookup-failed`다.
- **1번의 `cd` 범위.** `&&`·`;`·줄바꿈으로 이어진 `cd`는 따른다. 괄호가 닫히면 밖의 위치로 돌아간다. `|`·`||`·홀로 선 `&` 앞의 `cd`와 `cd -`·인자 없는 `cd`·`popd`, 변수·치환이 든 경로는 위치를 정할 수 없는 것으로 본다. 기존 시험의 `(cd . && git push)`는 그대로 통과해야 한다.
- **1번의 Windows 경로.** Git Bash의 `/c/…`는 hook이 띄우는 Windows `git.exe`가 그대로 읽지 못할 수 있다(추론, 시험으로 확인). 정상 명령을 잘못 막지 않도록 조회 전에 드라이브 형태로 바꾼다. CI는 ubuntu이므로 이 사례는 win32에서만 실행하고, 다른 OS에서 건너뛴 사실이 시험 출력에 드러나야 한다.
- **2번의 판정.** 정밀 판정과 마지막 그물이 같은 판정 함수를 쓴다. `gh --hostname h api …`처럼 정밀 판정을 지나는 꼴을 마지막 그물이 막기 때문이다. 쓰기 판정은 명시 `-X`·`--method`가 GET이 아니거나, method 없이 `-f`·`-F`·`--field`·`--raw-field`·`--input`이 있는 경우다.
- **3번의 수정.** `word`의 낱말 경계를 `sequence`가 건너뛰는 문자와 같은 공유 판정으로 맞춘다. 경계는 공백·탭·CR·LF다. CR은 기존 처리를 유지한다. NBSP를 분리자로 바꾸지 않는다.

## 항목별 선행 시험 계획

전부 제품 경로를 실제로 지난다. hook 프로세스에 stdin JSON을 넣고, 저장소가 필요하면 `createRepository`의 임시 저장소를 쓴다. 기대값을 제품과 같은 계산으로 다시 만들지 않는다. 세션 하나로 세 건을 쓰고 시험 파일은 PR별로 나눈다. 첫 단계는 수정 전 기준선이다. 두 시험 묶음의 같은 명령 통과·실패 수를 원시로 남긴다.

| 번호 | 시험 파일(예정) | 사례 | 수정 전 예상 |
|---|---|---|---|
| 1 | `MergeGate.Tests/push-target.test.mjs` | 작업 브랜치 위치에서 `-C`·`--git-dir`(값 분리·`=` 형태)·`--work-tree` 동반·`cd`·`pushd`·누적 `cd`·괄호 안 `cd`로 main 저장소를 가리키는 조회형 push, Monitor 도구, 한 명령에 push 둘 | 결정 없음(잘못 통과) |
| 1 | 같은 파일 | main 위치에서 작업 브랜치 저장소를 가리키는 push | `push-main`(잘못 차단) |
| 1 | 같은 파일 | 변수·치환·`cd -`·인자 없는 `cd`·`GIT_DIR=` 대입·`(cd 작업저장소); git push origin HEAD` | 위치에 따라 잘못 통과 |
| 1 | 같은 파일 | 보존: 위치 저장소 `git push -u origin HEAD`, 공백 경로 `-C "…"`, 변수 경로라도 목적지 명시 push, Git Bash 경로(win32) | 통과 또는 결정 없음 |
| 2 | `MergeGate.Tests/api-writes.test.mjs` | `/merges`, `-X PATCH`·`--method=DELETE`·`-XPATCH`·필드 동반 `git/refs/heads/main`, GraphQL 세 이름, `gh.exe`·`-R`·`--hostname` 꼴 | 결정 없음 |
| 2 | 같은 파일 | 보존: 읽기 `git/refs/heads/main`·`git/ref/heads/main`, `-X GET`, `pulls/12`, `mergeStateStatus`를 읽는 GraphQL query | 결정 없음 |
| 3 | `SessionGuard.Tests/shell-blank.test.mjs` | 인용부호 밖 NBSP·FF·VT·U+3000·U+FEFF·U+2028 든 명령이 상한 안에 끝나고 낱말이 나뉘지 않음, 그 낱말 뒤 리다이렉트 목적지가 위치 판정을 받음 | 시간 또는 메모리 초과 |
| 3 | 같은 파일 | 보존: 따옴표 안 NBSP, 일반 공백·탭·CRLF 명령 | 통과 |

3번 시험의 안전 조건은 아래 「위험」 절의 항목을 계약에 그대로 넣는다.

## PR 경계와 검증

`origin/main`에서 각각 독립으로 낸다. 3번은 차단 범위가 바뀌지 않고, 1·2번은 차단이 넓어져 잘못 막는 위험이 있다. 나눠 두면 발표 전에 멈춤 수정만 넣고 차단 확대는 뒤로 미룰 수 있다.

| PR | 항목 | branch | 등급과 이유 |
|---|---|---|---|
| 셸 분석 멈춤 | 3 | `fix/rules-audit-shell-blank-20261110` | 강. 셸 입력 경계, 실행 도구 |
| 관문 판정 | 1·2 | `fix/rules-audit-gate-tools-20261110` | 강. 보안 경계, 실행 도구 |

순서는 선행 시험 하나 → 셸 분석 멈춤 구현 → 검증 → 관문 판정 구현 → 검증이다. 이 goal 문서는 관문 판정 branch에 둔다. 셸 분석 멈춤 PR에는 도구와 시험 파일만 들어간다. R-7은 메인 승인이 없어 비해당이다. 실제 변경 줄 수의 원시는 각 PR 구현 뒤 근거 폴더에 둔다.

### 실행 검증

| 검증 | 언제 | 닿는 이유 또는 닿지 않는 이유 |
|---|---|---|
| `node --test 99_Tools/MergeGate.Tests/*.test.mjs` | 수정 전 기준선, 구현자, 검증자 | 변경 대상이다 |
| `node --test 99_Tools/SessionGuard.Tests/*.test.mjs` | 수정 전 기준선, 구현자, 검증자 | 변경 대상이다 |
| 실제 진입: hook 프로세스에 stdin JSON + 임시 git 저장소 | 구현자와 검증자 | **강 등급의 실제 경로 1회를 이것으로 정한다.** hook의 실제 진입점이고 메인 금지 명령을 치지 않는다 |
| 리드의 실제 작업 branch push가 고친 hook을 지남 | PR 올릴 때 | 보존 동작의 실제 경로다 |
| 규칙 검사 `node 99_Tools/CodeRules/check-code-rules.mjs --scope Changed` | PR마다 | 인자는 DEVELOPMENT 「코드 규칙 검사」를 따른다. `.mjs`가 대상이 아니면 대상 0건을 통과로 적지 않고 그 사실을 적는다 |
| 원격 CI `code-rules` | PR마다 | 새 시험 파일이 기존 단계의 glob에 들어간다 |
| 살아 있는 세션에서 차단 대상 명령을 직접 실행 | **미실행** | 메인 금지 사항이다 |
| 서버 시험·Unity 시험·자동 접속 봇·DB 저장 실행 | **미실행** | 서버·클라이언트 코드, 프로토콜, DB를 바꾸지 않는다 |

## 위험

- **hook은 작업 트리 파일을 그대로 실행한다.** 구현자가 고치는 동안 이 worktree의 Claude 세션 관문이 바뀐다. 문법 오류면 hook이 결정 없이 끝나 명령이 지나간다(추론, 미측정). 메인 checkout의 hook은 별도 파일이라 main을 받기 전에는 영향이 없다. 구현 계약에 편집마다 `node --check`와 구현 종료 전 두 시험 묶음 실행을 넣는다. 리드는 구현 중 push하지 않는다.
- **3번의 실패 재현은 무한 루프와 메모리 증가다.** 시연 PC이므로 기존 fixture의 30초 시간 상한을 쓰지 않는다. 자식 node에 `--max-old-space-size=64`와 5초 시간 상한, 강제 종료를 건다. 계약의 수치는 수정 전 재현 조건일 뿐 제품 상한이 아니다.
- **수정 전에는 어느 세션도 인용부호 밖 NBSP가 든 Bash 명령을 치지 않는다.** 살아 있는 SessionGuard가 같은 루프에 빠진다. 시험의 특수 문자는 `String.fromCharCode`로 조립한다. Write·Edit 도구는 역슬래시-u 표기를 실제 문자로 풀어 넣는다.
- **첫 실제 단독 병합.** 메인 checkout이 관문 판정 PR을 받은 뒤 처음 도는 병합이 고친 hook의 첫 실제 경로다. 시험은 정확한 병합 명령 형태를 대역으로만 확인한다. 그래서 관문 판정 PR의 병합 시점은 승인 요청 때 사용자 판단으로 올린다.

## 메인 판단 요청

세 안건 모두 추천대로 초안을 썼다. 다른 답이면 시험 계약 전에 고친다.

| 안건 | 추천 | 대안 |
|---|---|---|
| 1번에서 대상을 정할 수 없는 저장소 전환 | 조회형 push를 `branch-lookup-failed`로 막는다. 변수로 경로를 쓴 push는 경로를 직접 적어 다시 실행해야 한다 | 입력 `cwd`로 판정한다. 잘못 막는 일은 줄지만 감사가 지목한 통과 경로가 남는다 |
| `GIT_DIR=`·`GIT_WORK_TREE=`·`GIT_COMMON_DIR=` 대입을 1번에 넣을지 | 넣는다. 저장소 전환이 명령 글자에 드러나므로 정규식 하나로 막을 수 있고 범위의 「해석 불가한 저장소 전환 거부」에 해당한다. 조회형 push만 막는다 | 뺀다. README 한계에 적는다 |
| 2번의 `git/refs/heads/main` | 쓰기 호출만 막는다. main의 현재 커밋을 읽는 조회는 통과한다 | 낱말만으로 전부 막는다. 구현은 단순하지만 읽기 조회가 `api-merge`로 막힌다 |

## 결과와 남은 위험

아직 없다.

## 다음 계획 후보

아직 없다.

## 진척 단계

PR 번호는 PR을 만들 때 채운다.

- [>] goal 초안 승인
- [ ] 선행 시험 작성
- [ ] 셸 분석 멈춤 구현
- [ ] 셸 분석 독립 검증
- [ ] 관문 판정 구현
- [ ] 관문 판정 독립 검증
- [ ] 셸 분석 PR 병합
- [ ] 관문 판정 PR 병합
- [ ] 종료 기록 정리
