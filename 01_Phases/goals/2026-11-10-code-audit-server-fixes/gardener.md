# 전체 코드 감사 서버 묶음 종료 Gardener

## 작업 전 맥락 메모

- 역할·태그: 종료 Gardener `[Core Gardener Opus]`. 계약 v1, Task `task_15e44ff057f8`, Dispatch `ctx_eae85eabd012`. 지정 `claude-opus-5-5` high, backend unknown.
- 계약: `.backups/verification/2026-11-10-code-audit-server-fixes/closeout/gardener/contract.md`, SHA256 `d15d1997945f2a2aebf8addb27b1deb7cb2521eb50c0bcd3f9ebedd6d5f480ae`. 읽기 전 대조 일치.
- 계약 보충: 근거 폴더의 `closeout/gardener/context.md` 한 파일을 추가로 허용받았다(리드 `msg_00e75ece86ec`). 쓰기 가드가 그 위치의 메모를 먼저 요구하기 때문이다. 이 절은 그 메모의 요약이다.
- 기준: `core-active` · `docs/core-audit-closeout-20261110` · HEAD `ac9317201d3c6ee74f5dc40463aaed72c28b1f29`, 시작 시 미커밋 변경 0건.
- 메모 시각: `date -u` 출력 2026-11-10T10:28:23Z. 입력 읽기(10:19~10:27Z)가 메모보다 앞섰고, 그동안 파일 쓰기는 없었다.
- 읽을 범위: 계약 입력 표. 판정문은 필요한 절만 좁혀 읽는다.
- 적용 규칙: 계약에 붙은 CODE_CONVENTION 「역할별 적용」(commit `acd5a7db065b37baa7f491c4502002eb8ce184a3`, 파일 SHA256 `2e233bdacbf55b22d3c7796313766630c4c1c4ddfbc5fdf68d81cc6e848f20d0`), ORCA 「목표 종료 Gardener」, AGENTS 역할과 공학 조건.
- 판정 경계: 이 문서의 내용은 제안이다. 채택은 메인을 거쳐 사용자가 정한다.

## 결론

새 정리 후보는 **없음(0개)**이다. 반복이 원시로 확인된 두 묶음은 이미 BACKLOG에 있어 계약대로 새 후보로 세지 않고, 기존 행에 관측을 잇도록 제안한다.

goal 기록과 근거가 어긋난 곳은 3건이고 모두 비차단이다. 이번 범위의 경고 억제·설정 완화 추가는 아래 검색식으로 0건이다.

## 기존 행 연결(새 후보로 세지 않음)

### `verification-harness-powershell-helper`(BACKLOG:30) — 세션별 실행 harness 재작성

그 행은 검증·실행용 PowerShell harness를 세션마다 새로 짜는 문제와 helper·fixture 경계를 이미 담고 있다. 행은 앞 2,600자까지만 읽었다. Unity 쪽의 같은 부류는 `unity-baseline-harness-reuse`(BACKLOG:229)이고, 이 goal은 서버 WSL 단계의 관측을 더한다.

| 항목 | 이 goal의 관측 |
|---|---|
| 빈도 | 14개 폴더에 단계 실행기 14개가 있다. 이름별로 `run-stage.ps1` 7, `Invoke-Stage.ps1` 2, `Invoke-TestStage.ps1` 2, `run-step.ps1` 2, `Run-ApprovedStep.ps1` 1이고 합계 916줄이다. |
| 내용 차이 | `run-stage.ps1` 7개는 hash가 전부 다르다. 봇 로그 복사 스크립트(`Copy-BotLogs.ps1`·`copy-bot-logs.ps1`) 8개는 hash 7종이다. |
| 내용 확인 범위 | 읽은 표본은 `pr4/verify-3/harness/run-step.ps1`과 `gaps/verify/harness/run-stage.ps1`(앞 45줄) 2개다. 공통 책임은 기존 시도 거부, 단계별 `wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh` 인자 조립, `format-in-clone.sh`의 경로·hash 대조, `date.exe -u` 시각, Unity 프로세스 관문이다. 나머지 12개는 이름과 hash만 봤고 내용은 열지 않았다. |
| 관측된 비용 1 | `gaps/verify/verdict.md:139`: 집계 스크립트가 2회 실패했다(빈 로그의 null 처리, 실행 중 서식 로그의 공유 잠금). 판정 결과는 바뀌지 않았다. |
| 관측된 비용 2 | `pr4/db-2/report.md:89-92`: `sync-wsl.sh path`를 직접 부른 봇 DLL 존재 조회가 2회 실패했다. 첫 번째는 Git Bash 경로 변환이고, 두 번째는 `wsl --`가 `$R`을 바깥 셸에서 먼저 푼 것으로 보고자가 추정한다. 두 번째 원시는 덮여 원인을 확정하지 못했다. 세 번째 `--exec` 호출로 확인했고 봇·서버 실행에는 영향이 없었다. 원인 추정은 BACKLOG:30의 native 인자 소실 기록, BACKLOG:285 `wsl-entry-argument-split`과 같은 부류다. |
| 관측된 비용 3 | `pr1/verify/verdict.md:77`: 실행기 v1을 교체한 흔적(`tmp/Invoke-Stage-v1.ps1`)이 있다. 교체 이유는 열어 보지 않았다. |
| 제안 검사 | **정본 helper와 fixture 시험.** 행의 helper에 WSL 단계 입력을 포함한다. fixture는 가짜 `wsl`로 네 가지를 확인한다: 기존 시도 파일이 있으면 자식 실행 0회, 관문 실패면 자식 실행 0회, 자식 exit를 그대로 전달, 원시·exit 파일 생성. |
| 한계 | 이 goal에서 틀린 판정이나 누락을 낸 증거는 없다. 이득은 세션별 작성 비용의 절감이고, 크기는 측정하지 않았다. |

### `prepush-ci-format-checks`(BACKLOG:396) — PR306 C# 공백 4곳 CI 실패

행의 기록은 PR272를 서로 다른 PR 네 번째로 세므로, 행의 기록 기준으로 PR306은 다섯 번째 PR이다.

- 원인 기록은 `goal.md:269`와 `lead-context.md:59-62`다. 리드가 검증 계약에서 서식 검사를 「Windows SDK 없음」으로 미실행 처리했고 WSL 경로를 확인하지 않았다. 실패한 CI run의 로그는 근거 폴더에서 찾지 못했다. `pr2/format/lead/ci-watch.log`는 고친 뒤의 run으로 `error WHITESPACE` 검색 0건이다.
- 이 goal이 더하는 정보는 하나다. 정본 진입점 `format-check.ps1`·`format-check.sh`는 있지만, Windows에 SDK 10.0.301이 없는 이 머신의 worktree에서 실제로 쓴 경로는 근거 폴더의 `lead/harness/format-in-clone.sh`다. `99_Tools/`에 같은 도구가 없고 BACKLOG 검색도 0건이다. 스크립트는 `/mnt/c/Dev/DawnHolder_Project/.git`을 고정했고(`format-in-clone.sh:28`), 표본 실행기 2개가 경로와 hash를 박아 둔다.
- 제안 검사: 행의 「기존 검사 진입점 묶음」에 이 도구를 **정본 helper**로 승격해 넣는다. fixture는 공백 오류를 넣은 overlay는 exit 0이 아니고, 깨끗한 commit은 exit 0이며, 끝나면 복제 폴더가 남지 않음을 본다. 공통 Git 경로는 고정값 대신 계산한다.
- 「Windows 진입점은 쓸 수 없다」는 `goal.md:178`의 리드 기록이다. 이번 세션은 실행하지 않아 **미실행**이다.

## 검토했으나 후보가 아닌 것

| 관측 | 근거 | 처리 |
|---|---|---|
| 봇 기준선 실패 `DashSmoke`·`BossFightSmoke` | `goal.md:386`, BACKLOG:552 | 겹침 |
| HUD 렌더 시험의 캡처 전제 실패 5건 | `goal.md:391`, BACKLOG:557 | 겹침 |
| Unity 자동 drift(`MinimapRT.renderTexture`, `ProjectSettings.asset`) | `goal.md:348`, BACKLOG:223·327 | 겹침 |
| 사망 상태 이동 미잠금 | `goal.md:385`, BACKLOG:551 | 겹침. 아래 기록 어긋남 1번 참조 |
| PR4-01 빈 통지가 초대 팝업을 닫음 | `pr4/verify/verdict.md:3`, `:87` | 1회. 서버 회귀 시험으로 이미 전환했다. 클라이언트 잔여는 `goal.md:388`과 겹친다 |
| 리드 계약과 실제의 어긋남 4종 | `goal.md:230`(사망), `goal.md:267`(핸들러 22/21), `lead-context.md:62`(서식 미실행), 리드 `msg_00e75ece86ec`(이 세션의 메모 위치와 쓰기 가드) | 종류가 서로 달라 단일 검사로 묶이지 않는다. `contract-context-check`(BACKLOG:129)에 연결할지는 리드 판단이다. 그 행은 앞부분만 읽었다 |
| Codex 모델 거부 | `pr4/db/lead/first-run-note.md:12` | 1회, 원인 미확정, 외부 서비스 응답이라 저장소 검사 대상이 아니다 |

## 검사 실행 불가와 실제 위반

**실행 불가·생략(위반 아님):**

- 정식 서식 미실행: PR1 구현(`goal.md:240`), PR1 검증(`pr1/verify/verdict.md:88`), PR2 검증(`pr2/verify/verdict.md:163`). 사유는 Windows SDK 부재로 적혔다.
- 정식 서식 생략: 게임 규칙 첫 검증 단계 7(`pr4/verify/verdict.md:14`, `:50`, 리드 보충 `msg_60dac113b8ab`).
- 이후 실행은 `goal.md:287`, `:299`, `:328`에서 exit 0으로 기록됐다.

**실제 위반:**

- PR306 공백 4곳의 CI 서식 실패(`goal.md:269`). 고침 커밋 `5f5a23ab`까지 기록돼 있다.
- PR1 검증자의 생존 신호 간격 357초(`pr1/verify/verdict.md:108`). 5분을 넘었고 판정문이 스스로 적었다. 제품 판정에는 영향이 없다.

## 경고 억제·설정 완화 점검

범위는 `acd5a7db..ac931720`이다. 변경은 54파일이다.

- `git diff --stat acd5a7db ac931720 -- '*.csproj' '*.props' '*.targets' '.editorconfig' '*.ruleset' '.github' 'global.json' '99_Tools'` 출력 0줄.
- `git diff -U0 acd5a7db ac931720 -- '*.cs' '*.csproj' '*.props' '.editorconfig'`에 `grep -nE '^\+.*(pragma warning disable|SuppressMessage|NoWarn|Skip *=|\[Trait|TreatWarningsAsErrors|dotnet_diagnostic|TODO|HACK)'` 출력 0줄.
- 이 0건은 위 경로와 낱말에 한정된다. 빌드 경고 80은 판정문들 사이에서 같은 값일 뿐이다(`pr2/verify/verdict.md:158`, `pr3/verify/verdict.md:119`, `pr4/verify-3/verdict.md:37`). 수정 전 기준선 수는 열지 않아 「새 경고 없음」을 주장하지 않는다.

## goal 기록과 근거의 어긋남

모두 **비차단**이다. 고치지 않았다.

| file:line | 문제 | 근거 |
|---|---|---|
| `goal.md:385`, `BACKLOG.md:551` | 「`PlayerPhysicsSystem` 주석은 Death도 잠근다고 적었다」는 HEAD에서 사실이 아니다. 주석은 이 goal에서 `Attack/Hit`로 바뀌었다. 사망 미잠금 관측 자체는 유효하다 | commit `1619297f`의 diff, `PlayerPhysicsSystem.cs:40`. 재정의는 `PlayerCombatStates.cs:30`, `:63` 두 곳이고 기본값은 `ActorState.cs:18`의 false다 |
| `pr4/db/lead/first-run-note.md:21` | 다음 계획 후보로 넣겠다고 적은 「전원 차단 뒤 재검증 세션이 되살아난 사례(`term_92692c88…`)」가 goal 「다음 계획 후보」와 BACKLOG에 없다. 의도한 제외인지는 알 수 없다 | `goal.md`에서 `92692c88`·`되살` 검색 0건. `BACKLOG.md`에서 `92692c88`·`되살`·`부활`·`살아난` 각 0건, `재출현` 2건은 게임 몬스터 재출현 행(53, 283)이다 |
| `goal.md:378` | 「스택 PR base 자동 이동은 PR305 병합 뒤 확인했다(「입력 예산 결과」)」인데, 그 절의 base 관측은 PR306 병합 문단의 PR307 base뿐이다. PR305 병합 뒤 관측은 goal에 적혀 있지 않다. GitHub 실제 이력은 조회하지 않았다 | `goal.md:277`, `:157` |

**대조 일치:** 보고서 SHA256 4건(`pr4/unity/report.md`, `pr4/db-2/report.md`, `pr4/verify-3/verdict.md`, `pr4/db/card-v1.1.md`), 서식 도구 hash(표본 실행기의 고정값과 실제 파일), S2·S5 종료 시각, S3→S4 14초, `HudPanelSizingRenderTests`의 11-07 첫 commit, `03_Client`·`98_Shared` diff 0, `DAWNHOLDER_GUILD_SIZE_CAPTURE_DIR`의 DEVELOPMENT 미등재, goal의 시험 집계 산술(실패+통과+건너뜀=전체), Unity 집계(`pr4/unity/report.md:21-26`의 current 행과 `goal.md:342-343`).

## 미실행

이번 세션은 빌드·시험·봇·Unity·DB·서식 검사와 `sync-wsl.sh`를 실행하지 않았다. 위의 도구 동작과 실행 결과는 코드 읽기와 기존 기록에 근거한다. BACKLOG 전체는 읽지 않았고 검색한 행만 봤다.

## 실제로 읽은 파일과 절

`<E>`는 `.backups/verification/2026-11-10-code-audit-server-fixes`다.

| 파일 | 읽은 범위 |
|---|---|
| `<E>/closeout/gardener/contract.md` | 전체 |
| `goal.md` | 213~393행, 157·178행, 검색 |
| `<E>/pr1/verify/verdict.md` | 48~57행, 검색 일치 줄 |
| `<E>/pr2/verify/verdict.md` | 141~146행, 검색 일치 줄 |
| `<E>/pr3/verify/verdict.md` | 76~84행, 검색 일치 줄 |
| `<E>/gaps/verify/verdict.md` | 92~100행, 검색 일치 줄 |
| `<E>/pr4/verify/verdict.md`, `<E>/pr4/verify-3/verdict.md` | 검색 일치 줄(70줄 상한) |
| `<E>/pr4/unity/report.md` | 19~26행, 검색 일치 줄(60줄 상한, db-2와 합산) |
| `<E>/pr4/db-2/report.md` | 87~92행, 검색 일치 줄 |
| `<E>/pr4/db/lead/first-run-note.md`, `<E>/lead-context.md`, `<E>/lead/harness/format-in-clone.sh` | 전체 |
| `00_Document/operations/ORCA.md` | 「목표 종료 Gardener」 |
| `AGENTS.md` | 세션 시작 때 주어진 전체 |

입력 표 밖에서 읽은 것과 이유:

| 대상 | 이유 |
|---|---|
| `00_Document/operations/BACKLOG.md` 검색, 229·285·396행 전체, 30행 앞 2,600자, 551행 일부 | 겹침 판정 |
| `00_Document/operations/DEVELOPMENT.md` 143행과 검색 | 서식 진입점 확인 |
| `99_Tools/format-check.sh`, `99_Tools/sync-wsl.sh` 검색, `99_Tools` 목록 | 서식 경로가 정본에 있는지 확인 |
| `<E>` 아래 harness 폴더·파일 목록과 hash, 표본 스크립트 2개 | 기존 행에 잇는 빈도 |
| `<E>/tdd/report.md` 검색 | 사망 관측 대조 |
| `<E>/pr2/format/` 목록·검색 | PR306 원시 확인 |
| `PlayerPhysicsSystem.cs:38-42`, `HudPanelSizingRenderTests.cs:298`, `LocksMovement` 검색 | 기록 대조 |
| `git diff`·`git log`(`acd5a7db..ac931720`) | 억제·완화 점검과 주석 이력 |
