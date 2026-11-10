# 전체 코드 감사 병합 관문 도구 묶음 수정과 실행 검증

- 파트: Rules · 리드 `[Rules 리드 Opus]`(claude-opus-5-5 xhigh)
- 상태: **진행**, 사용자 계획 승인(결정 4 A). 셸 분석 멈춤은 PR311로 병합됐다. 관문 판정은 독립 검증 1회차 불합격 뒤 수정 회차다.
- 기준: main `c9dc44e44c4f37e598e2dd70ea0aa18d579529be` · branch `fix/rules-audit-gate-tools-20261110`
- 근거 폴더: `.backups/verification/2026-11-10-code-audit-gate-tools/`
- 초안 작성 시각: 2026-11-10T11:31Z(`date -u` 관측, 11-10 20:31 KST)
- 승인된 초안: 커밋 `45525cc1`, SHA256 `30e97e06e5d47fecc66bae4a2dcddab6dfdcc2a8e8163521a3730cff86fddab0`. 승인 뒤 최신 main 위로 옮겨 커밋은 `cd80b266`이 됐고 파일 hash는 같다. PR310과 도구 파일 차이는 없다.

## 재개 지점

**다음 첫 일:** 관문 판정 수정 계약 v2를 `pr-gate-fix1/`에 발행하고 새 구현자를 연다. 요구는 아래 「관문 판정 독립 검증 1회차」의 리드 판단과 고친 「설계 선택」 1번이다. 검증자 시험 `verify-push-api.test.mjs`는 로컬 커밋만 하고, 수정이 통과하기 전에는 원격에 올리지 않는다(PR312 CI가 실패로 바뀐다). PR312는 draft로 둔다. 근거는 선행 시험 `tdd/`, PR311 `pr-shell/`, 관문 판정 구현 `pr-gate/`, 그 검증 `pr-gate/verify/`, 리드 탐침 `lead-probe/fix-class-*`에 있다.

막힌 지점: 없음. 맥락 메모는 근거 폴더의 `rules-lead-context.md`, 승인 범위 문서는 `fix-scope-rules.md`(SHA256 `ee30d35dd614b1c405f01749c0ebf78c121c18afbac29969f766684682c8fa59`, 메인 원본과 일치)다.

## 적용 중인 사용자 결정

### 2026-11-11 PR311 병합 승인(메인 전달 `msg_73045027428b`)

사용자 원문(11-11 04:1x KST 메인 pane 제출, 메인 전달): 「병합 승인: PR311 head d531680e2fe726e7fec0b8bc00661f1b21c7b65c」

메인이 결정 188로 「A 승인(지금 병합)」·「B 발표 뒤로 보류」를 물었고, 사용자는 위 승인 줄을 제출했다. 메인이 병합했고 병합 커밋은 `6a0aa4536c8451aecf21e554c7ac6db1b8a88b16`(mergedAt 2026-11-10T19:18:22Z)이다.

### 2026-11-11 결정 4 A — 계획 승인(메인 전달 `msg_3c7e48074e88`)

사용자 원문(11-11 02:0x KST 메인 pane 제출, 메인 전달, 한 문장 전체):

> 대시보드 결정 응답: 2) 187 - 발표 시연에 Content·Core 작업 폴더를 쓰는지(게임 클라이언트와 서버를 띄우는 곳) → A 안 쓴다 · 3) 계획 검토 - Content Content 코드 감사 클라이언트 묶음 11건 목표 초안 → A 승인 (초안 msg_64140427bd4e) · 4) 계획 검토 - Rules Rules 코드 감사 병합 관문 도구 묶음 3건 목표 초안 → A 승인 (초안 msg_7f890ca476f4) · 5) 계획 검토 - Management Management 코드 감사 운영 도구 묶음 3건 목표 초안 → A 승인 (초안 msg_79a19b3f8478)

Rules에 해당하는 부분은 「4) 계획 검토 - Rules … → A 승인 (초안 msg_7f890ca476f4)」다. 사용자는 메인 정리본을 읽고 승인했다.

승인 범위 문서와 달라진 점으로 사용자에게 보인 것은 둘이다.

1. 결정 174의 실행 검증 가운데 서버 시험·Unity 시험·자동 접속 봇·DB 저장 실행을 돌리지 않는다.
2. 1번이 `pushd`와 환경 변수 대입까지 넓어졌다.

병합 시점: 병합 승인은 PR마다 따로 받는다. 메인은 관문 판정 PR을 발표(11-11 16:00 KST) 뒤에 병합하도록 사용자에게 권했다. 셸 분석 멈춤 PR은 준비되는 대로 보고하고, 언제 넣을지는 사용자가 정한다.

### 2026-11-10 결정 174 A·175 A·183·185(메인 전달 `msg_8dcc7476ee66`)

전부 메인 pane에서 Enter로 제출된 문장을 메인이 그대로 전달한 것이다. 메인 전달이며 사용자 직접 입력으로 격상하지 않는다.

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
- 관문 판정 독립 검증 1회차 뒤(`msg_97a1adfc9c72`): 검증 관찰 O2는 **A**, 이번 PR312에 넣지 않는다. 승인 범위는 감사 3건이고, O2는 수정 전부터 있던 동작이며 원인이 1번과 다르다. 넣으려면 사용자 범위 승인이 필요하다. 종료 기록의 다음 계획 후보에 재현 명령 한 줄과 함께 앞 순서로 올리고, README 한계 목록에는 이번 회차에 한 줄 적는다. 메인은 사용자에게 따로 알린다. 수정 회차 방침도 받아들였다. #1은 불변 조건으로 고치고 고친 설계 문장은 옛 문장과 함께 남긴다. #2의 보존 조건은 수정 계약의 실패 조건으로 넣는다. 수정은 새 구현자, 재검증은 새 검증자가 한다. 재검증 계약에는 1회차 결함 번호와 리드 탐침 꼴을 입력으로 주되, 통과 여부는 검증자가 스스로 판정한다.
- 착수 조건(`msg_3c7e48074e88`): 3번이 main에 들어가고 각 작업 폴더가 그 main을 받기 전에는, 어느 Claude 세션도 따옴표 밖에 특수 공백(U+00A0 등)이 든 Bash 명령을 치지 않는다. 이 문장을 모든 작업자와 검증자 계약에 넣는다. 실행은 Rules 작업 폴더와 임시 저장소 안에서만 한다. Unity·WSL·게임 포트·운영 도구 앱이 필요해지면 시작 전에 메인에 요청한다. Claude 5시간 한도에 걸리면 재시도를 반복하지 않고 상태를 이 문서에 남긴 뒤 메인에 알린다. 사용자 결정이 필요한 일은 메인에 올리고 그 항목만 멈춘 채 나머지를 진행한다.

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
- **1번의 `cd` 범위.** `cd`·`pushd`의 위치는 그 `cd`가 돌아야만 실행되는 명령에만 준다. 앞 명령의 성패나 중괄호 묶음·함수 본문 때문에 `cd`가 돌지 않아도 push가 돌 수 있으면, 위치를 정할 수 없는 것으로 본다. 예는 `false && cd x; git push`, `true || { cd x; }; git push`, `f() { cd x; }; git push`다. 무조건 도는 `cd` 뒤의 `;`·줄바꿈과 `&&` 사슬은 따른다. 괄호가 닫히면 밖의 위치로 돌아간다. 따옴표 안의 `(`·`)`·역따옴표는 범위를 열거나 닫지 않는다. 따옴표 안에서 이루어지는 위치 변경(감싼 `bash -c "…"`, 큰따옴표 안 치환)은 위치를 정할 수 없는 것으로 본다. `|`·`||`·홀로 선 `&` 앞의 `cd`와 `cd -`·인자 없는 `cd`·`popd`, 변수·치환이 든 경로도 정할 수 없다. 기존 시험의 `(cd . && git push)`는 그대로 통과해야 한다.
  - 11-11 독립 검증 1회차 결함 #1·#2 뒤 리드가 고친 문장이다. 이전 문장 「`&&`·`;`·줄바꿈으로 이어진 `cd`는 따른다」는 앞 명령이 실패해 `cd`가 돌지 않는 경우를 놓쳤고, 구현은 그 문장을 그대로 따랐다.
- **1번의 Windows 경로.** Git Bash의 `/c/…`는 hook이 띄우는 Windows `git.exe`가 읽지 못한다(11-11 리드 관측: `fatal: cannot change to '/c/Users/…'`, 근거 폴더 `lead-probe/git-path-forms.out.txt`). 정상 명령을 잘못 막지 않도록 조회 전에 드라이브 형태로 바꾼다. CI는 ubuntu이므로 이 사례는 win32에서만 실행하고, 다른 OS에서 건너뛴 사실이 시험 출력에 드러나야 한다.
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

## 메인 판단 요청과 답

세 안건 모두 메인의 답은 「추천대로」다(`msg_3c7e48074e88`, 정리본에 적혀 사용자가 함께 승인).

| 안건 | 추천(채택) | 대안 |
|---|---|---|
| 1번에서 대상을 정할 수 없는 저장소 전환 | 조회형 push를 `branch-lookup-failed`로 막는다. 변수로 경로를 쓴 push는 경로를 직접 적어 다시 실행해야 한다 | 입력 `cwd`로 판정한다. 잘못 막는 일은 줄지만 감사가 지목한 통과 경로가 남는다 |
| `GIT_DIR=`·`GIT_WORK_TREE=`·`GIT_COMMON_DIR=` 대입을 1번에 넣을지 | 넣는다. 저장소 전환이 명령 글자에 드러나므로 정규식 하나로 막을 수 있고 범위의 「해석 불가한 저장소 전환 거부」에 해당한다. 조회형 push만 막는다 | 뺀다. README 한계에 적는다 |
| 2번의 `git/refs/heads/main` | 쓰기 호출만 막는다. main의 현재 커밋을 읽는 조회는 통과한다 | 낱말만으로 전부 막는다. 구현은 단순하지만 읽기 조회가 `api-merge`로 막힌다 |

## 결과와 남은 위험

### 선행 시험 결과

세션 하나(`[Rules 시험 Opus]`, 첫 화면 Opus 5.5 high, backend unknown)가 세 건을 썼다. 계약·보고·원시는 근거 폴더 `tdd/`에 있다. 세 건 모두 재현됐고 범위에서 뺀 항목은 없다.

| 번호 | 시험 파일 | 수정 전 실패 | 보존 | 수정 전 실패의 모양 |
|---|---|---|---|---|
| 3 | `SessionGuard.Tests/shell-blank.test.mjs` | 18 | 10 | 64MB 힙 안에서 hook이 exit 134로 끝남(메모리 부족) |
| 2 | `MergeGate.Tests/api-writes.test.mjs` | 29 | 9 | 결정 없이 통과 |
| 1 | `MergeGate.Tests/push-target.test.mjs` | 31 | 13 | 결정 없이 통과 28건, 반대 배치의 잘못된 차단 3건 |

- 두 묶음의 기준선은 MergeGate 126건·SessionGuard 167건 전부 통과다. 시험을 더한 뒤 수정 전 코드의 최종 실행은 MergeGate 208건 중 실패 60건, SessionGuard 195건 중 실패 18건이다.
- 최종 실패 78건이 수정 전 실패 분류 78건과 일치하고 기존 시험 실패는 0건이다. 리드가 `tdd/raw/`의 요약 줄과 대조했다.
- 시험은 hook 입력 대역과 임시 저장소만 썼다. 실제 병합 명령, main으로 가는 push, API 호출은 실행하지 않았다.
- 정산: `worker-release`는 retained·external_terminal·none이었고, 빈 prompt를 확인한 뒤 pane을 닫았다(`tdd/worker-release.json`, `tdd/terminal-close.json`).

### 셸 분석 멈춤 결과(PR311)

- branch `fix/rules-audit-shell-blank-20261110`, PR311, head `d531680e2fe726e7fec0b8bc00661f1b21c7b65c`. 커밋은 선행 시험 `ee8998ce`, 구현 `24e66aab`, 독립 검증 시험 `d531680e`이다.
- 구현(`[Rules 구현 Opus]`, Opus 5.5 high, backend unknown): `shell-command.mjs` 한 파일 +8 −2. 낱말 경계를 `sequence()`가 소비하는 문자와 같은 판정으로 맞추고, 위치가 전진하지 않으면 명시적 오류를 낸다. 수정 전 28건 중 18건 실패가 같은 명령에서 28건 통과로 바뀌었다(`pr-shell/report.md`).
- 독립 검증(`[Rules 검증자]`, Opus 5.5 xhigh, backend unknown): **PASS**, 결함 0건(`pr-shell/verify/verdict.md`). 독립 시험 59건, SessionGuard 254건, MergeGate 126건이 통과했다. 실제 진입은 hook 프로세스에 stdin JSON을 넣는 9건이다. 살아 있는 세션 입력과 도달 경로가 없는 전진 가드는 미실행이다.
- 비차단 관찰 넷 가운데 리드 처리는 둘이다. 관찰 1(CR이 Bash와 달리 공백이라는 이유 주석)은 head를 바꾸면 재검증이 필요해 이번 PR에 넣지 않는다. 관찰 3(분석 예외의 결정 없음 종료)은 기존 설계이며 아래 다음 계획 후보로 둔다.
- 리드 대조: 판정 원문을 읽었고 `verify/raw/`의 요약 줄(28/28, 59/59, 254/254, 126/126)이 판정 수치와 같다.
- 규칙 검사 `check-code-rules.mjs`는 로컬에서 돌리지 않았다. 대상 언어가 PowerShell·TypeScript·Python이라 이 변경에는 해당 파일이 없다. CI `code-rules`가 두 시험 묶음을 함께 돈다.
- 정산: 구현자와 검증자 모두 `worker-release` retained·external_terminal·none 뒤 빈 prompt를 확인하고 pane을 닫았다.
- 리드의 실제 작업 branch push는 고친 SessionGuard와 기존 MergeGate를 지나 통과했다.

### 관문 판정 구현 결과

- 구현(`[Rules 구현 Opus]`, Opus 5.5 high, backend unknown): 커밋 `4e9b2672`, 네 파일 +203 −47. `merge-policy.mjs` +179 −39, `claude-hook.mjs` +15 −5, README +8 −2, ORCA +1 −1이다(`pr-gate/report.md`).
- 수정 전 실패 60건(1번 31, 2번 29)이 같은 명령에서 통과로 바뀌었다. MergeGate 208건과 SessionGuard 167건이 전부 통과했다. hook 프로세스에 stdin JSON을 넣는 실제 진입 19건은 불일치 0이다.
- 구현자가 `ask` 없이 정한 것 하나: 따옴표가 닫히지 않은 `-C` 값은 옛 규칙대로 그 낱말만 건너뛴다. 기존 시험 `word-grouping.test.mjs`의 기대를 지키려는 선택이다. 독립 검증이 판정한다.
- 구현자가 스스로 보고한 계약 이탈은 둘이다. `node --check`를 편집마다가 아니라 편집 묶음마다 돌렸다(Edit 16회, 점검 9회, 묶음 사이 Bash 없음). 5분 간격 신호를 지키지 못했다.
- 구현자가 적은 범위 밖 한계 가운데 판정이 필요한 것은 셋이다. 따옴표 안에 분리 문자가 든 경로의 push를 못 본다. 괄호 추적이 따옴표를 무시한 자르기 위에 있어 짝 없는 `)` 문구에서 잘못 막거나 지나간다. 명령 첫 낱말이 아닌 `cd` 낱말 뒤의 조회형 push를 막는다. 독립 검증이 차단 여부를 판정한다.
- 리드 대조: `pr-gate/raw/`의 요약 줄(44/44, 38/38, 208/208, 167/167, 진입 19건 일치)이 보고 수치와 같다. 시험 폴더는 HEAD와 같다. 보고는 `hook-fixture.mjs` 해시가 계약 값과 같다고 적었지만 계약에는 그 해시가 없었다. 파일 변경은 없다.
- 정산: `worker-release` retained·external_terminal·none 뒤 빈 prompt를 확인하고 pane을 닫았다.
- PR312를 draft로 열었다(ubuntu CI를 판정 전에 보려는 목적). 구현 커밋과 goal 커밋 위의 CI 4건이 통과했다. 리드의 작업 branch push는 고친 MergeGate hook을 지나 통과했다.

### PR311 병합

- 사용자 승인 원문과 병합 커밋은 「적용 중인 사용자 결정」의 PR311 절에 있다. 메인 대조 기록은 `msg_83952c237f64`, 병합 알림은 `msg_73045027428b`다.
- 관문 판정 branch는 병합된 main을 받았다(커밋 `d774eb27`). 들어온 변경은 PR311의 세 파일뿐이다. 그 뒤 리드 실행은 SessionGuard 254건 전부 통과, MergeGate 233건 중 실패 2건이다. 실패 2건은 아래 검증자 시험의 #1·#2다(`lead-probe/after-sync-*.log`).
- 이 작업 폴더의 hook도 고친 SessionGuard가 됐다. 메인 원문의 특수 공백 주의 문장은 계약에 계속 넣는다. 다른 작업 폴더가 새 main을 받았는지는 이 폴더에서 확인하지 않는다.

### 관문 판정 독립 검증 1회차(NOT PASS)

- 검증(`[Rules 검증자]`, Opus 5.5 xhigh, backend unknown): **NOT PASS**(`pr-gate/verify/verdict.md`). 기존 MergeGate 208건과 SessionGuard 167건은 통과했다. 독립 시험 `verify-push-api.test.mjs` 25건 중 2건이 실패했고, 실제 진입 41건 중 불일치는 9건이다.
- 차단 #1: main 위치의 `false && cd <작업 저장소>; git push origin HEAD`가 결정 없이 지난다. 옛 hook은 `push-main`으로 막았다. 귀속은 리드의 설계 문장이다.
- 차단 #2: `(cd <main> && git commit -m "1) fix" && git push origin HEAD)`처럼 따옴표 안의 `)`·역따옴표가 범위를 일찍 닫아 main push가 지난다. 귀속은 구현이다.
- 비차단 #3: 옵션 낱말을 따옴표로 감싼 `gh api "-X" DELETE …/git/refs/heads/main`이 결정 없이 지난다.
- 리드 대조: 판정이 지목한 원시 r3b·r7b 두 줄과 시험 실패 2건이 판정과 같다.
- 리드 탐침(`lead-probe/fix-class-attempt-1.log`, 24건): #1은 한 꼴이 아니라 부류다. 조건부 `cd`의 `;`·줄바꿈·`||` 뒤, `true || { cd x; }`, `false && { cd x; }`, 함수 본문 `f() { cd x; }` 뒤의 조회형 push 6건이 main에서 결정 없이 지난다. `if`·`case`·홀로 선 `&`는 이미 막힌다. #2를 따옴표 안 범위 글자만 무시하는 식으로 고치면, 지금 막히는 `bash -c "(cd <작업 저장소>); git push origin HEAD"`가 새로 지날 수 있다.
- 리드 판단:
  - 구현자가 `ask` 없이 정한 열린 따옴표 `-C` 건너뛰기는 사후 수용한다. 근거는 둘이다. Bash는 닫히지 않은 따옴표에 명령 나머지를 값으로 읽으므로, 쓰인 push가 실제로 돌려면 경로 이름 자체가 push 낱말을 담아야 한다. 검증자가 실제 진입(oq1·oq2)으로 판정이 옛 동작과 같은 입력 cwd 기준임을 확인했다. 계약의 `ask` 절차를 건너뛴 것은 비차단 관찰로 남기고, 수정 계약에 이 절차를 다시 적었다.
  - #1·#2는 이번 범위 안 결함으로 고친다. 고친 요구는 위 「설계 선택」 1번이다.
  - #3은 goal 「하지 않을 것」의 의도적 우회에 가까워 README 한계 문장으로 처리한다. 정규식을 바꾸려면 실패 시험이 따로 필요하다.
  - 검증자 관찰 O1(따옴표 안 분리 문자가 든 경로)·O3(남는 오탐)·O4(끝 `/`가 붙은 main 참조)는 README 문장으로 처리한다. 가독성 지적 1은 주석 한 줄이다. 이 넷은 같은 수정 회차에 넣는다.
  - O2(main 위치의 `git push origin 2>&1`이 결정 없이 지남)는 기존 동작이다. 메인 판단은 A로, 이번 PR에 넣지 않고 README 한계에 한 줄 적는다(「적용 중인 메인 결정」). O5(조회 수 상한과 hook timeout)는 다음 계획 후보다.
- 집계: 같은 계약의 독립 불합격 1회다. 구현에 귀속된 결함은 #2다. 다음 구현자는 표대로 `claude --model claude-opus-5-5 --effort high` 새 세션이다.
- 정산: `worker-release` retained·external_terminal·none 뒤 빈 prompt를 확인하고 pane을 닫았다.

## 다음 계획 후보

메인 판단(`msg_97a1adfc9c72`)에 따라 첫 항목을 앞 순서로 둔다.

1. 리다이렉트 숫자가 refspec으로 읽히는 push. 재현: main 위치(main checkout)에서 `git push origin 2>&1`을 치면 main 위치에서 결정 없이 지난다. 기존 동작이며 원인은 리다이렉트 숫자를 인자로 읽는 것이다. 마지막 그물에도 main 낱말이 없어 지난다. 원천은 관문 판정 독립 검증 관찰 O2(`pr-gate/verify/verdict.md`).
2. 조회형 push의 조회 수 상한. push마다 git 조회 하나와 5초 상한이 있고, hook 등록에는 timeout이 없다. 원천은 같은 판정의 관찰 O5.
3. SessionGuard 분석 예외의 결정 없음 종료. 따옴표가 닫히지 않은 명령이나 매우 깊은 `$(` 중첩에서 분석이 예외로 끝나면 hook이 결정 없이 통과시킨다. 기존 설계이며 이번 범위 밖이다. 원천은 PR311 독립 검증 관찰 3(`pr-shell/verify/verdict.md`). 종료 기록 때 BACKLOG에 올릴지 판단한다.

## 진척 단계

PR 번호는 PR을 만들 때 채운다.

- [x] goal 초안 승인
- [x] 선행 시험 작성
- [x] 셸 분석 멈춤 구현
- [x] 셸 분석 독립 검증
- [x] 관문 판정 구현
- [>] 관문 판정 독립 검증(1회차 NOT PASS, 수정 회차 진행)
- [x] 셸 분석 PR 병합(PR311, `6a0aa453`)
- [ ] 관문 판정 PR 병합
- [ ] 종료 기록 정리
