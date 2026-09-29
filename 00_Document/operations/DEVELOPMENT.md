# 개발 안내

현재 작업 규칙은 [AGENTS](../../AGENTS.md), 작업 위치는 [CURRENT](CURRENT.md)에서 확인한다. 프로젝트 스킬은 `.agents/skills/`에 있으며 별도 사용자 전역 설치는 하지 않는다. 기본 기능으로 충분하므로 프로젝트 `.codex/config.toml`은 추가하지 않는다.

## 환경

- .NET SDK 기준은 루트 [global.json](../../global.json)의 `10.0.203`, `rollForward: latestFeature`이다.
- Unity 프로젝트는 `03_Client/`이며 [ProjectVersion.txt](../../03_Client/ProjectSettings/ProjectVersion.txt)의 `6000.4.7f1`을 기준으로 연다.
- 저장소는 ASCII 경로를 권장한다. 현재 경로는 `C:\Dev\DawnHolder_Project`이며 스크립트에 개인별 절대 경로를 고정하지 않는다.
- WSL 실행에는 .NET SDK, Bash·rsync·flock(util-linux)·ss(iproute2)·coreutils(realpath·sha256sum·timeout)가 필요하다. Windows 빌드와 Unity 에디터 검증은 서로 다른 확인 항목이다.
- 현재 게임 서버의 DB 영속화는 구현 완료로 취급하지 않는다. MSSQL/LocalDB 설계 문서가 있어도 DB 연동 시연·WSL 연결 성공의 근거가 되지 않는다.

## 빌드와 실행

아래 .NET 명령은 저장소 루트에서 사용할 수 있는 프로젝트 진입점이다. 실행 가능 여부와 이번 정비의 실제 통과 결과는 [현재 목표](../../01_Phases/goals/2026-09-29-codex-setup/goal.md)에 따로 기록한다.

```powershell
dotnet build Dawnholder.slnx
```

주의: Shared와 ClientNet 빌드는 산출 DLL을 `03_Client/Assets/Plugins/Shared/`, `03_Client/Assets/Plugins/ClientNet/`로 복사한다. 빌드 전후 Git 상태를 확인하고 기존 사용자 에셋 변경과 생성물 변경을 구분한다. `.NET` 빌드 성공은 Unity 컴파일·실행 성공을 뜻하지 않는다. 현재 머신에서 서버·테스트 실행은 아래 WSL 경로를 사용한다. VS Code의 Windows 전용 디버그 설정은 네이티브 실행이 허용된 머신에 한정한다.

WSL Ubuntu 명령도 저장소 루트의 PowerShell에서 실행한다.

```powershell
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh path
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh build
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh test
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh run
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh bot DashSmoke
```

`path`는 해당 저장소의 Linux 복사 위치, `sync`는 동기화만 수행한다. `run-server.bat`도 이 helper의 `run`으로 진입한다. `run_dash_smoke.sh`, `run_bot_fresh_recheck.sh <Scenario...>`, `run_bot_regression.sh`는 같은 helper를 사용한다.

복사 위치는 원본 경로별로 격리된 `$HOME/.cache/dawnholder/workspaces/<hash>`다. `DAWNHOLDER_WSL_ROOT`로 바꾸려면 전용 Linux 디렉터리를 지정해야 하며 비어 있거나 해당 저장소 소유 표시가 있는 경로만 허용한다. WSL helper는 Windows 원본 Unity DLL을 갱신하지 않는다. Unity용 DLL 갱신이 필요하면 Windows에서 해당 프로젝트를 빌드하고 변경물을 확인한다. 실제 이번 빌드·테스트 결과는 목표 기록을 확인한다.

`DAWNHOLDER_WSL_ROOT`와 `DAWNHOLDER_DOTNET`은 WSL 환경변수다. PowerShell에서 전달하려면 `wsl -d Ubuntu -- env DAWNHOLDER_WSL_ROOT=/home/bass1/dawnholder-custom bash 99_Tools/sync-wsl.sh build`처럼 `env`로 지정한다.

## 변경 영역별 확인

| 영역 | 필요한 계약과 검증 |
|---|---|
| 서버 | 게임 맵 상태는 actor 경계를 지킨다. 핸들러는 파싱·검증 뒤 세션 메서드를 호출하며 내부 수명주기 상태를 우회 변경하지 않는다. 입력의 정상·거부·권한 경로를 검증한다. |
| Unity | 입력·표시·예측과 서버 권위 상태를 분리한다. 네트워크 결과는 안전하게 메인 스레드로 전달한다. 에셋과 `.meta`, GUID·문자열 경로 참조를 함께 확인한다. |
| Shared·PacketGenerator | `99_Tools/PacketGenerator/PDL.xml`을 원본으로 재생성한다. 패킷 ID 재사용을 금지하고 버전·직렬화·핸드셰이크 호환성을 확인한다. 생성 코드·공유 DLL과 서버·Unity 사용처를 함께 검증한다. |
| ClientNet·서버 Network | 전송 구현은 분리되어 있다. 프레임 검증 계약 변경은 양쪽을 확인한다. ClientNet에는 Unity API 의존성을 넣지 않으며 봇 사용처도 점검한다. |
| 게임 공식 | 공유 공식은 같은 입력에 같은 결과를 주도록 유지한다. 시간·난수 의존성을 명시하고 서버 판정을 우회하지 않는다. |

설계 근거는 [ADR](../ADR/INDEX.md), 코드 위치는 [FEATURE_MAP](../FEATURE_MAP.md), 코드 규약은 [conventions](../conventions/INDEX.md)에서 필요한 부분만 읽는다. 오래된 문서의 예정 구현·명령·모델 배정은 현행 코드와 분리해 확인한다.

## 목표별 개발 흐름

메인 세션이 사용자와 목표·완료조건을 정하고 최신 `main`에서 목표별 작업 브랜치를 만든다. 작업자 구현과 독립 검증을 거쳐 메인이 통합하고, 승인된 권한 범위에서 PR을 만들어 `main`에 병합한다. 현재의 `chore/codex-project-setup`은 이번 정비용이며 병합 후 종료한다.

목표의 상태·결과는 해당 `goal.md`에 모으고 CURRENT에는 링크만 둔다. Orca 다중 세션이 필요한 작업은 [ORCA](ORCA.md)를 필요한 시점에만 확인한다.

## 과거 환경

기존 Claude 규칙·슬래시 명령·훅·설정은 [고정 보관 브랜치](https://github.com/bass131/dawnholder-server/tree/archive/claude-setup-2026-09-29)에 남아 있다. 과거 ADR·Phase·정책·기술 지식은 설계와 이력 자료이며 현행 Codex 절차를 자동 정의하지 않는다. `.claude/knowledge`도 필요한 기술 자료만 선택해서 읽는다.
