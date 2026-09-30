# 개발과 검증

명령은 저장소 루트에서 실행한다. 역할·Git 승인 경계는 [AGENTS](../../AGENTS.md), 현재 작업은 [CURRENT](CURRENT.md), 영역별 변경 영향은 [domains](../domains/INDEX.md)에 있다.

## 환경

- .NET SDK: [global.json](../../global.json)의 `10.0.203`, `rollForward: latestFeature`.
- Unity: [ProjectVersion.txt](../../03_Client/ProjectSettings/ProjectVersion.txt)의 `6000.4.7f1`과 revision을 확인한다.
- WSL Ubuntu: .NET SDK, Bash·rsync·flock(util-linux)·ss(iproute2)·coreutils가 필요하다. 저장소는 ASCII 경로를 권장한다.
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

WSL 빌드는 Windows 원본 Unity DLL을 갱신하지 않는다. DLL 반영은 Windows 빌드 또는 별도로 정한 검증·배포 범위에서 수행한다.

## 패킷 생성과 로컬 Git 검사

PacketGenerator는 PDL 경로를 명시해야 저장소 루트에서도 동작한다. 정상 출력은 exit0, 잡힌 출력 실패는 exit1을 반환하며 파싱 예외는 기존대로 전파하고 `--no-wait`는 성공·실패 모두 후행 키 입력 대기를 생략한다. 다음 명령은 .NET 실행이 허용된 환경용이며 생성 소스를 변경한다.

```powershell
dotnet run --project 99_Tools/PacketGenerator -- 99_Tools/PacketGenerator/PDL.xml --no-wait
```

생성 후 ID·필드·버전 diff와 양쪽 소비자를 확인한다. 세부 계약은 [프로토콜](../domains/protocol.md)에 있다. 공유 Git 훅을 사용할 머신에서는 `git config core.hooksPath .githooks`로 연결한다. 훅은 알려진 Unity Cloud 필드 변경을 검사하며 모든 변경을 자동 보호하지는 않는다.

## 결과 해석

검사 명령·환경·관찰 결과·생략 항목·원본 로그 위치를 목표에 기록한다. 서버·봇 통과와 Unity 에디터·게임 외관·SQL 연결을 구분한다. 이전 실행 수치는 [완료된 셋업 목표](../../01_Phases/goals/2026-09-29-codex-setup/goal.md)의 당시 근거이며 새 변경의 성공을 대신하지 않는다. 현재 main의 SQL 영속화는 구현 완료로 취급하지 않는다.
