# 공용 실행 잠금

`execution-lock.mjs`는 명시적으로 맡긴 Unity/WSL 명령의 대기·admission·수명을 조율한다. Node 표준 라이브러리와 Windows PowerShell/CIM을 사용하며 설치·전역 설정은 필요 없다. 모든 IDE/셸 직접 실행을 막는 가드는 아니다. 실행 권한과 자원 소유 확인은 호출자의 계약을 따른다.

## 진입과 요청

```powershell
node 99_Tools/ExecutionLock/execution-lock.mjs .backups/inspect-request.json
node 99_Tools/ExecutionLock/execution-lock.mjs .backups/run-request.json
```

경로 하나만 받으며 요청은 UTF-8 JSON, 최대 1MiB다. 예제의 경로를 실제 기존 절대 경로로 바꾼다. `repoRoot`는 저장소/worktree 안의 디렉터리다.

```json
{ "version": 1, "action": "inspect", "repoRoot": "C:/project" }
```

`inspect`는 OS와 committed 상태를 조회하며 상태·잠금·근거 디렉터리를 만들지 않는다. `observed`는 실행 허가가 아니다. 유효한 transaction이 남아 있어도 committed snapshot을 읽을 수 있으며, 손상·부분 기록·아직 없는 첫 snapshot은 오류다.

정상 쓰기의 pending·원자 교체와 조회가 겹치면 최대 1초 동안 읽기만 재시도한다. 열린 파일의 유효 committed 바이트와
앞뒤 상태 파일 목록·transaction owner 세대를 확인한다. 그 안에 일관된 snapshot을 얻지 못하면 마지막 실제 오류를
반환하며, pending 승격·잠금 취득·수리를 하지 않는다. OS 관측 실패도 빈 상태로 바꾸지 않는다.

```json
{
  "version": 1,
  "action": "run",
  "repoRoot": "C:/project",
  "evidenceDir": "C:/project/.backups/execution-evidence",
  "execution": {
    "kind": "unity",
    "part": "Content",
    "file": "C:/Program Files/nodejs/node.exe",
    "args": ["C:/project/harmless-command.mjs"],
    "cwd": "C:/project",
    "timeoutMs": 10000,
    "waitTimeoutMs": 0
  }
}
```

이 예제는 무해한 Node 파일을 조율 대상으로 실행하는 형태다. 실제 Unity/WSL 실행에는 승인된 실행파일과 argv를 넣는다. `file`·`cwd`·`evidenceDir`는 기존 접근 가능한 절대 경로이며 `evidenceDir` 아래 매번 새 `run-<requestId>/`를 만든다. 기존 결과는 덮어쓰지 않는다. 명령은 `shell:false`, stdin 무시, argv 배열 그대로 실행한다.

| 입력 | 허용 범위 |
|---|---|
| `execution.kind` | `unity`, `wsl` |
| `execution.part` | 공백·NUL 없는 1~64자. 우선 파트는 정확히 `Content`/`Core` |
| `execution.args` | 문자열 배열, 최대 256개·각 32768자, NUL 금지 |
| `execution.timeoutMs` | 1~3600000 정수 |
| `execution.waitTimeoutMs` | 0~300000 정수, 생략하면 60000 |

정의되지 않은 필드·잘못된 타입·없는 파일은 `input-error`다. `inspect`는 `execution`을 받지 않는다. 관측은 호출당 10초, transaction 취득·해제 재시도는 각각 15초, 상태 파일 교체 재시도는 1초로 한정한다. 정리는 5초 예산을 두되 진행 중 transaction과 관측의 상한만큼 늦어질 수 있다. 이 비용 상한은 단독 실행의 새 메모리 임계값이 아니다.

## 공통 상태와 admission

기본 상태는 canonical Git common directory 아래 `dawnholder-execution-lock/`다. 같은 저장소의 worktree들은 하나를 공유한다. 다른 저장소의 상태는 별개이며 외부 Unity/WSL은 OS 관측으로 대조한다. 실행 identity는 UUID와 runner PID/시작 시각, root PID/시작 시각/parent PID, 관측한 자식 identity를 묶는다.

- Unity 독립 묶음은 한 개다. 부모 관계가 확인된 import 자식은 같은 묶음이며 이름이 같아도 독립 root는 합치지 않는다.
- 자식은 부모의 시작 identity와 시작 순서를 함께 확인한다. 현재 부모보다 먼저 시작한 외부 Unity는 재사용된 부모
  PID만으로 묶음에 넣거나 외부 root에서 제외하지 않는다. 부모 세대를 확인할 수 없는 자식은 미확정 상태로 보존한다.
- 등록된 Unity 대기는 Content 우선, WSL 대기는 Core 우선이다. 같은 우선순위는 등록 순서·안정된 ID 순이며 실행 중 작업은 선점하지 않는다.
- WSL 명령은 하나다. `vmmem`, `VmmemWSL`, `wslservice.exe` 상주는 명령으로 세지 않는다. 외부 `wsl.exe`의 소유를 확정할 수 없으면 차단한다.
- Core WSL과 Unity는 두 번째 admission 직전 물리 여유 `6 × 1024³` bytes 이상과 commit 여유 `4 × 1024³` bytes 이상을 모두 만족해야 한다. 시작 순서와 관계없이 같으며 다른 파트 WSL은 Unity와 직렬화한다. 단독 실행에는 메모리 임계값을 추가하지 않는다.
- Windows 관측은 `Win32_PerfRawData_PerfOS_Memory`의 AvailableBytes와 CommitLimit−CommittedBytes, `Win32_Process`의 ProcessId/ParentProcessId/CreationDate다. 바이트 원값·시각·출처를 보존한다. 조회 실패·불완전 관측·관련 identity 누락은 허가가 아니다.

[state-store.mjs](state-store.mjs)는 관측한 owner와 barrier 파일을 전용 candidate 디렉터리에 완성한 뒤 `transaction/`으로 원자 rename하여 배타 구간을 얻는다. 구간 안에서 상태 읽기→새 직전 관측→대기/예약 판정→동기화한 임시 상태의 rename을 수행한다. 예약과 spawn 시도 표식을 먼저 저장한 후 실제 자식 PID를 연결한다. root가 종료돼도 관측한 자식이 살아 있으면 lease를 유지한다. 공개 전 candidate와 원자적 해제 뒤 retired 폴더는 실행 상태가 아니며, 다른 runner는 그 잔여를 삭제하지 않는다.

owner 소실 뒤에도 root/자식 종료가 complete 관측과 시작 identity로 증명돼야 회수한다. 같은 PID의 다른 시작 시각, 불완전 spawn 기록, 손상 상태, 남겨진 transaction/임시 상태는 보존하고 확인 방법을 반환한다. 강제 unlock은 없다. 종료 처리는 이 runner의 직접 ChildProcess handle과 현재 identity를 대조한 root만 대상으로 한다. 남은 자식은 임의 PID 종료 없이 상태를 유지하므로 취소 결과 뒤에도 admission이 막힐 수 있다.

취소·timeout 정산은 자기 root의 종료 이벤트 뒤 새 관측과 상태 저장을 마친다. 다른 기록의 PID 충돌은 그 기록을
보존하고 자기 실행의 감시를 계속한다. 시작 후 상태·관측 실패도 감시와 유한 정리를 거치며 `tool-error`와 원인·잔여를
남긴다. 이미 시작된 명령을 `blocked`로 보고하거나 다시 spawn하지 않는다.

중단된 transaction도 owner의 시작 identity와 부재, PID 재사용 없음, 기록된 묶음의 완전 종료, 미확정 spawn 없음이 증명돼야 회수한다. pending이 있다면 커밋된 유효 상태와 바이트가 완전히 같은 복사만 원시에 보존한 뒤 제거한다. 다른 내용/부분 기록은 승격하거나 지우지 않는다. 회수 경쟁은 UUID를 포함한 정확한 owner 파일의 삭제 성공자 하나만 끝낸다. barrier가 디렉터리를 비어 있지 않게 유지하므로 회수 중 다른 세대가 게시되지 않는다. owner 삭제 뒤 중단된 barrier만 남았다면 자동 복구하지 않는다. 정상 해제도 전용 retired 경로로 원자 이동한 뒤 자기 파일만 정리한다.

관측 중 transaction 세대가 바뀌면 이전 owner 정보로 새 상태를 회수하지 않고 취득부터 재검증한다. 해제 중 `EPERM`/`EACCES`/`EBUSY`는 같은 소유·목적 경로를 확인하면서 제한 시간 안에서 재시도하고 errno·경로·token·시각·횟수를 원시에 남긴다. 시간 초과나 소유 변경은 실패로 보존한다.

관측 사이에 생성·종료된 중간 프로세스까지 모든 OS/WSL 내부 수명을 포괄하는 감독자는 아니다. 임의 daemon을 만드는 명령에는 완료 보장을 확대하지 않는다. Windows WSL client와 WSL 내부 영속 서버·DB·컨테이너 수명은 다르다. 기존 `sync-wsl.sh` 원본별 잠금·포트·소유 marker는 계속 필요하다.

## 결과와 근거

stdout에는 결과 JSON 한 객체만 나온다. 고정 root 필드는 `version`, `status`, `exitCode`, `code`, `message`, `repair`, `requestId`, `repositoryCommonDir`, `repositoryKey`, `stateRoot`, `evidenceDir`, `executionId`, `commandExitCode`, `observationSource`, `observedAt`이다. 얻지 못한 값은 `null`이다. `inspect`에는 읽은 `state`와 `observation`도 담는다.

| status | wrapper exit | 의미 |
|---|---|---|
| `observed` | 0 | 읽기 조회 완료 |
| `succeeded` | 0 | 명령 exit0과 묶음 종료 확인 |
| `failed` | 실제 1~255 또는 1 | 명령 실패; 범위 밖 원 exit는 `commandExitCode`에 보존 |
| `blocked` | 1 | 시작 전 충돌·우선순위·메모리·대기 만료·identity 불일치 |
| `input-error` | 2 | 요청/파일 계약 오류 |
| `tool-error` | 2 | 관측·상태·spawn·근거 I/O 실패 |
| `timed-out` | 124 | 실행 시간 초과; 실제 정리/잔여는 원시 참조 |
| `cancelled` | 130 | 취소; 실제 정리/잔여는 원시 참조 |

새 실행 폴더에서 `request.json` 원본 바이트, `invocation.json`의 hash/argv/cwd/환경/저장소, `events.jsonl`의 관측/예약/spawn/cleanup/exit, 자식 `stdout.txt`·`stderr.txt`, `result.json`·`exit.txt`를 찾는다. 상태에도 해당 실행 근거 경로를 연결한다. 실패 시 `code`·`message`·`repair`와 실제 남은 상태를 함께 확인한다.

## 시험과 지원 경계

```powershell
$env:EXECUTION_LOCK_TESTS_WORK = 'C:/project/.backups/execution-tests/work'
node --test --test-concurrency=1 99_Tools/ExecutionLock.Tests/execution-lock.test.mjs
node --test --test-concurrency=1 `
  99_Tools/ExecutionLock.Tests/lifetime-cleanup.test.mjs `
  99_Tools/ExecutionLock.Tests/ancestry-boundaries.test.mjs `
  99_Tools/ExecutionLock.Tests/inspect-consistency.test.mjs
```

work는 기존 절대 디렉터리로 직접 만들고 지정한다. 누락·상대 경로·없는 경로는 실패이며 TEMP fallback이나 skip이 없다. Windows Git 경로 길이에 맞게 허용 근거 안의 짧은 전용 work를 사용한다.

위 두 명령을 모두 실행해야 기본 회귀와 수명·조상 경계·조회 일관성 회귀를 포함한다. CI의 `*.test.mjs`도 같은
네 시험 파일을 포함한다. synthetic PID·시작 시각으로 재현한 재사용 snapshot은 실제 OS PID 재사용이나 Unity 실행이 아니다.

공개 `runCli(argv, dependencies = {})`의 argv는 `[requestPath]`다. 시험만 `observe`(async), `stateRoot`(격리 절대 경로), `signal`(AbortSignal)을 주입할 수 있다. 관측 반환은 `{ observedAt, complete, memory: { physicalAvailableBytes, commitAvailableBytes }, processes: [{ pid, parentPid, startedAt, name }] }`다. 시각은 ISO UTC, PID/parent는 정수다. seam을 쓰면 source와 원시는 `injected`, 실제 Windows는 `windows`다. JSON·환경변수·CLI 플래그로 대역을 켜는 옵션은 없다. import만으로 실행/상태 쓰기를 하지 않는다.

[code-rules](../../.github/workflows/code-rules.yml)는 별도 회귀에서 `EXECUTION_LOCK_TESTS_WORK`를 job 전용 근거 폴더에 설정하고 command/stdout/stderr/exit와 시험 자료를 기존 artifact에 보존한다. Linux CI는 대역 관측과 무해한 실제 Node 자식 경로를 확인한다. Windows 실제 CLI 관측은 따로 확인하며 실제 Unity/WSL/게임/DB 검증을 뜻하지 않는다. 기본 native adapter는 Windows만 지원하며 다른 OS는 `tool-error`다.

책임별 진입은 [contract.mjs](contract.mjs) 입력/결과·Git identity → [state-store.mjs](state-store.mjs) 원자 저장 → [admission.mjs](admission.mjs) 관문/묶음 → [runner.mjs](runner.mjs) 명령 수명이다. [observation.mjs](observation.mjs)·[observe-windows.ps1](observe-windows.ps1)은 OS 경계, [evidence.mjs](evidence.mjs)는 원시를 소유한다.
