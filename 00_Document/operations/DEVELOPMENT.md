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

제품 프로젝트는 `Dawnholder.slnx`의 명시 집합을 사용한다. slnx 밖의 독립 도구는 `99_Tools/Formatting/independent-projects.json`의 `SchemaVersion: 1`, `Projects` 배열에 저장소 상대 csproj 경로로 등록한다. 기본 목록은 Formatting과 Formatting.Tests이며 새 도구를 추가하는 파트가 자기 PR에서 해당 항목을 추가한다. 등록 검사 코드는 GameDev가 소유한다. 목록 자체도 입력 manifest와 hash 대조에 포함되며, 중복·없는 경로·제품과의 중복·미등록 csproj/C# 입력은 실패한다.

두 집합은 restore, 실제 Debug/Release Compile 수집, whitespace 검사·snapshot 적용, 보존 비교에 모두 연결된다. formatter 실행 준비에 필요한 bootstrap과 전체 등록 대상의 검사를 구분한다. `Formatting.Tests`의 필수 실행은 유지하며, 다른 독립 도구의 모든 기능 테스트를 이 등록만으로 자동 실행하지는 않는다. 각 파트가 자기 테스트의 CI 연결을 소유한다.

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

## 결과 해석

검사 명령·환경·관찰 결과·생략 항목·원본 로그 위치를 목표에 기록한다. 서버·봇 통과와 Unity 에디터·게임 외관·SQL 연결을 구분한다. 이전 실행 수치는 [완료된 셋업 목표](../../01_Phases/goals/2026-09-29-codex-setup/goal.md)의 당시 근거이며 새 변경의 성공을 대신하지 않는다. 현재 main의 SQL 영속화는 구현 완료로 취급하지 않는다.
