# 관리 백엔드 설계 — PR1

[운영툴 V1.0 goal](goal.md)의 PR1 「관리 백엔드와 서버 실행 관리」 설계다. 선행 시험과 구현이 같은 계약을 보도록 바깥에서 관찰할 수 있는 동작을 정한다. 클래스 나누기와 내부 이름은 구현자가 정하되 아래 상태 소유와 실패 처리를 지킨다. 화면 연결(PR2)과 장애 대응(PR3)은 이 문서의 범위가 아니다.

## 근거

- 승인 범위: goal 「적용 중인 사용자 결정」 1~3(서버 운영 기본, 게임 서버 코드 변경 없음, 같은 PC 전용 주소와 실행마다 바뀌는 비밀값).
- 실측(로컬 근거 `.backups/verification/2026-10-10-ops-tool-v1/measure/report.md`): 선택 SDK `10.0.301`에 ASP.NET Core 런타임 `10.0.9`가 있다. WSL 안에서 127.0.0.1에 묶은 수신 대기는 Windows의 127.0.0.1로 닿았고, 이더넷·Default Switch 주소로는 닿지 않았다. 같은 조건에서 0.0.0.0에 묶으면 이더넷 주소로 닿았다. 떼어 낸 WSL 프로세스는 180초 생존했지만 그때 다른 WSL 연결이 있어 연결 0 수명은 확인하지 못했다.
- 게임 서버: 표준 입력의 Enter로 정상 종료한다(`02_Server/GameServer/Program.cs:20-23`). 포트는 7777 고정이다(같은 파일 14행). 맵 파일은 빌드 출력의 `Maps/`로 복사된다(`GameServer.csproj:25-30`, `Maps/MapDataLoader.cs:21`).
- 개발 실행의 7777 잠금: `$HOME/.cache/dawnholder/locks/server-7777.lock`에 `flock -n`을 건다(`99_Tools/sync-wsl.sh:21,106-107`).

## 배치·이름·등록

| 경로 | 책임 |
|---|---|
| `05_Management/backend/ManagementBackend/ManagementBackend.csproj` | 관리 백엔드. `Microsoft.NET.Sdk.Web`, net10.0, AssemblyName·RootNamespace `Dawnholder.Management.Backend` |
| `05_Management/backend/ManagementBackend.Tests/` | 백엔드 시험. 기존과 같은 xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, xunit.runner.visualstudio 3.1.4. 네임스페이스 `Dawnholder.Management.Backend.Tests` |
| `05_Management/backend/GameServerStub/` | 시험 전용 가짜 게임 서버(콘솔 프로젝트). 시험 소유 |
| `05_Management/backend/backend-wsl.sh` | WSL 빌드·시험·실행 진입점(`build`, `test`, `run`) |

- 위치는 [CODE_CONVENTION 파일 위치와 이름](../../../00_Document/conventions/CODE_CONVENTION.md#파일-위치와-이름)의 `05_Management/`(운영 애플리케이션의 화면·기능과 해당 테스트)를 따른다. 시험 이름은 대상 옆 `<프로젝트>.Tests` 관례다.
- **솔루션·독립 목록에 등록하지 않는다.** 형식 검사의 대상 폴더가 `02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`로 고정이라(`99_Tools/Formatting/Execution.cs:64`), 05_Management 프로젝트를 `Dawnholder.slnx`나 `independent-projects.json`에 넣으면 등록 검사가 실패한다(`ProjectRegistration.cs:50-58`). 그 검사 코드는 Core 소유다. 개발 실행 helper도 네 폴더만 WSL로 복사한다(`sync-wsl.sh:48`).
- **V1.0 동안 CI에 넣지 않는다.** 운영툴 시험 CI 편입은 승인 범위에서 V1.0 뒤로 미뤘다(goal 「후속 후보」). 검증자가 `backend-wsl.sh test`를 WSL에서 실행한다. 이 동안 백엔드 회귀를 CI가 잡지 못하는 것이 남은 위험이다.
- 루트 `Directory.Build.props`(StyleCop 분석기)와 `.editorconfig`를 그대로 상속한다. 새 NuGet 패키지는 쓰지 않는다. ASP.NET Core는 SDK의 공유 프레임워크로 쓴다.

## 실행 형태

- 백엔드는 WSL Ubuntu에서 `dotnet Dawnholder.Management.Backend.dll [--config <경로>]`로 실행한다. 설정 파일이 없으면 기본값을 쓴다.
- `backend-wsl.sh`는 `05_Management/backend/`와 루트 `global.json`·`Directory.Build.props`·`.editorconfig`를 WSL 전용 복사본(`$HOME/.cache/dawnholder/management/backend-src`)으로 맞춘 뒤 빌드한다. SDK는 `99_Tools/Formatting/sdk.sh`의 `dawnholder_sdk`를 읽어 같은 순서로 고른다(그 파일은 고치지 않는다). Windows 원본에 bin·obj를 만들지 않는다.
- `run`은 빌드한 백엔드를 앞에서 실행한다(떼어 내지 않는다). 운영툴 창이 이 명령을 자식으로 잡는 방식은 PR2, 창을 닫거나 운영툴을 완전히 끈 뒤의 수명은 PR3에서 정한다. 실측 M3가 연결 0 수명을 확인하지 못했기 때문이다.

### 설정

| 키 | 기본값 | 뜻 |
|---|---|---|
| `listenPort` | `47321` | 127.0.0.1에서만 듣는 관리 포트. `0`이면 빈 포트를 고른다(시험용). 사용 중이면 시작 실패 |
| `dataDirectory` | `$HOME/.local/share/dawnholder/management` | 연결 파일·서버 기록·로그·운영 실행본의 뿌리 |
| `server.displayName` | `Dawnholder 라이브 서버` | 화면 표시 이름 |
| `server.port` | `7777` | 게임 서버가 듣는 포트(감시용). 게임 서버에 전달하지 않는다 |
| `server.portLockFile` | `$HOME/.cache/dawnholder/locks/server-7777.lock` | 개발 실행과 공유하는 잠금 파일 |
| `server.dotnetPath` | 환경 변수 `DOTNET_HOST_PATH`(`backend-wsl.sh`가 `sdk.sh`로 고른 값) | 게임 서버 실행·실행본 빌드 호스트. 둘 다 없으면 설정 오류 |
| `server.environment` | 없음 | 게임 서버 프로세스에 더할 환경 변수(시험의 가짜 서버용) |
| `server.startTimeoutSeconds` | `12` | 시작 뒤 포트 대기 확인까지 기다리는 시간 |
| `server.stopTimeoutSeconds` | `15` | 정상 종료 요청 뒤 기다리는 시간 |
| `release.sourceRepository` | 없음 | 운영 실행본을 만들 Git 저장소의 WSL 절대 경로(이 PC에서는 메인 checkout `/mnt/c/Dev/DawnHolder_Project`를 설정 파일에 적는다). 비어 있으면 실행본 API가 `409 releaseSourceNotConfigured` |
| `release.projectPath` | `02_Server/GameServer/GameServer.csproj` | publish할 프로젝트(저장소 상대) |
| `release.archivePaths` | `02_Server/GameServer`, `02_Server/Network`, `98_Shared`, `Directory.Build.props`, `global.json`, `.editorconfig` | 빌드에 꺼낼 경로 |
| `release.entryAssembly` | `GameServer.dll` | 실행본 안의 진입 어셈블리 |
| `release.buildTimeoutSeconds` | `600` | 실행본 빌드 상한 |
| `logs.retentionDays` | `7` | 서버당 일반 로그 보존 기간 |
| `logs.retentionBytes` | `1073741824` | 서버당 일반 로그 보존 용량(1 GiB) |
| `logs.retentionIntervalSeconds` | `600` | 실행 중 정리 주기(1 이상 정수) |

설정 파일은 JSON이고 표의 점 이름은 중첩 객체다(예: `{ "listenPort": 0, "server": { "port": 40001, "environment": { "K": "V" } }, "release": { "archivePaths": [ "…" ] }, "logs": { … } }`). 시간·용량은 JSON 정수다. 설정 파일 기본 위치는 `$HOME/.config/dawnholder/management/backend.json`(저장소 밖)이고 없으면 위 기본값을 쓴다. `backend-wsl.sh run`은 그 파일이 있으면 `--config`로 넘긴다. 설정 값은 시작 때 검증한다(절대 경로, 포트 범위, 양수 시간·용량). 잘못된 설정은 잘못된 키 이름(예: `dataDirectory`, `listenPort`)을 포함한 이유를 출력하고, 연결 파일을 만들지 않고, 0이 아닌 코드로 끝난다.

## 상태 소유와 수명

| 상태 | 단일 소유자 | 바꾸는 경로 | 실패·종료 때 |
|---|---|---|---|
| 게임 서버 프로세스·실행 상태 | 서버 감독(한 객체) | 시작·정상 종료·강제 종료 API, 프로세스 종료 관측 | 종료를 관측하면 종료 종류를 기록하고 7777 잠금을 푼다. 백엔드 종료 때는 정상 종료를 요청하고 시간 초과면 강제 종료한 뒤 기록한다 |
| 7777 잠금 | 서버 감독 | 시작 직전 획득, 서버 종료 관측 뒤 해제 | 시작 실패면 즉시 해제. 잠금 파일은 지우거나 비우지 않는다 |
| 운영 실행본·현재 운영 버전 | 실행본 저장소 | 빌드 API, 현재 버전 지정 API | 빌드 실패면 부분 출력과 작업 폴더를 지우고 현재 버전을 바꾸지 않는다 |
| 로그 파일 | 로그 저장소 | 서버 출력 수집, 보존 정리 | 쓰기 실패는 상태에 드러내고 서버를 멈추지 않는다. 실행 중 구간은 정리하지 않는다 |
| 서버·실행 기록 | 서버 기록 저장소 | 백엔드 첫 시작, 서버 시작·종료 | 끝 시각 없는 실행 기록은 백엔드 재시작 때 「결과 모름」으로 닫는다 |
| 연결 파일·비밀값 | 접근 경계 | 백엔드 시작·종료 | 정상 종료 때 자기 내용일 때만 지운다 |

- 서버 명령(시작·정상 종료·강제 종료)은 한 번에 하나다. 처리 중에 다른 서버 명령이 오면 `409 busy`다. 단 강제 종료는 정상 종료를 기다리는 중에도 받는다.
- 실행본 명령(빌드·현재 버전 지정)은 따로 한 번에 하나다. 서버가 실행 중이어도 빌드와 현재 버전 지정은 된다. 바뀐 현재 버전은 다음 시작부터 쓴다.
- 「실행 중」 판단은 저장된 기록이 아니라 살아 있는 프로세스와 포트 관측으로만 한다.

## 관리 접근 경계

- 127.0.0.1에만 묶는다. 다른 주소에는 묶지 않는다.
- 시작마다 32바이트 난수 비밀값을 만든다. 모든 요청은 `Authorization: Bearer <비밀값>`이 맞아야 한다. 비교는 시간 일정 비교로 한다. 없거나 틀리면 `401 unauthorized`다.
- 원격 주소가 루프백이 아니거나, `Origin` 헤더가 있거나, `Host`가 `127.0.0.1:<포트>`·`localhost:<포트>`가 아니면 `403 forbidden`이다. 인증보다 먼저 검사한다.
- 요청 본문은 16 KiB 이하다. 넘으면 `413`이다.
- 연결 파일 `<dataDirectory>/connection.json`에 `{ "port", "token", "pid", "startedAt" }`를 쓴다. 권한 0600으로 만들고 임시 파일 뒤 이름 바꾸기로 교체한다. 수신 대기가 실제로 열린 뒤에 쓴다.
- 같은 PC에서 사용자 권한으로 도는 다른 프로그램이 이 파일을 읽는 것은 막지 못한다(사용자 결정 3의 받아들인 한계).

## API

모든 응답은 JSON이고 이름은 camelCase, 시각은 UTC ISO 8601(`Z`)이다. 오류는 `{ "error": "<코드>", "message": "<한국어 설명>" }`다. 성공은 2xx다. 시작·강제 종료·실행본 빌드·현재 버전 지정의 성공 본문 모양은 정하지 않고, 결과는 `GET /api/status`·`GET /api/releases`로 확인한다. `POST /api/server/stop`의 성공 본문은 상태 응답과 같은 모양이고 `server.state`가 `stopping`이다.

| 메서드·경로 | 하는 일 | 주요 오류 |
|---|---|---|
| `GET /api/status` | 백엔드·게임 서버·운영 버전·마지막 종료 상태 | — |
| `POST /api/server/start` | 현재 운영 버전으로 게임 서버 시작, 포트 대기 확인까지 기다림 | `409 busy`, `409 alreadyRunning`, `409 noCurrentRelease`, `409 portBusy`, `500 startFailed` |
| `POST /api/server/stop` | 표준 입력에 줄바꿈을 보내고 바로 `stopping`으로 답함 | `409 busy`, `409 notRunning` |
| `POST /api/server/force-stop` | 자기 게임 서버 프로세스를 강제 종료하고 종료를 기다림 | `409 notRunning` |
| `GET /api/logs` | 최근 로그 조회 | `400 invalidQuery` |
| `GET /api/releases` | 운영 실행본 목록과 현재 운영 버전 | — |
| `POST /api/releases` | 본문 `{ "commit" }`의 운영 실행본 빌드(끝날 때까지 기다림) | `400 invalidCommit`, `404 commitNotFound`, `409 busy`, `409 releaseSourceNotConfigured`, `500 buildFailed` |
| `PUT /api/releases/current` | 본문 `{ "commit" }`을 현재 운영 버전으로 지정 | `400 invalidCommit`, `404 releaseNotFound`, `409 busy` |

### 상태 응답

```json
{
  "backend": { "startedAt": "…", "pid": 123 },
  "server": {
    "serverId": "…", "displayName": "…",
    "state": "stopped | starting | running | stopping",
    "pid": null, "runId": null, "startedAt": null,
    "release": null,
    "stopRequestedAt": null, "stopTimedOut": false,
    "port": 7777, "portListening": false,
    "portOwner": "none | self | other | unknown", "portOwnerDetail": null,
    "portLockHeldByOther": false
  },
  "currentRelease": { "commit": "…", "builtAt": "…" },
  "lastExit": { "runId": "…", "endedAt": "…", "kind": "graceful | forced | abnormal | startFailed | unknown", "exitCode": 0, "signal": null }
}
```

- `release`는 지금 실행 중인 실행본이고 `currentRelease`와 같은 `{ "commit", "builtAt" }` 모양이다. `currentRelease`와 다를 수 있다(다음 시작부터 적용). 실행본이 하나도 없으면 `currentRelease`는 `null`, 종료 기록이 없으면 `lastExit`는 `null`이다.
- 시작 실패(`startFailed`) 뒤 `server.state`는 `stopped`이고 `lastExit.kind`는 `startFailed`다.
- `portOwner`가 `other`면 `portOwnerDetail`에 관측한 PID·명령 이름을 넣고, 알 수 없으면 `unknown`이다. 다른 실행이 잠금을 쥐고 있으면 `portLockHeldByOther`가 `true`다. 남의 프로세스는 끄지 않는다.
- `stopTimedOut`은 정상 종료 요청 뒤 `stopTimeoutSeconds`가 지났는데 아직 살아 있을 때 `true`다. 화면은 이때 강제 종료를 따로 묻는다(PR2).

### 실행본 목록 응답

```json
{
  "releases": [ { "commit": "…", "builtAt": "…", "sdkVersion": "…", "sourceRepository": "…" } ],
  "currentRelease": { "commit": "…", "builtAt": "…" }
}
```

- `releases`의 각 항목은 그 실행본 `manifest.json`의 네 필드와 같다. 실행본이 없으면 빈 배열이고 `currentRelease`는 `null`이다.

### 로그 조회

`GET /api/logs?minutes=10&contains=error&contains=warn&limit=1000&runId=<선택>`

| 매개변수 | 범위 | 기본 |
|---|---|---|
| `minutes` | 1~1440 정수 | 10 |
| `contains` | 0~5개, 각 1~64자, 대소문자 무시, 하나라도 들어 있으면 일치 | 없음 |
| `limit` | 1~5000 정수 | 1000 |
| `runId` | 기록에 있는 실행 식별(기록에 없는 값은 `400 invalidQuery`) | 모든 실행 |

응답:

```json
{
  "serverId": "…", "from": "…", "to": "…",
  "lines": [ { "runId": "…", "seq": 1, "collectedAt": "…", "stream": "stdout | stderr", "text": "…", "textTruncated": false } ],
  "truncated": false,
  "retention": { "oldestCollectedAt": "…", "totalBytes": 0, "recentDeletions": [ { "from": "…", "to": "…", "bytes": 0, "reason": "age | size" } ] }
}
```

- 줄은 수집 시각 오름차순이다. `limit`을 넘으면 가장 최근 줄을 남기고 `truncated: true`다.
- 게임 서버 로그 줄에는 원래 시각이 없다. `collectedAt`은 백엔드가 그 줄을 읽은 시각이며 발생 시각으로 쓰지 않는다. 화면도 「수집 시각」으로 표시한다.
- 읽기 실패는 빈 결과가 아니라 오류로 답한다.

## 게임 서버 실행 수명

1. **시작:** 현재 운영 버전이 없으면 `noCurrentRelease`. 잠금 파일을 열어(없으면 만들고, 비우지 않음) `flock(LOCK_EX|LOCK_NB)`를 건다. 실패면 `portBusy`(`portLockHeldByOther: true`). 잠금을 얻은 뒤 포트가 이미 대기 중이면 잠금을 풀고 `portBusy`.
2. 새 `runId`를 만들고 `<dotnetPath> <실행본>/<entryAssembly>`를 실행본 폴더를 작업 폴더로 해서 띄운다. 표준 입력은 파이프로 열어 둔다(닫히면 게임 서버가 바로 멈춘다). 표준 출력·오류는 줄 단위로 로그 저장소에 넘긴다.
3. `startTimeoutSeconds` 안에 그 PID가 `server.port`에서 대기하면 `running`. 그 전에 프로세스가 끝나거나 시간이 지나면 프로세스를 정리하고 잠금을 푼 뒤 `startFailed`와 최근 로그 몇 줄을 돌려준다.
4. **정상 종료:** 표준 입력에 줄바꿈 하나를 쓰고 `stopping`으로 답한다. 종료를 관측하면 `graceful`.
5. **강제 종료:** 자기가 띄운 PID에만 SIGKILL을 보내고 종료를 기다린다. 결과는 `forced`.
6. **요청 없는 종료:** `abnormal`. 종료 코드나 신호를 기록한다. 자동 재시작은 PR3다.
7. **백엔드 종료(SIGTERM·SIGINT):** 서버가 실행 중이면 정상 종료를 요청하고 `stopTimeoutSeconds`까지 기다린 뒤 남아 있으면 강제 종료한다. 실행 기록의 `exitKind`는 정상 종료로 끝났으면 `graceful`, 강제 종료했으면 `forced`이고, `endedAt`을 채우며, `reason`에 백엔드 종료 때문임을 나타내는 비지 않은 문자열을 남긴다.

7777 잠금은 개발 실행 helper와 같은 파일·같은 `flock` 방식이다. 운영 서버가 켜져 있는 동안 에이전트의 `sync-wsl.sh run`·`bot`은 잠금 실패로 거부된다. 반대로 개발 실행이 잠금을 쥐고 있으면 운영 서버 시작이 거부된다. .NET 기본 파일 잠금은 `flock`과 서로 막는지 확실하지 않으므로 `flock` 호출을 직접 쓴다.

## 운영 실행본

1. `commit`은 소문자 16진수 40자만 받는다. 저장소에 그 commit이 있는지 `git -c safe.directory=<저장소> -C <저장소> cat-file -e <commit>^{commit}`로 확인한다. 전역 Git 설정은 바꾸지 않는다.
2. `<dataDirectory>/build-work/` 아래 새 작업 폴더에 `git archive <commit> -- <archivePaths>`를 푼다. Windows 원본 checkout에서는 빌드하지 않는다. 공유 DLL 빌드가 `../03_Client/Assets/Plugins/Shared/`로 복사하는 부작용(`98_Shared/Shared.csproj` CopyToUnityPlugins)이 작업 폴더 안에만 남게 하기 위해서다.
3. `dotnet publish <projectPath> -c Release -o <releases>/<commit>.partial --disable-build-servers`를 `buildTimeoutSeconds` 안에 실행한다. 빌드가 끝난 뒤 MSBuild·컴파일러 서버 프로세스가 남지 않게 하기 위해서다. 시간이 넘으면 자기가 띄운 빌드 프로세스 트리를 끝내고 `buildFailed`로 처리한다. 성공하면 `manifest.json`(`commit`, `builtAt`, `sdkVersion`, `sourceRepository`)을 쓰고(`sdkVersion`은 작업 폴더에서 `<dotnetPath> --version`이 낸 값이라 꺼낸 `global.json`을 따른다) `<releases>/<commit>`로 이름을 바꾼다. 빌드 출력은 `<releases>/<commit>.build.log`에 남긴다. 작업 폴더는 지운다.
4. 같은 commit의 실행본이 이미 있으면 다시 빌드하지 않고 그대로 돌려준다.
5. 현재 운영 버전은 `<releases>/current.json` 하나가 가리킨다. 지정 API로만 바뀐다. 개발 checkout을 고치거나 빌드해도 바뀌지 않는다.

## 로그 저장과 보존

- 위치: `<dataDirectory>/servers/<serverId>/runs/<runId>/log-<6자리 번호>.jsonl`. 한 파일이 16 MiB를 넘으면 다음 번호로 넘긴다. 한 줄은 `{ "seq", "collectedAt", "stream", "text", "textTruncated" }`이고 `text`는 8,192자에서 자르고 표시한다.
- 정리: 백엔드 시작 때, 실행이 끝날 때, 실행 중에는 `logs.retentionIntervalSeconds`(기본 600초)마다 서버별로 한다. 파일의 마지막 수집 시각(그 파일 마지막 줄의 `collectedAt`)이 `retentionDays`보다 오래된 파일과, 합계가 `retentionBytes`를 넘는 동안 가장 오래된 파일부터 지운다. 실행 중인 실행의 마지막 파일은 지우지 않는다. 지운 구간은 `<serverId>/retention-log.jsonl`에 남기고 로그 조회 응답의 `retention`에 보인다.
- 조사 근거의 별도 보존(합의 문서의 일반 정리 제외 대상)은 V1.0에 없다.

## 서버·실행 기록

- `<dataDirectory>/servers/<serverId>/server.json`: `serverId`(백엔드 첫 시작 때 만든 UUID, 이후 고정), `displayName`, `port`, `createdAt`. 서버는 하나다.
- `<serverId>/runs/<runId>/run.json`: `runId`, `startedAt`, `endedAt`, `pid`, `releaseCommit`, `exitKind`, `exitCode`, `signal`, `reason`.
- 끝 시각 없는 실행 기록은 백엔드 다시 시작 때 `exitKind: "unknown"`과 `endedAt`(닫은 시각)으로 닫는다.
- 저장된 기록만으로 실행 중이라고 판단하지 않는다([서버 등록·로그 합의](../2026-09-30-system-records/shared-read-agreements.md#서버-등록로그-합의)).

## 시험 계획

- **TDD:** 신규 `claude-opus-5-5`가 이 문서의 계약으로 실패하는 시험을 먼저 쓴다. 구현자는 같은 명령을 통과시킨다.
- **블랙박스:** 시험은 빌드한 백엔드를 별도 프로세스로 띄우고 연결 파일로 포트·비밀값을 얻어 HTTP로 검사한다. 내부 클래스에 기대지 않는다. 설정은 임시 폴더(`listenPort: 0`, 임시 `dataDirectory`·잠금 파일)를 쓰고 7777을 쓰지 않는다.
- **가짜 게임 서버:** `GameServerStub`은 환경 변수로 받은 포트에서 대기하고, 출력·오류에 줄을 쓰고, Enter에 0으로 끝난다. 모드로 「Enter 무시」, 「몇 초 뒤 비정상 종료」, 「대기 전에 종료」를 낸다. 실행본 빌드 시험은 임시 Git 저장소에 이 프로젝트를 넣고 `release.projectPath`·`archivePaths`·`entryAssembly`를 그쪽으로 맞춘다. 임시 저장소는 저장소 밖이라 루트 분석기 패키지가 끼지 않는다.
- **Linux 전용:** 프로세스·`flock`·`/proc` 시험은 Linux가 아니면 건너뛴다(기존 `LinuxFact` 관례). 실행은 `backend-wsl.sh test`.
- **실제 진입 1회(검증자):** 실제 저장소의 commit으로 진짜 GameServer 실행본을 빌드하고 7777로 시작해 대기를 확인한 뒤 정상 종료를 확인한다. 7777을 쓰기 전에 Core·World·Content 리드에게 알린다(goal 「적용 중인 메인 결정」).

## PR1에서 하지 않는 것

화면·Electron 연결, 자동 재시작·알림·트레이, 운영자 로그인, 접속자 목록, 게임 서버·개발 helper·형식 검사 코드 변경, 솔루션·CI 등록, Docker.
