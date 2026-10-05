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

새 Run `run_3abaa3ef999c`, coordinator `term_b6c748f5-f8bd-4532-acb4-14df49bf8c79`다. 아래 Sol 구현과 자체 점검·쓰기 종료·정산까지 끝났으며, 다음은 신규 Opus의 독립 강 검증이다. 실제 PR CI·PR 발행·병합은 아직 수행하지 않았다. 이전 Run·Task·Dispatch를 재사용하지 않는다.

## 진척 단계

- [x] 범위·goal 확정
- [x] workflow 구현과 자체 점검
- [>] 독립 검증
- [ ] PR 발행·실제 CI 확인
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

## 다음 계획 후보

범위 밖 개선은 [BACKLOG](../../../00_Document/operations/BACKLOG.md) 기존 기능 테스트 CI 후보와 연결하고 이번 workflow에 선행 구현하지 않는다. 운영툴 vitest·DB 오프라인 PowerShell·새 폴더 보존 검사·Windows 경로 예산 검사도 현재 목표 밖이다.
