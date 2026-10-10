# 운영툴 V1.0 — 서버 운영 기본

## 진척 단계

- [x] 범위 승인과 goal 고정
- [x] 실행 환경 실측
- [x] 백엔드 선행 시험
- [x] 백엔드 구현·검증
- [x] PR222 병합
- [>] 화면 연결 구현·검증
- [ ] 화면 PR 병합
- [ ] 중간 점검
- [ ] 장애 대응 구현·검증
- [ ] 장애 대응 PR 병합
- [ ] 마무리 PR 병합
- [ ] 종료 기록·Gardener

PR 번호가 생기면 「백엔드 PR 병합」 같은 단계 이름을 「PR000 병합」 형식으로 바꾼다.

## 재개 지점

**기록 시점: 2026-10-10 19:58 KST, PR222 병합 뒤.**

- **지금 단계:** PR1(PR222)이 병합됐다(아래 「PR222 병합」). PR2 branch `feat/server-operations-screen-20261010`을 최신 main `20630a90`에서 만들었다. 다음은 PR2 설계다. 아래 「PR2 전에 판단할 것」을 설계에 반영하고, 설계가 승인 범위를 바꾸면 구현 전에 메인에 알린다.
- **작업 경로:** `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- **Run:** `run_003b556f0ba8`(objective 「Management - 운영툴 V1.0」). 리드 handle은 이 세션의 관측값이며 다음 리드의 실행 권한이 아니다. 다시 열면 새 handle로 `orca orchestration run-use --id run_003b556f0ba8 --json` 뒤 메인에 `run:run_003b556f0ba8`을 알린다.
- **보류 중인 다른 goal:** 게임 소개 페이지 goal은 PR207 branch `feat/intro-site-20261008`(head `02fcd8a4`)에만 있고 main에는 없다. 10-13 교수 면담 뒤 재개가 정해지면 아래 「작업 공간과 소개 페이지」 순서를 따른다.
- **근거 폴더 E:** 저장소 로컬 `.backups/verification/2026-10-10-ops-tool-v1/`(Git 제외). 범위 초안 `scope-draft.md`, 맥락 메모 `context-memo.md`·`context-memo-goal.md`, Run·송신 receipt가 있다.

## 요청 원천과 승인

- 사용자 지시(메인 `msg_57853c9f68b7` 전달, 2026-10-10 16:3x KST): 「그러면 Management는 사이트쪽은 잠시 보류로 하고, 이제 실제로 사용할 수 있는 운영툴 V1.0 완성하는 쪽으로 가닥 잡자」.
- 같은 전달의 앞선 사용자 결정(16:24 KST): 소개 페이지 작업 재개 → 「이쪽은 우선순위가 아직 낮아서 당장은 보류, 중간 교수 면담 이후 재개」.
- 리드 범위 초안: `msg_7aae08911190`, E/`scope-draft.md` SHA256 `a5eb79cc2512c0a72ba64846b9143a47d619a6d865f4ba4958cfdfe282efff75`. 메인이 116줄 일치와 인용 사실 표본을 확인했다(`msg_88ea53ff21c8`).
- 사용자 승인(메인 `msg_d9a5047140d1`, 2026-10-10T07:47:34Z 전달, 메인 pane Enter 제출): 「2) 계획 검토 - Management Management 운영툴 V1.0 — 서버 운영 기본 goal 범위(질문 넷, 추천 전부 A) → A 승인 (초안 msg_7aae08911190)」. 메인 정리본은 `C:/Dev/DawnHolder_Dashboard/plans/plan-management-7aae0891.md`다. 메인이 전달한 사용자 결정이며 이 pane의 사용자 직접 입력으로 격상하지 않는다.

## 적용 중인 사용자 결정

출처는 모두 위 승인 `msg_d9a5047140d1`이다.

1. **V1.0 범위 = 서버 운영 기본(1A).** 시작·정상 종료·상태·로그 보기·비정상 종료 뒤 자동 재시작·알림. 유저·GM 권한 관리는 여러 계정 로그인과 DB 저장 뒤로 미룬다.
2. **게임 서버 코드를 고치지 않는다(2A).** 화면은 프로세스·포트·로그 수준 정보만 보인다. 접속자 목록 같은 서버 안쪽 정보는 V1.0 뒤다.
3. **관리 접근 보호 = 같은 PC 전용 주소 + 실행마다 바뀌는 비밀값(3A).** 사람 로그인은 두지 않는다. 같은 PC에서 사용자 권한으로 도는 다른 프로그램까지는 막지 못한다는 한계를 받아들였다.
4. **소개 페이지 재개 때 같은 작업 공간에서 순서대로(4A).** V1.0을 커밋 경계에서 잠시 멈추고 소개 페이지를 끝낸 뒤 돌아온다.
5. **계획:** goal 하나, PR 넷, PR2 병합 뒤 중간 점검 한 번(아래 「PR 경계와 종료」).

## 적용 중인 메인 결정

메인 판단이며 사용자 결정이 아니다.

- R-7 Fable 구현 전 설계 검토는 이 goal에 쓰지 않는다(`msg_88ea53ff21c8`).
- 실측 결과가 나빠 PR1 설계가 바뀌면 구현 전에 메인에 알린다(`msg_d9a5047140d1`).
- 게임 서버를 띄우는 실측·시험은 7777을 쓰기 전에 Core·World·Content 리드에게 알린다. 회신 주소는 Core `run:run_b36cc92a4cf4`, World `run:run_c21dddd08312`, Content `run:run_b680cd89da9a`다(`msg_d9a5047140d1`, 2026-10-10 기준 관측값).
- CURRENT의 Management 줄은 이 goal의 첫 PR이 이 goal로 바꾸고 소개 페이지는 보류로 남긴다. CodeMap 정리 PR도 그 줄을 바꾸는 중이라 늦게 병합하는 쪽이 충돌을 정리한다(`msg_d9a5047140d1`).
- PR3 전 세부 질문 셋(재시작 횟수·간격, 운영툴 완전 종료 때 서버 처리, 알림 받을 실패 종류)은 그때 메인에 올린다(`msg_d9a5047140d1`).
- 백엔드를 솔루션·독립 목록에 등록하지 않고 V1.0 동안 CI에 넣지 않는 판단을 받았다. 그 위험은 아래 「남은 위험」에 적고, PR마다 독립 검증자가 WSL에서 돌린 시험 원시 결과를 근거 폴더에 남긴다(`msg_27c6883a8b34`).
- 시험과 실측은 7777 말고도 Core의 DB 컨테이너 시험 포트 127.0.0.1:14333을 쓰지 않는다(`msg_27c6883a8b34`, 2026-10-10 기준).

## 만들 것

| PR | 만들 것 |
|---|---|
| PR1 관리 백엔드와 서버 실행 관리 | 같은 PC 전용 주소에서만 듣는 관리 백엔드(WSL, C# ASP.NET Core, [R-03](../../requirements.md#r-03) 채택 기술). 실행마다 바뀌는 비밀값 검사. 운영 실행본 빌드와 운영 버전(commit) 고정. 게임 서버 시작·정상 종료(표준 입력 Enter)·강제 종료·상태(실행 여부·시작 시각·PID·7777 대기). 개발 실행과 같은 7777 잠금 공유. 로그 수집(수집 시각 표시)·최근 10분 조회·오류/경고 낱말 찾기·서버당 7일·1GB 정리. 서버 식별(serverId)·실행 식별(runId) 기록. |
| PR2 화면 연결 | 운영툴이 관리 백엔드를 켜고 연결한다. 「서버 운영」 화면의 시작·종료 버튼, 상태 표시, 로그 보기·찾기. 연결 전·실패 때의 미연결 안내는 유지한다. 화면의 바깥 연결 차단(`connect-src 'none'`)은 그대로 두고 Electron 창 프로세스가 백엔드와 대화한다. |
| 중간 점검 | 사용자가 실제로 서버를 켜고 끄고 로그를 본다. 남은 범위를 그대로 갈지 정한다. |
| PR3 장애 대응 | 비정상 종료 감지, 제한 재시작, 한도 초과 시 중단과 실패 표시, 종료 종류 이력(정상·사람 종료·비정상), 트레이 상태, Windows 알림, 창을 닫아도 운영 지속. |
| PR4 마무리 | 실행 배치 실제 더블클릭(사람 1회), 실제 클라이언트 접속 상태에서 처음부터 끝까지 사용 점검, README·requirements·decisions 반영. |

## 건드릴 곳과 소유권

- `05_Management/backend/`(신규): 관리 백엔드와 그 시험. 위치 근거는 [CODE_CONVENTION 파일 위치와 이름](../../../00_Document/conventions/CODE_CONVENTION.md#파일-위치와-이름)의 `05_Management/` 행(운영 애플리케이션의 화면·기능과 해당 테스트)이다. 프로젝트 이름과 등록 방식은 [PR1 설계](backend-design.md#배치이름등록)에서 정했다: 솔루션·독립 목록에 등록하지 않고 V1.0 동안 CI에 넣지 않는다. 공용 파일은 건드리지 않는다.
- `05_Management/frontend/`: 「서버 운영」 화면, Electron 창 프로세스와 연결 통로, 해당 시험.
- `05_Management/` 문서(README·requirements·decisions·MCP 안내)와 이 goal.
- `00_Document/operations/CURRENT.md`의 Management 줄, `00_Document/operations/BACKLOG.md`의 담당 Management 행.
- 쓰기 소유: goal·CURRENT·BACKLOG·Git은 리드, 제품 파일은 계약별 구현자, 시험 파일은 계약별 선행 시험 작성자·검증자. 같은 파일 동시 쓰기는 하지 않는다.
- 읽기만: `02_Server/`(게임 서버), `99_Tools/sync-wsl.sh`(개발 실행 helper)와 그 7777 잠금 규칙.

## 하지 않을 것

- 게임 서버 코드·개발 실행 helper·프로토콜(PDL) 변경, 7777 포트 설정화.
- DB 연결, 예약 종료와 저장 완료 대기, 유저·GM·이상행동 화면.
- 접속자 목록·서버 안쪽 지표(BACKLOG `server-operations-view`).
- 운영자 로그인, 업데이트 예약·되돌리기.
- 에이전트에게 서버 조작 권한 주기. 공동 조회 도구는 지금처럼 읽기 전용이다.
- Docker 다중 서버, 배포 EXE·설치 프로그램·코드서명.
- OS 수준 합성 입력·전면화로 화면 시험하기.

## 관찰 가능한 완료조건

1. 「서버 운영」 화면의 「서버 시작」으로 게임 서버가 켜지고, 실행 중 여부·시작 시각·운영 버전(빌드한 commit)·7777 대기 여부가 보인다. 실제 클라이언트나 봇이 그 서버에 접속된다.
2. 「서버 종료」는 서버의 정상 종료 경로를 쓴다. 정해진 시간 안에 끝나지 않으면 강제 종료를 따로 묻는다. 사람이 끈 종료는 장애로 기록되지 않는다.
3. 서버 로그를 화면에서 최근 10분 단위로 보고 오류·경고 낱말로 찾는다. 로그 줄에는 원래 시각이 없으므로 「수집 시각」으로 표시해 발생 시각과 섞지 않는다. 일반 로그는 서버당 7일·1GB 한도로 정리되고 남은 기간이 보인다.
4. 서버가 비정상 종료되면 정한 횟수·간격으로 다시 시작한다. 한도를 넘으면 멈추고 실패를 표시한다. 이력에 종료 종류(정상·사람 종료·비정상)와 재시작 결과가 남는다.
5. 창을 X로 닫아도 서버와 자동 재시작은 계속된다. 트레이에서 다시 열면 상태와 이력이 이어진다. 조치가 필요한 실패는 Windows 알림으로 온다.
6. 관리 기능은 같은 PC 안에서만 열린다. LAN이나 외부에서 관리 주소로 접속하면 실패한다. 게임 접속의 외부 제공은 지금 그대로다.
7. 개발 checkout을 고쳐도 운영 서버 실행본은 바뀌지 않는다. 「운영 버전 올리기」를 눌러야 바뀐다.
8. 에이전트의 개발 검증 실행과 7777을 두고 겹치면 화면이 어느 쪽이 쓰는 중인지 알려 주고, 남의 실행을 끄지 않는다.
9. 실행 배치를 사람이 더블클릭해 운영툴이 열리는 것을 한 번 확인한다.

## 설계와 검증 경계

- **검증 등급: 강.** 설치·실행·I/O 도구, 보안 경계(관리 접근), 실패 수명(재시작)에 닿는다. 신규 검증자의 실사·독립 시험·실제 진입 경로 1회 실행이 필요하다. 각 계약에 변경 파일·줄 수 원시 근거를 적는다.
- **TDD:** 코드 변경 goal이라 신규 `claude-opus-5-5`가 요구사항 시험과 구현 전 실패 원시를 E에 먼저 둔다. 구현자가 같은 명령의 통과 원시를 둔 뒤 신규 검증자가 판정한다. 화면 확인처럼 시험으로 고정할 수 없는 요구는 계약에 이유와 대체 실측을 적는다.
- **구현자:** `gpt-6-astra` xhigh. [배정 신호](../../../.agents/skills/dawnholder-goal-loop/references/implementer-routing.md) 2(프로세스 수명·timeout·재시도), 3(Windows와 WSL 런타임 경계), 4(새 다파일 기능)에 해당한다. 측정·조사만 하는 계약은 신호가 없으면 `gpt-6.1-sol` max다.
- **검증자:** Astra 구현과 보안 경계라 신규 `claude-opus-5-5`다([검증자 모델 시범](../../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증자-모델-시범2026-10-31까지)).
- **화면 확인:** [README](../../README.md#프런트엔드-개발)대로 보조 디스플레이에서 하고 OS 입력 합성은 쓰지 않는다. 실행 배치 더블클릭만 사람이 한다.
- **설계 근거(범위 초안 실사):**
  - 게임 서버는 표준 입력의 Enter로 정상 종료한다(`02_Server/GameServer/Program.cs:20-23`). 관리 백엔드가 표준 입력을 쥐면 서버 코드 변경 없이 정상 종료할 수 있다. 표준 입력이 닫히면 서버가 바로 멈추므로 열어 둔다. 종료 신호 처리기는 없다.
  - 7777은 코드에 고정이고(`Program.cs:14`), 개발 실행은 공용 잠금 파일로 겹침을 막는다(`99_Tools/sync-wsl.sh:105-109`). 운영 서버도 같은 잠금을 잡으므로 운영 서버가 켜져 있는 동안 에이전트의 서버·봇 검증은 거부된다.
  - 로그는 표준 출력 자유문이고 발생 시각·수준이 없다([공동 조회 합의 「Game Dev와 맞출 계약」](../2026-09-30-system-records/shared-read-agreements.md#game-dev와-맞출-계약)). 수집 시각을 발생 시각으로 쓰지 않는다.
  - 서버·실행 식별과 로그 보존 계약은 [서버 등록·로그 합의](../2026-09-30-system-records/shared-read-agreements.md#서버-등록로그-합의)를 따른다.

### 남은 위험

- V1.0 동안 관리 백엔드 시험은 CI에서 돌지 않는다([PR1 설계](backend-design.md#배치이름등록)). 백엔드 회귀는 PR마다 독립 검증자의 WSL 실행 원시로만 확인한다. 크리티컬은 아니다.
- 운영 서버가 켜져 있는 동안 에이전트의 서버·봇 검증(`sync-wsl.sh run`·`bot`)은 7777 잠금 때문에 거부된다.
- WSL 안 프로세스가 Windows 쪽 연결이 모두 끊긴 뒤에도 사는지는 확인하지 못했다(실측 M3). PR3에서 다시 본다.

### PR1 첫 단계 실측

구현 계약 전에 아래를 측정한다. 결과가 PR1 설계를 바꾸면 구현 전에 메인에 알린다.

- 선택된 WSL .NET SDK에 ASP.NET Core 런타임이 있는지.
- 같은 PC 전용 주소(127.0.0.1)에 묶은 WSL 안 수신 대기가 Windows 창 쪽에서 닿고, 이 PC의 LAN 주소로는 막히는지. 다른 기기에서의 실제 LAN 접속은 사람이 해야 하므로 미실행으로 남긴다.
- Windows 쪽 연결이 끊긴 뒤 WSL 안에서 떼어 낸 프로세스가 계속 사는지. WSL을 끄거나 재시작하는 명령은 쓰지 않는다. 다른 파트 세션의 WSL 연결이 결과에 영향을 주므로 그 상태를 함께 기록한다.

## PR 경계와 종료

- PR마다 그 시점의 최신 main에서 새 branch를 만든다. PR1 branch는 `feat/server-operations-20261010`이다. 이 goal 고정과 CURRENT·BACKLOG 갱신은 PR1에 함께 들어간다.
- PR 생성은 리드가 하고, 병합은 사용자가 `병합 승인: PR<번호> head <40자>`를 메인 창에 제출한 뒤 메인이 한다. 리드·작업자·검증자는 병합하지 않는다.
- PR2 병합 뒤 중간 점검을 한 번 한다. 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다.
- 전체 종료는 마지막 제품 PR 병합·로컬 결과 기록 → Gardener → 결과 포함 종료 기록 PR → 종료 점검 → 리드 교체 순서다. 다음 goal은 자동으로 시작하지 않는다.
- 일정(추정, 측정값 아님): PR1·PR2는 10-17 전후, PR3·PR4는 10-25 전후. 10-26~11-01 플레이 영상 촬영 때 운영툴로 서버를 띄우는 것이 목표다.

### 작업 공간과 소개 페이지

1. 소개 페이지 재개가 정해지면 이 goal의 리드는 진행 중인 일을 커밋·push하고 이 「재개 지점」을 고친 뒤 멈춘다. 살아 있는 작업자·검증자가 있으면 정산한 뒤 멈춘다.
2. 메인이 소개 페이지 리드를 같은 작업 공간에서 다시 연다. 그 리드가 branch를 PR207로 바꿔 [마일스톤 운영 「보류된 PR」](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md#main-맞춤과-합류점) 재개 절차대로 끝낸다.
3. 끝나면 이 goal의 리드가 돌아와 자기 branch로 되돌리고 최신 main을 받는다.

## 후속 후보

V1.0 뒤로 미룬 것과 막는 것. 새 후보는 BACKLOG에 두고 여기에는 링크만 둔다.

- 예약 종료와 저장 완료 대기([R-06](../../requirements.md#r-06)): DB 저장 작업 단계 뒤.
- 유저·GM 권한 관리([R-08](../../requirements.md#r-08)): 여러 계정 로그인과 DB 뒤.
- 이상행동 근거([R-09](../../requirements.md#r-09)): 게임 서버의 사건 기록이 먼저.
- 접속자 목록·서버 안쪽 지표: BACKLOG `server-operations-view`, [공통 계약 로드맵](../../../01_Phases/milestones/2026-09-30-contracts-persistence/roadmap.md) P6.
- 서버 운영 백엔드 후속 전반: BACKLOG `management-operations-followup`(이 goal이 R-04·R-05·R-07 일부와 서버 등록·로그를 다룬다).
- 운영자 로그인, 업데이트 예약·되돌리기, 공동 조회 도구에 서버 상태·로그 읽기 추가와 실제 세션 연결, 운영툴 시험 CI 편입, Docker 다중 서버.

## 결과와 열린 사항

### 진입과 goal 고정

- 2026-10-10 16:36 KST 새 Run `run_003b556f0ba8`을 만들고 메인에 READY를 보냈다(`msg_0766a3e1cf8e`). receipt는 E/`run-create-receipt.json`·`ready-send-receipt.json`이다.
- 범위 초안을 보냈고(`msg_7aae08911190`) 사용자 승인을 받았다(`msg_d9a5047140d1`).
- 최신 main `cc20d428`에서 branch를 만들고 이 goal, CURRENT Management 줄, BACKLOG `management-launcher-real-run` 행의 상태를 고정했다(commit `1f56658b`). 맥락 메모는 E/`context-memo-goal.md`다.

### PR1 실행 환경 실측 결과

- 작업자 `[Management Sol]`(요청 `gpt-6.1-sol` max, 세션 기록 turn_context `gpt-6.1-sol`·`max`, backend unknown), Task `task_109b3a99b814`, Dispatch `ctx_eed2070ef558`. 계약 E/`contracts/measure-task.md`(SHA256 `3e59f9db…a6a71`).
- 연결: `worker-start`가 `turn_start_unobserved`였다. draft `[Pasted Content 13025 chars]`가 계약 8,237자 + 머리말 추정 4,787자와 1자 차이라 [공식 draft 복구](../../../00_Document/operations/ORCA.md#official-contract-draft)대로 Enter 한 번을 보냈다. 사후에 작업자 세션 기록에서 계약 전문(13,024자) 포함을 확인했다(E/`contracts/measure-contract-delivery-check.txt`).
- 완료: `worker_done` `msg_0aa1cd9bd018`(succeeded). 보고 E/`measure/report.md`. 정산 `worker-release`는 `retained`(외부 터미널)였고 idle 확인 뒤 pane을 닫았다.
- 결과:
  - M1: 선택 SDK `10.0.301`에 `Microsoft.AspNetCore.App 10.0.9`가 있다. PATH의 `dotnet`은 없다.
  - M2: 127.0.0.1에 묶은 WSL 수신 대기는 Windows 127.0.0.1에서 성공, 이더넷 `192.168.45.227`·Default Switch `172.27.48.1`에서 2초 시간 초과였다. 대조군 0.0.0.0은 이더넷에서 성공했다. 하마치 주소는 이 시점 열거에 없었다. 다른 기기의 실제 LAN·하마치 접속은 미실행이다.
  - M3: 떼어 낸 표식 프로세스는 10·60·180초에 살아 있었다. 180초에는 다른 `wsl.exe` 연결 4개와 docker-desktop이 있어 연결 0 수명은 미재현이다. 측정 시작 전 Ubuntu는 `Stopped`였다.
- 리드 표본 대조(일치): `raw/m1-runtime.stdout.txt` 12행, `raw/m2-loopback-bound-ss.stdout.txt`(127.0.0.1:49191, pid 433), `raw/m2-loopback-attempt-1.json`·`-3.json`, `raw/m2-wildcard-attempt-3.json`, `raw/m3-t180-summary.json`.
- 설계 반영: 관리 주소는 127.0.0.1만(M2). 백엔드는 운영툴이 앞에서 잡는 자식으로 시작하고 수명 문제는 PR3로 넘긴다(M3). 저장소 규칙 조사로 솔루션·독립 목록 미등록과 V1.0 CI 미편입을 정했다([PR1 설계](backend-design.md#배치이름등록)). 승인 범위 변경은 없다.

### PR1 선행 시험 결과

- 작성자 `[Management 검증자]`(신규 `claude-opus-5-5`, 화면 표시 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_5b0fd90abdf7`, Dispatch `ctx_a7366e3f3ec7`. 계약 E/`contracts/pr1-tests-task.md` v1(SHA256 `b65ec94c…7d`).
- 설계 질의 12건(`msg_8610f23efa78`)에 답하고 설계를 고쳤다(commit `b41d3617`, 답 `msg_6e7113793a0b`). 리드 판단은 실행 중 정리 주기 설정 키 `logs.retentionIntervalSeconds` 추가 하나다. 기본값 600초라 운영 동작은 그대로이고, E7을 결정적으로 재현하려고 넣었다.
- 허용 밖 쓰기 후보 질의(`msg_b2ea9a363362`): 고정 SDK 설치 폴더의 빈 `metadata` 폴더 mtime이 `dotnet build` 때 바뀐다. 허용 실행의 SDK 부작용으로 판단해 기록만 했다(답 `msg_c6b8238ba3e6`). 다음 구현·검증 계약에 미리 적는다.
- 완료: `worker_done` `msg_e72172c63a90`(succeeded), Dispatch `completed`·`settled`. idle 확인 뒤 pane을 닫았다. 보고 E/`pr1-tests/report.md`.
- 결과: `05_Management/backend/ManagementBackend.Tests/`·`GameServerStub/` 새 파일 30개. 요구 A1~H1 전부에 시험이 있고 미작성 요구는 없다. 같은 명령의 구현 전 실행(E/`pr1-tests/raw/pre-implementation/`)은 전체 72, 통과 8(가짜 서버·임시 원천 저장소 자체 점검), 실패 64, 건너뜀 0, 빌드 경고 0이다. 실패 64건은 모두 「Management backend is not built」다.
- 리드 표본 대조(일치): `results.trx`의 `<Counters>`와 결과 72줄(Failed 64·Passed 8)을 직접 셌고, 실패 메시지 64건 모두 같은 이유였다. 작업자 세션 기록에서 맥락 메모 첫 쓰기(08:43:26Z)가 첫 시험 파일(08:46:11Z)보다 앞섰다. E7 시험이 설계 「로그 저장과 보존」의 정리 순서와 맞는지 읽었다. 7777·14333은 제외 목록에만 있다.
- 남은 위험(보고 「남은 위험과 설계 관찰」): 백엔드를 상대하는 helper는 실제 백엔드와 아직 돌지 않았다. C6·D1·D2·E7 등은 2~4초 시간 가정에 기대며 직렬 실행으로 줄였다. 구현 뒤 전체 실행 시간은 미측정이다(작성자 추정 3~5분). 기본값 경로(관리 포트 47321, 기본 데이터 폴더, 7777 잠금)는 시험하지 않는다. 시험은 백엔드에 build server를 끄는 환경 변수를 넘기므로, 운영 중 실행본 빌드가 build server를 남기지 않는지는 설계 문장으로 고정하고 검증자가 코드로 본다.

### PR1 구현

| 회차 | 배정 모델(신호) | 세션 | 계약 | 독립 결함 수 | 절차 실패 |
|---|---|---|---|---|---|
| 1 | `gpt-6-astra` xhigh(1·2·3·4) | Task `task_b04cc43a7e24`, Dispatch `ctx_15e6495ae887` | E/`contracts/pr1-impl-task.md` v1(SHA256 `44fca8b2…4164`) | 0(독립 검증 PASS) | 없음(기동 `turn_started` 관측, worker_done 1회) |

- 배정 근거: 관리 접근 경계의 입력 검증(1), 게임 서버 프로세스 수명·시간 상한·잠금(2), WSL Linux 호출·Git·.NET 호스트 경계(3), 새 다파일 기능(4). 영역은 `05_Management/backend/ManagementBackend/**`와 `backend-wsl.sh`다. 위 「설계와 검증 경계」에 적은 2·3·4에 1을 더했다.
- 기동: Codex v0.162.1, 첫 화면 「GPT-6-Astra xhigh」, rollout turn_context `gpt-6-astra`·`xhigh`, backend unknown. 첫 화면에 선택창은 없었다.
- 완료: `worker_done` `msg_f291251eb2ea`(succeeded), Dispatch `completed`·`settled`. idle 확인 뒤 pane을 닫았다. 보고 E/`pr1-impl/report.md`.
- 결과: 제품 새 파일 16개 1,502줄(`ManagementBackend/` 15개, `backend-wsl.sh`). 같은 시험 명령의 깨끗한 빌드 실행은 72/72 통과, 건너뜀 0, 122초(E/`pr1-impl/raw/final-clean/`). 첫 실행은 71/72였다. A5(16 KiB 초과 본문) 실패를 구현자가 제품 결함으로 분류했고, Kestrel의 중복 본문 상한을 없애 접근 경계 하나가 검사하게 고쳤다. `backend-wsl.sh build` 제품 경고 0, `backend-wsl.sh test` 72/72.
- 구현자 자체 점검: `backend-wsl.sh run`으로 메인 checkout의 commit `a70cc205` 운영 실행본을 빌드했다(GameServer 시작 API는 호출하지 않음). 이 worktree는 `.git` 파일이 Windows 경로를 가리켜 WSL Git이 읽지 못하므로, `release.sourceRepository`는 설계대로 메인 checkout이어야 한다. 실행본 빌드에서 기존 코드의 SA1201·SA1202 경고 2건(`EnemyCatalog.cs`, `PartyRegistry.cs`)이 보였고 범위 밖이라 고치지 않았다.
- 리드 표본 대조(일치): `final-clean`·`entry-test`·`trial-1`의 `results.trx` 셈과 exit를 직접 셌다. 시험 파일 무변경은 `git diff --stat a70cc205`의 빈 출력으로 확인했다. rollout에서 맥락 메모 첫 쓰기(09:24:23Z)가 첫 제품 파일(09:25:31Z)보다 앞섰다. 제품 파일은 CR 0, BOM 없음이다.
- 독립 검증: `[Management 검증자]` 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_19b63a45a969`, Dispatch `ctx_4e5d1c448ae9`, 계약 E/`contracts/pr1-verify-task.md` v1(SHA256 `77adb8b2…88e3`). 실제 진입 1회 직전에 검증자가 묻고, 리드가 Core·World·Content 리드에게 7777 사용을 알린 뒤 진행 답을 준다.
- 검증자에게 넘길 관찰: 시험 원시의 `environment.txt`에 `head=unknown`이 찍힌다(WSL Git이 worktree를 못 읽음). 구현자 harness 일부가 Python·PowerShell(근거 폴더 안, 제품 아님)이다. `run`은 Debug 빌드로 실행하고 WSL 복사본 잠금(fd 9)을 쥔 채 백엔드로 exec한다.

### PR1 독립 검증 결과

- 판정 **PASS**: `worker_done` `msg_0f6489e5c321`, Dispatch `completed`·`settled`, idle 확인 뒤 pane을 닫았다. 판정 원문 E/`pr1-verify/verdict.md`, `verifies` `task_b04cc43a7e24`. 차단 결함 0, 설계 관찰 8.
- 강 등급 세 항목:
  - 실사: 구현 보고의 파일·줄 수·셈·메모 순서·서식이 실제 diff·원시와 일치했다.
  - 독립 시험: 새 시험 7파일 12건(경로 모양 입력, 64자 경계, 16 KiB 정확한 경계와 큰 본문 조기 거부, 실행본 명령 동시성, 빌드 시간 초과 정리, build server 잔류 없음, 강제 종료된 백엔드 뒤 재시작, 16 MiB 회전)을 더했다. 같은 시험 명령으로 84/84(147초)다.
  - 실제 진입 1회(10:07:48Z~10:08:02Z): `backend-wsl.sh run`으로 메인 checkout의 HEAD `2ae6a605` 실행본을 빌드했다. 진짜 GameServer를 7777로 시작해 `running`·`portOwner: self`·같은 pid 대기를 확인했다. 그동안 공유 잠금은 개발 helper가 거부될 상태였다. 정상 종료는 `graceful`·종료 코드 0으로 끝났고, 7777·잠금·47321이 다시 비었다. 두 checkout의 `git status`와 Unity DLL hash는 전후 같았다.
- 7777 알림: 시작 전 Core·World·Content 리드에게 알렸고(`msg_b01e67632858`·`msg_3f19a5178c87`·`msg_0330dceca6f9`), 리드 사전 관측(10:04:37Z)도 비어 있었다. 끝난 뒤 리드가 비었음을 다시 확인하고(10:13:48Z) 종료를 알렸다(`msg_8a944feae847`·`msg_88a21094ea9d`·`msg_68f69de886cd`).
- 기존 시험 B1 1회 실패: 판정용 실행 한 번(84 중 83)에서 `portListening`이 `true`였다. 그때 다른 세션의 `sync-wsl.sh test`가 같은 WSL에서 빈 포트를 쓰고 있었다. 시험의 빈 포트 고르기가 열었다 닫는 방식이라 겹칠 수 있다. 검증자는 원인 미확정 (d)로 남기고 시험을 고치지 않았다. 재실행·기준 실행 등 다른 네 번은 통과했다.
- 허용 실행의 부작용(기록만): WSL `/tmp/.dotnet` 생성(`msg_4d37625ec1ff`, 원인 미확정), 고정 SDK `metadata` mtime. 검증 중 WSL이 두 번 다시 켜져 `/tmp`가 비었다. 우리 쪽 shutdown 명령은 없었다.
- 리드 표본 대조(일치): `before-independent`·`after-independent`·`after-independent-rerun`의 `results.trx` 셈(72/72, 84/83/1, 84/84)과 실패 이름을 직접 셌다. B1 실패 원문(`after-independent/console.log` 78-96행)과 실제 진입 `summary.txt`를 읽었다. 검증자 세션 기록에서 메모 첫 쓰기(09:51:38Z)가 첫 저장소 쓰기(09:56:43Z)보다 앞섰고, 제품·기존 시험·`Support/` 쓰기는 0이다.

### PR2 전에 판단할 것

검증 판정의 설계 관찰과 B1 실패에서 나온 판단 거리다. PR1 범위의 설계 문장은 어기지 않는다. PR2 설계 때 리드가 판단하고, 범위 판단이 필요한 것만 메인에 올린다.

- 관찰 1: 1 GiB 로그 조회가 2.4초 동안 수집 잠금을 쥐어 상태 조회도 2초 기다린다. 화면이 상태를 주기적으로 묻기 전에 정한다.
- 관찰 4: 명시한 `--config` 파일이 없거나 모르는 키가 있어도 조용히 기본값을 쓴다. 경로 오타 하나로 실제 데이터 폴더·7777 잠금을 쓰게 되므로 실패로 바꾸는 쪽을 검토한다.
- 관찰 7: 서버가 꺼져 있을 때 상태 조회마다 공유 7777 잠금을 잠깐 잡는다. 화면이 자주 물으면 개발 helper가 잘못 거부될 수 있다.
- 관찰 3·5·6·8: `run`의 Debug 빌드, 종료 중 `portOwner: unknown` 표시, 응답이 익명 객체·상태 문자열, 남이 SIGKILL한 종료의 `signal: null`. 화면 연결 때 함께 본다.
- 관찰 2(PR3): 게임 서버가 WSL 복사본 잠금 fd를 물려받는다.
- B1 시험의 빈 포트 경합: PR2 안에서 처리한다(리드 결정 `msg_0967be03bc1d`, 메인이 처리 위치를 리드에게 맡김 `msg_4a1bb8f52967`). PR2 선행 시험 계약에 실패 메시지에 상태 JSON·포트를 넣고 빈 포트를 잡아 두는 helper로 바꾸는 일을 시험 소유자 작업으로 넣는다. PR2도 같은 시험 명령으로 검증하므로 같은 경합이 판정을 흐릴 수 있기 때문이다.

### PR222 병합

- PR222 「feat: add the management backend for server operations (ops tool V1.0 PR1)」, head `dbfe8f491b0cf97f8808957e8f52faf9d26a6624`. CI 4개 통과(check 2개, architecture-tests 7분 38초, dotnet-tests 23분 13초).
- 승인 요청 `msg_122f13d62688`. 메인 R-2 원천 대조 뒤 B1 (d)는 PASS 안에 둔다는 메인 판단을 받았다(`msg_4a1bb8f52967`, 사용자 결정 아님).
- 사용자가 메인 pane에 「병합 승인: PR222 head dbfe8f491b0cf97f8808957e8f52faf9d26a6624」를 제출했고, 메인이 head를 다시 확인한 뒤 병합했다. MERGED 2026-10-10T10:57:56Z, merge commit `20630a90b404fd4b736606c5dba81a20975204d2`(메인 `msg_bc88d40255f8`). 원격 branch는 자동 삭제됐다.
- CURRENT 충돌: PR220(CodeMap 정리)이 먼저 병합돼 이 PR이 정리했다. PR220이 지운 줄은 두고 Management 줄만 이 goal로 바꿨다(merge commit `dbfe8f49`).
