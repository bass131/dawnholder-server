# Architecture 테스트 전체를 PR CI에서 돌리기

## 기준과 승인 출처

메인 `msg_6d21e59a5f0b`(2026-10-05T10:48:06Z)가 R-8 새 Architecture Astra에 전달한 다음 목표다. [이전 goal의 사전 결정](../2026-10-05-module-boundary-warning/goal.md#다음-goal-사전-결정)의 `msg_0937ac1fc5f3`와 사용자 병렬 착수 결정에 연결한다. 메인이 전달한 사용자 결정과 이 세션의 직접 사용자 입력을 구분한다. 범위 밖 개선은 다음 계획으로 남기고 현재 목표 달성을 우선한다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`
- branch: `ci/architecture-tests-20261005`
- 기준 main: `e6324907e716105b1256f2376f96998ad567b3ca`(fetch 후 새 branch). 이전 PR182·184 병합 뒤의 기준이다.
- 진입 시 main `.NET` [run37298450873](https://github.com/bass131/dawnholder-server/actions/runs/37298450873)은 진행 중이었다. 성공으로 인용하지 않는다.
- 근거 기본 경로: `.backups/verification/2026-10-05-architecture-tests-ci/`. 첫 메모 `astra-context.md`.

## 범위와 완료조건

| 항목 | 이번 목표 |
|---|---|
| 만들 것 | 새 `architecture-tests` PR workflow. `99_Tools/Architecture.Tests`의 `test_*.py` 전체를 수집·실행하고 수집 ID, 실제 실행, skip 이유, expected failure, 실패·오류와 exit를 구분한 원시 근거를 보존한다. |
| 건드릴 곳 | 신규 `.github/workflows/architecture-tests.yml`, 이 goal, CURRENT의 CodeMap 링크·branch. 독립 검증용 테스트/harness는 할당 근거 폴더에 보존한다. 기존 테스트 수정이 꼭 필요하면 전 실패 분류와 범위 판단 뒤 별도 계약을 발행한다. |
| 하지 않을 것 | 기존 `code-rules.yml`·`module-boundaries.yml` 수정, Architecture 제품/도구 재설계, 테스트 기대값 완화, opt-in·플랫폼·Git 제외 로컬 증거의 기존 계약 변경, 운영툴 vitest·DB 오프라인 PowerShell CI, DB·게임·Unity 실행, 전역 설정 변경, 승인 없는 병합. |
| 관찰 가능한 완료조건 | 실제 PR CI가 현재 checkout의 전체 suite를 발견하고 실행한다. 0건 수집·0건 실행·발견/준비 실패를 PASS로 만들지 않는다. 테스트 실패/오류가 job 실패로 전파되고 정상/실패/skip·미실행을 원시로 대조한다. stdout/stderr·결과·exit·환경/checkout 정보가 artifact로 남고 신규 독립 Opus가 구현과 실제 진입을 검증한다. |

전체 수집은 모든 opt-in과 모든 OS 환경을 강제로 제공한다는 뜻이 아니다. 기존 독립/실측 opt-in, Windows interop, Git 제외 증거가 필요한 테스트는 원래 조건과 skip 이유를 보존한다. 새 workflow의 Linux 일반 실행 범위와 기존 module-boundaries workflow의 별도 실행 범위를 구분하고 두 결과를 하나의 전수 실행으로 합산하지 않는다. 기대된 실패도 정상 통과에 더하지 않는다. Linux에서 실행할 수 있는 기존 기본 테스트를 임의 제외하지 않는다.

## 설계·분할과 검증

보존된 `.backups/verification/2026-10-03-codegraph-adapter-cleanup/deferred-ci/architecture-tests.yml`은 출발점이며 현재 경로·의존성·실패 전파를 다시 확인한다. 새 workflow를 분리하면 Rules 소유 `code-rules.yml`과 기존 모듈 검사 책임을 보존할 수 있다. 기존 workflow 확장 대안은 이 소유 경계를 넓히므로 선택하지 않는다. npm 설치가 필요해지면 먼저 Rules와 EBADENGINE 처리·소유권을 조율한다.

검증 등급은 **강**이다. CI 실행/I/O·오류 전파를 바꾸므로 독립 실사, 독립 테스트 작성/보완·실행, 변경의 실제 진입 실행이 필요하다. 구현 전 제품 diff는 0이며 실제 파일/줄 수는 구현 뒤 `git diff --numstat` 원시로 남긴다. R-7은 메인이 지정한 시범 목표가 아니므로 현재 기본 라우팅을 적용한다.

1. Astra가 승인 범위를 메인과 대조하고 계약·원문 규칙·경로 검사 근거를 고정한다.
2. 새 Sol `gpt-6.1-sol max`가 workflow 하나를 구현하고 자체 실행 원시와 쓰기 종료를 보고한다. 기존 테스트/제품은 읽기 전용이다.
3. 신규 Opus `claude-opus-5-5`가 요구사항→보고→diff/원시를 독립 대조하고 근거 폴더에 harness·판정을 쓴다. 제품 결함은 번호로 반환한다.
4. Astra가 결과를 통합·commit/push/PR 생성하고 실제 Actions 로그·artifact를 확보한다. 실제 PR CI가 필요한 판정은 그 원시까지 새 독립 세션에서 확인한다.
5. 메인에게 판정 원문·표본 근거·남은 한계를 전달한다. 해당 PR 사용자 승인 후에만 병합한다. 전체 결과 기록·Gardener·종료 점검과 R-8 뒤 다음 목표를 정한다.

같은 파일 동시 쓰기를 금지한다. Sol·검증자는 commit/push·추가 위임을 하지 않고 작업 하나 후 정산·종료한다. Astra만 이 branch의 Git을 맡는다. WSL·테스트·SDK 쓰기는 작업별 새 전용 경로에서 수행하고 공유 clone·DB·7777·Unity·전역 설정을 사용하지 않는다. 자체 점검과 독립 판정, 로컬 재생과 실제 GitHub Actions 실행은 각각 구분한다.

## PR 경계와 점검

PR1은 새 workflow와 진입/goal 기록이다. 제품·기존 검사 정책·Rules CI는 포함하지 않는다. 구현/검증 완료 뒤 PR 전 중간 점검에서 허용 diff·전수 수집/skip·실패 전파·보존 계약을 확인한다. PR의 실제 실행 증거와 판정 원문은 사용자 병합 판단 전에 확보한다. 종료 기록을 별도 PR로 내면 그 PR도 개별 사용자 승인을 받는다.

## 재개 지점

메인 `msg_7b2085ef0fec`(2026-10-05T10:58:02Z)는 이 범위를 사용자 사전 결정과 일치한다고 대조하고 구현자 발행을 지시했다. 전체 수집·기존 opt-in 유지·skip/expected failure 분리·0건/발견 오류 비통과에 동의했다. 메인의 범위 대조이며 새 사용자 직접 입력이나 병합 승인은 아니다. 원문은 근거 폴더의 `scope-wait-1.json`이다.

새 Run `run_3abaa3ef999c`, coordinator `term_b6c748f5-f8bd-4532-acb4-14df49bf8c79`다. 첫 독립 Opus의 NOT PASS 결함 #1을 새 Sol이 수정했고 신규 Opus의 로컬 강 재검증을 통과했다. 현재는 PR 발행·실제 CI 확인 단계이며 병합은 미수행이다. 실제 Actions 결과는 확보 후 별도 기록하고 로컬 통과와 구분한다. 이전 Run·Task·Dispatch를 재사용하지 않는다.

## 진척 단계

- [x] 범위·goal 확정
- [x] workflow 구현과 자체 점검 — 독립 결함 #1 수정 포함
- [x] 독립 재검증
- [>] PR 발행·실제 CI 확인
- [ ] 병합 승인·병합
- [ ] 결과 기록·종료

메인 `msg_f528e0003d13`(2026-10-05T11:20:40Z)의 현황판 진척 표기 요청을 Sol 쓰기 종료 뒤 다음 goal 버전에 반영했다. Sol 계약의 이전 goal/hash는 근거 폴더 `goal-sol-v1.md`로 보존하고 신규 검증에는 이 버전을 고정한다.

## 수행 결과

### 1 - 새 workflow 구현과 자체 실행

Sol Task `task_f5835c7fec97`/Dispatch `ctx_be61e219cb93`는 `msg_36c67a9cdcf9`(2026-10-05T11:24:08Z)로 succeeded·쓰기 종료를 보고했다. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 화면 GPT-6.1-Sol max, backend unknown이다. 실제 시작 receipt는 `sol-start.json`의 input_accepted/turn_started이고 완료 원문은 `implementation-wait-24.json`이다. 자체 점검을 독립 통과나 goal 전체 완료로 표현하지 않는다.

- 신규 `.github/workflows/architecture-tests.yml` 377줄(+377/-0), SHA256 `ab4137f89de6ba81b388f62c3727cbeefc7ccb49e11fe65fcb01ee8f569fb7ef`. PR path filter 없이 전체 discovery, workflow_dispatch, 원시 수집/실행·skip/expected failure 분리, 실패/불가 exit 전파와 항상 finalize/artifact를 연결했다. 기존 workflow/제품/tests는 변경하지 않았다. 실제 줄 수는 `implementation/new-workflow-numstat.txt`다.
- `implementation/report.md` SHA256 `4097205ccf5081b027c86edd553a7e556f2164b22376f2e981b0edaccfb4a8c5` 전체와 실제 baseline 결과·stderr를 Astra가 읽었다. 고정 입력·CURRENT 보존 보고는 `implementation/final-scope-audit.json`이고 Astra의 현재 Git 조회는 허용된 신규 workflow·goal과 CURRENT만 나타냈다.
- 실제 YAML run 블록을 `/tmp/architecture-ci-sol-tzl9cean/source`의 새 clone에서 Python3.14.4로 재생했다. `implementation/self-check-1/baseline/artifact/result.json`·stderr에서 **199수집, 85실행, 정상성공82, skip114, expected failure3, 실패/오류0**·exit0을 대조했다. 시작/실행 ID와 skip reason/trace는 그 원시에 있다. Bash 진입86.45730688799995초와 unittest86.263초는 서로 다른 측정 경계다. Windows exe PATH를 제외한 WSL Linux 기본 조건이며 GitHub Ubuntu 호스트 자체 실행은 아니다.
- 28개 자체 시나리오 결과는 `implementation/self-check-1/summary.json`과 각 observation에 있다. 준비/발견 오류·0건·test/fixture 실패·프로세스 조기 종료의 상태/exit를 구분했다는 구현 보고이며 신규 Opus가 독립 대조한다. 기존3개 expected failure는 원래 decorator를 유지했고 정상성공에 더하지 않았다. 기존 opt-in·Windows·로컬 증거 의존 skip은 보존했으며 타 CI 실적과 합산하지 않는다.
- `worker-release`의 external_terminal/retained 뒤 같은 incarnation `583a6472-c300-4da5-b72c-8506fc91248d`·idle·완료 화면·빈 prompt를 확인해 정확한 Sol pane을 close했다(ptyKilled true). `sol-release.json`·`sol-before-close.json`·`sol-close.json`이 정산 근거다. 신규 검증자에만 다음 작업을 발행한다.

진입 시 진행 중이던 main `.NET` run37298450873은 후속 조회에서 completed/success였다(`entry-main-ci-completed.json`). 새 workflow의 실행 결과와 구분한다. 독립 강 검증과 실제 GitHub checkout/setup/upload·PR CI·DB·Unity·게임 실행은 이 구현 자체 점검으로 확인되지 않았다.

### 2 - 첫 독립 검증과 결함 #1

Opus Task `task_789f0cef8c0f`/Dispatch `ctx_86ceb6e0214f`는 구현 Task `task_f5835c7fec97`을 검증하고 `msg_623870f55b2b`(2026-10-05T11:55:33Z)로 **NOT PASS**를 반환했다. worker_done의 succeeded는 검증 작업 완료이며 제품 통과가 아니다. 최초 실행 `claude --model claude-opus-5-5`, 화면 Opus5.5 xhigh, backend unknown이다. 판정 원문 `review/verdict.md` SHA256 `5125ac25497a9cabc368b4d79f00baaf69e5c4fd56a8342a571ec5d17e4b06f7` 전체를 Astra가 읽었다.

- 결함 #1(중간·차단): 재귀 `test_*.py` 목록에 나타난 비패키지 하위 폴더의 테스트를 unittest discovery가 수집하지 않아도 성공한다. `review/raw/run-2/scenarios/probe_nested_non_package/artifact/result.json`은 파일2개·수집1개·성공 exit0이며 하위 실패 테스트가 빠졌다. Astra가 원시를 직접 대조했다. 현재 HEAD의 테스트는 모두 최상위라 현행199건 누락은 없으나 전체 수집 보존 계약을 위반한다.
- 실제 고정 HEAD `57674ed2b0276123872594c959152a41d781d327`의 YAML 진입과 stock unittest·AST 목록을 독립 대조했다. **199수집, 85실행, 정상성공82, skip114, expected failure3, 실패/오류0**이었다. 독립 예상 시나리오26건과 탐색3건을 구분하며 탐색에서 위 결함이 발견됐으므로 전부 통과로 합산하지 않는다. 원시는 `review/raw/run-2/summary.json`·`real-suite/`·`stock-unittest/`·`static-enumeration.json`이다. 실제 GitHub checkout/setup/upload는 아직 미검증이다.
- 고정 입력79개 leaf hash의 변경0건을 Astra가 확인했다(`review-astra-source-check.json`). 검증자는 제품을 고치지 않았다. release 후 같은 incarnation `984dafea-6ae3-437e-a1ff-c2caac34607d`·idle·빈 prompt를 확인해 close했다(ptyKilled true). 정산은 `review-release.json`·`review-before-close.json`·`review-close.json`이고 판정 시 goal은 `goal-review-v1.md`에 보존했다.
- 동일 계약·결함 #1의 확정 실패는 **1회**다. 새 Sol에 발견 불가 파일의 경로·원인·해결 안내와 비통과 exit를 요구하며 파일 목록 축소·기존 테스트 수정·opt-in 확대는 하지 않는다. 같은 범위의 수정이므로 기존 승인 아래 진행한다. 첫 발생 교정은 workflow 발견 경계에서 수행하고 새 반복 운영 규칙을 추가하지 않는다.

### 3 - 결함 #1 수정과 자체 재실행

신규 Sol Task `task_7c1e21e71e3f`/Dispatch `ctx_6ee3baa8d8c3`는 `msg_a6dc1b5aebf0`(2026-10-05T12:32:02Z)로 수정·자체 실행 완료와 쓰기 종료를 보고했다. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 화면 GPT-6.1-Sol max, backend unknown이다. 보고 `repair-1/report.md` SHA256 `14508BFECF53359D46EE1D5AECAF1AB7F9DA64048E865A37433F32F21214D560` 전체와 실제 diff·원시 표본을 Astra가 읽었다.

- workflow만 +55/-1, 최종431줄·SHA256 `0C95CEEC57F69896BB432B1816B2DC33724543713F7230D917DF7935AB8B1828`. 실제 발견 파일을 추적하고 누락 파일의 경로·이유·수리 안내와 unavailable exit2를 남겼다. 빈 모듈·import skip·기존 runner 결과 구분은 보존한다. Python3.14.4 private discovery hook 연결은 향후 pin 변경 시 재검증할 제약이다.
- `repair-1/raw/run-3/`의 수정 전후 비패키지 fixture에서 exit0→2, 패키지 실패에서 exit1을 Astra가 직접 대조했다. 실제 파싱한 YAML 본문의 기존 suite는 **12파일 발견, 199수집, 85실행, 정상성공82, skip114, expected failure3, 실패/오류0**이며 tests 진입81.551906962초다. 19개 자체 시나리오와 명시 추가 대조73개는 자체 점검이며 독립 판정이 아니다. checkout/setup/upload는 로컬 대역이고 실제 PR CI는 남았다.
- 보호 입력83개 중 수정 workflow를 제외한82개를 Sol이 확인했고, Astra는 그중81개 leaf hash 불일치0을 확인했다(`repair-1-astra-source-check.json`). 수정 전 goal은 `goal-repair-1-input.md`로 보존했다. release 뒤 같은 incarnation `c5875997-65c8-43ae-9e02-95e3e7cc8719`·idle·빈 prompt를 확인해 close했다(ptyKilled true). 정산 원시는 `repair-1-release.json`·`repair-1-before-close.json`·`repair-1-close.json`이다.
- 최초 Git 읽기2건은 **GIT_OPTIONAL_LOCKS=0 없이 실행됨**: batch12:04:08.883Z의 `git rev-parse HEAD`·`git status --short`, 출력 HEAD57674ed와 기존 goal 수정, exit0. 개별 subprocess 시각은 미계측이다. 이후 적용 batch는12:05:02.321Z의 branch 조회이며 원문은 `repair-1/git-initial-calls.jsonl`이다. 메인 `msg_d502a1d3b49c`(`repair-1-wait-2.json`)가 기록·이후 준수·새 Opus 원시 대조로 처리하도록 결정했고, 같은 Sol 재발에는 적용하지 않는다. 성공/무해로 단정하지 않으며 새 검증 계약에 두 원문과 후속 감사 요구를 포함한다.
- 자체 harness run1은 WSL의 Windows worktree Git 경로 준비 실패(exit128), run2는 YAML chomping과 추출 문자열 끝 줄바꿈 비교로 테스트 진입 전 중단했다. 원시를 보존한 run3에서 실제 파싱 YAML을 실행했다. 동일 harness 총4버전 체크포인트 `msg_2206557bfe8c`를 메인에게 `msg_9f86e4fa31af`로 전달했다. 제품 확정 실패 횟수와 구분하며 결함 #1의 확정 NOT PASS는 여전히1회다.

### 4 - 독립 재검증 통과와 PR 준비

신규 Opus Task `task_3a37647bbb27`/Dispatch `ctx_f9bedf6ffe3d`는 수정 Task `task_7c1e21e71e3f`를 검증하고 `msg_215bc166a0ec`(2026-10-05T12:56:08Z)로 **로컬 강 검증 통과·결함 #1 해소·새 차단 결함 없음**을 보고했다. 최초 명령 `claude --model claude-opus-5-5`, 화면 Opus5.5 xhigh, backend unknown이다. 원문 `review-2/verdict.md` SHA256 `8A18AAD1FEA3218DD5B1765EE82EDA22F203AA944966D73EBB4D3B34341BDA60` 전체를 Astra가 읽었다.

- 검증 HEAD는 최신 main `e9c78a0f`의 종료 기록을 통합한 `e9b704fcf3e737addf9bc79353aec5033d058266`이다. CURRENT의 인접 행 충돌은 CodeMap 새 goal과 main의 Management 종료 링크를 모두 보존했다. 제품 변경은 workflow 한 파일이며 main 대비 CURRENT 2줄 교체와 이 goal만 더해진다. 통합 원시는 `review-2-integration.json`이다.
- 고정 HEAD의 커밋된 YAML을 새 WSL depth-1 clone에서 실행했다. **12파일 발견, 199수집, 85실행, 정상성공82, skip114, expected failure3, 실패/오류0**을 stock unittest·AST·git ls-files와 ID 단위로 독립 대조했고 실제 suite 대조30개가 일치했다. 보존26·수정경계15개 fixture는 기대대로였고 probe3개는 관찰로 구분한다. 원시는 `review-2/raw/run-1/`이며 tests 단계85.66초는 로컬 측정이다.
- Astra는 결함 #1의 unavailable exit2·counts null·누락경로/이유/수리 안내, 실제 suite result와 대조30개, Git 감사의 최초 미설정2건·이후 shell/WSL 재발0을 원시에서 확인했다. 보호635개 leaf와 외부 Sol 원천 hash 불일치0, HEAD·빈 status를 확인했다(`review-2-astra-source-check.json`). index stat 갱신 여부는 사후 관측 불가다. 메인 편차 처리 조건을 독립 검증했으며 무해로 단정하지 않는다.
- release 뒤 같은 incarnation `4c784277-fc13-40c7-9296-ada9063d642d`·idle·완료화면·빈 prompt를 확인해 close했다(ptyKilled true). 근거는 `review-2-release.json`·`review-2-before-close.json`·`review-2-close-screen.json`·`review-2-close.json`이다. 검증 입력 goal은 `goal-review-2-input.md`에 보존했다.
- 실제 GitHub 호스트·checkout/setup-python/upload-artifact·PR merge checkout과 원격 artifact는 아직 미실행이다. 로컬 강 통과를 실제 PR CI나 전체 goal 완료로 표현하지 않는다. 신규 PR을 발행한 뒤 그 원시와 현재 workflow 동일성을 새 독립 Opus가 확인한다.

## 다음 계획 후보

범위 밖 개선은 [BACKLOG](../../../00_Document/operations/BACKLOG.md) 기존 기능 테스트 CI 후보와 연결하고 이번 workflow에 선행 구현하지 않는다. 운영툴 vitest·DB 오프라인 PowerShell·새 폴더 보존 검사·Windows 경로 예산 검사도 현재 목표 밖이다.

첫 검증의 비차단 관찰 O1~O8은 `review/verdict.md`에 보존한다. expected-failure만 있는 실행 정책, Python pin 중복, exit 상수 가독성, inline runner 책임 분리, 초기화/마감 중복, 기존 sqlite ResourceWarning, 자체 점검 중복 assertion, README/BACKLOG의 CI 인지는 후속 후보이며 결함 #1 수정에 포함하지 않는다.

재검증 N-1~N-3은 `review-2/verdict.md`의 비차단 관찰이다. package `__init__.py`의 SkipTest로 하위 파일이 발견되지 않으면 exit2가 되는 N-1은 현재 suite 영향0이고 전체 파일 발견 불가를 비통과로 처리하는 이번 계약에 부합한다. Astra는 현재 동작을 수용하고 향후 package 단위 skip 도입 시 별도 분류·안내를 검토할 후보로 둔다. 잘못된 모듈명/load_tests/skip의 원인 안내 세분화(N-2), private Python hook과 pin 변경 시 재검증 의무의 주석 보강(N-3)도 현재 제품 변경으로 확대하지 않는다.
