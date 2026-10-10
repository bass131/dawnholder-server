# 화면 연결 설계 — PR2

[운영툴 V1.0 goal](goal.md)의 PR2 「화면 연결」 설계다. 운영툴 창 프로세스가 [관리 백엔드](backend-design.md)를 켜고 연결해 「서버 운영」 화면에서 서버를 시작·종료하고 상태와 로그를 본다. PR1 검증에서 나온 백엔드 조정도 이 PR에 넣는다. 자동 재시작·알림·창을 닫은 뒤의 운영 지속(PR3)과 문서 반영(PR4)은 범위가 아니다.

## 근거

- 승인 범위: goal 「만들 것」 PR2 행, 「관찰 가능한 완료조건」 1·2·3·6·7·8. 화면의 바깥 연결 차단(`connect-src 'none'`)은 그대로 두고 창 프로세스가 백엔드와 대화한다.
- PR1 판정의 설계 관찰과 B1 실패(goal 「PR2 전에 판단할 것」). B1은 PR2 안에서 고친다(`msg_0967be03bc1d`).
- 현재 창 프로세스(`05_Management/frontend/electron/main.ts`): IPC는 읽기 전용 5채널이고 모두 `trustedSender`로 발신자를 확인한다(38-52행). checkout 위치는 앱 위치에서 구한다(31행). X 닫기는 트레이로 숨기고 트레이 「종료」만 앱을 끝낸다(83-121행).
- 관측: PR1 검증 중 WSL이 두 번 다시 켜져 `/tmp`가 비었다. 다른 WSL 연결이 없으면 유휴 종료가 일어난다고 추론한다(E/`pr1-verify/verdict.md`). 그래서 창 프로세스가 백엔드를 띄운 `wsl.exe`를 자식으로 쥐고 있는 동안만 백엔드 수명을 믿는다.

## 백엔드 조정

PR1 설계 문서의 해당 절을 이 내용으로 고친다.

| 번호 | 바꿀 것 | 이유(PR1 판정) | 관찰 가능한 결과 |
|---|---|---|---|
| B-1 | 명시한 `--config` 파일이 없으면 시작하지 않는다. 설정 파일의 모르는 키도 거부한다 | 관찰 4: 경로 오타 하나로 실제 데이터 폴더·7777 잠금을 쓴다 | 0이 아닌 종료 코드, 출력에 `config` 또는 모르는 키 이름, `connection.json` 없음 |
| B-2 | `backend-wsl.sh run`은 기본 설정 파일이 없으면 `release.sourceRepository`만 적은 기본 설정 파일을 만들고 한 줄 출력한다. 값은 그 스크립트가 속한 checkout의 Git 객체 저장소를 가진 작업 트리다. `.git`이 폴더면 그 checkout이다. `.git`이 `gitdir: <Windows 경로>/.git/worktrees/<이름>` 파일이면 `wslpath`로 바꾼 뒤 `/.git/worktrees/<이름>`을 뗀 원래 clone이다. 정할 수 없으면 만들지 않는다(`init-config`는 이유를 출력하고 0이 아닌 코드로 끝나며, `run`은 경고 한 줄 뒤 계속해 실행본 API가 `releaseSourceNotConfigured`를 낸다). 이 단계만 하고 끝나는 `backend-wsl.sh init-config`도 둔다. 이 동작은 WSL 복사본 잠금·복사·빌드보다 앞에서 처리한다 | PR1에서 `sourceRepository` 기본값이 없어 운영 버전을 만들 수 없다. 이 PC의 운영툴 checkout은 worktree라 `.git`이 Windows 경로를 가리켜 WSL Git이 그 폴더를 저장소로 읽지 못한다. worktree의 commit은 원래 clone과 같은 객체 저장소에 있다 | `HOME`을 임시 폴더로 둔 `init-config`가 일반 clone·worktree·이미 있는 파일 세 경우에 각각 그 checkout·원래 clone(이 PC는 `/mnt/c/Dev/DawnHolder_Project`)·변경 없음 |
| B-3 | 상태 조회는 서버가 꺼져 있을 때 공유 7777 잠금을 잡지 않는다. 다른 실행의 보유 여부는 `/proc/locks`에서 그 파일 inode의 `FLOCK` 항목으로 읽는다 | 관찰 7: 화면이 2초마다 물으면 개발 helper가 잘못 거부될 수 있다 | 상태 조회 중 다른 프로세스의 `flock -n`이 항상 성공, `portLockHeldByOther`는 그대로 맞음 |
| B-4 | 로그 조회와 보존 정리는 파일 목록만 수집 잠금 안에서 정하고 읽기는 잠금 밖에서 한다. 상태 응답은 로그 잠금을 기다리지 않는다 | 관찰 1: 1 GiB 조회 동안 상태 응답이 2초 기다린다 | 1 GiB 조회 중 보낸 상태 조회가 0.5초 안에 답한다(측정 조건은 시험 계약) |
| B-5 | `backend-wsl.sh run`은 Release 구성으로 빌드·실행한다 | 관찰 3 | 실제 진입에서 백엔드 프로세스의 명령줄(`/proc/<pid>/cmdline`)이 Release 출력 경로 |
| B-6 | B1이 다른 세션의 같은 포트 사용과 겹쳐도 판정이 흔들리지 않게 하고, 실패 메시지에 상태 JSON과 포트를 넣는다. 방식은 시험 작성자가 정한다(시험 소유자 작업) | B1 실패: 다른 세션 시험과 빈 포트 경합 | 다른 프로세스가 고른 포트에서 대기하는 상황을 시험이 만들어도 B1의 판정이 그 원인을 구분하고, 실패 때 상태 JSON·포트가 출력에 보임 |

관찰 2(게임 서버가 WSL 복사본 잠금 fd를 물려받음)는 PR3, 관찰 5·6·8은 화면 표시와 창 쪽 응답 검증으로 다룬다(아래).

바꾸지 않는 제약: `backend-wsl.sh run`은 백엔드가 끝날 때까지 WSL 복사본 잠금을 쥔다. 그래서 운영툴의 백엔드가 도는 동안 같은 WSL 사용자의 `backend-wsl.sh test`·`build`는 「another backend build, test or run owns the WSL copy」로 거부된다. 백엔드 코드는 운영툴을 연 checkout의 작업 트리에서 그때 빌드한다. 운영 버전과 달리 고정되지 않는다. 관찰 2와 같은 잠금이라 PR3에서 함께 본다. PR2 검증은 백엔드 시험과 실제 진입을 차례로 돌린다.

## 창 프로세스와 백엔드

### 단일 소유자

창 프로세스의 백엔드 연결 객체 하나가 백엔드 자식 프로세스, 연결 정보(포트·비밀값·pid), 연결 상태를 소유한다. IPC handler와 화면은 이 객체의 메서드만 부른다. 이 모듈은 MCP 공유 모듈(`tests/mcp-v1-boundary.test.ts` 목록)에서 import하지 않는다.

연결 상태: `starting`(백엔드를 띄우는 중), `connected`, `disconnected`(이유 코드와 함께), `stopping`(앱 종료 중).

### 모듈 배치

기존 기록·백로그 기능의 계약·I/O 분리(`backlog-contract.ts`·`backlog-store.ts`)를 따른다. 경로는 `05_Management/frontend/` 기준이다.

| 파일 | 책임 |
|---|---|
| `electron/server-operations-contract.ts`(새, 순수) | IPC 입력 검증, 백엔드 응답 검증(`unknown` → 타입), 오류 코드 → 한국어 문구 표, Windows 경로 → WSL 경로, 결과·연결 상태·bridge 타입 |
| `electron/backend-connection.ts`(새, I/O) | `createBackendConnection(…)`. 백엔드 자식 프로세스·연결 정보·연결 상태의 단일 소유자. 자식 프로세스 시작, `wsl.exe --exec` 한 번 실행, 127.0.0.1 HTTP, 시계를 주입받아 시험에서 대역을 쓴다. 기본 구현은 `node:child_process`·`node:http`다 |
| `electron/main.ts` | 연결 객체를 만들고 앱 준비 때 연결을 시작한다. `server-operations:*` handler를 등록하고(`trustedSender` 먼저), 트레이 「종료」의 확인과 연결 종료를 한다 |
| `electron/preload.cts` | `serverOperations` 노출 |
| `src/serverOperationsBridge.d.ts`(새) | renderer의 `window.serverOperations` 타입 |
| `src/ServerOperations.tsx`(새) | 「서버 운영」 화면. `App.tsx`의 `Operations`가 이 화면을 그린다 |

연결 객체의 메서드 이름과 주입 대역의 모양은 선행 시험 작성자가 위 책임 안에서 정하고 보고에 적는다. 구현자는 시험을 고치지 않고 그 모양을 따른다.

### 시작과 붙기

1. 앱이 준비되면 자동으로 연결을 시작한다.
2. 먼저 이미 도는 백엔드가 있는지 본다. `wsl.exe -d Ubuntu --exec cat <연결 파일>`로 연결 파일을 읽는다. 그 pid가 살아 있고 그 비밀값으로 `GET /api/status`가 200이면 그 백엔드에 붙는다. 이 경우 자식 프로세스는 없다.
3. 없으면 `wsl.exe -d Ubuntu --exec bash <checkout의 WSL 경로>/05_Management/backend/backend-wsl.sh run`을 자식으로 띄운다. checkout의 WSL 경로는 `main.ts`의 checkout 위치(`C:\…`)를 `/mnt/<소문자 드라이브>/…`로 바꾼 값이다. 셸 문자열을 만들지 않고 인자 배열로 넘긴다.
4. 새 연결 파일이 생기고(이전 파일과 `startedAt`·`pid`가 다름) `GET /api/status`가 200이 될 때까지 기다린다. 상한은 120초다(첫 빌드 포함). 넘으면 자식을 끝내고 `disconnected: startTimeout`.
5. 연결 파일 위치: 기본은 WSL `$HOME/.local/share/dawnholder/management/connection.json`이다. `$HOME`은 `wsl.exe -d Ubuntu --exec printenv HOME`으로 한 번 읽는다. 기본 설정 파일에서 `dataDirectory`를 바꾸면 창 프로세스가 연결 파일을 찾지 못한다. V1.0은 기본 데이터 폴더만 지원하고, 시간 초과 안내에 이 가능성을 적는다.
6. 검증용 바꿈: 환경 변수 `DAWNHOLDER_MANAGEMENT_BACKEND_CONFIG`(WSL 경로)가 있으면 `run --config <그 경로>`로 넘기고, `DAWNHOLDER_MANAGEMENT_DATA_DIR`(WSL 경로)가 있으면 그 아래 연결 파일을 읽는다. 둘은 함께 주거나 함께 비운다. 하나만 있으면 연결하지 않고 `disconnected: invalidOverride`.
7. 자식의 표준 출력·오류는 창 프로세스 콘솔에 줄 단위로 넘기되 비밀값이 섞일 수 있는 연결 파일 내용은 어디에도 기록하지 않는다.

### HTTP 대화

- 창 프로세스의 Node HTTP로 `http://127.0.0.1:<포트>`에 요청한다. `Authorization: Bearer <비밀값>`을 붙이고 `Origin`을 보내지 않는다. 화면(renderer)은 HTTP를 하지 않는다.
- 요청마다 시간 상한을 둔다: 상태·로그 5초, 시작·종료 30초, 강제 종료 30초, 실행본 빌드 660초(백엔드 상한 600초 + 여유).
- 응답은 계약 모듈이 `unknown`에서 검증한다. 모양이 다르면 `disconnected`로 바꾸지 않고 그 요청만 `invalidResponse` 실패로 돌린다. 연결 거부·시간 초과가 연속 3번이면 `disconnected: backendUnreachable`.
- 백엔드 오류 응답 `{ error, message }`의 `error` 코드는 한국어 메시지 표로 바꾼다. 표는 `05_Management/backend/ManagementBackend/`가 내는 코드 전부(`busy`, `alreadyRunning`, `noCurrentRelease`, `portBusy`, `startFailed`, `notRunning`, `invalidCommit`, `commitNotFound`, `releaseNotFound`, `releaseSourceNotConfigured`, `buildFailed`, `invalidQuery`, `logReadFailed`, `operationFailed`, `invalidRequest`, `bodyTooLarge`, `notFound`, `forbidden`, `unauthorized`)를 덮고, 모르는 코드는 일반 실패 문구로 보인다. 백엔드의 `message`는 보조로만 쓴다. `unauthorized`는 연결 파일이 바뀐 것이므로 연결 정보를 한 번 다시 읽는다.

### 백엔드가 죽거나 끊길 때

- 자식 `wsl.exe`가 끝나면 `disconnected: backendExited`와 종료 코드. 자동으로 다시 띄우지 않는다(자동 재시작은 PR3). 화면의 「다시 연결」이 시작과 붙기를 다시 한다.

### 앱 종료(트레이 「종료」) — 잠정

- 게임 서버가 실행 중이면 확인 창을 띄운다: 「서버가 실행 중입니다. 종료하면 서버도 정상 종료합니다.」 취소하면 앱을 끝내지 않는다.
- 끝낼 때는 `wsl.exe -d Ubuntu --exec kill -TERM <백엔드 pid>`를 보낸다. 백엔드가 서버를 정상 종료하고(시간 초과면 강제 종료) 스스로 끝난다(PR1 설계 「게임 서버 실행 수명」 7). 자식이 끝날 때까지 최대 30초 기다리고, 넘으면 `kill -KILL`을 보낸 뒤 앱을 끝낸다.
- 붙은 백엔드(자식 아님)도 같은 방식으로 끈다.
- X 닫기는 지금처럼 숨김이다. 앱이 살아 있으므로 백엔드와 서버는 계속 돈다.
- 이 동작은 PR3 질문 「운영툴 완전 종료 때 서버 처리」의 잠정 답이다. 창 프로세스가 끝나면 WSL 유휴 종료로 서버가 통제 없이 꺼질 수 있어서 정상 종료를 먼저 한다. PR3에서 사용자 결정으로 바꿀 수 있다.

## IPC 계약

preload는 새 객체 `serverOperations` 하나를 노출한다. 채널은 `server-operations:<동작>`이다. 모든 handler는 `trustedSender`를 먼저 확인한다.

| 동작 | 입력 | 하는 일 |
|---|---|---|
| `connection` | 없음 | 연결 상태와 이유 |
| `connect` | 없음 | `disconnected`일 때 시작과 붙기를 다시 함 |
| `status` | 없음 | `GET /api/status` |
| `start` | 없음 | `POST /api/server/start` |
| `stop` | 없음 | `POST /api/server/stop` |
| `force-stop` | 없음 | `POST /api/server/force-stop` |
| `logs` | `{ minutes, contains[], limit }` | `GET /api/logs`. 입력 범위는 백엔드 설계와 같고 창 쪽에서 먼저 거른다 |
| `release-candidate` | 없음 | checkout의 branch·HEAD commit(기존 `checkoutStore`)과 현재 운영 버전 |
| `release-build` | `{ commit }` | 40자 소문자 16진수이고 지금 checkout HEAD와 같을 때만 `POST /api/releases` |
| `release-select` | `{ commit }` | 같은 조건으로 `PUT /api/releases/current` |

- 결과는 기존 관례대로 `{ ok: true, … } | { ok: false, code, message }`다.
- 다른 commit을 고르는 화면은 만들지 않는다(되돌리기는 goal 「하지 않을 것」).
- 기존 읽기 전용 채널과 API 목록을 고정한 시험(`desktop-main.test.ts`, `records-ipc-preload.test.ts`, `diagram-asset-desktop.test.ts`, `backlog-ipc-preload.test.ts`)은 새 채널을 더한 목록으로 고친다. 기록 쪽 채널이 쓰기 권한을 얻지 않는다는 단정은 그대로 둔다.

## 화면

「서버 운영」 화면(`App.tsx`의 `Operations`)을 아래로 바꾼다. 데스크톱 bridge가 없으면(브라우저 개발 화면) 지금 화면과 미연결 안내를 그대로 보인다.

- **연결 안내:** 연결 전·실패 때 「관리 기능 미연결」과 이유를 보이고 「다시 연결」 버튼을 둔다. 연결되면 「관리 기능 연결됨」으로 바꾼다. 하단 상태 줄도 같은 상태를 보인다.
- **요약 줄:** 실행 상태(정지·시작 중·실행 중·종료 중, 시작 시각, pid), 7777 대기(대기 중·없음, 다른 실행이 쓰는 중이면 그 사실과 「남의 실행은 끄지 않습니다」), 운영 버전(실행 중 실행본 commit 앞 8자와 빌드 시각, 다음 시작에 쓸 현재 운영 버전이 다르면 둘 다). 접속 현황은 범위 밖이라 「접속 데이터 미수집」을 그대로 둔다.
- **운영 제어:** 「서버 시작」·「서버 종료」. 종료 요청 뒤 `stopTimedOut`이면 「강제 종료」 버튼과 확인 창을 따로 보인다. 처리 중에는 버튼을 막고 진행 상태를 보인다. 실패는 한국어 메시지로 보인다. 마지막 종료 종류(정상·강제·비정상·시작 실패·결과 모름)와 종료 코드를 보인다.
- **운영 버전 올리기:** checkout의 branch와 HEAD commit을 보이고 「이 commit으로 운영 버전 만들기」 → 빌드(수 분, 진행 표시) → 「현재 운영 버전으로 지정」 순서다. 바뀐 버전은 다음 시작부터 쓴다고 보이고, 커밋하지 않은 작업 트리 변경은 운영 버전에 들어가지 않는다고 함께 보인다. 개발 checkout을 고쳐도 운영 버전은 이 버튼을 누르기 전에는 바뀌지 않는다(완료조건 7).
- **서버 로그:** 기간(10·30·60분, 기본 10분), 찾을 낱말(최대 5개, 쉼표로 구분, 오류·경고 빠른 버튼), 「다시 읽기」. 서버가 실행 중이고 화면이 보이는 동안 5초마다 자동으로 다시 읽는다. 줄마다 「수집 시각」(현지 시각)·stdout/stderr·내용을 보이고 열 이름도 「수집 시각」이다. 잘림(`truncated`, `textTruncated`)과 보존 정보(가장 오래된 수집 시각, 최근 정리)를 보인다.
- **상태 갱신:** 「서버 운영」 화면이 보이고 창이 보이는 동안 2초마다 상태를 묻는다. 창이 숨겨지면 멈춘다. 요청 순번으로 늦게 온 응답을 버리고 실패 때는 마지막 정상 값을 「마지막 확인 시각」과 함께 남긴다.
- **종료 중 표시:** `stopping` 동안 `portOwner`가 `unknown`이어도 경고로 보이지 않는다(관찰 5).

## 시험

- **선행 시험(신규 `claude-opus-5-5`):**
  - 백엔드: B-1·B-2·B-3·B-4를 PR1과 같은 블랙박스 방식으로 더하고 B-6을 고친다. B-5는 시험 명령으로 `run`을 돌리지 않으므로 독립 검증의 실제 진입에서 확인한다. 같은 시험 명령(E/`pr1-tests/run-tests.sh`)을 쓴다.
  - 창 쪽: 계약 모듈(입력·응답 검증, 오류 메시지 표, WSL 경로 바꾸기), 연결 객체(자식 프로세스·HTTP·파일 읽기를 주입한 대역으로 시작·붙기·시간 초과·끊김·종료 순서), IPC handler(발신자 거부, 입력 거부, commit이 HEAD와 다를 때 거부), preload API 목록, 화면(연결 상태별 표시, 버튼 활성·비활성, 강제 종료 확인, 로그의 「수집 시각」, 상태 순번).
  - 기존 시험의 바뀌는 단정(미연결 고정, 채널 목록)은 요구 출처와 함께 고친다. 기존 실패 B01·B09는 지금 실패 그대로 분류만 한다.
- **구현:** 백엔드 조정과 창 쪽 구현을 각각 계약으로 나눈다.
- **독립 검증(신규 `claude-opus-5-5`):** 실사, 독립 시험, 실제 진입 1회. 실제 진입은 보조 디스플레이에서 실제 Electron 창을 띄워 앱 내부 수단(창 배치, `webContents` 안의 클릭, `capturePage`)으로 「운영 버전 만들기 → 지정 → 서버 시작 → 로그 보기 → 서버 종료 → 앱 종료」를 한 번 지난다. OS 합성 입력·전면화는 쓰지 않는다. 7777을 쓰기 전에 리드가 다른 리드에게 알린다. 검증용 바꿈 환경 변수로 임시 설정·데이터 폴더를 쓴다.
- 화면 확인은 README 「프런트엔드 개발」의 보조 디스플레이 규칙을 따른다.

## PR2에서 하지 않는 것

접속 현황·서버 안쪽 지표, 자동 재시작·Windows 알림·트레이 상태 표시, 창 프로세스가 끝난 뒤의 서버 지속(PR3), 다른 운영 버전 고르기·되돌리기, 운영자 로그인, 게임 서버·개발 helper 변경, 문서(README·requirements·decisions) 반영(PR4).
