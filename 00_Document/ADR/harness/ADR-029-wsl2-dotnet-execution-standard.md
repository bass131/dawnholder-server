### ADR-029: SAC(Smart App Control) dotnet 실행 차단 — WSL2 실행 표준 채택 (로컬 테스트 부활)

**날짜**: 2026-06-07 (세션17)
**상태**: 채택됨 (PoC 전 항목 통과 — 본문 "PoC 박제" 참조)
**결정**: SAC가 켜진 Windows 머신에서 **dotnet 런타임 실행(게임서버 / 헤드리스 봇 / `dotnet test`)은 WSL2 Ubuntu를 표준 경로**로 한다. Windows 쪽 `dotnet build`는 유지 — ADR-010 Unity Plugins DLL 복사 파이프라인의 전제이고, SAC는 *빌드*가 아니라 *CoreCLR 로드*를 차단하므로 빌드 자체는 무관. Unity 에디터는 Mono 로더라 SAC 비대상 — 변경 없음. 세션16의 "SAC 게이트 은퇴 = 로컬 dotnet test 포기, CI 단독 안전망" 결정을 **부분 supersede** — CI(ubuntu)는 원격 안전망으로 그대로 유지하되, 로컬 빠른 피드백이 WSL2로 부활한다.

**운영 규칙**:

- (a) WSL 빌드/실행은 **WSL 내부 파일시스템 복사본**(`~/dawnholder-poc` — 차기 정리 시 `~/dawnholder`로 개명 가능)에서 수행. `/mnt/c` 저장소 직접 빌드 금지 — Windows obj/bin 교차 오염 + 9p IO 느림.
- (b) 동기화 = rsync 한 줄 (소스 디렉토리 4개 + slnx + global.json, bin/obj 제외). bake된 `98_Shared/GameData/Maps/*.bin`은 소스 트리 소속이라 자동 포함:
  ```bash
  wsl -d Ubuntu -- bash -lc "cd /mnt/c/Dev/ClaudeDev && rsync -a --delete --exclude 'bin/' --exclude 'obj/' Dawnholder.slnx global.json 02_Server 98_Shared 99_Tools 04_ClientNet ~/dawnholder-poc/"
  ```
  **sync → build → run을 한 묶음으로** — sync 누락 시 옛 코드로 실행되는 stale 함정이 본 구조의 1순위 위험.
- (c) 서버는 WSL 안에서 7777 바인딩. **Windows 쪽 7777을 비워두면** wslrelay의 localhost 포워딩이 Unity(127.0.0.1:7777) 접속을 WSL2 서버로 연결한다. Windows dotnet 서버 잔존 시 0.0.0.0:7777 바인딩이 포워딩을 가로챔 — 전환 시 잔존 프로세스 정리 의무 (`netstat -ano | findstr 7777`).
- (d) SDK는 dotnet-install.sh 사용자 홈 설치(`~/.dotnet`, sudo 불필요) + libicu 의존(Ubuntu 26.04 = `libicu-dev`). **sudo가 필요한 설치류는 본인 별도 터미널 의무** — Claude 세션 백그라운드 실행은 TTY가 없어 `sudo: timed out` 실측. 원격 스크립트 다운로드+실행은 Claude 권한 게이트(curl deny) 대상이라 본인 `!` 실행 경로.
- (e) "신뢰 바이트 우회"(git HEAD에 커밋된 실행 이력 있는 dll을 bin에 복사)는 **비표준 응급 경로**로만 — 소스가 변경된 어셈블리에는 무력하고, git에 박제된 dll(Plugins 복사본)에만 적용 가능.

**이유**: SAC는 처음 보는(평판 없는) unsigned dll의 CoreCLR 로드를 차단한다 (0x800711C7). 세션 실측 4건이 결정 근거 — ① 세션16: `dotnet test` 차단 → CI 신설로 분담 ② 세션17: `--no-incremental` 풀 리빌드 후 서버 기동 실패 — entry `GameServer.dll`/`ServerCore.dll`은 통과하고 `Shared.dll`만 차단 = **차단 대상 선정이 비결정적("빌드 룰렛")**, 어떤 빌드 후에 뭐가 막힐지 예측 불가 ③ git HEAD에 박제된 세션16 바이트(실행 이력 있음)는 즉시 통과 → SAC 평판이 *파일 바이트(해시) 단위*임을 확인 ④ 소스 불변 리빌드에도 dll 바이트가 drift (비결정 요소 원인 미상 — 결정론 빌드 조사는 보강 백로그). SAC는 per-file/per-folder 예외 설정이 존재하지 않고, Off 전환은 비가역(재활성화 = Windows 재설치)이라 사용자가 보안 유지를 선택 (2026-06-07 의논, AskUserQuestion 3안 중 WSL2 표준 채택). WSL2는 Linux ELF 로더라 SAC(Windows PE 정책)가 원천 비적용이며, CI가 이미 ubuntu에서 동일 스위트를 돌리고 있어 Linux 환경 등가성은 사전 검증돼 있었다.

**PoC 박제 (2026-06-07, 채택 게이트)**: WSL2 Ubuntu 26.04 (기존 설치 — Riot Vanguard 간섭 의심은 기우로 확인, memory 등재 리스크 해소) + .NET SDK 10.0.300 (`global.json` 10.0.203 + latestFeature 충족):

| # | 항목 | 결과 |
|---|---|---|
| ① | `dotnet build Dawnholder.slnx` (WSL 내부) | 5.68s, 경고 0 / 오류 0 |
| ② | 서버 기동 + 신규 terrain.bin(발판) 로드 | 20 TPS 정상 |
| ③ | 봇 M2BasicMovement | success=True, desync (0.00, 0.00) |
| ④ | `dotnet test` 풀스위트 | **392/0/4 — CI 숫자와 동일** (로컬 테스트 부활) |
| ⑤ | Windows→wslrelay→WSL2 TCP 7777 | 접속 OK (Unity Play 경로 검증) |

**트레이드오프**: ① 환경 2벌 (Windows 빌드 = Unity DLL 공급 / WSL = 실행) — rsync 한 단계 추가, sync 누락 = stale 코드 실행 함정 ② WSL 상주 메모리 비용 (idle 자동 반환되나 수 GB 가능) ③ sudo 설치류는 Claude 위임 불가 (TTY + 권한 게이트 이중 차단 실측) — 환경 셋업에 본인 손 필수 구간 존재 ④ **LocalDB는 Linux 부재** — M5 영속화 진입 시 SQL Server Linux 컨테이너 vs Windows 실행 회귀 vs 원격 DB 결정 필요 (**명시 이월**) ⑤ 서버/봇 로그가 WSL 내부(`/tmp`)에 박힘 — Windows 도구로 직접 못 열고 `wsl` 경유 한 단계.

---

#### 부록 A — 표준 명령 모음 (재사용 자산)

```bash
# 1) 동기화 (Windows 쪽 변경 후 매번)
wsl -d Ubuntu -- bash -lc "cd /mnt/c/Dev/ClaudeDev && rsync -a --delete --exclude 'bin/' --exclude 'obj/' Dawnholder.slnx global.json 02_Server 98_Shared 99_Tools 04_ClientNet ~/dawnholder-poc/"

# 2) 빌드
wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && ~/.dotnet/dotnet build Dawnholder.slnx"

# 3) 테스트 (로컬 부활 — SAC 무관)
wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc && ~/.dotnet/dotnet test Dawnholder.slnx --no-build"

# 4) 서버 (백그라운드 — stdin 유지용 tail 파이프, ADR 본문 (c) 포트 규칙 참조)
wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc/02_Server/GameServer/bin/Debug/net10.0 && tail -f /dev/null | ~/.dotnet/dotnet GameServer.dll > /tmp/wsl-server.log 2>&1"

# 5) 봇 (시나리오명 교체)
wsl -d Ubuntu -- bash -lc "cd ~/dawnholder-poc/99_Tools/headless-bot/bin/Debug/net10.0 && ~/.dotnet/dotnet Dawnholder.Tools.HeadlessBot.dll --host 127.0.0.1 --port 7777 --scenario M2BasicMovement"
```

**백로그 (약속 아님 — 필요 시 승격)**: sync+build+run 래퍼 스크립트화 / 소스 불변 리빌드 바이트 drift 원인 조사(결정론 빌드) / 봇 CI 편입(기존 백로그)과 합류 검토.
