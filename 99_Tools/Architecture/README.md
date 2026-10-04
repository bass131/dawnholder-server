# Architecture 추출 도구

동결한 소스 입력에서 구조 snapshot과 비교 근거를 만든다. 기본 추출기는 Roslyn이며 CodeGraph와 두 도구 비교는 명시적으로 선택한다. 현재 HEAD를 새로 수집하는 도구는 아니다. 분석 상태와 명령 실행 상태를 별도로 기록한다.

## 실행 전제

아래 Windows 명령은 저장소 루트의 PowerShell 7.6.6(`pwsh`)을 기준으로 한다. WSL Ubuntu, Python 3, Git, rsync, flock 및 [comparison-settings.json](comparison-settings.json)이 가리키는 동결 manifest·freeze-record·외부 참조가 필요하다. Roslyn과 Compare는 `global.json`의 SDK 10.0.301을 설정의 `dotnetRelativePath`에 준비해야 한다. CodeGraph 단독은 Roslyn restore/추출을 실행하지 않는다.

동결 manifest의 sourceCommit과 입력 hash가 다르면 중단한다. 새 clone에는 Git 제외인 freeze-record와 동결 입력의 외부 DLL 참조까지 별도로 제공해야 한다. 준비 명령은 선택한 입력·도구를 소유한 WSL runtime으로 복사하고, 측정은 그 사본에서 빌드·추출한다. 서버·DB·Unity 플레이를 실행하는 명령은 아니다. [실행 전제와 부작용](../../00_Document/operations/DEVELOPMENT.md)을 함께 확인한다.

## 선택과 실행

| `-Extractor` | 실행 | 생략 시 근거 위치 |
|---|---|---|
| `Roslyn` (기본) | Roslyn cold + warm 3회 | `.backups/architecture/roslyn` |
| `CodeGraph` | CodeGraph cold + warm 3회 | `.backups/architecture/codegraph` |
| `Compare` | CodeGraph 4회 뒤 Roslyn 4회 | `.backups/architecture/compare` |

`path`는 runtime 위치 조회, `prepare`는 선택한 입력 준비, `measure`는 실제 추출·정규화·채점, `check`는 해당 선택의 저장 결과 검사다. `prepare` 성공은 추출 완료를 뜻하지 않는다.

```powershell
# 기본 Roslyn: CodeGraph·Node·npm 설치가 필요하지 않다.
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action prepare
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action measure
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action check

# CodeGraph만 실행
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action prepare -Extractor CodeGraph
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action measure -Extractor CodeGraph
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action check -Extractor CodeGraph

# 새 근거 위치에서 양쪽 비교
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action prepare -Extractor Compare -EvidencePath .backups/architecture/manual-compare
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action measure -Extractor Compare -EvidencePath .backups/architecture/manual-compare
pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action check -Extractor Compare -EvidencePath .backups/architecture/manual-compare
```

같은 실행의 세 명령에는 같은 선택과 `-EvidencePath`를 사용한다. 경로는 저장소 상대·Git 제외 `.backups/` 아래여야 하며 다른 소유자·symlink 및 소유 표식 없는 과거 비교 근거와 겹치면 거부한다. mode와 근거 경로마다 runtime과 잠금을 분리한다. 같은 owner의 근거 root는 재사용 가능하고 measure 성공 시 latest-run을 갱신한다. 비교 테스트의 기준으로 채택한 settings의 `evidencePath`는 재측정에 재사용하지 말고 다른 빈 경로를 선택한다. 이는 기준 batch를 유지하기 위한 운영 지침이며 자동 봉인 기능은 아니다.
검증 기준으로 채택한 batch root에서는 `check`도 재실행하지 않는다. 기존 재채점이 `scoring/command.json`·`time.txt`를 다시 쓰므로 이후 실행에는 새 근거 경로를 사용한다.

WSL 진입은 `bash 99_Tools/Architecture/run-wsl.sh <action> --extractor roslyn|codegraph|compare --evidence .backups/<name>`이다. 최초 준비에는 PowerShell이 수집하는 frozen Git metadata가 필요하므로 위 PowerShell 진입을 사용한다.

## 실패와 복구

선택한 근거 폴더의 `execution-result.json`에서 `executionStatus`, `reasonCode`, `message`, `repair`, `retryCommand`를 확인한다. 시도별 기록은 `attempts/`, 실제 명령·종료값·시간·출력은 batch의 `command.json`과 stdout/stderr에 남는다. 안전한 근거 경로나 잠금을 확보하지 못한 초기 단계는 stderr로 이유를 알린다. 매번 프로세스 종료값과 이번 명령의 출력을 먼저 확인한다. 초기 실패로 결과 파일이 갱신되지 않았다면 이전 completed 기록을 이번 성공으로 해석하지 않는다.

| 실행 상태 | 의미 |
|---|---|
| `completed`, exit 0 | 선택한 명령 완료. `analysisStatus=partial`과 diagnostics가 있으면 그대로 남는다. |
| `unavailable`, nonzero | 필요한 설치·입력·실행 도구가 없거나 시작할 수 없음. |
| `failed`, nonzero | 시작한 명령·검증·정규화가 실패함. 기존 성공 batch를 이번 성공으로 간주하지 않음. |

CodeGraph는 저장소의 고정 package/lock에 맞는 1.6.1 Linux x64 bundle, bundled Node, C# grammar와 `syntax-context.cjs`가 필요하다. 도구가 없을 때 자동 설치나 다른 추출기로 대체하지 않는다. 설치가 필요하면 기존 설치본/cache를 보존한 별도 checkout에서 [install-codegraph.ps1](install-codegraph.ps1)을 사용한다. 이 설치 스크립트는 해당 checkout의 package-lock·node_modules·npm cache와 settings의 `evidencePath/install`을 쓰므로 보존 근거가 있는 원본에서 복구 목적으로 반복 실행하지 않는다. 전역 npm/PATH 변경이나 cache 삭제는 필요하지 않다.

잠금 충돌은 해당 실행이 끝난 뒤 재시도한다. 소유자나 경로가 다르면 새 빈 근거 경로를 선택한다. 입력 hash 오류는 동결 입력의 실제 사본을 확인하고, 도구 실패는 command와 원시 stderr부터 확인한다.

## 테스트와 과거 결과

설치에 의존하지 않는 기본 Python 테스트는 WSL에서 다음과 같이 실행한다.
SDK 공개 진입을 사용하는 모듈 경계 요구사항 suite는
`MODULE_BOUNDARIES_TEST_WORK`가 없으면 사유와 함께 skip한다. 기본 discovery는
이 suite의 import/discovery/setUp에서 외부 명령과 출력 쓰기가 없는지도 검사한다.
독립 모듈 경계 suite는 별도 `MODULE_BOUNDARIES_INDEPENDENT_WORK` opt-in을 유지한다.

```powershell
wsl -d Ubuntu -- python3 -B -m unittest discover -s 99_Tools/Architecture.Tests -t 99_Tools/Architecture.Tests -p 'test_*.py' -v
```

로컬 비교 근거가 있으면 기본 suite는 settings의 `evidencePath/latest-run.json`이 가리키는 batch와 현재 실행 코드를 대조한다. 근거 없는 새 clone에서는 해당 재생 테스트가 skip되므로 사유와 실행 건수를 확인해야 한다. 전체 테스트의 성공은 새 추출 실행이나 Unity·DB 검증 완료를 뜻하지 않는다.

기본 대조는 줄바꿈을 포함한 실제 bytes를 검사한다. 새 검증 batch `20261004T053719804684Z`의 `Directory.Build.props`와 `Roslyn/Architecture.Roslyn.csproj`는 LF지만 Git 속성은 `eol=crlf`다. 이 두 파일을 다시 checkout해 CRLF가 되면 기본 hash 대조가 실패한다. 저장소 루트에서 다음 명령의 `w/lf` 또는 `w/crlf`와 `attr/text eol=crlf`를 확인한다. 비교 자료나 파일을 검사 통과 목적으로 바꾸지 않는다.

```powershell
git ls-files --eol -- 99_Tools/Architecture/Directory.Build.props 99_Tools/Architecture/Roslyn/Architecture.Roslyn.csproj
```

이 batch를 당시 실제 bytes로 재생하려면 보존된 runtime의 `tool`을 명시한다. 다음 예시는 해당 로컬 보존 자료가 있는 WSL 저장소 루트에서 실행하며, 새 추출이나 `check`를 실행하지 않는다.

```bash
ARCHITECTURE_EVIDENCE_BATCH="$PWD/.backups/verification/2026-10-03-codegraph-adapter-cleanup/behavior-fix-8-verification/compare-execution/runs/20261004T053719804684Z" \
ARCHITECTURE_EVIDENCE_TOOL_ROOT=/home/bass1/.cache/dawnholder/architecture/ff3952212f2c45d509f5-compare-83c93998c4af/tool \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests -t 99_Tools/Architecture.Tests -p 'test_final_batch_replay.py' -v
```
과거 batch는 `ARCHITECTURE_EVIDENCE_BATCH`에 절대 경로 `<근거 root>/runs/<batch>`를 지정한다. 이때 당시 실제 실행 도구를 다음 중 하나 이상으로 명시해야 한다. 기본 실행은 이 변수들 없이 현재 저장소 코드를 검사한다.

| 환경변수 | 과거 실행과 대조할 실제 바이트 |
|---|---|
| `ARCHITECTURE_EVIDENCE_TOOL_COMMIT` | 해당 commit의 `99_Tools/Architecture` raw Git blob. checkout의 개행 변환을 적용하지 않는다. |
| `ARCHITECTURE_EVIDENCE_TOOL_ROOT` | 당시 실행한 도구 사본 디렉터리. 기록에 대응하는 파일의 실제 hash를 읽는다. |

명시한 batch에 도구 출처를 지정하지 않거나 다른 코드가 들어 있으면 실패한다. 기록의 `implementationHead`만 믿거나 기록 hash끼리 비교하지 않는다. 당시 코드가 미커밋이었을 수도 있으므로 실제 바이트의 일치가 기준이다.

서버 모듈 경계 도구도 `99_Tools/Architecture`의 전체 도구 집합에 포함된다.
이 도구가 없던 과거 로컬 batch와 현재 도구를 기본 명령으로 대조하면 파일 집합이
달라 실패한다. 과거 실행을 검증할 때는 당시 bytes를 위 환경변수로 명시한다.
이 명시적 과거 재생의 통과는 현재 전체 suite의 통과를 뜻하지 않는다.

이전 검증 batch `20261003T170218569325Z`도 보존된 도구 사본으로 재생할 수 있다.

```bash
ARCHITECTURE_EVIDENCE_BATCH="$PWD/.backups/verification/2026-10-03-codegraph-adapter-cleanup/behavior-fix-1-verification/compare-execution/runs/20261003T170218569325Z" \
ARCHITECTURE_EVIDENCE_TOOL_ROOT=/home/bass1/.cache/dawnholder/architecture/ff3952212f2c45d509f5-compare-dd085a73442a/tool \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests -t 99_Tools/Architecture.Tests -p 'test_final_batch_replay.py' -v
```

다음은 로컬 보존 자료가 있는 checkout에서 확인한 과거 최종 batch와 commit의 조합이다. **WSL의 저장소 루트**에서 실행한다. 이 환경변수는 해당 명령에만 적용된다.

```bash
ARCHITECTURE_EVIDENCE_BATCH="$PWD/.backups/verification/2026-10-02-architecture-extractor-comparison/implementation/runs/20261002T065831395290Z" \
ARCHITECTURE_EVIDENCE_TOOL_COMMIT=d0dffd1feb6082c3ddeb50fed359e2cc94886bea \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests -t 99_Tools/Architecture.Tests -p 'test_final_batch_replay.py' -v
```

이 예시는 다른 과거 batch의 일치까지 보장하지 않는다. 다른 batch에는 그 실행 기록과 실제로 일치하는 commit 또는 보존 도구 사본이 필요하다. 과거 `latest-run.json`·batch·동결 답안을 현재 코드에 맞춰 수정하지 않는다.

## 현재 서버 모듈 경계 검사

현재 checkout 또는 정확한 Git commit의 서버 소스는 별도 공개 진입
`check-module-boundaries.sh`로 검사한다. 위 frozen 비교 입력·CodeGraph·Unity
외부 DLL·과거 로컬 근거가 필요하지 않다. 정책은
[Boundaries/module-boundaries.json](Boundaries/module-boundaries.json)에 있으며,
규칙 변경 주체는 이 도구를 담당하는 Architecture 파트다. 제품 변경은 코드
소유 파트에 경고 위치와 근거를 전달한다.

저장소 루트의 WSL/Linux에서 실행한다. 출력은 존재하지 않는 새 디렉터리여야
하며 저장소 `.backups/` 또는 CI의 `RUNNER_TEMP` 안에 둔다.

```bash
# 현재 작업 트리: HEAD 외 변경은 input hash/workspaceStatus/workspaceUntrackedInputs로 구분한다.
bash 99_Tools/Architecture/check-module-boundaries.sh \
  --output-root .backups/architecture/module-boundaries/current-run

# Git 포인터가 Linux에서 읽히는 checkout: main을 resolve하여 원본 blob 검사.
main_sha=$(git rev-parse origin/main)
bash 99_Tools/Architecture/check-module-boundaries.sh \
  --source-ref "$main_sha" --output-root .backups/architecture/module-boundaries/main-run

# SDK10.0.301이 필요한 요구사항 suite를 명시 opt-in으로 실행한다.
# WORK는 .backups 또는 RUNNER_TEMP 아래의 존재하지 않는 새 절대 경로다.
MODULE_BOUNDARIES_TEST_WORK="$PWD/.backups/architecture/module-boundaries/tests" \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests \
  -t 99_Tools/Architecture.Tests -p test_module_boundaries.py -v
```

Windows PowerShell에서는 같은 명령 앞에 `wsl -d Ubuntu --`를 붙인다. 새 실행은
새 출력 이름을 선택한다. 경로의 root 이탈·symlink·이미 존재하는 출력·소스와
겹치는 별도 출력은 쓰기 전에 거부한다. 이 초기 거부는 stderr와 nonzero로
확인하며, 과거 결과를 이번 결과로 해석하지 않는다.
요구사항 suite는 WORK 미설정 때만 skip한다. 빈 값·상대 경로·symlink·기존 WORK는
실패하며 기본 위치로 대체하지 않는다. 명시 opt-in 뒤 SDK 부재도 nonzero 실패다.
모든 fixture·외부 실행 결과는 지정한 WORK 안에 남긴다. 전용 CI는 요구사항과
discovery 회귀를 함께 실행해 수집/실행/skip 건수를 `tests/counts.json`에 기록하고,
두 suite 중 하나가 수집되지 않거나 skip하면 job을 실패시킨다.

workspace 모드의 `sourceSha`는 Git HEAD다. `workspaceStatus`는 추적된 파일의
변경 상태이며, `workspaceUntrackedInputs`는 실제 snapshot manifest에 포함된
미추적 입력의 상대 경로 목록이다. Git 추적 집합과 이 입력 목록만 대조하므로
관련 없는 미추적 파일·ignore된 `.backups/` 과거 근거는 목록에 넣지 않는다.
ignore된 파일이라도 snapshot 입력이면 미추적 목록에 포함된다.
`--source-ref`의 `git_blobs` 모드는 원본 blob만 사용하고 이 목록은 비어 있다.
현재 workspace bytes와 blob의 차이는 각 실행의 `input.sha256`/manifest로 확인한다.
Windows가 만든 Orca linked worktree는 WSL의 `/mnt/<drive>`로 Git metadata
경로를 읽고 backlink가 이 checkout을 가리키는지 대조한다. `.git` 포인터와
설정을 수정하지 않으며 Git 명령에는 `GIT_OPTIONAL_LOCKS=0`을 적용한다.
이 worktree의 main SHA는 PowerShell의 Windows Git으로 조회해 전달한다.

```powershell
$moduleMainSha = git rev-parse origin/main
wsl -d Ubuntu -- bash 99_Tools/Architecture/check-module-boundaries.sh `
  --source-ref $moduleMainSha --output-root .backups/architecture/module-boundaries/main-run
```

SDK는 `global.json`의 정확한 10.0.301이다. 선택 순서는 기존
`DAWNHOLDER_DOTNET` 절대 실행 경로 → 로컬 Dawnholder SDK → PATH의 dotnet →
기존 사용자 `.dotnet/dotnet`이며 선택한 host의 버전이 다르면 실패한다. 설치나
다른 버전 재선택을 하지 않는다. 새 실행의 `work/runtime/`에 CLI home·NuGet·
임시 경로를 격리하고, 입력은 `work/source/`, 검사 도구는 `work/tool/`에 복사한다.
서버와 프로젝트 참조를 restore하고 Debug 디자인타임 compilation으로 읽는다.
도구 자체만 build/emit하며 제품 build/emit·Shared DLL 복사·서버/Unity/DB 실행은
하지 않는다. SDK의 Roslyn/Workspace DLL을 사용하며 새 NuGet 의존성은 없다.

| 규칙 | 경고 방향 | 근거와 허용 동작 |
|---|---|---|
| MB001 | Handlers → Maps | ARCHITECTURE의 요청/상태 변경과 FEATURE_MAP의 제출 흐름. Handlers → Sessions 허용 |
| MB002 | Maps → Handlers | Maps 상태 소유와 MapPacketPublisher 계약. Maps → Sessions 송신/연결 허용 |
| MB003 | Sessions → 구체 Handlers | GameSession.OnRecvPacket dispatcher. 정확한 HandlerRegistry/IPacketHandler 타입만 예외 |

물리적 `02_Server/GameServer/{Handlers,Maps,Sessions}` 소스/원 선언 폴더를
분류한다. Roslyn이 해석한 alias·완전 수식 이름·필드·시그니처·멤버 호출·추론된
배열/제네릭 요소 타입을 검사한다. 참조 목록에는 이 세 영역의 타입/멤버 선언을
타깃으로 한 관측만 들어간다. 동일 소스 syntax span과 원 타깃의 중복 관측은
하나로 합치며 반복된 사용 위치는 별도 occurrence다. 집계 단위는 클래스 수나
서로 다른 의존 간선 수가 아니다.

주석·문자열·namespace 자체·비활성 전처리 분기·bin/obj와 boundary의
`.g.cs`/`.generated.cs`는 정책 관측에서 제외한다. 생성 입력은 compilation에
참여한다. 다른 영역/외부 타입은 이 세 규칙의 타깃에서 제외하고 같은 영역
참조는 허용한다. 경로 밖으로 코드를 옮기면 분류가 달라지는 한계가 있다.
reflection/dynamic의 런타임 타깃·actor/tick 안전·게임 동작을 판정하지 않는다.
동적 ProjectReference/Compile Include와 명시적 Import는 지원하지 않으며
실패한다. 실제 Workspace graph/Compile 집합을 입력 manifest와 대조하고,
경계 파일 누락·0대상·미해석 compilation 오류를 성공으로 바꾸지 않는다.

| 결과 | exit | 의미 |
|---|---:|---|
| `completed / clean` | 0 | 전체 대상 분석 완료, 위반 0 |
| `completed / warnings` | 0 | 전체 대상 분석 완료, 정책 warning 있음 |
| `unavailable / not_completed` | nonzero | 선택한 도구 부재/시작 불가 |
| `failed / not_completed` | 1 또는 124 | 입력/규칙/Workspace/실행 실패 또는 timeout |
| `cancelled / not_completed` | 130 | 취소 후 자기 process group 정산 |

실패의 `violationCount`는 null이다. `result.json`의 `reasonCode`, `message`,
`repair`, `stages[].rawPath`에서 이번 원인을 찾고 새 출력 경로로 재실행한다.
자동 삭제·cache 정리·재측정·이전 결과 재사용은 없다. 기본 상한은 5,000 C# 소스,
64MiB 소스 bytes, 개별 외부 단계 600초이며 CLI로 양수 범위를 좁힐 수 있다.
timeout/취소/종료에는 이 실행이 만든 Linux process group만 정산한다.

원시는 `result.json`, `input-manifest.json`, `tool-manifest.json`, `rules.json`,
`analysis.json`, `stages/*/{command.json,stdout.txt,stderr.txt}`와 `summary.md`다.
source/checkout/PR head SHA, 입력/미커밋 도구 hash, 파일수·커버리지·규칙별
occurrence·SDK/Roslyn/Python·argv/환경/종료값·실제 경과시간을 구분해 남긴다.
성공값을 상수로 채우지 않는다. `analysis.json`에는 Workspace/compilation/참조
분석 단계 시간이 있고, 바깥 결과에는 입력 snapshot과 각 프로세스 단계 시간이 있다.

전용 [module-boundaries.yml](../../.github/workflows/module-boundaries.yml)은
경로 필터 없이 모든 PR에 실행되며 contents:read, job 20분 상한을 사용한다.
정책 warning은 job을 실패시키지 않고 도구/입력 실패는 nonzero를 유지한다.
`--github-annotations`는 실제 소스 위치의 warning과 job summary/artifact를
제공한다. 실제 입력이 clean이면 별도 fixture의 의도된 warning을 시연하고
`inputKind=fixture`, `[fixture demonstration]`, 별도 결과 파일로 표시한다.
fixture 위반 수를 production 결과에 합산하지 않는다. required check/branch
protection은 변경하지 않는다. 로컬 annotation 문자열 확인과 실제 GitHub PR의
렌더링/완료 확인은 별개이며 후자는 PR 발행 뒤 원시와 함께 확인해야 한다.
