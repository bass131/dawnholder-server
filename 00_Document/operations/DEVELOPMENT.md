# 개발과 검증

명령은 저장소 루트에서 실행한다. 역할·Git 승인 경계는 [AGENTS](../../AGENTS.md), 현재 작업은 [CURRENT](CURRENT.md), 영역별 변경 영향은 [domains](../domains/INDEX.md)에 있다.

## 환경

- .NET SDK: [global.json](../../global.json)의 정확한 `10.0.301`, `rollForward: disable`. 다른 버전으로 대체하지 않는다.
- SDK pin 이전의 obj/bin이 남은 checkout은 해당 빌드 생성물을 확인·백업하고 정리한 뒤 1회 clean 빌드가 필요하다.
- Unity: [ProjectVersion.txt](../../03_Client/ProjectSettings/ProjectVersion.txt)의 `6000.4.7f1`과 revision을 확인한다.
- WSL Ubuntu: .NET SDK, Bash·Python 3·rsync·flock(util-linux)·ss(iproute2)·coreutils가 필요하다. 저장소는 ASCII 경로를 권장한다.
- 서버·테스트 실행이 Windows 정책으로 차단되는 현재 환경에서는 WSL을 사용한다. Windows 전용 디버그 설정은 네이티브 실행이 허용된 머신용이다.

## Windows 빌드

```powershell
dotnet build Dawnholder.slnx
```

Shared·ClientNet의 post-build는 `03_Client/Assets/Plugins/Shared/`와 `ClientNet/`에 DLL을 복사한다. 전후 Git 상태를 확인한다. 솔루션 빌드 성공은 Unity 컴파일·플레이 성공을 뜻하지 않는다.

## WSL 빌드·테스트·실행

저장소 루트의 PowerShell에서 실행한다.

```powershell
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh path
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh build
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh test
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh run
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh bot DashSmoke
```

`path`는 복사 위치만 표시하고 `sync`는 동기화만 한다. 나머지는 동기화·Debug 빌드를 먼저 수행한다. `run-server.bat`와 봇 wrapper 3개도 같은 helper를 사용한다. 서버는 현재 7777 포트를 사용하므로 다른 소유자의 실행과 겹치면 시작하지 않는다.

원본 경로별 Linux 복사본은 `$HOME/.cache/dawnholder/workspaces/<hash>`다. 소유 marker와 workspace lock으로 동일 대상의 동시 작업을 막는다. `DAWNHOLDER_WSL_ROOT`는 비어 있거나 해당 원본 소유 표시가 있는 전용 Linux 디렉터리만 허용한다. `DAWNHOLDER_DOTNET`과 함께 WSL 환경변수이므로 PowerShell에서 지정할 때는 `wsl -d Ubuntu -- env DAWNHOLDER_WSL_ROOT=/home/bass1/dawnholder-custom bash 99_Tools/sync-wsl.sh build`처럼 전달한다.

`build/test/run/bot`은 같은 SDK 선택 함수를 사용한다. 순서는 명시한 `DAWNHOLDER_DOTNET` → `/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet` → PATH의 `dotnet` → `$HOME/.dotnet/dotnet`이다. override는 실행 가능한 절대 경로여야 한다. 선택한 실행파일을 복제본의 `global.json`이 적용되는 cwd에서 확인하고 요구 버전·선택 경로·실제 SDK를 출력한다. override 부재·실행 오류·버전 불일치는 실패하며 다른 버전으로 다시 선택하지 않는다. 전역 PATH·기존 SDK·셸 profile은 바꾸지 않는다.

첫 dotnet 호출부터 개발 인증서 생성과 전역 도구 경로의 PATH 추가를 억제하고, 새 실행 전용 CLI home과 NuGet package/HTTP/plugins/scratch 경로를 사용한다. `DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0`은 프로세스와 자식에만 적용한다. 선택한 host를 `DOTNET_HOST_PATH`로 자식 테스트 프로세스에도 전달한다. 제품 restore를 명시한 다음 Debug build/test를 수행하며, 원본에는 역복사하지 않는다.

WSL 빌드는 Windows 원본 Unity DLL을 갱신하지 않는다. DLL 반영은 Windows 빌드 또는 별도로 정한 검증·배포 범위에서 수행한다.

## C# 서식 검사

Windows 원본 Git에서 입력 manifest를 만들고 전용 WSL 복제본을 검사한다.

```powershell
& ./99_Tools/format-check.ps1
```

CI나 Git이 있는 Linux checkout에서는 `bash 99_Tools/format-check.sh`를 사용한다. Git이 없는 복제본은 `--manifest <원본 manifest 경로>`가 필요하다. Windows 진입점은 실제 checkout SHA·작업 상태·파일/설정 hash·Compile 집합·SDK를 기록하고, sync는 루트 빌드/정책 입력과 manifest를 명시적으로 복사해 hash를 대조한다. 복제본의 `.git` 열거나 원본 C# 쓰기를 하지 않는다.

두 서식 진입점과 CI는 첫 SDK 조회 전부터 `DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0`으로 새 CLI home의 도구 경로가 사용자 PATH에 추가되는 것을 막는다. PowerShell 진입점은 종료·실패 시 호출자의 프로세스 환경값을 복원한다. 이미 추가된 사용자 PATH 항목은 자동 수정하지 않는다.

제품 참조와 독립 테스트의 restore/build 및 서식 적용·보존 비교는 소유 표시가 있는 검사 snapshot에서 수행한다. Shared/ClientNet 빌드의 Unity DLL 복사도 그 snapshot 안에 한정하며, 실제 checkout의 입력·Git 상태·기존 DLL hash를 다시 확인한다. CI의 제품 전체 빌드는 별도 restore 뒤 수행한다.

검사는 고정 SDK의 `dotnet format whitespace`만 사용한다. 수기 제품 소스·검사 도구·독립 `Formatting.Tests`를 각각 검사하고, 생성 `GenPackets.cs`는 제외하며 hash로 보존한다. 누락된 입력·SDK 불일치·로드/파싱 오류·실행 실패·독립 테스트 프로젝트 부재는 실패한다. 원본과 자체 서식 snapshot의 Debug/Release token·리터럴·주석·directive 보존 근거 및 원시 명령/exit/report는 출력된 전용 evidence 디렉터리에 남긴다. 이 검사는 제품 전체 build/test나 Unity 검증을 대신하지 않는다.

## 패킷 생성과 로컬 Git 검사

PacketGenerator는 PDL 경로를 명시해야 저장소 루트에서도 동작한다. 정상 출력은 exit0, 잡힌 출력 실패는 exit1을 반환하며 파싱 예외는 기존대로 전파하고 `--no-wait`는 성공·실패 모두 후행 키 입력 대기를 생략한다. 다음 명령은 .NET 실행이 허용된 환경용이며 생성 소스를 변경한다.

```powershell
dotnet run --project 99_Tools/PacketGenerator -- 99_Tools/PacketGenerator/PDL.xml --no-wait
```

생성 후 ID·필드·버전 diff와 양쪽 소비자를 확인한다. 세부 계약은 [프로토콜](../domains/protocol.md)에 있다. 공유 Git 훅을 사용할 머신에서는 `git config core.hooksPath .githooks`로 연결한다. 훅은 알려진 Unity Cloud 필드 변경을 검사하며 모든 변경을 자동 보호하지는 않는다.

## PowerShell·TypeScript·Python 코드 규칙 검사

`99_Tools/CodeRules/check-code-rules.mjs`의 `Changed`와 `All`은 같은 설정·도구·진단 처리를 사용하며 대상 수집만 다르다. Node, PowerShell 7의 `pwsh`, Python 3가 실제 실행 가능해야 한다. Windows의 Python WindowsApps alias를 interpreter로 사용하지 않는다. 현재 Windows 환경에서는 기존 WSL Ubuntu Python을 명시한다.

이 목표에서 승인·저장한 PSScriptAnalyzer 1.25.0의 정확 manifest를 사용하는 실제 로컬 명령은 다음과 같다. 다른 머신은 승인된 모듈 저장 위치와 실행 가능한 Python 경로를 명시한다. 도구가 전역 설치·PATH·정책·repository trust를 변경하거나 자동 설치하지 않는다.

```powershell
$rulesModuleManifest = Join-Path (Get-Location) '.backups/verification/2026-10-02-agent-rule-context/tool-cache/modules/PSScriptAnalyzer/1.25.0/PSScriptAnalyzer.psd1'
node 99_Tools/CodeRules/check-code-rules.mjs --scope Changed --base HEAD --pssa-manifest $rulesModuleManifest --python python3 --wsl-distribution Ubuntu
node 99_Tools/CodeRules/check-code-rules.mjs --scope All --base HEAD --pssa-manifest $rulesModuleManifest --python python3 --wsl-distribution Ubuntu
```

`Changed --base HEAD`는 staged/unstaged/untracked의 현재 파일을 포함한다. 커밋한 변경까지 검사하려면 `--base origin/main`처럼 실제 비교 기준을 지정한다. 추가·수정·rename의 새 경로를 검사하고 삭제는 결과에 기록한다. `All`은 Git tracked와 ignore되지 않은 untracked의 같은 허용 대상을 모은다. 입력은 현재 작업 트리이며 base/HEAD, dirty, 변경·삭제·제외 목록, 검사 파일·설정 SHA256을 기록한다. symlink 입력과 저장소 밖 소스는 거부한다.

- 수기 `.ps1/.psm1/.psd1`: AST 구문과 명시 선택한 `PSUseConsistentIndentation`(공백 4칸), `PSUseConsistentWhitespace`만 검사한다. 대상 import/dot-source나 default 전체 rule set을 실행하지 않는다. 배포 파일 hash와 정확 버전을 확인하며 source suppression도 진단에 포함한다.
- Management `.ts/.tsx/.cts` 또는 package/lock/tsconfig 변경: 정확 기존 lockfile 의존성의 `typecheck`, `desktop:typecheck`, `mcp:typecheck` 세 명령을 각 프로젝트 전체에 수행한다. 기존 순수 `tsc` 명령의 argv로 실행하고 `--noEmit`을 유지한다. 필요한 복원은 `05_Management/frontend`에서 `npm ci --ignore-scripts --no-audit --no-fund`다. 검사 자체는 의존성을 설치하지 않는다.
- Architecture Pipeline·Architecture.Tests와 CodeRules·CodeRules.Tests의 Python: 표준 라이브러리 AST/compile만 수행한다. 대상 모듈 import·bytecode 출력은 하지 않는다.
- Unity 자산·외부/생성/캐시/node_modules/`.backups`와 `99_Tools/CodeRules.Tests/fixtures/`는 제외한다. 일반 테스트 소스 전체를 제외하지 않는다. C#은 기존 workflow가 소유한다.

SQLFluff 4.3.0의 tsql/raw 적용성 시범은 고정 SQL 17개 중 15개 parse 통과, 저장 연동/main 양쪽 `verify-schema.sql` 2개 parse 실패였다. 각각 line 137:1과 56:23의 `PRS`이며 엔진 수락은 확인하지 않았다. [사용자 결정과 시범 근거](../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md#sql-deferred)에 따라 이번 SQL CI는 보류한다. Changed/All 모두 선택된 `.sql`의 목록·hash·개수·이유·근거를 `deferred`로 남긴다. 보류를 실패 수에 넣지 않고 SQL 통과로도 표시하지 않는다. 활성 검사에 실패가 없으면 exit 0이지만 결과 제목에 SQL 보류 수를 명시하고 `summary.passed`는 null이다.

SQLFluff venv 절차·고정 의존성과 LT01/LT02 설정·어댑터 초안은 남겼으며 SQL 설치/검사를 workflow에 연결하지 않는다. 승인된 준비는 기존 Python에서 목표 전용 `python3 -m venv`를 만들고 그 venv Python의 `-m pip install --only-binary=:all: sqlfluff==4.3.0`이다. 재현용 `sql-requirements.txt`는 관측 15개 배포 버전과 Linux wheel hash를 고정했고 `--require-hashes --only-binary=:all:`로 사용하는 자료다. 현재 goal에 설치 명령·exit·환경 및 시범 원문이 있다. SQL modules 구조 분리 이후 새 modules와 verify-schema로 다시 시범·도입을 결정한다. 이번에는 추가 parser/의존성, 제품 SQL 수정·DB 실행·PowerShell 문자열 내부의 동적 SQL 검사를 하지 않는다.

기본 출력은 `.backups/code-rules/run-.../`이며 `--output`으로 별도 부모 폴더를 지정할 수 있다. 매번 새 실행 폴더의 `results.json`, `results.txt`에 파일·규칙·줄·위반/실패 수를 남기고 명령 argv/cwd/timeout/exit와 원시 stdout/stderr를 보존한다. 관련 대상 0개는 `not-applicable`, 적용 도구 부재·버전/hash 불일치·parse/config/process 실패는 nonzero다. 자동 수정·baseline 생성은 없다.

새 `code-rules.yml`은 PR에서 확인한 base와 실제 checkout의 `Changed`를 기본으로 사용한다. `workflow_dispatch`의 명시 `All`/`Changed`와 base 입력도 같은 검사기를 호출하며 All을 PR 필수나 cron으로 연결하지 않는다. 실패해도 검사 결과와 독립 테스트 로그만 artifact로 보존한다. 설치 캐시·전체 저장소를 올리지 않는다. 독립 테스트 진입점은 신규 Opus 소유 `99_Tools/CodeRules.Tests/code-rules.test.mjs`이며 부재/load 실패도 CI 실패다. 구현 자체 점검과 원격 PR/manual job 성공은 별개이며, 이 문서 작성 시점에 원격 job과 독립 테스트는 미실행이다. 타입/구문/서식 검사 성공은 가독성·설계·IPC 통합·게임/Unity·DB 검증을 대신하지 않는다.

## 결과 해석

검사 명령·환경·관찰 결과·생략 항목·원본 로그 위치를 목표에 기록한다. 서버·봇 통과와 Unity 에디터·게임 외관·SQL 연결을 구분한다. 이전 실행 수치는 [완료된 셋업 목표](../../01_Phases/goals/2026-09-29-codex-setup/goal.md)의 당시 근거이며 새 변경의 성공을 대신하지 않는다. 현재 main의 SQL 영속화는 구현 완료로 취급하지 않는다.
