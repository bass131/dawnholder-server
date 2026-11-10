# 전체 코드 감사 운영툴 묶음 수정과 실행 검증

- 파트: Management · 리드 `[Management 리드 Opus]`(claude-opus-5-5 xhigh)
- 상태: **승인됨**(사용자 계획 검토 A, 11-11 02:0x KST). 첫 PR(3번)은 독립 검증 차단(결함 #1)으로 수정 단계, 둘째 PR(1·2번)은 독립 검증 통과로 첫 PR 병합 뒤 PR을 연다.
- 승인한 판: 이 파일 SHA256 `240e297b80be8fdbccf3694530077e28cb9e30caa8e8e9dfbf6687bd48f93f1b`(메인이 승인 뒤 대조). 그 뒤의 고침은 착수 회신 `msg_2296395f7b76`이 적으라고 한 기록과 상태 갱신이며 범위 네 항목은 바꾸지 않았다.
- 기준: main `c9dc44e44c4f37e598e2dd70ea0aa18d579529be`(PR310, 문서 4개 파일. 초안 기준 `7a724f18`에서 앞으로 맞춤, `05_Management` 변경 없음) · branch `fix/management-audit-fixes-20261110`
- 작업 폴더: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-audit-fixes`. 정본 표의 `management-active`는 쓰지 않는다. 이유는 「적용 중인 메인 결정」에 있다.
- 감사 기준 판: `acd5a7db065b37baa7f491c4502002eb8ce184a3`. 두 판 사이 `05_Management` 변경은 없다(`git diff --stat` 출력 없음).
- 근거 폴더: `.backups/verification/2026-11-10-code-audit-management-fixes/`
- 초안 작성 시각: 2026-11-10T11:26:31Z(`date -u` 관측, `TZ=KST-9 date` 20:26:31 KST)
- 이 위치로 옮긴 시각: 2026-11-10T12:47:11Z(`date -u` 관측, `TZ=KST-9 date` 21:47:11 KST). 새 리드가 앞 초안(SHA256 `bcd28f9837a11a3697154412f8d7c22226e05911f39021278aea9a927fdc62a1`)을 옮겨 달라진 곳만 고쳤다.

## 재개 지점

**다음 첫 일:** 첫 branch `fix/management-audit-fixes-20261110`으로 돌아가 결함 #1을 고친다. 순서는 결정적 시험을 먼저 쓰는 시험 작성자, `backlog-store.ts` 구현자, 소요 ms 전후를 비교하는 새 검증자다(아래 「3번 검증 결과와 결함 #1」). 이 goal의 살아 있는 판은 이 커밋부터 첫 branch로 옮긴다. 둘째 branch의 goal 기록 커밋은 첫 branch에 cherry-pick해 두 판을 맞춘다.

둘째 PR(1·2번) 독립 검증은 통과했다(아래 「둘째 PR 검증 결과」). 둘째 PR은 첫 PR 병합 뒤 둘째 branch를 main에 rebase하고 연다. 병합 순서는 그대로 첫 PR이 먼저다. 둘째 branch에는 첫 PR의 제품 코드를 싣지 않고 goal 기록 커밋만 옮겼다.

막힌 지점: 없음. 근거 메모는 근거 폴더의 `lead-context-audit-fixes-folder.md`(앞 리드의 `lead-context.md`는 당시 기록이다), 범위 문서 사본은 같은 폴더의 `fix-scope-management.md`다.

## 진척 단계

PR 번호는 PR을 여는 때 채운다.

- [x] 초안 승인
- [x] 선행 시험 작성
- [x] BACKLOG 읽기 구현
- [>] BACKLOG 읽기 검증(차단 결함 #1, 수정 단계)
- [ ] BACKLOG PR 병합
- [x] 백엔드 수명 구현
- [x] 백엔드 수명 검증(통과, 결함 0)
- [ ] 운영툴 실제 실행
- [ ] 백엔드 수명 PR 병합
- [ ] 종료 Gardener
- [ ] 종료 기록 병합

## 적용 중인 사용자 결정

메인 메시지 `msg_a76e1ea7d776`와 새 리드 진입 지시 `msg_63fdd6f7b560`(둘 다 메인 전달이며 사용자 직접 입력으로 격상하지 않는다)이 전달한 원문이다. 범위 문서 전문은 `msg_77074cd1c0c9`이고 사본의 SHA256은 `c4239985fc5b64cb3e787142cebc141dd47f19f75c23ce7213da694611bb7f3e`로 메인 원본 값과 같다.

- 「대시보드 결정 응답: 1) 174 - 전체 코드 감사 1차 수정 범위(심각도 높은 결함) → A 여섯 묶음 34건 · 2) 175 - 감사 수정 착수 시점(지금 또는 20:00 리셋 뒤) → A 지금 서버 묶음부터」
- 「대시보드 결정 응답: 2) 183 - Codex가 GPT Astra 모델을 거부함: 계정을 살릴지, Codex 없이 갈지 → C 코멘트 「구독이 끝났나보네, claude Opus로 해야할 거 같은데」」
- 사용자가 고른 결정문의 진행 방식(메인이 쓴 글): 「모든 항목은 실패하는 시험을 먼저 만든 뒤 고친다. 실패를 재현하지 못한 항목은 범위에서 빼고 목표 문서에 기록한다.」, 「PR마다 병합 승인 한 줄을 따로 받는다. 그래서 11-11 전에는 main에 넣지 않게 조절할 수 있다.」

- 계획 승인(11-11 02:0x KST 메인 pane 제출, 메인이 `msg_2296395f7b76`으로 전달. 한 문장 전체): 「대시보드 결정 응답: 2) 187 - 발표 시연에 Content·Core 작업 폴더를 쓰는지(게임 클라이언트와 서버를 띄우는 곳) → A 안 쓴다 · 3) 계획 검토 - Content Content 코드 감사 클라이언트 묶음 11건 목표 초안 → A 승인 (초안 msg_64140427bd4e) · 4) 계획 검토 - Rules Rules 코드 감사 병합 관문 도구 묶음 3건 목표 초안 → A 승인 (초안 msg_7f890ca476f4) · 5) 계획 검토 - Management Management 코드 감사 운영 도구 묶음 3건 목표 초안 → A 승인 (초안 msg_79a19b3f8478)」. 이 goal에 해당하는 부분은 5번이다.

### 사용자에게 보인 「승인 범위 문서와 달라진 점」

`msg_2296395f7b76`이 사용자 승인 때 보였다고 전달한 셋이다.

1. 새 폴더(`management-audit-fixes`)에서 작업한다.
2. 결정 174의 실행 검증 가운데 게임 서버 시험·Unity 시험·자동 접속 봇·DB 실행을 돌리지 않고, 운영툴 실제 실행은 발표 뒤로 미룬다.
3. 기존 시험 넷의 기대값을 고치고 이전 기대·새 기대·근거를 표로 남긴다.

### 사용자에게 알린 위험

첫 PR을 main에 병합해도 발표용 앱에는 들어가지 않는다. 발표용 폴더를 새 판으로 옮길지는 사용자가 따로 정한다. 리드와 작업자는 그 폴더를 건드리지 않는다.

## 적용 중인 메인 결정

`msg_a76e1ea7d776`와 `msg_63fdd6f7b560`의 내용이다. 사용자 문장이 아니다. 아래 배정 표는 `msg_63fdd6f7b560`에 따라 이 checkout의 CLAUDE.md 배정 표와 AGENTS 라우팅보다 먼저다.

| 역할 | 기동 명령 | 태그 |
|---|---|---|
| 구현자 | `claude --model claude-opus-5-5 --effort high` | `[Management 구현 Opus]` |
| 같은 계약 구현의 두 번째 독립 불합격 뒤 다음 시도 | `claude --model claude-opus-5-5 --effort xhigh` | `[Management 구현 Opus]` |
| 선행 시험 작성자 | `claude --model claude-opus-5-5 --effort high` | `[Management 시험 Opus]` |
| 독립 검증자 | `claude --model claude-opus-5-5 --effort xhigh --autocompact 250000` | `[Management 검증자]` |
| 목표 종료 Gardener(읽기 전용) | `claude --model claude-opus-5-5 --effort high` | `[Management Gardener Opus]` |

- Codex 세션은 열지 않는다. 작업 하나 뒤 정산·종료하고 재사용하지 않는다. 구현자와 검증자는 다른 새 세션이고 검증자는 동시에 하나만 연다.
- 작업자는 한 번에 하나, 끝난 뒤 검증자 하나를 기본으로 한다. Claude 5시간 한도가 먼저 찬다.
- 발표는 11-11 16:00 KST다. 메인 checkout, 사용자 서버 창, 사용자가 띄운 운영툴 앱과 그 실제 데이터 폴더·연결 파일·포트를 건드리지 않는다. 시험과 재현은 임시 데이터 폴더와 임시 포트로만 한다. 사용자의 운영툴 백엔드 프로세스에 신호를 보내지 않는다.
- 작업 폴더: 발표에 쓰는 운영툴 앱이 `management-active`의 작업 트리에서 빌드돼 뜬다고 메인이 폴더 상태로 판단했다(사용자 확인은 아직 없다). 그래서 이 goal은 별도 폴더에서 일한다. `management-active`에는 파일 수정, HEAD·index·작업 트리를 바꾸는 git 명령, npm 실행, 빌드를 하지 않는다. 읽기는 된다.
- WSL·Electron·포트 7777을 쓰는 실행은 아래 「실행 조건」을 따른다.
- 새 의존성이 필요해 보이면 구현 전에 메인에 올린다.

## 범위

### 만들 것

| 번호 | 결함 | 고친 뒤의 동작 |
|---|---|---|
| 1 | 같은 데이터 폴더를 두 백엔드가 쓰면 진행 중 기록과 활성 로그가 깨진다 | 백엔드는 시작하자마자 데이터 폴더의 소유를 잠금으로 얻고 수명 내내 쥔다. 이미 다른 백엔드가 쥐었으면 저장소를 만들기 전에 이유를 출력하고 0이 아닌 코드로 끝난다. 앞 백엔드의 기록·로그·연결 파일은 그대로다. |
| 2 | 백엔드 프로세스 번호 재사용 뒤 다른 프로세스에 종료 신호를 보낼 수 있다 | 인증에 성공한 시점의 프로세스 시작 시각을 함께 기억한다. TERM과 KILL 직전에 같은 프로세스인지 다시 본다. 다르거나 사라졌으면 신호를 보내지 않고, 확인할 수 없으면 신호를 보내지 않고 그 사실을 기록한다. |
| 3 | BACKLOG 파일이 256 KiB 구간 상한을 넘어 「이후 작업」 화면과 기록 검사를 막는다 | BACKLOG 전체 읽기는 파일 크기 상한 1 MiB만 받는다. 원문 구간 읽기와 공동 조회의 256 KiB 구간 상한은 그대로 둔다. |

### 건드릴 곳

- 1번: `05_Management/backend/ManagementBackend/Program.cs`, 잠금을 맡는 작은 단위(`PortLease.cs`와 같은 폴더, 이름은 구현 계약에서 정한다), `ManagementBackend.Tests/BackendRestartTests.cs`와 새 시험, `goals/2026-10-10-server-operations/backend-design.md`의 「상태 소유와 수명」 표.
- 2번: `05_Management/frontend/electron/backend-connection.ts`, `frontend/tests/backend-connection*.test.ts`, `goals/2026-10-10-server-operations/screen-design.md`의 「앱 종료」 절.
- 3번: `05_Management/frontend/electron/source-section-store.ts`, `backlog-store.ts`, `frontend/tests/backlog-store*.test.ts`, `goals/2026-10-06-record-source-unification/backlog-menu-design.md:71`.
- 이 goal, `00_Document/operations/CURRENT.md`의 Management 줄(승인 뒤).

고치는 동작을 이미 못박은 기존 시험은 선행 시험 작성자가 함께 고친다. 대상은 `BackendRestartTests.cs:42`(두 백엔드 공존 전제), `backend-connection.test.ts:616`과 그 대역(`/proc/<pid>/stat` 응답 추가), `backend-connection-new-file.test.ts`, `backlog-store-boundary.test.ts:158`이다. 고친 시험마다 이전 단정·새 단정·요구 출처(이 goal의 해당 번호)와 같은 명령의 전후 통과·실패 수를 표로 남긴다.

### 하지 않을 것

- `00_Document/operations/BACKLOG.md` 자체를 줄이는 일. Rules 파트의 파일이다.
- 공유 구간 상한 `MAX_SOURCE_SECTION_BYTES`를 올리는 일. 공동 조회와 원문 화면의 계약이다.
- 인증된 종료 API를 새로 만드는 일. 아래 설계 선택에 이유가 있다.
- 패키지 추가, 설정 형식 변경, 감사의 다른 다섯 묶음.
- 메인의 실행 창 회신 없이 WSL을 쓰는 실행, Electron을 띄우는 실행, 7777을 쓰는 실행.
- 사용자 데이터 폴더를 읽거나 쓰는 실행. 시험 전후 불변 근거를 위해 크기·수정 시각·hash를 재는 일은 예외다.
- `management-active`에 대한 쓰기, 소유 표식(`.source-owner`)·잠금 파일·실제 데이터 폴더의 변경.
- 게임 서버 시험·Unity 시험·자동 접속 봇·DB 저장 실행. 이번 변경은 `05_Management`의 프로세스 수명과 파일 읽기만 바꾸고 게임 서버 코드·공유 DLL·DB 스키마에 닿지 않아서 돌리지 않는다. 미실행을 통과로 적지 않는다.

### 관찰 가능한 완료조건

1. 범위에 남은 항목마다 수정 전 코드에서 실패한 시험과 수정 뒤 같은 명령의 통과 원시가 근거 폴더에 있다. 재현하지 못해 뺀 항목은 시도 내용과 함께 이 문서에 있다.
2. 1번: 첫 백엔드가 게임 서버 대역을 돌리는 동안 같은 폴더로 둘째를 띄우면 둘째는 0이 아닌 코드로 끝나고, 첫째의 열린 실행 기록·활성 로그·연결 파일이 그대로이며 첫째가 계속 응답한다. 첫째가 SIGKILL로 죽으면 다음 백엔드는 곧바로 소유를 얻는다. 남아 있는 게임 서버가 잠금을 물고 있지 않다.
3. 2번: 대역 시험에서 붙은 뒤 번호가 다른 프로세스로 넘어가면 TERM도 KILL도 나가지 않는다. TERM 뒤 대기 중 번호가 넘어가도 KILL은 나가지 않는다. 같은 프로세스면 종전과 같이 `kill -TERM <pid>`와 30초 뒤 `-KILL`이 나간다. 앱이 띄운 자식 경로도 같은 확인을 지난다.
4. 3번: 현재 BACKLOG 크기를 넘는 고정 크기 입력에서 읽기가 성공하고, 1 MiB 초과는 `too-large`다. `npm run records:check`가 이 checkout의 실제 BACKLOG로 끝까지 돌아 backlog 그룹이 `ran`이다. 범위 밖 기존 오류(「범위 확인 실행 결과」) 때문에 이 명령의 종료 코드는 고친 뒤에도 1로 남을 수 있어, 판정은 종료 코드가 아니라 backlog 그룹 상태로 한다. 구간 상한 시험과 경로·UTF-8·읽는 중 변경 거부는 그대로 통과한다.
5. 백엔드 시험, `typecheck`·`desktop:typecheck`·`mcp:typecheck`, `npm test`를 아래 실행 표대로 돌렸고, 수정 전 기준선과 같은 명령으로 비교한 원시가 있다. 기존 실패가 있으면 전부 분류해 적는다.
6. 운영툴 실제 실행(Electron)은 발표 전에는 하지 않는다. 발표 뒤(11-11 16:00 KST 이후) 메인과 방법을 정해, 백엔드 수명 PR의 병합 승인 요청 전에 임시 데이터 폴더·임시 설정으로 1회 끝낸다. 그때까지 못 하면 이유와 함께 미실행으로 적고 통과로 적지 않는다.
7. PR마다 새 독립 검증자의 PASS 판정 원문이 있고 사용자의 병합 승인 한 줄을 PR마다 따로 받는다.

## 설계 선택

| 번호 | 택한 것 | 대안과 버린 이유 | 받아들이는 한계 |
|---|---|---|---|
| 1 | 단일 소유. `<dataDirectory>/backend.lock`에 `flock`(O_CLOEXEC)을 걸고 수명 내내 쥔다. `PortLease`와 같은 방식이다. 커널이 프로세스 사망 때 풀어 준다. | 복수 실행 허용에 소유자 식별: `server.json`·실행본·연결 파일까지 프로세스 간 식별이 필요해 변경이 크고, 함께 돌 필요가 관측되지 않았다. pid 파일: 2번과 같은 재사용 문제가 다시 생긴다. | 둘째는 즉시 거절이다. 짧게 기다리지 않는다. 앱 경로는 `backend-wsl.sh`가 이미 `flock -n`으로 즉시 거절하므로 화면 동작은 지금과 같다(`disconnected: backendExited`, 「다시 연결」). |
| 2 | OS 프로세스 시작 시각. `/proc/<pid>/stat`의 22번째 값을 인증 직후 저장하고 TERM·KILL 직전에 비교한다. 괄호가 든 프로세스 이름에 대비해 마지막 `)` 뒤부터 센다. | 인증된 종료 API: 백엔드 변경과 구버전 호환이 필요하고, 종료 중 HTTP가 닫힌 뒤의 KILL 단계를 덮지 못한다. 설계 변경이라 메인 판단이 필요하다. 한 셸에서 확인과 신호를 묶기: `wsl.exe` 인자 인용 위험에 비해 줄어드는 시간창이 작다. | 확인과 신호는 원자적이지 않다. 사이 시간은 `wsl.exe` 호출 한 번 정도이고 이 PC의 `pid_max`는 4,194,304다(WSL 관측). 그 안에 번호가 한 바퀴 도는 경우는 막지 못한다. |
| 3 | 파일 읽기 경계(경로·동일성·크기·UTF-8·변경 판정)를 `source-section-store.ts` 안에서 나누고, BACKLOG는 전체 텍스트를 받는다. 상한은 기존 1 MiB 파일 상한 하나다. 현재 파일은 336,542바이트다. | 구간 상한 일괄 상향: 감사가 금지했고 공동 조회 계약이 바뀐다. 512 KiB 같은 BACKLOG 전용 수치: 파일이 다시 자라면 같은 고장이 난다. | BACKLOG가 1 MiB를 넘으면 다시 `too-large`다. 파일 줄이기는 Rules 몫이다. |

## PR 경계

| PR | 내용 | 등급 | 이유 |
|---|---|---|---|
| 첫 PR | 3번 BACKLOG 읽기 | 강 | 프런트엔드 파일 읽기만 바뀐다. `records:check`의 backlog 그룹 실패를 실행으로 확인했고 「이후 작업」 탭은 같은 읽기 경로라 막힐 것으로 본다(앱 화면은 미실행). 다른 변경과 독립으로 작게 유지해 병합을 따로 고를 수 있게 한다. 병합돼도 시연 앱에는 들어가지 않는다. 시연 앱은 `management-active`의 `acd5a7db`에서 빌드되며, 시연에 넣을지는 사용자가 따로 정한다. |
| 둘째 PR | 1번·2번 백엔드 수명 | 강 | 둘 다 백엔드 프로세스의 시작·종료 소유 문제라 한 검증자가 함께 본다. 시작·종료 동작을 바꾸므로 발표 전 병합은 권하지 않는다. |

첫 PR은 branch `fix/management-audit-fixes-20261110`, 둘째 PR은 branch `fix/management-audit-backend-lifetime-20261111`이다. 둘째 branch는 goal 커밋 `d894105c` 위에 1·2번 시험 커밋 `c7ebc0e6`을 두었고, 첫 PR 병합 뒤 main 위로 다시 올린다. 첫 PR의 작업 트리에 1·2번 시험이 섞이지 않게 하려는 것이다.

한 PR마다 구현자 하나, 검증자 하나다. 선행 시험은 한 세션이 세 건을 PR 순서로 쓴다. 전체 세션은 선행 시험 1, 구현 2, 검증 2, Gardener 1이다. 등급 근거의 줄 수 원시는 구현 뒤 계약에 적는다.

## 항목별 선행 시험 계획

0단계로 수정 전 기준선(`npm test`, 세 typecheck, `records:check`, 백엔드 시험)의 원시를 저장한다. 백엔드 시험은 메인의 실행 창 회신 뒤에 돈다. 리드가 착수 전에 돌린 `records:check`는 범위 확인이라 이 기준선을 대체하지 않는다. 통과하는 시험을 실패로 적지 않는다. 제품 계산을 복제한 시험은 통과 차단이다.

- **3번.** 임시 저장소에 현재 BACKLOG보다 큰 고정 크기(1 MiB 미만)의 입력을 두고 읽어 `too-large`를 재현한다. 고정 크기로 쓰는 이유는 Rules 파일의 현재 크기에 시험이 흔들리지 않게 하기 위해서다. 실제 파일로는 `records:check`의 backlog 그룹 실패와 `backlog-store-actual.test.ts`로 함께 본다. 256 KiB 구간 상한과 BOM·UTF-8·경로·변경 중 읽기 거부는 회귀로 덮는다.
- **2번.** `runProcess` 대역에 프로세스별 `/proc/<pid>/stat` 응답을 주고 재현한다. 붙은 뒤 번호 재사용(TERM이 나가면 실패), TERM 뒤 대기 중 재사용(KILL이 나가면 실패), 읽기 실패(신호가 나가면 실패)를 따로 쓴다. 대역의 응답은 사례마다 달라야 한다. 모든 명령에 생존을 답하는 대역으로 헛통과시키지 않는다.
- **1번.** 실제 백엔드 프로세스 둘을 임시 폴더에서 띄운다. 「열린 run.json이 닫힌다」와 「활성 로그가 지워진다」를 따로 시험해 한쪽만 재현돼도 기록하게 한다.

## 실행 표

| 단계 | 명령 | 자원·부작용 | 상태 |
|---|---|---|---|
| 프런트엔드 시험·타입 | `05_Management/frontend`에서 `npm run mcp:build` 뒤 `npm test`, `npm run typecheck`, `npm run desktop:typecheck`, `npm run mcp:typecheck` | 이 checkout의 `node_modules`. `mcp:build`는 Git 제외 `mcp-dist/`만 쓴다. 이것 없이 돌린 선행 시험 기준선에서는 MCP 시험 37건과 파일 2개가 빌드 출력 없음으로 실패했다. 그 MCP 시험이 공동 조회의 구간 상한을 지키므로 구현 전후 기준선에 넣는다. 이 PC의 Node v24.14.1은 `engines`의 `^24.15.0` 밖이라 `npm ci`가 경고를 낸다(exit 0). 기준선과 수정 뒤를 같은 Node로 돌리고 버전을 적는다. | 계획, 요청 없이 실행 |
| 기록 검사 | `npm run records:check` | 읽기만(전이 import 정적 검사 `records-check/import-write-audit.out`) | 리드의 범위 확인 1회 끝. 기준선은 선행 시험 작성자가 다시 돈다 |
| 백엔드 시험 | 근거 폴더의 `backend-test-preflight/backend-test-run.sh <새 출력 폴더>`를 `MSYS_NO_PATHCONV=1 wsl.exe -d Ubuntu --exec bash`로 실행한다. 이 harness가 `env HOME=/tmp/management-audit-home DAWNHOLDER_DOTNET=/home/user/.dotnet/dotnet bash <이 checkout의 WSL 경로>/05_Management/backend/backend-wsl.sh test`를 감싸고 앞뒤 불변 근거를 남긴다. | 임시 HOME에 빌드 사본과 NuGet 캐시. 사용자의 `~/.cache`·설정·데이터 폴더를 쓰지 않는다. `HOME` 전달과 SDK 위치는 `printenv`·`--list-sdks`로 확인했고 시험 전체는 미실행이다. 「실행 조건」의 코드 확인과 전후 불변 근거를 함께 남긴다. | 실행 창 필요 |
| 2번 실제 진입 | 검증자 실행 도구가 임시 연결 파일에 대역 프로세스의 번호를 적고 실제 `wsl.exe`로 확인·신호 경로를 지난다 | 대역 프로세스만 신호를 받는다. WSL을 쓴다. | 실행 창 필요 |
| 운영툴 실제 실행 | 메인이 준 창에서 Electron | 실행 도구가 `DAWNHOLDER_MANAGEMENT_BACKEND_CONFIG`·`DAWNHOLDER_MANAGEMENT_DATA_DIR`와 설정 안의 임시 `dataDirectory`, 7777이 아닌 `server.port`, 임시 `server.portLockFile`을 확인하고 아니면 거절한다. 이 앱의 백엔드는 실제 HOME의 `backend-src.lock`을 쓰므로 사용자 운영툴이 닫혀 있어야 한다. 실제 `.source-owner`가 `management-active`를 가리켜 이 폴더에서 앱을 띄우면 백엔드가 거절될 것이다(정적 대조, 미실행). 표식을 고치면 시연 폴더의 다음 기동이 막히므로 건드리지 않고 방법은 발표 뒤 메인과 정한다. | 발표 뒤, 실행 창 필요 |

두 변수를 주지 않고 Electron을 띄우면 앱이 실제 `connection.json`에 붙고, 종료 때 그 백엔드에 TERM을 보낸다. 그래서 실행 도구의 확인이 필수다.

## 실행 조건

`msg_63fdd6f7b560`과 착수 회신 `msg_2296395f7b76`의 조건이다. 작업자 계약에 그대로 넣는다.

- 실행 창 요청에는 명령, 예상 시간, 쓰는 포트와 임시 경로를 적는다. 메인은 한 번에 실행 창 하나만 내주고 창마다 직전에 사용자 쪽 실행을 확인한다. 11-11 12:00 KST부터 발표가 끝날 때까지는 실행 창을 내주지 않는다.
- `management-active`는 읽기만 한다. 그 폴더의 git 읽기는 `GIT_OPTIONAL_LOCKS=0`으로만 한다.
- 인용부호 밖에 특수 공백(U+00A0 등)이 든 Bash 명령을 치지 않는다. 그 세션의 Bash가 멈춘다. 모든 작업자와 검증자 계약에 이 문장을 넣는다.
- 수정 전후는 같은 Node 버전으로 비교한다(이 PC는 v24.14.1).
- Claude 5시간 한도에 걸리면 재시도를 반복하지 않고 상태를 이 goal에 남긴 뒤 메인에 알린다. 사용자 결정이 필요한 일은 메인에 올리고 그 항목만 멈춘 채 나머지를 진행한다.

- 사용자의 게임 서버는 같은 WSL VM(Ubuntu)에서 돈다고 본다. `wsl --shutdown`·`wsl --terminate`와 남의 프로세스 종료는 금지다. 사용자의 운영툴 백엔드 프로세스에 신호를 보내지 않는다.
- 백엔드 시험을 처음 돌리기 전에 시험 기본값이 포트 7777과 실제 데이터 경로를 쓰지 않는다는 것을 코드로 확인하고 근거를 남긴다. 확인되지 않으면 돌리지 않는다.
- 백엔드 시험은 위 실행 표의 harness(HOME 분리 명령을 감싼다)로만 돌린다. 시험 앞뒤로 실제 `backend-src.lock`, `.source-owner`, 실제 데이터 폴더가 바뀌지 않았다는 근거(크기·수정 시각·내용 또는 hash)를 남긴다.
- WSL을 쓰는 실행(백엔드 시험), Electron을 띄우는 실행, 포트 7777을 쓰는 실행은 시작 전에 메인에 「실행 창 요청」(무엇·예상 시간·근거 경로)을 보내고 회신을 받은 뒤에만 한다. 프런트엔드의 `npm test`와 typecheck는 요청 없이 돌려도 된다.
- 시험과 재현은 임시 데이터 폴더와 임시 포트로만 한다. 2번의 프로세스 번호 재사용 재현은 작업자가 만든 대역 프로세스로만 한다.
- 사용자의 시연 환경을 건드리지 않는다: 메인 checkout, `management-active`, 사용자 서버 창, 사용자가 띄운 운영툴 앱과 그 실제 데이터 폴더·연결 파일·포트, 열려 있는 Unity Editor와 게임 클라이언트.

## 메인 회신

앞 초안의 「메인에게 묻는 것」 세 항목에 대한 `msg_63fdd6f7b560`의 답이다.

1. 시연 앱의 폴더: 메인은 `management-active`로 본다(폴더 상태 판단, 사용자 확인 없음). 그래서 이 별도 폴더에서 작업한다.
2. 범위와 PR 경계는 승인 범위 문서와 맞는다. 2번의 「인증된 종료 API를 만들지 않음」과 1번의 「즉시 거절」에 이견이 없다. 3번을 첫 PR로 따로 두는 것도 그대로 간다.
3. 운영툴 실제 실행: 발표 전에는 창을 주지 않는다. 실제 소유 표식·잠금·데이터 폴더는 건드리지 않는다. 실제 실행은 발표 뒤 메인과 방법을 정해 백엔드 수명 PR의 병합 승인 요청 전에 1회 한다. 그때까지 못 하면 미실행으로 적고 통과로 적지 않는다.

## 범위 확인 실행 결과

메인 지시로 리드가 착수 전에 한 번 돌린 결과다. 원시는 근거 폴더의 `records-check/`에 있다.

- `npm run records:check`는 `backlog=failed`, 종료 코드 2였다. 제품 저장소를 직접 불렀을 때 BACKLOG 읽기의 결과 코드는 `too-large`다(파일 336,542바이트, `backlog-read-probe.out`).
- `too-large` 뒤의 경로는 관측하지 못했다. `record-index-check.ts`는 읽기에 성공해도 `uncheckedLinks`가 있으면 backlog를 실패로 처리하므로, 고친 뒤 `backlog=ran`이 되는지는 구현 뒤 실행으로 확인한다.
- 범위 밖 기존 오류: 같은 실행의 `SOURCE_TOO_LARGE` 한 건은 색인 `persistence-integration-goal`의 「재개 지점」 구간이 256 KiB 상한을 넘은 것이다(`catalog-section-probe.out`). 공유 구간 상한을 올리지 않는다는 이 goal의 경계에 따라 고치지 않고 다음 계획 후보로 둔다.
- 기준 판이 `c9dc44e4`로 올라가 BACKLOG는 340,216바이트가 됐다(`wc -c`, 11-11 02:11 KST). 3번 시험은 특정 바이트 수에 기대지 않고, PR을 올리기 전에 실제 크기를 다시 잰다.

## 선행 시험 결과

`[Management 시험 Opus]` 한 세션이 계약 v1과 보충 v1.1로 썼다(근거 폴더 `tests-first/report.md`). 리드가 원시 표본을 대조했다.

- 수정 전에 요구 사유로 실패하는 시험: 3번 4건, 2번 24건, 1번 4건. 컴파일·환경 실패는 0건이다.
- 고친 기존 시험은 사용자에게 보인 넷이고, 그중 2번 두 파일은 대역만 보정해 단정이 그대로다. 수정 뒤 깨질 기존 시험의 전수 결과 추가 대상은 0건이다(코드 읽기, 미실행).
- 백엔드 기준선: 99 통과, 실제 경로 불변, 잔여 프로세스 0(`tests-first/backend-runs/baseline/`). 1번 실패 확인은 같은 명령으로 105건 중 4건 실패다(`backend-runs/item1-red/`).
- 프런트엔드 기준선: `npm test`가 수정 전부터 45건 실패했다. 분류는 `tests-first/frontend-baseline/failures-classified.md`다. 3번 실제 회귀 2, 범위 밖 실제 회귀 1(위 `persistence-integration-goal` 구간), 옛 세부 단정 1(`desktop-main.test.ts`의 1280×720), MCP 빌드 출력 없음 37, 원인 미확정 4다. 이 goal 밖의 실패는 고치지 않고 PR 보고에 공개한다.
- 설계 보충(v1.1, 리드 결정): 붙을 때 `/proc/<pid>/stat`을 못 읽어도 붙기는 성공하고 종료 신호만 보내지 않고 기록한다. 기록은 대상 pid가 든 로그 줄로 판정한다. 둘째 백엔드의 거절 출력에는 데이터 폴더 경로가 든다.

## 구현 중 정한 것

- 3번 구현 계약 보충 v1.1(근거 폴더 `impl-item3/contract-v1.1-supplement.md`, 구현자 질문 `msg_574805ceff57`의 리드 답).
  - 「건드릴 곳」 3번 밖의 문서 줄 하나를 넣었다. `goals/2026-10-06-record-source-unification/index-v2-design.md` 「원문 구간 읽기 경계」의 「진입 함수는 둘」 문장(61행)과 진입 함수 목록 한 줄이다. 새 전체 읽기 진입점이 그 문장을 틀리게 만들기 때문이다. 같은 이유로 `backlog-menu-design.md`는 71행 말고 60행의 진입점 이름도 고칠 수 있게 했다. PR 본문에 적는다.
  - BOM은 종전 동작을 유지한다. 256 KiB 이하 BACKLOG에서 전체 읽기의 텍스트는 공유 읽기(`section: null`)와 같다. 크기 상한 말고는 바뀌지 않게 하려는 것이다.

- 둘째 PR 2번 구현의 한계(리드 수용, 구현 보고 `impl-item12/report.md` 「계약이 정하지 않은 곳의 해석」). 401 재읽기 뒤 재시도가 401이 아닌 응답을 받기 전까지 새 연결의 시작 시각은 「확인할 수 없음」이다. 재시도가 오류·시간 초과로 끝나면 그 상태가 다음 401 재읽기나 다시 연결할 때까지 남는다. 그동안 앱 종료는 그 백엔드에 신호를 보내지 않고 pid가 든 기록만 남긴다. 신호를 아끼는 쪽으로 실패하고, 401 재읽기와 재시도 실패가 겹치는 드문 경로라 받아들였다. 독립 검증자가 따로 판정하고 PR 본문에 적는다.

## 3번 검증 결과와 결함 #1

새 독립 검증자 `[Management 검증자]`(Task `task_f0ff1218d82e`)의 판정은 **차단(결함 #1)**이다. 판정 파일은 근거 폴더 `verify-item3/verdict.md`(SHA256 `b8f2c91ed311f8f0ee3195cd8ea31d5ba40cc97b40c8beb5b313f6116ed1aeaa`)다. 대상은 HEAD `35eca851`(구현 커밋 `51a096e3`)이다.

- 지적 없음: 구현 계약 조건 1~7, 선행 시험, 실제 진입(`records:check`가 실제 BACKLOG로 `backlog=ran`, 종료 코드는 범위 밖 오류로 1).
- 결함 #1(중간, 차단): 표준 `npm test`에서 실제 저장소를 읽는 시험이 기본 5초 한도를 오간다. `backlog-store-actual`은 6회 중 3회 시간 초과였고, `diagram-asset-desktop`의 IPC 시험은 인자 없는 두 실행 모두 새로 실패했다. 반환 결과는 매번 정확했다.
- 원인(측정): 저장소 읽기 한 번은 단독 233~451 ms, 전체 실행 안 1,079~1,944 ms다. 그중 goal 링크 판정 322회가 단독 89.5~93.3%, 전체 실행 안 95~98%다. 새 전체 읽기와 표 해석은 60 ms 아래다. 읽기가 성공하면서 그동안 돌지 않던 기존 링크 판정(`backlog-store.ts`의 링크마다 `inspectSourceFile`)이 돌게 된 것이다.
- 리드 결정(수정 방향): 제품 쪽에서 `backlog-store.ts` 안에서만 고친다. 공유 경로 확인 `resolveSourceFile`은 MCP까지 쓰는 경계라 건드리지 않는다. 한 번의 읽기 안에서 같은 locator의 판정 결과를 재사용하는 것을 먼저 보고, issue는 지금처럼 등장마다 낸다. 동시 판정은 순서 위험이 있어 그것으로 부족할 때만 쓴다. 수정 전에 시험 작성자가 중복 링크 fixture에서 경로 확인 횟수를 세는 결정적 시험을 먼저 쓴다. 시험 한도를 올리는 쪽은 BACKLOG가 자라면 다시 걸리고 사용자에게 보인 고칠 시험 넷 밖이라 택하지 않았다.
- 재검증은 통과 횟수가 아니라 소요로 판정한다. 지금 실패율이면 고치지 않아도 두 번 연속 통과할 수 있기 때문이다. 전체 실행 안의 해당 시험 ms와 측정용 시험(근거 폴더 `verify-item3/probe-files/`)의 링크 판정 ms를 전후로 비교한다.
- 그 밖: 검증자는 `mcp-v2-bytes` 기존 실패를 (b) 환경에서 (c) 범위 밖 실제 회귀로 재분류했다. 검증자의 판정용 시험 `source-section-whole-read.test.ts`는 첫 branch에 커밋했다(`313c6d84`).
- 메인 회신(`msg_686d958652b2`): 결함 #1의 처분, 작업 순서 변경, 소요 ms 전후 비교 판정을 받아들였다. 측정 원시와 계산 방법을 근거 폴더에 남긴다. `mcp-v2-bytes` 재분류는 종료 기록의 다음 계획 후보에 재현 명령과 함께 적는다.

## 둘째 PR 검증 결과

새 독립 검증자 `[Management 검증자]`(Task `task_dd012a9588d5`)의 판정은 **통과, 결함 0**이다. 판정 파일은 근거 폴더 `verify-item12/verdict.md`(SHA256 `74df005c8838dce778d3f30c2fd589079eab5cb78c924bcd58aeca261b909672`)다. 대상은 HEAD `e2dcd96b`(구현 커밋 `12aacca3`)다. 리드는 아래 수치를 원천 파일에서 대조했다.

- 1번: 백엔드 전체 시험 105/105 통과다(`verify-item12/backend-run/summary.txt`, 실제 상태 변화 없음, 남은 프로세스 0). 거절 출력 probe는 둘째 백엔드가 0.1초 만에 종료 코드 1로 끝나고 연결 파일을 쓰지 않았다(`backend-run-probe/stdout.log`). 거절 문구가 출력에 그대로 나왔다. 표준 오류로 나갔다는 근거는 코드뿐이다(probe가 두 출력을 합쳐 찍는다).
- 2번: 실제 `wsl.exe` 진입은 대역 sleep 사본 셋으로 돌았다. 고친 판(SHA256 `dc267c2c…6f41a4`)의 두 실행이 모두 7/7 통과다. 사례별 기록(`real-entry-3/records-summary.txt`)에서 신호는 대역 pid 둘에 TERM 하나씩뿐이고, 막힌 명령은 0이다. 끝난 대역에는 신호 0과 pid 기록이 남았다. 첫 판은 대역 이름 준비 실패로 6/7이었다(uutils 다중 호출 sleep이 파일 이름으로 도구를 고르지 못함).
- 프런트 전체 `npm test`: 기준선 34건 실패에서 4단계 9건이다(새 실패 0). 6단계 재확인은 10건이다(새 1건). 새 1건은 `mcp-v3-r1-server` R06 modern 시험의 5,064 ms 시간 초과다(한도 5,000, 4단계 3,781 ms 통과). 전이 import 20파일과 자식 진입점이 바뀐 2번 경로에 닿지 않아 (d) 미확정으로 분류했다. 두 실행 모두 아래 검증자 시험 파일이 작업 트리에 있는 상태였다. 그 파일을 뺀 PR tree의 통과 수는 따로 재지 않았다.
- 실제 진입이 지나지 않은 경로: TERM 뒤 30초 KILL, 401 재읽기, 번호 재사용(대역 시험과 코드 실사만). Electron 실행은 완료조건 6대로 발표 뒤에 한다.
- 401 재읽기 한계(「구현 중 정한 것」): 검증자가 완료조건 3과 어긋나지 않는다고 판정했다.
- 리드 결정 1, 검증자 시험 `backend-connection-real-wsl-entry.test.ts`는 이 PR에 넣지 않는다. 메인 실행 창 없이는 도는 사례가 없고, 넣으려면 233행 주석(reporter 설명) 정정에 작성 세션과 검증이 한 번 더 필요하다. 사본은 근거 폴더 `verify-item12/real-entry-test/`(SHA256 같음)에 두고 작업 트리에서 지웠다. 저장소에 넣을지는 종료 기록의 다음 계획 후보로 남긴다.
- 리드 결정 2, 설계 관찰 둘은 종료 기록의 다음 계획 후보로 넘긴다. 하나는 붙을 때 `/proc` 읽기가 한 번 실패하면 그 연결 동안 신호를 보내지 않는 점이다(보충 v1.1 결정 1의 동작). 다른 하나는 TERM 뒤 대기 중 pid가 바뀌면 30초를 다 기다리는 점이다. 둘 다 신호를 아끼는 쪽이고 기록으로 드러난다. 프런트 기본 5초 시험 한도의 흔들림도 같은 후보에 적는다.
- 남은 것(미확인, 차단 아님): 시험용 분리 HOME `/tmp/management-audit-home`의 백엔드 소스 사본에 probe 시험 사본과 빌드 출력이 남았을 수 있다. 다음 harness 실행의 `rsync --delete`가 소스 사본을 지운다(추론). 실행 창을 따로 요청하지 않았다.

## 리드 운영 기록

- 11-11 05:18 KST 무렵 첫 branch push를 커밋 명령과 한 줄로 이어 실행했다. 메인 조건 「push와 다른 명령은 나눠 실행」 위반이다. push 결과는 정상이었다(새 원격 branch `fix/management-audit-fixes-20261110`). 메인에 보고했고(`msg_9ddd9f3886a4`), 메인은 추가 조치 없이 기록만 하라고 했다(`msg_686d958652b2`). 이후 push는 단독 명령으로 돈다.

## 백엔드 시험 안전 전제

시험 기본값이 포트 7777과 실제 데이터 경로를 쓰지 않는다는 것을 코드로 확인했다(정적 대조이며 실행 관측이 아니다). 표와 예외 검색 범위는 근거 폴더의 `backend-test-preflight/defaults-check.md`다.
