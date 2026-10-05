# 서버 모듈 경계 검사

현재 checkout 또는 정확한 Git commit의 서버 소스는 별도 공개 진입
`check-module-boundaries.sh`로 검사한다. 동결 비교 입력·CodeGraph·Unity
외부 DLL·과거 로컬 근거가 필요하지 않다. 정책은
[module-boundaries.json](module-boundaries.json)에 있으며,
규칙 변경 주체는 이 도구를 담당하는 Architecture 파트다. 제품 변경은 코드
소유 파트에 경고 위치와 근거를 전달한다.

저장소 루트의 WSL/Linux에서 실행한다. 출력은 존재하지 않는 새 디렉터리여야
하며 저장소 `.backups/` 또는 CI의 `RUNNER_TEMP` 안에 둔다.

```bash
# 현재 작업 트리: HEAD 외 변경은 input hash/workspaceStatus/workspaceUntrackedInputs로 구분한다.
bash 99_Tools/ModuleBoundaries/check-module-boundaries.sh \
  --output-root .backups/architecture/module-boundaries/current-run

# Git 포인터가 Linux에서 읽히는 checkout: main을 resolve하여 원본 blob 검사.
main_sha=$(git rev-parse origin/main)
bash 99_Tools/ModuleBoundaries/check-module-boundaries.sh \
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
wsl -d Ubuntu -- bash 99_Tools/ModuleBoundaries/check-module-boundaries.sh `
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

## 이전 경로와 새 진입

| 이전 위치 | 현재 위치 |
|---|---|
| `99_Tools/Architecture/Boundaries/<파일>`의 10파일 | `99_Tools/ModuleBoundaries/<파일>` |
| `99_Tools/Architecture/check-module-boundaries.py` | `99_Tools/ModuleBoundaries/check-module-boundaries.py` |
| `99_Tools/Architecture/check-module-boundaries.sh` | `99_Tools/ModuleBoundaries/check-module-boundaries.sh` |

`Directory.Build.props`는 기존 비교 도구의 빈 독립 SDK 경계를 이 폴더에도 둔다.
기존 [Architecture 도구](../Architecture/README.md)는 동결 비교 입력과 과거 batch를
소유한다. 현재 검사기의 C# 프로젝트 등록은
[independent-projects.json](../Formatting/independent-projects.json)에 있다.

## 테스트 opt-in

설치에 의존하지 않는 기본 discovery는 Architecture.Tests의 기존 명령을 사용한다.
공개 SDK 진입을 실행하는 suite에는 각자의 WORK를 명시한다. 서로 다른 suite를
별도 명령과 새 WORK에서 실행하고, opt-out의 skip을 실행 통과로 세지 않는다.

| 환경변수 | 실행 계약 |
|---|---|
| `MODULE_BOUNDARIES_TEST_WORK` | 요구사항 suite. `.backups/` 또는 `RUNNER_TEMP` 아래 존재하지 않는 새 절대 경로. SDK10.0.301 필요 |
| `MODULE_BOUNDARIES_INDEPENDENT_WORK` | 독립 정책·실패 경계 suite의 별도 근거 root. 새 절대 경로를 사용 |
| `MODULE_BOUNDARIES_INDEPENDENT_REAL=1` | 독립 suite의 실제 소스 사례도 실행. 위 WORK와 실제 HEAD/main SHA 및 파일 수 입력을 함께 제공 |
| `MODULE_BOUNDARIES_EXECUTION_WORK` | 독립 실행 계약 suite. 저장소 `.backups/` 아래 존재하지 않는 새 절대 경로 |

독립 실제 소스 사례의 입력은 `MODULE_BOUNDARIES_INDEPENDENT_HEAD`,
`MODULE_BOUNDARIES_INDEPENDENT_MAIN`,
`MODULE_BOUNDARIES_INDEPENDENT_HEAD_BOUNDARY_FILES`,
`MODULE_BOUNDARIES_INDEPENDENT_MAIN_BOUNDARY_FILES`다. SHA와 물리 경계 파일 수는
해당 source/commit에서 실제로 확인한 값을 전달한다. 실행 계약 suite의 main blob
사례에는 `MODULE_BOUNDARIES_EXECUTION_MAIN_SHA`를 추가한다. 그 입력이 없으면
해당 main 사례는 skip하므로 로그에서 이유와 수를 확인한다.

```bash
MODULE_BOUNDARIES_INDEPENDENT_WORK="$PWD/.backups/architecture/module-boundaries/independent-new" \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests \
  -t 99_Tools/Architecture.Tests -p test_module_boundaries_independent.py -v

MODULE_BOUNDARIES_EXECUTION_WORK="$PWD/.backups/architecture/module-boundaries/execution-new" \
python3 -B -m unittest discover -s 99_Tools/Architecture.Tests \
  -t 99_Tools/Architecture.Tests -p test_module_boundaries_execution_contract.py -v
```

전용 workflow는 명시 TEST_WORK로 요구사항·discovery를 실제 실행하고 독립 suite의
opt-out은 별도 skip으로 기록한다. `tests/counts.json`에서 수집·실행·skip·실패·오류와
필수 suite의 실제 실행 여부를 확인한다. 로컬 자체 점검은 독립 판정과 구분한다.
